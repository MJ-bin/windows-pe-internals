# 144 com-rpcss — 정적 분석 요청

- 날짜: 2026-11-26
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q257a** (Q257 의 ① combase `Connect` 만 앞으로 — Q257 을 그 자리에서 Q257a / Q257b 로 나눔. ② 창 스테이션 · 데스크톱 커널 본문 · ③ `GetConnection +0xc5` `E_ACCESSDENIED` 는 Q257b 로 대기). PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/367_internals-com-rpcss.md` — **원문을 먼저 끝까지 읽을 것** ("이미 아는 것" 에 run 근거 · objdump 파일 · 로그 수). 사용자 결정 2026-11-26 "이것도 부탁해" (365 둘 다음). **정적만.**
- **먼저 읽는 것**: `_workspace/sast/134_winsta-name_result.md` (run136 — `CoInitializeSecurity +0x47d GetConnection` → 람다 `operator()` rva `0x18b9c0` → `+0x82e Connect`), `_workspace/sast/130_lrpc-wire_result.md` (run133 — LRPC 연결 메시지 · bind `0x70` B 칸 · 문법 마스크 `+0x48` · 보안 문맥 줄), `_workspace/sast/132_alpc-completion_result.md` (run137 — MIDL 스텁 칸 읽는 법 · 프로시저 8 표 꼴).
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `combase.dll` — **이미 analyze 됨**. 다시 analyze 하지 말고 `^이름$` export 만 (`Connect` rva `0x12f318` · 람다 `operator()` rva `0x18b9c0` — 이름 꼴이 길면 `pdb_aliases.py --where combase.dll` 로 찾고, 긴 이름이라 export 가 안 되면 `_oneoff` objdump 처럼 rva 로 뽑고 그 방법을 결과서 "스크립트 문제" 에 적기) · `RetryRPC` · `CheckStatus` · `GetConnection` (rva `0xa14d0`) refs · MIDL 클라이언트 인터페이스 구조체 · 형식 문자열 (PE 데이터).
  - `rpcss.dll` — **아직 analyze 안 됨**. PDB `fetch_pdb.py` (GUID 확인). **필요한 만큼만** — 같은 인터페이스의 서버 쪽 프로시저 표 · 첫 프로시저 몸에서 요청 · 응답 꼴을 정하는 검사. 원문 1 의 "이 포트의 이 인터페이스를 받는 모듈" 확인.
  - `rpcrt4.dll` — 이미 analyze 됨. **지금 sast/143 이 rpcrt4 프로젝트를 쓰므로 Ghidra 로 열지 말 것** — 있는 asm 읽기만. 더 필요하면 결과서에 "다음 요청으로".
- 주의: 같은 시간에 다른 decompiler 가 **user32 · win32kfull · win32kbase** (sast/142) · **RpcEpMap · rpcrt4** (sast/143) · (곧) **gdi32 · gdi32full · gdiplus** (sast/145) 를 돈다 — 그쪽은 건드리지 않는다. rpcss analyze 전에 `free -g` (넉넉하지 않으면 combase 몫부터 하고 기다림). scratchpad 임시 파일은 `s144/` 하위에.

## 왜 (요청한 쪽 말 요약)

- 악성 566 샘플이 `\RPC Control\epmapper` 에 **동기 연결** 뒤 바로 인터페이스 `e60c73e6-…` (`ILocalObjectExporter` `[추측]`) 로 bind → 에뮬레이터가 fault `0x6b5` → `CoInitializeSecurity` = `0x800706b5`. 예약 작업 · 프로세스 죽이기 · WMI 묻기가 그 바로 앞에서 끝남.
- 에뮬레이터 정책: 서비스 흉내는 **꼴만 맞춘 최소 대답 · 값 지어내지 않음 · 모르는 부탁은 정직한 실패**. bind 를 받아 주면 첫 프로시저가 오는데, 그 응답 꼴과 combase 검사를 모름. 틀린 성공 응답은 지금보다 나쁨 → 알고 싶은 것은 rpcss 본문이 아니라 **combase 가 무엇을 보내고 응답에서 무엇을 읽고 검사하나**.

## 알고 싶은 것 (원문 번호 그대로)

1. **bind — 무엇을 내미나** (람다 `+0x33c CreateFromBindingString` ~ `Connect` 입구)
   - 바인딩 글 (프로토콜 · 끝점). **`ept_map` 을 안 묻고 `\RPC Control\epmapper` 로 곧장 가는 것이 코드로 맞나** (끝점이 바인딩 글에 박혀 있나) — 예/아니오 + 줄.
   - 클라이언트 인터페이스 구조체: UUID 전체 · 주 / 부 버전 · 내미는 전송 문법 (NDR 2.0 / NDR64 — run133 마스크 칸으로).
   - `CreateFromBindingHandle` 셋 · `CreateWellKnownSid` 가 거는 보안 가운데 **연결 · bind 메시지 꼴을 바꾸는 것만** (요구 서버 SID · 인증 / 가장 수준 — run133 1-1 · 2-2 에 맞춰).
   - 진짜에서 이 인터페이스를 받는 모듈 (rpcss.dll 인가 · `epmapper` 끝점을 RpcEpMap 과 나눠 쓰나). 이름만. sast/143 결과서가 이미 있으면 가리키기만.
2. **첫 프로시저 — 요청 · 응답 · combase 가 읽는 칸** (`Connect` rva `0x12f318`)
   - 프로시저 번호 · 스텁 꼴: 요청 인자 차례 · 타입 (창 스테이션 글 · exe 경로 · 그 밖), 응답 out 인자 차례 · 타입. MIDL 형식 문자열 / 프로시저 표로 (run137 3-1 꼴).
   - **응답에서 combase 가 검사하는 것** — 0 이면 안 되는 칸 · 범위 · 서로 맞아야 하는 칸 · NULL 을 받는 포인터. 받은 값이 들어가는 전역 (`gGotSecurityData` 를 누가 언제 켜나) 과 그 전역을 뒤에서 읽는 쪽 (이름만).
   - out 값마다 가름: (가) 상수 · 규칙 (나) 프로세스마다 다름 (다) 부팅마다 다른 난수 · 식별자 (라) 0 / NULL / 빈 배열이어도 지나감.
   - 실패 쪽: fault `0x6b5` 를 받으면 `+0x884 RetryRPC` · `+0x8aa CheckStatus` 가 무엇을 하나. **다시 걸지 않고 깨끗이 끝나는 "서버가 못 함" 상태** 와 **끝없이 다시 거는 상태 (금지 꼴)** 를 줄로.
3. **그다음 — `Connect` 성공 뒤 같은 포트로 가는 것** (지도만 — 이름 · 번호 · 줄. 본문은 다음 요청)
   - `CoInitializeSecurity` 나머지 (`+0x486` 뒤 · 람다 `+0x8aa` 뒤) 와 `CoInitializeEx` 가 이 포트로 더 부르는 프로시저.
   - 첫 COM 사용 때의 **첫** 부름: (a) in-proc `CoCreateInstance` (b) `CoGetObject` (c) out-of-proc · 서비스. 서버가 "그 클래스 · 서비스 없음" 일 때 클라이언트가 받는 HRESULT 길 (이름 · 상수만).
   - `GetConnection` (rva `0xa14d0`) 을 부르는 곳 목록 (`refs`).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| combase | 람다 `operator()` (rva `0x18b9c0` — `+0x2b0 ~ +0x9b3` 조건 · 인자) · `Connect` (`0x12f318`) · `RetryRPC` · `CheckStatus` · MIDL 인터페이스 구조체 · 형식 문자열 | 1 | 1 · 2 |
| combase | `GetConnection` refs · `CoInitializeSecurity` `+0x486` 뒤 · `CoInitializeEx` 둘레에서 같은 인터페이스를 부르는 곳 | 0 | 3 |
| rpcss | 같은 인터페이스 서버 프로시저 표 · 첫 프로시저 입구 검사 (필요한 만큼) | 0~1 | 1 · 2 |

- 범위: 보내는 것 · 받은 뒤 검사 · 실패 처리까지. rpcss 본문 (OXID 해결 · 활성화) 은 이름만. Q257b (창 스테이션 커널 본문 · `E_ACCESSDENIED`) 는 안 함.
- 분량이 크면 **2 → 1 → 3** 이 아니라 원문 뜻대로 **1 → 2 먼저, 3 은 "다음 요청으로"** 라고 적어도 된다.

## 받고 싶은 것

- 결과서 `_workspace/sast/144_com-rpcss_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm` (또는 `_oneoff` objdump — 근거 줄에 어느 쪽인지). 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (1 · 2 · 3 의 작은 물음마다 한 줄).
  3. 표: (1) bind 표 — `칸 · 값 · 출처 줄` (UUID · 버전 · 문법 · 바인딩 글 · 보안) · (2) **첫 프로시저 응답 칸 표** — `칸 · 타입 · 가름 (가)~(라) · combase 가 읽는 줄 · 0 가능 여부` · (3) 실패 처리 표 — `받은 것 · RetryRPC / CheckStatus 가 하는 일 · 다시 거나 · 돌려주는 HRESULT · 줄` · (4) 그다음 지도.
  4. PDB GUID 확인 줄 (combase 기존 `DA1DC0C4F35CEE58F672CBB20460D11D1` 과 같은지 · rpcss 새로).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** — `ext/syscall/syscall_layer_cpp/src/rpc/lrpc.cpp:77` · `epm.cpp:33 ~ :44` (창구가 아는 인터페이스는 EPM 하나, 그 밖 bind 는 fault `0x6b5`) 에 대해: **stub 이 돌려줄 최소 대답** (bind 답 문법 · 첫 프로시저 응답 칸) · **금지 꼴** · "그다음 벽" 한 줄.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (VM 묶음 — `combase+0x18c1ee` (`operator() +0x82e`) 앞뒤 요청 · 응답 스텁 등, 멈춤점 `combase!함수+0x…`) 과 **"다음 요청 후보"**.
