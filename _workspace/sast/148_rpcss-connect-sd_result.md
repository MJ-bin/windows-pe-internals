# 148 rpcss-connect-sd — 정적 분석 결과

- 요청서: `_workspace/sast/148_rpcss-connect-sd.md` (Q288 + Q284a). PE_emulator 원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 의 "② rpcss `_Connect` 응답의 SD 다섯 조각" 절을 끝까지 읽고 시작했다 (읽기만).
- 빌드: Windows 11 24H2 26100.9457 x64. 날짜 2026-11-27.
- 표기: `[정적]` = 코드에 그렇게 쓰여 있다 (실행해서 본 것이 아님). `[규칙]` = 공개 규칙 (SDDL 약어 · COM 권한 비트 · self-relative SD 모양). `[추측]` · `[공백]` 은 따로 표시. 줄 근거 `+0x…` 는 함수 시작 기준, 부르기는 call 자리 (`ret` 은 돌아올 자리), 점프 목적지는 "목적지 − 함수 시작". **인용한 핵심 `+0x…` 30여 줄은 원본 바이트 (`pefile`) 와 한 번 더 맞췄다** (call 은 `e8` 또는 `48 ff 15`, 상수 읽기 · 쓰기는 `48 8b 05` · `4c 89 3d` 등). 불일치 0.
- 폴더 이름: `ms/disassemble/<모듈>/` 는 소문자 모듈 이름 (`rpcss`, `combase`, `ntdll`, `sechost`).
- 도는 작업: 없음 (내 Ghidra 는 전부 끝남). **ntdll 프로젝트는 이 요청에서 내가 열었다가 닫았다 → sast/153 이 열어도 된다.** rpcrt4 · kernelbase 는 열지 않았다 (kernelbase 는 `pefile` 로 export 한 줄만 읽음).
- **판정: 분석성공.** 2 · 1 · 3 · 5 는 줄 근거로 닫았다. 4 · 6 은 "몸 · 입력 출력 칸 · 실패 status" 까지 닫고, 진짜 값 (활성화 "클래스 없음" HRESULT 같은 서버 쪽 값) 은 `[공백]`.

## 스크립트 문제

없다. 쓴 도구 (`run_ghidra.sh export | refs`, `pdb_aliases.py`) 는 그대로 잘 돌았다. 읽을 때 조심할 것 한 가지 (스크립트 잘못 아님, sast/40 의 "전역 주석 주의" 와 같은 모양): rpcss `.asm` 의 `?gStartupSids@@3PAU_STARTUP_SID@@A+0xc8` 는 "SID 표의 칸" 이 **아니라** 바로 뒤 이름 없는 전역 (`0x1644b8`, 추적 수준 비교용 `CMP dword ptr [..],0`) 이다. 표 `gStartupSids` 는 `0x1643f0` 에서 시작하는 16 B × 9칸 (아래 1-3).

## PDB GUID 확인 · 버전 대조

| 모듈 | 바이너리 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 확인 |
|---|---|---|---|
| rpcss | `rpcss.dll` (이미 analyze, 다시 안 함) | `316298244AA2A8A6A520F13E7355EC211` | 디버그 디렉터리 앞 16자 `316298244AA2A8A6` 같음 (sast/144 와 같은 값) |
| combase | `combase.dll` (이미 analyze) | `DA1DC0C4F35CEE58F672CBB20460D11D1` | 앞 16자 `DA1DC0C4F35CEE58` 같음 |
| ntdll | `ntdll.dll` (이미 analyze) | `C3093720177DA6851519DCFFE8DF53FE1` | 앞 16자 `C3093720177DA685` 같음 |
| **sechost (새로 analyze)** | `sechost.dll` | `0E8BAC436697447988FB72C706D1F1631` | 앞 16자 `0E8BAC4366974479` 같음. **분석 증거 셋**: `ms/ghidra/sechost.gpr` · `ms/decomp/sechost/_index.tsv` (1,567줄) · `sechost.analyze.log` 의 `SetPdb: sechost.dll <- …/sechost.dll.pdb`. analyze 14:21:50 → 14:24:14 (약 2분 24초, `MAXMEM=8G`, 시작 전 `free -g` available 28). 이미 있던 PDB (`ms/sym/sechost.dll.pdb`) 재사용 |
| advapi32 | `advapi32.dll` (이미 analyze) | `43EC8DCD3F9CB692FE88343843C328201` | **열지 않았다** — 쓰는 함수가 advapi32 를 안 지난다 (rpcss · combase 는 `sddl` / `lsalookup` api-set 으로 sechost 에 직접 감, 아래 3). 앞 16자만 확인 |

- 커널 PDB GUID (`C29EBFB0…`) 는 이 요청에 커널이 없어 해당 없음.
- "알려진 사실" 표에 rpcss · combase · sechost 줄은 없다. 대신 sast/144 의 오프셋 (`CoInitializeSecurity +0x47d GetConnection`, `rpcss!_Connect +0x40f ~ +0x46c`, `combase!InitializeSystemSecurityDescriptors +0x1e ~ +0x12b`) 이 이 요청의 새 `.asm` 에서 **그대로** 나오고, 원문 (380) 이 말한 죽는 줄 `ntdll!RtlLengthSecurityDescriptor +0x2d` 가 `0f b6 49 01` (`MOVZX ECX,byte ptr [RCX+0x1]` = SID 머리 읽기) 로 **맞다** → 스크립트 · 바이너리 둘 다 믿어도 된다 (일치).

---

## 먼저 읽는 한 줄 답

| # | 물음 | 한 줄 답 |
|---|---|---|
| **2** | 죽는 사슬 | **`CoInitializeSecurity +0x385` (ret `+0x38a`) → `GetDefaultAccessRestrictionsSD +0x2c` (ret `+0x33`) → `GetSecurityDescriptorLength` (KernelBase `0xfd2b0` 은 `jmp [IAT]` 한 줄 → ntdll `RtlLengthSecurityDescriptor`) `+0x2d`.** 읽는 SD 는 **다섯째 조각 `_pDefaultAccessRestrictionsSD` (`0x3380e0`) 가 있으면 그것, 없으면 넷째 `_pNoRegAccessRestrictionsSD` (`0x3380c8`)** — 우리 "최소 대답" 은 다섯째 크기 0 → 건너뜀 → 넷째가 `LocalAlloc(0x40, 0)` 짜리 0 B 덩어리 → 길이 함수가 덩어리 **바깥** 20 바이트 (Control · 소유자 오프셋 …) 를 읽다 AV. 검사는 **없다** (`IsValid…` 안 부름): 길이 재고 → `HeapAlloc` → `memcpy` 만. 조각 안 읽는 것: **명시 길에서는 첫째 · 둘째 · 셋째를 안 읽음** (둘째는 암묵 길만, 아래 표). 암묵 길 (`InitRemoting +0xb6`) 은 **같지 않다**: 같은 `+0x385` 도 타지만 그 앞에 `EOAC_APPID` 로 `GetLegacyBlanket → GenerateDefaultAccessPermissionsSecurityDescriptor` 가 **둘째 조각** 을 읽고 lsapolicylookup 을 **치명적으로** 연다. |
| **1** | SD 출처 | **넷은 전부 (가) 코드 상수 SDDL** — 서비스 시작 때 `rpcss!InitializeSystemSecurityDescriptors` (rva `0x73e04`, `InitializeSCMBeforeListen +0x1a5` 가 부름) 가 `ConvertStringSecurityDescriptorToSecurityDescriptorW` 로 지음. 레지스트리를 **안 본다** (이름이 "NoReg"). 두 조각 (실행 제한 · 접근 제한) 에만 고정 BUILTIN SID 둘 (DCOM Users `S-1-5-32-562` · Performance Log Users `S-1-5-32-559`) 이 들어가고 (가), 접근 제한은 **워크스테이션이면 Anonymous 마스크 0x3 · 서버면 0x7**. 크기 **124 · 124 · 232 · 228 B** (계산, `[수집]` 레지스트리 124 · 232 · 228 과 **크기가 하나씩 정확히 같음**). **다섯째 (`GetDefaultAccessRestrictions`) 는 (나)**: `HKLM\SOFTWARE\Microsoft\OLE\MachineAccessRestriction` (원문 `[수집]` 228 B) 또는 정책 `HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM\MachineAccessRestriction` (SDDL 글) — `ReadPermissions` 가 서비스 시작 때 · 정책 바뀔 때 채움. (다) 기계별 SID 는 **없다**. 크기 0 은 넷은 **불가**, 다섯째만 가능 (둘 다 없거나 모양 검사에 걸리면 NULL). |
| **3** | lsapolicylookup | **명시 길에선 "끼지만 지나감"**: `CoInitializeSecurity +0x2af LookupPrincName(0xa)` → `LookupAccountSidLocalW` (sechost) 가 포트를 연다 (SID → 이름, 내 프로세스 토큰 사용자). 못 열면 (`0xc0000034`) 그 함수가 에러를 돌려주지만 `CoInitializeSecurity` 는 **받아서 삼키고** 기본 보안 배열로 가서 HRESULT 0 으로 계속 간다 (`+0xa53 → 조각 +0x1dbd2d → +0xeb8e`). SD 짓기에 안 쓰이고, 죽음과 **무관** (창구가 없어도 · 있어도 `+0x385` 에서 똑같이 죽는다). **암묵 (EOAC_APPID) 길에선 낌**: `GenerateDefaultAccessPermissionsSecurityDescriptor` 가 `LsaLookupOpenLocalPolicy` 를 직접 불러 도메인 SID 로 ACE 를 만들고, 실패하면 `CoInitializeSecurity` 가 그 에러 HRESULT 로 **끝난다**. → r19 표본 (명시 호출) 에서 LSA1 은 OR1 앞 바퀴가 **아니다**; `CoInitializeSecurity` 없이 COM 을 쓰는 표본 (r18 432) 은 LSA1 이 앞 바퀴. |
| **4** | 프로시저 7 → 6 | 순서가 **뒤집혀 있다**: `CallTheResolver +0x9d JZ → +0x1ad RegisterOXIDAndOIDs` — 아파트의 OXID 가 아직 안 등록 (`OXIDEntry+0xd8 == 0`) 이면 **6 이 먼저, 7 은 이미 등록된 뒤 OID 를 더 받을 때**. 7 = `ServerAllocateOIDs(hRpc, ph, OXID*, cAdd, add[], cWant, out[], *cOut)` → status 5 (ph · PID 안 맞음) · `0x57` (널 인수) · `0x776` (OXID 못 찾음) · `0xe`. 6 = 서버가 OXID_INFO 깃발 · 두 DSA (`DsaValid`) 검사 → `0x57`, ph → 5. combase 쪽 검사는 `RetryRPC · CheckStatus` (sast/144 표 3 그대로) + 6 은 `StartListen` 먼저 · 돌려받은 DSA `DsaValid` 거짓이면 `0x8000ffff`. |
| **5** | `LocalInterfaceOnlySecCallback` | 반환 **0 = 통과 / 5 = 거절**. 거절 조건: (a) 전송이 ncalrpc (`I_RpcBindingInqTransportType == 4`) 가 아님, (b) `RpcImpersonateClient` · `OpenThreadToken` 실패, (c) **클라이언트 토큰이 `gpDefaultAccessRestrictionsSD` (없으면 `gpNoRegAccessRestrictionsSD`) 에서 COM "실행 · 로컬" 권한 (0x2) 을 못 얻음**. 보는 SD 는 바로 **Connect 응답의 다섯째 · 넷째 조각과 같은 것** → 진짜 값이면 Everyone `0x7`/`0x3` 이라 로컬 test.exe 는 **통과**. |
| **6** | 활성화 "클래스 없음" | 이 두 함수 (`BindToSCMProxy` · `MakeSCMProxy`) 는 "클래스 없음" HRESULT 를 **만들지 않는다**: 연결 (`GetConnection +0x17`) → `MakeProxyHelper` 로 ORPC 프록시 만들기 → `CoSetProxyBlanket` 만. `DelegateActivationToSCM` 은 프록시 호출 (`+0x789` 간접 호출, ret `+0x78e`) 이 돌려준 값을 **그대로** 올리고 (`0x8004…` 상수 없음), 값은 rpcss 활성화 서버가 정한다 (이번 범위 밖) `[공백]`. 프록시 만들기 단계 실패는 그 HRESULT (`MakeSCMProxy +0x1b JS`). |

