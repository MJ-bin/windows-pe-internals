# 65 cpu-features — 정적 분석 결과

- 요청서: `_workspace/sast/65_cpu-features.md` (Q133, PE_emulator 요청 21)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`, pdb guid `C3093720177DA6851519DCFFE8DF53FE1`), ntoskrnl (같은 폴더, pdb guid `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준 빌드와 **같음**)
- analyze 는 다시 안 함 (두 모듈 모두 `.gpr` + `_index.tsv` + `SetPdb:` 줄 있음). export / refs 만.
- 뽑은 함수 (이번에 새로):
  - ntdll 12개 (정규식 4개, depth 1): `SymCryptDetectCpuFeaturesByCpuid` · `LdrpTouchThreadStack` · `LdrpTouchThreadStack$filt$0` · `LdrpGenericExceptionFilter` + 그 callee 8개 → `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`
  - ntoskrnl `KiPublishProcessorFeatures` · `KiSetProcessorSignature` · `KiGetProcessorSignature` · `KiRestoreFeatureBits` · `RtlDetectProcessorFeatures` (+ callee) · `entry` (= `KiSystemStartup`) · `KiInitializeNxSupportDiscard` (depth 0)
  - 이미 있던 `.asm` 재사용: `_LdrpInitialize`, `LdrpInitializeProcess`, `KiSetFeatureBits`, `KiInitializeKernel`, `KiInitializeBootStructures`, `InitBootProcessor`, `MmCreatePeb`, `LdrpInitializeExecutionOptions`
- 새 refs: ntdll `g_SymCryptCpuFeaturesNotPresent` · `g_SymCryptCpuid1` · `cpuidBitInfo` · `g_SymCryptFlags` · `g_SymCryptCpuFeaturesPresentCheck`, ntoskrnl `MmRegistryState` · `MmWriteableSharedUserData` · `0xc80d40` (레지스트리 표) → `ms/disassemble/*/_refs/`
- 새 표 파일 (바이너리 바이트를 읽어 만든 것, 커밋 안 함):
  - `ms/disassemble/ntoskrnl/_tables/KiCpuFeatureTable.tsv` — 커널 CPUID 규칙 표 59줄 전부
  - `ms/disassemble/ntdll/_tables/cpuidBitInfo.tsv` — SymCrypt 규칙 표 20줄 전부
