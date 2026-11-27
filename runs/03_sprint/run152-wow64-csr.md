# 2026-11-27 (작성일, 정적 분석 2026-11-27) run152 — WoW64 (x86) 프로세스의 32비트 csrss 첫 연결 (Q287, 정적만): **32비트 ntdll 에는 `CsrpConnectToServer` 가 없다.** 32비트 `CsrClientConnectToServer` (62 B) 는 syscall **하나** `NtWow64CsrClientConnectToServer` (칸 `0x1e9`) 만 내고, 그 몸 wow64 `whNtWow64CsrClientConnectToServer +0x1c` 가 **64비트 ntdll 의 `CsrClientConnectToServer` 를 import 로 직접 부른다** → run68 의 syscall 여섯이 **같은 순서 · 같은 인자** 로 64비트 ntdll 에서 나온다 (wow64 썽크 `whNtOpenSection` 류는 **안 낀다**). 연결 정보 블록은 64비트 꼴 `0x30` B, 32비트가 넘기는 건 `{CtrlRoutine32, 0}` 8 B. 연결 뒤 wow64 가 **상태를 안 보고** PEB32 `+0x4c` · `+0x54` (4 B) · `+0x248` (8 B) 에 PEB64 값을 옮긴다. 32비트 `__KernelBaseBaseDllInitialize@12` 는 `BaseStaticServerData = [[PEB32+0x54]+8] − [PEB32+0x248] + [PEB32+0x4c]` (32비트 셈) — BSSD 칸은 64비트와 같은 오프셋, 폭만 반. FALSE 가지는 안쪽 다섯 + `entry` 둘 + `PcwDllMain` 하나. 원문 막힘 `NtOpenSection(SharedSection) = 0xc000003a` 는 **F2 (`+0x20e JS`)** → `entry` 0 → ntdll32 `_LdrpInitializeNode@4 +0x247` 이 `0xc0000142` → `NtRaiseHardError(0xc0000145)`

> **보탬 · 표기 (run153 때 대조)**: ① F2 표 "그 밖" 에 **`0xc000000d` (사용자 정보 블록은 있는데 크기 0)** 이 빠졌었다 — 64비트 ntdll `CsrClientConnectToServer +0x2f TEST R8 · +0x32 JZ · +0x34 TEST R9D · +0x37 JZ → +0x1d5 MOV EAX,0xc000000d` (`+0x1da JMP → +0xca` 로 연결 없이 돌아감) `[정적]`, 결과서 147 3-3 과 같음. kernelbase32 는 크기 8 을 주므로 이 길은 안 탄다. ② "쓰는 칸" 표의 wow64 줄 범위 (`+0x57 ~ +0x63` · `+0x6f ~ +0x7b` · `+0x87 ~ +0x8e`) 는 **PEB64 칸 읽기 → PEB32 쓰기** 두 줄이고, 결과서 1-3 의 `+0x4e ~` · `+0x66 ~` · `+0x7e ~` 는 그 앞 **`MOV RAX,GS:[0x60]` (PEB64 주소 얻기)** 부터 센 것 — 쓰는 자리 (`+0x63` · `+0x7b` · `+0x8e`) 는 같고 어느 쪽도 틀리지 않음. 본문 두 곳에 한 마디씩 보탰다 → [run153](run153-gdi-font-dc.md)

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 점프 목적지도 **함수 시작 기준 `+0x…`** 로만 적었다 — 주소 끝자리와 안 섞음. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** (원문 `../PE_emulator/_workspace/05_tuning_planner/374_internals-wow64-csr.md`, 튜닝 `374_tune19.md` X2 · x86 34 / 34 가 같은 자리에서 끝남 — 요청 쪽 에뮬레이터 숫자) 이고 **WoW64 (x86) 프로세스** 길이다. test.exe 는 x64 라 이 길을 안 탄다 — EPROCESS `WoW64Process : (null)` `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:160` · `_P2.txt:160`, run106 이 맞춘 줄), 64비트 ntdll `UseWOW64` 0 `[관찰]` (flow.md 2.4 run44). 그래서 32비트 kernelbase · ntdll32 가 도는 순간이 없고, test.exe 의 csrss 연결은 run68 의 64비트 길 그대로다. 그림 1 · 2 에 WoW64 노드가 없어 **그림은 안 바꿨다.** flow.md 는 기준 (wkernelbase 줄 새로 · wntdll · wow64 · csrsrv 줄 한 마디 · 근거 줄) · 3장 run140 절 뒤 새 절 · run138 절 한 줄 · 4장 (Q287 해결됨 줄 하나 · run152 줄 셋) · 5장 (run152 두 줄) 만 고쳤다. 짐 지도 (`baggage.md`) 는 셈을 안 바꾸고 **7.6.17 에 메모 두 줄** 만 보탰다.

> **대기열**: **Q287** (새, 380 묶음 정적 차례 ①). 사용자 결정 2026-11-27 (PE_emulator D179). 결과서 판정 **분석성공** — 물음 3 · 1 · 2 를 `.asm` 줄로 닫음. csrsrv 의 WoW64 줄은 "없음" (읽은 세 함수 안).
> - 이어 쓰는 앞 run: **run68** (64비트 `CsrpConnectToServer` syscall 여섯 · PEB `+0x88` · `+0x98` · `+0x380` · `Csr*` 전역 9 — 이번 길의 **아래층 그대로**), **run71** (공유 섹션 표 · `BaseStaticServerData` 칸 · C ≠ R), **run138** (wow64 `Wow64SystemServiceEx` · 서비스 표 — 이번에 **한 줄 좁힘**, 아래 "정정"), **run139** (PEB32 27칸 · 4GB 상한 `HighestUserAddress`), **run140** (ntdll32 프로젝트 · `Nt*` 스텁 꼴 · `_LdrpInitializeNode`).
> - 남은 것 → 동적은 새 번호 없이 **Q84 · Q253d · Q262** 에 얹을 꼴 (결과서 "VM — 디버거 필요"), 디버거 없이 떠 올 것은 **없음**, 정적은 결과서 "다음 요청 후보" 1 ~ 6 (32비트 NLS · 콘솔 · `whNtQueryInformationProcess` · 32비트 kernel32 · 32비트 user32 연결 · 이름 글) — 대기열 자리는 오케스트레이터가 정함.

## 요약

- **길 (한눈에)** `[정적]`: 32비트 kernelbase `__KernelBaseBaseDllInitialize@12 +0x206` → ntdll32 `CsrClientConnectToServer +0x16` → ntdll32 `NtWow64CsrClientConnectToServer` (`MOV EAX,0x1e9` · `_Wow64SystemServiceCall`) ⇒ (wow64cpu → `Wow64SystemServiceEx` → nt 표 칸 `0x1e9`) → wow64 `whNtWow64CsrClientConnectToServer +0x1c` → **64비트** ntdll `CsrClientConnectToServer +0x113` → `CsrpConnectToServer` (run68 의 여섯 syscall).
- **"wow64 가 64비트 syscall 로 바꾼다" 는 틀린 그림이다.** wow64 는 syscall 을 하나씩 번역하지 않고, **64비트 ntdll 의 연결 함수 한 덩어리를 통째로 불러** 그 함수가 syscall 여섯을 낸다. 그래서 `whNtOpenSection` · `whNtCreateSection` · `whNtConnectPort` · `whNtMapViewOfSection` 은 **이 길에서 안 불린다** `[정적]`.
- **연결 정보 블록** `[정적]`: 64비트 ntdll 이 짓는 `0x30` B (포인터 칸 8 B) 그대로. 32비트가 건네는 건 "사용자 정보" 8 B `{CtrlRoutine32 (`0x102408b0`), 0}` 하나 — csrss basesrv 가 이 8 B 를 qword 로 받으니 0 확장 32비트 주소가 된다.
- **PEB32 세 칸은 wow64 가 쓴다** `[정적]`: `whNtWow64CsrClientConnectToServer +0x63` `PEB32+0x4c ← PEB64+0x88` (하위 4 B) · `+0x7b` `PEB32+0x54 ← PEB64+0x98` (하위 4 B) · `+0x8e` `PEB32+0x248 ← PEB64+0x380` (8 B 통째). **연결 상태를 안 보고** 쓴다 (실패면 0 을 옮김). 커널 PEB32 27칸 (run139) 에는 없는 칸.
- **32비트 `Csr*` 전역은 하나만 쓴다** `[정적]`: ntdll32 `_CsrClientProcess` (rva `0x13911c`, 1 B) ← 1 (`CsrClientConnectToServer +0x28`, 성공이고 출력 바이트 0 일 때). `_CsrInitOnceDone` · `_CsrServerApiRoutine` 은 이 길에서 아무도 안 씀. 진짜 `Csr*` 9개는 **64비트 ntdll 안** (run68 그대로).
- **BSSD 읽기** `[정적]`: 32비트도 64비트와 **같은 BSSD 칸** (`+0x0` · `+0x2` · `+0x8/+0xc` · `+0x10` · `+0x12` · `+0x18/+0x1c` · `+0x38` · `+0x9e8`, `+0x178` 은 주소만) 을 읽는다. 다른 건 폭 (8 B 칸의 하위 4 B) · `UNICODE_STRING` 16 B → 8 B 로 줄임 · PEB 오프셋 셋 · `KernelBaseGlobalData` 안 자리.
- **4GB** `[정적]`: 32비트 코드가 `R` (PEB32 `+0x4c`) 하위 4 B 로 BSSD 를 직접 읽으므로 **공유 섹션 뷰 `R` 은 4GB 아래여야** 한다. 그런데 64비트 `CsrpConnectToServer +0x338 NtMapViewOfSection` 은 `ZeroBits = 0` 이라 사용자 코드엔 그 요구가 없다 — **커널의 `HighestUserAddress` 상한** (run139) 에만 기댄다. `C` (csrss 쪽 주소) 는 높아도 된다 (차이만 씀).
- **FALSE 가지** `[정적]`: 안쪽 F1 ~ F5 (`+0x1bc` SysInfo · **`+0x20e` 연결** · `+0x3bc` NLS · `+0x3da` 락 · `+0x3f4` 콘솔, 모두 `+0x4d1` 로) + `entry` E1 (안쪽 결과) · E2 (`ARI::Globals::Initialize`, 비패키지는 늘 0) + E3 `PcwDllMain` (`NtQueryInformationProcess(−1, 0x1a)`).
- **원문 막힘의 뿌리** `[정적]`: `NtOpenSection(…\SharedSection) = 0xc000003a` → 64비트 `CsrpConnectToServer +0x1bf JS +0x46a` → … → wow64 가 같은 상태를 돌려줌 → ntdll32 `+0x1f JS +0x38` → kernelbase32 **F2** → `entry` 0 → ntdll32 `_LdrpInitializeNode@4 +0x247` `0xc0000142` → `_LdrpInitializationFailure@4 +0x5f NtRaiseHardError(0xc0000145)`. 연결이 풀리면 다음 막힐 곳은 F3 (NLS) · F5 (콘솔) 차례일 것 `[추측]`.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백열째 바퀴. plan **Q287**. **짐 지도 바퀴 아님** (7.6.17 메모 두 줄만).
- 정적: `ntdll32` · `wow64` · `csrsrv` 는 있던 프로젝트에 export 만 더, **`kernelbase32` (SysWOW64 `KernelBase.dll`) 는 처음 analyze** (결과서: 14:15:55 → 14:17:33, `_index.tsv` 7,604줄, `SetPdb` 줄 `ms/ghidra/kernelbase32.analyze.log:347 ~ 348`, 시작 전 `free -g` available 28). 64비트 `ntdll` · `kernelbase` · `basesrv` 는 있던 `.asm` 읽기만.

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| **kernelbase32 (새로)** = `SysWOW64\KernelBase.dll` (x86, 2,974,168 B, 머신 `0x14c`, ImageBase `0x10000000`) | `wkernelbase.pdb` `D14348CD34CC97EAEC7321B3CBCB001D1` | **새 줄**. flow-writer 가 디버그 디렉터리로 앞 16자 `D14348CD34CC97EA` · Age 1 · 이름 `wkernelbase.pdb` 다시 읽음 (같음). 64비트 `kernelbase` 프로젝트와 다른 모듈 이름 |
| ntdll32 = `SysWOW64\ntdll.dll` | `wntdll.pdb` `7C1F7D48576AAE47EB48E43D5FB752AD1` | 같음 (run140) |
| wow64 | `AED569E2D483C3CDC0C97DEC3691CD551` | 같음 (run138) |
| csrsrv | `32C5AA2B9D0AF671C35069A02C5A0C041` | 같음 (run34 · run71) |
| ntdll (64비트) | `C3093720177DA6851519DCFFE8DF53FE1` | 같음 (읽기만) |
| kernelbase (64비트) | `72CCE506944DE0267C172AD64287F9BD1` | 같음 (읽기만) |

