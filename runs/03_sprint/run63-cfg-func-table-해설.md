# run63 cfg-func-table — 해설

> 이 글은 [run63 문서](run63-cfg-func-table.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.
>
> **고쳐 읽기 (run107)**: 2.8 의 "`LdrpIsModuleUnderSystem32` 가 ntdll 에서 거짓인 까닭 — 왜 거짓인지는 Q121 (c)" 는 run107 에서 닫혔다 — 그 함수는 폴더 길이가 `LdrpSystem32.Length + 2` 보다 길 때만 비교해서 **System32 하위 폴더 안 모듈만 참**이고, ntdll 은 System32 바로 아래라 거짓이다 `[정적]`. 본문은 그대로 두었다 → [run107 해설 2.11](run107-search-path-small-해설.md)

- 대기열: **Q120a** — PE_emulator 요청 11 의 질문 1 ~ 6: "ntdll 모듈 항목 덫 안 함수 표·CFG 본문" (`_workspace/plan.md`, 이번에 **Q120a 끝**)
  - 다음 항목: **Q120b** (요청 11 의 7번 묶음 — TLS, `LdrpAllocateSchedulerSharedData`, KnownDlls 등) → Q104 VM 단계 → …
  - 남은 질문이 간 곳: 동적 1 ~ 6 → **Q104 VM 단계** (그중 3번이 F7 = **Q79** 출처), 남은 작은 정적 셋 → 새 **Q121** (맨 뒤).
- 근거:
  - run 문서 [`run63-cfg-func-table.md`](run63-cfg-func-table.md) (가장 중요)
  - 요청서 [`_workspace/sast/58_cfg-func-table.md`](../../_workspace/sast/58_cfg-func-table.md)
  - 결과서 [`_workspace/sast/58_cfg-func-table_result.md`](../../_workspace/sast/58_cfg-func-table_result.md) (판정: **분석성공**, PDB 일치, 질문 1 ~ 6 모두 답)
  - flow.md 변경: 기준 · 그림 2 상자 셋·화살표 셋 · 2.4 `.mrdata` 줄 · 2.4 run51 절 · `LdrpInitializeNtdllDataTableEntry` 트리 한 줄 · 2.4 새 절 **"ntdll 항목 마무리 안"** · 4장 · 4.1 · 5장
  - 고쳐진 옛 문서: [`run51-eprocess-later.md`](run51-eprocess-later.md) · [`run60-func-table-mrdata.md`](run60-func-table-mrdata.md) (둘 다 맨 위 정정 한 줄)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 `ms/logs/04_baggage-read_P1a.txt` 를 결과서가 **다시 읽은 것** 이다 (59 · 1551 · 2690 · 2712 · 2795 · 2817 · 2846줄). 새 실행이 아니다.
  - ntdll 파일 안 값 (구역 `SCPCFG*`, 로드 구성, PE 디렉터리) 은 결과서가 **PE 파일 바이트**를 직접 읽은 것이다 `[정적]`.
- 결과서와 run 문서가 다르게 적은 곳은 **2.13** 에 모았다. 이 글은 **run 문서 숫자**를 따른다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 꼬리 JMP (`═▶`, 콜스택에 안 보이는 넘겨주기) → [run46 해설](run46-process-heap-해설.md)
- VAD = 땅의 **구획 장부** → [run10 해설](../02_sprint/run10-allocate-process-해설.md)
- PTE = 페이지 표의 한 칸 → [run31 해설](../02_sprint/run31-image-section-해설.md) 2절
- CFG = 간접 호출 목적지를 확인하는 **교환원** → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) 2.9 · CFG 비트맵 = 교환원의 **출입 명단**, `LdrSystemDllInitBlock` = 관리실 **업무 수첩** → [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- 역함수 표 `LdrpInvertedFunctionTables` = **동네 지번 색인**, 칸 0 = 관리실 전용 칸 → [run60 해설](run60-func-table-mrdata-해설.md) 2.3 · 2.4
- `.mrdata` = **유리 덮개 씌운 칠판**, `LdrProtectMrdata` 의 **숫자판** (0 ↔ 1 을 넘을 때만 창구에 감) → [run60 해설](run60-func-table-mrdata-해설.md) 2.6 · 2.7
- 함수 항목 (`RUNTIME_FUNCTION`) · 풀림 정보 = **비상구 지도** 목차 줄 → [run59 해설](run59-init-leftovers-해설.md) 2.9
- 정보 클래스 (`NtQuery…`/`NtSet…` 의 번호), 클래스 `0x35` · `EPROCESS.InvertedFunctionTable` → [run51 해설](run51-eprocess-later-해설.md) 2.2 · 2.8
- 명부 한 줄 (`_LDR_DATA_TABLE_ENTRY`), `Flags` 의 `InExceptionTable` = "지번 색인엔 이미 올림" 표시 → [run35 해설](../02_sprint/run35-ntdll-entry-해설.md)
- KUSD = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 프로세스 힙 = **창고** → [run46 해설](run46-process-heap-해설.md)
- 빠른 실패 `INT 0x29` → [run24 해설](../02_sprint/run24-work-queue-해설.md) 2.3
- 사진 P1 · P1a → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

이번에 처음 나오는 말 (**이미지 확장 (확장 쪽)**, **클래스 `0xe`**, **ScpCfg 쪽**, **`0x117` "할 것 없음"**, **동적 함수 표 `_DYNAMIC_FUNCTION_TABLE`**, **로드 구성 · `GuardFlags` · 지연 가져오기**, **히스토리 표**, **클래스 6**) 은 본문에서 처음 나오는 자리에서 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소 **신청 창구**, 프로세스 = **가게**, ntdll = 가게 안 **관리실** (그리고 관리실이 든 건물), ntdll 전역 = 관리실 **칠판**.
- 모듈 항목 = 건물 명부의 **한 줄**, `Flags` = 그 줄에 찍는 **작은 도장들**.
- run 문서는 커널을 "**본사**", 확장 쪽을 "**창고**" 라고 불렀다. 이 글은 이렇게 바꿔 옮긴다. 가리키는 것은 같다.
  - 본사 → **관리사무소** (지금까지 해설 그대로).
  - 창고 → **별채**. "창고" 는 run46 해설부터 **프로세스 힙** 자리라서, 겹치지 않게 다른 말을 쓴다.
- "비상구 지도" 는 run 문서·run59·run60 해설과 같다: 함수 표 = 건물마다 붙은 **비상구 지도 목차**.

### 한 줄로 먼저 — 무엇을 얻었나

먼저 **핵심 줄기 하나**만 보자. 나머지는 다 이 줄기의 가지다.

```
① 관리사무소(커널)가 ntdll 건물 바로 뒤에 "별채"를 덧붙여 두고, 그 맨 앞 한 칸(0x1000)에 ScpCfg 판을 찍어 둔다
        │   (ntdll 을 가게 땅에 놓을 때, 커널 혼자)
        ▼
② 관리실(ntdll)이 창구에 묻는다: "우리 건물 뒤에 별채 있나요? 어디, 얼마나?"
        │   NtQueryVirtualMemory 클래스 0xe  → 답: {0, 0, 0x267000, 0x1000}
        ▼
③ 관리실이 별채 안 안내판(+0x14 = 0x2a4)을 읽어 "별채 비상구 지도"를 찾고,
   그걸 동적 함수 표 쪽지로 만들어 관리실 칠판 목록에 단다
        │   RtlAddGrowableFunctionTable
        ▼
④ 그 쪽지를 관리사무소 장부에도 올려 달라고 신청한다
        │   NtSetInformationProcess 클래스 0x35  → 커널 EPROCESS.InvertedFunctionTable
        ▼
⑤ 다 성공하면 ntdll 명부 줄에 "별채 지도 등록함" 도장 (Flags 비트 21)
```

| 질문 | 답 (run 문서 그대로) |
|---|---|
| 클래스 `0xe` 는 무엇을 묻나? | 이미지 뒤 **확장 쪽의 RVA·크기**. 커널 몸통은 `MiQueryImageExtensionInformation` `[정적]` (2.2 · 2.3) |
| ntdll 에 줄 값은? | 상태 `0`, 돌아온 길이 `0x18`, `+0x0` 0, `+0x4` 0, **`+0x8 = 0x267000`**, **`+0x10 = 0x1000`** `[정적]` (2.4) |
| PE_emulator 가 "성공, 출력 칸 안 씀" 을 주면? | `+0x10 = 0` → ntdll 은 `0x117` ("할 것 없음") 로 끝. 비트 21 안 켜짐 → `Flags 0xa2c4` (2.5) |
| 진짜 VM 은 어느 갈래? | **확장 있음.** ntdll 노드 `Flags 0x20a2c4`, 비트 21 켜짐 `[관찰]` → `NtSetInformationProcess(0x35)` 까지 성공 = **F7 거의 닫힘** `[정적 + 관찰]` (2.6) |
| `Flags` `0x8000` 은 누가? | `LdrpCfgProcessLoadConfig +0xc5` (`ProtectDelayLoad`) `[정적]` (2.8) |
| 지도의 `NtProtectVirtualMemory` 3자리는? | ntdll 노드에선 **하나도 안 난다**. 대신 `.mrdata` 열고 닫기가 넷 (2.10) |
| run60 `[추측]` 은? | 맞았다 → `[정적]`. 다만 "Add 가 Ensure 를 **안 탄다**" 는 해석은 틀림 — **늘 타고, 안에서 바로 0** (2.9) |
| 이 구간 syscall 은 몇 번? | **6번** (`LdrpInitializeProcess +0x119d` ~ `+0x127c`, ntdll 노드 길) `[정적]` (2.10) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

`LdrpInitializeProcess` 의 앞쪽 "살림 차리기" 안이다. 관리실이 **명부 첫 줄에 자기 이름을 쓰는** 바로 그 함수 (run35) 의 마무리 부분이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스·주소 공간·PEB·첫 스레드 만들기
 │        (ntdll 을 가게 땅에 놓을 때, 건물 뒤 별채도 같이 붙인다 — 커널 혼자)
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk                      (사진 P1)
 │    └─ LdrpInitializeProcess
 │         ├─ 묶음 3: 역함수 표 칸 0 에 ntdll 놓기, .mrdata 첫 잠금      (run60)
 │         ├─ +0x11a2 LdrpInitializeNtdllDataTableEntry  (명부 첫 줄 = ntdll)  (run35)
 │         │     └─ +0x124 LdrpProcessMappedModule [ntdll]   (마무리)
 │         │            ├─ +0x14a LdrpCfgProcessLoadConfig                ← 이번
 │         │            └─ +0x1a3 RtlpInsertOrRemoveScpCfgFunctionTable   ← 이번 (핵심)
 │         │                   └─ +0xc9 RtlAddGrowableFunctionTable      ← 이번
 │         ├─ …
 │         ├─ +0x127c RtlInitializeHistoryTable                          ← 이번
 │         └─ … exe 항목 → kernel32 → import DLL → TLS                   (run19 ~ run28)
 │  (사진 P1a)
 └─ … → NtContinue → exe 진입점 → main
```

### 왜 이 질문이 지금 왔나

- PE_emulator 가 진짜 ntdll 의 `LdrpInitializeNtdllDataTableEntry` 를 Unicorn 안에서 그대로 돌려 봤다.
- 그 안에서 나온 syscall 은 **딱 하나**: `NtQueryVirtualMemory(-1, 0x180000000, 0xe, &out, 0x18, &ret)`.
- PE_emulator 쪽 nt 엔 클래스 `0xe` 몸통이 없었다. 그래서 "성공, 출력 칸은 안 씀" 으로 대답했다.
- 그랬더니 ntdll 이 `RtlAddGrowableFunctionTable` → `NtSetInformationProcess(0x35)` 까지 **안 갔다**. 명부 줄 `Flags` 는 `0xa2c4`.
- PE_emulator 는 클래스 `0xe` 의 답 모양을 **지어낼 수 없어서** 이 길을 "정정 대기" 로 두고 물어 왔다.

### 무엇이 궁금했나

1. **(핵심)** 클래스 `0xe` 의 답은 어떤 모양이고, ntdll 에 줄 값은 무엇인가. 그 값으로 ntdll 의 갈래가 어떻게 갈리나.
2. 커널 쪽에서 클래스 `0xe` 를 받는 함수는 무엇인가.
3. `LdrpCfgProcessLoadConfig` 는 ntdll 노드에서 무엇을 하나. `Flags` 의 `0x8000` 은 누가 켜나.
4. `RtlAddGrowableFunctionTable` · `LdrEnsureMrdataHeapExists` 줄기, 쓰는 전역.
5. `RtlInitializeHistoryTable` 과 함수 항목 찾기 넷.
6. 클래스 6 답의 어느 칸을 읽나.

### 바퀴 전에 알던 것

- `[정적]` (run51) `RtlAddGrowableFunctionTable +0x14f` 에서 클래스 `0x35` 로 `NtSetInformationProcess` → 커널 `RtlInsertDynamicUserFunctionTable +0x20c` 가 `EPROCESS.InvertedFunctionTable` 을 만든다. test.exe 에서 **정말 탔는지는 `[공백]`** (F7).
- `[관찰]` (run44 사진) `EPROCESS.InvertedFunctionTable` 은 P1 ~ P1a 사이에 값이 생긴다. 그런데 `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` 는 끝까지 0 (run60).
- `[추측]` (요청서) 그 둘이 같이 맞으려면 — "Add 가 Ensure 를 안 타는 갈래로 갔다" 거나, "`InvertedFunctionTable` 은 다른 길이 썼다". 이번에 가를 수 있으면 갈라 달라고 했다.
- `[정적]` (run60) `RtlpxLookupFunctionTable` 은 `LdrInitState == 3` 이면 빠른 길, 아니면 클래스 6 으로 커널에 묻는다.
- `[관찰]` (PE_emulator, Unicorn — VM 관찰 아님) 덫 안 syscall 하나, `Flags 0xa2c4`.

---

## 2. 찾은 것

### 2.1 한눈에 — ntdll 노드 길 (대표만)

run 문서 "호출 관계" 를 줄였다. `+0x..` 는 ret 자리 (콜스택에 보이는 숫자).

```
LdrpProcessMappedModule [ntdll]
 ├─ +0x14a LdrpCfgProcessLoadConfig            +0xc5 Flags |= 0x8000 (ProtectDelayLoad)
 │                                             syscall 없음 → 반환 0
 │  +0x154 Flags |= 0x2000 (LoadConfigProcessed)
 ├─ +0x1a3 RtlpInsertOrRemoveScpCfgFunctionTable(ntdll, 1=넣기)
 │        ├─ +0x5d NtQueryVirtualMemory(-1, ntdll, 0xe, 0x18)   = {0, 0, 0x267000, 0x1000}
 │        │  +0x98 [ntdll+0x267000+0x14] = 0x2a4 ≠ 0
 │        └─ +0xc9 RtlAddGrowableFunctionTable(&h, 확장+0x2a4, 1, 1, 확장, 확장+0x1000)
 │                 ├─ +0x45  LdrEnsureMrdataHeapExists → CFG 꺼짐 → 바로 0
 │                 ├─ +0x100 RtlAllocateHeap(PEB.ProcessHeap, 0, 0x88)   새 쪽지
 │                 ├─ +0x154 NtSetInformationProcess(-1, 0x35, {쪽지, 0})  → 커널 InvertedFunctionTable
 │                 ├─ +0x165 .mrdata 열기 → 락 → 나무 둘 · 목록 끝 → 락 풀기
 │                 └─ +0x2a4 .mrdata 닫기 · *h = 쪽지
 │  +0x1a9 Flags 비트 21 (ScpInExceptionTable)   ← 반환이 정확히 0 일 때만
 └─ …
LdrpInitializeProcess
 └─ +0x127c RtlInitializeHistoryTable    .mrdata 열고 → 예외 함수 6개 찾아 적기 → ═▶ 닫기
```

모든 줄 `[정적]`. 진짜 VM 이 이 길 (확장 있음) 을 탔다는 건 2.6 의 `[관찰]` 이 받쳐 준다.

### 2.2 새 말 — 이미지 확장 (확장 쪽), 클래스 `0xe`

- **이미지 확장 (확장 쪽)** = 커널이 어떤 이미지를 땅에 놓을 때, 이미지 끝 **바로 뒤에 쪽 몇 개를 더 붙여** 둔 것 `[정적]` (`MiMapViewOfImageSection +0x172 ~ +0x1fa` 가 VAD 를 늘리고 표시를 심는다).
  - 비유: 관리사무소가 ntdll 건물을 땅에 세우면서 **건물 뒤에 별채를 덧붙여** 짓는다. 관리실이 지은 게 아니라서, 관리실은 별채가 있는지 **물어봐야** 안다.
  - 구획 장부 (VAD) 의 그 줄에는 "별채 붙음" 표시가 있다 — PDB 이름 `_MMVAD_FLAGS2.ImageMappingExtended` `[정적]`.
- **클래스 `0xe`** = `NtQueryVirtualMemory` 로 "이 이미지 뒤 확장 쪽이 어디부터, 얼마만큼이냐" 를 묻는 번호 `[정적]`.
  - 공개 이름 `MemoryImageExtensionInformation` 은 `[추측: 외부 이름]` — PDB 에 `MEMORY_INFORMATION_CLASS` 열거가 없어서 못 정했다.
  - 비유: 창구에 내는 질문지 "**우리 건물 뒤 별채 있나요?**"

#### 커널 쪽 길 `[정적]`

```
NtQueryVirtualMemory  +0x25
 └─ MmQueryVirtualMemory   +0x16f CMP R14D,0xe · +0x18b JMP RAX (클래스 표 15칸)
      └─ +0x274769 MiQueryImageExtensionInformation   ← 클래스 0xe 의 몸통
```

- `MmQueryVirtualMemory` 는 클래스 번호로 표를 찾아 **뛰어간다**. 클래스 `0xe` 와 6 의 몸통은 같은 함수 안 "떨어진 범위" (`+0x274348 ~`) 에 있다.
  - 그래서 오프셋이 `+0x274764` 처럼 크다. WinDbg 가 이 자리를 이 이름으로 보여 줄지는 `[추측]` — 멈춤점은 `MiQueryImageExtensionInformation` **입구**로 잡는 게 안전하다 (run 문서 "디버거 메모").

### 2.3 클래스 `0xe` 의 답 모양 — `0x18` 바이트, 칸 넷

이 답지는 **입력 겸 출력** 이다. 관리실이 앞칸에 "어떤 별채?" 를 적어 내면, 관리사무소가 뒤 칸 둘을 채워 돌려준다.

| 오프셋 | 크기 | 뜻 | 누가 채우나 |
|---|---|---|---|
| `+0x00` | 4 | 종류 (입력). ntdll 은 0 = `MiImageViewExtensionScpCfg` (PDB 열거 `_MI_IMAGE_VIEW_EXTENSION_TYPE`) | 입력 그대로 되돌려 씀 |
| `+0x04` | 4 | 예약. **0 이어야** 함 (아니면 `0xc000000d`) | 같이 되돌려 씀 |
| `+0x08` | 8 | **확장 시작 RVA** (이미지 시작 기준). 없으면 0 | 커널 계산 (2.4) |
| `+0x10` | 8 | **확장 크기**. 없으면 0 | 상수 `0x1000` (`+0x1ad`) |

모두 `[정적]`. 칸 이름 (`ExtensionType`·`Flags`·`ExtensionImageBaseRva`·`ExtensionSize`) 은 `[추측: 이름]` 이다.

#### 커널이 어떻게 대답하나 — 갈래 다섯 `[정적]`

| 상황 | 대답 |
|---|---|
| 그 주소를 덮는 구획 (VAD) 이 없음 | 실패 `0xc00000a0` |
| 구획은 있는데 이미지가 아님 | 실패 `0xc0000141` |
| 이미지인데 "별채 붙음" 표시가 꺼짐 | **성공 0, `+0x8 = +0x10 = 0`** |
| 표시는 켜졌는데 별채 쪽 PTE 가 0 | **성공 0, 둘 다 0** |
| 모두 통과 | **성공 0, `+0x8` = 확장 RVA, `+0x10 = 0x1000`** |

- 여기서 중요한 것: **"별채 없음" 도 실패가 아니라 성공(0)** 이다. 칸 두 개가 0 일 뿐이다.
- 그래서 관리실은 상태값만 봐서는 모른다. **크기 칸 (`+0x10`) 을 봐야** 안다. (2.5)

### 2.4 ntdll 에 줄 값 — `0x267000` 과 `0x1000`

`+0x8` 은 이렇게 계산된다 `[정적]` (`MiGetImageExtensionBaseAddress`, `MiQueryImageExtensionInformation +0x1a1 ~ +0x1a8`):

1. 구획 시작 + (`Segment.TotalNumberOfPtes` × 쪽 크기) = **이미지 끝 바로 뒤**.
2. 거기에 표 `DAT[종류]` 를 더한다. 종류 0 (ScpCfg) 의 `DAT[0]` 은 **늘 0** 이다 (쓰는 줄이 `AND … ,0` 하나뿐).
3. 구획 시작을 빼서 RVA 로 만든다.

ntdll 에 넣어 보면:

- ntdll `SizeOfImage` = `0x267000` (쪽 `0x267` 개 = `TotalNumberOfPtes`).
- 그래서 **`+0x8 = 0x267000 + 0 = 0x267000`** — "건물 끝 바로 뒤가 별채 입구" 다.
- **`+0x10 = 0x1000`** — 별채는 **한 쪽** (4KB).
- 상태 `0`, 돌아온 길이 `0x18`.

모두 `[정적]` 으로 정해진다. 값 자체를 디버거로 직접 본 건 아니다 (동적 1 · 2).

### 2.5 관리실은 답을 어떻게 읽나 — `RtlpInsertOrRemoveScpCfgFunctionTable`

#### 한 줄 읽기 — `LdrpProcessMappedModule` call `+0x19e` / ret `+0x1a3`

- **누가**: `LdrpProcessMappedModule` (매핑된 모듈을 마무리하는 함수. 이번엔 ntdll 노드).
- **어디서**: call `+0x19e`, 돌아올 자리 `+0x1a3`. 바로 앞 `+0x198 MOV R8B,1` 로 "넣기" 를 고른다.
- **누구를**: `RtlpInsertOrRemoveScpCfgFunctionTable(이미지 시작, 1)`.
- **언제만**: 명부 줄 비트 21 이 아직 꺼져 있고, 인자 비트 23 이 꺼져 있을 때 (`+0x185 ~ +0x196`).
- **무엇을 위해**: "별채가 있으면 그 비상구 지도를 등록해라". 돌아온 값이 **정확히 0** 이면 `+0x1a9 BTS [RSI],0x15` 로 비트 21 도장을 찍는다.

모두 `[정적]`.

#### 새 말 — `0x117` "할 것 없음"

- 이 함수가 돌려주는 값은 셋이다 `[정적]`: `0` = 넣기(또는 빼기) 했음 · **`0x117`** = 할 게 없음 · 음수 = 실패.
- `0x117` 은 **양수** 라서 실패가 아니다. caller 는 성공처럼 계속 간다. 다만 비트 21 도장만 **건너뛴다** (`+0x1a7 JNZ +0x203` → `+0x20c JNS +0x1ad`).
- 비유: 창구 직원이 "별채 없으니 등록할 것도 없어요" 하고 돌려보낸 것. 혼난 게 아니라, 그냥 할 일이 없었던 것.

#### 갈래 — 위에서 아래로 `[정적]`

| # | 보는 것 (줄) | 결과 |
|---|---|---|
| 1 | 상태가 `0xc00000bb` (`+0x5d`) | `0x117` |
| 2 | 그 밖 음수 (`+0x84 JS`) | 그대로 반환 → 모듈 적재 실패 |
| 3 | **크기 칸 `+0x10`** 이 0 (`+0x86` · `+0x8e JZ +0x64`) | `0x117` |
| 4 | **별채 안 `+0x14` 의 dword** 가 0 (`+0x90 ~ +0x9d JZ +0x64`) | `0x117` |
| 5 | 넣기면 (`+0xa6`) | `RtlAddGrowableFunctionTable` (`+0xc4/+0xc9`) |

- 4번이 재밌다. 관리실은 별채가 있다는 답을 받으면 **별채 안을 직접 들여다본다**. 입구에서 `+0x14` 자리의 안내판을 읽고, 그게 0 이면 역시 "할 것 없음" 이다.
- 5번에서 넘기는 여섯 인자 (run 문서 질문별 답 1):
  - 핸들 칸 `RSP+0x38` (받은 핸들은 **버린다**)
  - 함수 표 = `별채 + [별채+0x14]` = 별채 + `0x2a4`
  - 개수 1, 최대 개수 1
  - 범위 시작 = 이미지 + `+0x8` (= 별채 입구), 범위 끝 = 시작 + `+0x10`

#### PE_emulator 의 갈래가 여기서 달라졌다

- PE_emulator 는 "성공, 출력 칸 안 씀" 을 줬다. ntdll 은 출력 칸을 미리 0 으로 채워 두고 부른다 (`+0x28 MOVUPS` 등).
- 그러니 `+0x10` 은 0 그대로 → 3번 갈래 → `0x117` → 비트 21 안 찍힘 → `Flags 0xa2c4`.
- 요청서가 본 "덫 안 syscall 하나뿐" 이 바로 이 갈래다. **틀린 게 아니라 다른 갈래** 를 탄 것이다.

### 2.6 진짜 VM 은 "별채 있음" 갈래였다 — 왜 그렇게 말하나

디버거로 이 함수를 직접 멈춰 본 적은 없다. 그런데도 "탔다" 고 말할 수 있는 까닭을 단계별로 보자.

1. **근거** `[관찰]` run44 사진 P1a: ntdll 노드 `Flags = 0x20a2c4`, `ScpInExceptionTable : 0y1` (`04_baggage-read_P1a.txt:2795`·`:2817`).
   - `0x20a2c4` = PE_emulator 의 `0xa2c4` + `0x200000` (비트 21).
2. **그래서** `[정적]` 비트 21 은 `LdrpProcessMappedModule +0x1a9` **한 곳**에서, `RtlpInsertOrRemove…` 가 **정확히 0** 을 돌려줬을 때만 켜진다. `0x117` 이면 건너뛴다.
3. **그래서** `[정적]` 0 이 나오려면 2.5 표의 3 · 4번을 통과하고 5번 `RtlAddGrowableFunctionTable` 이 성공해야 한다. 즉 클래스 `0xe` 가 `+0x10 ≠ 0` 을 줬고, 별채 `+0x14 ≠ 0` 이었다.
4. **그래서** `[정적]` `RtlAddGrowableFunctionTable` 이 성공했다는 건 그 안 `NtSetInformationProcess(0x35)` 가 성공했다는 뜻이다. 반환값 = 그 syscall 상태 (`+0xbc MOV EAX,EDI`). 실패면 `+0x230` 으로 빠져 음수 → 비트 21 안 찍힘.
5. **다른 근거 하나 더** `[관찰]`: ntdll 구획 (VAD) `7ffafb660 ~ 7ffafb8cb` (`:1551`, `0x26c` 쪽) 는 `lm` 이미지 끝 `7ffa'fb8c7000` (`:59`, `0x267` 쪽) 보다 **5쪽 더 길다**. 이 5쪽이 별채 전체다 (ScpCfg 1쪽 + HPAT 4쪽 `[추측: DAT[3] = 0x5000]`). 클래스 `0xe` 가 알려 준 건 그 **맨 앞 ScpCfg 한 쪽** (`+0x267000`, `0x1000`) 이다.
6. **결론** `[정적 + 관찰]`: 이 프로세스에서 ntdll 은 별채 있음 갈래를 탔고, 쪽지를 관리사무소 장부에도 올렸다.
   - → **F7 ("`InvertedFunctionTable` 이 클래스 `0x35` 길로 왔나") 은 정적으로 거의 닫혔다.**
7. **아직 확인 아님**:
   - 커널 장부 (풀 블록) 의 **내용**을 직접 본 건 아니다. 항목이 몇 개인지 (3개인가) 는 동적 3 (Q104 VM 단계, F7 = Q79 출처).
   - kernel32·kernelbase 명부 줄도 비트 21 이 켜져 있다 (baggage 7.2). 그래서 **같은 길을 세 번** 탄 것으로 보인다 `[추측: 개수]`.

비유로: 관리실 명부 줄에 "별채 지도 등록함" 도장이 찍혀 있었다. 이 도장은 **등록이 다 끝났을 때만** 찍는 도장이다. 그러니 창구에 등록 신청도 성공했다고 읽을 수 있다. 다만 관리사무소 장부를 직접 펼쳐 본 건 아니다.

test.exe 명부 줄은 반대다 `[관찰]`: `Flags 0x22c4`, 비트 21 꺼짐 (`:2690`·`:2712`), 구획도 정확히 `0x8c` 쪽 (별채 없음). 별채가 붙으려면 이미지가 `ImageExpansionCompatible` 이어야 한다 (2.7).

### 2.7 별채 안에는 무엇이 있나 — ScpCfg 쪽

#### 새 말 — ScpCfg 쪽

- **ScpCfg 쪽** = 커널이 별채 한 쪽 (`0x1000`B) 에 채워 넣는 내용 `[정적]`. ntdll 파일 안 구역 넷 (`SCPCFG`·`SCPCFGNP`·`SCPCFGES`·`SCPCFGFP`, 각 `0x2b0`B) 중 **하나를 복사** 하고, 나머지를 `0xcc` 로 채운다 (`PsInitializeScpCfgPages`).
  - 어느 구역을 고르나 = 쪽 **형** (PDB `_RTL_SCP_CFG_PAGE_TYPE`): 0 `Nop` · 1 `Default` · 2 `ExportSuppression` · 3 `Fptr`.
  - "Scp" 가 무엇의 줄임말인지는 근거에 없다. 모른다.
  - 비유: 관리사무소가 ntdll 건물 안에 미리 넣어 둔 **본뜬 판 네 장** 중 하나를 골라 별채 벽에 그대로 찍어 놓는다.

#### 네 판의 공통점 `[정적]`

- 네 구역 모두 머리가 같다: dword `0x40, 0xc0, 0x140, 0x1c0, 0x240, 0x2a4`.
  - 그래서 **`+0x14` = `0x2a4`** — 2.5 의 4번 안내판 값이다.
- `+0x2a4` 의 12B 가 함수 항목 하나: `{BeginAddress 0, EndAddress 0x280, UnwindData 0x298}`.
- 그래서 관리실이 만드는 함수 표 = **`별채 + 0x2a4`, 개수 1, 범위 `[별채, 별채 + 0x1000)`**.
- 비유: 별채 벽에 붙은 안내판 (`+0x14`) 이 "비상구 지도는 `0x2a4` 쪽에 있음" 이라고 알려 준다. 그 지도엔 줄이 **한 줄** 뿐이다.

#### test.exe 는 형 0 (Nop) 으로 보인다 `[정적 추정]`

- `PsGetScpCfgPageTypeForProcess` 는 프로세스 `MitigationFlags` 비트 0 (CFG 켜짐) 이 꺼져 있으면 0 을 준다 `[정적]`.
- test.exe 는 `+0x750 MitigationFlags : 0x20` (비트 0 꺼짐), `LdrSystemDllInitBlock+0xb8 = 0` (사진) — 맞아 떨어진다.
- 형 0 판의 처음 네 부품은 `48 ff e0` (jmp rax) ×2, `c3` (ret) ×2 다 `[정적]`.
  - 이름 "Nop" 과 부품 모양 (그냥 넘겨주기 · 그냥 돌아가기) 으로 보면 **검사 없이 통과시키는 판** 같다 `[추측]` — 이유: CFG 가 꺼진 프로세스에 고르는 형이라서. 근거 문서는 뜻까지는 적지 않았다.
- 동적에서 별채 `+0x40` 이 `48 ff e0` 인지로 가를 수 있다 (동적 1).

#### 왜 ntdll 에만 별채가 붙나 — 조건만 `[정적]`, 까닭은 모름

- 별채가 붙는 조건 (`MiMapViewOfImageSection +0x172 ~ +0x1fa`): 부팅 전역 `[0xe2d8ec] ≠ 0` · 이미지가 **`ImageExpansionCompatible`** · 뷰가 세그먼트 전체 · 프로세스 쪽 플래그 둘 등.
  - **`ImageExpansionCompatible`** = 이미지 정보 (`_MI_EXTRA_IMAGE_INFORMATION +0xc`) 비트 6 의 PDB 이름. 이름대로라면 "뒤에 별채를 붙여도 되는 이미지" 표시다 `[추측: 뜻은 이름에서 읽음]`.
- ntdll 이미지가 **왜** 이 표시를 갖는지는 이번에 못 닫았다 → Q121 (a). VM 결과가 이미 "별채 있음" 이라 **ntdll 값에는 영향 없다**.

### 2.8 `LdrpCfgProcessLoadConfig` — 도장 `0x8000` 하나, syscall 은 없음

#### 새 말 — 로드 구성 · `GuardFlags` · 지연 가져오기

- **로드 구성 디렉터리** = PE 파일 안의 설정 표. 여기엔 `GuardFlags`, `DependentLoadFlags`, CFG 교환원 칸 주소 같은 값이 들어 있다 `[정적]` (ntdll 파일 값을 결과서가 읽음).
  - 비유: 건물 설계도 뒤에 붙은 **보안 설정 서류**.
- **`GuardFlags`** = 그 서류의 보안 옵션 비트 묶음. ntdll 은 `0x10417500` `[정적]` (비트 이름은 SDK 기억 `[추측: 이름]`).
- **지연 가져오기 디렉터리** (PE 디렉터리 13) = 일반적으로는 "처음 부를 때까지 연결을 미뤄 두는 가져오기 목록" 으로 알려져 있다 `[추측: 일반적으로 알려진 설명]`. ntdll 엔 **없다** (디렉터리 0·2·3·4·5·6·10 만) `[정적]`.

#### 한 줄 읽기 — `LdrpCfgProcessLoadConfig +0xc5 OR [RSI+0x68],0x8000`

- **누가**: `LdrpCfgProcessLoadConfig` (`LdrpProcessMappedModule` call `+0x145` / ret `+0x14a` 가 부름).
- **어디서**: `+0xb5 TEST [RDI+0x90],0x1000` → `+0xbf JZ +0x16f` 를 안 타면 → `+0xc5`.
- **누구를**: 부르기가 아니라 쓰기다. `RSI` = 명부 줄, `+0x68` = `Flags`.
- **무엇을 위해**: 보안 서류의 `GuardFlags` 에 `0x1000` 이 있으면 명부 줄에 **`ProtectDelayLoad`** (비트 15, PDB 이름) 도장을 찍는다.

모두 `[정적]`. PE_emulator 가 물은 "`0x8000` 은 누가 켜나" 의 답이 이 한 줄이다.

#### ntdll 노드가 지나가는 길 `[정적]`

1. 로드 구성 있음 (`+0x77`), 크기 ≥ `0x94` (`+0x80`).
2. `DependentLoadFlags` 를 마스크로 걸러 명부 줄 `+0x118` 에 (`+0x8c ~ +0xa7`) → `LdrpIsModuleUnderSystem32` 를 부르는데 ntdll 에선 **거짓** (사진 `DependentLoadFlags 0x800` 이 그대로라서 `[관찰]`. 왜 거짓인지는 Q121 (c)).
3. `+0xc5` `0x8000` 도장.
4. 지연 가져오기 디렉터리 없음 → `+0x16a NtProtectVirtualMemory` 안 탐.
5. `+0x187` (ret `+0x18c`) `LdrControlFlowGuardEnforced` = **0** → CFG 갈래 (`+0x4be`·`+0x535` `NtProtectVirtualMemory`) 안 탐.
6. 반환 0. 돌아가면 caller 가 `+0x154` 에서 `0x2000` (`LoadConfigProcessed`) 도장.

→ **syscall 없음.** 지도에 있던 `NtProtectVirtualMemory` 세 자리는 ntdll 노드에선 하나도 안 난다.

#### `LdrControlFlowGuardEnforced` — 한 줄만 `[정적]`

- 25바이트 잎 함수. 업무 수첩 `LdrSystemDllInitBlock +0xb8` (CFG 비트맵 주소 = 교환원 **출입 명단** 위치) 이 0 이면 0.
- test.exe 는 `+0xb8 = 0` (짐 지도 사진) → **CFG 꺼짐**.
- 비유: 출입 명단이 아예 없으니 교환원도 없다. 교환원이 해야 할 일 (CFG 갈래) 은 전부 건너뛴다.

#### `Flags 0xa2c4` 를 도장 하나하나로 `[정적]` (+ 비트 21 `[관찰]`)

| 비트 | PDB 이름 | 찍는 곳 |
|---|---|---|
| `0x4` | `ImageDll` | `LdrpAllocateModuleEntry +0x11a` (run35), `+0x93` 이 한 번 더 |
| `0x40` | `InLegacyLists` | `LdrpInsertDataTableEntry +0x5e` (run35) |
| `0x80` | `InIndexes` | `LdrpInsertModuleToIndexLockHeld +0xec` (run35) |
| `0x200` | `InExceptionTable` | `LdrpInitializeNtdllDataTableEntry +0x93` (run35) |
| `0x2000` | `LoadConfigProcessed` | `LdrpProcessMappedModule +0x154` |
| `0x8000` | `ProtectDelayLoad` | `LdrpCfgProcessLoadConfig +0xc5` |
| `0x200000` | `ScpInExceptionTable` (VM 만) | `LdrpProcessMappedModule +0x1a9` |

- 합 `0x4 + 0x40 + 0x80 + 0x200 + 0x2000 + 0x8000 = 0xa2c4` (PE_emulator), 여기에 `+ 0x200000 = 0x20a2c4` (VM) `[정적 + 관찰]`.
- 이 맞아떨어짐이 결과서에겐 "스크립트 오프셋도, 파일 읽기도 믿어도 된다" 는 버전 대조 역할도 했다.
- 비유: run35 에서 본 `InExceptionTable` 이 "**지번 색인엔 이미 올림**" 도장이었다면, `ScpInExceptionTable` 은 "**별채 지도도 올림**" 도장이다.

### 2.9 `RtlAddGrowableFunctionTable` — 쪽지를 만들고, 창구에 올리고, 칠판에 단다

#### 새 말 — 동적 함수 표 `_DYNAMIC_FUNCTION_TABLE`

- 정식 건물 (이미지) 의 비상구 지도는 **지번 색인** (역함수 표) 이 찾아 준다 (run60).
- 그런데 별채처럼 **색인에 없는 공간** 에도 지도가 필요할 수 있다. 그때 쓰는 게 **동적 함수 표** — "이 주소 범위의 비상구 지도는 여기" 를 적은 항목 `[정적]` (PDB `_DYNAMIC_FUNCTION_TABLE`, `0x88`B).
  - 비유: 색인 책 뒤에 따로 끼워 두는 **덧붙임 쪽지**. 관리실은 이 쪽지들을 칠판의 **목록** 하나와 **빨리 찾기용 나무 둘** 에 같이 달아 둔다.

#### 쪽지 칸 (이 함수가 채우는 것) `[정적]`

| 칸 | PDB 이름 | 값 |
|---|---|---|
| `+0x00/+0x08` | `ListEntry` | 목록 끝에 달 때 |
| `+0x10` | `FunctionTable` | 함수 표 (= 별채 + `0x2a4`) |
| `+0x18` | `TimeStamp` | `NtQuerySystemTime` — 실제로는 KUSD `0x7ffe0014` 읽기, **syscall 아님** |
| `+0x20` / `+0x28` | `MinimumAddress` / `MaximumAddress` | 범위 시작 / 끝 |
| `+0x30` | `BaseAddress` | 범위 시작 |
| `+0x50` | `Type` | **3 = `RF_KERNEL_DYNAMIC`** (`+0x13a`) |
| `+0x54` | `EntryCount` | 1 |
| `+0x58` / `+0x70` | `TreeNodeMin` / `TreeNodeMax` | 나무 둘에 넣을 마디 |

`+0x38 ~ +0x48` (콜백 쪽) 은 안 쓴다. 할당이 플래그 0 이라 0 으로 안 채워진다 — 힙 값 그대로.

#### 순서 — 단계별로 `[정적]`

1. 인자 검사: 개수 ≤ 최대 (`+0x24`·`+0x27`), 범위 시작 < 끝 (`+0x3a`). 어기면 빠른 실패 `INT 0x29` 코드 5.
2. call `+0x40` (ret `+0x45`) `LdrEnsureMrdataHeapExists` — **조건 없이 늘**. test.exe 는 안에서 바로 0 (2.10 에서 자세히).
3. 힙 고르기: `+0xdf` `LdrControlFlowGuardEnforced` → 0 → `+0xee JZ +0x20a` → **`PEB.ProcessHeap`** (창고).
4. call `+0xfb` (ret `+0x100`) `RtlAllocateHeap(창고, 0, 0x88)` → 새 쪽지.
5. 쪽지 칸 채우기 (위 표).
6. call `+0x14f` (ret `+0x154`) **`NtSetInformationProcess(-1, 0x35, {쪽지 주소, 바이트 0}, 0x10)`** — 이것도 **조건 없이 늘** (쪽지를 얻은 뒤). 커널은 `RtlInsertDynamicUserFunctionTable +0x20c` 로 `EPROCESS.InvertedFunctionTable` 을 만든다 (run51).
7. 실패면 (`+0x158 JS +0x230`) 쪽지를 풀고 상태를 돌려준다. 목록엔 안 단다.
8. 성공이면: call `+0x160` (ret `+0x165`) `.mrdata` 덮개 열기 → 락 `RtlpDynamicFunctionTableLock` 잡기 → `RtlAvlInsertNodeEx` ×2 (`TreeMin`·`TreeMax`) → 목록 `RtlpDynamicFunctionTable` 끝에 달기 (끝 검사 어기면 `INT 0x29` 코드 3) → 락 풀기 → call `+0x29f` (ret `+0x2a4`) 덮개 닫기 → `*h = 쪽지`.

- 비유: 관리실 직원이 창고에서 쪽지 한 장을 꺼내 별채 지도 위치를 적는다. 먼저 **창구에 "이것도 장부에 올려 주세요"** 를 신청하고, 받아들여지면 그때 칠판 덮개를 열고 목록과 정리함 둘에 꽂은 뒤 덮개를 닫는다.
- 쪽지가 **관리실 칠판 (유저)** 과 **관리사무소 장부 (커널)** 양쪽에 걸린다는 점이 핵심이다. 짐 지도 4장에 짝으로 적혔다 (3절).

### 2.10 run60 정정 — "Ensure 를 안 탄다" 가 아니라 "타지만 안에서 바로 나온다"

헷갈리기 쉬운 곳이라 천천히 보자.

#### 사진이 보여 준 두 사실 `[관찰]`

- `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` 는 **끝까지 0** (run60).
- `EPROCESS.InvertedFunctionTable` 은 P1 ~ P1a 에 **값이 생김** (run51).

#### 예전 짐작 둘

- run60 `[추측]`: "test.exe 에선 `LdrpMrdataHeap` 쓰기까지 안 간 것", "`LdrEnsureMrdataHeapExists +0xa` 의 CFG 확인도 그 갈래일 것".
- 그 뒤 요청서·flow.md 가 이걸 이렇게 읽었다: "**`RtlAddGrowableFunctionTable` 이 `LdrEnsureMrdataHeapExists` 를 안 부르는 갈래** 로 갔다". 아니면 "`InvertedFunctionTable` 은 다른 길이 썼다".

#### 이번에 본 코드 `[정적]`

1. `RtlAddGrowableFunctionTable +0x40` (ret `+0x45`) 은 인자 검사 (`+0x27`/`+0x3a`) 바로 뒤에서 **늘** `LdrEnsureMrdataHeapExists` 를 부른다. 앞에 CFG 갈림이 없다.
2. 갈림은 **그 안** 에 있다: `LdrEnsureMrdataHeapExists +0xa` (ret `+0xf`) `LdrControlFlowGuardEnforced` → `+0x11 JZ +0x1f` → `+0x1f XOR EAX,EAX` → **바로 0 반환**.
3. test.exe 는 CFG 꺼짐 (2.8) → 2번에서 끝. `NtAllocateVirtualMemory`·`RtlpCreateHeap`·`LdrpMrdataHeap` 쓰기 (`+0xef`) 까지 **안 간다**.
4. 그리고 Add 는 계속 가서 창고에 쪽지를 만들고 `0x35` 를 신청한다 (2.9).

#### 그래서

- run60 의 두 `[추측]` 은 **맞았다** → `[정적]`.
- "Add 가 Ensure 를 **안 탄다**" 는 해석은 **틀렸다**. 타긴 탄다. 들어가자마자 나온다.
- 사진의 두 사실은 **이 한 갈래로 같이** 설명된다. "다른 길이 `InvertedFunctionTable` 을 썼다" 는 가설은 필요 없다.

비유: 관리실 직원이 "칠판 전용 작은 창고 (CFG 용 힙)" 방 문을 **열고 들어가긴 한다**. 그런데 방 안 첫 줄에 "출입 명단 없음 → 할 일 없음" 이라고 적혀 있어서 **곧바로 나온다**. 예전엔 "방 앞에서 돌아섰다" 고 읽었던 것이다. 결과 (작은 창고는 안 생김) 는 같지만, **방에 들어간다는 사실**은 다르다.

- PE_emulator 쪽에서 이 차이가 중요한 까닭: 진짜 ntdll 을 돌리면 `LdrEnsureMrdataHeapExists` 가 **실제로 불린다**. 그 안의 첫 확인 (`LdrControlFlowGuardEnforced`) 이 0 을 주기만 하면 된다.

### 2.11 `.mrdata` 열고 닫기 넷, 그리고 syscall 6번

#### syscall 표 — ntdll 노드 길 (`LdrpInitializeProcess +0x119d` ~ `+0x127c`) `[정적]`

| # | 자리 (call/ret) | NtXxx | 조건 |
|---|---|---|---|
| 1 | `RtlpInsertOrRemoveScpCfgFunctionTable +0x58/+0x5d` | `NtQueryVirtualMemory` (클래스 `0xe`) | 비트 21 꺼짐 · 인자 비트 23 꺼짐 |
| 2 | `RtlAddGrowableFunctionTable +0x14f/+0x154` | `NtSetInformationProcess` (클래스 `0x35`) | #1 이 별채 있음 · 쪽지 할당 성공 |
| 3 | 〃 `+0x160/+0x165` → `LdrProtectMrdata(0)` | `NtProtectVirtualMemory` (`PAGE_READWRITE`) | 숫자판 0 → 1 |
| 4 | 〃 `+0x29f/+0x2a4` → `LdrProtectMrdata(1)` | `NtProtectVirtualMemory` (`PAGE_READONLY`) | 1 → 0 |
| 5 | `RtlInitializeHistoryTable +0x17/+0x1c` → `LdrProtectMrdata(0)` | `NtProtectVirtualMemory` (4) | 0 → 1 |
| 6 | 〃 `+0xb4` (꼬리 `JMP`) → `LdrProtectMrdata(1)` | `NtProtectVirtualMemory` (2) | 1 → 0 |

- 순서: #1 → #2 → #3 → #4 → (`+0x11a2 ~ +0x1277` 는 이번 범위 밖) → #5 → #6.
- 숫자판은 run60 에서 본 대로 **`+0x488` 에서 이미 0** 이 됐다. 그래서 여기 두 쌍은 매번 0 ↔ 1 을 넘고, 매번 창구에 간다. 쌍마다 2번, 두 쌍이라 4번.
- **확장 없음 (PE_emulator 지금)** 이면 #1 하나만 나고 끝이다.
- PE_emulator 가 숫자판 `LdrpMrdataUnprotected` 를 1 로 두면 (R4, 진짜는 0): #3 · #5 는 2 로 오르기만 하고 창구에 안 간다. 진짜와 syscall 수가 달라진다.

#### 안 나오는 것 (test.exe) `[정적]`

- `LdrpCfgProcessLoadConfig` 의 `NtProtectVirtualMemory` 셋 (2.8).
- `LdrEnsureMrdataHeapExists` 의 `NtAllocateVirtualMemory`·`NtFreeVirtualMemory` (2.10).
- `NtQuerySystemTime` — 이름은 Nt 지만 `RtlQuerySystemTime` 로 KUSD 를 읽을 뿐 (2.9).
- 히스토리 표의 여섯 찾기에서 클래스 6 (2.12).

### 2.12 히스토리 표와 함수 항목 찾기 — 질문 5 · 6

#### 새 말 — 히스토리 표 `RtlpUnwindHistoryTable`

- 예외가 나면 "이 주소는 어느 함수의 비상구 지도?" 를 자주 찾는다. 특히 **예외 처리 자체를 하는 함수들** 은 거의 매번 나온다.
- **히스토리 표** = 그런 함수 6개의 지도 위치를 **미리 찾아 적어 둔 표** `[정적]` (`.mrdata` `0x1e7350`, `0xd8`B). 이름 `_UNWIND_HISTORY_TABLE` 은 PDB 에 없어 칸 이름은 `[추측: 이름]`.
  - 비유: 칠판 구석의 **"자주 찾는 비상구" 메모**. 색인 책을 매번 펴지 않게 단골 여섯 개만 따로 적어 둔다.

#### `RtlInitializeHistoryTable` 이 하는 일 (`LdrpInitializeProcess` call `+0x1277` / ret `+0x127c`) `[정적]`

1. 덮개 열기 (`+0x17/+0x1c`).
2. `RtlpFunctionAddressTableEntry(i)` 를 i = 0, 1, … 로 부른다 (`+0x29/+0x2e`, **7번** — 마지막은 0 을 줘서 끝). 주는 함수 6개: `RtlUnwindEx` · `__C_specific_handler` · `RtlpExecuteHandlerForException` · `RtlDispatchException` · `RtlRaiseException` · `KiUserExceptionDispatcher`.
3. 하나마다 `RtlLookupFunctionEntry(pc, &시작, NULL)` (`+0x50/+0x55`, **6번**) 로 함수 항목을 찾아 칸에 적는다 (`+0x69`·`+0x6e`), 아래·위 한계도 갱신 (`+0x7c`·`+0x8c`).
4. `RtlRaiseException` 이면 그 순번을 `GlobalHint` 에 (`+0x3f`) → **4**.
5. 개수 **6** (`+0x9f`).
6. 꼬리 `JMP` (`+0xb4`) 로 덮개 닫기.

#### 왜 클래스 6 으로 커널에 안 묻나 — 단계별로

1. run60: `RtlpxLookupFunctionTable` 은 `LdrInitState ≠ 3` 이면 클래스 6 으로 커널에 묻는다 `[정적]`.
2. `+0x1277` 시점의 `LdrInitState` 는 **0** 이다 (`_LdrpInitialize +0x101` 이 0 으로 두고, 1 이 되는 건 한참 뒤 `LdrpInitializeProcess +0x1a4c`) `[정적]`.
3. 그러면 커널에 물을 것 같지만 — **`RtlLookupFunctionEntry` 가 그보다 먼저 지름길을 탄다** `[정적]`: `+0x21` 에서 지번 색인 **칸 0** (= ntdll, run60 이 묶음 3 에서 채운 관리실 전용 칸) 을 읽고, pc 가 ntdll 안이면 (`+0x3a JNC +0x1bf`) 그 칸을 그대로 쓴다.
4. 여섯 함수가 모두 ntdll 안이다 → 여섯 번 다 지름길 → `RtlpxLookupFunctionTable` 도, 클래스 6 도 **안 거친다**.
5. 아직 확인 아님: 칸 0 이 그때 정말 채워져 있다는 건 run60 의 정적 전제다. 동적 4 에서 `RtlpxLookupFunctionTable+0x2d5` 가 이 사슬에서 **안 걸리는지** 로 볼 수 있다.

비유: 단골 메모를 쓰려고 "이 비상구 어디?" 를 여섯 번 찾는데, 여섯 개가 다 **관리실 건물 안** 이다. 관리실 전용 칸만 보면 바로 나오니 창구까지 갈 일이 없다.

#### `RtlpLookupDynamicFunctionEntry` — 덧붙임 쪽지 찾기 `[정적]`

- `RtlLookupFunctionEntry` 가 색인에서 못 찾았을 때만 간다 (`+0x53 JZ +0x34c`).
- 공유 락을 잡고 **나무 넷만** 본다 (콜백 나무 Min·Max, 함수 나무 Min·Max). **목록은 안 걷는다.**
- 다 비었거나 못 찾으면 NULL. `_LdrpInitialize +0x10d ~ +0x13e` 가 나무·목록·락을 비워 둔 직후엔 당연히 NULL.
- 2.9 에서 Add 는 쪽지를 **목록과 나무 둘 다** 에 단다. 찾는 쪽은 그중 **나무만** 본다 `[정적]`. 그래서 나무는 찾기용으로 보인다 `[추측]` — 이유: 찾는 함수가 읽는 게 나무뿐이라서. 목록을 읽는 곳은 이번 범위에서 안 나왔다.

#### 새 말 — 클래스 6 (질문 6) `[정적]`

- **클래스 6** = `NtQueryVirtualMemory` 로 "이 주소가 든 **이미지의 시작·크기·성질**" 을 묻는 번호. 공개 이름 `MemoryImageInformation` 은 `[추측: 외부 이름]`. 답 `0x18`B.
  - 비유: 창구 질문지 "**이 번지는 어느 건물이고, 건물 크기는?**"
- ntdll (`RtlpxLookupFunctionTable +0x2d0`) 이 읽는 칸:
  - `+0x00` 이미지 시작 — 0 이거나 pc 보다 뒤면 "못 찾음".
  - `+0x08` 이미지 크기 (낮은 dword).
  - `+0x10` 플래그 — 비트 0 이나 1 이 켜져 있으면 "못 찾음". (비트 6 = `ImageMappingExtended` — 2.2 의 "별채 붙음" 표시가 여기에도 나온다.)
- 커널 몸통은 따로 함수가 없고 `MmQueryVirtualMemory` 안 (`+0x274a66 ~`).
- 이걸 실제로 타는 곳은 `LdrpInitializeProcess +0x43a` (run60, 칸 0 채울 때) 뿐. 이번 구간에선 안 탄다.

### 2.13 결과서와 run 문서가 다르게 적은 곳

뜻은 같고 숫자·태그만 다르다. 이 글은 run 문서를 따랐다.

- `RtlAddGrowableFunctionTable` 첫 덮개 열기: 결과서 `+0x15e/+0x165` → run 문서 **call `+0x160` / ret `+0x165`**. `+0x15e` 는 인자 0 을 만드는 `XOR ECX,ECX` 줄이다.
- `Flags` `0x40`·`0x80`: 결과서는 `[추측: 본문 안 읽음]` → run 문서는 run35 가 이미 본 줄로 `[정적]` (`LdrpInsertDataTableEntry +0x5e` · `LdrpInsertModuleToIndexLockHeld +0xec`).
- `ImageDll` `0x4`: 결과서는 `+0x93` 만 → run 문서는 `LdrpAllocateModuleEntry +0x11a` 가 먼저 켜고 `+0x93` 은 다시 켠다고 둘 다 적음.

### 2.14 정리 — 비유로 한 번 더

1. 관리사무소는 ntdll 건물을 땅에 세우며 **뒤에 별채 한 칸** 을 붙이고, 건물 안 **본뜬 판** 하나 (test.exe 는 "Nop" 판으로 보임) 를 별채 벽에 찍어 둔다.
2. 관리실은 명부 첫 줄을 마무리하면서 먼저 **보안 설정 서류** 를 읽고 `0x8000` 도장 · `0x2000` 도장을 찍는다. 교환원은 없으니 덮개 신청은 없다.
3. 그다음 창구에 "**우리 건물 뒤 별채 있나요?**" (클래스 `0xe`) 를 묻는다. "건물 끝 바로 뒤, 한 칸" 이라는 답을 받는다.
4. 별채 안 안내판 (`+0x14`) 을 따라 **비상구 지도** (`+0x2a4`, 한 줄) 를 찾고, 창고에서 꺼낸 **덧붙임 쪽지** 에 적는다.
5. 창구에 "**이 쪽지도 장부에 올려 주세요**" (클래스 `0x35`) → 관리사무소 장부 (`InvertedFunctionTable`) 에 올라간다.
6. 칠판 덮개를 열고 쪽지를 목록·정리함 둘에 꽂고 닫는다. 명부 줄에 "**별채 지도도 올림**" 도장 (비트 21).
7. 한참 뒤, 칠판 덮개를 한 번 더 열고 **단골 비상구 메모** (히스토리 표) 여섯 줄을 적고 닫는다.

PE_emulator 의 관리사무소는 3번에서 답 칸을 비워 줬다. 그래서 관리실은 4 ~ 6 을 건너뛰었다. 진짜 VM 카드에는 6번 도장이 찍혀 있었다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

- **기준**: ntkrnlmp·ntdll pdb 줄에 run63.
- **그림 2**: `p4d` (`LdrpProcessMappedModule`) 상자 글에 "`+0x154 Flags 0x2000`, 반환 0 이면 `+0x1a9` 비트 21" 을 더했다. 새 `(정적)` 상자 셋 — `p4e` `LdrpCfgProcessLoadConfig`, `p4f` `RtlpInsertOrRemoveScpCfgFunctionTable`, `p4g` `RtlAddGrowableFunctionTable` — 과 화살표 `(1) +0x14a` · `(2) +0x1a3` · `+0xc9`.
  - 전: `LdrpProcessMappedModule` 안이 비어 있었다.
  - 후: 그 안의 CFG·함수 표 길이 상자로 보인다. 근거는 코드 줄 `[정적]`, "셋 다 탔다" 는 간접 증거는 `Flags 0x20a2c4` `[관찰]` (콜스택은 아님 — 4.1 표).
- **2.4 `.mrdata` 줄**: 초기화 안 열고 닫기 두 쌍이 더 있다 (Add 한 쌍 · History 한 쌍).
- **2.4 run51 절** (`InvertedFunctionTable` 사슬):
  - 전: `RtlAddGrowableFunctionTable → NtSetInformationProcess(0x35)`, "ntdll 안 CFG 갈래에 따라 syscall 까지 가는지 `[공백]`".
  - 후: 앞에 `+0x5d NtQueryVirtualMemory(0xe)` 단계가 붙고, "CFG 갈래는 힙만 고름, syscall 은 늘 냄", "탄 것: 비트 21 `[관찰]` — **F7 거의 닫힘**".
  - Ensure 줄: "안 간 것" → "(run63 정정) `+0x40` 은 늘 부름, 안 `+0x11 JZ` 에서 바로 0 `[정적]`".
- **`LdrpInitializeNtdllDataTableEntry` 트리**: `(4) +0x124 LdrpProcessMappedModule` 아래 한 줄 (`+0x14a` · `+0x154` · `+0x1a3` → `+0x1a9`).
- **새 절 "ntdll 항목 마무리 안 — CFG 로드 구성·ScpCfg 함수 표"**: 2.1 트리와 같은 줄기, 클래스 `0xe` 설명, `[관찰]` 비트 21, `Flags` 비트 표, `LdrControlFlowGuardEnforced`, 히스토리 표.
- **4장**: F7 줄에 "(run63) 정적으로 거의 닫힘". run60 의 두 줄은 "(해결됨 → run63)". 새 줄 다섯 — Q120a 해결 · 동적 1 ~ 6 (선택) · 정적으로 남은 것 · 정정 · 결과서와 다르게.
- **5장**: run63 줄 둘.

### 짐 지도 `baggage.md` 에서 바뀐 것

- 3.5 모듈 항목: 4줄 (`Flags.LoadConfigProcessed` · `ProtectDelayLoad` · `ScpInExceptionTable` · `DependentLoadFlags`). 모르는 칸 34 → **30**, ntdll 31 → 35.
- 3.9 `.mrdata`: `RtlpUnwindHistoryTable` 새로 (모름 29 → **28**, 아는 것 21 → 22), `RtlpDynamicFunctionTable`·`TreeMin`·`TreeMax` 둘째 줄. `LdrpMrdataHeap` 줄은 정정 (조건은 Ensure 안).
- 3.11 `InvertedFunctionTable`: 사슬에 클래스 `0xe` 앞 단계, F7 거의 닫힘.
- 새 **7.6.4** (`_DYNAMIC_FUNCTION_TABLE` 칸 표 + 별채 + 커널 전역), 새 **7.23** (동적 몫).
- 4장 짝 2줄: "쪽지 ↔ `InvertedFunctionTable`", "별채 ↔ VAD 확장·커널 ScpCfg 쪽". 2.9 에서 본 **유저·커널 양쪽에 걸리는 짐** 이 바로 이것이다.

### 큰 그림에서 어떻게 이어지나

- **PE_emulator 에게**: "정정 대기" 를 풀 재료가 다 나왔다. nt 쪽 클래스 `0xe` 몸통이 2.3 표대로 `{0, 0, 0x267000, 0x1000}` 을 주고, ntdll 뒤 `0x1000`B 에 별채 내용 (`+0x14 = 0x2a4`, `+0x2a4` 함수 항목) 을 놓아 주면, 진짜 ntdll 이 알아서 #2 ~ #4 를 낸다. 그러면 `Flags` 도 `0x20a2c4` 로 맞는다.
  - 별채는 **커널이 놓는 짐** 이다 (만드는 건 커널이지만, ntdll 이 `ntdll + 0x267000` 유저 주소로 직접 읽는다). 쪽지는 **ntdll 이 놓는 짐** (창고 안) 이다.
- **짐 지도에게**: F7 이 거의 닫혀, `InvertedFunctionTable` 의 "누가 언제" 가 정해졌다. run60 의 `LdrpMrdataHeap` 설명도 한 갈래로 정리됐다.
- **다음 바퀴**: Q120b (요청 11 의 7번 묶음 — TLS, `LdrpAllocateSchedulerSharedData`, `RtlInitializeProcessorFeaturesBitMap`, KnownDlls 등).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 클래스 `0xe` 가 실제로 돌려준 버퍼 (`0, 0x267000, 0x1000`), 별채 `+0x40` 의 형 (`48 ff e0` 이면 Nop), `+0x14`·`+0x2a4` 값 | 정적으로 정해졌지만 실행 때 값을 직접 본 적 없음 | **Q104 VM 단계** 동적 1 (`RtlpInsertOrRemoveScpCfgFunctionTable+0x5d`) |
| 커널 표 `DAT[3]` (`0x5000`?) · `[0xe375ec]` (`0x4000`?) | 파일에 값이 없는 `.data` 칸. 부팅 때 `IoInitSystemPreDrivers +0x1123` 의 `R12D` 를 안 따라감 `[추측]` | 동적 2 (`MiQueryImageExtensionInformation` 입구) · 또는 정적 **Q121 (b)** |
| 커널 장부 `EPROCESS.InvertedFunctionTable` 풀 블록에 항목이 **3개** (ntdll·kernel32·kernelbase) 인가 | 비트 21 로 "탔다" 까지는 닫혔지만, 장부 내용은 안 봄 `[추측: 개수]` | 동적 3 (`RtlAddGrowableFunctionTable+0x154`·`+0x2a4`) = **F7 마저, Q79 출처** |
| 히스토리 표 최종 값 (개수 6 · `GlobalHint` 4), 칸 0 이 정말 채워져 지름길을 타는지 | 정적 전제 (run60 칸 0) | 동적 4 · 5 |
| ntdll 노드 `Flags` 를 `dt` 로 직접 | 사진으로 이미 `0x20a2c4` | 동적 6 (3 과 같이) |
| ntdll 이미지가 **왜** `ImageExpansionCompatible` 인가, 작업집합 쪽 `[[EPROCESS+0x410]+0x425] & 4` 가 언제 켜지나 | `MiMapAllImageScpPages +0x1c` 의 호출 조건을 안 따라감. ntdll 값엔 영향 없음 — test.exe·kernel32 등 **다른 이미지 값** 이 필요해질 때 중요 | **Q121 (a)** (정적) |
| `LdrpIsModuleUnderSystem32` 가 ntdll 에서 **왜** 거짓인가 | "거짓" 자체는 사진 `DependentLoadFlags 0x800` 으로 `[관찰]`. `LdrpSystem32` 값·길이 비교를 안 따라감 | **Q121 (c)** (정적) |
| `LdrSystemDllInitBlock+0xb8 = 0` 을 직접 | 사진 값으로 0 | Q104 VM 단계 (같이) |
| WinDbg 가 `MmQueryVirtualMemory+0x274764` 를 어떻게 보여 주나 | 떨어진 범위라 `[추측]` | 동적 2 를 돌린 뒤 pitfalls 후보 |
| "Scp"·"HPAT" 의 뜻 | 근거에 없음 | 번호 없음 (모름) |
| Q120b 묶음 (TLS, KnownDlls …) | 요청서 범위 밖 | **Q120b** (다음) |

- 동적 1 ~ 6 은 모두 "**이미 정적 + 사진으로 맞음**" 이라 급하지 않다 (run 문서). 멈춤점·읽을 명령은 run 문서 "아직 확인 필요" 에 그대로 있다.

---

## 5. 더 읽기

- run 문서: [`run63-cfg-func-table.md`](run63-cfg-func-table.md) — 1절 클래스 `0xe` 칸 표, 2절 syscall 표, "호출 관계", "사진 대조", "정정"
- 결과서: [`_workspace/sast/58_cfg-func-table_result.md`](../../_workspace/sast/58_cfg-func-table_result.md) — 질문 2-3 칸 표, 2-5 별채 내용, PE_emulator 용 작은 표
- 요청서: [`_workspace/sast/58_cfg-func-table.md`](../../_workspace/sast/58_cfg-func-table.md)
- flow.md: [`flow.md`](flow.md) 그림 2 (`p4d` ~ `p4g`), 2.4 run51 절, 2.4 "ntdll 항목 마무리 안" 절, 4장 F7 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.5 · 3.9 · 3.11 · 7.6.4 · 7.23
- 앞 이야기: [run35 해설](../02_sprint/run35-ntdll-entry-해설.md) (명부 첫 줄) · [run51 해설](run51-eprocess-later-해설.md) (클래스 `0x35`, F7) · [run60 해설](run60-func-table-mrdata-해설.md) (지번 색인 칸 0, `.mrdata` 숫자판)
