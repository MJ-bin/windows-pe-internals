# 29 parent-leftovers — 정적 분석 결과

- 요청서: `_workspace/sast/29_parent-leftovers.md` (Q41, Q65 의 bit 9 부분 포함)
- 날짜: 2026-10-24
- 모듈 / 바이너리 (`ms/bin/.../System32/`):
  - 이미 분석돼 있던 것 (다시 안 걸음): kernel32, KernelBase, ntdll, ntoskrnl (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 셋 다 있음, 돌고 있는 Ghidra 없음).
  - **이번에 처음 analyze**: `basesrv.dll` (7초, 함수 156개, `SetPdb:` OK), `csrsrv.dll` (함수 129개, `SetPdb:` OK). PDB guid: basesrv `4B0951C4FAD4CCBDDFDAA284D1ED2C2E1`, csrsrv `32C5AA2B9D0AF671C35069A02C5A0C041`. `ms/sym/` 에 `.pdb`·`.aliases.tsv`(basesrv 0무리, csrsrv 2무리).
  - 덤으로 받은 PDB: `cmd.exe` → `ms/sym/cmd.exe.pdb` (guid `6E820A0DD74C2CA6E23F25DEBA1651261`). 함수 이름을 붙이려고만 썼다 (Ghidra 로는 안 걸었다).
  - ntoskrnl PDB guid 는 기준과 같다 (`C29EBFB06B78B3C020DCA66D99713F9E1`, `ms/sym/ntoskrnl.exe.pdb.info.txt`).
- 뽑은 함수 (`ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`, 전부 depth 0 아니면 아래 표시):
  - kernel32: `CreateProcessW` (depth 1)
  - ntdll: `NtCreateUserProcess`, `NtResumeThread`, `CsrClientCallServer` (ApiNumber 를 메시지에 쓰는 줄 확인용)
  - ntoskrnl: `PspValidateCreateProcessProtection`, `PspImplicitAssignProcessToJob` (ProcessFlags 를 받는 다른 곳 확인용). `NtCreateUserProcess`·`PspAllocateProcess`·`PspInsertProcess`·`PspProcessClose` 는 이미 있었다.
  - basesrv: `ServerDllInitialization`, `BaseSrvCreateProcess2`, `BaseSrvCreateProcess`, `InternalBaseSrvCreateProcess`, `BaseSrvDeferredCreateProcess`, `BaseSrvCreateActivationContext(2)` (depth 1 → 18개)
  - csrsrv: `CsrApiRequestThread`, `CsrLoadServerDll`, `CsrCallServerFromServer`, `CsrpHandleInlineUserConnect`, `CsrParseServerCommandLine`, `CsrCreateProcess`, `CsrpCreateProcess`
  - `refs`: basesrv `sxsFunctions`, `UserNotifyProcessCreate` (`_refs/`)
- 버전 확인: **일치.** `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess`, ntdll `LdrpInitializeProcess` call `+0x1bb6` / ret **`+0x1bbb`** → `LdrpInitializeGraphRecurse` (`_calls.tsv`). basesrv·csrsrv 는 "알려진 사실" 표에 없다. 대신 커널 쪽 `KiServiceTable` 489칸이 전부 `_index.tsv` 함수 시작과 맞았고 (질문 3), kernelbase 의 run14 줄(`+0x7f6`, `+0x800`, `+0x30e7`)을 asm 으로 다시 확인했다.
- 임시 도구 (스크립트 폴더에 두고 하네스는 안 고침): PE 표 읽기·`pefile`·`objdump`·`llvm-pdbutil` 로 읽은 것은 `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/322bebe5-1eb2-4020-bd5c-ae4ffd583b6a/scratchpad/` 에 둠 (`pelib2.py`, `basesrv_publics.txt`, `cmd_publics.txt`, `cmd_fn.asm`, `cmd_fn2.asm`).

## 스크립트 문제

고칠 버그는 없다. 불편·관찰 3건.

