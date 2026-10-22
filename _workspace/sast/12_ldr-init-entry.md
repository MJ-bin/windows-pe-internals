# 12 ldr-init-entry — 정적 분석 요청

- 날짜: 2026-10-22
- 스프린트: 02_sprint
- 대기열: **Q8** — 2.4 `ntdll!LdrInitializeThunk`, `LdrpInitializeInternal`, `LdrpInitialize` callee (`_workspace/plan.md`). **단계 2(ntdll 지도)의 첫 항목.**
- 관련: `runs/02_sprint/flow.md` 2.4 (이름 정리는 Q8 로 미룸, run16 "`RDX` 를 본체에서 어떻게 쓰나는 Q8"), sast/02 결과 (이름 사슬, `_LdrpInitialize +0x9a` 진짜 간접 호출), run16 (커널이 넘기는 `Rcx`·`Rdx`)

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

| 함수 | rva | 크기 | 깊이 | caller 수 | 상태 |
|---|---|---|---|---|---|
| `LdrInitializeThunk` | 0xe1420 | 32 | 0 | 0 | expanded |
| `LdrpInitialize` | 0xe1448 | 99 | 1 | 1 | expanded |
| `LdrpInitializeInternal` | 0x8be88 | 251 | 2 | 1 | expanded |
| `_LdrpInitialize` | 0x8bf8c | 883 | 3 | 1 | expanded |
| `LdrpInitializeThread` / `LdrpInitializeProcess` | 0x10730 / 0x8c404 | 1123 / 7750 | 4 | 1 | expanded |

- 장부(`ms/disassemble/ntdll/_calls.tsv`, 이미 뽑혀 있음 — O1b 스크립트 판):
  - `LdrInitializeThunk`: `+0xe LdrpInitialize`, `+0x18 NtContinue`, `+0x1f RtlRaiseStatus`
  - `LdrpInitialize`(99바이트): `+0x27 InitSpecialMachineFrames`, `+0x2c __cpu_features_init`, `+0x3c tail LdrpInitializeInternal`, `+0x46 InitSecurityCookie`
  - `LdrpInitializeInternal`: `+0x5a _LdrpInitialize`, `+0x93 NtCreateEvent`, `+0x98 LdrpInitializeHotPatching`, `+0xc2 LdrpLoadPatchedNtdll`, `+0xdf LdrpInitializationComplete`, `+0xf7 LdrpWaitForInitializationComplete`
  - `_LdrpInitialize`: 23줄 + seh 1줄. 눈에 띄는 것: `+0x88 LdrInitializePerThreadRng`, `+0x9c indirect:?` (`CALL` 레지스터), `+0xa7 LdrpInitializeThread`, `+0xbe NtTestAlert`, `+0x14a LdrInitializeMrdata`, `+0x16c LdrpInitializeProcess`, `+0x1a1 LdrpTouchThreadStack`, `+0x252/+0x2be LdrpInitializationComplete`, `+0x278 LdrpWaitForInitializationComplete`, `+0x309 NtTerminateProcess`, `+0x354 LdrpDoDebuggerBreak`, `+0x368~+0x38f` SRW 잠금·조건 변수 기다림, `seh: LdrpInitializeProcessWrapperFilter`
- `pdb_aliases.py --where`: 세 이름 모두 서로 다른 함수 (`LdrpInitialize` 0xe1448, `_LdrpInitialize` 0x8bf8c, `LdrpInitializeInternal` 0x8be88).

## 알고 싶은 것

1. **이름 사슬 정리** (flow.md 2.4 이름 정정용):
   - 01_sprint `[관찰]` 콜스택은 `LdrInitializeThunk+0xe` ← `LdrpInitializeInternal+0x5a` ← `LdrpInitialize+0x16c` 였다. 장부로는 `+0x16c` 의 caller 가 `_LdrpInitialize`(0x8bf8c) 다.
   - PDB 공개 이름(`llvm-pdbutil-14` 등)에서 0x8bf8c 와 0xe1448 에 붙은 **정확한 이름 문자열**은 무엇인가 (앞 `_` 포함 여부, 다른 이름이 더 있나). WinDbg 가 0x8bf8c 를 `LdrpInitialize` 로 보여 준 이유를 이름 표에서 설명할 수 있나 — 못 하면 `[추측]` 으로.
   - 콜스택에 0xe1448(`LdrpInitialize`) 이 안 보인 이유가 `+0x3c` 꼬리 점프(JMP) 때문인지 `.asm` 으로 (예/아니오).
