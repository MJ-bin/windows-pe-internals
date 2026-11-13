# run87 sxs-create — 해설

> 이 글은 [run87 문서](run87-sxs-create.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.
>
> **고쳐 읽기 (run88)**: "run88 에서 다룸" 으로 남긴 자리 ("조심할 곳" 1, 2.7 끝 "책자 재료", 2.12 끝, 2.13 끝, 3절 짐 지도 끝 · "큰 그림에서" 끝, 4절 표 1 · 6 · ② · 마지막 줄) 의 답은 [run88 해설 2.12](run88-sxs-redirect-해설.md) 에 모았다 — 블롭은 위치를 전부 오프셋으로 적어 **어느 주소에 붙여도 읽히고**, 로더는 스레드 문맥 → `+0x2f8` → `+0x308` 차례로 편다. 블롭엔 **키만** (조립 폴더 이름 + 파일 이름) 있고 루트 `C:\WINDOWS\WinSxS\` 는 ntdll 이 붙이며, 폴더를 여는 쪽은 짐작대로 **ntdll** (`RtlpProbeAssemblyStorageRootForAssembly +0x26c NtOpenFile`). 공통 책자엔 조립 6개 · **v6 없음** `[관찰: 사진 덤프 오프라인, test.exe 블롭인지는 머리 0x20B 만 확인]`, "6.0.0.0" 은 `6.0.26100.9278` 로 묶였다 (고르는 코드 길은 Q164 ②). PEB `+0x300` · `+0x310` 은 ntdll `RtlpGetActivationContextDataStorageMapAndRosterHeader +0x292` 가 쓴다 (test.exe 는 안 탐).

- 대기열: **Q151a** — PE_emulator 요청 29 (SxS 활성화 문맥) 의 **첫 조각, 만드는 쪽** (`_workspace/plan.md`)
  - Q151 (요청 29 전체) 은 둘로 나눴다 (2026-11-13).
    - 만드는 쪽 — 부모 → csrss → 자식 PEB = **Q151a** (이번, run87 · sast/84).
    - 읽는 쪽 — 로더가 DLL 이름을 돌리는 길, 데이터 안쪽 모양 = **Q151b** (다음, run88 · sast/85).
  - 겹치던 **Q87** (부모 `BasepInitAppCompatData` 본문 — 부모가 자식 PEB 를 쓰나) 도 이번에 **닫혔다**.
  - 다음 항목: **Q151b** → Q152 → Q142 → [멈춤] (Q104 VM 단계 앞).
  - 이번에 남은 것이 간 곳 (4절):
    - 실행해 봐야 아는 것 → **Q104** (1 · 2 · 4 · 5 · 7) · **Q83** (3) · **Q162** (6).
    - 정적 → **Q164** ① ~ ③.
    - 블롭 안쪽 · 로더 쪽 → **run88**.
- 근거:
  - run 문서 [`run87-sxs-create.md`](run87-sxs-create.md) (가장 중요)
  - 요청서 [`_workspace/sast/84_sxs-create.md`](../../_workspace/sast/84_sxs-create.md) · 결과서 [`84_sxs-create_result.md`](../../_workspace/sast/84_sxs-create_result.md)
    - 판정 **분석성공 (지도 수준)**. 질문 1 · 2 · 3 은 함수 · 오프셋 · syscall 까지, 5 는 판단 + 이유, **4 는 일부**.
    - sxssrv · sxs 를 **처음 analyze** 했다 (PDB 붙음, 함수 99개 · 901개).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · **그림 1 (상자 둘 더함, 번호 다시 매김)** · 2.2 · 4장 · 4.1 · 5장 (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): **새 짐 칸 셋** (3.1 PEB, 마흔셋째판)
  - 같이 고친 것: [run45 문서](run45-work-list.md) 맨 위 정정 한 줄 (일 37)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없다.
  - `[관찰]` 로 적힌 값은 flow-writer 가 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_*.txt`) 에서 **다시 읽은 것** 이다 (2.11).

### 이번 바퀴에서 조심할 곳 다섯

1. **이번은 "만드는 쪽" 만이다.**
   - 자식 PEB 에 **무엇이, 누구 손으로** 들어가나 까지다.
   - 그 데이터의 **안쪽 모양**, 로더가 **어떻게 읽나**, 시스템 기본 문맥의 **실제 내용**, 블롭 안에 폴더가 **전체 경로냐 키만이냐**, "6.0.0.0" 요청이 **어느 버전으로 묶이나** 는 → **run88 에서 다룸**. 이 글은 앞질러 쓰지 않는다.
2. **쓰는 손이 test.exe 밖에 있다.**
   - 자식 PEB 칸 셋을 쓰는 건 **부모 cmd** 와 **csrss** 다. 둘 다 다른 프로세스다.
   - test.exe 첫 스레드는 그때 **멈춰 있다** (사진 구간 `K3~P0`).
3. **test.exe 는 매니페스트가 없다.**
   - 그래서 셋 중 `+0x308` · `+0x2d8` 은 채워지고 `+0x2f8` 은 비어 있다 `[관찰]` (2.11).
   - 매니페스트가 있는 exe (notepad 류) 이야기는 "그런 exe 라면" 으로 읽는다.
4. **이름 정정 — kernel32 다.**
   - `BasepConstructSxsCreateProcessMessage` · `BasepInitAppCompatData` 는 **KERNELBASE 가 아니라 kernel32** 함수다 `[정적]`. KERNELBASE 는 지연 import 로 부를 뿐이다 (2.3).
   - 요청서 sast/84 · 대기열 Q87 · 짐 지도 7.6 · run45 일 37 이 "KERNELBASE" 라고 적었다. 짐 지도와 run45 는 고쳤고, 요청서는 결과서가 정정한다. plan.md Q87 행 이름은 오케스트레이터 몫이다 (run 문서 "정정").
5. **사진 이름.**
   - 결과서는 사진 값 (`25996d00000` 등) 을 "run45 사진" 이라고 부른다. run 문서는 같은 값을 **run44 덤프 읽기 로그** 줄로 적었다. 이 글은 run 문서를 따라 "run44 사진" 으로 쓴다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜, 간접 호출, 한 주소에 이름 여럿 (ICF) → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) = 가게 **안내판**, WOW64 = 64비트 Windows 에서 32비트 프로그램을 돌리는 장치 → [run10 해설](../02_sprint/run10-allocate-process-해설.md)
- 부모 cmd = 새 가게를 **신청하는 쪽 가게**, 멈춘 채 만들기 = **"대기" 팻말**, csrss = 이웃 **인사팀 사무실**, `CsrClientCallServer` `0x1001d` = 인사팀에 내는 **새 가게 등록 서류**, `pShimData` = 안내판에 적는 **"호환 안내문" 위치** → [run14 해설](../02_sprint/run14-create-process-internal-해설.md) 2.6
- 1번 창구 = basesrv, `BaseSrvCreateProcess2` · `InternalBaseSrvCreateProcess` · `CsrCreateProcess` (사무실 명부에 올리기) → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md)
- csrss 연결 = 사무실에 거는 **전화** → [run68 해설](run68-dll-init-rest-해설.md) 2.4, 캡처 버퍼 = **답장 봉투** → [run85 해설](run85-user32-init-해설.md) 2.4
- 매니페스트 = exe 에 붙인 **설정 쪽지** (XML), 활성화 문맥 = 그 쪽지를 읽어 만든 **설정 묶음** → [run57 해설](run57-nls-apiset-해설.md) 2.8 · [run67 해설](run67-kb-heap-init-해설.md) 2.5 (run22 해설에선 "활성화 컨텍스트")
- SxS 리다이렉트 = 매니페스트를 보고 DLL 이름을 다른 버전 파일로 돌리기 → [run23 해설](../02_sprint/run23-load-dependent-해설.md) 2.5 "곁가지"
- 커널이 exe 의 `RT_MANIFEST` ID 1 자원을 찾는 `PspLocateInPEManifest` → [run59 해설](run59-init-leftovers-해설.md) "나머지 C 묶음" ⓔ
- 섹션 객체 = **주문 꾸러미** → [run31 해설](../02_sprint/run31-image-section-해설.md), 뷰 = 섹션을 우리 땅에 **펼쳐 놓은 한 판** (`NtMapViewOfSection`) → [run38 해설](../02_sprint/run38-map-module-해설.md) 2.3
- 공유 섹션 = 여러 가게 벽에 같은 판을 붙이는 **게시판** → [run71 해설](run71-named-obj-dir-해설.md) 2.4, pagefile 섹션 = 받치는 **파일이 없는** 섹션 → [run57 해설](run57-nls-apiset-해설.md) 2.3
- 새 가게 벽에 붙이고 안내판에 번지를 적기 (ApiSet 별명 사전) → [run57 해설](run57-nls-apiset-해설.md) 2.4
- delay import (지연 import) = **"필요하면 그때 부를게요" 메모** → [run25 해설](../02_sprint/run25-dll-entry-해설.md)
- CFG 간접 호출 = **교환원** 을 거쳐 부르기 → [run20 해설](../02_sprint/run20-kernel32-load-해설.md) 2.9
- `KUSER_SHARED_DATA` (KUSD) = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- VAD = 땅의 **구획 장부**, 사진 K3 · P0 → [run44 해설](run44-baggage-snap-해설.md) 2.8

이번에 처음 나오는 말 — **SxS**, **sxssrv · sxs.dll**, **블롭**, **시스템 기본 문맥 / 앱 문맥**, **지연 import 의 "있나" 검사** (그리고 짧게 **보호 프로세스**) — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, PEB = 가게 **안내판**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**.
- 부모 cmd = 새 가게를 **신청하는 쪽 가게**. 새 가게 첫 직원은 이때 **"대기" 팻말** 을 건 채 기다린다 (run14).
- csrss = 이웃 **인사팀 사무실**, basesrv = 그 사무실 **1번 창구**. 부모는 가게가 지어진 뒤 1번 창구에 **등록 서류** 를 낸다 (run14 · run34).
- 매니페스트 = **설정 쪽지**, 활성화 문맥 = **설정 묶음** (run57 · run67).
- 이번에 새로 붙는 그림:
  - 설정 묶음을 한 덩어리로 **찍어 낸 책자** = 블롭.
  - 책자는 두 가지다 — 모든 가게에 같은 것을 거는 **공통 책자** (시스템 기본 문맥), 쪽지가 있는 가게만 받는 **가게 전용 책자** (앱 문맥).
  - 인사팀 사무실 1번 창구 뒤에 **책자 담당** (sxssrv) 이 앉아 있다. 담당은 **제본 기계** (sxs.dll) 로 쪽지를 읽어 책자를 찍는다.
  - 책자는 새 가게 벽에 **읽기 전용** 으로 걸어 주고, 안내판에 "책자는 벽 몇 번지" 를 적는다.
  - 부모의 등록 서류엔 **"설정 쪽지 칸"** (SxS 칸) 이 따로 있다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| ① 자식 PEB 의 문맥 칸 둘 (`+0x308` · `+0x2f8`) 은 누가 쓰나 | **csrss** (sxssrv). 부모가 아니다 `[정적]` (2.7 · 2.8) |
