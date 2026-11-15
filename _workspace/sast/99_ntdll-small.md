# 99 ntdll-small — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q66 + Q67** (둘 다 ntdll 작은 정적, 대기열에서 연달아 있음 — 오케스트레이터 기본값으로 한 요청·한 바퀴). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 빌드: 26100.9457. ntdll analyze 됨 (PDB 붙음). `_index.tsv`: `CsrClientCallServer` rva `0xdbdd0` (449) · `RtlInsertInvertedFunctionTable` `0xa4074` (140) · `LdrpInsertDataTableEntry` `0xb380` (274) · `LdrpLoadPatchedNtdll` `0x8b9ac` (351) · `LdrpInitializeProcess` `0x8c404` (7750).
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/98) 을 돌린다 — **ntoskrnl Ghidra 프로젝트는 열지 않는다.** Q66 ① 의 KUSD 칸 이름은 ntdll PDB 타입 (또는 `ms/sym` 의 ntoskrnl PDB 를 `llvm-pdbutil`·파이썬으로 읽기만) 으로 본다.

## Q66 — 부모 쪽 작은 정적 둘 (run34 flow-writer "새 공백")

1. **`0x7ffe0308` 칸 이름**: ntdll syscall 스텁 `+0x8 TEST byte [0x7ffe0308],1` → `+0x10 JNZ` → `+0x15 INT 0x2e` (flow.md 469·2023줄). `_KUSER_SHARED_DATA +0x308` 의 칸 이름·크기 (`SystemCall` `[추측]`) 와 bit 0 뜻 — PDB 타입으로. 누가 쓰나 (커널 쪽 이름만, `_index.tsv` 문자열·xref 로 보이면).
   - 그리고 run44 사진 로그 (`ms/logs/` 의 KUSD 덤프 — 있으면) 에서 `+0x308` 값 (0 이면 `SYSCALL` 길) 을 찾아 한 줄.
2. **`CsrClientCallServer` 안쪽**: 부모 (`CreateProcessInternalW` → csrss 알림) 가 csrss 답을 **기다리나** — ALPC 보내고 받기 (`NtAlpcSendWaitReceivePort` 의 플래그·받는 버퍼 인자) 인가, 보내기만인가. 답 status 를 어디서 읽나 (메시지 칸 오프셋), 실패 status 를 돌려주나. depth 1.

## Q67 — ntdll 항목 주변 작은 정적 셋 (sast/30 "새로 생긴 공백", run35)

3. **(a) ntdll 예외 표 등록**: `LdrpInitializeProcess +0x415/+0x41a RtlInsertInvertedFunctionTable(ntdll 시작, SizeOfImage)` (run18 `[정적]`) — 이게 맞나 한 줄. 모듈 항목 `InExceptionTable` 비트를 미리 켜는 줄과 짝 (어느 줄이 먼저인가, 왜 미리 켜나 — 코드로 보이는 만큼).
4. **(b) `LdrpInitializeProcess +0x1625 LdrpInsertDataTableEntry`** 가 끼우는 항목 정체: 인자 `[RSP+0x60]` 이 무엇인가 (exe 항목? ntdll 항목? 복제?) — 그 칸을 채운 줄까지 거슬러.
5. **(c) `LdrpLoadPatchedNtdll`** 이 `[ctx+0]` 에 쓰는 것, 패치 항목·`LdrpNtdllHotPatchContext` 연결 — 핫패치 켜진 경우만. test.exe 에서 타나 (조건 줄 하나).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll PDB | `_KUSER_SHARED_DATA` 타입 `+0x308` | — | Q66 ① |
| ntdll | `^CsrClientCallServer$` | 1 | Q66 ② |
| ntdll | `^LdrpInitializeProcess$` | 0 | Q67 — `+0x3f0 ~ +0x460` · `+0x15f0 ~ +0x1630` 앞뒤만 |
| ntdll | `^RtlInsertInvertedFunctionTable$` | 0 | Q67 (a) |
| ntdll | `^LdrpInsertDataTableEntry$` | 0 | Q67 (b) |
| ntdll | `^LdrpLoadPatchedNtdll$` | 1 | Q67 (c) |

- 범위: 위 질문만. csrss 쪽 (`csrsrv`·`basesrv`) 처리는 이름만.
- 공용 부품은 이름만.

## 이미 아는 것

- `[정적]` run34 / flow.md 2023줄: syscall 스텁 모양.
- `[정적]` run18: `+0x415/+0x41a RtlInsertInvertedFunctionTable`.
- `[정적]` run68 (Q131 쪽): `CsrClientConnectToServer`·`CsrpConnectToServer` — 연결 쪽은 읽음. `CsrClientCallServer` 본문은 안 읽음.

## 받고 싶은 것

- 결과서 `_workspace/sast/99_ntdll-small_result.md`. Q66 · Q67 절을 나눠 질문별 답 + 근거 (`.asm` `+0x` 줄, PDB 타입), `[정적]`.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.
