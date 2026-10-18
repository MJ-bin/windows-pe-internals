# 요청서 양식

요청서는 **받는 에이전트가 이것만 읽고 일할 수 있게** 쓴다. 대화에서만 나온 내용은 여기에 옮겨 적는다.

## 정적 요청서 — `_workspace/sast/NN_키워드.md`

```markdown
# NN 키워드 — 정적 분석 요청

- 날짜: YYYY-MM-DD
- 스프린트: NN_sprint
- 관련: flow.md 2.5 [공백], 4.1 표 "(1) kernel32 준비" 줄 / 이전 요청 sast/NN

## 알고 싶은 것
1. (예/아니오나 함수 이름으로 답할 수 있게)
   예) `LdrpLoadDllInternal`은 `+0x284`(LdrpPrepareModuleForExecution) **앞에서** 어떤 함수들을 부르나? 그중 DLL을 메모리에 올리는 쪽은 어느 것인가?
2. ...

## 대상
| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpLoadDllInternal$` | 1 | +0x284 앞 호출 목록 |
| ntdll | `^LdrpMapDll` | 0 | 이름 후보 (정확한 이름은 _index.tsv로 확인) |

## 이미 아는 것
- `[관찰]` LdrpLoadDllInternal +0x284 → LdrpPrepareModuleForExecution (run7)
- `[추측]` (flow.md에 추측으로 둔 것. 정적으로 맞는지 틀린지 볼 것)

## 받고 싶은 것
- 질문별 답 + 근거 (asm 파일의 +0x 줄)
- 동적으로 확인할 멈춤점 추천 (함수 이름 또는 함수+오프셋)
```

## 동적 요청서 — `_workspace/dast/NN_키워드.md`

```markdown
# NN 키워드 — 동적 분석 요청

- 날짜: YYYY-MM-DD
- 스프린트: NN_sprint / 예정 run: runN
- 관련 정적 결과: _workspace/sast/NN_키워드_result.md
- 재시도: 없음  (재시도면 "dast/NN 사용자실수 — 이유" 처럼)

## 확인할 것 (정적으로 못 본 것)
1. 예) kernel32.dll이 메모리에 올라오는 순간의 콜스택 (`LdrpLoadDllInternal` 안 어디서?)
2. ...

## 멈출 곳 후보
| 지점 | 멈출지 / 기록만 | 찍을 것 | 근거 |
|---|---|---|---|
| `nt!NtMapViewOfSection` (이름은 `x`로 먼저 확인) | 기록만 | `k 10`, 모듈 이름 | sast/NN 질문 1 |
| `ntdll!LdrpInitializeProcess+0x1bb6` (BEFORE-GRAPH) | 멈춤 | `lm m dll*` | 기존 기준점 |

## 성공 기준
- 무엇이 로그에 찍히면 "분석성공"인가. 예) kernel32 매핑 1회 이상, 그때 콜스택에 `LdrpLoadDllInternal+0x...`

## 끝 지점
- `test!main`에서 멈추고 끝 (기본. 프로세스 종료까지 가면 Single step exception 문제)

## 범위 밖
- 이번에 보지 않을 것 (멈춤점이 늘어나 로그가 넘치지 않게)
```

## 진행표 — `_workspace/plan.md`

오케스트레이터만 고친다. 사람이 "어디까지 했지? 다음은 뭐지?"를 한눈에 보는 곳.

```markdown
# 분석 진행표

- 마지막 갱신: YYYY-MM-DD (runN 뒤)
- 다음 항목: Q3

## 대기열 (위에서부터 처리, 새 항목은 맨 아래에)

| # | 항목 | 단계 | 정적/동적 | 들어온 곳 | 상태 |
|---|---|---|---|---|---|
| Q1 | `nt!NtCreateUserProcess` callee (depth 1) | 1 | 정적 | 처음 | 끝 (run9) |
| Q2 | `nt!PspAllocateProcess`, `nt!MmCreatePeb` callee | 1 | 정적 | 처음 | 끝 (run10) |
| Q3 | 2.2 부모 쪽 마무리 ~ 첫 스레드 출발 | 1 | 정적 | 처음 | 대기 |
| Q4 | 2.3 커널 → 유저 전환 (`nt!KiStartUserThread`, `nt!PspUserThreadStartup`) | 1 | 정적 | 처음 | 대기 |
| … | (단계 2 ntdll 항목들) | 2 | 정적 | 처음 | 대기 |
| … | (단계 3 flow.md 4장·4.1 항목들) | 3 | 동적 | 처음 | 대기 |
| Q20 | <새로 나온 문제> | - | 동적 | sast/01 결과 "동적으로 확인할 것" | 대기 |

상태: `대기` / `진행 중` / `끝 (runN)` / `합침 → Qn` / `보류 (사용자 결정, 날짜)`

## 결정 기록
- YYYY-MM-DD: Q7이 Q3을 막음 → 사용자: "Q7 먼저" (그래서 Q7을 Q3 앞에서 처리)
```

- 위 표의 번호·상태는 양식 예시다. 처음 만들 땐 전부 `대기`.
- `#` 번호는 한 번 붙이면 안 바꾼다. 새 항목은 가장 큰 번호 + 1.
- 줄 순서는 사용자가 결정한 경우(결정 기록에 남김)에만 바뀐다.

## 이름 예시 (한 주제가 한 바퀴 도는 모습)

```
_workspace/sast/01_mapview.md
_workspace/sast/01_mapview_result.md
_workspace/dast/01_mapview.md
_workspace/dast/01_mapview_runbook.md
ms/logs/01_mapview.txt
ms/logs/01_mapview_result.md        ← 사용자실수면
_workspace/dast/02_mapview.md        ← 재시도 (번호만 늘고 키워드는 같음)
_workspace/dast/02_mapview_runbook.md
ms/logs/02_mapview.txt
ms/logs/02_mapview_result.md        ← 분석성공
runs/02_sprint/run9-mapview.md
runs/02_sprint/flow.md (갱신)
```
