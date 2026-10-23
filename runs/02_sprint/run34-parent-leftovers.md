# 2026-10-24 run34 — 부모 유저 쪽 남은 넷: **`kernel32!CreateProcessW` 껍데기** (ret `+0x54`), csrss 알림 `0x1001d` = **`basesrv!BaseSrvCreateProcess2`**, ntdll 스텁 번호 `0xd1`·`0x52`, ProcessFlags bit 9 = **`PspAllocateProcess +0x707`** (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 정적 (다시 analyze 안 함, flow.md 기준과 같은 분석본): kernel32 (`AE9E9D8F…D01`), KernelBase (`72CCE506…BD1`), ntdll (`C3093720…FE1`), ntoskrnl (`ntkrnlmp.pdb` `C29EBFB06B78B3C020DCA66D99713F9E1`).
- **처음 analyze 한 모듈 둘** (csrss 프로세스 안에서 도는 DLL):
  - `basesrv.dll` — 함수 156개, PDB `4B0951C4FAD4CCBDDFDAA284D1ED2C2E1`
  - `csrsrv.dll` — 함수 129개, PDB `32C5AA2B9D0AF671C35069A02C5A0C041`
- 덤: `cmd.exe` PDB (`6E820A0DD74C2CA6E23F25DEBA1651261`) 를 받아 **이름 붙이기에만** 썼다. cmd.exe 는 Ghidra 가 아니라 `objdump` 로 읽었다 (요청 밖 정보, 아래 "cmd.exe 쪽 참고").
- 버전 대조 (결과서): `NtCreateUserProcess` ret `+0xc36` → `PspAllocateProcess`, ntdll `LdrpInitializeProcess` ret `+0x1bbb` → `LdrpInitializeGraphRecurse` 가 기준과 같다.
- PE 표(IAT, API-set 표, `KiServiceTable`, `BaseServerApiDispatchTable`)는 결과서가 `pefile` 임시 스크립트로 읽었다 (하네스 밖, 커밋 안 함).

| 함수 | rva | 이번에 본 것 |
|---|---|---|
| `kernel32!CreateProcessW` | `0x3bdd0` (94B) | 전체 |
| `ntdll!NtCreateUserProcess` / `ntdll!NtResumeThread` | `0x1627c0` / `0x1617f0` (각 24B) | 전체 |
| `ntdll!CsrClientCallServer` | `0xdbdd0` | `+0x27`, `+0x51` 만 |
| `nt!NtCreateUserProcess` | (run9) | ProcessFlags(7번째 인자) 를 읽고 쓰는 줄만 |
| `nt!PspAllocateProcess` | (run10) | 9번째 인자 받는 줄 `+0x2ba ~ +0x2c1`, `+0x625 ~ +0x710` |
| `nt!PspInsertProcess`, `nt!PspValidateCreateProcessProtection`, `nt!PspImplicitAssignProcessToJob` | — | ProcessFlags 를 받아 어느 비트를 보는지만 |
| `basesrv!ServerDllInitialization` | `0x2f50` | `+0x149 ~ +0x186` (서버 구조체 채우기) |
| `basesrv!BaseSrvCreateProcess2`, `InternalBaseSrvCreateProcess` | `0x6ee0` (815B), — | 호출 줄 |
| `csrsrv!CsrApiRequestThread` | `0x9340` | `+0x47f ~ +0x4e6`, `+0x634 ~ +0x656` |
| `csrsrv!CsrParseServerCommandLine`, `CsrLoadServerDll`, `CsrCreateProcess`, `CsrpCreateProcess` | — | 호출 줄 |

## 목적

- Q41 (flow.md 4장 2.2, run14 이 남긴 작은 정적 넷):
  1. `kernel32!CreateProcessW` 가 `KERNELBASE!CreateProcessW` 를 부르나 → 그림 1 `kb1` 앞에 상자를 둘 수 있나.
  2. csrss 알림 ApiNumber `0x1001d` 의 정체.
  3. ntdll `NtCreateUserProcess`·`NtResumeThread` 스텁이 바로 시스템 콜을 하나 (번호가 커널 표의 그 칸인가).
  4. KERNELBASE 가 켜는 ProcessFlags bit 9 (`0x200`) 를 커널이 어떻게 쓰나.
- 함께: Q65 의 "`PspAllocateProcess` 인자 `[RSP+0x90]` bit 9" (run33) 가 4번과 같은 비트인가.

## 방법

