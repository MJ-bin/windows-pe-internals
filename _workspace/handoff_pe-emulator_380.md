# PE_emulator 에 넘길 요약 — 380 묶음 (374 둘 · 380 ② ③ ⑥ 덤 · 차례 ④) 의 답

- 쓴 날: 2026-11-27 (windows-pe-internals flow-writer, 오케스트레이터 부탁)
- 이 파일은 **길잡이**다. 근거 · 오프셋 · 표는 run 문서의 "PE_emulator 쪽" 절에 있다 (아래 경로). **이 파일과 run 문서가 다르면 run 문서가 맞다.** 결과서와 run 문서가 다른 오프셋은 run 문서 "flow-writer 대조" 절 (와 맨 위 "… 때 대조" 줄) 이 고친 값이다.
- 표시: `[정적]` 디컴파일 코드 · `[관찰]` 디버거 · `[추측]` · `[공백]` · `[규칙]` 공개 규칙 · `[추측: 계산]` 정적 글 + 규칙으로 계산한 값. "지금 우리" = PE_emulator. 여덟 run 모두 **정적만** — VM 확인거리는 아래 "남은 것" 의 대기열 번호, 디버거 없이 떠 올 것은 맨 끝 표.
- 에뮬레이터 로그 값 (`r19 … seq N`, rip 등) 은 run 문서에서도 `[관찰]` 이 아니다 ("에뮬레이터 로그와 맞음" 으로만 씀).
- 차례: 원문 D179 차례 ① → ② → ③ → ④ → ⑤ → ⑥ → 덤. run 번호 차례와는 다르다 (run150 이 ③).

| 차례 | 요청 (원문 `../PE_emulator/_workspace/05_tuning_planner/`) | 우리 Q | 답한 run (최종 근거) | 정적 결과서 | 해설 |
|---|---|---|---|---|---|
| ① | `374_internals-wow64-csr.md` | Q287 | `runs/03_sprint/run152-wow64-csr.md` | `_workspace/sast/147_wow64-csr_result.md` | `runs/03_sprint/run152-wow64-csr-해설.md` |
| ② | `380_internals-r19-lanes.md` ② (+ Q284 ① ~ ③) | Q288 + Q284a | `runs/03_sprint/run151-rpcss-connect-sd.md` | `_workspace/sast/148_rpcss-connect-sd_result.md` | `runs/03_sprint/run151-rpcss-connect-sd-해설.md` |
| ③ | `380_internals-r19-lanes.md` ③ | Q289 | `runs/03_sprint/run150-epm-ndr-procs.md` | `_workspace/sast/149_epm-ndr-procs_result.md` | `runs/03_sprint/run150-epm-ndr-procs-해설.md` |
| ④ | `380_internals-r19-lanes.md` 머리 "차례" ④ — Q278b (367 gdi-display-dc 의 "다음" 몫) | Q278b | `runs/03_sprint/run153-gdi-font-dc.md` | `_workspace/sast/150_gdi-font-dc_result.md` | `runs/03_sprint/run153-gdi-font-dc-해설.md` |
| ④ | 같은 ④ — Q282 (365 dialog-window 뒷몫, run148 남은 정적) | Q282 | `runs/03_sprint/run157-dialog-win32k.md` | `_workspace/sast/151_dialog-win32k_result.md` | `runs/03_sprint/run157-dialog-win32k-해설.md` |
| ⑤ | `374_internals-appinfo-runas.md` (+ Q276 ①) | Q290 + Q276a | `runs/03_sprint/run156-appinfo-runas.md` | `_workspace/sast/152_appinfo-runas_result.md` | `runs/03_sprint/run156-appinfo-runas-해설.md` |
| ⑥ | `380_internals-r19-lanes.md` ⑥ | Q291 | `runs/03_sprint/run154-dbgk-events.md` | `_workspace/sast/153_dbgk-events_result.md` | `runs/03_sprint/run154-dbgk-events-해설.md` |
| 덤 | `380_internals-r19-lanes.md` 덤 1 ~ 3 | Q292 | `runs/03_sprint/run155-rpc-extras.md` | `_workspace/sast/154_rpc-extras_result.md` | `runs/03_sprint/run155-rpc-extras-해설.md` |

- 이번에 앞 run 정정 · 고쳐 읽기가 생긴 곳: run137 · run138 · run142 · run143 · run144 · run145 · run146 · run147 · run148 · run149 · run113 (+ run129 · run151 · run152). 앞 문서는 원문을 두고 **맨 위에 한 줄** 을 넣었다 → 그 아래 줄 번호가 밀렸다. 절마다 "앞 답 고쳐 읽기" 에 밀린 줄을 적었다.

---

## ① Q287 — WoW64 (x86) 프로세스의 32비트 csrss 첫 연결 (374 wow64-csr) — run152

### 답

- **32비트 ntdll 에는 `CsrpConnectToServer` 가 없다** `[정적]`. 32비트 `CsrClientConnectToServer` (62 B) 는 syscall **하나** `NtWow64CsrClientConnectToServer` (칸 `0x1e9`) 만 낸다.
- 그 몸 wow64 `whNtWow64CsrClientConnectToServer +0x1c` 가 **64비트 ntdll `CsrClientConnectToServer` 를 import 로 통째 부른다** → run68 의 syscall 여섯이 **같은 순서 · 같은 인자** 로 64비트 ntdll 에서 나온다. `whNtOpenSection` · `whNtCreateSection` · `whNtConnectPort` · `whNtMapViewOfSection` 은 **이 길에 안 낀다.**
- 연결 정보 블록 = 64비트 꼴 `0x30` B. 32비트가 넘기는 건 사용자 정보 8 B `{CtrlRoutine32, 0}` 하나.
- 연결 뒤 wow64 가 **상태를 안 보고** PEB32 `+0x4c` ← PEB64 `+0x88` (하위 4 B) · `+0x54` ← `+0x98` (하위 4 B) · `+0x248` ← `+0x380` (8 B) 를 옮긴다 (`+0x63` · `+0x7b` · `+0x8e`). 32비트 `Csr*` 전역은 `_CsrClientProcess` 하나.
- 32비트 kernelbase: `BaseStaticServerData = [[PEB32+0x54]+8] − [PEB32+0x248] + [PEB32+0x4c]` (32비트 셈). BSSD 칸은 64비트와 같은 오프셋, 폭만 반.
- FALSE 가지: 안쪽 다섯 (`+0x1bc` · **`+0x20e` 연결** · `+0x3bc` NLS · `+0x3da` · `+0x3f4` 콘솔) + `entry` 둘 + `PcwDllMain` 하나. 64비트 `CsrClientConnectToServer` 의 `0xc000000d` (블록은 있는데 크기 0) 가지도 있다 — kernelbase32 는 크기 8 을 줘서 안 탄다 (run153 때 보탬).
- **원문 막힘의 뿌리**: `NtOpenSection(SharedSection) = 0xc000003a` → **F2 (`+0x20e JS`)** → `entry` 0 → ntdll32 `_LdrpInitializeNode@4 +0x247` `0xc0000142` → `NtRaiseHardError(0xc0000145)`.
- **4GB**: 공유 섹션 뷰 `R` 은 4GB 아래여야 한다 (32비트가 하위 4 B 로 읽음). 사용자 코드엔 요구가 없고 (`ZeroBits = 0`) 커널 `HighestUserAddress` 상한 (run139) 에만 기댄다.

### 지금 우리 맞나 · 틀리나

- **원문 "이미 아는 것" 에서 틀린 점 셋**: ① "wow64 가 64비트 syscall 로 바꿈" 은 틀린 그림 — 로그의 `NtOpenSection` 은 64비트 ntdll `CsrpConnectToServer` 안에서 나온 진짜 64비트 syscall ② 32비트 `CsrpConnectToServer` 는 없음 ③ `ms/decomp/ntdll32` 의 `Csr*` 는 원래 그 정도뿐.
- `Src/ntdll/BaseDllInit.h:30 ~ :31` (엔진이 결과만 씀) — **WoW64 엔 그대로 못 쓴다**: 진짜 syscall 여섯이 `TeamSyscall` 로 들어온다.
- **`MapCsrSharedSectionView` · `MapCsrPortView` (`Src/nt/AddressSpace.cpp:1421 ~ 1450`)** — 위 끝이 `SYSTEM_MAXIMUM_USER_MODE_ADDRESS` → **WoW64 면 4GB 아래로** (`refProcess.qwHighestUserAddress − 1`, 같은 파일 `:739` 처럼).
- `NtConnectPort` (`layer.cpp:322` · `alpc/nt_legacy_port_messages.cpp:124 ~ 195`) — **이번에 필요한 대답이 없다** (응답 블록 `+0x00` C · `+0x08` T · `+0x10` PID · `+0x24` 0, `PORT_VIEW` `ViewBase` · `ViewRemoteBase`).
- `csr_user_connect_stub.cpp` — USERSRV 연결 (서버 3) 만 대답 → **이번 길 (BASESRV, 서버 1) 이 아니다.**
- **stub 최소 목록 1 ~ 6** (`NtOpenSection` → `NtCreateSection` → `NtConnectPort` → `NtClose` → `NtMapViewOfSection` (`R` < 4GB) → `NtClose`) · 뷰 안 칸 · 먼저 있어야 하는 칸 (TEB64 `+0x180c` = `0x2000` 등) → run152 "PE_emulator 쪽".
- **길 A (진짜 코드 그대로, 표 기준) / 길 B (`0x1e9` 를 가로채 결과만 씀)** — 고르지 않음. 사용자 · PE_emulator 팀 결정.
- 그다음 32비트 쪽이 바라는 것: NLS (`NtGetNlsSectionPtr` · `NtInitializeNlsFiles`) · 콘솔 · `NtQueryInformationProcess(−1, 0x1a)`. CSR 이 풀리면 NLS 에서 다시 막힐 수 있다 `[추측]`.