- 버전 대조 (결과서 "버전 대조" — 모두 일치): 64비트 `CsrpConnectToServer +0x1b6/+0x1bb → NtOpenSection` (run68 표) = 이번 `.asm`. ntdll32 스텁 번호 ↔ wow64 nt 표 칸 (`0x37` · `0xa4` · `0x1e9`). PEB32 `+0x1d4 SessionId` 커널이 씀 (run139 `MiInitializeWowPeb +0x141 ~ +0x144`) ↔ kernelbase32 이 읽음 (`+0x13c`).

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): `r19 000b0f41b3f0 seq 647 NtQuerySystemInformation 0x3e → 648 NtOpenSection("\Sessions\1\Windows\SharedSection", 4)` (팀원 `0xc000003a`) → 651 · 654 32비트 KERNELBASE · KERNEL32 내림 → 657 `NtRaiseHardError 0xc0000145` → 658 `NtTerminateProcess 0xc0000142`.
  1. 32비트 `CsrClientConnectToServer` → `CsrpConnectToServer` 의 syscall 차례 · 인자가 64비트 여섯과 같은가. 연결 정보 블록 크기 · 칸 · 응답 칸 (포인터 폭 4 / 8 B), 쓰는 PEB32 칸 · 32비트 `Csr*` 전역.
  2. wow64 썽크 넷 (`whNtOpenSection` · `whNtCreateSection` · `whNtConnectPort` / `whNtSecureConnectPort` · `whNtMapViewOfSection`) 의 32 ↔ 64 변환 칸 · 4GB 아래 요구 · 연결 정보 블록을 그대로 넘기나 · status 를 바꾸는 줄.
  3. 32비트 `_KernelBaseBaseDllInitialize` 가 연결 뒤 읽는 칸 (`BaseStaticServerData` 꺼내는 PEB32 칸 · 인덱스 · 폭), 64비트 표와 다른 칸, **FALSE 로 나가는 가지 전부** (원문 `0xc000003a` 가 어디로 가나). (되면) csrsrv WoW64 줄.
- 쓰는 곳 (원문): `Src/nt/TeamSyscall.cpp` · `Src/nt/AddressSpace.cpp` (`MapCsrSharedSectionView` · `MapCsrPortView`) · 팀원 `src/alpc/csr_user_connect_stub.cpp`.

## 방법

- 정적 sast/147 (요청서 `_workspace/sast/147_wow64-csr.md`, 결과서 `_workspace/sast/147_wow64-csr_result.md`, **분석성공**, 스크립트 문제 없음 — 요청서 표기 하나만: `kernelbase32.dll=SysWOW64/KernelBase.dll` 로 써야 PDB 가 `ms/sym/kernelbase32.dll.pdb` 로 감):
  - kernelbase32 (새 analyze): `rva:0x15afb0,0x15b0d4,0x15b185,0x15b127` depth 1 + `rva:0x15babc` (`_NlsProcessInitialize@0`) depth 1 — 41개.
  - ntdll32 (export 더): `CsrClientConnectToServer` · `NtWow64CsrClientConnectToServer` · `NtOpenSection` · `NtCreateSection` · `NtConnectPort` · `NtSecureConnectPort` · `NtMapViewOfSection` (+`Ex` 둘) · `RtlRegisterThreadWithCsrss` · `NtWow64CsrClientCallServer` · `CsrClientCallServer` + 로더 셋 (`_LdrpInitializeNode@4` · `_LdrpInitializationFailure@4` · `_LdrpCallInitRoutine@16`). refs 셋 (`_CsrClientProcess` · `_CsrInitOnceDone` · `_CsrServerApiRoutine`).
  - wow64 (export 더): 썽크 여섯 (`whNtOpenSection` · `whNtCreateSection` · `whNtConnectPort` · `whNtSecureConnectPort` · `whNtMapViewOfSection` · `whNtWow64CsrClientConnectToServer`) depth 1 (도우미 포함 19) + `whNtQuerySystemInformation` · `…_SystemBasicInformation` · `…_SystemEmulationBasicInformation` · `Wow64InitializeEmulatedSystemInformation` · `RedirectPath`.
  - csrsrv: `CsrProcessLazyRegister` 하나 더.
  - 파이썬 (스크래치): wow64 PE 의 nt 표 (`sdwhnt32` 함수 배열 `0x3a550`) · import 표.
- flow-writer 가 다시 본 것:
  - `.asm`: ntdll32 `CsrClientConnectToServer` 전부 · `NtWow64CsrClientConnectToServer` 전부 · 스텁 다섯 (`NtOpenSection` `0x37`/`RET 0xc` · `NtCreateSection` `0x4a`/`0x1c` · `NtConnectPort` `0xa4`/`0x20` · `NtSecureConnectPort` `0x194`/`0x24` · `NtMapViewOfSection` `0x28`/`0x28`) · `_LdrpInitializeNode@4 +0x184 · +0x189` · 조각 `FUN_4b2b857e +0x15 · +0x3e` · `__LdrpInitialize@8 +0x2a6` · `_LdrpInitializationFailure@4 +0x5a · +0x5f` · `_refs/_CsrClientProcess.tsv`; wow64 `whNtWow64CsrClientConnectToServer` 전부 · `whNtOpenSection +0x33 ~ +0x60` · `whNtCreateSection +0x47 ~ +0x8c` · `whNtConnectPort +0x8a ~ +0x9d · +0x10e ~ +0x278` (전부) · `whNtSecureConnectPort +0x3f · +0x73 ~ +0xa2 · +0xb2 · +0x117 · +0x156 · +0x1a9 · +0x1c1 · +0x1d1 ~ +0x1d8 · +0x1ee · +0x229` · `whNtMapViewOfSection +0xa8 · +0xb5 ~ +0xc6 · +0x1b5 · +0x1ca ~ +0x1e4 · +0x255 · +0x288 · +0x2c3 · +0x2f5 · +0x306 · +0x33b` · `Wow64InitializeEmulatedSystemInformation` 전부 · `whNtQuerySystemInformation +0x62 · +0x65 · +0x7c` · `…_SystemBasicInformation +0x64 ~ +0x83 · +0x19b · +0x1a0` · `Wow64pThunkLegacyPortViewIn +0x2c · +0x2f · +0x6d · +0x92 · +0x99 · +0xb5` · `…PortViewOut +0x8 · +0x13 · +0x36` · `…RemoteViewIn +0x28` · `…RemoteViewOut +0x8` · `Wow64ShallowThunkAllocObjectAttributes32TO64_FNC +0x352 · +0x35a · +0x36f` · `…SecurityQualityOfService32TO64_FNC +0x65 · +0x85` · `RedirectPath +0x7b · +0x92`; kernelbase32 `entry +0x16 · +0x1b · +0x24 · +0x57 · +0x5a · +0x82 ~ +0x8d · +0x10c · +0x111 · +0x113 · +0x114` · `_KernelBaseBaseDllInitialize@12` 전부 · `__KernelBaseBaseDllInitialize@12 +0x3d · +0x118 ~ +0x4e0` (결과서 줄 전부, `+0x1dd ~ +0x252` 는 줄마다) · `_KernelBaseDllInitializeMemoryManager@0 +0x27 ~ +0x49` · `_PcwDllMain@12 +0x16 ~ +0x2a · +0x54` · `Initialize__159865 +0xe0 ~ +0xe5` · `_ConsoleInitialize@0` (C1 ~ C4 줄 전부) · `_BaseNlsDllInitialize@12 +0xe ~ +0x22 · +0x5b` · `_NlsProcessInitialize@0 +0x11 ~ +0x69` · `_RegKrnInitialize@12 +0x27 · +0x61 · +0x83`; 64비트 ntdll `CsrpConnectToServer` (결과서 줄 전부 — `+0xd9 · +0x1b6 ~ +0x1bf · +0x23d · +0x251 · +0x282 · +0x28c · +0x295 ~ +0x29c · +0x2d3 · +0x2df ~ +0x308 · +0x338 ~ +0x3ab · +0x3f3 · +0x3f8 · +0x411 · +0x42c · +0x43d · +0x46a ~ +0x481`) · `CsrClientConnectToServer +0x5e ~ +0x11a`; basesrv `BaseClientConnectRoutine +0x0 ~ +0x1e`; csrsrv `CsrApiHandleConnectionRequest +0x9c ~ +0xd0` · `CsrpHandleInlineUserConnect +0x15 · +0x2d · +0x84` · `CsrProcessLazyRegister +0x126 · +0x164 · +0x186 ~ +0x1cd`.
  - 파이썬 (`pefile`, 스크래치 — 레포 밖): wow64 `sdwhnt32` (`0x3a550`) 칸 여섯 (`0x37` → `0x27a0` · `0x4a` → `0x2830` · `0x28` → `0x9640` · `0xa4` → `0x1f870` · `0x194` → `0x1f390` · `0x1e9` → `0x23200`) — 결과서와 같음. wow64 import 칸 `0x3c5f8` = `ntdll.dll!CsrClientConnectToServer` · `0x3ba00` = `NtOpenSection` · `0x3c7f8` = `NtConnectPort`. kernelbase32 디버그 디렉터리.
  - `ms/decomp/kernelbase32/_index.tsv`: `CtrlRoutine` = rva `0x2408b0` (블록 첫 칸 `0x102408b0` 의 이름).
  - PE_emulator 쪽 (읽기만): 원문 374 · `Src/ntdll/BaseDllInit.h:30 ~ :31` (주석 두 줄) · `Src/nt/AddressSpace.cpp:735 ~ 743` · `:1421 ~ 1450` · `Src/nt/VirtualMemory.cpp:1450 ~ 1456` · `ext/syscall/syscall_layer_cpp/src/alpc/` 파일 목록. 결과서가 적은 줄 번호가 있는지만 봄.
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **csrss = 동네 주민센터.** 새 프로그램은 처음에 주민센터에 전입 신고 (연결) 를 하고, 센터가 나눠 주는 **공용 게시판 사본** (공유 섹션 뷰) 을 받아 간다.
- 64비트 프로그램은 직접 창구에 간다. 32비트 프로그램 (WoW64) 은 **대리인 (wow64) 에게 "전입 신고 해 줘" 쪽지 한 장** 만 준다. 대리인은 서류를 한 장씩 번역하지 않고, **64비트 사람이 쓰는 신고 절차 (64비트 ntdll) 를 그대로 대신 밟는다**.
- 끝나면 대리인이 받은 게시판 주소 셋을 32비트 수첩 (PEB32) 에 **옮겨 적어 준다**. 신고가 실패했어도 빈칸 (0) 을 옮겨 적는다 `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{kernelbase32,ntdll32,wow64,ntdll,basesrv,csrsrv}/`)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `kernelbase32!entry` (rva `0x15afb0`, 이유 1) | `+0x16` | `+0x1b` | `_KernelBaseBaseDllInitialize@12` (바깥, rva `0x15b0d4`) | [정적] |
| 〃 | `+0x82` | `+0x87` | `ARI::Globals::Initialize` (rva `0x159865`, 별칭 하나 `?Initialize@Globals@ARI@@SGJXZ`) | [정적] |
| 〃 | `+0x10c` | `+0x111` | `_PcwDllMain@12` (rva `0x15b127`) | [정적] |
| `kernelbase32!_KernelBaseBaseDllInitialize@12` | `+0x19` · `+0x25` | `+0x1e` · `+0x2a` | `__DllMainCRTStartupForGS2@12` · **`__KernelBaseBaseDllInitialize@12`** (안쪽, rva `0x15b185`) — 64비트는 꼬리 `JMP`, 32비트는 `CALL` | [정적] |
| `kernelbase32!__KernelBaseBaseDllInitialize@12` | `+0x118` | `+0x11d` | `_KernelBaseDllInitializeMemoryManager@0` (안에서 `+0x27/+0x2d NtQuerySystemInformation(0x32, 4 B)` · `+0x43/+0x49 RtlInitializeHandleTable`) | [정적] |
| 〃 | `+0x131` · `+0x142` | `+0x137` · `+0x148` | `RtlCreateTagHeap` (→ `KernelBaseGlobalData+0x0`) · `RtlGetCurrentServiceSessionId` (PEB32 `+0x1d4` 와 비교) | [정적] |
| 〃 | `+0x1a3` | `+0x1a8` | `_StringCbPrintfW` (`\Sessions\<n>\Windows`) | [정적] |
| 〃 | `+0x1b4` | `+0x1ba` | `NtQuerySystemInformation(0, &SysInfo, 0x2c)` — F1 | [정적] |
| 〃 | **`+0x206`** | **`+0x20c`** | **`CsrClientConnectToServer(dir [EBP-0x204], 1, &{CtrlRoutine, 0} [EBP-0x21c], 8, &byte [EBP-0x205])`** (import `[0x1028db38]`) — F2 | [정적] |
| 〃 | `+0x372` | `+0x378` | `RtlUnicodeStringToAnsiString` | [정적] |
| 〃 | `+0x3b5` | `+0x3ba` | `_BaseNlsDllInitialize@12` (별칭 하나 `BaseNlsDllInitialize`) — F3 | [정적] |
| 〃 | `+0x3d2` | `+0x3d8` | `RtlInitializeCriticalSection(KernelBaseGlobalData+0x5c)` — F4 | [정적] |
| 〃 | `+0x3eb` | `+0x3f0` | `_ConsoleInitialize@0` — F5 | [정적] |
| 〃 | `+0x3fb` · `+0x4ca` | `+0x400` · `+0x4cf` | ETW 등록 · `_RegKrnInitialize@12` (이유 1 은 늘 TRUE) | [정적] |
| `kernelbase32!_PcwDllMain@12` | `+0x22` | `+0x28` | `NtQueryInformationProcess(−1, 0x1a, &x, 4, 0)` — E3 | [정적] |
| `kernelbase32!_BaseNlsDllInitialize@12` | `+0x1b` | `+0x20` | `_NlsProcessInitialize@0` (0 아니면 `+0x22 JNZ +0x5b XOR AL,AL`) | [정적] |
| `kernelbase32!_NlsProcessInitialize@0` | `+0x11` · `+0x26` · `+0x4f` | `+0x16` · `+0x2b` · `+0x54` | `_AllocTables@0` · `_SetupMainNlsFiles@4` · `_MakeLocHashNode@4(0x7f)` | [정적] |
| `kernelbase32!_ConsoleInitialize@0` | `+0x28` · `+0xf4` · `+0x125` · `+0x156` · `+0x163` | `+0x2e` · `+0xf9` · `+0x12a` · `+0x15b` · `+0x168` | `RtlInitializeCriticalSection` · `_ConsoleCreateConnectionObject@20` · `_ConsoleAllocate@4` · `_ConsoleSanitizeStandardIoObjects@4` · `_ConsoleCleanupConnectionState@4` | [정적] |
| `ntdll32!CsrClientConnectToServer` (rva `0x72ab0`, 62 B) | `+0x16` | `+0x1b` | `NtWow64CsrClientConnectToServer` (rva `0x7ba30`, 별칭 둘 `_ZwWow64CsrClientConnectToServer@20` · `_NtWow64CsrClientConnectToServer@20`) | [정적] |
| `ntdll32!NtWow64CsrClientConnectToServer` | `+0xa` (`CALL EDX`) | `+0xc` | `_Wow64SystemServiceCall@0` (`+0x0 MOV EAX,0x1e9` · `+0xc RET 0x14`) ⇒ wow64cpu → `Wow64SystemServiceEx` (run138) | [정적] |
| `wow64!whNtWow64CsrClientConnectToServer` (rva `0x23200`, 158 B, 칸 `0x1e9`) | **`+0x1c`** (`CALL [0x18003c5f8]`) | **`+0x23`** | **64비트 `ntdll!CsrClientConnectToServer`** (import — PE 표로 확인) | [정적] |
| `ntdll!CsrClientConnectToServer` (rva `0xdb7a0`) | `+0x77` · `+0x113` | `+0x7c` · `+0x118` | `RtlImageNtHeaderEx` (`+0x81 CMP [RAX+0x5c],1` — WoW64 에선 32비트 exe 헤더) · `CsrpConnectToServer` (rva `0xdbfe4`) | [정적] (run68) |
| `ntdll!CsrpConnectToServer` | `+0x1b6` · `+0x23d` · `+0x2df` · `+0x2eb` · `+0x338` · `+0x344` | `+0x1bb` · `+0x242` · `+0x2e4` · `+0x2f0` · `+0x33d` · `+0x349` | `NtOpenSection` · `NtCreateSection` · `NtConnectPort` · `NtClose` · `NtMapViewOfSection` · `NtClose` (run68 여섯 그대로) | [정적] (run68) |
| 〃 | `+0x3f3` · `+0x47a` | `+0x3f8` · `+0x47f` | `RtlpCreateHeap(0x8000, 포트 뷰, 0x10000, …)` · `RtlFreeHeap(CsrHeap, CsrPortName.Buffer)` (실패 길) | [정적] |
| `ntdll32!_LdrpInitializeNode@4` (rva `0x38375`) | `+0x184` | `+0x189` | `_LdrpCallInitRoutine@16` (결과 → `[EBP-0x1a]`) | [정적] |
| `ntdll32!__LdrpInitialize@8` | `+0x2a6` | `+0x2ab` | `_LdrpInitializationFailure@4` | [정적] |
| `ntdll32!_LdrpInitializationFailure@4` | `+0x5f` | `+0x64` | `NtRaiseHardError` (`+0x5a PUSH 0xc0000145`, 별칭 둘 `_ZwRaiseHardError@24` · `_NtRaiseHardError@24`) | [정적] |
| `wow64!whNtQuerySystemInformation` | `+0x7c` | `+0x81` | `whNtQuerySystemInformation_SystemBasicInformation` (클래스 0 일 때 — `+0x62 TEST R10D` · `+0x65 JZ +0x79`) | [정적] |
| `wow64!whNtQuerySystemInformation_SystemBasicInformation` | `+0x6f` | `+0x76` | 64비트 `NtQuerySystemInformation` (클래스 `0x3e` — `+0x19b MOV ECX,0x3e` · `+0x1a0 JMP +0x64`) | [정적] |
| `wow64!Wow64InitializeEmulatedSystemInformation` | `+0x33` | `+0x3a` | 64비트 `NtQuerySystemInformation(0x3e, 버퍼 0x40 B)` → `+0x43 ~ +0x48` `Wow64pMaxAddress` ← 버퍼 `+0x28` | [정적] |
| `wow64!whNtOpenSection` (rva `0x27a0`, 칸 `0x37`) | `+0x33` · `+0x4a` | `+0x38` · `+0x51` | `Wow64ShallowThunkAllocObjectAttributes32TO64_FNC` · 64비트 `NtOpenSection` | [정적] — **CSR 길엔 안 낌** |
| `wow64!whNtCreateSection` (`0x2830`, `0x4a`) | `+0x47` | `+0x4c` | OA 썽크 (그다음 64비트 `NtCreateSection`) | [정적] — 안 낌 |
| `wow64!whNtConnectPort` (`0x1f870`, `0xa4`) | `+0x10e` · `+0x14d` · `+0x171` · `+0x1aa` · `+0x1ca` · `+0x1e5` | `+0x115` · `+0x152` · `+0x176` · `+0x1af` · `+0x1cf` · `+0x1ea` | 64비트 `NtConnectPort` · `Wow64pIsLegacyLpcPort` · `Wow64pThunkLegacyPortViewOut` · `…PortViewIn` · `…RemoteViewIn` · `…RemoteViewOut` | [정적] — 안 낌 |
| `wow64!whNtSecureConnectPort` (`0x1f390`, `0x194`) | `+0xb2` · `+0x117` · `+0x156` · `+0x1c1` · `+0x1ee` · `+0x229` | `+0xb7` · `+0x11c` · `+0x15b` · `+0x1c8` · `+0x1f3` · `+0x22e` | SQOS 썽크 · `…PortViewIn` · `…RemoteViewIn` · 64비트 `NtSecureConnectPort` · `…PortViewOut` · `…RemoteViewOut` | [정적] — 안 낌 |
| `wow64!whNtMapViewOfSection` (`0x9640`, `0x28`) | `+0xa8` · `+0x1b5` · `+0x255` · `+0x288` · `+0x2c3` · `+0x2f5` · `+0x306` | `+0xad` · `+0x1bc` · `+0x25c` · `+0x28f` · `+0x2c8` · `+0x2fa` · `+0x30d` | `Wow64ShallowThunkSIZE_T32TO64` · 64비트 `NtMapViewOfSection` · `RtlIsCurrentProcess` · `NtQuerySection` · `Wow64pIsCompatibleMachineType` · `CpuNotifyMapViewOfSection` · `NtUnmapViewOfSection` | [정적] — 안 낌 |
| `csrsrv!CsrProcessLazyRegister` | `+0x126` · `+0x164` · `+0x19d` · `+0x1cd` | `+0x12d` · `+0x16b` · `+0x1a4` · `+0x1d4` | `NtAlpcOpenSenderProcess` ×2 · `NtQueryInformationProcess(0x18 = 4+0x14)` · `NtQueryInformationProcess(0x25 = 0x40−0x1b)` | [정적] |

