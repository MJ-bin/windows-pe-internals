// vm_probe.exe: VM 수집 묶음 (sample/vm-collect) 의 "디버거 없이" 시료 프로그램.
// 380 handoff 의 디버거 없는 표 20 ~ 25 (run157 · run154) 와 wishlist P5 를 JSON 줄로 stdout 에 찍는다.
//   vm_probe.exe msgbox   : MessageBox 종류 x DEFBUTTON (+ MB_HELP) 에 Enter / Esc / WM_CLOSE 를 넣고 반환값 · 자식 차례 · DM_GETDEFID
//   vm_probe.exe dialog   : 메모리 템플릿 대화상자 (W 판 · A 판) — DLGPROC 메시지 차례 · 자식 차례 · GWLP_USERDATA · GWLP_ID
//   vm_probe.exe debug    : DEBUG_ONLY_THIS_PROCESS 자식 — NtWaitForDebugEvent 0xB8 바이트 · 핸들 접근 · NtDebugContinue 값 ·
//                           Win32 WaitForDebugEvent 차례 · 디버그 객체를 닫을 때 자식이 끝나나
// collect_vm.ps1 -Mode Full 이 세 모드를 차례로 불러 probe/vm_probe_<모드>.jsonl 로 받는다. 키 입력은 프로그램이 스스로 넣는다
// (사람이 누를 것 없음 — 창이 잠깐씩 뜨고 닫힘).
#define _CRT_SECURE_NO_WARNINGS
#include <windows.h>
#include <stdio.h>
#include <string.h>

#pragma comment(lib, "user32.lib")

// ---------- JSON helpers (ASCII only, other chars as \uXXXX) ----------
static void JStr(const wchar_t *s)
{
    putchar('"');
    for (; s && *s; s++) {
        if (*s == '"') fputs("\\\"", stdout);
        else if (*s == '\\') fputs("\\\\", stdout);
        else if (*s >= 0x20 && *s <= 0x7e) putchar((char)*s);
        else printf("\\u%04x", (unsigned)*s);
    }
    putchar('"');
}

static void JHex(const void *p, size_t n)
{
    const unsigned char *b = (const unsigned char *)p;
    size_t i;
    putchar('"');
    for (i = 0; i < n; i++) printf("%02x", b[i]);
    putchar('"');
}

// ---------- ntdll entry points (by GetProcAddress: no ntdll.lib needed) ----------
typedef LONG (NTAPI *PFN_NtWaitForDebugEvent)(HANDLE, BOOLEAN, PLARGE_INTEGER, PVOID);
typedef LONG (NTAPI *PFN_NtDebugContinue)(HANDLE, PVOID, LONG);
typedef HANDLE (NTAPI *PFN_DbgUiGetThreadDebugObject)(void);
typedef void (NTAPI *PFN_DbgUiSetThreadDebugObject)(HANDLE);
typedef LONG (NTAPI *PFN_NtQueryObject)(HANDLE, ULONG, PVOID, ULONG, PULONG);
typedef LONG (NTAPI *PFN_NtQueryInformationProcess)(HANDLE, ULONG, PVOID, ULONG, PULONG);

static PFN_NtWaitForDebugEvent pNtWaitForDebugEvent;
static PFN_NtDebugContinue pNtDebugContinue;
static PFN_DbgUiGetThreadDebugObject pDbgUiGetThreadDebugObject;
static PFN_DbgUiSetThreadDebugObject pDbgUiSetThreadDebugObject;
static PFN_NtQueryObject pNtQueryObject;
static PFN_NtQueryInformationProcess pNtQueryInformationProcess;