- 정적: `_workspace/sast/29_parent-leftovers.md` — kernel32 `^CreateProcessW$` depth 1, ntdll 스텁 둘 depth 0, ntoskrnl `NtCreateUserProcess`·`PspAllocateProcess` (이미 있음, 줄만), basesrv·csrsrv 처음 analyze (depth 1 / depth 0)
- 결과서: `_workspace/sast/29_parent-leftovers_result.md` (**분석성공**)
- 이번에 `.asm` 과 다시 맞춰 본 줄 (flow-writer):
  - kernel32 `CreateProcessW` `+0x7 ~ +0x49` (스택 인자 복사), `+0x4d/+0x54`, `+0x59`, `+0x5d`
  - ntdll `NtCreateUserProcess`·`NtResumeThread` `+0x0 ~ +0x17` 전체, `CsrClientCallServer` `+0x27`, `+0x51`
  - KERNELBASE `CreateProcessInternalW` `+0x7f6`, `+0x800`, `+0x306b`, `+0x30b3`, `+0x30e7/+0x30ee`
  - nt `NtCreateUserProcess` `+0x1b0`, `+0x1b6`, `+0x1bc`, `+0x1bf`, `+0x1c9 ~ +0x1ce`, `+0x2cd`, `+0xbf6`, `+0xbfc`, `+0xc31/+0xc36`, `+0xdea`, `+0xfaf`
  - nt `PspAllocateProcess` `+0x2ba`, `+0x2c1`, `+0x625`, `+0x668`, `+0x670`, `+0x6f1`, `+0x6f9`, `+0x703`, `+0x707`, `+0x70c`, `+0x710`
  - nt `PspProcessClose` `+0x9`, `+0xf`, `+0x22`, `+0x29`
  - basesrv `BaseSrvCreateProcess2` `+0xac/+0xb3`, `+0x1b3`, `+0x216/+0x21b`, `+0x26b`; `InternalBaseSrvCreateProcess` `+0xf7`, `+0xfb/+0x100`, `+0x175/+0x17c`, `+0x1b7/+0x1bc`; `ServerDllInitialization` `+0x149`, `+0x153`, `+0x15a`, `+0x17f`
  - csrsrv `CsrApiRequestThread` `+0x47f ~ +0x4e6`, `+0x651/+0x656`; `CsrCreateProcess` `+0x18/+0x1d`; `CsrpCreateProcess` `+0x9e`, `+0x507/+0x50c`, `+0x556`; `CsrParseServerCommandLine` `+0x4f7`, `+0x588/+0x58d`
  - 결과서와 다른 곳: 결과서 "`+0x483` … `SHR 0x10`" 은 한 줄 어긋남. `.asm` 으로는 **`+0x47f SHR R9D,0x10`**, `+0x483 CMP R9D,6`, `+0x487 JNC`. 뜻은 같다. 이 문서는 `.asm` 줄을 쓴다.

## 결과

비유 (관리사무소 = 커널, 관리실 = ntdll, csrss = 동네 주민센터):

- 부모(cmd.exe)가 "가게 하나 내 주세요" 할 때 **맨 처음 들르는 창구가 kernel32** 였다. 그런데 이 창구는 서류를 한 장도 안 보고 **옆 창구(KERNELBASE)로 그대로 넘긴다.**
- KERNELBASE 는 관리실(ntdll)의 **전화기**로 관리사무소(커널)에 건다. 전화기에 찍힌 번호가 `0xd1` (만들기), `0x52` (풀어 주기) 이고, 관리사무소 번호부(`KiServiceTable`)의 그 칸이 정말 그 담당자다.
- 가게를 만든 뒤 KERNELBASE 는 **주민센터(csrss)에 전입 신고**를 한다. 신고서 번호 `0x1001d` = "1번 창구(basesrv)의 `0x1d` 번 업무" = `BaseSrvCreateProcess2`. 주민센터는 새 가게 열쇠를 직접 받아 주민 명부(`CsrCreateProcess`)에 올린다.
- "빨간불 그대로 둬"(`CREATE_SUSPENDED`) 가 **없으면** KERNELBASE 는 신청서에 작은 표시(bit 9)를 하나 더 한다. 관리사무소는 그 표시를 보고 가게 카드의 "주인" 칸(`OwnerProcessId`)에 **부모 번호를 그대로** 적는다. 표시가 없으면 부모 번호에 `|2` 를 붙여 적는다.

