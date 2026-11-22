# 125 file-completion — 정적 분석 결과

- 요청서: `_workspace/sast/125_file-completion.md` (Q220, PE_emulator 요청 "소켓을 완료 포트에 묶기 — `NtSetInformationFile` 종류 `0x1e`·`0x29`")
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb `ntkrnlmp.pdb` guid+age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 이미 분석된 프로젝트라 analyze 는 안 했다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음). 같은 시간에 도는 sast/126 (다른 모듈) 과 프로젝트가 달라 부딪히지 않았다. export 는 세 번 (각 6초 안팎), 서버 사용 가능 메모리는 계속 31 GB 였다.
- 뽑은 함수:
  - `^NtSetInformationFile$` depth 1 → 47개. `ms/disassemble/ntoskrnl/NtSetInformationFile.asm` (1017줄), `ms/decomp/ntoskrnl/NtSetInformationFile.c`. 로그 `ms/ghidra/ntoskrnl.export.sast125a.stdout`.
  - **요청서 범위를 조금 넓힘 (depth 0, 작은 것만)**: `^(IopDeleteFile|ObpReferenceObjectByHandleWithTag)$` (sast125b), `^IopAllocateIrpPrivate$` (sast125c). 이유: "포트 참조를 잡나 → 누가 놓나", "포트 핸들 풀기가 실패하면 status 가 뭔가", "이 IRP 의 `ApcContext` 칸이 정말 0 인가" 가 이 셋을 안 보면 추측이 됐다.
  - 읽기만 한 것 (요청서 대로): `IopCompleteRequest.asm`, `IopXxxControlFile.asm`. `IopSynchronousServiceTail` 은 sast/124 가 읽은 줄을 가리킨다.
