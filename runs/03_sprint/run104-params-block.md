# 2026-11-16 (작성일) run104 — 매개변수 블록 · CFG 비트맵 칸 · ScpCfg 전역 · 표준 핸들 상태 (Q63a = Q63 (a)·(b)·(c)·(e)·(f), 커널 쪽, 정적만): **`Flags` 마스크 `0xf4010e11` 은 "켜져 있으면 거절하는 비트" 목록이다** (`(Flags ^ 1) & 마스크 ≠ 0 → 0xC000000D`) `[정적]`. 비트 0 만 켜져 있어야 하고, 4·9·10·11·16·26·28·29·30·31 은 꺼져 있어야 한다. PDB 에 비트 이름은 없다. **Environment 는 따로 할당하지 않고 블록 바로 뒤 (`블록 + Length`) 에 붙인다** — 한 번의 `ExAllocatePool2(0x41, Length + EnvironmentSize, 'PsPb')`. **작업 집합 구조 `+0x4a0`/`+0x4a8` (CFG 비트맵 시작·크기) 은 `MiCfgInitializeProcess` 가 아니라 그 아래 `MiReferenceCfgVad` 가 쓴다** — 그리고 CFG 가 꺼진 프로세스면 `MiCfgInitializeProcess +0x1e` 에서 아무것도 안 쓰고 끝난다. `PspNtdllScpFunctions` 넷에는 ntdll 함수 주소가 아니라 **"ntdll 끝 바로 뒤 쪽" 안의 주소** 가 들어간다. 만들기 정보 `+0x11` 은 **비트 2~3 = 표준 핸들 상태 (0 · 1 · 2, 3 은 거부)**, **비트 7 = `PsAttributeChpe`**. `Flags` 비트 31 을 켜는 커널 줄은 못 찾았다. `PspSystemDlls[0]+0x20` = `PspNativeSystemDllData+0x20` (같은 칸).

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 값 대조는 run44 사진 덤프 읽기 로그를 다시 읽은 것뿐이다 (아래 "사진 로그 대조").
- 정적: **ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1`** (기준과 같음). 다시 analyze 안 함, `export` 두 번 (결과서).
  - 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 그대로 (`_calls.tsv`).
- 질문 4 (비트 31 전수) 는 결과서가 스크래치 `objdump -d` 풀이 (`nt.dis`, 3,086,164줄, 커밋 안 함) 를 꼴로 훑은 것이다. "이 꼴들에는 없다" 이지 "전혀 없다" 가 아니다.
- 다른 decompiler 가 같은 시간에 ntoskrnl (sast/110) · ntdll (sast/111) 을 돌리는 중이다. **sast/99 · 100 · 102 · 105 ~ 111 결과는 이 문서에 안 넣었다.** ntdll 쪽 (Q63 (d)) 은 **Q63b 로 떼어 사용자 결정 대기**라 이번에 안 했다.

## 목적

- **Q63a** (= Q63 (a)·(b)·(c)·(e)·(f), run32 의 "정적으로 남은 것". Q63 (d) 는 Q63b):
  - (a) `PspCaptureUserProcessParameters` 의 `Flags` 마스크 `0xf4010e11` 비트마다 뜻, Environment 를 블록 어디에 두나.
  - (b) `MiCfgInitializeProcess` 가 작업 집합 구조 `+0x4a0`/`+0x4a8` (시작 정보 블록 `+0xb8`/`+0xc0` 출처) 을 쓰나. `PspLocateNtdllAddressesForScpCfg` 가 `PspNtdllScpFunctions` 에 넣는 것.
  - (c) `PspBuildCreateProcessContext` 가 만들기 정보 `+0x11` 을 채우는 줄과 값 뜻.
  - (e) `ProcessParameters.Flags` 비트 31 (ntdll `LdrpIsSecureProcess` 로 가는 비트, run17) 을 누가 켜나.
  - (f) `PackageDependencyData` · `CurrentDirectores` 를 왜 안 고치나, 스위치 이름 "SegmentHeap" · "SCP", `Rdx` 출처 이름 두 개가 같은 칸인가.
- 앞 바퀴 짝: run32 (`runs/02_sprint/run32-params-initblock.md` — 매개변수 블록 · 시작 정보 블록), run16 (`runs/02_sprint/run16-thunk-context.md` — `LdrInitializeThunk` 로 가는 `Rdx`), run63 (ntdll 확장 쪽 · ScpCfg 쪽), run50 (`Flags` 비트 14 조건).

## 방법

- 정적: `_workspace/sast/104_params-block.md` — ntoskrnl
  - 새로 뽑은 것 3개 (depth 0): `^MiCfgInitializeProcess$`, 그리고 질문 2 의 쓰기가 한 단계 아래라서 `^(MiReferenceCfgVad|MiMapSecurePureReserveView)$`.
  - 이미 있던 `.asm` · `.c` 를 읽기만: `PspCaptureUserProcessParameters` · `PspCaptureProcessParameters` · `PspCopyAndFixupParameters` · `PspSetupUserProcessAddressSpace` · `PspBuildCreateProcessContext` · `PspLocateNtdllAddressesForScpCfg` · `PsInitializeScpCfgPages` · `PspCopyNtdllExport` · `MmGetScpCfgFunctionOffset` · `PspGetScpCfgFunctions` · `PspPrepareSystemDllInitBlock` · `PspCaptureCreateInfo` · `MmInitializeProcessAddressSpace` · `MiMapProcessExecutable` · `PsLocateSystemDlls` · `PspInitializeThunkContext`.
  - PDB: `_RTL_USER_PROCESS_PARAMETERS` · `_PS_ATTRIBUTE_NUM` · `_PS_STD_HANDLE_STATE` · `_MI_SECTION_STATE` · `_MI_PROCESS_STATE` · `_ESERVERSILO_GLOBALS` 타입, `PspSystemDlls` · `PspNativeSystemDllData` 주소. 커널 `INIT` 글 `RtlpScpCfgNtdllExports` (`objdump -s`), ntdll 표 (ntdll PDB 이름).
- 결과서: `_workspace/sast/104_params-block_result.md` (**분석성공** — 비트 4·9·10·11·16·26·28·30·31 이름은 `[공백]` (ntdll 이 있어야 앎), 비트 31 은 "이 꼴 검색에서는 켜는 줄 없음" 까지, (f-1) 의 "왜" 는 `[추측]`).
- flow-writer 가 다시 본 것:
  - `.asm`: `PspCaptureUserProcessParameters` (`+0x29` · `+0x2c` · `+0x8f` · `+0x92` · `+0xa0` · `+0xa3` · `+0xa8` · `+0xba` · `+0xf3` · `+0x317/+0x31c` · `+0x335` · `+0x3a1` · `+0x40e` · `+0x411` · `+0x415` · `+0x418` · `+0x528 ~ +0x558` · `+0x576 ~ +0x588` · `+0x5a2 ~ +0x5f4` · `+0x625` · `+0x683` · `+0x780` · `+0x7d6 ~ +0x7f5` · `+0x809 ~ +0x84d` · 조각 `+0x13bcd0 ~ +0x13bd24`), `MiCfgInitializeProcess` (`+0x13` · `+0x1c` · `+0x1e` · `+0x76` · `+0x98` · `+0x9d` · `+0x119` · `+0x131` · `+0x156` · `+0x179`), `MiReferenceCfgVad` (`+0x16` · `+0x33` · `+0x37` · `+0x3b` · `+0x71` · `+0x77` · `+0x7e` · `+0x82`), `PspLocateNtdllAddressesForScpCfg` (`+0x77` · `+0x83` · `+0x8d` · `+0xf0` · `+0x101` · `+0x1af` · `+0x1b4` · `+0x1bf` · `+0x1d6` · `+0x1ec` · `+0x202`), `PspBuildCreateProcessContext` (`+0x2d1` · `+0x2e2` · `+0x2ed` · `+0x309` · `+0x318` · `+0x31c` · `+0x174b` · `+0x1754` · `+0x1760`), `PspCopyAndFixupParameters` (`+0x36` · `+0x39` · `+0x74` · `+0x7b` · `+0x82` · `+0x19d` · `+0x1a4` · `+0x3c7` · `+0x3eb` · `+0x3f8` · `+0x402/+0x407` · `+0x41f` · `+0x52b`), `PspSetupUserProcessAddressSpace` (`+0x41` · `+0xa0` · `+0xa6` · `+0xb1` · `+0xe6` · `+0xea` · `+0x115` · `+0x11f` · `+0x124` · `+0x15c` · `+0x15f` · `+0x161` · `+0x16b` · `+0x1a6`), `PspInitializeThunkContext` (`+0x196` · `+0x19d` · `+0x1c0`), `PsInitializeScpCfgPages` (`+0x77` · `+0x14d`).
  - `_calls.tsv` (ntoskrnl): `MiMapProcessExecutable +0xe3/+0xe8 MiCfgInitializeProcess` (run32 "지도상" 의 줄), `MiCfgInitializeProcess` 5줄, `MiReferenceCfgVad` 2줄, `PspLocateNtdllAddressesForScpCfg` 10줄, `PspCaptureUserProcessParameters` 27줄, `MmInitializeImageViewExtensionCfg +0x288/+0x28d PsInitializeScpCfgPages`, `MmInitializeProcessAddressSpace +0x4a5/+0x4aa` · `+0x4ba/+0x4bf MiReferenceCfgVad`.
  - `_refs/PspNtdllScpFunctions.tsv`: 쓰기 4줄 (`+0x0` ~ `+0x18`, 모두 `PspLocateNtdllAddressesForScpCfg`), 읽기 1줄 (`PspGetScpCfgFunctions +0x92 LEA`). 이름 거리로 본 크기는 `0x38` (7칸) 인데 `+0x20` ~ `+0x30` 셋을 쓰는 줄은 `_refs` 에 없다.
  - `alias:` 넷:
    - `PspCaptureUserProcessParameters +0x583` · `+0x696` · `+0x13bd09`, `PspCopyAndFixupParameters +0x41f` — 메모리 복사 함수 (같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`).
    - `PspLocateNtdllAddressesForScpCfg +0x8d` → `PspCopyNtdllExport`, `PsInitializeScpCfgPages +0x77` → `PspLocateNtdllAddressesForScpCfg`, `+0x14d` → `PspFinalizeScpCfgPage` — 셋 다 같은 이름의 C++ 꾸민 이름 하나가 더 있을 뿐이라 (`?Psp…@@YAJ…`) 보통 이름으로 쓴다.

