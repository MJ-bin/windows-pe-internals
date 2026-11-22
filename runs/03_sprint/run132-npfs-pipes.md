# 2026-11-22 (작성일, 정적 분석 2026-11-22) run132 — 파이프 (Q241, 정적만): kernelbase `CreateNamedPipeW` · `CreatePipe` · `ConnectNamedPipe` 가 syscall 에 넘기는 값과, npfs 가 만들기 · 열기 · 읽기 · 쓰기 · 듣기 · 닫기에 돌려주는 status. 방향은 **share 하나로** 정하고, 새 인스턴스는 **처음부터 듣는 중** 이라 서버가 `ConnectNamedPipe` 를 안 불러도 클라이언트가 붙는다. 쓰는 끝이 닫힌 뒤 빈 읽기는 **`0xC000014B` (`END_OF_FILE` 아님)**, 이미 붙은 파이프의 듣기는 `0xC00000B2` 라 `ConnectNamedPipe` 는 **FALSE + 535**
(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("파이프 (이름 있는·없는) — kernelbase 가 커널에 무엇을 넘기고, npfs 가 만들기·열기·읽기·쓰기에 무엇을 돌려주나", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/298_internals-npfs-pipes.md`, 튜닝 `298_tune10.md` T7′, 병목 25 · 파이프 만들기 샘플 77) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. `sample/*.c` · `log.h` 에 파이프 API 가 없고 `[소스]` (`grep -i pipe` 0줄), 지도 (flow.md) 에 `NtCreateNamedPipeFile` · 파이프 FSCTL 이 없다. test.exe 의 표준 핸들은 콘솔 (`DeviceType 0x50`, run83 · run84) 이다. 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (npfs PDB 새 줄 · ntkrnlmp · kernelbase · kernel32 · ntdll 줄) · 3장 콘솔 절 한 줄 · run126 절 한 줄 · 3장 끝 새 절 하나 · 4장 (Q241 닫힘 줄 · run132 줄 셋) · 5장 (run132 두 줄, run131 sast 줄의 대기열 번호를 Q242 · Q243 으로 채움) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.
> **대기열**: **Q241** (사용자 결정 2026-11-22 "이 세 요청 먼저 가장 우선"). 결과서 판정 **분석성공** — 요청서 ★ 전부 (1(a)(b)(c) · 2(a)(b)(c) · 3(a)(b)(c)) 와 1(d) · 3(d) 를 `.asm` 줄로 답함. 못 닫은 셋 (CLEANUP 이 오는 핸들 수 조건 · `WaitNamedPipeW` 타임아웃 status · 장치 `Characteristics` 최종값) 은 아래 "아직 확인 필요" (동적 **Q244** · 정적 **Q245**).
> - 이어 쓰는 앞 run: **run126** (sast/124 — IRP 완료: 오류 + PENDING 없음이면 IOSB 안 씀, 동기 핸들 PENDING 은 I/O 관리자가 기다림), **run127** (sast/125 — 파일을 완료 포트에 묶기, 묶인 파일의 패킷 조건), **run84** (`GetFileType` 의 `DeviceType 0x11` → 파이프), **run98** (smss 가 파이프 이름 앞 `SmpCreateProtectedPrefixes +0xab3` 에서 보안을 다는 줄).

## 요약

- **이름 있는 파이프 만들기** `[정적]`: `kernel32!CreateNamedPipeA +0x6a` → `kernelbase!CreateNamedPipeW +0x22e` → `NtCreateNamedPipeFile` (14 인자) ⇒ `nt!NtCreateNamedPipeFile +0x14c IopCreateFile` (`CreateFileType = 1`) ⇒ `IopParseDevice +0xfac` (`MajorFunction = 1`) ⇒ `npfs!NpFsdCreateNamedPipe` → 새 이름 `+0x30b NpCreateNewNamedPipe` / 있는 이름 `+0x3cd NpCreateExistingNamedPipe`.
- **kernelbase 가 넘기는 값** `[정적]`: 방향별 access `0x80100000` (들어옴) / `0x40100000` (나감) / `0xc0100000` (양쪽), share 2 / 1 / 3, disposition = `FILE_FLAG_FIRST_PIPE_INSTANCE` 면 2 아니면 3, options = `OVERLAPPED` 면 0 아니면 `0x20`, 시간은 **늘 포인터** (0 이면 상대 50 ms). 요청 원문의 `[추측]` 셋은 **다 맞다**.
- **이름 없는 파이프 `CreatePipe`** `[정적]`: ① `NtOpenFile(\Device\NamedPipe\)` (처음 한 번만, kernelbase 전역 rva `0x3ad980` 에 두고 **안 닫음**) → ② `NtCreateNamedPipeFile(root = ①, 빈 이름)` = 읽기 핸들 → ③ `NtOpenFile(root = ②, 빈 이름)` = 쓰기 핸들. `nSize` 0 → 쿼터 `0x1000` (두 방향). 상속 표시는 ② ③ 둘 다.
- **npfs 규칙** `[정적]`: 방향은 **share 만** 본다. 이미 있는 이름은 access → 인스턴스 수 (`0xC00000AB`) → disposition 2 (`0xC0000022`) → 방향 (`0xC0000022`) 차례, **종류 · 읽기 모드 · 쿼터 · 시간은 첫 인스턴스와 안 견준다**. 새 인스턴스는 access 에 데이터 권한 비트가 있으면 **처음부터 LISTENING** → 클라이언트 열기가 바로 붙는다.
- **데이터 길** `[정적]`: 빈 읽기 + 블로킹 = `0x103` (큐에 검), NOWAIT = `0xC00000D9`, 상대 닫힘 + 빈 = **`0xC000014B`**. 쓰기: 안 붙음 `0xC00000B3` · 상대 닫힘 `0xC00000B1` · 쿼터 안 `0` + `Information = Length` · 쿼터 넘음 블로킹 = 기다림.
- **듣기 `0x110008`** `[정적]`: 이미 붙음 `0xC00000B2` → kernelbase **FALSE + 535** (참 아님), 아직 안 붙음 + 블로킹 = PENDING (붙으면 0 으로 끝남), 상대가 닫은 뒤 = `0xC00000B1`.
- **`GetFileType`** `[정적]`: 파이프 핸들이면 **ntoskrnl 이 장치 객체 칸으로 직접** `DeviceType 0x11` 을 답한다 (npfs 안 감) → `FILE_TYPE_PIPE` (3).

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔째 바퀴. plan **Q241**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze** 는 npfs 하나 (10초). kernelbase · ntoskrnl · kernel32 · ntdll 은 다시 analyze 안 하고 `^이름$` export 만.

| 모듈 | 바이너리 (`System32\`) | PDB guid+age | 함수 수 |
|---|---|---|---|
| npfs | `drivers\npfs.sys` 120,264 B | **`C433ACB0BA96C41BACA83308CFF250A61`** (이번에 처음 받음, 118,784 B) | 130 |
| kernelbase | `KernelBase.dll` | `72CCE506944DE0267C172AD64287F9BD1` | — |
| ntoskrnl | `ntoskrnl.exe` | `C29EBFB06B78B3C020DCA66D99713F9E1` (기준 빌드와 같음) | — |
| kernel32 · ntdll | (전부터 있음) | 기준 줄과 같음 | — |

- npfs PDB 는 바이너리의 CodeView 줄에서 GUID 를 읽어 받았다 (`ms/sym/npfs.sys.pdb.info.txt`). `ms/ghidra/npfs.analyze.log` 에 `SetPdb:` 줄 있음. 공개 PDB 라 **함수 이름은 다 있고 구조체 타입은 0개** (`pdb_struct.py --list npfs.sys '.'`).
- 버전 대조 (결과서): npfs 는 "알려진 사실" 표에 없어서 바이너리끼리 맞췄다 — ntoskrnl PDB `_NAMED_PIPE_CREATE_PARAMETERS` (0x28 B) 칸 = `NtCreateNamedPipeFile` 이 채우는 칸 = npfs `NpCreateNewNamedPipe` 가 읽는 칸, `_OPEN_PACKET +0x78` · `+0x80` = `IopCreateFile` 이 쓰는 칸, `NpCreateDevice` 의 장치 종류 `0x11` · `.rdata` 문자열 `\Device\NamedPipe` (rva `0x42b8`), 요청 쪽 에뮬레이터 로그 40 샘플의 access · share · disposition 분포가 아래 1(a) 규칙으로 하나도 안 틀림. 에뮬레이터가 진짜 kernelbase 코드를 돌린 것이라 "오프셋 셈 · 바이너리를 믿어도 된다" 는 근거이지, 진짜 Windows `[관찰]` 은 아니다.

## 목적

- PE_emulator 에서 파이프가 두 꼴로 막힌다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님):
  - 이름 있는: `CreateNamedPipeA("\\.\pipe\MSSE-<수>-server", …)` 가 0 → 다른 스레드의 `CreateFileA(같은 이름)` 가 −1 → `Sleep` 뒤 되풀이 (20 / 20).
  - 이름 없는: `CreatePipe` 가 참인데 두 핸들 0 (세 syscall 다 기본 대답). 37 중 26 이 자식 프로세스를 만든다 = 자식 표준 출력 길.
- 요청서 `_workspace/sast/129_npfs-pipes.md` 의 물음:
  1. kernelbase 가 넘기는 것 — `CreateNamedPipeW` 14 인자 옮김표 · `CreatePipe` 세 syscall 의 인자와 root · `ConnectNamedPipe` 의 status → 반환값 (+ `WaitNamedPipeW` 등 넷).
  2. npfs 만들기 · 열기 · 닫기 — 조건별 status · `Information`.
  3. 데이터 길 — 읽기 · 쓰기 · 듣기의 완료 꼴, `GetFileType` 의 장치 종류.
- 쓰는 곳: PE_emulator 의 `nt_create_named_pipe_file` (지금 `named_pipe_filesystem_backend_unsupported`) · 빈 이름 + root 열기 · `\??\pipe\…` 이름 · `0x110008` · 파이프 읽기 / 쓰기. 원칙: 호스트에 진짜 파이프를 만들지 않는다.

## 방법

- 정적 sast/129:
  - npfs: `fetch_pdb.py` 로 PDB 받고 처음 analyze → `DriverEntry` · `NpFsd{Create,CreateNamedPipe,Read,Write,FileSystemControl,Cleanup,Close,QueryInformation,SetInformation,QueryVolumeInformation}` · `NpCommon*` 6 · `NpCreate{NewNamedPipe,ExistingNamedPipe,ClientEnd,NamedPipePrefix}` · `NpListen` · `NpReadDataQueue` · `NpWriteDataQueue` · `NpSet{Closing,Disconnected,Connected}PipeState` · `NpPeek` · `NpWaitForNamedPipe` · `NpFast{Read,Write}` · `NpOpenNamedPipePrefix` 32개 + depth 1 → 85개.
  - kernelbase `^(CreateNamedPipeW|CreatePipe|ConnectNamedPipe|WaitNamedPipeW|PeekNamedPipe|SetNamedPipeHandleState|DisconnectNamedPipe|GetNamedPipeInfo|GetNamedPipeHandleStateW)$` depth 1 → 13개. 있던 `GetFileType` · `ReadFile` · `WriteFile` 읽기만. refs `0x3ad980`.
  - ntoskrnl `^NtCreateNamedPipeFile$` depth 0 + 덤 `NtQueryVolumeInformationFile`. 있던 `IopCreateFile` · `IopParseDevice` · `IopCloseFile` 읽기만.
  - 덤: kernel32 `CreateNamedPipeA`, ntdll `RtlNtStatusToDosErrorNoTeb` · `RtlDefaultNpAcl`.
  - 일회용 읽기 (파일로 안 남김): status → DOS 오류 표 (`RtlpRunTable` rva `0x195b50` · `RtlpStatusTable` rva `0x193a10`), `GetFileType` 점프 표, `.rdata` 문자열 몇 개.
- 결과서: `_workspace/sast/129_npfs-pipes_result.md` (**분석성공**, 스크립트 문제 없음 — npfs `.c` 의 음수 십진 상수 · 깨진 반환값은 `.asm` 으로 다시 셈).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` call · ret 짝 — kernel32 `CreateNamedPipeA`, kernelbase 파이프 API 7개 · `GetFileType` · `ReadFile` · `WriteFile`, ntoskrnl `NtCreateNamedPipeFile`, ntdll `RtlDefaultNpAcl`, npfs `DriverEntry` · `NpCreateDevice` · `NpFsdCreateNamedPipe` · `NpCreateNewNamedPipe` · `NpCreateExistingNamedPipe` · `NpFsdCreate` · `NpCreateClientEnd` · `NpFsdRead` · `NpFsdWrite` · `NpCommonWrite` · `NpFsdFileSystemControl` · `NpCommonFileSystemControl` · `NpFsdCleanup` · `NpCommonCleanup` · `NpSetClosingPipeState` · `NpFsdQueryVolumeInformation` — **결과서와 모두 같음**.
  - `.asm` 줄: `ConnectNamedPipe +0x1b ~ +0xca` (통째), `CreateNamedPipeW +0x23c` · `+0x24c` · `+0x29a` · `+0x29c` · `+0x2a3` · `+0x2b1` · `+0x2fd` · `+0x302` · `+0x304` · `+0x349`, `CreatePipe +0x46` · `+0x6e` · `+0x72` · `+0x88` · `+0x19e ~ +0x1af` · `+0x1d4` · `+0x21f`, `NtCreateNamedPipeFile +0x4b` · `+0x5c` · `+0x62` · `+0x68` · `+0x109` · `+0x10e`, `IopCreateFile +0x2bf ~ +0x2d1`, `IopParseDevice +0xd31` · `+0xd36` · `+0xfa3 ~ +0xfb7`, `IopCloseFile +0x0` · `+0x67` · `+0x10a`, `NtQueryVolumeInformationFile +0x1fe` · `+0x202` · `+0x3d6 ~ +0x3e8` · `+0x41f` · `+0x426` · `+0x442` · `+0x445` · `+0x50b`, npfs `NpListen +0x86 ~ +0x107` · `+0x1d0 ~ +0x1e9`, `NpFsdRead +0x118 ~ +0x124` · `+0x23a` · `+0x28e` · `+0x298` · `+0x2a2` · `+0x34b`, `NpCreateClientEnd +0x162` · `+0x170` · `+0x360`, `NpCreateExistingNamedPipe +0x136 ~ +0x14a` · `+0x244` · `+0x267` · `+0x293`, `NpCreateNewNamedPipe +0x1c5` · `+0x20a` · `+0x2f8` · `+0x2fb`. 결과서와 다른 곳 하나 → 아래 "flow-writer 대조".
  - `_calls.tsv` 의 `alias:` 는 npfs 의 메모리 복사 함수 하나 (`alias:memcpy+1`) 뿐 — 글에서는 이름으로 안 쓴다.
  - refs `ms/disassemble/kernelbase/_refs/0x3ad980.tsv` — 참조 3줄 (`CreatePipe +0x46` 읽기 · `+0x88` 읽기 · `+0x21f` `LOCK CMPXCHG`), 다른 함수 없음.

## 결과

비유:
- 파이프는 **두 방 사이에 놓은 우체통 관** 이다. 관을 놓는 사람 (서버) 이 "이쪽에서만 넣을 수 있어요" 같은 방향을 정하는데, npfs 는 그 방향을 **"누구랑 나눠 쓰나 (share)" 칸 하나** 로만 읽는다 `[추측: 비유]`.
- 관을 놓자마자 **문이 열린 채로 기다리는 중** 이라, 맞은편 사람이 오면 바로 붙는다. "기다릴게요" (`ConnectNamedPipe`) 는 안 불러도 된다. 이미 붙었는데 또 부르면 "벌써 붙었어요" 라는 **오류** 로 답한다 `[추측: 비유]`.
- 한쪽이 관을 떼면, 남은 편지는 꺼낼 수 있고, 다 꺼낸 뒤엔 "관이 깨졌어요" (`BROKEN`) 다 — "끝까지 다 읽었어요" (`END_OF_FILE`) 가 아니다 `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/{kernel32,kernelbase,ntoskrnl,ntdll,npfs}/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `kernel32!CreateNamedPipeA` | `+0x26` | `+0x2b` | `Basep8BitStringToDynamicUnicodeString` (실패면 `+0x9e` −1) | [정적] |
| 〃 | **`+0x6a`** | **`+0x71`** | `CreateNamedPipeW` (뒤 네 인자는 스택 그대로 `+0x2f ~ +0x66`) | [정적] |
| 〃 | `+0x7e` | `+0x85` | `RtlFreeUnicodeString` | [정적] |
| `kernelbase!CreateNamedPipeW` | `+0x80` | `+0x87` | `RtlDosPathNameToNtPathName_U` (`\\.\pipe\X` → `\??\pipe\X`) | [정적] |
| 〃 | `+0xca` | `+0xd1` | `ntdll!RtlDefaultNpAcl` (SD 가 없을 때) | [정적] |
| 〃 | `+0xe9` · `+0x103` | `+0xf0` · `+0x10a` | `RtlCreateSecurityDescriptor` · `RtlSetDaclSecurityDescriptor` | [정적] |
| 〃 | **`+0x22e`** | **`+0x235`** | **`NtCreateNamedPipeFile`** (14 인자) | [정적] |
| 〃 | `+0x2a5` | `+0x2ac` | `RtlSetLastWin32Error` (0, 또는 `+0x2fd` 에서 온 `0xb7`) | [정적] |
| 〃 | `+0x2ba` | `+0x2bf` | `BaseSetLastNTError` (status < 0) | [정적] |
| 〃 | `+0x309` | `+0x310` | `RtlSetLastWin32Error(3)` (이름 바꾸기 실패) | [정적] |
| `ntdll!RtlDefaultNpAcl` | `+0x9e` · `+0xc5` · `+0x375` | `+0xa3` · `+0xca` · `+0x37a` | `NtQueryInformationToken` ×3 (클래스 4 · `0x1d` · `0x1f`) | [정적] |
| `kernelbase!CreatePipe` | `+0x209` | `+0x210` | `NtOpenFile` ① `\Device\NamedPipe\` (전역이 0 일 때만, `+0x46` → `+0x334`) | [정적] |
| 〃 | `+0x232` | `+0x239` | `NtClose` (① 을 다른 스레드가 먼저 넣었을 때 내 것) | [정적] |
| 〃 | **`+0xff`** | **`+0x106`** | **`NtCreateNamedPipeFile`** ② (root = ①) | [정적] |
| 〃 | **`+0x156`** | **`+0x15d`** | **`NtOpenFile`** ③ (root = ②) | [정적] |
| 〃 | `+0x251` | `+0x258` | `NtClose(②)` (③ 실패 때만) | [정적] |
| 〃 | `+0x245` | `+0x24a` | `BaseSetLastNTError` | [정적] |
| `kernelbase!ConnectNamedPipe` | **`+0x63`** | **`+0x6a`** | **`NtFsControlFile(0x110008)`** | [정적] |
| 〃 | `+0x7a` | `+0x7f` | `BaseSetLastNTError` (음수 · 겹침 PENDING) | [정적] |
| 〃 | `+0xaa` | `+0xb1` | `NtWaitForSingleObject(파이프 핸들)` (`lpOverlapped == NULL` 인데 PENDING 일 때) | [정적] |
| `kernelbase!WaitNamedPipeW` | `+0x182` | `+0x189` | `NtOpenFile(\DosDevices\pipe\)` | [정적] |
| 〃 | `+0x269` | `+0x270` | `NtFsControlFile(0x110018)` | [정적] |
| 〃 | `+0x299` | `+0x2a0` | `NtClose` | [정적] |
| `kernelbase!PeekNamedPipe` | `+0xb1` · `+0xfd` · `+0x11e` | `+0xb8` · `+0x104` · `+0x125` | `NtCreateEvent` · `NtFsControlFile(0x11400c)` · `NtWaitForSingleObject` | [정적] |
| `kernelbase!SetNamedPipeHandleState` | `+0x68` | `+0x6f` | `NtSetInformationFile` 클래스 `0x17` | [정적] |
| 〃 | `+0xc9` · `+0xb33da` | `+0xd0` · `+0xb33e1` | `NtQueryInformationFile` · `NtSetInformationFile` 클래스 `0x19` (조각) | [정적] |
| `kernelbase!DisconnectNamedPipe` | `+0x59` · `+0xa1` · `+0xbf` · `+0xd7` | `+0x60` · `+0xa8` · `+0xc6` · `+0xde` | `NtCreateEvent` · `NtFsControlFile(0x110004)` · `NtWaitForSingleObject` · `NtClose` | [정적] |
| `kernelbase!GetFileType` | `+0x72` | `+0x79` | `NtQueryVolumeInformationFile` (클래스 4, 8 B) | [정적] |
| `kernelbase!ReadFile` / `WriteFile` | `+0x86` · `+0x123` / `+0x86` · `+0x113` | `+0x8d` · `+0x12a` / `+0x8d` · `+0x11a` | `NtReadFile` / `NtWriteFile` | [정적] |
| `nt!NtCreateNamedPipeFile` | `+0x6d` | `+0x72` | `ExRaiseDatatypeMisalignment` (유저 모드 · 시간 포인터 `&3`) | [정적] |
| 〃 | **`+0x14c`** | **`+0x151`** | **`IopCreateFile`** (`CreateFileType 1` · 매개변수 포인터) | [정적] |
| `npfs!DriverEntry` (rva `0x18080`) | `+0x149` | `+0x14e` | `NpCreateDevice` → `+0x60/+0x67 IoCreateDevice` (종류 `0x11`, `\Device\NamedPipe`) | [정적] |
| `npfs!NpFsdCreateNamedPipe` (rva `0xeba0`) | `+0xdd` · `+0x195` | `+0xe2` · `+0x19a` | `NpTranslateAlias` · `NpFindPrefix` | [정적] |
| 〃 | `+0x240` | `+0x247` | `SeAccessCheck` (새 이름 + 루트 SD 있을 때) | [정적] |
| 〃 | **`+0x30b`** | **`+0x310`** | **`NpCreateNewNamedPipe`** | [정적] |
| 〃 | **`+0x3cd`** | **`+0x3d2`** | **`NpCreateExistingNamedPipe`** | [정적] |
| `npfs!NpCreateNewNamedPipe` | `+0xcf` · `+0x219` | `+0xd4` · `+0x21e` | `NpCreateFcb` · `NpCreateCcb` | [정적] |
| `npfs!NpCreateExistingNamedPipe` | `+0xa0` · `+0x1c6` | `+0xa7` · `+0x1cb` | `SeAccessCheck` · `NpCreateCcb` | [정적] |
| `npfs!NpFsdCreate` (rva `0xe3c0`) | `+0x1b1` · `+0x230` | `+0x1b6` · `+0x235` | `NpTranslateAlias` · `NpFindPrefix` | [정적] |
| 〃 | **`+0x2f2`** | **`+0x2f7`** | **`NpCreateClientEnd`** | [정적] |
| 〃 | `+0x353` | `+0x35a` | `IofCompleteRequest` (붙어서 풀린 듣기 IRP 들) | [정적] |
| `npfs!NpCreateClientEnd` | `+0xa8` · `+0x2e0` | `+0xaf` · `+0x2e5` | `SeAccessCheck` · `NpSetConnectedPipeState` | [정적] |
| `npfs!NpFsdRead` (rva `0x11d70`) | `+0x27e` · `+0x154` | `+0x283` · `+0x159` | `NpReadDataQueue` (자료 있음) · `NpAddDataQueueEntry` (빈 블로킹) | [정적] |
| 〃 | `+0x20d` | `+0x214` | `IofCompleteRequest` (status ≠ `0x103`) | [정적] |
| `npfs!NpFsdWrite` (rva `0x12540`) | `+0x68` | `+0x6d` | `NpCommonWrite` → `+0x138 NpWriteDataQueue` · `+0x239 NpAddDataQueueEntry` | [정적] |
| `npfs!NpFsdFileSystemControl` (rva `0xf5f0`) | `+0x22` · `+0x45` | `+0x27` · `+0x4c` | `NpCommonFileSystemControl` · `IofCompleteRequest` | [정적] |
| `npfs!NpCommonFileSystemControl` | `+0x1c1` · `+0x130` · `+0x5e` · `+0x6f` | `+0x1c6` · `+0x135` · `+0x63` · `+0x74` | `NpListen` (`0x110008`) · `NpSetDisconnectedPipeState` (`0x110004`) · `NpWaitForNamedPipe` (`0x110018`) · `NpPeek` (`0x11400c`) | [정적] |
| `npfs!NpFsdCleanup` (rva `0xf040`) | `+0x22` | `+0x27` | `NpCommonCleanup` → `+0x193 NpSetClosingPipeState` · `+0x33a NpUnlinkCcb` · `+0x353 NpUnlinkFcb` | [정적] |
| `npfs!NpFsdQueryVolumeInformation` (rva `0x16670`) | `+0x10` | `+0x15` | `NpCommonQueryVolumeInformation` (클래스 4 는 이 길로 안 옴 — 3(d)) | [정적] |

### 1. kernelbase 가 넘기는 것 [정적]

#### 1(a). `CreateNamedPipeW` → `NtCreateNamedPipeFile` 14 인자

**부르기 전 거름** (어느 것이든 `-1` 반환):
- `nMaxInstances` 가 0 이거나 255 초과 → `0xC000000D` (`+0x47 ~ +0x60`).
- 이름 바꾸기 실패 → `SetLastError(3)` (`+0x304` · `+0x309`).
- `dwOpenMode` 허용 밖 비트 · `dwPipeMode & ~0xf` · 방향 비트 0 → `0xC000000D` (`+0x162 ~ +0x1a1`).
- SD 를 안 주면 `RtlDefaultNpAcl` 로 기본 DACL 서술자를 만들어 단다 → **OA.SecurityDescriptor 는 거의 늘 NULL 아님** (`+0xc5 ~ +0x117`). `RtlDefaultNpAcl` 은 토큰을 세 번 묻는다 (`NtQueryInformationToken` 클래스 4 · `0x1d` · `0x1f`) — 에뮬레이터 로그에 이 호출이 `NtCreateNamedPipeFile` 앞에 나와야 한다.

| # | 인자 | 값 · 규칙 | 줄 |
|---|---|---|---|
| 2 | `DesiredAccess` | 방향 기본값 (들어옴 `0x80100000` · 나감 `0x40100000` · 양쪽 `0xc0100000`) \| (`dwOpenMode & 0x010c0000`) | `+0x1a7` · `+0x2e1` · `+0x339` · `+0x1ef` |
| 3 | `ObjectAttributes` | `Length 0x30`, Root 0, 이름 `\??\pipe\X`, Attributes `0x40` (상속이면 `0x42`), SD = 준 것 또는 기본 | `+0xa1 ~ +0xb7` · `+0x119 ~ +0x12f` |
| 5 | `ShareAccess` | 들어옴 **2** · 나감 **1** · 양쪽 **3** | `+0x183` · `+0x2e6` · `+0x33e` |
| 6 | `CreateDisposition` | `FILE_FLAG_FIRST_PIPE_INSTANCE` (`0x80000`) 면 **2**, 아니면 **3** | `+0x1d4 ~ +0x1e8` |
| 7 | `CreateOptions` | `OVERLAPPED` 없으면 **`0x20`**, 있으면 0. `WRITE_THROUGH` 면 +2 | `+0x137 ~ +0x14d` |
| 8 | `NamedPipeType` | 비트 0 = `PIPE_TYPE_MESSAGE`, 비트 1 = `PIPE_REJECT_REMOTE_CLIENTS` | `+0x1b1 ~ +0x1d1` |
| 9 · 10 | `ReadMode` · `CompletionMode` | `(dwPipeMode >> 1) & 1` · `dwPipeMode & 1` | `+0x1ba` · `+0x1c6` |
| 11 | `MaximumInstances` | 255 → `0xffffffff`, 아니면 그 수 | `+0x66 ~ +0x79` |
| 12 · 13 | `InboundQuota` · `OutboundQuota` | `nInBufferSize` · `nOutBufferSize` **그대로** (0 이어도) | `+0x1f5` · `+0x1e5` |
| 14 | `DefaultTimeout` | **늘 포인터**. 0 → `-500000` (상대 50 ms), 아니면 `ms × −10000` | `+0x150 ~ +0x168` · `+0x2f1` |

- 1 = 핸들 칸 `[RSP+0x78]`, 4 = IOSB `[RBP-0x30]`.
- **돌아온 뒤**: status ≥ 0 이고 `Information == 1` (이미 있던 이름) → 핸들 + `0xb7` (`ERROR_ALREADY_EXISTS`), 그 밖 성공 → 핸들 + 0, 음수 → `-1` + DOS 오류. `0xC00000BB` · `0xC0000010` 은 `0xC0000033` (123) 으로 바꿔서 (`+0x23c ~ +0x24c` → `+0x349`).
- 핸들은 `+0x2b1 MOV RAX,[RSP+0x78]` — 커널이 칸을 안 쓰면 그 자리 값 그대로. 요청 쪽 로그 "status 0 + 0 반환" 이 이 줄 그대로다.
- A 판 `kernel32!CreateNamedPipeA` 는 이름만 유니코드로 바꿔 W 를 부른다.

#### 1(b). `CreatePipe` 세 syscall

| syscall | access | 이름 · root | share · options · 기타 | 줄 |
|---|---|---|---|---|
| ① `NtOpenFile` | `0x80100000` | `\Device\NamedPipe\` (Length `0x24`, **MaximumLength 0**), Root 0, **Attributes 0** (대소문자 구분 · 상속 없음) | share 3 · options `0x20` | `+0x1b4 ~ +0x1fc` · `+0x209` |
| ② `NtCreateNamedPipeFile` | `0x80100100` | **빈 이름** (Length 0, Buffer NULL), **Root = ①**, Attributes `0x40` / `0x42` | share 3 · disposition 2 · options `0x20` · 종류 · 읽기 · 완료 0 · 최대 1 · 쿼터 둘 다 `nSize` 또는 `0x1000` · 시간 = 상대 120초 | `+0x6e` · `+0x72` · `+0x88` · `+0xba ~ +0xef` · `+0xff` |
| ③ `NtOpenFile` | `0x40100080` | **빈 이름**, **Root = ②**, Attributes `0x40` / `0x42` | share 3 · options `0x60` | `+0x113` · `+0x127` · `+0x12f` · `+0x14e` · `+0x156` |

- 결과서 대조: 세 syscall 의 인자가 요청 쪽 로그와 **같다**. 로그의 "② ③ root 0" 은 에뮬레이터가 ① 핸들을 안 줘서다.
- **① 은 처음 한 번만** 열고 kernelbase 전역 (rva `0x3ad980`) 에 `LOCK CMPXCHG` 로 넣는다 (`+0x46` · `+0x21f`). **끝에 안 닫는다.** `.asm` 의 `InteractiveUserNameSpaceSeparation+0x8` 주석은 "가장 가까운 앞 PDB 이름" 일 뿐 — refs 로 이 칸은 `CreatePipe` 만 만진다.
- 상속: `bInheritHandle ≠ 0` 이면 ② ③ 둘 다 `0x42` (`+0x19e ~ +0x1af`), ① 은 없음. SD 도 ② ③ 에 간다.
- 실패: ① · ② 실패 → `FALSE` (출력 안 건드림). ③ 실패 → **② 핸들만 닫고** `FALSE`. 성공 → `*hReadPipe = ②`, `*hWritePipe = ③`, 반환 1 (`+0x170 ~ +0x179`).
- npfs 쪽에서 본 모양: share 3 → 양쪽, 바이트 꼴, 최대 1, 이름표에 안 올라감. 읽기 핸들 = 서버 끝, 쓰기 핸들 = 클라이언트 끝.

#### 1(c). `ConnectNamedPipe` — `NtFsControlFile(h, ev, 0, ctx, iosb, 0x110008, 0, 0, 0, 0)`

- 동기 (`lpOverlapped == NULL`): 이벤트 · ApcContext NULL, IOSB 는 지역 `[RSP+0x50]` (`+0x1b` → `+0xc0 ~ +0xca`).
- 겹침: `Event = hEvent`, `ApcContext = lpOverlapped` (`hEvent` 아랫 비트 1 이면 NULL), IOSB = `lpOverlapped` 자신, 부르기 전에 `Internal = 0x103` (`+0x21 ~ +0x39`).

| `NtFsControlFile` 반환 | `ConnectNamedPipe` | `GetLastError` | 줄 |
|---|---|---|---|
| `0` | `TRUE` | 안 건드림 | `+0x76` → `+0x8d` · `+0x94` |
| 음수 (**`0xC00000B2` 포함**) | **`FALSE`** | DOS 오류 (`0xC00000B2` → **535**, `0xC00000B1` → 232, `0xC00000B3` → 536, `0xC000014B` → 109, `0xC0000120` → 995) | `+0x76` → `+0x78` · `+0x7a` |
| `0x103` + 겹침 | `FALSE` | 997 | `+0x8d` · `+0x92` → `+0x78` |
| `0x103` + `lpOverlapped == NULL` | `NtWaitForSingleObject(h)` 뒤 IOSB status 로 다시 판정 | — | `+0x9b` · `+0xa0` · `+0xaa` · `+0xba` |

- kernelbase 안에 `0xC00000B2` 를 따로 다루는 줄은 없다.
- 동기 **핸들** (`FILE_SYNCHRONOUS_IO_NONALERT`) 이면 I/O 관리자가 `NtFsControlFile` 안에서 기다린다 — run126 (sast/124 §4, `IopSynchronousServiceTail +0x3e6 IopWaitForSynchronousIo`). 그래서 `+0xaa` 기다림은 **비동기 핸들을 `lpOverlapped` 없이 부를 때만** 탄다.
- 오류 status 가 PENDING 없이 오면 `IopCompleteRequest` 가 IOSB 를 안 쓴다 (run126). 그래서 겹침 + `0xC00000B2` 일 때 `Internal` 이 `0x103` 으로 남을 수 있다 `[추측: 이어 붙인 결론]`.

#### 1(d). 나머지 넷 (+ 둘 이름만)

| 함수 | 내는 것 | 핸들 · 버퍼 | 결과 |
|---|---|---|---|
| `WaitNamedPipeW` | `NtFsControlFile(0x110018)`, 동기 | **새로 연 `\DosDevices\pipe\`** (`\Device\NamedPipe\` 아님 — `.rdata` rva `0x2a0ab8`, access `0x100080`, share 3, options `0x20`) 에 낸 뒤 닫음. 입력 = `{시간 8 B, 이름 길이 4 B, 시간 지정 1 B, 이름 (앞 `\\.\pipe\` 뗌)}` | ≥ 0 → `TRUE`, 음수 → `FALSE`. 시간 0 = 파이프 기본값, `0xffffffff` = 영원 |
| `PeekNamedPipe` | `NtFsControlFile(0x11400c)`, 임시 이벤트 | 출력 = 힙 `n + 0x10` (머리 `+4` 읽을 수 있는 바이트 · `+0xc` 메시지 길이) | `0x80000005` 도 성공으로 침 (`+0x139`) |
| `SetNamedPipeHandleState` | `NtSetInformationFile` 클래스 **`0x17`** (8 B {읽기 모드, 완료 모드}) | `*mode & ~3` 이면 syscall 없이 87 | `maxcnt` · `timeout` 을 주면 클래스 `0x19` 도 — npfs 가 안 받음 (3(d)) |
| `DisconnectNamedPipe` | `NtFsControlFile(0x110004)`, 임시 이벤트 | 서버 끝 핸들 | 클라이언트 핸들이면 npfs `0xC00000AF` → 1 |
| `GetNamedPipeInfo` · `GetNamedPipeHandleStateW` | `NtQueryInformationFile` `0x18` / `0x17` · `0x19` | — | (이름만) |

### 2. npfs 만들기 · 열기 · 닫기 [정적]

**들머리** (`DriverEntry +0x8b ~ +0x142` 이 `MajorFunction` 칸을 채움, `_DRIVER_OBJECT +0x70`):

| IRP_MJ | npfs 함수 (rva) |
|---|---|
| CREATE (0) | `NpFsdCreate` (`0xe3c0`) — 클라이언트 열기 |
| CREATE_NAMED_PIPE (1) | `NpFsdCreateNamedPipe` (`0xeba0`) — 서버 만들기 |
| CLOSE (2) · CLEANUP (0x12) | `NpFsdClose` (`0xd770`) · `NpFsdCleanup` (`0xf040`) |
| READ (3) · WRITE (4) | `NpFsdRead` (`0x11d70`) · `NpFsdWrite` (`0x12540`) |
| QUERY / SET_INFORMATION (5 / 6) | `NpFsdQueryInformation` (`0x14640`) · `NpFsdSetInformation` (`0x14e10`) |
| FLUSH (9) · QUERY_VOLUME (0xa) · DIRECTORY (0xc) · FSCTL (0xd) | `NpFsdFlushBuffers` · `NpFsdQueryVolumeInformation` · `NpFsdDirectoryControl` · `NpFsdFileSystemControl` |
| QUERY / SET_SECURITY (0x14 / 0x15) | `NpFsdQuerySecurityInfo` · `NpFsdSetSecurityInfo` |
| Fast I/O 읽기 · 쓰기 | `NpFastRead` · `NpFastWrite` (같은 규칙, 기다려야 하면 IRP 길로 넘김) |

- 안 채운 `DEVICE_CONTROL` (0xe) 등은 I/O 관리자 기본 `[추측]`.
- **넘기는 꼴 (ntoskrnl)**: `NtCreateNamedPipeFile` 은 8 ~ 13 인자를 지역 `NAMED_PIPE_CREATE_PARAMETERS` 에 옮기고, 시간 포인터가 있으면 `TimeoutSpecified = 1` (`+0x4b`). 포인터 정렬 · 읽기 검사는 **유저 모드일 때만** (`+0x5c` · `+0x62`). `IopCreateFile` 에 `CreateFileType 1` (`+0x10e`) · 매개변수 포인터 (`+0x109`) → `OPEN_PACKET +0x78` · `+0x80` (`IopCreateFile +0x2c6` · `+0x2d1`) → `IopParseDevice +0xfa3` 에서 1 이면 `+0xfac MajorFunction = 1` · `+0xfb7` 스택 `+0x20` = 매개변수 포인터. IRP 스택 `Options` 상위 1 B = disposition, 아래 = `CreateOptions`.
- **ntoskrnl 은 14 인자의 뜻을 안 본다** — 값 검사는 전부 npfs 몫.

#### 2(a). 서버 쪽 만들기

| 경우 | status | `Information` | 줄 |
|---|---|---|---|
| **새 이름** (이름표에 없음, 첫 글자 `\`, 길이 > 2 B) | **`0`** | **`2`** | `NpFsdCreateNamedPipe +0x195` · `+0x30b` ; `NpCreateNewNamedPipe +0x2f8` · `+0x2fb` |
| 새 이름 + disposition 1 (`FILE_OPEN`) | `0xC0000034` | 0 | `NpCreateNewNamedPipe +0x6f` → `+0x3ad` |
| 새 이름인데 `TimeoutSpecified 0` · 최대 0 · 시간 ≥ 0 · share 가 1 · 2 · 3 밖 · 바이트 꼴 + 메시지 읽기 · 이름이 `\` 뿐 | `0xC000000D` | 0 | `+0x4a ~ +0x9a` · `+0x375 ~ +0x3b4`, `NpCreateFcb` |
| 새 이름 + 루트 SD 검사 실패 | `SeAccessCheck` 값 (보통 `0xC0000022`) | 0 | `NpFsdCreateNamedPipe +0x1e6 ~ +0x25d` |
| **이미 있는 이름** — 파이프 SD 가 인스턴스 만들기 권한을 안 줌 | 보통 `0xC0000022` | 0 | `NpCreateExistingNamedPipe +0xa0` · `+0x12d` |
| 〃 **인스턴스 수 넘음** | **`0xC00000AB`** (→ 231) | 0 | `+0x136 ~ +0x13f` → `+0x267` |
| 〃 **disposition 2** (인스턴스 수 검사 **뒤**) | **`0xC0000022`** (→ 5) | 0 | `+0x145` · `+0x14a` → `+0x293` |
| 〃 **방향 (share) 이 첫 인스턴스와 다름** | **`0xC0000022`** | 0 | `+0x153 ~ +0x17a` → `+0x293` |
| 〃 통과 (**종류 · 읽기 모드 · 쿼터 · 최대 · 시간은 안 견줌**) | **`0`** | **`1`** | `+0x244` · `+0x247` |
| 이름 뒤에 남은 글이 있음 | `0xC0000033` | 0 | `+0x34c` → `+0x43e` |
| **이름 없는 파이프**: root = 루트 (`\Device\NamedPipe\`) 핸들 + 빈 이름 | `0` | `2` (이름표에 안 올림) | `NpFsdCreateNamedPipe +0xf4 ~ +0x10b` · `+0x318 ~ +0x329` |
| 빈 이름인데 root 가 루트가 아님 | `0xC0000033` | 0 | `+0x106` |

- **방향 규칙**: share 2 → 들어옴 (서버가 읽음), 1 → 나감, 3 → 양쪽. **`DesiredAccess` 는 방향을 안 정한다.**
- **access 가 정하는 것**: 처음 상태. `DesiredAccess & 0xf2000003 ≠ 0` 이면 **LISTENING (2)**, 0 이면 DISCONNECTED (1) (`NpCreateNewNamedPipe +0x1c5 ~ +0x20a`, 같은 식 `NpCreateExistingNamedPipe +0x18b ~ +0x1ab`). 요청 쪽 로그의 서버 access 는 전부 `GENERIC_*` 비트가 있어 **처음부터 LISTENING**.
- 상태 번호 (CCB `+0x8` 바이트): 1 DISCONNECTED · 2 LISTENING · 3 CONNECTED · 4 CLOSING (이름은 공개 헤더 값과 같다고 봄 `[추측: 이름]`).
- 파일 객체 표식: `Flags |= 0x80` (`FO_NAMED_PIPE`), `FsContext = FCB`, `FsContext2 = CCB | 3` (서버) · `CCB | 1` (클라이언트) · `루트 | 1`. 아랫 비트 0 = 쓸 수 있음, 비트 1 = 서버 (`NpListen +0x39 ~ +0x4f`).
- CCB · FCB 안쪽 칸은 요청대로 이름 · 쓰임만 (결과서 2(a) 끝). 루트 SD 는 부팅 때 smss 가 파이프 이름 앞에 다는 것 (run98, `SmpCreateProtectedPrefixes +0xab3`) 으로 보인다 `[추측: 두 결과서를 이은 것]`.

#### 2(b). 클라이언트 쪽 열기 (`NpFsdCreate` → `NpCreateClientEnd`)

| 경우 | status | `Information` | 줄 |
|---|---|---|---|
| **없는 이름** | **`0xC0000034`** (→ 2) | 0 | `NpFsdCreate +0x26b` · `+0x26f` → `+0x379 ~ +0x385` |
| 앞부분만 맞는 이름 (`\a\b`, `\a` 있음) · `\` 로 안 시작 · 2 B 이하 | `0xC0000033` | 0 | 같은 줄 · `+0x1dd ~ +0x1f0` |
| 이름은 있는데 인스턴스 0 개 (서버 끝 다 닫음) | `0xC0000034` | 0 | `+0x290` · `+0x298` → `+0x5d7` |
| **듣는 인스턴스 없음** (모두 붙었거나 닫는 중 · 끊김) | **`0xC00000AC`** (→ 231 `ERROR_PIPE_BUSY`) | 0 | `NpCreateClientEnd +0x162 ~ +0x170` → `+0x360` |
| **방향과 안 맞는 access** (읽기인데 들어옴 파이프, 쓰기인데 나감 파이프) | `0xC0000022` | 0 | `+0x140 ~ +0x14d` · `+0x31f ~ +0x339` |
| 파이프 SD 가 access 를 안 줌 | 보통 `0xC0000022` | 0 | `+0xa8` |
| **붙음** | **`0`** | **`1`** | `NpFsdCreate +0x2f2` |
| 끝 `\` 로 루트 열기 (`CreatePipe` ①) | `0` | `1` | `NpFsdCreate +0xf0 ~ +0x17c` |
| 장치 자체 (이름 · root 없음) | `0` | `1` | `+0xea` → `+0x40e ~ +0x428` |
| **서버 핸들을 root 로 한 빈 이름** (`CreatePipe` ③) | 위 "붙음 / 듣는 인스턴스 없음 / 방향" 과 같음 | | `+0x4b4 ~ +0x4d6` → `+0x27a` |
| root 가 끊긴 핸들 · 다른 노드 | `0xC0000033` | 0 | `+0x9a` → `+0xa1` |

- **붙으면**: 그 인스턴스 LISTENING → CONNECTED, 클라이언트 파일 객체 `FsContext2 = CCB | 1`, **기다리던 듣기 IRP (`0x110008`) 를 status 0 으로 끝냄** (`NpCreateClientEnd +0x2e0 NpSetConnectedPipeState` → `NpFsdCreate +0x353` 완료 고리).
- **서버가 `0x110008` 을 안 불러도 붙는다** — 새 인스턴스가 처음부터 LISTENING 이니까 (2(a)).
- 클라이언트 열기에서 `ShareAccess` · disposition 은 **안 쓴다** (`FILE_DIRECTORY_FILE` 갈래에만). 그래서 `CreateFile(OPEN_ALWAYS)` 로도 없는 파이프는 안 생기고 `0xC0000034`.

#### 2(c). 닫기 (CLEANUP → `NpSetClosingPipeState`, 그리고 CLOSE)

- `NpFsdCleanup` · `NpFsdClose` 는 늘 status 0 으로 IRP 를 끝낸다.

| 닫히는 때 | 상태 | 기다리던 IRP |
|---|---|---|
| CONNECTED 에서 한쪽 끝 | → **CLOSING (4)** (`NpSetClosingPipeState +0x6f`) | 닫는 쪽 대기열 전부 · 상대의 대기 읽기 → `0xC000014B`. **상대가 이미 써 둔 자료는 남긴다** (`+0x4b ~ +0x14e`) |
| LISTENING 에서 서버 끝 | 그대로 | 듣기 IRP 전부 `0xC000014B` (`+0x1f4 ~ +0x228`) |
| CLOSING 에서 나머지 끝 | 대기열 비움 · `NpClearSecurity` | 남은 것 `0xC000014B` (`+0x153 ~ +0x1f0`) |
| 서버 끝 (FCB 쪽) | `NpUnlinkCcb`, 인스턴스 0 이면 `NpUnlinkFcb` — **이름이 이름표에서 사라짐** → 뒤 열기 `0xC0000034` | (`NpCommonCleanup +0x33a` · `+0x353`) |

- 한쪽이 닫힌 뒤 상대: 읽기 + 자료 있음 → 읽힘, 읽기 + 빈 → `0xC000014B` (109), 쓰기 → `0xC00000B1` (232), 듣기 → `0xC00000B1`.
- `DisconnectNamedPipe` (`0x110004`, 서버 끝): CONNECTED · CLOSING → DISCONNECTED, 양쪽 대기 IRP `0xC00000B0` (233), 클라이언트 핸들의 `FsContext2` 아랫 비트를 지워 그 뒤 읽기 · 쓰기 · 정보 설정은 `0xC00000B0`. 이미 끊겼는데 또 → `0xC00000B0`.
- **CLEANUP 이 언제 오나는 `[공백]`** — `IopCloseFile +0x10a` 가 CLEANUP IRP 를 짓는 줄이고 `+0x67 CMP R9,1` 일 때 들어가는데, `R9` 가 "이 프로세스의 핸들 수" 인지 "시스템 전체 핸들 수" 인지 정적으로 못 정했다. 자식이 쓰는 끝을 물려받은 뒤 부모가 닫을 때 갈린다 → 동적 3.

### 3. 데이터 길 [정적]

#### 3(a). READ (`NpFsdRead`)

| 상황 | status | `Information` | 기다림 | 줄 |
|---|---|---|---|---|
| 파이프가 아님 (루트 등) · 방향 안 맞음 | `0xC000000D` | 0 | 아니오 | `+0x5e` → `+0x2ac` · `+0xdb ~ +0xe6` → `+0x230` |
| 끊긴 핸들 · DISCONNECTED | `0xC00000B0` | 0 | 아니오 | `+0x67` → `+0x28e` · `+0xbc` → `+0x2a2` |
| LISTENING | `0xC00000B3` (→ 536) | 0 | 아니오 | `+0xc5` → `+0x34b` |
| **자료 있음, 바이트 꼴** | **`0`** — 모자라도 **있는 만큼만** | `min(Length, 있는 바이트)` | 아니오 | `+0x10d` → `+0x27e NpReadDataQueue` |
| 자료 있음, 메시지 꼴 + 버퍼 작음 | `0x80000005` (→ 234), 나머지는 큐에 | 복사한 바이트 | 아니오 | `NpReadDataQueue +0x36e` · `+0x37c` |
| **빈 + 상대 닫힘 (CLOSING)** | **`0xC000014B`** (→ 109) — **`END_OF_FILE` 아님** | 0 | 아니오 | `+0x118` · `+0x11b` → `+0x23a` |
| 빈 + NOWAIT | `0xC00000D9` (→ 232) | 0 | 아니오 | `+0x121` · `+0x124` → `+0x298` |
| **빈 + 블로킹** | **`0x103`** (`+0x154 NpAddDataQueueEntry` 가 큐에 걸고 취소 루틴 달기) | — | **예** — 동기 핸들은 I/O 관리자가 파일 객체 이벤트를 기다림 (run126), 비동기는 PENDING 으로 돌아오고 상대가 쓸 때 **쓰는 스레드 안에서** 채워 끝남 | `NpFsdRead +0x154` |

- 자료 있음 검사 (`+0x10d`) 가 CLOSING 검사 (`+0x118`) 보다 **앞** — 닫힌 뒤에도 남은 자료가 먼저 읽힌다.
- `0xC000014B` · `0xC00000D9` 처럼 오류로 바로 끝난 `NtReadFile` 은 호출자 IOSB 를 **안 건드린다** (run126 의 "오류 + PENDING 없음" 갈래와 이은 결론 — 동적 1).
- 같은 스레드에서 `WriteFile` 뒤 `ReadFile` 은 바로 끝난다. **자료 없이 블로킹 `ReadFile` 을 부르면 그 스레드는 영원히 기다린다** (`CreatePipe` 파이프는 완료 모드 0).

#### 3(b). WRITE (`NpFsdWrite` → `NpCommonWrite`)

| 상황 | status | `Information` | 기다림 | 줄 |
|---|---|---|---|---|
| 끊긴 핸들 · DISCONNECTED | `0xC00000B0` | 0 | 아니오 | `+0x2c` · `+0x96` · `+0x2e1` → `+0x2c1` / `+0x2cc` |
| 파이프 아님 · 방향 안 맞음 | `0xC000000D` | 0 | 아니오 | `+0x3f` · `+0xb5` · `+0x1c9` |
| **LISTENING (상대 안 붙음)** | **`0xC00000B3`** (→ 536) | 0 | 아니오 | `+0x2e5` → `+0x34d` |
| **CLOSING (상대 닫힘)** | **`0xC00000B1`** (→ 232) | 0 | 아니오 | `+0x2e9` |
| **CONNECTED + 쿼터 안** | **`0`** | **`Length` 전부** | 아니오 | `+0xd4` · `+0xda` · `+0xe8 ~ +0x103` · `+0x138` |
| CONNECTED + **쿼터 넘음 + 블로킹** | **`0x103`** — 큐에 걸었다가 읽는 쪽이 비우면 `{0, Length}` 로 끝남 | 끝날 때 `Length` | **예** | `+0x141` → `+0x239` |
| CONNECTED + 쿼터 넘음 + NOWAIT | `0` (바이트 꼴은 준 만큼, 메시지 꼴은 0 바이트) | 그만큼 | 아니오 | `+0x2a6 ~ +0x30a` (요약만) |

- 쿼터 = 그 방향 대기열의 `Quota − QuotaUsed`. `CreatePipe` 는 `0x1000`. `CreateNamedPipe(…, 0, 0, …)` 는 **0 그대로** (올려 주는 줄 없음) — 그러면 읽기 대기가 있을 때만 바로 끝나고 아니면 기다린다 `[정적 읽기, 동작은 동적 권함]`.
- 길이 0 쓰기 (메시지 꼴은 빈 메시지, 바이트 꼴은 그냥 0) 는 `[추측: 자세히는 안 읽음]`.

#### 3(c). 듣기 `0x110008` (`NpListen`)

| 상태 | status | 완료 꼴 | 줄 |
|---|---|---|---|
| 서버 끝 아님 (클라이언트 · 루트 · 끊긴 핸들) | `0xC00000AF` (→ 1) | 바로 | `+0x33` · `+0x3d` · `+0x4f` → `+0x102` |
| **CONNECTED (이미 붙음)** | **`0xC00000B2`** (→ 535) | 바로 (오류라 IOSB 안 씀 — run126) | `+0x90 ~ +0x99` |
| **LISTENING + 블로킹** | **`0x103`** | 듣기 목록에 걸고 취소 루틴 달기. 클라이언트가 붙으면 **0** 으로 끝남 | `+0xf1` · `+0xf5` → `+0x1b9 ~ +0x1d5` |
| LISTENING + NOWAIT | `0xC00000B3` (→ 536) | 바로 | `+0xfb` |
| DISCONNECTED (끊은 뒤 다시 듣기) | 기다리던 `WaitNamedPipe` 들을 깨우고 (`+0x121 NpCancelWaiter`) → LISTENING 으로 바꾼 뒤 블로킹 `0x103` / NOWAIT `0xC00000B3` | 위와 같음 | `+0xe6` → `+0x109 ~ +0x1b4` |
| **CLOSING (상대가 닫음)** | **`0xC00000B1`** (→ 232) | 바로 | `+0x1df ~ +0x1e9` |
| 이미 취소된 IRP | `0xC0000120` (→ 995) | 바로 | `+0x1d5` |

- PENDING 듣기 IRP 가 끝나는 넷: 클라이언트 붙음 → 0, 서버 끝 닫힘 → `0xC000014B`, `DisconnectNamedPipe` → `0xC00000B0`, 취소 → `0xC0000120`.
- `NpFsdFileSystemControl` 은 `status ≠ 0x103` 이면 IRP 를 끝내고 (`+0x35 ~ +0x4c`), `Information` 은 안 쓴다 (0).

#### 3(d). `GetFileType` 의 장치 종류 · 정보 클래스

- `GetFileType +0x72` 가 `NtQueryVolumeInformationFile(클래스 4 = FileFsDeviceInformation, 8 B)` 을 부르고, `DeviceType 0x11` 이면 **3 (`FILE_TYPE_PIPE`)** (점프 표 원본 바이트, 결과서). 콘솔 `0x50` → 2 는 run83 · run84 와 같다.
- **누가 답하나**: ntoskrnl 이 **직접** — `NtQueryVolumeInformationFile +0x1fe` (클래스 4) → `+0x3d6 ~ +0x3e2` (장치 종류 `0x14` 네트워크 파일 시스템이 아니면) → `+0x41f` 장치 객체 `+0x48` (`DeviceType`) · `+0x426` `+0x34` (`Characteristics`) 를 출력에, `+0x442` · `+0x445` IOSB `{0, 8}`. 길이 8 미만이면 `0xC0000004` (`+0x50b`). **IRP 를 npfs 로 안 보낸다.**
- 그 장치 객체 = `NpCreateDevice +0x60 IoCreateDevice(종류 0x11, 특성 0x20000)`. → `DeviceType 0x11`, `Characteristics` 는 `0x20000` + (I/O 관리자가 더 얹는 비트 `[공백]` → 동적 2). `GetFileType` 은 특성을 안 읽는다.
- npfs 자신이 답하는 볼륨 클래스: 5 (`FileSystemAttributes 2`, 이름 `"NPFS"` 8 B) · 1 · 3 · 7. 그 밖 `0xC00000BB`.
- `NtQueryInformationFile`: 클래스 4 · 5 · 6 · 7 · 9 · `0xe` · `0x12` · **`0x17`** (8 B {읽기 모드, 완료 모드}) · **`0x18`** (0x28 B {종류, 방향 0/1/2, 최대, 현재, 들어옴 쿼터, 읽을 바이트, 나감 쿼터, 쓸 자리, 상태 1 ~ 4, 끝 0 클라이언트 / 1 서버}), 그 밖 `0xC000000D`. 클래스 `0x19` 는 **안 받는다**.
- `NtSetInformationFile`: `0x17` 만 일함 (`NpSetPipeInfo` — 값 0 / 1 밖 · 바이트 꼴에 읽기 모드 1 → `0xC000000D`, 대기 IRP 가 있는데 NOWAIT 로 → `0xC00000AE`, 끊긴 핸들 `0xC00000B0`). `4` 는 아무 일 없이 0.
- PDB 에 `FILE_PIPE_*` 타입이 없어 (ntoskrnl · npfs · kernelbase 다) 칸 모양은 npfs 코드가 읽고 쓰는 칸으로 적었다.

### 호출 관계

```
kernel32!CreateNamedPipeA
 └─ +0x71  kernelbase!CreateNamedPipeW                                                    [정적]
            ├─ +0x87   RtlDosPathNameToNtPathName_U      \\.\pipe\X → \??\pipe\X
            ├─ +0xd1   RtlDefaultNpAcl → NtQueryInformationToken ×3   (SD 없을 때 · 기본 DACL)
            └─ +0x235  NtCreateNamedPipeFile   ── SYSCALL  access 방향별 · share 2/1/3 · disp 2/3 · opt 0/0x20 · 시간 늘 포인터
                         ⇒ nt!NtCreateNamedPipeFile +0x151 IopCreateFile (CreateFileType 1)
                            ⇒ IopParseDevice +0xfac MajorFunction = 1
                               ⇒ npfs!NpFsdCreateNamedPipe
                                    ├─ +0x310  NpCreateNewNamedPipe        새 이름 / 빈 이름 + 루트 → 0 · Information 2 · 상태 LISTENING
                                    └─ +0x3d2  NpCreateExistingNamedPipe   있는 이름 → 0 · 1 / 0xC00000AB / 0xC0000022
kernelbase!CreatePipe
 ├─ ① +0x210  NtOpenFile(\Device\NamedPipe\)          처음 한 번만 · 전역 0x3ad980 · 안 닫음
 │              ⇒ npfs!NpFsdCreate (루트 열기) → 0 · 1
 ├─ ② +0x106  NtCreateNamedPipeFile(root ①, 빈 이름)  = 읽기 핸들 (서버 끝)
 │              ⇒ … ⇒ NpFsdCreateNamedPipe +0x310 NpCreateNewNamedPipe (이름표에 안 올림)
 └─ ③ +0x15d  NtOpenFile(root ②, 빈 이름)             = 쓰기 핸들 (클라이언트 끝)   실패면 +0x258 NtClose(②)
                ⇒ npfs!NpFsdCreate +0x2f7 NpCreateClientEnd → +0x2e5 NpSetConnectedPipeState (CONNECTED)
kernelbase!ConnectNamedPipe
 ├─ +0x6a  NtFsControlFile(0x110008)
 │           ⇒ npfs!NpFsdFileSystemControl +0x27 NpCommonFileSystemControl +0x1c6 NpListen
 │                CONNECTED 0xC00000B2 · LISTENING 0x103 / 0xC00000B3 · CLOSING 0xC00000B1
 └─ +0xb1  NtWaitForSingleObject                     (lpOverlapped 없음 + PENDING 만)
kernelbase!ReadFile +0x8d / WriteFile +0x8d  NtReadFile / NtWriteFile
 ⇒ npfs!NpFsdRead  +0x283 NpReadDataQueue · +0x159 NpAddDataQueueEntry (빈 블로킹 0x103)
 ⇒ npfs!NpFsdWrite +0x6d NpCommonWrite → +0x13d NpWriteDataQueue · +0x23e NpAddDataQueueEntry (쿼터 넘음)
CloseHandle … nt!IopCloseFile +0x10a CLEANUP (R9 == 1 일 때 — 뜻 [공백])
 ⇒ npfs!NpFsdCleanup +0x27 NpCommonCleanup +0x198 NpSetClosingPipeState   → CLOSING, 대기 IRP 0xC000014B
kernelbase!GetFileType +0x79  NtQueryVolumeInformationFile(4)
 ⇒ nt 가 직접: 장치 객체 DeviceType 0x11 → FILE_TYPE_PIPE 3        (npfs 안 감)
```

- (나무 오프셋은 ret 자리. `⇒` 는 syscall · I/O 관리자를 건너뜀. 콜스택으로 본 적 없음.)

## 확인된 것

- `[정적]` `CreateNamedPipeW` 의 14 인자 옮김표 (1(a) 표) — 요청 원문 `[추측]` 셋 (disposition 2 ↔ `FIRST_PIPE_INSTANCE` · options 0 ↔ `OVERLAPPED` · 종류 비트 1 ↔ `REJECT_REMOTE_CLIENTS`) 이 **다 맞다** (`+0x1d4` · `+0x137 ~ +0x14d` · `+0x1c3 ~ +0x1d1`).
- `[정적]` `CreatePipe` ② 의 root = ① 핸들, ③ 의 root = ② 핸들. ① 은 프로세스에 하나, 안 닫힘 (refs 3줄).
- `[정적]` npfs 는 방향을 **share 로만** 정하고 (`NpCreateNewNamedPipe +0x375 ~ +0x3b4`), 이미 있는 이름에서 종류 · 읽기 모드 · 쿼터 · 시간을 견주지 않는다 (`NpCreateExistingNamedPipe +0x180 ~ +0x1c6` 은 자기 CCB 에만 넣음).
- `[정적]` 새 인스턴스는 데이터 권한 access 면 처음부터 LISTENING — 클라이언트 열기 (`NpCreateClientEnd`) 가 듣기 목록 (FCB `+0x138`) 에서 바로 찾는다.
- `[정적]` `STATUS_PIPE_CONNECTED` (`0xC00000B2`) 는 kernelbase 에서 `FALSE` + 535 (`ConnectNamedPipe +0x76` 이 음수를 다 실패 길로).
- `[정적]` 쓰는 끝이 닫힌 뒤 빈 읽기 = `0xC000014B` (`NpFsdRead +0x118 ~ +0x23a`), 남은 자료는 먼저 읽힌다 (`+0x10d` 가 앞).
- `[정적]` 파이프 핸들의 `FileFsDeviceInformation` 은 ntoskrnl 이 장치 객체에서 바로 답한다 (`NtQueryVolumeInformationFile +0x1fe` → `+0x41f` · `+0x426` · `+0x442`).
- `[정적]` ntoskrnl 은 `NtCreateNamedPipeFile` 인자의 값을 검사하지 않는다 — 유저 모드 시간 포인터 정렬 · 읽기만 (`+0x5c ~ +0x92`).

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- 결과서 3(d) 의 "`NtQueryVolumeInformationFile +0x1fe CMP R15D,4; JZ +0x3b6`" 에서 점프 목적지는 **`+0x3d6`** 이다 (`.asm` `+0x202 JZ 0x1408c93b6`, 함수 시작 `0x1408c8fe0` → 차이 `0x3d6`). `0x3b6` 은 절대 주소 끝자리. 같은 결과서의 다음 줄 "`+0x3d6 ~ +0x3e2`" 와 이어진다.
- 결과서 1(c) 의 "`+0x9b/+0xa0 → +0xa2 ~ +0xbe`" 는 `+0x6f TEST RBX` · `+0x72 JZ +0x9b` (겹침 없음) 로 들어오는 갈래다. `NtWaitForSingleObject` call 은 `+0xaa` (ret `+0xb1`), 기다림 뒤 음수면 `+0xb8 JS +0x78`, 아니면 `+0xba` IOSB status 를 들고 `+0x74` 판정으로.

## 요청 쪽 로그와 코드가 다른 곳 (요청 쪽 에뮬레이터 로그 — 우리 `[관찰]` 아님)

- 요청 쪽은 "② ③ root 0" 을 봤다 — 진짜는 ① · ② 의 핸들이 간다. 에뮬레이터가 ① 에 핸들을 안 줘서 생긴 모습.
- "`CreateNamedPipeA` 가 0" — 에뮬레이터가 status 0 인데 핸들 칸을 안 써서, `CreateNamedPipeW +0x2b1` 이 그 칸 값 (0) 을 그대로 돌려준 것. 실패 꼴이 아니다 (실패면 `-1`).
- "`CreateFileA` 가 −1 → `Sleep` 되풀이" — 서버 쪽이 진짜로 만들어졌다면 첫 열기가 바로 붙는다 (처음부터 LISTENING). 지금은 서버 끝이 없어서 진짜라면 `0xC0000034` (→ 2) 일 자리.
- 요청 쪽 질문 "`STATUS_PIPE_CONNECTED` → 참 + `ERROR_PIPE_CONNECTED`?" — **참이 아니라 FALSE** + 535.

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 줄 (결과서가 적은 자리) | 판정 | 까닭 |
|---|---|---|
| `nt_legacy_file_frontends.cpp:48` `nt_create_named_pipe_file` — 인자 읽기 | **맞다** | 시간 포인터 정렬 (`&3`) · 읽기 실패가 `NtCreateNamedPipeFile +0x68 ~ +0x92` 와 같음 (**유저 모드일 때만**). 이름표 순서 (`a[4]` share … `a[12]` 나감 쿼터) 도 `+0x11f ~ +0x14c` 와 같음 |
| 〃 지금 `stop` | **아직 안 함** | 만들려면 2(a) 표가 규칙 전부 (값 검사는 npfs 몫이라 여기서). 성공 = status 0 · **IOSB.Information 2 (새 · 이름 없음) / 1 (있는 이름)** · 핸들 = 새 서버 끝 |
| `nt_create_file.cpp:110` (`object_name_empty_unverified`) | **틀림 (막힘)** | `CreatePipe` ② ③ 은 root ≠ 0 + 빈 이름 (Length 0, Buffer NULL) 이 정상. root 가 있으면 빈 이름을 통과시키고 root 가 "루트 핸들" 인지 "파이프 끝" 인지 보는 길로 가야 함. ① 의 `MaximumLength` 는 0 — `MaximumLength ≥ Length` 검사를 넣으면 틀림 |
| `nt_create_file.cpp:189 ~ 191` (`path_prefix_unsupported`) / `nt_open_file.cpp` | **틀림 (막힘)** | 이름 셋을 받아야 함: `\??\pipe\<이름>` · `\Device\NamedPipe\` (Attributes 0 = 대소문자 구분) · `\DosDevices\pipe\` (`WaitNamedPipeW`). SD 가 있으면 `stop` 하는 줄은 **`CreateNamedPipeW` 가 늘 기본 SD 를 달아 보내므로** 걸림돌 — 받아서 무시 (또는 저장). OA 속성 검사 `& ~0x42` · `NtOpenFile` 의 `FILE_OPEN` 고정은 맞음 |
| `namespace.cpp:93` 곁 | **일부 없음** | `\Device\NamedPipe` 장치는 있음 (맞음). `\GLOBAL??\PIPE` · `\??\PIPE` → `\Device\NamedPipe` 링크와 `\DosDevices` → `\??` 가 없음 (결과서 `grep DosDevices` 0줄) |
| `nt_file_misc.cpp:104` → `nt_device_io_control_file.cpp` (`0x110008`) | **틀은 맞음, 응답 없음** | `NtFsControlFile` 이 같은 요청 검사라는 건 맞음 (run126). 응답은 1(c) · 3(c) 표 — 버퍼 NULL · 0, 상태별 status, `0x103` 이면 동기 핸들은 기다림 길, 오류면 IOSB 안 씀 |
| `nt_read_file.cpp` ("끝이면 `STATUS_END_OF_FILE`") | **파이프엔 틀림** | 상대 닫힘 + 빈 = `0xC000014B`, NOWAIT 빈 = `0xC00000D9`, `ByteOffset` 안 씀 (오프셋 필수 검사 면제 여부는 `[공백]`) |
| `nt_write_file.cpp` | **파이프 갈래 필요** | 안 붙음 `0xC00000B3` · 닫힘 `0xC00000B1` · 쿼터 안 `0` + `Length` · 쿼터 넘음 블로킹 = 기다림 (3(b)) |
| `include/syscall_layer/files.hpp` | **새 종류 둘 필요** | "파이프 끝" (서버 / 클라이언트, 상태 1 ~ 4, 읽기 · 완료 모드, 두 방향 대기열 + 쿼터, 짝 끝, 이름 · 최대 · 방향 · 종류를 든 FCB 격, 듣기 목록) · "파이프 루트". `FO_NAMED_PIPE` 등 표식은 선택. 안쪽 칸 오프셋 불필요 |

- 한 줄로: **npfs 겉 동작은 프로세스 안 상태기계 (1 ~ 4) + 두 방향 바이트 대기열 + 쿼터 하나로 낼 수 있다** — 호스트 파이프 없이. 막힘을 푸는 첫 셋은 `NtCreateNamedPipeFile` 성공 대답 (Information 1 / 2) · 빈 이름 + root 열기 · `\??\pipe\` 이름.
- 덧 (flow-writer): 비동기 파이프 핸들을 완료 포트에 묶을 때의 패킷 조건은 run127 (sast/125) 표 — I/O 관리자 규칙이라 드라이버와 무관하다 `[추측: 결과서는 파이프 + 포트 조합을 안 읽음]`.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 7 — 대기열 **Q244**, 전부 선택. 파이프를 쓰는 작은 exe 가 필요 — test.exe 는 안 씀, 고르기는 사용자 결정. 커널 멈춤점 (`npfs!…` · `nt!IopCloseFile`) 은 시스템 전체 파이프가 지나가니 **그 프로세스일 때만** 멈추게 조건을 붙일 것):
1. **요청 원문의 다섯 status** — 같은 프로세스 `CreatePipe` → `WriteFile(n)` → `ReadFile` → 쓰는 끝 `CloseHandle` → `ReadFile`. 예측: TRUE · `0`/n · `0`/n · (CLEANUP 0) · **`0xC000014B` 이고 IOSB 안 써짐**. 멈춤점 `KERNELBASE!CreatePipe+0x210` (① 뒤, 처음 한 번) · `+0x106` (② 뒤) · `+0x15d` (③ 뒤), `KERNELBASE!WriteFile+0x8d` / `+0x11a`, `KERNELBASE!ReadFile+0x8d` / `+0x12a`, `npfs!NpFsdWrite` · `npfs!NpFsdRead` · `npfs!NpFsdCleanup` 입구.
2. **`FileFsDeviceInformation` 의 `Characteristics`** — `nt!NtQueryVolumeInformationFile+0x426` 의 값이 `0x20000` 인지 (`!devobj \Device\NamedPipe` 또는 `dt nt!_DEVICE_OBJECT <주소> Characteristics DeviceType`).
3. **CLEANUP 이 오는 때** — `nt!IopCloseFile` 입구 `r8` · `r9`, `+0x10a` 에 서나, `npfs!NpFsdCleanup` 이 오나. 자식이 쓰는 끝을 물려받은 상태에서 부모가 `CloseHandle` 했을 때 (= 자식이 살아 있는 동안 부모의 읽기가 BROKEN 인지 PENDING 인지). 요청 쪽 "자식 표준 출력 길" 에 바로 걸림.
4. **`WaitNamedPipeW` 의 기다림 · 타임아웃 status** — 듣는 인스턴스가 있으면 0 으로 바로, 없으면 `NpAddWaiter` 로 기다림까지는 읽음. 타임아웃 status 와 DOS 오류 매핑은 `[공백]` (정적 1 과 같이).
5. **`\DosDevices\pipe\` 가 진짜 풀리는지** — `KERNELBASE!WaitNamedPipeW+0x189` 앞 (`+0x182` 의 `r8`) OA → `!obja`.
6. **`FO_NAMED_PIPE` (0x80) 이 `NtReadFile` / `NtWriteFile` 에서 하는 일** — 비동기 핸들로 `ByteOffset` 없이 읽고 쓸 때 오프셋 필수 검사를 비켜 가는지.
7. 한 동기 핸들에서 한 스레드가 블로킹 읽기 중일 때 다른 스레드의 쓰기 (파일 객체 락 직렬화 — `NtReadFile` 쪽 줄은 안 봄 `[공백]`). 이름 있는 파이프 샘플 (서로 다른 파일 객체) 은 영향 없음.

정적 (결과서 "다음 요청 후보" — 대기열 **Q245**):
1. `NpAddWaiter` · `NpCancelWaiter` · `NpTimerDispatch` · `NpCancelWaitQueueIrp` — `WaitNamedPipeW` 의 기다림 · 깨움 · 타임아웃 status (동적 4).
2. ntoskrnl `NtReadFile` / `NtWriteFile` 의 `FO_NAMED_PIPE` · `FO_SYNCHRONOUS_IO` 갈래와 `IopCloseFile` 의 `R8` / `R9` 뜻 (동적 3 · 6 · 7) — "자식이 핸들을 물려받은 파이프의 CLEANUP 시점".
3. `NpPeek` (`0x11400c`) 출력 칸 · 상태별 status, `NpTransceive` (`TransactNamedPipe` — 이번엔 범위 밖), `NpFsdFlushBuffers`.
4. `NpFindPrefix` · `NpTranslateAlias` (`LOCAL\` 별칭 — AppContainer · 사일로에서 이름이 바뀜), `NpReadParameters` · `NpReadAlias` (레지스트리 별칭 표 — 이름 없는 파이프엔 영향 없음).
5. 메시지 꼴 파이프의 `NpReadDataQueue` · `NpWriteDataQueue` 정밀 읽기 (이번엔 바이트 꼴 중심) · NOWAIT 쓰기 조합 · 길이 0 쓰기.
6. kernelbase `GetNamedPipeInfo` · `GetNamedPipeHandleStateW` 의 사용자 이름 얻기 간접 호출 (`[0x1803c61d8]`) — 클라이언트 가장 쪽이라 범위 밖, 이름만.
- 그 밖 `[공백]`: npfs 루트 SD 가 smss `SmpCreateProtectedPrefixes +0xab3` 것인지 (run98 과 이은 `[추측]`), `NpTranslateAlias` 가 `LOCAL\` 밖 이름을 그대로 넘기는지 (`.c` 첫머리만 봄).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서: npfs `.c` 는 status 상수가 음수 십진수 (`-0x3fffff50` = `0xC00000B0`) 로 나오고 `NpCommonFileSystemControl.c` 반환값이 깨져 보인다 (Ghidra 레지스터 추적 실패) — 그래서 status 와 흐름은 `.asm` 으로 셌다. `CreatePipe.asm` 의 전역 주석 `InteractiveUserNameSpaceSeparation+0x8` 은 "가장 가까운 앞 이름" (refs 로 별개 칸 확인). 기록만, 하네스 제안 아님.
- 점프 목적지를 적을 때 절대 주소 끝자리와 함수 안 오프셋을 헷갈린 곳 하나 (위 "flow-writer 대조"). 결과서 표기 규칙 ("목적지 절대 주소 − 함수 시작") 은 이미 있으니 하네스 제안 아님.
