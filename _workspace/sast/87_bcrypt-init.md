# 87 bcrypt-init — 정적 분석 요청

- 날짜: 2026-11-13
- 스프린트: 03_sprint
- 대기열: **Q169** (PE_emulator 요청 31 — bcryptprimitives.dll `DllMain` attach 가 바깥에서 꼭 받아야 하는 답). 사용자 결정 2026-11-13: **지금 먼저** (Q104 VM 단계 앞 멈춤 자리에서 끼움), 끝나면 다시 Q104 앞에서 멈춤. 정적만 — VM 몫은 결과서 "동적으로 확인할 것" 으로만 적는다.
- 원문: `../PE_emulator/_workspace/05_tuning_planner/215_internals-bcryptprimitives.md` (읽어도 됨). 배경 표: `../PE_emulator/_workspace/05_tuning_planner/215_tune01.md` "T6 bcryptPrimitives" 절 (54행 근처).
- 빌드: 26100.9457. **bcryptprimitives.dll 은 아직 analyze 안 됨** → `ms/bin/.../System32/bcryptprimitives.dll` analyze (PDB 포함, GUID 를 결과서에 적기). ntdll 은 analyze 됨.
- 관련: run51·run62 (ntdll `EtwEventRegister` → `NtTraceControl` 길), run61 (`RtlRandom`·난수 씨앗 — 같은 "난수" 주제지만 ntdll 쪽).

## 배경

- PE_emulator 샘플 1183 개 (r01 병목의 14 %) 가 같은 자리에서 막힌다: bcryptprimitives 를 KnownDll 섹션으로 올린 뒤 `DllMain` (PROCESS_ATTACH) 이 FALSE → 로더가 내림 → `LoadLibraryExW` 0 (또는 초기화 중이면 `0xc0000142`).
- 에뮬레이터 로그 (대표 `r01 0087f228aca9 seq 205 ~ 237`, `r01 313eae524412 seq 193 ~ 199`) 에서 attach 안 syscall 차례 `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]`:
  1. `NtQueryInformationProcess(-1, 0x56, …, 길이 0xb0)` — 에뮬레이터 기본 대답
  2. `NtTraceControl` × 3
  3. **`NtOpenFile` × 5** — 접근 `0x100001` · 공유 `7` · 옵션 `0x20`. 에뮬레이터는 경로 앞글자를 몰라서 **status 0 을 돌려주지만 핸들 칸은 안 씀**. 열려는 이름은 로그에 없음.
  4. `NtAllocateVirtualMemory` → `NtClose` × 2 (대표에 따라 `NtUnmapViewOfSection`) → `DllMain` FALSE
- 에뮬레이터 이름 공간엔 `\Device\CNG`·`\Device\KsecDD` 가 이미 있음 (열기 몸만 없음).
- `NtDeviceIoControlFile` 은 로그에 안 나옴 — 열기가 "성공 + 핸들 없음" 이라 그 앞에서 갈렸을 수 있음 `[추측]`.

## 알고 싶은 것

1. **attach 길과 `NtOpenFile`**: `DllMain` (진입점 → CRT 래퍼가 있으면 그 안의 진짜 `DllMain`) PROCESS_ATTACH 가 부르는 함수 차례 (오프셋 + ret), syscall 까지. 그중
   - `NtOpenFile` 로 여는 **객체 이름** (`.rdata` 문자열 — 장치 경로), 접근·공유·옵션 값이 로그 (`0x100001`·`7`·`0x20`) 와 같은지.
   - **5 번 되풀이하는 조건** — 무엇을 보고 다시 여나 (status? 돌려받은 핸들 값? 다른 이름 5 개?).
   - **status 0 인데 핸들 칸이 안 쓰인 경우** (0 이나 처음 값 그대로) 코드가 어디로 가나.
   - **FALSE 로 가는 자리 전부** (오프셋).
