# 2026-10-25 run44 — 짐 지도 사진: 한 번의 실행에서 8 지점 스냅샷 → 덤프 → 칸마다 "처음 값이 생기는 구간". 구조체 직접 칸 1,456 중 1,428 칸과 ntdll 전역 493 중 332 개가 구간 또는 "끝까지 0" 으로 정해짐. `ProcessParameters` 는 ①→①-a 사이에 힙으로 옮겨짐

## 환경

- 26100.9457. 바퀴 하나에 동적 요청 넷 (dast/01 ~ 04), 정적 요청 없음. 03_sprint 둘째 바퀴 (짐 지도 B, plan Q77 = Q77a 시험 · Q77b 캡처 · Q77c 읽기).
- dast/01 · dast/02 는 시험이라 test.exe 실행이 따로다 (주소는 그 실행 기준, 아래 "방법").
- **dast/03 · dast/04 는 같은 한 번의 실행이다.** 아래 주소는 모두 이번 실행 기준.
  - test.exe: EPROCESS `ffff82859908b080`, PID `0x1dd0`, DirBase `192c2e000`
  - 첫 스레드: ETHREAD `ffff828598445080` (Cid `1dd0.26ec`), InitialStack `ffffb58b9ad075f0`, 트랩 프레임 `ffffb58b9ad07460` (= InitialStack − 0x190)
  - 부모 cmd.exe: EPROCESS `ffff828598add080` (Cid `19a8`), 스레드 `ffff828598adc080`
  - PEB `9b2fa66000`, TEB `9b2fa67000`, 맡긴 문맥 `_CONTEXT` `9b2fcffa40`
  - 모듈 베이스: test `7ff61a370000`, ntdll `7ffafb660000`, KERNEL32 `7ffafa630000`, KERNELBASE `7ffaf8920000`, dll1 `7ffae12e0000`, dll2 `7ffadb950000`
- 덤프 8개 모두 `!lmi nt` GUID `C29EBFB0-6B78-B3C0-20DC-A66D99713F9E` (flow.md 기준 ntkrnlmp.pdb 와 같음). ntdll 심볼은 `C3093720177DA6851519DCFFE8DF53FE1`.

## 목적

- 짐 지도 체크리스트 (run43) 의 칸마다, 초기화 동안 **언제 처음 값을 갖는지**를 실제 메모리로 본다.
- 정적으로는 못 보이는 "아무도 안 씀 (0 그대로)" 을 센다.
- 정적 쓴 자리 (run9 ~ run42) 가 실행과 어긋나는 곳을 찾는다.
- 한 번에 커널·유저를 같이 읽는 방법 (C 방식) 을 세운다.

## 방법

비유: 이사하는 집을 **여덟 번 사진 찍고**, 사진끼리 비교해서 "이 가구는 3번째와 4번째 사진 사이에 들어왔다" 를 가린다.

### C 방식과 시행착오

| 단계 | 요청 | 한 일 | 결과 |
|---|---|---|---|
| 시험 1 | dast/01 snapshot-test | ① `LdrInitializeThunk` 입구에서 멈춘 채 호스트 VMware 스냅샷 | 스냅샷은 찍힘 (`Snapshot4.vmem` 2,668,462,080 B). 그러나 서버 `vmss2core -W8` 이 "Unrecognized .vmss file (magic 8943dd9e)" 로 거부 `[관찰]` (plan 2026-10-25) |
| 원인·조치 | — | 타깃 vmx 에 `vmx.encryptionType = "partial"` + keySafe, 파일 머리 `9e dd 43 89` → 암호화된 스냅샷으로 봄 `[추측: 가상 TPM 때문]` | 사용자가 가상 TPM 제거 → "스냅샷이 있으면 암호화 못 푼다" → 백업 (`bob_baseline_vm.bak-20261025/`, 118 GB) 뒤 스냅샷 4개 삭제 → 암호화 해제 |
| 시험 2 | dast/02 snapshot-test | 암호화 푼 타깃에서 같은 ① 스냅샷 → `vmss2core -W8` (25.9초) → `memory.dmp` 8 GB (`PAGEDU64`) → 디버거 VM WinDbg 로 열기 | **분석성공.** EPROCESS·DirBase·`_PEB` 칸·`LdrInitializeThunk` 입구 바이트·`!vtop` 물리 주소가 라이브와 모두 같음 → 커널·유저 둘 다 읽힘 `[관찰]` |
| 남은 문제 1 | dast/02 | VMware 가 **lazy 스냅샷**: 메뉴 끝 18:11:44 → 메모리 쓰기 끝 18:18:52, 디버거로 멈춘 동안 초당 약 1.5 MB | 타깃 `.vmx` 에 `mainMem.partialLazySave = "FALSE"` → 시험 스냅샷 `lazy-test` 에서 `Wrote 8192MiB in 1885ms` `[관찰]` (요청 줄엔 여전히 `lazy=1` 이 찍힘 → `Wrote`/`Done with snapshot` 줄로 확인) |
| 남은 문제 2 | dast/02 | 덤프의 CPU 문맥은 멈춘 지점이 아니라 **디버거 대기 루프** (`kdcom!WaitForRxPacket`, "Unable to get program counter") `[관찰]` | 레지스터는 라이브에서 남기고, 오프라인에선 메모리의 트랩 프레임·`_CONTEXT` 로 읽기로 함 |
| 캡처 | dast/03 baggage-snap | 한 번의 test.exe 실행에서 8 지점에 차례로 멈춰 라이브 기록 + 스냅샷 (`03-K1` … `03-P2` = Snapshot7 ~ 14), 장마다 `Done with snapshot` 확인 뒤 `g` | **분석성공.** 장당 쓰기 3.2 ~ 3.3초. 8개 모두 변환 rc=0 (장당 약 26초), `ms/dump/03_baggage-snap/<P>/memory.dmp` (각 8 GB, 실제 5.0G) |
| 읽기 | dast/04 baggage-read | 덤프마다 같은 명령 파일로 구획 00 ~ 17 (`dt` + raw `dq`) → 지점별 로그 8개. 빠진 셋 (토큰·`PsNtdllExports`·KUSD) 은 보충 명령 파일로 다시 | **분석성공.** 로그 8개 모두 머리 35개 + `99 END`, 보충 8개 모두 머리 4개·에러 0 |