- **`run_ghidra.sh refs` 에 import 이름을 주면 엉뚱한 답이 나온다.** `refs KernelBase.dll CsrClientCallServer` 가 외부 주소(`rva=0xfffffffe8000011f`)로 풀려서 `ptr` 33줄(관계 없는 표)만 나왔다 (`RefsTo.java`). 이름이 import(외부 심볼)면 "import 라서 못 한다 — IAT 칸을 주소로 주세요" 로 멈추거나, import 이름이면 IAT 칸 rva 를 찾아 주면 좋겠다. 이번엔 `pefile` 로 IAT 칸을 찾아 `FF 15` 바이트 스캔으로 했다. (잘못 생긴 `_refs/CsrClientCallServer.tsv` 는 지웠다.)
- **"함수 포인터 표 안에 뭐가 들었나" 를 읽는 명령이 없다.** `refs` 는 "누가 이 주소를 가리키나" 만 한다. sast/24 (`KiServiceTable`)와 이번(`BaseServerApiDispatchTable`) 둘 다 `pefile` 임시 스크립트로 읽었다. `run_ghidra.sh table <바이너리> <이름|0xRVA> <개수> [항목크기] [rva|abs]` 같은 명령(칸마다 `_index.tsv` 함수 이름으로 풀어 줌)이 있으면 표 질문이 한 줄로 끝난다. 제안만.
- **analyze 로그의 `WindowsResourceReference` 경고는 새 문제가 아니다.** basesrv·csrsrv 로그에도 `GhidraScriptLoadException ... osgi.ee=UNKNOWN` 가 나오지만 7개 모듈 로그 전부에 있고 분석은 성공(`SetPdb:`·`ExportIndex` OK)이다. `~/.config/ghidra/ghidra_11.3.2_PUBLIC/osgi/compiled-bundles/93cb37dd` 가 9월 6일자 옛 캐시라서 그 하나만 안 지워진 것으로 보인다 `[추측]`. 영향은 "Windows 리소스 참조" 분석기 하나뿐이라 지금은 둔다. 지울지는 사용자 결정.
- 참고(도구 문제 아님): `llvm-pdbutil dump -publics` 의 `addr = 0003:1360` 은 **섹션:십진수 오프셋**이다. rva 는 `섹션 VA + 십진수→16진`. (예: basesrv `.rdata` VA `0xf000` + 1360(`0x550`) = `0xf550`)

## 한눈에

1. `kernel32!CreateProcessW` 는 **껍데기**: 인자 열 개를 그대로 넘기고 KERNELBASE `CreateProcessW` 를 한 번 부른다 (call `+0x4d`, ret `+0x54`). 다른 일 없음. → 그림 1 `kb1` 앞에 kernel32 상자를 **넣을 수 있다.**
2. `0x1001d` = 서버 번호 `1` (위 16비트) + API 번호 `0x1d` (아래 16비트). `0x1d` 칸 = **`basesrv!BaseSrvCreateProcess2`** (csrss 에 새 프로세스 등록 + SxS 호출 + winsrv 알림). 서버 번호 `1` = basesrv 는 코드에 박혀 있지 않고 csrss 명령줄 값이라 **정적으로는 "맞아떨어짐"까지**, 확정은 동적.
3. 두 스텁의 번호는 `NtCreateUserProcess` = `0xd1`, `NtResumeThread` = `0x52`, `KiServiceTable` 같은 칸이 **맞다.** (바로 SYSCALL 하나가 아니라 `INT 0x2e` 대체 길이 붙어 있다.)
4. `[RSP+0x90]` 는 `PspAllocateProcess` 의 **9번째 인자 = `NtCreateUserProcess` 의 `ProcessFlags`** 사본이다. **같은 비트다.** `CREATE_SUSPENDED` 없이 만들면 `OwnerProcessId` = 부모 PID 그대로 (`|2` 없음). **Q65 의 bit 9 부분은 풀렸다.**

## 질문별 답 [정적]

### 1. `kernel32!CreateProcessW` → `KERNELBASE!CreateProcessW`

- 답 `[정적]`: **예, 그대로 넘기는 껍데기다.**
  - kernel32 `CreateProcessW` (rva `0x3bdd0`, 94B, 진짜 함수. export 도 forwarder 아님) 는 `+0x4d` 에서 `CALL [0x18008baa8]` 한 번. ret **`+0x54`**.
  - 그 IAT 칸 (rva `0x8baa8`) 이름 = **`api-ms-win-core-processthreads-l1-1-0.dll` 의 `CreateProcessW`** (힌트 3). API-set 이름이다.
  - 인자: 앞 네 개(RCX·RDX·R8·R9)는 안 건드린다. 5~10번째(스택)는 새 스택 자리로 한 칸씩 복사해서 넘긴다: `+0x7`~`+0x49` 가 `[RSP+0xa8]`→`[R11-0x10]`, `0xa0`→`-0x18`, `0x98`→`-0x20`, `0x90`→`-0x28`, `0x88`→`[RSP+0x28]`, `0x80`→`[RSP+0x20]`. 값을 바꾸는 줄 없음.
  - 다른 일(검사·ETW 등): **없다.** 명령 18개에 호출은 이 하나뿐 (`_calls.tsv`: `CreateProcessW +0x4d +0x54 CreateProcessW`). 돌아와서도 `NOP; ADD RSP; RET` 로 EAX 를 그대로 돌려준다.