- 버전 확인: `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 둘 다 `_calls.tsv` 와 일치.
- 표 3개를 PE 파일에서 바이트로 직접 읽음 (`PAGE` 구역): `IopSetOperationLength` RVA `0xb4af00` (byte ×0x54), `IopQuerySetAlignmentRequirement` `0xb4ae40` (byte ×0x54), `IopSetOperationAccess` `0xb4bf40` (dword ×0x54). **PE_emulator `nt_set_information_file.cpp` 의 표 3개와 84칸 전부 같다** (비교 스크립트 결과 `len/align/access equal True`). 두 군데서 따로 읽은 값이 맞으니 표 값은 믿어도 된다.
- 표기: `함수 +0x…` 는 전부 `ms/disassemble/ntoskrnl/<함수>.asm` 의 줄. call 은 call 자리(ret 자리 `_calls.tsv`) 를 적었고, 점프 목적지는 "목적지 절대 주소 − 함수 시작" 으로 셌다. 이름 `[추측]` 표시는 PDB 에 비트 이름이 없어 공개 헤더 값으로 맞춘 것.

## 먼저 읽는 한 줄 답

- **두 종류 다 afd 로 안 내려간다.** `NtSetInformationFile` 이 IRP 를 만들긴 하지만 드라이버를 안 부르고 (`IopCallDriverReference` 줄을 건너뜀) 같은 스레드에서 `IopCompleteRequest` 로 스스로 끝낸다 `[정적]`.
- **`0x1e` (포트 묶기)**: 입력 16바이트 (`+0x0` 포트 핸들, `+0x8` 키). 이미 묶였거나 **동기 핸들이면 둘 다 `0xC000000D`**. 포트 핸들은 접근 `2` (`IO_COMPLETION_MODIFY_STATE`) 로 풀고, 성공하면 포트 참조를 **들고 있는다** (파일 객체가 지워질 때 놓음). 파일 객체 `+0xb0` (`CompletionContext`) 에 0x18 바이트 덩어리 `{Port +0, Key +8, UsageCount +0x10}` 를 달고 `Flags &= ~0x400`. 성공이면 IOSB 에 `{Status 0, Information 0}`.
- **`0x29` (알림 방식)**: 입력 4바이트. 비트 `0x1/0x2/0x4` 만 보고 각각 파일 객체 `Flags` (`+0x50`) 의 `0x2000000 / 0x4000000 / 0x8000000` 을 **켠다**. **끄는 줄은 없다.** 다른 비트는 검사 없이 무시 (status 0). 동기 핸들만 `0xC000000D`. 포트에 묶이기 전에 불러도 된다 (이 갈래가 `+0xb0` 을 안 읽음).
- **IOSB 는 오류일 땐 안 쓴다.** 두 종류의 오류 (`0xC000000D` 등) 는 IRP 에만 적히고, `IopCompleteRequest` 가 "오류 + PENDING 아님" 이라 IOSB 를 안 쓴다 (sast/124 와 같은 길). 성공일 때만 `{0, 0}` 을 쓴다.
- **`ConnectEx` 를 바로 오류로 돌려줄 때 패킷을 안 보내는 게 맞나 → 예** (afd 가 PENDING 표시 없이 오류 status 로 끝낼 때). 근거 `IopCompleteRequest +0xb3~+0xbf → +0x426/+0x42a → +0x445`. 단 **afd 가 먼저 PENDING 표시를 하고 나중에 오류로 끝내면 패킷이 간다** — 어느 쪽인지는 정적으로 못 정한다 `[공백]` (sast/121 동적 5번 (a)).
- **지금 우리**: `0x1e`·`0x29` 가 없다 → **틀림** (`nt_set_information_file.cpp:145`). 앞쪽 검사 순서·표는 맞다. 또 클래스 16 오류 때 IOSB 를 쓰는 줄 (`:173`) 은 이 asm 으로는 **틀려 보인다** (아래 "범위 밖에서 눈에 띈 것").

## 공통 앞부분 — `0x1e`·`0x29` 가 같이 지나는 검사 (`NtSetInformationFile.asm`)

| 차례 | 조건 | status | 쓰는 칸 | 줄 (+오프셋) |
|---|---|---|---|---|
| 1 | 호출자 모드 (`Thread+0x232`) 가 **0 (커널)** 이면 2~5 를 통째로 건너뜀. 길이·정렬 검사 없음 (그래서 커널 호출자는 16B 보다 짧게 줘도 안 막힘) | — | — | `+0x69` (읽기), `+0x76/+0x79 JZ → +0x4ce` |
| 2 | 사용자 모드: 종류 번호 ≥ `0x54`, 또는 길이표 값이 0 | `0xC0000003` | — | `+0x87/+0x8a`, `+0xa0/+0xa2 → +0xa2f` (`+0xa2f MOV EAX,0xc0000003`). 표 값: `0x1e`=16, `0x29`=4 → 통과 |
| 3 | 사용자 모드: `Length` < 길이표 값 (`0x1e`: 16, `0x29`: 4) | `0xC0000004` | — | `+0xa8/+0xab → +0x96b` (`+0x96b MOV EAX,0xc0000004`) |
| 4 | IOSB 포인터 쓰기 시험: 첫 4바이트를 읽고 그대로 되씀 (포인터가 `0x7fffffff0000` 이상이면 그 주소를 건드려 예외) | 예외 → 아래 5 의 착지점 | — | `+0xb1~+0xc7` |
| 5 | 입력 버퍼 정렬 (둘 다 표 값 4): 어긋나면 `ExRaiseDatatypeMisalignment`; 버퍼 끝이 `0x7fffffff0000` 을 넘으면 읽기 시험이 예외. (WOW64 는 마스크 3, `+0xd9~+0x11e`) | 예외 (`STATUS_DATATYPE_MISALIGNMENT`=`0x80000002` 등) 가 상태 코드로 돌아옴 `[추측: 착지점이 __except 블록]` | — | 정렬 `+0x125~+0x134 → +0x150`, 끝 검사 `+0x136~+0x146`. 착지점은 읽지 않은 틈 `+0x157` (원본 바이트 `e9 6b 02 00 00` = `jmp +0x3c7`, 곧 함수 끝 `RET`. EAX 를 안 건드림) |
| 6 | 파일 핸들에 요구하는 접근 권한 (표) | — | — | `+0x15c MOV ESI,[…+0xb4bf40]`. 두 종류 다 **0** (파일 핸들이기만 하면 됨) |
| 7 | 파일 핸들 풀기 `ObReferenceObjectByHandle(h, 0, IoFileObjectType, 호출자 모드)` | 실패하면 **그 status 그대로** 돌려줌 (`0xC0000008` 잘못된 핸들 · `0xC0000024` 종류 다름 등) | — | 호출 `+0x19a`, 실패 반환 `+0x1ae/+0x1bb/+0x1be → +0x3c4`. status 상수는 `ObpReferenceObjectByHandleWithTag.asm` 에 `0xC0000024`(`+0x1ee`,`+0x43f`)·`0xC0000008`(`+0x575` 등)·`0xC0000022`(`+0x693`,`+0x701`) 로 있음. 어느 조건에서 어느 값인지는 안 따라감 |
| 8 | AppContainer 프로세스이고 파일 객체 확장 (`FileObject+0xd0`) 의 첫 dword 에 비트 `4` | `0xC0000910` (파일 참조는 놓고 돌려줌) | — | `+0x3db~+0x417`, `+0x405 PsIsProcessAppContainer`, `+0xfd2 MOV R13D,0xc0000910`. (sast/124 의 같은 칸 4번과 같은 줄) |
| 9 | 동기 핸들 (`Flags&2`): 파일 객체 락을 잡음 (`+0x271 KeAbPreAcquire`, `+0x280 XCHG [FO+0x74]`, 막혀 있으면 `+0xb4c IopWaitAndAcquireFileObjectLock`). 비동기 핸들: 스택에 임시 이벤트·임시 IOSB | — | 동기 핸들은 IRP 의 `UserIosb` 를 처음부터 사용자 IOSB 로 (`+0x975 OR [IRP+0x47],2`, `+0x981`) | `+0x23d/+0x241`, `+0x5ff~+0x62d` (비동기 쪽 임시 칸), `+0x6ba MOV [IRP+0x10],4` |
| 10 | `IopResetEvent` → `IopAllocateIrpExReturn` → 입력을 풀 (태그 `IoSB`) 에 **복사** → `IRP.Flags \|= 0x830` → 스레드 IRP 목록에 걸기 | IRP 못 만들면 `0xC000009A` | IRP `+0x18` = 복사본, 스택 칸 `MajorFunction`=6 (`+0x6e1`), 길이 `+0x79d`, 종류 번호 `+0x7a1` | `+0x635`, `+0x657` (실패 `+0xbc5`), `+0x700~+0x72a`, `+0x78f`, `+0x7b8 IopQueueThreadIrp`. IRP 는 `IopAllocateIrpPrivate +0x192` 의 `memset` 으로 0 부터 시작 → `Irp+0x41` (PendingReturned)=0, `Irp+0x60` (ApcContext)=0 |
| 11 | 종류별 갈래 (아래 표) | | | `+0x7d5~+0x845` 비교 사슬 |

종류 번호로 점프표를 쓰지 않고 **비교 사슬**이다. 표는 위 세 개 (길이·정렬·접근) 뿐이다.

## 종류 `0x1e` (`FileCompletionInformation`) 검사 표

갈래 시작 `+0x815 CMP EDI,0x1e / +0x818 JZ → +0xa4a`. `RDI = IRP+0x18` = 복사해 둔 입력 (`+0x0` 포트 핸들, `+0x8` 키), `RBX = 파일 객체`.

| 차례 | 조건 | status | 쓰는 칸 | 줄 (+오프셋) |
|---|---|---|---|---|
| (공통 1~10) | 위 표 | | | |
| 1 | `FileObject+0xb0` (`CompletionContext`) ≠ 0 — **이미 묶임** | `0xC000000D` | IRP `+0x30`=status, `+0x38`=0 | `+0xa53/+0xa5b → +0xbe8 → +0xb0a/+0xb0e` |
| 2 | `FileObject.Flags & 0x2` (`FO_SYNCHRONOUS_IO`) — **동기 핸들** (락은 이미 잡은 뒤, 끝나고 놓음 `+0x909`) | `0xC000000D` | 같음 | `+0xa61/+0xa66 → +0xbe8` |
| 3 | 포트 핸들 풀기 `ObReferenceObjectByHandle(*입력, 접근 **2**, IoCompletionObjectType, 호출자 모드)` | 실패하면 **그 status 그대로** (잘못된 핸들 `0xC0000008`, 종류 다름 `0xC0000024`, 권한 부족 `0xC0000022` — 위 공통 7 과 같은 함수) `[정적: 상수 있음 / 조건별 매핑은 안 따라감]` | 성공하면 포트 객체 포인터를 스택 `[RSP+0x60]` 에 받음 (**참조 +1**) | `+0xa6c~+0xa95`, 실패 `+0xa9d/+0xa9f → +0xb0a` |
| 4 | 0x18 바이트 풀 할당 (비페이지, 태그 `IoCc`, 0 으로 채워짐 `[추측: ExAllocatePool2 플래그 0x40 에 UNINITIALIZED 없음]`) | 실패면 포트 참조 되돌림 (`+0xd8c`) 후 `0xC000009A`, IRP `+0x38`=0 | — | `+0xaa1~+0xaaf`, `+0xaba JZ → +0xd82~+0xd9f` |
| 5 | 스핀락 `FileObject+0xb8` (`IrpListLock`) 잡고 `+0xb0` 이 아직 0 인지 다시 봄 (다른 스레드와 경주 대비) | 아니면 락 놓고 풀 해제·포트 참조 되돌림 후 **`0xC0000048`** (`STATUS_PORT_ALREADY_SET` `[추측: 이름]`). 순차 호출에선 1 번에서 이미 걸러져 안 나옴 | — | `+0xac7`, `+0xad3/+0xadb → +0x1020~+0x1041` |
| 6 | **성공**: `Flags &= ~0x400` · `ctx.Port` = 포트 객체 · `ctx.Key` = 입력 `+0x8` · `FileObject+0xb0` = ctx | `0` | `FileObject+0x50` (`Flags`) 의 비트 `0x400` 끔 (`[추측: 공개 헤더 이름 FO_QUEUE_IRP_TO_THREAD, 포트에 묶이면 IRP 를 스레드 말고 파일 객체 목록에 걸게 함]`), `FileObject+0xb0` = `_IO_COMPLETION_CONTEXT*` (PDB: `Port` +0x0, `Key` +0x8, `UsageCount` +0x10, 크기 0x18) | `+0xae1` (`AND [FO+0x50],0xfffffbff`), `+0xaed/+0xaf4` (`ctx.Port`, `ctx.Key`), `+0xaf8` (`FO+0xb0`), `+0xaff` (status 0), 락 놓기 `+0xb05` |
| 7 | 끝맺음: IRP `+0x30`=status, `+0x38`=0 → 드라이버 호출 없이 `+0x86f` (0x1403abb3f) 로 점프 → 비동기 핸들이면 IRP 의 이벤트 칸을 0 으로, `UserIosb` 를 사용자 IOSB 로 → IRQL 1 로 올려 `IopCompleteRequest` | 반환 = 위 status | **IOSB: 성공일 때만 `Status`=0, `Information`=0** (`IopCompleteRequest +0x125~+0x142`, Information 먼저). **오류면 안 씀** (아래) | `+0xb0a/+0xb0e`, `+0x874` (`CMP R13D,0x103` — 이 갈래는 `0x103` 안 나옴), `+0x88e/+0x89a`, `+0x8a2~+0x8a7`, `+0x8de CALL IopCompleteRequest`, 반환 `+0x940~+0x947 → +0x3c4 MOV EAX,R13D → +0x3d9 RET` |

**포트 참조 (질문 1 의 "포트 참조를 잡나")**
- 성공하면 `ObReferenceObjectByHandle` 로 잡은 참조를 **놓지 않고** ctx 안에 둔다 (성공 줄에 `Dereference` 없음).
- 놓는 곳: 파일 객체가 지워질 때 `IopDeleteFile +0x1a0~+0x1aa → +0x267` (`ctx.UsageCount` ≠ 0 이면 `KeBugCheckEx(0x18…)` `+0x285`), 0 이면 `+0x29a~+0x29d ObfDereferenceObject(ctx.Port)`, `+0x2a9~+0x2ab ExFreePoolWithTag(ctx)`. 핸들을 닫을 때가 아니라 **객체가 사라질 때**다.
- 완료 때마다 잠깐 더 잡았다 놓는 참조는 따로 있다 (`IopCompleteRequest +0x109` 잡고 `+0x29e` 놓음, `UsageCount` 는 `+0x52c` 증가·`+0x4ad` 감소).

**이 IRP 자체가 포트에 패킷을 보내나 → 안 보낸다.** 방금 묶었기 때문에 `IopCompleteRequest` 가 포트 칸을 읽기는 하지만 (`+0xce`, `+0x52c`), `ApcRoutine` (`Irp+0x58`, `NtSetInformationFile +0x6d6` 에서 0) 이 0 이고 `ApcContext` (`Irp+0x60`) 도 0 이라 (`IopAllocateIrpPrivate +0x192` memset, 이 갈래가 안 씀) `+0x35b/+0x360 JZ → +0x244` 에서 패킷 없이 끝난다.

## 종류 `0x29` (`FileIoCompletionNotificationInformation`) 검사 표

갈래 시작 `+0x827 CMP EDI,0x29 / +0x82a JZ → +0xce2`. `RDX = IRP+0x18` = 복사해 둔 입력 (dword 하나), `RBX = 파일 객체`.

| 차례 | 조건 | status | 쓰는 칸 | 줄 (+오프셋) |
|---|---|---|---|---|
| (공통 1~10) | 위 표. 길이표 4, 정렬 4, 접근 0 | | | |
| 1 | `FileObject.Flags & 0x2` (`FO_SYNCHRONOUS_IO`) — **동기 핸들** | `0xC000000D`, IRP `+0x38`=0 | 비트 하나도 안 바꿈 | `+0xceb~+0xcf1 → +0xe8f~+0xe9d` |
| 2 | 입력 dword 의 비트 `0x1` (`FILE_SKIP_COMPLETION_PORT_ON_SUCCESS`) | — | `FileObject.Flags \|= 0x2000000` (`FO_SKIP_COMPLETION_PORT`, 비트 25) | `+0xcfa~+0xd04` (`BTS ECX,0x19`) |
| 3 | 비트 `0x2` (`FILE_SKIP_SET_EVENT_ON_HANDLE`) | — | `Flags \|= 0x4000000` (`FO_SKIP_SET_EVENT`, 비트 26) | `+0xd07~+0xd11` (`BTS ECX,0x1a`) |
| 4 | 비트 `0x4` (`FILE_SKIP_SET_USER_EVENT_ON_FAST_IO_COMPLETION`) | — | `Flags \|= 0x8000000` (비트 27, 이름은 헤더 기억 `[추측]`. 쓰이는 곳 `IopXxxControlFile +0xbc7`) | `+0xd14~+0xd18 → +0x104c~+0x1050` (`BTS ECX,0x1b`) |
| 5 | 입력의 **다른 비트 (`0x8` 이상)** | **검사 없음. 조용히 무시, status 0** | — | 이 갈래 `+0xcfa~+0xd22` 에 마스크 비교가 없다 |
| 6 | 끝맺음: IRP `+0x30`=0, `+0x38`=0 → `+0x86f` (0x1403abb3f) 로 점프 (드라이버 안 부름) → `IopCompleteRequest` | `0` | IOSB `{0, 0}` | `+0xd1e/+0xd22/+0xd26`, 이후 `+0x874~+0x8de` (0x1e 표 7 과 같음) |

- **묶기 (`0x1e`) 전에 불러도 되나 → 된다.** 이 갈래가 `FileObject+0xb0` 을 읽는 줄이 없다.
- **한 번 켠 비트를 끌 수 있나 → 못 끈다.** 세 곳 다 `BTS` (켜기) 뿐이고 끄는 `BTR`·`AND` 가 없다. 입력 비트가 0 이어도 그냥 두는 것이다.
- 비트를 읽는 곳 (이름 없는 비트가 어디에 쓰이는지, 읽은 줄만):
  - `0x2000000`: `IopCompleteRequest +0x366` (패킷 건너뛰기, 아래 질문 3), `IopXxxControlFile +0xe82` (빠른 길, 아래).
  - `0x4000000`: `IopCompleteRequest +0x2a5` (파일 객체 이벤트 안 세움), `IopXxxControlFile +0x721` (호출 앞에서 파일 객체 이벤트 안 비움).
  - `0x8000000`: `IopXxxControlFile +0xbc7` (빠른 길에서 사용자 이벤트 안 세움. 포트가 있고 상태가 경고면 세움 `+0x1651~+0x165b`).

## 내려가지 않는 종류 (I/O 관리자가 스스로 끝내는 것)

`+0x7d5~+0x845` 의 사슬. `IopCallDriverReference` 는 `+0x867` 한 곳이고 `+0x84b` (0x1403abb1b) 로만 들어온다.
- **드라이버를 안 부르는 종류**: `0x10` (모드, `+0x7d5`) · `0x1e` (`+0x815`) · `0x3d` (`FileReplaceCompletionInformation`, `+0x81e`, `IopReplaceCompletionPort` 를 `+0x33aefe` 에서 부름, 포트 안 묶였으면 `0xC000000D`) · `0x29` (`+0x827`) · `0x2a` (`+0x830`, `IopSetFileObjectIosbRange`) · `0x2b` (`+0x839`) · `0x45` (`+0x842`). 위에 따로: `0xe` (동기 핸들만, IRP 만들기 전 `+0x2ba~+0x38d`) · `0x24` (`+0x63a → +0xd50`, 추적 갱신).
- **드라이버로 가는 것 (이름만)**: `0xa/0xb/0x1f/0x41/0x48` (`+0x7de` 의 비트 마스크, 이름 바꾸기·링크 류), `0x28`, `0xd`/`0x40` (삭제), 그 밖 모두 `+0x84b`.

## 질문 3 — 묶인 파일의 I/O 가 끝날 때 (sast/124 를 잇는 한 절)

sast/124 의 표를 다시 읽어 맞췄다. 줄은 `IopCompleteRequest.asm`.

| 상황 (포트에 묶인 비동기 파일, `NtDeviceIoControlFile` 길) | 패킷 | 줄 (+오프셋) |
|---|---|---|
| **바로 오류** (`Irp+0x30` 이 `0xC…`, `Irp+0x41`=0) | **없음.** 포트 칸을 읽지도 않고 (`R14`=0), IOSB·이벤트도 안 건드림 | `+0xb3~+0xbf → +0x426/+0x42a JZ → +0x445`, 다시 `+0x10e~+0x11a → +0x4b7/+0x4bb` (PendingReturned=0 이면 `+0x120` 안 감), `+0x180~+0x198` |
| **바로 성공·정보** (`0x0…`/`0x4…`) + `0x2000000` **켜짐** | **없음** | `+0x366 JNZ → +0x5ac`, `+0x5ac/+0x5b0` (PendingReturned=0), `+0x5b6~+0x5be` (경고 아님), `+0x5c9 MOV EAX,1 → +0x376/+0x378 JNZ → +0x244` |
| **바로 성공·정보** + `0x2000000` 꺼짐 | `ApcRoutine`=0 이고 `ApcContext`≠0 이면 **있음** (`KeInsertQueueEx`, 키는 ctx.Key) | `+0x2d0 → +0x23b`, `+0x35b/+0x360`, `+0x37e~+0x383` (키), `+0x3b4` |
| **바로 경고** (`0x8…`) | **있음** (`0x2000000` 켜져 있어도) | `+0x5b6~+0x5c3 → +0x373` |
| `ApcContext` == 0 | **없음** (어느 경우든) | `+0x35b/+0x360 JZ → +0x244` |
| `ApcRoutine` ≠ 0 | 패킷이 아니라 사용자 APC | `+0x2c9~+0x351` (포트 핸들엔 `ApcRoutine` 을 못 주게 막는 줄은 sast/124 `IopXxxControlFile +0xd3b~+0xd55`) |
| **PENDING 이었다가 나중에 끝남** (`Irp+0x41`=1), 성공이든 오류든 | **있음** (`ApcContext`≠0 이면). `0x2000000` 이 켜져 있어도 건너뛰지 않음 | `+0x426/+0x42a` 를 안 탐, `+0x5ac JNZ → +0x373`. (오류+PENDING 의 동기 핸들·`SYNCHRONOUS_API` 예외는 sast/124 4번) |

**`fast I/O` 길 (`IopXxxControlFile`, 이미 있는 `.asm` 으로 확인)**: 따로 포트에 넣는다. 같은 규칙이다.
- 빠른 길 성공 뒤 `+0x70f JNZ → +0xb16`, `+0xbb4/+0xbbc` 에서 `FileObject+0xb0` 이 있으면 `+0xe82` 로.
- `+0xe82~+0xe96`: `0x2000000` 켜짐 → **경고일 때만** 계속, 아니면 건너뜀. 꺼짐 → `+0xe9c~+0xea9` 오류면 건너뜀. 아니면 `+0xec7 IopIncrementCompletionContextUsageCountAndReadData` 로 포트·키를 읽음.
- `+0xc10/+0xc13 → +0x10a9`: `ApcContext` (`[RSP+0x288]`) 가 0 이면 `+0x10b1/+0x10b4` 에서 패킷 없이 끝. 아니면 `+0x10df IoSetIoCompletionEx` (PDB 별칭 `IoSetIoCompletionEx2`) 로 넣고, 넣기 실패면 status 를 `0xC000009A` 로, 경고면 `0x103` 으로 바꿈 (`+0x10e4~+0x1101`).
- 그 뒤 `+0x110e JMP → +0xc19` 로 닫는다. (빠른 길이 실제로 쓰이는지는 afd 몫 — sast/123 `AfdFastIoDeviceControl`.)

**답: "`ConnectEx` 를 바로 오류로 돌려줄 때 패킷을 안 보내는 것이 맞나" → 예.**
- 조건: 오류가 **PENDING 표시 없이** 돌아올 때 (syscall 반환값이 `0xC000023C` 같은 오류, `0x103` 아님). 이때 `IopCompleteRequest +0xbf → +0x426 → +0x445` 라 포트도 IOSB 도 이벤트도 안 건드린다 (sast/124 와 같은 줄).
- 반례: afd 가 `IoMarkIrpPending` 하고 `0x103` 을 돌려준 뒤 나중에 오류로 끝내면 `ApcContext`≠0 일 때 패킷이 간다. afd 가 어느 쪽인지 정적으로 못 정함 `[공백]` (sast/121 동적 5번 (a)).

## 지금 우리 (PE_emulator) 맞나·틀리나

- `ext/syscall/syscall_layer_cpp/src/file/nt_set_information_file.cpp:145` (실제 파일은 `syscall_implementation/syscall_layer_cpp/src/file/nt_set_information_file.cpp` — `ext/syscall` 이 그걸 가리킴): **틀림.** `if (info_class != 14 && info_class != 16) return stop("unsupported_file_information_class")` 앞에 `30`·`41` 갈래가 없다. 앞쪽 (종류 번호 → 길이 → IOSB 접근 → 정렬 → 핸들 풀기 → 접근권한) 순서와 표 값은 위 공통 2~7 과 같아서 **그대로 쓰면 된다**. 차이 둘: ① 진짜는 IOSB 를 첫 4바이트만 시험하고 (`+0xb1~+0xc7`), 에뮬레이터는 16바이트 접근을 본다 (작은 차이). ② 호출자 모드가 커널이면 길이·정렬 검사를 건너뛴다 (`+0x79`) — 에뮬레이터는 항상 사용자 호출자라면 무관.
- 만들 갈래 (정적으로 정해진 값만):
  - `30`: 동기 핸들 → `0xC000000D`; 이미 묶임 → `0xC000000D`; 포트 핸들 접근 `2`, 종류 완료 포트 아니면 오류 (값은 Ob 규칙); 성공이면 파일 객체에 `포트`·`키 (입력 +0x8)` 를 두고 **포트 참조를 쥔다**, IOSB `{0, 0}`. 오류는 IOSB 안 씀.
  - `41`: 동기 핸들 → `0xC000000D`; 아니면 입력 dword 의 비트 `1/2/4` 를 파일 객체의 "포트 건너뛰기 / 이벤트 안 세움 / 빠른 길 이벤트 안 세움" 칸에 **OR** 만 한다 (끄기 없음, 다른 비트 무시, 묶임 여부 안 봄). IOSB `{0, 0}`.
- `include/syscall_layer/files.hpp:67` `FileObject` (요청서의 `src/files.hpp` 는 실제론 이 경로): **틀림 (칸 없음).** 지금 `access`·`share`·`options`·`synchronous`·`position` 뿐이다. 필요한 칸: 완료 포트 객체 참조 (없으면 "안 묶임"), 키 (64비트), 비트 셋 (`0x2000000/0x4000000/0x8000000`). 소켓 핸들이 `ObjectKind::file` 로 풀리는지는 에뮬레이터 쪽 문제 `[공백]` (`handles_.resolve(a[0], ObjectKind::file)` 가 소켓에 대해 `0xC0000024` 를 내면 이 갈래까지 못 온다).
- `tests/file.cpp`: 새로 필요한 시험 (진짜 값 기준): `0x1e` 성공 IOSB `{0,0}` · 두 번째 `0x1e` → `0xC000000D` 와 **IOSB 안 바뀜** · 동기 핸들 `0x1e`/`0x29` → `0xC000000D` · `Length` 15 → `0xC0000004` · `0x29` 로 켠 비트가 안 꺼짐 · `0x29` 를 `0x1e` 전에 불러도 성공.
- `src/threadpool/nt_io_completion.cpp` (패킷 넣기): **`ConnectEx` 가 바로 오류로 돌아올 땐 부르지 않는다 (지금 방식 맞음).** 부르는 경우는 위 질문 3 표 (바로 성공 + 비트 꺼짐 + `ApcContext`≠0 / 경고 / PENDING 뒤 완료) 뿐이다.

### 범위 밖에서 눈에 띈 것 (같은 완료 길이라 적어 둠)

- `nt_set_information_file.cpp:173` 이 클래스 16 의 `0xC000000D` 에서 `write_io_status(io_status, 0xc000000d, 0)` 으로 IOSB 에 쓴다 (주석: "디컴파일에서 읽은 추론"). 이 asm 으로는 **안 쓰는 쪽**이다: 클래스 16 의 잘못된 모드 값은 `+0xbe8 MOV R13D,0xc000000d → +0xb0a (IRP status)` 로 들어가 같은 `IopCompleteRequest` 를 타고, "오류 + PENDING 아님" 이라 IOSB 를 안 쓴다 (`IopCompleteRequest +0xb3~+0xbf`, `+0x10e~+0x11a → +0x4b7 → +0x4c1`). 위 `0x1e`·`0x29` 오류도 같다. 동적으로 한 번 확인할 만하다 (아래 3번).
- 비동기 핸들에서 성공하면 파일 객체 이벤트 (`FileObject+0x98`) 가 세워지고 `FinalStatus` (`+0x38`) 가 status 로 바뀐다 (`IopCompleteRequest +0x2a5~+0x2c1`, `FO_SKIP_SET_EVENT` 꺼져 있을 때). 에뮬레이터가 파일 객체 이벤트를 안 모델하면 보이지 않는다.

## 동적으로 확인할 것

정적은 "코드에 이렇게 쓰여 있다" 까지다.

1. **Go 가 실제로 내는 값**: `NtSetInformationFile` 호출 때 클래스 `0x1e` 의 입력 (포트 핸들·키), 클래스 `0x29` 의 dword (Go 는 `SetFileCompletionNotificationModes` 에 `1|2 = 3` 을 줄 것이라는 기억 `[추측]`) 와 반환값. 멈춤점 `nt!NtSetInformationFile+0xb0a` (`r13d`=status, `rbx`=FileObject, `rsi`=IRP): `dt nt!_FILE_OBJECT @rbx CompletionContext Flags`, `dt nt!_IO_COMPLETION_CONTEXT poi(@rbx+0xb0)`. `0x29` 는 `+0xd1e` 에서 `dwo(@rbx+0x50)`.
2. **소켓 핸들이 비동기 (`Flags&2`==0)** 인지, 앞 호출이 `0xC000000D` 가 아닌지 (`+0xa53`, `+0xa61` 에 걸림).
3. **오류 때 IOSB 를 안 쓴다** 확인: 이미 묶은 핸들에 `0x1e` 를 한 번 더 부르고, 호출 전 IOSB 에 표식 값 (예: `0xdeadbeefdeadbeef`) 을 넣어 두었다 후에 그대로인지. `nt!IopCompleteRequest+0x4c1` 에 걸리면 맞다.
4. **`ConnectEx` (`0x120c7`, `AfdSuperConnect`) 가 망 없는 PC 에서 바로 오류인가 PENDING 후 오류인가**: `nt!IopCompleteRequest+0x4b7` 의 `Irp+0x41` 값, `+0x3b4` (패킷 넣기) 가 걸리는지. 요청 sast/121 동적 5번 (a) 와 같은 질문.
5. `0x1e` 직후 같은 IRP 가 `IopCompleteRequest+0x3b4` 에 **안 걸리는지** (`ApcContext`=0 이라 패킷이 없다는 정적 읽기 확인).
6. 포트 참조 수: `0x1e` 전후 완료 포트 객체의 포인터 참조 수가 1 늘고, 소켓 핸들을 닫아도 파일 객체가 지워질 때까지는 그대로인지 (`IopDeleteFile+0x29d`).

추천 멈춤점 (test.exe/Go 프로세스만 걸리게 조건 필요, 런북은 dast-mentor 몫): `nt!NtSetInformationFile+0xa95` · `+0xb0a` · `+0xd1e` · `+0x1041` (경주 오류) · `nt!IopCompleteRequest+0x4c1` · `+0x3b4` · `nt!IopXxxControlFile+0xe82` (빠른 길 포트 확인).

## 다음 요청 후보

- **afd 쪽**: `0x120c7` (`AfdSuperConnect`) 가 `IoMarkIrpPending` 을 부르는지, 망 없는 PC 에서 어디서 `0xC000023C` 를 만드는지 (`AfdFastIoDeviceControl` 이 이 코드를 처리하는지 포함) — 질문 3 의 `[공백]` 을 닫는다. sast/121·123 와 이어짐.
- **`IopReplaceCompletionPort` (`0x3d`, rva `0x4bf098`, 221 B)**: Go 가 쓸 일은 없어 보이지만 포트 바꾸기까지 가면 필요.
- **`NtRemoveIoCompletion` / `IoSetIoCompletionEx` (`0x308690`, 956 B)**: 패킷이 `GetQueuedCompletionStatus` 로 나올 때의 모양 (Key·ApcContext·IOSB). sast/124 가 이미 후보로 올린 것.
- **`IopSetFileObjectIosbRange` (`0x2a`)**: 에뮬레이터가 이 종류를 받을 때만.

## 스크립트 문제

없음. export 정상 종료 (종료 코드 0), 로그에 `SCRIPT ERROR` 없음. 읽지 않은 틈 `NtSetInformationFile +0x157` 등 5곳은 SEH 착지점이라 원본 바이트를 `objdump` 로 따로 봤다 (`+0x157` = `jmp +0x3c7`, 예외 코드를 EAX 로 돌려주고 끝). 스크립트 문제가 아니라 설명대로 나온 것.

## 판정

**분석성공** — 요청서 질문 1~3 모두 `.asm` 줄로 답했다. 정적으로 못 정한 것: ① 포트 핸들 풀기 실패 때 조건별 status (`ObpReferenceObjectByHandleWithTag` 안 매핑, 상수만 확인), ② 잘못된 정렬·나쁜 포인터가 예외로 오는지 status 로 오는지 (착지점 모양만 봄, `[추측]`), ③ `ConnectEx` 오류가 PENDING 없이 오는지 (afd 몫), ④ 비트 `0x8000000` 의 헤더 이름.
