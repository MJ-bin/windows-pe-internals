# 2026-11-07 run70 — CPU 기능 감지 (Q133, PE_emulator 요청 21): `SymCryptDetectCpuFeaturesByCpuid` 는 **CPUID 만 4 ~ 6번** 부르는 잎 함수 (XGETBV·syscall 없음), KUSD `ProcessorFeatures` 64칸은 **부팅 때 커널 함수 넷이 28칸만** 쓴다 (`KiSetFeatureBits` 는 안 씀), `LdrpTouchThreadStack` 은 test.exe 에선 **안 탄다** (PEB `+0x318` = 0)

## PE_emulator 쪽에서 쓸 것

### 한 줄 결론 (셋)

1. **`SymCryptDetectCpuFeaturesByCpuid` 를 진짜로 돌려도 Unicorn 이 CPUID 리프 0·1·7 (서브리프 0) 만 맞게 답하면 된다** `[정적]`.
   - `XGETBV` 없음, syscall 없음, KUSD 안 읽음, `RtlProcessorFeaturesBuffer` 안 읽음.
   - 메모리 입력은 ntdll 안 읽기 전용 표 **`cpuidBitInfo` 160B** (`ntdll+0x186c10`, `.rdata`) 하나 (+ 늘 있는 `__security_cookie`).
   - 쓰는 곳은 `.data` 20바이트뿐: `g_SymCryptCpuFeaturesNotPresent` (`+0x1cd7b0` 4B) · `g_SymCryptCpuid1` (`+0x1d33f0` 16B).
   - 덫으로 둘 거면 이 두 전역만 아래 표대로 채우면 된다. 부르는 쪽 `LdrpInitializeProcess` 가 바로 뒤에 `g_SymCryptFlags` 비트 0 과 `g_SymCryptCpuFeaturesPresentCheck` (= NOT) 를 쓴다.
   - 같은 구간의 `LdrpInitializeProcess` **자신도 CPUID 를 둘 더** 부른다 (`+0x1d45` 리프 1, `+0x1dbb` 리프 7 조건부) — 이것도 Unicorn 몫. 그리고 `+0x1d99 RtlIsProcessorFeaturePresent(0x29)` 가 KUSD `+0x29d` (번호 41) 한 바이트를 읽는다.
2. **KUSD `ProcessorFeatures` 를 CPU 에서 셈하려면 규칙은 "CPUID → `KeFeatureBits` → KUSD 번호"** (아래 규칙 표) `[정적]`. 쓰는 곳은 부팅 때 커널 넷 (`KiInitializeKernel` 24칸 · `KiPublishProcessorFeatures` 2칸 · `KiInitializeNxSupportDiscard` 1칸 · `InitBootProcessor` 1칸 = **28칸**). 나머지 **36칸은 쓰는 줄이 안 나왔다** (0).
   - 한계: "쓰는 줄 없음" 은 **두 모양으로만** 훑은 결과다 (`MmWriteableSharedUserData` 포인터 + disp, 절대 주소 `0xFFFFF780000002xx`). 다른 레지스터로 들고 다니는 포인터는 놓칠 수 있다.
   - `[관찰]` run44 사진: 64칸 중 1 인 24칸이 **모두** 28칸 안에 있고, 36칸은 **모두 0** 이다 (아래 "사진과 맞춰 본 것"). 규칙과 어긋나는 칸이 없다.
3. **`LdrpTouchThreadStack` 은 test.exe 에서 안 탄다** — PEB `+0x318 MinimumStackCommit` 이 0 이라 `_LdrpInitialize +0x19a JZ` 가 건너뛴다.
   - `[관찰]` run44 사진 8장 모두 0 (마지막 P2 = `NtContinue` 입구, 읽는 줄 `+0x190` 보다 뒤).
   - 탄다면: 스택 맨 위 한 쪽 아래부터 4KiB 마다 **4바이트 읽기만** 하며 내려가고, syscall 은 `NtQueryVirtualMemory` (클래스 0, 길이 `0x30`) 1번. 예외가 나면 `0xC0000017` 을 돌려주고 프로세스 초기화가 실패한다.

### CPUID 4 ~ 6번 = 명령 자리 5곳 (결과서 "4 ~ 6번" 과 `.asm` 5자리 맞추기)

- `.asm` 의 `CPUID` 명령은 **5자리** (`+0x3b` · `+0x5a` · `+0x9d` · `+0xbe` · `+0xec`) 이고, 그중 **루프 안 `+0x5a` 는 두 번 탄다** (표 줄의 리프가 직전 리프와 다를 때만 — 표가 리프 1 줄 9개 다음 리프 7 줄 11개라 리프 1 한 번, 리프 7 한 번. 직전 리프 `EDI` 는 `+0x25 XOR EDI,EDI` 로 0 에서 시작). 그래서:
  - 최소 **4번** = `+0x3b` (리프 0) + `+0x5a` ×2 (리프 1, 7) + `+0xec` (리프 1).
  - **5번** = + `+0x9d` (리프 0) — 루프 뒤 AES-NI 가 "있음" 일 때 (`+0x93 TEST R8B,4` · `+0x97 JNZ +0xde` 안 탐). 제조사와 상관없음.
  - 최대 **6번** = + `+0xbe` (리프 1) — 그 위에 제조사 글자가 `AuthenticAMD` 일 때만 (`+0x9f ~ +0xb5`).

| 줄 | 리프 (EAX) / 서브리프 | 몇 번 | 조건 | 결과를 어디에 |
|---|---|---|---|---|
| `+0x3b` | 0 / 0 | 1 | 늘 | `ESI` = 최대 기본 리프, 지역 `[RBP-0x20..-0x14]` |
| `+0x5a` | 표 줄 리프 (1, 7) / 0 | 2 | 리프가 바뀔 때 (`+0x50 CMP R10D,EDI` · `+0x53 JZ +0x6b`) | 지역 4칸. 리프 > `ESI` 면 결과를 버리고 "없음" (`+0x6b`·`+0x6e JG +0x82`) — CPUID 는 그 앞이라 일단 부름 |
| `+0x9d` | 0 / 0 | 0 ~ 1 | AES-NI 있음 | `EBX`·`ECX`·`EDX` 를 `"Auth"`·`"cAMD"`·`"enti"` 와 비교 |
| `+0xbe` | 1 / 0 | 0 ~ 1 | 위 + AMD | 가문 합 `< 0x15` 이면 `+0xda OR R8D,4` (AES-NI 를 "없음"으로) |
| `+0xec` | 1 / 0 | 1 | 늘 | `g_SymCryptCpuid1` `+0x0` EAX (`+0xee`) · `+0x4` EBX (`+0xf4`) · `+0x8` ECX (`+0xfa`) · `+0xc` EDX (`+0x100`) |

