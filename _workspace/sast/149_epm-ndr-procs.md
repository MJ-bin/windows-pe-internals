# 149 epm-ndr-procs — 정적 분석 요청

- 날짜: 2026-11-27
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청)
- 대기열: **Q289** (새, 380 ③). PE_emulator 요청 원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 의 **"③ rpcrt4 EPM NDR 2.0 프로시저 3 · 7 파라미터 표" 절 전부** (읽기만 — 02 가 `pefile` 로 읽은 "확인 거리" 표가 있음. **근거 아님 — 맞는지 대조할 것**). 사용자 결정 2026-11-27 (PE_emulator D179): 차례 ③. **정적만.**
- 먼저 읽는 것: `runs/03_sprint/run137-alpc-completion.md` 3-0 · 3-1 (NDR 2.0 프로시저 문자열 시작 `0xefeb2` + 오프셋 표 · 8 의 파라미터 표 · `:238` "3 은 객체 · 탑이 `FC_FP`") · `runs/03_sprint/run145-epm-endpoint.md` (`:264` "PE_emulator 쪽" 줄 — 정정 대상) · `_workspace/sast/132_alpc-completion_result.md` (pefile 로 `.rdata` 를 읽은 꼴 — 같은 방법으로).
- 대상 빌드: 26100.9457 x64 `System32/rpcrt4.dll`. **`pefile` 로 `.rdata` 를 직접 읽는 것이 주** (sast/132 꼴). Ghidra 프로젝트 `rpcrt4` 는 꼭 필요할 때만 (`LookupNextChunk` asm 은 이미 있음: `+0x11d` (8) · `+0x160 ~ +0x16b` (3) · `+0x1a3 ~ +0x1ae` (7) · `+0x127 ~ +0x132` SID 검사).
- 같은 시간에 도는 decompiler: sast/147 · sast/148 · sast/152 — rpcrt4 를 여는 것은 이 요청뿐. 이 결과서가 나온 뒤 sast/154 (rpcrt4 · winnsi) 가 rpcrt4 를 연다. scratchpad 임시 파일은 `s149/` 하위.

## 알고 싶은 것 (원문 1 · 2)

1. **프로시저 3 (`0xeff42`) · 7 (`0xefff0`) 파라미터 표** — run137 3-1 의 8 표 꼴 (머리: 스택 크기 · 핸들 · Oi2 깃발 · 파라미터 수 / 파라미터마다 attr · 스택 자리 · 타입 오프셋 · 타입 바이트 · 포인터 종류). 요청 스텁 길이 예 — 탑 octets 66 일 때: 3 / 7 SID NULL / 7 SID 있음 (SID 길이 식). 원문 "확인 거리" 표와 칸마다 맞나 / 다르나.
2. **run145 `:264` 정정** — 7 이 8 꼴이고 3 만 `FC_FP` 면 그 줄을 어떻게 고쳐 읽어야 하나 한 줄 (고치는 일은 flow-writer 몫 — 결과서엔 고칠 글만). (되면 한 줄) NDR64 쪽 3 · 7 도 같은 관계인가.

## 받고 싶은 것

- 결과서 `_workspace/sast/149_epm-ndr-procs_result.md`. 근거는 `[정적] rpcrt4 .rdata rva 0x…` (pefile) 또는 `[정적] ms/disassemble/rpcrt4/<함수>.asm +오프셋`.
  1. 맨 위 **"먼저 읽는 한 줄 답"** (1 · 2).
  2. 파라미터 표 둘 (3 · 7) + 8 과 나란한 비교 줄 + 요청 스텁 길이 예 표 + "확인 거리" 대조 표.
  3. run145 `:264` 정정 글 (그대로 넣을 한 줄).
  4. PDB GUID 확인 줄 (pefile 이면 디버그 디렉터리 GUID).
- **"PE_emulator 쪽 — 지금 맞나·틀리나"**: `ext/syscall/syscall_layer_cpp/src/rpc/epm.cpp` 창구 표 7 · 3 의 `TODO(VM)` 에 쓸 꼴 — 프로시저마다 요청을 어떻게 풀어야 하나 한 줄 (BG04 시도 2 가 7 을 `FC_FP` 로 풀어 `0x6f7` 이 난 까닭 포함).
- 끝에: **"VM 으로 확인할 것 — 디버거 필요"** (선 위 진짜 바이트 — Q280 몫이면 그렇게) · **"VM 으로 확인할 것 — 디버거 없이"** (없으면 "없음") · **"다음 요청 후보"**.
