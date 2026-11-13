# run91 bcrypt-init — 해설

> 이 글은 [run91 문서](run91-bcrypt-init.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q169** — PE_emulator 요청 31 (bcryptprimitives.dll `DllMain` attach 가 바깥에서 꼭 받아야 하는 답) (`_workspace/plan.md`, 이번에 **끝**)
  - **한 바퀴로 끝났다.** 사용자 결정 (2026-11-13): Q104 VM 단계 앞 멈춤 자리에서 끼워 **지금 먼저**, 정적만.
  - 다음 항목: **[멈춤 — Q104 VM 단계 앞, 사용자 확인 대기]**.
  - 이번에 남은 것이 간 곳 (4절):
    - 정적 → 새 **Q170** (작고 후순위인 것들)
    - 동적 → 새 **Q171** (test.exe 로는 못 본다. bcryptprimitives 를 올리는 샘플 고르기는 **사용자 결정**)
- 근거:
  - run 문서 [`run91-bcrypt-init.md`](run91-bcrypt-init.md) (가장 중요)
  - 요청서 [`_workspace/sast/87_bcrypt-init.md`](../../_workspace/sast/87_bcrypt-init.md) · 결과서 [`87_bcrypt-init_result.md`](../../_workspace/sast/87_bcrypt-init_result.md)
    - 판정 **분석성공**. 요청서 질문 5개 모두 `.asm` 줄로 답했다.
    - bcryptprimitives 를 **처음 analyze** 했다 (PDB `E63663F36C3ABFBF74EACD102DC13DBB1`, 함수 1209, 지도 `_map/entry_*` 285 함수 · 깊이 10).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 근거 로그 · 흐름도 앞 글 · **3장 새 절** · 4장 · 5장. **그림은 안 바뀜** (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): 새 짐 칸 없음, 근거만 (마흔일곱째판)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없다.
  - `[관찰]` 로 적힌 값은 flow-writer 가 run44 사진 로그를 **다시 읽은 것** 이다 (2.13).

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 bcryptprimitives 를 안 올린다.**
   - run44 사진의 초기화 끝 (P2) 모듈은 6개다 — `test.exe` · `ntdll.dll` · `KERNEL32.DLL` · `KERNELBASE.dll` · `dll1.dll` · `dll2.dll` `[관찰]`.
   - sample 소스에도 암호 · 난수 API 글자가 없다 `[소스]`.
   - 그래서 이번 이야기는 **"이 DLL 을 올리는 다른 프로세스라면"** 이다. 흐름도 **그림은 그대로** 다.
2. **"로그" 는 진짜 Windows 로그가 아니다.**
   - 요청서의 syscall 차례는 PE_emulator 가 흉내 낸 실행의 로그다 `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]`.
   - 이 글의 결론은 전부 코드를 읽은 것 `[정적]` 이다. 진짜 Windows 에서 실행해 본 건 없다 (→ Q171).
3. **이름이 둘이다: Ghidra `entry` = PDB `DllMain`.**
   - 이 DLL 의 PE 진입점 (rva `0x433c0`) 을 Ghidra 는 `entry` 로, PDB 는 `DllMain` 으로 부른다 (`pdb_aliases.py --where` 로 확인).
   - 이 글은 `DllMain` 으로 쓴다. WinDbg 에서 `entry` 로 멈춤점을 걸면 안 될 것이다 `[추측]` (2.15).
4. **결과서와 run 문서가 다르게 적은 곳이 세 줄 있다 → run 문서를 따른다.**
   - 가장 중요한 하나: `GetCngHandle +0xa2 JS` 가 뛰는 곳은 **`+0xb8`** 다. 결과서의 `+0xc0` 은 없는 줄이다.
   - 셋 다 결론은 같다 (2.14).
5. **비유 하나 주의 — 핸들은 "손잡이" 로 부른다.**
   - flow.md 3장 새 절의 비유는 핸들을 "창구 **번호표**" 라고 불렀다.
   - 그런데 이 해설 시리즈에서 "번호표" 는 이미 **시스템 콜 번호** 다 ([run29 해설](../02_sprint/run29-continue-start-해설.md)).
   - 그래서 이 글은 run9 부터 쓴 **손잡이** 로 부른다. 그림만 다르고 사실은 같다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, [핸들](../02_sprint/run9-create-user-process-해설.md#핸들) = **손잡이**
- 전원 켜기 = DLL 시작 함수 (`DllMain`) 부르기, 이유 1 = 프로세스에 처음 붙음 → [run25 해설](../02_sprint/run25-dll-entry-해설.md) 2.3
- `0xc0000142` = DLL 초기화 실패 (`DllMain` 이 FALSE 면 로더가 쓰는 값), "최소 조건 표" 라는 꼴 → [run85 해설](run85-user32-init-해설.md) 2.6 · [run86 해설](run86-gdi32-init-해설.md)
- CFG 간접 호출 = **교환원** 을 거쳐 부르기 → [run20 해설 2.9](../02_sprint/run20-kernel32-load-해설.md)
- `LOCK CMPXCHG` = **발판** 에 먼저 오른 한 명만 "0 이면 고친다" → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.6
- `\KnownDlls` = 동네 **공용 창고**, KnownDll 길 = 공용 창고에서 꺼내 오기 → [run21 해설](../02_sprint/run21-load-dll-internal-해설.md)
- KUSD = 관리사무소가 붙여 두는 **공용 게시판** → [run18 해설 2.11](../02_sprint/run18-proc-init-setup-해설.md)
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = **번호 붙은 주문서** → [run66 해설 2.7](run66-apphelp-cache-해설.md), 상담실에 연결 줄 놓고 주문서 넣기 → [run83 해설](run83-console-kernelbase-해설.md)
- 주사위 상자 (ntdll `RtlRandom`), 씨앗 → [run61 해설](run61-random-ex-init-해설.md), CPU 주사위 (`RDRAND`) → [run59 해설 2.5](run59-init-leftovers-해설.md)
- 봉인 스티커 (`_DllMainCRTStartupForGS2`) → [run59 해설 2.3](run59-init-leftovers-해설.md) · [run68 해설 2.10](run68-dll-init-rest-해설.md)
- ETW = **방송국**, 등록 = **채널** 올리기, "Once" → [run62 해설](run62-tp-etw-init-해설.md) 2.6 · 2.7
- 엔클레이브 (따로 떼어 낸 보호 구역, **금고방**), VBS → [run48 해설](run48-system-dll-boot-해설.md) 2.5 (vertdll "새 말 둘") · 2.12
- `FORCE_INTEGRITY` (`DllCharacteristics & 0x80`) 가 나온 곳 → [run38 해설 2.8](../02_sprint/run38-map-module-해설.md)
- 사진 P1a · P2, raw 읽기 → [run44 해설 2.8](run44-baggage-snap-해설.md)

이번에 처음 나오는 말 — **출력 칸 (핸들 칸)**, **캐시 (`g_hCNGDriver`)**, **`\Device\CNG`**, **환경값 (`g_TrustedEnvironment`) · 트러스트렛 · IUM**, **점프 표**, **`SetLastError`**, (짧게) **WPP**, **lsass** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, ntdll = 가게 안 **관리실**.
- DLL = 가게에 들이는 **건물**. 시작 함수 (`DllMain`) = 건물 **전원 켜기**. DLL 전역 = 건물마다 가진 **수첩** (ntdll 전역 = 관리실 **칠판** 과 구별).
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = 상담실에 넣는 **번호 붙은 주문서**. 핸들 = **손잡이**.
- 이번에 새로 붙는 그림:
  - **bcryptprimitives = "암호 부품 건물"**. 전원을 켤 때 건물 안 **자기 주사위 상자** 를 처음 채운다.
    - run61 의 주사위 상자 (ntdll `RtlRandom`) 와는 **다른 상자** 다. run59 의 "CPU 주사위" 와도 다르다. 건물마다 따로 가진 상자다.
  - 상자를 채울 **씨앗 48B** 는 관리사무소 건물 안 **난수 상담실** (`\Device\CNG`) 에서 받아 온다.
  - 받는 순서는 두 걸음이다.
    1. 신청 창구 (`NtOpenFile`) 에 **바구니** 를 내밀며 "상담실 문 **손잡이** 하나 주세요".
    2. 그 손잡이로 상담실에 **주문서** (IOCTL `0x390008`) "씨앗 48개 주세요" 를 넣는다.
  - 받은 손잡이는 건물 벽 **손잡이 걸이** (캐시) 에 걸어 둔다. 다음부터는 창구에 안 가고 걸이에서 꺼낸다.

### 한 줄로 먼저 — 무엇을 얻었나

**이번 글의 중심은 이 한 줄이다.**

> **status 는 "성공" 인데 핸들 칸이 비어 있다 → 코드는 "못 열었다" 와 똑같이 움직인다 → 같은 장치를 5번 다시 연다 → 포기 → `DllMain` FALSE.**

| 요청 31 질문 | 답 (run 문서 그대로) |
|---|---|
| 1 여는 이름 · 5번 조건 · 칸이 비었을 때 · FALSE 자리 | 이름은 **`\Device\CNG`** (`\Device\KsecDD` 아님). 값 셋 (`0x100001` · 7 · `0x20`) 은 로그와 같다. 5번 = **같은 이름** 을, 시드 함수가 0 을 돌려줄 때마다 다시. 칸이 비면 status 0 이어도 NULL → 실패와 같은 길 → 5번 → FALSE. FALSE 자리는 셋, 일반 프로세스는 ② 하나뿐 `[정적]` (2.3 ~ 2.6, 2.10) |
| 2 연 뒤 syscall · TRUE 조건 | `NtDeviceIoControlFile` 코드 `0x390008`, 입력 없음, 출력 `0x30B`. **성공 status 만 본다** (출력 내용 안 봄) `[정적]` (2.7 · 2.8) |
| 3 class `0x56` 결과를 쓰나 | 쓴다 (첫 DWORD → 환경값). 그런데 **26100.9457 커널은 이 class 가 없다** (`0xc0000003`) → 진짜는 class 0 을 한 번 더 부른다. 결과는 같다 `[정적]` (2.9) |
| 4 `NtTraceControl` × 3 | 반환을 **아무도 안 본다** `[정적]`. 셋의 구성 1 + 1 + 1 은 `[추측]` (2.11) |
| 5 최소 조건 표 | 바깥이 맞출 것은 **둘** — `NtOpenFile` 이 0 아닌 핸들을 쓰고, 그 핸들로 IOCTL 이 성공 `[정적]` (2.8) |
| (덤) 로그 끝 `NtAllocateVirtualMemory` · `NtClose` × 2 | attach 코드에 **없다** `[정적]`. FALSE 뒤 로더 정리로 보인다 `[추측]` (2.12) |
| (덤) test.exe | 이 DLL 을 안 올린다 `[관찰]` run44 (2.13) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**DLL 전원 켜기** 구간이다 (flow.md 2.7). 다만 test.exe 에는 없는 건물이라 **옆길** 이다. run85 · run86 (user32 · gdi32) 과 같은 자리다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ (2.6) import DLL 들이기 — test.exe 는 dll1 · dll2
 │         │     (bcryptprimitives 를 올리는 프로세스라면: \KnownDlls 공용 창고 길 [추측: 실행 안 봄])
 │         └─ (2.7) DLL 전원 켜기 — LdrpCallInitRoutine
 │               ├─ (test.exe) dll2 → dll1
 │               └─ (bcryptprimitives 를 올리는 프로세스라면) bcryptprimitives!DllMain (이유 1)   ← 이번 (run91)
 │                     FALSE 면 → 로더가 DLL 을 내림 → 0xc0000142
 └─ … → NtContinue (은퇴) → exe 진입점 → main
```

- flow.md 3장 새 절: "bcryptprimitives 를 올리는 프로세스에서 2.7 의 `LdrpCallInitRoutine` 이 부르는 진입점 (이유 1) 이 하는 일".
- 요청서 배경: `DllMain` 이 FALSE 면 로더가 DLL 을 내리고, `LoadLibraryExW` 로 올리던 중이면 0 을, 초기화 중이면 `0xc0000142` 를 본다.

### 왜 이 질문이 지금 왔나

- PE_emulator 요청 31 이다.
- PE_emulator 샘플 **1183개** (r01 병목의 **14 %**) 가 같은 자리에서 막힌다.
  - bcryptprimitives 를 KnownDll 섹션으로 올린 뒤 `DllMain` (이유 1) 이 **FALSE** → 로더가 내림.
- 사용자 결정 (2026-11-13): Q104 VM 단계 앞 멈춤 자리에서 끼워 **지금 먼저**, 정적만.

### 무엇이 궁금했나

요청서 질문 다섯.

1. **attach 길과 `NtOpenFile`** — 여는 객체 이름, **5번 되풀이하는 조건**, **status 0 인데 핸들 칸이 안 쓰이면 어디로 가나**, FALSE 로 가는 자리 전부.
2. **연 뒤 syscall** — `NtDeviceIoControlFile` 이면 IOCTL 코드 · 버퍼 · 읽는 칸. **TRUE 가 되려면 답이 무엇이어야 하나.** 로그 끝의 `NtAllocateVirtualMemory` · `NtClose` × 2 는 무엇인가.
3. **`NtQueryInformationProcess` class `0x56`** — 결과를 쓰나.
4. (작게) **`NtTraceControl` × 3** — 실패가 TRUE / FALSE 에 닿나.
5. **최소 조건 표**.

### 바퀴 전에 알던 것

- `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]` attach 안 syscall 차례:
  1. `NtQueryInformationProcess(-1, 0x56, …, 길이 0xb0)` — 에뮬레이터 기본 대답
  2. `NtTraceControl` × 3
  3. **`NtOpenFile` × 5** — 접근 `0x100001` · 공유 7 · 옵션 `0x20`. 에뮬레이터는 경로를 몰라서 **status 0 을 주지만 핸들 칸은 안 씀**. 여는 이름은 로그에 없음.
  4. `NtAllocateVirtualMemory` → `NtClose` × 2 → `DllMain` FALSE
  - `NtDeviceIoControlFile` 은 로그에 **안 나옴**.
- `[추측]` (요청서) 여는 장치는 `\Device\CNG` 또는 `\Device\KsecDD` 쪽.
- `[추측]` (요청서) `NtDeviceIoControlFile` 이 안 나온 건 "성공 + 핸들 없음" 이라 그 앞에서 갈렸기 때문일 수 있다.
- 우리 repo 에 bcryptprimitives 분석은 0 이었다.
- `[정적]` (run62) ntdll 의 ETW 등록 → `NtTraceControl` 길, 그리고 "Once" 는 프로세스 첫 등록 때 한 번.

---

## 2. 찾은 것

### 2.1 한눈에 — attach 줄기

run 문서 "호출 관계" 를 줄였다. 모두 `[정적]`.

- 트리는 **실행 차례** 대로다. 그래서 `+0x15f` 가 `+0xc8` 보다 위에 있다 (`+0x2d` 에서 함수 뒤쪽 `+0x15f` 로 가서 `GetTrustedEnvironment` 를 부르고, 이어서 `+0x33` 부터의 PE 머리 검사로 간다 — run 문서 차례 표 2 → 4).
- ★ = 이번 막힘이 지나가는 줄.

```
bcryptprimitives!DllMain (Ghidra entry, rva 0x433c0)   +0x1c / +0x1f  이유 1 만 아래로
 ├─ +0x15f  GetTrustedEnvironment                        (+0x2d 환경값 0 = 처음일 때)
 │     ├─ +0x4  IsEnclave  → +0x44 NtQueryInformationProcess(-1, 0x56, buf, 0xb0)
 │     └─ +0xd  IsTrustlet → +0x34 NtQueryInformationProcess(-1, 0, buf, 0x40)     (위가 0 일 때만)
 │  +0x49  PE 머리 검사 → FALSE ① (일반은 안 탐)
 ├─ +0xc8  WppInitUm → +0x6e EtwRegisterTraceGuidsW                                  반환 안 봄
 ├─ +0xcd  MsProviderStart (rva 0x436c4)                                              ★
 │     ├─ +0x64   TraceLoggingRegisterEx… → +0x55 EventRegister · +0x7c EventSetInformation   반환 버림
 │     ├─ +0x13b  InitUmRootRngState (rva 0x2b5dc)                                    ★
 │     │     └─ +0x7a  [CFG 간접] GetSeedFromKernelState (rva 0x46d00)                ★  0 이면 +0x129 ~ +0x137 다시 (5번)
 │     │           ├─ +0x1e  GetCngHandle (rva 0x46da8)                               ★
 │     │           │     ├─ +0x45  RtlInitUnicodeString(L"\Device\CNG")
 │     │           │     └─ +0x94  NtOpenFile(&[RBP+0x10], 0x100001, OA, IOSB, 7, 0x20) ★
 │     │           │            +0xa2 JS · +0xaa LOCK CMPXCHG 캐시 · +0xb8 핸들 칸 값 반환
 │     │           │  +0x29 JZ +0xa0   핸들 0 → 반환 0                                ★ (로그는 여기서 갈림)
 │     │           ├─ +0x3a  ntdll!RtlGetSystemGlobalData(1, &v, 8)     KUSD +0x250 읽기
 │     │           └─ +0x77  DeviceIoControl(h, 0x390008, 입력 없음, 출력 0x30B)
 │     │                    └─ kernelbase +0x6c NtDeviceIoControlFile
 │     │  +0x142  0 이면 → EBX = 0xc00000e5                                          ★
 │     └─ +0x14f  IsInLSAProcess   레지스트리 …\Lsa LsaPid                            (성공 길)
 │  +0xd4 JS +0x1be → +0x1c3 SetLastError(0x54f) → +0x1cf FALSE ②                    ★ (막힘)
 └─ +0x128  MOV EAX,1   TRUE
```

- 그 밖에 `DllMain` 이 부르는 것 — `+0x52 DisableThreadLibraryCalls` · `+0x69 _DllMainCRTStartupForGS2` (봉인 스티커) · `+0x6e SymCryptInitEnvWindowsUsermodeWin8_1nLater` · `+0x8e SafeAllocaInitialize` — 은 결과를 안 보거나 syscall 이 없다 (결과서 1-a).
- `MsProviderStart` 안 셀프테스트 셋 (`+0x126` · `+0x12b` · `+0x130`) 은 **계산만** 한다 (결과서).

### 2.2 한 줄씩 읽는 법

#### 줄 1: `└─ +0x94  NtOpenFile(&[RBP+0x10], 0x100001, OA, IOSB, 7, 0x20)` (`GetCngHandle` 아래)

- **누가**: bcryptprimitives 의 `GetCngHandle` (rva `0x46da8`) `[정적]`.
- **어디서**: `+0x94` 에서 부르고 `+0x9b` 로 돌아온다.
  - 이 줄은 `+0x34 JNZ` 를 **안 탔을 때만** 온다 — 캐시 `g_hCNGDriver` 가 0 일 때 (2.4).
- **누구를**: ntdll `NtOpenFile` → 신청 창구. bcryptprimitives 는 이걸 ntdll 에서 **직접** 가져온다 (결과서 임포트 표).
- **무엇을**: `\Device\CNG` 를 연다. 인자를 하나씩 보면 (결과서 1-b):
  - **첫째 `&[RBP+0x10]`** = 핸들을 받을 **칸의 주소**. ← 이번 이야기의 주인공 (2.3).
  - `0x100001` = 접근 (`SYNCHRONIZE | FILE_READ_DATA`)
  - `OA` = 이름 (`\Device\CNG`, 문자열 `.rdata` rva `0x8d0b8`) · 속성 `0x40` (`OBJ_CASE_INSENSITIVE`) · 루트 0. 크기 `0x30`.
  - 공유 `7`, 옵션 `0x20` (`FILE_SYNCHRONOUS_IO_NONALERT`)
  - **에뮬레이터 로그 값 (`0x100001` · 7 · `0x20`) 과 같다.** 결과서는 이걸 "로그와 코드가 같은 DLL" 이라는 증거로 썼다.
- **무엇을 위해**: 씨앗 주문서 (IOCTL) 를 넣을 **상담실 손잡이** 를 얻으려고.

**새 말 — `\Device\CNG`**

- 커널 이름 공간의 **장치 이름** 이다. 이 이름으로 열면 그 장치를 맡은 드라이버에게 말을 걸 수 있다 `[추측]` (일반적으로 알려진 설명).
- 결과서 · run 문서는 이 장치에 가는 IOCTL `0x390008` 을 처리하는 쪽을 `cng.sys` 로 적었다. 그 안은 **이번에 안 읽었다** (Q170, 후순위).
- 비유: 관리사무소 건물 안 **난수 상담실** 문패.

#### 줄 2: `└─ +0x7a  [CFG 간접] GetSeedFromKernelState` (`InitUmRootRngState` 아래)

- **누가**: `InitUmRootRngState` (rva `0x2b5dc`).
- **어디서**: `+0x7a` 에서 부르고 `+0x7f` 로 돌아온다.
- **누구를**: 이름으로 부르지 않는다. 건물 수첩 칸 **`g_pGetSeedFunction`** (`0x1800a42a0`) 에 적힌 함수를 **교환원 (CFG)** 을 거쳐 부른다.
  - 그 칸엔 바로 앞 `+0x5d` · `+0x64` 에서 `GetSeedFromKernelState` 를 적어 두었다 (일반 프로세스일 때).
  - 환경이 다르면 다른 함수를 적는다 — 환경 bit 8 (SGX 엔클레이브) 이면 `GetSeedFromRdrand` (`+0xd8`), `& 0x16` (IUM 쪽) 이면 `GetProcAddress(g_hIumBase, "GetSeedFromIumKernelState")` (`+0xf3 ~ +0x108`) (2.9).
- **무엇을**: `(시드 버퍼 0x30B, 0x30, &버전)` — 씨앗 48B 와 버전 8B 를 받아 온다.
- **무엇을 위해**: 주사위 상자 (`g_rootAesRngState`) 를 채울 씨앗.

**왜 지도에 이 선이 없었나**

- Ghidra 지도는 **이름으로 부르는 호출** 만 선으로 긋는다.
- 여기는 수첩 칸에 적힌 주소로 부르니 지도에 선이 없다.
- 결과서는 `InitUmRootRngState` `.asm` 에서 **칸에 넣는 값** 을 따라가 `GetSeedFromKernelState` 를 찾아 따로 뽑았다. `_calls.tsv` 에는 `indirect:cfg:GetSeedFromKernelState` 로 남아 있다.

### 2.3 ★ 먼저 이것 — 창구의 대답은 두 군데로 온다

이번 막힘을 이해하는 열쇠는 이것 하나다.

**새 말 — 출력 칸 (핸들 칸)**

- 함수가 결과를 **반환값 말고**, 부르는 쪽이 건넨 **메모리 칸에 써 주는** 인자다.
- `NtOpenFile` 의 대답은 그래서 **두 군데** 로 온다 `[정적]`:

| 대답 | 어디로 오나 | 뜻 |
|---|---|---|
| **status** | 반환값 (`EAX`) | "처리됐어요 / 안 됐어요" — 결과 **도장** |
| **핸들** | 첫 인자가 가리키는 칸 `[RBP+0x10]` | 실제로 받은 **손잡이** |

- 보통은 둘이 같이 온다. 성공 도장이 찍히면 칸에도 손잡이가 들어 있다.
  - 진짜 커널은 성공이면 늘 핸들을 쓰는 것으로 보인다 `[추측: 진짜 IopCreateFile 은 성공이면 늘 핸들을 씀 — 이번엔 안 읽음]` (run 문서).
- 그런데 PE_emulator 는 **도장만 찍고 칸은 안 썼다** `[관찰 — 에뮬레이터 로그]`.
  - 경로를 모를 때 "status 0 + 칸 비움" 을 주는 엔진 동작이다 (요청서 배경).
  - 이건 진짜 커널에는 없는 대답이다 `[추측]` (run 문서 "PE_emulator 확인거리").

비유:

- 창구에 **바구니** 를 내밀며 "상담실 손잡이 주세요" 한다.
- 진짜 직원은 "처리됐어요" 하면서 바구니에 손잡이를 넣어 준다.
- 흉내 직원은 "처리됐어요" **도장만** 찍고, 바구니는 빈 채로 돌려준다.

그럼 건물 직원 (`GetCngHandle`) 은 무엇을 믿을까? 다음 절이 그 답이다.

### 2.4 ★ `GetCngHandle` 안 — "도장은 성공, 바구니는 빈 채" 를 한 줄씩

**새 말 — 캐시 (`g_hCNGDriver`)**

- `g_hCNGDriver` (`0x1800a42b0`) 는 한 번 받은 `\Device\CNG` 핸들을 **넣어 두는 전역 칸** 이다 `[정적]`.
- 0 이 아니면 창구에 안 가고 그 값을 바로 쓴다 (`+0x31` · `+0x34 JNZ +0xbc`).
- 비유: 건물 벽의 **손잡이 걸이**. 걸려 있으면 그걸 들고 가고, 비어 있으면 창구에 간다.

이제 에뮬레이터 실행을 그대로 따라간다. 처음엔 걸이도 비어 있다 (0).

| 자리 | 코드가 하는 일 `[정적]` | 바구니 `[RBP+0x10]` | 걸이 `g_hCNGDriver` |
|---|---|---|---|
| `+0x10` | 걸이 값을 읽는다 (`RBX`) | — | 0 |
| **`+0x25`** | **바구니에 걸이 값을 미리 넣어 둔다** | **0** | 0 |
| `+0x31` · `+0x34` | 걸이가 0 이 아니면 바로 돌려줌 (`JNZ +0xbc`) — 0 이라 안 탐 | 0 | 0 |
| `+0x45` | `RtlInitUnicodeString(L"\Device\CNG")`, OA · IOSB 준비 | 0 | 0 |
| **`+0x94`** | **`NtOpenFile(&바구니, …)`** — 엔진은 status 0, 바구니 안 씀 `[관찰 — 에뮬레이터 로그]` | **0 그대로** | 0 |
| `+0xa0` · `+0xa2` | `TEST EAX,EAX` · `JS` — status 가 음수인가? 아니다 → 안 뜀 | 0 | 0 |
| `+0xa4` | 바구니 값을 꺼낸다 (`RCX` = 0) | 0 | 0 |
| `+0xa8` · **`+0xaa`** | **`LOCK CMPXCHG` — "걸이가 0 이면 `RCX` 를 건다"**. 걸이 0 = 기대값 0 → "성공" | 0 | **0 위에 0** |
| `+0xb6` | `JNZ` (CMPXCHG 에 졌나?) — 이겼으니 안 탐 | 0 | 0 |
| **`+0xb8`** | **바구니 값을 돌려줄 값으로 (`RBX`)** → `+0xcf` 반환 | 0 | 0 |

결과: **`GetCngHandle` 은 NULL (0) 을 돌려준다. 걸이도 여전히 비어 있다.**

여기서 눈여겨볼 점 셋.

1. **코드는 status 만 본다.**
   - `+0xa0` · `+0xa2` 가 보는 건 status 뿐이다. 바구니가 비었는지는 **따로 묻지 않는다**.
   - 그리고 돌려주는 값은 **바구니 값** 이다 (`+0xb8`).
   - 그러니 "도장은 성공" 이어도 "바구니가 비었으면" 돌려주는 건 0 이다.
2. **"성공" 처럼 보이는 일이 두 번 일어난다.**
   - status 0 → "열었다".
   - `LOCK CMPXCHG` 도 기대값 (0) 과 걸이 값 (0) 이 같으니 "성공" → 하지만 **0 위에 0 을 쓴 것** 이라 아무것도 안 걸렸다.
   - 둘 다 겉으로는 성공인데, 실제로 손에 쥔 건 없다.
3. **실패해도 같은 자리로 온다.**
   - status 가 음수면 `+0xa2 JS` 가 바로 **`+0xb8`** 로 뛴다 → 역시 바구니 값 (0) 을 돌려준다 (run 문서 차례 표 m).
   - 그래서 **"성공인데 빈 바구니" 와 "실패" 는 이 함수 밖에서 보면 똑같다** — 둘 다 NULL. 다른 점은 없다 `[정적]` (결과서 1-d 끝).

`LOCK CMPXCHG` 의 원래 쓰임 (두 스레드가 동시에 열 때):

- 발판에 먼저 오른 스레드만 걸이에 자기 손잡이를 건다.
- 진 스레드는 `+0xb6 JNZ` → `+0xd1 ~ +0xe1` 에서 **자기 손잡이를 닫고** (`+0xd5 CloseHandle`) 이긴 쪽 손잡이를 돌려준다 `[정적]`.
- 이번 경우엔 경쟁이 없으니 이 갈래는 안 탄다.

비유로 다시:

- 건물 직원이 창구에서 "처리됐어요" 도장을 받고, **바구니 안은 안 들여다보고** 돌아온다.
- 걸이에 "바구니 안의 것" 을 건다 — 빈손이니 아무것도 안 걸린다.
- 그리고 상담실 앞에 가서 손을 펴 보니 빈손이다.

**왜 코드가 바구니를 따로 확인 안 하나** `[추측]`

- 근거: 진짜 커널은 성공이면 늘 핸들을 쓰는 것으로 보인다 `[추측]` (run 문서).
- 그래서: 코드가 허술한 게 아니라, "도장이 성공이면 바구니도 찼다" 는 창구의 약속을 믿는 꼴로 보인다.
- 아직: 진짜 커널 쪽 (`IopCreateFile`) 은 이번에 안 읽었다. 진짜 핸들 모양은 Q171 의 `GetCngHandle+0x9b` 멈춤점에서 본다.

### 2.5 ★ 왜 5번인가 — 시드 함수 다시 부르기

#### 0 이 위로 올라가는 첫 두 계단

1. **`GetSeedFromKernelState`** `[정적]`
   - `+0x1e` 에서 `GetCngHandle` 을 부르고 (`+0x23` 돌아옴) 결과를 `RDI` 에 둔다.
   - `+0x26 TEST` · **`+0x29 JZ +0xa0`** — 핸들이 0 이면 `+0xa0 JMP +0x8f` → **0 (BOOL FALSE)** 을 돌려준다.
   - 즉, 핸들이 비면 **IOCTL 을 부르기도 전에** 돌아선다. 이게 로그에 `NtDeviceIoControlFile` 이 없는 까닭이다 (아래).
2. **`InitUmRootRngState`** — 다시 부르기 고리 `[정적]`

| 자리 | 하는 일 |
|---|---|
| `+0x6b ~ +0x7a` | 인자 준비 → `+0x7a` 시드 함수 부르기 (2.2 줄 2) |
| `+0x81` · **`+0x83 JZ +0x129`** | 0 이면 다시 부르기 갈래로 |
| **`+0x129 INC EBX`** | 실패 횟수 +1 (`EBX` 는 0 에서 시작) |
| **`+0x12b CMP EBX,5`** · **`+0x12e JNC +0xbd`** | 5 가 되면 포기 → `+0xbd` 에서 **0 을 돌려줌** |
| `+0x130` · `+0x137 JMP +0x6b` | 아니면 수첩 칸 `g_pGetSeedFunction` 을 다시 읽고 처음으로 |

- 그래서 시드 함수를 **5번 부르고** (`EBX` = 0, 1, 2, 3, 4 일 때) 포기한다.
- 매번 `GetCngHandle` 을 지나는데, 걸이가 계속 0 이다 → **매번 `NtOpenFile` 을 다시 한다** → 로그의 `NtOpenFile` × 5.
- **이름 5개를 차례로 여는 게 아니다.** 같은 `\Device\CNG` 를 5번 연다. 핸들 값이나 status 를 직접 비교해서 다시 여는 것도 아니다 — **시드 함수의 BOOL** 하나만 본다 (결과서 1-c).

#### "5번" 이 알려 주는 것 — 경우 넷을 나란히

다시 부르기는 같아도, **어디서 막혔나** 에 따라 로그 모양이 달라진다 `[정적]` (run 문서 "PE_emulator 쪽" · 최소 조건 표 6).

| 경우 | `NtOpenFile` | `NtDeviceIoControlFile` | 끝 |
|---|---|---|---|
| A. status 0 + **바구니 비움** (이번) | **5번** | **0번** | FALSE |
| B. status 음수 (열기 실패) | 5번 | 0번 | FALSE |
| C. 손잡이는 받음, IOCTL 이 음수 | **1번** (걸이가 차서 그 뒤엔 안 감) | 5번 | FALSE |
| D. 손잡이 받음 + IOCTL 성공 | 1번 | 1번 | 시드 OK → 계속 |

- C 를 보면 알 수 있다: 한 번이라도 0 아닌 핸들을 받으면 걸이가 차서, **그 뒤 시도는 `NtOpenFile` 없이 `NtDeviceIoControlFile` 만** 다시 한다 (`GetCngHandle +0x34 JNZ +0xbc`).

#### 로그를 거꾸로 읽기 — 단계별로

1. 근거 `[관찰 — 에뮬레이터 로그]`: `NtOpenFile` 5번, 모두 status 0. `NtDeviceIoControlFile` 0번.
2. 근거 `[정적]`: `NtOpenFile` 은 걸이가 0 일 때만 나간다 (`+0x34`).
3. 그래서: 5번 내내 걸이가 0 이었다 = **0 아닌 핸들이 한 번도 안 걸렸다.**
4. 근거 `[정적]`: status 0 이면 `+0xaa` 에서 바구니 값을 건다.
5. 그래서: 바구니 값이 0 이었다 = **엔진이 바구니를 안 썼다.** (status 0 이라 B 도 아니다.)
6. run 문서: "코드로 설명되는 길은 이것뿐이다" `[정적]`.

그리고 요청서의 짐작 하나가 확인됐다.

- 요청서: "`NtDeviceIoControlFile` 이 안 나온 건 그 앞에서 갈렸기 때문일 수 있다" `[추측]`.
- 이번: `GetSeedFromKernelState +0x29 JZ` 가 IOCTL (`+0x77`) 보다 앞에서 돌려보낸다 → **`[정적]`**.

### 2.6 ★ 0 이 FALSE 가 되기까지 — 세 층 더 올라간다

`InitUmRootRngState` 가 0 을 돌려준 뒤 `[정적]`:

1. **`MsProviderStart`**
   - `+0x13b` 에서 부르고 `+0x140` 으로 돌아옴. `+0x142 JNZ` 를 **안 탄다** (0 이니까).
   - → `+0x144 R9D=0xa1` → `+0x14a JMP +0xec` → **`EBX = 0xc00000e5`**.
   - → `+0x104 DebugTraceError` (기록만, `EBX` 안 건드림) → `+0x109 JMP +0x34c` → **반환 `0xc00000e5`**.
   - `0xc00000e5` 는 맨 앞 비트가 1 인 음수 = 실패 status 다.
2. **`DllMain`**
   - `+0xd2 TEST EAX,EAX` · **`+0xd4 JS +0x1be`** — 음수면 실패 갈래로.
   - → `+0x1be MOV ECX,0x54f` → **`+0x1c3 SetLastError(0x54f)`** → **`+0x1cf XOR EAX,EAX`** (EAX = 0) → `+0x1d1 JMP +0x12d` → **FALSE**.
3. **로더 (ntdll)**
   - `DllMain` 이 FALSE 면 로더가 DLL 을 내린다. 초기화 중이면 `0xc0000142` (DLL 초기화 실패) ([run85 해설 2.6](run85-user32-init-해설.md)).
   - run 문서는 이 자리를 `LdrpInitializeNode +0x298` (`0xc0000142`) 로 적었다 (Q170 ①).

**새 말 — `SetLastError`**

- 스레드마다 있는 "**마지막 오류 번호**" 칸에 번호를 적어 두는 API 다. 부른 쪽이 나중에 `GetLastError` 로 읽는다 `[추측]` (일반적으로 알려진 설명).
- 이번 번호 `0x54f` 를 결과서는 `ERROR_INTERNAL_ERROR` 라고 적었다.
- 비유: 전원 켜기에 실패한 건물 문에 붙이는 **"고장" 메모**.

비유로 한 줄:

- 빈손으로 다섯 번 줄을 섰다 → 주사위 상자를 못 채움 → "가게 준비 실패" (`0xc00000e5`) → 건물 문에 "고장" 메모 → **전원 켜기 실패 (FALSE)** → 관리실이 건물을 다시 내린다.

### 2.7 열고 나서 할 일 — IOCTL `0x390008` 은 "성공" 만 본다

손잡이를 제대로 받았다면 `GetSeedFromKernelState` 는 이렇게 이어간다 `[정적]`.

1. **`+0x3a` `ntdll!RtlGetSystemGlobalData(1, &v, 8)`** (`+0x41` 돌아옴)
   - **syscall 이 없다.** 공용 게시판 (KUSD) `+0x250` `RNGSeedVersion` 8B 를 읽어 온다 (ntdll `+0x14`).
   - 음수가 오면 `+0x4a INT 0x2c` — 일부러 멈추는 **크래시** 다. 단 ID 1 + 길이 8 이면 음수 길이 없다 (결과서).
2. **`+0x77` `DeviceIoControl(h, 0x390008, NULL, 0, buf, 0x30, &n, NULL)`** (`+0x7e` 돌아옴)
   - `+0x6a MOV EDX,0x390008` 에서 주문서 번호를 싣는다.
   - **입력 없음, 출력 = 시드 버퍼 `0x30B`**.
   - kernelbase `DeviceIoControl` 은 장치 종류가 9 가 아니라서 (`0x390008` 은 종류 `0x39`) **`+0x6c NtDeviceIoControlFile`** 로 간다 (run 문서 보탬 ⑤).
3. **`+0x83 ~ +0x8c`** — `DeviceIoControl` 이 0 이면 그대로 0. 아니면 `*ver = v` 하고 0 아닌 값을 돌려준다.

여기서 중요한 것: **출력 내용을 아무도 안 본다** `[정적]`.

- `GetSeedFromKernelState` 는 `DeviceIoControl` 의 BOOL 만 본다.
- kernelbase 는 성공이면 `*lpBytesReturned = IOSB.Information` 을 쓰지만 (`+0x8d ~ +0x9e`), `GetSeedFromKernelState` 는 **안 읽는다**.
- 그 뒤 `AesRNGState_init` 은 시드 48B 를 그대로 씨앗으로 쓰고 **길이만** 본다 (결과서 2 표 3).
- 그래서 출력이 0 으로 남아도 코드는 통과한다. 다만 결과서는 **난수 48B 를 채우는 걸 권했다** (씨앗이 0 이 되니까).

kernelbase 쪽 갈래 둘 (run 문서 보탬 ⑤):

- status 음수 → `+0x87 JS +0x192` → `+0x154 BaseSetLastNTError` → FALSE → 다시 부르기 (2.5 경우 C).
- status `0x103` → `+0x7f JZ +0x1d6` → **`+0x1de NtWaitForSingleObject(핸들)`** 에 갔다 온다. 엔진은 **피하는 게 좋다** (run 문서 최소 조건 표 6).

IOCTL 코드 풀이 (결과서 "다음 요청 후보" 5): 장치 종류 `0x39` · 함수 2 · 버퍼 방식 · 접근 제한 없음 `[정적]`. 공개 자료의 `IOCTL_KSEC_RANDOM_FILL_BUFFER` 와 같은 값으로 기억 `[추측]`.

비유: 손잡이로 상담실 문을 열고 "씨앗 48개" 주문서를 넣는다. 상담실이 "**처리됐어요**" 라고만 하면 건물 직원은 봉투 안을 **세어 보지 않고** 상자에 붓는다.

### 2.8 ★ TRUE 최소 조건 — 바깥이 맞출 것은 둘

일반 프로세스 기준이다 (lsass 아님, 환경값 1). run 문서 "최소 조건 표" 9줄을 셋으로 묶었다. 모두 `[정적]`.

**꼭 맞출 것 둘**

| # | syscall | 입력 | 기대 답 |
|---|---|---|---|
| 1 | **`NtOpenFile`** | `\Device\CNG`, `0x100001`, 공유 7, 옵션 `0x20`, 속성 `0x40`, 루트 0 | **status ≥ 0 이고 `*FileHandle` ≠ 0** |
| 2 | **`NtDeviceIoControlFile`** | 1 의 핸들, `0x390008`, 입력 NULL/0, 출력 `0x30B` | **status ≥ 0** (출력 내용 · 길이 · `IoStatus.Information` 안 봄. 난수 48B 권장. `0x103` 은 피할 것) |

**아무 답이나 되지만, 하나씩 조심할 것**

| syscall · 칸 | 조심할 것 |
|---|---|
| `NtQueryInformationProcess` (class `0x56`, 그리고 실패면 class 0) | class 0 답의 `+0x38` **bit 7 은 0** (켜지면 트러스트렛 갈래 → FALSE). class `0x56` 이 성공이면 첫 DWORD 가 1 · `0x10` · `0x11` 이 **아니어야** (2.9) |
| `NtTraceControl` × 3 ~ | 없음 (반환 안 봄, 2.11) |
| 레지스트리 (`…\Control\Lsa` `LsaPid`) | `LsaPid` 가 **내 PID 와 같지만 않으면** 됨 |

**syscall 이 아니지만 메모리에 있어야 할 것**

| 칸 | 조심할 것 |
|---|---|
| KUSD `+0x250` (`RtlGetSystemGlobalData` 가 읽음) | 페이지가 **읽혀야** 함. 값은 아무거나 (run44 사진 `0xc`) |
| 제 PE 머리 (`[base+0x3c]` · `[base+e_lfanew+0x5e]`) | 메모리에 올라와 있고 `& 0x80` ≠ 0 (파일 값 `0x41e0` 은 켜짐) — FALSE ① (2.10) |

**새 말 — lsass**

- Windows 의 로그인 · 보안 담당 프로세스로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- `IsInLSAProcess` 는 레지스트리 `HKLM\System\CurrentControlset\Control\Lsa` 의 `LsaPid` 가 **내 PID 인가** 만 본다 (결과서). 키 열기 실패 · 값 읽기 실패 · 다른 PID → 모두 "lsass 아님" (0) → 성공 길.
- 이 DLL 은 lsass 일 때만 추가 검사 (`AlgorithmCheck` 등) 를 한다. 에뮬레이터에선 일어날 일이 없다 (결과서).

이걸 다 맞추면 attach 는 `+0x128 MOV EAX,1` (TRUE) 로 끝난다. 그때 남는 것 `[정적]` (결과서):

- `g_hCNGDriver` ≠ 0 (**열린 핸들 1개** — 걸이에 손잡이가 걸림)
- `g_rngState` (`0x1800a42a8`) = 1, `g_fProviderStarted` = 1
- `g_rootAesRngState` 채워짐 (주사위 상자가 참)
- `g_TrustedEnvironment` = 1

### 2.9 class `0x56` — 진짜 커널엔 이 칸이 없다

#### 어떻게 부르나

- `IsEnclave +0x44` (돌아옴 `+0x4b`) → `NtQueryInformationProcess(-1, 0x56, buf, 0xb0, NULL)` `[정적]`.
  - 버퍼는 부르기 전에 `+0x26` 에서 `0xb0` 만큼 0 으로 지운다.
  - 작은 읽기 요령: class 번호는 `+0x40 LEA EDX,[R9-0x5a]` 로 만든다. `R9D` 가 길이 `0xb0` 이니 `0xb0 − 0x5a = 0x56`. 그래서 `.asm` 에 `0x56` 이라는 숫자가 **바로 안 보인다**.
- class `0x56` 의 이름은 PDB 에 열거형이 없어 못 밝혔다. 결과서는 공개 헤더 기억으로 `ProcessEnclaveInformation` 이라고 적었다 `[추측]`.

#### 결과를 쓰나 — 쓴다

**새 말 — 환경값 (`g_TrustedEnvironment`) · 트러스트렛 · IUM**

- `g_TrustedEnvironment` (`0x1800a42c0`) 는 이 DLL 이 "**나는 어떤 곳에서 돌고 있나**" 를 적어 두는 칸이다 `[정적]`. 처음엔 0, `GetTrustedEnvironment` 가 채운다.
- 엔클레이브 = 따로 떼어 낸 보호 구역 ([run48 해설](run48-system-dll-boot-해설.md), **금고방**). 트러스트렛 · IUM = VBS 보호 구역 쪽에서 도는 특별한 프로세스로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유: 건물이 전원을 켜기 전에 "**여기가 보통 가게인가, 금고방인가**" 를 먼저 확인한다.

| 값 | 결과서 이름 | 어떻게 되나 |
|---|---|---|
| **1** | 일반 | 이번 이야기. 시드 = `GetSeedFromKernelState` |
| 4 | 트러스트렛 | IUM 갈래 → FALSE (run 문서 최소 조건 표 2) |
| 8 | SGX 엔클레이브 | 시드 = `GetSeedFromRdrand`, PE 머리 검사 건너뜀, CPU 기능 검사 켜짐 |
| `0x10` | VBS 엔클레이브 | IUM 갈래 → FALSE |

`IsEnclave` 가 class `0x56` 답에서 보는 것 `[정적]` (결과서 3):

- status 음수 → 0 → **`IsTrustlet` 으로** (class 0, 길이 `0x40`, `buf[0] = 0x40`).
  - `IsTrustlet` 은 성공이면 `buf+0x38` 의 **bit 7** 을 돌려준다. 실패면 0.
  - `GetTrustedEnvironment +0x12 ~ +0x19`: 0 → **1**, 1 → 4. `+0x1b` 에 저장.
- 성공 + 첫 DWORD 가 `0x10` / `0x11` → `0x10`. `1` → `8`. **그 밖 (0 포함) → `1`**.

#### 진짜 커널은 class `0x56` 에 실패를 준다 — 단계별로

**새 말 — 점프 표**

- 번호로 **갈 곳을 찾는 표** 다. 여기 커널은 **두 장** 을 쓴다: 첫 장 "class → 작은 번호", 둘째 장 "작은 번호 → 갈 주소".
- 비유: 안내 데스크의 표 두 장. "손님 용건 번호 → 담당 번호", "담당 번호 → 방 위치".

1. `nt!NtQueryInformationProcess +0x2d5 CMP EDI,0x73` — class 가 `0x73` 보다 크면 곧장 기본 갈래 (결과서: `+0x2d8 JA +0x42fe`). `0x56` 은 아니니 표로 간다.
2. `+0x2e5` **바이트 표** (rva `0x9bf75c`): `0x56` → **72**.
3. `+0x2ed` **4바이트 표** (rva `0x9bf638`): [72] → rva `0x9bf5de` = **`+0x42fe`**.
4. `+0x2f7 JMP RDX` 로 뛴다.
5. `+0x42fe` = **`MOV EAX,0xc0000003`** (`STATUS_INVALID_INFO_CLASS`, "그런 class 없음").
   - **class 5 와 같은 목적지** 다. 옆 번호 `0x55` → `0x9bf380`, `0x57` → `0x9bd25d` 는 각자 갈 곳이 따로 있다.
6. 그래서 26100.9457 커널은 class `0x56` 을 **구현하지 않는다** `[정적]`.
   - 결과서가 PE 바이트로 표를 읽었고, flow-writer 가 `pefile` 로 **다시 읽어 같음** 을 확인했다.

비유: 용건 번호 `0x56` 손님을 표에서 찾으니 "담당 72" → 그 방은 "**그런 업무 없어요**" 창구다 (class 5 손님과 같은 방).

#### 그래서 진짜와 에뮬레이터는 어떻게 다른가

| | 진짜 26100.9457 `[정적]` | PE_emulator `[관찰 — 에뮬레이터 로그]` |
|---|---|---|
| class `0x56` 답 | `0xc0000003` | 성공 + 0 칸 |
| 그 뒤 | `IsTrustlet` → class 0 (`0x40`) 한 번 더 | 없음 (`IsEnclave` 가 1) |
| attach 의 NtQIP 횟수 | **2번** | **1번** |
| 환경값 | 1 | 1 |

- **결과 (환경값 1) 는 같다.** syscall 개수만 다르다. 막힘의 원인이 아니다.
- 진짜 커널이 class 0 (길이 `0x40`) 을 성공시키는지는 커널 class 0 갈래를 안 읽어 `[추측]` 이다. 실패해도 환경값은 1 로 같다 (`IsTrustlet +0x42 JS +0x54` → 0 → 1) (run 문서 보탬 ⑨).
- `buf+0x38` bit 7 의 이름 (`IsSecureProcess`) 은 결과서 표기다. PDB 근거는 안 밝혔다.

### 2.10 다른 FALSE 자리 둘 — 일반 프로세스는 안 간다

attach 에서 FALSE 로 끝나는 자리는 **셋뿐** 이다. 나머지는 모두 `+0x128 MOV EAX,1` 이다 `[정적]` (결과서 1-e).

| # | 자리 | 언제 | 일반 프로세스 |
|---|---|---|---|
| ① | `DllMain +0x49 JZ +0x1cf` (`SetLastError` 없음) | 환경 bit 8 이 없고, **제 PE 머리** `DllCharacteristics & 0x80` 이 0 | 파일 값 `0x41e0` 이라 **안 감**. 단 엔진이 머리 페이지를 메모리에 올려 둬야 함 |
| ② | `+0xd4 JS +0x1be` → `SetLastError(0x54f)` → FALSE | `MsProviderStart` 음수 | **여기가 막힘** (2.6) |
| ③ | `+0x1a1 JNZ` / `+0x1b9 JMP` → 조각 `FUN_180081a96` → `SetLastError(0x32)` → FALSE | 엔클레이브 (bit 8) 일 때 CPU 기능 검사 | 안 감 |

①을 조금 더:

- `+0x33 ~ +0x49` 에서 `[이미지 시작 + e_lfanew + 0x5e] & 0x80` 을 본다. `DllCharacteristics` 의 `FORCE_INTEGRITY` 비트다 ([run38 해설 2.8](../02_sprint/run38-map-module-해설.md) 에 같은 비트가 나왔다).
  - `FORCE_INTEGRITY` = "이 이미지는 서명 검사를 꼭 받아야 한다" 는 표시로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 자기 PE 머리를 **메모리에서 직접 읽는다**. 그래서 엔진이 KnownDll 섹션으로 올릴 때 머리 페이지가 실제로 붙어 있어야 한다 (run 문서 "PE_emulator 확인거리").
- 비유: 건물이 전원을 켜기 전에 **자기 설계도 첫 장** 의 "검사 필수" 도장을 확인한다. 첫 장이 없으면 못 켠다.

③의 `FUN_180081a96` 은 Ghidra 가 따로 떼어 이름 붙인 **조각** 이다 (`MOV ECX,0x32` → `JMP DllMain+0x1c3`). `DllMain` 의 일부다.

FALSE 가 아니라 **크래시** 로 끝나는 자리도 셋 있다 (결과서) — 일반 첫 attach 에선 안 걸린다:

- `GetSeedFromKernelState +0x4a INT 0x2c` (`RtlGetSystemGlobalData` 음수)
- `TraceLoggingRegisterEx… +0x2e JNZ +0xab → INT 0x29` (이미 등록된 상태 — 첫 attach 는 안 걸림)
- `SymCryptFatal` (셀프테스트 실패)

### 2.11 `NtTraceControl` × 3 — 받아 주기만 하면 된다

ETW 등록 ([run62 해설](run62-tp-etw-init-해설.md) — **방송국 채널** 올리기) 을 부르는 곳이 셋이다 `[정적]`.

1. `DllMain +0xc8 WppInitUm` → `+0x6e` **`EtwRegisterTraceGuidsW`**
2. `MsProviderStart +0x64 TraceLoggingRegisterEx…` → `+0x55` **`EventRegister`** (ntdll `EtwEventRegister`)
3. 같은 함수 `+0x7c` **`EventSetInformation`** (ntdll `EtwEventSetInformation`)

**새 말 — WPP**: ETW 위에 얹어 쓰는 개발자용 추적 기록 방식으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이 DLL 에선 `WppInitUm` · `WPP_GLOBAL_Control` 같은 이름으로 나온다.

반환이 TRUE / FALSE 에 안 닿는 까닭 `[정적]`:

- `WppInitUm` 반환: `DllMain +0xcd` 가 **바로 다음 call** 이다 — `EAX` 를 안 본다.
- `TraceLoggingRegisterEx…` 반환: `MsProviderStart +0x69` 에서 `EAX` 를 다른 값으로 **덮는다**.

3 이라는 숫자의 구성은 `[추측]` 이다 (run 문서 보탬 ⑧):

- run62 의 ntdll 길로 보면 등록 하나마다 `EtwpRegisterProvider +0x14d NtTraceControl(0xf)` 한 번, `EtwEventSetInformation` 은 `EtwpSetProviderTraits +0xdd NtTraceControl(0x1e)` 한 번이다.
- "Once" 는 ntdll `LdrpInitializeProcess +0xbee` (프로세스 첫 등록) 에서 이미 끝났으니 다시 안 탄다.
- 그러면 1 + 1 + 1 = 3 으로 로그와 맞는다. 다만 `EtwRegisterTraceGuidsW` 안은 안 읽어서 짐작이다 → Q170.

### 2.12 로그 끝의 `NtAllocateVirtualMemory` · `NtClose` × 2 — attach 몫이 아니다

- `[정적]` attach 지도 (`entry_edges.tsv`) 에서 `CloseHandle` 은 **둘뿐** 이다.
  - `GetCngHandle +0xd5` — 두 스레드가 동시에 열어 CMPXCHG 에 졌을 때만 (2.4).
  - `TearDownRNGStates +0x4d` — lsass 갈래 · DETACH 길.
- `[정적]` `VirtualAlloc` · `VirtualProtect` · `VirtualQuery` 는 임포트엔 있지만 attach 지도에 **부르는 줄이 없다**.
- 그래서 로그의 `NtAllocateVirtualMemory → NtClose × 2` 는 **`DllMain` 이 FALSE 로 돌아온 뒤 ntdll 로더의 정리** 로 보인다 `[추측]`.
  - ntdll 쪽 길은 이번에 `LdrpInitializeNode +0x293/+0x298` 까지만 봤다 `[공백]` (결과서).
  - → **Q170 ①** (후순위). 엔진을 고친 뒤에도 같은 자리에서 나오면 앞당긴다.

### 2.13 test.exe 쪽 — 사진과 맞춰 보기

새 실행은 없다. flow-writer 가 run44 덤프 읽기 로그를 다시 읽었다.

| 무엇 | 값 | 뜻 |
|---|---|---|
| 초기화 끝 (P2) 모듈 목록 | **6개, bcryptprimitives 없음** (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`) | test.exe 는 초기화 동안 이 DLL 을 안 올린다 `[관찰]` |
| P2 이미지 VAD | 6개 (`_P2.txt:2256 ~ 2261`) | 위와 같음 `[관찰]` |
| `\KnownDlls` 목록 | `bcryptPrimitives.dll` 섹션 객체 (`_P1a.txt:1394` 등) | 부팅 때 만든 **공용 창고 칸** 으로만 있다 `[관찰]`. 다른 프로세스가 import 하면 2.6 KnownDll 길로 올라올 것 `[추측: 실행 안 봄]` |
| KUSD `RNGSeedVersion` (`+0x250`, 8B) | **`0xc`** (8장 같음, `_supp_<K1 ~ P2>.txt:197`) | `GetSeedFromKernelState` 가 시드 버전으로 읽는 칸 `[관찰]` (run44 부팅 기준) |

