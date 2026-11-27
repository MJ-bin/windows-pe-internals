# 2026-11-27 (작성일, 정적 분석 2026-11-27) run151 — rpcss `_Connect` 응답의 SD 다섯 조각과 combase 가 죽는 사슬 (Q288 + Q284a, 정적만): **넷 (`gpNoReg*SD`) 은 rpcss 가 서비스 시작 때 코드 상수 SDDL 로 짓는다 (레지스트리 안 봄)**, 크기 **124 · 124 · 232 · 228 B**. **다섯째 (`GetDefaultAccessRestrictions`) 만 레지스트리** (`HKLM\SOFTWARE\Microsoft\OLE\MachineAccessRestriction`, 정책 키가 있으면 그쪽) 이고 **없으면 빠진다 (크기 0 가능)**. 죽는 줄은 **`CoInitializeSecurity +0x385 GetDefaultAccessRestrictionsSD` → `GetSecurityDescriptorLength` → ntdll `RtlLengthSecurityDescriptor +0x2d`** — 다섯째가 없으면 넷째 (`_pNoRegAccessRestrictionsSD`) 를 읽는데, 우리 "SD 크기 0 ×5" 대답이면 그게 0 B 덩어리라 길이 함수가 덩어리 밖을 읽다 접근 위반. 검사는 없다. **lsapolicylookup 은 명시 호출 길에선 끼지만 실패를 삼키고 지나간다** (죽음과 무관). 암묵 길 (`EOAC_APPID`) 에선 치명. **run149 정정 둘**: "(라) SD 0 ×5 가능" 틀림 · "프로시저 7 → 6" 은 첫 부름이 **6 먼저**

> **정정 · 대조 (run152 때 대조 — explainer 가 찾은 어긋남 둘, `.asm` 으로 가림)**: (a) combase `InitializeSystemSecurityDescriptors` 오프셋 세 벌은 **다 맞고 뜻이 다르다** `[정적]` — `+0x2a` · `+0x62` · `+0x9a` · `+0xd0` · `+0x107` = **`LocalAlloc` call** (ret `+0x31` · `+0x69` · `+0xa1` · `+0xd7` · `+0x10e`), `+0x36` · `+0x6e` · `+0xa6` · `+0xdc` · `+0x113` = **전역에 넣는 줄** (`_pNoRegLaunchPermissionsSD` `0x3380e8` · `_pNoRegAccessPermissionsSD` `0x3380f8` · `_pNoRegLaunchRestrictionsSD` `0x3380d0` · `_pNoRegAccessRestrictionsSD` `0x3380c8` · `_pDefaultAccessRestrictionsSD` `0x3380e0`), `+0x4f` · `+0x8c` · `+0xc2` · `+0xf4` · `+0x12b` = **`memcpy` call** (결과서 148 1-1 끝 "LocalAlloc 후 memcpy" 는 이 줄). 2절 표 0 줄에 셋을 나눠 적었다. (b) `+0xd8` 은 **두 구조체의 다른 칸** `[정적]` — `CallTheResolver` 의 `RDI` = `CComApartment` (`this`, `+0x78 EnsureLinkToPrimaryOxid@CComApartment` 에 그대로 넘김). ① **`[아파트+0xd8]` DWORD = 더할 OID 수 (`cOidsToAdd`)** — `+0x33 MOV R15D,[RCX+0xd8]` · 배열 `[아파트+0xe0]` 을 `+0x4f memcpy` 로 지역에 · `+0x54` 0 으로 · `+0xfc` 프로시저 7 인자. ② **`[OXIDEntry+0xd8]` BYTE = 등록 표시** — `+0x87 RCX = [아파트+0x10]` (`OXIDEntry`) · `+0x94 MOV AL,[RCX+0xd8]` · `+0x9d JZ`, 같은 `RCX` 로 `+0x1b7 OXIDEntry::RegisterOXIDAndOIDs` 가 `+0xb1 XCHG [OXIDEntry+0xd8],1` (그 앞 `+0x93` OXID → `+0xc8` · `+0xa7` MID → `+0xd0`). 그래서 "`RegisterOXIDAndOIDs` 의 `[아파트+0xd8] = 1`" 꼴로 읽으면 틀리고, `+0x94` 와 `+0xb1` 은 **같은 칸** (`OXIDEntry+0xd8`), `cOidsToAdd` 만 **다른 칸** (`CComApartment+0xd8`). 4-1 표 · 요약 · 확인된 것 · 나무의 `[..+0xd8]` 을 이 이름으로 고쳤다. 결론 (6 먼저) 은 그대로.

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **태그 읽는 법 (이 문서)**: `[정적]` = `.asm` 줄 · `pefile` 로 읽은 `.rdata` 글. `[규칙]` = 공개 규칙 (SDDL 약어 → SID, `WELL_KNOWN_SID_TYPE` 번호, self-relative SD 모양). **`[추측: 계산]` = `[정적]` 글 + `[규칙]` 으로 계산한 값** (SD 16진 · 크기 · 덩어리 합) — 진짜 메모리에서 본 값이 아니다. 결과서도 "VM 대조 필요" 라고 한다. flow-writer 가 같은 규칙으로 따로 다시 계산해 같게 나왔지만, 같은 규칙이라 VM 대조를 대신하지 못한다.

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** (원 요청 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 의 **② 절** "rpcss `_Connect` 응답의 SD 다섯 조각", 튜닝 `374_tune19.md` OR1 — 받은 581 · 탓 18, 요청 쪽 에뮬레이터 숫자) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 combase · rpcrt4 · sechost 를 안 올린다 — 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 · run133 · run137 · run145 · run149 · run150 과 같은 줄), `sample/*.c` 에 COM API 가 없다 `[소스]` (`sample/log.h:3`). RPCSS 는 **다른 프로세스** (`svchost -k rpcss` 의 `rpcss.dll`, run149). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (sechost 줄 새로 · run151 근거 줄) · 3장 run149 절 (제목 고쳐 읽기 · 나무 한 줄 · run151 글머리 넷 + 작은 글자 나무 하나) · 4장 (run149 줄 셋 표시 · run151 줄 셋) · 5장 (run151 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q288** (380 ②, 새) + **Q284a** (Q284 의 ① ~ ③ — 프로시저 7 · 6 몸 · `LocalInterfaceOnlySecCallback` · `MakeSCMProxy` / `DelegateActivationToSCM`). 사용자 결정 2026-11-27 (PE_emulator D179) — 380 묶음 정적 차례 ②, 정적만. 결과서 판정 **분석성공** — 물음 2 · 1 · 3 · 5 는 줄 근거로 닫힘, 4 · 6 은 몸 · 입출력 칸 · 실패 status 까지 (서버 쪽 진짜 값은 `[공백]`).
> - 이어 쓰는 앞 run: **run149** (2-2 응답 칸 28 ~ 30 · "최소 대답" 3 · "그다음 벽" — 이번 정정 대상 둘), **run136** (`CoInitializeSecurity +0x47d GetConnection`), **run135** (LSA 이름 묻기의 lsass 쪽 — 이번엔 클라이언트 sechost 쪽).
> - 앞 run 정정 둘: run149 문서 · 해설서 맨 위 "고쳐 읽기" 두 줄씩 (아래 "정정").
> - 남은 것 → 동적은 새 번호 없이 **Q283 D5 · D8 에 "읽기만" 얹기** (결과서 "VM — 디버거 필요"), 디버거 없이 떠 올 레지스트리 재료 (결과서 "VM — 디버거 없이"), 정적은 결과서 "다음 요청 후보" 1 ~ 5 (Q284b 와 겹침) — 대기열 자리는 오케스트레이터가 정함.

## 요약

- **SD 다섯 조각의 출처** `[정적]`:
  - 넷 = **(가) 코드 상수 SDDL**. rpcss `InitializeSystemSecurityDescriptors` (rva `0x73e04`) 가 서비스 시작 때 (`InitializeSCMBeforeListen +0x1a5`) `ConvertStringSecurityDescriptorToSecurityDescriptorW` 로 한 번 짓는다. 레지스트리를 안 본다 (이름 그대로 "NoReg").
  - 둘 (실행 제한 · 접근 제한) 에만 **고정 BUILTIN SID 둘** (DCOM Users `S-1-5-32-562` · Performance Log Users `S-1-5-32-559`) 이 `%s` 로 끼워진다 — 기계마다 같다.
  - 접근 제한은 **워크스테이션이면 Anonymous 마스크 `0x3`, 서버면 `0x7`** (`+0x277 IsWorkstationOS`). Win 11 Education 은 워크스테이션.
  - 다섯째 = **(나) 레지스트리 값 그대로** — 정책 `HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM\MachineAccessRestriction` (SDDL 글) 이 있으면 그것, 아니면 `HKLM\SOFTWARE\Microsoft\OLE\MachineAccessRestriction` (REG_BINARY). 없거나 검사에 걸리면 **NULL → 응답에서 빠짐 (크기 0)**.
  - (다) 기계 · 부팅마다 다른 SID 는 **없다**.
- **크기** `[추측: 계산]`: 124 · 124 · 232 · 228 B. 원문 `[수집]` 레지스트리 값 크기 (124 · 232 · 228) 와 하나씩 정확히 같다. 그런데 Connect 응답에 실리는 레지스트리 값은 `MachineAccessRestriction` (228) 하나뿐이다 — 나머지 둘은 rpcss 안 다른 전역으로 간다.
- **죽는 사슬** `[정적]`: `CoInitializeSecurity +0x385 GetDefaultAccessRestrictionsSD` → `[0x3380e0]` (다섯째) 가 NULL 이면 `[0x3380c8]` (넷째) → `+0x2c GetSecurityDescriptorLength` (KernelBase `jmp [IAT]` 한 줄) → ntdll **`RtlLengthSecurityDescriptor +0x2d`** (`MOVZX ECX,byte ptr [RCX+1]` = SID 머리). 길이 재기 전 **검사가 없다** (`IsValid…` 안 부름).
- **우리 "SD 0 ×5" 대답이 죽는 까닭**: combase `InitializeSystemSecurityDescriptors` 가 크기 0 이어도 `LocalAlloc(0x40, 0)` 으로 **비-NULL 0 B 덩어리**를 만들어 넷째 전역에 넣는다 (다섯째만 크기 0 이면 건너뜀). 그 0 B 덩어리를 길이 함수가 SD 머리로 읽는다 → 덩어리 밖 쓰레기 → 접근 위반. (`LocalAlloc` 크기 0 이 NULL 을 안 준다는 것은 `[추측: 일반 동작]`, 원문도 "0 B 덩어리를 가리키는 비-NULL" 로 봄.)
- **넷 중 실제로 읽는 것** `[정적]`: 명시 `CoInitializeSecurity(SD NULL, −1, …)` 길은 **넷째 (또는 다섯째) 만**. 첫째 · 셋째는 공개 API `CoGetSystemSecurityPermissions` 만 읽고, 둘째는 암묵 (`EOAC_APPID`) 길만 읽는다.
- **lsapolicylookup** `[정적]`: 명시 길에선 `CoInitializeSecurity +0x2af LookupPrincName(0xa)` → sechost `LookupAccountSidLocalW` → `LsaLookupOpenLocalPolicy` 가 `ncalrpc:[lsapolicylookup]` 에 곧장 붙는다 (SID → 이름). 실패해도 `CoInitializeSecurity` 가 **삼키고** 기본 보안 배열로 계속 간다 → **죽음과 무관**. 암묵 길에선 `GenerateDefaultAccessPermissionsSecurityDescriptor` 가 같은 포트를 직접 열고, 실패면 `CoInitializeSecurity` 가 그 에러로 **끝난다**.
- **Q284a** `[정적]`:
  - 프로시저 **6 이 먼저**: `CallTheResolver +0x9d JZ` — 아파트의 `OXIDEntry` 가 아직 안 등록 (`[OXIDEntry+0xd8]` BYTE `== 0` — run152 때 대조) 이면 `RegisterOXIDAndOIDs` (6), 등록된 뒤 OID 를 더 받을 때 7 (첫 부름 때 그 칸이 0 인 것은 `[추측]`).
  - `LocalInterfaceOnlySecCallback`: 0 = 통과 · 5 = 거절. 보는 것은 전송 (ncalrpc) · 클라이언트 토큰 · **같은 접근 제한 SD (다섯째 / 넷째)** 의 비트 `0x2`. 로컬 test.exe 꼴이면 통과.
  - "클래스 없음" HRESULT 는 combase 에 상수가 없다 — rpcss 활성화 서버가 정해 보낸 값을 그대로 올린다 `[공백: 서버 몸]`.
- **run149 정정 둘**: (a) 칸 28 "가능 (라) · 0 ×5" 와 "최소 대답" 3 의 "SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL" 은 틀림. (b) "프로시저 7 → 6" 은 첫 부름 차례가 반대 (6 먼저).

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백아홉째 바퀴. plan **Q288 + Q284a**. **짐 지도 바퀴 아님.**
- 정적: rpcss · combase · ntdll 은 있던 프로젝트에서 export 만 더, **sechost 는 처음 analyze** (결과서: 14:21:50 → 14:24:14, `MAXMEM=8G`, 시작 전 `free -g` available 28). advapi32 는 안 열었다 (쓰는 함수가 advapi32 를 안 지남). rpcrt4 · kernelbase 프로젝트도 안 열었다 (kernelbase 는 `pefile` 로 export 한 줄만).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| rpcss | `316298244AA2A8A6A520F13E7355EC211` | 같음 (run149). flow-writer 가 `pefile` 디버그 디렉터리로 앞 16자 `316298244AA2A8A6` · Age 1 다시 읽음 |
| combase | `DA1DC0C4F35CEE58F672CBB20460D11D1` | 같음 (run131 · run136 · run149). 디버그 디렉터리 앞 16자 같음 |
| ntdll | `C3093720177DA6851519DCFFE8DF53FE1` | 같음. 디버그 디렉터리 앞 16자 같음 |
| **sechost (새로)** | `0E8BAC436697447988FB72C706D1F1631` | **새 줄**. `System32\sechost.dll` (704,008 B). 분석 증거: `ms/ghidra/sechost.gpr` · `ms/decomp/sechost/_index.tsv` (1,568줄 = 머리 1 + 함수 1,567, `FUN_` 2) · `ms/ghidra/sechost.analyze.log` 의 `SetPdb: sechost.dll <- …/ms/sym/sechost.dll.pdb` 줄. 디버그 디렉터리 앞 16자 `0E8BAC4366974479` · Age 1 (flow-writer 도 `pefile` 로 같음) |
| advapi32 | `43EC8DCD3F9CB692FE88343843C328201` | 안 엶 (앞 16자만 확인 — 결과서) |

- 버전 대조 (결과서 "알려진 사실" 표에 rpcss · combase · sechost 줄이 없어 앞 run 오프셋과 원본 바이트로): sast/144 오프셋 (`CoInitializeSecurity +0x47d GetConnection` · `rpcss!_Connect +0x40f ~ +0x483` · combase `InitializeSystemSecurityDescriptors +0x1e ~ +0x12b`) 이 이번 `.asm` 에 그대로 있다. 원문이 말한 죽는 rip `ntdll+0xe8f3d` (= `0xe8f10 + 0x2d`) 의 바이트는 `0f b6 49 01` (`MOVZX ECX,byte ptr [RCX+0x1]`) — flow-writer 도 `pefile` 로 같은 4 B 를 읽음. **일치.**

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): 갈래 BG05 가 run149 "최소 대답" 3 (SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL) 으로 `Connect` 를 세웠더니 게스트가 `rBG05_rv1 36a5d5d8e948 seq 1354` 에서 읽기 `0x1700744cd540` · rip `ntdll +0xe8f3d` 로 죽음 (`0xc0000005`). 그 사이 `1339 ~ 1353` 에 `\RPC Control\lsapolicylookup` 연결 8 번 `0xc0000034`.
  1. **rpcss `gpNoReg*SD` 넷 + `GetDefaultAccessRestrictions`** 를 누가 무엇으로 짓나 — (가) 상수 · (나) 레지스트리 · (다) 기계별. 원문 `[수집]` 셋 (`DefaultLaunchPermission` 124 · `MachineAccessRestriction` 228 · `MachineLaunchRestriction` 232) 이 어느 조각인가. 크기 0 이 진짜에서 나오나.
  2. **combase 가 읽는 길** — `CoInitializeSecurity(SD NULL, −1, …)` · AppID 없음 에서 `RtlLengthSecurityDescriptor` 까지 사슬 · 검사 · 실패 HRESULT · 실제로 읽는 조각. 암묵 길도 같은가. `GetSystemSecurityDescriptors +0xf` 본문.
  3. **`lsapolicylookup` 이 끼는가** — 여는 함수 · 실패 때 끝나는 꼴.
  4. ~ 6. (Q284a) 프로시저 7 · 6 몸과 combase 검사 · `LocalInterfaceOnlySecCallback` 거절 조건 · `MakeSCMProxy` · `DelegateActivationToSCM` "클래스 없음" 길.
