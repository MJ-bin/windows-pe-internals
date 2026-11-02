# 54 init-leftovers — 정적 분석 요청

- 날짜: 2026-11-02
- 스프린트: 03_sprint / 예정 run: run59
- 대기열: **Q109 나머지 (B·C·D (1)~(3)·E)** — PE_emulator 요청 4-B~E (사용자 결정 2026-11-01). ⓠ 는 run58 에서 끝남 (이번 범위 밖)
- 관련: run17 (sast/12 `_LdrpInitialize` 입구), run18 (sast/13 `LdrpInitializeProcess` 묶음), run32 (sast/27 매개변수·시작 정보 블록), run45 (sast/39 일 목록), run50 (sast/50 todo-vm), run54 (sast/48 TEB), baggage.md 3.4·3.8·7.3
- 원문: `../PE_emulator/_workspace/orchestrator/37_nls-codepage.md` "internals 요청 4" 절, `../PE_emulator/_workspace/00_planner/{30..37}_*.md` "internals 에 알릴 것" 절 (자리·이름이 헷갈리면 여기를 본다)
- 왜: PE_emulator 가 이 함수들을 "공사 중 빈 함수" 로 두고 있다. 무엇을 쓰고 syscall 을 부르는지 알아야 채운다. D 는 PE_emulator 가 asm 으로 이미 닫은 것 — 맞는지 확인만.

## 크기·순서
- 묶음이 크다. **D → B → C → E** 순서로 한다. 다 못 하면 결과서에 "끝낸 것 / 못 한 것" 을 나눠 적고 멈춘다 (못 한 것은 다음 바퀴로 나눔).
- 각 질문의 답은 **예/아니오 또는 함수·전역 이름 + `.asm` `+0x` 줄**. 오프셋은 `.asm` 의 `CALL … ret=` 줄로 맞춰 **CALL 자리인지 ret 자리인지 밝힌다** (run56~58 에서 세 번 섞임). 분기 목적지는 함수 시작 주소를 빼서 오프셋으로 (주소 끝자리를 오프셋으로 읽지 않기 — run57·run58 정정).
- 이미 아는 것 (아래) 과 같으면 "같음 + 근거" 한 줄로 끝낸다.

## 알고 싶은 것

### D 기록 정정 확인 (ntdll, 빠름)
1. baggage 7.3 "쓴 자리 모름" `RtlpDynamicFunctionTable*`·`RtlpDynamicFunctionTableTreeMin/Max`·`RtlpDynamicCallbackTableTreeMin/Max` 를 쓰는 곳이 `_LdrpInitialize +0x10d ~ +0x13e` 인가 (+ `.data` `RtlpDynamicFunctionTableLock`). 칸마다 쓰는 값.
2. run32·baggage 의 시작 정보 블록 `+0x98` 을 읽는 곳 `[추측]` → `LdrInitializePerThreadRng +0x4`·`LdrpProcessMappedModule +0xe7` 이 맞나 (읽는 칸 크기·쓰임).
3. baggage 3.4 `_PEB_LDR_DATA +0x4 Initialized` = 1 을 쓰는 줄이 `LdrpInitializeProcess +0x162` 인가. `RtlpFcProcessManager` 쓰기 `+0x20d ~ +0x286` (어느 칸에 무엇), 스핀 횟수 쓰기 `+0x1cd ~ +0x203` 이 몇 곳·어느 전역인가 (run18 묶음 2 엔 "스핀 3곳" 만).

### B ntdll 본문 (`.asm`·`.c` 없으면 뽑기)
4. `InitSecurityCookie` (rva `0xe1548`, 112B, `LdrpInitialize +0x41`) · `InitSpecialMachineFrames` (rva `0xe14b4`, 139B, `+0x22`) · `__cpu_features_init` (rva `0x12e47c`, 130B, `+0x27`) 각각:
   - 쓰는 전역과 값. 특히 깃발 `SecurityCookieInitialized` (rva `0x1d23ce`)·`SpecialMachineFramesInitialized` (rva `0x1d23d0`) 를 1 로 쓰는 줄.
   - syscall·하위 호출, 갈래 조건 (두 번째 스레드면 건너뛰나 등).
   - 힌트 이름 `SecurityCookieInitCount`·`SpecialMachineFramesInitCount`·`__isa_info` (baggage 7.3) 이 PDB 이름으로 있나 (`pdb_aliases.py --where ntdll <이름>`), 이 함수들이 쓰나.
   - `__cpu_features_init` 이 run44 사진 P1b~P2 사이에 바뀐 CPU 기능 전역을 쓰나 (baggage 에서 그 구간 전역 이름 찾아 대조).