### 앞 답 고쳐 읽기

- **run138** (+2 줄): 3-3 표 "부르기" (원래 `:272`, 지금 `:274`) · 나무 (원래 `:320`, 지금 `:322`) 의 "`whNt*` 는 64비트 `Nt*` 를 import 해서 부른다" → 대부분은 그렇지만 **`0x1e9` 는 64비트 `CsrClientConnectToServer` (Nt 아님) 를 부른다**.
- run152 자신: 맨 위 "보탬 · 표기 (run153 때 대조)" — `0xc000000d` 가지 보탬 · PEB32 세 칸 줄 범위는 세는 시작만 다름 (쓰는 자리 `+0x63` · `+0x7b` · `+0x8e` 같음).

### 남은 것

- VM (x86 시험 exe 필요 — 사용자 결정): run152 동적 1 ~ 8 (`ntdll!CsrpConnectToServer+0x1bb/+0x2e4/+0x33d` · `wow64!whNtWow64CsrClientConnectToServer+0x95` · `KERNELBASE!__KernelBaseBaseDllInitialize@12+0x20c/+0x248/+0x3ba/+0x3f0` · `_PcwDllMain@12+0x28`) → **Q84 · Q253d · Q262**.
- 정적: 32비트 NLS (F3 — 다음 막힘 후보, 가장 앞) · 32비트 콘솔 (F5) · `whNtQueryInformationProcess` · `PcwDllMain` · 32비트 kernel32 · 32비트 user32 csrss 연결 · `RedirectPath` 이름 · 커널 legacy `NtConnectPort` → **Q295**.

---

## ② Q288 + Q284a — rpcss `Connect` 응답 SD 다섯 조각 · combase 가 죽는 사슬 (380 ②) — run151

### 답

- **넷 (`gpNoReg*SD`) 은 rpcss 가 서비스 시작 때 코드 상수 SDDL 로 짓는다 (레지스트리 안 봄)** `[정적]` — 출처 (가). 크기 **124 · 124 · 232 · 228 B** `[추측: 계산]`. 둘에 BUILTIN SID 둘 (`S-1-5-32-562` · `S-1-5-32-559`) 이 끼고 **기계마다 같다**. `[3]` 은 OS 종류로 고름 (워크스테이션 AN `0x3` / 서버 `0x7`).
- **다섯째 (`GetDefaultAccessRestrictions`) 만 레지스트리** (나) — 정책 `HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM` 먼저, 없으면 `HKLM\SOFTWARE\Microsoft\OLE` 값 `MachineAccessRestriction`. **없으면 빠진다 (크기 0 — 진짜에서도 나는 꼴).** (다) 기계 · 부팅마다 다른 SID 는 없다.
- 원문 `[수집]` 셋: `MachineAccessRestriction` (228) → `[4]` 실림. `DefaultLaunchPermission` (124) · `MachineLaunchRestriction` (232) → **Connect 응답에 안 실림.** `DefaultAccessPermission` 은 rpcss 가 안 읽는다.
- **죽는 줄** `[정적]`: 명시 `CoInitializeSecurity +0x385 GetDefaultAccessRestrictionsSD` → 다섯째가 NULL 이면 넷째 (`_pNoRegAccessRestrictionsSD`) → `+0x2c GetSecurityDescriptorLength` → ntdll **`RtlLengthSecurityDescriptor +0x2d`**. "SD 크기 0 ×5" 면 넷째가 0 B 비-NULL 덩어리라 길이 함수가 밖을 읽다 접근 위반. **검사는 없다.**
- **lsapolicylookup** `[정적]`: 명시 길 (`LookupPrincName +0x2af` → `LookupAccountSidLocalW` → `LsaLookupOpenLocalPolicy`) 에선 **끼지만 실패를 삼키고 지나간다** (HRESULT 0, 죽음과 무관). 암묵 길 (`EOAC_APPID`) 에선 **치명** (둘째 조각 124 B 도 쓰임).
- **Q284a**: 첫 COM 사용의 첫 부름은 **프로시저 6 (`ServerAllocateOXIDAndOIDs`) 먼저** (`CallTheResolver +0x9d JZ` — `[OXIDEntry+0xd8]` 등록 표시 0 이면; 첫 때 0 인 것은 `[추측]`), 7 은 등록 뒤 OID 를 더 받을 때. `LocalInterfaceOnlySecCallback` = 반환 0 통과 · 5 거절 (서버 흉내면 구현 불필요). "클래스 없음" HRESULT 는 **combase 가 안 만든다** (rpcss 활성화 서버 몫 `[공백]`).

### 지금 우리 맞나 · 틀리나

- **BG05 의 "SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL" = 금지 꼴 1** (BG05 와 같은 죽음).
- **응답 칸 28 ~ 30 에 쓸 값**: 28 = **`{124, 124, 232, 228, 0}`** · 29 = **708** · 30 = run151 1-1 의 자기 상대 SD 넷 16진을 **패딩 없이 이어 붙인 것** `[추측: 계산]`. VM 에 더 가깝게: `{…, 228}` + 다섯째 = VM `MachineAccessRestriction` 바이트 (`[수집]`), 총 936.
- **금지 꼴 다섯** (0 B 넷째 · 절대 형식 SD · 크기 합 > 총 크기 · 쓰레기 다섯째 · …) → run151 "받을 꼴 ③". **자기 상대로 줄 것.**
- **LSA1 판단**: 명시 `CoInitializeSecurity(SD NULL, −1)` 표본 (r19 OR1) 은 **LSA1 이 OR1 앞 바퀴가 아니다** (칸 28 ~ 30 만 고치면 됨). `CoInitializeSecurity` 없이 COM 을 쓰는 표본 (암묵) 은 **LSA1 이 앞**.
- **다음 벽 = 프로시저 6 (7 아님)** — `ServerRegisterOXID +0x30 StartListen` 먼저, 대답 `상태 0 · OXID ≠ 0 · DSA NULL`. 그 뒤 7 (`상태 0 · 개수 ≤ 원하는 수`). 몸 전엔 솔직한 실패 `0x6ba` (7 이면 `0x776` 도).
- `rpcss.cpp` · `alpc_frontend.cpp` 는 안 읽음 (결과서도).

### 앞 답 고쳐 읽기

- **run149** (+4 줄, 정정 둘): (a) 2-2 칸 28 "(라) 0 ×5 가능" (원래 `:201`, 지금 `:205`) · "최소 대답" 3 (원래 `:315`, 지금 `:319`) **틀림** — 위 값으로. (b) "첫 COM 사용은 7 → 6" (원래 `:21` · `:238` · `:284` · `:332` · `:353`, 지금 `:25` · `:242` · `:288` · `:336` · `:357`) → **6 먼저**. 지난 길잡이 Q257a "그다음 벽: 프로시저 7 → 6" 도 6 → 7 로 읽을 것. run149 해설서도 맨 위 두 줄.
- run151 자신: 맨 위 "정정 · 대조 (run152 때 대조)" — combase `InitializeSystemSecurityDescriptors` 오프셋 세 벌 (`LocalAlloc` call · 전역 넣기 · `memcpy` call) 은 다 맞고 뜻이 다름 · `+0xd8` 은 두 구조체의 다른 칸 (`CComApartment+0xd8` = 더할 OID 수, `OXIDEntry+0xd8` = 등록 표시). 결론 (6 먼저) 그대로.

### 남은 것

- VM: SD 계산값 대조 (`combase!CRpcResolver::_pNoRegAccessRestrictionsSD` · `_pDefaultAccessRestrictionsSD` 덤프 · `rpcss!_Connect+0x5ce` 앞 크기 다섯) · `LookupPrincName+0x116` · `CallTheResolver+0x9d` — 새 번호 없이 **Q283** (D5 · D8 "읽기만").
- 정적: `ISCMLocalActivator` 서버 ("클래스 없음") · `GetLegacyBlanket` AppID 있는 길 · `CallAccessCheck` · `MakeProxyHelper` · 암묵 길 실패 HRESULT · `OXIDEntry+0xd8` 첫 값 → **Q294**. rpcss 프로시저 10 · 1 · `CoGetObject` · `PickAssociation` (lsapolicylookup "8 번" 재시도 포함) → **Q284b**.

