# 106 cet-context-small — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q75** (2.10 `NtContinue` 길 CET 남은 정적, 작음). Q74 (ntdll+ntoskrnl) 는 ntdll 이 sast/103 에 쓰이는 중이라 그 뒤에 돈다 (문서는 대기열 차례대로 닫음). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 빌드: 26100.9457. ntoskrnl analyze 됨. `_index.tsv`: `RtlpCopyXStateChunk` `0x3f5db4` (700) · `RtlpCopyKernelCetChunk` `0x5e710c` (63) · `PspGetSetContextInternal` `0x9b7a20` (1712) · `PspGetContextThreadInternal` `0x90fa40` (736) · `KeContextFromKframes` `0x3ecaf0` (1493) · `KxContextToKframes` `0x3e9fc0` (1381).
- 관련: run42 (`runs/02_sprint/run42-continue-detail.md`, sast/37) — `NtContinue` 길 CET. 110줄: 이름 없는 조각 `FUN_1406f9e12 +0xf4` (`nt+0x6f9f06`) `RDMSR` → −8 → `WRUSSQ` → `WRMSR 0x6a7`. sast/91·run95 와는 별개.
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/103) 과 **sample dll** (sast/105) 을 돌린다 — 그쪽은 건드리지 않는다. 이미 있는 ntoskrnl `.asm`·`.c` 는 다시 뽑지 않고, 필요한 것만 `^이름$`.

## 알고 싶은 것

1. **(a) 시동 문맥 (`0x10005b`) 의 XState CET_U 칸 SSP 값은 어디서 오나**: `PspGetContextThreadInternal` · `KeContextFromKframes` · `RtlCopyContext` 가 부르는 `+0x11c RtlpCopyXStateChunk` · `+0x1b3 RtlpCopyKernelCetChunk` 가 CET_U 칸에 무엇을 넣나 (MSR `0x6a7` `IA32_PL3_SSP` 를 읽나, 스레드 구조 칸을 읽나 — 칸 오프셋). `KxContextToKframes +0x211 XRSTORS` 가 이 값으로 유저 SSP 를 되돌림 (run42). 처음 스레드의 SSP 값을 누가 정하나 (스택 만들 때 섀도 스택 할당 — 이름만).
2. **(b) `PspGetSetContextInternal`** 이 5번째 칸 (인자 5 — run42 에서 말한 칸) 을 쓰나 (O/X + 줄).
3. **(c) 조각 `FUN_1406f9e12`** 가 어느 함수의 조각인가 — `pdb_aliases.py --where ntoskrnl.exe 0x6f9e12` 류로 PDB 이름, 또는 조각으로 뛰어오는 부모 함수 (`.pdata` 언와인드 부모 · JMP 하는 곳).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^RtlpCopyXStateChunk$`, `^RtlpCopyKernelCetChunk$` | 0 | 질문 1 |
| ntoskrnl | `^KeContextFromKframes$`, `^PspGetContextThreadInternal$` | 0 | 질문 1 — `+0x11c`·`+0x1b3` 앞뒤만 |
| ntoskrnl | `^PspGetSetContextInternal$` | 0 | 질문 2 |
| ntoskrnl | rva `0x6f9e12` 조각 | — | 질문 3 |

- 범위: 위 질문만. 공용 부품은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/106_cet-context-small_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** (Q57 과 겹치면 표시 — `rdmsr 0x6a7` 한 번) 과 **"다음 요청 후보"**.