5. (ⓝ) `RtlEncodePointer` (rva `0xe9610`, 95B): 쿠키를 어디서 얻나 — `NtQueryInformationProcess` 클래스 `0x24` (`ProcessCookie`) 를 부르나, 캐시 전역 (이름·rva) 이 있나, 처음 부를 때만 syscall 인가. (커널 쪽은 sast/45 에 있음)
6. (ⓝ) `RtlpInitializeNonVolatileFlush` (rva `0xfb648`, 144B): 무엇을 쓰나 (전역 이름·값), syscall 을 부르나, 갈래 조건.
7. (덤) `RtlGetCurrentServiceSessionId` (`LdrpInitializationComplete +0x19`): ETW 갈래 조건만 (무엇을 읽어 어느 쪽으로 가나).

### C ntoskrnl 본문·비트 뜻
8. ⓐ `PspAllocateProcess +0x2277 ~ +0x22e1`: PEB `BitField` 비트마다 출처 (지역 플래그 비트 12·13 은 무엇인가 — 어디서 켜나).
9. ⓒ `MmCreatePeb +0x2b8`: 프로세스 `+0x170` (PDB `_EPROCESS` 칸 이름으로) 비트 0 의 뜻.
10. ⓓ `PspAllocateProcess +0x2248 BT EBX,8`: 비트 8 의 **뜻**. (켜는 줄 `+0x21e9 BTS` 와 조건은 sast/50 에 있음 — 뜻만)
11. ⓔ `PspSetupUserProcessAddressSpace +0xfa ~ +0x10f`: 만들기 정보 `+0x10` 비트 4 의 뜻, `PspLocateInPEManifest` (call `+0x106`) 하는 일, test.exe 가 이 길을 타나 (run32 순서 표에 빠짐).
12. ⓖ `PspGetProcessParameterOverrides` (`NtCreateUserProcess +0xab3`) 본문: 만들기 정보·매개변수 사본에 무엇을 쓰나. 커널 전역 `PsDefaultLoaderThreads` 를 읽나 (Q79 와 이음).
13. ⓗ 시작 정보 블록 완화 옵션 24B 셋째 칸 (`+0xb0`, 사진 `0x02002002`) 의 비트 뜻·출처 (`PspApplyMitigationOptions` 쪽), ntdll `LdrpInitializeProcess +0x4b3`·`+0xa09` 지역 사본을 블록 어디서 읽나.
14. ⓙ `PspUpdatePebForAffinityChange` (`+0x234`)·`KeCopyXfdMaskToPeb` (`+0x227`) 본문: 어느 PEB 칸에 무엇을 쓰나 (PEB `+0x138`, 사진 K1 `3`).
15. ⓛ `PspMapSiloSharedDataView` (`+0x263`): silo 없는 보통 프로세스에서 하는 일 (일찍 나가나, 무엇을 쓰나).
16. ⓑ PEB `+0x7b8` `LeapSecondData` 를 읽는 ntdll 함수 (이름·`+0x`), leap second 섹션 (`ExLeapSecondDataSectionPointer`) 크기·내용 (만드는 곳이 정적으로 보이면).

### E 있으면 좋음
17. ⓕ K1 환경 블록 전체의 변수 **이름** 목록 (값 빼고). 사진 로그는 앞 `0x200` 바이트만 있음. 정적으로는 못 볼 것 — K1 덤프 (`ms/dump/03_baggage-snap/K1/memory.dmp`) 를 서버에서 읽을 수 있으면 읽고, 못 하면 "덤프 몫 (Q79 방식)" 이라고만 적고 넘어간다.