- 라이브 로그: `ms/logs/01_snapshot-test.txt`, `02_snapshot-test.txt` (+ `_offline.txt`), `03_baggage-snap.txt` (+ `_offline.txt`, 끝 HOST NOTES = 서버 쪽 기록)
- 덤프 읽기 로그: `ms/logs/04_baggage-read_<P>.txt` (구획 `==== <P> NN 이름`), 보충 `ms/logs/04_baggage-read_supp_<P>.txt` (구획 `05s` 토큰 · `09s` `PsNtdllExports` · `13s` KUSD 커널 별칭)
- 명령 파일: `ms/logs/04_baggage-read_cmds_<P>.txt`, `04_baggage-read_supp_cmds_<P>.txt`
- 판정: `ms/logs/0[1-4]_*_result.md` (넷 다 **분석성공**)
- 덤프 원본: `ms/dump/03_baggage-snap/` (커밋 안 함)

### 8 지점 (dast/03 요청서)

| 이름 | 짐 지도 기호 | 멈춘 곳 | 이 사진으로 갈리는 것 |
|---|---|---|---|
| K1 | — | `nt!PspAllocateThread` 입구 (부모 cmd 스레드, `NtCreateUserProcess +0xe5c`) | 프로세스 만들기 끝 / 스레드 만들기 전 |
| K2 | — | `nt!PspInsertProcess` 입구 (`+0xfc2`) | 스레드 만들기 끝 / 목록·핸들 표에 넣기 전 |
| K3 | — | `nt!PspCallProcessNotifyRoutines` 입구 (`PspInsertThread +0x807`) | 넣기 끝 / 알림·마무리 |
| P0 | ⓪ | `nt!PspUserThreadStartup` 입구 (test.exe 첫 스레드) | 부모가 한 것 전부 (부모의 csrss 알림·`NtResumeThread` 포함) / 자식 스레드가 커널에서 한 것 |
| P1 | ① | `ntdll!LdrInitializeThunk` 입구 | 커널 몫 끝 / ntdll 시작 |
| P1a | ①-a | `ntdll!LdrpInitializeProcess +0x199d` (call → `LdrpInitializeKernel32Functions`) | ntdll 혼자 한 것 / KERNEL32·KERNELBASE 준비 |
| P1b | ①-b | `ntdll!LdrpInitializeProcess +0x1bb6` (call → `LdrpInitializeGraphRecurse`, BEFORE-GRAPH) | 로더가 깐 것 (dll 올리기·잇기) / dll 시작 함수·TLS 콜백 |
| P2 | ② | `ntdll!NtContinue` 입구 (`LdrInitializeThunk +0x18` 에서 부름) | 초기화 끝 (짐 지도 범위 끝) |

- 걸기: K1 ~ K3 `bp /p <cmd>`, K1 의 `rcx` 에서 test.exe 를 얻어 P1a·P1b `bp /p <test>`, P0 에서 첫 스레드를 얻어 P1 (스레드 조건)·P2 (스레드 + `poi(@rsp) == LdrInitializeThunk+0x18`) (dast/03 판정).

### 지점끼리 비교한 법 (서버 임시 스크립트, 커밋 안 함)

- 로그를 `^==== <P> NN` 으로 자르고 CRLF 를 뗐다. 구획 안 **raw `dq` 줄**을 대상 시작 주소 기준 바이트 배열로 펼쳤다. `????` 는 0 으로 바꾸지 않고 "읽기 불가" 로 뒀다.
- 칸 목록 (`ms/disassemble/{ntoskrnl,ntdll}/_baggage/struct__*.tsv` 의 직접 칸, `globals.tsv`) 의 (오프셋, 크기, 비트) 로 바이트를 잘라 지점마다 값을 냈다. 비트 칸·union 은 칸마다 따로 (같은 바이트라도 이름이 다르면 다른 칸).
  - dast/04 판정 제안은 `dt` 줄을 (오프셋, 이름) 열쇠로 비교하는 것이었는데, `dt` 는 펼친 구조체 칸 (`Pcb : _KPROCESS`, `ProcessLock : _EX_PUSH_LOCK` 등) 의 값을 안 보여 준다. 그래서 **값은 모두 raw 로** 비교하고 `dt` 는 이름·뜻을 읽는 데만 썼다. raw 는 21개 구조체 모두 전체 크기만큼 떠 있다.
  - `_KPROCESS` = `_EPROCESS` raw 앞 0x1c8, `_KTHREAD` = `_ETHREAD` raw 앞 0x4c0.
  - `_SECTION_IMAGE_INFORMATION` = 구획 07d (`_MI_SECTION_IMAGE_INFORMATION`) 앞 0x40.
  - `_PEB_LDR_DATA` = 구획 12 (P1a ~), 그 앞은 구획 17a 안 `PebLdr` (rva `0x1d2880`).
  - 모듈 항목·그래프 노드는 `BaseDllName` 으로 모듈을 맞춰 모듈마다 따로.
  - ntdll 전역은 구획 17a·17b·17c 의 qword 오프셋 + 구역 rva (`.data` `0x1cc000`, `.mrdata` `0x1e7000`, `.00cfg` `0x1e6000`) 로 `globals.tsv` 와 이었다. **전역 크기는 "다음 이름까지 거리" `[추측]`** 이라 이름 없는 이웃 바이트가 섞일 수 있다.
