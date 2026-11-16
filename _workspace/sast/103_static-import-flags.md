# 103 static-import-flags — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q72** (2.5·2.7 Q54 남은 전수 확인, 작음). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 빌드: 26100.9457. ntdll analyze 됨. `_index.tsv`: `LdrpAllocateModuleEntry` `0x6fab0` (412) · `LdrpRecordModuleDependency` `0xd8e40` (326) · `LdrpLoadDependentModuleInternal` `0x21ab0` (9250) · `LdrpInitializeGraphRecurse` `0x87d60` (209) · `LdrpInitializeNtdllDataTableEntry` `0x7015c` (633).
- 관련: Q54 를 문서로 닫으며 남은 것 (2026-10-24 사용자 결정). 모듈 항목 `Flags (+0x68)` 비트 `0x20` = `ProcessStaticImport`, 간선 칸 `+0x18` 비트 0.
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/101) 을 돌린다 — ntoskrnl 쪽은 건드리지 않는다. **전수 검색은 `.asm` 모음 (ntdll 함수 4594 중 일부뿐) 으로는 안 된다** → sast/100·102 처럼 `objdump -d` 로 ntdll `.text` 전체를 풀어 바이트·명령 꼴 검색 (스크래치 폴더에 `ntdll.dis`·`fnmap.py` 가 남아 있으면 재사용, 없으면 다시 만듦). 이미 있는 `.asm`·`.c` 는 다시 뽑지 않는다.

## 알고 싶은 것

1. **(a) `Flags (+0x68)` 에 `0x20` 을 켜는 곳 전수**: `LdrpAllocateModuleEntry +0x194` 하나뿐인가 — ntdll 전체에서 모듈 항목 `+0x68` 에 `OR …,0x20` / `BTS …,5` / `0x20` 이 든 마스크를 쓰는 줄. 찾은 줄마다 함수+오프셋·조건.
   - 조건 칸 `ntdll+0x1cc920` (`LdrpDllNotificationLock+0x10` 으로 보임) 의 뜻 — PDB 이름 (`pdb_aliases.py --where ntdll.dll` 로 그 주소 이름), 누가 쓰나 (`_refs`).
2. **(b) 간선 칸 `+0x18` 비트 0 전수**: 켜는 곳·지우는 곳·읽는 곳. 지금까지 아는 것: 켬 `LdrpRecordModuleDependency +0x11a` · `LdrpLoadDependentModuleInternal +0x17d9`, 지움 `+0xede`, 읽음 `LdrpInitializeGraphRecurse +0x3a`. 빠진 게 있나. 간선 구조체의 다른 칸이 같은 오프셋 `+0x18` 을 쓰지 않는지 (간선 포인터로 접근하는 줄만 세기 — 판단 근거 적기).
3. **(b) `LdrpRecordModuleDependency` caller** 가 정말 `LdrpInitializeNtdllDataTableEntry +0x23e` 하나인가 (직접 호출 + 간접 (함수 포인터 표) 둘 다).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpAllocateModuleEntry$` | 0 | 질문 1 확인 |
| ntdll | `^LdrpRecordModuleDependency$` | 0 | 질문 2·3 |
| ntdll | `^LdrpInitializeGraphRecurse$` | 0 | 질문 2 |
| ntdll | `.text` 전체 `objdump` 검색 | — | 질문 1·2·3 전수 |

- 범위: 위 질문만. 찾은 줄이 새 함수 안이면 그 함수는 이름·오프셋만 (본문 읽기는 조건 줄까지).

## 받고 싶은 것

- 결과서 `_workspace/sast/103_static-import-flags_result.md`. 질문별 **전수 표** (함수+오프셋 · 명령 · 켬/지움/읽음 · 조건) + 검색 방법 (패턴·놓칠 수 있는 꼴) 한 줄, `[정적]`.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.