### 3. 32비트 `__KernelBaseBaseDllInitialize@12` 가 연결 뒤 읽는 것 (먼저 — 에뮬레이터를 막는 줄) [정적]

- **이름 짝**: 64비트 `KernelBaseBaseDllInitialize` ↔ 32비트 `_KernelBaseBaseDllInitialize@12` (77 B, 바깥) · 64비트 `_KernelBaseBaseDllInitialize` ↔ 32비트 `__KernelBaseBaseDllInitialize@12` (1251 B, 안쪽 — 일은 여기) · `entry` ↔ `entry` (286 B). 32비트 `+0x206/+0x20c` 연결이 64비트 `+0x2d2/+0x2d9` 자리 (run68).
- **PEB32 에서 꺼내는 셈** (`+0x21f ~ +0x248`):
  - `+0x22d MOV EAX,[EAX+0x54]` — PEB32 `+0x54 ReadOnlyStaticServerData` = 표 시작 `T` (4 B). 64비트 PEB `+0x98` (8 B).
  - `+0x230 MOV EDI,[EAX+0x8]` — **표 인덱스 1** (BASESRV 번호 1) 의 **하위 4 B**. 표 칸은 8 B 단위 — 64비트 `[T+8]` 과 인덱스 같고 폭만 반.
  - `+0x239 SUB EDI,[ECX+0x248]` — PEB32 `+0x248 CsrServerReadOnlySharedMemoryBase` (칸 8 B 의 하위 4 B) = `C`. 64비트 PEB `+0x380`.
  - `+0x23f ADD EDI,[EAX+0x4c]` — PEB32 `+0x4c ReadOnlySharedMemoryBase` = 내 뷰 `R` (4 B). 64비트 PEB `+0x88`. (`+0x233` 에서 `EAX` 를 PEB32 로 다시 읽음.)
  - `+0x248` `_BaseStaticServerData` (rva `0x2848cc`) ← 그 값. 64비트 rva `0x3aedb8` 과 같은 역할.
- **BSSD 칸 읽기 — 64비트 표 (run68 · run71) 와 다른 칸은 없다**:

| 32비트 줄 | 칸 | 폭 | 64비트 | 같음 / 다름 |
|---|---|---|---|---|
| `+0x242 CMP [EBP-0x205],BL` · `+0x252 SETNZ` | 연결이 돌려준 출력 바이트 ≠ 0 → `KernelBaseGlobalData+0x4` (`0x102848e4`) | 1 B | `+0x326` · `+0x337` | 같음 |
| `+0x24e` · `+0x259` | BSSD `+0x38` (word) → `_BaseRCNumber` (rva `0x2848d0`) | 2 B | `+0x332` · `+0x342` | 같음 |
| `+0x25f` · `+0x268` → `+0x262` · `+0x26c` | BSSD `+0x0` · `+0x2` (`Length` · `MaximumLength`) → `KernelBaseGlobalData+0x8` · `+0xa` | 2 + 2 B | `MOVUPS` 16 B → `+0x8` | 칸 같고 **16 B → 8 B 로 줄임** |
| `+0x272 MOV EAX,[EDI+8]` · `+0x275 OR EAX,[EDI+0xc]` | BSSD `+0x8` `Buffer` 8 B 가 0 인지 (하위 · 상위 둘 다) | 4 + 4 B | `MOVQ` 8 B `TEST` | 같음 (0 이면 옮김 건너뜀) |
| `+0x2b6` · `+0x2cf` | `KernelBaseGlobalData+0xc` ← `BSSD지역 + [BSSD+8] − [BSSD지역+0x9e8]` | 4 B | `+0x363 ~ +0x3d3` | 식 같음, 4 B 셈 |
| `+0x2d5` · `+0x2df` → `+0x2d9` · `+0x2e3` | BSSD `+0x10` · `+0x12` → `KernelBaseGlobalData+0x10` · `+0x12` | 2 + 2 B | `MOVUPS [R10+0x10]` | 같음 |
| `+0x2e9 ~ +0x346` (`+0x32d` 가 `+0x9e8` 다시 읽음) | BSSD `+0x18` `Buffer` (하위 `+0x18` · 상위 `+0x1c`) → `KernelBaseGlobalData+0x14` | 4 B | `+0x3f0 ~ +0x469` | 같음 |
| `_BaseNlsDllInitialize@12 +0xe ~ +0x16` | BSSD `+0x178` 의 **주소만** → `_gpServerNlsUserInfo` (rva `0x285790`) | 4 B | `+0x9 ~ +0x10` (run71 정정) | 같음 |