## 결과

비유 (관리사무소 = 커널, 관리실 = ntdll):

- 부모가 써 준 **개업 서류** (매개변수 블록) 를 관리사무소가 받을 때, 먼저 **"이 칸들엔 체크하면 안 됨" 목록** (마스크) 으로 거른다. "정리됨" 칸 (비트 0) 만 꼭 체크돼 있어야 한다.
- 통과하면 서류 사본을 **한 봉투에** 만든다: 앞에 서류 본문 (0x450 바이트), 그 뒤에 글자들 (경로 · 명령줄 …), **맨 뒤에 환경 변수 쪽지** 를 이어 붙인다. 환경용 봉투를 따로 만들지 않는다.
- CFG 출입 명단 (비트맵) 의 자리는 **명단을 쓰는 가게** (CFG 켜진 프로세스) 만 수첩에 적는다. test.exe 처럼 CFG 를 안 쓰면 그 칸은 빈칸.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0xac6` | `+0xacb` | `nt!PspCaptureProcessParameters` | 부모가 준 9번째 인자 | [정적] (run32 그대로) |
| `nt!PspCaptureProcessParameters` | `+0x3a` | `+0x3f` | `nt!PspCaptureUserProcessParameters` | 사용자 모드 호출일 때 | [정적] (run32 그대로) |
| `nt!PspCaptureUserProcessParameters` | `+0x317` | `+0x31c` | `ExAllocatePool2` | **한 번만** — `0x41`, 크기 `F + EnvironmentSize`, 꼬리표 `0x62507350` (`PsPb`) | [정적] |
| `nt!PspCaptureUserProcessParameters` | `+0x43b` ~ `+0x523` (8곳) · `+0x844` | `+0x440` · `+0x45d` · `+0x47a` · `+0x49b` · `+0x4bf` · `+0x4e3` · `+0x507` · `+0x528` · `+0x849` | `nt!PspCopyUnicodeString` | 문자열을 블록 `+0x450` 뒤에 차례로. `+0x849` 는 `HeapPartitionName` (셋째 인자 `+0x4` 비트 2 일 때) | [정적] |
| `nt!PspCaptureUserProcessParameters` | `+0x583` | `+0x588` | 메모리 복사 함수 (같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`) | **환경을 `블록 + F` 에 복사** | [정적] |
| `nt!PspCaptureUserProcessParameters` | `+0x5bb` | `+0x5c0` | `nt!PspValidateEnvironmentBlock` | `param_4 & 3 ≠ 1` 일 때. `EnvironmentSize` 를 줄일 수 있음 | [정적] |
| `nt!MiMapProcessExecutable` | `+0xe3` | `+0xe8` | `nt!MiCfgInitializeProcess` | 늘 (exe · ntdll 붙인 뒤) | [정적] (run32 "지도상" → 이번에 본문) |
| `nt!MiCfgInitializeProcess` | `+0x76` · `+0x156` | `+0x7b` · `+0x15b` | `nt!MiMapSecurePureReserveView` | CFG 켜짐 (`+0x1e`). `+0x156` 은 기준주소 0 으로 다시 | [정적] |
| `nt!MiCfgInitializeProcess` | `+0x98` | `+0x9d` | `nt!MiReferenceCfgVad` (번호 0) | 위 붙이기 성공 | [정적] |
| `nt!MiCfgInitializeProcess` | `+0x119` · `+0x131` | `+0x11e` · `+0x136` | `MiMapSecurePureReserveView` · `MiReferenceCfgVad` (번호 1) | WoW64 이고 `Machine` `0x14c`/`0x1c4` | [정적] |
| `nt!MiReferenceCfgVad` | `+0x43` · `+0x5f` | `+0x48` · `+0x64` | `MiObtainReferencedVadEx` · `MiUnlockVadShared` | 비트맵 VAD 끝 페이지 읽기 | [정적] |
| `nt!MmInitializeImageViewExtensionCfg` | `+0x288` | `+0x28d` | `nt!PsInitializeScpCfgPages` | **부팅 (INIT 구역)** | [정적] |
| `nt!PsInitializeScpCfgPages` | `+0x77` | `+0x7c` | `nt!PspLocateNtdllAddressesForScpCfg` | 부팅 한 번 | [정적] |
| `nt!PspLocateNtdllAddressesForScpCfg` | `+0x4b` · `+0x69` · `+0x8d` | `+0x50` · `+0x6e` · `+0x92` | `PsQuerySystemDllInfo` · `RtlImageNtHeaderEx` · `PspCopyNtdllExport` | `"RtlpScpCfgNtdllExports"` `0x68`B 복사 | [정적] |
| `nt!PspLocateNtdllAddressesForScpCfg` | `+0x1b4` · `+0x1cb` · `+0x1e2` · `+0x1f8` | `+0x1b9` · `+0x1d0` · `+0x1e7` · `+0x1fd` | `MmGetScpCfgFunctionOffset` ×4 | 전역 넷 채우기 | [정적] |
| `nt!PsInitializeScpCfgPages` | `+0x14d` | `+0x152` | `nt!PspFinalizeScpCfgPage` | 쪽 종류 1 ~ 3 (run63) | [정적] (본문 안 봄) |
| `nt!PspGetScpCfgFunctions` | `+0x51` · `+0x85` | `+0x56` · `+0x8a` | `ZwQueryVirtualMemory` · `PsGetScpCfgPageTypeForProcess` | 결과 3 · 4 면 NULL | [정적] |
| `nt!PspCopyAndFixupParameters` | `+0x3eb` · `+0x402` | `+0x3f0` · `+0x407` | `PspCopyUserAttributes` · `RtlWriteULong64ToUser` | 유저 복사 속성 개수 `[R15+0x250]` > 0 일 때만 → **`AttributeList (+0x448)` 를 새 주소로** | [정적] (`+0x3eb` 는 run60) |

