# run47 section-chain — 해설

> 이 글은 [run47 문서](run47-section-chain.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q78b**: Q78 나머지를 **시간 순서 조각**으로 나눈 것의 첫째, "exe 섹션 묶음" (`_workspace/plan.md`, 이번에 **끝**)
  - 조각 순서 (사용자 결정 2026-10-28): Q78b 섹션 묶음 → Q78c 프로세스 객체 → Q78d 첫 스레드 → Q78e ntdll 앞쪽 → Q78f KERNELBASE·KERNEL32 구간 → Q78g KUSD·남은 전역.
  - 시간으로 맨 앞이라 이번이 첫째다. (Q78a 힙은 사용자 요청으로 먼저 떼어 낸 조각이었다, run46.)
  - 다음 항목: **Q78c** (프로세스 객체).
  - 남은 동적 질문: D1 은 원래 있던 **Q61** 에 합쳤고, D2 ~ D10 은 새 항목 **Q92** 로 맨 뒤에 붙었다.
- 근거:
  - run 문서 [`run47-section-chain.md`](run47-section-chain.md) (가장 중요)
  - 요청서 [`_workspace/sast/41_section-chain.md`](../../_workspace/sast/41_section-chain.md)
  - 결과서 [`_workspace/sast/41_section-chain_result.md`](../../_workspace/sast/41_section-chain_result.md) (판정: **분석성공**)
  - 짐 지도 [`baggage.md`](baggage.md) 의 고친 곳: 2장 숫자 · 2.1 · 3.20 ~ 3.24 · 7.2 · 7.10
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (그림 1 · 2.1 · 4장 · 4.1 · 5장)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 값을 맞춰 보려고 run44 K1 덤프 읽기 로그 (`ms/logs/04_baggage-read_K1.txt` 1381 ~ 1500줄) 를 **다시 읽었다**. 새로 VM 을 돌린 건 없다.
  - 그래서 이 글의 "사진" 은 전부 run44 때 찍어 둔 K1 사진이다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 핸들(**손잡이**) → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 이미지 섹션 (**조립식 건물 세트**) → [run9 해설 "이미지 섹션"](../02_sprint/run9-create-user-process-해설.md#이미지-섹션)
- 섹션 사슬 다섯 고리, 방 배치표, 프로토 PTE, 재배치 델타 → [run31 해설](../02_sprint/run31-image-section-해설.md) 2.4 · 2.8 · 2.10
- 컨트롤 영역 (설계도 서류함의 **서랍**), 파일 객체, "작성 중" 팻말, 서랍 공유 갈림길 → [run31 해설 2.5 "새 말 — 컨트롤 영역"](../02_sprint/run31-image-section-해설.md#새-말--컨트롤-영역)
- 섹션 객체 (**주문 꾸러미**, 안에 "서랍 몇 번" 이 적힘) → [run31 해설 2.6 "새 말 — 섹션 객체"](../02_sprint/run31-image-section-해설.md#새-말--섹션-객체)
- 세그먼트와 이미지 정보 (배치표 묶음 맨 앞 **요약 카드**) → [run31 해설 2.9 "그 칸은 어디 사나"](../02_sprint/run31-image-section-해설.md#그-칸은-어디-사나-정적-pdb-타입)
- 짐 · 짐 지도 · 줄기 · 공용 부품, 짐 지도 "쓰는 말" (공간 · 단계 · 놓는 법 · 쓴 주인) → [run43 해설 2.2](run43-baggage-checklist-해설.md#22-읽기-0단계--짐-지도-맨-위-쓰는-말)
- 사진 지점 K1, "처음 값을 갖는 구간" (`K1 이전` · `끝까지 0`), "끝까지 0 은 아무도 안 씀과 같지 않다" → [run44 해설 2.10](run44-baggage-snap-해설.md#210-새-말--처음-값을-갖는-구간)
- C6 (다른 모듈의 섹션·파일 객체 = **옆방 가구**) → [run45 해설 2.6](run45-work-list-해설.md)

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 주소 공간 = 가게의 **빈 땅**, 짐 = **개업 준비물**.
- exe 파일 = **설계도**. `IopCreateFile` = **설계도 꺼내 오기** (run9). 디스크 = 설계도가 꽂힌 **서고** (run31 2.7).
- 컨트롤 영역 = 서류함의 **서랍**, 그 안에 **배치표 원본**. 세그먼트 = 배치표 묶음, 이미지 정보 = 맨 앞 **요약 카드** (run31).
- 섹션 객체 = **주문 꾸러미**. 손잡이(핸들)를 달아 건넨다 (run31).
- run 문서가 이번에 쓴 비유 ("가구 공장": 창고·대출 카드·창고지기·배치표 사본 봉투) 는 이 글에서 이렇게 바꿔 부른다:
  - **창고 → 서고**, **창고지기 → 서고지기**. run46 해설에서 **창고 = 프로세스 힙**이었기 때문이다. 둘이 섞이면 헷갈린다.
  - **배치표 사본 봉투 → 주문 꾸러미** (run31 과 같은 말).
  - **대출 카드** (파일 객체) 는 그대로 쓴다.
- 이 글이 더하는 비유는 새 말이 나올 때 하나씩 붙인다.

### 한 줄로 먼저: 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| 다섯 구조체 모르는 칸 | **74 → 여전히 모름 0** |
| `_SECTION` 객체를 만드는 곳 | 사슬 **안**이 아니라 사슬이 끝난 뒤의 **형제** `MiCreateSection +0x125 MiFinishCreateSection` |
| `_SECTION` 칸을 채우는 법 | 스택 패킷에 모아 둔 값을 객체에 **한 번에 통째 복사** (`+0x15b ~ +0x179`) |
| 컨트롤 영역·세그먼트·이미지 정보 칸 | 거의 **`MiBuildImageControlArea` 한 함수** (새 길) |
| exe 파일 객체를 만드는 곳 | `IopCreateFile → ObOpenObjectByNameEx → ObpLookupObjectName → IopParseDevice +0xe1e IopAllocRealFileObject` |
| 파일 시스템 칸 4개 (`Vpb`·`FsContext`·`FsContext2`·`SectionObjectPointer`) | **nt 에 쓰는 줄이 없다** `[정적]`. 쓰는 쪽은 ntfs.sys `[추측]` |
| test.exe 가 새 길인가 공유 길인가 | **사진 값으로 못 가른다** (두 길이 같은 숫자를 만든다) → Q61 |
| 정정 | `NumberOfUserReferences` +1 은 `MiReferenceControlArea +0x11d` (짐 지도·run31 문구) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

시간으로 **맨 앞**이다. 가게(프로세스)를 짓기도 전에, 관리사무소가 설계도를 꺼내 오고 조립식 세트를 준비하는 단계다. run31 이 "세트를 어떻게 짜나" 를 봤다면, 이번엔 **그 결과물의 칸 하나하나를 누가 채웠나** 를 본다.

오프셋은 모두 돌아올 자리(ret) 다.

```
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)

 커널, 부모 스레드 안: NtCreateUserProcess
  ├─ +0x6cb  IopCreateFile                  설계도 꺼내 오기 → 대출 카드 (_FILE_OBJECT)        ← 이번
  ├─ +0x93c  MmCreateSpecialImageSection    조립식 세트 준비
  │     └─ +0xd4 MiCreateSection
  │           ├─ (1) +0xf8   …사슬…         서랍·배치표·요약 카드 (_CONTROL_AREA·_SEGMENT·이미지 정보)  ← 이번
  │           └─ (2) +0x125  MiFinishCreateSection   주문 꾸러미 (_SECTION)                        ← 이번
  ├─ +0xc36  PspAllocateProcess             가게 짓기                                   (사진 K1 은 이 뒤, PspAllocateThread 앞)
  └─ …
 test.exe 메인 스레드: ntdll 로더 … NtContinue 은퇴 → (CRT) → main
```

- flow.md 로는 **2.1** 의 `nt!IopCreateFile` 절과 `nt!MiCreateSection` 절, 그림 1 의 `s2`·`s3` 아래다.
- 짐 지도로는 **3.20 ~ 3.24** 다섯 절이다. 단계는 전부 `①` (커널 단계).

### 무엇이 궁금했나

run44 사진은 이 다섯 구조체 칸이 거의 다 **`K1 이전`** 에 값이 있다는 것만 보여 줬다. **"언제"** 는 알았는데 **"누가"** 는 몰랐다 (run44 해설 2.10 "구간은 언제만 말한다").

그래서 요청서 질문은 여섯이다.

1. `_SECTION` 객체를 만들고 칸을 채우는 줄
2. 컨트롤 영역·세그먼트 칸, 그리고 **두 길** (새로 만들기 / 같이 쓰기) 에서 각각 누가 쓰나
3. 이미지 정보 남은 10칸
4. exe 파일 객체 30칸과 **주인 셋** (nt / 파일 시스템 / 0)
5. 칸마다 경로 (짐 지도 3장 양식)
6. 읽는 입력, 갈림길

- 범위 밖: 프로세스 객체 (`_EPROCESS` 등, Q78c), 매핑 (`MmMapViewOfSection`·VAD), 서브섹션·프로토 PTE 표 안 내용, 파일 시스템 내부 (ntfs.sys 는 안 뽑음).

### 바퀴 전에 알던 것

- `[정적]` (run31 / sast26) 섹션 사슬 다섯 고리의 call/ret. `MiBuildImageControlArea` 가 컨트롤 영역 + 서브섹션, 세그먼트 `0x98` 바이트를 풀에 만든다. `TransferAddress` 는 `+0x16b ~ +0x172`.
- `[정적]` (run9) exe 열기 = `NtCreateUserProcess +0x6cb IopCreateFile`. 다만 `+0x740 IoCreateFileEx` 도 있어서 어느 쪽이 진짜 여는지는 확인이 필요했다.
- `[관찰]` (run44) 다섯 구조체 칸 거의 다 `K1 이전`. 뒤에 또 바뀐 칸 셋 (`NumberOfPfnReferences` 3 → `0x2d`, `FilePointer` 아래 비트 `P0~P1`, `_FILE_OBJECT.Flags` `0xc0042` → `0xc4442` `K3~P0`).
- `[추측]` (요청서) `FsContext`·`FsContext2`·`SectionObjectPointer` 는 파일 시스템 (ntfs.sys) 이 채운다.
- 짐 지도 (넷째판) 에서 이 다섯 구조체는 모르는 칸 **74** (`_SECTION` 9 · `_CONTROL_AREA` 15 · `_SEGMENT` 10 · 이미지 정보 10 · `_FILE_OBJECT` 30).

---

## 2. 찾은 것

### 2.1 한눈에: 두 사슬, 서로 다른 함수가 만든다

run 문서 "호출 관계" 를 줄였다. 모두 `[정적]`. `(n)` 은 트리 안 코드 순서다 (flow.md 2.1 번호와 다름).

```
NtCreateUserProcess
 ├─ (1) +0x6cb  IopCreateFile                         대출 카드 만들기
 │       └─ 조각 +0x6f  ObOpenObjectByNameEx
 │               ├─ +0x224  ObpLookupObjectName       이름 따라가기 → 장치
 │               │       └─ +0xbcd  IopParseDevice    (장치 형식의 parse 프로시저)
 │               │               ├─ +0xe1e  IopAllocRealFileObject   카드 할당·칸 채우기
 │               │               └─ +0xf5f  IofCallDriver            → 서고지기 (ntfs.sys [추측])
 │               └─ +0x40a  ObpCreateHandle
 │  (+0x740 IoCreateFileEx — 위가 실패했을 때만)
 ├─ (2) +0x791  ObReferenceObjectByHandle              카드를 다시 집어 둠 (Q78c 로 이어짐)
 ├─ (3) +0x93c  MmCreateSpecialImageSection
 │       └─ +0xd4  MiCreateSection                    (+0x5d memset 패킷 0xc8)
 │               ├─ (1) +0xf8   MiCreateImageOrDataSection
 │               │       ├─ +0x1a3  MiReferenceControlArea      갈림길. 공유 길이면 카운터 +1
 │               │       ├─ +0x1d5  MiShareExistingControlArea  (공유 길)
 │               │       └─ +0x385  MiCreateNewSection          (새 길) → … MiBuildImageControlArea
 │               └─ (2) +0x125  MiFinishCreateSection       _SECTION 객체 (패킷 통째 복사)
 └─ (4) +0x11d5 PspDeleteCreateProcessContext          exe 핸들 닫기 (Flags 0x4400 후보 [추측])
```

- 한 줄 요약: **설계도를 꺼내며 대출 카드를 만들고 → 서랍·배치표를 그리고 → 마지막에 주문 꾸러미를 만든다.**
- 세 묶음의 "주로 쓰는 사람" 이 각각 다르다:
  - 대출 카드 (`_FILE_OBJECT`) → `IopAllocRealFileObject`·`IopParseDevice` (+ 서고지기)
  - 서랍·배치표·요약 카드 → 거의 `MiBuildImageControlArea`
  - 주문 꾸러미 (`_SECTION`) → `MiFinishCreateSection`

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: 주문 꾸러미를 만드는 줄

run 문서 호출 지점 표의 `nt!MiCreateSection | +0x120 | +0x125 | nt!MiFinishCreateSection` 줄.

- **누가**: `MiCreateSection`
- **어디서**: `+0x120` 에서 부르고, 돌아올 자리는 `+0x125`
- **누구를**: `MiFinishCreateSection`
- **무엇을 위해**: `_SECTION` 객체 (주문 꾸러미) 를 만들려고. 안에서 `+0x143` (ret `+0x148`) `ObCreateObjectEx` 를 부른다 `[정적]`.
- 눈여겨볼 점: 바로 앞 형제 `+0xf8 MiCreateImageOrDataSection` (서랍·배치표 사슬) 이 **다 끝나고 돌아온 뒤**다. 사슬 **안**이 아니다 → 2.5.

#### 한 줄 읽기 ②: 대출 카드를 만드는 줄

run 문서 호출 지점 표의 `nt!IopParseDevice | +0xe19 | +0xe1e | nt!IopAllocRealFileObject` 줄.

- **누가**: `IopParseDevice`
- **어디서**: `+0xe19` 에서 부르고, 돌아올 자리는 `+0xe1e`
- **누구를**: `IopAllocRealFileObject`
- **무엇을 위해**: exe 파일 객체를 할당하고 칸을 채우려고 `[정적]`.
  - 안에서 `+0x16a` (ret `+0x16f`) `ObpAllocateObject` (크기 `0xd8`, 형식 `IoFileObjectType`) → `+0x21e` (ret `+0x223`) `memset(…, 0xd8)` → 칸 채우기.
  - **룩어사이드** (같은 크기 조각을 모아 두고 다시 쓰는 재활용 선반. 일반적으로 알려진 설명 `[추측]`) 에서 받으면 `ObpAllocateObject` 는 안 부른다 `[정적]`. 어느 쪽이든 `memset` 은 지난다.
- 같은 함수를 `+0x10d7` (ret `+0x10dc`) 에서도 부르는데, 그건 `QueryOnly` 갈래다. `IopCreateFile +0x29e` 가 `QueryOnly` 를 0 으로 두니 exe 는 안 탄다 `[정적]`.

#### 한 줄 읽기 ③: 값이 0 인데 "nt 가 씀" 인 줄

CALL 이 아니라 **값을 쓰는 줄**이다. 짐 지도 3.20 의 `+0x18 StartingVpn` 줄: `MiFinishCreateSection +0x17d`.

- **누가**: `MiFinishCreateSection`
- **어디서**: `+0x17d`
- **무엇을**: 꾸러미 `+0x18` (`StartingVpn`) 에 **0 을 쓴다** `[정적]`. 사진 값도 `끝까지 0`.
- **왜 이 줄이 중요한가**: 값만 보면 "아무것도 안 한 칸" 같다. 그런데 코드에는 0 을 **일부러 쓰는** 줄이 있다. 이게 2.4 의 "0 을 씀" 과 "아무도 안 씀" 의 차이다.

### 2.3 74칸은 어디로 갔나

run 문서 맨 앞 표 그대로다.

| 구조체 | 모르는 칸 | 아는 칸 (nt) | 0 그대로 | 다른 드라이버 | nt 함수·FS 가 부름 | 범위 밖 길 | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `_SECTION` | 9 | 5 | 4 | 0 | 0 | 0 | 0 |
| `_CONTROL_AREA` | 15 | 8 | 5 | 0 | 0 | 2 | 0 |
| `_SEGMENT` | 10 | 8 | 2 | 0 | 0 | 0 | 0 |
| `_SECTION_IMAGE_INFORMATION` | 10 | 3 (겹침 2 + `ImageFlags`) | 7 | 0 | 0 | 0 | 0 |
| `_FILE_OBJECT` | 30 | 8 | 15 | 4 | 3 | 0 | 0 |
| **합** | **74** | **32** | **33** | **4** | **3** | **2** | **0** |

열 뜻 (run 문서 그대로):

- **아는 칸 (nt)**: nt 에서 그 칸을 쓰는 줄을 찾음.
- **0 그대로**: 이 실행에서 값 0. 이유는 셋 중 하나 → 바로 다음 2.4.
- **다른 드라이버**: nt 에 쓰는 줄이 없음 `[정적]`, 쓰는 쪽은 파일 시스템 (ntfs.sys) `[추측]` → 2.11 · 2.12.
- **nt 함수·FS 가 부름**: 쓰는 코드는 nt (`IoSetLinkShareAccess`) 인데 부르는 쪽이 파일 시스템 `[추측]` → 2.11.
- **범위 밖 길**: 쓰는 길이 매핑·페이지 폴트 쪽 (이름만). `_CONTROL_AREA` 의 `NumberOfPfnReferences`·`NumberOfMappedViews` 둘.

#### 한 줄 읽기: `_FILE_OBJECT` 줄

- 모르던 30칸 중 nt 가 쓰는 줄을 찾은 게 8, 이 실행에서 0 인 게 15, 서고지기 몫 4, "nt 코드인데 서고지기가 부름" 3.
- 8 + 15 + 4 + 3 = 30. "여전히 모름" 0.
- 다섯 구조체 중 **주인이 nt 밖까지 퍼진 건 이것 하나뿐**이다. 대출 카드는 관리사무소와 서고지기가 **같이** 채우는 카드라서다.

### 2.4 새 말: "0" 에는 세 가지가 있다 (0 을 씀 / 아무도 안 씀)

#### 왜 따로 봐야 하나

- run44 해설 2.10 에서 "**끝까지 0 은 아무도 안 씀과 같지 않다**" 고 했다. 사진으로는 못 가린다는 뜻이었다.
- 이번엔 코드를 봤으니 **가를 수 있다.** 사진 값이 0 인 칸 33개를, 0 이 된 이유로 셋으로 나눈다 (run 문서 열 뜻 그대로):

| 0 이 된 이유 | 뜻 | 비유 | 예 (run 문서) |
|---|---|---|---|
| 쓰는 줄이 조건부라 안 탐 | 코드엔 쓰는 줄이 있는데, 이번 조건이 안 맞아 지나치지 않았다 | 양식에 "해당자만 기입" 칸. 해당이 아니라 비워 둠 | `_SECTION.SectionNode`·`EndingVpn` (`SEC_BASED` 일 때만), `SessionId` |
| 코드가 0 을 씀 | 코드가 일부러 0 을 적는다 | 칸에 직접 **"0"** 이라고 써넣음 | `StartingVpn` (`+0x17d`), `_SEGMENT.SegmentLock`, `_CONTROL_AREA.FileObjectLock` |
| 할당 때 0 으로 채운 그대로 | 쓰는 줄이 아예 없다. 받은 메모리가 0 이라 0 | 새 양식을 받았더니 처음부터 빈칸이었고, **아무도 펜을 안 댔다** | `_CONTROL_AREA.ModifiedWriteCount`, `_FILE_OBJECT` 8칸 (`memset` 만) |

#### 짐 지도에서는 이렇게 센다

run 문서 "다시 가른 것" 그대로.

- 위 두 줄 (조건부 안 탐 · 0 을 씀) = **nt 에 쓰는 줄이 있음** → 짐 지도 **"nt 가 씀"** 으로 센다. **22칸**.
- 마지막 줄 (할당 때 0) = **쓰는 줄 없음** → 짐 지도 **"아무도 안 씀"**. **11칸**:
  - `_CONTROL_AREA.ModifiedWriteCount`
  - `_SECTION_IMAGE_INFORMATION.ZeroBits`·`ImageFlags.Reserved` (`Reserved` 는 "못 찾음" 이라 `[추측]`)
  - `_FILE_OBJECT` 8칸: `PrivateCacheMap`·`FinalStatus`·`LockOperation`·`DeletePending`·`Busy`·`LastLock`·`CompletionContext`·`FileObjectExtension`
- 짐 지도 정의 (run47 에 생긴 말): "**아무도 안 씀** = 쓰는 줄이 없고, 할당 때 0 으로 채워진 그대로. 아는 칸도 모르는 칸도 아니라 2장 아래 줄에서 따로 센다."

#### "할당 때 0" 은 어떻게 아나

- `_FILE_OBJECT`: `IopAllocRealFileObject +0x21e` 가 `memset(…, 0xd8)` 로 카드 전체를 지운다 `[정적]`.
- 컨트롤 영역·세그먼트: `MiAllocatePool` 로 받은 풀은 **0 으로 채워져 온다**. 플래그 `0x40`·`0x100` 에 "초기화 안 함" 비트가 없어서다 `[정적]`. (플래그 비트의 뜻은 `ExAllocatePool2` POOL_FLAGS 이름 기억이라 `[추측]`.)
  - 프로토 PTE 풀만 그 비트가 있어서 따로 `memset` 한다 `[정적]`.
- `_SECTION`: 꾸러미에 복사할 쪽지 (스택 패킷) 를 `MiCreateSection +0x58` 이 `memset` 으로 지운다 `[정적]` → 2.6.

#### 이 구분이 에뮬레이터에 왜 쓸모 있나 (내 짐작)

- `[추측]` 이유: 에뮬레이터는 진짜 nt 와 **같은 이름·호출 관계**로 짓는다 (CLAUDE.md). 그러니
  - "nt 가 씀" 칸 = 그 이름의 함수 **몇 번째 줄**이 쓰는지 있다 → 에뮬레이터의 같은 함수가 맡을 자리가 정해진다. "0 을 씀" 도 이 함수가 맡는다.
  - "조건부 안 탐" 칸 = 조건이 바뀌면 값이 생긴다 → 갈림길을 같이 흉내 내야 한다.
  - "아무도 안 씀" 칸 = 맡을 함수가 없다 → 메모리를 0 으로 받기만 하면 된다.
- 어디까지 흉내 낼지는 목록이 다 된 뒤 사용자와 정한다 (CLAUDE.md "지금 목표"). 여기선 **분류만** 한 것이다.

### 2.5 주문 꾸러미는 사슬 끝 "형제" 가 만든다 (`MiFinishCreateSection`)

#### 무엇이 바뀌었나

- 전 (짐 지도 3.20 "만드는 길", run31 근거): `MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap`. 즉 **사슬 안쪽** 어딘가에서 만든다고 봤다. 칸 단위 근거는 없었다.
- 후 (run 문서 "정정") `[정적]`:
  1. `MiCreateSection` 이 `+0xf8` 에서 `MiCreateImageOrDataSection` 을 부른다 → 서랍·배치표 사슬이 돈다.
  2. 그게 **돌아온 뒤**, 같은 `MiCreateSection` 이 `+0x125` 에서 `MiFinishCreateSection` 을 부른다.
  3. 거기서 `+0x143 ObCreateObjectEx` (`MmSectionObjectType`, 몸통 `0x40`) 로 꾸러미를 받는다.
  4. 사슬 안 (`MiCreateNewSection` 이하) 에서는 섹션 객체를 **안 만든다.**
- 그 뒤 손잡이는 `MmCreateSpecialImageSection +0x141 ObInsertObjectEx` 가 단다 (run31 에서 본 것 그대로).

- 비유: 서랍에 배치표 원본을 다 넣고 나서야, 창구 직원이 **빈 꾸러미 한 개**를 받아 와 "서랍 몇 번" 을 적어 건넨다. 배치표를 그리는 사람과 꾸러미를 싸는 사람이 다르다.

#### 새 말: 객체 만들기 `ObCreateObjectEx`

- **`ObCreateObjectEx`** = 커널 객체 하나를 새로 받는 함수. 부르는 쪽이 **객체 형식** (여기선 `MmSectionObjectType` = "섹션" 종류) 과 **몸통 크기** (`0x40`) 를 넘긴다 `[정적]`.
  - 일반적으로 알려진 설명 `[추측]`: 객체 관리자가 앞에 **객체 머리** (`_OBJECT_HEADER`) 를 붙인 메모리를 주고, 몸통 칸 채우기는 부른 쪽 몫이다.
  - 이번 근거에서 객체 머리는 **범위 밖**이고, `ObCreateObjectEx` 는 지도의 **공용 부품** (helper) 이라 이름으로만 가리킨다 (요청서·결과서).
- 비유: 관리사무소의 **공용 서류철 발급 창구**. "섹션용, 이 크기" 하면 종류 도장 찍힌 빈 서류철을 내준다. 안을 채우는 건 받아 간 사람이다.
- 파일 객체 쪽의 같은 일은 `ObpAllocateObject` (`0xd8`, `IoFileObjectType`) 다 (2.2 한 줄 읽기 ②).

#### 왜 찾기 어려웠나

- 요청서 표에 이 함수가 **없었다.** 결과서가 `MmSectionObjectType` 을 쓰는 곳을 `refs` 로 거꾸로 찾아서 나왔다 (결과서 맨 앞).
- 요청서 질문 1 은 "할당이 사슬 (`… → MiCreateNewSection / MiShareExistingControlArea`) 중 어디인가?" 였다. 답은 "그 중 어디도 아니고 **옆**" 이었다. run31 에서 진입점 줄이 "`MiVerifyImageHeader` 아래가 아니라 옆" 에 있었던 것과 같은 모양이다.

### 2.6 새 말: 스택 쪽지에 모았다가 한 번에 복사

#### 뜻

- `MiCreateSection` 은 시작할 때 스택에 **만들기 패킷** (0xc8바이트) 을 두고 `+0x58 memset` 으로 0 으로 지운다 `[정적]`. (run31 해설의 "만들기 패킷" 과 같은 것이다.)
- 사슬이 도는 동안 꾸러미에 들어갈 값을 이 패킷의 **`+0x50 ~ +0x8f`** (64바이트) 에 모은다 `[정적]`.
- 꾸러미를 받으면 `MiFinishCreateSection +0x15b ~ +0x179` 가 `MOVUPS` 4번 (16바이트씩) 으로 패킷 `+0x50 ~ +0x8f` 를 꾸러미 `+0x00 ~ +0x3f` 에 **통째로** 옮긴다 `[정적]`.
  - 그래서 **패킷 `+0x50 + X` = `_SECTION + X`** 다 (짐 지도 3.20).
  - 그 뒤 `+0x17d` 에서 `StartingVpn` = 0, `+0x1b3 ~ +0x1ba` 에서 `u.UserReference` 를 켠다.
- 비유: 꾸러미가 아직 없으니, 꾸러미에 적을 내용을 **책상 위 쪽지**에 먼저 적어 둔다. 쪽지 칸 배치는 꾸러미 칸 배치와 **똑같다.** 꾸러미가 오면 쪽지를 대고 **통째로 베껴 쓴다.**
  - flow.md 는 같은 그림을 "메모지에 적어 둔 걸 봉투에 한 번에 옮겨 적기" 라고 썼다. 이 글에서 "메모지" 는 run31 의 검사관 칸 모음 (`MiVerifyImageHeader` 의 0x70) 이라, 헷갈리지 않게 **쪽지**라고 부른다.

#### 칸마다 "값이 정해지는 줄" 과 "꾸러미에 쓰이는 줄" 이 다르다

이게 이 방식의 핵심이다. 칸 하나에 줄이 **두 개** 붙는다: 쪽지에 넣는 줄 + 복사 줄. 짐 지도 3.20 경로 끝에 "복사" 가 붙는 이유다.

| 꾸러미 칸 | 쪽지에 값을 넣는 곳 | 사진 (K1) | 태그 |
|---|---|---|---|
| `+0x28` `u1` | `MiFinishCreateSection +0x2a` (컨트롤 영역 주소) | `…983992b0` | `[정적]` |
| `+0x30` `SizeOfSection` | 새 길 `MiCreateNewSection +0x21d/+0x228` · 공유 길 `MiShareExistingControlArea +0x104/+0xac` | `0x8c000` | `[정적]` |
| `+0x38` `u` | `MiFinishCreateSection +0x31 ~ +0x43` (컨트롤 영역 `u` 복사), 복사 뒤 `+0x1b3 ~ +0x1ba` `UserReference` | `0x000100a0` | `[정적]` |
| `+0x3c` `InitialPageProtection` | `MiInitializeCreateSectionPacket +0x68 · +0xd7` (값 `0x10`) | `0x10` | `[정적]` |
| `+0x3c` `NoValidationNeeded` | `MiFinishCreateSection +0x243 ~ +0x247` (서명 수준 ≤ 1 일 때) | 1 | `[정적]` |

- 한 줄 읽기: `u` 칸. 사진 `0x000100a0` = 컨트롤 영역 `u` `0xa0` + `UserReference` (run 문서 "확인된 것"). 즉 **서랍 쪽 표시를 그대로 베끼고, 꾸러미 쪽 표시 하나를 더 켠** 값이다.
  - `u1` 도 같은 결: 꾸러미에 적힌 건 **서랍 주소**다. run31 해설에서 "꾸러미 안엔 '서랍 몇 번' 이 적혀 있다" 고 한 게 바로 이 칸이었다.
- 0 인 네 칸 (`SectionNode`·`EndingVpn`·`StartingVpn`·`SessionId`) 은 2.4 표 그대로: 셋은 조건부 안 탐, `StartingVpn` 은 0 을 씀.

#### 이 방식 때문에 사진으로 못 보는 것

- 사진 K1 은 이미 **복사가 끝난 뒤의 최종 값**이다. 쪽지에 값이 맞게 모였는지 (복사 직전) 는 사진으로 못 본다 → 동적 **D2** (`nt+0x96dd64` = `MiFinishCreateSection +0x148`, 앞뒤로 쪽지와 꾸러미 찍기, Q92 낮은 우선순위).

### 2.7 서랍·배치표·요약 카드: 거의 한 사람이 쓴다

#### 새 길에서는 `MiBuildImageControlArea` 가 거의 다 `[정적]`

- 풀 3개를 받는다: `+0x8d` (태그 `MmCi`, 컨트롤 영역 + 서브섹션) · `+0xdb` (`MmSt`, 프로토 PTE) · `+0x107` (`MSeg`, 세그먼트 `0x98`).
- 그리고 칸 대부분을 이 함수 안에서 쓴다. 대표:
  - 서랍: `Segment` `+0x1f8`, `NumberOfSectionReferences` = 1 `+0x20b`, `NumberOfUserReferences` = 1 `+0x214`, `LockedPages` = 1 `+0x207`, `u` (`Image`·`File`·`BeingCreated`) `+0x9e`·`+0x222`.
  - 배치표 묶음 (세그먼트): `ControlArea` `+0x138`, `TotalNumberOfPtes` `+0x13b` (`SizeOfImage` 쪽 수 `0x8c`), `SizeOfSegment` `+0x145 ~ +0x149`, `PrototypePte` `+0x134`, `BasedAddress` `+0x25e ~ +0x261` (헤더 `ImageBase`).
- 비유: 서랍·배치표·요약 카드는 **같은 사람이 한 자리에서** 거의 다 채운다. 나머지는 몇 명이 한두 칸씩 보탠다.

#### 한두 칸씩 보태는 사람들 `[정적]`

| 누가 | 무엇 |
|---|---|
| `MiParseImageSectionHeaders` | `u3` (구역 쪽마다 +1), `ImageCommitment`, `DefaultProtectionMask`, (조건부) `NumberOfCommittedPages` |
| `MiCreateImageFileMap` | `u1.PartitionId` = 0, (조건부) `Binary32`·`BaseBelow4gb` |
| `MiSectionCreated` | `FilePointer`·`WaitList`, `BeingCreated` 끄기 |
| `MiSelectImageBase`·`MiRelocateImage` | `u2` 재배치 칸, `BasedAddress` 를 새 기본 주소로 (사진 `0x7ff61a370000`), `ImageActive` |

#### "작성 중" 팻말이 진짜 서랍으로 바뀌는 순간 (`MiSectionCreated`)

run31 해설 2.5 의 "작성 중" 팻말 이야기가 이번에 칸 단위로 이어졌다 `[정적]`.

1. 서랍이 비었으면 `MiReferenceControlArea +0x178` 이 **자리표시용 컨트롤 영역** (팻말) 을 파일의 `ImageSectionObject` 칸에 꽂는다.
   - 팻말은 `MiCreateImageOrDataSection` 의 스택 위 0x80바이트다. 짐이 아니라 임시다 (짐 지도 3.21 정정 줄).
   - 팻말의 `+0x40` 에는 파일 객체 주소가 적혀 있다 (`MiCreateImageOrDataSection +0x120`).
2. 새 길이 배치표를 다 그리면 `MiSectionCreated` 가:
   - 팻말의 파일 객체 주소를 진짜 서랍 `FilePointer` 로 옮기고 (`+0x6f ~ +0x7a`),
   - 팻말의 `WaitList` 를 옮기고 (`+0x11b`. 기다린 사람이 없으면 0, 사진 null),
   - `BeingCreated` 를 끄고 (`+0xa6`),
   - 파일의 `ImageSectionObject` 칸을 **진짜 서랍**으로 바꿔 꽂는다 (`+0x124`).
- 비유: 서랍 자리에 꽂아 둔 "작성 중" 팻말 뒷면에 "이 설계도 대출 카드 번호" 를 적어 두었다. 배치표가 다 되면 그 번호를 진짜 서랍 이름표로 옮겨 적고, 팻말을 뽑고 진짜 서랍을 꽂는다.
- 그런데 `FilePointer` 사진 값 `…dc5d` 의 아래 4비트 `0xd` 는 이 줄이 쓴 게 아니다. 쓴 값은 포인터만 (아래 4비트 0) 이고, 아래 비트는 뒤에 참조 수로 바뀐 흔적이다 (`_EX_FAST_REF` `RefCnt`, 범위 밖 길 `[추측]`, D5).

#### 요약 카드 (이미지 정보) 남은 10칸 `[정적]`

- **겹침 2**: `SubSystemVersion` (4바이트) 은 `SubSystemMinorVersion` + `SubSystemMajorVersion` 과 **같은 바이트**다. `OperatingSystemVersion` 도 같은 모양 (`[정적] PDB`). 그래서 이미 아는 Minor/Major 줄 (`MiBuildImageControlArea +0x193 ~ +0x1b1`) 이 곧 쓰는 줄이다.
  - 비유: 카드에 "버전 6.0" 칸과, 같은 자리를 둘로 쪼갠 "큰 번호 6 / 작은 번호 0" 칸이 **겹쳐 인쇄**돼 있다. 한쪽에 쓰면 다른 쪽도 채워진다.
- **`ImageFlags` 비트**: `ImageDynamicallyRelocated` 말고는 **전부 조건부**다 (COM 디렉터리 · 플랫 매핑 요청 · 4GB 아래 요청이 있을 때만). test.exe 는 그런 게 없어서 `0x4` 하나만 켜진다.
- **`ZeroBits`**: 쓰는 줄 없음 → 아무도 안 씀.
- 덤: 짐 지도 3.23 의 **"오프셋 미상" 14줄**이 이번에 채워졌다 (`MiBuildImageControlArea +0x167 ~ +0x1f0`).

### 2.8 두 길 (새로 만들기 / 같이 쓰기): 각자 무엇을 쓰나, 왜 사진으로 못 가르나

갈림길 자체는 run31 해설 2.5 에서 봤다 (서랍이 비었으면 새로 그리기, 있으면 같이 쓰기). 이번엔 **두 길이 남기는 칸이 어떻게 다른가** 를 봤다.

#### 두 길이 쓰는 것 `[정적]`

| | 새 길 (`MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea`) | 공유 길 (`MiReferenceControlArea` → `MiShareExistingControlArea`) |
|---|---|---|
| 서랍·배치표 칸 | **거의 다 새로 쓴다** (2.7) | **안 건드린다** |
| `NumberOfSectionReferences` | 1 을 씀 (`MiBuildImageControlArea +0x20b`) | +1 (`MiReferenceControlArea +0x107`) |
| `NumberOfUserReferences` | 1 을 씀 (`MiBuildImageControlArea +0x214`) | +1 (`MiReferenceControlArea +0x11d`) |
| 꾸러미 `SizeOfSection` | `MiCreateNewSection +0x21d/+0x228` | `MiShareExistingControlArea +0x104/+0xac` |

- 비유: 새 길은 서랍을 새로 짜고 배치표를 처음부터 그린다. 공유 길은 서랍 앞 "사용자 명단" 에 **이름 두 줄만** 보태고 그대로 가져다 쓴다.

#### 사진으로 못 가르는 이유: 단계별로

1. 사진 K1 값 `[관찰]` (run44): `NumberOfSectionReferences` 1 · `NumberOfUserReferences` 2 · `NumberOfMappedViews` 1 · `LockedPages` 1 · `u` `0xa0`.
2. 새 길이라면: 섹션 참조 1 (`+0x20b`), 사용자 참조 1 (`+0x214`). 그 위에 이 프로세스의 exe 매핑이 사용자 참조를 1 더 올려 **2** (매핑 쪽은 `[추측]`, 매핑 코드는 안 읽음).
3. 공유 길이라면: 이전 프로세스의 섹션이 이미 닫혀 서랍이 놀고 있던 경우, 섹션 참조 0 → **1** (`+0x107`), 사용자 참조 0 → 1 (`+0x11d`), 그 위에 매핑 → **2**.
4. → **같은 숫자**다. `LockedPages` 1 · `u2` · `BasedAddress` 도 두 길에서 같다 (결과서).
5. 그래서 → 사진 한 장으로는 못 가른다. 아직 확인 아님.

- 비유: 명단에 이름이 둘 적혀 있다. 서랍을 새로 짜며 처음 적은 이름인지, 빈 서랍에 다시 적은 이름인지는 **명단만 봐서는** 모른다.

#### 가르는 방법 (D1 → Q61)

- 멈춤점: `nt+0x96f4d0` (`MiCreateNewSection` 입구) / `nt+0x96e6cc` (`MiShareExistingControlArea` 입구) 중 서는 쪽.
- **사진으로도 된다**: test.exe 를 **재부팅 없이 두 번** 돌려 두 K1 의 `_SECTION.u1` (= 서랍 주소) 을 비교한다. 같으면 공유 (run 문서 "아직 확인 필요").
  - 비유: 두 번 받은 꾸러미에 적힌 "서랍 번호" 가 같으면, 두 번째는 같은 서랍을 같이 쓴 것이다.

### 2.9 정정: `NumberOfUserReferences` +1 은 어느 함수인가

- 전 (짐 지도 3.21, 5장 ①1.1): `MiCreateImageOrDataSection +0x11d` 가 +1.
- 후 `[정적]`: **`MiReferenceControlArea +0x11d`** (`.asm` `INC qword ptr [RBX + 0x30]`). 오케스트레이터·run 문서 둘 다 확인. `MiCreateImageOrDataSection` 에는 `[…+0x30]` 을 올리는 줄이 없다 (결과서).
- 어떻게 틀렸나 (run 문서):
  1. run31 은 `MiReferenceControlArea` 갈림길 절 안에서 "사용 수를 1 올리고 (`+0x11d`)" 라고 **함수 이름 없이** 썼다.
  2. run43 (sast/38 대조) 이 이걸 옮기며 바깥 함수 `MiCreateImageOrDataSection` 에 붙였다.
- run31 문서 맨 위에 정정 한 줄이 붙었다. 짐 지도는 이 스프린트 문서라 바로 고쳤다.
- run31 해설 2.5 도 "사용 수를 1 올리고(`+0x11d`)" 라고 함수 이름 없이 썼다. 그 절이 `MiReferenceControlArea` 세 갈래 이야기라 틀린 말은 아니지만, 읽을 때 **`MiReferenceControlArea` 안 `+0x11d`** 로 읽으면 된다.
- 같이 바뀐 것: 3.21 `u` 줄이 `[추측]` 짐작 ("자리표시용 사본에 `BeingCreated`, 진짜 칸 아님") → `[정적]` 정확 (2.7 의 진짜 칸 줄들).

### 2.10 대출 카드 사슬: 이름 따라가기와 parse 프로시저

#### 새 말: parse 프로시저

- 커널 안 물건은 `\??\…` 같은 **이름 길**로 찾는다. `ObpLookupObjectName` 이 이 이름을 앞에서부터 따라간다 `[정적]`:
  - `\??\…` → 심볼릭 링크 (`ObpParseSymbolicLinkEx`) → **장치 객체**.
- 장치에 닿으면, **남은 이름** (그 장치 안의 파일 이름) 은 장치 **형식에 등록된 Parse 함수**에게 넘긴다. 장치 형식의 Parse = `IopParseDevice` 다 `[정적]`.
  - 이 포인터는 부팅 때 `IoCreateObjectTypes +0xeb/+0xfd` 가 장치 형식에 꽂는다 (`refs`) `[정적]`.
  - 일반적으로 알려진 설명 `[추측]`: 객체 형식마다 "남은 이름을 어떻게 풀지" 를 맡는 함수 (parse 프로시저) 를 하나씩 등록해 두고, 객체 관리자는 이름이 그 형식에 닿으면 그 함수를 부른다.
- 비유: 관리사무소 **안내 데스크**가 주소를 따라간다. "별칭 표지판" (심볼릭 링크) 을 따라 어느 **건물** (장치) 에 닿으면, "그 건물 안 몇 층 몇 호" (남은 이름) 는 그 건물 종류의 **담당자** (parse 프로시저) 에게 넘긴다. 장치 건물 담당자가 `IopParseDevice` 다.

#### 그래서 지도 (`_map`) 에 없었다

- `IopParseDevice`·`IopParseFile`·`ObpLookupObjectName` 은 지도에 없다 (요청서). 형식에 꽂힌 **함수 포인터로 부르는 간접 호출**이라서다.
- 결과서가 `.asm`·`refs` 로 이었다: `ObpLookupObjectName +0xb75 LEA` 로 "장치 형식의 Parse 가 `IopParseDevice` 인가" 를 비교하고, 맞으면 **직접 호출**한다 (ret `+0xbcd` 또는 `+0xc95`) `[정적]`.

#### 사슬 (run 문서 "확인된 것") `[정적]`

1. `NtCreateUserProcess +0x6cb` `IopCreateFile`. 열기 패킷 (`_OPEN_PACKET` 0xe0) 을 채운다 (`RelatedFileObject` 0, `QueryOnly` 0).
2. 조각 `+0x6f` `ObOpenObjectByNameEx` → `+0x224` `ObpLookupObjectName` (이름 따라가기).
3. `+0xbcd` `IopParseDevice` (장치 담당자).
4. `+0xe1e` `IopAllocRealFileObject` (대출 카드 할당·칸 채우기). 이어서 `IopParseDevice` 가 `FileName` (`+0xead`·`+0xecb`)·`Event` (`+0xf1f`) 를 쓰고 `+0xf5f IofCallDriver` 로 서고지기에게 넘긴다.
5. 돌아와 `ObOpenObjectByNameEx +0x40a ObpCreateHandle` 로 손잡이, `IopCreateFile` 조각 `+0xbb` 에서 `Flags` `0x40000` (핸들 만들어짐).

#### 안 타는 두 가지 `[정적]`

- **`+0x740 IoCreateFileEx`** 는 재시도다. `+0x6cf JNS` 가 첫 번째 열기가 성공이면 건너뛴다. 그래서 **exe 를 여는 쪽은 `+0x6cb`** 이고, run9 의 말이 맞다. (test.exe 가 첫 번째 성공 쪽인 건 `[추측]`.)
- **`IopParseFile`** 은 "이미 열린 파일 밑으로 **상대 경로**로 열 때" 오는 징검다리다 (`+0xc2` 에서 `IopParseDevice` 를 부름). exe 는 절대 경로라 안 탄다 `[정적 조건]` → 동적 D8 (Q92).
  - 비유: "아까 그 건물 기준으로 옆방" 처럼 물을 때만 쓰는 샛길이다. 우리는 주소를 처음부터 다 불렀다.

### 2.11 새 말: 파일 시스템 드라이버가 쓰는 칸 (대출 카드의 주인 셋)

#### 새 말 둘

- **파일 시스템 드라이버** (여기선 ntfs.sys) = 디스크 위 파일을 실제로 관리하는 커널 드라이버. nt (ntoskrnl) 와는 **다른 모듈**이다. 비유: 서고를 관리하는 **서고지기**. 관리사무소 직원이 아니다.
  - `IopParseDevice +0xf5f IofCallDriver` 가 마운트된 볼륨의 파일 시스템을 부른다. "여기부터 ntfs.sys" (run 문서) `[정적]`. 그 안은 이번에 안 뽑았다.
- **IRP** = 그때 건네는 **일 요청서**. `IopParseDevice +0xcbc IopAllocateIrpExReturn` 이 `IRP_MJ_CREATE` (= "열어 주세요") 요청서를 만든다 `[정적]`. (I/O 요청 패킷이라는 이름과 뜻은 일반적으로 알려진 설명 `[추측]`.)
  - 비유: 관리사무소가 서고지기에게 "이 설계도 꺼내 주세요" 쪽지를 건넨다. 서고지기는 책을 찾아 주고, **대출 카드의 "서가 위치" 칸을 자기가 채운다.**

#### 주인 셋 (run 문서 "`_FILE_OBJECT` 주인 셋")

| 주인 | 칸 | 쓰는 곳 | 태그 |
|---|---|---|---|
| (가) nt | `Type`·`Size`, `DeviceObject`, `Lock`, `IrpList`, `Flags` `0x2`·`0x4` / 0 을 씀 `RelatedFileObject`·`IrpListLock`·`CurrentByteOffset`·`Waiters` | `IopAllocRealFileObject` | `[정적]` |
| (가) nt | `FileName`, `Event` | `IopParseDevice` | `[정적]` |
| (가) nt | `Flags` `0x40000` | `IopCreateFile` 조각 | `[정적]` |
| (나) 다른 드라이버 | **`Vpb`·`FsContext`·`FsContext2`·`SectionObjectPointer`**, `Flags` `0x40`·`0x80000` | nt 에 쓰는 줄이 **없다** | 줄 없음 `[정적]` · 주체 ntfs.sys `[추측]` |
| (나') nt 함수·FS 가 부름 | `ReadAccess`·`SharedRead`·`SharedDelete` (+ 0 을 쓰는 `WriteAccess`·`DeleteAccess`·`SharedWrite`) | `IoSetLinkShareAccess +0x16 ~ +0x67` (nt 코드) | 코드 `[정적]` · 부르는 쪽 FS `[추측]` |
| (다) 아무도 안 씀 | `PrivateCacheMap`·`FinalStatus`·`LockOperation`·`DeletePending`·`Busy`·`LastLock`·`CompletionContext`·`FileObjectExtension` | `memset` 만 | `[정적]` |

#### (나) "쓰는 줄이 없다" 는 어떻게 말하나: 단계별로

1. `[정적]` `IopParseDevice`·`IopAllocRealFileObject` 에 이 네 칸을 쓰는 줄이 없다. (`IopAllocRealFileObject +0x419 ~ +0x44d` 에 복사가 있지만 그건 **다시 열기** 갈래다.)
2. `[정적]` 그런데 사진엔 값이 있다 (`K1 이전`).
3. 그 사이에 불리는 다른 모듈은 `+0xf5f IofCallDriver` 너머의 파일 시스템이다.
4. 그래서 → IRP 를 받은 파일 시스템이 채웠다고 본다 `[추측]`.
5. 아직 확인 아님: 쓴 모듈이 정말 ntfs 인지는 동적 **D7** (`nt+0x8bbb8f` 에서 카드 주소를 잡고 `ba w8` 로 네 칸을 지켜보다 서면 `lm a @rip`, Q92).

#### (나') "nt 코드인데 부르는 쪽이 FS" 는 무슨 말인가

- 공유 접근 칸을 실제로 쓰는 코드는 nt 의 공개 함수 `IoSetLinkShareAccess` 다 `[정적]`.
- 그런데 nt 안에서 이 함수를 부르는 곳은 `IoSetShareAccess`·`IoSetShareAccessEx`·`IoCheckShareAccess*`·`RawCreate`·`FsRtlpCheckSharingAgainstOplockOwner` 뿐이고, `IopParseDevice` 는 아니다 (`refs`) `[정적]`.
- 그래서 → 서고지기가 관리사무소의 **공용 도장** (`IoSetShareAccess`) 을 빌려 찍은 것으로 본다 `[추측]` (D9).
- 값은 맞는다 `[정적]`: 접근 `0x100020`·공유 5 를 넣으면 `ReadAccess`·`WriteAccess`·`DeleteAccess` = 1·0·0, `SharedRead`·`SharedWrite`·`SharedDelete` = 1·0·1. K1 과 같다.
- 그래서 짐 지도는 이 칸들을 **"nt (부르는 쪽 다른 드라이버)"** 로, **nt 로 센다** (짐 지도 "쓰는 말").

### 2.12 "다른 드라이버" 분류가 에뮬레이터에 왜 중요한가

#### 짐 지도에서의 자리

- 짐 지도 "쓰는 말" (run47 에 생김): **`다른 드라이버 (ntfs.sys)` = 커널 쪽 `다른 DLL`**. 뜻이 같다 — 범위 밖, 표시만, **에뮬레이터에선 입력으로 취급**. 2장에선 `다른 DLL` 열에 센다.
- 공간도 다시 보자 ([run43 해설 2.2](run43-baggage-checklist-해설.md#22-읽기-0단계--짐-지도-맨-위-쓰는-말)): 커널 공간 = **에뮬레이터 바깥, nt 엔진·syscall 로만 닿음**.
- 이 둘을 합치면 이 칸들의 처지가 보인다:
  - 대출 카드는 커널 공간에 있다 → nt 엔진이 맡는 곳.
  - 그런데 그 안 네 칸은 nt 가 아니라 **서고지기**가 채운다 → nt 엔진 **안에 그 칸을 채울 함수가 없다.**
- 비유: 관리사무소 업무를 그대로 흉내 내는 **모형 사무소**(에뮬레이터)를 짓는데, 서고지기는 모형에 없다. 그러면 카드의 "서가 위치" 칸을 채울 사람이 모형 안에 없다.

#### 그런데 그냥 비워 둘 수가 없다: `SectionObjectPointer`

run 문서가 따로 짚은 점이다.

- `[정적]` `SectionObjectPointer` 는 **nt 가 곧바로 읽고 쓴다.**
  - `MiReferenceControlArea +0x44`·`MiSectionCreated +0x53` 이 읽는다.
  - 가리키는 블록 (`_SECTION_OBJECT_POINTERS`, 0x18, 파일 시스템 메모리) 의 `ImageSectionObject` 칸은 **nt 가 쓴다** (`MiReferenceControlArea +0x178` 팻말, `MiSectionCreated +0x124` 진짜 서랍).
- 2.7 의 "작성 중 팻말" 이 꽂히는 곳이 바로 이 블록이다. 서랍을 찾고, 공유 갈림길을 가르는 것도 이 칸을 통해서다 (run31 해설 2.5).
- → run 문서 (결과서 의견): **에뮬레이터는 ntfs.sys 가 없으니 대신 채워 줘야 하는 칸**이다.
- 비유: 서고지기가 카드에 "서랍장 위치" 를 적어 줘야, 관리사무소가 그 서랍장에 팻말을 꽂고 서랍을 넣는다. 모형 사무소엔 서고지기가 없으니 **누군가 그 칸을 미리 적어 줘야** 섹션 사슬이 돌기 시작한다.
- `[추측]` (내 짐작, 이유: 위 `[정적]` 두 줄) 에뮬레이터 쪽에서 보면 이 네 칸은 "구현할 함수가 있는 칸" 이 아니라 "**밖에서 넣어 줄 입력**" 목록에 들어간다. 다만 `SectionObjectPointer` 는 그 입력이 가리키는 블록 안을 nt 엔진이 다시 쓴다는 점이 다른 입력과 다르다.

#### 공유 접근 칸은 반대다

- (나') 칸은 쓰는 코드가 nt (`IoSetLinkShareAccess`) 다. 그래서 결과서 의견은 "쓴 주인 nt, 부르는 쪽 다른 드라이버" 로 따로 적는 편이 **PE_emulator 구현 이름과 맞는다** 였다.
- 즉 그 함수는 nt 엔진 안에 있을 수 있는 함수다. 없는 건 **부르는 사람**(서고지기) 쪽이다.

### 2.13 `Flags` 는 양쪽에 센다, 그리고 나중에 또 바뀐다

- `_FILE_OBJECT.Flags` 한 칸에 비트 주인이 둘이다 (run 문서):
  - nt 줄이 있는 비트: `0x2`·`0x4` (`IopAllocRealFileObject`), `0x40000` (`IopCreateFile` 조각) `[정적]`
  - nt 에 켜는 줄이 없는 비트: `0x40`·`0x80000` → 파일 시스템 몫 `[추측]`
  - 비트 이름은 ntifs.h 기억이라 `[추측]` (결과서).
- 짐 지도 규칙 "**한 칸을 둘이 쓰면 양쪽에 센다**" 그대로라, 짐 지도 2장 `_FILE_OBJECT` 의 다른 드라이버 열은 결과서의 4 가 아니라 **5** 다 (run 문서).
- 사진에서 `K3~P0` 에 `0xc0042` → `0xc4442` (비트 `0x4400`) 로 또 바뀌었다 `[관찰]`. 누가 켰나:
  1. `[정적]` `NtCreateUserProcess +0x11d5 PspDeleteCreateProcessContext` 가 `+0x139 ObCloseHandle` 로 exe 핸들을 닫는다. `PspInsertThread` 뒤라서 K3 다음 구간이다.
  2. `[정적]` 핸들을 닫으면 `IopCloseFile` 이 `+0x13d IofCallDriver` 로 `IRP_MJ_CLEANUP` 을 서고지기에게 보낸다.
  3. `[정적]` `IopCloseFile` 에는 `0x4000`·`0x400` 상수가 없다.
  4. 그래서 → 반납 처리 중에 서고지기가 켰다고 본다 `[추측]`. `0x400` 은 이름 못 정함 `[공백]`.
  5. 아직 확인 아님 → 동적 **D6** (Q92).
- 비유: 카드를 다 쓰고 손잡이를 놓으면, 서고지기가 반납 도장을 찍는다. 그 도장이 `0x4400` 일 것 같다.

### 2.14 갈림길: test.exe 는 어느 쪽 (대표)

run 문서 "갈림길" 표에서 골랐다.

| 갈림길 | test.exe | 근거 |
|---|---|---|
| 같은 파일 서랍이 이미 있나 (새 길 / 공유 길) | **못 정함** | 사진 값이 두 길에서 같음 → D1 (2.8) |
| 서랍 만드는 중이라 기다리나 | 안 기다림 | 사진 `WaitList` null |
| 원격 파일인가 | 아님 | 사진 `u1` = 서랍 주소 (아래 비트 0) |
| `SEC_BASED` 인가 | 아님 | 사진 `StartingVpn`·`SectionNode` 0 |
| 세션 전역 이미지인가 | 아님 | 사진 `SessionId` 0 |
| COM (.NET) 디렉터리 | 아님 | 헤더 COM 디렉터리 0 (결과서가 파일 직접 읽음) |
| 플랫 매핑 / 4GB 아래 요청 | 아님 | 사진 `ImageFlags` `0x4` |
| exe 열기가 상대 열기인가 | 아님 (절대 경로) | `[정적 조건]` → D8 |
| 첫 열기 실패 → 재시도 | 첫 번째 성공 쪽 `[추측]` | — |

- 표를 읽는 법: 오른쪽 "근거" 가 **사진** 인 줄이 많다. 정적은 "조건이 이러면 이 줄을 탄다" 까지 말하고, 사진 값이 "이번엔 그 조건이 아니었다" 를 말해 준다. 둘을 합쳐 갈래를 정한 것이다.

### 2.15 읽는 입력, 그리고 시스템 콜 0

- 이 조각 안에는 `syscall` 이 **없다.** 전부 부모 cmd.exe 의 `NtCreateUserProcess` 시스템 콜 **안**이다 `[정적]`.
- 밖에서 들어오는 값 (run 문서 "읽는 입력"):
  - **exe 이름**: `NtCreateUserProcess` `[RBP+0x290]` → 객체 속성 `ObjectName`. 이 칸을 채우는 줄은 못 찾았다 `[공백]`. 부모가 넘긴 NT 경로로 보인다 `[추측]` (Q92 에 같이).
  - **서고지기에게 묻는 것**: 파일 크기 (`FsRtlGetFileSize`), 헤더 첫 쪽, 열기 결과 (`FsContext*`·`SectionObjectPointer`·`Vpb`·공유 접근).
  - **PE 헤더 칸** → `MiVerifyImageHeader` 칸 모음 → 요약 카드 (run31 의 검사관 메모지).
  - **호출자 값**: 페이지 보호 `0x10`, 서명 수준 바이트, 세션 id, 플래그.
  - **시스템 상태**: 시스템 파티션 (파티션 0), 무작위 값 (`MiSelectImageBase` ASLR), `MiHalfSecond`.

### 2.16 정리: 비유로 한 번 더

1. 관리사무소 창구가 **설계도를 꺼내 온다** (`IopCreateFile`).
   - 안내 데스크가 주소를 따라가 설계도가 있는 **서고 건물**에 닿고 (`ObpLookupObjectName`), 건물 담당자 (`IopParseDevice`) 에게 넘긴다.
   - 담당자는 빈 **대출 카드**를 받아 (`IopAllocRealFileObject`) 이름·종류·건물 칸을 적고, **서고지기**에게 "꺼내 주세요" 요청서를 건넨다 (`IofCallDriver`).
   - 서고지기는 카드의 "서가 위치" 칸 넷과 공용 도장 칸을 채운다 (nt 에 줄 없음, `[추측]` ntfs.sys).
2. 창구가 **조립식 세트 주문**을 넣는다 (`MmCreateSpecialImageSection → MiCreateSection`).
   - 책상 위에 빈 **쪽지**를 하나 깔아 둔다 (스택 패킷, `memset`).
   - 서랍을 본다. 비었으면 "작성 중" 팻말을 꽂고, 거의 한 사람 (`MiBuildImageControlArea`) 이 **서랍·배치표·요약 카드**를 그린다. 다 되면 팻말을 뽑고 진짜 서랍을 꽂는다 (`MiSectionCreated`).
   - 서랍이 이미 있으면 명단에 이름 두 줄만 보탠다 (공유 길). test.exe 가 어느 쪽이었는지는 아직 모른다.
   - 그동안 꾸러미에 적을 값은 쪽지에 모인다.
3. 사슬이 다 끝나면 옆 직원 (`MiFinishCreateSection`) 이 공용 창구에서 빈 **주문 꾸러미**를 받아 와 (`ObCreateObjectEx`), 쪽지를 대고 통째로 베껴 쓰고, "서랍 번호" 가 적힌 꾸러미를 건넨다.
4. 나중에 손잡이를 놓으면 (`PspDeleteCreateProcessContext`) 서고지기가 반납 도장을 찍는 것 같다 (`Flags` `0x4400`, `[추측]`).

### 2.17 곁가지: 도구 메모

- 구조체 칸 표는 이번에 하네스에 들어온 `pdb_struct.py` 로 뽑았다 (첫 실사용). 다섯 구조체 칸 수가 짐 지도 7.2 와 정확히 같았고 한 번에 0.1초 (결과서).
- 한계 둘 (하네스라 오케스트레이터가 사용자에게 보임):
  - **이름 없는 공용체** (`<unnamed-tag>`) 안쪽을 안 펼친다 (`_SECTION.u1`·`.u`, `_CONTROL_AREA.u`·`u1`·`u2`·`u3` 등). 이번엔 sast/38 의 펼친 표로 대신 봤다. 제안: `--expand`.
  - **겹치는 칸 표시가 없다** (2.7 의 `SubSystemVersion` 같은 것). 제안: `overlap` 열.
- 복사 줄 오프셋 한 가지 (run 문서 "도구 메모"): 결과서 3.20 표의 "복사 `+0x172`" 는 마지막 `MOVUPS` **읽기** 자리다. 꾸러미에 쓰는 자리는 `+0x15f`·`+0x166`·`+0x16e`·`+0x179`. 그래서 짐 지도에는 범위 `+0x15b ~ +0x179` 로 적었다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp.pdb 쓴 run 목록에 run47 없음 | run47 추가. 근거 로그에 "(run47) 정적 바퀴, K1 덤프 읽기 로그 다시 읽음" 한 줄 |
| 그림 1 제목 | run 목록 … run34 | … run34, **run47** |
| 그림 1 `s2` (`IopCreateFile`) | "정적: exe 파일 열기" | "파일 객체는 안쪽 `IopParseDevice +0xe1e IopAllocRealFileObject`, 2.1" 추가 |
| 그림 1 `s3` 아래 | `MiCreateSection` → `+0xf8` 한 화살표 | `(1) +0xf8` 과 **`(2) +0x125` → 새 `(정적)` 상자 `i6` `MiFinishCreateSection`** |
| 2.1 트리 | `+0x6cb IopCreateFile [정적]` 한 줄, `MiCreateSection` 아래 한 갈래 | `IopCreateFile` 아래 `ObOpenObjectByNameEx → ObpLookupObjectName → IopParseDevice → (1) IopAllocRealFileObject / (2) IofCallDriver`. `MiCreateSection` 아래 `(1) +0xf8` / **`(2) +0x125 MiFinishCreateSection`** |
| 2.1 `nt!IopCreateFile` 절 | "실패하면 `IoCreateFileEx` 로 한 번 더 시도하는 코드가 있다" | 재시도가 맞다 (`+0x6cf JNS`), 열기 인자, **파일 객체가 생기는 사슬 4단계**, 파일 시스템 칸 넷은 nt 에 줄 없음, exe 핸들 닫기와 `Flags` `0x4400` `[추측]` |
| 2.1 `MiCreateSection` 절 | `+0xf8` 에서 `MiCreateImageOrDataSection` 을 부른다까지 | **`+0x125 MiFinishCreateSection` 이 `_SECTION` 을 만든다**, 패킷 통째 복사 (보탬이지 정정 아님) |
| 2.1 갈림길 2번 "사용 수 +1" | 함수 이름 없이 | `MiReferenceControlArea +0x107` / `+0x11d` 라고 명시 |
| 2.1 Q61 줄 | "같은 부팅에 이미 돌렸으면 공유" `[추측]` | run44 사진 값으로는 못 가름 `[정적]`, 두 번 실행 비교로 사진으로도 가능 |
| 2.1 `MiBuildImageControlArea` 절 | 컨트롤 영역·프로토 PTE·세그먼트를 만든다 | 칸은 거의 이 함수 하나, 풀 0, 뒤에 `MiSectionCreated` 가 마무리 |
| 4장 | Q61 · Q62 줄 | Q61·Q62 (c) 에 run47 보탬. 새 줄 넷: **(해결됨) Q78b**, 새 **Q92** (D4 ~ D7 + 낮은 우선순위), exe 이름 문자열 `[공백]`, 섹션 묶음 옆 짐 → Q78c |
| 4.1 | 상자 다섯 확인 방법 (Q61) | + `MiFinishCreateSection` 상자 확인 (Q92 D2, `nt+0x96dd64`) |
| 5장 | — | run47 줄 둘 |

태그 변화 대표:

- `IoCreateFileEx` 의 정체: "실패하면 한 번 더 시도하는 코드" `[정적]` → **실패 때만 타는 재시도**, exe 를 여는 쪽은 `+0x6cb` `[정적]` (run9 말 확인).
- 짐 지도 3.21 `u`: `[추측]` 짐작 → **`[정적]` 정확**.
- 짐 지도 3.20 만드는 길: 사슬 안쪽 (구조체 단위, 칸 근거 없음) → **사슬 끝 형제 `MiFinishCreateSection`** `[정적]`.

### 짐 지도 `baggage.md` 에서 바뀐 것 (다섯째판)

- 2장 숫자 (짐 지도 그대로):
  - 구조체 21개: 아는 칸 186 → **249** (정확 195 · 짐작 54) + **아무도 안 씀 11** + 모름 1,270 → **1,196**.
  - 커널 구조체 14개 합: nt 가 씀 62 → **121**, 다른 DLL 0 → **5**, 모름 906 → **832**.
  - 다섯 구조체 (run 문서 표):

| 구조체 | nt 가 씀 | 다른 DLL (드라이버) | 아무도 안 씀 | 아직 모름 |
|---|---|---|---|---|
| `_SECTION` | 0 → **9** | 0 | 0 | 9 → **0** |
| `_CONTROL_AREA` | 2 → **16** | 0 | 1 | 15 → **0** |
| `_SEGMENT` | 1 → **11** | 0 | 0 | 10 → **0** |
| `_SECTION_IMAGE_INFORMATION` | 16 → **24** | 0 | 2 | 10 → **0** |
| `_FILE_OBJECT` | 0 → **18** | 0 → **5** | 8 | 30 → **0** |

  - 한 줄 읽기: `_FILE_OBJECT` 18 + 5 − 1 (`Flags` 양쪽) + 8 = 30 (짐 지도 2장 아래 줄).
  - `K1 이전` 인 모르는 칸 215 → **174**. 남은 커널 `K1 이전` 106 은 프로세스 객체·토큰·핸들 표 (Q78c) 몫이다.
- 3장 3.20 ~ 3.24: 20줄 → **85줄**. 줄 끝 "**Q78b 로 닫힘**" = 이 조각으로 닫힌 칸.
- 경로 줄임말 넷이 새로 생겼다: `①S…` (섹션 사슬 머리), `①SC…` (새로 만드는 길), `①SN…` (이미지 파일 맵), `①F…` (exe 열기 사슬).
- "쓰는 말" 에 둘이 생겼다: **`다른 드라이버 (ntfs.sys)`** (= 커널 쪽 `다른 DLL`), **`아무도 안 씀`** (2.4).
- 정정 2줄 (3.21 `NumberOfUserReferences` 함수 이름, `u` 짐작 → 정확), 3.23 "오프셋 미상" 14줄 채움.

### 큰 그림에서 어떻게 이어지나

- **exe 섹션 묶음은 이제 칸 단위로 다 보인다.** run31 이 "세트를 어떻게 짜나" (사슬) 를, run44 가 "언제 값이 있나" (사진) 를 줬다면, 이번에 "**누가, 몇 번째 줄에서**" 가 채워졌다.
- **에뮬레이터 바깥 칸이 처음으로 이름을 얻었다.** 짐 지도에 "다른 DLL" 열은 원래 있었지만 커널 쪽은 0 이었다. 이번에 드라이버 (ntfs.sys) 몫 5칸이 들어왔고, 그중 `SectionObjectPointer` 는 "**바깥 몫이지만 대신 채워야 하는 칸**" 이라는 꼬리표가 붙었다 (2.12).
- 다음 조각 **Q78c (프로세스 객체)** 로 바로 이어진다 (결과서 "나머지 Q78 로 넘길 것"):
  - 이번 `NtCreateUserProcess +0x791 ObReferenceObjectByHandle` 가 쥔 대출 카드 (`[RBP+0x258]`) → `EPROCESS.ImageFilePointer` (+0x330).
  - 이번 주문 꾸러미 → `EPROCESS.SectionObject` (+0x2a8).
  - 매핑 (`MiMapProcessExecutable → MmMapViewOfSectionEx → MiMapViewOfImageSection`) 이 `NumberOfMappedViews`·`NumberOfUserReferences` 를 올린다 `[추측]` (Q78c / Q79). 2.8 의 "사용자 참조 2" 의 나머지 한 줄이 여기다.
- 다른 DLL (dll1·dll2·kernel32·kernelbase) 의 섹션·파일 객체 (run45 의 C6 "옆방 가구") 는 **Q79** 에서 본다. 이번엔 exe 것만이다.

---

## 4. 아직 모르는 것

멈춤점은 모두 커널이라 **모든 프로세스에 걸린다.** test.exe 일 때만 서게 하는 조건은 dast-mentor 몫이다 (run 문서).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **D1** test.exe 가 새 길인가 공유 길인가 | 두 길이 같은 사진 숫자를 만든다 (2.8) | 동적 **Q61**: `nt+0x96f4d0` / `nt+0x96e6cc` 중 서는 쪽. 또는 재부팅 없이 두 번 돌려 두 K1 의 `_SECTION.u1` 비교 (사진으로 가능) |
| **D4** `_CONTROL_AREA.u2` 가 어느 갈래로 `ffffffff`/`0x00c00009` 가 되나 | 쓰는 줄 후보는 찾았지만 어느 갈래를 탔는지는 실행 때 값 | 동적 **Q92**: `nt+0x972de0` (`MiSelectImageBase` 입구), `nt+0x972fed` |
| **D5** `NumberOfPfnReferences` 3 → `0x2d`, `FilePointer` 아래 비트를 누가 바꾸나 | 섹션 사슬 밖 길 (페이지 폴트·참조) | 동적 **Q92**: K1 에서 `ba w8 <CA+0x20>`, `ba w8 <CA+0x40>`, 서면 `k` |
| **D6** `_FILE_OBJECT.Flags` `0x4400` 을 누가 켜나 (exe 핸들 닫을 때가 맞나) | `IopCloseFile` 에 상수가 없음. 서고지기 쪽 `[추측]` | 동적 **Q92**: `nt+0x842838`, `nt+0x86c660`, `ba w4 <FO+0x50>` |
| **D7** `Vpb`·`FsContext`·`FsContext2`·`SectionObjectPointer`·`Flags 0x40/0x80000` 쓴 모듈이 ntfs 인가 | nt 에 줄 없는 건 `[정적]` 이지만, 주체는 ntfs.sys 를 안 뽑아서 `[추측]` | 동적 **Q92**: `nt+0x8bbb8f` 뒤 `ba w8` + `lm a @rip` |
| (낮은 우선순위) D2 쪽지 사본, D3 원격·`SEC_BASED`·세션 갈래, D8 `IopParseFile` 안 탐, D9 `IoSetShareAccess` 부르는 쪽, D10 서명 수준 값 | 정적으로 답은 했고, 확인만 남음. K1 은 최종 값이라 복사 전은 못 봄 | 동적 **Q92** |
| exe 이름 문자열 `NtCreateUserProcess [RBP+0x290]` 을 채우는 줄 | `NtCreateUserProcess` 안에서 못 찾음 `[공백]` (앞 함수가 채움) | **Q92** 에 같이 |
| `EPROCESS.SectionObject`·`ImageFilePointer`·`SectionBaseAddress` 쓰기, 매핑이 올리는 참조 수 | 프로세스 객체·매핑은 이번 범위 밖 | 정적 **Q78c** (/ Q79) |
| 칸 표가 아직 없는 이웃 짐 `_SECTION_OBJECT_POINTERS`·`_MI_IMAGE_ADDITIONAL_INFO`·`_MI_EXTRA_IMAGE_INFORMATION`, 서브섹션·프로토 PTE 표 안 | 체크리스트 21종 밖. PDB 정의는 있음 | 짐 지도 7.6 에 기록만 (Q78c 출처) |
| 줄기 밖 함수 본문 (`MiInitializeNewImageSectionProtos`·`MiValidateExistingImage`·`MiCallCreateSectionFilters` 등) | 이름만 두기로 함 (뽑아 둠, 본문 안 읽음) | 정적 Q62 (c)·(d) 쪽 |

- D5·D6·D7 은 데이터 멈춤점 (`ba w`) 이라 **한 실행에서 같은 주소로** 같이 본다. 주소는 실행마다 바뀐다 (run 문서).
- 대기열 순서 (plan): **Q78c** → Q78d → Q78e → Q78f → Q78g → Q79 → Q80 → Q82. Q61·Q92 는 동적 차례에.

---

## 5. 더 읽기

- run 문서: [`run47-section-chain.md`](run47-section-chain.md) — 구조체별 요약 표와 "다시 가른 것", 호출 지점 표, 호출 관계 트리, 두 길, `_FILE_OBJECT` 주인 셋, 갈림길 표, 읽는 입력, 확인된 것, 정정, 아직 확인 필요 (D1 ~ D10), 도구 메모
- 결과서: [`_workspace/sast/41_section-chain_result.md`](../../_workspace/sast/41_section-chain_result.md) — 질문 1 (`_SECTION` 칸마다), 질문 2 (컨트롤 영역 칸 표, 공유 길, `MiSectionCreated`, 값 출처 표), 질문 3 (겹침, `ImageFlags`, 오프셋 미상 채우기), 질문 4 (사슬, 주인 셋 표, `Flags` 비트 표, "다른 DLL" 열 의견), 칸별 쓰는 곳 (3.20 ~ 3.24 양식), 나머지 Q78 로 넘길 것
- 요청서: [`_workspace/sast/41_section-chain.md`](../../_workspace/sast/41_section-chain.md)
- 짐 지도: [`baggage.md`](baggage.md) — "쓰는 말" (`다른 드라이버`, `아무도 안 씀`), 2장 (다섯째판 숫자), 3.20 ~ 3.24, 경로 줄임말 `①S…`·`①SC…`·`①SN…`·`①F…`, 7.2, 7.10
- 흐름도: [`flow.md`](flow.md) — 그림 1 (`s2` 글, `i6` 상자), 2.1 트리, `nt!IopCreateFile` 절, `nt!MiCreateSection` → `nt!MiCreateImageOrDataSection` 절, `nt!MiBuildImageControlArea` 절, 4장 run47 줄, 4.1, 5장
- 진행표: `_workspace/plan.md` Q78b (끝) · Q78c · Q61 · Q92, 결정 기록 2026-10-28
- 앞 해설:
  - [run31](../02_sprint/run31-image-section-해설.md) — 섹션 사슬, 서랍, 팻말, 꾸러미, 요약 카드 (이번 글의 바탕)
  - [run9](../02_sprint/run9-create-user-process-해설.md) — 설계도 꺼내 오기 (`IopCreateFile`), 조립식 세트
  - [run43](run43-baggage-checklist-해설.md) 2.2 — 짐 지도 "쓰는 말"
  - [run44](run44-baggage-snap-해설.md) 2.10 — 사진 구간, "끝까지 0 ≠ 아무도 안 씀"
  - [run45](run45-work-list-해설.md) 2.6 — C6 다른 모듈의 섹션·파일 객체
  - [run46](run46-process-heap-해설.md) — 바로 앞 Q78 조각 (창고 = 프로세스 힙)