- 버전 확인: `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치 (`_calls.tsv`), `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess` 일치 (`.asm`). 커널 PDB GUID 일치.

## 스크립트 문제

- 없음. 다만 읽을 때 조심할 것 둘 (스크립트 버그 아님, 해석 주의):
  - **Ghidra `.c` 의 `cpuid*()` 결과는 칸 순서가 EAX, EBX, EDX, ECX** 다 (`[2]` = EDX, `[3]` = ECX). `.asm` 의 레지스터 이름이 정답. 근거: `KiSetFeatureBits.c` 에서 `(uVar7 & 0x789f3fd) == 0x789f3fd` 가 리프 1 EDX 기본 비트 모양이고 `uVar18 >> 0xd` 가 ECX 의 CMPXCHG16B(비트 13) 라서.
  - `SymCryptDetectCpuFeaturesByCpuid.c` 는 `g_SymCryptCpuFeaturesNotPresent = 0xffffc020` 을 **함수 맨 앞에서 쓰는 것처럼** 그렸다. `.asm` 에서는 `R8D` 레지스터에서 모았다가 `+0xe3` 에서 **딱 한 번** 쓴다. `g_SymCryptCpuid1` 의 `+0x8`/`+0xc` 도 `.c` 에선 EDX/ECX 가 뒤바뀌어 보인다 (`.asm`: `+0x8`=ECX, `+0xc`=EDX).
- 참고 (오케스트레이터): `pgrep -fa analyzeHeadless` (소문자) 는 `run_ghidra.sh` 가 쓰는 `launch.sh ... ghidra.app.util.headless.AnalyzeHeadless` 를 못 잡는다 (자기 자신의 `bash -c` 만 잡힘). 확인하려면 `pgrep -fa 'ghidra.app.util.headless'`. 이번엔 다른 사용자(`/home/hnan`, Ghidra 12.1.4, 프로젝트 `ntos`) 의 Ghidra 가 잠깐 보였는데 우리 `ms/ghidra` 와 무관했고 충돌 없이 끝남.

## 한눈에

| 질문 | 한 줄 답 | 닫힘? |
|---|---|---|
| 1 | `SymCryptDetectCpuFeaturesByCpuid` = CPUID 만 부르는 295바이트 리프 함수. CPUID 4~6번 (리프 0·1·7 + 마지막 1 + AMD 일 때만 0·1), **XGETBV 없음**, syscall 없음, 메모리 입력은 읽기 전용 표 160바이트뿐. Unicorn 이 CPUID 만 맞게 답하면 된다 | 정적으로 닫힘 (VM 에서 실제 값 대조는 동적) |
| 2 | KUSD `ProcessorFeatures` 를 채우는 건 `KiSetFeatureBits` **아님**. 몸통은 `KiInitializeKernel +0x58a ~ +0x77a`. 규칙은 CPUID → (`RtlDetectProcessorFeatures` 의 표 `KiCpuFeatureTable`) → `KeFeatureBits` 비트 → KUSD 바이트 | 규칙 표 닫힘 / 실제 VM 값 대조는 동적 |
| 3 | `LdrpTouchThreadStack` = 스택 맨 위 쪽부터 한 쪽(4KiB)씩 **읽기만** 하며 내려가는 함수. test.exe 는 PEB `+0x318` 이 0 이라 **안 부른다** (8장 사진 모두 0) | 정적 + `[관찰]` 로 닫힘 (안 불리는 것 확인은 멈춤점 하나면 됨) |

---

## 질문 1. `SymCryptDetectCpuFeaturesByCpuid` (ntdll rva `0x11f42c`, 295B)

근거 파일: `ms/disassemble/ntdll/SymCryptDetectCpuFeaturesByCpuid.asm`, `_refs/g_SymCrypt*.tsv`, `_refs/cpuidBitInfo.tsv`, 표 `_tables/cpuidBitInfo.tsv`.

### 1-1. CPUID 를 몇 번, 어떤 leaf/subleaf 로

모든 CPUID 의 subleaf(ECX) 는 0 이다 (`XOR ECX,ECX` 가 매번 앞에 있음).

| 줄 | leaf (EAX) | 언제 |
|---|---|---|
| `+0x3b` | 0 (`+0x2e XOR EAX,EAX`, `+0x36 XOR ECX,ECX`) | 항상. `ESI` = 최대 기본 leaf, 4개 레지스터를 지역 칸 `[RBP-0x20..-0x14]` 에 둠 |
| `+0x5a` | 표 줄의 leaf (`+0x55 MOV EAX,R10D`) | 표 줄의 leaf 가 직전에 부른 leaf (`EDI`) 와 **다를 때만** (`+0x50 CMP R10D,EDI`, `+0x53 JZ` → `+0x6b`). 표가 leaf 1 이 9줄, leaf 7 이 11줄이라 실제로는 **leaf 1 한 번, leaf 7 한 번** |
| `+0x9d` | 0 (`+0x99`, `+0x9b`) | AES-NI 가 있다고 나왔을 때만 (`+0x93 TEST R8B,4`, `+0x97 JNZ` → `+0xde`). 제조사 글자 읽기 |
| `+0xbe` | 1 (`+0xb9 MOV EAX,1`, `+0xb7 XOR ECX,ECX`) | 제조사가 AMD 일 때만. 가문(family) 읽기 |
| `+0xec` | 1 (`+0xde MOV EAX,1`, `+0xea XOR ECX,ECX`) | 항상. 결과를 `g_SymCryptCpuid1` 에 저장 |

- 그러니 CPUID 는 **최소 4번** (0, 1, 7, 1), **최대 6번** (+ 0, 1).
- leaf 7 은 최대 leaf (`ESI`) 보다 커도 **CPUID 는 일단 부른다** (`+0x5a` 가 `+0x6b CMP R10D,ESI`/`+0x6e JG` 보다 앞). 큰 leaf 면 결과를 버리고 "없음" 처리 (`+0x82`).
- `XGETBV` / `XSAVE` / `OSXSAVE` 검사 = **없음**. 함수 안에 `XGETBV` 명령 없음 (`.asm` 전체 훑음). AVX/AVX2/AVX512 는 CPUID 비트만 본다 (OS 가 YMM 상태를 켰는지는 안 봄).

### 1-2. 루프와 "없음" 비트 모으기

- 표 `cpuidBitInfo` (ntdll rva `0x186c10`, 8바이트 × 20줄 = 160B, 이미지 `.rdata` 읽기 전용). 줄 모양: `byte0` = leaf, `byte1` = 레지스터 번호 (0=EAX 1=EBX 2=ECX 3=EDX), `byte2` = 비트, `byte3` = 쓰이지 않음, `dword@+4` = "없음" 마스크.
  - `R9` = 표 + 2 에서 시작 (`+0x27 LEA R9,[0x180186c12]`), 줄마다 `+0x89 ADD R9,8`.
  - `+0x4b` leaf 읽기 `[R9-2]`, `+0x70` 레지스터 `[R9-1]`, `+0x75` 비트 `[R9]`, `+0x82` 마스크 `[R9+2]` (= 줄 `+4`).
- 모으는 변수 = `R8D`. **시작값 `0xffffc020`** (`+0x30 MOV R8D,0xffffc020`). 이 비트들 (5, 14~31) 은 표에 줄이 없어서 **항상 "없음"**.
- 줄마다:
  - leaf > 최대 leaf (`ESI`) 이면 (`+0x6b CMP R10D,ESI`, `+0x6e JG` → `+0x82`) "없음" 마스크를 `OR`.
  - 아니면 `BT EAX,ECX` (`+0x7d`), 비트가 **꺼져 있으면** (`+0x80 JC` 안 탐) `+0x82 OR R8D,[R9+2]`. 켜져 있으면 `+0x86` 으로 건너뜀.
  - 20줄 (`+0x8d CMP R11D,0x14`, `+0x91 JC` → `+0x4b`) 반복.
- 그 뒤 AMD 보정 (`+0x93 ~ +0xda`): `R8D` 의 비트 2 (AES-NI 없음) 가 **꺼져 있을 때만** 들어옴.
  - 제조사 글자 비교 `+0x9f CMP EBX,0x68747541` ("Auth") · `+0xa7 CMP ECX,0x444d4163` ("cAMD") · `+0xaf CMP EDX,0x69746e65` ("enti"). 하나라도 다르면 `+0xa5/+0xad/+0xb5 JNZ` → `+0xde`.
  - AMD 이면 leaf 1 의 EAX 에서 `(EAX>>8)&0xf` = 기본 가문 (`+0xc2 SAR`, `+0xc5 AND`). 15 보다 작으면 `+0xcb JC` → `+0xda`. 15 이상이면 `+0xcd SAR EAX,0x14`, `+0xd0 MOVZX ECX,AL` (확장 가문), `+0xd3 ADD`, `+0xd5 CMP ECX,0x15`. 합이 `0x15` 보다 작으면 (`+0xd8 JNC` 안 탐) `+0xda OR R8D,4`.
  - 뜻: **AMD 가문 합 < 0x15 이면 AES-NI 를 "없음"으로 바꾼다** (`[추측]` 이유: 옛 AMD 의 AES 구현 문제로 보임, 코드엔 이유 없음).

### 1-3. CPUID 비트 → "없음" 비트 표 (`cpuidBitInfo` 20줄 전부)

비트 이름은 표의 CPUID 비트에서 읽은 것이다 (`[추측]`: 이름은 공개 CPUID 문서 기준, 코드에 이름은 없음).

| 줄 | leaf | 레지스터 | 비트 | 없음 마스크 | CPUID 가 가리키는 기능 `[추측]` |
|---|---|---|---|---|---|
| 0 | 1 | ECX | 30 | `0x200` (비트 9) | RDRAND |
| 1 | 1 | ECX | 1 | `0x8` (비트 3) | PCLMULQDQ |
| 2 | 1 | ECX | 25 | `0x4` (비트 2) | AES-NI |
| 3 | 1 | EDX | 25 | `0x3` (비트 0,1) | SSE |
| 4 | 1 | EDX | 26 | `0x3` (비트 0,1) | SSE2 |
| 5 | 1 | ECX | 0 | `0x2` (비트 1) | SSE3 |
| 6 | 1 | ECX | 9 | `0x2` (비트 1) | SSSE3 |
| 7 | 1 | ECX | 28 | `0x10` (비트 4) | AVX |
| 8 | 1 | ECX | 13 | `0x2000` (비트 13) | CMPXCHG16B |
| 9 | 7 | EBX | 5 | `0x10` (비트 4) | AVX2 |
| 10 | 7 | EBX | 18 | `0x400` (비트 10) | RDSEED |
| 11 | 7 | EBX | 29 | `0x40` (비트 6) | SHA |
| 12 | 7 | EBX | 19 | `0x100` (비트 8) | ADX |
| 13 | 7 | EBX | 8 | `0x80` (비트 7) | BMI2 |
| 14 | 7 | EBX | 16 | `0x1000` (비트 12) | AVX512F |
| 15 | 7 | EBX | 31 | `0x1000` (비트 12) | AVX512VL |
| 16 | 7 | EBX | 30 | `0x1000` (비트 12) | AVX512BW |
| 17 | 7 | EBX | 17 | `0x1000` (비트 12) | AVX512DQ |
| 18 | 7 | ECX | 9 | `0x800` (비트 11) | VAES |
| 19 | 7 | ECX | 10 | `0x800` (비트 11) | VPCLMULQDQ |

- 여러 CPUID 비트가 한 "없음" 비트로 모인다: 비트 0 = SSE·SSE2 둘 다 필요, 비트 1 = SSE·SSE2·SSE3·SSSE3 모두 필요, 비트 4 = AVX·AVX2 모두 필요, 비트 12 = AVX512 4종 모두, 비트 11 = VAES·VPCLMULQDQ 둘 다.
- 항상 "없음": 비트 5 (`0x20`), 비트 14~31 (`0xffffc000`). 줄이 없어서.
- 그 밖에 비트 2 (AES-NI) 만 AMD 보정을 받는다 (1-2).

### 1-4. 쓰는 전역 전부

| 전역 | rva · 크기 | 읽기/쓰기 | 근거 |
|---|---|---|---|
| `g_SymCryptCpuFeaturesNotPresent` | `0x1cd7b0` · 4B (`.data`) | **쓰기** `+0xe3 MOV [..],R8D` 한 번. 다른 읽는 곳 = `LdrpInitializeProcess +0x1e1f` 하나뿐 (refs) | `_refs/g_SymCryptCpuFeaturesNotPresent.tsv` |
| `g_SymCryptCpuid1` | `0x1d33f0` · 16B (`.data`) | **쓰기** `+0xee` EAX→`+0x0`, `+0xf4` EBX→`+0x4`, `+0xfa` ECX→`+0x8`, `+0x100` EDX→`+0xc` (모두 leaf 1, subleaf 0). 읽는 곳 refs 에 없음 | `_refs/g_SymCryptCpuid1.tsv` |
| `cpuidBitInfo` | `0x186c10` · 160B (`.rdata`) | 읽기 (루프) | `_refs/cpuidBitInfo.tsv` |
| `__security_cookie` | `0x1ea5a8` (`.mrdata`) | 읽기 `+0x17`, 확인 `+0x106 ~ +0x10d` | |

- **KUSD 읽기 없음, `RtlProcessorFeaturesBuffer`·`RtlProcessorFeaturesBitMap` 읽기 없음.** 이 함수는 CPUID 와 표만 본다.
- 뒤에 이 전역을 쓰는 쪽 (`LdrpInitializeProcess`) 은 1-6.

### 1-5. 하위 호출 · syscall · 반환

- 하위 호출 = `+0x10d/+0x112 __security_check_cookie` **하나뿐** (`_calls.tsv`). 그 함수도 `CMP RCX,[__security_cookie]` 뒤 맞으면 `RET`, 틀리면 `__report_gsfailure` 로 꼬리호출 (`__security_check_cookie.asm` `+0x19 JMP`) — 정상 때는 안 탄다.
- `SYSCALL` / `INT` 명령 없음. 인자 없음 (`void`), 반환값 안 씀 (`RET` 는 `+0x126`).

### 1-6. 부르는 조건 (`LdrpInitializeProcess` 시작 `0x18008c404`)

- 부르는 줄: `+0x1def CALL` (ret `+0x1df4`). 부르는 곳은 이것 하나뿐 (`.c` 머리 `callers:`).
- 갈래:
  - `+0x1de6 TEST byte [g_SymCryptFlags],DIL` (`DIL`=1, `+0x1d3c MOV EDI,1`) 비트 0 이 이미 켜졌으면 `+0x1ded JNZ 0x18008e231` (= `0x18008e231 − 0x18008c404` = `+0x1e2d`, 거기 `XOR EAX,EAX`) 로 건너뛴다.
  - 비트 0 이 꺼져 있으면 `+0x1def` 호출 → `+0x1df4 OR.LOCK [g_SymCryptFlags],EDI` (비트 0 켬) → `+0x1e1f MOV EAX,[g_SymCryptCpuFeaturesNotPresent]` · `+0x1e25 NOT EAX` · `+0x1e27 MOV [g_SymCryptCpuFeaturesPresentCheck],EAX`.
  - 새 프로세스에서 `g_SymCryptFlags` 는 `.data` 0 으로 시작하고, 이것 말고 쓰는 줄 없음 (refs: 읽기 `+0x1de6`, 읽고 쓰기 `+0x1df4` 두 곳뿐) → 프로세스당 **한 번** 탄다.
- `+0x1dfb ~ +0x1e1d` 는 `0x180186ee0` 의 글자열 (`v103.12.0__2026-05-07...` 로 보이는 SymCrypt 버전 글자) 을 한 글자씩 스택 한 칸 (`[RSP+0x5c]`) 에 옮기는 루프. 할 일이 CPUID 와 무관하고 읽기뿐 `[추측]` (값을 쓰는 곳은 스택 한 칸).
- 그 앞 (`+0x1d3c ~ +0x1de6`) 에 `LdrpInitializeProcess` **자신의 CPUID** 가 둘 더 있다: `+0x1d45` (leaf 1, `+0x1d41 MOV EAX,EDI`, `+0x1d43 XOR ECX,ECX`), `+0x1dbb` (leaf 7, 조건부). Unicorn 이 같이 답해야 한다. `+0x1d99 RtlIsProcessorFeaturePresent(0x29)` 가 KUSD `ProcessorFeatures[41]` 을 읽는다 (질문 2 의 AVX512F 줄과 이어짐).

### 1-6b. PE_emulator 한 줄

> 이 함수를 진짜로 돌려도 Unicorn 이 **CPUID (leaf 0·1·7, subleaf 0) 만 맞게 답하면 된다.** XGETBV 도 syscall 도 KUSD 도 없다. 메모리 입력은 ntdll 이미지 안 읽기 전용 표 160B (`+0x186c10`) 와 `__security_cookie` (`+0x1ea5a8`, `+0x17`) 둘뿐이고, 쓰는 건 `.data` 20바이트 (`+0x1cd7b0` 4B, `+0x1d33f0` 16B) 뿐이다. 진짜로 안 돌리고 덫으로 둘 거면 결과 두 전역만 위 표대로 채우면 된다.

---

## 질문 2. KUSD `ProcessorFeatures` (`+0x274`, 64B) 를 채우는 커널 쪽

근거 파일: `ms/disassemble/ntoskrnl/KiInitializeKernel.asm` · `KiPublishProcessorFeatures.asm` · `KiSetProcessorSignature.asm` · `RtlDetectProcessorFeatures.asm` · `KiSetFeatureBits.asm` · `KiInitializeNxSupportDiscard.asm` · `InitBootProcessor.asm` · `entry.asm` · `_refs/MmWriteableSharedUserData.tsv`, 표 `_tables/KiCpuFeatureTable.tsv`.

### 2-1. 함수 이름

`KiSetFeatureBits` (rva `0xb6805c`) 는 **KUSD `+0x274` 칸을 쓰는 줄이 없다.** 이 함수는 (a) 못 만드는 CPU 를 막고 (bugcheck `0x5d`), (b) 기본 기능 비트 묶음 (`0x20113dfe` 등) 과 가상화 비트 둘을 `PRCB +0x8fa0` 에 쓰고, (c) `KeProcessorFeaturesBuffer` 를 0 으로 채운다 (`+0x888`, run65 가 이미 봄 — 겹치므로 가리키기만, 이 비트맵은 KUSD 와 다른 칸).

KUSD `ProcessorFeatures` 를 쓰는 곳은 이 넷이다 (스캔 방법과 한계는 2-5).

| 쓰는 함수 | 칸 (번호) | 비고 |
|---|---|---|
| **`KiInitializeKernel`** `0xb66d40` | 24칸 (표 2-3) | 몸통. `+0x5a3 ~ +0x798` 쓰기. 7칸은 조건 없이 1 |
| `KiPublishProcessorFeatures` `0xc37ed8` (126B) | `+0x295` (33), `+0x297` (35) | `KiSetProcessorSignature +0x5f/+0x64` 가 부름 |
| `KiInitializeNxSupportDiscard` `0xc37828` | `+0x280` (12) | NX 지원 때 |
| `InitBootProcessor` `0xc191c8` | `+0x28b` (23) | `+0x3ff` 에서 `+0x406` 쓰기. `R15B` = 1 (`+0x16b MOV R15D,1`) `[추정]` 갈래 둘을 안 따라가 봄 |

### 2-2. 사슬 (부팅 순서, call / ret)

BSP (프로세서 번호 0) 기준. 전부 `.asm` 의 `CALL … ret=` 줄.

```
entry (= KiSystemStartup, rva 0xb593e0)
 ├ +0x1b5 / +0x1ba   KiInitializeBootStructures
 │   ├ +0x219 / +0x21e  KiGetProcessorSignature
 │   ├ +0x27e / +0x283  KiSetProcessorSignature            (+0x270 TEST EBP,EBP ; +0x272 JNZ → +0x35a : BSP 만)
 │   │   ├ +0x2e / +0x33    RtlDetectProcessorFeatures      → CPUID 를 표대로 읽어 FeatureBits(PRCB+0x8fa0), FeatureBits2 만듦
 │   │   └ +0x5f / +0x64    KiPublishProcessorFeatures      (+0x4c TEST EAX,EAX ; +0x4e JNZ → +0x6b : BSP 만)
 │   │                         → KUSD +0x295(33), +0x297(35), KeSmapEnabled
 │   ├ +0x370 / +0x375  KiInitializeNxSupportDiscard        (+0x364 MOV EAX,GS:[0x1a4] ; +0x36e JNZ → +0x375 : 프로세서 번호 0 일 때만)
 │   │                         → KUSD +0x280(12)
 │   └ +0x382 / +0x387  KiSetFeatureBits                    → PRCB FeatureBits 에 기본 묶음·가상화 비트
 ├ +0x1e8 / +0x1ed   KiInitializeXSaveConfiguration          → KUSD XState (+0x3d8) — 이름만, 안 읽음
 └ +0x27e / +0x283   KiInitializeKernel
     ├ +0x585 / +0x58a  KiEnableXSave
     ├ +0x58a ~ +0x798  KUSD ProcessorFeatures 쓰기 (표 2-3)
     └ +0x7fe / +0x803  InitBootProcessor                    (+0x7f6 TEST R15D,R15D ; +0x7f9 JNZ → +0x805 : BSP 만) → KUSD +0x28b(23)