- sample 소스 (`sample/*.c` · `log.h`) 에도 `bcrypt` · `Crypt` · `rand` 글자가 없다 `[소스]`. 진입점 뒤에 올리는지는 사진 범위 밖이다.
- 그래서 **test.exe 로는 이번 길을 실행해 볼 수 없다** → Q171 은 다른 샘플이 필요하다.

### 2.14 결과서와 다르게 적은 곳 · 보탠 것

**다르게 적은 곳 셋** — run 문서를 따른다. 셋 다 결론은 같다.

| # | 결과서 | run 문서 (맞는 쪽) |
|---|---|---|
| ① | `GetCngHandle +0xa2` "`JS +0xc0`" | **`JS +0xb8`**. `+0xc0` 줄은 없다 (`0x180046e60 − 0x180046da8 = 0xb8`). `+0xb8` 에서 핸들 칸 값을 돌려주는 건 같다 |
| ② | `DebugTraceError` 가 `+0x17 JZ` 로 바로 돌아옴 (`WPP_GLOBAL_Control` 이 자기 자신) | 이 호출 때엔 `DllMain +0xa8 / +0xaf` 가 이미 `WPP_GLOBAL_Control = &WPP_MAIN_CB` 로 바꿔 둠. 돌아오는 건 `+0x19` · `+0x1d JNZ` 안 탐 쪽 (ETW 세션이 이 공급자를 안 켰을 때 `[추측]`). 어느 쪽이든 `EBX` 를 안 건드려 `0xc00000e5` |
| ③ | `GetModuleHandleExW` 실패도 TRUE 인 갈림 "`+0x16b JZ +0x128`" | **`+0x176 JZ +0x128`**. `+0x16b` 는 `MOV RCX,[WPP_GLOBAL_Control]` 이다 |

