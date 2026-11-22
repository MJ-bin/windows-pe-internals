# 129 npfs-pipes — 정적 분석 결과

- 요청서: `_workspace/sast/129_npfs-pipes.md` (원문 `../PE_emulator/_workspace/05_tuning_planner/298_internals-npfs-pipes.md`)
- 모듈 / 바이너리 (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/` 아래):
  - `KernelBase.dll` pdb `kernelbase.pdb` guid+age `72CCE506944DE0267C172AD64287F9BD1` — 이미 analyze 됨, export 만.
  - `ntoskrnl.exe` pdb `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준 빌드와 같음 (`ms/sym/ntoskrnl.exe.pdb.info.txt`). 이미 analyze 됨, export 만.
  - `drivers/npfs.sys` (120,264 B) pdb `npfs.pdb` guid+age **`C433ACB0BA96C41BACA83308CFF250A61`** (`ms/sym/npfs.sys.pdb.info.txt` — 바이너리의 CodeView 줄에서 읽어 `msdl` 에서 받음, 118,784 B). 이번에 **처음 analyze** (10초, 종료코드 0, `ms/ghidra/npfs.analyze.log` 60~61줄 `SetPdb: npfs.sys <- …` 있음, 함수 130개 → `ms/decomp/npfs/_index.tsv`). 공개 PDB 라 **함수 이름은 다 나오고 구조체 타입은 없다** (`pdb_struct.py --list npfs.sys '.'` 결과 0개).
  - 덤으로 export 한 것 (질문 답에 필요해서): `kernel32.dll` `CreateNamedPipeA`, `ntoskrnl.exe` `NtQueryVolumeInformationFile`, `ntdll.dll` `RtlNtStatusToDosErrorNoTeb`·`RtlDefaultNpAcl` (depth 1).
- 서버 메모리: analyze 전 `free -g` = 전체 46 / 사용 14 / 가용 30 GB, `ps` 에 java·Ghidra 프로세스 없음 (sast/128 은 아직 안 돌고 있었거나 다른 프로젝트). 이번에 건드린 Ghidra 프로젝트는 `npfs`·`kernelbase`·`ntoskrnl`·`kernel32`·`ntdll` 뿐 — sast/128 의 `user32`·`combase` 는 안 건드림. 한 번에 하나씩만 돌림.
- 뽑은 함수:
  - kernelbase `^(CreateNamedPipeW|CreatePipe|ConnectNamedPipe|WaitNamedPipeW|PeekNamedPipe|SetNamedPipeHandleState|DisconnectNamedPipe|GetNamedPipeInfo|GetNamedPipeHandleStateW)$` depth 1 → 13개 (`BaseSetLastNTError` 포함). 이 지도를 보고 더 깊이 갈 필요는 없었다 (일은 전부 ntdll 스텁과 커널 쪽에서 일어남).
  - ntoskrnl `^NtCreateNamedPipeFile$` depth 0.
  - npfs: `DriverEntry`, `NpFsd{Create,CreateNamedPipe,Read,Write,FileSystemControl,Cleanup,Close,QueryInformation,SetInformation,QueryVolumeInformation}`, `NpCommon{FileSystemControl,Cleanup,Write,QueryInformation,SetInformation,QueryVolumeInformation}`, `NpCreate{NewNamedPipe,ExistingNamedPipe,ClientEnd,NamedPipePrefix}`, `NpListen`, `NpReadDataQueue`, `NpWriteDataQueue`, `NpSet{Closing,Disconnected,Connected}PipeState`, `NpPeek`, `NpWaitForNamedPipe`, `NpFast{Read,Write}`, `NpOpenNamedPipePrefix` 32개 + depth 1 → 85개 (`ms/decomp/npfs/`, `ms/disassemble/npfs/`).
- 표기: 이 문서의 `함수 +0x…` 는 전부 `ms/disassemble/<모듈>/<함수>.asm` 의 줄이다. call 은 `call 자리/ret 자리` 를 같이 적고, 점프 목적지는 "목적지 절대 주소 − 함수 시작" 으로 셌다. npfs 의 이미지 베이스는 `0x1c0000000`.
- 버전 확인:
  - ntoskrnl `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv`). kernelbase 는 `ReadFile`·`WriteFile`·`GetFileType` 이 이미 다른 요청에서 쓰이던 것 그대로.
  - **npfs 는 처음 보는 모듈이라 "알려진 사실" 표에 줄이 없다.** 대신 서로 다른 바이너리끼리 맞추어 봤다: (a) ntoskrnl PDB 의 `_NAMED_PIPE_CREATE_PARAMETERS` (0x28 바이트: `+0x0 NamedPipeType +0x4 ReadMode +0x8 CompletionMode +0xc MaximumInstances +0x10 InboundQuota +0x14 OutboundQuota +0x18 DefaultTimeout +0x20 TimeoutSpecified`) 와 `NtCreateNamedPipeFile` 이 채우는 칸, npfs `NpCreateNewNamedPipe` 가 읽는 칸이 세 곳 다 같다. (b) ntoskrnl PDB `_OPEN_PACKET` `+0x78 CreateFileType` `+0x80 MailslotOrPipeParameters` 와 `IopCreateFile` 이 쓰는 칸이 같다. (c) `NpCreateDevice` 의 `IoCreateDevice` 장치 종류 `0x11` = 요청서가 말한 `FILE_DEVICE_NAMED_PIPE`, 장치 이름 문자열은 `\Device\NamedPipe` (npfs `.rdata` RVA 0x42b8 을 직접 읽음). (d) 에뮬레이터 로그 40 샘플의 access·share·disposition 분포 (`0x40100000` 20 + `0x40180000` 1 = share 1 의 21 / `0xc01…` 16 = share 3 의 16 / `0x80180000` 3 = share 2 의 3 / disposition 2 가 `0x80000` 비트 선 12) 가 아래 1(a) 규칙으로 **하나도 안 틀리고** 다 설명된다. → 스크립트 오프셋 계산과 바이너리를 믿어도 된다. **일치.**

## 먼저 읽는 한 줄 답

