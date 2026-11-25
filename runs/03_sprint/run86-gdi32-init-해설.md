# run86 gdi32-init — 해설

> **고쳐 읽기 (run142)**: 2.11 · 표 A13 의 `NtUserGetThreadState(0xe)` 답 "아무 값" — 정상 답은 **상수 1** 이다 `[정적]` (win32kfull `+0x14 JZ → +0x16c MOV EAX,1`, 칸을 안 읽음). `GetAppCompatFlags2` 는 0 이어도 넘어가지만, 0 이면 user32 창 API 넷이 같은 꼴 가드로 실패한다. TEB `+0x78` 을 쓰는 곳은 win32kbase `xxxCreateThreadInfo +0x756` `[정적]` (값은 스레드 ID 로 보임 `[추측]`). → [run142 해설](run142-user32-connect-해설.md) 2.5 · 2.7

> 이 글은 [run86 문서](run86-gdi32-init.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

> **고쳐 읽기 (run121)**: 2.14 ⑧ · 3절 짐 지도의 "짐 지도 3.3 의 `gdi32 6 (Gdi*)` 짐작은 그대로 `[추측]`" — 그 짐작은 둘만 맞다. `GdiTebBatch` · `GdiBatchCount` 는 **gdi32full** (본관) 이 주로 쓰고 (gdi32 는 `InternalDeleteObject` · `DeleteRegion` 둘만, 커널 win32kfull `GdiThreadCalloutFlushUserBatch` 가 비움), 넷 (`GdiCachedProcessHandle` · `GdiClientPID` · `GdiClientTID` · `GdiThreadLocalInfo`) 은 System32 어디에서도 쓰는 줄을 못 찾았다 `[공백]`. "`DllMain` 줄기엔 `Gdi*` 쓰기가 없다" 는 그대로 맞다. 본문은 그대로 두었다 → [run121 해설 2.6](run121-teb-other-dlls-해설.md)

- 대기열: **Q150b** — PE_emulator 요청 28 (user32 · gdi32 초기화 최소 길, "GUI 벽") 의 **둘째 · 마지막 조각**. gdi32 · gdi32full `DllMain` 과 "창 안 만드는 프로세스가 진입점까지 가는 **최소 조건 표 둘**" (`_workspace/plan.md`, 이번에 **끝**)
  - 이것으로 **요청 28 이 끝났다**. 첫 조각 Q150a 는 [run85](run85-user32-init.md) ([해설](run85-user32-init-해설.md)).
  - 다음 항목: **Q151a** (요청 29 첫 조각 — 기본 활성화 문맥) → Q151b → Q152 → Q142 → [멈춤] (Q104 VM 단계 앞).
  - 이번에 남은 것이 간 곳 (4절):
    - 실행해 봐야 아는 것 → **Q162** — run85 가 만든 행 (user32 · gdi32 를 올리는 프로세스). **test.exe 로 볼 수 있는 것은 없어서 Q104 에 붙인 것은 없다.**
    - 정적 → **Q163** ① ~ ⑧. 그중 win32k 쪽 ① ② 는 **PE_emulator · 사용자 결정**.
- 근거:
  - run 문서 [`run86-gdi32-init.md`](run86-gdi32-init.md) (가장 중요)
  - 요청서 [`_workspace/sast/83_gdi32-init.md`](../../_workspace/sast/83_gdi32-init.md) · 결과서 [`83_gdi32-init_result.md`](../../_workspace/sast/83_gdi32-init_result.md)
    - 판정 **분석성공 (최소 길 지도 수준)**. 질문 1 ~ 4 모두 `.asm` 줄로 답했다.
    - gdi32 · gdi32full 을 **처음 analyze** 했다 (PDB 붙음, `SCRIPT ERROR` 0개, 둘 다 1분 안쪽).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 2.4 · **3장 user32 절 아래 새 하위 절** · 4장 · 5장 (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): 근거만 (새 짐 칸 없음, 마흔둘째판)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없다.
  - `[관찰]` 로 적힌 값 둘 (PEB `GdiSharedHandleTable`, KUSD `QpcBypassEnabled`) 은 flow-writer 가 run44 덤프 읽기 로그에서 **다시 읽은 것** 이다 (2.7 · 2.10).

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 user32 · gdi32 를 안 올린다.**
   - `sample/log.h:3` `[소스]`, run44 사진 모듈 6개 `[관찰]` (run85 해설 "조심할 곳" 1).
   - 사진의 PEB `GdiSharedHandleTable` (`+0xf8`) 도 (null) 이다 `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:1779`, `_P2.txt:2489`).
   - 그래서 이번 길도 **흐름도 그림 밖 옆길** 이다. "user32 를 import 하는 **다른** 프로세스라면" 의 이야기다.
2. **"시작 함수" 처럼 보이는 게 셋이다.** 헷갈리기 쉬워서 처음부터 갈라 둔다.

   | 이름 | 무엇 | 누가 부르나 |
   |---|---|---|
   | `Gdi32DllInitialize` (rva `0x5ed0`) | gdi32 의 PE 진입점 | 로더 (이유 1) |
   | `entry` (rva `0x89720`) | gdi32full 의 PE 진입점 | 로더 (이유 1) |
   | `DllMain` (rva `0x89600`) | gdi32full 안 함수 — **진입점이 아니다** | user32 `+0x996 GdiDllInitialize` 를 거쳐서 |

   - 진짜 GDI 초기화는 **셋째** 다 (2.1).
3. **칸 이름 · 뜻은 많이 `[추측]` 이다.**
   - gdi32 · gdi32full PDB 도 공개 심볼만이라 **타입이 0개** 다 (`llvm-pdbutil dump -types` 0 records).
   - 그래서 GDI 공유 메모리 칸 이름 · 뜻은 `[추측]`, **오프셋 · 크기 · 읽는 줄은 `[정적]`** 이다.
4. **`[objdump]` 표시가 붙은 줄이 있다.**
   - kernelbase · ntdll · win32kbase 는 Ghidra 프로젝트를 안 열고 **파일을 `objdump` 로** 읽었다 (다른 decompiler 가 그 프로젝트를 쓰는 중이었다 — 결과서). 이름은 `_index.tsv` 로 붙였다.
   - 그래서 그 줄들은 call 자리 기준이고, ret 은 call + 5 / + 7 이다.
   - win32kbase 는 `[objdump + PDB 공개 이름]` — "꼴 검색" 으로 찾은 것이라 **하한** 이고, 함수 크기도 안 맞췄다 (2.10).
5. **run85 해설에서 "run86 에서 다룸" 이던 자리의 답이 여기 있다** (2.13).
   - 결과서 sast/82 의 gdi32 쪽 짐작 둘은 **틀렸다**.
   - 다만 run85 문서는 그 둘을 옮기지 않았다. 그래서 run 문서 "정정" 은 "**이전 run 이 틀린 것은 없다**" 다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 전원 켜기 = DLL 시작 함수 (DllMain) 부르기, 이유 1 = 프로세스에 처음 붙음, PE 진입점 (`AddressOfEntryPoint`) → [run25 해설](../02_sprint/run25-dll-entry-해설.md) 2.3
- 진입점 = 건물 **현관**, CRT = 현관 안 **짐 푸는 법**, 보안 쿠키 = **봉인 스티커** → [run30 해설](../02_sprint/run30-exe-crt-start-해설.md) 2.1 · 2.5
- `_DllMainCRTStartup` = **정문 경비**, `dllmain_dispatch` = **접수처**, `dllmain_crt_process_attach` = **준비 담당**, `DllMain` = **손님**, 경비가 꼬리 `JMP` 로 콜스택에서 사라짐 → [run40 해설](../02_sprint/run40-dll-crt-entry-해설.md) 비유 · 2.4 · 2.8
- delay import = "**필요하면 그때 부를게요**" 메모 → [run25 해설](../02_sprint/run25-dll-entry-해설.md) 2.5
- `GuardFlags` = **보안 설정 서류**, 명부 줄의 `ProtectDelayLoad` (`0x8000`) **도장** → [run63 해설](run63-cfg-func-table-해설.md) 2.8
- api-set = **별명**, PEB `ApiSetMap` = **별명 사전** → [run37 해설](../02_sprint/run37-k32-import-order-해설.md) · [run57 해설](run57-nls-apiset-해설.md)
- CFG 간접 호출 = **교환원** 을 거쳐 부르기 → [run20 해설](../02_sprint/run20-kernel32-load-해설.md) 2.9
- 프로세스 힙 = 가게 **창고** → [run46 해설](run46-process-heap-해설.md)
- `RtlEncodePointer` 는 열쇠 번호를 **한 번만 묻고 적어 둔다** (캐시가 0 일 때만 syscall) → [run59 해설](run59-init-leftovers-해설.md) 2.7
- `KUSER_SHARED_DATA` (KUSD) = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- `.mrdata` = **유리 덮개 칠판** → [run60 해설](run60-func-table-mrdata-해설.md)
- 동네 기본 규격표 (`NtQuerySystemInformation(0)`) → [run68 해설](run68-dll-init-rest-해설.md) 2.3
- KnownDll 길 = **공용 창고** 에서 꺼내 오기 → [run21 해설](../02_sprint/run21-load-dll-internal-해설.md) 2.4
- win32k = 관리사무소의 **그래픽 부서**, 번호 `0x1000` 이상 = **옆 건물 번호부** → [run51 해설](run51-eprocess-later-해설.md) 2.9 · [run56 해설](run56-syscall-args-해설.md) 2.9
- **run85 해설에서 푼 말** ([run85 해설](run85-user32-init-해설.md)):
  - user32 = **창 담당 건물**, win32u = **옆 건물 단축번호 버튼판** → 2.1
  - USERSRV = 인사팀 **3번 창구**, 캡처 버퍼 = **답장 봉투** → 2.4
  - `SHAREDINFO` = **공용 안내 묶음**, `psi` = 묶음 첫 줄 **화살표** → 2.5
  - `gfServerProcess` = "나는 서버인가" 표시 → 2.11
  - GUI 스레드, 커널 콜백 (관리사무소가 가게 사람을 **거꾸로 불러냄**), `KernelCallbackTable` = **내선 번호표** → 2.12
- 사진 P1a · P2 → [run44 해설](run44-baggage-snap-해설.md) 2.8

이번에 처음 나오는 말 — **gdi32 · gdi32full**, **순환 가져오기**, **보호된 지연 가져오기 풀기**, **GDI 공유 메모리 (`GdiSharedHandleTable`)**, **스톡 객체** (그리고 짧게 **GDI**, **DC**, **fontdrvhost**, **언어팩 (LPK)**, **`QpcBypassEnabled`**) — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, PEB = 가게 **안내판**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**.
- ntdll = 가게 안 **관리실**. DLL = 가게에 들인 **건물**, 시작 함수 = 건물 **전원 켜기**, DLL 전역 = 건물마다 가진 **수첩**.
- csrss = 이웃 **인사팀 사무실**. user32 = **창 담당 건물**. win32k = 관리사무소의 **그래픽 부서**.
- 이번에 새로 붙는 그림:
  - **gdi32 = 그림 도구 건물의 "앞 동"**, **gdi32full = 그 건물의 "본관"**. 앞 동엔 이름표와 본관으로 가는 **통로** (6바이트 `JMP` 껍데기) 가 있고, 실제 일은 본관에서 한다.
  - 두 건물의 **현관** (진입점) 은 거의 빈손이다. 진짜 개점 준비는 창 담당 건물 (user32) 이 **뒷문** (`GdiDllInitialize`) 을 두드릴 때 시작한다.
    - flow.md 3장 새 하위 절은 현관을 "**정문**" 이라고 불렀다. 같은 것이다. 이 글은 시리즈대로 **현관** 으로 쓴다.
  - 뒷문이 열리면 본관은 그래픽 부서에 **첫 신청** (`NtGdiInit2`) 을 넣는다. 그래픽 부서는 가게 땅에 **그림 도구 진열장** (GDI 공유 메모리) 을 걸고, 안내판의 "진열장 위치" 칸 (PEB `+0xf8`) 에 주소를 적어 준다.
    - flow.md 는 이걸 "그림 도구 **창고**" 라고 불렀다. 이 시리즈에서 **창고** 는 이미 힙 (run46) 이라, 이 글은 **진열장** 으로 쓴다. 가리키는 건 같다.
  - 진열장엔 늘 걸려 있는 **기본 붓 · 기본 펜** (스톡 객체) 이 있다. 본관은 개점 준비 끝에 그 둘이 있나 확인한다.
  - 본관은 자기 몫의 **작은 개인 창고** (GDI 힙) 를 따로 짓는다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| ① gdi32 현관 (`Gdi32DllInitialize`) 은 무엇을 하나 | 거의 빈손. syscall 은 `GetProcessMitigationPolicy` 안 `NtQueryInformationProcess(0x34)` **하나** (실패해도 계속). win32k syscall 0, PEB `+0xf8` 안 읽음. FALSE 는 gdi32full (또는 gdi32min) 이 없거나 그 안 이름을 못 찾을 때뿐 `[정적]` (2.3) |