| ② 부모는 무엇을 하나 | kernel32 `BasepConstructSxsCreateProcessMessage` (지연 import) 로 등록 메시지 안 **SxS 칸 `0x1c8`B** 를 채워 보낼 뿐. 보통 길에선 매니페스트 파일도 자원도 **안 연다** (Flags `0x40` = "csrss 가 찾아라") `[정적]` (2.3 · 2.4) |
| ③ 매니페스트 없는 exe 도 받나 | **`+0x308` 시스템 기본 문맥은 늘 받는다** (보호 프로세스류만 빼고). csrss 가 구조마다 한 번 만들어 돌려쓴다 `[정적]`. test.exe 사진도 채워져 있다 `[관찰]` (2.7 · 2.11) |
| ④ 매니페스트 해석은 어디서 하나 | **csrss 안 sxs.dll** (`SxsGenerateActivationContext`). 자식 프로세스 안이 아니다 `[정적]` (2.8) |
| ⑤ 부모가 자식 PEB 에 직접 쓰는 칸은 | `pShimData` (`+0x2d8`) **하나** — `CreateProcessInternalW +0x57d0`. `BasepInitAppCompatData` 는 PEB 를 안 쓴다 → **Q87 닫힘** `[정적]` (2.9) |
| ⑥ PE_emulator 는 무엇을 하면 되나 | 블롭을 미리 떠 와 **읽기 전용으로 붙이고 PEB 칸에 주소만** 쓰면 된다. 매니페스트 해석은 흉내 안 내도 된다 `[정적 판단 — 결과서 질문 5]` (2.12) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

flow.md **2.2 부모 쪽 마무리** 구간이다. 커널이 새 프로세스를 다 만들고 돌아온 뒤 (`NtCreateUserProcess`), 부모가 첫 스레드를 풀어 주기 (`NtResumeThread`) 전.

```
cmd.exe 의 부탁 (CreateProcess)
 └─ KERNELBASE!CreateProcessInternalW                 (부모 cmd 스레드)
      ├─ (1) NtCreateUserProcess  ══▶ 커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 (멈춘 채)
      │        · 매니페스트 자원이 있으면 커널이 여기서 찾아 둔다 (PspLocateInPEManifest)
      │        · ●K3 사진은 이 안 (PspInsertThread +0x807)
      ├─ (2) SxS 칸 채우기 (kernel32)                              ← 이번
      ├─ (3) csrss 등록 ┄┄▶ csrss: SxS 문맥 → 자식 PEB +0x308 (+0x2f8)  ← 이번
      ├─ (4) 자식 PEB +0x2d8 pShimData                              ← 이번
      └─ (5) NtResumeThread  ══▶ 첫 스레드 깸
               ●P0 사진 (PspUserThreadStartup 입구)
 … → ntdll!LdrInitializeThunk → … → NtContinue (은퇴) → exe 진입점 → main
```

- (1) ~ (5) 는 flow.md 그림 1 의 **새 번호** 다. 전엔 (1) · (2) · (3) 셋이었는데, 이번에 (2) 와 (4) 가 끼어들었다 (3절).
- 이번 칸 셋은 **K3 와 P0 사이** 에 값이 생긴다 `[관찰]` (run44).

### 왜 이 질문이 지금 왔나

- PE_emulator 엔 부모도 csrss 도 없다. 그래서 진짜 Windows 에서 둘이 만들어 넣는 문맥 데이터가 **통째로 빠져 있다** (요청서 "배경").
- 막히는 장면 `[관찰 — PE_emulator 흉내]` (요청서):
  - 덤프 notepad 는 매니페스트로 comctl32 **v6** (WinSxS) 를 쓴다.
  - PE_emulator 는 활성화 문맥이 없어 System32 comctl32 **v5.82** 를 올린다 → 서수 #345 · #381 이 없다 → `NtRaiseHardError(0xc0000138)`.
  - GUI 앱 대부분이 같은 길이다.
- 그래서 묻는다: **nt 엔진이 그 데이터를 직접 넣을 수 있나, 최소 모양은 무엇인가.** 이번 바퀴는 그 앞 반쪽 — "누가 · 언제 · 어떻게 넣나" 다.
- 사용자 결정 (2026-11-12): 첫 답은 **지도 수준**.

**새 말 — SxS**

- SxS = "Side-by-Side" (나란히) 의 줄임으로 알려져 있다. 같은 DLL 의 **여러 버전을 WinSxS 폴더에 나란히** 두고, 매니페스트를 보고 골라 쓰는 장치다 `[추측]` (일반적으로 알려진 설명).
- 활성화 문맥 = 그 고르기에 쓰는 **설정 묶음** 이다 (위 링크). run23 해설의 "SxS 리다이렉트" 가 이 묶음을 보고 DLL 이름을 돌리는 쪽이다.
- 비유: 창고에 같은 이름 부품 (comctl32) 이 v5.82 · v6 두 가지 있다. 가게 설정 묶음에 "v6 써요" 가 있어야 v6 을 꺼내 온다. 묶음이 없으니 PE_emulator 는 기본 선반 (System32) 의 v5.82 를 꺼내 왔고, 거기엔 필요한 부품 번호가 없었다.

### 무엇이 궁금했나

요청서 질문 다섯.

1. **부모 쪽** (`CreateProcessInternalW` 의 SxS 갈래만) — 매니페스트를 찾는 줄, csrss 로 보내는 SxS 메시지를 만드는 함수와 메시지 모양, `CsrClientCallServer` 자리. 매니페스트가 **없을 때** 는 어떻게 되나.
2. **csrss 쪽** — 블롭을 만드는 함수, 자식에 넣는 법 (섹션을 붙이나? 메모리에 쓰나?), 자식 PEB 칸에 주소를 쓰는 쪽이 **csrss 인가 부모인가**.
3. **시스템 기본 문맥** — 매니페스트 없는 exe 에도 늘 만드나, 내용은 무엇으로 정해지나.
4. **WinSxS 경로 규칙** — 블롭 안에 무엇이 들어가나, 폴더를 실제로 찾는 쪽은 누구인가.
5. **nt 엔진이 직접 넣는다면** 무엇을 흉내 내야 하나.

그리고 겹치던 **Q87** — "부모 `BasepInitAppCompatData` 가 자식 PEB `pShimData` · `SystemDefaultActivationContextData` 를 쓰나".

### 바퀴 전에 알던 것

- `[관찰]` (run44 사진 → run45 일 37) K3 → P0 사이에 자식 PEB `pShimData` (`25996d10000`, 새 private VAD) · `SystemDefaultActivationContextData` (`25996d00000`, 새 매핑 VAD) 가 처음 값을 갖는다. 쓴 쪽은 몰랐다 `[추측: 부모 KERNELBASE · csrss 쪽]`.
- `[정적]` (run14) `CreateProcessInternalW` 는 `+0x445f BasepConstructSxsCreateProcessMessage` → `+0x4640 BasepCsrCaptureSxsMessage` → `+0x4672 CsrClientCallServer` (`0x1001d`, 길이 `0x218`) 를 지난다.
  - 그 뒤 `NtWriteVirtualMemory` 세 번 (`+0x5795` · `+0x57d7` · `+0x5831`). 둘째가 자식 PEB `+0x2d8` 자리에 쓴다 — `pShimData` 자리로 보인다 `[추측]`.
  - 앞의 "있나" 검사는 같은 주소에 이름 21개가 붙어 있어서 "어떤 import 가 있는지 알려 주는 검사" `[추측]` 로만 적었다.
- `[정적]` (run34) csrss basesrv `InternalBaseSrvCreateProcess` 안에 **SxS 쪽 간접 호출** (ret `+0x100`, 조건부) 이 있다. 표는 `BaseSrvRegisterSxS` 가 채운다. 그 안은 안 봤다.
- `[정적]` (run59, sast/54) 커널 `PspLocateInPEManifest` 가 exe 의 `RT_MANIFEST` ID 1 자원을 찾아 매개변수 `Flags` 비트 13 등을 켠다. test.exe 는 자원 디렉터리가 없다.
- `[관찰 — PE_emulator 흉내]` 위의 `0xc0000138`.

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 안내판 칸 셋, 쓰는 손 둘

run 문서 "호출 관계" 그림을 줄여 그렸다. 라벨은 ret 자리. 모두 `[정적]`.

```
(부모 cmd 스레드)  KERNELBASE!CreateProcessInternalW
 ├─ +0x30ee  NtCreateUserProcess                커널이 매니페스트를 찾아 둠 (있으면)
 ├─ +0x445f  KERNEL32!BasepConstructSxsCreateProcessMessage   SxS 칸 0x1c8B (지연 import)
 ├─ +0x4640  BasepCsrCaptureSxsMessage           문자열 캡처
 ├─ +0x4672  CsrClientCallServer(0x1001d, 0x218)
 │     ┄┄▶ (csrss 요청 스레드)  basesrv!InternalBaseSrvCreateProcess
 │            ├─ (1) +0x100  (CFG 간접) sxssrv!BaseSrvSxsCreateProcess
 │            │        ├─ +0x92  시스템 기본 문맥 ─ NtMapViewOfSection · NtWriteVirtualMemory → 자식 PEB +0x308   (늘)
 │            │        └─ +0x51  앱 문맥 (매니페스트 있을 때만)
 │            │                    └─ sxs!SxsGenerateActivationContext … NtMapViewOfSection · NtWriteVirtualMemory → +0x2f8
 │            └─ (2) +0x17c  csrsrv!CsrCreateProcess          사무실 명부에 올리기 (run34)
 ├─ +0x57d7  NtWriteVirtualMemory               → 자식 PEB +0x2d8 (pShimData)   앱 호환 데이터가 있을 때
 └─ +0x5bd3  NtResumeThread                      (run14)
```