---

## ③ Q289 — rpcrt4 EPM NDR 2.0 프로시저 3 · 7 파라미터 표 (380 ③) — run150

### 답

- **7 은 8 과 요청 꼴이 같다** `[정적]` — 객체 UUID 16 B 인라인 (attr `0x010a`, 포인터 ID 없음) · 탑 인라인 (`0x010b`) · SID `FC_UP` · 이어받기 핸들 20 B · `max_towers` (0 ~ 500). 다른 것은 **비동기 핸들이 없다는 것뿐** (Oi2 `0x43` ↔ 8 `0xc3`, 스택 자리 −8).
- **`FC_FP` 포인터 ID 가 붙는 것은 3 뿐**, 3 에는 **SID 칸이 없다** (파라미터 7).
- 요청 스텁 길이 (탑 octets 66) `[정적]` + `[규칙]`: **3 = 124 B** (객체 NULL 108 · 탑 NULL 48) · **7 SID NULL = 120 B** · **7 SID 있음 = 132 + 4n B** (n = 1 → 136). 응답은 셋 다 같다 (탑 1개 · octets 73 → 128 B, 탑 0개 → 40 B).
- 클라이언트 `LookupNextChunk` `[정적]`: 비동기 → **8**, 동기 + SID NULL → **3**, 동기 + SID 있음 → **7**. 그래서 이 코드에선 **7 요청에 늘 SID 가 실린다.** `max_towers` 는 늘 4, 3 의 객체 포인터는 늘 비-NULL.
- 원문 "확인 거리" 표 (02 덤프) 와 **칸마다 맞다.** NDR64 쪽도 같은 관계 (바이트 꼴은 `[공백]`).

### 지금 우리 맞나 · 틀리나 (`epm.cpp`, 286줄)

- `:203 ~ :221` `ept_read_full_head` (3) + `:194 ~ :199` — **맞음**. `:205` `TODO(VM)` "3 파라미터 표 안 풂" 은 `[정적]` 으로 닫힘.
- `:175 ~ :190` `ept_read_sid` · `:162 ~ :171` `ept_read_tower` · `:256 ~ :267` `ept_map_auth_async` (8) · `:121 ~ :142` `ept_map_reply` — **맞음**.
- **`:279` · `:280` — 7 을 이제 달 수 있다**: **8 의 풀기 그대로** (`{7, …}` 를 같은 함수 또는 얇은 래퍼로), 응답도 같은 `ept_map_reply`. **7 을 3 의 `FC_FP` 꼴로 풀면 안 된다** — BG04 시도 2 의 `0x6f7` 까닭 (처음 16 B 를 ID 둘로 읽어 칸이 어긋남, 어느 검사에서 걸렸는지는 `[추측]`).
- `TODO(VM)` 에 쓸 글 (7 · 3 한 줄씩) 과 시험 길이 (7: SID NULL **120** · nsi 질문 + `S-1-5-19` **136**, 3: **124**) → run150 "PE_emulator 쪽".

### 앞 답 고쳐 읽기

- **run145 정정** (+2 줄): "PE_emulator 쪽" 표 셋째 줄 둘째 칸 — 원래 **`:264`, 지금 `:266`** — 의 "3 · 7 은 요청 NDR 꼴만 다름 (`FC_FP` 포인터 ID, run137 3-1)" **틀림** → 7 = 8 꼴, `FC_FP` 는 3 뿐. 셋째 칸 "같은 장부 함수에 붙이면 됨" 은 그대로 맞다. run145 해설서 2.3 끝 줄 (원래 `:278`, 지금 `:280`) 도 같음.
- run137 `:238` · 해설 `:553` · `:560` 은 처음부터 3 만 말했다 — 고칠 것 없음.

### 남은 것

- VM: 선 위 진짜 바이트 (3 의 포인터 ID 값 · 7 에 실리는 실제 SID · 3 · 7 응답) — 멈춤점 `rpcrt4!NdrClientCall3` (ret `LookupNextChunk+0x170` / `+0x1b3`) 또는 `LookupNextChunk+0x127` `r9` → **Q280**.
- 정적: `LookupNextChunk` 가 SID 를 어디서 가져오나 (`[[RDI+0x10]+0x10]` `+0x22f` · 어느 바인딩 옵션이 심나 — `epm.cpp:106` `AccessCheck` 범위를 정함) · NDR64 바이트 꼴 → **Q293**. "디버거 없이 VM" 은 없음.

---

## ④ Q278b — 메시지 상자 글 폭 · 세션 기본 `DC_ATTR` 틀 · 필터 값 5 · gdiplus 뒷몫 (380 차례 ④ 앞) — run153

### 답

- **글 폭 길** `[정적]`: `SoftModalMessageBox +0xf5 NtUserGetDCEx(0,0,3)` → `+0x149 UserGetCharDimensionsEx` → gdi32full `GdiGetCharDimensions` → `pcfLocateCFONT` → `bGetTextMetricsWInternal`.
  - **표 검사 실패면 syscall 없이 0** → `MessageBoxW` 0 (run142 · run144 그대로).
  - 표가 맞으면 **글꼴 사본 (CFONT) 길이 막혀도 `NtGdiGetTextMetricsW` (`0x106e`) · `NtGdiGetTextExtent` (`0x108c`) 되돌이로 지나간다** → 에뮬레이터는 사본을 안 만들어도 된다.
- **`NtUserGetDCEx(0,0,3)` = 0 이 되는 길은 일반 프로세스 · 화면 있음이면 없다** → 12 샘플의 0 은 **stub 탓**.
- **세션 기본 `DC_ATTR` 틀** `[정적]`: `InitializeGre +0xb8 → InitializeDefaultDCAttributes` (0x1b0 B) — `+0x88 = 1` · `+0x98 = 0x10` (DC 사본엔 `0x12001f`) · `+0x40 = 0x4b` · 붓 스톡 0 · 펜 스톡 7 · 글꼴 스톡 13 (마지막은 `[추측]`). 스톡 핸들 = 객체 핸들에 `0x800000`.
- **`GDIINFO` · `pGdiDevCaps`**: 가짜 화면 (`StubDisp*`) 이면 상수 (1024 × 768 · 32 비트 · 60 Hz), 진짜 드라이버 쪽 `[공백]`. `+0x180094` (언어팩) = 레지스트리 `…\Control\LanguagePack` DWORD, `+0x180098` = 1.
- **필터 값 5** `[정적]` + `[objdump]`: `g_systemCallFilterId` = `EPROCESS+0x6e8` (부모가 준 Win32k 필터 속성) 하위 4 비트 → **일반 프로세스는 0**. 5 면 `GetDC` 가 가짜 HDC `0xFFFFFFFFDCDE5000` · 25 곳이 다른 길.
- **gdiplus `+0x62b ~ +0x7b3` 은 벽이 아니다** (`InitImagingLibrary` 늘 1 · 파일 · 레지스트리 안 읽음). 새로 나오는 것은 `+0x7b3 BackgroundThreadStartup` 의 **이벤트 둘 + 스레드 하나** (하나라도 NULL 이면 `GdiplusStartup` 실패).
- N5 `NtQIP(0x25)` 를 누가 부르나는 반만 — kernelbase 안 줄은 `GetProcessVersion` 뿐, 부르는 곳 `[공백]`.

### 지금 우리 맞나 · 틀리나

- 메시지 상자 12 샘플 — **stub 탓**. 고칠 것 = HDC 돌려주기 + 글 폭 대답 (전제: run147 목록 8 ~ 10 — DC 핸들 `번호 | 0x10000` · 표 칸 · `DC_ATTR`).
- 최소 순서: ① `NtUserGetDCEx(0,0,3)` → 목록 8 꼴 HDC (`0x21000` 꼴은 안 됨) · `NtUserReleaseDC` → 1 ② **`NtGdiGetTextMetricsW` ≠ 0 + `+0x00` · `+0x14` ≥ 1** ③ **`+0x37` 비트 0 = 0** ④ **`NtGdiGetTextExtent` ≠ 0 + `SIZE`** ⑤ 나머지 (`NtGdiGetTextFaceW` · `NtGdiQueryFontAssocInfo` · `NtGdiComputeXformCoefficients` · `NtGdiGetDCDpiScaleValue`) 기본 0 그대로.
- run147 목록 13 (`NtGdiGetDeviceCaps` 최소값) **맞음** · 목록 10 (`DC_ATTR`) **더하기** (사본 길을 안 쓸 거면 `+0x40` = 0 이 단순) · 목록 16 (`g_systemCallFilterId`) **5 를 줄 이유 없음 (금지)**.
- **stub 최소 목록 17 ~ 20** (run147 1 ~ 16 에 이어): 17 글 폭 되돌이 · 18 사본 길 흉내 (권하지 않음) · 19 조용한 부수 효과 (`+0x180094 = 0` · `+0x180098 = 1` · `gpsi` · 필터 0 · edgegdi 없음) · 20 gdiplus 백그라운드 스레드 (`NtCreateEvent` ×2 · `NtCreateThreadEx` — 그 스레드는 메시지 고리에서 영영 기다림 `[추측: 스케줄러 영향]`) → run153 "PE_emulator 쪽".
- 결과서 · flow-writer 둘 다 `../PE_emulator*` 를 이번엔 안 열었다 (run147 때 상태 기준).
- **그 뒤 벽 후보**: 글꼴 열거 (`GpInstalledFontCollection` 첫 사용) — 안 읽음.