| ② gdi32full `DllMain` 은 누가 부르나 | **로더가 아니다.** gdi32full 현관 (`entry`) 은 이유 1 이면 아무것도 안 하고 1. 진짜 길은 user32 `+0x996 GdiDllInitialize` → gdi32 껍데기 → 지연 가져오기 → gdi32full `GdiDllInitialize` → CRT → `DllMain` `[정적]` (2.4 · 2.6 · 2.7) |
| ③ 그 안 win32k syscall 은 | **`+0x2e NtGdiInit2` (win32u `0x12ee`) 하나, 조건 없이.** user32 `DllMain` 본체의 win32k 다섯은 모두 조건부 (run85) 라, 매니페스트 `uiAccess` 가 없고 `gfServerProcess` 가 아닐 때 이게 **무조건 나가는 첫 win32k syscall** `[정적]` (2.9) |
| ④ 0 을 돌려받으면 | `+0x4a` FALSE → `GdiDllInitialize` `0xc0000001` → user32 FALSE → 로더 `0xc0000142` `[정적]` (2.9) |
| ⑤ `GdiSharedHandleTable` (PEB `+0xf8`) 은 | gdi32full `GdiProcessSetup` 이 읽는다. **쓰는 건 유저 DLL 이 아니라 `win32kbase!GdiProcessCallout +0x22b`** `[objdump + PDB 공개 이름]`. 최소 `0x180150`B 읽기 가능, `+0x180140` · `+0x180148` 은 0 아님 (2.10) |
| ⑥ 두 번 불리는 건 | `GdiDllInitialize` 가 아니라 **`GdiProcessSetup`** (`ClientThreadSetup +0x93` · gdi32full `DllMain +0x67`) `[정적]` (2.11) |
| ⑦ 요청 28 최소 조건 | `DllMain` 을 FALSE 로 끝내는 바깥 답 **다섯** (A1 · A2 · A3 · A7 · A11) + 서로 다른 길에서 오는 **데이터 둘** (csrss 답 `psi` · PEB `+0xf8` GDI 공유 메모리) `[정적]` (2.12) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run85 와 같은 **DLL 전원 켜기** 구간 (flow.md 2.7) 의 옆길이다. 이번엔 그 옆길의 **안쪽 끝** — user32 전원 켜기 안의 `+0x996` 줄과, gdi32 · gdi32full 의 현관 둘이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ kernel32 들이기 + 전원 켜기
 │         │     └─ KERNELBASE 전원 켜기 → csrss 1번 창구에 첫 전화 (run68)
 │         ├─ exe 의 import 들이기 (test.exe 는 dll1 · dll2)
 │         │     (user32 를 import 하는 exe 라면 user32 · gdi32 · gdi32full · win32u 도.
 │         │      gdi32full 은 gdi32 가 정적으로 가져와서 같이 올라온다 [정적: PE 표])        ← 이번 2.5
 │         └─ (2.7) dll 전원 켜기 — LdrpCallInitRoutine
 │               ├─ (test.exe) dll2 → dll1
 │               └─ (user32 를 올리는 프로세스라면 — 차례는 로더가 실행 때 정함. 순환이라서)
 │                    ├─ gdi32 현관 (Gdi32DllInitialize)       거의 빈손                ← 이번 2.3
 │                    ├─ gdi32full 현관 (entry)                 이유 1 이면 그냥 1        ← 이번 2.4
 │                    └─ user32 전원 켜기 (run85)
 │                          └─ +0x996 GdiDllInitialize ⇒ … ⇒ gdi32full DllMain       ← 이번 (진짜 GDI 초기화)
 │                                └─ +0x2e NtGdiInit2   ── 그래픽 부서에 첫 신청
 └─ … → NtContinue (은퇴) → exe 진입점 → main
```

- 셋 다 **은퇴 앞** 이다 (user32 진입점은 2.7 의 `LdrpCallInitRoutine` 이 부른다 — run85).
- 현관 셋을 위 그림처럼 줄 세운 건 **그리기 편해서** 다. 실제 차례는 모른다 → 2.5.

### 왜 이 질문이 지금 왔나

- PE_emulator 는 은퇴 전 DLL 의 `DllMain` 을 **진짜로** 부르고, syscall 은 nt 엔진 · 팀원 몸이 대답한다. csrss 도 win32k 도 **없다** (요청서 "배경", sast/82 와 같은 경계).
- user32 를 import 만 해도 초기화에서 막혔다 (`0xc0000142`).
  - 그 첫 벽 (`NtOpenSection(SharedSection)` `0xc000003a`) 은 run85 가 읽었다 — `CsrPortHandle` 이 0 이었다는 뜻 (run85 해설 2.6).
- 그 벽을 넘으면 user32 는 `+0x996` 에서 gdi32 쪽으로 간다. 그 안이 이번 질문이다.
- 그리고 요청 28 의 **마지막 답** — "창을 안 만드는 프로세스가 진입점까지 가려면 엔진이 무엇을 맞춰야 하나" 표 둘 — 을 run85 와 합쳐서 낸다.
- 사용자 결정 (2026-11-12): **"최소 길 지도" 수준**. 더 깊은 건 "다음 요청 후보" 로.

### 무엇이 궁금했나

요청서 질문 넷.

1. **gdi32 `DllMain` (PROCESS_ATTACH)** — 부르는 함수 · syscall 차례, win32u `NtGdi*` 가 있나, `GdiSharedHandleTable` 을 어디서 받나, FALSE 자리.
2. **gdi32full `DllMain`** — gdi32 가 부르나, 따로 import 되나. `DllMain` 줄기만.
3. **"창 안 만드는 프로세스가 진입점까지 가는 최소 조건" 표 둘** — syscall 답 표 / 데이터 칸 표 (sast/82 와 합쳐서).
4. **"다음 요청 후보"** — 창 만들기 · 메시지 루프 · GDI 그리기로 가려면 더 볼 것.

그리고 짝 바퀴 sast/82 가 끝에 "Q150b 로 넘길 것" 다섯을 남겼다 (`GdiDllInitialize` 안 `NtGdi*` · `GdiSharedHandleTable` 받는 곳 · 음수 자리 · 두 번째 호출 · `GdiProcessSetup`). 답은 2.13 표.

### 바퀴 전에 알던 것

- `[정적]` (run85) user32 `_UserClientDllInitialize +0x996/+0x99d` 가 `GdiDllInitialize(hmodUser32, 1)` 를 **무조건** 부른다. 반환이 음수면 FALSE (FALSE 자리 11번). 반환 규칙은 "음수인가" 만 본다.
- `[정적]` (run85) user32 `DllMain` 본체의 win32k syscall 다섯은 **모두 조건부**. GUI 변환은 그 스레드의 첫 win32k syscall 때 커널이 한다 (커널은 이름만).
- `[정적]` (run85) user32 가 `+0x3fc` 에서 PEB `+0x58` (`KernelCallbackTable`) 에 `apfnDispatch` 를 꽂는다. 84번째 칸이 `__ClientThreadSetup` → `ClientThreadSetup +0x93 GdiProcessSetup`.
- `[정적: PE 표]` (run85) user32 는 `GDI32.dll` 을 정적으로 가져온다 (IAT `0xb4fe0 GdiDllInitialize` · `0xb4fd8 GdiProcessSetup`).
- `[관찰]` (run44 → run85) `\KnownDlls` 에 `user32` · `win32u` · `gdi32` · `gdi32full` 넷.
- `[정적]` (run63) 명부 줄의 `0x8000` (`ProtectDelayLoad`) 도장은 `LdrpCfgProcessLoadConfig +0xc5` 가 찍는다. **누가 읽는지는 몰랐다.**
- 짐작 (결과서 sast/82 — run85 문서엔 안 옮김): "`GdiDllInitialize` 는 두 번 불린다", "`NtGdiInit` (`0x12ed`) 이 첫 win32k syscall 후보", "gdi32full 은 `+0x996` 에서 처음 로드될 수 있다".

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 현관 둘은 빈손, 진짜 일은 뒷문 하나

**새 말 — GDI · gdi32 · gdi32full**

- **GDI** = 붓 · 펜 · 글꼴 · 비트맵으로 화면이나 프린터에 **그리는 쪽** 을 맡는 Windows 부분이다 `[추측: 일반적으로 알려진 설명]`.
- **gdi32** = 그 GDI 의 유저 DLL 중 **바깥 이름표** 쪽. 이번에 본 이름 둘 (`GdiDllInitialize` · `GdiProcessSetup`) 은 **6바이트 `JMP` 껍데기** 였다 `[정적]`. 비유: 그림 도구 건물 **앞 동**.
- **gdi32full** = 실제 몸통. 껍데기가 가리키는 진짜 함수가 여기 있다 `[정적]`. 비유: 그림 도구 건물 **본관**.

run 문서 "호출 관계" 그림을 줄였다. 라벨은 ret 자리, `⇒` 는 꼬리 · 껍데기 `JMP`. 모두 `[정적]` (`[objdump]` 는 따로 적음).

```
[뒷문 — user32 전원 켜기 안, run85 차례 18]
user32!_UserClientDllInitialize (0x6ba84)
 └─ +0x99d  GdiDllInitialize(hmodUser32, 1)                 음수 → +0x9b1 FALSE
      gdi32!GdiDllInitialize (0xa6e0)  +0x0 JMP [0x280e0]     ── 지연 가져오기 칸
        (첫 호출) ntdll 이 칸을 풀어 적음 → NtProtectVirtualMemory ×2   [objdump]    (2.6)
      ⇒ gdi32full!GdiDllInitialize (0x82070)                 반환 0 / 0xc0000017 / 0xc0000001
           ├─ +0x2e  InitializeGdiHeap → HeapCreate(0,0,0)       0 → 0xc0000017       (2.7)
           └─ +0x48  _DllMainCRTStartup(hmodUser32, 1, 0)        0 → 0xc0000001
                 ⇒ dllmain_dispatch → +0x8e DllMain (0x89600)                          (2.8)
                       ├─ +0x29 · +0x2e   이름 확인 · 레지스트리 하나       (실패해도 됨)
                       ├─ +0x35  NtGdiInit2   ── SYSCALL [win32u 0x12ee]   ★ 0 → FALSE    (2.9)
                       ├─ +0x6c  GdiProcessSetup   PEB+0xf8 진열장 읽기                 (2.10 · 2.11)
                       └─ +0x81 · +0x90  GetStockObject(0x12 · 0x13)   0 → FALSE

