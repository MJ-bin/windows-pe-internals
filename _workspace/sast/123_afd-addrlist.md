# 123 afd-addrlist — 정적 분석 요청

- 날짜: 2026-11-17
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **새 Q219** (PE_emulator 요청 "AFD 주소 목록 묻기 `0x120b3`" — 튜닝 `282_tune07.md T1`, 갈래 K 셋째 바퀴 K03). 사용자 결정 2026-11-17 "다른 것보다 이거 먼저". **정적만** — 정적으로 안 닫히는 것 (TL·NSI 몫) 은 `[추측]`·`[공백]` + "동적으로 확인할 것".
- **나눔 (오케스트레이터)**: 원 요청의 R1~R5 (afd·mswsock) 는 이 요청서, **R6 (ntoskrnl IRP 완료) 은 따로 `_workspace/sast/124_irp-complete.md`** — 다른 decompiler 가 같은 시간에 ntoskrnl 을 돈다. 이 요청서는 ntoskrnl 을 건드리지 않는다 (이미 있는 ntoskrnl `.asm` 읽기만은 됨).
- 대상 빌드: Windows 11 24H2 26100.9457 x64. afd.sys · mswsock.dll 은 sast/121 에서 이미 analyze 됨 (`ms/ghidra/`, PDB GUID afd `582DC2B31D185EBE4A3FDD4F872920491` · mswsock `A3E5C0BA2810B53B2646FE9284927D821`) — 다시 analyze 하지 말고 `^이름$` export 만.
- `_index.tsv` 확인: afd `AfdAddressListQuery` `0x25840` (1237 B) · `AfdTliIoControl` `0x26430` (3144) · `AfdBind` `0x39160` (2997) · `SOCKADDR_SIZE` `0x5b40` (29) · `AfdCompleteTliIoControl` `0x4880` (128) · `AfdCompleteTliIoControlCore` `0x4908` (631); mswsock `WSPIoctl` `0x191b0` (8547) · `WSPGetSockOpt` `0x9710` (3446).
- 이어 쓰는 것: **sast/121 결과서** (`_workspace/sast/121_afd-socket_result.md`, Q217) — 그 "다음 요청 후보" 2 의 일부 (`AfdBind`·`AfdTliIoControl`) + 새 것 (`AfdAddressListQuery`·`WSPIoctl`·`WSPGetSockOpt`·`SOCKADDR_SIZE`). 이번에 안 함: ws2_32·mswsock 시작·tcpip·nsi·`AfdSetInformation`·`AfdQueryHandles`·`AfdSuperConnect`·`AfdGetAddress`·`AfdGetRemoteAddress`.

## 왜 (요청한 쪽 말 요약)

- 에뮬레이터 로그 (진짜 Windows 관찰 아님 — 우리 기본 대답이 틀려 길이 바뀐 것): 소켓이 열리자 WinHttp 길에서 새 멈춤 `UC_ERR_WRITE_UNMAPPED` (`ntdll!RtlpAllocateHeap+0x7ed`). 이게 풀려야 갈래 K 를 합칠 수 있음.
- 두 표본 소켓 10/10 이 같은 꼴: 열기 → (`0x1207b` ×2) → `0x12047` → `0x120bf` (입력 `0x20`·출력 없음) → `0x12047` → **`0x120b3` ×3** → `0x120bf` (입력 `0x20`·**출력 8 B 힙**) → 닫기 (주소 목록만 묻고 닫는 임시 소켓).
- `0x120b3` 42/42: 입력 길이 2 · IOSB = 입력 주소 + `0x10` · Event = mswsock 스레드 이벤트 · APC 0. 세 번 묶음 14/14: 출력 **N B 힙 → 4 B 스택 (입력 주소 + `0x28`) → 8 B 힙**, N = `0x200` (10) · `0xc98` (4, 한 소켓에서 같은 버퍼로 되풀이).
- 요청한 쪽 짐작 `[추측]`: 우리 기본 대답이 "성공" 만 주고 출력·IOSB 를 안 써서, 부른 쪽이 안 쓴 칸에서 개수·크기를 읽어 다음 버퍼 크기를 `머리 8 + 칸 × 개수` 로 셈 → 쓰레기 크기로 64 MiB 넘는 할당. **어느 칸이 크기를 정하나가 핵심.**

## 이미 아는 것 (요청한 쪽이 근거와 함께 준 것)