- 쓰는 곳 (원문): `ext/syscall/syscall_layer_cpp/src/rpc/rpcss.cpp` 응답 칸 28 ~ 30 · `tests/alpc_frontend.cpp`. 3 이 "낌" 이면 LSA1 이 OR1 앞 바퀴.

## 방법

- 정적 sast/148 (요청서 `_workspace/sast/148_rpcss-connect-sd.md`, 결과서 `_workspace/sast/148_rpcss-connect-sd_result.md`, **분석성공**, 스크립트 문제 없음):
  - rpcss (depth 0): `InitializeSystemSecurityDescriptors` · `GetDefaultAccessRestrictions` · `ReadPermissions` · `ReadSecurityDescriptor` · `IsWorkstationOS` · `StartTokenHandling` · `_ServerAllocateOIDs` · `_ServerAllocateOXIDAndOIDs` · `ServerAllocateOIDsInternal` · `LocalInterfaceOnlySecCallback` 외. refs 22 곳 (전역 `0x1656e0 ~ 0x1656b0` 등).
  - combase (depth 0): `GetDefaultAccessRestrictionsSD` · `CalculateAnonymousGrantedRemoteAccessInRestrictions` · `GetLegacyBlanket` · `GenerateDefaultAccessPermissionsSecurityDescriptor` · `GetSystemSecurityDescriptors` · `LookupPrincName` · `RegisterOXIDAndOIDs` · `MakeSCMProxy` · `rva:0x7f438` 외. refs 15 곳 (여섯 전역 `0x3380c8 ~ 0x3380f8` 등).
  - ntdll (depth 0): `RtlLengthSecurityDescriptor` · `RtlMakeSelfRelativeSD` · `RtlAbsoluteToSelfRelativeSD` 등 14개.
  - sechost (새 analyze, depth 0): `LookupAccountSidLocalW` · `LookupAccountSidInternal` · `LsaLookupOpenLocalPolicy` · `PLSA_LOOKPR_SERVER_NAME_bind` · `ConvertStringSecurityDescriptorToSecurityDescriptorW` · `LocalGetAclForString` 등 17개. refs `0x80c50` · `0x80518`.
  - `pefile`: rpcss `.rdata` SDDL 글 · `gStartupSids` 표, KernelBase `GetSecurityDescriptorLength` → IAT, SD 바이트 계산 (스크래치패드, 커밋 안 함).
- flow-writer 가 다시 본 것:
  - `.asm`: combase `GetDefaultAccessRestrictionsSD` 전부 · `CoInitializeSecurity` (`+0xb2 ~ +0x116` · `+0x15c ~ +0x176` · `+0x1a3 ~ +0x208` · `+0x29c ~ +0x3a4` · `+0x47d ~ +0x494` · `+0x5e7 ~ +0x5f3` · `+0x64a ~ +0x651` · `+0xa53 ~ +0xac2` · `+0xb20 ~ +0xb41`) · `InitializeSystemSecurityDescriptors` `+0x0 ~ +0x137` · `CallTheResolver` `+0x7d ~ +0xa3` · `+0x106 ~ +0x1cd` · `LookupPrincName` · `GetSystemSecurityDescriptors` · `GenerateDefault…` · `GetLegacyBlanket` · `ServerRegisterOXID` · `RegisterOXIDAndOIDs` · `MakeSCMProxy` · `BindToSCMProxy` · `DelegateActivationToSCM` · `InitRemoting +0xb6`; rpcss `InitializeSystemSecurityDescriptors` 의 CALL 줄 전부 · `+0x2d ~ +0xd4` · `+0x253 ~ +0x2c8` · `_Connect +0x40f ~ +0x5ee` · `ReadPermissions` LEA · CALL 줄 · `IsWorkstationOS +0x5f ~ +0x66` · `LocalInterfaceOnlySecCallback` CALL 줄 전부 · `_ServerAllocateOXIDAndOIDs` · `ServerAllocateOIDsInternal`; ntdll `RtlLengthSecurityDescriptor +0x0 ~ +0x4a` · `RtlMakeSelfRelativeSD +0xe1 ~ +0x17e`; sechost `LookupAccountSidLocalW` · `LookupAccountSidInternal` · `LsaLookupOpenLocalPolicy` · `PLSA_LOOKPR_SERVER_NAME_bind` CALL 줄.
  - `pefile` (스크래치, 레포 밖): rpcss `.rdata` 글 7개 (`0x1472b0` · `0x147240` · `0x147320` · `0x147050` · `0x146ed0` · `0x131af0` · `0x1465f0`), sechost 글 둘 (`0x80518` · `0x80c50`), `gStartupSids` 9칸, KernelBase export `GetSecurityDescriptorLength` 7 B 와 IAT 대상, ntdll `+0xe8f3d` 4 B, 네 모듈 디버그 디렉터리. **SD 16진을 같은 규칙으로 따로 계산** (아래 "flow-writer 대조").
  - `pdb_aliases.py --lookup combase.dll` `0x7e5f4` · `0x7f4f4` · `0x7e67c` · `0x80200` · `0x37608` — 다섯 다 "별칭 무리 아님" (이름 하나). 이 문서는 PDB 이름을 그대로 쓴다.
  - 원문 380 ② 절 · "받을 꼴" (읽기만). `rpcss.cpp` · `alpc_frontend.cpp` 는 안 읽음 (결과서도 안 읽음).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **SD = 출입 명단 카드.** rpcss (건물 관리 사무소) 는 `Connect` 답장 끝에 명단 카드 다섯 장을 붙여 보낸다.