**새 말 — sxssrv · sxs.dll · 블롭**

- **sxssrv** = csrss 안에서 SxS 일을 맡는 **서버 DLL**. 자기 창구를 따로 열지 않고, 1번 창구 (basesrv) 의 표에 자기 함수를 올려 둔다 (sxssrv `ServerDllInitialization +0xdf/+0xe6` → `BaseSrvRegisterSxS`) `[정적]`.
  - 비유: 인사팀 사무실 1번 창구 뒤에 앉은 **책자 담당**.
- **sxs.dll** = 매니페스트를 해석해 문맥 데이터를 만드는 DLL. sxssrv 가 **필요할 때 불러와** 내보내기 `SxsGenerateActivationContext` (rva `0x5a2b0`) 를 부른다 `[정적]` (2.8).
  - 비유: 책자 담당이 쓰는 **제본 기계**.
- **블롭** = 한 덩어리로 된 데이터. 여기선 활성화 문맥 데이터 한 벌이다. 자식은 자기 땅에 걸린 이 덩어리를 **읽기만** 한다 (붙일 때 읽기 전용, 2.7).
  - 비유: 설정 묶음을 **찍어 낸 책자**.

자식 PEB 칸 셋을 한 표로 (run 문서 "PE_emulator 쪽에서 쓸 것" · "확인된 것"):

| PEB 칸 | 이름 | 누가 쓰나 | 쓰는 줄 (call/ret) | 언제 | test.exe 사진 (P0) |
|---|---|---|---|---|---|
| `+0x308` | `SystemDefaultActivationContextData` | **csrss** (sxssrv) | `BaseSrvSxsDoSystemDefaultActivationContext +0x211/+0x218` | **늘** (보호 프로세스류만 빼고) | `25996d00000` |
| `+0x2f8` | `ActivationContextData` | **csrss** (sxssrv) | `BaseSrvSxsCreateActivationContextFromStructEx +0xb09/+0xb10` | exe 매니페스트가 **있을 때만** | (null) |
| `+0x2d8` | `pShimData` | **부모** (KERNELBASE) | `CreateProcessInternalW +0x57d0/+0x57d7` | `BasepGetAppCompatData` 가 값을 줬을 때 | `25996d10000` |

- 쓰는 줄 셋은 `[정적]`, 사진 값은 `[관찰]` (run44, 2.11).
- 셋 다 **`NtWriteVirtualMemory`** 로 쓴다 `[정적]`. 쓰는 쪽이 자식 밖에 있어서, 자식 칸엔 이 syscall 로만 닿는다 (짐 지도 3장 줄임 표 `①B…`).
- `pShimData` 는 SxS 와는 **다른 이야기** (앱 호환) 다. 같은 구간 · 같은 꼴이라 같이 닫았다.

비유: 새 가게 안내판에 세 줄이 적힌다. "공통 책자는 벽 몇 번지" 와 "전용 책자는 벽 몇 번지" 는 인사팀 책자 담당이 적고, "호환 안내문은 몇 번 방" 은 신청한 가게 (부모) 가 직접 적는다. 그동안 새 가게 직원은 "대기" 팻말을 걸고 서 있다.

### 2.2 한 줄씩 읽는 법

run 문서 "호출 관계" 그림에서 두 줄을 골라 끝까지 읽어 본다.

#### 줄 1: `│ │ └─ +0x218  NtWriteVirtualMemory  → 자식 PEB +0x308`

(csrss 쪽, `BaseSrvSxsDoSystemDefaultActivationContext` 아래 줄)

- **누가**: csrss 프로세스의 요청 스레드. 그 안 sxssrv 함수 `BaseSrvSxsDoSystemDefaultActivationContext` (rva `0x1440`).
- **어디서**: CALL 이 `+0x211`, 돌아올 자리가 `+0x218`. 콜스택엔 `BaseSrvSxsDoSystemDefaultActivationContext+0x218` 로 보일 자리다.
- **누구를**: `NtWriteVirtualMemory` — 다른 프로세스 메모리에 쓰는 syscall.
- **어디에 쓰나** `[정적]`:
  - `+0x1f2 MOV RDX,[RSP+0xc0]` — 메시지 `+0x240` 에서 꺼낸 **자식 PEB 주소**.
  - `+0x1fa ADD RDX,0x308` — 거기에 `0x308` 을 더한다 → `SystemDefaultActivationContextData` 칸.
- **무엇을 쓰나**: 바로 앞 `+0x1ab/+0x1b2 NtMapViewOfSection` 으로 자식 땅에 붙인 **시스템 기본 섹션의 주소** `[정적]`.
  - 크기는 보통 (AMD64) 8B. 구조 번호에 따라 달라진다 (2.7 "붙이고 · 적는다", run 문서 "보탠 것" ①).
- **무엇을 위해**: 자식이 나중에 "공통 책자가 어디 걸렸나" 를 안내판에서 찾게 하려고.

#### 줄 2: `├─ +0x445f  KERNEL32!BasepConstructSxsCreateProcessMessage   SxS 칸 0x1c8B   (보호 프로세스 갈래면 안 탐)`

(부모 쪽)

- **누가**: 부모 cmd 스레드 안 KERNELBASE `CreateProcessInternalW`.
- **어디서**: CALL `+0x4458`, 돌아올 자리 `+0x445f`.
- **누구를**: **kernel32** 의 `BasepConstructSxsCreateProcessMessage` (rva `0x26070`, 1877B). 직접 call 이 아니라 **지연 import 칸** `[0x1803c66d8]` 을 거친다 `[정적]`.
- **무엇을 위해**: 곧 csrss 로 보낼 등록 메시지 안 **SxS 칸 `0x1c8`B** 를 채우려고 (2.4 · 2.5).
- **안 타는 때** `[정적]`:
  - 앞 `+0x4361/+0x4366` "있나" 검사가 0 이면 `+0x4368 JZ` → `+0x4466` 으로 건너뛴다 (2.3).
  - `NtCreateUserProcess` 출력 Flags 비트 0 켜짐 · 비트 4 꺼짐이면 `+0x4336 JZ` → `+0x4477` 로 건너뛴다 (보호 프로세스 갈래, 2.3).

---

### 2.3 부모 ① — 등록 서류의 "설정 쪽지 칸" 은 kernel32 가 채운다

#### 함수가 kernel32 에 있다 `[정적]`

- 결과서가 찾은 것: `BasepConstructSxsCreateProcessMessage` · `BasepInitAppCompatData` · `BasepReleaseSxsCreateProcessUtilityStruct` 는 **kernel32** 에 본문이 있다.
- KERNELBASE 엔 `DelayLoad_<이름>` **스텁만** 있다 (`DelayLoad_BasepConstructSxsCreateProcessMessage` rva `0x12a3c4`, API 세트 `ext-ms-win-kernelbase-processthread-l1-1-0`, 결과서 `pefile`).
- 그래서 Ghidra 지도 (`_edges.tsv`) 엔 `external` 로만 찍혔다. 이번에 kernel32 프로젝트를 열어 본문을 뽑았다 (13개 지정 + depth 1 = 35개).

비유: 신청서 작성 담당 (KERNELBASE) 이 "설정 쪽지 칸" 만은 **옆 건물 (kernel32) 직원에게 맡긴다**. 그 직원 연락처는 "필요하면 그때 부를게요" 메모 (지연 import) 에 적혀 있다.

#### "있나" 검사 21개의 정체 — `[추측]` 이 `[정적]` 으로

**새 말 — 지연 import 의 "있나" 검사**: 지연 import 로 부를 함수가 **실제로 있는지 먼저 묻는** 작은 함수. 이름이 `Is<이름>Present` 꼴이다 `[정적: 이름]`.

- run14 때: `+0x4361/+0x4366` 의 검사 함수는 **같은 주소에 이름 21개** 라 이름을 못 골랐다. 그래서 "어떤 import 가 있는지 알려 주는 검사" `[추측]` 로만 적었다.
- 이번 (flow-writer `_index.tsv` · `pdb_aliases.py --lookup KernelBase.dll 0x12a02c`) `[정적]`:
  - 21개 이름이 모두 `Is<이름>Present` 꼴이다.
  - 그 `<이름>` 들은 **kernel32 에 본문이 있고 KERNELBASE 엔 `DelayLoad_<이름>` 스텁만 있는** 함수들이다.
- 그러니 이 검사는 "kernel32 에 그 함수가 있나" 를 묻는 것이다. 21개 중 이 자리에 맞는 이름은 여전히 못 고른다 (후보 `IsBasepConstructSxsCreateProcessMessagePresent` 등) — 한 주소에 이름 여럿이라서.
- 덤 (run 문서 "보탠 것" ④): `CreateProcessInternalW` 가 부르는 `Base*` · `Basep*` **대부분이 kernel32** 다. `BasepCheckWebBladeHashes` · `BasepIsProcessAllowed` · `BasepCheckWinSaferRestrictions` · `BaseCheckElevation` · `BaseMarkProcessTokenForInstallers` · `BasepGetAppCompatData` · `BaseElevationPostProcessing` · `BasepPostSuccessAppXExtension` 등 `[정적]`.

#### 보호 프로세스 갈래 — SxS 를 통째로 건너뛴다 `[정적]`

**새 말 — 보호 프로세스**: 다른 프로세스가 함부로 들여다보거나 건드리지 못하게 막아 둔 특별한 프로세스로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이번 코드에서 **이름은 `[추측]`** 이고, **조건** 만 `[정적]` 이다.

