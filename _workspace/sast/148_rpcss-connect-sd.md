# 148 rpcss-connect-sd — 정적 분석 요청

- 날짜: 2026-11-27
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q288** (새, 380 ②) + **Q284a** (Q284 를 나눈 앞 몫 ① ~ ③). PE_emulator 요청 원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 의 **"② rpcss `_Connect` 응답의 SD 다섯 조각" 절 전부** (읽기만 — "이미 아는 것" 에 rva · 막힌 자리 · `[수집]` 레지스트리 값 크기가 다 있음). 사용자 결정 2026-11-27 (PE_emulator D179): 차례 ②. 원문 "**② 가 먼저, 이것만 와도 서 바퀴를 엶**". **정적만.**
- 먼저 읽는 것: `_workspace/sast/144_com-rpcss_result.md` 전부 (특히 `Connect` 칸 28 ~ 30 · 4-d `GetConnection` 부르는 곳 · 다음 요청 후보) · `runs/03_sprint/run149-com-rpcss.md` (2-2 칸 표 · "PE_emulator 쪽"). 대기열 Q284 줄.
- 대상 빌드: 26100.9457 x64 `System32/`:
  - `rpcss.dll` · `combase.dll` — 프로젝트 있음 (analyze 됨). 있는 asm 먼저 읽고, 없는 함수만 export. refs 는 `RefsTo.java` (전역 주소).
  - `ntdll.dll` — `RtlLengthSecurityDescriptor` (rva `0xe8f10`, `+0x2d` 가 죽는 줄) · 그 둘레 SD 검사 함수만.
  - `sechost.dll` (없으면 처음 analyze) · `advapi32.dll` (있음) — 물음 3 `\RPC Control\lsapolicylookup` 을 여는 함수만.
- 같은 시간에 도는 decompiler: sast/147 (ntdll32 · kernelbase32 · wow64 · csrsrv) · sast/149 (rpcrt4) · sast/152 (windows.storage · shell32 · appinfo · kernelbase) · 뒤에 sast/153 (ntoskrnl — **ntdll 은 이 요청 결과서가 나온 뒤에 연다**). **이 요청은 rpcss · combase · ntdll · sechost · advapi32 만.** rpcrt4 는 열지 않는다 (Q284 ⑤ `PickAssociation` 은 Q284b 로 남김). 새 analyze · export 전에 `free -g`. scratchpad 임시 파일은 `s148/` 하위.

## 알고 싶은 것

### 380 ② (원문 1 ~ 3)

1. **rpcss `gpNoReg*SD` 넷 (`0x1656e0` · `0x1656d8` · `0x1656c8` · `0x1656d0`) + `GetDefaultAccessRestrictions` (rva `0x617f4`)**: 어느 함수가 (서비스 시작 때인가) 무엇으로 짓나. 조각마다 (가) 코드 상수 · 고정 SID / SDDL (나) 레지스트리 값 그대로 (키 · 값 이름 — 원문의 `HKLM\SOFTWARE\Microsoft\Ole` `DefaultLaunchPermission` 124 B · `MachineAccessRestriction` 228 B · `MachineLaunchRestriction` 232 B 가 어느 조각인가, `DefaultAccessPermission` 이 없을 때 어느 기본으로 가나, `SOFTWARE\Policies\Microsoft\Windows NT\DCOM` 을 보나) (다) 기계 · 부팅마다 다른 SID. SD 꼴 (자기 상대 · 소유자 / 그룹 · ACE 수 · SID · 마스크 — 코드 상수면 바이트까지). 크기 0 조각이 진짜에서 나올 수 있나.
2. **combase 가 읽는 길**: 여섯 전역 (`0x3380e8` · `0x3380f8` · `0x3380f0` · `0x3380d0` · `0x3380c8` · `0x3380e0`) 의 refs 가운데 **`CoInitializeSecurity(SD NULL, −1, …)` · AppID 없음** 길이 `RtlLengthSecurityDescriptor` 에 닿는 사슬 (함수 · 줄). 그 길이 SD 에 하는 검사 · 실패 HRESULT · 넷 중 실제로 읽는 조각 (안 읽는 것은 0 이어도 되나). 암묵 길 (`InitRemoting +0xb6 InitializeSecurity`) 도 같은가. `GetSystemSecurityDescriptors +0xf` 본문 포함.
3. **`lsapolicylookup` 이 끼는가**: `Connect` 반환과 죽는 줄 사이에서 그 포트를 여는 함수 (SID ↔ 이름 찾기인가) · 실패 (`0xc0000034`) 면 그 길이 어떻게 끝나나 (SD 짓기에 쓰이나 · 지나가나 · HRESULT 가 바뀌나).