[현관 — 로더가 부름, 이유 1. 차례는 로더가 정함]
gdi32!Gdi32DllInitialize (0x5ed0)   NtQueryInformationProcess(0x34) 하나 [objdump] · gdi32full 찾기 · RET 0 부르기   (2.3)
gdi32full!entry (0x89720)           이유 1 → +0x2a JNZ → +0x3f   아무것도 안 함 (1)                       (2.4)
win32u                              진입점 없음 (AddressOfEntryPoint 0)
```

- **현관은 둘 다 거의 빈손이다** `[정적]`.
- **진짜 GDI 초기화는 user32 `+0x996` 한 번** 이다 `[정적]`. 부르는 곳이 그 하나뿐이다 — user32 refs `0xb4fe0` 1줄, gdi32 안에서 부르는 코드 없음 (refs `0xa6e0`).
- 그러니 "로더가 gdi32full `DllMain` 을 부른다" 고 흉내 내면 **안 맞는다** (run 문서 "PE_emulator 쪽에서 쓸 것").

비유: 그림 도구 건물은 현관에 불만 켜 두고 손님을 안 받는다. 창 담당 건물 (user32) 이 전원을 켜다가 **뒷문** 을 두드리면, 그제서야 본관에서 경비 → 접수처 → 준비 담당 → 손님 순으로 개점 준비가 돈다. 그 준비 중에 본관은 관리사무소 그래픽 부서에 **첫 신청** 을 넣는다.

### 2.2 한 줄씩 읽는 법

run 문서 "호출 관계" 그림에서 두 줄을 골라 끝까지 읽어 본다.

#### 줄 1: `└─ +0x99d  GdiDllInitialize(hmodUser32, 1)       GDI32 가져오기 (IAT 0xb4fe0)        음수 → +0x9af JNS 안 탐 → +0x9b1 FALSE`

- **누가**: user32 몸통 `_UserClientDllInitialize` (rva `0x6ba84`).
- **어디서**: CALL `+0x996`, 다음 줄 `+0x99d`. 콜스택엔 `user32!_UserClientDllInitialize+0x99d` 로 보일 자리다.
- **누구를**: 가져오기 칸 `0xb4fe0` = gdi32 `GdiDllInitialize` (rva `0xa6e0`).
  - 그런데 그 함수는 `+0x0 JMP [0x280e0]` 한 줄짜리 **껍데기** 다 `[정적]`. 진짜 몸은 그 칸이 가리키는 곳 (2.6).
- **인자**: user32 자기 hmod, 이유 1.
- **무엇을 위해**: 그림 도구 건물 본관의 개점 준비를 시작하려고.
- **실패면**: 반환이 음수면 `+0x9af JNS` 를 안 타고 `+0x9b1` FALSE (run85 FALSE 자리 11번).
- 이 줄은 user32 전체에서 **하나뿐** 이다 (refs 1줄) `[정적]`.

#### 줄 2: `├─ (3) +0x35  NtGdiInit2   ── SYSCALL [win32u 0x12ee] — 이 스레드 첫 win32k syscall` 와 바로 아랫줄 `+0x44 gCookie ← RAX · 0 → +0x4a FALSE`

- **누가**: gdi32full `DllMain` (rva `0x89600`).
- **어디서**: CALL `+0x2e`, ret `+0x35`.
- **누구를**: gdi32full 가져오기 칸 `0xc1fe8` = win32u `NtGdiInit2` 스텁 → 번호 **`0x12ee`** 로 `SYSCALL` `[정적]`. 인자는 없다.
  - `.asm` 의 `CALL [0x1800c1fe8]` 가 PE 가져오기 표의 `win32u!NtGdiInit2` 칸과 같다 (결과서 버전 대조).
- **무엇을 위해**: 관리사무소 그래픽 부서에 처음으로 신청을 넣으려고 (커널 안에서 무엇을 하는지는 2.9).
- **돌아오면**: `RAX` 를 `+0x44` 에서 gdi32 수첩 `gCookie` (gdi32 rva `0x257a0`) 에 적는다.
- **실패면**: 0 이면 `+0x4a JZ → +0xaa` → `DllMain` FALSE.
- 제목 줄의 "이 스레드 첫 win32k syscall" 은 **매니페스트 `uiAccess` 가 없고 `gfServerProcess` 가 아닐 때** 의 말이다 (2.9 의 6번).

---

### 2.3 gdi32 현관 (`Gdi32DllInitialize`) — 일곱 걸음, syscall 하나, FALSE 둘

- PE `AddressOfEntryPoint 0x5ed0` = Ghidra `Gdi32DllInitialize` 시작이다 (flow-writer `objdump -p` 도 같음) `[정적]`.
- gdi32 엔 `^DllMain$` 이름이 없다 (결과서).
- 이유 ≠ 1 이면 `+0x4 CMP EDX,1` · `+0x7 JZ` 를 안 타고 바로 TRUE.

이유 1 이면 (라벨은 ret) `[정적]`, `[objdump]` 는 따로:

| # | 자리 | 하는 일 | syscall | 실패면 |
|---|---|---|---|---|
| 1 | `+0x1b` | `DisableThreadLibraryCalls` — gdi32 자신은 스레드 붙기 · 떨어지기 (이유 2 · 3) 를 안 받음 | 없음 | |
| 2 | `+0x25` | `__security_init_cookie` (봉인 스티커) | `QueryPerformanceCounter` 갈래뿐 — 2.7 과 같은 이야기 `[추측]` | |
| 3 | `+0x34` | `GetCurrentProcess` | 없음 | |
| 4 | `+0x52` | `GetProcessMitigationPolicy(자기, 0xb, &v, 4)` → kernelbase (`0x216f0`) `+0x47/+0x4e` | **`NtQueryInformationProcess` (`0x19`) 클래스 `0x34`** `[objdump]` | `+0x59 JZ` 건너뜀 — **FALSE 아님**. 성공이면 `+0x62` gdi32 `g_systemCallFilterId ← v & 0xf` |
| 5 | `+0x76` | `GetModuleHandleA("gdi32full.dll")`. 0 이면 `+0x7e JZ → +0xab`, `+0xb9` 에서 `"gdi32min.dll"` | 없음 (로더 목록 조회) | 둘 다 0 → `+0xc1` 안 탐 → `+0xc3` **FALSE** |
| 6 | `+0x91` | `GetProcAddress(…, "GdiDllInitializeWrapper")` | 없음 | 0 → `+0x99 JZ → +0xc3` **FALSE** |
| 7 | `+0xa0` | CFG 간접 호출 (교환원 `__guard_dispatch_icall`) → gdi32full `0x71030` | 없음 | `+0xa0 MOV EAX,1` → TRUE |

- **win32k syscall 은 0 개, PEB `+0xf8` 도 안 읽는다** `[정적]`.
  - gdi32 가 가져오는 win32u 함수 118개는 가져오기 표에만 있고, 현관은 안 부른다 (결과서).
- 엔진이 줄 답은 4번의 `NtQueryInformationProcess(0x34)` **하나** 다. 실패해도 계속 간다 (표 A0).
- 덤 — run85 해설 2.8 에서 user32 차례 12 의 같은 `GetProcessMitigationPolicy` 를 "kernelbase 안이라 안 풂 `[추측: NtQueryInformationProcess]`" 이라고 했다.
  - 이번에 그 kernelbase 줄을 `objdump` 로 읽었다 — `+0x47/+0x4e NtQueryInformationProcess` 클래스 `0x34` `[objdump]` (flow.md 4장 run85 정적 ⑦ "일부 해결").

#### 7번이 부르는 건 — "아무것도 안 하는 함수"

- `GetProcAddress` 로 찾은 주소 = gdi32full rva **`0x71030`** = **`RET 0` 한 줄**. 0 을 돌려주는 빈 함수다 `[정적]`.
- 이 주소엔 **이름이 9개** 붙어 있다 (`pdb_aliases.py --lookup gdi32full.dll 0x71030`).
  - 후보: `GdiDllInitializeWrapper` (gdi32 가 찾는 이름) · `LpkPresent` · `GdiEntry12` · `GdiDisableUMPDSandboxing` · `GdiSetServerAttr` 등.
  - 똑같이 생긴 빈 함수들을 링커가 한 주소로 합치면 이렇게 된다 `[추측: 일반적으로 알려진 설명]`. 그래서 Ghidra 가 붙인 이름 (`GdiSetServerAttr`) 이 아니라, gdi32 가 **찾는 이름** 으로 읽으면 된다.
- 그러니 gdi32 현관이 하는 진짜 일은 "**본관이 있나 확인**" 하나다.
  - 본관이 없거나 그 이름표를 못 찾으면 FALSE.
  - 있으면 빈 함수 하나 부르고 TRUE.

비유: 앞 동 현관은 "본관 있어요?" 하고 안쪽 문을 한 번 두드려 보고 끝난다. 문 뒤엔 아무도 없다 (빈 함수). 대답을 들으려던 게 아니라 **문이 있는지** 만 본 것이다.

---

### 2.4 gdi32full 현관 (`entry`) — 이유 1 이면 그냥 1

`entry` (rva `0x89720`, 81B) `[정적]`:

```
gdi32full!entry (0x89720)       이유 1 → +0x23 SUB EAX,2 (−1) · +0x28 CMP EAX,1 → +0x2a JNZ → +0x3f   아무것도 안 함 (1)
 ├─ (1) +0x31  _DllMainCRTStartup        이유 0 · 2 · 3 만
 └─ (2) +0x3f  UninitializeGdiHeap       이유 0 만
```

- 이유 1 (프로세스에 처음 붙음) 이면 `+0x2a JNZ → +0x3f` 로 건너뛰고 `EBX = 1` 을 돌려준다. **정문 경비 (`_DllMainCRTStartup`) 도 안 부른다** `[정적]`.
- 이유 0 · 2 · 3 일 때만 경비를 부른다.
  - gdi32full 은 `DisableThreadLibraryCalls` 를 안 부른다. 그래서 스레드 붙기 · 떨어지기 (2 · 3) 는 경비 → 접수처 → `DllMain` 으로 와서 TRUE 로 끝난다 (결과서. 새 스레드는 이 바퀴 밖).

#### 왜 이렇게 짰을까 — 단계별로

1. 근거 `[정적]`: 현관은 이유 1 에서 아무것도 안 한다 (위).
2. 근거 `[정적]`: 진짜 초기화 (`DllMain`, 그 안의 `NtGdiInit2`) 는 user32 `+0x996` 에서 시작한다 (2.1).
3. 근거 `[정적]` (run85): user32 는 PEB `+0x58` 내선 번호표를 **`+0x3fc` 에서**, 곧 `+0x996` **보다 먼저** 붙인다.
4. 근거 `[정적: PE 표]`: user32 · gdi32 · gdi32full 은 서로를 가져온다 (순환, 2.5). 그래서 로더의 전원 켜기 차례가 "의존하는 쪽이 먼저" 로 깔끔하게 안 정해진다.
5. **그래서** (결과서의 짐작) `[추측]`:
   - gdi32full 이 자기 현관 차례에 스스로 개점 준비를 하면, user32 가 내선 번호표를 붙이기 **전에** 그래픽 부서에 첫 신청을 할 수도 있다.
   - 첫 신청 안은 관리사무소가 내선 번호표로 user32 를 거꾸로 부를 자리다 (2.9). 번호표가 먼저 붙어 있어야 한다.
   - 그래서 "user32 가 준비된 뒤 뒷문으로 불러 달라" 로 짠 것으로 읽힌다.
6. **아직 확인 아님**: 설계 의도는 코드에 안 적혀 있다. 실제 차례는 동적 (Q162 1).

비유: 본관 현관 경비는 이유 1 (첫 출근) 날만 현관을 비운다. 그날은 **뒷문** 에 서 있다가, 창 담당 건물이 "우리 내선 번호표 붙였어요, 이제 시작하세요" 하고 뒷문을 두드리면 그때 일을 시작한다.

---

### 2.5 세 건물이 서로를 가져온다 — 순환, 같이 올라옴, 차례는 로더가

**새 말 — 순환 가져오기**: A 가 B 를, B 가 C 를, C 가 다시 A 를 가져오는 꼴. 이러면 "가져오는 쪽이 나중에 켜진다" 는 순서가 한 줄로 안 선다 `[추측: 일반적으로 알려진 설명]`.

PE 가져오기 표 (flow-writer `objdump -p`, 결과서) `[정적: PE 표]`, 이번 이야기에 나오는 것만:

| 누가 | 정적으로 가져오는 것 |
|---|---|
| user32 | `GDI32.dll` (`GdiDllInitialize` · `GdiProcessSetup`) (run85) |
| gdi32 | `win32u.dll` · **`api-ms-win-gdi-internal-uap-l1-1-0.dll`** (별명 → `gdi32full.dll`) |
| gdi32full | `GDI32.dll` (변수 칸 포함 68개) · `USER32.dll` (36개) · `win32u.dll` (345개) · `ntdll.dll` · `msvcp_win.dll` · UCRT |

- 그래서 **user32 → gdi32 → gdi32full → user32** 로 돈다.
- **gdi32full 은 gdi32 와 같이 올라온다** — gdi32 가 별명 `api-ms-win-gdi-internal-uap-l1-1-0` 으로 정적으로 가져오니까 `[정적: PE 표]`.
  - 별명 → 본명 풀이는 결과서가 `apisetschema.dll` 의 `.apiset` 를 직접 읽어 확인했다.
  - 이게 **정정 셋째** 다. "gdi32full 이 `+0x996` 에서 처음 로드될 수 있다" (sast/82 동적 질문) 는 **맞지 않는다**.
  - 지연 가져오기는 `GdiDllInitialize` · `GdiProcessSetup` 두 이름의 **주소를 처음 풀 때** 몫이다 (2.6).
- **넷 다 `\KnownDlls` 에 있다** (`user32` · `win32u` · `gdi32` · `gdi32full`, run44 사진 `[관찰]` — run85 ⑤). PE_emulator 가 KnownDll 섹션을 흉내 낸다면 이 넷이 필요하다.
- **win32u 는 진입점이 없다** (`AddressOfEntryPoint 0`). 올라오기만 하면 끝이다.

#### run85 의 "gdi32 먼저" 를 고쳐 읽기

- run85 "보탠 것" ⑥ 은 "gdi32 는 user32 `DllMain` 전에 이미 올라와 있다 `[추측: 2.7 초기화 순서 — 의존하는 쪽이 먼저]`" 였다.
  - run85 해설 1장 그림의 "gdi32 먼저 `[추측]` → user32 전원 켜기" 와 2.14 도 같은 말이다.
- 이번에 (run 문서 "보탠 것" ⑨):
  - **올라오는 것 (매핑) 은 맞다** — 위 PE 표.
  - 하지만 **`DllMain` 차례** 는 순환이라 "의존하는 쪽이 먼저" 로 정할 수 없다. 로더가 실행 때 정한다 → 동적 **Q162 1**.
  - 차례가 달라도, 이번에 읽은 줄 안에선 진입점까지 가는 데 걸릴 곳이 안 보인다 `[추측: 읽은 줄 안에서]`.
    - gdi32full 현관은 이유 1 에서 아무것도 안 하고,
    - gdi32 현관은 gdi32full 이 **매핑만** 돼 있으면 되기 때문이다.

---

### 2.6 뒷문 열기 — 지연 가져오기 칸을 처음 풀 때

user32 가 두드린 gdi32 `GdiDllInitialize` 는 `JMP [0x280e0]` 다. 이 `0x280e0` 이 **지연 가져오기 칸** 이다 `[정적]`.

- 처음 값: gdi32 안 `DelayLoad_GdiDllInitialize` (rva `0xa765`) 껍데기를 가리킨다.
- 처음 부르면 ntdll 이 칸을 풀어 **gdi32full `GdiDllInitialize` (rva `0x82070`) 주소** 로 바꿔 적는다.
- 별명: `ext-ms-win-gdi-internal-uap-init-l1-1-0` → `gdi32full.dll` (PEB `ApiSetMap`).
- `GdiProcessSetup` 도 같은 꼴이다: 칸 `0x27b80`, 처음 값 `0xc40c`, 별명 `ext-ms-win-gdi-internal-desktop-l1-1-0` → gdi32full `GdiProcessSetup` (rva `0x81a00`).

**새 말 — 보호된 지연 가져오기 풀기**: run25 의 "필요하면 그때 부를게요" 메모 (delay import) 를 **처음 부를 때**, 관리실이 진짜 번호를 찾아 메모 칸에 적어 두는 일이다. 이 DLL 들은 그 메모 칸에 **덮개** 가 있어서 (보호), 적을 때 덮개를 열고 닫는다.

푸는 길 `[objdump]` (ntdll 프로젝트를 안 열었다):

```
ntdll!LdrResolveDelayLoadedAPI (0xa47d0)
  +0x17f test [항목+0x68],0x8000          ← run63 의 ProtectDelayLoad 도장을 여기서 읽는다
  ├─ 켜짐 → +0x188 LdrpHandleProtectedDelayload (0xa6100)
  │          +0x13e LdrpLoadForwardedDll · +0x2f7 LdrpResolveProcedureAddress
  │          +0x3f0 · +0x550 LdrpWriteBackProtectedDelayLoad (0xa45b0)
  │                     → +0x89 · +0xf1 NtProtectVirtualMemory (0x50)   ── 덮개 열기 · 닫기 [추측]
  └─ 꺼짐 → +0x192 LdrpHandleUnprotectedDelayLoad
