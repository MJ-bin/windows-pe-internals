# 132 alpc-completion — 정적 분석 결과

- 요청서: `_workspace/sast/132_alpc-completion.md` (Q248) · 원문 `../PE_emulator/_workspace/05_tuning_planner/309_internals-alpc-completion.md`
- 빌드: Windows 11 24H2 26100.9457 x64. 날짜 2026-11-23.
- **모두 이미 analyze 된 모듈이라 다시 analyze 하지 않았다.** `^이름$` export (depth 0) 만 했다: ntdll 11개 · ntoskrnl 15개 · rpcrt4 3개 (rpcrt4 프로젝트는 잠겨 있지 않았다 — 도는 Ghidra 없음 확인 후 바로 export). winnsi 는 export 없이 `pefile` 로 PE 데이터만 읽었다.
- 근거 표기: `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` (부르기는 call 자리, 괄호 `ret` 가 돌아올 자리). 형식 문자열은 `pefile` 로 읽은 PE 데이터 rva. **NDR 바이트 꼴은 "형식 문자열을 읽은 것 `[정적]`" 과 "공개 NDR 2.0 규칙으로 풀어 쓴 것" 을 칸마다 구분해 적었다.**
- 이 문서에 디컴파일 코드를 붙이지 않았다. 경로 + 우리 말 요약만.

## 스크립트 문제

없음. (참고: `TppWorkerThread` 디컴파일은 스택 프레임이 뒤섞여 읽기 어렵다. 이 문서는 모두 `.asm` 줄로 확인했다.)

## PDB GUID 확인 (쓴 모듈마다)

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | PDB 안 GUID (`llvm-pdbutil dump --summary`) | 비고 |
|---|---|---|---|
| ntoskrnl.exe | `C29EBFB06B78B3C020DCA66D99713F9E1` | `{C29EBFB0-6B78-B3C0-20DC-A66D99713F9E}` | **기준 GUID 와 같다.** analyze 로그 `SetPdb:` 줄 있음 (2026-10-19). 공개 PDB 인데 `_ALPC_PORT` 등 구조체 타입이 들어 있어 `pdb_struct.py` 로 칸 이름을 읽었다. |
| ntdll.dll | `C3093720177DA6851519DCFFE8DF53FE1` | `{C3093720-177D-A685-1519-DCFFE8DF53FE}` | `SetPdb:` 줄 있음 |
| rpcrt4.dll | `0204A8A137C42B7DF1F0811A3A8277B21` | `{0204A8A1-37C4-2B7D-F1F0-811A3A8277B2}` | `SetPdb:` 줄 있음 (2026-11-15) |
| winnsi.dll | `907CE42FFA71F5085D1468AD2D40D62C1` | `{907CE42F-FA71-F508-5D14-68AD2D40D62C}` | `SetPdb:` 줄 있음 (2026-11-22). export 안 함, PE 데이터만 읽음 |

(PDB 안 Age 는 ntoskrnl 6 · ntdll 4 · rpcrt4 2 · winnsi 3 이고 심볼 서버 키는 바이너리의 Age 1 — 기존 문서와 같은 모양.)

## 버전·스크립트 대조 ("알려진 사실" 표에 이 함수들 줄이 없어 원본 바이트로 대조)

`.asm` 의 `+0x` 를 `pefile` 로 읽은 원본 바이트와 맞췄다 — **전부 일치**.

| 자리 | 바이트 | 뜻 |
|---|---|---|
| `TpAllocAlpcCompletionEx +0x13` | `e8 0c 00 00 00` | `CALL TppAllocAlpcCompletion` |
| `TppAllocAlpcCompletion +0x1bf / +0x1ca / +0x1d1` | `41 b9 10 00 00 00` · `41 8d 51 f2` · `e8 …` | `MOV R9D,0x10` · `LEA EDX,[R9-0xe]` (= 2) · `CALL NtAlpcSetInformation` |
| `TppAllocAlpcCompletion +0x1e4` | `e8 03 04 00 00` | `CALL TpAdjustBindingCount` |
| `TppWorkerThread +0x379 / +0x55e` | `e8 …` · `e8 5d f2 ff ff` | `NtWaitForWorkViaWorkerFactory` · `TppAlpcpExecuteCallback` |
| `TppAlpcpExecuteCallback +0x438 / +0x449` | `4d 8b cd` · `e8 …` | `MOV R9,R13` · 콜백 호출 |
| `nt!NtAlpcSetInformation +0x127 / +0x13b` | `0f 85 79 02 00 00` · `e8 8c 03 00 00` | `JNZ` (길이 ≠ 0x10) · `CALL AlpcpAssociateIoCompletionPort` |
| `nt!AlpcpAssociateIoCompletionPort +0x1e5 / +0x212` | `b8 48 00 00 c0` · `b8 bb 00 00 c0` | `0xc0000048` · `0xc00000bb` |
| `nt!AlpcpQueueIoCompletionPort +0x1b7` | `49 89 5f 20` | `MOV [R15+0x20],RBX` (ApcContext 칸) |
| `nt!AlpcpCompleteDispatchMessage +0xeb4` | `e8 57 e7 a7 ff` | `CALL AlpcpQueueIoCompletionPort` |
| `nt!AlpcpReceiveMessagePort +0x19c / +0x1ba` | `75 1c` · `bf 01 00 00 c0` | `JNZ` · `MOV EDI,0xc0000001` |
| `rpcrt4!CreateAlpc +0x66` · `IoCompleted +0xc8` | `48 ff 15 …` | `CALL [TpAllocAlpcCompletionEx]` · `CALL [NtAlpcSendWaitReceivePort]` |

**판정: 일치.** 스크립트·바이너리 둘 다 믿어도 된다.

---

## 먼저 읽는 한 줄 답

**질문 1 (완료 포트 붙이기)**
1. `CreateAlpc +0x66` → `TpAllocAlpcCompletionEx` (Ex 깃발 1 만 얹은 29 바이트 썽크) → `TppAllocAlpcCompletion` 이 **`NtAlpcSetInformation(포트 핸들, 정보 종류 2, &입력, 길이 0x10)` 한 번**을 부른다 (`+0x1d1`). 입력 16바이트 = **`+0x00` CompletionKey = 새 TP_ALPC 객체 포인터 · `+0x08` CompletionPort = 풀의 IO 완료 포트 핸들 (풀 `+0x40`)**. 같은 함수 안에 `NtAlpcQueryInformation`·`NtSetIoCompletion*` 는 **없다**. 다른 syscall 은 `TpAdjustBindingCount(풀, +1)` 안의 `NtSetInformationWorkerFactory(공장, 3, &1, 4)` 하나 (풀의 묶음 수가 0 → 1 일 때만), 그리고 전역 풀을 처음 만드는 호출 (run125).
2. 커널 (종류 2): 길이 ≠ 0x10 → `0xc0000004` · 포트 핸들 0 → `0xc000000d` · 핸들이 ALPC 포트 아니면 `0xc0000024` 등 · 완료 포트 핸들 0 → `0xc000000d` · **이미 붙어 있음 → `0xc0000048`** · 완료 목록 있음 → `0xc00000bb` · 완료 포트 핸들 참조 (접근 2) 실패 → 그 status · 패킷 쌈 만들기 실패 → `0xc000009a`. 성공이면 포트 `+0x20 CompletionPort` (객체) · `+0x28 CompletionKey` · `+0x30 CompletionPacketLookaside` 가 채워진다. rpcrt4 는 붙이기가 어떤 이유로든 실패하면 `CreateAlpc +0xb6` 에서 **14** 로 바꿔 돌려준다.

**질문 2 (통지 한 번)**
1. (a) **메시지 하나 = 완료 포트에 패킷 하나.** 합치지도 건너뛰지도 않는다 ("이미 알림" 표시 칸 없음). 패킷 = **`KeyContext` = 포트의 CompletionKey (= TP_ALPC 포인터) · `ApcContext` = 0 (메시지를 완료 목록에 올렸을 때만 −1) · `IoStatus.Status` = 0 · `IoStatusInformation` = 0**. 메시지를 포트 큐 (`MainQueue +0x90`) 에 넣은 **뒤**에 넣는다. 붙기 전에 이미 쌓인 메시지는 붙이는 순간 메시지 수만큼 한꺼번에 넣는다.
2. (b) 일꾼은 `NtWaitForWorkViaWorkerFactory` 출력 (항목 0x20 바이트 `{키, ApcContext, 상태, 정보}`) 의 키로 `[키+0x38] == TppAlpcpExecuteCallback` 을 가려 **`TppAlpcpExecuteCallback(인스턴스, 키, ApcContext, &IoStatus)`** → **`LrpcIoComplete(인스턴스, 문맥 = [키+0xa0] = &연결객체+0xe0, TP_ALPC = 키, ApcContext)`** (Ex 갈래, 4 인자). `LrpcIoComplete` 는 문맥의 첫 칸 함수 (얇은 썽크 `LrpcCAssociationConnectIoHandler`) 를 부르는데, 이 썽크가 **`this = RCX−0xe0`, `RDX = 0` 으로 바꿔 `IoCompleted` 로 꼬리 점프** — 그래서 클라이언트 길에선 ApcContext 값이 버려지고 `IoCompleted` 는 늘 "완료 문맥 없음 (Flags 0)" 으로 받는다. **콜백 뒤에 알림을 다시 켜는 syscall 은 없다** (커널에 켜고 끄는 칸이 없다).
3. (c) 통지 하나당 `NtAlpcSendWaitReceivePort` 받기 호출은 **정확히 한 번** (Flags 0, 타임아웃 NULL). **완료 포트가 붙은 포트에 쌓인 메시지가 없으면 커널은 기다리지 않고 `0xc0000001` (STATUS_UNSUCCESSFUL) 을 돌려주고 버퍼·길이·속성에 아무것도 쓰지 않는다.** rpcrt4 는 `0xc0`·`0xc0000041` 밖의 값을 `RpcpReportFatalError` (치명) 로 보내므로 **"패킷 하나 = 메시지 하나 = 받기 한 번" 이 어긋나면 프로세스가 죽는다.** (Flags `0x1000000` 의 직접 받기는 이 길에 안 쓰인다 — 2-3.)

**질문 3 (NDR 본문) — 형식 문자열 읽은 결과 (NDR 2.0 판, 3 절)**
1. EPM 프로시저 8 요청 스텁 (`+0x68` 부터, 질문 탑 octets 66 바이트일 때 **120 바이트**): `+0x00` 객체 UUID 16 (참조 포인터 → 인라인) · `+0x10` 탑 conformance u32 (= 66) · `+0x14` `tower_length` u32 (= 66) · `+0x18` octets 66 (+2 패딩) · `+0x5c` SID 포인터 u32 (NULL = 0) · `+0x60` 끝점 핸들 20 (처음엔 20바이트 0) · `+0x74` `max_towers` u32 (= 4). 응답 스텁 (탑 1개, octets 73): `+0x00` 끝점 핸들 20 · `+0x14` 탑 개수 u32 · `+0x18` 배열 max_count (= 요청의 `max_towers`) · `+0x1c` offset 0 · `+0x20` actual_count (= 개수) · `+0x24` 포인터 ID u32 × 개수 · 탑마다 {max_count, `tower_length`, octets, 4 정렬 패딩} · 맨 끝 상태 u32 → **128 바이트**.
2. nsi 프로시저 5: 응답 = **컨텍스트 핸들 20 + 반환 DWORD (`+0x14`) = 24 바이트**. 요청 (서버 흉내는 안 읽어도 됨) = 구조체 12 + 모듈 ID 20 + compartment 8 = 40 바이트 (모듈 ID 가 있을 때).