### 앞 답 고쳐 읽기

- **run144** (문서 · 해설서 맨 위): "`GetTextExtentPointW(DC)` → 0 + `0x57`" 은 앞에서 **`GetDCDpiScaleValue(hdc) == 1`** 을 지난 뒤의 얘기 — 1 이 아니면 곧장 `NtGdiGetTextExtent` (LastError 안 세움). `0x25` 출처 `[추측]` → `[공백]` 그대로. N2 닫힘 (벽 아님) · N3 · N4 닫힘 · N5 반만.
- **run147** (문서 · 해설서 맨 위): `+0x114` 닫힘 (`gbLpk`) · `+0x160` 은 `pcfLocateCFONT` 가 사본 목록 머리로 씀 · `+0x98` 세션 틀 값은 `0x10` (`0x12001f` 는 DC 만들 때 덮는 값, 유저가 보는 처음 값은 그대로) · 스톡 0 · 7 · 13 · "win32k 몫 남음" 1 ~ 6 닫힘.
- 지난 길잡이 Q278a "그 뒤 벽: `InitImagingLibrary` (N2)" → **벽 아님**. 다음은 위 목록 20 · 글꼴 열거.

### 남은 것

- VM (같은 VM GUI 세션): 글 폭 사본 길 · 그 DC 의 `DC_ATTR` · win32k `NtUserGetDCEx` · 세션 틀 0x1b0 B · 언어팩 `[+0x180094]` · `PspAllocateProcess +0x1f11` 필터 · `GetProcessVersion +0x3b` 콜스택 · gdiplus 스레드 → **Q277**.
- 정적: gdiplus 글꼴 열거 · N5 나머지 · CFONT 진짜 꼴 · Win32k 필터 속성 넣는 쪽 · `[세션+0xace8]` · 진짜 화면 드라이버 GDIINFO · DPI 문맥 첫 값 → **Q296**.

---

## ④ Q282 — 대화상자 · 메시지 상자의 커널 쪽 몸 (380 차례 ④ 뒤) — run157

### 답

- **자식 연결** `[정적]`: 새 컨트롤은 부모 자식 목록의 **맨 끝** (`xxxCreateWindowEx` 가 `WS_CHILD` 면 `HWND_BOTTOM (1)` → `LinkWindow`) — **연결 차례 = 템플릿 차례**, 이전 형제 칸 `사본+0x50` 도 쓴다.
- **ANSI**: 확장 스타일 `0x80000000` → `사본+0x78` = A 표 칸 (대화상자 = `DefDlgProcA`, `gpsi+0x1d8`) · `사본+0x13` 비트 5. `사본+0x12` 비트 3 은 클래스 쪽 조건으로만 (대화상자 클래스가 아홉 칸에 드는지 `[공백]`).
- **`NtUserPeekMessage` 꺼내는 차례**: **보낸 것 (안에서 처리) → 게시 → 입력 → `WM_PAINT` → `WM_TIMER` → 0**. 막 선 모달 대화상자의 첫 부름 = **`WM_PAINT`** (`WM_NCACTIVATE` 류는 보내기라 안 나옴). 몸 = `NtUserPeekMessage +0xff xxxInternalGetMessage +0x78 xxxRealInternalGetMessage`.
- **MessageBox**: 같은 `DialogBox2` 고리 · 반환 안 바꿈. 기본 단추 ID 는 `MessageBoxWorker` 표 (표 C) — **`MB_YESNO` = `IDYES (6)`** (`DEFBUTTON2` 면 `IDNO 7`). 단추 ID = `gpsi + 칸*0x28 + 0x3c4` (`uID`, 커널이 `칸 + 1` 로 씀). **`MB_YESNO` · `MB_ABORTRETRYIGNORE` 는 닫기 · Esc 로 못 끝낸다.**
- **비트표** (`user32+0xda568 ~ +0xda580`) = **`gSharedInfo` 복사** (`NtUserInitializeClientPfnArrays` 아님), 한도 **`0x33f` · `0x349`**. 그 0x240 B 는 **보통 프로세스에선 csrss USERSRV 연결 답 (`CsrClientConnectToServer(…, 3, 0x248 B)`) 의 `+8` 부터** (`_UserClientDllInitialize +0x30b ~ +0x37c`) — 비트표 칸은 답 버퍼로 `+0x220` · `+0x228` · `+0x230` · `+0x238`. `ClientThreadSetup +0x120 NtUserProcessConnect` 복사는 `gfServerProcess ≠ 0` (csrss 같은 서버 프로세스) 첫 스레드만 (`+0xdd ~ +0xe5` · `+0xeb ~ +0xed`) `[정적]` — **run157 맨 위 "정정 (run157 해설 때 대조)"**, run85 가 맞다. `WM_GETDLGCODE` · `WM_COMMAND` · `WM_INITDIALOG` 는 목록에 없어 syscall 없이 0.
- 단추: `BS_DEFPUSHBUTTON` 의 `WM_GETDLGCODE` = `0x2010`, 클릭 = 부모에게 보내기 `WM_COMMAND`. `WM_DESTROY` · `WM_NCDESTROY` 는 **커널이 되부르며 보낸다**. `GWLP_USERDATA` = **`사본+0xd8`** (옛값 반환). IMM 훅 `[user32+0xd96f0]` = `imm32!ImmTranslateMessage`.

### 지금 우리 맞나 · 틀리나