### 정적 — 호출 지점 (`.asm` CALL 줄, 라벨은 ret)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `kernel32!CreateProcessW` | `+0x4d` | **`+0x54`** | `KERNELBASE!CreateProcessW` (IAT `0x8baa8` = `api-ms-win-core-processthreads-l1-1-0.dll!CreateProcessW` → kernel32 가 가져오면 kernelbase 로 풀림) | [정적] |
| `KERNELBASE!CreateProcessW` | `+0x61` | `+0x66` | `KERNELBASE!CreateProcessInternalW` | [정적] (run14) |
| `KERNELBASE!CreateProcessInternalW` | `+0x30e7` | `+0x30ee` | `ntdll!NtCreateUserProcess` | [정적] (run14, 다시 확인) |
| `ntdll!NtCreateUserProcess` | `+0x12` `SYSCALL` (번호 `0xd1`) | — (유저 쪽 `+0x14 RET`) | `KiServiceTable[0xd1]` = `nt!NtCreateUserProcess` | [정적] |
| `ntdll!NtResumeThread` | `+0x12` `SYSCALL` (번호 `0x52`) | — (`+0x14 RET`) | `KiServiceTable[0x52]` = `nt!NtResumeThread` | [정적] |
| (위 둘) | `+0x15` `INT 0x2e` | — (`+0x17 RET`) | (같은 번호, 대체 길) | [정적] |
| `nt!NtCreateUserProcess` | `+0xc31` | `+0xc36` | `nt!PspAllocateProcess` (9번째 인자 = ProcessFlags) | [정적] (run9 자리) |
| `csrsrv!CsrApiRequestThread` | `+0x4e1` (캡처 버퍼 있는 갈래 `+0x651`) | `+0x4e6` (`+0x656`) | CFG 간접: 서버 구조체 `[+0x28]` 표의 칸 → `0x1001d` 면 `basesrv!BaseSrvCreateProcess2` | [정적] (대상은 표 값) |
| `basesrv!BaseSrvCreateProcess2` | `+0xac` / `+0x1b3` | `+0xb3` / `+0x1ba` | `NtOpenProcess` / `NtOpenThread` | [정적] |
| `basesrv!BaseSrvCreateProcess2` | `+0x216` | `+0x21b` | `basesrv!InternalBaseSrvCreateProcess` | [정적] |
| `basesrv!InternalBaseSrvCreateProcess` | `+0xfb` | `+0x100` | CFG 간접: 전역 `sxsFunctions` 표 `+0x8` 칸 (SxS, 조건부) | [정적] (대상은 실행 때) |
| `basesrv!InternalBaseSrvCreateProcess` | `+0x175` | `+0x17c` | `csrsrv!CsrCreateProcess` | [정적] |
| `basesrv!InternalBaseSrvCreateProcess` | `+0x1b7` | `+0x1bc` | CFG 간접: 전역 `UserNotifyProcessCreate` (성공했고 값이 있을 때) | [정적] (대상은 실행 때) |
| `csrsrv!CsrCreateProcess` | `+0x18` | `+0x1d` | `csrsrv!CsrpCreateProcess` | [정적] |
| `csrsrv!CsrpCreateProcess` | `+0x507` | `+0x50c` | `csrsrv!CsrInsertProcess` | [정적] |

### 정적 — 질문 1: `kernel32!CreateProcessW` 는 껍데기

- **인자 열 개를 그대로 넘기고 `KERNELBASE!CreateProcessW` 를 한 번 부른다.** call `+0x4d` / ret **`+0x54`**. 명령 18개, 호출은 이 하나뿐.
  - 앞 넷(레지스터)은 안 건드린다. 5 ~ 10번째(스택)는 `+0x7 ~ +0x49` 에서 새 자리로 한 칸씩 옮길 뿐, 값을 바꾸는 줄이 없다.
  - 돌아와서 `NOP; ADD RSP; RET` — 결과(EAX)를 그대로 돌려준다. 검사·ETW 같은 다른 일 없음.
- **IAT 칸 이름은 API-set 이름**이다: `api-ms-win-core-processthreads-l1-1-0.dll` 의 `CreateProcessW`.
  - 같은 OS 의 `apisetschema.dll` 표로 보면: 기본은 `kernel32.dll`, **가져오는 쪽이 `kernel32.dll` 이면 `kernelbase.dll`**. 그래서 이 칸은 `KERNELBASE!CreateProcessW` (export rva `0x7d5f0`, 107B) 로 간다 `[정적: PE 표]`. 실행 때 실제로 풀어 주는 건 ntdll 이 PEB 의 ApiSet 표로 한다.
- 그러니 부모 유저 쪽 사슬 (코드 기준): **`kernel32!CreateProcessW +0x54` → `KERNELBASE!CreateProcessW +0x66` → `KERNELBASE!CreateProcessInternalW +0x30ee` → `ntdll!NtCreateUserProcess`**.

#### cmd.exe 쪽 참고 (요청 밖, `objdump` + `cmd.exe.pdb`)

- `[정적]` cmd.exe import 표에 `CreateProcessW`·`CreateProcessAsUserW`·`ResumeThread` 가 **같은 API-set 이름**으로 있다. cmd.exe 가 가져오면 기본값 `kernel32.dll` 로 풀린다 → cmd.exe 는 `kernel32!CreateProcessW` 에 닿는다.
- `[정적]` 그 칸을 부르는 곳 중 대표: **`cmd!ExecPgm` `CreateProcessW` call `+0x595` / ret `+0x59c`** (AsUser 쪽 ret `+0x51b`). `dwCreationFlags` 자리에 상수 **`0x80000`** (`EXTENDED_STARTUPINFO_PRESENT`) 만 싣는다 (`+0x57d`). **`CREATE_SUSPENDED`(4) 없음.**
- `[정적]` `cmd!Start` (`start` 명령으로 보임 `[추측: 이름]`) 는 다르다: `CreateProcessW` ret `+0xe52`, 조건에 따라 4 를 켜고 나중에 `ResumeThread` (ret `+0xfa2`).
- `[추측]` test.exe 를 그냥 입력해 실행하면 `ExecPgm` 길. 실제로 어느 길을 타는지는 동적 (Q19).

### 정적 — 질문 2: ApiNumber `0x1001d` = `basesrv!BaseSrvCreateProcess2`