- API-set 이 어디로 풀리나 `[정적]`: `ms/bin` 의 `apisetschema.dll` (`.apiset` 섹션, 버전 6 표) 에서 `api-ms-win-core-processthreads-l1-1-*` 항목 값이 둘이다: 기본 = `kernel32.dll`, **가져오는 쪽이 `kernel32.dll` 이면 `kernelbase.dll`.** 그래서 kernel32 안의 이 IAT 칸은 `KERNELBASE!CreateProcessW` (export rva `0x7d5f0`, 107B) 로 간다. (실제 풀이는 실행 때 ntdll 이 PEB 의 ApiSet 표로 한다. 이 파일은 같은 OS 의 표.)
- 부모(cmd.exe) 쪽 `[정적]` (cmd.exe 는 `ms/bin` 에 있다):
  - cmd.exe import 표에 **`CreateProcessW`·`CreateProcessAsUserW`·`ResumeThread`** 가 모두 `api-ms-win-core-processthreads-l1-1-0.dll` 이름으로 있다 (IAT rva `0x3bf10`·`0x3bf08`·`0x3bee8`). cmd.exe 는 이 표에서 기본 호스트 `kernel32.dll` 로 풀린다. 그러니 **cmd.exe → `kernel32!CreateProcessW` → `KERNELBASE!CreateProcessW` 길**이 코드에 있다.
  - 그 IAT 칸을 부르는 곳 4곳 (PDB `cmd.exe.pdb` 이름, `objdump` 로 읽음):
    - `cmd!ExecPgm` (rva `0x23720`, 3083B): `CreateProcessAsUserW` call `+0x514` (ret `+0x51b`), **`CreateProcessW` call `+0x595` (ret `+0x59c`)**. 두 곳 모두 `dwCreationFlags` 자리에 상수 **`0x80000` (`EXTENDED_STARTUPINFO_PRESENT`) 만** 싣는다 (`+0x57d` 의 `MOV [RSP+0x28],0x80000`). **`CREATE_SUSPENDED`(4) 없음.** 일반 외부 명령 실행 함수로 보인다 `[추측: 이름 뜻]`.
    - `cmd!Start` (rva `0x20c54`, 4520B, `start` 명령 `[추측: 이름 뜻]`): `CreateProcessW` call `+0xe4b` (ret `+0xe52`), `ResumeThread` call `+0xf9b` (ret `+0xfa2`). 플래그 변수는 시작이 `0x80010`, 조건 하나에서 `|= 4` 와 "나중에 `ResumeThread`" 표시 (`+0x3fb` 부근, 16진수를 읽는 옵션 뒤 `[추측: /AFFINITY 같은 마스크]`), 호출 직전 `|= 0x400`.
  - 한계: 이 길을 test.exe 때 **실제로 타는지**, `ExecPgm` 안에서 어느 쪽(AsUser/일반)이 도는지는 정적으로 모른다.
- flow.md 판정:
  - **그림 1 `kb1` 앞에 `kernel32!CreateProcessW` 상자를 넣어도 된다.** 간선 `k32 -->|+0x54| kb1`. 상자 글 예: "껍데기. 인자 열 개 그대로 넘김, 다른 일 없음 (정적)".
  - `kb1` 글 "(정적: 부모가 이 입구로 오는지는 추측)" → "(정적: cmd.exe import 표에 CreateProcessW 있음. cmd.exe 가 test.exe 때 이 칸을 부르는지는 동적 Q19)" 로 바꿀 수 있다.
  - 2.2 부모 쪽 절(`flow.md` 1327·1384줄): `[추측]` 한 줄을 "`[정적]` cmd.exe 는 import 표로 `kernel32!CreateProcessW` 에 닿고, 그 껍데기가 KERNELBASE 를 부른다. cmd.exe 쪽 부르는 코드 `ExecPgm +0x59c`·`Start +0xe52`. 실제로 타는지는 동적" 으로. 뒤의 `[공백]` 은 "코드는 봄, 실행 확인은 Q19" 로 줄인다.
  - cmd.exe 상자(`ExecPgm`)는 넣을지 오케스트레이터가 정한다. cmd.exe 는 Ghidra 가 아니라 `objdump` 로 읽었다는 점만 밝히면 된다.
- 근거: `ms/disassemble/kernel32/CreateProcessW.asm` (+0x0~+0x5d, `+0x4d` 줄), `ms/disassemble/kernel32/_calls.tsv`, `ms/decomp/kernel32/CreateProcessW.c` (자기 이름 호출 한 번, 같은 인자). IAT·API-set·cmd.exe 는 `pefile` 로 PE 표를 읽음 (임시 스크립트).

### 2. ApiNumber `0x1001d` 의 정체

