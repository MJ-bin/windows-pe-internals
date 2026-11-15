# run104 params-block — 해설

> 이 글은 [run104 문서](run104-params-block.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q63a** — Q63 의 (a)·(b)·(c)·(e)·(f), 커널 쪽: `Flags` 마스크 `0xf4010e11` 비트 · Environment 자리, `MiCfgInitializeProcess` 와 `+0x4a0`/`+0x4a8` · `PspLocateNtdllAddressesForScpCfg` 가 넣는 것, `PspBuildCreateProcessContext` 의 만들기 정보 `+0x11`, `Flags` 비트 31 을 누가 켜나, 작은 것 셋 (`_workspace/plan.md`, 이번에 **끝**)
  - Q63 은 원래 run32 가 남긴 "작은 정적 묶음" 이다. 2026-11-15 에 둘로 나눴다.
    - 커널 쪽 = **Q63a** (이번 바퀴).
    - ntdll 쪽 (d) = **Q63b** — 할지 말지 **사용자 결정 대기**. 이번엔 안 했다.
  - 남은 것: 동적 → **Q190 ①~⑤** (새로), 정적 → **Q191 ①~⑤** (새로, ⑤ 는 사용자 결정), `Flags` 비트 이름 → **Q63b**.
- 근거:
  - run 문서 [`run104-params-block.md`](run104-params-block.md) (가장 중요)
  - 요청서 [`_workspace/sast/104_params-block.md`](../../_workspace/sast/104_params-block.md)
  - 결과서 [`_workspace/sast/104_params-block_result.md`](../../_workspace/sast/104_params-block_result.md) (판정: **분석성공**)
  - flow.md · baggage.md 변경: 이번 바퀴 `git diff` (아직 커밋 전)
- 이번 바퀴는 **정적만** 했다. VM 은 안 켰다.
  - ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음).
  - 값 대조는 **run44 사진 로그를 다시 읽은 것**뿐이다 (2.10). 새로 돌린 실행은 없다.

### 먼저 짚어 둘 것 — 틀린 건 요청서 한 줄뿐이다

- 요청서 질문 4 에 "마스크 `0xf4010e11` 에 있어 **사용자가 줄 수 있는** 비트인데" 라는 말이 있었다.
- 이건 **반대**다. 마스크 안 비트는 **켜져 있으면 거절**된다 (2.3).
- 이 틀린 표현은 **요청서에만** 있었다 (run 문서 "정정").
  - run32 문서 · flow.md · [run32 해설 2.5 · 2.11](../02_sprint/run32-params-initblock-해설.md) 은 처음부터 "켜져 있으면 실패, 사용자는 못 켬" 으로 바르게 적혀 있다.
  - 그래서 run32 에 정정 줄은 안 붙였다. 요청서 파일도 고치지 않는다.
- 이 글을 읽을 때도 **run32 에서 읽은 그대로가 맞다**고 보면 된다.

### 이미 푼 말 — 링크만