1. `NtCreateUserProcess` 출력 Flags 비트 0 이 켜지고 비트 4 가 꺼져 있으면 (`+0x4328 ~ +0x4336`, 이름은 보호 프로세스 · 경량 보호 `[추측]`) → 메시지 함수를 **안 부른다**.
2. 대신 `+0x4623` 에서 메시지의 ThreadHandle 칸 **비트 1** 을 켠다.
3. csrss `InternalBaseSrvCreateProcess +0xca ~ +0xcc` 가 그 비트를 보고 **SxS 호출 자체를 건너뛴다**.
4. 그래서 → 그 프로세스는 **시스템 기본 문맥도 못 받는다**.

test.exe 는 사진에 `+0x308` 이 채워져 있으니 (2.11) 이 갈래를 안 탔을 것이다 `[추측: 사진 값 + 코드 길]`. csrss 가 그 줄 (`+0xfb`) 을 지나는 걸 직접 보는 건 Q104 5.

---

### 2.4 부모 ② — 보통 길에선 부모가 쪽지를 안 연다

#### 쪽지를 찾는 건 커널이다 `[정적]`

- exe 안 매니페스트 (`RT_MANIFEST` 24, ID 1) 를 찾는 건 부모가 아니라 **커널** `PspLocateInPEManifest` 다 (`NtCreateUserProcess` 안, sast/54 · run59).
- 찾으면 만들기 정보로 **주소 · 크기** 를 돌려주고, 자식 `ProcessParameters.Flags` **비트 13** (`0x2000`) 을 켠다.
- 부모는 그 주소 · 크기를 SxS 칸 `+0x50` · `+0x58` 에 옮겨 적을 뿐이다 (2.5).

#### 두 갈래 — 빠른 길 (보통) · 느린 길 `[정적]`

kernel32 `BasepConstructSxsCreateProcessMessage` 안에서 갈린다.

**빠른 길** — 아래가 **모두** 맞으면 (또는 자식이 보호 프로세스면) `+0x65a` 로 간다:

- 레지스트리 `PreferExternalManifest` 값이 0
- 인자 7 이 꺼짐
- 이동식 매체가 아님
- 안전 부팅이 아님 — `+0x3b7 CMP byte [0x7ffe02ec]` = KUSD `SafeBootMode`

가면 → Flags 에 **`0x40`** ("매니페스트는 csrss 가 찾아라") 을 켜고, exe 경로 · 파일 핸들 · 커널이 준 주소 · 크기만 채우고 끝. **부모는 매니페스트 파일도 자원도 안 연다.**

**느린 길** — 하나라도 어긋나면:

- `+0x5ba BasepSxsCreateProcessCsrMessage` → `BasepSxsCreateStreams`.
- `PreferExternalManifest` 가 0 이면 **자원 먼저** (`LdrResSearchResource` 타입 24), 없으면 **`<exe>.Manifest` 파일** (`NtOpenFile` · `NtCreateSection`). 값이 1 이면 순서가 반대.
- `.Config` (정책) 파일도 같은 파일 함수로 연다.

비유: 등록 서류의 설정 쪽지 칸에 보통은 **"쪽지는 사무실에서 직접 찾아 주세요"** 에 체크 (`0x40`) 하고, 쪽지가 있을 만한 자리 (exe 경로 · 파일 손잡이 · 관리사무소가 알려 준 쪽지 위치) 만 적어 보낸다. 특별한 경우 (안전 부팅 등) 에만 부모가 쪽지를 직접 뜯어 동봉한다.

#### 매니페스트가 없으면? — 메시지는 그대로 간다 `[정적]`

- 이 메시지는 SxS 전용이 아니라 **프로세스 등록 알림** 이라 늘 간다 (run14: 보안 프로세스가 아니면 늘 지남).
- 부모 쪽 변화는 없다 — 빠른 길이면 Flags `0x40`, 주소 · 크기 칸 0 (결과서).
- **없는지 알아보는 쪽은 csrss** 다 (2.8).
- test.exe 가 빠른 길인지 (Flags = `0x40` 인지) 는 실행 때 값이라 → Q104 4.

---

### 2.5 등록 메시지 모양 — `0x218`B 속지 안의 `0x1c8`B (짧게)

`CreateProcessInternalW` 안 지역 `[RSP+0xbe0]` 가 메시지 **머리** (`0x40`B) 이고, 그 뒤가 **데이터** (`0x218`B) 다 `[정적]`. csrss 는 머리부터 세기 때문에 csrss 쪽 오프셋은 아래 값 + `0x40` 이다.

| 데이터 기준 | 뜻 | csrss 쪽 (`BaseSrvSxsCreateProcess`) |
|---|---|---|
| `+0x0` · `+0x8` · `+0x10` | ProcessHandle · ThreadHandle · ClientId | — |
| `+0x20` | CreationFlags | `+0x60` — **비트 31 이 켜져 있으면 SxS 전체 건너뜀** |
| `+0x38` (`0x1c8`B) | **SxS 칸** | `+0x78` |
| `+0x200` · `+0x208` | 자식 PEB 주소 (native · WOW64) | `+0x240` · `+0x248` |
| `+0x210` (word) | 프로세서 구조 번호 | `+0x250` |

SxS 칸에서 이번 이야기에 쓰는 것만 (쓰는 줄 · 읽는 줄로 확인한 것):

| SxS 기준 | 뜻 |
|---|---|
| `+0x0` Flags | `0x1` 매니페스트 스트림 · `0x2` 정책 스트림 · **`0x20` SxS 건너뜀** · **`0x40` csrss 가 찾아라** |
| `+0x4` | 자식 `ProcessParameters.Flags` 사본 — 비트 13 `0x2000` = 매니페스트 자원 찾음, `0x1000` = `.local` |
| `+0x8` | exe 파일 핸들 (`0x40` 갈래) |
| `+0x50` · `+0x58` | **커널이 준** 자식 안 매니페스트 주소 · 크기 |
| `+0xa8` | 빈 "어셈블리 이름 자리" (`-` 59개로 채움, csrss 가 답을 씀) |

비유: 등록 서류 = **봉투 겉면** (`0x40`B) + **속지** (`0x218`B). 속지 `+0x38` 부터 `0x1c8`B 가 "설정 쪽지 칸" 이고, 속지 끝 쪽엔 "새 가게 안내판 주소" (자식 PEB) 가 적혀 있다. 그 주소가 있어야 사무실이 안내판에 적으러 갈 수 있다.

- 문자열은 `+0x463b/+0x4640 BasepCsrCaptureSxsMessage` (KERNELBASE rva `0xfadc0`) 가 캡처 버퍼 (답장 봉투) 로 옮긴다. 음수면 `+0x4649 JS` 로 실패 길 `[정적]`.
- 그다음 `+0x466b/+0x4672 CsrClientCallServer` — `R8D = 0x1001d` (`+0x4655`), `R9D = 0x218` (`+0x464f`) `[정적]`.

---

### 2.6 csrss ① — 1번 창구가 책자 담당을 부른다

#### 어떻게 sxssrv 로 가나 `[정적]`

```
csrss 요청 스레드  basesrv!BaseSrvCreateProcess2
 └─ +0x21b  InternalBaseSrvCreateProcess
      ├─ (1) +0x100  (CFG 간접, call +0xfb) sxsFunctions 표 +0x8 = sxssrv!BaseSrvSxsCreateProcess
      │              음수면 +0x102 JS → +0x1bc (아래 (2) 도 건너뛰고 끝)
      └─ (2) +0x17c  csrsrv!CsrCreateProcess     (run34)
```

- `+0xf7 MOV RAX,[RAX+0x8]` 이 표 칸을 읽고, **call 은 `+0xfb`** 다 (결과서 지도는 "`+0xf7/+0xfb`" 라고 적었다 — run 문서 "보탠 것" ⑥).
- 그 표 칸이 `BaseSrvSxsCreateProcess` (sxssrv rva `0x1cd0`) 인 건 이렇게 맞췄다:
  1. sxssrv `ServerDllInitialization +0xdf/+0xe6` 이 자기 `functions` 표를 `BaseSrvRegisterSxS` 로 넘긴다.
  2. 결과서가 `pefile` 로 그 표 (rva `0x90f0`) 를 읽었더니 `+0x8` 칸이 `BaseSrvSxsCreateProcess` 였다.
- CFG 간접 호출이라 **교환원** 을 거쳐 부른다.

비유: 1번 창구 직원이 등록 서류를 받으면, 먼저 뒤에 앉은 **책자 담당** 에게 서류를 넘긴다. 책자 담당의 이름은 창구 책상 위 **내선표 둘째 칸** (`+0x8`) 에 적혀 있다 — 담당이 출근할 때 직접 적어 둔 것이다.

#### 부르는 조건 넷 `[정적]` (`InternalBaseSrvCreateProcess +0xca ~ +0xee`)

1. 첫 인자 0 — 부모가 ThreadHandle 칸 비트 1 을 **안 켰다** (보호 프로세스 갈래 아님, 2.3).
2. 부모 CSR_PROCESS `[+0x5c]` 비트 `0x2000` 꺼짐 — 뜻은 `[공백]` (Q164 ③).
3. `sxsFunctions` 표가 있다.
4. 메시지가 있다.

#### 순서 — SxS 가 먼저, 명부 등록이 나중 `[정적]`

- SxS (`+0xfb`) 가 `CsrCreateProcess` (`+0x175`) 보다 **먼저** 다.
- **SxS 가 실패하면 명부 등록도 없다** (run 문서 "보탠 것" ②): `+0x100 TEST EAX,EAX` · `+0x102 JS +0x1bc` 는 `CsrCreateProcess` (`+0x175`) · `UserNotifyProcessCreate` (`+0x1b7`) 를 건너뛰고 함수 끝으로 간다.
- 부모 쪽이 그 실패를 어떻게 받는지 (프로세스 만들기 실패로 끝나나) 는 안 봤다 `[추측]` → Q164 ③. PE_emulator 엔 csrss 가 없어 상관없다.

---

### 2.7 ★ csrss ② — 공통 책자 (시스템 기본 문맥) 는 늘, 한 권을 돌려 쓴다

**새 말 — 시스템 기본 문맥 / 앱 문맥**

- **시스템 기본 문맥** = 매니페스트가 없어도 모든 프로세스가 받는 **기본 설정 묶음**. 자리는 PEB `+0x308` `SystemDefaultActivationContextData` `[정적: PDB _PEB]`.
- **앱 문맥** = 그 exe 의 매니페스트로 만든 **가게 전용 설정 묶음**. 자리는 PEB `+0x2f8` `ActivationContextData`.
- 비유: 공통 책자 · 가게 전용 책자.

