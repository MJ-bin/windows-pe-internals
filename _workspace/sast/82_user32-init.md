# 82 user32-init — 정적 분석 요청

- 날짜: 2026-11-13
- 스프린트: 03_sprint
- 대기열: **Q150a** (PE_emulator 요청 28 의 user32 쪽 — GUI 벽의 최소 길 지도). 사용자 결정 2026-11-12: 요청 26 ~ 30 맨 앞, 28 은 3순위, **첫 답은 "최소 길 지도" 수준**. 더 깊은 것은 끝에 "다음 요청 후보" 로. 정적만 — VM 몫은 Q104 출처로. gdi32·gdi32full·최소 조건 표 (요청 28 의 4·5) 는 다음 바퀴 Q150b — **이번엔 안 본다**.
- 원문: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-PE-emulator/d6bd3369-b666-4505-9231-47b12d2704dd/scratchpad/internals_requests/request28_user32_init.md` (읽어도 됨).
- 빌드: 26100.9457. **user32.dll (1.9MB)·win32u.dll (170KB) 은 아직 analyze 안 됨** → 먼저 analyze (PDB 받기 포함), PDB GUID 를 결과서에. ntdll 은 이미 있음 (다른 decompiler 가 kernelbase·ucrtbase·condrv 를 쓰는 중이니 **kernelbase 프로젝트는 열지 않는다**).
- 관련: run68 (sast/63 — ntdll `CsrClientConnectToServer` (`0xdb7a0`) 클라이언트 길: kernelbase 가 `\Sessions\1\Windows`·서버 번호 1 (BASESRV)·연결 정보 8B 로 부름, 안에서 `CsrpConnectToServer` (`0xdbfe4`) 가 `CsrPortHandle`·PEB `+0x380`·`+0x88`·`+0x98` (게시판 `SharedSection` 뷰) 을 채움), run71 (csrsrv `CsrSrvCreateSharedSection +0x1bf NtCreateSection (\Sessions\N\Windows\SharedSection)` — 게시판 원본)

## 배경

- PE_emulator 첫 실험 회차에서 GUI DLL 을 직접 import 하는 샘플 4,655 개를 뺐다 (user32 를 import 만 해도 초기화에서 막힘). 콘솔 프로그램도 user32 를 import 하는 게 많다.
- 경계: 은퇴 전 DLL 의 `DllMain` 은 ntdll 엔진이 진짜로 부르고, syscall (ntdll 것·win32u 것) 은 nt 엔진·팀원 몸이 대답. csrss·win32k 는 없다. → **user32 `DllMain` 이 TRUE 를 돌려주려면 어떤 syscall 에 어떤 답, 어떤 공유 데이터가 있어야 하나.**
- 지금 우리 (PE_emulator, 덤프 notepad 흉내): user32 `DllMain` → `NtOpenSection("\Sessions\1\Windows\SharedSection")` 이 `0xc000003a` (팀원 몸) → `0xc0000142` (DLL 초기화 실패) `[관찰 — 흉내]`. win32u syscall (`0x1000` 대) 은 이름이 안 붙음 `[추측]`.

## 알고 싶은 것

1. **user32 `DllMain` (PROCESS_ATTACH) 의 줄기**: 진입점 (`_DllMainCRTStartup`?) → `UserClientDllInitialize` (이름은 PDB 로) 가 부르는 함수·syscall 을 **차례대로** (오프셋 + ret):
   - (a) ntdll `CsrClientConnectToServer` 를 부르나 — 인자 (`\Sessions\N\Windows`·서버 번호 (USERSRV = 3?)·연결 정보 모양·크기). run68 의 BASESRV 연결과 무엇이 다른가 (이미 연결돼 있으면 `CsrpConnectToServer` 를 다시 안 타나).
   - (b) **`NtOpenSection("\Sessions\N\Windows\SharedSection")` 을 부르는 자리** — user32 안인가, ntdll `CsrpConnectToServer` 안인가 (PE_emulator 가 본 실패 자리). 그 뒤 `NtMapViewOfSection` 인자 (크기·보호).
   - (c) win32u 쪽 syscall (`NtUserProcessConnect`·`NtUserInitializeClientPfnArrays`·그 밖 — 실제 이름·차례는 asm 으로): 각각 인자 모양과 **어떤 답이어야 다음으로 가나**, 실패면 어디서 FALSE 인가.
   - (d) `DllMain` 이 FALSE 를 돌려주는 자리 목록 (조건 · 줄).
2. **공유 데이터 최소 칸**: `gSharedInfo`·`gpsi` (·`gHandleTable`·`gpsiDisplay` 류) 를 어디서 받나 (`NtUserProcessConnect` 의 출력 구조체? 공유 섹션 뷰의 오프셋?), `DllMain` 과 "창을 안 만드는 프로세스" 가 **읽는 칸만** — (구조체 · 오프셋 · 크기 · 읽는 줄 · 뜻). PDB 에 타입이 있으면 이름으로.
3. **GUI 스레드로 바뀌는 때**: TEB `Win32ThreadInfo` 를 채우는 것이 `DllMain` 안인가, 첫 win32k syscall 때 (커널 `PsConvertToGuiThread` — 이름만) 인가. user32 가 `DllMain` 안에서 win32k syscall 을 부르면 그때 바뀐다는 뜻인지 — 줄로.
4. **win32u syscall 번호표**: win32u.dll 스텁에서 이번 길에 나온 syscall 들의 번호 (`MOV EAX, 0x10xx`) 와 이름 — PE_emulator 가 이름표를 붙일 수 있게. (전체 표가 싸게 뽑히면 전체도 — 별도 파일 `ms/` 아래, 결과서엔 이번 길 것만.)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| user32 | 진입점·`UserClientDllInitialize` (이름은 `_index.tsv` 로) | 2 | 질문 1 — 줄기만, 창·메시지 쪽은 이름만 |
| user32 | 질문 2 의 전역 (`gSharedInfo`·`gpsi`) 을 쓰는 초기화 함수 | 0 | 질문 2 — `refs` 로 쓰는 곳 |
| ntdll | `^(CsrClientConnectToServer|CsrpConnectToServer)$` | 0 | 질문 1(a)(b) — 이미 있음, 두 번째 연결 갈래·`NtOpenSection` 자리만 |
| win32u | 질문 1(c) 에 나온 `NtUser*` 스텁 | 0 | 질문 4 — 번호 |

- 지도: `run_ghidra.sh map user32.dll <DllMain 진입점>`.
- 창 만들기·메시지 루프·그리기·win32k 커널 안쪽은 넣지 않는다 (이름만).
- user32 가 gdi32 를 부르면 이름만 적고 Q150b 로 넘긴다.

## 이미 아는 것

- `[정적]` run68: ntdll `CsrClientConnectToServer` 클라이언트 길 (kernelbase 가 BASESRV 로 부름), `CsrpConnectToServer` 가 PEB `+0x380`·`+0x88`·`+0x98` (게시판 뷰) 을 채움.
- `[정적]` run71: csrsrv `CsrSrvCreateSharedSection +0x1bf` 가 `\Sessions\N\Windows\SharedSection` 을 만듦 (게시판 원본).
- `[관찰 — PE_emulator 흉내]` user32 `DllMain` → `NtOpenSection(SharedSection)` `0xc000003a` → `0xc0000142`.

## 받고 싶은 것

- 결과서 `_workspace/sast/82_user32-init_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`. user32·win32u PDB GUID.
- **질문 1 은 syscall 차례 표** (차례 · 함수+오프셋 · syscall · 인자 · 다음으로 가려면 필요한 답 · 실패면).
- **질문 2 는 데이터 칸 표**.
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 Q150b 로 넘길 것 (gdi32 를 부르는 자리, 최소 조건 표 재료) 과 "다음 요청 후보" (더 깊이 볼 것).
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다.