### "없음" 비트 표 (`cpuidBitInfo` 20줄, 결과서 1-3 그대로 줄임)

- 모으는 레지스터 `R8D` 는 **`0xffffc020` 에서 시작** (`+0x30`). 표에 없는 비트 5·14 ~ 31 은 늘 "없음".
- 줄마다 CPUID 비트가 **꺼져 있으면** (`+0x7d BT` · `+0x80 JC` 안 탐) 그 줄 마스크를 `OR` (`+0x82`). 다 돌고 **`+0xe3` 에서 딱 한 번** `g_SymCryptCpuFeaturesNotPresent` 에 쓴다.

| 없음 비트 (마스크) | 켜지려면 CPUID 가 꺼져야 하는 비트 (하나라도) | 이름 `[추측: 공개 CPUID 문서]` |
|---|---|---|
| 0 (`0x1`) · 1 (`0x2`) | 0 = 리프 1 EDX[25]·EDX[26]. 1 = 그 둘 + ECX[0]·ECX[9] | SSE·SSE2 / + SSE3·SSSE3 |
| 2 (`0x4`) | 리프 1 ECX[25] (+ AMD 가문 합 < `0x15`) | AES-NI |
| 3 (`0x8`) | 리프 1 ECX[1] | PCLMULQDQ |
| 4 (`0x10`) | 리프 1 ECX[28] · 리프 7 EBX[5] | AVX·AVX2 |
| 6 (`0x40`) | 리프 7 EBX[29] | SHA |
| 7 (`0x80`) | 리프 7 EBX[8] | BMI2 |
| 8 (`0x100`) | 리프 7 EBX[19] | ADX |
| 9 (`0x200`) | 리프 1 ECX[30] | RDRAND |
| 10 (`0x400`) | 리프 7 EBX[18] | RDSEED |
| 11 (`0x800`) | 리프 7 ECX[9] · ECX[10] | VAES·VPCLMULQDQ |
| 12 (`0x1000`) | 리프 7 EBX[16]·[31]·[30]·[17] | AVX512 F·VL·BW·DQ |
| 13 (`0x2000`) | 리프 1 ECX[13] | CMPXCHG16B |

### KUSD `ProcessorFeatures` 규칙 표 (CPUID → `KeFeatureBits` → KUSD 번호)

- "FB1" = `KeFeatureBits` (nt rva `0xfc6b70`, = PRCB `+0x8fa0` 를 BSP 가 `KiInitializeKernel +0x382` 에 옮긴 것) 의 비트, "FB2" = `KeFeatureBits2` (`0xfc6ba0`). CPUID → FB 는 `RtlDetectProcessorFeatures` 가 표 `KiCpuFeatureTable` (nt rva `0x8800`, 59줄) 로 모은다.
- 번호 = `0x7ffe0274 + 번호` 바이트 (0 또는 1). `PF_*` 이름은 `[추측: winnt.h 기억]`.

| 번호 | 쓰는 함수 · 줄 | 조건 | 그 조건의 출처 | `PF_*` `[추측]` |
|---|---|---|---|---|
| 2 · 3 · 6 · 8 · 9 · 10 · 14 | `KiInitializeKernel +0x5a3` · `+0x5b1` · `+0x5bf` · `+0x5cd` · `+0x5db` · `+0x5e9` · `+0x5f7` | 없음 (`SIL` = 1) | — | CMPXCHG8B · MMX · SSE · RDTSC · PAE · SSE2 · CMPXCHG16B |
| 23 | `InitBootProcessor +0x406` | `R15B` = 1 (`+0x16b`) `[추측: 갈래 둘을 안 따라감]` | — | FASTFAIL |
| 12 | `KiInitializeNxSupportDiscard +0x170` | `KiIsNXSupported` 참 (`+0x144/+0x149`, `+0x14b JZ`) | EFER `0xC0000080` 비트 11 을 켬 (`+0x152 RDMSR` ~ `+0x167 WRMSR`) | NX_ENABLED |
| 13 | `KiInitializeKernel +0x60c` | FB1 19 | 리프 1 ECX[0] | SSE3 |
| 7 | `+0x621` | FB1 14 | 리프 `0x80000001` EDX[31] | 3DNOW |
| 17 | `+0x636` | FB1 23 | CPUID 표에 없음 — KUSD `+0x3d8` 가 0 이 아니면 `+0x2e5 BTS RBX,0x17` | XSAVE_ENABLED |
| 22 | `+0x64b` | FB1 28 | 리프 7 EBX[0] | RDWRFSGSBASE |
| 20 | `+0x660` | FB1 26 | CPUID 표에 없음 — `KiSetFeatureBits` (Intel: VMX + MSR `0x482`·`0x48b`, AMD: 리프 `0x8000000a` EDX[0]) | SECOND_LEVEL_ADDRESS_TRANSLATION |
| 21 | `+0x675` | FB1 27 | CPUID 표에 없음 — `KiSetFeatureBits` (Intel: MSR `0x3a`, AMD: MSR `0xc0010114`) | VIRT_FIRMWARE_ENABLED |
| 28 | `+0x68a` | FB1 32 | 리프 1 ECX[30] | RDRAND |
| 32 | `+0x69f` | FB1 34 | 리프 `0x80000001` EDX[27] | RDTSCP |
| 36 · 37 · 38 | `+0x6b4` · `+0x6c9` · `+0x6de` | FB1 47 · 48 · 49 | 리프 1 ECX[9] (콜백 `RtlHandleSSSE3Override`) · ECX[19] · ECX[20] | SSSE3 · SSE4_1 · SSE4_2 |
| 39 | `+0x708` | FB1 50 **그리고** KUSD `+0x3d8` 비트 2 (`+0x6ef`) | 리프 1 ECX[28] | AVX |
| 40 | `+0x728` | FB1 51 **그리고** `+0x3d8` 비트 2 (`+0x70f`) | 리프 7 EBX[5] | AVX2 |
| 41 | `+0x750` | FB1 52 **그리고** `+0x3d8` 비트 5 ~ 7 중 하나 (`+0x738 TEST AL,0xe0`) | 리프 7 EBX[16] | AVX512F |
| 42 | `+0x765` | FB1 54 | 리프 7 EBX[9] | ERMS |
| 60 | `+0x77a` | FB1 57 | 리프 7 EBX[8] (콜백 `RtlHandleBMI2Override`) | BMI2 |
| 61 | `+0x798` | FB1 58 **그리고** `Feature_Test57481295 …IsEnabledDeviceUsageNoInline` (`+0x781/+0x786`) | 리프 7 ECX[28] | MOVDIR64B |
| 33 | `KiPublishProcessorFeatures +0x19` | FB2 1 (조건 없이 그 값을 씀) | 리프 7 ECX[22] | RDPID |
| 35 | `KiPublishProcessorFeatures +0x48` | FB2 13, 로더 플래그 `SuppressMonitorX` (`[KeLoaderBlock+0xf0]+0x84` 비트 13) 꺼짐 | 리프 `0x80000001` ECX[29] (AMD 만) | MONITORX |
| 0 · 1 · 4 · 5 · 11 · 15 · 16 · 18 · 19 · 24 ~ 27 · 29 ~ 31 · 34 · 43 ~ 59 · 62 · 63 (**36칸**) | 쓰는 줄 안 나옴 | — | — | — |

