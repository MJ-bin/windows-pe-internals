# run103 image-share — 해설

> 이 글은 [run103 문서](run103-image-share.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q62b** — Q62 의 (c)·(d)·(e): 공유 길 `MiShareExistingControlArea → MiValidateExistingImage` 안쪽, 헤더 페이지 → 프로토 PTE (`MiSectionCreated +0x103 MiInitializeNewImageSectionProtos`), 작은 것 넷 (`MiVerifyImageHeader` 이유 `0x57` ~ `0x59`, `Feature_TrustedLaunchHosts`, 세션 번호 함수 별칭, `MiCreateSection` 재시도) (`_workspace/plan.md`, 이번에 **끝**)
  - Q62 를 Q62a · Q62b 로 나눈 뒤쪽이다. 앞쪽 Q62a 는 [run102](run102-image-reloc-해설.md). 이번으로 **Q62 정적은 모두 끝났다.**
  - 남은 것: 동적 → **Q61** (run103 몫을 얹음), 정적 → **Q189 ①~⑦** (새로), PE_emulator 쪽 셋 → flow.md 4장 (고칠지는 사용자 결정).
- 근거:
  - run 문서 [`run103-image-share.md`](run103-image-share.md) (가장 중요)
  - 요청서 [`_workspace/sast/101_image-share.md`](../../_workspace/sast/101_image-share.md)
  - 결과서 [`_workspace/sast/101_image-share_result.md`](../../_workspace/sast/101_image-share_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/03_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴는 **정적만** 했다. VM 은 안 켰다.
  - ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음).
  - 값 대조는 **run44 사진 로그를 다시 읽은 것**뿐이다 (2.11). 새로 돌린 실행은 없다.
- 이번에 **run73 을 정정**했다 (2.5). [run73 해설](run73-image-base-select-해설.md) 맨 위에 "고쳐 읽기 (run103)" 한 줄을 붙였다.

이미 푼 말은 **링크만** 건다. 비유도 그대로 이어 쓴다.

- 커널 = **관리사무소**, 시스템 콜 = **신청 창구**, ntdll = 가게 안 **관리실** → [run38 해설 "비유"](../02_sprint/run38-map-module-해설.md#비유--그대로-이어-쓴다)
- 이미지 섹션 = **조립식 건물 세트**, 매핑 = 세트를 땅에 **갖다 놓기** → [run9 해설](../02_sprint/run9-create-user-process-해설.md#이미지-섹션) · [run10 해설](../02_sprint/run10-allocate-process-해설.md#매핑-올리기)
- 컨트롤 영역 = 설계도 서류함의 **서랍**, 세 갈래 (비었음 / 멀쩡 / 만드는 중) 와 "작성 중" 팻말 (`BeingCreated`), "서랍이 빈 날" → [run31 해설 2.5](../02_sprint/run31-image-section-해설.md#새-말--컨트롤-영역)
- **프로토 PTE** = 서랍 속 **배치표**의 한 줄 한 줄 ("3번 방 = 설계도 몇 쪽") → [run31 해설 2.8](../02_sprint/run31-image-section-해설.md#새-말--프로토-pte)
  - run31 때는 뜻을 함수 이름으로만 읽어 `[추측]` 이었다. 이번에 그 모양이 코드로 보였다 (2.8).
- 세그먼트 = 배치표 묶음, 이미지 정보 = 맨 앞 **요약 카드**, `TransferAddress` = 요약 카드 맨 윗줄 **"첫 출근 방"** → [run31 해설 2.9](../02_sprint/run31-image-section-해설.md#그-칸은-어디-사나-정적-pdb-타입)
- `_SEGMENT.BasedAddress` = 세트의 **기준 주소**, 비트맵 = 관리사무소의 **구획 배정표**, 패킷 = **주문서** → [run73 해설 2.2 · 2.3](run73-image-base-select-해설.md#23-그래서-dll1dll2-는-구획-배정표에서-따로-받는다)
- 쪽 (페이지) = **상자**, `MiRelocateImage` 가 붙이는 **쪽지**, page-in = 상자를 **처음 뜯을 때**, 재배치 기록 = 관리사무소 **공책**, PFN = 창고 **선반 번호** → [run102 해설 2.1 · 2.3](run102-image-reloc-해설.md#21-한눈에)
- VAD = 땅의 **구획 장부** → [run10 해설 "껍데기와 장부"](../02_sprint/run10-allocate-process-해설.md#껍데기와-장부)
- 서랍을 두 길 (새로 그리기 / 같이 쓰기) 이 각각 어떻게 채우나, 왜 사진으로 못 가르나 → [run47 해설 2.8](run47-section-chain-해설.md)

### 한 줄로 먼저

> 서랍 (컨트롤 영역) 이 이미 있으면, 관리사무소는 **다시 그리지 않는다.**
> 서랍을 다시 쓰기 전에 세 가지만 확인하고, 하나라도 안 맞으면 **"안 돼요"** 하고 돌려보낸다.
> 상자 겉의 **"만져도 되는 법" 스티커** (보호값) 는 서랍을 처음 그릴 때 이미 붙어 있다.

- 같이 쓰기 길 (`MiShareExistingControlArea`) 은 새 서랍을 **안 만든다.** 검사가 실패하면 status 로 끝난다 `[정적]`.
  - 예외 하나: 파티션이 안 맞는 `0xC0000476` 만 `MiCreateSection` 이 주문서부터 다시 쓴다 `[정적]`.
- 확인하는 셋 = 이동식 미디어 · 기준 주소 · 서명. **"파일이 바뀌었나" 는 안 본다** `[정적]`.
- **기준 주소는 서랍의 `ImageActive` 표시가 꺼졌을 때만 다시 고른다** `[정적]`. → run73 의 "살아 있으면 그대로" 를 고쳤다 (2.5).
- 섹션마다 보호값은 `MiInitializeNewImageSectionProtos` 가 아니라 **더 앞의 `MiParseImageSectionHeaders`** 가 16칸 표로 붙인다 `[정적]`. → 요청서 전제를 고쳤다 (2.7).
- `MiInitializeNewImageSectionProtos` 가 하는 일은 따로 있다 — **이미 뜯어 둔 헤더 상자 하나**를 배치표 첫 줄에 꽂아 둔다 `[정적]` (2.9).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 이야기는 거의 다 **세트를 짜는 순간** (부모 cmd 스레드, 커널) 에 있다.
같은 자리에서 길이 둘로 갈리는데, 이번엔 **오른쪽 (같이 쓰기)** 을 처음 열어 봤다.

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess
 ├─ +0x93c MmCreateSpecialImageSection                   ← (e) Feature 스위치 · 세션 번호
 │    └─ MiCreateSection                                ← (e) 안쪽 재시도
 │         └─ MiCreateImageOrDataSection   갈림길
 │              ├─ 서랍 없음 → MiCreateNewSection        (run31 · run102)
 │              │    ├─ MiCreateImageFileMap → … MiParseImageSectionHeaders   ← (d) 보호값
 │              │    ├─ MiSectionCreated → MiInitializeNewImageSectionProtos ← (d) 헤더 상자 꽂기
 │              │    ├─ MiRelocateImage                  (run102)
 │              │    └─ MiFinalizeImageHeaderPage        ← (d) 헤더 상자 내려놓기
 │              └─ 서랍 있음 → MiShareExistingControlArea ← (c) ★ 이번 중심
 │                   └─ MiValidateExistingImage → MiRelocateImageAgain → (MiSwitchBaseAddress)
 ├─ +0xc36 PspAllocateProcess → … 세트를 땅에 갖다 놓기  (run10 · run102)
 └─ 첫 스레드 출발 → ntdll … → NtContinue 은퇴 → main

(지도 범위 밖) 앞 실행이 끝난 뒤 → MiCheckControlArea → MiImageUnused  ← (c) ImageActive 끄기
```

- 맨 아래 줄은 **시간 순서 밖**이다. 앞에 돌린 test.exe 가 끝난 뒤 언젠가 일어나는 일이다 (run 문서: "앞 실행이 끝난 뒤 — 지도 범위 밖").
- (e) 넷은 서로 상관없는 **작은 질문 묶음**이다. 2.10 에서 하나씩 짧게 다룬다.

### 무엇이 궁금했나

- **(c)** 서랍이 이미 있을 때 (같이 쓰기 길) 무엇을 검사하나? 실패하면 **새로 그리러 가나, 그냥 끝나나?** 기준 주소는 언제 다시 고르고, 그때 `BasedAddress` · `TransferAddress` 는 어떻게 바뀌나?
- **(d)** `MiInitializeNewImageSectionProtos` 는 무슨 일을 하나? 섹션 특성 (읽기 · 쓰기 · 실행) → 보호값 표는? 헤더 페이지는 어떤 차례로 다뤄지나?
- **(e)** `MiVerifyImageHeader` 이유 `0x57` · `0x58` · `0x59` 의 조건, `Feature_TrustedLaunchHosts…` 가 켜고 끄는 것, run31 `+0x86` 세션 번호 함수의 다른 이름, `MiCreateSection` 안쪽 재시도 조건.

### 바퀴 전에 알던 것

- `[정적]` (run31) `MiCreateImageOrDataSection` 은 갈림길이다. 서랍이 있고 "작성 중" 팻말 (`BeingCreated`) 이 꺼져 있으면 `+0x1d5 MiShareExistingControlArea`. 안쪽은 안 읽었다.
- `[정적]` (run47) 같이 쓰기 길이 쓰는 칸은 카운터 · `SizeOfSection` 정도. `MiValidateExistingImage` · `MiInitializeNewImageSectionProtos` 는 **이름만** 있었다.
- `[정적: 흐름으로 읽음]` (run73) 서랍이 살아 있으면 `MiRelocateImageAgain +0x87 JNZ` 로 먼저 고른 구획을 그대로 쓴다. ← 이번에 반만 맞다고 고쳤다.
- `[정적]` (run102) 기준을 다시 고르면 `MiSwitchBaseAddress +0x46` 이 `BasedAddress`, `+0x4e` 가 `TransferAddress` 를 바꾼다. 언제 타는지는 Q62b 몫으로 남았다.
- `[추측]` (run31 · flow.md 2.1) 배치표 칸 (프로토 PTE) 의 뜻은 함수 이름으로만 읽었다.

---

## 2. 찾은 것

### 2.1 한눈에

세트 짤 때 — 부모 cmd 스레드 (트리 숫자는 ret 자리, run 문서 "호출 관계" 를 줄였다):

```
MiCreateSection
 ├─ +0xf8  MiCreateImageOrDataSection
 │    ├─ +0x1d5  MiShareExistingControlArea   (서랍 있음)
 │    │    ├─ +0x2d   MiValidateControlAreaPartition   실패 0xC0000476 → 주문서부터 다시
 │    │    └─ +0xf9   MiValidateExistingImage          실패 → status 로 끝 (새로 안 그림)
 │    │         ├─ +0x56   MiRelocateImageAgain        ImageActive 켜짐 → 그대로 (+0x87)
 │    │         │    ├─ +0xd8   MiSelectImageBase      꺼짐일 때만
 │    │         │    └─ +0x183  MiSwitchBaseAddress    새 ≠ 옛일 때만
 │    │         └─ +0xdb · +0x16f · +0x1ec · +0x25d   서명 검사 · 기능 스위치
 │    └─ +0x385  MiCreateNewSection           (서랍 없음)
 │         ├─ +0x215  MiCreateImageFileMap → … +0x367 MiParseImageSectionHeaders  ★ 보호값
 │         ├─ +0x167  MiSectionCreated → +0x103 MiInitializeNewImageSectionProtos ★ 헤더 상자 꽂기
 │         ├─ +0x3da  MiRelocateImage   (run102)
 │         └─ +0x6e7  MiFinalizeImageHeaderPage                                   ★ 내려놓기
 └─ +0xfa ~ +0x10a  0xC0000054 · 0xC0000476 이면 +0x237 JMP +0x48 (주문서부터 다시)
```

- 모두 `[정적]` (run 문서).
- 눈여겨볼 것: 왼쪽 위 `MiShareExistingControlArea` 아래에 **`MiCreateNewSection` 이 없다.** 같이 쓰기 길에서 새로 그리기 길로 넘어가는 줄이 없다는 뜻이다 (2.3).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 기준 주소를 다시 고를지 가르는 줄

`MiRelocateImageAgain +0x80` / `+0x87` (CA `+0x5c` 비트 22 `ImageActive` 를 보고 `JNZ`)

- **누가**: `MiRelocateImageAgain` 이 (같이 쓰기 길 검사 `MiValidateExistingImage +0x51` 이 부름)
- **어디서**: `+0x7b` 에서 재배치 락을 잡은 뒤 `+0x80` 에서 서랍 (CA) `+0x5c` 의 **비트 22 (`ImageActive`)** 를 본다.
- **켜져 있으면**: `+0x87` 에서 `+0x117` 로 뛰어 락을 풀고 `0` 을 돌려준다 = **먼저 고른 기준 주소 그대로** `[정적]`.
- **꺼져 있으면**: 아래로 내려가 `+0xd3 MiSelectImageBase` 로 **새로 고른다** `[정적]`.
- **무엇을 위해**: 서랍이 아직 구획을 쥐고 있으면 그대로 쓰고, 놓아 버렸으면 다시 받으려고 (2.5 에서 단계별로).

#### 한 줄 읽기 ② — 상자 스티커를 표에서 꺼내는 줄

```
MiParseImageSectionHeaders +0x1a8  LEA RCX,[0x14001a6b0]
MiParseImageSectionHeaders +0x1af  MOVSX R15D,[RAX+RCX]
```

- **누가**: `MiParseImageSectionHeaders` 가 (새로 그리기 길, `MiBuildImageControlArea +0x367` 이 부름)
- **어디서**: `+0x1a8` 에서 표 `MiImageProtectionArray` (`.rdata` rva `0x1a6b0`, 16바이트) 의 주소를 잡고, `+0x1af` 에서 그 표의 한 칸을 읽는다.
- **몇 번째 칸을**: `RAX` = 섹션 특성의 `EXECUTE` · `READ` · `WRITE` · `SHARED` 네 비트로 만든 번호 (`+0x17d ~ +0x1a5`).
- **무엇을 위해**: 그 섹션 (`.text`, `.data` …) 의 보호값을 정해 서브섹션과 배치표 칸에 적으려고 (2.7).

#### 한 줄 읽기 ③ — 구획 표시를 끄는 줄

`MiImageUnused +0x83 BTR [RBX+0x5c],0x16`

- **누가**: `MiImageUnused` 가 (주로 `MiCheckControlArea +0xc9` · `+0x3be` 가 부름)
- **어디서**: `+0x83`. `BTR` 은 비트 하나를 끄는 명령, `0x16` = 22.
- **무엇을**: 서랍 `+0x5c` 비트 22 (`ImageActive`) 를 **끈다**. 바로 앞 `+0x7f` 에서는 서랍 `+0x58` 에 −1 을 써 **64KB 칸을 반납**한다 `[정적]`.
- **무엇을 위해**: 서랍은 남겨 두되, 쥐고 있던 구획만 내놓으려고 (2.5).

### 2.3 ★ 같이 쓰기 길은 새로 그리지 않는다 — 실패하면 그냥 끝

#### 무엇을 확인하나 (결과서 1-2 · 1-3 요약)

`MiShareExistingControlArea` 가 먼저 보는 것:

| # | 무엇 | 실패하면 |
|---|---|---|
| S1 | `+0x28 MiValidateControlAreaPartition` — 주문서의 파티션 vs 서랍의 파티션 | `0xC0000476` (test.exe 는 파티션 하나라 `+0x44` 로 통과 `[정적 + 가정]`) |
| S2 | 서랍이 프리페처가 만든 것 (`PrefetchCreated`, CA `+0x38` bit9) 이면 `MmChangeSectionBackingFile` 로 파일 객체를 이번 것으로 바꿈 | 반환값 안 봄 |
| S5 | `SEC_IMAGE` 면 **`+0xf4 MiValidateExistingImage`** | 그 status 그대로 |
| S6 | 크기 (`MaximumSize`) 가 있으면 쓰기 보호 필요 | `0xC0000040`. test.exe 는 `MaximumSize` 0 이라 안 탐 |

- S3 · S4 (주문서 비트 켜기 · 파일 잠금 풀기) 는 실패가 없어서 뺐다. 모두 `[정적]`.

본 검사 `MiValidateExistingImage` (이유 번호를 전역 `nt+0xe2d838` 에 남김):

| # | 무엇 | 실패하면 |
|---|---|---|
| V1 | **이동식 미디어** — 서랍이 이동식 미디어 (CA `+0x38` bit11) + 주문서 비트 18 | 이유 `0xc`, `0xC0000433` |
| V2 | **기준 주소** — `+0x51 MiRelocateImageAgain` (2.5) | 이유 `0xd`, 그 status |
| V3 | **서명 ①** 요구 수준 — `+0xd6 SeGetImageRequiredSigningLevel` | 이유 `0xe`, 그 status |
| V4 ~ V7 | 서명 ② 인자 모으기 · 이미 올라온 이미지 수준과 비교 → 비트 `ESI` 만 모음 | **여기선 status 안 만듦** |
| V8 | **서명 ③** 정책 — `+0x258 MiValidateSectionSigningPolicy`. `ESI & 6` 이 0 이면 바로 0 | 이유 `0xf`, 그 status |
| V9 | 파일 수정 시각 · 크기 · 내용 비교 | **두 함수 안에 없다** |

- 모두 `[정적]`.
- 비유: 서랍을 다시 꺼내 쓰기 전에 관리사무소가 확인하는 건
  1. "이 설계도, 빼 갈 수 있는 USB 에 들어 있던 거 아니죠?" (이동식 미디어)
  2. "놓을 구획은 아직 유효한가?" (기준 주소)
  3. "도장 (서명) 은 이번 주문 기준에도 맞나?" (서명)
  - 그리고 **"설계도가 그새 바뀌었나?" 는 안 묻는다.**

#### 실패하면 어디로 — 단계별로

1. 근거 `[정적]`: 검사가 실패하면 `MiCreateImageOrDataSection +0x1da JS → +0x521 MiDereferenceFailedControlArea`. 서랍은 **안 지우고** "나도 쓴다" 는 이름 (참조) 만 지운다.
2. 근거 `[정적]`: 그다음 status 를 돌려주고 `MiCreateSection` 이 정리한다.
3. 근거 `[정적]`: `MiShareExistingControlArea` 가 부르는 11줄에 `MiCreateNewSection` 이 **없다** (결과서, flow-writer 도 `_calls.tsv` 로 확인).
4. 그래서 → **"검사 실패 = 새로 그리기"가 아니다. 그냥 "안 돼요"로 끝난다.**

예외 하나 — 파티션 불일치 `0xC0000476`:

- `+0x26f JZ → +0x43e` 에서 `CcWaitForUninitializeCacheMap` · `MiForceSectionClosed` 를 부른 뒤 status 를 올린다 `[정적]`.
- 그러면 `MiCreateSection +0x10a JZ → +0x21a` 가 **주문서부터 다시** 쓴다 (2.10 의 재시도) `[정적]`.
- 둘째 바퀴에서 `MiReferenceControlArea` 가 서랍을 다시 찾으니, 그 사이 서랍이 없어졌으면 새로 그리기 쪽으로 갈 수도 있다 (갈림은 매번 새로 한다, 결과서 1-7).
- test.exe 는 파티션 하나라 이 길을 안 탈 것이다 `[정적 + 가정]`.

#### 이 길에 없는 것 하나 더 — 주소 충돌 검사

- "이 프로세스 땅의 그 번지에 이미 뭔가 있나" 는 **여기서 안 본다** `[정적]`.
- 그건 세트를 땅에 갖다 놓을 때 창구가 본다 — `MiMapViewOfImageSection +0x42a` ([run102 해설 2.7](run102-image-reloc-해설.md#27-창구-대답-0--0x40000003--기준-주소에-놓였나-한-줄)).

### 2.4 왜 "파일이 바뀌었나" 를 안 보는 게 눈에 띄나

- 요청서가 "파일 바뀜" 을 검사 후보로 꼽았다. 그런데 두 함수 어디에도 시각 · 크기 · 내용을 맞대 보는 줄이 없다 `[정적]`.
- 서랍이 "만드는 중 / 지우는 중" 인 경우는 이미 앞에서 `MiReferenceControlArea` 가 걸러 준다 (기다렸다 처음부터, run31 — 결과서 V9 칸).
- 파일이 바뀌었을 때 서랍이 어떻게 되는지는 이번 근거에 없다. 모른다.

### 2.5 run73 정정 — 기준 주소는 ImageActive 가 꺼졌을 때만 다시 고른다

#### 새 말 — `ImageActive`

- **`ImageActive`** = 서랍 (CA) `+0x5c` 의 비트 22 `[정적]`. PDB 이름이다 (결과서 1-0).
- 켜는 곳: `MiRelocateImageAgain +0xde` (새로 고른 직후), `+0x1a1` (임시 PTE 실패 때 비트만), 새로 그리기 길의 확정 조각 `+0x1b127c` (flow.md 2.1) `[정적]`.
- 끄는 곳: `MiImageUnused +0x83` — 같은 곳 `+0x7f` 에서 64KB 칸도 반납한다 `[정적]`.
- 그래서 이 비트는 **"이 서랍이 구획 배정표의 칸을 쥐고 있다"** 는 표시로 읽힌다. (켜고 끄는 자리가 칸을 받고 내놓는 자리와 붙어 있어서다.)
- 비유: 서랍에 꽂힌 **구획 열쇠**. 열쇠가 꽂혀 있으면 그 구획은 이 세트 몫이다.

#### run73 이 뭐라고 했나, 무엇이 틀렸나

- run73: "같은 파일 서랍이 **살아 있으면** `+0x87 JNZ` 로 먼저 고른 구획을 그대로 쓴다" `[정적: 흐름으로 읽음]`.
- 이번 `[정적]`: 그대로 쓰는 건 **`ImageActive` 가 켜져 있을 때뿐**이다.
- 서랍이 살아 있어도 열쇠 (`ImageActive`) 를 이미 반납했으면, 다음 같이 쓰기는 구획을 **새로 받는다** (`+0xd3 MiSelectImageBase`).

#### 서랍은 남는데 열쇠만 반납되는 때 — 단계별로

1. 근거 `[정적]`: `MiCheckControlArea +0xc9` 가 `MiImageUnused` 를 부르는 조건 = 땅에 놓인 세트 수 (뷰, CA `+0x28`) 0 · 섹션 참조 (`+0x18`) 0 · `FilePointer` ≠ 0 · PFN 참조 (`+0x20`) ≠ 0 · `Image`.
2. 풀어 읽으면: **아무도 이 세트를 땅에 놓고 있지 않고, 아무도 섹션을 쥐고 있지 않은데, 선반에 상자는 아직 남아 있다.**
3. 근거 `[정적]`: 그때 `MiImageUnused` 가 `+0x7f` 에서 64KB 칸을 반납하고 `+0x83` 에서 `ImageActive` 를 끈다.
4. 그래서 → 그 서랍을 다시 쓰는 다음 `SEC_IMAGE` 는 같이 쓰기 길로 와서, `+0x87` 을 지나 **기준을 새로 고른다** `[정적]`.
5. 아직 확인 아님: 앞 test.exe 가 끝난 뒤 이 줄이 **실제로 언제** 도는지는 `[추측]` 이다 → Q61 (동적 2).

- 비유: 가게 (프로세스) 가 문을 닫고 세트를 다 치웠는데, 창고 선반엔 상자가 남아 있다. 관리사무소는 **서랍은 두고 구획 열쇠만 회수**한다. 다음에 같은 설계도로 주문이 오면 서랍은 꺼내 쓰지만, 구획은 배정표에서 새로 받는다.

#### 그럼 늘 다시 고르나? — 아니다

- 다른 프로세스에 계속 놓여 있는 DLL (`kernelbase` 류) 은 뷰가 0 이 안 되니 `ImageActive` 가 켜져 있다 → 같은 기준 그대로 `[정적: 흐름]`.
- KnownDlls 섹션은 섹션이 이미 있어서 이 길 자체를 안 탄다 (run 문서).
- dll1 · dll2 는? 다시 고를 때도 `MiSelectImageBase` 넷째 인자 비트 0 은 늘 0 이다 (flow.md 2.6, run73) → 구획 배정표에서 따로 받는다는 **run73 의 결론은 그대로**다. 바뀐 건 "그대로 쓰나 / 새로 받나" 하는 한 단계뿐이다.

#### `MiSwitchBaseAddress` 까지 가려면 — 여섯 문

| # | 문 | 어디 |
|---|---|---|
| ① | 재배치 기록 (공책) 이 있다 | `+0x16 ~ +0x35` (없으면 바로 0 — run102 "일찍 끝" 세트) |
| ② | `ImageActive` 꺼짐 | `+0x80/+0x87` |
| ③ | 임시 PTE 하나 예약 성공 | `+0xa3` (실패면 `+0x1a1` 에서 비트만 켜고 0) |
| ④ | `MiSelectImageBase` 성공 | `+0xd3` (뒤 `+0xde` 에서 `ImageActive` 켬) |
| ⑤ | CFG 비트맵 · CI 갱신 성공 | `+0xfc MiUpdateImageSystemWideBitmaps`, `SeSetImageBaseAddress` (실패면 `+0x1b5 MiImageUnused` 로 도로 끔) |
| ⑥ | 새 기준 ≠ 옛 `BasedAddress` | `+0x10b/+0x113` (같으면 안 탐) |

- 모두 `[정적]` (run 문서 "확인된 것").
- ⑥ 이 재미있다. 열쇠를 반납했다가 새로 받았는데 **우연히 같은 구획**이면, 고쳐 쓸 게 없으니 `MiSwitchBaseAddress` 를 안 부른다.

### 2.6 다시 고르면 바뀌는 칸 (`MiSwitchBaseAddress`, run102 와 이어서)

| 줄 | 칸 | 전 → 후 |
|---|---|---|
| `+0x3a ~ +0x3e` | 공책 `+0x28` | 누적 델타 → **이번 델타** (걷는 동안) |
| **`+0x46`** | `_SEGMENT.BasedAddress` | 옛 → 새 |
| **`+0x4a ~ +0x4e`** | `TransferAddress` ("첫 출근 방") | `+= 새 − 옛` |
| `+0x9a/+0x9f` | `MiWalkEntireImage(…, 2, 세션)` | 이미 뜯어 둔 상자만 지금 고침 (run102) |
| `+0x9f ~ +0xa3` | 공책 `+0x28` | 이번 → **누적** (안 뜯은 상자는 나중에 이 값으로) |
| `+0xb0 ~ +0xb6`, `+0xff ~ +0x129` | `SegmentFlags` 비트 11 (`DebugSymbolsLoaded`) | 켜짐 → 꺼짐 |

- 모두 `[정적]`. 공책 `+0x10` 목록을 도는 `MiApplyBytestreamFixup` 두 고리와 적재 구성 `+0x18` 칸 둘은 뜻을 못 읽었다 `[공백]` (4절).
- 공책 `+0x28` 이 왜 "이번 → 누적" 으로 두 번 바뀌나:
  - 이미 뜯어 둔 상자는 예전 구획 기준으로 한 번 고쳐져 있다. 그래서 **이번에 옮긴 만큼만** 더 고쳐야 한다 (걷는 동안 = 이번 델타).
  - 아직 안 뜯은 상자는 설계도 그대로다. 나중에 뜯을 때 **헤더 기준부터 전부** 고쳐야 한다 (걷기 뒤 = 누적).
  - 이 설명은 표의 두 줄을 이어 읽은 내 짐작이다 `[추측]` — 결과서는 "걷는 동안은 이번 한 번의 델타", "아직 안 올라온 페이지는 나중 page-in 때 이 누적 값으로" 까지만 적었다.

### 2.7 ★ 상자 스티커 (섹션 보호값) 는 `MiParseImageSectionHeaders` 가 붙인다 — 요청서 전제 정정

#### 무엇이 정정됐나

- 요청서 전제: "`MiInitializeNewImageSectionProtos` 가 프로토 PTE 를 섹션마다 보호값으로 채운다."
- 이번 `[정적]`: 보호값은 **그보다 앞**, 새로 그리기 길의 `MiBuildImageControlArea +0x367 MiParseImageSectionHeaders` 가 표 `MiImageProtectionArray` 로 채운다.
- 이 전제는 요청서에만 있었다. run31 · run47 · run102 · flow.md 는 "헤더 페이지 → 프로토 PTE" · "이름만" 으로만 적어서, 다른 run 문서는 안 고쳤다 (run 문서 "정정").

#### 새 말 — copy-on-write (`PAGE_WRITECOPY`, 쓰기 시 복사)

- **copy-on-write** = 여럿이 같은 쪽을 같이 보다가, **누가 쓰려는 순간에만** 그 사람 몫 사본을 만들어 주는 방식.
- 비유: 같은 세트의 상자를 여러 가게가 같이 들여다본다. 한 가게가 상자에 **낙서하려는 순간**, 관리사무소가 그 가게 몫 복사 상자를 하나 만들어 준다. 다른 가게 상자는 깨끗하다.
- 이 방식이라는 건 `PAGE_WRITECOPY` 라는 이름과 일반적으로 알려진 설명에서 온 것이다 `[추측]`. 이번 코드로 본 건 "`WRITE` 만 있고 `SHARED` 가 없으면 값 5 · 7 (`PAGE_WRITECOPY` 짝) 을 붙인다" 까지다 `[정적]`.

#### 16칸 표 (run 문서 표를 그대로)

| EXEC · READ · WRITE | `SHARED` 꺼짐 | `SHARED` 켜짐 |
|---|---|---|
| — — — | `0x18` `PAGE_NOACCESS` | 같음 |
| X — — | 2 `PAGE_EXECUTE` | 같음 |
| — R — | 1 `PAGE_READONLY` | 같음 |
| X R — | 3 `PAGE_EXECUTE_READ` | 같음 |
| — — W 또는 — R W | **5 `PAGE_WRITECOPY`** | 4 `PAGE_READWRITE` |
| X — W 또는 X R W | **7 `PAGE_EXECUTE_WRITECOPY`** | 6 `PAGE_EXECUTE_READWRITE` |

- 표 원본 바이트 `18 02 01 03 05 07 05 07 18 02 01 03 04 06 04 06`. `PAGE_*` 짝은 `MmUserProtectionToMask1/2` 원본 바이트로 맞췄다 `[정적]`.
- 읽는 법: **`SHARED` 가 갈라놓는 건 "쓰기" 가 들어간 두 줄뿐**이다.
  - `WRITE` 만 → 쓰기 시 복사 (5 · 7).
  - `WRITE` + `SHARED` → 진짜로 같이 쓰는 읽기쓰기 (4 · 6).
- `DISCARDABLE` 같은 다른 특성 비트는 안 본다 `[정적]`.

#### test.exe 에 대입하면 (결과서 2-4)

| 서브섹션 | 특성 | 보호 | `PAGE_*` |
|---|---|---|---|
| 헤더 | — | 1 | `PAGE_READONLY` |
| `.text` | `0x60000020` | 3 | `PAGE_EXECUTE_READ` |
| `.rdata` · `.pdata` · `.reloc` | `0x40000040` · `0x40000040` · `0x42000040` | 1 | `PAGE_READONLY` |
| `.data` · `.fptable` | `0xC0000040` | 5 | `PAGE_WRITECOPY` |

- 특성 값은 `sample/bin/test.exe` 헤더를 읽은 것 `[소스: 빌드 산출물 헤더]`, 보호값은 표에서 `[정적]`.
- 여기서 **서브섹션** = 배치표 안에서 PE 섹션 (`.text`, `.data` …) 하나마다 한 장씩 있는 묶음이다 (위 표처럼 헤더 포함 7장). 비유: 배치표의 **장(章)**. 보호값은 이 장 머리에 붙는다.
- `.reloc` 특성 `0x42000040` 에는 `DISCARDABLE` 이 섞여 있지만, 그 비트는 안 보니 그냥 읽기만 (1) 이다 (결과서 2-4).

#### 스티커가 붙는 곳 (모두 `[정적]`)

- 서브섹션 `+0x20` 비트 1 ~ 5 (`Protection`) — `+0x1c9 ~ +0x1d1`.
- 배치표 칸 (프로토 PTE) 비트 5 ~ 9 — 2.8.
- 실행이 있으면 (`값 & 2`) 요약 카드 `ImageContainsCode` = 1 — `+0x1b4 ~ +0x1bf`.
- 쓰기가 있으면 (`값 & 4`):
  - 쓰기 시 복사 (5 · 7) → 쪽마다 세그먼트 `ImageCommitment` +1 (`+0x249`).
  - 공유 쓰기 (4 · 6) → `GlobalMemory` (서브섹션 · CA `+0x38` 비트 17, `+0x395`) + 쪽마다 `NumberOfCommittedPages` +1 (`+0x240`).
  - test.exe 는 5 쪽뿐이라 `ImageCommitment` 쪽이다.
- 헤더 서브섹션은 늘 1 (`+0x2e6 ~ +0x2f3`). .NET IL-only 만 `MiCreateNewSection +0x6b6 MiMakeImageReadOnly` 가 모두 1 로 덮는다 (test.exe 는 안 탐).

### 2.8 배치표 칸 (프로토 PTE) 의 세 모양 — run31 `[추측]` 닫힘

run31 해설 2.8 에서 "배치표 한 줄 = '3번 방 = 설계도 몇 쪽'" 이라고 했다. 그 줄이 실제로 **세 모양**이다.

| 모양 | 만드는 함수 | 무엇이 들어 있나 | 언제 |
|---|---|---|---|
| 파일에서 읽을 쪽 | `MiMakeSubsectionPte` | **`Prototype` 비트 10 켜짐** · 보호 비트 5 ~ 9 · 서브섹션 주소 (비트 16~) | 파일 내용이 있는 쪽 |
| 0 으로 채울 쪽 | `MiMakeDemandZeroPte` | 보호 비트 5 ~ 9, 비트 10 = 0 | `VirtualSize > SizeOfRawData` 구간 |
| 이미지 밖 | 전역 `ZeroPte` | 0 | 섹션 크기 넘는 쪽 |

- 모두 `[정적]` (`MiParseImageSectionHeaders +0x1d5` · `+0x1e0` · `+0x2b2`, flow.md 2.1).
- 비유로:
  - 첫째 줄 = "이 방은 **설계도 3장 (서브섹션) 몇 쪽**을 보고 지을 것, 스티커는 이것".
  - 둘째 줄 = "이 방은 **빈 방으로** 지을 것 (설계도에 내용 없음), 스티커는 이것".
  - 셋째 줄 = 빈 칸.
- 그래서 flow.md 2.1 `MiCreateImageFileMap` 절의 "(PTE 의 뜻은 함수 이름으로 본 것 `[추측]`)" 이 `[정적]` 으로 바뀌었다.

### 2.9 헤더 상자 하나는 미리 뜯어 둔다 — 꽂기 → 내려놓기 차례

#### 왜 헤더 상자만 따로인가

- 관리사무소는 설계도 첫 장 (헤더) 을 **읽어야** 검사도 하고 배치표도 그린다.
- 그래서 헤더만은 세트를 짤 때 이미 **물리 페이지 하나에 읽어 둔다** (run31 해설 2.8 의 "헤더 한 페이지만") `[정적]`.
- 이 상자를 다 쓰고 버리면 아깝다. 그래서 배치표 첫 줄에 **"이 상자는 이미 선반에 있음"** 으로 꽂아 둔다. 그 일이 `MiInitializeNewImageSectionProtos` 다.

#### 새 말 — transition (대기) 페이지

- **transition** = 물리 메모리에 내용이 **아직 남아 있지만**, 어느 프로세스 땅에도 지금 붙어 있지 않은 상태로 일반적으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 결과서는 "프로토가 소유한 transition(대기) 페이지" 라고 적었다.
- 비유: 이미 뜯어서 **선반에 올려 둔 상자**. 누가 그 방을 처음 쓰려 할 때 창고에서 다시 꺼낼 필요 없이 선반에서 바로 집으면 된다.

#### 차례 (결과서 2-3)

| 순서 | 어디 | 하는 일 | 비유 |
|---|---|---|---|
| 1 | `MiCreateNewSection +0x215 MiCreateImageFileMap` | `MiCreateMdl(…, 0x1000, 1)` 이 `MiGetPageForHeader` 로 **물리 페이지 하나** → 헤더 읽기 → 검사 → 배치표 그리기 (보호값 2.7) | 설계도 첫 장을 상자 하나에 꺼내 펴 놓고 일함 |
| 2 | `+0x167 MiSectionCreated → +0x103 MiInitializeNewImageSectionProtos` | 서랍을 `ImageSectionObject` 에 꽂고 "작성 중" 팻말을 내린 **뒤**, 배치표 칸이 비어 있고 `Prototype` 이면 `+0x1a2 MiInitializeTransitionPfn` 으로 그 페이지를 **transition 으로 꽂음** | 그 상자를 배치표 첫 줄에 "선반에 있음" 으로 등록 |
| 3 | `+0x3da MiRelocateImage` | (run102) 쪽지 붙이기 | |
| 4 | `+0x6e2 MiFinalizeImageHeaderPage` | 꽂힌 쪽만 — 일할 때 쥐고 있던 잠금 · 참조를 풂 → 프로토가 가진 대기 페이지 | 펴 놓고 누르던 **집게를 풀어** 선반에 그냥 둠 |
| 5 · 6 | `+0x41e ~ +0x460` · `+0x468 MiReleaseImageSection` | MDL 해제 · (run102) | 작업 쪽지 버리기 |

- 모두 `[정적]`.
- 여기서 **MDL** = 이 작업 동안 그 물리 페이지를 쥐고 있는 작은 기록이다 (결과서 2-3: `MiCreateMdl` 이 페이지를 받아 MDL 에 넣음). 비유로는 위 표의 "집게".
- 2 번에서 칸에 **이미 다른 페이지가 있으면**, 우리 페이지는 버리고 MDL 칸에 −1 을 쓴다 (`+0x110` · `+0x115`). 4 번은 −1 이 **아닌** 칸만 내려놓는다 `[정적]`.

#### 덤 — 서랍의 PFN 참조 수가 하나 오른다

- 2 번에서 그 페이지의 `OriginalPte` 비트 10 이 켜져 있으면 `+0x1cd MiReferenceControlAreaPfn(CA, 0, 1)` 을 부른다 `[정적]`.
- 그 안 `+0x36 ADD [RDI+0x20],RBP` 가 서랍 `NumberOfPfnReferences` 에 1 을 더한다 (`RDI` = CA, `RBP` = 셋째 인자 1 — flow-writer 가 줄 확인) `[정적]`.
- 비유: 서랍 겉에 적힌 **"선반에 올라간 이 세트 상자 수"** 가 하나 는다.
- 이 칸은 짐 지도 3.21 에서 그동안 짐작이었는데, 이번에 첫 `[정적]` 쓰는 줄이 생겼다 (3절).

#### test.exe 는?

- `SizeOfHeaders 0x400` → 헤더 쪽 1 → **2 번이 한 쪽을 꽂고 4 번이 그 한 쪽을 내려놓는다고 읽힌다** `[정적]`.
- 실제로 그 갈래를 타는지는 동적이다 (Q61, 4절 동적 7).

### 2.10 작은 것 넷

#### ① `MiVerifyImageHeader` 이유 `0x57` · `0x58` · `0x59` — 헤더 마지막 검사 (모두 `0xC000007B`)

| 이유 | 조건 |
|---|---|
| `0x57` | x86/x64 **이고** `RELOCS_STRIPPED` **이고** `DllCharacteristics` 비트 12 `APPCONTAINER` |
| `0x58` | x86/x64 아님 **이고** `RELOCS_STRIPPED` |
| `0x59` | x86/x64 아님 · `RELOCS_STRIPPED` 꺼짐 **이고** `DllCharacteristics & 0x140 ≠ 0x140` (`DYNAMIC_BASE` + `NX_COMPAT` 둘 다 필요) |

- 모두 `[정적]` (`+0x259 ~ +0x27e`, `+0x54f`).
- 비유: 설계도 검사관의 마지막 질문. "옮길 수 없는 설계도 (`RELOCS_STRIPPED`) 인데 앱 컨테이너용이라고요? 안 돼요." x86/x64 가 아닌 기계면 "옮길 수 있고, 무작위 배치 · 실행 막기 둘 다 되는 설계도만" 받는다.
- test.exe: `Machine 0x8664` · `Characteristics 0x22` (`RELOCS_STRIPPED` 꺼짐) → `+0x274 TEST R8W,R8W` 0 → `+0x27e` 성공. **셋 다 안 탄다** `[정적]`. 값 `0x22` 는 run44 K1 `ImageCharacteristics 0x22` `[관찰]` 과 같다.

#### ② `Feature_TrustedLaunchHosts…` — 한 비트짜리 기능 스위치

- **WIL 기능 스위치** = Windows 가 기능을 켜고 끄려고 두는 한 비트짜리 스위치다 (run 문서: "한 비트짜리 WIL 기능 스위치"). 비유: 관리사무소 벽의 **기능 스위치 하나**.
- 스위치 값은 전역 `…featureState` (`.data` rva `0xfc40f8`) 에 있는데, **파일에 처음 값이 없다** — 실행 때 채운다 `[정적]`.
- 켜졌을 때 갈라지는 곳 (모두 `[정적]`):
  - `MmCreateSpecialImageSection +0x64` → 호출자 플래그 비트 3 이 있으면 만들기 플래그 `|= 0x20`.
  - `MiInitializeCreateSectionPacket +0x1a5` → 만들기 플래그 `& 0x20` 이면 주문서 비트 27.
  - `MiCreateNewSection +0x338` · `MiValidateExistingImage +0x16a` → 주문서 비트 27 이면 서명 정책 인자 비트 21.
  - `NtCreateUserProcess +0x7c3` → 토큰 복제 함수가 갈린다 (`SeDuplicateTokenWithPredictedClaims` / `SeDuplicateTokenAndAddOriginClaim`).
- test.exe 길에서 **스위치를 들여다보는 줄은 늘 지난다** `[정적]`. 켜졌나 꺼졌나는 실행 때 값 → 동적.
- 켜져도 서명 정책까지 닿으려면 `NtCreateUserProcess [RBP+0x328]` 비트 3 이 같이 서야 하는데, 그 출처는 `[공백]` (Q189 ⑤).

#### ③ 세션 번호 함수 — 이름이 둘

- run31 의 `+0x86` (ret `+0x8b`) = nt rva `0x2e8280`. PDB 이름 **둘** — `PsGetProcessSessionIdEx` · `PsGetSessionIdEx` `[정적]` (`pdb_aliases.py --lookup`, 결과서).
- 하는 일 (34바이트): `EPROCESS +0x2e8` (`Session`) 이 0 이거나 `Flags3.SystemProcess` (`+0x5fc & 0x1000`) 면 `0xFFFFFFFF`, 아니면 `[Session+0x8]` `[정적]`.
- 누구의 세션인가: **지금 프로세스 = 만드는 쪽 = 부모 cmd** `[정적]`. 결과는 주문서 `+0xa8` 로 간다.
- 조심: `PsGetProcessSessionId` (끝에 `Ex` 없음, rva `0x379000`) 는 **다른 함수**다.
- 비유: 주문서 칸 하나에 "주문한 사람이 몇 동 주민인가" 를 적는다. 관리사무소 직원 (시스템 프로세스) 이면 "동 없음" (`0xFFFFFFFF`).

#### ④ `MiCreateSection` 안쪽 재시도

- `+0xfa CMP EAX,0xC0000054 / +0xff JZ` · `+0x105 CMP EAX,0xC0000476 / +0x10a JZ` → `+0x21a` → `+0x237 JMP +0x48` `[정적]`.
- 곧 `MiCreateImageOrDataSection` 이 **`0xC0000054` 나 `0xC0000476` 을 돌려주면 주문서를 새로 써서 처음부터 다시** 한다.
  - `+0x21a` 는 파티션 참조를 풀고 주문서 비트 24 만 남겨 둔다 (그래서 파티션 불일치는 둘째 바퀴에 통과).
- 몇 번 했는지 세는 줄이 없고, 쉬지도 않는다 `[정적]`.
- `0xC0000054` 는 `MiCreateImageFileMap +0x108 MiFlushDataSection` · `MiReadImageHeaders` 에서 올 수 있다. 이름 `STATUS_FILE_LOCK_CONFLICT` 는 `[추측]`.
- 바깥쪽 재시도 (`MmCreateSpecialImageSection +0xea KeDelayExecutionThread`, 반 초 쉬고) 는 run31 그대로. **안쪽은 바로, 바깥쪽은 쉬었다가** — 두 겹이다.

### 2.11 사진과 맞춰 보면 (run44, 새로 돌린 것 아님)

flow-writer 가 [run44](run44-baggage-snap-해설.md) 사진 로그를 다시 읽었다. 값은 `[관찰]` (run 문서 표의 `[26100.9457 확인]`), 뜻은 `[정적]` / `[추측]`.

| 무엇 | 값 | 근거 |
|---|---|---|
| exe VAD 보호 | `EXECUTE_WRITECOPY` (exe · ntdll 둘 다) | `ms/logs/04_baggage-read_P1a.txt:1550` · `1551` |
| exe 서랍 `u` (`+0x38`) | `0xa0` | 짐 지도 3.21 (run47 이 K1 로그에서 옮긴 값) |
| exe 서랍 `+0x5c` | `0x00c00009` | 같음 |
| exe 서랍 `NumberOfPfnReferences` | 3 | 같음 |

읽는 법:

1. VAD 보호 `EXECUTE_WRITECOPY` = 보호값 7 = 세그먼트 `DefaultProtectionMask` 7 과 같은 수다.
   - 구획 장부 (VAD) 한 줄에는 세트 **전체**의 대표 스티커 하나만 보인다. 섹션마다 스티커 (2.7 표) 는 이 한 줄로는 안 보인다 `[추측: 짝 맞추기]`.
2. `u` `0xa0` 에 비트 9 (`PrefetchCreated`) · 비트 17 (`GlobalMemory`) 이 없다.
   - → 프리페처가 만든 서랍이 아니고 (2.3 S2 갈래 안 탐), 공유 쓰기 섹션도 없다 (test.exe 특성 값과 맞음).
3. `+0x5c` `0x00c00009` 는 비트 22 (`ImageActive`) · 23 (`ImageBaseOkToReuse`) 켜짐, 비트 18 ~ 19 (`StrongCode`) 0.
   - → 사진 K1 순간에 **구획 열쇠를 쥐고 있었다**는 것뿐이다. 이번 실행이 새로 그리기였는지 같이 쓰기였는지는 여전히 모른다 (Q61).
4. `NumberOfPfnReferences` 3 중 1 은 헤더 상자 꽂기 (2.9 덤) 로 읽힌다 `[추측: 새 길일 때]`. 나머지 2 는 모른다 (Q189 ⑦).

### 2.12 비유로 다시 한 번

1. 관리사무소에 세트 주문이 온다. 먼저 그 설계도의 **서랍**이 있나 본다 (run31).
2. **서랍이 없으면** 새로 그린다.
   - 설계도 첫 장 (헤더) 을 상자 하나에 꺼내 펴 놓고 검사한다.
   - 배치표를 장 (서브섹션) 마다 그리면서, 상자마다 **"만져도 되는 법" 스티커**를 16칸 조견표에서 골라 붙인다. 쓰기만 되는 장은 "낙서하려면 복사본 받기" (copy-on-write).
   - 배치표 한 줄은 "설계도 몇 쪽 보고 지을 것" / "빈 방으로 지을 것" / 빈 칸, 셋 중 하나다.
   - 다 그리고 서랍을 꽂은 뒤, 펴 놓았던 헤더 상자는 배치표 첫 줄에 **"선반에 있음"** 으로 등록하고 (꽂기), 나중에 집게를 풀어 선반에 둔다 (내려놓기).
   - 구획을 고르고 쪽지를 붙인다 (run102). 서랍에 **구획 열쇠** (`ImageActive`) 가 꽂힌다.
3. **서랍이 있으면** 다시 안 그린다. 세 가지만 확인한다 — 빼 갈 수 있는 미디어인가, 구획은 유효한가, 도장은 맞나. 설계도가 바뀌었나는 안 묻는다.
   - 하나라도 안 맞으면 **"안 돼요"** 로 끝. 새로 그리러 가지 않는다.
   - 파티션이 안 맞을 때만 주문서를 새로 써서 처음부터.
4. 구획 확인은 이렇게 한다. 서랍에 **열쇠가 꽂혀 있으면** 그 구획 그대로. **열쇠가 반납돼 있으면** 배정표에서 새로 받고, 그게 예전과 다르면 기준 번지 · 첫 출근 방을 고쳐 쓰고 이미 뜯은 상자만 다시 고친다.
5. 열쇠는 언제 반납되나 — 가게들이 세트를 다 치웠는데 선반에 상자만 남아 있을 때. 서랍은 그대로 둔다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 — 그림 1

이번엔 **상자 글 둘만** 바뀌었다. 노드 · 화살표는 그대로다. 그림 제목 괄호에 run103 이 붙었다.

| 상자 | 전 | 후 |
|---|---|---|
| `i2` `nt!MiCreateImageOrDataSection` | "갈림길. 같은 파일 컨트롤 영역이 이미 있으면 `+0x1d5 MiShareExistingControlArea` 로 가고 오른쪽 넷 안 탐" | + "그 길의 검사 (`MiValidateExistingImage`) 가 실패하면 status 로 끝 — 새로 안 만듦, run103" |
| `i5` `nt!MiBuildImageControlArea` | "컨트롤 영역·프로토 PTE 만들기. `+0x172 TransferAddress = ImageBase + 진입점 rva`" | + "섹션마다 보호값은 안쪽 `+0x367 MiParseImageSectionHeaders` 가 `MiImageProtectionArray` 표로 (쓰기만 있으면 WRITECOPY), run103" |

- `MiSectionCreated` · `MiFinalizeImageHeaderPage` 는 대표 지점 밖이라 **그림엔 안 넣고 2.1 트리에만** 넣었다 (flow.md 4.1 메모).

### flow.md 에서 바뀐 것 — 글

| 자리 | 전 | 후 |
|---|---|---|
| 2.1 트리 | `+0x1d5 MiShareExistingControlArea` 한 줄, `MiCreateNewSection` 아래 셋 | 네 줄 더함: `+0xf9 MiValidateExistingImage` · `+0x367 MiParseImageSectionHeaders` · `+0x103 MiInitializeNewImageSectionProtos` · `(4) +0x6e7 MiFinalizeImageHeaderPage` |
| 2.1 `MmCreateSpecialImageSection` | `+0x64` · `+0x86` 자리만 | Feature 스위치 갈래 · 세션 번호 함수 (이름 둘) `[정적]` |
| 2.1 `MiCreateImageOrDataSection` | 공유 길 = 이름만 | 공유 길 검사 · 실패하면 status · 안 재시도 `[정적]`, `MiCreateSection` 안쪽 재시도 줄 |
| 2.1 `MiCreateNewSection` | — | 헤더 페이지 차례 (꽂기 → 내려놓기) `[정적]` |
| 2.1 `MiCreateImageFileMap` | "PTE 의 뜻은 함수 이름으로 본 것 `[추측]`" | 줄 그음 → 세 모양 `[정적]` |
| 2.1 `MiVerifyImageHeader` | — | `0x57` ~ `0x59` 조건 `[정적]` |
| 2.1 `MiBuildImageControlArea` | — | 보호값 표 `[정적]` |
| 2.1 `MiRelocateImage` 절 (run73 줄) | "서랍이 살아 있으면 `+0x87 JNZ` 로 그대로" | 줄 그음 → `ImageActive` 켜졌을 때만 (run103 정정) |
| 2.1 `MiRelocateImage` 절 둘째 길 | "비트 22 가 꺼졌을 때만 — Q62b 몫" | Q62b 몫 줄 그음 → 여섯 문 · 끄는 곳 `MiImageUnused +0x83` `[정적]` |
| 2.6 run73 아래 줄 | "`+0x87 JNZ` 로 먼저 고른 주소를 그대로 쓴다 `[정적]`" | 줄 그음 → run103 정정 `[정적]` (다른 프로세스에 계속 놓인 DLL 은 그대로) |
| 4장 Q62 줄 | (c) · (d) · (e) 남음 → Q62b | **(해결됨, 정적 — Q62 전부)** |
| 4장 run102 정적 ① | Q62b | 줄 그음 → (해결됨, 정적) run103 |
| 4장 새 줄 셋 | — | run103 동적 · 정적 · PE_emulator 쪽 |

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- **새 짐 칸 없음.** 서브섹션 · 프로토 PTE 는 체크리스트 밖이라 7.6 메모에만 적었다.
- 2장 합: 정확 **684 → 685**, 짐작 **54 → 53**. 서랍 `NumberOfPfnReferences` 가 짐작 → 정확으로 옮겨서다 (2.9 덤).
- 3.21 (컨트롤 영역) 새 줄 6, 3.22 (세그먼트) 새 줄 1 · 고친 줄 3, 3.23 (이미지 정보) 고친 줄 1, 새 7.35.
- 3.22 `BasedAddress` · 3.23 `TransferAddress` 의 공유 길 줄 (run102 때 씀) "앞 실행의 `MiRelocateImage` 가 비트 22 를 켜 두므로 안 탐 `[정적: 흐름]`" 도 run73 과 같은 잘못이라 고쳤다.

### 큰 그림에서

- run31 에서 처음 본 **갈림길의 오른쪽** (같이 쓰기) 이 이제 열렸다. 왼쪽 (새로 그리기) 은 run31 · run47 · run102 로 이미 열려 있었다.
  - 그래서 "test.exe 를 같은 부팅에서 두 번 돌리면 무엇이 달라지나" 를 정적으로는 다 말할 수 있게 됐다: 서랍은 같이 쓰고, 검사 셋을 지나고, 열쇠가 반납돼 있으면 구획을 새로 받는다.
  - 실제로 어느 쪽을 타는지는 여전히 Q61 이다. 이번 결과로 Q61 에서 **볼 자리 (멈춤점)** 가 늘었다.
- 디버거 함정 하나가 더 분명해졌다 (run 문서 "디버거 메모"): 같은 부팅에서 여러 번 돌리면 `MiSwitchBaseAddress` 가 **설 수도** 있다. 전엔 "안 선다" 쪽만 생각했다.
- PE_emulator 쪽 (결과서 요약, 고칠지는 사용자 결정 — flow.md 4장):
  - 같이 쓰기 길: 우리에 없음. test.exe 한 실행에 같은 파일을 두 번 `SEC_IMAGE` 하지 않으면 결과 같음 → **빠짐, 영향 작음**.
  - `Section.cpp:418` 주석 "`MiRelocateImageAgain` 으로 먼저 고른 주소를 씀" → run73 과 같은 **반쪽**.
  - 보호: 쓰기 효과는 같지만, `.data` 를 `PAGE_*` 로 되읽으면 진짜는 `PAGE_WRITECOPY` (`0x08`), 우리는 `PAGE_READWRITE`.
  - `0x57` 검사가 빠져 있음 (이제 근거 있음). test.exe · dll1 · dll2 는 안 걸린다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 가 같이 쓰기 길을 타나 — `nt+0x96e6cc` (`MiShareExistingControlArea`) vs `nt+0x96f4d0` (`MiCreateNewSection`) | 같은 부팅에 앞서 돌렸는지에 달림 (실행 때 값) | **Q61** 동적 1 |
| `ImageActive` 실제 값 — `nt+0x971d68` (`+0x80`) 의 `dd @rbx+0x5c L1` 비트 22, 꺼졌으면 `MiSwitchBaseAddress` 가 서나 · `+0x4e` 전후, 첫 실행 끝 무렵 `MiImageUnused` 가 서나 | 실행 때 값. "앞 실행이 끝나면 꺼져 있을 것" 은 `[추측]` | **Q61** 동적 2 |
| 서명 검사 갈래 · Feature 스위치 값 · 비교 비트 `ESI` | 실행 때 값 (스위치는 파일에 처음 값 없음) | **Q61** 동적 3 · 4 |
| `MiCreateSection +0x21a` 재시도가 안 서나 | 실행 때 값 (안 서야 맞음) | **Q61** 동적 5 |
| 보호값이 정말 `3, 1, 5, 1, 5, 1` 순서로 나오나 (서브섹션 사슬은 헤더 포함 `1, 3, 1, 5, 1, 5, 1`) | 실행 때 값. run44 덤프를 오프라인으로 읽어도 될 것 `[추측]` | **Q61** 동적 6 |
| 헤더 상자 꽂기 · 내려놓기가 test.exe 한 번에 각각 1 번 서나 | 실행 때 갈래 | **Q61** 동적 7 |
| `MiReferenceControlArea` 세 갈래 · status, `MiCheckControlArea +0xb1 → +0x29c` (서랍이 **언제 사라지나**) | 이번 범위 밖 (`+0xc9` 근처만 읽음) | **Q189 ①** 정적 |
| `MiApplyBytestreamFixup` 과 공책 `+0x10` 목록, 적재 구성 `+0x18` 칸 둘 | 뜻을 못 따라감 `[공백]` | **Q189 ②** 정적 |
| 이유 코드 `0x10 ~ 0x19` · `0x1a ~ 0x1f` 를 `MiVerifyImageHeader` 표와 한 장으로 | 번호 · 줄만 나왔고 표는 안 만듦 | **Q189 ③** 정적 |
| `SeGetImageRequiredSigningLevel` · `SeCompareSigningLevels` · `MiValidateSectionCreate` 본문 | 요청서 범위가 "이름만" | **Q189 ④** 정적 |
| `NtCreateUserProcess [RBP+0x328]` 비트 3 의 출처 — Feature 스위치가 test.exe 길에서 갈래를 바꾸나 | 안 따라감 `[공백]` | **Q189 ⑤** 정적 |
| `MiShareExistingControlArea +0x69 FsRtlGetFileSize` · `+0x77 ~ +0x84` 갈래가 무엇을 보나 | 호출 줄만 봄 (결과서 1-1 순서에 빠져 flow-writer 가 보탬) | **Q189 ⑥** 정적 (작음) |
| `NumberOfPfnReferences` 사진 값 3 중 나머지 2 는 누가 올리나 | 이번엔 헤더 상자 몫 1 만 | **Q189 ⑦** 정적 (작음, Q92 D5 와 같이) |
| 파일이 바뀌면 서랍은 어떻게 되나 | 두 함수에 비교 줄이 없다는 것까지만 | 번호 없음 — 모른다 |
| PE_emulator 를 고칠지 (보호 되읽기 · 주석 · `0x57` 검사) | 사용자 결정 | 번호 없음 — flow.md 4장 run103 PE_emulator 줄 |

---

## 5. 더 읽기

- run 문서: [`run103-image-share.md`](run103-image-share.md) — 호출 지점 표, 공유 길 검사 표 (S · V), `MiRelocateImageAgain` 줄 표, `MiSwitchBaseAddress` 칸 표, 보호 표 · test.exe 대입, 헤더 페이지 차례, 작은 것 넷, 사진 로그 대조, 호출 관계 그림 둘, 정정
- 결과서: [`_workspace/sast/101_image-share_result.md`](../../_workspace/sast/101_image-share_result.md) — 1-2 · 1-3 (검사 표 원본), 1-4 (다시 고르는 조건), 1-6 (새 길과 이유 번호 짝), 1-7 (실패하면), 2-1 ~ 2-4, 3-1 ~ 3-4
- 요청서: [`_workspace/sast/101_image-share.md`](../../_workspace/sast/101_image-share.md)
- flow.md: [그림 1](flow.md) (`i2` · `i5`) · [2.1](flow.md) `MmCreateSpecialImageSection` · `MiCreateImageOrDataSection` · `MiCreateNewSection` · `MiCreateImageFileMap` · `MiVerifyImageHeader` · `MiBuildImageControlArea` · `MiRelocateImage` 절 · [2.6](flow.md) run73 아래 줄 · 4장 run103 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.21 · 3.22 · 3.23 · 7.6 · 7.35
- 앞 이야기:
  - [run31 해설](../02_sprint/run31-image-section-해설.md) — 서랍 · 세 갈래 · 배치표 · "서랍이 빈 날"
  - [run47 해설](run47-section-chain-해설.md) — 두 길이 각자 쓰는 칸, 사진으로 못 가르는 이유
  - [run73 해설](run73-image-base-select-해설.md) — 구획 배정표 · 주문서 체크박스 (이번에 맨 위 "고쳐 읽기" 를 붙임)
  - [run102 해설](run102-image-reloc-해설.md) — 상자 · 쪽지 · 처음 뜯을 때, `MiSwitchBaseAddress +0x46` · `+0x4e`
  - [run44 해설](run44-baggage-snap-해설.md) — 사진 K1 · P1a
