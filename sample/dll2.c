// dll2.dll: dll1.dll이 import 하는 DLL. 의존 관계 test.exe → dll1.dll → dll2.dll
#include "log.h"

// ---- DLL의 TLS 콜백 ----
static void NTAPI Dll2Tls(PVOID hModule, DWORD reason, PVOID reserved)
{
    Log("[dll2 TLS    ]", ReasonName(reason));
}

#pragma comment(linker, "/INCLUDE:_tls_used")
#pragma comment(linker, "/INCLUDE:dll2_tls_cb")
#pragma section(".CRT$XLB", read)
__declspec(allocate(".CRT$XLB")) const PIMAGE_TLS_CALLBACK dll2_tls_cb = Dll2Tls;

// ---- DllMain ----
BOOL WINAPI DllMain(HINSTANCE hinst, DWORD reason, LPVOID reserved)
{
    Log("[dll2 DllMain]", ReasonName(reason));
    return TRUE;
}

__declspec(dllexport) void Dll2Hello(void)
{
    Log("[dll2 export ]", "Dll2Hello");
}
