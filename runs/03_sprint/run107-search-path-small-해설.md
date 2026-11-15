# run107 search-path-small — 해설

> 이 글은 [run107 문서](run107-search-path-small.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q71** — 2.4·2.6 검색 경로 주변 작은 정적 (`_workspace/plan.md`, 이번에 **끝**, 한 바퀴로)
  - run39 가 남긴 "작은 정적 넷" (a) ~ (d) + 번호 없이 남긴 셋.
  - run101 이 남긴 정적 ① (`LdrpInitializePerUserWindowsDirectory`) 도 **같은 함수**라 이번에 같이 닫혔다.
  - 덤으로 run63 이 남긴 정적 ③ ("`LdrpIsModuleUnderSystem32` 가 ntdll 에서 왜 거짓인가") 도 닫혔다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q79** (사진 덤프 오프라인 읽기), **Q46** (실행 값)
    - 정적 → 새 **Q195 ① ~ ④**
    - 짐 지도 셈 (7.3 "모름" 의 칸 몇 개를 3.8 로 옮길지) → **사용자 결정 대기**
- 근거:
  - run 문서 [`run107-search-path-small.md`](run107-search-path-small.md) (가장 중요)
  - 요청서 [`_workspace/sast/102_search-path-small.md`](../../_workspace/sast/102_search-path-small.md) · 결과서 [`102_search-path-small_result.md`](../../_workspace/sast/102_search-path-small_result.md) (판정 **분석성공**)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 그림에서는 **그림 2 메모 한 마디**만 바뀌었다 (노드·화살표 그대로). 글 쪽은 2.4 · 2.5 · 2.6 · 4장 · 5장에 줄이 붙거나 고쳐졌다 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): **새 줄 없음, 고친 줄 5**, 새 7.38.
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` (기준과 같다). 다시 analyze 안 했다.
  - 커널 쪽은 이미 있던 `.asm` 두 개를 **읽기만** 했다. kernel32 `TermsrvGetWindowsDirectoryW` 는 `.asm` 이 없어서 `objdump` 로 바이트를 직접 읽었다.
  - 이 글의 `[관찰]` 은 전부 **예전 사진 로그 (run44 · run46 · run65)** 를 다시 읽은 것이다.

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **사용자 Windows 폴더** → 2.3
- **`AddDllDirectory` 목록** → 2.4
- **허공 포인터** → 2.4
- **옵션 15 `PREFER_SYSTEM32`** → 2.6
- **옵션 있는 검색** → 2.14
- 이미 푼 말은 링크만 건다: DLL 검색 경로 = **주소록** ([run39 해설 "비유"](../02_sprint/run39-dll-search-path-해설.md)), 완화 옵션 = **안전 규칙** ([run32 해설 2.9](../02_sprint/run32-params-initblock-해설.md)), 로드 구성 = 설계도 뒤 **보안 설정 서류** ([run63 해설 2.8](run63-cfg-func-table-해설.md)).

### 이번 바퀴에서 조심할 곳 넷

1. **"코드에 이런 길이 있다" 와 "test.exe 가 이 길을 탔다" 는 다르다.**
   - (a) 사용자 Windows 폴더 길, (b) 시스템 폴더 먼저 길, 2.14 옵션 있는 검색 길 — 셋 다 test.exe 는 **안 탄다.**
   - 그래도 길 모양은 `[정적]` 으로 다 읽었다. 이 글은 "길 모양" 과 "test.exe 는?" 을 늘 나눠 쓴다.
2. **정정이 하나 있다.** run39 의 "비트 60~61 이 1 이면 **목록 전체가 시스템 폴더 하나로**" 는 틀렸다. 맞는 건 "시스템 폴더 글을 **맨 앞으로 옮긴다**" 다 (2.6).
   - test.exe 는 그 값이 0 이라 지도 결과는 안 바뀐다.
3. **run 문서가 결과서와 다르게 적은 곳이 두 군데 있다.** 이 글은 run 문서를 따른다 (2.17).
   - 칸 이름: 결과서 `gpTermsrvGetWindowsDirectoryW` → run 문서 **`pfnTermsrvGetWindowsDirectoryW`**.
   - `RtlpDllSearchPath` 에 쓰는 줄: 결과서 `LdrpGetDllPath +0xa1` → run 문서 **`RtlpGetCachedPath +0x111`**.
4. **오프셋은 call 자리다.** ret 자리는 run 문서 "정적 — 호출 지점" 표에 있다.
   - 다만 1절 큰 그림과 2.1 트리는 앞 해설들처럼 **ret 자리**로 적었다. 숫자가 조금 다르면 call / ret 차이다.

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 시스템 콜 = **신청 창구**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**, DLL = **건물** → [run39 해설 "비유"](../02_sprint/run39-dll-search-path-해설.md)
- 모듈 항목 = **명부 한 줄**, 로드 문맥 = **지시서** → [run26 해설](../02_sprint/run26-map-dll-file-해설.md)
- **DLL 검색 경로 = 주소록** ("어느 폴더들을, 어떤 순서로 뒤져 볼지" 적은 책 한 권), 경로 구조체 `P` = 주소록 **표지** (처음엔 빈 표지), 칸 코드 순서 표 = 주소록 **목차 양식** → [run39 해설 2.3 · 2.6](../02_sprint/run39-dll-search-path-해설.md)
- 검색 경로 캐시 (`RtlpDllSearchPath` 등) = 관리실 책장의 **주소록 인쇄 원본** → [run39 해설 2.4 ④](../02_sprint/run39-dll-search-path-해설.md)
  - run107 문서는 같은 것을 "**복사본 서랍**" 이라고 불렀다. 같은 물건이다. 이 글은 run39 해설의 "인쇄 원본" 을 이어 쓴다.
- `RtlpSystemDirs` = 주소록의 **시스템 칸 한 덩어리** (`<System32>;<Windows>\system;<Windows>;`) → [run39 해설 2.6](../02_sprint/run39-dll-search-path-해설.md)
- `LdrSystemDllInitBlock` = 관리실 책상 위 **업무 수첩** (관리사무소가 들어와 채워 둠), 그 `+0xa0` 24바이트 = **안전 규칙** (완화 옵션) → [run32 해설 2.8 · 2.9](../02_sprint/run32-params-initblock-해설.md)
- `ProcessParameters` = **개업 서류** → [run32 해설 2.3](../02_sprint/run32-params-initblock-해설.md#23-새-말--프로세스-매개변수-블록)
- KUSD (`KUSER_SHARED_DATA`) = 건물 로비의 **공용 게시판** → [run34 해설 2.5](../02_sprint/run34-parent-leftovers-해설.md)
- CFG 간접 호출 = **교환원** → [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)
- 유저 쪽 잠금 = **열쇠** → [run18 해설 2.4](../02_sprint/run18-proc-init-setup-해설.md) · 참조 수 = 지금 **손에 쥐고 있는 쪽의 수** → [run23 해설](../02_sprint/run23-load-dependent-해설.md)
- kernel32 전역 = 건물 **수첩**, `pfnTermsrvGetWindowsDirectoryW` = 그 수첩의 "터미널 서비스" 페이지 한 칸, TS 인식 표시 = 간판 옆 "**여러 손님 겸용 매장 인증**" 스티커 → [run101 해설 2.5 · 2.7](run101-k32-leftovers-해설.md)
- 사진 P1a · P1b · P2 → [run44 해설](run44-baggage-snap-해설.md)

run 문서가 이번에 쓴 비유도 같은 그림이다.

- `LdrpInitializePerUserWindowsDirectory` 는 "손님마다 따로 쓰는 Windows 폴더가 있으면 주소록 시스템 칸 끝에 한 줄 더 적는" 사람이다.
  - 물어볼 곳이 "그런 폴더 없어요 (0)" 하면 **아무것도 안 하고 돌아간다.** test.exe 가 그 경우다.
- 적을 일이 있으면 시스템 칸을 새 종이에 옮겨 적고, **인쇄 원본 셋을 버린다.** 새로 찍어 두지는 않는다. 다음에 누가 주소록을 펼칠 때 새로 찍는다.
- 비트 60~61 은 "**시스템 폴더 먼저 보기**" 스위치다. 켜지면 시스템 칸이 주소록 **맨 위**로 올라가고 나머지 줄은 순서 그대로다. 주소록이 한 줄로 줄어드는 게 아니다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) | 표시 |
|---|---|---|
| (a) `LdrpInitializePerUserWindowsDirectory` 는 test.exe 에서 무엇을 하나 | **아무것도 안 바꾸고 `+0x4e` 에서 0 을 돌려준다.** 넘겨받은 `TermsrvGetWindowsDirectoryW` 가 칸 `pfnTermsrvGetWindowsDirectoryW` (kernel32 rva `0xc1d10`, 0 그대로) 를 보고 0 을 주기 때문 | `[정적]` (칸 값 자체는 `[VM]` 한 줄 남음) |
| (a) 탔다면 | `RtlpSystemDirs` 뒤에 사용자 Windows 폴더를 붙이고, 캐시 셋을 **비우기만** 하고 (다시 안 지음), 같은 폴더를 `AddDllDirectory` 목록에 넣는다 | `[정적]` |
| (b) `LdrSystemDllInitBlock+0xa0` 비트 60~61 | **완화 옵션 15 `PS_MITIGATION_OPTION_IMAGE_LOAD_PREFER_SYSTEM32`.** 1 이면 시스템 폴더 글이 **맨 앞으로** (run39 정정). test.exe 는 0 | `[정적]` · 값 `[관찰]` run46 |
| (c) 모듈 항목 `+0x118 DependentLoadFlags` 는 누가 쓰나 | `LdrpCfgProcessLoadConfig +0xa7` 이 **PE 로드 구성 `+0x4e` & 마스크** 로. 6 모듈 파일 값 = 사진 값 | `[정적+관찰]` |
| (d) `<Windows>` 글은 어디서 오나 | **`RtlGetNtSystemRoot()`** → KUSD `NtSystemRoot` = `C:\WINDOWS` | `[정적+관찰]` |
| (d) `ProcessParameters.Flags & 0x20000000` 은 run32 비트 29 인가 | **같은 칸 · 같은 비트** | `[정적]` |
| 덤 | `LdrpIsModuleUnderSystem32` 는 System32 **하위 폴더** 안 모듈만 참 → run63 질문 닫힘 | `[정적]` |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

숫자는 ret 자리, 앞 run 들 그대로다. 이번 자리는 여러 군데에 흩어져 있다 (← 표시).

```
cmd.exe 의 부탁 → 커널 (프로세스 · 스레드 만들기)
 │   PspSetupUserProcessAddressSpace +0xea   개업 서류 Flags 비트 29 (조건부)          ← (d) 이번 (읽기만)
 │   PspPrepareSystemDllInitBlock +0x12b/+0x138  업무 수첩 +0xa0 안전 규칙 24B      ← (b) 이번 (읽기만)
 → ntdll LdrInitializeThunk → … → LdrpInitializeProcess
     ├─ +0xce1   RtlGetNtSystemRoot   <Windows> 글                                       ← (d) 이번
     ├─ +0x1065 ~ +0x1161  시스템 칸 RtlpSystemDirs 짓기                              (run39)
     ├─ +0x1318  LdrpInitializeDllPath(0, 0, &P)   주소록 빈 표지                     (run39)
     ├─ (모듈마다) LdrpProcessMappedModule +0x14a LdrpCfgProcessLoadConfig
     │                 → 명부 줄 +0x118 DependentLoadFlags                             ← (c) 이번
     ├─ +0x1ab1  dll1 · dll2 올리기 → LdrpSearchPath → … → RtlpComputePath             (run39)
     │                 +0x21 비트 60~61 보기                                            ← (b) 이번
     ├─ +0x1b5e  BaseThreadInitThunk(1,0,0)                                            (run101)
     ├─ +0x1b77  LdrpInitializePerUserWindowsDirectory                                 ← (a) 이번 (중심)
     ├─ +0x1bbb  LdrpInitializeGraphRecurse   (dll 시작 함수들)                       (run27)
     └─ …
 → NtContinue → … → test.exe 진입점 → (CRT) LoadLibraryExW ×3 → main
