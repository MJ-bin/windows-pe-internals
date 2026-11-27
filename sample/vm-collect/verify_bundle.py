#!/usr/bin/env python3
"""Check a VM bundle made by collect_vm.ps1 on the host (Linux).

usage:
  verify_bundle.py INCOMING_DIR [BUNDLE_DIR] [--content all|N]

  INCOMING_DIR  the folder copied from the VM (has *.tar.gz, *.sha256, info.txt, manifest.csv ...)
  BUNDLE_DIR    where the archives were extracted (registry/ sd/ disk/ sysinfo/ objects/ probe/ C/).
                If given, manifest.csv rows and content.csv rows are checked against the extracted files.
  --content     how many content.csv rows to hash (default: all). Use a number for a quick sample.

Exit code 0 = every check passed, 1 = something did not match (listed on stdout).
"""
import csv
import hashlib
import os
import random
import sys

TARGET_BUILD = "26100.9457"


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def read_csv(path):
    with open(path, encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def count_rows(path):
    n = 0
    with open(path, "rb") as f:
        for _ in f:
            n += 1
    return n


def main(argv):
    args = [a for a in argv[1:] if not a.startswith("--")]
    content_limit = None
    if "--content" in argv:
        v = argv[argv.index("--content") + 1]
        args = [a for a in args if a != v]
        content_limit = None if v == "all" else int(v)
    if not args:
        print(__doc__)
        return 2
    incoming = args[0]
    bundle = args[1] if len(args) > 1 else None
    bad = []

    # 1. info.txt: build and layout
    info = {}
    with open(os.path.join(incoming, "info.txt"), encoding="utf-8-sig") as f:
        for line in f:
            if ":" in line:
                k, v = line.split(":", 1)
                info.setdefault(k.strip(), v.strip())
    if info.get("full_build") != TARGET_BUILD:
        bad.append("info.txt full_build is %r, expected %s" % (info.get("full_build"), TARGET_BUILD))
    print("build %s  layout %s  collected %s" % (info.get("full_build"), info.get("layout_version"), info.get("collected_at_utc")))

    # 2. archives against their .sha256
    for name in sorted(os.listdir(incoming)):
        if not name.endswith(".tar.gz"):
            continue
        side = os.path.join(incoming, name + ".sha256")
        if not os.path.exists(side):
            bad.append("%s: no .sha256" % name)
            continue
        want = open(side, encoding="utf-8").read().split()[0].lower()
        got = sha256(os.path.join(incoming, name))
        print("%-26s %s" % (name, "OK" if got == want else "MISMATCH"))
        if got != want:
            bad.append("%s: sha256 %s != %s" % (name, got, want))
    for line in open(os.path.join(incoming, "info.txt"), encoding="utf-8-sig"):
        if line.startswith("archive ") and "exit 0" not in line:
            bad.append("tar did not exit 0: " + line.strip())

    if bundle:
        # 3. manifest.csv: every file inside registry.tar.gz / meta.tar.gz
        rows = read_csv(os.path.join(incoming, "manifest.csv"))
        miss = 0
        for r in rows:
            p = os.path.join(bundle, r["RelPath"])
            if not os.path.exists(p):
                bad.append("manifest: missing " + r["RelPath"]); miss += 1; continue
            if os.path.getsize(p) != int(r["Size"]):
                bad.append("manifest: size differs " + r["RelPath"])
            elif sha256(p) != r["SHA256"].lower():
                bad.append("manifest: sha256 differs " + r["RelPath"])
        print("manifest rows %d, missing %d" % (len(rows), miss))

        # 4. tree.csv keeps the old 6 columns (ref_reg_users.py is strict)
        with open(os.path.join(bundle, "disk", "tree.csv"), encoding="utf-8-sig", newline="") as f:
            header = next(csv.reader(f))
        if header != ["Path", "Kind", "Size", "Attributes", "LinkType", "Target"]:
            bad.append("disk/tree.csv header changed: %r" % header)

        # 5. content.csv against C/
        content = read_csv(os.path.join(bundle, "disk", "content.csv"))
        pick = content if content_limit is None else random.sample(content, min(content_limit, len(content)))
        cmiss = 0
        for i, r in enumerate(pick):
            rel = r["Path"][3:].replace("\\", "/")          # C:\Windows\... -> Windows/...
            p = os.path.join(bundle, "C", rel)
            if not os.path.exists(p):
                bad.append("content: missing C/" + rel); cmiss += 1; continue
            if os.path.getsize(p) != int(r["Size"]):
                bad.append("content: size differs C/" + rel)
            elif sha256(p) != r["SHA256"].lower():
                bad.append("content: sha256 differs C/" + rel)
            if (i + 1) % 20000 == 0:
                print("  content checked %d / %d" % (i + 1, len(pick)))
        print("content rows %d, checked %d, missing %d" % (len(content), len(pick), cmiss))

    print()
    if bad:
        print("PROBLEMS (%d):" % len(bad))
        for b in bad[:200]:
            print("  " + b)
        return 1
    print("ALL OK")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