**보탠 것** (flow-writer 가 `.asm` · `pefile` · 사진으로, 짧게):

- ④ `DllMain +0xda ~ +0xe5` — WPP 플래그가 켜져 있으면 할당 · `GetModuleFileNameW` 를 하고 돌아온다. ETW 세션이 켤 때만이라 TRUE / FALSE 에 안 닿는다.
- ⑤ kernelbase `DeviceIoControl` 갈래 (2.7).
- ⑥ KUSD `RNGSeedVersion` = `0xc` `[관찰]` (2.13).
- ⑦ test.exe 는 이 DLL 을 안 올린다 `[관찰]` → 그림 안 고침.
- ⑧ `NtTraceControl` × 3 = 1 + 1 + 1 `[추측]` (2.11).
- ⑨ class 0 성공 여부 `[추측]` (2.9).
- ⑩ `SymCryptDetectCpuFeaturesByCpuid` 는 ntdll 에도 같은 이름이 있다 (run70). bcryptprimitives 는 **제 사본** 을 `SymCryptInitEnvWindowsUsermodeWin8_1nLater +0xe` 에서 부른다.
- 이전 run 이 틀린 것은 없다 (bcryptprimitives 는 처음) — 정정 없음.

### 2.15 디버거 · 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 `BeingDebugged` 같은 디버거 칸을 읽는 줄은 없다 `[정적: 읽은 줄 안에서]`.
  - 다만 ETW · WPP 세션이 이 공급자를 켜 두면 기록 줄 (`DllMain +0xe5` · `+0x122` · `DebugTraceError +0x1d` 등) 이 더 탄다. TRUE / FALSE 엔 안 닿는다.