```

- **run63 의 도장이 여기서 이어진다.**
  - run63 은 "`GuardFlags` 에 `0x1000` 이 있으면 명부 줄에 `0x8000` (`ProtectDelayLoad`) 도장" 까지였다 (run63 해설 2.8).
  - 그 도장을 **읽는 쪽** 하나가 `LdrResolveDelayLoadedAPI +0x17f` 다 `[objdump]` (flow.md 2.4 run63 절에 한 줄).
  - gdi32 · gdi32full · user32 · win32u 모두 `GuardFlags` 에 `0x1000` 이 있다 (결과서).
- 그래서 syscall 목록에 **`NtProtectVirtualMemory` ×2** 가 더해진다 — `GdiDllInitialize` 칸을 처음 풀 때 한 번, `GdiProcessSetup` 칸을 처음 풀 때 한 번 (표 A6 · A12).
  - "두 번 = 덮개 열기 · 닫기" 라는 뜻은 `[추측]` 이고, 실제로 서는지는 동적 (Q162 7).
- `LdrpLoadForwardedDll` 은 별명 → gdi32full 을 찾는다. 이미 올라와 있으니 **찾기만** 한다 (결과서) — 2.5 와 이어진다.
- 풀기가 실패하면 `LdrpRedirectDelayloadFailure` 갈래로 갈 것이다 `[추측]` (표 A6).

비유: 앞 동 통로 끝 문에 "본관 몇 호 — 처음 오시면 관리실에 물어보세요" 메모가 붙어 있다. 처음 온 사람이 관리실에 물으면, 관리실이 본관 호수를 찾아 메모 칸에 적어 준다. 메모 칸엔 유리 덮개가 있어서 열고 적고 닫는다 (`NtProtectVirtualMemory` 두 번). 다음부터는 바로 간다.

---

### 2.7 gdi32full `GdiDllInitialize` — 작은 창고 짓고, 경비를 거쳐 `DllMain` 으로

gdi32full `GdiDllInitialize` (rva `0x82070`) `[정적]`. 이름은 gdi32 껍데기와 같고, gdi32full 의 `GdiDllInitializeWrapper` (`0x71030`, 빈 함수) 와는 **다른** 함수다.

| 자리 | 하는 일 | 실패 · 건너뜀 |
|---|---|---|
| `+0xa ~ +0x16` | gdi32 수첩 `gMaxGdiHandleCount ← 0x1000000` (**매번**) | |
| `+0x1c` · `+0x1f JNZ` | 이유 ≠ 1 → 0 반환 | |
| `+0x21` · `+0x27 JZ` | `GdiDllFullyInitialized` 가 이미 1 → **0 반환** (두 번째 호출은 여기서 끝) | |
| `+0x29/+0x2e` | `InitializeGdiHeap` (`0x93828`) → `GdiHeap == 0` 이면 `+0x18/+0x1f HeapCreate(0,0,0)` | 0 → `+0x32` **`0xc0000017`** |
| `+0x43/+0x48` | `_DllMainCRTStartup(hmodUser32, 1, 0)` | 0 → `+0x4e/+0x53 UninitializeGdiHeap(0)`, **`0xc0000001`** |
| `+0x5a` | `GdiDllFullyInitialized ← 1` | |
| `+0x61` | 반환 | |

- **반환은 셋뿐** — `0` (성공 · 이미 함 · 이유 ≠ 1), `0xc0000017` (힙), `0xc0000001` (`DllMain` FALSE) `[정적]`.
- user32 는 **음수만** FALSE 로 본다 (run85 해설 2.7). 그래서 뒤 둘이 user32 FALSE → 로더 `0xc0000142` 로 이어진다.
- **GDI 힙** = gdi32full 이 `HeapCreate(0,0,0)` 로 따로 짓는 힙 → ntdll `RtlCreateHeap` → `NtAllocateVirtualMemory` (`0x18`).
  - 몇 번 부르는지는 이번에 안 풀었다 (표 A7, Q163 ⑧).
  - 비유: 가게 큰 창고 (프로세스 힙, run46) 말고 본관이 따로 짓는 **작은 개인 창고**.
- **재밌는 점**: 경비 (`_DllMainCRTStartup`) 에 넘기는 첫 인자가 **user32 의 hmod** 다 (`RDI = hmod 인자 = user32 의 hmod`) `[정적]`. 뒷문으로 들어왔으니 "누가 문을 두드렸나" 가 그대로 간다.

#### 경비 → 접수처 → 준비 담당 → 손님 (run40 그림 그대로)

```
_DllMainCRTStartup (0x86620)
  +0x21 __security_init_cookie                (이유 1 만)
  └─ +0x38 JMP ▶ dllmain_dispatch (0x864e8)    ── 꼬리 점프 (run40 2.4 와 같은 꼴)
        ├─ +0x73  dllmain_crt_dispatch ⇒ +0x3c dllmain_crt_process_attach    CRT · _initterm 12 (syscall · win32k 없음)
        └─ +0x8e  DllMain (0x89600)           0 이면 +0xa5 DllMain(0) · +0xb2 CRT 정리 뒤 0