```

- AP (다른 프로세서) 도 `KiInitializeKernel` 의 `+0x58a` 줄을 **다시 지난다.** 그 앞 `+0x42c ~ +0x54a` 에서 BSP 의 `KeFeatureBits` 와 비교해 다르면 `JNZ 0x140b6764a` (`+0x4f7`, `+0x504`, `+0x51e`, `+0x531`, `+0x544`) 로 빠진다 (bugcheck 쪽 `[추정]`, 도착 줄은 안 읽음). BSP 는 `+0x41c JZ 0x140b672ac` 또는 `+0x427 JMP 0x140b672ac` (= `+0x56c`) 로 그 구간을 건너뛰고 `+0x56c` 에서 합류.
- `entry` 호출 순서의 확실한 근거: `entry.asm` `+0x1b5 → +0x1e8 → +0x27e` (offset 순).

### 2-3. 규칙 표 (CPUID 비트 → `KeFeatureBits` 비트 → KUSD 번호)

"FB1 비트" = `KeFeatureBits` (= `PRCB +0x8fa0`, 비트 번호) 의 비트. "표 줄" = `KiCpuFeatureTable` 줄 번호 (`_tables/KiCpuFeatureTable.tsv`). `PF_*` 이름은 `[추측]` (공개 `winnt.h` 이름을 기억으로 맞춤. 번호·CPUID 비트가 이름과 맞아떨어져서 믿을 만하지만 코드엔 이름이 없음).

조건 없이 1 (`SIL`=1) 로 쓰는 칸 (`KiInitializeKernel`):

| KUSD 칸 | 번호 | 쓰는 줄 | `PF_*` `[추측]` |
|---|---|---|---|
| `+0x276` | 2 | `+0x5a3` | COMPARE_EXCHANGE_DOUBLE (CMPXCHG8B) |
| `+0x277` | 3 | `+0x5b1` | MMX_INSTRUCTIONS_AVAILABLE |
| `+0x27a` | 6 | `+0x5bf` | XMMI_INSTRUCTIONS_AVAILABLE (SSE) |
| `+0x27c` | 8 | `+0x5cd` | RDTSC_INSTRUCTION_AVAILABLE |
| `+0x27d` | 9 | `+0x5db` | PAE_ENABLED |
| `+0x27e` | 10 | `+0x5e9` | XMMI64_INSTRUCTIONS_AVAILABLE (SSE2) |
| `+0x282` | 14 | `+0x5f7` | COMPARE_EXCHANGE128 (CMPXCHG16B) |
| `+0x28b` | 23 | `InitBootProcessor +0x406` | FASTFAIL_AVAILABLE |

`FB1` 비트가 켜졌을 때만 1:

| FB1 비트 | 확인 줄 (`BT RBX,…`) | KUSD 칸 (쓰는 줄) | 번호 | `PF_*` `[추측]` | 그 FB1 비트를 켜는 CPUID 비트 (표 줄) |
|---|---|---|---|---|---|
| 19 (`0x13`) | `+0x5fe` | `+0x281` (`+0x60c`) | 13 | SSE3 | leaf 1 ECX[0] (표 0) |
| 14 (`0xe`) | `+0x613` | `+0x27b` (`+0x621`) | 7 | 3DNOW | leaf `0x80000001` EDX[31] (표 47) |
| 23 (`0x17`) | `+0x628` | `+0x285` (`+0x636`) | 17 | XSAVE_ENABLED | CPUID 표에 없음. `KiInitializeKernel +0x2da CMP [KUSD+0x3d8],0` 이 0 이 아니면 `+0x2e5 BTS RBX,0x17` (스스로 켬) |
| 28 (`0x1c`) | `+0x63d` | `+0x28a` (`+0x64b`) | 22 | RDWRFSGSBASE | leaf 7 EBX[0] (표 15, Intel·AMD) |
| 26 (`0x1a`) | `+0x652` | `+0x288` (`+0x660`) | 20 | SECOND_LEVEL_ADDRESS_TRANSLATION | CPUID 표에 없음. `KiSetFeatureBits`: Intel = leaf 1 ECX[5](VMX, `+0x707`) 이고 MSR `0x482` 비트 63 (`+0x715 ~ +0x721`) 이고 MSR `0x48b` 비트 33 (`+0x731`) → `+0x738 BTS RBX,0x1a`. AMD = leaf `0x8000000a` EDX[0] (`+0x7b5`) → `+0x7ba BTS RSI,0x1a` |
| 27 (`0x1b`) | `+0x667` | `+0x289` (`+0x675`) | 21 | VIRT_FIRMWARE_ENABLED | CPUID 표에 없음. `KiSetFeatureBits`: Intel = MSR `0x3a` 의 하위 바이트 `& 5 == 5` (`+0x763 ~ +0x779`, 마스크 `R14`=`0x8000000`, `+0x63e`). AMD = MSR `0xc0010114` 비트 4 가 꺼짐 (`+0x7d4 ~ +0x7e6`) |
| 32 (`0x20`) | `+0x67c` | `+0x290` (`+0x68a`) | 28 | RDRAND_INSTRUCTION_AVAILABLE | leaf 1 ECX[30] (표 11) |
| 34 (`0x22`) | `+0x691` | `+0x294` (`+0x69f`) | 32 | RDTSCP_INSTRUCTION_AVAILABLE | leaf `0x80000001` EDX[27] (표 46) |
| 47 (`0x2f`) | `+0x6a6` | `+0x298` (`+0x6b4`) | 36 | SSSE3 | leaf 1 ECX[9] (표 2, 콜백 `RtlHandleSSSE3Override`) |
| 48 (`0x30`) | `+0x6bb` | `+0x299` (`+0x6c9`) | 37 | SSE4_1 | leaf 1 ECX[19] (표 5) |
| 49 (`0x31`) | `+0x6d0` | `+0x29a` (`+0x6de`) | 38 | SSE4_2 | leaf 1 ECX[20] (표 6, 필수) |
| 50 (`0x32`) | `+0x6f5` | `+0x29b` (`+0x708`) | 39 | AVX | leaf 1 ECX[28] (표 10) **그리고** `KUSD+0x3d8` (XState.EnabledFeatures) 비트 2 (`+0x6ef TEST byte [RDX],4`) |
| 51 (`0x33`) | `+0x715` | `+0x29c` (`+0x728`) | 40 | AVX2 | leaf 7 EBX[5] (표 17) **그리고** XState 비트 2 (`+0x70f`) |
| 52 (`0x34`) | `+0x73d` | `+0x29d` (`+0x750`) | 41 | AVX512F | leaf 7 EBX[16] (표 23) **그리고** XState 비트 5·6·7 중 하나 (`+0x72f MOV AL,[0xfffff780000003d8]`, `+0x738 TEST AL,0xe0`) |
| 54 (`0x36`) | `+0x757` | `+0x29e` (`+0x765`) | 42 | ERMS_AVAILABLE | leaf 7 EBX[9] (표 20) |
| 57 (`0x39`) | `+0x76c` | `+0x2b0` (`+0x77a`) | 60 | BMI2_INSTRUCTIONS_AVAILABLE | leaf 7 EBX[8] (표 19, 콜백 `RtlHandleBMI2Override`) |
| 58 (`0x3a`) | `+0x78a` | `+0x2b1` (`+0x798`) | 61 | MOVDIR64B_INSTRUCTION_AVAILABLE | leaf 7 ECX[28] (표 31). **그리고** `Feature_Test57481295 …IsEnabledDeviceUsageNoInline` (`+0x781/+0x786`) 가 켜짐 |

`KeFeatureBits2` 비트로 쓰는 칸 (`KiPublishProcessorFeatures`):

| FB2 비트 | KUSD 칸 (쓰는 줄) | 번호 | `PF_*` `[추측]` | 그 FB2 비트를 켜는 CPUID 비트 (표 줄) | 막는 조건 |
|---|---|---|---|---|---|
| 1 | `+0x295` (`+0x19`) | 33 | RDPID_INSTRUCTION_AVAILABLE | leaf 7 ECX[22] (표 30) | 없음 (조건 없이 씀) |
| 13 | `+0x297` (`+0x48`) | 35 | MONITORX_INSTRUCTION_AVAILABLE | leaf `0x80000001` ECX[29] (표 42, **AMD 만**) | 로더 확장 플래그 `SuppressMonitorX` (`[KeLoaderBlock+0xf0]+0x84` 비트 13, `+0x2d TEST … 0x2000`, `+0x37 JNZ`) 이 켜지면 안 씀 |

- 같은 함수에서 `KeSmapEnabled` ← `PRCB+0x8fa0` 비트 33 (SMAP, 표 25 = leaf 7 EBX[20]), 로더 플래그 `SuppressSmap` (비트 15, `+0x5d TEST … 0x8000`) 이 꺼져 있을 때만 (`+0x69 ~ +0x77`).
- NX: `KiInitializeNxSupportDiscard +0x152 RDMSR`(0xC0000080, `+0x14d MOV ECX,0xc0000080`) → `+0x15b BTS RAX,0xb` → `+0x167 WRMSR` → `+0x169/+0x170 MOV byte [KUSD+0x280],1` (번호 12 NX_ENABLED), `+0x14b JZ` 로 `KiIsNXSupported` (`+0x144` 호출, ret `+0x149`) 가 거짓이면 건너뜀.
- 여기 없는 번호 (0, 1, 4, 5, 11, 15, 16, 18, 19, 24~27, 29~31, 34, 43~59, 62, 63) 는 이 스캔에서 쓰는 줄이 안 나왔다 → 0 으로 남는 것으로 읽힌다 (`[정적]`, 한계는 2-5).

FB1 비트를 CPUID 비트에서 모으는 곳 = `RtlDetectProcessorFeatures` (rva `0xb6f9f0`, 1684B):
- 표 `KiCpuFeatureTable` (rva `0x8800`, 0x3b = 59줄, 줄 40바이트): `+0x0` leaf, `+0x4` subleaf (CPUID 를 이걸로 부름: `+0x179 MOV ECX,EBX`, `+0x17b CPUID`), `+0x8` 마스크, `+0xc` 레지스터 번호 (0~3 = EAX, EBX, ECX, EDX), `+0x10` 깃발, `+0x18` 켤 FeatureBits 마스크 (8바이트), `+0x20` 콜백 포인터.
- 깃발: 비트 0 = 필수 (없으면 bugcheck `0x5d`, 인자 `0xffffffff`), 비트 1 = 건너뜀, 비트 2 = AP 도 BSP 와 같아야 함 (다르면 bugcheck `0x5d`, 인자 `0xfffffffa ~ 0xfffffffe`), 비트 3 = `FeatureBits` 에 모음 (`R13`), 비트 4 = `FeatureBits2` 에 모음 (`R12`), 위 8비트 = 제조사 마스크 (`1<<제조사`, AMD=1·Intel=2 — `KiSetFeatureBits` 가 `PRCB+0x8d == 1` 일 때 AMD 용 SVM 길, `== 2` 일 때 Intel 용 VMX·MSR 길을 타서 이렇게 읽음 `[추정]`).
- 필수 줄 5개: 표 3 (leaf 1 ECX[13] CMPXCHG16B), 6 (leaf 1 ECX[20] SSE4.2), 41 (leaf `0x80000001` ECX[0] LAHF), 43 (leaf `0x80000001` EDX[11] SYSCALL), 44 (leaf `0x80000001` EDX[20] NX, Intel 만). 하나라도 없으면 `KeBugCheckEx(0x5d, 0xffffffff, …)`.
- 건너뜀 줄 (깃발 비트 1): 표 1 (leaf 1 ECX[1] PCLMULQDQ), 7 (ECX[22] MOVBE), 8 (ECX[23] POPCNT), 9 (ECX[25] AES-NI), 16 (leaf 7 EBX[3]), 24 (leaf 7 EBX[18]). 이 CPUID 비트는 `FeatureBits` 에 안 들어간다.
- 표 59줄 중 KUSD 에 닿는 건 위 표의 줄들 뿐. 나머지 (leaf 6, leaf 7 서브리프 1·2, leaf `0x80000008`, `0x80000021` 줄 등) 는 `FeatureBits2` 의 다른 비트 (투기 실행 방어, 하이브리드 등 `[추측]`) 로 간다 — KUSD `ProcessorFeatures` 는 아님.
- 콜백 호출 둘 (`_calls.tsv`): `RtlDetectProcessorFeatures +0x23c/+0x241` = `indirect:cfg:[R14+RSI*0x8+0x8820]` (CPUID 표 `+0x20` 칸의 콜백), `+0x541/+0x546` = `indirect:cfg:@0x9158` (MSR 표 `KiMsrFeatureTable` rva `0xb6d0` 쪽 콜백 `[추정]`). 콜백이 나오는 CPUID 표 줄은 둘뿐 (`RtlHandleSSSE3Override` 표 2, `RtlHandleBMI2Override` 표 19) — 본문은 안 읽음. 콜백이 비트를 지울 수 있다 (실행 때 결과는 동적).

### 2-4. PE_emulator 쪽 읽는 법 (Q2 (나) 를 고르면)

CPU 에서 직접 셈하려면: (1) CPUID 로 위 표의 `FB1`/`FB2` 비트를 만든다 (표 59줄 중 KUSD 에 닿는 줄만), (2) AVX 3종은 XState 의 EnabledFeatures 도 같이 맞춰야 한다 (`KUSD+0x3d8`: 비트 2 = YMM, 비트 5~7 = AVX512), (3) 조건 없이 1 인 칸 8개와 `FASTFAIL`·`NX` 를 얹는다. 그 외 번호는 0.

### 2-5. 한계 (스캔 방법)

- "쓰는 줄이 없다" 는 이렇게 찾았다: ① `refs MmWriteableSharedUserData` 200줄 (`_refs/MmWriteableSharedUserData.tsv`, 상한에 걸렸을 수 있음 — 파일 끝 `# 참조 200줄`) ② 커널 이미지의 실행 구역 전체에서 "`MOV r64,[MmWriteableSharedUserData]` 뒤 40바이트 안에 레지스터+disp32 (`0x274 ~ 0x2b3`)" 모양 찾기 ③ 절대 주소 `0xFFFFF780000002xx` (`0x274 ~ 0x2b3`) 를 직접 쓰는 곳 찾기. 코드를 다르게 짠 곳 (포인터를 다른 변수에서 읽거나, 읽은 포인터를 오래 들고 가는 곳) 은 놓칠 수 있다.
- ③ 에서 찾은 절대 주소 곳은 모두 **읽기** (시간 함수·푸시락의 `+0x294` RDTSCP · `+0x297` MONITORX 검사, `RtlIsProcessorFeaturePresent`(커널) `+0x11`, `KiInitializeKernel +0x176` NX 읽기).
- `KiRestoreFeatureBits` (rva `0x4d92a8`, 314B, 절전 복귀용 `[추측]`) 는 `.asm` 만 뽑고 안 읽음.