**질문 4 (고르는 문법)**
1. 클라이언트는 **서버가 bind 답 `+0x48` 에 쓴 비트 하나를 그대로 따른다** (둘 다 켜면 거부 — `CompleteBind` 가 비트가 정확히 하나여야 통과시킴, sast/130). NDR 2.0 (비트 1) 을 고르면 `Ndr64pClientSetupTransferSyntax` 가 NDR 2.0 GUID (`0x8a885d04…`) 를 알아보고 **DCE 엔진 (`AsyncDceClient`)** 으로, 아니면 NDR64 엔진으로 간다 — **둘 다 코드가 있다.** 서버 흉내가 하나만 골라 답할 때 **이번에 형식 문자열로 풀어 쓴 쪽은 NDR 2.0 (마스크 1)** 이라 그쪽이 안전하다. NDR64 판은 이번에 안 풀었다. 진짜 서버가 뭘 고르는지는 정적으로 못 정함 (동적 1).

---

## 1. 완료 포트 붙이기 (질문 1)

### 1-1. syscall 차례 표 (비동기 연결 뒤 한 번)

`NtAlpcSetInformation` 의 정보 종류 숫자는 코드에서 읽은 값이고, 이름 (`AlpcAssociateCompletionPortInformation` = 2, `AlpcAdjustCompletionListConcurrencyCountInformation` = 8) 은 공개 phnt 이름이라 `[이름 추측]`.

| 차례 | 부르는 것 | 인자·칸 | 실패 status | 줄 |
|---|---|---|---|---|
| 0a | `NtAlpcConnectPort` (비동기, Flags 0) | sast/127 2-1 그대로 | | `rpcrt4 AlpcConnect +0x293` |
| 0b | **`NtAlpcQueryInformation(포트, 0, &버퍼 0x10, 0x10, NULL)`** (연결 직후 한 번 — 이 호출은 "완료 포트 붙이기" 가 아니라 "공유 섹션 있나" 묻기) | 성공이고 `Flags & 0x1000000` 이면 "없음", 그 밖 (실패 포함) = "있음" 취급 | 실패해도 됨 | `AlpcConnect +0x388` (ret `+0x38d`) → `IsPortWithSharedSection +0x25` (ret `+0x2c`), `+0x35 TEST [RSP+0x30],0x1000000` |
| 1 | `Connect` 가 `EnableAsync` 호출 | `AlpcConnect` 가 `0xc0021018` + `*출력=1` 로 돌아온 뒤 | | `rpcrt4 Connect +0x3b1` (ret `+0x3b6`), 다른 길 `+0x398` |
| 2 | `EnableAsync` → `CreateAlpc(포트 = [연결객체+0x88], …, 문맥 = &[연결객체+0xe0], 출력 = &[연결객체+0xe8])` | `[연결객체+0xe0]` 칸에 썽크 `LrpcCAssociationConnectIoHandler` (rva `0x96a70`) 주소를 먼저 씀 | `CreateAlpc` 가 실패값을 14 로 바꿈 | `EnableAsync +0x53 LEA RAX,[0x180096a70]` · `+0x64 MOV [R14],RAX` · `+0x6a` (ret `+0x6f`) (둘째 갈래 `+0xa5`·`+0xb3`·`+0xb6`) |
| 3 | `CreateAlpc` → `RPC_THREAD_POOL::InitializeCallbackEnvironmentIfNecessary` (유저) → `AllocWrapper(0x10)` → **`TpAllocAlpcCompletionEx(&결과, 포트 핸들, LrpcIoComplete, 문맥, 환경 `CallbackEnvironment`)`** | `RPC_THREAD_POOL::PrivatePool` 전역 (rva `0x1044fc`, 이미지 초기값 0 — `.data` 원본 바이트 밖이라 로드 때 0) 이 0 이면 환경 = NULL → **전역 풀** (`PrivatePool` 을 켜는 함수는 안 읽음 `[추측: 안 켜면 NULL]`; `InitializeCallbackEnvironmentIfNecessary +0x46`) | 아래 | `CreateAlpc +0x26` · `+0x34` · `+0x66` (ret `+0x6d`) · `+0x76` `RtlNtStatusToDosError`, 실패 `+0x87 JS` → `+0xb6 MOV EBX,0xe` |
| 4 | `TpAllocAlpcCompletionEx` = 5번째 인자 (환경) 를 넘기고 **6번째 인자 (Ex 깃발) = 1** 을 얹어 `TppAllocAlpcCompletion` 호출 | | | `TpAllocAlpcCompletionEx +0x9 MOV byte [RSP+0x28],1` · `+0x13` (ret `+0x18`) |
| 5 | (유저) 검사: 결과 포인터 · 콜백 · 포트 핸들이 0 이면, 환경 플래그 `[환경+0x38] & ~3` ≠ 0 이면, `PEB.Ldr.ShutdownInProgress` (`[Ldr+0x48]`) ≠ 0 이면 | | `0xc000000d` (`TppRaiseInvalidParameter` 호출 뒤) | `TppAllocAlpcCompletion +0x3f ~ +0x77`, `+0x5a TEST EBX,0xfffffffc`, `+0x27b ~ +0x285` |
| 6 | `RtlAllocateHeap(프로세스 힙, 태그, 0x128)` → TP_ALPC 객체 | | `0xc0000017` (`+0x244`) | `+0xa4` (ret `+0xa9`) |
| 7 | `TppCleanupGroupMemberInitialize(객체+0x48, 문맥, 환경, 플래그, TppAlpcpCleanupGroupMemberVFuncs)` — 환경이 NULL 이면 여기서 **전역 풀을 참조** (`TppPoolpReferenceGlobalPool`, 처음이면 풀 만들기 = run125) | 객체에 채움: `+0x38` = `TppAlpcpExecuteCallback` · `+0x98` = 콜백 (LrpcIoComplete) · `+0xa0` = 문맥 · `+0xd8` = 풀 · `+0x110` = 포트 핸들 · `+0x120` 비트 0 = Ex | 실패면 그 status | `+0xea` (ret `+0xef`), `+0xfd`, `+0x121`, `+0x136`, `+0x110 ~ +0x11b` |
| 8 | **`NtAlpcSetInformation(포트 핸들, 2, &{키, 풀 완료 포트}, 0x10)`** | `[RSP+0x38]` = 객체 포인터 (키), `[RSP+0x40]` = `[풀+0x40]` (풀의 IO 완료 포트 핸들) | 1-3 표 | `+0x1a9 MOV RAX,[R15+0x40]` · `+0x1ba MOV [RSP+0x38],RDI` · `+0x1bf` · `+0x1c5` · `+0x1ca` · `+0x1ce MOV RCX,R12` · **`+0x1d1` (ret `+0x1d6`)** |
| 9 | 성공이면 **`TpAdjustBindingCount(풀, +1)`** → 풀 `+0x1b0` 묶음 수 += 1, **이전 값이 0 이하이고 새 값이 1 이상일 때만** `NtSetInformationWorkerFactory([풀+0x38], 3, &1, 4)` | 정보 종류 3 = 묶음 수 (`WorkerFactoryBindingCount` `[이름 추측]`) | | `TppAllocAlpcCompletion +0x1dc` · `+0x1e4` (ret `+0x1e9`); `TpAdjustBindingCount +0xa` · `+0x16 ~ +0x1d` · `+0x3b` (ret `+0x40`) |
| 10 | 정리 그룹이 있을 때만 `TppCleanupGroupAddMember` (환경이 NULL 이면 안 감) | | | `+0x206 CMP [RSI+0x10],RBX` · `+0x20f` (ret `+0x214`) |
| 11 | 실패면 `TppCleanupGroupMemberDestroy` + `RtlFreeHeap` 하고 상태를 돌려줌, 성공이면 `*결과 = 객체` (`+0x251 MOV [R13],RDI`) | | | `+0x23d` · `+0x2ad` |

- **같은 포트·같은 완료 포트에 대한 syscall 은 8 번 하나뿐**이다. (`TpReleaseAlpcCompletion` → `TppAlpcpFree` 도 연결을 풀어 주는 `NtAlpcSetInformation` 은 안 부르고 `TpAdjustBindingCount(풀, −1)` 만 부른다 — 연결은 포트를 닫을 때 사라진다 `[추측: 닫는 쪽 커널은 안 읽음]`.)
- 풀의 IO 완료 포트 핸들 칸: `TpAllocPoolInternal +0x283 LEA R12,[RDI+0x40]` → `+0x292 NtCreateIoCompletion(&풀+0x40, 0x1f0003, 0, 동시성)` (ret `+0x297`) → 같은 핸들이 `+0x2f4 MOV R9,[R12]` 로 `NtCreateWorkerFactory` (`+0x303`) 에 들어간다. 공장 핸들 = 풀 `+0x38`. (run125 이 이미 다룬 몫 — 가리키기만.)
- 풀 `+0x1b0` (묶음 수): 이 풀에 묶인 객체 수. `TpAdjustBindingCount` 가 증감.

### 1-2. 입력 구조 (유저가 쓰는 16바이트) — 정보 종류 2

| 칸 | 오프셋 | 크기 | 값 | 줄 |
|---|---|---|---|---|
| CompletionKey | `+0x00` | 8 | **TP_ALPC 객체 포인터** (일꾼이 나중에 패킷의 `KeyContext` 로 돌려받는 값) | `TppAllocAlpcCompletion +0x1ba MOV [RSP+0x38],RDI` |
| CompletionPort | `+0x08` | 8 | **풀의 IO 완료 포트 핸들** (`[풀+0x40]`) | `+0x1a9`, `+0x1ad MOV [RSP+0x40],RAX` |
| 길이 | | | **0x10** | `+0x1bf MOV R9D,0x10` |
| 정보 종류 | | | **2** | `+0x1ca LEA EDX,[R9-0xe]` |

(커널 쪽 읽는 순서: `AlpcpAssociateIoCompletionPort(포트 객체, 핸들 = [입력+8], 키 = [입력+0])` — `NtAlpcSetInformation +0x12d MOV R8,[R12]` (키), `+0x131 MOV RDX,[R12+8]` (핸들), `+0x136 MOV RCX,[RSP+0x38]` (포트 객체).)

### 1-3. 커널 검사 표 — `NtAlpcSetInformation` 종류 2 → `AlpcpAssociateIoCompletionPort`

