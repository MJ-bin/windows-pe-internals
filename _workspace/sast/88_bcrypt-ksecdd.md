# 88 bcrypt-ksecdd — 정적 분석 요청

- 날짜: 2026-11-14
- 스프린트: 03_sprint
- 대기열: **Q172** (PE_emulator 요청 32 — bcrypt.dll `DllMain` attach 가 `\Device\KsecDD` 에서 꼭 받아야 하는 답). 사용자 결정 2026-11-14: **지금 먼저** (Q104 VM 단계 앞 멈춤 자리에서 끼움), 끝나면 다시 Q104 앞에서 멈춤. 정적만 — VM 몫은 결과서 "동적으로 확인할 것" 으로만 적는다.
- 원문: `../PE_emulator/_workspace/05_tuning_planner/226_internals-bcrypt-ksecdd.md` (읽어도 됨). 배경 표: `../PE_emulator/_workspace/05_tuning_planner/226_tune02.md` "T5" 절.
- 빌드: 26100.9457. **bcrypt.dll 은 아직 analyze 안 됨** → `ms/bin/.../System32/bcrypt.dll` analyze (PDB 포함, GUID 를 결과서에 적기). ntdll·bcryptprimitives 는 analyze 됨.
- 관련: **run91 / sast/87 (bcryptprimitives, `\Device\CNG`, IOCTL `0x390008`)** — 이번 것은 **다른 DLL** (bcrypt.dll, 이름 `\Device\KsecDD`, IOCTL `0x390400`). 같은 꼴 (status 만 보나·핸들 칸을 보나) 을 비교해 한 줄로 적어 주면 좋다. run91 의 `IsEnclave` (NtQIP class `0x56`) 꼴이 여기서도 나온다.

## 배경

- PE_emulator 병목 샘플 221 개 (악성 57 / 정상 164) 가 같은 자리에서 막힌다: bcrypt.dll `DllMain` (PROCESS_ATTACH) 이 FALSE → 로더가 `0xc0000142`. 같은 기본 대답을 받은 샘플은 531 개.
- 대표 자리: `r02 03930f8410bc seq 279 → 286 ~ 288 → 289 → 290 → 291 ~ 295`, `r02 5f91c53f270e seq 356 → 363 ~ 365 → 366 → 367`.
- attach 안 syscall 차례 `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]` (221/221 같은 꼴):
  1. `NtTraceControl` 여럿
  2. `NtQueryInformationProcess(-1, 0x56, …, 0xb0)` (run91 `IsEnclave` 꼴)
  3. `NtQueryInformationProcess(-1, 0x2b, …, 0x218)`
  4. `NtTraceControl` × 4
  5. `NtCreateSemaphore` × 2 (접근 `0x100003`, 처음 0, 한도 `0x7fffffff`)
  6. `NtCreateEvent` (접근 `0x1f0003`, 넷째 인자 1)
  7. **`NtOpenFile(\Device\KsecDD)`** — 핸들 칸 = **bcrypt.dll + `0x25ae0`** (전역), 접근 `0x100003` · 공유 `7` · 옵션 `0x20` · 속성 `0x0` · 루트 0. 에뮬레이터는 경로 앞글자를 몰라 **status 0 을 돌려주지만 핸들 칸은 안 씀** (run91 과 같은 틀린 답).
  8. **`NtDeviceIoControlFile(핸들 0, 이벤트 0, …, IoStatus, 코드 0x390400, 입력 버퍼, 입력 길이 0x68, 출력 버퍼, 출력 길이 8)`** → 핸들 0 이라 `0xc0000008`.
  9. `NtClose` × 5 → `DllMain` FALSE.
- 에뮬레이터 쪽 계획: T1 바퀴 (`\Device\CNG`, run91) 가 장치 열기를 만들면 핸들은 생긴다 → **남는 것이 IOCTL `0x390400` 의 답**. 그래서 질문 2 가 가장 중요하다.

## 알고 싶은 것

1. **`\Device\KsecDD` 를 여는 함수와 FALSE 자리**: attach 갈래에서 `NtOpenFile(\Device\KsecDD)` 까지 가는 함수 사슬 (함수+오프셋, ret). 그 함수가 보는 것이 무엇인가 — **`NtOpenFile` status / 핸들 칸 (`+0x25ae0`) 값 / `NtDeviceIoControlFile` status** 중 어느 것(들). FALSE 로 가는 자리 전부 (오프셋). 핸들 칸 `+0x25ae0` 이 PDB 이름이 있는 전역이면 그 이름.
2. **IOCTL `0x390400`**:
   - 입력 `0x68` 바이트에 무엇을 넣나 — 칸별 (오프셋·크기·값의 출처). 특히 **세마포어 둘·이벤트 핸들이 들어가나**, 프로세스 ID·함수 주소 같은 것이 들어가나.
   - 출력 8 바이트에서 **무엇을 읽어 어디에 쓰나** (전역 이름·오프셋).
   - **TRUE 가 되려면 답이 무엇이어야 하나** — 성공 status 만으로 되나 / 출력 값 모양까지 보나 (0 아님·특정 값) / 출력이 0 이어도 되나. `STATUS_PENDING` 이면 이벤트를 기다리나 (`NtWaitForSingleObject` 등).
   - IOCTL 실패 때 어디로 가나 (FALSE 인가, 그냥 넘어가나).