- **(새 함정 후보) Ghidra `entry` 는 WinDbg 심볼이 아니다**.
  - 결과서 추천 멈춤점 "`bcryptPrimitives!entry +0xd2`" 의 `entry` 는 Ghidra 이름이다.
  - WinDbg 에선 `bcryptPrimitives!DllMain+0xd2` 나 rva (`bcryptPrimitives+0x43492`) 로 걸어야 할 것이다 `[추측]`. → windbg-kd-runbook pitfalls 에 추가 제안 (P46 `FUN_…` 조각과 같은 꼴).
- 모듈 이름 대소문자: `\KnownDlls` 이름은 `bcryptPrimitives.dll` (`_P1a.txt:1394`) — WinDbg 모듈 이름도 `bcryptPrimitives` 일 것 `[추측]`.

### 2.16 정리 — 비유로 한 번 더

1. 어떤 가게가 **암호 부품 건물** (bcryptprimitives) 을 들인다. 관리실이 전원을 켠다 (`DllMain`, 이유 1).
2. 건물은 먼저 "여기가 보통 가게인가, 금고방인가" 를 묻는다 (`IsEnclave` · `IsTrustlet`). 보통 가게 (1) 다.
   - 진짜 관리사무소는 첫 질문 (`0x56`) 에 "그런 업무 없어요" 라고 해서 한 번 더 묻는다. 흉내 쪽은 한 번에 "보통" 이 나온다. 결론은 같다.
