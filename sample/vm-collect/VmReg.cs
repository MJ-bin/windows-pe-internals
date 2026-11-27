// VmReg.cs - registry walker. The key-line format is byte-for-byte the one of
// PE_emulator/Script/collect_ref_reg_users.ps1 (RegUsersWalker), so the old reader still works.
// New: optional key_meta.jsonl line per opened key (last write FILETIME, class, sd id) and root listing.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class RegWalker
{
    const int KEY_READ = 0x20019;
    const int KEY_WOW64_64KEY = 0x0100;
    const int REG_OPTION_OPEN_LINK = 0x8;
    const int REG_LINK = 6;
    const int ERROR_FILE_NOT_FOUND = 2;
    const int ERROR_MORE_DATA = 234;
    const int ERROR_NO_MORE_ITEMS = 259;
    const int MAX_DEPTH = 512;
    const int MAX_RETRY = 3;
    const int MAX_FAILURES_IN_A_ROW = 64;
    const long PROGRESS_KEYS = 50000;
    const int KEYS = 0, VALUES = 1, LINKS = 2, SKIPPED = 3, DENIED = 4, EXCLUDED_VALUES = 5;
    static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
    static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RegOpenKeyExW(IntPtr hKey, string subKey, int options, int samDesired, out IntPtr result);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RegEnumKeyExW(IntPtr hKey, int index, [Out] char[] name, ref int nameLength,
        IntPtr reserved, IntPtr className, IntPtr classLength, IntPtr lastWriteTime);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RegEnumValueW(IntPtr hKey, int index, [Out] char[] name, ref int nameLength,
        IntPtr reserved, out int type, [Out] byte[] data, ref int dataLength);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RegQueryInfoKeyW(IntPtr hKey, IntPtr className, IntPtr classLength, IntPtr reserved,
        IntPtr subKeys, IntPtr maxSubKeyLength, IntPtr maxClassLength, IntPtr values,
        out int maxValueNameLength, out int maxValueLength, IntPtr securityDescriptor, IntPtr lastWriteTime);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryInfoKeyW")]
    static extern int RegQueryInfoKeyMeta(IntPtr hKey, [Out] char[] className, ref int classLength, IntPtr reserved,
        IntPtr subKeys, IntPtr maxSubKeyLength, IntPtr maxClassLength, IntPtr values,
        IntPtr maxValueNameLength, IntPtr maxValueLength, IntPtr securityDescriptor, out long lastWriteTime);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RegQueryValueExW(IntPtr hKey, string valueName, IntPtr reserved, out int type,
        [Out] byte[] data, ref int dataLength);
    [DllImport("advapi32.dll")]
    static extern int RegGetKeySecurity(IntPtr hKey, int info, [Out] byte[] sd, ref int length);
    [DllImport("advapi32.dll")]
    static extern int RegCloseKey(IntPtr hKey);

    static string[] k1KeySuffix = new string[0];
    static string[] k2LeafSuffix = new string[0];
    static string[] v1Contains = new string[0];
    static string[] v1Prefix = new string[0];
    static string[] v1Equals = new string[0];
    static StreamWriter outWriter;
    static StreamWriter excludedWriter;
    static StreamWriter errorWriter;
    static StreamWriter metaWriter;
    static long[] counts = new long[6];
    static string progressLabel = "";
    static char[] keyNameBuffer = new char[512];
    static char[] valueNameBuffer = new char[512];
    static char[] classBuffer = new char[512];
    static byte[] dataBuffer = new byte[4096];
    static byte[] linkBuffer = new byte[1024];
    static byte[] sdBuffer = new byte[4096];

    public static void SetRules(string[] k1, string[] k2, string[] contains, string[] prefix, string[] equals)
    {
        k1KeySuffix = k1;
        k2LeafSuffix = k2;
        v1Contains = contains;
        v1Prefix = prefix;
        v1Equals = equals;
    }

    /// Walk one root. metaPath may be null (no key_meta lines). Returns keys, values, links, skipped, denied, excluded_values.
    public static long[] WalkRoot(string hive, string subKey, string ntRoot,
        string outPath, string excludedPath, string errorsPath, string metaPath, string label)
    {
        counts = new long[6];
        progressLabel = label;
        outWriter = Vc.Open(outPath, false);
        excludedWriter = Vc.Open(excludedPath, true);
        errorWriter = Vc.Open(errorsPath, true);
        metaWriter = metaPath == null ? null : Vc.Open(metaPath, true);
        try { WalkRootKey(hive == "HKLM" ? HKEY_LOCAL_MACHINE : HKEY_USERS, subKey, ntRoot); }
        finally
        {
            outWriter.Dispose();
            excludedWriter.Dispose();
            errorWriter.Dispose();
            if (metaWriter != null) metaWriter.Dispose();
            metaWriter = null;
        }
        return counts;
    }

    static void WalkRootKey(IntPtr hive, string subKey, string ntRoot)
    {
        IntPtr key;
        int rc = RegOpenKeyExW(hive, subKey, REG_OPTION_OPEN_LINK, KEY_READ | KEY_WOW64_64KEY, out key);
        if (rc == ERROR_FILE_NOT_FOUND) { WriteError("MISSING-ROOT", ntRoot, "Win32 error " + rc); return; }
        if (rc != 0) { WriteDenied(ntRoot, rc); return; }
        try { WalkKey(key, ntRoot, 0); }
        finally { RegCloseKey(key); }
    }

    static void WalkKey(IntPtr key, string path, int depth)
    {
        WriteMeta(key, path);
        string target = ReadLinkTarget(key);
        if (target != null)
        {
            WriteKeyLine(path, "\"link\":" + Vc.Json(target), "");
            counts[LINKS]++;
            return;
        }
        string values = ReadValues(key, path);
        WriteKeyLine(path, "", values);
        List<string> names = ReadSubKeyNames(key, path);
        foreach (string name in names) WalkChild(key, path, name, depth + 1);
    }

    static void WalkChild(IntPtr parent, string parentPath, string name, int depth)
    {
        string path = parentPath + "\\" + name;
        string rule = MatchKeyRule(path, name);
        if (rule != null)
        {
            WriteKeyLine(path, "\"skipped\":\"" + rule + "\"", "");
            excludedWriter.Write("{\"kind\":\"key\",\"key\":" + Vc.Json(path) + ",\"rule\":\"" + rule + "\"}\n");
            counts[SKIPPED]++;
            return;
        }
        if (depth > MAX_DEPTH) { WriteError("DEPTH", path, "depth limit " + MAX_DEPTH); return; }
        IntPtr key;
        int rc = RegOpenKeyExW(parent, name, REG_OPTION_OPEN_LINK, KEY_READ | KEY_WOW64_64KEY, out key);
        if (rc != 0) { WriteDenied(path, rc); return; }
        try { WalkKey(key, path, depth); }
        finally { RegCloseKey(key); }
    }

    // key_meta.jsonl: {"key":...,"last_write":"<16 hex FILETIME>","class":...,"sd":"<id>"}
    static void WriteMeta(IntPtr key, string path)
    {
        if (metaWriter == null) return;
        long lastWrite = 0;
        int classLength = classBuffer.Length;
        int rc = RegQueryInfoKeyMeta(key, classBuffer, ref classLength, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out lastWrite);
        if (rc == ERROR_MORE_DATA)
        {
            classBuffer = new char[classLength + 1];
            classLength = classBuffer.Length;
            rc = RegQueryInfoKeyMeta(key, classBuffer, ref classLength, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out lastWrite);
        }
        string className = rc == 0 ? new string(classBuffer, 0, classLength) : "";
        int sdLength = sdBuffer.Length;
        int src = RegGetKeySecurity(key, SdTable.SECINFO, sdBuffer, ref sdLength);
        if (src == 122) // ERROR_INSUFFICIENT_BUFFER
        {
            sdBuffer = new byte[sdLength];
            src = RegGetKeySecurity(key, SdTable.SECINFO, sdBuffer, ref sdLength);
        }
        string sdId = src == 0 ? SdTable.Add(sdBuffer, sdLength) : "";
        Vc.Obj o = new Vc.Obj().S("key", path).S("last_write", rc == 0 ? Vc.FileTime(lastWrite) : "").S("class", className).S("sd", sdId);
        if (rc != 0) o.N("info_error", rc);
        if (src != 0) o.N("sd_error", src);
        metaWriter.Write(o.ToString());
        metaWriter.Write('\n');
    }

    static void WriteDenied(string path, int rc)
    {
        WriteKeyLine(path, "\"denied\":" + rc, "");
        WriteError("DENIED-KEY", path, "Win32 error " + rc);
        counts[DENIED]++;
    }

    static string MatchKeyRule(string path, string name)
    {
        foreach (string suffix in k1KeySuffix)
            if (path.EndsWith("\\" + suffix, StringComparison.OrdinalIgnoreCase)) return "K1";
        foreach (string suffix in k2LeafSuffix)
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return "K2";
        return null;
    }

    static bool MatchValueRule(string name)
    {
        if (name.Length == 0) return false;
        foreach (string part in v1Contains)
            if (name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        foreach (string prefix in v1Prefix)
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (string whole in v1Equals)
            if (string.Equals(name, whole, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static string ReadLinkTarget(IntPtr key)
    {
        int type;
        int length = linkBuffer.Length;
        int rc = RegQueryValueExW(key, "SymbolicLinkValue", IntPtr.Zero, out type, linkBuffer, ref length);
        if (rc == ERROR_MORE_DATA)
        {
            linkBuffer = new byte[length];
            rc = RegQueryValueExW(key, "SymbolicLinkValue", IntPtr.Zero, out type, linkBuffer, ref length);
        }
        if (rc != 0 || type != REG_LINK) return null;
        return BytesToChars(linkBuffer, length).TrimEnd('\0');
    }

    static string BytesToChars(byte[] data, int length)
    {
        char[] chars = new char[length / 2];
        for (int i = 0; i < chars.Length; i++) chars[i] = (char)(data[2 * i] | (data[2 * i + 1] << 8));
        return new string(chars);
    }

    static void QueryValueMaxima(IntPtr key, out int maxName, out int maxData)
    {
        int rc = RegQueryInfoKeyW(key, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
            IntPtr.Zero, IntPtr.Zero, out maxName, out maxData, IntPtr.Zero, IntPtr.Zero);
        if (rc != 0) { maxName = 0; maxData = 0; }
    }

    static void EnsureValueBuffers(int maxName, int maxData)
    {
        if (valueNameBuffer.Length < maxName + 1) valueNameBuffer = new char[maxName + 1];
        if (dataBuffer.Length < maxData) dataBuffer = new byte[maxData];
    }

    static void GrowValueBuffers(IntPtr key, int dataLength)
    {
        int maxName, maxData;
        QueryValueMaxima(key, out maxName, out maxData);
        EnsureValueBuffers(Math.Max(maxName, valueNameBuffer.Length * 2),
            Math.Max(Math.Max(maxData, dataLength), dataBuffer.Length * 2));
    }

    static string ReadValues(IntPtr key, string path)
    {
        int maxName, maxData;
        QueryValueMaxima(key, out maxName, out maxData);
        EnsureValueBuffers(maxName, maxData);
        StringBuilder values = new StringBuilder();
        int failures = 0;
        for (int index = 0; ; index++)
        {
            int rc = ReadValueAt(key, path, index, values);
            if (rc == ERROR_NO_MORE_ITEMS) break;
            failures = (rc == 0) ? 0 : failures + 1;
            if (failures >= MAX_FAILURES_IN_A_ROW) { WriteError("VALUE", path, "too many failures, rest skipped"); break; }
        }
        return values.ToString();
    }

    static int ReadValueAt(IntPtr key, string path, int index, StringBuilder values)
    {
        int rc = 0;
        for (int attempt = 0; attempt <= MAX_RETRY; attempt++)
        {
            int nameLength = valueNameBuffer.Length;
            int dataLength = dataBuffer.Length;
            int type;
            rc = RegEnumValueW(key, index, valueNameBuffer, ref nameLength, IntPtr.Zero, out type, dataBuffer, ref dataLength);
            if (rc == ERROR_MORE_DATA && attempt < MAX_RETRY) { GrowValueBuffers(key, dataLength); continue; }
            if (rc == ERROR_NO_MORE_ITEMS) return rc;
            if (rc != 0) break;
            AddValue(path, new string(valueNameBuffer, 0, nameLength), (uint)type, dataLength, values);
            return 0;
        }
        WriteError("VALUE", path + " (value index " + index + ")", "Win32 error " + rc);
        return rc;
    }

    static void AddValue(string path, string name, uint type, int length, StringBuilder values)
    {
        if (MatchValueRule(name))
        {
            excludedWriter.Write("{\"kind\":\"value\",\"key\":" + Vc.Json(path) + ",\"name\":" + Vc.Json(name)
                + ",\"type\":" + type + ",\"size\":" + length + ",\"rule\":\"V1\"}\n");
            counts[EXCLUDED_VALUES]++;
            return;
        }
        if (values.Length > 0) values.Append(',');
        values.Append("{\"name\":").Append(Vc.Json(name)).Append(",\"type\":").Append(type).Append(",\"hex\":\"");
        values.Append(Vc.Hex(dataBuffer, length));
        values.Append("\"}");
        counts[VALUES]++;
    }

    static List<string> ReadSubKeyNames(IntPtr key, string path)
    {
        List<string> names = new List<string>();
        for (int index = 0; ; index++)
        {
            int length = keyNameBuffer.Length;
            int rc = RegEnumKeyExW(key, index, keyNameBuffer, ref length, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (rc == ERROR_MORE_DATA && keyNameBuffer.Length < 65536)
            {
                keyNameBuffer = new char[keyNameBuffer.Length * 2];
                index--;
                continue;
            }
            if (rc == ERROR_NO_MORE_ITEMS) break;
            if (rc != 0) { WriteError("DENIED-KEY", path, "subkey index " + index + ": Win32 error " + rc); break; }
            names.Add(new string(keyNameBuffer, 0, length));
        }
        return names;
    }

    static void WriteKeyLine(string path, string mark, string values)
    {
        outWriter.Write("{\"key\":");
        outWriter.Write(Vc.Json(path));
        if (mark.Length > 0) { outWriter.Write(','); outWriter.Write(mark); }
        outWriter.Write(",\"values\":[");
        outWriter.Write(values);
        outWriter.Write("]}\n");
        counts[KEYS]++;
        if (counts[KEYS] % PROGRESS_KEYS == 0) Console.WriteLine("    " + progressLabel + ": " + counts[KEYS] + " keys");
    }

    static void WriteError(string kind, string path, string message)
    {
        errorWriter.Write(kind + "\t" + path + "\t" + message + "\n");
    }

    // ---------- A16: root names and the closed hives (open status + root SDDL only) ----------
    /// registry/roots.jsonl
    public static void WriteRoots(string outPath)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            foreach (string root in new string[] { "\\Registry", "\\Registry\\Machine", "\\Registry\\User" })
            {
                Vc.Obj o = new Vc.Obj().S("kind", "root").S("key", root);
                List<string> names;
                int st = Nt.EnumKeyNames(root, out names);
                o.St("enum_status", st).StrArr("subkeys", names);
                o.S("sd", Nt.KeySdId(root));
                w.Write(o.ToString()); w.Write('\n');
            }
            foreach (string closed in new string[] { "\\Registry\\Machine\\SAM", "\\Registry\\Machine\\SECURITY", "\\Registry\\Machine\\BCD00000000" })
            {
                int stRead = Nt.OpenKeyStatus(closed, KEY_READ);
                Vc.Obj o = new Vc.Obj().S("kind", "closed").S("key", closed).St("open_key_read", stRead)
                    .St("open_read_control", Nt.OpenKeyStatus(closed, 0x20000)).S("sd", Nt.KeySdId(closed));
                w.Write(o.ToString()); w.Write('\n');
            }
        }
    }
}
