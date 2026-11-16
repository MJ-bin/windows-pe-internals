# 114 systemroot-link — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴** — 부팅 준비 `Ⓑ` 짐)
- 대기열: **Q95** (`\SystemRoot` 링크 만드는 곳, 작음). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인). `_index.tsv`: `CreateSystemRootLink` `0xc18a18` (1111).
- 관련: sast/42 결과서 (`_workspace/sast/42_system-dll-boot_result.md` 248줄 공백 2 — "`\SystemRoot` 링크를 만드는 곳과 그 시점 (`PsLocateSystemDlls` 앞)", 76줄 `Phase1InitializationDiscard` 앞뒤 호출: `+0x10de PpLastGoodDoBootProcessing`·`+0x1126 MmInitializeImageViewExtension`·`+0x112d PfSnBeginBootPhase`·`+0x1139 IopReassignSystemRoot`), run48 (`runs/03_sprint/run48-*.md`, Q93 부팅 준비 갈래). 후보 줄 (Q95): `Phase1InitializationDiscard +0xc31 CreateSystemRootLink` (ret `+0xc36`) `[추측]`.
- 이유: test.exe 시동에서 ntdll·exe 경로가 `\SystemRoot\System32\ntdll.dll` 꼴로 열린다 (`PspSystemDlls` 표 처음 값, sast/42) — 그 이름이 풀리려면 `\SystemRoot` 심볼릭 링크가 먼저 있어야 한다. 짐 지도의 `Ⓑ` (부팅) 입력 짐.
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/113) 을 돌린다 — ntdll 쪽은 건드리지 않는다. 이미 있는 ntoskrnl `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`.

## 알고 싶은 것

1. **만드는 곳**: `CreateSystemRootLink` 가 `\SystemRoot` 심볼릭 링크 객체를 만드는 줄 (`NtCreateSymbolicLinkObject`/`ObCreateSymbolicLink` 류 — 함수+오프셋), 대상 글 (`\Device\Harddisk…\Partition…\WINDOWS` 꼴 — 어디서 오나: 로더 블록 `ArcBootDeviceName`·`NtBootPathName`, `IoAssignArcName` 류), 링크 속성 (영구·대소문자 무시), SD (있으면).
2. **시점**: `Phase1InitializationDiscard +0xc31` 이 맞나 (부르는 곳 전수), `PsLocateSystemDlls` 보다 앞인가 (두 호출 자리 오프셋 비교). `IopReassignSystemRoot` (`+0x1139`) 가 나중에 링크 대상을 바꾸나 — 바꾸면 조건 (BitLocker·VHD 부팅 등) 과 test.exe VM 에서 타나 `[추측]` 이면 `[VM]`.
3. **같이 만드는 것**: 같은 함수가 `\ArcName\…`·`\Device\…` 등 다른 링크도 만들면 이름만 목록으로.
4. 짐 지도: `\SystemRoot` 링크 객체를 baggage 7.6 (체크리스트 밖, 부팅 `Ⓑ` 커널 짐) 에 넣을 3장 양식 줄 (공간 = 커널 객체 이름 공간, 단계 `Ⓑ`, 놓는 법 = 직접, 쓴 주인 = nt, 경로).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^CreateSystemRootLink$` | 1 | 질문 1·3 |
| ntoskrnl | `^Phase1InitializationDiscard$` (있으면 읽기만) | 0 | 질문 2 — 호출 자리 |
| ntoskrnl | `^IopReassignSystemRoot$` | 0 | 질문 2 — 바꾸는 조건만 |

- 범위: 위 질문만. 공용 부품 (`ObCreateObject`, 문자열) 은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/114_systemroot-link_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`, baggage 3장 양식 줄.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (에뮬레이터 이름 공간에 `\SystemRoot` 가 있나 — 모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** (`!object \SystemRoot` 대상 글 — 부팅 뒤 읽기만) 과 **"다음 요청 후보"**.