3. 방송국 채널을 올린다 (ETW 셋). 답은 안 본다.
4. 이제 **주사위 상자** 를 채워야 한다. 씨앗 48B 는 관리사무소 **난수 상담실** (`\Device\CNG`) 에서 받는다.
5. 벽의 손잡이 걸이를 본다. 비어 있다. 창구에 **바구니** 를 내밀며 "상담실 손잡이 주세요" (`NtOpenFile`).
6. 흉내 창구는 "**처리됐어요**" 도장만 찍고 바구니를 **빈 채** 돌려준다.
7. 건물 직원은 도장만 보고 "됐다" 한다. 바구니 속 (0) 을 걸이에 건다 — 아무것도 안 걸린다. 그리고 빈손을 들고 간다.
8. 빈손이니 주문서를 못 넣는다. "씨앗 못 받음" (FALSE).
9. 다시 걸이를 본다 — 여전히 비어 있다 → 또 창구로. **다섯 번** 되풀이.
10. 포기 → 상자 못 채움 (`0xc00000e5`) → 문에 "고장" 메모 (`0x54f`) → **전원 켜기 실패 (FALSE)** → 관리실이 건물을 내린다.

고치는 법도 한 줄이다: **창구가 "처리됐어요" 할 때 바구니에 진짜 손잡이를 넣어 주고, 그 손잡이로 넣은 주문서에 "처리됐어요" 만 해 주면 된다.**

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 줄 | bcryptprimitives 줄 없음 | **새 줄** `bcryptprimitives.pdb E63663F3…` (처음 analyze, 함수 1209, `DllMain` = Ghidra `entry` rva `0x433c0`, 지도 285 함수, 47개 뽑음, test.exe 는 안 올림). ntkrnlmp · kernelbase · ntdll 줄에 run91 몫 (NtQIP 점프 표 · `DeviceIoControl` · `RtlGetSystemGlobalData`) |
| 기준 — 근거 로그 | — | run91 줄: 모듈 6 · 이미지 VAD 6 · `\KnownDlls` · KUSD `RNGSeedVersion` `0xc` (새 실행 없음) |
| 흐름도 앞 글 | — | run91 문단 하나: **그림은 안 고쳤다** (run85 · run86 과 같은 까닭) |
| 그림 | — | **그대로** |
| 3장 | user32 · gdi32 절이 끝 | **새 절 "bcryptprimitives 시작 함수 (`DllMain`, Ghidra `entry`)"** — 비유 한 줄 + 트리 하나 + 줄 일곱 (막힘 · 최소 조건 · class `0x56` · `NtAllocateVirtualMemory` · `NtTraceControl` · KUSD · 남은 것 → Q171 / Q170) |
| 4장 | — | run91 새 줄 셋: 동적 **Q171** · 정적 **Q170** · PE_emulator 쪽 · 결과서와 다르게 적은 곳 · 보탠 것 |
| 5장 | — | run 표 · 03_sprint 요청서 번호에 run91 줄 |

