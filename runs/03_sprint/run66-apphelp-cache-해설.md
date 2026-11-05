# run66 apphelp-cache — 해설

> 이 글은 [run66 문서](run66-apphelp-cache.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q125** — PE_emulator 요청 13 의 8번: "kernel32 를 올린 뒤 apphelp 캐시 묻기" (`NtApphelpCacheControl(0x6, …)`) (`_workspace/plan.md`, 이번에 **끝**)
  - 원문은 "급함 높음" — PE_emulator 가 kernel32 를 올리다 **여기서 멈췄다**. 그래서 사용자 결정 (2026-11-05) 으로 Q104 VM 단계 **앞**으로 당겨 했다.
  - 다음 항목: **Q104 VM 단계** (동적).
  - 남은 질문이 간 곳: 동적 몫 → **Q104 VM 단계**, 작은 정적 셋 → 새 **Q127** (맨 뒤).
- 근거:
  - run 문서 [`run66-apphelp-cache.md`](run66-apphelp-cache.md) (가장 중요)
  - 요청서 [`_workspace/sast/61_apphelp-cache.md`](../../_workspace/sast/61_apphelp-cache.md)
  - 결과서 [`_workspace/sast/61_apphelp-cache_result.md`](../../_workspace/sast/61_apphelp-cache_result.md) (판정: **분석성공**)
  - flow.md 변경: 기준 · 그림 2 (kernel32 준비 상자 아래 알림 사슬, 새 단계 상자, 새 모듈 상자 `ahcache.sys`) · 2.5 새 절 · 4장 · 4.1
  - 짐 지도 변경: 3.5 · 3.6 · 3.8 · 3.9 · 새 7.6.6 · 새 7.25 (3절에서 짧게)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다.
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`·`_P1b.txt`·`_P2.txt`) 를 **다시 읽은 것** 이다. 새 실행이 아니다.
  - 이번에 처음으로 **커널 드라이버 하나 (`ahcache.sys`)** 를 Ghidra 로 열었다. 지금까지는 nt·ntdll·kernel32 같은 큰 모듈만 봤다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 명부 (`PEB.Ldr`) · 모듈 항목 = **명부 한 줄** → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 2.6
- DDAG 노드 = 명부 줄마다 붙은 **연결 카드**, `State` = 카드의 **진행 도장** (5 ~ 9) → [run21 해설 2.6](../02_sprint/run21-load-dll-internal-해설.md) · [run27 해설](../02_sprint/run27-init-graph-해설.md) 2.1
- shim (호환성 고치기 장치, `[추측]` 일반적으로 알려진 설명) → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.5 · [run21 해설](../02_sprint/run21-load-dll-internal-해설.md) 2.7
- 쿠키로 **풀어서** 부르기 (전역에 숨겨 둔 함수 주소 풀기) → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.10
- KUSD = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 프로세스 힙 = **창고** → [run46 해설](run46-process-heap-해설.md)
- CFG = 간접 호출 목적지를 확인하는 **교환원** → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) 2.9
- 사진 P1a·P1b·P2 → [run44 해설 2.8](run44-baggage-snap-해설.md)

이번에 처음 나오는 말 (**apphelp**, **드라이버**, **IOCTL**, **IRP**, **AVL 표**, **호환성 DB**) 은 본문에서 처음 나오는 자리에서 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소 **신청 창구**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**.
- 명부 = 가게 **건물 명부**, DDAG 노드 = **연결 카드**, `State` = 카드의 **진행 도장**.
- 이번 바퀴에 새로 들어오는 그림 (flow.md 2.5 새 절의 비유를 이어받음):
  - 관리실이 물건 (DLL) 을 다 들인 뒤, 관리사무소에 **"이 물건, 리콜 대상인가요?"** 하고 묻는다.
  - 관리사무소는 스스로 답하지 않고, 건물 안 **호환성 상담실** (`ahcache.sys`) 에 **주문서** (IOCTL) 를 넣는다.
  - 상담실은 **리콜 목록 카드철** (AVL 표) 을 뒤져서 답한다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| 언제 묻나? | 모듈을 다 올리고 노드 도장이 7 이 된 **바로 뒤**, 모듈 하나에 한 번 `[정적]` (2.3) |
| 묻기 전에 거르나? | 조건은 하나 — 들어갈 때 `g_pShimmedModuleList == 0`. test.exe 에선 늘 참 `[정적]` + `[관찰]` (2.4) |
| **답칸을 미리 비우나? (U1 걱정)** | **예.** syscall 전에 버퍼 `0x188` 바이트와 출력 DWORD 를 0 으로 비운다 (`+0x29`·`+0x33`) `[정적]` (2.5) |
| 커널이 답하나? | **아니다.** `\Device\ahcache` 에 IOCTL `0x22001B` 를 넣고 드라이버 답을 그대로 돌려준다 `[정적]` (2.7) |
| 드라이버는 뭐라고 답하나? | 이름을 못 찾으면 **성공 (0) + 출력 0** `[정적]` (2.8) |
| **PE_emulator 기본 대답 (성공·출력 안 씀) 은 괜찮나?** | **안전하다.** 단 출력 비트 **`0x10`** 만 켜지 말 것 `[정적]` (2.10 · 2.11) |
| test.exe 초기화에서 몇 번? | **5번** (exe · dll1 · dll2 · KERNEL32 · KERNELBASE, ntdll 0번) `[추측]` — 사진이 강하게 받침 (2.12) |
| 앞선 run 과 어긋난 곳은? | **없다.** 정정 없음 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

DLL 을 메모리에 올리고 주소를 다 이은 뒤 (snap), **전원 켜기 (DllMain) 전**이다.
같은 자리가 두 번 나온다 — kernel32 들일 때 한 번, exe 의 import DLL 들일 때 한 번.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스·주소 공간·PEB·첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ … 명부에 ntdll·exe (run35, run19)
 │         ├─ kernel32 들이기 (LdrLoadDll → LdrpLoadDllInternal +0x284)
 │         │    └─ 올리기 → 주소 잇기 → LdrpPrepareModuleForExecution
 │         │         ├─ +0x7b 올린 뒤 알림 → apphelp 캐시 묻기      ← 이번 ★ (KERNELBASE, KERNEL32)
 │         │         └─ +0xef 켜기
 │         ├─ exe 의 import (dll1·dll2) 들이기
 │         │    └─ LdrpInitializeProcess +0x1af2 → LdrpPrepareModuleForExecution
 │         │         └─ +0x7b 올린 뒤 알림 → apphelp 캐시 묻기      ← 이번 ★ (dll2, dll1, exe)
 │         └─ … 전원 켜기 → TLS 콜백 (run25 ~ run28)
 └─ … → NtContinue → exe 진입점 → main
```

- 위 `+0x1af2`·`+0x284` 두 자리가 "부르는 쪽 둘" 이다 `[정적]` (run 문서 질문 5).
- 같은 코드라서, 아래 2절은 kernel32 쪽 하나로 설명한다.

### 왜 이 질문이 지금 왔나

- PE_emulator 는 진짜 ntdll 코드를 그대로 돌린다. kernel32 를 `LdrLoadDll` 로 올렸더니, kernel32·KernelBase 를 다 올린 뒤 **약속에 없던 syscall** 에서 멈췄다.
  - 그 syscall 이 `NtApphelpCacheControl(0x6, …)`, 번호 `0x4c`.
  - 사슬 (PE_emulator 실측, 요청서): `LdrpPrepareModuleForExecution` → `LdrpNotifyLoadOfGraph` → `LdrpSendPostSnapNotifications` → `LdrpCheckModule` → `CompatCachepLookupCdb` → `NtApphelpCacheControl`.
- PE_emulator 는 근거 없이 syscall 을 약속에 넣지 않는다. 그런데 근거가 될 asm 이 이쪽 (internals) 에 없었다.
- **걱정 (요청서 U1)**: "성공, 출력 안 씀" 으로 대충 대답하면, ntdll 이 **안 쓴 출력 칸 (스택 쓰레기)** 을 읽고 엉뚱한 갈래 (shim 등) 로 갈 수 있지 않나?

### 무엇이 궁금했나

1. `LdrpSendPostSnapNotifications` 가 `LdrpCheckModule` 을 부르는 **조건** — 건너뛰는 갈래가 있나.
2. `LdrpCheckModule` 이 하는 일.
3. `CompatCachepLookupCdb` — 버퍼 꼴, **출력을 미리 비우나 (U1)**.
4. 커널 `NtApphelpCacheControl` 클래스 6 — 무엇을 돌려주나.
5. 노드마다 몇 번 부르나.
6. PE_emulator 가 그대로 쓸 **한 줄 결론**.

### 바퀴 전에 알던 것

- `[정적]` (run21) `LdrpPrepareModuleForExecution +0x76/+0x7b` 가 `LdrpNotifyLoadOfGraph` 를 부른다. 안쪽은 안 봤다.
- `[정적]` (run27) `LdrpNotifyLoadOfGraph +0x19` 가 노드 `State = 7` 을 쓴다 (실패면 `+0x29` 에서 6).
- `[정적]` (run36) `LdrpSnapModule` 도 `CompatCachepLookupCdb` 를 부른다. 단 **못 찾음 오류 처리 안에서만**.
- `[관찰]` (run59 사진) shim 엔진 함수 칸 `g_pfnApphelpCheckModuleProc`·`g_pfnSE_*` 은 끝까지 0 — shim 엔진은 안 올라왔다.
- `[정적]` (sast/51) syscall 번호 `0x4c` = `NtApphelpCacheControl`. 인자 수는 기계 추출로 "4" 라고만 적혀 있었다 (2.6 에서 실제 2개로 바로잡힘).

---

## 2. 찾은 것

### 2.1 한눈에 — 핵심 줄기

먼저 뼈대만. 아래 일곱 칸이 이번 바퀴의 전부다. 2.3 ~ 2.9 에서 한 칸씩 푼다.

```
[ntdll — 유저]
 LdrpPrepareModuleForExecution  (노드 하나)
  └─ +0x7b LdrpNotifyLoadOfGraph            ① 의존 노드 먼저, 그다음 +0x19 자기 State = 7
      └─ +0x25 LdrpSendPostSnapNotifications   ② 노드 안 모듈마다 (조건: g_pShimmedModuleList == 0)
          └─ +0xbb LdrpCheckModule (전체 경로)
              └─ +0x25 CompatCachepLookupCdb (경로, 0x10)    ③ 답칸 0 으로 비우기 → 질문지 쓰기
                  └─ +0xb9 NtApphelpCacheControl (6, 버퍼)    ④ SYSCALL 0x4c
════════════════════════════════════════ 유저 → 커널
[nt — 커널]
 NtApphelpCacheControl                        ⑤ 스스로 답 안 함 — IOCTL 0x22001B 로 넘김
  └─ +0xf7 IofCallDriver ── ? ──►
[ahcache.sys — 커널 드라이버]
 AhcDriverDispatchDeviceControl
  └─ +0x3d AhcDispatch                       ⑥ AhcEnabled 확인
      └─ +0x1d8 AhcApiLookupCdb              ⑥ 카드철에서 이름 찾기 — 못 찾으면 성공 + 출력 0
════════════════════════════════════════ 커널 → 유저
[ntdll]  CompatCachepLookupCdb +0xb9 뒤       ⑦ 음수면 0, 아니면 (출력 & 0x10) 이면 1
```

- 라벨은 ret 자리 (콜스택에 보이는 오프셋) 다. 전부 `[정적]`. 가운데 `?` 하나만 `[추측: 배달]` (2.7).
- 줄기에서 빠진 옆가지 (`SbUpdateSwitchContextBasedOnDll`·`LdrpSendDllNotifications`·`LdrpLogDllState`·shim 간접 호출·`AVrfDllLoadNotification`) 는 2.4 에서 한 줄씩.

비유로 한 번:

- ① 관리실이 연결 카드에 **도장 7 ("들일 준비 끝")** 을 찍는다. 의존하는 카드가 있으면 그쪽부터.
- ② 카드에 묶인 물건 (모듈) 마다 "리콜 확인" 을 한다.
- ③ 질문지를 쓴다. **쓰기 전에 답칸부터 지우개로 깨끗이 지운다.** 물건 이름은 전체 주소가 아니라 **파일 이름만** 적는다.
- ④ 관리사무소 신청 창구 `0x4c` 에 낸다.
- ⑤ 관리사무소는 직접 답하지 않고, 건물 안 **호환성 상담실** 에 주문서를 넣는다.
- ⑥ 상담실은 리콜 목록 카드철을 뒤진다. 없으면 "확인했습니다 (성공), 해당 없음 (0)".
- ⑦ 관리실은 답지의 **"통역사 필요" 칸 (비트 `0x10`)** 만 본다. 체크가 없으면 아무 일도 없다.

### 2.2 한 줄씩 읽는 법

run 문서 "호출 지점" 표에서 두 줄을 골라 끝까지 읽어 본다.

**줄 하나: `ntdll!LdrpCheckModule | +0x20 | +0x25 | ntdll!CompatCachepLookupCdb(전체 경로, 0x10) | 늘`**

- **누가**: `LdrpCheckModule` — 모듈 하나를 "호환성 확인" 하는 함수.
- **어디서**: CALL 명령이 `+0x20`, 그 다음 줄이 `+0x25`. 디버거 콜스택에는 `LdrpCheckModule+0x25` 로 보인다.
- **누구를**: `CompatCachepLookupCdb`.
- **무엇을 넘기나**: 첫째 = 모듈의 **전체 경로** (`FullDllName.Buffer`), 둘째 = **마스크 `0x10`** (`+0x18 MOV EDX,0x10`).
  - 마스크 = "답지에서 내가 볼 칸". 이 호출자는 `0x10` 칸만 본다.
- **조건**: "늘" — 이 함수 안에는 부르기 전에 거르는 조건이 없다 `[정적]`.
- **무엇을 위해**: 이 모듈이 shim 대상인지 묻고, 답이 1 이면 shim 목록에 올리려고 (2.11).

**줄 둘: `ntdll!CompatCachepLookupCdb | +0xb4 | +0xb9 | ntdll!NtApphelpCacheControl (스텁 → syscall 0x4c) | 이름 ≤ 0x7ffe 글자`**

- **누가 / 어디서**: `CompatCachepLookupCdb` 의 `+0xb4` 에서 부르고, 돌아오면 `+0xb9`.
- **누구를**: ntdll 스텁 `NtApphelpCacheControl` → 곧바로 syscall `0x4c` 로 커널에 들어간다.
- **무엇을 넘기나**: `ECX = 6` (`+0xaf`), `RDX = 버퍼 RSP+0x20` (`+0xa2`). 인자는 **2개** 뿐.
- **조건**: 파일 이름이 `0x7ffe` 글자 이하일 때. 더 길면 syscall 없이 0 을 돌려준다 (`+0x77 JA` → `+0xea`).
- **무엇을 위해**: 커널에 "이 파일 이름, 호환성 DB 에 있나요?" 를 묻는다.

### 2.3 ① 언제 묻나 — 도장 7 바로 뒤, 노드마다 한 번

**새 말 — apphelp**: Windows 의 **호환성 도우미** 쪽 이름이다. 옛 프로그램이 새 Windows 에서 문제를 일으키면, 그 프로그램에 shim (호환성 고치기 장치) 을 붙여 준다 `[추측]` (일반적으로 알려진 설명). 비유: 오래된 가전에 붙이는 **변환 어댑터**. 이번에 보는 건 "어댑터가 필요한 물건인가?" 를 묻는 **질문 한 번** 이다.

순서 `[정적]`:

- `LdrpPrepareModuleForExecution` 이 노드 도장을 본다.
  - 5 면 먼저 `+0x12f` 그래프 정리로 6 을 만들고, 6 이면 그대로 → `+0x76/+0x7b LdrpNotifyLoadOfGraph(노드)`.
  - 7 이면 `+0x115` → `+0xbf` 로 **알림 없이** 지나간다. 8·9 는 끝.
- `LdrpNotifyLoadOfGraph` 는 의존 노드부터 본다.
  - 의존 노드 도장이 **6 이면 먼저 재귀** (`+0x61/+0x66`). 7 이상이면 건너뛴다.
  - 그다음 `+0x19` 에서 **자기 도장 = 7**, 바로 `+0x20/+0x25 LdrpSendPostSnapNotifications(자기 노드)`.

왜 "노드마다 한 번" 이라고 하나 — 단계별로:

1. 도장 6 → 7 은 `+0x19` **한 번뿐**이다 `[정적]`.
2. 그리고 알림은 도장 6 인 노드만 들어간다 (7 이상은 건너뜀) `[정적]`.
3. 그래서 → 같은 노드에 알림이 두 번 갈 수 없다.
4. 노드 안 모듈마다 syscall 한 번. 보통 노드 하나에 모듈 하나다 (결과서).

ntdll 은? ntdll 노드는 처음부터 도장이 **9** 다 (결과서 — run43 기록). 그래서 건너뛴다 → **0번** `[추측]` (2.12 사진이 받침).

비유: 연결 카드 묶음을 위에서부터 훑으면서, 도장 6 짜리를 만나면 그 아래 카드부터 처리하고 자기 카드에 7 을 찍은 뒤 리콜 확인. 이미 7 이 찍힌 카드는 다시 안 본다.

### 2.4 ② 누가 묻나 — 조건은 하나, `g_pShimmedModuleList == 0`

`LdrpSendPostSnapNotifications` (ntdll rva `0xc240`, 321B) — 인자 = **노드** (`+0x2a MOV R14,RCX`). 자물쇠 `LdrpDllNotificationLock` 을 잡고 (`+0x4a/+0x4f`), 노드 안 모듈마다 아래 순서로 한다 `[정적]`:

| 순서 | 자리 (ret) | 무엇 | 조건 | test.exe |
|---|---|---|---|---|
| (a 앞) | `+0x135` | `AVrfDllLoadNotification` (켜졌으면 `+0x97 JNZ` → `+0x12d`, 끝나면 a 로) | `PEB.NtGlobalFlag & 0x100` | **안 탐** |
| a | `+0xa8` | `SbUpdateSwitchContextBasedOnDll` | 늘 | 탐 |
| b | `+0x127` | shim 엔진 간접 호출 (푼 `g_pfnSE_DllLoaded`, CFG 거침) | `g_ShimsEnabled != 0` · 푼 값 `!= 0` · `g_ShimsLoading == 0` | **안 탐** |
| c | `+0xbb` | **`LdrpCheckModule`** | **들어갈 때 `g_pShimmedModuleList == 0`** | 탐 |
| d | `+0xc6` | `LdrpSendDllNotifications(모듈, 1)` | 늘 | 탐 |
| e | `+0xdd` | `LdrpLogDllState` (앞 `+0xce` 에서 항목 `Flags \|= 8`) | 늘 | 탐 |

- c 의 조건은 **이것 하나뿐**이다 `[정적]`. 노드 칸·PEB 비트·`ShimData`·`g_pfnApphelpCheckModuleProc` 는 **안 본다**.
- `g_pShimmedModuleList` = "shim 이 필요한 모듈 이름 목록" (2.11). test.exe 에서는 사진 세 장 모두 0 이다 `[관찰]` (2.12). 그러니 c 는 매번 탄다.

#### 짧게 — RSI 갈래는 "건너뛰기" 가 아니다

오케스트레이터가 따로 물었다: `+0xa8 TEST RSI,RSI` · `+0xab JNZ` (→ `+0x113`) 가 `LdrpCheckModule` 을 건너뛰는 길 아닌가?

flow-writer 가 `.asm` 으로 확인한 답 `[정적]`:

1. RSI 는 0 에서 시작한다 (`+0x20 XOR R12D,R12D` → `+0x3d MOV ESI,R12D`).
2. `g_ShimsEnabled` 가 켜졌을 때만 (`+0x4f`·`+0x56 JZ` → `+0x7a`) RSI 에 `g_pfnSE_DllLoaded` 를 읽어 (`+0x64`) KUSD 쿠키 (`[0x7ffe0330]`) 로 **푼다** (`+0x74 ROR` · `+0x77 XOR`). 즉 RSI = shim 엔진 함수 주소, 엔진이 없으면 0.
3. RSI 가 0 이 아니어서 `+0x113` 으로 뛰어도 → 두 갈래 모두 **`+0xad` 로 돌아온다** (`+0x11a JNZ` 또는 간접 호출 뒤 `+0x12b JMP`).
4. `+0xad TEST R15,R15` 가 바로 c 의 조건이다.

그래서 → RSI 갈래는 shim 엔진 호출 (b) 을 c **앞에 끼워 넣는** 갈래일 뿐이다. 결과서의 "조건 하나" 가 맞다.

비유: 줄 서는 길에 "통역사가 와 있으면 먼저 인사" 라는 샛길이 있는데, 샛길로 가도 결국 같은 줄로 돌아온다.

### 2.5 ③ ★ 질문지 쓰기 — 답칸부터 지운다 (`+0x29`·`+0x33`)

`CompatCachepLookupCdb` (ntdll rva `0xc144`, 244B). 인자: `RCX` = 이름, `EDX` = 마스크. 돌려주는 값은 **0 또는 1 뿐**.

**함수의 첫 일이 "지우기" 다** `[정적]` (run 문서 "PE_emulator 쪽에서 쓸 것", 오케스트레이터가 `.asm` 으로 한 번 더 봄):

| 자리 | 명령 | 뜻 |
|---|---|---|
| `+0x1e` | `LEA RCX,[RSP+0x20]` | 지울 곳 = 스택 버퍼 시작 |
| `+0x23` | `MOV R8D,0x188` | 지울 길이 = `0x188` 바이트 |
| `+0x29` (ret `+0x2e`) | `memset` | 버퍼 **통째로 0** |
| `+0x2e` | `XOR EBX,EBX` | `EBX = 0` |
| `+0x33` | `MOV [RSP+0x1d0],EBX` | 버퍼 **밖**에 있는 **출력 DWORD 도 따로 0** |

- 출력 DWORD (`RSP+0x1d0`) 는 `0x188` 블록 밖이라 memset 으로는 안 지워진다. 그래서 `+0x33` 이 **따로** 0 을 쓴다.
- 이 함수 안에서 이 칸을 쓰는 줄은 `+0x33` **하나뿐**이다. 그 뒤로 쓰는 쪽은 커널·드라이버뿐.

비유: 상담실에 보낼 질문지를 쓰기 전에, **답칸에 미리 "0 (해당 없음)" 을 적어 둔다.** 상담실이 아무것도 안 적어 돌려줘도, 답칸엔 쓰레기가 아니라 "0" 이 남아 있다.

그다음 질문지를 채운다 `[정적]`:

| 버퍼 칸 | 크기 | 뜻 | 쓰는 줄 |
|---|---|---|---|
| `+0x0f8` | 2 | 이름 길이 (`UNICODE_STRING.Length`) = 글자 수 × 2 | `+0x7c` |
| `+0x0fa` | 2 | `MaximumLength` = `Length + 2` | `+0x87` |
| `+0x100` | 8 | 이름 시작 주소 — **파일 이름만** | `+0x5c` |
| `+0x178` | 8 | 출력 DWORD 주소 (= `RSP+0x1d0`) | `+0xa7` |
| `+0x180` | 4 | 출력 크기 = 4 | `+0x97` |
| 나머지 | — | 0 | `memset` |

- **파일 이름만**: `+0x3d/+0x42 wcsrchr(이름, '\\')` 로 마지막 `\` 뒤만 쓴다. `\` 가 없으면 이름 그대로 (`+0x48 JZ` → `+0xe2`). 복사는 안 하고 호출자 문자열 안을 가리킨다.
- 부르기 전에 건너뛰는 갈래는 **딱 하나** — 이름이 `0x7ffe` 글자보다 길 때 (`+0x77 JA` → `+0xea`, syscall 없이 0).

### 2.6 ④ 창구 — 스텁 `0x4c`, 인자 2개

ntdll 스텁 `NtApphelpCacheControl` (rva `0x161730`, 24B) `[정적]`:

- `+0x3 MOV EAX,0x4c` → `+0x12 SYSCALL`. (`+0x8 TEST [0x7ffe0308],1` · `+0x10 JNZ` → `+0x15 INT 0x2e` 는 다른 진입 방식 갈래.)
- 인자: `ECX` = 서비스 번호 (클래스) `6`, `RDX` = 버퍼. 스택 인자 0.
- sast/51 표의 "인자 4" 는 기계 추출의 자리 수 상한일 뿐이고, 커널 쪽이 실제로 쓰는 건 `ECX` (`+0x35 CMP ECX,0xc`) 와 `RDX` (`+0x26 MOV RSI,RDX`) 둘이다 (결과서).

### 2.7 ⑤ 관리사무소는 스스로 답하지 않는다 — IOCTL 로 넘김

**새 말 — 드라이버**: 커널 공간에 따로 올라오는 모듈이다. nt 본체 밖에 있지만 같은 커널 공간에서 돈다 `[추측]` (일반적으로 알려진 설명). 비유: 관리사무소 건물 안에 입주한 **전문 상담실**. 이번 상담실 이름이 `ahcache.sys` (`System32\drivers\ahcache.sys`) 다.

**새 말 — IOCTL**: 드라이버에 "이 일 해 주세요" 하고 보내는 **번호 붙은 주문서** 다. 번호 (코드) 가 일의 종류를 가른다 `[추측]` (일반적으로 알려진 설명). 이번 주문서 번호는 `0x22001B`.

**새 말 — IRP**: 그 주문서를 담아 드라이버에게 건네는 커널의 표준 **배달 봉투** `[추측]` (일반적으로 알려진 설명). `IofCallDriver` 가 이 봉투를 드라이버에 넘긴다.

`nt!NtApphelpCacheControl` (rva `0x9f46c0`) 클래스 6 길 `[정적]`:

1. 기본 답을 먼저 정해 둔다: `+0x79 MOV EBX,0xC0000225` (`STATUS_NOT_FOUND`).
2. 상담실 문 찾기: 전역 `g_AhcDeviceObject` 가 0 이면 (`+0x7e`) `\Device\ahcache` 를 연다 (`+0x139/+0x13e IoGetDeviceObjectPointer`) → `+0x148` 전역에 저장. 한 번 잡으면 다음부터는 안 연다.
   - 문을 못 찾으면 → 기본 답 `0xC0000225` 를 그대로 돌려준다.
3. 주문서 만들기: `+0xcf/+0xd4 IoBuildDeviceIoControlRequest`.
   - 주문서 번호 = `클래스 × 4 | 0x220003` (`+0x42` · `+0x4d`) → 클래스 6 은 **`0x22001B`**.
   - 입력 = **유저 버퍼 주소 그대로**, 길이는 **늘 `0x188`** (`+0xab`). 사용자가 준 크기를 안 본다.
   - 출력 칸은 따로 없다 (출력 = 버퍼 안 `+0x178` 이 가리키는 DWORD, 드라이버가 직접 씀).
   - 봉투를 못 만들면 `0xC0000001`.
4. 보내기: `+0xf2/+0xf7 IofCallDriver`. **돌아온 값이 곧 syscall 반환** (`+0xf7 MOV EBX,EAX`).

그래서 → 클래스 6 의 대답은 nt 가 아니라 **ahcache.sys 가 정한다**. nt 안 `Ahc*` 함수는 `AhcCacheQueryHwId` 하나뿐인데 클래스 9 전용이다 `[정적]`. 이 때문에 이번에 ahcache.sys 를 **새로 analyze** 했다 (376 KB, PDB GUID `D54D2D17350903291E74D1E13B85E1831`).

#### `?` 하나 — 봉투가 어떻게 상담실 함수에 닿나

- `[정적]` `DriverEntry +0xd3` · `+0xda` 가 드라이버 객체의 `MajorFunction[14]` 칸 (`_DRIVER_OBJECT +0x70 + 14×8 = +0xe0`) 에 `AhcDriverDispatchDeviceControl` 을 넣는다.
- `[추측]` `IofCallDriver` 안에서 그 칸을 간접 호출해 배달한다 — `IofCallDriver` 본문은 안 봤다. 14 가 `IRP_MJ_DEVICE_CONTROL` 이라는 이름도 `[추측]` (PDB 열거를 안 봄).
- 그래서 flow.md 그림에 `?` 화살표로 남았다.

비유: 상담실이 문 여는 날 접수대 14번 칸에 "주문서 담당 = 나" 라고 이름표를 붙여 둔 건 봤다. 관리사무소 배달원이 실제로 그 칸을 보고 배달하는 장면은 아직 못 봤다.

### 2.8 ⑥ 상담실의 답 — 못 찾으면 "성공 + 0"

**새 말 — 호환성 DB**: "이 파일 이름에는 이런 호환성 처리가 필요하다" 는 목록이다. ahcache 는 이걸 부팅 때 레지스트리 `CacheMainSdb` 값과 sysmain.sdb 에서 읽어 둔다 (결과서 — 내용은 `[공백]`, 시점은 `[추측]`).

**새 말 — AVL 표**: 항목을 정렬해서 넣어 두는 트리 모양 표라서, 이름으로 빨리 찾을 수 있다 `[추측]` (일반적으로 알려진 설명). 비유: 상담실 서랍 속 **가나다순 리콜 카드철**. 이번 표는 `AhcCdbHandle` 이 가리키는 DB 개체 `+0x8` 에 있다 (짐 지도 7.6.6).

`ahcache.sys` 안 길 `[정적]`:

1. `AhcDriverDispatchDeviceControl`: 주문서 번호에서 클래스를 꺼낸다 (`(코드 >> 2) & 0xff`). `0xc` 이상이면 거절. → `+0x38/+0x3d AhcDispatch`.
2. `AhcDispatch`:
   - 유저가 보낸 주문서면 4바이트 정렬·주소 범위를 검사하고, **`0x188` 바이트를 자기 스택 사본으로 복사**한다. 이후는 사본만 쓴다.
   - 클래스 6 이면 **`AhcEnabled` 가 0 인지** 본다 (`+0x1ae` · `+0x1b5`). 0 이면 → `+0x121` → `0xC0000225`.
   - 아니면 `+0x1d3/+0x1d8 AhcApiLookupCdb(이름, 출력 주소, 4)`.
3. `AhcApiLookupCdb`:
   - 출력 주소가 0 이거나 크기가 4 가 아니면 거절. 이름이 비면 거절.
   - 카드철에서 이름을 찾는다 — **대소문자 무시** (`+0x24d/+0x254 RtlLookupElementGenericTableAvl`).
   - **찾으면**: 카드의 플래그 DWORD 가 출력.
   - **못 찾으면** — 여기가 핵심:

```
+0x262          내부 상태 = 0xC0000225 (못 찾음)
+0x292 ~ +0x296 출력 값 = 0
+0x2b0          출력 DWORD 에 0 을 씀
+0x2b4 JMP → +0x2dc XOR EDI,EDI   반환 상태를 0 (성공) 으로 바꿈
```

- 그래서 → **못 찾음 = `STATUS_SUCCESS` + 출력 0** `[정적]`. "없어요" 를 오류로 안 돌려주고 "확인했어요, 해당 없음" 으로 돌려준다.
- 자물쇠 단계에서 내부 오류가 나도 같은 마무리로 합류해 **성공 + 출력 0** 이 된다 (결과서 표 5번 줄).

`AhcEnabled` 는 켜져 있을까? `[정적]` `AhcInitialize +0x23c` 가 `EDI` 를 쓰는데, `EDI` 는 `+0x152 MOV EDI,1` 뒤로 이 함수 안에서 안 바뀐다 → 1. 단 앞 단계 (`AhcLockCreate`·`AhcCacheCreate`·`AhcCdbCreate`·`AhcSdbQueryCreate`·`CitmInitialize`) 가 다 성공해야 거기 닿는다. 실제 VM 값은 동적 몫이다 (4절).

### 2.9 ⑦ 돌아와서 읽기 — 두 단계

`CompatCachepLookupCdb` 의 `+0xb9` 이후 `[정적]`:

| 자리 | 명령 | 뜻 |
|---|---|---|
| `+0xb9` · `+0xbb` | `TEST EAX,EAX` · `JS` (→ `+0xc6`) | 상태가 **음수 (실패)** 면 → **0 반환** |
| `+0xbd` · `+0xc4` | `TEST [RSP+0x1d0],ESI` · `JNZ` (→ `+0xee`) | `(출력 DWORD & 마스크) != 0` 일 때만 → **1 반환** |
| `+0xc6` | `MOV EAX,EBX` (`EBX = 0`) | 그 밖은 → **0 반환** |

- 상태값은 위로 안 넘긴다. 호출자가 받는 건 0 / 1 뿐.

### 2.10 ★ PE_emulator 의 기본 대답은 왜 안전한가

PE_emulator 의 기본 대답 = "syscall 은 성공 (0), 출력 칸은 안 건드림". 걱정은 "안 건드린 출력 칸에 쓰레기가 있으면?" 이었다.

단계별로:

1. **근거 1** `[정적]`: syscall **전에** 출력 DWORD 를 0 으로 쓴다 (`+0x33`, `EBX = 0` from `+0x2e`). 버퍼 `0x188` 도 0 (`+0x29`).
2. **근거 2** `[정적]`: 이 함수 안에서 그 칸을 쓰는 줄은 `+0x33` 하나뿐이다.
3. 그래서 → PE_emulator 가 출력 칸을 안 건드리면, 돌아왔을 때 그 칸엔 **0** 이 남아 있다.
4. **근거 3** `[정적]`: 돌아와서는 `(출력 & 0x10) != 0` 일 때만 1 이다 (`+0xbd` · `+0xc4`).
5. 그래서 → 0 & `0x10` = 0 → **반환 0** → `LdrpCheckModule` 은 아무것도 안 한다.
6. 상태를 실패로 줘도 → `+0xbb JS` 로 바로 0. 결과는 같다.

정리하면 (run 문서 "커널·드라이버가 주는 상태값" 을 ntdll 쪽 결과로 묶음):

| PE_emulator 가 주는 대답 | 출력 칸 | ntdll 반환 | 뒤에 생기는 일 |
|---|---|---|---|
| `0` (성공), 출력 안 씀 | 0 (`+0x33` 이 미리 씀) | 0 | 없음 |
| `0` (성공), 출력 0 씀 | 0 | 0 | 없음 — **진짜 "못 찾음" 과 똑같다** |
| `0xC0000225` 등 실패 | (안 봄) | 0 | 없음 |
| `0` (성공), 출력에 **비트 `0x10`** | `0x10` 포함 | **1** | **shim 길 입구** (2.11) |

- 그러니 **"스택 쓰레기를 읽는 길" 은 없다** — U1 걱정 닫힘 `[정적]`.
- 진짜 커널·드라이버도 shim 대상이 아닌 모듈엔 "성공 + 출력 0" 을 준다 (2.8). 그래서 **0 이 가장 진짜에 가깝다**.
- PE_emulator 약속에 넣을 꼴 (run 문서 그대로): `NtApphelpCacheControl(ECX = 6, RDX = 버퍼)`, 인자 2개, 스택 인자 0. 원하면 `*(DWORD*)*(QWORD*)(버퍼+0x178) = 0` (안 써도 됨).

비유: 관리실이 답칸에 미리 "0" 을 적어서 보냈다. 상담실 흉내를 내는 쪽이 아무것도 안 적어 돌려줘도 "해당 없음" 으로 읽힌다. **딱 하나, "통역사 필요" 칸에 체크만 하지 않으면 된다.**

### 2.11 ★ 지켜야 할 것 — 출력 비트 `0x10`

비트 `0x10` 이 켜지면 무슨 일이 생기나 `[정적]`:

1. `CompatCachepLookupCdb` 가 1 을 돌려준다.
2. `LdrpCheckModule` 이 (`+0x28` · `+0x2a` 에서 0 이 아니면) 그 모듈의 **전체 경로**를 NUL 로 이은 목록 `g_pShimmedModuleList` 에 붙인다.
   - 창고에서 상자를 꺼낸다 (`+0x7e/+0x83 RtlAllocateHeap`) → `+0xb0` 목록 주소, `+0xb7` 길이 `g_pShimmedModuleListLength`.
3. 바로 뒤 `LdrpPrepareModuleForExecution +0x88/+0x8d LdrpDynamicShimModule` 이 목록을 본다.
   - 목록이 있으면 `+0x36 JNZ` → `+0x9d/+0xa2 LdrpGetProcApphelpCheckModule` — **apphelp 를 올리는 shim 길** 로 들어간다 (본문 범위 밖).
   - 목록이 0 이면 상태 DWORD (ntdll rva `0x1cd7cc`) 에 **읽은 값을 그대로 다시 쓰고** (`+0x7b`) 끝.

그래서 → **비트 `0x10` 하나가 shim 길로 가는 열쇠**다. 요청서가 걱정한 "엉뚱한 shim 갈래" 의 입구가 정확히 여기다.

- 같은 함수를 다른 호출자는 **다른 칸**으로 본다 (마스크가 다름) `[정적]`:

| 호출자 | 마스크 | 어디서 |
|---|---|---|
| `LdrpCheckModule` | `0x10` | 이번 길 |
| `LdrpSnapModule` ×2 | `0x80` | 못 찾음 오류 처리 안 (run36) |
| `LdrpLogDeprecatedDllEtwEvent` | `0x4` | ETW 기록 |
| `LdrpCheckComponentOnDemandEtwEvent` | `0x100` | ETW 기록 |

  - 그러니 PE_emulator 가 나중에 다른 호출자를 만나도 "출력 0" 이면 다 0 이다. 0 이 아닌 값을 줄 때만 어느 칸을 켜는지 조심하면 된다.

### 2.12 사진과 맞춰 보기 — "5번" 은 왜 `[추측]` 이고, 왜 강한가

정적 추정: syscall **5번** (exe · dll1 · dll2 · KERNEL32 · KERNELBASE), ntdll 0번. 근거 사슬:

1. `[정적]` `LdrpSendPostSnapNotifications` 는 모듈마다 `+0xce OR [RBX+0x68],8` — 모듈 항목 `Flags.LoadNotificationsSent` (비트 3) 를 켠다.
2. `[관찰]` run44 사진 P1b 에서 그 비트:
   - test.exe · KERNEL32 · KERNELBASE · dll1 · dll2 = `0y1`, **ntdll = `0y0`** (`ms/logs/04_baggage-read_P1b.txt:3392` · `3602` · `3707` · `3812` · `3917` · `3497`).
3. 그래서 → 이 함수의 모듈 몸통을 탄 모듈이 정확히 다섯이다. 모듈 몸통을 타면 2.4 의 c 를 지나 syscall 한 번.
4. **아직 확인 아님**: 이 비트를 켜는 곳이 `+0xce` **하나뿐인지** 는 안 봤다. 다른 곳에서도 켠다면 "비트 1 = 이 함수를 탔다" 가 깨진다. → 그래서 5번은 `[추측]` (강한 받침). → **Q127 (a)**.

같은 사진에서 함께 맞은 것 `[관찰]`:

| 무엇 | 사진 값 | 무엇과 맞나 |
|---|---|---|
| `Flags.ShimEngineCalloutSent` (비트 11, `+0x127` 이 켬) | 여섯 모듈 모두 `0y0` | shim 간접 호출 (2.4 b) 안 탐 |
| `g_pShimmedModuleList` (`ntdll+0x1d23d8`) | P1a·P1b·P2 모두 0 | 2.4 c 를 매번 탐 |
| `g_ShimsEnabled` (`0x1d23cc`)·`g_ShimsLoading` (`0x1d23cf`) | 0 | shim 엔진 없음 |
| `0x1cd7cc` (`LdrpDynamicShimModule` 상태 DWORD) | P1a·P1b·P2 모두 **1** | 파일 처음 값 1 · `+0x7b` 가 같은 값을 다시 씀 |
| `PEB.NtGlobalFlag` | P1a·P1b·P2 모두 0 | 2.4 "(a 앞)" 줄 (`& 0x100`) 안 탐 |
| 노드 `State` | exe·dll1·dll2 P1b 7 → P2 9, ntdll·k32·kb 9 | `+0x19` 가 7 을 씀 |

- 사진 줄 번호는 run 문서 "사진과 맞춰 본 것" 에 다 있다.
- 짐 지도 7.5 가 `g_pShimmedModuleList` 를 `[추측]` 으로 두었던 것도 맞아서 `(해결됨)` 이 됐다 (정정은 아님).

### 2.13 그 밖에 보탠 것 (결과서보다 한 걸음 더)

- `[정적]` `LdrpDynamicShimModule` 은 목록이 0 인 정상 길에서도 상태 DWORD 에 같은 값을 다시 쓴다 (`+0x18` 읽기 → `+0x7b` 쓰기). 0 으로 바꾸는 건 목록이 있을 때 `+0x96` 에서만.
- `[정적]` `g_ShimsEnabled` 셋째 쓰기 `LdrpInitializeProcess +0x1ceb` 는 **0 을 쓴다** (`+0x1ce4 XOR R15D,R15D`). 조건은 `g_ShimsEnabled != 0` 이고 shim 후처리 (`g_pfnSE_InstallAfterInit`) 가 `AL == 0` 을 줄 때. test.exe 는 안 탄다.
- `[정적]` `AhcEnabled` 의 값 1 출처 줄 `AhcInitialize +0x152` (2.8), `MajorFunction[14]` 를 채우는 줄 `DriverEntry +0xda` (2.7).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 곳

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | pdb 목록에 run65 까지 | ntkrnlmp·ntdll 목록에 **run66**, 새 줄 **`ahcache.pdb`** (`D54D2D17350903291E74D1E13B85E1831`) · 정적 바퀴 한 줄 (run44 로그 다시 읽음, 새 실행 없음) |
| 그림 2 읽는 법 | 모듈 상자 7개 | + **`ahcache.sys`** (커널 공간이라 `nt` 와 같은 분홍, 이름으로 가름). `nt` 안 단계 상자에 **`(3) apphelp 캐시 묻기`** |
| 그림 2 `(1) kernel32 준비` | `LdrpPrepareModuleForExecution` → `+0xef` 켜기 하나 | `(1) +0x7b LdrpNotifyLoadOfGraph` [KERNEL32 추정] → `(1) +0x66` 재귀 [KERNELBASE 추정] / `(2) +0x25 LdrpSendPostSnapNotifications` → `LdrpCheckModule` → `CompatCachepLookupCdb` → 스텁 `NtApphelpCacheControl` ==`SYSCALL 0x4c`==> 커널 → `?` → `ahcache.sys` 세 상자. `(2) +0x8d LdrpDynamicShimModule`. 켜기는 **`(3) +0xef`** 로 번호만 바뀜. 모두 `(정적)` 상자 |
| 그림 2 `(1)~(2) 사이` (exe) | `+0x12f` 그래프 정리 | `(1) +0x12f` + **`(2) +0x7b LdrpNotifyLoadOfGraph` [test.exe 추정]** 상자 하나 (안쪽은 kernel32 쪽과 같은 코드라 안 그림) |
| 2.5 트리 | `+0x7b` 줄 | + 안쪽 사슬 한 줄 `[정적]` |
| 2.5 **새 절** | — | "올린 뒤 알림과 apphelp 캐시 묻기" — 트리 + `[정적]` 줄들 (2.3 ~ 2.11 의 내용) + 사진 대조 `[관찰]` + `?` 의 `[추측]` |
| `LdrpInitializeGraphRecurse` 절의 Prepare 줄 | — | `+0x7b` 안쪽·`+0x8d` 한 줄 |
| 4장 | Q125 대기 | 해결됨 줄 + 새 줄 셋 (동적 → Q104, 정적 셋 → Q127, 결과서와 같음·보탠 것) |
| 4.1 표 | — | 새 `(정적)` 상자 두 묶음 (kernel32 준비 쪽 · exe 쪽). 재귀 ↔ 모듈 대응과 dll2 → dll1 → exe 순서는 `[추측]` |

### 짐 지도 (`baggage.md`) 에서 바뀐 곳

- 모듈 항목 (3.5): `Flags.LoadNotificationsSent`·`ShimEngineCalloutSent` — 모르던 칸 2 닫힘.
- 3.6 `State = 7` 줄의 경로를 고침.
- `.data` (3.8): shim 전역 넷 (`g_pShimmedModuleList`·`g_pShimmedModuleListLength`·`g_ShimsEnabled`·`g_ShimsLoading`) 닫힘 + 목록 밖 자리 `0x1cd7cc` 2줄.
- `.mrdata` (3.9): `g_pfnApphelpCheckModuleProc`·`g_pfnSE_DllLoaded` 닫힘.
- 숫자 (짐 지도 머리 그대로): 구조체 아는 칸 721 → 723, 전역 아는 것 159 → 165 · 모름 334 → 328.
- 새 **7.6.6**: 커널 쪽 짐 — ahcache 전역 (`AhcEnabled`·`AhcCdbEnabled`·`AhcCdbHandle`·장치·AVL 표 원소) 과 nt `g_AhcDeviceObject`. **체크리스트 밖** (다른 드라이버, 부팅 때 놓임 `[추측]`).
  - 에뮬레이터 쪽에선 이 짐 전부가 **`NtApphelpCacheControl(6)` 대답 하나 (상태 0, 출력 0)** 로 줄어든다.
  - 유저 쪽 `0x188` 버퍼는 함수 스택 지역이라 짐이 아니다 (표시만).
- 새 **7.25**: 이번에 넘긴 동적 질문.

### 큰 그림과 이어지는 곳

- **PE_emulator 의 kernel32 올리기 (B) 가 막힌 벽이 풀렸다.** `Service == 6` 이면 `STATUS_SUCCESS`, 출력 안 써도 됨 — 근거 줄과 함께 약속에 넣을 수 있다.
- 같은 벽이 exe import 길 (`LdrpInitializeProcess +0x1af2`) 에도 있었는데, 같은 코드라 같은 답으로 넘어간다.
- 이 알림 다음은 `LdrpPrepareModuleForExecution` 의 `+0x8d` (shim 목록이 0 이라 바로 끝) → `+0xef` 켜기 (run21 · run25) 로 이어진다.
- 지도에 처음으로 **nt 밖의 커널 모듈** 이 들어왔다. "커널 = nt 하나" 가 아니라, syscall 하나가 드라이버까지 건너갈 수 있다는 걸 그림에 남겼다.
- 다음 바퀴는 **Q104 VM 단계** — 이번 동적 몫이 거기 얹힌다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| ahcache 가 VM 에 올라와 있고 `AhcEnabled` 가 1 인가, `AhcCdbHandle`·`g_AhcDeviceObject` 값 | 정적으로는 "성공하면 1" 까지만. 실제 값은 부팅 뒤 메모리 | 동적 1 → **Q104 VM 단계** (`lm m ahcache` · `dd ahcache!AhcEnabled L1` · `dq ahcache!AhcCdbHandle L1` · 덤 `dq nt!g_AhcDeviceObject L1`) |
| `CompatCachepLookupCdb` 히트 수 (추정 5), 묻는 이름, 마스크 | 몇 번 부르는지는 실행 때 정해진다 | 동적 2 → **Q104** (입구에서 `du @rcx` · `r edx`) |
| 돌아온 상태·출력 실제 값 (추정 둘 다 0), 반환 1 인 모듈이 있나 | 같음 | 동적 3 → **Q104** (`+0xb9` 에서 `r eax` · `dd @rsp+1d0 L1`) |
| **DB 에 그 이름이 있나, 플래그 `0x10` 이 켜졌나** | DB 내용은 sysmain.sdb·레지스트리에 달려 정적으로 안 정해진다 (`[공백]`). 결과서 짐작은 "보통 0" `[추측]` | 동적 4 → **Q104** (`ahcache!AhcApiLookupCdb+0x254` `r rax`, 찾았으면 `dd poi(@rax+0x28) L1`, `+0x2b0` `r esi`) |
| 노드 순서 (kernel32 길 KERNELBASE → KERNEL32, exe 길 dll2 → dll1 → exe) | 재귀 ↔ 모듈 대응은 `[추측]` | 동적 7 → **Q104** (`ntdll!LdrpNotifyLoadOfGraph+0x19` 히트마다 노드 첫 모듈 이름) |
| `LoadNotificationsSent` 를 켜는 다른 줄이 있나 | 구조체 비트라 `_refs` 로 안 잡힌다 | 정적 → **Q127 (a)** — "5번" `[추측]` 을 닫는 데 필요 |
| shim 엔진을 올리는 길의 조건 (`LdrpInitShimEngine` → `LdrpLoadShimEngine` → `LdrpGetShimEngineInterface`) | test.exe 에서 안 탄다는 건 사진 (`g_pfnSE_*` 끝까지 0) 으로만 안다 | 정적 → **Q127 (b)** |
| `SbUpdateSwitchContextBasedOnDll` 이 모듈 항목 `SwitchBackContext` 를 쓰나 | 줄기 밖 옆가지라 본문을 안 봤다. 사진상 같은 다섯 모듈에 값이 있다 (짐 지도 7.2) | 정적 → **Q127 (c)** |
| `IofCallDriver` → `AhcDriverDispatchDeviceControl` 배달 (그림의 `?`) | `IofCallDriver` 본문을 안 봤다. 14 = `IRP_MJ_DEVICE_CONTROL` 도 `[추측]` | 따로 대기열 줄 없음 (flow.md 4.1 `?`). 동적 4 에서 `AhcApiLookupCdb` 에 멈추면 콜스택으로 같이 보인다 |
| 드라이버가 언제 올라오나 (부팅 때?) | 드라이버 시작 시점은 안 봤다 → `[추측]` | 대기열 줄 없음 (짐 지도 7.6.6 단계 `Ⓑ` `[추측]`) |

- 선택 (사진으로 이미 답함): `g_pShimmedModuleList` 를 은퇴 직전에 (P2 ~ `NtContinue` 사이만 남음), `NtGlobalFlag` (P1a·P1b·P2 모두 0).
- 커널 쪽 `ahcache!` 멈춤점은 **모든 프로세스에 걸린다**. 실행 안내 때 test.exe 조건이 꼭 필요하다 (run 문서).

---

## 5. 더 읽기

- run 문서: [`run66-apphelp-cache.md`](run66-apphelp-cache.md) — "PE_emulator 쪽에서 쓸 것" (한 줄 결론 · syscall 표 · 버퍼 꼴 · 상태값 표), 호출 지점 표, 호출 관계 트리, 사진과 맞춰 본 것, 결과서와 같음/다르게 적은 곳
- 결과서: [`_workspace/sast/61_apphelp-cache_result.md`](../../_workspace/sast/61_apphelp-cache_result.md) — 질문 1 ~ 6, 실패 상태값 표, **클래스 0 ~ 11 핸들러 표**, `AhcApiLookupCdb` 순서 표, "캐시가 부팅 때 채워지나"
- 요청서: [`_workspace/sast/61_apphelp-cache.md`](../../_workspace/sast/61_apphelp-cache.md) — U1 걱정 원문
- flow.md: [2.5 "올린 뒤 알림과 apphelp 캐시 묻기"](flow.md) · 그림 2 `(1) kernel32 준비` · `T2AHC` 상자 · 4장 run66 줄 · 4.1
- 짐 지도: [`baggage.md`](baggage.md) 3.5 · 3.6 · 3.8 · 3.9 · 7.5 · **7.6.6** · **7.25**
- 앞선 해설: [run21](../02_sprint/run21-load-dll-internal-해설.md) (진행 도장 · `Prepare` 순서) · [run27](../02_sprint/run27-init-graph-해설.md) (`+0x19` 도장 7) · [run36](../02_sprint/run36-snap-module-해설.md) (`LdrpSnapModule` 의 마스크 `0x80` 호출) · [run19](../02_sprint/run19-proc-init-modules-해설.md) (shim · 쿠키 풀기) · [run44](run44-baggage-snap-해설.md) (사진)