- 지점마다 주소가 다른 대상은 대상 안 오프셋으로 비교했다: `ProcessParameters` (P1 `25996cb0000` → P1a `25996f37a50`).
- "없음" 으로 친 것: **K1 의 ETHREAD·KTHREAD·트랩 프레임** (스레드 생기기 전 메모리, 쓰레기), **K1 의 TEB** (전부 `????`, `MmCreateTeb` 는 `PspAllocateThread +0x631` 안 `[정적]` run11).
- 소음 (구간은 내되 "바뀜" 은 뜻 없음): KUSD 시각 칸 8개 (`InterruptTime`·`SystemTime`·`TickCount`·`TimeUpdateLock`·`Baseline*Qpc` 등), `vertarget` 시각, `!handle` PointerCount, 스레드 실행 통계 칸 (`CycleTime`·`ContextSwitches`·`KernelStack` 등).
- 판정 규칙 — 칸마다 8개 상태 (`없음` / `읽기 불가` / `0` / 값) 에서:
  - `K1 이전` = K1 에서 이미 0 이 아님
  - `A~B` = A 에서 0 (또는 없음), B 에서 처음 0 이 아님 → **A 와 B 사이에 처음 값**
  - `끝까지 0` = 읽힌 지점 모두 0. `*` 를 붙이면 K1 ~ P1 은 읽기 불가였던 것 (ntdll `.data`·`.mrdata` 앞 페이지, `PebLdr`)
  - `≤P1a` = K1 ~ P1 읽기 불가, P1a 에 이미 값 (그 앞 언제인지 못 좁힘)
  - `읽기 불가` = 8장 모두 `????` / `칸 안 읽음` = VAD 노드 (`!vad` 목록만 읽음)
  - 처음 값 뒤에 또 바뀐 구간은 "바뀜" 으로 따로 적었다.
- **구간은 "언제" 만 말한다. "누가" 는 말하지 않는다.** 한 구간 안에 여러 함수·부모 쪽 코드·다른 스레드가 섞인다. 쓴 자리는 Q78.
- "끝까지 0" 은 "아무도 안 씀" 과 같지 않다: 0 을 썼거나, 썼다가 사진 사이에 되돌렸거나 (예: `SpecialApcDisable`), 조건이 안 맞아 안 썼거나를 사진으로는 못 가린다.

## 결과

### 지점별 라이브 관찰 (dast/03)

| 지점 | 문맥 (`!process -1 0`) | 스레드 | 콜스택 (`k 5` 요약) | `rip` | 표시 |
|---|---|---|---|---|---|
| K1 | cmd.exe `19a8` | `19a8.1594` | `PspAllocateThread` ← `NtCreateUserProcess+0xe5c` ← `KiSystemServiceCopyEnd+0x28` ← `ntdll!NtCreateUserProcess+0x14` ← `KERNELBASE!CreateProcessInternalW+0x30ee` | `fffff805b344296c` | [26100.9457 확인] |
| K2 | cmd.exe | `19a8.1594` | `PspInsertProcess` ← `NtCreateUserProcess+0xfc2` ← … | `fffff805b344480c` | [26100.9457 확인] |
| K3 | cmd.exe | `19a8.1594` | `PspCallProcessNotifyRoutines` ← `PspInsertThread+0x807` ← `NtCreateUserProcess+0x101a` ← … | `fffff805b35b1ca8` | [26100.9457 확인] |
| P0 | test.exe `1dd0` | `1dd0.26ec` | `PspUserThreadStartup` ← `KiStartUserThread+0x28` ← `KiStartUserThreadReturn` ← (유저 칸) `ntdll!RtlUserThreadStart` | `fffff805b3510de0` | [26100.9457 확인] |
| P1 | test.exe | `1dd0.26ec` | `LdrInitializeThunk` 혼자 | `7ffafb741420` (`rcx` = `9b2fcffa40`) | [26100.9457 확인] |
| P1a | test.exe | `1dd0.26ec` | `LdrpInitializeProcess+0x199d` ← `LdrpInitialize+0x16c` ← `LdrpInitializeInternal+0x5a` ← `LdrInitializeThunk+0xe` | `7ffafb6edda1` | [26100.9457 확인] |
| P1b | test.exe | `1dd0.26ec` | `LdrpInitializeProcess+0x1bb6` ← (위와 같음) | `7ffafb6edfba` | [26100.9457 확인] |
| P2 | test.exe | `1dd0.26ec` | `NtContinue` ← `LdrInitializeThunk+0x18` | `7ffafb7c1610` (`rcx` = `9b2fcffa40`) | [26100.9457 확인] |

- 콜스택 표시 이름 `LdrpInitialize+0x16c` 의 주인은 PDB 이름 `_LdrpInitialize` (flow.md 기준, run17).
- `KTHREAD.TrapFrame`: P0·P1 `(null)`, P1a·P1b `ffffb58b9ad07460` (= InitialStack − 0x190) `[관찰]` (dast/03).
- 덤프 쪽 확인: K3 ~ P2 에서 `!process 0 0 test.exe` 가 라이브와 같은 EPROCESS·DirBase, `HandleCount` 4·4·4·26·44·46 (K3·P0·P1·P1a·P1b·P2) 도 라이브와 같다 → 덤프마다 그 지점의 순간이다 `[관찰]`.
- K1·K2 덤프에선 `!process 0 0 test.exe` 가 빈칸이다. K2 에서 주소로 읽으니 test.exe 이고 `ActiveProcessLinks [0 - 0]` → **아직 프로세스 목록에 안 넣음** `[관찰]` (dast/04).

### 지점별 큰 숫자 (dast/04 덤프)

| | K1 | K2 | K3 | P0 | P1 | P1a | P1b | P2 |
|---|---|---|---|---|---|---|---|---|
| 핸들 수 | 4 | 4 | 4 | 4 | 4 | 26 | 44 | 46 |
| VAD 수 | 8 | 9 | 9 | 11 | 11 | 21 | 32 | 32 |
| 모듈 목록 | — | — | — | — | — | 2 (test·ntdll) | 6 | 6 |
| `ActiveThreads` | 0 | 0 | 1 | 1 | 1 | 1 | 3 | 3 |
| `PEB.ProcessParameters` | `25996cb0000` | ← | ← | ← | ← | `25996f37a50` | ← | ← |
| `PEB.Ldr` | null | null | null | null | null | `7ffafb832880` (= ntdll+`0x1d2880` `PebLdr`) | ← | ← |
| `PEB.ProcessHeap` | null | … | | | null | `25996f30000` | ← | ← |
| `HANDLE_TABLE.UniqueProcessId` | `0x19a8` (cmd) | `0x19a8` | `0x1dd0` | ← | ← | ← | ← | ← |
| ntdll `.data`·`.mrdata` 앞 3페이지 | 읽기 불가 | ← | ← | ← | ← | 읽힘 | 읽힘 | 읽힘 |