### (a) `Flags` 검사 — 마스크는 "켜져 있으면 거절" (결과서 1 (a))

- 순서: `+0x8f MOV EDI,[RDX+8]` (사용자 블록 `Flags`) → **`+0x92 BTR EDI,0xe`** (비트 14 를 먼저 지움) → `+0xa0 XOR EAX,1` → **`+0xa3 TEST EAX,0xf4010e11`** → `+0xa8 JNZ` → `+0xf3 MOV EAX,0xc000000d`.
  - 그래서 **비트 0 은 켜져 있어야** 통과, 마스크의 나머지 비트는 **꺼져 있어야** 통과.
- 둘째 검사 `+0xaa ~ +0xba`: 비트 5 (`0x20`) · 6 (`0x40`) · 24 (`0x1000000`) 중 **둘 이상** 켜면 실패 (`+0xba JNZ → +0xfd`).
- 사용자가 줄 수 있는 비트 = 0 (필수) + 1 · 2 · 3 · 5 · 6 · 7 · 8 · 12 · 13 · 15 · 17 ~ 25 · 27 (5 · 6 · 24 는 하나만). 14 는 줘도 지워진다.

| 비트 | 값 | 입력 검사 | 커널이 읽거나 쓰는 줄 | 뜻 |
|---|---|---|---|---|
| 0 | `0x1` | **켜져야 함** | 복사본에 그대로 (`+0x40e`) | "정규화됨" (run32). PDB 이름 없음. 공개 이름 `RTL_USER_PROC_PARAMS_NORMALIZED` `[추측: 기억]` |
| 4 · 9 · 10 · 11 · 16 · 26 · 28 · 30 | | 꺼져야 | 본 커널 함수에 없음 | `[공백]` — 읽는 쪽은 ntdll (Q63b) |
| 29 | `0x20000000` | 꺼져야 | **커널이 켬** `PspSetupUserProcessAddressSpace +0xe6` (run32) | OneCore forwarders `[추측: 이름]` |
| 31 | `0x80000000` | 꺼져야 | 켜는 줄 없음 (아래 (e)) | ntdll `LdrpIsSecureProcess` 로 감 (run17), "보안 프로세스" `[추측]` |
| (참고) 5 · 6 · 24 | `0x20` · `0x40` · `0x1000000` | 마스크 밖, 서로 배타 | `PspSetupUserProcessAddressSpace +0xa6 AND R14D,0x1000060` → `+0x15c` · `+0x15f` — 셋 다 꺼지고 크기가 `0x20000` 보다 작으면 `+0x161 ~ +0x16b` 에서 `0x20000` 으로 올림 | `RESERVE_1MB` · `RESERVE_16MB` `[추측: 기억]`, 24 는 `[공백]` |
| (참고) 14 | `0x4000` | 입력에서 **지움** | `PspCopyAndFixupParameters +0x82` 가 켬 (run50 조건) | IFEO 키 없음 |
| (참고) 15 | `0x8000` | 마스크 밖 | `PspSetupUserProcessAddressSpace +0xb1` (조건 `PspGlobalFlags & 1`) | `[공백]` |

### (a) Environment 자리 — 블록 뒤에 붙인다 (결과서 1 (a) 아래 표)

| 줄 (`PspCaptureUserProcessParameters`) | 하는 일 |
|---|---|
| `+0xbc` · `+0xc8` | 사용자의 `Environment` (`+0x80`) · `EnvironmentSize` (`+0x3f0`) 읽기. 크기 0 · 홀수면 실패, 끝이 `0x7fffffff0000` 을 넘으면 일부러 읽어 접근 위반 |
| `+0x26c ~ +0x2d4` | **`F = (0x450 + 1 + 문자열 아홉 개 MaximumLength 합 + 현재 폴더 칸) & ~1`**. 현재 폴더 칸은 이 길에서 `0x208` (`+0x625`) |
| `+0x7d6 ~ +0x7f5` | 셋째 인자 `+0x4` 비트 0 이면 `F = ((F + 3) & ~3) + 개수 × 8` (CPU 집합 배열 자리) |
| `+0x317` (ret `+0x31c`) | `ExAllocatePool2(0x41, F + EnvironmentSize, 'PsPb')` — 한 번만 |
| `+0x335 ~ +0x3a1` | 사용자 블록 앞 `0x450` 바이트 **통째 복사** |
| `+0x40e` · `+0x411` · `+0x415` | `Flags` (비트 14 뺀 값), `Length` = `MaximumLength` = F |
| `+0x418` → `+0x43b ~ +0x528` | 문자열 커서 = `블록 + 0x450`, 차례 `+0x38` · `+0x50` · `+0x60` · `+0x70` · `+0xb0` · `+0xc0` · `+0xd0` · `+0x410` (+ 조건부 `+0x420`), `RuntimeData` 는 머리 `+0x53b` + 본문 `+0x683` |
| `+0x567 ~ +0x570` · `+0x780` | 커서가 `블록 + F` 보다 모자라면 `memset 0` 으로 채워 맞춤 |
| **`+0x583`** (ret `+0x588`) | **환경을 `블록 + F` 에 복사** |
| `+0x5bb` (ret `+0x5c0`) | `PspValidateEnvironmentBlock` — 크기를 줄일 수 있음 |
| **`+0x5e1`** · `+0x5ed` · `+0x5f4` | **`Environment` = `블록 + F`**, `EnvironmentSize` = (검사 뒤) 크기, `EnvironmentVersion` = 0 |

