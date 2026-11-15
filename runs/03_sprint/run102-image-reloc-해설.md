# run102 image-reloc — 해설

> 이 글은 [run102 문서](run102-image-reloc.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q62a** — Q62 의 (a)·(b)·(f): 커널이 이미지 안 번지수 (`.reloc`) 를 미리 고치나, `NtMapViewOfSection` 이 `0` / `0x40000003` 을 정하는 줄, `PspAllocateProcess +0x20b3 ~ +0x20ce` 보정, 안 읽은 여섯 함수의 `TransferAddress` 쓰기 (`_workspace/plan.md`, 이번에 **끝**)
  - Q62 가 커서 2026-11-15 에 **Q62a · Q62b** 로 나눴다. 이번이 앞쪽이다.
  - 남은 것: 동적 → **Q61**, 정적 → **Q188 ①~⑥** (새로), **Q62b** (다음 바퀴 run103 — 공유 길·프로토 PTE).
- 근거:
  - run 문서 [`run102-image-reloc.md`](run102-image-reloc.md) (가장 중요)
  - 요청서 [`_workspace/sast/98_image-reloc.md`](../../_workspace/sast/98_image-reloc.md)
  - 결과서 [`_workspace/sast/98_image-reloc_result.md`](../../_workspace/sast/98_image-reloc_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/03_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴는 **정적만** 했다. VM 은 안 켰다.
  - ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음).
  - 값 대조는 **run44 사진 로그를 다시 읽은 것**뿐이다 (2.10). 새로 돌린 실행은 없다.

이미 푼 말은 **링크만** 건다. 비유도 그대로 이어 쓴다.

- 커널 = **관리사무소**, 시스템 콜 = **신청 창구**, ntdll = 가게 안 **관리실** → [run38 해설 "비유"](../02_sprint/run38-map-module-해설.md#비유--그대로-이어-쓴다)
- 이미지 섹션 = **조립식 건물 세트**, 매핑 = 세트를 땅에 **갖다 놓기** → [run9 해설](../02_sprint/run9-create-user-process-해설.md#이미지-섹션) · [run10 해설](../02_sprint/run10-allocate-process-해설.md#매핑-올리기)
- 컨트롤 영역 = 설계도 서류함의 **서랍** → [run31 해설 2.5](../02_sprint/run31-image-section-해설.md#새-말--컨트롤-영역)
- 프로토 PTE = **배치표** ("세트를 짤 땐 땅에 아직 안 올린다") → [run31 해설 2.8](../02_sprint/run31-image-section-해설.md#새-말--프로토-pte)
- `TransferAddress` = 배치표 요약 카드 맨 윗줄 **"첫 출근 방"**, ASLR, 델타 → [run31 해설 2.9 · 2.10](../02_sprint/run31-image-section-해설.md#210-재배치-델타--다른-번지에-놓을-거면-그-칸을-고쳐-쓴다)
- **재배치** = 다른 번지에 놓였으니 안에 적힌 **번지수를 고쳐 쓰기**, `.reloc` = **번지수 적힌 안내판 위치 목록**, `RELOCS_STRIPPED` → [run38 해설 2.9](../02_sprint/run38-map-module-해설.md#29-실제로-펜을-드는-곳은-두-단계-아래-정적)
- `0x40000003` = 창구 대답 "**놓았어요. 그런데 원래 자리가 아닌 곳에 놓였어요**" → [run38 해설 2.6](../02_sprint/run38-map-module-해설.md#26-창구의-대답-가르기-정적)
- `_SEGMENT.BasedAddress` = 세트의 **기준 주소**, 비트맵 = 관리사무소의 **구획 배정표**, 주문서 체크박스 → [run73 해설 2.2 · 2.3](run73-image-base-select-해설.md#23-그래서-dll1dll2-는-구획-배정표에서-따로-받는다)

### 한 줄로 먼저

> 관리사무소는 세트를 짤 때 **상자를 다 열어 번지수를 고치지 않는다.**
> 상자마다 "**이 상자는 목록 몇 번 묶음을 보고 고칠 것**" 쪽지만 붙여 두고,
> **그 상자를 처음 열 때** (page-in) 쪽지대로 고친다.

- (여기서 "상자" 는 세트를 이루는 **쪽 (페이지)** 이다. 짐 지도 (`baggage.md`) 의 "짐" 과는 다른 말이다.)
- 쪽지 붙이는 사람 = **`MiRelocateImage`** (세트 짤 때) `[정적]`.
- 실제로 펜을 드는 사람 = **`MiPerformFixups`** (← `MiRelocateImagePfn`) — 상자를 처음 열 때, 그리고 쪽지 붙일 때 이미 열려 있던 상자만 바로 `[정적]`.
- 창구 대답 `0` / `0x40000003` 은 "**놓인 번지 = 기준 주소인가**" 한 줄로 정해진다. 설계도 (헤더) 의 `ImageBase` 와는 안 맞대 본다 `[정적]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 이야기는 **세 자리**에 걸쳐 있다 (①②③). 하나씩 표시했다.

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess
 ├─ +0x93c MmCreateSpecialImageSection → … MiCreateNewSection
 │     └─ +0x3da MiRelocateImage                 ← ① 세트 짤 때: 쪽지 붙이기
 ├─ +0xc36 PspAllocateProcess
 │     ├─ +0x991 MmGetSectionInformation           (요약 카드 복사)
 │     ├─ … MiMapProcessExecutable → MmMapViewOfSectionEx
 │     │                                          ← ③ 창구 대답 0 / 0x40000003
 │     └─ +0x20b3 ~ +0x20ce 보정                  ← ② 진입점 두 번째 보정
 └─ 첫 스레드 출발
ntdll LdrInitializeThunk → LdrpInitializeProcess
 ├─ +0x1430 exe 재배치 문 (run38)
 └─ DLL 들이기: NtCreateSection  → (커널) MiRelocateImage   ← ① 같은 길
               NtMapViewOfSection → 창구 대답            ← ③
               (0x40000003 이면) LdrpRelocateImage (run38)
→ NtContinue 은퇴 → main

(아무 때나) 쪽을 처음 만질 때 → MiValidateInPage +0x233   ← ① 실제로 고치기
```

- 맨 아래 줄은 **시간 순서 밖**이다. 어느 스레드든, 그 쪽을 처음 만지는 순간 일어난다 (run 문서: "페이지 폴트 길 — 지도 대표 지점 밖").

### 무엇이 궁금했나

- **(f, 가장 중요)** 커널이 이미지 안 번지수 (`.reloc`) 를 **미리 다 고쳐 두나**, 아니면 기준 주소만 정하고 나중에 고치나? DLL 세트도 그 길을 지나나? 창구 대답 `0` / `0x40000003` 을 정하는 줄은 어디인가?
- **(b)** `PspAllocateProcess +0x20b3 ~ +0x20ce` 보정은 `MiRelocateImage` 델타와 무슨 사이인가? (같은 걸 두 번 더하나?)
- **(a)** 안 읽은 여섯 함수에도 `TransferAddress` 를 쓰는 줄이 있나?

### 바퀴 전에 알던 것

- `[정적]` (run31) `MiCreateNewSection +0x3da` 가 `MiRelocateImage` 를 부르고, 그 안 조각 하나가 `TransferAddress` 에 델타를 더한다. **함수 전체는 안 읽었다.**
- `[정적]` (run38) ntdll 쪽 펜 `LdrpCompleteMapModule +0x31b LdrpRelocateImage` 는 창구 대답이 **정확히 `0x40000003`** 일 때만 든다.
- `[추측]` (run38) "보통 DLL 은 관리실이 안 고칠 수도 있다" — 관리사무소가 다 고쳐 두면. 근거 약함.
- `[정적]` (run72 · run73) 창구가 `0x40000003` 을 쓰는 건 `MiMapViewOfImageSection +0xc0a`. DLL 기준 주소는 구획 배정표에서 따로 받는다.
- `[공백]` (run73 · flow.md 2.6) **커널이 메모리 속 헤더 `ImageBase` 를 고치는 줄**은 못 찾았다.
- `[추측]` (flow.md 2.1) test.exe 는 `MiRelocateImage` 길을 탈 것이다.

---

## 2. 찾은 것

### 2.1 한눈에

섹션 (세트) 짤 때 — 부모 cmd 스레드:

```
MiCreateNewSection +0x3da  MiRelocateImage
 ├─ +0x2e6  MiSelectImageBase        새 기준 주소 고르기
 ├─ +0x3aa  (메모리 복사)             .reloc → 기록 풀 블록 (태그 MmRe)
 ├─ +0x4cb  MiScanRelocationPage     블록마다: 쪽 칸에 블록 주소만 적음 (안 고침)
 │  +0x581  헤더 쪽 칸에 1             ImageBase 칸 표시
 └─ 조각 FUN_140b24658
      +0x55   _SEGMENT.BasedAddress = 새 기준
      +0x65   TransferAddress += 델타
      +0x106 / +0x11d  MiWalkEntireImage
                └─ +0x4cd MiRelocateImagePfn → +0x141 MiPerformFixups   (올라와 있던 쪽만)
      +0x143  ImageFlags |= 4
```

쪽이 들어올 때 — 어느 스레드든:

```
MiValidateInPage +0x233  MiRelocateImagePfn
 └─ +0x141  MiPerformFixups          ★ 실제로 번지수 고치기
      ├─ +0x131 MiApplyCompressedFixups
      ├─ +0x2c5 MiApplyRawFixups
      └─ +0x15f MiApplyStraddleFixups  (쪽 경계에 걸친 항목)
```

- 모두 `[정적]` (run 문서 "호출 관계").

새 말 둘.

- **page-in (페이지 들어오기)** = 그 쪽을 처음 만질 때, 파일 (디스크) 에서 메모리로 **실제로 가져오는 순간**.
  - 비유: 창고에 쌓아 둔 상자를 **처음 뜯는 순간**.
  - "처음 만질 때 일어난다" 는 건 일반적으로 알려진 설명이다 `[추측]`. 이번 코드로 본 건 "페이지가 파일에서 들어올 때 `MiValidateInPage` 가 부른다" 까지다 `[정적]`.
- **PFN** (`MiRelocateImagePfn` 이름 끝) = 실제 메모리 쪽 하나하나에 붙은 번호로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 창고 선반 번호. 그래서 이름은 "선반에 올라온 상자 하나를 고치기" 쯤으로 읽힌다 `[추측: 이름]`.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 상자를 처음 열 때 고치러 가는 줄

`MiValidateInPage +0x233` (ret `+0x238`) → `MiRelocateImagePfn`

- **누가**: `MiValidateInPage` 가 (페이지 폴트 길, 쪽이 파일에서 들어올 때)
- **어디서**: `+0x233`, 쪽마다 도는 고리 안에서
- **누구를**: `MiRelocateImagePfn` — 그 쪽 몫의 쪽지를 꺼내 `MiPerformFixups` 에게 넘기는 함수
- **무엇을 위해**: 방금 들어온 쪽 안의 번지수를 **새 기준 주소에 맞게** 고치려고.
- 거르는 문: 재배치 기록 포인터가 널이 아닐 때 (`+0x14e` · `+0x152`) 켜지는 비트로 거른다 `[정적]`. 그 조건을 끝까지 따라가진 못했다 `[공백]` (4절, Q188 ②).

#### 한 줄 읽기 ② — 창구 대답을 가르는 줄

```
MiMapViewOfImageSection +0x42a  CMP RCX,[RBP-0x60]
MiMapViewOfImageSection +0x42e  JNZ → +0xc0a
MiMapViewOfImageSection +0xc0a  MOV [RSP+0x60],0x40000003
```

- **누가**: `MiMapViewOfImageSection` 이 (창구 `NtMapViewOfSection` 안쪽)
- **어디서**: `+0x42a`
- **무엇과 무엇을**: `RCX` = 놓인 시작 번지 (정확히는 뷰 시작 − `SectionOffset` 낮은 32비트, `+0x424 ~ +0x427`) 와 `[RBP-0x60]` = **`_SEGMENT.BasedAddress`** (세트의 기준 주소) 를 맞대 본다.
- **같으면**: `+0x42e JNZ` 를 안 타고, 처음값 `0` (`+0x353`) 그대로 `+0x64c` 에서 돌려준다.
- **다르면**: `+0xc0a` 로 뛰어 대답 칸에 `0x40000003` 을 쓴다.
- **무엇을 위해**: 관리실 (ntdll) 에게 "원래 자리에 놓였나" 를 알려 주려고. 이게 run38 의 펜 드는 문 하나뿐인 그 신호다.

### 2.3 ★ `MiRelocateImage` 는 쪽지만 붙인다 — 열 단계

run 문서 표 (결과서 1-1) 를 줄였다. 마지막 열이 핵심이다: **이미지 쪽을 직접 고치나?**

| 순서 | 하는 일 | 쪽을 고치나 | 비유 |
|---|---|---|---|
| ① 일찍 끝 | 세 경우 (`ForceCollision` 켜짐 · x86/x64 인데 `RELOCS_STRIPPED` · x86/x64 인데 `DYNAMICBASE` 없음) 면 `0` 돌려주고 아무것도 안 바꿈 (`+0x6c6 XOR EAX,EAX`) | 아니오 | "옮기면 안 되는 세트" 는 그냥 돌려보냄 |
| ② 검사 | 재배치 디렉터리 크기·범위. 둘 다 0 이면 "항목 없음" (오류 아님) | 아니오 | 목록이 붙어 있나 보기 |
| ③ 기록 풀 블록 | `+0x1a4 MiAllocatePool` (태그 `MmRe`). 크기 = 디렉터리 크기 + `0x68` + 쪽 수 × 8. 앞부분이 쪽마다 칸 | 아니오 | 관리사무소 **공책** 한 권 마련. 앞쪽에 상자마다 쪽지 칸 |
| ④ 읽기 | `+0x1fc MiMapImageInSystemSpace` · `+0x24f MiParseImageLoadConfig` | 읽기만 | 설계도 부록 읽기 |
| ⑤ 기준 주소 | `+0x2e6 MiSelectImageBase`. 기록 `+0x28` = 새 − 옛 `BasedAddress` | 아니오 | 구획 고르고, "얼마나 밀렸나" (델타) 를 공책에 |
| ⑥ `.reloc` 걷기 | `+0x3aa` 복사, 고리 `+0x3ed ~ +0x4e6`, `+0x4cb MiScanRelocationPage`. **"이 쪽 = 이 블록" 을 쪽 칸에 적기만** (`+0x48a`) | 아니오 | 안내판 목록을 공책에 베끼고, 상자마다 "**목록 몇 번 묶음 볼 것**" 쪽지 |
| ⑦ 헤더 쪽 표시 | `+0x575 ~ +0x581 OR [쪽 칸],1` | 아니오 | 첫 상자엔 "**목록 없음. 설계도 기준 번지 칸 하나만 고칠 것**" 쪽지 |
| ⑧ 확정 | 조각 `+0x44` 부가 정보 `+0x20` = 기록 블록 → `+0x55` **`BasedAddress` = 새 기준** → `+0x65` **`TransferAddress += 델타`** | 아니오 | 서랍 서류에 공책 위치·새 기준 번지를 적고, 요약 카드 "첫 출근 방" 줄 고쳐 쓰기 |
| ⑨ 올라와 있던 쪽 | 조각 `+0x106` · `+0x11d` `MiWalkEntireImage` → 이미 메모리에 있는 쪽만 `MiRelocateImagePfn` | **예 (그 쪽만)** | **이미 뜯어 놓은 상자만** 지금 바로 고침 |
| ⑩ 표시 | 조각 `+0x143` `OR byte [R15+0x33],4` = `ImageDynamicallyRelocated` | 아니오 | "번지 옮겼음" 도장 |

- 모두 `[정적]` (run 문서). 조각 오프셋을 함수 기준으로 쓰면 `+0x55` = `+0x1b1269`, `+0x65` = `+0x1b1279`, `+0x143` = `+0x1b1357` 이다.
- 그러니까 **"고치기" 는 ⑨ 한 칸뿐**이고, 그것도 그 순간 메모리에 있던 쪽만이다.
  - 섹션을 막 만든 순간에 올라와 있는 쪽이 헤더 쪽뿐인지는 모른다 `[공백]`.
- 왜 이렇게 미뤄 두나? 코드에 이유가 적혀 있진 않다. 짐작 `[추측]`: 쪽을 미리 다 읽어 오지 않아도 되고, 안 만지는 쪽은 끝까지 안 고쳐도 되니까. (근거 없음, 그림으로만.)

#### 곁가지 — 낯선 항목 종류면 "조용히 안 옮긴다"

- `MiScanRelocationPage` 는 항목 종류 `0` · `3` · `0xA` 만 받는다. 다른 종류면 `0xC0000269` `[정적]`.
- 그런데 `MiRelocateImage` 는 그걸 **오류로 안 돌려준다.** `+0x7df ~ +0x7eb` 가 `0` 으로 바꿔 끝낸다 → ⑧ 을 안 탄다 = **이동을 안 한다** `[정적]`.
- 비유: 쪽지를 쓰다가 못 읽는 안내판이 나오면, 관리사무소는 "그럼 이 세트는 원래 번지 그대로 두자" 하고 접는다.
- test.exe · dll1 · dll2 는 `DYNAMICBASE` · `.reloc` 있음 · `RELOCS_STRIPPED` 없음이라 ① 에 안 걸린다 `[소스: 빌드 산출물 헤더]`. 낯선 종류가 있는지는 run 문서에 없다.

### 2.4 ★ 실제로 펜을 드는 사람 = `MiPerformFixups`

| 언제 | 누가 부르나 | 비유 |
|---|---|---|
| 쪽이 파일에서 들어올 때 | `MiValidateInPage +0x233` → `MiRelocateImagePfn` | 상자를 **처음 뜯을 때** |
| 섹션을 막 만들었을 때, 이미 올라와 있던 쪽 | `MiWalkEntireImage +0x4cd` → `MiRelocateImagePfn` (`+0x496` 이 "올라와 있음" 표시를 읽음, `TEST AL,2`) | 이미 뜯어 둔 상자 |
| 기준을 다시 바꿀 때 | `MiSwitchBaseAddress +0x9a` → `MiWalkEntireImage(…, 2, …)` | 구획을 바꿔서 다시 고칠 때 (공유 길, Q62b 몫) |

- 셋 다 `[정적]`.
- `MiRelocateImagePfn` 은 "쪽 번호 < 쪽 수" 이고 "그 쪽 칸 ≠ 0" (쪽지가 붙어 있음) 일 때만 `+0x141 MiPerformFixups` 를 부른다 `[정적]`.
- `MiPerformFixups` 는 실제로 고치는 셋에게 넘긴다 — `MiApplyCompressedFixups` · `MiApplyRawFixups` · `MiApplyStraddleFixups` (쪽 경계에 걸친 항목) `[정적]`.

**새 말 — 쪽 경계에 걸친 항목 (`Straddle`)**: 고칠 번지수 하나가 두 쪽에 반씩 걸쳐 있는 경우 (run 문서 "쪽 경계 항목").

- 비유: 안내판 하나가 상자 두 개에 반씩 걸쳐 붙어 있다. 한 상자만 뜯어선 다 못 고치니 따로 다룬다.

#### run38 의 짐작 `[추측]` 은 어떻게 됐나 — 단계별로

1. run38 의 짐작: "**만약** 관리사무소가 다 고쳐 둔다면 → 창구가 `0` 을 주고 → 관리실은 펜을 안 든다."
2. 이번 근거 `[정적]`: 관리사무소가 **고치긴 고친다.** 다만 세트 짤 때가 아니라 상자를 뜯을 때.
3. 이번 근거 `[정적]`: 창구 대답은 "놓인 번지 = 기준 주소인가" 로만 정한다 (2.7).
4. 그래서: 세트가 기준 주소에 그대로 놓이면 `0` → 관리실 펜 안 듦. 짐작의 **앞 다리는 코드로 섰다** `[정적: 흐름]`.
5. 아직 확인 아님: dll1 · dll2 가 정말 기준 주소에 놓였는지, 페이지가 들어올 때 정말 `MiRelocateImagePfn` 이 서는지 — 실행해서 볼 일 (Q61).

### 2.5 설계도 기준 번지 칸 (`ImageBase`) 도 관리사무소가 고친다 — run73 `[공백]` 닫힘

- run73 까지: "커널이 메모리 속 헤더 `ImageBase` 를 고치는 줄은 못 찾았다" `[공백]`.
- 이번 `[정적]`:
  1. `MiRelocateImage +0x581` 이 헤더 `ImageBase` 칸이 든 쪽의 칸에 `1` 을 적는다 (2.3 ⑦).
  2. `MiPerformFixups +0x1b6 ~ +0x1c4` 가 "이 쪽이 그 쪽인가" 를 본다.
  3. 맞으면 `+0x278` (32비트) / `+0x37a` (64비트) 에서 그 칸을 **`BasedAddress` 쪽으로 옮긴다.**
- 비유: 첫 상자에 붙은 "기준 번지 칸 하나만" 쪽지가, 상자를 뜯을 때 실제로 그 칸을 새 번지로 고쳐 쓰게 한다.
- 덤 증거 `[정적]` (결과서): 부팅 때 `PspMapSystemDll` 이 ntdll 의 기준 칸 `+0x20` 을 **메모리 헤더** `ImageBase` 에서 읽는다 (`RtlImageNtHeader` 뒤 `[RAX+0x30]`). 헤더가 이미 옮겨져 있어야 run48 `+0x172` 검사와 맞는다.

#### 이게 exe 재배치 문 (`+0x1430`) 과 이어지는 이유

- exe 고쳐 쓰기 문은 `LdrpInitializeProcess +0x1419` 가 "메모리 헤더 `ImageBase` ≠ `PEB.ImageBaseAddress`" 일 때만 연다 ([run19 해설](../02_sprint/run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1), run38).
- 관리사무소가 헤더 칸을 `BasedAddress` 로 옮겨 두고, exe 가 `BasedAddress` 에 놓이면 → 두 값이 같다 → `+0x1430` 은 안 탄다 `[정적: 흐름]` (flow.md 2.4).
- 실제로 안 서는지는 동적 (Q61, 4절).

### 2.6 DLL · ntdll 도 같은 길 — 다른 건 "구획 고르는 법" 하나

run 문서 표 (결과서 1-3) 를 줄였다.

| | exe (test.exe) | 보통 DLL (dll1 · dll2 · kernel32 …) | ntdll |
|---|---|---|---|
| 세트 짜러 오는 길 | `NtCreateUserProcess +0x93c MmCreateSpecialImageSection` | `NtCreateSection` (run73) | 부팅 `PspLocateSystemDll +0xf4 MmCreateSpecialImageSection` (run48) |
| 공통 안쪽 | `MiCreateImageOrDataSection +0x385 MiCreateNewSection +0x3da MiRelocateImage` | 같음 | 같음 |
| 기준 주소 고르기 | **임의** (`ExGenRandom` → `0x7ff600000000` 부터 64KB 칸) | 구획 배정표 (run73) | 구획 배정표 |
| 번지수 고치기 | 커널 `MiPerformFixups` | 같음 | 같음 |
| 헤더 `ImageBase` 칸 | 커널이 `BasedAddress` 쪽으로 | 같음 | 같음 |
| `TransferAddress` | 커널 두 번 (2.8) | 안 씀 (ntdll 이 헤더로 계산) | 안 씀 |

- 모두 `[정적]` (run 문서).
- run38 해설 4절에 남았던 질문 "**DLL 세트도 커널 `MiRelocateImage` 를 지나나, 이미지 안 번지수 전체를 고치나**" 의 답이다: **지난다. 고친다 — 쪽을 뜯을 때.**

#### exe 가 "임의" 쪽이라는 건 왜 `[정적]` 이 됐나 — 단계별로

1. run73 까지 `[추측]` 이었다.
2. 근거 `[정적]`: exe 가 구획 배정표 쪽으로 가려면 `MiRelocateImage` 여섯째 인자 **비트 1** 이 필요하다.
3. 근거 `[정적]`: 그 비트의 출처는 주문서 (패킷) 비트 28 ← `MiInitializeCreateSectionPacket +0x9d` 의 `0x40` 이다.
4. 근거 `[정적]`: 그런데 `MmCreateSpecialImageSection` 이 만드는 값은 `{1, 8, 0x20}` 조합뿐이다 (`+0x3f ~ +0x73`, `+0xc7` 로 넘김). `0x40` 이 없다.
5. 그래서 → exe 는 구획 배정표 체크박스를 못 받는다 → 임의 갈래 `[정적: 인자 따라가기]`.

- 비유 (run73 주문서 이어서): DLL 접수 창구는 주문서에 "구획 배정표 쓰기" 칸을 체크해서 넘기는데, exe 접수 창구 (`MmCreateSpecialImageSection`) 는 그 칸을 아예 안 쓴다. 그래서 exe 는 주사위 (`ExGenRandom`) 로 정한다.

### 2.7 창구 대답 `0` / `0x40000003` — "기준 주소에 놓였나" 한 줄

2.2 의 한 줄 읽기 ② 가 이 절의 핵심이다. 흐름을 이야기로 펴면:

1. 관리실이 "아무 데나 놓아 주세요" (주소 안 줌) 하고 창구에 낸다.
2. 창구는 먼저 **기준 주소 자리**를 시도한다 — 시작 후보 = `BasedAddress + SectionOffset` (`+0x365 ~ +0x373`) `[정적]`.
3. 그 자리가 **비면** 그대로 놓는다 → 비교 같음 → `0`.
4. 그 자리가 **차 있으면** `+0x9ae MiSelectUserAddress` 로 다른 자리를 찾는다 → 비교 다름 → `0x40000003`.

- "주소를 줌" 비트 (`[R12+0x3c]` 비트 0) 의 뜻은 흐름으로 읽은 것이다 `[정적: 흐름]`.
- **설계도 (헤더) 의 `ImageBase` 와는 맞대 보지 않는다** `[정적]`. 이게 중요하다.
  - 예전엔 "헤더 번지 `0x180000000` 이 아니니까 `0x40000003` 이겠지" 라고 생각하기 쉬웠다 (run38 사실 ①).
  - 그런데 창구가 보는 건 헤더가 아니라 **관리사무소가 고른 기준 주소**다.
- 그래서 결론 `[정적]`: ntdll 의 `LdrpRelocateImage` 문은 **세트가 기준 주소 말고 다른 곳에 놓였을 때만** 탄다.
- `/DYNAMICBASE` 없는 x86/x64 이미지는? `MiRelocateImage` 가 ① 에서 일찍 끝나 `BasedAddress` = 헤더 `ImageBase` 그대로. 그 자리가 비면 `0`, 차면 `0x40000003` → ntdll 이 `.reloc` 로 직접 고친다 `[정적]`.

다른 대답들 (짧게, 모두 `[정적]`):

| 대답 | 때 |
|---|---|
| `0x40000036` | `0x40000003` 갈래에서 `BL` 비트 1 · 3 이 같이 켜짐 (`+0xc1b ~ +0xc82`, `BL` 출처 `[공백]`) |
| `0x4000000e` | 기계 종류 불일치 |
| `0xc0000018` | 호출자가 준 범위가 차 있음 |

- exe 쪽: `MiMapProcessExecutable +0xb3` 이 `MmMapViewOfSectionEx` 를 주소 NULL 로 부르고, 반환이 **음수일 때만** 실패로 본다 (`+0xbd JS`) → `0x40000003` 도 통과 `[정적]`.
  - 다만 exe 길 `MmMapViewOfSectionEx → MiMapViewOfSectionExCommon → MiMapViewOfSection → MiMapViewOfImageSection` 이 정말 이 갈래로 이어지는지는 호출 줄만 봤다 `[정적: 호출 줄만]` (Q188 ⑥).

### 2.8 진입점 보정 둘 — 두 번 세는 게 아니라 **이어 붙는다** (질문 b)

진입점 칸 (`TransferAddress`, "첫 출근 방") 이 바뀌는 순서:

| # | 어디 | 무엇을 더하나 | 어느 사본 |
|---|---|---|---|
| 0 | `MiBuildImageControlArea +0x172` (run31) | 처음 값 = `ImageBase` + 진입점 rva | 섹션 사본 |
| 1 | `MiRelocateImage +0x1b1279` | **새 − 옛 `BasedAddress`** | 섹션 사본 |
| — | `PspAllocateProcess +0x98c` `MmGetSectionInformation` | (복사) | 섹션 → 프로세스 사본 |
| 2 | `PspAllocateProcess +0x20ce` | **붙은 주소 − `BasedAddress`** (다를 때만) | 프로세스 사본 |

- 모두 `[정적]`.
- 합 = **붙은 주소 + 진입점 rva**. 두 보정의 "기준" 이 이어져 있어서 같은 델타를 두 번 세지 않는다.
  - 1 은 "헤더 기준 → 관리사무소가 고른 기준" 까지.
  - 2 는 "고른 기준 → 실제로 놓인 자리" 까지.
- 비유: 첫 출근 방 번지를 **두 번 나눠 고쳐 쓴다.** 세트 짤 때 "구획이 바뀌었으니" 한 번, 땅에 놓았더니 그 구획이 아니었으면 "실제 자리로" 한 번 더.

#### 한 줄 읽기 — 둘째 보정을 건너뛰는 문

`PspAllocateProcess +0x20c6 CMP RDX,RAX` / `+0x20c9 JZ → +0x20d2`

- **누가**: `PspAllocateProcess` 가
- **어디서**: `+0x20c6`
- **무엇과 무엇을**: `RDX` = 세그먼트 `+0x20` = **`_SEGMENT.BasedAddress`** (`+0x20bb` 가 읽음) 와 `RAX` = `EPROCESS.SectionBaseAddress` (붙은 주소, `+0x20bf` 가 읽음)
- **같으면**: `+0x20c9 JZ` 로 건너뛴다 → 보정 없음.
- **다르면**: `+0x20cb SUB` · `+0x20ce ADD [R15+0x38],RAX` 로 차이를 더한다.
- 그 앞 문 둘 `[정적]`: 이미지 정보 사본이 있을 때만 (`+0x20a6 TEST R15,R15`), `BL` 비트 `2` 가 꺼져 있을 때만 (`+0x20ab TEST BL,2`. 만들기 플래그 비트 11 · 13 · 14 에서만 켜지는 비트 — 최소 프로세스 · 복제 갈래로 보임 `[추측: 이름]`, 평범한 exe 는 꺼짐).
- 보통은 같다: `MiMapProcessExecutable` 이 주소 NULL 로 불러 `BasedAddress` 를 먼저 시도하니까 (2.7) `[정적: 흐름]`.

#### 조심할 것 하나 `[추측]`

- `+0x20bb` 는 `BasedAddress` 를 복사 (`+0x98c`) **보다 뒤에 다시** 읽는다.
- 그 사이 같은 서랍 (컨트롤 영역) 에서 `MiSwitchBaseAddress` 가 돌면 사본과 어긋날 수 있다. 같은 파일을 동시에 여는 드문 경우다.

### 2.9 여섯 함수는 모두 X — 대신 더하는 줄 하나가 새로 나왔다 (질문 a)

| 함수 | `TransferAddress` 쓰나 |
|---|---|
| `MiParseComAndCetHeaders` | X |
| `MiMakeImageReadOnly` | X |
| `MiInitializeNewImageSectionProtos` | X |
| `MiValidateSectionSigningPolicy` | X |
| `MiReleaseImageSection` | X |
| `MiFinalizeImageHeaderPage` | X |

- 모두 `[정적]`. 각 함수가 대신 쓰는 칸은 run 문서 표에 있다.
- 여섯 밖에서 `TransferAddress` 를 더하는 줄 (이번에 읽은 범위) `[정적]`:
  1. `MiBuildImageControlArea +0x172` (처음 값, run31)
  2. `MiRelocateImage +0x1b1279`
  3. **`MiSwitchBaseAddress +0x4e ADD [RCX],RAX`** ← 새로. 새 `BasedAddress` 를 `+0x46` 에 쓰고 이번 델타를 더한다 (공유 길에서 기준을 다시 고를 때).
  4. `PspAllocateProcess +0x20ce` (프로세스 사본)
- "이 넷뿐" 이라고는 못 한다. 포인터를 거쳐 쓰는 줄은 정적으로 다 못 찾는다.

### 2.10 사진과 맞춰 보면 (run44 K1 · P1a, 새로 돌린 것 아님)

flow-writer 가 [run44](run44-baggage-snap-해설.md) 사진 로그를 다시 읽었다. 값은 `[관찰]`, 읽는 길은 `[정적]`.

| 무엇 | 값 |
|---|---|
| exe `_SEGMENT.BasedAddress` | `0x7ff61a370000` |
| exe `EPROCESS.SectionBaseAddress` | `0x7ff61a370000` |
| exe `TransferAddress` | `0x7ff61a371d40` (= `BasedAddress` + `0x1d40`) |
| exe `ImageFlags` | `0x4` (`ImageDynamicallyRelocated` 만) |
| `PEB.ImageBaseAddress` | `0x7ff61a370000` |
| exe 모듈 항목 `OriginalBase` | `0x7ff61a370000` |

읽는 법:

1. `BasedAddress` 가 헤더 `0x140000000` 이 아니고 `0x7ff6…` 대역이다 (`0x7ff61a370000 − 0x7ff600000000 = 0x1a37 × 0x10000`). `ImageFlags` 비트 2 도 켜져 있다.
   → 이 세트에서 `MiRelocateImage` 가 ⑧ ~ ⑩ 까지 갔다.
   - 다만 **이번 실행에서인지, 같은 부팅의 앞 실행에서인지는 모른다** (공유 길이면 앞 실행의 값) → Q61.
2. `TransferAddress` = `BasedAddress` + 진입점 rva → ⑧ 의 델타가 더해진 값.
3. `SectionBaseAddress` == `BasedAddress` → `+0x20c6 CMP` 같음 → **`+0x20ce` 보정은 안 탔을 것**. 같은 이유로 exe 창구 대답은 `0` 일 것.
   - 값으로 읽은 갈래다. 멈춤점으로 본 건 아니다.
4. `OriginalBase` 가 이미 붙은 주소다 → 메모리 헤더 `ImageBase` 가 이미 옮겨져 있었다는 뜻이고, 2.5 와 맞는다.
   - 하지만 ntdll `+0x1430` 이 헤더를 고쳐도 같은 값이 나올 수 있다. 그래서 이 칸만으로 "ntdll 은 안 고쳤다" 를 가리진 못한다 `[추측]` (Q61 동적 3, Q188 ①).

- run30 의 `0x7ff70ee10000` (다른 부팅) 과 값이 다르다 → 부팅마다 고르는 임의 갈래와 어긋나지 않는다 `[추측: 두 값으로만]`.

### 2.11 정정 — sast/11 결과서의 "컨트롤 영역 `+0x20`"

- 예전 (sast/11 결과서, run16): "컨트롤 영역 `+0x20` 칸 (선호 주소)".
- 바르게는 `[정적]`: **컨트롤 영역 `+0x0` (`Segment`) → 세그먼트 `+0x20` = `_SEGMENT.BasedAddress`** (`+0x20b8 MOV RCX,[RAX]` · `+0x20bb MOV RDX,[RCX+0x20]`).
  - 컨트롤 영역 `+0x20` 은 따로 있다 — `NumberOfPfnReferences`.
  - 값도 "헤더의 선호 주소" 가 아니라 **`MiSelectImageBase` 가 고른 기준**이다 (처음엔 헤더 값, `MiRelocateImage` 가 덮음).
- 비유: 서랍 (컨트롤 영역) 안 서류철 (세그먼트) 의 칸이었는데, 서랍 겉면 칸으로 잘못 적어 둔 것.
- 이 표현은 결과서에만 있고 run16 문서 · flow.md 엔 안 옮겨졌다. 그래서 다른 run 문서는 안 고쳤다.

### 2.12 비유로 다시 한 번

1. 관리사무소가 조립식 세트를 짠다. 아직 땅에 안 올리고 배치표만 그린다 (run31).
2. "옮기면 안 되는 세트" 면 그냥 돌려보낸다 (① 일찍 끝).
3. 아니면 구획을 고른다 — DLL·ntdll 은 구획 배정표, exe 는 주사위 (⑤).
4. 설계도에 붙은 **안내판 위치 목록** (`.reloc`) 을 공책 (`MmRe`) 에 베끼고, **상자마다 "목록 몇 번 묶음 볼 것" 쪽지**를 붙인다. 첫 상자엔 "기준 번지 칸 하나만" 쪽지 (⑥ ⑦).
5. 서랍 서류에 새 기준 번지를 적고, 요약 카드 "첫 출근 방" 줄을 차이만큼 고쳐 쓴다 (⑧).
6. 이미 뜯어 놓은 상자가 있으면 그것만 지금 고친다 (⑨). "번지 옮겼음" 도장 (⑩).
7. 나중에 누가 어떤 상자를 **처음 뜯으면** (page-in), 그때 쪽지대로 안내판을 고친다 (`MiPerformFixups`). 첫 상자를 뜯으면 설계도 기준 번지 칸도 새 번지가 된다.
8. 창구에서 세트를 땅에 놓을 때, 창구 직원은 **"새 기준 번지에 놓였나"** 만 본다. 같으면 "놓았어요" (`0`), 다르면 "원래 자리 아니에요" (`0x40000003`).
9. 관리실 (ntdll) 은 "원래 자리 아니에요" 를 들을 때만 직접 펜을 든다 (run38).
10. 가게 짓는 단계 (`PspAllocateProcess`) 는 요약 카드 사본을 받아, 실제 놓인 자리가 기준 번지와 다를 때만 첫 출근 방을 한 번 더 고친다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 — 그림 1

그림 1 (부모 cmd.exe 스레드) 의 섹션 짜기 줄기에 **상자가 하나 늘었다.**

```
전:  i2 MiCreateImageOrDataSection ─+0x385→ i3 MiCreateNewSection ─+0x215→ i4 MiCreateImageFileMap ─+0x2e6→ i5 …
                                         (i3 글: "뒤에 +0x3da MiRelocateImage 가 새 기본 주소면 TransferAddress 에 델타 더함")

후:  i2 MiCreateImageOrDataSection ─+0x385→ i3 MiCreateNewSection ─(1) +0x215→ i4 MiCreateImageFileMap ─+0x2e6→ i5 …
                                                                    └─(3) +0x3da→ i7 MiRelocateImage   ★ 새 상자
```

| 자리 | 전 | 후 |
|---|---|---|
| `i7` | 없음 | **새 상자 `nt!MiRelocateImage`** — 기준 주소를 `_SEGMENT.BasedAddress` 에 (exe `ExGenRandom` `0x7ff6…`, DLL 비트맵), `.reloc` 는 쪽마다 표만, `TransferAddress += 델타`, 페이지 안 번지수는 page-in 때 `MiValidateInPage → MiRelocateImagePfn → MiPerformFixups`, 올라와 있던 쪽만 `MiWalkEntireImage` 로 바로, 헤더 `ImageBase` 칸도 같은 길 (정적 run102) |
| `i3` 글 | "없을 때만. 뒤에 `+0x3da MiRelocateImage` 가 … 델타 더함" | "없을 때만" — 그 설명은 `i7` 상자로 옮겼다 |
| `i3 → i4` 화살표 | `+0x215` | `(1) +0x215` — 코드 순서 번호를 붙였다. `(3) +0x3da` 가 새로 생겨서다 |
| `i2` 글 | 공유 길이면 "오른쪽 **셋** 안 탐" | "오른쪽 **넷** 안 탐" — `i7` 이 늘었으니 |
| `p4` `MmMapViewOfSectionEx` | "exe 올리기" | + "맵 결과 `0` / `0x40000003` 은 안쪽 `MiMapViewOfImageSection +0x42a` 가 붙은 자리 vs `BasedAddress` 로 정함 (run102)" |

- 번호 `(2)` 가 그림에 없는 건, `(2) +0x167 MiSectionCreated` 상자가 그림엔 없고 2.1 트리에만 있어서다 (flow.md 2.1).
- 그림 제목 괄호에 run102 가 붙었다.
- 쪽이 들어올 때 고치는 길 (`MiValidateInPage …`) 은 **그림에 상자로 안 넣었다.** 시간 순서 밖 (페이지 폴트 길) 이라 `i7` 상자 글로만 적었다.

### flow.md 에서 바뀐 것 — 글

| 자리 | 전 | 후 |
|---|---|---|
| 2.1 `MiRelocateImage` 절 | `[추측]` "test.exe 는 이 길을 탈 것", "이 조각에서 더하는지, `+0x20b3 ~ +0x20ce` 에서 보정하는지는 동적" | 줄 그음 → **탄다 `[정적]` + 값 `[관찰]`** (run44 K1). 둘 다 하고 이어 붙음 `[정적]` |
| 2.1 진입점 흐름 | `+0x20ce` 줄 없음 | `PspAllocateProcess +0x20ce += 붙은 주소 − BasedAddress (다를 때만)` 줄 더함 |
| 2.1 `TransferAddress` 더하는 줄 | — | 넷 (`+0x172` · `+0x1b1279` · `MiSwitchBaseAddress +0x4e` · `+0x20ce`), 여섯 함수 X |
| 2.4 exe 재배치 `+0x1430` | — | "커널이 바꿔 둔다" → `BasedAddress` 에 붙으면 `+0x1419` 같음 → `+0x1430` 안 탐 `[정적: 흐름]` |
| 2.6 "커널이 메모리 헤더 `ImageBase` 를 고치는 줄" | `[공백]` | 줄 그음 → (해결됨) `MiPerformFixups +0x278` / `+0x37a` `[정적]` |
| 2.6 run72 줄 아래 | `+0xc0a` 만 | 그 앞 비교 줄 `+0x42a` 까지 (헤더 `ImageBase` 와 안 비교) |
| 4장 Q35 줄 | `+0x20b3 ~ +0x20ce` 관계 `[추측]` | (해결됨, 정적) 두 번째 보정 |
| 4장 Q62 줄 | (a)~(f) 모두 남음 | (a) · (b) · (f) 해결됨 → 남은 (c) · (d) · (e) → Q62b |
| 4장 새 줄 셋 | — | run102 동적 · 정적 남은 것 · PE_emulator 쪽 |
| 4.1 그림 2 회색 상자 번호 | (12) · (15) | (13) · (16) — 그림 2 화살표 번호에 맞춤 (run101 해설에서 짚은 것) |

- 짐 지도 `baggage.md`: **새 짐 칸 없음**, 2장 숫자 그대로. 3.22 `BasedAddress` 줄 13 → 14 (`MiSwitchBaseAddress +0x46` 조건부 줄), 3.23 `TransferAddress` 줄 26 → 27 (`+0x4e` 조건부 줄), 새 7.34.

### 큰 그림에서

- run31 부터 남아 있던 "**번지수 고치는 펜을 누가 드나**" 가 이번에 커널 쪽은 다 보였다.
  - 커널: 쪽지 붙이기 (`MiRelocateImage`) + 상자 뜯을 때 고치기 (`MiPerformFixups`).
  - ntdll: 창구가 "원래 자리 아니에요" 라고 할 때만 (run38).
  - 둘을 잇는 끈은 여전히 **창구 대답 하나**인데, 이번에 그 대답을 가르는 줄 (`+0x42a`) 과 기준 (`BasedAddress`) 까지 닫혔다.
- PE_emulator 쪽 (결과서 요약, 고칠지는 사용자 결정):
  - DLL 재배치 **결과 값은 맞다** (같은 델타, 같은 헤더 칸). **때만 다르다** — 진짜는 상자 뜯을 때, 우리는 세트를 놓을 때 한 번.
  - 다른 곳: `.reloc` 없는 DLL (우리 실패 / 진짜는 헤더 칸만 고침), 낯선 재배치 종류 (우리 `STATUS_NOT_SUPPORTED` / 진짜는 0 으로 끝내고 이동 안 함). 샘플엔 해당 없음.
  - exe · ntdll 기준 주소: 우리는 헤더 값 (D8 · 12 결정으로 막아 둠), 진짜는 exe 임의 · ntdll 구획 배정표.
  - 진입점 보정 둘: 우리는 앞의 것만 있지만, exe 가 늘 `BasedAddress` 에 놓이니 지금은 맞다.
- 다음 바퀴 (run103, Q62b) 는 **같은 서랍을 다시 쓰는 공유 길** (`MiShareExistingControlArea → MiValidateExistingImage`) 과 프로토 PTE 다. 이번에 `MiRelocateImageAgain → MiSwitchBaseAddress` 까지 닿아서 바로 이어진다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 창구 대답 실제 값 — `MiMapViewOfImageSection +0x42e` 에서 `rcx` vs `poi(@rbp-0x60)`, `+0xc0a` 를 지나나 (exe · dll1 · dll2 · kernel32) | 실행 때 값 | **Q61** 동적 1 (run72 · run73 과 같은 질문, Q104) |
| 누가 `.reloc` 를 실제로 적용하나 — `MiRelocateImagePfn` 입구 caller 가 `MiWalkEntireImage+0x4d2` 인가 `MiValidateInPage+0x238` 인가, DLL 마다 몇 번, `MiPerformFixups +0x278/+0x37a` 가 헤더 쪽에서 도나 | 실행 때 값 | **Q61** 동적 2 |
| exe 헤더 `ImageBase` 와 ntdll 재배치 — `ntdll+0x8d834` (`+0x1430`) 가 안 서나, `+0x1419` 의 `[NT+0x30]` | 실행 때 값. 사진 `OriginalBase` 만으론 못 가름 `[추측]` | **Q61** 동적 3 |
| 섹션 사본 `TransferAddress` 가 더해지는 순간 (조각 `+0x65` · `+0x55`) — 같은 부팅 첫 실행이면 서고, 공유 길이면 안 섬 | 사진 값이 이번 실행 것인지 앞 실행 것인지 모름 | **Q61** 동적 4 |
| `PspAllocateProcess +0x20c6` 의 `rdx` · `rax` 가 같아서 `+0x20c9 JZ` 를 타나 (사진 값으론 같음, 기대: 탐) | 값으로만 읽음 | **Q61** 동적 5 |
| exe 가 `MiSelectImageBase +0x304` (임의 갈래) 를 지나나 | 실행 때 값 | **Q61** 동적 6 |
| 공유 길 `MiShareExistingControlArea → MiValidateExistingImage`, 프로토 PTE, `MiVerifyImageHeader` 이유 코드 | 범위 밖 (Q62 를 나눈 뒤쪽) | **Q62b** 정적 (다음 바퀴 run103) |
| ntdll `LdrpRelocateImage` · `LdrpProtectAndRelocateImage` · `LdrRelocateImageWithBias` 본문 — 헤더 `ImageBase` 를 고치나 (2.10 의 `[추측]` 닫기) | ntdll 은 이번 범위 밖 | **Q188 ①** 정적 |
| `MiCanPageMove` + `MiValidateInPage +0x14e ~ +0x351` 비트 3 · 5 조건 — **평범한 DLL 이 page-in 때 정말 `MiRelocateImagePfn` 을 타나**, `MiPrivateFixup +0x340` 의 쓰임 | 조건을 끝까지 못 따라감 `[공백]`. Ghidra `.c` 가 묶음을 잘못 보여 `.asm` 을 믿었다 | **Q188 ②** 정적 |
| `MiSelectImageBase` `ESI` 갈래 (1 · 2 · 3 · 4) 와 `MiBuildImageControlArea +0x25e/+0x261` (`BasedAddress` 처음 값) 한 장 | 이번엔 exe 갈래만 | **Q188 ③** 정적 |
| `0xC0000269` 로 끝날 때 켜 둔 구획 배정표 칸은 어떻게 되나 | 안 따라감 (`MiReturnImageBase` 류를 안 부르는 것으로 읽힘 `[추측]`) | **Q188 ④** 정적 |
| `0x40000036` 의 `BL` 비트 1 · 3, `[R12+0x3c]` 비트 0 의 이름 · 출처 | run72 부터 `[공백]` | **Q188 ⑤** 정적 |
| exe 길 `MiMapViewOfSectionExCommon` 이 `+0x1c8 MiMapViewOfSection` 쪽인가 (`+0xb6 MiMapViewOfSectionCommon` 과 갈림) | 호출 줄만 봄 | **Q188 ⑥** 정적 |
| 섹션을 막 만든 순간 올라와 있는 쪽이 헤더 쪽뿐인가 | 정적으로 못 정함 `[공백]` | 따로 번호 없음. Q61 동적 2 의 caller 가 단서가 될 수 있다 `[추측]` |
| PE_emulator 를 고칠지 (때 · `.reloc` 없는 DLL · 낯선 종류 · 기준 주소) | 사용자 결정 | 번호 없음 — 사용자와 정함 |

---

## 5. 더 읽기

- run 문서: [`run102-image-reloc.md`](run102-image-reloc.md) — 호출 지점 표, `MiRelocateImage` 열 단계 표, exe · DLL · ntdll 표, status 표, `+0x20a6 ~ +0x20d2` 보정 표, 사진 로그 대조, 호출 관계 그림 셋
- 결과서: [`_workspace/sast/98_image-reloc_result.md`](../../_workspace/sast/98_image-reloc_result.md) — 1-1 (열 단계 원본), 1-2 (페이지 고치는 길), 1-3, 1-5, 질문 2 · 3
- 요청서: [`_workspace/sast/98_image-reloc.md`](../../_workspace/sast/98_image-reloc.md)
- flow.md: [그림 1](flow.md) (`i7`) · [2.1](flow.md) `MiRelocateImage` 절 · 진입점 흐름 · [2.4](flow.md) `+0x1430` · [2.6](flow.md) 붙이기·재배치 절 · 4장 run102 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.22 · 3.23 · 7.34
- 앞 이야기:
  - [run31 해설](../02_sprint/run31-image-section-해설.md) — 세트·서랍·배치표, 진입점 값이 흘러가는 길 (2.11)
  - [run38 해설](../02_sprint/run38-map-module-해설.md) — 창구 대답, ntdll 펜, "커널 재배치와 유저 재배치는 어떤 사이인가" (2.10)
  - [run73 해설](run73-image-base-select-해설.md) — 주문서 체크박스, 구획 배정표
  - [run44 해설](run44-baggage-snap-해설.md) — 사진 K1 · P1a
