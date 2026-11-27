// VmNt.cs - native queries for collect_vm.ps1 (raw status + raw bytes, no interpretation).
// Every public Dump* writes JSON lines and never throws (errors go to errors.log).
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class Nt
{
    public const int STATUS_SUCCESS = 0;
    public const int STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005);
    public const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);
    public const int STATUS_BUFFER_TOO_SMALL = unchecked((int)0xC0000023);
    public const int OBJ_CASE_INSENSITIVE = 0x40;
    public const int OBJ_OPENIF = 0x80;
    public const int MAX_QUERY = 64 * 1024 * 1024;
    public const int MAX_HEX = 4 * 1024 * 1024;

    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING { public ushort Length; public ushort MaximumLength; public IntPtr Buffer; }

    [StructLayout(LayoutKind.Sequential)]
    public struct OBJECT_ATTRIBUTES
    {
        public int Length; public IntPtr RootDirectory; public IntPtr ObjectName;
        public int Attributes; public IntPtr SecurityDescriptor; public IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IO_STATUS_BLOCK { public IntPtr Status; public IntPtr Information; }

    /// UNICODE_STRING + OBJECT_ATTRIBUTES in unmanaged memory (free with Dispose)
    public class Name : IDisposable
    {
        public IntPtr Us;    // UNICODE_STRING*
        IntPtr text;
        public Name(string s)
        {
            text = Marshal.StringToHGlobalUni(s);
            UNICODE_STRING u = new UNICODE_STRING();
            u.Length = (ushort)(s.Length * 2);
            u.MaximumLength = (ushort)(s.Length * 2 + 2);
            u.Buffer = text;
            Us = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UNICODE_STRING)));
            Marshal.StructureToPtr(u, Us, false);
        }
        public OBJECT_ATTRIBUTES Oa(int attributes, IntPtr root)
        {
            OBJECT_ATTRIBUTES oa = new OBJECT_ATTRIBUTES();
            oa.Length = Marshal.SizeOf(typeof(OBJECT_ATTRIBUTES));
            oa.RootDirectory = root;
            oa.ObjectName = Us;
            oa.Attributes = attributes;
            return oa;
        }
        public void Dispose() { Marshal.FreeHGlobal(Us); Marshal.FreeHGlobal(text); }
    }

    [DllImport("ntdll.dll")] public static extern int NtClose(IntPtr h);
    [DllImport("ntdll.dll")] public static extern int NtOpenKey(out IntPtr h, int access, ref OBJECT_ATTRIBUTES oa);
    [DllImport("ntdll.dll")] public static extern int NtEnumerateKey(IntPtr h, int index, int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQueryKey(IntPtr h, int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQueryValueKey(IntPtr h, IntPtr name, int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQuerySecurityObject(IntPtr h, int info, IntPtr sd, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQuerySystemInformation(int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQuerySystemInformationEx(int cls, byte[] input, int inLen, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQueryInformationProcess(IntPtr h, int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQueryInformationThread(IntPtr h, int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQueryInformationToken(IntPtr h, int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQuerySecurityAttributesToken(IntPtr h, IntPtr names, int count, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtOpenProcessToken(IntPtr process, int access, out IntPtr token);
    [DllImport("ntdll.dll")] public static extern int NtOpenDirectoryObject(out IntPtr h, int access, ref OBJECT_ATTRIBUTES oa);
    [DllImport("ntdll.dll")] public static extern int NtQueryDirectoryObject(IntPtr h, IntPtr buf, int len, byte single, byte restart, ref int context, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtOpenSymbolicLinkObject(out IntPtr h, int access, ref OBJECT_ATTRIBUTES oa);
    [DllImport("ntdll.dll")] public static extern int NtQuerySymbolicLinkObject(IntPtr h, IntPtr target, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtOpenSection(out IntPtr h, int access, ref OBJECT_ATTRIBUTES oa);
    [DllImport("ntdll.dll")] public static extern int NtQuerySection(IntPtr h, int cls, IntPtr buf, IntPtr len, out IntPtr ret);
    [DllImport("ntdll.dll")] public static extern int NtOpenEvent(out IntPtr h, int access, ref OBJECT_ATTRIBUTES oa);
    [DllImport("ntdll.dll")] public static extern int NtQueryEvent(IntPtr h, int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtQueryLicenseValue(IntPtr name, out int type, IntPtr data, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtGetMUIRegistryInfo(int flags, ref int size, IntPtr data);
    [DllImport("ntdll.dll")] public static extern int NtQueryInformationFile(IntPtr h, out IO_STATUS_BLOCK io, IntPtr buf, int len, int cls);
    [DllImport("ntdll.dll")] public static extern int NtQueryVolumeInformationFile(IntPtr h, out IO_STATUS_BLOCK io, IntPtr buf, int len, int cls);
    [DllImport("ntdll.dll")] public static extern int NtQueryObject(IntPtr h, int cls, IntPtr buf, int len, out int ret);
    [DllImport("ntdll.dll")] public static extern int NtCreateSection(out IntPtr h, int access, ref OBJECT_ATTRIBUTES oa, ref long maxSize, int protect, int attributes, IntPtr file);
    [DllImport("ntdll.dll")] public static extern int NtAllocateVirtualMemory(IntPtr process, ref IntPtr baseAddress, IntPtr zeroBits, ref IntPtr size, int type, int protect);
    [DllImport("ntdll.dll")] public static extern int NtFreeVirtualMemory(IntPtr process, ref IntPtr baseAddress, ref IntPtr size, int type);
    [DllImport("ntdll.dll")] public static extern int NtQueryVirtualMemory(IntPtr process, IntPtr baseAddress, int cls, IntPtr buf, IntPtr len, out IntPtr ret);
    [DllImport("ntdll.dll")] public static extern int RtlGetLastNtStatus();

    public static readonly IntPtr CurrentProcess = new IntPtr(-1);
    public static readonly IntPtr CurrentThread = new IntPtr(-2);

    public delegate int QueryFn(IntPtr buf, int len, out int ret);

    /// Calls f with a growing buffer. data = bytes the call reported (min(ret, len)). Returns the last status.
    public static int Query(QueryFn f, int start, int max, out byte[] data, out int ret)
    {
        int len = Math.Max(start, 16);
        data = new byte[0];
        ret = 0;
        int status = 0;
        for (int round = 0; round < 10; round++)
        {
            IntPtr buf = Marshal.AllocHGlobal(len);
            try
            {
                byte[] zero = new byte[len];
                Marshal.Copy(zero, 0, buf, len);
                ret = 0;
                status = f(buf, len, out ret);
                bool small = status == STATUS_INFO_LENGTH_MISMATCH || status == STATUS_BUFFER_TOO_SMALL || status == STATUS_BUFFER_OVERFLOW;
                if (small && len < max)
                {
                    int next = Math.Max(len * 2, ret + 256);
                    if (next > max) next = max;
                    if (next > len) { len = next; continue; }
                }
                int have = (status >= 0 || status == STATUS_BUFFER_OVERFLOW) ? Math.Min(ret > 0 ? ret : len, len) : 0;
                if (status >= 0 && ret == 0) have = 0;
                data = Vc.Copy(buf, have);
                return status;
            }
            catch (Exception e) { Vc.Err("QUERY", "", e.Message); return -1; }
            finally { Marshal.FreeHGlobal(buf); }
        }
        return status;
    }

    static string HexCapped(byte[] d, Vc.Obj o)
    {
        if (d.Length > MAX_HEX) { o.B("truncated", true); return Vc.Hex(d, MAX_HEX); }
        return Vc.Hex(d);
    }

    static void Line(StreamWriter w, Vc.Obj o) { w.Write(o.ToString()); w.Write('\n'); }

    // ---------- keys (used by RegWalker.WriteRoots and probes) ----------
    public static int OpenKey(string path, int access, out IntPtr h)
    {
        using (Name n = new Name(path))
        {
            OBJECT_ATTRIBUTES oa = n.Oa(OBJ_CASE_INSENSITIVE, IntPtr.Zero);
            return NtOpenKey(out h, access, ref oa);
        }
    }

    public static int OpenKeyStatus(string path, int access)
    {
        IntPtr h;
        int st = OpenKey(path, access, out h);
        if (st >= 0) NtClose(h);
        return st;
    }

    public static int EnumKeyNames(string path, out List<string> names)
    {
        names = new List<string>();
        IntPtr h;
        int st = OpenKey(path, 0x20019, out h);
        if (st < 0) return st;
        try
        {
            for (int i = 0; i < 4096; i++)
            {
                byte[] d; int ret;
                int idx = i;
                int s = Query(delegate(IntPtr b, int l, out int r) { return NtEnumerateKey(h, idx, 0, b, l, out r); }, 512, 65536, out d, out ret);
                if (s == unchecked((int)0x8000001A)) break; // STATUS_NO_MORE_ENTRIES
                if (s < 0) return s;
                if (d.Length < 16) break;
                int nameLength = BitConverter.ToInt32(d, 12);
                names.Add(Encoding.Unicode.GetString(d, 16, Math.Min(nameLength, d.Length - 16)));
            }
            return 0;
        }
        finally { NtClose(h); }
    }

    public static string SdIdOf(IntPtr h)
    {
        byte[] d; int ret;
        int st = Query(delegate(IntPtr b, int l, out int r) { return NtQuerySecurityObject(h, SdTable.SECINFO, b, l, out r); }, 1024, 1 << 20, out d, out ret);
        if (st < 0 || d.Length == 0) return "";
        return SdTable.Add(d, d.Length);
    }

    public static string KeySdId(string path)
    {
        IntPtr h;
        if (OpenKey(path, 0x20000, out h) < 0) return "";
        try { return SdIdOf(h); }
        finally { NtClose(h); }
    }

    // ---------- A2 system / process / thread information ----------
    public static void DumpSystemInformation(string outPath)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            for (int cls = 0; cls < 0x100; cls++)
            {
                try
                {
                    byte[] d; int ret;
                    int c = cls;
                    int st = Query(delegate(IntPtr b, int l, out int r) { return NtQuerySystemInformation(c, b, l, out r); }, 256, MAX_QUERY, out d, out ret);
                    Vc.Obj o = new Vc.Obj().S("api", "NtQuerySystemInformation").N("class", cls).St("status", st).N("ret", ret).N("len", d.Length);
                    o.Raw("hex", Vc.Json(HexCapped(d, o)));
                    Line(w, o);
                }
                catch (Exception e) { Vc.Err("SYSINFO", "class " + cls, e.Message); }
            }
            // Ex classes from the wishlist (input guessed: 0 as 2, 4 and 8 bytes; 0x6b with RelationAll)
            int[] exClasses = new int[] { 0x49, 0x6b, 0x73, 0xa4, 0xce, 0xe6 };
            foreach (int cls in exClasses)
            {
                List<byte[]> inputs = new List<byte[]>();
                inputs.Add(new byte[2]); inputs.Add(new byte[4]); inputs.Add(new byte[8]);
                if (cls == 0x6b) inputs.Add(BitConverter.GetBytes(0xffff));
                foreach (byte[] input in inputs)
                {
                    try
                    {
                        byte[] d; int ret;
                        int c = cls; byte[] inp = input;
                        int st = Query(delegate(IntPtr b, int l, out int r) { return NtQuerySystemInformationEx(c, inp, inp.Length, b, l, out r); }, 256, MAX_QUERY, out d, out ret);
                        Vc.Obj o = new Vc.Obj().S("api", "NtQuerySystemInformationEx").N("class", cls).H("input", input, input.Length).St("status", st).N("ret", ret).N("len", d.Length);
                        o.Raw("hex", Vc.Json(HexCapped(d, o)));
                        Line(w, o);
                    }
                    catch (Exception e) { Vc.Err("SYSINFO", "ex class " + cls, e.Message); }
                }
            }
        }
    }

    public static void DumpProcessThreadInformation(string outPath, string label)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            for (int cls = 0; cls < 0x80; cls++)
            {
                try
                {
                    byte[] d; int ret; int c = cls;
                    int st = Query(delegate(IntPtr b, int l, out int r) { return NtQueryInformationProcess(CurrentProcess, c, b, l, out r); }, 64, 1 << 22, out d, out ret);
                    Vc.Obj o = new Vc.Obj().S("who", label).S("api", "NtQueryInformationProcess").N("class", cls).St("status", st).N("ret", ret).H("hex", d, d.Length);
                    Line(w, o);
                }
                catch (Exception e) { Vc.Err("PROCINFO", "class " + cls, e.Message); }
            }
            for (int cls = 0; cls < 0x40; cls++)
            {
                try
                {
                    byte[] d; int ret; int c = cls;
                    int st = Query(delegate(IntPtr b, int l, out int r) { return NtQueryInformationThread(CurrentThread, c, b, l, out r); }, 64, 1 << 20, out d, out ret);
                    Vc.Obj o = new Vc.Obj().S("who", label).S("api", "NtQueryInformationThread").N("class", cls).St("status", st).N("ret", ret).H("hex", d, d.Length);
                    Line(w, o);
                }
                catch (Exception e) { Vc.Err("THREADINFO", "class " + cls, e.Message); }
            }
        }
    }

    /// KUSER_SHARED_DATA page (0x7ffe0000, 0x1000 B)
    public static void DumpKusd(string outPath)
    {
        try
        {
            byte[] page = Vc.Copy(new IntPtr(0x7ffe0000), 0x1000);
            using (StreamWriter w = Vc.Open(outPath, false))
                Line(w, new Vc.Obj().S("what", "KUSER_SHARED_DATA").S("address", "0x7ffe0000").H("hex", page, page.Length));
        }
        catch (Exception e) { Vc.Err("KUSD", "", e.Message); }
    }

    // ---------- A3 tokens ----------
    public static void DumpToken(string outPath, string label, bool alsoLinked)
    {
        IntPtr token;
        int st = NtOpenProcessToken(CurrentProcess, 0x8 | 0x20000, out token); // TOKEN_QUERY | READ_CONTROL
        if (st < 0) { Vc.Err("TOKEN", label, "NtOpenProcessToken " + Vc.Status(st)); return; }
        using (StreamWriter w = Vc.Open(outPath, true))
        {
            DumpOneToken(w, token, label);
            if (alsoLinked)
            {
                byte[] d; int ret;
                IntPtr t = token;
                int ls = Query(delegate(IntPtr b, int l, out int r) { return NtQueryInformationToken(t, 19, b, l, out r); }, 16, 64, out d, out ret);
                if (ls >= 0 && d.Length >= 8)
                {
                    IntPtr linked = new IntPtr(BitConverter.ToInt64(d, 0));
                    DumpOneToken(w, linked, label + "_linked");
                    NtClose(linked);
                }
                else Line(w, new Vc.Obj().S("who", label + "_linked").St("status", ls));
            }
        }
        NtClose(token);
    }

    static void DumpOneToken(StreamWriter w, IntPtr token, string label)
    {
        for (int cls = 1; cls <= 0x34; cls++)
        {
            try
            {
                byte[] d; int ret; int c = cls;
                int st = Query(delegate(IntPtr b, int l, out int r) { return NtQueryInformationToken(token, c, b, l, out r); }, 64, 1 << 20, out d, out ret);
                Line(w, new Vc.Obj().S("who", label).S("api", "NtQueryInformationToken").N("class", cls).St("status", st).N("ret", ret).H("hex", d, d.Length));
            }
            catch (Exception e) { Vc.Err("TOKEN", label + " class " + cls, e.Message); }
        }
        try
        {
            byte[] d; int ret;
            int st = Query(delegate(IntPtr b, int l, out int r) { return NtQuerySecurityAttributesToken(token, IntPtr.Zero, 0, b, l, out r); }, 1024, 1 << 20, out d, out ret);
            Line(w, new Vc.Obj().S("who", label).S("api", "NtQuerySecurityAttributesToken").St("status", st).N("ret", ret).H("hex", d, d.Length));
        }
        catch (Exception e) { Vc.Err("TOKEN", label + " attributes", e.Message); }
        Line(w, new Vc.Obj().S("who", label).S("api", "token_sd").S("sd", SdIdOf(token)));
    }

    // ---------- A1 object name space ----------
    static long objectRows = 0;

    public static void DumpObjects(string outPath)
    {
        objectRows = 0;
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            WalkDirectory(w, "\\", 0);
        }
    }

    static void WalkDirectory(StreamWriter w, string path, int depth)
    {
        if (depth > 24 || objectRows > 400000) return;
        IntPtr dir;
        int st;
        using (Name n = new Name(path))
        {
            OBJECT_ATTRIBUTES oa = n.Oa(OBJ_CASE_INSENSITIVE, IntPtr.Zero);
            st = NtOpenDirectoryObject(out dir, 0x3 | 0x20000, ref oa);
            if (st < 0) st = NtOpenDirectoryObject(out dir, 0x3, ref oa);
        }
        if (st < 0) { Line(w, new Vc.Obj().S("path", path).S("type", "Directory").St("open_status", st)); objectRows++; return; }
        List<string[]> entries = new List<string[]>();
        try
        {
            Line(w, new Vc.Obj().S("path", path).S("type", "Directory").S("sd", SdIdOf(dir)));
            objectRows++;
            int context = 0;
            IntPtr buf = Marshal.AllocHGlobal(8192);
            try
            {
                for (int i = 0; i < 200000; i++)
                {
                    int ret;
                    int qs = NtQueryDirectoryObject(dir, buf, 8192, 1, (byte)(i == 0 ? 1 : 0), ref context, out ret);
                    if (qs < 0 || qs == unchecked((int)0x8000001A) || qs == 0x105 && i > 0 && ret == 0) break;
                    if (qs == unchecked((int)0x8000001A)) break;
                    ushort nameLen = (ushort)Marshal.ReadInt16(buf, 0);
                    IntPtr nameBuf = Marshal.ReadIntPtr(buf, 8);
                    ushort typeLen = (ushort)Marshal.ReadInt16(buf, 16);
                    IntPtr typeBuf = Marshal.ReadIntPtr(buf, 24);
                    if (nameBuf == IntPtr.Zero) break;
                    string name = Marshal.PtrToStringUni(nameBuf, nameLen / 2);
                    string type = typeBuf == IntPtr.Zero ? "" : Marshal.PtrToStringUni(typeBuf, typeLen / 2);
                    entries.Add(new string[] { name, type });
                    if (qs == unchecked((int)0x8000001A)) break;
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        finally { NtClose(dir); }
        foreach (string[] e in entries)
        {
            string child = (path == "\\" ? "\\" : path + "\\") + e[0];
            if (e[1] == "Directory") { WalkDirectory(w, child, depth + 1); continue; }
            Vc.Obj o = new Vc.Obj().S("path", child).S("type", e[1]);
            try { AddObjectDetail(o, child, e[1]); }
            catch (Exception ex) { Vc.Err("OBJECT", child, ex.Message); }
            Line(w, o);
            objectRows++;
        }
    }

    static void AddObjectDetail(Vc.Obj o, string path, string type)
    {
        IntPtr h = IntPtr.Zero;
        int st = -1;
        using (Name n = new Name(path))
        {
            OBJECT_ATTRIBUTES oa = n.Oa(OBJ_CASE_INSENSITIVE, IntPtr.Zero);
            if (type == "SymbolicLink")
            {
                st = NtOpenSymbolicLinkObject(out h, 0x1 | 0x20000, ref oa);
                if (st < 0) st = NtOpenSymbolicLinkObject(out h, 0x1, ref oa);
            }
            else if (type == "Section")
            {
                st = NtOpenSection(out h, 0x1 | 0x20000, ref oa);
                if (st < 0) st = NtOpenSection(out h, 0x1, ref oa);
            }
            else if (type == "Event")
            {
                st = NtOpenEvent(out h, 0x1 | 0x20000, ref oa);
                if (st < 0) st = NtOpenEvent(out h, 0x1, ref oa);
            }
            else return;
        }
        o.St("open_status", st);
        if (st < 0) return;
        try
        {
            if (type == "SymbolicLink")
            {
                IntPtr us = Marshal.AllocHGlobal(16);
                IntPtr text = Marshal.AllocHGlobal(65536);
                try
                {
                    Marshal.WriteInt16(us, 0, 0);
                    Marshal.WriteInt16(us, 2, unchecked((short)65534));
                    Marshal.WriteIntPtr(us, 8, text);
                    int ret;
                    int qs = NtQuerySymbolicLinkObject(h, us, out ret);
                    if (qs >= 0) o.S("target", Marshal.PtrToStringUni(text, (ushort)Marshal.ReadInt16(us, 0) / 2));
                    else o.St("target_status", qs);
                }
                finally { Marshal.FreeHGlobal(us); Marshal.FreeHGlobal(text); }
            }
            else if (type == "Section")
            {
                IntPtr buf = Marshal.AllocHGlobal(64);
                try
                {
                    IntPtr ret;
                    int qs = NtQuerySection(h, 0, buf, new IntPtr(24), out ret);  // SectionBasicInformation: base, attributes, size
                    if (qs >= 0) o.N("section_attributes", Marshal.ReadInt32(buf, 8)).N("section_size", Marshal.ReadInt64(buf, 16));
                    else o.St("query_status", qs);
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
            else if (type == "Event")
            {
                IntPtr buf = Marshal.AllocHGlobal(16);
                try
                {
                    int ret;
                    int qs = NtQueryEvent(h, 0, buf, 8, out ret);  // EventBasicInformation: type, state
                    if (qs >= 0) o.N("event_type", Marshal.ReadInt32(buf, 0)).N("event_state", Marshal.ReadInt32(buf, 4));
                    else o.St("query_status", qs);
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
            o.S("sd", SdIdOf(h));
        }
        finally { NtClose(h); }
    }

    // ---------- A4 license values and MUI registry info ----------
    /// policy = raw ProductPolicy value (HKLM\SYSTEM\CurrentControlSet\Control\ProductOptions)
    public static void DumpLicense(string outPath, byte[] policy)
    {
        List<string> names = new List<string>();
        try
        {
            if (policy != null && policy.Length >= 20)
            {
                int dataSize = BitConverter.ToInt32(policy, 4);
                int end = Math.Min(policy.Length, 20 + Math.Max(0, dataSize));
                int off = 20;
                while (off + 16 <= end)
                {
                    int size = BitConverter.ToUInt16(policy, off);
                    int nameSize = BitConverter.ToUInt16(policy, off + 2);
                    if (size < 16 || off + size > end || 16 + nameSize > size) break;
                    names.Add(Encoding.Unicode.GetString(policy, off + 16, nameSize));
                    off += size;
                }
            }
        }
        catch (Exception e) { Vc.Err("LICENSE", "ProductPolicy", e.Message); }
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            Line(w, new Vc.Obj().S("what", "ProductPolicy names").N("count", names.Count));
            foreach (string name in names)
            {
                try
                {
                    using (Name n = new Name(name))
                    {
                        int type = 0; byte[] d; int ret;
                        int st = Query(delegate(IntPtr b, int l, out int r) { return NtQueryLicenseValue(n.Us, out type, b, l, out r); }, 64, 1 << 20, out d, out ret);
                        Line(w, new Vc.Obj().S("api", "NtQueryLicenseValue").S("name", name).St("status", st).N("type", type).N("ret", ret).H("hex", d, d.Length));
                    }
                }
                catch (Exception e) { Vc.Err("LICENSE", name, e.Message); }
            }
            foreach (string missing in new string[] { "Kernel-MUI-Language-Allowed", "Kernel-MUI-Number-Allowed", "Kernel-ProductInfo", "Security-SPP-GenuineLocalStatus", "NoSuchLicenseValue-pe" })
            {
                if (names.Contains(missing)) continue;
                using (Name n = new Name(missing))
                {
                    int type = 0; byte[] d; int ret;
                    int st = Query(delegate(IntPtr b, int l, out int r) { return NtQueryLicenseValue(n.Us, out type, b, l, out r); }, 64, 1 << 20, out d, out ret);
                    Line(w, new Vc.Obj().S("api", "NtQueryLicenseValue").S("name", missing).B("not_in_policy", true).St("status", st).N("type", type).N("ret", ret).H("hex", d, d.Length));
                }
            }
        }
    }

    public static void DumpMui(string outPath)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            for (int flags = 0; flags < 4; flags++)
            {
                try
                {
                    int size = 0;
                    int st0 = NtGetMUIRegistryInfo(flags, ref size, IntPtr.Zero);
                    Vc.Obj o = new Vc.Obj().S("api", "NtGetMUIRegistryInfo").N("flags", flags).St("size_status", st0).N("size", size);
                    if (size > 0 && size < (16 << 20))
                    {
                        IntPtr buf = Marshal.AllocHGlobal(size);
                        try
                        {
                            int got = size;
                            int st = NtGetMUIRegistryInfo(flags, ref got, buf);
                            o.St("status", st).N("got", got).H("hex", Vc.Copy(buf, Math.Min(got, size)), Math.Min(got, size));
                        }
                        finally { Marshal.FreeHGlobal(buf); }
                    }
                    Line(w, o);
                }
                catch (Exception e) { Vc.Err("MUI", "flags " + flags, e.Message); }
            }
        }
    }

    // ---------- A20 firmware tables ----------
    [DllImport("kernel32.dll", SetLastError = true)] static extern uint EnumSystemFirmwareTables(uint provider, IntPtr buf, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] static extern uint GetSystemFirmwareTable(uint provider, uint id, IntPtr buf, uint size);

    static string FourCC(uint v) { return new string(new char[] { (char)(v & 0xff), (char)((v >> 8) & 0xff), (char)((v >> 16) & 0xff), (char)(v >> 24) }); }

    public static void DumpFirmware(string outPath)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            uint[] providers = new uint[] { 0x52534D42, 0x41435049, 0x4649524D }; // 'RSMB' 'ACPI' 'FIRM'
            string[] names = new string[] { "RSMB", "ACPI", "FIRM" };
            for (int p = 0; p < providers.Length; p++)
            {
                try
                {
                    uint need = EnumSystemFirmwareTables(providers[p], IntPtr.Zero, 0);
                    List<uint> ids = new List<uint>();
                    if (need > 0)
                    {
                        IntPtr buf = Marshal.AllocHGlobal((int)need);
                        try
                        {
                            uint got = EnumSystemFirmwareTables(providers[p], buf, need);
                            for (int i = 0; i + 4 <= got; i += 4) ids.Add((uint)Marshal.ReadInt32(buf, i));
                        }
                        finally { Marshal.FreeHGlobal(buf); }
                    }
                    if (names[p] == "RSMB" && ids.Count == 0) ids.Add(0);
                    foreach (uint id in ids)
                    {
                        uint size = GetSystemFirmwareTable(providers[p], id, IntPtr.Zero, 0);
                        Vc.Obj o = new Vc.Obj().S("provider", names[p]).S("id", names[p] == "RSMB" ? Vc.HexU32(id) : FourCC(id)).N("size", size);
                        if (names[p] == "RSMB" && size > 0)   // SMBIOS: whole table (D189). ACPI / FIRM: names and sizes only.
                        {
                            IntPtr buf = Marshal.AllocHGlobal((int)size);
                            try
                            {
                                uint got = GetSystemFirmwareTable(providers[p], id, buf, size);
                                o.H("hex", Vc.Copy(buf, (int)got), (int)got);
                            }
                            finally { Marshal.FreeHGlobal(buf); }
                        }
                        Line(w, o);
                    }
                }
                catch (Exception e) { Vc.Err("FIRMWARE", names[p], e.Message); }
            }
        }
    }

    // ---------- A12 volumes, mount manager, disks ----------
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateFileW(string name, int access, int share, IntPtr sa, int disposition, int flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr h, int code, byte[] inBuf, int inLen, IntPtr outBuf, int outLen, out int ret, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError = true)] public static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetVolumeInformationW(string root, StringBuilder name, int nameSize, out uint serial, out uint maxComponent, out uint flags, StringBuilder fs, int fsSize);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetVolumeNameForVolumeMountPointW(string mountPoint, StringBuilder name, int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int QueryDosDeviceW(string name, [Out] char[] target, int size);

    static readonly IntPtr INVALID = new IntPtr(-1);

    static void Ioctl(StreamWriter w, string label, IntPtr h, int code, byte[] input)
    {
        int len = 65536;
        IntPtr buf = Marshal.AllocHGlobal(len);
        try
        {
            int ret;
            bool ok = DeviceIoControl(h, code, input, input == null ? 0 : input.Length, buf, len, out ret, IntPtr.Zero);
            int gle = Marshal.GetLastWin32Error();
            int nts = RtlGetLastNtStatus();
            Vc.Obj o = new Vc.Obj().S("handle", label).S("ioctl", Vc.HexU32((uint)code)).B("ok", ok).N("gle", ok ? 0 : gle).St("nt_status", ok ? 0 : nts).N("ret", ret);
            if (input != null) o.H("input", input, input.Length);
            o.H("hex", Vc.Copy(buf, ok ? ret : 0), ok ? ret : 0);
            Line(w, o);
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    public static void DumpVolumes(string outPath, string systemDrive, string sampleFile, string sampleDir)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            try
            {
                StringBuilder name = new StringBuilder(261), fs = new StringBuilder(261);
                uint serial, maxComp, flags;
                bool ok = GetVolumeInformationW(systemDrive + "\\", name, 261, out serial, out maxComp, out flags, fs, 261);
                StringBuilder guid = new StringBuilder(128);
                bool gok = GetVolumeNameForVolumeMountPointW(systemDrive + "\\", guid, 128);
                Line(w, new Vc.Obj().S("api", "GetVolumeInformationW").S("root", systemDrive + "\\").B("ok", ok).S("label", name.ToString())
                    .S("serial", Vc.HexU32(serial)).N("max_component", maxComp).S("flags", Vc.HexU32(flags)).S("fs", fs.ToString()).S("volume_guid_path", gok ? guid.ToString() : ""));
                // QueryDosDevice: every name and its targets
                char[] all = new char[1 << 20];
                int n = QueryDosDeviceW(null, all, all.Length);
                List<string> dosNames = SplitMultiSz(all, n);
                foreach (string dn in dosNames)
                {
                    char[] t = new char[32768];
                    int tn = QueryDosDeviceW(dn, t, t.Length);
                    Line(w, new Vc.Obj().S("api", "QueryDosDeviceW").S("name", dn).StrArr("targets", SplitMultiSz(t, tn)));
                }
                // mount manager
                IntPtr mm = CreateFileW("\\\\.\\MountPointManager", 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
                if (mm != INVALID)
                {
                    try
                    {
                        Ioctl(w, "MountPointManager", mm, 0x6d0008, new byte[24]);          // QUERY_POINTS (all)
                        if (gok)
                        {
                            string nt = "\\??\\" + guid.ToString().Substring(4).TrimEnd('\\');  // \\?\Volume{..}\ -> \??\Volume{..}
                            byte[] nameBytes = Encoding.Unicode.GetBytes(nt);
                            byte[] target = new byte[2 + nameBytes.Length];
                            BitConverter.GetBytes((ushort)nameBytes.Length).CopyTo(target, 0);
                            nameBytes.CopyTo(target, 2);
                            Ioctl(w, "MountPointManager", mm, 0x6d0030, target);            // QUERY_DOS_VOLUME_PATH
                            Ioctl(w, "MountPointManager", mm, 0x6d0034, target);            // QUERY_DOS_VOLUME_PATHS
                        }
                    }
                    finally { CloseHandle(mm); }
                }
                else Vc.Err("VOLUME", "MountPointManager", "open gle " + Marshal.GetLastWin32Error());
                // 0x4d0008 MOUNTDEV_QUERY_DEVICE_NAME on a volume, a file and a folder handle; disk queries
                string[] labels = new string[] { "volume " + systemDrive, "file " + sampleFile, "folder " + sampleDir, "disk PhysicalDrive0" };
                string[] paths = new string[] { "\\\\.\\" + systemDrive, sampleFile, sampleDir, "\\\\.\\PhysicalDrive0" };
                for (int i = 0; i < paths.Length; i++)
                {
                    IntPtr h = CreateFileW(paths[i], i == 1 || i == 2 ? 0x80 : 0, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
                    if (h == INVALID) { Line(w, new Vc.Obj().S("handle", labels[i]).N("open_gle", Marshal.GetLastWin32Error())); continue; }
                    try
                    {
                        Ioctl(w, labels[i], h, 0x4d0008, null);
                        if (i == 0)
                        {
                            Ioctl(w, labels[i], h, 0x560000, null);   // VOLUME_GET_VOLUME_DISK_EXTENTS
                            Ioctl(w, labels[i], h, 0x70048, null);    // DISK_GET_PARTITION_INFO_EX
                            Ioctl(w, labels[i], h, 0x4d000c, null);   // MOUNTDEV_QUERY_UNIQUE_ID
                        }
                        if (i == 3)
                        {
                            Ioctl(w, labels[i], h, 0x700a0, null);    // DISK_GET_DRIVE_GEOMETRY_EX
                            Ioctl(w, labels[i], h, 0x2d1400, new byte[12]);                        // STORAGE_QUERY_PROPERTY device
                            byte[] adapter = new byte[12]; adapter[0] = 1;
                            Ioctl(w, labels[i], h, 0x2d1400, adapter);                             // ... adapter
                        }
                    }
                    finally { CloseHandle(h); }
                }
            }
            catch (Exception e) { Vc.Err("VOLUME", "", e.Message); }
        }
    }

    static List<string> SplitMultiSz(char[] buf, int n)
    {
        List<string> list = new List<string>();
        int start = 0;
        for (int i = 0; i < n; i++)
        {
            if (buf[i] == '\0')
            {
                if (i > start) list.Add(new string(buf, start, i - start));
                start = i + 1;
            }
        }
        return list;
    }

    // ---------- P1 registry query shapes ----------
    public static void ProbeRegistry(string outPath)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            string keyPath = "\\Registry\\Machine\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion";
            IntPtr h;
            int st = OpenKey(keyPath, 0x20019, out h);
            Line(w, new Vc.Obj().S("probe", "P1").S("api", "NtOpenKey").S("name", keyPath).St("status", st));
            if (st >= 0)
            {
                try
                {
                    int[] lengths = new int[] { 0, 4, 16, 24, 64, 4096 };
                    for (int cls = 0; cls <= 9; cls++)
                        foreach (int len in lengths) QueryOnce(w, "NtQueryKey", cls, len, delegate(IntPtr b, int l, out int r) { return NtQueryKey(h, cls, b, l, out r); });
                    using (Name v = new Name("ProductName"))
                    {
                        for (int cls = 0; cls <= 3; cls++)
                            foreach (int len in new int[] { 0, 8, 12, 16, 20, 24, 30, 40, 4096 })
                                QueryOnce(w, "NtQueryValueKey ProductName", cls, len, delegate(IntPtr b, int l, out int r) { return NtQueryValueKey(h, v.Us, cls, b, l, out r); });
                    }
                }
                finally { NtClose(h); }
            }
            string[] forms = new string[] {
                "\\Registry\\Machine\\SOFTWARE", "\\Registry\\Machine\\SOFTWARE\\", "\\REGISTRY\\MACHINE\\software",
                "\\Registry\\\\Machine\\SOFTWARE", "\\Registry\\Machine\\SOFTWARE\\.", "\\Registry\\Machine\\SOFTWARE\\Microsoft\\..",
                "Registry\\Machine\\SOFTWARE", "\\Registry\\Machine\\SOFTWARE\\NoSuchKey_pe" };
            foreach (string f in forms)
            {
                IntPtr k;
                int s1 = OpenKey(f, 0x20019, out k);
                if (s1 >= 0) NtClose(k);
                int s2;
                using (Name n = new Name(f))
                {
                    OBJECT_ATTRIBUTES oa = n.Oa(0, IntPtr.Zero);
                    s2 = NtOpenKey(out k, 0x20019, ref oa);
                    if (s2 >= 0) NtClose(k);
                }
                Line(w, new Vc.Obj().S("probe", "P1").S("api", "NtOpenKey").S("name", f).St("status_case_insensitive", s1).St("status_case_sensitive", s2));
            }
        }
    }

    static void QueryOnce(StreamWriter w, string api, int cls, int len, QueryFn f)
    {
        IntPtr buf = Marshal.AllocHGlobal(Math.Max(len, 8) + 64);
        try
        {
            byte[] fill = new byte[Math.Max(len, 8) + 64];
            for (int i = 0; i < fill.Length; i++) fill[i] = 0xcc;
            Marshal.Copy(fill, 0, buf, fill.Length);
            int ret = -1;
            int st = f(len == 0 ? IntPtr.Zero : buf, len, out ret);
            Line(w, new Vc.Obj().S("api", api).N("class", cls).N("len", len).St("status", st).N("ret", ret).H("hex", Vc.Copy(buf, Math.Min(len + 8, fill.Length)), Math.Min(len + 8, fill.Length)).N("hex_note_len_plus", 8));
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    // ---------- P2 file / volume / object information on four handle kinds ----------
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool CreatePipe(out IntPtr read, out IntPtr write, IntPtr sa, int size);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool GetKernelObjectSecurity(IntPtr h, int info, byte[] sd, int len, out int need);

    public static void ProbeFiles(string outPath, string rootDir, string sampleFile)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            IntPtr pr, pw;
            bool pipeOk = CreatePipe(out pr, out pw, IntPtr.Zero, 0);
            List<string> labels = new List<string>();
            List<IntPtr> handles = new List<IntPtr>();
            labels.Add("dir " + rootDir); handles.Add(CreateFileW(rootDir, unchecked((int)0x80000000) | 0x80 | 0x20000, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero));
            labels.Add("file " + sampleFile); handles.Add(CreateFileW(sampleFile, unchecked((int)0x80000000) | 0x80 | 0x20000, 7, IntPtr.Zero, 3, 0, IntPtr.Zero));
            labels.Add("NUL"); handles.Add(CreateFileW("NUL", unchecked((int)0x80000000), 7, IntPtr.Zero, 3, 0, IntPtr.Zero));
            labels.Add("anonymous pipe (read end)"); handles.Add(pipeOk ? pr : INVALID);
            for (int i = 0; i < handles.Count; i++)
            {
                IntPtr h = handles[i];
                if (h == INVALID) { Line(w, new Vc.Obj().S("probe", "P2").S("handle", labels[i]).N("open_gle", Marshal.GetLastWin32Error())); continue; }
                for (int cls = 1; cls < 0x50; cls++)
                {
                    try
                    {
                        IntPtr buf = Marshal.AllocHGlobal(4096);
                        try
                        {
                            IO_STATUS_BLOCK io;
                            int st = NtQueryInformationFile(h, out io, buf, 4096, cls);
                            int info = (int)io.Information.ToInt64();
                            int show = st >= 0 || st == STATUS_BUFFER_OVERFLOW ? Math.Min(Math.Max(info, 0), 4096) : 0;
                            Line(w, new Vc.Obj().S("probe", "P2").S("handle", labels[i]).S("api", "NtQueryInformationFile").N("class", cls).St("status", st).N("information", info).H("hex", Vc.Copy(buf, show), show));
                        }
                        finally { Marshal.FreeHGlobal(buf); }
                    }
                    catch (Exception e) { Vc.Err("P2", labels[i] + " class " + cls, e.Message); }
                }
                for (int cls = 1; cls <= 0x0d; cls++)
                {
                    IntPtr buf = Marshal.AllocHGlobal(4096);
                    try
                    {
                        IO_STATUS_BLOCK io;
                        int st = NtQueryVolumeInformationFile(h, out io, buf, 4096, cls);
                        int info = (int)io.Information.ToInt64();
                        int show = st >= 0 || st == STATUS_BUFFER_OVERFLOW ? Math.Min(Math.Max(info, 0), 4096) : 0;
                        Line(w, new Vc.Obj().S("probe", "P2").S("handle", labels[i]).S("api", "NtQueryVolumeInformationFile").N("class", cls).St("status", st).N("information", info).H("hex", Vc.Copy(buf, show), show));
                    }
                    finally { Marshal.FreeHGlobal(buf); }
                }
                for (int cls = 0; cls <= 2; cls++)
                {
                    byte[] d; int ret; IntPtr hh = h; int c = cls;
                    int st = Query(delegate(IntPtr b, int l, out int r) { return NtQueryObject(hh, c, b, l, out r); }, 64, 65536, out d, out ret);
                    Line(w, new Vc.Obj().S("probe", "P2").S("handle", labels[i]).S("api", "NtQueryObject").N("class", cls).St("status", st).N("ret", ret).H("hex", d, d.Length));
                }
                int need;
                bool saclOk = GetKernelObjectSecurity(h, 0x8, new byte[4096], 4096, out need);
                Line(w, new Vc.Obj().S("probe", "P2").S("handle", labels[i]).S("api", "GetKernelObjectSecurity SACL").B("ok", saclOk).N("gle", saclOk ? 0 : Marshal.GetLastWin32Error()).St("nt_status", RtlGetLastNtStatus()));
            }
            foreach (IntPtr h in handles) if (h != INVALID) CloseHandle(h);
            if (pipeOk) CloseHandle(pw);
        }
    }

    // ---------- P3 sections and memory ----------
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileMappingW(IntPtr file, IntPtr sa, int protect, int sizeHigh, int sizeLow, string name);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr MapViewOfFile(IntPtr mapping, int access, int offHigh, int offLow, IntPtr size);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool UnmapViewOfFile(IntPtr view);

    [DllImport("kernel32.dll")] static extern int GetCurrentProcessId();

    public static void ProbeSections(string outPath, int sessionId)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            string local = "Local\\pe_vmprobe_" + GetCurrentProcessId();
            IntPtr m1 = CreateFileMappingW(INVALID, IntPtr.Zero, 0x04, 0, 4096, local);
            int g1 = Marshal.GetLastWin32Error();
            IntPtr m2 = CreateFileMappingW(INVALID, IntPtr.Zero, 0x04, 0, 4096, local);
            int g2 = Marshal.GetLastWin32Error();
            Line(w, new Vc.Obj().S("probe", "P3").S("api", "CreateFileMappingW twice").S("name", local).B("first_ok", m1 != IntPtr.Zero).N("first_gle", g1).B("second_ok", m2 != IntPtr.Zero).N("second_gle", g2));
            string ntName = "\\Sessions\\" + sessionId + "\\BaseNamedObjects\\pe_vmprobe_nt_" + GetCurrentProcessId();
            using (Name n = new Name(ntName))
            {
                long size = 4096;
                IntPtr s1, s2, s3;
                OBJECT_ATTRIBUTES oa = n.Oa(OBJ_CASE_INSENSITIVE, IntPtr.Zero);
                int st1 = NtCreateSection(out s1, 0xF001F, ref oa, ref size, 0x04, 0x8000000, IntPtr.Zero);
                size = 4096;
                int st2 = NtCreateSection(out s2, 0xF001F, ref oa, ref size, 0x04, 0x8000000, IntPtr.Zero);
                OBJECT_ATTRIBUTES oaIf = n.Oa(OBJ_CASE_INSENSITIVE | OBJ_OPENIF, IntPtr.Zero);
                size = 4096;
                int st3 = NtCreateSection(out s3, 0xF001F, ref oaIf, ref size, 0x04, 0x8000000, IntPtr.Zero);
                Line(w, new Vc.Obj().S("probe", "P3").S("api", "NtCreateSection same name").S("name", ntName).St("first", st1).St("second", st2).St("second_openif", st3));
                if (st1 >= 0) NtClose(s1);
                if (st2 >= 0) NtClose(s2);
                if (st3 >= 0) NtClose(s3);
            }
            IntPtr baseAddr = IntPtr.Zero, sz = new IntPtr(0x2000);
            int a1 = NtAllocateVirtualMemory(CurrentProcess, ref baseAddr, IntPtr.Zero, ref sz, 0x3000, 0x04);
            IntPtr b2 = baseAddr, sz2 = new IntPtr(0x1000);
            int a2 = NtAllocateVirtualMemory(CurrentProcess, ref b2, IntPtr.Zero, ref sz2, 0x1000, 0x02);
            IntPtr mbi = Marshal.AllocHGlobal(48);
            IntPtr ret;
            int q = NtQueryVirtualMemory(CurrentProcess, baseAddr, 0, mbi, new IntPtr(48), out ret);
            Line(w, new Vc.Obj().S("probe", "P3").S("api", "commit again with PAGE_READONLY").St("reserve_commit", a1).St("commit_again", a2).St("query", q)
                .N("protect_after", q >= 0 ? Marshal.ReadInt32(mbi, 36) : -1).N("allocation_protect", q >= 0 ? Marshal.ReadInt32(mbi, 16) : -1));
            Marshal.FreeHGlobal(mbi);
            IntPtr fz = IntPtr.Zero;
            NtFreeVirtualMemory(CurrentProcess, ref baseAddr, ref fz, 0x8000);
            if (m1 != IntPtr.Zero)
            {
                IntPtr view = MapViewOfFile(m1, 0x4, 0, 0, new IntPtr(4096)); // FILE_MAP_READ
                if (view != IntPtr.Zero)
                {
                    IntPtr vb = view, vs = new IntPtr(4096);
                    int c = NtAllocateVirtualMemory(CurrentProcess, ref vb, IntPtr.Zero, ref vs, 0x1000, 0x04);
                    Line(w, new Vc.Obj().S("probe", "P3").S("api", "MEM_COMMIT RW on a read-only view").St("status", c));
                    UnmapViewOfFile(view);
                }
                CloseHandle(m1);
            }
            if (m2 != IntPtr.Zero) CloseHandle(m2);
        }
    }

    // ---------- P4 anonymous pipe status sequence ----------
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteFile(IntPtr h, byte[] data, int len, out int written, IntPtr ov);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadFile(IntPtr h, byte[] data, int len, out int read, IntPtr ov);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool PeekNamedPipe(IntPtr h, IntPtr buf, int len, out int read, out int avail, out int left);

    public static void ProbePipe(string outPath)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            IntPtr r, wr;
            bool ok = CreatePipe(out r, out wr, IntPtr.Zero, 0);
            Line(w, new Vc.Obj().S("probe", "P4").S("step", "CreatePipe").B("ok", ok).N("gle", ok ? 0 : Marshal.GetLastWin32Error()));
            if (!ok) return;
            int n;
            bool b = WriteFile(wr, Encoding.ASCII.GetBytes("hello"), 5, out n, IntPtr.Zero);
            Line(w, new Vc.Obj().S("probe", "P4").S("step", "WriteFile 5").B("ok", b).N("bytes", n).N("gle", b ? 0 : Marshal.GetLastWin32Error()).St("nt_status", b ? 0 : RtlGetLastNtStatus()));
            byte[] buf = new byte[16];
            b = ReadFile(r, buf, 5, out n, IntPtr.Zero);
            Line(w, new Vc.Obj().S("probe", "P4").S("step", "ReadFile 5").B("ok", b).N("bytes", n).N("gle", b ? 0 : Marshal.GetLastWin32Error()).St("nt_status", b ? 0 : RtlGetLastNtStatus()));
            int avail, left;
            b = PeekNamedPipe(r, IntPtr.Zero, 0, out n, out avail, out left);
            Line(w, new Vc.Obj().S("probe", "P4").S("step", "PeekNamedPipe (open, empty)").B("ok", b).N("avail", avail).N("gle", b ? 0 : Marshal.GetLastWin32Error()));
            CloseHandle(wr);
            Line(w, new Vc.Obj().S("probe", "P4").S("step", "CloseHandle(write end)"));
            b = ReadFile(r, buf, 5, out n, IntPtr.Zero);
            Line(w, new Vc.Obj().S("probe", "P4").S("step", "ReadFile after writer closed").B("ok", b).N("bytes", n).N("gle", b ? 0 : Marshal.GetLastWin32Error()).St("nt_status", b ? 0 : RtlGetLastNtStatus()));
            b = PeekNamedPipe(r, IntPtr.Zero, 0, out n, out avail, out left);
            Line(w, new Vc.Obj().S("probe", "P4").S("step", "PeekNamedPipe after writer closed").B("ok", b).N("gle", b ? 0 : Marshal.GetLastWin32Error()).St("nt_status", b ? 0 : RtlGetLastNtStatus()));
            CloseHandle(r);
        }
    }

    // ---------- api sets and mitigation policies ----------
    [DllImport("ntdll.dll")] static extern int ApiSetQueryApiSetPresence(IntPtr name, out byte present);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetProcessMitigationPolicy(IntPtr process, int policy, byte[] buf, int size);

    public static void DumpApiSetsAndMitigation(string outPath, string label, string[] apiSets)
    {
        using (StreamWriter w = Vc.Open(outPath, true))
        {
            foreach (string s in apiSets)
            {
                try
                {
                    using (Name n = new Name(s))
                    {
                        byte present;
                        int st = ApiSetQueryApiSetPresence(n.Us, out present);
                        Line(w, new Vc.Obj().S("who", label).S("api", "ApiSetQueryApiSetPresence").S("name", s).St("status", st).N("present", present));
                    }
                }
                catch (Exception e) { Vc.Err("APISET", s, e.Message); }
            }
            for (int policy = 0; policy <= 20; policy++)
            {
                bool ok = false; int size = 0; int gle = 0; byte[] buf = null;
                foreach (int s in new int[] { 4, 8, 16, 32 })
                {
                    buf = new byte[s];
                    ok = GetProcessMitigationPolicy(CurrentProcess, policy, buf, s);
                    gle = ok ? 0 : Marshal.GetLastWin32Error();
                    if (ok) { size = s; break; }
                }
                Line(w, new Vc.Obj().S("who", label).S("api", "GetProcessMitigationPolicy").N("policy", policy).B("ok", ok).N("size", size).N("gle", gle).H("hex", buf, ok ? size : 0));
            }
        }
    }
}