| 차례 | 검사 | 실패 status | 줄 |
|---|---|---|---|
| 1 | 포트 핸들 == 0 | `0xc000000d` | `nt!NtAlpcSetInformation +0x5f TEST RDI,RDI / +0x62 JZ` → `+0x243 MOV EDI,0xc000000d` |
| 2 | 입력 포인터 NULL (종류가 7·10 이 아닐 때) | `0xc000000d` | `+0x68 CMP [RSP+0x40],RSI / +0x6d JZ +0x22f`, `+0x239 CMP R14D,0xa` → `+0x243` |
| 3 | **사용자 모드 호출이고 길이 ≠ 0 이면 길이 > 0x48 → `0xc0000004`**; 아니면 입력을 (≤0x48 바이트, 주소는 `0x7fffffff0000` 으로 눌러) 복사 | `0xc0000004` | `+0x95 ~ +0xa3 JA +0x3c7`, `+0xa9 ~ +0xca` |
| 4 | `ObReferenceObjectByHandle(핸들, 접근 1, AlpcPortObjectType, 이전 모드)` | 그 status (`0xc0000008` 잘못된 핸들 · `0xc0000024` 종류 다름 · `0xc0000022` 접근 없음) | `+0xe0 MOV R8,[AlpcPortObjectType]`, `+0xfe MOV EDX,1`, `+0x108` (ret `+0x10d`), `+0x111 JS` |
| 5 | **길이 ≠ 0x10** (종류 2 의 길이 검사. 종류 5 는 `0xc000000d`, 종류 2 는 `0xc0000004` — 다르다) | **`0xc0000004`** | `+0x123 CMP R15D,0x10 / +0x127 JNZ +0x3a6` → `+0x3a6 MOV EDI,0xc0000004` |
| 6 | `AlpcpAssociateIoCompletionPort`: 완료 포트 핸들 == 0 | `0xc000000d` | `nt!AlpcpAssociateIoCompletionPort +0x21 TEST RDX,RDX / +0x24 JZ` → `+0x1a1` |
| 7 | 포트 `PortObjectLock` (`+0x160`) 을 독점으로 잡은 뒤 **`[포트+0x20] CompletionPort ≠ 0` (이미 붙음)** | **`0xc0000048`** (STATUS_PORT_ALREADY_SET) | `+0x2a ~ +0x57` 잠금, `+0x60 CMP [RSI+0x20],0 / +0x65 JNZ +0x1cc` → `+0x1e5 MOV EAX,0xc0000048` |
| 8 | **`[포트+0x168] CompletionList ≠ 0`** (완료 목록을 이미 등록한 포트) | **`0xc00000bb`** (STATUS_NOT_SUPPORTED) | `+0x6b CMP [RSI+0x168],0 / +0x73 JNZ +0x1f9` → `+0x212 MOV EAX,0xc00000bb` |
| 9 | `ObReferenceObjectByHandle(완료 포트 핸들, **접근 2**, IoCompletionObjectType, 이전 모드)` | 그 status (`0xc0000008`·`0xc0000024`·`0xc0000022`). 이 자리 직후 `[포트+0x20]` 에 얻은 객체 (실패면 0) 를 쓴다 | `+0x82 MOV EDX,2`, `+0x87 MOV R8,[IoCompletionObjectType]`, `+0xae` (ret `+0xb3`), `+0xbc MOV [RSI+0x20],RDX`, `+0xc4 JS` |
| 10 | `[포트+0x28] = 키` 를 쓰고, 완료 포트 객체의 동시성 수 (`[객체+0x2c]`, 객체 종류가 `0x15` 이면 `+0x298`) 와 4 중 **작은 값** 개의 패킷 쌈을 `AlpcpAllocateCompletionPacketLookaside` 로 만들어 `[포트+0x30]` 에 | 못 만들면 **`0xc000009a`** (STATUS_INSUFFICIENT_RESOURCES) + 참조 풀기 | `+0xd1 MOV [RSI+0x28],R14`, `+0xde ~ +0xf2`, `+0xfa` (ret `+0xff`), `+0xff MOV [RSI+0x30],RAX`, `+0x106 JZ +0x248` |
| 11 | **이미 쌓인 메시지 수만큼 패킷을 넣는다**: 포트 큐 `IncomingQueueLock` (`+0x88`) 아래 `MainQueue` (`+0x90`) 를 훑으며 항목마다 `AlpcpQueueIoCompletionPort(포트, 0, 0, 0, 0)` | | `+0x10c ~ +0x14e`, `+0x1a8 ~ +0x1b8` CALL (ret `+0x1bd`) |
| 12 | 성공 | 0 | `+0x16e XOR EBP,EBP` → `+0x185 MOV EAX,EBP` |

**호출한 쪽에서 본 오류 차례 요약**: 핸들 0 → 입력 NULL → 길이 > 0x48 → 포트 핸들 참조 → **길이 0x10** → 완료 포트 핸들 0 → **이미 붙음** → 완료 목록 → 완료 포트 핸들 참조 → 룩어사이드. (즉 "이미 붙은 포트에 틀린 완료 포트 핸들을 줘도 `0xc0000048`", "핸들 0 은 그보다 앞 `0xc000000d`".)

### 1-4. 포트 객체에 남는 칸 (PDB `_ALPC_PORT`, `pdb_struct.py ntoskrnl.exe _ALPC_PORT`)

| 칸 | 이름 (PDB) | 쓰는 값 |
|---|---|---|
| `+0x20` | `CompletionPort` (`_KQUEUE*`) | 참조를 얻은 IO 완료 객체 (핸들 아님) |
| `+0x28` | `CompletionKey` | 입력 `+0x00` 값 (TP_ALPC 포인터) |
| `+0x30` | `CompletionPacketLookaside` | 아래 쌈. 칸: `+0x08 Size` (= min(4, 동시성)) · `+0x0c ActiveCount` · `+0x10 PendingNullCount` · `+0x14 PendingCheckCompletionListCount` · `+0x20 FreeListHead` · `+0x28 CompletionPort` · `+0x30 CompletionKey` · `+0x38 Entry[Size]` (항목 `{ListEntry, Packet, Lookaside}` 0x18 바이트) |
| 쌈 속 패킷 | `_IO_MINI_COMPLETION_PACKET_USER` (0x50) | `PacketType = 4` (`+0x10`) · `MiniPacketCallback = AlpcpLookasidePacketCallbackRoutine` (`+0x38`) · `Context` = 항목 (`+0x40`) · `Allocated = 1` (`+0x48`) |

`[정적] nt!AlpcpAllocateCompletionPacketLookaside` (디컴파일: 풀 태그 `AlIn`, 크기 `Size×0x18+0x38`, 패킷은 `ExAllocatePool3(0x50)`). 이 칸들은 **유저에게 안 보인다.** 에뮬레이터가 따라 할 필요는 "포트에 (완료 포트, 키) 두 칸" 뿐이다.

---

## 2. 통지 한 번의 꼴 (질문 2)

### 2-1. (a) 커널 — 패킷이 어디서 어떤 값으로 들어가나

**누가 부르나**: `AlpcpCompleteDispatchMessage` (보내기·답하기·닫기·연결요청 처리의 끝, caller `AlpcpDispatchNewMessage +0x2e3` (ret `+0x2e8`) 등) 가 **메시지를 대상 포트의 `MainQueue` 에 먼저 넣고** (`+0x541 INC [RBP+0x1c0]` = `MainQueueLength`), 그다음 대상 포트에 완료 포트가 있으면 (`+0x470 CMP [RBP+0x20],RSI / +0x474 JZ`) 패킷을 넣는다.

| 조건 (`AlpcpCompleteDispatchMessage`) | 하는 일 | 줄 |
|---|---|---|
| 대상 포트 `[+0x20]` (CompletionPort) == 0 | 완료 포트 길 아님 → `Waitable` (`u1` 비트 9) 이면 세마포어, 아니면 콜백 객체 알림 | `+0x470`, `+0xb5e ~ +0xb71`, `+0xf0b ~ +0xf3b`, `+0xeca ~ +0xf09` |
| 대상 포트에 완료 포트 **있음** 이고 **(디스패치 맥락 `Flags & 0x20000` = 동기 요청) 도 아니고 (`Flags & 4` = 보내기 길) 도 아님** | **여기서 바로** `AlpcpQueueIoCompletionPort(대상 포트, DIL, 1, 0, Flags&0x400000 ≠ 0)` | `+0x5d1 TEST R12B,R12B`, `+0x5da TEST R15D,R15D / +0x5dd JZ +0xe92`, `+0xe92 CMP [RSP+0x100],ESI`, `+0xe9f ~ +0xeb4` (ret `+0xeb9`) |
| 대상 포트에 완료 포트 있음 이고 (**`Flags & 0x20000` 또는 `Flags & 4`**) | 지금 안 넣고 맥락에 표시: `+0x3a` (`SignalCompletion`) = 1, `+0x3b` (`PostedToCompletionList`) = DIL. 나중에 `NtAlpcSendWaitReceivePort` 가 끝에서 `AlpcpSignal(맥락, 0, (Flags>>0x16)&1)` 를 불러 **똑같은 패킷**을 넣는다 | `+0x5e3 MOV byte [R13+0x3a],1`, `+0x5e8 MOV [R13+0x3b],DIL`; `nt!NtAlpcSendWaitReceivePort +0x21b OR EDI,4` (보내기 길에서 켬), `+0x150 BTR [맥락 Flags],2` · `+0x176` (ret `+0x17b`) `AlpcpSignal`; `AlpcpSignal +0x2c CMP [RCX+0x3a],0` → `+0xa31 CALL AlpcpQueueIoCompletion` |
| `AlpcpSignalPortAndUnlock` (취소·끊기) | `[포트+0x20] ≠ 0` 이면 `AlpcpQueueIoCompletionPort(포트, 0, 1, 0)` | `AlpcpSignalPortAndUnlock` 디컴파일 (첫 갈래) |

**패킷 칸** (`AlpcpQueueIoCompletionPort`):

| 칸 (PDB `_IO_MINI_COMPLETION_PACKET_USER`) | 값 | 어디서 오나 | 줄 |
|---|---|---|---|
| `KeyContext` `+0x18` | **포트의 `CompletionKey`** (= TP_ALPC 포인터) | `[포트+0x28]` | `nt!AlpcpQueueIoCompletionPort +0x17b MOV RDI,[RSI+0x28]`, `+0x1af MOV [R15+0x18],RDI` |
| `ApcContext` `+0x20` | **0** (메시지를 완료 목록에 올렸을 때만 `0xffffffffffffffff`) | 두 번째 인자 `DL` (= `AlpcpCompleteDispatchMessage` 의 `DIL`: 완료 목록 갈래에서만 1) → `−(DL≠0)` | `+0x17f NEG BPL`, `+0x182 SBB RBX,RBX`, `+0x1b7 MOV [R15+0x20],RBX`; `AlpcpCompleteDispatchMessage +0x560 XOR DIL,DIL` (완료 목록 없는 길), `+0x9d2 MOV DIL,1` (완료 목록 길) |
| `IoStatus` `+0x28` | **0** | 상수 0 | `+0x1bb MOV [R15+0x28],EBP` (EBP = 0) |
| `IoStatusInformation` `+0x30` | **0** | 상수 0 | `+0x1bf MOV [R15+0x30],RBP` |

- **패킷은 메시지마다 하나다.** 건너뛰기·합치기 없음: `AlpcpQueueIoCompletionPort` 는 호출될 때마다 `Lookaside.ActiveCount < Size` 면 쌈에서 패킷을 꺼내 채워 큐에 넣고 (`+0x91 MOV EDX,[RBX+0xc]` · `+0x94 CMP EDX,[RBX+8]` · `+0x97 JNC +0x2ea`), 가득이면 **`AlpcpQueueIoCompletion(완료 포트, 키, ApcContext, NULL, …)` 로 새 패킷** (`+0x2ff ~ +0x31b`, ret `+0x320`). 새 패킷을 못 만들면 (반환 0) `PendingNullCount` (`ApcContext` 가 0 일 때) 또는 `PendingCheckCompletionListCount` 를 올려 두었다가 패킷이 반납될 때 (`AlpcpLookasidePacketCallbackRoutine`) 대신 넣는다 — 건너뛰는 일은 없다. "이미 넣은 패킷이 안 꺼졌으면" 이라는 검사는 **어디에도 없다.**
- `AlpcpQueueIoCompletion(완료 포트, 키, ApcContext, 패킷, …)` 은 패킷에 `+0x18` 키 · `+0x20` ApcContext · `+0x28` 0 · `+0x30` 0 을 쓰고 큐 (`KQUEUE`) 에 넣는다 (`nt!AlpcpQueueIoCompletion` 디컴파일 앞부분). 같은 4칸이다.
- 메시지 종류와 상관없다. 답 (종류 2), 요청 (종류 1), **닫기 메시지 (종류 5, `AlpcpDispatchCloseMessage`)** 도 각각 패킷 하나. rpcrt4 는 종류 5 를 `HandleConnectionClose` 로 처리한다 (sast/130 1-3).

### 2-2. (b) 유저 — 일꾼 → 콜백 인자 표

