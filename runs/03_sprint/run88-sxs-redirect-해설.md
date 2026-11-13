# run88 sxs-redirect — 해설

> 이 글은 [run88 문서](run88-sxs-redirect.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q151b** — PE_emulator 요청 29 (SxS 활성화 문맥) 의 **둘째 · 마지막 조각, 읽는 쪽** (`_workspace/plan.md`, 이번에 **끝**)
  - 이것으로 **요청 29 가 끝났다**. 첫 조각 (만드는 쪽) Q151a 는 [run87](run87-sxs-create.md) ([해설](run87-sxs-create-해설.md)).
  - 겹치던 **Q90** ("ntdll 이 PEB 문맥 칸을 **읽는** 곳") 은 **문맥 쪽만 닫혔다**. `pShimData` 를 읽는 쪽만 남는다 (2.11).
  - 다음 항목: **Q152** (요청 30 — .NET exe 시작 길) → Q142 → [멈춤] (Q104 VM 단계 앞).
  - 이번에 남은 것이 간 곳 (4절):
    - 실행해 봐야 아는 것 → test.exe 로 되는 것 **Q104** (3 · 4 의 test.exe 몫), 쓰는 순간 **Q83** (6), comctl32 를 쓰는 프로세스가 필요한 것 **Q162** (1 · 2 · 5 · 4 나머지).
    - 정적 → **Q164** ① ② (sxs 가 블롭을 쓰는 쪽 · 버전 정책), 새 **Q165** ① ~ ④ (ntdll 쪽 남은 조각).
- 근거:
  - run 문서 [`run88-sxs-redirect.md`](run88-sxs-redirect.md) (가장 중요)
  - 요청서 [`_workspace/sast/85_sxs-redirect.md`](../../_workspace/sast/85_sxs-redirect.md) · 결과서 [`85_sxs-redirect_result.md`](../../_workspace/sast/85_sxs-redirect_result.md)
    - 판정 **분석성공 (지도 수준 + 데이터 모양 검증)**.
    - 결과서가 요청서 범위를 둘 넓혔다: ① sxs 가 DLL 구역을 **쓰는** 쪽 (2.6), ② **사진 덤프 오프라인 읽기** (2.7).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · **그림 2 (상자 둘, 번호 다시 매김)** · 2.2 · 2.5 · 2.6 (새 절 "SxS 리다이렉트") · 4장 · 4.1 · 5장 (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): **모르던 칸 셋 닫음** (마흔넷째판) + 7.1 묵은 숫자 바로잡음
  - 같이 고친 것: [run87 문서](run87-sxs-create.md) 의 "run88 에서 닫힘 / 다룸" 자리마다 "→ run88" 한 줄
- 이번 바퀴는 **정적 + 사진 덤프 오프라인 읽기** 다. 새 VM 실행은 없고, 동적 판정도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **이번은 "읽는 쪽" 이다.**
   - 책자 (블롭) 를 **누가 찍어 걸었나** 는 run87 이 답했다 — csrss 다.
   - 이번은 걸린 책자를 **ntdll 로더가 어떻게 펴 보나**, 책자 **안이 어떻게 생겼나** 다.
2. **test.exe 에선 길이 안 바뀐다 (짐작).**
   - test.exe 는 매니페스트가 없고 (run87 — `PEB+0x2f8` 8장 모두 null `[관찰]`) comctl32 도 안 쓴다 `[소스]`.
   - 공통 책자 (`PEB+0x308`) 만 있고, test.exe 가 올리는 이름은 그 책자에 없다 → 모두 "리다이렉트 없음" `[추측: 사진 블롭 내용 + 코드]` (2.8).
   - comctl32 이야기는 "**comctl32 를 쓰는 다른 exe 라면**" 으로 읽는다.
3. **새 방법 — 사진 덤프 오프라인 읽기.**
   - 멈춘 순간 찍어 둔 메모리 사진을 **서버에서 다시 뒤진 것** 이다. 한계: 찾은 책자가 **어느 프로세스 것인지는 아직 못 짝지었다** (2.7).
   - 결과서는 이걸 `[덤프 오프라인]` 이라고 적었다. CLAUDE.md 태그 목록에 없어서 run 문서는 `[관찰]` 로 옮기고 "test.exe `25996d00000` 이 그 블롭인지는 `[확인 필요]`" 를 붙였다 (오케스트레이터 지시). 이 글도 그대로 따른다.
   - 사진에서 찾은 **앱 블롭 (comctl32 v6) 은 test.exe 것이 아니다** — 다른 GUI 프로세스 것이다.
4. **"미니 해석기" 결과는 진짜 ntdll 이 돌려준 값이 아니다.**
   - 로더 코드를 읽고 파이썬으로 **다시 쓴 것** 에 블롭을 넣어 본 결과다. "코드로 예상한 값과 같았다" 까지다 (2.7).
5. **이름은 대부분 `[추측: 이름]` 이다.**
   - PDB 에 타입이 있는 건 블롭 머리 `_ACTIVATION_CONTEXT_DATA` **하나뿐** 이다. 목차 · 구역 · 원소 · 값의 이름은 공개 이름을 빌린 것이다.
   - 상태 값 이름 (`0xC0150008` = `STATUS_SXS_KEY_NOT_FOUND` 등) 도 `[추측: 이름]`.
   - **오프셋 · 크기 · 읽는 줄은 `[정적]`** 이다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) = 가게 **안내판**, TEB = 직원 **개인 안내판** → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.2
- DLL = **건물**, 모듈 항목 = **명부 한 줄**, 로드 문맥 = **작업 지시서**, `LdrpLoadDependentModuleInternal` = **배달 기사** → [run23 해설](../02_sprint/run23-load-dependent-해설.md) 비유
- 맨 이름 / **전체 이름** (지시서 flags `0x200`) = 짐표에 이름만 적혔나 / **주소까지 적혔나** → [run23 해설](../02_sprint/run23-load-dependent-해설.md) 2.4 "새 말 둘"
- `\KnownDlls` = **동네 공용 창고**, KnownDll 길 = 공용 창고에서 꺼내 오기, 파일 길 = **설계도를 펴서 세트를 새로 짜기** → [run21 해설](../02_sprint/run21-load-dll-internal-해설.md) 2.4 · [run26 해설](../02_sprint/run26-map-dll-file-해설.md)
- SxS 리다이렉트 = 매니페스트를 보고 DLL 이름을 다른 버전 파일로 돌리기 (`+0x142b`) → [run23 해설](../02_sprint/run23-load-dependent-해설.md) 2.5 "곁가지"
- api-set = **별명**, PEB `ApiSetMap` = **별명 사전** → [run37 해설](../02_sprint/run37-k32-import-order-해설.md) · [run57 해설](run57-nls-apiset-해설.md)
- 보안 프로세스 (`LdrpIsSecureProcess`) → [run32 해설](../02_sprint/run32-params-initblock-해설.md) 2.13
- 매니페스트 = **설정 쪽지**, 활성화 문맥 = **설정 묶음** → [run57 해설](run57-nls-apiset-해설.md) 2.8 · [run67 해설](run67-kb-heap-init-해설.md) 2.5
- **SxS**, **sxssrv** = 인사팀 사무실 **책자 담당**, **sxs.dll** = **제본 기계**, **블롭** = 설정 묶음을 **찍어 낸 책자**, **시스템 기본 문맥 / 앱 문맥** = **공통 책자 / 가게 전용 책자** → [run87 해설](run87-sxs-create-해설.md) 1절 · 2.1 · 2.7
- 활성화 문맥 스택 (TEB `+0x290` `_ActivationStack`, 포인터 칸 `+0x2c8`) = 직원이 손에 든 **더미** (run54 해설에선 "지침서 더미") → [run54 해설](run54-teb-fill-해설.md) 2.10
- 해시 표 = **이름 색인 카드함** → [run20 해설](../02_sprint/run20-kernel32-load-해설.md) 2.3, 해시로 찾기 = **색인 탭** → [run37 해설](../02_sprint/run37-k32-import-order-해설.md) 2.4
- 이분 탐색 = 가나다순 목록을 **반씩 접어** 찾기 → [run36 해설](../02_sprint/run36-snap-module-해설.md) 2.4
- `LOCK CMPXCHG` = "비교 후 바꾸기" 를 한 번에 → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.6
- 스냅샷 = **보관 상자**, 덤프 = **앨범**, 사진 K1 ~ P2, `????` = 읽기 불가 → [run44 해설](run44-baggage-snap-해설.md) 2.2 · 2.8
- 가상 주소 → 물리 주소 = **페이지 걷기** → [run59 해설](run59-init-leftovers-해설.md) 2.13
- `KUSER_SHARED_DATA` (KUSD) = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5