- 새 프로세스 쪽도 같은 모양이다: `PspSetupUserProcessAddressSpace +0x115 ~ +0x11f` 가 `F + EnvironmentSize` 자리를 잡고, `PspCopyAndFixupParameters +0x36 ~ +0x39` 가 같은 크기를 복사한다 (run32 와 같음).
- 커널 사본에 다시 쓰는 칸 전부 (flow-writer `.asm`, `[RDI…]` 저장 줄을 다 셌다): 위 표 + `+0x534` (`HeapPartitionName` 0 — 셋째 인자가 없을 때) · `+0x551` · `+0x558` (`DefaultThreadpoolCpuSetMasks` · `…Count` 0) / 조각 `+0x13bce9` · `+0x13bcf4` (있으면 배열 자리 · 개수) · `+0x819` (`DefaultThreadpoolThreadMaximum`, 셋째 인자 `+0x4` 비트 3) · `+0x84d` (`HeapMemoryTypeMask`, 비트 2) · `+0x5d1 ~ +0x5dd` (`param_4 & 1` 갈래 — `ConsoleHandle` · 표준 핸들 셋 0). **나머지 칸은 사용자 블록 값 그대로다.**
- `param_4 & 1` (= 만들기 정보 `+0x10` 비트 3, `PspCaptureProcessParameters +0x1f ~ +0x31`) 갈래는 현재 폴더 · `RuntimeData` · `DllPath` 를 서버 silo 값에서 가져온다 (`+0x135 ~ +0x185`, 이름 모름 `[공백]`). 환경은 그래도 사용자 것.

### (b) CFG 비트맵 칸 `+0x4a0`/`+0x4a8` (결과서 2 (b-1))

- **`MiCfgInitializeProcess` 는 직접 안 쓴다.** 메모리 쓰기는 지역 변수뿐, 새 프로세스 `EPROCESS` 는 읽기만 (`+0x13 [RCX+0x750]` · `+0x3e [RDI+0x368]` · `+0xa7 [RDI+0x6ec]`).
- **`MiReferenceCfgVad` 가 쓴다:**
  - `+0x16 [RCX+0xb8]` = 지금 붙어 있는 프로세스 (새 프로세스 — `MmInitializeProcessAddressSpace +0xb0 KiStackAttachProcess` 뒤라서).
  - `+0x33 ~ +0x3b`: **칸 = `VmWorkingSetList + (번호 + 0x25) × 0x20`** — 번호 0 → `+0x4a0`, 번호 1 → `+0x4c0`.
  - **`+0x71 MOV [R15],RBP`** → `+0x4a0` = 비트맵 시작 · **`+0x82 MOV [R15+8],RBX`** → `+0x4a8` = 크기 (`((EndingVpn << 12) | 0xfff) − 시작 + 1`) · `+0x77` `+0x10` = VAD · `+0x7e` `+0x18` = 0 (번호 1 · `Machine 0x1c4` 면 1).
- **언제 서나**: `+0x13 MOV EAX,[RCX+0x750]` · `+0x1c TEST AL,1` · **`+0x1e JZ +0x179`** — `EPROCESS.MitigationFlags` 비트 0 (PDB `ControlFlowGuardEnabled`) 이 꺼지면 **아무것도 안 쓰고 0**. 켜지면 `+0x76 MiMapSecurePureReserveView(프로세스, MiState.Sections.CfgBitMapSection, &MiState.Processes.NativeCfgBitmapPreferredAddress, …)` → `+0x98 MiReferenceCfgVad(…, 0)`. WoW64 면 한 번 더 (번호 1).
- 그래서 `PspPrepareSystemDllInitBlock +0x15a` · `+0x169` 가 옮기는 블록 `+0xb8` / `+0xc0` = **CFG 비트맵 시작 · 크기** (run32 의 `[추측]` 이름 → 칸 뜻은 `[정적]`, 공개 이름은 PDB 에 없음).
- `MmInitializeProcessAddressSpace` 안에도 `MiReferenceCfgVad` 직접 호출이 둘 더 있다 (`+0x4a5` 번호 1 · `+0x4ba` 번호 0) — 어느 길인지 안 봄 `[공백]`.

### (b) ScpCfg 전역 `PspNtdllScpFunctions` (결과서 2 (b-2))

- **부팅 때 한 번** (INIT 구역): `MmInitializeImageViewExtensionCfg +0x288 PsInitializeScpCfgPages` → `+0x77 PspLocateNtdllAddressesForScpCfg`.
- 이름 문자열은 **하나**: `+0x83 LEA R8,[0x140c7a0c0]` = `"RtlpScpCfgNtdllExports"`. `+0x8d PspCopyNtdllExport` 가 ntdll 내보내기 그 이름의 표 `0x68`B 를 복사한다 (ntdll rva `0x1746f0`).
  - 표 = 쪽 넷의 (시작, 끝) 쌍 (`ScpCfgHeader_Nop` · `ScpCfgHeader` · `…_ES` · `…_Fptr`, 각 `0x2b0`B) + 8B 칸 넷 (`LdrpGuardDispatchIcallNoESFptr` · `__guard_dispatch_icall_fptr` · `LdrpGuardCheckIcallNoESFptr` · `__guard_check_icall_fptr`) + 함수 하나 (`LdrpHandleInvalidUserCallTarget`) — ntdll PDB 이름.
  - 검사: 쌍은 길이 ≤ `0x1000` · ntdll 이미지 안 (아니면 `+0xf0` `0xC000000D`), 다섯 칸은 이미지 안인지만.
- **전역 넷** = `MmGetScpCfgFunctionOffset(쪽 안 위치, SizeOfImage)` = `(SizeOfImage 쪽 올림) + MiState.Sections.ImageViewExtensionOffsets[0] + 위치`:

| 전역 칸 (rva) | 저장 줄 | 쪽 안 위치 | 가리키는 것 (ntdll PDB 쪽 배치) |
|---|---|---|---|
| `PspNtdllScpFunctions+0x0` (`0xfc7640`) | `+0x1bf` | `0x140` | `ScpCfgValidateUserCallTarget` 자리 |
| `+0x8` (`0xfc7648`) | `+0x1d6` | `0x1c0` | `…ValidateUserCallTargetES` 자리 |
| `+0x10` (`0xfc7650`) | `+0x1ec` | `0x40` | `ScpCfgDispatchUserCallTarget` 자리 |
| `+0x18` (`0xfc7658`) | `+0x202` | `0xc0` | `…DispatchUserCallTargetES` 자리 |