---

## 1. SD 조각 표 (원문 "받을 꼴")

`_Connect +0x40f ~ +0x4ae` 가 만드는 응답 끝 (칸 28 ~ 30). 조각 순서와 칸 (`ulong[5]` 의 번호) 은 `_Connect +0x41d`/`+0x436`/`+0x44b`/`+0x455`/`+0x483` 이 `[RDI+0]·[+4]·[+8]·[+0xc]·[+0x10]` 에 크기를 쓰고, `+0x5ce ~ +0x5ee` 루프가 같은 순서로 이어 붙인다 (`memcpy` ret `+0x5df`, 크기가 0 이면 건너뜀).

| 칸 | 조각 (rpcss 전역) | 출처 | 바이트 / 값 이름 | combase 가 읽는 줄 | 0 가능? |
|---|---|---|---|---|---|
| `[0]` | 실행 권한 `gpNoRegLaunchPermissionsSD` (`0x1656e0`) | **(가)** SDDL 상수 (rpcss `.rdata` rva `0x1472b0`): `O:BAG:BAD:(A;;0x1f;;;BA)(A;;0xb;;;IU)(A;;0xb;;;SY)` | **124 B** (계산). 쓰는 줄 `InitializeSystemSecurityDescriptors +0x391` | combase 전역 `_pNoRegLaunchPermissionsSD` (`0x3380e8`): **초기화 길에선 안 읽음.** 읽는 곳은 공개 API `CoGetSystemSecurityPermissions → GetSystemSecurityDescriptors` (`+0x11b` · `+0x14b`, 종류 0) 뿐 | 서버: **불가**. combase 초기화 길: 0 B 여도 안 읽어 상관없음 (그러나 API 쓰는 프로그램이 있으면 쓰레기). 진짜 바이트를 주는 게 안전 |
| `[1]` | 접근 권한 `gpNoRegAccessPermissionsSD` (`0x1656d8`) | **(가)** `.rdata` `0x147240`: `O:BAG:BAD:(A;;0x7;;;PS)(A;;0x3;;;SY)(A;;0x7;;;BA)` | **124 B**. `+0x43b` | `_pNoRegAccessPermissionsSD` (`0x3380f8`) + 크기 `_dwNoRegAccessPermissionsSDSize` (`0x3380f0`): **암묵 (EOAC_APPID) 길만** `GenerateDefaultAccessPermissionsSecurityDescriptor +0x41` · `+0x48` (등록값 `DefaultAccessPermission` 이 없을 때 — VM 에 없음). 공개 API `GetSystemSecurityDescriptors +0xe2` | 서버 불가. 명시 길 combase: 안 읽어 0 B 여도 됨. **암묵 길: 이 SD 에 ACE 를 덧붙이는 API (`AddAcesToSecurityDescriptorViaApi`) 가 읽으므로 진짜 바이트 필요** |
| `[2]` | 실행 제한 `gpNoRegLaunchRestrictionsSD` (`0x1656c8`) | **(가)** 형식 `.rdata` `0x147320` 에 BUILTIN SID 글 둘: `O:BAG:BAD:(A;;0x1f;;;BA)(A;;0xb;;;WD)(A;;0x1f;;;S-1-5-32-562)(A;;0x1f;;;S-1-5-32-559)(A;;0xb;;;AC)(A;;0xb;;;S-1-15-3-1024-2405443489-874036122-4286035555-1823921565-1746547431-2453885448-3625952902-991631256)` | **232 B**. `+0x4e1` | `_pNoRegLaunchRestrictionsSD` (`0x3380d0`): 공개 API `GetSystemSecurityDescriptors +0xa5` · `+0xd9` (종류 2) 뿐 | 서버 불가. combase 초기화 길: 안 읽음 |
| `[3]` | 접근 제한 `gpNoRegAccessRestrictionsSD` (`0x1656d0`) | **(가)** 형식 `.rdata` `0x147050` (워크스테이션) / `0x146ed0` (서버): `O:BAG:BAD:(A;;0x7;;;WD)(A;;<0x3 또는 0x7>;;;AN)(A;;0x7;;;S-1-5-32-562)(A;;0x7;;;S-1-5-32-559)(A;;0x3;;;AC)(A;;0x3;;;S-1-15-3-1024-…위와 같은 글)` — 고르는 줄 `InitializeSystemSecurityDescriptors +0x277 IsWorkstationOS` (ret `+0x27c`) → `+0x28d … +0x2a1`. `IsWorkstationOS` = `GetVersionExW` 의 제품 종류 1 (`VER_NT_WORKSTATION`, `IsWorkstationOS +0x5f ~ +0x66`) → **Win 11 Education = 워크스테이션 → AN 마스크 0x3** | **228 B**. `+0x577` | **`_pNoRegAccessRestrictionsSD` (`0x3380c8`): `GetDefaultAccessRestrictionsSD +0x16` (명시 · 암묵 둘 다 `CoInitializeSecurity +0x385`)** — 여기서 죽는다. 공개 API `GetSystemSecurityDescriptors +0x65` · `+0x99` (종류 3) | **서버 불가. combase: 반드시 유효한 자기 상대 SD** (다섯째가 비어 있으면 이것이 쓰임) |
| `[4]` | 기본 접근 제한 `GetDefaultAccessRestrictions()` (rva `0x617f4`, 전역 `gpDefaultAccessRestrictionsSD` `0x1656b0`) | **(나)**: ① 정책 `HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM` 의 값 `MachineAccessRestriction` (**SDDL 글**, `ReadPermissions +0x27 ~ +0x77`) 이 있으면 그것 ② 아니면 `HKLM\SOFTWARE\Microsoft\OLE` 의 값 `MachineAccessRestriction` (**REG_BINARY 자기 상대 SD**, `ReadPermissions +0x25a ~ +0x29f`; 값 읽기 `ReadSecurityDescriptor` (rva `0x60010`) `+0xb0 ~ +0x209`: 종류 REG_BINARY · 길이 ≥ 0x28 · 소유자 · 그룹 오프셋 ≠ 0 · `IsValidSecurityDescriptor` · `IsValidCOMSecurityDescriptor`, 그 뒤 `ReadPermissions +0x280 CheckForExpectedAccess` — 거스르면 `HandleInvalidRestrictionFromOleKey` 가 고치거나 NULL) | **VM 값 그대로 (원문 `[수집]` 228 B)**. 둘 다 없거나 검사에 걸리면 **NULL → 크기 칸 `[4]` 안 씀 (`_Connect +0x47b JZ`)** | `_pDefaultAccessRestrictionsSD` (`0x3380e0`): `GetDefaultAccessRestrictionsSD +0xf` 가 **먼저 봄**, 있으면 이것 · 없으면 `[3]`. 크기 0 이면 `InitializeSystemSecurityDescriptors +0xfc` 가 건너뛰어 NULL 로 남음 | **가능** (진짜에서도). 0 이면 combase 는 `[3]` 으로 감 → `[3]` 만 유효하면 통과 |