static void LoadNt(void)
{
    HMODULE nt = GetModuleHandleW(L"ntdll.dll");
    pNtWaitForDebugEvent = (PFN_NtWaitForDebugEvent)GetProcAddress(nt, "NtWaitForDebugEvent");
    pNtDebugContinue = (PFN_NtDebugContinue)GetProcAddress(nt, "NtDebugContinue");
    pDbgUiGetThreadDebugObject = (PFN_DbgUiGetThreadDebugObject)GetProcAddress(nt, "DbgUiGetThreadDebugObject");
    pDbgUiSetThreadDebugObject = (PFN_DbgUiSetThreadDebugObject)GetProcAddress(nt, "DbgUiSetThreadDebugObject");
    pNtQueryObject = (PFN_NtQueryObject)GetProcAddress(nt, "NtQueryObject");
    pNtQueryInformationProcess = (PFN_NtQueryInformationProcess)GetProcAddress(nt, "NtQueryInformationProcess");
}

// ---------- msgbox ----------
typedef struct { DWORD tid; int key; volatile LONG done; } WATCH;   // key: 0 Enter, 1 Esc, 2 WM_CLOSE

static HWND g_found;
static BOOL CALLBACK FindDlg(HWND h, LPARAM lp)
{
    wchar_t cls[64];
    (void)lp;
    if (GetClassNameW(h, cls, 64) && wcscmp(cls, L"#32770") == 0 && IsWindowVisible(h)) { g_found = h; return FALSE; }
    return TRUE;
}

static HWND WaitDialog(DWORD tid, int ms)
{
    int t;
    for (t = 0; t < ms; t += 20) {
        g_found = NULL;
        EnumThreadWindows(tid, FindDlg, 0);
        if (g_found) return g_found;
        Sleep(20);
    }
    return NULL;
}

static void PrintChildren(HWND dlg)
{
    HWND c;
    int n = 0;
    fputs(",\"children\":[", stdout);
    for (c = GetWindow(dlg, GW_CHILD); c && n < 64; c = GetWindow(c, GW_HWNDNEXT), n++) {
        wchar_t cls[64], text[128];
        GetClassNameW(c, cls, 64);
        GetWindowTextW(c, text, 128);
        if (n) putchar(',');
        printf("{\"id\":%d,\"class\":", GetDlgCtrlID(c));
        JStr(cls);
        fputs(",\"text\":", stdout);
        JStr(text);
        printf(",\"style\":\"0x%08lx\"}", (unsigned long)GetWindowLongW(c, GWL_STYLE));
    }
    putchar(']');
}

static void PressKey(HWND dlg, DWORD tid, int key)
{
    GUITHREADINFO gi;
    HWND target = dlg;
    gi.cbSize = sizeof(gi);
    if (GetGUIThreadInfo(tid, &gi) && gi.hwndFocus) target = gi.hwndFocus;
    if (key == 2) { PostMessageW(dlg, WM_CLOSE, 0, 0); return; }
    PostMessageW(target, WM_KEYDOWN, key == 0 ? VK_RETURN : VK_ESCAPE, 0x00000001);
    PostMessageW(target, WM_KEYUP, key == 0 ? VK_RETURN : VK_ESCAPE, 0xC0000001);
}

static DWORD WINAPI MsgBoxWatcher(LPVOID arg)
{
    WATCH *w = (WATCH *)arg;
    HWND dlg = WaitDialog(w->tid, 5000);
    LRESULT def;
    int tries;
    if (!dlg) { fputs(",\"found\":false", stdout); w->done = 1; return 0; }
    Sleep(150);
    def = SendMessageW(dlg, DM_GETDEFID, 0, 0);
    printf(",\"found\":true,\"dm_getdefid\":\"0x%08lx\"", (unsigned long)def);
    PrintChildren(dlg);
    PressKey(dlg, w->tid, w->key);
    for (tries = 0; tries < 40 && IsWindow(dlg); tries++) Sleep(50);
    printf(",\"closed_by_key\":%s", IsWindow(dlg) ? "false" : "true");
    if (IsWindow(dlg)) {           // e.g. Esc on MB_YESNO: finish with Enter
        PressKey(dlg, w->tid, 0);
        for (tries = 0; tries < 40 && IsWindow(dlg); tries++) Sleep(50);
        printf(",\"closed_by_enter_after\":%s", IsWindow(dlg) ? "false" : "true");
        if (IsWindow(dlg)) PostMessageW(dlg, WM_COMMAND, IDCANCEL, 0);
    }
    w->done = 1;
    return 0;
}

