# run31 image-section — 해설

> 이 글은 [run31 문서](run31-image-section.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q33** — 2.1 `nt!MmCreateSpecialImageSection` callee (exe 이미지 섹션, `MiCreateImageFileMap` 과의 관계) (`_workspace/plan.md`, 이번에 **끝**)
  - **단계 3 "작은 정적부터 몰아서"** 의 첫 바퀴다 (사용자 결정, 2026-10-23).
  - 줄의 첫 항목 Q32 (`PspMapThreadCreationFlags` bit0) 는 run12 가 이미 답해서 **바퀴 없이 닫았다.** 그래서 실제로 돈 첫 바퀴가 이번 Q33 이다.
  - 새 항목: **Q61** (동적, 이미지 섹션 만들기 실제 길과 값), **Q62** (정적, 작은 것 묶음). 출처를 더한 항목: **Q34**, **Q35**.
  - 다음 항목: **Q36** (plan.md).
- 근거:
  - run 문서 [`run31-image-section.md`](run31-image-section.md) (가장 중요)
  - 요청서 [`_workspace/sast/26_image-section.md`](../../_workspace/sast/26_image-section.md)
  - 결과서 [`_workspace/sast/26_image-section_result.md`](../../_workspace/sast/26_image-section_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 연 바이너리는 ntoskrnl (`ntkrnlmp.pdb`, flow.md 기준과 같은 GUID).
  - test.exe 의 헤더 값(진입점, 기본 주소, 정렬 …)은 결과서가 `ms/bin/test.exe` (run30 과 같은 파일) 를 직접 읽은 것이다.

이 글은 **앞으로 되감기**다.

- run30 까지 우리는 시간 순서대로 **현관(exe 진입점)** 에 도착했다.
- 단계 3 은 "빈 곳 채우기" 라서, 이번엔 맨 앞 — 부모 cmd.exe 가 `NtCreateUserProcess` 를 부른 바로 그 안 — 으로 돌아간다.
- 그런데 돌아가 보니, run30 의 **현관 번지가 처음 적히는 곳**이 바로 여기였다. 그래서 이번 바퀴로 진입점 값이 **처음부터 끝까지 한 줄로** 이어졌다 (2.11).

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 핸들(**손잡이**) → [run9 해설](run9-create-user-process-해설.md) 2절
- 이미지 섹션 (**조립식 건물 세트**) → [run9 해설 "이미지 섹션"](run9-create-user-process-해설.md#이미지-섹션)
- 매핑 = 세트를 땅에 **갖다 놓기** ("올리기") → [run10 해설 "매핑 올리기"](run10-allocate-process-해설.md#매핑-올리기)
- `SEC_IMAGE` (**"조립식 세트로 짜 주세요" 도장**) → [run26 해설 걸음 3](run26-map-dll-file-해설.md#걸음-3--세트-짜기-0x194-ntcreatesection-sec_image)
- 재배치 (**다른 번지에 놓였으니 안의 번지수를 고쳐 쓰기**) → [run26 해설 4절](run26-map-dll-file-해설.md#4-아직-모르는-것)
- PE 진입점 `AddressOfEntryPoint` (**설계도 첫 장의 "전원 스위치는 몇 호"**) → [run25 해설 2.3](run25-dll-entry-해설.md#새-말--pe-진입점-addressofentrypoint)
- `TransferAddress`, `_SECTION_IMAGE_INFORMATION` (이미지 정보 묶음), 업무 시작 메모의 `Rcx` → [run16 해설 2.4](run16-thunk-context-해설.md#24-장면--처음-메모--pspcreateusercontext-가-채우는-값)
- `NtContinue` 뒤 `Rcx` 가 살아서 본업 자리까지 가는 이유 → [run29 해설 2.11](run29-continue-start-해설.md#211-유저로-나가기--반환-없이-iretq-그래서-rcx-가-산다)
- 현관 = exe 진입점, 지도의 경계 → [run30 해설 2.1](run30-exe-crt-start-해설.md#21-먼저--지도의-경계가-정해졌다)

비유도 그대로 이어 쓴다.

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**
- 프로세스 = **가게**, 주소 공간 = **빈 땅**, 스레드 = **직원**, PEB = **안내판**
- exe 파일 = **설계도**, 이미지 섹션 = **조립식 건물 세트**, 매핑 = 세트를 땅에 **갖다 놓기**
- exe 진입점 = **현관** (run30), 첫 스레드 문맥 = **업무 시작 메모** (run16)
- **이번에 더하는 비유 하나** (flow.md 2.1 과 같다):
  - 조립식 세트의 **속을 열어 보니, 대부분은 "방 배치표"** 였다. 관리사무소가 설계도를 읽고 "어느 방에 설계도의 어느 쪽이 들어간다" 를 적은 표다.
  - 배치표 맨 위 칸에는 **"첫 출근하면 이 방부터"** (진입점) 를 적는다. → 이게 run30 의 **현관 번지**의 처음 출처다.
  - 같은 설계도로 그린 배치표가 **서랍에 이미 있으면** 새로 안 그리고 그걸 같이 쓴다.

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 넷**:

1. **points.md 단계 3 "exe 헤더 읽기" = `MiCreateImageFileMap` 이 맞다.** `MmCreateSpecialImageSection` 안쪽 다섯 고리로 이어진다 `[정적]` → 2.4
2. **단, 늘 타는 길은 아니다.** 같은 파일의 배치표가 서랍에 이미 있으면 공유 길로 가서 `MiCreateImageFileMap` 을 **건너뛴다** `[정적]`. 같은 부팅에서 test.exe 를 이미 돌렸다면 디버거로 볼 때 안 설 수 있다 → 2.5
3. **이 단계에선 프로세스 메모리에 아직 안 올린다.** 배치표만 그린다 (헤더 한 페이지만 예외) `[정적]` → 2.8
4. **진입점 값이 처음 적히는 줄 = `MiBuildImageControlArea +0x172`** (`ImageBase + AddressOfEntryPoint`). 기본 주소를 새로 고르면 `MiRelocateImage` 조각이 차이(델타)를 더한다 `[정적]`. 이걸로 PE 헤더 → 현관까지 길이 다 이어졌다 → 2.9 ~ 2.11

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 모두 **돌아올 자리(ret)** 다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess                    (run14·run9)

 커널, 부모 스레드 안: NtCreateUserProcess
  ├─ +0x6cb  IopCreateFile                 설계도(exe 파일) 꺼내기                    (run9)
  ├─ +0x93c  MmCreateSpecialImageSection   조립식 세트(이미지 섹션) 준비              (run9)
  │     └─ … MiCreateImageFileMap          세트 속: 설계도 읽고 방 배치표 그리기      ← 이번
  │           └─ … MiBuildImageControlArea  배치표 맨 위에 "첫 출근 방" 적기         ← 이번
  ├─ +0xc36  PspAllocateProcess            가게 짓기, 세트를 땅에 갖다 놓기           (run10)
  │     └─ +0x991 MmGetSectionInformation   배치표 맨 위 칸 꺼내기                    (run16)
  └─ +0xd22  PspCreateUserContext          업무 시작 메모 Rcx = 진입점                (run16)

 test.exe 메인 스레드: … 로더 … NtContinue → RtlUserThreadStart → BaseThreadInitThunk +0x17   (run15 ~ run29)
 ───────────────────────── 현관 (exe 진입점 mainCRTStartup, rva 0x1d40) ─────────────────   (run30)
```

- 시간으로는 **맨 앞쪽**이다. 프로세스(가게)를 짓기도 전, 설계도를 펴 보는 단계다.
- flow.md 로는 **2.1** 의 `nt!MmCreateSpecialImageSection` 절 아래, 그림 1 의 `s3` 상자 아래다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. `MmCreateSpecialImageSection` 은 안에서 무엇을 부르나? points.md 단계 3 의 `MiCreateImageFileMap` 이 이 함수 **안쪽**인가? (flow.md 2.1 `[추측]`)
2. run30 과 잇기: PE 헤더의 `AddressOfEntryPoint` 가 이미지 정보 `TransferAddress` 칸으로 **처음** 옮겨지는 줄은 어디인가?
   - run16 은 그 칸을 **꺼내는 쪽**(`PspAllocateProcess +0x991` `MmGetSectionInformation`)만 봤다. **넣는 쪽**은 빈칸이었다.

### 바퀴 전에 알던 것

- `[정적]` (run9) `NtCreateUserProcess +0x937` (ret `+0x93c`) 가 `MmCreateSpecialImageSection` 을 부른다. 서명 재시도 고리(`+0xa08 ~ +0xa2a`) 안에 있다.
- `[정적]` (run9) `MiCreateImageFileMap` 은 `NtCreateUserProcess` 가 **직접** 부르지 않는다. 이 함수 안쪽인지는 모른다 `[추측]`.
- `[정적]` (run10) 만든 세트는 `PspAllocateProcess` 로 넘어가고, 나중에 `MiMapProcessExecutable +0xb8` `MmMapViewOfSectionEx` 에서 땅에 놓인다.
- `[정적]` (run16) `PspAllocateProcess +0x991` `MmGetSectionInformation` 이 이미지 정보를 복사하고, 그 첫 칸 `TransferAddress` 가 `PspCreateUserContext` 에서 `CONTEXT.Rcx` 가 된다. 다른 주소에 올라가면 `+0x20b3 ~ +0x20ce` 에서 보정한다.
- `[정적]` (run30) test.exe 의 `AddressOfEntryPoint` = rva `0x1d40` = `mainCRTStartup`. 커널은 이 값을 `Rcx` 로 넘길 뿐이다.
- points.md 단계 3: "exe 헤더 읽기 — `nt!MiCreateImageFileMap` — `[확인 필요]`".

---

## 2. 찾은 것

### 2.1 한눈에 — 세트 속을 열면

run 문서 "호출 관계" 를 줄였다. 오프셋은 ret, `(n)` 은 성공 길로 보이는 순서다. 모두 `[정적]`.

```
NtCreateUserProcess
 └─ +0x93c  MmCreateSpecialImageSection          세트 준비. 끝에 손잡이(핸들)를 돌려줌
      ├─ (1) +0xd4   MiCreateSection
      │     └─ +0xf8  MiCreateImageOrDataSection
      │          ├─ (1) +0x1a3  MiReferenceControlArea      서랍에 배치표가 이미 있나?
      │          ├─ (2) +0x1d5  MiShareExistingControlArea  있으면 → 같이 쓰기 (아래 안 탐)   ┐ 둘 중
      │          └─ (2) +0x385  MiCreateNewSection          없으면 → 새로 그리기              ┘ 하나
      │                ├─ (1) +0x215  MiCreateImageFileMap      ← points.md 단계 3
      │                │     ├─ (1) +0x15c  MiCopyHeaderIfResident  / +0x501 MiReadImageHeaders   헤더 읽기
      │                │     ├─ (2) +0x1f6  MiVerifyImageHeader      헤더 검사, +0x8a 진입점 rva 복사
      │                │     └─ (3) +0x2e6  MiBuildImageControlArea  배치표 그리기, +0x172 진입점 적기
      │                ├─ (2) +0x167  MiSectionCreated
      │                └─ (3) +0x3da  MiRelocateImage          (조건부) 진입점 칸에 델타 더하기
      └─ (2) +0x146  ObInsertObjectEx                → +0x14f 손잡이를 호출자 칸에
```

- 한 줄 요약: **설계도를 읽고 → 검사하고 → 배치표를 그리고 → 맨 위에 첫 출근 방을 적고 → 손잡이를 건넨다.**
- `MiCreateNewSection` 은 `+0x215` 에서 `JMP` 로 앞쪽 `+0x5da` 로 돌아가서, `(n)` 순서가 오프셋 순서와 다르다 (run 문서). 오프셋 순서 ≠ 실행 순서는 run9 에서 본 그 얘기다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 갈림길에서 "새로 그리기" 쪽으로 가는 줄

트리의 `(2) +0x385  MiCreateNewSection` 줄.

- **누가**: `MiCreateImageOrDataSection`
- **어디서**: `+0x380` 에서 부르고, 돌아올 자리는 `+0x385`
- **누구를**: `MiCreateNewSection`
- **무엇을 위해**: 이 파일의 배치표가 **서랍에 아직 없을 때**, 새로 그리려고. `[정적]`
  - 바로 위 형제 `+0x1d5 MiShareExistingControlArea` 와 **둘 중 하나만** 탄다. 가르는 줄은 2.5 에서.

#### 한 줄 읽기 ② — 진입점 주소가 처음 만들어지는 줄

트리의 `MiBuildImageControlArea  … +0x172 진입점 적기`. 명령 세 줄이다 (run 문서 인용):

```
+0x16b  MOV EAX,[R15+0x1c]
+0x16f  ADD RAX,[R15]
+0x172  MOV [R8],RAX
```

- **누가**: `MiBuildImageControlArea`
- **어디서**: `+0x16b ~ +0x172` (CALL 이 아니라 **값을 쓰는 줄**이다)
- **무엇을**:
  - `R15` = 헤더 검사 때 채워 둔 **칸 모음**. `+0x1c` 칸 = 진입점 rva, `+0x0` 칸 = `ImageBase` (결과서).
  - 그래서 위 두 줄 = "진입점 rva + 기본 주소" 를 계산한다.
  - `R8` = 이미지 정보. 그 `+0x0` 칸이 `TransferAddress` 다 (PDB 타입).
- **무엇을 위해**: 배치표 맨 위 "첫 출근 방" 칸을 채우려고. **이 칸이 나중에 업무 시작 메모의 `Rcx` 가 된다** (run16). `[정적]`

### 2.3 먼저 — "조립식 세트" 와 "방 배치표" 는 같은 이야기다

비유가 하나 늘어서 헷갈릴 수 있으니 먼저 맞춰 두자.

- run9 에서 이미지 섹션을 **조립식 건물 세트**라고 불렀다. "설계도를 보고 미리 짜 둔 세트. 아직 땅에 갖다 놓지는 않았다."
- 이번에 그 세트를 **어떻게 짜는지** 처음 봤다. 그랬더니 세트 안에 자재(파일 내용)는 거의 없고, **"어느 방에 설계도의 어느 쪽이 들어간다"** 는 표가 대부분이었다 (2.8).
- 그래서 flow.md 는 이번 절에서 **방 배치표**라는 말을 쓴다.
  - 조립식 세트 = 배치표 + 설계도 첫 장(헤더 한 페이지) 정도로 생각하면 된다.
  - "세트를 땅에 갖다 놓기"(run10) 는 여전히 맞는 말이다. 다만 놓을 때도 배치표를 붙이는 것이지, 자재를 다 옮기는 게 아니다 — 이건 run10 해설의 "매핑 = 복사가 아니라 연결" 과 같은 얘기다.

### 2.4 핵심 ① 사슬 — `MiCreateImageFileMap` 은 안쪽이 맞다

#### 다섯 고리와 각 고리를 타는 조건 `[정적]`

| 고리 | 부르는 쪽 → 불리는 쪽 (ret) | 이 고리를 타는 조건 |
|---|---|---|
| 1 | `NtCreateUserProcess` → `MmCreateSpecialImageSection` (`+0x93c`) | 서명 재시도 고리 안 (run9, 2.6) |
| 2 | `MmCreateSpecialImageSection` → `MiCreateSection` (`+0xd4`) | 늘 |
| 3 | `MiCreateSection` → `MiCreateImageOrDataSection` (`+0xf8`) | 파일 핸들이나 파일 객체가 넘어올 때. 여기선 핸들을 넘기니 늘 |
| 4 | `MiCreateImageOrDataSection` → `MiCreateNewSection` (`+0x385`) | **서랍에 배치표가 없을 때만** (2.5) |
| 5 | `MiCreateNewSection` → `MiCreateImageFileMap` (`+0x215`) | `SEC_IMAGE` 가 켜졌을 때. 아니면 `MiCreateDataFileMap` |

- 다섯 줄 모두 `.asm` 의 CALL 줄과 ret 이 맞았다 (flow-writer 가 다시 맞춰 봄, 어긋난 곳 없음).

#### test.exe 는 `SEC_IMAGE` 쪽인가 — 단계별로

1. `[정적]` `NtCreateUserProcess +0x7bc` 가 플래그 **bit0 를 늘 켠다.**
2. `[정적]` `MmCreateSpecialImageSection +0x3f ~ +0x56` 이 그 bit0 를 보고 할당 속성 **`0x01000000`** 을 고른다 (결과서가 원본 바이트로 확인).
3. 그래서 → 5번 고리는 **늘 `MiCreateImageFileMap` 쪽**이다 `[정적]`.
   - 비유: 관리사무소는 이 창구에서 설계도를 받으면 **늘 "조립식 세트로 짜 주세요" 도장**을 찍는다.

#### "다른 길로 오는 건 없나?"

- `[정적]` 이 빌드에서 `MiCreateNewSection` 을 부르는 곳, `MiCreateImageFileMap` 을 부르는 곳은 **각각 하나뿐**이다 (`_refs`, CALL 참조 기준).
- 그래서 → **새 이미지 배치표는 모두 이 사슬로 그린다.**
- 단 `refs` 는 함수 포인터로 부르는 길은 못 본다 (run 문서). 그 길까지 없다고 말하는 건 아니다.

#### 판정

- flow.md 2.1 의 `[추측]` "`MiCreateImageFileMap` 은 이 함수 안쪽인지 모른다" → **안쪽이 맞다** `[정적]`.
- points.md 단계 3 = `MiCreateImageFileMap` 은 **대표 지점으로 둬도 된다** (결과서 질문 6). 이유: 이 함수 하나가 "읽기·검사·배치표 그리기" 한 묶음을 대표한다.
  - 더 작은 `MiVerifyImageHeader` 는 검사만 하고 진입점을 **복사만** 해서 대표로는 부족하다 (결과서).
  - 대신 조건이 하나 붙는다 — 바로 다음 절이다.

### 2.5 핵심 ② 갈림길 — 서랍에 배치표가 이미 있으면 건너뛴다

#### 새 말 — 컨트롤 영역

- **컨트롤 영역(control area)** = 파일 **하나의** 이미지 배치표를 묶어 관리하는 커널 쪽 머리 서류. **파일 객체**(커널이 열린 파일을 나타내는 물건)의 `SectionObjectPointer` 안 **`ImageSectionObject`** 칸에 걸린다 `[정적]`.
- 비유: 설계도 서류함에 붙은 **서랍**. 서랍 안에 그 설계도로 그린 **배치표 원본**이 들어 있다.
- 같은 파일이면 이 서랍을 **같이 쓴다** (사용 수를 1 올림) `[정적]`. 그래서 "파일마다 하나" 라고 보면 된다.

#### 세 갈래 `[정적]` (`MiReferenceControlArea`, `MiCreateImageOrDataSection +0x1a3` 에서 부름)

전역 잠금을 잡고 `ImageSectionObject` 칸을 본다.

1. **비었음** → "작성 중" 자리표시 서류(`BeingCreated` 켜짐)를 꽂고 돌아온다 → `+0x385` `MiCreateNewSection` → `MiCreateImageFileMap` (**새로 그리기**).
2. **있고 멀쩡함** → 사용 수를 1 올리고(`+0x11d`) 돌아온다 → `+0x1d5` `MiShareExistingControlArea` (**같이 쓰기**). `MiCreateImageFileMap` 은 **안 탄다.**
3. **만드는 중 / 지우는 중** → 기다렸다가(`KeWaitForGate`) 처음부터 다시.

- 1번과 2번을 가르는 줄은 `MiCreateImageOrDataSection` 의 `+0x1ba MOV EAX,[RAX+0x38]` / `+0x1bd TEST AL,2` / `+0x1bf JNZ` 다. 돌려받은 서류의 `+0x38` bit1 (`BeingCreated`) 이 켜져 있으면 "내가 방금 꽂은 자리표시" → 새로 그리러 간다.
- 비유:
  - 서랍이 비었으면 **"작성 중" 팻말**을 먼저 꽂고 그리러 간다. 다른 사람이 동시에 와도 팻말을 보고 기다린다 (3번).
  - 서랍에 다 그린 배치표가 있으면 "나도 쓴다" 고 이름만 적고 그대로 가져다 쓴다.

#### test.exe 는 어느 쪽인가 — "실행 때 값"

- `SEC_IMAGE` 갈래: **예** `[정적]` (2.4).
- 서랍에 이미 있나: **정적으로 못 정한다.**
  - 이 부팅에서 test.exe 로 배치표를 그린 적이 없으면 → 1번 (새로 그리기).
  - 이미 돌렸고 그 서랍이 아직 남아 있으면 → 2번 (같이 쓰기).
  - 프로세스가 끝난 뒤 서랍이 **얼마나 남는지**는 이번에 안 봤다 `[추측]`.

#### 왜 이게 중요한가 — 동적 함정 후보

- 우리는 test.exe 를 **여러 번** 돌리면서 디버거를 붙인다.
- 그러면 두 번째 실행부터는 `MiCreateImageFileMap` 멈춤점이 **안 설 수 있다.** "코드가 틀렸나?" 하고 헷갈리기 딱 좋다.
- 그래서 run 문서는 이걸 **동적 함정 후보**로 적고, windbg-kd-runbook pitfalls 에 넣자고 보고했다.
- 피하는 법 (Q61): **재부팅 직후 첫 실행**, 또는 **파일을 복사해서** 다른 파일로 실행.
  - 비유: 같은 설계도면 서랍에서 꺼내 쓰니까, 그리는 장면을 보려면 서랍이 빈 날(재부팅 직후) 오거나 **설계도를 새 서류함에 복사**해서 가져와야 한다.
- points.md 판정에 붙는 조건도 이것이다: `MiCreateImageFileMap` 은 **"이 파일의 컨트롤 영역이 아직 없을 때만"** 선다.

### 2.6 `MmCreateSpecialImageSection` 이 "특별한" 이유

#### 새 말 — 섹션 객체

- **섹션 객체** = 이미지 섹션을 커널 물건으로 나타낸 것. 손잡이(핸들)를 쥐면 이걸 가리키게 된다. 안에서 컨트롤 영역을 찾아갈 수 있다 (`MmCreateSpecialImageSection +0xf6 ~ +0x11c`, 결과서) `[정적]`.
- 비유: 세트를 주문했을 때 받는 **주문 꾸러미**. 꾸러미에 손잡이를 달아 건네고, 꾸러미 안엔 "서랍 몇 번" 이 적혀 있다.

#### 사용자 모드 `NtCreateSection` 의 커널 안 버전 `[정적]`

같은 `MiCreateSection` 을 쓴다. 일반 길(`MiCreateSectionCommon`)과 다른 점:

1. **토큰·서명 수준을 인자로 받는다** (3번째·4번째 인자 → `MiCreateSection` 7번째·8번째).
   - 일반 길은 **지금 프로세스**에서 구한다 (`PsReferencePrimaryTokenWithTag`, `_EPROCESS.SectionSignatureLevel`).
   - 이유 `[추측]` (run 문서): 새 프로세스는 아직 없다. 그러니 부모(cmd.exe) 기준이 아니라 **새 프로세스 기준**으로 검사하려고 따로 받는 것.
   - 비유: 보통 손님은 "당신 신분증 보여 주세요" 로 끝난다. 그런데 이 창구는 **아직 태어나지 않은 가게**의 세트를 짜는 곳이라, 부모가 **새 가게의 신분증과 보안 등급을 대신 들고** 온다.
2. **사용자 인자 처리가 없다.** 할당 속성은 이미지, 페이지 보호 `0x10`, 접근 모드 = 커널.
3. **손잡이를 돌려준다.** `+0x141` (ret `+0x146`) `ObInsertObjectEx` (접근권 `0xf001f`) → `+0x14f` 에서 호출자 칸에 쓴다.
   - 부모 쪽은 `NtCreateUserProcess +0x96d` (ret `+0x972`) `ObReferenceObjectByHandle` 로 꾸러미를 다시 집어 `PspAllocateProcess` 로 넘긴다 (run10 과 맞음).

그 밖에 (대표만): `+0x69` 기능 검사(`Feature_TrustedLaunchHosts…`), `+0x8b` 세션 id, `+0xf6 ~ +0x11c` 파일 마지막 페이지 꼬리 0 채우기, `MiCreateSection` 이 `0xc0000054` 를 돌려주면 `+0xef` `KeDelayExecutionThread` 로 쉬고 다시. 마지막 고리는 일반 길에도 있는 것이라 "특별해서" 가 아니다 (결과서).

- 세션 id 를 구하는 함수는 그 주소에 **PDB 이름이 둘**이다 (`PsGetProcessSessionIdEx`, `PsGetSessionIdEx`). 어느 쪽인지는 안 가렸다 (Q62).

#### 서명 재시도 고리는 왜 도나 — 바로잡힌 그림 `[정적]`

run9 때는 "`MmCreateSpecialImageSection` 이 서명 고리 안에 있다" 까지만 알았다. 이번에 **누가 고리를 돌리는지** 보였다.

1. 이 사슬(`MiCreateSection` 아래)이 실패를 돌려줘서 도는 게 **아니다.**
2. 세트를 다 짠 뒤, `NtCreateUserProcess` 가 서명 수준을 **다시 알아낸다** (`+0x995` `PspGetProcessProtectionRequirementsFromImage`, `+0x9dd` `SeQuerySigningPolicy`).
3. 처음 넘긴 값과 다르면 (`+0x9ea`, `+0xa0d` `SeCompareSigningLevels`) 세트를 닫고, **새 수준으로** `MmCreateSpecialImageSection` 을 다시 부른다.
4. 몇 번 도는지는 실행 때 값이다 → **Q34** (동적).

- 비유: 세트를 받아 보니 설계도의 실제 보안 등급이 처음 적어 낸 것과 다르다. 그러면 주문을 **취소하고, 맞는 등급으로 다시 주문**한다.

### 2.7 `MiCreateImageFileMap` — 설계도를 읽고, 검사하고, 배치표를 그린다

#### 5묶음 `[정적]` (대표 줄은 ret)

| 묶음 | 하는 일 | 대표 줄 | 비유 |
|---|---|---|---|
| A. 준비 | 파일 크기(4GB 넘으면 거절), 헤더 한 페이지 버퍼, 파일 끝 정리 | `+0x96` `FsRtlGetFileSize`, `+0xdb` `MiCreateMdl` | 책상 위에 설계도 첫 장 놓을 자리 마련 |
| B. **헤더 읽기** | 메모리에 이미 있으면 복사, 없으면 디스크에서 | `+0x15c` `MiCopyHeaderIfResident` / `+0x501` `MiReadImageHeaders` | 복사본이 책상에 있으면 그걸 보고, 없으면 서고에서 꺼내 옴 |
| C. **헤더 검사** | `MZ`·`e_lfanew` 범위는 여기서, 나머지 PE 검사는 맡김 | `+0x188` (`0x5a4d` 비교), `+0x1f6` `MiVerifyImageHeader` | 표지 확인, 세부는 검사관에게 |
| D. **배치표 그리기** | 컨트롤 영역·세그먼트·서브섹션·프로토 PTE 배열, 이미지 정보 칸 채우기 | `+0x2e6` `MiBuildImageControlArea` | 서랍·배치표 작성, 맨 위 요약 칸 채우기 |
| E. 뒷정리·옵션 | 커밋 청구, (조건부) 작은 이미지 통째 읽기 / 헤더 페이지 꼬리 0 | `+0x3e1` `MiInitializeImageProtos`, `+0x3fb` `MiInitializeImageHeaderPage` | |

- points.md 가 "exe 헤더 **읽기**" 라고 부른 일은 이 함수 안에서 B·C 이고, 실제로 값이 만들어지는 건 D 의 더 작은 함수 안이다 (결과서 질문 6).

#### 검사관 `MiVerifyImageHeader` 가 보는 것 (대표) `[정적]`

- `PE\0\0` 서명 (`+0x21`), 실행 가능 비트, `Magic` = `0x10b`/`0x20b`, 파일·섹션 정렬, `SizeOfImage` ≤ `0x77000000`, 머신 (`0x8664` 등), `ImageBase` 64KB 정렬, `SizeOfOptionalHeader` 8의 배수.
- 틀리면 이유 번호를 전역(rva `0xe2d838`, 공개 이름 없음)에 적고 `0xc000007b`/`0xc0000130` 을 돌려준다.

#### test.exe 는 통과하나 — 값을 대입해 보면 `[정적]`

- test.exe 헤더 값: `Magic 0x20b`, `Machine 0x8664`, `SectionAlignment 0x1000`, `FileAlignment 0x200`, `ImageBase 0x140000000` …
- 위 검사에 대입하면 **모두 통과**한다.
- 헤더가 첫 페이지 안이라 (`e_lfanew 0x108`, 섹션 6개, `SizeOfHeaders 0x400`, flow.md) 멀리 떨어진 **"더 읽기" 조각**(`+0x1b40b3 ~ +0x1b42f6`) 도 안 탄다.
- 이건 코드에 값을 넣어 본 것이지, 실행을 본 건 아니다. 실제로 서는지는 Q61.

#### 검사하면서 메모지에 옮겨 적는다 `[정적]`

- `MiVerifyImageHeader` 는 검사만 하는 게 아니라, 검사하면서 **칸 모음**(0x70바이트) 을 채운다.
  - `+0x79 ~ +0x7d` `ImageBase` → 칸 `+0x0`
  - **`+0x87 ~ +0x8a` `AddressOfEntryPoint` → 칸 `+0x1c`**
- 여기선 **복사만** 한다. 더하기는 아직 안 한다.
- 비유: 검사관이 설계도 첫 장을 보면서 "기본 번지", "전원 스위치 호수" 를 **메모지에 옮겨 적는다.** 배치표에 정식으로 적는 건 다음 사람(2.9) 몫이다.

### 2.8 핵심 ③ 아직 땅에 안 올린다 — 배치표(프로토 PTE)만

#### 새 말 — 프로토 PTE

- **PTE** = 페이지 표의 한 칸. "이 번지 페이지는 실제로 어디에 있다" 를 적는 칸으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- **프로토 PTE** = 섹션 쪽에 **미리 만들어 두는 원본 칸들**. 나중에 프로세스에 붙일 때 이걸 바탕으로 쓴다고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유: 방 배치표의 **한 줄 한 줄**. "3번 방 = 설계도 몇 쪽" 처럼 적혀 있다.
- 이번 근거에서 PTE 의 뜻은 **함수 이름으로 본 것**이라 `[추측]` 이다 (`MiMakeSubsectionPte`, `MiMakeDemandZeroPte`, run 문서).

#### 무엇이 `[정적]` 이고 무엇이 `[추측]` 인가 — 단계별로

1. `[정적]` 이 사슬 어디에도 `MmMapViewOfSection*` 이 **없다.** 즉 프로세스 땅에 갖다 놓는 호출이 없다.
2. `[정적]` 헤더 **한 페이지만** 물리 메모리에 읽어 둔다 (2.7 의 B 묶음).
3. `[정적]` `MiBuildImageControlArea` 는 컨트롤 영역+서브섹션, 프로토 PTE 배열, 세그먼트를 만들고, 섹션 표를 훑어 서브섹션별 PTE 를 채운다 (`+0x367` `MiParseImageSectionHeaders`). 코드 구역에서 파일 내용을 읽어 오는 호출은 이 함수에 없다 (결과서).
4. `[추측]` 그러니 나머지 칸은 "이 조각은 파일의 이 자리" 라는 **표시**일 것이다 (이름 기준).
5. → 결론: **이 단계에선 배치표만 그린다.** 자재(파일 내용)는 거의 안 실었다.

#### 실제로 땅에 놓는 건 나중 `[정적]` (run10)

- `MmInitializeProcessAddressSpace +0x1ce` → `MiMapProcessExecutable +0xb8` `MmMapViewOfSectionEx`.
- flow.md 그림 1 로는 `p2` → `p3` → `p4` 다. 이번에 그린 다섯 상자(`s3` 아래)와는 **다른 가지**다.
- 비유: 관리사무소는 서류 단계에서 **배치표만** 그려 둔다. 땅을 받은 뒤(`PspAllocateProcess`) 그 배치표를 들고 가서 붙인다 (run10 해설 [3.5](run10-allocate-process-해설.md#35-exe-세트는-run9-에서-만든-바로-그것)).

### 2.9 핵심 ④ 진입점이 처음 적히는 줄 — `MiBuildImageControlArea +0x172`

#### 단계별로 `[정적]`

| 단계 | 어디 | 무엇 |
|---|---|---|
| ① | PE 파일 | NT 헤더 `+0x28` `AddressOfEntryPoint`, `+0x30` `ImageBase`. test.exe: `0x1d40` / `0x140000000` |
| ② | `MiVerifyImageHeader +0x79 ~ +0x8a` | 칸 모음 `+0x0` ← `ImageBase`, **`+0x1c` ← `AddressOfEntryPoint`** (복사만) |
| ③ | **`MiBuildImageControlArea +0x16b ~ +0x172`** | `[R15+0x1c]` + `[R15]` → **`[R8]` = 이미지 정보 `+0x0` `TransferAddress`** |

- ③ 이 2.2 "한 줄 읽기 ②" 의 세 줄이다.
- 이 시점의 값은 **헤더의 `ImageBase` 기준**이다. test.exe 면 `0x140001d40` 일 것이다 — 실행으로는 아직 안 봤다 (Q61).
- 결과서가 ②·③ 을 **원본 바이트**로도 다시 봤다.

#### 왜 ② 가 아니라 ③ 이 "처음" 인가

- ② 는 헤더 숫자 rva(`0x1d40`) 를 메모지에 **옮겨 적기만** 한다. 아직 "주소" 가 아니다.
- ③ 에서 처음으로 **기본 주소를 더해 진짜 주소 모양**이 되고, 그게 이미지 정보의 **정식 칸**(`TransferAddress`)에 들어간다.
- 그래서 run 문서는 ③ 을 "**진입점 주소가 처음 만들어지는 줄**" 이라고 부른다.
- 요청서는 처음에 `MiVerifyImageHeader` 까지만 내려가도 된다고 했는데, 답은 그 **아래가 아니라 옆**(`MiBuildImageControlArea`)에 있었다. 결과서가 그래서 한 단계 더 내려갔다.

#### 그 칸은 어디 사나 `[정적]` (PDB 타입)

- 이미지 정보는 `MiBuildImageControlArea` 가 만든 **세그먼트**(0x98바이트) 안 `+0x48` 쪽에 산다.
- 세그먼트 `+0x38` (`_SEGMENT.u2.ImageInformation`) 이 그 주소를 가리킨다.
- 앞 0x40 이 `_SECTION_IMAGE_INFORMATION` 이고, 그 `+0x0` 이 `TransferAddress` 다.
- 같은 함수가 스택 크기·서브시스템·OS 버전·`Machine`·`DllCharacteristics` 칸도 채운다 (결과서 표).
- 비유: 배치표 묶음 맨 앞에 붙은 **요약 카드**. 맨 윗줄이 "첫 출근 방", 그 아래에 "작업대 크기", "어느 나라 규격" 같은 것들.
- 그리고 run16 의 `MmGetSectionInformation` 이 바로 **세그먼트 `+0x38` 이 가리키는 이 카드 0x40바이트를 복사**하는 함수였다. 그래서 ③ 에서 쓴 칸과 run16 에서 꺼낸 칸이 **같은 칸**이라고 이어진다 `[정적]`.

### 2.10 재배치 델타 — 다른 번지에 놓을 거면 그 칸을 고쳐 쓴다

#### 새 말 — ASLR, 델타

- **ASLR** = 실행할 때마다 모듈을 놓는 기준 주소를 무작위로 바꾸는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). PE 헤더 `DllCharacteristics` 의 `DYNAMIC_BASE` 표시가 "나를 옮겨도 된다" 는 뜻이다 `[추측: 일반적으로 알려진 설명]`.
- **델타** = 새로 고른 기본 주소 − 헤더에 적힌 기본 주소 (run 문서). 이만큼 "밀렸다" 는 차이다.
- 비유: 설계도엔 "이 건물은 140번지 기준" 이라고 적혀 있는데, 실제로는 다른 번지에 놓기로 했다. 그러면 배치표의 번지수를 **차이만큼 다 밀어서** 고쳐 써야 한다 (run26 의 "번지수 고쳐 쓰기" 와 같은 그림).

#### `TransferAddress` 두 번째 쓰기 `[정적]`

- `MiCreateNewSection +0x3da` 에서 `MiRelocateImage` 를 부른다. 조건: **새 기본 주소를 골랐을 때** (run 문서).
- 그 안 멀리 떨어진 조각 `+0x1b1279` (`FUN_140b24658 +0x65`) 가 **`ADD [R15],RAX`** — `TransferAddress` 칸에 델타를 더한다.
- 같은 조각이 `ImageFlags` 의 `ImageDynamicallyRelocated` 를 켠다 (`+0x1b1357`) `[정적]`. "이 배치표는 번지를 옮겼음" 이라는 표시로 보인다 `[추측: 이름]`.
- 이번에 `MiRelocateImage` 는 **앞부분과 이 조각만** 봤다. 함수 전체를 읽은 건 아니다.
- 이 사슬에서 `TransferAddress` 를 쓰는 줄은 ③·④ **둘**이다. 안 읽은 함수에 더 있는지는 모른다 `[추측: 이름으로는 아닐 것 같음]` (Q62).

#### test.exe 에서 실제로 더해지나 — 짐작을 단계별로

1. 근거 `[정적]`: test.exe 는 `DllCharacteristics 0x8160` (`DYNAMIC_BASE` 포함) 이고, 재배치 디렉터리가 있다.
2. 근거: run30 이 run5·run7 `[관찰]` 값으로 계산한 실제 베이스 `0x7ff70ee10000` 은 헤더의 `0x140000000` 과 **다르다.**
3. 그래서 → **어디선가는 옮겨진다.** 이 조각에서 더할 것이라는 게 flow.md 의 `[추측]` 이다.
4. 아직 확인 아님:
   - 이 조각에서 더하는지, 아니면 나중 `PspAllocateProcess +0x20b3 ~ +0x20ce` (run16 보정) 에서 고치는지는 **동적**이다 (Q61·Q35).
   - run16 보정은 "④ 까지 반영된 값이 실제로 올라간 주소와 다를 때" 쓰는 것으로 보인다 `[추측]` (이번에 `PspAllocateProcess` 는 다시 안 읽음, Q62 (b)).
- 비유: 번지수를 고쳐 쓰는 사람이 **둘** 보인다. 서류 단계의 `MiRelocateImage` 와, 가게 짓는 단계의 `PspAllocateProcess` 보정. test.exe 때 누가 펜을 드는지는 버스를 타 봐야 안다.

### 2.11 진입점 값이 흘러가는 길 — 처음부터 끝까지

이번 바퀴로 빈칸이 다 찼다. run 문서·flow.md 의 같은 이름 절을 표로 풀었다.

| # | 어디 | 무엇 | 태그 | 비유 | 어느 해설 |
|---|---|---|---|---|---|
| 1 | PE 파일 NT 헤더 `+0x28` | `AddressOfEntryPoint` (test.exe `0x1d40`) | `[정적]` (PE 파일) | 설계도 첫 장 "전원 스위치는 몇 호" | [run25 2.3](run25-dll-entry-해설.md#새-말--pe-진입점-addressofentrypoint) |
| 2 | `MiVerifyImageHeader +0x8a` | 칸 모음 `+0x1c` 에 복사 | `[정적]` | 검사관의 메모지 | 이번 2.7 |
| 3 | **`MiBuildImageControlArea +0x172`** | `TransferAddress = ImageBase + rva` | `[정적]` | 배치표 요약 카드 맨 윗줄 "첫 출근 방" | 이번 2.9 |
| 4 | `MiRelocateImage` 조각 `+0x1b1279` | 새 기본 주소면 델타 더함 (조건부) | `[정적]`, 실행 Q61 | 번지수 고쳐 쓰기 | 이번 2.10 |
| 5 | `PspAllocateProcess +0x991` `MmGetSectionInformation` | 이미지 정보 0x40바이트 복사 | `[정적]` run16 | 세트를 땅에 놓을 때 요약 카드를 한 장 복사 | [run16 2.4](run16-thunk-context-해설.md#24-장면--처음-메모--pspcreateusercontext-가-채우는-값) |
| 6 | `PspCreateUserContext` | `CONTEXT.Rcx` 에 씀 | `[정적]` run16 | 업무 시작 메모 "처음 손에 쥘 것" | [run16 2.4](run16-thunk-context-해설.md#24-장면--처음-메모--pspcreateusercontext-가-채우는-값) |
| 7 | `LdrInitializeThunk +0x18` `NtContinue` → 트랩 프레임 `Rcx` → `RtlUserThreadStart(rcx)` | 맡겨 둔 메모대로 출발, `Rcx` 가 산다 | `[정적]` run29 | 사물함 메모대로 본업 자리로 순간 이동 | [run29 2.11](run29-continue-start-해설.md#211-유저로-나가기--반환-없이-iretq-그래서-rcx-가-산다) |
| 8 | `RtlUserThreadStart +0x2c` → `BaseThreadInitThunk +0x17` → 진입점 | 진입점으로 간접 호출 | `[정적]` run29·run25, 대상 Q48·Q30 | 출근 인사 담당이 현관으로 안내 | [run25 2.6](run25-dll-entry-해설.md#26-핵심--basethreadinitthunk-는-kernel32-안-진짜-코드다) |
| 9 | `test!mainCRTStartup` (rva `0x1d40`) | run30 경계, 여기부터 CRT | `[정적]` run30 | **현관** | [run30 2.1](run30-exe-crt-start-해설.md#21-먼저--지도의-경계가-정해졌다) |

#### 이야기로 한 번에

1. 관리사무소가 설계도 첫 장에서 "전원 스위치는 `0x1d40` 호" 를 읽는다. 검사관이 메모지에 옮겨 적는다 (2).
2. 배치표를 그리면서, 요약 카드 맨 윗줄에 **"첫 출근 방 = 기본 번지 + `0x1d40`"** 을 정식으로 적는다 (3).
3. 건물을 다른 번지에 놓기로 했으면, 그 줄을 차이만큼 고쳐 쓴다 (4).
4. 가게 땅에 세트를 놓을 때 요약 카드를 복사해 간다 (5).
5. 첫 직원의 업무 시작 메모에 "처음 손에 쥘 것 = 그 번지" 를 적는다 (6).
6. 직원은 오리엔테이션(로더)을 먼저 받고, 사물함에 맡겨 둔 메모대로 본업 자리로 간다. 손에는 아직 그 번지가 있다 (7).
7. 출근 인사 담당이 그 번지 — **현관** — 으로 안내한다 (8, 9).

- 비유로 말하면: run30 에서 "관리사무소가 송장에 **현관 번지**를 적어 준다" 고 했다. 이번에 그 번지가 **어디서 왔는지** — 배치표 요약 카드 맨 윗줄 — 가 보였다.

#### 숫자로 보면 (예측과 관찰)

- 3 에서 `0x140001d40` 일 것 — 정적 예측, Q61 (`nt+0x971136` 에서 `rax`).
- 4 에서 델타 = 새 기본 주소 − `0x140000000` — Q61 (`nt+0xb246bd` 가 서면 `rax`).
- 끝에서 본 값: run5 `[관찰]` 메인 스레드 시작 주소 `0x7ff70ee11d40` (run30 해설 1절). run30 은 여기서 베이스 `0x7ff70ee10000` 을 계산했다.
- 그 사이 **어느 줄에서 `0x140…` 이 `0x7ff7…` 로 바뀌는지**가 Q61·Q35 의 핵심 질문이다.

### 2.12 정리 — 비유로 한 번 더

- 부모 가게 직원이 관리사무소 창구에 "새 가게 하나요" 를 낸다 (`NtCreateUserProcess`).
- 관리사무소는 먼저 **설계도(exe 파일)** 를 꺼내고 (`IopCreateFile`), **조립식 세트 주문**을 넣는다 (`MmCreateSpecialImageSection`). 아직 태어나지 않은 가게의 신분증·보안 등급을 대신 들고 간다.
- 주문 창구는 먼저 **서랍**을 본다 (`MiReferenceControlArea`).
  - 같은 설계도의 배치표가 있으면 "나도 씀" 하고 같이 쓴다 (`MiShareExistingControlArea`).
  - 없으면 "작성 중" 팻말을 꽂고 새로 그린다 (`MiCreateNewSection` → `MiCreateImageFileMap`).
- 새로 그릴 때는:
  - 설계도 첫 장을 책상에 놓고 (헤더 한 페이지 읽기),
  - 표지(`MZ`)와 세부(`MiVerifyImageHeader`)를 검사하면서 기본 번지·전원 스위치 호수를 메모지에 옮기고,
  - 배치표(프로토 PTE)와 요약 카드를 만들고, 카드 맨 윗줄에 **"첫 출근 방"** 을 적는다 (`MiBuildImageControlArea +0x172`).
  - 다른 번지에 놓기로 했으면 그 줄을 고쳐 쓴다 (`MiRelocateImage`).
- 이때 **땅에는 아직 아무것도 안 놓는다.** 손잡이 달린 주문 꾸러미만 부모 직원에게 건넨다 (`ObInsertObjectEx`).
- 꾸러미를 받은 뒤 보안 등급이 처음 적어 낸 것과 다르면, 주문을 취소하고 다시 넣는다 (서명 재시도 고리).
- 땅을 받으면 그때 배치표를 붙이고(run10), 요약 카드를 복사해 업무 시작 메모로 옮긴다(run16).

### 2.13 곁가지 — 도구 메모

- 결과서는 callee 가 100개 가까이 되어서, depth 1 대신 **depth 0 으로 21개**를 이름으로 골라 뽑았다. `MiBuildImageControlArea`·`MiRelocateImage` 도 그래서 들어왔다.
- 구조체 칸 이름은 PDB 타입 덤프(`llvm-pdbutil`)로 맞췄다. 이걸 뽑는 명령이 스킬에 없어서 이번에도 임시 스크립트를 썼다 → 결과서 "스크립트 문제" 제안.
- `.asm` 의 전역 주석은 PDB 공개 이름이 있는 것만 붙는다. 실패 이유 번호 전역(`[0x140e2d838]`) 은 이름이 없어서 주석이 없다. 오류가 아니다.
- `refs` 의 `(함수 밖) DATA` 줄은 `.pdata` (예외표) 항목이다. 부르는 곳이 아니다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp.pdb 쓴 run 목록에 run31 없음, 정적 근거 run9 ~ run30 | run31 추가, 정적 근거 run9 ~ run31. test.exe 헤더 값도 `sample/bin` 같은 파일을 직접 읽었다고 적음 |
| 그림 1 `s3` | "정적: 이미지 섹션" | "핸들을 돌려줌, 서명 재시도 고리 안" 추가 |
| 그림 1 `s3` 아래 | 없음 | **`(정적)` 상자 다섯** 한 줄 사슬: `MiCreateSection` → `MiCreateImageOrDataSection` (갈림길) → `MiCreateNewSection` → `MiCreateImageFileMap` → `MiBuildImageControlArea`. 화살표 `+0xd4`/`+0xf8`/`+0x385`/`+0x215`/`+0x2e6` |
| 2.1 트리 | `+0x93c MmCreateSpecialImageSection [정적]` 한 줄 | 그 아래 사슬 전체 (갈림길, 세 고리, `MiRelocateImage`) |
| 2.1 `MmCreateSpecialImageSection` 절 | `[추측]` "`MiCreateImageFileMap` 은 안쪽인지 모른다" | 취소선 → **안쪽이 맞다 `[정적]`**, 단 공유 길이면 건너뜀. "특별한" 이유, 서명 재시도 고리 설명, 배치표 비유 추가 |
| 2.1 새 절 | 없음 | `MiCreateSection` → `MiCreateImageOrDataSection`, `MiCreateNewSection`, `MiCreateImageFileMap` (points.md 단계 3), `MiVerifyImageHeader`, `MiBuildImageControlArea`, `MiRelocateImage`, **"진입점 값이 흘러가는 길"** |
| 2.1 `PspCreateUserContext` 절 | 진입점 칸을 꺼내 쓰는 쪽만 | 그 칸의 **처음 출처** = `MiBuildImageControlArea +0x172`, 재배치 때 `MiRelocateImage` 조각 한 줄 추가 |
| 2.1 "아직 어디서 하는지 모르는 일" | "남은 것: `MiCreateImageFileMap` `[추측]`" | 취소선 → **해결됨** (사슬) |
| 2.10 "경계" 줄 | 커널이 그 값을 `Rcx` 로 넘긴다 (run16) | 헤더 값이 처음 적히는 곳부터의 길은 2.1 을 보라는 줄 추가 |
| 4장 | 2.1 `MiCreateImageFileMap` 어디서 불리나 (빈칸) | **해결됨** 두 줄 (사슬, 진입점 처음 출처). 새 줄 넷: **Q61**, **Q35** (D6), **Q34** (횟수), **Q62** |
| 4.1 | 그림 1 nt `MmCreateSpecialImageSection` 두 번 불리는지 | Q34 멈춤점 `nt+0x96bd64` 추가. 새 줄: 상자 다섯의 확인 방법 (Q61) |

- 상자를 **다섯** 둔 이유 (run 문서): 대표 지점은 결과서 후보 셋(`MiCreateImageOrDataSection`, `MiCreateImageFileMap`, `MiBuildImageControlArea`)이다. 그런데 사이의 둘을 빼면 화살표에 진짜 ret 을 못 쓰고 `?` (추측) 로 그려야 한다. 코드로 확인한 길이 추측처럼 보이면 안 되니까 넣었다.
- `MiShareExistingControlArea`·`MiVerifyImageHeader`·`MiRelocateImage` 는 상자 글에만 적었다.
- **points.md 는 고치지 않았다.** 판정(대표 지점으로 둬도 됨 + "컨트롤 영역이 없을 때만" 조건 + 값이 바뀌는 순간 `MiBuildImageControlArea +0x172` 추가 권고)은 오케스트레이터가 판단할 몫이다 (run 문서).

### 큰 그림에서 어떻게 이어지나

- **진입점 실이 한 가닥으로 이어졌다.** 설계도 첫 장의 숫자 하나(`0x1d40`)가 관리사무소의 배치표 → 가게 짓기 → 업무 시작 메모 → 관리실 오리엔테이션 → 본업 자리 → 현관까지 **어디서 손을 바꾸는지** 정적으로는 다 보인다 (2.11).
  - 남은 건 "**어느 줄에서 실제 번지로 바뀌나**" 하나다. 이건 버스를 타 봐야 안다 (Q61·Q35).
- **그림 1 의 `s3` 가 더 이상 "속 모를 상자" 가 아니다.** run9 에선 이름뿐이었고, run10 은 결과물(세트)이 어디로 가는지만 봤다. 이번에 **세트를 어떻게 짜는지** 가 채워졌다.
- **동적 단계 준비**: Q34·Q35(D6)·Q61 은 **같은 `NtCreateUserProcess` 한 번 안**에서 차례로 선다 (plan.md). 순서는 D1 → D2 → D3 → D4 → D5 → (섹션 반환) → D6 (결과서). 동적 차례가 오면 한 실행으로 묶기 좋은 후보다.
  - 단 2.5 의 함정 때문에 **재부팅 직후 첫 실행**(또는 파일 복사)으로 해야 D2 ~ D5 가 선다.
- 다음 바퀴: 단계 3 정적 묶음의 다음 항목 **Q36** (plan.md). 정적 묶음이 끝나면 동적 Q19 부터다.

---

## 4. 아직 모르는 것

멈춤점은 모두 커널이라 **모든 프로세스에 걸린다.** test.exe 일 때만 서게 조건이 필요하다 (`_FILE_OBJECT.FileName`, dast-mentor 몫).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 실행 때 **새로 그리기**(`MiCreateNewSection`) 와 **같이 쓰기**(`MiShareExistingControlArea`) 중 어느 쪽을 타나 | 서랍에 배치표가 있나는 실행 때 값 (이 부팅에 이미 돌렸나) | 동적 **Q61** (D1): `nt+0x96f4d0` / `nt+0x96e6cc` 중 어느 쪽이 서나. 재부팅 직후 첫 실행, 또는 파일 복사 |
| `MiCreateImageFileMap` 입구의 실제 콜스택이 정적 사슬과 같은가 | 정적은 코드 모양만 | 동적 **Q61** (D2): `nt+0x96ff50` 에서 `k` — `MiCreateNewSection+0x215` ← `MiCreateImageOrDataSection+0x385` ← `MiCreateSection+0xf8` ← `MmCreateSpecialImageSection+0xd4` ← `NtCreateUserProcess+0x93c` 인가 |
| 진입점 rva 와 처음 만들어지는 주소의 실제 값 | 실행 때 값 | 동적 **Q61** (D3·D4): `nt+0x970886` `eax` (기대 `0x1d40`), `nt+0x971136` `rax` (기대 `0x140001d40`), 한 칸 뒤 `dq @r8 L1` |
| `MiRelocateImage` 조각이 test.exe 때 델타를 실제로 더하나, 델타는 얼마인가 | 새 기본 주소를 골랐나는 실행 때 값. `MiRelocateImage` 는 조각만 봄 | 동적 **Q61** (D5): `nt+0xb246bd` 가 서나, 서면 `rax`. 안 서면 이 길로는 안 더한 것 |
| `TransferAddress` 가 `PspAllocateProcess` 까지 안 바뀌고 오나 | `PspAllocateProcess` 쪽 실행 길은 안 봄 | 동적 **Q35** (D6): `nt+0xa02ef9` (`PspAllocateProcess +0x991` ret 직후) `dq @r15+0x38 L1` 이 위 값과 같은가 |
| `MmCreateSpecialImageSection` 을 몇 번 지나나 (서명 재시도) | 서명 수준 비교는 실행 때 값 | 동적 **Q34**: `nt+0x96bd64` 입구 횟수, ret `nt+0xa01c7c` (`NtCreateUserProcess +0x93c`) 의 `eax` |
| 서랍(컨트롤 영역)이 프로세스가 끝난 뒤 얼마나 남나 | 정적으로 보기 어렵다 `[추측]` | 정적 대신 **Q61 재실행 비교**로 본다 |
| 안 읽은 함수(`MiParseComAndCetHeaders`, `MiMakeImageReadOnly`, `MiInitializeNewImageSectionProtos`, `MiValidateSectionSigningPolicy`, `MiReleaseImageSection`, `MiFinalizeImageHeaderPage`)에 `TransferAddress` 를 쓰는 줄이 더 있나 | 이번 범위 밖 (depth 0 21개) | 정적 **Q62 (a)** |
| `PspAllocateProcess +0x20b3 ~ +0x20ce` 보정 조건과 `MiRelocateImage` 델타의 관계 | 지금 `[추측]`. `PspAllocateProcess` 를 다시 안 읽음 | 정적 **Q62 (b)** (+ 동적 Q61·Q35 로 값 비교) |
| 같이 쓰기 길 안쪽: `MiShareExistingControlArea` → `MiValidateExistingImage` ("다시 쓸 만한지 확인" 으로 보임 `[추측]`) | 호출 표와 일부 줄만 봄 | 정적 **Q62 (c)** |
| 헤더 페이지가 프로토 PTE 쪽으로 넘어가는 곳 (`MiSectionCreated +0x103` `MiInitializeNewImageSectionProtos`), PTE 의 뜻 | 이 함수 안 읽음. PTE 뜻은 이름으로 본 것 `[추측]` | 정적 **Q62 (d)** |
| 작은 것들: `MiVerifyImageHeader` 이유 번호 0x57 ~ 0x59 검사, `Feature_TrustedLaunchHosts` 의 뜻, `+0x8b` 세션 id 함수 이름 둘 중 어느 쪽, `MiCreateSection` 안 재시도가 위로 올라오는 경우 | 대표 지점 목표엔 덜 중요해서 안 풀었다 | 정적 **Q62 (e)** |
| 파일 이미지 섹션을 함수 포인터로 만드는 다른 길이 있나 | `refs` 는 CALL 참조만 본다 | 번호 없음 (run 문서 주의) |

---

## 5. 더 읽기

- run 문서: [`run31-image-section.md`](run31-image-section.md) — 호출 지점 표, 갈림길(`MiReferenceControlArea`) 세 갈래, "특별한" 이유, `MiCreateImageFileMap` 5묶음, `MiVerifyImageHeader` 검사, 진입점 → `TransferAddress` 표, 호출 관계 트리, "진입점 값이 흘러가는 길", 그림 반영, `[추측]` 판정, 동적 확인거리, 디버거·도구 메모
- 정적 결과서: [`_workspace/sast/26_image-section_result.md`](../../_workspace/sast/26_image-section_result.md) — 질문 1 ~ 6 답, 패킷 칸, `MmCreateSpecialImageSection` 인자 6개, 검사 표(이유 번호), 이미지 정보 칸 표, 칸 모음(`ctx`) 칸 이름, 동적 D1 ~ D6
- 요청서: [`_workspace/sast/26_image-section.md`](../../_workspace/sast/26_image-section.md)
- flow.md: [`flow.md`](flow.md) — 1장 그림 1 (`s3` 아래 상자 다섯), 2.1 `nt!MmCreateSpecialImageSection` ~ "진입점 값이 흘러가는 길", 2.1 `nt!PspCreateUserContext`, 2.10 "CRT 안쪽 (참고)" 의 경계 줄, 4장 2.1 줄들
- 앞 해설과 잇기:
  - [run9 해설](run9-create-user-process-해설.md) — 이미지 섹션(조립식 세트), 핸들(손잡이), 서명 재시도 고리를 처음 본 곳
  - [run10 해설 3.5](run10-allocate-process-해설.md#35-exe-세트는-run9-에서-만든-바로-그것) — 세트를 땅에 놓는 쪽
  - [run16 해설 2.4](run16-thunk-context-해설.md#24-장면--처음-메모--pspcreateusercontext-가-채우는-값) — `TransferAddress` 를 꺼내 `Rcx` 로
  - [run29 해설 2.11](run29-continue-start-해설.md#211-유저로-나가기--반환-없이-iretq-그래서-rcx-가-산다) — `Rcx` 가 본업 자리까지 사는 이유
  - [run25 해설 2.6](run25-dll-entry-해설.md#26-핵심--basethreadinitthunk-는-kernel32-안-진짜-코드다) — 출근 인사 담당 `+0x17`
  - [run30 해설 2.1](run30-exe-crt-start-해설.md#21-먼저--지도의-경계가-정해졌다) — 현관(진입점) = 지도의 경계