## 대상
| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^_?LdrpInitialize$` (`+0x10d ~ +0x13e`), `^LdrInitializePerThreadRng$`, `^LdrpProcessMappedModule$`, `^LdrpInitializeProcess$` | 0 | D (이미 있으면 다시 뽑지 않음) |
| ntdll | `^InitSecurityCookie$`, `^InitSpecialMachineFrames$`, `^__cpu_features_init$`, `^RtlEncodePointer$`, `^RtlpInitializeNonVolatileFlush$`, `^RtlGetCurrentServiceSessionId$` | 1 | B |
| ntdll | `+0x7b8` (`LeapSecondData`) 을 읽는 함수 — `_refs`·검색으로 찾기 | 0 | C ⓑ |
| ntoskrnl | `^PspAllocateProcess$`, `^MmCreatePeb$`, `^PspSetupUserProcessAddressSpace$` | 0 | C ⓐⓒⓓⓔ (이미 있음) |
| ntoskrnl | `^PspLocateInPEManifest$`, `^PspGetProcessParameterOverrides$`, `^PspApplyMitigationOptions$`, `^PspUpdatePebForAffinityChange$`, `^KeCopyXfdMaskToPeb$`, `^PspMapSiloSharedDataView$` | 1 | C |

이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where <모듈> <이름>` 로 확인.

## 이미 아는 것
- `[정적]` sast/12·run17: `_LdrpInitialize` 입구 갈래, `.c` 의 `_RtlpDynamicFunctionTableLock` = `.asm` `RtlpDynamicFunctionTableLock` (Ghidra 겹침 표기). 깃발 둘의 실행 값은 Q43 (동적) 몫.
- `[정적]` sast/13·run18: `LdrpInitializeProcess` 묶음 2 (`+0x158 ~ +0x28d`) = `PebLdr` 채우기 (Length 0x58, Initialized=1, 목록 머리 3개), PEB `+0x18`, NLS 준비, CPU 2개 이상이면 스핀 3곳. `+0x12f RtlEncodePointer(0)` → `RtlpUnhandledExceptionFilter`.
- `[정적]` sast/45: 커널 쪽 `NtQueryInformationProcess` `0x24` 는 `_EPROCESS.Cookie` (`+0x2b8`) 를 처음 0 이면 만들어 `lock cmpxchg` 로 씀.
- `[정적]` sast/50 (run50): `PspAllocateProcess +0x2248 BT EBX,8 / JNC → +0x2364` — 꺼지면 `PsQueryProcessAttributes`·`MmCreatePeb`·`PspSetProcessAffinitySafe` 를 모두 건너뜀. 켜는 줄 `+0x21e9 BTS EBX,8` (`+0x21c1 TEST R13B,BL`·`+0x21e4 TEST BL,2` 둘 다 0 일 때). test.exe 는 켜짐.
- `[정적]` sast/27·run32: `PspSetupUserProcessAddressSpace` `+0x106` → `PspLocateInPEManifest` (구조 `+0x10` 비트 4 일 때), `+0x227 KeCopyXfdMaskToPeb`, `+0x234 PspUpdatePebForAffinityChange`, `+0x263 PspMapSiloSharedDataView` — 본문은 안 봄.
- `[관찰]` run44 사진: PEB `+0x138` K1 = `3`, 시작 정보 블록 `+0xb0` = `0x02002002`.
- PE_emulator 쪽 asm 읽기 (D 의 근거): 위 D 1~3 의 자리. `[정적]` 그쪽 기록 — 우리 `.asm` 으로 다시 확인.

## 범위 밖
- 실행 값 (깃발 실행 값 = Q43, `PsDefaultLoaderThreads` 값 = Q79, 스위치 전역 = Q35) — 동적은 이번에 안 함.
- ⓠ (run58 끝), Q111 (함수표·`.mrdata`, 다음 항목).

## 받고 싶은 것
- 질문별 답 + 근거 (`.asm` `+0x` 줄, CALL/ret 구분).
- 짐 지도용: 새로 "쓴 자리" 를 알게 된 전역·구조체 칸 목록 (baggage 3장 어느 절·어느 줄에 들어갈지, 단계 ①/②, 놓는 법, 경로).
- PE_emulator 용 한 줄 표: 함수 · 쓰는 것 · syscall (있으면 번호 아닌 이름) · 갈래 조건.
- PDB GUID 확인 (ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E`, ntdll `C3093720177DA6851519DCFFE8DF53FE`).
- 동적으로 확인할 것 (있으면 멈춤점 후보와 함께).