- **옮김 식은 64비트와 같다**: `BSSD지역 = [T+8] − C + R`, `Buffer지역 = [BSSD+8] + BSSD지역 − [BSSD지역+0x9e8]`. 차이만 쓰므로 **`R` 이 4GB 아래이기만 하면 `C` 는 64비트 높은 주소여도 맞다** (하위 4 B 끼리 빼도 결과가 같음). 단 `T − C` 와 `S − C` (S = BSSD 의 csrss 쪽 주소) 가 4GB 안이어야 한다.
- **FALSE 가지 전부 (이유 1)** — 안쪽 FALSE 는 모두 `+0x4d1` (`MOV ECX,[EBP-4]`) → `+0x4d4 MOV AL,BL` (BL = 0, `+0x3d XOR EBX,EBX`) → `+0x4e0 RET 4`. `_RegKrnInitialize` (`+0x4ca`) 는 건너뛴다.

| # | 자리 | 조건 | 아래에서 FALSE 가 되는 까닭 |
|---|---|---|---|
| F1 | `+0x1b4/+0x1ba` · `+0x1bc JS +0x4d1` | `NtQuerySystemInformation(0, …, 0x2c)` 음수 | wow64 가 클래스 0 을 `0x3e` 로 바꿔 부르고 (`…_SystemBasicInformation +0x19b`) 결과를 그대로 올림 (`+0x7b MOV EBX,EAX` · `+0x81 TEST` · `+0x83 JS +0x162`) |
| **F2** | **`+0x206/+0x20c` · `+0x20e JS +0x4d1`** | **`CsrClientConnectToServer` 음수** | 아래 "원문 막힘". 그 밖 `0xc0000017` (힙) · `NtConnectPort` · `NtMapViewOfSection` 실패도 같은 길. (run153 때 보탬) `0xc000000d` — 블록은 있는데 크기 0 (64비트 ntdll `CsrClientConnectToServer +0x37 JZ → +0x1d5`, 결과서 3-3) |
| F3 | `+0x3b5/+0x3ba` · `+0x3bc JZ +0x4d1` | `_BaseNlsDllInitialize@12` AL = 0 | `_NlsProcessInitialize@0` 이 0 아닌 값: `_AllocTables@0` (`+0x1a JNZ`) · `_SetupMainNlsFiles@4` (`+0x2f JNZ`) · `_MakeLocHashNode@4(0x7f)` 가 0 이면 `0x57` (`+0x5c JNZ` 아니면 `+0x69 PUSH 0x57`) |
| F4 | `+0x3d2/+0x3d8` · `+0x3da JS +0x4d1` | `RtlInitializeCriticalSection` 음수 | (ntdll, 메모리 부족 급) |
| F5 | `+0x3eb/+0x3f0` · `+0x3f0 MOV BL,AL` · `+0x3f2 TEST BL,BL` · `+0x3f4 JZ +0x4d1` | `_ConsoleInitialize@0` AL = 0 | C1 `+0x30 JS +0x16f` (락) · C2 `+0xfb JNS` 아니고 `+0xfd CMP EAX,0xc0000022` · `+0x102 JNZ +0x168` (연결 객체, 또는 로우박스 검사 `+0x10f JS` · `+0x116 JZ`) · C3 `+0x12c JS +0x168` (`_ConsoleAllocate`) · C4 `+0x15d JNS` 아니면 `+0x163 _ConsoleCleanupConnectionState` → `+0x168`. 모두 `+0x16f XOR AL,AL` |
| E1 | `entry +0x16/+0x1b` · `+0x24 MOV DL,AL` · `+0x57 CMP DL,1` · `+0x5a JNZ +0x113` | 안쪽 결과 FALSE | F1 ~ F5 가 여기로 모여 `+0x114 MOVZX EAX,DL` = 0 |
| E2 | `entry +0x82/+0x87` · `+0x89 JNS +0x92` · `+0x8b XOR DL,DL` · `+0x8d JMP +0x113` | `ARI::Globals::Initialize` HRESULT 음수 | **비패키지 프로세스는 늘 0**: `Initialize +0xe0 MOV AL,[PEB32+3]` · `+0xe3 TEST AL,0x10` · `+0xe5 JZ +0x39d` → 0 `[정적]`. 음수는 `AppModelPolicy_GetPolicy` 실패 때 `[추측]` |
| E3 | `entry +0x10c/+0x111 _PcwDllMain@12` · `+0x111 MOV DL,AL` | `_PcwDllMain@12` AL = 0 | `+0x22/+0x28 NtQueryInformationProcess(−1, 0x1a = ProcessWow64Information, &x, 4, 0)` 음수면 `+0x2a JS +0x54 XOR AL,AL` — 32비트 초기화의 syscall 하나 더 |

- **원문 막힘 `NtOpenSection(…\SharedSection, 4) = 0xc000003a` 가 가는 길** (한 줄씩, `.asm` 에서 다시 봄):
  1. 64비트 `CsrpConnectToServer +0x1bb MOV EDI,EAX` · `+0x1bf JS +0x46a` → `+0x46a` `CsrPortName.Buffer` 읽기 → `+0x47a/+0x47f RtlFreeHeap(CsrHeap, …)` → `+0x47f MOV EAX,EDI` → `+0x481 JMP +0xd9` (맺음). `CsrPortName.Buffer` 는 풀렸지만 전역에 값이 그대로 남는다 (0 으로 안 지움).
  2. 64비트 `CsrClientConnectToServer +0x118 TEST EAX,EAX` · `+0x11a JS +0xca` (출력 바이트 안 씀) → 반환.
  3. wow64 `whNtWow64CsrClientConnectToServer +0x28` 이 상태를 `[RSP+0x40]` 에 두고 **상태를 안 보고** PEB32 세 칸에 PEB64 값 (이때 0) 을 옮긴 뒤 `+0x95 MOV EAX,[RSP+0x40]` 로 같은 상태를 돌려준다. nt 표 오류 종류 칸이 0 이라 `Wow64SystemServiceEx` 도 안 바꾼다 (run138).
  4. ntdll32 `CsrClientConnectToServer +0x1b MOV EDX,EAX` · `+0x1f JS +0x38` (`_CsrClientProcess` 안 씀) → `+0x3b RET 0x14`.
  5. kernelbase32 `+0x20c TEST` · `+0x20e JS +0x4d1` → **F2** → AL 0 → 바깥 `+0x2a ~ +0x2d RET 4` → `entry +0x24 MOV DL,AL` · `+0x5a JNZ +0x113` → 0 반환.
  6. ntdll32 `_LdrpInitializeNode@4 +0x189 MOV [EBP-0x1a],AL` → 조각 `FUN_4b2b857e +0x15` (= 함수 `+0x21e`) `CMP [EBP-0x1a],0` → 조각 `+0x3e` (= `+0x247`) **`MOV [EBP-0x28],0xc0000142`** → 올라가 `__LdrpInitialize@8 +0x2a6 _LdrpInitializationFailure@4` → `+0x5a PUSH 0xc0000145` · `+0x5f NtRaiseHardError`. 원문 657 · 658 과 같은 꼴 (658 `NtTerminateProcess` 쪽은 안 따라감 `[추측]`).
- 그래서 **지금 에뮬레이터의 `0xc0000142` 뿌리는 `NtOpenSection(SharedSection)` 한 번의 실패** 이고, F2 하나로 모인다 `[정적]`. 고치면 그다음은 F3 (NLS) · F5 (콘솔) 차례일 것 `[추측]` (F4 는 ntdll 메모리 몫).

### 1. 32비트 `CsrClientConnectToServer` 길 [정적]

- **요청서 · 원문의 전제 "32비트 ntdll `CsrpConnectToServer` / `CsrpCaptureCsrPortServerInfo`" 는 없다.** ntdll32 `_index.tsv` 의 `Csr*` 는 `CsrClientConnectToServer` · `CsrClientCallServer` · `CsrAllocateCaptureBuffer`… 썽크 12개와 `NtWow64Csr*` 10개뿐, `pdb_aliases.py --where ntdll32.dll CsrpConnectToServer` 도 "공개 심볼에 없음" (결과서).
- **32비트 `CsrClientConnectToServer` 몸** (62 B): `+0x6 LEA EAX,[EBP-1]` · `+0x9 PUSH EAX` (자기 지역 바이트 주소) · `+0xa ~ +0x13` 인자 1 ~ 4 (`[EBP+0x8 ~ +0x14]`) 다시 push → `+0x16` syscall 스텁 → `+0x1f JS +0x38` → 성공이면 `+0x21 MOV CL,[EBP-1]` · `+0x26 JNZ +0x2f` · **`+0x28 _CsrClientProcess ← 1`** (바이트가 0 일 때) · `+0x2f ~ +0x36` 호출자 다섯째 인자 (`[EBP+0x18]`) 가 있으면 `*ptr = CL`. (결과서 "인자 5개를 그대로 다시 push" 는 넷 + 자기 바이트 — 아래 "flow-writer 대조" 3.)
- **syscall 차례 — 32비트 / 64비트 (run68) 나란히**: 0 번만 32비트, 1 ~ 6 은 **같은 64비트 코드** 가 낸다.

| # | syscall | 부르는 자리 | 인자 (WoW64 에서) | 64비트 프로세스 (run68) |
|---|---|---|---|---|
| 0 | `NtWow64CsrClientConnectToServer` (칸 `0x1e9`, 인자 5 = `0x14` B) | ntdll32 `CsrClientConnectToServer +0x16/+0x1b` | 32비트 스택 인자 → wow64 `RCX = 인자 포인터` | 없음 — **32비트만** |
| 1 | `NtOpenSection` | 64비트 `CsrpConnectToServer +0x1b6/+0x1bb` | `(&h, 4, OA{0x30, Root 0, &CsrPortName = 디렉터리 + \SharedSection, 0x40})` | 같음 |
| 2 | `NtCreateSection` | `+0x23d/+0x242` | `(&h2, 0xf001f, NULL, &{0x10000}, PAGE_READWRITE 4, SEC_COMMIT 0x8000000, 0)` | 같음 |
| 3 | `NtConnectPort` (legacy, ALPC 아님) | `+0x2df/+0x2e4` | `(&CsrPortHandle, &CsrPortName = …\ApiPort (54 B), SQOS{0, 2, 1, 1}, ClientView{0x30, …}, ServerView{0x18, …}, &최대메시지, 블록 0x30 B, &길이 = 0x30)` | 같음 |
| 4 | `NtClose` | `+0x2eb/+0x2f0` | 만든 섹션 | 같음 |
| 5 | `NtMapViewOfSection` | `+0x338/+0x33d` | `(SharedSection, −1, &R (0 에서), ZeroBits 0 (`+0x305 XOR R9D,R9D`), 0, NULL, &크기 (0), ViewUnmap 2, 0x500000, PAGE_READONLY 2)` | 같음 — 다만 WoW64 에선 `R` 이 4GB 아래로 앉아야 함 (커널 몫) |
| 6 | `NtClose` | `+0x344/+0x349` | `SharedSection` 핸들 | 같음 |
| — | (유저) `RtlpCreateHeap(0x8000, 포트 뷰, 0x10000, …)` | `+0x3f3/+0x3f8` | 포트 뷰 안 개인 힙 | 같음 |

- **wow64 의 인자 바꿈** (`whNtWow64CsrClientConnectToServer +0x4 ~ +0x17`): `RCX = [+0]` 디렉터리 포인터 (0 확장) · `EDX = [+4]` 서버 번호 · `R8 = [+8]` 블록 포인터 (0 확장) · `R9D = [+0xc]` **크기 값 8** · `[RSP+0x20] = [+0x10]` 출력 바이트 포인터 (0 확장). 포인터는 다 32비트 스택 · 데이터라 **64비트 코드가 4GB 아래 주소를 직접 읽는다** (`CsrpConnectToServer +0x295 MOV RAX,[R15]` 8 B).
- **클라이언트 길 조건**: 64비트 `CsrClientConnectToServer +0x81 CMP [RAX+0x5c],1` — `PEB64.ImageBaseAddress` 의 PE 헤더 `Subsystem` ≠ 1. WoW64 에선 이 칸이 32비트 exe 헤더 (같은 오프셋 `+0x5c`). 콘솔 · GUI x86 exe 는 클라이언트 길.
- **연결 정보 블록 칸** — 64비트 ntdll 이 짓는다 (`+0x295 ~ +0x2df`) → WoW64 도 `0x30` B, 포인터 칸 8 B:

| 블록 칸 | 크기 | 방향 | 뜻 | WoW64 (32비트 클라이언트) | 근거 (클라이언트 ; csrss) |
|---|---|---|---|---|---|
| `+0x00` | 8 | 응답 | `SharedSectionBase` = **C** (csrss 쪽 뷰 주소) → PEB64 `+0x380` | 같음 (wow64 가 PEB32 `+0x248` 로 8 B 옮김) | `+0x364 ~ +0x36d` ; csrsrv `CsrApiHandleConnectionRequest +0x9c ~ +0xa6` (메시지 `+0x28`) |
| `+0x08` | 8 | 응답 | `SharedStaticServerData` = 표 **T** (csrss 쪽) → PEB64 `+0x98 = T − C + R` | 같음 | `+0x380 ~ +0x38d` ; `+0xaa ~ +0xb1` (메시지 `+0x30`) |
| `+0x10` | 8 | 응답 | csrss PID → `CsrProcessId` | 같음 | `+0x398 ~ +0x39c` ; `+0xcc ~ +0xd0` (메시지 `+0x38` = csrss `TEB+0x40`) |
| `+0x18` | 8 | — | 0 (읽는 줄 없음) | 같음 | 결과서 |
| `+0x20` | 4 | 요청 | 서버 번호 = 1 | 같음 | `+0x29c` ; `CsrpHandleInlineUserConnect +0x2d CMP [RCX+0x48],1` |
| `+0x24` | 4 | 응답 | 서버 DLL connect 루틴 반환 (0 = 성공) | 같음 | `+0x2f8 ~ +0x308` ; `+0x84 MOV [RBX+0x4c],EAX` |
| `+0x28` | 8 | 요청 | **사용자 정보** = 호출자 블록 8 B 복사 | **`{CtrlRoutine32, 0}`** (0 확장 32비트 주소) | `+0x295 · +0x298` ; kernelbase32 `+0x1ec` · `+0x1ff` ; basesrv `BaseClientConnectRoutine +0x0 CMP [R8],8` · `+0x6 ~ +0x10` |

  - 크기 인자 ≠ 8 이거나 서버 번호 ≠ 1 이면 `+0x282 CMP R12D,8` · `+0x28c CMP R14D,EDI` → `+0x411` 이 상태 칸 `+0x24` 에 `0xc000000d` 를 미리 넣고 사용자 정보는 안 복사한 채 **연결은 그대로 한다** (csrss 가 `CsrpHandleInlineUserConnect +0x15 CMP [RCX+0x4c],0` 로 건너뜀).
  - csrss 쪽 메시지 오프셋 = 블록 오프셋 + `0x28` (PORT_MESSAGE 머리). 커널의 legacy `NtConnectPort` → ALPC 변환 몸은 안 봄 `[공백]` (맞물림만 확인).
- **포트 뷰 구조체 (64비트 `CsrpConnectToServer` 가 짓는 것)**: 클라이언트 `PORT_VIEW` `0x30` B (`RBP-0x88`) — `+0x0 Length 0x30` · `+0x8 SectionHandle` (이름 없는 `0x10000` B 섹션) · `+0x10 SectionOffset` 0 · `+0x18 ViewSize 0x10000` · `+0x20 ViewBase` (출력) · `+0x28 ViewRemoteBase` (출력). 서버 `REMOTE_PORT_VIEW` `0x18` B (`RBP-0x58`) — `+0x0 Length 0x18` · `+0x8 ViewSize 0` · `+0x10 ViewBase 0` (받아만 둠). 연결 뒤 `ViewRemoteBase − ViewBase` → `CsrPortMemoryRemoteDelta` (`+0x3a3 ~ +0x3ab`), `ViewBase` · `ViewSize` → `RtlpCreateHeap` (`+0x3f3`).
- **쓰는 칸**:

| 칸 | 크기 | 쓰는 이 | 값 | 줄 |
|---|---|---|---|---|
| PEB32 `+0x4c ReadOnlySharedMemoryBase` | 4 | **wow64** (연결 뒤, 상태 안 보고) | PEB64 `+0x88` 하위 4 B | `whNtWow64CsrClientConnectToServer +0x57 ~ +0x63` |
| PEB32 `+0x54 ReadOnlyStaticServerData` | 4 | wow64 | PEB64 `+0x98` 하위 4 B | `+0x6f ~ +0x7b` |
| PEB32 `+0x248 CsrServerReadOnlySharedMemoryBase` | **8** | wow64 | PEB64 `+0x380` 통째 | `+0x87 ~ +0x8e` |
| (범위 읽는 법 — run153 때 보탬) | — | — | 위 세 줄 범위 = **PEB64 칸 읽기 → PEB32 쓰기**. 각 앞줄 `+0x4e` · `+0x66` · `+0x7e` 가 `MOV RAX,GS:[0x60]` (PEB64 주소) — 결과서 1-3 은 거기서부터 셈. 쓰는 자리는 `+0x63` · `+0x7b` · `+0x8e` 로 같음 | `+0x4e ~ +0x8e` |
| (PEB32 찾기) | — | wow64 | `TEB32 = TEB64 + [TEB64+0x180c]` (0 이면 `RAX = 0`, 음수면 TEB64 그대로) → `EDX = [TEB32+0x30]` | `+0x2c ~ +0x4b` |
| PEB64 `+0x88` · `+0x98` · `+0x380` · 64비트 ntdll `Csr*` 9개 | 8 … | 64비트 ntdll (run68 그대로) | `CsrHeap` ← **PEB64.ProcessHeap** (`+0xf2`) 등 | run68 |
| ntdll32 `_CsrClientProcess` (rva `0x13911c`) | 1 | ntdll32 `CsrClientConnectToServer +0x28` | 1 | `_refs` 쓰기 1 · 읽기 1 (`RtlRegisterThreadWithCsrss +0x54641`) |
| ntdll32 `_CsrInitOnceDone` (`0x13911d`) · `_CsrServerApiRoutine` (`0x139118`) | 1 · 4 | **이 길에선 아무도** | — | `_refs` 읽기만 (`RtlRegisterThreadWithCsrss +0x1f` · `+0x5464d` · `+0x54672`) |
| kernelbase32 `_BaseStaticServerData` (`0x2848cc`) · `_BaseRCNumber` (`0x2848d0`) · `KernelBaseGlobalData` (`0x2848e0`, `+0x0` · `+0x4` · `+0x8` · `+0xa` · `+0xc` · `+0x10` · `+0x12` · `+0x14`) · `_BasepInitializationStage` (`0x284954`, `0x64` → `0xc8` → `0x190` → `0x1f4` → `0x258` → `0x2bc`) | | `__KernelBaseBaseDllInitialize@12` | | `+0x137` · `+0x1d7` · `+0x219` · `+0x248` · `+0x252` · `+0x259` · `+0x262` · `+0x26c` · `+0x2cf` · `+0x2d9` · `+0x2e3` · `+0x346` · `+0x3af` · `+0x3cc` · `+0x3e5` · `+0x405` |

- **PEB32 세 칸은 연결 전엔 0** — 커널이 안 쓴다 (run139 27칸에 없음) `[정적]`.
- 64비트 `CsrHeap = PEB64.ProcessHeap` 이라 **WoW64 프로세스의 PEB64 `+0x30` 에 쓸 수 있는 힙이 먼저 있어야** `CsrpConnectToServer +0x119 RtlAllocateHeap` 이 통과한다 (원문 로그가 648 까지 갔으니 에뮬레이터엔 있음 — 에뮬레이터 로그, `[관찰]` 아님).

### 2. wow64 썽크 넷 — CSR 길엔 안 낀다 (32비트 코드가 이 넷을 직접 부를 때의 꼴) [정적]

- `whNt*` 입구 `RCX` = 32비트 스택 인자 포인터. 핸들은 `MOVSXD` (부호 확장), 포인터 · 정수는 0 확장.
- **구조체 변환** (`whNtConnectPort` · `whNtSecureConnectPort` 가 `Wow64pThunkLegacy*` 넷으로):

| 구조체 | 32비트 | 64비트 | 안 (32 → 64) | 밖 (64 → 32) |
|---|---|---|---|---|
| `PORT_VIEW` | `0x18`: `+0 Length` · `+4 SectionHandle` · `+8 SectionOffset` · `+0xc ViewSize` · `+0x10 ViewBase` · `+0x14 ViewRemoteBase` | `0x30` (위 1절) | `Length == 0x18` 일 때만 (`…PortViewIn +0x2c CMP [RCX],0x18`): `Length ← 0x30` (`+0x6d`), 핸들 부호 확장, 나머지 0 확장, 임시 힙 `0x30` B, 표시 바이트 ← 1 (`+0x99`). 아니면 **포인터 그대로** (`+0x2f JNZ +0xb5`) | `…PortViewOut +0x8 CMP [RDX],0x18` 이면 `Length ← 0x18` · **하위 4 B 로 자름**, 반환 늘 0 (`+0x36`) — **4GB 넘는지 안 봄** |
| `REMOTE_PORT_VIEW` | `0xc`: `+0 Length` · `+4 ViewSize` · `+8 ViewBase` | `0x18` | `…RemoteViewIn +0x28 CMP [RCX],0xc` 일 때: `Length ← 0x18`, 둘 0 확장, 임시 힙 `0x20` B | `…RemoteViewOut +0x8`: `Length ← 0xc`, 하위 4 B |
| `SECURITY_QUALITY_OF_SERVICE` | `0xc` | `0xc` | 임시 `0x10` B 에 `Length = 0xc` (`+0x65`) · 세 칸 복사 (`~ +0x85`) | — |
| 포트 이름 `UNICODE_STRING` | 8 B | 16 B | 썽크 안에서 (`+0x8a ~ +0x9d`, 두 썽크 같은 줄): `Length` · `MaximumLength` 복사, `Buffer` 0 확장 | — |

- **썽크 넷 한 줄씩**:

| 썽크 | 4GB 아래 요구 | 연결 정보 | status 를 바꾸는 줄 | 핸들 쓰기 |
|---|---|---|---|---|
| `whNtOpenSection` | 없음 | — | OA 변환 실패 (`+0x3c TEST` · `+0x3e JS +0x60`) 만 — 그 상태 반환. 그 밖엔 `NtOpenSection` (`+0x4a/+0x51`) 결과 그대로 | `+0x56 ~ +0x5c` 하위 4 B (상태 무관) |
| `whNtCreateSection` | 없음 | 최대 크기 `LARGE_INTEGER*` 포인터만 넘김 | OA 변환 실패 (`+0x47/+0x4c` · `+0x55 JS +0x8c`) 만 | `+0x82 ~ +0x88` 하위 4 B |
| `whNtConnectPort` | **없음** (뷰 주소는 커널이 정함, 밖 변환은 자르기만) | **포인터 · 길이 포인터 그대로 (0 확장), 길이 안 바꿈** | 뷰 안 변환 실패 (`+0x1aa/+0x1af` · `+0x1ca/+0x1cf`, `0xc0000017` 만) · 밖 변환 실패 (`PortViewOut +0x171/+0x176` → `+0x17e JNS` 아니면 `_local_unwind`, **`RemoteViewOut +0x1e5/+0x1ea` → `+0x1f2 JNS` 아니면 `_local_unwind`**). 그 밖엔 `NtConnectPort` (`+0x10e/+0x115`, `+0x11a MOV EBX,EAX`) 결과 그대로 | `+0x120 ~ +0x128` 하위 4 B (상태 무관). **성공이고 (뷰를 변환했거나 `Wow64pIsLegacyLpcPort == 1`) 면 핸들 `\|= 1`** (`+0x152 CMP [RSP+0x40],0` · `+0x159 CMP AL,1` · `+0x194 OR [R15],1`) — 옛 LPC 포트 표시 |
| `whNtSecureConnectPort` | 없음 | 같음 + `RequiredServerSid` 포인터 그대로 (`+0x3f` → `+0x1a9`) | SQOS (`+0xb2/+0xb7`) · 뷰 안 (`+0x117` · `+0x156`) · 뷰 밖 (`+0x1ee` · `+0x229`) 실패만. 그 밖엔 `NtSecureConnectPort` (`+0x1c1/+0x1c8`) 결과 그대로 | `+0x1d1 ~ +0x1d8` 하위 4 B. `\|= 1` 표시 없음 |
| `whNtMapViewOfSection` | **있음**: `*Base == 0` 이고 `ZeroBits == 0` 이면 `ZeroBits ← Wow64pMaxAddress` (`+0xb5 CMP [RBX],0` · `+0xbb TEST RDI` · **`+0xbe CMOVZ RDI,[Wow64pMaxAddress]`** · `+0xc6`). 그 값 = `Wow64InitializeEmulatedSystemInformation +0x33/+0x3a NtQuerySystemInformation(0x3e)` 버퍼 `+0x28` (`+0x43 ~ +0x48`) | — | 네이티브 결과 그대로 (`+0x1b5/+0x1bc`). 바꾸는 곳: 성공 · TEB32 `+0x14` ≠ 0 · `RtlIsCurrentProcess` · `NtQuerySection` 성공일 때만 — 기계 불일치 (`Wow64pIsCompatibleMachineType +0x2c3/+0x2c8`) → **`0x4000000e`** (`+0x33b`), `CpuNotifyMapViewOfSection` (`+0x2f5/+0x2fa`) 실패 → `NtUnmapViewOfSection` (`+0x306/+0x30d`) 후 그 상태 | `+0x1ca/+0x1cc` `*Base` 하위 4 B (상태 무관). `ViewSize` 는 4GB 넘으면 **`0xffffffff` 로 줄임** (`+0x1d8 MOV ECX,0xffffffff` · `+0x1dd CMP` · `+0x1e0 CMOVC` · `+0x1e4`) — 자르기 아님 |