- `NtUserPeekMessage` (`nt_user_peek_message.cpp :29` · `:50`) — **순서는 맞음 (게시 한 칸만 한 꼴)**. 빠진 것: `WM_PAINT` · `WM_TIMER`. 큐가 비면 멈춤은 진짜 "0 + `WaitMessage`" 와 다름 `[정책]`. `WM_PAINT` 를 주려면 `NtUserMessageCall(WM_PAINT)` 가 갱신 영역을 비워야 (아니면 끝없이 `[추측]`).
- `NtUserWaitMessage` = 잠 — **맞음**.
- `NtUserCreateWindowEx` — 자식 연결 안 함 → **run146 stub 10 "템플릿 차례" 확정 + `사본+0x50` 추가**.
- `사본+0x78` 고정 W 판 (`nt_user_create_window_ex.cpp :45` · `:162`) — **A 판 시료에서 틀림**. W 판은 맞음.
- **"기본 단추 누름" `WM_COMMAND(LOWORD(pdlg+0x20), 0 이면 1)` (run148 목록 7)** — `pdlg+0x20` 이 표 C 값일 때만 맞음. 컨트롤 프로시저를 안 타면 `pdlg+0x20 = 1` 그대로 → **`MB_YESNO` 가 `WM_COMMAND(1)` → 무시 → 끝없는 고리**. 고칠 꼴: ① `BS_DEFPUSHBUTTON` 컨트롤 ID 를 `pdlg+0x20` 에 직접 ② 표 C 를 stub 이 직접.
- `MBStrings` 0 채움 (run142) — **`uID` (`+0x3c4`) 가 0 이면 단추 ID 가 전부 0** → 눌러도 무시 · 반환 0.
- **비트표를 줄 곳 = `csr_user_connect_stub.cpp` (USERSRV 연결 답)** — `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 보통 프로세스가 **안 부른다** (몸이 없어도 이 길엔 무관). 답의 비트표 칸이 0 이면 (한도 0) → 모든 `DefWindowProc` syscall 없이 0 (끝 길엔 영향 없음, `WM_PAINT` · `WM_CLOSE` 기본 처리는 아무 일도 안 함). A / W 표 (`gpsi+0x188` · `+0x248`) 는 보통 프로세스에선 공유 `gpsi` 에 이미 있는 값을 읽는 것 `[추측]` → stub 은 `psi` 의 그 칸을 미리 채워야 A 판 `사본+0x78` 값을 안다.
- `NtUserDestroyWindow` 기본 0 — 반환은 맞음, 되부름 (`WM_DESTROY`) 안 하면 앱 자기 루프엔 영향 `[추측]`. `NtUserSetWindowLongPtr` — run148 판정 (틀림) + `−21` 자리 확정. `NtUserSetMsgBox` → 0 **맞음**.
- **stub 최소 목록 18 ~ 25** (결과서 번호, run148 문서 7 ~ 15 에 이어): 18 자식 연결 · 19 A 판 (A / W 표는 `psi` 칸) · 20 **USERSRV 연결 답 (`csr_user_connect_stub.cpp`) `+8` 의 `gSharedInfo` 비트표** (run157 해설 때 고침 — 처음엔 `NtUserProcessConnect` 로 적음) · 21 `MBStrings` (`uID = i + 1`) · 22 `pdlg+0x20` · 23 `NtUserSetMsgBox` · 24 `NtUserMessageCall(WM_PAINT)` · 25 `NtUserDestroyWindow` 되부름 정책 → run157 "PE_emulator 쪽".

### 앞 답 고쳐 읽기

- **run157 자신** (맨 위 "정정 (run157 해설 때 대조)"): 비트표 출처 = 보통 프로세스는 csrss USERSRV 답, `NtUserProcessConnect` 는 서버 프로세스만 (위). 목록 19 · 20 · 표 E `NtUserPostMessage` call `+0xd9` (결과서의 `+0xb0` 은 `NtUserSetWindowPos` ret).
- **run148** (문서 · 해설서 맨 위): 비트표 출처 (`gSharedInfo` 복사 — run148 문서 맨 위 줄은 run157 해설 때 "보통 프로세스는 USERSRV 답" 으로 고침, 해설서 맨 위 줄은 아직 `NtUserProcessConnect` 로 적혀 있음) · `WM_GETDLGCODE` 0 길 = syscall 0 · `+0x38` 템플릿 차례 `[정적]` · `+0xd8` `[정적]` · `NtUserDestroyWindow` 되부름 `[정적]` · IMM 훅 몸 · `MessageBoxWorker` 반환 안 바꿈 · **최소 목록 7 은 `pdlg+0x20` 이 표 C 값일 때만** · `xxxPeekMessage` 는 이 빌드에 없음 (정정).
- **run146** (맨 위): stub 10 "템플릿 차례" 확정 + `+0x50` · `+0x12` 비트 3 켜는 조건 · `psi+0x320` 은 `gpsi+0x308` 배열 안.
- **run142** (맨 위): 5절 "`+0x3c4` 0 이면 빈 글 단추 (상자는 뜸)" → **`uID` 0 이면 단추를 눌러 끝낼 수 없다.**
- 지난 길잡이 Q274a "조심: `MB_YESNO` 기본 ID 가 1 이 아님 (Q282)" → 닫힘: `IDYES 6`.

### 남은 것

- VM (VM GUI 세션): run157 동적 1 ~ 9 (`DialogBox2+0x270` 첫 메시지 · `WM_PAINT` 횟수 · 자식 사슬 · A 판 · MessageBox 종류별 `pdlg+0x20` · `MBStrings` · 비트표 값 · `xxxButtonNotifyParent` · `DestroyWindow` 차례 · `SC_CLOSE`) → **Q273**.
- 정적: `#32770` 원자 아홉 칸 · `xxxEndDeferWindowPosEx` 갈래 · `xxxInternalDoPaint` · `xxxReadPostMessage` · 단추 클릭 입구 · `gfDMREnable` · `0x272` · `GetCurrentNlsCache` · `ImmTranslateMessage` → **Q301**.

---

## ⑤ Q290 + Q276a — `runas` 승격 부탁 길 (374 appinfo-runas) — run156

### 답

- **부탁하는 쪽** `[정적]`: shell32 `ShellExecuteExW` 는 지연 로드 썽크 한 줄 → **windows.storage 안에서 끝난다** — … → `CInvokeCreateProcessVerb::Execute` → `Launch` → `_TryCreateProcess` → `_PrepareAndCallCreateProcess` → `CallCreateProcess +0xc04` → **`AicLaunchAdminProcess +0x636 Ndr64AsyncClientCall`** (비동기, 프로시저 **0** = appinfo `RAiLaunchAdminProcess`).
- **RPC** `[정적: PE]`: **`201ef99a-7fa0-444c-9399-19ba84f12a1a` v1.0**, `ncalrpc`, **끝점 없음** → rpcrt4 가 `ept_map` 을 묻는다. 인증 6 · 10 · QOS V3 `Sid` = **`S-1-5-18`**. 요청 11칸 (실행 파일 · 명령줄 · `StartFlags` · **`CreationFlags 0x04080414`** · 폴더 · `WinSta0\Default` · 시작 정보 56 B · `HWND` · **시간 제한 `0xffffffff`** · out `PROCESS_INFORMATION` 24 B · out `ELEVATION_REASON`) + 반환.
- **원문 iface `0xe1af8308…` = EPM 자신** (run145, 프로시저 8). 그 요청 안 SID 는 클라이언트가 넣은 LocalSystem.
- **승격으로 가는 문 둘**: verb "runas" (`QueryExists` 글자 비교) · **`SHTestTokenMembership(NULL, 0x220)` 가 거짓** (참이면 RPC 없이 그냥 `CreateProcessW`).
- **응답 뒤**: 핸들 검사 없음. **깨우는 것은 클라이언트** (`CallCreateProcess +0xdf7 ResumeThread`, 호출자 플래그에 `CREATE_SUSPENDED` 가 없을 때).
- **오류**: `ShellExecuteExW` FALSE. `SEE_MASK_FLAG_NO_UI` 가 꺼져 있으면 **`GetLastError` 는 `0x4c7`** 로 덮임. `hInstApp` 은 `0x4c7` · `0x6f7` · `0x5` 모두 **5**. 재시도 집합 `{0x6ac, 0x6b5, 0x6ba, 0x6d9}` (두 번까지, 사이에 `AicpStartAIS`) — **`0x6f7` 은 안 든다.**
- **RPCSS 연결 셋은 꼭 필요하지 않다** (`CoCreateInstance(ImmersiveShell)` 실패 무시) — 그 셋이 이 호출에서 났다는 짝은 `[추측]`.
- **들어주는 쪽 (appinfo)**: consent 승인 뒤 받은 승격 토큰으로 **호출자를 부모로 (`0x20000`)**, 플래그 `0x04080414 | 0x80004` 로 `CreateProcessAsUserW(…, FALSE, …)`, 두 핸들을 **호출자 핸들 표로 복제** (플래그 `& 0x401` 이 0 이면 옵션 2 `DUPLICATE_SAME_ACCESS` · 접근 0, 아니면 옵션 0 · 접근 `0x20000100` — 비트 뜻 `[추측]`). 토큰 칸을 짓는 줄은 appinfo 에 없다 (consent 쪽 `[추측]`).
- **Q276a**: windows.storage `_map` 함수 424 (깊이 10) — 가상 호출에서 끊김, 사슬은 `_refs` 로 거꾸로 이음.

### 지금 우리 맞나 · 틀리나

- **원문 "이미 아는 것" 과 지금 파일이 다른 곳 둘**:
  - `epm.cpp` — 원문 "SID 든 요청 → fault `0x6f7` (`:18`)" 와 달리 **지금 `:231 ~ :240` 은 SID 를 검사만** 하고 장부로 넘김. 지금 막힘은 **장부** (nsi 한 항목뿐 → `0x16c9a0d6` · 탑 0) → rpcrt4 다음 SID 재시도 → `AicpStartAIS` 두 번 `[추측: 코드 읽기, 로그 없음]`.
  - `tokens.hpp` — 원문 "관리자 그룹" 과 달리 **Administrators (`S-1-5-32-544`) 가 없음** → `SHTestTokenMembership` 거짓 → RPC 길 (r19 로그와 맞음 `[추측]`).
- **stub 최소 목록 1 ~ 6**: ① EPM 장부에 `201ef99a` v1.0 (객체 nil · `AppInfo`) ② appinfo 창구 bind + 프로시저 0 (비동기) ③ 응답 반환 0 · `PROCESS_INFORMATION` · `ELEVATION_REASON` 6 ④ 엔진: 승격 토큰으로 호출자의 자식을 **멈춘 채** (깨우기는 클라이언트) ⑤ 부수 호출 (`PcaMonitorProcess2` — 모듈 없을 때 지연 로드 실패가 예외가 되는지 `[공백]`) ⑥ 호출자 토큰 그룹 (Administrators 켜지 말 것) · verb 레지스트리 → run156 "PE_emulator 쪽".
- **토큰 꼴**: 호출자 = Administrators **deny-only** + `elevation_type 3` + `TokenLinkedToken` = 승격 토큰 짝 `[정적: appinfo 줄]` · `[추측: deny-only 값]`. 승격 토큰 = `TokenElevation 1` · `elevation_type 2` · Administrators 켜짐 · High · 세션 = 호출자 세션 (appinfo 가 보는 건 승격 · 관리자 · 세션 셋뿐).
- **cmstp COM 승격 (딸림)**: 같은 인터페이스 · 같은 승인 엔진, **다른 프로시저** (6 `RAiForceElevationPromptForCOM` · 7 `RAiGetElevatedToken` — 토큰을 돌려줌). rpcss 가 `ole32!CoAicGetTokenForCOM` 을 부르는 길은 `[공백]`.

