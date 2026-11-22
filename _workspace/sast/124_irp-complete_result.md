# 124 irp-complete — 정적 분석 결과

- 요청서: `_workspace/sast/124_irp-complete.md` (Q219 의 R6 몫, sast/123 afd·mswsock 과 같은 시간에 돌림)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb `ntkrnlmp.pdb` guid+age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 이미 분석된 프로젝트라 analyze 는 안 했다 (`.gpr` + `_index.tsv` 34982줄 + analyze 로그 `SetPdb:` 줄 다 있음).
- 뽑은 함수:
  - `^(IopSynchronousServiceTail|IopCompleteRequest|IopfCompleteRequest)$` depth 1 → 54개. 세 함수의 `.asm`/`.c` 가 이제 있다.
  - **요청서 범위를 조금 넓힘**: `^(NtDeviceIoControlFile|NtFsControlFile)$` depth 0 (2개, 각 99바이트). 이유: "NtDeviceIoControlFile 이 완료를 누가 하게 만드나" 가 이 둘의 마지막 인자 한 칸 (`[RSP+0x50]` = 1 또는 0) 으로 갈리는데, 그걸 `IopXxxControlFile` 쪽에서만 보면 추측이 돼서 입구 두 개를 같이 봤다.
- 출력: `ms/disassemble/ntoskrnl/{IopSynchronousServiceTail,IopCompleteRequest,IopfCompleteRequest}.asm`, 같은 이름 `ms/decomp/ntoskrnl/*.c`, `_calls.tsv` 에 줄 추가.
- 버전 확인: `NtCreateUserProcess +0xc36 → PspAllocateProcess` 일치, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv`). 이미 분석된 모듈이라 이 둘만 봤다.
- 표기: 이 문서의 `함수 +0x…` 는 전부 `ms/disassemble/ntoskrnl/<함수>.asm` 의 줄이다. call 은 `call 자리/ret 자리` 를 같이 적고, 점프 목적지는 "목적지 절대 주소 − 함수 시작" 으로 셌다.

## 먼저 읽는 한 줄 답

**`NtDeviceIoControlFile` 로 보낸 IRP 를 드라이버가 PENDING 없이 끝내면:**
- **성공·경고 (`0x0…`/`0x4…`/`0x8…`)**: 호출한 스레드 안에서 `IopCompleteRequest` 가 **IOSB 를 쓰고** (Information 먼저, Status 나중), 사용자 이벤트를 **세우고**, 동기 핸들이면 파일 객체 이벤트·`FinalStatus` 도 처리하고, `ApcRoutine` 이 있으면 사용자 APC 를 걸고, 없고 포트 연결 핸들이면 완료 패킷을 넣는다. syscall 반환값은 **드라이버 status 그대로**.
- **오류 (`0xC…`)**: IOSB **안 씀**, 사용자 이벤트 **안 세움** (참조만 풂), 파일 객체 이벤트·`FinalStatus` **안 건드림**, APC·포트 **없음**. status 만 돌려준다.
- **지금 에뮬레이터 K02 ("status 만 · IOSB 안 씀 · 이벤트 비운 채")** 는 **오류 때만 맞고, 성공·경고 때는 틀리다.**

## IRP·파일 객체 칸 (PDB 로 확인한 오프셋)

`pdb_struct.py ntoskrnl.exe _IRP _FILE_OBJECT _IO_COMPLETION_CONTEXT` 결과와 asm 이 맞는다.

| 칸 | 뜻 |
|---|---|
| Irp+0x10 `Flags` | 아래 비트 표 |
| Irp+0x30 `IoStatus.Status` / +0x38 `Information` | 드라이버가 채운 결과 |
| Irp+0x41 `PendingReturned` | 드라이버가 `IoMarkIrpPending` 한 IRP 면 1. **"PENDING 없이" = 0** (`IopfCompleteRequest +0xbe/+0xc3` 이 스택 칸의 `Control & 1` 을 여기에 복사) |
| Irp+0x47 `AllocationFlags` | `0x10` = 32비트 IOSB 표식 (아래) |
| Irp+0x48 `UserIosb` / +0x50 `UserEvent` | 사용자가 준 IOSB 포인터 / (참조를 잡은) 이벤트 객체 |
| Irp+0x58 `UserApcRoutine` / +0x60 `UserApcContext` | `ApcRoutine` / `ApcContext` |
| FileObject+0x38 `FinalStatus` / +0x98 `Event` | 동기 핸들이 기다리는 쪽 |
| FileObject+0xb0 `CompletionContext` | 포트에 연결됐으면 비어 있지 않음 (+0x0 `Port`, +0x8 `Key`) |

비트 이름은 공개 헤더의 값과 같다 `[추측: PDB 로는 이름이 안 나옴, 쓰이는 모양이 맞음]`:
Irp.Flags `0x4`=SYNCHRONOUS_API · `0x800`=DEFER_IO_COMPLETION · `0x1000`=OB_QUERY_NAME · `0x2000`=파일 객체 IRP 목록에 걸림 (`IopQueueIrpToFileObject +0xd7` 이 세움 — 코드 확인) · `0x200000`=IoRing.
FileObject.Flags `0x2`=FO_SYNCHRONOUS_IO · `0x4`=FO_ALERTABLE_IO · `0x2000000`=FO_SKIP_COMPLETION_PORT · `0x4000000`=FO_SKIP_SET_EVENT.

## 표 — PENDING 없이 끝났을 때 (Irp+0x41 = 0, `NtDeviceIoControlFile` 길)

`NT_ERROR` 검사 = `(Status & 0xC0000000) == 0xC0000000` (`IopCompleteRequest +0xb3~+0xbf`). 성공·정보·경고는 이 검사에 안 걸려 같은 길을 탄다.

| | 반환 status | IOSB 에 쓰나 | 사용자 이벤트 | 파일 객체 이벤트 / FinalStatus | 완료 포트 | APC (사용자) | 줄 (+오프셋) |
|---|---|---|---|---|---|---|---|
| **성공 / 동기 핸들** | 드라이버 status 그대로 | **씀** (Information → Status) | 줬으면 **세움**, 그다음 참조 풂 | **세움** + `FinalStatus = Status` (이벤트를 줬어도 동기 핸들이라 세움) | 이 함수만 보면 `CompletionContext` 가 있으면 비동기와 같은 길. 동기 핸들에 그게 붙는지는 이 범위 밖 `[공백]` | `ApcRoutine≠0` 이면 건다 | IOSB `IopCompleteRequest +0x125~+0x142` · 이벤트 `+0x3d5` · 파일 이벤트 `+0x3f8~+0x411 → +0x2ae/+0x2ba` · `FinalStatus` `+0x2c1` · APC `+0x351` · 반환 `IopSynchronousServiceTail +0x3f5` |
| **성공 / 비동기 핸들** | 드라이버 status 그대로 | **씀** | 줬으면 **세움** | 이벤트를 **안 줬을 때만** 세움 (+ `FO_SKIP_SET_EVENT` 가 꺼져 있을 때) + `FinalStatus` 갱신. 이벤트를 줬으면 **안 건드림** | `CompletionContext≠0` 이고 `ApcRoutine==0` 이고 `ApcContext≠0` 이면 **패킷 넣음**. 단 `FO_SKIP_COMPLETION_PORT` 가 켜져 있으면 PENDING 없는 성공은 **건너뜀** | `ApcRoutine≠0` 이면 건다 (포트 연결 핸들엔 `ApcRoutine` 을 아예 못 줌 → `0xC000000D`) | 파일 이벤트 `IopCompleteRequest +0x177~+0x2c4` · 건너뛰기 `+0x35b~+0x5c9` · 패킷 `+0x3b4` · `ApcRoutine` 거절 `IopXxxControlFile +0x237/+0xd3b~+0xd55` · 반환 `IopSynchronousServiceTail +0x3fe~+0x420` |
| **경고 / 동기 핸들** | 드라이버 status 그대로 | **씀** | 줬으면 **세움** | 성공/동기와 같음 | 성공/동기와 같음 | 같음 | 성공/동기와 같음 (경고는 `NT_ERROR` 가 아님) |
| **경고 / 비동기 핸들** | 드라이버 status 그대로 — **단 `CompletionContext≠0` 이면 `0x103` 으로 바꿔 돌려줌** | **씀** | 줬으면 **세움** | 성공/비동기와 같음 | 같음. `FO_SKIP_COMPLETION_PORT` 가 켜져 있어도 **경고는 패킷을 넣음** | 같음 | 반환 바꾸기 `IopSynchronousServiceTail +0x403~+0x41b` · 경고는 건너뛰기 예외 `IopCompleteRequest +0x5b6~+0x5c3` |
| **오류 / 동기 핸들** | 드라이버 status 그대로 | **안 씀** | **안 세움** (참조만 풂, 이미 호출 앞에서 비워 둔 채) | **안 세움**, `FinalStatus` 도 그대로 | 없음 (포트 찾는 줄 자체를 건너뜀) | 없음 | `IopCompleteRequest +0x426→+0x445` (포트 건너뜀) · `+0x4b7~+0x4ec` (이벤트 참조만 풂) · `+0x180~+0x198` (IRP 그냥 풂) · `+0x1d3/+0x1db` |
| **오류 / 비동기 핸들** | 드라이버 status 그대로 | **안 씀** | **안 세움** | **안 세움** | 없음 | 없음 | 오류/동기와 같음 |

표 읽는 법:
- "반환 status" 는 `IopCallDriverReference` 가 `IofCallDriver` 반환값을 그대로 돌려주고 (`+0xe1/+0xe6` → `+0x131`), `IopSynchronousServiceTail` 이 `+0x30b/+0x30d` 에 담아 `+0x420` 에서 돌려주는 값이다. `IopXxxControlFile` 도 그 값을 안 바꾼다 (`+0x997/+0x99c` 뒤 바로 `RET`).
- 사용자 이벤트는 호출 **앞에서** 먼저 비워진다 (`IopXxxControlFile +0x2db` `KeClearEvent`, 파일 객체 이벤트도 `FO_SKIP_SET_EVENT` 가 꺼져 있으면 `+0x730` 에서 비움). 그래서 오류일 때 "비운 채" 가 정확히 그 상태다.
- IOSB 는 8바이트(상태 4 + 정보 4) 또는 16바이트 (상태 4 + 빈칸 4 + 정보 8) 로 **칸 따로** 쓴다: `Information` 먼저 (`+0x133` 또는 `+0x12f`), `Status` 나중 (`+0x142`). 32비트 호출자면 `Information` 이 +4 에 4바이트 (`+0x13c`). 이 표식은 `ApcRoutine` 의 맨 아랫 비트 — `IopSynchronousServiceTail +0x34~+0x40` 이 읽고 (`OR [Irp+0x47],0x10`, `AND RDX,-2`) 비트를 지운다.
- IOSB 쓰기는 예외를 삼키는 구조다. 예외 필터 `IopCompleteRequest$filt$0` 은 항상 1 (처리) 을 돌려주고 (`seh:…$filt$0+0x55`), 착지점은 사용자 이벤트 검사로 이어진다 (읽지 않은 틈 `+0x146~+0x169` 를 원본 바이트로 디스어셈블: `xor r12d,r12d` → 레지스터 다시 읽기 → `mov rcx,[rbx+0x50]; test rcx,rcx; jnz` = `+0x16a` 와 같은 코드). 즉 IOSB 주소가 나빠도 syscall 이 죽지 않고 이벤트 단계로 넘어간다.

## 질문별 답 [정적]

### 1. PENDING 없이 끝났을 때 (가) IOSB (나) 이벤트 (다) 포트 (라) APC

이 일은 전부 `IopCompleteRequest` (IRP 의 `Tail.Apc` 를 첫 인자로 받는 함수, `ms/disassemble/ntoskrnl/IopCompleteRequest.asm`) 가 한다. 누가 언제 부르나가 두 갈래다.

- **`NtDeviceIoControlFile` 길 (우리 `0x12007`/`0x120bf` 가 쓰는 길)**:
  - `NtDeviceIoControlFile +0x13` 이 마지막 인자 `[RSP+0x50]` 에 **1** 을 쓴다 (`NtFsControlFile +0x13` 은 **0**). 이게 `IopXxxControlFile` 에서 `DeviceIoControl` 칸이 되고, `IopXxxControlFile +0x7e2~+0x7eb` 에서 `MajorFunction = 0xd + (그 값≠0)` = 0xe (DEVICE_CONTROL).
  - `DeviceIoControl=1` 이면 Irp.Flags 에 `0x800` (DEFER) 을 **안 세운다** (`IopXxxControlFile +0x937~+0x93b`: `TEST BL,BL; JNZ` 가 `OR [R13+0x10],0x800` 을 건너뜀). 그리고 `IopSynchronousServiceTail` 의 4번째 인자 `R9B` 가 0 (`+0x96a~+0x96f`: `[RSP+0x72]==0` 의 `SETZ`).
  - 그래서 완료는 **드라이버가 `IoCompleteRequest` 를 부르는 그 자리에서** 시작된다: `IofCompleteRequest +0xe/+0x13` (래퍼, `IopDispatchCompleteRequest==0` 일 때) → `IopfCompleteRequest`. 거기서 `IopCompleteRequest` 를 **그 자리에서 직접** 부르거나 (`IopfCompleteRequest +0x62d/+0x632`), 아래 조건 중 하나라도 걸리면 **특수 커널 APC 로 걸어 둔다** (`+0x684` `KeInitializeApc`, 그다음 `+0x3eb` `KeInsertQueueApc`):
    - IRP 를 만든 스레드가 지금 스레드가 아님 (`+0x5d1~+0x5d4`)
    - `KeAreAllApcsDisabled()` 가 참 (`+0x5d6~+0x5dd`)
    - 스레드가 다른 프로세스에 붙어 있음 (`[Thread+0x24a]==1`, `+0x5e8~+0x5ef`)
    - IRP 가 취소됨 (`Irp+0x44≠0`, `+0x5be→+0x69e`)
  - 어느 쪽이든 같은 스레드가 syscall 을 끝내고 유저로 돌아가기 전에 `IopCompleteRequest` 가 돈다 `[추측: 특수 커널 APC 는 IRQL 이 내려갈 때 바로 배달되는 보통 동작]`. 어느 갈래를 실제로 타는지는 동적 `[공백]`.
- **`NtFsControlFile` 길 (비교용)**: DEFER 가 세워지고 (`IopfCompleteRequest +0x454~+0x466`: `Flags&0x800` 이고 `PendingReturned==0` 이면 아무것도 안 하고 끝냄), 대신 `IopSynchronousServiceTail` 가 `IRQL 1` 로 올려서 `IopCompleteRequest` 를 직접 부른다 (`IopSynchronousServiceTail +0x314~+0x328` 에서 4번째 인자≠0 이고 status≠0x103 이면 `+0x39c/+0x3a1`). 결과 효과는 같고 부르는 자리만 다르다.

`IopCompleteRequest` 안 순서 (오류가 아닐 때):
1. 버퍼드 I/O 복사 (`IopProcessBufferedIoCompletion`, `+0x94/+0x99`) → MDL 해제 (`+0x5d6`).
2. 포트·키 찾기: `FileObject+0xb0` 이 있으면 참조를 잡는다 (`+0xc5~+0x109`).
3. **(가) IOSB 쓰기** `+0x125~+0x142` (위 표 설명).
4. **(나) 이벤트**: `UserEvent≠0` → `KeSetEvent` `+0x3d5`, `ObfDereferenceObjectWithTag` `+0x3f3` (`Flags&4==0` 일 때), 동기 핸들이고 `Flags&0x1000==0` 이면 파일 객체 이벤트도 `+0x2ba`. `UserEvent==0` → 파일 객체 이벤트 `+0x2ba` (`FO_SKIP_SET_EVENT` 없을 때). 두 경우 모두 `FinalStatus = Status` (`+0x2c1`).
5. 전송량 세기 `IopUpdateIrpTransferCount` (`+0x1fc`), 스레드/파일 객체 목록에서 IRP 빼기 (`+0x21d` 또는 `+0x3c1`).
6. **(라) APC**: `Irp+0x58≠0` (맨 아랫 비트 지운 값) → Tail.Apc 를 채워 `KeInsertQueueApc(Apc, UserIosb, 0, 2)` (`+0x2c9~+0x351`). `KernelRoutine`·`RundownRoutine` 은 같은 주소 `0x140a3bb40` (PDB: `IopUserCompletion` = `IopUserRundown`, 한 주소에 이름 둘). `NormalRoutine = UserApcRoutine`, `NormalContext = UserApcContext`, 모드 = `Irp->RequestorMode`. → 사용자 APC 는 (ApcContext, IOSB 포인터, 0) 으로 불리게 큐에만 들어간다. 배달은 이름만 (범위 밖).
7. **(다) 포트**: `ApcRoutine==0` 이고 포트가 있고 `Irp+0x60 (ApcContext)≠0` 이면 `KeInsertQueueEx(Port, &Irp->Tail.Overlay.ListEntry(Irp+0xa8), 플래그)` (`+0x35b~+0x3b4`). 키는 `[Tail 첫 칸] = Key` 로 넣는다 (`+0x37e~+0x383`). 플래그는 `FileObject->DeviceObject` 의 DeviceType 이 0x8 또는 0x14 이면 1 (`+0x38e~+0x3a1`). `ApcContext==0` 이면 패킷을 안 넣는다 (`+0x35b/+0x360`).
8. 다 끝나면 IRP 를 풀고 (`IoFreeIrp` `+0x251`) 파일 객체 참조를 푼다 (`ObDereferenceObjectDeferDeleteWithTag` `+0x27a`).

### 2. status 별로 (성공 · 경고 · 오류) — 오류 때 IOSB 를 안 쓰는 갈래가 있나

**있다.** 오류이고 `PendingReturned==0` 일 때다.
- `IopCompleteRequest +0xb3~+0xbf`: `(Status & 0xC0000000) == 0xC0000000` 이면 `+0x426` 으로 간다.
- `+0x426`: `CMP [RBX+0x41],R12B` → `PendingReturned==0` 이면 `+0x445` 로 가서 포트·키 찾기를 **건너뛴다**.
- `+0x10e~+0x11a` 에서 오류를 한 번 더 검사해 `+0x4b7` 로 간다: `PendingReturned≠0` 이면 IOSB 쓰기 (`+0x120`) 로, **`==0` 이면 `+0x4c1~+0x4ec` 에서 사용자 이벤트 참조만 풀고** `+0x180` 로 간다 (IOSB 쓰기·이벤트 세우기·`FinalStatus` 모두 건너뜀).
- `+0x180~+0x18e`: 오류이고 `PendingReturned==0` 이면 그냥 IRP 를 풀고 끝낸다 (`+0x1d3/+0x1db`). APC·포트 없음.
- 성공·정보·경고는 `NT_ERROR` 가 아니라 모두 IOSB·이벤트 쪽 길로 간다. 성공과 경고의 차이는 딱 둘: ① 비동기 + 포트 연결 핸들에서 경고면 syscall 반환이 `0x103` 이 됨 (`IopSynchronousServiceTail +0x40d~+0x41b`) ② `FO_SKIP_COMPLETION_PORT` 일 때 경고는 패킷을 넣고 성공은 안 넣음 (`IopCompleteRequest +0x5ac~+0x5c9`).

**참고 — 오류인데 `PendingReturned≠0` (드라이버가 `IoMarkIrpPending` 한 뒤 오류로 끝냄)**: IOSB 쓰고 이벤트·`FinalStatus` 도 처리한다 (`+0x120` 으로 감). 그다음 IRP 를 버리는 갈래: `SYNCHRONOUS_API` 이거나 동기 핸들 (`+0x4f1~+0x512`) 이면 APC·포트 없이 IRP 풀기, 비동기 핸들이면 성공처럼 APC·포트까지 간다.

### 3. 핸들별 (동기 `FO_SYNCHRONOUS_IO` / 비동기)

- 동기 핸들은 `IopXxxControlFile` 에서 파일 객체 락을 잡고 (`+0xc5f~+0xcdc`, `[RSP+0x50]=1` 은 `+0xcc7`) 그 값이 `IopSynchronousServiceTail` 의 6번째 인자 (`R12B`) 로 간다 (`IopXxxControlFile +0x97b/+0x980`).
- **PENDING 없음**: 반환은 드라이버 status 그대로. 동기든 비동기든 `IopSynchronousServiceTail` 은 오류를 바꾸지 않는다. 동기 핸들은 파일 객체 락만 풀고 (`+0x3f0`) 그대로 돌려준다 (`+0x3f5`). 완료 때 파일 객체 이벤트·`FinalStatus` 는 이미 위 4번 단계에서 세워졌다 (성공·경고일 때만).
- 반환 status 가 드라이버 값과 **달라지는 경우** (찾은 것 전부):
  1. 동기 + PENDING → `IopWaitForSynchronousIo` 가 돌려주는 `FileObject->FinalStatus` (`IopWaitForSynchronousIo +0x40`).
  2. 비동기 + `FileObject+0xb0≠0` + 경고 → `0x103` (`IopSynchronousServiceTail +0x41b`).
  3. IoRing 플래그 (`Irp.Flags & 0x200000`) → `0x103` (`+0x401/+0x41b`). 범위 밖.
  4. AppContainer 프로세스가 특정 파일 객체 (`FileObject+0xd0` 의 칸 `&4`) 에 보내면 드라이버를 안 부르고 `0xC0000910` 으로 바로 닫음 (`+0x186~+0x1b6`) `[추측: 이 갈래가 실제로 타는 대상은 정적으로 모름]`.
  5. 그 밖엔 드라이버 값 그대로.

### 4. PENDING 일 때 (참고, 짧게)

- **동기 핸들**: `IopSynchronousServiceTail +0x3ca~+0x3e1` 에서 `status==0x103` 이면 `IopWaitForSynchronousIo(Irp, FileObject, 대기모드)` 를 부른다. 그 안에서 **`FileObject->Event` (`FileObject+0x98`)** 를 `KeWaitForSingleObject` 한다 (`IopWaitForSynchronousIo +0x60~+0x7a`: 알림 가능 대기 `R9B=1`, 대기 모드는 `FO_ALERTABLE_IO` 가 있으면 요청자 모드, 없으면 커널 모드). 이미 세워져 있으면 기다리지 않고 바로 `FinalStatus` 를 돌려준다 (`+0x2e~+0x40`). 끝나면 `IopReleaseFileObjectLock` (`+0x3f0`) 후 반환. 호출 앞에서 `FileObject->Event` 를 비워 둔 것 (`IopXxxControlFile +0x730`) 과 짝.
- **비동기 핸들**: 기다리지 않고 `0x103` 을 그대로 돌려준다 (`IopSynchronousServiceTail +0x3c5→+0x3fe→+0x420`; `0x103` 은 경고 비트가 아니라 어느 쪽 바꾸기에도 안 걸림). IOSB·이벤트·APC·포트는 나중에 드라이버가 끝낼 때 같은 `IopCompleteRequest` 가 처리한다 (`PendingReturned≠0` 이라 오류여도 IOSB·이벤트 처리).

## 지금 우리 (PE_emulator) 맞나·틀리나

- **틀림 (성공·경고, PENDING 없음)**: 진짜는 IOSB 를 쓰고 이벤트를 세운다. K02 가 "IOSB 안 씀·이벤트 비운 채" 면 이 둘이 다르다.
  - mswsock 은 PENDING 이 아니면 IOSB·이벤트를 안 봐서 (sast/121) **mswsock 쪽 눈에는 안 드러난다.** 하지만 `NtDeviceIoControlFile` 계약이 아니고, 그 호출자가 이벤트를 `NtWaitForSingleObject` 하거나 IOSB 를 직접 읽는 다른 호출자 (다른 DLL) 에서는 깨진다.
- **맞음 (오류, PENDING 없음)**: IOSB 안 씀·이벤트 안 세움·status 만 반환. 이벤트는 호출 앞에서 `KeClearEvent` 로 비워 두는 것까지 맞아야 한다 (`IopXxxControlFile +0x2db`).
- **고치는 법 (성공·경고, PENDING 없음, 비동기·동기 모두)** — 이 순서로:
  1. (버퍼드 IOCTL 이면) 출력 버퍼 복사. `METHOD_NEITHER` (`0x12007` 등 코드 아랫 2비트 3) 는 드라이버가 사용자 버퍼에 직접 쓰니 이 단계 없음 `[추측: 코드 값의 아랫 2비트가 방식이라는 일반 규칙. IopXxxControlFile 의 방식 분기는 이번에 안 읽음]`.
  2. IOSB 쓰기: `+0x0` 에 `Status` (4바이트), `+0x8` 에 `Information` (8바이트), 순서는 Information 먼저. (32비트 표식이면 4+4.)
  3. 사용자 이벤트가 있으면 세운다.
  4. 파일 객체 이벤트: 이벤트를 안 줬으면 (비동기든 동기든) 세우고 `FinalStatus = Status`. 이벤트를 줬으면 동기 핸들일 때만 세운다. (`FO_SKIP_SET_EVENT` 면 안 세움.)
  5. `ApcRoutine≠0` 이면 사용자 APC 를 (ApcRoutine, ApcContext, IOSB 포인터, 0) 로 큐에 넣는다. 아니면 포트에 연결된 핸들이고 `ApcContext≠0` 이면 패킷 (Key, ApcContext, Status, Information) 을 포트에 넣는다. `FO_SKIP_COMPLETION_PORT` 가 켜져 있으면 PENDING 없는 성공은 건너뛰고 경고는 넣는다.
  6. 반환: 드라이버 status 그대로. 예외는 "비동기 + 포트 연결 + 경고 → `0x103`".
- 고치기 전에 확인할 것: AFD 가 이 IOCTL 을 **빠른 길 (FastIo)** 로 처리해서 IRP 길을 아예 안 타는 건 아닌지 (sast/123 몫). 빠른 길은 이번 범위 밖이고 IOSB 16바이트 통째 복사 + 이벤트 세우기만 하는 다른 모양이다 (`IopXxxControlFile +0xb38~+0xb48`, `+0xbe4`, 요청서 비교용).

## 동적으로 확인할 것

정적으로는 "코드에 이렇게 쓰여 있다" 까지다. 실제로 이 길을 타는지는 아래가 동적이다.

1. `NtDeviceIoControlFile` 로 보낸 AFD IRP 가 **PENDING 없이** 끝나는지 (드라이버가 `IoMarkIrpPending` 안 부르는지): Irp+0x41 값. AFD 쪽 코드는 sast/123.
2. 완료가 **인라인** 인지 **큐잉된 특수 커널 APC** 인지 (`IopfCompleteRequest +0x62d` 에 걸리나, `+0x684` 에 걸리나). 둘 다 같은 효과지만 정확한 순서 확인용.
3. `IopSynchronousServiceTail` 4번째 인자 (`R9B`) 가 `NtDeviceIoControlFile` 일 때 0 이고 `Irp.Flags & 0x800` 이 없는지.
4. 파일 객체 큐 갈래: `IopSynchronousServiceTail +0xa4` 의 `Feature_3043197241` 값으로 새 길 (`IopQueueIrpForServiceTail`) 인지 옛 길 (`+0x126/+0x16f`) 인지, 옛 길이면 우리 핸들이 `Irp.Flags & 0x2000` 갈래를 타는지 (포트 연결 핸들 + `ApcContext≠0` + 이벤트 없음일 때만, `IopSynchronousServiceTail +0x108~+0x11d`). 그 갈래는 `IopfCompleteRequest +0x58b` 의 `IopCompleteIrpInFileObjectList` 를 타고 끝에 `IopCompleteRequest` 를 부른다 (그 함수 `+0x69e/+0x6a3`) — 안은 안 읽음.
5. 동기 핸들에 `FileObject+0xb0` (포트) 가 실제로 붙는 일이 있는지.

추천 멈춤점 (test.exe 만 걸리게 조건 필요, 런북은 dast-mentor 몫):
- `nt!IopCompleteRequest+0x120` — IOSB 쓰기 입구. `rbx` = Irp: `+0x30` Status, `+0x41` PendingReturned, `+0x48` UserIosb, `+0x50` UserEvent, `+0x58` ApcRoutine, `+0x60` ApcContext.
- `nt!IopCompleteRequest+0x4c1` — 오류 + PENDING 없음, 이벤트 참조만 푸는 자리 (여기 걸리면 IOSB·이벤트 안 건드린다는 증거).
- `nt!IopCompleteRequest+0x3d5` (사용자 이벤트 `KeSetEvent`) · `+0x2ba` (파일 객체 이벤트 `KeSetEvent`) · `+0x351` (사용자 APC) · `+0x3b4` (포트 패킷).
- `nt!IopSynchronousServiceTail+0x30b` (드라이버가 돌려준 `eax`) 와 `+0x420` (최종 `eax`) — 둘 비교로 "status 가 바뀌나" 를 본다.
- `nt!IopfCompleteRequest+0x62d` (인라인) / `+0x3eb` (큐잉).

## 다음 요청 후보

- **`IopUserCompletion`(=`IopUserRundown`, `0xa3bb40`)** — 사용자 APC 가 배달될 때 커널 루틴이 `NormalRoutine`·인자를 바꾸는지, 사용자 APC 가 (ApcContext, IOSB, 0) 로 불리는 게 맞는지. 에뮬레이터의 APC 배달 모양을 정하는 데 필요.
- **`IopCompleteIrpInFileObjectList` · `IopQueueIrpForServiceTail`** — 포트 연결 핸들 + 이벤트 없는 비동기 IOCTL 이 타는 길 (우리 `0x120bf` 가 포트에 연결돼 있다면).
- **`IoRemoveIoCompletion` / `KeInsertQueueEx` 쪽 패킷 모양** — `NtRemoveIoCompletion` 이 돌려주는 값 (Key·ApcContext·IOSB) 을 맞추려면. 이번엔 "완료 포트 내부" 라 이름만 남겼다.
- **`IopXxxControlFile` 의 방식 (METHOD_*) 분기** — 입출력 버퍼를 어떻게 만들고 (`+0x86b~+0x8b6`, `+0xf14~+0xfd6`) 출력이 언제 사용자 버퍼에 돌아가는지.
- **AFD 의 IOCTL 디스패치에서 `IoMarkIrpPending`/`IoCompleteRequest` 를 부르는 줄** (sast/123 와 이어짐).

## 스크립트 문제

없음. (export 정상 종료, 로그에 `SCRIPT ERROR` 없음. 읽지 않은 틈 `IopCompleteRequest +0x146~+0x169` 는 SEH 착지점이라 원본 바이트를 objdump 로 따로 봤다 — 스크립트 문제가 아니라 설명대로 나온 것.)

## 판정

**분석성공** — 요청서 R6 의 1~4 모두 `.asm` 줄로 답했다. 단 "PENDING 없음 길에서 완료가 인라인인지 큐잉된 APC 인지", "AFD 가 그 IOCTL 에서 PENDING 을 실제로 내는지", "포트 연결 핸들의 파일 객체 목록 갈래" 는 정적으로 못 정해 동적·다음 요청으로 넘겼다.