- `0x120b3` → `AfdAddressListQuery`, 즉시 처리 표 (`AfdImmediateCallDispatch`). `0x120bf` → `AfdTliIoControl` · `0x12003` → `AfdBind` — 둘은 IRP 길 (`AfdIrpCallDispatch`). (sast/121 `AfdIoctlTable` 74칸)
- 즉시 처리 fast I/O 길: 표 찾기 → `&Information` = IoStatus + 8 → 처리 함수 → `IoStatus.Status` = 반환값 → TRUE `[정적] ms/disassemble/afd/AfdFastIoDeviceControl.asm +0xff ~ +0x114, +0x15d9 ~ +0x160a`. 그 뒤 IOSB 16 B 복사·`KeSetEvent` `[정적] ms/disassemble/ntoskrnl/IopXxxControlFile.asm +0xb38 ~ +0xb66, +0xbc2 ~ +0xbe4`.
- 처리 함수 인자 꼴 = (파일 객체, 코드, 요청 모드, 입력 포인터, 입력 길이, 출력 포인터, 출력 길이, `&Information`) `[정적] ms/disassemble/afd/AfdDispatchImmediateIrp.asm +0x5e ~ +0x8f`. 코드는 모두 `METHOD_NEITHER`.
- `0x120bf` 출력 없는 꼴: 입력 `0x20` (`+0x00` = 1 · `+0x04` 레벨 · `+0x08` 옵션 · `+0x0c` 바이트 · `+0x10` 값 포인터 · `+0x18` 값 길이), 부르는 쪽은 status 부호만 `[정적] ms/disassemble/mswsock/WSPSetSockOpt.asm +0x192 ~ +0x1c4, +0x1d6, +0x223 ~ +0x228, +0x250 ~ +0x252`. **출력 있는 꼴을 내는 줄은 모름.**
- `0x12003` mswsock 쪽: 입력 = 출력 같은 버퍼 · 길이 = `Information` · `WSPBind` 가 IOSB 를 먼저 0 `[정적] ms/disassemble/mswsock/WSPBind.asm +0x65, +0x68, +0x3cf, +0x45b, +0x4bf`. afd 쪽 모름.
- `AfdConnect +0x514` 가 `SOCKADDR_SIZE` 를 부름. 본문 없음.
- 이름만 본 이웃 (뜻 `[추측]`): afd `AfdInitializeAddressList` `0x291c0` · `AfdNsiAddressChangeEvent(AtPassive)` `0x29670`/`0x29778` · `AfdProcessAddressChangeList` `0x2a270` · `AfdTLBind` `0x41d8` · `AfdTLBindComplete(2)` `0x13d70`/`0x13e5c` · `AfdTLBindGetAddrComplete` `0x4b90`.

## 알고 싶은 것 — **순서대로, 나눠 답해도 됨**

**묶음 가 (1순위) 가 끝나면 결과서를 먼저 써 두고 (`## 묶음 가` 절까지), 오케스트레이터에게 "묶음 가 끝" 을 알린 뒤 나·다를 이어서 같은 결과서에 덧붙인다.** (요청한 쪽: 묶음 가만 먼저 와도 K03 을 바로 시작)

### 묶음 가 — 주소 목록 묻기 `0x120b3`

- **R1. afd `AfdAddressListQuery`**
  - (a) 입력 2 B 가 주소 계열인가. 길이·값 검사와 status. 엔드포인트 종류·상태로 거절하는 status.
  - (b) **출력 바이트 꼴**: 개수 칸 · 주소마다의 칸 (위치/포인터·길이) · sockaddr 본문. 포인터 칸 값이 버퍼 안 오프셋인지 주소인지. 성공 때 `Information`.
  - (c) **작은 버퍼**: 출력 길이 4·8 처럼 모자랄 때 status · `Information` (필요 크기인가) · 그때도 개수 4 B 를 쓰나. 출력 길이 0·포인터 NULL 일 때.
  - (d) 목록의 출처: 어느 전역 목록을 읽고 누가 채우나 · 계열로 거르는 줄 · 루프백을 빼는 줄 → **주소 0 개가 정상 갈래인가**, 그때 출력·`Information`. (망 없는 PC 에서 목록에 무엇이 남나는 NSI·TL 몫이면 `[추측]`)
- **R2. mswsock — `0x120b3` 을 내는 함수** (짐작 `WSPIoctl` 의 `SIO_ADDRESS_LIST_QUERY` 갈래. 아니면 즉치 `0x120b3` (바이트 `b3 20 01 00`) 을 가진 함수 전부와 부르는 사슬. mswsock 에 없으면 어느 모듈인지만 — ws2_32·webio 는 analyze 하지 말 것)
  - (a) 내는 줄마다 인자: 입력 2 B 출처 · IOSB 초기값 · 출력 버퍼와 길이의 출처 (로그: `0x200` 힙 · 4 B 지역 · 8 B 힙이 한 스택 틀).
  - (b) **돌아온 뒤 읽는 것**: 반환 status 와 견주는 값 (`0x103`·`0xC0000023`·`0x80000005` …) · `IoStatusBlock.Status`·`.Information` · 출력 `+0` 개수 · 출력 버퍼에 **쓰는** 줄 (포인터 고쳐 쓰기 고리) · `*lpcbBytesReturned` 와 WSA 오류로 가는 값.
  - (c) **다음 버퍼 크기 셈**: 어느 칸에서 개수를 읽어 어떤 식으로 크기를 내나 (`8 + 칸 × 개수` 꼴?). 셈이 mswsock 안인가, 부른 쪽 (ws2_32·webio) 인가. 되풀이 한도.