---

## 질문 3. `LdrpTouchThreadStack` (ntdll rva `0x8bdc4`, 180B)

근거 파일: `ms/disassemble/ntdll/LdrpTouchThreadStack.asm` (+ `LdrpTouchThreadStack$filt$0.asm`, `LdrpGenericExceptionFilter.asm`), `_LdrpInitialize.asm`, 원본 바이트.

### 3-1. 부르는 자리와 인자

- `_LdrpInitialize` (시작 `0x18008bf8c`): `+0x190 MOV RCX,[RSI+0x318]` (`RSI` = PEB, PDB 로 `+0x318 MinimumStackCommit`, 8바이트) · `+0x197 TEST RCX,RCX` · `+0x19a JZ 0x18008c16f` (= `+0x1e3`) · `+0x19c CALL LdrpTouchThreadStack` (ret `+0x1a1`) · `+0x1a1 MOV [RSP+0x30],EAX` · `+0x1a5 JMP` → `+0x1e3`.
- 인자 `RCX` = `MinimumStackCommit` (바이트 수). 반환 `EAX` = NTSTATUS.
- 반환값 쓰임: `[RSP+0x30]` 에 담겨 있다가 `+0x221 MOV EDI,[RSP+0x30]` · `+0x225 TEST` · `+0x227 JS 0x18008c283` (= `+0x2f7`) → `+0x2f9/+0x2fe LdrpInitializationFailure` → `+0x304/+0x309 NtTerminateProcess`. **음수 = 프로세스 초기화 실패로 종료.**

