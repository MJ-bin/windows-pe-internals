# 84 sxs-create — 정적 분석 결과

- 요청서: `_workspace/sast/84_sxs-create.md` (Q151a, 첫 답은 지도 수준)
- 빌드: 26100.9457. **버전 확인: 일치** (아래 "버전 확인").
- 스크립트 문제: 없음 (`pdb_struct.py` 에 `_SECTION_IMAGE_INFORMATION` 정의가 없는 것만 메모 — 공개 PDB 한계).
- 중간에 사용량 한도로 한 번 멈췄다가 이어서 끝냄. 이어받을 때 `sxs`·`sxssrv` 의 `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄을 다시 확인함 (아래 표) → 다시 analyze 안 함.

## 모듈 / PDB

| 모듈 | 상태 | PDB guid+age | 뽑은 것 |
|---|---|---|---|
| kernelbase | 이미 analyze (sast/09 등) | (기존) | `CreateProcessInternalW` 는 기존 `.asm` 재사용, `BasepCsrCaptureSxsMessage` 1개 새로 |
| kernel32 | 이미 analyze (`.gpr`·`_index.tsv`·`SetPdb:` 다 있음) | (기존) | 13개 지정 + depth 1 = 35개 |
| basesrv | 이미 analyze | (기존) | 새로 안 뽑음 — sast/29 의 `.asm` 재사용 |
| **sxssrv** (새 analyze) | 2026-11-13 00:17 분석 끝, `SetPdb:` 있음, 함수 99개 | **`8FAA75EF96847C62E731DC9A759FE2C21`** | 21개 지정 + depth 1 = 44개 |
| **sxs** (새 analyze) | 2026-11-13 00:18 분석 끝, `SetPdb:` 있음, 함수 901개 | **`76674E9722F9EFB085A6BE05B23E9B371`** | `SxsGenerateActivationContext` depth 1 + 경로 만드는 함수 5개 depth 0 |

- **중요한 발견 (요청서 이름 정정)**: `BasepConstructSxsCreateProcessMessage`·`BasepInitAppCompatData`·`BasepReleaseSxsCreateProcessUtilityStruct` 는 **KernelBase 가 아니라 kernel32 에 있다.** KernelBase 는 `ext-ms-win-kernelbase-processthread-l1-1-0` 로 지연 import 해서 부른다 (`DelayLoad_BasepConstructSxsCreateProcessMessage` rva `0x12a3c4`, pefile 로 import 표 확인). 그래서 지도 `_edges.tsv` 에는 `external` 로만 찍혔다. 이번에 kernel32 프로젝트를 열어 뽑았다.
- ntdll·ucrtbase·condrv·user32·win32u·gdi32 프로젝트는 **열지 않음**.
- analyze 로그의 `Exception` 두 줄은 Ghidra 기본 스크립트(`WindowsResourceReference`) 경고와 분석기 이름이라 오류 아님.
- 출력 위치: `ms/decomp/{kernel32,sxssrv,sxs}/`, `ms/disassemble/{kernel32,sxssrv,sxs,kernelbase}/` (+ 각 `_calls.tsv`). 로그 `ms/ghidra/*.sast84*.stdout`.

## 버전 확인

- 이전 결과와 오프셋이 그대로 맞음: `CreateProcessInternalW +0x466b/+0x4672 → CsrClientCallServer` (sast/09), `+0x5795/+0x57d7/+0x5831 NtWriteVirtualMemory` (run58·59, 요청서), `BaseSrvCreateProcess2 +0x216/+0x21b → InternalBaseSrvCreateProcess` (sast/29), `InternalBaseSrvCreateProcess +0xf7/+0xfb` 간접 호출과 `sxsFunctions` (`_refs/sxsFunctions.tsv`).
- 새 일치: sxssrv 가 `functions` 표를 `BaseSrvRegisterSxS` 로 넘기는 줄 (`ServerDllInitialization +0xdf/+0xe6`) ↔ basesrv `sxsFunctions+0x8` 호출 → 표 `+0x8` 칸이 **`BaseSrvSxsCreateProcess`** (sxssrv rva `0x1cd0`) 인 것을 `pefile` 로 표(rva `0x90f0`)를 읽어 확인.

## 한 장 지도 (누가 → 누구, 오프셋은 call/ret)

```
[부모 test 를 만드는 프로세스]  KERNELBASE!CreateProcessInternalW
  +0x30e7/+0x30ee  NtCreateUserProcess   ← 커널이 매니페스트 자원 ID 1 을 찾고 PEB 주소를 돌려줌
  +0x4458/+0x445f  BasepConstructSxsCreateProcessMessage (kernel32, 지연 import)  ← SxS 칸 채우기
  +0x463b/+0x4640  BasepCsrCaptureSxsMessage (kernelbase rva 0xfadc0)            ← 문자열 캡처
  +0x466b/+0x4672  CsrClientCallServer(msg, capture, 0x1001d, 0x218)               ← csrss 로
  +0x56bb ~ +0x5831 (앱 호환) NtAllocateVirtualMemory → NtWriteVirtualMemory ×3     ← pShimData (SxS 아님)

[csrss]  csrsrv!CsrApiRequestThread(+0x4e6/+0x656) → basesrv!BaseSrvCreateProcess2 (표 [0x1d])
  +0x216/+0x21b  InternalBaseSrvCreateProcess
      +0xf7/+0xfb (ret +0x100)  sxsFunctions[+8] = sxssrv!BaseSrvSxsCreateProcess
          +0x8d/+0x92  BaseSrvSxsDoSystemDefaultActivationContext  (먼저)
          +0x4c/+0x51  InternalSxsCreateProcess                    (다음)
              +0x3f6  BaseSrvSxsProbeManifestAndPolicyAndDotLocal
              +0x66d/+0x672  BaseSrvSxsCreateActivationContextFromStructEx
                  +0x830  BaseSrvSxsGetActivationContextGenerationFunction (sxs.dll 로드)
                  +0x8d6/+0x8db  간접 호출 = sxs!SxsGenerateActivationContext
                  +0xadb  NtMapViewOfSection(자식)   +0xb09  NtWriteVirtualMemory(자식 PEB+0x2f8)
      +0x175/+0x17c  CsrCreateProcess (sast/29)
```

## 질문별 답 [정적]

### 1. 부모 쪽 (`CreateProcessInternalW` SxS 갈래)

- 답:
  1. **부모는 exe 의 자원을 직접 뒤지지 않는 게 기본이다.** 매니페스트(`RT_MANIFEST` 24, ID 1) 를 찾는 건 **커널** (`PspLocateInPEManifest`, sast/54): 찾으면 만들기 정보에 주소·크기를 돌려준다. 부모는 `+0x3f51 TEST byte [RSP+0x560],8` (R14B=8, 돌아온 `OutputFlags` 비트 3, 이름 `ManifestDetected` 는 phnt `[추측]`) 가 켜졌으면 `+0x3f5b`·`+0x3f6b` 에서 `[RSP+0x598]`(주소)·`[RSP+0x5a0]`(크기)를 지역 `[RSP+0x5f8]`·`[RSP+0x414]` 에 옮긴다. 이 둘이 `BasepConstructSxsCreateProcessMessage` 의 14번째·15번째 인자 → 메시지 `+0x50`·`+0x58`.
  2. 메시지 만드는 함수: **`kernel32!BasepConstructSxsCreateProcessMessage`** (rva `0x26070`, 1877B). 호출 `CreateProcessInternalW +0x4458`/ret `+0x445f`. 앞에서 `+0x4361`/`+0x4366` 로 "그 API 가 있나" (`ApiSetQueryApiSetPresence`) 확인, 없으면 `+0x4466` 으로 건너뜀. 인자 12·13·14번째(스택 `[RSP+0x78]·[RSP+0x80]·[RSP+0x88]`)가 **SxS 메시지 `[RSP+0xc58]`(0x1c8 바이트)** 와 처리 구조체 `[RSP+0xad0]`(0x58 바이트).
  3. **메시지 모양** (`CreateProcessInternalW` 안 지역 `[RSP+0xbe0]` 가 API 메시지 머리, `+0xc20` 이 데이터 시작): 데이터 `+0x0` ProcessHandle(`[RSP+0xc20]`) · `+0x8` ThreadHandle(`+0xc28`) · `+0x10` ClientId(`+0xc30`) · `+0x20` CreationFlags(`+0xc40`, `[RSP+0x108] & ~3`) · `+0x24` `+0xc44` (VDM 관련으로 보임 `[추측]`) · `+0x28` `+0xc48` · `+0x30` `+0xc50` · **`+0x38` SxS 메시지 0x1c8 바이트 (`+0xc58`)** · `+0x200` PEB 주소 native(`+0xe20` ← `[RSP+0x588]`) · `+0x208` PEB 주소 wow64(`+0xe28` ← `[RSP+0x590]`) · `+0x210` 프로세서 구조(`+0xe30`, word). 합쳐서 데이터 길이 **0x218**. **csrss 쪽 오프셋(`BaseSrvSxsCreateProcess`) = 메시지 머리 기준: SxS 시작 `+0x78`, CreationFlags `+0x60`, PEB native `+0x240`, wow64 `+0x248`, 구조 `+0x250`** — 위 표와 `0x40` 만큼 차이(머리 `0x40`)로 맞는다.
  4. **SxS 메시지 칸 (최소 칸)** — `kernel32` 쪽 쓰는 줄 / csrss 쪽 읽는 줄로 확인한 것만:
     | SxS 시작 기준 | 뜻 | 쓰는 줄 (`BasepConstructSxsCreateProcessMessage`) | csrss 읽는 줄 |
     |---|---|---|---|
     | `+0x0` Flags | 비트 `0x1` 매니페스트 스트림 있음 · `0x2` 정책(.Config) 스트림 · `0x10` (인자8 비트0) · **`0x20` SxS 건너뜀** (`+0x37f`) · **`0x40` "매니페스트는 csrss 가 직접 찾아라"** (`+0x622`) · `0x80` (`+0x673`) · `0x200` (`+0x63a`) · `0x400`·`0x800` (`+0x198`·`+0x1b3`, 대체 exe 경로 갈래) | 위 | `InternalSxsCreateProcess` 맨 위 게이트: `(flags & 0x20)==0 && (flags & 0x41)!=0` 일 때만 앱 문맥을 만든다 |
     | `+0x4` 프로세스 매개변수 Flags | 자식 `ProcessParameters.Flags` 사본. 비트 13 `0x2000` = 매니페스트 자원 찾음 (`+0x67c`), `0x1000` = `.local` 있음 (`+0x6f0`), `0x8000` (`+0x6b7`) | 끝에서 `+0x6f6 MOV [RBX+0x4],EAX` | 메시지 `+0x7c` — `InternalSxsCreateProcess +0x405 TEST [R15],0x2000` |
     | `+0x8` FileHandle | exe 파일 핸들 (`0x40` 갈래) | `+0x625` | `NtDuplicateObject +0x2d6/+0x2dd` 로 csrss 로 복제 |
     | `+0x10` · `+0x20` 경로 문자열 둘 | exe 경로 (UNICODE_STRING 16B ×2) | `+0x630` (→`+0x10`) · `+0x60a` (→`+0x20`) | 문자열 검증 `BaseSrvSxsValidateMessageStrings +0x26f` |
     | `+0x30` · `+0x38` | 덮어쓰기 매니페스트 주소·크기 (있을 때만) | `+0x5f9`·`+0x5f5` | `InternalSxsCreateProcess` 가 `+0xb0`/`+0xc0` 으로 봄 |
     | **`+0x50` · `+0x58`** | **자식 안 매니페스트 주소·크기 (커널이 준 값)** | `+0x616`·`+0x61d` | `Probe…` 인자 (`param_3 + 0xc8`, `+0xd0`) |
     | `+0x88` | 언어 대체 목록 UNICODE_STRING (16B) | `+0x649/+0x64e` `BasepSxsGetCreateProcessLanguageFallbacksCached` 또는 `DoubleNulString` | 문자열 검증 대상 |
     | `+0xa8` | **빈 "어셈블리 이름 자리"** — `"-----------------------------------------------------------"` 로 초기화 (`+0x69f`), csrss 가 여기에 결과를 쓴다 | `+0x691` | `CsrValidateMessageBuffer +0x800` 로 버퍼 확인 |
  5. 문자열 캡처: `BasepCsrCaptureSxsMessage` (kernelbase rva `0xfadc0`) — `Flags == 0` 이거나 `Flags & 0x20` 이면 아무것도 안 함 (`+0x1e`~`+0x25`). `0x40` 갈래는 **4개** (`+0x10`, `+0x20`, `+0x88`, `+0xa8`), 아닌 갈래는 **5개** (`+0x10`, `+0x48`, `+0x78`, `+0x88`, `+0xa8`) 를 `CsrCaptureMessageMultiUnicodeStringsInPlace` (`+0x5e/+0x65`) 로 캡처.
  6. **`CsrClientCallServer`** 부르는 자리: **`+0x466b`/ret `+0x4672`**. 인자: `+0x464f R9D=0x218` (데이터 길이), `+0x4655 R8D=0x1001d` (API 번호), `+0x465b RDX=[RSP+0x2c0]` (캡처 버퍼), `+0x4663 RCX=&[RSP+0xbe0]` (메시지). `0x1001d` = 서버 `1`(basesrv) + API `0x1d` = `BaseSrvCreateProcess2` (sast/29).
  7. **두 갈래** (`BasepConstructSxsCreateProcessMessage`):
     - **빠른 길 (보통)**: `+0x65a` 로 가는 조건 = (자식이 보호 프로세스 — `+0x11f` 로 읽은 `NtQueryInformationProcess` 클래스 0 의 0x40 바이트 결과 비트 0, `+0x390`~`+0x393` `JNZ +0x65a`) **또는** (`PreferExternalManifest`=0 `+0x399`, 인자 7 꺼짐 `+0x39f`, 이동식 매체 아님 `+0x3ab`/`+0x3b0`, 안전 부팅 아님 `+0x3b7 CMP byte [0x7ffe02ec]` = `KUSER_SHARED_DATA.SafeBootMode`, PDB 로 확인; `+0x3bf JZ +0x65a`). 가면 Flags |= `0x40` (`+0x622`), 경로·파일 핸들·커널이 준 매니페스트 주소/크기만 채우고 끝 (`+0x5fd`~`+0x63e`). **부모는 매니페스트 파일도 자원도 안 연다.**
     - **느린 길**: 위 조건이 하나라도 어긋나면 `+0x435` 로 가서 (`+0x39d`/`+0x3a6`/`+0x3b5` `JNZ +0x435`) `+0x5ba/+0x5bf` `BasepSxsCreateProcessCsrMessage` (kernel32 rva `0x26ae4`) → 경로 뒤에 **`.Manifest`**, **`.Config`** 를 붙여 (문자열 상수 `SxsManifestSuffix` rva `0x886b0`, `SxsPolicySuffix` rva `0x886a0` 을 읽음; `+0x63`·`+0xac`·`+0xea`·`+0x119`) `BasepSxsCreateStreams` (rva `0x258a8`, call `+0x1ac/+0x1b1`). 그 안:
       - 설정 읽기: `BasepGetPreferExternalManifestConfig` (rva `0x25afc`) = `HKLM\Software\Microsoft\Windows\CurrentVersion\SideBySide` 의 `PreferExternalManifest` (`NtOpenKey +0xa0`, `NtQueryValueKey +0xf0`; 전역 `DAT_1800c1310` 에 -1=안 읽음).
       - 값 0: **자원 먼저** (`BasepSxsCreateResourceStream` rva `0x25c90`, `LdrResSearchResource` call `+0x95/+0x9c`, 경로 {타입 `0x18`=24, ID, 언어 0}, 수준 3, 크기 8MB 넘으면 거부 `0x800001` 미만만), 없으면 **외부 파일** (`BasepSxsCreateFileStreamEx` rva `0x25e5c`: `NtOpenFile +0x9c`, `NtQueryInformationFile +0x11f`, **`NtCreateSection +0x1c1`** — 파일을 읽기 전용 섹션으로 만들어 스트림 구조에 핸들 저장, 8MB 넘으면 거부).
       - 값 1: 순서 반대 (파일 먼저, 자원은 대신).
       - 그다음 `.Config` (정책) 도 같은 파일 함수로 → Flags `0x1`·`0x2`.
  8. **매니페스트 없을 때**: 메시지는 **그대로 보낸다** (`CsrClientCallServer` 는 SxS 와 별개로 프로세스 등록 알림이라 늘 불림). 부모 쪽 변화는 없다 (빠른 길이면 Flags=`0x40`, 주소·크기 칸 0). **없는지 알아보는 쪽은 csrss**: `InternalSxsCreateProcess +0x3f6/+0x3fb` `BaseSrvSxsProbeManifestAndPolicyAndDotLocal` 가 앱 호환 캐시 → 자식 메모리의 자원 → `.Manifest` 파일 → `.local` 순으로 찾고, 못 찾으면 `+0x405 TEST [R15],0x2000` / `+0x40c JZ +0x8b2` 로 앱 문맥 만들기를 건너뛴다 → `PEB+0x2f8` 은 0 으로 남는다. **시스템 기본 문맥은 이 앞 단계(`BaseSrvSxsCreateProcess +0x8d`)에서 이미 들어간다** (질문 3).
  9. **건너뛰는 갈래**:
     - `+0x4328`~`+0x4336`: 돌아온 `OutputFlags` 비트 0 (보호 프로세스 `[추측: 이름]`) 이 켜졌고 비트 4 (경량 보호 `[추측]`) 가 꺼졌으면 `+0x4477` 로 건너뛰어 `BasepConstructSxsCreateProcessMessage` 를 **안 부른다** (SxS 칸 0 → `BasepCsrCaptureSxsMessage` 도 즉시 끝). 같은 조건에서 `+0x4623 OR [RSP+0xc28],2` 로 **ThreadHandle 칸 비트 1** 을 켠다 → csrss `BaseSrvCreateProcess2 +0x62` 가 이 비트를 읽어 `InternalBaseSrvCreateProcess` 첫 인자로 넘기고, 그게 0 이 아니면 SxS 호출 자체를 건너뛴다 (`+0xca TEST ESI,ESI; JNZ +0x108`). 즉 **그 프로세스는 시스템 기본 문맥도 못 받는다** (`+0x108` 로 직행).
     - `Flags |= 0x20` (`+0x37f`): 인자 11 (`[RSP+0x89e]` 의 비트 9) 가 켜졌을 때. `[RSP+0x870]` 은 `SECTION_IMAGE_INFORMATION` 시작으로 보이고 (`[RSP+0x890]` 비교가 서브시스템 2 와 맞음), 그 `+0x2e` word = `DllCharacteristics` 비트 9 = `IMAGE_DLLCHARACTERISTICS_NO_ISOLATION` `[추측]` — PDB 에 이 구조체 정의가 없어서 칸 이름은 확인 못 함 `[공백]`.
- 근거: `ms/disassemble/kernelbase/CreateProcessInternalW.asm` +0x3f51~+0x3f72, +0x4328~+0x4336, +0x4361~+0x4366, +0x4458, +0x4613~+0x4672; `ms/disassemble/kernel32/BasepConstructSxsCreateProcessMessage.asm` +0x37d~+0x3b7, +0x5ba~+0x6e1; `BasepSxsCreateProcessCsrMessage.asm`; `BasepSxsCreateStreams.asm`; `BasepSxsCreateFileStreamEx.asm`; `ms/disassemble/kernelbase/BasepCsrCaptureSxsMessage.asm`.
- 지금 PE_emulator: **부모 쪽 흉내는 필요 없다** (부모·csrss 가 없으니 메시지를 만들 일이 없음). 대신 **커널이 해야 할 일 — 매니페스트를 찾아 `PEB.ProcessParameters.Flags` 비트 13 을 켜는 것 — 이 있는지 확인 필요** (sast/54 로 코드 위치는 알려짐; PE_emulator 쪽은 이 요청 범위 밖).

### 2. csrss 쪽 — 누가 무엇을 자식에 넣나

- 답: **자식 PEB 에 주소를 쓰는 쪽은 부모가 아니라 csrss 다.** 블롭을 만드는 함수는 sxssrv 가 `sxs.dll` 을 불러와 부르는 **`sxs!SxsGenerateActivationContext`** (rva `0x5a2b0`, 내보내기). 만든 블롭은 **"섹션으로 만들어 자식에 매핑 + 자식 메모리에 주소 쓰기"** 둘 다 한다.
- **누가 무엇을 자식에 넣나 표**

| 차례 | 프로세스 | 함수 + 오프셋 (call/ret) | syscall | 자식에 생기는 것 |
|---|---|---|---|---|
| 1 | 커널 (부모의 `NtCreateUserProcess`) | `CreateProcessInternalW +0x30e7/+0x30ee` 안, `PspLocateInPEManifest` (sast/54) | (같은 호출) | 자식 PEB 생성, `ProcessParameters.Flags` 비트 13 (매니페스트 찾았을 때), 만들기 정보에 매니페스트 주소·크기 |
| 2 | 부모 | `CreateProcessInternalW +0x466b/+0x4672` | `CsrClientCallServer` → ALPC | 아직 없음 (csrss 로 PEB 주소 `+0x200`/`+0x208`, 프로세스 구조, SxS 칸을 보냄) |
| 3 | csrss | `BaseSrvSxsCreateProcess +0x8d/+0x92` → `BaseSrvSxsDoSystemDefaultActivationContext +0x1ab/+0x1b2` | `NtMapViewOfSection` (csrss 가 가진 **시스템 기본 섹션** → 자식) | 자식 VAD 1개: 시스템 기본 블롭 (읽기 전용 공유 보기 `[추측: 보호값 R14D=2]`) |
| 4 | csrss | 같은 함수 `+0x211/+0x218` | `NtWriteVirtualMemory` (자식에 8바이트) | **`PEB+0x308` `SystemDefaultActivationContextData`** ← 매핑 주소 (PDB `_PEB` 로 `+0x308` 확인) |
| 5 | csrss (wow64 일 때) | 같은 함수 `+0x279/+0x280` | `NtWriteVirtualMemory` (4바이트) | `PEB32+0x200` `SystemDefaultActivationContextData` (PDB `_PEB32`) |
| 6 | csrss | `InternalSxsCreateProcess +0x2d6/+0x2dd` | `NtDuplicateObject` (부모의 exe 파일 핸들 → csrss) | csrss 쪽 핸들만 (`0x40` 갈래) |
| 7 | csrss | `InternalSxsCreateProcess +0x330/+0x337` | `NtReadVirtualMemory` (자식 `PEB+0x10` = `ImageBaseAddress`) | 읽기만 |
| 8 | csrss | `+0x3f6` `BaseSrvSxsProbeManifestAndPolicyAndDotLocal` → `BaseSrvSxsCheckManifestAndDotLocal` | `NtApphelpCacheControl` (`g_CompatCacheServiceApi`, `+0x126`), 자원 스트림, `.Manifest` 파일, `.local` | 읽기만 (`CsrImpersonateClient` 로 **호출한 쪽(부모) 토큰을 흉내 내고** 파일 접근) |
| 9 | csrss | `+0x66d/+0x672` → `BaseSrvSxsCreateActivationContextFromStructEx` → 간접 호출 `+0x8d6/+0x8db` = **`sxs!SxsGenerateActivationContext`** | (sxs 안: `CreateFileMappingW`(익명, 읽기쓰기) + `MapViewOfFile`, `SxspBuildActCtxData +0x16c/+0x1a4`) | csrss 안에 블롭 섹션 (자식 아님). 같은 키면 **캐시 재사용** (`BaseSrvSxsGetCacheKey +0x414`, `LookupEntry +0x468`, `InsertEntry +0xa37`) |
| 10 | csrss | `FromStructEx +0xadb/+0xae2` | `NtMapViewOfSection` (섹션 → **자식**, 보기 공유 1, 읽기 전용 2) | 자식 VAD: **앱 블롭** (매니페스트가 있을 때만) |
| 11 | csrss | `FromStructEx +0xb09/+0xb10` | `NtWriteVirtualMemory` (8바이트) | **`PEB+0x2f8` `ActivationContextData`** ← 매핑 주소 (`InternalSxsCreateProcess +0x60c LEA [R13+0x2f8]`) |
| 12 | csrss (wow64) | `FromStructEx +0xb54/+0xb5b` | `NtWriteVirtualMemory` (4바이트) | `PEB32+0x1f8` `ActivationContextData` (`InternalSxsCreateProcess +0x8c9 ADD RAX,0x1f8`) |
| 13 | csrss | `InternalBaseSrvCreateProcess +0x175/+0x17c` | `CsrCreateProcess` (sast/29) | csrss 쪽 프로세스 등록 (자식 메모리 아님) |
| 14 | **부모** (SxS 아님) | `CreateProcessInternalW +0x574e/+0x5755`, `+0x578e/+0x5795`, `+0x57d0/+0x57d7`, `+0x582a/+0x5831` | `NtAllocateVirtualMemory`, `NtWriteVirtualMemory` ×3 | 앱 호환 데이터 개인 영역 + **`PEB+0x2d8` `pShimData`** (8B) + `PEB32+0x1e8` (4B). `BasepGetAppCompatData` 가 값을 줬을 때만 (`+0x56c7 TEST RAX; JZ`) |

- 순서 메모: 14 (부모) 는 2 (`CsrClientCallServer`) **뒤**다 (`+0x56bb > +0x466b`). run45 사진에서 시스템 기본 매핑 `25996d00000` 이 앞, pShimData 개인 영역 `25996d10000` 이 뒤인 것과 맞는다.
- 부모가 쓰는 PEB 칸: `+0x2d8` (`pShimData`) 만. `+0x2f8`·`+0x308` 은 **쓰지 않는다** → "부모가 `SystemDefaultActivationContextData` 를 쓰나" 는 **아니오** (Q87 닫힘, 아래).
- csrss 가 SxS 를 부르는 조건 (`InternalBaseSrvCreateProcess +0xca`~`+0xee`): 첫 인자 0 (ThreadHandle 비트 1 꺼짐), **부모의 CSR_PROCESS `[+0x5c]` 비트 `0x2000` 꺼짐**, `sxsFunctions` 채워짐, 메시지 포인터 있음. 그 비트는 `InternalBaseSrvCreateProcess +0x108~+0x10c` 가 첫 인자≠0 일 때 `CsrCreateProcess` 에 `0x2000` 을 넘겨서 켜는 것으로 보이나 (뜻/이름은 `[공백]`), 부모가 SxS 없이 등록된 프로세스면 자식도 건너뛰는 모양이다 `[추측]` → 동적.
- 근거: `ms/disassemble/sxssrv/BaseSrvSxsCreateProcess.asm` (+0x1a~+0x96), `InternalSxsCreateProcess.asm`, `BaseSrvSxsCreateActivationContextFromStructEx.asm` (+0xa70~+0xb64), `BaseSrvSxsDoSystemDefaultActivationContext.asm` (+0x172~+0x289), `ms/disassemble/basesrv/InternalBaseSrvCreateProcess.asm` (+0xca~+0x100), `ms/disassemble/sxs/SxspBuildActCtxData.asm` (+0x152~+0x1ab).
- 지금 PE_emulator: **빠져 있다 (틀림).** 해야 할 일은 **3~5·10~12 (매핑 + PEB 쓰기) 를 nt 엔진이 대신하는 것** 하나다. 6~9 (해석) 는 블롭을 미리 떠 오면 필요 없다 (질문 5).

### 3. 시스템 기본 문맥 (`SystemDefaultActivationContextData`)

- 답:
  - **매니페스트 없는 exe 에도 늘 만든다 (부모가 SxS 를 건너뛰는 보호 프로세스류만 빼고).** `BaseSrvSxsCreateProcess` 가 **먼저** `BaseSrvSxsDoSystemDefaultActivationContext` 를 부르고 (`+0x8d`/ret `+0x92`), 실패(음수)면 앱 문맥은 건너뛴다 (`+0x94 JS +0x51`). 이 갈래 조건은 메시지 `+0x60` (CreationFlags) 의 **비트 31 이 꺼져 있을 때** (`+0x1a CMP [R8+0x60],0; JGE +0x6d`) 뿐이다. 비트 31 을 켜는 줄은 `CreateProcessInternalW` 에서 못 찾았다 (`BTS`/`BTR` 는 비트 `0xa`·`0xb`·`0x16` 뿐) → 보통 꺼짐 `[추측]`. run45 의 `25996d00000` (test.exe 에도 있음) 과 맞다.
  - **한 번만 만들고 돌려쓴다.** csrss 안 전역 표 `SxsSystemDefaultActivationContexts` (5칸, 구조별) 에 섹션 핸들을 두고 (`+0x12d` 임계영역 안에서 비었는지 확인), 비었을 때만 (`+0x5ef`/ret `+0x5f4`) `FromStructEx(-1, -1, 구조체, NULL, &핸들)` 로 만든다. 그 뒤엔 **같은 섹션을 새 프로세스마다 자식에 매핑**만 한다 (`+0x1ab`).
  - 구조 번호: 메시지 `+0x250` word 가 `0` → `10`, `5` → `13`, 나머지는 그대로. 표(`rva 0xc038`, 칸 간격 0x20) 의 구조 값은 `10, 13, 6, 9, 12` (= x86-on-x64, ARM32-on-x64, IA64, AMD64, ARM64 로 읽힘 `[추측: PROCESSOR_ARCHITECTURE 값]`).
  - **내용을 정하는 것**: 코드가 만드는 **합성 문맥 요청** 하나뿐이다 — 어셈블리 이름 문자열 `Microsoft.Windows.SystemCompatible,version="6.0.0.0",type="win32",publicKeyToken="6595b64144ccf1df",processorArchitecture="<구조>"` (sxssrv 문자열 상수; `ms/decomp/sxssrv/BaseSrvSxsDoSystemDefaultActivationContext.c` 의 `local_2e0` — 만드는 함수 호출은 `.asm` `+0x433`·`+0x523` `RtlMultiAppendUnicodeStringBuffer`), 이름표 `"System Default Context"`, 어셈블리 루트 `<Windows 폴더>\WinSxS` (`ServerDllInitialization +0x4f~+0xaf` 에서 `<SystemRoot>` 를 펼쳐 `BaseSrvWindowsDirectory`, `\system32\sxs.dll` 를 `BaseSrvSxsDllPath` 에 둠), 언어 대체 목록 (`BasepSxsGetLanguageFallbacks`). 실제 문맥 내용(어떤 OS GUID 를 지원한다고 선언하는지 등)은 **WinSxS 안의 `SystemCompatible` 어셈블리 매니페스트** 에서 오는 것으로 보이며 (이름으로 추정) 이 레포의 `ms/bin/.../WinSxS` 사본에는 그 폴더가 없어서 확인 못 함 `[공백]` — Windows 버전(빌드)이 곧 내용을 정한다.
- 근거: `ms/disassemble/sxssrv/BaseSrvSxsCreateProcess.asm` +0x1a~+0x96, `BaseSrvSxsDoSystemDefaultActivationContext.asm` +0x12d, +0x1ab, +0x211, +0x279, +0x5ef; `ServerDllInitialization.asm` +0x4f~+0xdf; 표는 `pefile` 로 sxssrv rva `0xc038` 읽음.
- 지금 PE_emulator: **빠져 있다 (틀림).** test.exe 도 `PEB+0x308` 이 채워져야 맞다 (run45). 나중에 comctl32 v6 를 쓰는 앱이 오기 전에도 필요한지는 로더가 `PEB+0x308` 을 언제 읽는지 (Q151b) 에 달림.

### 4. WinSxS 경로 규칙

- 답 (일부만 `[정적]`, 나머지는 Q151b 로):
  - **csrss(sxs.dll) 쪽 `[정적]`**: 어셈블리 이름 → 폴더 이름 만드는 함수들이 `sxs.dll` 안에 있다 — `SxspGenerateKeyform` (rva `0x15b70`, `CreateKeyformFromAttributesIntoBuffer` 를 부름), `SxspGenerateTextualIdentity` (rva `0x120c0`), `SxspGenerateSxsPath` (rva `0x3b930`), `SxspGenerateManifestPathForProbing` (rva `0x143a0`), `SxspGenerateManifestPathOnAssemblyIdentity` (rva `0x5f260`). 문자열 상수 `\WinSxS\`, `manifests`, `.manifest` 가 sxs.dll 에 있다. 즉 **의존 어셈블리를 찾을 때 (매니페스트 해석 때) csrss 쪽에서 `WinSxS\manifests\<키폼>.manifest` 로 간다.** 이 레포 `ms/bin/.../WinSxS` 사본의 폴더 이름이 곧 이 모양이다: `amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201\comctl32.dll` (6.0 두 개: `.1591`, `.9278`; 5.82 두 개) — 그래서 "6.0.0.0 요청 → 어떤 6.0.26100.x 로 묶이나" 는 **해석 때 정책**(`SxspParseApplicationPolicy`, 정책 폴더)으로 정해질 것이다 `[추측]` → 덤프로 확인.
  - **블롭 안에 전체 경로를 넣나 키만 넣나**: 이번 바퀴에서 **확인 못 함 `[공백]`**. `SxspBuildActCtxData` 가 쓰는 줄이 아직 안 읽혔고 (블롭 머리 일부만 봤음, Q151b 재료), 이름표 위치는 PDB 에 구조체 정의가 없다. 시스템 기본 문맥을 만들 때 csrss 가 **루트 폴더(`<Windows>\WinSxS`) 를 인자로 주는** 것은 확인됐다 — 블롭이 폴더 하나하나의 전체 경로 대신 "루트 + 어셈블리 폴더 이름" 으로 나눠 가진다는 쪽을 가리킨다 `[추측]`.
  - **실제 폴더를 여는 쪽 `[추측]`**: ntdll `_index.tsv` 에 `RtlpResolveAssemblyStorageMapEntry`(rva `0xb6848`, 1696B), `RtlpProbeAssemblyStorageRootForAssembly`(rva `0xb749c`), `RtlpGetAssemblyStorageMapRootLocation`(rva `0xb7984`), `RtlpInitializeAssemblyStorageMap`(rva `0xb6694`) 가 있다. `.asm` 이 아직 없어서 안 읽었다 (ntdll 프로젝트 안 열기로 했음). PEB 의 `ProcessAssemblyStorageMap`(`+0x300`)·`SystemAssemblyStorageMap`(`+0x310`) 이 이쪽이 쓰는 칸으로 보이며 csrss 는 이 둘을 안 쓴다 (sxssrv 에서 이 오프셋 참조 없음) → **폴더를 실제로 해석해 DLL 경로를 만드는 건 로더 때 (ntdll)** 로 보인다.
- 근거: `ms/decomp/sxs/_index.tsv` (함수 이름·rva), `ms/disassemble/sxs/_calls.tsv`, `strings` 로 sxs.dll UTF-16 문자열, `ms/bin/.../WinSxS/` 폴더 목록 (16개 폴더).
- 지금 PE_emulator: **블롭 안 들어가는 것의 모양을 알기 전엔 못 정한다.** 최소한 `ms/bin/.../WinSxS/` 사본에 comctl32 v6 폴더(`.9278`, `.1591`) 는 있어서 파일 쪽은 맞출 수 있다.

### 5. nt 엔진이 직접 만들어 넣는다면

- 판단 (한 줄): **미리 만든 블롭 두 개(시스템 기본 1개 + 앱 1개)를 덤프에서 떠 와서 지정 주소에 매핑하고 `PEB+0x308`·`+0x2f8` 에 그 주소만 쓰는 게 가장 싸다.** 매니페스트 XML 해석·정책·캐시는 흉내 안 내도 된다.
- 이유:
  1. 자식에 실제로 닿는 건 **포인터 두 개 + 읽기 전용 메모리 덩어리 둘** 뿐이다 (표 3~5, 10~12). 해석 단계(6~9) 결과는 csrss 안에서만 오가고, 블롭은 `CreateFileMappingW` 로 만든 **익명 메모리 섹션** (`SxspBuildActCtxData +0x16c/+0x1a4`) 을 자식에 읽기 전용으로 매핑한 것이라, 자식 입장에서는 "그 주소에 읽을 수 있는 바이트가 있다" 면 된다.
  2. 블롭 안 위치는 **블롭 시작 기준 오프셋** (`SxspBuildActCtxData +0x1bf~+0x215`: `머리 오프셋 = 포인터 − 시작`) 이라 어느 주소에 매핑해도 그대로 읽힐 가능성이 높다 `[추측: 블롭 전체가 상대 주소인지는 덤프 비교로 확인]`.
  3. 머리: `+0x0` = `0x78746341` (`"Actx"`), `+0x4` = `0x20`, `+0x8` = `1`, `+0xc` = 전체 크기, `+0x10`·`+0x14`·`+0x18` = 표/명부 오프셋 (이름은 공개 구조체 이름 `[추측]`), `+0x1c` 플래그 (`SxspBuildActCtxData +0x1c3~+0x24d`).
- 한계: (a) 앱 블롭은 **exe 마다** 다르고 (매니페스트가 달라서) 시스템 기본은 **구조마다** 다름, (b) 같은 빌드·같은 WinSxS 폴더에서만 맞음, (c) `ProcessAssemblyStorageMap`(`+0x300`) 같은 칸은 로더가 나중에 직접 채우는 걸로 보이나 `[추측]` Q151b.
- 지금 PE_emulator: **맞는 방향** (요청서의 "덤프에서 떠 와 매핑" 과 같음). 필요한 것: 매핑 크기·보호값(읽기 전용)·**64KB 정렬 주소** (run45 사진 `25996d00000`) 정도.

## Q87 닫힘

- 질문: "부모 `KERNELBASE!BasepInitAppCompatData` 본문 — 부모가 자식 PEB `pShimData`·`SystemDefaultActivationContextData` 를 쓰나?"
- 답 `[정적]`:
  - `BasepInitAppCompatData` 는 **kernel32** 에 있다 (rva `0x78e40`, 123B). `CreateProcessInternalW +0x5976/+0x597d` 가 지연 import 로 부른다. 하는 일: 0x188 바이트 구조체를 0 으로 채우고 인자 셋을 넣어 **`g_CompatCacheServiceApi(0xa, 구조체)`** 를 부를 뿐 (`+0x2f` memset, `+0x34`~`+0x5c`). **PEB 를 쓰지 않는다.**
  - **`pShimData`**: 부모가 `CreateProcessInternalW +0x57d0/+0x57d7` 에서 `PEB+0x2d8` 에 8바이트, `+0x582a/+0x5831` 에서 `PEB32+0x1e8` 에 4바이트를 `NtWriteVirtualMemory` 로 쓴다. 값은 `+0x574e/+0x5755` 에서 자식 안에 `NtAllocateVirtualMemory` 로 받은 개인 영역 (크기 `[RSP+0x1f8]`, 보호 4) 의 주소이고 내용은 `+0x578e/+0x5795` 가 `BasepGetAppCompatData` 결과 (`[RSP+0x2a0]`) 를 복사.
  - **`SystemDefaultActivationContextData`·`ActivationContextData`**: 부모는 안 쓴다. csrss 만 쓴다 (질문 2 표 4·5·11·12).

## 동적으로 확인할 것 (Q104 / Q83 에 붙임)

1. **블롭 덤프 (Q104 사진 P0)**: 자식 `PEB+0x308`, `+0x2f8` 가 가리키는 주소에서 `Actx` 머리 (`+0xc` 전체 크기) 부터 전체 크기만큼 덤프. test.exe 는 `+0x308` 만, notepad 같은 GUI 앱은 둘 다. 덤프 안에 절대 주소처럼 보이는 8바이트 값이 있는지 확인 (질문 5 의 상대 오프셋 가정).
2. **`!address` / `!vad`** 로 두 보기의 크기·보호값·유형 (Mapped/섹션) 확인. 정적으로 본 `NtMapViewOfSection` 인자 (`AllocationType 0x400000`, 공유 1, 보호 `[추측: 2]`) 의 실제 효과.
3. **PEB 쓰기 순간 (Q83)**: csrss 에서 `sxssrv!BaseSrvSxsDoSystemDefaultActivationContext +0x211` (rva `0x1651`), `sxssrv!BaseSrvSxsCreateActivationContextFromStructEx +0xb09` (rva `0x3c79`) 의 `NtWriteVirtualMemory` 인자 `rdx` = 자식 `PEB+0x308`/`PEB+0x2f8`. csrss 에 멈춤점을 걸려면 csrss 프로세스 문맥이 필요.
4. **빠른 길인지**: 부모에서 `CreateProcessInternalW +0x445f` (ret) 직후 `dd @rsp+0xc58 L2` → Flags 가 `0x40` 인지 (test.exe 는 자원 디렉터리가 없다, sast/54), 메시지 `+0x7c` (프로세스 매개변수 사본 Flags) 비트 13.
5. **csrss 가 SxS 를 부르는 조건** (`InternalBaseSrvCreateProcess +0xca`~`+0xee`): 첫 인자(`esi`)·부모 CSR_PROCESS `[+0x5c]` 값이 실제로 0 / `0x2000` 꺼짐인지. test.exe 일 때 `+0xf7` 간접 호출이 타는지.
6. **정책/버전 묶기**: notepad 류에서 comctl32 6.0 이 `.1591` 인지 `.9278` 인지 (블롭 안 폴더 이름 문자열로 확인).
7. **앱 문맥 없는 exe (test.exe)**: `PEB+0x2f8` 이 0 인지 (`InternalSxsCreateProcess +0x3f6` 뒤 `*(+0x7c) & 0x2000 == 0` 로 빠지는 길의 결과).
- 추천 멈춤점 (csrss 안): `sxssrv!BaseSrvSxsCreateProcess` (rva `0x1cd0`) 입구 — `r8` = 메시지, `[r8+0x78]` Flags, `[r8+0x7c]` 매개변수 Flags, `[r8+0x240]` PEB, `[r8+0x60]` CreationFlags. (dast 요청 필요: 위 1~5 번 — 커널 디버거로 csrss 문맥에서 걸기.)

## Q151b 로 넘길 것 (로더가 읽는 쪽 + 블롭 모양)

- **블롭 머리·섹션 모양 재료**: `ms/disassemble/sxs/SxspBuildActCtxData.asm` (rva `0x1d1c0`, 1055줄) — 머리 `+0x0~+0x1c` 를 `+0x1c3~+0x24d` 에서 채움; 표 머리 `+0x20` (`0x14`, `1`, 개수+1, 첫 항목 오프셋) 가 `+0x1ea~+0x21a`. 이후 어셈블리 명부·문자열 영역 채우는 줄은 안 읽음. 입력 `_ACTCTXGENCTX` (`SxsGenerateActivationContext +0x3c2` `SxspInitActCtxGenCtx`, `+0x5bc` `SxspAddRootManifestToActCtxGenCtx`, `+0x61d` `SxspBuildActCtxData` — PDB 이름은 `.asm` 의 ALIASES 줄에 있음).
- **로더 쪽 읽는 함수** (ntdll, 이미 `.asm` 있는 것 + `_index.tsv` 에만 있는 것): `.asm` 있음 — `LdrpFindDllActivationContext`, `RtlpLocateActivationContextSection`; `_index.tsv` 만 — `RtlpFindActivationContextSection_FillOutReturnedData`(rva `0x6fe88`), `RtlpResolveAssemblyStorageMapEntry`, `RtlpProbeAssemblyStorageRootForAssembly`, `RtlpGetAssemblyStorageMapRootLocation`, `RtlpInitializeAssemblyStorageMap`, `RtlpInsertAssemblyStorageMapEntry`.
- **자식이 읽는 PEB 칸 (PDB `_PEB`)**: `+0x2d8` pShimData, `+0x2f8` ActivationContextData, `+0x300` ProcessAssemblyStorageMap, `+0x308` SystemDefaultActivationContextData, `+0x310` SystemAssemblyStorageMap. `_PEB32`: `+0x1e8`, `+0x1f8`, `+0x1fc`, `+0x200`, `+0x204`.
- **WinSxS 규칙 확정**에 필요: 블롭에서 어셈블리 폴더 이름 칸이 어떻게 놓이는지 (질문 4 `[공백]`), `SxspGenerateKeyform` 의 키 구성 순서.

## 다음 요청 후보

1. **Q151b** (정적): ntdll 로더 쪽 — `LdrpFindDllActivationContext`/`RtlpLocateActivationContextSection` 의 `.asm` 은 이미 있으니 먼저 읽고, 저장소 맵 함수 6개를 depth 0 으로 뽑기 (ntdll 프로젝트 필요).
2. **sxs 블롭 채우기** (정적): `SxspBuildActCtxData` 와 `SxspAddRootManifestToActCtxGenCtx` 에서 명부·문자열 영역 칸 (질문 4 의 폴더 이름 칸 포함). sxs 는 이제 analyze 되어 있음.
3. **dast** (Q104/Q83 에 붙임): 위 "동적으로 확인할 것" 1~5 — 특히 1 (블롭 덤프) 이 질문 5 의 판단 근거.
4. (요청서 범위 밖 확인) PE_emulator 의 `NtCreateUserProcess` 흉내가 `PS_CREATE_INFO` 매니페스트 주소/크기와 `ProcessParameters.Flags` 비트 13 을 이미 쓰는지.

## 판정

**분석성공 (지도 수준).** 질문 1·2·3 은 함수 이름·오프셋·syscall 까지 답했고, 5 는 판단 + 이유, **4 는 일부(csrss 쪽 함수·폴더 모양)만** — 블롭 안 폴더 칸과 로더 쪽 해석은 `[공백]` 으로 Q151b 로 넘김. Q87 닫힘.