static void OneMsgBox(UINT type, UINT def, BOOL help, int key)
{
    static const char *keys[] = { "enter", "esc", "wm_close" };
    WATCH w;
    HANDLE th;
    int ret;
    DWORD gle;
    UINT style = type | def | (help ? MB_HELP : 0) | MB_SETFOREGROUND | MB_TOPMOST;
    w.tid = GetCurrentThreadId(); w.key = key; w.done = 0;
    printf("{\"probe\":\"msgbox\",\"type\":%u,\"defbutton\":%u,\"help\":%s,\"key\":\"%s\",\"uType\":\"0x%x\"",
           type, def >> 8, help ? "true" : "false", keys[key], style);
    th = CreateThread(NULL, 0, MsgBoxWatcher, &w, 0, NULL);
    SetLastError(0);
    ret = MessageBoxW(NULL, L"pe vm_probe", L"pe_vmprobe", style);
    gle = GetLastError();
    WaitForSingleObject(th, 10000);
    CloseHandle(th);
    printf(",\"ret\":%d,\"gle\":%lu}\n", ret, (unsigned long)gle);
    fflush(stdout);
}

static void ProbeMsgBox(void)
{
    UINT type, def;
    for (type = 0; type <= 6; type++)
        for (def = 0; def <= 3; def++) OneMsgBox(type, def << 8, FALSE, 0);
    for (type = 0; type <= 6; type++) OneMsgBox(type, 0, TRUE, 0);
    for (type = 0; type <= 6; type++) OneMsgBox(type, 0, FALSE, 1);
    for (type = 0; type <= 6; type++) OneMsgBox(type, 0, FALSE, 2);
}

// ---------- dialog ----------
#define MAX_LOG 4000
typedef struct { DWORD tick; UINT msg; WPARAM wp; LPARAM lp; } LOGROW;
static LOGROW g_log[MAX_LOG];
static int g_nlog;
static BOOL g_ansi;

static void Log(UINT m, WPARAM wp, LPARAM lp)
{
    if (g_nlog < MAX_LOG) { g_log[g_nlog].tick = GetTickCount(); g_log[g_nlog].msg = m; g_log[g_nlog].wp = wp; g_log[g_nlog].lp = lp; g_nlog++; }
}

static void InitDialogChecks(HWND dlg)
{
    LONG_PTR oldUser, gotUser, oldId;
    HWND edit = GetDlgItem(dlg, 100);
    printf("{\"probe\":\"dialog\",\"ansi\":%s,\"step\":\"WM_INITDIALOG\",\"IsWindowUnicode\":%s,\"wndproc_W\":\"0x%llx\",\"wndproc_A\":\"0x%llx\"",
           g_ansi ? "true" : "false", IsWindowUnicode(dlg) ? "true" : "false",
           (unsigned long long)GetWindowLongPtrW(dlg, GWLP_WNDPROC), (unsigned long long)GetWindowLongPtrA(dlg, GWLP_WNDPROC));
    PrintChildren(dlg);
    oldUser = SetWindowLongPtrW(dlg, GWLP_USERDATA, 0x1234);
    gotUser = GetWindowLongPtrW(dlg, GWLP_USERDATA);
    printf(",\"userdata_old\":%lld,\"userdata_get\":%lld", (long long)oldUser, (long long)gotUser);
    if (edit) {
        oldId = SetWindowLongPtrW(edit, GWLP_ID, 200);
        printf(",\"gwlp_id_old\":%lld,\"GetDlgCtrlID_after\":%d,\"GetDlgItem_200\":%s", (long long)oldId, GetDlgCtrlID(edit),
               GetDlgItem(dlg, 200) == edit ? "true" : "false");
        SetWindowLongPtrW(edit, GWLP_ID, 100);
    }
    printf(",\"dm_getdefid\":\"0x%08lx\"}\n", (unsigned long)SendMessageW(dlg, DM_GETDEFID, 0, 0));
    fflush(stdout);
}