### 앞 답 고쳐 읽기

- **run143** (+2 줄): "`ShellExecute*` → `Execute` 는 가상 함수라 `[공백]`" (`:23` · `:208` · 나무 `:274` · `:328`, 지금 각 +2) → **정적으로 닫힘** (가상 칸 `[+0x48]` 대상은 가상표 값). "실제로 불리나" (Q275 5) 는 그대로 동적.

### 남은 것

- VM: run156 D1 ~ D8 (요청 스텁 · `ept_map_auth_async` SID · 응답 스텁 · 방법 값 · COM 짝 · 진짜 승격 토큰 `!token` · 돌려받은 핸들 · 창 갈래) → **Q283** (RPC 묶음, Q280 과 같은 세션).
- 정적: `CShellExecute+0xa8` (`hProcess`) 쓰는 곳 · `this+0x1f8` 출처 · `CLSID_ExecuteAssociation` · kernel32 `BaseCheckElevation` · consent.exe 토큰 · `ResolveEndpoint` `0x6d9` 재시도 · appinfo 깃발 표 · `PcaMonitorProcess2` · `CoAicGetTokenForCOM` → **Q300**.

---

## ⑥ Q291 — 디버그 속성 자식의 첫 이벤트 · 기다림 · 이어가기 (380 ⑥) — run154

### 답

- **붙는 길** `[정적]`: 속성 `0x60001` 값 = **디버그 객체 핸들**. `PspBuildCreateProcessContext` 는 크기 8 만 보고 (아니면 `0xc000000d`) 문맥 `+0x90` 에 **저장만**. `PspInsertProcess +0x2f3` 이 접근 **2** 로 열어 `+0x13e DbgkCopyProcessDebugPort` 에 → 자식 `EPROCESS.DebugPort` (`+0x308`) = 객체 · `DbgkpMarkProcessPeb +0x84` 가 **`PEB.BeingDebugged` = 1** (WoW64 면 PEB32 도).
- 핸들이 없으면 **만든 쪽 (현재) 프로세스** 의 `DebugPort` 를 그쪽 `NoDebugInherit` 가 꺼졌을 때 물려받는다.
- **첫 이벤트** — 자식 첫 스레드가 **유저로 가기 전에 스스로** 보낸다 (`PspUserThreadStartup +0x9c → PspNotifyThreadCreation → DbgkCreateThread`): **① CreateProcess (ApiNumber 2, Flags 0 — 안 얼림) → ② LoadDll ntdll (5, Flags 3 — 다른 스레드 얼림)** (WoW64 면 ③ 32비트 ntdll). **exe 는 LoadDll 이 따로 없고 ① 안에** 있다.
- 깃발 `[정적] PDB`: `+0x2c4` = `HideFromDebugger` · `+0x327` = `ClonedThread` · `+0x2b3` = 옛 `CreateReported`.
- **큐 · 기다림**: 보낸 스레드는 **자기 커널 스택의 0x168 B 이벤트** 를 객체 큐 꼬리에 끼우고 그 이벤트를 **커널 모드 · 비경보 · 무한** 으로 기다린다. 깨우는 건 `NtDebugContinue → DbgkpWakeTarget` 하나 (또는 디버거가 객체를 닫을 때).
- **고르기**: 큐 앞에서부터 `Flags & 5 == 0` 이고 앞에 같은 PID 가 없는 첫 이벤트 → READ 표시 (큐에서 안 뺌) = **한 프로세스는 한 번에 이벤트 하나만**. 빈 큐면 `0x102` · `0x101` · `0xc0` 셋 다 출력 0 으로. ntdll 은 **늘 `Alertable = 1`**.
- **`NtWaitForDebugEvent` 출력 0xB8 B 맞음**, `NtDebugContinue` ContinueStatus **여섯 값 맞음** (팀원 주석 둘 다 맞다). `0x40010001` 로 이으면 **같은 이벤트를 다시** 넣는다.
- 이벤트 차례 표 · `0xB8` 칸 표 (`NewState` 0 · 2 ~ 10, 접근 `0x12067b` / `0x12007b`, 파일은 복제 후 원본 닫음) → run154 "PE_emulator 쪽".

### 지금 우리 맞나 · 틀리나

- **팀원 주석 여덟 줄 모두 맞다** (0xB8 · 여섯 값 · 순서 · 시간 초과 0 채움 · 접근 1 · ClientId 로 찾기 (+READ 켜져야) · `:94` 멈춤 · `NtCreateDebugObject` `flags & ~1`).
- **"기다림 가짜 성공" (D155) 은 진짜엔 없는 조합** — 진짜 status 0 은 늘 `NewState ≥ 2` · ClientId · 핸들이 찬 구조, 빈 구조는 `0x102` · `0x101` · `0xc0` 일 때만. 그 뒤 `NtDebugContinue` 도 `0xc000000d`.
- **엔진 몫 1 ~ 4** (`nt_create_user_process.cpp:169` 앞): 속성 받기 (크기 8, 저장만) → 짓기 끝에 접근 2 로 참조 · "닫는 중" 이면 안 붙이고 성공 · 객체가 WoW 표시인데 자식이 32비트 WoW 가 아니면 `0xc00000bb` · 붙이면 `DebugPort` · `BeingDebugged` → 첫 스레드 알림 (① → ② 차례, 하나마다 보낸 쪽 멈춤, **부모가 처음 기다릴 때 ① 이 큐에 있어야**) → 그 뒤 이벤트 없으면 `stop(…)`. ② 의 이름 (자식 `TEB+0x28`) 은 가상 자식에 TEB 가 없으면 못 읽음 `[공백: 엔진 설계]`.
- **팀원 몫** (`nt_debug_more.cpp :94` · `:119`): 이벤트 칸 · 고르기 규칙 · 핸들 열기 · 빈 큐 기다림 (`alertable` 가지가 실제로 타는 길) · `NtDebugContinue` 빼기 · 깨우기 · 다음 하나 INACTIVE 풀기 (못 찾으면 `0xc000000d`) · 마지막 핸들 닫기 (`0xc0000354`).
- **`debug.hpp:8` flags**: 지금은 사용자 값 (bit 0 = KillOnClose) — 진짜 커널 칸은 bit 0 닫는 중 · bit 1 KillOnClose · bit 2 WoW. "닫는 중" · "WoW" 를 모델할 때 **bit 0 에 섞지 말 것.**

### 앞 답 고쳐 읽기

- **run113** (맨 위 "표기 정정"): "`DbgkpMarkProcessPeb` (ret `+0x2c8972`)" 는 **조각 `FUN_140845053` 기준** — 함수 기준은 **call `+0x2c8a0c` · ret `+0x2c8a11`**.
- run50 · run113 · flow.md 2.1 · baggage 3.1 / 3.11 의 "부모 `DebugPort` 복사" → 정확히는 **만든 쪽 (현재) 프로세스, 핸들 속성이 없을 때만** (좁힘 — 앞 문서 맨 위 줄은 안 넣음, flow.md · 짐 지도에 한 마디).

### 남은 것

- VM (새 시험 exe — `CreateProcessA(자식, DEBUG_PROCESS)` + 디버그 루프): 멈춤점 9 (`nt!PspInsertProcess+0x2f3/+0x13e` · `DbgkpMarkProcessPeb+0x84` · `PspUserThreadStartup+0x9c` · `DbgkpSendApiMessage` · `DbgkpQueueMessage+0x1f0` · `NtWaitForDebugEvent+0x2bb` · `DbgkpWakeTarget+0x51` · `NtDebugContinue+0x13b`) → **Q297**. 디버거 없이 몫은 맨 끝 표.
- 정적: attach 길 · 첫 이벤트 뒤 (`DbgkForwardException` · Exit 류) · `ClonedThread` 켜는 곳 · ntdll `DbgUi*` · `BeingDebugged` 분기 · 이벤트 `Flags 0x10` · `0x40` · 첫 스케줄 → **Q298**.

---

## 덤 Q292 — winnsi 6 · 7 형식 · 취소 메시지 · nsi 포트 종류 0 · 1 밖 (380 덤 1 ~ 3) — run155

### 답

