// VmCommon.cs - shared helpers for collect_vm.ps1 (C# 5: compiled by Windows PowerShell 5.1 Add-Type).
// No string interpolation, no ?. , no expression-bodied members, no unsafe code.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

public static class Vc
{
    public static string ErrorsPath = null;
    static readonly object errorLock = new object();

    /// One line in errors.log: kind <TAB> path <TAB> message
    public static void Err(string kind, string path, string message)
    {
        if (ErrorsPath == null) return;
        lock (errorLock)
        {
            try { File.AppendAllText(ErrorsPath, kind + "\t" + path + "\t" + (message ?? "").Replace("\r", " ").Replace("\n", " ") + "\n", new UTF8Encoding(false)); }
            catch { }
        }
    }

    public static StreamWriter Open(string path, bool append)
    {
        FileStream stream = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
        return new StreamWriter(stream, new UTF8Encoding(false), 65536);
    }

    /// ASCII-only JSON string: 0x20..0x7E as is, everything else as \uXXXX (same as the old reg-users walker)
    public static string Json(string text)
    {
        if (text == null) return "null";
        StringBuilder sb = new StringBuilder(text.Length + 2);
        sb.Append('"');
        foreach (char c in text)
        {
            if (c == '"') sb.Append("\\\"");
            else if (c == '\\') sb.Append("\\\\");
            else if (c >= ' ' && c <= '~') sb.Append(c);
            else sb.Append("\\u").Append(((int)c).ToString("x4"));
        }
        return sb.Append('"').ToString();
    }

    public static string Hex(byte[] data, int length)
    {
        if (data == null) return "";
        const string digits = "0123456789abcdef";
        if (length > data.Length) length = data.Length;
        if (length < 0) length = 0;
        StringBuilder sb = new StringBuilder(length * 2);
        for (int i = 0; i < length; i++) sb.Append(digits[data[i] >> 4]).Append(digits[data[i] & 15]);
        return sb.ToString();
    }

    public static string Hex(byte[] data) { return data == null ? "" : Hex(data, data.Length); }

    public static string HexU32(uint v) { return "0x" + v.ToString("x8"); }

    public static string Status(int s) { return "0x" + ((uint)s).ToString("x8"); }

    /// FILETIME (100 ns since 1601) as 16 hex digits (I5)
    public static string FileTime(long ft) { return ft.ToString("x16"); }

    public static string Sha256Hex(byte[] data)
    {
        using (SHA256 sha = SHA256.Create()) { return Hex(sha.ComputeHash(data)); }
    }

    public static byte[] Copy(IntPtr p, int length)
    {
        byte[] b = new byte[length];
        if (length > 0 && p != IntPtr.Zero) Marshal.Copy(p, b, 0, length);
        return b;
    }

    // ---------- small JSON object builder ----------
    public class Obj
    {
        StringBuilder sb = new StringBuilder("{");
        bool first = true;
        void Key(string k) { if (!first) sb.Append(','); first = false; sb.Append(Json(k)).Append(':'); }
        public Obj S(string k, string v) { Key(k); sb.Append(Json(v)); return this; }
        public Obj N(string k, long v) { Key(k); sb.Append(v); return this; }
        public Obj U(string k, ulong v) { Key(k); sb.Append(v); return this; }
        public Obj B(string k, bool v) { Key(k); sb.Append(v ? "true" : "false"); return this; }
        public Obj Raw(string k, string json) { Key(k); sb.Append(json); return this; }
        public Obj H(string k, byte[] data, int length) { Key(k); sb.Append('"').Append(Hex(data, length)).Append('"'); return this; }
        public Obj St(string k, int status) { Key(k); sb.Append(Json(Status(status))); return this; }
        public Obj Arr(string k, List<string> jsonItems) { Key(k); sb.Append('[').Append(string.Join(",", jsonItems.ToArray())).Append(']'); return this; }
        public Obj StrArr(string k, IList<string> items)
        {
            List<string> j = new List<string>();
            foreach (string s in items) j.Add(Json(s));
            return Arr(k, j);
        }
        public override string ToString() { return sb.ToString() + "}"; }
    }
}

/// sd/sd_table.jsonl - one line per distinct self-relative security descriptor (id = first 16 hex of SHA-256)
public static class SdTable
{
    const int SDDL_REVISION_1 = 1;
    // OWNER | GROUP | DACL | LABEL (SACL needs SeSecurityPrivilege - not asked)
    public const int SECINFO = 0x1 | 0x2 | 0x4 | 0x10;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool ConvertSecurityDescriptorToStringSecurityDescriptorW(byte[] sd, int revision, int info, out IntPtr text, out int length);
    [DllImport("kernel32.dll")]
    static extern IntPtr LocalFree(IntPtr p);

    static Dictionary<string, bool> seen = new Dictionary<string, bool>();
    static StreamWriter writer;
    public static long Rows = 0;

    public static void OpenTable(string path) { writer = Vc.Open(path, true); }
    public static void CloseTable() { if (writer != null) { writer.Dispose(); writer = null; } }

    /// Returns the id ("" if no data). Writes the row the first time this SD is seen.
    public static string Add(byte[] sd, int length)
    {
        if (sd == null || length <= 0) return "";
        byte[] exact = new byte[length];
        Array.Copy(sd, exact, length);
        string id = Vc.Sha256Hex(exact).Substring(0, 16);
        lock (seen)
        {
            if (seen.ContainsKey(id)) return id;
            seen[id] = true;
            string sddl = "";
            IntPtr text;
            int textLength;
            if (ConvertSecurityDescriptorToStringSecurityDescriptorW(exact, SDDL_REVISION_1, SECINFO, out text, out textLength))
            {
                sddl = Marshal.PtrToStringUni(text);
                LocalFree(text);
            }
            if (writer != null)
            {
                writer.Write(new Vc.Obj().S("id", id).S("sddl", sddl).H("hex", exact, exact.Length).ToString());
                writer.Write('\n');
            }
            Rows++;
        }
        return id;
    }
}
