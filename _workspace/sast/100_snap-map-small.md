# 100 snap-map-small — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q69 + Q70** (둘 다 ntdll 로더 작은 정적, 대기열에서 연달아 있음 — 오케스트레이터 기본값으로 한 요청·한 바퀴). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 빌드: 26100.9457. ntdll analyze 됨 (PDB 붙음). `_index.tsv`: `LdrpCondenseGraphRecurse` `0xe91a0` (344) · `LdrpSnapModule` `0x3a8c0` (4052) · `LdrpDoPostSnapWork` `0xa4200` (228) · `LdrpMapAndSnapDependency` `0x210a0` (1544) · `LdrpMinimalMapModule` `0xa220` (915) · `LdrpPrepareModuleForExecution` `0x86e4c` (307).
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/98) 을 돌린다 — ntoskrnl 쪽은 건드리지 않는다. **이미 있는 ntdll `.asm`·`.c` 를 다시 쓰지 않게** 요청한 함수만 이름을 정확히 (`^이름$`) 지정해 뽑는다 (sast/99 때 정규식을 넓게 잡아 약 100개를 다시 쓴 일이 있음).

## Q69 — snap 실패 길 (sast/31 `[공백]`, run36)

1. 노드 `State = -3` (`LdrModulesSnapError`) 를 **처음** 쓰는 줄은 어디인가. 지금 아는 것은 전파 `LdrpCondenseGraphRecurse +0x14e` 뿐. 후보: `LdrpDoPostSnapWork` · `LdrpSnapModule` caller · `LdrpProcessWork` · `LdrpMapAndSnapDependency` 뒤 갈래 — ntdll 전체에서 노드 `+0x38` (State, run 문서 기준 — 오프셋 확인) 에 `-3` (`0xfffffffd`) 을 쓰는 줄을 `.asm` 전수 검색 (`MOV dword [..+0x38],0xfffffffd` 꼴) 으로 찾는다.
2. `LdrpSnapModule` 실패 반환이 caller `*(ctx+0x28)` (로드 문맥 status 칸) 에 들어간 뒤 **누가** 그걸 보고 State 로 바꾸나 — 사슬 (함수+오프셋).

## Q70 — 매핑 갈래 (sast/33 "정적으로 못 정한 것", run38)

3. 로드 문맥 `ctx+0x20` 비트 `0x800` · `0x800000` (→ `LdrpMinimalMapModule` 의 `NtMapViewOfSectionEx` 길) 을 **켜는 줄** — ntdll 전체 `OR`/`BTS` 전수 (`ctx+0x20` 에 그 비트를 넣는 줄), 그 조건 (test.exe 에서 타나 — 조건 칸 값으로 판단, 모르면 `[VM]`).
4. 결과 코드 `0x40000036` 의 이름 (`STATUS_IMAGE_AT_DIFFERENT_BASE` `[추측]`) — ntdll 안 상태 이름 표·PDB 열거·문자열로 확인 (없으면 "PDB 에 이름 없음" 이라고).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpCondenseGraphRecurse$` | 0 | Q69 — `+0x14e` 확인 |
| ntdll | `^LdrpDoPostSnapWork$` | 0 | Q69 후보 |
| ntdll | `^LdrpSnapModule$` | 0 | Q69 — 실패 반환 자리만 |
| ntdll | `^LdrpMapAndSnapDependency$` | 0 | Q69 — 뒤 갈래 |
| ntdll | `^LdrpMinimalMapModule$` | 0 | Q70 — 비트 읽는 줄 확인 |
| ntdll | 전수 검색 (이미 있는 `.asm` 모음 + 필요하면 `run_ghidra.sh refs`) | — | Q69 ①, Q70 ③ |

- 범위: 위 질문만. 공용 부품은 이름만.

## 이미 아는 것

- `[정적]` run36: 전파 `LdrpCondenseGraphRecurse +0x14e`.
- `[정적]` run38: `LdrpMinimalMapModule` 은 `ctx+0x20` 비트 `0x800`·`0x800000` 이면 `NtMapViewOfSectionEx` 길, 맵 결과 `0x40000003`·`0x40000036` 공통 검사 (`LdrpCompleteMapModule +0xe4`).

## 받고 싶은 것

- 결과서 `_workspace/sast/100_snap-map-small_result.md`. Q69 · Q70 절을 나눠 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.