이번에 처음 나오는 말 — **`.local`**, **구역 · 목차**, **원소 · 키 · 값**, **조립 · 조립 명부**, **저장소 루트 · 저장소 맵**, **사진 덤프 오프라인 읽기 · 물리 연속 가정**, **미니 해석기** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, PEB = 가게 **안내판**, TEB = 직원 **개인 안내판**, ntdll = 가게 안 **관리실**, 커널 = **관리사무소**.
- csrss = 이웃 **인사팀 사무실**, sxssrv = 그 안 **책자 담당**, sxs.dll = **제본 기계**.
- 블롭 = **찍어 낸 책자**. 모든 가게에 거는 **공통 책자** (`PEB+0x308`), 쪽지가 있는 가게만 받는 **가게 전용 책자** (`PEB+0x2f8`).
- DLL = **건물**, 배달 기사 (`LdrpLoadDependentModuleInternal`) 가 짐표 (DLL 이름) 를 들고 다닌다. `\KnownDlls` = **동네 공용 창고**.
- run87 해설의 창고 그림도 잇는다: 같은 이름 부품 (comctl32) 이 v5.82 · v6 두 가지 있다. **기본 선반** = System32, **버전별 선반 칸** = WinSxS 폴더들.
- 이번에 새로 붙는 그림 (본문에서 하나씩):
  - 책자 안은 **표지 → 목차 → 장 → 색인 줄 → 뜻풀이** 로 생겼다.
  - 책자는 위치를 **쪽수로만** 적는다 — 그래서 어느 벽에 걸어도 읽힌다.
  - 책자엔 "부품 **세트 이름**" 만 있고, 창고 **동네 주소** (`C:\WINDOWS\WinSxS\`) 는 관리실이 붙인다.
  - 관리실은 처음 찾아간 세트 창고 주소를 **주소 수첩** (저장소 맵) 에 적어 둔다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 (요청서 번호) | 답 (run 문서 그대로) |
|---|---|
| 1 리다이렉트와 KnownDll 중 무엇이 먼저인가 | **리다이렉트가 먼저.** 전체 경로가 나오면 KnownDll 은 "폴더가 System32 가 아님" 으로 거절 → 디스크 파일에서 올린다 `[정적]` (2.3) |
| 2 어느 문맥을 보나 | ① 스레드 활성화 문맥 (TEB `+0x2c8`) → ② `PEB+0x2f8` → ③ `PEB+0x308`. 셋 다 없으면 `0xC0150008` = 리다이렉트 없음 `[정적]` (2.4) |
| 3 블롭 모양 | 위치 칸이 **전부 32비트 오프셋** — 어느 주소에 붙여도 읽힌다. 머리 → 목차 → 문자열 구역 → 원소 → DLL 값 `[정적]` (2.5) |
| 4 comctl32 v6 최소 조건 | DLL 구역 (id 2) 에 키 `comctl32.dll` 한 줄 + 값. **윈도우는 키만 넣는다** — 루트 `C:\WINDOWS\WinSxS\` 는 ntdll 이 런타임에 붙인다 `[정적]` (2.6) |
| 5 시스템 기본 문맥만 있을 때 | `comctl32.dll` 은 **v5.82 로 가되 WinSxS 5.82 폴더 경로로** (KnownDll 아님). 공통 책자엔 v6 항목이 없다 `[관찰]` — 사진 덤프 오프라인, test.exe 블롭인지는 `[확인 필요]` (2.8) |
| 6 엔진이 흉내 낼 최소 블롭 | **B** (`0x1dc`B, 전체 경로) 먼저, 다음 **A** (`0x21c`B, 키만). 해시 알고리즘 1 (2.9) |
| (겹침) Q90 | ntdll 이 `PEB+0x2f8` · `+0x308` 을 읽는 곳 — **문맥 쪽 닫힘** `[정적]` (2.11) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run87 은 **부모 쪽 마무리 구간** (flow.md 2.2) 이었다. 이번엔 시간이 훌쩍 뒤로 간다. 첫 스레드가 깨어나 ntdll 이 **DLL 을 들이는 구간** (flow.md 2.5 · 2.6) 이다.

```
cmd.exe 의 부탁 (CreateProcess)
 ├─ 커널: 프로세스 · PEB · 첫 스레드 (멈춘 채)
 ├─ csrss: 공통 책자를 걸고 PEB +0x308 에 번지 (+ 쪽지 있으면 +0x2f8)     ← run87 (만드는 쪽)
 └─ 첫 스레드 깸 → ntdll!LdrInitializeThunk → … → LdrpInitializeProcess
      ├─ ●P1a  kernel32 준비 → LdrLoadDll(kernel32)
      │          └─ LdrpLoadDll +0xbd LdrpPreprocessDllName +0x622
      │               → RtlDosApplyFileIsolationRedirection_Ustr          ← 이번 (LoadLibrary 길)
      ├─ import 이름마다  LdrpLoadDependentModuleInternal  (배달 기사)
      │          ├─ +0x1426 → RtlDosApplyFileIsolationRedirection_Ustr     ← 이번 (import 길)
      │          └─ +0x178d → LdrpLoadKnownDll  (그 뒤)
      ├─ ●P1b  그래프 켜기 (dll 시작 함수 · TLS 콜백)
      … → NtContinue (은퇴) → exe 진입점 → main
```

- 이름을 받는 자리가 **둘** 이다 `[정적]`: import 는 `LdrpLoadDependentModuleInternal +0x1426`, `LoadLibrary` 는 `LdrpPreprocessDllName +0x622`. 둘 다 같은 함수를 같은 모양으로 부른다.
- test.exe 에서 이 검색이 **처음** 도는 때는 `P1a~P1b` 사이, kernel32 를 올릴 때로 보인다 `[추측]` (2.7 끝).

### 왜 이 질문이 지금 왔나

- run87 해설 1절 그대로다. PE_emulator 엔 csrss 가 없어서 활성화 문맥 데이터가 통째로 빠져 있다.
- 막히는 장면 `[관찰 — PE_emulator 흉내]` (요청서): 문맥 없음 → KnownDll `COMCTL32` = System32 **v5.82** → 서수 #345 · #381 이 없다 → `0xc0000138`.
- run87 이 "책자를 누가 · 어떻게 거나" 를 답했다. 그래서 이번엔 뒤 반쪽 — **"걸린 책자를 로더가 어떻게 읽나, 엔진이 걸 가장 작은 책자는 어떤 모양인가"** 다.
- 사용자 결정 (2026-11-12): 첫 답은 **지도 수준**.

### 무엇이 궁금했나

요청서 질문 여섯.

1. **리다이렉트 길** — `LdrpLoadDependentModuleInternal +0x1426` 아래 부르는 순서. **KnownDll 검사와 리다이렉트 중 무엇이 먼저인가.** comctl32 는 KnownDll 인데, 리다이렉트가 이기려면?
2. **어느 문맥을 보나** — 스레드 → 프로세스 기본 → 시스템 기본 순서인가. 셋 다 없으면 (PE_emulator 지금) 결과는 "리다이렉트 없음" 인가.
3. **`ACTIVATION_CONTEXT_DATA` 최소 칸 표** — 로더가 DLL 이름을 찾을 때 읽는 칸만 (오프셋 · 크기 · 뜻 · 읽는 줄).
4. **comctl32 v6 로 가는 최소 조건** — 블롭에 무엇이 있으면 `C:\Windows\WinSxS\<폴더>\comctl32.dll` 로 바뀌나, 그 파일을 여는 syscall.
5. **시스템 기본 문맥만 있을 때** (매니페스트 없는 exe) — comctl32 는 v5.82 로 가나 v6 로 가나.
6. **nt 엔진이 흉내 낼 최소 블롭** — 모양 (칸과 값) 과 해시 함수.

그리고 겹치던 **Q90** — "ntdll 이 PEB `SystemDefaultActivationContextData` 를 **읽는** 곳".

### 바퀴 전에 알던 것

- `[정적]` (run23) `LdrpLoadDependentModuleInternal +0x1426` (ret `+0x142b`) 이 의존 DLL 이름 리다이렉트 자리다.
  - 이 함수는 부모 명부 줄의 활성화 문맥을 켠 채 돈다 → SxS 리다이렉트가 **부모의 매니페스트 기준** 일 것 `[추측]`.
  - 지시서 flags `0x200` 은 "전체 이름" 쪽이라고 읽었다. 비트 이름은 못 정했다.
- `[정적]` (run21 · run65) KnownDll 길 = `LdrpLoadKnownDll` → `LdrpFindKnownDll` → `NtOpenSection(\KnownDlls\<이름>)`.
- `[정적]` (run54) 첫 스레드의 TEB `+0x2c8` 은 TEB `+0x290` (빈 활성화 문맥 스택) 을 가리킨다.
- `[정적]` + `[관찰]` (run87 · run44) 자식 PEB `+0x308` 은 csrss 가 **늘** 채운다 — test.exe P0 `25996d00000`. `+0x2f8` 은 매니페스트가 있을 때만 — test.exe 는 8장 모두 (null).
- `[정적]` (run67) `LdrpFindDllActivationContext +0x16c` 가 `PEB+0x2f8` 을 본다 (≠ 0 이면 exe 자기 매니페스트를 다시 안 찾음).
- `[관찰 — PE_emulator 흉내]` 위의 `0xc0000138`.

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 짐표 하나가 지나는 길

run 문서 "호출 관계" 그림을 줄여 그렸다. 라벨은 ret 자리. 모두 `[정적]`.

```
[import 이름 하나]  LdrpLoadDependentModuleInternal          (배달 기사)
 ├─ +0x2ff   ApiSetpResolveHost                         (api-set 이름이면 진짜 이름으로)
 ├─ +0x142b  RtlDosApplyFileIsolationRedirection_Ustr   ← 이름 돌리기 (보안 프로세스면 건너뜀)
 │     ├─ +0x6d1  sxsisol_RespectDotLocal               .local 있을 때만. 찾으면 책자는 안 봄
 │     └─ +0x489  sxsisol_SearchActCtxForDllName
 │            ├─ +0xce  RtlFindActivationContextSectionString(…, 구역 id 2, 이름, 결과)
 │            │     ├─ +0xf2   RtlpFindNextActivationContextSection   ① 스레드 → ② PEB+0x2f8 → ③ PEB+0x308
 │            │     │     └─ +0x9a  RtlpLocateActivationContextSection   목차에서 id 2 찾기
 │            │     ├─ +0x213  RtlpFindUnicodeStringInSection         구역 안에서 이름 찾기
 │            │     └─ +0x2e5  RtlpFindActivationContextSection_FillOutReturnedData
 │            │   셋 다 없으면 0xc0150001 → +0xdf 에서 0xc0150008 = "리다이렉트 없음"
 │            ├─ +0x21a  RtlGetAssemblyStorageRoot       값 플래그 비트 1 (키만 방식) 일 때만
 │            │     → (처음만) PEB +0x300 / +0x310 저장소 맵 · NtOpenKey · 폴더 NtOpenFile
 │            └─ +0x2f5 / +0x3ba / +0x50b  memcpy       루트 · 조각 · 물어본 이름 이어 붙이기
 │   성공이면 +0x15af BTS ESI,9  (지시서에 "전체 이름")
 ├─ +0x16ee  LdrpFindLoadedDllByName                    (이미 있나)
 ├─ +0x115c  LdrpAllocateModuleEntry                    (새 명부 줄)
 ├─ +0x1792  LdrpLoadKnownDll
 │     ├─ (비트 9)  +0x37 LdrpCheckKnownDllFullPath     폴더 ≠ System32 → 0xc0000135
 │     └─ (이름만)  +0x63 LdrpFindKnownDll → NtOpenSection(\KnownDlls\<이름>)
 └─ (0xc0000135) +0x1cae LdrpProcessWork → (비트 9) +0x144 LdrpMapDllFullPath → NtOpenFile · NtCreateSection
                 또는 +0x182c LdrpQueueWork

[LoadLibrary]  LdrpLoadDll
 ├─ +0xc2  LdrpPreprocessDllName → +0x627 RtlDosApplyFileIsolationRedirection_Ustr   (같은 모양)
 └─ +0x100 LdrpLoadDllInternal → … LdrpFindOrPrepareLoadingModule → +0x5e5 LdrpLoadKnownDll
```

- 위에서 아래로가 **시간 순서** 다. 리다이렉트 (`+0x142b`) 가 KnownDll (`+0x1792`) 보다 위에 있다 — 이게 이번 바퀴의 첫째 답이다 (2.3).
- `sxsisol_*` 도 ntdll 안 함수다 (결과서가 새로 뽑은 ntdll 22개 안).

비유: 배달 기사가 짐표 (DLL 이름) 를 받으면, 공용 창고에 가기 **전에** 먼저 책자를 펴 "이 이름은 다른 데서 가져와라" 줄이 있나 본다. flow.md 2.6 새 절이 같은 그림을 쓴다.

**새 말 — `.local`**

- `ProcessParameters.Flags` 비트 `0x1000` 으로 켜지는 **또 하나의 돌리기 장치** 다 (run87 해설 2.5 표에서 이름만 나옴). 일반적으로는 exe 옆에 `<exe>.local` 이 있으면 DLL 을 exe 쪽에서 먼저 찾게 하는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이번 코드에서 본 건 "켜져 있으면 **책자보다 먼저** 본다" 뿐이다 `[정적]` (`RtlDosApplyFileIsolationRedirection_Ustr +0x443` → `+0x6cc`, 찾으면 `+0x455` 에서 책자는 건너뜀).
- 비유: 가게 문에 붙은 "**우리 가게 선반 먼저**" 표지.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ +0x142b  RtlDosApplyFileIsolationRedirection_Ustr   ← 이름 돌리기 (보안 프로세스면 건너뜀)`

- **누가**: 첫 스레드의 ntdll 배달 기사 `LdrpLoadDependentModuleInternal`.
- **어디서**: CALL 이 `+0x1426`, 돌아올 자리가 `+0x142b`. 콜스택엔 `LdrpLoadDependentModuleInternal+0x142b` 로 보일 자리다.
- **누구를**: `RtlDosApplyFileIsolationRedirection_Ustr` (rva `0x3eb80`).
- **무엇을 넘기나** `[정적]`: `ecx = 1`, `rdx` = 이름, `r8` = `".DLL"` (점이 없으면 붙일 확장자), `[rsp+0x20]` = 결과 문자열 자리.
- **무엇을 위해**: 이 이름을 활성화 문맥으로 **다른 경로로 돌릴지** 알아보려고.
- **안 타는 때** `[정적]`:
  - 보안 프로세스면 — `+0x13e7 CMP [LdrpIsSecureProcess],0`.
  - api-set 이름을 진짜 이름으로 바꾼 길이고 `.local` 이 꺼져 있으면 — `+0x62f TEST [RAX+8],0x1000` · `+0x638 XOR R15B,R15B` → `+0x661 TEST R15B` · `+0x664 JNZ` (R15B 처음 값 1 은 `+0x264`).
- **돌아온 뒤**: `+0x142b MOV R14D,EAX` 로 결과를 받는다. `0xc0150008` (리다이렉트 없음) 이면 오류로 안 적는다 (`+0x14f2 CMP R14D,0xc0150008` · `+0x14f9 JZ`).

#### 줄 2: `│     ├─ (비트 9)  +0x37 LdrpCheckKnownDllFullPath     폴더 ≠ System32 → 0xc0000135`

- **누가**: `LdrpLoadKnownDll` (rva `0x9970`) — 공용 창고 담당.
- **어디서**: CALL `+0x32`, 돌아올 자리 `+0x37`.
- **언제만** `[정적]`: 작업 지시서 `+0x20` 의 비트 9 가 켜졌을 때만 — `+0x9 MOV EAX,[RCX+0x20]` · `+0x23 BT EAX,9` · `+0x27 JNC`.
- **누구를**: `LdrpCheckKnownDllFullPath` (rva `0x9ac0`).
- **무엇을 하나** `[정적]`: 경로에서 마지막 `\` 앞 폴더를 잘라 `LdrpKnownDllPath` (System32) 와 길이 · 글자를 비교한다 (flow.md 2.5).
- **결과**: 다르면 `AL = 0` → `+0x3e JZ` → `+0x113 MOV EAX,0xc0000135` ("KnownDll 아님"). `\KnownDlls` 를 **열지도 않는다**.

---

### 2.3 ★ 질문 1 — 리다이렉트가 KnownDll 을 이긴다

#### 먼저, 순서 `[정적]`

- import 길: `LdrpLoadDependentModuleInternal +0x1426` (리다이렉트) → `+0x178d` (`LdrpLoadKnownDll`).
- LoadLibrary 길: `LdrpLoadDll +0xbd` (`LdrpPreprocessDllName +0x622` 리다이렉트) → `+0xfb` (… `LdrpFindOrPrepareLoadingModule +0x5e0 LdrpLoadKnownDll`).
- 두 길 다 **리다이렉트가 먼저** 다 (오케스트레이터 · flow-writer `.asm`).

#### 그런데 순서만으론 부족하다 — 왜 이기나, 단계별로

`comctl32.dll` 은 KnownDll 이다. 리다이렉트가 먼저 돌아도, KnownDll 이 다시 "공용 창고에 있네" 하고 System32 것을 주면 소용없다. 그래서 둘 사이에 **신호** 가 필요하다.

1. **근거** `[정적]`: 리다이렉트가 성공해서 경로가 나오면 `+0x15af BTS ESI,9` — 지역 flags 비트 9 (`0x200`, run23 해설의 "**전체 이름**") 를 켠다. SxS 로 얻었으면 `+0x15bc OR ESI,4` 도 켠다.
2. **근거** `[정적]`: `LdrpLoadKnownDll +0x23` 이 작업 지시서 `+0x20` 의 **같은 비트 9** 를 본다 (줄 2).
3. **그래서** `[정적]`: 켜져 있으면 공용 창고 장부를 뒤지지 않고 **경로의 폴더만** System32 와 비교한다. WinSxS 폴더는 System32 가 아니니 `0xc0000135`.
4. **그래서** `[정적]`: 기사는 `+0x1ca9` (ret `+0x1cae`) `LdrpProcessWork` 로 간다. 거기서도 `+0x5b BT EAX,9` 를 보고 `+0x13f LdrpMapDllFullPath` → `LdrpMapDllNtFileName +0x132 NtOpenFile` · `+0x18f NtCreateSection` — **디스크 파일 길** 이다.
5. **아직 확인 아님**: ESI (지역 flags) 가 새 작업 지시서 `+0x20` 으로 옮겨지는 길 (`+0x94a MOV [RSP+0x98],ESI` … `+0x10ec MOV [RAX+0x20],R15D`) 은 끝까지 안 따라갔다 `[추측]` → **Q165 ①**. 확실한 건 `LdrpLoadKnownDll +0x23` 과 `LdrpProcessWork +0x5b` 가 **같은 지시서 `+0x20` 의 비트 9** 를 읽는다는 것이다.

- 덤: api-set 이름을 System32 경로로 푼 길도 비트 9 를 켠다 `[정적]` (run23 해설 2.5). 그땐 폴더가 System32 라 비교를 **통과** 하고, 원래대로 `+0x5e LdrpFindKnownDll` 로 갈 것이다 `[추측: run23 의 "System32 경로" + 이번 조건 줄 "비트 9 꺼짐 또는 폴더가 System32" (run 문서 호출 지점 표)]`.

비유: 짐표에 **주소까지 적혀 있으면** (비트 9), 공용 창고 직원은 창고 장부를 안 뒤지고 주소의 **동네** 만 본다. "우리 동네 (System32) 주소면 창고에서 꺼내 주고, 다른 동네 (WinSxS) 면 '우리 물건 아님'" 하고 돌려보낸다. 기사는 그 주소로 직접 가서 **설계도를 펴 새로 짠다** (파일 길).

#### `RtlDosApplyFileIsolationRedirection_Ustr` 안의 차례 `[정적]`

1. 확장자 붙이기 — `+0x1dd/+0x1e2 RtlFindCharInUnicodeString` 로 `.` 을 찾고, 없으면 `.DLL` 을 붙인다.
2. (이름이 드라이브 · 슬래시 모양일 때만) `+0x361/+0x366 RtlGetFullPathName_UstrEx`.
3. **`.local` 먼저** (`+0x443` → `+0x6cc/+0x6d1`). 찾았으면 책자는 안 본다 (`+0x455`).
4. **책자 (활성화 문맥)** — `+0x484/+0x489 sxsisol_SearchActCtxForDllName`.
5. 마무리 — `+0x603/+0x608` 결과를 부른 쪽 문자열에 확정.

#### 지금 PE_emulator 는

- 문맥이 없을 때의 **결과** ("리다이렉트 없음") 는 맞다 (2.4).
- 그러나 순서를 **"KnownDll 먼저"** 로 짰다면 틀림이다. 책자를 넣어도 v6 로 안 간다. "리다이렉트 → (폴더 ≠ System32 면) KnownDll 거절" 차례여야 한다.

---

### 2.4 ★ 질문 2 — 책자 셋을 정해진 차례로 편다

#### 차례 `[정적]`

`RtlpFindNextActivationContextSection` (rva `0x41f60`) 이 "다음에 펼 책자" 를 하나씩 골라 준다.

| 차례 | 어디서 꺼내나 | 고르는 줄 | 비유 |
|---|---|---|---|
| ① | TEB `+0x2c8` → 스택 **맨 위 틀** 의 문맥 | `+0x105` | 직원이 지금 **손에 든 책자** (더미 맨 위) |
| ② | `PEB+0x2f8` `ActivationContextData` (프로세스 기본 = 앱 문맥) | `+0x119` | 가게 **전용 책자** |
| ③ | `PEB+0x308` `SystemDefaultActivationContextData` (시스템 기본) | `+0x131` | **공통 책자** |

- 고른 책자마다 `+0x95/+0x9a RtlpLocateActivationContextSection` 이 목차에서 **DLL 구역 (id 2)** 을 찾고, `RtlpFindUnicodeStringInSection` 이 그 안에서 이름을 찾는다 (2.5).
- 못 찾으면 (`0xC0150001`) `+0x255/+0x25a` 에서 **다음 책자** 로 넘어간다.
- 틀 값이 특수 표식 `-4` 면 `+0x192` 에서 `PEB+0x308` 을 본다. `-3` 은 "비어 있는 문맥" 표식이다 (결과서). 두 표식의 이름은 `[추측]` → **Q165 ③**.
- 어느 책자에서 찾았는지는 결과 칸 `ulFlags` (`+0x44`) 에 남는다: 스레드 **0** · 프로세스 기본 **1** · 시스템 기본 **2** `[정적]`. `hActCtx` (`+0x38`) 는 기본 문맥이면 0.
- 결과 칸 `ACTCTX_SECTION_KEYED_DATA` (`0x70`B) 는 블롭이 아니라 **함수끼리 주고받는 양식** 이다 (run 문서).

#### 셋 다 없으면 — "리다이렉트 없음" `[정적]`

1. `RtlFindActivationContextSectionString` (rva `0x41680`) **맨 앞** 에서 먼저 본다: `+0x30` (`PEB+0x2f8`) · `+0x134` (`PEB+0x308`) · `+0x141 ~ +0x148` (TEB `+0x2c8` → 맨 위 틀).
2. 셋 다 비면 `+0x151 MOV EAX,0xc0150001` 로 바로 끝.
3. 받은 쪽 `sxsisol_SearchActCtxForDllName +0xd8 CMP EAX,0xc0150001` → `+0xdf LEA EBX,[RAX+7]` = **`0xc0150008`** 로 바꿔 돌려준다.
   - 왜 바꾸나 (결과서): 그대로 두면 `RtlDosApply…` 안의 확인 문 (`RtlAssert "Status != STATUS_SXS_SECTION_NOT_FOUND"`) 에 걸린다.
4. 그래서 → **"리다이렉트 없음"** 이다. 책자는 있는데 DLL 구역 (id 2) 이 없을 때도 같은 결과다.

- 지금 PE_emulator (셋 다 없음) 의 결과는 **맞다**. 진짜 ntdll 도 같은 `0xC0150008` 을 낸다.

#### TEB `+0x2c8` 은 널이면 안 된다 `[정적]`

- `+0x141 MOV RAX,[RCX+0x2c8]` · `+0x148 CMP [RAX],R15` — 포인터를 **널 검사 없이** 따라간다. 널이면 읽기 오류다.
- 진짜에선 첫 스레드가 `RtlpInitializeThreadActivationContextStack` 으로 TEB `+0x290` (빈 스택, `ActiveFrame` 0) 을 가리키게 둔다 (run54, 짐 지도 3.3).
- 엔진에 주는 뜻: 책자를 하나도 안 넣더라도 이 칸은 **빈 더미** 를 가리켜야 한다.

비유: 직원 개인 안내판에 "내 책자 더미는 저기" 가 적혀 있어야 한다. 더미가 비어 있는 건 괜찮다. 그런데 **적는 칸 자체가 비어 있으면**, 관리실은 확인도 안 하고 그 자리로 손을 뻗다가 넘어진다.

#### 부모 매니페스트 기준이라던 run23 짐작은? — 방향은 맞다

- 찾는 차례의 **첫째가 스레드 맨 위 틀** 이다 (`+0x105`). 그래서 기사가 부모 명부 줄의 문맥 틀을 올려 두었다면 그게 먼저 보인다 `[정적]`.
- 다만 틀을 올리는 줄 (`LdrpLoadDependentModuleInternal +0x71 ~ +0x1d3`) 자체는 안 읽었다. 부모 `+0x88` 이 0 일 때 (test.exe 의 exe · dll1) **틀을 안 올리나, 빈 틀을 올리나** 는 `[추측]` 그대로 → **Q165 ②** (flow.md 2.6).

---

### 2.5 질문 3 — 책자 안은 이렇게 생겼다

#### 먼저 가장 중요한 한 줄 — 쪽수만 적힌 책 `[정적]`

- 블롭 안에서 "어디에 무엇이 있다" 를 가리키는 칸은 **전부 32비트 오프셋** 이다. 기준은 **블롭 시작** 이거나 **구역 시작** 이다.
- 읽는 길에 8바이트 **절대 주소를 따라가는 곳이 없다** (결과서 표 3, 미니 해석기).
- 그래서 → **어느 주소에 붙여도 읽힌다.** test.exe 사진의 `25996d00000` 처럼 64KB 에 맞춰 붙이는 건 윈도우가 그렇게 붙인다는 것뿐, 로더는 안 따진다.

비유: 책자 안 "→ 37쪽 참고" 처럼 **쪽수로만** 가리킨다. "몇 번지 벽" 같은 말은 한 번도 안 나온다. 그래서 어느 가게 어느 벽에 걸어도 그대로 읽힌다.

**새 말 — 구역 · 목차**

- **구역** (section) = 블롭 안의 **장** 하나. 장마다 번호 (id) 가 있고, **DLL 리다이렉트 장이 id 2** 다 `[정적]` (`sxsisol +0x6a MOV R8D,2`).
  - 덤프 목차에서 본 다른 번호도 있다 — 1 조립 정보 · 3 창 클래스 · 4 COM 서버 · 5 COM 인터페이스 · 6 타입 라이브러리 · 7 ProgID · 9 CLR · 10 앱 설정 · 11 호환성 · 12 WinRT (이름은 공개 이름 `[추측]`). 이번 길은 2 만 본다.
- **목차** (TOC) = 장 번호 → "몇 쪽부터 몇 쪽 길이" 를 적은 앞쪽 표.

**새 말 — 원소 · 키 · 값**

- 문자열 구역 (DLL 장) 은 **색인 줄** 들의 모음이다. 한 줄 = **원소** (`0x18`B).
- 원소는 **키** (찾는 이름, 예: `comctl32.dll`) 와 **값** (그 이름을 어디로 돌릴지) 의 위치를 적는다.
- 비유: 색인의 **표제어** (키) 와 그 **뜻풀이** (값).

#### 읽는 차례 — 표지 → 목차 → 장 → 색인 줄 → 뜻풀이

run 문서 "최소 칸 표" A ~ F 에서 **로더가 실제로 보는 칸만** 골랐다. 모두 `[정적]`. 이름은 표 A 만 PDB, 나머지는 `[추측: 이름]`.

| 차례 | 어디 (크기) | 보는 칸 | 읽는 줄 (대표) |
|---|---|---|---|
| 1 표지 = **블롭 머리** (`0x20`B, PDB `_ACTIVATION_CONTEXT_DATA`) | 블롭 `+0` | `HeaderSize` ≥ `0x20` · **`TotalSize`** (모든 오프셋의 경계 검사) · `DefaultTocOffset` (목차 위치, 블롭 기준) · `AssemblyRosterOffset` (A 방식만) | `RtlpLocateActivationContextSection +0x3e` · `+0x2a` · `+0x58` |
| 2 **목차** (머리 `0x10`B + 항목 `0x10`B 씩) | 블롭 `+DefaultTocOffset` | `EntryCount` · `FirstEntryOffset` (블롭 기준) · `Flags` (비트 0 빽빽 / 비트 1 정렬 → 이분 탐색) · 항목 `{SectionId, Offset, Length}` — **id 2 를 찾는다** | `Locate +0x6f` · `+0x82` · `+0xed` · `+0x151` · `+0x15e` |
| 3 **장 머리** = 문자열 구역 머리 (`0x2c`B) | 블롭 `+목차 항목 Offset` | `Magic` `0x64487353` ("SsHd") · `FormatVersion` 1 · `DataFormatVersion` 1 · `Flags` (비트 0 대소문자 무시 / 비트 1 정렬) · `ElementCount` (0 이면 못 찾음) · `ElementListOffset` · `HashAlgorithm` · `SearchStructureOffset` (해시표) | `RtlFindActivationContextSectionString +0x1e3` (Magic), `RtlpFindUnicodeStringInSection` |
| 4 **색인 줄** = 원소 (`0x18`B) | 구역 `+ElementListOffset` | `PseudoKey` (키 해시) · `KeyOffset` · `KeyLength` (바이트) · `Offset` (값 위치, **0 이면 안 채움**) · `Length` (DLL 은 **`0x14` 이상**) · `AssemblyRosterIndex` (**0 은 못 씀**) | `RtlpFindUnicodeStringInSection +0x28c` · `+0x290` · `+0x2c9`, `sxsisol +0x163` |
| 5 **뜻풀이** = DLL 리다이렉트 값 | 구역 `+원소 Offset` | `Flags` (아래) · `PathSegmentCount` · `PathSegmentOffset` → 조각 `{Length, Offset}` | `sxsisol +0x193` · `+0x186` |
| (A 방식만) 6 **세트 목록** = 조립 명부 · 조립 정보 | 블롭 `+AssemblyRosterOffset` | 명부 번호 < `EntryCount` → 조립 정보 `+0x50` 폴더 이름 길이 · `+0x54` 폴더 이름 위치 | `RtlGetAssemblyStorageRoot +0xba`, `RtlpResolveAssemblyStorageMapEntry` (2.6) |

- 블롭 머리의 다른 칸 (`Magic` · `FormatVersion` · `Flags`) 은 **이 길에선 안 읽는다**. `Magic` · `FormatVersion` 은 블롭을 만들 때 `RtlpValidateActivationContextData` 가 검사할 뿐이다 (run 문서 표 A). (장 머리의 `Magic` · `FormatVersion` 은 다르다 — 위 3번처럼 이 길에서 읽는다.)

**DLL 값의 `Flags` 네 비트** `[정적]` (모두 `sxsisol_SearchActCtxForDllName` 이 읽음)

| 비트 | 뜻 | 줄 |
|---|---|---|
| 0 (`1`) | 조각이 **파일 이름까지** 포함 → 물어본 이름을 안 붙임 | `+0x337` |
| 1 (`2`) | **조립 폴더 루트를 앞에 붙임** (A 방식, 2.6) | `+0x1cb` → `+0x215 RtlGetAssemblyStorageRoot` |
| 2 (`4`) | 환경변수 펼침. 비트 1 과 같이 켜면 거부 (`0xc0150003`) | `+0x3d9` → `+0x3f9` · `+0x1d7` → `+0x5c8` |
| 3 (`8`) | 이름은 마지막 `\` 뒤 조각만 붙임 | `+0x33f` |

#### 기준점이 둘이다 — 헷갈리기 쉬운 곳 `[정적]`

- **블롭 기준**: 머리의 `DefaultTocOffset` · `AssemblyRosterOffset`, 목차의 `FirstEntryOffset` · 항목 `Offset`, 명부의 `FirstEntryOffset` · 조립 정보 위치.
  - (A 방식에만 셋째 기준점이 있다 — 조립 정보 안 폴더 이름 위치 `+0x54` 는 명부 머리 `+0x10` 이 가리키는 곳 기준.)
- **구역 기준**: 구역 머리의 `ElementListOffset` · `SearchStructureOffset`, 원소의 `KeyOffset` · `Offset`, 값의 `PathSegmentOffset`, 조각의 `Offset`.
- 왜 구역 기준인가: 결과 칸 `lpSectionBase` (`+0x28`) 가 **블롭 시작이 아니라 구역 시작** 이라서다 (결과서 3-H).

**한 칸 읽기** — 최소 블롭 B (2.9) 의 원소[0] `KeyOffset = 0x44`:

- 이 값은 **구역 기준** 이다. B 의 구역 (DLL 장) 은 블롭 `0x6c` 에서 시작한다 (목차[0] `Offset 0x6c`).
- 그래서 키 문자열 `comctl32.dll` 은 블롭 안 **`0xb0`** 에 있다 (run 문서 B 표 "구역 (→ `0xb0`)").
- 같은 원소의 `KeyLength = 0x18` 은 **바이트** 수다 — 12글자 × 2바이트 (UTF-16).

#### 해시 — 색인 탭 `[정적]`

해시는 처음이 아니다 — 이름을 숫자 하나로 바꿔 그 숫자로 바로 찾는 방법 ([run20 해설](../02_sprint/run20-kernel32-load-해설.md) 2.3 "이름 색인 카드함", [run37 해설](../02_sprint/run37-k32-import-order-해설.md) 2.4 "색인 탭").

- 구역 머리 `HashAlgorithm = 1` (`HASH_STRING_ALGORITHM_X65599` `[추측: 공개 이름]`).
- 식: 키를 **먼저 대문자로** 바꾸고, 글자마다 `h = h × 0x1003f + 글자` (32비트에서 넘침 버림) — `RtlpFindUnicodeStringInSection +0x100 IMUL R10D,R10D,0x1003f`.
  - `comctl32.dll` → `COMCTL32.DLL` → **`0xf1c4bc4f`**. 오케스트레이터 · flow-writer 가 파이썬으로 다시 셈했고, 사진 블롭의 `PseudoKey` 와 같다 `[관찰]`.
  - `comctl32.dll.mui` → `0xbbf34ea2` (사진 블롭과 같음 `[관찰]`).
  - 대소문자 구분 구역 (Flags 비트 0 꺼짐) 이면 대문자 변환 없이 같은 식.
- `HashAlgorithm = 0xFFFFFFFF` 면 해시를 **안 쓴다** — `PseudoKey` 를 안 보고 문자열만 비교 (`+0x88 CMP EAX,-1`). 엔진이 해시 계산을 피하고 싶으면 가능하지만, 윈도우가 만든 블롭과는 다르다.

**구역 안에서 찾는 법 셋** `[정적]`

| 방법 | 언제 | 누가 이 모양인가 |
|---|---|---|
| **해시표** — 칸 N 개, 칸 번호 = `PseudoKey % N`, 칸 = `{체인 길이, 체인 위치}`, 체인 = 원소의 구역 안 오프셋 | `SearchStructureOffset ≠ 0` (`+0x395`) | 시스템 기본 블롭 (구역 Flags 1, 칸 3개, 원소 12) `[관찰]` |
| **정렬 + 이분 탐색** (`+0x188 bsearch`) | 구역 Flags 비트 1 (`+0x135`) | 앱 블롭 (Flags 3, 원소 2) `[관찰]`, 최소 블롭 A · B |
| 처음부터 훑기 | 둘 다 아님 | — |

---

### 2.6 ★ 질문 4 — 책자엔 "세트 이름" 만, 동네 주소는 관리실이 붙인다

**새 말 — 조립 (assembly) · 조립 명부**

- **조립** = run 문서가 assembly 를 옮긴 말. 이름 · 버전이 붙은 **부품 한 벌** 로, WinSxS 안 **폴더 하나** 가 한 벌이다. 예: `Common-Controls 6.0.26100.9278` ↔ 폴더 `amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201`.
- **조립 명부** (roster) = 블롭 안 **세트 목록**. 0번은 자리 채움이라 못 쓴다. 명부 항목이 **조립 정보** (`0x6c`B) 를 가리키고, 조립 정보 `+0x50` · `+0x54` 가 **폴더 이름** 의 길이 · 위치다 `[정적]` (칸 이름은 `[추측]`).
- 비유: 책자 뒤쪽 **부품 세트 목록**. 한 줄에 "세트 이름 · 그 세트가 놓인 선반 칸 이름".

#### 뜻풀이를 적는 두 방법 — A (키만) · B (전체 경로)

로더는 **둘 다 읽을 수 있다** `[정적]`. 질문은 "윈도우는 어느 쪽으로 쓰나" 였다.

| | **A — 키만** (윈도우가 실제로 쓰는 방식) | **B — 전체 경로** (로더가 읽을 수 있는 방식) |
|---|---|---|
| 값 `Flags` | **2** (루트 붙임), 조각 0개 | **1** (조각이 파일 이름까지), 조각 1개 |
| 폴더 이름은 어디서 | 조립 명부 → 조립 정보 `+0x54` | 조각 문자열 안에 통째로 |
| `C:\WINDOWS\WinSxS\` | **ntdll 이 런타임에 붙임** | 블롭에 박힘 |
| 런타임에 더 타는 것 | 저장소 맵 (힙 할당) · `NtOpenKey` · **폴더 `NtOpenFile`** | 없음 |
| 윈도우 근거 | 사진 앱 블롭 (`comctl32.dll` → 플래그 2 · 조각 0 · 명부[2] Common-Controls 6.0.26100.9278) `[관찰]` + sxs `SxspDllRedirectionStringSectionGenerationCallback` (`loadFrom` 없으면 `플래그 \|= 2`, 조각 0) `[정적]` | 매니페스트에 `loadFrom=` 이 있을 때 이 모양 (`CDllRedir::ContributorCallback`) `[정적]` |

- 그래서 → **"블롭 안에 전체 경로냐 키만이냐" 는 닫혔다 — 윈도우는 키만** `[정적]`.
  - 블롭엔 **조립 폴더 이름** (상대) 과 **파일 이름** (키) 만 있다. 루트는 없다.
  - 전체 경로가 블롭에 들어가는 건 `loadFrom=` 일 때, 그리고 시스템 기본의 `C:\WINDOWS\system32\<이름>` **키** 쪽뿐이다 (그 값은 여전히 플래그 `0xa` · 조각 0 — 2.8).
- 이건 결과서가 **범위를 넓혀** 얻은 답이다. 로더 코드만 보면 "둘 다 읽힌다" 까지만 말할 수 있어서, sxs 가 **쓰는** 쪽을 한 번 더 열었다.

#### 경로를 이어 붙이는 차례 `[정적]` (`sxsisol_SearchActCtxForDllName`)

1. 플래그 비트 1 이면 **저장소 루트** 문자열 먼저 (`+0x2f0` 복사).
2. 조각들 (`+0x3b5` 복사).
3. 비트 0 이 꺼져 있으면 **물어본 이름** 그대로 (비트 3 이면 마지막 조각만, `+0x506` 복사).
4. 비트 2 면 환경변수 펼침 (`+0x3f9`).
5. "리다이렉트 됨" 표시.

- 복사 셋은 모두 `memcpy` 다 (같은 주소 이름 2개 — `memcpy` · `memmove`, `pdb_aliases.py --lookup ntdll.dll 0x166500`).
- **붙는 이름은 물어본 철자 그대로** 다. `COMCTL32.dll` 로 물으면 `…\COMCTL32.dll`. 키 비교는 대소문자를 무시하지만 결과엔 키의 철자를 안 쓴다 (결과서 미니 해석기).

**새 말 — 저장소 루트 · 저장소 맵**

- **저장소 루트** = 조립 폴더 앞에 붙는 "창고 동네 주소" — 보통 `C:\WINDOWS\WinSxS\<폴더>\`.
- **저장소 맵** = 한 번 찾은 루트 문자열과 **폴더 핸들** 을 담아 두는 표. PEB `+0x300` (프로세스 기본 문맥용) · `+0x310` (시스템 기본용) 에 매달린다 (2.10).
- 비유: 관리실의 **세트 창고 주소 수첩**. 처음 한 번 찾아가 적어 두고, 다음부턴 수첩을 본다.

#### 루트를 짓는 줄 (A 방식만) `[정적]`

아래 오프셋은 **call 자리** 다 (run 문서 "PE_emulator 쪽에서 쓸 것" · 결과서 질문 4 와 같은 표기).

```
sxsisol +0x215  RtlGetAssemblyStorageRoot
   +0x8a   …StorageMapAndRosterHeader   (처음만) 힙에 맵 만들기 → PEB +0x300 / +0x310
   +0xd7   RtlpResolveAssemblyStorageMapEntry
             ├─ …DefaultCallback  +0x169 NtOpenKey (…\SideBySide\AssemblyStorageRoots, 없어도 됨)
             │                    +0x1df RtlGetNtSystemRoot → C:\WINDOWS  ·  +0x240 "\WinSxS\" 복사
             └─ …ProbeAssemblyStorageRootForAssembly  +0x11a · +0x141 폴더 이름 붙이기
                                                      +0x26c NtOpenFile (폴더 열어 보기)
```

- `RtlGetNtSystemRoot()` 의 `C:\WINDOWS` 는 KUSD `0x7ffe0030` 에서 온다 (로비 공용 게시판).
- 폴더 열기 `NtOpenFile`: 접근 `0x100020`, 공유 3, 옵션 `0x21` (폴더 전용 + 동기). 폴더가 **없으면** 레지스트리 후보를 돌다가 (`NtEnumerateKey`) 다 실패하면 오류.
- `NtOpenKey` 는 없어도 된다 (`0xC0000034` · `0xC0000189`).

비유: 책자엔 "comctl32 → **Common-Controls 6.0 세트**" 와 그 세트의 **선반 칸 이름** 만 적혀 있다. 관리실이 로비 게시판에서 **동네 이름** (`C:\WINDOWS`) 을 읽고, "\WinSxS\" 와 선반 칸 이름을 붙여 주소를 완성한다. 그 선반 칸이 정말 있는지 문을 한 번 열어 보고 (폴더 `NtOpenFile`), 주소 수첩에 적어 둔다.

#### run87 의 짐작 — 맞았다

- run87 은 "폴더를 실제로 여는 쪽은 로더 = ntdll" 을 `[추측]` 으로 두었다.
- 이번 답: **ntdll `RtlpProbeAssemblyStorageRootForAssembly +0x26c NtOpenFile`** `[정적]`.

---

### 2.7 새 방법 — 사진 덤프 오프라인 읽기

#### 무엇을 했나

- **한 줄로**: 멈춘 순간 찍어 둔 메모리 사진을 **서버에서 파이썬으로 다시 뒤졌다.** WinDbg 는 안 썼다.
- 찾은 것: 블롭 머리 12바이트 `41 63 74 78 20 00 00 00 01 00 00 00` — "Actx" + `HeaderSize 0x20` + `FormatVersion 1`.
- 뒤진 사진 둘 (run 문서 "사진 덤프 대조"):
  - **run44 시험 사진 dast/02** — `…Snapshot5.vmem` (VMware 물리 메모리 8GB). test.exe 를 `LdrInitializeThunk` 입구에 세운 순간이지만, **짐 사진 8장과 다른 test.exe 실행** 이다 (Cid `0a58`).
  - **run44 짐 사진 P0** — `memory.dmp`.
- 결과 (시험 사진): 후보 약 200개.
  - DLL 안 임베디드 매니페스트류 110개
  - **comctl32 v6 를 쓰는 앱 블롭** 약 50개 — 다른 GUI 프로세스 것
  - **시스템 기본 블롭** (AMD64, `0x3084`B, 명부 7칸) 몇 개
- 짐 사진 P0 에도 시스템 기본 블롭 사본 3개 — 같은 결과.

비유: 앨범 (덤프) 을 WinDbg 로 **가게별로** 펼쳐 보는 대신, 동네 전체를 찍은 사진에서 **책자 표지 ("Actx")** 가 찍힌 곳을 모조리 찾아 읽었다.

#### 한계 둘

**새 말 — 물리 메모리 · 물리 연속 가정**

- 가게마다 보이는 번지 (가상 주소) 와 메모리 칩 위의 실제 자리 (물리 주소) 는 다르고, 한 **쪽** (페이지) 씩 따로 놓일 수 있다 `[추측]` (일반적으로 알려진 설명). 가상 → 물리 바꾸기 = [run59 해설](run59-init-leftovers-해설.md) 2.13 의 **페이지 걷기**.
- `.vmem` 은 **물리** 메모리 그대로다. 그래서 이번 읽기는:

1. **물리 연속 가정** — 블롭이 여러 쪽이면, 물리 메모리에서 **바로 다음 쪽이 진짜 다음 쪽** 이라고 믿고 읽었다.
   - 사본 셋 중 `0x1e4892000` · `0x1e260c000` 은 끝까지 맞았고, `0xf1f3d000` 은 **2쪽째부터 다른 데이터** 였다 (결과서).
   - 비유: 여러 장짜리 책자를 사진에서 읽는데, 바로 옆에 찍힌 종이가 다음 장이라고 믿었다. 어떤 사본은 둘째 장부터 남의 종이였다.
2. **어느 프로세스 것인지 모른다** — 가상 ↔ 물리 변환을 안 했다.
   - 그래서 **test.exe `25996d00000` 이 그 블롭인지는 `[확인 필요]`** 다.
   - 비유: 표지는 많이 찾았지만, 그 책자가 **어느 가게 벽** 에 걸린 것인지는 아직 짝을 못 지었다.

#### 그래서 태그는

- 결과서는 `[덤프 오프라인]` 이라고 적었다. run 문서는 `[관찰]` 로 쓰고, 처음 한 번 "사진 덤프 오프라인 읽기, 물리 연속 가정 — test.exe `25996d00000` 이 그 블롭인지는 `[확인 필요]`" 를 붙였다 (오케스트레이터 지시).
- 이 글에서 사진 블롭 내용에 `[관찰]` 이 붙어 있으면 **이 꼬리표가 늘 같이 있다** 고 읽으면 된다.

#### 미니 해석기 — 코드를 다시 쓴 것

**새 말 — 미니 해석기**: 결과서가 `RtlpLocateActivationContextSection` · `RtlpFindUnicodeStringInSection` · `sxsisol_SearchActCtxForDllName` 의 줄을 **파이썬으로 옮긴 것** 이다 (`_sast85/miniloader.py`, 커밋 안 됨).

- 실제 블롭 5개에 물려 보니 코드로 예상한 값과 **모두 맞았다** (결과서):
  - 앱 블롭 `0xd4c17000`: `COMCTL32.dll` → `…6.0.26100.9278…\COMCTL32.dll`, `comctl32.dll.mui` → ko-KR 폴더, `user32.dll` → 없음.
  - 시스템 기본 셋 + P0 셋: `comctl32.dll` · `C:\WINDOWS\system32\comctl32.dll` → `…5.82.26100.8328…\comctl32.dll`, `gdiplus.dll` → GdiPlus 1.1.26100.9444 폴더, `kernel32.dll` → 없음.
- **진짜 ntdll 이 같은 값을 낸다는 증명은 아니다.** 우리가 코드를 읽고 다시 쓴 것이다.
- 비유: 관리실 업무 지침서를 보고 **우리가 흉내 낸 관리실** 에 책자를 넣어 본 것. 진짜 관리실에 넣어 본 건 아니다 (→ Q162 5).

#### flow-writer 가 보탠 것 — test.exe 블롭 머리는 같다 `[관찰]`

run44 덤프 읽기 로그에 test.exe `PEB+0x308` 이 가리키는 `dq` 4칸이 **이미 있었다**.

| 사진 | 값 |
|---|---|
| K1 · K2 · K3 | `PEB+0x308` = 0 (아직 없음) |
| P0 · P1 · P1a | 주소 `25996d00000` 은 있으나 내용 `????` |
| P1b · P2 | `Magic` · `HeaderSize 0x20` · `FormatVersion 1` · **`TotalSize 0x3084`** · **`DefaultTocOffset 0xdc`** · `ExtendedTocOffset 0` · **`AssemblyRosterOffset 0x20`** · `Flags 0` |

- 줄: `ms/logs/04_baggage-read_P1b.txt:4469 ~ 4470` · `_P2.txt:4481 ~ 4482`.
- → 사진에서 찾은 **시스템 기본 블롭 머리와 같다**. 다만 머리 `0x20`B 만이라, 나머지 `0x3064`B 는 `[확인 필요]` (Q104 3).
- `????` → 보임 (P1a → P1b): 그때까지 이 프로세스에 그 페이지가 안 붙어 있다가, **처음 건드릴 때** 붙었다고 보면 맞는다 `[추측: 공유 섹션 페이지는 처음 건드릴 때 붙음]`.
  - 그럼 처음 건드린 때는 `P1a~P1b` — kernel32 를 올리는 **첫 리다이렉트 검색** 과 맞는다 `[추측]`.

---

### 2.8 ★ 질문 5 — 매니페스트 없는 exe 도 공통 책자를 편다

#### 공통 책자 안에 무엇이 있나 `[관찰]` (사진 덤프 오프라인)

- 크기 `0x3084`B, 이름표 "System Default Context".
- **조립 6개**:
  - `Microsoft.Windows.SystemCompatible 6.0.26100.1` (루트)
  - `IsolationAutomation 1.0.26100.5074` (`sxsoa.dll`)
  - `GdiPlus 1.1.26100.9444` (`GdiPlus.dll`)
  - **`Common-Controls 5.82.26100.8328` (`comctl32.dll`)**
  - `IsolationAutomation.ProxyStub` (`sxsoaps.dll` / `.tlb`)
  - `Common-Controls.Resources 5.82.26100.8117 ko-KR` (`comctl32.dll.mui`)
  - 명부는 7칸이다 (0번 자리 채움 포함).
- **DLL 구역 원소 12** = DLL 6개 × (이름 키 + `C:\WINDOWS\system32\<이름>` 키).
  - 이름 키의 값은 플래그 **2** (키만 방식).
  - 전체 경로 키의 값은 플래그 **`0xa`** (2 + 8, 조각 0).
  - 구역 Flags 1 (해시표), 칸 3개.
- **v6 항목은 없다.**

이게 run87 해설 2.7 "책자 재료" 에서 `[공백]` 으로 남긴 "공통 책자에 실제 무엇이 들어가나" 의 답이다.

#### 그래서 comctl32 는 — 단계별로

1. **근거** `[관찰]`: 공통 책자 DLL 장에 키 `comctl32.dll` 이 있고, 값은 Common-Controls **5.82** 세트를 가리킨다.
2. **근거** (결과서 미니 해석기 — 진짜 ntdll 아님): `comctl32.dll` (또는 `C:\WINDOWS\system32\comctl32.dll`) → **`C:\WINDOWS\WinSxS\amd64_microsoft.windows.common-controls_6595b64144ccf1df_5.82.26100.8328_none_87eede957a2ccddd\comctl32.dll`**.
3. **근거** `[정적]` (2.3): 경로가 나오면 비트 9 → 공용 창고가 "폴더 ≠ System32" 로 거절.
4. **그래서**: 매니페스트 없는 exe 라도 `comctl32.dll` 을 올리면 **v5.82 를 WinSxS 5.82 폴더에서 디스크 파일로** 올린다. System32 KnownDll 이 아니다.
5. **아직 확인 아님**:
   - 사진 블롭이 test.exe 의 블롭과 같은지는 **머리 `0x20`B 만** 확인했다 (2.7).
   - 진짜 ntdll 로 돌려 본 게 아니다 → **Q162 2** (매니페스트 없는 작은 exe 에서 `LoadLibrary("comctl32.dll")`).

#### 파일은 같다, 송장이 다르다

- 이 폴더의 `comctl32.dll` 은 `System32\comctl32.dll` 과 **바이트가 같다** — 레포 사본 SHA-256 둘 다 `67e04b3a…` (flow-writer 다시 셈).
- 그러니 **서수 #345 · #381 이 없는 문제는 그대로** 다. v5.82 는 v5.82 다.
- 다른 것은 둘:
  - 모듈 이름 · 경로 (`FullDllName`).
  - 연 syscall — KnownDll `NtOpenSection` 대신 폴더 · 파일 `NtOpenFile` · `NtCreateSection`.

비유: 공통 책자에도 "comctl32 는 **5.82 세트 선반 칸** 에서 가져와라" 한 줄이 있다. 그래서 공용 창고 (System32) 에서 꺼내 오는 게 아니라, **같은 물건을 다른 선반** 에서 가져온다. 물건은 같지만 **송장** (`FullDllName`) 과 **가져온 길** (syscall) 이 다르다.

#### v6 는 어디서 오나

- v6 는 **exe 매니페스트가 `Common-Controls 6.0.0.0` 을 요구해서 앱 블롭 (`+0x2f8`) 이 생길 때만** 온다.
- 이 빌드에선 정책으로 **`6.0.26100.9278`** 이 골라졌다 — 사진 앱 블롭 `[관찰]`. 고르는 **코드 길** 은 **Q164 ②**.
- 그래서 notepad 처럼 매니페스트로 v6 를 쓰는 exe 는 **앱 블롭이 없으면 v6 로 갈 길이 없다**. 그 블롭을 넣어야 "서수 없음 `0xc0000138`" 벽이 풀린다 `[추측: 서수 #345 · #381 이 v6 에만 있다는 원문 관찰에 기댐]`.

#### test.exe 에서는 `[추측: 사진 블롭이 test.exe 블롭과 같다는 가정 — 머리 0x20B 만 확인]`

- 리다이렉트 검색은 **탄다** — `PEB+0x308` 이 있으니까.
  - kernel32 를 올리는 `LdrLoadDll` 길 (`LdrpPreprocessDllName +0x622`).
  - 각 모듈 import 중 api-set 이 아닌 이름마다 (`LdrpLoadDependentModuleInternal +0x1426`).
- test.exe 의 모듈 이름 (`kernel32.dll` · `kernelbase.dll` · `dll1.dll` · `dll2.dll` …) 은 **12 키에 없다** → 모두 `0xC0150008` → 이름 그대로. **지금 흐름도와 같은 길** 이다.
- 그래서 저장소 맵 · 레지스트리 · 폴더 열기는 안 탄다 → PEB `+0x300` · `+0x310` 은 0 그대로. 사진 8장 모두 (null) `[관찰]` (run44, `04_baggage-read_P0.txt:1639 · 1641`, `_P2.txt:2516 · 2518`) 과 맞는다.

#### 지금 PE_emulator 는 — 틀림 (작게)

- "문맥 없음 → **System32 KnownDll** `COMCTL32` (v5.82)" 는 진짜와 **경로 · syscall 이 다르다**. 파일 바이트는 같다.
- `sample/` 의 test.exe 는 comctl32 를 안 쓰므로, 영향은 GUI 샘플 · notepad 부터다.

---

### 2.9 ★ 질문 6 — 엔진이 걸 가장 작은 책자

#### 판단 — 만드는 코드는 흉내 안 낸다 (run87 과 같은 결론)

- 블롭을 만드는 코드 (매니페스트 해석 · 정책 · 캐시) 는 흉내 내지 않는다.
- **미리 만든 바이트 덩어리를 읽기 전용으로 붙이고, PEB 칸에 주소만** 쓴다. 위치가 전부 오프셋이라 어느 주소든 된다 (2.5).
- comctl32 v6 가 필요하면 → **최소 블롭 B** 를 붙이고 `PEB+0x2f8` 에 주소. 다음 단계로 **A**. 둘 다 바뀌는 PEB 칸은 `+0x2f8` **하나** 다.

#### 최소 블롭 B — 전체 경로 한 줄 (`0x1dc`B)

run 문서엔 **바이트 하나하나** 의 표가 있다 (블롭 파일 `min_B.bin` 은 `ms/` 라 커밋 안 됨 → 표만 보고 다시 만들 수 있게). 여기선 덩어리만 보인다.

| 블롭 오프셋 | 크기 | 무엇 | 핵심 값 |
|---|---|---|---|
| `0x00` | `0x20` | 표지 (블롭 머리) | `TotalSize 0x1dc` · `DefaultTocOffset 0x4c` · `AssemblyRosterOffset 0x20` |
| `0x20` | `0x14` | 세트 목록 머리 (명부) | `EntryCount 1` |
| `0x34` | `0x18` | 명부[0] — 자리 채움 | `Flags 1` (번호 0 은 못 써서 둔다) |
| `0x4c` | `0x10` | 목차 머리 | `EntryCount 1` · `FirstEntryOffset 0x5c` · `Flags 2` |
| `0x5c` | `0x10` | 목차[0] | **`SectionId 2`** · `Offset 0x6c` · `Length 0x170` |
| `0x6c` | `0x2c` | DLL 장 머리 | `Magic 0x64487353` · **`Flags 3`** (대소문자 무시 + 정렬) · `ElementCount 1` · `HashAlgorithm 1` · `SearchStructureOffset 0` |
| `0x98` | `0x18` | 색인 줄 (원소[0]) | **`PseudoKey 0xf1c4bc4f`** · `KeyOffset 0x44` · `KeyLength 0x18` · `Offset 0x5c` · **`Length 0x114`** |
| `0xb0` | `0x18` | 키 | `comctl32.dll` |
| `0xc8` | `0x14` | 뜻풀이 (DLL 값) | **`Flags 1`** · `PathSegmentCount 1` · `PathSegmentOffset 0x70` |
| `0xdc` | 8 | 조각[0] | `Length 0xf8` · `Offset 0x78` |
| `0xe4` | `0xf8` | 경로 문자열 (124글자) | `C:\WINDOWS\WinSxS\amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201\comctl32.dll` |

- 끝이 정확히 `0x1dc` 다 — 비는 바이트가 없다 (run 문서).
- 목차[0] 의 `Offset 0x6c + Length 0x170 = 0x1dc` (run 문서 B 표의 "기준" 열).
- flow-writer 가 작은 파서로 **칸마다 `min_B.bin` 과 대조** 했다 — 전부 같다.
- 결과 (결과서 미니 해석기 — 진짜 ntdll 아님 `[확인 필요]`): `comctl32.dll` (대소문자 아무거나) → 위 경로 그대로. `user32.dll` → `0xC0150008`.
- 결과서가 두 최소 블롭이 `RtlpValidateActivationContextData` 의 만들 때 검사 규칙도 통과한다고 확인 `[정적]`.

비유: 관리사무소 (nt 엔진) 가 "comctl32 는 **이 주소** 에서" 한 줄짜리 **얇은 책자** 를 찍어 와 벽에 걸고, 안내판 `+0x2f8` 줄에 번지를 적는다.

#### 최소 블롭 A — 키만, 윈도우 방식 (`0x21c`B)

B 와 다른 곳만:

- 명부가 **2칸** — 1번이 조립 정보 (`0x84`, `0x6c`B) 를 가리킨다. 조립 정보 `Flags 0`, 폴더 이름 길이 `0xba`, 위치 `0x6c` (기준점 `0x84` → `0xf0`).
- `0xf0` 에 **폴더 이름** (`…6.0.26100.9278_none_3e0d1ba8e3303201`, `0xba`B + 패딩 2).
- 뜻풀이 **`Flags 2`**, 조각 0개. 원소 `AssemblyRosterIndex 1`.
- 런타임에 저장소 맵 (힙 할당) · `NtOpenKey` · **폴더 `NtOpenFile`** 이 응답돼야 한다 (2.6).
- 결과서 표와 파일이 한 칸 다르다: 명부[1] `AssemblyInformationLength` — 결과서 `0x126`, 파일 **`0x128`** (= `0x6c` + `0xbc`). 이 길에선 안 읽는 칸이라 결과는 같다.

#### B 먼저, A 나중 — 왜

| | B | A |
|---|---|---|
| 크기 | `0x1dc`B | `0x21c`B |
| 엔진이 더 응답할 것 | 없음 | 힙 할당 2번 · `NtOpenKey` · 폴더 `NtOpenFile` |
| 한계 | 명부를 읽는 다른 API (`QueryActCtx` 류) 엔 못 씀. 폴더가 정말 있는지 안 보고 파일 열기에서 바로 터짐. 루트 `C:\WINDOWS` 가 박힘 `[추측: 엔진의 시스템 루트가 C:\WINDOWS 가 아니면 B 문자열도 바꿔야]` | — |

- 시스템 기본 (`+0x308`) 까지 맞추려면: 진짜 시스템 기본 블롭 (`0x3084`B, 해시표 12원소) 을 **떠 와 그대로** 붙이는 게 가장 싸다. 떠 올 곳은 Q104 3.

#### 엔진이 지킬 것 넷 (run 문서 "엔진이 할 일")

1. **TEB `+0x2c8` 은 널이 아니어야** — 빈 스택을 가리키면 된다 (2.4).
2. **리다이렉트 → KnownDll 차례** 를 지킨다 (2.3).
3. 리다이렉트된 경로의 **파일이 엔진 파일 시스템에 있어야** 한다. v6 파일 = `ms/bin/…/WinSxS/amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201/comctl32.dll` (SHA-256 `17de1830…`, flow-writer 다시 셈).
4. 블롭은 **읽기 전용** 으로 붙인다 (run87).

PE_emulator 쪽 확인거리 (결과서 후보 4 — 우리 대기열 아님):

- 지금 엔진 TEB `+0x2c8` 이 널 아닌 빈 스택을 가리키나.
- `PEB+0x2f8` 에 앱 블롭을 넣을 때 `ProcessParameters.Flags` 비트 13 (`0x2000`, "매니페스트 있음") 도 같이 켜나. 진짜에선 이 비트가 csrss 의 "앱 문맥을 만들까" 를 정한다 (run87).

---

### 2.10 짐 지도 — 모르던 칸 셋이 닫혔다

#### PEB `+0x300` · `+0x310` — 주소 수첩 둘 `[정적]`

- `+0x300` `ProcessAssemblyStorageMap` · `+0x310` `SystemAssemblyStorageMap` (2.6 의 저장소 맵).
- 쓰는 줄은 **하나**: ntdll `RtlpGetActivationContextDataStorageMapAndRosterHeader +0x292 CMPXCHG.LOCK [R14],R13`.
  - R14 = `+0xd1 LEA R14,[RDX+0x300]` (플래그 비트 1 꺼짐) / `+0x21f LEA R14,[RDX+0x310]` (비트 1 켜짐, `+0xb5`).
    - 비트 1 이 켜지는 때가 "시스템 기본 책자에서 찾았을 때" 라는 건 `[추측]` (짐 지도 3.1 — 결과 칸 `ulFlags` 2 에서 온다고 봄).
  - 값 = `+0x266 RtlAllocateHeap(PEB.ProcessHeap, 0, EntryCount × 8 + 0x10)` 로 만든 맵.
  - `LOCK CMPXCHG` 라서 칸이 **비었을 때만** 넣는다. 비교에서 지면 `+0x29c` 되돌리기 · `+0x2b3 RtlFreeHeap`.
- 언제: `RtlGetAssemblyStorageRoot +0x8a` 를 **처음** 부를 때만 = 뜻풀이 플래그 비트 1 (키만 방식) 일 때만.
- test.exe 는 키를 못 찾아 **안 탄다** `[추측]` — 사진 8장 모두 (null) `[관찰]`.
- **엔진이 미리 넣을 칸이 아니다** — ntdll 이 알아서 만든다.
- 줄 (`+0x292` · 두 칸 가르는 갈래) 은 flow-writer `.asm` 이다. 결과서는 "LOCK 비교교환으로 넣는다" 까지였다.

#### 모듈 항목 `+0x88` `EntryPointActivationContext` `[정적]`

명부 한 줄을 만들 때 (`LdrpAllocateModuleEntry`) "이 건물의 책자" 칸을 채운다.

- **부모가 있는 길**: `+0xa7` 부모 `+0x88` 을 읽어, 0 이 아니면 AddRef 뒤 `+0xbb MOV [RBX+0x88],RSI`. 0 이면 `+0xb1 JZ` 로 안 쓴다.
- **부모가 없는 길**: `+0x15f LEA RCX,[RBX+0x88]` → `+0x166 RtlGetActiveActivationContext` (rva `0x6ef00`).
  - `+0x49` 에서 **0 을 쓰고**, 스레드 맨 위 틀이 있으면 (`+0x61 ~ +0x67`) `+0x86` 에서 그 틀의 문맥을 쓴다 (AddRef `+0x7d`).
  - `RtlGetActiveActivationContext` 를 부르는 곳은 **여기 하나뿐** 이고, 리다이렉트 길엔 없다 (리다이렉트는 TEB · PEB 를 직접 읽음, 2.4).
- 셋째 줄: 모듈 매니페스트 탐침 결과 — `LdrpFindDllActivationContext +0x192` (run67 이 이미 읽은 줄을 7.5 실마리에서 옮김).
- test.exe: 사진 `LoadReason` exe 4 · k32 4 `[관찰]` (run44) → 부모 없는 길 → 첫 스레드 맨 위 틀은 비어 있으니 **0 을 받았을 것** `[추측: TEB 활성화 스택 ActiveFrame 0 — 짐 지도 3.3]`. 사진 8장 모두 0 과 맞는다.
- 결과서는 "현재 스레드 맨 위 틀의 문맥을 복사" 까지였고, `+0x49` · `+0x86` · `+0xbb` 줄은 flow-writer 가 붙였다.

#### 숫자 (짐 지도 2장 · 7장)

| 곳 | 전 → 뒤 |
|---|---|
| PEB ntdll · 모름 | 22 → **24** · 65 → **63** |
| 모듈 항목 ntdll · 모름 | 38 → **39** · 27 → **26** |
| 유저 합 ntdll · 모름 | 130 → **133** · 200 → **197** |
| 구조체 아는 칸 (정확) · 모름 | 733 → **736** (679 → **682**) · 306 → **303** |
| 2.1 유저 `끝까지 0` 모름 | 132 → **129** |

#### 7.1 묵은 숫자 바로잡음 (정정)

- 7.1 의 "0 그대로 중 모르는 칸 **136**" 은 **run59 뒤 안 고쳐진 숫자** 였다.
- run62 (`TppWorkerpListLock`) · run66 (`ShimEngineCalloutSent`) · run70 (`MinimumStackCommit`) · run87 (`ActivationContextData`) 이 닫은 끝까지 0 칸 넷을 안 뺐다.
- 바로잡으면: run87 뒤 **132** (2.1 유저 `끝까지 0` 모름과 같다), run88 뒤 **129**.
- 같은 절 "아는 칸 중 끝까지 0 **295**" 도 같은 넷을 안 더했다 → run87 뒤 299, run88 뒤 **302**.
- 맞춰 보기 (짐 지도): 구조체 끝까지 0 847 − 모름 129 − 아는 칸 302 = **416**. run59 뒤 (847 − 136 − 295 = 416) 와 같다 — 그래서 셈이 맞다.
- 이전 run 문서가 틀린 게 아니라 **짐 지도 장부 셈** 이라, run 문서 맨 위 정정 줄은 안 넣었다.

#### 이미 아는 칸에 붙은 "읽는 쪽" 메모

- 3.1 `+0x2f8` · `+0x308` — 로더 리다이렉트 검색 ② · ③ 차례 (Q90 문맥 쪽).
- 3.3 TEB `ActivationContextStackPointer` — 리다이렉트 검색 맨 앞에서 **널 검사 없이** 읽음.
- 6장 부품: 활성화 문맥 찾기 함수들 — **짐을 안 놓는다** (읽기만, syscall 없음).

---

### 2.11 Q90 — 문맥 쪽은 닫혔다

- 질문: "ntdll 이 PEB `pShimData` · `SystemDefaultActivationContextData` 를 **읽는** 곳".
- 답 `[정적]` (결과서 — 내보낸 ntdll `.asm` 662개 안에서 찾은 범위, ntdll 전체 전수는 아님):

| 칸 | 읽는 곳 |
|---|---|
| `PEB+0x308` | `RtlFindActivationContextSectionString +0x134` (맨 앞 검사) · `RtlpFindNextActivationContextSection +0x131` · `+0x192` · `RtlpGetActivationContextDataStorageMapAndRosterHeader +0x215 ~ +0x21c` · `RtlpFindActivationContextSection_FillOutReturnedData +0x1d7` |
| `PEB+0x2f8` | 같은 넷의 짝 줄 (`+0x30` · `+0x119` · `+0xc7 ~ +0xce` · `FillOut +0xc7`) + `LdrpFindDllActivationContext +0x16c` (run67) |

- 읽어서 하는 일은 **DLL 이름 리다이렉트** 다 (위 전부).
- `pShimData` (`+0x2d8`) 를 읽는 쪽은 **이번 범위가 아니다** — 후보 이름만 `[공백]`: `LdrpInitializeProcess +0x69f`, `LdrpCheckComponentOnDemandEtwEvent +0x6c`, `RtlpHpOptIntoSegmentHeap +0x184`, `SbSelectProcedure` 넷, `SbObtainTraceHandle +0x22`. **Q90 은 이쪽만 남는다.**
- 662개 밖까지 다 훑는 일은 **Q165 ④**.

---

### 2.12 run87 해설에서 "run88 에서 다룸" 이던 자리 — 답

| run87 해설 자리 | 남겼던 질문 | 답 (이 글) |
|---|---|---|
| "조심할 곳" 1 · 2.12 끝 · 4절 마지막 줄 | 블롭 **안쪽 모양** (머리 · 목차 · 구역) | 표지 → 목차 → DLL 장 → 색인 줄 → 뜻풀이 `[정적]` (2.5) |
| 2.12 끝 | **어느 주소에 붙여도 읽히나** | **예** — 위치 칸이 전부 32비트 오프셋 `[정적]` (2.5) |
| 2.12 끝 · 3절 "큰 그림에서" 끝 | 로더가 세 문맥을 **보는 차례** | ① 스레드 → ② `+0x2f8` → ③ `+0x308` `[정적]` (2.4) |
| 2.7 끝 "책자 재료" · 4절 마지막 줄 | 공통 책자의 **실제 내용** | 조립 6개 · DLL 키 12, **v6 없음** `[관찰]` — 사진 덤프 오프라인 (2.8) |
| 2.13 ① | 블롭 안 폴더가 **전체 경로냐 키만이냐** | **키만** — 루트는 ntdll 이 붙임 `[정적]` (2.6) |
| 2.13 ② | 폴더를 **실제로 여는 쪽** | **ntdll** `RtlpProbeAssemblyStorageRootForAssembly +0x26c NtOpenFile` — 짐작 맞음 `[정적]` (2.6) |
| 2.13 ③ · 4절 표 6 · ② | "6.0.0.0" 이 **어느 버전으로 묶이나** | **`6.0.26100.9278`** `[관찰]` (사진 앱 블롭). 고르는 코드 길은 Q164 ② (2.8) |
| 3절 짐 지도 끝 | PEB `+0x300` · `+0x310` **쓰는 쪽** | ntdll `…StorageMapAndRosterHeader +0x292` (조건부, test.exe 안 탐) `[정적]` (2.10) |
| 4절 표 1 | 블롭 덤프 (사진 덤프 오프라인 읽기) | 머리 `0x20`B 는 test.exe 값과 같음 `[관찰]`, 몸통은 Q104 3 (2.7) |

run87 해설 맨 위에 이 절을 가리키는 "고쳐 읽기 (run88)" 한 줄을 붙였다.

---

### 2.13 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

| # | 무엇 | 한 줄 |
|---|---|---|
| ① | 태그 | 결과서 `[덤프 오프라인]` → `[관찰]` + "test.exe 블롭인지는 `[확인 필요]`" (오케스트레이터 지시, 2.7) |
| ② | 사진 이름 | 결과서 "P0 (run44/45 사진)" = **run44 짐 사진 P0** (dast/03, run45 는 사진을 안 찍음). "02_snapshot-test" = **run44 시험 사진 dast/02** — 짐 사진과 **다른 실행** |
| ③ | (보탬) test.exe 블롭 머리 | run44 로그 P1b · P2 `dq` 가 이미 머리 `0x20`B 를 담고 있었다 → 결과서 "동적 3" 의 머리 부분 닫힘 (2.7) |
| ④ | (보탬) PEB `+0x300` · `+0x310` 쓰는 줄 | `+0x292` · 두 칸 가르는 갈래 (`+0xb5 TEST CL,2` → `+0x215`) — 짐 지도 3.1 두 칸 닫음 (2.10) |
| ⑤ | (보탬) 모듈 항목 `+0x88` 쓰는 줄 | `+0x49` · `+0x86` · `LdrpAllocateModuleEntry +0xbb` — 짐 지도 3.5 한 칸 닫음 (2.10) |
| ⑥ | `sxsisol` 길이 검사 | CMP 는 **`+0x163`**, `+0x16c` 는 `JC` |
| ⑦ | `min_A.bin` 명부[1] 길이 | 결과서 `0x126` → 파일 **`0x128`** (안 읽는 칸, 2.9) |
| ⑧ | 복사 함수 | `alias:RtlCopyMemory+3` → 같은 주소 이름 2개 `memcpy` · `memmove`, 문서엔 `memcpy` |
| ⑨ | (보탬) 저장소 맵 푸는 길 call 자리 | `RtlpResolveAssemblyStorageMapEntry` 의 Callback · Probe · Insert 자리 |
| ⑩ | (보탬) Q90 `+0x2f8` 짝 줄 | `FillOutReturnedData +0xc7 MOV ECX,0x2f8` |
| ⑪ | (보탬) 대조 · 다시 셈 | 최소 블롭 B 를 파일과 칸마다 대조 (전부 같음), 해시 · 경로 길이, comctl32 사본 셋 SHA-256 (`67e04b3a` System32 = 5.82 `.8328` 폴더, `17de1830` 6.0 `.9278`) |
| ⑫ | (보탬) `????` → 보임 | 첫 읽기가 `P1a~P1b` 라는 짐작 `[추측]` (2.7) |

- 디버거가 붙어서 달라지는 점: 이번에 읽은 줄 안에서 디버거 여부를 보는 줄은 없다 `[정적: 읽은 줄 안에서]`. `+0x15aa LdrpLogInternal` 은 로더 로그 (켜졌을 때만 남김) 이지 디버거 검사가 아니다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 — ntdll · sxs PDB 줄 | … run87 | + run88 몫 (ntdll 22개 depth 0 새로, 새 지도 `_map/RtlDosApplyFileIsolationRedirection_Ustr_*` 깊이 6 · 106함수 / sxs 16개 읽기 전용) |
| 기준 — 근거 메모 | — | run88 줄: **사진 덤프 오프라인 읽기** (물리 연속 가정, 변환 없음), run44 로그 `dq` 4칸, PEB `+0x300` · `+0x310` 8장 |
| **그림 2** — `LdrpLoadDependentModuleInternal` [dll1 추정] · [dll2 추정] 아래 | (1) `+0x2ff` · (2) `+0x16ee` · (3) `+0x115c` · (4) `+0x1792` · (5) `+0x182c` | (1) `+0x2ff` · **(2) `+0x142b` `RtlDosApplyFileIsolationRedirection_Ustr`** (`jd0` · `je0`, `(정적)` 점선) · (3) `+0x16ee` · (4) `+0x115c` · (5) `+0x1792` · (6) `+0x182c`. 상자 글자에 "(ApiSet · SxS 리다이렉트)" |
| 그림 2 읽는 법 | — | run88 문단: 왜 넣었나 (comctl32 가 갈리는 자리 + test.exe 도 import 마다 `+0x308` 을 읽음), 안쪽 사슬 · `LdrpCheckKnownDllFullPath` 는 상자 대신 글로 |
| 2.2 csrss 절 끝 | (run87) 칸 둘을 csrss 가 씀 | + "→ (run88) 로더는 DLL 이름마다 이 두 칸을 스레드 문맥 다음 차례로 읽는다. 블롭엔 폴더 이름 · 파일 이름만, 루트는 ntdll" |
| 2.5 `LdrpLoadKnownDll` 본문 | 비트 9 뜻 모름 | **`[정적]` 비트 9 = SxS 리다이렉트로 전체 경로가 됨** → `LdrpCheckKnownDllFullPath` 폴더 비교 → `0xc0000135` |
| 2.6 `LdrpLoadDependentModuleInternal` 트리 `+0x142b` 줄 | "SxS 리다이렉트" | + "(run88: ② KnownDll 보다 먼저)". 그리고 새 절 **"SxS 리다이렉트"** (트리 · 결과 상태 · TEB 널 금지 · 블롭 모양 · 키만 · 사진 블롭 · test.exe 에선 · Q90) |
| 2.6 run23 `[추측]` "부모의 매니페스트 기준" | `[추측]` | 방향은 맞음 (찾는 차례 첫째 = 스레드 틀) — 틀 올리기 줄은 `[추측]` 그대로 (Q165 ②) |
| 2.6 `LdrpAllocateModuleEntry` 절 | — | + 항목 `+0x88` 쓰는 줄 (2.10) |
| 4장 | run23 "이름 · 뜻 못 정한 것" (지역 flags 비트들) · run87 동적 · 정적 줄 | run23 줄 **일부 해결** ("SxS 로 바뀜" · "전체 이름" 짐작 맞음, 공개 이름은 `[추측]`), run87 줄에 → run88 표시. run88 새 줄 셋: 동적 (Q104 · Q83 · Q162) · 정적 (Q164 · Q165 · Q90) · PE_emulator · 보탠 것 · 셈 정정 |
| 4.1 그림 2 `LdrpLoadDependentModuleInternal` 칸 | (1) ~ (5), `+0x142b` SxS 는 상자 없음 | (1) ~ (6), 새 (2) 의 근거 · 안 타는 때, 멈춤점 — ret `ntdll+0x22edb` (`+0x142b`) 에서 `r eax` (test.exe 는 `c0150008` 기대) → Q104 |
| 5장 | … run87 | + run88 (run 표 · 요청서 번호 표) |

태그 · 판단이 바뀐 곳:

- flow.md 2.5 "비트 9" — 뜻 모름 → **`[정적]`** "리다이렉트로 전체 경로가 됨".
- run23 지역 flags `0x200` "전체 이름" · `0x4` "SxS 로 바뀜" 짐작 → **읽는 쪽으로 맞음** `[정적]`. 공개 이름은 그대로 `[추측]`, ESI → 지시서 `+0x20` 길은 `[추측]` (Q165 ①).
- run87 "폴더를 여는 쪽 = 로더" `[추측]` → **`[정적]`** (ntdll `+0x26c NtOpenFile`).
- run87 "공통 책자 실제 내용" `[공백]` → **`[관찰]`** (사진 덤프 오프라인, test.exe 블롭인지는 `[확인 필요]`).

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- **모르던 칸 셋 닫음** (마흔넷째판) — 3.1 PEB 2줄 (`+0x300` · `+0x310`, 조건부 안 탐), 3.5 모듈 항목 3줄 (`EntryPointActivationContext`). 자세히는 2.10.
- 3장 줄임 표에 새 기호 **`②R…`** — SxS 리다이렉트 검색 경로 (import 길 · LoadLibrary 길 둘).
- 4장 짝 1줄 (저장소 맵 항목의 조립 폴더 핸들 ↔ 파일 객체), 5장 ② 1줄, 6장 부품 1줄 (활성화 문맥 찾기 — 짐 안 놓음).
- 7.1 셈 정정 (2.10), 7.2 · 7.5 해결 표시.

### 큰 그림에서

- **요청 29 가 한 줄로 이어졌다** (run87 + run88):
  - 커널이 쪽지 (매니페스트) 를 찾아 둠 → 부모가 등록 서류에 쪽지 칸을 채움 → **csrss 책자 담당이 책자를 찍어 걸고 안내판에 번지** (run87) → 첫 직원 깸 → **관리실이 DLL 이름마다 책자를 펴 봄** → 찾으면 WinSxS 경로 → 공용 창고가 거절 → 디스크에서 올림 (run88).
- **test.exe 흐름도의 길은 그대로다** `[추측]` — 키가 없어 "리다이렉트 없음". 하지만 그림 2 에 "**import 마다 공통 책자를 읽는 자리**" 가 대표 지점으로 올라갔다.
- **PE_emulator 의 할 일이 분명해졌다**:
  - TEB `+0x2c8` 을 빈 스택에, 리다이렉트 → KnownDll 차례, v6 가 필요하면 최소 블롭 B + `PEB+0x2f8`.
  - 지금의 "System32 KnownDll v5.82" 는 **작게 틀림** — 경로 · syscall 이 다르다 (파일은 같다).
- **다음**: Q152 (요청 30 — .NET exe 시작 길).

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 남은 것은 다섯 무리다.

- **실행해 봐야 아는 것** → 결과서 "동적으로 확인할 것" 번호 그대로.
  - test.exe 로 볼 수 있는 것 (3 · 4 의 test.exe 몫) → **Q104**, 6 → **Q83**.
  - comctl32 를 쓰는 프로세스나 작은 exe 가 필요한 것 (1 · 2 · 5 · 4 나머지) → **Q162**. test.exe 는 comctl32 를 안 쓴다 `[소스]`.
  - 4 를 둘로 나눈 까닭 (run 문서 판단 메모): test.exe 에선 저장소 맵 · 레지스트리 · 폴더 열기가 **안 서는 게 기대값** 이라, "안 서는지" 만 test.exe 로 볼 수 있어서다.
- **정적** → **Q164** ① ② · 새 **Q165** ① ~ ④.
- **Q90 남은 쪽** → `pShimData` 읽는 곳.
- **PE_emulator 쪽 확인거리** (우리 대기열 아님) → 2.9 끝.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 3 test.exe 공통 책자 **몸통** (`0x3064`B) 이 사진 블롭과 같나 | 사진 읽기가 물리 메모리 · 변환 없음이라 어느 프로세스 것인지 못 짝지음. 머리 `0x20`B 만 run44 로그로 같음 `[관찰]` | **Q104 3** — `PEB+0x308` (`25996d00000`) 을 `.writemem` 으로 `0x3084`B, 또는 P0 `memory.dmp` 를 가상 → 물리 변환해 오프라인으로 |
| 4 (test.exe 몫) 리다이렉트 검색 결과가 정말 `0xc0150008` 이고, 저장소 맵 · 레지스트리 · 폴더 열기 셋이 **안 서나** | 실행 때 값. 셋이 안 서야 맞다 `[추측]` | **Q104 4** — `RtlDosApplyFileIsolationRedirection_Ustr` 입구 `rdx` · `+0x489` `eax` · `LdrpLoadDependentModuleInternal+0x142b` `eax` |
| 6 PEB 쓰기 **순간** | run87 과 같음 | **Q83** |
| 1 notepad 류 (v6) 리다이렉트 결과 문자열이 `…6.0.26100.9278…\comctl32.dll` 인가 | test.exe 는 comctl32 를 안 씀 | **Q162 1** — `+0x142b` 직후 `[rsp+0x140]` · `eax` |
| 2 KnownDll 거절 (`LdrpLoadKnownDll` `0xc0000135`, `LdrpCheckKnownDllFullPath` `al=0`), 매니페스트 없는 작은 exe 의 `LoadLibrary("comctl32.dll")` → **WinSxS 5.82 경로** 인가 (질문 5 의 핵심) | 같음 + 진짜 ntdll 로 안 돌려 봄 | **Q162 2** |
| 4 나머지 — `NtOpenKey` 상태, 폴더 `NtOpenFile` 상태 · 경로, PEB `+0x300` / `+0x310` 이 채워지는 순간 (A 방식 흉내용 실제 값) | A 방식 길은 test.exe 가 안 탐 | **Q162 4** |
| 5 최소 블롭 B 를 **진짜 ntdll** 에 먹이면 `FullDllName` 이 v6 경로인가 | 미니 해석기는 진짜 ntdll 아님 | **Q162 5** (dast 가 정함, 사용자 결정 몫) |
| ① sxs 가 조립 명부 · 조립 정보 칸을 **쓰는** 줄 — 폴더 이름 (`+0x54`) · 플래그 `0x10` 이 정해지는 곳 | 이번엔 로더가 읽는 칸과 실제 블롭으로 모양만 굳힘. 쓰는 줄은 DLL 구역만 읽음 | **Q164 ①** |
| ② "6.0.0.0 → 6.0.26100.9278" 을 고르는 **코드 길** (`SxspParseApplicationPolicy` · 정책 폴더) | 고른 값은 사진으로 봤지만 길은 안 읽음 | **Q164 ②** |
| ① ESI (지역 flags) → 새 작업 지시서 `+0x20` 길 (`+0x94a` → R15D → `+0x10ec`) | 결과서 `[추측]`, 끝까지 안 따라감 | **Q165 ①** |
| ② 부모 항목 문맥 틀 올리기 (`+0x71 ~ +0x1d3`) — 부모 `+0x88` 이 0 일 때 틀을 안 올리나 · 빈 틀인가 | 이번 범위 밖. flow.md 2.6 `[추측]` 이 여기 걸림 | **Q165 ②** |
| ③ `_ACTIVATION_CONTEXT` 개체 칸 (`+0x18` = 블롭 말고는 `[공백]`), 틀 특수 값 `-3` · `-4` 이름, 상태 값 `0xC0150001` · `0xC0150003` · `0xC0150008` 이름 | PDB 에 정의 없음 | **Q165 ③** |
| ④ ntdll **전체** 에서 PEB `+0x2f8` · `+0x308` 을 읽는 줄 | 이번 목록은 내보낸 `.asm` 662개 안 | **Q165 ④** |
| `pShimData` (`+0x2d8`) 를 읽는 곳 · 그 값으로 하는 일 | 이번 범위 밖 — 후보 이름만 `[공백]` | **Q90** |
| PE_emulator 엔진 TEB `+0x2c8` 값, `ProcessParameters.Flags` 비트 `0x2000` 을 같이 켜나 | PE_emulator 쪽 사정 | PE_emulator 확인거리 (2.9) |

- 참고 (오프라인 변환): run 문서는 "가상 ↔ 물리 **변환 도구가 아직 없다**" 고 적었다. 다만 run59 때 임시 스크립트 (scratchpad, 커밋 안 함) 로 K1 덤프에서 test.exe `DirectoryTableBase` 로 4단계 페이지 걷기를 **한 번 해 본 적은 있다** ([run59 문서](run59-init-leftovers.md) "방법" 질문 17). 어떻게 할지는 오케스트레이터가 정한다 (run 문서 "도구 메모").

---

## 5. 더 읽기

- run 문서: [`run88-sxs-redirect.md`](run88-sxs-redirect.md) — 맨 위 한 줄 결론, "PE_emulator 쪽에서 쓸 것" (요청 29 전체 결론 · 원문 질문이 어디서 답해졌나 · 질문마다 표 · **`ACTIVATION_CONTEXT_DATA` 최소 칸 표 A ~ G** · comctl32 v6 최소 조건 A / B · 해시 · **최소 블롭 B 바이트 표** · 최소 블롭 A · 시스템 기본 문맥만 있을 때 · Q90 닫힘), 호출 지점 표, 호출 관계 그림, 사진 덤프 대조, 확인된 것, 결과서와 다르게 적은 곳 · 보탠 것 ① ~ ⑫, 정정 (짐 지도 7.1), 아직 확인 필요
- 결과서: [`_workspace/sast/85_sxs-redirect_result.md`](../../_workspace/sast/85_sxs-redirect_result.md) — 한 장 지도, 질문 1 ~ 6, 표 3-A ~ 3-I (3-H 결과 칸 `ACTCTX_SECTION_KEYED_DATA`, 3-I 런타임에 ntdll 이 만드는 것), 최소 블롭 A · B, 사진 덤프 대조 · 미니 해석기, 동적으로 확인할 것 1 ~ 6, Q90 닫힘. (사진 이름 "P0 (run44/45 사진)" 은 run44 짐 사진 P0 로, `sxsisol +0x16c CMP` 는 `+0x163` 으로, A 명부[1] 길이 `0x126` 은 `0x128` 로, `[덤프 오프라인]` 은 `[관찰]` + 꼬리표로 읽기)
- 요청서: [`_workspace/sast/85_sxs-redirect.md`](../../_workspace/sast/85_sxs-redirect.md) ("`[관찰]` run45" 사진은 run44 사진으로 읽기)
- flow.md: [그림 2](flow.md) (`LdrpLoadDependentModuleInternal` 아래 (2) `+0x142b`) · 그림 2 읽는 법 run88 문단 · 2.2 csrss 절 끝 · 2.5 `LdrpLoadKnownDll` 비트 9 · **2.6 "SxS 리다이렉트" 절** · 2.6 `LdrpAllocateModuleEntry` 절 · 4장 run88 줄 셋 · 4.1 그림 2 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.1 PEB (`+0x2f8` · `+0x300` · `+0x308` · `+0x310`) · 3.3 TEB `ActivationContextStackPointer` · 3.5 `EntryPointActivationContext` · 3장 줄임 표 `②R…` · 4장 짝 · 6장 부품 · 7.1 (셈 정정) · 7.2 · 7.5
- 짝 바퀴: [run87 문서](run87-sxs-create.md) ("→ run88" 줄들) · [run87 해설](run87-sxs-create-해설.md) (만드는 쪽 — 책자 담당 · 제본 기계 · 공통 / 전용 책자)
- 앞 이야기:
  - [run23 해설](../02_sprint/run23-load-dependent-해설.md) 2.4 "맨 이름 / 전체 이름" · 2.5 "곁가지 — SxS 리다이렉트" — 이번 2.3 · 2.4 의 앞
  - [run21 해설](../02_sprint/run21-load-dll-internal-해설.md) 2.4 — KnownDll 길 · 파일 길
  - [run54 해설](run54-teb-fill-해설.md) 2.10 — TEB 활성화 문맥 스택 (`+0x290` · `+0x2c8`)
  - [run67 해설](run67-kb-heap-init-해설.md) 2.5 — 매니페스트 탐침, `LdrpFindDllActivationContext`
  - [run44 해설](run44-baggage-snap-해설.md) 2.2 · 2.8 — 스냅샷 · 덤프 · 사진 8장
  - [run59 해설](run59-init-leftovers-해설.md) 2.13 — 덤프에서 페이지 걷기