```

- 중심은 **`+0x1b77`** 한 줄이다. dll1 · dll2 를 다 올린 **뒤**, 그래프 켜기 **앞**이다.
- run39 · run101 둘 다 이 줄을 "뜻 모름" 으로 남겼다. run39 는 "시스템 칸을 **다시 쓰는 줄**이 있다" 를 보고, run101 은 "그 함수가 **kernel32 함수 하나를 넘겨받는다**" 를 보고.
- 나머지 (b) · (c) · (d) 는 run39 가 검색 경로를 읽다가 "이건 어디서 오지?" 하고 남긴 작은 질문들이다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. **(a)** `+0x1b72 LdrpInitializePerUserWindowsDirectory` 는 시스템 칸을 **무엇으로, 어떤 조건에서** 다시 쓰나. 그 뒤 인쇄 원본 (캐시) 을 다시 찍나. test.exe 에서 타나.
2. **(b)** 업무 수첩 `+0xa0` 비트 60~61 은 무슨 스위치인가.
3. **(c)** 명부 줄 `+0x118 DependentLoadFlags` 는 **누가** 쓰나. sample 값이 0 이라는 근거가 "링크 옵션에 없다" 뿐이었다.
4. **(d)** 주소록의 `<Windows>` 글은 어디서 오나. 개업 서류 `Flags & 0x20000000` 은 run32 가 본 비트 29 와 같은 것인가.
5. (작은 것) "옵션 있는 검색" 의 칸 차례, 표지 `P+0x28 ~ +0x77` 을 읽는 곳, `.` 칸이 현재 폴더로 풀리는 곳.

### 바퀴 전에 알던 것

- `[정적: refs 줄]` (run39) `RtlpSystemDirs` 를 쓰는 곳이 `LdrpInitializeProcess +0x1161` 말고 `LdrpInitializePerUserWindowsDirectory +0x157` 도 있다. `RtlpSystem32Dirs` 버퍼 칸도 `+0x164`. **무엇을 바꾸는지 · test.exe 에서 타는지는 `[공백]`.**
  - 그래서 run39 는 "나중 `LoadLibraryExW` ×3 쪽에 영향이 있을 수 있다" 고 `[추측]` 했다.
- `[정적]` (run20 · run101) 그 함수는 `LdrpInitializeKernel32Functions` 가 kernel32 에서 **이름으로 찾은 `TermsrvGetWindowsDirectoryW` 주소**를 넘겨받는다. 그 함수는 수첩 칸 `pfnTermsrvGetWindowsDirectoryW` 를 보는데, test.exe 에선 그 칸이 **0 그대로**다 (run101).
  - run101 은 "그래서 그 함수가 무엇을 돌려주나" 는 안 봤다.
- `[정적]` (run39) `RtlpComputePath +0x21 ~ +0x32` 가 업무 수첩 `+0xa0` 비트 60~61 을 보고, 1 이면 "목록 전체를 시스템 폴더 하나로" 바꾼다고 적었다. **← 이번에 틀린 것으로 드러남.**
- `[추측: 링크 옵션]` (run39) test.exe · dll1 명부 줄 `+0x118` 은 0 일 것 (`build.bat` 에 `/DEPENDENTLOADFLAG` 가 없음). 그 칸에 값을 넣는 줄은 못 찾았다.
- `[관찰]` (run63) ntdll 명부 줄 `DependentLoadFlags` 는 사진에서 `0x800`. 그래서 `LdrpIsModuleUnderSystem32` 가 ntdll 에서 **거짓**인 건 알았지만 **왜** 거짓인지는 몰랐다.
- `[정적]` (run32) 커널 `PspSetupUserProcessAddressSpace +0xe6` 이 개업 서류 `Flags` 비트 29 (`0x20000000`) 를 켠다 (OneCore forwarders silo 일 때).
- `[추측: NtSystemRoot]` (run39) `<Windows>` 글의 정확한 출처 줄은 안 봤다.

---

## 2. 찾은 것

### 2.1 한눈에 — 트리 하나

run 문서 "호출 관계" 에서 대표만 옮겼다 (ret 자리). 전체는 run 문서.

```
(a) 사용자 Windows 폴더 — test.exe 는 +0x4e 에서 끝
LdrpInitializeKernel32Functions
 └─ +0x130  LdrGetProcedureAddressForCaller("TermsrvGetWindowsDirectoryW") → [RSP+0x188]
LdrpInitializeProcess
 └─ +0x1b77  LdrpInitializePerUserWindowsDirectory(RCX = [RSP+0x188])
              ├─ +0x4c   (교환원) KERNEL32!TermsrvGetWindowsDirectoryW(buf, 0x104)
              │            칸 [kernel32+0xc1d10] = 0 → RET 0                    ← test.exe
              │  +0x4e  0 → +0x50 / +0x75 RET 0                                ← test.exe
              │  (0 이 아닐 때만)
              ├─ +0xc1 ~ +0x152  새 버퍼에 옛 글 + 새 폴더 + ";"
              │  +0x157 [RtlpSystemDirs]   +0x164 [RtlpSystem32Dirs+8]
              ├─ +0x171  RtlpSignalSystemDirsModification   인쇄 원본 셋 비우기
              └─ +0x180  LdrAddDllDirectory                  AddDllDirectory 목록에 넣기