| 단계 | 하는 일 | 인자·칸 | 줄 |
|---|---|---|---|
| 1 | `NtWaitForWorkViaWorkerFactory(공장 = [풀+0x38], 항목 배열, 개수, &받은 수)` (배열은 처음 0x10 개, 항목 안은 커널이 쓸 땐 0x20 바이트) | 출력 항목: `+0x00` 키 · `+0x08` ApcContext · `+0x10` 상태 · `+0x18` 정보 | `TppWorkerThread +0x379` (ret `+0x37e`) (배열 `+0x2e9 ~ +0x376`) |
| 2 | `TppPrepareDirectParams` 가 배열에서 **마지막 (키 ≠ 0) 항목을 이 스레드 몫으로** 일꾼 스택 `+0x298 ~ +0x2b8` 에 복사하고 나머지는 풀의 일감 줄에 올림 | 스택 `+0x298` 키 · `+0x2a0` ApcContext · `+0x2a8` 상태 (qword) · `+0x2b0` 정보 | `TppWorkerThread +0x46e` (ret `+0x473`); `TppPrepareDirectParams` 디컴파일 (`param_2 + 항목번호×4 qword` → `+0x138 ~ +0x150` 복사) |
| 3 | 키 ≠ 0 이면 **`[키+0x38]` 의 함수 주소로 종류를 가른다**: `== TppAlpcpExecuteCallback (0x19aa0)` → ALPC 갈래, `TppIopExecuteCallback`, `TppWaitCompletion`, 그 밖 = 간접 호출 (일/타이머) | `RDX = 키`, `R8 = [RSP+0x2a0]` (ApcContext), `R9 = &[RSP+0x2a8]` (상태 칸 주소), `RCX = &[RSP+0x198]` (콜백 인스턴스 칸) | `TppWorkerThread +0x485 MOV RCX,[RSP+0x298]`, `+0x49e MOV RAX,[RCX+0x38]`, `+0x536 MOV RAX,[RDX+0x38]`, `+0x53a LEA RSI,[0x180019aa0]`, `+0x541 LEA R9,[RSP+0x2a8]`, `+0x549 MOV R8,[RSP+0x2a0]`, `+0x551 CMP RAX,RSI`, `+0x556 LEA RCX,[RSP+0x198]`, **`+0x55e CALL TppAlpcpExecuteCallback` (ret `+0x563`)** |
| 4 | `TppAlpcpExecuteCallback(인스턴스, TP_ALPC, ApcContext)` 가 모듈 참조·카운터 (`+0x4d INC.LOCK [R14+0x48]`)·잠금 (`+0x80`..)·활성화 문맥·ETW 를 처리하고 **콜백 호출** | Ex 깃발 (`[TP_ALPC+0x120]` 비트 0) 이 **켜져 있으면** 4 인자 `콜백(인스턴스, [객체+0xa0], 객체, ApcContext)`, 꺼져 있으면 3 인자 `콜백(인스턴스, [객체+0xa0], 객체)` | `TppAlpcpExecuteCallback +0x34 MOV [RSP+0x40],R8`, `+0x19c MOV R13,[RSP+0x40]`, `+0x423 TEST [R14+0x120],1 / +0x42b JZ +0x7d2`, `+0x431 MOV [R15+0x88],R14`, **`+0x438 MOV R9,R13`**, `+0x43b MOV RDX,[R14+0xa0]`, `+0x442 MOV RAX,[R14+0x98]`, **`+0x449` (ret `+0x44e`) 간접 호출 (CFG)**; 3 인자 갈래 `+0x7d2 ~ +0x7e0` (ret `+0x7e5`) |
| 5 | `LrpcIoComplete(인스턴스 RCX, 문맥 RDX, TP_ALPC R8, ApcContext R9)`: **`RBX = RCX`, `R14 = RDX`, `R15 = R9`**. 스레드 객체 `[THREAD+0xc0]` 에 인스턴스를 적어 두고, **`[R14]` 에 든 함수를 `(RCX = 문맥, RDX = ApcContext)` 로 부른다** | 문맥 = `&[연결객체+0xe0]`, 그 첫 칸 = `LrpcCAssociationConnectIoHandler` | `LrpcIoComplete +0x25 MOV R15,R9`, `+0x28 MOV R14,RDX`, `+0x2b MOV RBX,RCX`, `+0x93 MOV [RDI+0xc0],RBX`, `+0xba ~ +0xc3` (ret `+0xc8`) `CALL [cfg][R14]`, 끝에 `+0x11b MOV [RBX+0xc0],R12` (R12 = 0) |
| 6 | 썽크 `LrpcCAssociationConnectIoHandler`: **`RCX = RCX − 0xe0` (연결 객체), `EDX = 0`, 꼬리 점프 `IoCompleted`** → `IoCompleted(연결객체, RPCP_ALPC_CONTEXT_ATTR* = NULL)` | → 완료 문맥 = NULL → Flags 0, 받기만 하는 호출 (sast/130 2-4) | `LrpcCAssociationConnectIoHandler +0x0 ADD RCX,-0xe0`, `+0x7 XOR EDX,EDX`, `+0x9 JMP IoCompleted` (바이트 `48 81 c1 20 ff ff ff 33 d2 e9 …`) |

- `IoCompleted` 의 둘째 인자 형은 PDB 이름으로 `RPCP_ALPC_CONTEXT_ATTR*` — `+0x14` = `MessageId`, `+0x18` = `CallbackId` 로 읽는 칸이 ALPC 컨텍스트 속성 (`PortContext`, `MessageContext`, `Sequence`, `MessageId`, `CallbackId`) 의 모양과 맞다 `[추측: 모양 대조]`. **클라이언트 연결 객체는 썽크가 늘 NULL 을 넘기므로 `Flags 0x1000000` 갈래 (`IoCompleted +0x277 ~ +0x288`) 는 이 길에서 안 쓰인다.** (서버 쪽 `ServerStartingToListen` 이 쓰는 문맥은 다른 썽크일 수 있다 `[공백: 안 읽음]`.)
- **콜백 뒤 다시 알림을 켜는 syscall 이 있나: 없다.** `TppAlpcpExecuteCallback` 안의 `NtAlpcSetInformation` 은 한 곳 (`+0x684`, ret `+0x689`: 정보 종류 8, 길이 4, 입력 = 새 목표 값) 이고 **`[객체+0x120] & 3 == 3` 일 때만** 간다 (`+0x52 ~ +0x5e`); `TppAllocAlpcCompletion` 은 비트 0 만 쓰므로 (`+0x110 ~ +0x11b`) 이 길에서는 안 간다 (비트 1 을 켜는 자리는 완료 목록을 등록하는 다른 함수 `[추측]`). 그 밖 syscall 은 ETW (`NtTraceEvent`)·`NtSetInformationThread`·`NtAlertThreadByThreadId` 뿐 (조건부). 일꾼은 콜백 뒤 `TppCallbackEpilog` 를 부르고 다음 일을 찾거나 다시 `NtWaitForWorkViaWorkerFactory` 로 돌아간다.
- 항목 하나만 한 번에 오든 여럿이든 에뮬레이터는 **항목을 하나씩 순서대로** 주면 된다 (둘 이상이면 일꾼은 마지막 것을 자기가, 나머지를 풀에 올림 — 같은 결과).

### 2-3. (c) 받기만 하는 `NtAlpcSendWaitReceivePort` — 쌓인 메시지 없을 때

호출 모양 (`IoCompleted +0xc8`): `(포트, Flags = 0, 보낼 메시지 NULL, 보낼 속성 NULL, 받기 버퍼 = 새 BCACHE 버퍼, &길이 (0x1000 또는 0xff00), &받기 속성 (Allocated 0xfa000000), 타임아웃 NULL)`.

| 경우 | 커널이 돌려주는 status | 버퍼·길이·속성에 쓰는 것 | 줄 |
|---|---|---|---|
| **큐에 메시지가 있음** (정상) | `0` | 헤더 0x28 + 데이터 (`DataLength` 바이트) 를 버퍼에, 받기 속성 (`AlpcpExposeAttributes`), 길이는 쓰지 않음 (이전 길이 칸은 읽기만) | `NtAlpcSendWaitReceivePort +0x141` (ret `+0x146`) → `AlpcpReceiveMessage +0x2da` (ret `+0x2df`) `AlpcpReceiveMessagePort`; 속성 `+0x402` |
| 큐가 **비었고** 포트에 **완료 포트가 붙어 있음** (이 길) | **`0xc0000001`** (STATUS_UNSUCCESSFUL). **기다리지 않음** (타임아웃 NULL 이어도) | **아무것도 안 씀.** (들어갈 때 사용자 버퍼 앞·뒤 바이트를 읽고 되쓰는 접근 확인만 함) | `AlpcpReceiveMessagePort +0xd4 ~ +0x107` (Large·Main·Canceled 큐 모두 빔), `+0x10d ~ +0x117 JZ +0x17d` (완료 목록 없음), **`+0x197 CMP [RDI+0x20],0 / +0x19c JNZ +0x1ba` → `+0x1ba MOV EDI,0xc0000001`** |
| 같은 빈 큐, 완료 포트 **없는** 포트 | 기다림 (`Waitable` 세마포어 / 이벤트) — 타임아웃이 NULL 이면 무한, 끝나면 `0x102` (STATUS_TIMEOUT) 등 | — | `AlpcpReceiveMessagePort +0x19e TEST [RDI+0x1a0],0x200`, `+0x1c3 ~ +0x1d9`, `+0x78a MOV EDI,0x102` — **이 길 아님** |
| 포트가 **닫힘** (`u1.Closed` 비트 6) | `0xc0000700` | 안 씀 | `AlpcpReceiveMessagePort +0x8b TEST AL,0x40 / +0x8d JNZ +0x66c` → `+0x66c MOV EDI,0xc0000700` |
| 연결 **거절됨** (`u1.ConnectionRefused` 비트 4) | `0xc0000041` | 안 씀 | `+0x93 TEST AL,0x10 / +0x95 JNZ +0x676` → `+0x676 MOV EDI,0xc0000041` |
| 버퍼가 메시지보다 작음 (`*길이 < 0x28 + DataLength`) | `0xc0000023` (STATUS_BUFFER_TOO_SMALL) — 메시지는 큐에 남음 | `*길이` 에 필요한 크기를 씀 (`AlpcpReturnMessageOnInsufficientBuffer`) | `AlpcpReceiveMessage +0x506` (ret `+0x50b`) |
| **Flags `0x1000000`** (직접 받기: 버퍼의 `MessageId`·`CallbackId` 로 한 메시지를 집음) | `MessageId` 0 → `0xc000000d`; 번호 표에 없음·콜백 번호 안 맞음 → `0xc0000702` (`AlpcpLookupMessage`); 예약 항목 이미 쓰는 중 `0xc0000708`; 포트 닫힘 `0xc0000700`; 거절 `0xc0000041` | 안 씀 | `AlpcpReceiveMessage +0x10f AND R14D,0x1000000`, `+0x5dc`, `+0x630` (ret `+0x635`) → `AlpcpReceiveDirectMessagePort +0x232 / +0x239 / +0x240`; `AlpcpLookupMessage +0x20e / +0x2f1 / +0x30e / +0x331 / +0x34d` |

- **rpcrt4 가 이 status 를 다루는 법** (`IoCompleted`): `+0xd4 MOVSXD RSI,EAX` · `+0xd7 TEST EAX,EAX` · `+0xd9 JNZ +0x39b` → `+0x12f JNZ +0x486` → **`+0x486 CMP ESI,0xc0 / JZ`** (버리고 끝) · **`+0x492 CMP ESI,0xc0000041 / JZ +0x3b2`** (연결 끊김 처리) · **그 밖 `+0x49e ~ +0x4a6 RpcpReportFatalError(2, 상태)`** + `INT3`. **`0xc0000001` 은 셋 어디에도 없어 치명이다.** → 에뮬레이터는 "완료 포트가 붙은 포트에 쌓인 메시지가 없는데 받기만 오는" 상황을 만들면 안 된다 (패킷 수 = 메시지 수 · 받기는 패킷당 하나).
- **통지 하나에 받기 호출은 몇 번?** `IoCompleted` 안의 `NtAlpcSendWaitReceivePort` 호출은 **`+0xc8` 하나뿐** (돌아가는 점프 `+0x481 JMP +0x98` 은 버퍼 얻기 실패 시 비상 메시지 버퍼로 같은 받기를 하러 가는 길 — 호출 수는 그대로 1). 한 메시지가 끝나면 (`MessageReceived` `+0x22c`, ret `+0x231`) 끝난다. 쌓인 메시지를 더 꺼내려고 도는 줄은 없다. 그러므로 **패킷 하나당 받기 한 번 · 메시지 하나** 이고, 그 정합성은 커널이 메시지마다 패킷 하나를 넣는 것으로 유지된다 (2-1).
- 받은 메시지의 ALPC 종류 (`+0x04` 아래 바이트) 가 **2** 여야 답으로 처리 (`IoCompleted +0x187 ~ +0x19d`), 종류 5 = 포트 닫힘, 종류 1 = 서버가 보낸 콜백 요청 (sast/130 1-3). 종류 2 의 메시지 맨 앞 `Type` 에 `0x2000` 비트가 켜져 있지 않으면 (`TEST [RBX+4],0x2000`, `+0x6bc ~ +0x6ce`) 클라이언트가 확인 응답을 되보내는 일은 없다 `[추측: 이 비트의 이름·뜻은 안 읽음]`.

