# 2026-11-22 (작성일, 정적 분석 2026-11-21) run127 — 파일을 완료 포트에 묶기 (Q220, 정적만): `NtSetInformationFile` 종류 `0x1e` (포트 묶기) · `0x29` (알림 방식) 는 **드라이버 (afd) 로 안 내려가고** I/O 관리자가 같은 스레드에서 `IopCompleteRequest` 로 스스로 끝낸다. `0x1e` 는 이미 묶였거나 동기 핸들이면 `0xC000000D`, 성공이면 파일 객체 `+0xb0` 에 `{Port, Key, UsageCount}` 덩어리를 달고 포트 참조를 쥔다. `0x29` 는 비트 `1/2/4` 를 파일 객체 `Flags` 의 `0x2000000/0x4000000/0x8000000` 으로 **켜기만** 한다. 오류는 IOSB 를 안 쓴다. 묶인 파일의 I/O 가 **PENDING 없이 오류**로 끝나면 완료 패킷은 없다

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("`NtSetInformationFile` 로 소켓을 완료 포트에 묶기 — 종류 `0x1e` · `0x29`", 튜닝 `290_tune09.md N2`) 이다. test.exe 초기화 지도에는 `NtSetInformationFile` 이 없고 (flow.md 에 이름 0건 — 불리지 않는다는 뜻이 아니라 본 적이 없다), `sample/*.c` 에 완료 포트 · 겹침 I/O · 소켓 API 가 없다 `[소스]` (`grep -il 'completion\|overlapped\|socket'` 0건). 완료 함수 (`IopCompleteRequest`) 는 커널 공용 길이지만 run126 과 같은 이유로 그림엔 안 넣는다. **그림은 안 바꿨다.** flow.md 는 기준 · 3장 끝 새 절 하나 · 4장 (run126 줄 일부 닫힘 표시 + run127 줄) · 5장만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q220** (사용자 결정 2026-11-21 "이것들부터 먼저", 2026-11-22 "문서로 만들어서 커밋"). 이번 run 으로 닫는 몫:
> - 요청서 질문 1 ~ 3 — 결과서 **분석성공**. 정적으로 못 정한 넷 (포트 핸들 풀기 실패의 조건별 status · 정렬 / 나쁜 포인터 예외의 착지 · `ConnectEx` 오류가 PENDING 없이 오는지 · 비트 `0x8000000` 의 헤더 이름) 은 `[공백]` / `[추측]` 으로 남김.
> - run126 "아직 확인 필요" 정적 ⑩ 의 뒷몫 (빠른 길 포트 갈래 `IopXxxControlFile +0xbbc` 뒤) 과 run126 10절 "동기 핸들에 포트가 붙는지 `[공백]`" 의 `NtSetInformationFile` 몫 — 아래 "정정" 절.

## 요약

- **두 종류 다 드라이버를 안 부른다** `[정적]`. `NtSetInformationFile` 이 IRP 를 만들긴 하지만 `+0x867 IopCallDriverReference` 줄로 안 가고 (`+0x84b` 로만 들어오는 줄), 갈래 끝에서 `+0x86f` 로 뛰어 `+0x8de IopCompleteRequest` 로 스스로 끝낸다. → 소켓이든 파일이든 afd 는 이 두 호출을 모른다.
- **`0x1e` (`FileCompletionInformation`, 입력 16 B = 포트 핸들 `+0x0` · 키 `+0x8`)** `[정적]`:
  - 검사 차례: (공통) 종류 번호 · 길이 (< 16 → `0xC0000004`) · IOSB · 정렬 · 파일 핸들 풀기 → **이미 묶임 (`FileObject+0xb0` ≠ 0) `0xC000000D`** → **동기 핸들 (`Flags & 2`) `0xC000000D`** → 포트 핸들 풀기 (접근 `2`, 완료 포트 종류, 실패 status 그대로) → 0x18 B 풀 (실패 `0xC000009A`) → 락 잡고 다시 봄 (경주면 `0xC0000048`).
  - 성공: `Flags &= ~0x400`, `FileObject+0xb0` = `_IO_COMPLETION_CONTEXT*` (`Port +0` · `Key +8` · `UsageCount +0x10`), IOSB `{Status 0, Information 0}`.
  - **포트 참조를 놓지 않는다** — 파일 객체가 지워질 때 (`IopDeleteFile`) 놓는다. 핸들을 닫을 때가 아니다.
  - 묶은 바로 그 IRP 는 패킷을 안 보낸다 (`ApcContext` = 0).
- **`0x29` (`FileIoCompletionNotificationInformation`, 입력 4 B)** `[정적]`: 동기 핸들만 `0xC000000D`. 비트 `0x1 / 0x2 / 0x4` → `Flags` 의 `0x2000000 / 0x4000000 / 0x8000000` 을 **켠다 (끄는 줄 없음)**. 다른 비트는 검사 없이 무시 (status 0). **묶기 전에 불러도 된다** (이 갈래가 `+0xb0` 을 안 읽음).
- **오류는 IOSB 를 안 쓴다** `[정적]`: 두 종류의 오류 status 는 IRP 에만 적히고, `IopCompleteRequest` 가 "오류 + PENDING 아님" 이라 IOSB 를 안 건드린다 (run126 10절과 같은 줄).
- **묶인 파일의 I/O 가 끝날 때 패킷 조건** `[정적]` (run126 10절을 잇는 것):
  - 바로 오류 → **없음**. 바로 성공 + `0x2000000` 켜짐 → **없음**. 바로 성공 + 꺼짐 + `ApcContext` ≠ 0 → 있음. 바로 경고 → 있음 (비트와 상관없이). PENDING 뒤 완료 → 있음 (성공이든 오류든, 비트와 상관없이). `ApcContext` = 0 → 늘 없음.
  - 빠른 길 (`IopXxxControlFile`) 도 **같은 규칙으로 따로** 넣는다 (`+0xe82` 비트 · `+0xe9c` 오류 검사 → `+0x10df` 넣기).
