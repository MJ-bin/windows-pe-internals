# 65 cpu-features — 정적 분석 요청

- 날짜: 2026-11-07
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run70
- 대기열: **Q133** (PE_emulator 요청 21, 원문 급함 낮음. 사용자 결정 2026-11-07: Q132 다음, Q104 VM 단계 앞 — "vm 분석 전까지 쭉")
- 관련: run65 (sast/59 — `RtlInitializeProcessorFeaturesBitMap` 클래스 `0xfa`, `KeProcessorFeaturesBuffer`, `KiSetFeatureBits +0x888` 0 채우기), run43·run44 (KUSD 사진), run14·run35 (`_LdrpInitialize` 앞부분)
- 원문 (읽기만): `../PE_emulator/_workspace/00_planner/117_cpu-features.md` "근거 요청 21", PE_emulator 118

## 무엇을 위해

- PE_emulator 117 은 `SymCryptDetectCpuFeaturesByCpuid` 를 공사 중 빈 함수로 두었다. 진짜 덫 (약속 asm 근거) 으로 바꿀 재료가 필요하다. PE_emulator 118 은 `LdrpTouchThreadStack` 본문이 필요하다.
- KUSD `ProcessorFeatures` 를 PE_emulator 가 Unicorn CPU 로 셈하려 할 때 쓸 규칙 (CPUID 비트 → 번호) 의 정적 근거가 필요하다 (원문 "Q2 (나) 를 고르면" — PE_emulator 가 아직 안 골랐을 수 있으니 2번은 정적 근거만, 짧게).

## 알고 싶은 것

1. **ntdll `SymCryptDetectCpuFeaturesByCpuid`** (rva `0x11f42c`, 295B, 부르는 곳 `LdrpInitializeProcess +0x1def/+0x1df4`)
   - 본문 줄 요약 — `CPUID` 를 몇 번·어떤 leaf/subleaf 로 부르나, `XGETBV` 가 있나 (있으면 조건 — OSXSAVE 비트 검사 뒤인가)
   - 쓰는 전역 전부 (`g_SymCryptCpuFeaturesNotPresent` `0x1cd7b0`·`g_SymCryptCpuid1` `0x1d33f0` 외 — 이름·크기), 어느 CPUID 비트를 어느 "없음" 비트로 옮기나 (표)
   - syscall 유무 (`+0x10d __security_check_cookie` 말고 하위 호출이 있나), 다른 입력 (KUSD·`RtlProcessorFeaturesBuffer` 를 읽나)
   - `LdrpInitializeProcess +0x1def` 를 부르는 조건 (갈래 줄)
   - PE_emulator 한 줄: 이 함수를 진짜로 돌리면 Unicorn 의 CPUID/XGETBV 대답만 맞으면 되나 (메모리 입력이 더 있나)
2. **KUSD `ProcessorFeatures` (`+0x274`, 64B) 를 채우는 커널 쪽** — 함수 이름 (`KiSetFeatureBits` rva `0xb6805c` 2297B 인가, 또는 다른 함수), 규칙 표 (CPUID leaf·레지스터·비트 → `PF_*` 번호; 공개 `PF_*` 이름은 `[추측]` 으로), 부팅 순서 자리 (부르는 사슬 call/ret). 2297B 로 크다 — 규칙 표를 뽑는 데 필요한 만큼만, 나머지는 이름만. run65 `KeProcessorFeaturesBuffer` 쪽과 겹치면 가리키기만
3. **ntdll `LdrpTouchThreadStack`** (rva `0x8bdc4`, 180B, 부르는 곳 `_LdrpInitialize +0x19c/+0x1a1` — `+0x190 MOV RCX,[RSI+0x318]` · `+0x197 TEST` · `+0x19a JZ` 로 PEB `+0x318 MinimumStackCommit` ≠ 0 일 때만)
   - 인자 (`RCX` = `MinimumStackCommit`), `+0x4e/+0x53 NtQueryVirtualMemory` 클래스·인자 (스택 주소·버퍼), 스택을 어떻게 건드리나 (쪽마다 읽기/쓰기 — 가드 페이지를 울려 커밋을 늘리나), SEH 필터 `LdrpGenericExceptionFilter` 로 가는 경우
   - test.exe 에서 타나: PEB `+0x318` 값 (run44 사진 — 있으면 `[관찰]`, 없으면 무엇이 채우나: `MmCreatePeb`·이미지 헤더?)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^SymCryptDetectCpuFeaturesByCpuid$` | 1 | 1 |
| ntdll | (refs) `g_SymCryptCpuFeaturesNotPresent` `0x1cd7b0`, `g_SymCryptCpuid1` `0x1d33f0` (와 1 에서 나오는 전역) | — | 1 쓰는 곳·읽는 곳 |
| ntoskrnl | `^KiSetFeatureBits$` (또는 KUSD `+0x274` 를 쓰는 함수 — `_refs`·지도로 찾아 그것만) | 0 | 2 |
| ntdll | `^LdrpTouchThreadStack$` | 1 | 3 |
| ntdll | `^LdrpInitializeProcess$`, `^_LdrpInitialize$` | 0 | 부르는 자리 (`.asm` 있음 — 다시 뽑지 않음) |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where <모듈 파일 이름> <이름>`.
- 공용 부품은 이름만.

## 지도·`.asm` 에서 본 것 (오케스트레이터)

- ntdll 지도: `LdrpInitializeProcess +0x1def/+0x1df4 SymCryptDetectCpuFeaturesByCpuid` · `SymCryptDetectCpuFeaturesByCpuid +0x10d/+0x112 __security_check_cookie` (다른 직접 호출 없음) · `_LdrpInitialize +0x19c/+0x1a1 LdrpTouchThreadStack` · `LdrpTouchThreadStack +0x4e/+0x53 NtQueryVirtualMemory` · SEH 필터 `LdrpGenericExceptionFilter`
- `_LdrpInitialize.asm`: `+0x190 MOV RCX,[RSI+0x318]` · `+0x197 TEST RCX,RCX` · `+0x19a JZ 0x18008c16f` · `+0x19c CALL LdrpTouchThreadStack ret=+0x1a1`
- ntoskrnl `_index.tsv`: `KiSetFeatureBits` `0xb6805c` 2297B (run65 에서 `+0x888` 을 봄)

## 이미 아는 것

- `[정적]` run65: `KeProcessorFeaturesBuffer` (클래스 `0xfa`) 를 쓰는 코드는 `KiSetFeatureBits +0x888` 의 0 채우기뿐
- PE_emulator 117·118 원문 (위)

## 이번에 안 하는 것

- SymCrypt 나머지 (암호 함수 본문)
- 커널 CPU 기능 감지 전체 (KUSD `+0x274` 규칙 표에 필요한 만큼만)

## 받고 싶은 것

- 질문별 답 + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다, 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋. 결과서 쓰기 전에 `.asm` 줄과 다시 대조
- 1번 **CPUID 비트 → "없음" 비트 표**, 2번 **CPUID 비트 → `PF_*` 번호 표**
- PE_emulator 가 그대로 쓸 **작은 표**: 함수 × (입력 (CPUID/XGETBV/메모리) · 쓰는 칸 · 하위 호출 · syscall · 반환)
- 질문마다 **"정적으로 닫힘 / 동적 필요"**. 동적 몫은 Q104 VM 단계에 얹을 멈춤점·읽을 칸으로
- 짐 지도 반영거리 (SymCrypt 전역, KUSD `+0x274`, 스택 커밋)