- **짜임: 위 16비트 = 서버 DLL 번호 `1`, 아래 16비트 = 그 서버의 API 번호 `0x1d`.** csrss 쪽이 이렇게 나눈다 (`csrsrv!CsrApiRequestThread`):
  - `+0x47f SHR R9D,0x10` → 서버 번호. `+0x483 CMP R9D,6` / `+0x487 JNC` (6 미만이어야). `+0x493` 전역 `CsrLoadedServerDll[번호]` 를 꺼냄 (비었으면 `+0x49a` 오류 길).
  - `+0x49c MOVZX` 아래 16비트 − 서버 구조체 `[+0x20]` (시작 번호) 가 `[+0x24] − [+0x20]` 보다 작아야 (`+0x4ac`·`+0x4ae`).
  - `+0x4c4 ~ +0x4c8` `[+0x28]` 표의 그 칸 → `+0x4e1` CFG 간접 호출 (ret `+0x4e6`). 캡처 버퍼 갈래는 `+0x651` (ret `+0x656`).
- ntdll 쪽: `CsrClientCallServer +0x27 BTR R8D,0x1c` (bit 28 지움) → `+0x51` 메시지 `+0x30` 에 ApiNumber 저장. `0x1001d` 에는 bit 28 이 없다.
- basesrv 가 서버 구조체를 채우는 줄 (`ServerDllInitialization`): `+0x153 ~ +0x15a` `[+0x28]` ← **`BaseServerApiDispatchTable`** (rva `0xf550`), `+0x149` `[+0x20]` ← 0, `+0x17f` `[+0x24]` ← **`0x1f`**. → 칸은 `0 ~ 0x1e` 31개, `0x1d` 는 안이다. (칸 이름은 공개 문서 `CSR_SERVER_DLL` 모양에서 빌림 `[추측]`, 오프셋·값은 `[정적]`)
- 표의 `0x1d` 칸 (rva `0xf638`) = **`BaseSrvCreateProcess2`** (rva `0x6ee0`). 앞뒤 `[0x1c]` `BaseSrvNlsUpdateCacheCount`, `[0x1e]` `BaseSrvCreateActivationContext2`. `[0]` `BaseSrvCreateProcess` 는 옛 V1 메시지를 V2 로 바꿔 같은 `BaseSrvCreateProcess2` 를 부른다.
- **`BaseSrvCreateProcess2` 가 하는 일 (대표만)** `[정적]`:
  1. 메시지 안 CLIENT_ID 로 새 프로세스·스레드 핸들을 csrss 가 **직접 연다** (`NtOpenProcess` ret `+0xb3`, `NtOpenThread` ret `+0x1ba`). 사이에 `NtQueryInformationProcess` 셋으로 부모·자식 관계 검사 (실패면 `0xc0000022`).
  2. `InternalBaseSrvCreateProcess` (ret `+0x21b`) 안에서:
     - SxS 쪽 간접 호출 (ret `+0x100`, 조건부. 표는 `BaseSrvRegisterSxS` 가 채우고 sxssrv.dll 이 그 함수를 import)
     - **`CsrCreateProcess`** (ret `+0x17c`) → `CsrpCreateProcess` (ret `+0x1d`): 프로세스 구조 만들기 (`CsrAllocateProcess` call `+0x9e`) → 예외 포트 → **`CsrInsertProcess`** (ret `+0x50c`, 목록에 넣기) → 첫 스레드도 등록 (`CsrCreateThread` call `+0x556`). 즉 **"csrss 에 새 프로세스 등록"**.
     - 성공하면 `UserNotifyProcessCreate` 간접 호출 (ret `+0x1bc`, 표는 `BaseSetProcessCreateNotify` 가 채우고 winsrvext.dll 이 import)
  3. VDM 인자가 있을 때만 `BaseSrvUpdateVDMSequenceNumber` (call `+0x26b`).
- **서버 번호 `1` = basesrv 는 코드에 박혀 있지 않다.** csrss 명령줄 `ServerDLL=이름[:함수],번호` 에서 온다 (`CsrParseServerCommandLine` → `CsrLoadServerDll` ret `+0x58d`. 번호 0 은 csrsrv 자신, `+0x4f7`). 명령줄은 smss 가 레지스트리에서 만든다 `[추측: 일반 지식]`.
  - **간접 증거 `[정적]`**: kernel32·KernelBase 가 `CsrClientCallServer` 에 싣는 다른 번호들(`0x10014`, `0x1000c`, `0x1000d`, `0x1001c`, `0x10016`, `0x10012`, `0x10015`, bit 28 지운 `0x1001e`·`0x10007`·`0x10011`)의 아래 16비트가 **basesrv 표 칸 이름과 전부 맞고**, 위 16비트는 모두 `1` 이다. 그래서 `0x1001d` 의 `1` 도 basesrv 로 본다. 확정은 동적 (Q19, 낮음).
- 이 일은 **csrss 프로세스의 요청 스레드**에서 돈다. 부모 cmd.exe 스레드가 아니다. 부모는 `CsrClientCallServer` 로 보내고 답을 기다린다 `[추측: 일반 지식, 기다리는 줄은 안 봄]`.

### 정적 — 질문 3: ntdll 스텁 둘