- **"`ConnectEx` 를 바로 오류로 돌려줄 때 패킷을 안 보내는 게 맞나" → 예** — 단 afd 가 PENDING 표시 없이 오류로 끝낼 때. afd 가 먼저 `0x103` 을 돌려주고 나중에 오류로 끝내면 패킷이 간다. afd 가 어느 쪽인지는 `[공백]` (run124 동적 ⑤ 와 같은 질문).

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든다섯째 바퀴. plan **Q220**. **짐 지도 바퀴 아님.**
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (기준값과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`). 다시 analyze 안 함, `export` 만.
- 버전 대조: `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` → `_calls.tsv` 와 **일치**.
- 같은 시간에 다른 decompiler 가 sast/126 (winhttp · IPHLPAPI · dnsapi · webio · nsi) 을 돌았다 — 프로젝트가 달라 부딪히지 않음.
- `_FILE_OBJECT` · `_IRP` · `_IO_COMPLETION_CONTEXT` 칸은 PDB 타입 이름 · 오프셋. **비트 이름** (`FO_SYNCHRONOUS_IO` · `FO_SKIP_COMPLETION_PORT` · `FO_SKIP_SET_EVENT` · `FO_QUEUE_IRP_TO_THREAD` 등) 은 PDB 에 없어 공개 헤더 값으로 맞춘 `[추측: 이름]`. 값과 줄은 `[정적]`.

## 목적

- PE_emulator 에서 Go 런타임이 새 소켓마다 `NtSetInformationFile(소켓, …, 0x1e)` · `(…, 0x29)` 를 부르는데, 에뮬레이터가 두 종류를 몰라 "성공인데 아무것도 안 함" 으로 대답한다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님). 요청서 `_workspace/sast/125_file-completion.md`:
  1. `0x1e` — 검사 차례 · status · 파일 객체에 쓰는 칸 · 포트 참조 · IOSB · 드라이버로 내려가나.
  2. `0x29` — 받는 비트 · 옮기는 칸 · 묶기 전에 되나 · 끌 수 있나.
  3. 묶인 파일의 I/O 가 끝날 때 패킷이 가는 조건 — "`ConnectEx` 바로 오류면 패킷 없음" 이 맞나.

## 방법

- 정적 sast/125 (ntoskrnl, 이미 analyze 된 프로젝트):
  - `^NtSetInformationFile$` depth 1 → 47개 (`ms/disassemble/ntoskrnl/NtSetInformationFile.asm` 1017줄). 로그 `ms/ghidra/ntoskrnl.export.sast125a.stdout`.
  - 요청서에서 넓힘 (depth 0, 작은 것): `IopDeleteFile` · `ObpReferenceObjectByHandleWithTag` (sast125b), `IopAllocateIrpPrivate` (sast125c) — "포트 참조를 누가 놓나", "포트 핸들 풀기 실패 status", "이 IRP 의 `ApcContext` 가 정말 0 인가" 를 추측 없이 닫으려고.
  - 읽기만: `IopCompleteRequest.asm` · `IopXxxControlFile.asm` (run126 때 뽑은 것). `IopSynchronousServiceTail` 은 sast/124 줄을 가리킴.
  - **정보 종류 표 셋**을 PE 파일 `PAGE` 구역 바이트로 직접 읽음: 길이 `IopSetOperationLength` (rva `0xb4af00`, byte × `0x54`) · 정렬 `IopQuerySetAlignmentRequirement` (`0xb4ae40`, byte × `0x54`) · 접근 `IopSetOperationAccess` (`0xb4bf40`, dword × `0x54`). 결과서: PE_emulator 쪽 같은 표 84칸과 전부 같음 (두 곳에서 따로 읽은 값이 맞음).
  - SEH 착지 틈 (`NtSetInformationFile +0x157` 등 5곳) 은 원본 바이트 `objdump` 로.
- 결과서: `_workspace/sast/125_file-completion_result.md` (**분석성공**, 스크립트 문제 없음).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` 로 call · ret 짝 — `NtSetInformationFile` · `IopCompleteRequest` · `IopXxxControlFile` · `IopDeleteFile`.
  - `.asm` 줄: `NtSetInformationFile +0x815 ~ +0x84b` (비교 사슬) · `+0xa53 ~ +0xb0a` (`0x1e` 몸) · `+0xceb ~ +0xd1e` · `+0x104c ~ +0x1050` (`0x29` 몸) · `+0xbe8`, `IopCompleteRequest +0xbf` · `+0x35b ~ +0x378` · `+0x426 ~ +0x445` · `+0x5ac ~ +0x5c9`, `IopXxxControlFile +0xbb4 ~ +0xbc7` · `+0xe82 ~ +0xec7` · `+0x10a9 ~ +0x110e`. 결과서와 다른 곳 → 아래 "flow-writer 대조".
- 앞 바퀴 짝: run126 (sast/124 — IRP 완료 때 IOSB · 이벤트 · 포트), run125 (sast/122 — 완료 포트 `_KQUEUE`), run124 (sast/121 — `ConnectEx` = `0x120c7`, `ApcContext` 를 정하는 줄), run93 (완료 포트 꺼내기).

## 결과

비유 (run124 · run126 의 우체국 사서함 이어서):

- `0x1e` = 창구에 "이 사서함 (파일) 소식은 저 우편함 (완료 포트) 으로 보내 주세요" 하고 **우편함 열쇠 (포트 핸들) 와 꼬리표 (키)** 를 맡기는 것. 사무실 (I/O 관리자) 이 서랍 (`+0xb0`) 에 쪽지를 넣어 두고 끝낸다 — 창구 직원 (afd) 은 부르지도 않는다.
- 이미 쪽지가 있거나 "늘 기다리는 손님" (동기 핸들) 이면 "안 돼요" (`0xC000000D`).
- `0x29` = "잘 끝난 건 우편함에 안 넣어도 돼요" 같은 **스위치를 켜는 것**. 한 번 켠 스위치는 끄는 길이 없다.
- 일이 끝날 때: 바로 "실패" 라고 돌려준 일은 우편함에 아무것도 안 넣는다. "접수했어요 (PENDING)" 라고 한 뒤 나중에 실패하면 넣는다 `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtSetInformationFile` | `+0x19a` | `+0x19f` | `ObReferenceObjectByHandle` (파일 핸들, 접근 0) | [정적] |
| `nt!NtSetInformationFile` | `+0x271` | `+0x276` | `KeAbPreAcquire` (동기 핸들 — 파일 객체 락) | [정적] |
| `nt!NtSetInformationFile` | `+0x405` | `+0x40a` | `PsIsProcessAppContainer` (→ `0xC0000910` 갈래) | [정적] |
| `nt!NtSetInformationFile` | `+0x635` | `+0x63a` | `nt!IopResetEvent` | [정적] |
| `nt!NtSetInformationFile` | `+0x657` | `+0x65c` | `nt!IopAllocateIrpExReturn` (실패 `0xC000009A`) | [정적] |
| `nt!NtSetInformationFile` | `+0x70b` · `+0x72a` | `+0x710` · `+0x72f` | `ExAllocatePool2` · 바이트 복사 (`_calls.tsv` 이름 `alias:RtlCopyMemory`) — 입력을 풀에 복사 | [정적] |
| `nt!NtSetInformationFile` | `+0x7b8` | `+0x7bd` | `nt!IopQueueThreadIrp` | [정적] |
| `nt!NtSetInformationFile` | `+0x867` | `+0x86c` | `nt!IopCallDriverReference` — **`0x1e` · `0x29` 는 안 지남** | [정적] |
| `nt!NtSetInformationFile` | `+0x8de` | `+0x8e3` | `nt!IopCompleteRequest` (드라이버 안 부른 종류의 끝맺음) | [정적] |
| `nt!NtSetInformationFile` | `+0x909` | `+0x90e` | `nt!IopReleaseFileObjectLock` | [정적] |
| `nt!NtSetInformationFile` | `+0xa95` | `+0xa9a` | `ObReferenceObjectByHandle` (`0x1e` 포트 핸들, 접근 2, `IoCompletionObjectType`) | [정적] |
| `nt!NtSetInformationFile` | `+0xaaf` | `+0xab4` | `ExAllocatePool2` (`0x18` B, 태그 `IoCc`) | [정적] |
| `nt!NtSetInformationFile` | `+0xac7` | `+0xacc` | `KeAcquireSpinLockRaiseToDpc` (`FileObject+0xb8`) | [정적] |
| `nt!NtSetInformationFile` | `+0xb05` | `+0xb0a` | `KeReleaseSpinLock` (성공 갈래) | [정적] |
| `nt!NtSetInformationFile` | `+0xd8c` | `+0xd91` | `ObfDereferenceObjectWithTag` (풀 할당 실패 — 포트 참조 되돌림) | [정적] |
| `nt!NtSetInformationFile` | `+0x1023` · `+0x102d` · `+0x103c` | `+0x1028` · `+0x1032` · `+0x1041` | `KeReleaseSpinLock` · `ExFreePoolWithTag` · `ObfDereferenceObjectWithTag` (경주 — `0xC0000048`) | [정적] |
| `nt!NtSetInformationFile` | `+0x33aefe` (조각 `FUN_1406e61bb +0x13`) | 조각 `+0x18` | `nt!IopReplaceCompletionPort` (종류 `0x3d`) | [정적] |
| `nt!NtSetInformationFile` | `+0x33af3e` | `+0x33af43` | `nt!IopSetFileObjectIosbRange` (종류 `0x2a`) | [정적] |
| `nt!IopCompleteRequest` | `+0x109` · `+0x29e` | `+0x10e` · `+0x2a3` | `ObfReferenceObjectWithTag` · `ObfDereferenceObjectWithTag` (완료마다 잠깐 잡는 포트 참조) | [정적] |
| `nt!IopCompleteRequest` | `+0x52c` | `+0x531` | `nt!IopIncrementCompletionContextUsageCountAndReadData` (포트 · 키 읽기) | [정적] |
| `nt!IopCompleteRequest` | `+0x4ad` | `+0x4b2` | `nt!IopDecrementCompletionContextUsageCount` | [정적] |
| `nt!IopCompleteRequest` | `+0x3b4` | `+0x3b9` | `KeInsertQueueEx` (완료 패킷) | [정적] |
| `nt!IopXxxControlFile` | `+0xec7` | `+0xecc` | `nt!IopIncrementCompletionContextUsageCountAndReadData` (빠른 길) | [정적] |
| `nt!IopXxxControlFile` | `+0x10df` | `+0x10e4` | 완료 포트에 패킷 넣기 (`_calls.tsv` 이름 `alias:IoSetIoCompletionEx`. 같은 주소 이름 2개, 후보: `IoSetIoCompletionEx` · `IoSetIoCompletionEx2`) | [정적] |
| `nt!IopDeleteFile` | `+0x285` | `+0x28a` | `KeBugCheckEx` (`UsageCount` ≠ 0) | [정적] |
| `nt!IopDeleteFile` | `+0x29d` | `+0x2a2` | `ObfDereferenceObject` (`ctx.Port` — **묶을 때 잡은 참조를 여기서 놓음**) | [정적] |
| `nt!IopDeleteFile` | `+0x2ab` | `+0x2b0` | `ExFreePoolWithTag` (ctx) | [정적] |

### 1. 공통 앞부분 — `0x1e` · `0x29` 가 같이 지나는 검사 (`NtSetInformationFile`, rva `0x3ab2d0`, 4323B) [정적]

| 차례 | 조건 | status | 줄 |
|---|---|---|---|
| 1 | 호출자 모드 (`Thread+0x232`) 가 커널이면 2 ~ 5 건너뜀 (길이 · 정렬 검사 없음) | — | `+0x69` · `+0x76 / +0x79 → +0x4ce` |
| 2 | 종류 번호 ≥ `0x54` 또는 길이표 0 | `0xC0000003` | `+0x87 ~ +0xa2 → +0xa2f` |
| 3 | `Length` < 길이표 (`0x1e` 16 · `0x29` 4) | `0xC0000004` | `+0xa8 / +0xab → +0x96b` |
| 4 | IOSB 첫 4 B 읽고 되쓰기 (사용자 주소 끝 넘으면 예외) | 예외 → 5 의 착지 | `+0xb1 ~ +0xc7` |
| 5 | 입력 정렬 (둘 다 4) · 버퍼 끝 검사 | 예외 코드가 반환값이 됨 `[추측: 착지 `+0x157` = `jmp +0x3c7` → 함수 끝, EAX 그대로]` | `+0x125 ~ +0x146` |
| 6 | 파일 핸들에 요구하는 접근 (표) — **둘 다 0** | — | `+0x15c` (`IopSetOperationAccess`) |
| 7 | 파일 핸들 풀기 | 실패 status 그대로 (`0xC0000008` · `0xC0000024` 등, 조건별 매핑은 안 따라감) | `+0x19a`, 실패 `+0x1ae ~ +0x1be → +0x3c4` |
| 8 | AppContainer + 파일 객체 확장 `+0xd0` 첫 dword 비트 4 | `0xC0000910` | `+0x3db ~ +0x417` (sast/124 의 같은 칸) |
| 9 | 동기 핸들 (`Flags & 2`) 이면 파일 객체 락 · 사용자 IOSB 를 IRP 에 바로. 비동기면 스택 임시 이벤트 · 임시 IOSB | — | `+0x23d / +0x241` · `+0x271` · `+0x280` · `+0x5ff ~ +0x62d` · `+0x975 ~ +0x981` |
| 10 | IRP 만들기 → 입력을 풀 (태그 `IoSB`) 에 복사 → `Irp.Flags \|= 0x830` → 스레드 IRP 목록에 걸기. **IRP 는 0 부터** (`IopAllocateIrpPrivate +0x192` memset) → `ApcRoutine` (`+0x58`) · `ApcContext` (`+0x60`) · `PendingReturned` (`+0x41`) = 0 | IRP 실패 `0xC000009A` | `+0x635 ~ +0x7b8`, `+0x6d6` (`ApcRoutine` 0) |
| 11 | 종류별 갈래 — **점프표가 아니라 비교 사슬** | | `+0x7d5 ~ +0x845` |

### 2. 종류 `0x1e` (`FileCompletionInformation`) [정적]

갈래 시작 `+0x815 CMP EDI,0x1e / +0x818 JZ → +0xa4a`. 입력 복사본 = `Irp+0x18` (`+0x0` 포트 핸들 · `+0x8` 키), `RBX` = 파일 객체.

| 차례 | 조건 | status | 쓰는 칸 | 줄 |
|---|---|---|---|---|
| 1 | **이미 묶임** — `FileObject+0xb0` (`CompletionContext`) ≠ 0 | `0xC000000D` | IRP `+0x30` = status · `+0x38` = 0 | `+0xa53 / +0xa5b → +0xbe8 → +0xb0a` |
| 2 | **동기 핸들** — `Flags & 0x2` (`FO_SYNCHRONOUS_IO` `[추측: 이름]`) | `0xC000000D` | 같음 | `+0xa61 / +0xa66 → +0xbe8` |
| 3 | 포트 핸들 풀기 — 접근 **2** (`IO_COMPLETION_MODIFY_STATE` `[추측: 이름]`), 종류 `IoCompletionObjectType` | 실패 status 그대로 (`0xC0000008` · `0xC0000024` · `0xC0000022` 가 `ObpReferenceObjectByHandleWithTag` 에 상수로 있음, 조건별은 안 따라감) | 성공이면 포트 참조 +1 | `+0xa6c ~ +0xa95`, 실패 `+0xa9d / +0xa9f → +0xb0a` |
| 4 | `0x18` B 풀 (비페이지, 태그 `IoCc`) | 실패 → 포트 참조 되돌림 · `0xC000009A` | — | `+0xaa1 ~ +0xaba → +0xd82 ~ +0xd9f` |
| 5 | 스핀락 `FileObject+0xb8` 잡고 `+0xb0` 다시 봄 (다른 스레드와 경주) | 아니면 풀 해제 · 참조 되돌림 · **`0xC0000048`** `[추측: 이름 STATUS_PORT_ALREADY_SET]`. 순차 호출이면 1 에서 걸러져 안 나옴 | — | `+0xac7` · `+0xad3 / +0xadb → +0x1020 ~ +0x1041` |
| 6 | **성공** | `0` | `Flags &= ~0x400` (`[추측: FO_QUEUE_IRP_TO_THREAD]`) · `ctx.Port` = 포트 객체 · `ctx.Key` = 입력 `+0x8` · `FileObject+0xb0` = ctx | `+0xae1` · `+0xaed` · `+0xaf4` · `+0xaf8` · `+0xaff` · 락 놓기 `+0xb05` |
| 7 | 끝맺음 — 드라이버 없이 `+0x86f` → (비동기면 IRP 이벤트 칸 0 · `UserIosb` = 사용자 IOSB) → `IopCompleteRequest` | 반환 = 위 status | **IOSB 는 성공일 때만 `{0, 0}`** (`IopCompleteRequest +0x125 ~ +0x142`, Information 먼저). 오류면 안 씀 | `+0xb0a / +0xb0e` · `+0x874` · `+0x88e / +0x89a` · `+0x8de`, 반환 `+0x940 ~ +0x947 → +0x3c4 → +0x3d9` |

- `_IO_COMPLETION_CONTEXT` (PDB): `Port +0x0` · `Key +0x8` · `UsageCount +0x10`, 크기 `0x18`.
- **포트 참조**: 성공 줄에 `Dereference` 가 없다 → 참조를 ctx 에 쥔 채 둔다. 놓는 곳은 `IopDeleteFile +0x29d` (`UsageCount` ≠ 0 이면 `+0x285 KeBugCheckEx(0x18…)`), 이어 `+0x2ab` 에서 ctx 해제. **핸들을 닫을 때가 아니라 파일 객체가 사라질 때.** 완료마다 잠깐 더 잡았다 놓는 참조는 `IopCompleteRequest +0x109` / `+0x29e`, `UsageCount` 는 `+0x52c` 증가 · `+0x4ad` 감소.
- **이 IRP 자체의 패킷 → 없음**: 방금 묶어서 `IopCompleteRequest` 가 포트 칸을 읽긴 하지만 (`+0xce` · `+0x52c`), `ApcRoutine` = 0 · `ApcContext` = 0 이라 `+0x35b / +0x360 JZ → +0x244` 로 패킷 없이 끝난다.

### 3. 종류 `0x29` (`FileIoCompletionNotificationInformation`) [정적]

갈래 시작 `+0x827 CMP EDI,0x29 / +0x82a JZ → +0xce2`. 입력 복사본 = dword 하나.

| 차례 | 조건 | status | 쓰는 칸 | 줄 |
|---|---|---|---|---|
| 1 | 동기 핸들 (`Flags & 2`) | `0xC000000D` | 비트 안 바꿈 | `+0xceb ~ +0xcf1 → +0xe8f ~ +0xe9d` |
| 2 | 입력 비트 `0x1` (`FILE_SKIP_COMPLETION_PORT_ON_SUCCESS` `[추측: 이름]`) | — | `Flags \|= 0x2000000` (비트 25, `FO_SKIP_COMPLETION_PORT` `[추측: 이름]`) | `+0xcfa ~ +0xd04` (`BTS ECX,0x19`) |
| 3 | 비트 `0x2` (`FILE_SKIP_SET_EVENT_ON_HANDLE` `[추측: 이름]`) | — | `Flags \|= 0x4000000` (비트 26, `FO_SKIP_SET_EVENT` `[추측: 이름]`) | `+0xd07 ~ +0xd11` (`BTS ECX,0x1a`) |
| 4 | 비트 `0x4` (`FILE_SKIP_SET_USER_EVENT_ON_FAST_IO` `[추측: 이름]`) | — | `Flags \|= 0x8000000` (비트 27, 헤더 이름 `[공백]`) | `+0xd14 / +0xd18 → +0x104c ~ +0x1050` (`BTS ECX,0x1b`) |
| 5 | 다른 비트 (`0x8` 이상) | **검사 없음 — 조용히 무시, status 0** | — | `+0xcfa ~ +0xd22` 에 마스크 비교 없음 |
| 6 | 끝맺음 — `0x1e` 의 7 과 같은 길 | `0` | IOSB `{0, 0}` | `+0xd1e ~ +0xd26` → `+0x874 ~ +0x8de` |

- **묶기 전에 불러도 된다** — 이 갈래에 `+0xb0` 을 읽는 줄이 없다.
- **끌 수 없다** — 세 곳 다 `BTS` (켜기) 뿐, `BTR` · `AND` 없음. 입력 비트가 0 이면 그냥 둔다.
- 비트를 읽는 곳:
  - `0x2000000`: `IopCompleteRequest +0x366` (IRP 길 패킷 건너뛰기) · `IopXxxControlFile +0xe82` (빠른 길).
  - `0x4000000`: `IopCompleteRequest +0x2a5` (파일 객체 이벤트 안 세움) · `IopXxxControlFile +0x721` (호출 앞 파일 객체 이벤트 안 비움).
  - `0x8000000`: `IopXxxControlFile +0xbc7` (빠른 길 사용자 이벤트 안 세움. 포트가 있고 경고면 세움 `+0x1651 ~ +0x165b`).

### 4. 드라이버를 안 부르는 종류 [정적]

`+0x7d5 ~ +0x845` 비교 사슬. `IopCallDriverReference` 는 `+0x867` 한 곳이고 `+0x84b` 로만 들어온다.

- **안 부름**: `0x10` (`+0x7d5`) · `0x1e` (`+0x815`) · `0x3d` (`+0x81e`, `IopReplaceCompletionPort` — 안 묶였으면 `0xC000000D`) · `0x29` (`+0x827`) · `0x2a` (`+0x830`, `IopSetFileObjectIosbRange`) · `0x2b` (`+0x839`) · `0x45` (`+0x842`). 앞에서 따로: `0xe` (동기 핸들만, IRP 앞 `+0x2ba ~ +0x38d`) · `0x24` (`+0x63a → +0xd50`).
- **부름 (이름만)**: `0xa / 0xb / 0x1f / 0x41 / 0x48` (`+0x7de` 비트 마스크), `0x28`, `0xd / 0x40`, 그 밖 모두 `+0x84b`.

### 5. 묶인 파일의 I/O 가 끝날 때 — 완료 패킷 조건 [정적]

run126 10절 (`IopCompleteRequest`) 표의 "완료 포트" 칸을 펼친 것. 포트에 묶인 비동기 파일, `NtDeviceIoControlFile` IRP 길. 줄은 `IopCompleteRequest.asm`.

| 상황 | 패킷 | 줄 |
|---|---|---|
| **바로 오류** (`Irp+0x30` `0xC…`, `PendingReturned` 0) | **없음** (포트 칸도 안 읽음, IOSB · 이벤트도 안 건드림) | `+0xb3 ~ +0xbf → +0x426 / +0x42a → +0x445` · `+0x10e ~ +0x11a → +0x4b7` · `+0x180 ~ +0x198` |
| 바로 성공 · 정보 + `0x2000000` **켜짐** | **없음** | `+0x366 → +0x5ac / +0x5b0` (PENDING 0) · `+0x5b6 ~ +0x5be` (경고 아님) · `+0x5c9 → +0x376 / +0x378 → +0x244` |
| 바로 성공 · 정보 + 꺼짐 | `ApcRoutine` 0 · `ApcContext` ≠ 0 이면 **있음** (키 = `ctx.Key`) | `+0x2d0 → +0x23b` · `+0x35b / +0x360` · `+0x37e ~ +0x383` · `+0x3b4` |
| 바로 경고 (`0x8…`) | **있음** (비트 켜져 있어도) | `+0x5b6 ~ +0x5c3 → +0x373` |
| `ApcContext` = 0 | **없음** (어느 경우든) | `+0x35b / +0x360 JZ → +0x244` |
| `ApcRoutine` ≠ 0 | 패킷 대신 사용자 APC | `+0x2c9 ~ +0x351` (포트 핸들엔 `ApcRoutine` 을 못 주게 막는 줄 — sast/124 `IopXxxControlFile +0xd3b ~ +0xd55`) |
| **PENDING 뒤 완료** (`PendingReturned` 1), 성공 · 오류 다 | **있음** (`ApcContext` ≠ 0 이면, 비트 무시) | `+0x426 / +0x42a` 안 탐 · `+0x5ac JNZ → +0x373` |

**빠른 길 (`IopXxxControlFile`)** — 따로 넣는다, 같은 규칙:

- 빠른 길 TRUE (`+0x70f → +0xb16`) 뒤 `+0xbb4 / +0xbbc`: `FileObject+0xb0` 이 있으면 `+0xe82` 로.
- `+0xe82 ~ +0xe96`: `0x2000000` 켜짐 → 경고일 때만 계속, 아니면 `+0xbc2` (사용자 이벤트 단계) 로 건너뜀. 꺼짐 → `+0xe9c ~ +0xea9` 오류면 건너뜀. 아니면 `+0xec7` 로 포트 · 키 읽기.
- `+0xc10 / +0xc13 → +0x10a9`: `ApcContext` (`[RSP+0x288]`) 가 0 이면 `+0x10b1 / +0x10b4` 에서 패킷 없이. 아니면 `+0x10df` 넣기, 실패면 status `0xC000009A`, 경고면 `0x103` 으로 바꿈 (`+0x10e4 ~ +0x1101`) → `+0x110e → +0xc19`.
- → 빠른 길은 오류여도 IOSB · 이벤트는 쓰지만 (run126 11절) **패킷은 오류면 안 넣는다**.

### 6. 답 — "`ConnectEx` 를 바로 오류로 돌려줄 때 패킷을 안 보내는 게 맞나" [정적 + 공백]

- **예** — afd 가 PENDING 표시 없이 오류 status 로 끝낼 때 (syscall 반환이 `0xC000023C` 같은 오류, `0x103` 아님). IRP 길이면 `IopCompleteRequest +0xbf → +0x426 → +0x445`, 빠른 길이면 `IopXxxControlFile +0xe9c ~ +0xea9` — 포트 · IOSB · 이벤트 다 안 건드린다 (빠른 길은 IOSB · 이벤트는 씀).
- 반례: afd 가 `IoMarkIrpPending` 뒤 `0x103` 을 돌려주고 나중에 오류로 끝내면 `ApcContext` ≠ 0 일 때 패킷이 간다. `ConnectEx` 의 `ApcContext` 는 `hEvent` 맨 아래 비트가 0 이면 `OVERLAPPED*` (run124 8절). afd `AfdSuperConnect` (`0x120c7`) 가 어느 쪽인지는 정적으로 못 정함 `[공백]` — run124 동적 ⑤ 와 같은 질문.

### 호출 관계

```
(PE_emulator 요청 — test.exe 초기화 밖. Go 런타임이 새 소켓마다)
nt!NtSetInformationFile                                  공통 1 ~ 10 (길이 · IOSB · 정렬 · 파일 핸들 · IRP)
 ├─ +0x19f  ObReferenceObjectByHandle                     파일 핸들, 접근 0
 ├─ +0x65c  IopAllocateIrpExReturn                        IRP 0 부터 (ApcContext 0)
 │  +0x7d5 ~ +0x845  비교 사슬
 │    ├─ 0x1e (+0x815 → +0xa4a)
 │    │    +0xa53 이미 묶임 / +0xa61 동기 핸들 → 0xC000000D
 │    │    ├─ +0xa9a  ObReferenceObjectByHandle            포트, 접근 2 (참조 +1, 안 놓음)
 │    │    ├─ +0xab4  ExAllocatePool2                       ctx 0x18 B
 │    │    ├─ +0xacc  KeAcquireSpinLockRaiseToDpc           +0xb0 다시 봄 (경주 → 0xC0000048)
 │    │    │  +0xae1 Flags &= ~0x400 · +0xaf8 FileObject+0xb0 = ctx {Port, Key, UsageCount}
 │    │    └─ +0xb0a  KeReleaseSpinLock
 │    ├─ 0x29 (+0x827 → +0xce2)
 │    │    +0xceb 동기 핸들 → 0xC000000D
 │    │    +0xcfa ~ +0xd18 · +0x104c  Flags |= 0x2000000 / 0x4000000 / 0x8000000 (켜기만)
 │    └─ 그 밖 → +0x84b → +0x86c  IopCallDriverReference   (0x1e · 0x29 는 여기 안 옴)
 ├─ +0x8e3  IopCompleteRequest                            성공이면 IOSB {0,0}, 오류면 안 씀, 패킷 없음 (ApcContext 0)
 └─ +0x90e  IopReleaseFileObjectLock                       (동기 핸들)

(묶인 파일의 다음 I/O 가 끝날 때)
nt!IopCompleteRequest
 │  +0xbf 오류 + PENDING 0 → +0x445 (포트 안 읽음)
 ├─ +0x531  IopIncrementCompletionContextUsageCountAndReadData   포트 · 키
 │  +0x366 0x2000000 · +0x5ac PENDING · +0x5b6 경고 · +0x35b ApcContext
 ├─ +0x3b9  KeInsertQueueEx                                     패킷
 └─ +0x4b2  IopDecrementCompletionContextUsageCount
nt!IopXxxControlFile (빠른 길)
 │  +0xbbc → +0xe82 0x2000000 · +0xe9c 오류
 ├─ +0xecc  IopIncrementCompletionContextUsageCountAndReadData
 └─ +0x10e4 [패킷 넣기 — 별칭 IoSetIoCompletionEx / IoSetIoCompletionEx2]   (ApcContext ≠ 0 일 때)

(파일 객체가 사라질 때)
nt!IopDeleteFile
 ├─ +0x28a  KeBugCheckEx                                   (UsageCount ≠ 0)
 ├─ +0x2a2  ObfDereferenceObject                           ctx.Port — 묶을 때 잡은 참조
 └─ +0x2b0  ExFreePoolWithTag                              ctx
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 들여쓴 `+0x..` 는 그 함수 안 줄.
- `IopCompleteRequest` 안 줄은 한 함수의 갈래라 한 줄에 몰았다.

## 확인된 것

- `[정적]` `0x1e` · `0x29` 는 드라이버를 안 부른다 — `IopCallDriverReference` 는 `+0x867` 한 곳, `+0x84b` 로만 들어오고 두 갈래는 `+0x86f` 로 간다.
- `[정적]` 길이 · 정렬 · 접근 표 값: `0x1e` = 16 / 4 / 0, `0x29` = 4 / 4 / 0 (PE 파일 바이트, 결과서).
- `[정적]` `0x1e`: 이미 묶임 → 동기 핸들 → 포트 핸들 (접근 2) → 풀 → 경주 차례. 둘 다 `0xC000000D`, 경주만 `0xC0000048`. 성공이면 `FileObject+0xb0` = ctx, `Flags &= ~0x400`.
- `[정적]` `0x1e` 성공은 포트 참조를 쥐고, `IopDeleteFile +0x29d` 가 놓는다.
- `[정적]` `0x29`: 동기 핸들만 거절, 비트 `1/2/4` → `Flags` 비트 25 / 26 / 27 켜기만, 다른 비트 무시, 묶기 전에 가능.
- `[정적]` 두 종류의 오류는 IOSB 를 안 쓴다 (`IopCompleteRequest` 오류 + PENDING 0 갈래). 성공만 `{0, 0}`.
- `[정적]` 묶인 파일의 IRP 길 완료: 바로 오류 · 바로 성공 + 비트 25 · `ApcContext` 0 → 패킷 없음. 바로 경고 · PENDING 뒤 완료 → 비트와 상관없이 패킷.
- `[정적]` 빠른 길도 같은 규칙으로 `IopXxxControlFile +0x10df` 에서 따로 넣는다.

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- 결과서 "`IopReplaceCompletionPort` 를 `+0x33aefe` 에서 부름" — `_calls.tsv` 는 조각 표기 `frag:FUN_1406e61bb+0x13` (ret `+0x18`). `.asm` 에 `(= NtSetInformationFile +0x33aefe)` 로 같은 자리. 이 문서는 둘 다 적었다.
- 결과서 "`+0x10df IoSetIoCompletionEx` (PDB 별칭 `IoSetIoCompletionEx2`)" — `_calls.tsv` 이름은 `alias:IoSetIoCompletionEx`, `pdb_aliases.py --lookup ntoskrnl.exe 0x308690` = 2개 (`IoSetIoCompletionEx2` · `IoSetIoCompletionEx`). 이 문서는 "패킷 넣기" 로 적고 후보 둘을 붙였다.
- 결과서 빠른 길 "`0x2000000` 켜짐 → 경고일 때만 계속, 아니면 건너뜀" — 건너뛰는 곳을 flow-writer 가 `.asm` 으로 맞춤: `+0xe96 JNZ` · `+0xea9 JZ` 목적지 둘 다 `+0xbc2` (사용자 이벤트 단계). → 건너뛰는 건 **패킷만**이다.
- 결과서가 `_calls.tsv` 이름 `alias:RtlCopyMemory` (`+0x72a`) 를 "입력을 풀에 복사" 로 적은 것 — 이 문서도 하는 일로 적었다.

## 정정

- **run126** (`runs/03_sprint/run126-afd-addrlist.md`) — 정정 아님, **일부 닫힘**. 맨 위에 한 줄:
  - 11절 "`+0xbb4 ~ +0xbbc`: 파일 객체 `+0xb0` 이 있으면 다른 갈래로 (안 읽음)" → 읽었다 (위 5절 빠른 길). "아직 확인 필요" 정적 ⑩ 의 뒷몫 (빠른 길 포트 갈래) 닫힘. 앞몫 (afd `AfdFastIoDeviceControl` 이 FALSE 를 돌리는 갈래) 은 남음.
  - 10절 표 "동기 핸들에 붙는지는 `[공백]`" → `NtSetInformationFile 0x1e` 로는 **못 붙인다** (동기 핸들이면 `0xC000000D`, `+0xa61`). 다른 길 (만들 때 등) 은 안 봄.
  - 11절 "`+0xbc2 ~ +0xbe4` … 비트 `0x8000000` 이 꺼져 있을 때" → 그 비트는 `0x29` 의 입력 비트 `0x4` 가 켠다.

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 코드 | 지금 (결과서) | 해야 할 일 |
|---|---|---|
| `nt_set_information_file.cpp:145` (실제 파일 `syscall_implementation/syscall_layer_cpp/src/file/…`) | **틀림** — 종류 14 · 16 만 받고 `30` · `41` 갈래 없음 | 앞쪽 검사 순서 · 표 값은 **맞으니 그대로**. `30`: 동기 → `0xC000000D` · 이미 묶임 → `0xC000000D` · 포트 핸들 접근 2 · 성공이면 포트 · 키를 파일 객체에 두고 **포트 참조를 쥠** · IOSB `{0,0}`. `41`: 동기 → `0xC000000D` · 비트 `1/2/4` OR 만 (끄기 없음, 다른 비트 무시, 묶임 안 봄) · IOSB `{0,0}`. 오류는 IOSB 안 씀 |
| 작은 차이 | IOSB 를 16 B 로 시험 (진짜는 첫 4 B) · 커널 호출자면 길이 · 정렬 건너뜀 | 에뮬레이터가 늘 사용자 호출자면 무관 |
| `include/syscall_layer/files.hpp:67` `FileObject` | **틀림 (칸 없음)** — `access` · `share` · `options` · `synchronous` · `position` 뿐 | 완료 포트 참조 (없으면 안 묶임) · 키 (64 비트) · 비트 셋 (`0x2000000/0x4000000/0x8000000`). 소켓 핸들이 `ObjectKind::file` 로 풀리는지는 에뮬레이터 쪽 `[공백]` (`0xC0000024` 면 이 갈래까지 못 옴) |
| `tests/file.cpp` | 없음 | `0x1e` 성공 IOSB `{0,0}` · 두 번째 `0x1e` → `0xC000000D` + **IOSB 안 바뀜** · 동기 핸들 `0x1e` / `0x29` → `0xC000000D` · `Length` 15 → `0xC0000004` · `0x29` 로 켠 비트 안 꺼짐 · `0x29` 를 `0x1e` 앞에 불러도 성공 |
| `src/threadpool/nt_io_completion.cpp` (패킷 넣기) | `ConnectEx` 바로 오류면 안 부름 | **지금 방식 맞음.** 부를 때는 5절 표 (바로 성공 + 비트 25 꺼짐 + `ApcContext` ≠ 0 / 경고 / PENDING 뒤 완료) 뿐 |
| (범위 밖) `nt_set_information_file.cpp:173` 종류 16 오류 | `write_io_status(io_status, 0xc000000d, 0)` 로 IOSB 에 씀 | 이 asm 으로는 **안 쓰는 쪽** — 종류 16 잘못된 모드도 `+0xbe8 → +0xb0a` → `IopCompleteRequest` 오류 갈래 (`+0xb3 ~ +0xbf`, `+0x10e ~ +0x11a → +0x4b7 → +0x4c1`). 동적 3 으로 한 번 확인할 만함 |
| (범위 밖) 파일 객체 이벤트 | 결과서: 에뮬레이터가 모델 안 하면 안 보임 | 비동기 핸들 성공이면 `FileObject+0x98` 이벤트를 세우고 `FinalStatus` (`+0x38`) 를 status 로 (`IopCompleteRequest +0x2a5 ~ +0x2c1`, `0x4000000` 꺼짐일 때) |

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 6, 대기열 번호는 오케스트레이터가 붙임. test.exe 가 아니라 **Go 프로그램 (소켓)** — run124 · run126 동적과 같은 세션에 얹을 수 있음. 멈춤점은 프로세스 조건):

1. Go 가 실제로 내는 값 — `nt!NtSetInformationFile+0xb0a` (`r13d` = status, `rbx` = FileObject, `rsi` = IRP): `dt nt!_FILE_OBJECT @rbx CompletionContext Flags`, `dt nt!_IO_COMPLETION_CONTEXT poi(@rbx+0xb0)`. `0x29` 는 `+0xd1e` 에서 `dwo(@rbx+0x50)` (Go 가 `1|2 = 3` 을 줄 것이라는 기억 `[추측]`).
2. 소켓 핸들이 비동기 (`Flags & 2` = 0) 인지, 앞 호출이 `0xC000000D` 가 아닌지 (`+0xa53` · `+0xa61`).
3. 오류 때 IOSB 안 씀 — 이미 묶은 핸들에 `0x1e` 를 한 번 더, 호출 전 IOSB 에 표식 값 (`0xdeadbeefdeadbeef`) → 그대로인지. `nt!IopCompleteRequest+0x4c1` 에 걸리면 맞음.
4. `ConnectEx` (`0x120c7`, `AfdSuperConnect`) 가 망 없는 PC 에서 바로 오류인가 PENDING 뒤 오류인가 — `nt!IopCompleteRequest+0x4b7` 의 `Irp+0x41`, `+0x3b4` 가 걸리나. run124 동적 ⑤ 와 같은 질문.
5. `0x1e` 직후 같은 IRP 가 `IopCompleteRequest+0x3b4` 에 **안** 걸리는지.
6. 포트 참조 수 — `0x1e` 전후 완료 포트 객체 포인터 참조 수 +1, 소켓 핸들을 닫아도 파일 객체가 지워질 때까지 그대로인지 (`IopDeleteFile+0x29d`).

- 추천 멈춤점 (결과서): `nt!NtSetInformationFile+0xa95` · `+0xb0a` · `+0xd1e` · `+0x1041` (경주) · `nt!IopCompleteRequest+0x4c1` · `+0x3b4` · `nt!IopXxxControlFile+0xe82`.

정적 (결과서 "다음 요청 후보"):

1. **afd `AfdSuperConnect` (`0x120c7`)** — `IoMarkIrpPending` 을 부르나, 망 없는 PC 에서 어디서 `0xC000023C` 를 만드나, `AfdFastIoDeviceControl` 이 이 코드를 처리하나. 위 6절 `[공백]` 을 닫는다 (run124 정적 ② · run126 정적 ④ 의 `AfdSuperConnect` 몫과 같음).
2. `IopReplaceCompletionPort` (`0x3d`, rva `0x4bf098`, 221 B) — 포트 바꾸기까지 가면.
3. `NtRemoveIoCompletion` / 패킷 넣기 (`0x308690`, 956 B) — 패킷이 `GetQueuedCompletionStatus` 로 나올 때의 모양 (Key · ApcContext · IOSB). run126 정적 ⑧ 과 같음.
4. `IopSetFileObjectIosbRange` (`0x2a`) — 에뮬레이터가 이 종류를 받을 때만.
5. (flow-writer 보탬, 낮음) `ObpReferenceObjectByHandleWithTag` 의 조건별 status (`0xC0000008` · `0xC0000024` · `0xC0000022` 가 어느 조건에서인지) — 포트 핸들 풀기 실패의 정확한 값.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서가 정보 종류 표 셋 (`0x54` 칸) 을 PE 파일 바이트로 읽고 PE_emulator 쪽 표와 84칸 대조로 검증했다 — 다른 `Nt*InformationXxx` 표에도 같은 방법을 쓸 수 있다 (하네스 제안 아님, 기록만).
- `_IO_COMPLETION_CONTEXT` 는 PDB 타입이 있어서 `dt nt!_IO_COMPLETION_CONTEXT` 로 바로 읽힌다. 파일 객체 `Flags` 비트 이름은 PDB 에 없으니 값 (`0x2000000` 등) 으로 본다.