- **덤 1** `[정적]`: **6 응답 24 B 맞음**, **6 요청은 52 B 가 아니라 56 B** (모듈 ID GUID — LUID 면 48, 포인터 NULL 이면 32). 모듈 ID 24 B = Length 2 · Type 2 · **가름값 2** · 패딩 2 · GUID 16 — 가름값을 쓰는 줄은 `[정적]`, 그 함수가 이 길에서 불린다는 사슬은 `[추측]` → 진짜 길이는 VM. 같은 까닭으로 **5 요청도 40 B 가 아니라 44 B** 후보. **7 요청 = 컨텍스트 핸들 20 B**, **7 완료 = 일반 응답 (종류 3) 32 B · 끝 4 B `0x4c7`** — 진짜 서버는 걸린 7 을 **fault 가 아니라 일반 응답** 으로 끝낸다.
- **덤 2** `[정적]`: 취소 (`RpcAsyncCancelCall`) = **LRPC 종류 4 · 0x40 B · Flags `0x10000` · 보내기만, 서버는 답하지 않는다.** 클라이언트의 **2 ms 잠 (`AlpcCancelMessage`) 은 요청 때 넘긴 Context 속성의 `MessageId` / `CallbackId` 가 0 이 아니게 될 때만 끝난다** (시간 제한 없음). `NtAlpcCancelMessage` 반환은 `0` · `0xc0000702` · `0xc0000703` · `0x4000002e` · `0x103` 밖이면 **치명** (`RpcpReportFatalError` + `INT3`).
- **덤 3** `[정적]`: 클라이언트가 먼저 보내는 종류 0 · 1 밖은 **4 (취소) · 5 (예약 메시지 묶음)** 둘뿐. 원문 표본 (`svc_lrpc_kind_unsupported`) 은 **5** (PE_emulator 이유 문자열로 가름 — 표본 로그는 지워져 확인 못 함, 결과서 "부분성공"). 거절해도 클라이언트는 안 막힘 `[추측]`.

### 지금 우리 맞나 · 틀리나

- `nsi.cpp :14` · `:77` (32 하한) **하한으로 맞음** · `:79` 끝 20 B 문맥 **맞음** · `:36` 5 `< 40` **영향 없음** (하한은 36 이 맞다) · `:10` 6 응답 24 B **맞음**.
- **`nsi.cpp :98` 7 완료를 fault 로 — 틀림** → 일반 응답 (종류 3) · 스텁 32 B · 끝 `0x000004c7`. `TODO(VM)` 에 쓸 글 → run155 "PE_emulator 쪽".
- **`lrpc.cpp :137 ~ :139` — 취소 (종류 4 · Flags `0x10000`) 가 빠짐** → 상태 0 · 답 없음 · 큐에 아무것도 안 넣음이 진짜와 같다.
- **비동기 요청 받기 — Context 속성 `MessageId` (`+0x14`) · `Sequence` (`+0x10`) 를 0 아닌 값으로 써 줘야** 2 ms 잠이 안 돈다 (안 쓰면 BG02 의 끝없는 `NtDelayExecution(2 ms)` 꼴 `[추측: 로그 원본 없음]`).
- **`nt_alpc_cleanup.cpp:42` `NtAlpcCancelMessage` → `0xc0000109` — 치명으로 이어짐** → `0` 을 돌려주면 rpcrt4 가 부름을 `0x71a` 로 직접 끝냄.
- `lrpc.cpp :328` — 자리는 맞음, 오는 것은 **종류 5**. 급하지 않음. 로그에 Flags (`a[1]`) · `+0x28` · `+0x18` 을 먼저 남길 것.

### 앞 답 고쳐 읽기

- **run137** (+2 줄, "VM 확인 전"): 요약 `:25` (지금 `:27`) · 3-2 `:276` (`:278`) · 5절 ⑦ `:306` (`:308`) · 정정 3 `:364` (`:366`) 의 "nsi 5 요청 40 B · 모듈 ID 20 B" → **모듈 ID 24 B · 5 요청 44 B** (LUID 36). 해설서 `:115` · `:614` · `:623` · `:626` · `:675` · `:697` (각 +2) 도 같음.
- **run129** (+2 줄): 3절 표 7 줄 `:152` (지금 `:154`) "24 B 모듈 ID 꼴 · DWORD" → 16 B 구조체 A · 24 B 구조체 B (모듈 ID 꼴 아님) · enum16 · 반환.
- 원문 덤 1 의 "6 요청 52 B" → 56 B.

### 남은 것

- VM: 선 위 요청 길이 (5 · 6 = 44 / 56?, `AlpcSend+0x128` `db @r8 L0x80`) · 7 완료 응답 32 B · 취소 (`AlpcSendCancelMessage+0x9a` · `CancelAsyncCall+0x6d` · `AlpcCancelMessage+0x27/+0x87`) · 새 스레드 첫 메시지 (`SendReservedMessage` 입구) → **Q280** (RPC 묶음).
- 정적: 커널 보내기 쪽 `MessageId` 쓰는 줄 · `NtAlpcCancelMessage` 반환 표 · rpcrt4 호출 상태 9 ~ 12 · 모듈 ID 24 B 사슬 · 종류 5 본문 · 7 출력 B 배열 개수 칸 → **Q299**.

---

## VM 으로 — 디버거 없이 떠 올 수 있는 것 (2 단계 수집 재료)

- 모은 곳: 결과서 `_workspace/sast/147` ~ `154_*_result.md` 의 "디버거 없이" 절 + run 문서 "아직 확인 필요" 의 같은 절. 겹친 줄은 하나로 합쳤다.
- "없음" 이라고 한 곳: sast/147 · run152 (실행 중 값뿐) · sast/149 · run150 · sast/154 · run155 (VM 쪽 없음).
- "wishlist" 칸 = `../PE_emulator/_workspace/05_tuning_planner/380_vm-collect-wishlist.md` 의 번호. 그 표와 **눈으로 맞춘 것** 이라 "일부" 는 그 줄 범위 안에 든다는 뜻만이다. "reg-users" = 지금 묶음 `…-reg-users` (레지스트리 줄로 이미 뜨는 키 — 값 · 바이트 확인만 더함). "—" = 겹치는 번호 없음.
- 무엇 · 어디는 결과서 · run 문서 글 그대로 (명령 이름 · API 이름), 기대 값은 run 문서의 것.