태그가 바뀐 것 (요청서 짐작 → 이번 결과):

- 여는 장치: `\Device\CNG` 또는 `\Device\KsecDD` `[추측]` → **`\Device\CNG`** `[정적]` (`.rdata` `0x8d0b8`, flow-writer 가 `pefile` 로 다시 읽음).
- `NtDeviceIoControlFile` 이 로그에 없는 까닭: "그 앞에서 갈렸을 수 있음" `[추측]` → **`GetSeedFromKernelState +0x29 JZ`** `[정적]`.
- `NtAllocateVirtualMemory` · `NtClose` × 2: 정체 모름 → **attach 코드 몫 아님** `[정적]` + 로더 정리 `[추측]`.

### 짐 지도 `baggage.md` 에서 바뀐 것 (마흔일곱째판)

- **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로** — test.exe 는 이 DLL 을 안 올리니까.
- 3.7 KUSD 머리 "ntdll 은 읽기만" 줄에 읽는 줄 하나: `RNGSeedVersion` ← ntdll `RtlGetSystemGlobalData +0x14` ← bcryptprimitives `GetSeedFromKernelState +0x3a`.
- 7.2 KUSD `K1 이전` 에 `RNGSeedVersion` 실마리: 값 `0xc` `[관찰]`, 읽는 곳은 알지만 **쓰는 쪽은 모름**.
- 기준 run91 줄 · 판 목록 · 8장 두 표.