- 넷은 **사무소에 인쇄돼 있는 기본 카드** (코드 상수). 다섯째는 **건물주가 따로 써 둔 카드** (레지스트리) 라 없을 수도 있다.
- combase 는 다섯째가 없으면 넷째 카드를 꺼내 **"몇 쪽짜리지?" 하고 길이부터 센다**. 우리가 빈 봉투 (0 B) 를 보냈더니 봉투 밖 종이를 읽다 넘어졌다 `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{rpcss,combase,ntdll,sechost}/`)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `rpcss!InitializeSCMBeforeListen` | `+0x1a5` · `+0x1b5` | `+0x1aa` · `+0x1ba` | `InitializeSystemSecurityDescriptors` (rva `0x73e04`) · `ReadRemoteActivationKeys` (→ `+0x17c ReadPermissions`, 결과서) | [정적] |
| `rpcss!InitializeSystemSecurityDescriptors` | `+0x4e` · `+0xcd` | `+0x55` · `+0xd4` | `ConvertSidToStringSidW` (`gSidDCOMUsers` `0x1655b8` → `[RBP+0x10]` · `gSidPerfLoggingUsers` `0x1655a8` → `[RBP+0x18]`) | [정적] |
| 〃 | `+0x277` | `+0x27c` | `IsWorkstationOS` (≠ 0 → 형식 `0x147050`, 0 → `0x146ed0`, `+0x280 ~ +0x29a`) | [정적] |
| 〃 | `+0x2a1` · `+0x2c8` | `+0x2a6` · `+0x2cd` | `StringCbPrintfW` (접근 제한 · 실행 제한 글에 SID 글 둘) | [정적] |
| 〃 | `+0x2f5` · `+0x39f` · `+0x445` · `+0x4eb` | `+0x2fc` · `+0x3a6` · `+0x44c` · `+0x4f2` | `ConvertStringSecurityDescriptorToSecurityDescriptorW` (넷; 첫째 글 `+0x2ee LEA 0x1472b0`, 둘째 `+0x398 LEA 0x147240`) | [정적] |
| 〃 | `+0x373` · `+0x41d` · `+0x4c3` · `+0x561` | `+0x37a` · `+0x424` · `+0x4ca` · `+0x568` | `GetSecurityDescriptorLength` (크기 → `CSecDescriptor`) — 전역 쓰기 `+0x391` `0x1656e0` · `+0x43b` `0x1656d8` · `+0x4e1` `0x1656c8` · `+0x577` `0x1656d0` | [정적] |
| `rpcss!_Connect` | `+0x46c` | `+0x471` | `GetDefaultAccessRestrictions` (rva `0x617f4`; NULL 이면 `+0x47b JZ` 로 칸 `[4]` 안 씀) | [정적] |
| 〃 | `+0x5da` | `+0x5df` | `memcpy` (`+0x5ce ~ +0x5ee` 조각 이어 붙이기 루프) | [정적] |
| `rpcss!ReadPermissions` | `+0x171` · `+0x17e` | `+0x176` · `+0x183` | `ReadSecurityDescriptor` · `SetDefaultLaunchPermissions` | [정적] |
| 〃 | `+0x280` · `+0x29f` | `+0x285` · `+0x2a4` | `CheckForExpectedAccess` · `SetDefaultAccessRestrictions` (값 이름 `+0x25a LEA 0x1465f0` = "MachineAccessRestriction") | [정적] |
| `rpcss!StartTokenHandling` | `+0x216` | `+0x21d` | `CreateWellKnownSid` (`gStartupSids` 표) | [정적] |
| `combase!CoInitializeSecurity` | `+0xe2` | `+0xe7` | `operator()` (rva `0x7f438`, `OLE\AppCompat` 읽기) | [정적] |
| 〃 | `+0x2af` | `+0x2b4` | `LookupPrincName(0xa, …)` (rva `0x7f4f4`) | [정적] |
| 〃 | **`+0x385`** | **`+0x38a`** | **`GetDefaultAccessRestrictionsSD`** (rva `0x7e5f4`) → `gSecDescRestrictions` (`+0x38a`) | [정적] |
| 〃 | `+0x39a` · `+0x39f` | `+0x39f` · `+0x3a4` | `CalculateAnonymousGrantedRemoteAccessInRestrictions` (반환값 안 봄) · `CoVrfCheckSecuritySettings` | [정적] |
| 〃 | `+0x47d` | `+0x482` | `GetConnection` (→ 람다 → `Connect`, run149) | [정적] |
| 〃 | `+0x5e7` | `+0x5ec` | `GetLegacyBlanket` (rva `0x80200`, 능력 `& 8` 일 때만) | [정적] |
| 〃 | `+0x64a` | `+0x64f` | `utGetAppIdForModule` | [정적] |
| 〃 | `+0xa97` | `+0xa9e` | `HeapAlloc(0xc)` — `LookupPrincName` 실패 갈래의 기본 `gpsaSecurity` | [정적] |
| `combase!GetDefaultAccessRestrictionsSD` | **`+0x2c`** | **`+0x33`** | **`GetSecurityDescriptorLength`** (IAT `0x1802b5198`) | [정적] |
| 〃 | `+0x46` · `+0x63` | `+0x4d` · `+0x68` | `HeapAlloc` · `memcpy` | [정적] |
| `KernelBase!GetSecurityDescriptorLength` (rva `0xfd2b0`) | `+0x0` `jmp [IAT 0x29af70]` | — | **`ntdll!RtlLengthSecurityDescriptor`** (rva `0xe8f10`) — 죽는 줄 `+0x2d` | [정적] (`pefile`) |
| `combase!LookupPrincName` | `+0x5e` · `+0x116` | `+0x63` · `+0x11d` | `ProcessToken::GetSelfSid` · `LookupAccountSidLocalW` (sechost, 실패면 `+0x124 JZ` 조각에서 한 번 더) | [정적] |
| `sechost!LookupAccountSidLocalW` | `+0x20` | `+0x25` | `LookupAccountSidInternal` | [정적] |
| `sechost!LookupAccountSidInternal` | `+0xdd` | `+0xe2` | `LsaLookupOpenLocalPolicy` (실패 `+0xe4 JS → +0x26c`) | [정적] |
| `sechost!LsaLookupOpenLocalPolicy` | `+0x42` | `+0x49` | `NdrClientCall3` ⇒ (ALPC, 서버는 lsass `[추측]`) | [정적] |
| `sechost!PLSA_LOOKPR_SERVER_NAME_bind` | `+0x3f` · `+0x5b` | `+0x46` · `+0x62` | `RpcStringBindingComposeW` ("ncalrpc" `0x80518` · "lsapolicylookup" `0x80c50`) · `RpcBindingFromStringBindingW` | [정적] |
| `combase!GenerateDefaultAccessPermissionsSecurityDescriptor` | `+0x1d9799` · `+0x1d97dc` · `+0x1d984b` · `+0x122` | `+0x1d97a0` · `+0x1d97e3` · `+0x1d9852` · `+0x127` | `LsaLookupOpenLocalPolicy` · `LsaLookupGetDomainInfo` · `CreateWellKnownSid` · `AddAcesToSecurityDescriptorViaApi` (조각 `+0x1d…` 는 함수 바깥 조각) | [정적] |
| `combase!GetLegacyBlanket` | `+0x299` · `+0x4b7` | `+0x29e` · `+0x4bc` | `GetRegistrySecDesc` (`DefaultAccessPermission`) · `GenerateDefaultAccessPermissionsSecurityDescriptor` | [정적] |
| `combase!CComApartment::InitRemoting` | `+0xb6` | `+0xbb` | `InitializeSecurity` (→ `CoInitializeSecurity` 능력 8) | [정적] |
| `combase!CComApartment::CallTheResolver` | `+0x106` · `+0x117` · `+0x122` · `+0x172` | `+0x10d` · `+0x11c` · `+0x127` · `+0x177` | `NdrClientCall2` (프로시저 7) · `RetryRPC` · `CheckStatus` · `memcpy` (OID 넣기) | [정적] |
| 〃 | `+0x1b7` (`+0x9d JZ → +0x1ad` 갈래) | `+0x1bc` | `OXIDEntry::RegisterOXIDAndOIDs` | [정적] |
| `combase!OXIDEntry::RegisterOXIDAndOIDs` | `+0x78` | `+0x7d` | `CRpcResolver::ServerRegisterOXID` | [정적] |
| `combase!CRpcResolver::ServerRegisterOXID` | `+0x30` · `+0xce` · `+0xf2` · `+0x101` · `+0x115` | `+0x35` · `+0xd5` · `+0xf7` · `+0x106` · `+0x11a` | `StartListen` · `NdrClientCall2` (프로시저 6) · `RetryRPC` · `CheckStatus` · `DsaValid` | [정적] |
| `combase!GetSystemSecurityDescriptors` | `+0xf` | `+0x14` | `GetConnection` | [정적] |
| `combase!CRpcResolver::BindToSCMProxy` | `+0xc` · `+0x17` · `+0x49` | `+0x11` · `+0x1c` · `+0x4e` | `InitChannelIfNecessary` · `GetConnection` · `MakeSCMProxy` | [정적] |
| `combase!MakeSCMProxy` | `+0x12` · `+0x5f` | `+0x17` · `+0x64` | `MakeProxyHelper` (rva `0x94284`, 안 읽음) · `CoSetProxyBlanket` | [정적] |
| `combase!DelegateActivationToSCM` | `+0x156` · `+0x75b` · `+0x789` | `+0x15b` · `+0x760` · `+0x78e` | `BindToSCMProxy` · `GetSCM` · 간접 호출 (`indirect:cfg:?`, ISCM 메서드) | [정적] |
| `rpcss!LocalInterfaceOnlySecCallback` (rva `0x1fbf0`) | `+0x56` · `+0xb0` · `+0xff` · `+0x12c` · `+0x1d4` · `+0x242` | `+0x5d` · `+0xb7` · `+0x106` · `+0x131` · `+0x1db` · `+0x249` | `I_RpcBindingInqTransportType` · `RpcImpersonateClient` · `OpenThreadToken` · `LookupOrCreateTokenFromHandle` · `GetSecurityDescriptorDacl` · `AccessCheckByType` | [정적] |
| `rpcss!_ServerAllocateOXIDAndOIDs` (rva `0x13990`) | `+0x20a` | `+0x20f` | `CServerOxid::Create` | [정적] |
| `rpcss!ServerAllocateOIDsInternal` (rva `0x26700`) | `+0x198` | `+0x19f` | `ProcessPrng` (새 OID 8 B 난수) | [정적] |

### 1. SD 다섯 조각 — 어디서 무엇으로 서나 [정적]

`rpcss!_Connect +0x40f ~ +0x483` 이 크기 다섯을 `[RDI+0] · [+4] · [+8] · [+0xc] · [+0x10]` 에 쓰고 (`+0x41d` · `+0x436` · `+0x44b` · `+0x455` · `+0x483`), `+0x5ce ~ +0x5ee` 루프가 같은 차례로 이어 붙인다 (`memcpy` ret `+0x5df`, 크기 0 이면 건너뜀 — 결과서).

| 칸 | 조각 (rpcss 전역) | 출처 | 글 (rpcss `.rdata`, `pefile`) · 짓는 줄 | 크기 | combase 전역 |
|---|---|---|---|---|---|
| `[0]` | 실행 권한 `gpNoRegLaunchPermissionsSD` (`0x1656e0`) | **(가)** | `0x1472b0` `O:BAG:BAD:(A;;0x1f;;;BA)(A;;0xb;;;IU)(A;;0xb;;;SY)` · 쓰기 `+0x391` | 124 B `[추측: 계산]` | `_pNoRegLaunchPermissionsSD` `0x3380e8` |
| `[1]` | 접근 권한 `gpNoRegAccessPermissionsSD` (`0x1656d8`) | **(가)** | `0x147240` `O:BAG:BAD:(A;;0x7;;;PS)(A;;0x3;;;SY)(A;;0x7;;;BA)` · `+0x43b` | 124 B `[추측: 계산]` | `_pNoRegAccessPermissionsSD` `0x3380f8` (+ 크기 `0x3380f0`) |
| `[2]` | 실행 제한 `gpNoRegLaunchRestrictionsSD` (`0x1656c8`) | **(가)** + 고정 SID 둘 | `0x147320` `O:BAG:BAD:(A;;0x1f;;;BA)(A;;0xb;;;WD)(A;;0x1f;;;%s)(A;;0x1f;;;%s)(A;;0xb;;;AC)(A;;0xb;;;S-1-15-3-1024-2405443489-…-991631256)` · `+0x4e1` | 232 B `[추측: 계산]` | `_pNoRegLaunchRestrictionsSD` `0x3380d0` |
| `[3]` | 접근 제한 `gpNoRegAccessRestrictionsSD` (`0x1656d0`) | **(가)** + 고정 SID 둘 + OS 종류 | `0x147050` (워크스테이션) `O:BAG:BAD:(A;;0x7;;;WD)(A;;0x3;;;AN)(A;;0x7;;;%s)(A;;0x7;;;%s)(A;;0x3;;;AC)(A;;0x3;;;S-1-15-3-1024-…)` / `0x146ed0` (서버) — AN 만 `0x7` · `+0x577` | 228 B `[추측: 계산]` | **`_pNoRegAccessRestrictionsSD` `0x3380c8`** |
| `[4]` | 기본 접근 제한 `GetDefaultAccessRestrictions()` (rva `0x617f4`, 전역 `gpDefaultAccessRestrictionsSD` `0x1656b0`) | **(나)** | ① 정책 `HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM` (`ReadPermissions +0x27 LEA 0x131af0`) 값 `MachineAccessRestriction` (SDDL 글) ② 없으면 `HKLM\SOFTWARE\Microsoft\OLE` 값 `MachineAccessRestriction` (REG_BINARY, `+0x25a LEA 0x1465f0`) — `ReadSecurityDescriptor` 의 모양 검사 · `CheckForExpectedAccess` (`+0x280`) 를 거쳐 `SetDefaultAccessRestrictions` (`+0x29f`) | VM 값 그대로 (원문 `[수집]` 228 B). 없으면 **NULL → 0** | `_pDefaultAccessRestrictionsSD` `0x3380e0` (크기 0 이면 combase `+0xfc` 가 건너뜀 → NULL) |

