// 로그 한 줄을 디버거(OutputDebugString)와 콘솔(stdout) 양쪽에 찍는다.
// TLS 콜백은 CRT 초기화 전에도 불릴 수 있어서 printf 같은 CRT 함수는 안 쓴다.
// user32(wsprintf)도 안 쓴다. import 목록에 DLL이 늘어나면 실험이 지저분해지니까.
#pragma once
#include <windows.h>

static const char *ReasonName(DWORD reason)
{
    switch (reason) {
    case DLL_PROCESS_ATTACH: return "PROCESS_ATTACH";
    case DLL_THREAD_ATTACH:  return "THREAD_ATTACH";
    case DLL_THREAD_DETACH:  return "THREAD_DETACH";
    case DLL_PROCESS_DETACH: return "PROCESS_DETACH";
    }
    return "?";
}

static void Log(const char *who, const char *what)
{
    char buf[128];
    char num[12];
    int i = 0, k = 0;
    const char *s;
    DWORD tid = GetCurrentThreadId();
    DWORD written;

    for (s = who; *s; s++) buf[i++] = *s;
    buf[i++] = ' ';
    for (s = what; *s; s++) buf[i++] = *s;
    for (s = "  tid="; *s; s++) buf[i++] = *s;
    do { num[k++] = (char)('0' + tid % 10); tid /= 10; } while (tid);
    while (k) buf[i++] = num[--k];
    buf[i++] = '\r';
    buf[i++] = '\n';
    buf[i] = 0;

    OutputDebugStringA(buf);
    WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), buf, (DWORD)i, &written, NULL);
}