- **`[수집]` 셋이 어느 조각인가 (원문 질문)**: `DefaultLaunchPermission` (124 B) → `ReadPermissions +0x171 ~ +0x17e` → `SetDefaultLaunchPermissions` (전역 `gpDefaultLaunchPermissionsSD` `0x1656a8`) — **Connect 응답에 안 실림** (활성화 검사 `LaunchOrActivationAllowed` · `IsObjectCreationAllowed` 가 읽음). `MachineLaunchRestriction` (232 B) → `SetDefaultLaunchRestrictions` (`gpDefaultLaunchRestrictionsSD` `0x1656b8`) — **안 실림**. **`MachineAccessRestriction` (228 B) → 다섯째 조각 `[4]`** — 실림. `DefaultAccessPermission` 은 rpcss 는 **안 읽는다** (combase 쪽 `GetLegacyBlanket +0x299` 가 읽음, 없으면 위 `[1]` 로 감). **크기가 하나씩 정확히 같다** (124 = `[0]`, 232 = `[2]`, 228 = `[3]`) `[정적 계산]` → 이 VM 의 등록값은 "NoReg 와 같은 SDDL 을 레지스트리에 써 둔 것" 일 가능성이 크다 `[추측]` (바이트 비교는 VM, 아래).
- **`SOFTWARE\Policies\Microsoft\Windows NT\DCOM` 를 보나**: 예. `ReadPermissions +0x27` (`LEA RDX,[0x180131af0]` = 글 `SOFTWARE\Policies\Microsoft\Windows NT\DCOM`) 로 `RegOpenKeyExW(HKLM, …, 0x20019)` (ret `+0x4b`) → 성공이면 값 `MachineAccessRestriction` · `MachineLaunchRestriction` 을 **SDDL 글** (`ReadSecurityDescriptorFromSDDL`) 로 읽고 `s_fAccessRestrictionsViaPolicy` · `s_fLaunchRestrictionsViaPolicy` 를 켜서 등록값은 건너뜀. 원문 `[수집]` "묶음에 안 보임" = 키 없음 → ② 로.
- **`ReadPermissions` 가 불리는 곳 (서비스 시작 · 이후)**: `ReadRemoteActivationKeys +0x17c` ← `InitializeSCMBeforeListen +0x1b5` (서비스 시작; ret `+0x1ba`) · `TimerCallbackFn +0xe` · `UpdateActSettingsInternal +0x4`, 그리고 정책 알림 `PolicyCallbackFn +0x10`. 즉 다섯째만 **시작 뒤에도 바뀔 수 있다**. 넷은 `InitializeSystemSecurityDescriptors` 가 한 번 짓고 다시 안 쓴다 (`_refs/0x1656e0 ~ 0x1656c8` 의 쓰기는 이 함수 안뿐).
- **`UpdateComSecurityDescriptors` (rva `0x100a18`)**: `HandleInvalidRestrictionFromOleKey` 가 부름. 등록값에 "기대 ACE" 가 없으면 **`MachineAccessRestriction` · `MachineLaunchRestriction` 값을 다시 써 넣는다** (`SetNamedValueSD`, 옛값은 `…Old` 이름으로). → VM 에서 레지스트리는 **rpcss 가 한 번 뜬 뒤에** 읽어야 시작 때 고쳐진 값이 나온다.

### 1-1. 넷의 바이트 (계산) — `[정적: 글 + self-relative 규칙]`

만든 법: SDDL 글 (위 `.rdata`) → sechost `ConvertStringSecurityDescriptorToSecurityDescriptorW (+0x4a)` → `LocalConvertStringSDToSD_Rev1` → `RtlCreateSecurityDescriptor` + `RtlSet{Owner,Group,Dacl}SecurityDescriptor` → **`RtlAbsoluteToSelfRelativeSD` 두 번 (크기 재기 · 채우기)**. 자기 상대 모양은 `ntdll!RtlMakeSelfRelativeSD` 가 정함: **머리 `0x14` → (SACL) → DACL → 소유자 → 그룹** 차례, 머리 `+0` 의 4 바이트 (리비전 · Sbz1 · Control) 를 복사하고 Control 에 `0x8000` 을 or (`RtlMakeSelfRelativeSD +0xf0 · +0x174` , DACL 복사 `+0x118`, 오프셋 쓰는 줄 `+0x129` (DACL `+0x10`) · `+0x14a` (소유자 `+4`) · `+0x16b` (그룹 `+8`)). ACL 리비전 2 (`LocalGetAclForString +0x4a3`), `AclSize` 는 8 에서 시작해 ACE 크기를 더한 값 (`+0x4ac · +0x42f`), ACE = 형 0 (허용) · 플래그 0 · 크기 · 마스크 · SID. SDDL 약어 → SID 는 공개 표 `[규칙]` (BA `S-1-5-32-544`, IU `S-1-5-4`, SY `S-1-5-18`, PS `S-1-5-10`, WD `S-1-1-0`, AN `S-1-5-7`, AC `S-1-15-2-1`). 고정 SID 둘은 `StartTokenHandling +0x1ec ~ +0x216` 가 `CreateWellKnownSid(종류 …)` (call `+0x216`, ret `+0x21d`) 로 짓는 표 `gStartupSids` (`0x1643f0`, 칸 16 B = {종류 `u32`, 도메인 필요 `u32`, 받을 전역 `u64`}) 의 종류 **61** (`WinBuiltinDCOMUsersSid` → 전역 `gSidDCOMUsers` `0x1655b8`) · **58** (`WinBuiltinPerfLoggingUsersSid` → `gSidPerfLoggingUsers` `0x1655a8`) — 둘 다 BUILTIN (도메인 필요 0) 이라 기계마다 같다. 종류 110 칸 (도메인 필요 1) 은 네 SDDL 에 안 쓰인다. `[SID 글 ↔ 종류 번호] [규칙]`.

크기 (SDDL 에서 계산, 등록값과 대조): `[0]` 20 + 소유자 16 + 그룹 16 + ACL 72 = **124**, `[1]` **124**, `[2]` 20 + 32 + ACL 180 = **232**, `[3]` 20 + 32 + ACL 176 = **228**. 소유자 · 그룹 = BA (`S-1-5-32-544`, 16 B), Control = `0x8004` (자기 상대 + DACL 있음), SACL 오프셋 0.

```
[0] LaunchPermissions   (124)
010004805c0000006c00000000000000140000000200480003000000000018001f00000001020000000000052000000020020000000014000b000000010100000000000504000000000014000b0000000101000000000005120000000102000000000005200000002002000001020000000000052000000020020000
[1] AccessPermissions   (124)
010004805c0000006c00000000000000140000000200480003000000000014000700000001010000000000050a00000000001400030000000101000000000005120000000000180007000000010200000000000520000000200200000102000000000005200000002002000001020000000000052000000020020000
[2] LaunchRestrictions  (232)
01000480c8000000d800000000000000140000000200b40006000000000018001f00000001020000000000052000000020020000000014000b000000010100000000000100000000000018001f00000001020000000000052000000032020000000018001f0000000102000000000005200000002f020000000018000b000000010200000000000f0200000001000000000038000b000000010a00000000000f0300000000040000a127608f9abb183463b677ff9dd5b66ce7321a680852439286a61fd898171b3b0102000000000005200000002002000001020000000000052000000020020000
[3] AccessRestrictions  (228, 워크스테이션; 서버는 둘째 ACE (AN) 마스크 03 → 07 한 바이트만 다름)
01000480c4000000d400000000000000140000000200b000060000000000140007000000010100000000000100000000000014000300000001010000000000050700000000001800070000000102000000000005200000003202000000001800070000000102000000000005200000002f0200000000180003000000010200000000000f02000000010000000000380003000000010a00000000000f0300000000040000a127608f9abb183463b677ff9dd5b66ce7321a680852439286a61fd898171b3b0102000000000005200000002002000001020000000000052000000020020000
```

- 이 바이트는 **글 + 규칙으로 계산한 값**이다 (진짜에서 본 것 아님). 같은 일을 하는 계산 스크립트는 스크래치패드 `s148/mksd.py` (커밋 안 함). VM 에서 `combase!_pNoRegAccessRestrictionsSD` 를 덤프해 대조하면 닫힌다 (아래 "디버거 필요").
- 응답 덩어리 = `[0][1][2][3]` 이어 붙임 = **708 B** (다섯째 없음) 또는 `[0][1][2][3][4]` = 708 + 다섯째 크기 (VM 등록값이 228 B 면 **936 B**). 크기 칸 `[0..4]` = `124, 124, 232, 228, (0 또는 228)`, 총 크기 칸 = 708 (또는 936). combase 가 자르는 곳: `InitializeSystemSecurityDescriptors +0x4f · +0x8c · +0xc2 · +0xf4 · +0x12b` (`LocalAlloc(0x40, 크기)` 후 `memcpy(조각, 덩어리 + 누적, 크기)`).

---

## 2. 죽는 사슬 표 (`Connect` 반환 → `RtlLengthSecurityDescriptor +0x2d`) — 명시 `CoInitializeSecurity(SD NULL, cAuthSvc −1, …)` · AppID 없음

호출 인수 차례는 `CoInitializeSecurity` 의 지역 (`RCX` pSecDesc → `[rsp+0x30]`, `EDX` cAuthSvc → `R14D`, `[rbp+0x240]` 인증 수준, `[rbp+0x248]` 가장 수준, `[rbp+0x258]` 능력 비트 → `R12D`). 표의 "조건" 은 이 길에 들어서는 인수.