#### 먼저, 그리고 늘 `[정적]`

`sxssrv!BaseSrvSxsCreateProcess` 안:

1. `+0x1a CMP [R8+0x60],0` · `+0x2f JGE` — 메시지 CreationFlags (`+0x60`) 의 **비트 31 이 꺼져 있으면** 시스템 기본 쪽으로 간다.
   - 비트 31 을 켜는 줄은 `CreateProcessInternalW` 에 없다 → 보통 꺼짐 `[추측]`. 켜는 쪽은 Q164 ③.
2. `+0x8d/+0x92` → **`BaseSrvSxsDoSystemDefaultActivationContext`** (시스템 기본).
3. 음수면 `+0x94 JS` → 앱 문맥은 안 부르고 그 값을 돌려준다.
4. 성공하면 `+0x96 JMP +0x38` → `+0x4c/+0x51` **`InternalSxsCreateProcess`** (앱 문맥, 2.8).

그래서 → **매니페스트가 없어도 시스템 기본 문맥은 늘 들어간다.** 빠지는 건 csrss 가 SxS 를 아예 안 부를 때 (2.6 조건 넷 — 보호 프로세스류 등) 와 비트 31 이 켜졌을 때다 `[정적]`.

#### 한 권을 만들어 두고 돌려 쓴다 `[정적]`

- csrss 안 전역 표 **`SxsSystemDefaultActivationContexts`** — **5칸**, 프로세서 구조마다 한 칸.
- 칸이 비었을 때만 (`+0x13e CMP [R15],0` · `+0x142 JZ`) `+0x5ef/+0x5f4 BaseSrvSxsCreateActivationContextFromStructEx(-1, -1, …)` 로 **처음 한 번** 만든다.
- 그 뒤엔 새 프로세스마다 **같은 섹션을 붙이기만** 한다.
- 구조 번호는 메시지 `+0x250` (word) 에서 온다: 0 → `0xa`, 5 → `0xd`, 나머지는 그대로 (`+0x6d ~ +0x9f`). 값의 뜻 (x86 · ARM32 on x64 등) 은 `[추측: PROCESSOR_ARCHITECTURE 값]`.

비유: 인사팀 사무실 서가에 **공통 책자 칸이 다섯** (구조마다 한 권) 있다. 그 구조의 첫 손님이 왔을 때 한 번 찍어 꽂아 두고, 그 뒤로는 새 가게마다 **같은 책자** 를 걸어 준다.

#### 붙이고 · 적는다 `[정적]`

| 차례 | 줄 (call/ret) | syscall | 자식에 생기는 것 |
|---|---|---|---|
| 3 | `+0x1ab/+0x1b2` | `NtMapViewOfSection` — 섹션 `[R15]` → 자식 (`R13`). 보호 `R14D = 2` (`+0x172`), `0x400000` (`+0x177`), 공유 1 (`+0x17f`) | VAD 하나: 시스템 기본 블롭 (읽기 전용) |
| 4 | `+0x211/+0x218` | `NtWriteVirtualMemory` | **PEB `+0x308`** ← 붙인 주소 (2.2 줄 1) |
| 5 | `+0x279/+0x280` | `NtWriteVirtualMemory` (4B) | (WOW64 자식일 때) PEB32 `+0x200` |

- 보호 `2` = 읽기 전용이라는 뜻은 결과서에선 `[추측]` 이었다. run44 사진 VAD 가 **READONLY** 로 맞춰 준다 (2.11).
- 쓰기 크기 (run 문서 "보탠 것" ①, flow-writer `.asm`): `+0x1e0 CMP EBX,0xa` · `+0x29b CMP EBX,0xd` 가 맞으면 `+0x1e9 MOV R9,RSI` (섹션이 이미 있던 길에선 `+0x148` 의 4), 아니면 `+0x2a4` 에서 8. 결과서 표의 "8바이트" 는 보통 (AMD64) 길이다.

비유: 공통 책자를 새 가게 벽에 **유리 액자로** 걸고 (읽기 전용 붙이기), 안내판 `+0x308` 줄에 "공통 책자는 벽 몇 번지" 를 적는다. run57 의 별명 사전 (ApiSet) 을 걸고 안내판에 적던 것과 같은 꼴이다. 다만 이번엔 거는 손이 관리사무소가 아니라 **인사팀 사무실** 이다.

#### 책자 재료 — 무엇으로 만드나 (이름까지만)

- 코드가 직접 짓는 **합성 요청** 하나로 만든다 `[정적]`:
  - 어셈블리 이름 `Microsoft.Windows.SystemCompatible,version="6.0.0.0",…`
  - 이름표 `"System Default Context"`
  - 루트 `<Windows>\WinSxS`
- 그 요청으로 **실제 무엇이 들어가나** (어떤 조립이 들었나) 는 결과서에서 `[공백]` 이었다 → **run88 에서 다룸**.

---

### 2.8 csrss ③ — 가게 전용 책자 (앱 문맥) 는 쪽지가 있을 때만

#### 문 하나 — 들어갈지 말지 `[정적]`

`InternalSxsCreateProcess` 맨 위: **`(Flags & 0x20) == 0 && (Flags & 0x41) != 0`** 일 때만 앱 문맥을 만든다.

- `0x20` (SxS 건너뜀) 은 부모 kernel32 가 켠다 (`+0x37f`). 켜는 조건은 인자 11 (`[RSP+0x89e]`) 의 비트 9 다 `[정적]`.
  - 그 칸이 무엇인가 (run 문서 "보탠 것" ⑤): 결과서 짐작대로 `[RSP+0x870]` 이 `SECTION_IMAGE_INFORMATION` 시작이면, `[RSP+0x89e]` = `+0x2e` = ntoskrnl PDB `_SECTION_IMAGE_INFORMATION.DllCharacteristics` 다. 비트 9 (`0x200`) 이름 `IMAGE_DLLCHARACTERISTICS_NO_ISOLATION` 은 공개 상수 이름 `[추측]`.
  - test.exe `DllCharacteristics = 0x8160` 은 비트 9 가 꺼져 있어 이 갈래를 **안 탄다** (짐 지도 3.23, 사진 K1).
- `0x40` (csrss 가 찾아라) 또는 `0x1` (매니페스트 스트림 있음) 중 하나가 있어야 한다.

#### 쪽지 찾기 → 제본 → 걸기 → 적기 `[정적]`

run 문서 "누가 무엇을 자식에 넣나" 표 6 ~ 12 를 이야기로 풀면:

| 차례 | 줄 | 하는 일 | 자식에 생기는 것 |
|---|---|---|---|
| 6 | `InternalSxsCreateProcess +0x2d6/+0x2dd` `NtDuplicateObject` | 부모의 exe 파일 핸들을 csrss 쪽으로 복제 (`0x40` 갈래) | 없음 |
| 7 | `+0x330/+0x337` `NtReadVirtualMemory` | 자식 `PEB+0x10` (`ImageBaseAddress`) 읽기 | 없음 (읽기만) |
| 8 | `+0x3f6/+0x3fb BaseSrvSxsProbeManifestAndPolicyAndDotLocal` | 쪽지 찾기: 앱 호환 캐시 (`NtApphelpCacheControl`) → 자식 메모리의 자원 → `.Manifest` 파일 → `.local`. **부모 토큰을 흉내 내고** 파일에 접근 | 없음 (읽기만) |
| — | `+0x405 TEST [R15],0x2000` · `+0x40c JZ +0x8b2` | 쪽지를 **못 찾으면 여기서 끝** | — |
| 9 | `+0x66d/+0x672 FromStructEx` → `+0x8d6/+0x8db` (간접) = **sxs `SxsGenerateActivationContext`** | 제본: sxs.dll 이 블롭을 만든다. 같은 키면 캐시 재사용 (`GetCacheKey +0x414` · `LookupEntry +0x468` · `InsertEntry +0xa37`) | 없음 (csrss 안에 블롭 섹션) |
| 10 | `FromStructEx +0xadb/+0xae2` `NtMapViewOfSection` | 블롭 섹션 → 자식, 보호 2 · `0x400000` · 공유 1 | VAD: **앱 블롭** |
| 11 | `FromStructEx +0xb09/+0xb10` `NtWriteVirtualMemory` (8B) | 대상 칸 = `InternalSxsCreateProcess +0x60c LEA RAX,[R13+0x2f8]` | **PEB `+0x2f8`** ← 붙인 주소 |
| 12 | `FromStructEx +0xb54/+0xb5b` `NtWriteVirtualMemory` (4B) | WOW64 일 때 | PEB32 `+0x1f8` |

- sxs.dll 은 `FromStructEx +0x830 BaseSrvSxsGetActivationContextGenerationFunction` 이 올리고 내보내기 주소를 얻는다. 그 주소를 `+0x8d6` 에서 간접으로 부른다 `[정적]` (결과서 — 위 함수가 얻은 주소).
- 제본 기계 안: `SxsGenerateActivationContext` → `SxspInitActCtxGenCtx` · `SxspAddRootManifestToActCtxGenCtx` · **`SxspBuildActCtxData`** (`+0x16c CreateFileMappingW` · `+0x1a4 MapViewOfFile`) — 블롭을 담을 **익명 섹션** 을 **csrss 안에** 만든다 `[정적]`.

그래서 → **매니페스트 해석 (XML · 정책 · WinSxS 폴더 이름) 은 csrss 안 sxs.dll 의 일이다. 자식 프로세스 안이 아니다** `[정적]`. 부모 kernel32 는 느린 길에서 쪽지 스트림을 **열기만** 한다 (2.4).

비유: 책자 담당이 서류의 "쪽지 위치" 를 보고 가게에 가서 쪽지를 찾는다 (신청한 가게의 출입증을 빌려서). 쪽지가 없으면 그냥 돌아온다. 있으면 제본 기계로 전용 책자를 찍고 (같은 쪽지를 전에 찍은 적 있으면 그 책자를 다시 꺼내 쓰고), 새 가게 벽에 걸고 안내판 `+0x2f8` 줄에 적는다.

#### test.exe 는? `[관찰]`

