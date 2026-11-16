# 119 run58-60-leftovers — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q112 + Q113 + Q115** (run58·59·60 남은 작은 정적 — 사이에 있는 Q114 는 동적이라 건너뜀, 오케스트레이터 기본값으로 한 요청·한 바퀴). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe · ntdll.dll (PDB GUID 확인)
- 관련: sast/53 결과서 (run58 레지스트리 글), sast/54 결과서 (run59 "못 한 것"), sast/55 결과서 (run60 "안 닫힌 것"), sast/104 결과서 (Q63a — 시작 정보 블록·`PspCopyAndFixupParameters`), **sast/111 결과서** (`LdrpEnforceIntegrityContinuity`·`LdrpAuditIntegrityContinuity`·`RtlpUnhandledExceptionFilter` 를 이미 가름 — Q113 (c) 는 근거만 옮김).
- 주의: 지금 다른 decompiler 는 없다 (ntdll·ntoskrnl 둘 다 써도 됨, 한 번에 하나씩). 이미 있는 `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`. 전수 검색은 스크래치 objdump 풀이 (`/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/` 의 `ntdll.dis`·`nt.dis`) 재사용.

## Q112 — run58 남은 것

1. 상태값 `0xc000090b` 의 이름 (`RtlpGetNtProductTypeFromRegistry` 가 불일치·형식 불일치 때 돌려줌) — PDB 열거·ntdll 메시지 표 (sast/100 이 쓴 방법) 로.
2. ntoskrnl 사본 `RtlpQueryNlsSystemCodePages` 의 레지스트리 글 (키·값 이름) 이 ntdll 것과 같은가 (sast/52 이름 주의 — 같은 이름 다른 함수일 수 있음).
3. ntdll `.rdata` 의 같은 글 (rva `0x180ad0`·`0x180b50`·`0x180b58`·제품 종류 글) 을 쓰는 다른 함수가 있나 (refs 전수).

## Q113 — run59 남은 것 (PE_emulator 요청 4 몫)

4. 시작 정보 블록 완화 옵션 셋째 칸 (`+0xb0`, 사진 `0x02002002`) 비트 12~13·24~25 의 "2" 가 처음 어디서 오나 (커널 `PspPrepareSystemDllInitBlock`·`PspSetupUserProcessAddressSpace`·완화 정책 계산 — sast/104 결과와 이어서), 옵션 번호 32~39 와 이름 표 (PDB 열거 `PS_MITIGATION_OPTION` — sast/102 가 15번을 찾은 방법).
5. 만들기 정보 `+0x1d8` (`PspGetProcessParameterOverrides` 가 쓰는 유일한 칸) 을 읽는 함수 (커널 refs 전수).
6. baggage 7.5 결과서 밖 전역 셋 (`RtlpUnhandledExceptionFilter` · `LdrpEnforceIntegrityContinuity` · `LdrpAuditIntegrityContinuity`) — **sast/111 · run68 근거를 옮기기만** (새로 안 봄). 2장에 셀지 판단 한 줄.

## Q115 — run60 남은 것

7. `PspCopyUserAttributes` (`PspCopyAndFixupParameters +0x3eb`) 가 쓴 속성 목록이 새 프로세스 `ProcessParameters +0x448` 에 이어지는 줄 — sast/104 결과서 (`+0x430/+0x438` = `DefaultThreadpoolCpuSetMasks`/`…Count`, `AttributeList` 조건부 안 탐) 와 이어서 닫히면 근거만.
8. `LdrpSuppressedExportOverrideListPtr` 를 읽는 곳 (`_refs` 엔 쓰기 `LdrpInitializeProcess +0x49d` 하나뿐 — 간접 읽기? 포인터를 다른 전역에 담아 읽는 꼴, objdump 전수로).
9. (덤 — **싸게 되는 것만**, 길면 "다음 요청 후보" 로) 속성 `0x22` 공개 이름, `LdrpMakePermanentImageCommit` 목적, `+0xdc4` `ECX` 값, `LdrEnsureMrdataHeapExists` 가 쓰기까지 가는 조건, `.mrdata` 를 여닫는 다른 caller 약 20곳의 순서 (PE_emulator 가 따라갈 때).

## 받고 싶은 것

- 결과서 `_workspace/sast/119_run58-60-leftovers_result.md`. Q112 · Q113 · Q115 절을 나눠 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`, baggage 줄 (있으면).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.