- AVX 셋 (39·40·41) 은 CPUID 만으로 안 된다: KUSD `+0x3d8` (`XState.EnabledFeatures`) 도 맞춰야 한다. 그 칸은 `entry +0x1e8 KiInitializeXSaveConfiguration` 이 쓴다고 보이지만 본문은 안 읽었다 `[공백]`.
- 표 59줄 중 KUSD 에 닿는 건 위 줄들뿐. 나머지 (리프 6·리프 7 서브리프 1·2·`0x80000008`·`0x80000021` 등) 는 FB2 다른 비트로 간다 (KUSD 아님). 필수 줄 5개 (CMPXCHG16B·SSE4.2·LAHF·SYSCALL·NX(Intel)) 가 없으면 `KeBugCheckEx(0x5d, 0xffffffff, …)`.

### 작은 표 (결과서 표를 줄임)

| 함수 | 입력 (CPUID / XGETBV / 메모리) | 쓰는 칸 | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|
| ntdll `SymCryptDetectCpuFeaturesByCpuid` (`+0x11f42c`, 295B) | 리프 0·1·7 + (0) + (1) / 없음 / `cpuidBitInfo` 160B · `__security_cookie` | `+0x1cd7b0` 4B · `+0x1d33f0` 16B | `__security_check_cookie` 1번 | 없음 | 없음 (`void`) |
| ntdll `LdrpTouchThreadStack` (`+0x8bdc4`, 180B — test.exe 안 부름) | 없음 / 없음 / TEB `+0x8 StackBase` · `+0x10 StackLimit`, 스택 쪽 읽기 | 스택 지역 `0x30`B 뿐 | `NtQueryVirtualMemory` 1번 · 예외 때 `LdrpGenericExceptionFilter` | 1번 (클래스 0, 길이 `0x30`) | 0 / 실패 상태 / 예외면 `0xC0000017` |
| ntoskrnl `RtlDetectProcessorFeatures` (부팅) | 표 59줄의 리프 / 이 함수엔 없음 (콜백 본문은 안 읽음) / `KiCpuFeatureTable` | PRCB `+0x8fa0` FeatureBits · FeatureBits2 · 제조사 플래그 | `RtlGetProcessorSignature` · `RtlpGetSpecificProcessorFlags` · 표 콜백 · `KeBugCheckEx` | 없음 | 출력 셋 |
| ntoskrnl `KiPublishProcessorFeatures` (부팅) | `KeFeatureBits2` · 로더 확장 플래그 · PRCB `+0x8fa0` | KUSD `+0x295` · `+0x297` · `KeSmapEnabled` | 없음 | 없음 | 없음 |
| ntoskrnl `KiInitializeKernel` (KUSD 몫만, 부팅) | PRCB FeatureBits · KUSD `+0x3d8` · `Feature_Test57481295` | KUSD 24칸 | `KiEnableXSave` 등 | 없음 | — |

## 환경

