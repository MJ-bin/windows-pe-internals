# 50 todo-vm — 정적 분석 요청 (거르기)

- 날짜: 2026-10-31
- 스프린트: 03_sprint
- 대기열: **Q104** (PE_emulator 요청 2, 사용자 결정 2026-10-31로 Q78d4 앞). 묶인 기존 Q: Q34·Q35·Q42·Q47·Q61
- 관련: run9, run10, run15, run16, run29, run31, run32, run44, run47, run48, run50, run53, run55 / sast/03·04·26·27·44·47

## 배경

- `../PE_emulator`(C++ 에뮬레이터) 코드에 `TODO(VM)`로 남은 값 17개가 있다. 아래 표가 전부다.
- 원칙: **정적으로 이미 풀렸거나 지금 asm 으로 풀리는 것은 동적에서 뺀다.** 남은 것만 다음 dast 요청(test.exe 한 번 실행)으로 본다.
- 이번 요청은 그 **거르기 + 정적으로 풀 수 있는 것 풀기**다. 짐 지도 바퀴가 아니다.

## 알고 싶은 것

항목마다 셋 중 하나로 가르고 근거를 단다.

- **A. 이미 풀림**: 우리 문서(run·결과서)에 정적 답이 있다. → 문서·줄 + asm 경로·오프셋.
- **B. 지금 정적으로 풀림**: asm/디컴파일을 보면 답이 나온다 (test.exe 조건 — 64비트 exe, 섹션 있음, 부모 cmd, IFEO 없음, `sample/` 소스 — 을 넣으면 갈래가 정해지는 것 포함). → 이번에 풀고 근거.
- **C. 동적 필요**: 실행 때만 정해지는 값 (전역 변수 값, 콜백 표에 등록된 대상, 부팅 설정). → 멈춤점(함수+오프셋 또는 `nt+rva`)과 읽을 명령 (`dq`, `r`, `ln` 등)을 적는다. dast-mentor 가 그대로 옮길 수 있게.
  - 전역 하나만 읽으면 되는 것은 "멈춤점 없이 아무 때나 `dq nt!이름`" 으로 충분한지도 적는다 (부팅 뒤 안 바뀌는 값이면).