| ntdll 스텁 | rva / 크기 | `+0x3 MOV EAX,` | `KiServiceTable[번호]` | 앞뒤 칸 |
|---|---|---|---|---|
| `NtCreateUserProcess` | `0x1627c0` / 24B | **`0xd1`** | `nt!NtCreateUserProcess` (rva `0xa01340`) | `[0xd0]` NtCreateTransactionManager, `[0xd2]` NtCreateWaitCompletionPacket |
| `NtResumeThread` | `0x1617f0` / 24B | **`0x52`** | `nt!NtResumeThread` (rva `0xa2d600`) | `[0x51]` NtQuerySection, `[0x53]` NtTerminateThread |

- 읽는 법은 run29 (`NtContinue` `0x43`) 와 같다: `KiServiceTable` (rva `0xdadc0`, 4바이트 칸), `KiServiceLimit` = 489. 489칸 전부가 `_index.tsv` 함수 시작과 맞았다.
- 모양 (둘 다 같음): `+0x0 MOV R10,RCX` → `+0x3 MOV EAX,번호` → **`+0x8 TEST byte [0x7ffe0308],1`** → `+0x10 JNZ +0x15` → **`+0x12 SYSCALL`** → `+0x14 RET` / **`+0x15 INT 0x2e`** → `+0x17 RET`.
  - 즉 "바로 SYSCALL 하나" 가 아니라 **갈래가 하나 붙어 있다**: 공유 사용자 데이터 `+0x308` (칸 이름 `SystemCall` 로 보임 `[추측]`) 의 bit 0 이 켜져 있으면 `INT 0x2e`. 보통은 SYSCALL 쪽일 것 `[추측]`. 어느 쪽인지는 동적 (Q19).
  - 같은 모양이 run29 의 `NtContinue` 스텁에도 있다 (그때는 `+0x12 SYSCALL` 만 적었다).

### 정적 — 질문 4: ProcessFlags bit 9 의 길

**답: KERNELBASE 가 켜는 bit 9 = run33 의 `[RSP+0x90]` bit 9. 같은 비트다.** → Q65 의 bit 9 부분이 풀렸다.

```
KERNELBASE!CreateProcessInternalW                                         (run14 + run34)
 │  +0x7f6  TEST [RSP+0x108](dwCreationFlags 사본), 4
 │  +0x800  4 가 없으면 BTS [RSP+0x110], 9        ← bit 9 켜기
 │  +0x306b EAX = [RSP+0x110] → +0x30b3 [RSP+0x30] (7번째 인자)
 └─ +0x30ee ntdll!NtCreateUserProcess ══▶ SYSCALL 0xd1
      nt!NtCreateUserProcess                    7번째 인자 = [RBP+0x7d0]
       │  +0x1b0 EBX = [RBP+0x7d0]
       │  +0x1b6 TEST EBX,0xff317838  (+0x1bf ThreadFlags & ~1 와 함께, +0x1ce 걸리면 0xc000000d)
       │         → bit 9 는 마스크에 없음 = 호출자가 켜도 되는 비트
       │  +0x2cd (bit 6 지운 값 되쓰기), +0xdea (BTS bit 10, PspAllocateProcess 뒤) — bit 9 안 건드림
       │  +0xbf6 EAX = [RBP+0x7d0] → +0xbfc [RSP+0x40] (9번째 인자)
       └─ +0xc36 PspAllocateProcess              9번째 인자 = [RSP+0x8b0]
             │  +0x2ba / +0x2c1  [RSP+0x90] = 9번째 인자          (여기서 [RSP+0x90] 이 생김)
             │  +0x625 R8D = [RSP+0x90]  (또는 +0x668 ~ +0x670 같은 값 다시 읽기)
             │  +0x6f1 / +0x6f9  RDX = 부모 EPROCESS +0x1d0 (PID)
             │  +0x703  RAX = PID | 2
             │  +0x707  BT R8D, 9   → +0x70c CMOVC RAX,RDX  (켜졌으면 |2 버림)
             └─ +0x710  [새 EPROCESS +0x2d8] (OwnerProcessId) = RAX
```

- 표로 정리 `[정적]`:

| 부모가 준 `dwCreationFlags` | KERNELBASE bit 9 | `OwnerProcessId` (`+0x2d8`) |
|---|---|---|
| `CREATE_SUSPENDED`(4) **없음** | 켬 (`+0x800`) | **부모 PID 그대로** |
| `CREATE_SUSPENDED`(4) 있음 | 안 켬 | 부모 PID `\|2` |

- cmd.exe `ExecPgm` 은 `0x80000` 이라 4 가 없다 → 그 길로 만든 test.exe 는 **`|2` 없는 쪽**이다 `[정적: 두 코드의 상수, 실제 길은 동적 Q19]`.
- **bit 9 를 읽는 곳은 `PspAllocateProcess +0x707` 하나로 보인다** `[정적]`. ProcessFlags 를 받는 다른 셋은 다른 비트만 본다:
  - `PspValidateCreateProcessProtection` (`NtCreateUserProcess +0xa4f`·`+0xb95` 에서 부름): bit 2 만.
  - `PspInsertProcess` (`NtCreateUserProcess +0xfaf` 에서 `R9D ← [RBP+0x7d0]`): bit 1 만 (`+0x318`). 사본을 `PspImplicitAssignProcessToJob` (`+0xce`) 로 넘김 → 거기선 bit 10·bit 0 만.
  - `PspAllocateProcess` 안 다른 `BT …,9` (`+0x765`, `+0x145d`) 는 다른 변수다.