- run44 사진 8장 모두 `ActivationContextData` = (null) (`_P0.txt:1638`, `_P2.txt:2515`).
- 위 표와 맞는다 — test.exe 는 매니페스트가 없어 11 을 안 탄다.
- 정말 `+0x40c JZ` 로 빠졌는지 (Probe 결과) 는 실행해 봐야 안다 → Q104 7.

---

### 2.9 ★ 부모 ③ — `pShimData` 는 부모가 직접 쓴다 (Q87 닫힘)

#### 다섯 걸음 `[정적]` (`CreateProcessInternalW`, 라벨은 call/ret)

| 걸음 | 자리 | 하는 일 |
|---|---|---|
| 1 | `+0x56bb/+0x56c2` | **kernel32** `BasepGetAppCompatData` (지연 import). 결과 `[RSP+0x2a0]` 가 0 이면 `+0x56d2 JZ` → 아래 셋 안 탐 |
| 2 | `+0x574e/+0x5755` | `NtAllocateVirtualMemory` — 자식 땅에 영역 (`+0x572b` `0x1000` MEM_COMMIT, 크기 `[RSP+0x1f8]`) |
| 3 | `+0x578e/+0x5795` | `NtWriteVirtualMemory` — 그 영역에 앱 호환 데이터 복사 |
| 4 | **`+0x57d0/+0x57d7`** | `NtWriteVirtualMemory` — **자식 PEB `+0x2d8` (`pShimData`) 8B**. `+0x57a9 MOV RDX,[RSP+0x1a0]` (자식 PEB) · `+0x57b1 ADD RDX,0x2d8`, 값 `[RSP+0x3a8]` = 걸음 2 영역 주소 |
| 5 | `+0x582a/+0x5831` | `NtWriteVirtualMemory` — WOW64 PEB `+0x1e8` 4B (`+0x57f4` `[RSP+0x590]` 0 이면 안 탐) |

- run14 의 `[추측]` "앱 호환 데이터 쓰기 중 하나가 자식 PEB `+0x2d8` 에 주소를 쓴다" → **맞았다. 둘째 쓰기 `+0x57d0/+0x57d7` 이다** `[정적]` (오케스트레이터 · flow-writer `.asm`).

비유: 신청한 가게 (부모) 가 새 가게 땅에 **작은 방** 하나를 얻어 (걸음 2) "호환 안내문" 을 넣고 (걸음 3), 안내판에 "안내문은 저 방" 을 적는다 (걸음 4). run14 해설에서 짐작한 그 장면이 코드로 확인됐다.

#### 그럼 `BasepInitAppCompatData` 는? — PEB 를 안 쓴다 `[정적]`

- **kernel32** 함수다 (rva `0x78e40`, 123B). `CreateProcessInternalW +0x5976/+0x597d` 가 지연 import 로 부른다.
- 하는 일은 딱 이것: `0x188`B 구조체를 0 으로 채우고 (`+0x2f memset`) 인자 셋을 넣어 (`+0x45 ~ +0x55`) **`g_CompatCacheServiceApi(0xa, 구조체)` 한 번** (`+0x5c`, CFG 간접). 이 함수 안 호출은 이 둘뿐이다 (flow-writer 가 전부 읽음).
- **PEB 를 안 쓴다.**

#### 그래서 Q87 이 닫혔다 `[정적]`

- 질문: "부모가 자식 PEB `pShimData` · `SystemDefaultActivationContextData` 를 쓰나?"
- 답:
  - `pShimData` (`+0x2d8`) — **부모가 쓴다** (`+0x57d0`).
  - `SystemDefaultActivationContextData` (`+0x308`) · `ActivationContextData` (`+0x2f8`) — **부모는 안 쓴다. csrss 만 쓴다.** `+0x2f8` · `+0x308` 을 쓰는 줄은 부모 쪽에 없다.
- 그래서 Q83 (동적) 의 "누가 쓰나" 는 답이 나왔고, **"실제 순간 · 값 확인"** 만 남았다.

#### 정정 — run45 일 37 (run 문서 "정정")

- run45 1장 일 37 은 사슬 끝 `+0x597d BasepInitAppCompatData` 를 "자식 메모리에 써 넣기" 와 같이 넣었다 → **틀림**. kernel32 함수이고 자식 PEB 를 안 쓴다.
- 같은 줄의 짐작 둘 — "`pShimData` 후보 `[추측]`", "활성화 문맥 데이터는 csrss 의 SxS 가 쓸 수도" — 은 **맞았다** (`+0x57d0` · sxssrv).
- run45 문서 맨 위에 정정 한 줄을 넣었다.

---

### 2.10 누가 먼저 쓰나 — 근거 → 그래서 → 아직 확인 아님

csrss 의 일 (표 3 ~ 13) 과 부모의 `pShimData` (표 14) 중 무엇이 먼저인가.

1. **근거** `[정적]`: 부모 코드 안에서 csrss 부르기 (`+0x466b`) 가 `pShimData` 쓰기 (`+0x57d0`) 보다 **앞** 이다.
2. **근거** `[추측: 일반 지식, flow.md 2.2 Q66]`: 부모는 `CsrClientCallServer` 에서 csrss 의 **답을 기다린다**.
3. **그래서** `[추측]`: csrss 가 `+0x308` (와 `+0x2f8`) 을 다 쓴 **뒤에** 부모가 `+0x2d8` 을 쓴다.
4. **아직 확인 아님**: 2 가 일반 지식이라 이 차례도 `[추측]` 이다 → **Q83** (동적).

결과서의 다른 근거 하나는 **약하다** (run 문서 "보탠 것" ⑦):

- 결과서는 "사진에서 시스템 기본 매핑 (`25996d00000`) 이 앞, `pShimData` 영역 (`25996d10000`) 이 뒤" 를 차례 근거로 들었다.
- 그런데 그건 **주소 크기 순서** 다. 땅 번지가 작다고 먼저 지은 건 아니다. 그래서 쓴 순서의 근거로는 약하다 `[추측]`.

---

### 2.11 사진과 맞춰 보기 — 코드가 말한 모양 그대로다 `[관찰]`

flow-writer 가 run44 덤프 읽기 로그를 다시 읽었다 (run 문서 "보탠 것" ③).

| | K3 | P0 |
|---|---|---|
| PEB `pShimData` (`+0x2d8`) | (null) (`04_baggage-read_K3.txt:1633`) | `25996d10000` (`04_baggage-read_P0.txt:1635`) |
| PEB `SystemDefaultActivationContextData` (`+0x308`) | (null) (`_K3.txt:1638`) | `25996d00000` (`_P0.txt:1640`) |
| PEB `ActivationContextData` (`+0x2f8`) | (null) | (null) — 8장 모두 (`_P0.txt:1638`, `_P2.txt:2515`) |

P0 VAD 두 줄:

- `25996d00 – 25996d03` **Mapped READONLY "Pagefile section, shared commit 0x4"** (`_P0.txt:1380`) = PEB `+0x308` 값. 4쪽 (`0x4000`B).
  - 코드와 맞는 점: 보호 2 (읽기 전용) · 익명 (pagefile) 섹션 (`CreateFileMappingW`). 결과서가 `[추측]` 으로 둔 "보호 2 = 읽기 전용" 을 사진이 맞춰 준다.
- `25996d10 – 25996d11` **Private READWRITE** 2쪽 (`_P0.txt:1381`) = PEB `pShimData` 값.
  - 코드와 맞는 점: 부모 `NtAllocateVirtualMemory` (`+0x572b` MEM_COMMIT) 로 받은 개인 영역.

그래서 → 결과서 "동적 2" (`!address` / `!vad` 로 크기 · 보호 · 유형) 의 **test.exe 몫은 사진으로 이미 `[관찰]`** 이다. 남은 건 `NtMapViewOfSection` 인자를 그 줄에서 직접 보는 것 (Q104 2).

주의: 사진은 **값** 이 맞는다는 것이지, 그 줄을 **지나가는 순간** 을 본 건 아니다 (flow.md 4.1).

---

### 2.12 ★ PE_emulator 쪽 — 걸어 주고 번지만 적으면 된다

run 문서 맨 위 "PE_emulator 쪽에서 쓸 것".

#### 자식에 실제로 닿는 것 — 포인터 셋 + 메모리 덩어리 셋 `[정적]`

- `PEB+0x308` ← 시스템 기본 블롭 주소 — **매니페스트 없는 exe 에도 늘**.
- `PEB+0x2f8` ← 앱 블롭 주소 — 매니페스트 있을 때만.
- `PEB+0x2d8` ← 앱 호환 데이터 영역 주소 — 부모가, 값이 있을 때만 (SxS 와 다른 이야기).
- 셋 다 같은 구간 (`K3~P0`) 에, 자식 첫 스레드가 멈춰 있을 때 들어간다.

#### 엔진이 할 일 `[정적 판단 — 결과서 질문 5]`

- 2.7 · 2.8 표의 **3 ~ 5 · 10 ~ 12** (붙이기 + PEB 쓰기) 를 nt 엔진이 대신하면 된다.
- **6 ~ 9** (쪽지 찾기 · 해석 · 캐시) 는 csrss 안에서만 오간다. 블롭을 미리 떠 오면 필요 없다.
- 붙이는 모양: **읽기 전용 공유 보기** (`NtMapViewOfSection` 공유 1 · `0x400000` · 보호 2). test.exe 사진은 4쪽, 주소 `25996d00000` (64KB 정렬) `[관찰]`.
- **부모 쪽 흉내는 필요 없다.** 부모 · csrss 가 없으니 메시지를 만들 일이 없다.
- 한계 (결과서): 앱 블롭은 **exe 마다**, 시스템 기본은 **구조마다** 다르고, **같은 빌드 · 같은 WinSxS** 에서만 맞는다.

#### 지금 PE_emulator 는 — 빠져 있다 (틀림)

- test.exe 같은 매니페스트 없는 exe 도 진짜 Windows 에선 `PEB+0x308` 이 채워져 있다 (`[관찰]` run44 P0 `25996d00000`).
- 그러니 GUI 앱만의 문제가 아니다. **매니페스트 없는 exe 도 안내판에 한 줄이 빠진** 상태다.

#### 확인거리 — PE_emulator 쪽 (우리 대기열 아님)