- `ImageViewExtensionOffsets[0]` 이 0 이면 (run63 · 짐 지도 7.6.4: `MmInitializeImageViewExtension +0x14` 가 0) 네 주소는 **ntdll 이미지 끝을 쪽 올림한 곳 = ntdll 바로 뒤 첫 쪽 + 위치** — run63 의 "ntdll 이미지 뒤 확장 쪽" (`ntdll + 0x267000`) 과 같은 쪽이다 `[정적]`.
- 이 표를 돌려주는 쪽 = `PspGetScpCfgFunctions +0x92 LEA RCX,[PspNtdllScpFunctions]` (`+0x85 PsGetScpCfgPageTypeForProcess` 가 3 · 4 면 NULL). 받아서 블록 `+0xf0 ~ +0x127` 에 넣는 건 `PspPrepareSystemDllInitBlock +0x196` (run32).
- 이름 크기 `0x38` (7칸) 중 `+0x20 ~ +0x30` 셋을 쓰는 줄은 `_refs` 에 없다 `[공백]` (flow-writer `_refs/PspNtdllScpFunctions.tsv`).

### (c) 만들기 정보 `+0x11` (결과서 3)

`PspBuildCreateProcessContext` 안에서 `[R12+0x11]` 에 쓰는 줄은 **둘뿐** (결과서 `grep` 전수):

| 줄 | 속성 (PDB `_PS_ATTRIBUTE_NUM`) | 하는 일 | 값 뜻 |
|---|---|---|---|
| `+0x282 ~ +0x2ff` → **`+0x309 MOV [R12+0x11],DL`** | `0x2000a` = `PsAttributeStdHandleInfo` (10) | 크기 8 검사, 첫 8B 읽기. `+0x2d1 TEST AL,0x1c` 켜지면 `0xC00000BB`, `+0x2e2` 낮은 2비트 = 상태, `+0x2ed` 3 이면 `0xC000000D`. `+0x11 = (+0x11 & 0xf3) \| (상태 << 2)`. 상태 1 이면 윗 4B 를 `[R12+0x120]` (`+0x318 ~ +0x31c`) | **비트 2~3 = 표준 핸들 상태** (PDB `_PS_STD_HANDLE_STATE`): 0 `PsNeverDuplicate` · 1 `PsRequestDuplicate` · 2 `PsAlwaysDuplicate` · 3 `PsMaxStdHandleStates` 은 거부 |
| `+0x1740 ~ +0x1765` → **`+0x1760 MOV [R12+0x11],CL`** | `0x6001a` = **`PsAttributeChpe`** (26) | 크기 1, 값 ≠ 0 이면 비트 7 (`+0x174f NEG` · `+0x1752 SBB` · `+0x1754 AND CL,0x80`) | **비트 7 = CHPE 속성 켜짐** (뜻 설명은 모름 `[공백]`) |

- 읽는 쪽 = `PspSetupUserProcessAddressSpace +0x41 ~ +0x71` (run32 그대로: 1 → `+0x92 PspGetStandardHandleList`, 2 → 사용자 매개변수 안 표준 핸들 칸, 0 · 3 → 없음).
- `+0x11` 의 비트 0 은 `PspCaptureCreateInfo +0x56 ~ +0x68` 이 채우고, `PspCaptureProcessParameters +0x63 AND [RBX+0x11],0xf3` 이 비트 2~3 을 지우는 갈래도 있다 (`param_4 & 1` 갈래).
- test.exe 가 상태 몇을 받는지는 정적으로 못 정한다 (동적 2).

### (e) `Flags` 비트 31 (결과서 4)

- **입력으로는 못 준다** — 마스크 안 비트라 켜서 넘기면 `0xC000000D`.
- 커널이 `Flags` (`+0x8`) 를 쓰는 줄 (본 함수들): `PspCaptureUserProcessParameters +0x40e` (입력 − 비트 14), `PspCopyAndFixupParameters +0x82` (14), `PspSetupUserProcessAddressSpace +0xb1` (15) · `+0xe6` (29). 31 없음.
- ntoskrnl 전체 꼴 검색 (결과서): `bts [reg+0x8],0x1f` 1곳 (`HvlAddPagesCallbackRoutine +0xdb`) · `or [reg+0x8],0x80000000` 0곳 · 레지스터에 켜고 15명령 안에 `[reg+0x8]` 저장 3곳 (`SmPageWrite` · `ExBuildPasidMsrForThread` · `ExpUpdateDebugInfo`) · 즉시값 저장 28곳 — **모두 매개변수 블록과 무관.**
- 곁눈질 (범위 밖, 읽기만): `securekernel.exe` · ntdll 스크래치 풀이에도 같은 꼴 0곳.
- → **test.exe 같은 일반 `NtCreateUserProcess` 길에서 비트 31 은 꺼진 채** `[정적: 꼴 검색 한계 안에서]`. 보안 프로세스 (trustlet) 를 만드는 다른 길이 켤 것 `[추측]`.

### (f) 작은 것 셋 (결과서 5)

- **(f-1) 안 고치는 칸**: `PspCopyAndFixupParameters` 가 고치는 칸은 **12개** (두 갈래 같음 — 켜짐 `+0x22b ~ +0x314`, 꺼짐 `+0x424 ~ +0x50d`). `PackageDependencyData (+0x400)` · `CurrentDirectores (+0xf0 ~ +0x3ef)` 는 어느 목록에도 없고, 커널 사본에서도 `+0x335 ~ +0x3a1` 통째 복사 뒤 쓰는 줄이 없다 → **사용자 값 그대로** 자식에 간다. 왜 안 고치는지는 코드로 못 정함 `[추측: 만드는 쪽이 0 으로 두는 칸]`.
  - 곁다리: `AttributeList (+0x448)` 는 따로 — `PspCopyAndFixupParameters +0x3c7 CMP [R15+0x250],R13D` 가 0 보다 크면 `+0x3f8 LEA RCX,[RDI+0x448]` → `+0x402 RtlWriteULong64ToUser` (ret `+0x407`) 가 새 주소로 덮는다. **flow.md 4장 run60 "정적으로 남은 것 ①" 이 이 줄이다.**
