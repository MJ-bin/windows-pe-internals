# 84 sxs-create — 정적 분석 요청

- 날짜: 2026-11-13
- 스프린트: 03_sprint
- 대기열: **Q151a** (PE_emulator 요청 29 의 1·3 — 프로세스 기본 활성화 문맥을 누가·언제 만드나). 사용자 결정 2026-11-12: 요청 26 ~ 30 맨 앞, 29 는 4순위, **첫 답은 지도 수준**. 정적만 — VM 몫은 Q104 출처로. 로더가 읽는 쪽 (요청 29 의 2·4) 은 다음 바퀴 Q151b — **이번엔 안 본다**.
- 원문: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-PE-emulator/d6bd3369-b666-4505-9231-47b12d2704dd/scratchpad/internals_requests/request29_sxs_activation_context.md` (읽어도 됨).
- 겹침: 대기열 Q87 (부모 `KERNELBASE!BasepInitAppCompatData` 본문 — 부모가 자식 PEB `pShimData`·`SystemDefaultActivationContextData` 를 쓰나), Q83 (동적 — K3 → P0 사이 쓰는 순간). 이번에 닫히면 결과서에 "Q87 닫힘" 을 적는다.
- 빌드: 26100.9457. kernelbase·basesrv 는 analyze 됨. **sxssrv.dll·sxs.dll 은 아직 analyze 안 됨** → 필요하면 analyze (PDB 포함). 다른 decompiler 가 user32·win32u·ucrtbase·condrv·gdi32 를 쓰는 중일 수 있으니 그 프로젝트는 열지 않는다.
- 관련: run14 (`KERNELBASE!CreateProcessInternalW` 가 `NtCreateUserProcess` 뒤 `NtResumeThread`), run45 (일 37 `K3~P0` — 부모·csrss 가 자식 PEB `pShimData` (`25996d10000`, private)·`SystemDefaultActivationContextData` (`25996d00000`, 매핑) 를 씀 `[관찰]`), run58·run59 (`CreateProcessInternalW` ③ 앱 호환 후처리 — `BasepInitAppCompatData`·`NtWriteVirtualMemory +0x5795/+0x57d7/+0x5831`), run71 (csrsrv·basesrv 연결)

## 배경

- 덤프 notepad 는 매니페스트로 comctl32 v6 (WinSxS) 를 쓰는데, PE_emulator 는 활성화 문맥이 없어 System32 v5.82 를 올리고 서수가 없어 멈춘다 (`0xc0000138`). GUI 앱 대부분이 같은 길.
- 진짜에선 부모·csrss 가 문맥 데이터를 만들어 자식 PEB 에 넣는다. PE_emulator 엔 부모·csrss 가 없으니 **nt 엔진이 그 데이터를 직접 만들어 넣을 수 있는지, 최소 모양이 무엇인지** 가 필요하다. 이번 바퀴는 "누가·언제·어떻게 만들어 넣나" 쪽.

## 알고 싶은 것

1. **부모 쪽** (`CreateProcessInternalW`, rva `0x7d6e0`, 28087B — **SxS 갈래만**): 자식 exe 의 매니페스트를 찾는 줄 (자원 `RT_MANIFEST` id 1 · 외부 `.manifest` 파일), csrss 로 보내는 SxS 메시지를 만드는 함수 (`BasepConstructSxsCreateProcessMessage` 류 — 이름은 `_index.tsv`/`pdb_aliases.py` 로) 와 그 메시지 모양 (최소 칸), `CsrClientCallServer` (API 번호) 를 부르는 오프셋. 매니페스트 **없을 때** 갈래 (메시지를 안 보내나, 시스템 기본만 만드나).
2. **csrss 쪽** (basesrv `BaseSrvCreateProcess2` (rva `0x6ee0`) 등 → sxssrv → sxs.dll): 매니페스트를 해석하고 `ACTIVATION_CONTEXT_DATA` 블롭을 만드는 함수 이름, 만든 블롭을 **자식 프로세스에 넣는 법** — 섹션을 만들어 자식에 매핑하나 (`NtCreateSection`·`NtMapViewOfSection(자식 핸들)`), 자식 메모리에 쓰나 (`NtWriteVirtualMemory`), 그리고 자식 PEB 칸 (`ActivationContextData` `+0x2f8`?·`SystemDefaultActivationContextData` `+0x308`? — PDB 오프셋으로) 에 주소를 쓰는 쪽이 csrss 인가 부모인가.
3. **시스템 기본 문맥** (`SystemDefaultActivationContextData`) 은 매니페스트 없는 exe 에도 늘 만드나 (run45 사진: test.exe 도 `25996d00000` 매핑이 있음 `[관찰]`), 그 내용은 무엇으로 정해지나 (시스템 매니페스트·Windows 버전).
4. **WinSxS 경로 규칙** (요청 29 의 3): comctl32 v6 를 가리킬 때 블롭 안에 들어가는 것 — 전체 경로인가 어셈블리 이름 + 버전·공개키·아키텍처 키 (`amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.x_none_…`) 인가, 실제 폴더를 찾는 쪽이 해석 때 (csrss) 인가 로더 때 (ntdll) 인가.
5. **nt 엔진이 직접 만들어 넣는다면** 무엇을 흉내 내야 하나 — 한 줄 판단과 이유 (매니페스트 해석을 피할 수 있나: 예) 미리 만든 블롭 파일을 덤프에서 떠 와 매핑).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernelbase | `CreateProcessInternalW` 의 SxS 갈래 + 메시지 만드는 함수 (이름 확인) | 1 | 질문 1 — 28KB 함수, **SxS 갈래만** |
| basesrv | `^BaseSrvCreateProcess2$` 와 SxS 쪽 callee | 1 | 질문 2 |
| sxssrv / sxs | 블롭을 만드는 함수 (이름 확인) | 1 | 질문 2·3·4 — 해석기 본문 (XML) 은 이름만 |

- 매니페스트 XML 파서 본문·활성화 문맥 스택 API (`ActivateActCtx`) 전부는 넣지 않는다.
- 지도: `run_ghidra.sh map basesrv.dll BaseSrvCreateProcess2`.

## 이미 아는 것

- `[관찰]` run45: K3 ~ P0 에 자식 VAD 2 개 생김 — `25996d00000` 매핑 = `PEB.SystemDefaultActivationContextData` 값, `25996d10000` private = `PEB.pShimData` 값.
- `[정적]` run58·run59: `CreateProcessInternalW` ③ 앱 호환 후처리 (`BasepInitAppCompatData`, `NtWriteVirtualMemory +0x5795/+0x57d7/+0x5831`).
- `[관찰 — PE_emulator 흉내]` 문맥 없음 → System32 comctl32 v5.82 → 서수 #345·#381 없음 → `NtRaiseHardError(0xc0000138)`.

## 받고 싶은 것

- 결과서 `_workspace/sast/84_sxs-create_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`. 새로 analyze 한 모듈 PDB GUID.
- **질문 2 의 "누가 무엇을 자식에 넣나" 표** (차례 · 프로세스 (부모/csrss) · 함수+오프셋 · syscall · 자식에 생기는 것).
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 Q151b 로 넘길 것 (블롭 머리·섹션 모양 재료), "다음 요청 후보".
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 (또는 겹치는 Q83) 에 붙인다.