static INT_PTR CALLBACK DlgProc(HWND dlg, UINT m, WPARAM wp, LPARAM lp)
{
    Log(m, wp, lp);
    if (m == WM_INITDIALOG) { InitDialogChecks(dlg); return TRUE; }
    if (m == WM_COMMAND && (LOWORD(wp) == IDOK || LOWORD(wp) == IDCANCEL)) { EndDialog(dlg, LOWORD(wp)); return TRUE; }
    return FALSE;
}

static WORD *Align4(WORD *p) { return (WORD *)(((ULONG_PTR)p + 3) & ~(ULONG_PTR)3); }

static WORD *AddItem(WORD *p, DWORD style, short x, short y, short cx, short cy, WORD id, WORD atom, const wchar_t *text)
{
    DLGITEMTEMPLATE *it;
    p = Align4(p);
    it = (DLGITEMTEMPLATE *)p;
    it->style = style | WS_CHILD | WS_VISIBLE; it->dwExtendedStyle = 0;
    it->x = x; it->y = y; it->cx = cx; it->cy = cy; it->id = id;
    p = (WORD *)(it + 1);
    *p++ = 0xFFFF; *p++ = atom;
    while (*text) *p++ = *text++;
    *p++ = 0;
    *p++ = 0;   // no creation data
    return p;
}

static void ProbeDialogOnce(BOOL ansi)
{
    static DWORD buf[512];
    DLGTEMPLATE *t = (DLGTEMPLATE *)buf;
    WORD *p;
    const wchar_t *title = L"pe_vmprobe dialog";
    INT_PTR ret;
    int i;
    MSG msg;
    BOOL got;
    memset(buf, 0, sizeof(buf));
    t->style = WS_POPUP | WS_CAPTION | WS_SYSMENU | DS_MODALFRAME | WS_VISIBLE;
    t->cdit = 4; t->x = 10; t->y = 10; t->cx = 180; t->cy = 80;
    p = (WORD *)(t + 1);
    *p++ = 0; *p++ = 0;                                  // no menu, default class
    while (*title) *p++ = *title++;
    *p++ = 0;
    p = AddItem(p, BS_DEFPUSHBUTTON | WS_TABSTOP, 10, 55, 50, 14, IDOK, 0x0080, L"OK");
    p = AddItem(p, BS_PUSHBUTTON | WS_TABSTOP, 70, 55, 50, 14, IDCANCEL, 0x0080, L"Cancel");
    p = AddItem(p, ES_AUTOHSCROLL | WS_BORDER | WS_TABSTOP, 10, 10, 160, 14, 100, 0x0081, L"");
    p = AddItem(p, 0, 10, 30, 160, 10, 101, 0x0082, L"label");
    g_nlog = 0;
    g_ansi = ansi;
    ret = ansi ? DialogBoxIndirectParamA(GetModuleHandleW(NULL), t, NULL, DlgProc, 0)
               : DialogBoxIndirectParamW(GetModuleHandleW(NULL), t, NULL, DlgProc, 0);
    got = PeekMessageW(&msg, NULL, 0, 0, PM_REMOVE);
    printf("{\"probe\":\"dialog\",\"ansi\":%s,\"step\":\"returned\",\"ret\":%lld,\"gle\":%lu,\"peek_after\":%s,\"peek_msg\":\"0x%x\",\"messages\":[",
           ansi ? "true" : "false", (long long)ret, (unsigned long)GetLastError(), got ? "true" : "false", got ? msg.message : 0);
    for (i = 0; i < g_nlog; i++)
        printf("%s{\"t\":%lu,\"msg\":\"0x%04x\",\"wp\":\"0x%llx\",\"lp\":\"0x%llx\"}", i ? "," : "",
               (unsigned long)(g_log[i].tick - g_log[0].tick), g_log[i].msg, (unsigned long long)g_log[i].wp, (unsigned long long)g_log[i].lp);
    puts("]}");
    fflush(stdout);
}