| # | 줄 | 하는 일 | 조건 · 값 |
|---|---|---|---|
| 0 | 람다 `operator() (rva 0x18b9c0) +0x82e` (ret `+0x833`) 이하 | `Connect` 반환 → 응답 검사 → `InitializeSystemSecurityDescriptors` (combase) → `gGotSecurityData = 1` (`+0xa83`) | sast/144 표 2-2 · 표 3. **응답의 SD 다섯 조각이 여기서 `LocalAlloc(0x40, 크기)` 로 combase 전역 여섯에 복사** (`InitializeSystemSecurityDescriptors +0x36 · +0x6e · +0xa6 · +0xdc · +0x113`, 첫째 조각은 크기만큼, **크기 0 이면 0 B 짜리 비-NULL 할당**) |
| 1 | `GetConnection +0xd2 JNS` → `CoInitializeSecurity +0x482` (ret) · `+0x486 JS` 안 지남 · `+0x48e CMP [gGotSecurityData]` · `+0x494 JNZ → +0xb2` | 본 줄기에 합류 | `Connect` 가 성공 (상태 0) |
| 2 | `+0xb2 ~ +0xe2` | 능력 비트 `& 0xc == 0xc` 면 `0x80070057`; pSecDesc NULL 이면 인증 수준 0 → **2** (`+0xd7 ~ +0xdc`) | 명시 길 (`SD NULL`) |
| 3 | `+0xe2 CALL operator() rva 0x7f438` (ret `+0xe7`) | `RegOpenKeyExW(HKLM, "Software\Microsoft\OLE\AppCompat")` → 값 `RaiseActivationAuthenticationLevel` (`operator() rva 0x7f438 +0x41 · +0x75`, 있고 REG_DWORD 면 `gdwRaiseActivationAuthenticationLevel` `0x334e38` 에 저장) | 원문 로그 `1306 · 1307` 과 같은 일 (값 없음) `[정적 ↔ 요청서 인용]` |
| 4 | `+0xf5 ~ +0x116` · `+0x64a utGetAppIdForModule` (ret `+0x64f`) | `g_AppId == GUID_NULL` 이면 실행 파일 이름으로 `HKCR\AppID\<exe>` 를 찾음 (`+0x657 wCLSIDFromString` 은 찾았을 때만). **AppID 없음 → `g_AppId` 그대로 NULL**, `+0x651 JS → +0x99c` 로 계속. 원문 로그 `1319 · 1320 · 1327 · 1330` 과 같은 일 | `+0x116 JZ → +0x641` |
| 5 | `+0x11c ~ +0x1ae` | 인수 검사 (가장 수준 1 ~ 4 · 인증 수준 ≤ 6 · 예약 인수 NULL). `cAuthSvc == −1` → `+0x1a3 → CAuthInfo::Copy` (ret `+0x1b3`, pAuthList NULL 이면 통과) | 모두 통과해야 다음. 틀리면 `0x80070057` |
| 6 | `+0x1fc … gComLock.Request` (`+0x203`) · `+0x208 CMP [gpsaSecurity],0` | `gpsaSecurity` 가 이미 있으면 `RpcETooLate` (두 번째 호출) | 첫 호출 |
| 7 | **`+0x2a5 ~ +0x2af LookupPrincName(0xa, &이름)`** (ret `+0x2b4`) | **lsapolicylookup 이 끼는 자리 (표 3)**. 성공이든 실패든 `+0x2bb JS` 에서 갈림 → **실패면 `+0xa53 → 조각 +0x1dbd2d` 로 `gpsaSecurity = {wNumEntries 4, wSecurityOffset 2, 0 …}` (0xc B) · `gDefaultService = 1` · `ESI = 0` 으로 `+0xeb8e` 합류** (HRESULT 안 바뀜) | `cAuthSvc == −1` (`+0x29c CMP R14D,EDI`) |
| 8 | `+0x30e ~ +0x369` | `[rsp+0x50]` (복사한 SD, SD NULL 이므로 **NULL**) 이면 유효성 검사 건너뜀. `gAuthnLevel` (`+0x33b`) · `gImpLevel` (`+0x345`) · `gAccessControl` · `_sList` · `gCapabilities` (`+0x362`) · **`gSecDesc` (`+0x369`) = NULL** | |
| 9 | **`+0x385 CALL GetDefaultAccessRestrictionsSD`** (rva `0x7e5f4`, ret `+0x38a`) | `[0x3380e0]` 가 NULL 이면 `[0x3380c8]` 로 (`+0xf · +0x16 · +0x20 CMOVNZ`). 둘 다 NULL 이면 `+0x394 JZ → +0x1dbe65` → **`0x8007000e`** (`R15D`, `+0x296`) | 능력 비트 `& 0x60` 이 0 이면 바로 (`+0x37a JNZ +0xefad` 안 탐) |
| 10 | `GetDefaultAccessRestrictionsSD +0x29 → +0x2c CALL [GetSecurityDescriptorLength]` (ret `+0x33`) | `RCX` = 고른 SD 포인터. **combase 가 임포트한 `api-ms-win-security-base-l1-1-0!GetSecurityDescriptorLength`** → KernelBase `GetSecurityDescriptorLength` (rva `0xfd2b0` = `48 ff 25 …` = `jmp [IAT 0x29af70]`, 그 IAT = **ntdll `RtlLengthSecurityDescriptor`**, `pefile` 로 풂) | api-set 이름은 `apisetschema` 표에서 못 찾음 → "KernelBase" 는 `[추측: 일반 규칙]`, export 한 줄 · IAT 는 `[정적]` |
| 11 | **`ntdll!RtlLengthSecurityDescriptor +0x2d`** | `MOVSX R9D,[RCX+2]` (Control) → `TEST R9W,R9W; JNS` (비트 15 꺼짐 = **절대 형식**: 소유자 포인터를 `[RCX+8]` 에서) / 켜짐 = **자기 상대**: 소유자 오프셋 `[RCX+4]` (≠ 0 이면) `+ RCX` 를 따라가서 `+0x2b TEST RCX,RCX; JZ` (널만 거름) → **`+0x2d MOVZX ECX,byte ptr [RCX+1]`** (SID 부속권한 수) | 0 B 짜리 덩어리 뒤 쓰레기가 비트 15 켜짐 + 소유자 오프셋 ≠ 0 이면 엉뚱한 주소를 읽어 **접근 위반** (원문 관찰: 읽은 주소 `0x1700744cd540`, rip `ntdll+0xe8f3d` = `0xe8f10 + 0x2d` ✓) |
| 12 | (안 죽을 때) `GetDefaultAccessRestrictionsSD +0x38 HeapAlloc(길이)` · `+0x63 memcpy` → `gSecDescRestrictions` (`CoInitializeSecurity +0x38a`) → `+0x39a CalculateAnonymousGrantedRemoteAccessInRestrictions` (ret `+0x39f`, **반환값 안 봄**) → `+0x39f CoVrfCheckSecuritySettings` → `+0x3a4 JMP +0xec38` → 잠금 풀기 → (`g_AppId` 가 NULL 이면 `SetAppID` 안 부름, `+0x3f6 ~ +0x417 JZ → +0x443`) → **HRESULT 0** | 진짜 SD 면 여기까지 간다 |

### 2-1. 길이 함수 · 호출 둘레의 "검사" 요약

- `GetDefaultAccessRestrictionsSD` 는 **검사가 없다**: NULL 이면 NULL 반환 (→ `0x8007000e`), 아니면 `GetSecurityDescriptorLength` → `HeapAlloc(길이)` → `memcpy`. `IsValidSecurityDescriptor` 같은 건 부르지 않는다. 실패 HRESULT 가 나는 길은 둘뿐: 둘 다 NULL → `0x8007000e`, `HeapAlloc` 실패 → NULL 반환 → 같은 `0x8007000e` (`+0x394 JZ`).
- `RtlLengthSecurityDescriptor` 가 보는 칸 (자기 상대 SD): `+2` Control · `+4` 소유자 오프셋 · `+8` 그룹 오프셋 · `+0xc` SACL 오프셋 (Control & 0x10) · `+0x10` DACL 오프셋 (Control & 0x4); 각 SID 는 `[+1]` 개수, ACL 은 `[+2]` 크기 (`+0x7d · +0xa4`) 를 읽음. 반환 = `0x14 + SID 둘 (각 8 + 4·개수, 4 정렬) + ACL 둘 (크기, 4 정렬)`. 우리 바이트 `[3]` 으로는 `0x14 + 16 + 16 + 176 = 228` 이 나와 크기 칸과 맞는다.
- `CalculateAnonymousGrantedRemoteAccessInRestrictions` 는 익명 토큰 가장 · `CallAccessCheck (rva 0x7fcc4)` 로 **같은 SD 에 접근 검사** 를 해 `gAnonymousAccessAllowed` 를 정하는데 (`CalculateAnonymous… +0xdb`) 실패해도 `CoInitializeSecurity` 는 **반환값을 안 본다**. 그래서 SD 가 유효하지만 소유자 · 그룹이 없는 등 검사가 실패하는 모양도 HRESULT 를 안 바꾼다 (토큰 · 접근 검사 syscall 은 에뮬레이터가 에러만 안 내면 됨 `[추측: 어떤 syscall 인지는 이번에 안 따라감]`).

### 2-2. 금지 꼴 (접근 위반 · 실패 HRESULT 가 나는 응답)

1. **칸 `[3]` (넷째) 크기 0 (또는 덩어리 NULL 인데 크기 > 0)**, 다섯째 `[4]` 도 0 — `+0xf` 가 NULL → `+0x16` 의 0 B 짜리 비-NULL 을 골라 **쓰레기를 읽다 접근 위반** (원문 관찰).
2. **조각 안 모양이 틀림**: Control 비트 15 켜짐 + 소유자 · 그룹 오프셋이 조각 밖 → 접근 위반 (`+0x2d`/`+0x52`). Control 비트 15 꺼짐 (절대 형식) 이면 `[+8]` · `[+0x10]` 포인터를 **주소로** 따라감 → 접근 위반. **자기 상대로 줄 것.**
3. **크기 합 > 총 크기 · 덩어리** (sast/144 금지 5): combase `memcpy` 가 덩어리 밖을 읽음 (읽기 초과; 이 경우 안 죽을 수도 있지만 쓰레기 SD).
4. **다섯째가 비-NULL 인데 쓰레기**: `[4]` 가 있으면 `[3]` 대신 이것이 쓰임 → 같은 사슬로 죽음. 모르겠으면 **`[4]` = 0** (아래 PE_emulator 쪽).
5. 참고: 조각이 combase 전역에서 **NULL 로 남는 일은 크기 0 으로는 안 생긴다** — `LocalAlloc(0x40, 0)` 이 NULL 을 안 주므로 "크기 0" 은 늘 "0 B 짜리 비-NULL" (위 1 의 꼴) 이다. `LocalAlloc` 자체가 NULL 이면 `InitializeSystemSecurityDescriptors +0x14e` 가 `0x8007000e` 로 끝난다.

### 2-3. 암묵 길 (`InitRemoting +0xb6 InitializeSecurity → CoInitializeSecurity`) 이 같은가

**아니다.** `InitializeSecurity.asm +0x1c ~ +0x7c` 가 `CoInitializeSecurity(NULL, −1, NULL, NULL, 인증수준 1, 가장 2, NULL, 능력 **8 (EOAC_APPID)**, NULL)` 로 부른다 (sast/144 4-c). 달라지는 곳:

| 단계 | 명시 (SD NULL, 능력 0) | 암묵 (능력 8) |
|---|---|---|
| 능력 `& 8` (`+0xeb TEST R12B,8`) | 안 탐 | **`+0xee4f → GetLegacyBlanket (rva 0x80200)` (`+0x5e7`, ret `+0x5ec`)** |
| `GetLegacyBlanket` | — | AppID 없음 → 모듈 이름으로 `HKCR\AppID\<exe>` 열기 실패 → `HKLM\SOFTWARE\Microsoft\OLE` 값 **`DefaultAccessPermission`** (`+0x299` `GetRegistrySecDesc`) 읽기 → 값 없음 → `+0x4ad → +0x4b7 GenerateDefaultAccessPermissionsSecurityDescriptor(NULL, …)` |
| `Generate…` | — | `RCX == NULL` 이면 **`_pNoRegAccessPermissionsSD` (`0x3380f8`) + 크기 `0x3380f0`** 을 기준 SD 로 (`+0x41 · +0x48`). 보통 Win 32 데스크톱 프로세스 (IL < `0x2100`, 다중 세션 SKU 아님) 면 **`LsaLookupOpenLocalPolicy` (조각 `+0x1d9799`) → `LsaLookupGetDomainInfo(핸들, 5)` → `CreateWellKnownSid(110, 도메인 SID)` ACE** 를 덧붙임 (`+0x10b JNZ → +0x1d9774`) → `AddAcesToSecurityDescriptorViaApi (+0x122)` (기본 SD 에 ACE 5 ~ 6 개 덧붙임) 또는 (그 갈래들이 다 꺼지면) `CopySelfRelativeSd (+0x1ed)` |
| 포트 실패 | — | `Return_NtStatus` → **`CoInitializeSecurity` 가 그 에러로 끝남** (`GetLegacyBlanket +0x4bc → +0x2c1`, `CoInitializeSecurity +0x5f3 JS → +0xf0fb`, 값은 wil 변환 `[공백: 정확한 HRESULT]`). **명시 길과 반대로 치명적** |
| 이후 (`+0x30e …`) | 같음 | 같음 — `+0x385 GetDefaultAccessRestrictionsSD` 까지 똑같이 간다 (`gSecDesc` 만 `GetLegacyBlanket` 이 만든 SD) |

→ 암묵 길은 **둘째 조각 (`[1]`, 124 B) 이 쓰이고 · lsapolicylookup 을 반드시 연다.** 원문 로그 (`seq 1217` 명시 `CoInitializeSecurity(SD NULL, −1, …)`, `1339 ~ 1353` 연결 · `1354` 죽음) 는 명시 길 (표 2 의 7 → 9) 과 맞는다: `LookupPrincName` 의 연결 시도 → (실패 삼킴) → `+0x385` 에서 죽음 `[정적 ↔ 요청서 인용: 로그 줄 번호는 확인 못 함 — 지워진 로그]`.

### 2-4. `GetSystemSecurityDescriptors +0xf` 본문 (원문 "본문 포함")

공개 API `CoGetSystemSecurityPermissions(COMSD 0..3, PSECURITY_DESCRIPTOR*)` (`+0xf TAILJMP`, 종류 `> 3` 이면 `0x80070057`) 의 일꾼. `+0xf GetConnection` (ret `+0x14`, 실패면 그 HRESULT) → `_dwFlags & 1` 이 꺼져 있으면 `0x80004005` (`+0x16f`) → 종류별로 넷 중 하나 (`0` 실행 권한 `0x3380e8` · `1` 접근 권한 `0x3380f8` · `2` 실행 제한 `0x3380d0` · `3` 접근 제한 `0x3380c8`; **다섯째 `0x3380e0` 는 안 줌**) 를 `GetSecurityDescriptorLength` → `LocalAlloc` → `memcpy` 로 복사해 돌려줌. **초기화 길 (`CoInitializeSecurity` · `CoInitializeEx`) 에서는 안 불린다** (`refs 0x18db10` 의 부르는 곳은 `CoGetSystemSecurityPermissions +0xf` 하나). 죽기 전에 불리지 않는다는 원문 `[모름]` 은 닫힘.

---

## 3. `lsapolicylookup` 한 줄 · 사슬

**한 줄**: `\RPC Control\lsapolicylookup` 은 SID ↔ 이름 찾기 (LSA lookup) 포트다. 명시 `CoInitializeSecurity(SD NULL, −1)` 에서는 **`LookupPrincName (+0x2af) → LookupAccountSidLocalW (sechost) → 포트 열기`** 로 끼지만, 실패하면 `CoInitializeSecurity` 가 **삼키고 지나가** (기본 보안 배열 · HRESULT 0 유지) SD 짓기와 무관하게 `+0x385` 에서 죽는다. **→ 명시 길이 막힌 r19 표본에선 LSA1 이 OR1 의 앞 바퀴가 아니다. 암묵 길 (`CoInitializeSecurity` 없이 COM 사용) 에선 낌 (치명).**

| 단계 | 줄 | 설명 |
|---|---|---|
| 1 | combase `LookupPrincName +0x5e GetSelfSid` (ret `+0x63`) · `+0x6b SuspendImpersonate` (ret `+0x74`) | 내 프로세스 토큰의 사용자 SID (원문 `1310 ~ 1313 · 1333` "프로세스 토큰 · 토큰 사용자" 와 같은 일 `[공백: 1310 ~ 1313 이 정확히 어느 줄인지는 못 맞춤]`) |
| 2 | `LookupPrincName +0x116 CALL [LookupAccountSidLocalW]` (ret `+0x11d`; 실패 `+0x124 JZ → 조각 +0x1db342` 로 **한 번 더**) | 인증 서비스 번호가 `0xa` (WINNT) 일 때만 이 길 (`+0x82 CMP R12W,0xa; JNZ +0x1f9` 는 LocalSystem · NetworkService 이름 길). `0xa` 이므로 `LookupAccountSidLocalW` **최대 두 번** (크기 읽고 다시) |
| 3 | sechost `LookupAccountSidLocalW +0x20` (ret `+0x25`) → `LookupAccountSidInternal +0xdd` (ret `+0xe2`) | SID 캐시 (`LsaLookupClientSidCacheEnabled`) 가 꺼져 있으면 (`+0x33 CMP byte [..444],AL`, 처음 값) 곧장 `LsaLookupOpenLocalPolicy` |
| 4 | sechost `LsaLookupOpenLocalPolicy +0x42` (ret `+0x49`) `NdrClientCall3` | 바인딩 핸들은 일반 핸들 콜백 `PLSA_LOOKPR_SERVER_NAME_bind +0x3f` (ret `+0x46`) 가 `RpcStringBindingComposeW(프로토콜 "ncalrpc" (sechost `.rdata` `0x80518`), 끝점 "lsapolicylookup" (`0x80c50`))` → `RpcBindingFromStringBindingW` (`+0x56`) 로 만듦. 즉 **`ept_map` 없이 곧장 ALPC 연결** (원문 "연결 8 번" 의 정체) |
| 5 | 실패 (포트 없음 → RPC 예외 → NTSTATUS `0xc0000034` 계열) | `LookupAccountSidInternal +0xe4 JS → +0x26c RtlNtStatusToDosError → SetLastError → FALSE` (`+0x28a`) |
| 6 | `LookupPrincName` 결과: 두 번째도 실패 → `GetLastError | 0x80070000` (`조각 +0x1db35e ~ +0x1db360`), `+0x133 TEST EBX,EBX` → 이름 버퍼 해제 · **에러를 돌려줌** | |
| 7 | `CoInitializeSecurity +0x2b9 TEST EAX,EAX; +0x2bb JS → +0xa53` | **여기서 삼킴**: 로그 · 중단점 훅만 (`BreakOnCoInitializeSecurityRuntimeFailureIfRequested` 는 이 갈래에선 안 부름) 하고 `+0xa79 조각`: `HeapAlloc(0xc)` → `gpsaSecurity = {0x20004, 0}` · `gDefaultService = 1` · `ESI = R14D (0)` → `+0xeb8e` |

- "8 번" 의 이유: sechost 안에는 재시도 반복이 **없다** (호출 두 번 × `NdrClientCall3` 한 번씩). 나머지 (호출당 4번 연결) 는 rpcrt4 가 `ncalrpc` 연결을 다시 시도하는 것으로 읽히지만 rpcrt4 를 이번에 안 열었다 `[공백]`.
- 서버 쪽: 진짜 rpcss 도 시작 때 같은 포트를 연다 — `CommonServiceMain +0x15a StartTokenHandling` → `LsaLookupOpenLocalPolicy` (`+0x70`, ret `+0x7b`) → `LsaLookupGetDomainInfo(핸들, 5)` (`+0xfb`) → `gStartupSids` 의 `CreateWellKnownSid` 표 채움. 이것은 **서버의 일** (에뮬레이터가 rpcss 를 흉내내므로 불필요) `[참고]`.

---

## 4. Q284a — 표

### 4-1. 프로시저 7 (`ServerAllocateOIDs`) · 프로시저 6 (`ServerAllocateOXIDAndOIDs`)

