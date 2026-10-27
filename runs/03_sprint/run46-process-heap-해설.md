# run46 process-heap — 해설

> 이 글은 [run46 문서](run46-process-heap.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q78a** — Q78 (짐 지도 C, 쓴 자리·줄기) 의 첫 조각, "ntdll 프로세스 힙 만들기 구간" (`_workspace/plan.md`, 이번에 **끝**)
  - Q78 정의에 "한 바퀴에 너무 크면 그 자리에서 나눈다" 가 있다. 사용자 요청 (2026-10-27) 으로 힙 구간을 먼저 떼어 냈다.
  - 다음 항목: **Q78 나머지** → Q79 → Q80 → Q82 (plan "다음 항목").
  - 이번에 남은 동적 질문은 새 항목 **Q91** 로 맨 뒤에 붙었다.
- 근거:
  - run 문서 [`run46-process-heap.md`](run46-process-heap.md) (가장 중요)
  - 요청서 [`_workspace/sast/40_process-heap.md`](../../_workspace/sast/40_process-heap.md)
  - 결과서 [`_workspace/sast/40_process-heap_result.md`](../../_workspace/sast/40_process-heap_result.md) (판정: **분석성공**)
  - 짐 지도 [`baggage.md`](baggage.md) 의 고친 곳: 2장 숫자 · 3.1 · 3.8 · 6장 · 7.6.1 · 7.9
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (그림 2 · 2.4 · 3장 · 4장 · 5장)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 값을 맞춰 보려고 run44 P1a 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`) 를 **다시 읽었다**. 새로 VM 을 돌린 건 없다.
  - 그래서 이 글의 "사진" 은 전부 run44 때 찍어 둔 것이다. 실행을 멈춰 본 게 아니라 "그 순간 메모리에 그 값이 있었다" 이다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) (**안내판**), VAD (땅의 **구획 장부**) → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 2절
- 프로세스 힙 = 가게 **창고**, 묶음 7 = **창고 짓기** → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md)
- ntdll 전역 = 관리실 **칠판** → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md)
- 매개변수 블록 = 부모가 써 준 **개업 서류**, `LdrSystemDllInitBlock` = **업무 수첩** → [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- `KUSER_SHARED_DATA` (KUSD) = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 짐 · 짐 지도 · 줄기 · 공용 부품 → [run43 해설](run43-baggage-checklist-해설.md)
- 사진 지점 P1 · P1a, "처음 값을 갖는 구간" (`P1~P1a`, `≤P1a`) → [run44 해설](run44-baggage-snap-해설.md) 1절 · 2.10
- 일 52 (프로세스 힙 만들기) · 일 55 (매개변수를 힙으로 옮기기) · C7 (창고 안 선반과 상자들) → [run45 해설](run45-work-list-해설.md) 2.3 · 2.5 · 2.6

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**, 주소 공간 = 가게의 **빈 땅**, 짐 = **개업 준비물**.
- 프로세스 힙 = 가게 **창고** (run18 해설). 직원들이 "이만큼 자리 좀" 하면 창고 선반을 내준다.
- run 문서가 이번에 쓴 비유 ("가게 창고 짓기") 를 그대로 따른다:
  - **창고 관리실 (힙 관리자)** 을 먼저 차리고 → **창고 하나 (프로세스 힙)** 를 짓고 → 창고 주소를 **가게 장부 (PEB `ProcessHeap`)** 에 적는다.
  - 관리실을 차릴 때 **큰 빈 땅 둘** 을 미리 맡아 둔다.
  - 창고는 땅을 조금 넉넉히 빌린 뒤 **앞쪽을 무작위로 잘라 돌려주고** 시작한다. 창고 자리를 매번 흔들려는 것이다.
- 이 글이 더하는 비유는 새 말이 나올 때 하나씩 붙인다.

### 한 줄로 먼저 — 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| test.exe 프로세스 힙의 종류 | **NT 힙** (세그먼트 힙 아님) |
| 구간 `LdrpInitializeProcess +0x9ed ~ +0xa50` 안 시스템 콜 | **12개** (무작위 몫 n 이 0 이면 11개) |
| run44 사진의 큰 예약 둘의 주인 | **힙 관리자** (희소 비트맵). 크기 `0x2001000` · `0x100020000` 이 사진과 **정확히 같음** |
| PEB `+0x30 ProcessHeap` 을 쓰는 줄 | **`LdrpInitializeProcessHeap +0x1a2`** |
| PEB 힙 목록 3칸 (`NumberOfHeaps` · `MaximumNumberOfHeaps` · `ProcessHeaps`) | 힙보다 **먼저** `RtlInitializeHeapManager` 가 준비, 첫 힙을 넣을 때 `RtlpProcessHeapsInsert` 가 마무리 |
| IFEO | 이 구간에서 **처음** 열린다 |
| 정정 | run18 · run45 일 52 — `RtlSetHeapInformation` 은 조건부 (test.exe 안 탐), 태그 힙은 안 만듦 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

ntdll 로더가 막 출근해서 **가게 안 살림을 차리는** 앞부분이다 (run18 해설의 "살림 차리기").

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스·주소 공간·PEB·첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk                  (사진 P1)
 │    └─ LdrpInitializeProcess
 │         ├─ 묶음 4  +0x536  LdrpInitializeExecutionOptions   (IFEO 를 읽어야 할 자리 — test.exe 는 건너뜀)
 │         ├─ 묶음 7  +0x9f2  RtlInitializeHeapManager          ← 이번 바퀴 (창고 관리실 차리기)
 │         │          +0xa50  LdrpInitializeProcessHeap         ← 이번 바퀴 (창고 짓기 → 장부에 적기)
 │         ├─ 묶음 8  +0xc3f  RtlpInitParameterBlock            (개업 서류를 창고로 옮김, run45)
 │         ├─ …       +0x19a2 kernel32 준비                      (사진 P1a)
 │         └─ … DLL 올리기 · 켜기 → NtContinue 은퇴             (사진 P2)
 └─ RtlUserThreadStart → (CRT) → main    (범위 밖)
```

- 이번 구간은 사진 **P1 과 P1a 사이**에 들어 있다. 그래서 run44 는 "P1~P1a 에 값이 생김" 까지만 말할 수 있었다.

### 무엇이 궁금했나

요청서 질문은 여섯이다.

1. 호출 사슬과 순서
2. 보통 길 (test.exe 값으로 가는 길) 의 시스템 콜 전부
3. 갈림길 — 세그먼트 힙 / NT 힙, 디버그·페이지·태그·파티션 힙
4. 읽는 입력
5. 쓰는 상태 — PEB 4칸, ntdll 힙 전역, `_HEAP` 머리
6. run44 P1 → P1a 에 새로 생긴 VAD 3개와 맞추기

그리고 사용자가 짚은 포인트가 하나 있다.

> **왜 하필 힙을 먼저 닫나?**

짧은 답: **뒤에 오는 칸들이 다 이 창고에서 블록을 받기 때문이다.** 이건 이 바퀴의 "왜" 이니까 2.1 에서 따로 천천히 본다.

- 범위: `_HEAP` 머리는 "힙 하나를 만드는 동안 머리에 쓰는 칸" 까지만.
- **블록을 나눠 주는 속 (`RtlAllocateHeap` 본문, LFH) 은 범위 밖**이다 (사용자 결정 2026-10-26 "기록만", 2026-10-27 "힙 내부 정적 안 넣음").

### 바퀴 전에 알던 것

- `[정적]` (run18) 묶음 7 = `LdrpInitializeProcess +0x9e5 ~ +0xb7d`, 호출 13곳 표. "힙 관리자 → 힙 정보 → `LdrpInitializeProcessHeap`". PEB `+0x30` 칸 **주소**를 `+0xa39` 에서 넘긴다. 실제로 쓰는 줄은 "callee 안 (안 봄)".
- `[관찰]` (run44) P1 → P1a 사이에 PEB `ProcessHeap` · `NumberOfHeaps` · `MaximumNumberOfHeaps` · `ProcessHeaps` 와 ntdll 힙 전역 여럿이 값을 갖는다. VAD 도 새로 생긴다 — 1MB 하나, 아주 큰 예약 둘.
- 오케스트레이터의 짐작 (요청서): "4GB · 32MB+4KB 예약은 힙 관리자 것" — 이번에 확인할 것.
- `[정적]` (run45) 일 55 — `RtlpInitParameterBlock` 이 `RtlAllocateHeap(PEB.ProcessHeap, …)` 으로 매개변수 블록 자리를 이 힙에서 받는다.

---

## 2. 찾은 것

### 2.1 왜 힙을 먼저 닫나 — 뒤 칸들이 이 창고에서 블록을 받는다

이 바퀴가 "Q78 전체" 가 아니라 "힙 구간 하나" 부터 시작한 이유다. 두 가지 "먼저" 가 겹친다.

#### (가) 실행 순서에서 먼저 — 창고가 없으면 아무도 상자를 못 받는다

- 프로세스 힙은 **뒤 짐 대부분의 블록 출처**다 (짐 지도 6장 "공용 부품" 첫 줄, 요청서).
  - 매개변수 블록 · 환경 블록 — run45 일 55. `RtlpInitParameterBlock` 이 이 힙에서 같은 크기 자리를 받아 통째로 옮긴다 `[정적]`.
  - 모듈 항목 · 노드 (건물 **명부** 의 줄들)
  - TLS
  - 활성화 컨텍스트 스택
  - run45 의 C7 ("창고 안 선반과 상자들") 에 걸린 일: 52 53 54 55 62 71 75 76 78.
- `[관찰]` run44 P1a 에서 새 `ProcessParameters` 주소 `25996f37a50` 은 프로세스 힙 `25996f30000` **안**이다.
- `[정적]` ntdll 로더 자신의 전역 `LdrpHeap` 도 따로 만든 로더 힙이 아니라 **`PEB.ProcessHeap` 값을 복사한 것** 이다 (run18 "LdrpHeap = PEB 힙" + 사진 같은 값). 관리실도 같은 창고를 쓴다.
- `[정적]` 그래서 창고 짓기가 실패하면 초기화가 거기서 끝난다: `LdrpInitializeProcess +0xa55` 에서 로그를 남기고 `0xc0000017` (run18 해설에도 같은 길).
- 비유: 개업 날 직원들이 서류철, 명부 카드, 개인 사물함을 받으러 온다. 전부 **창고 선반**에서 나온다. 창고가 안 지어졌으면 아무도 아무것도 못 받는다.

#### (나) 분석 순서에서 먼저 — "이 블록은 어디서 왔나" 를 한 번만 판다

- 나머지 Q78 은 칸마다 "누가 썼나" 를 거꾸로 따라간다 (run45 해설 2.5).
- 그런데 뒤 칸 상당수는 "어떤 함수가 `RtlAllocateHeap` 으로 받은 블록에 썼다" 로 끝난다. 그 블록의 출처를 칸마다 다시 파면 같은 일을 수십 번 한다.
- 그래서 창고 자체를 **공용 부품** 으로 한 번 닫아 둔다 (CLAUDE.md "공용 부품은 한 번만 보고 이름으로 가리킨다").
- 이번에 정해진 것 (결과서 "Q78 에서 이 조각으로 닫히는 칸"):
  - 프로세스 힙 = `_HEAP` 시작 `25996f30000` (이 실행)
  - 첫 빈 블록 = 힙 `+0x740`
  - 힙 안 첫 블록 = `FrontEndHeapUsageData` (`0x100`바이트, 힙 `+0x750`)
  - 둘째 블록 = 프로세스 힙 목록 노드 (`0x30`바이트, 힙 `+0x860`)
  - 그 뒤 블록은 `RtlAllocateHeap` 본문이라 범위 밖.
- 비유: 이삿짐 상자마다 "이 상자 어디서 났지?" 를 묻기 전에, **창고가 어디 있고 첫 선반이 어딘지** 를 먼저 적어 둔다. 그다음부터는 "창고에서 꺼냄" 한마디로 끝난다.

#### 덤 — 창고가 생기기 전에 필요한 것들은 어디 두나

창고를 짓는 일 자체도 자리가 필요하다. 그런데 창고가 아직 없다. 그래서 이 구간엔 **닭이 먼저냐 달걀이 먼저냐** 같은 모양이 둘 보인다.

- `[정적]` PEB `ProcessHeaps` (힙 목록 배열) 는 힙 블록이 아니라 ntdll `.data` 의 **정적 배열** `RtlpPebHeapListStaticBuffer` (16칸) 를 가리킨다 (`RtlInitializeHeapManager +0xf5`).
  - `[추측]` 이 시점엔 힙이 없으니 칠판 (ntdll 전역) 에 미리 마련된 칸을 쓰는 것으로 보인다. 이유는 코드에 적혀 있지 않다.
- `[정적]` 첫 힙을 목록에 넣을 때 필요한 노드 (`0x30`바이트) 는 **새 힙 자신**에서 받는다 (`RtlpProcessHeapsInsert`, `PEB.ProcessHeap` 이 아직 0 이면). 다음 힙부터는 프로세스 힙에서 받는다.
- 비유: 창고를 다 짓고 나서, 창고의 첫 선반에 "창고 목록 카드" 를 꽂아 둔다. 두 번째 창고부터는 그 카드를 **첫 창고**에서 꺼내 온다.

### 2.2 한눈에 — 관리실 차리기 → 창고 짓기 → 장부에 적기

run 문서 "호출 관계" 를 대표만 줄였다. `+0x..` 는 ret 자리 (WinDbg 콜스택 기준).

```
LdrpInitializeProcess
 ├─ +0x9f2  RtlInitializeHeapManager           ① 창고 관리실 차리기                     [정적]
 │       ├─ +0x12   RtlHpGlobalsInitialize           난수 상태·키
 │       ├─ +0x2d   RtlpHpOptIntoSegmentHeap         세그먼트 힙? → test.exe 0 (NT 힙)
 │       ├─ +0x7d   RtlpHpQueryGCTimerInterval       NtOpenKey ×2 (IFEO)
 │       ├─ +0xa2   RtlpGetModifiedProcessCookie     NtQueryInformationProcess 0x24
 │       │  +0xe7 ~ +0x10a  PEB 힙 목록 3칸 준비
 │       ├─ +0x196  RtlpInitializeLowFragHeapManager  NtQuerySystemInformation 0
 │       └─ +0x1a0 ⇒ RtlHpInitializeHeapManager (꼬리 JMP)
 │               ├─ +0xae   NtQuerySystemInformation 0x3e (사용자 공간 끝)
 │               └─ +0xdd   RtlHpHeapManagerStart     큰 예약 둘
 │  +0xa34  RtlSetHeapInformation             조건부 — test.exe 안 탐
 └─ +0xa50  LdrpInitializeProcessHeap          ② 창고 짓기 → ③ 장부에 적기              [정적]
         ├─ +0x196  RtlpCreateHeap                    NT 힙 길
         │       ├─ +0x45e  예약 1MB + 무작위 몫  (+0x4a6 앞 몫 풀기)
         │       ├─ +0x517  확정 8KB
         │       ├─ … _HEAP 머리 칸, 첫 세그먼트, 블록 색인, 첫 블록
         │       └─ +0x974  RtlpProcessHeapsInsert    목록에 넣기 · ProcessHeaps[0] · NumberOfHeaps 1
         └─ +0x1a2  PEB +0x30 ProcessHeap = 힙 주소
```

- 시스템 콜 12개가 어디 붙는지는 2.12 표.
- `⇒` 는 꼬리 JMP. 2.5 에서 본다.

### 2.3 한 줄 읽기 — `+0x1a2` (PEB `ProcessHeap` 을 쓰는 줄)

run18 부터 비어 있던 줄이 이번에 닫혔다. 천천히 읽는다.

- **누가**: ntdll `LdrpInitializeProcessHeap`.
- **어디서**: 그 함수의 `+0x1a2`. 명령은 `MOV qword ptr [R15],RAX` (run 문서가 `.asm` 으로 다시 봄) `[정적]`.
- **무엇을**: `RAX` = 방금 `+0x196` 에서 돌아온 `RtlpCreateHeap` 의 반환값 = 새 힙 주소. 그걸 `R15` 가 가리키는 칸에 쓴다.
- **`R15` 는 어디서 왔나**: 이 함수의 4번째 인자다. 부르는 쪽 `LdrpInitializeProcess +0xa39` 의 `LEA R9,[RDI+0x30]` 이 넘긴 **PEB `+0x30` 칸 주소** 다 `[정적]`.
  - run18 의 "칸 주소를 `+0xa39` 에서 넘김" 이 **넘기는 쪽**, 이번 `+0x1a2` 가 **받는 쪽** 이다. 두 반쪽이 이어졌다.
- **조건**: `RtlpCreateHeap` 반환이 0 이 아닐 때만 (`+0x19c` 검사). 0 이면 `0xc0000017` 길.
- **사진**: P1a `ProcessHeap` = `25996f30000` `[관찰]` run44.
- 비유: 창고 짓기 담당이 "창고 주소 적을 칸" 을 손에 쥐고 나갔다가, 창고를 다 지은 뒤 그 칸에 주소를 적는다.

짐 지도 3.1 의 놓는 법도 그래서 "주소 넘김 (칸 주소 `+0xa39` `LEA R9`) → 받은 함수가 직접 씀" 으로 바뀌었다.

### 2.4 새 말 — NT 힙 / 세그먼트 힙, 그리고 test.exe 는 NT 힙

#### 새 말

- **NT 힙 / 세그먼트 힙**: ntdll 이 가진 **두 가지 힙 구현**이다.
  - 일반적으로 알려진 설명 `[추측]`: NT 힙은 오래전부터 써 온 기본 힙, 세그먼트 힙은 Windows 10 에서 새로 들어온 구현이고 일부 시스템 프로세스·패키지 앱에서 기본으로 쓰인다고 알려져 있다.
  - 비유: 창고 **설계도가 두 종류** 있다. 옛날식 설계도 (NT 힙) 와 새 설계도 (세그먼트 힙). 같은 "창고" 지만 선반 배치가 다르다.
- 둘은 머리에 찍힌 표시로 구별된다 `[정적]`: 힙 `+0x10` 이 NT 힙이면 `0xffeeffee`, 세그먼트 힙이면 `0xddeeddee`.

#### 고르는 곳 — 조건 9개

`RtlpHpOptIntoSegmentHeap` 이 조건 9개를 **순서대로** 본다. 하나라도 맞으면 거기서 끝 `[정적]`.

| # | 조건 | test.exe |
|---|---|---|
| ① | 전역 `RtlpLowFragHeapGlobalFlags` 비트 3 ("끔") | 꺼짐 |
| ② | 같은 전역 비트 4 ("켬") | 꺼짐 |
| ③ | 이름 없는 전역 `0x1ce268 & 3 == 1` | 0 |
| ④ | `PEB.pShimData` 안 두 바이트 | **못 봄** (D1) |
| ⑤ | 스위트 마스크 비트 16 | 꺼짐 |
| ⑥ | 매니페스트 `heapType = SegmentHeap` | 아님 |
| ⑦ | 패키지 앱 | 아님 |
| ⑧ | 매개변수 `HeapPartitionName` | 빈 값 |
| ⑨ | 이미지 이름 목록 24개 | `test.exe` 없음 |

- 목록 ⑨ 의 이름은 `svchost.exe` · `csrss.exe` · `lsass.exe` · `dwm.exe` … 같은 시스템 프로그램이다 (결과서 질문 3).
  - `[추측]` 위 "일반적으로 알려진 설명" (시스템 프로세스·패키지 앱) 과 ⑦ · ⑨ 가 맞아 보인다.

#### 왜 "NT 힙" 이라고 말할 수 있나 — 단계별로

1. 근거 (정적): 9개 중 **8개**가 test.exe 값으로 "아님" 이다 `[정적]` (값은 run44 사진 P1 · P1a `[관찰]`).
2. 근거 (사진): P1a 힙 `+0x10` = `0xffeeffee` (NT 힙 표시), `+0x14` = 2, `RtlpHpHeapFeatures` = 0 `[관찰]`.
3. 그래서: 결과 (NT 힙) 는 **사진이 정했다**.
4. 아직 아닌 것: ④ 의 두 바이트를 못 봤다. 그래서 "**어느 조건 때문에** 세그먼트 힙을 안 골랐나" 는 못 가린다. ④ 는 맞으면 "0 반환하고 뒤 조건 안 봄" 길도 있어서, ④ 에서 끝났을 수도 있다 `[공백]` → D1 (Q91).

- 비유: 설계도 고르는 담당이 체크리스트 9줄을 위에서부터 본다. 결과는 "옛날식" 으로 나왔고 창고 문패에도 옛날식이라 찍혀 있다. 다만 넷째 줄 칸이 흐려서 "넷째 줄에서 멈췄는지, 끝까지 내려갔는지" 는 모른다.

#### 다른 곳에서도 승격될 수 있었나

- `RtlpCreateHeap` 안에도 "NT 요청을 세그먼트 힙으로 바꿔 주는" 길이 있다: 플래그 `0x100`, 또는 `RtlpHpHeapFeatures & 1` + (자라는 힙 · `Base` · `Lock` NULL) `[정적]`.
- test.exe 는 `RtlpHpHeapFeatures` = 0 `[관찰]`, LoadConfig `ProcessHeapFlags` = 0 `[정적+샘플]` → 안 탐.

### 2.5 꼬리 JMP — 콜스택에서 `RtlInitializeHeapManager` 가 사라진다

- **꼬리 JMP** (꼬리 점프): 함수 끝에서 `call` 대신 `JMP` 로 다른 함수에 넘어가는 것. 돌아오지 않으니 콜스택에 자기 자리를 안 남긴다 → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) 에서 처음 봤다.
  - 비유: 바통을 넘기고 **트랙에서 나가 버리는** 주자. 다음 주자가 결승선에서 돌아갈 곳은 "그 앞 주자" 가 아니라 **그 앞의 앞** 이다.