static DWORD WINAPI DialogPresser(LPVOID arg)
{
    DWORD tid = (DWORD)(ULONG_PTR)arg;
    HWND dlg;
    int round;
    for (round = 0; round < 2; round++) {
        int tries;
        dlg = WaitDialog(tid, 8000);
        if (!dlg) return 0;
        Sleep(400);
        PressKey(dlg, tid, 0);
        for (tries = 0; tries < 60 && IsWindow(dlg); tries++) Sleep(50);
    }
    return 0;
}

static void ProbeDialog(void)
{
    HANDLE th = CreateThread(NULL, 0, DialogPresser, (LPVOID)(ULONG_PTR)GetCurrentThreadId(), 0, NULL);
    ProbeDialogOnce(FALSE);
    ProbeDialogOnce(TRUE);
    WaitForSingleObject(th, 20000);
    CloseHandle(th);
}

// ---------- debug ----------
static void Access(const char *name, HANDLE h)
{
    BYTE info[64];
    ULONG ret = 0;
    LONG st;
    if (!h) { printf(",\"%s\":null", name); return; }
    st = pNtQueryObject(h, 0, info, sizeof(info), &ret);   // ObjectBasicInformation: Attributes, GrantedAccess
    printf(",\"%s\":{\"handle\":\"0x%llx\",\"status\":\"0x%08lx\",\"granted\":\"0x%08lx\"}", name, (unsigned long long)(ULONG_PTR)h,
           (unsigned long)st, st >= 0 ? (unsigned long)*(ULONG *)(info + 4) : 0UL);
}

static BOOL StartChild(PROCESS_INFORMATION *pi)
{
    wchar_t cmd[MAX_PATH + 32];
    STARTUPINFOW si;
    GetSystemDirectoryW(cmd, MAX_PATH);
    wcscat(cmd, L"\\cmd.exe /c exit 3");
    memset(&si, 0, sizeof(si)); si.cb = sizeof(si);
    return CreateProcessW(NULL, cmd, NULL, NULL, FALSE, DEBUG_ONLY_THIS_PROCESS | CREATE_NO_WINDOW, NULL, NULL, &si, pi);
}

static void ChildFacts(PROCESS_INFORMATION *pi)
{
    ULONG_PTR port = 0, pbi[6];
    ULONG ret;
    LONG s1 = pNtQueryInformationProcess(pi->hProcess, 7, &port, sizeof(port), &ret);          // ProcessDebugPort
    LONG s2 = pNtQueryInformationProcess(pi->hProcess, 0, pbi, sizeof(pbi), &ret);             // basic: PebBaseAddress at [1]
    BYTE being = 0xff;
    SIZE_T got = 0;
    if (s2 >= 0) ReadProcessMemory(pi->hProcess, (BYTE *)pbi[1] + 2, &being, 1, &got);
    printf(",\"ProcessDebugPort_status\":\"0x%08lx\",\"ProcessDebugPort\":\"0x%llx\",\"peb_BeingDebugged\":%d",
           (unsigned long)s1, (unsigned long long)port, got == 1 ? being : -1);
}