- **`%s` 둘** `[정적]`: `+0x2d` `gSidDCOMUsers` → `ConvertSidToStringSidW` → `[RBP+0x10]` (첫 `%s`, `R9`), `+0xc2` `gSidPerfLoggingUsers` → `[RBP+0x18]` (둘째 `%s`, `[RSP+0x20]`). 두 SID 는 `StartTokenHandling +0x216 CreateWellKnownSid` 가 `gStartupSids` (`0x1643f0`, 16 B × 9칸) 로 짓는다 — flow-writer 가 `pefile` 로 다시 읽은 칸: 3 = 종류 **61**, 도메인 0 → `0x1655b8` · 5 = 종류 **58**, 도메인 0 → `0x1655a8` · 8 = 종류 110, 도메인 **1** → `0x165590` (네 SDDL 에 안 쓰임). 61 = `WinBuiltinDCOMUsersSid` `S-1-5-32-562` · 58 = `WinBuiltinPerfLoggingUsersSid` `S-1-5-32-559` `[규칙]` → **기계마다 같다**.
- **OS 종류** `[정적]`: `IsWorkstationOS +0x5f CMP [..],2` (NT 플랫폼) · `+0x66 CMP byte [..+0x14a],1` (제품 종류 1 = 워크스테이션). Win 11 Education → 워크스테이션 → AN `0x3` `[추측: OS 이름에서 — VM 의 제품 종류 값은 안 봄]`.
- **`[수집]` 셋은 어느 조각?** `[정적]` (결과서 1절):
  - `DefaultLaunchPermission` (124 B) → `ReadPermissions +0x171 ~ +0x17e` → `SetDefaultLaunchPermissions` (`gpDefaultLaunchPermissionsSD` `0x1656a8`) — **Connect 응답에 안 실림** (활성화 검사가 읽음).
  - `MachineLaunchRestriction` (232 B) → `gpDefaultLaunchRestrictionsSD` (`0x1656b8`) — **안 실림**.
  - **`MachineAccessRestriction` (228 B) → `[4]`** — 실림.
  - `DefaultAccessPermission` 은 rpcss 가 안 읽는다 (combase `GetLegacyBlanket +0x299` 가 읽음, 없으면 `[1]` 로).
  - 크기가 NoReg 와 하나씩 같으므로 (124 = `[0]` · 232 = `[2]` · 228 = `[3]`) "레지스트리 값 = 같은 SDDL 로 지은 SD" 일 가능성이 크다 `[추측: 크기만 같음, 바이트는 VM]`.
- **다섯째만 시작 뒤 바뀔 수 있다** `[정적]` (결과서): `ReadPermissions` 를 부르는 곳 = `ReadRemoteActivationKeys +0x17c` (서비스 시작) · `TimerCallbackFn` · `UpdateActSettingsInternal` · 정책 알림 `PolicyCallbackFn`. 넷은 `InitializeSystemSecurityDescriptors` 안에서만 쓰인다 (refs).
- **rpcss 가 등록값을 고쳐 쓴다** `[정적]` (결과서): 기대 ACE 가 없으면 `HandleInvalidRestrictionFromOleKey` → `UpdateComSecurityDescriptors` 가 `MachineAccessRestriction` · `MachineLaunchRestriction` 을 다시 써 넣는다 (옛값은 `…Old`). → VM 레지스트리는 rpcss 가 한 번 뜬 뒤에 떠야 한다.

#### 1-1. 넷의 바이트 `[추측: 계산]` (SDDL 글 `[정적]` + self-relative 규칙)

- 짓는 법 (결과서, `[정적]` 줄): sechost `ConvertStringSecurityDescriptorToSecurityDescriptorW` → `LocalConvertStringSDToSD_Rev1` → `RtlAbsoluteToSelfRelativeSD` → ntdll `RtlMakeSelfRelativeSD`.
- 모양 (`RtlMakeSelfRelativeSD +0xf0 ~ +0x179` `[정적]`, flow-writer 도 읽음): 머리 `0x14` 다음에 **DACL (`+0x118` 복사, 오프셋 `+0x129` → 머리 `+0x10`) → 소유자 (`+0x14a` → `+4`) → 그룹 (`+0x16b` → `+8`)**, Control 에 `0x8000` (`+0x174 ~ +0x179`). SACL 없음 → `+0xc` = 0.
- 그래서 머리 = `01 00 04 80` (리비전 1 · Control `0x8004` = 자기 상대 + DACL 있음) · 소유자 · 그룹 = BA (`S-1-5-32-544`, 16 B). ACL 리비전 2 · ACE = 허용 (형 0 · 깃발 0). 약어 → SID 는 `[규칙]` (BA `S-1-5-32-544` · IU `S-1-5-4` · SY `S-1-5-18` · PS `S-1-5-10` · WD `S-1-1-0` · AN `S-1-5-7` · AC `S-1-15-2-1`).
- 크기: `[0]` 20 + ACL 72 + 16 + 16 = **124** · `[1]` **124** · `[2]` 20 + ACL 180 + 32 = **232** · `[3]` 20 + ACL 176 + 32 = **228**.

```
[0] LaunchPermissions   (124)
010004805c0000006c00000000000000140000000200480003000000000018001f00000001020000000000052000000020020000000014000b000000010100000000000504000000000014000b0000000101000000000005120000000102000000000005200000002002000001020000000000052000000020020000
[1] AccessPermissions   (124)
010004805c0000006c00000000000000140000000200480003000000000014000700000001010000000000050a00000000001400030000000101000000000005120000000000180007000000010200000000000520000000200200000102000000000005200000002002000001020000000000052000000020020000
[2] LaunchRestrictions  (232)
01000480c8000000d800000000000000140000000200b40006000000000018001f00000001020000000000052000000020020000000014000b000000010100000000000100000000000018001f00000001020000000000052000000032020000000018001f0000000102000000000005200000002f020000000018000b000000010200000000000f0200000001000000000038000b000000010a00000000000f0300000000040000a127608f9abb183463b677ff9dd5b66ce7321a680852439286a61fd898171b3b0102000000000005200000002002000001020000000000052000000020020000
[3] AccessRestrictions  (228, 워크스테이션. 서버는 둘째 ACE (AN) 마스크 03 → 07 한 바이트만 다름)
01000480c4000000d400000000000000140000000200b000060000000000140007000000010100000000000100000000000014000300000001010000000000050700000000001800070000000102000000000005200000003202000000001800070000000102000000000005200000002f0200000000180003000000010200000000000f02000000010000000000380003000000010a00000000000f0300000000040000a127608f9abb183463b677ff9dd5b66ce7321a680852439286a61fd898171b3b0102000000000005200000002002000001020000000000052000000020020000
```

- 이 16진은 **계산값**이다 (결과서 · flow-writer 둘 다 같은 규칙으로 계산해 같게 나옴). 진짜와 어긋날 수 있는 곳 (결과서): ACL 안 ACE 차례 · SACL 없을 때 `+0xc` 값 · 소유자 / 그룹 / DACL 배치 차례. VM 에서 `combase!CRpcResolver::_pNoRegAccessRestrictionsSD` 를 덤프하면 닫힌다 (아래 "아직 확인 필요").
- `RtlLengthSecurityDescriptor` 로 `[3]` 을 재면 `0x14 + 16 + 16 + 176 = 228` — 크기 칸과 맞는다 `[추측: 계산 — +0x0 ~ +0x4a 읽는 칸 (결과서 2-1)]`.

### 2. 죽는 사슬 — 명시 `CoInitializeSecurity(SD NULL, cAuthSvc −1, …)` · AppID 없음 [정적]

