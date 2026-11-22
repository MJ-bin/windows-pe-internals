# 120 run65-66-leftovers — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q124 + Q127** (run65·run66 남은 작은 정적, ntdll). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
  - Q124 (a) `LdrpPolicyBits` 를 3.8 로 옮길지 → sast/111 이 답함 ("옮기자", 쓰는 줄 다섯 모두 패키지 갈래) — 근거만 옮김.
  - Q124 (e) 그림 2 에 새 상자 넣기 → **사용자 결정 대기** — 이번에 안 함.
  - Q127 (c) `SbUpdateSwitchContextBasedOnDll` 이 `SwitchBackContext` 를 쓰나 → sast/109 (`+0xd9`)·sast/116 이 답함 — 근거만 옮김.
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntdll.dll (PDB GUID 확인). `_index.tsv`: `LdrpQueryIllegalCWDDevices` `0x57d54` · `LdrpInitializeExecutionOptions` `0x11a4e0` · `LdrpInitShimEngine` `0x89444` · `LdrpLoadShimEngine` `0x8a07c` · `LdrpGetShimEngineInterface` `0x89b0c` · `LdrpSendPostSnapNotifications` `0xc240` · `LdrpAllocateTls` `0x104a0`.
- 관련: sast/59 결과서 (run65), sast/61 결과서 (run66), sast/107 결과서 (스케줄러 공유 데이터 — `LdrpAllocateSchedulerSharedData`), sast/111·113·116 결과서 (shim 엔진 조건 `[pShimData+0x210]`, `g_ShimsEnabled`, `g_pShimEngineModule`).
- 주의: 지금 다른 decompiler 는 없다. 이미 있는 `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`. 전수 검색은 스크래치 `ntdll.dis` 재사용.

## Q124 — run65 남은 것

1. **(b)** `LdrpInitializeExecutionOptions +0x733` (`LdrpQueryIllegalCWDDevices`) 가 IFEO 키가 없을 때도 닿나 (갈림 줄), 값은 어느 쪽이든 0 인가.
2. **(c)** exe TLS 블록 크기 (`0x11`B `[추측]`) 의 근거 줄 — `LdrpAllocateTls`·`LdrpInitializeTls` 가 exe TLS 디렉터리 (`StartAddressOfRawData`~`EndAddressOfRawData` + `SizeOfZeroFill`) 로 크기를 셈하는 줄, test.exe 파일 값으로 계산 (PE 바이트).
3. **(d)** 스케줄러 공유 데이터 섹션 크기 셈 (사진 2쪽) — sast/107 이 본 `LdrpAllocateSchedulerSharedData` (`NtSetInformationProcess` 클래스 `0x70`) 에서 크기가 어디서 정해지나 (ntdll 쪽 상수인가, 커널이 정하나 — 커널이면 이름만).

## Q127 — run66 남은 것

4. **(a)** 모듈 항목 `Flags.LoadNotificationsSent` 를 켜는 줄 전수 (`LdrpSendPostSnapNotifications +0xce` 말고 더 있나) — 모듈 항목 `Flags` 쓰기 전수 (sast/103 방법).
5. **(b)** shim 엔진을 올리는 길 (`LdrpInitializeProcess +0x19ef LdrpInitShimEngine` → `LdrpLoadShimEngine` → `LdrpGetShimEngineInterface`, `g_ShimsEnabled` 켜짐) 의 조건 — sast/116 의 `[pShimData+0x210]` 와 이어서, test.exe 가 왜 안 타나 한 줄.

## 받고 싶은 것

- 결과서 `_workspace/sast/120_run65-66-leftovers_result.md`. Q124 · Q127 절을 나눠 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`, baggage 줄 (있으면).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.