2. **`LdrpInitialize`(0xe1448, 99바이트)**: 꼬리 점프 앞에 하는 일(`InitSpecialMachineFrames`, `__cpu_features_init`)과, `+0x46 InitSecurityCookie` 갈래가 언제 타는지 (조건 줄). `LdrpInitializeInternal` 로 넘기는 `RCX`·`RDX` 가 `LdrInitializeThunk` 가 받은 그대로인가.
3. **`LdrpInitializeInternal`**: 첫 스레드가 타는 길(점프 따라) 에서 `_LdrpInitialize`(+0x5a) 앞뒤로 부르는 것과 조건. 특히 `NtCreateEvent`·`LdrpInitializeHotPatching`·`LdrpLoadPatchedNtdll` 은 test.exe 첫 스레드에서 타는가 (조건 줄과 그 값의 출처 — PEB 칸·전역 변수 이름). `_LdrpInitialize` 가 돌아온 뒤 어디로 가서 `LdrInitializeThunk +0xe` 로 돌아가나.
4. **`_LdrpInitialize` 갈림길**: `+0xa7 LdrpInitializeThread`(나중 스레드) 와 `+0x16c LdrpInitializeProcess`(첫 스레드) 를 가르는 **검사 줄**과 그 값(무엇을 보나 — PEB 칸·전역 변수 이름, 비교 상대). 첫 스레드 길에서 `+0x16c` 앞뒤로 부르는 것을 **하는 일 단위**로 시간 순서 (잡일은 개수만). 다음도 짧게:
   - `+0x9c indirect:?` 의 대상 출처 (어느 전역 변수·칸에서 꺼내나, 이름 있으면 이름). 첫 스레드가 타는가.
   - `+0x354 LdrpDoDebuggerBreak` 가 불리는 조건 — **디버거가 붙었을 때만** 타는 길인가 (CLAUDE.md "디버거가 붙어서 달라지는 점" 으로 적을 거리).
   - 나중 스레드가 첫 스레드의 로더 일이 끝날 때까지 기다리는 곳 (`LdrpWaitForInitializationComplete`, SRW·조건 변수) 이 어느 쪽 길인지 한 줄.
5. **커널이 넘긴 `RDX`** (run16: 커널 `PspSystemDlls[0]` 칸 `+0x20` 값, ntdll 이 올라간 주소 `[추측: 이름]`): 위 네 함수 중 어디서 어떤 이름의 변수·인자로 쓰이나 (`LdrpInitializeProcess` 안이면 "거기로 넘어간다" 까지만 — 본문은 Q9).
6. 표기 규칙은 reference.md 대로 (tail/frag, `seh:`, indirect-cfg, 새 O1a/O1b 표시: 조각 환산, `FAKECALL`, 전역 변수 주석, `.asm` 끝 "읽지 않은 틈" 목록 — `LdrpInitializeNode`·`_LdrpInitialize` 에 착지점 틈이 있다). `.c` 는 "`.c` 읽을 때 주의"대로 — 흐름·조건은 `.asm`.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^(LdrInitializeThunk\|LdrpInitialize\|LdrpInitializeInternal\|_LdrpInitialize)$` | 0 | Q8 본체. 이미 뽑혀 있으니 `.asm`·`.c`·장부를 그대로 읽어도 된다 (다시 뽑을 필요 없음) |

- depth 0. `LdrpInitializeProcess`·`LdrpInitializeThread` 본문은 보지 않는다 (Q9, 그 뒤).
- 다른 함수가 꼭 필요하면(예: 갈림길 값이 작은 도우미 함수에서 나옴) 지도·`--where` 로 찾아 그것만 넓히고 이유를 쓴다. analyze 는 다시 하지 않는다.

## 이미 아는 것

- `[관찰]` (01_sprint run4·run7) 메인 스레드: `LdrInitializeThunk+0xe` ← `LdrpInitializeInternal+0x5a` ← `LdrpInitialize+0x16c` → `LdrpInitializeProcess`. Worker 스레드: `LdrpInitialize+0xa7` → `LdrpInitializeThread`.
- `[정적]` (sast/02) 실제 사슬: `LdrInitializeThunk +0x9`(ret `+0xe`) → `LdrpInitialize`(0xe1448) → `+0x3c` JMP → `LdrpInitializeInternal` → `+0x55`(ret `+0x5a`) → `_LdrpInitialize` → `+0x167`(ret `+0x16c`) → `LdrpInitializeProcess`.
- `[정적]` (run16) 커널이 넘기는 `Rcx` = 유저 스택에 맡긴 원래 문맥(`CONTEXT`) 주소, `Rdx` = 커널 `PspSystemDlls[0]` `+0x20` 값. `LdrInitializeThunk +0x6 MOV RBX,RCX`, 로더가 끝나면 `+0x18 NtContinue(맡긴 문맥, TRUE)`.
- `[추측]` (flow.md 2.4) `LdrpInitialize` 는 "이 프로세스의 첫 스레드인가?"를 보고 갈림길을 고른다.

## 받고 싶은 것

- 질문 1~6 답 + 근거 (`.asm` 의 `+0x` 줄, PDB 이름 표 줄). 대표 지점은 ret_off, call_off 는 괄호.
- flow.md 2.4 그림(트리)을 고칠 안: 이름과 ret_off 로 4~8줄.
- 동적으로 확인할 것 + 추천 멈춤점 (`/p` 로 test.exe 만 — dast 몫이니 후보만).
- 결과서: `_workspace/sast/12_ldr-init-entry_result.md`