| # | PE_emulator 자리 | 알고 싶은 것 | 묶인 Q | 이미 아는 것 (확인해서 A/B/C 로) |
|---|---|---|---|---|
| 1 | `PspAllocateProcess` | `MmInitializeProcessAddressSpace` 를 `+0x2095`(보통) / `+0x2113`(복제) 중 어디서 타나 | Q35 | run50 289줄 `[정적]`: 지역 플래그 비트 4 = "부모 복제(섹션 없음)" (`+0x2ec ~ +0x317`, 7번째 인자 섹션이 0일 때만). test.exe 는 섹션 있음 → 보통 길. **A 후보** — 근거 줄만 다시 확인 |
| 2 | `PspAllocateProcess +0x17c4` | 간접 호출 대상 (전역 `nt+0xf04d20`) | Q35 | run50 256줄: `SeCiCallbacks+0xc0` (`+0x17a7` 에서 읽음). 대상은 CI 가 부팅 때 등록 → **C 후보**. `dqs nt!SeCiCallbacks` 류로 아무 때나 읽어도 되는지 |
| 3 | `PspAllocateProcess +0x121a` | 간접 호출 대상 (전역 `nt+0xf04d28`) + test.exe 때 타기는 하나 | Q35 | run50 255줄: `SeCiCallbacks+0xc8`, 실패·정리 블록 (`+0x1189 ~ +0x121c`) 안 `[추측]`. 보통 길에서 이 블록을 지나는지가 B 로 풀리면 좋음 |
| 4 | `PspAllocateProcess +0xffb` | test.exe 때 `PspWow64PickBestNtdll` 안 타나 | Q35 | run10 184줄 "64비트면 안 타야 함". 타는 조건을 asm 으로 → **B 후보** |
| 5 | `PspAllocateProcess +0x2364` | test.exe 때 `PspSetProcessAffinitySafe` 안 타나 | Q35 (새로 더함) | run49 90줄에 호출 줄만. 조건을 asm 으로 → B 또는 C |
| 6 | `PspAllocateProcess +0x27a0` | test.exe 때 `KeSecureProcess` 안 타나 | Q35 (새로 더함) | run49 90줄, run45 일 23. 조건을 asm 으로 → B 또는 C (보안 프로세스 = VBS 신뢰 앱 조건이면 B) |
| 7 | PEB `+0x20` | `PspCopyAndFixupParameters` 가 `+0x52b` / `+0x34c RtlWriteULong64ToUser` 중 어느 쪽 | Q35 | run32 114줄: 스위치 = 전역 `Feature_CreateProcessSegmentHeapEnablement__private_featureState` (rva `0xfc4110`) 비트 4·0, 실행 때 값 → **C 후보** (`dd nt+0xfc4110 L1`, 아무 때나 읽어도 되는지) |
| 8 | `MiRelocateImage` | 새 기본 주소일 때 `TransferAddress` 델타를 어디서 더하나: 조각 `+0x1b1279` / `PspAllocateProcess +0x20b3 ~ +0x20ce` | Q61·Q35 | run31 105·111줄: 조각 `+0x1b1279` `ADD [R15],RAX` `[정적]`, `+0x20b3 ~ +0x20ce` 는 "④ 반영 값이 실제 주소와 다를 때" `[추측]` (`PspAllocateProcess` 안 읽음). **`+0x20b3 ~ +0x20ce` 를 읽어 B 로** — 두 곳이 각각 무슨 경우에 더하나 (둘 다? 하나만?) |
| 9 | `PspSystemDlls` | `PspSystemDlls[0]+0x20` = 실제 ntdll 베이스? | Q42 | run48 188줄: `+0x172` 가 "`+0x20` == 올라간 주소" 요구 `[정적]` + run44 첫 트랩 프레임 `Rdx` = ntdll 실제 베이스 `[관찰]` + `Rdx` ← `PspSystemDlls[0]+0x20` (run16 `[정적]`). **A 후보** (관찰+정적 사슬로 닫히는지 판단. 직접 `dq` 로 본 적은 없음) |
| 10 | 첫 `IRETQ` | `KiKvaShadow` 갈래: `KxStartUserThread` 조각 `IRETQ` / `KiKernelExit +0xa5` | Q42 | run29 72·73·142줄 (`NtContinueEx` 쪽 갈래만), run15. 갈래 조건은 정적, 값은 전역 → **C 후보** (`db nt!KiKvaShadow L1`, 아무 때나) |
| 11 | `LdrInitializeThunk` | 첫 명령에서 `r fpcw` = `0x27f` / `0x37f` (맡긴 문맥 쪽 `0x27f` 는 run44 관찰) | Q42 (새로 더함) | 유저로 넘어갈 때 x87 제어 값을 누가 정하나 (`KiInitializeUserApc`·`KxStartUserThread` 쪽에서 `FNINIT`/`FLDCW`/`XRSTOR` 류가 있나) 를 asm 으로 볼 수 있으면 B, 아니면 **C** (`bp ntdll!LdrInitializeThunk` + `r fpcw`) |
| 12 | PEB `+0x40c LoaderThreads` | `PsDefaultLoaderThreads` 값 | Q47 | run32 191·243줄: 0 이면 이 전역으로 채움 `[정적]`. run44 196줄: 사진에서 `LoaderThreads` 0 → 전역도 0 `[추측]`. 전역이 쓰이는 곳(부팅 때 레지스트리에서 읽나)을 보면 B 일 수도. 아니면 **C** (`dd nt!PsDefaultLoaderThreads L1`) |
| 13 | `NtCreateUserProcess +0x5b2` | 간접 호출 대상 (전역 `nt+0xfd83d0`) | Q34 | run9 33·136줄: 대상은 실행해 봐야 앎. 전역 이름 (`pdb_aliases.py --where ntoskrnl` 로 `nt+0xfd83d0` 이름) 과 등록 주체를 알면 좋음 → **C 후보** |
| 14 | `NtCreateUserProcess +0x11bb` | 간접 호출 대상 (전역 `nt+0xf04d30`) | Q34 | `0xf04d30` 은 2·3번 (`0xf04d20`·`0xf04d28`) 바로 옆 → `SeCiCallbacks+0xd0` 인가? 확인. → **C 후보** |
| 15 | `NtCreateUserProcess` | test.exe 때 `PspCreateUserContext` 길 `+0xd22` / `PspGetContextThreadInternal` 길 `+0xd3e` | Q34 | run16 87줄: `+0xce0` 에서 섹션 참조가 0 이면 `+0xd3e`. test.exe 는 섹션 있음 (run50) → **A/B 후보** — 섹션 참조가 무엇이고 test.exe 때 0 이 아닌지 정적으로 닫히나 |
| 16 | `PspSetContext +0x2aa` | `KiMxCsrMask` 값 (`0x1f80 & 마스크` = `0x1f80` 그대로?) | 새 (Q104 d) | sast/47 101줄: `KxContextToKframes` 쪽 `& KiMxCsrMask` 는 안 탐. **`PspSetContext +0x2aa` 는 다른 자리** — 이 줄을 test.exe 때 타나 (B), 타면 마스크 값이 필요 → 전역이 부팅 때 어떻게 정해지나 (CPUID `FXSAVE` 의 `MXCSR_MASK`?) 를 보면 B 로 "0x1f80 는 언제나 남음" 이 되는지. 아니면 **C** (`dd nt!KiMxCsrMask L1`) |
| 17 | `MiCreateImageFileMap +0x188` | MZ 검사 실패 때 돌려주는 진짜 상태값 (디컴파일이 void 로 풀려 안 보임) | 새 (Q104 e) | run31 37줄에 `+0x17f ~ +0x188` 확인 기록. **B 후보** — asm 에서 실패 갈래가 `eax` 에 넣는 NTSTATUS (예: `0xC000007B`? 추측 금지, asm 값 그대로) |

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^PspAllocateProcess$` | 0 | 1·3·4·5·6·8 — 이미 있음. 읽기만 (`+0x2ec ~ +0x317`, `+0xf?? ~ +0xffb`, `+0x1189 ~ +0x121c`, `+0x2340 ~ +0x2364`, `+0x2780 ~ +0x27a0`, `+0x20b3 ~ +0x20ce`) |
| ntoskrnl | `^NtCreateUserProcess$` | 0 | 13·14·15 — 이미 있음. 읽기만 (`+0x590 ~ +0x5b2`, `+0x1190 ~ +0x11bb`, `+0xce0 ~ +0xd3e`) |
| ntoskrnl | `^PspSetContext$` | 0 | 16 — 이미 있음. `+0x2aa` 앞뒤 |
| ntoskrnl | `^MiCreateImageFileMap$` | 0 | 17 — 이미 있음. `+0x17f ~ +0x188` 와 실패 갈래 끝 |
| ntoskrnl | `KiMxCsrMask`·`PsDefaultLoaderThreads`·`KiKvaShadow` 를 **쓰는** 함수 | 0 | 10·12·16 — 부팅 때 값이 정해지는 곳 (`_refs` 나 `pdb_aliases.py --where` 로 찾기). 쓰는 줄만 보고 함수 통째 분석은 하지 않음 |
| ntoskrnl | 11번 x87 제어 값 (`KxStartUserThread`·`KiInitializeUserApc`·`KiStartUserThread` 중 FPU 상태 넣는 곳) | 0 | 11 — 이미 있는 파일이면 읽기만 |
| ntoskrnl | `nt+0xfd83d0`, `nt+0xf04d30` 전역 이름 | — | 13·14 — `pdb_aliases.py`, `pdb_struct.py` (`SeCiCallbacks` 가 구조체면 `+0xc0`·`+0xc8`·`+0xd0` 칸 이름) |

## 지도에서 본 것

- `PspAllocateProcess`, `NtCreateUserProcess`, `PspSetContext`, `MiCreateImageFileMap`, `KeSecureProcess`, `PspSetProcessAffinitySafe`, `PspWow64PickBestNtdll` 는 `ms/disassemble/ntoskrnl/` 에 `.asm` 이 이미 있다. 새로 뽑을 필요가 있으면 depth 0 만.

## 이미 아는 것

- 위 표 "이미 아는 것" 열. 태그는 그 문서 것을 그대로 따른다.
- `[소스]` test.exe 는 x64, `sample/` (exe TLS 콜백, dll1 → dll2 import, CreateThread 1개). IFEO 키 없음 (run50).
- `[관찰]` run44 사진 (K1 ~ P2): 맡긴 문맥 `FltSave.ControlWord` `0x27f`, `LoaderThreads` 0, 첫 트랩 프레임 `Rdx` = ntdll 실제 베이스.

## 받고 싶은 것

- 맨 위에 **17줄 표**: `#` / A·B·C / 한 줄 답 (B 면 값·갈래) / 근거 (`[정적] sast50` + asm 경로+오프셋, 또는 기존 run) / C 면 멈춤점·명령.
  - PE_emulator 가 그대로 옮겨 쓸 수 있게 오프셋과 태그를 꼭 붙인다.
- C 항목들은 **test.exe 한 번 실행**에 다 보게 묶은 추천 순서 (멈춤점 시간 순 + "아무 때나 읽는 전역" 목록 따로).
- 범위 밖 (하지 말 것): Q34·Q35·Q42·Q47·Q61 의 PE_emulator 몫이 아닌 나머지 질문, 짐 지도 칸 셈, 함수 통째 분석.