3. **TRUE 뒤 첫 사용 (이름만)**: attach 가 TRUE 로 끝난 뒤 이 핸들 (`+0x25ae0`) 이나 출력 값을 다시 쓰는 첫 함수 이름 (xref). 엔진이 출력 0 을 줬을 때 뒤에서 터질 자리가 있는지 — 이름·한 줄 이유만, 본문 따라가지 않음.
4. (작게) attach 차례 앞쪽 — `NtQueryInformationProcess` class `0x2b` (길이 `0x218`) 의 PDB `PROCESSINFOCLASS` 이름, 그 결과가 attach 결과 (TRUE/FALSE) 에 닿는지만. 세마포어 둘·이벤트를 무엇에 쓰는지 (이름 붙은 전역이면 이름만). class `0x56` 은 run91 `IsEnclave` 와 같은 함수/같은 꼴이면 가리키기만.
5. **최소 조건 표**: "attach 가 TRUE 로 끝나려면 바깥 (nt 엔진·팀원 syscall 몸) 이 줘야 하는 답" — syscall · 입력 (이름·IOCTL) · 기대 답 (status·핸들·출력 칸) · 안 맞으면 가는 자리. run91 (bcryptprimitives) 표와 나란히 볼 수 있게.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| bcrypt | 진입점 (`DllMain`·`_DllMainCRTStartup` 류 — PDB 이름으로 확인) | 1 | 질문 1 — attach 갈래 |
| bcrypt | attach 갈래에서 `NtOpenFile`·`NtDeviceIoControlFile` 을 부르는 함수까지 (`+0x25ae0` 의 xref 로 찾아도 됨) | 1 ~ 줄기 끝 | 질문 1·2 |
| bcrypt | `+0x25ae0` 과 IOCTL 출력 저장 칸의 xref (읽는 쪽) | 0 (이름만) | 질문 3 |
| bcrypt | `NtQueryInformationProcess` 0x2b·0x56, `NtCreateSemaphore`·`NtCreateEvent` 부르는 곳 | 0 ~ 1 | 질문 4 (갈림 자리만) |

- 범위: attach (PROCESS_ATTACH) 한 갈래만. DETACH·THREAD_ATTACH·export 된 암호 함수 본문은 넣지 않는다 (질문 3 은 이름만).
- **커널 `ksecdd.sys` 의 `0x390400` 처리는 범위 밖** — 유저 쪽이 무엇을 기대하나만.
- 공용 부품 (메모리 복사·락·문자열) 은 이름만. ETW (`NtTraceControl`) 는 run91 과 같은 꼴이면 가리키기만.
- 함수가 많으면 (5 개 넘게 깊음) 질문 1·2 를 먼저, 3·4 는 이름·갈림 자리만.

## 지도에서 본 것

- 없음 — bcrypt.dll 은 처음. analyze 뒤 `run_ghidra.sh map` 으로 진입점 깊이·직접 호출을 보고 대상 함수를 골라도 된다.

## 이미 아는 것

- `[관찰 — 에뮬레이터 로그]` 위 "배경" 의 syscall 차례·인자. 진짜 Windows 에서 본 것 아님.
- `[정적]` run91 (sast/87): bcryptprimitives `GetCngHandle` 은 `NtOpenFile` status 만 보고 핸들 칸을 그대로 돌려줌, IOCTL `0x390008` 출력 내용은 안 봄. bcrypt.dll 이 같은 꼴인지는 모름.
- 덤프: `System32/bcrypt.dll` 있음 (오케스트레이터 확인). 우리 repo (`ms/decomp`·`ms/disassemble`·`runs`·`_workspace`) 에 bcrypt 분석 0.
- `[추측]` 세마포어 둘·이벤트는 KsecDD 와 나누는 무언가 (IOCTL 입력에 핸들로 들어감) — 코드로 맞는지 틀린지 볼 것.

## 받고 싶은 것

- 결과서 `_workspace/sast/88_bcrypt-ksecdd_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`. bcrypt PDB GUID.
- 질문 1·2 는 차례 표 (함수+오프셋 · syscall · 인자 · 갈림). 질문 2 입력 `0x68` 은 칸 표 (오프셋 · 크기 · 값 출처).
- 질문 5 최소 조건 표.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (지금 우리 = 경로 모르면 status 0 + 핸들 안 씀 → IOCTL 핸들 0 → `0xc0000008`).
- 끝에 **"동적으로 확인할 것"** (IOCTL 출력의 실제 값처럼 실행 때만 정해지는 것 — 칸만, `[추측]`) 과 **"다음 요청 후보"**.