(b) 비트 60~61 — test.exe 는 0
RtlpComputePath
 │  +0x21 / +0x2b / +0x32  R14 = (+0xa0 >> 60) & 3
 └─ R14B == 1 이면 시스템 폴더 글을 항목 0 번으로 먼저 복사 → 나머지는 원래 순서

(c) DependentLoadFlags
LdrpProcessMappedModule
 └─ +0x14a  LdrpCfgProcessLoadConfig   +0x8c 로드 구성 +0x4e → +0xa7 [명부 줄+0x118] = 값 & 마스크

(d) <Windows> 글
LdrpInitializeProcess
 └─ +0xce1  RtlGetNtSystemRoot   PEB.SharedData null → 0x7ffe0030 (KUSD NtSystemRoot "C:\WINDOWS")
```

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — `LdrpInitializePerUserWindowsDirectory +0x4e JNZ +0x77`

- **누가**: `LdrpInitializePerUserWindowsDirectory` (rva `0xfb04c`, 388바이트). `LdrpInitializeProcess` 가 `+0x1b72` 에서 부른다 (ret `+0x1b77`).
- **어디서**: 바로 앞 `+0x47` 에서 교환원을 거쳐 `TermsrvGetWindowsDirectoryW` 를 부르고 (ret `+0x4c`), `+0x4c ADD EAX,EAX` 로 돌려받은 **글자 수를 바이트 수로** 바꾼 직후다.
- **누구를**: 부르기가 아니라 갈림길이다. `EAX` 가 0 이 아니면 `+0x77` 로 뛴다.
- **무엇을 위해**: "사용자 Windows 폴더가 있다" 일 때만 시스템 칸을 고치러 간다. 0 이면 `+0x50 XOR EAX,EAX` → `+0x75 RET` — **성공 (0) 을 돌려주고 끝.**

모두 `[정적]`. 이 한 줄이 이번 바퀴 (a) 의 답이다. test.exe 는 여기서 **안 뛴다.**

#### 한 줄 읽기 ② — `LdrpCfgProcessLoadConfig +0xa7 MOV [RSI+0x118],ECX`

- **누가**: `LdrpCfgProcessLoadConfig`. `LdrpProcessMappedModule` 이 `+0x145` 에서 부른다 (ret `+0x14a`).
- **어디서**: 앞에서 `+0x8c MOVZX ECX,word [RDI+0x4e]` 로 **로드 구성 디렉터리 `+0x4e`** 의 2바이트를 읽고, `+0x90 ~ +0xa5` 에서 마스크로 걸러 낸 직후다.
  - 조건: 로드 구성 크기가 `0x94` 이상일 때만 여기까지 온다 (`+0x80 CMP [RDI],0x94` · `+0x86 JC`).
- **누구를**: 쓰기다. `RSI` = 명부 줄, `+0x118` = `DependentLoadFlags`.
- **무엇을 위해**: 그 모듈 파일에 적힌 "내 의존 DLL 은 이렇게 찾아 줘" 값을 명부 줄에 옮겨 둔다. 나중에 찾기 담당 (`LdrpMapDllSearchPath +0xc1`) 이 **부모 명부 줄**의 이 칸을 읽는다 (run39).

모두 `[정적]`. ntdll 에서 이 칸에 **주로 쓰는 줄은 이것 하나**다 (2.7).

---

## (a) 사용자 Windows 폴더 — test.exe 에선 아무것도 안 바꾼다

### 2.3 물어볼 곳이 "없어요 (0)" 하면 돌아간다 `[정적]`

**새 말 — 사용자 Windows 폴더**: 터미널 서비스 앱 호환 환경에서 "**손님마다 따로 쓰는 Windows 폴더**" 다 (run 문서의 말). `TermsrvGetWindowsDirectoryW` 가 그 폴더 글을 돌려준다.

- 비유: 여러 손님이 한 건물을 나눠 쓰는 매장이면, 손님마다 자기 전용 "시스템 창고" 를 하나씩 받는다. 관리실은 그 창고 주소를 주소록 시스템 칸 끝에 한 줄 더 적어 준다.
- 이 폴더가 무엇에 쓰이는지 더 자세한 건 근거에 없다.

**누가 무엇을 넘겨주나** `[정적]`

1. `LdrpInitializeKernel32Functions +0x12b` (ret `+0x130`) 가 kernel32 에서 이름 `"TermsrvGetWindowsDirectoryW"` 로 함수 주소를 찾아 `[RSP+0x188]` (부르는 쪽 `LdrpInitializeProcess` 의 칸) 에 둔다 (run20 그대로).
2. `LdrpInitializeProcess +0x1b6a MOV RCX,[RSP+0x188]` → `+0x1b72` 에서 그 주소를 첫 인자로 넘긴다.
   - 앞에 조건이 하나 있다: `+0x1b4c TEST` · `+0x1b4f JZ +0x1b83` — 칸 `Kernel32ThreadInitThunkFunction` 이 0 이면 이 함수를 아예 안 부른다.
   - 뒤에도 하나: `+0x1b7b TEST EAX` · `+0x1b7d JS` — 음수가 오면 실패 꼬리로.
3. 함수 안 `+0x2d MOV RAX,RCX` → `+0x47` 교환원 호출 = `TermsrvGetWindowsDirectoryW(버퍼 [RSP+0x60], 0x104)`.
   - 버퍼는 `0x104` 글자짜리다.

**`TermsrvGetWindowsDirectoryW` 몸통** `[정적: objdump]`

`.asm` 이 없어서 결과서가 바이트를 직접 읽었고, flow-writer 가 한 번 더 봤다. 다섯 줄뿐이다.

| 자리 | 하는 일 |
|---|---|
| `+0x0` | `MOV RAX,[kernel32+0xc1d10]` — 수첩 칸 `pfnTermsrvGetWindowsDirectoryW` 를 읽는다 |
| `+0x7` | `TEST RAX,RAX` |
| `+0xa` | `JNZ +0xe` — 칸이 0 이 아니면 아래로 |
| `+0xc` | `RET` — **칸이 0 이면 RAX = 0 그대로 돌아간다** |
| `+0xe` | `JMP` — 칸에 적힌 함수 (tsappcmp 쪽) 로 넘긴다 |

- 비유: 이 kernel32 함수는 사실 **전화 돌려 주는 사람**이다. 수첩 칸에 "진짜 담당자 번호" 가 적혀 있으면 그쪽으로 돌리고, 칸이 비었으면 "담당자 없어요 (0)" 하고 끊는다.

**그 칸은 test.exe 에서 비어 있다** `[정적]` (run101)

- 칸 `pfnTermsrvGetWindowsDirectoryW` (kernel32 `.data` rva `0xc1d10`) 에 쓰는 줄은 `BasepInitializeTermsrvFpns` 의 `GetProcAddress` 12바퀴 루프 **하나뿐**이다.
- test.exe 는 그 루프 앞 `+0x2b` 에서 나간다 ([run101 해설 2.5](run101-k32-leftovers-해설.md) — 간판 옆 "여러 손님 겸용 매장 인증" 스티커가 있어서).
  - 갈래를 가르는 값 (KUSD `SuiteMask 0x110` · PEB `SharedData` null · exe `DllCharacteristics 0x8160`) 은 run44 사진 `[관찰]` 이다.
- 그래서 칸은 **0 그대로**.

**그래서 — 단계별로**

1. 칸 0 → `TermsrvGetWindowsDirectoryW` 는 `+0xc RET` 에서 0 을 돌려준다 `[정적]`.
2. 0 → `+0x4c ADD EAX,EAX` 해도 0 → `+0x4e JNZ` 안 탐 `[정적]`.
3. `+0x50` → `+0x75 RET` 0 `[정적]`.
4. → **`RtlpSystemDirs` · `RtlpSystem32Dirs` · 인쇄 원본 셋 · `AddDllDirectory` 목록 모두 그대로** `[정적]`.
5. → 진입점 뒤 `LoadLibraryExW` ×3 에도 **영향 없음** `[정적]`. run39 의 "영향이 있을 수 있다 `[추측]`" 은 지워졌다.

- 아직 확인 아님: 칸 값 **자체**는 아무도 못 봤다. run44 는 kernel32 `.data` 를 안 읽었다 → `[VM]` 한 줄 (4절, Q79).

### 2.4 만약 탔다면 — 세 가지를 한다 `[정적]`

test.exe 는 안 타는 길이다. 그래도 "다른 환경에선 무엇이 바뀌나" 를 알아야 지도가 빠짐없으니, 길 모양만 따라간다.

**① 시스템 칸에 한 줄 덧붙이기**

1. `+0x77 CMP [RtlpSystemDirs+8],0` · `+0x97 JZ +0x171` — 시스템 칸이 아직 없으면 (예: `LdrpPolicyBits & 1` 로 안 지었으면) 글 바꾸기를 건너뛰고 `+0x171` 로.
2. `+0xbc RtlAllocateHeap` — 새 버퍼 (옛 길이 + 새 폴더 + 2).
   - 실패하면 `+0xed LdrpLogInternal` → `0xc0000017`.
3. `+0x112` · `+0x121` · `+0x132` — 옛 `RtlpSystemDirs` + 새 폴더 + `;` 를 이어 붙인다.
4. `+0x14d RtlFreeHeap` — 옛 버퍼를 돌려준다.
5. 쓰기 둘:
   - **`+0x157 MOVDQU [RtlpSystemDirs]`** — 16바이트 (길이 · 최대 길이 · 새 버퍼).
   - **`+0x164 MOVQ [RtlpSystem32Dirs+8]`** — 새 버퍼 **포인터만**. 길이 칸은 그대로.

왜 `RtlpSystem32Dirs` 의 포인터도 고치나 — 근거 → 그래서:

- 근거 `[정적]`: 두 전역이 **같은 옛 버퍼를 같이 쓰고 있었다** (baggage.md 3.8 고친 줄). 그 옛 버퍼를 4번에서 돌려줬다.
- 그래서 `RtlpSystem32Dirs` 가 옛 버퍼를 계속 가리키면 안 되니까 포인터만 새것으로 바꿔 둔다 (run 문서).
- 비유: 두 사람이 같은 종이 한 장을 보고 있었는데, 그 종이를 버리고 새 종이에 옮겨 적었다. 그러니 두 사람 모두에게 "이제 새 종이 봐" 하고 알려 줘야 한다.

**새 말 — 허공 포인터**: 이미 돌려준 메모리를 **아직 가리키고 있는** 포인터다. 비유: 버린 종이 자리를 계속 가리키는 손가락.

- 이 함수는 **`LdrpSystem32`** (`ntdll+0x1d2990`) 는 안 고친다 `[정적]`. 그 칸은 `LdrpInitializeProcess +0x10a6` 이 같은 버퍼 포인터를 넣어 둔 칸이다.
- 그래서 이 길을 타는 환경에선 옛 버퍼가 풀린 뒤 `LdrpSystem32` 가 **허공 포인터**가 된다 `[정적]`. 읽는 곳은 `LdrpIsModuleUnderSystem32 +0x49`.
- 실제로 무슨 일이 나는지는 `[추측]` 이다. test.exe 는 이 길을 안 타서 상관없다.

**② 인쇄 원본 셋 비우기** — 2.5 에서 따로.

**③ `AddDllDirectory` 목록에 넣기**

**새 말 — `AddDllDirectory` 목록**: 로더 전역 `LdrpUserDllDirectories` 에 모아 둔 "**나중에 더한 DLL 폴더들**" 목록이다. "옵션 있는 검색" (2.14) 이 칸 코드 6 으로 이 목록을 넣는다 `[정적]`.

- 비유: 주소록 본문과 별도로, 뒤표지에 붙여 둔 "**추가 주소 메모**". 특정 방식으로 찾을 때만 펴 본다.

- `+0x17b LdrAddDllDirectory(&새 폴더, &쿠키)` → `+0x180 JMP +0x50` (0 반환, 반환값은 안 본다).
- `LdrAddDllDirectory` 안 `[정적]`:
  - `+0x5c TEST [LdrpPolicyBits],4` — 꺼져 있으면 `0xc000000d`. 사진 `LdrpPolicyBits = 0x16` `[관찰]` (run65) → 켜짐 쪽.
  - `+0x1b5` — `LdrpUserDllDirectories` 끝에 그 폴더를 붙인다.
  - `+0x1e8` — `RtlpDllSearchPathWithOptions` 칸을 0 으로.

### 2.5 인쇄 원본은 "버리기만" 하고 다시 안 찍는다 `[정적]`

run39 가 물은 "그 뒤 캐시를 다시 짓나" 의 답이다. **안 짓는다. 비우기만 한다.**

**`RtlpSignalSystemDirsModification` — 차례대로**

1. `+0x16` `RtlpCachedPathLock` 열쇠를 **혼자 쥔다** (독점).
2. 원본 셋을 비운다 — `RtlpInvalidatePathCache` 로:
   - `+0x22` `RtlpDllSearchPath` (DLL 찾기용 — run39 의 그 원본)
   - `+0x31` `RtlpExeSearchPath`
   - `+0x40` `RtlpSearchPath`
3. `+0x4f` 열쇠를 놓는다.
4. 돌려받은 블록을 `+0x6b` / `+0x87` / `+0xa3` `RtlFreeHeap` (각각 0 이 아닐 때만).

**`RtlpInvalidatePathCache` — "칸 비우기" 와 "종이 버리기" 는 다르다**

- `+0x5` — 칸 값을 읽고 **칸에 0 을 쓴다.** (책장에서 원본을 뺀다.)
- `+0xf ADD …,-1` · `+0x14 CMOVNZ` — 블록의 참조 수 (`[+0x50]`) 를 1 내리고, **0 이 됐을 때만** 그 블록을 돌려준다. (원본을 아무도 안 보고 있을 때만 버린다.)
- 그래서 **표지 `P` 가 아직 그 블록을 들고 있으면 블록은 안 풀린다** (결과서).
  - `P` 는 `LdrpReleaseDllPath` (`LdrpInitializeProcess +0x1bfb`) 때 내려놓는다.
  - [run39 해설 2.4](../02_sprint/run39-dll-search-path-해설.md) 에서 본 대로, `P+0` 은 원본 블록에 **끼운 책갈피**였다. 책갈피가 끼워져 있으면 원본을 못 버린다.

**그럼 언제 새로 찍나**

- 다음에 누가 주소록을 달라고 하면 `RtlpGetCachedPath +0x43 JZ` ("원본 없네") → 다시 계산 → **`+0x111 MOV [R14],RDI`** 로 다시 꽂아 둔다 (결과서 + flow-writer `.asm`).
- 켜지는 환경에서도:
  - `P` 가 이미 들고 있는 옛 목록은 **그대로**다.
  - 그 뒤 새로 시작하는 LoadLibrary 가 `LdrpInitializeDllPath` 로 `P` 를 0 으로 비운 뒤 (`+0x13 ~ +0x35`) 원본을 새로 찍어 **새 글을 본다** (결과서).
- 덤 `[정적]`: `LdrpComputeLazyDllPath` · `LdrpGetDllPath` · `LdrpInitializeDllPath` 세 함수엔 `RtlpSystemDirs` · `<Windows>` 글을 **읽는 줄이 없다**. 시스템 칸을 읽는 건 `RtlpComputePath` (`+0x456 / +0x524 / +0x53b`) 와 `RtlpSystem32Dirs` 쪽 (`+0x3ac` · `RtlpAddSystemPath +0x20`) 이다.

- 비유: 시스템 칸이 바뀌면 관리실은 책장의 인쇄 원본을 **빼서 치운다.** 그 자리에서 새로 인쇄하진 않는다. 다음에 누가 "주소록 주세요" 하면 그때 새로 찍는다. 이미 원본을 펴 보고 있는 사람 손에서는 빼앗지 않는다.

---

## (b) 비트 60~61 — "시스템 폴더 먼저" 스위치 (run39 정정)

### 2.6 비트 60~61 = 옵션 15 `PREFER_SYSTEM32` `[정적]`

**새 말 — 옵션 15 `PREFER_SYSTEM32`**: 프로세스 완화 옵션 표의 **15번** 이다 (PDB 열거형 이름 `PS_MITIGATION_OPTION_IMAGE_LOAD_PREFER_SYSTEM32`). 켜면 **시스템 폴더를 먼저** 찾는다.

- 비유: 안전 규칙 목록 15번 줄 "**모르는 건물 이름이 오면, 관리사무소 공식 창고부터 찾아볼 것**".

**왜 "15번" 인가 — 근거 → 그래서**

1. 근거 `[정적]`: 업무 수첩 `+0xa0` 은 커널이 `PspPrepareSystemDllInitBlock +0x12b / +0x138` 로 복사하는 **24바이트 완화 옵션 지도**다 (run32).
2. 근거 `[정적]`: 그 지도는 **칸 = 옵션 번호 × 4 비트** 다. 한 칸에서 2비트가 상태다.
3. 근거 `[정적]`: ntdll PDB 열거형 `PS_MITIGATION_OPTION` 에서 번호 15 가 `…_IMAGE_LOAD_PREFER_SYSTEM32` 다. 15 × 4 = 60.
4. "칸 = 번호 × 4" 가 맞는지 **다른 읽는 곳 4개**로 맞춰 봤다 (결과서):

| 번호 | 이름 | 비트 | 읽는 줄 |
|---|---|---|---|
| 3 | `HEAP_TERMINATE` | 12~13 | `LdrpInitializeProcess +0xa1f SHR 0xc` (run46) |
| 10 | `CONTROL_FLOW_GUARD` | 40~41 | `LdrGetProcedureAddressForCaller +0x68c SHR 0x28` |
| 17 | `LOADER_INTEGRITY_CONTINUITY` | 68~69 | `+0x4d2 SHR 4` |
| 19 | `RESTRICT_SET_THREAD_CONTEXT` | 둘째 8바이트 비트 12 | `RtlRestoreContext +0x16d BT 0xc` |

- 그래서 → 비트 60~61 = 옵션 15 `[정적]`.
- 값 이름 (`ALWAYS_ON` 등) 은 SDK 기억이라 `[추측: 이름]`. **코드가 보는 값은 1 뿐**이다. 0 · 2 · 3 이면 아무 영향 없다.
- ntdll 에서 이 두 비트를 읽는 곳은 **`RtlpComputePath +0x21` 하나**다 (`objdump` 로 수첩 주소 참조 전부를 찾음 — 결과서).

### 2.7 값이 1 이면 — 시스템 칸을 맨 앞으로 옮긴다 `[정적]`

`RtlpComputePath` (run39 의 "실제로 글자를 짓는 곳") 안에서, 차례대로:

1. `+0x21` / `+0x2b SHR 0x3c` / `+0x32 AND 3` — `R14` = (수첩 `+0xa0` >> 60) & 3.
2. 칸 코드 **2** (`RtlpSystemDirs`) 나 **7** (`RtlpSystem32Dirs`) 를 만나면, 크기를 더한 뒤 `+0x37a` / `+0x460 CMP R14B,1` 이 맞을 때 `[RSP+0x48]` 에 **그 글 주소를 적어 둔다** (`+0x397` / `+0x484`, 마지막 것이 이김).
3. 블록을 만든 뒤 `+0x135 TEST RDI,RDI` / `+0x138 JNZ +0x671` → `+0x671` 이 **그 시스템 폴더 글을 항목 0 번으로 먼저 복사**한다 (`+0x699` 메모리 복사).
4. 개수를 1 로 둔 채 (`+0x6a1`) 일반 고리로 간다.
5. 일반 고리에서는 코드 2 · 7 칸을 **건너뛰고** (`+0x613`) 나머지를 **원래 순서대로** 쓴다.

그래서 →

- 기본 목차 양식 줄 1 (`5 2 4 3 8`) 이면 → **`2 5 4 3 8`** 이 된다.
- 곧 "exe 폴더 → 시스템 → 현재 폴더 → PATH → 패키지" 가 "**시스템 → exe 폴더** → 현재 폴더 → PATH → 패키지" 로.
- 목록이 **한 줄로 줄어들지 않는다.**

- 비유: 안전 규칙 15번이 켜진 가게에선 주소록 목차 양식을 그대로 쓰되, **시스템 칸 한 장만 뽑아서 맨 앞에 끼운다.** 나머지 페이지 순서는 안 건드린다.

### 2.8 run39 는 무엇을 틀렸나

- run39 (와 그 해설 2.6 "안 탈 것 둘") 는 "1 이면 **목록 전체를 시스템 폴더 하나로** 바꾼다" 고 적었다.
- 지워진 flow.md 줄의 근거는 `RtlpComputePath +0x21`, `+0x138` 두 자리였다. 이번엔 그 뒤 `+0x37a` · `+0x460` · `+0x671` · `+0x699` · `+0x613` 까지 따라가서 **"맨 앞으로 옮기기"** 로 바로잡았다 `[정적]`.
- `[추측]` run39 는 `+0x138` 에서 따로 가는 갈래를 보고 "그 갈래가 목록 전부" 로 읽은 것 같다. 이유: 지워진 줄의 근거가 갈림 자리 (`+0x21`, `+0x138`) 뿐이고, 일반 고리로 **돌아오는** 줄 (`+0x6a1`) · 건너뛰는 줄 (`+0x613`) 은 그 줄에 없었다.
- 어디를 고쳤나:
  - run39 문서 맨 위에 정정 한 줄 (본문은 그대로).
  - [run39 해설](../02_sprint/run39-dll-search-path-해설.md) 맨 위에도 "고쳐 읽기 (run107)" 한 줄 (본문은 그대로).
  - flow.md 2.6 조건부 줄 (취소선 + 정정 줄), 4장 run39 Q71 줄.

**test.exe 는?**

- 사진 `ntdll+0x1ea4f0` (수첩 `+0xa0`) = 0 `[관찰]` (run46) → **안 탄다.**
- 그래서 실행 흐름 · 지도 결과는 안 바뀐다. 바뀐 건 "만약 켜졌다면" 의 설명뿐이다.

---

## (c) `DependentLoadFlags` — 모듈 파일이 직접 적어 온 값

### 2.9 쓰는 줄은 `LdrpCfgProcessLoadConfig` 하나 (+ 고쳐 쓰기 하나) `[정적]`

ntdll 전체에서 명부 줄 `+0x118` 을 만지는 줄을 `objdump` 로 다 찾았다 (결과서):

| 줄 | 하는 일 |
|---|---|
| **`LdrpCfgProcessLoadConfig +0xa7`** | **쓰기 (주 경로)** — 로드 구성 `+0x4e` (2바이트) & 마스크 (한 줄 읽기 ②) |
| `LdrpCfgProcessLoadConfig +0x69e` | **고쳐 쓰기** — 걸러 낸 값 ≠ 0 이고 `LdrpIsModuleUnderSystem32` 가 참일 때, exe 면 비트 9 (`0x200`), 아니면 비트 8 (`0x100`) 을 더함 |
| `LdrpMapDllSearchPath +0xc1` | 읽기 (부모 명부 줄, run39) |
| `LdrpValidateIntegrityContinuity +0x33` | 읽기 (`0x8000` 비트) |

- 다른 쓰는 줄은 **없다**. `LdrpCfgProcessLoadConfig` 를 부르는 곳도 `LdrpProcessMappedModule +0x145` 와 `AvrfMiniLoadDll +0x869` 둘뿐이다.
- 마스크 = `(((LdrpPolicyBits & 4) | 0x7b) << 8) | 0x8000`.
  - 사진 `LdrpPolicyBits = 0x16` (run65) → 비트 2 켜짐 → `0x7f00 | 0x8000` 쪽.

**값은 어디서 오나** — 근거 → 그래서 `[정적]`

1. `LdrpProcessMappedModule +0x107 LdrInitSecurityCookie` 가 다섯째 인자 `[RSP+0x88]` 에 **로드 구성 포인터**를 받아 온다.
2. `+0x13a MOV R8,[RSP+0x88]` 로 그걸 `LdrpCfgProcessLoadConfig` 에 넘긴다.
3. 그래서 `+0x8c` 가 읽는 `[RDI+0x4e]` = **그 모듈 PE 파일의 로드 구성 디렉터리 `DependentLoadFlags`** 다.

- 비유: 건물 설계도 뒤에 붙은 **보안 설정 서류** ([run63 해설 2.8](run63-cfg-func-table-해설.md)) 의 한 칸 "내 의존 건물은 이런 방식으로 찾아 줘" 를, 관리실이 명부 줄에 **베껴 둔다.** 관리실이 지어낸 값이 아니라 **건물이 들고 온 값**이다.

**값이 없을 때** `[정적]`

- 명부 줄은 `LdrpAllocateModuleEntry +0x23 / +0x29` 에서 0 으로 채워진 채 시작한다 (`HEAP_ZERO_MEMORY`).
- 로드 구성이 없거나 크기 < `0x94` 면 `+0x7a JZ` / `+0x86 JC` 로 나가서 안 쓴다 → **0 그대로**.

### 2.10 6 모듈 대조 — 코드 · 파일 · 사진 셋이 맞는다 `[정적+관찰]`

결과서가 PE 파일 `+0x4e` 를 직접 읽었고, flow-writer 가 사진 줄을 다시 읽었다.

| 모듈 | 파일 값 | 사진 P1b `+0x118` | 비트 8·9 갈래 |
|---|---|---|---|
| test.exe | 0 | 0 (`04_baggage-read_P1b.txt:3439`) | 걸러 낸 값 0 → 안 감 |
| ntdll | `0x800` | `0x800` (`:3544`) | 마스크 통과, System32 바로 아래 → 거짓, 안 더함 |
| KERNEL32 | 0 | 0 (`:3649`) | 안 감 |
| KERNELBASE | `0x800` | `0x800` (`:3754`) | ntdll 과 같음 |
| dll1 | 0 | 0 (`:3859`) | 안 감 |
| dll2 | 0 | 0 (`:3964`) | 안 감 |

- 6/6 맞는다. 이것으로 바이너리가 타깃 VM 것과 같다는 확인도 겸했다 (결과서).
- 그래서 run39 의 "test.exe · dll1 의 `+0x118` 은 0 일 것 `[추측: 링크 옵션]`" → **`[정적+관찰]`** (코드 · 파일 값 · 사진 셋).
- 이어서 run39 의 "dll1 · dll2 는 부모가 들고 온 주소록 `P` 를 그대로 쓴다 (`LdrpMapDllSearchPath +0xe1`)" 도 근거 하나가 단단해졌다. 부모 (exe · dll1) 값이 0 이라 마스크와 안 겹치기 때문이다.

### 2.11 덤 — `LdrpIsModuleUnderSystem32` 가 ntdll 에서 거짓인 까닭 `[정적]`

run63 이 남긴 질문이다. ntdll 은 분명 System32 안에 있는데 왜 "System32 아래 모듈이냐" 가 거짓이었나.

1. 이 함수는 먼저 **폴더 길이** = `FullDllName − BaseDllName` 을 구한다.
2. 그 길이가 `LdrpSystem32.Length + 2` 보다 **길 때만** (`+0x1a ~ +0x22 JA +0x2f`) 앞부분을 `LdrpSystem32` 와 비교한다.
3. 그래서 → **System32 의 하위 폴더 안 모듈만 참**이다.
   - ntdll · KernelBase 는 System32 **바로 아래**라 폴더 길이가 충분히 길지 않다 → 거짓 → `0x800` 그대로.

- 비유: "관리사무소 창고 **안쪽 방**에 있는 건물이냐" 를 묻는 함수였다. 창고 **입구 바로 앞**에 있는 건물 (ntdll) 은 "아니오".

---

## (d) `<Windows>` 글과 비트 29

### 2.12 `<Windows>` = `RtlGetNtSystemRoot()` → 공용 게시판 `[정적+관찰]`

**어디서 짓나** `[정적]`

- `LdrpInitializeProcess +0xcdc` (ret `+0xce1`) `RtlGetNtSystemRoot` → `+0xcec RtlInitUnicodeStringEx(&[RSP+0x120], 결과)` (ret `+0xcf1`).
- 이 `[RSP+0x120]` 이 `<Windows>` 다. 두 곳에 쓰인다:
  - System32 글 (`+0xd36` 이어 붙이기)
  - 시스템 칸 `RtlpSystemDirs` 의 `<Windows>\system;` · `<Windows>;` 두 조각 (`+0x1123 / +0x1146`)

**`RtlGetNtSystemRoot` 안** (`+0x0 ~ +0x35`, 전부 읽음) `[정적]`

1. `+0x0 MOV RAX,GS:[0x60]` → PEB.
2. `+0x9 MOV RAX,[RAX+0x90]` → **`PEB.SharedData`**.
3. 그게 0 이거나 (`+0x10` · `+0x13`) 첫 DWORD 가 0 이면 (`+0x1c` · `+0x1f`):
   - **`+0x15 MOV EAX,0x7ffe0030`** = 공용 게시판 (KUSD) 의 **`NtSystemRoot`** 칸 (PDB `_KUSER_SHARED_DATA +0x30`).
4. 아니면 (서버 silo) `+0x31 ADD RAX,0x1e` = `SharedData+0x1e` (PDB `_SILO_USER_SHARED_DATA +0x1e NtSystemRoot`).
   - silo = 상가 안의 **구역** ([run32 해설 2.11](../02_sprint/run32-params-initblock-해설.md)).

**test.exe 는** `[관찰]`

- `PEB.SharedData = (null)` (`04_baggage-read_P1b.txt:2462`) → 3번 갈래.
- KUSD `NtSystemRoot = "C:\WINDOWS"` (`04_baggage-read_P2.txt:4028`).
- 그래서 → **`<Windows>` = `C:\WINDOWS`**. run39 의 `[추측: NtSystemRoot]` 가 맞았다.

- 비유: 관리실은 "Windows 폴더 주소" 를 직접 외우지 않는다. 먼저 **구역 게시판** (`SharedData`) 이 있나 보고, 없으면 **건물 로비 공용 게시판**에 적힌 주소를 그대로 옮겨 적는다.

### 2.13 `Flags & 0x20000000` = run32 비트 29, 같은 칸 · 같은 비트 `[정적]`

- **ntdll 이 읽는 줄 넷** (`0x20000000` 상수로 전부 찾음):
  - `LdrpInitializeProcess +0xfdb` · `+0x10bd` (둘 다 `TEST [RAX+0x8],0x20000000`, `RAX = PEB.ProcessParameters`)
  - `LdrpInitializeKernel32Functions +0x21a` (run20 의 "kernel32 찾기 건너뜀")
  - `RtlpComputeDllPathWithOptions +0x92` (2.14)
  - 전부 `[PEB+0x20]+0x8` = PDB `_RTL_USER_PROCESS_PARAMETERS +0x8 Flags`.
- **커널이 켜는 줄**: `PspSetupUserProcessAddressSpace +0xe6 BTS EBX,0x1d` → `+0xea MOV [RBP+0x8],EBX` (조건 `+0xdd CMP [RAX+0x3f8],0` = `OneCoreForwardersEnabled`).
- 같은 오프셋 `+0x8`, 같은 비트 29 → **같은 것**이다.
- 켜지면: 시스템 칸에 `<Windows>\System32\forwarders\alt;` (`LdrpIncludeAlternateForwarders` 참일 때) · `…forwarders;` 가 붙고, 옵션 있는 검색의 넷째 인자 (forwarders 포함) 가 플래그 비트 11 (`0x800`) 일 때 1 이 되고, `LdrpInitializeKernel32Functions` 는 kernel32 찾기를 건너뛴다 (run20).
- test.exe: 개업 서류 `Flags = 0x4001` `[관찰]` (`ms/logs/04_baggage-read_K1.txt:2337`, run104 · run65) → 비트 29 꺼짐 → **안 붙는다.**

- 비유: run32 해설에서 본 "**관리사무소가 특수 구역 가게에만 찍어 주는 표시**" 가, 관리실 업무 매뉴얼 네 군데에서 "이 표시가 있으면…" 으로 읽히는 **바로 그 표시**라는 게 확인됐다.

---

## 작은 것 셋

### 2.14 옵션 있는 검색 — 칸 차례 `5 → 1 → 6 → 0 → 7 → 8` `[정적]`

**새 말 — 옵션 있는 검색**: 표지 `P+0x18` 플래그가 0 이 아닐 때 타는 검색 길이다 (`LdrpGetDllPath` 가 `…WithOptions` 짝을 고름). **test.exe 초기화 길엔 없다** (run39 — 우리 플래그 0).

- 비유: 보통 주소록 대신, 부탁하는 사람이 "**이 칸들만 봐 줘**" 하고 체크해 온 **맞춤 주소록**.

`RtlpComputeDllPathWithOptions` (rva `0xba650`, 315바이트) 가 플래그를 보고 칸 코드를 쌓는 차례:

| 순서 | 플래그 비트 | 쌓는 코드 | 뜻 |
|---|---|---|---|
| 1 | 8 (`0x100`) | **5** | 응용 프로그램 폴더 |
| 2 | 9 (`0x200`) | **1** | exe 폴더 |
| 3 | 10 (`0x400`) | **6** (+ SetDllDirectory 폴더가 있으면 **0**) | `AddDllDirectory` 목록 (+ SetDllDirectory 폴더) |
| 4 | 11 (`0x800`) 또는 14 (`0x4000`) | **7** | `RtlpSystem32Dirs` (System32 하나) |
| 5 | (플래그 아님) 패키지 글이 있으면 | **8** | `LdrpAppPackagesPath` |

- 코드 **2** (시스템 칸 덩어리) · **3** (`PATH`) · **4** (`.`) 는 **절대 안 들어간다** → 옵션 있는 검색은 PATH · 현재 폴더를 안 본다 `[정적]`.
- 캐시도 안 쓴다: 플래그 ≠ 0 이라 `RtlpGetCachedPath +0x27 JNZ` 로 **매번 새로 계산**하고 저장도 안 한다.
  - 그래서 `RtlpDllSearchPathWithOptions` 칸은 이 길로는 안 채워지는 것으로 보인다 `[정적 추정]`. 그러면 2.4 의 `LdrAddDllDirectory +0x1e8` 0 쓰기는 늘 빈 칸에 쓰는 것이다.
- 들어오는 자리 (run 문서가 결과서를 바로잡은 곳): `LdrpGetDllPath +0x17f` 은 `LEA` (주소 준비) 이고, 실제 call 은 `+0x192 JMP` 로 돌아가 기본 길과 **같은 `+0xa8`** 이다.

### 2.15 `.` 칸은 글자 `.;` 일 뿐 — 현재 폴더로 푸는 건 나중 `[정적]` + `[추측]`

- 기본 목록의 `.` 은 `RtlpComputePath +0x270 ~ +0x287` 이 **글자 `.;` 만** 써 넣는다. 그 함수엔 `CurrentDirectory` 를 읽는 줄이 없다 `[정적]`.
- 현재 폴더로 풀리는 건 후보를 **전체 경로로 만들 때**다:
  - `LdrpResolveDllName` → `LdrpGetFullPath` → `RtlGetFullPathName_Ustr` 안에 `[PEB+0x20]+0x38` (`CurrentDirectory`) 를 읽는 줄이 `+0xeed` · `+0xf1f` · `+0x1059` 셋 있다 `[정적]`.
  - 셋 중 어느 갈래인지는 안 읽었다 `[추측: 상대 경로 갈래]`.
- 비유: 주소록엔 "**여기**" 라고만 적혀 있다. "여기가 어디냐" 는 실제로 찾아갈 때 그 순간 서 있는 곳을 보고 푼다.

### 2.16 표지 `P+0x28 ~ +0x77` 을 읽는 곳 — 못 찾음 `[정적]` / `[공백]`

- 쓰는 곳은 안다: `LdrpGetDllPath +0xca ~ +0xec` 가 원본 블록 앞 `0x50` 바이트를 `P+0x28` 로 복사한다.
- 그런데 `P` 를 받는 함수 일곱 (`LdrpSearchPath` · `LdrpReleaseDllPath` · `LdrpMapDllSearchPath` · `LdrpFindLoadedDllInternal` · `LdrpFindLoadedDll` · `LdrpInitializeDllPath` · `LdrpComputeLazyDllPath`) 을 다 따라가도 **읽는 줄이 없다** `[정적]`.
- 다른 함수가 `P` 를 레지스터로 받아 읽을 가능성은 `[공백]` → Q195 ④.

---

### 2.17 run 문서가 결과서와 다르게 적은 곳 — run 문서를 따랐다

1. **칸 이름**: 결과서 `gpTermsrvGetWindowsDirectoryW` → **`pfnTermsrvGetWindowsDirectoryW`**.
   - 같은 칸 (rva `0xc1d10`) 의 이름이 kernel32 `.asm` 주석 · sast/97 결과서 · run101 에서 `pfn…` 이다.
   - 결과서의 `gp…` 는 옆 칸들 (`gpTermsrvGetComputerName` 등) 이름에서 옮겨 온 것으로 보인다 `[추측]` (run 문서).
2. **`RtlpDllSearchPath` 에 쓰는 줄**: 결과서 `LdrpGetDllPath +0xa1` 은 **`LEA` (주소만 넘김)** 이다. 실제로 칸에 쓰는 줄은 **`RtlpGetCachedPath +0x111 MOV [R14],RDI`** 다 (열쇠 `+0x107` 안, `+0x10c` 로 칸이 비었을 때만).
   - 짐 지도 3.8 의 `RtlpDllSearchPath` 줄에 이 근거를 붙였다. 다만 "짐작 → 정확" 으로 올리진 않았다 (3절).
3. **옵션 호출 자리**: 결과서 `LdrpGetDllPath +0x17f` → 그건 `LEA`, call 은 `+0xa8` (2.14).
4. 결과서가 적은 문서 줄 번호 (`baggage.md` · `flow.md`) 는 그 뒤 바퀴들이 줄을 더해서 밀렸다. 내용 기준으로 고쳤다.

### 2.18 정리 — 비유로 한 번 더

- `+0x1b77` 의 사람은 "손님 전용 시스템 창고가 있나요?" 하고 kernel32 에 전화한다. kernel32 는 수첩 칸이 비어서 "없어요 (0)" 하고 끊는다. 그 사람은 **아무것도 안 하고** 돌아간다. 주소록 · 인쇄 원본 · 추가 주소 메모 모두 그대로다.
- 만약 창고가 있었다면 시스템 칸 끝에 한 줄 더 적고, 책장의 인쇄 원본을 **빼서 치우고** (다시 안 찍음), 추가 주소 메모에도 적는다. 그때 `LdrpSystem32` 하나는 버린 종이를 계속 가리킨다.
- 안전 규칙 15번 (비트 60~61) 은 "시스템 칸을 **맨 앞으로**" 다. 주소록이 한 줄로 줄어드는 게 아니다 (run39 정정). test.exe 는 꺼짐.
- 명부 줄 `DependentLoadFlags` 는 건물이 **보안 설정 서류에 적어 온 값**을 베낀 것이다. test.exe · dll1 · dll2 는 0.
- `<Windows>` 는 로비 공용 게시판의 `C:\WINDOWS`. 비트 29 는 run32 의 "특수 구역 표시" 와 같은 것이고 test.exe 는 꺼짐.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 — 그림

- **그림 2 메모 한 마디**만 바뀌었다. 노드 · 화살표는 그대로다.

| 자리 | 전 | 후 |
|---|---|---|
| 그림 2 메모 "목록 짓기는 … 한 번만 짓는다" 줄 | "(부모 `DependentLoadFlags` 가 0 일 때)" | "(부모 `DependentLoadFlags` 가 0 일 때 — run107: 0 은 코드 `LdrpCfgProcessLoadConfig +0xa7` · 파일 로드 구성 `+0x4e` · 사진 P1b 셋으로 `[정적+관찰]`)" |

### flow.md 에서 바뀐 것 — 글

| 절 | 전 | 후 |
|---|---|---|
| 2.4 뒤7 트리 `+0x1b77` 줄 | "뜻 모름. `RtlpSystemDirs` · `RtlpSystem32Dirs` 를 다시 쓰는 줄이 있음" | `[정적]` `TermsrvGetWindowsDirectoryW` 가 0 → `+0x4e` 에서 0 반환, 아무것도 안 바꿈. 0 이 아닐 때만 다시 쓰기 · 캐시 비우기 · `AddDllDirectory` |
| 2.4 앞쪽 표 9 (시스템 경로) | `<Windows>` 출처 없음 | + `<Windows>` = `+0xcdc RtlGetNtSystemRoot` (KUSD `C:\WINDOWS` `[관찰]`), `Flags & 0x20000000` = run32 비트 29 와 같은 칸 |
| 2.4 ntdll 항목 마무리 절 | — | + `DependentLoadFlags` 쓰는 줄은 둘뿐 (`+0xa7` · `+0x69e`), `LdrpIsModuleUnderSystem32` 는 하위 폴더만 참 → run63 ③ 닫힘 |
| 2.5 `TermsrvGetWindowsDirectoryW` 칸 줄 | 칸 0 그대로 (run101) | + 몸통 다섯 줄 → 0 반환 → 받는 쪽 `+0x4e` 안 탐 (칸 값은 `[VM]`) |
| 2.6 파일로 올리는 길 `DependentLoadFlags` 줄 | `[추측: 링크 옵션]` | `[정적+관찰]` (코드 · 파일 · 사진 셋) |
| 2.6 검색 경로 조건부 줄 | "비트 60~61 이 1 이면 목록 전체가 시스템 폴더 하나로" `[정적]` | 취소선 + **정정** `[정적]`: 옵션 15 `PREFER_SYSTEM32`, 1 이면 시스템 칸이 맨 앞으로 |
| 2.6 검색 경로 절 (새 줄) | — | + `<Windows>` 출처 · 비트 29 · 옵션 있는 검색 차례 · `.` 칸 · `P+0x28` 못 찾음 |
| 2.6 `P` 공유 줄 | 부모 `+0x118` 0 은 `[추측: 링크 옵션]` | `[정적+관찰]` |
| 2.6 `LdrpInitializePerUserWindowsDirectory` 줄 | "무엇을 바꾸는지 · test.exe 에서 타는지 `[공백]`", "`LoadLibraryExW` ×3 에 영향이 있을 수 있다 `[추측]`" | 둘 다 취소선. **Q71 닫힘**: test.exe 에선 아무것도 안 바꿈 `[정적]`, 탔다면 무엇을 하는지 |
| 4장 | run39 Q71 줄 · run63 ③ · run101 ① 열림 | 셋 다 "해결됨", + run107 새 줄 셋 (동적 · 정적 · PE_emulator) |

### 짐 지도 `baggage.md` 에서 바뀐 것

- **새 짐 칸 없음. 칸 수 · 줄 수 · 2장 숫자 그대로.** 고친 줄 5개뿐이다.
  - 3.8 `RtlpSystem32Dirs +0x8` · `RtlpSystemDirs` 둘째 줄: "뜻 모름" → **뜻 정해짐 · 조건부 (test.exe 안 탐)**.
  - 3.8 `RtlpDllSearchPath`: 쓰는 줄 근거 (`RtlpGetCachedPath +0x111`) 를 붙임. **태그는 짐작 그대로** — 올릴지는 다음 바퀴 몫 (4절).
  - 3.5 `DependentLoadFlags`: 0 의 근거 셋.
  - 3.9 `LdrSystemDllInitBlock`: `+0xa0` 비트 읽는 곳 전수.
- 새 7.38 — 이번 바퀴가 넘긴 질문 모음.

### 큰 그림에서 어떻게 이어지나

- **초기화 쪽**: `+0x1b77` 한 줄이 "뜻 모름" 에서 "**test.exe 에선 빈 호출**" 로 바뀌었다. `LdrpInitializeProcess` 뒤쪽에 남은 모르는 자리가 하나 줄었다.
- **진입점 뒤쪽**: CRT 가 부르는 `LoadLibraryExW` ×3 에 `+0x1b77` 이 주는 영향은 **없다** `[정적]`. test.exe 에선 그 사이 시스템 칸 · 인쇄 원본을 바꾸는 사람이 없기 때문이다.
  - 다만 그 셋이 **어느 주소록**을 펴는지는 이번 근거에 없다. 플래그를 주면 옵션 있는 검색 (2.14) 이 불릴 수 있고, 플래그 0 이면 안 불린다 (run 문서 "PE_emulator 쪽" 5).
- **검색 경로 이야기 전체** (run26 → run39 → run107):
  - 주소록은 처음 펼칠 때 짓고 (run39), 첫 칸은 exe 폴더 (run39), `<Windows>` 는 공용 게시판 `C:\WINDOWS` (이번), 시스템 칸이 앞으로 오는 스위치는 꺼짐 (이번), test.exe 에선 짓고 나서 시스템 칸을 바꾸는 사람도 없다 (이번).
  - 이제 남은 건 **실행 때 값** 뿐이다 (Q23 · Q46 · Q79).
- **PE_emulator 쪽** (run 문서 요약 — PE_emulator 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정):
  - (a) 빈 몸통 그대로 둬도 **결과가 같다**. 주석 "본문 asm 이 없어" 만 낡았다.
  - (b) 24바이트를 0 으로 두는 것도 맞다 (사진도 0). 주석의 "알려진 읽기" 목록에 몇 줄이 빠졌을 뿐.
  - (c) `+0x118` 은 진짜 코드가 쓰니 맞을 것이고, 6 모듈 표로 한 번 맞춰 볼 가치가 있다.
  - (d) 맞음. 5 (옵션 있는 검색) 는 확인 필요.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 (동적)

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| kernel32 칸 `pfnTermsrvGetWindowsDirectoryW` (`kernel32+0xc1d10`) 가 정말 0 인가 — 이 바퀴의 핵심 조건 한 칸 | run44 사진은 ntdll `.data` 만 읽었다. kernel32 `.data` 는 안 읽었다. 갈래 값 (`SuiteMask` 등) 은 0 쪽과 맞지만 칸 자체는 못 봤다 | **Q79** — 이미 있는 사진 덤프 (P1b · P2) 에서 `dq kernel32+0xc1d10 L1` 을 오프라인으로. 새 실행 없이 닫힌다 (run101 의 `+0xc1d08` 읽기와 같은 방법) |
| `ntdll+0xfb098` (`+0x4c`) 에서 `rax` = 0 이고, `ntdll+0xfb1a3` · `0xfb1b8` · `0xfb1c7` (`+0x157` · `+0x16c` · `+0x17b`) 엔 **안 서는가** | 정적은 길만 안다 | **Q46** (실행 값, 멈춤점은 test.exe 조건) |
| `RtlpSystemDirs` (`dq ntdll+0x1d2360 L2`) · `RtlpDllSearchPath` (`dq ntdll+0x1d3050 L1`) 가 `+0x1b72` 앞뒤로 같은가 | 짐 지도 3.8 사진은 `≤P1a` 까지만 좁혔고, 그 뒤 값 비교는 안 했다 | **Q46** (P1b · P2 사진 값 비교로도 된다) |
| `RtlGetFullPathName_Ustr` 의 `CurrentDirectory` 읽기 3곳 중 test.exe 가 실제로 타는 곳 | 정적으로도 아직 갈래를 안 읽었다 (아래 ①) | **Q46** |

- run 문서가 뺀 것: 결과서 동적 4 (`+0xa0` 비트 60~61 = 0, `+0x118` 값) 는 사진 (run46 · run44 P1b) 으로 이미 닫혔다.

### 정적으로 남은 것

| 무엇 | 왜 남았나 | 대기열 |
|---|---|---|
| ① `RtlGetFullPathName_Ustr` (`0x18003f3f0`) `+0xeed` / `+0xf1f` / `+0x1059` — 어느 갈래에서 `CurrentDirectory` 를 읽나, `.\name` 같은 상대 경로가 어떻게 풀리나 | 이번 질문 밖이라 읽는 줄만 찾고 갈래는 안 읽었다 (2.15) | **Q195 ①** |
| ② `LdrpInitializePolicy` 의 `LdrpPolicyBits` 비트 1 · 2 · 4 가 각각 무슨 정책인가 (사진 `0x16`) | 이번 코드가 `& 1` (Windows 두 조각 빠짐) · `& 4` (`AddDllDirectory` · 마스크 `0x7f00`) 를 쓰는 건 봤지만, 정책 이름은 그 함수 몫이다 | **Q195 ②** |
| ③ (선택) `RtlGetSearchPath` · `RtlGetExePath` 의 목록 코드 (원본 `RtlpSearchPath` · `RtlpExeSearchPath`) | 이번엔 "비워진다" 까지만 봤다 | **Q195 ③** |
| ④ `P+0x28 ~ +0x77` 을 레지스터로 받아 읽는 다른 함수 | `P` 를 받는 함수 일곱엔 없었다. 그 밖은 `[공백]` (2.16) | **Q195 ④** |

### 사용자가 정할 것 — 짐 지도 셈

- 이번에 **쓰는 줄이 보인** `.data` 전역 넷이 있다: `RtlpExeSearchPath` · `RtlpSearchPath` · `RtlpDllSearchPathWithOptions` (0 쓰기, 조건부 안 탐) · `LdrpUserDllDirectories` (`LdrAddDllDirectory +0x1b5`, 조건부 안 탐).
- 이들을 짐 지도 7.3 "모름" 에서 3.8 로 **옮길지** 는 2장 숫자가 바뀌는 일이라, 다른 바퀴 (sast/111 · 113) 의 같은 질문과 묶어 **사용자 결정 대기**다 (plan 결정 기록).
- `RtlpDllSearchPath` 짐작 → 정확 (쓰는 줄 `RtlpGetCachedPath +0x111`) 은 plan 에 "다음 flow-writer 가 올림 (오케스트레이터 기본값)" 으로 적혀 있다.

---

## 5. 더 읽기

- run 문서: [`run107-search-path-small.md`](run107-search-path-small.md) — 특히 "(a) ~ (d)", "5", "사진 로그 대조", "결과서와 다르게 적은 곳", "정정"
- 요청서 · 결과서: [`_workspace/sast/102_search-path-small.md`](../../_workspace/sast/102_search-path-small.md) · [`102_search-path-small_result.md`](../../_workspace/sast/102_search-path-small_result.md)
- flow.md: [그림 2 메모](flow.md), 2.4 (뒤7 `+0x1b77` · 앞쪽 표 9 · ntdll 항목 마무리), 2.5 (`TermsrvGetWindowsDirectoryW` 칸), 2.6 (파일로 올리는 길 · 검색 경로 절), 4장 run107 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.5 · 3.8 · 3.9 · 7.38
- 앞 바퀴 짝:
  - [run39 해설](../02_sprint/run39-dll-search-path-해설.md) — 검색 경로 짓기 전체. 2.6 "안 탈 것 둘" 의 비트 60~61 설명은 **이번에 정정** (그 해설 맨 위 "고쳐 읽기")
  - [run101 해설 2.5 ~ 2.7](run101-k32-leftovers-해설.md) — `pfnTermsrvGetWindowsDirectoryW` 칸이 0 그대로인 까닭 (TS 인식 스티커)
  - [run32 해설 2.9 · 2.11](../02_sprint/run32-params-initblock-해설.md) — 업무 수첩 `+0xa0` 안전 규칙, 개업 서류 비트 29
  - [run63 해설 2.8](run63-cfg-func-table-해설.md) — 로드 구성 · `LdrpCfgProcessLoadConfig` 첫 소개
