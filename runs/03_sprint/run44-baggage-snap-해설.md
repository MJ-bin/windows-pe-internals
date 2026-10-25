# run44 baggage-snap — 해설

> 이 글은 [run44 문서](run44-baggage-snap.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·판정·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q77** — 짐 지도 B, 사진 (동적, 빠짐없음 확인용) (`_workspace/plan.md`, 이번에 **끝**)
  - 이번 바퀴 안에서 셋으로 나눴다: **Q77a** 시험 (dast/01·02) → **Q77b** 캡처 (dast/03) → **Q77c** 읽기 (dast/04).
  - 다음 항목: **Q78** — 짐 지도 C, 쓴 자리·줄기 (정적). 이번 바퀴에서 새로 생긴 **Q79** (덤프 보충 읽기) · **Q80** (ntdll `.data` `≤P1a` 좁히기) 은 맨 뒤에 붙었다.
- 근거:
  - run 문서 [`run44-baggage-snap.md`](run44-baggage-snap.md) (가장 중요)
  - 판정 넷 (넷 다 **분석성공**): `ms/logs/01_snapshot-test_result.md` · `02_snapshot-test_result.md` · `03_baggage-snap_result.md` · `04_baggage-read_result.md`
  - 요청서·runbook: `_workspace/dast/01_snapshot-test.md` ~ `04_baggage-read.md` (+ 각 `_runbook.md`)
  - 짐 지도 [`baggage.md`](baggage.md) 의 고친 곳: 2장 · 2.1 · 5.0 · 7장 (특히 7.1 · 7.8)
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md`
  - 진행 결정: `_workspace/plan.md` 결정 기록 2026-10-25 줄들
- 이번 바퀴는 **동적만** 했다. 정적 요청서는 없다 (sast 없음).
  - 이 repo 의 **첫 dast 요청서**가 이번 바퀴에서 나왔다 (plan 2026-10-25).
  - 그런데 디버거로 "함수에 멈춰 콜스택 보기" 를 한 게 중심이 아니다. **멈춘 순간 메모리 전체를 통째로 떠서**, 나중에 천천히 읽었다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, 콜스택, 태그, [핸들](../02_sprint/run9-create-user-process-해설.md#핸들) → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) (**안내판**), `_EPROCESS` (**가게 장부**), VAD (땅의 **구획 장부**) → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 2절
- `_ETHREAD` (**직원 인사 카드**), TEB (**개인 안내판**) → [run11 해설](../02_sprint/run11-allocate-thread-해설.md)
- 트랩 프레임 = 직원 책상의 **출발 메모**, `IRETQ` = 출발 메모대로 **순간 이동** → [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) 2.6
- 맡긴 문맥 `_CONTEXT` = **사물함 메모**, 바꿔 끼우기 → [run16 해설](../02_sprint/run16-thunk-context-해설.md) 2.3 · 2.6
- ntdll 전역 = 관리실 **칠판**, `PEB.Ldr` = 건물 **명부**, 모듈 항목 = **명부 한 줄**, 노드 `State` = **진행 도장** → [run17](../02_sprint/run17-ldr-init-entry-해설.md) · [run18](../02_sprint/run18-proc-init-setup-해설.md) · [run19](../02_sprint/run19-proc-init-modules-해설.md) 해설
- 매개변수 블록 = 부모가 써 준 **개업 서류**, `LdrSystemDllInitBlock` = **업무 수첩** → [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- `KUSER_SHARED_DATA` = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 짐 · 짐 지도 · 직접 칸 · 상태 사진 · 줄기 → [run43 해설](run43-baggage-checklist-해설.md) (비유·쓰는 말 2.2, 직접 칸 2.3, Q77·Q78 은 3절)

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**, 짐 = **개업 준비물**, 짐 지도 = 준비물 **점검표**.
- run 문서는 이번 방법을 "이사하는 집을 여덟 번 사진 찍고 비교한다" 고 비유했다. 이 글은 앞 해설과 맞추려고 **집 → 가게** 로 부른다:
  - **개업 준비하는 가게를, 준비 도중 여덟 번 사진 찍는다.**
  - 사진끼리 비교해서 "이 준비물은 3번째와 4번째 사진 사이에 들어왔다" 를 가린다.
- 이번에 더하는 비유는 본문에서 새 말이 나올 때 하나씩 붙인다 (스냅샷 = 가게를 통째로 얼린 **보관 상자**, 덤프 = WinDbg 가 펼쳐 볼 수 있는 **앨범** …).

### 한 줄로 먼저 — 무엇을 얻었나

| 무엇 | 숫자 (run 문서 그대로) |
|---|---|
| 한 번의 test.exe 실행에서 멈춘 지점 | **8곳** (K1 · K2 · K3 · P0 · P1 · P1a · P1b · P2) |
| 그 8곳의 덤프 | **8개** (각 8 GB, 실제 5.0G) |
| 구조체 21개 직접 칸 1,456 중 사진으로 정해진 칸 | **1,428** (구간 581 + 끝까지 0 847) |
| ntdll 전역 493 중 사진으로 정해진 것 | **332** (구간 35 + 끝까지 0 297) |
| 정적 쓴 자리 (3장 아는 칸 243줄) 와 어긋난 줄 | **0** |
| 정적 문서에 없던 일 | 여럿 — 대표가 **`PEB.ProcessParameters` 가 ①→①-a 사이에 힙으로 옮겨짐** |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번에도 길 위의 한 점이 아니다. 짐 지도 범위 **전체**에 말뚝 8개를 박았다.

```
cmd.exe 의 부탁 (CreateProcess)
 │
 ├─ 단계 ① 커널 ── 부모 cmd 스레드의 NtCreateUserProcess 안
 │    PspAllocateProcess 까지 (가게 장부 · 안내판 · 개업 서류)
 │  ●K1  PspAllocateThread 입구
 │    (직원 인사 카드 · 개인 안내판 · 출발 메모)
 │  ●K2  PspInsertProcess 입구
 │    (가게 목록에 넣기 · 직원 넣기)
 │  ●K3  PspCallProcessNotifyRoutines 입구 (PspInsertThread +0x807)
 │    부모 마무리 · csrss 알림 · NtResumeThread (풀어 주기)
 │  ●P0 (⓪)  PspUserThreadStartup 입구      ← 여기부터 test.exe 첫 스레드
 │    출발 메모 바꿔 끼우기 → IRETQ
 ├─ 단계 ② ntdll
 │  ●P1 (①)  LdrInitializeThunk 입구
 │    관리실 혼자 정리 (힙 · 명부 · 칠판)
 │  ●P1a (①-a) LdrpInitializeProcess +0x199d  (kernel32 준비 call 자리)
 │    KERNEL32·KERNELBASE 준비 → dll1·dll2 올리기·잇기
 │  ●P1b (①-b) LdrpInitializeProcess +0x1bb6  (그래프 켜기 call 자리)
 │    dll 시작 함수 · TLS 콜백
 │  ●P2 (②)  NtContinue 입구 (LdrInitializeThunk +0x18 에서 부름)
 │                                                    ← 이번 바퀴: ● 8곳에서 사진
 └─ RtlUserThreadStart → (CRT) → main   (짐 지도 범위 밖)
```

- K1 ~ K3 은 **부모** cmd 스레드 안이고, P0 ~ P2 는 **test.exe 첫 스레드** 안이다 (짐 지도 "쓰는 말").
- 이름 주의: run43 해설에서 사진 ①·② 두 장만 말했는데, 이번엔 8장이 됐다. 짐 지도 기호 ⓪ ① ①-a ①-b ② 는 그대로 쓰고, 커널 쪽 세 장은 기호 없이 K1 ~ K3 이다.

### 무엇이 궁금했나

run 문서 "목적" 넷:

1. 짐 지도 체크리스트 (run43) 의 칸마다, 초기화 동안 **언제 처음 값을 갖는지**를 실제 메모리로 본다.
2. 정적으로는 못 보이는 "**아무도 안 씀 (0 그대로)**" 을 센다.
3. 정적 쓴 자리 (run9 ~ run42) 가 실행과 **어긋나는 곳**을 찾는다.
4. 한 번에 커널·유저를 같이 읽는 **방법** (C 방식) 을 세운다.

그리고 사용자가 대화에서 물었던 것:

> "스냅샷이 **정답지**인가, 아니면 거꾸로 **언제 세팅됐는지 찾는** 건가?"

이 질문의 답은 2.16 에 따로 모았다. 미리 한 줄로: **둘 다 조금씩이다.** 사진은 "그 순간 무엇이 있었나" 의 정답지는 되지만, "누가 썼나" 는 안 알려 주고, "언제" 도 **두 사진 사이의 구간**까지만 좁혀 준다.

### 바퀴 전에 알던 것

- `[정적]` (run43) 구조체 21개 직접 칸 **1,456** — 아는 칸 181, 모름 1,275. ntdll 전역 **493** — 아는 것 54, 모름 439. 2장 "0 그대로" 열은 전부 **"사진 전"**.
- `[정적]` (run15·run16) 트랩 프레임 자리 = `KTHREAD.InitialStack − 0x190`. 처음 값은 `KxContextToKframes`, 바꿔 끼우기는 `PspInitializeThunkContext` (`PspSetContext`). 원래 문맥은 유저 스택에 맡긴다.
- `[정적]` (run12) 프로세스 락은 호출자 (`PspAllocateProcess +0x26e9`) 가 잡아 들고 들어온다 — 단 `[정적 + 간접]` 이었다.
- `[관찰]` (01_sprint) `LdrInitializeThunk` 입구에서 멈추면 콜스택이 자기 한 줄뿐이다 (run4).
- sast/38 결과서의 추천 = "사진 ① `LdrInitializeThunk` 입구, ② `NtContinue` 입구" 두 장 (run43 해설 3절). 이번에 사용자 제안으로 8장이 됐다 (plan 2026-10-25).

---

## 2. 찾은 것

### 2.1 한눈에 — 바퀴 하나, 동적 요청 넷

```
dast/01 snapshot-test   시험 1: ① 에서 VMware 스냅샷 → vmss2core 가 거부 (암호화)
   │  (가상 TPM 제거 → 백업 → 스냅샷 4개 삭제 → 암호화 해제)
dast/02 snapshot-test   시험 2: ① 스냅샷 → vmss2core → memory.dmp → WinDbg 로 열기 → 라이브와 같음
   │  남은 문제 1: lazy 스냅샷 → 끔 (partialLazySave = FALSE)
   │  남은 문제 2: 덤프의 CPU 문맥 = 디버거 대기 루프 → 레지스터는 라이브에서
dast/03 baggage-snap    캡처: 한 번의 실행에서 8 지점에 차례로 멈춤 → 스냅샷 8장 → 덤프 8개
dast/04 baggage-read    읽기: 덤프마다 같은 명령 파일 (구획 00 ~ 17, dt + raw dq) → 로그 8개 (+ 보충 8개)
   │
   └─→ (서버 임시 스크립트) 지점끼리 비교 → 칸마다 "처음 값이 생기는 구간" → run44 · baggage.md
```

- dast/01 · dast/02 는 시험이라 test.exe 실행이 **따로**다. 주소도 그 실행 기준이다.
- **dast/03 · dast/04 는 같은 한 번의 실행**이다. 이 글의 주소·숫자는 다 그 실행 기준이다 (run 문서 "환경").
  - test.exe: EPROCESS `ffff82859908b080`, PID `0x1dd0`
  - 첫 스레드: ETHREAD `ffff828598445080` (Cid `1dd0.26ec`)
  - PEB `9b2fa66000`, TEB `9b2fa67000`, 맡긴 문맥 `9b2fcffa40`
- 이 방식을 run 문서는 **C 방식**이라 부른다 (plan 2026-10-25 사용자 결정): VM 에선 스냅샷만 → 서버 `vmss2core` 로 `.dmp` → 디버거 VM WinDbg 로 열어 명령 묶음 실행.
  - 다른 안 둘은 안 했다: 서버에서 덤프 읽는 도구를 새로 짜기 (A), 멈출 때마다 명령 묶음을 치기 (B).

### 2.2 새 말 — 스냅샷과 덤프는 다르다

#### 스냅샷

- **스냅샷** = VMware 가 VM **전체**를 그 순간 그대로 저장해 둔 것. 이번엔 **호스트**(분석 서버)의 VMware 에서 찍었다.
  - 파일 둘이 생긴다 `[관찰]` (dast/01·02 HOST NOTES): `-SnapshotN.vmsn` (상태 파일) + `-SnapshotN.vmem` (메모리 파일).
  - 비유: 가게를 통째로 **얼려서 보관 상자에 넣기**. 나중에 상자를 열면 그 순간으로 돌아간다. 다만 상자는 VMware 전용 포장이라 **VMware 만** 연다.
- 디버거로 멈춘 채 스냅샷을 찍었으니, 상자 안 가게는 "멈춤점에 선 순간" 이다.

#### 덤프

- **덤프** (`memory.dmp`) = WinDbg 가 열 수 있는 꼴로 쓴 메모리 파일. 이번 덤프 머리는 `PAGEDU64` 다 `[관찰]` (dast/02 HOST NOTES).
  - 비유: 얼린 상자를 **WinDbg 가 펼쳐 볼 수 있는 앨범**으로 옮겨 붙인 것.
- 우리 프로젝트에서 말하는 **상태 사진** ("멈춘 순간의 덤프", CLAUDE.md) 은 이 앨범이다.

#### 스냅샷 → 덤프: `vmss2core`

- **`vmss2core`** = 스냅샷 파일 (`.vmsn` + `.vmem`) 을 읽어 `memory.dmp` 를 만드는 변환 도구. 서버에 있다.
  - 일반적으로 VMware 가 주는 도구로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 이번 옵션은 `-W8` 이다. runbook 이 `-W` 를 "옛 Windows 형식" 이라 적었으니, `-W8` 은 새 Windows 꼴로 쓰라는 뜻으로 보인다 `[추측: runbook 표현에서]`.
  - 비유: 보관 상자를 열어 앨범으로 옮겨 붙이는 **현상소**.
- 걸린 것 `[관찰]` (HOST NOTES): 장당 약 26초 (dast/02 는 25.9초), 끝 줄 `Finished writing core.`, `memory.dmp` 8,589,942,784 B.

#### 왜 굳이 이렇게 돌아가나

- WinDbg 로 직접 메모리를 하나하나 읽으면, 칸 수천 개를 읽는 동안 타깃을 계속 멈춰 둬야 하고 명령을 수십 번 오가야 한다.
- 사용자 결정 이유 (plan 2026-10-25): "연결 장치를 새로 만들 시간이 안 보임 … 오가는 횟수가 수십 번".
- 그래서 → **멈춘 순간엔 사진만 찍고 바로 보내 주고**, 읽기는 나중에 앨범으로 천천히 한다.
- 덕분에 dast/04 때는 **타깃 VM 을 꺼도 됐다** (덤프만 씀, plan 2026-10-25).

### 2.3 시험 1 — 사진은 찍혔는데 현상소가 거부했다 (dast/01)

- ① `LdrInitializeThunk` 입구에서 멈추고, 그대로 스냅샷을 찍었다. 스냅샷은 찍혔다 `[관찰]` (`Snapshot4.vmem` 2,668,462,080 B).
- 그런데 `vmss2core -W8` 이 이 스냅샷을 거부했다 `[관찰]`:
  - "Unrecognized .vmss file (magic 8943dd9e)"
- 원인 짐작 (run 문서 "C 방식과 시행착오", plan 2026-10-25):
  1. 근거: 타깃 vmx 에 `vmx.encryptionType = "partial"` 과 keySafe 가 있다. 파일 머리가 `9e dd 43 89` 다.
  2. 그래서: **암호화된 스냅샷**으로 봤다.
  3. 왜 암호화됐나: 가상 TPM 때문 `[추측]`.
- 조치 (사용자가 VMware 에서):
  - 가상 TPM 제거 → "스냅샷이 있으면 암호화를 못 푼다" → VM 백업 (`bob_baseline_vm.bak-20261025/`, 118 GB) → 스냅샷 4개 삭제 → 암호화 해제.
- 비유: 보관 상자가 **자물쇠 달린 상자**여서 현상소가 못 열었다. 자물쇠를 떼려면 상자 안의 옛 사진들을 먼저 치워야 했다. 그래서 통째로 복사해 두고 치웠다.

### 2.4 시험 2 — 앨범이 라이브와 똑같았다 (dast/02)

- 암호화를 푼 타깃에서 같은 ① 스냅샷 → `vmss2core -W8` → `memory.dmp` 8 GB → 디버거 VM 의 새 WinDbg 로 열었다.
- **라이브 값과 오프라인 값이 모두 같았다** `[관찰]` (dast/02 판정):
  - test.exe EPROCESS · DirBase · Peb (`!process 0 0 test.exe`)
  - `_PEB` 네 칸 (`ImageBaseAddress` · `Ldr` · `ProcessHeap` · `ProcessParameters`), `dq` 로 본 PEB 두 줄
  - `LdrInitializeThunk` 입구 16바이트
  - `!vtop` 물리 주소 `1d0f6b000`
- 그래서 → **커널 메모리도, test.exe 유저 메모리도 ① 상태로 읽힌다.** C 방식이 된다.

### 2.5 남은 문제 1 — lazy 스냅샷

#### 새 말 — lazy 스냅샷

- **lazy 스냅샷** = VMware 가 "찍었다" 고 한 뒤에도, 메모리 내용을 디스크에 **천천히 나중에** 쓰는 방식.
  - dast/02 때 `[관찰]` (HOST NOTES, plan): 메뉴 끝 18:11:44 → 메모리 쓰기 끝 18:18:52. 디버거로 멈춘 동안은 초당 약 1.5 MB 였고, 타깃을 풀어 준 뒤 빨라졌다.
  - 비유: 셔터는 눌렀는데 필름에 **천천히 감광되는** 카메라. 감광이 끝나기 전에 피사체가 움직이면 사진이 섞일 수 있다.

#### 왜 문제인가

- dast/02 는 **쓰기가 끝나기 전에 타깃을 풀어 줬다** (판정).
- 확인한 두 페이지 (PEB, ntdll 입구) 는 ① 상태였다. 하지만 "덤프 **전체**가 같은 순간인지" 는 그 두 페이지만으로는 확정 못 한다 `[추측]` (dast/02 판정 "남은 의문").
- 멈춘 채 쓰기가 끝나길 기다리면 장당 약 50분 `[추측]` (plan 2026-10-25, 초당 1.5 MB 로 어림).

#### 조치

- 타깃 `.vmx` 에 `mainMem.partialLazySave = "FALSE"` 를 넣었다 (사용자 결정, 알려진 설정이라 `[추측]` 으로 시작).
- 시험 스냅샷 `lazy-test` 에서 `Wrote 8192MiB in 1885ms` `[관찰]` — 약 2초에 다 썼다.
  - 요청 줄엔 여전히 `lazy=1` 이 찍힌다. 그래서 확인은 `Wrote ... MiB` / `Done with snapshot` 줄로 한다.
- 본 캡처 (dast/03) 에서는 장마다 `Done with snapshot` 을 **확인한 뒤에** `g` 했다. 장당 쓰기 3.2 ~ 3.3초 `[관찰]`.
- 비유: 즉석 카메라로 바꿨다. 셔터를 누르면 2 ~ 3초 안에 사진이 다 나온다. 다 나온 걸 보고 나서 가게를 다시 움직이게 한다.

### 2.6 남은 문제 2 — 덤프의 CPU 문맥은 디버거 대기 루프다

#### 무엇을 봤나

- 덤프에는 메모리 말고 **CPU 레지스터** (이 글에서 "CPU 문맥") 도 들어 있다. 그걸 WinDbg 에서 `~0s` → `r` / `k` 로 봤다.
- 그런데 ① 의 레지스터 (`rip` = `LdrInitializeThunk` 등) 가 아니었다 `[관찰]` (dast/02 판정):
  - `nt!HalpTimerStallExecutionProcessor+0x2a5` ← `kdcom!WaitForRxPacket+0x1c9`
  - `cs=0010 ss=0000`, "Unable to get program counter"

#### 왜 그런가 — 짐작

- 일반적으로 알려진 설명 `[추측]`:
  1. 멈춤점에 서면 CPU 가 정말로 얼어붙는 게 아니다. 커널 안의 **디버거 담당 코드**로 넘어가서, 디버거 VM 이 보낼 명령을 **기다리며 계속 돈다** (빙빙 도는 고리).
  2. 멈춤점 순간의 레지스터는 그 담당 코드가 따로 챙겨 두고, WinDbg 가 `r` 을 치면 그걸 보여 준다.
  3. 그런데 VMware 스냅샷은 **CPU 가 실제로 그 순간 하던 일**을 찍는다 = 기다리는 고리 한가운데.
- 콜스택에 `WaitForRxPacket` (받을 꾸러미를 기다림) 이라는 이름이 보이는 게 이 짐작과 맞는다. 하지만 디버거 담당 코드가 레지스터를 어디에 챙겨 두는지는 이번에 안 봤다.
- 비유: 가게 사진을 찍는 순간, 직원은 **"다음 지시 기다리는 중"** 자세로 서 있었다. 사진엔 그 자세가 찍힌다. 직원이 원래 하던 동작은 사진에 없다.

#### 그래서 어떻게 했나

- **레지스터는 라이브에서 남긴다.** dast/03 에서 지점마다 `r` · `k 5` · `!process -1 0` · `!thread -1 0` 을 쳐서 로그에 남겼다.
- 오프라인에선 **메모리 안에 적힌 레지스터 사본**을 읽는다: 트랩 프레임 (다음 절), 맡긴 `_CONTEXT`.

### 2.7 새 말 — 트랩 프레임을 `InitialStack − 0x190` 에서 찾는 까닭

#### 트랩 프레임 다시 한 번

- [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) 2.6: 트랩 프레임 = 유저 → 커널로 들어올 때 유저 쪽 레지스터를 적어 두고, 나갈 때 되살리는 묶음 (크기 `0x190`). 비유는 **출발 메모**.
- 자리 `[정적]` (run15): `KTHREAD.InitialStack − 0x190`. `KiInitializeContextThread` 가 이 자리에 만든다.
  - `InitialStack` 은 `_KTHREAD +0x28` 칸이다 (PDB 타입, run15). 이름대로 그 스레드 커널 스택이 시작하는 자리로 보인다 `[추측]` (일반적으로 알려진 설명). 거기서 `0x190` (트랩 프레임 크기) 만큼 아래가 출발 메모 자리다.

#### 왜 `KTHREAD.TrapFrame` 칸을 안 믿었나

- `KTHREAD` 에는 `TrapFrame` 이라는 이름의 칸이 따로 있다. 거기 주소가 있으면 편하다.
- 그런데 `[관찰]` (dast/03): **P0 · P1 에선 `TrapFrame` 이 null**, P1a · P1b 에선 `ffffb58b9ad07460` (= InitialStack − 0x190).
- 즉 시스템 콜을 하기 전엔 그 칸이 비어 있고, 시스템 콜을 한 뒤 (P1a ~) 에야 같은 자리를 가리킨다 (run 문서 "확인된 것").
- 그래서 → 칸을 믿지 않고, **고정 주소 `InitialStack − 0x190`** 으로 읽었다. 이번 실행에선 `ffffb58b9ad07460` 이다.
- 비유: 직원 인사 카드에 "출발 메모는 책상 서랍 몇 번" 이라는 칸이 있는데, 처음엔 비어 있다. 그래도 메모는 **늘 같은 서랍**에 있으니 서랍을 바로 연다.

#### 그 자리를 멈춤점이 덮어쓴다 — `Rip` = 멈춘 주소 + 1

- `[관찰]` (run 문서 "디버거 영향"): 유저 쪽 멈춤점 지점의 트랩 프레임 `Rip` 는 **멈춘 주소 + 1** 이다.
  - P1 `7ffafb741421`, P1a `7ffafb6edda2`, P1b `7ffafb6edfbb`, P2 `7ffafb7c1611`.
- run 문서 설명: 멈춤점 `int3` 이 만든 트랩 프레임이 `InitialStack − 0x190` 자리에 **덮인** 것이다.
  - `int3` 는 1바이트 명령이라, 실행된 뒤 적히는 `Rip` 가 그다음 바이트를 가리킨다 `[추측]` (일반적으로 알려진 설명).
  - 유저 → 커널로 들어올 때마다 (멈춤점이든 시스템 콜이든) 커널은 출발 메모를 **같은 자리**에 새로 쓴다 — 그래서 겹친다 `[추측]` (일반적으로 알려진 설명, P1a 이후 `TrapFrame` 칸이 같은 자리를 가리킨다는 `[관찰]` 과 맞음).
- 그래서 **P1 ~ P2 의 트랩 프레임 칸은 짐이 아니라 디버거가 만든 값**이다 (run 문서). P1a 이후는 시스템 콜 트랩 프레임도 같은 자리다.
- 짐으로 쓸 수 있는 트랩 프레임 값 (run 문서 "디버거 영향"):
  - K2 · K3 · P0 의 값 (처음 값)
  - P1 의 `Rcx` · `Rdx` · `Rsp` (`int3` 직전 = `IRETQ` 로 받은 값)
- 비유: 서랍의 출발 메모를 사진 찍으려는데, 사진사가 문을 두드릴 때마다 (멈춤점) 직원이 서랍 메모를 "**지금 문 앞에 서 있음**" 으로 새로 써 버린다. 그래서 원래 메모는 문을 두드리기 **전** 사진에서만 믿을 수 있다.

### 2.8 8 지점 — 어디서 멈췄고, 정말 그 순간인가

#### 지점과 갈리는 것 (run 문서 "8 지점")

| 이름 | 멈춘 곳 | 이 사진으로 갈리는 것 |
|---|---|---|
| K1 | `nt!PspAllocateThread` 입구 (`NtCreateUserProcess +0xe5c`) | 프로세스 만들기 끝 / 스레드 만들기 전 |
| K2 | `nt!PspInsertProcess` 입구 (`+0xfc2`) | 스레드 만들기 끝 / 목록·핸들 표에 넣기 전 |
| K3 | `nt!PspCallProcessNotifyRoutines` 입구 (`PspInsertThread +0x807`) | 넣기 끝 / 알림·마무리 |
| P0 ⓪ | `nt!PspUserThreadStartup` 입구 | 부모가 한 것 전부 / 자식 스레드가 커널에서 한 것 |
| P1 ① | `ntdll!LdrInitializeThunk` 입구 | 커널 몫 끝 / ntdll 시작 |
| P1a ①-a | `LdrpInitializeProcess +0x199d` (call → `LdrpInitializeKernel32Functions`) | ntdll 혼자 한 것 / KERNEL32·KERNELBASE 준비 |
| P1b ①-b | `LdrpInitializeProcess +0x1bb6` (call → `LdrpInitializeGraphRecurse`) | 로더가 깐 것 / dll 시작 함수·TLS 콜백 |
| P2 ② | `ntdll!NtContinue` 입구 (`LdrInitializeThunk +0x18` 에서 부름) | 초기화 끝 (짐 지도 범위 끝) |

#### 한 줄 읽기 — K2 줄

- **누가**: 부모 cmd 스레드 (`19a8.1594`) 가, 시스템 콜 `NtCreateUserProcess` 안에서
- **어디서**: `NtCreateUserProcess +0xfc2` 에서 `PspInsertProcess` 를 부르는 순간 — 그 함수의 **입구**에서 멈췄다
- **누구를**: `PspInsertProcess` (새 가게를 관리사무소 목록·핸들 표에 넣는 함수)
- **무엇을 위해 여기서 찍나**: 이 순간은 `PspAllocateThread` 가 **이미 끝났고** `PspInsertProcess` 는 **아직 시작 전**이다. 그래서 K1 사진과 K2 사진의 차이 = 거의 "스레드 만들기" 가 놓은 것이다.
- 콜스택 `[관찰]` (run 문서 "지점별 라이브 관찰"): `PspInsertProcess` ← `NtCreateUserProcess+0xfc2` ← …

#### 멈춤점은 어떻게 걸었나 (dast/03 판정)

- 커널 멈춤점은 모든 프로세스에 걸린다 → 조건을 붙였다 (CLAUDE.md).
  1. K1 ~ K3 은 부모 cmd 에만 서게 (`bp /p <cmd>`).
  2. K1 에서 test.exe EPROCESS 를 얻어 → P1a · P1b 를 test.exe 에만 서게 (`bp /p <test>`).
  3. P0 에서 첫 스레드 ETHREAD 를 얻어 → P1 은 그 스레드일 때만, P2 는 그 스레드 + "돌아갈 주소가 `LdrInitializeThunk+0x18`" 일 때만.
- 왜 "첫 스레드만" 조건이 필요한가: ntdll 일꾼 스레드도 `PspUserThreadStartup` · `LdrInitializeThunk` · `NtContinue` 를 지난다 (dast/03 요청서).

#### 정말 같은 실행, 같은 스레드인가

- `[관찰]` (dast/03 판정): 8 지점 모두 한 번씩 섰다. P0 · P1 · P1a · P1b · P2 는 모두 같은 첫 스레드 (`1dd0.26ec`). P2 의 돌아갈 주소는 `LdrInitializeThunk+0x18` (`7ffafb741438`).

#### 덤프마다 "그 지점의 순간" 인가

- 근거 `[관찰]` (dast/03·04 판정):
  - 8개 덤프 모두 `!lmi nt` GUID 가 라이브와 같다.
  - K3 ~ P2 에서 `!process 0 0 test.exe` 가 라이브와 같은 EPROCESS · DirBase 를 낸다.
  - **핸들 수** (`HandleCount`) 가 지점별 라이브 값과 같다: K3 · P0 · P1 · P1a · P1b · P2 = 4 · 4 · 4 · 26 · 44 · 46.
- 그래서 → 핸들 수처럼 **지점마다 달라지는 값**이 라이브와 맞으니, 각 덤프는 그 지점의 순간이다 (run 문서).
- K1 · K2 덤프에선 `!process 0 0 test.exe` 가 빈칸이었다. K2 를 **주소로** 읽으니 test.exe 이고 `ActiveProcessLinks` = `[0 - 0]` → **아직 프로세스 목록에 안 넣음** `[관찰]` (dast/04). K2 가 `PspInsertProcess` **입구**라는 것과 딱 맞다.

### 2.9 새 말 — raw `dq` 와 `dt` 의 차이

덤프마다 같은 명령 파일을 돌렸다 (구획 00 ~ 17, 머리 `==== <P> NN`). 구조체 하나를 **두 번** 읽었다.

```
dt nt!_EPROCESS ffff82859908b080
dq ffff82859908b080 L0x108
```

(dast/04 명령 파일 `04_baggage-read_cmds_<P>.txt` 의 구획 01 · 01c)

#### `dt` — 이름표 붙은 칸 목록

- **`dt`** = PDB 의 양식대로 구조체를 펼쳐, 칸마다 `+0x오프셋 이름 : 값` 한 줄씩 보여 준다.
- 좋은 점: 칸 이름과 뜻을 바로 읽는다.
- 모자란 점 (run 문서 "지점끼리 비교한 법"): **펼친 구조체 칸은 값을 안 보여 준다.** 예를 들어 P1 로그엔 이렇게만 나온다:
  - `+0x000 Pcb : _KPROCESS`
  - `+0x1c8 ProcessLock : _EX_PUSH_LOCK`
  - "여기 이런 타입이 있다" 뿐, 안에 든 숫자가 없다.
- 비유: 양식 견본집 위에 **이름표만** 붙어 있고, 어떤 칸은 "이 칸은 별지 참조" 라고만 적혀 있는 것.

#### raw `dq` — 이름 없는 숫자 줄

- **raw `dq`** = 그 주소부터 메모리를 **8바이트씩 숫자 그대로** 찍는다. 한 줄에 주소 하나 + 8바이트 숫자 둘. 이름은 없다.
- 좋은 점: 구조체 **전체 크기만큼 모든 바이트**가 남는다. 이름 없는 바이트, 펼친 칸 안쪽까지.
- 모자란 점: 사람이 읽기엔 어디가 무슨 칸인지 모른다.
- 비유: 이름표 없이 찍은 **근접 사진**. 무엇이 무엇인지는 견본집 (칸 목록) 과 맞대 봐야 안다.

#### 그래서 이렇게 썼다

- **값 비교는 모두 raw 로**, `dt` 는 이름·뜻을 읽는 데만 썼다 (run 문서).
  - dast/04 판정의 원래 제안은 `dt` 줄을 (오프셋, 이름) 열쇠로 비교하는 것이었는데, 위 "펼친 칸" 때문에 모자랐다.
- 방법 (run 문서 "지점끼리 비교한 법"):
  1. raw `dq` 줄을 대상 시작 주소 기준 **바이트 배열**로 펼친다. `????` (읽기 불가) 는 0 으로 바꾸지 않고 "읽기 불가" 로 둔다.
  2. run43 칸 목록 (`struct__*.tsv`, `globals.tsv`) 의 (오프셋, 크기, 비트) 로 바이트를 잘라 칸마다 값을 낸다.
  3. 비트 칸·union 은 칸마다 따로 (같은 바이트라도 이름이 다르면 다른 칸).
  4. `_KPROCESS` = `_EPROCESS` raw 앞 `0x1c8`, `_KTHREAD` = `_ETHREAD` raw 앞 `0x4c0` 처럼 잘라 쓴다.
- 비유: 근접 사진 (raw) 위에 견본집의 칸 테두리를 **투명 필름으로 겹쳐서**, 칸마다 숫자를 읽어 낸다.

#### 한 줄 읽기 — `ProcessLock` 을 raw 로

같은 raw 줄이 K1 ~ P0 로그에 이렇게 있다 (`04_baggage-read_<P>.txt`, EPROCESS raw 구획):

```
K2: ffff8285`9908b240  ffff8285`9908bb00 00000000`00000001
K3: ffff8285`9908b240  ffff8285`9908bb00 00000000`00000000
```

- 줄 앞 주소 `…9908b240` = EPROCESS 시작 (`…9908b080`) 에서 `0x1c0` 떨어진 자리. 그 줄의 **둘째 숫자**가 `+0x1c8` 칸 = `ProcessLock` 이다.
- K1 · K2 = 1, K3 · P0 = 0.
- 그래서 → `ProcessLock` 은 **K2 와 K3 사이에 1 → 0** (풀림). `dt` 에선 `_EX_PUSH_LOCK` 이라는 타입 이름만 나와서 이 변화가 안 보였을 것이다.
- 이 값이 2.13 의 "run12 `[정적 + 간접]` 이 실제로 맞다" 의 근거다.

#### 읽다 막힌 것과 보충 (dast/04 판정)

- 명령 쪽 실패: 토큰 `dt`/`dq` 줄 앞부분이 잘려 `Syntax error` (원인 모름), `_PS_NTDLL_EXPORTS` `dt` 꼴 잘못, KUSD 를 유저 주소 `0x7ffe0000` 으로 읽으면 K1 ~ P1 은 전부 `??`.
- 같은 dast/04 안에서 **보충 명령 파일**로 다시 읽었다 (토큰 · `PsNtdllExports` · KUSD 커널 별칭 `fffff78000000000`). 보충 8개 모두 에러 0 `[관찰]`.

### 2.10 새 말 — "처음 값을 갖는 구간"

#### 뜻

- 칸 하나에 대해 사진 8장에서 값이 8개 나온다 (`없음` / `읽기 불가` / `0` / 값).
- **처음 값을 갖는 구간** `A~B` = **A 사진에선 0 (또는 아직 없음), B 사진에선 처음으로 0 이 아님** → 그 칸은 A 와 B **사이 어딘가에서** 처음 값을 얻었다 (run 문서 "판정 규칙").
- 비유: 3번째 사진엔 그 화분이 없고 4번째 사진엔 있다 → "화분은 3번과 4번 **사이에** 들어왔다". 몇 시 몇 분에, **누가** 들고 왔는지는 사진만으론 모른다.

#### 판정 규칙 (run 문서 그대로, 짐 지도 "쓰는 말")

| 표시 | 뜻 |
|---|---|
| `K1 이전` | K1 에서 이미 0 이 아님 (첫 사진보다 먼저) |
| `A~B` | A 와 B 사이에 처음 값 |
| `끝까지 0` | 읽힌 사진 모두 0. `*` 가 붙으면 K1 ~ P1 은 읽기 불가였던 것 |
| `≤P1a` | K1 ~ P1 은 읽기 불가, P1a 에 이미 값 (그 앞 언제인지 못 좁힘) |
| `못 읽음` / `칸 안 읽음` | 8장 모두 `????` (`.00cfg`) / VAD 노드 (`!vad` 목록만 읽음) |
| 바뀜 | 처음 값 뒤에 또 바뀐 구간은 따로 적음 |

- "없음" 으로 친 것: K1 의 ETHREAD · KTHREAD · 트랩 프레임 (스레드 생기기 전 메모리, 쓰레기), K1 의 TEB (전부 `????`).
- 소음 (구간은 내되 "바뀜" 은 뜻 없음): KUSD 시각 칸, 스레드 실행 통계 칸 (`CycleTime` · `ContextSwitches` 등) — 멈춰 있던 시간이 섞인다 `[추측]`.

#### 한 줄 읽기 세 가지

1. **`PEB.Ldr`** — K1 ~ P1 null, P1a `7ffafb832880` (= ntdll + `0x1d2880` `PebLdr`) `[관찰]` → 구간 **`P1~P1a`**. run18 `[정적]` `LdrpInitializeProcess +0x1c1` 이 그 구간 안에 있다. 건물 명부가 생기는 순간이 사진에서도 그 사이였다.
2. **`EPROCESS.UniqueProcessId`** — K1 에서 이미 `0x1dd0` → **`K1 이전`**. 가게 번호는 장부를 만들 때 (`PspAllocateProcess`) 벌써 적혀 있었다.
3. **`PEB.BeingDebugged`** — K1 ~ P2 모두 0 → **`끝까지 0`** `[관찰]`. 커널 디버거만 붙었을 땐 안 켜진다 (run 문서 "새로 확인된 것" 8).

#### 조심할 것 둘 (run 문서 그대로)

- **구간은 "언제" 만 말한다. "누가" 는 말하지 않는다.** 한 구간 안에 여러 함수 · 부모 쪽 코드 · 다른 스레드가 섞인다. 쓴 자리는 Q78.
- **"끝까지 0" 은 "아무도 안 씀" 과 같지 않다.** 0 을 썼거나, 썼다가 사진 사이에 되돌렸거나 (예: `SpecialApcDisable`), 조건이 안 맞아 안 썼거나를 사진으로는 못 가린다.

### 2.11 숫자 — 얼마나 정해졌나

#### 표 (대표만, run 문서 · `baggage.md` 2.1 과 같은 숫자)

| 구조체·구역 | 칸 수 | K1 이전 | K1~K2 | K2~K3 | K3~P0 | P0~P1 | P1~P1a | P1a~P1b | P1b~P2 | ≤P1a | 끝까지 0 | 못 읽음 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `_PEB` | 124 | 23 | · | · | 2 | · | 13 | 5 | · | · | 81 | · |
| `_TEB` | 137 | · | 9 | · | · | 6 | 7 | 1 | 2 | · | 112 | · |
| `_EPROCESS` | 263 | 62 | · | 7 | 12 | 9 | 5 | 1 | · | · | 167 | · |
| `_KTHREAD` | 237 | · | 49 | 11 | 9 | 9 | 7 | 6 | 6 | · | 140 | · |
| `_KTRAP_FRAME` | 66 | · | 9 | · | · | · | 24 | 1 | · | · | 32 | · |
| **유저 구조체 합** | **488** | **105** | **9** | **0** | **2** | **6** | **57** | **17** | **2** | **5** | **285** | **0** |
| **커널 구조체 합** | **968** | **172** | **89** | **26** | **27** | **18** | **37** | **8** | **6** | **0** | **562** | **23** |
| ntdll `.data` 전역 | 438 | · | · | · | · | · | · | 24 | 6 | 143 | 265 | · |
| ntdll `.mrdata` 전역 | 50 | 5 | · | · | · | · | · | · | · | 13 | 32 | · |
| ntdll `.00cfg` 전역 | 5 | · | · | · | · | · | · | · | · | · | · | 5 |

#### 한 줄 읽기 — `_TEB` 줄

- **칸 수 137** = 개인 안내판 양식의 직접 칸.
- **K1~K2 9** = `PspAllocateThread` 구간에 처음 값이 생긴 칸 9개 (`NtTib` · `ClientId` · `ProcessEnvironmentBlock` · `DeallocationStack` · `SameTebFlags.InitialThread` …). K1 엔 TEB 자리가 아예 `????` 였다 — 개인 안내판이 이 구간에 **생겼다**.
- **P0~P1 6** = `PspUserThreadStartup` 구간 (`CurrentLocale` `0x412` · `IdealProcessor` · `PrimaryGroupAffinity` …).
- **P1~P1a 7 · P1a~P1b 1 · P1b~P2 2** = ntdll 단계 (`RanProcessInit` · `ThreadLocalStoragePointer` · `FlsData` …).
- **끝까지 0 112** = 137칸 중 112칸은 사진 8장 내내 0.
- 비유: 개인 안내판 137칸 중 25칸만 개업 전에 채워지고, 112칸은 개업 날까지 빈칸이었다 (채웠다 지웠을 수도 있다 — 2.10 조심할 것).

#### 합계는 이렇게 맞는다 (run 문서 "요약" 아래)

- 구조체 21개 1,456칸 = 구간이 정해진 칸 **581** + 끝까지 0 **847** = **1,428 칸이 사진으로 정해짐**. 못 좁힌 것 **28** (`≤P1a` 5 · VAD 칸 안 읽음 23).
- ntdll 전역 493 = 구간 **35** + 끝까지 0 **297** = **332 개가 정해짐**. 못 좁힌 것 **161** (`≤P1a` 156 · `.00cfg` 읽기 불가 5).
- 헷갈리기 쉬운 숫자: 짐 지도 7.1 · 7.8 · flow.md 의 "`≤P1a` 161" 은 **다른 161** 이다 = 전역 `≤P1a` 156 (`.data` 143 + `.mrdata` 13) + `PebLdr` 5칸. 위 "못 좁힌 전역 161" (= `≤P1a` 156 + `.00cfg` 5) 과 숫자만 같다.

#### `_KTRAP_FRAME` 의 `P1~P1a` 24 는 짐이 아니다

- 2.7 에서 본 것처럼, P1 부터 이 자리에 멈춤점 `int3` · 시스템 콜의 트랩 프레임이 덮인다.
- 그래서 표의 24칸은 "처음 값" 이 아니라 **디버거·시스템 콜이 쓴 값**이다 (run 문서, 짐 지도 2.1).
- 짐으로 쓸 트랩 프레임 값은 `K1~K2` 9칸 (처음 문맥) 과 `P0~P1` 의 바꿔 끼우기다.

#### 왜 `≤P1a` 가 생기나 — ntdll 칠판이 K1 ~ P1 엔 안 읽혔다

- `[관찰]` (run 문서 "새로 확인된 것" 10): K1 ~ P1 에서 ntdll `.data` 전부와 `.mrdata` 앞 3페이지가 **읽기 불가**. `.mrdata` 끝 페이지 (`LdrSystemDllInitBlock` · `__security_cookie` 가 있는 곳) 만 K1 부터 읽혔다. `.00cfg` 는 8장 모두 읽기 불가.
- run 문서의 짐작 `[추측]`:
  1. 커널이 K1 전에 `.mrdata` 끝 페이지에 썼다 (run32 `PspPrepareSystemDllInitBlock` — 업무 수첩 채우기).
  2. 그래서 그 페이지만 "이 프로세스 몫" 페이지가 생겼다.
  3. 나머지는 아직 아무도 안 건드려서 이 프로세스 페이지가 없었다.
- 비유: 관리실 칠판 대부분이 아직 **포장도 안 뜯긴** 상태라 사진에 안 찍혔다. 커널이 미리 써 둔 업무 수첩 한 장만 꺼내져 있었다.
- 그래서 칠판 칸 상당수는 "P1a 에 이미 값이 있다" 까지만 말할 수 있다 (`≤P1a`). 파일에 처음부터 적혀 있던 값인지, ① 뒤에 누가 쓴 값인지 못 가린다 → Q80.

### 2.12 구간마다 무엇이 생기나 (대표만)

짐 지도 5.0 "사진으로 본 구간 순서" 를 대표만 옮긴다. 모두 `[관찰]`.

| 구간 | 그 사이에 돈 것 (run 문서) | 처음 값이 생기는 대표 칸 | 비유 |
|---|---|---|---|
| **K1 이전** | `PspAllocateProcess` 까지 + 섹션 만들기 | EPROCESS 62칸 (`UniqueProcessId` · `Peb` · `SectionBaseAddress` · `Token` …), PEB 23칸 (`ImageBaseAddress` · `ProcessParameters` `25996cb0000` · `ApiSetMap` …), 매개변수 20칸, `.mrdata` 끝 페이지, VAD 8개 | 가게 장부·안내판·개업 서류가 이미 있음 |
| **K1~K2** | `PspAllocateThread` | ETHREAD 31 · KTHREAD 49 · 트랩 프레임 9 · TEB 9, 유저 스택 VAD | 직원 인사 카드·개인 안내판·출발 메모가 생김 |
| **K2~K3** | `PspInsertProcess` 전부 + `PspInsertThread +0x807` 까지 | `ActiveProcessLinks` · `SequenceNumber 0x134`, `HANDLE_TABLE.UniqueProcessId` `0x19a8` → `0x1dd0`, `ProcessLock` · `ThreadLock` 1 → 0, `SuspendCount` 0 → 1 | 가게를 목록에 올리고, 직원은 "잠깐 대기" 표시 |
| **K3~P0** | 부모 마무리 · csrss 알림 · `NtResumeThread`, 자식 스레드는 잠들었다 깸 | `ProcessFirstResume` 0 → 1, `SuspendCount` 1 → 0, PEB `pShimData` · `SystemDefaultActivationContextData` | 부모가 마지막 서류를 넣고 직원을 풀어 줌 |
| **P0~P1** | `PspUserThreadStartup` → `PspInitializeThunkContext` | TEB `CurrentLocale` `0x412`, 맡긴 `_CONTEXT`, 트랩 프레임 `Rip` → `LdrInitializeThunk` | 출발 메모를 "오리엔테이션실로" 로 바꿔 끼움 |
| **P1~P1a** | ntdll 혼자 (`LdrpInitialize` ~ `LdrpInitializeProcess +0x199d`) | PEB `Ldr` · `ProcessHeap` · `FastPebLock` · `LoaderLock`, **`ProcessParameters` 주소 바뀜**, exe·ntdll 명부 줄, 보안 쿠키 새 값. 핸들 4 → 26, VAD 11 → 21 | 관리실이 명부·칠판·창고를 차림 |
| **P1a~P1b** | kernel32·kernelbase 준비 → 병렬 로드 → dll1·dll2 올리기·잇기 | KERNEL32 · KERNELBASE · dll1 · dll2 명부 줄, `Kernel32ThreadInitThunkFunction`, `Csr*` 전역 8개, `ConsoleHandle` `0x60`, `OwnerProcessId` `0x729`, 일꾼 2개 (`ActiveThreads` 1 → 3). 핸들 44, VAD 32 | 이웃 건물 들이기·연결 |
| **P1b~P2** | 그래프 켜기 (dll2 · dll1 TLS · `DllMain`, exe TLS) → `_LdrpInitialize` 뒤쪽 | dll1 · dll2 `ProcessAttachCalled`, 노드 `State` 9, `LdrInitState` 2 → 3, `PEB.ProcessInitializing` 1 → 0, `FlsData` | 이웃 건물 불 켜기, 개업 준비 끝 표시 |

- 지점별 큰 숫자 (dast/04): 핸들 4 · 4 · 4 · 4 · 4 · 26 · 44 · 46, VAD 8 · 9 · 9 · 11 · 11 · 21 · 32 · 32, 모듈 목록 — · — · — · — · — · 2 · 6 · 6 (K1 → P2 순).
- 그러니 **가게 땅과 손잡이가 크게 늘어나는 건 ntdll 단계 (P1 이후)** 다. 커널 단계에선 핸들 4개 (부모에게 받은 콘솔·표준 입출력, flow.md 2.1) 그대로다.

### 2.13 정적 쓴 자리와 맞춰 보기 — 어긋난 줄 0

- 짐 지도 3장 아는 칸 줄 **243개**를 칸 단위로 맞췄다. **단계 (①/②) 와 구간이 어긋나는 줄은 없었다** (run 문서).
  - 어긋나 보이는 것은 모두 "그 줄 뒤에 다른 누가 또 바꿈" 이거나 "조건이 안 맞아 0" 이었다.
- 정적 줄이 사진에 그대로 보인 예 (`[관찰]` + `[정적]`):
  - **프로세스 락**: `PspAllocateProcess +0x26e9` 잡음 → `PspInsertThread +0x2a2` 풀기. 사진 K1 · K2 = 1, K3 = 0 (2.9 의 raw 줄). → run12 의 `[정적 + 간접]` "호출자가 잡아 들고 들어옴" 이 **실제로 맞다**.
  - **트랩 프레임**: 처음 값 = `KxContextToKframes` (K1~K2), 바꿔 끼우기 = `PspSetContext` (P0~P1) (run16). 새 `Rsp` `9b2fcffa18` = `_CONTEXT` 주소 − 0x28.
  - **`LdrInitState`**: P1a 0 → P1b 2 → P2 3. 정적 `+0x1a4c` · `+0x1b1e` 는 P1a 와 P1b 사이, `+0x205` 는 P1b 뒤 — 사진의 구간과 맞다.
  - **모듈 항목 `LoadReason`**: exe 4, KERNEL32 4, KERNELBASE · dll1 · dll2 0 — run22 · run23 짐작과 같은 값.
- 조건이 안 맞아 0 인 것 (정적 줄은 있지만 사진엔 0): `Flags3` 비트 7개, `UseWOW64`, `LdrpIsHotPatchingEnabled`, `AvrfAppVerifierMode` 등.
- 썼다 되돌려서 0 으로 보이는 것: `SpecialApcDisable`, `SuspendSchedulerApcWait`, `LdrpMainThreadToken`, `LdrpTlsLock`, `LdrpCurrentDllInitializer`.
- 비유: 점검표에 "이 준비물은 2번 담당이 3번 사진과 4번 사진 사이에 갖다 놓는다" 고 적힌 243줄을 사진과 대 봤더니, **한 줄도 틀린 시간에 있지 않았다**.

### 2.14 정적 문서에 없던 일 — 대표

정적이 틀린 게 아니라, **아직 안 찾은 쓴 자리**다 (run 문서 "정적과 동적이 다른 곳").

#### (1) `PEB.ProcessParameters` 가 ①→①-a 사이에 힙으로 옮겨진다

- 무엇을 봤나 `[관찰]`:
  - P1 `25996cb0000` → P1a `25996f37a50`. 새 주소는 프로세스 힙 (`25996f30000`) 안이다.
  - 같은 구간에 옛 블록 자리 (private 32쪽) 가 VAD 에서 **사라진다**. 그 주소엔 2쪽짜리 매핑 섹션이 생기는데, TEB `SchedulerSharedDataSlot` 값과 같은 주소다.
  - 새 블록은 길이 · `Flags` · 핸들 · 문자열 길이가 옛 블록과 같다. **안쪽 포인터만** (`CurrentDirectory` · `ImagePathName` · `CommandLine` · `Environment` …) 새 자리 기준이다. `EnvironmentVersion` 만 0 → 3.
- 그래서 무엇으로 보이나:
  - 근거: 내용은 같고, 포인터만 새 자리를 가리키고, 옛 자리는 풀렸다.
  - 그래서: **옛 블록을 힙으로 복사하고, 안쪽 포인터를 고쳐 주고, 옛 자리를 푼 것**처럼 보인다 `[추측]` (run 문서 "아직 확인 필요" 의 "힙에 복사하고 옛 영역을 푸는 줄" 표현).
  - 아직 확인 아님: **누가 옮겼는지는 모른다** `[공백]`. 정적 문서엔 이 구간에 블록을 옮기는 줄이 없다. 후보는 `LdrpInitializeProcess` 앞쪽 `+0x6c5 RtlNormalizeProcessParams` 근처 `[추측]` (run18 은 그 함수가 하는 일을 모른다고 적었다) → Q78.
- 비유: 부모가 써 준 **개업 서류**를, 관리실이 출근하자마자 자기 창고 (힙) 의 새 서류철로 **옮겨 적고**, 서류 안의 "몇 쪽 참조" 쪽수를 새 서류철 기준으로 고친 뒤, 옛 서류 뭉치는 치운 것 같다. 누가 옮겨 적었는지는 사진에 안 찍혔다.
- 왜 중요한가: 에뮬레이터 입장에선 "개업 서류가 어디 있나" 가 ① 과 ② 에서 **다르다**는 뜻이다. ① 의 주소만 보고 짐을 놓으면 ② 이후와 어긋난다.

#### (2) 핸들 표의 주인 PID 가 K2~K3 에 바뀐다

- `HANDLE_TABLE.UniqueProcessId` 가 K1 · K2 에선 **부모 PID** `0x19a8`, K3 부터 자기 `0x1dd0` `[관찰]`.
- 쓰는 줄은 `[공백]` (`PspInsertProcess` 안이거나 `PspInsertThread +0x807` 앞) → Q78.
- 비유: 새 가게의 손잡이 목록 맨 위 "주인" 칸에 처음엔 **부모 가게 번호**가 적혀 있다가, 목록에 올릴 즈음 자기 번호로 바뀐다.

#### (3) `OwnerProcessId` 와 `ConsoleHandle` 이 P1a~P1b 에 바뀐다

- `EPROCESS.OwnerProcessId` `0x19a8` → `0x729` `[관찰]`. 정적으로 아는 쓰는 곳은 `PspAllocateProcess +0x710` 하나뿐 (run33). Q65 에 남은 `NtSetInformationProcess +0x1ecd` 다른 쓰기와 이어질 수 있다 `[추측]`.
- 매개변수 `ConsoleHandle` 4 (`\Reference {ConDrv}`) → `0x60` (`\Connect {ConDrv}`) `[관찰]`. 쓰는 쪽 `[공백]` (KERNELBASE 콘솔 준비 `[추측]`).
- 이 구간엔 KERNELBASE · KERNEL32 시작 함수가 섞인다. 그래서 짐 지도는 이 칸들을 아직 "다른 DLL" 로 세지 않았다 — 구간에 ntdll 코드도 섞여서 주인을 못 정한다 (`baggage.md` 2장).
- **사용자에게 물을 것** (plan 2026-10-25): 이 둘이 Q65 와 겹친다 — Q65 에 합칠지, Q78 에서 볼지.

#### (4) 부모가 ⓪ 전에 자식 PEB 에 쓰는 칸

- `pShimData` `25996d10000` (새 private VAD), `SystemDefaultActivationContextData` `25996d00000` (새 매핑 VAD) 가 **K3~P0** 에 처음 값 `[관찰]`.
- 그 구간엔 부모의 `NtCreateUserProcess` 뒷부분 · `CreateProcessInternalW` 뒤쪽 (csrss 알림 포함) · `NtResumeThread` 가 섞인다. 쓴 쪽은 부모 KERNELBASE · csrss `[추측]`.
- 짐 지도 7.8: "쓴 주인" 이 `다른 DLL` 일 수 있는 **첫 후보**.

#### (5) 일꾼 2개와 `LoaderThreads` 0

- `ProcessParameters.LoaderThreads` = 0 (K1 ~ P2) `[관찰]`.
- `LdrpMapAndSnapWork` 가 P1b 에 0 이 아니고, `ActiveThreads` 가 P1a~P1b 에 1 → 3 `[관찰]`.
- → run24 `[정적]` "0 이면 스레드 수 4 → 일감 개체 생김" 과 run5 `[관찰]` "ntdll 일꾼 2개" 가 **같은 실행에서** 맞아떨어진다.
- run32 `[정적]` "0 이면 `PsDefaultLoaderThreads` 로 채움" 이 있으니 그 커널 전역도 0 이었을 것 `[추측]` — 전역은 안 읽었다 → Q79.

#### (6) 그 밖에

- KERNEL32 · KERNELBASE 의 `ProcessAttachCalled` 는 P1a~P1b, dll1 · dll2 는 P1b~P2 `[관찰]` — 두 시작 함수가 kernel32 준비 안에서 불린다는 flow 2.5 그림과 시점이 맞다.
- `Kernel32ThreadInitThunkFunction` = `7ffafa65cd70` = KERNEL32 + `0x2cd70` `[관찰]` — run25 `[정적]` `BaseThreadInitThunk` rva 와 같다.
- `PsNtdllExports` 는 8개 덤프 모두 같다 `[관찰]`: `+0x8 LdrInitializeThunk` = ntdll + `0xe1420`, `+0x10 RtlUserThreadStart` = ntdll + `0xacac0` 등.

### 2.15 확인된 것 — 맡긴 문맥과 출발 메모

[run16 해설](../02_sprint/run16-thunk-context-해설.md) 2.3 의 이야기 4번 ("출발 메모를 베껴서 사물함에 넣고, 책상 메모는 오리엔테이션실로 바꿔 붙인다") 가 이번에 **실제 값으로** 보였다.

- `[관찰]` **P0 트랩 프레임 = 원래 출발 문맥**: `Rip` `RtlUserThreadStart` (`7ffafb70cac0`), `Rcx` 진입점 `7ff61a371d40`, `Rdx` PEB, `Rsp` `9b2fcfff48`, `EFlags 0x200`. K2 · K3 사진도 같은 값.
- `[관찰]` **P1 의 맡긴 `_CONTEXT`** (`9b2fcffa40`, P0 까지는 읽기 불가) = 위 넷과 같다 (`ContextFlags 0x10001b`). → run16 `[정적]` "원래 문맥을 유저 스택에 맡김" 이 실행 값으로 맞다.
- `[관찰]` **P1 트랩 프레임**: `Rcx` = `_CONTEXT` 주소, `Rdx` = ntdll 베이스 `7ffafb660000`, `Rsp` `9b2fcffa18` (= `Rcx` − 0x28). `Rip` 는 +1 (2.7) 이라 라이브 `rip` `7ffafb741420` 로 본다.
- `[관찰]` **P2 의 `_CONTEXT`** 는 ① 과 같은 주소, **0x4d0 바이트가 모두 같다**. 로더가 사물함 메모를 안 건드렸다.
- `[관찰]` **P2 콜스택** `NtContinue` ← `LdrInitializeThunk+0x18`, `rcx` = 같은 `_CONTEXT` 주소. → 그림 2 의 `NtContinue` 상자가 실선이 된 근거 (3절).
- `[관찰]` `KTHREAD.Teb` = TEB 주소 (run11 `[추측]` 이던 칸 이름이 값으로 맞음). test.exe 첫 스레드는 `PspAllocateThread` **보통 길** (스택·TEB 를 새로 만듦) 을 탔다 — K2 에 TEB · `DeallocationStack` · 트랩 프레임이 생겨 있다.
- 비유: 사물함 메모를 오리엔테이션 전 (P1) 과 끝난 뒤 (P2) 에 둘 다 찍어 보니 **글자 하나 안 바뀌었다**. 그리고 그 메모를 들고 신청 창구 (`NtContinue`) 앞에 선 순간도 찍혔다.

### 2.16 그래서, 사진은 정답지인가 — 사용자 질문에 대한 답

> "스냅샷이 정답지인가, 거꾸로 언제 세팅됐는지 찾는 건가?"

#### 정답지 역할 — 된다, "그 순간 무엇이 있었나" 에 대해서는

- 8장 각각은 **그 순간 메모리에 그 값이 있었다** 의 증거다 `[관찰]` (flow.md 기준 메모: "run44 의 `[관찰]` 은 '그 순간 메모리에 그 값이 있었다' 이다").
- 특히 P2 는 짐 지도 범위 **끝 순간**이다. 에뮬레이터가 은퇴 직전에 맞춰야 할 값 (`LdrInitState` 3, 모듈 6개, 핸들 46, VAD 32 …) 의 **정답지**로 쓸 수 있다 (짐 지도 5.0).
- 그리고 정적 줄 243개가 틀린 시간에 있지 않다는 **채점**도 했다 (2.13).

#### "언제" 찾기 역할 — 된다, 다만 **구간까지만**

- 사진끼리 비교해서 칸마다 "A 와 B 사이" 를 냈다 (2.10). 1,456칸 중 1,428칸, 493개 중 332개.
- 하지만 구간 안 **몇 번째 줄**에서인지는 모른다. 한 구간에 함수 여럿 · 부모 쪽 코드 · 다른 스레드가 섞여 있다.

#### 정답지가 못 보여 주는 것 셋

1. **"누가" 썼나** — 사진은 결과만 찍는다. 배달한 사람은 안 찍힌다.
2. **0 을 쓴 칸** — 처음부터 0 인 것과 누가 0 을 쓴 것이 사진에선 똑같다.
3. **잠깐 썼다 지운 값** — 두 사진 **사이에서** 생겼다 사라진 값은 어느 사진에도 없다. 예: `SpecialApcDisable` 는 `PspInitializeThunkContext` 안에서 −1 했다 되돌려서 8장 모두 0 (flow.md 2.3). 정적 줄이 있으니까 "썼다 지웠다" 를 아는 것이지, 사진만으론 "아무도 안 썼다" 와 구별이 안 된다.

- 덧붙여 **찍히지 않은 곳**도 있다: K1 ~ P1 의 ntdll 칠판 (`≤P1a`), `.00cfg` 5칸, VAD 노드 칸 (2.11, 4절).

#### 그래서 다음은 정적 (Q78)

- 사진이 해 준 일 = 서랍 1,275개 중 **"언제 물건이 들어왔나" 를 구간으로 표시**해 둔 것.
- 남은 일 = 그 구간 안 함수만 열어서 **누가 넣었나**를 찾는 것 → **Q78 (정적)**.
  - 짐 지도 2.1 · 7.8: 모르는 칸 중 구간이 `K1 이전` 이 아닌 곳으로 정해진 것 **254** (유저 44 · 커널 187 · 전역 23) 은 "그 구간 안 함수만 뒤지면" 된다.
  - 예: `K1~K2` TEB 8칸 ↔ `MmCreateTeb` 대입 약 47줄 (run43), `P0~P1` 칸 ↔ `PspUserThreadStartup` 정상 길 4개.
- 비유: 사진은 "화분은 3번과 4번 사진 사이에 들어왔다" 까지 알려 준다. 그 사이에 가게에 드나든 사람 명단 (그 구간의 함수들) 을 펴 놓고 **누가 들고 왔는지**를 찾는 게 Q78 이다. 명단이 짧아졌으니 찾기가 훨씬 쉽다.

### 2.17 디버거가 붙어서 달라진 점 (run 문서 "디버거 영향")

- 멈춤점 지점의 트랩 프레임 `Rip` = 멈춘 주소 + 1 (2.7). P1 ~ P2 의 트랩 프레임 칸은 짐이 아니다.
- 멈춘 동안 코드 바이트는 원래 값이다 (`LdrInitializeThunk` 입구 `40 53 48 83 …`, dast/01 · 02). 덤프 안 코드도 원래 바이트.
- 덤프의 CPU 문맥은 디버거 대기 루프 (2.6).
- 스레드 통계 · 기다림 칸 · KUSD 시각 칸에는 멈춰 있던 시간이 섞인다 `[추측]`.
- P1 · P2 멈춤점은 스레드 조건이 붙어 있어서, 일꾼이 같은 자리를 지나면 멈췄다가 바로 갔을 수 있다 `[추측]` — 시간만 조금 바뀐다.
- `PEB.BeingDebugged` = 0 (K1 ~ P2) `[관찰]` — 커널 디버거만 붙어서는 test.exe 가 "디버거 붙음" 을 모른다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 절 | 전 | 후 (run44) |
|---|---|---|
| 기준 "근거 로그" | 01_sprint run3 ~ run8 (디버거) | + 03_sprint run44 (스냅샷 8장 → `vmss2core` → WinDbg 오프라인). 메모: run44 `[관찰]` = "그 순간 메모리에 그 값이 있었다". 콜스택으로 본 호출은 지점 8곳의 `k` 뿐 |
| 그림 2 `ntdll!NtContinue` 상자 (`q1`) | 점선 `(정적)` "코드 사슬은 정적, 실행은 미관찰" | **실선** — P2 콜스택 `NtContinue` ← `LdrInitializeThunk+0x18`, `rcx` = 맡긴 문맥 `[관찰]`. `SYSCALL` 뒤 커널 쪽 상자는 그대로 `(정적)` |
| 그림 2 나머지 | — | 그대로. 사진은 호출이 아니라 칸 값이라 화살표를 바꿀 근거가 없다. 8 지점 콜스택은 이미 그림에 있는 `+0x…` 호출과 같다 |
| 2.1 `MmCreatePeb` PEB 표 | 칸 이름 `[추측]` (알려진 PEB 배치) | 칸 이름 `[정적]` (PDB `_PEB`, run43 에서 미뤄 둔 태그 정정) + K1 사진에 그 칸들이 이미 값을 가짐 `[관찰]` |
| 2.1 `PspAllocateThread` · `PspInsertProcess` · `PspInsertThread` | `[정적]` 코드 순서 | `[관찰]` 줄 추가: K1 엔 첫 스레드 없음 / K2 엔 다 있음, **보통 길**, 프로세스 락 K1 · K2 1 → K3 0, `SuspendCount` K3 1, `HANDLE_TABLE.UniqueProcessId` 부모 → 자기 |
| 2.2 부모 마무리 | — | K3~P0 에 자식 PEB `pShimData` · `SystemDefaultActivationContextData`, `ProcessFirstResume` 0 → 1 `[관찰]` (구간만 — `+0x45` · `+0x56` 자리에서 본 건 아님) |
| 2.3 `PspUserThreadStartup` · `PspInitializeThunkContext` | 바꿔 끼우기 값 `[정적]` | P0 · P1 트랩 프레임과 `_CONTEXT` 값 `[관찰]` — "바꾸기 전 · 후" 가 관찰 |
| 2.4 · 2.5 · 2.6 · 2.10 | 실행 값 `[정적]` 갈림 조건 | 사진 값 `[관찰]` 줄 추가: `LdrpInitializeProcess` 앞 · 중 · 뒤 세 구간, `ProcessParameters` 옮겨짐, 일꾼 2개 구간, `Kernel32ThreadInitThunkFunction` 값 |
| 4장 공백 표 | 2.1 보통 길 / 프로세스 락 / 알림 문턱, 2.2 bit0 · `ProcessFirstResume`, 2.4 `BeingDebugged`, 2.10 Q48 값 … | 여러 줄이 **(해결됨)** 또는 **(일부 해결)**. 새 줄 다섯 (짐 지도 표시): `ProcessParameters` 를 누가 옮기나, `HANDLE_TABLE.UniqueProcessId`, `OwnerProcessId` · `ConsoleHandle`, 부모가 자식 PEB 에 쓰는 칸, 사진으로 못 좁힌 것 |
| 5장 근거 run | run43 까지 | run44 줄 (dast/01 ~ 04, sast 없음) |

- 핵심 하나만 고르면: **흐름도 그림은 거의 그대로**고 (실선 하나), 대신 **각 절에 "그 순간 실제 값" 줄이 붙었다.** 흐름도는 동선, 사진은 짐이라서다.

### 짐 지도 `baggage.md` 에서 바뀐 것 (둘째판)

| 곳 | 첫판 (run43) | 둘째판 (run44) |
|---|---|---|
| 2장 "0 그대로" 열 | 전부 "사진 전" | 숫자가 찼다: 구조체 847 (유저 285 · 커널 562), 전역 297. `*` · "칸 안 읽음" · "못 읽음" 표시 |
| 2.1 (새 절) | — | 칸마다 처음 값 구간 표 (2.11 의 표) + 모르는 칸만 따로 센 줄 (254 = Q78 후보) |
| 3장 | 아는 칸만 | 243줄 모두 근거 칸 끝에 "사진" |
| 5.0 (새 절) | 코드 순서만 | 사진으로 본 **구간 순서** 8단계 (2.12 의 표) |
| 7.1 | "0 그대로 — 사진 전" | 사진으로 셈. "아무도 안 씀" 의 증거가 아님을 다시 적음 |
| 7.2 · 7.3 | 모르는 칸 이름 나열 | **사진 구간별로** 묶음 |
| 7.8 (새 절) | — | 사진이 새로 연 빈 곳 (2.14 의 (1) ~ (4), 못 좁힌 것, `LoaderThreads`) |

- 짐 지도의 "쓰는 말" 에 사진 지점 · 사진 구간 · "끝까지 0 은 아무도 안 씀과 같지 않다" · 트랩 프레임 덮어쓰기가 더해졌다.

### 큰 그림에서 어떻게 이어지나

run43 해설 3절의 "다음 두 바퀴가 채우는 것" 표를 이번 결과로 다시 보면:

| 2장 열 | run43 | **run44 (이번)** | Q78 이 할 일 |
|---|---|---|---|
| 0 그대로 | 전부 "사진 전" | **채움** (847 · 297) | 그중 "0 으로 둬도 되나" 를 쓴 자리와 같이 봄 (7.1) |
| 아직 모름 | 1,275 · 439 | 이름은 그대로지만 **구간이 붙음** — 254개는 구간 안 함수만 보면 됨 | 그 함수들을 열어 **누가** 썼나 |
| 다른 DLL | 0 | 여전히 0. 후보는 생김 (P1a~P1b 칸, K3~P0 부모 칸) | 섞인 구간의 주인을 가름 |

- 그러니 이번 바퀴로 **Q78 은 "어디부터 열어 볼지" 를 알고 시작한다.** 서랍 1,275개를 다 여는 대신, 구간이 정해진 254개는 그 구간 함수만 연다.
- 덤프 8개는 서버에 남아 있다 (`ms/dump/03_baggage-snap/`, 커밋 안 함). 그래서 **다시 VM 을 돌리지 않고도** 더 읽을 수 있다 → Q79.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 구간이 정해진 모르는 칸 254개를 **누가** 쓰나 (유저 44 · 커널 187 · 전역 23) | 사진은 "언제" 만 말한다 (2.10) | **Q78** (정적) — 구간 안 함수만 |
| `PEB.ProcessParameters` 를 누가 힙으로 옮기나, `EnvironmentVersion` 0 → 3 | 정적 문서에 그 줄이 없다 `[공백]` | **Q78** (정적) — `LdrpInitializeProcess` 앞쪽, `+0x6c5 RtlNormalizeProcessParams` 근처 `[추측]` |
| `HANDLE_TABLE.UniqueProcessId` 를 부모 → 자기 PID 로 바꾸는 줄 (K2~K3) | 같음 `[공백]` | **Q78** (정적) |
| `OwnerProcessId` `0x729` · `ConsoleHandle` `0x60` 을 누가 쓰나 (P1a~P1b) | 같음. KERNELBASE 콘솔 준비 → `NtSetInformationProcess` 쪽 `[추측]` | **Q78** 또는 **Q65** — 사용자와 정할 것 (plan 2026-10-25) |
| 부모가 자식 PEB 에 쓰는 칸 (`pShimData` · `SystemDefaultActivationContextData` · `ExceptionPortData` …) 의 주인 | 구간에 부모 KERNELBASE · csrss · 커널이 섞임 | **Q78** (정적) / 동적 |
| "끝까지 0" 847 · 297 중 정말 아무도 안 쓰는 것 | 0 을 씀 · 썼다 지움 · 조건 안 맞음을 사진으로 못 가림 (2.16) | **Q78** (쓴 자리와 같이 봄) |
| VAD 노드 칸 23개 (`_MMVAD` 9 · `_MMVAD_SHORT` 14) | 이번엔 `!vad` 목록만 읽었다 (명령 묶음에서 빠짐) | **Q79** (오프라인, 덤프 그대로 — 노드마다 `dt`) |
| `PsDefaultLoaderThreads` 값 (`LoaderThreads` 0 의 까닭) | 커널 전역을 안 읽었다 | **Q79** (오프라인, 덤프 하나로 충분) |
| ntdll 칠판 `≤P1a` (`.data` 143 · `.mrdata` 13) 와 `PebLdr` 5칸 — P1 에 이미 값이 있었나 | K1 ~ P1 에선 그 페이지가 읽기 불가 (2.11) | **Q80** (정적) — ntdll 파일 `.data` 처음 값과 P1a 값 비교. 동적 방법 (`!pte` / 물리 주소로 읽기) 은 run 문서에만, 번호 없음 |
| `.00cfg` 5칸 | 8장 모두 읽기 불가 | 번호 없음 (run 문서 "아직 확인 필요", 짐 지도 7.8 — 물리 주소 `!db` 같은 다른 읽는 법) |
| `KTHREAD` · `KTRAP_FRAME` 의 P1a 이후 칸 | 디버거 · 시스템 콜 값이 덮임 (2.7) | 멈춤점 없는 순간이 필요 — **이번 범위에선 안 함** (run 문서) |
| 간접 호출 10줄의 실제 대상 | 사진으론 못 봄 (호출이 아니라 값) | 그 줄에 멈추는 동적 (짐 지도 7.7 · 7.8) |
| 일꾼이 풀 만들 때 생기나, 일감 올라올 때 생기나 | 둘 다 P1a~P1b 한 구간 안이라 사진으로 못 가름 | 그대로 **Q47** (flow.md 3장) |

- 대기열 순서 (plan): **Q78** (정적) 이 다음이다. Q79 · Q80 은 이번 바퀴에 새로 생겨 맨 뒤에 붙었다.

---

## 5. 더 읽기

- run 문서: [`run44-baggage-snap.md`](run44-baggage-snap.md) — "방법" (C 방식과 시행착오 · 8 지점 · 지점끼리 비교한 법 · 판정 규칙), "결과" (요약 표 · 구간마다 · 정적과 맞춰 보기 · 새로 확인된 것), "디버거 영향", "아직 확인 필요"
- 짐 지도: [`baggage.md`](baggage.md) — "쓰는 말" (사진 지점 · 사진 구간), 2장 · **2.1**, 3장 근거 칸 끝 "사진", **5.0**, 7.1 · 7.2 · 7.3 · **7.8**
- 흐름도: [`flow.md`](flow.md) — 기준 "근거 로그", 그림 2 `q1`, 2.1 ~ 2.10 의 `(run44 사진)` 줄, 4장 공백 표
- 판정: `ms/logs/01_snapshot-test_result.md` (암호화 전 시험), `02_snapshot-test_result.md` (C 방식 성공 · lazy · 디버거 루프), `03_baggage-snap_result.md` (8 지점 캡처), `04_baggage-read_result.md` (덤프 읽기 · 보충)
- 요청서 · runbook: `_workspace/dast/01_snapshot-test.md` ~ `04_baggage-read.md`
- 로그: `ms/logs/03_baggage-snap.txt` (라이브, 끝 HOST NOTES), `ms/logs/04_baggage-read_<P>.txt` (+ `_supp_<P>.txt`)
- 진행표: `_workspace/plan.md` Q77 (끝) · Q78 · Q79 · Q80, 결정 기록 2026-10-25
- 앞 해설: [run43](run43-baggage-checklist-해설.md) (점검표 · 상태 사진 · 줄기) · [run15](../02_sprint/run15-user-thread-startup-해설.md) 2.6 (트랩 프레임) · [run16](../02_sprint/run16-thunk-context-해설.md) 2.3 · 2.6 (사물함 메모 · 바꿔 끼우기) · [run18](../02_sprint/run18-proc-init-setup-해설.md) 2.6 (건물 명부) · [run32](../02_sprint/run32-params-initblock-해설.md) (개업 서류 · 업무 수첩)