- 답 `[정적]`: **위 16비트 = 서버 DLL 번호 `1`, 아래 16비트 = 그 서버의 API 번호 `0x1d`.** `0x1d` 칸 = **`basesrv!BaseSrvCreateProcess2`** (PDB 이름. basesrv rva `0x6ee0`, 815B).
- csrss 쪽이 번호를 나누는 줄 (`csrsrv!CsrApiRequestThread`, rva `0x9340`):
  - `+0x483` 메시지의 ApiNumber (`[RSP+0xe0]` = 메시지 시작 `+0x30`) 를 `SHR 0x10` → 서버 번호. `+0x487` **6 미만**이어야 하고, `+0x493` 전역 `CsrLoadedServerDll[번호]` (칸 8바이트) 를 꺼냄. 비었으면 오류 로그로.
  - `+0x49c` 아래 16비트 (`MOVZX`) − 서버 구조체 `[+0x20]` (API 시작 번호) 가 `[+0x24]`(최대 번호) − `[+0x20]` 보다 작아야 하고, `+0x4c4`·`+0x4c8` (캡처 버퍼 있는 갈래는 `+0x634`·`+0x638`) 에서 `[+0x28]` (처리 표) 의 그 칸을 꺼내 `+0x4e1` (캡처 있는 갈래 `+0x651`, 간접 호출 `indirect:cfg:[RAX+RSI*8]`) 로 부른다.
  - ntdll 쪽에서 메시지에 쓰는 줄: `CsrClientCallServer` (rva `0xdbdd0`) `+0x27` 이 ApiNumber 의 **bit 28 (`0x10000000`)을 지우고** `+0x51` `MOV [RBX+0x30],R8D`. kernel32 의 base API 호출자 일부가 이 bit 28 을 켜서 부르기도 한다 (아래). `0x1001d` 에는 없다.
- basesrv 가 서버 구조체를 채우는 줄 (`basesrv!ServerDllInitialization` rva `0x2f50`):
  - `+0x153`~`+0x15a` `[+0x28]` ← `BaseServerApiDispatchTable` (rva `0xf550`), `+0x15e`~`+0x165` `[+0x30]` ← `BaseServerApiServerValidTable` (rva `0x10438`), `+0x149` `[+0x20]` ← 0, `+0x17f` `[+0x24]` ← **`0x1f`**, `+0x186` `[+0x40]` ← 8 (프로세스당 데이터 크기로 보임 `[추측]`), `+0x170`·`+0x17b` `[+0x48]`·`[+0x50]` ← `BaseClientConnectRoutine`·`BaseClientDisconnectRoutine`. 칸 이름은 공개 문서 `CSR_SERVER_DLL` 모양과 맞는 `[추측]`, 오프셋·값은 `[정적]`.
  - 그래서 칸은 `0 ~ 0x1e` 31개. `0x1d` 는 안이다.
- `BaseServerApiDispatchTable` 의 `0x1d` 번째 칸 = rva `0xf638` → `BaseSrvCreateProcess2` (rva `0x6ee0`). 앞뒤: `[0x1c]` `BaseSrvNlsUpdateCacheCount`, `[0x1e]` `BaseSrvCreateActivationContext2`. `[0]` = `BaseSrvCreateProcess` (옛 V1 메시지를 `BaseSrvConvertV1CreateProcessToV2` 로 바꿔 `BaseSrvCreateProcess2` 를 부름: `BaseSrvCreateProcess +0xb4`/ret `+0xb9`, `+0xc1`/ret `+0xc6`). `[1~4]`·`[0x18]` = `BaseSrvDeadEntry` (ValidTable 값 0), 나머지 ValidTable 값 1 (`[0x1d]` = 1).
- `BaseSrvCreateProcess2` 가 하는 일 (대표만):
  1. 메시지 안 CLIENT_ID (메시지 `+0x50`) 로 `NtOpenProcess` (`+0xac`, ret `+0xb3`) · `NtOpenThread` (`+0x1b3`, ret `+0x1ba`) — csrss 가 새 프로세스·스레드 핸들을 직접 연다. 중간에 `NtQueryInformationProcess` 셋 (`+0xe6`·`+0x13d`·`+0x16e`) 으로 부모·자식 관계·값 비교 검사 (실패하면 `0xc0000022`).
  2. `InternalBaseSrvCreateProcess` (`+0x216`, ret `+0x21b`) 를 부른다. 이 안에서 차례로:
     - **SxS (side-by-side) 호출**: 전역 `sxsFunctions` (`BaseSrvRegisterSxS` 가 채움, **sxssrv.dll 이 이 함수를 import** 한다) 가 있고 `param_1 == 0`, 메시지 `[+0x5c] & 0x2000 == 0` 등 조건이면 그 표의 `+0x8` 칸을 간접 호출 (`InternalBaseSrvCreateProcess +0xf7`~`+0xfb`, ret `+0x100`).
     - **`CsrCreateProcess`** (`+0x175`, ret `+0x17c`) — 새 프로세스를 csrss 의 프로세스 목록에 넣는다 (아래).
     - 성공하면 전역 `UserNotifyProcessCreate` (`BaseSetProcessCreateNotify` 가 채움, **winsrvext.dll 이 import**) 가 있으면 간접 호출 (`+0x1b7`, ret `+0x1bc`).
  3. 마지막에 `BaseSrvUpdateVDMSequenceNumber` (`+0x26b`) 는 VDM 인자가 있을 때만.