### PE_emulator 쪽 — 무엇을 고치면 되나 (우리 대기열 아님)

run 문서 "PE_emulator 확인거리" 를 그대로 옮긴다.

1. 엔진 `NtOpenFile` 몸이 `\Device\CNG` 를 알아보고 **핸들 칸을 쓰는지**. 그 핸들을 `NtDeviceIoControlFile(0x390008)` 몸이 받아 48B 를 채우는지.
2. KnownDll 섹션으로 올릴 때 **PE 머리 페이지** 가 실제로 붙어 있는지 (FALSE ①).
3. class `0x56` 을 `0xc0000003` 으로 바꾸면 class 0 (`0x40`) 이 한 번 더 나간다 — 그 답의 `+0x38` bit 7 을 0 으로.
4. 고친 뒤에도 `NtAllocateVirtualMemory` · `NtClose` × 2 가 같은 자리에서 나오면 → Q170 ① 을 앞당길 것.

### 큰 그림에서 어떻게 이어지나

- 우리 지도 (test.exe) 는 **안 바뀐다**. 이 DLL 은 "**다른 프로세스라면**" 의 옆길이다. 3장에 user32 · gdi32 와 나란히 놓였다.
- 이 DLL 을 올리는 프로세스에서 순서는 이렇다:
  - 2.6 에서 `\KnownDlls` 공용 창고 길로 올라옴 `[추측: 실행 안 봄]` → 2.7 `LdrpCallInitRoutine` → **이번 `DllMain`** → TRUE 면 다음 DLL, FALSE 면 `0xc0000142`.