### 3-2. 본문 (시작 `0x18008bdc4`)

1. `+0xc ~ +0x1a` `RBX = RCX`(최소 커밋), 지역 구조체 0x30바이트 (`RSP+0x38 ~`) 를 0 으로 (`MEMORY_BASIC_INFORMATION` 크기), `+0x1e AND [RAX+0x10],0` (반환 길이 칸을 0).
2. `+0x23 MOV RDI,GS:[0x30]` = **TEB** (`NtTib.Self`). `+0x46 MOV RDX,[RDI+0x10]` = TEB `StackLimit`.
3. **`+0x4e/+0x53 NtQueryVirtualMemory`**: `RCX=-1` (현재 프로세스, `+0x4a OR RCX,-1`) · `RDX` = `StackLimit` · `R8D=0` (`+0x43`, 클래스 0 = `MemoryBasicInformation`) · `R9` = `RSP+0x38` (결과 구조체) · `[RSP+0x20]=0x30` (`+0x35`, 길이) · `[RSP+0x28]` = 반환 길이 칸 (`+0x2c LEA RAX,[RAX+0x10]`, `+0x30 MOV [RSP+0x28],RAX`). `+0x55 JS` → `+0x9e` (실패면 그 상태 그대로 반환).
4. 구조체 `+0x8` = `AllocationBase` (`+0x57 MOV RDX,[RSP+0x40]`) → `+0x5c ADD RDX,0x3000`: **아래 한계 = 스택 예약 영역 맨 밑 + 3쪽**.
5. `+0x63 MOV RAX,[RDI+0x8]` = TEB `StackBase`, `+0x67 ADD RAX,-0x1000`: **시작 주소 = 스택 맨 위에서 한 쪽 아래**.
6. 한계 정하기: `+0x72 CMP RAX,RBX` · `+0x75 JA 0x18008be71` (= `+0xad`). 시작 주소가 최소 커밋보다 크면 (보통 이쪽) `+0xad ~ +0xba`: `RCX = 시작 − 최소커밋`, 이게 `RDX` 보다 작으면 `RDX` 로 올림 (`CMOVBE`) → **한계 = max(시작 − 최소커밋, AllocationBase + 0x3000)**. 아니면 `+0x77 MOV RCX,RDX` (한계 = `AllocationBase + 0x3000`).
7. **루프** `+0x7a CMP RAX,RCX` · `+0x7d JC 0x18008be57` (= `+0x93`, 끝) / `+0x7f MOV EAX,[RAX]` (**그 쪽에서 4바이트 읽기**) · `+0x81 ~ +0x8c` 주소 `−0x1000` · `+0x91 JMP` → `+0x7a`. **쓰기 없음**, 읽기만. 위에서 아래로 한 쪽씩.
8. 정상 끝: `+0x93 JMP` → `+0x9c XOR EAX,EAX` → `+0xab RET` (반환 0).