2. **연 뒤 부르는 syscall 차례**: `NtDeviceIoControlFile` 이면 IOCTL 코드 · 입력/출력 버퍼 크기 · 출력에서 읽는 칸 (오프셋·크기). **TRUE 가 되려면 답이 무엇이어야 하나** — 성공 status 만으로 되나, 출력 값 모양 (길이·0 아님 등) 까지 보나. 열기 뒤 `NtAllocateVirtualMemory`·`NtClose` × 2 가 무엇인지 (무엇을 할당하고 무엇을 닫나).
3. **`NtQueryInformationProcess` class `0x56`**: PDB 의 `PROCESSINFOCLASS` 이름, 길이 `0xb0` 이 맞는지, attach 가 그 결과를 쓰는지 (쓰면 어느 칸을 보고 어디로 가나, 실패·0 이면 어디로 가나).
4. (작게) **`NtTraceControl` × 3**: ETW 등록인지 (`EtwEventRegister`·TraceLogging 류 — 이름만), 그 실패가 attach 결과 (TRUE/FALSE) 에 닿는지만.
5. **최소 조건 표**: "attach 가 TRUE 로 끝나려면 바깥 (nt 엔진·팀원 syscall 몸) 이 줘야 하는 답" — syscall · 입력 (이름·IOCTL) · 기대 답 (status·핸들·출력 칸) · 안 맞으면 가는 자리.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| bcryptprimitives | 진입점 (`DllMain`·`_DllMainCRTStartup`·`DllMainCRTStartup` 류 — PDB 이름으로 확인) | 1 | 질문 1 — attach 갈래 |
| bcryptprimitives | attach 갈래에서 부르는 초기화 함수들 (난수·장치 열기 쪽) | 1 ~ 줄기 끝 | 질문 1·2·3 — `NtOpenFile`·`NtDeviceIoControlFile`·`NtQueryInformationProcess` 를 부르는 곳까지 |
| ntdll | (필요할 때만) `EtwEventRegister`·`EtwNotificationRegister` 이름 확인 — 이미 run51·run62 에 있으면 가리키기만 | 0 | 질문 4 |

- 범위: attach (PROCESS_ATTACH) 한 갈래만. DETACH·THREAD_ATTACH·export 된 암호 함수 본문 (`ProcessPrng` 등) 은 넣지 않는다 — 단, attach 가 그 함수를 직접 부르면 그 줄기는 따라간다.
- 공용 부품 (메모리 복사·락·문자열) 은 이름만.
- 함수가 많으면 (5 개 넘게 깊음) 질문 1·2 를 먼저, 3·4 는 이름·갈림 자리만.

## 지도에서 본 것

- 없음 — bcryptprimitives 는 처음. analyze 뒤 `run_ghidra.sh map` 으로 진입점 깊이·직접 호출을 보고 대상 함수를 골라도 된다.

## 이미 아는 것

- `[관찰 — 에뮬레이터 로그]` 위 "배경" 의 syscall 차례. 진짜 Windows 에서 본 것 아님.
- 덤프: `System32/bcryptprimitives.dll` 있음 (오케스트레이터 확인). 우리 repo (`ms/decomp`·`ms/disassemble`·`runs`·`_workspace`) 에 bcryptprimitives 분석 0.
- `[추측]` 여는 장치는 `\Device\CNG` 또는 `\Device\KsecDD` 쪽 (난수 씨앗을 커널에서 받는 길) — 코드로 맞는지 틀린지 볼 것.

## 받고 싶은 것

- 결과서 `_workspace/sast/87_bcrypt-init_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`. bcryptprimitives PDB GUID.
- 질문 1·2 는 차례 표 (함수+오프셋 · syscall · 인자 · 갈림).
- 질문 5 최소 조건 표.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (지금 우리 = 경로 모르면 status 0 + 핸들 안 씀, class 0x56 기본 대답).
- 끝에 **"동적으로 확인할 것"** (IOCTL 출력의 실제 값처럼 실행 때만 정해지는 것 — 칸만, `[추측]`) 과 **"다음 요청 후보"**.