---

## 3. NDR 본문 바이트 (질문 3)

### 3-0. 읽은 길 — 프록시 → 형식 문자열

`Ndr64AsyncClientCall(프록시 정보, 프로시저 번호, …)` 의 프록시 정보 (`MIDL_STUBLESS_PROXY_INFO`, 6 칸) 와 그 안의 `MIDL_SYNTAX_INFO` 두 항목 (NDR 2.0 · NDR64). 모두 `pefile` 로 읽은 PE 데이터.

| 모듈 | 프록시 정보 | 전송 문법 표 (0x50 바이트 항목 × 2) | NDR 2.0 프로시저 형식 | NDR 2.0 형식 오프셋 표 | NDR 2.0 형식 (타입) 문자열 | NDR64 오프셋 표 |
|---|---|---|---|---|---|---|
| rpcrt4 (EPM, 인터페이스 `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0, 인터페이스 구조체 rva `0xe1220`) | `0xe11e8` (StubDesc `0xe68b0`, 프로시저 형식 `0xefeb2`, 오프셋 `0xefe98`, `nCount 2`, 문법 표 `0xe7560`) | `0xe7560` (항목 0: NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860` v2.0 · 형식 `0xefeb2` · 형식 오프셋 `0xefe98` · 타입 문자열 `0xefb62`) · `0xe75b0` (항목 1: NDR64 `71710533-beba-4937-8319-b5dbef9ccc36` v1.0 · 프로시저 표 `0xe7e40`) | 프로시저 N = `0xefeb2 + 오프셋[N]`; 오프셋 `[0, 30, 60, 144, 216, 258, 288, 318, 396]` → **프로시저 8 = `0xf003e`**, 프로시저 3 = `0xeff42`, 7 = `0xefff0` | `0xefe98` | `0xefb62` | `0xe7e40` → 프로시저 8 = `0xe7870`, 3 = `0xe7d00`, 7 = `0xe7c60` |
| winnsi (인터페이스 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0, rva `0x67b0`) | `0x6000` (StubDesc `0x6040`, 프로시저 형식 `0x70c2`, 오프셋 `0x7310`, `nCount 2`, 문법 표 `0x6150`) | `0x6150` (항목 0 NDR 2.0 · 형식 `0x70c2` · 타입 `0x6eb2`) · `0x61a0` (항목 1 NDR64 · 프로시저 표 `0x6940`) | 오프셋 `[0, 66, 132, 204, 270, 336, 390, 438, 498]` → **프로시저 5 = `0x7212`**, 6 = `0x7248`, 7 = `0x7278` | `0x7310` | `0x6eb2` | `0x6940` → 프로시저 5 = `0x66e0`, 6 = `0x6590`, 7 = `0x6360` |

(sast/130 이 적은 "프로시저 형식 `0xefeb2`" 는 EPM 의 **NDR 2.0** 프로시저 문자열 시작이고 프로시저 8 은 거기서 +396 이다. winnsi `0x66e0`·`0x6590`·`0x6360` 은 **NDR64** 프로시저 5·6·7 이다.)

### 3-1. EPM 프로시저 8 (`ept_map_auth_async` `[추측: 이름 대응]`) — NDR 2.0 형식을 읽은 것

프로시저 형식 `0xf003e` 의 바이트와 뜻 `[정적]`:

| 바이트 | 뜻 |
|---|---|
| `00` | 명시적 핸들 서술이 뒤에 온다 |
| `49` | Oi 깃발: 전체 포인터 사용 · RPC 깃발 있음 · 새 초기화 루틴 |
| `00 00 00 00` · `08 00` · `50 00` | RPC 깃발 0 · **프로시저 번호 8** · **스택 크기 0x50** |
| `32 00 08 00` | 명시적 핸들 = `FC_BIND_PRIMITIVE`, 스택 오프셋 8 (비동기 상태 포인터가 스택 0, 바인딩 핸들 8) |
| `84 00` · `70 00` | 클라이언트 버퍼 상수 크기 0x84 · 서버 0x70 (크기 힌트일 뿐) |
| `c3` · `08` | Oi2 깃발: 서버 크기 필요 · 클라이언트 크기 필요 · 확장 있음 · **비동기 핸들 있음** · 파라미터 **8 개** (반환값 깃발 없음 — 상태가 보통 출력 파라미터) |
| `0a 47 01 00 01 00 00 00 00 00` | 확장 머리 10 바이트 |

파라미터 8 개 `[정적]` (속성 비트: 1 크기필요 · 2 해제필요 · 8 입력 · 0x10 출력 · 0x20 반환 · 0x40 기본형 · 0x80 값전달 · 0x100 단순참조):

| # | 이름 (인자 순서로 대응, `[추측]`) | 속성 | 스택 | 타입 오프셋 → 형식 문자열 읽은 것 | 선 위 꼴 |
|---|---|---|---|---|---|
| 1 | 객체 UUID | `0x010a` 입력 · 단순참조 | `0x10` | `0x0c` → `FC_STRUCT` 정렬 4 크기 16 {LONG, SHORT, SHORT, BYTE[8]} | 16바이트 인라인 (참조 포인터라 포인터 ID 없음) |
| 2 | 질문 탑 | `0x010b` 입력 · 단순참조 · 크기필요 | `0x18` | `0x58` → `FC_CSTRUCT` 정렬 4 고정부 4 + 끝에 `FC_CARRAY` (원소 1 바이트, 크기 = 앞 4바이트 `tower_length`, 범위 0 ~ 2000, 상관 위치 −4) | conformance u32 → `tower_length` u32 → octets |
| 3 | SID | `0x000b` 입력 · 크기필요 | `0x20` | `0xe4` → **`FC_UP` (unique 포인터)** → `RPC_SID` (`FC_CSTRUCT` 고정 8 + `FC_CARRAY` ULONG 원소, 크기 = 하위 권한 개수 바이트) | 포인터 ID u32 (NULL = 0) (+ 있을 때 SID 본문) |
| 4 | 끝점 핸들 (`entry_handle`) | `0x0118` 입력·출력 · 단순참조 | `0x28` | `0x2c` → `FC_BIND_CONTEXT` 깃발 `e8` (NULL 허용) | 컨텍스트 핸들 20바이트 (속성 u32 + GUID 16, 정렬 4) |
| 5 | `max_towers` | `0x0088` 입력 · 값전달 | `0x30` | `0x154` → `FC_RANGE` (LONG, 0 ~ 500) | u32 (0..500 범위 검사) |
| 6 | 탑 개수 (`num_towers`) | `0x2150` 출력 · 기본형 · 단순참조 | `0x38` | 기본형 `0x08` (LONG) | u32 |
| 7 | 탑 배열 (`towers`) | `0x0113` 출력 · 크기필요 · 단순참조 | `0x40` | `0x162` → `FC_BOGUS_ARRAY` 정렬 4: **conformance = 스택 `0x30` (`max_towers`) 상관 (범위 0 ~ 500)**, **variance = 스택 `0x38` 를 한 번 역참조 (`*num_towers`)**, 원소 = `FC_UP` → `0x58` (질문 탑과 같은 `twr_t`) | 배열 (max_count, offset, actual_count) + 포인터 ID 들 + 지연 본문 |
| 8 | 상태 | `0x2150` 출력 · 기본형 | `0x48` | 기본형 `0x08` | u32 |

- 같은 `rpcrt4` 에서 **공개 `ept_map` (프로시저 3)** 은 객체·탑이 `FC_FP` (전체 포인터, `[in, ptr]`) 라 **포인터 ID 4 바이트가 앞에 붙는다** — 프로시저 8 은 둘 다 참조 포인터 (속성 `단순참조`, 형식이 구조체 바로) 라 **ID 가 없다.** 공개 명세 그대로 쓰면 안 된다는 뜻 (요청서가 짚은 "인자가 다름").
- `LookupNextChunk +0x11d` 의 인자 순서 (sast/130 3-1: 비동기 상태, 바인딩, 객체 UUID, 질문 탑, SID, 끝점 핸들, `max_towers = 4`, 탑 개수, 탑 배열, 상태) 가 위 스택 오프셋 (0, 8, 0x10, 0x18, 0x20, 0x28, 0x30, 0x38, 0x40, 0x48) 과 **그대로 맞는다.**

#### 요청 스텁 (NDR 2.0) — 요청 머리 `+0x68` 부터

해석 규칙: 공개 NDR 2.0 (C706 14장) — 정렬은 "자기 크기" (u32 = 4), 구조체의 conformance 는 구조체 앞, 참조 포인터는 인라인. 칸의 존재·순서·크기는 위 형식 문자열에서 읽은 `[정적]`, 패딩 위치는 NDR 규칙 `[규칙]`.

질문 탑은 sast/130 3-1 의 **4층 70바이트 (앞 u32 길이 4 + octets 66)** 를 쓴 예. 스텁 길이 = **120** (`0x78`).

| 스텁 오프셋 | 크기 | 정렬 | 뜻 | 값 (예) | 근거 |
|---|---|---|---|---|---|
| `+0x00` | 16 | 4 | 객체 UUID (`Data1` u32 · `Data2` u16 · `Data3` u16 · `Data4` 8) | 질문에 객체가 없으면 nil (16바이트 0) | 형식 `0x0c` |
| `+0x10` | 4 | 4 | 질문 탑 구조체의 conformance (max_count) | `0x42` (= 66) | `FC_CSTRUCT` 의 conformance 가 구조체 앞으로 올라옴 `[규칙]` |
| `+0x14` | 4 | 4 | `tower_length` | `0x42` | 형식 `0x58` (sast/130 3-1 `+0x00`) |
| `+0x18` | 66 | 1 | `tower_octet_string` = 층 수 u16 + 4층 (sast/130 3-1 표의 `+0x04 ~ +0x45`) | `04 00 13 00 0d …` | 형식 `0x42` `FC_CARRAY` |
| `+0x5a` | 2 | | 다음 칸 정렬용 패딩 | `00 00` | `[규칙]` |
| `+0x5c` | 4 | 4 | SID 포인터 ID (`FC_UP`). NULL | `00 00 00 00` | 형식 `0xe4` (sast/130: 클라이언트가 SID 를 NULL 로 줌) |
| `+0x60` | 20 | 4 | 끝점 핸들 `[in,out]` (속성 u32 + GUID 16). 처음엔 NULL | 20바이트 0 | 형식 `0x2c` |
| `+0x74` | 4 | 4 | `max_towers` | `04 00 00 00` | 형식 `0x154` (범위 0..500) |
| 끝 `+0x78` | | | 스텁 길이 120 | | |

(SID 가 NULL 이 아니면 `+0x60` 앞에 SID 본문 (conformance u32 · 개정 1 · 하위 권한 수 1 · 식별자 권한 6 · 하위 권한 u32 × 수) 이 끼어든다. sast/130 에서 클라이언트는 NULL 만 준다.)

#### 응답 스텁 (NDR 2.0) — 응답 머리 `+0x40` 부터

출력 파라미터 순서 = 속성 `출력` 인 것을 형식 순서대로: 끝점 핸들 → 탑 개수 → 탑 배열 → 상태. 탑 1개 (octets 73 = sast/130 3-3 의 `tower_length 0x49`) 예, 스텁 길이 = **128** (`0x80`).