### Q284a (Q284 ① ~ ③ — `Connect` 가 서면 바로 다음 벽)

4. rpcss ILocalObjectExporter **프로시저 7 (OID 할당) → 6 (OXID 등록)** 몸 (입력 · 출력 칸 · 실패 status) 과 combase 가 그 답에 하는 검사 (부르는 줄 · 보는 칸 · 실패 HRESULT).
5. `LocalInterfaceOnlySecCallback` — 로컬 클라이언트를 거절하는 조건 (무엇을 보고 · 무엇을 돌려주나).
6. `MakeSCMProxy` · `DelegateActivationToSCM` — 활성화가 "클래스 없음" HRESULT 로 끝나는 길 (줄 · 값).

- 남은 Q284 ④ ~ ⑥ (프로시저 10 · 1 몸 · ole32 `CoGetObject` · rpcrt4 `PickAssociation`) 은 이번 범위 밖 (Q284b).

## 범위 · 차례

- 분량이 크면 **2 → 1 → 3 → 4 → 5 → 6** 차례로 결과서에 쓰고 나머지는 "다음 요청으로". 2 (죽는 사슬) 와 1 (SD 출처) 이 서 바퀴를 연다.

## 받고 싶은 것

- 결과서 `_workspace/sast/148_rpcss-connect-sd_result.md`. 근거 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋`, 이름은 PDB 에 있으면 `[정적] PDB`, 없으면 `[추측: 이름]`.
  1. 맨 위 **"먼저 읽는 한 줄 답"** (1 ~ 6 마다 한 줄).
  2. **SD 조각 표** (원문 "받을 꼴"): `조각 · 출처 (가)/(나)/(다) · 바이트 또는 레지스트리 값 이름 · combase 가 읽는 줄 · 0 가능 여부`.
  3. 죽는 사슬 표 (`Connect` 반환 → … → `RtlLengthSecurityDescriptor +0x2d`) + **금지 꼴** (접근 위반 · 실패 HRESULT 가 나는 응답 꼴).
  4. `lsapolicylookup` 한 줄.
  5. Q284a 표: 프로시저 7 · 6 입출력 칸 · combase 검사 / 거절 조건 / "클래스 없음" 길.
  6. PDB GUID 확인 줄.
- **"PE_emulator 쪽 — 지금 맞나·틀리나"**: 원문 "답이 오면 쓰는 곳" (`rpcss.cpp` 응답 칸 28 ~ 30 · `tests/alpc_frontend.cpp`) 에 쓸 값 — (가) 면 `[정적]` 바이트, (나) 면 어느 `[수집]` 값, "stub 이 채울 최소 목록" (run149 목록에 이어). 3 이 "낌" 이면 그렇다고 한 줄 (LSA1 이 앞 바퀴가 됨).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에:
  - **"VM 으로 확인할 것 — 디버거 필요"** (원문: 1 이 (다) 면 새 번호 없이 Q283 D5 · D8 에 "응답 끝 SD 덩어리 · 크기 다섯" 을 얹는 꼴로)
  - **"VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것"** (레지스트리 키 · 값 · 명령 출력 — 수집 스크립트 재료. 예: `HKLM\SOFTWARE\Microsoft\Ole` 전체 · DCOM 정책 키 · AppID 키 등 무엇이 필요한지)
  - **"다음 요청 후보"**