PE_emulator 의 `NtCreateUserProcess` 흉내가:

- 만들기 정보 (`PS_CREATE_INFO`) 출력에 exe 안 매니페스트 **주소 · 크기** 를 주나,
- `ProcessParameters.Flags` **비트 13** (`0x2000`, 커널 `PspLocateInPEManifest +0x99` 가 켬 — 짐 지도 3.2) 을 켜나.

진짜에선 이 둘이 부모 → csrss 로 넘어가 "앱 문맥을 만들까" 를 정한다 (`InternalSxsCreateProcess +0x405 TEST [R15],0x2000`). 엔진이 앱 블롭을 넣을지 고를 때 같은 기준을 쓰면 된다 `[추측: 엔진 쪽]`.

#### run88 로 넘긴 것

블롭 **안쪽 모양** (머리 · 목차 · 구역), **어느 주소에 붙여도 읽히는지**, 로더가 세 문맥을 **어떤 차례로 보는지** → **run88 에서 다룸**.

비유: 진짜 Windows 에선 인사팀이 책자를 찍어 걸어 주지만, PE_emulator 동네엔 인사팀이 없다. 그러니 관리사무소 (nt 엔진) 가 **미리 찍어 둔 책자** 를 들고 와서 벽에 걸고 안내판에 번지만 적어 주면 된다. 제본 기계까지 들여올 필요는 없다.

---

### 2.13 WinSxS 경로 — 이번엔 이름만 (질문 4 일부)

- csrss (sxs.dll) 쪽에 **어셈블리 이름 → 폴더 이름** 을 만드는 함수 다섯이 있다 `[정적]`:
  - `SxspGenerateKeyform` · `SxspGenerateTextualIdentity` · `SxspGenerateSxsPath` · `SxspGenerateManifestPathForProbing` · `SxspGenerateManifestPathOnAssemblyIdentity`