| # | 줄 (`combase!CoInitializeSecurity` 기준, 다른 함수는 이름) | 하는 일 | 이 길의 조건 · 값 |
|---|---|---|---|
| 0 | 람다 `operator()` (rva `0x18b9c0`) `+0x82e Connect` 이하 (run149) | 응답 검사 → combase `InitializeSystemSecurityDescriptors` → `gGotSecurityData = 1` | 다섯 조각을 `LocalAlloc(0x40, 크기)` + `memcpy` 로 다섯 포인터 전역 (+ 크기 전역 `+0x5c`) 에 복사 — **`LocalAlloc` call `+0x2a` · `+0x62` · `+0x9a` · `+0xd0` · `+0x107`** (ret `+0x31` · `+0x69` · `+0xa1` · `+0xd7` · `+0x10e`) → **전역 넣기 `+0x36` · `+0x6e` · `+0xa6` · `+0xdc` · `+0x113`** → **`memcpy` call `+0x4f` · `+0x8c` · `+0xc2` · `+0xf4` · `+0x12b`** (run152 때 대조). 할당이 NULL 이면 `+0x40` 등 `JZ +0x14e` → `0x8007000e`. **넷째까지는 크기 0 이어도 할당**, 다섯째만 `+0xfc CMP [RDI+0x10],0; JZ` 로 건너뜀 |
| 1 | `+0x47d GetConnection` (ret `+0x482`) · `+0x486 JS` · `+0x48e CMP [gGotSecurityData]` · `+0x494 JNZ → +0xb2` | 본 줄기로 | `Connect` 성공 |
| 2 | `+0xb2 ~ +0xdc` | SD NULL 이면 인증 수준 → 2 (`+0xd7` · `+0xdc`) | 명시 길 |
| 3 | `+0xe2 operator() rva 0x7f438` (ret `+0xe7`) | `OLE\AppCompat` 값 `RaiseActivationAuthenticationLevel` 읽기 (결과서) | 원문 seq `1306 · 1307` 과 같은 일 `[추측: 요청서 인용 대조]` |
| 4 | `+0xeb TEST R12B,8` (안 탐) · `+0xf5 g_AppId` · `+0x116 JZ` · `+0x64a utGetAppIdForModule` (ret `+0x64f`) · `+0x651 JS` | 실행 파일 이름으로 AppID 찾기 → 없음 → `g_AppId` NULL 그대로 | 원문 seq `1319 · 1320 · 1327 · 1330` 과 같은 일 `[추측: 요청서 인용 대조]` |
| 5 | `+0x13d ~ +0x1b3` · `+0x15c ~ +0x16a` | 인수 검사 (틀리면 `0x80070057`). **`[RSP+0x60]` = 능력 `& 0x60`** (`+0x15f AND R15D,0x60` · `+0x16a` 저장) | 통과 |
| 6 | `+0x208 CMP [gpsaSecurity],0` | 이미 있으면 두 번째 호출 → 너무 늦음 | 첫 호출 |
| 7 | **`+0x29c CMP R14D,EDI` · `+0x2a5 MOV ECX,0xa` · `+0x2af LookupPrincName`** (ret `+0x2b4`) · `+0x2b9 TEST EAX,EAX` · `+0x2bb JS → +0xa53` | **lsapolicylookup 이 끼는 자리 (아래 3절)**. 실패면 `+0xa53` (추적 검사) → … → `+0xa97 HeapAlloc(0xc)` → `+0xaa6` `gpsaSecurity` · `+0xab8 [RAX] = 0x20004` (항목 4 · 보안 오프셋 2) · `+0xabe [RAX+4] = 0` → `+0xac2 JMP +0x306` (`TEST ESI,ESI`) → `+0x30e` 합류. **HRESULT 안 바뀜** | `cAuthSvc == −1` |
| 8 | `+0x30e ~ +0x369` | `[RSP+0x50]` (복사한 SD) 가 NULL 이면 `IsValidCOMSecurityDescriptor` (`+0x31b`) 건너뜀. `gAuthnLevel` (`+0x33b`) · `gImpLevel` (`+0x345`) · `gCapabilities` (`+0x362`) · **`gSecDesc` = NULL** (`+0x369`) | SD NULL |
| 9 | `+0x37a CMP [RSP+0x60],R14D` · `+0x37f JNZ → +0x72d` (안 탐) → **`+0x385 GetDefaultAccessRestrictionsSD`** (ret `+0x38a`) | `[0x3380e0]` (다섯째) 가 NULL 이면 `[0x3380c8]` (넷째) (`GetDefaultAccessRestrictionsSD +0xf · +0x16 · +0x20 CMOVNZ`). 둘 다 NULL → `+0x27 JZ` → NULL 반환 → `+0x394 JZ → +0xb20` (추적 검사) → … → **`0x8007000e`** (`R15D`, `+0x296`) | 능력 `& 0x60` 이 0 |
| 10 | `GetDefaultAccessRestrictionsSD +0x29 MOV RCX,RBX` · **`+0x2c CALL [GetSecurityDescriptorLength]`** (ret `+0x33`) | combase 가 임포트한 `GetSecurityDescriptorLength` → KernelBase rva `0xfd2b0` = `48 ff 25 …` (`jmp [IAT 0x29af70]`) → IAT = **ntdll `RtlLengthSecurityDescriptor`** | api-set → KernelBase 연결은 `[추측: 일반 규칙]` (결과서: `apisetschema` 표에서 못 찾음). export 한 줄 · IAT 는 `[정적]` (`pefile`, flow-writer 도 같음) |
| 11 | **`ntdll!RtlLengthSecurityDescriptor`** `+0x0 MOVSX R9D,word [RCX+2]` (Control) · `+0x14 TEST R9W,R9W` · `+0x18 JNS` (비트 15 꺼짐 = 절대 형식 → `+0xb8` 쪽) · 켜짐이면 `+0x1e MOV ECX,[RCX+4]` (소유자 오프셋) · `+0x23 JZ` (0 이면 건너뜀) · `+0x25 ADD RCX,RDX` · `+0x2b JZ` (NULL 만 거름) · **`+0x2d MOVZX ECX,byte ptr [RCX+1]`** | 0 B 덩어리 뒤 쓰레기가 "비트 15 켜짐 + 소유자 오프셋 ≠ 0" 이면 엉뚱한 주소를 SID 로 읽음 → **접근 위반** | 원문: 읽은 주소 `0x1700744cd540`, rip `ntdll+0xe8f3d` = `0xe8f10 + 0x2d` ✓ (에뮬레이터 로그 — `[관찰]` 아님) |
| 12 | (안 죽을 때) `GetDefaultAccessRestrictionsSD +0x46 HeapAlloc(길이)` · `+0x63 memcpy` → `+0x38a gSecDescRestrictions` → `+0x39a CalculateAnonymous…` (반환값 안 봄) → `+0x39f CoVrfCheckSecuritySettings` → `+0x3a4 JMP +0x3b8` → … → **HRESULT 0** | 진짜 SD 면 여기까지 | (결과서: `g_AppId` NULL 이면 `SetAppID` 안 부름) |

- **검사는 없다** `[정적]`: `GetDefaultAccessRestrictionsSD` 는 NULL 검사 (`+0x24 TEST RBX,RBX`) 하나뿐 — 길이 → `HeapAlloc` → `memcpy`. 실패 HRESULT 가 나는 길은 둘: 둘 다 NULL, 또는 `HeapAlloc` 실패 → 둘 다 `0x8007000e`.
- `RtlLengthSecurityDescriptor` 는 맨 처음 (`+0x0`) 에도 `[RCX+2]` 를 읽는다. 그러니 0 B 덩어리면 **Control 부터 이미 덩어리 밖**이다. 거기서 안 죽고 `+0x2d` 에서 죽은 것은 덩어리 바로 뒤는 읽을 수 있는 힙이고, 그 쓰레기 안 오프셋이 먼 주소를 가리켰기 때문으로 보인다 `[추측: 원문 rip · 주소에서]`.
- `CalculateAnonymousGrantedRemoteAccessInRestrictions` 는 같은 SD 로 익명 접근 검사를 하지만 반환값을 안 본다 → 소유자 · 그룹이 없는 SD 같은 꼴도 HRESULT 는 안 바꾼다 (결과서 `[정적]`). 그 안 syscall 은 안 따라감 `[공백]`.

#### 2-1. 암묵 길 (`InitRemoting +0xb6 InitializeSecurity` → 능력 8 `EOAC_APPID`) 은 같지 않다

| 단계 | 명시 (SD NULL, 능력 0) | 암묵 (능력 8) |
|---|---|---|
| `+0xeb TEST R12B,8` · `+0xef JNZ → +0x5cf` | 안 탐 | **`+0x5e7 GetLegacyBlanket`** (ret `+0x5ec`) |
| `GetLegacyBlanket` | — | AppID 없음 → `HKLM\SOFTWARE\Microsoft\OLE` 값 `DefaultAccessPermission` (`+0x299 GetRegistrySecDesc`) → 값 없음 (원문 `[수집]`) → `+0x4b7 GenerateDefaultAccessPermissionsSecurityDescriptor(NULL, …)` |
| `Generate…` | — | **둘째 조각** `_pNoRegAccessPermissionsSD` (`+0x41`) + 크기 (`+0x48`) 를 기준 SD 로 · `LsaLookupOpenLocalPolicy` (`+0x1d9799`) → `LsaLookupGetDomainInfo` (`+0x1d97dc`) → `CreateWellKnownSid` (`+0x1d984b`, 종류 110 · 도메인 SID — 결과서) ACE → `AddAcesToSecurityDescriptorViaApi` (`+0x122`) |
| 포트 실패 | — | **`CoInitializeSecurity` 가 그 에러로 끝남** (`+0x5f3 JS → +0x87b`, 값은 wil 변환 `[공백: 정확한 HRESULT]`) — 명시 길과 반대로 치명 |
| 그 뒤 | 같음 | 같음 — `+0x385` 까지 똑같이 감 (`gSecDesc` 만 `GetLegacyBlanket` 이 만든 SD) |

→ 암묵 길은 **둘째 조각 (124 B) 이 쓰이고 lsapolicylookup 을 반드시 연다.** 원문 로그 (`seq 1217` 명시 `CoInitializeSecurity(SD NULL, −1, …)` → `1339 ~ 1353` 연결 → `1354` 죽음) 은 **명시 길** (표 2 의 7 → 9) 과 맞는다 `[추측: 요청서 인용 대조 — 로그는 지워져 직접 못 봄]`.

#### 2-2. `GetSystemSecurityDescriptors` (`CoGetSystemSecurityPermissions +0xf` 의 일꾼) [정적]

- `+0xf GetConnection` (ret `+0x14`) → `_dwFlags & 1` 꺼짐이면 `0x80004005` (`+0x16f`) → 종류 (0 ~ 3) 별로 넷 중 하나 (`+0x11b`/`+0x14b` 첫째 · `+0xe2` 둘째 · `+0xa5`/`+0xd9` 셋째 · `+0x65`/`+0x99` 넷째) 를 길이 → `LocalAlloc` → `memcpy` 로 복사해 돌려줌. **다섯째는 안 준다.**
- 부르는 곳은 공개 API `CoGetSystemSecurityPermissions +0xf` 하나 (결과서 refs) → **초기화 길에선 안 불린다**. 원문 "죽기 전에 불리나" `[모름]` 은 닫힘.

### 3. `lsapolicylookup` — 끼지만 (명시 길에선) 지나간다 [정적]

**한 줄**: `\RPC Control\lsapolicylookup` 은 SID ↔ 이름 찾기 (LSA lookup) 창구다. 명시 `CoInitializeSecurity(SD NULL, −1)` 에서는 `LookupPrincName (+0x2af) → LookupAccountSidLocalW (sechost) → LsaLookupOpenLocalPolicy → 포트` 로 끼지만, 실패해도 `CoInitializeSecurity` 가 **삼키고 지나가** (기본 보안 배열 · HRESULT 0) SD 짓기와 무관하게 `+0x385` 에서 죽는다. 암묵 길 (`CoInitializeSecurity` 없이 COM 사용) 에선 **낌 (치명)**.

| 단계 | 줄 | 설명 |
|---|---|---|
| 1 | combase `LookupPrincName +0x5e GetSelfSid` (ret `+0x63`) | 내 프로세스 토큰 사용자 SID (원문 seq `1310 ~ 1313 · 1333` 과 같은 일로 보임 `[추측]`) |
| 2 | `+0x82 CMP R12W,0xa` · `+0x116 LookupAccountSidLocalW` (ret `+0x11d`) · `+0x124 JZ` → 조각에서 한 번 더 | 인증 서비스 `0xa` 일 때 이 길. **최대 두 번** (크기 읽고 다시 — 결과서) |
| 3 | sechost `LookupAccountSidLocalW +0x20` → `LookupAccountSidInternal +0x33` (`LsaLookupClientSidCacheEnabled` 비교) · `+0xdd LsaLookupOpenLocalPolicy` (ret `+0xe2`) | SID 캐시가 꺼져 있으면 곧장 정책 열기 |
| 4 | `LsaLookupOpenLocalPolicy +0x42 NdrClientCall3` (ret `+0x49`) · 바인딩 콜백 `PLSA_LOOKPR_SERVER_NAME_bind +0x3f RpcStringBindingComposeW("ncalrpc", "lsapolicylookup")` · `+0x5b RpcBindingFromStringBindingW` | **`ept_map` 없이 곧장 ALPC 연결** (원문 "연결 8 번" 의 상대) |
| 5 | 실패 → `LookupAccountSidInternal +0xe4 JS → +0x26c` (`RtlNtStatusToDosError` → `SetLastError` → FALSE, 결과서) | |
| 6 | `LookupPrincName` 둘째도 실패 → `GetLastError \| 0x80070000` · `+0x133 TEST EBX,EBX` → 에러 반환 (결과서) | |
| 7 | `CoInitializeSecurity +0x2b9` · `+0x2bb JS → +0xa53` | **삼킴** — 표 2 의 7 |

- "8 번" 의 이유: sechost 안엔 재시도 반복이 없다 (호출 두 번 × `NdrClientCall3` 한 번씩). 나머지는 rpcrt4 `ncalrpc` 연결 재시도로 읽히지만 rpcrt4 를 안 열었다 `[공백]`.
- 진짜 rpcss 도 서비스 시작 때 같은 포트를 연다 (`CommonServiceMain +0x15a StartTokenHandling → LsaLookupOpenLocalPolicy`, 결과서) — 서버의 일이라 에뮬레이터 클라이언트 쪽엔 상관없다.