- **(f-2) 이름 뜻** `[추측]`:
  - `Feature_CreateProcessSegmentHeapEnablement__private_featureState` (rva `0xfc4110`) = 단계적 배포 기능 스위치 이름 모양. "SegmentHeap" = 윈도 새 힙 이름 — 이 스위치 아래에서 쓰는 칸이 `HeapPartitionName (+0x420)` · `DefaultThreadpoolCpuSetMasks (+0x430)` · `HeapMemoryTypeMask (+0x440)` 이고 PDB 속성 열거에 `PsAttributeHeapFeatures = 34` 가 있다. 확정 아님.
  - "SCP" — 코드에 풀이 없다. 단서: 커널 구역 `KSCP`, `MiMapKernelScp` · `RtlCreateKernelScpFunctionTable` · `MiMapImageScpCfgPages` · `KscpCfgDispatchUserCallTargetEsSmep`. "이미지 밖에 따로 붙이는 CFG 검사 · 디스패치 코드 쪽" 이름으로 보인다. 약어 풀이는 모른다 `[공백]`.
- **(f-3) `Rdx` 출처 이름**: **같은 칸이다.** PDB 주소 `PspSystemDlls` = rva `0xfd8130` (포인터 6칸 표), 파일 초기값 `[0]` = `0x140fd8980` = **`PspNativeSystemDllData`** (rva `0xfd8980`). 표 `[0]` 을 쓰는 줄은 `PsLocateSystemDlls +0x8d AND [RBX],0` (실패 때 비움) 하나. 그래서 `PspInitializeThunkContext +0x196 MOV RAX,[PspSystemDlls]` · `+0x19d MOV RCX,[RAX+0x20]` = `[PspNativeSystemDllData+0x20]` = rva **`0xfd89a0`** (`.asm` 주석도 `PspNativeSystemDllData+0x20`).
  - **헷갈리지 말 것**: `_refs/PspSystemDlls.tsv` 의 `PspSystemDlls +0x20` (`MOV RDX,[0x140fd8150]`, `PspInitPhase3 +0x89` 등) 은 표의 5번째 칸 `[4]` 이다. `PspSystemDlls[0]+0x20` (0번 칸이 가리키는 구조체의 `+0x20`) 과 다르다.
  - `+0x20` 이 "ntdll 실제 베이스" 인지, 옆 `+0x18` 과 같은지는 정적으로 못 정한다 (`PspLocateNtdllAddressesForScpCfg` 는 `+0x18` 을 기준, `PspGetScpCfgFunctions +0x36 ~ +0x51` 은 `+0x20` 을 `ZwQueryVirtualMemory(-1, …)` 주소로 씀 — 동적 4).

### 사진 로그 대조 (run44, 새로 돌린 것 아님 — flow-writer)

| 정적 | 사진 값 (`ms/logs/04_baggage-read_K1.txt`, 새 프로세스 블록 = PEB `+0x20` `25996cb0000`) | 맞나 |
|---|---|---|
| `Length` = `MaximumLength` = F (`+0x411` · `+0x415`) | `MaximumLength 0x734` · `Length 0x734` (2335 ~ 2336줄) | **맞음** (둘이 같음) |
| `Environment` = `블록 + F` (`+0x5e1`, 새 프로세스로 옮긴 뒤 Fixup) | `Environment 0x25996cb0734` (2348줄) = `25996cb0000 + 0x734` | **맞음** |
| `EnvironmentVersion` = 0 (`+0x5f4`) | `0` (K1), P1a 부터 3 (짐 지도 3.2) | 맞음 (뒤 바뀜은 ntdll) |
| `Flags` = 입력 − 비트 14, 마스크 통과, 그 뒤 14 켬 | `Flags 0x4001` (2337줄) — 비트 0 · 14 만 | 맞음 (비트 31 꺼짐) |
| `PackageDependencyData` · `CurrentDirectores` 그대로 | `(null)` · 배열 (짐 지도 3.2 `끝까지 0`) | 맞음 (부모가 0) |
| CFG 갈래: `MitigationFlags` 비트 0 꺼지면 `+0x1e` 에서 끝 | `MitigationFlags 0x20` (287줄, 비트 0 꺼짐) · 블록 `+0xb8` = 0 (run82 · sast55) | **맞음** — test.exe 는 `+0x1e` 갈래였을 것 `[추측: 그 사이 비트 0 이 안 바뀜]` |

### 호출 관계

```
NtCreateUserProcess
 ├─ +0xacb  PspCaptureProcessParameters
 │           └─ +0x3f  PspCaptureUserProcessParameters
 │                ├─ +0x8f ~ +0xa8   Flags 검사 ((Flags^1) & 0xf4010e11, 비트 14 먼저 지움) → 아니면 0xC000000D
 │                ├─ +0x31c  ExAllocatePool2(0x41, F + EnvironmentSize, 'PsPb')     한 덩어리
 │                ├─ +0x335 ~ +0x3a1  앞 0x450B 통째 복사 · +0x40e Flags · +0x411/+0x415 F
 │                ├─ +0x440 … +0x528  PspCopyUnicodeString ×8 (블록 +0x450 뒤)
 │                ├─ +0x588  메모리 복사 함수   환경 → 블록 + F
 │                ├─ +0x5c0  PspValidateEnvironmentBlock
 │                └─ +0x5e1 Environment = 블록 + F · +0x5ed 크기 · +0x5f4 Version 0
 └─ +0xc36  PspAllocateProcess
             ├─ +0x2095  MmInitializeProcessAddressSpace
             │            └─ +0x1ce  MiMapProcessExecutable
             │                        └─ (3) +0xe8  MiCfgInitializeProcess
             │                              +0x1e  CFG 꺼짐 → 0 (아무것도 안 씀)   ← test.exe [추측]
             │                              ├─ +0x7b  MiMapSecurePureReserveView   CfgBitMapSection
             │                              └─ +0x9d  MiReferenceCfgVad(…, 0)
             │                                         +0x71 [WSL+0x4a0] = 시작 · +0x82 [WSL+0x4a8] = 크기
             └─ +0x239c  PspSetupUserProcessAddressSpace
                          ├─ +0x41  [CreateInfo+0x11] 비트 2~3 읽기 (← PspBuildCreateProcessContext +0x309)
                          ├─ +0x1b9  PspCopyAndFixupParameters    12칸 고침 · (조건) +0x407 AttributeList
                          └─ +0x1e6  PspPrepareSystemDllInitBlock  +0xb8/+0xc0 ← WSL+0x4a0/+0x4a8, +0xf0 ~ ← PspNtdllScpFunctions

(부팅, 흐름도 범위 앞)
MmInitializeImageViewExtensionCfg
 └─ +0x28d  PsInitializeScpCfgPages
             └─ +0x7c  PspLocateNtdllAddressesForScpCfg
                        ├─ +0x92  PspCopyNtdllExport  "RtlpScpCfgNtdllExports" 0x68B
                        └─ +0x1b9 · +0x1d0 · +0x1e7 · +0x1fd  MmGetScpCfgFunctionOffset → PspNtdllScpFunctions +0x0 ~ +0x18
```

## 확인된 것

- `[정적]` **마스크 `0xf4010e11` = 꺼져 있어야 하는 비트 (비트 0 만 반대로 켜져 있어야 함).** 비트 5 · 6 · 24 는 셋 중 하나만. 비트 14 는 입력에서 지운다.
- `[정적]` **Environment 는 블록 바로 뒤 (`블록 + Length`)**, 할당 한 번. `Length` = `MaximumLength` = F 를 커널이 다시 셈. `EnvironmentVersion` 은 커널 사본에서 0.
  - `[관찰]` run44 K1 `Length = MaximumLength = 0x734`, `Environment = 블록 + 0x734` — 위 모양과 맞음.