1. **(a) `CreateNamedPipeW`**: `NtCreateNamedPipeFile` 로 그대로 옮긴다. access = 방향별 `0x80100000`(들어옴) / `0x40100000`(나감) / `0xc0100000`(양쪽) + `dwOpenMode & 0x010c0000`, share = 2 / 1 / 3, disposition = `FIRST_PIPE_INSTANCE`(0x80000) 이면 2 아니면 3, options = `OVERLAPPED` 이면 0 아니면 `0x20` (`WRITE_THROUGH` 이면 +2), 종류 = 메시지 1 + 원격거부 2, 읽기 = 메시지 1, 완료 = `NOWAIT` 1, 최대 = 255→`0xffffffff`, 시간 = **늘 포인터** (0 이면 −500000 = 상대 50 ms, 아니면 ms×−10000). **요청 원문의 `[추측]` 셋 다 맞다.** 실패 = `INVALID_HANDLE_VALUE` + 그 status 의 DOS 오류 (`NOT_SUPPORTED`·`INVALID_DEVICE_REQUEST` 는 `0xC0000033` 으로 바꿔서).
2. **(b) `CreatePipe`**: 세 syscall 이 로그와 **같다**. ② 의 root = ① 의 핸들, ③ 의 root = ② 의 핸들. `nSize` 0 → `0x1000` (인바운드·아웃바운드 둘 다). 상속 표시 = ② ③ 둘 다 `OBJ_INHERIT`(0x2) (① 은 없음). ③ 실패 때만 ② 핸들을 닫고 거짓. ① 핸들은 **안 닫고 kernelbase 전역(RVA 0x3ad980)에 둔다** (처음 한 번만 열고 다시 씀).
3. **(c) `ConnectNamedPipe`**: `NtFsControlFile(h, ev, 0, 0, iosb, 0x110008, 0,0,0,0)`. 동기면 이벤트·ApcContext 모두 비움, `OVERLAPPED` 면 `hEvent`·`Internal=0x103`·IOSB=OVERLAPPED 자신. **`STATUS_PIPE_CONNECTED`(0xC00000B2) 는 오류 status 라 `FALSE` + `ERROR_PIPE_CONNECTED`(535)** — 참이 아니다. `STATUS_PENDING`: 겹침이면 `FALSE`+`ERROR_IO_PENDING`(997), 동기 핸들이면 I/O 관리자가 안에서 기다려서 이 함수는 PENDING 을 못 본다.
4. **(d)** `WaitNamedPipeW` 는 `\DosDevices\pipe\` (**`\Device\NamedPipe\` 가 아니다**) 를 열고 `0x110018`, `PeekNamedPipe` `0x11400c`, `DisconnectNamedPipe` `0x110004` (임시 이벤트를 만들어 기다림), `SetNamedPipeHandleState` 는 `NtSetInformationFile` 클래스 `0x17`(8바이트) / `0x19`(16바이트).
5. **2(a) 만들기**: 방향은 **share 로만** 정한다 (2→들어옴 0, 1→나감 1, 3→양쪽 2, 그 밖엔 `0xC000000D`). 새 이름 → `0`/`FILE_CREATED(2)`. 이미 있는 이름 → access 검사 → 인스턴스 수 (`0xC00000AB`) → disposition 2 면 `0xC0000022` → share(방향) 다르면 `0xC0000022` → 성공 `0`/`FILE_OPENED(1)`. **종류·읽기·쿼터·시간은 첫 인스턴스와 비교 안 한다.** 빈 이름 + root(`\Device\NamedPipe\` 핸들) → 이름표에 안 올리는 이름 없는 파이프, `0`/2.
6. **2(b) 열기**: 없는 이름 `0xC0000034` · 인스턴스 0개 `0xC0000034` · **듣는 인스턴스 없음 `0xC00000AC`**(→ `ERROR_PIPE_BUSY` 231) · 방향과 안 맞는 access `0xC0000022` · 끝 `\` 루트 열기 `0`/1 · 서버 핸들을 root 로 한 빈 이름 → 같은 client 길. **서버가 `0x110008` 을 안 불러도 붙는다**: 새 인스턴스는 처음부터 LISTENING 이다 (access 에 데이터 권한 비트가 있으면).
7. **2(c) 닫기**: 한쪽 마지막 핸들이 닫히면 (CLEANUP) 상태 CONNECTED → **CLOSING(4)**. 그 뒤 상대 읽기는 자료 있으면 읽히고 없으면 `0xC000014B`(109), 상대 쓰기·듣기는 `0xC00000B1`(232). 기다리던 IRP: 상대의 대기 읽기와 닫는 쪽 대기열 → `0xC000014B`, 듣는 중에 서버가 닫으면 듣기 IRP → `0xC000014B`.
8. **3(a) READ**: 자료 없음 + 블로킹 → `0x103` (큐에 걸고 동기 핸들은 I/O 관리자가 `FileObject->Event` 를 기다림, sast/124 §4) · NOWAIT → `0xC00000D9` · 쓰는 끝 닫힘 + 자료 없음 → `0xC000014B` (**`END_OF_FILE` 아님**) · 닫힘 + 자료 있음 → 자료가 먼저 읽힘 · 바이트 꼴은 있는 만큼 주고 status 0, 메시지 꼴만 `0x80000005`.
9. **3(b) WRITE**: 상대가 안 붙음 `0xC00000B3` · 상대 닫힘 `0xC00000B1` · 끊김 `0xC00000B0` · 쿼터 안이면 status 0 + Information = 쓴 바이트 전부 · 쿼터 넘으면 블로킹은 `0x103` 으로 **기다림** (읽는 쪽이 비우면 끝남).
10. **3(c)** 1(c) 표와 같다 (`CONNECTED` 0xC00000B2 / `LISTENING` 은 PENDING 또는 NOWAIT 이면 0xC00000B3 / 붙었다 닫음 `CLOSING` 0xC00000B1).
11. **3(d)** `GetFileType` 은 `NtQueryVolumeInformationFile(FileFsDeviceInformation=4, 8바이트)` 를 부르고 `DeviceType == 0x11` 이면 `FILE_TYPE_PIPE`(3) 를 돌려준다. 이 질문은 **npfs 까지 안 가고 ntoskrnl 이 직접 답한다** (장치 객체의 `DeviceType 0x11`·`Characteristics`). `NtSetInformationFile` 은 클래스 `0x17`(+ 아무 일 안 하는 `4`)만, `NtQueryInformationFile` 은 4·5·6·7·9·0xe·0x12·0x17·0x18 만 받는다. 칸 모양은 PDB 에 타입이 없어 npfs 코드가 읽고 쓰는 칸으로 적었다 (3(d) 표).
12. **끝까지 못 닫은 것**: `IopCloseFile` 이 CLEANUP 을 보내는 조건 (이 프로세스 마지막 핸들인지 시스템 전체 마지막 핸들인지 — 자식이 핸들을 물려받았을 때 갈림), `WaitNamedPipeW` 의 타임아웃 status·깨우기 (`NpAddWaiter`/`NpTimerDispatch` 는 이번에 안 봄), 장치 `Characteristics` 의 최종 값. → "동적으로 확인할 것" 과 "다음 요청 후보".

## 질문별 답 [정적]

### 1(a). `CreateNamedPipeW` — Win32 인자 → `NtCreateNamedPipeFile` 14 인자

근거 파일: `ms/disassemble/kernelbase/CreateNamedPipeW.asm`. 인자 자리: 들어올 때 `lpName`=RCX, `dwOpenMode`=EDX(→EBX), `dwPipeMode`=R8D(→R15D), `nMaxInstances`=R9D, 그 뒤 `nOutBufferSize`=`[RBP+0x50]`, `nInBufferSize`=`[RBP+0x58]`, `nDefaultTimeOut`=`[RBP+0x60]`, `lpSecurityAttributes`=`[RBP+0x68]` (`+0x14 LEA RBP,[RAX-0x28]` 기준).

A 판: `kernel32!CreateNamedPipeA` (`ms/disassemble/kernel32/CreateNamedPipeA.asm`) 는 이름을 `Basep8BitStringToDynamicUnicodeString` (`+0x26/+0x2b`) 으로 바꾼 뒤 `CreateNamedPipeW` 를 부르고 (`+0x6a/+0x71`) 문자열을 풀고 (`+0x7e/+0x85`) 돌아온다. 뒤 네 인자는 스택에서 그대로 옮김 (`+0x2f~+0x66`). 변환 실패면 `-1` (`+0x9e`).

**먼저 하는 검사·준비 (syscall 전):**

| 순서 | 하는 일 | 실패 때 | 줄 |
|---|---|---|---|
| 1 | `nMaxInstances - 1 > 0xfe` (즉 0 또는 255 초과) | `STATUS_INVALID_PARAMETER`(0xC000000D) → `BaseSetLastNTError` → `-1` | `+0x47/+0x5b/+0x60` → `+0x399/+0x39e` |
| 2 | `RtlDosPathNameToNtPathName_U(lpName, &NtName, &FilePart, 0)` (`\\.\pipe\X` → `\??\pipe\X`) | `SetLastError(3)` (`ERROR_PATH_NOT_FOUND`) 하고 `-1` | `+0x80/+0x87`, `+0x8e → +0x304/+0x309` |
| 3 | OBJECT_ATTRIBUTES: `Length 0x30`, `RootDirectory 0`, `ObjectName = &NtName`, `Attributes 0x40`. `lpSecurityAttributes` 가 있고 `bInheritHandle≠0` 이면 `0x42` (`OBJ_INHERIT` 더함) | — | `+0xa1~+0xb7`, `+0x119~+0x12f` |
| 4 | 보안 서술자: `lpSecurityAttributes==NULL` 이거나 그 안 서술자가 NULL 이면 `RtlDefaultNpAcl` → `RtlCreateSecurityDescriptor(rev 1)` → `RtlSetDaclSecurityDescriptor` 로 **기본 DACL 서술자를 만들어 OA 에 단다.** 있으면 그것을 단다. → **OA.SecurityDescriptor 는 거의 항상 NULL 이 아니다.** | `RtlDefaultNpAcl` 음수면 NtName 해제 후 그 status 로 실패 (`+0xda → +0x317`) | `+0xc5~+0x117`, `+0x135 JZ +0xc5` |
| 5 | `dwOpenMode & 0x3ef3fffc ≠ 0` 또는 `dwPipeMode & 0xfffffff0 ≠ 0` (허용 비트 = `0xc10c0003`: `WRITE_THROUGH`·`OVERLAPPED`·`ACCESS_SYSTEM_SECURITY`·`0x80000`·`0x40000`·방향 2비트 / `dwPipeMode` 는 `0xf`) | `0xC000000D` | `+0x162~+0x17b` → `+0x353~+0x39e` |
| 6 | `dwOpenMode & 3 == 0` (방향 없음) | `0xC000000D` | `+0x18f/+0x198/+0x19e/+0x1a1` → `+0x353` |

(`RtlDefaultNpAcl` 은 ntdll 안에서 `NtQueryInformationToken` 을 세 번 부른다: 핸들 `-6` (`NtCurrentThreadEffectiveToken` 가짜 핸들 `[추측: R13=0 이라 R12=-6]`), 정보 클래스 4 · `0x1d` · `0x1f` — `ms/disassemble/ntdll/RtlDefaultNpAcl.asm` +0x9e/+0xc5/+0x375. 에뮬레이터 로그에 이 호출이 `NtCreateNamedPipeFile` 앞에 나와야 한다.)

**14 인자 표** (`NtCreateNamedPipeFile` 호출: `+0x22e/+0x235`):

| # | `NtCreateNamedPipeFile` 인자 | 값 · 바꾸는 규칙 | 줄 |
|---|---|---|---|
| 1 | `FileHandle` | 지역 변수 `[RSP+0x78]` 의 주소 | `+0x224` |
| 2 | `DesiredAccess` | `(dwOpenMode & 0x010c0000) \| 방향별 기본값`. 기본값: `dwOpenMode&3` = 1 (`PIPE_ACCESS_INBOUND`) → `0x80100000`, 2 (`OUTBOUND`) → `0x40100000`, 3 (`DUPLEX`) → `0xc0100000`. `0x80000` = `FILE_FLAG_FIRST_PIPE_INSTANCE`, `0x40000`·`0x01000000` 은 `WRITE_DAC`·`ACCESS_SYSTEM_SECURITY` 가 같이 감 | `+0x1a7`(양쪽), `+0x2e1`(들어옴), `+0x339`(나감), `+0x1ef/+0x1f8` |
| 3 | `ObjectAttributes` | 위 표 3·4. `Root 0` | `+0x211` `LEA R8,[RBP-0x60]` |
| 4 | `IoStatusBlock` | 지역 `[RBP-0x30]` (Information 은 `[RBP-0x28]`) | `+0x208` |
| 5 | `ShareAccess` | 들어옴 **2** (`FILE_SHARE_WRITE`), 나감 **1** (`FILE_SHARE_READ`), 양쪽 **3** | `+0x183/+0x2e6/+0x33e` → `[RSP+0x20]` `+0x229` |
| 6 | `CreateDisposition` | `dwOpenMode` 의 `0x80000` 비트가 서 있으면 **2** (`FILE_CREATE`), 없으면 **3** (`FILE_OPEN_IF`) | `+0x1d4~+0x1e8` (`SHR ECX,0x13; NOT; AND 1; OR 2`) → `+0x220` |
| 7 | `CreateOptions` | `FILE_FLAG_OVERLAPPED`(0x40000000) 가 **없으면 `0x20`** (`FILE_SYNCHRONOUS_IO_NONALERT`), 있으면 0. `FILE_FLAG_WRITE_THROUGH`(0x80000000) 가 서 있으면 `0x2` (`FILE_WRITE_THROUGH`) 를 더함 | `+0x137~+0x14d` → `+0x21b` |
| 8 | `NamedPipeType` | `PIPE_TYPE_MESSAGE`(0x4) → 비트 0 (=1), `PIPE_REJECT_REMOTE_CLIENTS`(0x8) → 비트 1 (=2) | `+0x1b1/+0x1b7`, `+0x1c3/+0x1cd/+0x1d1` → `+0x215` |
| 9 | `ReadMode` | `(dwPipeMode>>1)&1` (`PIPE_READMODE_MESSAGE` 0x2) | `+0x1ba/+0x1bf` → `+0x20c` |
| 10 | `CompletionMode` | `dwPipeMode & 1` (`PIPE_NOWAIT` 0x1) | `+0x1c6/+0x1c9` → `+0x203` |
| 11 | `MaximumInstances` | `nMaxInstances == 255` (`PIPE_UNLIMITED_INSTANCES`) 면 `0xffffffff`, 아니면 그 수 | `+0x66~+0x79` → `+0x1fe` |
| 12 | `InboundQuota` | `nInBufferSize` **그대로** (0 이어도 0) | `+0x1f5/+0x1fa` |
| 13 | `OutboundQuota` | `nOutBufferSize` **그대로** | `+0x1e5/+0x1eb` |
| 14 | `DefaultTimeout` | **늘 NULL 이 아닌 포인터** (지역 `[RBP-0x78]`). 값: `nDefaultTimeOut == 0` → `-0x7a120` (= −500,000 × 100ns = 상대 50 ms), 아니면 `nDefaultTimeOut × −0x2710` (ms → 100ns, 음수 = 상대) | `+0x150~+0x168`, `+0x2f1` (`IMUL`), `+0x1dd` → `[RSP+0x68]` |

**요청 원문 `[추측]` 셋 대조**

| 원문 `[추측]` | 판정 | 근거 |
|---|---|---|
| disposition `0x2` ↔ `FILE_FLAG_FIRST_PIPE_INSTANCE` | **맞다** (access 의 `0x80000` 비트와 같은 비트에서 나옴. 로그의 12 = 0x80000 선 access 12) | 위 표 6번, `+0x1d4` 가 `dwOpenMode>>0x13` |
| options `0x0` ↔ `FILE_FLAG_OVERLAPPED` | **맞다** | 위 표 7번, `+0x137~+0x14d` (`SHR 0x19`, `AND 0x20`) |
| 종류 `0x2` 비트 ↔ `PIPE_REJECT_REMOTE_CLIENTS` | **맞다** (종류 비트 0 = 메시지, 비트 1 = 원격 거부) | 위 표 8번 |

**돌아오는 값**

| 상황 | 반환 | `GetLastError` | 줄 |
|---|---|---|---|
| status ≥ 0 이고 `IoStatusBlock.Information == 1` (`FILE_OPENED`: 이미 있던 이름에 인스턴스를 더함) | 핸들 | `0xb7` (`ERROR_ALREADY_EXISTS`) | `+0x29c → +0x2fd/+0x302 → +0x2a5` |
| status ≥ 0 이고 `Information ≠ 1` (`FILE_CREATED`=2) | 핸들 (`[RSP+0x78]`) | `0` | `+0x2a3~+0x2b1` |
| status < 0 | `-1` (`INVALID_HANDLE_VALUE`) | `RtlNtStatusToDosError(status)` (`BaseSetLastNTError` = `ms/disassemble/kernelbase/BaseSetLastNTError.asm` +0x6/+0x16) | `+0x29a → +0x2b8 → +0x2ba/+0x2bf` |
| status 가 `0xC00000BB`(NOT_SUPPORTED) 또는 `0xC0000010`(INVALID_DEVICE_REQUEST) | status 를 **`0xC0000033`(OBJECT_NAME_INVALID → 123)** 으로 바꿔서 위 실패 길 | 123 | `+0x23c~+0x24c` → `+0x349` |

(에뮬레이터가 지금 "status 0 + 핸들 칸 안 씀" 으로 대답해서 `CreateNamedPipeA` 가 0 을 받은 것은 이 표대로다: `+0x2b1 MOV RAX,[RSP+0x78]` 가 안 쓴 칸을 그대로 돌려준다.)

### 1(b). `CreatePipe`

근거 파일: `ms/disassemble/kernelbase/CreatePipe.asm`. 인자: `hReadPipe`=RCX(→R15), `hWritePipe`=RDX(→R14), `lpPipeAttributes`=R8(→RBX), `nSize`=R9D.

**먼저**: `nSize==0` 이면 쿼터 `0x1000` (`+0x35 MOV ESI,0x1000; +0x3e CMOVNZ ESI,R9D`) — **인바운드·아웃바운드 둘 다 이 값** (`+0xbf/+0xc3` 이 둘을 같이 `ESI` 로 채움). 기본 시간 = `[RBP+0x2f]` 에 **`-0x47868c00` (= −1,200,000,000 × 100ns = 상대 120초)** (`+0x26`), ② 의 14번째 인자로 포인터를 넘김 (`+0xb5`).

| syscall | 인자 (asm 에서 센 값) | root | 줄 | 로그와 |
|---|---|---|---|---|
| ① `NtOpenFile` | 핸들 칸 `[RBP-0x21]`, **access `0x80100000`**, OA{`Length 0x30`, Root 0, 이름 **`\Device\NamedPipe\`** (Length `0x24`, **MaximumLength 는 0 으로 둠** — `+0x1d4` 가 Length 만 세움), **Attributes 0** (대소문자 구분!·상속 없음), SD/SQOS 0}, **share `3`**, **options `0x20`** | 0 | `+0x1b4~+0x1fc`(OA), `+0x1e7`(access), `+0x201`(share), `+0x1bb`(options), `+0x209/+0x210`(호출) | 같다 |
| ② `NtCreateNamedPipeFile` | access **`0x80100100`**, OA{Root = **① 의 핸들** (전역), 이름 **빈 문자열** (Length 0, Buffer NULL — `+0x6e/+0x72`), Attributes `0x40` 또는 `0x42`, SD = `lpSecurityAttributes->lpSecurityDescriptor` 또는 0}, share **3**, disposition **2** (`FILE_CREATE`), options **`0x20`**, 종류 **0**, 읽기 **0**, 완료 **0**, 최대 **1**, 인바운드 쿼터 = 아웃바운드 쿼터 = `nSize 또는 0x1000`, 시간 포인터 (−120초) | **① 의 핸들** (`+0x88 MOV RAX,[전역]` → `+0x93 [RBP-1]`) | `+0xba`(access), `+0xef/+0xe7/+0xdf/+0xda/+0xd5/+0xd0/+0xcc/+0xc3/+0xbf`(스택 인자), `+0xff/+0x106`(호출) | 같다 |
| ③ `NtOpenFile` | 핸들 칸 `[RBP+0x27]`, access **`0x40100080`**, OA{Root = **② 의 핸들** (`+0x113 MOV RAX,[RBP-0x29]` → `+0x11b`), 이름 **빈 문자열**, Attributes `0x40`/`0x42`, SD 같음}, share **3**, options **`0x60`** (`0x20 \| 0x40`) | **② 의 핸들** | `+0x12f`(access), `+0x14e`(share), `+0x127`(options), `+0x156/+0x15d`(호출) | 같다 |

로그의 "② ③ root 는 0" 은 에뮬레이터가 ① 핸들을 안 줘서 그렇다 — 진짜는 ① 의 핸들이 ② 에, ② 의 핸들이 ③ 에 간다.

| 질문 | 답 | 줄 |
|---|---|---|
| 상속 표시는 어느 syscall 로? | `lpPipeAttributes != NULL` 이고 `bInheritHandle != 0` 이면 `Attributes = 0x42` (`OBJ_CASE_INSENSITIVE`\|`OBJ_INHERIT`), 아니면 `0x40`. **② 와 ③ 둘 다** (= 읽기 핸들과 쓰기 핸들 둘 다 상속 가능). ① 은 안 붙음. 같은 구조체의 `lpSecurityDescriptor` 는 ② ③ 의 OA.SecurityDescriptor 로 감 | `+0x77/+0x7a → +0x19e~+0x1af` (`NEG; SBB; AND 2; ADD 0x40`), `+0x80/+0x85`(없을 때) |
| ① 은 매번 여나? 끝에 닫나? | **처음 한 번만 연다.** `+0x46` 에서 전역 `[0x1803ad980]` (RVA `0x3ad980`) 가 0 이면 `+0x334` 로 가서 ① 을 열고 `LOCK CMPXCHG` 로 전역에 넣는다 (`+0x21f`). 다른 스레드가 먼저 넣었으면 내 핸들을 `NtClose` (`+0x232/+0x239`). **끝에 닫지 않는다.** (`refs` 로 확인: 이 전역을 만지는 곳은 `CreatePipe` 의 세 줄 뿐 — `ms/disassemble/kernelbase/_refs/0x3ad980.tsv`. `.asm` 에 보이는 `InteractiveUserNameSpaceSeparation+0x8` 이름표는 "가장 가까운 앞 이름" 일 뿐 별개 변수다.) | `+0x46`, `+0x88`, `+0x21f`, `+0x232` |
| 중간에 실패하면? | ① 실패 → `BaseSetLastNTError(status)` + `FALSE`, 출력 인자 안 건드림 (`+0x217 → +0x243/+0x245/+0x24a`). ② 실패 → 같은 길 (`+0x10d`). ③ 실패 → **② 가 만든 읽기 핸들만 `NtClose`** (`+0x251/+0x258`, RCX 는 `+0x162` 에서 `[RBP-0x29]` 로 채움) 한 뒤 `BaseSetLastNTError` + `FALSE`. ① 은 안 닫음 (전역에 남음). 출력 `*hReadPipe`·`*hWritePipe` 는 둘 다 성공해야 쓴다 | `+0x162/+0x166/+0x16a`, `+0x25d/+0x25f` |
| 성공 때 돌려주는 것 | `*hReadPipe = ②`, `*hWritePipe = ③`, 반환 `1` (`+0x170~+0x179`, `+0x173 MOV EAX,ESI` 가 1) | `+0x170~+0x179` |
| 에뮬레이터 로그 "참인데 두 핸들 0" | 세 syscall 이 status 0 으로 와서 `+0x162` 이후 곧장 성공 길을 탄 결과 (핸들 칸을 안 써서 0) — 이 코드대로다 | |

(npfs 쪽에서 본 이 파이프의 모양: ② 는 share 3 이라 `NamedPipeConfiguration = 양쪽(2)`, 바이트 꼴, 최대 1, 이름표에 안 올림. 읽기 핸들 = 서버 끝 (access `GENERIC_READ`), 쓰기 핸들 = 클라이언트 끝 (access `GENERIC_WRITE`). 자세히는 2(a)(b).)

### 1(c). `ConnectNamedPipe`

근거 파일: `ms/disassemble/kernelbase/ConnectNamedPipe.asm`. 인자: `hNamedPipe`=RCX(→RDI), `lpOverlapped`=RDX(→RBX).

| 모양 | `NtFsControlFile` 인자 | 줄 |
|---|---|---|
| 동기 (`lpOverlapped == NULL`) | `FileHandle=h`, `Event=NULL`, `ApcRoutine=NULL`, `ApcContext=NULL`, `IoStatusBlock=지역 [RSP+0x50]`, `FsControlCode=0x110008`, 입출력 버퍼 모두 NULL·0 | `+0x1b → +0xc0~+0xca → +0x3d~+0x63` |
| `OVERLAPPED` | `Event = lpOverlapped->hEvent`, `ApcRoutine=NULL`, `ApcContext = (hEvent 의 맨 아랫 비트가 0 이면 lpOverlapped, 1 이면 NULL)`, `IoStatusBlock = lpOverlapped 자신`. 부르기 전에 `lpOverlapped->Internal = 0x103` | `+0x21`(`TEST byte [RDX+0x18],1`), `+0x2b`(`MOV [RDX],0x103`), `+0x35`(`CMOVZ`), `+0x39`(`RDX=[RDX+0x18]`) |

**status → 돌려주는 값** (npfs 쪽 status 는 2(b)/3(c) 표와 같이 보라):

| `NtFsControlFile` 반환 | `ConnectNamedPipe` 반환 | `GetLastError` | 줄 |
|---|---|---|---|
| `0` (붙었음) | `TRUE` | (건드리지 않음) | `+0x74/+0x76 → +0x8d/+0x92/+0x94` |
| 그 밖의 **음수 (오류)** — `0xC00000B2` `STATUS_PIPE_CONNECTED` 포함 | **`FALSE`** | `RtlNtStatusToDosError(status)`: `0xC00000B2`→**535** `ERROR_PIPE_CONNECTED`, `0xC00000B1`→232, `0xC00000B3`→536, `0xC00000AF`→1, `0xC000014B`→109, `0xC0000120`→995 | `+0x76 → +0x78/+0x7a/+0x7f` |
| `0x103` (`STATUS_PENDING`) + `OVERLAPPED` | `FALSE` | **997** (`ERROR_IO_PENDING`: `RtlNtStatusToDosErrorNoTeb +0xc` 가 `0x103` 을 `0x3e5` 로) | `+0x8d/+0x92 → +0x78` |
| `0x103` + 동기 호출 | **기다린다**: `NtWaitForSingleObject(hNamedPipe, alertable=FALSE, 시간 NULL)` 한 뒤 `IoStatusBlock.Status` 로 위 표를 다시 탄다 | — | `+0x9b/+0xa0 → +0xa2~+0xbe` |

→ **요청서 질문 답**: "`STATUS_PIPE_CONNECTED` → 참 + `ERROR_PIPE_CONNECTED` 류인지" 는 **참이 아니라 `FALSE` + `ERROR_PIPE_CONNECTED`(535)** 다. kernelbase 안에 `0xC00000B2` 를 특별 취급하는 줄은 없다 (`+0x76 JNS` 로 음수는 전부 같은 실패 길). "동기면 기다리나": 동기 **핸들** (`FILE_SYNCHRONOUS_IO_NONALERT` 로 만든 핸들, 곧 `FILE_FLAG_OVERLAPPED` 없이 만든 서버 핸들) 은 **I/O 관리자가** `NtFsControlFile` 안에서 기다린다 (sast/124 §4: `IopSynchronousServiceTail +0x3ca~+0x3e1` → `IopWaitForSynchronousIo` → `FileObject->Event`). 그래서 kernelbase 의 `NtWaitForSingleObject` 줄은 **비동기 핸들인데 `lpOverlapped=NULL` 로 부른 경우**에만 탄다.
(참고: 오류 status 로 PENDING 없이 끝나면 `IopCompleteRequest` 가 **IOSB 를 안 쓴다** — sast/124 표 "오류 / 동기·비동기 핸들". 그래서 `STATUS_PIPE_CONNECTED` 일 때 `lpOverlapped` 의 `Internal` 은 `0x103` 으로 남을 수 있다 `[추측: 겹침 + 즉시 오류]`. kernelbase 는 이 경우 IOSB 를 읽지 않는다.)

### 1(d). `WaitNamedPipeW` · `PeekNamedPipe` · `SetNamedPipeHandleState` · `DisconnectNamedPipe` (+ `GetNamedPipeInfo`·`GetNamedPipeHandleStateW` 이름만)

| 함수 | 제어 코드 / 클래스 | 입력 | 출력 | 어느 핸들에 | 줄 |
|---|---|---|---|---|---|
| `WaitNamedPipeW(name, ms)` | `NtFsControlFile` **`0x110018`**, 이벤트 없음 (동기) | 힙에 만든 버퍼 `[+0 시간(LARGE_INTEGER) +8 이름길이(ULONG) +0xc 시간지정(BOOLEAN) +0xe 이름…]`, 크기 `이름길이 + 0xe` | 없음 | **새로 연 `\DosDevices\pipe\` 핸들** (`NtOpenFile` access **`0x100080`**, share 3, options `0x20`, Attributes `0x40`). 부른 뒤 `NtClose`. **`\Device\NamedPipe\` 가 아니라 `\DosDevices\pipe\`** (kernelbase `.rdata` RVA `0x2a0ab8`) | `+0x122~+0x189`(열기), `+0x25c/+0x264`(`0x110018`, `[RSP+0x30]=버퍼`, `[RSP+0x38]=길이`), `+0x269/+0x270`, `+0x299/+0x2a0` |
| 〃 시간 규칙 | `nTimeOut==0` → `Timeout=0`, 시간지정=FALSE (파이프의 기본 시간 쓰라) / `0xffffffff` → `Timeout = 0x8000000000000000`, TRUE (영원) / 그 밖 → `ms × −10000`, TRUE | | | | `+0x1f0~+0x21b`, `+0x311~+0x31b`, `+0x36d~+0x372` |
| 〃 이름 | `\\.\pipe\X` 만 쓴다: 앞 9글자 `\\.\pipe\` 를 떼고 (`+0x11d ADD [RBP-0x41],0x12`, 길이 −0x12) 나머지를 버퍼에 복사. `/` 는 `\` 로 바꿈 (`+0x9d`). 경로 종류가 `\\.\`(6) 이 아니고 `\\서버\pipe\` (원격, 범위 밖) 도 아니면 `0xC000003B`(→161) | | | | `+0xca → +0xd5~+0xe1`, `+0x34e` |
| 〃 결과 | status ≥ 0 → `TRUE`, 음수 → `BaseSetLastNTError` + `FALSE` (PENDING 검사 없음 — 동기 핸들이라 I/O 관리자가 기다림) | | | | `+0x2a5/+0x2a7 → +0x30a`, `+0x2a9~+0x2b0` |
| `PeekNamedPipe(h, buf, n, &read, &avail, &left)` | `NtFsControlFile` **`0x11400c`**, 임시 이벤트 (`NtCreateEvent` 0x1f0003, 알림형, 꺼진 채) | 없음 | 힙 버퍼 `n + 0x10` 바이트 (머리 0x10: `+4 ReadDataAvailable`, `+0xc MessageLength` 를 읽음) | 파이프 핸들 (어느 끝이든) | `+0x35`(`n+0x10`), `+0x4f/+0x56`, `+0xb1/+0xb8`, `+0xdd`(코드), `+0xfd/+0x104`, `+0x11e/+0x125`(PENDING 이면 이벤트 기다림), `+0x139 CMP 0x80000005`(버퍼 모자람은 성공으로 침), `+0x15c~+0x18d`(결과 복사) |
| `SetNamedPipeHandleState(h, &mode, &maxcnt, &timeout)` | `NtSetInformationFile` 클래스 **`0x17`** (`FilePipeInformation`), 8바이트 `{ReadMode = (mode>>1)&1, CompletionMode = mode&1}` — `*mode & ~3` 이 있으면 `SetLastError(87)` 하고 `FALSE` (syscall 없음) | | | 파이프 핸들 | `+0x32/+0x38 → +0xe4/+0xe9`, `+0x47~+0x6f` |
| 〃 `maxcnt`/`timeout` | 둘 중 하나라도 주면 클래스 **`0x19`** (`FilePipeRemoteInformation`) 16바이트 `{CollectDataTime = ms×−10000, MaximumCollectionCount}` 를 `NtQueryInformationFile`(`+0xc9`) 한 뒤 `NtSetInformationFile`(`+0xb33da`, 조각) | | | | `+0xa4~+0xd7`, `+0x102~+0x10b`, 조각 `+0xb33aa~+0xb33ee` |
| `DisconnectNamedPipe(h)` | `NtFsControlFile` **`0x110004`**, 임시 이벤트 (`+0x59` `NtCreateEvent` 0x1f0003, 알림형, 꺼진 채), PENDING 이면 이벤트 기다림, 끝에 `NtClose(이벤트)` | 없음 | 없음 | **서버 끝 핸들** (npfs 는 FCB 가 아니거나 `FsContext2` 아랫비트 0 이면 `0xC00000B0` (`NpCommonFileSystemControl +0xce/+0xd8 → +0x1f4`), 서버 표시 비트가 없는 클라이언트 핸들이면 `0xC00000AF` (`+0xe9 → +0x348`) — kernelbase 는 `FALSE` + 233 / 1) | `+0x59/+0x60`, `+0x94`(코드), `+0xa1/+0xa8`, `+0xbf/+0xc6`, `+0xd7/+0xde`, 결과 `+0xe3~+0x10b` |
| `GetNamedPipeInfo` / `GetNamedPipeHandleStateW` | `NtQueryInformationFile` 클래스 `0x18` (`FilePipeLocalInformation`, 0x28 바이트) / `0x17` / `0x19` | | | | `GetNamedPipeInfo +0x55`, `GetNamedPipeHandleStateW +0xa5/+0x105/+0x14a` |

### 2. npfs 들머리 — `MajorFunction` 표

근거: `ms/disassemble/npfs/DriverEntry.asm` (`+0x92~+0x142` 이 `[RSI+오프셋] = 함수` 를 채움, RSI = DriverObject, `MajorFunction` 은 `_DRIVER_OBJECT +0x70` — PDB 로 확인). `DriverEntry +0x18/+0x1d` 가 `NpReadParameters`·`NpInitializeAliases` (레지스트리에서 별칭 표를 읽음, 이 요청 범위 밖), `+0x3c` 가 `ExAllocatePool2` 로 `FAST_IO_DISPATCH` (0xe0 바이트, `+0x67 MOV [RDI],EBX`) 를 만들고 `+0x149` 가 `NpCreateDevice` → `IoCreateDevice(장치종류 0x11, 이름 \Device\NamedPipe, 특성 0x20000)` (`NpCreateDevice +0x4d/+0x58/+0x60`).

| IRP_MJ | 번호 | `DriverEntry` 줄 | npfs 함수 (RVA) |
|---|---|---|---|
| CREATE | 0 | `+0x8b/+0x92` | `NpFsdCreate` (0xe3c0) — **클라이언트 열기** |
| CREATE_NAMED_PIPE | 1 | `+0x96/+0x9d` | `NpFsdCreateNamedPipe` (0xeba0) — **서버 만들기** |
| CLOSE | 2 | `+0xa1/+0xa8` | `NpFsdClose` (0xd770) |
| READ | 3 | `+0xaf/+0xb6` | `NpFsdRead` (0x11d70) |
| WRITE | 4 | `+0xbd/+0xc4` | `NpFsdWrite` (0x12540) |
| QUERY_INFORMATION | 5 | `+0xcb/+0xd2` | `NpFsdQueryInformation` (0x14640) |
| SET_INFORMATION | 6 | `+0xd9/+0xe0` | `NpFsdSetInformation` (0x14e10) |
| FLUSH_BUFFERS | 9 | `+0x103/+0x10a` | `NpFsdFlushBuffers` (0xe260) |
| QUERY_VOLUME_INFORMATION | 0xa | `+0xe7/+0xee` | `NpFsdQueryVolumeInformation` (0x16670) |
| DIRECTORY_CONTROL | 0xc | `+0x111/+0x118` | `NpFsdDirectoryControl` (0x13b60) |
| FILE_SYSTEM_CONTROL | 0xd | `+0x11f/+0x126` | `NpFsdFileSystemControl` (0xf5f0) |
| CLEANUP | 0x12 | `+0xf5/+0xfc` | `NpFsdCleanup` (0xf040) |
| QUERY_SECURITY / SET_SECURITY | 0x14 / 0x15 | `+0x12d/+0x134`, `+0x13b/+0x142` | `NpFsdQuerySecurityInfo` (0x14420) / `NpFsdSetSecurityInfo` (0x154b0) |
| Fast I/O | `FastIoRead` (`+0x10`) / `FastIoWrite` (`+0x18`) | `+0x69/+0x70`, `+0x79/+0x87` | `NpFastRead` (0x11a50) / `NpFastWrite` (0x11980) — **같은 status 규칙**, 단 블로킹이 필요하면 "처리 안 함" 을 돌려 IRP 길로 넘긴다 (`NpFastRead` 에서 NOWAIT 아닌 빈 읽기 → 락 풀고 0 반환) |

`DEVICE_CONTROL`(0xe) 등 안 채운 칸은 기본 처리 (`STATUS_INVALID_DEVICE_REQUEST`) `[추측: I/O 관리자 기본 — 이 요청 범위 밖]`.

**IRP 로 가는 길 (ntoskrnl, "넘기는 꼴 한 줄")**: `NtCreateNamedPipeFile` (`ms/disassemble/ntoskrnl/NtCreateNamedPipeFile.asm`) 은 14 인자 중 8~13 을 지역 `NAMED_PIPE_CREATE_PARAMETERS` (`[RSP+0x80~+0x94]`, `+0xa0~+0xed`) 에 옮기고, 14번째 (시간 포인터) 가 NULL 이 아니면 `TimeoutSpecified=1` (`+0x4b`) 과 `DefaultTimeout = *포인터` (`+0x87~+0x92`) 를 채운다. **유저 모드 호출일 때만** (`GS:[0x188]` 스레드의 `+0x232` 가 0 이 아닐 때, `+0x5c/+0x62`) 포인터 4바이트 정렬 검사 (`+0x68 TEST DL,3` → `ExRaiseDatatypeMisalignment`) 와 읽기 검사 (`+0x73~+0x85`) 를 한다. 그다음 `IopCreateFile` 을 **`CreateFileType = 1`** (`+0x10e MOV [RSP+0x58],1`), **`ExtraCreateParameters = &그 지역 구조체`** (`+0x109 MOV [RSP+0x60],RAX`), `EaBuffer`·`EaLength`·`AllocationSize`·`FileAttributes` 0 으로 부른다 (`+0x14c/+0x151`). `IopCreateFile +0x2bf/+0x2c6` 이 그 `1` 을 `OPEN_PACKET +0x78` 에, `+0x2c9/+0x2d1` 이 포인터를 `+0x80` 에 넣고, `IopParseDevice +0xd31~+0xd36` 이 `+0x78 ≠ 0` 이면 `+0xfa3` 으로 가서 `CMP EAX,1; JNZ +0x21fc` (1 아니면 `0x13`=메일슬롯), **1 이면 `MajorFunction = 1` (`+0xfac MOV [R13],AL`)** 로 하고 `+0xfb0/+0xfb7` 이 `IO_STACK_LOCATION +0x20 = OPEN_PACKET +0x80` (파라미터 포인터) 을 넣는다. 그래서 npfs `NpFsdCreateNamedPipe` 는 스택 `+0x20` 에서 같은 구조체 포인터를 읽는다 (`NpFsdCreateNamedPipe +0x6c/+0x74`: `MOV RAX,[R13+0x20]` → `[RBP-0x9]`). IRP 스택의 `Options` (`+0x10`) 은 `상위 1바이트 = disposition`, 아래 24비트 = `CreateOptions` (`IopParseDevice +0xd6d~+0xd7d`: `SHR ECX,0x18 … OR`), `ShareAccess` 는 `+0x1a` (`+0xd8a`).
**ntoskrnl 은 14 인자의 의미를 검사하지 않는다** (값 검사는 전부 npfs 몫 — 아래).

### 2(a). 서버 쪽 만들기 (`NpFsdCreateNamedPipe` → `NpCreateNewNamedPipe` / `NpCreateExistingNamedPipe`)

근거 파일: `ms/disassemble/npfs/NpFsdCreateNamedPipe.asm`, `NpCreateNewNamedPipe.asm`, `NpCreateExistingNamedPipe.asm`, `NpCreateCcb.asm`, `NpCreateFcb.asm` (`.c` 로 먼저 읽고 `.asm` 으로 다시 확인). 이 요청은 `CreateOptions` 의 `FILE_DIRECTORY_FILE`(1) 갈래 (`NpCreateNamedPipePrefix`) 는 안 본다.

**공통 앞부분** (`NpFsdCreateNamedPipe`): 파일 객체의 이름 (`FileObject->FileName`, +0x58/+0x60) 과 `RelatedFileObject` (+0x40) 를 읽는다 (`+0x57~+0x74`, `+0xef/+0xf2` 가 `TEST RDI,RDI; JZ`). 이름은 먼저 `NpTranslateAlias` (`+0xdd/+0xe2`) 를 지난다 — 이름이 `LOCAL\` 로 시작하는 컨테이너 별칭이 아니면 그대로 통과 `[정적: NpTranslateAlias.c 첫머리 — 자세히는 안 봄]`.

| 요청 종류 | 조건 | status | `Information` | 기다림 | 줄 |
|---|---|---|---|---|---|
| **새 이름** (이름표에 없음) | `RelatedFileObject == NULL`, 이름 길이 > 2 바이트이고 첫 글자 `\`, `NpFindPrefix` 가 못 찾음. (루트 SD 가 있으면 `SeAccessCheck(DesiredAccess=2)` 통과해야 함 — 실패하면 그 status, 보통 `0xC0000022`) → `NpCreateNewNamedPipe` | **`0`** | **`2`** (`FILE_CREATED`) | 아니오 | `NpFsdCreateNamedPipe +0x154~+0x15f`(이름 검사), `+0x195`(`NpFindPrefix`), `+0x1e6~+0x25d`(접근 검사), `+0x30b/+0x310`(호출) ; `NpCreateNewNamedPipe +0x2f8/+0x2fb` (`status 0`, `Information=2`) |
| 새 이름 + `disposition == 1` (`FILE_OPEN`) | | `0xC0000034` (`OBJECT_NAME_NOT_FOUND` → 2) | 0 | 아니오 | `NpCreateNewNamedPipe +0x6f/+0x72 → +0x3ad` |
| 새 이름인데 **`TimeoutSpecified==0`**, **`MaximumInstances==0`**, **`DefaultTimeout ≥ 0`** 중 하나 | (kernelbase 는 항상 `TimeoutSpecified=1`·음수 시간을 주므로 직접 `NtCreateNamedPipeFile` 을 부를 때만) | `0xC000000D` | 0 | | `NpCreateNewNamedPipe +0x4a/+0x4e`, `+0x54/+0x5b`, `+0x61/+0x65` → `+0x3b4` |
| 〃 **방향(share)이 1·2·3 이 아님** | | `0xC000000D` | 0 | | `+0x78~+0x85`, `+0x375~+0x37d` → `+0x3b4` |
| 〃 **바이트 꼴인데 `ReadMode==메시지`** (`NamedPipeType` 비트0=0 이고 `ReadMode==1`) | | `0xC000000D` | 0 | | `+0x8d~+0x9a` → `+0x3b4` |
| 〃 이름이 `\` 한 글자뿐 (`NpCreateFcb` 가 `\` 를 뗀 뒤 2바이트 미만) | | `0xC000000D` | 0 | | `NpCreateFcb.asm` 앞머리 |
| **이미 있는 이름** (`NpFindPrefix` 가 FCB(`0x202`) 를 찾고 남은 이름 길이 0) | → `NpCreateExistingNamedPipe` (`+0x3cd/+0x3d2`). 남은 이름이 있으면 `0xC0000033` (`+0x34c/+0x350 → +0x43e → +0x3e1`) | 아래 세 줄 | | | |
| 〃 파이프 SD 에서 `FILE_CREATE_PIPE_INSTANCE`(= `DesiredAccess \| 4`) 권한 없음 | | `SeAccessCheck` 가 채운 값 (보통 `0xC0000022`) | 0 | 아니오 | `NpCreateExistingNamedPipe +0xa0/+0xa7`, `+0x12d/+0x130 → +0x31f` |
| 〃 **인스턴스 수 넘음** (`FCB+0x78 ≥ FCB+0xfc`, 곧 현재 ≥ 첫 만들 때 정한 최대) | | **`0xC00000AB`** (`INSTANCE_NOT_AVAILABLE` → 231 `ERROR_PIPE_BUSY`) | 0 | 아니오 | `+0x136/+0x13c/+0x13f → +0x267` |
| 〃 **`disposition == 2`** (`FILE_CREATE` = `FILE_FLAG_FIRST_PIPE_INSTANCE`) | 인스턴스 수 검사를 **통과한 뒤** | **`0xC0000022`** (`ACCESS_DENIED` → 5) | 0 | 아니오 | `+0x145/+0x14a → +0x293/+0x299` |
| 〃 **방향(share)이 첫 인스턴스와 다름** (FCB 의 `NamedPipeConfiguration` 0·1·2 ↔ share 2·1·3 비교) | | **`0xC0000022`** | 0 | 아니오 | `+0x153~+0x17a → +0x293` |
| 〃 **종류·읽기·쿼터·최대 수·시간이 첫 인스턴스와 다름** | **비교하는 줄이 없다.** 두 번째 인스턴스의 `ReadMode`·`CompletionMode`·쿼터는 **자기 CCB 에만** 들어가고 (`+0x180~+0x1c6`), 종류·최대 수·시간·방향은 첫 인스턴스(FCB) 것이 그대로 쓰인다 | `0` | **`1`** (`FILE_OPENED`) | 아니오 | `+0x244/+0x247` (`status 0`, `Information=1`) |
| **이름 없는 파이프**: `RelatedFileObject` = **루트(`\Device\NamedPipe\`) 핸들** + **빈 이름** | 관련 파일 객체의 `FsContext` 노드 종류가 `0x206`(루트 DCB) 이고 `FsContext2` 의 아랫 1비트가 서 있어야 함 → 이름 길이 0 → `NpCreateNewNamedPipe` | `0` | `2` | 아니오 | `NpFsdCreateNamedPipe +0xf4~+0x10b`(루트 검사, 실패 시 `0xC0000033`), `+0x318~+0x329`(빈 이름 → `+0x1b9` → `+0x2a5` → `+0x2ae`), `+0x30b/+0x310`. 이름표에는 안 올라간다 (`NpCreateFcb` 가 이름 길이 0 이면 `RtlInsertUnicodePrefix` 를 건너뜀) |
| 빈 이름인데 root 가 **루트 DCB 가 아님** | | `0xC0000033` | 0 | | `+0x106` (`MOV EAX,0xc0000033`) |

**access·share 에서 방향을 정하는 규칙**: **share 만 본다.** share `2` (`FILE_SHARE_WRITE`) → 들어옴 (서버가 읽고 클라이언트가 씀), share `1` (`FILE_SHARE_READ`) → 나감, share `3` → 양쪽. 그 밖의 값 (0, 4 이상) 은 `0xC000000D` (`NpCreateNewNamedPipe +0x375~+0x3b4`). **`DesiredAccess` 는 방향을 정하지 않는다.** access 는 (1) `NpCreateCcb` 에 넘기는 **처음 상태**를 정한다: `DesiredAccess & 0xf2000003 ≠ 0` (`FILE_READ_DATA`·`FILE_WRITE_DATA`·`GENERIC_*`·`MAXIMUM_ALLOWED` 비트 `[추측: 이름]`) 이면 **LISTENING(2)**, 0 이면 **DISCONNECTED(1)** (`NpCreateNewNamedPipe +0x1c5~+0x20a` — `TEST … 0xf2000003; SETNZ; ADD 1` → 세 번째 인자 → `NpCreateCcb`; 같은 식이 `NpCreateExistingNamedPipe +0x18b/+0x1a0/+0x1ab`), (2) `SeAccessCheck` 의 대상이다. 로그의 모든 서버 access (`0x40100000` 등) 는 `GENERIC_*` 비트가 있어서 **처음부터 LISTENING** 이다.

**상태 번호** (`NpCreateCcb`·`NpListen`·`NpSet*State` 가 CCB `+0x8` 바이트로 쓰는 값): `1` DISCONNECTED, `2` LISTENING, `3` CONNECTED, `4` CLOSING `[추측: 이름 — 코드가 쓰는 값과 공개 헤더 FILE_PIPE_*_STATE 가 같음]`.
**CCB·FCB 안쪽 칸 오프셋은 요청에 따라 안 정리**: 위에 나온 칸은 코드에서 읽은 것만 이름 붙였다 (`FCB +0x78` 현재 인스턴스, `+0xfc` 최대, `+0x100` 방향, `+0x104` 종류(0=메시지, 1=바이트 `[추측: 0/1 이 거꾸로가 아닌지는 NpQueryPipeLocalInfo 가 "≠1 이면 메시지 비트" 로 쓰는 것과 NpWriteDataQueue 의 0바이트 메시지 갈래로 맞춤]`), `+0x108` 원격 허용, `+0xf8` 인스턴스 수, `+0x138` 듣는 인스턴스 목록; `CCB +0x8` 상태, `+0x9`/`+0xa` 클라이언트/서버 끝의 읽기·완료 모드 바이트 (비트0 = 읽기 모드 메시지, 비트1 = 완료 모드 NOWAIT), `+0x48`/`+0xa8` 두 방향 대기열 (서버가 읽는 쪽 `+0x48`, 클라이언트가 읽는 쪽 `+0xa8`), `+0x30`/`+0x38` 클라이언트/서버 파일 객체, `+0x110` 듣기 IRP 목록).
**파일 객체에 남는 것** (이 파이프를 나중에 알아보는 표식): `FILE_OBJECT.Flags |= 0x80` (`FO_NAMED_PIPE`), `FsContext = FCB`, `FsContext2 = CCB \| 3` (서버 끝) — 클라이언트 끝은 `CCB \| 1`, 루트 핸들은 `루트노드 \| 1`. **아랫 1비트 = 쓸 수 있는 상태, 아랫 2번째 비트 = 서버** (`NpListen +0x39~+0x4f`, `NpSetConnectedPipeState`).

### 2(b). 클라이언트 쪽 열기 (`NpFsdCreate` → `NpCreateClientEnd`)

근거 파일: `ms/disassemble/npfs/NpFsdCreate.asm`, `NpCreateClientEnd.asm`. **서버가 아직 `0x110008` 을 안 불렀다면** 새 인스턴스는 이미 LISTENING 이라 붙는다 (위 2(a) 의 처음 상태).

| 요청 | 조건 | status | `Information` | 기다림 | 줄 |
|---|---|---|---|---|---|
| **없는 이름** | 이름표에 FCB 도 없고 남은 이름이 있음 | **`0xC0000034`** (→ 2) | 0 | 아니오 | `NpFsdCreate +0x26b/+0x26f → +0x379~+0x385` (`0xC0000033 + (노드==NULL)`) |
| 이름은 앞부분만 맞음 (예: `\a\b` 인데 `\a` 가 있음) | 남은 이름이 있고 노드 있음 | `0xC0000033` (→ 123) | 0 | 아니오 | 같은 줄, 노드≠NULL 일 때 |
| 이름이 `\` 로 시작하지 않거나 2바이트 이하 | `RelatedFileObject==NULL` | `0xC0000033` | 0 | | `NpFsdCreate +0x1dd/+0x1e2`, `+0x1ec/+0x1f0 → +0xa1` |
| 이름은 있는데 **인스턴스가 하나도 없음** (서버 끝 다 닫음) | FCB `+0xf8 == 0` | `0xC0000034` | 0 | 아니오 | `NpFsdCreate +0x290/+0x298 → +0x5d7` |
| **듣는 인스턴스가 없음** (모두 CONNECTED·CLOSING·DISCONNECTED) | FCB 의 듣는 인스턴스 목록 (`+0x138`) 이 비어 있음 | **`0xC00000AC`** (`PIPE_NOT_AVAILABLE` → 231 `ERROR_PIPE_BUSY`) | 0 | 아니오 (기다리려면 `WaitNamedPipeW`) | `NpCreateClientEnd +0x162~+0x170 → +0x360/+0x367` |
| **방향과 안 맞는 access** | 승인된 access 가 `FILE_READ_DATA`(1) 인데 파이프가 **들어옴**(0), 또는 `FILE_WRITE_DATA`(2) 인데 **나감**(1) | **`0xC0000022`** (→ 5) | 0 | 아니오 | `NpCreateClientEnd +0x140~+0x14d`, `+0x31f~+0x32f`(쓰기·나감), `+0x331~+0x339`(읽기·들어옴) |
| 파이프의 SD 가 access 를 안 줌 | `SeAccessCheck` 실패 | 그 status (보통 `0xC0000022`) | 0 | | `NpCreateClientEnd +0xa8~+0xc0`, `+0x138/+0x13a` |
| **붙음** | 위 검사 다 통과 | **`0`** | **`1`** (`FILE_OPENED`) | 아니오 | `NpCreateClientEnd` 끝부분, `NpFsdCreate +0x2f2/+0x2f7` |
| 〃 붙으면 일어나는 일 | 듣던 인스턴스의 상태 LISTENING → **CONNECTED**, 클라이언트 파일 객체 `FsContext2 = CCB\|1`, `FO_NAMED_PIPE` 표식, **기다리던 듣기 IRP (`0x110008`) 를 상태 `0` 으로 끝낸다** (같은 스레드의 `IofCompleteRequest` 루프) | | | | `NpSetConnectedPipeState.asm`, `NpFsdCreate +0x347~+0x366` |
| **끝 `\` 로 루트 열기** (`\Device\NamedPipe\` — `CreatePipe` ①, `WaitNamedPipeW` 의 `\DosDevices\pipe\`) | `RelatedFileObject==NULL`, 이름 `\` (길이 2) | **`0`** | **`1`** | 아니오 | `NpFsdCreate +0xf0~+0x102`, `+0x108`, `+0x10d~+0x17c` (루트 DCB 객체 `NpFC` 0x18 바이트를 만들고 `FsContext = 루트`, `FsContext2 = 객체\|1`, 장치 열림 수 +1) |
| 장치 자체 열기 (이름 없음, root 없음) | | `0` | `1` | | `NpFsdCreate +0xea → +0x40e~+0x428` |
| **서버 핸들을 root 로 한 빈 이름 열기** (`CreatePipe` ③) | `RelatedFileObject` 의 `FsContext2` 아랫비트 1 이 서 있고, 노드 종류 `0x202`(FCB), 이름 길이 0 → 그 FCB 로 **위와 같은 `NpCreateClientEnd`** | 위 "붙음"/"듣는 인스턴스 없음"/"방향" 줄과 같다 | | | `NpFsdCreate +0x9a/+0x9f`, `+0x4b4~+0x4d6`(FCB 꺼내기), `+0x513 → +0x27a`, `+0x290`(인스턴스 0 검사), `+0x2f2` |
| root 가 있는데 아랫 1비트가 없음 (끊긴 핸들) 또는 노드 종류가 그 밖 | | `0xC0000033` | 0 | | `NpFsdCreate +0x9a → +0xa1` , `+0x4b9~+0x4c3 → +0xa1` |

(`ShareAccess`·`disposition` 은 클라이언트 열기에서 **쓰이지 않는다** — `NpFsdCreate` 에서 disposition 은 `FILE_DIRECTORY_FILE` 갈래 (`NpOpenNamedPipePrefix`/`NpCreateNamedPipePrefix`) 에만 쓰이고 `NpCreateClientEnd` 로 안 넘어간다. 그래서 `CreateFile(OPEN_ALWAYS)` 로도 없는 파이프는 생기지 않고 `0xC0000034` 다.)

### 2(c). 닫기 (`NpFsdCleanup` → `NpCommonCleanup` → `NpSetClosingPipeState`, 그리고 `NpFsdClose`)

근거 파일: `ms/disassemble/npfs/NpCommonCleanup.asm`, `NpSetClosingPipeState.asm`, `NpSetDisconnectedPipeState.asm`, `NpFsdClose.asm`. CLEANUP 은 (한 파일 객체의 마지막 핸들이 닫힐 때 — 이 조건은 아래 [공백]) 온다. `NpFsdCleanup` 은 늘 **status `0`** 으로 IRP 를 끝낸다 (`NpFsdCleanup +0x22/+0x27` 뒤 `MOV [RDI+0x30],…`). `NpFsdClose` (IRP_MJ_CLOSE) 는 노드를 풀고 status `0` (`+0xc1/+0xc8`).

| 닫히는 쪽 | 그때 상대·자기 상태 | 기다리던 IRP 는 | 줄 |
|---|---|---|---|
| **CONNECTED(3) 에서 한쪽 끝이 닫힘** (`NpCommonCleanup +0x193/+0x198` 이 `NpSetClosingPipeState`) | CCB 상태 → **CLOSING(4)** (`NpSetClosingPipeState +0x6f`). 닫는 쪽 파일 객체의 `FsContext2` 아랫비트를 지우고 CCB 에서 뺌 | **닫는 쪽 대기열의 모든 항목** (그 쪽이 걸어 둔 읽기, 상대가 못 끝낸 쓰기 대기) → **`0xC000014B`** (`BROKEN`, +0x126). **상대 대기열이 "읽기 대기(ReadEntries)" 상태면 그 읽기들도 `0xC000014B`** (`+0xd6`). **상대가 이미 써 둔 자료(WriteEntries)는 남긴다** | `NpSetClosingPipeState +0x4b~+0x14e` |
| **LISTENING(2) 에서 서버 끝이 닫힘** | 상태는 그대로 | **듣기(`0x110008`) 대기 IRP 전부 `0xC000014B`** | `NpSetClosingPipeState +0x39/+0x3c → +0x1f4~+0x228` (`+0x228 MOV [RCX-0x78],0xc000014b`) |
| **CLOSING(4) 에서 나머지 한쪽도 닫힘** | 대기열 비움, 보안 정리 (`NpClearSecurity`) | 남은 대기 IRP `0xC000014B` | `+0x153~+0x1f0` (`+0x1ce` 이 `0xC000014B`, `+0x17f/+0x184` 가 `NpClearSecurity`) |
| 서버 끝 핸들이 닫힘 (FCB 쪽) | `NpUnlinkCcb`, FCB 인스턴스 수 −1, 0 이 되면 `NpUnlinkFcb` (**이름표에서 이름이 사라짐**) → 이후 클라이언트 열기는 `0xC0000034` | | `NpCommonCleanup +0x33a/+0x33f`, `+0x353/+0x358` |

**한쪽이 닫힌 뒤 상대가 하는 일의 status** (이게 3 의 "상대가 닫힘" status 와 **같은 값**이다):

| 상대의 요청 | status | DOS 오류 |
|---|---|---|
| 읽기, 남은 자료 있음 | 자료가 읽힘 (3(a)) | — |
| 읽기, 자료 없음 | `0xC000014B` | 109 `ERROR_BROKEN_PIPE` |
| 쓰기 | `0xC00000B1` (`STATUS_PIPE_CLOSING`) | 232 `ERROR_NO_DATA` |
| `0x110008` 듣기 (서버가 연결 끊김을 안 풀고 부름) | `0xC00000B1` | 232 |
| `0x110004` 끊기 | 정상 (아래) | |

**`DisconnectNamedPipe`(`0x110004`) 가 서버 끝에서 불렸을 때** (`NpCommonFileSystemControl +0x130/+0x135` → `NpSetDisconnectedPipeState`): CONNECTED·CLOSING → **DISCONNECTED(1)**; 양쪽 대기열의 대기 IRP 는 `0xC00000B0` (`PIPE_DISCONNECTED` → 233) 으로 끝나고, **클라이언트 핸들의 `FsContext2` 아랫비트를 지워서 그 핸들의 이후 읽기·쓰기·`SetInformation` 은 `0xC00000B0`**. 이미 DISCONNECTED 인데 또 끊으면 `0xC00000B0` (`NpSetDisconnectedPipeState +0xc3`). LISTENING 이면 듣기 대기 IRP 를 `0xC00000B0` 으로 끝내고 DISCONNECTED 로. 반환 status 가 그대로 IRP status 가 된다 (`NpCommonFileSystemControl +0x13f MOV EBP,EAX`).

### 3(a). READ (`NpFsdRead` → `NpReadDataQueue`)

근거 파일: `ms/disassemble/npfs/NpFsdRead.asm`, `NpReadDataQueue.asm`, `NpAddDataQueueEntry.asm`. 끝에서 `status ≠ 0x103` 이면 `IRP.Information = 읽은 바이트`, `IRP.Status = status` 로 `IofCompleteRequest` (`NpFsdRead +0x1f9~+0x214`).

| 상황 | status | `Information` | 기다림 | 줄 |
|---|---|---|---|---|
| 파일 객체가 파이프(FCB) 가 아님 (루트 등) | `0xC000000D` | 0 | 아니오 | `NpFsdRead +0x5e/+0x61 → +0x2ac` |
| `FsContext2` 아랫비트 0 (끊긴 핸들) | `0xC00000B0` | 0 | | `+0x67/+0x6b → +0x28e` |
| CCB 상태 DISCONNECTED(1) | `0xC00000B0` | 0 | | `+0xbc/+0xbf → +0x2a2` |
| CCB 상태 LISTENING(2) | `0xC00000B3` (`PIPE_LISTENING` → 536) | 0 | | `+0xc5/+0xc8 → +0x34b` |
| **방향 안 맞음** (서버가 **나감** 파이프를 읽음 / 클라이언트가 **들어옴** 파이프를 읽음) | `0xC000000D` | 0 | | `+0xdb~+0xe6 → +0x230` |
| 읽을 자료 있음 (이 끝의 대기열 상태 = "쓴 자료 있음") | `NpReadDataQueue` 결과 (아래) | 읽은 바이트 | 아니오 | `+0x10d/+0x112 → +0x244~+0x27e` |
| └ **바이트 꼴** (이 끝의 `ReadMode` 비트 = 0) | **`0`**. 대기열 항목을 이어서 `Length` 까지 채운다. **자료가 모자라도 기다리지 않고 있는 만큼만 돌려준다** | `min(Length, 있는 바이트)` | 아니오 | `NpReadDataQueue +0x6b~+0xa5`, 복사 `+0xaf` |
| └ **메시지 꼴** (`ReadMode` 비트 = 1) | 한 메시지만. 버퍼가 작으면 **`0x80000005`** (`BUFFER_OVERFLOW` → 234 `ERROR_MORE_DATA`), 나머지는 큐에 남음. 맞으면 `0` | 복사한 바이트 | 아니오 | `NpReadDataQueue +0x36e/+0x37c` (`[RSP+0xb0]==1` 이면 `0x80000005`) |
| **읽을 자료 없음, 상대가 닫힘 (CLOSING 4)** | **`0xC000014B`** (`STATUS_PIPE_BROKEN` → 109). **`STATUS_END_OF_FILE` 가 아니다** | 0 | 아니오 | `+0x118/+0x11b → +0x23a` |
| **읽을 자료 없음, NOWAIT** (이 끝 `CompletionMode` 비트 = 1) | **`0xC00000D9`** (`STATUS_PIPE_EMPTY` → 232 `ERROR_NO_DATA`) | 0 | 아니오 | `+0x121/+0x124 → +0x298` |
| **읽을 자료 없음, 블로킹** (기본) | **`0x103`** (`STATUS_PENDING`): `NpAddDataQueueEntry` 가 이 읽기 IRP 를 대기열에 걸고 (`+0x154/+0x159`, 스택 `Control \|= 1`, 취소 루틴 `NpCancelDataQueueIrp`) 돌아온다 | — | **동기 핸들**: I/O 관리자가 `NtReadFile` 안에서 `FileObject->Event` 를 기다림 (sast/124 §4). **비동기 핸들**: `STATUS_PENDING` 으로 돌아옴 → 나중에 상대가 쓰면 **그 쓰는 스레드 안에서** 이 IRP 를 채워 끝낸다 (`NpWriteDataQueue` 가 대기 읽기에 직접 복사 → 완료 목록 → `NpFsdWrite` 끝의 `IofCompleteRequest`) | `NpAddDataQueueEntry.c`, `NpFsdWrite.c` 의 완료 루프 |
| 상대가 닫혔고 자료 있음 | 위 "읽을 자료 있음" 이 먼저 (CLOSING 상태여도 자료가 있으면 읽힘) | | | `+0x10d` 가 `+0x118` 보다 앞 |

(성공·경고 status 면 `IopCompleteRequest` 가 IOSB 를 쓰고, **오류 status 이고 PENDING 없이 끝나면 IOSB 를 안 쓴다** — sast/124 표. 그래서 `0xC000014B`·`0xC00000D9` 로 끝난 `NtReadFile` 은 호출자의 IOSB 를 안 건드린다 (`Information` 을 읽지 말 것) `[정적: sast/124 와 이 문서의 status 를 이어 붙인 결론 — 동적으로 확인할 것 1번]`.)
**한 스레드에서 쓰고 읽는 첫 걸음**: 같은 프로세스에서 `WriteFile(쓰기끝)` 한 뒤 `ReadFile(읽기끝)` 은 위 "읽을 자료 있음 / 바이트 꼴" 로 바로 끝난다. **자료 없이 블로킹으로 `ReadFile` 을 부르면 그 스레드는 영원히 기다린다** (이 파이프는 `CompletionMode` 0) — 에뮬레이터가 기다림 길을 안 만들었으면 `stop` 해야 한다.

### 3(b). WRITE (`NpFsdWrite` → `NpCommonWrite` → `NpWriteDataQueue`/`NpAddDataQueueEntry`)

근거 파일: `ms/disassemble/npfs/NpCommonWrite.asm`, `NpWriteDataQueue.asm`, `NpAddDataQueueEntry.asm`. `NpFsdWrite` 는 `NpCommonWrite` 가 채운 `{status, Information}` 를 IRP 에 옮겨 끝낸다 (`status ≠ 0x103` 일 때).

| 상황 | status | `Information` | 기다림 | 줄 |
|---|---|---|---|---|
| `FsContext2` 아랫비트 0 (끊긴 핸들) | `0xC00000B0` | 0 | 아니오 | `NpCommonWrite +0x2c, +0x30 → +0x2cc` ; 잠근 뒤 `+0x96/+0x9a → +0x2c1` |
| 파이프(FCB) 가 아님 | `0xC000000D` | 0 | | `+0x3f/+0x42 → +0x247` |
| 상태 DISCONNECTED(1) | `0xC00000B0` | 0 | | `+0x2e1/+0x2e3 → +0x2c1` |
| 상태 **LISTENING(2)** (**상대가 아직 안 붙음**) | **`0xC00000B3`** (→ 536) | 0 | 아니오 | `+0x2e5/+0x2e7 → +0x34d` |
| 상태 **CLOSING(4)** (**상대가 닫힘**) | **`0xC00000B1`** (`PIPE_CLOSING` → 232) | 0 | 아니오 | `+0x2e9` |
| 방향 안 맞음 (서버가 **들어옴** 에 씀 / 클라이언트가 **나감** 에 씀) | `0xC000000D` | 0 | | 서버 끝 `+0xb5/+0xb8`, 클라이언트 끝 `+0x1c9/+0x1cd` → `+0x382` |
| **CONNECTED(3), 쿼터 안** (읽기 대기 IRP 가 있으면 그 읽기에 바로 채움, 아니면 대기열에 복사) | **`0`** | **`Length` (쓴 바이트 전부)** | 아니오 | `+0xd4/+0xda`(`status 0`, `Information=Length` 미리 채움), `+0xe8~+0x103`(쿼터 비교), `+0x138`(`NpWriteDataQueue`) → `NpAddDataQueueEntry` 가 `0` |
| CONNECTED, **쿼터 넘음, 블로킹** | **`0x103`**: 쓰기 IRP 를 대기열에 걸어 둔다. 읽는 쪽이 비워서 자리가 나면 (`NpReadDataQueue` 가 대기 쓰기 IRP 에 `Information = Length`, `Status = 0` 을 채워 완료 목록에 넣음) 끝난다 | 끝날 때 `Length` | **기다린다** (동기 핸들은 I/O 관리자가 `FileObject->Event` 대기, 비동기는 PENDING) | `+0x141 CMP EAX,0xc0000016` (아직 다 못 넣음) → `+0x239` (`NpAddDataQueueEntry`) |
| CONNECTED, 쿼터 넘음, **NOWAIT** | 안 기다림. 쓰기 끝의 `CompletionMode` NOWAIT 이면: 바이트 꼴은 기다리는 읽기에 바로 준 만큼만 쓴 것으로 하고 `0` (`Information` = 그만큼), **메시지 꼴은 `0` 바이트 `0`** | | 아니오 | `+0x2a6~+0x2bc`, `+0x2f4~+0x30a` — 자세한 조합은 NOWAIT 범위라 요약만 |
| 길이 0 쓰기 | 메시지 꼴이면 길이 0 메시지가 하나 들어감, 바이트 꼴이면 아무 일 없이 `0` | 0 | 아니오 | `NpWriteDataQueue +0x…` (`param_5==0 && 길이==0`) `[추측: 자세히는 안 읽음]` |

(쿼터: 이 끝이 쓰는 방향 대기열의 `Quota`(`+0x1c`) 에서 `QuotaUsed`(`+0x20`) 를 뺀 값. `CreatePipe` 는 `0x1000`, `CreateNamedPipe(…, 0, 0, …)` 는 0 — **0 이어도 이 코드는 따로 올려 주는 줄이 없다** (`NpCreateCcb` 가 받은 값을 그대로 `+0x64`·`+0xc4` 에 넣음). 그래서 쿼터 0 인 파이프의 쓰기는 읽기 대기가 있을 때만 바로 끝나고 아니면 위 "쿼터 넘음" 으로 간다 `[정적 읽기, 동작은 동적 확인 권함]`.)

### 3(c). 듣기 `0x110008` (`NpFsdFileSystemControl` → `NpCommonFileSystemControl` → `NpListen`)

근거 파일: `ms/disassemble/npfs/NpListen.asm`, `NpFsdFileSystemControl.asm`. `NpCommonFileSystemControl` 은 제어 코드를 비교해 `NpListen` 을 부르고 (`+0x1c1/+0x1c6`, 점프표 `+0x195~+0x1b0`), `NpFsdFileSystemControl` 은 **`status ≠ 0x103` 이면 `IRP.Status` 에 넣고 `IofCompleteRequest`** (`+0x35~+0x4c`). `Information` 은 안 쓴다 (0 그대로).

| 상태 | 조건 | status | 완료 꼴 · 기다림 | 줄 |
|---|---|---|---|---|
| (서버 끝 아님) | 파일 객체가 FCB 가 아니거나 `FsContext2` 아랫 2비트가 둘 다 안 섬 (클라이언트 핸들·루트·끊긴 핸들) | `0xC00000AF` (`ILLEGAL_FUNCTION` → 1) | 바로 끝 | `NpListen +0x33/+0x3d/+0x4f → +0x102` |
| CONNECTED(3) (**이미 붙어 있음**) | | **`0xC00000B2`** (`PIPE_CONNECTED` → 535) | 바로 끝 (오류 status 라 IOSB 안 씀 — sast/124) | `+0x90/+0x94/+0x97 → +0x99` |
| LISTENING(2) (**아직 안 붙음**) + 서버 끝이 **블로킹** (`CCB+0xa` 의 비트1=0) | | **`0x103`** | **PENDING**. 취소 루틴 `NpCancelListeningQueueIrp` 를 달고 (`+0x1b9~+0x1c0`) IRP 를 CCB 의 듣기 목록에 걸고 `Control \|= 1` (`+0x17f~+0x1b4`). 이미 취소된 IRP 면 `0xC0000120`. **동기 핸들 = I/O 관리자가 기다림 (sast/124 §4)**. 클라이언트가 붙으면 `NpSetConnectedPipeState` 가 이 IRP 를 **status `0`** 으로 끝낸다 | `+0xe8/+0xeb`, `+0xf1/+0xf5 → +0x1b9~+0x1d5` |
| LISTENING(2) + 서버 끝이 **NOWAIT** (`CCB+0xa` 비트1=1) | | **`0xC00000B3`** (`PIPE_LISTENING` → 536) | 바로 끝 | `+0xf1/+0xf5 → +0xfb` |
| DISCONNECTED(1) (끊긴 뒤 다시 듣기) | `NpCancelWaiter` 로 이 이름을 기다리던 `WaitNamedPipe` 들을 깨움 (status 0) 한 뒤, 블로킹이면 **상태를 LISTENING 으로 바꾸고 듣기 목록에 넣고 `0x103`** (PENDING), NOWAIT 이면 상태를 LISTENING 으로 바꾸고 `0xC00000B3` | | 같음 | `+0xe6 → +0x109~+0x126`, `+0x130 → +0x217/+0x228`, `+0x13a~+0x1b4` |
| **붙었다 닫음** (CLOSING 4: 상대가 이미 닫음) | | **`0xC00000B1`** (→ 232 `ERROR_NO_DATA`) | 바로 끝 | `+0x1df~+0x1e9` |
| 그 밖의 상태값 | 버그체크 `0x25` | | | `+0x203` |

**kernelbase 가 돌려주는 것**: 1(c) 표. `0` → `TRUE`, 나머지는 `FALSE` + 위 DOS 오류.
**완료 꼴 요약**: `PENDING` 으로 걸린 듣기 IRP 는 (1) 클라이언트 열기가 오면 `NpSetConnectedPipeState` 가 `Status=0` 으로 완료 목록에 옮겨 `NpFsdCreate` 의 루프 (`+0x33a~+0x366`) 가 끝내고, (2) 서버 끝이 닫히면 `0xC000014B`, (3) `DisconnectNamedPipe` 면 `0xC00000B0`, (4) 취소되면 `0xC0000120` (→ 995) 이다.

### 3(d). `GetFileType` 이 묻는 장치 종류, `NtQuery/SetInformationFile` 칸 모양

**장치 종류**: `ms/disassemble/kernelbase/GetFileType.asm` `+0x5a~+0x72` 가 `NtQueryVolumeInformationFile(h, &iosb, &buf[8], 8, class=4 FileFsDeviceInformation)` 를 부르고, 성공하면 `[RSP+0x50]` 의 `DeviceType` 으로: `0x50` (콘솔) → `2` (`FILE_TYPE_CHAR`, `+0x82~+0x8c`), 그 밖에는 `DeviceType−2 ≤ 0x29` 인 점프표 (`+0xb8~+0x105`) 로 — 표를 원본 바이트로 읽으면 (RVA `0xc63c4` 바이트표 15번째 = 2 → 대상 `0xc63a6` = `MOV EAX,3; …RET`) **`DeviceType 0x11` → `3` (`FILE_TYPE_PIPE`)**, 디스크류 (7·8…) → 1, 콘솔/직렬 등 → 2, 그 밖 (네트워크 0x12 등) → 0 + `SetLastError(0)` (`+0xc0~+0xce`).
**누가 답하나**: 파이프 핸들이면 **ntoskrnl 이 직접 답한다 — IRP 를 npfs 로 안 보낸다.** `NtQueryVolumeInformationFile +0x1fe CMP R15D,4; JZ +0x3b6` (클래스 4) → `+0x3d6~+0x3e2` 에서 파일 객체의 장치 객체 `DeviceType == 0x14` (네트워크 파일 시스템) 가 아니면 `+0x3e8~+0x44d` 에서 **`장치객체+0x48 (DeviceType)` 와 `+0x34 (Characteristics)` 를 8바이트 출력에 쓰고 IOSB = `{0, 8}`** (`+0x41f/+0x426` 가 두 값, `+0x442/+0x445` 가 IOSB, 길이 8 미만이면 `0xC0000004` `+0x50b`). 그 장치 객체는 `NpCreateDevice` 가 `IoCreateDevice(…, 장치종류 0x11, 특성 0x20000, …)` 로 만든 것 (`+0x4d R9D=0x11`, `+0x58 [RSP+0x20]=0x20000`). → **`DeviceType = 0x11` (`FILE_DEVICE_NAMED_PIPE`), `Characteristics = 0x20000 (+ IoCreateDevice 가 더 얹는 비트는 [공백])`, IOSB = {STATUS_SUCCESS, Information 8}.** (npfs 자신의 `NpCommonQueryVolumeInformation +0xda~+0xf7` 도 클래스 4 에 `{0x11, 0}` 을 쓰지만 이 길로는 안 온다. 클래스 5 (`FileFsAttributeInformation`) 는 npfs 가 답해서 `FileSystemAttributes=2`, 이름 길이 8 `"NPFS"`; 클래스 1·3·7 도 npfs 가 채우고, 그 밖은 `0xC00000BB`.)
C 런타임이 표준 핸들마다 부르는 이 호출은 에뮬레이터가 **장치 종류 `0x11`** 만 주면 `FILE_TYPE_PIPE` 가 된다 (특성 값은 `GetFileType` 이 안 읽는다).

**`NtQueryInformationFile` (`NpCommonQueryInformation`)**: 클래스 `4` (Basic, 0x28 바이트를 0 으로 채움), `5` (Standard — `NpQueryStandardInfo`), `6` (Internal), `7` (Ea), `9` (Name), `0xe` (Position), `0x12` (All), **`0x17` (`FilePipeInformation`)**, **`0x18` (`FilePipeLocalInformation`)**; 그 밖은 `0xC000000D`. 파이프(`0x202`) 나 루트 (`0x206`) 가 아니면 `0xC000000D`, `FsContext2` 아랫비트 0 이면 `0xC00000B0`. **PDB 에 `FILE_PIPE_*` 타입 없음 (ntoskrnl·npfs·kernelbase 다)** → 아래는 코드가 쓰는 칸만:

| 클래스 | 크기 | 칸 (코드에서 읽음) |
|---|---|---|
| `0x17` `FilePipeInformation` | 8 | `+0 ReadMode` (이 끝 읽기모드 비트), `+4 CompletionMode` (이 끝 완료모드 비트) — `NpQueryPipeInfo` |
| `0x18` `FilePipeLocalInformation` | 0x28 | `+0 NamedPipeType` (비트0 = 메시지, 비트1 = 원격 거부), `+4 NamedPipeConfiguration` (0 들어옴·1 나감·2 양쪽), `+8 MaximumInstances`, `+0xc CurrentInstances`, `+0x10 InboundQuota`, `+0x14 ReadDataAvailable`, `+0x18 OutboundQuota`, `+0x1c WriteQuotaAvailable`, `+0x20 NamedPipeState` (1~4), `+0x24 NamedPipeEnd` (0 클라이언트 / 1 서버) — `NpQueryPipeLocalInfo` |
| `0x19` `FilePipeRemoteInformation` | 0x10 | npfs 의 `NpCommonQueryInformation`·`NpCommonSetInformation` 이 **안 받는다 (`0xC000000D`)**. kernelbase 가 `SetNamedPipeHandleState(…, maxcnt, timeout)` 에서 쓰는 클래스 (1(d) 표) 인데, 같은 컴퓨터 파이프에서는 실패한다 (문서의 "같은 머신이면 NULL" 과 맞음) |

**`NtSetInformationFile` (`NpCommonSetInformation`)**: 클래스 `0x17` 만 일을 한다 (`NpSetPipeInfo`): 값은 `{ReadMode, CompletionMode}` 둘 다 0 또는 1 (아니면 `0xC000000D`), **바이트 꼴 파이프에 `ReadMode=1` → `0xC000000D`**, 이 끝에 걸려 있는 대기 IRP 가 있는데 `CompletionMode` 를 NOWAIT 으로 바꾸면 `0xC00000AE` (`PIPE_BUSY`), 끊긴 핸들은 `0xC00000B0`. 클래스 `4` (Basic) 는 아무 일 없이 `0`. 그 밖은 `0xC000000D`. (`NpSetPipeInfo.asm`, `NpCommonSetInformation.asm`)

## 지금 우리 (PE_emulator) 맞나·틀리나

쓰는 곳 (`ext/syscall/syscall_layer_cpp/…`) 하나씩. 원칙 "호스트에 진짜 파이프를 안 만든다" 는 이 결과와 충돌하지 않는다 — npfs 규칙은 전부 프로세스 안 상태기계 + 두 방향 바이트 대기열이다.

| 곳 | 맞다 / 틀리다 · 한 줄 |
|---|---|
| `src/file/nt_legacy_file_frontends.cpp:48` `nt_create_named_pipe_file` | **인자 읽기는 맞다**: 시간 포인터 `a[13]` 의 4바이트 정렬 (`&3` → misalignment) 과 읽기 실패 (AV) 는 `NtCreateNamedPipeFile +0x68~+0x92` 와 같고 (단 **유저 모드일 때만** — `+0x5c/+0x62`), 출력 이름표 (`a[4]` share … `a[12]` outbound) 의 순서는 `+0x11f~+0x14c` 와 같다. **지금 `stop` 하는 것은 틀린 대신 "아직 안 함"**: 만들려면 위 2(a) 표가 규칙 전부다 (ntoskrnl 은 값 검사를 안 하고 npfs 가 한다 — `TimeoutSpecified`·최대 수·시간 부호·share 값·바이트꼴+메시지읽기 검사를 여기서 해야 함). 성공 = `status 0`, **IOSB.Information = 2 (새 이름·이름 없음) 또는 1 (이미 있는 이름)**, 핸들 = 새 "서버 끝" 객체. |
| `src/file/nt_create_file.cpp:110` (`object_name_empty_unverified`) | **`CreatePipe` ② ③ 는 여기서 멈춘다 (틀림).** 이 두 호출은 `root_directory ≠ 0` + **빈 이름** (Length 0, Buffer NULL — `CreatePipe +0x6e/+0x72`) 이 정상이다 (`NpFsdCreateNamedPipe +0x318~+0x329`, `NpFsdCreate +0x4b4~+0x4d6`). 이 줄 앞에서 **root 가 있으면 빈 이름을 통과**시키고 root 가 "루트 핸들" 인지 "파이프 끝" 인지 보는 길로 가야 한다. 이름 `UNICODE_STRING` 의 `MaximumLength` 는 ① 에서 0 이다 (Length 만 세움 — `CreatePipe +0x1d4`) — 에뮬레이터가 `MaximumLength ≥ Length` 를 검사하면 틀린다 (지금은 Length 만 읽어서 괜찮음). |
| `src/file/nt_create_file.cpp:189~191` (`path_prefix_unsupported`) / `nt_open_file.cpp` | **이름 형태 셋을 받아야 한다**: ① `\??\pipe\<이름>` (`CreateNamedPipe`·`CreateFile`), ② `\Device\NamedPipe\` (`CreatePipe` ①, **Attributes 0 = 대소문자 구분**, 정확히 `NamedPipe`), ③ `\DosDevices\pipe\` (`WaitNamedPipeW`, 이 이름은 `\DosDevices` → `\??` 링크가 있어야 하는데 `namespace.cpp` 에 **없다** — `grep DosDevices` 0줄). OA 속성 검사 `oa_flags & ~0x42` 는 `CreatePipe` 의 `0x40`·`0x42`·`0` 에 맞다. `security_descriptor` 가 있으면 `stop` (110줄 위) 하는 것은 **`CreateNamedPipeW` 가 늘 기본 DACL 서술자를 달아 보내므로** 이 길에서는 걸림돌이다 — SD 는 받아서 무시 (또는 저장) 해야 한다. `NtOpenFile` 의 `FILE_OPEN` 고정 인자 변환 (`nt_open_file.cpp`) 은 맞다. |
| `src/namespace.cpp:93` 곁 | `\Device\NamedPipe` 장치는 이미 있다 (맞음). **`\GLOBAL??\PIPE → \Device\NamedPipe` 와 `\??\PIPE` 링크가 없다** ([수집] reg-users `DOS Devices` 의 `PIPE` 근거는 요청서에 이미 있음). `\DosDevices → \??` 도 필요. |
| `src/file/nt_file_misc.cpp:104` → `nt_device_io_control_file.cpp` (`0x110008`) | `NtFsControlFile` 이 `NtDeviceIoControlFile` 과 "같은 요청 검사" 라는 것은 맞다 (sast/124: 마지막 인자만 0/1 이고 `MajorFunction` 이 0xd/0xe). 파이프 제어 코드는 `FILE_DEVICE_NAMED_PIPE`(0x11) 라서 코드 올림 `0x110008` = `(0x11<<16)\|(2<<2)`. 응답은 1(c)·3(c) 표: 입출력 버퍼는 NULL·0, status 는 상태별 표, `0x103` 이면 동기 핸들은 기다림 길 (nt 엔진), 오류 status 면 **IOSB 안 씀**. |
| `src/file/nt_read_file.cpp` (파일 읽기 6번 "끝이면 `STATUS_END_OF_FILE`") | **파이프에는 틀린다**: 쓰는 끝이 닫힌 뒤 자료 없음 = **`0xC000014B`** (EOF 아님), NOWAIT 빈 읽기 = `0xC00000D9`, `ByteOffset` 을 안 쓴다 (`FO_NAMED_PIPE` 표식이라 오프셋 필수 검사 면제 여부는 `NtReadFile` 쪽 확인이 남음 `[공백]`). 읽기 핸들의 access `GENERIC_READ` → `kReadData` 는 통과한다. |
| `src/file/nt_write_file.cpp` | 파이프에는: 상대가 안 붙음 `0xC00000B3`, 상대 닫힘 `0xC00000B1`, 쿼터 안이면 `0` + `Information = Length`, 쿼터 넘으면 블로킹은 `0x103` 으로 기다림 (3(b) 표). |
| `include/syscall_layer/files.hpp` (파일 객체 종류) | **새 종류가 둘 필요**: "파이프 끝" (서버/클라이언트, 상태 1~4, 읽기·완료 모드, 두 방향 대기열 + 쿼터, 짝 끝, 이름·최대 수·방향·종류를 든 FCB 격 객체, 듣는 인스턴스 목록) 과 "파이프 루트 (`\Device\NamedPipe\` 열기)". `NamedPipe` 열기에서 `FILE_OBJECT.Flags` 에 `0x80` (`FO_NAMED_PIPE`), `FsContext2` 아랫 2비트 (쓸 수 있음·서버) 도 모양을 흉내낼지는 선택 (겉 동작엔 필요 없음). 안쪽 CCB·FCB 칸 오프셋은 필요 없다. |

## 동적으로 확인할 것

(전부 선택 — 정적으로 이 문서의 답은 닫혔다. 아래는 "정적으로 못 정한 것" 과 "한 번 보면 마음이 놓이는 것".)

1. **요청 원문의 다섯 status** — 같은 프로세스에서 `CreatePipe` → `WriteFile`(n바이트) → `ReadFile` → 쓰는 끝 `CloseHandle` → `ReadFile`. 정적 예측: ① `CreatePipe` TRUE ② 쓰기 status `0`, Information = n ③ 읽기 `0`, Information = n ④ (`CLEANUP` 0) ⑤ 두 번째 읽기 **`0xC000014B`** (→ 109), **그때 IOSB 는 안 써진다** (오류 + PENDING 없음, sast/124). 멈춤점:
   - `KERNELBASE!CreatePipe+0x106` (② `NtCreateNamedPipeFile` 직후, `rax` = status), `+0x15d` (③ 직후), `+0x210` (① 직후 — 처음 한 번만),
   - `KERNELBASE!WriteFile+0x8d`/`+0x11a`, `KERNELBASE!ReadFile+0x8d`/`+0x12a` (`NtWriteFile`/`NtReadFile` 의 ret 자리 — `_calls.tsv`),
   - `npfs!NpFsdWrite`, `npfs!NpFsdRead` (입구; `r8`/IRP 의 `+0x30` 이 status), `npfs!NpFsdCleanup` (쓰는 끝 `CloseHandle` 때 오는지 — 아래 3),
   - 커널 멈춤점 (`npfs!…`, `nt!IopCloseFile`) 은 모든 프로세스에 걸리니 test.exe 일 때만 멈추게 조건을 붙일 것 (npfs 에는 시스템 전체의 파이프가 지나간다).
2. **`FileFsDeviceInformation` 의 `Characteristics`** — `NtQueryVolumeInformationFile +0x426` 의 값 (`[R12+4]`) 이 `0x20000` 인지, `IoCreateDevice` 가 더 얹는 비트가 있는지. `!devobj \Device\NamedPipe` 또는 `dt nt!_DEVICE_OBJECT <주소> Characteristics DeviceType`.
3. **`IRP_MJ_CLEANUP` 이 오는 때 (`IopCloseFile` `+0x0 CMP R8,1`, `+0x67 CMP R9,1`)** — `CLEANUP` 을 만드는 줄은 `IopCloseFile +0x10a` (`MOV byte [RCX-0x48],0x12`) 이고 `R9 == 1` 일 때 들어가는데, **`R9` 가 "이 프로세스의 이 객체 핸들 수" 인지 "시스템 전체 핸들 수" 인지는 정적으로 못 정했다.** 자식 프로세스가 쓰는 끝을 물려받은 상태에서 부모가 `CloseHandle` 했을 때 `npfs!NpFsdCleanup` 이 **오는가** (= 자식이 살아 있는 동안 부모가 읽으면 BROKEN 대신 PENDING 인가) 가 갈림. 요청서의 "자식 표준 출력 길" 에 바로 걸림. 멈춤점 `nt!IopCloseFile` (`r8`·`r9`), `npfs!NpFsdCleanup`.
4. **`WaitNamedPipeW` 의 기다림·타임아웃 status** — `npfs!NpWaitForNamedPipe` 가 듣는 인스턴스가 있으면 status 0 으로 바로 끝나고 (`NpWaitForNamedPipe.c` 에서 읽음), 없으면 `NpAddWaiter` 로 기다리는 것까지는 읽었지만 **타임아웃 때 status (`STATUS_IO_TIMEOUT`?) 와 `ERROR_SEM_TIMEOUT` 매핑**은 `NpTimerDispatch`/`NpAddWaiter` 를 안 봤다 (아래 "다음 요청 후보").
5. **`\DosDevices\pipe\` 가 진짜 풀리는지** — `WaitNamedPipeW` 의 `NtOpenFile` (`KERNELBASE!WaitNamedPipeW+0x189`) 의 `r8` (OA) → `ObjectName` 을 `!obja` 로. 정적으로는 문자열을 `.rdata` 에서 직접 읽어 확인함 (RVA 0x2a0ab8 = `\DosDevices\pipe\`).
6. **`FO_NAMED_PIPE`(0x80) 이 `NtReadFile`/`NtWriteFile` 에서 하는 일** — 오프셋 없는 읽기·쓰기가 허용되는 이유 (`ByteOffset` 필수 검사 면제). 비동기 핸들로 `ByteOffset` 없이 `ReadFile` 하는 경우.
7. **한 동기 핸들에서 한 스레드가 블로킹 읽기 중일 때 다른 스레드의 쓰기** — 동기 핸들은 파일 객체 락으로 직렬화되는 것으로 알려져 있다 (`IopWaitAndAcquireFileObjectLock` 은 `NtQueryVolumeInformationFile +0x556` 에서 봄, `NtReadFile`/`NtFsControlFile` 쪽 줄은 이번에 안 봄 `[공백]`). 요청서의 이름 있는 파이프 샘플 (한 스레드 `CreateNamedPipe` + 다른 스레드 `CreateFile`) 은 서로 다른 파일 객체라 영향 없음.

## 다음 요청 후보

1. `NpAddWaiter`·`NpCancelWaiter`·`NpTimerDispatch`·`NpCancelWaitQueueIrp` — `WaitNamedPipeW` 의 기다림·깨움·타임아웃 status (위 4).
2. ntoskrnl `NtReadFile`/`NtWriteFile` 의 `FO_NAMED_PIPE`·`FO_SYNCHRONOUS_IO` 갈래와 `IopCloseFile` 의 `R8`/`R9` 뜻 (위 3·6·7) — "자식이 핸들을 물려받은 파이프의 CLEANUP 시점".
3. `NpPeek` (FSCTL `0x11400c`) 출력 칸·상태별 status, `NpTransceive` (`TransactNamedPipe`, 이번엔 범위 밖), `NpFsdFlushBuffers`.
4. `NpFindPrefix`·`NpTranslateAlias` (`LOCAL\` 별칭 — AppContainer·사일로에서 이름이 바뀜), `NpReadParameters`·`NpReadAlias` (레지스트리 별칭 표 기본값 — 이름 없는 파이프엔 영향 없음).
5. 메시지 꼴 파이프 (`PIPE_TYPE_MESSAGE`) 의 `NpReadDataQueue`·`NpWriteDataQueue` 정밀 읽기 (이번엔 바이트 꼴 중심), `NOWAIT` 쓰기 조합.
6. kernelbase `GetNamedPipeInfo`·`GetNamedPipeHandleStateW` 의 `NpGetUserName` (`[0x1803c61d8]` 간접 호출, 사용자 이름 얻기) — 이름 있는 파이프의 클라이언트 가장 쪽 (범위 밖이라 이름만).

## 스크립트 문제

없음. 다만 읽을 때 조심한 것 (스크립트 버그 아님):
- 이번 npfs `.c` 는 상수가 `-0x3fffff50` 처럼 음수 십진수로 나오고 (`0xC00000B0`), `NpCommonFileSystemControl.c` 는 반환값이 `unaff_RBP`/`extraout_EAX` 로 깨져 보인다 (Ghidra 의 레지스터 추적 실패). **그래서 status 값과 흐름은 `.asm` 로 다시 셌다** (본문 줄은 전부 `.asm` 기준).
- `kernelbase!CreatePipe.asm` 의 전역 주석 `InteractiveUserNameSpaceSeparation+0x8` 은 "가장 가까운 앞 PDB 이름" 이다 (스킬 문서의 "전역 주석 주의" 와 같은 경우) — `refs` 로 이 칸이 `CreatePipe` 만 만지는 별개 변수임을 확인했다.
- 임시 도구: `RtlNtStatusToDosErrorNoTeb` 의 이분 탐색표 (`RtlpRunTable` RVA `0x195b50` 8바이트×0x158, `RtlpStatusTable` RVA `0x193a10`) 와 `GetFileType` 의 점프표, 몇 개 `.rdata` 문자열은 작은 파이썬 (`/tmp/claude-1000/…/scratchpad/rva.py` 와 한 줄 짜리) 로 바이너리에서 직접 읽었다 (커밋 대상 아님). DOS 오류값 (231·232·233·535·536·109·123·995 등) 은 이 표에서 나온 값이다.

## 판정

**분석성공.** 요청의 ★ 전부 (1(a)(b)(c), 2(a)(b)(c), 3(a)(b)(c)) 와 1(d)·3(d) 를 `.asm` 줄과 함께 닫았다. 정적으로 못 닫은 것은 `IopCloseFile` 이 CLEANUP 을 보내는 핸들 수 조건, `WaitNamedPipeW` 의 타임아웃 status, 장치 `Characteristics` 최종값 셋이고 "동적으로 확인할 것" 3·4·2 에 멈춤점과 함께 적었다. 새 모듈 npfs 의 PDB GUID `C433ACB0BA96C41BACA83308CFF250A61`. 오래 도는 작업은 없다.