- OA 썽크의 이름 바꾸기: `RootDirectory == 0` 이고 이름이 `WindirPath` 접두가 아니면 `RedirectPath +0x7b ~ +0x92` 가 `0xc0000272` 를 주고, OA 썽크는 그걸 무시하고 성공 (`+0x352 TEST` · `+0x36f MOV EAX,[RSP+0x30]`) → `\Sessions\1\Windows\SharedSection` 같은 이름은 안 바뀐다 (`0xc0000272` 이름 뜻 `[공백]`).
- **4GB 정리**: 포트 뷰를 4GB 아래로 요구하는 줄은 없다 (넘으면 조용히 잘림). 요구가 있는 곳은 `whNtMapViewOfSection` 의 `ZeroBits ← Wow64pMaxAddress` 하나. **64비트 ntdll 이 직접 부르는 CSR 의 `NtMapViewOfSection` 은 `ZeroBits = 0` 이라 이 요구 없이 커널 `HighestUserAddress` 에만 기댄다.**

### csrsrv — WoW64 클라이언트를 따로 다루는 줄 (되면) [정적]

- **없다** (읽은 세 함수 안): `CsrApiHandleConnectionRequest` (586 B — 메시지 `+0x28` C · `+0x30` T · `+0x38` PID 를 쓰고 `NtAlpcAcceptConnectPort`) · `CsrpHandleInlineUserConnect` (197 B — 서버 번호 1 · 상태 0 이면 `CsrLoadedServerDll` 의 connect 루틴을 `(프로세스, 메시지+0x50, &8)` 로) · `CsrProcessLazyRegister` (1089 B — `NtAlpcOpenSenderProcess` 뒤 `NtQueryInformationProcess` 둘: `0x18` 세션 · `0x25` 이미지 정보, 세션 같고 `SubSystemType − 2 ≤ 1` 일 때만 등록).
- `ProcessWow64Information (0x1a)` 질문 · 기계 `0x14c` 비교 둘 다 없다. csrsrv `_index.tsv` 129 함수 이름에 `wow` · `32` 없음 (결과서). 64 / 32비트 클라이언트가 같은 길.
- basesrv `BaseClientConnectRoutine`: 크기 8 아니면 `0xc000000d` (`+0x1e`), 8 이면 qword 를 `*[CSR프로세스 +0x90]` 에 (`+0x6 ~ +0x10`), 반환 0 (`+0x13`). WoW64 면 그 qword = 0 확장 `CtrlRoutine32` — 그래서 kernelbase32 가 일부러 8 B 를 만든다.
- 공유 섹션 (`CsrSrvCreateSharedSection +0x17e ~ +0x22c`, run71) 은 64비트 csrss 뷰라 높은 주소 **C** → WoW64 클라이언트는 **C ≠ R** 이 정상 (64비트 사진도 `R = 0x7ff4ecf70000` ≠ `C = 0x7ff5575f0000`, run71).

### 호출 관계

```
kernelbase32!entry (DllMain, 이유 1)                                                         [정적]
 ├─ (1) +0x1b   _KernelBaseBaseDllInitialize@12 → +0x2a __KernelBaseBaseDllInitialize@12
 │               ├─ +0x11d  KernelBaseDllInitializeMemoryManager   (+0x2d NtQuerySystemInformation(0x32))
 │               ├─ +0x1ba  NtQuerySystemInformation(0, 0x2c) ⇢ wow64 whNtQuerySystemInformation → …SystemBasicInformation ⇒ NtQuerySystemInformation(0x3e)   F1
 │               ├─ +0x20c  ntdll32!CsrClientConnectToServer(dir, 1, &{CtrlRoutine,0}, 8, &b)                                                              F2
 │               │           └─ +0x1b  NtWow64CsrClientConnectToServer (0x1e9) ⇢ wow64cpu ⇢ Wow64SystemServiceEx
 │               │                      └─ wow64!whNtWow64CsrClientConnectToServer
 │               │                          ├─ +0x23  ntdll!CsrClientConnectToServer (64비트, import)
 │               │                          │          └─ +0x118  CsrpConnectToServer ⇒ NtOpenSection · NtCreateSection · NtConnectPort · NtClose · NtMapViewOfSection · NtClose   (run68)
 │               │                          └─ +0x63 · +0x7b · +0x8e  PEB32 +0x4c · +0x54 · +0x248 ← PEB64 +0x88 · +0x98 · +0x380   (상태 안 봄)
 │               │           +0x28  _CsrClientProcess ← 1
 │               │  +0x248  BaseStaticServerData ← [[PEB32+0x54]+8] − [PEB32+0x248] + [PEB32+0x4c]
 │               ├─ +0x3ba  BaseNlsDllInitialize → +0x20 NlsProcessInitialize                                                                                F3
 │               ├─ +0x3d8  RtlInitializeCriticalSection                                                                                                     F4
 │               ├─ +0x3f0  ConsoleInitialize                                                                                                                F5
 │               └─ +0x4cf  RegKrnInitialize
 ├─ (2) +0x87   ARI::Globals::Initialize   (비패키지 → 0)                                                                                                    E2
 └─ (3) +0x111  PcwDllMain → +0x28 NtQueryInformationProcess(−1, 0x1a)                                                                                      E3
(FALSE 면) ntdll32!_LdrpInitializeNode +0x189 → +0x247 0xc0000142 → __LdrpInitialize +0x2ab LdrpInitializationFailure → +0x64 NtRaiseHardError(0xc0000145)
```