```

- dll1 · dll2 (run40) 와 같은 MSVC CRT 꼴이다.
- `_initterm` 12개 (WIL · 기능 스테이징 정적 초기화) 는 `atexit` · 객체 만들기뿐, **syscall · win32k 없음** (결과서).
- run 문서 "보탠 것" ②: `DllMain` 을 부르는 줄은 **`dllmain_dispatch +0x89/+0x8e`** 다.
  - 결과서 한 문장이 `dllmain_crt_dispatch` 뒤에 `+0x89` 를 붙여 썼는데, 그 오프셋의 주인은 `dllmain_dispatch` 다. 뜻은 같다.

#### 봉인 스티커 갈래 — 이 VM 에선 syscall 이 안 나갈 것 (표 A8)

단계별로 (run 문서 "보탠 것" ⑤):

1. 근거 `[정적]` (flow-writer `.asm`): gdi32full `__security_init_cookie` 는 스티커 무늬 (`__security_cookie`) 가 **파일 기본값 `0x2b992ddfa232`** 일 때만 (`+0x1e CMP` · `+0x21 JNZ`) `+0x5b QueryPerformanceCounter` 를 부른다.
2. 근거 `[추측]` (결과서 조건): `QueryPerformanceCounter` 는 KUSD 의 `QpcBypassEnabled` 가 **꺼졌을 때만** `NtQueryPerformanceCounter` (`0x31`) 를 부른다.
   - **새 말 — `QpcBypassEnabled`**: 로비 공용 게시판 (KUSD) 의 한 칸. 이름 그대로면 "시계 읽기를 관리사무소를 안 거치고 (bypass) 하나" 표시다 `[추측: 이름 풀이]`.
3. 근거 `[관찰]`: run44 사진의 KUSD `QpcBypassEnabled` = **`0x83`** (`ms/logs/04_baggage-read_P1a.txt:2985`).
4. **그래서** — 결과서 조건대로면 이 VM 에선 `NtQueryPerformanceCounter` 가 **안 나간다** `[추측: 조건은 결과서 추측]`.
5. **아직 모름**: 로더가 DLL 의 스티커 무늬를 먼저 바꿔 두는지 (바꿔 두면 1번에서 QPC 자체를 안 부름) → Q163 ⑧.

- 이 갈래는 gdi32full 만의 것이 아니라 **MSVC DLL 공통** 이다 (표 A8).

---

### 2.8 gdi32full `DllMain` — 일곱 걸음, FALSE 여섯

`DllMain` (rva `0x89600`), 이유 1. `[정적]` (`[objdump]` 는 따로):

| # | ret | 부르는 것 | syscall | 실패면 |
|---|---|---|---|---|
| 1 | `+0x29` | `AssertProcessIsNotFontdrvhost` → `+0x5d K32GetModuleBaseNameW(자기, NULL, buf, 0x11)` | `NtQueryInformationProcess(0)` ×1 + `NtReadVirtualMemory` (`0x3f`) ×5 `[objdump]` | **반환 안 봄** |
| 2 | `+0x2e` | `ReadDisableMetaFilesRegKey` | `NtOpenKey` · `NtQueryValueKey` · `NtClose` | 키 없어도 됨 (`+0x10c JS`) |
| 3 | **`+0x35`** | **`NtGdiInit2`** | **`[win32u 0x12ee]`** | 0 → **`+0x4a` FALSE** |
| 4 | `+0x5d` | gdi32 `LpkpInitializeEditControl(&LpkEditControlInternal, 0xe)` | 없음 | 1 아니면 `+0x65` FALSE (실제론 늘 1) |
| 5 | `+0x6c` | `GdiProcessSetup` (2.10 · 2.11) | (조건부) `[win32u 0x1000]` | 음수 → `+0x6e` FALSE |
| 6 | `+0x75` | `InitializeCFontCache` (`operator new` 둘 + 해시 표, 힙만) | 없음 | 0 → `+0x77` FALSE (늘 1) |
| 7 | `+0x81` · `+0x90` | `GetStockObject(0x12)` · `(0x13)` | **없음** (진열장 읽기) | 앞이 0 → `+0x9e` FALSE, 뒤가 0 → `+0xa3` 0 |

- **FALSE 자리 여섯**: `+0x4a` · `+0x65` · `+0x6e` · `+0x77` · `+0x9e` · `+0xa3`.
  - 다섯은 `+0xaa XOR EAX,EAX` 로 모인다.
  - `+0xa3` 만 `SETNZ BL` 로 0 을 만들어 `+0xa8 JMP` 로 나간다 `[정적]`.
- 그중 **바깥 (엔진) 에 달린 건** 3번 (`NtGdiInit2` 반환) 과, 진열장 내용에 달린 5 · 7번이다. 4 · 6번은 "실제론 안 남" (결과서).

#### 1번 — "내가 fontdrvhost 인가?"

**새 말 — fontdrvhost**: 글꼴 드라이버를 따로 떼어 돌리는 Windows 프로세스 이름 (`fontdrvhost.exe`) 이다 `[추측: 일반적으로 알려진 설명]`.

- gdi32full 은 자기를 올린 exe 이름을 알아내서 `fontdrvhost.exe` 와 비교한다. 같을 때만 텔레메트리 갈래로 간다 `[정적]`. 우리 exe 는 아니다.
- 이름을 알아내는 방법이 재밌다 — **자기 프로세스를 남의 프로세스 읽듯이** 읽는다 (kernelbase `K32GetModuleBaseNameW`, rva `0xb6900`) `[objdump]`.
  1. `NtQueryInformationProcess(자기, 0, 0x30B)` → `PebBaseAddress` (`+0x8`).
  2. `NtReadVirtualMemory(−1, …)` 다섯 번 → PEB `+0x10` (이미지 주소) · PEB `+0x18` (Ldr) · `Ldr+0x20` (메모리 순서 목록 머리) · 로더 항목 0x88B · 이름 글자 0x22B.
  - call 자리는 여섯 (`+0x7d` · `+0xc2` · `+0x108` · `+0x159` · `+0x1ce` · `+0x241`, ret = call + 7). 어느 자리가 어느 syscall 인지는 결과서가 나누지 않았다 (Q163 ⑧).
- 엔진 쪽 (표 A9): NtQIP 은 `PebBaseAddress` 를, NtRVM 은 **자기 주소 공간에서 그대로** 읽어 주면 된다. 실패해도 이름이 0 이 될 뿐 통과다.

비유: 본관이 개점 준비를 하다가 "여기가 글꼴 전문점이던가?" 하고, 가게 안내판을 **관리사무소 열람 창구를 거쳐** 다섯 번 들춰 본다. 아니면 그냥 넘어간다.

#### 2번 — 레지스트리 하나

- 키 `HKLM\Software\Microsoft\Windows NT\CurrentVersion\GRE_Initialize`, 값 `DisableMetaFiles` 를 읽는다 (`+0xfe/+0x105 NtOpenKey` · `+0x14c/+0x153 NtQueryValueKey` · `+0x161/+0x168 NtClose`) `[정적]`.
- 키 · 값이 없어도 된다. `gbDisableMetaFiles` 가 0 으로 남을 뿐이다 (표 A10).

---

### 2.9 ★ `NtGdiInit2` — 그래픽 부서에 거는 첫 신청

#### 왜 "무조건 나가는 첫 win32k syscall" 인가 — 단계별로

1. 근거 `[정적]` (run85): user32 `DllMain` 본체의 win32k syscall 다섯은 **모두 조건부** 다. `psi` 0 채움 · 매니페스트 없음 · IFEO 없음이면 하나도 안 나간다.
2. 근거 `[정적]`: gdi32 현관은 win32k syscall 0, gdi32full 현관은 이유 1 에서 아무것도 안 한다 (2.3 · 2.4).
3. 근거 (표 A6 ~ A10, 태그는 표 그대로): `+0x996` 뒤 `NtGdiInit2` 앞까지 나가는 syscall 은 모두 **ntdll 번호** 다.
   - 지연 풀기 `0x50`, GDI 힙 `0x18`, 봉인 스티커 `0x31` (이 VM 에선 안 나갈 것), 이름 확인 `0x19` · `0x3f`, 레지스트리 `0x12` · `0x17` · `0xf`. CRT `_initterm` 은 syscall 이 없다.
4. 근거 `[정적]`: `DllMain +0x2e` 의 `NtGdiInit2` 는 **조건 없이** 부른다.
5. **그래서**: 매니페스트 `uiAccess` 가 없고 `gfServerProcess` 가 아니면 (6번), 이게 그 스레드의 **첫 win32k syscall** 이다.
   - run85 해설 2.12 끝에 "무조건 부르는 첫 후보는 `GdiDllInitialize` 쪽 → run86" 이라고 남겼던 답이다 — **맞다**, 그 안 gdi32full `DllMain +0x2e NtGdiInit2` 다 (flow.md 3장 user32 절에 한 줄).
6. 조건이 붙는 까닭 `[정적]` (오케스트레이터가 `.asm` 으로 확인): user32 `_UserClientDllInitialize +0x5d2/+0x5d9 NtUserSetProcessUIAccessZorder` (`0x157b`) 는 `+0x996 GdiDllInitialize` 보다 **앞** 이다. 매니페스트 `uiAccess` 일 때만 타고, 아니면 `+0x5c7 JS` · `+0x5d0 JZ` 로 건너뛴다.
   - 그러니 `uiAccess` 가 켜진 프로세스라면 그쪽이 먼저다.
   - 그래서 "첫 win32k syscall" 은 **매니페스트 `uiAccess` 가 없고 `gfServerProcess` 가 아닐 때** 의 말이다 (run 문서 표 A · 결론에도 같은 조건).

#### `NtGdiInit` (`0x12ed`) 는? — 아무도 안 부른다

- `NtGdiInit` 의 가져오기 칸 `0xc1fe0` 을 읽는 건 gdi32full 의 **내보내기 껍데기 `NtGdiInit+0x0`** 뿐이다 (refs) `[정적]`. gdi32 · gdi32full 줄기 어디서도 안 부른다.
- 그래서 sast/82 의 "`NtGdiInit` 이 첫 후보" 는 **틀렸다** (정정 둘째). 이름이 비슷해 생긴 짐작이었다.

#### 커널 안에서 무엇이 일어나나 — 이름만, 순서는 모름

진짜 커널이 `NtGdiInit2` 를 받아 하는 일로 run 문서가 적은 셋 `[추측: 순서 · 때]`:

- ① 스레드를 **GUI 스레드로 바꾼다** — run85 해설 2.12 의 "첫 win32k syscall 때 `KiConvertToGuiThread` → `PsConvertToGuiThread`" (이름만).
- ② PEB `+0xf8` 에 **GDI 공유 메모리 (진열장)** 를 잇는다 — `win32kbase!GdiProcessCallout +0x22b` `[objdump + PDB 공개 이름]` (2.10).
- ③ 내선 번호표 84번 (`__ClientThreadSetup`) 으로 user32 를 **거꾸로 부를 자리** 다 (표 A12, 2.11).

- 돌려주는 64비트 값 (`gCookie` 에 들어가는 것) 의 **정체** 는 모른다 → Q163 ②.
- 엔진 (PE_emulator) 이 지금 `NtGdiInit2` 에 무엇을 돌려주는지는 `[공백]` (Q162 엔진 쪽).

#### 엔진이 `NtGdiInit2` 에서 할 최소 셋 (run 문서)

1. **0 아닌 64비트 값** 을 돌려준다 `[정적]` — 0 이면 `+0x4a` FALSE → `0xc0000001` → user32 FALSE → `0xc0000142`.
2. PEB `+0xf8` 에 **진열장** 을 건다 `[정적]` — 안 걸면 0 아닌 값을 줘도 `GdiProcessSetup +0x161` 에서 넘어진다 (2.10).
3. (선택) **내선 번호표 84번 콜백** — 안 해도 `GdiProcessSetup` 첫 호출이 `DllMain +0x67` 로 옮겨질 뿐, 진입점까지는 갈 것으로 읽힌다 `[추측]` (2.11).

비유: 본관이 그래픽 부서에 "그림 도구 쓰게 해 주세요" 첫 신청을 넣는다. 진짜 관리사무소는 그 자리에서 ① 이 직원을 "그래픽 손님" 으로 등록하고, ② 가게 땅에 **그림 도구 진열장** 을 걸고 안내판에 위치를 적고, ③ 내선 84번으로 창 담당 건물 사람을 불러 준비시킨다 — 이 셋의 순서는 모른다. 그리고 **접수 번호** (0 아닌 값) 를 준다. 접수 번호가 0 이면 본관은 "신청이 안 됐네" 하고 개점을 포기한다.

---

### 2.10 ★ PEB `+0xf8` 진열장 — 누가 걸고, 누가 읽나

**새 말 — GDI 공유 메모리 · `GdiSharedHandleTable`**

- **`GdiSharedHandleTable`** = PEB `+0xf8` 칸의 이름이다 (ntdll PDB `_PEB`). 이 칸이 가리키는 곳이 **GDI 공유 메모리** 다.
- 그 메모리 안 칸 이름 · 뜻은 PDB 에 타입이 없어 `[추측]` 이다. 이 글은 **그림 도구 진열장** 이라고 부른다.

#### 읽는 줄 — gdi32full 만 `[정적]`

| 줄 | 언제 | 무엇을 |
|---|---|---|
| `GdiProcessSetup +0x15a` · `+0x173` | 첫 호출만 | PEB `+0xf8` 를 읽고 `+0x161 CMP [그곳+0x180094],0` |
| `GdiProcessSetup +0x438` (`+0x428 ~ +0x45d`) | **매번** | gdi32 수첩 `pGdiSharedMemory` · `pGdiSharedHandleTable` ← 그 값, `pGdiDevCaps` ← 그 값 `+0x180000` (`+0x456`) |
| `GetStockObject +0x65 ~ +0x72` (← `DllMain +0x7c` · `+0x8b`) | `DllMain` 끝 | `*pGdiSharedMemory + 0x1800b0 + 번호*8` |

#### 진열장 안 — `DllMain` 줄기가 보는 칸 (표 B)

| 진열장 안 자리 | 크기 | 최소 값 | 뜻 `[추측]` | 아니면 |
|---|---|---|---|---|
| `+0x180094` | dword | **0** | 언어팩 켜짐 표시 | 0 아니면 `+0x180 GdiInitializeLanguagePack` 이 더 돈다 |
| `+0x1800b0 + 8*i` (`i` = 0 ~ `0x15`) | qword ×22 | — | 스톡 객체 핸들 표 | `DllMain` 은 아래 둘만 읽음 |
| ↳ `+0x180140` (`i = 0x12`) | qword | **0 아님** | DC 브러시 | 0 → `+0x9e` FALSE |
| ↳ `+0x180148` (`i = 0x13`) | qword | **0 아님** | DC 펜 | 0 → `+0xa3` FALSE |
| `+0x180000` | — | 주소만 계산 (읽지 않음) | 장치 정보 | 읽는 건 그리기 쪽 (Q163 ⑤) |
| 전체 크기 | | **최소 `0x180150`B 읽기 가능** (진짜는 더 큼 `[추측]`) | | 작으면 접근 위반 |

- `0x180150` 은 마지막으로 읽는 칸 (`+0x180148`, 8B) 의 끝이다.

**새 말 — DC · 스톡 객체**

- **DC** = 그림을 그릴 곳 (화면 · 프린터 등) 과 그릴 때 설정을 묶은 것 `[추측: 일반적으로 알려진 설명]`. 비유: 도화지 + 그 도화지 전용 도구 세트.
- **스톡 객체** = 처음부터 만들어져 있는 공용 붓 · 펜 같은 기본 도구 `[추측: 일반적으로 알려진 설명]`.
  - `GetStockObject(0x12 · 0x13)` 은 그중 "DC 브러시" · "DC 펜" 의 **핸들 (번호표)** 을 진열장에서 꺼내 gdi32full 수첩 `ghbrDCBrush` · `ghbrDCPen` 에 적는다 `[정적: 읽는 줄]`.
  - `0x12` · `0x13` 은 DPI 갈래를 안 타고 `+0x65` 기본 갈래로 간다 (결과서). 그래서 syscall 이 없다 (표 A14).

**새 말 — 언어팩 (LPK)**: 아랍어 · 히브리어처럼 오른쪽에서 왼쪽으로 쓰거나 글자 모양이 이어지는 글 (복합 스크립트) 을 다루는 부품 `[추측: 일반적으로 알려진 설명]`.

- `+0x180094` 가 켜져 있으면 `GdiInitializeLanguagePack` → `LpkInitialize` · user32 `InitializeLpkHooks` 가 돌고, user32 `ClientThreadSetup` 의 `NtUserRegisterLPK` 조건이 된다 `[정적]`.
- 최소 길엔 0 (안 탐).

#### 널이면 — FALSE 가 아니라 넘어진다

1. `GdiProcessSetup +0x161` 은 PEB `+0xf8` 값에 `+0x180094` 를 더한 곳을 **바로 읽는다** `[정적]`.
2. PEB `+0xf8` 이 0 이면 주소 **`0x180094`** 를 읽으려다 **접근 위반** 이 난다 (run 문서 · 결과서 "지금 우리" ②).
3. 곧 "`NtGdiInit2` 가 0 아닌 값 + PEB `+0xf8` 0" 은, `DllMain` 이 FALSE 도 못 돌려주고 **터지는** 조합이다.
   - run85 의 "`psi` 널 → `+0x38e` 접근 위반" 과 같은 꼴이다 (run85 해설 2.5).

#### 거는 쪽 — 유저 DLL 이 아니라 그래픽 부서 (단계별로)

1. 근거 (결과서 한계 메모): 유저 DLL 다섯 (gdi32 · gdi32full · user32 · ntdll · kernelbase) 을 `objdump` 로 훑었다. "`gs:0x60` (PEB 읽기) 뒤 8줄 안에 `[reg+0xf8]`" 꼴을 찾았는데 **못 찾았다**.
   - 이 꼴 검색은 **하한** 이다. PEB 포인터를 다른 함수에서 받아 쓰는 모양은 못 잡는다.
2. 근거 `[objdump + PDB 공개 이름]`: win32kbase (`.sys`) 에서 "`PsGetProcessPeb` 호출 뒤 150줄 안 `[reg+0xf8]` 쓰기" 를 찾았다. 9곳 중 PEB 쓰기 모양은 **하나** — `GdiProcessCallout` (rva `0xe2010`) 안, `+0xca PsGetProcessPeb` 뒤 **`+0x22b`**.
   - 함수 이름은 "가장 가까운 앞쪽 공개 PDB 이름" 으로 붙였고, 함수 크기는 안 맞췄다.
3. 근거 `[관찰]`: test.exe 사진의 PEB `GdiSharedHandleTable` = (null) (`_P1a.txt:1779` · `_P2.txt:2489`). gdi32 를 안 올린 것과 맞고, 짐 지도 7.2 "끝까지 0" 과도 맞다.
4. **그래서**: 진열장은 **커널 (win32k) 이 그 프로세스의 첫 `NtGdiInit2` 때** 거는 것으로 읽힌다 `[추측: 때]` (짐 지도 7.2 메모).
5. **아직 확인 아님**:
   - 실제로 `+0x22b` 가 서는지 → 동적 Q162 2 (`ba w8 @$peb+0xf8`).
   - 진열장 내용을 누가 어떻게 채우는지 → 정적 Q163 ① (사용자 결정).

#### run85 의 데이터와 **다른 길** 이다

| | user32 쪽 (run85) | gdi32 쪽 (이번) |
|---|---|---|
| 무엇 | csrss 답장 봉투 0x248B → `gSharedInfo` · `gpsi` | PEB `+0xf8` 이 가리키는 GDI 공유 메모리 |
| 누가 주나 | 인사팀 3번 창구 (csrss USERSRV) | 그래픽 부서 (win32k) |
| 최소 | `psi` → 읽기 가능 `0x1b68`B (0 채움) | 읽기 가능 `0x180150`B, `+0x180140` · `+0x180148` 0 아님, `+0x180094` 0 |
| 없으면 | `psi` 널 → `+0x38e` 접근 위반 | PEB `+0xf8` 0 → `+0x161` 접근 위반 |

- **이 둘은 같은 메모리가 아니다** `[정적]`. `SharedSection` 뷰를 채운다고 GDI 쪽이 풀리지 않는다.
- PE_emulator 에 "공유 핸들 표 = `SharedSection` 뷰" 같은 가정이 있으면 **안 맞는다** (결과서 질문 3 "지금 우리").

비유: 창 담당 건물은 인사팀 3번 창구에서 **공용 안내 묶음** 을 받고, 그림 도구 건물은 그래픽 부서가 걸어 준 **진열장** 을 본다. 받는 곳도, 주는 사람도 다르다. 하나를 채웠다고 다른 하나가 생기지 않는다.

---

### 2.11 `GdiProcessSetup` — 두 번 불리는 건 이쪽

`GdiProcessSetup` (gdi32full rva `0x81a00`, 1172B) 을 부르는 곳은 **둘** 이다 `[정적]`:

- user32 `ClientThreadSetup +0x93/+0x9a` (내선 84번 콜백 안, 첫 GUI 스레드) — gdi32 껍데기 `JMP [0x27b80]` 를 거쳐서.
- gdi32full `DllMain +0x67/+0x6c`.

**`gbFirst`** (rva `0x108474`, **파일 처음 값 1**) 로 두 갈래:

```
gdi32full!GdiProcessSetup (0x81a00)
  +0x15 gbFirst == 0 → +0x1b JZ → +0x428 (둘째 호출 — 공통 끝만)
  첫 호출:
    임계구역 ×6 · +0xfe GetACP · +0x142 gW32PID ← TEB+0x40 & ~3
    +0x161 [PEB+0xf8]+0x180094 ≠ 0 이면 +0x185 GdiInitializeLanguagePack
    +0x18e gbFirst ← 0
    +0x1d8 ~ +0x3d3 RtlEncodePointer(0) ×25 · +0x3eb UspProcessAttach · +0x401 GetModuleHandleW · +0x41c GetProcAddress
  공통 +0x428 ~ +0x45d   pGdiSharedMemory · pGdiSharedHandleTable ← PEB+0xf8 · pGdiDevCaps ← +0x180000
  └─ +0x46b  user32!GetAppCompatFlags2(0x400) → (TEB+0x78 == 0) +0x4c NtUserGetThreadState(0xe)   ── [win32u 0x1000]
       +0x470 ~ +0x485 GdiBatchLimit ← 0x14 (플래그 0x200 꺼짐) / 0
