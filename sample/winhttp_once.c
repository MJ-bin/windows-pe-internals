// winhttp_once.exe: WinHTTP 를 딱 한 번 부르는 시험 프로그램 (VM RPC·네트워크 묶음용).
// 부르는 차례는 PE_emulator 대표 샘플 (r17 009d7da84784 seq 1479 ~ 2083) 과 같게 맞춘다:
//   WinHttpOpen(접근 종류 1) -> WinHttpConnect(443) -> WinHttpOpenRequest(POST)
//   -> WinHttpGetProxyForUrl -> WinHttpGetIEProxyConfigForCurrentUser -> WinHttpSendRequest
// 랜선을 뽑은 VM 에서 돌린다 (Q222 조건). 이름 풀기·연결은 실패해도 된다 — 보려는 것은
// 그 안에서 rpcrt4 가 epmapper·nsi 에 거는 RPC 다.
//
// 멈춤점 걸 틈: 시작하면 PID 를 찍고 Enter 를 기다린다. 디버거에서 이 프로세스 조건으로
// 멈춤점을 건 뒤 Enter. 끝나기 전에도 한 번 더 기다린다 (덤프·상태 사진용).
// printf 는 안 쓴다 (log.h 와 같은 이유 — import 를 줄여 실험을 깨끗하게).
#include <windows.h>
#include <winhttp.h>
#include "log.h"

#pragma comment(lib, "winhttp.lib")

// "이름 = 0x값  gle=0x값" 한 줄 (16진, 8자리)
static void LogHex(const char *who, const char *what, DWORD value, DWORD gle)
{
    char buf[96];
    const char *hex = "0123456789abcdef";
    const char *s;
    int i = 0, k;

    for (s = what; *s && i < 48; s++) buf[i++] = *s;
    for (s = " = 0x"; *s; s++) buf[i++] = *s;
    for (k = 28; k >= 0; k -= 4) buf[i++] = hex[(value >> k) & 0xf];
    for (s = "  gle=0x"; *s; s++) buf[i++] = *s;
    for (k = 28; k >= 0; k -= 4) buf[i++] = hex[(gle >> k) & 0xf];
    buf[i] = 0;
    Log(who, buf);
}

static void WaitEnter(const char *why)
{
    char c[8];
    DWORD n;
    Log("[winhttp_once]", why);
    ReadFile(GetStdHandle(STD_INPUT_HANDLE), c, sizeof(c), &n, NULL);
}

int main(void)
{
    HINTERNET hSession = NULL, hConnect = NULL, hRequest = NULL;
    WINHTTP_AUTOPROXY_OPTIONS ap;
    WINHTTP_PROXY_INFO pi;
    WINHTTP_CURRENT_USER_IE_PROXY_CONFIG ie;
    BOOL ok;

    LogHex("[winhttp_once]", "pid", GetCurrentProcessId(), 0);
    WaitEnter("set breakpoints, then press Enter");

    // 1. WinHttpOpen — 접근 종류 1 (WINHTTP_ACCESS_TYPE_NO_PROXY)
    hSession = WinHttpOpen(L"winhttp_once/1.0", WINHTTP_ACCESS_TYPE_NO_PROXY,
                           WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    LogHex("[winhttp_once]", "WinHttpOpen", (DWORD)(ULONG_PTR)hSession, GetLastError());
    if (!hSession) goto done;

    // 2. WinHttpConnect — 443
    hConnect = WinHttpConnect(hSession, L"example.com", INTERNET_DEFAULT_HTTPS_PORT, 0);
    LogHex("[winhttp_once]", "WinHttpConnect", (DWORD)(ULONG_PTR)hConnect, GetLastError());
    if (!hConnect) goto done;

    // 3. WinHttpOpenRequest — POST
    hRequest = WinHttpOpenRequest(hConnect, L"POST", L"/", NULL, WINHTTP_NO_REFERER,
                                  WINHTTP_DEFAULT_ACCEPT_TYPES, WINHTTP_FLAG_SECURE);
    LogHex("[winhttp_once]", "WinHttpOpenRequest", (DWORD)(ULONG_PTR)hRequest, GetLastError());
    if (!hRequest) goto done;

    // 4. WinHttpGetProxyForUrl — 자동 찾기 (랜선 없으면 실패가 정상)
    ZeroMemory(&ap, sizeof(ap));
    ZeroMemory(&pi, sizeof(pi));
    ap.dwFlags = WINHTTP_AUTOPROXY_AUTO_DETECT;
    ap.dwAutoDetectFlags = WINHTTP_AUTO_DETECT_TYPE_DHCP | WINHTTP_AUTO_DETECT_TYPE_DNS_A;
    ap.fAutoLogonIfChallenged = TRUE;
    ok = WinHttpGetProxyForUrl(hSession, L"https://example.com/", &ap, &pi);
    LogHex("[winhttp_once]", "WinHttpGetProxyForUrl", (DWORD)ok, GetLastError());
    if (pi.lpszProxy) GlobalFree(pi.lpszProxy);
    if (pi.lpszProxyBypass) GlobalFree(pi.lpszProxyBypass);

    // 5. WinHttpGetIEProxyConfigForCurrentUser
    ZeroMemory(&ie, sizeof(ie));
    ok = WinHttpGetIEProxyConfigForCurrentUser(&ie);
    LogHex("[winhttp_once]", "WinHttpGetIEProxyConfigForCurrentUser", (DWORD)ok, GetLastError());
    if (ie.lpszAutoConfigUrl) GlobalFree(ie.lpszAutoConfigUrl);
    if (ie.lpszProxy) GlobalFree(ie.lpszProxy);
    if (ie.lpszProxyBypass) GlobalFree(ie.lpszProxyBypass);

    // 6. WinHttpSendRequest — 본문 없음 (랜선 없으면 FALSE 가 정상, gle 를 본다)
    ok = WinHttpSendRequest(hRequest, WINHTTP_NO_ADDITIONAL_HEADERS, 0,
                            WINHTTP_NO_REQUEST_DATA, 0, 0, 0);
    LogHex("[winhttp_once]", "WinHttpSendRequest", (DWORD)ok, GetLastError());

done:
    WaitEnter("done; press Enter to exit");
    if (hRequest) WinHttpCloseHandle(hRequest);
    if (hConnect) WinHttpCloseHandle(hConnect);
    if (hSession) WinHttpCloseHandle(hSession);
    return 0;
}
