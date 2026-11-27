// VmUi.cs - user-session values: locale, keyboard, fonts, display, GDI, windows, sound, window stations, P6.
// Runs in the session of the logged-on user (both the unelevated and the elevated part call it).
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class Ui
{
    static void Line(StreamWriter w, Vc.Obj o) { w.Write(o.ToString()); w.Write('\n'); }

    // ---------- A10 locale, languages, keyboard ----------
    [DllImport("kernel32.dll")] static extern int GetSystemDefaultLCID();
    [DllImport("kernel32.dll")] static extern int GetUserDefaultLCID();
    [DllImport("kernel32.dll")] static extern int GetThreadLocale();
    [DllImport("kernel32.dll")] static extern ushort GetSystemDefaultUILanguage();
    [DllImport("kernel32.dll")] static extern ushort GetUserDefaultUILanguage();
    [DllImport("kernel32.dll")] static extern ushort GetSystemDefaultLangID();
    [DllImport("kernel32.dll")] static extern ushort GetUserDefaultLangID();
    [DllImport("kernel32.dll")] static extern uint GetACP();
    [DllImport("kernel32.dll")] static extern uint GetOEMCP();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern int GetUserDefaultLocaleName(StringBuilder name, int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern int GetSystemDefaultLocaleName(StringBuilder name, int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern int GetUserDefaultGeoName(StringBuilder name, int size);
    [DllImport("kernel32.dll")] static extern int GetUserGeoID(int geoClass);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetUserPreferredUILanguages(int flags, out int count, [Out] char[] buf, ref int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetSystemPreferredUILanguages(int flags, out int count, [Out] char[] buf, ref int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetProcessPreferredUILanguages(int flags, out int count, [Out] char[] buf, ref int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetThreadPreferredUILanguages(int flags, out int count, [Out] char[] buf, ref int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetFileMUIPath(int flags, string path, StringBuilder lang, ref int langLen, StringBuilder mui, ref int muiLen, ref long enumerator);
    [DllImport("user32.dll")] static extern int GetKeyboardLayoutList(int n, [Out] IntPtr[] list);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool GetKeyboardLayoutNameW(StringBuilder name);
    [DllImport("user32.dll")] static extern IntPtr GetKeyboardLayout(int thread);
    [DllImport("kernel32.dll")] static extern int GetDynamicTimeZoneInformation(byte[] tz);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern int GetSystemDirectoryW(StringBuilder buf, int size);

    delegate bool LangFn(int flags, out int count, char[] buf, ref int size);

    static List<string> Langs(LangFn f, int flags, out bool ok)
    {
        int count; int size = 0;
        f(flags, out count, null, ref size);
        char[] buf = new char[Math.Max(size, 1)];
        ok = f(flags, out count, buf, ref size);
        List<string> list = new List<string>();
        if (!ok) return list;
        int start = 0;
        for (int i = 0; i < size; i++) { if (buf[i] == '\0') { if (i > start) list.Add(new string(buf, start, i - start)); start = i + 1; } }
        return list;
    }

    public static void DumpLocale(string outPath, string label)
    {
        using (StreamWriter w = Vc.Open(outPath, true))
        {
            try
            {
                StringBuilder u = new StringBuilder(128), s = new StringBuilder(128), g = new StringBuilder(128), sys = new StringBuilder(300);
                GetUserDefaultLocaleName(u, 128); GetSystemDefaultLocaleName(s, 128); GetUserDefaultGeoName(g, 128); GetSystemDirectoryW(sys, 300);
                Line(w, new Vc.Obj().S("who", label).S("what", "lcid")
                    .S("GetSystemDefaultLCID", Vc.HexU32((uint)GetSystemDefaultLCID())).S("GetUserDefaultLCID", Vc.HexU32((uint)GetUserDefaultLCID()))
                    .S("GetThreadLocale", Vc.HexU32((uint)GetThreadLocale())).S("GetSystemDefaultUILanguage", Vc.HexU32(GetSystemDefaultUILanguage()))
                    .S("GetUserDefaultUILanguage", Vc.HexU32(GetUserDefaultUILanguage())).S("GetSystemDefaultLangID", Vc.HexU32(GetSystemDefaultLangID()))
                    .S("GetUserDefaultLangID", Vc.HexU32(GetUserDefaultLangID())).N("GetACP", GetACP()).N("GetOEMCP", GetOEMCP())
                    .S("GetUserDefaultLocaleName", u.ToString()).S("GetSystemDefaultLocaleName", s.ToString()).S("GetUserDefaultGeoName", g.ToString())
                    .N("GetUserGeoID_nation", GetUserGeoID(16)).S("GetSystemDirectoryW", sys.ToString()));
                bool ok;
                foreach (int flags in new int[] { 0x8, 0x4 })   // MUI_LANGUAGE_NAME, MUI_LANGUAGE_ID
                {
                    Line(w, new Vc.Obj().S("who", label).S("api", "GetUserPreferredUILanguages").N("flags", flags).StrArr("langs", Langs(GetUserPreferredUILanguages, flags, out ok)).B("ok", ok));
                    Line(w, new Vc.Obj().S("who", label).S("api", "GetSystemPreferredUILanguages").N("flags", flags).StrArr("langs", Langs(GetSystemPreferredUILanguages, flags, out ok)).B("ok", ok));
                    Line(w, new Vc.Obj().S("who", label).S("api", "GetProcessPreferredUILanguages").N("flags", flags).StrArr("langs", Langs(GetProcessPreferredUILanguages, flags, out ok)).B("ok", ok));
                    Line(w, new Vc.Obj().S("who", label).S("api", "GetThreadPreferredUILanguages").N("flags", flags).StrArr("langs", Langs(GetThreadPreferredUILanguages, flags, out ok)).B("ok", ok));
                }
                IntPtr[] layouts = new IntPtr[64];
                int n = GetKeyboardLayoutList(64, layouts);
                List<string> hkl = new List<string>();
                for (int i = 0; i < n; i++) hkl.Add("0x" + layouts[i].ToInt64().ToString("x"));
                StringBuilder kln = new StringBuilder(16);
                GetKeyboardLayoutNameW(kln);
                Line(w, new Vc.Obj().S("who", label).S("what", "keyboard").StrArr("GetKeyboardLayoutList", hkl).S("GetKeyboardLayoutNameW", kln.ToString())
                    .S("GetKeyboardLayout0", "0x" + GetKeyboardLayout(0).ToInt64().ToString("x")));
                byte[] tz = new byte[432];
                int tzr = GetDynamicTimeZoneInformation(tz);
                Line(w, new Vc.Obj().S("who", label).S("api", "GetDynamicTimeZoneInformation").N("ret", tzr).H("hex", tz, tz.Length));
            }
            catch (Exception e) { Vc.Err("LOCALE", label, e.Message); }
        }
    }

    // ---------- A21 display, A11-ish GDI values (sast/150 rows 11 12 16 19) ----------
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool EnumDisplayDevicesW(string device, int index, ref DISPLAY_DEVICE dd, int flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool EnumDisplaySettingsExW(string device, int mode, byte[] devmode, int flags);
    delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool GetMonitorInfoW(IntPtr monitor, byte[] info);
    [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
    [DllImport("shcore.dll")] static extern int GetProcessDpiAwareness(IntPtr process, out int value);
    [DllImport("user32.dll")] static extern uint GetDpiForSystem();
    [DllImport("user32.dll")] static extern IntPtr GetThreadDpiAwarenessContext();
    [DllImport("user32.dll")] static extern int GetAwarenessFromDpiAwarenessContext(IntPtr ctx);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("gdi32.dll")] static extern int GetDeviceCaps(IntPtr hdc, int index);
    [DllImport("gdi32.dll")] static extern IntPtr GetStockObject(int index);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr GetCurrentObject(IntPtr hdc, int type);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern IntPtr CreateFontIndirectW(byte[] logfont);
    [DllImport("gdi32.dll")] static extern bool GetTextMetricsW(IntPtr hdc, byte[] tm);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] static extern int GetTextFaceW(IntPtr hdc, int n, StringBuilder face);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] static extern bool GetTextExtentPoint32W(IntPtr hdc, string s, int n, out long size);
    [DllImport("gdi32.dll")] static extern int GdiGetCharDimensions(IntPtr hdc, IntPtr tm, out int height);
    [DllImport("user32.dll")] static extern bool SystemParametersInfoW(int action, int param, byte[] buf, int winIni);
    [DllImport("user32.dll")] static extern uint GetSysColor(int index);

    public static void DumpDisplay(string outPath, string label)
    {
        using (StreamWriter w = Vc.Open(outPath, true))
        {
            try
            {
                for (int i = 0; i < 16; i++)
                {
                    DISPLAY_DEVICE dd = new DISPLAY_DEVICE(); dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                    if (!EnumDisplayDevicesW(null, i, ref dd, 0)) break;
                    Line(w, new Vc.Obj().S("who", label).S("api", "EnumDisplayDevicesW").S("level", "adapter").N("index", i).S("DeviceName", dd.DeviceName)
                        .S("DeviceString", dd.DeviceString).S("StateFlags", Vc.HexU32((uint)dd.StateFlags)).S("DeviceID", dd.DeviceID).S("DeviceKey", dd.DeviceKey));
                    for (int j = 0; j < 16; j++)
                    {
                        DISPLAY_DEVICE md = new DISPLAY_DEVICE(); md.cb = dd.cb;
                        if (!EnumDisplayDevicesW(dd.DeviceName, j, ref md, 1)) break;   // EDD_GET_DEVICE_INTERFACE_NAME
                        Line(w, new Vc.Obj().S("who", label).S("api", "EnumDisplayDevicesW").S("level", "monitor").S("adapter", dd.DeviceName).N("index", j).S("DeviceName", md.DeviceName)
                            .S("DeviceString", md.DeviceString).S("StateFlags", Vc.HexU32((uint)md.StateFlags)).S("DeviceID", md.DeviceID).S("DeviceKey", md.DeviceKey));
                    }
                    byte[] dm = new byte[220]; BitConverter.GetBytes((ushort)220).CopyTo(dm, 68);
                    bool cur = EnumDisplaySettingsExW(dd.DeviceName, -1, dm, 0);
                    Line(w, new Vc.Obj().S("who", label).S("api", "EnumDisplaySettingsExW").S("adapter", dd.DeviceName).S("mode", "current").B("ok", cur).H("devmode", dm, cur ? dm.Length : 0));
                    List<string> modes = new List<string>();
                    for (int m = 0; m < 4096; m++)
                    {
                        byte[] d2 = new byte[220]; BitConverter.GetBytes((ushort)220).CopyTo(d2, 68);
                        if (!EnumDisplaySettingsExW(dd.DeviceName, m, d2, 0)) break;
                        // DEVMODEW: dmBitsPerPel 168, dmPelsWidth 172, dmPelsHeight 176, dmDisplayFrequency 184
                        modes.Add(BitConverter.ToInt32(d2, 172) + "x" + BitConverter.ToInt32(d2, 176) + "x" + BitConverter.ToInt32(d2, 168) + "@" + BitConverter.ToInt32(d2, 184));
                    }
                    Line(w, new Vc.Obj().S("who", label).S("api", "EnumDisplaySettingsExW").S("adapter", dd.DeviceName).S("mode", "all").StrArr("modes", modes));
                }
                List<IntPtr> monitors = new List<IntPtr>();
                MonitorEnumProc proc = delegate(IntPtr mon, IntPtr hdc, IntPtr rect, IntPtr data) { monitors.Add(mon); return true; };
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
                GC.KeepAlive(proc);
                foreach (IntPtr mon in monitors)
                {
                    byte[] mi = new byte[104]; BitConverter.GetBytes(104).CopyTo(mi, 0);
                    bool ok = GetMonitorInfoW(mon, mi);
                    uint dx = 0, dy = 0; int dpiSt = -1;
                    try { dpiSt = GetDpiForMonitor(mon, 0, out dx, out dy); } catch { }
                    Line(w, new Vc.Obj().S("who", label).S("api", "GetMonitorInfoW").B("ok", ok).H("monitorinfoex", mi, ok ? mi.Length : 0).St("GetDpiForMonitor_status", dpiSt).N("dpi_x", dx).N("dpi_y", dy));
                }
                int aware = -1;
                try { GetProcessDpiAwareness(IntPtr.Zero, out aware); } catch { }
                uint sysDpi = 0; int threadAware = -1;
                try { sysDpi = GetDpiForSystem(); threadAware = GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()); } catch { }
                Line(w, new Vc.Obj().S("who", label).S("what", "dpi").N("GetProcessDpiAwareness", aware).N("GetDpiForSystem", sysDpi).N("thread_awareness", threadAware));
                // system metrics 0..100 and the 0x1000 / 0x2000 groups
                List<string> sm = new List<string>();
                for (int i = 0; i <= 100; i++) sm.Add(i + "=" + GetSystemMetrics(i));
                for (int i = 0x1000; i <= 0x1004; i++) sm.Add("0x" + i.ToString("x") + "=" + GetSystemMetrics(i));
                for (int i = 0x2000; i <= 0x2004; i++) sm.Add("0x" + i.ToString("x") + "=" + GetSystemMetrics(i));
                Line(w, new Vc.Obj().S("who", label).S("api", "GetSystemMetrics").StrArr("values", sm));
                // device caps of the screen DC 0..124
                IntPtr screen = GetDC(IntPtr.Zero);
                List<string> caps = new List<string>();
                for (int i = 0; i <= 124; i++) caps.Add(i + "=" + GetDeviceCaps(screen, i));
                Line(w, new Vc.Obj().S("who", label).S("api", "GetDeviceCaps(GetDC(NULL))").S("hdc", "0x" + screen.ToInt64().ToString("x")).StrArr("values", caps));
                // stock objects, current objects of a new memory DC, system colors
                List<string> stock = new List<string>();
                for (int i = 0; i <= 19; i++) stock.Add(i + "=0x" + GetStockObject(i).ToInt64().ToString("x"));
                IntPtr mem = CreateCompatibleDC(screen);
                Line(w, new Vc.Obj().S("who", label).S("what", "gdi_objects").StrArr("GetStockObject", stock)
                    .S("memdc", "0x" + mem.ToInt64().ToString("x"))
                    .S("memdc_brush", "0x" + GetCurrentObject(mem, 2).ToInt64().ToString("x")).S("memdc_pen", "0x" + GetCurrentObject(mem, 1).ToInt64().ToString("x"))
                    .S("memdc_font", "0x" + GetCurrentObject(mem, 6).ToInt64().ToString("x")).S("memdc_bitmap", "0x" + GetCurrentObject(mem, 7).ToInt64().ToString("x"))
                    .S("memdc_palette", "0x" + GetCurrentObject(mem, 5).ToInt64().ToString("x")));
                DeleteDC(mem);
                List<string> colors = new List<string>();
                for (int i = 0; i <= 30; i++) colors.Add(i + "=0x" + GetSysColor(i).ToString("x6"));
                Line(w, new Vc.Obj().S("who", label).S("api", "GetSysColor").StrArr("values", colors));
                // message box font (NONCLIENTMETRICSW.lfMessageFont at 0x198, 92 B LOGFONTW) and its text metrics
                byte[] ncm = new byte[504]; BitConverter.GetBytes(504).CopyTo(ncm, 0);
                bool nok = SystemParametersInfoW(0x29, 504, ncm, 0);
                Vc.Obj f = new Vc.Obj().S("who", label).S("what", "message_font").B("spi_ok", nok).H("nonclientmetrics", ncm, nok ? 504 : 0);
                if (nok)
                {
                    byte[] lf = new byte[92]; Array.Copy(ncm, 0x198, lf, 0, 92);
                    IntPtr font = CreateFontIndirectW(lf);
                    IntPtr old = SelectObject(screen, font);
                    byte[] tm = new byte[60];
                    bool tok = GetTextMetricsW(screen, tm);
                    StringBuilder face = new StringBuilder(64);
                    GetTextFaceW(screen, 64, face);
                    int cy;
                    int cx = GdiGetCharDimensions(screen, IntPtr.Zero, out cy);
                    f.B("tm_ok", tok).H("textmetricw", tm, tok ? 60 : 0).S("face", face.ToString()).N("GdiGetCharDimensions_cx", cx).N("GdiGetCharDimensions_cy", cy);
                    foreach (string text in new string[] { "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", "OK", "Cancel", "\ud655\uc778", "\ucde8\uc18c", "\uc608", "\uc544\ub2c8\uc624" })
                    {
                        long size;
                        bool eok = GetTextExtentPoint32W(screen, text, text.Length, out size);
                        f.S("extent_" + text, eok ? ((int)(size & 0xffffffff)) + "x" + ((int)(size >> 32)) : "fail");
                    }
                    SelectObject(screen, old);
                    DeleteObject(font);
                }
                Line(w, f);
                ReleaseDC(IntPtr.Zero, screen);
            }
            catch (Exception e) { Vc.Err("DISPLAY", label, e.Message); }
        }
    }

    // ---------- A14 fonts ----------
    delegate int FontEnumProc(IntPtr logfont, IntPtr textmetric, int type, IntPtr data);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] static extern int EnumFontFamiliesExW(IntPtr hdc, byte[] logfont, FontEnumProc proc, IntPtr data, int flags);

    public static void DumpFonts(string outPath, string label)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            try
            {
                IntPtr screen = GetDC(IntPtr.Zero);
                byte[] lf = new byte[92]; lf[23] = 1;   // lfCharSet = DEFAULT_CHARSET, lfFaceName empty = every face and script
                int count = 0;
                FontEnumProc proc = delegate(IntPtr elf, IntPtr tm, int type, IntPtr data)
                {
                    // ENUMLOGFONTEXW: LOGFONTW (92) + elfFullName[64] + elfStyle[32] + elfScript[32]
                    byte[] e = Vc.Copy(elf, 92 + 128 + 64 + 64);
                    byte[] t = Vc.Copy(tm, 60);
                    string face = Encoding.Unicode.GetString(e, 28, 64).TrimEnd('\0');
                    string full = Encoding.Unicode.GetString(e, 92, 128).TrimEnd('\0');
                    string style = Encoding.Unicode.GetString(e, 220, 64).TrimEnd('\0');
                    string script = Encoding.Unicode.GetString(e, 284, 64).TrimEnd('\0');
                    Line(w, new Vc.Obj().S("who", label).S("face", face).S("full", full).S("style", style).S("script", script).N("type", type)
                        .N("charset", e[23]).H("logfont", e, 92).H("textmetric", t, 60));
                    count++;
                    return 1;
                };
                EnumFontFamiliesExW(screen, lf, proc, IntPtr.Zero, 0);
                GC.KeepAlive(proc);
                ReleaseDC(IntPtr.Zero, screen);
                Line(w, new Vc.Obj().S("who", label).S("what", "count").N("count", count));
            }
            catch (Exception e) { Vc.Err("FONTS", label, e.Message); }
        }
    }

    // ---------- A14 sound devices ----------
    [DllImport("winmm.dll")] static extern int waveOutGetNumDevs();
    [DllImport("winmm.dll")] static extern int waveInGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] static extern int waveOutGetDevCapsW(IntPtr id, byte[] caps, int size);
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] static extern int waveInGetDevCapsW(IntPtr id, byte[] caps, int size);

    public static void DumpSound(string outPath, string label)
    {
        using (StreamWriter w = Vc.Open(outPath, true))
        {
            try
            {
                int outs = waveOutGetNumDevs(), ins = waveInGetNumDevs();
                Line(w, new Vc.Obj().S("who", label).N("waveOutGetNumDevs", outs).N("waveInGetNumDevs", ins));
                for (int i = -1; i < outs; i++)
                {
                    byte[] caps = new byte[84];
                    int r = waveOutGetDevCapsW(new IntPtr(i), caps, 84);   // -1 = WAVE_MAPPER
                    Line(w, new Vc.Obj().S("who", label).S("api", "waveOutGetDevCapsW").N("id", i).N("ret", r).S("name", Encoding.Unicode.GetString(caps, 8, 64).TrimEnd('\0')).H("hex", caps, 84));
                }
                for (int i = -1; i < ins; i++)
                {
                    byte[] caps = new byte[80];
                    int r = waveInGetDevCapsW(new IntPtr(i), caps, 80);
                    Line(w, new Vc.Obj().S("who", label).S("api", "waveInGetDevCapsW").N("id", i).N("ret", r).S("name", Encoding.Unicode.GetString(caps, 8, 64).TrimEnd('\0')).H("hex", caps, 80));
                }
            }
            catch (Exception e) { Vc.Err("SOUND", label, e.Message); }
        }
    }

    // ---------- A7 top-level windows, A22 window stations / desktops / power ----------
    delegate bool WndEnumProc(IntPtr hwnd, IntPtr data);
    delegate bool NameEnumProc(IntPtr name, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(WndEnumProc proc, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassNameW(IntPtr hwnd, StringBuilder name, int size);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowTextW(IntPtr hwnd, StringBuilder text, int size);
    [DllImport("user32.dll")] static extern int GetWindowThreadProcessId(IntPtr hwnd, out int pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hwnd, int cmd);
    [DllImport("user32.dll")] static extern IntPtr GetWindowLongPtrW(IntPtr hwnd, int index);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, int[] rect);
    [DllImport("user32.dll")] static extern IntPtr GetProcessWindowStation();
    [DllImport("user32.dll")] static extern IntPtr GetThreadDesktop(int thread);
    [DllImport("kernel32.dll")] static extern int GetCurrentThreadId();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool GetUserObjectInformationW(IntPtr obj, int index, byte[] buf, int len, out int need);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool EnumWindowStationsW(NameEnumProc proc, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool EnumDesktopsW(IntPtr winsta, NameEnumProc proc, IntPtr data);
    [DllImport("kernel32.dll")] static extern bool GetSystemPowerStatus(byte[] status);

    public static void DumpWindows(string outPath, string label)
    {
        using (StreamWriter w = Vc.Open(outPath, false))
        {
            try
            {
                WndEnumProc proc = delegate(IntPtr h, IntPtr d)
                {
                    StringBuilder cls = new StringBuilder(256), text = new StringBuilder(512);
                    GetClassNameW(h, cls, 256); GetWindowTextW(h, text, 512);
                    int pid; int tid = GetWindowThreadProcessId(h, out pid);
                    int[] rect = new int[4]; GetWindowRect(h, rect);
                    Line(w, new Vc.Obj().S("who", label).S("hwnd", "0x" + h.ToInt64().ToString("x")).S("class", cls.ToString()).S("title", text.ToString())
                        .N("pid", pid).N("tid", tid).B("visible", IsWindowVisible(h)).S("owner", "0x" + GetWindow(h, 4).ToInt64().ToString("x"))
                        .S("style", "0x" + GetWindowLongPtrW(h, -16).ToInt64().ToString("x")).S("exstyle", "0x" + GetWindowLongPtrW(h, -20).ToInt64().ToString("x"))
                        .Raw("rect", "[" + rect[0] + "," + rect[1] + "," + rect[2] + "," + rect[3] + "]"));
                    return true;
                };
                EnumWindows(proc, IntPtr.Zero);
                GC.KeepAlive(proc);
            }
            catch (Exception e) { Vc.Err("WINDOWS", label, e.Message); }
        }
    }

    static string ObjName(IntPtr obj, int index)
    {
        byte[] buf = new byte[1024]; int need;
        if (!GetUserObjectInformationW(obj, index, buf, buf.Length, out need)) return "";
        return Encoding.Unicode.GetString(buf, 0, Math.Max(0, need - 2));
    }

    public static void DumpSession(string outPath, string label)
    {
        using (StreamWriter w = Vc.Open(outPath, true))
        {
            try
            {
                IntPtr ws = GetProcessWindowStation(), desk = GetThreadDesktop(GetCurrentThreadId());
                List<string> stations = new List<string>(), desktops = new List<string>();
                NameEnumProc a = delegate(IntPtr n, IntPtr d) { stations.Add(Marshal.PtrToStringUni(n)); return true; };
                NameEnumProc b = delegate(IntPtr n, IntPtr d) { desktops.Add(Marshal.PtrToStringUni(n)); return true; };
                EnumWindowStationsW(a, IntPtr.Zero);
                EnumDesktopsW(ws, b, IntPtr.Zero);
                GC.KeepAlive(a); GC.KeepAlive(b);
                byte[] power = new byte[12];
                bool pok = GetSystemPowerStatus(power);
                Line(w, new Vc.Obj().S("who", label).S("what", "session").S("winsta_name", ObjName(ws, 2)).S("winsta_type", ObjName(ws, 3))
                    .S("desktop_name", ObjName(desk, 2)).S("desktop_type", ObjName(desk, 3)).StrArr("window_stations", stations).StrArr("desktops", desktops)
                    .B("power_ok", pok).H("system_power_status", power, pok ? 12 : 0));
            }
            catch (Exception e) { Vc.Err("SESSION", label, e.Message); }
        }
    }

    // ---------- P6 clipboard, FindWindow, waiting on an empty queue ----------
    [DllImport("user32.dll", SetLastError = true)] static extern bool OpenClipboard(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError = true)] static extern int CountClipboardFormats();
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr GetClipboardOwner();
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr FindWindowW(string cls, string title);
    [DllImport("user32.dll", SetLastError = true)] static extern int MsgWaitForMultipleObjectsEx(int count, IntPtr handles, int ms, int wake, int flags);
    [DllImport("user32.dll")] static extern int GetQueueStatus(int flags);
    [DllImport("kernel32.dll")] static extern void SetLastError(int e);

    public static void ProbeUser(string outPath, string label)
    {
        using (StreamWriter w = Vc.Open(outPath, true))
        {
            try
            {
                SetLastError(0);
                bool o1 = OpenClipboard(IntPtr.Zero); int g1 = Marshal.GetLastWin32Error();
                int formats = CountClipboardFormats(); int g2 = Marshal.GetLastWin32Error();
                IntPtr owner = GetClipboardOwner();
                bool c1 = CloseClipboard(); int g3 = Marshal.GetLastWin32Error();
                SetLastError(0);
                bool c2 = CloseClipboard(); int g4 = Marshal.GetLastWin32Error();
                Line(w, new Vc.Obj().S("who", label).S("probe", "P6").S("what", "clipboard").B("open", o1).N("open_gle", g1).N("formats", formats).N("formats_gle", g2)
                    .B("owner_nonzero", owner != IntPtr.Zero).B("close", c1).N("close_gle", g3).B("close_again", c2).N("close_again_gle", g4));
                SetLastError(0);
                IntPtr tray = FindWindowW("Shell_TrayWnd", null); int g5 = Marshal.GetLastWin32Error();
                SetLastError(0);
                IntPtr none = FindWindowW("NoSuchClass_pe_vmprobe", null); int g6 = Marshal.GetLastWin32Error();
                Line(w, new Vc.Obj().S("who", label).S("probe", "P6").S("what", "FindWindowW").B("tray_found", tray != IntPtr.Zero).N("tray_gle", g5).S("missing", "0x" + none.ToInt64().ToString("x")).N("missing_gle", g6));
                int q0 = GetQueueStatus(0x4ff);
                SetLastError(0);
                int r = MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, 0, 0x4ff, 0); int g7 = Marshal.GetLastWin32Error();
                SetLastError(0);
                int r2 = MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, 0, 0x4ff, 0x4); int g8 = Marshal.GetLastWin32Error();   // MWMO_INPUTAVAILABLE
                Line(w, new Vc.Obj().S("who", label).S("probe", "P6").S("what", "MsgWaitForMultipleObjectsEx(0, NULL, 0, QS_ALLINPUT)").S("queue_status_before", Vc.HexU32((uint)q0))
                    .S("ret", Vc.HexU32((uint)r)).N("gle", g7).S("ret_inputavailable", Vc.HexU32((uint)r2)).N("gle_inputavailable", g8));
            }
            catch (Exception e) { Vc.Err("P6", label, e.Message); }
        }
    }

    // ---------- A6 environment block (raw, in block order) ----------
    [DllImport("kernel32.dll")] static extern IntPtr GetEnvironmentStringsW();
    [DllImport("kernel32.dll")] static extern bool FreeEnvironmentStringsW(IntPtr block);

    public static void DumpEnvironmentBlock(string outPath, string label)
    {
        using (StreamWriter w = Vc.Open(outPath, true))
        {
            try
            {
                IntPtr block = GetEnvironmentStringsW();
                int len = 0;
                while (len < (1 << 22) && !(Marshal.ReadInt16(block, len) == 0 && Marshal.ReadInt16(block, len + 2) == 0)) len += 2;
                len += 4;
                byte[] raw = Vc.Copy(block, len);
                FreeEnvironmentStringsW(block);
                List<string> vars = new List<string>(Encoding.Unicode.GetString(raw).Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries));
                Line(w, new Vc.Obj().S("who", label).S("api", "GetEnvironmentStringsW").N("bytes", len).StrArr("vars", vars).H("hex", raw, raw.Length));
            }
            catch (Exception e) { Vc.Err("ENV", label, e.Message); }
        }
    }
}