- 엔진을 고쳐 TRUE 가 되면, 다음에 막힐 수 있는 곳은 둘이다 (run 문서):
  - `IsInLSAProcess` 의 레지스트리 (`Reg*` → NT 함수) → Q170 ②.
  - attach 뒤 첫 사용 `ProcessPrng` (난수를 실제로 꺼내는 길) → Q170 ③.

---

## 4. 아직 모르는 것

### 정적으로 남은 것 → **Q170** (작음 · 후순위)

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 |
|---|---|---|
| ntdll `DllMain` FALSE 뒤 정리 길 — `NtAllocateVirtualMemory` · `NtClose` × 2 가 어디서 나오나 | ntdll 쪽은 `LdrpInitializeNode +0x298` (`0xc0000142`) 까지만 봄. attach 범위 밖 | Q170 ① (후순위. 엔진을 고친 뒤에도 같은 자리면 앞당김) |
| kernelbase `RegOpenKeyExW` · `RegQueryValueExW` · `RegCloseKey` 가 어떤 NT 함수로 가나 | kernelbase 안은 안 읽음. NT 함수 이름은 `[추측]` | Q170 ② (Q166 ② 와 같은 kernelbase 함수라 같이 볼 수 있음) |
| attach 뒤 첫 사용 `ProcessPrng` (rva `0x2df90`) → `AesRNGState_select` → `AesRNGState_generate`, 재시드 `AesRNGState_root_reseed +0x43` (같은 시드 함수를 CFG 간접으로) | 요청 범위 밖 (attach 만) | Q170 ③ |
| `cng.sys` 의 `0x390008` 처리 | 엔진은 "48B 채워 주기" 로 충분해서 안 읽음 | Q170 (후순위) |
| `NtTraceControl` × 3 의 구성 (지금 1 + 1 + 1 `[추측]`) | ntdll `EtwRegisterTraceGuidsW` 안을 안 읽음 | Q170 |

### 실행해 봐야 아는 것 → **Q171** (동적)

- **test.exe 로는 못 본다** — 초기화 동안 안 올림 `[관찰]`, 진입점 뒤도 sample 에 그런 API 없음 `[소스]`.
- bcryptprimitives 를 올리는 프로세스가 필요하다. **샘플 고르기는 사용자 결정** 이다 (이 DLL 은 어느 프로세스든 올릴 수 있어서, 멈춤점은 그 프로세스 조건으로). Q104 에 안 붙였다.
- 추천 멈춤점 넷 (run 문서):

| 멈춤점 | 볼 것 | 무엇을 확인하나 |
|---|---|---|
| `DllMain+0xd2` (rva `0x43492`) | `eax` | 0 이면 성공 길, `0xc00000e5` 면 시드 길 실패 |
| `GetCngHandle+0x9b` | `eax` · `dq @rbp+0x10 L1` | 진짜 status 와 **진짜 핸들 모양** (4의 배수 작은 수 `[추측]`) |
| `GetSeedFromKernelState+0x7e` | `eax` · `db @rsi L30` | IOCTL BOOL 과 시드 48B |
| `IsEnclave+0x4b` (이어 `IsTrustlet+0x3b`) | `eax` · `dd @rsp+0x30 L1` | 진짜 class `0x56` 이 `0xc0000003` 인가, class 0 이 따라오나 (NtQIP 2번) |

- 그 밖: `IoStatus.Information` (`0x30` 인가 `[추측]`), `LsaPid` 값 · `RegOpenKeyExW` status, `NtTraceControl` 실제 개수 · 인자, TRUE 로 끝나는 진짜 실행에서 `NtAllocateVirtualMemory` · `NtClose` × 2 가 나오나 (안 나오면 "attach 몫 아님" 확인).
- 결과서 "동적 4" (KUSD `+0x250` 값) 는 run44 사진 `0xc` 로 닫혔다. 다른 부팅에서도 같은지는 모른다.

### 대기열 밖 — 이름 · 짐작으로 남은 것

- class `0x56` 이름 (`ProcessEnclaveInformation`) · `buf+0x38` bit 7 이름 (`IsSecureProcess`) — PDB 에 열거형 · 근거가 없어 결과서 표기 그대로 `[추측]`.
- KUSD `RNGSeedVersion` 을 **누가 쓰나** — 이름으로 보아 커널이 난수 씨앗을 새로 할 때 올리는 번호 `[추측]` (짐 지도 7.2).
- `DebugTraceError` 가 돌아오는 정확한 까닭 — WPP 비트를 켜는 콜백 (`0x18005f1e0`) 을 안 읽음 `[추측]` (2.14 ②).

---

## 5. 더 읽기

- run 문서: [`run91-bcrypt-init.md`](run91-bcrypt-init.md) — "PE_emulator 쪽에서 쓸 것" (요청 31 결론 · attach 차례 표 · FALSE 자리 · 최소 조건 표), "호출 관계", "사진 대조", "결과서와 다르게 적은 곳 · 보탠 것"
- 결과서: [`_workspace/sast/87_bcrypt-init_result.md`](../../_workspace/sast/87_bcrypt-init_result.md) — 1-d "status 0 인데 핸들 칸이 안 쓰인 경우" (7단계), 3 "class `0x56`", 5 "최소 조건 표"
- 요청서: [`_workspace/sast/87_bcrypt-init.md`](../../_workspace/sast/87_bcrypt-init.md) — 배경 (에뮬레이터 로그 syscall 차례)
- flow.md: [3장 "bcryptprimitives 시작 함수"](flow.md) 절, 4장 run91 줄 셋, 2.7 (DLL 전원 켜기 틀)
- 짐 지도: [`baggage.md`](baggage.md) 3.7 KUSD 머리 · 7.2 KUSD `RNGSeedVersion`
- 대기열: [`_workspace/plan.md`](../../_workspace/plan.md) Q169 (끝) · Q170 · Q171
- 앞 해설:
  - [run85](run85-user32-init-해설.md) · [run86](run86-gdi32-init-해설.md) — 같은 꼴 ("test.exe 는 안 올리는 DLL 의 `DllMain` 최소 조건")
  - [run61](run61-random-ex-init-해설.md) — ntdll 쪽 주사위 상자 (다른 상자)
  - [run62](run62-tp-etw-init-해설.md) — ETW 등록 · `NtTraceControl` · Once
  - [run66](run66-apphelp-cache-해설.md) 2.7 — 드라이버 · IOCTL (상담실 · 주문서)
  - [run17](../02_sprint/run17-ldr-init-entry-해설.md) 2.6 — `LOCK CMPXCHG`