#### run33 `PspProcessClose` 와 이어 보기

- run33: `PspProcessClose +0xf` 에서 `ProcessFirstResume`(bit 15) 가 **꺼져 있으면** `+0x22 ~ +0x29` "닫히는 프로세스 `OwnerProcessId` == 닫는 프로세스 PID" 를 보고, 맞고 닫는 쪽이 지워지는 중이면 `PsTerminateProcess(0xc000010a)` (ret `+0x4c`).
- 이번 결과로 **비교가 맞을 수 있는 쪽이 보통 생성**이다: `CREATE_SUSPENDED` 없이 만들면 `OwnerProcessId` = 부모 PID 그대로라 "부모가 닫을 때" 같다.
- `CREATE_SUSPENDED` 로 만들면 `|2` 가 붙어 어느 PID 와도 안 맞는다 (PID 는 4 의 배수 `[추측: 일반 지식]`) → 이 종료 길이 막힌다.
- 뜻 `[추측]`: "보통 생성이면, 첫 스레드가 한 번 풀리기 전에 부모가 죽으며 핸들을 닫을 때 자식도 같이 끝낸다. 일부러 멈춰 만든(`CREATE_SUSPENDED`) 자식은 그 길에서 뺀다."
- test.exe 시동 길 `[추측: 순서]`: 부모는 `NtResumeThread` (KERNELBASE `+0x5bd3`) 로 bit 15 를 먼저 켠다. 그 뒤에 핸들을 닫으면 `+0x1b` 로 바로 return. 실제로 서는지는 그대로 Q64.

### 호출 관계

```
(부모 cmd.exe 스레드, 유저)                                         (run14 + run34)
cmd!ExecPgm  (cmd.exe 쪽 참고, objdump)                 [정적, 실제 길은 Q19]
 └─ +0x59c  kernel32!CreateProcessW                    껍데기, 인자 그대로
      └─ +0x54  KERNELBASE!CreateProcessW              껍데기
           └─ +0x66  KERNELBASE!CreateProcessInternalW
                ├─ (1) +0x30ee  ntdll!NtCreateUserProcess   EAX 0xd1
                │        ═▶ +0x12 SYSCALL (0x7ffe0308 bit0 면 +0x15 INT 0x2e)
                │             → KiServiceTable[0xd1] = nt!NtCreateUserProcess
                │                  └─ +0xc36 PspAllocateProcess  +0x707 bit 9 → +0x710 OwnerProcessId
                ├─ (2) +0x4672  ntdll!CsrClientCallServer   ApiNumber 0x1001d (+0x27 bit 28 지움, +0x51 저장)
                │        ┄┄ csrss 요청 스레드 (다른 프로세스) ┄┄
                │        csrsrv!CsrApiRequestThread  +0x47f 서버 1 / 아래 0x1d
                │         └─ +0x4e6 (CFG)  basesrv!BaseSrvCreateProcess2
                │              └─ +0x21b  InternalBaseSrvCreateProcess
                │                   └─ +0x17c  csrsrv!CsrCreateProcess
                │                        └─ +0x1d  CsrpCreateProcess  → +0x50c CsrInsertProcess
                └─ (3) +0x5bd3  ntdll!NtResumeThread        EAX 0x52
                         ═▶ +0x12 SYSCALL → KiServiceTable[0x52] = nt!NtResumeThread
```

### 그림 반영 (flow.md 그림 1)

- **`KERNEL32` 상자를 새로 넣고 `kernel32!CreateProcessW` 를 `(정적)` 점선 상자로 `kb1` 앞에 뒀다.** 간선 `+0x54`.
  - 넣은 이유 (대표 지점 기준): cmd.exe 가 import 표로 **실제로 닿는 첫 OS 함수**가 이것이다. 동적 Q19 의 `k` 에서도 `kernel32!CreateProcessW+0x54` 로 보일 자리다. 하는 일이 없는 껍데기지만 `KERNELBASE!CreateProcessW` 도 같은 껍데기로 이미 그림에 있고, 모듈 경계(kernel32 → KERNELBASE)를 보여 주는 칸이라 뺄 이유가 없다 (R7).
  - cmd.exe 상자(`ExecPgm`)는 **안 넣었다.** cmd.exe 는 분석 범위 밖이고 Ghidra 가 아니라 `objdump` 로 읽은 참고 정보다. 글(2.2)에만 둔다.