- K1 의 VAD 8개: KUSD 두 페이지, PEB 가 든 private 자리 (`9b2fa00` ~ `9b2fbff`, 나중 TEB 도 여기), 매개변수 블록 (`25996cb0` ~ `25996ccf`, private 32쪽), API set 지도 (`25996cd0` ~), 한 쪽짜리 매핑 (`7ff5ef0a0`, `PEB.LeapSecondData` 값과 같은 주소), test.exe, ntdll `[관찰]`.
- K1~K2 에 유저 스택 자리 (`9b2fc00` ~ `9b2fcff`) 가, K3~P0 에 2개 (`25996d00` 매핑 = `PEB.SystemDefaultActivationContextData` 값, `25996d10` private = `PEB.pShimData` 값) 가 늘어난다 `[관찰]`.
- P1a 에서 `25996cb0` 자리는 **private 32쪽이 사라지고 2쪽짜리 매핑 섹션**이 됐다. 그 주소가 TEB `SchedulerSharedDataSlot` 값 (`25996cb0000`) 과 같다 `[관찰]`. 프로세스 힙 (`25996f30` ~) 이 P1a 에 처음 보인다.
- 토큰은 8개 모두 같은 `ffffb689ae6b6060` (`EPROCESS.Token` 아래 4비트만 바뀜 — `_EX_FAST_REF` 셈 `[추측]`).

### 칸마다 처음 값 구간 — 요약

구간 열의 숫자 = 그 구간에 처음 값이 생긴 칸 수. 모듈 항목·노드는 6개 모듈 중 **가장 이른** 구간 (exe·ntdll 은 거의 `P1~P1a`, 나머지 넷은 `P1a~P1b`), "끝까지 0" 은 6개 모두 0 일 때만.

| 구조체·구역 | 칸 수 | K1 이전 | K1~K2 | K2~K3 | K3~P0 | P0~P1 | P1~P1a | P1a~P1b | P1b~P2 | ≤P1a | 끝까지 0 | 못 읽음 | 바뀜 있음 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `_PEB` | 124 | 23 | · | · | 2 | · | 13 | 5 | · | · | 81 | · | 4 |
| `_RTL_USER_PROCESS_PARAMETERS` | 40 | 20 | · | · | · | · | 1 | · | · | · | 19 | · | 8 |
| `_TEB` | 137 | · | 9 | · | · | 6 | 7 | 1 | 2 | · | 112 | · | 4 |
| `_PEB_LDR_DATA` | 9 | · | · | · | · | · | · | · | · | 5 | 4 | · | 3 |
| `_LDR_DATA_TABLE_ENTRY` | 65 | · | · | · | · | · | 33 | 7 | · | · | 25 | · | 8 |
| `_LDR_DDAG_NODE` | 10 | · | · | · | · | · | 3 | 4 | · | · | 3 | · | 1 |
| `_KUSER_SHARED_DATA` | 103 | 62 | · | · | · | · | · | · | · | · | 41 | · | 8 |
| **유저 구조체 합** | **488** | **105** | **9** | **0** | **2** | **6** | **57** | **17** | **2** | **5** | **285** | **0** | **36** |
| `_EPROCESS` | 263 | 62 | · | 7 | 12 | 9 | 5 | 1 | · | · | 167 | · | 26 |
| `_KPROCESS` | 65 | 15 | · | 2 | 2 | · | · | · | · | · | 46 | · | 3 |
| `_ETHREAD` | 149 | · | 31 | 6 | 4 | · | 1 | · | · | · | 107 | · | 11 |
| `_KTHREAD` | 237 | · | 49 | 11 | 9 | 9 | 7 | 6 | 6 | · | 140 | · | 44 |
| `_KTRAP_FRAME` | 66 | · | 9 | · | · | · | 24 | 1 | · | · | 32 | · | 25 |
| `_HANDLE_TABLE` | 17 | 10 | · | · | · | · | · | · | · | · | 7 | · | 3 |
| `_MMVAD` | 9 | · | · | · | · | · | · | · | · | · | · | 9 | 0 |
| `_MMVAD_SHORT` | 14 | · | · | · | · | · | · | · | · | · | · | 14 | 0 |
| `_TOKEN` | 55 | 29 | · | · | · | · | · | · | · | · | 26 | · | 0 |
| `_SECTION` | 9 | 5 | · | · | · | · | · | · | · | · | 4 | · | 0 |
| `_CONTROL_AREA` | 17 | 12 | · | · | · | · | · | · | · | · | 5 | · | 2 |
| `_SEGMENT` | 11 | 9 | · | · | · | · | · | · | · | · | 2 | · | 0 |
| `_SECTION_IMAGE_INFORMATION` | 26 | 15 | · | · | · | · | · | · | · | · | 11 | · | 0 |
| `_FILE_OBJECT` | 30 | 15 | · | · | · | · | · | · | · | · | 15 | · | 2 |
| **커널 구조체 합** | **968** | **172** | **89** | **26** | **27** | **18** | **37** | **8** | **6** | **0** | **562** | **23** | **116** |
| ntdll `.data` 전역 | 438 | · | · | · | · | · | · | 24 | 6 | 143 | 265 | · | 34 |
| ntdll `.mrdata` 전역 | 50 | 5 | · | · | · | · | · | · | · | 13 | 32 | · | 5 |
| ntdll `.00cfg` 전역 | 5 | · | · | · | · | · | · | · | · | · | · | 5 | 0 |

- 구조체 21개 1,456칸: 구간이 정해진 칸 581 + 끝까지 0 847 = **1,428 칸이 사진으로 정해짐**. 못 좁힌 것 28 (`≤P1a` 5 · VAD 칸 안 읽음 23).
- ntdll 전역 493: 구간 35 + 끝까지 0 297 = **332 개가 정해짐**. 못 좁힌 것 161 (`≤P1a` 156 · `.00cfg` 읽기 불가 5).
- `_KTRAP_FRAME` 의 `P1~P1a` 24칸은 짐이 아니다. P1a 부터 이 자리에 **시스템 콜·int3 의 트랩 프레임**이 덮인다 (아래 "디버거 영향").
- `_KTHREAD` 의 P0 이후 구간 칸 다수는 스케줄러 통계·기다림 칸 (`CycleTime`·`ContextSwitches`·`WaitBlock*`·`WaitReason` 등) 이다. 멈춰 있던 시간이 섞인다 `[추측]`.
- 칸마다 구간 전체는 짐 지도 `runs/03_sprint/baggage.md` 3장 (아는 칸, 근거 칸 끝 "사진") 과 7.2·7.3 (모르는 칸, 구간별로 묶음).