- (나무 오프셋은 ret 자리. `⇢` 는 간접 · syscall 다리, `⇒` 는 syscall → 커널. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (결과서 "PE_emulator 쪽 — 지금 맞나 · 틀리나")

flow-writer 는 원문 374 와, 결과서가 적은 PE_emulator 줄 번호가 있는지만 봤다 (읽기만, 위 "방법"). 고칠지 · 어느 길로 갈지는 사용자 · PE_emulator 결정.

### 원문 "이미 아는 것" 에서 틀린 점

1. **"(wow64 가 64비트 syscall 로 바꿈 — 로그 rip `0x7ffafb7c14a2`)" 는 틀린 그림이다** `[정적]`. wow64 가 32비트 syscall 을 64비트 syscall 로 **번역하는 게 아니라**, `whNtWow64CsrClientConnectToServer +0x1c` 가 **64비트 ntdll 의 `CsrClientConnectToServer` 를 통째로 부른다**. 그래서 로그의 `NtOpenSection` 은 64비트 ntdll `CsrpConnectToServer` 안에서 나온 진짜 64비트 syscall 이다 — rip 끝 `0x1614a2` = 64비트 ntdll `NtOpenSection` 스텁 (`0x161490`) `+0x12` `SYSCALL` (결과서, 에뮬레이터 로그 값과 맞음). `whNtOpenSection` 류는 이 길에 **안 불린다**.
2. "32비트 ntdll `CsrClientConnectToServer` → `CsrpConnectToServer`" 의 **`CsrpConnectToServer` 는 32비트에 없다**. 32비트는 syscall 하나 (`0x1e9`).
3. "internals `ms/decomp/ntdll32` 에 `Csr*` 없음" 은 모자란 게 아니라 **원래 그 정도뿐** (`CsrClientConnectToServer` 62 B + `NtWow64Csr*` 스텁).

### 지금 코드와 맞대기

| 자리 | 지금 (결과서가 읽은 줄 — flow-writer 는 줄 있음만 확인) | 판정 · 할 것 |
|---|---|---|
| 64비트 프로세스 `Src/ntdll/BaseDllInit.h:30 ~ :31` | csrss 연결 결과 (공유 섹션 표 · 블록 · PEB 세 칸) 를 엔진이 직접 씀, 뷰는 `MapCsrSharedSectionView` · 포트는 `MapCsrPortView` (syscall 없음) | **WoW64 엔 그대로 못 쓴다**: 32비트 kernelbase 는 진짜 코드로 돌고, 연결은 wow64 + **64비트 ntdll 진짜 코드** 가 하므로 **진짜 syscall 여섯이 `TeamSyscall` 로 들어온다** — 64비트 프로세스에선 한 번도 안 나온 대답들 |
| **`MapCsrSharedSectionView` · `MapCsrPortView`** (`Src/nt/AddressSpace.cpp:1421 ~ 1450`) | 위 끝 = 3 인자 `FindFreeRangeDown` → **`SYSTEM_MAXIMUM_USER_MODE_ADDRESS`** (`Src/nt/VirtualMemory.cpp:1450 ~ 1452`) | **위 끝을 4GB 아래로**: WoW64 프로세스에 쓰면 뷰가 4GB 위에 앉을 수 있다 → PEB32 `+0x4c` 하위 4 B 가 잘려 32비트 코드가 엉뚱한 곳을 읽는다. 위 끝을 `refProcess.qwHighestUserAddress − 1` 로 (같은 파일 `NtMapViewOfSection` 쪽 `:739` 가 이미 그렇게 함). 진짜는 커널 `HighestUserAddress` 상한 (`0x7fff0000` / LAA `0xffff0000`, run139) |
| `NtConnectPort` (`ext/syscall/syscall_layer_cpp/src/…/layer.cpp:322` · `alpc/nt_legacy_port_messages.cpp:124 ~ 195`) | 이름 조회 전 입력 확인만, "실제 연결 endpoint와 서버 응답은 만들지 않는다" (결과서 인용) | **이번에 필요한 대답이 없다** — 아래 "응답 블록" |
| `csr_user_connect_stub.cpp` | `\Sessions\<n>\Windows\ApiPort` 를 알아보지만 **USERSRV 연결 (서버 3 · `0x248` B · API 0, `NtAlpcSendWaitReceivePort`)** 만 대답 (결과서) | **이번 길이 아니다** (BASESRV, `NtConnectPort`, 서버 1, 블록 `0x30` B). 32비트 user32 연결은 별개 (결과서 다음 요청 후보 5) |

### `NtConnectPort` 응답 블록 (csrss ApiPort, 서버 1) — 써 줘야 할 칸

- 입력 블록 `0x30` B 중 **요청 칸**: `+0x20` 서버 번호 1 · `+0x28` 사용자 정보 8 B (`{CtrlRoutine32, 0}`). 길이 포인터 `*len = 0x30`.
- **응답으로 써 줄 칸**: `+0x00` **C** (csrss 쪽 공유 섹션 주소) · `+0x08` **T** (csrss 쪽 표 주소) · `+0x10` csrss PID · `+0x24` = **0** (서버 connect 루틴 성공). `+0x18` 은 0.
- **출력 구조체**: `*PortHandle` (→ 64비트 ntdll `CsrPortHandle`, rva `0x1d4e20`) · 클라이언트 `PORT_VIEW` `+0x20 ViewBase` ≠ 0 · `+0x28 ViewRemoteBase` (`ViewSize` 는 `0x10000` 그대로). 뷰는 **읽기 · 쓰기** (`RtlpCreateHeap` 이 그 안에 힙을 만듦). WoW64 포인터 칸이 아니라 64비트 코드가 쓰는 칸이라 4GB 요구는 없지만, 진짜 커널은 `HighestUserAddress` 로 자른다 → 같은 규칙으로 두는 게 안전.
- `REMOTE_PORT_VIEW` 는 받아만 두니 안 채워도 된다.

### stub 이 채울 최소 목록 (길 A — WoW64 클라이언트, 64비트 syscall 모양)

| # | syscall | 돌려줄 것 | 출력 칸 | 뷰 주소 조건 |
|---|---|---|---|---|
| 1 | `NtOpenSection(&h, 4, OA)` — 이름 `\Sessions\<세션>\Windows\SharedSection` (`OBJ_CASE_INSENSITIVE` `0x40`) | `STATUS_SUCCESS`. **`0xc000003a` 면 위 3절 길로 `0xc0000142`** | `*h` (아래 5 · 6 이 씀) | — |
| 2 | `NtCreateSection(&h2, 0xf001f, NULL, &0x10000, 4, 0x8000000, 0)` | 성공 | `*h2` 이름 없는 `0x10000` B 섹션 | — |
| 3 | `NtConnectPort(&CsrPortHandle, …\Windows\ApiPort, SQOS{0,2,1,1}, ClientView{0x30, h2, 0, 0x10000, 0, 0}, ServerView{0x18, 0, 0}, &max, 블록 0x30 B, &0x30)` | 성공 | 핸들 · `ViewBase` · `ViewRemoteBase` · 블록 `+0x00` C · `+0x08` T · `+0x10` PID · `+0x24` 0 | 포트 뷰 읽기 · 쓰기, `HighestUserAddress` 아래 |
| 4 | `NtClose(h2)` | 성공 | — | — |
| 5 | `NtMapViewOfSection(h, −1, &R, 0, 0, NULL, &크기, 2, 0x500000, 2)` | 성공 | `*R` · `*크기` | **`R` < 4GB 필수** (PEB32 `+0x4c` 4 B). 읽기 전용 |
| 6 | `NtClose(h)` | 성공 | — | — |

- **뷰 안 내용** (32비트가 읽는 것 — run71 "최소 칸 목록" 과 같음): 표 `T` 는 내 뷰에서 `R + (T − C)` 자리, `T[1]` (8 B) = `S` (BSSD 의 csrss 쪽 주소). BSSD `0xa00` B: `+0x38` word (0 가능) · `+0x0` / `+0x10` `UNICODE_STRING` (`Buffer` 0 이면 건너뜀) · **`+0x9e8` 8 B = `S`** (`Buffer` 가 0 아니면 필수) · `+0x178` 영역 (주소만, 0 으로). 32비트 하위 4 B 셈이 맞으려면 **`T − C` · `S − C` 가 4GB 안**.
- **`R == C` 로 줄이기**: 된다. 단 WoW64 에선 그러면 `C` 도 4GB 아래여야 한다 (PEB32 `+0x248` 하위 4 B 와 `R` 하위 4 B 가 같아야 옮김 0).
- **먼저 있어야 하는 칸**: TEB64 `+0x180c` = `0x2000` (wow64 `+0x35 ~ +0x4b` — 0 이면 PEB32 를 주소 0 근처에서 찾다 죽음) · TEB32 `+0x30` = PEB32 · PEB64 `+0x10 ImageBaseAddress` = 32비트 exe (서브시스템 읽음) · PEB64 `+0x30 ProcessHeap` 유효 · PEB32 `+0x1d4 SessionId` (디렉터리 이름 → syscall 1 ~ 3 의 이름).
- **그다음 32비트 쪽이 바라는 것**: `NtQuerySystemInformation(0x32, 4 B)` · `(0 → 0x3e, 0x2c B)` (지금 통과 — 에뮬레이터 로그), NLS `NtGetNlsSectionPtr` (`_SetupMainNlsFiles@4 +0x3b/+0x41`) · `NtInitializeNlsFiles` (`+0x76/+0x7c`), 콘솔 (`ConsoleCreateConnectionObject` 등), `NtQueryInformationProcess(−1, 0x1a)` (`PcwDllMain`). CSR 이 풀리면 NLS 에서 다시 막힐 수 있다 `[추측]`. (NLS 줄 번호는 결과서 그대로 — flow-writer 는 `_SetupMainNlsFiles@4` 를 안 열었다.)

### 길 A / 길 B — 팀 결정

- **길 A (진짜 코드 그대로)**: 위 최소 목록을 `TeamSyscall` 이 64비트 syscall 여섯으로 대답. 32비트 · wow64 · 64비트 ntdll 은 진짜 코드 그대로 돈다. 지금 로그와 같은 길. 이 문서의 표는 길 A 기준.
- **길 B (엔진이 결과만 씀)**: `NtWow64CsrClientConnectToServer` (칸 `0x1e9`) 를 `TeamSyscall` 이 가로채 **PEB64 `+0x88` · `+0x98` · `+0x380` · PEB32 `+0x4c` · `+0x54` · `+0x248` · 64비트 ntdll `Csr*` 9개** (뒤에 `NtWow64CsrClientCallServer` 등이 읽음) 를 직접 쓴다. wow64 몸을 건너뛰는 선택이라 "진짜 코드" 원칙과 어긋날 수 있다 `[추측: 팀 규칙 확인 필요]`.
- 어느 쪽인지는 **사용자 · PE_emulator 팀이 정한다** (이 문서는 고르지 않는다).

## 확인된 것

- `[정적]` 32비트 `CsrClientConnectToServer` 는 syscall 하나 (`NtWow64CsrClientConnectToServer`, 칸 `0x1e9`, `RET 0x14`) 만 내고, wow64 nt 표 칸 `0x1e9` = `whNtWow64CsrClientConnectToServer` (rva `0x23200`) — flow-writer 가 `sdwhnt32` 를 PE 로 다시 읽음.
- `[정적]` `whNtWow64CsrClientConnectToServer +0x1c` 의 호출 대상 import 칸 `0x3c5f8` = `ntdll.dll!CsrClientConnectToServer` (PE import 표).
- `[정적]` 그 뒤 PEB32 `+0x4c` · `+0x54` · `+0x248` 쓰기 (`+0x63` · `+0x7b` · `+0x8e`) 는 상태 검사 없이 지나간다.
- `[정적]` 32비트 `__KernelBaseBaseDllInitialize@12 +0x22d ~ +0x248` 의 BSSD 셈과 FALSE 가지 다섯 (`+0x1bc` · `+0x20e` · `+0x3bc` · `+0x3da` · `+0x3f4` → `+0x4d1`), `entry` 둘, `PcwDllMain` 하나.
- `[정적]` `0xc000003a` → F2 → `_LdrpInitializeNode@4 +0x247` `0xc0000142` → `_LdrpInitializationFailure@4 +0x5a/+0x5f` `0xc0000145` 사슬.
- `[정적]` wow64 썽크 넷의 변환 칸 · status 줄 (위 표). 4GB 요구는 `whNtMapViewOfSection +0xbe` 하나.
- `[정적]` csrsrv 세 함수에 WoW64 갈래 없음.
- `[관찰]` test.exe 는 이 길을 안 탄다 — `WoW64Process : (null)` (`ms/logs/04_baggage-read_P1a.txt:160` · `_P2.txt:160`).

## flow-writer 대조

결과서 오프셋을 `.asm` 과 PE 로 맞춰 봤다. **결론 (syscall 하나 · 64비트 함수 통째 호출 · PEB32 세 칸 · F2 사슬) 을 바꾸는 어긋남은 없다.** 점프 목적지는 결과서가 모두 함수 기준 `+0x…` 로 적었고, flow-writer 가 "목적지 주소 − 함수 시작" 으로 다시 뺀 값과 다 같았다 (`+0x4d1` · `+0x46a` · `+0xd9` · `+0xca` · `+0x38` · `+0x113` · `+0x92` · `+0x39d` · `+0x54` · `+0x16f` · `+0x168` · `+0x60` · `+0x8c` · `+0xb5` · `+0x79` · `+0x162` · `+0x64` · `+0x5b`) — run151 같은 주소 끝자리 섞임은 **없다**. 줄 표기만 고친 곳:

1. **`Wow64InitializeEmulatedSystemInformation` 의 `NtQuerySystemInformation(0x3e)`** — 결과서 "`+0x2f/+0x33`" → call **`+0x33`**, ret **`+0x3a`**. `+0x2f` 는 클래스를 넣는 `LEA ECX,[R9+0x3e]`.
2. **F5 줄** — 결과서 3-1 표 k · 3-3 F5 "`+0x3f2 MOV BL,AL` → `+0x3f4 JZ`" → `.asm` 은 **`+0x3f0 MOV BL,AL`** · `+0x3f2 TEST BL,BL` · `+0x3f4 JZ +0x4d1`.
3. **ntdll32 `CsrClientConnectToServer` 인자** — 결과서 "`+0x6 ~ +0x13` 인자 5개를 그대로 다시 push" → 다시 push 하는 건 **인자 1 ~ 4** (`[EBP+0x8 ~ +0x14]`) 넷이고, 다섯째 자리엔 **자기 지역 바이트 주소** (`+0x6 LEA EAX,[EBP-1]` · `+0x9 PUSH EAX`) 를 넣는다. 호출자 다섯째 인자 (`[EBP+0x18]`) 는 syscall 뒤 `+0x2f ~ +0x36` 에서 따로 채운다. 그래서 wow64 `[RCX+0x10]` 은 ntdll32 지역 바이트를 가리킨다. 결론 (출력 바이트가 `_CsrClientProcess` 를 정함) 은 같다.
4. **TEB64 `+0x180c` 가 0 일 때** — 결과서 "0 이면 PEB32 를 못 찾음" → 더 정확히는 `+0x3e TEST EDX` · `+0x40 JZ +0x4b` 로 `RAX = 0` 인 채 `+0x4b MOV EDX,[RAX+0x30]` (주소 `0x30` 읽기 → 접근 위반), **음수** 면 `+0x45 JS +0x4b` 로 `RAX = TEB64` 그대로 (TEB64 를 TEB32 로 읽음).
5. **`whNtConnectPort` status 줄 보탬** — 결과서 2-2 는 밖 변환 실패로 `PortViewOut` 만 적었다. `.asm` 엔 **`RemoteViewOut +0x1e5/+0x1ea` → `+0x1f2 JNS` 아니면 `_local_unwind`** 도 있다 (위 표에 넣음). `+0x17e JNS` 의 목적지는 `+0x12f`.
6. **`_LdrpInitializeNode@4 +0x21e` · `+0x247`** — 함수 몸이 아니라 점프로 가는 조각 `FUN_4b2b857e` (rva `0x3857e`) 안 줄이다 (조각 `+0x15` · `+0x3e`, `.asm` 주석이 함수 기준으로 바꿔 줌). 값은 결과서 그대로.
7. **확인만 (결과서 맞음)**: wow64 nt 표 여섯 칸 · import 세 칸 (PE), kernelbase32 PDB 디버그 디렉터리, `CtrlRoutine` rva `0x2408b0` (`_index.tsv`), `[EBP+0xfffffde4]` = `EBP-0x21c` · `[EBP+0xfffffdfc]` = `EBP-0x204` · `[EBP+0xfffffdfb]` = `EBP-0x205`, csrsrv `NtQueryInformationProcess` 클래스 `0x18` · `0x25` (`LEA EDX,[R9+0x14]` · `[R9−0x1b]`), basesrv `+0x0 ~ +0x1e`, `__KernelBaseBaseDllInitialize@12 +0x4e0 RET 4`.
8. **에뮬레이터 값은 `[관찰]` 아님**: 결과서의 "`[관찰 ↔ 정적]`" (rip `0x7ffafb7c14a2`) · "`[정적 순서 ↔ 관찰]`" (seq 647 → 648) · "원문 로그는 648 까지 갔으니 있음 `[관찰]`" 은 **요청 쪽 에뮬레이터 로그** 와 맞춘 것이다. 이 문서는 그 값들을 "에뮬레이터 로그와 맞음" 으로만 쓰고 `[관찰]` 태그는 안 붙였다.

## 정정

- **run138** (`runs/03_sprint/run138-wow64-dlls.md`) — 한 곳 좁힘. 원문은 두고 문서 · 해설서 맨 위에 "고쳐 읽기 (run152)" 한 줄씩 넣었다 (그래서 run138 문서 아래 줄 번호가 2 씩 밀림).
  - 틀린 (넓은) 말: 3-3 표 "부르기" 줄 (원래 `:272`, 지금 `:274`) 과 나무 (원래 `:320`, 지금 `:322`) 의 "`whNt*` 는 … **64비트 ntdll `Nt*` 를 import 해서 부른다**" · "`whNt*` → `ntdll!Nt*` ⇒ syscall" — 모든 `whNt*` 가 64비트 `Nt*` 하나로 가는 것처럼 읽힌다.
  - 고쳐 읽기: "대부분은 그렇다. 하지만 **`whNtWow64CsrClientConnectToServer` (칸 `0x1e9`) 는 `+0x1c` 에서 64비트 ntdll 의 `CsrClientConnectToServer` (Nt 가 아닌 함수) 를 import 로 불러, 그 안의 `CsrpConnectToServer` 가 syscall 여섯을 낸다**. `whNtContinue` → `Wow64NtContinue` (run140) 처럼 `Nt*` 를 바로 안 부르는 썽크도 있다."
  - flow.md 3장 run138 절의 같은 글머리 ("syscall 다리") 에도 `(run152)` 한 마디를 붙였다.
- **run68 · run139 · run140** — 어긋나는 줄 없음.
  - run68 의 syscall 여섯 · PEB 세 칸 · `Csr*` 9개는 이번에 **WoW64 에서도 같은 64비트 코드가 낸다** 로 넓어질 뿐 틀리지 않는다.
  - run139 "PEB32 27칸" 에 이번 세 칸이 없는 것은 맞다 (커널이 아니라 wow64 가 씀).
  - run140 "AR07 이후 — 초기화 중 syscall 모두 `whNt*` 를 거침" 은 ntdll32 `LdrpInitializeProcess` 안 목록 얘기라 그대로 맞다.

## run151 대조 (run152 쓰는 중 — 오케스트레이터 요청, explainer 가 찾은 어긋남 둘)

run151 은 이미 커밋돼 있어 **run151 문서 본문을 바로 고치고 맨 위에 "정정 · 대조 (run152 때 대조)" 한 줄** 을 넣었다. run151 해설서는 안 건드렸다 (오케스트레이터가 따로).

1. **combase `InitializeSystemSecurityDescriptors` 오프셋 세 벌 — 셋 다 맞고 뜻이 다르다** `[정적]` (`ms/disassemble/combase/InitializeSystemSecurityDescriptors.asm`):
   - `+0x2a` · `+0x62` · `+0x9a` · `+0xd0` · `+0x107` = **`LocalAlloc` call** (ret `+0x31` · `+0x69` · `+0xa1` · `+0xd7` · `+0x10e`) — run151 2절 표 0 이 쓴 것.
   - `+0x36` · `+0x6e` · `+0xa6` · `+0xdc` · `+0x113` = **전역에 넣는 줄** (`_pNoRegLaunchPermissionsSD` `0x3380e8` · `_pNoRegAccessPermissionsSD` `0x3380f8` · `_pNoRegLaunchRestrictionsSD` `0x3380d0` · `_pNoRegAccessRestrictionsSD` `0x3380c8` · `_pDefaultAccessRestrictionsSD` `0x3380e0`) — 결과서 148 2절 표 0 이 쓴 것.
   - `+0x4f` · `+0x8c` · `+0xc2` · `+0xf4` · `+0x12b` = **`memcpy` call** (ret `+0x54` · …) — 결과서 148 1-1 끝 "LocalAlloc 후 memcpy" 가 쓴 것.
   - 그 밖: 크기 전역 `_dwNoRegAccessPermissionsSDSize` ← `+0x5c`, 할당 NULL 이면 `+0x40` 등 `JZ +0x14e` → `0x8007000e`, 다섯째 건너뛰기 `+0xfc CMP [RDI+0x10],0` · `+0xff JZ +0x130`. run151 표 0 줄에 셋을 나눠 적었다.
2. **`+0xd8` 은 두 구조체의 다른 칸** `[정적]` (`CallTheResolver.asm` · `RegisterOXIDAndOIDs.asm`):
   - `CallTheResolver` 의 `RDI` = **`CComApartment`** (`this` — `+0x75 MOV RCX,RDI` · `+0x78 EnsureLinkToPrimaryOxid@CComApartment`).
   - **`[CComApartment+0xd8]` DWORD = 더할 OID 수 (`cOidsToAdd`)**: `+0x33 MOV R15D,[RCX+0xd8]` → 배열 `[+0xe0]` 을 `+0x4f memcpy` 로 지역 `[RBP]` 에 (`R15 × 8` B) → `+0x54 AND [RDI+0xd8],0` → `+0xfc MOV [RSP+0x28],R15D` (프로시저 7 인자).
   - **`[OXIDEntry+0xd8]` BYTE = 등록 표시**: `+0x87 RCX = [RDI+0x10]` (아파트의 `OXIDEntry`) → `+0x94 MOV AL,[RCX+0xd8]` · `+0x9d JZ +0x1ad`. 같은 `RCX` 로 `+0x1b7 OXIDEntry::RegisterOXIDAndOIDs` (PDB 이름 `?RegisterOXIDAndOIDs@OXIDEntry@@…` — `this` = `OXIDEntry`) 가 `+0x93` OXID → `[+0xc8]` · `+0xa7` MID → `[+0xd0]` · **`+0xb1 XCHG [+0xd8],1`**. primary `OXIDEntry` (`+0xb7` atomic `load`) 의 그 칸이 0 이면 거기에도 같은 셋 (`+0x12d` · `+0x13b` · `+0x142`).
   - 그래서 `CallTheResolver +0x94` 와 `RegisterOXIDAndOIDs +0xb1` 은 **같은 칸** (`OXIDEntry+0xd8`), 프로시저 7 의 `cOidsToAdd` 만 **다른 칸** (`CComApartment+0xd8`). "`RegisterOXIDAndOIDs` 의 `[아파트+0xd8] = 1`" 로 읽으면 틀린다. `CallTheResolver +0xa3` 의 `OXID*` 인자도 `[OXIDEntry+0xc8]` (= `+0x93` 이 쓴 칸) 이라 앞뒤가 맞는다.
   - run151 4-1 표 "언제" · "입력" · 6 쪽 "combase 검사" 줄 (`+0x87 ~ +0xae` → `+0x87 ~ +0xb1`), 요약 글머리 · "확인된 것" · 나무의 `[..+0xd8]` 을 이 이름으로 고쳤다. 결론 (6 먼저 · 첫 때 그 칸이 0 인 것은 `[추측]`) 은 그대로.
   - (덤, run151 은 안 고침) 결과서 148 4-1 의 "입력 (스택 칸)" 은 레지스터 이름을 **프로시저 인자 순서** 로 붙였다. `.asm` 의 `NdrClientCall2` 실제 자리는 `RCX` 스텁 · `RDX` 서식 · `R8` hRpc (`+0xd7`) · `R9` `_ph` (`+0xba`) · `[RSP+0x20]` `&OXID` · `[+0x28]` `cOidsToAdd` · `[+0x30]` 배열 · `[+0x38]` 원하는 수 · `[+0x40]` 출력 OID · `[+0x48]` 출력 수. 인자 뜻 · 순서는 같다.

## 짐 지도 반영 (`baggage.md` 7.6.17 메모 두 줄만 — 셈 밖 그대로)

- **2장 숫자 · 3장 줄 수 · 7.6.17 "줄 35" 그대로.** test.exe (x64) 엔 이 길이 안 생긴다 (`[관찰]` 위).
- 7.6.17 (라) 표 아래에 두 줄:
  1. **PEB32 `+0x4c` · `+0x54` · `+0x248` 는 wow64 가 쓴다** (커널 27칸 밖) — 32비트 kernelbase 연결 (`+0x206`) → `whNtWow64CsrClientConnectToServer +0x63` · `+0x7b` · `+0x8e` (상태 안 봄). 32비트 `Csr*` 전역은 ntdll32 `_CsrClientProcess` 하나 (`+0x28`), 진짜 `Csr*` 9개 · PEB64 세 칸은 64비트 ntdll (run68 길 그대로).
  2. **CSR 공유 섹션 뷰 `R` 은 4GB 아래여야** (32비트가 하위 4 B 로 직접 읽음) — 사용자 코드엔 요구가 없고 (`ZeroBits = 0`) 커널 `HighestUserAddress` 상한 (run139 C8) 에 기댄다. (라) 표 "놓는 법" 의 "32비트 `Nt*` 스텁 → … → 64비트 `Nt*`" 에 예외 하나 (`0x1e9` 는 64비트 `CsrClientConnectToServer` 통째).

## 아직 확인 필요

동적 (결과서 "VM — 디버거 필요" — 새 번호 없이 **Q84 (WoW64 판) · Q253d · Q262** 에 얹을 꼴. x86 시험 exe 가 있어야 함 — 사용자 결정. 커널 멈춤점은 그 프로세스 조건, 32비트 모듈은 `.effmach x86` 에서 `@N` 이름일 것 `[추측: 심볼 표기]`):

1. `ntdll!CsrpConnectToServer+0x1bb` (64비트, `NtOpenSection` ret) — `r eax` · `dq @rsp+0x50 L1` · 이름 `du poi(ntdll+0x1d4e08)`. 닫는 것: 진짜 VM 에선 `0` 인가. (Q84 · Q253d)
2. `ntdll!CsrpConnectToServer+0x2e4` (`NtConnectPort` ret) — `r eax` · `dq @rbp-8 L6` (응답 블록) · `dq @rbp-0x68 L2` (`ViewBase` · `ViewRemoteBase`) · `dq ntdll+0x1d4e20 L1`. 닫는 것: 블록 값 · 포트 뷰가 4GB 아래인가. (Q84 · Q262)
3. `ntdll!CsrpConnectToServer+0x33d` (`NtMapViewOfSection` ret) — `dq @rsp+0x70 L1` (**R**) · `dq @rbp-0x40 L1`. 닫는 것: **R 이 4GB 아래인가** · R vs C. (Q262 `MiComputeProcessUserVa+0xaf` 와 같이)
4. `wow64!whNtWow64CsrClientConnectToServer+0x95` — PEB64 `+0x88` · `+0x98` · `+0x380` 과 PEB32 `+0x4c` · `+0x54` · `+0x248` (PEB32 = `dwo(@$teb + dwo(@$teb+0x180c) + 0x30)`). 닫는 것: 연결 전 0 → 이 줄에서 채워짐. (Q253d)
5. `KERNELBASE!__KernelBaseBaseDllInitialize@12+0x20c` (32비트) — `r eax`, 직전 `+0x206` 에서 `dd esp L5` · `db poi(esp) L0x40` · `dd poi(esp+8) L2`. (Q253d · Q84)
6. `KERNELBASE!__KernelBaseBaseDllInitialize@12+0x248` — `r edi` · `dd edi+0x9e8 L2` · `dd poi(@$peb32+0x54) L4`. (Q84 · Q253d)
7. `KERNELBASE!__KernelBaseBaseDllInitialize@12+0x3ba` · `+0x3f0` · `entry+0x1b` — `r al` (F3 · F5 · 안쪽 결과가 1 인가). (Q84)
8. `KERNELBASE!_PcwDllMain@12+0x28` — `r eax` (E3 의 `0x1a`). (Q253d)

디버거 없이 VM 으로 떠 올 것: **없음** (결과서 — 뷰 주소 · PID · 응답 블록 · 핸들은 모두 실행 중 값).

정적 (결과서 "다음 요청 후보" — 대기열 자리는 오케스트레이터가 정함):

1. **32비트 NLS** — `_NlsProcessInitialize@0` 아래 (`_AllocTables@0` · `_SetupMainNlsFiles@4` · `_SetupAnsiOemCodeHashNodes@0` · `_MakeLocHashNode@4`) syscall + wow64 `whNtGetNlsSectionPtr` · `whNtInitializeNlsFiles` (F3 — CSR 다음 막힘 후보). Q261 · Q266 과 닿음.
2. **32비트 콘솔** — `_ConsoleInitialize@0` 아래 (`ConsoleCreateConnectionObject` · `ConsoleAllocate` · `ConsoleCommitState`) + `whNtCreateFile` · `whNtDeviceIoControlFile` (F5).
3. wow64 `whNtQueryInformationProcess` `0x1a` · `0x1c` 갈래 + `PcwDllMain` (E3) · `whNtQuerySystemInformation_SystemRangeStartInformation` (rva `0x188dc`, 안 읽음).
4. 32비트 kernel32 `_BaseDllInitialize` · `_Kernel32LegacyLib_BaseDllInitialize` 의 BSSD 32비트 폭.
5. 32비트 user32 csrss 연결 (`CsrClientConnectToServer(…, 3, 0x248 B)` 가 WoW64 에서 무엇을 타나 — run85 · run142 와 짝).
6. (낮음) `RedirectPath` 의 `0xc0000272` 이름 · `Wow64pIsLegacyLpcPort` 이름 글.
- 그 밖 `[공백]`: 커널 legacy `NtConnectPort` → ALPC 변환 몸 · `ARI::Globals::Initialize` 안 음수 가지 · seq 648 이 `0xc000003a` 가 된 까닭 (에뮬레이터가 공유 섹션 이름을 객체 이름공간에 안 갖고 있어서일 것 `[추측]`, 팀 코드 쪽).

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에선 못 봤다 `[정적: 읽은 줄 안에서]`.
- **읽을 때 조심 (WoW64 에서 "어느 ntdll 인가")**: 같은 이름 `CsrClientConnectToServer` 가 **두 이미지에 다른 몸으로** 있다 (ntdll32 62 B 껍데기 · 64비트 ntdll 진짜). 멈춤점 · 근거를 적을 때 모듈 이름 (`ntdll32` / `ntdll`, VM 에선 `ntdll_<기준>` 류일 것 `[추측]`) 을 꼭 붙일 것.
- **`.asm` 조각 주석**: ntdll32 `_LdrpInitializeNode@4.asm` 은 점프로만 가는 조각을 `frag:FUN_… +0x..  (= 함수 +0x..)` 로 적는다 — 근거엔 괄호 안 함수 기준 값을 쓰면 된다 (이번 `+0x21e` · `+0x247`).
- 스크립트 문제 없음 (결과서). 요청서 표기 하나 (`kernelbase32.dll=…` 로 써야 PDB 이름이 `ntdll32.dll.pdb` 꼴) 는 결과서 "스크립트 문제" 절에 있다 — 다음 32비트 모듈 요청서 쓸 때 오케스트레이터가 참고할 거리.
- `pefile` 은 시스템 파이썬 사용자 사이트에 있어 `python3 -I` 로는 안 불린다 — flow-writer 는 스크래치 폴더에서 `-I` 없이 돌렸다 (MS 바이너리만 읽음, run151 과 같음).