- `[정적]` 커널 사본에 다시 쓰는 칸은 위 "Environment 자리" 표 + 셋째 인자 칸 넷 + silo 갈래 핸들 넷뿐, 나머지는 사용자 값 그대로.
- `[정적]` **CFG 비트맵 칸 `VmWorkingSetList +0x4a0` / `+0x4a8` = `MiReferenceCfgVad +0x71` / `+0x82`.** `MiCfgInitializeProcess` 는 `MitigationFlags` 비트 0 이 꺼지면 `+0x1e` 에서 그냥 0.
- `[정적]` `PspNtdllScpFunctions +0x0 ~ +0x18` = ntdll 뒤 쪽 안 주소 넷 (`0x140` · `0x1c0` · `0x40` · `0xc0`), 부팅 때 한 번. 이름 문자열은 `"RtlpScpCfgNtdllExports"` 하나.
- `[정적]` 만들기 정보 `+0x11` 비트 2~3 = 표준 핸들 상태 (`PspBuildCreateProcessContext +0x309`), 비트 7 = `PsAttributeChpe` (`+0x1760`).
- `[정적]` `Flags` 비트 31 은 입력 불가 + 커널에서 켜는 꼴 없음.
- `[정적]` `PspSystemDlls[0]+0x20` = `PspNativeSystemDllData+0x20` (rva `0xfd89a0`). `PspSystemDlls +0x20` (표 칸 `[4]`) 와는 다르다.
- `[정적]` `AttributeList (+0x448)` 는 `PspCopyAndFixupParameters +0x402` (ret `+0x407`) 가 유저 복사 속성이 있을 때만 덮는다.

## 결과서와 다르게 적은 곳 · 보탠 것

- 다르게 적은 곳: 없음. 결과서 줄은 모두 `.asm` 과 같다.
- 보탠 것 (flow-writer `.asm` · `_calls.tsv` · `_refs`):
  - 커널 사본에 쓰는 칸 전수 (`[RDI…]` 저장 줄): 결과서 표에 없던 `+0x534` · `+0x551` · `+0x558` · `+0x5d1 ~ +0x5dd` · `+0x819` · `+0x84d`. 이 둘 (`+0x819` `DefaultThreadpoolThreadMaximum`, `+0x84d` `HeapMemoryTypeMask`) 과 조각 `+0x13bce9` · `+0x13bcf4` 는 **셋째 인자 구조** (`R15 = R8`, `+0x29`) 의 `+0x4` 비트 3 · 2 · 0 에서 온다. 결과서가 "사용자 속성 목록" 이라 부른 `+0x430` / `+0x438` 은 PDB 칸 이름으로 `DefaultThreadpoolCpuSetMasks` / `…Count` 다.
  - `PsInitializeScpCfgPages` 를 부르는 쪽 = `MmInitializeImageViewExtensionCfg +0x288` (ret `+0x28d`, `_calls.tsv`) — 짐 지도 7.6.4 의 부팅 함수와 이어짐.
  - `_refs/PspNtdllScpFunctions.tsv` 쓰기 4줄뿐 — 이름 크기 7칸 중 뒤 셋은 `[공백]`.
  - 사진 대조 표 (위).

## 정정

- **요청서 sast/104 질문 4 의 "입력 마스크 `0xf4010e11` 에 있어 사용자가 줄 수 있는 비트인데"** → 반대다. 마스크 비트는 켜져 있으면 거절한다 (결과서 1 (a) "읽는 법"). 이 반대 표현은 **요청서에만 있다** — run32 (131 · 196 · 272줄) · flow.md (2.1 `PspCopyAndFixupParameters` · 짝 표 · 2.4 `LdrpInitialize` · 2.5 · 4장) 는 모두 "켜져 있으면 실패 / 사용자는 못 켬" 으로 바르게 적었다. 그래서 **run32 문서 맨 위 정정 줄은 넣지 않았다.** 요청서 파일은 고치지 않는다.
- **이름 두 개 (`PspSystemDlls[0]+0x20` · `PspNativeSystemDllData+0x20`)** — 정정이 아니라 run32 254줄의 "같은 칸인지 [확인 필요] Q63 (f)" 를 닫은 것이다. run16 · flow.md 의 `PspSystemDlls[0]+0x20` 은 맞는 말이라 그대로 두고, 같은 칸 이름을 붙였다.

## 이전 run 에서 닫은 것

- run32 "아직 확인 필요" **Q63 (a) · (b) · (c) · (e) · (f)** — 닫힘 (Q63a). (e) 는 "일반 길에선 꺼짐" 까지, (a) 비트 이름 · (f) "왜" 는 남음. (d) 는 Q63b (사용자 결정 대기).
- run32 254줄 "`PspNativeSystemDllData +0x20` 과 같은 칸인지 [확인 필요]" — 같은 칸.
- run60 "정적으로 남은 것 ①" (`PspCopyUserAttributes` 목록이 매개변수 `+0x448` 에 이어지는 줄) — `PspCopyAndFixupParameters +0x402`.
- flow.md 2.1 `PspCopyAndFixupParameters` 절 "`+0x11` 을 채우는 줄은 Q63 (c)" · `PspPrepareSystemDllInitBlock` 절 "그 함수가 `+0x4a0` 칸을 쓰는지는 Q63 (b)" · "값이 ntdll 의 어떤 함수인지, SCP 뜻은 모른다 (Q63 (b))" — 닫힘 (SCP 약어 풀이만 남음).

## 아직 확인 필요

### 동적 (`[VM]`, 결과서 "동적분석으로 확인해야 할 것")

멈춤점은 모두 커널이라 test.exe 조건을 붙인다. 주소는 이 빌드 rva 기준 (`nt+…`).