- `kb1` 글: "(정적: 부모가 이 입구로 오는지는 추측)" → "(정적: 107B 껍데기. cmd.exe 는 import 표로 kernel32 거쳐 옴, 실제 호출은 Q19)".
- `nd1`·`nd3` 글: "시스템 콜 스텁 추측" → 번호 `0xd1`/`0x52` = `KiServiceTable` 같은 칸 (`[정적]`), `INT 0x2e` 갈래.
- `nd1 ==> a0`, `nd3 ==> r0a` 굵은 화살표 라벨: "시스템 콜 (추측)" → "`+0x12 SYSCALL` (정적, `INT 0x2e` 갈래)". 굵은 화살표 끝의 `KiSystemServiceCopyEnd` 는 그대로다 (run3 `[관찰]` 은 `NtCreateUserProcess` 쪽만).
- `nd2` 글에 `0x1001d` = basesrv `0x1d` 칸 `BaseSrvCreateProcess2` 를 적었다.
- **csrss 쪽은 노란 메모 하나로만** 넣었다 (R13, `nd2 -.- wc`). `BaseSrvCreateProcess2` 는 **csrss 프로세스의 다른 스레드**에서 돌아서 그림 1 (부모 cmd.exe 스레드 한 장, R4) 의 함수 상자로 둘 수 없다.
- `a2` (`PspAllocateProcess`) 상자 글에 `+0x710 OwnerProcessId` 를 넣지는 않았다. 호출이 아니라 값 쓰기 한 줄이라 글(2.1·2.2)에만 둔다.

## 확인된 것

- (`[정적]`) `kernel32!CreateProcessW` 는 인자 열 개를 그대로 넘기는 껍데기다. call `+0x4d` / ret `+0x54`, IAT 는 API-set 이름이고 kernel32 가 가져오면 `KERNELBASE!CreateProcessW` 로 풀린다.
- (`[정적]`) cmd.exe import 표에 같은 API-set 이름의 `CreateProcessW` 가 있다 → 코드상 **cmd.exe → `kernel32!CreateProcessW` → `KERNELBASE!CreateProcessW`** 길이 있다 (cmd.exe 쪽은 `objdump` 참고).
- (`[정적]`) ApiNumber `0x1001d` = 서버 번호 1 + API `0x1d`. basesrv 표 `0x1d` 칸 = **`BaseSrvCreateProcess2`** — csrss 가 새 프로세스·스레드 핸들을 열고 `CsrCreateProcess` → `CsrInsertProcess` 로 **csrss 목록에 등록**, SxS·`UserNotifyProcessCreate` 간접 호출.
- (`[정적]` 간접 증거) 서버 번호 1 = basesrv. 코드에 박혀 있지 않고 csrss 명령줄 값이다.
- (`[정적]`) ntdll `NtCreateUserProcess` = `SYSCALL 0xd1`, `NtResumeThread` = `SYSCALL 0x52`, `KiServiceTable` 의 그 칸이 같은 이름의 커널 함수. 둘 다 `0x7ffe0308` bit 0 이면 `INT 0x2e` 로 가는 갈래가 있다.
- (`[정적]`) KERNELBASE ProcessFlags bit 9 → 커널 `NtCreateUserProcess` 7번째 인자 (검사 마스크에 없음, 안 바꿈) → `PspAllocateProcess` 9번째 인자 `[RSP+0x90]` → **`+0x707 BT R8D,9`** → `+0x710 OwnerProcessId`. **켜졌으면 부모 PID 그대로, 꺼졌으면 `|2`.**
- (`[정적]`) bit 9 를 읽는 곳은 `PspAllocateProcess +0x707` 하나로 보인다 (ProcessFlags 를 받는 다른 셋은 bit 0·1·2·10 만).

### flow.md 판정

| 예전 글 | 판정 | 근거 |
|---|---|---|
| 그림 1 `kb1` "(정적: 부모가 이 입구로 오는지는 추측)" | **`[정적]` 으로 좁힘** — cmd.exe import 표 → kernel32 껍데기 → KERNELBASE. 실제 호출은 동적 Q19 | 질문 1 |
| 2.2 `KERNELBASE!CreateProcessW` `[추측]` "cmd.exe 는 이 입구(또는 `kernel32!CreateProcessW` 를 거쳐)" `[공백]` | **`[정적]`** kernel32 `+0x54` → KERNELBASE `+0x66`. 남은 건 실행 확인 (Q19) | 질문 1 |
| 그림 1 `nd1`·`nd3` "시스템 콜 스텁 추측", 굵은 화살표 "(추측)" | **`[정적]`** `SYSCALL 0xd1`/`0x52` = `KiServiceTable` 같은 칸. `INT 0x2e` 갈래는 값에 따라 (Q19) | 질문 3 |
| 2.2 csrss 알림 "ApiNumber `0x1001d`" (정체 모름) | **`[정적]`** basesrv `BaseSrvCreateProcess2` (csrss 등록). 서버 1 = basesrv 는 간접 증거 | 질문 2 |
| 2.2 `CreateProcessInternalW` "커널에서 이 비트(bit 9)의 뜻은 안 봤다" | **`[정적]`** `PspAllocateProcess +0x707` → `OwnerProcessId` `|2` 여부 | 질문 4 |
| 2.2 `PspProcessClose` "일반 생성에선 이 비교가 맞기 어려워 보인다 … bit 9 의 뜻은 Q65" | **정정** (아래) | 질문 4 |
| 4장 2.2 줄 넷 (cmd.exe → kernel32?, bit 9, `0x1001d`·스텁, Q65 bit 9 부분) | **해결됨** (실행 확인은 Q19, Q65 는 둘만 남음) | 위 |