| 스텁 오프셋 | 크기 | 정렬 | 뜻 | 값 (예) | 근거 |
|---|---|---|---|---|---|
| `+0x00` | 20 | 4 | 끝점 핸들 `[in,out]`. **NULL (20바이트 0) 이면 "더 없음"**: 클라이언트가 `[조회+0x2c8]` 을 NULL 로 받아 다음 조각을 안 묻는다 (sast/130 3-2 마지막 두 줄) | 20바이트 0 | 형식 `0x2c` |
| `+0x14` | 4 | 4 | `num_towers` | `01 00 00 00` | 형식 `0x38` 기본형 |
| `+0x18` | 4 | 4 | 배열 max_count (= 클라이언트가 보낸 `max_towers`) | `04 00 00 00` | 형식 `0x162` conformance = 스택 `0x30` `[규칙]` |
| `+0x1c` | 4 | 4 | 배열 offset | `00 00 00 00` | variance 의 시작 `[규칙]` |
| `+0x20` | 4 | 4 | 배열 actual_count (= `num_towers`) | `01 00 00 00` | variance = `*num_towers` |
| `+0x24` | 4 × n | 4 | 원소 포인터 ID (`FC_UP`, NULL 이 아니면 0 이 아닌 값) | `00 00 02 00` | 형식 `0x162` 원소 |
| `+0x28` | 4 | 4 | 탑 1 의 conformance | `49 00 00 00` | 형식 `0x58` |
| `+0x2c` | 4 | 4 | 탑 1 의 `tower_length` | `49 00 00 00` | 〃 (sast/130 3-3 `+0x00`) |
| `+0x30` | 73 | 1 | 탑 1 의 octets = sast/130 3-3 의 `+0x04 ~ +0x4c` (층 수 4 … 4층 RHS `LRPC-emu\0`) | | 〃 |
| `+0x79` | 3 | | 상태 칸 정렬용 패딩 | `00 00 00` | `[규칙]` |
| `+0x7c` | 4 | 4 | 상태 | `00 00 00 00` | 형식 `0x48` 기본형 |
| 끝 `+0x80` | | | 스텁 길이 128 | | |

- **탑 개수 0**: `num_towers = 0`, 배열 `max_count 4 · offset 0 · actual_count 0`, 포인터·본문 없음, 끝점 핸들 NULL, 상태 `0x16c9a0d6` (EPT_S_NOT_REGISTERED 의 DCE 코드; 클라이언트가 `0x6d9` 로 바꿈, sast/127 `FatalLookupErrorEncountered`) 또는 0 → 스텁 `20 + 4 + 12 + 4 = 40` 바이트 `[규칙]`.
- 응답 머리 쪽: `TotalLength = 0x40 + 128 = 0xc0`, `DataLength = 0x18 + 128` (sast/130 2-3).
- `[공백]` 클라이언트의 [out] 배열 conformance 확인: "`max_count` 를 자기 `max_towers` 와 대조하는지" 는 NDR 해석기 본문을 안 읽었다. **같게 보내는 것이 안전.**

### 3-2. nsi 프로시저 5 (`RpcNsiRegisterChangeNotification`) — NDR 2.0 형식을 읽은 것

프로시저 형식 `0x7212` `[정적]`: `00 48` (명시적 핸들 · 깃발 `0x48`) · `00 00 00 00` · **프로시저 5** · **스택 0x30** · `32 00 08 00` (핸들 스택 8) · 클라이언트 상수 0x18 · 서버 0x40 · `c6` (클라이언트 크기 필요 · **반환값 있음** · 확장 · 비동기 핸들) · 파라미터 **4 개**.

| # | 속성 | 스택 | 타입 오프셋 → 읽은 것 | 선 위 꼴 (NDR 2.0) |
|---|---|---|---|---|
| 1 | `0x010b` 입력 · 단순참조 · 크기필요 | `0x10` | `0x4c` → `FC_BOGUS_STRUCT` 크기 24, 멤버 `{LONG, (메모리 패딩 4), POINTER, LONG, (패딩 4)}`, 포인터 = `FC_UP` → `0x3c` (크기 24 `{SHORT, (패딩 2), ENUM16, 비캡슐 공용체}`) | 12바이트: `u32 예약` · `u32 모듈ID 포인터 ID` · `u32 객체 번호`; 포인터가 NULL 이 아니면 뒤에 모듈 ID 본문 |
| 2 | `0x008a` 입력 · 값전달 | `0x18` | `0x34` → `FC_STRUCT` 정렬 4 크기 8 `{LONG, LONG}` | 8바이트 (compartment 두 u32) |
| 3 | `0x0110` 출력 · 단순참조 | `0x20` | `0x1b4` → `FC_RP` → `0x1b8` `FC_BIND_CONTEXT` 깃발 `a0` (엄격·직렬화), 해제 루틴 0 | 컨텍스트 핸들 20바이트 |
| 4 | `0x0070` 출력 · 반환값 · 기본형 | `0x28` | 기본형 `0x08` (LONG) | u32 |

모듈 ID (`0x3c`): 공용체 가름값 = 앞 `ENUM16` 형 칸, 1 → GUID 16 바이트, 2 → 8 바이트 (`{LONG, LONG}`).

#### 요청 스텁 (NDR 2.0) — 서버 흉내는 읽지 않아도 된다 (sast/130 4-2). 모듈 ID 가 있을 때의 꼴, 스텁 길이 **40**

| 스텁 오프셋 | 크기 | 정렬 | 뜻 | 근거 |
|---|---|---|---|---|
| `+0x00` | 4 | 4 | 예약 (메모리의 64비트 칸 중 앞 4바이트만 선 위에 나감) | 형식 `0x54` `08 40` `[규칙: 패딩 표시는 선 위에 안 나감]` |
| `+0x04` | 4 | 4 | 모듈 ID 포인터 ID (`FC_UP`) | 형식 `0x56 36` |
| `+0x08` | 4 | 4 | 객체 번호 | 형식 `0x57 08` |
| `+0x0c` | 2 | 2 | 모듈 ID `Length` (메모리 값 그대로 `0x18`) | 형식 `0x3c` |
| `+0x0e` | 2 | 2 | 모듈 ID 형 (`ENUM16`, 1 = GUID) | 〃 |
| `+0x10` | 16 | 4 | GUID (sast/127: IPv4 `eb004a00-9b1a-11d4-9123-0050047759bc` · IPv6 `eb004a01-…`) | 공용체 1 번 갈래 → `0x28` |
| `+0x20` | 8 | 4 | compartment `{u32, u32}` | 형식 `0x34` |

(해석은 NDR 규칙에 기댄 `[규칙]` — 특히 "메모리 패딩 표시 (`FC_STRUCTPAD`) 는 선 위에 안 나간다" 와 "비캡슐 공용체의 가름값은 앞 멤버를 쓴다"; 클라이언트 `ConstantClientBufferSize = 0x18` 과 앞 12 + 8 = 20 이 맞아 떨어진다.)

#### 응답 스텁 (NDR 2.0) — 스텁 길이 **24** (`0x18`)

| 스텁 오프셋 | 크기 | 정렬 | 뜻 | 값 | 근거 |
|---|---|---|---|---|---|
| `+0x00` | 20 | 4 | **컨텍스트 핸들** = 속성 u32 (0) + GUID 16. 클라이언트는 해석하지 않고 프로시저 7 에 그대로 되돌릴 뿐 | `00 00 00 00` + 새 GUID (0 이 아닌 16바이트) | 형식 `0x1b8` (sast/130 4-2) |
| `+0x14` | 4 | 4 | **반환 DWORD** (0 = 성공) | `00 00 00 00` | 파라미터 4 (반환값) |
| 끝 `+0x18` | | | 스텁 길이 24 | | |

실패 대답은 컨텍스트 핸들 **20바이트를 그대로 두고** (서버 코드는 `*컨텍스트 = 0` 으로 시작 → 핸들 NULL = 20바이트 0) 반환 DWORD 에 오류 값 (sast/130 4-2: `0x490` · `8` · `0x139f` · 그 밖) 을 쓴다.

### 3-3. NDR64 판 — 이번에 풀지 않은 것과 읽은 것

- 읽은 것 `[정적]`: 프로시저 형식 머리 (`NDR64_PROC_FORMAT` 24 바이트): EPM 프로시저 8 (`0xe7870`) = `StackSize 0x50` · `ConstantClientBufferSize 0x8c` · `ConstantServerBufferSize 0x7c` · `NumberOfParams 8`. 파라미터 16바이트 항목 8 개의 속성·스택은 NDR 2.0 판과 같다 (`0x10a`/`0x10b`/`0xb`/`0x118`/`0x88`/`0x8150`/`0x113`/`0x8150` · 스택 `0x10 … 0x48`). winnsi 프로시저 5 (`0x66e0`) = `StackSize 0x30` · 클라이언트 `0x18` · 서버 `0x44` · 파라미터 4 (속성 `0x10b`/`0x8a`/`0x110`/`0xf0`).
- **안 읽은 것 `[공백]`**: NDR64 타입 서술자 (`0xefe18` GUID, `0xe76a0` 탑, `0xe7978` SID 포인터, `0xefe40` 컨텍스트 핸들, `0xe7be0` 탑 배열, winnsi `0x7328`·`0x62d8`·`0x72f8`·`0x7324`) 를 바이트로 풀지 않았다. 규칙상 NDR 2.0 판과 달라지는 곳은 (a) 포인터 ID 가 8바이트 · (b) conformance/offset/actual_count 가 8바이트 (정렬 8) · (c) 구조체 conformance 앞 정렬 — 이라 EPM 프로시저 8 은 바이트 위치가 모두 바뀐다. nsi 프로시저 5 응답은 NDR 2.0 판과 같은 24바이트일 가능성이 크다 `[추측]`. **서버 흉내가 NDR64 를 고르려면 다음 요청에서 풀어야 한다.**

---

## 4. bind 에서 고르는 문법 (질문 4)

- **클라이언트는 고르지 않는다. 서버가 bind 답 `+0x48` 에 쓴 비트 하나를 그대로 쓴다.** `CompleteBind` (sast/130 1-3) 는 수락 마스크가 0 이면 `0x6c2`, 제안의 부분집합이 아니거나 비트가 정확히 하나 (1/2/4) 가 아니면 `0x6c0` 이라 **"둘 다 켜져 있을 때" 는 거부**된다. 제안은 비트 1 (NDR 2.0) → 2 (NDR64) → 4 순으로 OR 해서 만든다 (`Bind__4e330 +0x4e0` · `+0x713` · `+0x8a5`).
- 고른 비트는 연결 객체 표의 결합 → 슬롯 번호 (`+0x4c` 문법 1, `+0x4e` 문법 2) 로 이어져 **요청 머리 `+0x38` 에 같은 슬롯**이 들어간다. 그 슬롯이 NDR 2.0 이면 `Ndr64pClientSetupTransferSyntax` 가 고른 문법 정보의 첫 dword (`0x8a885d04` = NDR 2.0 GUID 의 `Data1`, 인자 `param_5` 가 그 정보라는 것은 `[추측: 인자 이름 짐작]`) 를 알아보고 **DCE 쪽 엔진** (`AsyncDceClient` · `NdrpClientMarshal` …) 을, 아니면 **NDR64 엔진** (`AsyncNdr64Client` · `Ndr64pClientMarshal` …) 을 고른다: `rpcrt4!Ndr64pClientSetupTransferSyntax +0x0 ~` 디컴파일의 `if (*param_5 == -0x7577a2fc)` 갈래 (EPM·winnsi 프록시 모두 문법 표에 두 항목이 있음 — 3-0).
- **서버 흉내가 하나만 골라 답할 때: 마스크 `1` (NDR 2.0) 이 안전하다.** 이유 — (i) 코드가 두 길을 다 받는다 (정적), (ii) 이번에 **형식 문자열로 풀어 낸 바이트 꼴은 NDR 2.0 판** (3 절) — NDR64 판은 안 풀었다, (iii) NDR 2.0 은 공개 명세 (C706 14장) 와 비교해 확인할 수 있다. NDR64 를 고르려면 3-3 의 `[공백]` 을 먼저 메워야 한다. **진짜 epmapper·nsisvc 가 실제로 무엇을 고르는지는 정적으로 못 정한다** (동적 1).
- 우리 코드 쪽 메모: `src/rpc/lrpc.cpp:68` 은 마스크 3 이면 NDR64 (`(mask & 2) ? 2 : (mask & 1)`) 를 고른다. 이것도 `CompleteBind` 는 통과시킨다 (유효). 다만 NDR64 스텁 바이트는 이번에 없으니, EPM·nsi 창구는 `(mask & 1) ? 1 : 2` 로 NDR 2.0 을 우선하는 편이 이번 결과로 바로 쓸 수 있다.