- 26100.9457, 정적만 (VM 안 씀)
- 정적: ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE1`, ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서 — 기준 빌드와 같음). 두 모듈 다 다시 analyze 안 함.
- 버전 확인 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` (`_calls.tsv`), `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess` (`.asm`) 일치.
- 값 대조: run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_<K1 ~ P2>.txt`, `_supp_<K1 ~ P2>.txt`) 를 flow-writer 가 다시 읽었다. 새 동적 실행은 없다.

## 목적

- PE_emulator 117 은 `SymCryptDetectCpuFeaturesByCpuid` 를 공사 중 빈 함수로 두었다. 진짜 덫으로 바꿀 재료 (CPUID 횟수·리프, XGETBV, 쓰는 전역, 메모리 입력) 가 필요하다.
- PE_emulator 118 은 `LdrpTouchThreadStack` 본문이 필요하다.
- KUSD `ProcessorFeatures` 를 CPU 에서 셈할 때 쓸 규칙 (CPUID 비트 → 번호) 의 정적 근거가 필요하다 (PE_emulator 가 아직 고르지 않았을 수 있어 짧게).

## 방법

- 정적: `_workspace/sast/65_cpu-features.md`
  - ntdll `^SymCryptDetectCpuFeaturesByCpuid$` · `^LdrpTouchThreadStack$` (+ `$filt$0` · `LdrpGenericExceptionFilter`) depth 1 → 12개.
  - ntoskrnl `KiPublishProcessorFeatures` · `KiSetProcessorSignature` · `KiGetProcessorSignature` · `KiRestoreFeatureBits` · `RtlDetectProcessorFeatures` (+ callee) · `entry` (= `KiSystemStartup`) · `KiInitializeNxSupportDiscard` depth 0. 있던 `.asm` 재사용: `_LdrpInitialize` · `LdrpInitializeProcess` · `KiSetFeatureBits` · `KiInitializeKernel` · `KiInitializeBootStructures` · `InitBootProcessor` · `MmCreatePeb` · `LdrpInitializeExecutionOptions`.
  - 새 `_refs`: ntdll `g_SymCryptCpuFeaturesNotPresent` · `g_SymCryptCpuid1` · `cpuidBitInfo` · `g_SymCryptFlags` · `g_SymCryptCpuFeaturesPresentCheck`, ntoskrnl `MmRegistryState` · `MmWriteableSharedUserData` · `0xc80d40`.
  - 새 표 (바이너리 바이트에서 뽑음, 커밋 안 함): `ms/disassemble/ntdll/_tables/cpuidBitInfo.tsv` (20줄), `ms/disassemble/ntoskrnl/_tables/KiCpuFeatureTable.tsv` (59줄).
- 결과서: `_workspace/sast/65_cpu-features_result.md` (**분석성공**).
- 오케스트레이터가 `.asm` 으로 한 번 더 본 줄: `SymCryptDetectCpuFeaturesByCpuid` 안 `CPUID` 5자리 · `XGETBV` 0자리 · `+0xe3 MOV [g_SymCryptCpuFeaturesNotPresent],R8D` (한 번만); `LdrpInitializeProcess +0x1de6 TEST [g_SymCryptFlags],DIL` · `+0x1ded JNZ` · `+0x1def/+0x1df4 CALL` · `+0x1df4 OR.LOCK [g_SymCryptFlags],EDI`; `LdrpTouchThreadStack +0x43 XOR R8D,R8D` (클래스 0) · `+0x4a OR RCX,-1` · `+0x4e/+0x53 NtQueryVirtualMemory`.
- flow-writer 가 `.asm` 으로 한 번 더 본 줄:
  - `SymCryptDetectCpuFeaturesByCpuid` 전체 (`+0x0 ~ +0x126`) — `+0x25 XOR EDI,EDI` (루프 안 CPUID 가 두 번인 근거), `+0x93 ~ +0xda` 갈래.
  - `LdrpInitializeProcess +0x1d3c` · `+0x1d41` · `+0x1d45` · `+0x1d99` · `+0x1dbb` · `+0x1e1f ~ +0x1e2d`. `_LdrpInitialize +0x190 ~ +0x1a5`. `LdrpTouchThreadStack +0x23` · `+0x46` · `+0x57 ~ +0x67` · `+0x7f`.
  - ntoskrnl: `entry +0x1b5` · `+0x1e8` · `+0x27e`, `KiInitializeBootStructures +0x219` · `+0x27e` · `+0x370` · `+0x382`, `KiSetProcessorSignature +0x2e` · `+0x58` · `+0x5f`, `KiInitializeKernel +0x2e5` · `+0x382` · `+0x585` · `+0x5a3` · `+0x6ef` · `+0x76c` · `+0x781` · `+0x7fe`, `KiPublishProcessorFeatures +0x19` · `+0x48`, `KiInitializeNxSupportDiscard +0x144` · `+0x169` · `+0x170`, `InitBootProcessor +0x16b` · `+0x3ff` · `+0x406`, `MmCreatePeb +0x4c6` · `+0x4cc`, `RtlDetectProcessorFeatures +0x17b` · `+0x23c` · `+0x541`, `KiSetFeatureBits +0x888`.
- 로그: 없음 (정적만). 값 대조만 run44 덤프 읽기 로그.

## 결과

### 정적 — 호출 지점 (`_calls.tsv`·`.asm`)

ntdll (test.exe 첫 스레드):

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x1d99` | `+0x1d9e` | `ntdll!RtlIsProcessorFeaturePresent(0x29)` | 뒤10 (run59) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1def` | `+0x1df4` | `ntdll!SymCryptDetectCpuFeaturesByCpuid` | `g_SymCryptFlags` 비트 0 꺼짐 (`+0x1de6 TEST` · `+0x1ded JNZ +0x1e2d` 안 탐) — 프로세스당 한 번 | [정적] |
| `ntdll!SymCryptDetectCpuFeaturesByCpuid` | `+0x10d` | `+0x112` | `ntdll!__security_check_cookie` | 늘 (유일한 하위 호출) | [정적] |
| `ntdll!_LdrpInitialize` | `+0x19c` | `+0x1a1` | `ntdll!LdrpTouchThreadStack(RCX = PEB+0x318)` | `+0x197 TEST RCX,RCX` · `+0x19a JZ +0x1e3` — **test.exe 안 탐** | [정적] + 사진 0 `[관찰]` |
| `ntdll!LdrpTouchThreadStack` | `+0x4e` | `+0x53` | `ntdll!NtQueryVirtualMemory(-1, StackLimit, 0, &지역, 0x30, &길이)` | 늘 (위가 탈 때) | [정적] |
| `ntdll!LdrpTouchThreadStack$filt$0` | `+0x10` | `+0x15` | `ntdll!LdrpGenericExceptionFilter` | 루프 (`+0x7a ~ +0x95`) 안에서 예외 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x531` | `+0x536` | `ntdll!LdrpInitializeExecutionOptions` → `+0x401/+0x406 RtlQueryImageFileKeyOption("MinimumStackCommitInBytes")` | IFEO 키 열림 + 매개변수 `Flags & 0x4000` 꺼짐 — test.exe 는 `0x4001` 이라 안 닿음 | [정적] |

- 결과서 1-6 은 `+0x1d99 RtlIsProcessorFeaturePresent(0x29)` 로 call 자리만 적었다. ret `+0x1d9e` 는 `.asm` (`CALL … ret=+0x1d9e`).