| # | 무엇 (닫는 것) | 어디 (레지스트리 키 / 명령 / API) | 어느 run (결과서) | wishlist |
|---|---|---|---|---|
| 1 | OLE 키 전체 — **값 이름 · 종류 · 바이트**. `MachineAccessRestriction` (다섯째 조각) 바이트 · `DefaultLaunchPermission` · `MachineLaunchRestriction` 이 NoReg 넷 (124 · 232 · 228) 과 같은가 · `DefaultAccessPermission` 이 정말 없나 · `MachineAccessRestrictionOld` · `MachineLaunchRestrictionOld` 흔적 · `EnableDCOM` · `LegacyImpersonationLevel`. **rpcss 가 한 번 뜬 뒤에 뜰 것** (rpcss 가 등록값을 고쳐 씀) | `reg export HKLM\SOFTWARE\Microsoft\OLE` (REG_BINARY 16진 그대로) 또는 `reg query … /s` | run151 (sast/148) | reg-users (380 ② `[수집]` 셋 값은 이미 있음 — 바이트 · `…Old` 확인만) |
| 2 | `RaiseActivationAuthenticationLevel` 값이 있나 | `reg query "HKLM\SOFTWARE\Microsoft\OLE\AppCompat" /s` | run151 (sast/148) | reg-users |
| 3 | DCOM 정책 키 — 있으면 `MachineAccessRestriction` · `MachineLaunchRestriction` (SDDL 글), 없으면 등록값 쪽이 쓰임 | `reg query "HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM" /s` | run151 (sast/148) | reg-users (380 ②: 묶음에 안 보임) |
| 4 | "AppID 없음" 이 진짜에서도 같나 (실행 파일 이름으로) | `reg query "HKLM\SOFTWARE\Classes\AppID\test.exe"` · `reg query "HKCU\SOFTWARE\Classes\AppID\test.exe"` | run151 (sast/148) | reg-users |
| 5 | OS 종류 (1 = 워크스테이션 → 넷째 조각 AN `0x3`) | `wmic os get producttype` · `systeminfo` | run151 (sast/148) | — |
| 6 | **토큰 — 승격 전 창 · 승격된 창 둘 다**: 그룹 (BUILTIN\Administrators 가 승격 전엔 "Group used for deny only" 인가, 승격 뒤 `Enabled`) · `High Mandatory Level` · 특권 목록 · 사용자 SID (`LookupPrincName` 이름 꼴) | `whoami /all` 두 번 (`whoami /user` 는 그 안에) | run151 (sast/148) · run156 (sast/152) | A3 (+ "뜨기 전에" 3: 승격 안 된 것도) |
| 7 | (선택) SDDL 약어 → SID 대조 (`[규칙]` 약어 표 확인) | PowerShell `ConvertFrom-SddlString` · `sc sdshow` | sast/148 결과서만 | — |
| 8 | UAC 정책 값 (appinfo `LUAGetUACPromptPolicy` 가 읽는 것) | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` 의 `EnableLUA` · `ConsentPromptBehaviorAdmin` · `ConsentPromptBehaviorUser` · `ConsentPromptBehaviorEnhancedAdmin` · `EnableAutomaticAdmin` (+ `PromptOnSecureDesktop` `[추측]`) | run156 (sast/152) | reg-users |
| 9 | verb 키 — `runas` `command` 기본값 · `HasLUAShield` · (`Elevate` 는 없어도 됨) · `open` 에 `DelegateExecute` · `DropTarget` 이 **없나** | `reg query HKCR\exefile\shell\runas /s` · `reg query HKCR\exefile\shell\open /s` | run156 (sast/152) | reg-users (`HKLM\SOFTWARE\Classes`) |
| 10 | appinfo 서비스 — 시작 종류 · 상태 · `ImagePath` · `ObjectName` (LocalSystem?) · `Parameters\ServiceDll` | `sc qc appinfo` · `sc query appinfo` · `reg query HKLM\SYSTEM\CurrentControlSet\Services\Appinfo` | run156 (sast/152) | A7 일부 (서비스 상태) · reg-users |
| 11 | 화면 지표 전부 — `GDIINFO` 를 `StubDisp` 가 채웠나 진짜 드라이버인가 (`HORZSIZE` · `RASTERCAPS` · `PHYSICAL*` 이 0 이고 1024 × 768 이면 StubDisp 쪽) | `GetDeviceCaps(화면 HDC, 0 ~ 121)` | run153 (sast/150) | A21 |
| 12 | **메시지 상자 글꼴과 폭** — 에뮬레이터 `NtGdiGetTextMetricsW` · `NtGdiGetTextExtent` 대답 재료 (`tmHeight` · `tmAveCharWidth` · `tmMaxCharWidth` · `tmPitchAndFamily` · `tmCharSet`, 52 자 · `"OK"` · `"확인"` 폭) | `SystemParametersInfoW(SPI_GETNONCLIENTMETRICS)` 의 `lfMessageFont` → `CreateFontIndirectW` → `SelectObject` → `GetTextMetricsW` · `GetTextFaceW` · `GdiGetCharDimensions` (gdi32 내보내기) · `GetTextExtentPoint32W` · `GetSystemMetrics(SM_CXICON …)` | run153 (sast/150) | A14 일부 (글꼴) |
| 13 | 글꼴 레지스트리 · 파일 (다음 벽 글꼴 열거 재료) | `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts` · `…\FontSubstitutes` · `…\FontLink\SystemLink` · `HKCU\Control Panel\Desktop\WindowMetrics` · `HKCU\Control Panel\Desktop` (`LogPixels` · `Win8DpiScaling` · `DpiScalingVer`) · `C:\Windows\Fonts` 이름 · 크기 | run153 (sast/150) | A14 일부 · reg-users · A9 (파일 목록) |
| 14 | 언어팩 값 전부 (`REG_DWORD` 가 있으면 `gbLpk` 켜짐 → `+0x180094`) | `HKLM\SYSTEM\CurrentControlSet\Control\LanguagePack` (값 이름 · 종류 · 데이터) | run153 (sast/150) | reg-users |
| 15 | 로캘 · 코드 페이지 (`0x409` 인지, `0x1000 → 0xc00` 갈래인지 · `GetACP` 949 면 run153 2절 4e 길) | `GetUserDefaultLCID` · `GetSystemDefaultLCID` · `GetThreadLocale` · `GetUserDefaultLocaleName` · `GetACP` · `GetSystemDirectoryW` | run153 (sast/150) · run157 (sast/151) | A10 |
| 16 | DPI | `GetProcessDpiAwareness` (shcore) · `GetDpiForSystem` · `GetThreadDpiAwarenessContext` | run153 (sast/150) | A21 일부 (DPI) |
| 17 | 필터 값 (test.exe · 일반 GUI exe · Edge 렌더러 비교 — 일반은 0 인가) | `GetProcessMitigationPolicy(ProcessSystemCallFilterPolicy)` | run153 (sast/150) | — |
| 18 | edgegdi api 집합이 있나 (`g_pEdgeGdi`) | `ApiSetQueryApiSetPresence("ext-ms-win-gdi-edgegdi-l1-1-0")` (ntdll 내보내기) · `IsApiSetImplemented` | run153 (sast/150) | — |
| 19 | 스톡 · 시스템 색 · 버전 (세션 틀 스톡 0 · 7 · 13 대조) | `GetStockObject(0 · 7 · 13)` 값 · `GetCurrentObject(새 DC, OBJ_BRUSH · OBJ_PEN · OBJ_FONT)` 비교 · `GetSysColor(1 · 15 · 16 · 20)` · `GetVersionExA` | run153 (sast/150) | — |
| 20 | **MessageBox 반환값 표** (표 C 대조) — 일곱 종류 × `DEFBUTTON1 ~ 4` (+ `MB_HELP` 하나) 에 Enter 한 번 → 반환값 파일 기록. 닫기 (×) · Esc 도 (`MB_YESNO` 는 안 닫혀야) | 시료 exe | run157 (sast/151) | — (B2 의 디버거 없는 몫) |
| 21 | 자식 순서 (템플릿 차례인가 · 메시지 상자는 단추가 먼저인가) | 시료 exe: `WM_INITDIALOG` 에서 `GetWindow(GW_CHILD)` → `GW_HWNDNEXT` (+ `GW_HWNDPREV` 거꾸로) ID · `FindWindow(L"#32770")` 뒤 `EnumChildWindows` / `GetWindow` | run157 (sast/151) | — (B2 의 디버거 없는 몫) |
| 22 | 대화상자 프로시저 메시지 차례 — `WM_PAINT` 횟수 · `WM_ERASEBKGND` · `WM_NCACTIVATE` · `WM_SHOWWINDOW` · `WM_WINDOWPOSCHANGED` · 끝의 `WM_DESTROY` · `WM_NCDESTROY`, 모달 뒤 첫 `GetMessage` 의 `message = 0` | 시료 exe: `DLGPROC` 맨 앞에서 모든 `uMsg` 를 시각과 함께 | run157 (sast/151) | — (B2 의 디버거 없는 몫) |
| 23 | A 판 대화상자 | 시료 exe: `DialogBoxIndirectParamA` 로 `IsWindowUnicode(hDlg)` · `GetWindowLongPtrA/W(GWLP_WNDPROC)` | run157 (sast/151) | — |
| 24 | `GWLP_USERDATA` 왕복 · `GWLP_ID` 쓰기 | 시료 exe: `SetWindowLongPtr(h, GWLP_USERDATA, v)` / `GetWindowLongPtr` · `GWLP_ID` 쓰기 뒤 `GetDlgCtrlID` | run157 (sast/151) | — |
| 25 | **디버그 이벤트 루프** — 이벤트 코드 차례 · `dwProcessId` / `dwThreadId` · `CREATE_PROCESS_DEBUG_INFO` (`hFile` · `hProcess` · `hThread` · `lpBaseOfImage` · `lpStartAddress` = 기준 + 진입점인가) · `LOAD_DLL_DEBUG_INFO` (`lpBaseOfDll` · `lpImageName` 문자열) · 받은 핸들 `GrantedAccess` (기대 `0x12067b` · `0x12007b`) · 자식 `ProcessDebugPort` ≠ 0 · 자식 PEB `+0x2` = 1 · **0xB8 16진** (Timeout 0 → 있으면 0 · 없으면 `0x102`) · `ContinueDebugEvent` 값별 결과 (`0x10002` · `0x80010001` · 그 밖 → `ERROR_INVALID_PARAMETER`, `NtDebugContinue` 직접이면 `0xc000000d`) · 디버그 객체 핸들을 닫으면 자식이 끝나나 (`DebugSetProcessKillOnExit(TRUE)` 짝) | 시료 exe: `CreateProcessA(자식, DEBUG_PROCESS)` + `WaitForDebugEvent` · `NtQueryObject(ObjectBasicInformation)` · `NtQueryInformationProcess(ProcessDebugPort)` · `ReadProcessMemory` · `ntdll!NtWaitForDebugEvent` 직접 · `ContinueDebugEvent` / `NtDebugContinue` · `CloseHandle` | run154 (sast/153) | P5 |
| 26 | (참고만 — 이번 질문엔 필요 없음) `\Sessions\1\Windows` 아래 `SharedSection` (Section) · `ApiPort` (Port) 가 있나 | WinObj 류 객체 이름공간 목록 | sast/147 결과서만 | A1 |

- VM 이 아닌 것 (같이 알림): ① 원문 샘플이 `SHELLEXECUTEINFO.fMask` 에 `SEE_MASK_FLAG_NO_UI (0x400)` 를 쓰나 — **PE_emulator 표본 소스** 쪽 (run156 3-2 의 `GetLastError` 갈림). ② nsi 포트 종류 0 · 1 밖 메시지 — **PE_emulator 로그에 Flags (`a[1]`) · `+0x28` · `+0x18` 을 남기는 한 줄** (run155 덤 3).
- 디버거가 있어야 하는 것은 각 절 "남은 것" 의 대기열 번호 (Q84 · Q253d · Q262 · Q283 · Q280 · Q277 · Q273 · Q297).