| 항목 | 프로시저 7 (rpcss `_ServerAllocateOIDs` rva `0x213e0`, combase `CallTheResolver` rva `0x37608`) | 프로시저 6 (rpcss `_ServerAllocateOXIDAndOIDs` rva `0x13990`, combase `ServerRegisterOXID` rva `0xa1e68`) |
|---|---|---|
| 부르는 줄 | `CallTheResolver +0x106` (ret `+0x10d`) `NdrClientCall2`, 서식 `0x2c2994` (combase `.rdata`) | `ServerRegisterOXID +0xce` (ret `+0xd5`) `NdrClientCall2`, 서식 `0x2c292e`. 그 앞 `+0x30 StartListen` (ret `+0x35`, 음수면 그 HRESULT, 줄 `0x496`). 윗길: `CallTheResolver +0x1b7 → RegisterOXIDAndOIDs +0x78` |
| 언제 | **아파트 OXID 가 이미 등록**된 뒤 (`+0x94 MOV AL,[OXIDEntry+0xd8]; +0x9d JZ` 안 탐) | **미등록** (`+0xd8 == 0`, `+0x9d JZ → +0x1ad`) — **첫 부름은 이쪽** `[정적: 분기. 초기값이 0 인지는 OXIDEntry 생성자를 안 읽음 → 추측]` → **sast/144 4-c "7 → 6" 정정 필요** |
| 입력 (스택 칸) | RCX hRpc (`[gResolver+0x10]`) · RDX `ph` (`_ph`, Connect 가 준 컨텍스트 핸들) · R8 `OXID*` (8 B, `[OXIDEntry+0xc8]`) · R9D `cOidsToAdd` (`[아파트+0xd8]`) · `[+0x20]` `add[]` (`[아파트+0xe0]` 에서 `cOidsToAdd×8` 복사) · `[+0x28]` `cOidsWanted` (= `0x14 − [아파트+0x30]`, **한 아파트 OID 버퍼 20 개 중 빈 칸 수**) · `[+0x30]` 출력 `OID*` (`[rsp+0x60]`) · `[+0x38]` 출력 `ULONG*` (`[rsp+0x50]`) | hRpc · `ph` · 출력 `OXID*` · 출력 `…*` · **`OXID_INFO` (0x98 B, `FillOXID_INFO` 가 채움)** · **로컬 프로세스 DSA (`gpsaCurrentProcess`)** · **보안 DSA (`gpsaSecurity`, CoInitializeSecurity 가 만든 것)** · OID 입 · 출력 개수와 배열 |
| 서버 검사 · status | `ReferenceProcess(ph)` NULL → **5**; `I_RpcBindingInqLocalClientPID` 실패 → 그 에러; `[CProcess+0x58]` (등록 PID) ≠ 클라이언트 PID → **5**; 이어 `ServerAllocateOIDsInternal (rva 0x26700)`: `OXID*` NULL · 출력 포인터 NULL · (`cWant>0` 인데 출력 NULL) → **`0x57`**; 서버 OXID 표에 **없음 → `0x776`** (`+0x143`); 메모리 → **`0xe`** (`+0x1a8`) / `8` | `OXID_INFO` 깃발 (`[+0x38] & 0xe07fffff ≠ 0`, 종류 비트 `0xf800000` 이 0 · 모양 틀림) → **`0x57`** (`+0x3b2`); `CheckLocalSecurity(hRpc, ph)` NULL → **5** (`+0x640`); **`DsaValid` 로컬 DSA 거짓 · 보안 DSA 거짓 → `0x57`** (`+0x47c` · `+0x650`); 그 밖은 `CServerOxid::Create` (`+0x20a`, ret `+0x20f`) 가 돌려준 상태 |
| 출력 (칸 · 값) | 새 OID 배열 (요청한 수만큼, 서버가 `ProcessPrng` 으로 8 B 난수 지음 `+0x198`) · 개수 (`*pcNew`) · **상태 0** | `OXID` (`[CServerOxid+0x18]`) · (선택) 서버 DSA 한 개 (**NULL 가능**) · OID 배열 · 상태 |
| combase 가 하는 검사 | `+0x117 RetryRPC` · `+0x122 CheckStatus` (sast/144 표 3: `0x6bb` 만 재시도 · 양수 N → `0x8007NNNN`) → 음수면 `+0x1cd Return_Hr(줄 0x4e3)` 후 그대로 반환. **성공이면 `+0x150 ~ +0x17b`**: `min(돌려준 개수, 빈 칸 수)` 개를 `memcpy(아파트 OID 버퍼 + 현재 개수)` (call `+0x172`, ret `+0x177`) 하고 `[아파트+0x30] += 돌려준 개수` (**잘라 더하지 않음**: 서버가 요청보다 많이 주면 개수가 20 을 넘는다) | `+0xf2 RetryRPC` · `+0x101 CheckStatus` (음수면 `+0x188 Return_Hr 줄 0x4b3`); **돌려받은 DSA 가 비-NULL 이면 `DsaValid` 거짓 → `0x8000ffff`** (`+0x115 · +0x1e1`, 줄 `0x4b8`); `IUpdateResolverBindings` 음수면 그 HRESULT (`+0x12f · +0x1f3`, 줄 `0x4b9`); 이어 `RegisterOXIDAndOIDs +0x87 ~ +0xae` 가 `[아파트+0xc8] = OXID` · `[+0xd0] = gLocalMid` · `[+0xd8] = 1` (등록 표시); 기존 OXID 와 다르면 `MicrosoftTelemetryAssertTriggeredNoArgs` 호출만 (HRESULT 안 바뀜) |
| 안전한 최소 대답 | status 0 · 개수 0 (요청 수 ≤ 개수 같게 주면 `memcpy` 크기 0) 가능 `[추측: 그 뒤에 OID 가 모자라는 부름이 어떻게 되는지는 안 따라감]`; 솔직한 실패는 `0x776` (→ `0x80070776`) | status 0 · OXID 0 이 아닌 값 · DSA NULL. 서버가 `DsaValid` 로 보안 DSA 를 **검사하므로** 우리가 서버 흉내면 combase 가 보낸 `gpsaSecurity` 가 4 항목 기본 모양 `{wNumEntries 4, wSecurityOffset 2, …}` 라는 점만 알면 된다 (서버 흉내가 `0x57` 을 내면 `CoCreateInstance` 가 `0x80070057`) |

### 4-2. `LocalInterfaceOnlySecCallback` (rpcss rva `0x1fbf0`) — 로컬 클라이언트를 거절하는 조건

`RpcServerRegisterIf3(…, 콜백)` 의 보안 콜백 (`StartObjectExporter +0xb65`, sast/144 1-4). 반환 **0 = 통과, 5 (`RPC_S_ACCESS_DENIED`) = 거절**.

| # | 줄 | 보는 것 | 거절 값 |
|---|---|---|---|
| 1 | `+0x46 I_RpcBindingInqTransportType` (ret `+0x5d`) → `+0x6a CMP [..],4` | 전송이 **ncalrpc (4)** 인가 | 아니면 5 (`+0x556 → +0x570 MOV EAX,5`) |
| 2 | `+0xa0 RpcImpersonateClient` (ret `+0xb7`) → `+0xff OpenThreadToken(MAXIMUM_ALLOWED 0x2000000)` (ret `+0x106`) → `+0x116 RpcRevertToSelfEx` → `+0x12c LookupOrCreateTokenFromHandle` (rva `0x20990`) | 클라이언트 토큰 | 실패 → 에러 · 5 (`0x6e4` 는 5 로 고침 `+0x5a5 ~ +0x5ac`) |
| 3 | `+0x156 [gpClientLock 공유] → [0x1656b0] gpDefaultAccessRestrictionsSD` (**없으면 `+0x586` `[0x1656d0] gpNoRegAccessRestrictionsSD`**) | 접근 제한 SD (= Connect 응답 `[4]` / `[3]`) | |
| 4 | `+0x1d4 GetSecurityDescriptorDacl` (ret `+0x1db`) → DACL 없음 (`[rbp-0x7c]==0`) 이거나 NULL DACL 이면 **무조건 통과** (`+0x1e8 JZ → +0x27f`) | | DACL 읽기 실패 → 거절 |
| 5 | `+0x242 AccessCheckByType(SD, 토큰, MAXIMUM_ALLOWED, sGenericMapping `0x164948` …)` (ret `+0x249`) | 얻은 권한 비트 | 호출 실패 (`+0x252 JZ → +0x359`) → 거절 |
| 6 | `+0x25d ~ +0x274`: `granted & 0x7f` 에서 **비트 1 (`0x2` = COM "실행 · 로컬")** 가 켜져 있거나, 구체 비트 (`0x7e`) 가 하나도 없이 비트 0 (`0x1`, 옛 "실행") 만 켜진 경우 통과 | | 못 얻으면 5 (`+0x145 MOV EBX,5` 로 시작해 통과 갈래만 `+0x40f XOR EBX,EBX` 로 0, 반환 `+0x44b MOV EAX,EBX`) |
| 7 | `+0x2a6 CMP [gfCheckActivationSecurity]` · `+0x2b7 ~ +0x2c8` RCX 가 가리키는 16 B 가 GUID `00000136-0000-0000-C000-000000000046` 과 같을 때만 **둘째 검사** (`gpDefaultLaunchRestrictionsSD` `0x1656b8` · 없으면 `gpNoRegLaunchRestrictionsSD` `0x1656c8`, 비트 3 `0x8` "활성화 · 로컬") | | **ILocalObjectExporter (`e60c73e6-…`) 는 GUID 가 달라 이 갈래를 안 탄다** `[추측: RCX = 인터페이스 UUID 포인터]` |

→ 실제 VM 에서 test.exe (일반 사용자 · 중간 IL) 는: Everyone ACE `0x7`/`0x3` 에 비트 1 이 있어 **통과** (진짜 MachineAccessRestriction 도 `[3]` 과 같은 ACL 로 보임). 거절되는 토큰 모양은 (a) ncalrpc 가 아닌 전송, (b) ACE 에 자기 SID · Everyone 이 모두 없는 토큰, (c) 익명 (AN 은 `[3]` 에서 워크스테이션 0x3 → 통과, 서버 0x7 → 통과). 즉 **로컬 클라이언트를 흉내낼 때 이 콜백은 걸리는 곳이 거의 없다** — 에뮬레이터가 rpcss 서버를 흉내내면 이 콜백을 구현할 필요가 없고, 만약 거절 흉내를 낸다면 반환 5 → combase `CheckStatus(5) = 0x80070005` → `GetConnection +0xc5` 갈래 (sast/144 표 3).

### 4-3. `MakeSCMProxy` · `DelegateActivationToSCM` — 활성화가 "클래스 없음" 으로 끝나는 길

