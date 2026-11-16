# 112 parent-handle-nls — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q88 + Q89** (둘 다 ntoskrnl 작은 정적, 연달아 있음 — 오케스트레이터 기본값으로 한 요청·한 바퀴). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인). `_index.tsv`: `PspCreateObjectHandle` `0x84509c` (175) · `PspUpdateCreateInfo` `0xa48da4` (386) · `NtGetNlsSectionPtr` `0xa27da0` (1298).
- 관련: run45 (`runs/03_sprint/run45-work-list.md` 1장 일 33·47, 422줄), baggage.md 7.6 (일 목록에서 칸 표가 없는 짐), run57 (`runs/03_sprint/run57-*.md`, sast/52 — NLS·ApiSet: `NtGetNlsSectionPtr` 부르는 자리·횟수, `ExInitializeNls`) — **sast/52 결과서를 먼저 읽고, 이미 답한 것은 근거만 옮기고 다시 안 본다.**
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/111) 을 돌린다 — ntdll 쪽은 건드리지 않는다. 이미 있는 ntoskrnl `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`.

## Q88 — 부모 쪽에 쓰는 것 (일 33)

1. `NtCreateUserProcess` 가 끝 무렵 부르는 `PspCreateObjectHandle` (프로세스·스레드 핸들을 **부모** 핸들 표에 만들기) · `PspUpdateCreateInfo` (부모가 준 만들기 정보 구조체의 **출력 칸** 채우기) 가 무엇을 어디에 쓰나:
   - 부모 핸들 표에 넣는 핸들 둘 (프로세스·스레드) 의 접근 마스크·속성, 부모 쪽 출력 인자 (`ProcessHandle`·`ThreadHandle` 포인터) 에 쓰는 줄.
   - `PspUpdateCreateInfo` 가 만들기 정보 (`PS_CREATE_INFO`) 의 어떤 칸 (상태 `State`, 섹션 핸들·파일 핸들·PEB 주소·`ManifestAddress` 등 — PDB 타입 이름) 을 쓰나, 값의 출처.
   - 부르는 자리 (`NtCreateUserProcess +오프셋`) 와 조건.
2. 짐 지도 관점: 이것들은 **부모 (cmd) 쪽** 짐이라 test.exe 의 짐이 아님 — baggage 7.6 "체크리스트 밖" 에 넣을 칸 목록 (공간 = 부모 유저 / 부모 핸들 표 커널) 을 3장 양식으로.

## Q89 — `NtGetNlsSectionPtr` 커널 쪽 (일 47: NLS VAD 의 주인)

3. test.exe 의 NLS VAD (`C_949.NLS`·`l_intl.nls` 등, run44 `!vad` 목록) 를 만드는 커널 길: `NtGetNlsSectionPtr` 가 섹션을 열고 (`\NLS\…` 객체 이름 — 부팅 때 누가 만들었나는 이름만) 뷰를 붙이는 줄 (`MmMapViewOfSection` 류 — 주소·크기·보호), 부르는 쪽 (ntdll `RtlpInitCodePageTables` 류, sast/52) 과 짝. 인자별 갈래 (타입 11 = 코드 페이지 · 12 = 대문자 표 등) 와 test.exe 가 타는 갈래.
4. 이 VAD 짐 칸 (3.17·3.18 `_MMVAD*` 또는 7.6) 에 "쓴 주인 = nt, 놓는 법 = syscall (`NtGetNlsSectionPtr`), 부르는 쪽 = ntdll" 줄을 3장 양식으로.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^PspCreateObjectHandle$`, `^PspUpdateCreateInfo$` | 1 | Q88 |
| ntoskrnl | `^NtCreateUserProcess$` (있음) | 0 | Q88 — 부르는 자리만 |
| ntoskrnl | `^NtGetNlsSectionPtr$` | 1 | Q89 |

- 범위: 위 질문만. 공용 부품 (`ObOpenObjectByPointer`, `ObInsertObject`, 핸들 표 안쪽) 은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/112_parent-handle-nls_result.md`. Q88 · Q89 절을 나눠 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`, baggage 3장 양식 줄.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.