- 오프셋, call/ret, 콜스택, 태그, 비트·플래그, 핸들 (**손잡이**) → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- PEB (**안내판**), `_EPROCESS` (관리사무소 쪽 **장부**) → [run10 해설](../02_sprint/run10-allocate-process-해설.md#peb)
- 작업 집합 (= 프로세스가 지금 실제 메모리에 들고 있는 페이지 목록) → [run10 해설 "껍데기와 장부"](../02_sprint/run10-allocate-process-해설.md#껍데기와-장부) · 그 구조의 `+0x4a0` 칸 → [run32 해설 2.2 한 줄 읽기 ②](../02_sprint/run32-params-initblock-해설.md)
- 프로세스 매개변수 블록 (**개업 서류**), 커널 풀 (관리사무소 **서랍**), 만들기 정보 구조 (**작업 지시서**), Fixup, 기능 스위치, 표준 핸들, `LdrSystemDllInitBlock` (관리실 **업무 수첩**) → [run32 해설](../02_sprint/run32-params-initblock-해설.md) 2.3 ~ 2.9
- CFG = 교환원, **CFG 비트맵 = 교환원의 출입 명단** → [run32 해설 2.9](../02_sprint/run32-params-initblock-해설.md) · [run63 해설](run63-cfg-func-table-해설.md) 머리 · [run96 해설](run96-bcrypt-interface-해설.md)
- 이미지 확장 쪽 (ntdll 건물 뒤 **별채**), ScpCfg 쪽 (별채 벽에 찍는 **본뜬 판**) → [run63 해설 2.2 · 2.7](run63-cfg-func-table-해설.md)
- `PspSystemDlls` (관리사무소 **세트 대장**), 그 0번 줄이 가리키는 카드 `PspNativeSystemDllData` → [run48 해설 2.5](run48-system-dll-boot-해설.md)
- `PsNtdllExports` (관리사무소 **벽 주소록**), INIT 구역 (부팅 뒤 버려지는 곳 `[추측]`) → [run16 해설 2.9](../02_sprint/run16-thunk-context-해설.md)
- `MitigationFlags` (장부의 완화 플래그 칸, test.exe `0x20`) → [run50 해설 2.11](run50-eprocess-create-해설.md)
- 프로세스 만들기 속성 (개업 신청서의 **별지 요청서**) → [run60 해설 2.11](run60-func-table-mrdata-해설.md) · 속성 목록 → [run74 해설 2.7](run74-thread-create-해설.md)
- VAD = 땅의 **구획 장부** → [run10 해설](../02_sprint/run10-allocate-process-해설.md)
- WoW64 → [run17 해설 2.9](../02_sprint/run17-ldr-init-entry-해설.md)
- 사진 K1 · P1a → [run44 해설](run44-baggage-snap-해설.md)

이번에 처음 나오는 말 (**마스크 읽는 법**, **풀 꼬리표**, **silo**, **표준 핸들 상태**, **꼴 검색**, **보안 프로세스**) 은 본문에서 처음 나오는 자리에서 한 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**, 프로세스 = **가게**.
- 프로세스 매개변수 = 부모가 써 준 **개업 서류**, 만들기 정보 = 이번 개업 건의 **작업 지시서**.
- `LdrSystemDllInitBlock` = 관리실 **업무 수첩**, CFG 비트맵 = 교환원의 **출입 명단**.
- run 문서가 이번에 새로 쓴 비유도 그대로 받는다:
  - 마스크 = **"이 칸들엔 체크하면 안 됨" 목록**, 비트 0 = 꼭 체크해야 하는 **"정리됨" 칸**.
  - 서류 사본 = **한 봉투**. 앞에 서류 본문, 그 뒤 글자들, 맨 뒤에 **환경 변수 쪽지**.

### 한 줄로 먼저

> 관리사무소는 부모가 낸 개업 서류를 받을 때 **"체크하면 안 되는 칸" 목록**으로 거른다.
> 통과하면 서류 사본을 **봉투 하나**에 만든다 — 환경 변수 쪽지는 따로 봉투를 안 쓰고 **맨 뒤에 붙인다.**
> 교환원 출입 명단 (CFG 비트맵) 자리는 **명단을 쓰는 가게만** 수첩에 적는다. test.exe 는 안 쓴다.

1. 마스크 `0xf4010e11` = **"켜져 있으면 거절"** 목록. 비트 0 만 반대로 **켜져 있어야** 한다 `[정적]` → 2.3
2. Environment 는 **블록 바로 뒤 (`블록 + Length`)**, 할당은 **한 번** (`ExAllocatePool2(0x41, Length + EnvironmentSize, 'PsPb')`) `[정적]` → 2.4
3. CFG 비트맵 칸 `+0x4a0`/`+0x4a8` 은 `MiCfgInitializeProcess` 가 아니라 **그 아래 `MiReferenceCfgVad`** 가 쓴다. CFG 가 꺼진 프로세스면 `+0x1e` 에서 아무것도 안 쓰고 끝 `[정적]` → 2.5
4. `PspNtdllScpFunctions` 넷 = ntdll 함수 주소가 아니라 **ntdll 바로 뒤 별채 안 주소** `[정적]` → 2.6
5. 만들기 정보 `+0x11` = **비트 2~3 표준 핸들 상태**, **비트 7 `PsAttributeChpe`** `[정적]` → 2.7
6. `Flags` 비트 31 을 켜는 커널 줄은 **못 찾았다** — 일반 길에선 꺼진 채 `[정적: 꼴 검색 한계 안에서]` → 2.8
7. 작은 것: `PackageDependencyData` · `CurrentDirectores` 는 부모 값 그대로, `PspSystemDlls[0]+0x20` = `PspNativeSystemDllData+0x20` **같은 칸** `[정적]` → 2.9

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 이야기는 거의 다 **부모 cmd 스레드 안, 커널**이다. 한 조각만 **부팅 때**다.

```
(부팅, 흐름도 범위 앞)
 MmInitializeImageViewExtensionCfg → PsInitializeScpCfgPages
   → PspLocateNtdllAddressesForScpCfg           ← (b) ScpCfg 전역 넷 채우기

cmd.exe 의 부탁 → 커널 NtCreateUserProcess
 ├─ +0xacb PspCaptureProcessParameters
 │    └─ +0x3f PspCaptureUserProcessParameters  ← (a) 마스크 검사 · 봉투 만들기 · (e) 비트 31
 ├─ PspBuildCreateProcessContext                ← (c) 작업 지시서 +0x11
 ├─ +0xc36 PspAllocateProcess
 │    ├─ +0x2095 MmInitializeProcessAddressSpace
 │    │    └─ +0x1ce MiMapProcessExecutable
 │    │         └─ (3) +0xe8 MiCfgInitializeProcess   ← (b) CFG 비트맵 칸  ★ 그림 1 새 노드
 │    └─ +0x239c PspSetupUserProcessAddressSpace
 │         ├─ +0x1b9 PspCopyAndFixupParameters  ← (f) 안 고치는 칸 · AttributeList
 │         └─ +0x1e6 PspPrepareSystemDllInitBlock  수첩 +0xb8/+0xc0 · +0xf0 ~ (run32)
 └─ 첫 스레드 출발 → LdrInitializeThunk (Rdx ← (f) 칸 이름) → … → NtContinue 은퇴 → main
```

- 맨 위 부팅 조각은 **시간 순서 밖**이다. test.exe 를 돌리기 훨씬 전에, 컴퓨터가 켜질 때 **한 번** 지나간다 (2.6).
- `PspBuildCreateProcessContext` 는 run 문서 호출 관계 그림에 offset 이 없어서 여기도 안 붙였다. flow.md 그림 1 에선 `s1` 상자다.

### 무엇이 궁금했나

run32 가 "정적으로 남은 것" 으로 넘긴 질문들이다.

- **(a)** 서류 `Flags` 마스크 `0xf4010e11` 의 비트마다 뜻은? 환경 변수 (Environment) 는 블록 어디에 두나 — 뒤에 붙이나, 따로 받나?
- **(b)** 수첩 `+0xb8`/`+0xc0` 의 출처인 작업 집합 구조 `+0x4a0`/`+0x4a8` 을 `MiCfgInitializeProcess` 가 쓰나? `PspLocateNtdllAddressesForScpCfg` 는 `PspNtdllScpFunctions` 에 무엇을 넣나?
- **(c)** 작업 지시서 `+0x11` (표준 핸들 상태) 을 채우는 줄과 값 뜻은?
- **(e)** `Flags` 비트 31 (ntdll 이 `LdrpIsSecureProcess` 로 옮기는 비트, run17) 을 누가 켜나?
- **(f)** `PackageDependencyData` · `CurrentDirectores` 를 왜 안 고치나, 스위치 이름 "SegmentHeap" · "SCP" 뜻, `Rdx` 출처 이름 두 개가 같은 칸인가?

### 바퀴 전에 알던 것

- `[정적]` (run32) `PspCaptureUserProcessParameters +0x8f ~ +0xa8` 이 비트 14 를 지운 뒤 `(Flags ^ 1) & 0xf4010e11 ≠ 0` 이면 `0xC000000D`. 곧 비트 0 은 켜져 있어야, 마스크의 다른 비트는 꺼져 있어야 한다. 비트 이름은 몰랐다.
- `[정적]` (run32) 새 프로세스 쪽은 `PspSetupUserProcessAddressSpace` 가 `MaximumLength + EnvironmentSize` 만큼 자리를 잡고 `PspCopyAndFixupParameters` 가 같은 크기를 복사한다. 커널 사본을 **어떻게 만드는지**는 안 봤다.
- `[정적]` (run32) 수첩 `+0xb8` ← 작업 집합 구조 `+0x4a0`, `+0xc0` ← `+0x4a8`. 칸 이름은 CfgBitMap · 크기 `[추측]`. 그 값을 만드는 쪽은 지도상 `MiMapProcessExecutable +0xe3 → MiCfgInitializeProcess` 로 **보였다**.
- `[정적]` (run32) `PspSetupUserProcessAddressSpace +0x41 ~` 이 `+0x11` 비트 2~3 을 **읽는다** (1 → 부모 표준 핸들 목록, 2 → 서류 안 칸, 0·3 → 없음). 채우는 줄은 Q63 (c).
- `[정적]` (run32) 커널이 `Flags` 에 켜는 비트는 14 · 15 · 29. 31 은 없었다.
- `[정적]` (run16) `LdrInitializeThunk` 로 가는 `Rdx` = `PspSystemDlls[0]` 칸 `+0x20`. sast/27 (run32) 은 같은 걸 `PspNativeSystemDllData +0x20` 이라 불렀다. 같은 칸인지 `[확인 필요]`.
- `[관찰]` (run44 K1) `Flags 0x4001` · `MaximumLength = Length = 0x734` · `MitigationFlags 0x20` · 수첩 `+0xb8 = 0`.

---

## 2. 찾은 것

### 2.1 한눈에

run 문서 "호출 관계" 를 줄였다 (숫자는 ret 자리, 줄 안 `+0x…` 는 그 함수 안 자리).

```
NtCreateUserProcess
 ├─ +0xacb  PspCaptureProcessParameters
 │           └─ +0x3f  PspCaptureUserProcessParameters
 │                ├─ +0x8f ~ +0xa8   Flags 검사 → 아니면 0xC000000D            ★ 2.3
 │                ├─ +0x31c  ExAllocatePool2(0x41, F + EnvironmentSize, 'PsPb')  한 덩어리 ★ 2.4
 │                ├─ +0x335 ~ +0x3a1  앞 0x450B 통째 복사 · +0x40e Flags · +0x411/+0x415 F
 │                ├─ +0x440 … +0x528  PspCopyUnicodeString ×8 (블록 +0x450 뒤)
 │                ├─ +0x588  메모리 복사 함수   환경 → 블록 + F
 │                └─ +0x5e1 Environment = 블록 + F
 └─ +0xc36  PspAllocateProcess
             ├─ +0x2095 MmInitializeProcessAddressSpace → +0x1ce MiMapProcessExecutable
             │            └─ (3) +0xe8  MiCfgInitializeProcess                     ★ 2.5
             │                  +0x1e  CFG 꺼짐 → 0 (아무것도 안 씀)   ← test.exe [추측]
             │                  ├─ +0x7b  MiMapSecurePureReserveView
             │                  └─ +0x9d  MiReferenceCfgVad(…, 0)  +0x71 시작 · +0x82 크기
             └─ +0x239c PspSetupUserProcessAddressSpace
                          ├─ +0x41  [만들기 정보 +0x11] 비트 2~3 읽기 (← PspBuildCreateProcessContext +0x309) ★ 2.7
                          ├─ +0x1b9 PspCopyAndFixupParameters   12칸 고침 · (조건) +0x407 AttributeList  ★ 2.9
                          └─ +0x1e6 PspPrepareSystemDllInitBlock  +0xb8/+0xc0 · +0xf0 ~
```

- 모두 `[정적]` (run 문서). `← test.exe` 줄만 `[추측]` 이다 (2.5 에서 이유).
- 부팅 쪽 (`PspLocateNtdllAddressesForScpCfg`) 은 2.6 에 따로 그렸다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 마스크로 거르는 줄

`PspCaptureUserProcessParameters +0xa3 TEST EAX,0xf4010e11` (바로 앞 `+0xa0 XOR EAX,1`, 바로 뒤 `+0xa8 JNZ`).

- **누가**: 커널 `PspCaptureUserProcessParameters` (부모가 낸 서류를 관리사무소 서랍으로 받아 오는 함수).
- **어디서**: 함수 안 `+0xa3`. CALL 이 아니라 **검사하는 줄**이다.
- **누구를**: 부모 서류의 `Flags` 칸 (`+0x8f MOV EDI,[RDX+8]` 로 읽고, `+0x92` 에서 비트 14 를 지운 값).
- **무엇을 위해**: "체크하면 안 되는 칸에 체크가 있나, 꼭 체크해야 할 칸이 비었나" 를 **한 번에** 보려고. 하나라도 걸리면 `+0xa8 JNZ` 로 뛰어 `+0xf3` 에서 `0xC000000D` 를 돌려준다 `[정적]`.

#### 한 줄 읽기 ② — 출입 명단 자리를 적는 줄

`MiReferenceCfgVad +0x71 MOV [R15],RBP`.

- **누가**: 커널 `MiReferenceCfgVad` (`MiCfgInitializeProcess` 가 `+0x98` 에서 부르는 아래 함수).
- **어디서**: 함수 안 `+0x71`. 값을 쓰는 줄이다.
- **누구를**: `R15` = 새 프로세스 `VmWorkingSetList + (번호 + 0x25) × 0x20`. 번호 0 이면 **`+0x4a0`** 칸이다 (`+0x33 ~ +0x3b` 에서 계산).
- **무엇을 위해**: 그 칸에 **CFG 비트맵 시작 주소** (`RBP`) 를 적으려고. 조금 뒤 `+0x82 MOV [R15+8],RBX` 가 옆 칸 `+0x4a8` 에 **크기**를 적는다 `[정적]`.
- 이 두 칸이 나중에 `PspPrepareSystemDllInitBlock +0x15a` · `+0x169` 를 거쳐 수첩 `+0xb8` / `+0xc0` 이 된다 (run32).

#### 한 줄 읽기 ③ — 작업 지시서 `+0x11` 에 표준 핸들 상태를 적는 줄

`PspBuildCreateProcessContext +0x309 MOV [R12+0x11],DL`.

- **누가**: 커널 `PspBuildCreateProcessContext` (부모가 붙인 별지 요청서 = 속성 목록을 하나씩 푸는 함수).
- **어디서**: 함수 안 `+0x309`.
- **누구를**: `R12` = 만들기 정보 (작업 지시서). 그 `+0x11` 바이트.
- **무엇을 위해**: 속성 `0x2000a` (`PsAttributeStdHandleInfo`) 가 왔을 때, 그 값의 **낮은 2비트 (상태)** 를 `+0x11` 의 **비트 2~3** 자리에 끼워 넣으려고. `+0x11 = (+0x11 & 0xf3) | (상태 << 2)` `[정적]`.

---

### 2.3 ★ (a) 마스크는 "켜져 있으면 거절" 목록이다

#### 새 말 — 마스크 읽는 법 (`XOR` 하고 `TEST`)

- **마스크** = 여러 비트 중 "이 자리들만 본다" 하고 골라 둔 숫자. 비유: 서류 위에 대는 **구멍 뚫린 판**. 구멍 자리만 보인다.
- `TEST A, 마스크` → 구멍 자리 중 **하나라도 켜져 있으면** "걸림" (`JNZ` 로 뜀).
- 그런데 그 앞에 `XOR 1` 을 하면 **비트 0 만 뒤집힌다.** 그래서 비트 0 만 뜻이 거꾸로 된다.

#### 단계별로 `[정적]` (`PspCaptureUserProcessParameters`)

1. `+0x8f MOV EDI,[RDX+8]` — 부모 서류의 `Flags` 를 읽는다.
2. `+0x92 BTR EDI,0xe` — **비트 14 를 먼저 지운다.** 부모가 켜서 내도 여기서 사라진다.
3. `+0xa0 XOR EAX,1` — 비트 0 을 뒤집는다.
4. `+0xa3 TEST EAX,0xf4010e11` — 구멍 자리에 켜진 게 있나.
5. `+0xa8 JNZ` → `+0xf3 MOV EAX,0xc000000d` — 있으면 실패.

그래서 →

- **비트 0**: 뒤집힌 뒤 꺼져 있어야 통과 = 원래는 **켜져 있어야** 한다.
- **비트 4 · 9 · 10 · 11 · 16 · 26 · 28 · 29 · 30 · 31**: 뒤집히지 않으니 **꺼져 있어야** 통과.
- 곧 마스크는 "받아 주는 비트" 가 아니라 **"켜져 있으면 안 되는 비트"** (+ 비트 0 하나만 "꼭 켜져 있어야 하는 비트") 목록이다.

- 비유: 관리사무소 창구에 **구멍 뚫린 판**이 있다. 판을 서류에 대면 열한 칸이 보인다.
  - "정리됨" 칸 (비트 0) 은 **꼭 체크**돼 있어야 한다.
  - 나머지 열 칸은 **비어 있어야** 한다. 하나라도 체크돼 있으면 "이 서류는 못 받습니다".
  - 판을 대기 전에 직원이 14번 칸 체크는 **지우개로 지운다.**

#### 둘째 검사 — 셋 중 하나만 `[정적]`

- `+0xaa ~ +0xba`: 비트 5 (`0x20`) · 6 (`0x40`) · 24 (`0x1000000`) 중 **둘 이상** 켜면 실패 (`+0xba JNZ → +0xfd`).
- 셋은 마스크 밖이라 줄 수는 있지만, **서로 배타**다.

#### 그래서 부모가 줄 수 있는 비트 `[정적]`

- **0 (필수)** + 1 · 2 · 3 · 5 · 6 · 7 · 8 · 12 · 13 · 15 · 17 ~ 25 · 27.
  - 이 중 5 · 6 · 24 는 하나만.
  - 14 는 줘도 지워진다.

#### 비트마다 — 이름은 PDB 에 없다

`_RTL_USER_PROCESS_PARAMETERS.Flags` 는 PDB 에서 그냥 `unsigned long` 이다. 비트 이름 열거가 없다 (결과서 1 (a)). 그래서 **코드가 알려 주는 만큼만** 적는다.

| 비트 | 값 | 들어올 때 | 커널이 읽거나 쓰는 줄 | 뜻 |
|---|---|---|---|---|
| 0 | `0x1` | **켜져야** | 사본에 그대로 (`+0x40e`) | "정규화됨" (run32). 공개 이름 `RTL_USER_PROC_PARAMS_NORMALIZED` 는 `[추측: 기억]` |
| 4 · 9 · 10 · 11 · 16 · 26 · 28 · 30 | | 꺼져야 | 본 커널 함수에 없음 | `[공백]` — 읽는 쪽은 ntdll (Q63b) |
| 29 | `0x20000000` | 꺼져야 | **커널이 켬** `PspSetupUserProcessAddressSpace +0xe6` (run32) | OneCore forwarders `[추측: 이름]` |
| 31 | `0x80000000` | 꺼져야 | 켜는 줄 없음 (2.8) | ntdll `LdrpIsSecureProcess` 로 감 (run17), "보안 프로세스" `[추측]` |
| (참고) 5 · 6 · 24 | `0x20` · `0x40` · `0x1000000` | 마스크 밖, 서로 배타 | `PspSetupUserProcessAddressSpace +0xa6` · `+0x15c` · `+0x15f` — 셋 다 꺼지고 크기가 `0x20000` 보다 작으면 `+0x161 ~ +0x16b` 에서 `0x20000` 으로 올림 | `RESERVE_1MB` · `RESERVE_16MB` `[추측: 기억]`, 24 는 `[공백]` |
| (참고) 14 | `0x4000` | **지움** | `PspCopyAndFixupParameters +0x82` 가 켬 (run50 조건) | IFEO 키 없음 |
| (참고) 15 | `0x8000` | 마스크 밖 | `PspSetupUserProcessAddressSpace +0xb1` (조건 `PspGlobalFlags & 1`) | `[공백]` |

- 눈여겨볼 것: 4 · 9 · 10 · 11 · 16 · 26 · 28 · 30 은 커널이 **막기만** 하고 **읽지 않는다.** 그러니 뜻은 **읽는 쪽 (ntdll)** 을 봐야 안다 → Q63b (결과서: "억지로 추측하지 않는다").
- 마스크 안 비트 중 **커널이 직접 켜는 건 29 하나**다. 곧 "부모는 못 켜고, 관리사무소만 찍어 주는 표시" 다 — [run32 해설 2.11](../02_sprint/run32-params-initblock-해설.md) 의 결론 그대로.

---

### 2.4 (a) 환경 변수 쪽지는 봉투 맨 뒤에 붙인다

#### 결론 `[정적]`

- **따로 할당하지 않는다.** `+0x317` (ret `+0x31c`) `ExAllocatePool2(0x41, F + EnvironmentSize, 'PsPb')` — **딱 한 번**.
- 환경은 `블록 + F` 에서 시작해 `EnvironmentSize` 바이트다. 여기서 **F = `MaximumLength` = `Length`**.

#### 새 말 — 풀 꼬리표 (`'PsPb'`)

- `ExAllocatePool2` 의 셋째 인자 `0x62507350` 을 글자로 읽으면 `PsPb` 다 `[정적]`.
- 커널 풀에서 받은 덩어리마다 붙이는 **네 글자 이름표**로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 디버거로 "이 덩어리 누구 거?" 를 볼 때 쓴다.
- 비유: 관리사무소 서랍 봉투에 붙인 **이름 스티커**.

#### 봉투를 만드는 차례 — 단계별로 `[정적]`

1. **재료 확인** (`+0xbc` · `+0xc8`): 부모 서류의 `Environment` (`+0x80`) 와 `EnvironmentSize` (`+0x3f0`) 를 읽는다.
   - 크기가 0 이거나 홀수면 실패.
   - 끝 주소가 `0x7fffffff0000` 을 넘으면 그 주소를 **일부러 읽어** 접근 위반을 낸다.
2. **봉투 크기 F 정하기** (`+0x26c ~ +0x2d4`): `F = (0x450 + 1 + 문자열 아홉 개 MaximumLength 합 + 현재 폴더 칸) & ~1`.
   - 현재 폴더 칸은 이 길에서 `0x208` (`+0x625`).
   - 셋째 인자 `+0x4` 비트 0 이면 4 맞춤 + 개수 × 8 을 더한다 (`+0x7d6 ~ +0x7f5`, CPU 집합 배열 자리).
3. **봉투 받기** (`+0x317`): `F + EnvironmentSize` 한 덩어리.
4. **서류 본문 통째 복사** (`+0x335 ~ +0x3a1`): 부모 서류 앞 `0x450` 바이트.
5. **머리 고쳐 쓰기** (`+0x40e` · `+0x411` · `+0x415`): `Flags` = 비트 14 뺀 값, `Length` = `MaximumLength` = F.
   - 부모가 적어 낸 길이가 아니라 **커널이 다시 센 값**이다.
6. **글자들 붙이기** (`+0x418` → `+0x43b ~ +0x528`): 커서를 `블록 + 0x450` 에 두고, 문자열 여덟 개를 `PspCopyUnicodeString` 으로 차례로 (`+0x38` · `+0x50` · `+0x60` · `+0x70` · `+0xb0` · `+0xc0` · `+0xd0` · `+0x410`, 조건부 `+0x420`). `RuntimeData` 는 따로 (`+0x53b` · `+0x683`).
7. **빈 곳 메우기** (`+0x567 ~ +0x570` · `+0x780`): 커서가 `블록 + F` 에 모자라면 0 으로 채워 맞춘다.
8. **환경 쪽지 붙이기** (`+0x583`, ret `+0x588`): 환경을 **`블록 + F`** 에 복사한다.
   - 부르는 함수는 "메모리 복사 함수" 다. 같은 주소에 이름이 4개 있어서 (후보 `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`) 하나로 못 정했다 (run 문서).
9. **환경 검사** (`+0x5bb`, ret `+0x5c0`): `PspValidateEnvironmentBlock` — 크기를 **줄일** 수 있다.
10. **주소 적기** (`+0x5e1` · `+0x5ed` · `+0x5f4`): `Environment` = `블록 + F`, `EnvironmentSize` = 검사 뒤 크기, `EnvironmentVersion` = 0.

- 비유: 관리사무소 직원이 **봉투 하나**를 꺼낸다.
  - 맨 앞에 서류 본문 (0x450) 을 넣고, 그 뒤에 글자 쪽지들 (경로 · 명령줄 …) 을 줄줄이 넣고, 모자란 자리는 빈 종이로 채운다.
  - **맨 뒤에 환경 변수 쪽지**를 붙인다. 쪽지용 봉투를 따로 만들지 않는다.
  - 봉투 표지의 "총 몇 쪽" 은 부모가 쓴 걸 믿지 않고 **직원이 다시 세서** 적는다.

#### 새 가게 쪽도 같은 모양 `[정적]` (run32 와 같음)

- `PspSetupUserProcessAddressSpace +0x115 ~ +0x11f` 가 `F + EnvironmentSize` 자리를 잡고, `PspCopyAndFixupParameters +0x36 ~ +0x39` 가 같은 크기를 복사한다.
- 그래서 새 가게 안 서류도 **봉투 하나 + 맨 뒤 환경** 모양이다. 2.10 의 사진 값이 바로 이걸 보여 준다.

#### 커널 사본에서 "다시 쓰는 칸" 은 이게 전부다 `[정적]`

flow-writer 가 `.asm` 에서 `[RDI…]` 에 저장하는 줄을 **다 셌다** (run 문서).

- 위 차례의 칸 + `+0x534` (`HeapPartitionName` 0, 셋째 인자 없을 때) · `+0x551` · `+0x558` (`DefaultThreadpoolCpuSetMasks` · `…Count` 0) · 조각 `+0x13bce9` · `+0x13bcf4` (배열 자리 · 개수) · `+0x819` (`DefaultThreadpoolThreadMaximum`, 셋째 인자 `+0x4` 비트 3) · `+0x84d` (`HeapMemoryTypeMask`, 비트 2) · `+0x5d1 ~ +0x5dd` (아래 silo 갈래 — `ConsoleHandle` · 표준 핸들 셋 0).
- **나머지 칸은 부모 서류 값 그대로**다.
- 이게 짐 지도 3.2 의 "짐작 22 → 정확" 의 근거다 (3절).

#### silo 갈래 (짧게)

- **silo** = 컨테이너처럼 따로 떼어 둔 이름 공간 `[추측]` (일반적으로 알려진 설명). 비유: 상가 안의 **구역** ([run32 해설 2.11](../02_sprint/run32-params-initblock-해설.md) 에서 처음 씀).
- `param_4 & 1` (= 만들기 정보 `+0x10` 비트 3, `PspCaptureProcessParameters +0x1f ~ +0x31`) 이면, 현재 폴더 · `RuntimeData` · `DllPath` 를 부모 서류 대신 **서버 silo 값**에서 가져온다 (`+0x135 ~ +0x185`) `[정적]`.
  - 이 갈래의 이름은 모른다 `[공백]`.
  - 환경은 그래도 **부모 것**이다.
  - 이 갈래에선 `ConsoleHandle` · 표준 핸들 셋을 0 으로 지운다 (`+0x5d1 ~ +0x5dd`). test.exe 는 안 탄다 (짐 지도 3.2).

---

### 2.5 ★ (b) 출입 명단 자리는 "명단 쓰는 가게" 만 적는다

#### `MiCfgInitializeProcess` 는 직접 안 쓴다 `[정적]`

- 이 함수에서 메모리에 쓰는 줄은 **지역 변수뿐**이다.
- 새 프로세스 장부 (`EPROCESS`) 는 **읽기만** 한다: `+0x13 [RCX+0x750]` · `+0x3e [RDI+0x368]` · `+0xa7 [RDI+0x6ec]`.

#### 쓰는 건 한 단계 아래 `MiReferenceCfgVad` 다 `[정적]`

1. `+0x16 [RCX+0xb8]` — 지금 붙어 있는 프로세스를 고른다. 새 프로세스다 (`MmInitializeProcessAddressSpace +0xb0 KiStackAttachProcess` 뒤라서).
2. `+0x33 ~ +0x3b` — 칸 = `VmWorkingSetList + (번호 + 0x25) × 0x20`.
   - 번호 0 → `+0x4a0`, 번호 1 → `+0x4c0`.
3. `+0x43` · `+0x5f` — `MiObtainReferencedVadEx` · `MiUnlockVadShared` 로 비트맵 VAD 의 끝 페이지를 읽는다.
4. 쓰기 넷:
   - **`+0x71` → `+0x4a0` = 비트맵 시작**
   - **`+0x82` → `+0x4a8` = 크기** (`((EndingVpn << 12) | 0xfff) − 시작 + 1`)
   - `+0x77` → `+0x10` = VAD
   - `+0x7e` → `+0x18` = 0 (번호 1 · `Machine 0x1c4` 면 1)

- 그래서 run32 의 수첩 `+0xb8` / `+0xc0` 이름 "CfgBitMap · 크기" `[추측]` 은 **칸 뜻으로는 `[정적]`** 이 됐다. 공개 이름은 여전히 PDB 에 없다.

#### 언제 서나 — 갈림길 하나 `[정적]`

1. `+0x13 MOV EAX,[RCX+0x750]` — 장부의 `MitigationFlags` 를 읽는다.
2. `+0x1c TEST AL,1` — 비트 0 (PDB `ControlFlowGuardEnabled`).
3. **`+0x1e JZ +0x179`** — 꺼져 있으면 **아무것도 안 쓰고 0** 을 돌려준다.
4. 켜져 있으면:
   - `+0x76` (ret `+0x7b`) `MiMapSecurePureReserveView` — 비트맵 섹션 (`MiState.Sections.CfgBitMapSection`) 을 기준 주소 (`MiState.Processes.NativeCfgBitmapPreferredAddress`) 에 붙인다. 실패하면 `+0x156` 에서 기준 주소 0 으로 다시.
   - `+0x98` (ret `+0x9d`) `MiReferenceCfgVad(…, 0)` — 위 넷을 쓴다.
   - WoW64 이고 `Machine` 이 `0x14c` / `0x1c4` 면 한 번 더 (`+0x119` · `+0x131`, 번호 1).

- 비유: 관리사무소가 땅을 고를 때 "**이 가게, 교환원 쓰나?**" 를 장부에서 본다.
  - 쓰면 → 출입 명단을 땅에 갖다 놓고, 명단 위치 · 크기를 작업 집합 노트 `+0x4a0` · `+0x4a8` 에 적는다. 이 노트를 나중에 수첩 `+0xb8` · `+0xc0` 에 옮긴다 (run32).
  - 안 쓰면 → **그냥 지나간다.** 노트 칸은 빈칸.

#### test.exe 는 "안 씀" 갈래로 보인다 — 왜 그렇게 짐작하나

1. 근거 `[관찰]`: run44 K1 사진에서 `MitigationFlags 0x20` (287줄). 비트 0 이 **꺼져** 있다.
2. 근거 `[관찰]`: 수첩 `+0xb8 = 0` (run82 · sast55).
3. 그래서 → `+0x1e` 에서 바로 끝나는 갈래였을 것이다.
4. 아직 확인 아님 `[추측]`: 사진 순간과 `MiCfgInitializeProcess` 가 도는 순간 사이에 비트 0 이 **안 바뀐다**는 가정이 들어 있다. → Q190 ③.

#### 남은 작은 것

- `MmInitializeProcessAddressSpace` 안에도 `MiReferenceCfgVad` 를 **직접** 부르는 자리가 둘 더 있다 (`+0x4a5` 번호 1 · `+0x4ba` 번호 0). 어느 길인지 안 봤다 `[공백]` → Q191 ②.

---

### 2.6 (b) 수첩 `+0xf0 ~` 의 재료 — 부팅 때 한 번, 별채 안 주소 넷

#### 언제 — 부팅 때 한 번 `[정적]`

```
MmInitializeImageViewExtensionCfg
 └─ +0x28d  PsInitializeScpCfgPages
             └─ +0x7c  PspLocateNtdllAddressesForScpCfg
                        ├─ +0x92  PspCopyNtdllExport  "RtlpScpCfgNtdllExports" 0x68B
                        └─ +0x1b9 · +0x1d0 · +0x1e7 · +0x1fd  MmGetScpCfgFunctionOffset
                                    → PspNtdllScpFunctions +0x0 ~ +0x18
```

- 이 함수들은 **INIT 구역**에 있다. 그래서 test.exe 를 돌릴 땐 **이미 지나갔다.** 디버거로 볼 때는 멈춤점이 아니라 **결과 칸을 읽어야** 한다.

#### 무엇을 가져오나 `[정적]`

- 이름 문자열은 **딱 하나**: `+0x83` 의 `"RtlpScpCfgNtdllExports"`.
- `+0x8d PspCopyNtdllExport` 가 ntdll 의 그 이름 내보내기 (ntdll rva `0x1746f0`) 에서 **표 `0x68` 바이트**를 통째로 복사한다.
- 표 안 (ntdll PDB 이름):
  - 쪽 넷의 (시작, 끝) 쌍 — `ScpCfgHeader_Nop` · `ScpCfgHeader` · `…_ES` · `…_Fptr`, 각 `0x2b0` 바이트. [run63 해설 2.7](run63-cfg-func-table-해설.md) 의 **본뜬 판 네 장**이다.
  - 8바이트 칸 넷 — `LdrpGuardDispatchIcallNoESFptr` · `__guard_dispatch_icall_fptr` · `LdrpGuardCheckIcallNoESFptr` · `__guard_check_icall_fptr`.
  - 함수 하나 — `LdrpHandleInvalidUserCallTarget`.
- 검사: 쌍은 길이 ≤ `0x1000` · ntdll 이미지 안이어야 한다 (아니면 `+0xf0` 에서 `0xC000000D`). 나머지 다섯 칸은 이미지 안인지만 본다.

#### 전역 넷에 넣는 것 `[정적]`

공식: `MmGetScpCfgFunctionOffset(쪽 안 위치, SizeOfImage)` = `(SizeOfImage 쪽 올림) + ImageViewExtensionOffsets[0] + 위치`.

| 전역 칸 | 저장 줄 | 쪽 안 위치 | 가리키는 것 (ntdll PDB 쪽 배치) |
|---|---|---|---|
| `PspNtdllScpFunctions+0x0` | `+0x1bf` | `0x140` | `ScpCfgValidateUserCallTarget` 자리 |
| `+0x8` | `+0x1d6` | `0x1c0` | `…ValidateUserCallTargetES` 자리 |
| `+0x10` | `+0x1ec` | `0x40` | `ScpCfgDispatchUserCallTarget` 자리 |
| `+0x18` | `+0x202` | `0xc0` | `…DispatchUserCallTargetES` 자리 |

#### 그래서 이 주소는 어디인가 — 단계별로

1. 공식 첫 항 = ntdll 이미지 끝을 쪽 단위로 올린 곳 = **ntdll 바로 뒤 첫 쪽** `[정적]`.
2. `ImageViewExtensionOffsets[0]` 이 0 이면 (run63 · 짐 지도 7.6.4: `MmInitializeImageViewExtension +0x14` 가 0) 더할 게 없다.
3. 그래서 → 네 주소 = **ntdll 바로 뒤 첫 쪽 + 위치**. run63 의 "ntdll 이미지 뒤 확장 쪽" (`ntdll + 0x267000`) 과 **같은 쪽**이다 `[정적]`.

- 비유: 관리사무소는 문 여는 날 관리실 건물 설계도를 보고, "**건물 뒤 별채** 벽의 몇 번째 자리에 무엇이 찍힐지" 를 미리 수첩용 메모에 적어 둔다.
  - 메모에 적는 건 **관리실 안 방 번호**가 아니라 **별채 벽 자리**다.
  - 가게가 생길 때마다 그 메모를 업무 수첩 `+0xf0 ~` 에 베껴 준다 (`PspPrepareSystemDllInitBlock +0x196`, run32).
- 돌려주는 쪽 `PspGetScpCfgFunctions +0x92` 는 `+0x85 PsGetScpCfgPageTypeForProcess` 가 3 · 4 면 NULL 을 준다 `[정적]`. 그러면 수첩에 안 베낀다.

#### 아직 빈칸 하나

- 이름 거리로 본 `PspNtdllScpFunctions` 크기는 `0x38` (7칸) 인데, 뒤 셋 (`+0x20 ~ +0x30`) 을 쓰는 줄은 `_refs` 에 없다 `[공백]`. 레지스터+오프셋 쓰기라서 못 잡았을 수 있다 → Q191 ③.

---

### 2.7 (c) 작업 지시서 `+0x11` — 두 비트 묶음

#### 새 말 — 표준 핸들 상태

- 표준 핸들 (입력 · 출력 · 오류 손잡이 셋) 을 새 가게에 **어떻게 넘겨줄지** 정한 값. PDB 열거형 `_PS_STD_HANDLE_STATE` 이름 그대로 `[정적]`:
  - **0** `PsNeverDuplicate` — 안 줌
  - **1** `PsRequestDuplicate` — 요청하면 줌 (부모 표준 핸들 목록을 가져옴)
  - **2** `PsAlwaysDuplicate` — 늘 줌 (서류 안 표준 핸들 칸 그대로)
  - **3** `PsMaxStdHandleStates` — "끝 표시" 라 **거부**
- 비유: 부모 가게 **창구 열쇠** 복사 지시. "복사 안 함 / 달라고 하면 복사 / 무조건 복사" 셋 중 하나를 지시서에 적는다.
- run32 가 `[추측]` 으로 "공개 문서의 `PS_STD_HANDLE_INFO.StdHandleState` 와 같은 모양" 이라 했던 것이, 이번에 PDB 열거형 이름으로 맞춰졌다.

#### 쓰는 줄은 이 함수에 **둘뿐** `[정적]` (결과서 `grep` 전수)

| 줄 | 어느 별지 (속성) | 하는 일 | 값 뜻 |
|---|---|---|---|
| **`+0x309`** | `0x2000a` = `PsAttributeStdHandleInfo` (10) | 크기 8 검사, 첫 8B 읽기. `+0x2d1 TEST AL,0x1c` 켜지면 `0xC00000BB`. `+0x2e2` 낮은 2비트 = 상태, `+0x2ed` 3 이면 `0xC000000D`. 상태 1 이면 윗 4B 를 `[R12+0x120]` (`+0x318 ~ +0x31c`) | **비트 2~3 = 표준 핸들 상태** |
| **`+0x1760`** | `0x6001a` = `PsAttributeChpe` (26) | 크기 1, 값 ≠ 0 이면 비트 7 (`+0x174f NEG` · `+0x1752 SBB` · `+0x1754 AND CL,0x80`) | **비트 7 = CHPE 속성 켜짐** (뜻 설명은 모름 `[공백]`) |

- 이 지시서를 읽는 쪽은 run32 그대로다: `PspSetupUserProcessAddressSpace +0x41 ~ +0x71` (1 → `+0x92 PspGetStandardHandleList`, 2 → 서류 안 표준 핸들 칸, 0 · 3 → 없음).
- 같은 `+0x11` 바이트의 다른 손:
  - 비트 0 = `PspCaptureCreateInfo +0x56 ~ +0x68` 이 채운다.
  - `PspCaptureProcessParameters +0x63 AND [RBX+0x11],0xf3` 는 비트 2~3 을 **지운다** — 2.4 의 silo 갈래 (`param_4 & 1`) 일 때.
- test.exe 가 상태 **몇**을 받는지는 정적으로 못 정한다. 부모 cmd 가 무엇을 붙여 보냈느냐 (실행 때 값) 라서다 → Q190 ②.

---

### 2.8 (e) `Flags` 비트 31 — 일반 길에선 아무도 안 켠다

#### 입력으로는 못 준다 `[정적]`

- 비트 31 (`0x80000000`) 은 마스크 안이다. 켜서 내면 2.3 의 판에 걸려 `0xC000000D`.

#### 커널이 `Flags` 에 쓰는 줄 (본 함수들) `[정적]`

- `PspCaptureUserProcessParameters +0x40e` (입력 − 비트 14)
- `PspCopyAndFixupParameters +0x82` (14)
- `PspSetupUserProcessAddressSpace +0xb1` (15) · `+0xe6` (29)
- **31 은 없다.**

#### 새 말 — 꼴 검색

- 기계어 풀이 전체 (`objdump -d`) 에서 "**이런 모양의 명령**" 을 글자 맞추기로 찾는 것. 이번엔 `nt.dis` 3,086,164줄을 훑었다 (스크래치, 커밋 안 함).
- 한계: 정해 둔 모양만 찾는다. 레지스터로 값을 계산해 넣는 다른 모양은 놓칠 수 있다. 그래서 결과는 "**이 꼴들에는 없다**" 이지 "전혀 없다" 가 아니다 (run 문서 "환경").
- 비유: 서류 더미에서 "31번 칸에 체크" 라는 **도장 모양**만 찾아본 것. 손글씨로 쓴 체크는 못 찾았을 수 있다.

#### 찾은 결과 `[정적]` (결과서 4)

| 꼴 | 맞은 곳 | 판정 |
|---|---|---|
| `bts [reg+0x8],0x1f` | 1곳 (`HvlAddPagesCallbackRoutine +0xdb`) | 매개변수 블록 아님 |
| `or [reg+0x8],0x80000000` | 0곳 | — |
| 레지스터에 켜고 15명령 안에 `[reg+0x8]` 저장 | 3곳 (`SmPageWrite` · `ExBuildPasidMsrForThread` · `ExpUpdateDebugInfo`) | 무관 |
| 즉시값 저장 | 28곳 | 무관 |

- 곁눈질 (범위 밖, 읽기만): `securekernel.exe` · ntdll 스크래치 풀이에도 같은 꼴 **0곳**.

#### 그래서 — 단계별로

1. 부모는 못 켠다 (판에 걸림) `[정적]`.
2. 커널이 켜는 꼴도 못 찾았다 `[정적: 꼴 검색 한계 안에서]`.
3. 그래서 → **test.exe 같은 일반 `NtCreateUserProcess` 길에서 비트 31 은 꺼진 채**다 `[정적: 꼴 검색 한계 안에서]`.
4. 사진도 맞다: `Flags 0x4001` = 비트 0 · 14 만 `[관찰]` (run44 K1).
5. 그럼 누가 켜나? **보안 프로세스**를 만드는 다른 길이 켤 것이다 `[추측]`.
   - **보안 프로세스 (trustlet)** = 가상화 기반 보안 쪽에서 따로 보호받으며 도는 특수 프로세스로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 일반 창구가 아니라 **금고방 쪽 창구**로 여는 특수 가게.
   - 이 길은 이번에 안 봤다 → Q191 ⑤ (사용자 결정).

---

### 2.9 (f) 작은 것 셋

#### (f-1) 안 고치는 두 칸 — 부모 값이 그대로 간다 `[정적]`

- `PspCopyAndFixupParameters` 가 고치는 칸은 **12개**다. 두 갈래가 같다 (스위치 켜짐 `+0x22b ~ +0x314`, 꺼짐 `+0x424 ~ +0x50d`).
- `PackageDependencyData (+0x400)` · `CurrentDirectores (+0xf0 ~ +0x3ef)` 는 **어느 목록에도 없다.**
- 커널 사본에서도 2.4 의 통째 복사 (`+0x335 ~ +0x3a1`) 뒤로 이 칸들을 쓰는 줄이 없다.
- 그래서 → 부모가 0 을 적었으면 0, 아니면 **부모 가게 안 주소가 그대로** 자식에게 간다.
- **왜** 안 고치는지는 코드로 못 정한다 `[추측: 만드는 쪽이 0 으로 두는 칸]`.
  - 사진은 `PackageDependencyData (null)` · `CurrentDirectores` 0 이다 (짐 지도 3.2 "끝까지 0") — 부모가 0 이라 문제가 안 됐다.

덤 — `AttributeList (+0x448)` 는 따로 덮는다 `[정적]`:

- `+0x3c7 CMP [R15+0x250],R13D` — 유저 복사 속성 개수가 0 보다 크면
- `+0x3f8 LEA RCX,[RDI+0x448]` → `+0x402 RtlWriteULong64ToUser` (ret `+0x407`) 가 새 목록 주소로 덮는다.
- 이게 run60 이 남긴 "정적으로 남은 것 ①" (`PspCopyUserAttributes` 목록이 `+0x448` 에 이어지는 줄) 의 답이다. test.exe 는 목록이 비어 안 탄다 (run44 `+0x448` 0).

#### (f-2) 이름 두 개의 뜻 — 둘 다 `[추측]`

- **"SegmentHeap"** — 스위치 `Feature_CreateProcessSegmentHeapEnablement__private_featureState` (rva `0xfc4110`).
  - "Segment Heap" = 윈도 새 힙 구현 이름 `[추측]`.
  - 단서: 이 스위치 아래에서 쓰는 칸이 `HeapPartitionName (+0x420)` · `DefaultThreadpoolCpuSetMasks (+0x430)` · `HeapMemoryTypeMask (+0x440)` 이고, PDB 속성 열거에 `PsAttributeHeapFeatures = 34` 가 있다.
  - 그래서 "프로세스 만들 때 힙 기능 속성을 서류 뒷칸에 담는 기능" 이름으로 보인다. 확정 아님.
- **"SCP"** — 코드에 풀이가 없다. 약어 풀이는 모른다 `[공백]`.
  - 단서: 커널 구역 `KSCP`, `MiMapKernelScp` · `RtlCreateKernelScpFunctionTable` · `MiMapImageScpCfgPages` · `KscpCfgDispatchUserCallTargetEsSmep`.
  - "이미지 밖에 따로 붙이는 CFG 검사 · 디스패치 코드 쪽" 이름으로 보인다 `[추측]` — 2.6 의 별채 그림과 맞는다.

#### (f-3) `Rdx` 출처 이름 둘 — **같은 칸**이다 `[정적]`

단계별로:

1. PDB 주소: `PspSystemDlls` = rva `0xfd8130` (포인터 6칸 표).
2. 파일 초기값: 표 `[0]` = `0x140fd8980` = **`PspNativeSystemDllData`** (rva `0xfd8980`).
   - [run48 해설 2.5](run48-system-dll-boot-해설.md) 의 표 그대로다: 세트 대장 0번 줄이 가리키는 **관리실 세트 카드**.
3. 표 `[0]` 을 쓰는 줄은 `PsLocateSystemDlls +0x8d AND [RBX],0` 하나 (실패 때 비움). 평소엔 초기값 그대로.
4. 그래서 → `PspInitializeThunkContext +0x196 MOV RAX,[PspSystemDlls]` · `+0x19d MOV RCX,[RAX+0x20]` = `[PspNativeSystemDllData+0x20]` = rva **`0xfd89a0`**.

- 곧 run16 · flow.md 의 `PspSystemDlls[0]+0x20` 도 맞고, sast/27 의 `PspNativeSystemDllData+0x20` 도 맞다. **같은 칸을 두 이름으로 부른 것뿐**이다. 그래서 정정이 아니라 run32 의 열린 질문을 닫은 것이다 (run 문서 "정정").
- 비유: "세트 대장 0번 줄이 가리키는 카드의 기본 주소 칸" = "관리실 세트 카드의 기본 주소 칸". 같은 카드, 같은 칸.

**헷갈리지 말 것** `[정적]`:

- `_refs/PspSystemDlls.tsv` 의 **`PspSystemDlls +0x20`** (`MOV RDX,[0x140fd8150]`, `PspInitPhase3 +0x89` 등) 은 **표의 5번째 칸 `[4]`** 다.
- **`PspSystemDlls[0]+0x20`** 은 "0번 칸이 가리키는 카드의 `+0x20`" 이다.
- 비유: "대장 **다섯째 줄**" 과 "대장 첫째 줄이 가리키는 카드의 **+0x20 칸**" 은 다르다. 글자가 비슷해서 적어 둔다.

남은 것: 이 칸이 "ntdll 실제 베이스" 인지, 옆 `+0x18` 과 같은지는 정적으로 못 정한다 → Q190 ④.

---

### 2.10 사진과 맞춰 보면 (run44, 새로 돌린 것 아님)

flow-writer 가 run44 K1 덤프 읽기 로그 (`ms/logs/04_baggage-read_K1.txt`) 를 다시 읽어 정적 결과와 맞춰 봤다 (run 문서). 새 프로세스 블록 = PEB `+0x20` `25996cb0000`.

| 정적이 말한 것 | 사진 값 `[관찰]` | 맞나 |
|---|---|---|
| `Length` = `MaximumLength` = F (`+0x411` · `+0x415`) | 둘 다 `0x734` (2335 ~ 2336줄) | **맞음** |
| `Environment` = `블록 + F` (`+0x5e1`) | `0x25996cb0734` = `25996cb0000 + 0x734` (2348줄) | **맞음** |
| `EnvironmentVersion` = 0 (`+0x5f4`) | K1 `0`, P1a 부터 3 (짐 지도 3.2) | 맞음 (뒤 바뀜은 ntdll) |
| `Flags` = 입력 − 비트 14, 그 뒤 14 켬 | `0x4001` (2337줄) — 비트 0 · 14 만 | 맞음 (비트 31 꺼짐) |
| `PackageDependencyData` · `CurrentDirectores` 그대로 | `(null)` · 배열 0 | 맞음 (부모가 0) |
| CFG 꺼지면 `+0x1e` 에서 끝 | `MitigationFlags 0x20` (287줄) · 수첩 `+0xb8` = 0 | **맞음** — test.exe 는 `+0x1e` 갈래였을 것 `[추측: 그 사이 비트 0 이 안 바뀜]` |

- 둘째 줄이 2.4 결론의 가장 분명한 증거다. **환경 주소 − 블록 주소 = `0x734` = `Length`.** 곧 환경은 서류 바로 뒤에 붙어 있다.

### 2.11 비유로 다시 한 번

- 부모 cmd 가 **개업 서류**를 낸다.
- 관리사무소 창구 직원이 14번 칸 체크를 지우고, **구멍 뚫린 판**을 댄다. "정리됨" 칸만 체크돼 있고 나머지 열 칸이 비어 있어야 받는다. (2.3)
- 받으면 **봉투 하나**에 사본을 만든다: 본문 → 글자 쪽지 → 빈 종이 → 맨 뒤 **환경 쪽지**. 표지의 쪽수는 직원이 다시 센다. (2.4)
- 땅을 고를 때 "이 가게 교환원 쓰나?" 를 본다. 안 쓰면 **출입 명단 자리는 빈칸**. test.exe 는 빈칸. (2.5)
- 문 여는 날 미리 적어 둔 **별채 벽 자리 메모 넷**을 업무 수첩에 베껴 준다. (2.6)
- 작업 지시서 `+0x11` 에는 "**창구 열쇠 복사 방식**" (비트 2~3) 과 CHPE 표시 (비트 7) 가 적힌다. (2.7)
- 비트 31 은 이 창구에선 **아무도 안 찍는다.** 금고방 가게를 만드는 다른 창구가 찍을 것 같다. (2.8)

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 — 그림 1

이번엔 그림 1 에 **노드가 하나 늘었다.** 그림 제목 괄호에 run104 가 붙었다.

| 무엇 | 전 | 후 |
|---|---|---|
| 새 노드 **`p11`** | 없음 | `nt!MiCfgInitializeProcess` (정적 run104: CFG 비트맵 칸. `MitigationFlags` 비트 0 꺼지면 `+0x1e` 에서 바로 0 (test.exe 는 `0x20` — 이 길 추측). 켜지면 안쪽 `MiReferenceCfgVad` 가 작업 집합 구조 `+0x4a0` 시작 · `+0x4a8` 크기) |
| 새 화살표 | `p3` (`MiMapProcessExecutable`) 아래 **(1) exe · (2) ntdll** 둘 | **(3) `+0xe8` → `p11`** 이 더해져 셋 |
| `s1` `PspBuildCreateProcessContext` | "(정적)" | + "속성 목록 풀기. run104: 만들기 정보 `+0x11` 비트 2~3 = 표준 핸들 상태 (`+0x309`), 비트 7 = `PsAttributeChpe` (`+0x1760`)" |
| `p10` `PspPrepareSystemDllInitBlock` | "`+0xb8` CFG 비트맵 주소" | "`+0xb8/+0xc0` CFG 비트맵 시작·크기 (← 작업 집합 구조 `+0x4a0/+0x4a8`), `+0xf0 ~` ScpCfg 표 (← 부팅 때 채운 `PspNtdllScpFunctions`)" |
| 커널 색 (`kerng`) 목록 | `… p9,p10,u1 …` | `p11` 더함 |

- 그림을 따라 읽으면 이제 이렇게 된다: `MiMapProcessExecutable` 이 **exe 를 놓고 → ntdll 을 놓고 → 출입 명단 자리를 정한다** (CFG 쓰는 가게만).
  - 그리고 오른쪽 아래 `p10` 상자에 "그 자리를 수첩에 옮긴다" 가 적혀 있어서, **`p11` 에서 만든 값이 `p10` 으로 가는 길**이 상자 글로 이어진다.
- 4.1 (대표 지점 표) 그림 1 nt 줄에도 "(3) `MiCfgInitializeProcess` (`(정적)` 상자, run104 — ret `+0xe8`) · (3) 은 늘 부르고 CFG 꺼지면 `+0x1e` 에서 바로 끝" 이 더해졌다.

### flow.md 에서 바뀐 것 — 글

| 자리 | 전 | 후 |
|---|---|---|
| 2.1 `PspBuildCreateProcessContext` | `+0x11` 채우는 줄 없음 | `+0x309` (비트 2~3) · `+0x1760` (비트 7) `[정적]` |
| 2.1 `MiMapProcessExecutable` | (1) · (2) | **새 항목 4: `+0xe3` (ret `+0xe8`) `MiCfgInitializeProcess`** 본문 요약 `[정적]` + test.exe 갈래 `[관찰]`+`[추측]` + `MmInitializeProcessAddressSpace +0x4a5` · `+0x4ba` `[공백]` |
| 2.1 `PspCopyAndFixupParameters` | `Dst` 크기 "작으면 `0x20000` 으로 올림" | + "비트 5 · 6 · 24 가 셋 다 꺼졌을 때만" |
| 2.1 같은 절 | 커널 사본 모양 없음 | **커널 사본 모양** (`PspCaptureUserProcessParameters` 차례) `[정적]` |
| 2.1 같은 절 SegmentHeap | "왜 묶였는지 모른다" | + run104 `[추측]` 단서 |
| 2.1 같은 절 Fixup | "이유 모름" | + 두 갈래 모두 12칸, 두 칸은 커널 사본에서도 안 씀 `[정적]` |
| 2.1 같은 절 run60 `+0x448` | `[공백]` (4장) | 줄 그음 → `+0x3c7` · `+0x3f8` · `+0x402` `[정적]` |
| 2.1 같은 절 표준 핸들 | "`+0x11` 을 채우는 줄은 Q63 (c)" | 줄 그음 → `PspBuildCreateProcessContext +0x309` `[정적]` |
| 2.1 `PspPrepareSystemDllInitBlock` `+0xb8` | 크기 `[추측]` | + 두 칸 뜻 = CFG 비트맵 시작 · 크기 `[정적]` |
| 2.1 같은 절 `+0xf0 ~` | "어떤 함수인지, SCP 뜻은 모른다 (Q63 (b))" | 줄 그음 → ntdll 끝 바로 뒤 쪽 안 주소 넷 `[정적]` |
| 2.1 같은 절 "그 함수가 `+0x4a0` 칸을 쓰는지는 Q63 (b)" | Q63 (b) | 줄 그음 → `MiReferenceCfgVad +0x71` / `+0x82` `[정적]` |
| 2.1 · 2.3 · 2.4 `Rdx` 칸 | `PspSystemDlls[0]+0x20` | + "같은 칸 = `PspNativeSystemDllData+0x20` (rva `0xfd89a0`), `PspSystemDlls +0x20` (표 칸 `[4]`) 과는 다름" `[정적]` |
| 2.1 짝 표 | `Q63 (d)` | `Q63 (d) = Q63b, 사용자 결정 대기` (셋) + 마스크 뜻 한 줄 `[정적]` |
| 2.3 부팅 절 | — | ScpCfg 전역 한 줄 `[정적]` |
| 2.4 `LdrpInitialize` 비트 31 | 이름 `[공백]` | + 일반 길에선 꺼짐 `[정적: 꼴 검색 한계 안에서]` |
| 4장 | Q63 줄 · run60 정적 ① 열림 | Q63 줄 (a)(b)(c)(e)(f) 해결됨, run60 ① 해결됨, run104 새 줄 셋 (동적 · 정적 · PE_emulator) |

- 태그가 바뀐 곳만 다시 말하면:
  - 수첩 `+0xb8` / `+0xc0` 뜻 — `[추측]` → **`[정적]`** (이름은 여전히 PDB 에 없음).
  - `PspNtdllScpFunctions` 넷의 정체 — 모름 → **`[정적]`**.
  - 만들기 정보 `+0x11` 을 쓰는 줄 — 빈 곳 → **`[정적]`**.
  - `Rdx` 칸 두 이름 — `[확인 필요]` → **`[정적]` 같은 칸**.

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- **새 짐 칸 없음.** 대신 **짐작 22칸이 정확으로** 올라갔다.
- 무엇이 짐작이었나:
  - 3.2 (프로세스 매개변수) 첫 줄은 "부모 블록 값을 **통째 복사**" 한 22칸이었다 (`MaximumLength` · `Length` · `DebugFlags` · `ConsoleFlags` · 창 칸 10 · `CurrentDirectores` · `EnvironmentSize` · `EnvironmentVersion` · `PackageDependencyData` · `ProcessGroupId` · CPU 집합 개수 · 스레드 최대 · `HeapMemoryTypeMask` · `AttributeList`).
  - 이게 `[추측]` 이었던 건 "**칸 단위로 말한 문서가 없어서**" 였다 (baggage.md 2장).
- 왜 이번에 정확이 됐나 — 단계별로:
  1. 커널 사본 함수 `PspCaptureUserProcessParameters` 를 읽어 **앞 `0x450`B 통째 복사** (`+0x335 ~ +0x3a1`) 가 코드로 보였다.
  2. 그 뒤 **다시 쓰는 칸을 전부** 셌다 (2.4 끝).
  3. 그래서 22칸 각각이 "커널이 다시 씀 / 조건부 / 부모 값 그대로" 중 어디인지 칸 단위로 말할 수 있게 됐다.
     - 7칸은 커널이 다시 씀 (F 두 칸 · 환경 크기 · Version 0 · 셋째 인자 칸 셋).
     - `AttributeList` 는 조건부 (`PspCopyAndFixupParameters +0x402`).
     - 14칸은 부모 값 그대로.
- 숫자:
  - 2장 합: 정확 **685 → 707**, 짐작 **53 → 31**. 아는 칸 738 · 아무도 안 씀 160 · 범위 밖 257 · 모름 301 은 그대로.
  - 3.2 머리: 정확 18 → **40**, 짐작 22 → **0**, 줄 18 → **25** (새 줄 7 = 커널 사본 6 · `AttributeList` 1, 근거 칸 `sast104`).
- 그 밖: 3.9 `LdrSystemDllInitBlock` (`+0xb8/+0xc0` 쓰는 쪽 · `+0xf0` 내용), 4장 짝 두 줄 (블록 `+0xb8` 출처 · `Rdx` 칸 이름), 7.4 의 22칸 줄 해결됨, 7.6.4 커널 전역 `PspNtdllScpFunctions` 넷 (체크리스트 밖), 새 **7.36**.

### 큰 그림에서

- run32 에서 그려 둔 "**개업 서류가 지나는 세 정거장**" 중 **첫 정거장 (관리사무소 사본)** 의 속이 이제 다 열렸다.
  - 둘째 (새 가게 빈 방) · 셋째 (옮기고 안내판에 적기) 는 run32 에서 이미 열렸다.
  - 그래서 서류 칸 하나하나가 "**누가 어느 정거장에서 정했나**" 를 끝까지 말할 수 있다. 짐 지도 3.2 가 짐작 0 이 된 이유다.
- 업무 수첩 (`LdrSystemDllInitBlock`) 쪽도 출처가 한 단계 더 깊어졌다.
  - `+0xb8` / `+0xc0` ← 작업 집합 노트 ← **`MiReferenceCfgVad`** (CFG 가게만).
  - `+0xf0 ~` ← **부팅 때 메모** ← ntdll 의 `RtlpScpCfgNtdllExports` 표.
  - 관리실 쪽에서 이 칸을 읽는 길 (run28 교환원 · run63 별채) 과 이제 **양 끝이 다 이어졌다.**
- PE_emulator 쪽 (결과서 요약, `../PE_emulator` 코드는 flow-writer 가 안 읽음 — 고칠지는 사용자 결정):
  - **맞음** — 부모 블록 `Flags = 1`, 환경 `base + dwLength`, 현재 폴더 칸 `0x208`, 문자열 차례, `+0x4a0`/`+0x4a8` 0 (test.exe 한정), 비트 31 안 켬, Fixup 12칸.
  - **줄임 (틀리지 않음)** — ScpCfg 쌍 1 ~ 3 · 다섯 칸 · 전역 넷 채우기 "공사 중 (D33)". 빠진 값 계산은 2.6 표.
  - **확인 필요** — `+0x11` 실제 상태 값 (Q190 ②), `SystemDll.cpp:244` `Rdx` 값 ("같은 칸" 까지만 확정, Q190 ④).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `Flags` 세 지점 값 (`nt+0x9f151e` `eax` · `nt+0xae5e21` `ebx` · `nt+0xae5eed` 뒤 블록 `+0x8`) — `0x4001` 인가, 비트 31 = 0 인가 | 실행 때 값. 사진이 이미 `0x4001` 이라 **확인용** | **Q190 ①** 동적 |
| test.exe 의 만들기 정보 `+0x11` — 비트 2~3 (표준 핸들 상태) · 비트 7, 쓰는 줄 `nt+0x840c29` · `nt+0x842080` 이 서나 | 부모가 무엇을 붙였나 (실행 때 값). **새로 볼 값** | **Q190 ②** 동적 |
| `ControlFlowGuardEnabled` 와 `nt+0x90b652` (`MiCfgInitializeProcess +0x1e`) 에서 뛰나 | 사진 순간과 그 순간 사이 비트 0 이 안 바뀐다는 가정 `[추측]`. **확인용** | **Q190 ③** 동적 |
| `PspNtdllScpFunctions` 7칸 실제 값 (뒤 셋 포함), `ImageViewExtensionOffsets[0]` = 0 인가, `PspNativeSystemDllData +0x18` 과 `+0x20` 이 같은가 (= `Rdx`) | 부팅 때 정해지는 값 — 멈춤점이 아니라 부팅 뒤 **읽기만**. **새로 볼 값** (Q42 D1 과 같은 질문) | **Q190 ④** 동적 |
| 블록 `+0x400` · `+0xf0 ~ +0x3ef` 가 0 인가 | 실행 때 값. 사진이 이미 0 이라 **확인용** | **Q190 ⑤** 동적 |
| `PspFinalizeScpCfgPage` · `PsGetScpCfgPageTypeForProcess` — 다섯 칸 (`+0x40 ~ +0x60`) 이 별채 안에서 어떻게 쓰이나, 어떤 프로세스가 어떤 쪽 종류를 받나 | 이번 범위 밖 (이름만) | **Q191 ①** 정적 (PE_emulator D33) |
| `MmInitializeProcessAddressSpace +0x4a5` · `+0x4ba` 의 `MiReferenceCfgVad` 두 자리가 어느 길인가 | 안 봄 `[공백]` | **Q191 ②** 정적 (작음) |
| `PspNtdllScpFunctions +0x20 ~ +0x30` 셋을 쓰는 곳 | `_refs` 에 없음 (레지스터+오프셋 쓰기일 수 있음) | **Q191 ③** 정적 (작음) |
| 커널 사본 셋째 인자 (`R15`) 의 정체와 `+0x4` 비트 0 · 2 · 3 을 켜는 쪽, `param_4 & 1` silo 갈래 이름 | 안 따라감. 속성 `0x20` 대 (`PsAttributeHeapFeatures` 류) 로 보임 `[추측]` | **Q191 ④** 정적 (작음) |
| `Flags` 비트 31 을 정말 누가 켜나 (보안 프로세스 길 · `securekernel.exe`) | 일반 길 밖. `securekernel.exe` 는 PDB 없음, 새 analyze 필요 | **Q191 ⑤** 정적 — **사용자 결정** (꼭 필요할 때만) |
| `Flags` 비트 4 · 9 · 10 · 11 · 16 · 26 · 28 · 30 · 31 (+ 비트 25 `0x2000000`) 이름, 블록 문서 없는 칸, `RtlNormalizeProcessParams` 가 하는 일 | 커널은 막기만 하고 안 읽음. PDB 에 이름 없음. 읽는 쪽 ntdll 은 이번 범위 밖 | **Q63b** 정적 (ntdll) — **사용자 결정 대기** |
| 비트 0 · 5 · 6 의 공개 이름 (`NORMALIZED` · `RESERVE_1MB` · `RESERVE_16MB`), 비트 24 뜻 | 앞 셋은 기억으로만 맞춘 `[추측]`, 24 는 `[공백]` | 번호 없음 (Q63b 가 단서를 줄 수 있음) |
| `PackageDependencyData` · `CurrentDirectores` 를 **왜** 안 고치나 | 코드로 못 정함 `[추측: 만드는 쪽이 0 으로 두는 칸]` | 번호 없음 — 모른다 |
| CHPE 속성 (`+0x11` 비트 7) 이 무슨 뜻인가 | PDB 열거형 이름만 있음 `[공백]` | 번호 없음 — 모른다 |
| "SCP" 약어 풀이 | 코드에 풀이 없음 `[공백]` | 번호 없음 — 모른다 |

---

## 5. 더 읽기

- run 문서: [`run104-params-block.md`](run104-params-block.md) — 호출 지점 표, (a) `Flags` 비트 표 · Environment 자리 표, (b) `MiReferenceCfgVad` 쓰기 · ScpCfg 전역 표, (c) `+0x11` 표, (e) 꼴 검색, (f) 작은 것 셋, 사진 로그 대조, 호출 관계 그림, 정정
- 결과서: [`_workspace/sast/104_params-block_result.md`](../../_workspace/sast/104_params-block_result.md) — 1 (a) "읽는 법" · 비트 표 · Environment 표, 2 (b-1) · (b-2) (ntdll 표 `0x68`B 의 칸 원본), 3 (c), 4 (e) 꼴 표, 5 (f-1) ~ (f-3), "지금 우리 (PE_emulator) 맞나"
- 요청서: [`_workspace/sast/104_params-block.md`](../../_workspace/sast/104_params-block.md) (질문 4 의 "사용자가 줄 수 있는" 은 반대 — 2.3)
- flow.md: [그림 1](flow.md) (`p3` → `p11` · `s1` · `p10`) · [2.1](flow.md) `PspBuildCreateProcessContext` · `MiMapProcessExecutable` 4번 · `PspCopyAndFixupParameters` · `PspPrepareSystemDllInitBlock` · 짝 표 · [2.3](flow.md) 부팅 절 · `PspInitializeThunkContext` · [2.4](flow.md) `LdrpInitialize` · 4장 run104 줄 · 4.1 그림 1 nt 줄
- 짐 지도: [`baggage.md`](baggage.md) 2장 합 · 3.2 · 3.9 · 4장 짝 · 7.4 · 7.6.4 · 7.36
- 앞 이야기:
  - [run32 해설](../02_sprint/run32-params-initblock-해설.md) — 개업 서류 세 정거장, Fixup, 표준 핸들, 업무 수첩, 비트 29 (이번 결과로 더 깊어짐, 고칠 곳 없음)
  - [run16 해설](../02_sprint/run16-thunk-context-해설.md) — `Rdx` = `PspSystemDlls[0]` 칸 `+0x20`, 부팅 때 벽 주소록
  - [run48 해설](run48-system-dll-boot-해설.md) — 세트 대장 `PspSystemDlls` 와 카드 `PspNativeSystemDllData`
  - [run63 해설](run63-cfg-func-table-해설.md) — 별채 (확장 쪽) · 본뜬 판 네 장 (ScpCfg 쪽)
  - [run50 해설](run50-eprocess-create-해설.md) — test.exe `MitigationFlags 0x20` 의 출처
  - [run60 해설](run60-func-table-mrdata-해설.md) — 별지 요청서 (속성), `PspCopyUserAttributes`
  - [run44 해설](run44-baggage-snap-해설.md) — 사진 K1 · P1a