### 구간마다 무엇이 생기나 (대표 칸만)

- **K1 이전** (`PspAllocateProcess` 까지 + 섹션 만들기): EPROCESS 62칸 (`UniqueProcessId` `0x1dd0`, `Peb`, `SectionBaseAddress` `7ff61a370000`, `ObjectTable`, `Token` …), 섹션 사슬·파일 객체·토큰 칸 거의 전부, PEB 23칸 (`ImageBaseAddress` = `SectionBaseAddress`, `ImageSubsystem` 3, `SessionId` 1, `LeapSecondData` `7ff5ef0a0000`, `ApiSetMap` `25996cd0000`, `ProcessParameters` `25996cb0000` …), 매개변수 20칸, ntdll `.mrdata` 끝 페이지 5개 (`LdrSystemDllInitBlock`, `__security_cookie` 파일 값 `0x2b992ddfa232` 등). `ProcessLock` = 1 (잡힌 채).
- **K1~K2** (`PspAllocateThread`): ETHREAD 31 · KTHREAD 49 · 트랩 프레임 9 · TEB 9 (`NtTib`·`ClientId`·`ProcessEnvironmentBlock`·`RealClientId`·`StaticUnicodeString`·`DeallocationStack` `9b2fc00000`·`SameTebFlags.InitialThread` …). `StartAddress` = `RtlUserThreadStart` (`7ffafb70cac0`), `Win32StartAddress` = `7ff61a371d40` (test+`0x1d40`), `Cid` `0x26ec`, `KTHREAD.Teb` = TEB 주소, 트랩 프레임 `Rip`/`Rcx`/`Rdx`/`Rsp` 처음 값·`SegCs 0x33`·`SegSs 0x2b`·`EFlags 0x200`·`MxCsr 0x1f80`.
- **K2~K3** (`PspInsertProcess` 전부 + `PspInsertThread +0x807` 까지): `ActiveProcessLinks`·`SequenceNumber 0x134`·`Flags.ProcessInserted`, `HANDLE_TABLE.UniqueProcessId` `0x19a8` → `0x1dd0`, `ActiveThreads` 1·`ThreadWasActive`·`ThreadListHead`, `ProcessLock` 1 → 0, `ThreadLock` 1 → 0, `ThreadInserted`, `SuspendCount` 0 → 1, KTHREAD `Priority`/`BasePriority` 8·`QuantumReset` 2·`IdealProcessor`.
- **K3~P0** (알림 뒤 부모 마무리 + 부모의 csrss 알림·`NtResumeThread` + 스레드가 잠들었다 깸): `ProcessFirstResume` 0 → 1, `SuspendCount` 1 → 0, PEB `pShimData` `25996d10000`·`SystemDefaultActivationContextData` `25996d00000`, EPROCESS `ExceptionPortData`·`Flags3` 비트 몇 개 (`EnableProcessSuspendResumeLogging` 등)·`WakeChannel`·`BamPpmPolicy` 7, KTHREAD `State` 2·`Running`.
- **P0~P1** (`PspUserThreadStartup` → `PspInitializeThunkContext`): TEB `CurrentLocale` `0x412`·`IdealProcessor`·`PrimaryGroupAffinity`, 맡긴 `_CONTEXT` (`9b2fcffa40`, 이 구간 전엔 읽기 불가), 트랩 프레임 `Rip` → `LdrInitializeThunk`, `Rcx` → `_CONTEXT` 주소, `Rdx` → ntdll 베이스, `Rsp` → `9b2fcffa18`. EPROCESS `CreateReported`·`ImageNotifyDone`·`ImagePathHash`·`PrimaryTokenFrozen`.
- **P1~P1a** (ntdll 혼자: `LdrpInitialize` ~ `LdrpInitializeProcess +0x199d`): PEB `Ldr`·`ProcessHeap`·`FastPebLock`·`LoaderLock`·`TlsBitmap`·`ProcessInitializing`·`NumberOfHeaps`·`ProcessHeaps`, **`ProcessParameters` 주소 바뀜**, TEB `RanProcessInit`·`SchedulerSharedDataSlot`·`ThreadLocalStoragePointer`·`ActivationContextStackPointer`, exe·ntdll 모듈 항목·노드, `__security_cookie` 새 값 (`0x461f7adbe3f8`), EPROCESS `Cookie`·`DeviceMap`·`InvertedFunctionTable`·`WnfContext`, KTHREAD `SchedulerSharedDataObject`.
- **P1a~P1b** (kernel32·kernelbase 준비, 병렬 로드 켜기, dll1·dll2 올리기·잇기): KERNEL32·KERNELBASE·dll1·dll2 항목·노드, `Kernel32ThreadInitThunkFunction` = `7ffafa65cd70` (= KERNEL32+`0x2cd70`), `LdrpMapAndSnapWork`·`LdrpThreadPool`, `ActiveThreads` 1 → 3, ntdll `Csr*` 전역 8개 (`CsrPortHandle` `0x74` = ALPC 포트, `CsrProcessId` `0x2b4` …), PEB `ReadOnlySharedMemoryBase`·`ReadOnlyStaticServerData`·`CSDVersion`·`CsrServerReadOnlySharedMemoryBase`, 매개변수 `ConsoleHandle` 4 → `0x60`, **`OwnerProcessId` `0x19a8` → `0x729`**, `LdrInitState` 0 → 2, `LdrpProcessInitContextRecord` = `_CONTEXT` 주소, TEB `LoadOwner` 1, KERNEL32·KERNELBASE 항목 `ProcessAttachCalled`.
- **P1b~P2** (그래프 켜기 — dll2·dll1 TLS·`DllMain`, exe TLS, `_LdrpInitialize` 뒤쪽): dll1·dll2 `ProcessAttachCalled`·초기화 순서 목록, 노드 `State` 7 → 9, `LdrInitState` 2 → 3, `LdrpProcessInitialized` 1 → 2, `PEB.ProcessInitializing` 1 → 0, `LdrpProcessInitContextRecord` → 0, `LdrpNtdllHotPatchContext` → 0, TEB `FlsData`·`LastStatusValue` `0xc0000034`, ntdll `x86_cpu_enable_*`·`g_SymCrypt*` (CPU 기능 칸, 처음 값).

