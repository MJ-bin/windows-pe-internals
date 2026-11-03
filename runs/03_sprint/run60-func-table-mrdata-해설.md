# run60 func-table-mrdata — 해설

> 이 글은 [run60 문서](run60-func-table-mrdata.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q111** — PE_emulator 요청 5: 함수표·`.mrdata` 보호. `LdrpInitializeProcess +0x3e6 ~ +0x4a1` 에서 "공사 중 빈 함수" 로 둔 다섯의 본문·쓰는 칸·syscall (`_workspace/plan.md`, 이번에 **Q111 끝**)
  - 다음 항목: **Q104 VM 단계** → Q78d4 → Q78e → …
  - 남은 질문이 간 곳: 선택 동적 1 ~ 6 → **Q104 VM 단계** (Q111 의 VM 몫이 원래 여기 얹혀 있었다), 새 공백 ①② 와 질문 밖 메모 → 새 **Q115** (맨 뒤). run35 의 **Q67 (a)** 는 이번에 답이 이어졌다 (2.12).
- 근거:
  - run 문서 [`run60-func-table-mrdata.md`](run60-func-table-mrdata.md) (가장 중요)
  - 요청서 [`_workspace/sast/55_func-table-mrdata.md`](../../_workspace/sast/55_func-table-mrdata.md)
  - 결과서 [`_workspace/sast/55_func-table-mrdata_result.md`](../../_workspace/sast/55_func-table-mrdata_result.md) (판정: **분석성공**, PDB 일치 — 부분 답 둘: 넣기·되읽기를 거치는 **이유**, 속성 `0x22` 공개 이름)
  - flow.md 변경: 기준 · 2.1 (`PspBuildCreateProcessContext`·`PspCopyAndFixupParameters`) · 2.4 (`LdrpInitializeProcess` 트리, 새 절 **"묶음 3 뒤"**, `LdrpInitializeNtdllDataTableEntry` 절) · 4장 · 5장. **그림은 안 바뀜**
  - baggage.md 변경: 2장 숫자 · 3.8 새 줄 4 · 3.9 새 줄 6 · 새 **3.9.1** (역함수 표 안쪽 칸) · 4·5·6장 한 줄씩 · 새 **7.20**
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - `[관찰]` 로 적힌 값은 run44 사진 (P1a, K1 보충) 로그를 **다시 읽은 것** 이다. 새 실행이 아니다.
  - "파일 처음 값" (`CurrentSize 1`·`MaximumSize 0x200`·`LdrpMrdataUnprotected 1`) 은 결과서가 ntdll **PE 파일 바이트**를 읽은 것이다 `[정적]`.
- 결과서와 run 문서가 다른 곳은 **2.12 (라)** 에 모았다. 이 글은 **run 문서 숫자**를 따른다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 꼬리 점프 (`⇒`) → [run17 해설 2.4](../02_sprint/run17-ldr-init-entry-해설.md#24-꼬리-점프--콜스택에서-함수-하나가-지워지는-원리)
- 한 주소에 이름 여럿 (메모리 옮기기 `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove` 가 한 주소) → [run9 해설](../02_sprint/run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭)
- 빠른 실패 `INT 0x29` (그 자리에서 프로세스를 멈추는 안전장치) → [run24 해설](../02_sprint/run24-work-queue-해설.md) 2.3
- 묶음 3 (`+0x292 ~ +0x4a1`) 과 **역함수 표 = 동네 지번 색인** → [run18 해설 2.4](../02_sprint/run18-proc-init-setup-해설.md) "묶음 하나씩"
- ntdll 항목의 `InExceptionTable` 비트 9 → [run35 해설 2.5](../02_sprint/run35-ntdll-entry-해설.md)
- `LdrInitState` 팻말 (0 → 1 → 2 → 3) → [run19 해설 2.12](../02_sprint/run19-proc-init-modules-해설.md)
- `.mrdata` = **유리 덮개를 씌운 칠판** → [run43 해설 2.9](run43-baggage-checklist-해설.md) "구역 새 말 둘"
- `.mrdata` 칸을 쓰려고 `NtProtectVirtualMemory` 로 덮개를 열고 닫는 꼴 (쿠키) → [run59 해설 2.6](run59-init-leftovers-해설.md)
- 풀림 정보 (함수마다 "스택을 거꾸로 되짚는 법" 쪽지) → [run59 해설 2.9](run59-init-leftovers-해설.md)
- 게으른 만들기 (처음 필요할 때 만든다) → [run51 해설 2.8](run51-eprocess-later-해설.md)
- `LdrSystemDllInitBlock` = 관리실 **업무 수첩**, 완화 옵션 = 가게 **안전 규칙** → [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- CFG (간접 호출 대상이 허락된 주소인지 확인하는 보호 장치 `[추측]`) → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 묶음 4
- NT 힙 / 세그먼트 힙 = 창고 **설계도 두 종류** → [run46 해설 2.4](run46-process-heap-해설.md)
- 사진 K1 · P1a · P1b … → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

이번에 처음 나오는 말 (함수표 `FunctionTable`, 정렬 구간, `Epoch`, 읽기 전용 보호, 중첩 카운터, CFG 내보내기 억제, 프로세스 만들기 속성 …) 은 **본문에서 처음 나오는 자리**에서 한 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**, 프로세스 = **가게**, 모듈 (exe·DLL) = 가게 땅 위 **건물**.
- 시스템 콜 = 관리사무소 **신청 창구**.
- ntdll 전역 = 관리실 **칠판**. `.mrdata` 칠판엔 **유리 덮개**가 씌워져 있다.
- 역함수 표 = "**이 번지는 어느 건물?**" 을 찾는 **동네 지번 색인** (run18·run35 해설 그대로).
- 이번에 새로 붙이는 그림 하나: 유리 덮개 옆에 **"지금 덮개 열어 달라고 한 사람 수" 를 적는 작은 숫자판** 이 있다 (2.6).
  - run 문서·flow.md 는 같은 것을 "게시판 · 유리문 · 지금 n명 고치는 중" 이라고 그렸다. 이 글은 앞 해설과 맞춰 **칠판 · 유리 덮개 · 숫자판** 으로 쓴다. 같은 뜻이다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| `+0x415` 넣기 → `+0x470` 빼기는 왜 하나 | **ntdll 을 역함수 표 칸 0 에 놓는 한 묶음**이다: 넣기 (칸 1) → 되읽기 → 칸 0 직접 쓰기 → 빼기. 순효과 칸 0 = ntdll, `CurrentSize 1`, `Epoch +4` `[정적]`. "왜 이 길로" 는 `[추측]` |
| 표의 정체 | `LdrpInvertedFunctionTables` = PDB `_INVERTED_FUNCTION_TABLE_USER_MODE`, **`0x3010` B** (머리 `0x10` + 칸 512 × `0x18`) `[정적]` |
| `LdrProtectMrdata` 는 무엇을 하나 | 인자 0 = 열기, ≠ 0 = 닫기. **중첩 카운터** `LdrpMrdataUnprotected` (파일 값 **1**) 를 ±1, **0 ↔ 1 을 넘을 때만** `NtProtectVirtualMemory` `[정적]` |
| baggage 3.9 "쓴 자리 모름" 셋 | `LdrpMrdataUnprotected` = `LdrProtectMrdata` 몫. `LdrpMrdataSize`·`LdrpMrdataBase` = 그 아래 `LdrpLocateMrdata` (`+0x52`·`+0x59`) `[정적]` |
| 이 구간의 syscall | `.mrdata` 쪽은 **`+0x488` 닫기 하나** (`NtProtectVirtualMemory` `0x50`, `PAGE_READONLY`). 되읽기에서 `NtQueryVirtualMemory` `0x23` 하나 `[정적]` |
| CFG 내보내기 억제 확인 | test.exe 는 **0** (`LdrSystemDllInitBlock +0xb8 = 0`) → `+0x49d` 안 탐 `[정적]` + `[관찰]` |
| 덤 `.data 0x1ce268` | 프로세스 만들기 속성 `0xa0022` 값 (0/1) — **세그먼트 힙 요청** 칸 `[정적]` (이름은 `[추측]`) |

이 글에서 붙잡아 둘 세 줄:

> **(1) 이 구간은 처음부터 끝까지 ntdll 자기 이야기다.** `RSI` = ntdll 시작이다. exe 는 한참 뒤 `LdrpProcessMappedModule +0x177` 에서 색인에 들어온다.
>
> **(2) 칸 0 은 색인의 정렬 구간 밖, "관리실 전용 칸" 이다.** 그래서 ntdll 을 정렬 구간 (칸 1) 에 넣었다가, 칸 0 에 옮겨 적고, 정렬 구간에서는 뺀다.
>
> **(3) `.mrdata` 덮개는 처음에 열린 채로, 숫자판 1 에서 시작한다.** 열기 셋 · 닫기 넷이 지나가며 숫자판이 `+0x488` 에서 처음 0 이 되고, 그때 처음으로 창구에 "덮개 닫아 주세요 (`PAGE_READONLY`)" 를 신청한다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

라벨은 ret 자리 (콜스택 숫자) 다.

```
cmd.exe 의 부탁 → 커널 (부모 cmd 스레드, flow.md 2.1)
 NtCreateUserProcess
   └─ … PspBuildCreateProcessContext  (+0x15a4 PspProcessUserCopyAttribute)   ← 덤: 속성 0xa0022 의 커널 쪽
 → 첫 스레드 출발

test.exe 첫 스레드 (flow.md 2.4)
 LdrInitializeThunk +0xe → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize
   └─ +0x16c LdrpInitializeProcess
        ├─ 묶음 1 · 2, 그리고 묶음 3 앞쪽 (+0x292 ~ +0x3d5: exe 헤더, 보안 프로세스 검사, LdrpHashTable 비우기)
        ├─ 묶음 3 뒤 (+0x3d5 ~ +0x4a4)                  ← 이번 바퀴 (전부 ntdll 자기 이야기)
        │    ├─ +0x3de  LdrpHandleUserCopyAttributeList      ← 덤
        │    ├─ +0x41a  RtlInsertInvertedFunctionTable  ┐
        │    ├─ +0x43f  RtlpxLookupFunctionTable        │ 역함수 표 칸 0 에 ntdll 놓기
        │    ├─ (+0x44e ~ +0x467 칸 0 직접 쓰기)          │
        │    ├─ +0x475  RtlRemoveInvertedFunctionTable  ┘
        │    ├─ +0x446 / +0x481 / +0x48d  LdrProtectMrdata  ← .mrdata 덮개
        │    └─ +0x492  LdrControlFlowGuardEnforcedWithExportSuppression  ← CFG 확인
        ├─ 묶음 4 ~ … (+0xda6 / +0xdc9 에서 .mrdata 덮개를 한 번 더 열고 닫음)
        └─ … LdrpProcessMappedModule +0x177 (exe 를 색인 칸 1 에)  (run35)
 → (CRT) → main
```

- PE_emulator 가 `+0x3e6 ~ +0x4a1` 이라 부른 구간과 같은 곳이다. 이 글이 `+0x4a4` 까지 적는 건, CFG 확인 뒤 점프 목적지가 `+0x4a4` 라서다 (`+0x494 JNZ → +0x4a4`, run 문서 4절).

### 왜 이 질문이 지금 왔나

- `../PE_emulator` 다음 바퀴 (로드맵 2-B③b) 가 이 구간이다. 거기서 다섯 함수를 **"공사 중" 빈 함수** 로 두고 있다 (요청서 "왜").
  - `RtlInsertInvertedFunctionTable` · `RtlpxLookupFunctionTable` · `RtlRemoveInvertedFunctionTable` · `LdrProtectMrdata` · `LdrControlFlowGuardEnforcedWithExportSuppression`
- PE_emulator 는 진짜와 **같은 이름·호출관계** 로 짓는다. 그래서 "무엇을 쓰고, syscall 이 **어느 함수 안에** 있나" 까지 맞아야 한다.
- 짐 지도 쪽에도 구멍이 있었다: baggage 3.9 `.mrdata` 의 `LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataUnprotected` 가 **"쓴 자리 모름"** 이었다.
- 사용자 결정 (2026-11-02): Q109 다음, Q104 VM 단계 앞.

### 무엇이 궁금했나

요청서 질문을 다섯 + 덤 하나로:

1. **(a) 무엇을 쓰나** — 역함수 표의 머리·칸 이름과 크기, 그리고 3.9 "쓴 자리 모름" 셋이 `LdrProtectMrdata` 몫인가. 인자 꼴과 중첩 셈이 있나.
2. **(b) syscall 을 부르나** — 특히 `LdrProtectMrdata` → `NtProtectVirtualMemory`. 하위 `Rtlp…Entry` 까지 줄기 끝까지.
3. **(c) 왜 넣고 빼나** — `+0x415` 넣기 뒤 `+0x470` 빼기, 그 사이 `+0x44e ~ +0x467` 직접 쓰기가 칸 0 에 무엇을 놓나. 끝난 뒤 기대 값.
4. **(d) CFG 확인 함수가 test.exe 에서 무엇을 돌려주나** — 무엇을 읽어 정하나.
5. **(e) 1339B 짜리 룩업** — `+0x43a` 가 부를 때 타는 갈래만.
6. **덤** — `LdrpHandleUserCopyAttributeList +0x82` 가 쓰는 이름 없는 `.data 0x1ce268` 과 속성 번호 `0xa0022`.

### 바퀴 전에 알던 것

- `[정적]` (run18, sast/13) 묶음 3 에 `+0x415` (ret `+0x41a`) `RtlInsertInvertedFunctionTable(ntdll 시작, SizeOfImage)`. 그 뒤 "`.mrdata` 쓰기 열고 닫기, 중요 thunk 잡기, CFG 확인" 은 **이름만**.
- `[정적]` (run35) ntdll 항목은 `InExceptionTable` (비트 9) 을 미리 켜서 `LdrpProcessMappedModule +0x15e` 에서 `+0x177` Insert 를 건너뛴다.
  - "ntdll 은 다른 곳에서 이미 등록됐다" 는 `[추측]` 이었고, 그 자리 후보 둘 (`+0x45e` 근처 쓰기 / `+0x41a` Insert) 을 **Q67 (a)** 로 넘겼다.
- `[정적]` (run45) `LdrInitializeMrdata` 는 `LdrpMrdataBase`·`Size`·`Heap`·`HeapUnprotected`·`Unprotected` 다섯을 **안 쓴다**. 그래서 주인이 누군지 다시 비었다.
- `[정적]` (run59) `__security_cookie` (`.mrdata`) 를 쓸 땐 `LdrInitSecurityCookie` 가 `NtProtectVirtualMemory` 로 덮개를 직접 열고 닫는다. `LdrProtectMrdata` 도 같은 꼴일지는 **몰랐다** (run59 해설 3절).
- `[관찰]` (run44 사진) `LdrpMrdataSize`·`LdrpMrdataBase`·`LdrpInvertedFunctionTables` 는 `≤P1a` 에 값이 있고, `LdrpMrdataHeap`·`HeapUnprotected`·`LdrpSuppressedExportOverrideListPtr` 는 끝까지 0. `LdrpMrdataUnprotected` 는 `끝까지 0*` 로 분류돼 있었다.
- PE_emulator 가 파일에서 읽어 둔 값: 표 `CurrentSize 1`·`MaximumSize 0x200`·칸 0 모두 0 (요청서 — 그때는 아직 확인 안 함).

---

## 2. 찾은 것

### 2.1 한눈에 — 묶음 3 뒤 (대표만)

run 문서 "호출 관계" 를 줄였다. 괄호 숫자는 숫자판 (카운터) 값, 모두 `[정적]`.

```
LdrpInitializeProcess                    RSI = ntdll 시작 (+0x2d MOV RSI,RDX)
 ├─ +0x3de  LdrpHandleUserCopyAttributeList        (보안 프로세스 아닐 때) test.exe 목록 0 → 안 씀
 ├─ +0x405  RtlImageNtHeaderEx                      → +0x40d EBX = ntdll SizeOfImage
 ├─ +0x41a  RtlInsertInvertedFunctionTable          ntdll → 칸 1   (안에서 열기 1→2 · 닫기 2→1)
 ├─ +0x43f  RtlpxLookupFunctionTable                LdrInitState 0 → 느린 길 → +0x2d5 NtQueryVirtualMemory (클래스 6)
 ├─ +0x446  LdrProtectMrdata(0)                     열기 (1→2)
 │  +0x44e · +0x45e · +0x467                        칸 0 = ntdll (직접 쓰기)
 ├─ +0x475  RtlRemoveInvertedFunctionTable          칸 1 을 정렬 구간에서 뺌   (안에서 열기 2→3 · 닫기 3→2)
 ├─ +0x481  LdrProtectMrdata(1)                     닫기 (2→1)
 ├─ +0x486  LdrpCaptureCriticalThunks               (이름만)
 ├─ +0x48d  LdrProtectMrdata(1)                     닫기 (1→0) → +0x72 LdrpChangeMrdataProtection(2)
 │                                                    ├─ +0x19 LdrpLocateMrdata   (LdrpMrdataSize·Base 처음 씀)
 │                                                    └─ +0x56 NtProtectVirtualMemory (PAGE_READONLY)
 └─ +0x492  LdrControlFlowGuardEnforcedWithExportSuppression   → 0 → +0x494 JNZ +0x4a4
```

- syscall 은 딱 둘이다: 되읽기 안 `NtQueryVirtualMemory` `0x23`, 마지막 닫기 안 `NtProtectVirtualMemory` `0x50` `[정적]`.
  - `LdrpCaptureCriticalThunks` 안에도 `NtQueryVirtualMemory` (call `+0x67`, 클래스 4) 가 있지만, 이 함수는 이번엔 이름만 봤다.
- 칸 표를 고치는 함수 다섯 (`Insert`·`Remove`·`Rtlx…Remove`·두 `Entry`) 은 **syscall 이 없다** `[정적]`.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — `LdrpInitializeProcess` call `+0x488` / ret `+0x48d` → `LdrProtectMrdata(1)`

- **누가**: `LdrpInitializeProcess` (관리실 직원이 가게 전체를 세팅하는 큰 함수).
- **어디서**: `+0x488` 에서 부르고, 돌아올 자리는 `+0x48d`. 콜스택엔 `LdrpInitializeProcess+0x48d` 로 보일 자리다.
- **누구를**: `LdrProtectMrdata`. 인자 `ECX` 는 바로 앞 `+0x486 MOV ECX,EBX` 로 넣은 **1** (그 전에 `+0x475 MOV EBX,1`).
- **무엇을 위해**: `.mrdata` 덮개를 **닫는다**. 이번엔 숫자판이 1 → 0 이 되므로, 안에서 정말로 창구에 "읽기 전용으로" 를 신청한다.
- 모두 `[정적]` (run 문서 4절 트리, `.asm` 줄).

#### 한 줄 읽기 ② — `LdrpChangeMrdataProtection` call `+0x51` / ret `+0x56` → `NtProtectVirtualMemory`

- **누가**: `LdrpChangeMrdataProtection` — `LdrProtectMrdata` 가 숫자판이 0 ↔ 1 을 넘을 때만 부르는 조수.
- **어디서**: `+0x51` (ret `+0x56`).
- **누구를**: `NtProtectVirtualMemory` (syscall 번호 `0x50` — 스텁의 `+0x3 MOV EAX,0x50`).
- **무엇을 넘기나**: (−1 = 이 가게, `&Base 사본`, `&Size 사본`, 새 보호 = 입구에서 받은 `ECX` (4 또는 2), `&옛보호`). 사본은 `LdrpMrdataBase`·`LdrpMrdataSize` 를 지역에 복사한 것.
- **무엇을 위해**: `.mrdata` 구역 전체의 보호를 바꾼다. 실패하면 (`+0x58 JNS` 를 못 타면) `INT 0x29` 코드 5 로 그 자리에서 멈춘다.
- 모두 `[정적]` (run 문서 1절 표).

#### 한 줄 읽기 ③ — `+0x467 MOV dword [0x1e7450],EBX` (부르기가 아니라 쓰기)

- **누가**: `LdrpInitializeProcess` 가 **직접** 쓴다 (함수를 안 부른다).
- **어디에**: `0x1e7450` = 표 시작 `0x1e7430` + `0x20` = 칸 0 (`+0x10`) 의 `SizeOfImage` (`+0x10`) 칸.
- **무엇을**: `EBX` = `+0x40d` 에서 ntdll 헤더로 읽은 `SizeOfImage`.
- **왜 눈여겨보나**: 바로 앞 `+0x45e` 가 같은 칸을 이미 채웠는데, 그 위를 **헤더 값으로 한 번 더 덮는다**. 칸 0 의 네 값 중 이것 하나만 출처가 다르다 (2.4).
- `[정적]` (run 문서 3절 차례 표).

### 2.3 역함수 표 — 이번에 안을 열어 봤다

run18 해설에서 역함수 표를 "동네 지번 색인" 으로 불렀다. 이번엔 색인 책의 **쪽 모양**까지 봤다.

- `[정적]` 전역 `LdrpInvertedFunctionTables` (`.mrdata` rva `0x1e7430`) = PDB 타입 `_INVERTED_FUNCTION_TABLE_USER_MODE`, 크기 **`0x3010` B**.
  - = 머리 `0x10` + 칸 512 × `0x18`. 다음 공개 이름 `LdrpGuardCheckIcallNoESFptr` (`0x1ea440`) 까지 거리도 딱 `0x3010` 이다.
  - PE_emulator 가 적은 "0x28B" 는 머리 + 칸 0 (`0x10 + 0x18`) 만 본 것으로 읽힌다 `[추측]` (결과서).

**머리 네 칸** `[정적]` (괄호는 파일 처음 값):

| 칸 | 이름 | 뜻 |
|---|---|---|
| `+0x0` | `CurrentSize` (1) | 지금 쓰는 칸 수 (칸 0 포함) |
| `+0x4` | `MaximumSize` (`0x200`) | 칸 최대 수 = 512. 아무도 안 고친다 |
| `+0x8` | `Epoch` (0) | 아래 새 말 |
| `+0xc` | `Overflow` (0) | 512 칸이 다 찼을 때만 1 |

- **새 말 — `Epoch`** = 표를 고칠 때마다 올라가는 번호. 넣기·빼기 한 번에 **앞뒤로 한 번씩, 모두 +2** 올린다 `[정적]` (`INC.LOCK`).
  - 고치는 중이면 홀수가 되는 꼴로 보인다 `[추측]` (baggage 3.9.1).
  - 비유: 색인 책 표지의 **개정 번호**. 고치기 시작할 때 한 번, 다 고치고 한 번 올린다.

**칸 하나 (`0x18` B)** `[정적]`: `FunctionTable` (`+0x0`) · `ImageBase` (`+0x8`) · `SizeOfImage` (`+0x10`, dword) · `SizeOfTable` (`+0x14`, dword).

- `ImageBase`·`SizeOfImage` = 건물이 **몇 번지부터 몇 번지까지** 인가.
- **새 말 — 함수표 (`FunctionTable`)** = 그 모듈 안 **예외 디렉터리** 의 주소다 `[정적]` (칸 0 값 = ntdll 시작 + 예외 디렉터리 RVA `0x1d5000`). `SizeOfTable` 은 그 크기 (`0x108d8`).
  - 예외 디렉터리는 함수마다 [풀림 정보](run59-init-leftovers-해설.md) 를 찾아 주는 목차로 알려져 있다 `[추측: 일반적으로 알려진 설명]`.
  - 비유: 색인 한 줄 = "**○○번지 ~ ○○번지는 이 건물, 이 건물의 비상구 안내 목차는 여기, 목차 길이는 이만큼**".
  - 주의: [run51 해설](run51-eprocess-later-해설.md) 의 "함수표" (함수 주소를 묶어 넘기는 **내선 번호표**) 와 "동적 함수표" 와는 **다른 것**이다. 이번 "함수표" 는 모듈 파일 안에 원래 들어 있는 목차다.

**커널도 이 색인 주소를 안다** `[관찰]`: `PsNtdllExports +0x68 KiUserInvertedFunctionTable = 7ffafb847430` = ntdll + `0x1e7430` (run44 K1 보충 로그 172줄). 표 내용은 ntdll 만 쓴다 (baggage 4장 짝).

### 2.4 칸 0 은 정렬 구간 밖 — 그래서 넣고, 옮겨 적고, 뺀다

이번 바퀴의 가운데 질문이다. 단계별로 간다.

#### (가) 근거 — 칸 0 은 "정렬 구간" 에 없다 `[정적]`

- **새 말 — 정렬 구간** = 칸 1 ~ `CurrentSize − 1`. 여기 칸들은 `ImageBase` 가 **작은 것부터 큰 것 순서** 로 늘어선다. 그래야 반씩 잘라 찾는 **이진 검색** 을 할 수 있다.
- 이진 검색은 칸 1 부터 본다 (`RtlpxLookupFunctionTable +0x99 MOV R8D,1` · `+0xa6 DEC R9D`).
- 끼우기도 칸 1 부터 비교한다 (`RtlpInsertInvertedFunctionTableEntry +0x4d`).
- 칸 0 은 따로 **직접 읽는 곳 15곳** 이 본다 (예: `RtlLookupFunctionEntry +0x1d1`, 결과서 이름만).
- 비유: 색인 책의 본문 (칸 1 ~) 은 번지 순서로 정렬돼 있다. 그런데 맨 앞 표지 안쪽에 **관리실 전용 칸** (칸 0) 이 하나 따로 있다. 찾는 사람은 그 칸을 먼저 보고, 아니면 본문을 뒤진다.

#### (나) 그래서 — ntdll 은 칸 0 에 있어야 하고, 정렬 구간엔 없어야 한다

차례 `[정적]` (run 문서 3절 표, 숫자는 그대로):

| 자리 (call / ret) | 하는 일 | `CurrentSize` | `Epoch` | 칸 0 | 칸 1 |
|---|---|---|---|---|---|
| 시작 (파일 값) | — | 1 | 0 | 0 | 0 |
| `+0x415 / +0x41a` 넣기 (`RSI`, `EBX`) | `CurrentSize` 가 1 이라 `RtlpInsert…Entry +0x49 JZ → +0x95` 로 바로 칸 1 에 | **2** | **2** | 0 | ntdll |
| `+0x43a / +0x43f` 되읽기 | 지역 24B 를 채움 (2.5) | 2 | 2 | 0 | ntdll |
| `+0x44e` · `+0x45e` · `+0x467` | 칸 0 ← 지역 24B, `SizeOfImage` 는 헤더 값으로 덮음 | 2 | 2 | **ntdll** | ntdll |
| `+0x470 / +0x475` 빼기 | 칸 1 을 뺌 (`CurrentSize == 2` 라 `+0x17 JZ` 로 메모리 옮기기 없음) | **1** | **4** | ntdll | ntdll (남음) |

- 순효과 (`+0x475` 직후): 머리 `1 · 0x200 · 4 · 0`, 칸 0 = `{ntdll시작 + 0x1d5000, ntdll시작, 0x267000, 0x108d8}` `[정적]`.
- **칸 1 엔 ntdll 복사본이 남는다.** 빼기 (`RtlpRemoveInvertedFunctionTableEntry`) 는 뒤 칸을 앞으로 당기기만 하고 **마지막 칸 바이트를 안 지운다** `[정적]`. 나중에 exe 가 칸 1 에 들어올 때 덮인다.
  - `CurrentSize` 가 1 이니 칸 1 은 "없는 칸" 취급이다. 바이트가 남아 있어도 아무도 안 본다.

#### (다) 칸 0 의 네 값은 어디서 왔나 `[정적]`

- `+0x44e MOVAPS` (`FunctionTable`·`ImageBase`) · `+0x45e MOVSD` (`SizeOfImage`·`SizeOfTable`) = `+0x43a` 되읽기가 채운 **지역 24B** 그대로.
- `+0x467` = **헤더 `SizeOfImage`** (`EBX`, 한 줄 읽기 ③).
- 그러니 칸 1 에서 "복사" 한 게 아니다. **되읽기 결과** 를 옮겨 적은 것이다.

#### (라) 아직 확인 아님 — 왜 이렇게 돌아가나

- 왜 칸 0 에 바로 안 쓰고 넣기·되읽기를 거치는지는 **코드로 못 정한다** `[추측: 칸 만들기를 한 곳에 모음]` (결과서·run 문서).
- 이 질문은 Q115 (c) 덤으로 넘어갔다 (4절).

### 2.5 되읽기 — 룩업은 이때 색인을 안 보고 창구에 묻는다

`RtlpxLookupFunctionTable` (1339B) 은 큰 함수인데, `+0x43a` 때 타는 길은 하나다.

#### 갈림길 `[정적]`

- `+0x16 CMP [LdrInitState],3` → 3 이 아니면 `+0x2b JNZ → +0x2a3` (느린 길).
- 이 순간 `LdrInitState` 는 **0** 이다: `_LdrpInitialize +0x101` 이 0 을 쓰고, 1 · 2 · 3 을 쓰는 줄 (`+0x1a4c` · `+0x1b1e` · `+0x17dd`) 은 모두 `+0x43a` 보다 뒤다 (`_refs/LdrInitState.tsv`).
- 그래서 **느린 길**. [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 의 "세팅 중" 팻말이 아직 0 인 것이다.

#### 느린 길이 하는 일 `[정적]`

1. call `+0x2d0` (ret `+0x2d5`) `NtQueryVirtualMemory(−1, ntdll 시작, 클래스 6, 버퍼 [RSP+0x30], 0x18, NULL)`.
   - 클래스 6 = 이미지 정보 — 공개 이름 `MemoryImageInformation` 은 `[추측]`, 번호·길이 `0x18` 은 `[정적]`.
   - 창구에 "이 번지를 덮는 건물이 어디서 시작하고 얼마나 큰가" 를 묻는 셈이다.
2. 실패 · 시작 0 · 플래그 비트 · 주소가 시작보다 작음 → 모두 `+0x487` 로 가서 0 반환.
   - test.exe 에선 이 실패 길을 안 탄 것으로 보인다 — 채워진 24B 가 사진 P1a 칸 0 과 같아서다 (결과서 1(e) 끝 `[관찰]`).
3. `+0x30d` 결과 `SizeOfImage`, `+0x310` 결과 `ImageBase` 저장.
4. call `+0x35b` (ret `+0x360`) `RtlImageNtHeaderEx(1, 이미지 시작, 0, &헤더)` → PE32+ 확인, 예외 디렉터리 RVA (`[헤더+0xa0]`) 와 크기 → `+0x3bc` `FunctionTable = RVA + 시작`.

- 결과: 24B = `{ntdll시작 + 0x1d5000, ntdll시작, 0x267000, 0x108d8}` — 2.4 의 칸 0 값과 같다.
- 반환값 `RAX` 는 `LdrpInitializeProcess` 가 **안 쓴다** `[정적]`. 쓰는 건 채워진 24B 뿐이다.

#### 빠른 길은 이름만

- `LdrInitState == 3` 일 때: 색인 락 `LdrpInvertedFunctionTableSRWLock` 을 공유로 잡고, 칸 1 ~ 끝을 이진 검색한다. 경합 때만 `NtAlertThreadByThreadIdEx` `0x72`·`NtSetInformationThread` `0xd` `[정적]`.
- 비유: 가게 문을 열기 전 (팻말 0) 엔 색인을 안 믿고 **관리사무소 창구에 직접 묻는다**. 문을 연 뒤 (팻말 3) 엔 색인 책을 펴서 찾는다.

### 2.6 `.mrdata` 열고 닫기 — 덮개 옆 숫자판

#### 새 말 둘

- **읽기 전용 보호** = 메모리 쪽(page)마다 붙은 "읽기만 됨 / 읽기·쓰기 됨" 표지. 이번에 나오는 값은 둘이다 `[정적]`: **4 = `PAGE_READWRITE`**, **2 = `PAGE_READONLY`**.
  - 이 표지를 바꾸는 창구 신청이 `NtProtectVirtualMemory` 다.
  - 비유: 칠판 유리 덮개를 **잠그느냐 여느냐**. 열쇠는 관리사무소가 쥐고 있어서, 바꾸려면 창구에 신청해야 한다.
- **중첩 카운터** = "지금 열어 달라고 한 사람이 몇 명인가" 를 세는 숫자. 여는 사람마다 +1, 닫는 사람마다 −1 하고, **첫 사람이 올 때 (0 → 1) 만 진짜로 열고, 마지막 사람이 나갈 때 (1 → 0) 만 진짜로 닫는다.**
  - 비유: 덮개 옆 **숫자판**. 이미 누가 열어 둔 상태에서 또 누가 오면, 창구에 안 가고 숫자만 하나 올린다.

#### 규칙 — `LdrProtectMrdata(ECX)` `[정적]`

- 숫자판 = 전역 `LdrpMrdataUnprotected` (`.mrdata` rva `0x1e7128`, dword). 락 `LdrpMrdataLock` (`.data` `0x1d4de8`) 을 잡은 안에서만 만진다 (`+0x47 BTS.LOCK`).

| 인자 | 숫자판 | 하는 일 |
|---|---|---|
| `ECX = 0` 열기 | 0 | `LdrpChangeMrdataProtection(4)` (call `+0xb4`) → `PAGE_READWRITE`, 그 뒤 +1 |
| | 0 도 −1 도 아님 | syscall 없이 +1 (**중첩**) |
| | −1 | `INT 0x29` 코드 `0xe` |
| `ECX ≠ 0` 닫기 | 0 | `INT 0x29` 코드 `0xe` (닫을 게 없다) |
| | 그 밖 | −1. 결과가 0 이면 `LdrpChangeMrdataProtection(2)` (call `+0x6d`) → `PAGE_READONLY`. 아니면 syscall 없이 끝 |

- 쓰는 줄: 닫을 때 `+0x60`, 열 때 `+0xbb` `[정적]`. → **baggage 3.9 "쓴 자리 모름" 셋 중 `LdrpMrdataUnprotected` 는 이 함수 몫이 맞다.**
- 끝은 `+0x84` 에서 `RtlReleaseSRWLockExclusive` 로 꼬리 점프. 돌려주는 값은 없다 (`void` 모양).
- 코드 `0xe`·`5` 의 SDK 이름 (`FAST_FAIL_INVALID_REFERENCE_COUNT`·`FAST_FAIL_INVALID_ARG`) 은 결과서가 기억으로 적은 것 `[추측]`. 번호는 `[정적]`.

#### 같은 열고 닫기가 이 구간 뒤에도 또 있다 `[정적]` + `[추측]`

- call `+0xda1` (ret `+0xda6`) `LdrProtectMrdata(0)` → `+0xdb8 NtOpenDirectoryObject` (출력 칸이 `.mrdata` `LdrpKnownDllDirectoryHandle`) → call `+0xdc4` (ret `+0xdc9`) `LdrProtectMrdata`.
- 그래서 숫자판 0 → 1 (`NtProtectVirtualMemory` 4) → 0 (`NtProtectVirtualMemory` 2) 이 한 번 더 있을 것이다.
- 둘째 호출의 `ECX` 는 `LEA ECX,[RBX−0x2f]` 라서, `RBX` 를 안 따라가 봤다. 1 (닫기) 이라는 건 `[추측]` 이다.
- 지도에서 `LdrProtectMrdata` 를 부르는 곳은 모두 21곳이다 (예: `LdrpInitializeExecutionOptions +0x373·+0x384`, `LdrpInitializeCfgScpHelpers +0x5a·+0x80`).

### 2.7 숫자판은 1 에서 시작한다 — 단계별로

이 바퀴에서 가장 "아하" 인 대목이다. 천천히.

1. **근거** `[정적]`: 이 구간의 **열기는 셋** (`Insert +0x4b`, `+0x441`, `Rtlx…Remove +0x37`), **닫기는 넷** (`Insert +0x67`, `Rtlx…Remove +0x48`, `+0x47c`, `+0x488`) 이다.
2. **그래서**: 다 지나가면 숫자판은 순 **−1**.
3. **만약 0 에서 시작했다면**: 마지막 닫기 `+0x488` 때 숫자판이 이미 0 이라 `INT 0x29` 로 죽는다 (2.6 표, 닫기 · 0 줄). 그런데 test.exe 는 안 죽는다.
4. **그래서 1 에서 시작해야 맞는다.** 결과서가 PE 파일 바이트를 읽어 확인했다 `[정적]`: `LdrpMrdataUnprotected` **파일 처음 값 = 1** (`.mrdata` 파일 오프셋 `0x1e0128`).
5. **덮개는 처음에 열려 있다** `[정적]`: `.mrdata` 구역 특성이 `0xc0000040` (읽기·쓰기) 라서 매핑된 순간엔 쓸 수 있다.
   - 즉 "숫자판 1" = "**누군가 한 명이 열어 둔 상태**" 로 시작하는 것과 같다. 파일 값 1 이 첫 "열기" 노릇을 한다 (결과서).
6. **결론** `[정적]`: 숫자판은 이 구간을 지나며 `1 → 2 → 1 → 2 → 3 → 2 → 1 → 0` 이 되고, **`+0x488` 에서 처음 0 이 된다.** ntdll 초기화가 `.mrdata` 를 **처음 읽기 전용으로 바꾸는 자리가 `+0x488`** 이다.
   - 그 앞 열기·닫기 여섯 번은 숫자판이 1 ↔ 3 사이만 오가서 **창구에 한 번도 안 간다.**
   - `_LdrpInitialize` 의 `LdrInitializeMrdata` 와 `+0x415` 앞 호출들은 `LdrProtectMrdata` 를 안 부른다 (결과서).

비유: 칠판 덮개는 처음에 **열린 채**, 숫자판엔 "1명" 이 적혀 있다. 색인 담당 둘이 들락날락하며 숫자를 올렸다 내렸다 하지만 0 까지는 안 내려간다. 마지막으로 관리실 직원이 "1명" 을 지우는 순간 (`+0x488`), 처음으로 창구에 "덮개 잠가 주세요" 를 신청한다.

### 2.8 `+0x488` — 첫 잠금, 그리고 칠판 위치는 그때 처음 적는다

#### 사슬 `[정적]`

```
+0x488 LdrProtectMrdata(1)  → 숫자판 1 → 0
  └─ +0x6d LdrpChangeMrdataProtection(2)   +0x8 Base 읽기 → 0 이라 +0x12 JNZ 안 뜀
       ├─ +0x14 LdrpLocateMrdata          → LdrpMrdataSize (+0x52) · LdrpMrdataBase (+0x59) 처음 씀
       └─ +0x51 NtProtectVirtualMemory(−1, &Base, &Size, 2, &옛보호)
```

- `LdrpChangeMrdataProtection` 은 `LdrpMrdataBase == 0` 일 때만 `LdrpLocateMrdata` 를 부른다.
- 2.7 대로 `+0x488` 전엔 이 조수가 한 번도 안 불렸으니, `+0x488` 이 **첫 부름**이다. 그래서 `LdrpMrdataSize`·`LdrpMrdataBase` 도 이때 처음 써진다.
  - → **baggage 3.9 "쓴 자리 모름" 나머지 둘은 `LdrProtectMrdata` 가 직접 쓰는 게 아니라, 두 층 아래 `LdrpLocateMrdata` 몫이다.**
- [게으른 만들기](run51-eprocess-later-해설.md) 의 또 한 예다: 칠판이 어디 있는지 (시작·크기) 를 **처음 덮개를 잠글 일이 생겼을 때** 알아본다.

#### `LdrpLocateMrdata` 가 칠판 위치를 찾는 법 `[정적]`

1. `RtlImageNtHeaderEx(3, ntdll 시작, 0, &헤더)` (call `+0x22`).
2. `RtlSectionTableFromVirtualAddress(헤더, ntdll, 0x1ea450)` (call `+0x36`) — `0x1ea450` 은 `LdrSystemDllInitBlock` 의 RVA. 즉 "**업무 수첩이 들어 있는 구역**" 을 찾는다. 그게 `.mrdata` 다.
3. 그 구역 헤더에서 시작 (`0x1e7000`) 과 크기 (`0x35b8`).
4. `LdrpMakePermanentImageCommit(ntdll+0x1e7000, 0x35b8)` (call `+0x4d`) — 걸친 4쪽마다 8B 를 읽어 **같은 자리에 되쓴다**. 내용은 안 바뀐다.
   - 목적은 이미지 쪽을 프로세스 개인 쪽으로 굳히는 것으로 보인다 `[추측]` (결과서).
5. `+0x52 LdrpMrdataSize = 0x35b8`, `+0x59 LdrpMrdataBase = ntdll + 0x1e7000`.
6. 구역을 못 찾으면 (`+0x3e JZ → +0x67`) `INT 0x29` 코드 5.

### 2.9 사진과 맞나 — run44 P1a `[관찰]`

정적 셈을 사진 값에 대 본다. P1a 는 exe 가 색인에 들어간 **뒤** 순간이다.

| 무엇 | 정적 기대 (표는 `+0x475` 직후, `.mrdata` 전역은 `+0x48d` 직후 — 둘 다 exe 전) | 사진 P1a | 차이의 설명 |
|---|---|---|---|
| `CurrentSize` | 1 | 2 | + exe 하나 |
| `MaximumSize` | `0x200` | `0x200` | 같음 |
| `Epoch` | 4 | 6 | + exe 넣기 한 번 (+2) |
| `Overflow` | 0 | 0 | 같음 |
| 칸 0 | `{ntdll+0x1d5000, ntdll, 0x267000, 0x108d8}` | `{7ffafb835000, 7ffafb660000, 0x267000, 0x108d8}` | 같음 (`7ffafb660000` + `0x1d5000` = `7ffafb835000`) |
| 칸 1 | ntdll 복사본 | `{7ff61a3f5000, 7ff61a370000, 0x8c000, 0x4fbc}` (test.exe) | exe 가 덮음 (`LdrpProcessMappedModule +0x177`) |
| `LdrpMrdataSize` · `Base` | `0x35b8` · ntdll + `0x1e7000` | `0x35b8` · `7ffafb847000` | 같음 |
| `LdrpMrdataUnprotected` | 0 | 0 | 같음 |

- `Epoch 6 = 4 (ntdll 넣고 뺌) + 2 (exe 넣음)`, `CurrentSize 2 = 1 + exe` — **정적 셈과 맞다** (run 문서 2절).
- P1b 는 `CurrentSize 6`·`Epoch 0xe` (결과서).
- 근거 로그: `ms/logs/04_baggage-read_P1a.txt:5814 ~ 5817` (표), `:5748 ~ 5749`·`:5765` (`.mrdata` 전역).

### 2.10 CFG 내보내기 억제 확인 — test.exe 는 0

- **새 말 — CFG 내보내기 억제** = CFG 의 한 옵션. DLL 이 내보낸 함수도 기본으로는 간접 호출 허용 목록에서 빼 두는 방식으로 알려져 있다 `[추측: 일반적으로 알려진 설명]`.
  - 비유: 가게 [안전 규칙](../02_sprint/run32-params-initblock-해설.md) 중 "**바깥 손님용 문도 기본은 잠가 둔다**" 는 더 엄한 조항.

#### 무엇을 읽나 `[정적]`

`LdrControlFlowGuardEnforcedWithExportSuppression` (45B, 잎 함수 — 안에서 다른 함수를 안 부름) 은 **업무 수첩 `LdrSystemDllInitBlock` 세 칸만** 본다. PEB·이미지 헤더는 안 본다.

1. `+0x0` `+0xb8` 가 0 이면 → 바로 0 (`+0x8 JZ → +0x2b`).
2. `+0xa` `+0x9c` 비트 0 이 1 이면 → 0.
3. `+0x13 ~ +0x26` `(+0xa0 >> 40) & 3 == 3` 이면 1, 아니면 0.

- 칸 이름 (`CfgBitMap`·`Flags` 비트 0 `CfgOverride`·완화 옵션 비트 40~41) 은 공개 자료에서 빌린 것 `[추측]` — 두 PDB 에 이 블록 타입이 없다 (run43).

#### test.exe 에서 `[관찰]` + `[정적]`

- run44 K1·P1a·P1b·P2 모두 `+0xb8 = 0` (결과서) → 1번에서 **0**.
- `+0x492 CMP EAX,EBX` (`EBX = 1`) → `+0x494 JNZ → +0x4a4`. 그래서 `+0x496 ~ +0x49d` (`LdrpSuppressedExportOverrideListPtr` ← `&LdrpSuppressedExportOverrideList`) 를 **건너뛴다**.
- 그 전역은 사진에서 끝까지 0 — **PE_emulator 의 `[추측]` ("1 이 아님") 이 맞다.**
- `+0xb8 = 0` 인 이유로 짐작되는 것: `sample/build.bat` 에 `/guard:cf` 가 없고 `[소스]`, test.exe `DllCharacteristics = 0x8160` 에 GUARD_CF `0x4000` 이 없다 (결과서). → 커널이 CFG 비트맵을 안 만든 것으로 보인다 `[추측]`.

### 2.11 덤 — `.data 0x1ce268` 와 속성 `0xa0022` (세그먼트 힙 요청)

#### 새 말 — 프로세스 만들기 속성

- 부모가 `NtCreateUserProcess` 에 "이런 조건으로 만들어 줘" 하고 붙여 넘기는 항목 목록으로 알려져 있다 `[추측: 일반적으로 알려진 설명]`.
  - 이번 근거로 본 것 `[정적]`: 속성값은 번호 + 플래그 비트가 섞인 꼴 (공개 `PS_ATTRIBUTE_*` 모양, 결과서 2-2). 새 가게 쪽 목록은 머리 8B (총 길이) + 항목 `0x20` B 씩 (결과서 2-1).
- 비유: 가게 개업 신청서에 붙이는 **별지 요청서**.

#### 길을 따라가면 `[정적]` (이름은 `[추측]`)

```
부모 → 커널 PspBuildCreateProcessContext
         +0x1577 기능 확인 (Feature_CreateProcessSegmentHeapEnablement…) → 꺼지면 건너뜀
         +0x1584 TEST …,0x80000 → 켜진 항목만 call +0x159f PspProcessUserCopyAttribute
       PspCopyAndFixupParameters call +0x3eb PspCopyUserAttributes   (새 가게 쪽 목록으로)
         … (목록이 ProcessParameters +0x448 에 이어지는 줄은 안 봄 [공백])
새 가게 ntdll  LdrpInitializeProcess +0x3de LdrpHandleUserCopyAttributeList
         +0x448 목록에서 속성 0xa0022, 값 0 또는 1 → +0x82 [0x1ce268] = 값
       … RtlpHpOptIntoSegmentHeap +0x56 이 읽음: & 3 == 1 이면 "세그먼트 힙 쓰기"
```

- 속성 `0xa0022` 풀이: 번호 `0x22`, 플래그 `0x20000` (입력) + `0x80000` `[정적]`.
  - 커널 속성 표 (`.rdata` rva `0x2b750`) 에서 `0x80000` 을 가진 번호는 `0x22` **하나뿐** (PE 바이트).
  - `0x80000` = "유저 복사" 비트 (새 프로세스의 ntdll 이 읽도록 복사) 로 보인다 — 함수 이름 근거 `[추측]`.
- `.data 0x1ce268` 은 PDB 이름이 없다. 쓰기 하나 (`+0x82`), 읽기 하나 (`RtlpHpOptIntoSegmentHeap +0x56`) 뿐이다.
  - `LdrpThunkSignature` (`0x1cdfa0`) 의 짐작 크기 안에 있지만, 그 이름을 쓰는 `LdrpCaptureCriticalThunks` 는 앞 `0x50` B 만 쓴다 → **별개 이름 없는 변수** `[정적]`.
  - 그래서 "**프로세스 만들기 속성으로 받은 세그먼트 힙 요청 (0/1)**". 이름은 우리가 붙인 것 `[추측]`.
- 비유: 부모가 별지에 "**새 설계도 (세그먼트 힙) 로 창고 지어 주세요**" 를 적어 넘기면, 관리사무소가 그 별지를 새 가게에 넣어 주고, 관리실은 그걸 칠판 귀퉁이에 0/1 로 옮겨 적는다. 창고 짓는 담당이 나중에 그 칸을 본다.
- **test.exe** `[관찰]`: 매개변수 `+0x430 ~ +0x448` 가 0 (run44) → 목록이 비어 `+0x23 JZ → +0x15b` 로 바로 끝, `0x1ce268` 은 처음 값 0 그대로. [run46 해설](run46-process-heap-해설.md) 의 "test.exe 는 NT 힙" 과도 어긋나지 않는 것으로 보인다 `[추측]` (이 칸 하나만 놓고 본 것 — 세그먼트 힙을 고르는 조건은 run46 에 9개다).

### 2.12 정정과 이음 — 한 절에 모아서

#### (가) run35 Q67 (a) — 이음 (틀린 게 아니다)

- run35 의 질문: "ntdll 예외 표를 등록하는 곳은 `+0x45e` 근처 쓰기냐, `+0x41a` Insert 냐".
- 답 `[정적]`: **둘 다.** `+0x415` (ret `+0x41a`) 가 칸 1 에 올리고 (`Epoch +2`), `+0x44e ~ +0x467` 이 칸 0 에 놓고, `+0x470` (ret `+0x475`) 이 칸 1 을 뺀다 (2.4).
- 그래서 ntdll 항목이 `InExceptionTable` 을 미리 켜고 `LdrpProcessMappedModule +0x177` Insert 를 건너뛰는 게 앞뒤가 맞는다. run35 의 `[추측]` ("다른 곳에서 이미 등록") 이 `[정적]` 으로 맞았다.
- run35 문서 맨 위에 이음 한 줄, flow.md 2.4 `LdrpInitializeNtdllDataTableEntry` 절·4장 Q67 줄에도 반영.

#### (나) 짐 지도 사진 분류 — `LdrpMrdataUnprotected`

- 전: `끝까지 0*` (사진 8장 모두 0).
- 후: **`≤P1a`**. 사진 값 0 은 그대로지만, 파일 처음 값이 **1** 이라 0 은 누군가 **쓴** 값이다 (`+0x488`).
- run44 표의 `*` 판정은 "처음 값 0" 을 전제했었다. 비유하면, 칠판에 0 이 적혀 있다고 "아무도 안 건드렸다" 고 볼 수 없다 — 원래 1 이 적혀 있었으니까.

#### (다) 짐 지도 7.1 옛 숫자

- "아는 칸 중 끝까지 0 … 전역 16" 은 옛 숫자였다. 이번 셈으로 `296 − 266 = 30` 으로 고쳤다 (flow-writer 셈, run 문서 "정정").

#### (라) 결과서와 run 문서가 다른 곳 (결과서는 안 고침, 이 글은 run 문서를 따름)

| 결과서 | run 문서 (`.asm`) | 뜻 |
|---|---|---|
| `LdrpCaptureCriticalThunks +0x7a CMP EBX,5` | **`+0x77 CMP EBX,0x5`** (`+0x7a` 는 `JNC`) | 같음 (16B × 최대 5) |
| "PE32+ (`+0x389` 이 `0x20b`)" | `+0x381 MOV EDX,0x20b` · `+0x386 CMP` · `+0x389 JNZ` | 같음 |
| `LdrpMrdataHeap`·`HeapUnprotected` 쓰는 길 = "초기화 길 밖" | **"② 조건부"** | 아래 |

- 셋째 줄의 까닭 (run 문서): run51 이 초기화 길 안에서 `LdrpProcessMappedModule (call +0x19e) → RtlpInsertOrRemoveScpCfgFunctionTable (call +0xc4) → RtlAddGrowableFunctionTable` 길을 이미 적어 두었고, 그 함수의 call `+0x40` 이 `LdrEnsureMrdataHeapExists` 를 부른다 (지도). test.exe 에선 사진이 끝까지 0 이라 쓰기까지 안 간 것 `[추측]`. 숫자는 어느 쪽이든 "ntdll 이 씀" 이라 짐 지도 2장 셈은 같다.
- 앞 두 줄은 run59 때와 같은 "명령 하나 옆" 꼴이다 (run 문서 "도구 메모").

### 2.13 정리 — 비유로 한 번 더

1. 관리실 직원이 가게 세팅 중 (팻말 0) 에 **동네 지번 색인** 을 손본다. 첫 손님은 **관리실 건물 자신** 이다 (2.1).
2. 색인 책 맨 앞에는 정렬된 본문과 따로 **관리실 전용 칸** (칸 0) 이 있다. 관리실 건물은 거기 적혀야 한다 (2.4).
3. 그래서 본문 첫 줄 (칸 1) 에 일단 적고, 창구에 "관리실 건물이 몇 번지부터 얼마나 큰가" 를 직접 물어 (2.5) 전용 칸에 옮겨 적고, 본문 줄은 지운 셈 친다. 개정 번호는 4 가 된다. 왜 이렇게 돌아가는지는 아직 짐작뿐이다.
4. 색인이 적힌 칠판엔 **유리 덮개와 숫자판** 이 있다. 숫자판은 1 에서 시작하고 덮개는 열려 있다. 들락날락하는 동안 창구에 안 간다 (2.6 · 2.7).
5. 숫자판이 처음 0 이 되는 `+0x488` 에서, 조수가 그제서야 칠판 위치 (시작·크기) 를 알아보고 창구에 "덮개 잠가 주세요" 를 신청한다 (2.8).
6. 마지막으로 업무 수첩을 보고 "바깥 손님 문도 잠그는 날인가" 를 확인한다. test.exe 는 아니다 (2.10).
7. 그 앞에 부모의 별지 요청서 (세그먼트 힙) 를 칠판 귀퉁이에 옮기는 일이 있지만, test.exe 는 별지가 없다 (2.11).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 절 | 전 | 후 |
|---|---|---|
| 기준 | PDB 줄에 run60 없음 | ntkrnlmp·ntdll PDB 줄에 run60. "run44 P1a·K1 보충 로그 다시 읽음, 파일 처음 값은 PE 바이트, 새 동적 없음" 메모 |
| 2.1 `PspBuildCreateProcessContext` | 속성 줄 없음 | `[정적]` 속성 **`0xa0022`** — `+0x1584 TEST …,0x80000` 켜진 항목만 call `+0x159f` `PspProcessUserCopyAttribute`, 그 앞 기능 확인 `+0x1577` |
| 2.1 `PspCopyAndFixupParameters` | — | `[정적]` call `+0x3eb` `PspCopyUserAttributes`. `+0x448` 으로 이어지는 줄은 `[공백]` |
| 2.4 `LdrpInitializeProcess` 트리 | `+0x41a` 한 줄 ("ntdll 을 역함수 표에") | 묶음 3 뒤 **9줄** (`+0x3de`·`+0x41a`·`+0x43f`·`+0x446`·`+0x475`·`+0x481`·`+0x486`·`+0x48d`·`+0x492`) |
| 2.4 `+0xdbd` 줄 | `NtOpenDirectoryObject` 만 | 앞뒤 `+0xda6 / +0xdc9` `LdrProtectMrdata` 열고 닫기 (출력 칸이 `.mrdata`) |
| 2.4 새 절 | 없음 | **"역함수 표·`.mrdata` 열고 닫기 — 묶음 3 뒤 `+0x3d5 ~ +0x4a4`"** `[정적]` (+ P1a `[관찰]` 대조, 비유) |
| 2.4 `LdrpInitializeNtdllDataTableEntry` 절 | Q67 (a) 후보 둘 `[추측]` | **둘 다** `[정적]` — 짐작이 맞았다 |
| 2.4 뒤4 `LdrpInitializeCfgScpHelpers` | `LdrProtectMrdata` 이름만 | "열고 닫기 규칙은 run60" 링크 |
| 4장 | Q67 (a) 열림 | Q67 (a) **해결됨**, Q111 **해결됨**, 새 줄 셋: 선택 동적 (→ Q104 VM 단계) · 정적 공백 · 결과서와 다르게 적은 곳 |
| 5장 | — | run60 근거 줄 둘 |
| 그림 | — | **안 바뀜** |

- 한 줄로: 묶음 3 의 "`.mrdata` 쓰기 열고 닫기, CFG 확인" 이 **이름만** 이던 것이 `[정적]` 사슬과 숫자판 규칙이 됐다.

### 짐 지도 `baggage.md` 에서 바뀐 것

- **3장 새 줄 10** (3.8 `.data` 4 · 3.9 `.mrdata` 6). 3장 줄 647 → **657**. 줄 끝마다 **"Q111 로 닫힘"**.
  - 3.8: `0x1ce268` (목록 밖 자리, 조건부 안 탐) · `LdrpInvertedFunctionTableSRWLock` · `LdrpSuppressedExportOverrideListPtr` (조건부 안 탐) · `LdrpMrdataLock` 둘째 줄. 첫째 줄의 "락 `[추측]`" 은 `[정적]` 으로.
  - 3.9: `LdrpMrdataHeap`·`HeapUnprotected` (② 조건부) · `LdrpMrdataSize`·`Base` · `LdrpMrdataUnprotected` · `LdrpInvertedFunctionTables` 셋 (넣기 / 칸 0 직접 / 빼기).
  - 새 **3.9.1**: 역함수 표 안쪽 칸 표 (머리 4 + 칸 0 + 칸 1 ~). 전역 하나 안쪽이라 2장 칸 수엔 안 센다.
- **2장 숫자**: 전역 쓴 자리 아는 것 105 → **113** (`.data` 89 → 91 · `.mrdata` 15 → 21) · 모름 388 → **380**. 사진 `끝까지 0` 전역 297 → **296** (분류 정정). 구조체 숫자는 그대로.
- 4장 짝 1줄 (`LdrpInvertedFunctionTables` ↔ `PsNtdllExports.KiUserInvertedFunctionTable`), 5장 ② 묶음 3 순서, 6장 공용 부품 1줄 (`LdrProtectMrdata` 열고 닫기), 7.3·7.5 정리, 새 **7.20**.

### 큰 그림에서 어떻게 이어지나

- **PE_emulator 쪽**: run 문서 1절 표 (함수 × 인자·쓰는 칸·하위 호출·syscall·반환) 와 3절 끝 "채울 순서 ① ~ ⑥" 이 2-B③b 바퀴의 근거가 된다.
  - 특히 PE_emulator 가 지켜야 할 것: `NtProtectVirtualMemory` 는 `LdrProtectMrdata` 몸통이 아니라 **두 층 아래 `LdrpChangeMrdataProtection`** 에 있고, **숫자판이 0 ↔ 1 을 넘을 때만** 불린다. 숫자판 처음 값은 **1** 이다.
  - run59 의 쿠키 (`LdrInitSecurityCookie` 가 덮개를 직접 여닫음) 와는 **다른 길** 이다. 같은 `.mrdata` 칠판이지만 덮개를 여는 담당이 둘이다 `[정적]`.
- **이 구간 다음**: `+0x4a4` 에서 묶음 4 로 넘어간다. 색인 칸 1 에 exe 가 들어오는 건 그 뒤 `LdrpProcessMappedModule +0x177` (run35) 이고, 그 결과가 사진 P1a 의 `2 · 0x200 · 6 · 0` 이다.
- **다음 차례 Q104 VM 단계**: 이번 결과는 정적 + 사진으로 이미 맞아서 **꼭 필요한 동적 확인은 없다.** 하지만 같은 VM 세션에 얹어 두면 "중간 값" (넣기 직후 `2, 0x200, 2, 0`, 숫자판 1 → 0 순간) 을 직접 볼 수 있다 (4절).

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 (선택) → **Q104 VM 단계**

결과서 "동적" 1 ~ 6 그대로. 정적과 run44 P1a 가 이미 맞아서 **선택** 이다. ntdll 멈춤점은 모든 프로세스에 걸리니 **test.exe 조건** 을 붙인다.

| 번 | 무엇을 보나 | 왜 정적으론 "중간 값" 이 안 되나 | 멈출 곳·읽을 것 |
|---|---|---|---|
| 1 | 넣기 직후 표 머리·칸, 숫자판 | 사진 P1a 는 exe 넣은 **뒤** 라 중간 값이 섞임 | `ntdll!LdrpInitializeProcess+0x41a`: `dd ntdll+1e7430 L4` (→ `2, 0x200, 2, 0`), `dd ntdll+1e7128 L1` (= 1) |
| 2 | 룩업이 정말 느린 길인가 | 갈래가 `LdrInitState` 실행 값에 달림 | `ntdll!RtlpxLookupFunctionTable+0x2d0`: `r rcx, rdx, r8, r9` (= `-1`, ntdll, `6`, 버퍼) |
| 3 | 빼기 직후 | 1 과 같음 | `+0x475`: 머리 `1, 0x200, 4, 0`, 숫자판 2 |
| 4 | 숫자판 1 → 0 과 칠판 위치 첫 쓰기, 보호 | 순간 값 | `+0x486` / `+0x48d`: `dq ntdll+1e7018 L1`·`dq ntdll+1e7028 L1` 0 → `0x35b8`·ntdll+`0x1e7000`, `!vprot ntdll+1e7000` = `PAGE_READONLY` |
| 5 | 커널 쪽 `NtProtectVirtualMemory` 가 이 구간 첫 호출인가, `+0xda1`·`+0xdc4` 한 쌍이 4 → 2 로 또 부르나 | `+0xdc4` 의 `ECX` 는 `RBX` 값 (2.6) | `nt!NtProtectVirtualMemory` (test.exe 조건), `+0xdc4` 에서 `r ecx` |
| 6 | CFG 반환 0 | 사진으로 이미 닫힘 — 생략 가능 | `+0x492`: `r eax` |

### 정적으로 남은 것 → 새 **Q115**

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 |
|---|---|---|
| `PspCopyUserAttributes` 가 쓴 속성 목록이 새 프로세스 `ProcessParameters +0x448` 칸에 이어지는 줄 | `PspCopyAndFixupParameters` 쪽을 이번엔 안 봄 | Q115 (a) |
| `LdrpSuppressedExportOverrideListPtr` 를 **읽는** 곳 | `_refs` 엔 쓰기 `+0x49d` 1줄뿐 — 레지스터 상대로 읽으면 안 잡힘 | Q115 (b) |
| (덤) 속성 `0x22` 공개 이름 · `LdrpMakePermanentImageCommit` 목적 · 칸 0 을 넣기·되읽기로 채우는 이유 · `+0xdc4` 의 `ECX` · `LdrEnsureMrdataHeapExists` 가 쓰기까지 가는 조건 · `.mrdata` 를 여닫는 다른 caller 약 20곳의 순서 | 질문 밖이거나 PDB 에 없음, 본문 `.asm` 없음 | Q115 (c) (필요할 때만) |

### 대기열 항목이 없는 것 (기록만)

- test.exe 로는 속성 목록이 비어서 `0x1ce268` 이 **쓰이는 길** 을 동적으로 못 본다. 다른 부모가 `0xa0022` 속성을 주는 시험이 따로 필요하다 (run 문서 "아직 확인 필요"). 이것만 따로 묻는 대기열 항목은 없다.

### 닫힌 것

- **Q67 (a)** (run35) — 이번에 답이 이어졌다 (2.12 (가)).
- **Q111** — 끝.

---

## 5. 더 읽기

- run 문서: [`run60-func-table-mrdata.md`](run60-func-table-mrdata.md)
  - 1절 PE_emulator 용 함수 표 · 2절 역함수 표 칸 표와 기대 값 · 3절 숫자판 규칙과 구간 차례 표 · 4절 함수별 `.asm` 줄 트리
  - "확인된 것" 1(a) ~ (e)·2, "결과서와 다르게 적은 곳", "정정", "짐 지도 반영", "아직 확인 필요"
- 요청서: [`_workspace/sast/55_func-table-mrdata.md`](../../_workspace/sast/55_func-table-mrdata.md)
- 결과서: [`_workspace/sast/55_func-table-mrdata_result.md`](../../_workspace/sast/55_func-table-mrdata_result.md) — 1(a) 표 머리·칸과 `.mrdata` 전역 표 · 1(c) 카운터 표 · 1(d) CFG · 1(e) 룩업 갈래 · 2-1 / 2-2 덤
- flow.md: [2.4 ntdll 로더 시작](flow.md) 의 새 절 "역함수 표·`.mrdata` 열고 닫기 — 묶음 3 뒤" · `LdrpInitializeNtdllDataTableEntry` 절 (Q67 (a)) · 2.1 `PspBuildCreateProcessContext` · 4장 run60 줄
- baggage.md: [3.8 · 3.9 · 3.9.1](baggage.md) · 7.20
- 이어 읽기: [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 묶음 3 · [run35 해설](../02_sprint/run35-ntdll-entry-해설.md) 2.5 · [run59 해설](run59-init-leftovers-해설.md) 2.6 (쿠키 쪽 덮개 여닫기)