### 묶음 나 (2순위) — `0x120bf` 출력 꼴

- **R3. afd `AfdTliIoControl`** (+ 완료가 따로면 `AfdCompleteTliIoControl(Core)`) **· mswsock 에서 `0x120bf` 를 출력 버퍼와 함께 내는 함수** (짐작 `WSPGetSockOpt`)
  - (a) 입력 `0x20` 의 `+0x00` 값별 뜻 (1 = set? get 은?). afd 가 스스로 대답하는 것과 TL (tcpip) 로 넘기는 것.
  - (b) 출력에 무엇을 몇 바이트 · `Information` · 작은 버퍼 status.
  - (c) 부르는 쪽이 읽는 것. 로그 호출 (입력 `0x20` · 출력 8 B 힙 · IOSB = 입력 주소 + `0x30`) 이 어떤 레벨·옵션인지 가릴 수 있으면.
  - TL 이 정하는 값이면 "TL 몫" 이라고만.
- (R6 IRP 완료는 sast/124)

### 묶음 다 (3순위) — 연결 길 남은 칸

- **R4. afd `AfdBind`** (`0x12003`; `AfdTLBind`·`AfdTLBindComplete(2)`·`AfdTLBindGetAddrComplete` 가 관련되면 같이): TL 모드 입력 검사와 status · 출력 sockaddr 과 `Information` · 임시 포트를 누가 고르나 (TL 몫이면 그렇다고만) · 묶인 뒤 바뀌는 엔드포인트 칸. 대표 호출: 입력 = 출력 같은 버퍼 · 길이 `0x14` / `0x10`.
- **R5. afd `SOCKADDR_SIZE`**: 계열 → 길이 (계열 2·`0x17`, 표 밖 계열).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| afd | `^AfdAddressListQuery$` (+ 목록 출처가 따로면 `AfdInitializeAddressList` 등 필요한 것만) | 1 | R1 |
| mswsock | `^WSPIoctl$` (또는 `0x120b3` 을 내는 함수) | 1 | R2 |
| afd | `^AfdTliIoControl$` · `^AfdCompleteTliIoControl$` · `^AfdCompleteTliIoControlCore$` | 1 | R3 |
| mswsock | `^WSPGetSockOpt$` (또는 `0x120bf` 를 출력과 함께 내는 함수) | 1 | R3 |
| afd | `^AfdBind$` (+ TL bind 셋 필요한 것만) · `^SOCKADDR_SIZE$` | 0~1 | R4·R5 |

## 받고 싶은 것

- 결과서 `_workspace/sast/123_afd-addrlist_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/afd/{AfdAddressListQuery,AfdTliIoControl,AfdBind,SOCKADDR_SIZE}.asm` · `ms/disassemble/mswsock/{WSPIoctl,WSPGetSockOpt}.asm` (다른 함수면 그 함수). 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴로 (요청한 쪽이 코드에 붙임).
  2. 맨 위 **"먼저 읽는 한 줄 답"** (sast/121 꼴).
  3. 함수마다 표 셋:
     - **검사 표** — `차례 · 조건 · status · Information · 출력에 쓰는 것 · 줄 (+오프셋)`
     - **출력 바이트 칸 표** — `오프셋 · 크기 · 뜻 · 값 · 쓰는 줄 (+오프셋)`
     - **부르는 쪽 표** — `호출 · 보는 것 · 안 보는 것 · 줄` + 다음 크기 셈 식
  4. **최소 응답 한 절** (묶음 가): 주소 0 개인 PC 에서 `0x120b3` 을 출력 길이 `0x200`·4·8 로 부를 때 각각 반환 status · `IoStatusBlock.Status` · `.Information` · 출력 바이트. 주소 1 개 (IPv4) 일 때 출력 바이트 예 하나.
  5. 정적으로 못 닫은 줄은 `[추측]`·`[공백]`, **"동적으로 확인할 것"** 에 멈춤점 (`caller+0x…`) 과 함께 (sast/121 "동적" 4·6 과 같은 VM 세션에 얹을 수 있게).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 — 쓰는 곳: 갈래 K `ext/syscall/syscall_layer_cpp/src/file/nt_device_io_control_file.cpp` afd 갈래 (지금 `0x1207b`·`0x12047`·`0x12007` 만 대답, 나머지 기본 대답) · `tests/file.cpp`. 원칙: 진짜 망에 안 나감, 망 없는 PC 의 정직한 대답 — TL 몫이면 정책 값 + `TODO(VM)`.
- 끝에 **"다음 요청 후보"**.