### 정적 쓴 자리와 맞춰 보기

- 3장 아는 칸 줄 243개를 칸 단위로 맞췄다. **단계 (①/②) 와 구간이 어긋나는 줄은 없었다.** 어긋나 보이는 것은 모두 "그 줄 뒤에 다른 누가 또 바꿈" 이거나 "조건이 안 맞아 0" 이다.
- 정적 줄이 그대로 보인 예 `[관찰]` + `[정적]`:
  - `ProcessLock` (`PspAllocateProcess +0x26e9` 잡음 → `PspInsertThread +0x2a2` 풀기) = K1·K2 1, K3 0 → run12 의 `[정적 + 간접]` "호출자가 잡아 들고 들어옴" 이 실제로 맞다.
  - `ThreadLock` K2 1 → K3 0, `SuspendCount` K3 1 → P0 0, `ProcessFirstResume` K3 0 → P0 1, `SuspendEvent` 바뀜 `K2~K3`·`K3~P0` (run13).
  - 트랩 프레임 처음 값 = `KxContextToKframes` (K1~K2), 바꿔 끼우기 = `PspSetContext` (P0~P1) (run16). `Rsp` 새 값 `9b2fcffa18` = `_CONTEXT` 주소 − 0x28.
  - `ProcessInitializing` P1a·P1b 1, P2 0 (`_LdrpInitialize +0x107`/`+0x20f`), `LdrInitState` P1a 0 → P1b 2 → P2 3 (`+0x1a4c`·`+0x1b1e` 는 P1a 와 P1b 사이, `+0x205` 는 P1b 뒤), 노드 `State` exe P1a 9 → P1b 7 → P2 9 (`+0x1508`, `+0x1a60` 뒤 7 = `LdrpNotifyLoadOfGraph`), `LdrpProcessInitContextRecord` `+0x1b83` → `+0x1bec`.
  - 모듈 항목 `LoadReason`: exe 4 (run22 짐작 "부모 칸이 0 이라 4" 와 같은 값), KERNEL32 4, KERNELBASE·dll1·dll2 0 (run23 "인자 4 = 0" 과 같음).
- 조건이 안 맞아 0 인 것 (정적 줄은 있지만 사진엔 0): `Flags3` 비트 7개, `LdrpIsSecureProcess`, `UseWOW64`, `LdrpIsHotPatchingEnabled`, `LdrpImageExpansionMitigation`, `LdrpLargePageDllKeyHandle`, `LdrpAdvapi32DllHandle`, `LdrpSaferIsDllAllowedRoutine`, `LdrpDetourExist`, `AvrfAppVerifierMode`, `g_pfnSE_InstallAfterInit`, `g_LdrpWow64LdrpInitialize`, `HotPatchState`.
- 썼다 되돌려서 0 으로 보이는 것: `SpecialApcDisable`, `SuspendSchedulerApcWait`, `LdrpMainThreadToken`, `LdrpTlsLock`, `LdrpCurrentDllInitializer`.
- 값 자체가 0 일 것 (헤더·버전 값 `[추측]`): `_SECTION_IMAGE_INFORMATION` `SubSystemMinorVersion`·`MinorOperatingSystemVersion`·`LoaderFlags`·`CheckSum`, PEB `OSMinorVersion`·`OSCSDVersion`·`ImageSubsystemMinorVersion`·`NtGlobalFlag`.

### 새로 확인된 것 (정적 문서에 없던 것)

1. **`PEB.ProcessParameters` 가 P1 → P1a 사이에 다른 주소로 바뀐다** (`25996cb0000` → `25996f37a50`, 새 주소는 프로세스 힙 `25996f30000` 안) `[관찰]`.
   - 같은 구간에 옛 블록 자리 (private 32쪽) 가 VAD 에서 사라진다 `[관찰]`.
   - 새 블록은 길이·`Flags`·핸들·문자열 길이가 옛 블록과 같고, 안쪽 포인터 (`CurrentDirectory`·`ImagePathName`·`CommandLine`·`Environment`·`WindowTitle`·`DesktopInfo`·`ShellInfo`) 만 새 자리 기준이다. `EnvironmentVersion` 만 0 → 3 `[관찰]`.
   - 누가 옮겼는지는 모른다 `[공백]`. 정적 문서엔 이 구간에 블록을 옮기는 줄이 없다 (run18 의 `+0x6c5 RtlNormalizeProcessParams` 는 하는 일 모름).
