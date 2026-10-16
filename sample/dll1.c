// dll1.dll: TLS 콜백 + DllMain에서 reason별로 로그를 찍는다.
// dll2.dll을 import 한다. 의존 관계 test.exe → dll1.dll → dll2.dll
#include "log.h"

__declspec(dllimport) void Dll2Hello(void);

// ---- DLL의 TLS 콜백 ----
static void NTAPI Dll1Tls(PVOID hModule, DWORD reason, PVOID reserved)
{
    Log("[dll1 TLS    ]", ReasonName(reason));
}

#pragma comment(linker, "/INCLUDE:_tls_used")
#pragma comment(linker, "/INCLUDE:dll1_tls_cb")
#pragma section(".CRT$XLB", read)
__declspec(allocate(".CRT$XLB")) const PIMAGE_TLS_CALLBACK dll1_tls_cb = Dll1Tls;

// ---- DllMain ----
BOOL WINAPI DllMain(HINSTANCE hinst, DWORD reason, LPVOID reserved)
{
    Log("[dll1 DllMain]", ReasonName(reason));
    return TRUE;
}

// exe가 이 함수를 import 하게 만들어서, dll1.dll이 정적으로 로드되도록 한다.
// 안에서 Dll2Hello를 불러서 dll1.dll → dll2.dll import도 실제로 생기게 한다.
__declspec(dllexport) void Dll1Hello(void)
{
    Log("[dll1 export ]", "Dll1Hello");
    Dll2Hello();
}