static void ProbeDebugRaw(void)
{
    PROCESS_INFORMATION pi;
    HANDLE dbg;
    int n, bad = 0;
    if (!StartChild(&pi)) { printf("{\"probe\":\"debug_raw\",\"create_gle\":%lu}\n", (unsigned long)GetLastError()); return; }
    dbg = pDbgUiGetThreadDebugObject();
    printf("{\"probe\":\"debug_raw\",\"step\":\"created\",\"pid\":%lu,\"tid\":%lu", (unsigned long)pi.dwProcessId, (unsigned long)pi.dwThreadId);
    Access("debug_object", dbg);
    ChildFacts(&pi);
    puts("}");
    for (n = 0; n < 64; n++) {
        BYTE st[0x100];
        LARGE_INTEGER timeout;
        LONG ws, cs;
        ULONG state;
        memset(st, 0, sizeof(st));
        timeout.QuadPart = -50000000LL;   // 5 s
        ws = pNtWaitForDebugEvent(dbg, FALSE, &timeout, st);
        state = *(ULONG *)st;
        printf("{\"probe\":\"debug_raw\",\"n\":%d,\"wait_status\":\"0x%08lx\",\"NewState\":%lu,\"hex\":", n, (unsigned long)ws, (unsigned long)state);
        JHex(st, 0xB8);
        if (ws != 0) { puts("}"); break; }
        if (state == 2) Access("thread", *(HANDLE *)(st + 0x18));
        if (state == 3) { Access("process", *(HANDLE *)(st + 0x18)); Access("thread", *(HANDLE *)(st + 0x20)); Access("file", *(HANDLE *)(st + 0x30)); }
        if (state == 9) Access("file", *(HANDLE *)(st + 0x18));
        if (!bad) {                                   // once: an invalid continue status
            cs = pNtDebugContinue(dbg, st + 8, 0x12345);
            printf(",\"continue_invalid_0x12345\":\"0x%08lx\"", (unsigned long)cs);
            bad = 1;
        }
        cs = pNtDebugContinue(dbg, st + 8, 0x00010002);   // DBG_CONTINUE
        printf(",\"continue_status\":\"0x%08lx\"}\n", (unsigned long)cs);
        if (state == 2) CloseHandle(*(HANDLE *)(st + 0x18));
        if (state == 3) { CloseHandle(*(HANDLE *)(st + 0x18)); CloseHandle(*(HANDLE *)(st + 0x20)); if (*(HANDLE *)(st + 0x30)) CloseHandle(*(HANDLE *)(st + 0x30)); }
        if (state == 9 && *(HANDLE *)(st + 0x18)) CloseHandle(*(HANDLE *)(st + 0x18));
        if (state == 5) break;
    }
    {
        BYTE st[0x100];
        LARGE_INTEGER zero;
        LONG ws;
        zero.QuadPart = 0;
        ws = pNtWaitForDebugEvent(dbg, FALSE, &zero, st);
        printf("{\"probe\":\"debug_raw\",\"step\":\"wait_after_exit_timeout0\",\"wait_status\":\"0x%08lx\"}\n", (unsigned long)ws);
    }
    WaitForSingleObject(pi.hProcess, 5000);
    CloseHandle(pi.hThread); CloseHandle(pi.hProcess);
    fflush(stdout);
}