ntoskrnl (부팅 — 흐름도 범위 앞, BSP):

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `nt!entry` (= `KiSystemStartup`) | `+0x1b5` | `+0x1ba` | `nt!KiInitializeBootStructures` | 늘 | [정적] |
| `nt!KiInitializeBootStructures` | `+0x219` | `+0x21e` | `nt!KiGetProcessorSignature` | 늘 | [정적] |
| `nt!KiInitializeBootStructures` | `+0x27e` | `+0x283` | `nt!KiSetProcessorSignature` | BSP 만 (`+0x270 TEST EBP,EBP` · `+0x272 JNZ +0x35a`) | [정적] |
| `nt!KiSetProcessorSignature` | `+0x2e` | `+0x33` | `nt!RtlDetectProcessorFeatures` | 늘 | [정적] |
| `nt!KiSetProcessorSignature` | `+0x5f` | `+0x64` | `nt!KiPublishProcessorFeatures` | BSP 만 (`+0x4c TEST EAX,EAX` · `+0x4e JNZ +0x6b`), 그 앞 `+0x58 KeFeatureBits2 \|=` | [정적] |
| `nt!KiInitializeBootStructures` | `+0x370` | `+0x375` | `nt!KiInitializeNxSupportDiscard` | 프로세서 번호 0 (`+0x364 MOV EAX,GS:[0x1a4]` · `+0x36e JNZ +0x375`) | [정적] |
| `nt!KiInitializeBootStructures` | `+0x382` | `+0x387` | `nt!KiSetFeatureBits` | 늘 | [정적] |
| `nt!entry` | `+0x1e8` | `+0x1ed` | `nt!KiInitializeXSaveConfiguration` | 늘 (본문 안 읽음 — KUSD `+0x3d8` `[추측]`) | [정적] |
| `nt!entry` | `+0x27e` | `+0x283` | `nt!KiInitializeKernel` | 늘 | [정적] |
| `nt!KiInitializeKernel` | `+0x585` | `+0x58a` | `nt!KiEnableXSave` | 늘 | [정적] |
| `nt!KiInitializeKernel` | `+0x7fe` | `+0x803` | `nt!InitBootProcessor` | BSP 만 (`+0x7f6 TEST R15D,R15D` · `+0x7f9 JNZ +0x805`) | [정적] |
| `nt!RtlDetectProcessorFeatures` | `+0x23c` | `+0x241` | (간접 CFG) `KiCpuFeatureTable` 줄 `+0x20` 콜백 | 콜백이 있는 줄 (표 2 `RtlHandleSSSE3Override` · 표 19 `RtlHandleBMI2Override`) | [정적] |
| `nt!RtlDetectProcessorFeatures` | `+0x541` | `+0x546` | (간접 CFG) `@0x9158` — MSR 표 `KiMsrFeatureTable` 쪽 콜백 `[추측]` | — | [정적] |

### 호출 관계

```
(부팅, BSP — 흐름도 범위 앞)
entry (= KiSystemStartup)
 ├─ (1) +0x1ba  KiInitializeBootStructures
 │    ├─ (1) +0x21e  KiGetProcessorSignature
 │    ├─ (2) +0x283  KiSetProcessorSignature                    BSP 만
 │    │    ├─ (1) +0x33  RtlDetectProcessorFeatures             CPUID × 표 59줄 → PRCB+0x8fa0 FeatureBits · FeatureBits2
 │    │    │       +0x58  KeFeatureBits2 |= …
 │    │    └─ (2) +0x64  KiPublishProcessorFeatures             BSP 만 → KUSD +0x295 (33) · +0x297 (35) · KeSmapEnabled
 │    ├─ (3) +0x375  KiInitializeNxSupportDiscard               프로세서 0 만 → KUSD +0x280 (12)
 │    └─ (4) +0x387  KiSetFeatureBits                           PRCB FeatureBits 에 기본 묶음·가상화 비트, KeProcessorFeaturesBuffer 0 (+0x888, run65)
 ├─ (2) +0x1ed  KiInitializeXSaveConfiguration                  KUSD +0x3d8 XState [추측] — 안 읽음
 └─ (3) +0x283  KiInitializeKernel
      │    +0x382  KeFeatureBits ← RBX (BSP)
      ├─ (1) +0x58a  KiEnableXSave
      │    +0x5a3 ~ +0x798  KUSD ProcessorFeatures 24칸 (조건 없이 7 + FB1 조건 17)
      └─ (2) +0x803  InitBootProcessor                          BSP 만 → +0x406 KUSD +0x28b (23)

(test.exe 첫 스레드)
_LdrpInitialize
 ├─ (1) +0x16c  LdrpInitializeProcess
 │    ├─ +0x536  LdrpInitializeExecutionOptions                 (+0x413 PEB+0x318 ← IFEO 값, 더 클 때만 — test.exe 안 닿음)
 │    │  … (뒤10) +0x1d45 CPUID(1) · +0x1d9e RtlIsProcessorFeaturePresent(0x29) · +0x1dbb CPUID(7) (조건부)
 │    ├─ +0x1df4  SymCryptDetectCpuFeaturesByCpuid               g_SymCryptFlags 비트 0 꺼짐일 때만
 │    │    └─ +0x112  __security_check_cookie
 │    │  +0x1df4 OR.LOCK g_SymCryptFlags |= 1 · +0x1e27 g_SymCryptCpuFeaturesPresentCheck = ~NotPresent
 │    └─ +0x1e2d  정상 반환
 └─ (2) +0x1a1  LdrpTouchThreadStack                            PEB+0x318 ≠ 0 일 때만 — test.exe 안 탐
      └─ +0x53  NtQueryVirtualMemory (클래스 0)
           (예외 → $filt$0 +0x15 LdrpGenericExceptionFilter → 착지 +0x95 → 0xC0000017)
```

- 이 트리의 (n) 은 이 절에서만 쓴 번호다 (흐름도 그림 번호와 다름). 부팅 쪽은 흐름도 범위 앞이라 그림에 안 넣는다.

### 질문별 요약 (근거 줄은 결과서 해당 절)