### 3-3. 스택을 어떻게 건드리나

- **읽기만 한다.** 시작 주소 (`StackBase − 0x1000`) 부터 한계까지 4KiB 간격으로 한 쪽에 4바이트씩 읽는다. 총 길이는 대략 `MinimumStackCommit` 바이트 (한계에서 잘릴 수 있음).
- 읽어서 가드 페이지(맨 아래 보호 쪽)를 건드리면 **커널이 가드 페이지를 옮기며 커밋을 늘린다** `[추측]` — 이 함수 안에는 커밋하는 호출이 없고 (`VirtualAlloc` 류 없음), 읽기 한 번으로 커밋이 늘어나게 하는 방식이다. 이 커널 동작은 이 요청에서 안 읽음.

### 3-4. 예외 (SEH 필터)

- 루프 범위 `+0x7a ~ +0x95` 가 `__try`. 근거: 이 함수의 `.pdata` 풀기 정보 (`handler rva 0x124670`, 범위 표 1줄: 시작 `0x8be3e` = `+0x7a`, 끝 `0x8be59` = `+0x95`, 필터 `0x169620`, 착지 `0x8be59` = `+0x95`).
- 필터 `LdrpTouchThreadStack$filt$0` (rva `0x169620`): `+0x9 LEA RDX,[0x18019c000]` (글자열 `LdrpTouchThreadStack`) · `+0x10/+0x15 CALL LdrpGenericExceptionFilter` · `+0x1b RET`.
- `LdrpGenericExceptionFilter` (rva `0x119980`) 는 `+0x4a/+0x4f LdrpLogInternal` 로 기록하고, `LdrpDebugFlags & 0x30 == 0x20` (`+0x4f ~ +0x59`) 일 때만 `DbgPrint`/`DbgPrompt` 로 사용자에게 묻는다 (글자 `b`·`i`·`p`·`r` 에 따라 중단/종료 갈래). 아니면 `+0x11c MOV EAX,1` (`EXCEPTION_EXECUTE_HANDLER`) 로 돌아옴.
- 착지 `+0x95 MOV EAX,0xC0000017` (`STATUS_NO_MEMORY`) · `+0x9a JMP` → `+0x9e` → `RET`. 이 5바이트가 `.asm` 의 "읽지 않은 틈 `+0x95~+0x9b`" 이다 (원본 바이트 `b8 17 00 00 c0 eb 02`).
- 그러니 **예외가 나면 (예: 스택 한계 밖 읽기) 필터가 1 을 돌려주고 함수는 `STATUS_NO_MEMORY` 를 반환 → 3-1 의 실패 길.**