- `csrsrv!CsrCreateProcess` → `CsrpCreateProcess` (call `+0x18`/ret `+0x1d`) 가 하는 일: `CsrAllocateProcess` (`+0x9e`, 프로세스 구조 만들기) → 토큰 읽기 (`NtOpenProcessToken`·`NtQueryInformationToken`) → `CsrSetExceptionPort` (`+0x34d`) → `CsrInsertProcess` (`+0x507`, ret `+0x50c`, 프로세스 표에 넣기) → `CsrCreateThread` (`+0x556`, 첫 스레드도 등록). 즉 **"csrss 에 새 프로세스 등록"** 이 맞다.
- 서버 번호 `1` = basesrv 인가:
  - **코드에 `1` 이 박혀 있지 않다.** 번호는 csrss 가 받은 명령줄의 `ServerDLL=이름[:함수],번호` 에서 온다: `csrsrv!CsrParseServerCommandLine` (rva `0x30a0`) `+0x50e`·`+0x562` 에서 `_stricmp("ServerDLL")` 후 `:` 로 이름·함수를 나누고 `,` 뒤 숫자를 `RtlCharToInteger(…,10)` 로 읽어 `CsrLoadServerDll(이름, 함수, 번호)` 를 부른다 (`+0x588`, ret `+0x58d`). 번호 `0` 은 `+0x4f7` 에서 이름 `"CSRSS"` 로 부르는 csrsrv 자신 (`CsrLoadServerDll +0xc5` `TEST EBX; JZ` 로 `LdrLoadDll` 을 건너뛰고 `+0x36c` 에서 csrsrv 안 `CsrServerDllInitialization` 을 씀). 번호는 6 미만 (`+0x45`), 이미 찬 칸이면 실패(`+0x5c`).
  - 이 명령줄 문자열은 smss 가 레지스트리 `SubSystems\Windows` 값에서 읽어 만든다 `[추측: 일반 지식, ms/bin 에 없음]`. 정적으론 못 본다.
  - **간접 증거 `[정적]`**: kernel32·KernelBase 의 base API 호출자들이 `CsrClientCallServer` 에 싣는 번호의 **아래 16비트가 basesrv 표 칸 이름과 전부 맞고 위 16비트는 모두 `1`** 이다 (바이트 스캔, `CsrClientCallServer` 호출 앞 `41 B8 imm32`): `CsrBasepDefineDosDevice` `0x10014` ↔ `[0x14]` `BaseSrvDefineDosDevice`; `CsrBasepSetProcessShutdownParam` `0x1000c` ↔ `[0xc]` `BaseSrvSetProcessShutdownParam`; `GetProcessShutdownParameters` `0x1000d` ↔ `[0xd]` `BaseSrvGetProcessShutdownParam`; `CsrBasepNlsUpdateCacheCount` `0x1001c` ↔ `[0x1c]` `BaseSrvNlsUpdateCacheCount`; `CsrBasepSetClientTimeZoneInformation` `0x10016` ↔ `[0x16]` `BaseSrvSetTermsrvClientTimeZone`; kernel32 `NotifySoundSentry` `0x10012` ↔ `[0x12]` `BaseSrvSoundSentryNotification`; `SetTermsrvAppInstallMode` `0x10015` ↔ `[0x15]`; `CsrBasepCreateActCtxCommon` `0x1001001e` (bit 28 은 위 `CsrClientCallServer +0x27` 이 지움 → `0x1001e`) ↔ `[0x1e]` `BaseSrvCreateActivationContext2`; `GetNextVDMCommand` `0x10010007` ↔ `[7]`; `RegisterWowExec` `0x10010011` ↔ `[0x11]`. 그래서 `0x1001d` 의 `1` 도 basesrv 로 보는 게 맞다. (확정은 csrss 의 `CsrLoadedServerDll[1]` 이름 칸을 보는 동적, 아래.)
- 근거: `ms/disassemble/csrsrv/CsrApiRequestThread.asm` (+0x47f~+0x4e6, +0x588~+0x656), `CsrLoadServerDll.asm`, `CsrParseServerCommandLine.asm`, `CsrCreateProcess`·`CsrpCreateProcess` 의 `_calls.tsv`; `ms/disassemble/basesrv/ServerDllInitialization.asm` (+0x149~+0x186), `BaseSrvCreateProcess2.asm`, `InternalBaseSrvCreateProcess.asm`, `_refs/sxsFunctions.tsv`, `_refs/UserNotifyProcessCreate.tsv`; 표는 `pefile` 로 basesrv.dll rva `0xf550` 읽음 (31칸, 뒤는 다른 데이터).

### 3. ntdll 스텁 둘과 `KiServiceTable`

- 답 `[정적]`: **둘 다 맞다.**