## 정정

- **run33 "일반 생성에선 `PspProcessClose` 의 주인 PID 비교가 맞기 어려워 보인다" `[정적 읽기 + 추측]` 는 틀렸다.**
  - run33 은 `OwnerProcessId` 에 `|2` 가 붙는 쪽을 보통으로 짐작했다 (bit 9 뜻을 몰라서).
  - 이번에 보니 bit 9 는 **`CREATE_SUSPENDED` 가 없을 때** KERNELBASE 가 켠다. 그래서 보통 생성(cmd.exe `0x80000`)에선 **`|2` 가 안 붙고 `OwnerProcessId` = 부모 PID 그대로**다. 비교가 맞을 수 있다.
  - `|2` 가 붙어 비교가 막히는 건 `CREATE_SUSPENDED` 로 만든 경우다.
  - run33 의 나머지 (읽는 곳 둘, `+0x1b` return, Q64 의 "test.exe 땐 bit 15 가 먼저 켜져 바로 return 할 것" `[추측]`) 는 그대로다.
  - run33 문서 맨 위에 정정 한 줄을 넣었다. flow.md 2.2 `PspProcessClose` 절도 고쳤다.

## 아직 확인 필요

동적 확인거리는 오케스트레이터가 대기열 **Q19** 에 출처(sast/29)로 붙였다. 새 번호 없음.

- **Q19** (a) 부모 쪽 길: `nt!NtCreateUserProcess` 입구 (cmd.exe 문맥) 긴 `k` — 기대 `KERNELBASE!CreateProcessInternalW+0x30ee` ← `KERNELBASE!CreateProcessW+0x66` ← `kernel32!CreateProcessW+0x54` ← `cmd!ExecPgm+0x59c`. 맞으면 그림 1 `KERNEL32` 상자·`kb1` 이 `[관찰]` 이 된다.
- **Q19** (b) 같은 자리에서 7번째 인자 (ProcessFlags) bit 9 켜짐 — 입구 기준 `dwo(@rsp+0x38)` `[추측: 오프셋 계산]`. cmd.exe `dwCreationFlags` = `0x80000`.
- **Q19** (c) `nt!PspAllocateProcess+0x710` 직전 `rax` = 부모 PID 그대로 (`|2` 없음), 지난 뒤 `dq <새 EPROCESS>+0x2d8 L1` 같은 값.
- **Q19** (d) ntdll 스텁이 `SYSCALL` 쪽인가 `INT 0x2e` 쪽인가 (`db 0x7ffe0308 L1` bit 0).
- **Q19** (e, 낮음) csrss 쪽: csrss 문맥에서 `basesrv!BaseSrvCreateProcess2` 입구 `k` (기대 `csrsrv!CsrApiRequestThread+0x4e6` 또는 `+0x656`), 메시지 `poi(@rcx+0x30)` = `0x1001d`, `CsrClientCallServer +0x4672` 와 순서. 서버 번호 확정은 `dq csrsrv!CsrLoadedServerDll L6` 의 `[1]` 이름이 basesrv 인가 (부팅 뒤 아무 때나).
- **Q65** (좁힘, 정적): bit 9 부분은 풀렸다. 남은 것 — `NtSetInformationProcess +0x1ecd` (`OwnerProcessId` 를 쓰는 다른 곳, 어느 정보 클래스), `ObCloseHandleTableEntry +0x7a9` 간접 호출 앞 인자 채움 (`PspProcessClose` 인자 이름 확인).
- **Q66** (새, 정적, 작은 둘): (a) 스텁이 보는 `0x7ffe0308` 칸 이름 (`_KUSER_SHARED_DATA.SystemCall` 로 보임 `[추측]`, PDB 타입), (b) `CsrClientCallServer` 가 csrss 답을 기다리나 (지금 `[추측: 일반 지식]`). `INT 0x2e` 갈래의 커널 입구는 동적 **Q19** (d) 에 같이.

## 디버거 메모 / 도구 메모

- (도구, 하네스 제안 — 결과서 "스크립트 문제") **`run_ghidra.sh refs` 에 import 이름을 주면 외부 주소로 풀려 엉뚱한 줄이 나온다.** import 면 멈추거나 IAT 칸 rva 로 바꿔 주면 좋다. 이번엔 `pefile` + `FF 15` 바이트 스캔으로 했다.
- (도구, 하네스 제안) **함수 포인터 표 안을 읽는 명령이 없다.** `KiServiceTable` (run29)·`BaseServerApiDispatchTable` (이번) 둘 다 임시 스크립트로 읽었다. `table <바이너리> <이름|rva> <개수>` 같은 명령이 있으면 한 줄로 끝난다.
- (도구 메모) `llvm-pdbutil dump -publics` 의 `addr = 0003:1360` 은 **섹션:십진수 오프셋**이다. rva = 섹션 VA + 십진수를 16진으로.
- (디버거 메모, 다음 동적용) csrss 는 보호 프로세스다. `basesrv!` 멈춤점은 csrss 프로세스 문맥이 필요하다 (Q19 (e)).