### 3-5. test.exe 에서 타나

- **안 탄다.** PEB `+0x318` = 0 → `+0x19a JZ` 가 건너뜀.
- `[관찰]` run44 사진 8장 (`ms/logs/04_baggage-read_{K1,K2,K3,P0,P1,P1a,P1b,P2}.txt`) 모두 `+0x318 MinimumStackCommit : 0`. 마지막 사진 `P2` 는 `NtContinue` 입구 (이 읽기 `+0x190` 보다 **뒤**) 라서 읽는 시점에도 0 이었다고 읽힌다.
- 누가 채우나 (둘뿐, 둘 다 test.exe 에선 값이 안 들어옴):
  1. **커널** `MmCreatePeb +0x4c6 MOV EAX,[MmRegistryState+0x28]` → `+0x4cc MOV [RBX+0x318],RAX` (`RBX` = 새 PEB). `MmRegistryState+0x28` (rva `0xfc5218`) 는 레지스트리 표로 채워진다: 표 `0xc80d40` 을 읽는 건 `CmpGetSystemControlValues +0x27a` 하나 (`_refs/0xc80d40.tsv`), 표 줄: 부모 키 글자 `Session Manager\Memory Management`, 값 이름 `MinimumStackCommitInBytes`, 담을 곳 `0x140fc5218`. 기본값 없음 (표 줄의 기본 형식 0). 이 값은 `MiInitializeWowPeb +0x11b/+0x22f` (32비트 PEB) 도 읽는다. 위쪽 키 (`…\Control` 아래) 는 함수 이름에서 짐작 `[추측]`.
  2. **ntdll** `LdrpInitializeProcess +0x531/+0x536` → `LdrpInitializeExecutionOptions` (시작 `0x18011a4e0`) `+0x3f7 LEA RDX,"MinimumStackCommitInBytes"` · `+0x401 RtlQueryImageFileKeyOption` (ret `+0x406`) · `+0x40a CMP [RBX+0x318],RAX` · `+0x411 JNC` · `+0x413 MOV [RBX+0x318],RAX`. **이미지 파일 실행 옵션(IFEO)** 키의 값이 지금 PEB 값보다 클 때 올림. IFEO 키 열기가 실패하면 (`+0xc5 RtlpOpenImageFileOptionsKeyEx` ret `+0xca`, `+0xd1 JS` → `0x18011aae4` = `+0x604`) 또는 `ProcessParameters` 플래그에 `0x4000` (`+0xa6 TEST [RAX+8],0x4000`, `+0xad JNZ` → `+0x604`, 이름 `RTL_USER_PROC_IMAGE_KEY_MISSING` `[추측]`) 이 있으면 이 줄(`+0x3f7 ~ +0x413`)에 닿지 못한다 (건너뜀).
- 이미지 헤더의 `SizeOfStackCommit` 이 이 칸을 채우는 줄은 **안 나왔다.** ntdll 전체에서 `[reg+0x318]` 쓰기 모양을 훑었을 때 쓰기는 `LdrpInitializeExecutionOptions +0x413` 하나뿐 (읽기: `_LdrpInitialize +0x190`, `TpAllocPoolInternal +0x87`, `RtlCreateUserStack +0x123`).
- 다른 읽는 곳: `TpAllocPoolInternal +0x87` (스레드 풀 일꾼 스택 커밋 크기, sast/64 에서 이미 봄), `RtlCreateUserStack +0x123` (새 스레드 스택 만들기, 이번 범위 밖).

---

## PE_emulator 가 그대로 쓸 작은 표

| 함수 | 입력 (CPUID / XGETBV / 메모리) | 쓰는 칸 | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|
| ntdll `SymCryptDetectCpuFeaturesByCpuid` | CPUID leaf 0 · 1 · 7 (subleaf 0) 항상, AMD 때 leaf 0·1 한 번씩 더, 마지막 leaf 1 / XGETBV **없음** / 메모리: `cpuidBitInfo` 160B (`+0x186c10`, 읽기 전용), `__security_cookie` | `+0x1cd7b0` 4B (NotPresent), `+0x1d33f0` 16B (leaf 1 의 EAX·EBX·ECX·EDX) | `__security_check_cookie` 1번 | 없음 | 없음 (`void`) |
| ntdll `LdrpTouchThreadStack` (test.exe 는 안 부름) | RCX = PEB `+0x318` / CPUID·XGETBV 없음 / 메모리: TEB `+0x8 StackBase`, `+0x10 StackLimit`, 스택 쪽들 읽기 | 지역 구조체 (스택) 뿐 | `NtQueryVirtualMemory` 1번, 예외 때 `LdrpGenericExceptionFilter` | **1번** (`NtQueryVirtualMemory`, 클래스 0, 길이 0x30) | 0 / NTSTATUS (음수면 초기화 실패) / 예외면 `0xC0000017` |
| ntoskrnl `RtlDetectProcessorFeatures` | CPUID 표 59줄 (leaf 1·6·7(0/1/2)·`0x80000001`·`0x80000008`·`0x80000021`, `0x80000000`·0 먼저) / XGETBV 없음 (이 함수 `.asm` 에는 없음, 콜백 본문은 안 읽음) / 메모리: `KiCpuFeatureTable` | `FeatureBits`(PRCB `+0x8fa0`), `FeatureBits2`, 제조사별 플래그 (`RtlpGetSpecificProcessorFlags`) | `RtlGetProcessorSignature`, `RtlpGetSpecificProcessorFlags`, 표 콜백, 필수 CPUID 비트 없으면 `KeBugCheckEx` | 없음 | 출력 3개 (`param_2~4`) |
| ntoskrnl `KiPublishProcessorFeatures` | `KeFeatureBits2`, 로더 확장 플래그, PRCB `+0x8fa0` | KUSD `+0x295`, `+0x297`, `KeSmapEnabled` | 없음 | 없음 | 없음 |
| ntoskrnl `KiInitializeKernel` (KUSD 부분만) | PRCB `FeatureBits`(`+0x8fa0`), KUSD `+0x3d8` XState, `Feature_Test57481295` | KUSD 24칸 (표 2-3) | `KiEnableXSave` 등 | 없음 | — |