| ntdll 스텁 | rva / 크기 | `MOV EAX,` | `KiServiceTable[번호]` (rva) | 그 칸이 가리키는 함수 | 앞뒤 칸 |
|---|---|---|---|---|---|
| `NtCreateUserProcess` | `0x1627c0` / 24B | **`0xd1`** (`+0x3`) | `0xa01340` | `nt!NtCreateUserProcess` | `[0xd0]` NtCreateTransactionManager, `[0xd2]` NtCreateWaitCompletionPacket |
| `NtResumeThread` | `0x1617f0` / 24B | **`0x52`** (`+0x3`) | `0xa2d600` | `nt!NtResumeThread` | `[0x51]` NtQuerySection, `[0x53]` NtTerminateThread |

- 읽는 법 (sast/24 와 같음): `KiServiceTable` rva `0xdadc0` (PDB 공개 이름, 4바이트 칸, 부호 있는 rva), `KiServiceLimit` (rva `0xdb564`) 값 = **489**. 489칸이 전부 `ntoskrnl/_index.tsv` 의 함수 시작과 맞았다 (489/489). 위 `NtCreateUserProcess`·`NtResumeThread` 칸 값은 `_index.tsv` 의 두 함수 rva 와 같다.
- 스텁 모양은 둘이 같다: `+0x0 MOV R10,RCX`, `+0x3 MOV EAX,번호`, `+0x8 TEST byte [0x7ffe0308],1`, `+0x10 JNZ +0x15`, **`+0x12 SYSCALL`**, `+0x14 RET`, `+0x15 INT 0x2e`, `+0x17 RET`. 그러니 "바로 SYSCALL 하나" 에 **`0x7ffe0308` (공유 사용자 데이터 `+0x308`, 이름은 `SystemCall` 칸으로 보임 `[추측]`) 비트0 이 켜져 있으면 `INT 0x2e` 로 가는 길**이 붙어 있다. 어느 쪽을 타는지는 값에 달려서 동적 (보통 SYSCALL 일 것 `[추측]`). ntdll 안에서 이 스텁이 커널로 넘어가는 자리가 `+0x12`, 유저 쪽 호출자로 돌아가는 건 `+0x14`.
- flow.md: `nd1`·(`NtResumeThread` 쪽 `nd`) 글의 "시스템 콜 스텁 추측" → "`[정적]` 번호 `0xd1`/`0x52` 가 `KiServiceTable` 같은 칸의 `nt!NtCreateUserProcess`/`nt!NtResumeThread`. `INT 0x2e` 대체 길 있음" 으로 바꿀 수 있다.
- 근거: `ms/disassemble/ntdll/NtCreateUserProcess.asm`, `NtResumeThread.asm`; `ntoskrnl.exe` 표는 `pefile` 로 읽음 (rva `0xdadc0`, 489칸).

### 4. ProcessFlags bit 9 (`0x200`)

- 답 `[정적]`: **예. `[RSP+0x90]` bit 9 는 이 비트다.** 흐름:
  1. KERNELBASE `CreateProcessInternalW`: `+0x7f6` `TEST [RSP+0x108],R15B(4)` → 4 가 **없으면** `+0x800` `BTS [RSP+0x110],9`. `+0x306b` `MOV EAX,[RSP+0x110]` → `+0x30b3` `MOV [RSP+0x30],EAX` (7번째 인자) → call `+0x30e7`, ret `+0x30ee` `NtCreateUserProcess` (run14 와 같음, 다시 확인).
  2. 커널 `NtCreateUserProcess`: 7번째 인자 = 스택 `[RBP+0x7d0]` (`RBP = RSP+0x80`, 프롤로그 8푸시+`SUB 0x7d8`; 5번째 `[RBP+0x7c0]`, 6번째 `0x7c8`, ThreadFlags `[RBP+0x7d8]`).
     - **검사 마스크** `+0x1b0` `MOV EBX,[RBP+0x7d0]` → `+0x1b6` `TEST EBX,0xff317838` + `+0x1bf` `TEST [RBP+0x7d8],0xfffffffe` → 마스크에 걸리면 `+0x1ce` `JZ` 로 `0xc000000d` (`STATUS_INVALID_PARAMETER`). **bit 9 는 마스크에 없다** (마스크 비트: 3·4·5·11·12·13·14·16·20·21·24~31). 즉 호출자가 마음대로 켤 수 있는 비트.
     - `+0x1d4` `AND 0x8400` 이 둘 다 켜지면 `0xc0000030` — bit 9 와 무관.
     - 저장 칸: 따로 안 옮기고 같은 스택 칸 `[RBP+0x7d0]` 을 계속 쓴다. 쓰는 줄은 `+0x2cd` (bit 6 지운 값 되쓰기), `+0xdea` (`BTS … ,0xa`, **PspAllocateProcess 에 넘긴 뒤**) 둘뿐. bit 9 를 건드리는 줄 없음.
     - 이 함수 안에서 bit 9 를 직접 검사하는 줄 없음 (`TEST BL,0x40`·`TEST BL,4`·`BT EBX,0x16`·`>>6 & 1` 만 있고 bit 9 안 나옴).
  3. `+0xbf6` `MOV EAX,[RBP+0x7d0]` → `+0xbfc` `MOV [RSP+0x40],EAX` → **`PspAllocateProcess` 9번째 인자** (call `+0xc31`, ret `+0xc36`).
  4. `PspAllocateProcess`: 프롤로그 7푸시 + `SUB 0x830` 라서 9번째 인자는 `[RSP+0x8b0]`. `+0x2ba` `MOV ECX,[RSP+0x8b0]` → `+0x2c1` **`MOV [RSP+0x90],ECX`** (여기서 `[RSP+0x90]` 이 생긴다). `+0x625` `R8D ← [RSP+0x90]` (또는 `+0x668`~`+0x670` 에서 `[RSP+0x8b0]` 로 다시 읽어 `[RSP+0x90]` 에 되씀 — 같은 값). 둘 사이에 `[RSP+0x90]`·`[RSP+0x8b0]`·R8 을 바꾸는 줄이 없다.
  5. `+0x6f1`~`+0x710`: `RDX = [[RSP+0xe8]+0x1d0]` (PID, sast/28 에서 부모), `+0x703` `OR RAX,2`, **`+0x707` `BT R8D,9`**, `+0x70c` `CMOVC RAX,RDX` (켜졌으면 `|2` 버림), `+0x710` `MOV [R14+0x2d8],RAX` (`OwnerProcessId`). → **bit 9 켜짐 = `OwnerProcessId` = 부모 PID 그대로. 꺼짐 = 부모 PID `|2`.**