- 이번 자리 `[정적]`: `RtlInitializeHeapManager +0x1a0` 이 `RtlHpInitializeHeapManager` 로 꼬리 JMP.
- 그래서 큰 예약을 하는 순간 멈추고 `k` 를 치면 이렇게 보일 것이다 (run 문서 "콜스택 기대", `[정적]`):

```
RtlpHpEnvAllocVA+0xe2 ← RtlCSparseBitmapStart+0xc0 ← RtlHpHeapManagerStart+0x31
  ← RtlHpInitializeHeapManager+0xdd ← LdrpInitializeProcess+0x9f2 ← _LdrpInitialize+0x16c
```

- `RtlHpInitializeHeapManager` 바로 위가 `RtlInitializeHeapManager` 가 아니라 **`LdrpInitializeProcess+0x9f2`** 다.
- 왜 신경 쓰나: 나중에 동적으로 멈췄을 때 "어, `RtlInitializeHeapManager` 를 안 거쳤네?" 하고 헷갈리지 않으려고. flow.md 2.4 에도 "콜스택 주의" 로 적혔다.

### 2.6 IFEO 가 이 구간에서 처음 열린다

#### 새 말 다시 한 번

- **IFEO** (Image File Execution Options): exe 이름별로 실행 옵션을 적어 두는 레지스트리 자리 `[추측]` (일반적으로 알려진 설명) → [run10 해설 3.4](../02_sprint/run10-allocate-process-해설.md#34-144줄-중-어디쯤인가).
  - 비유: 관리사무소 서랍의 **가게 이름별 특별 지시** 폴더 (run18 해설). "test.exe 가게는 이렇게 다뤄 줘" 같은 쪽지가 있을 수 있다.

#### 여기서 무엇을 여나 — 시스템 콜 #1 · #2

- 왜 힙 관리자가 IFEO 를 보나: **GC 간격** 이라는 힙 설정값을 IFEO 에서 찾으려고 (`RtlpHpQueryGCTimerInterval`, 값 이름 `GCInterval`) `[정적]`.
- #1 `NtOpenKey` — IFEO **바탕 키** (`…\Image File Execution Options`) 를 연다. 핸들은 전역 `0x1ce430` 에 캐시 `[정적]`. 사진 `0x1ce430` = `0x10` `[관찰]` → 열렸다.
- #2 `NtOpenKey` — 그 아래 `test.exe` 키를 **열기만** 한다 `[정적]`.
  - test.exe 키가 없으면 실패 (기대 `0xC0000034`) → GC 간격 기본값 1000ms, 값 읽기 · 닫기 줄은 안 탄다 `[정적]`.
  - 사진: GC 간격 전역 `0x1ce278` = −10,000,000 (= 기본 1000ms × −10000) `[관찰]` → 기본값이 들어갔다 = 키가 없었을 것 (간접).

#### 왜 "처음" 인가

- 앞 묶음 4 의 `LdrpInitializeExecutionOptions` 가 원래 IFEO 를 읽는 자리다.
- 그런데 매개변수 `Flags` 가 `0x4001` (`[관찰]` run44 P1) 이고, 이 함수는 `Flags & 0x4000` 이면 IFEO 읽기를 **건너뛴다** `[정적]`.
  - 비트 이름은 SDK 의 `RTL_USER_PROC_IMAGE_KEY_MISSING` 으로 보인다 `[추측]` — "이 exe 는 IFEO 키가 없음" 표시.
- 다른 길 (`LdrpInitializeProcess +0x8f2`, 스택 추적 DB) 도 조건이 안 맞아 안 탄다 (`NtGlobalFlag` = 0 `[관찰]`).
- 그래서 이 프로세스에서 IFEO 를 처음 여는 건 **힙 관리자의 GC 간격 조회** 다 `[정적]`. 실제 순서는 동적으로 확인할 거리 (D4).
- 비유: 부모가 개업 서류에 "특별 지시 폴더에 우리 가게 쪽지 없음" 이라고 표시해 줬다. 그래서 관리실은 서랍 확인을 건너뛰었다. 그런데 창고 관리실은 "GC 간격" 하나 보려고 **서랍을 직접 연다**. 결과는 역시 쪽지 없음.
- 덤: 구간 **앞**의 `Segment Heap` 레지스트리 키 (`NtOpenKey`) 는 IFEO 를 건너뛰어도 **항상** 연다 (`RtlSetGlobalHeapFeatures` 아래) `[정적]`. 구간 밖이라 12개엔 안 셌다.

### 2.7 새 말 — 예약 / 확정

이번 결과의 숫자 대부분이 이 두 말로 읽힌다.

- **예약** (`MEM_RESERVE`, 형 `0x2000`): 주소 범위만 "여기 내 자리" 로 잡아 두는 것. 아직 실제 메모리는 안 붙는다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 빈 땅에 **말뚝만 박아** 두는 것. 구획 장부 (VAD) 에 "이 범위는 내 것" 줄이 올라간다. 그 위엔 아직 아무것도 못 놓는다.
- **확정** (`MEM_COMMIT`, 형 `0x1000`): 예약한 자리 일부를 실제로 쓸 수 있게 하는 것 `[추측]` (일반적으로 알려진 설명).
  - 비유: 말뚝 박은 땅 일부에 **바닥을 깔아** 물건을 놓을 수 있게 하는 것.
- run44 사진의 `!vad` 는 둘 다 보여 준다: 구역의 **쪽 수** (예약 크기) 와 **확정 쪽 수**. 그래서 이번 바퀴가 정적 계산과 쪽 단위로 맞춰 볼 수 있었다 (2.8 · 2.9).
- 형 숫자 `0x2000` · `0x1000` 은 코드에 있는 값이다 `[정적]`. 이름은 SDK 이름.

### 2.8 힙 관리자의 큰 예약 둘 — 희소 비트맵

#### 새 말

- **희소 비트맵**: "칸마다 1비트" 로 쓰임 여부를 적는 체크표인데, 표가 아주 커서 **필요한 쪽만 확정해 쓰는** 것 (결과서 "희소 비트맵", 크기 계산은 2단: 데이터 쪽 + 요약 쪽 `[정적]`).
  - 비유: 동네 전체 땅을 칸칸이 나눈 **두꺼운 체크표 책**. 책장은 많이 맡아 두지만 (예약), 실제로 펼친 쪽만 종이를 넣는다 (확정). 맨 뒤에 "어느 쪽을 펼쳤나" 를 적는 **요약 쪽** 이 붙는다.
- **힙 관리자의 큰 예약**: 힙 관리자가 시작할 때 (`RtlHpHeapManagerStart`) 맡아 두는 이 체크표 둘이다.

#### 무엇을 찾았나

| # | 누가 | 크기 | 확정 | 사진 VAD (P1a) |
|---|---|---|---|---|
| #6 · #7 | `RtlHpHeapManagerStart +0x31` → `RtlCSparseBitmapStart` → `RtlpHpEnvAllocVA` | 예약 `0x2001000` (32MB + 4KB) | 1쪽 (요약 쪽) | `7ff5ed090 – 7ff5ef090` (`0x2001` 쪽, 확정 1쪽) |
| #8 | `RtlHpHeapManagerStart +0x56` → `RtlpHpVaMgrCtxStart` → (같은 아래) | 예약 `0x100020000` (4GB + 128KB) | 없음 | `7ff4ed070 – 7ff5ed08f` (`0x100020` 쪽, 확정 0) |

- 둘 다 `NtAllocateVirtualMemoryEx` 에 형 `0x102000` (`MEM_RESERVE | MEM_TOP_DOWN`) 이다 `[정적]`. 위에서부터 잡으니 32MB 쪽이 위, 4GB 쪽이 바로 아래 (사진 주소와 같은 순서).

#### 왜 "힙 관리자 것" 이라고 말할 수 있나 — 단계별로

1. 근거 (정적): 크기는 **사용자 공간 끝** 에서 계산된다. #5 `NtQuerySystemInformation` 클래스 `0x3e` 로 그 값을 알아낸 뒤 `[정적]`:
   - #6: 비트 수 `0xFFFFFFE` (사용자 공간 끝 ÷ 1MB × 2) → 32MB 비트맵 + 4KB 요약 → 예약 `0x2001000`. 요약이 `0x1000` 이하라 요약 1쪽만 확정 (#7).
   - #8: 비트 수 `0x800000000` → 예약 `0x100020000`. 요약 쪽이 `0x20000` 으로 `0x1000` 을 넘어서 확정 없음.
2. 근거 (사진): 사진 VAD 쪽 수 `0x2001` · `0x100020`, 확정 1 · 0 — 계산과 **정확히 같다** `[관찰]`. 시작 주소도 덤프의 `RtlpHpHeapManager` 칸 (`0x1ce910+0x8`, `0x1ce958+0x28`) 값과 같다.
3. 그래서: 요청서 짐작 "힙 관리자 것" 이 맞다. 다만 "4GB" 가 아니라 정확히 **4GB + 128KB** 다.
4. 아직 아닌 것: 세 번 부른 **순서** 와 `MEM_TOP_DOWN` 효과는 실행을 봐야 안다 (D7, 사진으로 대부분 답돼서 낮은 우선순위).

#### test.exe 는 NT 힙인데 왜 이걸 만드나

- 결과서 질문 2: #5 가 실패하면 `RtlHpHeapManagerStart` 를 건너뛰고, 그러면 큰 예약 둘이 안 생긴다. 이때 **NT 힙 프로세스 힙은 영향이 없고, 세그먼트 힙을 나중에 만들면 실패** 한다 `[정적]`.
- 그래서 이 둘은 세그먼트 힙 쪽 준비물로 보인다 `[추측]` — 이유: 없을 때 깨지는 게 세그먼트 힙뿐이고, 받는 부품 `RtlpHpEnvAllocVA` 를 세그먼트 힙 길도 쓴다 (짐 지도 6장). 코드가 "무엇에 쓰려고" 를 적어 두진 않았다.
- 어쨌든 힙 종류와 상관없이 **항상** 만든다 (#5 성공이면) `[정적]`.
- 비유: 창고 관리실은 옛날식 창고만 지을 예정이어도, 새식 창고용 체크표 책을 일단 맡아 둔다. 펼치진 않는다.
- 짐 지도 쪽 의미: run44 에서 "누구 것인지 모르는 아주 큰 VAD 둘" 이던 게, 이제 주인 (ntdll 전역 `RtlpHpHeapManager`) 과 쓴 자리를 갖는다 (3.8).

### 2.9 창고 짓기 — `RtlpCreateHeap` (NT 힙 길)

`LdrpInitializeProcessHeap +0x196` 이 부른다. 인자 `[정적]`: 플래그 `2`, `Base` 0, 예약 `0x100000`, 커밋 `0x1000`, `Lock` 0, 매개변수, 7번째 0.

- `0x100000` · `0x1000` 은 test.exe PE 헤더의 `SizeOfHeapReserve` · `SizeOfHeapCommit` 이다 `[정적+샘플]`.
  - 비유: 개업 서류가 아니라 **건물 설계도 (exe 헤더)** 에 "창고는 이만큼" 이 적혀 있다.

순서 (시스템 콜만, run 문서 표):

1. **#9** `NtQuerySystemInformation` 클래스 0 — 사용자 공간 최대 주소를 전역 `0x1ce6e8` 에 캐시. 이 프로세스 첫 `RtlpCreateHeap` 이라서 부른다 `[정적]`. 사진 `0x1ce6e8` = `7ffffffeffff` `[관찰]`.
   - 뒤에 `RtlpCreateHeap` 를 또 부르는 쪽 (`LdrpInitializeTlsHeap`, `CsrpConnectToServer` 등) 은 캐시가 있어 이 호출이 없다 (결과서).
2. **#10** `NtAllocateVirtualMemory` **예약** `0x100000 + n×0x10000` — 2.10.
3. **#11** `NtFreeVirtualMemory` (`MEM_RELEASE`) 앞쪽 `n×0x10000` 만 풀기 — n ≠ 0 일 때만.
4. **#12** `NtAllocateVirtualMemory` **확정** `0x2000` — `0x1000` 을 8KB 단위로 올린 값.

그다음은 시스템 콜 없이 창고 안을 꾸민다 `[정적]`:

- `_HEAP` 머리 칸 (`+0x5a1 ~`), 인코딩 (`RtlpCreateHeapEncoding`), 힙 안 락, 첫 세그먼트 = 힙 자신 (`RtlpInitializeHeapSegment`), 블록 색인 (`RtlpPopulateListIndex`).
- 첫 블록 `FrontEndHeapUsageData` 를 **자기 자신에서** `RtlAllocateHeap(힙, 0x80000a, 0x100)` 으로 받는다.
- `RtlpProcessHeapsInsert` — 프로세스 힙 목록에 넣고, PEB `ProcessHeaps[0]` = 힙, `NumberOfHeaps` = 1 (2.11).
- `_HEAP` 머리에 쓰는 칸은 짐 지도 7.6.1 에 25줄로 들어갔다 (체크리스트 밖 짐이라 2장 숫자엔 안 셈).

사진과 맞춰 보기:

- P1a 힙 VAD `25996f30 – 2599702f` = `0x100` 쪽 (1MB), 확정 9쪽 `[관찰]`.
- 정적으론 첫 확정 2쪽 (`0x2000`) → 그 뒤 힙이 자라 9쪽 (`NumberOfUnCommittedPages` `0xf6`) — 맞음.
- 자라는 부분은 `RtlAllocateHeap` 본문 쪽이라 범위 밖.

### 2.10 새 말 — 무작위 몫 (힙 주소 흔들기)

- **무작위 몫**: 힙을 예약할 때 원래 크기에 덤으로 얹는 `n × 0x10000` (n = 0 ~ 31) `[정적]`.
  - 받은 뒤 **앞쪽 몫만큼 다시 풀어** 버린다. 그러면 힙의 시작 주소가 실행마다 `0x10000` 단위로 달라진다.
  - 비유: 창고 땅을 1MB + 주사위 눈만큼 넉넉히 빌린 뒤, **앞쪽 주사위 눈만큼을 돌려준다**. 창고 크기는 늘 1MB 인데, 문 위치가 매번 조금씩 다르다.
- 사진에 흔적이 없는 이유: 풀린 앞쪽은 VAD 에서 사라진다. 그래서 사진엔 1MB 구역 하나만 있고 앞 구멍이 없다 `[관찰]`. 실제 n 은 사진으로 못 본다.

n 은 어디서 오나 `[정적]`:

1. `RtlHpGlobalsInitialize` 가 난수 상태를 심는다. 씨앗 = TEB 스레드 ID ^ KUSD 틱 값 → `RtlRandom` 두 번.
2. 상태는 전역 `RtlpHpHeapGlobals +0x40` 에 있고, 쓸 때마다 한 칸씩 돌린다 (xorshift 계열).
3. 같은 상태가 키 (`RtlpHeapKey` · `RtlpLFHKey`), 무작위 몫, `_HEAP` 인코딩에 차례로 쓰인다.
4. **시스템 콜 없이** 만든다. 직원 번호표와 로비 게시판 시계만 보고 주사위를 굴리는 셈이다.

- 그래서 시스템 콜 개수가 "12개 (n = 0 이면 11개)" 다. n = 0 이면 풀 것이 없어 #11 이 빠진다 (32번에 1번).
- 실제 n 과 #11 이 정말 불렸는지는 동적 D9.

### 2.11 PEB 힙 4칸 — 누가 언제 쓰나

| PEB 칸 | 처음 | 마지막 | P1a 사진 |
|---|---|---|---|
| `+0xe8 NumberOfHeaps` | `RtlInitializeHeapManager +0xe7` → 0 | `RtlpProcessHeapsInsert +0xb9` → 1 | 1 |
| `+0xec MaximumNumberOfHeaps` | `RtlInitializeHeapManager +0x10a` → `0x10` | — | `0x10` |
| `+0xf0 ProcessHeaps` | `RtlInitializeHeapManager +0xf5` → `&RtlpPebHeapListStaticBuffer` | 배열 `[0]` = 힙 ← `RtlpProcessHeapsInsert +0xca` | `7ffafb833ec0` (`[0]` = `25996f30000`) |
| `+0x30 ProcessHeap` | — | `LdrpInitializeProcessHeap +0x1a2` (2.3) | `25996f30000` |

- 모두 `[정적]` (run 문서가 `.asm` 으로 다시 봄), 사진 값은 `[관찰]` run44 (P1 은 0 · 0 · null).
- 읽는 순서 포인트:
  - **목록 3칸이 힙보다 먼저** 준비된다. 힙 관리자 단계에서 "목록 칸 16개짜리 빈 명단" 을 깔아 둔다.
  - 첫 힙이 생기면 그 명단 첫 줄에 적고 개수를 1 로.
  - `ProcessHeap` 은 **맨 마지막**. `RtlpProcessHeapsInsert` 가 도는 동안엔 아직 0 이다. 바로 그래서 2.1 의 "첫 노드는 새 힙 자신에서" 길을 탄다.
- 비유: 가게 장부에 "창고 목록" 쪽을 먼저 만들어 두고 (빈 칸 16개), 창고를 짓자마자 첫 칸에 적고, 맨 마지막에 장부 앞면 "우리 창고" 칸에 주소를 적는다.

### 2.12 시스템 콜 12개 — 한 표로

run 문서 맨 앞 표를 줄였다. 모두 `[정적]`. "사진" 열은 run44 P1a 값이 그 결과와 맞는다는 뜻 (실행을 멈춰 본 것 아님).

| # | 부르는 함수 (ret) | 시스템 콜 | 무엇 | 사진 |
|---|---|---|---|---|
| 1 | `RtlpOpenBaseImageFileOptionsKey +0x80` | `NtOpenKey` | IFEO 바탕 키 | `0x1ce430` = `0x10` |
| 2 | `RtlpOpenImageFileOptionsKeyEx +0xec` | `NtOpenKey` | `test.exe` 키 (없으면 실패 기대) | GC 간격 기본 (간접) |
| 3 | `RtlpGetModifiedProcessCookie +0x27` | `NtQueryInformationProcess` | 클래스 `0x24`, 4바이트 | — (곧 0 으로 덮임) |
| 4 | `RtlpInitializeLowFragHeapManager +0x89` | `NtQuerySystemInformation` | 클래스 0, 프로세서 수 | `RtlpAffinityState` = 2 |
| 5 | `RtlHpInitializeHeapManager +0xae` | `NtQuerySystemInformation` | 클래스 `0x3e`, 사용자 공간 끝 | 큰 예약 둘 있음 = 성공 |
| 6 | `RtlpHpEnvAllocVA +0xe2` | `NtAllocateVirtualMemoryEx` | 예약 `0x2001000` | VAD 32MB + 4KB |
| 7 | `RtlpHpEnvAllocVA +0xe2` | `NtAllocateVirtualMemoryEx` | 확정 `0x1000` | 확정 1쪽 |
| 8 | `RtlpHpEnvAllocVA +0xe2` | `NtAllocateVirtualMemoryEx` | 예약 `0x100020000` | VAD 4GB + 128KB, 확정 0 |
| 9 | `RtlpCreateHeap +0xe0c` | `NtQuerySystemInformation` | 클래스 0, 최대 주소 캐시 | `0x1ce6e8` = `7ffffffeffff` |
| 10 | `RtlpCreateHeap +0x45e` | `NtAllocateVirtualMemory` | 예약 `0x100000 + n×0x10000` | 힙 VAD 1MB |
| 11 | `RtlpSecMemFreeVirtualMemory +0x2f` | `NtFreeVirtualMemory` | 앞 `n×0x10000` 풀기 (n ≠ 0) | 흔적 없음 |
| 12 | `RtlpCreateHeap +0x517` | `NtAllocateVirtualMemory` | 확정 `0x2000` | — |

- 합: `NtOpenKey` 2 · `NtQueryInformationProcess` 1 · `NtQuerySystemInformation` 3 · `NtAllocateVirtualMemoryEx` 3 · `NtAllocateVirtualMemory` 2 · `NtFreeVirtualMemory` 1 = **12**.
- 클래스 `0x24` 이름 `ProcessCookie` 는 PDB 열거형에 없어 공개 헤더 이름 `[추측]`. 클래스 0 · `0x3e` 이름은 PDB `[정적]`.

#### 한 줄 읽기 — #10

- **누가**: `RtlpCreateHeap`.
- **어디서**: call `+0x459`, ret `+0x45e` (콜스택에 보일 자리).
- **누구를**: `NtAllocateVirtualMemory` — 커널에 "땅 좀 잡아 줘" 하는 시스템 콜.
- **무엇을 위해**: 프로세스 `-1` (= 자기 자신) 땅에 **예약** (형 `0x2000`) 으로, 보호 `4` (읽기·쓰기), 크기 `0x100000 + n×0x10000`. 창고 땅에 말뚝을 박는다.
- **조건**: 호출자가 힙 터 (`Base`) 를 안 줬을 때 (`Base = NULL`). test.exe 는 `Base = 0`.
- **결과가 남는 곳**: 커널의 VAD (구획 장부) 한 줄. 이게 run44 의 1MB 구역이다.

### 2.13 안 타는 갈래 — test.exe 값으로

| 갈래 | 왜 안 타나 (test.exe 값) |
|---|---|
| `+0xa34 RtlSetHeapInformation` ("손상 때 종료" 켜기) | `LdrSystemDllInitBlock+0xa0` 비트 12~13 = 0 (사진 `ntdll+0x1ea4f0` = 0) |
| `RtlInitializeHeapManager +0x67` 간접 호출 | 세그먼트 힙 조건이 0 이라 도달 안 함 (사진 `g_RegInfo+8` = 0) |
| 디버그 힙 `RtlDebugCreateHeap` | `NtGlobalFlag` = 0 |
| 페이지 힙 `RtlpDebugPageHeapCreate` | 전역 `0x1cc578` / `0x1cc588` = 0 |
| 태그 힙 | `NtGlobalFlag & 0x800` 아님 → `RtlCreateTagHeap` 은 0 반환 |
| 파티션 힙 `NtOpenPartition` | `HeapPartitionName` 빈 값 (사진 P1) |
| `NtQueryVirtualMemory` ×2 | 힙 터 `Base ≠ NULL` 일 때만. test.exe `Base = 0` |
| `RtlpHpLfhContextEnable` | 세그먼트 힙일 때만 |

- 조건 줄은 `[정적]`, 값은 대부분 run44 P1 · P1a `[관찰]`.
- 디버거 관련 `[정적]`: 이 구간은 `PEB.BeingDebugged` 를 안 읽는다. 디버그·태그 힙을 켜는 건 `NtGlobalFlag` 비트인데 test.exe 는 0 이다 (run44 는 커널 디버거만 붙음). flow.md 2.4 에 "디버거가 붙어서 달라지는 길" 로 적혔다.
- `RtlSetHeapInformation` 비트 배치 (옵션 번호 × 4비트) 는 `[추측]` 이다. PDB 의 `PS_MITIGATION_OPTION_HEAP_TERMINATE` = 3 만 `[정적]`.

### 2.14 `RtlCreateHeap` 은 껍데기 — 그래도 혼자 부르면 안 된다

- `[정적]` `RtlCreateHeap` (39바이트) 은 `RtlpCreateHeap` 을 한 번 부르는 **껍데기** 다. 같은 6개 인자 + 7번째 0.
- `[정적]` 그런데 `RtlInitializeHeapManager` 없이 부르면 안 된다. `RtlpProcessHeapsInsert` 가 기대는 것들 (목록 머리 · 락 · PEB 힙 배열 · 난수 상태) 이 없어서:
  - 목록 꼬리 0 을 따라가 접근 위반, 또는 `INT 0x29` (fast fail).
  - 난수 상태가 없으면 죽지는 않지만 무작위 몫·인코딩이 전부 0.
- 비유: 창고 짓기 업체 (`RtlCreateHeap`) 는 언제든 부를 수 있는 것처럼 보이지만, **창고 관리실 (힙 관리자)** 이 먼저 차려져 있어야 등록이 된다.
- 에뮬레이터 쪽에서 보면: 힙을 흉내 낼 때 "관리실 → 창고" 순서를 지켜야 한다는 뜻으로 읽힌다 `[추측]` (구현 범위는 사용자와 정할 일).

### 2.15 정정 둘 (run 문서 "정정")

- **run18 묶음 7 · run45 일 52 — `RtlSetHeapInformation` 은 조건부다.**
  - 전: "힙 관리자 → 힙 정보 → `LdrpInitializeProcessHeap`" (조건 없이).
  - 후: `LdrSystemDllInitBlock+0xa0` 첫 8바이트의 비트 12~13 이 1 일 때만 (`LdrpInitializeProcess +0xa1a ~ +0xa28`). test.exe 는 0 → **안 탄다** `[정적]` + 사진.
- **run45 일 52 — "태그 힙" 은 test.exe 에선 안 만든다.**
  - `RtlCreateTagHeap` (`+0xb78`) 은 `NtGlobalFlag & 0x800` 이 아니면 0 반환. `NtdllBaseTag` · `RtlpGlobalTagHeap` = 0 (사진).
- run18 · run45 문서 맨 위에 정정 한 줄이 붙었다. 본문은 그대로.
- 짐 지도 3.1 · 3.8 은 이 스프린트 문서라 바로 고쳤다 (3절).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 그림 2 `(0) 프로세스 준비` | `LdrpInitializeProcess` → `(2) +0xa50 LdrpInitializeProcessHeap` 상자 하나 | `(2) +0x9f2 RtlInitializeHeapManager` 와 `LdrpInitializeProcessHeap` → `+0x196 RtlpCreateHeap` 의 `(정적)` 상자 둘 추가 |
| 그림 2 `c4` 번호 | `(2)` ~ `(15)` | 하나씩 밀림 → `(3)` ~ `(16)`. 사진 P1a 자리 `+0x19a2` 는 `(7)` → **`(8)`** |
| 그림 2 상자 글 `LdrpInitializeProcessHeap` | "(정적: 결과가 PEB.ProcessHeap)" | "(정적: `+0x1a2` 에서 PEB.ProcessHeap = 힙, run46)" |
| 2.4 트리 | `+0xa50 LdrpInitializeProcessHeap → PEB+0x30` | `+0x9f2 RtlInitializeHeapManager` 줄, 조건부 `+0xa34` 줄, `+0xa50 → +0x196 RtlpCreateHeap → +0x1a2` |
| 2.4 묶음 표 7 줄 | "힙 만들기 + … 태그 힙" | 힙 관리자 → (조건부) → 힙, `LdrpHeap` (= PEB 힙), 태그 힙 (test.exe 는 0 반환) |
| 2.4 새 절 | — | `#### 프로세스 힙 만들기 — 묶음 7` (대표 트리 + NT 힙 · `+0x1a2` · 꼬리 JMP 주의 · IFEO 첫 열기 · 큰 예약 둘 · 주소 흔들기 · 껍데기 · 안 타는 갈래 · 디버거 영향) |
| 4.1 `(정적)` 상자 표 | `(2) LdrpInitializeProcessHeap` … | `(2) RtlInitializeHeapManager (run46)` 끼움, 새 줄 (상자 `p2h` · `p2c`, 확인할 멈춤 자리 `ntdll+0x8cdf1` 등) |
| 4장 공백 표 (2.4 줄들) | 짐 지도 Q78a 줄 (PEB 힙 칸 쓴 자리 모름) | **(해결됨) run46** + 새 줄 셋: 동적 D1 · D4 · D5 · D9 (→ Q91), 조건부 힙 전역 8개, 묶음 7 의 힙 뒤 부분 |
| 5장 | — | run46 줄 둘 |

태그 변화 대표:

- PEB `+0x30` 쓰는 줄: "callee 안 (안 봄)" → **`[정적]` `LdrpInitializeProcessHeap +0x1a2`**.
- `LdrpHeap` 뜻: "로더 힙 — 이름으로 본 뜻 `[추측]`" → **PEB 힙 값 복사** (run18 `[정적]` + 사진 같은 값).
- run44 큰 VAD 둘: 주인 짐작 → **`[정적]` 힙 관리자 희소 비트맵** (+ 사진과 쪽 수 일치).

### 짐 지도 `baggage.md` 에서 바뀐 것 (넷째판)

- 2장 숫자 (baggage 2장 그대로):
  - 구조체 21개: 아는 칸 183 → **186**, 모름 1,273 → **1,270** (PEB 힙 목록 3칸).
  - `_PEB`: ntdll 이 씀 12 → **15**, 모름 91 → **88**.
  - ntdll 전역: 아는 것 59 → **76**, 모름 434 → **417** (`.data` ntdll 47 → 64).
  - 구간이 정해진 모르는 칸 254 → **251**.
- 3장에 30줄 (3.1 4 · 3.8 26). 줄 끝 "**Q78a 로 닫힘**" = 나머지 Q78 이 다시 안 봐도 되는 칸.
  - 3.8 에는 이름 없는 자리 4줄도 들어갔다 (`0x1ce270~` GC 타이머, `0x1ce430` IFEO 핸들, `0x1ce6e8` · `0x1ce6f0` 최대 주소, `RtlpHpHeapManager` 끝쪽). 다른 이름의 짐작 크기 안에 묻혀 있던 자리라 전역 수 438 은 안 바뀐다.
- 6장 공용 부품 **첫 표**: 프로세스 힙 (블록 출처), `RtlAllocateHeap` (범위 밖), `RtlpHpEnvAllocVA`, 희소 비트맵 함수, `RtlpSecMemFreeVirtualMemory`, 난수, 임계 구역.
- 7.6.1 새 절: `_HEAP` 머리 (체크리스트 밖 짐, 2장 숫자 밖).
- 7.9 새 절: 이 조각의 동적 질문 (D1 ~ D13).

### 큰 그림에서 어떻게 이어지나

- 2.1 에서 본 대로, 이번 바퀴는 **뒤 Q78 의 바닥** 이다.
  - 다음 Q78 조각에서 "모듈 항목은 어디서 왔나", "TLS 블록은 어디서 왔나" 가 나오면 → "프로세스 힙 (run46) 에서 `RtlAllocateHeap`" 으로 가리키고 끝낸다.
- 시간 순서로도 이어진다: 이 창고가 생긴 직후 묶음 8 의 `RtlpInitParameterBlock` (run45) 이 개업 서류를 창고로 옮긴다. run44 사진에서 본 "`ProcessParameters` 가 P1 → P1a 사이에 힙으로" 의 **창고 쪽 절반** 이 이번에 채워진 셈이다.
- 에뮬레이터 쪽에서 보면 이 구간이 남기는 짐이 분명해졌다:
  - 유저 공간: PEB 4칸, ntdll 전역 (힙 관리자 · 키 · 목록), `_HEAP` 머리.
  - 커널 공간 (시스템 콜로만 닿음): VAD 3개 (1MB 힙, 32MB + 4KB, 4GB + 128KB), IFEO 키 핸들.
  - 어디까지 흉내 낼지는 목록이 다 된 뒤 사용자와 정한다 (CLAUDE.md "지금 목표").

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **D1** `PEB.pShimData` 안 두 바이트 (`+0x11aa` 비트 `0x40`, `+0x11b2` 비트 `0x4`) — 세그먼트 힙을 안 고른 게 **어느 조건** 때문인가 | 실행 때 값. 결과 (NT 힙) 는 사진이 정했지만 "어느 줄에서 멈췄나" 는 못 가림 | **Q91** (동적, `RtlpHpOptIntoSegmentHeap+0x190`). 덤프에 그 쪽이 있으면 덤프로도 |
| **D4** IFEO 열기 두 번의 결과, 그게 이 프로세스 **첫** IFEO 열기인지 | 반환값·순서는 실행을 봐야 함. 사진은 "바탕 키 열림 · GC 간격 기본" 만 간접으로 | **Q91** (`RtlpOpenBaseImageFileOptionsKey+0x80`, `RtlpOpenImageFileOptionsKeyEx+0xec`) |
| **D5** `NtQueryInformationProcess(0x24)` 반환·쿠키, 클래스 이름 `ProcessCookie` | 결과 칸이 곧 0 으로 덮여 사진에 안 남음 | **Q91** (`RtlpGetModifiedProcessCookie+0x27`) |
| **D9** 무작위 몫 n, `NtFreeVirtualMemory` 가 정말 불렸나 | 풀린 앞쪽은 사진에 흔적이 없음 | **Q91** (`RtlpCreateHeap+0x459`) |
| (선택) D3 매니페스트 `heapType` 조회가 음수로 끝나나 · D13 힙 머리 나머지 칸 값 | D3 은 정적 `[추측]`, D13 은 덤프 읽기라 이번 범위 밖 | **Q91** 낮은 우선순위 |
| 조건부로만 쓰는 힙 전역 8개 (`RtlpHpHeapFeatures` 등) 를 3장으로 옮길지 | 쓰는 줄은 결과서에 있지만 test.exe 에선 조건이 안 맞아 "닫힌 칸" 으로 안 셈 | 나머지 **Q78** (정적, baggage 7.5) |
| 묶음 7 의 힙 **뒤** (`+0xa50 ~ +0xb7d`: 기능 스위치, 로더 스냅 버퍼, 활성화 컨텍스트 스택, 클래스 `0xc5`) | 이번 요청 함수 밖 | 나머지 **Q78** (정적) |
| 이 구간이 만든 VAD 3개의 커널 쪽 `_MMVAD` 칸 | 커널 객체 칸 표는 아직 없음 | **Q79** (VAD 노드 `dt`) |
| 블록 나눠 주기 속 (`RtlAllocateHeap` 본문, LFH), 힙이 9쪽으로 자란 과정 | 사용자 결정으로 범위 밖 ("기록만") | 기록만 (baggage 7.6) |
| `alias:NtQuerySystemInformation+1` 의 같은 주소 이름 전부 | `ms/sym/ntdll.aliases.tsv` 가 없었음 (스텁 번호 `0x36` 으로 맞춤) | 도구 메모 (`pdb_aliases.py ntdll` 로 만들면 됨) |

- 대기열 순서 (plan): **Q78 나머지** (정적) → Q79 → Q80 → Q82 → 원래 순서. Q91 은 맨 뒤 (동적).

---

## 5. 더 읽기

- run 문서: [`run46-process-heap.md`](run46-process-heap.md) — 맨 앞 시스템 콜 12개 표와 "안 타는 시스템 콜", 호출 지점 표, 호출 관계 트리, 갈림길 표, 쓰는 상태, run44 사진과 맞춰 보기, 정정, 아직 확인 필요 (D1 ~ D13)
- 결과서: [`_workspace/sast/40_process-heap_result.md`](../../_workspace/sast/40_process-heap_result.md) — 질문 1 (사슬, `RtlCreateHeap` 껍데기), 질문 2 (크기 계산 · 무작위 몫 식), 질문 3 (조건 9개 표), 질문 4 (읽는 입력), 질문 5 (`_HEAP` 머리 3.x 표), 질문 6, "Q78 에서 이 조각으로 닫히는 칸"
- 요청서: [`_workspace/sast/40_process-heap.md`](../../_workspace/sast/40_process-heap.md) — "왜 이 구간을 먼저 (Q78a)"
- 짐 지도: [`baggage.md`](baggage.md) — 2장 (넷째판 숫자), 3.1 PEB `+0x30` · `+0xe8` · `+0xec` · `+0xf0`, 3.8 힙 전역, 6장 공용 부품 첫 표, 7.6.1 `_HEAP` 머리, 7.9 동적 질문
- 흐름도: [`flow.md`](flow.md) — 그림 2 `(0) 프로세스 준비`, 2.4 `#### 프로세스 힙 만들기 — 묶음 7`, 4장 공백 표 run46 줄, 5장 run46 줄
- 진행표: `_workspace/plan.md` Q78a (끝) · Q78 · Q91, 결정 기록 2026-10-27
- 앞 해설: [run18](../02_sprint/run18-proc-init-setup-해설.md) (창고 · 묶음 7) · [run44](run44-baggage-snap-해설.md) (사진 · `ProcessParameters` 이동) · [run45](run45-work-list-해설.md) (일 52 · 일 55 · C7) · [run15](../02_sprint/run15-user-thread-startup-해설.md) 2.6 (꼬리 점프) · [run10](../02_sprint/run10-allocate-process-해설.md) 3.4 (IFEO)
