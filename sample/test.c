// test.exe: TLS 콜백 + dll1.dll import + CreateThread 1개 + main return
#include "log.h"

// DllHello() 는 dll 에서 import 함
__declspec(dllimport) void Dll1Hello(void);

// ---- exe의 TLS 콜백 ----
static void NTAPI ExeTls(PVOID hModule, DWORD reason, PVOID reserved)
{
    Log("[exe  TLS    ]", ReasonName(reason));
}

#pragma comment(linker, "/INCLUDE:_tls_used")   // 링커에 _tls_used 심볼을 전달
#pragma comment(linker, "/INCLUDE:exe_tls_cb")  // 링커에 함수포인터 exe_tls_cb 전역변수를 전달(최적화로 인한 제거 방지)
#pragma section(".CRT$XLB", read)               // TLS 콜백 함수 포인터를 담는 섹션을 정의
__declspec(allocate(".CRT$XLB")) const PIMAGE_TLS_CALLBACK exe_tls_cb = ExeTls;
// .CRT$XLA~.CRT$XLZ 섹션은 TLS 콜백 함수 포인터를 담는 섹션들이다. 링커는 이 섹션들에 있는 모든 콜백 함수를 TLS 콜백으로 등록한다.

// ---- 새 스레드 ----
static DWORD WINAPI Worker(LPVOID param)
{
    Log("[exe  thread ]", "start");
    Log("[exe  thread ]", "end");
    return 0;
}

int main(void)
{
    HANDLE t;

    Log("[exe  main   ]", "start");
    Dll1Hello();

    t = CreateThread(NULL, 0, Worker, NULL, 0, NULL);
    WaitForSingleObject(t, INFINITE);
    CloseHandle(t);

    Log("[exe  main   ]", "return");
    return 0;
}