2. **`HANDLE_TABLE.UniqueProcessId` 는 K1·K2 에서 부모 PID `0x19a8`, K3 부터 `0x1dd0`** `[관찰]`. 쓰는 줄은 `[공백]` (`PspInsertProcess` 안이거나 `PspInsertThread +0x807` 앞).
3. **`EPROCESS.OwnerProcessId` 가 P1a~P1b 에 `0x19a8` → `0x729`** `[관찰]`. 정적으로 아는 쓰는 곳은 `PspAllocateProcess +0x710` 하나뿐 (run33). Q65 에 남은 `NtSetInformationProcess +0x1ecd` 다른 쓰기와 이어질 수 있다 `[추측]`.
4. **매개변수 `ConsoleHandle` 이 P1a~P1b 에 4 (`\Reference {ConDrv}`) → `0x60` (`\Connect {ConDrv}`)** `[관찰]` (P2 `!handle`). 쓰는 쪽 `[공백]` (KERNELBASE 콘솔 준비 `[추측]`).
5. **부모 쪽이 ⓪ 전에 자식 PEB 에 쓰는 칸**: `pShimData`, `SystemDefaultActivationContextData` (K3~P0) `[관찰]`. 쓴 쪽은 부모 KERNELBASE·csrss `[추측]` (2.2 의 csrss 알림 무렵).
6. **`ProcessParameters.LoaderThreads` = 0 (K1 ~ P2)** `[관찰]`. run32 `[정적]` "0 이면 `PsDefaultLoaderThreads` 로 채움" 이 있으니 그 전역도 0 이었다 `[추측]` (전역은 안 읽음). IFEO 로도 안 바뀜. 그리고 `LdrpMapAndSnapWork` 가 P1b 에 0 이 아니고, `ActiveThreads` 가 P1a~P1b 에 1 → 3 → run24 `[정적]` "0 이면 스레드 수 4 → 일감 개체 생김" 과 run5 `[관찰]` "ntdll 일꾼 2개" 가 같은 실행에서 맞아떨어진다.
7. **KERNEL32·KERNELBASE 의 `ProcessAttachCalled` 는 P1a~P1b, dll1·dll2 는 P1b~P2** `[관찰]` — kernel32 준비 안에서 둘의 시작 함수가 불린다는 flow 2.5 그림 (`LdrpCallInitRoutine → ? 시작 함수`) 과 시점이 맞다.
8. **커널 디버거만 붙었을 때 `PEB.BeingDebugged` = 0 (K1 ~ P2)** `[관찰]`.
9. 값 확인 `[관찰]`: `PsNtdllExports` `+0x0` `LdrSystemDllInitBlock` = ntdll+`0x1ea450`, `+0x8` `LdrInitializeThunk` = ntdll+`0xe1420`, `+0x10` `RtlUserThreadStart` = ntdll+`0xacac0` (`_PS_NTDLL_EXPORTS` 21칸, 8개 덤프 모두). `Kernel32ThreadInitThunkFunction` = KERNEL32+`0x2cd70` (run25 `[정적]` `BaseThreadInitThunk` rva 와 같음). 이벤트 전역 `LdrpInitCompleteEvent` `0xc` · `LdrpLoadCompleteEvent` `0x50` · `LdrpWorkCompleteEvent` `0x54` = Event, `LdrpKnownDllDirectoryHandle` `0x4c` = Directory `KnownDlls` (P2 `!handle`). KUSD 커널 별칭 `fffff78000000000` 과 유저 `0x7ffe0000` 은 P1a 에서 0xa80 바이트가 모두 같다.
10. ntdll 쓰기 가능 구역은 **K1 ~ P1 에서 `.data` 전부와 `.mrdata` 앞 3페이지가 읽기 불가**, `.mrdata` 끝 페이지 (`0x1ea000` ~, `LdrSystemDllInitBlock`·`__security_cookie` 가 있는 곳) 만 K1 부터 읽힌다 `[관찰]`. 커널이 K1 전에 그 페이지에 써서 (run32 `PspPrepareSystemDllInitBlock`) 프로세스 몫 페이지가 생겼고, 나머지는 아직 아무도 안 건드려 이 프로세스 페이지가 없었다 `[추측]`. `.00cfg` 는 8장 모두 읽기 불가.

## 확인된 것

- `[관찰]` (P0 트랩 프레임, P1 `_CONTEXT`) **⓪ 입구의 트랩 프레임 (`InitialStack − 0x190`) = 원래 출발 문맥** — `Rip` `RtlUserThreadStart` (`7ffafb70cac0`), `Rcx` 진입점 `7ff61a371d40`, `Rdx` PEB, `Rsp` `9b2fcfff48`, `EFlags 0x200`. 이 넷이 **① 의 `_CONTEXT` 와 같다** (`ContextFlags 0x10001b`). run16 `[정적]` "원래 문맥을 유저 스택에 맡김" 이 실행 값으로 맞다.
- `[관찰]` ② 의 `_CONTEXT` 는 ① 과 같은 주소 (`9b2fcffa40`), 0x4d0 바이트가 모두 같다. P2 라이브 `rcx` 도 같은 주소.
- `[관찰]` ② 콜스택 `NtContinue` ← `LdrInitializeThunk+0x18` + `[정적]` run16 (call `+0x13` / ret `+0x18`) → flow.md 그림 2 의 `ntdll!NtContinue` 상자 (`q1`) 를 점선 `(정적)` 에서 실선으로 바꿨다. `SYSCALL` 뒤 커널 쪽은 그대로 `(정적)`.
- `[관찰]` ⓪·① 에서 `KTHREAD.TrapFrame` 은 null 이다. 트랩 프레임은 `InitialStack − 0x190` 으로 찾아야 한다. 시스템 콜을 한 뒤 (P1a ~) 에는 그 칸이 같은 자리를 가리킨다.
- `[관찰]` `PEB.Ldr`·`ProcessHeap`·`FastPebLock`·`LoaderLock`·`TlsBitmap` 은 P1~P1a, `ActiveProcessLinks`·`SequenceNumber`·`HANDLE_TABLE.UniqueProcessId` 는 K2~K3.
- `[관찰]` test.exe 첫 스레드는 `PspAllocateThread` 보통 길 (유저 스택·TEB 를 새로 만듦) 을 탔다: K2 에 TEB·`DeallocationStack`·트랩 프레임이 생겨 있다 (run11 `[정적]` 갈림길 중 Pico·본뜨기 길은 TEB 를 새로 안 만듦).
- `[관찰]` `KTHREAD.Teb` = TEB 주소 (run11 `[추측]` 칸 이름이 값으로 맞음).
- `[관찰]` 디버거로 멈춘 채 스냅샷 → `vmss2core` → WinDbg 오프라인으로 커널·유저 메모리를 같은 순간으로 읽을 수 있다 (dast/02 대조, dast/03 `HandleCount` 대조).

## 정적과 동적이 다른 곳

- 다른 것은 없다. 정적 문서에 **없던** 일이 보인 것뿐이다 (위 "새로 확인된 것" 1 ~ 5). 이것들은 정적이 틀린 게 아니라 아직 안 찾은 쓴 자리다 → Q78.

## 디버거 영향