| 줄 | 하는 일 | 값 |
|---|---|---|
| `DelegateActivationToSCM +0x156` (ret `+0x15b`) → `BindToSCMProxy (rva 0xa1440) +0xc` `InitChannelIfNecessary` → `+0x17 GetConnection` (ret `+0x1c`) → `+0x38 CMP [_pSCMProxy],0` → **`+0x49 MakeSCMProxy`** (ret `+0x4e`) | 연결 먼저 (표 2 와 같은 `Connect` — 이미 성공했으면 `_dwFlags & 1` 로 건너뜀), 프록시가 없으면 만든다 | 어느 단계든 음수면 그 HRESULT 로 `BindToSCMProxy` 반환 (`+0x1e JS → +0x60`) |
| `MakeSCMProxy +0xc MakeProxyHelper(_pdsaLocalResolver 쪽 DSA, IID_ISCMLocalActivator, …)` (rva `0x94284`, 안 읽음) → `+0x1b JS` | ORPC 객체 참조 풀어 프록시 만들기 | 음수면 그 HRESULT (`+0x1b JS → +0x63e`) `[공백: MakeProxyHelper 안]` |
| `MakeSCMProxy +0x1d ~ +0x5f` `CoSetProxyBlanket(프록시, −1, −1, 주체 …, 인증 수준 6, 가장 3, …, 능력 `0x40` (cloaking 비트가 있으면))` | 보안 담요 | 음수면 프록시 해제 (`+0x7e ~ +0x8d` `Release` 가상 호출 · `[RDI]=0`) 후 그 HRESULT |
| `DelegateActivationToSCM +0x16b SetImpersonatingFlag` → `+0x759 GetSCM (rva 0x11eb2c)` → `+0x789` **간접 호출 (`indirect:cfg:?`)** (ret `+0x78e`) → `+0x7aa Return_Hr` (음수일 때) | **실제 활성화 요청** = `ISCMLocalActivator` 메서드 (ORPC) | 돌아온 HRESULT 를 **그대로** 올린다. **combase 안에 `0x80040154` 같은 "클래스 없음" 상수가 없다** (이 두 함수 · `BindToSCMProxy` · `MakeSCMProxy` · `CallTheResolver` 의 `.asm` 에서 `0x8004…` 상수 0개) → "클래스 없음" 은 **rpcss 활성화 서버가 정해 보내는 값** `[공백: 서버 몸은 이번 범위 밖 (ISCM 프로시저, sast/144 표 4-e 의 ISCM)]` |
| `DelegateActivationToSCM +0x22f MOV EBX,0x8007000e` (ret 앞 `+0x226 GetEnvBlock`) | 환경 블록 짓기 실패 | `0x8007000e` |

---

## "지금 PE_emulator 맞나 · 틀리나" — 쓸 값

읽은 것: 원문 380 "② … 이미 아는 것" 과 run149 2-2 칸 표 (sast/144 표 2-2) 만. `rpcss.cpp` · `alpc_frontend.cpp` 는 안 읽음 (읽기 범위 밖).

| 우리 지금 (원문 인용) | 진짜 (정적) | 판정 |
|---|---|---|
| 응답 칸 28 ~ 30 = "SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL" (run149 "최소 대답" 3) | `[3]` 이 크기 0 이면 combase 가 `0 B 비-NULL 할당` 을 만들고 `CoInitializeSecurity +0x385` 에서 길이 함수가 **쓰레기를 읽다 접근 위반** | **틀림** (원문 BG05 관찰과 일치). sast/144 의 "(라) 0 ×5 가능" 도 **정정**: "`InitializeSystemSecurityDescriptors` 안은 통과하지만 뒤의 `GetDefaultAccessRestrictionsSD` 가 넷째 조각을 읽는다" |

**칸 28 ~ 30 에 쓸 값 (`[정적]` 바이트 — 위 1-1 의 16진)**

| 칸 | 값 |
|---|---|
| 28 `ulong[5]` | **`{124, 124, 232, 228, 0}`** (다섯째 0 = "정책 · 등록값 없음" 으로 진짜에서도 가능 `[정적]`). 진짜 VM 에 더 가깝게 하려면 `{124, 124, 232, 228, 228}` + 다섯째 바이트 = VM 의 `HKLM\SOFTWARE\Microsoft\OLE\MachineAccessRestriction` (`[수집]`, 아래 레지스트리 재료) |
| 29 총 크기 | **708** (다섯째 0) / **936** (다섯째 228) |
| 30 덩어리 | 1-1 의 `[0][1][2][3]` 16진 연결 (+ 다섯째) — **자기 상대 SD 그대로**, 조각 사이 패딩 없음 |

- 다섯째 `0` 을 권하는 이유: combase 는 다섯째가 없으면 넷째를 쓰므로 `gSecDescRestrictions` 가 `[3]` 의 바이트 그대로가 되고, VM 의 `MachineAccessRestriction` 이 `[3]` 과 같다면 (크기 일치) 결과가 같다 `[추측: 바이트 일치]`. VM 값이 다르면 5번째에 그 값을 넣는다.
- **stub 이 채울 최소 목록 (run149 목록에 이어)**: ① 위 SD 다섯 조각 (칸 28 ~ 30) — **이것이 `Connect` 가 서려면 필요한 마지막 칸**. ② 그 밖은 run149 3. 값 그대로 (`gSecDescRestrictions` 가 서면 `CoInitializeSecurity` 는 HRESULT 0 으로 끝남, 이후 단계는 `CalculateAnonymousGrantedRemoteAccessInRestrictions` 의 토큰 · 접근 검사 syscall 이 **에러여도 HRESULT 에 영향 없음**). ③ **그 다음 벽은 첫 COM 사용 때 프로시저 6** (표 4-1: `StartListen` 이 먼저 — 에뮬레이터가 `rpcrt4` 서버 쪽 `RpcServerUseProtseq`/`RpcServerRegisterIf` 호출을 소화해야 함 `[추측: StartListen 안은 안 읽음]`) → `status 0 · OXID ≠ 0 · DSA NULL`.
- **3 이 "낌" 인가**: **명시 호출 표본은 안 낌 (LSA1 은 OR1 앞 바퀴 아님)**. `CoInitializeSecurity` 없이 COM 을 쓰는 표본 (r18 432, 암묵 길) 은 `\RPC Control\lsapolicylookup` 창구 (LSA 도메인 정보 `LsaLookupGetDomainInfo(5)` 포함) 가 **먼저** 필요하다 — 이 경우 LSA1 이 OR1 앞 바퀴.
- **`LocalInterfaceOnlySecCallback`**: 우리가 rpcss 서버를 흉내내므로 구현 불필요. 다만 서버 흉내가 `ph` 를 거르려면 5 를 내는 조건 (PID 불일치 · ph 모름) 은 프로시저 6 · 7 에서 같다 (표 4-1).
- **금지 꼴 추가**: 칸 `[3]` 0 · 쓰레기 · 절대 형식 SD (2-2).

---

## 정적으로 못 닫은 것

- `[공백]` 위 SD 바이트의 **진짜 값**: 글과 `RtlMakeSelfRelativeSD` 규칙에서 계산했을 뿐 (ACL 크기 · 순서 · Control `0x8004` 는 코드로 읽음, 약어 → SID 는 공개 표). 어긋날 수 있는 곳: `RtlAbsoluteToSelfRelativeSD` 가 SACL 이 없을 때 쓰는 값 (0), ACL 리비전 (`LocalGetAclForString` 의 개체 · 콜백 ACE 분기로 바뀌지 않는다고 봄).
- `[공백]` `lsapolicylookup` 연결이 8 번인 이유 (rpcrt4 재시도, 이번에 rpcrt4 를 열지 않음).
- `[공백]` 암묵 길 포트 실패 시 `CoInitializeSecurity` 의 **정확한 HRESULT** (wil `Return_NtStatus` 변환, RPC 예외 → NTSTATUS 가 무엇인지).
- `[공백]` `MakeProxyHelper` · `ISCMLocalActivator` 서버 몸 (클래스 없음 HRESULT).
- `[공백]` 원문 로그 `1310 ~ 1313` 이 코드의 정확히 어느 줄인지 (로그가 지워져 못 맞춤). 요청서에 인용된 로그 줄 번호는 이 결과서에서 직접 열어보지 못했다.
- `[추측]` 프로시저 6 이 7 보다 먼저인 것 (`OXIDEntry+0xd8` 초기값 0 가정).
- `[추측]` `LocalInterfaceOnlySecCallback` 의 RCX 가 인터페이스 UUID 포인터라는 것.

## VM 으로 확인할 것 — 디버거 필요

원문 (1 이 (다) 면 Q283 D5 · D8 에 얹기) 에 따르면 이번 답은 (가) · (나) 이므로 **새 번호 없이 Q283 D5 · D8 에 "읽기만" 얹는 한 줄**이면 된다 (필수 아님, 계산값 대조용):

| 자리 | 볼 것 | 닫는 것 |
|---|---|---|
| D5 (`combase+0x18c1f3`, `Connect` 반환) 뒤 `combase!CoInitializeSecurity+0x385` 직전 | `dq combase!CRpcResolver::_pNoRegAccessRestrictionsSD L1`, `dd combase!CRpcResolver::_dwNoRegAccessPermissionsSDSize L1`, 각 포인터의 `db <ptr> L0xe4` (넷째 228 B), `dq combase!CRpcResolver::_pDefaultAccessRestrictionsSD L1` (NULL 인지 · 228 B 인지) | 1-1 의 계산 바이트와 다섯째의 존재 |
| D8 (`rpcss!_Connect+0x5ce` 루프 앞) | `dd poi(rpcss!gpNoRegLaunchPermissionsSD)+8 L1` 등 크기 넷 · `rpcss!GetDefaultAccessRestrictions` 반환 RAX · 다섯째 크기 | 칸 28 `{124,124,232,228,?}` 와 총 708/936 |
| (선택) `combase!LookupPrincName+0x116` 반환 · `+0x2b4` 이후 `eax` | `LookupAccountSidLocalW` 가 진짜 VM 에서 성공하는지 · 이름 (예 `DESKTOP\사용자`) | 명시 길의 `gpsaSecurity` 가 4 항목 기본 모양인지 · 이름이 들어가는지 (`gDefaultService`) |
| (선택) `combase!CComApartment::CallTheResolver+0x9d` | 첫 부름이 `RegisterOXIDAndOIDs` (6) 인지 | 4-1 의 "6 이 먼저" |

## VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것 (수집 스크립트 재료)

| 무엇 | 명령 (예) | 쓰는 곳 |
|---|---|---|
| `HKLM\SOFTWARE\Microsoft\OLE` 전체 (값 이름 · 종류 · **바이트**) | `reg export HKLM\SOFTWARE\Microsoft\OLE ole.reg` (REG_BINARY 는 16진 그대로), 또는 `reg query … /s` | 다섯째 조각 `MachineAccessRestriction` 바이트, `DefaultLaunchPermission` · `MachineLaunchRestriction` 이 NoReg 와 같은지 (위 124 · 232 · 228), `DefaultAccessPermission` 이 정말 없는지, **`MachineAccessRestrictionOld` · `MachineLaunchRestrictionOld`** 가 있는지 (`UpdateComSecurityDescriptors` 가 쓴 흔적), `EnableDCOM` · `LegacyImpersonationLevel` 등 |
| 같은 키의 하위 `AppCompat` | `reg query "HKLM\SOFTWARE\Microsoft\OLE\AppCompat" /s` | `RaiseActivationAuthenticationLevel` (람다 앞 `operator() 0x7f438`) 값 있는지 |
| DCOM 정책 키 | `reg query "HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM" /s` | 있으면 `MachineAccessRestriction` · `MachineLaunchRestriction` (SDDL **글**) — 없으면 등록값 쪽이 쓰임 |
| AppID 키 | `reg query "HKLM\SOFTWARE\Classes\AppID\test.exe"` · `reg query "HKCU\SOFTWARE\Classes\AppID\test.exe"` (실행 파일 이름) | "AppID 없음" 이 진짜 VM 에서도 같은지 |
| OS 종류 (`IsWorkstationOS`) | `systeminfo` (OS 이름), `wmic os get producttype` (1 = 워크스테이션) | 넷째 조각 AN 마스크 0x3 (워크스테이션) · 0x7 (서버) |
| 토큰 사용자 SID | `whoami /user` | `LookupPrincName` 의 이름이 `DESKTOP-…\user` 모양인지 (원문 로그 `1333`) |
| SID 약어 대조 (선택) | `reg query` 말고 `sc sdshow` 나 PowerShell `ConvertFrom-SddlString` 로 위 SDDL 글을 풀어 SID 확인 | `[규칙]` 인 약어 → SID 표 |

## 다음 요청 후보

1. **`rpcrt4` LRPC 연결 재시도** (`ncalrpc` 연결을 몇 번 · 왜 다시 시도하나) — lsapolicylookup "8 번" 닫기 + `PickAssociation` (Q284b ⑤).
2. rpcss `ISCMLocalActivator` 서버 (활성화 "클래스 없음" HRESULT 를 정하는 곳 · 활성화 프로시저 몸) — Q284b ⑥ 와 합칠 만함.
3. `GetLegacyBlanket` 나머지 (AppID 가 **있는** 길: `AccessPermission` 값 읽기 · `AuthenticationLevel`) · `CallAccessCheck (rva 0x7fcc4)` — 접근 검사 syscall 이 무엇인지 (명시 길의 `gAnonymousAccessAllowed`).
4. `MakeProxyHelper` (rva `0x94284`) — `_pdsaLocalResolver` 가 어떤 DSA 면 프록시가 서는지 (sast/144 `[공백]` 의 `DUALSTRINGARRAY 진짜 내용`).
5. Q284b ④ (프로시저 10 · 1 몸) · ⑤ `PickAssociation` · ⑥ `CoGetObject` (그대로 대기).

## 뽑은 것

- **rpcss** (`ms/disassemble/rpcss/`, `ms/decomp/rpcss/`; 다시 analyze 안 함): `^(InitializeSystemSecurityDescriptors|GetDefaultAccessRestrictions|GetDefaultLaunchPermissions|GetDefaultLaunchRestrictions|ReadSecurityDescriptor|GetRegistrySecurityDescriptor|ReadSecurityDescriptorFromSDDL|ReadSecurityDescriptorHelper|IsValidCOMSecurityDescriptor|ValidateCatalogSecurityDescriptor|SetDefaultLaunchRestrictions|SetDefaultLaunchPermissions|SetDefaultAccessRestrictions|FixRestrictionSecurityDescriptor|AddAceToSecurityDescriptor|ResetComSecurityDescriptors|UpdateComSecurityDescriptors|CreateSd|~CSecDescriptor|_GetDefaultSecurityPermissions)$` · `^(ReadPermissions|StartTokenHandling|IsWorkstationOS|GetNamedValueSD|InitializeSCMAfterListen|GetDefaultLaunchRestrictions)$` · `^(ReadRemoteActivationKeys|PolicyCallbackFn|HandleInvalidRestrictionFromOleKey|CheckForExpectedAccess|IsValidCOMSecurityDescriptor)$` · `^(_ServerAllocateOIDs|_ServerAllocateOXIDAndOIDs|LocalInterfaceOnlySecCallback|IsClientRpcss|RPCSStoOLE32SecurityCallback)$` · `^ServerAllocateOIDsInternal$` (모두 depth 0). `refs` (`_refs/`): `0x1656e0 · 0x1656d8 · 0x1656c8 · 0x1656d0 · 0x1656b0 · 0x1656a8 · 0xeb9c8 · 0x97e74 · 0x75298 · 0x617f4 · 0x1c100 · 0x1ce14 · 0x1655b8 · 0x1655a8 · 0x164408 · 0x73e04 · 0x5fd18 · 0x100a18 · 0x10092c · 0xec488 · 0x5fcf0 · 0xa3ffc`.
- **combase** (`ms/disassemble/combase/`): `^(GetDefaultAccessRestrictionsSD|CalculateAnonymousGrantedRemoteAccessInRestrictions|CoVrfCheckSecuritySettings|GetLegacyBlanket|GenerateDefaultAccessPermissionsSecurityDescriptor|GetSystemSecurityDescriptors|LookupPrincName|CopySecDesc|IsValidCOMSecurityDescriptor|EnsureRestrictedCodePresent|FixupSecurityDescriptor|GetRegistrySecurityDescriptor|CoGetSystemSecurityPermissions|DefaultAuthnServices|utGetAppIdForModule|ValidateCatalogSecurityDescriptor|AddAceToSecurityDescriptor|AddAcesToSecurityDescriptorViaApi|CWorldSecurityDescriptor|GetAccessPermission|UninitializeSecurity|CAuthInfo)$` · `rva:0x7f438` (`operator____7f438.asm`) · `^(RegisterOXIDAndOIDs|MakeSCMProxy|EnsureLinkToPrimaryOxid|MakeCloakedSCMProxy|ServerAllocateOIDs)$` (모두 depth 0). `refs`: `0x3380e8 · 0x3380f8 · 0x3380f0 · 0x3380d0 · 0x3380c8 · 0x3380e0 · 0x178e12 · 0x18db10 · 0x7e5f4 · 0x81508 · 0x7f4f4 · 0x363160 · 0x363168 · 0x80200 · 0x7deec`.
- **ntdll** (`ms/disassemble/ntdll/`): `^(RtlLengthSecurityDescriptor|RtlValidRelativeSecurityDescriptor|RtlValidSecurityDescriptor|RtlLengthSid|RtlValidSid|RtlGetOwnerSecurityDescriptor|RtlGetGroupSecurityDescriptor|RtlGetDaclSecurityDescriptor|RtlCopySecurityDescriptor|RtlGetSaclSecurityDescriptor)$` · `^(RtlAbsoluteToSelfRelativeSD|RtlCreateSecurityDescriptor|RtlSetOwnerSecurityDescriptor|RtlSetDaclSecurityDescriptor)$` · `^RtlMakeSelfRelativeSD$` (depth 0).
- **sechost** (새 analyze 후 `ms/disassemble/sechost/`, `ms/decomp/sechost/`): `^(LookupAccountSidLocalW|LookupAccountSidInternal|LsaLookupOpenLocalPolicy|LsaLookupTranslateSids|LsaLookupClose|ConvertStringSecurityDescriptorToSecurityDescriptorW|LocalConvertStringSDToSD_Rev1|ConvertStringSDToSDDomainW|LsaLookupGetDomainInfo|LsaLookupInitialize)$` · `^(PLSA_LOOKPR_SERVER_NAME_bind|PLSA_LOOKPR_SERVER_NAME_unbind|LsaInternalClientLookupSids)$` · `^(LocalGetAclForString|LocalGetSDControlForString|LocalGetSidForString|LocalConvertStringSidToSid)$` (depth 0). `refs`: `0x80c50 · 0x80518`. `map` 은 안 만듦 (뿌리 없음).
- **advapi32**: 열지 않음 (`GetSecurityDescriptorLength` = `jmp` 한 줄, `ConvertString…` 은 combase · rpcss 가 sddl api-set → sechost 로 직접 호출).
- **일회용 읽기 · 계산 (스크래치패드 `…/scratchpad/s148/`, 커밋 안 함)**: `pe148.py` (pefile 로 rva 읽기, sast/144 `pe.py` 복사) · `rdstrs.py` · `rdstrs2.py` (UTF-16 글) · `tab2.py` (`gStartupSids` 표) · `mksd.py` (SD 바이트 계산) · `strsrch.py` · `chk.py` (핵심 `+0x…` 원본 바이트 대조) · `imp.py` (KernelBase `GetSecurityDescriptorLength` → ntdll IAT).
- 로그: `ms/ghidra/rpcss.export.sast148{a..e}.stdout` · `rpcss.refs.sast148_*.stdout` · `combase.export.sast148{a..c}.stdout` · `combase.refs.sast148_*.stdout` · `ntdll.export.sast148{a..c}.stdout` · `sechost.analyze.{log,stdout}` · `sechost.export.sast148{a..c}.stdout` · `sechost.refs.sast148_*.stdout`. **도는 작업 없음.**