- `CREATE_SUSPENDED` 와 짝지으면 `[정적]`:
  - 없음(4 꺼짐) → KERNELBASE 가 bit 9 켬 → `OwnerProcessId` = 부모 PID 그대로 (`|2` 없음).
  - 있음(4 켜짐) → bit 9 안 켬 → `OwnerProcessId` = 부모 PID `|2`.
  - cmd.exe `ExecPgm` 의 플래그는 `0x80000` 상수라 4 가 없다 (질문 1). 그러니 일반 외부 명령으로 만든 test.exe 는 `|2` 없는 쪽이다 `[정적: 코드 상수, 실제 길은 동적]`.
- `PspProcessClose` (run33) 와의 관계:
  - `PspProcessClose` (rva `0x9b3b50`) 조건: `+0x9` `R9 > 1` 이면 → `+0xf` `ProcessFirstResume`(`[RDX+0x5fc]` `0x8000`) 켜졌으면 return; 아니면 `+0x22`~`+0x29` **`[닫는 쪽 +0x1d0] == [대상 +0x2d8](OwnerProcessId)`** 이어야 계속 → `+0x32`~`+0x38` 닫는 쪽 `[+0x1f4] & 0x40000008` → `+0x47` `PsTerminateProcess(0xc000010a)` (ret `+0x4c`).
  - 비교 대상이 `OwnerProcessId` 라서: `|2` 없음(= 부모 PID 그대로) 이면 **부모가 핸들을 닫을 때 "같은 PID" 조건이 맞을 수 있다.** `|2` 가 붙으면(=`CREATE_SUSPENDED`) 프로세스 ID 는 4의 배수라서 `[추측: 일반 지식, 핸들 값 규칙]` 어느 프로세스와도 안 맞는다 → 이 종료 길이 막힌다.
  - 뜻(`[추측]`): "첫 스레드가 한 번 풀리기 전에 부모가 자식 핸들을 닫아 버리면 (예: csrss 등록이 실패해 부모가 정리) 커널이 자식을 끝낸다." `CREATE_SUSPENDED` 로 일부러 멈춘 프로세스는 그 길에서 빼는 표시가 `|2`.
  - test.exe 를 cmd.exe 가 `0x80000` 로 만들면 `ProcessFirstResume` 가 꺼져 있는 동안 (풀기 전, `PsMultiResumeThread +0x4e` 가 켠다 — run33) 부모가 핸들을 닫으면 이 길이 열려 있다. 실제로 서는지는 Q64.
- 이 비트를 읽는 다른 곳 `[정적]`: **`PspAllocateProcess +0x707` 하나뿐으로 보인다.** ProcessFlags 를 받는 다른 함수 셋을 봤다.
  - `PspValidateCreateProcessProtection` (rva `0xaa6098`): 4번째 인자 `R9B` 의 **bit 2 (`TEST R9B,4`)** 만 본다 (`NtCreateUserProcess +0xa4f`/`+0xb95` 에서 부름).
  - `PspInsertProcess` (4번째 인자, `NtCreateUserProcess +0xfaf` 에서 `R9D ← [RBP+0x7d0]`): 인자 사본(`[RBP+0x58]`)에서 **bit 1 (`+0x318` `TEST byte,2`)** 만 검사. 따로 `EDI` 에 둔 사본은 `PspImplicitAssignProcessToJob` 3번째 인자로 간다 (`+0xce`).
  - `PspImplicitAssignProcessToJob` (rva `0x9745fc`): **bit 10** (`+0x35` `BT R14D,0xa`) 과 **bit 0** (`+0x60` `TEST R14B,1`).
  - (`PspAllocateProcess` 안 다른 `BT … ,9` 줄 — `+0x765` `[R15+8]`, `+0x145d` `EBX` — 은 다른 변수의 bit 9.)