- **멈춤점 지점의 트랩 프레임 `Rip` = 멈춘 주소 + 1.** P1 `7ffafb741421`, P1a `7ffafb6edda2`, P1b `7ffafb6edfbb`, P2 `7ffafb7c1611` `[관찰]`. 멈춤점 `int3` 이 만든 트랩 프레임이 `InitialStack − 0x190` 자리에 덮인 것이다. 그래서 **P1 ~ P2 의 트랩 프레임 칸은 짐이 아니라 디버거가 만든 값**이다 (P1a 이후 시스템 콜 트랩 프레임도 같은 자리). 짐으로 쓸 수 있는 트랩 프레임 값은 K2·K3·P0 (처음 값) 과 P1 의 `Rcx`·`Rdx`·`Rsp` (int3 직전 = `IRETQ` 로 받은 값) 뿐이다.
- 멈춘 동안 코드 바이트는 원래 값이다 (`LdrInitializeThunk` 입구 `40 53 48 83 …`, dast/01·02 `db`). 덤프 안 코드도 원래 바이트.
- 덤프의 CPU 문맥은 디버거 대기 루프다 → 레지스터는 라이브 기록 (dast/03 `r`) 으로만.
- 스레드 통계·기다림 칸 (`CycleTime`·`ContextSwitches`·`WaitBlock`·`WaitTime` 등) 과 KUSD 시각 칸에는 멈춰 있던 시간이 섞인다 `[추측]`.
- P1 · P2 멈춤점은 스레드 조건이 붙어 있어서, 다른 스레드 (일꾼) 가 같은 자리를 지나면 멈췄다가 바로 갔을 수 있다 `[추측]` — 시간만 조금 바뀐다.
- `.process /p /r` 뒤 "PEB is paged out (Peb.Ldr = …)" 은 `Ldr` 가 null 일 때 나오는 문구일 뿐, PEB 는 읽혔다 (dast/02 HOST NOTES).

## 아직 확인 필요

- **Q78 정적 쓴 자리 후보 — 구간이 정해진 모르는 칸** (출처: 이 문서 "구간마다", 짐 지도 7.2·7.3·7.8). 구간 안 함수만 뒤지면 된다:
  - K1~K2 TEB 8칸 (`ClientId`·`ProcessEnvironmentBlock`·`TxFsContext`·`RealClientId`·`StaticUnicodeString`·`DeallocationStack`·`SameTebFlags`·`InitialThread`) → `MmCreateTeb` 대입 약 47줄 (run43) 과 맞추기.
  - K1~K2 ETHREAD 31 · KTHREAD 49 → `PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread`.
  - K2~K3 `HANDLE_TABLE.UniqueProcessId`, EPROCESS `Flags.ProcessInserted`, KPROCESS `ProcessListEntry`·`StackCount`, KTHREAD 우선순위 칸 → `PspInsertProcess`·`PspInsertThread` (`KeStartThread` 본문 포함).
  - K3~P0 PEB `pShimData`·`SystemDefaultActivationContextData`, EPROCESS `ExceptionPortData`·`Flags3` 비트·`WakeChannel`·`BamPpmPolicy` → 부모 쪽 (`CreateProcessInternalW` 뒤쪽, csrss) 과 `PspCallProcessNotifyRoutines` 이후.
  - P0~P1 TEB `IdealProcessor`·`PrimaryGroupAffinity`, EPROCESS `CreateReported`·`ImageNotifyDone`·`ImagePathHash` → `PspUserThreadStartup` 정상 길 4개 (`KeUpdateTebSchedulingPropertiesCurrentThread`, `PspNotifyThreadCreation` 등, 본문 안 봄).
  - P1~P1a·P1a~P1b·P1b~P2 의 ntdll 모르는 전역 (`.data` 24+6, `≤P1a` 143) 과 PEB·TEB 칸.
- **`ProcessParameters` 를 누가 옮기나** (P1~P1a): `LdrpInitializeProcess` 앞쪽 (`+0x6c5 RtlNormalizeProcessParams` 근처 `[추측]`) 에서 힙에 복사하고 옛 영역을 푸는 줄. `EnvironmentVersion` 3 도 같이.
- **`OwnerProcessId` `0x729`·`ConsoleHandle` `0x60` 을 누가 쓰나** (P1a~P1b): KERNELBASE 콘솔 준비 → `NtSetInformationProcess` 쪽 `[추측]` (Q65 와 겹침, 사용자와 정할 것).
- **`HANDLE_TABLE.UniqueProcessId` 를 누가 바꾸나** (K2~K3).
- **K1~P1 에 못 읽은 것을 좁히기**: ntdll `.data` 전역 (`≤P1a` 143 + `.mrdata` 13) 과 `PebLdr` 는 P1 에 값이 있었는지 모른다. 방법 하나: ntdll 파일의 `.data` 처음 값 (파일 0x2000 + 나머지 0, run17) 과 P1a 값을 비교 (정적). 둘: 다음 사진에서 K1~P1 지점에 `!pte`/물리 주소로 읽기 (동적).
- **`.00cfg` 5칸 읽기 불가** (8장 모두): 다른 읽는 법 (물리 주소 `!db`) 이 필요하다.
- **VAD 노드 칸** (`_MMVAD`·`_MMVAD_SHORT` 23칸): 이번엔 `!vad` 목록만 읽었다. 노드 `dt` 를 덤프 8개에서 다시 읽으면 칸 단위로 된다 (덤프는 남아 있음).
- **`PsDefaultLoaderThreads` 값**: `LoaderThreads` 0 이 커널 기본값 0 때문인지 (`dd nt!PsDefaultLoaderThreads`, 덤프 하나로 충분).
- `KTHREAD`·`KTRAP_FRAME` 의 P1a 이후 칸은 디버거·시스템 콜 값이라, 짐으로 쓰려면 멈춤점 없는 순간 (예: 시스템 콜 안 지점) 이 필요하다 — 이번 범위에선 안 함.

## 디버거 메모 / 도구 메모

- windbg-kd-runbook pitfalls 에 추가 제안 (오케스트레이터가 plan 하네스 후보 (57) ~ (73) 으로 이미 모은 것과 겹치는 건 빼고):
  - 사진 비교는 **raw `dq` 를 기준**으로 할 것. `dt` 는 펼친 구조체 칸 (`_KPROCESS`, `_EX_PUSH_LOCK`, `_LIST_ENTRY` 안쪽 등) 의 값을 줄에 안 보여 줘서 칸 비교 열쇠로는 모자란다.
  - 덤프 읽기 명령에 **VAD 노드 `dt`** 와 **`PsDefaultLoaderThreads` 같은 커널 전역** 을 넣을 것 (이번에 빠져서 23칸이 "칸 안 읽음").
  - 유저 멈춤점 지점의 트랩 프레임은 `int3` 것 (`Rip` + 1) — 짐 값으로 쓰지 말 것 (하네스 후보 (66) 보완).
- 비교 스크립트는 서버 임시 폴더에만 두었다 (커밋 안 함). 다시 하려면 이 문서 "지점끼리 비교한 법" 대로 짜면 된다.