| 질문 | 답 | 닫힘? |
|---|---|---|
| 1 SymCrypt | CPUID 만, 4 ~ 6번, XGETBV·syscall·KUSD 없음. 쓰는 전역 둘 (+ 부르는 쪽이 둘 더). 부르는 조건 `g_SymCryptFlags` 비트 0 | 정적으로 닫힘. VM 값은 run44 사진으로 이미 맞춤 (아래) |
| 2 KUSD `ProcessorFeatures` | `KiSetFeatureBits` **아님**. 쓰는 함수 넷, 28칸 / 36칸 쓰는 줄 없음. 규칙 = CPUID → FB → 번호 | 규칙 정적으로 닫힘 (스캔 한계 있음). VM 64칸은 사진으로 맞춤 |
| 3 `LdrpTouchThreadStack` | 읽기만 하는 스택 건드리기, syscall 1, 예외면 `0xC0000017`. test.exe 안 탐 | 정적 + 사진 `[관찰]` 로 닫힘 (안 불리는 것 멈춤점 확인만 남음) |

## 확인된 것

- `[정적]` `SymCryptDetectCpuFeaturesByCpuid` 는 인자 없는 `void` 잎 함수다. 하위 호출은 `__security_check_cookie` (`+0x10d/+0x112`) 하나, `SYSCALL`·`INT`·`XGETBV` 명령 없음 (결과서 1-1·1-5, `.asm` 전체).
- `[정적]` CPUID 서브리프는 모두 0 (`XOR ECX,ECX` 가 매번 앞에). 명령 자리 5, 실행 4 ~ 6번 (위 "CPUID 4 ~ 6번" 절).
- `[정적]` `g_SymCryptCpuFeaturesNotPresent` 는 `+0xe3` 에서 **한 번만** 쓴다. 다른 읽는 곳은 `LdrpInitializeProcess +0x1e1f` 하나 (`_refs`).
- `[정적]` `g_SymCryptCpuid1` 16B 는 마지막 CPUID(1) 의 EAX·EBX·ECX·EDX 를 차례로 (`+0xee` · `+0xf4` · `+0xfa` · `+0x100`). 읽는 곳은 `_refs` 에 없다.
- `[정적]` `g_SymCryptFlags` 는 `.data` 0 으로 시작하고, 쓰는 줄은 `LdrpInitializeProcess +0x1df4 OR.LOCK [..],EDI` (`EDI` = 1, `+0x1d3c`) 하나, 읽는 줄은 `+0x1de6` 하나 → **프로세스당 한 번** 탄다. 켜는 비트는 **비트 0 (값 1)** 이다.
- `[정적]` `+0x1dfb ~ +0x1e1d` 는 SymCrypt 버전 글자를 스택 한 칸에 옮기는 루프 (CPUID 와 무관, 짐 아님 `[추측]`).
- `[정적]` KUSD `ProcessorFeatures` 를 쓰는 함수는 `KiInitializeKernel` · `KiPublishProcessorFeatures` · `KiInitializeNxSupportDiscard` · `InitBootProcessor` 넷. `KiSetFeatureBits` 에는 KUSD `+0x274` 쓰기 줄이 없다 (그 함수는 PRCB `+0x8fa0` 와 `KeProcessorFeaturesBuffer` 몫, run65).
- `[정적]` KUSD 를 쓰는 포인터는 `MmWriteableSharedUserData` (nt `0xfc5008`) 에서 읽는다 (`KiInitializeNxSupportDiscard +0x169`, `InitBootProcessor +0x3ff`).
- `[정적]` AP (다른 프로세서) 도 `KiInitializeKernel` 의 KUSD 쓰기 줄을 다시 지나간다. 그 앞 `+0x42c ~ +0x54a` 에서 BSP 의 `KeFeatureBits` 와 다르면 빠진다 (도착 줄은 안 읽음 — bugcheck 쪽 `[추측]`).
- `[정적]` `LdrpTouchThreadStack`: `RBX` = 최소 커밋 → TEB (`GS:[0x30]`, `+0x23`) → `NtQueryVirtualMemory(-1, StackLimit, 0, &MBI, 0x30, &길이)` → 아래 한계 = `AllocationBase + 0x3000` (`+0x57 ~ +0x5c`) → 시작 = `StackBase − 0x1000` (`+0x63 ~ +0x67`) → 한계 = `max(시작 − 최소커밋, AllocationBase + 0x3000)` → 루프 `+0x7f MOV EAX,[RAX]` 로 한 쪽에 4바이트씩 읽기 (쓰기 없음). 정상이면 0.
- `[정적]` 예외 범위 `+0x7a ~ +0x95` (`.pdata`), 필터 `LdrpTouchThreadStack$filt$0` → `LdrpGenericExceptionFilter` (보통 `EXCEPTION_EXECUTE_HANDLER`), 착지 `+0x95 MOV EAX,0xC0000017`. 반환이 음수면 `_LdrpInitialize +0x227 JS` → `LdrpInitializationFailure` → `NtTerminateProcess`.
- `[정적]` PEB `+0x318 MinimumStackCommit` 을 쓰는 줄은 둘: 커널 `MmCreatePeb +0x4cc` (값 = `MmRegistryState+0x28`, 레지스트리 `Session Manager\Memory Management` 의 `MinimumStackCommitInBytes`, 표 `0xc80d40` 은 `CmpGetSystemControlValues +0x27a` 가 읽음, 기본값 없음) · ntdll `LdrpInitializeExecutionOptions +0x413` (IFEO 값이 지금 PEB 값보다 클 때만). ntdll 전체에서 `[reg+0x318]` 쓰기 모양은 이 하나 (결과서 3-5). exe 헤더 `SizeOfStackCommit` 이 이 칸을 채우는 줄은 안 나왔다.

## 사진과 맞춰 본 것 (run44 `[관찰]`, 새 실행 없음)

flow-writer 가 덤프 읽기 로그를 다시 읽었다 (ntdll 바탕 `0x7ffafb660000`).