- Q65 에 남는 것: **bit 9 부분은 풀렸다.** 남은 둘: `NtSetInformationProcess +0x1ecd` (OwnerProcessId 를 쓰는 다른 곳), `ObCloseHandleTableEntry +0x7a9` 간접 호출 앞에서 `PspProcessClose` 로 넘기는 인자 채움.
- 근거: `ms/disassemble/kernelbase/CreateProcessInternalW.asm` (+0x7f6, +0x800, +0x306b, +0x30b3, +0x30e7), `ms/disassemble/ntoskrnl/NtCreateUserProcess.asm` (+0x1b0, +0x1b6, +0x1bf, +0x1d4, +0x2cd, +0xbf6, +0xbfc, +0xc31, +0xdea, +0xfaf), `PspAllocateProcess.asm` (+0x2ba, +0x2c1, +0x625, +0x668, +0x670, +0x6f1~+0x710), `PspProcessClose.asm`, `PspInsertProcess.asm`, `PspValidateCreateProcessProtection.asm`, `PspImplicitAssignProcessToJob.asm`.

## 동적분석으로 확인해야 할 것

- **dast 요청 필요: Q19 에 붙일 것 (이미 대기 중인 Q19 와 겹침).** 새 번호 안 만들어도 된다.
  - (a) 부모 쪽 길: test.exe 가 만들어질 때 `nt!NtCreateUserProcess` 입구의 유저 쪽 호출 줄 (`k` 로 유저 프레임까지 보일 때): 기대 `KERNELBASE!CreateProcessInternalW+0x30ee` ← `KERNELBASE!CreateProcessW+0x66` ← **`kernel32!CreateProcessW+0x54`** ← `cmd!ExecPgm+0x59c` (또는 AsUser `+0x51b`, `start` 면 `cmd!Start+0xe52`). 이 줄이 맞으면 그림 1 `kb1` 앞 kernel32 상자와 cmd.exe 상자가 `[관찰]` 이 된다.
  - (b) `NtCreateUserProcess` 입구의 7번째 인자 (ProcessFlags): bit 9 (`0x200`) 켜짐, `cmd!ExecPgm` 의 `dwCreationFlags` = `0x80000`. 입구에서 스택 `dwo(@rsp+0x38)` (7번째, 반환 주소 뒤 `+0x28` 이 5번째이므로 `+0x38`) `[추측: 입구 기준 오프셋 계산]`.
  - (c) `nt!PspAllocateProcess+0x710` 직전 `rax`: 부모 PID 그대로 (`|2` 없음). 지난 뒤 `dq <새 EPROCESS>+0x2d8 L1` 도 같은 값.
- **csrss 쪽 (새, 낮은 우선순위)**: csrss 는 보호 프로세스라 커널 디버거에서 그 프로세스 컨텍스트가 필요하다.
  - `basesrv!BaseSrvCreateProcess2` 입구 (basesrv rva `0x6ee0`) 에서 `k`: 기대 `csrsrv!CsrApiRequestThread+0x4e6` 또는 `+0x656` ← … ; 메시지 `poi(@rcx+0x30)` 가 `0x1001d`. test.exe 시동 때 `CsrClientCallServer +0x4672` 와 순서.
  - 서버 번호 `1` = basesrv 확정: csrss 컨텍스트에서 `dq csrsrv!CsrLoadedServerDll L6` → `[1]` 칸 `+0x8` 의 이름 문자열 (`CsrLoadServerDll +0x273`~`+0x28d` 가 이름을 칸 `+0x8` 쪽에 둠) 이 "basesrv". 지나간 일이라 부팅 뒤 아무 때나 된다.
- 정적으로 못 정한 것: cmd.exe 가 실제로 `ExecPgm`(`0x80000`) 로 가는지 `Start` 로 가는지, `INT 0x2e` 대체 길을 타는지 (보통 SYSCALL `[추측]`), 서버 번호 `1` 의 최종 확인.

## 판정

**분석성공.** 네 질문 모두 줄 번호로 답했고, basesrv·csrsrv 는 처음 analyze 해서 PDB 이름이 붙었다 (함수 156/129개). 서버 번호 `1`=basesrv 만 "정적 간접 증거 + 동적 확인 대기"로 남긴다.
