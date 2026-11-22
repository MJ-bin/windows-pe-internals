# 124 irp-complete — 정적 분석 요청

- 날짜: 2026-11-17
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청)
- 대기열: **Q219 의 R6 몫** (PE_emulator 요청 "AFD 주소 목록 묻기" 의 묶음 나 — IRP 길 완료). 사용자 결정 2026-11-17 "다른 것보다 이거 먼저". 오케스트레이터가 ntoskrnl 몫만 떼어 sast/123 (afd·mswsock) 과 같은 시간에 돌린다. **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인). 이미 analyze 된 프로젝트 — 다시 analyze 하지 말고 `^이름$` export 만.
- `_index.tsv`: `IopSynchronousServiceTail` `0x8eb0e0` (1080 B) · `IopCompleteRequest` `0x214750` (1567) · `IopfCompleteRequest` `0x2b2318` (1991). 셋 다 `.asm` 없음.
- 주의: 같은 시간에 다른 decompiler 가 **afd·mswsock** (sast/123) 을 돈다 — 그쪽은 건드리지 않는다.

## 이미 아는 것

- IRP 길: 이벤트는 드라이버 앞에서 비움 · 그 뒤 `IopSynchronousServiceTail` `[정적] ms/disassemble/ntoskrnl/IopXxxControlFile.asm +0x2db, +0x997`.
- 즉시 (fast I/O) 길은 IOSB 16 B 복사·`KeSetEvent` `[정적] IopXxxControlFile.asm +0xb38 ~ +0xb66, +0xbc2 ~ +0xbe4` — 이번 범위 아님 (비교용).
- mswsock 은 PENDING 이 아니면 IOSB·이벤트를 안 봄 `[정적] ms/disassemble/mswsock/SockDoConnectReal.asm +0x420, +0x46f, +0x471` (sast/121).
- 에뮬레이터 (갈래 K, K02) 는 지금 IRP 길 IOCTL 에 "status 만 돌려줌 · IOSB 안 씀 · 이벤트 비운 채". 대표 호출: 동기 핸들 (`FILE_SYNCHRONOUS_IO_NONALERT`) 의 `0x12007`, 비동기 핸들 (CreateOptions 0) 의 `0x120bf`.

## 알고 싶은 것

**R6. `IopSynchronousServiceTail` · `IopCompleteRequest`** (갈래가 거기 있으면 `IopfCompleteRequest` 도)
1. 드라이버가 IRP 를 **PENDING 없이** 끝냈을 때 (가) 사용자 IoStatusBlock 에 쓰나 (나) 사용자 이벤트를 세우나 (다) 완료 포트 패킷을 넣나 (라) APC 를 거는가.
2. 위를 status 별로 나눠: 성공 · 경고 (`0x8…`) · 오류 (`0xC…`) — 특히 오류 때 IOSB 를 안 쓰는 갈래가 있나 (`NT_ERROR` 검사 줄).
3. 핸들별로 나눠: 동기 핸들 (`FO_SYNCHRONOUS_IO`) 과 비동기 핸들 (CreateOptions 0) — 동기 핸들이면 파일 객체 이벤트·`FinalStatus` 쪽, 비동기면 사용자 이벤트 쪽. 반환 status 가 무엇이 되나 (드라이버 status 그대로인가).
4. PENDING 일 때 (참고용, 짧게): 동기 핸들은 어디서 기다리나, 비동기는 바로 `0x103` 을 돌려주나.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^IopSynchronousServiceTail$` | 1 | R6 |
| ntoskrnl | `^IopCompleteRequest$` · `^IopfCompleteRequest$` | 1 | R6 |

- 범위: I/O 관리자 완료 쪽만. APC 배달·완료 포트 내부 (`KeInsertQueue` 류) 는 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/124_irp-complete_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/ntoskrnl/{IopSynchronousServiceTail,IopCompleteRequest,IopfCompleteRequest}.asm`. 근거는 `[정적] ms/disassemble/ntoskrnl/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"**.
  3. **표 하나**: 행 = (성공 / 경고 / 오류) × (동기 핸들 / 비동기 핸들), 열 = 반환 status · IOSB 에 쓰나 · 사용자 이벤트 · 파일 객체 이벤트 · 완료 포트 · APC · 줄 (+오프셋).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 — K02 의 "status 만 · IOSB 안 씀 · 이벤트 비운 채" 가 진짜와 다르면 어떻게 고치나.
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.
