// VmDisk.cs - content rules (which file bytes are collected), dry-run counts, content hashing / tar list,
// and per-path metadata (tree_meta.csv: times, file id, short name, links, allocation, reparse tag, streams, SD).
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public static class Disk
{
    // ---------- rules: first match wins ----------
    static List<Regex> ruleRegex = new List<Regex>();
    static List<string> ruleKind = new List<string>();
    static List<string> ruleMode = new List<string>();
    static List<string> ruleName = new List<string>();
    static List<string> rulePattern = new List<string>();
    public static string DefaultMode = "name";
    public static string DriveRoot = "C:\\";

    /// pattern: relative to the drive root, '\' separated, case-insensitive. '*' = inside one name, '**' = anything,
    /// a trailing '\**' = that folder and everything below. kind: dir | file | *. mode: content | name.
    public static void SetRules(string[] patterns, string[] kinds, string[] modes, string[] names)
    {
        ruleRegex.Clear(); ruleKind.Clear(); ruleMode.Clear(); ruleName.Clear(); rulePattern.Clear();
        for (int i = 0; i < patterns.Length; i++)
        {
            ruleRegex.Add(new Regex(GlobToRegex(patterns[i]), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
            ruleKind.Add(kinds[i]); ruleMode.Add(modes[i]); ruleName.Add(names[i]); rulePattern.Add(patterns[i]);
        }
    }

    static string GlobToRegex(string glob)
    {
        bool below = glob.EndsWith("\\**");
        string body = below ? glob.Substring(0, glob.Length - 3) : glob;
        StringBuilder sb = new StringBuilder("^");
        for (int i = 0; i < body.Length; i++)
        {
            char c = body[i];
            if (c == '*' && i + 1 < body.Length && body[i + 1] == '*') { sb.Append(".*"); i++; }
            else if (c == '*') sb.Append("[^\\\\]*");
            else sb.Append(Regex.Escape(c.ToString()));
        }
        if (below) sb.Append("(\\\\.*)?");
        return sb.Append("$").ToString();
    }

    public static string Relative(string fullPath)
    {
        if (fullPath.StartsWith(DriveRoot, StringComparison.OrdinalIgnoreCase)) return fullPath.Substring(DriveRoot.Length);
        return fullPath;
    }

    /// index of the first matching rule, or -1 (= DefaultMode)
    public static int Classify(string fullPath, bool isDir)
    {
        string rel = Relative(fullPath);
        for (int i = 0; i < ruleRegex.Count; i++)
        {
            string k = ruleKind[i];
            if (k == "file" && isDir) continue;
            if (k == "dir" && !isDir) continue;
            if (ruleRegex[i].IsMatch(rel)) return i;
        }
        return -1;
    }

    public static string ModeOf(int rule) { return rule < 0 ? DefaultMode : ruleMode[rule]; }
    public static string NameOf(int rule) { return rule < 0 ? "default" : ruleName[rule]; }

    // ---------- CSV like Export-Csv (every field quoted, UTF-8 with BOM, CRLF) ----------
    public static StreamWriter OpenCsv(string path, string[] header)
    {
        StreamWriter w = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(true), 65536);
        w.NewLine = "\r\n";
        WriteCsv(w, header);
        return w;
    }

    public static void WriteCsv(StreamWriter w, string[] fields)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append((fields[i] ?? "").Replace("\"", "\"\"")).Append('"');
        }
        w.WriteLine(sb.ToString());
    }

    // ---------- dry run ----------
    /// paths / sizes / isDir = every tree row. Writes dryrun.csv (per rule) and dryrun_top.csv (per two top folders).
    /// Returns content files and bytes.
    public static long[] DryRun(string[] paths, long[] sizes, bool[] isDir, string ruleCsv, string topCsv)
    {
        Dictionary<int, long[]> perRule = new Dictionary<int, long[]>();
        Dictionary<string, long[]> perTop = new Dictionary<string, long[]>(StringComparer.OrdinalIgnoreCase);
        long contentFiles = 0, contentBytes = 0;
        for (int i = 0; i < paths.Length; i++)
        {
            if (isDir[i]) continue;
            int r = Classify(paths[i], false);
            string mode = ModeOf(r);
            long[] a;
            if (!perRule.TryGetValue(r, out a)) { a = new long[2]; perRule[r] = a; }
            a[0]++; a[1] += sizes[i];
            string[] parts = Relative(paths[i]).Split('\\');
            string top = parts.Length >= 3 ? parts[0] + "\\" + parts[1] : (parts.Length == 2 ? parts[0] : "(root files)");
            if (parts.Length >= 4 && string.Equals(parts[1], "WindowsApps", StringComparison.OrdinalIgnoreCase)) top = parts[0] + "\\" + parts[1] + "\\" + parts[2];
            string key = top + "|" + mode;
            long[] b;
            if (!perTop.TryGetValue(key, out b)) { b = new long[2]; perTop[key] = b; }
            b[0]++; b[1] += sizes[i];
            if (mode == "content") { contentFiles++; contentBytes += sizes[i]; }
        }
        using (StreamWriter w = OpenCsv(ruleCsv, new string[] { "Rule", "Mode", "Pattern", "Files", "Bytes" }))
        {
            List<int> keys = new List<int>(perRule.Keys);
            keys.Sort();
            foreach (int r in keys)
                WriteCsv(w, new string[] { NameOf(r), ModeOf(r), r < 0 ? "(no rule)" : rulePattern[r], perRule[r][0].ToString(), perRule[r][1].ToString() });
        }
        using (StreamWriter w = OpenCsv(topCsv, new string[] { "Folder", "Mode", "Files", "Bytes" }))
        {
            List<string> keys = new List<string>(perTop.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string k in keys)
            {
                string[] fm = k.Split('|');
                WriteCsv(w, new string[] { fm[0], fm[1], perTop[k][0].ToString(), perTop[k][1].ToString() });
            }
        }
        return new long[] { contentFiles, contentBytes };
    }

    // ---------- content: hash + version + machine, tar list ----------
    static readonly string[] PeExt = new string[] { ".dll", ".exe", ".sys", ".drv", ".ocx", ".cpl", ".ax", ".acm", ".efi", ".mui", ".tlb", ".scr", ".com", ".mun" };

    /// For every content file: content.csv row, a line in listAnsi (relative, '/' separated, ANSI code page like the
    /// old -full collector), or - if the name does not survive the ANSI code page - a copy under stagingDir.
    /// Returns files, bytes, unreadable, non-ANSI.
    public static long[] Content(string[] paths, long[] sizes, string contentCsv, string listAnsi, string stagingDir)
    {
        long files = 0, bytes = 0, failed = 0, nonAnsi = 0;
        Encoding ansi = Encoding.Default;
        using (StreamWriter csv = OpenCsv(contentCsv, new string[] { "Path", "Size", "SHA256", "FileVersion", "Machine" }))
        using (StreamWriter list = new StreamWriter(new FileStream(listAnsi, FileMode.Create, FileAccess.Write, FileShare.Read), ansi, 65536))
        using (SHA256 sha = SHA256.Create())
        {
            list.NewLine = "\n";
            for (int i = 0; i < paths.Length; i++)
            {
                string p = paths[i];
                if ((i + 1) % 5000 == 0) Console.WriteLine("    content: " + (i + 1) + " / " + paths.Length);
                string hash;
                try
                {
                    using (FileStream fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1 << 16))
                        hash = Vc.Hex(sha.ComputeHash(fs)).ToUpperInvariant();
                }
                catch (Exception e) { Vc.Err("UNREADABLE", p, e.Message); failed++; continue; }
                string ver = "", mach = "";
                string ext = Path.GetExtension(p).ToLowerInvariant();
                if (Array.IndexOf(PeExt, ext) >= 0) { mach = PeMachine(p); ver = FileVersion(p); }
                string rel = Relative(p);
                bool ansiOk = ansi.GetString(ansi.GetBytes(rel)) == rel;
                if (ansiOk) list.WriteLine(rel.Replace('\\', '/'));
                else
                {
                    try
                    {
                        string dest = Path.Combine(stagingDir, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        File.Copy(p, dest, true);
                        nonAnsi++;
                    }
                    catch (Exception e) { Vc.Err("NONANSI-COPY", p, e.Message); failed++; continue; }
                }
                WriteCsv(csv, new string[] { p, sizes[i].ToString(), hash, ver, mach });
                files++; bytes += sizes[i];
            }
        }
        return new long[] { files, bytes, failed, nonAnsi };
    }

    static string PeMachine(string path)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                byte[] head = new byte[64];
                if (fs.Read(head, 0, 64) < 64 || head[0] != 'M' || head[1] != 'Z') return "";
                int lfanew = BitConverter.ToInt32(head, 0x3c);
                if (lfanew <= 0 || lfanew > fs.Length - 6) return "";
                fs.Seek(lfanew, SeekOrigin.Begin);
                byte[] pe = new byte[6];
                if (fs.Read(pe, 0, 6) < 6 || pe[0] != 'P' || pe[1] != 'E' || pe[2] != 0 || pe[3] != 0) return "";
                ushort m = BitConverter.ToUInt16(pe, 4);
                if (m == 0x8664) return "x64";
                if (m == 0x14c) return "x86";
                if (m == 0xaa64) return "arm64";
                if (m == 0x1c4) return "arm";
                return "0x" + m.ToString("x4");
            }
        }
        catch { return ""; }
    }

    [DllImport("version.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int GetFileVersionInfoSizeW(string path, out int handle);
    [DllImport("version.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetFileVersionInfoW(string path, int handle, int size, byte[] data);
    [DllImport("version.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool VerQueryValueW(byte[] block, string sub, out IntPtr buf, out int len);

    /// StringFileInfo FileVersion (the same text [Diagnostics.FileVersionInfo]::FileVersion gives)
    static string FileVersion(string path)
    {
        try
        {
            int dummy;
            int size = GetFileVersionInfoSizeW(path, out dummy);
            if (size <= 0) return "";
            byte[] block = new byte[size];
            if (!GetFileVersionInfoW(path, 0, size, block)) return "";
            IntPtr p; int len;
            List<string> keys = new List<string>();
            if (VerQueryValueW(block, "\\VarFileInfo\\Translation", out p, out len) && len >= 4)
            {
                for (int i = 0; i + 4 <= len; i += 4)
                {
                    ushort lang = (ushort)Marshal.ReadInt16(p, i), cp = (ushort)Marshal.ReadInt16(p, i + 2);
                    keys.Add(lang.ToString("x4") + cp.ToString("x4"));
                }
            }
            keys.Add("040904b0"); keys.Add("040904e4"); keys.Add("04090000");
            foreach (string k in keys)
                if (VerQueryValueW(block, "\\StringFileInfo\\" + k + "\\FileVersion", out p, out len) && len > 0)
                    return Marshal.PtrToStringUni(p).TrimEnd('\0');
            return "";
        }
        catch { return ""; }
    }

    // ---------- tree_meta.csv ----------
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr FindFirstFileW(string path, byte[] data);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool FindClose(IntPtr h);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr FindFirstStreamW(string path, int level, byte[] data, int flags);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool FindNextStreamW(IntPtr h, byte[] data);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetFileInformationByHandle(IntPtr h, byte[] info);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetFileInformationByHandleEx(IntPtr h, int cls, byte[] info, int size);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool GetKernelObjectSecurity(IntPtr h, int info, byte[] sd, int len, out int need);
    [DllImport("advapi32.dll")] static extern int GetSecurityDescriptorLength(byte[] sd);

    static readonly IntPtr INVALID = new IntPtr(-1);

    /// One row per distinct path (rows repeat in tree.csv where roots overlap).
    public static long Meta(string[] paths, string metaCsv)
    {
        Dictionary<string, bool> done = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        byte[] find = new byte[592], stream = new byte[600], byHandle = new byte[52], basic = new byte[40], standard = new byte[24], fileId = new byte[24];
        byte[] sd = new byte[8192];
        long rows = 0;
        using (StreamWriter w = OpenCsv(metaCsv, new string[] { "Path", "Created", "LastWrite", "LastAccess", "Change", "FileId", "ShortName", "Links", "AllocSize", "ReparseTag", "Streams", "Sd" }))
        {
            foreach (string path in paths)
            {
                if (done.ContainsKey(path)) continue;
                done[path] = true;
                if ((rows + 1) % 20000 == 0) Console.WriteLine("    meta: " + (rows + 1) + " / " + paths.Length);
                string created = "", lastWrite = "", lastAccess = "", change = "", id = "", shortName = "", links = "", alloc = "", tag = "", streams = "", sdId = "";
                try
                {
                    string probe = path.EndsWith("\\") ? null : path;   // drive roots have no find data
                    if (probe != null)
                    {
                        IntPtr fh = FindFirstFileW(probe, find);
                        if (fh != INVALID)
                        {
                            FindClose(fh);
                            created = Vc.FileTime(BitConverter.ToInt64(find, 4));
                            lastAccess = Vc.FileTime(BitConverter.ToInt64(find, 12));
                            lastWrite = Vc.FileTime(BitConverter.ToInt64(find, 20));
                            uint attrs = BitConverter.ToUInt32(find, 0);
                            if ((attrs & 0x400) != 0) tag = Vc.HexU32(BitConverter.ToUInt32(find, 36));
                            shortName = Encoding.Unicode.GetString(find, 564, 28).Split('\0')[0];
                        }
                    }
                    IntPtr h = Nt.CreateFileW(path, 0x80 | 0x20000, 7, IntPtr.Zero, 3, 0x02000000 | 0x00200000, IntPtr.Zero);
                    if (h == INVALID) h = Nt.CreateFileW(path, 0x80, 7, IntPtr.Zero, 3, 0x02000000 | 0x00200000, IntPtr.Zero);
                    if (h != INVALID)
                    {
                        try
                        {
                            if (GetFileInformationByHandle(h, byHandle))
                            {
                                links = BitConverter.ToUInt32(byHandle, 40).ToString();
                                if (id.Length == 0) id = BitConverter.ToUInt32(byHandle, 44).ToString("x8") + BitConverter.ToUInt32(byHandle, 48).ToString("x8");
                            }
                            if (GetFileInformationByHandleEx(h, 0x12, fileId, 24)) id = Vc.Hex(fileId, 24).Substring(16);   // FILE_ID_128 after the volume serial
                            if (GetFileInformationByHandleEx(h, 0, basic, 40))
                            {
                                change = Vc.FileTime(BitConverter.ToInt64(basic, 24));
                                if (created.Length == 0)
                                {
                                    created = Vc.FileTime(BitConverter.ToInt64(basic, 0));
                                    lastAccess = Vc.FileTime(BitConverter.ToInt64(basic, 8));
                                    lastWrite = Vc.FileTime(BitConverter.ToInt64(basic, 16));
                                }
                            }
                            if (GetFileInformationByHandleEx(h, 1, standard, 24)) alloc = BitConverter.ToInt64(standard, 0).ToString();
                            int need;
                            if (GetKernelObjectSecurity(h, SdTable.SECINFO, sd, sd.Length, out need)) sdId = SdTable.Add(sd, GetSecurityDescriptorLength(sd));
                            else if (need > sd.Length)
                            {
                                sd = new byte[need];
                                if (GetKernelObjectSecurity(h, SdTable.SECINFO, sd, sd.Length, out need)) sdId = SdTable.Add(sd, GetSecurityDescriptorLength(sd));
                            }
                        }
                        finally { Nt.CloseHandle(h); }
                    }
                    IntPtr sh = FindFirstStreamW(path, 0, stream, 0);
                    if (sh != INVALID)
                    {
                        StringBuilder sb = new StringBuilder();
                        int guard = 0;
                        do
                        {
                            string name = Encoding.Unicode.GetString(stream, 8, 592).Split('\0')[0];
                            if (sb.Length > 0) sb.Append(';');
                            sb.Append(name).Append('=').Append(BitConverter.ToInt64(stream, 0));
                        } while (++guard < 256 && FindNextStreamW(sh, stream));
                        FindClose(sh);
                        streams = sb.ToString();
                    }
                }
                catch (Exception e) { Vc.Err("META", path, e.Message); }
                WriteCsv(w, new string[] { path, created, lastWrite, lastAccess, change, id, shortName, links, alloc, tag, streams, sdId });
                rows++;
            }
        }
        return rows;
    }
}