---

## 5. sast/130 5 절 (서버 흉내 차례) 을 고쳐 다시 적은 것 — 완료 포트 붙이기 · 패킷 넣기 · 빈 받기 대답이 들어간 판

바로잡음: sast/130 5 절은 "비동기 대화 = 보내기 + 완료 통지 + 받기 한 번 더" 까지만 적었고 **완료 통지가 오는 길 (완료 포트 붙이기 · 패킷 칸 · 일꾼 · 빈 받기)** 이 `[공백]` 이었다. 이번에 닫았다. 단계 번호는 sast/130 5 절을 따른다. **불변식: 포트에 쌓이는 메시지 수 = 완료 포트에 들어가는 패킷 수 = 받기 호출 수.**

| 단계 | 클라이언트가 하는 것 (syscall) | 서버 흉내가 할 일 | 근거 |
|---|---|---|---|
| ① 연결 | `NtAlpcConnectPort(\RPC Control\epmapper, Flags 0, InMessageAttributes = 빈 8바이트 버퍼)` | 핸들을 쓰고 0 (sast/130 ① 그대로) | sast/127 2-1 |
| ①b | `NtAlpcQueryInformation(핸들, 0, &버퍼, 0x10, NULL)` | **실패해도 되고** (= 공유 섹션 있음 → 받기 버퍼 0x1000), 성공이면 `Flags & 0x1000000` 을 0 으로 (→ 있음) 두면 같은 결과. 켜면 받기 버퍼가 0xff00 | 1-1 차례 0b |
| ①c **[새]** | **`NtAlpcSetInformation(핸들, 2, &{키, 풀 IO 완료 포트 핸들}, 0x10)`** | 길이 0x10 확인 → 완료 포트 핸들을 IO 완료 객체로 참조 (접근 2) → **그 포트 객체에 `completion_port` (객체) · `completion_key` (키) 를 보관**, 0. 실패 status 는 1-3 표 (이미 붙음 `0xc0000048` · 길이 `0xc0000004` …). 붙이기 전에 쌓인 메시지 수만큼 패킷을 바로 넣는다 (연결 직후라 0 개) | 1-3, 1-4 |
| ①d **[새]** | (풀의 첫 묶임이면) `NtSetInformationWorkerFactory(공장, 3, &1, 4)` | run125 몸 그대로 (묶음 수 칸만) | 1-1 차례 9 |
| ② 핸들 질문 | ①b 로 흡수 (sast/130 ② 와 같은 호출) | | |
| ③ epmapper bind | `NtAlpcSendWaitReceivePort(핸들, **0**, bind 0x70, 속성, NULL…)` | 보내기는 **즉시 0** (이미 있음). 서버 흉내가 답 (종류 2, LRPC `+0x28 = 1`, `+0x48` = 1 또는 2, `+0x08` PID, Context 속성 `MessageContext` = 보낸 값) 을 **그 포트의 받기 큐 끝에 넣고, 같은 동작 안에서 완료 포트에 패킷 하나**: `{KeyContext = 포트의 completion_key, ApcContext = 0, IoStatus = 0, Information = 0}` | 2-1 |
| ③b **[새]** | (유저, 에뮬레이터가 할 일 없음) 일꾼이 `NtWaitForWorkViaWorkerFactory` 로 그 패킷을 받아 → `TppAlpcpExecuteCallback` → `LrpcIoComplete` → 썽크 → `IoCompleted` | 에뮬레이터의 `NtWaitForWorkViaWorkerFactory` 는 항목 `{키, ApcContext, 상태, 정보}` 를 0x20 바이트로 쓰는 몸이 이미 있음 — 같은 `IoPacket` 에 위 4칸만 넣으면 된다 | 2-2 |
| ③c **[새]** | `IoCompleted +0xc8`: `NtAlpcSendWaitReceivePort(핸들, **0**, NULL, NULL, 버퍼, &0x1000 또는 0xff00, &{Allocated 0xfa000000}, NULL)` | **큐 머리 하나를 꺼내** 버퍼에 헤더 0x28 + 데이터, 받기 속성 (Context 속성 `MessageContext` = 요청 때 보낸 값, DataView·Handle 끔) 을 쓰고 **0**. 큐가 비어 있으면 (완료 포트가 붙은 포트): **`0xc0000001`, 아무것도 안 씀, 기다리지 않음** — 이 값은 rpcrt4 가 치명 처리하므로 이 상황이 생기면 안 된다 | 2-3, sast/130 2-2 |
| ④ `ept_map` | 요청 (`+0x28 = 0`, `+0x3c = 8`, 스텁 = **3-1 요청 표**) → 보내기는 **즉시 0** | 답 메시지 (종류 2, LRPC `+0x28 = 3`, `+0x34` = 호출 ID, 스텁 = **3-1 응답 표**) 를 큐에 + 패킷 하나. 이어서 ③b·③c | 3-1 |
| ⑤ 두 번째 ALPC 연결 | `\RPC Control\<탑 4층 끝점>` | ① 과 같은 응답. **새 핸들이므로 ①b·①c 가 다시 온다** (포트마다 따로 완료 포트에 붙음; ①d 는 풀 묶음 수가 0 → 1 일 때만이라 이미 묶여 있으면 안 옴) | 1-1 |
| ⑥ nsi bind | ③ 과 같은 꼴 (`+0x34` = `7ea70bcf…`, `+0x48 = 3`) | ③ 과 같다 | |
| ⑦ 프로시저 5 | 요청 (`+0x3c = 5`, 스텁 = 3-2 요청 표) | 답 (스텁 = **3-2 응답 표**: 컨텍스트 핸들 20 + 0) 을 큐에 + 패킷 하나 | 3-2 |
| ⑧ 프로시저 7 | 요청 (`+0x3c = 7`, 컨텍스트 핸들 20) | **큐에도 완료 포트에도 아무것도 넣지 않는다** (서버가 답하지 않음, 패킷도 없음) | sast/130 4-3 |
| ⑨ 끊기 (포트를 닫을 때) | | 서버가 닫을 때는 **종류 5 의 메시지 + 패킷 하나** (`HandleConnectionClose`). 클라이언트가 닫으면 완료 포트 객체 참조를 놓는다 `[추측: 닫는 쪽 커널은 안 읽음]` | 2-1 |

- **패킷을 넣는 시각**: 진짜 커널은 서버가 답하는 시점에 넣는다 — 클라이언트의 보내기 호출이 돌아온 뒤일 수도, 돌아오기 전일 수도 있다 (서버가 따로 도는 스레드라서). rpcrt4 는 보내기 전에 호출 상태 (bind 보냄 = 9) 를 먼저 써 두므로 `[추측: 어느 쪽이든 견딘다 — 상태 쓰는 순서는 sast/130 1-3 에서 읽음]`, 에뮬레이터는 "보내는 스레드가 syscall 에서 나간 뒤 일꾼이 받게" 하는 쪽이 결정적이다.
- **여러 메시지를 한꺼번에 줄 때**: 메시지 k 개 → 패킷 k 개 (한 번에 k 개를 한 패킷으로 합치지 않는다).

---

## 6. 지금 우리 (PE_emulator) 맞나·틀리나

(읽은 파일: `ext/syscall/syscall_layer_cpp/src/alpc/nt_alpc_create_port.cpp` · `src/threadpool/nt_io_completion.cpp` · `src/threadpool/nt_wait_for_work_via_worker_factory.cpp` · `src/rpc/lrpc.cpp`. 읽기만. 줄 번호는 읽은 시점 파일 기준이고 요청서와 같은 자리 근처.)

| 곳 | 지금 코드 | 맞나·틀리나 |
|---|---|---|
| `nt_alpc_create_port.cpp:581 ~ :602` `NtAlpcSetInformation` 공통 앞부분 | 핸들 NULL / 입력 NULL (종류 7·10 제외) → `invalid`; 길이 > 0x48 → `0xc0000004`; 의사 핸들 `0xc0000024`; 종류 다름 `0xc0000024`; 접근 1 없음 `0xc0000022` | **맞다.** 커널 `+0x5f ~ +0x111` (1-3 표 1~4행) 과 같은 차례·값. |
| 같은 함수, 종류 5 | 길이 0x10 이면 성공, 아니면 `invalid` | **맞다.** `nt!NtAlpcSetInformation +0x472 ~ +0x482` (`길이 == 0x10 → 0`, 아니면 `0xc000000d`). |
| 같은 함수, **종류 2 (`:602` `stop`)** | 종류 5 밖이면 `stop("alpc_set_information_class_unsupported")` | **틀림 (빠짐) — 이번 결과로 채울 수 있다.** 종류 2 갈래: ① 길이 ≠ 0x10 → **`0xc0000004`** (종류 5 의 `invalid` 와 다르다), ② 입력 `{키 @0, 완료 포트 핸들 @8}` 읽기, ③ 핸들 0 → `0xc000000d`, ④ **포트에 이미 완료 포트가 있으면 `0xc0000048`** (핸들 참조보다 앞), ⑤ 완료 목록이 있으면 `0xc00000bb`, ⑥ 핸들을 IO 완료 객체로 참조 (**접근 2**, 실패 status 그대로), ⑦ 포트 객체에 `completion_port` · `completion_key` 두 칸 보관 → 0. (`AlpcPort` 에 두 칸이 없으면 늘려야 한다.) |
| `:612 ~ :626` `NtAlpcSendWaitReceivePort` 비동기 보내기 | "받기 칸 0 이면 곧바로 0" | **맞다.** 커널은 받기가 없으면 보내기만 하고 돌아온다 (`NtAlpcSendWaitReceivePort +0x112 ~ +0x11d`: 받기 메시지 포인터가 0 이면 `AlpcpReceiveMessage` 를 건너뜀). 단 **답을 큐에 넣을 때 패킷 하나를 같이 넣어야 한다** (빠짐, 5 절 ③). |
| 같은 함수, **받기만 (`a[2] == 0 && a[4] != 0`)** | "쌓인 답 넘김" | **절반 맞다.** 맞는 것: 큐 머리 하나를 꺼내 0. **추가로 필요한 것**: (i) 큐가 비면 완료 포트가 붙은 포트는 **`0xc0000001` 즉시, 버퍼·길이·속성 안 씀** (기다리지 않음 — 타임아웃 NULL 이어도); 완료 포트 없는 포트만 기다림. (ii) 포트가 닫힘 `0xc0000700` · 거절 `0xc0000041`. (iii) 받기 버퍼가 메시지보다 작으면 `0xc0000023` + 길이 칸에 필요한 크기, 메시지는 큐에 남김. (iv) Flags `0x1000000` 은 이 길에서 안 온다 (썽크가 NULL 을 넘김). |
| `src/threadpool/nt_io_completion.cpp` (패킷 넣기) | `NtSetIoCompletion(Ex)` 로 넣은 패킷만 큐에 있다고 적혀 있음. 패킷 = 키·APC 문맥·상태·정보 | **틀을 그대로 쓸 수 있다 (맞는 틀).** ALPC 패킷도 같은 큐에 **`{키 = completion_key, apc = 0, status = 0, information = 0}`** 한 줄을 푸시하면 된다 (`IoCompletion` 에 넣는 길만 새로 필요). `NtWaitForWorkViaWorkerFactory` 의 출력 `{키, apc, 상태, 정보}` × 0x20 (`nt_wait_for_work_via_worker_factory.cpp:135 ~ :139`) 은 커널 stride 와 맞다 (유저 `TppPrepareDirectParams` 가 0x20 → 0x38 로 펴는 것도 그 stride 를 전제로 한다). `NtQueryIoCompletion` 이 세는 큐 길이에 ALPC 패킷도 들어가는 것도 맞다. |
| `src/rpc/lrpc.cpp:108` `lrpc_shape_error` | 동기 (`Flags & 0x20000`, 받기 버퍼 = 보낸 버퍼) 만 지원, 비동기는 `svc_async_unsupported` | **winnsi·epmapper 길은 이번 결과가 필요한 자리.** 비동기 갈래를 열려면 5 절 ③ ③b ③c 가 들어가야 한다. |
| `src/rpc/lrpc.cpp:68` 문법 선택 | 마스크가 3 이면 NDR64 | `CompleteBind` 엔 **유효**. 다만 NDR64 바이트는 이번에 안 풀었음 — **EPM·nsi 는 `(mask & 1) ? 1 : 2` 로 NDR 2.0 우선을 권한다** (4 절). |
| 새 `src/rpc/` 창구 — epmapper `ept_map` | (미구현) | 3-1 응답 표 (스텁 128 / 탑 0 개는 40) 그대로. 요청의 `+0x3c == 8` 확인, `+0x34` 호출 ID 를 응답 `+0x34` 에 되돌림 (이미 있음 — `lrpc_handle_request`). 끝점 핸들은 항상 NULL 로 돌려주면 한 번에 끝난다. |
| 새 `src/rpc/` 창구 — nsi 프로시저 5 | (미구현) | 3-2 응답 표 (스텁 24: 컨텍스트 핸들 20 + 0). 요청은 읽지 않아도 된다. 프로시저 7 은 답 안 함 (5 절 ⑧). |