1. **`Flags` 세 지점**: `nt+0x9f151e` (`PspCaptureUserProcessParameters +0x40e`) `eax` · `nt+0xae5e21` (`PspSetupUserProcessAddressSpace +0xed`) `ebx` · `nt+0xae5eed` (`+0x1b9` 복사 직후) 새 블록 `+0x8`. 사진 `0x4001` 과 같은지, 비트 31 이 0 인지. (사진이 이미 `0x4001` 이라 확인용)
2. **`+0x11` 값**: `nt+0xae5d75` (`PspSetupUserProcessAddressSpace +0x41`) 에서 `db @r9+0x11 L1` — 비트 2~3 (표준 핸들 상태), 비트 7. 쓰는 줄로는 `nt+0x840c29` (`PspBuildCreateProcessContext +0x309`) · `nt+0x842080` (`+0x1760`) 이 서는지.
3. **CFG 갈래**: `dt nt!_EPROCESS <새 프로세스> MitigationFlagsValues.ControlFlowGuardEnabled`. 0 이면 `nt+0x90b652` (`MiCfgInitializeProcess +0x1e`) 에서 뜀. 1 이면 `nt+0xaaf359` (`MiReferenceCfgVad +0x71`) · `nt+0xaaf36a` (`+0x82`) 의 `r15` · 값. (run44 K1 `MitigationFlags 0x20` 으로 이미 거의 답함 — 확인용)
4. **ScpCfg 결과 칸** (INIT 이라 멈춤점 아님, 부팅 뒤 읽기만): `dq nt!PspNtdllScpFunctions L7` (기대: ntdll 끝 쪽 올림 + `0x140` · `0x1c0` · `0x40` · `0xc0`, 뒤 셋은?), `dd nt+0xe2d8e0 L1` (`ImageViewExtensionOffsets[0]` = 0 인가), `dq nt!PspNativeSystemDllData+0x18 L2` 와 `lm m ntdll` (`+0x18` · `+0x20` 이 같은가 = `Rdx` 값 — 기존 Q42 D1 과 같은 질문).
5. **`PackageDependencyData` · `CurrentDirectores`**: `nt+0xae5eed` 뒤 새 블록 `+0x400`, `+0xf0 ~ +0x3ef` 가 0 인지 (사진 K1 이 이미 `(null)` · 0 — 확인용).
- 1 · 2 · 3 · 5 는 한 번의 test.exe 실행에서 같이, 4 는 부팅 뒤 읽기만. 새로 볼 값은 **2 (`+0x11`) 와 4 (`PspNtdllScpFunctions`)** 뿐이다 — 나머지는 run44 사진과 이미 맞는다.

### 정적 (결과서 "다음 요청 후보" + 보탬)

1. **Q63b (ntdll, 사용자 결정 대기)**: `Flags` 비트 4 · 9 · 10 · 11 · 16 · 26 · 28 · 30 · 31 과 `0x2000000` (비트 25) 를 ntdll 이 읽는 줄 → 비트 이름 단서. 블록 문서 없는 칸 · `RtlNormalizeProcessParams` 도 이쪽.
2. **`PspFinalizeScpCfgPage` + `PsGetScpCfgPageTypeForProcess`**: 다섯 칸 (`+0x40 ~ +0x60`) 이 쪽 안에서 어떻게 쓰이나, 어떤 프로세스가 어떤 쪽 종류를 받나 (PE_emulator D33).
3. (작음) `MmInitializeProcessAddressSpace +0x4a5` · `+0x4ba` 의 `MiReferenceCfgVad` 두 자리가 어느 길인지.
4. (비트 31 이 꼭 필요하면) 보안 프로세스 만드는 길 (`PsAttributeSecureProcess` 갈래) · `securekernel.exe` (PDB 없음, 새 analyze — 사용자 결정).
5. (보탬, 작음) `PspNtdllScpFunctions +0x20 ~ +0x30` 셋을 쓰는 곳 (레지스터+오프셋 쓰기라 `_refs` 가 못 잡았을 수 있음).
6. (보탬, 작음) `PspCaptureUserProcessParameters` 셋째 인자 구조 (`R15`) 의 정체와 `+0x4` 비트 0 · 2 · 3 (CPU 집합 · 힙 파티션 · 스레드 최대) 을 켜는 쪽 — `PspBuildCreateProcessContext` 속성 `0x20` 대 (`PsAttributeHeapFeatures` 류) 로 보임 `[추측]`.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

- (a) `Flags = 1` · 환경 `base + dwLength` · 현재 폴더 칸 `0x208` · 문자열 차례 — **맞음**.
- (b) `+0x4a0` / `+0x4a8` 0 · `MiCfgInitializeProcess` 안 만듦 — **맞음 (test.exe 한정)**. ScpCfg: 이름 · 쌍 0 검사 · 환산은 함, 쌍 1~3 · 다섯 칸 · 전역 넷 채우기는 "공사 중 (D33)" — **틀리지 않음, 줄임** (빠진 값 계산은 위 표).
- (c) `+0x11` 해석은 맞음, test.exe 실제 상태 값은 **확인 필요** (동적 2).
- (e) 비트 31 안 켬 — **맞음**.
- (f-1) Fixup 12칸 — **맞음**. (f-3) `SystemDll.cpp:244` TODO — "같은 칸" 까지만 확정, 값은 **확인 필요** (동적 4).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntkrnlmp 줄에 run104, 근거 로그 (run104) 한 줄), 그림 1 (`p3` 에서 (3) `MiCfgInitializeProcess` 노드 하나 — 정적 상자, `p10` · `s1` 글, 제목 run104), 2.1 (절 머리 · `PspBuildCreateProcessContext` (`+0x11` 두 줄) · `MiMapProcessExecutable` ((3) `+0xe8` · 본문 요약) · `PspCopyAndFixupParameters` (원본 = `PspCaptureUserProcessParameters` 사본 모양 · `Dst` 크기 `0x20000` 올림 조건 · 안 고치는 칸 · `AttributeList` · `+0x11` · SegmentHeap) · `PspPrepareSystemDllInitBlock` (`+0xb8/+0xc0` · `+0xf0` · `Rdx` 이름) · 짝 표 (`Flags` 마스크 뜻 · `Q63 (d)` → Q63b 표시 셋)), 2.3 (`PspInitializeThunkContext` `Rdx` 칸 이름 · 부팅 절 ScpCfg 한 줄), 2.4 (`LdrpInitialize` 비트 31 줄), 4장 (2.1 Q63 줄 (a)(b)(c)(e)(f) 해결됨 · run60 정적 ① 해결됨 · run32 블록 칸 줄 메모 · run104 새 줄 셋 — 동적 · 정적 · PE_emulator), 4.1 (그림 1 `MiMapProcessExecutable` 줄), 5장 (run104 · sast/104).
- baggage.md: 기준 (run104), 판 (쉰넷째판), 1장 그림 한 마디, 2장 합 문장 (3.2 짐작 22 → 정확), 3장 머리 메모, 7.4 (3.2 22칸 줄 해결됨), 3.2 (머리 숫자 · 첫 줄 근거 고침 · 새 줄 7), 3.9 `LdrSystemDllInitBlock` (`+0xb8/+0xc0` 출처 · `+0xf0` 내용), 4장 짝 (블록 `+0xb8` 출처 · `Rdx` 칸 이름), 7.6.4 (커널 전역 `PspNtdllScpFunctions` 넷), 새 7.36, 8장.

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 중 디버거 칸을 읽는 것은 없다 `[정적: 읽은 줄 안에서]`. ScpCfg 전역은 부팅 때 정해지므로 디버거로 볼 때 멈춤점이 아니라 **읽기** (동적 4).
- 도구: 질문 4 같은 "칸에 비트 켜는 줄 전수" 는 Ghidra `_refs` 로 못 잡는다 (레지스터+오프셋). 결과서는 `objdump` 꼴 검색으로 했고 한계를 적었다 — flow.md 기준의 run41 · run33 방식과 같아 pitfalls 제안은 안 한다.
