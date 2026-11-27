# 147 wow64-csr — 정적 분석 요청

- 날짜: 2026-11-27
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, WoW64 프로세스 = test.exe 초기화 범위 밖)
- 대기열: **Q287** (새). PE_emulator 요청 원문 `../PE_emulator/_workspace/05_tuning_planner/374_internals-wow64-csr.md` (읽기만) — **원문 "이미 아는 것" · "알고 싶은 것 (셋)" · "답이 오면 쓰는 곳" 을 먼저 다 읽을 것**. 튜닝 근거 `374_tune19.md` X2. 사용자 결정 2026-11-27 (PE_emulator D179): 380 묶음 차례 ① (가장 먼저). **정적만.**
- 먼저 읽는 것: `runs/03_sprint/run68-dll-init-rest.md` (64 비트 `CsrpConnectToServer` syscall 여섯 · PEB `+0x88/+0x98/+0x380` · `Csr*` 전역 9) · `run71-named-obj-dir.md` (공유 섹션 표 · `BaseStaticServerData`) · `run85-user32-init.md` (user32 csrss 연결) · `run138-wow64-dlls.md` · `run139-wow64-kernel.md` · `run140-wow64-ntdll32.md` (32 비트 바닥 · `ntdll32` 프로젝트 · wow64 지도). 대기열 Q261 · Q266 (`whNt*` 본문 대기 — 이번엔 넷만).
- 대상 빌드: 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/`:
  - `SysWOW64/ntdll.dll` — 프로젝트 **`ntdll32`** 있음 (analyze 됨). `CsrClientConnectToServer` · `CsrpConnectToServer` · `CsrpCaptureCsrPortServerInfo`류 등 export.
  - `SysWOW64/KernelBase.dll` — **처음** analyze. 프로젝트 이름은 **`kernelbase32`** (하네스 a5e73e4 의 `<이름>=<경로>` 꼴: `kernelbase32=SysWOW64/KernelBase.dll`. 64 비트 `kernelbase` 프로젝트와 섞지 말 것). PDB 는 `fetch_pdb.py` 로 (32 비트 PDB GUID 따로 확인).
  - `System32/wow64.dll` — 프로젝트 `wow64` 있음. `whNtOpenSection` · `whNtCreateSection` · `whNtConnectPort` / `whNtSecureConnectPort` · `whNtMapViewOfSection` (+ 그 썽크가 부르는 변환 도우미 depth 1) 만.
  - (되면) `System32/csrsrv.dll` (프로젝트 있음) — WoW64 클라이언트를 따로 다루는 줄만.
- 같은 시간에 도는 decompiler: sast/148 (rpcss · combase · ntdll · sechost · advapi32) · sast/149 (rpcrt4) · sast/152 (windows.storage · shell32 · appinfo · kernelbase **64 비트**) · 뒤에 sast/150 (gdi·win32k) · sast/153 (ntoskrnl). **이 요청은 `ntdll32` · `kernelbase32` · `wow64` · `csrsrv` 만 연다.** 새 analyze · export 전에 `free -g` (available 10G 아래면 asm 읽기 먼저 하고 기다림). scratchpad 임시 파일은 `s147/` 하위.

## 알고 싶은 것 (원문 1 ~ 3)

1. **32 비트 `CsrClientConnectToServer` → `CsrpConnectToServer`**: syscall 차례와 인자가 64 비트 여섯 (run68) 과 같은가 — 줄마다 표. `NtConnectPort` / `NtSecureConnectPort` 로 보내는 연결 정보 블록 크기 · 칸 · 응답에서 읽는 칸 (포인터 폭 4 B / 8 B), 쓰는 PEB32 칸 (오프셋) · 32 비트 `Csr*` 전역 (rva).
2. **wow64 썽크 넷**: 포트 뷰 구조체 (클라이언트 · 서버 뷰) 32 ↔ 64 변환 칸 표, 4 GB 아래 요구 (ZeroBits · 주소 고르기 등) 가 있나, 연결 정보 블록을 그대로 넘기나 (크기 바꿈?), 썽크가 status 를 바꾸는 줄.
3. **32 비트 `_KernelBaseBaseDllInitialize` 가 연결 뒤 읽는 것**: `BaseStaticServerData` 를 PEB32 어느 칸 · 어느 인덱스 · 어떤 폭으로 꺼내나, 공유 섹션에서 읽는 칸 가운데 64 비트 표 (run68 · run71) 와 다른 것, **FALSE 로 나가는 가지 전부** (지금 에뮬레이터 `0xc0000142` 의 뿌리 후보 — 원문 "막힌 자리" 의 `NtOpenSection(SharedSection, 4)` 가 `0xc000003a` 일 때 어디로 가나 포함). (되면) csrsrv WoW64 줄.

## 범위

- 줄기 위는 syscall 까지. 공용 부품 (문자열 · 할당) 은 이름만. 분량이 크면 **3 → 1 → 2** 차례로 먼저 쓴다 (3 이 에뮬레이터를 막는 줄).

## 받고 싶은 것

- 결과서 `_workspace/sast/147_wow64-csr_result.md`. asm 은 `ms/disassemble/<모듈>/<함수>.asm`, 근거 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋`.
  1. 맨 위 **"먼저 읽는 한 줄 답"** (1 ~ 3 마다 한 줄).
  2. 표: (1) 32 비트 연결 syscall 차례 표 (64 비트 run68 과 나란히 · 같음/다름) (2) 연결 정보 블록 칸 표 (32 / 64) (3) 썽크 넷 변환 칸 표 (4) `_KernelBaseBaseDllInitialize` 가 읽는 칸 표 + FALSE 가지 표 (5) 쓰는 PEB32 칸 · 32 비트 `Csr*` 전역 표.
  3. PDB GUID 확인 줄 (32 비트 둘 따로).
- **"PE_emulator 쪽 — 지금 맞나·틀리나"**: 원문 "답이 오면 쓰는 곳" (`TeamSyscall.cpp` · `AddressSpace.cpp` `MapCsrSharedSectionView` · `MapCsrPortView` · 팀원 `csr_user_connect_stub.cpp`) 에 쓸 꼴 — WoW64 프로세스에서 누가 무엇을 돌려줘야 하나 (syscall 마다 반환 · 출력 칸 · 뷰 주소 조건) 한 줄씩, "stub 이 채울 최소 목록".
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에:
  - **"VM 으로 확인할 것 — 디버거 필요"** (멈춤점 `모듈!함수+0x…` · 찍을 값. 대기열 Q253d · Q260 · Q262 · Q265 · Q84 에 얹을 꼴)
  - **"VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것"** (레지스트리 키 · 파일 · 명령 출력 — 수집 스크립트 재료. 없으면 "없음")
  - **"다음 요청 후보"**