- **KUSD `ProcessorFeatures` 64칸** — 커널 별칭 `fffff780'00000270 ~ 2b0` 줄이 **8장 모두 같다** (`_supp_<K1 ~ P2>.txt` 326 ~ 330줄, 유저 주소 `7ffe0270` 은 `_P1a.txt:3034 ~ 3038` · `_P1b.txt:4152 ~ 4156` · `_P2.txt:4164 ~ 4168` 이 같은 값). 1 인 칸 **24**: 2·3·6·8·9·10·12·13·14·17·21·22·23·28·32·33·36·37·38·39·40·41·42·60.
  - 24칸이 **모두** 정적 28칸 안에 있다. 28칸 중 0 인 넷 = 7 (3DNOW) · 20 (SLAT) · 35 (MONITORX) · 61 (MOVDIR64B) — 모두 조건부 칸.
  - 정적 "쓰는 줄 없음" 36칸은 **모두 0**. (0 이라는 건 "아무도 안 씀" 의 증거는 아니다 — 0 을 썼을 수도 있다.)
  - 값은 K1 (`PspAllocateThread` 입구) 이전부터 끝까지 그대로 → 부팅 때 놓인 짐이라는 정적 결론과 맞다.
- **KUSD `+0x3d8` `XState.EnabledFeatures`** = `0xe7` (비트 0·1·2·5·6·7, `_supp_K1.txt:348`, `_P2.txt:4186`) → 39 (AVX) 의 비트 2, 41 (AVX512F) 의 비트 5 ~ 7 조건이 맞고, 사진 39·41 = 1 과 맞다. 17 (XSAVE_ENABLED, `+0x3d8 ≠ 0`) = 1 도 맞다.
- **`g_SymCryptCpuid1`** (`0x1d33f0`): P1a·P1b 0 → **P2 EAX `0x00a60f12` · EBX `0x02000800` · ECX `0xfef83203` · EDX `0x078bfbff`** (`_P1a.txt:5305`, `_P1b.txt:6423`, `_P2.txt:6435`).
  - 리프 1 ECX 비트 0·9·19·20·28·30 이 켜졌다 → KUSD 13·36·37·38·39·28 = 1 과 맞다 (같은 VM, 같은 CPU).
  - EAX 의 가문 = 기본 `0xf` + 확장 `0xa` = **`0x19`** → `[추측]` AMD 계열 (Intel 은 가문 6). 제조사 글자는 사진에 없다.
- **`g_SymCryptCpuFeaturesNotPresent`** (`0x1cd7b0`): P1a·P1b **`0xffffffff`** → **P2 `0xffffc020`** (`_P1a.txt:3829`, `_P1b.txt:4947`, `_P2.txt:4959`).
  - P2 값 = 시작값 그대로 → 표 20줄이 모두 "있음" 이었고 AMD 보정 (가문 `0x19` ≥ `0x15`) 도 안 탔다 — 정적 규칙과 맞다.
  - P1a 의 `0xffffffff` 는 `.data` 파일 쪽 (`0x1cc000 + 0x2000` 안) 처음 값으로 보인다 `[추측: 파일 바이트는 안 읽음]`.
- **`g_SymCryptFlags`** (`0x1d2230`) P2 = **1**, **`g_SymCryptCpuFeaturesPresentCheck`** (`0x1d2234`) P2 = **`0x3fdf`** = `~0xffffc020` (`_P2.txt:6151`, P1a·P1b 0 — `_P1a.txt:5021`, `_P1b.txt:6139`).
- `x86_cpu_enable_avx512` · `_ssse3` · `_simd` (`0x1d21e0` ~): P2 = 1 · `0x200` · 1 (`_P2.txt:6146`, P1a·P1b 0) — `RtlIsProcessorFeaturePresent(0x29)` = KUSD 41 = 1 과 맞다.
- **PEB `+0x318 MinimumStackCommit`** = 0, 8장 모두 (`_K1.txt:1634` · `_K2.txt:1635` · `_K3.txt:1640` · `_P0.txt:1642` · `_P1.txt:1642` · `_P1a.txt:1809` · `_P1b.txt:2507` · `_P2.txt:2519`).
  - K1 에서 이미 0 → 커널 `MmCreatePeb +0x4cc` 가 0 을 썼다 = `MmRegistryState+0x28` 이 0 `[관찰 간접]` (전역 값 자체는 사진에 없음).
  - ntdll `+0x413` 은 "지금보다 클 때만" 쓰므로 썼다면 0 이 아니었을 것 → 안 썼다 `[관찰 간접]`. 매개변수 `Flags 0x4001` (`_K1.txt:2337`, 비트 14 = IFEO 키 없음) 이라 IFEO 쪽 줄에 아예 안 닿는다는 정적 갈래와 맞다.

## 결과서와 같음 / 다르게 적은 곳 · 보탠 것

- 같음: 질문 1 ~ 3 의 결론, 표 두 개 (`cpuidBitInfo` 20줄, KUSD 규칙), 사슬 call/ret — `.asm` 줄이 결과서와 같다.
- (맞춤) 결과서 "CPUID 4 ~ 6번" ↔ `.asm` 명령 5자리: 루프 안 `+0x5a` 가 두 번 탄다 (위 절). 결과서 "한눈에" 의 "AMD 일 때만 0·1" 은 정확히는 **리프 0 (`+0x9d`) 은 AES-NI 가 있을 때 (제조사 무관), 리프 1 (`+0xbe`) 만 AMD 일 때** 다.
- (다르게) 결과서 "한눈에" 의 `KiInitializeKernel` 몸통 범위 `+0x58a ~ +0x77a` 는 2-1·2-3 의 쓰기 줄로는 **`+0x5a3 ~ +0x798`** 이다 (`+0x58a` 는 `KiEnableXSave` ret, 마지막 쓰기는 61번 `+0x798`). 이 문서·flow.md·짐 지도는 `+0x5a3 ~ +0x798` 로 적었다.
- (보탬) 결과서 "동적 1" (VM 값 대조) 의 KUSD 64칸·`+0x3d8`·SymCrypt 전역 넷은 **run44 사진에 이미 있다** (위 절). 남는 동적 몫은 `KeFeatureBits`·`KeFeatureBits2` 값과 콜백 효과뿐이다.
- (보탬) 결과서 "동적 4" (VM 제조사) 는 `g_SymCryptCpuid1` EAX 로 가문 `0x19` 까지는 사진으로 나온다 (제조사 이름은 `[추측]`).