static void ProbeDebugWin32(void)
{
    PROCESS_INFORMATION pi;
    DEBUG_EVENT ev;
    int n, bad = 0;
    if (!StartChild(&pi)) { printf("{\"probe\":\"debug_win32\",\"create_gle\":%lu}\n", (unsigned long)GetLastError()); return; }
    for (n = 0; n < 64; n++) {
        if (!WaitForDebugEvent(&ev, 5000)) { printf("{\"probe\":\"debug_win32\",\"n\":%d,\"wait_gle\":%lu}\n", n, (unsigned long)GetLastError()); break; }
        printf("{\"probe\":\"debug_win32\",\"n\":%d,\"code\":%lu,\"pid\":%lu,\"tid\":%lu", n, (unsigned long)ev.dwDebugEventCode,
               (unsigned long)ev.dwProcessId, (unsigned long)ev.dwThreadId);
        if (ev.dwDebugEventCode == CREATE_PROCESS_DEBUG_EVENT) {
            printf(",\"base\":\"0x%llx\",\"start\":\"0x%llx\"", (unsigned long long)(ULONG_PTR)ev.u.CreateProcessInfo.lpBaseOfImage,
                   (unsigned long long)(ULONG_PTR)ev.u.CreateProcessInfo.lpStartAddress);
            Access("hProcess", ev.u.CreateProcessInfo.hProcess); Access("hThread", ev.u.CreateProcessInfo.hThread); Access("hFile", ev.u.CreateProcessInfo.hFile);
            if (ev.u.CreateProcessInfo.hFile) CloseHandle(ev.u.CreateProcessInfo.hFile);
        }
        if (ev.dwDebugEventCode == LOAD_DLL_DEBUG_EVENT) {
            ULONG_PTR namePtr = 0;
            wchar_t name[MAX_PATH] = L"";
            SIZE_T got;
            if (ev.u.LoadDll.lpImageName && ReadProcessMemory(pi.hProcess, ev.u.LoadDll.lpImageName, &namePtr, sizeof(namePtr), &got) && namePtr)
                ReadProcessMemory(pi.hProcess, (LPCVOID)namePtr, name, sizeof(name) - 2, &got);
            printf(",\"base\":\"0x%llx\",\"unicode\":%u,\"name\":", (unsigned long long)(ULONG_PTR)ev.u.LoadDll.lpBaseOfDll, ev.u.LoadDll.fUnicode);
            JStr(ev.u.LoadDll.fUnicode ? name : L"");
            Access("hFile", ev.u.LoadDll.hFile);
            if (ev.u.LoadDll.hFile) CloseHandle(ev.u.LoadDll.hFile);
        }
        if (ev.dwDebugEventCode == EXCEPTION_DEBUG_EVENT)
            printf(",\"exception\":\"0x%08lx\",\"first\":%lu", (unsigned long)ev.u.Exception.ExceptionRecord.ExceptionCode, (unsigned long)ev.u.Exception.dwFirstChance);
        if (!bad) {
            BOOL ok = ContinueDebugEvent(ev.dwProcessId, ev.dwThreadId, 0x12345);
            printf(",\"continue_invalid_ok\":%s,\"continue_invalid_gle\":%lu", ok ? "true" : "false", ok ? 0UL : (unsigned long)GetLastError());
            bad = 1;
        }
        puts("}");
        ContinueDebugEvent(ev.dwProcessId, ev.dwThreadId, DBG_CONTINUE);
        if (ev.dwDebugEventCode == EXIT_PROCESS_DEBUG_EVENT) break;
    }
    WaitForSingleObject(pi.hProcess, 5000);
    CloseHandle(pi.hThread); CloseHandle(pi.hProcess);
    fflush(stdout);
}

static void ProbeDebugClose(BOOL killOnExit)
{
    PROCESS_INFORMATION pi;
    DEBUG_EVENT ev;
    DWORD code = 0, w;
    HANDLE dbg;
    if (!StartChild(&pi)) { printf("{\"probe\":\"debug_close\",\"create_gle\":%lu}\n", (unsigned long)GetLastError()); return; }
    DebugSetProcessKillOnExit(killOnExit);
    if (WaitForDebugEvent(&ev, 5000) && ev.dwDebugEventCode == CREATE_PROCESS_DEBUG_EVENT && ev.u.CreateProcessInfo.hFile)
        CloseHandle(ev.u.CreateProcessInfo.hFile);
    dbg = pDbgUiGetThreadDebugObject();
    CloseHandle(dbg);
    pDbgUiSetThreadDebugObject(NULL);
    w = WaitForSingleObject(pi.hProcess, 3000);
    GetExitCodeProcess(pi.hProcess, &code);
    printf("{\"probe\":\"debug_close\",\"kill_on_exit\":%s,\"wait\":%lu,\"exit_code\":\"0x%08lx\"}\n", killOnExit ? "true" : "false",
           (unsigned long)w, (unsigned long)code);
    if (w != WAIT_OBJECT_0) TerminateProcess(pi.hProcess, 1);
    CloseHandle(pi.hThread); CloseHandle(pi.hProcess);
    fflush(stdout);
}

int wmain(int argc, wchar_t **argv)
{
    LoadNt();
    if (argc < 2) { fputs("usage: vm_probe.exe msgbox|dialog|debug\n", stderr); return 2; }
    if (wcscmp(argv[1], L"msgbox") == 0) ProbeMsgBox();
    else if (wcscmp(argv[1], L"dialog") == 0) ProbeDialog();
    else if (wcscmp(argv[1], L"debug") == 0) { ProbeDebugRaw(); ProbeDebugWin32(); ProbeDebugClose(TRUE); ProbeDebugClose(FALSE); }
    else { fputs("unknown mode\n", stderr); return 2; }
    return 0;
}
