# run73 image-base-select — 해설

> 이 글은 [run73 문서](run73-image-base-select.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q139a**: PE_emulator 요청 23 의 3번, "dll1·dll2 맵 결과가 `0` 인가 `0x40000003` 인가" 를 정적으로 닫기 (`_workspace/plan.md`, 이번에 **끝**)
  - 새로 생긴 할 일: **Q140** (정적 셋 + 사용자 결정 하나, 맨 뒤). 4절에서 다룬다.
  - 동적 몫은 **Q104 VM 단계** 에 얹었다.
  - 다음 항목: **Q104 VM 단계**. 여기서 멈춘다.
- 근거:
  - run 문서 [`run73-image-base-select.md`](run73-image-base-select.md) (가장 중요)
  - 요청서 [`_workspace/sast/68_image-base-select.md`](../../_workspace/sast/68_image-base-select.md)
  - 결과서 [`_workspace/sast/68_image-base-select_result.md`](../../_workspace/sast/68_image-base-select_result.md) (판정: **분석성공**)
- 이번 바퀴는 **정적만**, 그리고 **작은 바퀴** 다. 핵심은 하나다.

이미 푼 말은 **링크만** 건다.

- 이미지 섹션 = **조립식 건물 세트**, 매핑 = 세트를 땅에 **갖다 놓기** → [run9 해설](../02_sprint/run9-create-user-process-해설.md#이미지-섹션) · [run10 해설](../02_sprint/run10-allocate-process-해설.md#매핑-올리기)
- 컨트롤 영역 = 설계도 서류함의 **서랍** (같은 설계도면 서랍 것을 같이 씀) → [run31 해설 2.5](../02_sprint/run31-image-section-해설.md#새-말--컨트롤-영역)
- `SEC_IMAGE` = **"조립식 세트로 짜 주세요" 도장** → [run26 해설](../02_sprint/run26-map-dll-file-해설.md)
- 락 = **"작성 중" 자물쇠** → [run11 해설](../02_sprint/run11-allocate-thread-해설.md)
- `0x40000003` = 창구 대답 "**놓았어요. 그런데 원래 자리가 아닌 곳에 놓였어요**" → [run38 해설](../02_sprint/run38-map-module-해설.md)
- 비트맵 = 관리사무소의 **구획 배정표**, 재시도 길, dll1·dll2 가 둘 다 `0x180000000` 을 원한다는 것 → [run72 해설 2.10](run72-pool-work-post-해설.md)

### 한 줄로 먼저

- dll1·dll2 는 헤더에 **같은 자리** (`0x180000000`) 를 원한다.
- 그런데 커널은 그 헤더 값을 안 쓰고, 세트를 짤 때 **구획 배정표에서 파일마다 따로 구획을 정해 준다** `[정적]`.
- 그래서 두 DLL 은 서로 부딪히지 않는다 → 그 구획이 test.exe 안에서 비어 있으면 맵 결과 `0`, 재시도 길은 안 탄다 `[정적]`.
- 실제 값은 아직 실행해서 본 게 아니다 (Q104).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 출발
 └─ ntdll LdrInitializeThunk → … LdrpInitializeProcess
      ├─ +0x1aa4 LdrpMapAndSnapDependency          exe 가 import 하는 DLL 들이기
      │    ├─ … LdrpMapDllNtFileName → NtCreateSection (SEC_IMAGE)
      │    │       세트 짜기 → 커널이 "놓을 구획" 을 정함                    ← 이번
      │    └─ … LdrpMinimalMapModule → NtMapViewOfSection
      │            세트 갖다 놓기 → 창구 대답 0 / 0x40000003
      │            └─ (0x40000003 이면) 재시도 길  (run72)
      └─ … NtContinue 은퇴 → main
```

- 이번에 본 곳은 **세트를 짜는 순간** (`NtCreateSection` 안, 커널) 이다.
- 창구 대답이 정해지는 건 그 뒤 **갖다 놓는 순간** 이지만, 대답을 가르는 기준이 짜는 순간에 정해진다. 그래서 여기를 봤다.

### 무엇이 궁금했나

- run72 에서 이런 갈림이 남았다:
  - 커널 `MiSelectImageBase` 의 넷째 인자 **비트 0** 이 켜지면 → 헤더 값 그대로 (`0x180000000`) → 두 DLL 이 같은 자리를 받으니 **둘째 DLL 은 반드시 `0x40000003`** → 재시도 길.
  - 꺼져 있으면 → 구획 배정표에서 따로 → 보통 `0`.
- 그 비트를 실제로 주는 쪽 (`MiCreateNewSection`) 은 안 읽었다. **그 비트가 뭔가?** 가 이번 질문이다.

### 바퀴 전에 알던 것

- `[정적]` (run72) 창구가 `0x40000003` 을 쓰는 건 `MiMapViewOfImageSection +0xc0a` 한 자리. 조건은 "실제로 놓인 번지 ≠ 섹션의 기준 주소 (`_SEGMENT.BasedAddress`)".
- `[정적]` (run72) 그 기준 주소는 세트를 짤 때 `MiSelectImageBase` 가 고른다. 갈림은 넷째 인자 비트 0. 그 비트는 `MiRelocateImage` 여섯째 인자 `& 1` 에서 온다.
- `[공백]` (run72) `MiCreateNewSection` 이 그 여섯째 인자에 무엇을 주는지.
- `[관찰]` (run44 P1b, run72 가 옮김) dll1 `DllBase` = `OriginalBase` = `0x7ffae12e0000`, dll2 `0x7ffadb950000`.

---

## 2. 찾은 것

### 2.1 한눈에

```
NtCreateSection → … MiCreateImageOrDataSection
 └─ (새 파일) MiCreateNewSection
      │  +0x361 · +0x38b · +0x391 : 여섯째 인자 만들기 → 비트 0 늘 0        ★ 2.2
      └─ +0x3d5 / +0x695  MiRelocateImage (여섯째 = 그 값)
           └─ +0x2e6  MiSelectImageBase (넷째 = 여섯째 & 3)
                ├─ 비트 0 = 1 : 헤더 ImageBase 그대로        ← dll1·dll2 는 안 탐
                └─ 비트 0 = 0 · DLL : 구획 배정표에서 고르기   ← dll1·dll2       ★ 2.3
                     └─ +0x1ab MiObtainRelocationBits
                          └─ +0x86 RtlFindClearBitsAndSetEx  (자물쇠 안에서 찾기+켜기)
           └─ 조각 +0x1b1269 : 고른 주소를 _SEGMENT.BasedAddress 에 적기
```

- 모두 `[정적]` (run 문서 "호출 관계").
- 비트 0 을 1 로 주는 곳은 **딱 하나**: `MmLoadSystemImageEx +0x202 → MiGetSystemAddressForImage`. 커널 드라이버·시스템 이미지를 올리는 길이다. test.exe 의 DLL 길이 아니다 `[정적]`.

### 2.2 ★ 비트 0 이 늘 0 인 이유 — `AND ESI,2` 한 줄

먼저 새 말 하나.

- **패킷** = 커널이 섹션을 짜는 동안 들고 다니는 상태 덩어리. 맨 앞 4바이트가 플래그 모음이다 `[정적]`. 공개 PDB 에 이 형 이름은 없다.
  - 비유: 세트 **주문서**. 맨 앞 줄에 체크박스 32칸이 있다.

#### 한 줄 읽기 — 체크박스 줄을 밀고, 거르고, 넘긴다

```
MiCreateNewSection +0x38b  SHR ESI,0x1b
MiCreateNewSection +0x391  AND ESI,0x2
MiCreateNewSection +0x3cc  MOV dword ptr [RSP+0x28],ESI
```

- **누가**: `MiCreateNewSection` 이
- **어디서**: 바로 앞 `+0x361` 에서 주문서 맨 앞 4바이트 (체크박스 32칸) 를 ESI 에 복사해 둔 상태로
- **`+0x38b` 밀기**: `SHR ESI,0x1b` = 칸들을 **오른쪽으로 27칸** 민다.
  - 원래 27번 칸 → 0번 자리
  - 원래 28번 칸 → 1번 자리
- **`+0x391` 거르기**: `AND ESI,0x2` = **1번 자리만 남기고 나머지는 다 지운다.**
  - 그래서 값은 `0` 아니면 `2` 뿐.
  - 0번 자리 (원래 27번 칸) 는 **여기서 무조건 지워진다.** ← 이게 "비트 0 은 늘 0" 이다.
- **`+0x3cc` 넘기기**: 그 값을 `MiRelocateImage` 의 **여섯째 인자 칸** (`[RSP+0x28]`) 에 놓는다.
- **무엇을 위해**: `MiRelocateImage` 는 이 값 `& 3` 을 그대로 `MiSelectImageBase` 넷째 인자로 준다 (`+0x2b0 ~ +0x2c8`). 비트 0 이 0 이니 넷째 인자 비트 0 도 0.

#### "사이에 ESI 가 바뀌면?" — 확인했다 `[정적]`

- 둘째 부르는 자리도 같은 ESI 를 쓴다 (`+0x68c MOV [RSP+0x28],ESI`).
- 함수 안에서 ESI 를 새로 쓰는 줄은 18곳. 그중 `+0x3cc`·`+0x68c` 에 닿는 건 **`+0x391` 하나뿐** (결과서 1번 착지점 표).
- `+0x38b ~ +0x394` 한가운데로 밖에서 뛰어드는 점프도 없다.
- 사람 눈 + 임시 스크립트, 두 방법이 같은 답을 냈다 (결과서).

#### 비유로 한 번 더

- 주문서에 "헤더에 적힌 자리 그대로 놓아 주세요" 체크박스가 있다고 치자.
- 그런데 접수 직원 (`MiCreateNewSection`) 은 주문서를 옮겨 적을 때 **그 칸을 늘 지우고** 넘긴다.
- 그 칸을 체크해서 넘기는 건 **드라이버 접수 창구** (`MmLoadSystemImageEx` 길) 뿐이다.

### 2.3 그래서 dll1·dll2 는 구획 배정표에서 따로 받는다

비트 0 이 0 이고 DLL 이면, `MiSelectImageBase` 는 이렇게 간다 `[정적]`:

1. 구획 배정표 (`ImageBitMapNative`, 전역) 에서 빈 칸을 찾는다.
   - 칸 하나 = 64KB. dll1·dll2 는 `SizeOfImage 0x8c000` → **9칸**.
2. `MiObtainRelocationBits +0x86 RtlFindClearBitsAndSetEx` 가 **찾기와 켜기를 한 번에** 한다. 자물쇠 (`RelocateBitmapsLock`) 를 잡은 채로.
   - 켜진 칸은 다른 파일이 못 받는다 → **dll1·dll2 는 서로 다른 구획.**
3. 기준 주소 = `0x7fffffff0000 − ((시작 비트 + 길이) << 16)`. 꼭대기에서 아래로 내려오며 정한다.
4. 고른 주소는 `MiRelocateImage +0x1b1269` 에서 섹션의 기준 주소 칸 `_SEGMENT.BasedAddress` 에 적힌다.

- 비유 (run72 의 구획 배정표 이어서): 관리사무소 직원이 배정표에서 빈 구획 9칸을 찾으면, **찾자마자 그 자리에 세트 이름을 적는다.** 자물쇠를 걸고 하니, 다른 직원이 같은 구획을 동시에 적을 틈이 없다.
- 그러니 두 설계도가 둘 다 "`0x180000000` 에 놓아 주세요" 라고 써 있어도 **상관없다.** 관리사무소는 그 글씨를 안 보고 배정표대로 정한다.

#### 왜 이게 "맵 결과 0" 으로 이어지나 — 단계별로

1. 근거 `[정적]` (run72): 창구가 `0x40000003` 을 주는 건 "놓인 번지 ≠ `_SEGMENT.BasedAddress`" 일 때뿐.
2. 근거 `[정적]` (이번): dll1·dll2 의 `_SEGMENT.BasedAddress` 는 서로 다른 구획.
3. 그래서: 그 구획이 test.exe 땅에서 **비어 있으면** 거기 그대로 놓인다 → 놓인 번지 = 기준 주소 → 대답 `0` → 재시도 길 안 탐.
4. 아직 확인 아님: 그 구획이 test.exe 안에서 정말 비었는지는 실행 때 값이다 (Q104).

#### 사진과 맞춰 보면

- `[관찰]` run44 P1b: dll1 `0x7ffae12e0000`, dll2 `0x7ffadb950000`.
- 둘 다 `0x180000000` 이 아니고, 서로 다르고, `0x7fffffff0000` 아래 64KB 로 떨어지는 값 → 이번 결론과 어긋나지 않는다 `[추측: 주소 모양으로만]`. 정확한 칸 번호는 모른다.

#### 조심할 것 하나 — 같은 부팅에서 두 번째 실행

- 같은 파일의 서랍 (컨트롤 영역) 이 아직 살아 있으면 `MiCreateNewSection` 을 안 타고 `MiRelocateImageAgain` 으로 간다.
- `+0x87 JNZ` 로 **먼저 고른 구획을 그대로** 쓴다 `[정적: 흐름으로 읽음]`. 다시 고를 때도 비트 0 은 늘 0 이다 `[정적]`.
- 그래서 결론은 같지만, VM 에서 `MiSelectImageBase` 멈춤점이 **안 설 수 있다** (run31 해설의 "서랍이 빈 날" 이야기와 같다).

### 2.4 run72 정정 — 한 줄

- run72 의 "헤더 그대로 갈래면 **둘째 DLL 은 반드시 `0x40000003`**" 은 틀린 셈은 아니지만, **dll1·dll2 에는 해당 없는 갈래** 다. 그 갈래는 드라이버 올리는 길 (`MmLoadSystemImageEx`) 뿐이다 `[정적]`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 2.1 커널 | — | 새 줄 둘 `[정적]`: 여섯째 인자 = 패킷 플래그 `(>> 27) & 2` (비트 0 늘 0) · 조각 `+0x1b1269` 가 `_SEGMENT.BasedAddress` 를 씀 |
| 2.6 run72 줄 "그 비트를 주는 `MiCreateNewSection` 줄" | `[공백]` | (해결됨) `[정적]` 늘 0 |
| 2.6 "헤더 그대로 → 둘째 DLL 반드시 `0x40000003`" | 갈래 하나로 적힘 | 줄 그음 → "이 두 DLL 에 해당 없음" |
| 2.6 새 run73 줄 | — | dll1·dll2 는 비트맵 갈래 → 비어 있으면 `0` `[정적]` + 같은 부팅 재사용 갈래 + `MmRegistryState+0x20` 갈래 `[공백]` |
| 4장 동적 1 기대값 "dll1·dll2 `0`" | `[추측]` | 근거 `[정적]` (실제 값은 여전히 동적) |

- 짐 지도 `baggage.md`: **새 짐 칸 없음.** 3.22 `BasedAddress` 줄 경로 표기만 고침 (`MiRelocateImage +0x1b1269` = `nt+0xb246ad`), 7.31 정적 ① 닫힘, 새 7.32.

### 큰 그림에서

- run72 가 남긴 불확실함은 "dll1·dll2 를 놓을 때 창구 대답" 하나였다. 이번에 그 **기대값의 근거가 코드로 섰다.**
- PE_emulator 쪽: dll1·dll2 섹션에 **서로 다른 기준 주소** 를 주고, 거기 그대로 놓고, 대답 `0` 을 주면 커널과 같은 길이다. 재시도 길은 지도에서 "안 타는 갈래" 로 남는다.
- 남은 건 "정말 비어 있었나" 하나뿐이고, 이건 VM 에서 한 번 보면 된다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| dll1·dll2 맵 결과 실제 값 (기대 `0`) | 구획이 test.exe 안에서 비었나는 실행 때 값 | **Q104** 동적 1 (`ntdll!LdrpMinimalMapModule +0x179` `r eax`, run72 동적 1 과 같은 멈춤점) |
| 커널이 고른 기준 주소·시작 비트·칸 수 | 실행 때 값 | **Q104** 동적 2·3 (선택: `nt!MiSelectImageBase +0x215` `rdi`, `+0x1d4`·`+0x1d8`, `_SEGMENT +0x20`). 같은 부팅 재실행이면 안 설 수 있음 |
| `MmRegistryState+0x20` 이 0 인가 (아니면 `MiImageCanUseHighOverflowArea` 갈래) | 실행 때 값 `[공백]` | **Q104** 동적 4 (`dq nt+0xfc5210 L1`) |
| 패킷 비트 5 (`MiRelocateImage` 넷째 인자) 를 `NtCreateSection → MiCreateSectionCommon → MiCreateSection` 중간에서 더하는 줄이 있나 | 그 중간 길은 줄로 안 봤다 `[공백]` | **Q140 (a)** 정적 |
| 패킷 비트 28 (여섯째 인자의 `2`) 을 켜는 `0x40` 을 누가 주나 | 안 따라감 `[공백]`. 비트 0 결론과는 무관 | **Q140 (b)** 정적 |
| `OriginalBase` = `DllBase` 인 이유 (메모리 헤더 `ImageBase` 를 누가 고쳤나) | `MiRelocateImage` 안 헤더 쪽 쓰기는 `TransferAddress` 더하기뿐이었다 `[공백]` | **Q140 (c)** 정적 (`MiWalkEntireImage`·`MiScanRelocationPage` / ntdll `LdrpRelocateImage`) |
| 구획 배정표 (`ImageBitMapNative`) 에서 dll1·dll2 몫 칸 (각 9칸) 켜기를 짐으로 셀지 | 커널 전역이라 지금은 baggage 7.32 에만 적음 | **Q140 (d)** — **사용자 결정** |
| `EPROCESS.Flags3` 비트 13 (`AllImagesAtBasePristineBase`) 의 뜻 | run72 에서 이어짐, 이번 범위 밖 | **Q139 (b)** 정적 |

---

## 5. 더 읽기

- run 문서: [`run73-image-base-select.md`](run73-image-base-select.md) — 호출 지점 표, ESI 사슬 표, 호출 관계 그림
- 결과서: [`_workspace/sast/68_image-base-select_result.md`](../../_workspace/sast/68_image-base-select_result.md) — 1번 착지점 표 (ESI 가 어디서 오나), 4번 비트맵 갈래
- 요청서: [`_workspace/sast/68_image-base-select.md`](../../_workspace/sast/68_image-base-select.md)
- flow.md: [2.1](flow.md) run73 줄 둘 · [2.6](flow.md) 맨 위 run72·run73 줄 · 4장 2.6 (짐 지도) 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.22 · 7.31 · 7.32
- 앞 이야기: [run72 해설 2.10](run72-pool-work-post-해설.md) (같은 자리를 원하는 두 DLL, 구획 배정표) · [run31 해설](../02_sprint/run31-image-section-해설.md) (세트·서랍) · [run38 해설](../02_sprint/run38-map-module-해설.md) (창구 대답)