## 정정

- 짐 지도 `baggage.md` 3.8 `g_SymCryptFlags` 줄의 "**비트 1** 켬" (run59, sast/54 결과서 표 "비트 1" 을 옮김) → **비트 0 켬 (값 1)**. `OR.LOCK [..],EDI` 의 `EDI` 가 `+0x1d3c MOV EDI,1` 이고 `+0x1de6 TEST [..],DIL` 도 같은 1 을 본다. `[관찰]` P2 값 1. run59 문서 자체엔 비트 번호가 없어 맨 위 정정 줄은 안 넣었다. 결과서 sast/54 는 고치지 않는다.
- 짐 지도 3.8 `g_SymCryptCpuid1` 줄의 "EAX · EBX" (`0x1d33f0`·`0x1d33f4`) → **16B 넷** (`+0x8` ECX `+0xfa`, `+0xc` EDX `+0x100` 보탬).

## 아직 확인 필요

동적 몫은 모두 **Q104 VM 단계**에 얹는다. ntdll 멈춤점은 test.exe 일 때만 걸리게 조건을 붙인다.

1. **커널 전역 값** (부팅이 끝난 뒤 아무 때나 한 번 읽으면 됨): `dq nt!KeFeatureBits L1` (nt rva `0xfc6b70`) · `dq nt!KeFeatureBits2 L1` (`0xfc6ba0`) · `dd nt+0xfc5218 L1` (`MmRegistryState+0x28`, 기대 0). FB1 비트 19·14·23·28·26·27·32·34·47 ~ 52·54·57·58 와 FB2 비트 1·13 이 사진 KUSD 값 (위) 과 맞는지. `KeFeatureBits` 비트 `0x37`·`0x38` 은 짐 지도 7.15·7.16·7.17·7.19 의 D1 과 같은 읽기라 한 번에 닫힌다.
2. **`LdrpTouchThreadStack` 이 안 불리는 것**: `ntdll+0x8c123` (`_LdrpInitialize+0x197`, `TEST RCX,RCX`) 에서 `r rcx` = 0, `ntdll+0x8bdc4` (`LdrpTouchThreadStack` 입구) 가 안 서는지.
3. (선택) SymCrypt 전역을 한 자리에서: `ntdll+0x8e231` (`LdrpInitializeProcess+0x1e2d`) 에서 `dd ntdll+0x1cd7b0 L1` · `dd ntdll+0x1d33f0 L4` · `dd ntdll+0x1d2230 L2` — 사진 P2 값과 같아야 한다.
4. (선택) VM CPU 제조사: `!cpuinfo` (가문 `0x19` 가 AMD 인지).

- 다음 바퀴 후보 (정적):
  - 콜백 둘 `RtlHandleSSSE3Override` · `RtlHandleBMI2Override` 본문 — FB1 비트를 지우는 조건 (KUSD 36·60 이 CPUID 만으로 안 정해질 수 있음).
  - `KiInitializeXSaveConfiguration` (`entry +0x1e8`) — KUSD `+0x3d8` XState 를 정말 여기서 쓰나, 어떤 값으로 (AVX 셋의 둘째 조건).
  - `InitBootProcessor +0x16b` `R15D` = 1 이 `+0x406` 까지 그대로인지 (갈래 둘).
  - `KiInitializeKernel +0x42c ~ +0x54a` AP 비교가 빠지는 도착 줄 (`0x140b6764a`).
  - `RtlDetectProcessorFeatures +0x541` 간접 호출 (`@0x9158`) — MSR 표 쪽인지.
  - `KiRestoreFeatureBits` (rva `0x4d92a8`, 314B) — 절전 복귀 때 KUSD 를 다시 쓰나 `[추측]`.
  - KUSD `+0x274` 36칸 "쓰는 줄 없음" 을 다른 모양 (레지스터에 오래 든 포인터, `REP STOSB` 류) 으로 한 번 더 훑기.
  - `MmRegistryState` 다른 18칸 (`_refs/MmRegistryState.tsv` 끝 `ptr` 줄) — 레지스트리 표 `0xc80d40` 가 채우는 칸.

## 도구 메모

- (결과서) **Ghidra `.c` 의 `cpuid*()` 결과는 칸 순서가 EAX, EBX, EDX, ECX** 다 (`[2]` = EDX, `[3]` = ECX). `.asm` 레지스터 이름이 정답이다. 근거: `KiSetFeatureBits.c` 의 `0x789f3fd` 비교가 리프 1 EDX 기본 비트 모양이고 `>> 0xd` 가 ECX CMPXCHG16B.
- (결과서) **`SymCryptDetectCpuFeaturesByCpuid.c` 는 `NotPresent = 0xffffc020` 쓰기를 함수 맨 앞에 그렸다.** `.asm` 에서는 `R8D` 에 모았다가 `+0xe3` 에서 한 번 쓴다. `g_SymCryptCpuid1 +0x8`/`+0xc` 도 `.c` 에선 EDX/ECX 가 뒤바뀌어 보인다 (`.asm`: `+0x8` = ECX, `+0xc` = EDX). 짐 지도·이 문서는 `.asm` 기준.
- windbg-kd-runbook pitfalls 에 추가 제안: "KUSD 를 유저 주소 `0x7ffe0000` 로 읽으면 K1 ~ P1 은 읽기 불가였다 (run44). 커널 별칭 `0xfffff78000000000` 으로 읽으면 8장 모두 읽힌다 — `ProcessorFeatures` 같은 부팅 칸은 별칭으로 읽을 것." (run44 문서에 이미 사실은 있음, 함정 목록에 한 줄로.)
- (결과서, 오케스트레이터 참고) `pgrep -fa analyzeHeadless` 는 `run_ghidra.sh` 의 Ghidra 를 못 잡는다 → `pgrep -fa 'ghidra.app.util.headless'`.