### 4. Q284a [정적]

#### 4-1. 프로시저 6 · 7 — **첫 부름은 6**

| 항목 | 프로시저 7 (`ServerAllocateOIDs`) | 프로시저 6 (`ServerAllocateOXIDAndOIDs`) |
|---|---|---|
| 서버 | rpcss `_ServerAllocateOIDs` (rva `0x213e0`) → `ServerAllocateOIDsInternal` (`0x26700`) | rpcss `_ServerAllocateOXIDAndOIDs` (rva `0x13990`) |
| 부르는 줄 | combase `CallTheResolver +0x106 NdrClientCall2` (ret `+0x10d`, 서식 `0x2c2994` — 결과서) | `CallTheResolver +0x1b7 → RegisterOXIDAndOIDs +0x78 → ServerRegisterOXID +0xce NdrClientCall2` (ret `+0xd5`, 서식 `0x2c292e`). 그 앞 `+0x30 StartListen` (음수면 그 HRESULT) |
| 언제 | `CallTheResolver +0x94 MOV AL,[RCX+0xd8]` (`RCX = [RDI+0x10]` = 아파트의 **`OXIDEntry`**, `RDI` = `CComApartment` — 칸은 **`OXIDEntry+0xd8` BYTE 등록 표시**, run152 때 대조) · `+0x9b TEST AL,AL` · `+0x9d JZ` **안 탐** = 아파트 OXID 가 이미 등록 | **`+0x9d JZ → +0x1ad`** = 미등록 → **첫 부름은 이쪽** (첫 때 그 칸이 0 인 것은 `[추측: OXIDEntry 생성자 안 읽음]`) |
| 입력 | 핸들 · `ph` (Connect 컨텍스트 핸들) · `OXID*` (`+0xa3` `[OXIDEntry+0xc8]` 사본) · 더할 OID 수 (**`[아파트+0xd8]` DWORD** — `+0x33 MOV R15D,[RCX+0xd8]`, `RCX` = `CComApartment`; 위 `OXIDEntry+0xd8` 과 **다른 구조체의 칸**. 읽은 뒤 `+0x54` 0 으로, `+0xfc` 인자로) · 배열 (`[아파트+0xe0]` 을 `+0x4f memcpy` 로 지역 `[RBP]` 에) · **원하는 수 = `0x14 − [아파트+0x30]`** (OID 버퍼 20 칸 중 빈 칸) · 출력 둘 (결과서) | 핸들 · `ph` · 출력 `OXID*` · `OXID_INFO` (0x98 B) · 로컬 DSA (`gpsaCurrentProcess`) · 보안 DSA (`gpsaSecurity` — 표 2 의 7 에서 만든 것) · OID 입출력 (결과서) |
| 서버 status | `ph` 모름 · PID 다름 → **5**; 널 인수 → **`0x57`**; OXID 없음 → **`0x776`** (`ServerAllocateOIDsInternal +0x143`); 메모리 → `0xe` (결과서) | 깃발 · 모양 틀림 → **`0x57`**; `CheckLocalSecurity` 실패 → **5**; 로컬 / 보안 DSA `DsaValid` 거짓 → **`0x57`**; 그 밖 `CServerOxid::Create` (`+0x20a`) 상태 |
| 출력 | 새 OID (서버가 `ProcessPrng` 8 B 난수, `ServerAllocateOIDsInternal +0x198`) · 개수 · 상태 0 | OXID · (선택) 서버 DSA (NULL 가능) · OID · 상태 |
| combase 검사 | `+0x117 RetryRPC` · `+0x122 CheckStatus` (run149 표 3: `0x6bb` 만 재시도 · 양수 N → `0x8007NNNN`). 성공이면 `+0x172 memcpy` 로 `min(돌려준 수, 빈 칸)` 개 넣고 `[아파트+0x30] += 돌려준 수` (**잘라 더하지 않음** — 많이 주면 20 을 넘음, 결과서) | `+0xf2 RetryRPC` · `+0x101 CheckStatus`; 돌려받은 DSA 가 비-NULL 인데 `+0x115 DsaValid` 거짓 → **`0x8000ffff`** (`+0x1e1`); 이어 `RegisterOXIDAndOIDs +0x87 ~ +0xb1` 가 OXID (`+0x93` → `[OXIDEntry+0xc8]`) · MID (`+0xa7` → `+0xd0`) 저장 · 등록 표시 (`+0xb1 XCHG [OXIDEntry+0xd8],1` — `CallTheResolver +0x94` 가 보는 바로 그 칸). primary `OXIDEntry` 의 그 칸이 0 이면 거기에도 같은 셋 (`+0x12d` · `+0x13b` · `+0x142`) (run152 때 대조) |
| 안전한 최소 대답 | 상태 0 · 개수 ≤ 원하는 수 (결과서; 그 뒤는 `[추측]`). 솔직한 실패 `0x776` → `0x80070776` | 상태 0 · **OXID ≠ 0** · DSA NULL. 서버 흉내가 보안 DSA 를 `DsaValid` 로 보면 combase 가 보낸 것은 4 항목 기본 꼴 `{wNumEntries 4, wSecurityOffset 2, …}` (명시 길 lsapolicylookup 실패 때) |

#### 4-2. `rpcss!LocalInterfaceOnlySecCallback` — 반환 0 = 통과 · 5 = 거절

| # | 줄 | 보는 것 | 거절 |
|---|---|---|---|
| 1 | `+0x56 I_RpcBindingInqTransportType` (ret `+0x5d`) · `+0x6a CMP [RBP-0x80],4` | 전송이 ncalrpc (4) 인가 | 아니면 5 (`+0x570 MOV EAX,5`) |
| 2 | `+0xb0 RpcImpersonateClient` · `+0xff OpenThreadToken` · `+0x116 RpcRevertToSelfEx` · `+0x12c LookupOrCreateTokenFromHandle` | 클라이언트 토큰 | 실패 → 5 |
| 3 | `+0x156 [0x1656b0] gpDefaultAccessRestrictionsSD` (없으면 `+0x586 [0x1656d0] gpNoRegAccessRestrictionsSD`) | **Connect 응답 `[4]` / `[3]` 과 같은 SD** | |
| 4 | `+0x1d4 GetSecurityDescriptorDacl` | DACL 없음 · NULL DACL 이면 무조건 통과 (결과서) | DACL 읽기 실패 → 5 |
| 5 | `+0x242 AccessCheckByType` (ret `+0x249`) | 얻은 권한 비트 | 호출 실패 → 5 |
| 6 | (결과서 `+0x25d ~ +0x274`) | 비트 `0x2` (COM "실행 · 로컬") 가 있으면 통과 (`+0x145 MOV EBX,5` 로 시작, 통과 갈래만 `+0x40f XOR EBX,EBX`, `+0x44b MOV EAX,EBX`) | 못 얻으면 5 |
| 7 | (결과서 `+0x2a6 ~ +0x2c8`) 인터페이스가 `00000136-…` 일 때만 실행 제한 둘째 검사 | ILocalObjectExporter 는 안 탐 `[추측: RCX = 인터페이스 UUID]` | |

→ 진짜 SD 면 Everyone `0x7` / `0x3` 에 비트 `0x2` 가 있어 로컬 test.exe 꼴은 **통과**. 에뮬레이터는 rpcss 서버를 흉내내므로 이 콜백을 구현할 필요는 없다 (결과서).

#### 4-3. `MakeSCMProxy` · `DelegateActivationToSCM` — "클래스 없음" 은 combase 가 안 만든다

- `DelegateActivationToSCM +0x156 BindToSCMProxy` → `+0xc InitChannelIfNecessary` · `+0x17 GetConnection` · `+0x38 CMP [_pSCMProxy],0` · **`+0x49 MakeSCMProxy`** → `+0x12 MakeProxyHelper` (안 읽음 `[공백]`) · `+0x1b JS` · `+0x5f CoSetProxyBlanket` · 실패면 `+0x88` 간접 `Release`.
- `DelegateActivationToSCM +0x75b GetSCM` → **`+0x789` 간접 호출** (ret `+0x78e`, ISCM 메서드) → 음수면 `+0x7aa Return_Hr` — 돌아온 값을 **그대로** 올린다. 이 함수들 `.asm` 에 `0x8004…` "클래스 없음" 상수가 없다 (결과서) → rpcss 활성화 서버가 정한다 `[공백: 서버 몸]`.
- `+0x226 GetEnvBlock` 실패 → `+0x22f MOV EBX,0x8007000e`.

### 호출 관계

```
rpcss (svchost -k rpcss) — 서비스 시작                                                       [정적]
InitializeSCMBeforeListen
 ├─ +0x1aa  InitializeSystemSecurityDescriptors (0x73e04)
 │           ├─ +0x55 · +0xd4  ConvertSidToStringSidW (DCOM Users · Perf Log Users)
 │           ├─ +0x27c IsWorkstationOS → 형식 0x147050 / 0x146ed0 · +0x2a6 · +0x2cd StringCbPrintfW
 │           └─ +0x2fc · +0x3a6 · +0x44c · +0x4f2  ConvertStringSecurityDescriptorToSecurityDescriptorW
 │                                         → gpNoReg{LaunchPerm, AccessPerm, LaunchRestr, AccessRestr}SD
 └─ +0x1ba  ReadRemoteActivationKeys → ReadPermissions → SetDefaultAccessRestrictions (레지스트리 MachineAccessRestriction)

rpcss!_Connect  +0x40f ~ +0x483 크기 다섯 · +0x471 GetDefaultAccessRestrictions (NULL 이면 다섯째 없음) · +0x5df memcpy 루프

combase (클라이언트)
CoInitializeSecurity (0x7e880)                                                                  [정적]
 ├─ +0x482  GetConnection → … Connect ⇐ 응답 → InitializeSystemSecurityDescriptors (크기 0 이어도 LocalAlloc; 다섯째만 건너뜀)
 ├─ +0xe7   operator() (0x7f438)  OLE\AppCompat
 ├─ +0x64f  utGetAppIdForModule   AppID 없음
 ├─ +0x2b4  LookupPrincName(0xa)
 │           ├─ +0x63  ProcessToken::GetSelfSid
 │           └─ +0x11d LookupAccountSidLocalW (sechost) +0x25 LookupAccountSidInternal +0xe2 LsaLookupOpenLocalPolicy
 │                       +0x49 NdrClientCall3 ── ALPC ──▶ ncalrpc:[lsapolicylookup]   실패 → 삼킴 (+0x2bb JS → +0xa53 → +0xa9e 기본 gpsaSecurity)
 └─ +0x38a  GetDefaultAccessRestrictionsSD (0x7e5f4)   [0x3380e0] ?: [0x3380c8]
             └─ +0x33  GetSecurityDescriptorLength (KernelBase jmp) → ntdll!RtlLengthSecurityDescriptor   ✗ +0x2d (0 B 덩어리면)

(암묵) InitRemoting +0xbb InitializeSecurity → CoInitializeSecurity 능력 8
 └─ +0x5ec  GetLegacyBlanket +0x4bc GenerateDefaultAccessPermissionsSecurityDescriptor
             └─ LsaLookupOpenLocalPolicy · LsaLookupGetDomainInfo · CreateWellKnownSid · +0x127 AddAcesToSecurityDescriptorViaApi   실패 → 끝

(첫 COM 사용) CallTheResolver
 ├─ +0x9d JZ ([OXIDEntry+0xd8] 0 = 미등록) → +0x1bc RegisterOXIDAndOIDs +0x7d ServerRegisterOXID  +0x35 StartListen · +0xd5 NdrClientCall2 (프로시저 6)  ← 첫 부름
 └─ (등록 뒤)                      +0x10d NdrClientCall2 (프로시저 7)
```