## 동적으로 확인할 것 (Q104 VM 단계에 얹을 것)

정적으로 닫힌 것:
- 질문 1 전부 (CPUID 횟수·leaf·표·전역·조건), 질문 3 전부 (본문·인자·예외), 질문 2 의 규칙 표.

정적으로 못 정하는 것 / 동적으로 확인할 것:
1. **VM 의 진짜 값 대조** (정적 규칙이 맞는지): 읽을 칸 —
   - ntdll: `g_SymCryptCpuFeaturesNotPresent` (`ntdll+0x1cd7b0`), `g_SymCryptCpuid1` (`ntdll+0x1d33f0`, 16B), `g_SymCryptCpuFeaturesPresentCheck` (`ntdll+0x1d2234`), `g_SymCryptFlags` (`ntdll+0x1d2230`). 멈춤점 `ntdll!LdrpInitializeProcess+0x1e2d` (이 줄 이후 값이 전부 채워짐; test.exe 일 때만).
   - 커널: KUSD `0xfffff78000000274` 64B (또는 유저 `0x7ffe0274`), `nt!KeFeatureBits` (rva `0xfc6b70`), `nt!KeFeatureBits2` (rva `0xfc6ba0`), KUSD `+0x3d8` (XState.EnabledFeatures). 이미 부팅이 끝난 뒤 한 번 읽으면 됨 (부팅 때 멈출 필요 없음).
2. **`LdrpTouchThreadStack` 안 불리는 것 확인**: 멈춤점 `ntdll!_LdrpInitialize+0x197` (`TEST RCX,RCX` 줄) 에서 `rcx` 가 0 인지, 그리고 `ntdll!LdrpTouchThreadStack` 입구 멈춤점이 안 걸리는지 (test.exe 만, `.process` 조건).
3. **콜백 둘의 효과**: `RtlHandleSSSE3Override` · `RtlHandleBMI2Override` 가 비트를 지우는 조건 (본문 안 읽음). 필요하면 정적 후속 요청.
4. (선택) 이 VM 의 CPU 제조사 (AMD/Intel) 에 따라 위 표의 제조사 마스크 줄이 달라진다. VM 에서 `!cpuinfo` 나 `g_SymCryptCpuid1` 의 EAX 로 가문 확인.

## 짐 지도 반영거리

- **SymCrypt 전역 4개** — 이미 `baggage.md` 에 이름이 있음. 이번에 확정된 것: 쓰는 줄 (NotPresent `+0xe3`, Cpuid1 `+0xee ~ +0x100`, Flags `LdrpInitializeProcess +0x1df4`, PresentCheck `LdrpInitializeProcess +0x1e27`), 크기 (4B · 16B · 4B(비트 0 만 씀) · 4B), 값 모양 (NotPresent = 시작 `0xffffc020` 에서 표 20줄이 비트를 더함, PresentCheck = NOT). 경로: `LdrpInitializeProcess +0x1def → SymCryptDetectCpuFeaturesByCpuid`. 단계: `+0x1def` 는 `P1b` 사진 자리 (`+0x1bb6`) 보다 뒤 → **P1b 이후, `P2` 이전**.
- **x86_cpu_enable_ssse3/simd/avx512** (`LdrpInitializeProcess +0x1d75/+0x1d8e/+0x1da1/+0x1de0`) — 이미 이름 있음. 이번에 `+0x1d45` (leaf 1) · `+0x1dbb` (leaf 7, 조건부) CPUID 가 값의 출처임을 확인.
- **KUSD `+0x274` ProcessorFeatures (64B)** — 지금 `끝까지`/`K1 이전` 쪽 목록에 한 칸으로 있을 수 있음. 이번에 쓰는 함수·번호가 갈라짐: `KiInitializeKernel` 24칸 (조건 없이 7 + 조건부 17) · `KiPublishProcessorFeatures` 2칸 · `KiInitializeNxSupportDiscard` 1칸 · `InitBootProcessor` 1칸 = **28칸 쓰는 줄 있음**, 나머지 36칸은 쓰는 줄 안 나옴 (0). 경로: 2-2. 부팅 때 값 (KUSD 는 유저 가상 주소 공간으로 매핑됨).
- **PEB `+0x318` MinimumStackCommit** — `끝까지 0 (65)` 목록에 있음. 이번에 "쓰는 줄이 있는데 test.exe 에선 값이 0" 으로 분류가 바뀜: 커널 `MmCreatePeb +0x4cc` (레지스트리 `MinimumStackCommitInBytes` → `MmRegistryState+0x28`) 와 ntdll `LdrpInitializeExecutionOptions +0x413` (IFEO). 쓰는 줄이 있으므로 3.1(쓴 자리 아는 것) 쪽에 옮길 후보 — 사진은 `끝까지 0`.
- **새 nt 전역** `MmRegistryState+0x28` (rva `0xfc5218`, 4B) — 레지스트리 표 (`CmpGetSystemControlValues` 가 읽는 표 `0xc80d40`) 로 채워짐. 이 표가 `MmRegistryState` 의 다른 칸도 채움 (`+0x0 ~ +0x5c` 중 표 포인터가 가리키는 18칸, `_refs/MmRegistryState.tsv` 끝 `ptr` 줄들). 이번 요청 밖이라 이름만.
- **`KeFeatureBits` · `KeFeatureBits2`** (rva `0xfc6b70` · `0xfc6ba0`) — 커널 전역. 쓰는 곳: `KiInitializeKernel +0x382` (BSP, `KeFeatureBits`), `KiSetProcessorSignature +0x58` (`KeFeatureBits2 |=`). 짐 지도에 있는지 확인 필요 (`[공백]`, 안 찾아봄).

## 판정

**분석성공.**
- 질문 1·3 은 `.asm` 줄로 전부 닫음. 질문 2 는 KUSD `ProcessorFeatures` 의 쓰는 함수 넷과 CPUID → FeatureBits → KUSD 규칙 표를 `.asm` + 바이트로 닫음 (한계는 2-5 에 적음). `PF_*` 이름은 공개 헤더 기억이라 `[추측]`.
- 버전 대조 일치, 커널 PDB GUID 일치.
- dast 요청 필요: 위 "동적으로 확인할 것" 1·2 (읽기만, 멈춤점 둘). 급하지 않음 (Q104 VM 단계에 얹을 것).