- sxs.dll 문자열에 `\WinSxS\` · `manifests` · `.manifest` 가 있다 `[정적]`.
- 그래서 → 의존 어셈블리를 찾을 때 csrss 가 `WinSxS\manifests\<키폼>.manifest` 로 간다 (run 문서 질문 4).
- 남은 것 셋 → **run88 에서 다룸**:
  - 블롭 안에 폴더가 **전체 경로냐 키만이냐**
  - 폴더를 **실제로 여는 쪽** (결과서 짐작은 `[추측: 로더 = ntdll]`)
  - "6.0.0.0" 요청이 **어느 6.0.26100.x 로 묶이나**

---

### 2.14 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

| # | 무엇 | 한 줄 |
|---|---|---|
| ① | `+0x211` 쓰기 크기 | 구조 번호로 갈린다 (`0xa` · `0xd` 면 `R9 = RSI` — 섹션이 이미 있던 길에선 4, 아니면 8). 결과서의 "8바이트" 는 보통 (AMD64) 길 (2.7) |
| ② | SxS 실패 | `CsrCreateProcess` · `UserNotifyProcessCreate` 도 건너뛴다. 부모가 받는 처리는 `[추측]` (2.6) |
| ③ | 사진 VAD | Mapped READONLY Pagefile 4쪽 = `+0x308`, Private READWRITE 2쪽 = `pShimData`. "동적 2" 의 test.exe 몫은 이미 `[관찰]` (2.11) |
| ④ | 이름 위치 | `CreateProcessInternalW` 가 부르는 `Base*` · `Basep*` 대부분이 kernel32. "있나" 검사 21개 = 그 지연 import 들의 `Is<이름>Present` (2.3) |
| ⑤ | 결과서 `[공백]` "`+0x2e` 칸 이름" | ntoskrnl PDB 로 `_SECTION_IMAGE_INFORMATION.DllCharacteristics`. test.exe `0x8160` 은 비트 9 꺼짐 → Flags `0x20` 갈래 안 탐 (2.8) |
| ⑥ | `InternalBaseSrvCreateProcess` SxS 호출 자리 | call 은 `+0xfb` (`+0xf7` 은 표 칸 읽기) (2.6) |
| ⑦ | 결과서 순서 메모 | 사진 주소 순서는 쓴 순서의 근거로 약함 `[추측]` (2.10) |

- 정정 둘: run45 일 37 (2.9), 이름 위치 kernel32 (조심할 곳 4).
- 디버거가 붙어서 달라지는 점: 이번 줄기에서 디버거 여부를 보는 줄은 부모 `CreateProcessInternalW` 의 디버그 플래그 갈래 (`+0x81a DbgUiConnectToDbg`, run14) 뿐이고, SxS · 앱 호환 길과는 무관하다 `[정적: 읽은 줄 안에서]`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 — 새 PDB 둘 | — | **sxssrv** `8FAA75EF96847C62E731DC9A759FE2C21` · **sxs** `76674E9722F9EFB085A6BE05B23E9B371` |
| 기준 — kernelbase · kernel32 · basesrv PDB 줄, 근거 메모 | … | + run87 몫, run87 줄 (정적 바퀴, run44 로그로 PEB 세 칸 · VAD 두 줄 값 대조) |
| **그림 1** — KERNELBASE 대표 지점 | (1) `+0x30ee` · (2) `+0x4672` · (3) `+0x5bd3` | (1) `+0x30ee` · **(2) `+0x445f` kernel32 SxS 메시지** · (3) `+0x4672` · **(4) `+0x57d7` `pShimData`** · (5) `+0x5bd3`. 새 상자 둘은 `(정적)` 점선 |
| **그림 1** — 노란 메모 (csrss) | basesrv 가 `0x1001d` 를 받아 csrss 목록에 등록 | (1) **sxssrv `BaseSrvSxsCreateProcess`** — 시스템 기본 섹션을 붙이고 PEB `+0x308` (늘), 매니페스트 있으면 `+0x2f8` → (2) 등록 |
| 2.2 전체 순서 트리 | `(2)` csrss 알림 · `(3)` `NtResumeThread` | `(2)` SxS 메시지 · `(3)` csrss 알림 (→ sxssrv 한 줄) · `(4)` `pShimData` · `(5)` `NtResumeThread`. "풀어 주기" 절의 부르는 곳 번호도 (3) → (5) |
| 2.2 `CreateProcessInternalW` 절 | `+0x2d8` 쓰기는 `[추측]` | **`[정적]` `+0x57d0/+0x57d7`** (줄 그음 + 근거). 새 글머리: kernel32 지연 import · 메시지 모양 · 빠른 길 · 21개 이름 · `BasepInitAppCompatData` PEB 안 씀 (Q87 닫힘) |
| 2.2 run44 사진 줄 | 쓴 쪽 모름 `[추측: 부모 KERNELBASE · csrss 쪽]` | + "(run87 `[정적]`) **짐작이 맞았다**" |
| 2.2 csrss 절 | `(1) +0x100 sxsFunctions 표 +0x8 — SxS, 조건부` | 트리에 **sxssrv · sxs 사슬** (2.6 ~ 2.8), 새 글머리 (부르는 조건 · 시스템 기본 먼저 · 앱 문맥 조건 · 붙이는 모양 · SxS 실패 시 등록 없음 · 해석은 csrss 안 sxs.dll) |
| 4장 | run44 2.2 짐 지도 줄 · run45 일 목록 줄 | 두 줄에 **해결 표시** (PEB 쪽 → run87). run87 새 줄 셋: 동적 (Q104 · Q83 · Q162) · 정적 (Q164) · PE_emulator · 보탠 것 · 정정 |
| 4.1 | 그림 1 KERNELBASE (1)(2)(3), 노란 메모 | 번호 (1) ~ (5), 새 상자 둘의 근거 · 멈춤점 (`+0x4458` · `+0x57d0`). 노란 메모 줄에 sxssrv 사슬 |
| 5장 | … run86 | + run87 (run 표 · 요청서 번호 표) |

태그 · 판단이 바뀐 곳:

- flow.md 2.2 "앱 호환 데이터 쓰기 중 하나가 자식 PEB `+0x2d8` 에 주소를 쓴다" `[추측]` (run14) → **`[정적]`** `+0x57d0/+0x57d7`. 틀린 게 아니라 올린 것이다 (run14 문서는 안 고침).
- "있나 검사 (같은 주소 이름 21개)" — "어떤 import 가 있는지" `[추측]` (run14) → **kernel32 지연 import 들의 `Is<이름>Present`** `[정적]`. 이 자리의 정확한 이름은 여전히 못 고름.
- run44 사진 `K3~P0` PEB 두 칸 "쓴 쪽 모름" → **부모 `+0x57d0` · csrss sxssrv `+0x211`** `[정적]`. 순간은 동적 Q83.
- run45 일 37 "`BasepInitAppCompatData` = 자식 메모리에 써 넣기" → **틀림** (PEB 안 씀).

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- **새 짐 칸 셋** (마흔셋째판). 3.1 PEB 에 세 줄:
  - `+0x2d8` `pShimData` — 부모 KERNELBASE `CreateProcessInternalW +0x57d0`.
  - `+0x2f8` `ActivationContextData` — csrss sxssrv `FromStructEx +0xb09` (조건부 — test.exe 안 탐).
  - `+0x308` `SystemDefaultActivationContextData` — csrss sxssrv `BaseSrvSxsDoSystemDefaultActivationContext +0x211`.
- 세는 법: 쓰는 줄이 `NtWriteVirtualMemory` 라 **쓴 주인은 nt**, 부르는 쪽 (부모 · csrss) 은 괄호에 적는다 (run51 규칙). 놓는 법은 "syscall (다른 프로세스가 부름)".
  - 그래서 3장 줄임 표에 새 기호 둘: `①B…` (부모 `CreateProcessInternalW` 안 줄) · `①X…` (csrss `BaseSrvSxsCreateProcess` 안 줄).
- 숫자:
  - PEB: ① nt **38 → 41**, 모름 **68 → 65** (아는 칸 56 → 59).
  - 7.2 PEB: `K3~P0` **2 → 0**, `끝까지 0` **64 → 63** (`ActivationContextData` 가 빠짐).
  - 구조체 21개: 아는 칸 **730 → 733** (정확 676 → 679), 모름 **309 → 306**.
- 4장 짝 2줄: 문맥 뷰 ↔ csrss 가 가진 섹션 (시스템 기본은 **모든 프로세스가 같은 섹션**) · `pShimData` 영역 ↔ VAD (Private).
- 7.6 이름 정정 (`BasepInitAppCompatData` 는 kernel32), 7.8 "부모 쪽이 ⓪ 전에 자식에 쓰는 칸" PEB 쪽 해결.
- `+0x300` `ProcessAssemblyStorageMap` · `+0x310` `SystemAssemblyStorageMap` 은 csrss 가 안 쓴다 (sxssrv 에 이 오프셋 참조 없음 — 결과서). 쓰는 쪽은 **run88 에서 다룸** → 7.2 에 남김.

### 큰 그림에서

- **부모 쪽 마무리 구간 (K3 ~ P0) 의 "누가" 가 풀렸다.** 그림 1 의 KERNELBASE 대표 지점이 이제 다섯으로 이어진다: (1) 커널이 쪽지 찾아 둠 → (2) 부모가 쪽지 칸 채움 → (3) csrss 가 책자 걸고 안내판에 적음 → (4) 부모가 호환 안내문 적음 → (5) 직원 풀어 줌.
- **안내판에 test.exe 밖의 손이 적는 줄 셋이 짐 지도에 올랐다.** 부모와 인사팀이 `NtWriteVirtualMemory` 로 적는다. 짐 지도는 이걸 "쓴 주인 nt, 부르는 쪽 괄호" (run51 규칙) 로 센다.
- **PE_emulator 엔 할 일이 하나 분명해졌다** — 매니페스트 없는 exe 에도 `PEB+0x308` 을 채우는 것 (2.12).
- **다음 (run88)**: 같은 책자를 **읽는 쪽**. 첫 스레드가 깨어나 ntdll 이 DLL 을 들일 때 (run23 의 SxS 리다이렉트 자리) 이 책자들을 어떻게 펴 보는지, 책자 안은 어떻게 생겼는지 → **run88 에서 다룸**.

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 남은 것은 네 무리다.

- **실행해 봐야 아는 것** → 결과서 "동적으로 확인할 것" 번호 그대로.
  - test.exe 로 볼 수 있는 것 (1 ~ 5 · 7) → **Q104**, 그중 3 은 **Q83**.
  - test.exe 가 아닌 프로세스가 필요한 것 (6) → **Q162** (notepad 류 · GUI 샘플 행).
  - 추천 멈춤점 (결과서): csrss 안 `sxssrv!BaseSrvSxsCreateProcess` (rva `0x1cd0`) 입구 — `r8` = 메시지. csrss 문맥이 필요하다.
- **정적** → **Q164** ① ~ ③.
- **블롭 안쪽 · 로더 쪽** → **run88 에서 다룸**.
- **PE_emulator 쪽 확인거리** (우리 대기열 아님) → 2.12.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 1 블롭 덤프 — `PEB+0x308` 이 가리키는 곳을 전체 크기만큼 | 실행 때 메모리 내용. 사진 덤프 오프라인 읽기는 run88 에서 다룸 | **Q104 1** (라이브 대조) |
| 2 `NtMapViewOfSection` 인자 (`0x400000`, 공유 1, 보호 2) 를 그 줄에서 직접 | 크기 · 보호 · 유형은 사진으로 이미 `[관찰]` (2.11). 인자 자체는 실행 때 값 | **Q104 2** |
| 3 PEB 쓰기 **순간** 과 **차례** — csrss `BaseSrvSxsDoSystemDefaultActivationContext+0x211` (rva `0x1651`) 의 `rdx` = 자식 `PEB+0x308`, 부모 `CreateProcessInternalW+0x57d0` 의 `rdx` = 자식 `PEB+0x2d8`, 둘의 앞뒤 | "누가" 는 정적으로 답했지만, 차례는 "부모가 기다린다" `[추측]` 에 기댐 (2.10). `FromStructEx+0xb09` (rva `0x3c79`) 는 test.exe 에선 안 서야 맞다 | **Q83** |
| 4 빠른 길인가 — 부모 `+0x445f` 직후 `dd @rsp+0xc58 L2` → SxS Flags `0x40`, 메시지 `+0x7c` 비트 13 | 실행 때 값 (레지스트리 · 매체 · 부팅 상태) | **Q104 4** |
| 5 csrss 가 SxS 를 부르는 조건 (`InternalBaseSrvCreateProcess +0xca ~ +0xee`) — 첫 인자 · 부모 CSR_PROCESS `[+0x5c]` 비트 `0x2000`, test.exe 때 `+0xfb` 가 타나 | 실행 때 값 | **Q104 5** |
| 6 comctl32 6.0 이 어느 버전 폴더로 묶이나 (라이브) | test.exe 는 매니페스트가 없어 앱 문맥이 안 생김. 어느 버전인가 자체는 run88 에서 다룸 | **Q162** |
| 7 test.exe `PEB+0x2f8` 이 정말 `+0x40c JZ` 로 빠져서 0 인가 | 값 (null) 은 사진으로 이미 `[관찰]`. 빠진 자리 (Probe 결과) 는 실행 때 | **Q104 7** |
| ① sxs 블롭을 **쓰는 쪽** — `SxspBuildActCtxData` · `SxspAddRootManifestToActCtxGenCtx` 가 채우는 조립 명부 · 조립 정보 · 문자열 영역 칸 (run88 이 다룬 것은 빼고) | 지도 수준이라 쓰는 줄을 다 안 읽음 | **Q164 ①** |
| ② "6.0.0.0 요청 → 6.0.26100.x" 를 고르는 **코드 길** (`SxspParseApplicationPolicy` · 정책 폴더) | 같음. 고른 값 자체는 run88 에서 다룸 | **Q164 ②** |
| ③ 작은 `[공백]` 넷: 부모 CSR_PROCESS `[+0x5c]` 비트 `0x2000` 의 뜻 · 메시지 데이터 `+0x24` (`[RSP+0xc44]`, VDM 쪽 `[추측]`) · CreationFlags 비트 31 을 켜는 쪽 (`CreateProcessInternalW` 엔 없음) · SxS 실패 때 부모가 받는 값 · 처리 | 이번 줄기 밖 | **Q164 ③** |
| 블롭 안쪽 모양 (머리 · 목차 · 구역), 어느 주소에 붙여도 읽히나, 로더가 세 문맥을 보는 차례, 시스템 기본 문맥 실제 내용, 블롭 안 폴더 칸 (전체 경로 / 키), 폴더를 여는 쪽 | 이번은 만드는 쪽만 (Q151 을 둘로 나눔) | **run88 에서 다룸** (Q151b, sast/85) |
| PE_emulator `NtCreateUserProcess` 흉내가 매니페스트 주소 · 크기 · `Flags` 비트 13 을 주나 | PE_emulator 쪽 사정 | PE_emulator 확인거리 (2.12) |

---

## 5. 더 읽기

- run 문서: [`run87-sxs-create.md`](run87-sxs-create.md) — 맨 위 한 줄 결론, "PE_emulator 쪽에서 쓸 것" (요청 29 첫 조각 결론 · 원문 질문이 어디서 답해졌나 · 질문마다 표 · **누가 무엇을 자식에 넣나 (차례 1 ~ 14)** · SxS 메시지 표 · 두 갈래), 호출 지점 표, 호출 관계 그림, 확인된 것, 결과서와 다르게 적은 곳 · 보탠 것 ① ~ ⑦, Q87 닫힘, 정정, 아직 확인 필요 (동적 1 ~ 7 · 정적 Q164)
- 결과서: [`_workspace/sast/84_sxs-create_result.md`](../../_workspace/sast/84_sxs-create_result.md) — 한 장 지도, 질문 1 (부모 쪽, 메시지 모양 · 두 갈래) · 질문 2 (누가 무엇을 자식에 넣나 표) · 질문 3 (시스템 기본) · 질문 4 (WinSxS, 일부) · 질문 5 (엔진 판단), Q87 닫힘, 동적으로 확인할 것 1 ~ 7, Q151b 로 넘길 것. (같은 사진을 "run45 사진" 으로 부르는 곳은 run44 사진으로 읽기, `InternalBaseSrvCreateProcess` "`+0xf7/+0xfb`" 는 call `+0xfb` 로 읽기)
- 요청서: [`_workspace/sast/84_sxs-create.md`](../../_workspace/sast/84_sxs-create.md) ("KERNELBASE `BasepInitAppCompatData`" 는 kernel32 로 읽기)
- flow.md: [그림 1](flow.md) (KERNELBASE 대표 지점 (1) ~ (5) · 노란 메모 csrss) · 2.2 전체 순서 트리 · 2.2 `CreateProcessInternalW` 절 · 2.2 csrss 절 (sxssrv · sxs 사슬) · 4장 run87 줄 셋 · 4.1 그림 1 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.1 PEB (`+0x2d8` · `+0x2f8` · `+0x308`) · 3장 줄임 표 `①B…` · `①X…` · 4장 짝 · 7.2 `_PEB` · 7.6 · 7.8
- 정정: [run45 문서](run45-work-list.md) 맨 위 한 줄 (일 37)
- 앞 이야기:
  - [run14 해설](../02_sprint/run14-create-process-internal-해설.md) 2.6 — 부모가 하는 일 ① 검사 → ② csrss 알림 → ③ 후처리, `+0x2d8` 짐작, "있나" 검사 21개 (이번 2.3 · 2.9 에서 `[정적]` 으로)
  - [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) — csrss 1번 창구 `BaseSrvCreateProcess2` · SxS 간접 호출 (이번 2.6 의 앞)
  - [run44 해설](run44-baggage-snap-해설.md) 2.8 — 사진 K3 · P0
  - [run45 해설](run45-work-list-해설.md) — 일 37 · Q83 · Q87 이 생긴 곳
  - [run57 해설](run57-nls-apiset-해설.md) 2.4 · 2.8 — 벽에 붙이고 안내판에 적기 (ApiSet), 매니페스트 · 활성화 문맥
  - [run59 해설](run59-init-leftovers-해설.md) "나머지 C 묶음" ⓔ — 커널 `PspLocateInPEManifest`
  - [run23 해설](../02_sprint/run23-load-dependent-해설.md) 2.5 "곁가지" — SxS 리다이렉트 (읽는 쪽, run88 으로 이어짐)
