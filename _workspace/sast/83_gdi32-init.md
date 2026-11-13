# 83 gdi32-init — 정적 분석 요청

- 날짜: 2026-11-13
- 스프린트: 03_sprint
- 대기열: **Q150b** (PE_emulator 요청 28 의 4·5 — gdi32 초기화와 "창 안 만드는 프로세스" 최소 조건). 사용자 결정 2026-11-12: 요청 26 ~ 30 맨 앞, 28 은 3순위, **첫 답은 "최소 길 지도" 수준**. 정적만 — VM 몫은 Q104 출처로.
- 원문: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-PE-emulator/d6bd3369-b666-4505-9231-47b12d2704dd/scratchpad/internals_requests/request28_user32_init.md` (읽어도 됨).
- 짝 바퀴: sast/82 user32-init (Q150a — user32 `DllMain` syscall 차례·`gSharedInfo`/`gpsi`·GUI 스레드 전환·win32u 번호). **결과서 `_workspace/sast/82_user32-init_result.md` 가 있으면 먼저 읽고**, 끝의 "Q150b 로 넘길 것" 을 따른다. 아직 없으면 gdi32 쪽부터 하고, 질문 3 (최소 조건 표) 은 sast/82 결과가 생긴 뒤 합친다.
- 빌드: 26100.9457. **gdi32.dll (190KB)·gdi32full.dll (1.2MB) 은 아직 analyze 안 됨** → 먼저 analyze (PDB 포함). user32·win32u 는 sast/82 가 쓰는 중이면 그 프로젝트는 열지 않고 결과서·`.asm` 만 읽는다.

## 배경

- user32 를 import 만 해도 PE_emulator 는 초기화에서 막힌다 (`0xc0000142`). gdi32 도 user32 와 함께 오는 일이 많다. 경계는 sast/82 와 같다 (DllMain 은 진짜, syscall 은 nt 엔진·팀원 몸, csrss·win32k 없음).

## 알고 싶은 것

1. **gdi32 `DllMain` (PROCESS_ATTACH)** — 진입점 → `GdiDllInitialize` 류 (이름은 PDB 로): 부르는 함수·syscall 을 차례대로 (오프셋 + ret). win32u `NtGdi*` syscall 이 있나, 공유 핸들 표 (`GdiSharedHandleTable` — PEB 칸) 를 어디서 받나, FALSE 를 돌려주는 자리.
2. **gdi32full `DllMain`** (gdi32 가 gdi32full 을 부르나, 따로 import 되나) — 같은 꼴로. 큰 모듈이니 `DllMain` 줄기만.
3. **"창을 안 만드는 프로세스가 진입점까지 가는 최소 조건" 표 둘** (요청 28 의 5 — sast/82 결과와 합쳐서):
   - **syscall 답 표**: (차례 · DLL · syscall (ntdll / win32u 번호·이름) · nt 엔진이 줄 답 · 이게 없으면 무슨 일).
   - **데이터 칸 표**: (어디 · 구조체·칸 · 값 · 읽는 줄 · 이게 없으면 무슨 일) — PEB 칸 (`GdiSharedHandleTable`·`ReadOnlySharedMemoryBase` 등), 공유 섹션 뷰, `gSharedInfo`/`gpsi` 최소 칸.
4. **"다음 요청 후보" 목록**: 창 만들기·메시지 루프·GDI 그리기로 가려면 더 봐야 할 것 (이름과 이유 한 줄씩).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| gdi32 | 진입점·`GdiDllInitialize` 류 (`_index.tsv` 로) | 2 | 질문 1 |
| gdi32full | 진입점·`DllMain` 류 | 1 | 질문 2 — 줄기만 |
| win32u | 질문 1·2 에 나온 `NtGdi*` 스텁 | 0 | 번호 (sast/82 가 전체 표를 뽑았으면 그것을 씀) |

- 그리기·글꼴·DC 안쪽은 이름만.

## 이미 아는 것

- sast/82 결과 (있으면): user32 `DllMain` syscall 차례·데이터 칸·win32u 번호.
- `[정적]` run68: kernelbase 가 csrss (BASESRV) 에 연결, PEB `+0x380`·`+0x88`·`+0x98` (게시판 뷰).
- `[관찰 — PE_emulator 흉내]` user32 `DllMain` → `NtOpenSection(SharedSection)` `0xc000003a` → `0xc0000142`.

## 받고 싶은 것

- 결과서 `_workspace/sast/83_gdi32-init_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`. gdi32·gdi32full PDB GUID.
- **질문 3 의 표 둘** — 요청 28 의 마지막 답.
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다.