- (나무 오프셋은 ret 자리, `+0x40f ~ +0x483` · `+0x9d` 는 함수 안 줄. `── ALPC ──▶` 는 다른 프로세스 — rpcrt4 안 길은 이번에 안 읽음. `✗` = 원문의 죽는 줄. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (결과서 "지금 PE_emulator 맞나 · 틀리나")

flow-writer 는 원문 380 ② 절 · "받을 꼴" 만 읽었다 (읽기만). `rpcss.cpp` · `alpc_frontend.cpp` 는 안 읽음 (결과서도). 고칠지는 사용자 · PE_emulator 결정.

### 받을 꼴 ① — SD 조각 표 (원문 "조각 · 출처 (가) / (나) / (다) · 바이트 또는 레지스트리 값 이름 · combase 가 읽는 줄 · 0 가능 여부")

| 조각 | 출처 | 바이트 또는 레지스트리 값 이름 | combase 가 읽는 줄 | 0 가능 여부 |
|---|---|---|---|---|
| `[0]` 실행 권한 `gpNoRegLaunchPermissionsSD` | (가) SDDL 상수 `0x1472b0` | 124 B — 1-1 의 `[0]` 16진 `[추측: 계산]` | 초기화 길은 **안 읽음**. 공개 API `GetSystemSecurityDescriptors +0x11b · +0x14b` (종류 0) 만 | 진짜: 불가. combase 초기화 길: 0 이어도 안 읽어 지나감 — 그래도 진짜 바이트를 주는 게 안전 |
| `[1]` 접근 권한 `gpNoRegAccessPermissionsSD` | (가) `0x147240` | 124 B — `[1]` 16진 `[추측: 계산]` | 명시 길 안 읽음. **암묵 (`EOAC_APPID`) 길** `GenerateDefaultAccessPermissionsSecurityDescriptor +0x41 · +0x48` (`DefaultAccessPermission` 없을 때). 공개 API `+0xe2` | 진짜: 불가. 명시 길: 0 이어도 지나감. **암묵 길: 진짜 바이트 필요** (ACE 를 덧붙임) |
| `[2]` 실행 제한 `gpNoRegLaunchRestrictionsSD` | (가) `0x147320` + BUILTIN SID 둘 (`S-1-5-32-562` · `S-1-5-32-559`) | 232 B — `[2]` 16진 `[추측: 계산]` | 공개 API `+0xa5 · +0xd9` (종류 2) 만 | 진짜: 불가. 초기화 길: 안 읽음 |
| `[3]` 접근 제한 `gpNoRegAccessRestrictionsSD` | (가) `0x147050` (워크스테이션, AN `0x3`) / `0x146ed0` (서버, AN `0x7`) + BUILTIN SID 둘 | 228 B — `[3]` 16진 `[추측: 계산]` | **`GetDefaultAccessRestrictionsSD +0x16` (다섯째 없을 때) ← `CoInitializeSecurity +0x385` (명시 · 암묵 둘 다)** → `+0x2c` 길이 → `RtlLengthSecurityDescriptor +0x2d`. 공개 API `+0x65 · +0x99` | 진짜: 불가. **combase: 반드시 유효한 자기 상대 SD** (여기서 죽음) |
| `[4]` 기본 접근 제한 `GetDefaultAccessRestrictions()` | **(나)** 정책 `HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM` → 없으면 `HKLM\SOFTWARE\Microsoft\OLE` | 값 `MachineAccessRestriction` (원문 `[수집]` 228 B) | `GetDefaultAccessRestrictionsSD +0xf` 가 **먼저 봄** — 있으면 이것 | **가능 (진짜에서도)** — 둘 다 없거나 검사에 걸리면 빠짐. 0 이면 `[3]` 으로 감 |

- (다) 기계 · 부팅마다 다른 SID 는 없다 → 원문 "1 이 (다) 면 Q283 D5 · D8 에 얹음" 의 조건에는 안 걸린다. 계산값 대조용으로 "읽기만" 얹을 수는 있다 (아래).

### 받을 꼴 ② — 응답 칸 28 ~ 30 에 쓸 값

| 칸 | 값 |
|---|---|
| 28 `ulong[5]` | **`{124, 124, 232, 228, 0}`** — 다섯째 0 = "정책 · 등록값 없음", 진짜에서도 나는 꼴 `[정적]`. VM 에 더 가깝게 하려면 `{124, 124, 232, 228, 228}` + 다섯째 = VM 의 `MachineAccessRestriction` 바이트 (`[수집]`) |
| 29 총 크기 | **708** (다섯째 0) / 936 (다섯째 228) `[추측: 계산 — 합]` |
| 30 덩어리 | 1-1 의 `[0][1][2][3]` 16진을 **패딩 없이 이어 붙인 자기 상대 SD 넷** (+ 다섯째) `[추측: 계산]` |

- **다섯째 0 을 권하는 이유** (결과서): combase 는 다섯째가 없으면 넷째를 쓴다. VM 의 `MachineAccessRestriction` 이 `[3]` 과 같다면 (크기는 같음) 결과도 같다 `[추측: 바이트 같음은 VM]`.
- 이 값이 서면 `gSecDescRestrictions` 가 서고 `CoInitializeSecurity` 는 HRESULT 0 으로 끝난다 (`CalculateAnonymous…` 의 토큰 · 접근 검사 syscall 은 에러여도 HRESULT 에 영향 없음 — 결과서 `[정적]`, 어떤 syscall 인지는 `[공백]`).

### 받을 꼴 ③ — 금지 꼴 (접근 위반 · 실패 HRESULT 가 나는 응답)

1. **`[3]` 크기 0 + `[4]` 도 0** (지금 "0 ×5") — combase 가 0 B 비-NULL 덩어리를 골라 길이 함수가 밖을 읽음 → **접근 위반** (BG05 와 같은 꼴).
2. **SD 안 모양이 틀림** — Control 비트 15 켜짐 + 소유자 · 그룹 오프셋이 조각 밖 → 접근 위반 (`+0x2d` / 그룹 쪽 같은 꼴). **비트 15 꺼짐 (절대 형식)** 이면 `[+4]` · `[+8]` 을 **주소로** 따라감 → 접근 위반. → **자기 상대로 줄 것.**
3. **크기 합 > 총 크기 · 덩어리** (run149 금지 5 그대로) — combase `memcpy` 가 덩어리 밖을 읽음.
4. **`[4]` 가 비-NULL 인데 쓰레기** — `[3]` 대신 이것이 쓰여 같은 사슬로 죽음. 모르면 `[4]` = 0.
5. 참고: 크기 0 은 combase 전역을 NULL 로 만들지 못한다 (늘 0 B 비-NULL — 위 1). `LocalAlloc` 자체가 NULL 이면 `0x8007000e` (결과서).

### 받을 꼴 ④ — `lsapolicylookup` 한 줄 · LSA1 판단

- **명시 `CoInitializeSecurity(SD NULL, −1, …)` 표본 (r19 OR1): 끼지만 실패를 삼키고 지나감 → LSA1 은 OR1 앞 바퀴가 아니다.** 창구가 없어도 · 있어도 `+0x385` 에서 똑같이 죽는다 — 칸 28 ~ 30 만 고치면 된다.
- **`CoInitializeSecurity` 없이 COM 을 쓰는 표본 (암묵 `EOAC_APPID`, 결과서 예: r18 432): `lsapolicylookup` (도메인 정보 `LsaLookupGetDomainInfo` 포함) 이 먼저 필요 → 이쪽은 LSA1 이 OR1 앞 바퀴.**

### 다음 벽 · stub 이 채울 최소 목록 (run149 목록에 이어)

1. 위 SD 다섯 조각 (칸 28 ~ 30) — `Connect` 가 서려면 필요한 **마지막 칸**.
2. 그 밖은 run149 "최소 대답" 3 그대로 (단 "SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL" 은 위 값으로 바꿈).
3. **다음 벽 = 첫 COM 사용 때 프로시저 6 (7 아님)** — `ServerRegisterOXID +0x30 StartListen` 이 먼저 (에뮬레이터가 rpcrt4 서버 쪽 호출을 소화해야 함 `[추측: StartListen 안 안 읽음]`) → 대답 `상태 0 · OXID ≠ 0 · DSA NULL`. 그 뒤 OID 를 더 받을 때 7 (`상태 0 · 개수 ≤ 원하는 수`). 몸을 짓기 전엔 솔직한 실패 (`0x6ba` — run149 권고, 7 이면 `0x776` 도 가능).
4. `LocalInterfaceOnlySecCallback` 은 구현 불필요 (서버 흉내). 거절 흉내를 내면 5 → `0x80070005`.

## 확인된 것

- `[정적]` rpcss 넷 `gpNoReg*SD` 는 `InitializeSystemSecurityDescriptors` (`InitializeSCMBeforeListen +0x1a5`) 가 `.rdata` SDDL 상수 넷 (`0x1472b0` · `0x147240` · `0x147320` · `0x147050`/`0x146ed0`) 으로 짓고, 둘에 BUILTIN SID 둘 (`gStartupSids` 종류 61 · 58) 을 끼우고, 접근 제한은 `IsWorkstationOS` 로 형식을 고른다.
- `[정적]` 다섯째 (`GetDefaultAccessRestrictions`) 는 레지스트리 `MachineAccessRestriction` (정책 키 먼저) 이고, NULL 이면 `_Connect +0x47b` 가 칸 `[4]` 을 안 쓴다.
- `[정적]` combase `InitializeSystemSecurityDescriptors` 는 넷째까지 크기 0 이어도 `LocalAlloc` 하고 다섯째만 `+0xfc` 로 건너뛴다.
- `[정적]` 명시 `CoInitializeSecurity` 의 `+0x385 GetDefaultAccessRestrictionsSD` → `+0x2c GetSecurityDescriptorLength` → (KernelBase `jmp [IAT]`) → `ntdll!RtlLengthSecurityDescriptor`, 길이 전 검사 없음. `+0x2d` = `0f b6 49 01`.
- `[정적]` `LookupPrincName` → `LookupAccountSidLocalW` → `LsaLookupOpenLocalPolicy` → `ncalrpc:[lsapolicylookup]`, 실패면 `CoInitializeSecurity +0x2bb JS` 갈래가 기본 `gpsaSecurity {0x20004, 0}` 을 만들고 `+0x306` 으로 합류.
- `[정적]` `CallTheResolver +0x9d JZ` — `[OXIDEntry+0xd8]` (BYTE, `RCX = [아파트+0x10]`) `== 0` 이면 `RegisterOXIDAndOIDs` (6), 아니면 `NdrClientCall2` (7). 프로시저 7 의 더할 수 `[아파트+0xd8]` (DWORD) 은 다른 칸 (run152 때 대조).
- `[관찰]` test.exe 초기화 끝 모듈 6개에 combase · sechost 가 없다 (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`) — 이번 길은 test.exe 에선 안 생긴다.

## flow-writer 대조

결과서 오프셋 · 바이트를 `.asm` 과 `pefile` 로 맞춰 봤다. **결론 (SD 출처 · 죽는 사슬 · lsapolicylookup · 6 먼저) 을 바꾸는 어긋남은 없다.** 줄 표기만 고친 곳:

1. **`CoInitializeSecurity` 점프 목적지 표기** — 결과서의 `+0xeb8e` · `+0xefad` · `+0xee4f` · `+0xec38` · `+0xf0fb` 는 **주소 끝자리** (`0x18007eb8e` 등) 이다. 함수 시작 (`0x18007e880`) 기준으로는 `+0x30e` · `+0x72d` · `+0x5cf` · `+0x3b8` · `+0x87b`. 이 문서는 함수 기준으로 적었다.
2. **`+0x394 JZ → +0x1dbe65`** — 바로 가지 않고 `+0xb20` (`0x18007f3a0`, 추적 검사) 을 거쳐 `+0xb34 JZ` 로 조각 `+0x1dbe65` 에 간다. 끝 값 `0x8007000e` 는 같다.
3. **lsapolicylookup 실패 갈래** — 결과서 "`+0xa79` 조각: `HeapAlloc(0xc)`" 의 `+0xa79` 는 명령 경계가 아니다. `.asm`: `+0xa53` 추적 검사 → … `+0xa97 HeapAlloc` (ret `+0xa9e`, 크기 `+0xa91 LEA R8D,[R14+0xc]`) · `+0xaa6` `gpsaSecurity` 저장 · `+0xab8 [RAX] = 0x20004` · `+0xabe [RAX+4] = 0` · `+0xac2 JMP +0x306`. 결과서의 "`+0xeb8e` 합류" 는 `+0x306` (`TEST ESI,ESI; JS`) 을 지나 `+0x30e`.
4. **`GetDefaultAccessRestrictionsSD` 의 `HeapAlloc`** — 결과서 "`+0x38 HeapAlloc`" → call 은 **`+0x46`** (ret `+0x4d`). `+0x38` 은 `g_hHeap` 읽는 줄.
5. **call 자리 다섯** (ret 는 결과서와 같음): `PLSA_LOOKPR_SERVER_NAME_bind` `RpcBindingFromStringBindingW` = **`+0x5b`** (결과서 `+0x56` 은 `LEA`) · `LocalInterfaceOnlySecCallback` `I_RpcBindingInqTransportType` = **`+0x56`** (결과서 `+0x46`) · `RpcImpersonateClient` = **`+0xb0`** (결과서 `+0xa0`) · `MakeSCMProxy` `MakeProxyHelper` = **`+0x12`** (ret `+0x17`, 결과서 `+0xc`) · `DelegateActivationToSCM` `GetSCM` = **`+0x75b`** (ret `+0x760`, 결과서 `+0x759`).
6. **`ServerAllocateOIDsInternal` "메모리 → `0xe` (`+0x1a8`)"** — `+0x1a8` 은 명령 경계가 아니다 (`+0x1a6 JZ` · `+0x1ac`). 값 `0xe` 는 결과서 그대로 두고 줄은 못 맞췄다.
7. **확인만 (결과서 맞음)**: `+0x37a CMP [RSP+0x60],R14D` 의 `[RSP+0x60]` 은 `+0x15c ~ +0x16a` 에서 `능력 & 0x60` 을 넣은 칸 — 결과서 "능력 비트 `& 0x60`" 그대로.
8. **SD 16진** — rpcss `.rdata` 글 다섯을 `pefile` 로 다시 읽고 (결과서 인용과 같음), 같은 규칙 (머리 → DACL → 소유자 → 그룹, Control `0x8004`, ACL 리비전 2, 약어 표) 으로 따로 계산 → 넷 다 결과서 16진과 **바이트까지 같다** (124 · 124 · 232 · 228). 같은 규칙이라 독립 증거는 아니다 → `[추측: 계산]` 유지.
9. **`gStartupSids`** 9칸 · KernelBase `0xfd2b0` `48 ff 25 b9 dc 19 00` → IAT `0x29af70` = ntdll `RtlLengthSecurityDescriptor` · ntdll `+0xe8f3d` `0f b6 49 01` · sechost "ncalrpc" / "lsapolicylookup" · rpcss `0x131af0` / `0x1465f0` 글 — 결과서와 같음.

## 정정

- **run149** (`runs/03_sprint/run149-com-rpcss.md`) — 두 곳. 원문은 그대로 두고 문서 맨 위에 "정정 · 고쳐 읽기 (run151)" 두 줄을 넣었다 (그래서 아래 줄 번호가 4 씩 밀림).
  - (a) **SD 크기 0** — 2-2 응답 칸 표 칸 28 줄 (원래 `:201`, 지금 `:205`) "가능 (라) | 0 ×5" · "최소 대답" 3 (원래 `:315`, 지금 `:319`) "SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL".
    - 틀린 말: 크기 0 ×5 여도 지나간다.
    - 고쳐 읽기: "(라) 는 combase `InitializeSystemSecurityDescriptors` **안에서만** 맞다. 뒤에서 `CoInitializeSecurity +0x385 GetDefaultAccessRestrictionsSD` 가 다섯째 (없으면 넷째) 를 `GetSecurityDescriptorLength` 로 읽는다 → 넷째가 0 B 덩어리면 `ntdll!RtlLengthSecurityDescriptor +0x2d` 접근 위반. 칸 28 = `{124, 124, 232, 228, 0}` · 29 = 708 · 30 = 자기 상대 SD 넷 (sast/148 `[정적]` + `[추측: 계산]`)".
  - (b) **프로시저 차례** — "7 → 6" (원래 `:21` · `:238` · `:284` 나무 · `:332` · `:353`, 지금 `:25` · `:242` · `:288` · `:336` · `:357`).
    - 틀린 말: 첫 COM 사용이 프로시저 7 → 6 차례로 보낸다.
    - 고쳐 읽기: "`CallTheResolver +0x9d JZ` — 아파트 OXID 미등록 (`[..+0xd8] == 0`) 이면 `RegisterOXIDAndOIDs` → **6 먼저**, 7 은 등록된 뒤 OID 를 더 받을 때. 첫 부름 때 그 칸이 0 인 것은 `[추측]`. 그래서 '그다음 벽' 은 프로시저 6 (`StartListen` 먼저)".
    - 원래 표 · 나무의 call 자리 (`+0x106` · `+0x1b7`) 는 맞다. 틀린 것은 "차례" 말뿐이다.
- **run149 해설서** (`runs/03_sprint/run149-com-rpcss-해설.md`) — 맨 위 두 줄 (원문 그대로, 줄 번호 4 씩 밀림):
  - (a) 2.8 표 칸 28 ~ 30 줄 (원래 `:436`) "0 ×5 · 0 · NULL" · 3절 최소 칸 3 (원래 `:638`) "SD 0".
  - (b) "메뉴 7 → 6" (원래 `:114` · `:208` 나무 · `:590` · `:619` 비유 · `:654` · `:730` · `:735`).
  - 맨 위 줄 쓰는 말은 해설서 말투 (메뉴 · 0 B 덩어리) 로 맞췄다. 해설서 본문은 explainer 몫이라 안 고쳤다.
- 고칠 것 없음: sast/144 결과서 (`_workspace/`) 는 flow-writer 가 고치지 않는다 — 결과서 쪽 정정은 이 run 과 sast/148 결과서로 남는다.

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (combase · sechost 안 올림) 이고, 읽은 것은 다른 프로세스 (rpcss) 의 상수 · 레지스트리와 COM 클라이언트 길이다.

## 아직 확인 필요

동적 (결과서 "VM — 디버거 필요" — 새 번호 없이 **Q283 D5 · D8 에 "읽기만" 얹기**. 필수 아님, 계산값 대조용. 멈춤점은 test.exe 가 아니라 COM 을 쓰는 프로세스 · svchost rpcss 조건):

1. **D5 뒤 (`combase!CoInitializeSecurity+0x385` 직전)** — `dq combase!CRpcResolver::_pNoRegAccessRestrictionsSD L1` → `db <ptr> L0xe4` (넷째 228 B), `dq combase!CRpcResolver::_pDefaultAccessRestrictionsSD L1` (NULL 인지 · 228 B 인지). 닫는 것: 1-1 계산 바이트 · 다섯째가 있나.
2. **D8 (`rpcss!_Connect+0x5ce` 루프 앞)** — 크기 칸 다섯 · `GetDefaultAccessRestrictions` 반환. 닫는 것: `{124,124,232,228,?}` · 708 / 936.
3. (선택) `combase!LookupPrincName+0x116` 반환 · `CoInitializeSecurity+0x2b4` 뒤 `eax` — 진짜 VM 에선 `LookupAccountSidLocalW` 가 성공하나.
4. (선택) `combase!CComApartment::CallTheResolver+0x9d` — 첫 부름이 6 쪽인가 (위 `[추측]` 닫기).

디버거 없이 VM 으로 떠 올 것 (결과서 — 수집 스크립트 재료):

- `reg export HKLM\SOFTWARE\Microsoft\OLE` (값 이름 · 종류 · **바이트** — `MachineAccessRestriction` · `DefaultLaunchPermission` · `MachineLaunchRestriction` 이 NoReg 와 같은지, `DefaultAccessPermission` 이 정말 없는지, `…Old` 흔적, `AppCompat` 하위 키).
- `reg query "HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM" /s` (없으면 등록값 쪽이 쓰임).
- `reg query` AppID `test.exe` (HKLM · HKCU `SOFTWARE\Classes\AppID`) — "AppID 없음" 이 진짜에서도 같은지.
- `wmic os get producttype` (1 = 워크스테이션 → AN `0x3`) · `whoami /user`.

정적 (결과서 "다음 요청 후보" — 대기열 자리는 오케스트레이터가 정함):

1. rpcrt4 `ncalrpc` 연결 재시도 (lsapolicylookup "8 번") + `PickAssociation` — Q284b ⑤ 와 겹침.
2. rpcss `ISCMLocalActivator` 서버 (활성화 "클래스 없음" HRESULT) — Q284b 와 합칠 만함.
3. `GetLegacyBlanket` AppID 있는 길 · `CallAccessCheck` (rva `0x7fcc4`) — 명시 길 `CalculateAnonymous…` 의 syscall.
4. `MakeProxyHelper` (rva `0x94284`) — 어떤 DSA 면 프록시가 서나.
5. Q284b 나머지 (프로시저 10 · 1 몸 · `CoGetObject`) 그대로.
- 그 밖 `[공백]`: 암묵 길 포트 실패의 정확한 HRESULT · `OXIDEntry` 생성자 (6 먼저의 `[추측]`) · `LocalInterfaceOnlySecCallback` 의 RCX.

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에선 못 봤다 `[정적: 읽은 줄 안에서]`. 다만 `CoInitializeSecurity` 의 실패 갈래들은 `BreakOnCoInitializeSecurityRuntimeFailureIfRequested` 같은 "중단 요청" 훅을 지난다 (결과서 3절 7: lsapolicylookup 실패 갈래에선 안 부름) — D5 를 걸 때 이 훅이 켜져 있으면 멈춤이 하나 더 생길 수 있다 `[추측]`.
- **읽을 때 조심** (하네스 제안 거리 — 아래 보고 참고): 결과서가 함수 안 줄 (`+0x…`) 과 **주소 끝자리** (`+0xeb8e` = `0x18007eb8e`) 를 같은 꼴로 섞어 썼다. 함수가 `0x18007e880` 처럼 `0x…e…` 근처에서 시작하면 둘이 비슷해 보여 헷갈린다. 점프 목적지는 "목적지 − 함수 시작" 으로 다시 빼 보는 게 안전하다.
- rpcss `.asm` 의 `?gStartupSids@@3PAU_STARTUP_SID@@A+0xc8` 주석은 표 칸이 아니라 바로 뒤 이름 없는 전역 (`0x1644b8`) 이다 (결과서 "스크립트 문제" — sast/40 의 "전역 주석 주의" 와 같은 모양). flow-writer 도 `InitializeSystemSecurityDescriptors +0x6a` 에서 같은 주석을 봤다.
- 스크립트 문제 없음 (결과서). `pefile` 은 시스템 파이썬 사용자 사이트에 있어 `python3 -I` 로는 안 불린다 — flow-writer 는 스크래치 스크립트를 `-I` 없이 돌렸다 (MS 바이너리만 읽음).