```

- **반환**: 첫 호출은 마지막 `RtlInitializeCriticalSection` 결과, 둘째는 0 (결과서). 음수면 `DllMain +0x6e` FALSE.
- **`RtlEncodePointer(0)` ×25 는 syscall 이 없다** `[정적: run59 줄 + 차례]` (run 문서 "보탠 것" ④).
  - `RtlEncodePointer` 는 열쇠 번호 캐시가 **0 일 때만** `NtQueryInformationProcess(0x24)` 를 부른다 (run59 해설 2.7).
  - 그 캐시는 `LdrpInitializeProcess +0x12f` 가 이미 채웠다. 그러니 여기선 적어 둔 번호만 쓴다.

#### 첫 호출은 누구? — 두 경우

| 경우 | 첫 호출 | `DllMain +0x67` 은 |
|---|---|---|
| 콜백이 `NtGdiInit2` 안에서 돈다 | `ClientThreadSetup +0x93` (처음이면 칸 `0x27b80` 지연 풀기 `NtProtectVirtualMemory` ×2 가 더 붙음) | 둘째 → `+0x1b → +0x428` |
| 엔진이 콜백을 안 부른다 | `DllMain +0x67` | 첫 호출 |

- 어느 쪽이 진짜인지 → 동적 **Q162 4** (`gdi32full!GdiProcessSetup` 입구에서 `kb`).
- 콜백을 안 부르면 `ClientThreadSetup` 이 안 돌아 `pdiLocal` · TEB `+0x820` 이 빈 채로 남는다. 그래도 진입점 도달엔 필수 아님 `[추측]` (표 A12).

#### 끝의 `GetAppCompatFlags2` — win32k syscall 이 하나 더 나갈 수 있는 곳

user32 `GetAppCompatFlags2` (rva `0x6100`) `[정적]`:

1. `+0x12` TEB `+0x78` (`Win32ThreadInfo`) 가 0 인가 본다.
2. **0 이면** `+0x45/+0x4c NtUserGetThreadState(0xe)` (`[win32u 0x1000]`) 를 부른다. 반환이 0 이면 그냥 0.
3. **0 이 아니면** TEB `+0x810` · `+0x818` 을 읽는다 (run 문서 "보탠 것" ⑦): `+0x22 CMP EBX(0x400),[TEB+0x810]`, `+0x28 JC` 면 0, 아니면 `+0x33` 의 `[TEB+0x818]`.

- TEB `+0x78` 은 `NtGdiInit2` 가 GUI 로 바꾼 뒤엔 0 아님이 정상일 것이다 `[추측]` → 그러면 syscall 없음.
- 엔진이 TEB 를 0 으로 채워 두면 `0x1000` 이 한 번 더 나간다. **답은 아무거나** 다 (표 A13).
- TEB 0 채움이면 `GetAppCompatFlags2` 가 0 → `GdiBatchLimit = 0x14` (보탠 것 ⑦).

---

### 2.12 ★ 요청 28 의 마지막 답 — 최소 조건 표 둘

run85 + run86 을 합친 표다. 전제: 창 없는 보통 프로세스 (`gfServerProcess == 0`), `psi` 0 채움, 매니페스트 · IFEO 없음 (결과서 질문 3).

#### 표 A — syscall 답 (대표만, 전체는 run 문서)

**`DllMain` 을 FALSE 로 끝내는 다섯** `[정적]` — 엔진이 꼭 맞춰야 하는 바깥 답:

| 차례 | 어디 | syscall | 엔진이 줄 답 | 아니면 |
|---|---|---|---|---|
| A1 | user32 `+0x1d1/+0x1d8` (run85) | `[ntdll 0x36]` `NtQuerySystemInformation(0, 0x40B)` | 성공 + 버퍼 `+0x28` `MaximumUserModeAddress` | FALSE (`+0x1df JS`) |
| A2 | user32 `+0x1f3` `InitializeNtdllUserPfn` → ntdll `RtlInitializeNtUserPfn` (run85) | `[ntdll 0x50]` `NtProtectVirtualMemory` ×2 (`.mrdata` 열기 · 닫기) | 성공 (이 ntdll 함수는 프로세스당 한 번만 성공) | FALSE (`+0x1fa JZ`) |
| A3 | user32 `+0x2aa/+0x2b1` → … → `CsrClientCallServer +0xed/+0xf2` (run85) | `[ntdll 0x8e]` `NtAlpcSendWaitReceivePort` (서버 3) | 메시지 `+0x34` 0 + 봉투 `+8` 부터 `SHAREDINFO` 모양, `psi` 읽기 가능 `0x1b68`B. 그때 `CsrPortHandle` ≠ 0 | 음수 → FALSE. `CsrPortHandle == 0` 이면 첫 연결 길 (PE_emulator 가 본 `0xc000003a`) |
| A7 | gdi32full `GdiDllInitialize +0x29` → `HeapCreate(0,0,0)` | `[ntdll 0x18]` `NtAllocateVirtualMemory` | 성공 | `0xc0000017` → user32 FALSE |
| **A11** | **gdi32full `DllMain +0x2e/+0x35`** | **`[win32u 0x12ee]` `NtGdiInit2`** | **0 아닌 64비트 값** (+ 부수 효과로 PEB `+0xf8` 진열장) | `0xc0000001` → user32 FALSE → `0xc0000142` |

**실패해도 통과하는 것** — 엔진은 대충 답해도 된다 (태그는 run 문서 표 A 그대로):

| 차례 | 무엇 | 비고 |
|---|---|---|
| A0 | gdi32 현관 `NtQueryInformationProcess(0x34)` `[objdump]` | 성공 + 0 이 무난 (2.3) |
| A4 | user32 차례 12 의 같은 `GetProcessMitigationPolicy` | 답의 `v & 0xf` 가 5 면 run85 차례 13 갈래 |
| A5 · A10 | 레지스트리 (`NtOpenKey 0x12` · `NtQueryValueKey 0x17` · `NtClose 0xf`) | 키 없음도 됨 |
| A5″ | user32 `+0x5d2/+0x5d9` `NtUserSetProcessUIAccessZorder` `[win32u 0x157b]` — 매니페스트 `uiAccess` 일 때만 | 타면 A11 보다 앞의 첫 win32k syscall (2.9 의 6번) |
| A5′ | user32 TraceLogging ×3 → `NtTraceControl` `[추측: run62 · run68 꼴]` | 안 봄 (Q161 ⑦) |
| A6 | 지연 풀기 `NtProtectVirtualMemory` ×2 `[objdump]` | 풀기 실패 → `LdrpRedirectDelayloadFailure` `[추측]` (2.6) |
| A8 | `NtQueryPerformanceCounter 0x31` — 쿠키 파일 기본값 + `QpcBypassEnabled` 꺼짐일 때만 `[추측]` | 이 VM 에선 안 나갈 것 (2.7) |
| A9 | `NtQueryInformationProcess(0)` + `NtReadVirtualMemory` ×5 `[objdump]` | 자기 메모리 그대로 (2.8) |
| A12 | (콜백 안) `GdiProcessSetup` 지연 풀기 ×2 · `ClientThreadSetup` 의 `0x151f` · `0x107b` · `0x1475` · (`0x150a`) | 엔진이 콜백을 안 하면 안 나감 (2.11) |
| A13 | `NtUserGetThreadState(0xe)` `[win32u 0x1000]` — TEB `+0x78` 0 일 때만 | 아무 값 (2.11) |
| A14 | `GetStockObject` | **syscall 없음** (진열장 읽기) |
| A15 | user32 `+0x9b8` 뒤 조건부 win32k 넷 (`0x1576` · `0x1440` · `0x13ec` · `0x1579`) — `0x157b` 는 `+0x996` 앞이라 A5″ 로 옮김 | `psi` 0 채움 · 매니페스트 없음이면 안 부름 |

- A6 ~ A13 은 user32 `DllMain` 안에서 `GdiDllInitialize` 를 **한 번** 부르는 동안, **한 스레드에서** 일어난다.

#### 표 B — 데이터 둘

- **user32 쪽** (run85): csrss 답 0x248B 의 `+8` → `gSharedInfo`, 첫 8B → `psi` (읽기 가능 `0x1b68`B, 0 채움 OK). 그리고 ntdll `CsrPortHandle` ≠ 0, `.mrdata` `NtUserPfn` 플래그 0 (파일 값).
- **gdi32 쪽** (이번): PEB `+0xf8` → 진열장 (2.10 표).
- 그 밖 — 모두 "진짜 값 그대로면 됨":
  - PEB `+0x58` `KernelCallbackTable` — user32 가 `+0x3fc` 에서 채운다 (`+0x996` **보다 먼저**). 0 이면 콜백을 못 부른다.
  - TEB `+0x40` (프로세스 ID → `gW32PID`), TEB `+0x78` · `+0x810` · `+0x818` (2.11).
  - PEB `+0x10` · `+0x18` · `Ldr+0x20` · 로더 항목 — `K32GetModuleBaseNameW` 가 읽는다 (2.8).
  - gdi32 수첩 칸 (`gMaxGdiHandleCount` · `gCookie` · `pGdiSharedMemory` …) 은 gdi32full 이 가져오기 표로 쓴다. gdi32full 전역 (`gbFirst` 1 · 나머지 0) 은 **파일 값 그대로** 면 문제 없다.
  - PEB `ApiSetMap` 의 별명 셋 → `gdi32full.dll`.

#### PE_emulator 에게

- 지금 막힌 곳 (`NtOpenSection(SharedSection)` `0xc000003a`) 은 run85 대로 `CsrPortHandle` 이 0 이었다는 뜻이다 (run85 해설 2.6).
- 그걸 넘기면 **다음으로 걸릴 곳은 A11 (`NtGdiInit2`) · PEB `+0xf8`** 일 것이다 `[추측: 엔진이 지금 win32k syscall 에 무엇을 돌려주는지 모름]`.
- 엔진의 `NtGdiInit2` 최소 셋 = ① 0 아닌 값 ② PEB `+0xf8` 진열장 ③ (선택) 내선 84번 콜백 (2.9).
- 흉내 낼 때 조심: "**로더가 gdi32full `DllMain` 을 부른다**" 로 흉내 내면 안 맞는다 — user32 가 뒷문으로 부른다 (2.1).

비유로 다시: 창 담당 건물과 그림 도구 건물이 문을 열려면, 관리사무소 (엔진) 가 꼭 내줘야 할 서류가 **다섯 장** 이다 — 동네 규격표 · 유리 덮개 열기 허락 · 인사팀 3번 창구 답장 · 작은 창고 땅 · 그래픽 부서 접수 번호. 그리고 걸어 줘야 할 판이 **두 개** 다 — 인사팀이 주는 공용 안내 묶음, 그래픽 부서가 거는 진열장.

---

### 2.13 run85 해설에서 "run86 에서 다룸" 이던 자리 — 답

| run85 해설 자리 | 거기 남긴 말 | 이번 답 |
|---|---|---|
| "조심할 곳" 5 · 2.15 정정 | 결과서 sast/82 의 gdi32 쪽 짐작 둘 → run86 | ① "`GdiDllInitialize` 가 gdi32 `DllMain` 과 user32 `DllMain` 에서 두 번 (다른 `hmod`) 불린다" → **틀림.** 부르는 곳은 user32 `+0x996` 하나 (refs `0xb4fe0` 1줄). gdi32 현관은 `GdiDllInitializeWrapper` (= `RET 0`) 를 부른다 (2.3). **두 번 불리는 건 `GdiProcessSetup`** (2.11). `GdiDllInitialize` 도 두 번째면 `GdiDllFullyInitialized` 로 0 (2.7). ② "`NtGdiInit` (`0x12ed`) 이 첫 win32k 후보" → **틀림.** 실제는 **`NtGdiInit2` (`0x12ee`)**, gdi32full `DllMain +0x2e` (2.9) |
| 2.12 끝 | `+0x996` 안에서 win32k syscall 이 나가는지 → run86 | **나간다.** gdi32full `DllMain +0x2e NtGdiInit2` — 무조건 나가는 첫 win32k syscall (매니페스트 `uiAccess` 가 없고 `gfServerProcess` 가 아닐 때, 2.9) |
| 2.13 끝 | gdi32 쪽 번호 (`NtGdi*`) → run86 | 이번 길의 win32u 번호는 **`0x12ee` `NtGdiInit2`** (스텁 rva `0x7070`) 와 **`0x1000` `NtUserGetThreadState`** (rva `0x12b0`, 조건부) 둘. `NtGdiInit` (`0x12ed`, rva `0x7050`) 은 안 불림. 스텁 `MOV EAX` 값이 `_syscalls.tsv` 와 같다 |
| 2.14 · 1장 그림 | "gdi32 는 user32 `DllMain` 전에 이미 올라와 있을 것 `[추측: 의존하는 쪽이 먼저]`", "gdi32 먼저 `[추측]` → user32 전원 켜기" | **올라오는 것 (매핑) 은 맞다** `[정적: PE 표]`. **`DllMain` 차례는 순환이라 모른다** (Q162 1) (2.5) |
| 2.7 FALSE 자리 11번 | `GdiDllInitialize` 음수 → run86 | 음수는 `0xc0000017` (힙) · `0xc0000001` (gdi32full `DllMain` FALSE) 둘뿐 (2.7) |
| 4절 표 | gdi32full 이 user32 `DllMain` 때 이미 올라와 있나 | gdi32 가 정적으로 가져와 **같이 올라온다** `[정적: PE 표]`. 실행 확인은 Q162 그대로 (2.5) |

- run85 **문서** 에도 정정 표시 자리 (win32u 번호표 `0x12ed · 0x12ee` 줄, "정정" 절 두 줄) 에 결과가 한 줄씩 이어졌다 (run 문서 "정정").
- run85 **해설** 맨 위에도 "고쳐 읽기 (run86)" 한 줄로 이 절을 가리켜 두었다.

---

### 2.14 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

run 문서 "결과서와 다르게 적은 곳 · 보탠 것" ① ~ ⑨ — 본문에 다 녹였다:

- ① `GetAppCompatFlags2` call = `GdiProcessSetup +0x464` (ret `+0x46b`). 결과서 본문 한 곳의 `+0x460` 은 인자 싣는 줄 (`MOV CX,0x400`) → 2.11
- ② gdi32full `DllMain` 을 부르는 줄 = `dllmain_dispatch +0x89/+0x8e` → 2.7
- ③ gdi32 `+0xc1 JNZ` 의 목적지는 `+0x80` (gdi32min 을 찾으면 같은 `GetProcAddress` 줄로 돌아감). 뜻은 같다 → 2.3
- ④ `RtlEncodePointer(0)` ×25 는 syscall 없음 (run59) → 2.11
- ⑤ 봉인 스티커 QPC 갈래 + KUSD `QpcBypassEnabled 0x83` `[관찰]` → 2.7
- ⑥ 사진 PEB `GdiSharedHandleTable` (null) `[관찰]` → 2.10
- ⑦ TEB 0 채움이면 `GdiBatchLimit 0x14` → 2.11
- ⑧ gdi32 · gdi32full 이 TEB 를 건드리는 줄 (뽑은 `.asm` 안 `GS:` 검색): `GdiProcessSetup +0xa` (→ `+0x142` 프로세스 ID) · `+0x151` · `+0x16a` · `+0x428` (PEB) · TLS 포인터 `GS:[0x58]` 셋 · `__scrt_acquire_startup_lock +0xd`.
  - **TEB `Gdi*` 칸 쓰기는 이 줄기에 없다.** 짐 지도 3.3 의 "gdi32 6 (`Gdi*`)" 짐작은 그대로 `[추측]` (쓰는 때는 그리기 쪽일 것) → 3절 짐 지도
- ⑨ run85 ⑥ "의존하는 쪽이 먼저" — 매핑은 맞고, `DllMain` 차례는 동적 → 2.5
- 이름 확인: 이번에 이름을 쓴 함수는 `pdb_aliases.py --lookup` 으로 별칭 무리가 아님을 확인했다. gdi32full `0x71030` 만 같은 주소 이름 9개 (2.3).
- 정정 셋 (결과서 sast/82 짐작, run85 문서엔 안 옮김): `GdiDllInitialize` 두 번 → `GdiProcessSetup` 두 번 · `NtGdiInit` → `NtGdiInit2` · gdi32full `+0x996` 첫 로드 → gdi32 와 같이 올라옴. **이전 run 이 틀린 것은 없다.**
- 디버거가 붙어서 달라지는 점: 이번에 읽은 gdi32 · gdi32full 줄기에선 디버거 여부를 보는 줄이 없었다 `[정적: 읽은 줄 안에서]`. KUSD `QpcBypassEnabled` 는 디버거와 무관한 값이다 `[추측]`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 — 새 PDB 셋 | — | **gdi32.pdb** `92C6EC2ABE478836C99BC83C2F3AE3551` · **gdi32full.pdb** `A6245E4F5C78EB06438EA4820F1F6EB41` (둘 다 처음 analyze, 타입 없음) · **win32kbase.pdb** `D22DFDA3D54B87E6BD0567A57877904C1` (Ghidra 없이 `objdump` + 공개 이름 — `GdiProcessCallout +0xca` · `+0x22b`) |
| 기준 — kernelbase · ntdll · user32 · win32u PDB 줄 | … run85 까지 | + run86 몫 (kernelbase `objdump`: `GetProcessMitigationPolicy +0x47` · `K32GetModuleBaseNameW` 여섯 자리 / ntdll `objdump`: 지연 가져오기 길 / user32 depth 0 넷 · refs 둘 / win32u `0x12ee` · `0x12ed` · `0x1000`) |
| 기준 — 근거 메모 | — | run86 줄 (정적 바퀴, 사진 PEB `+0xf8` null · KUSD `QpcBypassEnabled 0x83`) |
| 2.4 머리 run 목록 | … run82 | + run86 |
| 2.4 run63 CFG 절 (`0x8000`) | 도장을 "누가 찍나" 까지 | + 한 줄: **읽는 쪽** `LdrResolveDelayLoadedAPI +0x17f` → `+0x188 LdrpHandleProtectedDelayload` … `NtProtectVirtualMemory` 두 번 `[objdump]` |
| 3장 user32 절 제목 · 첫 문단 | gdi32 쪽은 "짝 바퀴 Q150b" | + gdi32 쪽 이름 (run86), "→ 아래 하위 절" |
| 3장 user32 절 GUI 스레드 글머리 | "무조건 부르는 첫 후보는 `+0x996` 쪽 → run86" | + "(run86 `[정적]`: 맞다 — gdi32full `DllMain +0x2e NtGdiInit2` …)" |
| **3장 새 하위 절** | — | "**gdi32 · gdi32full 시작 함수와 '창 안 만드는 프로세스' 최소 조건**" — 비유 · 트리 · `[정적]` 다섯 · `[정적: PE 표]` 하나 · 요청 28 최소 조건 · 남은 것 |
| 4장 run85 줄 셋 | 동적 5 · 6, 정적 ① · ⑦, 정정 표시 | 동적 5 · 6 **정적으로 좁힘** (첫 win32k 자리 · gdi32full 같이 올라옴), 정적 ① "Q163 ① ② 와 같은 결정", ⑦ **일부 해결** (`NtQueryInformationProcess 0x34`), 정정 표시 → **(정정됨, run86)** |
| 4장 run86 새 줄 셋 | — | 동적 (**Q162**, Q104 엔 안 붙임) · 정적 (**Q163** ① ~ ⑧) · PE_emulator · 보탠 것 · 정정 |
| 5장 | … run85 | + run86 (표 둘) |

- **흐름도 그림은 안 바뀌었다.** test.exe 는 user32 · gdi32 를 안 올린다.

태그 · 판단이 바뀐 곳:

- "user32 `DllMain` 안 무조건 부르는 첫 win32k syscall 후보 = `GdiDllInitialize` 쪽" (run85, 후보) → **gdi32full `DllMain +0x2e NtGdiInit2` (`0x12ee`)** `[정적]` (매니페스트 `uiAccess` 가 없고 `gfServerProcess` 가 아닐 때).
- "`NtGdiInit` (`0x12ed`) 후보" (sast/82 짐작) → **틀림**, 아무도 안 부름 `[정적]`.
- "`GdiDllInitialize` 두 번" (sast/82 짐작) → **틀림**, 두 번은 `GdiProcessSetup` `[정적]`.
- "gdi32full 은 `+0x996` 에서 처음 로드될 수 있음" (sast/82 동적 질문) → **맞지 않음**, gdi32 와 같이 올라옴 `[정적: PE 표]`.
- "gdi32 는 user32 `DllMain` 전에 `[추측: 의존하는 쪽이 먼저]`" (run85 ⑥) → 매핑은 `[정적: PE 표]`, `DllMain` 차례는 여전히 모름 (동적).
- user32 차례 12 `GetProcessMitigationPolicy` 의 syscall `[추측: NtQueryInformationProcess]` (run85) → `NtQueryInformationProcess` 클래스 `0x34` `[objdump]`.
- run63 `ProtectDelayLoad 0x8000` "누가 읽나" (몰랐음) → `LdrResolveDelayLoadedAPI +0x17f` `[objdump]`.
- 짐 지도 7.2 PEB `GdiSharedHandleTable` "끝까지 0" → 그대로. **읽는 줄 · 쓰는 곳** 근거만 붙음.

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로** (마흔둘째판). test.exe 는 gdi32 를 안 올려서, gdi32 칸은 모두 "gdi32 를 올릴 때만" 이다.
- 근거만 붙인 곳:
  - 7.2 `_PEB` `끝까지 0` 의 `GdiSharedHandleTable` (`+0xf8`) — 읽는 줄 (gdi32full `GdiProcessSetup`) · 쓰는 곳 (`win32kbase!GdiProcessCallout +0x22b`). 놓는 법은 짐 지도 말로 "다른 모듈 (win32k) 이 `NtGdiInit2` syscall 안에서 직접" 이 될 것.
  - 3.3 TEB "범위 밖 길 — 다른 DLL 39" 줄 — gdi32 · gdi32full `DllMain` 줄기엔 TEB `Gdi*` 쓰기가 없다 (짐작 그대로). `Win32ThreadInfo` · `Win32ClientInfo +0x810/+0x818` 는 user32 `GetAppCompatFlags2` 가 읽는다.
- **결정이 남았다** (오케스트레이터 · 사용자): PEB `GdiSharedHandleTable` 을 "범위 밖 길 — 다른 모듈 (win32k)" 로 셀지. run85 의 PEB user32 칸 (짐 지도 7.2 메모는 넷) 과 같은 결정이다.

### 큰 그림에서

- **요청 28 이 닫혔다.** user32 를 import 만 한 프로세스가 진입점까지 가려면 엔진이 맞출 것이 **syscall 다섯 자리 + 데이터 둘** 로 정리됐다.
- **은퇴 앞 전원 켜기 셋이 한 줄로 이어졌다**:
  - kernelbase 의 첫 전화 (run68) → 전화선 손잡이 `CsrPortHandle`.
  - user32 의 둘째 전화 (run85) → 공용 안내 묶음 (`psi`). 손잡이가 있어야 한 통화로 끝난다.
  - user32 가 뒷문을 두드림 → gdi32full 의 첫 그래픽 신청 (이번) → 접수 번호 + 진열장 (PEB `+0xf8`).
  - 그러니 PE_emulator 는 **앞에서부터** 하나씩 풀면 된다 — 손잡이 → 3번 창구 답 → 접수 번호 · 진열장.
- **로더 쪽 지도에도 한 조각** — run63 의 `0x8000` 도장이 "누가 찍나" 에서 "누가 읽나" (지연 가져오기 풀기) 까지 이어졌다. test.exe 은퇴 앞 길에 지연 가져오기 풀기가 있는지는 이번에 안 봤다 (flow.md 2.4 run63 절).
- **다음**: 대기열은 **Q151a** (요청 29 첫 조각). 요청 28 의 남은 것은 Q162 (동적) · Q163 (정적) 에 줄 서 있다.

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 남은 것은 네 무리다.

- **실행해 봐야 아는 값** → **Q162** (결과서 "동적분석으로 확인해야 할 것" 번호 그대로, run85 와 같은 행).
  - **test.exe 로는 볼 게 없다** — 그래서 Q104 에 붙인 건 없다. 무엇으로 볼지 (notepad 류 / 새 GUI 샘플) 는 사용자 결정.
  - 9번 (레지스트리 값) 만은 프로세스 없이 VM 에서 `reg query` 로 볼 수 있다.
- **엔진 쪽** (PE_emulator) — 지금 `NtGdiInit2` 에 무엇을 돌려주나, PEB `+0xf8` 를 채우나 `[공백]`.
- **정적** → **Q163** ① ~ ⑧.
  - ① ② (win32k 안쪽) 은 원문 "win32k 커널 안쪽 깊이 — 이번에 넣지 않는 것" 의 예외라 **PE_emulator · 사용자 결정** (Q161 ① 과 같은 결정).
  - ④ ⑤ 는 원문 "이번에 넣지 않는 것" 이라 PE_emulator 가 새 요청으로 낼 때.
- **짐 지도 결정** → 오케스트레이터 · 사용자 (3절).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `DllMain` 차례 (gdi32full `entry` · gdi32 `Gdi32DllInitialize` · user32), `entry` 이유 1 이 정말 `+0x2a` 로 건너뛰나 | 순환이라 로더가 실행 때 정함 (2.5) | **Q162 1** — 각 입구 · `ntdll!LdrpCallInitRoutine` |
| `NtGdiInit2` 가 돌려주는 값, 그 앞뒤 PEB `+0xf8`, 그 사이에 `ClientThreadSetup` 이 서나, `+0xf8` 를 쓰는 게 정말 `GdiProcessCallout+0x22b` 인가 | 커널 쪽은 이름만, 꼴 검색은 하한 (2.9 · 2.10) | **Q162 2** — `gdi32full!DllMain+0x2e` / `+0x35`, 커널 `ba w8 @$peb+0xf8` |
| 진열장 실제 내용 · 크기, `+0x180140` · `+0x180148` 이 0 아닌가 | win32k 가 채우는 값 | **Q162 3** — `dd poi(@$peb+f8)+180094 L1` · `dq poi(@$peb+f8)+1800b0 L16` · `!address` |
| `GdiProcessSetup` 첫 호출자 (콜백 쪽 / `DllMain` 쪽), 둘째가 `+0x428` 로 바로 가나 | 콜백 때가 실행 순서에 달림 (2.11) | **Q162 4** |
| `GetAppCompatFlags2` 때 TEB `+0x78` 이 0 인가 → `0x1000` 이 나가나 | 실행 때 값 | **Q162 5** |
| `K32GetModuleBaseNameW` 안 `NtReadVirtualMemory` 가 정말 5번인가, 이름이 exe 이름인가 | `objdump` 로만 읽음 | **Q162 6** |
| 지연 풀기 `NtProtectVirtualMemory` ×2 가 두 이름에서 각각 서나, 칸 `gdi32+0x280e0` · `+0x27b80` 이 풀린 뒤 gdi32full 주소인가 | `objdump` 로만, 실행 순서 | **Q162 7** |
| `NtQueryInformationProcess(0x34)` 실제 답 | 실행 때 값 | **Q162 8** |
| `GRE_Initialize\DisableMetaFiles` 가 VM 에 있나 | 실행 환경 값 | **Q162 9** (VM `reg query`) |
| `+0x180094` 가 켜져 있나 → 언어팩 길이 도나 | win32k 가 채우는 값 | **Q162 10** |
| CFG 간접 호출 (`gdi32!Gdi32DllInitialize+0x9b`) 이 통과하나, `GetProcAddress` 가 내보내기 억제를 푸는 `NtSetInformationVirtualMemory` 가 따로 보이나 | 안 봄 `[추측]` | **Q162 11** |
| A6 ~ A13 syscall 순서 한 번에 | 실행 순서 | **Q162 12** — 그 프로세스 조건 syscall 추적 |
| 엔진이 지금 `NtGdiInit2` 에 무엇을 주나, PEB `+0xf8` 를 채우나 | PE_emulator 쪽 사정 `[공백]` | **Q162 엔진 쪽** |
| ① `win32kbase!GdiProcessCallout` 과 진열장 칸 (`+0x180094` · `+0x1800b0` 표 · `+0x180000`) 을 채우는 곳 — 칸 이름 · 뜻 | PDB 타입 없음, win32k 안쪽 (원문 "넣지 않는 것" 의 예외) | **Q163 ①** — PE_emulator · 사용자 결정 |
| ② win32kfull `NtGdiInit2` 쪽 — 돌려주는 값의 정체 · GUI 변환 순서 · 콜백 때 | 같음 | **Q163 ②** — 같은 결정 |
| ③ ntdll 지연 가져오기 길을 `.asm` 으로 | ntdll 프로젝트가 쓰이는 중이라 `objdump` 만 | **Q163 ③** (flow.md 2.4 run63 `0x8000` · run82 "첫 UEF 에서 delay-load" 와 같이 닫힘) |
| ④ 창 만들기 · ⑤ GDI 그리기 (진열장 핸들 표 항목 · `+0x180000` · TEB `GdiTebBatch` · 첫 `NtGdi*`) | 원문 "이번에 넣지 않는 것" | **Q163 ④ ⑤** — PE_emulator 새 요청 때 (④ 는 Q161 ③ ④ 와 겹침) |
| ⑥ `LpkInitialize` · `InitializeLpkHooks` (언어팩 켜짐 갈래) | 최소 길엔 없음 | **Q163 ⑥** (Q161 ⑤ 와 같이) |
| ⑦ `msvcp_win.dll` 시작 함수 (gdi32full 이 정적으로 가져옴) | 안 봄 | **Q163 ⑦** |
| ⑧ 작은 것: `RtlCreateHeap` syscall 횟수 (A7), `K32GetModuleBaseNameW` 여섯 자리 짝, CFG 내보내기 억제 갈래, 로더가 DLL 봉인 스티커를 먼저 바꾸나 (A8) | 이번 범위 밖 | **Q163 ⑧** |
| 짐 지도: PEB `GdiSharedHandleTable` 을 "범위 밖 길 — 다른 모듈 (win32k)" 로 셀지 | 세는 기준을 정하는 일 | 오케스트레이터 · 사용자 결정 |

---

## 5. 더 읽기

- run 문서: [`run86-gdi32-init.md`](run86-gdi32-init.md) — 맨 위 한 줄 결론, "PE_emulator 쪽에서 쓸 것" (요청 28 전체 결론 · 원문 질문이 어디서 답해졌나 · 질문마다 표), **표 A (syscall 답, A0 ~ A15)**, **표 B (데이터 칸)**, 다음 요청 후보 1 ~ 7, 호출 지점 표, 호출 관계 그림, sast/82 "넘길 것" 답, 확인된 것, 결과서와 다르게 적은 곳 · 보탠 것 ① ~ ⑨, 정정, 아직 확인 필요 (동적 1 ~ 12 · 정적 ① ~ ⑧ · 짐 지도 결정)
- 결과서: [`_workspace/sast/83_gdi32-init_result.md`](../../_workspace/sast/83_gdi32-init_result.md) — 먼저 읽을 여섯 줄, 질문 1 (gdi32 현관), 질문 2 (gdi32full · 지연 가져오기 길), 질문 3 (표 A · B), 질문 4 (다음 요청 후보), 동적으로 확인할 것 (머리의 "Q104 에 붙임" 은 run 문서대로 Q162 로 고쳐 읽기)
- 요청서: [`_workspace/sast/83_gdi32-init.md`](../../_workspace/sast/83_gdi32-init.md)
- flow.md: [3장 user32 절 아래 새 하위 절 "gdi32 · gdi32full 시작 함수와 '창 안 만드는 프로세스' 최소 조건"](flow.md) · 3장 user32 절 GUI 스레드 글머리 · 2.4 run63 CFG 절 (`0x8000` 읽는 쪽) · 기준 (gdi32 · gdi32full · win32kbase PDB) · 4장 run86 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 7.2 `_PEB` `끝까지 0` (`GdiSharedHandleTable`) · 3.3 TEB "범위 밖 길 — 다른 DLL 39"
- win32u 전체 번호표: `ms/disassemble/win32u/_syscalls.tsv` (커밋 안 함)
- 앞 이야기:
  - [run85 해설](run85-user32-init-해설.md) — 짝 바퀴 (user32 쪽). 2.4 둘째 전화 · 2.5 `psi` · 2.6 `0xc000003a` · 2.7 FALSE 11 · 2.12 GUI 스레드 · 콜백. "run86 에서 다룸" 자리는 이 글 2.13 으로 고쳐 읽기.
  - [run68 해설](run68-dll-init-rest-해설.md) 2.4 — 첫 전화 (`CsrPortHandle`).
  - [run63 해설](run63-cfg-func-table-해설.md) 2.8 — `ProtectDelayLoad` `0x8000` 도장 (이번 2.6 의 앞).
  - [run40 해설](../02_sprint/run40-dll-crt-entry-해설.md) — 정문 경비 · 접수처 · 준비 담당 · 손님 (이번 2.7 과 같은 꼴).
  - [run59 해설](run59-init-leftovers-해설.md) 2.7 — `RtlEncodePointer` 캐시 (이번 2.11).
  - [run56 해설](run56-syscall-args-해설.md) 2.9 — win32k 번호부 · `KiConvertToGuiThread`.
- 같이 한 일 (이 해설을 쓰며):
  - [run85 해설](run85-user32-init-해설.md) 맨 위에 "고쳐 읽기 (run86)" 한 줄 — "run86 에서 다룸" 자리와 "gdi32 먼저" 짐작을 이 글 2.13 · 2.5 로 가리킨다.
  - [run68 해설](run68-dll-init-rest-해설.md) 맨 위에 "고쳐 읽기 (run85)" 한 줄 — 2.4 끝 "덤" 은 첫 연결 이야기라는 것을 run85 해설 2.4 로 가리킨다.
