# 115 eprocess-locks — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q99** (`_EPROCESS` 작은 정적 묶음, run51 넘김). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe · ntdll.dll (PDB GUID 확인)
- 관련: `runs/03_sprint/baggage.md` **3.11 `_EPROCESS`** — `+0x318 DeviceMap` (②, 게으른 만들기, `ObpLookupObjectName +0x143d`) · `+0x370 ThreadListHead` (run79 — `PspInsertThread +0x6ac` `ThreadListLock`) · `+0x550 ModifiedPageCount` (①, 런타임 카운터 — "어느 목록인지는 …") · `+0x5c8 InvertedFunctionTable` (②, 락 `+0x5d0`) · 3.11 "범위 밖 길에서만 씀 52" 줄 (`InvertedFunctionTableLock`·`ThreadListLock` 가 여기 있음). sast/45 결과서 (run51 Q78c2b), sast/75 결과서 (run79).
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/113) 을 돌린다 — **ntdll Ghidra 프로젝트는 열지 않는다.** ntdll 쪽 질문 (②) 은 이미 있는 `ms/disassemble/ntdll/*.asm`·`_calls.tsv`·스크래치 objdump (`/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/ntdll.dis` 류) **읽기만**으로. 이미 있는 ntoskrnl `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`.

## 알고 싶은 것

1. **두 락이 초기화 길에서 잡혔다 놓이나** — 지금 3.11 에 "범위 밖 길에서만 씀" 으로 둔 칸:
   - `InvertedFunctionTableLock` (`+0x5d0`) — 클래스 `0x35` 길 (`NtSetInformationProcess` → 동적 함수표 넣기, `+0x5c8` 를 처음 만드는 길) 에서 `ExAcquirePushLock*`/`ExReleasePushLock*` 로 잡았다 놓나 (줄). 그 길을 test.exe 초기화가 타면 (run51 이 ② 로 셈) 이 락 칸도 "② 에서 쓰고 0 으로 돌아옴" 으로 가름 바꿀 수 있나.
   - `ThreadListLock` (`+0x5e0`) — `PspInsertThread +0x6ac` 푸시락 (run79) 이 잡았다 놓는 줄 (`KeAbPreAcquire`·`ExAcquirePushLockExclusiveEx`·`ExReleasePushLockEx` — 오프셋). ① 에서 잡고 놓음 → 가름 "① 에서 쓰고 0 으로" 로 옮길지.
   - 둘 다 셈 기준: 잡았다 놓으면 칸 값이 처음 (0) 으로 돌아오는가 (푸시락 비트 꼴 — 대기자 없으면 0) 를 코드로.
2. **`DeviceMap` 을 처음 채우는 ntdll 쪽 호출자** — 커널 쪽은 `ObpLookupObjectName +0x143d` (`\??\` 로 시작하는 이름을 풀 때 게으르게). 그 이름을 처음 넘기는 ntdll 호출 (초기화 길 — `LdrpInitializeProcess` 아래 `NtOpenFile`·`NtOpenDirectoryObject`·`NtQueryAttributesFile` 중 `\??\` 경로를 쓰는 첫 syscall) — ntdll `.asm`·`_calls.tsv` 와 run 문서 (run21·run39 등 경로를 여는 바퀴) 로 함수+오프셋, 사진 구간 (`P1~P1a` 등).
3. **`ModifiedPageCount` 가 오르는 페이지 목록** — `MiInsertPageInList`·`MiInsertPageInModifiedList` 류 (이름 확인) 의 어느 줄이 `[EPROCESS+0x550]` 을 올리나, 그 목록 이름 (변경 목록 `MmModifiedPageListHead` 류). test.exe 초기화에서 이 카운터가 오르는 이유 한 줄 (쓰기 가능 페이지를 내보낼 때 `[추측]`).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | 클래스 `0x35` 처리 (`PspSetInformationProcess`/`NtSetInformationProcess` 갈래 — 동적 함수표 넣기 함수, 이름 확인) | 0 | 질문 1 |
| ntoskrnl | `^PspInsertThread$` (있음 — 읽기만) | 0 | 질문 1 |
| ntoskrnl | `+0x550` 쓰기 전수 (스크래치 `nt.dis`) → 든 함수 | 0 | 질문 3 |
| ntdll | (읽기만) `\??\` 경로를 여는 초기화 길 syscall 자리 | — | 질문 2 |

- 범위: 위 질문만. 공용 부품 (푸시락 본문) 은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/115_eprocess-locks_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`, baggage 3.11 에 고칠 줄 (가름이 바뀌면 바뀐 셈 — 3.11 머리 숫자).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.
