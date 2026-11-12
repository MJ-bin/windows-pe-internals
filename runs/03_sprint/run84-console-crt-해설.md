# run84 console-crt — 해설

> 이 글은 [run84 문서](run84-console-crt.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q149b** — PE_emulator 요청 27 (콘솔 입출력) 의 **CRT · condrv 쪽과 최소 조건** (`_workspace/plan.md`, 이번에 **끝**)
  - 짝 바퀴 [run83](run83-console-kernelbase-해설.md) (Q149a, kernelbase 쪽) 과 합쳐 **요청 27 이 다 끝났다**. 이번이 요청 27 의 마지막 바퀴다.
  - 다음 항목: **Q150a** (요청 28 의 user32 쪽) → Q150b → Q151a → Q151b → Q152 → Q142 → [멈춤].
  - 이번에 남은 것이 간 곳 (4절):
    - 실행해 봐야 아는 것 → **Q104** (VM 단계)
    - 작은 정적 → 새 **Q159** (① 이 가장 중요 — sample `/MT` CRT 가 은퇴 앞 syscall 을 내나)
    - `/MD` 콘솔 새 샘플 → 새 **Q160** (만들지는 사용자 결정)
- 근거:
  - run 문서 [`run84-console-crt.md`](run84-console-crt.md) (가장 중요)
  - 요청서 [`_workspace/sast/81_console-crt.md`](../../_workspace/sast/81_console-crt.md) · 결과서 [`81_console-crt_result.md`](../../_workspace/sast/81_console-crt_result.md)
    - 판정 **분석성공**. 질문 1 ~ 5 모두 `.asm` 줄로 답했다. ucrtbase · condrv 를 **처음 analyze** 했다. 스크립트 문제 없음.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 2.8 `Log` 절 한 줄 · **3장 "콘솔 입출력" 절 아래 새 하위 절** · 4장 · 5장 (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): 근거만 (새 짐 칸 없음, 마흔째판)
  - 같이 한 일: **run83 뒷정리** — 사실이 틀린 건 아니고, 너무 넓게 적은 한 줄을 다듬었다 (2.14).
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없다.
  - CRT 는 진입점 뒤, condrv 는 커널 드라이버라 run44 사진에 값을 맞춰 볼 칸이 없다 (run 문서 "방법").
  - `[관찰]` 은 run83 이 옮긴 run44 값 (`ConsoleHandle` 4 → `0x60`, 표준 핸들 `0x68` · `0x6c` · `0x70`) 만 다시 썼다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이번 길을 안 탄다.**
   - test.exe · dll1 · dll2 는 `/MT` 빌드다 (`sample/build.bat:12`) `[소스]`.
   - 콘솔 출력은 CRT `printf` 가 아니라 `Log` 의 `WriteFile` 직접 호출이다 (`sample/log.h:38`) `[소스]`.
   - 이번에 읽은 CRT 는 **ucrtbase.dll** 이다. `/MD` 로 빌드한 프로그램이 쓰는 CRT 다 (run 문서 "호출 관계").
   - 그러니 이번 CRT 이야기는 PE_emulator 의 실험 대상 7천 개 — **CRT `printf` · `puts` 를 쓰는 콘솔 프로그램** — 쪽이다 (결과서 한 줄 결론 1).
   - sample 안 `/MT` CRT 에도 같은 이름 함수가 있다는 것까지만 봤다 (이름만, 본문 안 읽음 — 2.13).
2. **"콘솔이면 `WriteConsoleW`" 는 틀렸다.**
   - 요청서의 가정이었다. 기본 `printf` 는 콘솔이어도 `WriteFile` 이다 (2.6).
3. **−2 가 두 군데 나온다. 서로 다른 것이다.**
   - run83 의 `ConsoleHandle = −2` — 부모가 개업 서류 손잡이 칸에 적는 **신호** (kernelbase 가 봄).
   - 이번의 `osfhnd = −2` · `FILE._file = −2` — CRT 가 자기 장부에 적는 "**연락처 없음**" 표시 (`_NO_CONSOLE_FILENO`, 2.4 · 2.8).
4. **이름은 거의 다 `[추측: 이름]` 이다.**
   - ucrtbase · condrv PDB 에 **타입이 없다** (`pdb_struct.py --list` 0개).
   - 그래서 장부 칸 이름 (`osfhnd` · `osfile` · `pmode`), 딱지 이름 (`FDEV` · `FPIPE` …), IOCTL 이름은 짐작이다.
   - 대신 **칸 위치 · 값은 `.asm` 의 `[정적]`** 이다.
5. **flow.md 3장 새 하위 절의 비유는 이 해설과 그림이 다르다.**
   - flow.md: "우편함마다 안내 데스크냐 창고냐 묻고 이름표를 붙인다".
   - 이 해설: run83 해설 그림 (콘솔 상담실 · 우체통 · 창고 · 관) 을 이어 쓰고, CRT 쪽은 **장부 · 딱지** 로 부른다 (아래).
   - 그림만 다르고 사실은 같다.

이미 푼 말은 **링크만** 건다.

- 가게 · 관리사무소 · 신청 창구 · 건물 (KERNELBASE) · 전원 켜기 · 개업 서류 · 입 · 귀 · 비상 확성기 · 옆집 (conhost) · 콘솔 상담실 (`ConDrv`) · 주문서 (IOCTL) · 서류 봉투 (EA) · 소개장 · 출입증 · 우체통 (`WriteFile`) · 창고 (파일) · 관 (파이프) → [run83 해설 "비유"](run83-console-kernelbase-해설.md#비유--그대로-이어-쓴다)
- 콘솔을 얻는 갈래 (붙기 a · 새 콘솔 b · 없음 c), `ConsoleHandle` 0 · −1 · −2 · −3 → [run83 해설 2.1](run83-console-kernelbase-해설.md#21-큰-그림-먼저--문-하나-갈림길-둘) · [2.4](run83-console-kernelbase-해설.md#24--갈림길-②--손잡이-칸-값-넷-그리고-0--조용히-성공-이-왜-틀렸나)
- `WriteFile` 엔 콘솔 갈림이 없다, test.exe 의 `Log` → [run83 해설 2.10](run83-console-kernelbase-해설.md#210-writefile-엔-콘솔-갈림이-없다--testexe-의-log-와-손잡이-0-질문-4--5)
- `GetFileType` 과 `DeviceType` (`0x50` → CHAR) → [run83 해설 2.11](run83-console-kernelbase-해설.md#211-getfiletype--이-손잡이-뒤가-무슨-장치-질문-3)
- 드라이버 = 관리사무소 안 **전문 상담실**, IOCTL = **번호 붙은 주문서**, IRP = **배달 봉투**, 드라이버 객체의 처리 함수 칸 (`MajorFunction`) → [run66 해설 2.7](run66-apphelp-cache-해설.md)
- CRT = 현관 안에서 **짐 푸는 법**, `/MT` = 짐 푸는 도구를 **집 안에 다 갖고 있는** 빌드 → [run30 해설 2.1](../02_sprint/run30-exe-crt-start-해설.md#21-먼저--지도의-경계가-정해졌다)
- DLL = **연장통** (run30 해설 2.8), dll 의 CRT **준비 담당** (`dllmain_crt_process_attach`) → [run40 해설 2.8](../02_sprint/run40-dll-crt-entry-해설.md)
- 이유 1 = DLL 이 프로세스에 처음 붙음 → [run25 해설](../02_sprint/run25-dll-entry-해설.md)
- KUSD = **공용 게시판** → [run70 해설 "비유"](run70-cpu-features-해설.md#비유--그대로-이어-쓴다)
- TEB = 직원 **이름표**, 로케일 (TEB `CurrentLocale`) = 이름표에 적힌 사용 언어 → [run83 해설 2.6](run83-console-kernelbase-해설.md#26--run68-정정--넷째-syscall-어느-나라-말-써요)

### 비유 — 그대로 이어 쓴다

- 가게 · 관리사무소 · 신청 창구 · 건물 · 개업 서류 · 옆집 · 콘솔 상담실 · 주문서 · 우체통 — run83 해설 그대로.
- 개업 서류의 표준 핸들 칸 셋 = **입** (출력) · **귀** (입력) · **비상 확성기** (에러).
  - 손잡이 뒤가 상담실이면 콘솔, **창고** 면 파일, **관** 이면 파이프.
- CRT = 현관 안에서 짐 푸는 법 (run30). 이번에 둘을 더 붙인다.
  - **`/MD`** = 짐 푸는 도구를 집에 안 들이고 **공용 연장통** (ucrtbase.dll) 에서 꺼내 쓰는 집.
  - CRT 의 **장부** = 줄 번호 (fd) 마다 "진짜 손잡이 번호" 와 "**딱지**" 를 적어 둔 연락처 장부. 딱지 = "이 줄은 상담실 줄" · "이 줄은 관" 같은 표시 (2.3).
- condrv 안 (2.9):
  - 상담실 입구의 **창구 이름 안내판** (열기 이름 12칸).
  - 상담실이 다루는 **줄의 종류** 여섯 (손님 입 · 귀 줄, 출입증 줄, 옆집 쪽 줄, 참조 번호표 …).

### 한 줄로 먼저

- **CRT 는 시작 때 표준 핸들마다 두 가지만 묻는다** `[정적]`.
  - `GetStdHandle` (syscall 없음) 과 `GetFileType` (= `NtQueryVolumeInformationFile`). `GetConsoleMode` 는 안 부른다.
  - 답은 장부 딱지로 적는다: 상담실 (CHAR) → `0x40`, 관 (PIPE) → `0x08`.
  - 손잡이가 0 · −1 이면 `GetFileType` 도 안 부르고 "연락처 없음 (−2)".
- **기본 (C 로캘) `printf` 는 콘솔이어도 `WriteFile` → `NtWriteFile`** `[정적]`.
  - `WriteConsoleW` 는 UTF-8 · UTF-16 모드를 켰을 때만, 그것도 CRT 가 따로 연 `CONOUT$` 로 글자 하나씩.
- **표준 핸들이 0 이면 CRT 는 조용히 버린다** `[정적]`.
  - 시작 성공, `printf` 도 실패 없이 돌고, 버퍼를 비울 때 `errno 9` · −1. abort 없음. `NtWriteFile` 은 0번.
- **test.exe 의 `NtWriteFile(0)` 26번은 CRT 가 아니다** `[소스]` + `[정적]` — `Log` 의 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)`.
- **condrv 쪽도 맞물린다** `[정적]`.
  - `\Device\ConDrv` 는 `DeviceType 0x50` 으로 만들어진다.
  - 앱 쪽이 보내는 IOCTL 셋 (`0x500016` · `0x500023` · `0x500037`) 과 열기 이름이 kernelbase 가 보내는 값과 같다.
  - 나머지 IOCTL 은 conhost 몫 `[추측]`.
- 그래서 요청 27 결론: **쓰기는 거의 다 `NtWriteFile` 하나, 막히는 곳은 시작** (run83). PE_emulator 가 고를 길은 둘이다.
  - (가) 파일 리다이렉트처럼 — **`ConsoleHandle = −1` 이 꼭 있어야** 한다.
  - (나) 가짜 condrv — 지금 값 (`ConsoleHandle 0`) 그대로 가는 **길 B** 가 가장 가깝다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
(부팅 때) condrv DriverEntry — "\Device\ConDrv" 상담실 문 달기              ← 이번 ⑤ (2.9)

cmd.exe 의 부탁 → 커널 NtCreateUserProcess → … → ntdll 로더
 │
 ├─ KERNELBASE 전원 켜기 → ConsoleInitialize (run68 · run83)            ← 막히는 곳은 여기 (2.10)
 ├─ dll2 · dll1 CRT 준비 (dllmain_crt_process_attach)                   ← /MT CRT 사본도 표준 핸들을 묻나? [추측] (2.13)
 ├─ dll2 TLS 콜백 · DllMain … 의 Log → WriteFile → NtWriteFile (run83)   ← test.exe 의 26번 (2.8)
 │
 → NtContinue 은퇴 → 진입점 → CRT 준비 → main
                       │         └─ 표준 핸들 종류 묻기                     ← 이번 ① (2.3 · 2.4)
                       └─ printf → … → _write → WriteFile → NtWriteFile     ← 이번 ② ③ ④ (2.5 ~ 2.8)

PE_emulator 가 고를 길 (가) · (나)                                         ← 이번 ⑥ (2.10 ~ 2.12)
```

- 이번 이야기는 대부분 **진입점 뒤** 다.
  - 흐름도 범위 밖이라 그림 2 엔 안 넣었다. flow.md 3장 "콘솔 입출력" 절 아래 **새 하위 절** 에 적었다.
- 은퇴 앞에 걸리는 건 둘이다.
  - 막히는 곳 = KERNELBASE 전원 켜기 안 `ConsoleInitialize` (run83 이 본 곳).
  - dll2 · dll1 의 `/MT` CRT 준비가 표준 핸들을 물을지도 모른다는 새 질문 `[추측]` (Q159 ①).

#### 내 짐작 하나 — `/MD` 면 "표준 핸들 묻기" 가 은퇴 앞일 수 있다 `[추측]`

run 문서와 flow.md 는 CRT 를 "진입점 뒤" 로 묶어 적었다. 그런데 ucrtbase 쪽은 조금 다를 수 있다. 이유를 단계별로:

1. ucrtbase 의 시작은 `entry` → `DllMainDispatch` **이유 1** 이다 `[정적]` (run 문서 "호출 관계").
2. 이유 1 = DLL 이 프로세스에 처음 붙을 때다 (run25 해설).
3. 우리 지도에서 import 로 들어온 DLL 의 시작 함수 (KERNELBASE, dll2 · dll1) 는 모두 **은퇴 앞** 에 불렸다 (flow.md 2.5 · 2.8).
4. 그래서 `/MD` 프로그램이면 ucrtbase 의 "표준 핸들 묻기" 도 진입점 뒤가 아니라 **은퇴 앞 DLL 전원 켜기 때** 일 수 있다 `[추측]`.
5. 아직 확인 아님. sample 은 `/MT` 라 지금 test.exe 로는 볼 수 없다. `/MD` 새 샘플 (Q160) 을 돌리면 같이 보일 것이다.

### 왜 이 질문이 지금 왔나

- run83 이 kernelbase 쪽을 풀었다.
  - `WriteFile` 은 콘솔 갈림이 없다.
  - 콘솔 앱의 `ConsoleHandle 0` 은 "콘솔 없음" 이 아니라 **새 콘솔 길** 이다.
- 남은 것:
  - PE_emulator 의 실험 대상 약 7천 개는 **콘솔 프로그램** 이다 (요청서 "배경"). 이들이 거치는 CRT 가 무엇을 묻고 무엇을 고르는지 모르면, "가짜 콘솔" 을 어디까지 만들지 정할 수 없다.
  - 상담실 (condrv) 쪽이 실제로 무엇을 받는지 대조해야 했다.
  - 마지막으로, 둘을 합친 **최소 조건 표** — PE_emulator 가 (가) · (나) 중 하나를 고를 수 있게.

### 무엇이 궁금했나

한 문장으로: **CRT 는 시작 때 표준 핸들에 무엇을 묻고, 쓸 때 콘솔 · 파일을 어디서 가르나. 핸들이 0 이면 CRT 는 죽나. condrv 는 무엇을 받나. 그래서 콘솔 없이 / 가짜 콘솔로 가려면 최소한 무엇이 있어야 하나.**

번호는 원문 요청 27 과 sast/81 이 다르다 (run 문서 표의 "3 · 1" 꼴 = 원문 · sast).

| 원문 · sast/81 | 질문 | 이 글 어디서 |
|---|---|---|
| 3 · 1 | CRT 가 처음 핸들 종류를 묻는 길 | 2.3 · 2.4 |
| 3 · 2 | 콘솔 · 파일 · 파이프로 갈라 쓰는 자리 | 2.5 · 2.6 · 2.7 |
| 4 · 3 | 표준 핸들 0 일 때 CRT | 2.8 |
| 5 · (2 · 5) | 리다이렉트 (CRT 몫) | 2.7 |
| (sast/81 4) | condrv 가 받는 IOCTL, `GetFileType` 이 콘솔에 CHAR 를 주는 근거 | 2.9 |
| 목표 · 5 | (가) · (나) 최소 조건 | 2.10 · 2.11 · 2.12 |

### 바퀴 전에 알던 것

- `[정적]` run83: kernelbase `WriteFile` 엔 콘솔 갈림이 없다 → 늘 `+0x86 NtWriteFile`.
- `[정적]` run83: `GetFileType` = `+0x72/+0x79 NtQueryVolumeInformationFile`. `DeviceType 0x50` → 2 (CHAR). 핸들 0 이면 syscall 없이 막는다.
- `[정적]` run83: 콘솔 앱 + `ConsoleHandle 0` → `ConsoleAllocate` (새 콘솔). 실패면 kernelbase 시작 함수 FALSE. −1 이면 commit 만.
- `[관찰]` run44: test.exe `ConsoleHandle` 4 (`\Reference {ConDrv}`) → `0x60` (`\Connect {ConDrv}`), 표준 핸들 `0x68` · `0x6c` · `0x70`.
- `[소스]` `sample/log.h:38` — `Log` 는 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)`. `log.h:2` — `printf` 는 일부러 안 씀. `build.bat:12` — `/MT`.
- PE_emulator 쪽 기록 (요청서): `ConsoleHandle` · 표준 핸들 0, `NtWriteFile(0)` 26번 `0xc0000008`.
- 몰랐던 것:
  - CRT 가 `GetFileType` 을 언제 부르나, 콘솔이면 `WriteConsoleW` 를 고르나 (run83 해설 2.11 끝에 `[추측]` → Q149b 로 넘김).
  - 표준 핸들 0 일 때 CRT 가 어떻게 되나.
  - condrv 가 받는 쪽.

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 시작 때 한 번 묻고, 쓸 때는 우체통

run 문서 "호출 관계" 에서 대표만 옮겼다 `[정적]`.

```
[시작 — 표준 핸들 종류 묻기]
ucrtbase!DllMainDispatch (0xbce0)              이유 1
 └─ +0xe6  __acrt_execute_initializers(표 0xf94d0)      8 lowio · … · 15 initialize_c
      └─ (8) __acrt_initialize_lowio (0x71f20)
              └─ (3) +0x2a  initialize_stdio_handles_nolock (0x720e4)        fd 0 · 1 · 2
                      ├─ (1) +0x68  GetStdHandle(−10 · −11 · −12)        syscall 없음 (run83)
                      └─ (2) +0xcc  GetFileType(핸들) → NtQueryVolumeInformationFile (run83)

[쓰기 — printf]
ucrtbase!__stdio_common_vfprintf (0x19df0) → … → _write_internal (0x1c310)   +0x29 fd == −2 → errno 9 · −1
 └─ +0xae  _write_nolock (0x1c620)
      ├─ (1) +0xca   write_requires_double_translation_nolock (0x1c940)     기본은 거짓
      ├─ (2) +0x195  write_text_ansi_nolock (0x1d450)                        ← C 로캘 기본
      │       └─ +0xc7   WriteFile(osfhnd, …) → NtWriteFile (run83)
      └─ (2) +0x21c  write_double_translated_unicode_nolock (0x874f8)        UTF 모드만
              └─ … _putwch_nolock → CreateFileW(L"CONOUT$") · WriteConsoleW (글자 하나)
```

- 트리의 `+0x..` 는 ret 자리다 (콜스택에 보일 자리). 같은 번호 둘 = 값에 따라 둘 중 하나.
- 위 덩어리 = **시작 때 한 번** (2.3 · 2.4).
- 아래 덩어리 = **쓸 때마다** (2.5 ~ 2.8).
- condrv 는 2.9 에서 따로 본다.

비유로:

- 가게 문을 열면, 현관 안 짐 푸는 사람 (CRT) 이 개업 서류의 입 · 귀 · 비상 확성기 칸을 하나씩 본다.
- 그 줄 끝이 상담실인지 창고인지 관인지 **한 번 묻고**, 자기 장부에 딱지를 붙인다.
- 그 뒤 편지를 쓸 때는, 기본 설정이면 딱지와 상관없이 그냥 **우체통 (`WriteFile`)** 에 넣는다.
- 상담실 전용 서비스 (`WriteConsoleW`) 는 특별한 설정을 켰을 때만 쓴다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `└─ (2) +0xcc  GetFileType(핸들) → NtQueryVolumeInformationFile`

- **누가**: `initialize_stdio_handles_nolock` (ucrtbase rva `0x720e4`). fd 0 · 1 · 2 를 차례로 돈다.
- **어디서**: call `+0xc6` / ret `+0xcc` (트리 숫자는 ret).
  - 결과서는 `+0xc3 ~ +0xcc` 로 적었는데, `+0xc3` 은 인자 싣기 (`MOV RCX,RBP`) 다 (run 문서 "보탠 것" ①).
  - 그 앞 `+0x6b ~ +0x73` 이 "손잡이가 0 · −1 이 아닌가" 를 먼저 본다. 0 · −1 이면 이 줄까지 안 온다.
- **누구를**: kernelbase `GetFileType` → (run83) `+0x72/+0x79 NtQueryVolumeInformationFile(h, FileFsDeviceInformation)`.
- **무엇을 위해**: 그 표준 핸들 뒤가 상담실 · 창고 · 관 중 무엇인지 알아서, 장부에 딱지를 붙이려고.
- 돌아온 뒤 `[정적]`:
  - 0 (모름 · 오류) → `+0xce JZ` → 실패 갈래 ("연락처 없음 −2").
  - 아니면 `+0xd3` 장부에 손잡이 번호를 적는다.
  - 2 (CHAR) 면 `+0xe9 |= 0x40`, 3 (PIPE) 면 `+0xe2 |= 0x08`, 1 (DISK) 면 더 안 함.

#### 줄 2: `├─ (2) +0x195  write_text_ansi_nolock (0x1d450)   ← C 로캘 기본`

- **누가**: `_write_nolock` (ucrtbase rva `0x1c620`).
- **어디서**: call `+0x190` / ret `+0x195`. 여기 오려면 세 문을 지난다 `[정적]`.
  - `+0xc5/+0xca` "이중 번역" 검사가 거짓 (`+0xcc JNZ` 안 탐, 2.6).
  - `+0xd6 CMP byte [칸+0x38],0` · `+0xdb JL` — 텍스트 모드 (`FTEXT`) 면 텍스트 쪽으로.
  - 칸 `+0x39` = 0 (ANSI).
- **누구를**: `write_text_ansi_nolock`.
- **무엇을 위해**: 글자를 번역 없이 그대로 쓰되, 줄바꿈 `\n` 을 `\r\n` 으로 바꿔 스택 버퍼 (최대 `0x13ff`B) 에 담아 `+0xc1/+0xc7 WriteFile` 로 보낸다.
- 이 줄이 **C 로캘 기본 길** 이다. 손잡이 뒤가 상담실이든 창고든 같다.

### 2.3 새 말 — CRT 의 두 층과 장부 (fd · `osfhnd` · `osfile`)

CRT 안 입출력은 두 층이다. 트리 이름에서 바로 보인다.

```
printf · puts · fwrite(stdout)       stdio 층 — FILE (_iob) · 버퍼      편지를 모으는 곳
   │  버퍼를 비울 때
   ▼
_write_internal → _write_nolock      lowio 층 — fd 장부 (__pioinfo)    장부를 보고 우체통에
   │  WriteFile(장부의 손잡이)
   ▼
kernelbase WriteFile → NtWriteFile (run83)                              관리사무소 신청 창구
```

- 시작 순서도 이 층을 따른다. 초기화 표 (`0xf94d0`) 에서 **8번 lowio** 가 **15번 `initialize_c` → `__acrt_initialize_stdio`** 보다 먼저다 `[정적]`.

**새 말 — lowio**: CRT 안에서 **fd 번호와 OS 손잡이를 잇는 아래층** 이다. `_write` · `_isatty` 가 여기 있다. 이름의 "low io" 는 "낮은 층 입출력" 일 것이다 `[추측: 이름 풀이]`. 비유: 연락처 장부를 맡은 직원.

**새 말 — fd**: CRT 가 쓰는 **줄 번호** 다. CRT 시작은 fd 0 · 1 · 2 에 `GetStdHandle(−10 · −11 · −12)` 를 하나씩 짝지어 묻는다 `[정적]`. 비유: 장부 0번 줄 = 귀, 1번 줄 = 입, 2번 줄 = 비상 확성기.

**장부** — `__pioinfo` (rva `0x139e80`) `[정적]`:

- 첫 묶음 64칸 (`__acrt_lowio_ensure_fh_exists(0)` → `_calloc_base(0x40, 0x48)`), 칸 하나 `0x48`B.
- 칸 안 (위치 `[정적]`, 이름 `[추측: 이름]`):
  - `+0x28` **`osfhnd`** — 진짜 OS 손잡이 번호
  - `+0x38` **`osfile`** — 딱지 (1바이트 비트 모음)
  - `+0x39` **`pmode`** — 텍스트 모드 종류 (2.5)

**딱지 (`osfile`) 비트** — 값 `[정적]`, 이름은 CRT 관례 `[추측: 이름]`:

| 비트 | 이름 | 뜻 | 비유 |
|---|---|---|---|
| `0x01` | `FOPEN` | 열려 있음 | 쓰는 줄 |
| `0x08` | `FPIPE` | 파이프 | 관 딱지 |
| `0x40` | `FDEV` | 문자 장치 (콘솔류) | 상담실 딱지 |
| `0x80` | `FTEXT` | 텍스트 모드 | 줄바꿈 고쳐 쓰기 |

(`0x20` `FAPPEND` 도 있지만 이번 길엔 안 나온다.)

**`_isatty`**: "이 fd 가 콘솔류인가" 를 답한다. **`osfile & 0x40` 하나만** 본다. **API 를 안 부른다** `[정적]`.

- 비유: 상담실에 전화해서 묻지 않고, 장부 딱지만 보고 답한다.

### 2.4 ★ 시작 때 묻는 것은 둘뿐 — `GetStdHandle` · `GetFileType` (질문 3 · 1)

`initialize_stdio_handles_nolock` 이 fd 0 · 1 · 2 마다 하는 일 `[정적]`:

1. `+0x3a ~ +0x47` — 그 칸이 비었나 (`osfhnd + 2 ≤ 1`, 곧 부모가 C 런타임 핸들 표로 안 채운 칸) 를 본다. 이미 차 있으면 `FTEXT` 만 켜고 넘어간다.
2. `+0x49` — 딱지를 먼저 `0x81` (`FOPEN` · `FTEXT`) 로 둔다.
3. `+0x62/+0x68` — **`GetStdHandle`** (−10 · −11 · −12). syscall 없이 개업 서류 칸 값을 그대로 준다 (run83).
4. `+0x6b ~ +0x73` — 손잡이가 **0 또는 −1** 이면 `GetFileType` 을 **안 부르고** 실패 갈래로.
   - 실패 갈래: `+0x75 osfile |= 0x40` · `+0x81 osfhnd = −2`.
5. 아니면 `+0xc6/+0xcc` — **`GetFileType`**. 답에 따라 딱지를 붙인다 (2.2 줄 1).

결과를 한 표로 (run 문서 "CRT 시작" 표 그대로):

| `GetStdHandle` 값 | `GetFileType` | `osfhnd` (칸 `+0x28`) | `osfile` (칸 `+0x38`) | `FILE._file` |
|---|---|---|---|---|
| **0** 또는 **−1** | **안 부름** (`+0x6b ~ +0x73`) | **−2** (`+0x81`) | `0xC1` (`+0x75 \|= 0x40`) | **−2** (`__acrt_initialize_stdio`) |
| 그 밖 | 0 (모름 · 오류) | −2 | `0xC1` | −2 |
| 그 밖 | 1 (DISK) | 핸들 (`+0xd3`) | `0x81` | 0 · 1 · 2 그대로 |
| 그 밖 | 2 (CHAR) | 핸들 | `0xC1` (`+0xe9 \|= 0x40` FDEV) | 그대로 |
| 그 밖 | 3 (PIPE) | 핸들 | `0x89` (`+0xe2 \|= 0x08`) | 그대로 |

꼭 짚을 것 `[정적]`:

- **`GetConsoleMode` 는 시작 때 안 부른다.** 콘솔인지는 `GetFileType` 의 답 하나로만 정한다.
- 실패 갈래에서도 `FDEV` (`0x40`) 를 켠다. 그런데 2.8 에서 보듯, `stdout` 은 장부 1번 줄이 아니라 −2 를 가리키게 돼서 결국 콘솔로 안 보인다.
- 표준 핸들이 **NUL 장치** (`DeviceType 0x15`) 여도 kernelbase 가 CHAR 로 바꿔 주므로 (run83 점프표), CRT 는 이것도 `FDEV` 로 본다.
- 그 앞 `initialize_inherited_file_handles_nolock` (`+0x36 GetStartupInfoW`) 은 개업 서류의 `RuntimeData` (`lpReserved2` · `cbReserved2`) 에 C 런타임 핸들 표가 있으면 먼저 복사한다. 비어 있으면 아무것도 안 한다.

비유:

- 짐 푸는 사람은 장부 세 줄 (귀 · 입 · 확성기) 을 하나씩 보며 묻는다. "이 줄 끝이 뭐예요?"
- 서류 칸이 비어 있으면 (0 · −1) **묻지도 않고** "연락처 없음" 이라고 적는다.
- 상담실이면 상담실 딱지, 관이면 관 딱지, 창고면 딱지 없음.
- 전화는 이 시작 때 **한 번씩** 뿐이다. 그 뒤엔 장부만 본다.

PE_emulator 에게: 지금처럼 표준 핸들이 0 이면 CRT 는 `GetFileType` 도 안 부르고 "콘솔 없음 (−2)" 으로 둔다 — run 문서 표의 "지금 우리" 칸 **맞다**.

### 2.5 새 말 — 텍스트 모드 · `pmode` · 로캘 · 이중 번역

2.6 의 갈림 조건을 읽으려면 네 말이 필요하다.

**새 말 — 텍스트 모드 / 바이너리 모드**: 텍스트 모드면 줄바꿈 `\n` 을 `\r\n` 으로 바꿔 쓴다 (`write_text_ansi_nolock +0x81 CMP AL,0xa` · `+0x88 MOV byte [RBX],0xd`) `[정적]`. 바이너리 모드면 그대로 쓴다 (`_write_nolock +0xff/+0x105 WriteFile`). 딱지 `0x80` (`FTEXT`) 이 이걸 가른다. 표준 줄은 시작 때 `0x81` 로 텍스트가 켜진다.

- 비유: 편지 줄 끝을 "가게 양식" 으로 고쳐 쓰나, 원고 그대로 보내나.

**새 말 — `pmode`** (칸 `+0x39`): 텍스트 모드의 **글씨체** 다. 0 = ANSI · 1 = UTF-8 · 2 = UTF-16 (`_write_nolock` 의 갈래로 확인) `[정적]`.

- `_setmode` (`_O_U16TEXT` 류) 로 바꾼다 (결과서).
- 기본은 0 이다 (`__acrt_lowio_create_handle_array` 가 `+0x39` 를 0 으로 둠) `[정적]`.

**새 말 — 로캘 (CRT 의)**: 프로그램이 글자 · 숫자를 어느 나라 규칙으로 다루나 정하는 설정이다. `setlocale` 로 바꾸고, 아무것도 안 바꾼 기본을 "C 로캘" 이라 부른다 `[추측]` (일반적으로 알려진 설명).

- 이번 코드가 보는 건 로캘 데이터의 `+0x138` 한 칸이다.
  - 기본값 `__acrt_initial_locale_data` (rva `0x1380b0`) 의 `+0x138` = **0** `[정적]`.
  - 칸 이름은 `[추측: 로캘 이름 포인터]`. `setlocale(LC_ALL, "")` 뒤 값은 실행해 봐야 안다.
- run83 해설 2.6 의 로케일 (TEB `CurrentLocale`, kernelbase `SetThreadLocale` 이 씀) 과는 **다른 칸** 이다. 둘이 서로 어떻게 이어지는지는 이번에 안 봤다.
- 비유: 가게의 **번역 규칙표**. C 로캘 = 번역 규칙 없음.

**새 말 — 이중 번역** ("double translation"): 콘솔에 로캘 글자를 올바르게 쓰려고, 글자를 한 번 UTF-16 으로 풀었다가 콘솔 출력 코드 페이지로 다시 바꾸는 것이다 (결과서).

- 비유: 원고를 한 번 공통어로 옮겼다가, 상담실이 쓰는 말로 다시 옮기기.

### 2.6 ★ 갈림 줄은 하나 — 기본 `printf` 는 콘솔이어도 우체통 (질문 3 · 2)

#### 갈림 줄 `_write_nolock +0xc5`

CRT 가 콘솔과 파일을 가르는 곳은 **`_write_nolock +0xc5 write_requires_double_translation_nolock` 하나** 다 `[정적]`. 그 함수 (rva `0x1c940`) 가 "이중 번역이 필요한가" 를 이렇게 본다:

```
write_requires_double_translation_nolock
 ① +0x17/+0x1c  _isatty(fd)                      +0x1e JZ   콘솔류 (FDEV) 아니면 거짓
 ② +0x4b CMP [칸+0x38],0 · +0x50 JGE              텍스트 모드 아니면 거짓
 ③ +0x64 CMP [로캘+0x138],0 · +0x6c JZ → +0x9e
    +0xa1 CMP [칸+0x39],0 · +0xa6 JNZ             로캘도 0, pmode 도 0 이면 거짓   ← 기본은 여기서 끝
 ④ +0x83/+0x89  GetConsoleMode(osfhnd)            성공이면 참
```

- 참이 되려면 넷 다: **FDEV ∧ 텍스트 ∧ (로캘 `+0x138` ≠ 0 ∨ `pmode` ≠ 0) ∧ `GetConsoleMode` 성공**.

#### 갈래 넷 — 어디서 `WriteConsoleW` 가 나오나

| 조건 | `_write_nolock` 에서 가는 곳 (call/ret) | 끝 |
|---|---|---|
| 이중 번역 아님 · 텍스트 · ANSI (**C 로캘 기본**) | `+0x190/+0x195` `write_text_ansi_nolock` | `+0xc1/+0xc7` **`WriteFile`** |
| 바이너리 모드 | — | `+0xff/+0x105` **`WriteFile`** 바로 |
| 이중 번역 · ANSI | `+0x2af/+0x2b4` `write_double_translated_ansi_nolock` (`0x803e8`) | `+0x7c GetConsoleOutputCP` · `+0x19b WideCharToMultiByte` → `+0x1c6` · `+0x2f4` **`WriteFile`** (ret 자리) |
| 이중 번역 · UTF-8 / UTF-16 | `+0x217/+0x21c` `write_double_translated_unicode_nolock` (`0x874f8`) | 글자마다 `_putwch_nolock` → **`WriteConsoleW`** (`CONOUT$`) |

- `WriteConsoleW` 가 나오는 곳은 **맨 아래 한 줄뿐** 이다 `[정적]`.
- (이중 번역 아닌 UTF-8 · UTF-16 텍스트는 `+0x24a` `write_text_utf8_nolock` · `+0x272` `write_text_utf16le_nolock` 로 간다 — 결과서. 그 안쪽은 이 글 범위 밖.)

#### 왜 "콘솔이어도 `WriteFile`" 인가 — 단계별로

1. 갈림 줄은 `_write_nolock +0xc5` 하나다 `[정적]`.
2. 참이 되려면 위 ① ~ ④ 가 다 맞아야 한다. 그중 ③ 은 "로캘 `+0x138` ≠ 0 또는 `pmode` ≠ 0".
3. 기본 상태는 **둘 다 0** 이다 `[정적]` (`__acrt_initial_locale_data +0x138` = 0, 표준 칸 `pmode` = 0).
4. 그래서 ④ `GetConsoleMode` 까지 가지도 않고 거짓 → `write_text_ansi_nolock` → `WriteFile` → `NtWriteFile`.
5. 참이 돼도 ANSI 면 끝은 `WriteFile` 이다. `WriteConsoleW` 는 UTF 모드 + 콘솔 + `GetConsoleMode` 성공일 때만.
6. 아직 확인 아님: 실제 콘솔에서 C 로캘 CRT 가 정말 `WriteFile` 로 쓰는지는 실행해 봐야 한다 (Q104 동적 4 — 새 샘플, Q160).

#### `GetConsoleMode` 는 언제 부르나 `[정적]`

- CRT 시작 때는 **안 부른다** (2.4).
- 쓸 때는 ①②③ 이 맞을 때만, **`_write_nolock` 한 번 부를 때마다** (캐시 없음).

#### 유니코드 길 — `CONOUT$` 로 글자 하나씩 `[정적]`

**새 말 — `CONOUT$`**: "지금 이 가게에 붙은 콘솔 화면" 을 가리키는 특별한 파일 이름이다 `[추측]` (일반적으로 알려진 설명). condrv 쪽 이름은 `\CurrentOut` 일 것이다 `[추측]`. ntdll 이 이걸 어떤 NT 이름 · 루트로 바꾸는지는 `[공백]` (Q159 ③). 비유: 입 칸 (표준 출력) 과 상관없이 **상담실로 바로 가는 전용선**.

- `_putwch_nolock` (`0x87590`) → `+0x9/+0xe __dcrt_lowio_ensure_console_output_initialized` → 처음 한 번 `CreateFileW(L"CONOUT$", GENERIC_WRITE, 공유 3, …, OPEN_EXISTING)`.
  - 이 CRT 핸들은 표준 핸들과 **별개** 다.
- `+0x38/+0x3d console_output_reopen_and_retry` → `+0x25/+0x2b WriteConsoleW(CONOUT$ 핸들, 글자 1개)`.
  - 오류 6 이면 닫고, 다시 열고 (`+0x63`), 한 번 더 (`+0x84`).
- 그래서 유니코드 모드 `printf` 는 **글자 1개 = IOCTL 1번** 이다 (결과서).
- conio (`_cputs` · `_cputws` · `_putch`) 도 같은 `_putwch_nolock` 길이다.

#### 곁가지 — `printf` 는 ucrtbase 에 없다

- `_index.tsv` 에 `printf` 가 0개다 `[정적]`. 앱 쪽 헤더가 `__stdio_common_vfprintf` 로 풀어 쓴다.
- `puts` · `fputs` · `fwrite` · `fputc` 도 같은 `_write_internal` 로 모인다 (`_calls.tsv` callers). `puts` 의 람다 안쪽은 안 읽었다 `[추측]` (Q159 ⑥).
- `std::cout` 은 msvcp140 을 안 열어서 `_write` 로 모인다는 건 `[추측]` (Q159 ④).

#### PE_emulator 에게

- 요청서 가정 "`FDEV` + `GetConsoleMode` 성공이면 `WriteConsoleW`" 는 **틀렸다** `[정적]`.
- run83 결론 (kernelbase `WriteFile` 에 콘솔 갈림 없음) 과 이어서: **엔진은 `NtWriteFile` 몸 하나만 콘솔을 알면 된다.**
- 콘솔 IOCTL (`0x500016`) 은 앱이 콘솔 API (`GetConsoleMode` · `WriteConsoleW` 등) 를 **직접** 부를 때만 나간다.

비유: 상담실 줄이든 창고 줄이든, 기본 설정의 짐 푸는 사람은 편지를 그냥 **우체통** 에 넣는다. 상담실 전용 창구 (`WriteConsoleW`) 로 들고 가는 건 "유니코드 글씨체로 써라" 같은 특별한 지시를 받았을 때뿐이다.

### 2.7 언제 `_write` 까지 가나 — 상담실 줄은 한 번에, 창고 · 관은 모아서 (리다이렉트 포함)

**새 말 — 버퍼링**: 글자를 바로 보내지 않고 상자에 모았다가 한꺼번에 보내는 것. stdio 층 (2.3) 이 한다.

- **표준 출력이 콘솔류 (FDEV) 면** `[정적]`:
  - `printf` 한 번의 출력을 **4096B 임시 버퍼** 에 모았다가 끝에서 한 번 비운다.
  - 곧 **`printf` 한 번 = `_write` 한 번**.
  - 근거: `__acrt_stdio_begin_temporary_buffering_nolock +0x39/+0x3e _isatty`, 람다 `operator()` `+0x114/+0x119 __acrt_stdio_flush_nolock`.
  - `stderr` 는 늘 임시 버퍼다.
- **파일 · 파이프면**:
  - 보통 버퍼링 — 버퍼가 찰 때 · `fflush` · 종료 때 `_write` 가 나간다.
  - 버퍼 크기는 `__acrt_stdio_allocate_buffer_nolock` 안이라 안 읽었다 `[공백]` (보통 4096 `[추측]`).
  - 종료 때 비우는 길 (`exit` → `common_flush_all`) 도 안 따라갔다 `[공백]` (Q159 ②).
- **리다이렉트 (질문 5 의 CRT 몫)** `[정적]`:
  - 표준 출력이 파일 · 파이프면 `_isatty` 가 거짓 → `write_requires_double_translation_nolock +0x1e JZ` 에서 끝 → `WriteFile` → `NtWriteFile` 만.
  - condrv 는 안 낀다. kernelbase 쪽도 같다 (run83: `WriteFile` 은 안 가름).
  - 다만 **시작 때** `ConsoleHandle` 이 문제다 → 2.10.
- **덜 쓰면 멈춘다** `[정적]`: `write_text_ansi_nolock +0xd2 CMP EAX,EBX` — 쓴 바이트가 요청보다 적으면 거기서 멈춘다. 그러니 엔진의 `NtWriteFile` 답에서 `IoStatusBlock.Information` 은 `Length` 와 같아야 한다.

비유:

- 상담실 줄이면, 한 마디 할 때마다 쪽지 한 장에 다 적어서 바로 우체통.
- 창고 · 관이면, 큰 상자에 모았다가 상자가 차거나 문 닫을 때 한꺼번에.
- 우체국이 "반만 받았어요" 하면 짐 푸는 사람은 나머지를 안 보낸다.

### 2.8 ★ 표준 핸들 0 이면 — CRT 는 조용히 버린다, 그리고 test.exe 의 26번 (질문 4 · 3)

#### CRT `printf` 가 어떻게 되나 — 단계별로 `[정적]`

1. **시작**: `GetStdHandle` → 0 → `+0x6b ~ +0x73` → `GetFileType` 안 부름 → `+0x75 osfile |= 0x40` · `+0x81 osfhnd = −2`.
   - 세 줄 (fd 0 · 1 · 2) 모두 `osfhnd −2` · `osfile 0xC1`.
2. **stdio 준비** (15번): `__acrt_initialize_stdio` 가 `osfhnd + 2 < 3` (−2 · −1 · 0) 이면 `FILE._file = −2` (`_NO_CONSOLE_FILENO`) 로 쓴다.
   - 이제 `stdout` 편지함은 장부 1번 줄이 아니라 "**−2 = 연락처 없음**" 을 가리킨다.
   - 시작은 **성공** 이다 (`__acrt_initialize_lowio` true).
3. **`printf`**: `__acrt_stdio_begin_temporary_buffering_nolock +0x39` 이 `_isatty(_fileno(stdout))` = `_isatty(−2)` 를 묻는다.
   - `_isatty(−2)` 는 `errno 9` 만 세우고 0 (거짓) 이다. abort 없음.
   - 장부 1번 줄에 상담실 딱지가 붙어 있어도, 묻는 번호가 −2 라 상관없다.
   - 그래서 임시 버퍼를 안 쓰고, 글자는 `stdout` 보통 버퍼에 쌓인다 (`common_flush_and_write_nolock<char>`).
   - 반환값은 글자 수일 것이다 `[추측]` (글자 세는 줄은 직접 안 읽음 — Q159 ⑥).
4. **버퍼를 비울 때** (`fflush` · 4096 넘음 · 종료):
   - `_write_internal +0x29 CMP EBX,-2` · **`+0x2c JZ → +0x122`** → `errno 9 (EBADF)` · −1.
   - **`_invalid_parameter` 를 안 부른다** → abort · 예외 없음.
5. **프로그램은 계속 돈다.** `NtWriteFile` 은 한 번도 안 나간다.

비유: 장부에 "연락처 없음" 이라고 적힌 줄로 편지를 쓰면, 편지는 상자에 쌓였다가, 보낼 때 "주소 없음" 으로 조용히 버려진다. 가게는 문을 안 닫는다.

PE_emulator 에게: CRT `printf` 쪽은 **걱정 없다** — 표준 핸들 0 이어도 abort 없이 돈다 (run 문서 표의 "지금 우리": 맞다 · 걱정 없음).

#### test.exe 의 `NtWriteFile(0)` 26번은 CRT 가 아니다 — 단계별로

1. `Log` 는 `printf` 를 일부러 안 쓴다 `[소스]`. `sample/log.h` 2번째 줄 주석:
   ```
   // TLS 콜백은 CRT 초기화 전에도 불릴 수 있어서 printf 같은 CRT 함수는 안 쓴다.
   ```
2. `Log` 는 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), buf, i, &written, NULL)` 을 직접 부른다 (`log.h:38`) `[소스]`.
3. PE_emulator 의 표준 출력 칸이 0 → `GetStdHandle` 이 0 을 그대로 준다 → `WriteFile +0x86 NtWriteFile(0)` (run83) `[정적]`.
4. 만약 CRT `printf` 였다면 위처럼 `NtWriteFile` 이 **아예 안 나갔을 것** 이다 `[정적]` (flow.md 2.8 의 run84 새 줄).
5. 그러니 "26번" 은 CRT 가 아니라 `Log` 쪽 이야기와 맞는다.
6. 26 이 `Log` 횟수와 같은지는 여전히 **안 셌다** `[추측]`.

### 2.9 condrv — 상담실 안을 처음 들여다봤다 (sast/81 질문 4)

**새 말 — condrv.sys**: `System32\drivers\condrv.sys`. 콘솔 장치 `\Device\ConDrv` 를 맡는 **커널 드라이버** 다 (flow.md 기준). run83 해설의 **콘솔 상담실** 그 자체다. 이번에 처음 analyze 했다 (함수 116개, PDB 타입 없음).

#### ① 상담실 문 달기 — `DriverEntry` (rva `0x10010`), 부팅 때

- `+0x77 MOV R9D,0x50` (`DeviceType`) · `+0x120 MOV dword [RSP+0x20],0x20000` (`Characteristics`) · call `+0x128` / ret `+0x12f` **`IoCreateDevice(DriverObject, 0, "\Device\ConDrv", 0x50, 0x20000, FALSE, &장치)`** `[정적]`.
- 부팅 때 일이라 흐름도 범위 앞이다.
- 그래서 이어지는 사슬 `[정적]`:
  - 콘솔 손잡이에 `FileFsDeviceInformation` 을 물으면 `DeviceType 0x50`
  - → kernelbase `GetFileType +0x87 CMP EAX,0x50` → 2 (CHAR) (run83)
  - → CRT 딱지 `0x40` (FDEV) (2.4).
- run83 해설 2.11 에선 kernelbase 가 `0x50` 을 CHAR 로 **바꾸는** 걸 봤다. 이번엔 **그 `0x50` 을 누가 정하나** 를 봤다.
- 이 질의엔 condrv 처리 함수가 따로 없다. I/O 관리자가 장치 객체로 답한다 (결과서). 그래서 답은 `DeviceType 0x50`, `Characteristics 0x20000` 일 것이다 `[추측: 장치 객체 값 그대로]`.

비유: 상담실은 부팅 때 문에 "**업종 번호 0x50**" 간판을 단다. 누가 "이 줄 끝이 뭐예요?" 하고 물으면, 관리사무소가 상담실 대신 그 간판을 읽어 준다.

#### ② 창구 이름 안내판 — `CdpDispatchCreate` (rva `0xebd0`, 열기)

- 열 때 받은 이름을 **12칸 표** 와 하나씩 맞춰 본다 (`+0x47/+0x4e RtlCompareUnicodeString`, `+0x31 CMP EBX,0xc`) `[정적]`. 못 찾으면 `0xC0000225`.
- 12칸: `\Connect` · `\KernelConnect` · `\Reference` · `\Input` · `\Output` · `\Server` · `\Console` · `\CurrentIn` · `\CurrentOut` · `\ScreenBuffer` · `\Broker` · `\Display`.
- kernelbase 가 여는 건 그중 다섯이다 (run83 과 짝):

| # | 이름 | kernelbase 가 여는 자리 (run83) | 비유 |
|---|---|---|---|
| 0 | `\Connect` | a1 · b5 | 출입증 받기 |
| 2 | `\Reference` | b2 | 참조 번호표 |
| 3 | `\Input` | b6 | 귀 줄 |
| 4 | `\Output` | b7 | 입 줄 |
| 5 | `\Server` | b1 | 서버 창구 줄 |

- 8번 `\CurrentOut` 은 `CONOUT$` 일 것이다 `[추측]` (2.6).

#### `\Connect` — 소개장은 사실 참조 번호표였다

`\Connect` 를 맡는 `CdCreateConnection` (rva `0xd5a0`) `[정적]`:

1. 열 때 기준 객체 (`RootDirectory`) 가 있으면, 그 객체의 종류가 **Reference 여야** 서버 연결 (`CdpCreateServerConnectionIo`) 이 된다.
2. Reference 가 아니면 `0xC0000008` (잘못된 손잡이).
3. 기준 객체가 없으면 중개 길 (`CdpCreateBrokerConnectionIo`). EA `"attach"` 길도 따로 있다.

그래서:

- kernelbase 붙기 길이 `RootDirectory = ConsoleHandle (4)` 로 `\Connect` 를 여는 것 (run83 a1) 과 맞는다.
- run44 `[관찰]` "핸들 4 = `\Reference {ConDrv}`" 과도 맞는다 (run 문서 "보탠 것" ⑥).
- `"server"` EA 를 condrv 가 읽는 자리는 이 함수엔 없다. 더 안쪽이다 `[공백]` (Q159 ⑤).

비유: 부모가 넘겨 준 **소개장** 은 사실 상담실이 발급한 **참조 번호표** 였다. 번호표가 아닌 걸 내밀면 상담실은 "잘못된 손잡이" 로 돌려보낸다.

#### ③ 줄의 종류 여섯

열린 줄 (파일 객체) 마다 종류가 있다 (`FileObject → FsContext2 → 종류 표`). 종류 표가 "쓰기 · 읽기 · 주문서" 를 누가 받을지 정한다 `[정적]` (결과서, `.rdata` 에서 직접 읽음).

**새 말 — 빠른 길 (FastIo)**: IRP 배달 봉투를 안 만들고 드라이버 함수를 바로 부르는 길이다 `[추측]` (일반적으로 알려진 설명). condrv 는 빠른 길 표에 `FastIoDeviceControl` 하나만 둔다 (`CdpFastIoDeviceControl`). 비유: 봉투 없이 창구에서 바로 처리.

| 종류 | 쓰기 · 읽기 · Flush | 주문서 (보통 길) | 주문서 (빠른 길) | 비유 |
|---|---|---|---|---|
| **Client** | **받는다** (`CdpClientWrite` · `CdpClientRead` · `CdpClientFlushBuffers`) | `CdpClientIoctl` — `0x500016` 만 | 없음 | 손님의 입 · 귀 줄 |
| **Connection** | 안 받음 (`0xC00000BB`) | `CdpConnectionIoctl` — `0x500016` | `CdpConnectionFastIoctl` — `0x500023` | 출입증 줄 |
| **Server** | 안 받음 | `CdpServerIoctl` | `CdpServerFastIoctl` — `0x500037` 과 conhost 쪽 코드들 | 옆집 (conhost) 쪽 줄 |
| **Reference** | 안 받음 | 없음 | 없음 (닫기만) | 참조 번호표 |
| Broker | 안 받음 | `CdpBrokerIoctl` | `CdpBrokerFastIoctl` | 중개 — 이번엔 이름만 |
| Display | 안 받음 | 없음 | `CdpDisplayFastIoctl` | 화면 — 이번엔 이름만 |

- 그러니 앱 쪽에서 **편지 (`NtWriteFile` · `NtReadFile` · Flush) 를 받는 줄은 Client 뿐** 이다 `[정적]`.

#### ④ 주문서 — 앱 쪽이 보내는 건 셋

| 코드 | 받는 종류 · 근거 줄 | 하는 일 `[추측: 이름]` | 보내는 자리 (run83) |
|---|---|---|---|
| `0x500016` | Client (`CdpClientIoctl +0x13`) · Connection (`CdpConnectionIoctl +0x2d`) | 설명서를 읽어 서버 (conhost) 에 넘김 | 콘솔 API 전부 · a4 GetLangId |
| `0x500023` | Connection 빠른 길 (`CdpConnectionFastIoctl +0x26`) — 출력이 8B 가 아니면 `0xC0000206` | 서버 쪽 값 8B (서버 PID) | a2 · b9 |
| `0x500037` | Server 빠른 길 (점프표 → `CdpLaunchServerProcess`) | conhost 띄우기 | b3 |

- 나머지 (`0x500006` · `0x50000b` ~ `0x50003b`) 는 Server · Broker · Display 만 받는다 `[정적]`.
- 그래서 그건 **conhost 몫** 이라고 짐작한다 `[추측: 받는 객체로 짐작]` (run 문서 "보탠 것" ⑤). 근거 둘:
  - 그 코드를 받는 객체가 Server · Broker · Display 뿐이다 `[정적]`.
  - kernelbase 는 그 코드를 안 보낸다 `[정적]` (sast/80).
- run83 해설 2.7 "kernelbase 는 옆집을 직접 안 짓고 상담실에 맡긴다" 의 **받는 쪽** 이 `0x500037` → `CdpLaunchServerProcess` 다 (안쪽은 이름만).
- kernelbase 가 보내는 값과 condrv 가 받는 값이 **정확히 같다**. 서로 다른 바이너리가 맞물려서, 결과서는 이걸 버전 대조로 썼다.

#### ⑤ 편지 받기 — `CdpClientWrite` (rva `0xa890`)

- `+0x5b/+0x60 CdGetProcessConnection` (보낸 가게의 연결 찾기) → `+0xe1/+0xe6 CdAddIoToPipe` (서버에 넘김) → **`+0x100 MOV EAX,0x103`** `[정적]`.
- `0x103` = `STATUS_PENDING`, 곧 "**접수했어요, 처리는 나중에**" 다.
- 실제 끝맺음 (상태 · 쓴 바이트 수 `Information`) 은 서버 (conhost) 가 `0x50000b` 로 채운다 (`CdCompleteIo`, 결과서).
- kernelbase `WriteFile` 은 `0x103` 이면 `+0x1d1 NtWaitForSingleObject` 로 기다린다 (run83).

비유: 상담실은 편지를 받으면 바로 답하지 않고 "접수" 도장만 찍어 옆집에 넘긴다. 옆집이 화면에 다 쓰고 나서야 "몇 글자 썼음" 답이 온다.

PE_emulator 에게: 엔진에는 옆집이 없으니 **바로 끝내 주면 된다** (`Information = Length`, 2.12). 흉내 낼 것은 IOCTL 셋 + Client 줄의 `NtWriteFile` / `NtReadFile` / `NtFlushBuffersFile` + 열기 이름 5 ~ 6개다 (run 문서).

### 2.10 ★ 막히는 곳은 쓰기가 아니라 시작 — 그래서 (가) 는 −1 이어야 한다

여기서 run83 과 run84 가 합쳐진다. 단계별로:

1. **쓰기는 `NtWriteFile` 하나다** `[정적]`. kernelbase `WriteFile` 도 (run83), CRT 기본 `printf` 도 (2.6) 콘솔인지 안 가른다.
2. **그런데 시작 때** kernelbase `ConsoleInitialize` 가 개업 서류의 `ConsoleHandle` **한 칸으로** 길을 가른다 (run83).
3. 콘솔 앱이 **0** 이면 `+0x177 JZ` → 새 콘솔 길 (call `+0x146 ConsoleAllocate`) — condrv 쪽 syscall 8개 (run83 b1 ~ b8) `[정적]`.
   - **표준 핸들이 파일이어도 그렇다.** 갈림은 `ConsoleHandle` 칸만 보기 때문이다.
4. `ConsoleAllocate` 가 실패를 돌려주면 `_KernelBaseBaseDllInitialize +0x526 TEST AL` · `+0x528 JZ` → **kernelbase 시작 함수 FALSE** `[정적]`.
5. **−1** 이면 `+0x179 CMP RDX,-1` · `+0x17d JZ → +0x1c6` → 빈 상태로 `ConsoleCommitState` (`+0x1ca`) `[정적]`.
   - commit 이 `+0x40` 에서 `ConsoleHandle ← 0` 으로 바꾼다 (run83 ③).
   - syscall 은 `NtSetInformationProcess` **1번** 뿐.
6. 그래서 **"`ConsoleHandle = 0` 인데 표준 핸들만 파일로 바꾸는" 절충안은 condrv 를 못 피한다** `[정적]`.
7. 아직 모름: −1 이 실제 Windows 의 어느 시작 방식에서 오는지는 `[추측]` (Q158 ①).

비유:

- 입 · 확성기 줄을 창고에 이어 놔도, 개업 서류의 손잡이 칸이 비어 있으면 (0) 건물은 "쇼윈도가 필요한 가게인데 소개장이 없네" 하고 상담실로 간다 (run83 해설 2.4).
- 상담실이 대답을 못 하면 건물 전원 켜기가 실패한다.
- 상담실을 건너뛰려면 손잡이 칸에 "**필요 없음**" (−1) 이라고 적어 줘야 한다.

### 2.11 (가) 파일 리다이렉트처럼 — 최소 조건

콘솔 서버 없이 가는 길이다. run 문서 "(가)" 표에서 쉬운 말로 옮겼다 (근거 줄은 run 문서).

| 항목 | 값 | 이게 없으면 |
|---|---|---|
| exe `Subsystem` | 3 (콘솔) 그대로 | GUI 로 바꾸면 다른 갈래 — 콘솔 프로그램 7천 개의 실제 모습과 달라진다 |
| `params.ConsoleHandle` (`+0x10`) | **−1** (`0xFFFFFFFFFFFFFFFF`) | 0 · −2 · −3 → `ConsoleAllocate` (condrv 필요, 실패면 kernelbase 시작 FALSE). 진짜 핸들 → (나) 길 A |
| `params.ConsoleFlags` (`+0x18`) | 0 | 비트 4 · 2 가 켜지면 표준 핸들 검사 · 교체가 끼어 `NtQueryVolumeInformationFile` (+ 콘솔 장치면 `NtClose`) 이 더 나간다 |
| `params.WindowFlags` (`+0xa4`) 의 `0x200` · `0x400` | 둘 다 0 | 켜져 있으면 `GetStdHandle` 이 칸을 읽지도 않고 0 (`+0x98 XOR EBX,EBX`) → 표준 핸들 0 처럼 보인다 |
| `params.StandardOutput` (`+0x28`) · `StandardError` (`+0x30`) (· `StandardInput` `+0x20`) | 0 도 −1 도 아닌 핸들 (엔진이 연 파일 핸들, 서로 같아도 됨) | 0 · −1 이면 CRT 가 "콘솔 없음 (−2)" 으로 두고 `printf` 를 조용히 버린다 (2.8) |
| 열어 둘 핸들의 종류 | 동기 (synchronous) 파일 핸들 또는 이름 있는 파이프, 쓰기 권한 | 비동기면 `0x103` 뒤 `WriteFile +0x1d1 NtWaitForSingleObject` 기다림 길을 엔진이 흉내 내야 한다 |
| `NtQueryVolumeInformationFile(h, 4, 8B)` 답 | SUCCESS, `DeviceType = 7` (디스크) 또는 `0x11` (이름 있는 파이프) | `0x50` 이면 CRT 가 콘솔류로 본다 (아래 메모). 실패 상태면 `GetFileType` 0 → CRT 가 그 표준 핸들을 −2 로 버린다 |
| `NtWriteFile` 답 | SUCCESS (바로, `0x103` 없이), `IoStatusBlock.Information = Length` | `Information` 이 작으면 CRT 가 거기서 멈춘다 (2.7). 실패 상태면 `_write` −1 |
| `NtSetInformationProcess(−1, 0x31, &{1}, 8)` | SUCCESS 권장 | **실패해도 시작 실패는 아니다** `[정적]` (2.12 "시작이 깨지는 곳은 어디인가"). 커널 쪽 `EPROCESS.OwnerProcessId` 둘째 쓰기 (run51) 는 빠진다 |
| `params.RuntimeData` | 비움 (0) | 채우면 CRT 가 그 표를 표준 핸들보다 먼저 장부에 복사 |
| 콘솔 API 답 (`GetConsoleMode` 등) | 필요 없음 — commit 뒤 `ConsoleHandle = 0` 이라 syscall 없이 `0xC0000008` | 앱이 `GetConsoleMode(stdout)` 로 리다이렉트를 가려내면 "콘솔 아님" — 진짜 리다이렉트와 같다 |
| KUSD `SharedDataFlags` (`0x7ffe02f0`) | 상관없음 | −1 갈래는 `ConsoleAllocate` 를 안 탄다 |

- **이것만으로 끝까지 가나**: 정적으로는 **예** `[정적]`.
  - syscall = 시작 때 `NtSetInformationProcess` 1번 + 표준 핸들마다 `NtQueryVolumeInformationFile` (CRT `GetFileType` 3번 + 앱이 따로 부르면 더) + `NtWriteFile`.
- **단점**: 파일 · 파이프는 `stdout` 이 버퍼링이라 `NtWriteFile` 이 버퍼 찰 때 · 종료 때 몰아서 나간다 (2.7). 종료 때 비우는 길은 `[공백]`.
- **메모** `[추측: 정적 줄 셋을 이은 것]`:
  - (가) 에서 `DeviceType` 을 `0x50` 으로 답하면, CRT 는 콘솔류로 보고 `printf` 마다 `_write` 한다.
  - C 로캘이면 그대로 `WriteFile` 이다.
  - 로캘을 바꿔 `write_requires_double_translation_nolock +0x83 GetConsoleMode` 를 불러도, `ConsoleHandle = 0` 이라 syscall 없이 실패 → 거짓 → `WriteFile`.
  - 곧 condrv 없이 **줄마다 나가는 출력** 이 될 것이다. 실제로 그런지는 동적 (Q104 2 · 새 샘플).

비유: (가) = 개업 서류 손잡이 칸에 "**쇼윈도 필요 없음**" (−1) 이라고 적고, 입 · 확성기 줄을 창고에 이어 두는 것. 건물은 상담실에 안 가고, 짐 푸는 사람은 창고 딱지 (딱지 없음) 를 붙이고, 편지는 우체통을 거쳐 창고에 쌓인다.

### 2.12 (나) 가짜 condrv — 길 A 와 길 B

엔진이 상담실 흉내를 내는 길이다. 둘로 나뉜다.

#### 길 A: 붙기 — 엔진이 소개장 (참조 번호표) 을 미리 쥐여 준다

- `ConsoleHandle` = 0 · −1 · −2 · −3 이 아닌 양수 핸들 H0. condrv 쪽에선 `\Reference` 객체여야 맞다 (2.9).
- VM 의 test.exe 가 타는 길이다 (H0 = 4, run44 `[관찰]`).
- 표준 핸들 셋 = condrv **Client** 객체 핸들 셋 (`\Input` · `\Output`, 에러는 출력 복사본) — 엔진이 미리 열어 둔다.
  - 붙기 길은 commit 이 표준 핸들을 안 채우기 때문이다 (run83 ④).
- syscall 4 (run83 (a)):

| # | syscall | 엔진이 줄 답 | 실패하면 |
|---|---|---|---|
| a1 | `NtCreateFile("\Connect", RootDirectory = H0, EA "server" + 연결 데이터 0x53c B)` | SUCCESS + 새 핸들 H1 (≠ 0) | `ConsoleInitialize` AL 0 → **kernelbase 시작 FALSE** (`0xc0000022` 면 로우박스 확인 뒤 길 B 로) |
| a2 | `NtDeviceIoControlFile(H1, 0x500023, 출력 8B)` | SUCCESS + 출력 8B 에 서버 PID (아무 수 — 가짜 conhost PID) | 시작은 계속 — 값이 `0 \| 1` 이 될 뿐 |
| a3 | `NtSetInformationProcess(−1, 0x31, &(PID \| 1), 8)` | SUCCESS | 시작 실패로는 안 이어짐 `[정적]` |
| a4 | `NtDeviceIoControlFile(H1, 0x500016, API 0x01000008 GetLangId, body 2B)` | SUCCESS + `WORD` 언어 ID | 오류 코드만 세우고 넘어감. 성공이면 `SetThreadLocale` 이 TEB `CurrentLocale` 을 쓴다 |

- a4 의 VM 값은 동적이다. run44 TEB `CurrentLocale` 이 P1 ~ P2 `0x412` `[관찰]` 이라 같은 값일 것이다 `[추측]`.
- 그 밖에 엔진이 답할 것:
  - 표준 핸들의 `NtQueryVolumeInformationFile` → SUCCESS, **`DeviceType = 0x50`**, `Characteristics = 0x20000` `[추측: 장치 객체 값 그대로]`. 다른 값이면 CRT 가 FDEV 를 안 켜서 `stdout` 이 파일처럼 버퍼링된다 (종료 때만 출력).
  - `NtWriteFile` → SUCCESS, `Information = Length`. 진짜 condrv 는 `0x103` 을 주지만 엔진은 바로 끝내면 된다.
  - 모르는 IOCTL → `0xC0000010`, Connection 줄에 쓰기 → `0xC00000BB`.

#### 길 B: 새 콘솔 — 개업 서류는 그대로, 창구 답만

- `ConsoleHandle = 0` 그대로 — **지금 PE_emulator 값** 이다. `params` 를 안 바꾼다.
- 엔진이 run83 해설 2.7 의 b1 ~ b8 에 답하고, 이어서 b9 ~ b11 (= a2 · a3 · a4, 핸들 Hc) 에 답한다.
  - b3 (`0x500037`) 은 SUCCESS 만 주면 된다 — 진짜로 conhost 를 안 띄워도 된다.
- 표준 핸들은 commit 이 개업 서류에 **저절로 채운다** (`ConsoleCommitState +0x11c` · `+0x12e` · `+0x140`, run83).
- 전제: KUSD `SharedDataFlags` (`0x7ffe02f0`) 비트 6 = 0. VM 은 `0x10e` `[관찰]` (run68). 켜지면 중개 길 (안 읽음).

#### 두 길 모두

- 그 뒤 표준 핸들 셋이 CHAR 로 보이고 `NtWriteFile` 이 성공하면 `printf` 는 끝까지 간다.
- C 로캘 `printf` 만 쓰면 **콘솔 IOCTL 은 commit 의 GetLangId 하나뿐** 이다 `[정적]` (2.6).

#### 시작이 깨지는 곳은 어디인가 — 다듬은 결론 `[정적]`

- 길 A 는 a1 실패, 길 B 는 `ConsoleAllocate` 가 실패를 돌려줄 때 → kernelbase 시작 FALSE.
- **commit 쪽 a2 · a3 · a4 실패로는 시작이 안 깨진다.** 단계별로 (run 문서 "보탠 것" ③, flow-writer `.asm`):
  1. `ConsoleCommitState +0x8a/+0x91` (a2) 뒤 `+0x91 NOP` → `+0x96` — 상태를 안 본다.
  2. `+0xe9/+0xf0` (a3) 뒤 `+0xf0 NOP` → `+0xf5 CALL` — 역시 안 본다.
  3. a2 의 출력 칸 `[RSP+0x70]` 은 `+0x44` 에서 0 으로 두니, a2 가 실패하면 a3 값은 `0 | 1` 이 된다.
  4. `ConsoleInitialize` 는 commit 뒤 `+0x1cf JMP → +0x126 MOV AL,SIL` 로 끝난다.
  5. `SIL` 은 `+0x1b XOR EDI,EDI` · `+0x6f LEA ESI,[RDI+0x1]` 로 1 이고, 그 뒤 안 바뀐다.
  6. 그래서 commit 을 거치면 `ConsoleInitialize` 는 늘 AL 1 이다.
- 결과서 (가) 표의 "`NtSetInformationProcess` 실패를 어떻게 다루는지 안 읽음 `[공백]`" 이 이걸로 좁혀졌다.

#### 세 길을 나란히

run 문서 (가) · (나) 표에서 칸만 모았다.

| | (가) 파일처럼 | (나) 길 A 붙기 | (나) 길 B 새 콘솔 |
|---|---|---|---|
| `ConsoleHandle` | **−1 로 바꿈** | Reference 핸들 H0 | 0 그대로 (지금 값) |
| 표준 핸들 | 엔진이 연 파일 핸들 | 엔진이 연 Client 핸들 셋 | commit 이 채움 |
| 시작 syscall | `NtSetInformationProcess` 1 | 4 | 11 (run83) |
| 표준 핸들 질의 `DeviceType` | 7 · `0x11` | `0x50` | `0x50` |
| C 로캘 `printf` 출력 시점 | 버퍼 찰 때 · 종료 때 | `printf` 마다 | `printf` 마다 |
| 앱이 콘솔 API 직접 | syscall 없이 실패 (진짜 리다이렉트와 같음) | 엔진이 답해야 함 | 엔진이 답해야 함 |

- **지금 우리** (run 문서):
  - 길 B 가 지금 값에 가장 가깝다. 엔진이 b1 ~ b11 에 답하면 `params` 의 표준 핸들이 저절로 채워져 `WriteFile` · `printf` 가 `NtWriteFile` 까지 간다.
  - (가) 는 더 단순하지만 `ConsoleHandle` 을 −1 로 바꿔야 한다.
  - 지금 `NtWriteFile(0)` 까지 온 것이 무엇을 뜻하는지는 run83 `[공백]` 그대로 (PE_emulator 쪽 확인 — run83 해설 2.12).
- 앱이 콘솔 API 를 직접 부를 때 더 답할 것 (`GetConsoleMode` · `GetConsoleOutputCP` · `WriteConsoleW` · `GetConsoleScreenBufferInfo(Ex)` · CRT 의 `CONOUT$` 열기) 은 선택이다. 표는 run 문서 "앱이 콘솔 API 를 직접 부를 때 더 답할 것".

비유:

- 길 A = 엔진이 부모 노릇까지 해서 **참조 번호표 (소개장)** 와 입 · 귀 줄을 미리 만들어 쥐여 주는 것.
- 길 B = 개업 서류는 그대로 두고, 상담실 창구 직원 노릇만 해서 "새 옆집 세워 주세요" 부탁에 차례로 "네, 여기요" 하고 답해 주는 것.

### 2.13 덤 — sample 의 `/MT` CRT 에도 같은 함수가 있다 (이름만)

flow-writer 가 sample 세 모듈의 `_index.tsv` 에서 이름만 확인했다 (run 문서 "보탠 것" ④). **본문은 안 읽었다.**

| 이름 | test | dll1 | dll2 |
|---|---|---|---|
| `initialize_stdio_handles_nolock` | `0x5232c` | `0x50d04` | `0x50cf4` |
| `__acrt_initialize_lowio` | `0x52434` | `0x50e0c` | `0x50dfc` |
| `GetFileType` import 썽크 | `0x6b8d5` | `0x6b637` | `0x6b627` |

- `initialize_stdio_handles_nolock` 는 세 모듈 모두 264B, ucrtbase 는 266B.

그래서 `[추측]` — 단계별로:

1. `/MT` 라 세 모듈이 **각자** CRT 사본을 갖고 있다 (run30 해설: 도구를 집 안에 다 갖고 있는 집).
2. 그 사본에도 "표준 핸들 종류 묻기" 함수와 `GetFileType` 가져오기가 있다 (위 표).
3. 그렇다면 VM 에서 dll2 · dll1 의 CRT 준비 (`dllmain_crt_process_attach`, **은퇴 앞**) 와 test.exe 의 CRT 준비 (`mainCRTStartup`, 은퇴 뒤) 가 각자 표준 핸들 셋에 `GetFileType` → `NtQueryVolumeInformationFile` 을 할 수 있다.
4. 맞다면 **은퇴 앞에 지금 지도 · run44 사진에 없던 syscall** 이 있는 셈이다 (plan Q159 ①).
5. run83 동적 4 의 "IOCTL 이 서는 건 … 있다면 CRT 초기화 쪽 `[추측]`" 은, ucrtbase 기준으로는 IOCTL 이 아니라 **이 질의** 다.
6. 아직 확인 아님 — 정적 Q159 ① (가장 중요) + 동적 Q104 (flow-writer 더함).

- PE_emulator 는 표준 핸들이 0 이라 이 질의도 안 나온다 (2.4 표 첫 줄).
- 짐 지도: 불려도 놓는 것은 각 모듈 `.data` 의 CRT 핸들 표라 체크리스트 밖이다 (baggage.md run84 줄).

비유: 현관 안 짐 푸는 법을 집마다 따로 갖고 있으니 (dll2 · dll1 · test.exe), 세 집이 각자 상담실에 "이 줄 끝이 뭐예요?" 를 물을 수 있다. 그중 둘 (dll) 은 은퇴 앞이다.

### 2.14 run83 을 다듬은 것과 결과서와 다르게 적은 곳 (짧게)

#### run83 뒷정리 (explainer 알림 — 같은 스프린트 문서라 그 자리에서 고침)

- ① run83 (b) "지금 우리" 줄의 "**하나라도 실패를 돌려주면 kernelbase 시작 함수가 FALSE**" 는 너무 넓었다. 이렇게 다듬었다:
  - `ConsoleAllocate` 가 실패를 돌려주면 FALSE (`ConsoleInitialize +0x14d JNS` 안 탐 → AL 0 → `_KernelBaseBaseDllInitialize +0x528 JZ`).
  - commit 쪽 a4 (`GetConsoleLangId`) 실패는 오류 코드만.
  - a2 · a3 실패는 FALSE 근거 없음 (2.12 "시작이 깨지는 곳은 어디인가").
  - **run83 해설에도 같은 넓은 말이 있다** — "한 줄로 먼저" 둘째 글머리 끝, 2.7 "PE_emulator 가 이 길에서 대답해야 할 것" 끝 줄. 그 두 줄은 이 글 2.12 "시작이 깨지는 곳은 어디인가" 로 고쳐 읽으면 된다.
- ② run83 "보탠 것" ⑨: `ConsoleFlags & 2` 갈래의 `ConsoleSanitizeStandardIoObjects` 는 **call `+0x233` / ret `+0x238`** 이다 (결과서 sast/80 `+0x22f` 는 `+0x1c4 JNZ` 의 목적지). 성공이면 `+0x23a JNS → +0x1c6` commit.
- ③ run83 정적 후보 → 대기열 **Q158**.
- 이전 run 이 틀린 사실은 없다. 결과서 sast/80 과 sast/81 도 서로 어긋난 곳이 없다.

#### 결과서와 다르게 적은 곳 · 보탠 것

| # | 무엇 | 이 글 |
|---|---|---|
| ① | `GetFileType` call 은 `+0xc6` / ret `+0xcc` (결과서 `+0xc3 ~ +0xcc` 는 인자 싣기부터). 실패 갈래의 `FILE._file` 쓰기는 `+0x8d JNZ → +0xfd` · `+0x101` (`__piob` 가 이미 있을 때만) | 2.2 줄 1 |
| ② | condrv `DriverEntry` 줄 맞춤 — `+0x77` · `+0x120` · call `+0x128` / ret `+0x12f` (값은 결과서와 같음) | 2.9 ① |
| ③ | commit 쪽 syscall 실패는 시작 실패가 아니다 | 2.12 "시작이 깨지는 곳" |
| ④ | sample 세 모듈 `/MT` CRT 이름 | 2.13 |
| ⑤ | condrv "앱이 안 보내는 IOCTL = conhost 몫" 은 `[추측]` | 2.9 ④ |
| ⑥ | `\Connect` 기준 객체 확인 → run44 "핸들 4 = `\Reference {ConDrv}`" 과 맞음 | 2.9 ② |

디버거 메모 `[정적: 읽은 줄 안에서]`: 이번 갈래를 가르는 칸 (`osfile` · `osfhnd` · 로캘 `+0x138` · `pmode` · `ConsoleHandle`) 을 읽는 줄 가운데 **디버거 여부를 보는 줄은 없었다**.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — kernelbase PDB 줄 | … run83 까지 | + run84 (`CreateFileW` · `CreateFileInternal` 읽기 전용 한 번 — CRT 가 여는 `CONOUT$` 길) |
| 기준 — ntdll PDB 줄 | … run82 까지 | + run84 (`RtlpDosPathNameToRelativeNtPathName` · `RtlpIsDosDeviceName_Ustr` 읽기 전용 한 번 — `CONOUT$` 이름 바꾸기) |
| 기준 — 새 PDB 두 줄 | — | **ucrtbase.pdb** `158F9F38CC492128FE8C9354A209CFB51` · **condrv.pdb** `B37C9D13666B9A282B3CEA43C4AECBF31` (둘 다 처음 analyze, 타입 없음) |
| 기준 — 근거 메모 | — | run84 줄 (정적 바퀴, 값 대조할 칸 없음, run83 값만 다시 씀) |
| 2.8 `Log` 절 | `WriteFile` → `NtWriteFile` 한 줄 (run83) | + "**이 줄은 CRT 가 아니다**" `[정적]` + sample `/MT` CRT 이름 · 은퇴 앞에 불리는지 `[추측]` |
| 3장 "콘솔 입출력" 절 제목 · 머리 | kernelbase 쪽만 | 제목에 "CRT `printf` → `WriteFile` · condrv (run84)", 머리 글에 "(→ run84 …)", 동적 남은 것 줄에 "(해결됨 → run84)" |
| **3장 새 하위 절** | — | "**CRT (ucrtbase) · condrv 쪽과 (가) · (나) 최소 조건**" — 비유 + 트리 + `[정적]` 넷 + (가) · (나) 요약 + 시작 실패 조건 + 동적 남은 것 |
| 4장 run83 정적 줄 | "대기열 번호 없음", "Q149b 로 넘길 것" | **Q158**, "Q149b 로 넘길 것" 은 **(해결됨 → run84 …)** + 남은 것 (`"server"` EA → run84 정적 ⑤, `GetConsoleCP` · 입력 → 범위 밖) |
| 4장 run84 새 줄 셋 | — | 실행 확인 (Q104 — 지금 test.exe 로 / 새 샘플 / 띄우는 쪽 / 엔진 쪽) · 정적 남은 것 ① ~ ⑥ · 다르게 적은 곳 · 보탠 것 · run83 뒷정리 |
| 5장 | run83 까지 | + run84 (표 둘), run83 줄에 "Q158 (run84 때 붙임)" |

태그 · 판단이 바뀐 곳:

- CRT 가 표준 핸들에 무엇을 묻나: run83 해설 2.11 끝 `[추측]` (→ Q149b) → **`[정적]`** (ucrtbase 기준 `GetStdHandle` + `GetFileType` 뿐).
- run83 동적 4 "IOCTL 이 서는 건 … CRT 초기화 쪽 `[추측]`" → ucrtbase 기준 **IOCTL 이 아니라 `NtQueryVolumeInformationFile`** `[정적]`. sample `/MT` 쪽은 여전히 `[추측]`.
- 요청서 가정 "FDEV + `GetConsoleMode` 성공 → `WriteConsoleW`" → **틀림** `[정적]`.
- test.exe `NtWriteFile(0)` 26번 = CRT 아님: `[소스]` + `[정적]` (flow.md 2.8 새 줄).
- run83 "하나라도 실패면 FALSE" → `ConsoleAllocate` 실패 · 길 A a1 실패만 FALSE `[정적]` (2.14).
- 짐 지도 7.6.8 condrv `\Connect` 파일 객체: "condrv 쪽 드라이버 몫은 `[추측]`" → **condrv 몫은 run84 `[정적]`** (만드는 길 · 기준 객체 Reference · 종류 Connection). 객체 안쪽 칸은 `[공백]` 그대로.

### run83 문서와 짐 지도에서 바뀐 것

- **run83 문서** — 뒷정리 셋 (사실이 틀린 건 아님, 2.14):
  - (b) "지금 우리" 줄을 다듬음 (+ 근거 줄).
  - "보탠 것" ⑨ 한 줄.
  - "정적 — 다음 바퀴 후보" 에 대기열 **Q158**.
- **짐 지도** (`baggage.md`): **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로** (마흔째판). 짐 지도 바퀴가 아니다 — CRT 는 진입점 뒤, condrv 는 커널이다.
  - 7.6.8 "(커널) … condrv `\Connect` 파일 객체" 줄에 condrv 쪽 만드는 길을 근거로 붙였다 (`CdpDispatchCreate +0x47` → 표 0번 → `CdCreateConnection`, 기준 객체 Reference, 종류 표 Connection).
  - sample `/MT` CRT 가 은퇴 앞에 불리는지는 `[추측]` 으로 적고, 불려도 체크리스트 밖이라고 적었다.

### 큰 그림에서

- **지도 쪽**: 진입점까지의 정상 길은 안 바뀌었다. 새 질문이 하나 생겼다.
  - 은퇴 앞 dll2 · dll1 의 `/MT` CRT 준비에 `NtQueryVolumeInformationFile` 이 있을 수 있다 (Q159 ①). 맞다면 은퇴 앞 syscall 목록에 줄이 는다.
- **은퇴 앞과 뒤가 이어졌다**:
  - 은퇴 앞 `ConsoleInitialize` 가 개업 서류의 `ConsoleHandle` · 표준 핸들 칸을 정한다 (run83).
  - 은퇴 뒤 CRT 가 그 표준 핸들 칸을 **한 번 묻고** 장부에 딱지를 붙인다 (이번).
  - 쓸 때는 기본이면 딱지와 상관없이 `NtWriteFile` 이다 (run83 + 이번).
- **PE_emulator 쪽**: 고를 길 둘 (가) · (나) 가 표로 정리됐다. 어느 쪽인지는 PE_emulator 가 정한다.
- **다음**: **Q150a** — PE_emulator 요청 28 (user32 · gdi32 초기화 최소 길) 의 user32 쪽.

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 남은 것은 네 무리다.

- **실행해 봐야 아는 값** → **Q104** VM 단계 (결과서 "동적으로 확인해야 할 것" 번호 그대로, 멈춤점은 test.exe 조건). 그 안에서도 셋으로 갈린다:
  - **지금 test.exe 로** 볼 수 있는 것
  - **새 샘플** 이 있어야 보이는 것 → **Q160** (`printf` 만 쓰는 `/MD` 콘솔 프로그램, + `setlocale` 판, + 리다이렉트 판 — 만들지 · 언제는 **사용자 결정**)
  - **엔진 쪽** (PE_emulator 실행 기록)
- **작은 정적** → 새 **Q159** (① ~ ⑥, ① 이 가장 중요).
- **−1 의 출처** → 정적 **Q158 ①** 또는 동적 3.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| sample `/MT` CRT 의 lowio 초기화가 초기화 표에 있나, dll1 · dll2 `dllmain_crt_process_attach` (run40) 아래에서 닿나 = 은퇴 앞 syscall 이 되나 | 이름만 확인, 본문 안 읽음 (2.13) | **Q159 ①** (가장 중요) |
| 위가 실제로 서나 — `nt!NtQueryVolumeInformationFile` 이 은퇴 앞에 몇 번 · 어느 핸들 (`0x68` · `0x6c` · `0x70`?) · `k` 가 `dllN!initialize_stdio_handles_nolock` 인가 | 실행 때 일 | **Q104** (flow-writer 더함, 지금 test.exe 로) |
| `test.exe > out.txt` 로 돌릴 때 `ConsoleHandle` · `StandardOutput` · `ConsoleFlags` · `WindowFlags`, `kernelbase!GetFileType+0x79` 의 `eax` · `DeviceType` (기대 7), 부모 cmd 가 넘기는 `ConsoleHandle` (진짜 핸들일 것 `[추측]`) | 실행 때 값 | **Q104** 동적 2 (지금 test.exe 로 — cmd 리다이렉트만) |
| `Log` 의 `NtWriteFile` `Information` 이 진짜 콘솔에서 `Length` 와 같은가, GetLangId `WORD` | 서버 (conhost) 가 정하는 값 | **Q104** 동적 6 일부 (지금 test.exe 로 — `nt!NtWriteFile` 반환 · `kernelbase!WriteFile+0x8d`, `kernelbase!GetConsoleLangId+0xe8`) |
| C 로캘 CRT 가 콘솔에 실제로 `WriteFile` 로 쓰나 (`ucrtbase!write_text_ansi_nolock+0xc7` 서고, `write_requires_double_translation_nolock+0x89` 안 서야, `initialize_stdio_handles_nolock+0x68` · `+0xcc` 값) | sample 은 `/MT` + 직접 `WriteFile` 이라 ucrtbase 길을 안 탄다 `[소스]` | **Q104** 동적 4 — **새 샘플 (Q160)** |
| `setlocale(LC_ALL, "")` 뒤 로캘 `+0x138` 이 0 이 아니게 되나, 그러면 `GetConsoleMode` 가 불리나 | 실행 때 값 | **Q104** 동적 5 — 새 샘플 (Q160) |
| `GetConsoleMode` 모드 · `GetConsoleOutputCP` 코드 페이지 | test.exe 는 콘솔 API 를 직접 안 부른다 `[소스]` | **Q104** 동적 6 일부 — 새 샘플 (Q160) |
| `stdout` 이 파일일 때 버퍼 크기 · 비우는 시점 | `__acrt_stdio_allocate_buffer_nolock` 안 읽음, 종료 길 안 따라감 | **Q104** 동적 7 (새 샘플 리다이렉트 판) + 정적 **Q159 ②** |
| −1 이 실제 어느 시작 방식에서 오나 (`DETACHED_PROCESS` 로 띄운 자식이 −1 인가) | 값을 쓰는 부모 쪽을 안 읽음 | **Q104** 동적 3 (띄우는 쪽 새 샘플) 또는 정적 **Q158 ①** |
| 지금 PE_emulator 가 `ConsoleInitialize` 를 지났나, `ConsoleHandle` · `WindowFlags` 값, `ConsoleAllocate` 의 `NtCreateFile` 들에 무엇을 답하나 | PE_emulator 쪽 실행 기록 | **Q104** 동적 1 (엔진 쪽 — PE_emulator, run83 `[공백]` 과 같은 몫) |
| `CONOUT$` 의 NT 이름 · 루트 | ntdll `RtlpIsDosDeviceName_Ustr +0x2c7` 뒤를 안 읽음 | 정적 **Q159 ③** |
| `std::cout` 쓰기 길 | msvcp140 아직 analyze 안 됨 | 정적 **Q159 ④** |
| condrv 가 EA `"server"` · 연결 데이터 `0x53c`B 를 읽는 자리 | `CdpCreateServerConnectionIo` 안을 안 읽음 | 정적 **Q159 ⑤** |
| `printf` 반환값 (글자 세는 줄) · `puts` 람다 안쪽 | 직접 안 읽음 `[추측]` | 정적 **Q159 ⑥** |
| `/MD` 프로그램에서 ucrtbase 의 표준 핸들 묻기가 은퇴 앞인가 (1절 내 짐작) | sample 이 `/MT` | 대기열 줄 없음 — Q160 새 샘플을 돌릴 때 같이 보일 것 `[추측]` |
| 입력 쪽 (`_read_nolock` · `ReadConsoleW` · `std::cin`) | 이번 범위 밖 | 기록만 (결과서 동적 10) |

### 요청 27 을 돌아보며 — 한 문단

run83 과 run84, 두 바퀴로 PE_emulator 요청 27 (콘솔 입출력) 이 끝났다. run 문서 맨 위 한 줄 결론을 그대로 풀면 이렇다. 콘솔 프로그램이 화면에 쓰는 길은 거의 다 `NtWriteFile` 하나다 — kernelbase `WriteFile` 도 (run83), CRT 기본 `printf` 도 (run84) 콘솔인지 안 가르고 그리로 가고, CRT 가 시작 때 묻는 건 표준 핸들마다 `GetStdHandle` 과 `GetFileType` 둘뿐이다. 막히는 곳은 쓰기가 아니라 **시작** 이다 — 콘솔 앱이 `ConsoleHandle 0` 이면 kernelbase 가 상담실 (condrv) 에 새 콘솔을 부탁하는 `ConsoleAllocate` 로 가고, 그게 실패하면 kernelbase 전원 켜기가 FALSE 다 (run83). 그래서 PE_emulator 앞에는 길이 둘이다: (가) `ConsoleHandle = −1` 과 파일 손잡이로 상담실을 아예 비켜 가거나, (나) 상담실이 앱에게서 받는 주문서 셋 (`0x500016` · `0x500023` · `0x500037`) 과 열기 · 쓰기에 그럴듯하게 답하거나. 그리고 표준 핸들이 0 이면 CRT `printf` 는 abort 없이 조용히 버려지고, test.exe 의 `NtWriteFile(0)` 26번은 CRT 가 아니라 `sample/log.h:38` 의 `WriteFile` 이다.

---

## 5. 더 읽기

- run 문서: [`run84-console-crt.md`](run84-console-crt.md) — 맨 위 한 줄 결론, "PE_emulator 쪽에서 쓸 것" (요청 27 전체 결론 · 원문 질문이 어디서 답해졌나 · 질문마다 표), **CRT 시작 표**, CRT 쓰기, **(가) 최소 조건 표**, **(나) 길 A · 길 B 표**, 앱이 콘솔 API 를 직접 부를 때 표, 호출 지점 표, 호출 관계 그림, 결과서와 다르게 적은 곳 · 보탠 것 ① ~ ⑥, 정정 (run83 뒷정리), 아직 확인 필요 (동적 1 ~ 10 · 정적 ① ~ ⑥)
- 결과서: [`_workspace/sast/81_console-crt_result.md`](../../_workspace/sast/81_console-crt_result.md) — 한 줄 결론, 질문 1 (초기화 순서 · 장부 칸 · `osfile` 비트), 질문 2 (`printf` 사슬 · 버퍼링 · 갈림 줄), 질문 3 (표준 핸들 0 표), 질문 4 (condrv 디스패치 · 종류 표 · 열기 이름 12칸 · IOCTL 목록), 질문 5 (최소 조건 표 둘)
- 요청서: [`_workspace/sast/81_console-crt.md`](../../_workspace/sast/81_console-crt.md)
- flow.md: [3장 "콘솔 입출력" 절과 새 하위 절 "CRT (ucrtbase) · condrv 쪽과 (가) · (나) 최소 조건"](flow.md) · 2.8 `Log` 줄 · 기준 (ucrtbase · condrv PDB) · 4장 run84 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 7.6.8 "(커널) … condrv `\Connect` 파일 객체" 줄
- 앞 이야기:
  - [run83 해설](run83-console-kernelbase-해설.md) — 짝 바퀴 (kernelbase 쪽). 2.4 갈림길 ② · 2.5 붙기 · 2.7 새 콘솔 · 2.10 `WriteFile` · 2.11 `GetFileType` (이번 2.10 ~ 2.12 의 앞). "한 줄로 먼저" 둘째 글머리 끝과 2.7 끝 줄은 이 글 2.12 로 고쳐 읽기.
  - [run68 해설](run68-dll-init-rest-해설.md) 2.8 — 콘솔 연결 · 소개장 · 출입증 (syscall 수는 run83 정정으로 4)
  - [run66 해설](run66-apphelp-cache-해설.md) 2.7 — 드라이버 · IOCTL · IRP (상담실 · 주문서 · 배달 봉투)
  - [run30 해설](../02_sprint/run30-exe-crt-start-해설.md) 2.1 — CRT · `/MT`, 지도 경계 (현관)
  - [run40 해설](../02_sprint/run40-dll-crt-entry-해설.md) 2.8 — dll 의 CRT 준비 담당 (`dllmain_crt_process_attach`, 이번 2.13)
  - [run44 해설](run44-baggage-snap-해설.md) 2.8 — 사진 P1a · P1b (`ConsoleHandle` 4 → `0x60`)