---

## 동적으로 확인할 것 (정적으로 못 닫은 것 — Q231 ④·⑤ 와 합치기 좋게)

유저 모드 (test.exe) 는 `rpcrt4`·`ntdll` 오프셋 = 이 문서의 `ret_off`. 멈춤점은 함수 입구가 아니라 돌아온 자리 또는 입구 + 조건.

| # | 볼 것 | 자리 | 레지스터·메모리 |
|---|---|---|---|
| 1 | **진짜 서버가 bind 답에서 고르는 문법** (1 인가 2 인가) · 비동기 bind 인지 | `rpcrt4!LRPC_CASSOCIATION::CompleteBind` 입구 (`rdx` = 메시지) — sast/130 동적 2 와 같은 줄 | `db @rdx L0x70`: `+0x48` (수락 마스크 1/2), `+0x28`, `+0x08`. epmapper·nsi 둘 다 |
| 2 | **완료 포트 붙이기 인자 진짜 값** | `ntdll!TppAllocAlpcCompletion +0x1d6` (call 직전 `+0x1d1`) 또는 `ntdll!NtAlpcSetInformation` 입구 + 반환 주소 = `TppAllocAlpcCompletion+0x1d6` | `rcx` = 포트 핸들, `edx` = **2**, `r9d` = **0x10**, `dq @r8 L2` = {TP_ALPC 포인터, 풀 IO 완료 포트 핸들}, 반환 `eax` = 0. 풀 `[rdi+…]` 와 대조 (`[TP_ALPC+0xd8]` = 풀, `[풀+0x40]` = 핸들) |
| 3 | **첫 묶임에서 `NtSetInformationWorkerFactory(…, 3, …)` 가 가나** | `ntdll!TpAdjustBindingCount +0x40` (ret) | `edx == 3`, `[rsp+0x38] == 1` (또는 `0xffffffff`) 여부 |
| 4 | **패킷의 진짜 값** | `ntdll!TppAlpcpExecuteCallback` 입구, 조건 `poi(@rdx+0x98) == rpcrt4!LrpcIoComplete` | `rdx` = TP_ALPC, **`r8` = ApcContext (0 일 것)**, `dq @r9 L2` = {상태 0, 정보 0}. 커널 쪽은 `nt!AlpcpQueueIoCompletionPort` 입구 조건 `rcx` = 해당 포트: `dl` (0 일 것), `[rcx+0x28]` (키), `[rcx+0x20]` |
| 5 | **빈 받기가 실제로 오나** (오면 안 됨) | `nt!AlpcpReceiveMessagePort +0x1ba` (`mov edi,0xc0000001`) | 걸리면 "패킷 수 ≠ 메시지 수" 인 경우 — 호출한 쪽 `rpcrt4!IoCompleted +0xcf` 의 `rsi` |
| 6 | **패킷이 보내기 호출 안에서 먼저 들어가나 / 나중인가** | `ntdll!NtAlpcSendWaitReceivePort` 반환 직후 (`AlpcSend +0x12f`, `Bind__4e330 +0x62d`) 와 `TppAlpcpExecuteCallback` 입구의 시각 차 | 로그 순서 |
| 7 | **`NtAlpcQueryInformation` 결과 `Flags`** (`0x1000000` 켜졌나 → 받기 버퍼 0xff00) | `rpcrt4!IsPortWithSharedSection +0x2c` (ret) | `[rsp+0x30]` |
| 8 | **EPM 프로시저 8 · nsi 프로시저 5 의 진짜 스텁 바이트** (3-1·3-2 표 대조, NDR64 인지) | `rpcrt4!DoSendReceive`/`AlpcSend` (요청, `+0x68` 부터) · `IoCompleted +0xcf` (응답, `+0x40` 부터) — sast/130 동적 3·4·5 | 요청 스텁 길이 (120 / 40), 응답 스텁 길이 (128 / 24), `+0x38` 슬롯의 문법 |
| 9 | 클라이언트 [out] 배열 conformance 대조 (3-1 `[공백]`) | 응답 `+0x18` 을 `max_towers` 와 다르게 준 실험 | `RPC_X_INVALID_BOUND` 류가 나는지 |

## 다음 요청 후보

1. **NDR64 형식 풀기** — EPM 프로시저 8 (`0xe7870`) · nsi 프로시저 5 (`0x66e0`) 의 타입 서술자 (3-3 `[공백]`). 서버 흉내가 NDR64 를 고를 때만 필요.
2. **서버 쪽 답 보내기 (`AlpcpSendMessage` → `AlpcpDispatchReplyToPort`)**: 답 메시지의 `Type`·`MessageId`·Context 속성 (`AlpcpExposeAttributes`, `AlpcpExposeCapturedContextAttribute`) 을 커널이 어떻게 만드나 — 지금은 sast/130 2-2 의 "보낸 값 그대로" 만 있다.
3. **`NtWaitForWorkViaWorkerFactory` 커널 쪽 + `TppPoolpReferenceGlobalPool`** 의 "기본 풀 만들기" 와 이번 1-1 차례 7 의 접점 (run125 이후 바뀐 것 대조).
4. `AlpcpSignalAndWait` (`AlpcpQueueIoCompletion` 의 또 다른 부른 쪽) — ApcContext 에 −1·0 이 아닌 값이 들어가는 길이 있나 (서버 쪽 `ServerStartingToListen` 의 문맥 썽크와 함께).
5. sast/130 후보 4·6 (`nsi.dll!NsiRegisterChangeNotificationEx`, `RegisterNsiSvcRestartNotification`) 이월.

## 뽑은 것

- ntdll (depth 0, `^이름$`, 로그 `ms/ghidra/ntdll.export.sast132{a,b,c}.stdout`): `TpAllocAlpcCompletionEx` `TppAllocAlpcCompletion` `TppAlpcpExecuteCallback` `TppWorkerThread` `TppAlpcpValidateAlpc` `TpReleaseAlpcCompletion` `NtAlpcSetInformation` `NtWaitForWorkViaWorkerFactory` (썽크) · `TpAdjustBindingCount` `TppCleanupGroupAddMember` `TppPrepareDirectParams` (1 → 2 단계 아래 일이 일어나서 더함, 요청 범위를 넓힌 것: 풀 묶음 수·일꾼이 패킷을 집는 줄). `TppAlpcpFree`·`TppCleanupGroupMemberInitialize`·`TpAllocPoolInternal` 은 이미 있던 것을 읽음.
- ntoskrnl (depth 0, 로그 `ms/ghidra/ntoskrnl.export.sast132{a,b,c,d}.stdout`): `NtAlpcSetInformation` `AlpcpAssociateIoCompletionPort` `AlpcpQueueIoCompletion` `AlpcpQueueIoCompletionPort` `AlpcpCompleteDispatchMessage` `AlpcpDispatchNewMessage` `AlpcpReceiveMessage` `AlpcpReceiveMessagePort` `NtAlpcSendWaitReceivePort` · 더함: `AlpcpSignal` `AlpcpSignalPortAndUnlock` (미룬 패킷 넣기) · `AlpcpAllocateCompletionPacketLookaside` `AlpcpLookasidePacketCallbackRoutine` (쌈) · `AlpcpReceiveDirectMessagePort` `AlpcpLookupMessage` (직접 받기 오류).
- rpcrt4 (depth 0, 로그 `ms/ghidra/rpcrt4.export.sast132{a,b}.stdout`): `InitializeCallbackEnvironmentIfNecessary` `Ndr64pClientSetupTransferSyntax` `I_RpcNegotiateTransferSyntax` · 이미 있던 `CreateAlpc` `EnableAsync` `LrpcIoComplete` `IoCompleted` `IsPortWithSharedSection` `LrpcCAssociationConnectIoHandler` 읽음.
- PE 데이터 읽기 (`pefile`, 파일로 안 남김): rpcrt4 프록시 `0xe11e8` · 인터페이스 `0xe1220` · 문법 표 `0xe7560` · NDR 2.0 프로시저 3·7·8 · 타입 문자열 `0xefb62` · NDR64 프로시저 8 머리 `0xe7870`; winnsi 프록시 `0x6000` · 문법 표 `0x6150` · NDR 2.0 프로시저 5·6·7 · 타입 문자열 `0x6eb2` · NDR64 프로시저 5 머리 `0x66e0`; `rpcrt4!0x96a70` 썽크 바이트.
- PDB 구조체 칸 (`pdb_struct.py ntoskrnl.exe`): `_ALPC_PORT` `_ALPC_COMPLETION_PACKET_LOOKASIDE` `_ALPC_COMPLETION_PACKET_LOOKASIDE_ENTRY` `_IO_MINI_COMPLETION_PACKET_USER` `_ALPC_DISPATCH_CONTEXT` `_KALPC_MESSAGE`; `_ALPC_PORT.u1` 비트 (`llvm-pdbutil`): 4 = `ConnectionRefused` · 6 = `Closed` · 9 = `Waitable` · 16 = `EnableCompletionList`.

## 판정

**분석성공 (정적으로 닫을 수 있는 만큼).** 질문 1 (붙이기 syscall 차례·입력 칸·커널 검사·실패 status·포트에 남는 칸), 질문 2 (패킷 4칸과 어디서 오나·메시지당 하나·미룬 경로, 일꾼 → 콜백 인자 4개, 재개 syscall 없음, 빈 받기 `0xc0000001` 과 rpcrt4 의 치명 처리, 통지당 받기 한 번), 질문 3 (NDR 2.0 판 EPM 프로시저 8 요청 120 / 응답 128 바이트 · nsi 프로시저 5 응답 24 바이트, 요청 40 바이트), 질문 4 (서버가 고른 비트 그대로 · NDR 2.0 권장) 을 코드 줄과 형식 문자열 근거로 답했다. sast/130 5 절을 완료 포트·패킷·빈 받기가 들어간 판으로 고쳐 적었다.

**`[공백]` 으로 남긴 것**: ① **NDR64 판 바이트** (3-3) — 서버가 NDR64 를 고를 때만 필요; ② **진짜 서버가 고르는 문법** (동적 1); ③ 패킷이 보내기 호출 안에서 먼저 들어가는지 나중인지 (동적 6); ④ `AlpcpSignalAndWait` 가 ApcContext 에 넣는 값 · 서버 쪽 썽크 (다음 요청 4); ⑤ NDR 클라이언트 스텁의 [out] 배열 conformance 대조 (동적 9). 해석이 공개 NDR 규칙에 기댄 칸 (패딩 위치·`FC_STRUCTPAD` 처리·비캡슐 공용체 가름값) 은 표에 `[규칙]` 으로 적었다.

**고친 것 (sast/130 의 `[공백]` 닫음)**: 2-4 의 "완료 통지가 `LrpcIoComplete` 까지 오는 길" 과 "받기 한 번 더" 의 나머지 반쪽, 5 절의 ①~④ 에 빠져 있던 완료 포트 붙이기·패킷·빈 받기.
