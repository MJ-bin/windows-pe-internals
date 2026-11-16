# run120 eprocess-locks — 해설

> 이 글은 [run120 문서](run120-eprocess-locks.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q99**: `_EPROCESS` 작은 정적 묶음 (run51 이 넘김) — "범위 밖 길에서만 씀" 의 두 락이 초기화 길에서 잡혔다 놓이나 · `DeviceMap` 을 처음 채우는 ntdll 쪽 호출자 · `ModifiedPageCount` 가 오르는 페이지 목록 (`_workspace/plan.md`, 이번에 **끝**)
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다. **짐 지도 바퀴**다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q98** (run120 몫 D1 ~ D3. D1 은 예전 F13, D3 은 예전 F5 와 같은 질문)
    - 정적 → 새 **Q210 ① ~ ③**
- 근거:
  - run 문서 [`run120-eprocess-locks.md`](run120-eprocess-locks.md) (가장 중요)
  - 요청서 [`_workspace/sast/115_eprocess-locks.md`](../../_workspace/sast/115_eprocess-locks.md) · 결과서 [`115_eprocess-locks_result.md`](../../_workspace/sast/115_eprocess-locks_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 그림 메모 · 2.1 · 2.4 · 4장 · 5장. **그림은 안 바뀜** (3절)
  - 짐 지도 [`baggage.md`](baggage.md): 2장 · 3.11 · 5장 ② · 6장 · 7장 (새 7.51)
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 값 대조는 결과서가 run44 때 찍어 둔 사진 로그의 `_EPROCESS` 줄을 다시 짚은 것뿐이다. 그래서 이 글의 "사진" 은 전부 run44 사진이다.
  - ntdll 은 Ghidra 프로젝트를 **안 열었다** (그때 sast/113 이 ntdll 을 돌리는 중). 이미 있던 `.asm` · `_calls.tsv` 와 파일 바이트만 읽었다.
- **run 문서가 결과서와 다르게 적은 곳이 있다** (2.11). 이 글은 **run 문서**를 따른다.
  - 두 락의 가름 이름: 결과서 "썼다 되돌림" → run 문서 **"잡고 푸는 잠금 칸"** (run116 과 같은 이름)
- 이번에 [run50 해설](run50-eprocess-create-해설.md) · [run51 해설](run51-eprocess-later-해설.md) 맨 위에 "고쳐 읽기 (run120)" 한 줄씩을 붙였다 (2.7 · 2.8 · 2.9).

---

### 이미 푼 말 (링크만)

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소 **신청 창구**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실** → [run65 해설 "비유"](run65-ldr-init-bodies-해설.md)
- `_EPROCESS` = 관리사무소의 **가게 장부** → [run10 해설 껍데기와 장부](../02_sprint/run10-allocate-process-해설.md#껍데기와-장부)
- 락 = **"작성 중" 자물쇠**, 가게 장부 자물쇠 (`ProcessLock`) → [run79 해설](run79-thread-exit-해설.md) 2.7 · [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)
- 가게 장부의 **직원 명부** (`ThreadListHead +0x370`), `PspInsertThread` 가 명부 꼬리에 다는 줄 `+0x239 ~ +0x243` → [run79 해설 2.12](run79-thread-exit-해설.md)
- 잠금 칸 = **열쇠**, **잡고 풂** (잠갔다 풀어서 처음 값으로 돌아오는 칸은 3장에 센다) → [run116 해설 2.5](run116-ntdll-globals-zero-해설.md)
- 사진 지점 K1 · K2 · K3 · P0 · P1 · P1a · P1b · P2, "처음 값을 갖는 구간" (`끝까지 0` 등) → [run44 해설 1절 · 2.10](run44-baggage-snap-해설.md)
- 짐 지도의 "0" 가름 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀) → [run47 해설 2.4](run47-section-chain-해설.md) · "범위 밖 길에서만 씀 `[추측]`" → [run50 해설 2.4](run50-eprocess-create-해설.md) · 오프셋 숫자 훑기의 **오탐** → [run50 해설 2.5](run50-eprocess-create-해설.md)
- 정보 클래스 (`0x35` 등), 게으른 만들기, `DeviceMap` 의 커널 쪽 (`ObpLookupObjectName` → `ObpSetCurrentProcessDeviceMap +0xf8`), `InvertedFunctionTable` (= **동적 함수표**) → [run51 해설 2.2 · 2.8](run51-eprocess-later-해설.md)
- ntdll 이 클래스 `0x35` 길을 탄다 (노드 비트 21 `[관찰]`, "세 번" `[추측: 개수]`), 동적 함수 표 → [run63 해설 2.6 · 2.9](run63-cfg-func-table-해설.md)
- 현재 디렉터리 = 지도 위 **"현재 위치" 핀**, `RtlpInitCurrentDir` 순서, 마스크 `LdrpIllegalCWDDevices` 와 안 불리는 `LdrpCheckAppDirType` → [run65 해설 2.6 · 2.7](run65-ldr-init-bodies-해설.md)
- 객체 이름 공간 = 관리사무소 안 **동네**, 심볼릭 링크 = **안내판** → [run119 해설 "한 줄로 먼저"](run119-systemroot-link-해설.md)
- 페이지 오류 입구 = **사고 접수 창구** → [run81 해설 2.4](run81-exception-kernel-해설.md)

### 이번 글의 새 말 (본문에서 처음 나올 때 한 줄씩 푼다)

- **푸시락 (push lock)** · **펼친 꼴 (inline)**: 2.2
- **`\??\`** (짧게): 2.8
- **변경 목록 (`ModifiedPageList`)**: 2.9

### 비유 (그대로 이어 쓴다)

- 가게 장부 (`_EPROCESS`) 에는 페이지마다 **작은 자물쇠** 가 달려 있다. 이번에 본 것은 그중 둘이다.
  - **직원 명부 자물쇠** (`ThreadListLock`) — run79 해설의 "직원 명부" 옆에 달린 것
  - **동적 함수표 자물쇠** (`InvertedFunctionTableLock`) — run51 해설의 "동적 함수표" 옆에 달린 것
- run 문서는 이 자물쇠를 화장실 문의 **"사용 중" 팻말** 로 그렸다. 이 글도 그 그림을 같이 쓴다.
  - 자물쇠 = 팻말이 달린 문. 팻말 "비어 있음" = 0, "사용 중" = 1.
- run116 해설의 **열쇠** (ntdll 칠판 옆 열쇠 걸이) 와 같은 종류의 이야기다. 이번 것은 관리사무소 쪽 자물쇠라는 점만 다르다.
- 현재 디렉터리는 run65 해설 그대로 **"현재 위치" 핀** 이다.

### 한 줄로 먼저: 무엇을 얻었나

| 요청서 질문 | 한 줄 답 (run 문서 그대로) | 이 글 |
|---|---|---|
| 1 두 락이 초기화 길에서 잡혔다 놓이나 | **예.** 둘 다 `ExAcquirePushLock*` 를 부르지 않고 **펼친 꼴** 로 잡고 푼다. 끝 값 0. `InvertedFunctionTableLock` (`+0x5d0`) = ② `RtlInsertDynamicUserFunctionTable`, `ThreadListLock` (`+0x5e0`) = ① `PspInsertThread`. 사진 여덟 장 0 `[관찰]` 과 맞다 | 2.2 ~ 2.7 |
| 2 `DeviceMap` 을 처음 채우는 ntdll 호출자 | **`LdrpInitializeProcess +0x1517 → RtlpInitCurrentDir +0x32 → RtlpCreateNewDirectoryReference +0xbb NtOpenFile`** (이름 `\??\C:\…`, 구간 `P1~P1a`). 정말 첫 번째인지는 동적 | 2.8 |
| 3 `ModifiedPageCount` 가 오르는 목록 | **변경 목록 (`ModifiedPageList`)**. 그리고 새로 찾은 **`MiUserFault +0x116` 0 으로 되돌림** (속도 조절, test.exe 는 안 탐) | 2.9 |

- 이전 run 이 틀린 곳: **run50** (두 락을 "범위 밖 `[추측]`" 으로 셈) · **run51** (`DeviceMap` ntdll 호출자 `[공백]` 닫힘, `ModifiedPageCount` 목록 `[추측]` → `[정적]`).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 바퀴는 길 위의 한 점이 아니다. 가게 장부의 **칸 넷** 을 쓰는 자리가 지도 여기저기에 흩어져 있다.

```
cmd.exe 의 부탁 (CreateProcess)
 │
 ├─ 단계 ① 커널 ── 부모 cmd 스레드의 NtCreateUserProcess 안
 │  ●K1 … ●K2
 │    PspInsertThread   직원 명부 달기       ← ① ThreadListLock 잡고 풂 (스레드마다)
 │  ●K3 … ●P0 PspUserThreadStartup         ← 여기부터 test.exe 첫 스레드
 ├─ 단계 ② ntdll
 │  ●P1 LdrInitializeThunk
 │    … RtlAddGrowableFunctionTable → NtSetInformationProcess(0x35)
 │         → nt RtlInsertDynamicUserFunctionTable   ← ② InvertedFunctionTableLock 잡고 풂
 │    LdrpInitializeProcess +0x1517 현재 디렉터리 → NtOpenFile (\??\…)
 │         → nt … EPROCESS.DeviceMap 처음 채움     ← ② DeviceMap 의 ntdll 호출자
 │  ●P1a … ●P1b … ●P2 NtContinue
 └─ (CRT) → main

(지도 밖) 메모리 관리자: 변경 목록에 쪽을 넣을 때 ModifiedPageCount +1   ← P0~P1, P1a~P1b
```

- 새 상자는 없다. 모두 **이미 그림에 있는 상자 안쪽** 이다 (`nt!PspInsertThread` · ntdll `NtSetInformationProcess(0x35)` 길 · `LdrpInitializeProcess` 뒤1 `RtlpInitCurrentDir`).
- `MiInsertPageInList` · `MiUserFault` 는 지도 밖 (페이지 목록 · 폴트 길) 이다 (flow.md 그림 메모).

### 무엇이 궁금했나

run51 이 넘긴 세 질문이다 (요청서).

1. run50 이 "범위 밖 길에서만 씀 `[추측]`" 으로 둔 **두 락** 이 사실은 초기화 길에서 잡혔다 놓이나? 놓으면 0 으로 돌아오나?
2. `DeviceMap` 을 처음 채우는 **ntdll 쪽** 호출자는 누구인가? (커널 쪽은 run51 이 이미 찾음)
3. `ModifiedPageCount` 는 **어느 페이지 목록** 에 넣을 때 오르나? (run51 은 "변경 목록?" 이라고 짐작만)

### 바퀴 전에 알던 것

- `[추측]` run50: 두 락은 "범위 밖 길에서만 씀". 그런데 run50 의 표에 이미 `PspInsertThread` (락은 잡았다 놓음) 가 적혀 있었다. **잡는 줄을 보고도 가름은 범위 밖** 이었다 (run 문서 "정정").
- `[정적]` run79: `PspInsertThread +0x6ac LEA R12,[R15+0x5e0]` 로 `ThreadListLock` 주소를 집고, 명부 꼬리 달기 `+0x239 ~ +0x243`.
- `[정적]` run51: `InvertedFunctionTable` (`+0x5c8`) 은 클래스 `0x35` → `RtlInsertDynamicUserFunctionTable +0x20c` 가 꽂는다. 락은 `+0x5d0`.
- `[정적]` run51: `DeviceMap` 의 커널 쪽 — `ObpLookupObjectName +0x143d` 이름 앞 `\??` 대조 → `ObpSetCurrentProcessDeviceMap +0xf8`. ntdll 쪽 호출자는 `[공백]`.
- `[정적]` run51: `ModifiedPageCount` 를 올리는 줄 `MiInsertPageInList +0x1fc` · `MiInsertPagesInList +0x64a` (`.asm` 없이 `objdump` 로 본 것). 어느 목록인지는 `[추측]`.
- `[관찰]` run63: test.exe 의 ntdll 노드 비트 21 — 클래스 `0x35` 길을 탔다. 횟수는 "세 번" `[추측: 개수]`.
- `[정적]` run65: 현재 디렉터리 순서 (`+0x1517 RtlpInitCurrentDir` …), 마스크 `LdrpIllegalCWDDevices` 0 → `LdrpCheckAppDirType` 안 불림.
- `[관찰]` run44: 사진 8장.

---

## 2. 찾은 것

### 2.1 한눈에: 세 묶음 (대표만)

```
[① 부모 cmd 스레드 — 스레드마다]
PspInsertThread
 ├─ +0x6ac  LEA  ThreadListLock (+0x5e0)
 ├─ +0x6bb  KeAbPreAcquire · +0x6c3 BTS.LOCK (0 → 1)
 ├─ +0x239 ~ +0x243  직원 명부 꼬리 달기 (run79)
 └─ +0x24e  XADD.LOCK −1 (1 → 0) → +0x261 KeAbPostRelease

[② test.exe 첫 스레드 — 클래스 0x35]
nt NtSetInformationProcess +0x566  RtlInsertDynamicUserFunctionTable
 ├─ +0xa7   LEA  InvertedFunctionTableLock (+0x5d0)
 ├─ +0xb4   KeAbPreAcquire · +0xbc BTS.LOCK (0 → 1)
 ├─ +0x20c  표 꽂기 (+0x5c8)
 └─ +0x125  XADD.LOCK −1 (1 → 0) → +0x141 KeAbPostRelease

[② test.exe 첫 스레드 — 뒤1, P1 ~ P1a]
LdrpInitializeProcess +0x1517  RtlpInitCurrentDir
 └─ +0x32  RtlpCreateNewDirectoryReference
            └─ +0xbb  NtOpenFile (\??\ + 현재 디렉터리)
                  └─ nt … ObpSetCurrentProcessDeviceMap +0xf8  EPROCESS.DeviceMap (run51)

[지도 밖 — 메모리 관리자]
MiInsertPageInList +0x1fc   (+1)       ┐ 변경 목록에 넣을 때만
MiInsertPagesInList +0x64a  (+개수)    ┘
MiUserFault +0x116          (0 으로)   16 을 넘을 때만 — test.exe 는 안 탐
```

- 전부 `[정적]` (`.asm` 줄). 자세한 call / ret 는 run 문서 "정적 — 호출 지점" 표.

### 2.2 새 말: 푸시락, 그리고 "펼친 꼴"

#### 새 말 두 개

- **새 말: 푸시락 (push lock)** = 커널이 짧게 잡는 가벼운 자물쇠. **비면 0, 잡으면 1.**
  - 칸 크기는 8바이트다 (`_EX_PUSH_LOCK`, run 문서 표).
  - 비유: 화장실 문 **"사용 중" 팻말**. 들어갈 때 "사용 중" 으로 돌리고, 나올 때 "비어 있음" 으로 돌린다.
- **새 말: 펼친 꼴 (inline)** = 함수를 부르지 않고, 그 함수 안의 몇 줄을 **그 자리에 바로 써 넣은 꼴**.
  - 그래서 함수 **이름으로 찾으면 놓친다.** `_calls.tsv` 에는 "부른 것" 만 남으니까.
  - 비유: "팻말 담당을 불러 와서 돌려 달라" 가 아니라, 들어가는 사람이 **자기 손으로** 팻말을 돌린다. 담당 명부 (부른 기록) 엔 아무것도 안 남는다.

#### 두 락 모두 이 꼴이다 `[정적]`

- 둘 다 `ExAcquirePushLockExclusiveEx` · `ExReleasePushLockEx` 를 **부르지 않는다** (결과서).
- 대신 같은 일을 함수 안에 펼쳐 놓았다. 순서는 늘 이렇다:
  1. `KeAbPreAcquire` 를 부른다 (잡기 앞)
  2. `BTS.LOCK [락],0` — 비트 0 을 켠다 (0 → 1). **잡음**
  3. … 자물쇠 안에서 할 일 …
  4. `XADD.LOCK [락],−1` — 1 을 뺀다 (1 → 0). **놓음**
  5. `KeAbPostRelease` 를 부른다 (놓기 뒤)
- `KeAbPreAcquire` · `KeAbPostRelease` 가 안에서 무엇을 하는지는 이번 근거에 없다. 이름만 본다.
- **느린 길** 은 남이 같은 자물쇠를 쥐고 있을 때만 탄다:
  - 잡을 때 이미 켜져 있으면 → `ExfAcquirePushLockExclusiveEx` (줄 서서 기다리기)
  - 놓을 때 대기자가 있으면 → `ExfTryToWakePushLock` (다음 사람 깨우기)
- 비유 (run 문서): 줄 선 사람이 없으면 팻말만 돌리고 끝 (빠른 길). 누가 줄 서 있을 때만 "다음 분" 을 부른다 (느린 길).

#### 왜 이게 이번 바퀴에서 중요했나

- 공용 부품 이름 (`ExAcquirePushLockExclusive` · `ExReleasePushLockExclusive`) 은 헤더에서 펼쳐진 것으로 본다 `[추측: 헤더 인라인]` (결과서).
- run 문서 도구 메모: 커널 푸시락은 이런 펼친 꼴이 많다. 다음에 락 칸을 찾을 땐 `ExAcquirePushLock*` 이름이 아니라 **`KeAbPreAcquire` · `KeAbPostRelease` 짝 + `LEA [reg+칸]`** 으로 찾는 편이 낫다. 짐 지도 6장 푸시락 줄에 적혔다.

### 2.3 한 줄씩 읽는 법

run 문서 "정적 — 호출 지점" 표에서 세 줄을 골라 풀어 읽는다.

#### 한 줄 읽기 ①: 직원 명부 자물쇠를 잡기 직전

> `nt!PspInsertThread` | `+0x6bb` | `+0x6c0` | `nt!KeAbPreAcquire` | 락 `[프로세스+0x5e0]` 잡기 앞 (`+0x6ac LEA R12,[R15+0x5e0]`) → `+0x6c3 BTS.LOCK [R12],0`

- **누가**: `PspInsertThread` — 새 직원을 가게에 다는 커널 함수 (부모 cmd 스레드가 돈다).
- **어디서**: `+0x6bb` 에서 부르고, 돌아오는 자리는 `+0x6c0`.
- **누구를**: `KeAbPreAcquire` (잡기 앞 짝).
- **무엇을 위해**:
  - 바로 앞 `+0x6ac` 에서 `R15` (가게 장부) `+0x5e0` 의 주소, 곧 **직원 명부 자물쇠** 를 `R12` 에 집어 둔다.
  - 바로 뒤 `+0x6c3 BTS.LOCK [R12],0` 으로 팻말을 "사용 중" 으로 돌린다.
- 그러니 이 한 줄은 "명부를 고치기 전에 자물쇠를 잠그는 준비" 다.

#### 한 줄 읽기 ②: `DeviceMap` 을 처음 채우게 하는 창구 방문

> `ntdll!RtlpCreateNewDirectoryReference` | **`+0xbb`** | **`+0xc0`** | **`ntdll!NtOpenFile`** | syscall `0x33`, 접근 `0x100020` (`+0x94`), 옵션 `0x21` · 공유 `3`, OA `RootDirectory` 0 · 속성 `0x40` · 이름 = 위 `\??\C:\…`

- **누가**: `RtlpCreateNewDirectoryReference` — 관리실이 "현재 위치" 핀 상자를 만드는 함수 (run65 해설 2.7).
- **어디서**: `+0xbb` (ret `+0xc0`).
- **누구를**: `NtOpenFile` — 신청 창구 `0x33` (파일 · 폴더 열기).
- **무엇을 위해**: 현재 디렉터리 폴더를 연다. 넘기는 이름이 **`\??\` 로 시작** 한다 (2.8).
- 커널은 이 이름을 풀다가 `\??` 를 보고, 가게 장부 `DeviceMap` 칸이 비었으면 **그 자리에서 채운다** (run51 의 게으른 만들기).
- 그러니 이 한 줄은 run51 이 `[공백]` 으로 둔 "ntdll 쪽 첫 호출자" 의 답이다 (정말 첫 번째인지는 동적 — 4절).

#### 한 줄 읽기 ③: 카운터를 0 으로 되돌리기 직전

> `nt!MiUserFault` | `+0x10d` | `+0x112` | `nt!MiCanFlushMakeProgress` | 참이면 `+0x116` 0 으로 되돌림

- **누가**: `MiUserFault` — 사용자 영역 페이지 오류를 다루는 함수 (결과서: caller `MmAccessFault`). [사고 접수 창구](run81-exception-kernel-해설.md) 뒤쪽이다.
- **어디서**: `+0x10d` (ret `+0x112`).
- **누구를**: `MiCanFlushMakeProgress`.
- **무엇을 위해**: 이 답이 "참" 이면 바로 뒤 `+0x116` 에서 `ModifiedPageCount` 를 0 으로 되돌리고, 스레드를 잠깐 늦춘다 (2.9).
- 이 함수가 안에서 무엇을 보는지는 이번에 안 풀었다. 이름만 본다.

### 2.4 동적 함수표 자물쇠 (`InvertedFunctionTableLock`, `+0x5d0`) — ② 에서 잡고 푼다

#### 어디서 잡나: `NtSetInformationProcess` 가 아니라 그 callee `[정적]`

- 요청서는 "클래스 `0x35` 길에서 잡나" 를 물었다. 답: 잡긴 잡는데, **한 칸 더 안쪽** 이다.
- `NtSetInformationProcess` 의 클래스 `0x35` 갈래 (run 문서 표 첫 줄):
  1. `+0x394 CMP EBX,0x35` — 클래스가 `0x35` 인가
  2. 핸들 −1 (`+0x52d`), 길이 16 (`+0x537`) 확인
  3. `+0x55c` 버퍼 `+8` 바이트가 0 이면 → **`+0x566` `RtlInsertDynamicUserFunctionTable`** (넣기)
  4. 0 이 아니면 → `+0x584` `RtlRemoveDynamicUserFunctionTable` (빼기 — test.exe 넣기 길엔 없음)
- `NtSetInformationProcess` 자신 안엔 락이 없다 (결과서). 자물쇠는 `RtlInsertDynamicUserFunctionTable` 안에 있다.

#### 안에서의 순서 `[정적]`

| 단계 | 줄 | 하는 일 |
|---|---|---|
| 가드 영역 들어가기 | `+0x9f` | `_KTHREAD +0x1e6` (`SpecialApcDisable`) −1 — 이것도 펼친 꼴 |
| 자물쇠 주소 집기 | `+0xa7 LEA RDI,[R15+0x5d0]` | 가게 장부 `+0x5d0` |
| 잡기 | `+0xb4 KeAbPreAcquire` · **`+0xbc BTS.LOCK`** | 0 → 1 (켜져 있었으면 `+0xcd` 느린 길) |
| 자물쇠 안 | `+0xe1` 표 읽기 · (`+0x1bf ExAllocatePool2`) · **`+0x20c` 표 꽂기 (`+0x5c8`)** · 항목 끼우기 | run51 의 동적 함수표 일 |
| 놓기 | **`+0x125 XADD.LOCK`** −1 · `+0x141 KeAbPostRelease` | 1 → 0 (대기자 있으면 `+0x302` 느린 길) |
| 가드 영역 나오기 | `+0x14f` | `+0x1e6` +1 (남은 APC 있으면 `+0x166 KiCheckForKernelApcDelivery`) |
| 옛 블록 버리기 | `+0x178 ExFreePoolWithTag` | **자물쇠를 푼 뒤** |

- 가드 영역은 이 표에선 이름 (`SpecialApcDisable` 칸 ±1) 만 본다. 2.5 의 `PspInsertThread` 엔 이게 없다 — 그 몫은 부른 쪽 `[추측]` (결과서).
- **자물쇠를 잡은 뒤의 길은 모두 `+0x115` 로 모여 `+0x125` 를 지난다** (run 문서). 그래서 잡기만 하고 안 놓는 길은 없다.
- 자물쇠 **앞** 에서 끝나는 길도 있다: `+0x19 JNZ` 정렬 틀림, `+0x60` 범위 오류 `0xc0000095`. 이 길은 잡지도 않는다.

#### test.exe 는 이 길을 타나: 탄다 `[정적 + 관찰]` — 단계별로

1. `[관찰]` 사진: 표 칸 `+0x5c8` 이 `P1` 에선 0, `P1a` 에선 `ffffb689'b064d820` (`ms/logs/04_baggage-read_P1a.txt:489`, 결과서).
2. `[정적]` 그 칸을 쓰는 줄은 `+0x20c` **하나뿐** 이다.
3. `[정적]` `+0x20c` 는 `+0xbc` (잡기) 와 `+0x125` (놓기) **사이** 에 있다.
4. 그래서 → `P1 ~ P1a` 사이에 누군가 이 자물쇠 안을 지나갔다. 곧 잡고 풀었다.
5. 아직 확인 아님: 몇 번이냐. run63 의 "ntdll · kernel32 · kernelbase 세 번 `[추측: 개수]`" 그대로다. 매번 0 → 1 → 0 쌍이다.

### 2.5 직원 명부 자물쇠 (`ThreadListLock`, `+0x5e0`) — ① 에서 잡고 푼다

#### 순서 `[정적]` (`PspInsertThread`, R15 = 가게 장부, R14 = 직원 인사 카드)

1. `+0x657 ~ +0x65a` — `ActiveThreads` (근무 중 인원) +1 을 먼저 한다.
2. 첫 직원인가 / 둘째인가 / 그 밖인가로 **갈래 셋** 이 생기는데, 셋 다 `+0x6ac` 로 모인다 (결과서).
3. `+0x6ac LEA R12,[R15+0x5e0]` — 직원 명부 자물쇠 주소 (2.3 한 줄 읽기 ①)
4. `+0x6bb KeAbPreAcquire` · **`+0x6c3 BTS.LOCK`** — 0 → 1 (남이 쥐었으면 `+0x6d5` 느린 길)
5. `+0x6e3 ~ +0x6f8` — 명부가 안 깨졌나 검사 (안 맞으면 `INT 0x29`, 결과서)
6. **`+0x239 ~ +0x243`** — 명부 (`+0x370`) 꼬리에 새 직원 달기 (run79 해설 2.12 와 같은 줄)
7. **`+0x24e XADD.LOCK`** −1 — 1 → 0 (대기자 있으면 `+0x8b5 ExfTryToWakePushLock`)
8. `+0x261 KeAbPostRelease`

- 오프셋 숫자가 `+0x6c3` → `+0x239` 로 **작아지는데** 실행은 그 순서다. `+0x6f8 JZ` 가 `+0x239` 로 뛰어가기 때문이다 (오프셋 순서 ≠ 실행 순서, [run12 해설 2.3](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다) 과 같은 이야기).
- **직원마다 같은 길** 이다. 갈래 셋이 다 한 곳으로 모이니까 첫 직원도, 둘째 직원도 이 자물쇠를 잡고 푼다.

#### 비유로

- 새 직원 이름을 명부에 적으려면, 명부 페이지의 팻말을 "사용 중" 으로 돌리고 → 적고 → "비어 있음" 으로 돌린다.
- 같은 때 다른 직원이 명부를 고치려 하면 (느린 길) 줄 서서 기다린다.
- test.exe 초기화 때 그런 줄서기가 있었는지는 아무도 본 적이 없다. 다만 다 끝난 뒤 사진은 늘 0 이다 (2.6).

### 2.6 왜 끝 값이 0 인가, 그리고 사진과 맞나

#### 코드로 `[정적]`

- 잡기 `BTS` 는 **비트 0 하나만** 켠다 (0 → 1).
- 놓기는 `XADD −1` 이다. 1 에서 1 을 빼면 0.
- 대기자가 없으면 그걸로 끝. 칸은 처음 값 0 으로 돌아온다.

#### 사진으로 `[관찰]`

- 두 칸 모두 **여덟 장 다 0** 이다 (run44 사진, 결과서가 로그 줄을 짚음).
- 특히 직원 명부 쪽엔 **"그 사이에 자물쇠를 썼다" 는 흔적** 이 같이 찍혀 있다:
  - 같은 사이에 명부 `+0x370` 의 `Blink` (꼬리) 가 첫 직원 → **둘째 직원** 으로 바뀌었다 (`K3` 454줄 → `P1b` 452줄).
  - 명부에 직원이 새로 달렸다 → 명부에 다는 줄은 2.5 의 6번 (`+0x239 ~ +0x243`, 자물쇠 안) 이다 (run79 `[정적]`) → 그러니 그 사이 자물쇠를 잡고 풀었다.
  - 그런데도 자물쇠 칸은 두 사진 모두 0 이다.
- 동적 함수표 쪽도 같다. 표 칸 `+0x5c8` 은 `P1a` 부터 0 이 아닌데 (2.4), 자물쇠 칸은 계속 0 이다.

#### 그래서 `[정적 + 관찰]`

- 비유 (run 문서): 나중에 와서 보면 팻말은 늘 "비어 있음" 이다. 사진 여덟 장이 모두 0 인 까닭이다.
- 아직 확인 아님: **잡혀 있는 동안 정말 1 인가** 는 아무도 본 적이 없다. 코드와 사진이 이미 맞아서 급하지 않다 (동적 D2, 선택).

### 2.7 셈: "범위 밖" 에서 "잡고 푸는 잠금 칸" 으로 (run50 정정)

#### 셈 기준 (run 문서 · 오케스트레이터)

- run116 의 규칙을 그대로 쓴다: **잡고 푸는 줄이 초기화 길에 있으면 3장 (아는 칸).** ([run116 해설 2.5](run116-ntdll-globals-zero-해설.md))
- 두 락 모두 그 줄이 초기화 길에 있다 (2.4 · 2.5). 그래서 "범위 밖 길에서만 씀 `[추측]`" 에서 빼고 짐 지도 3.11 에 새 줄 둘로 옮겼다.
- 가름 이름: 결과서는 "썼다 되돌림" 이라고 했지만 run 문서는 run116 이 쓴 **"잡고 푸는 잠금 칸"** 으로 맞췄다. 뜻은 같다 (2.11).

#### run50 이 왜 범위 밖으로 셌나 (run 문서 "정정")

- run50 의 "범위 밖 길" 표에 이미 `PspInsertThread` (락은 잡았다 놓음) 가 적혀 있었다 ([run50 해설 2.4](run50-eprocess-create-해설.md) 의 표 첫 줄).
- 그런데 run50 은 커널 전체를 **오프셋 숫자로 훑어** 칸을 갈랐다 ([run50 해설 2.5](run50-eprocess-create-해설.md) — 오탐 가능, 경계가 약함).
- 이번에 `.asm` 로 한 줄씩 보니 두 칸은 초기화 길 안에서 잡고 푼다. 그래서 바뀌었다.
- 비유: run50 해설은 "폐업일" 칸처럼 **개업 때 아무도 안 만지는 칸** 이라고 봤다. 실제로는 개업 날에도 직원이 들어와서 **잠깐 잠갔다가 풀고 나간 칸** 이었다. 나중에 보면 둘 다 빈칸이라 사진만으론 구별이 안 된다.

#### `_EPROCESS` 숫자 (run 문서 셈 표)

| | 전 | 뒤 |
|---|---|---|
| 3.11 아는 칸 (정확 · 짐작) | 183 (179 · 4) | **185 (181 · 4)** |
| 3.11 범위 밖 길에서만 씀 `[추측]` | 53 | **51** |
| 3.11 줄 | 109 | **111** |
| `끝까지 0` 160 가름 | 0 을 씀 50 · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 52 | 0 을 씀 50 · **잡고 푸는 잠금 칸 2** · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 **50** |

- 두 칸은 원래 사진 `끝까지 0` 이라 2장 "0 그대로 167" 은 안 바뀐다 (run 문서).
- 짐 지도 전체 숫자는 3절.

### 2.8 `DeviceMap` 을 처음 채우는 ntdll 호출자 — 뒤1 현재 디렉터리

#### 먼저 새 말 하나

- **새 말: `\??\`** = `C:` 같은 드라이브 이름을 진짜 장치 이름으로 이어 주는 **안내판들이 모인 자리** 로 일반적으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 관리사무소 동네 ([run119 해설](run119-systemroot-link-해설.md)) 입구의 **"이 가게 손님용 안내판 묶음"**.
  - `DeviceMap` 은 "이 가게는 어느 안내판 묶음을 쓰나" 를 적어 두는 장부 칸이다 (run51: 토큰의 장치 맵 또는 사일로 기본값).
  - 근거에 있는 것만 말하면: 이름 앞이 `\??` 이면 커널이 `DeviceMap` 칸을 보고, 비었으면 그 자리에서 채운다 `[정적]` (run51).

#### 사슬 `[정적]`

```
LdrpInitializeProcess +0x1517 (ret +0x151c)  RtlpInitCurrentDir
 └─ +0x32 (ret +0x37)  RtlpCreateNewDirectoryReference
      ├─ +0x62  RtlpDosPathNameToRelativeNtPathName   "\??\" + 현재 디렉터리
      └─ +0xbb (ret +0xc0)  NtOpenFile (syscall 0x33)
           └─ nt … ObpLookupObjectName +0x145e ObpReferenceCurrentDeviceMap
                  └─ +0x286 ObpSetCurrentProcessDeviceMap +0xf8   EPROCESS.DeviceMap   (run51)
```

- run65 해설 2.7 의 "현재 위치 핀 꽂기" 순서 바로 그것이다. 핀 상자를 만들려고 폴더를 여는 그 창구 방문이, 덤으로 가게 장부의 `DeviceMap` 칸을 처음 채우게 한다.

#### 이름이 왜 `\??\` 로 시작하나 `[정적]`

- `RtlpDosPathNameToRelativeNtPathName +0x201` 이 `RtlpDosDevicesPrefix` (rva `0x173200`) 를 집어, 새 버퍼 맨 앞에 붙인다 (`+0x2a8` 복사).
- ntdll 파일 바이트로 읽으면 그 칸은 `{Length 8, MaxLength 10, Buffer → "\??\"}` 다. 옆 칸 `0x173210` 은 `\??\UNC\` (결과서).
- 그래서 `C:\…` 현재 디렉터리 → `\??\C:\…` 가 되어 창구에 간다.

#### 정말 "처음" 인가 — 단계별로

1. `[정적]` 결과서가 `LdrpInitializeProcess` 의 `+0x1517` **앞** callee 를 `_calls.tsv` 로 깊이 5 까지 훑었다.
2. `[정적]` 그 안에서 이름을 넘기는 창구 방문은 이것뿐이다:
   - `NtOpenKey` (`\Registry\…`)
   - `NtOpenDirectoryObject` `\KnownDlls` (`+0xdb8`)
   - `NtOpenSymbolicLinkObject` `KnownDllPath` (`+0xe47`, 뿌리 = 위 핸들)
   - 이름 없는 `NtCreateEvent` 들
   - 어느 것도 `\??\` 로 시작하지 않는다.
3. `[정적]` `\??\` 이름 후보는 **조건부 셋** 뿐이고, test.exe 는 셋 다 안 탄다:
   - `+0x117e LdrpCheckAppDirType` (exe 경로 `NtOpenFile`) — 마스크 `LdrpIllegalCWDDevices` 가 0 이면 건너뜀. test.exe 는 0 (run65 해설 2.6).
     - **마스크가 켜진 프로세스라면 이 줄이 먼저다** (run 문서).
   - `+0x531 LdrpInitializeExecutionOptions → … → AvrfMiniLoadDll` — App Verifier 가 켜진 프로세스만 (run105).
   - `+0x1302 LdrpDrainWorkQueue` 아래 DLL 파일 길 — 이때 일감 큐는 비어 있을 것 `[정적 추론]`. DLL 을 파일로 여는 길 (run21 · run39) 은 `P1a~P1b` 라 더 늦다.
4. `[정적 + 관찰]` 사진 구간도 맞다: `P1a` 는 `LdrpInitializeProcess +0x199d` 라 `+0x1517` 은 그 앞 → `P1~P1a`. run44 사진에서 `DeviceMap` 이 처음 값을 갖는 구간과 같다.
5. 그래서 → test.exe 에서 `\??\` 이름을 넘기는 첫 창구 방문은 `+0xbb NtOpenFile` 로 보인다.
6. 아직 확인 아님: 실제로 그 호출이 `DeviceMap` 을 채운 순간의 콜스택은 아무도 안 봤다 (동적 D3 = 예전 F5).

### 2.9 `ModifiedPageCount` (`+0x550`) — 변경 목록 카운터, 그리고 0 으로 되돌리는 줄

#### 먼저 새 말 하나

- **새 말: 변경 목록 (`ModifiedPageList`)** = 프로세스가 손에서 내려놓은 메모리 쪽 중 **내용이 고쳐져서 디스크에 아직 안 적은 것** 들이 서는 줄로 일반적으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 근거에 있는 것: 이름이 `_MMLISTS` 의 3 번 `ModifiedPageList` 라는 것, 머리가 시스템 파티션 안 칸이라는 것 `[정적]` (아래).
  - 비유: 관리사무소의 **"옮겨 적기 전" 바구니**. 고쳐 쓴 종이를 여기 모았다가 나중에 서고에 옮겨 적는다.

#### 쓰는 줄은 셋 `[정적]` (nt 전체에서 `+0x550]` 에 쓰는 명령 39개 중 `_EPROCESS` 것)

| 줄 | 하는 일 |
|---|---|
| `MiInsertPageInList +0x1fc` (`INC.LOCK`) | 변경 목록에 **한 쪽** 넣을 때 +1 |
| `MiInsertPagesInList +0x64a` (`ADD.LOCK`) | 변경 목록에 **여러 쪽** 한꺼번에 넣을 때 +개수 |
| **`MiUserFault +0x116`** (`MOV … ,0`) | **0 으로 되돌림** (새로 찾음) |

- 나머지 36개는 **다른 구조체** 다 (`_ETHREAD +0x550 TopLevelIrp`, 캐시 파티션, 저장소 관리자, 추적 칸 등 — 결과서).
- 어떻게 갈랐나: 베이스 레지스터가 어디서 왔나를 봤다. 셋은 `[스레드+0xb8]` 에서 오고, 36개는 아니다.
  - 이게 바로 [run50 해설 2.5](run50-eprocess-create-해설.md) 가 말한 오탐 줄이는 법 ("레지스터가 어디서 왔는지까지 같이 보기") 이다.

#### 어느 목록일 때 오르나 — 변경 목록 `[정적]`

- `MiInsertPageInList` 는 플래그로 목록을 고른다. 플래그 `0x8` → 파티션 `+0x4bc0` = `Vp.ModifiedPageListHead`.
- 그 머리의 이름표 (`ListName`) 가 3 (`ModifiedPageList`) 이면 변경 길 (`+0x167 CMP EDI,3`).
  - "변경-쓰지-않음" (4) 도 섹션이 "고쳐 쓰기 금지" 가 아니면 3 으로 바꿔 같은 길로 합류한다 (`+0x6d4`).
- 변경 길: `+0x185` 목록 합 +1 → `+0x1b9 MiDetermineModifiedPageListHead` (실제로 줄 세울 머리 고르기) → `+0x1d0 MiLockPageListAndLastPage` → **`+0x1fc` 카운터 +1**.
- `MiInsertPagesInList` 는 첫 쪽의 `Modified` 비트가 켜졌을 때만 변경 목록으로 간다 (`+0xff` · `+0x40b JNZ`) → **`+0x64a` 카운터 += 개수**.
- 이름 하나 주의: 이 빌드 PDB 엔 `MmModifiedPageListHead` 라는 전역 이름이 **없다** (`pdb_aliases.py --where`). 목록 머리는 `MiSystemPartition` (`_MI_PARTITION`) 안 칸이다.

#### 누구의 카운터인가 `[정적]`

- 페이지 **주인** 이 아니다.
- **지금 코드가 도는 스레드가 붙어 있는 프로세스** (`[GS:[0x188]+0xb8]`) 의 칸이다.
- 비유: 바구니에 종이를 넣은 **그 순간 일하던 직원의 가게** 장부에 "+1" 을 적는다. 종이 원래 주인 가게가 아니라.

#### 0 으로 되돌리는 줄 (새) — 단계별로 `[정적]`

`MiUserFault` 는 사용자 영역 페이지 오류마다 불린다 (결과서: caller `MmAccessFault`).

1. `+0x58` — 카운터를 읽는다.
2. `+0x5e CMP EAX,0x10` → `+0x61 JBE` — **16 이하면 아무것도 안 하고 끝.**
3. 16 을 넘으면 더 본다:
   - `+0x67` 시스템 프로세스가 아닌가 (`Flags3.SystemProcess` 꺼짐)
   - `+0xf6` 파티션 변경 목록 합이 기준 이상인가
   - `+0x10d MiCanFlushMakeProgress` 가 참인가 (2.3 한 줄 읽기 ③)
4. 다 맞으면 → **`+0x116` 카운터 = 0** → `+0x68f MiDelayFaultingThread` 로 그 스레드를 잠깐 늦춘다.

- 뜻 풀이: **"변경 목록으로 많이 보낸 프로세스의 폴트를 늦추는 속도 조절 카운터"**. 코드 모양은 `[정적]`, 이 이름 풀이는 `[추측]` (run 문서).
- 비유: 바구니에 종이를 너무 많이 (16장 넘게) 넣은 가게가, 마침 바구니도 꽉 찼을 때 또 사고 접수 창구에 오면, 관리사무소가 "잠깐만 기다려요" 하고 그 가게 장부의 숫자를 0 으로 지운다.

#### test.exe 에서는 `[정적 + 관찰]`

- 사진: `P0 0 → P1 1`, `P1a 1 → P1b 2` (결과서).
- 2 라서 위 2번 (`+0x61 JBE`) 에서 걸러진다 → **되돌림 줄은 안 탄다.**
- 누가 올렸나는 **정적으로 못 정한다**:
  - 후보 = 플래그 `0x8` 로 `MiInsertPageInList` 를 부르는 줄 15 + `MiInsertPagesInList` 를 부르는 쪽 6 (결과서 목록).
  - 지도 `NtCreateUserProcess` 안에서 닿는 길 (`MiHandleForkTransitionPte` · `MmUnmapViewInSystemCache`, 깊이 7) 도 있지만, 그건 부모 스레드 길 (`K1` 앞) 이고 `K1 = 0` 이라 원인이 아니다 `[추측]`.
  - → 동적 D1 (4절).

### 2.10 에뮬레이터 쪽에서 보면 (결과서가 본 줄)

`../PE_emulator` 코드는 flow-writer 가 안 읽었다. 결과서가 본 줄만 옮긴다. 고칠지는 사용자 결정이다.

- **두 락 — 맞음.** `ProcessObject::dynamic_function_tables` 는 락 값을 안 둔다 (`ext/syscall/syscall_layer_cpp/include/syscall_layer/processes.hpp:29~31` 주석이 `+0x5d0` 을 짚음). `ThreadListLock` 이름은 `ext/syscall` 에 없다. 끝 값 0 과 같다.
  - EPROCESS 날 바이트를 만들어 비교하는 쪽이 있다면 두 칸은 0 — 그쪽이 있는지는 **확인 필요**.
- **`DeviceMap` 호출 순서 — 맞음.** 진짜 ntdll 을 돌리면 같은 길이 저절로 간다.
  - 단, 개업 서류의 `CurrentDirectory.DosPath` 가 **절대 경로** 여야 `RtlpInitCurrentDir +0x32` 를 탄다 (run65 조건 그대로).
- **커널 `DeviceMap` · `ModifiedPageCount` — 확인 필요.** 에뮬레이터에 칸이 없다. 칸 값을 비교하는 곳이 없으면 무관하다.
  - `ModifiedPageCount` 의 `P1` 이후 1 ~ 2 는 진짜 VM 메모리 관리자 값이라 맞출 이유가 약하다 `[추측]`.

### 2.11 결과서와 다르게 적은 곳 (뜻은 같음)

| 결과서 | run 문서 (이 글이 따름) | 까닭 |
|---|---|---|
| 두 락 가름 = "썼다 되돌림 2" | **"잡고 푸는 잠금 칸 2"** | run116 (짐 지도 3.8) 이 쓴 이름과 맞춤. 7.1 의 run49 "썼다 되돌림 1" 과도 뜻은 같다 (run 문서) |
| 질문 2 의 `\??\` 이름 후보 = "조건부 **둘**" (그런데 아래에 셋을 듦) | **"조건부 셋"** | 결과서 글머리 숫자만 어긋남. 든 것은 셋 (`+0x117e` · `+0x531` · `+0x1302`) — 2.8 |
| 짐 지도 범위 밖 전체 "257 → 255" | **267 → 265** (짐 지도 2장) | 결과서가 예전 합을 기준으로 셈. 빠지는 칸 수 (−2) 는 같다 — 3절 |

- 표의 아래 두 줄은 이 글을 쓰며 본 차이다. 어느 문서도 고치지 않았다. 이 글은 run 문서 숫자를 썼다.

### 2.12 디버거가 붙어서 달라지는 점

- 이번엔 디버거를 안 붙였다 (정적만). 새 함정 없음 (run 문서).
- 짐 지도 7.51: 이 줄들에서 디버거 깃발을 보는 줄은 없다 `[추측: 없음]`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 절 | 전 | 후 |
|---|---|---|
| 기준 | | ntkrnlmp · ntdll pdb 줄에 run120, 사진 대조 메모 한 줄 (새 동적 실행 없음) |
| 그림 메모 | | run120 도 **그림을 안 고쳤다**. 모두 이미 있는 상자 안쪽, `MiInsertPageInList` · `MiUserFault` 는 지도 밖 |
| 2.1 `PspInsertThread` 3번 아래 | 명부 달기 (`+0x5e0` 푸시락 안, run79) | 한 줄 더 `[정적]`: 그 푸시락은 **펼친 꼴** (`+0x6c3 BTS` … `+0x24e XADD −1`), 끝 값 0 |
| 2.4 "ntdll 이 부르는 syscall 이 커널 프로세스 객체 칸을 처음 만드는 자리" 나무 | `?  \??\ 이름을 찾는 첫 syscall → … DeviceMap (ntdll 호출자 [공백])` | `+0x151c RtlpInitCurrentDir → +0x37 RtlpCreateNewDirectoryReference → +0xc0 NtOpenFile` `[정적]` (나무는 ret 자리로 적음) + 동적 함수표 자물쇠 한 줄 |
| 2.4 같은 절 아래 | `ModifiedPageCount` 목록 없음 | 변경 목록 · `MiUserFault +0x116` 되돌림 한 줄 |
| 2.4 run65 현재 디렉터리 절 | | `+0xc0 NtOpenFile` 이름이 `\??\` + 현재 디렉터리, `DeviceMap` 을 처음 채우는 첫 syscall 한 줄 |
| 4장 run51 "작은 정적" 줄 | 할 일 | **(해결됨, 정적 → run120)** · F5 · F13 줄에 보탬 · 새 줄 셋 (동적 D1 ~ D3 · 정적 ① ~ ③ · PE_emulator) |
| 5장 | | 근거 run 표 · 요청서 번호 표에 run120 |

- 태그 변화 셋:
  - 두 락: "범위 밖 길에서만 씀 `[추측]`" → **아는 칸 `[정적]`** (잡고 푸는 잠금 칸)
  - `DeviceMap` 의 ntdll 첫 호출자: `[공백]` → **`[정적]`** (탔는지는 동적 그대로)
  - `ModifiedPageCount` 목록: `[추측]` → **`[정적]`** (뜻 이름 "속도 조절" 만 `[추측]`)

### 짐 지도에서 바뀐 것

- **2장 숫자** (구조체 21개, 직접 칸 1,456 그대로):
  - 아는 칸 **938 → 940** (정확 907 → 909)
  - 범위 밖 길에서만 씀 `[추측]` **267 → 265**
  - 모름 24 · 아무도 안 씀 202 는 그대로
  - `_EPROCESS` "nt 가 씀" 183 → 185, 커널 14개 합 605 → 607
- 3.11: 새 줄 2 (`+0x5d0` · `+0x5e0`) · 고친 줄 4 (`DeviceMap` · `ThreadListHead` · `ModifiedPageCount` · "범위 밖 52 → 50" 줄) + `+0x5c8` 에 자물쇠 한 마디
- 6장 공용 부품에 **푸시락 한 줄** (펼친 꼴, 찾는 법)
- 5장 ② `DeviceMap` 문장, 7.1 셈 (381 → 383 · 466 → 464) · 7.2 · 7.4 · 7.14 F5 · F13 보탬 · 새 7.51

### 큰 그림에서

- 가게 장부 (`_EPROCESS`) 263칸 중 **모름은 여전히 1칸** (`SharedCommitCharge`) 이다. 이번 바퀴는 모름을 줄인 게 아니라 **"짐작으로 센 칸" 을 "줄로 확인한 칸" 으로** 바꿨다.
- 에뮬레이터 쪽 의미:
  - 두 자물쇠는 "0 으로 둬도 되는 칸" 이 **확인으로** 바뀌었다. 초기화 끝에 늘 0 이다.
  - `DeviceMap` 은 ntdll 을 그대로 돌리면 같은 순간 (`P1~P1a`, 현재 디렉터리 핀 꽂기) 에 창구 `0x33` 으로 온다. 에뮬레이터가 이 창구에서 `\??\` 이름을 어떻게 받는지 볼 자리가 정해졌다.
  - `ModifiedPageCount` 는 메모리 관리자 사정에 따라 오르는 숫자라, 초기화 짐이라기보다 **지나가며 남는 흔적** 에 가깝다 (`[추측]` — 결과서 "맞출 이유가 약하다").

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `ModifiedPageCount` 0 → 1 (`P0~P1`) · 1 → 2 (`P1a~P1b`) 를 **누가** 올렸나 (후보 15 + 6) | 지금 도는 스레드에 따라 갈리는 실행 때 일. 첫 스레드 시작 길은 지도 뿌리 밖이라 좁혀지지 않음 | 동적 **Q98 D1** (= 예전 F13): `nt!MiInsertPageInList+0x1fc` · `nt!MiInsertPagesInList+0x64a` 에서 `k` · 플래그 `EDX`. `nt!MiUserFault+0x116` 은 안 서야 정상 |
| `DeviceMap` 을 채운 순간의 콜스택이 정말 2.8 사슬인가, `LdrpIllegalCWDDevices` 가 정말 0 인가 | 정적으로는 "후보 중 test.exe 가 탈 수 있는 첫 번째" 까지만 | 동적 **Q98 D3** (= 예전 F5): `nt!ObpSetCurrentProcessDeviceMap+0xf8` 에서 `k` |
| (선택) 자물쇠가 잡혀 있는 동안 정말 1 인가 | 사진은 다 놓은 뒤만 찍힘. 코드와 사진이 이미 맞아 급하지 않음 | 동적 **Q98 D2** |
| D1 이 정한 호출자가 **어느 쪽을 왜** 변경 목록으로 보냈나 | D1 이 먼저 와야 함 | 정적 **Q210 ①** |
| `RtlRemoveDynamicUserFunctionTable` (빼기) 도 같은 `+0x5d0` 자물쇠를 쓰나 | 은퇴 뒤 길이라 범위 밖, `.asm` 없음 | 정적 **Q210 ②** (낮음) |
| 직원 명부 자물쇠를 **공유로** 잡는 쪽 (`PsGetNextProcessThread` · `PspThreadDelete +0x374` 등) | 은퇴 뒤 길. 초기화 안에선 필요 없음 | 정적 **Q210 ③** (낮음, 선택) |
| `KeAbPreAcquire` · `KeAbPostRelease` · `MiCanFlushMakeProgress` 안에서 무엇을 하나 | 이름만 봄 (공용 부품 · 범위 밖) | (정하지 않음) |
| `ModifiedPageCount` 의 뜻 이름 ("속도 조절 카운터") | 코드 모양에서 읽은 풀이일 뿐 `[추측]` | (정하지 않음) |
| 에뮬레이터에 EPROCESS 날 바이트를 비교하는 쪽이 있나 | 우리 일이 아니라 에뮬레이터 쪽 확인 | 사용자 결정 (확인 필요) |

---

## 5. 더 읽기

- run 문서: [`run120-eprocess-locks.md`](run120-eprocess-locks.md). 특히 "정적 — 호출 지점" 표 (call · ret 전부), "호출 관계" 트리, 1 · 2 · 3 절 표, "짐 지도 셈", "정정"
- 결과서: [`_workspace/sast/115_eprocess-locks_result.md`](../../_workspace/sast/115_eprocess-locks_result.md). 질문별 `.asm` 줄, `+0x550` 쓰기 39개 가름, 후보 호출자 목록 (15 + 6), "동적으로 확인할 것" D1 ~ D3
- flow.md: [`flow.md`](flow.md) 2.1 `PspInsertThread` 절 · 2.4 "ntdll 이 부르는 syscall 이 커널 프로세스 객체 칸을 처음 만드는 자리" · run65 현재 디렉터리 절 · 4장 run51 · run120 줄
- 짐 지도: [`baggage.md`](baggage.md) 2장 · 3.11 (`+0x318` · `+0x370` · `+0x550` · `+0x5c8` · `+0x5d0` · `+0x5e0` · 범위 밖 50 줄) · 6장 푸시락 · 7.2 · 7.4 · 7.14 · **7.51**
- 앞 바퀴 해설:
  - [run50 해설](run50-eprocess-create-해설.md) 2.4 · 2.5: 범위 밖 길 · 오탐. 이번에 맨 위 "고쳐 읽기" 를 붙임
  - [run51 해설](run51-eprocess-later-해설.md) 2.6 · 2.8: `ModifiedPageCount` · `DeviceMap` · 동적 함수표. 이번에 맨 위 "고쳐 읽기" 를 붙임
  - [run79 해설](run79-thread-exit-해설.md) 2.12: 직원 명부 두 개
  - [run116 해설](run116-ntdll-globals-zero-해설.md) 2.5: 잡고 풂 (잠금 칸 셈 규칙)
  - [run65 해설](run65-ldr-init-bodies-해설.md) 2.6 · 2.7: 마스크 · 현재 위치 핀
  - [run63 해설](run63-cfg-func-table-해설.md) 2.6 · 2.9: 클래스 `0x35` 길을 탄 증거
