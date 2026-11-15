# 2026-11-15 run98 — 부팅 때 smss 가 `\RPC Control` · `\Windows` 를 만드는 법과 처음 SD (`SmpInit` → `SmpCreateSecurityDescriptors(1)` · `SmpLoadDataFromRegistry` → `RtlQueryRegistryValuesEx` ⇢ 표 콜백 `SmpConfigureProtectionMode` · `SmpConfigureObjectDirectories` → `NtCreateDirectoryObject`) (Q180b = Q180 ②, PE_emulator 요청 34 의 2-가 · 2-나 — 정적만): **`\RPC Control` 은 smss 가 레지스트리 `Session Manager\ObjectDirectories` 이름마다 부르는 표 콜백 `SmpConfigureObjectDirectories +0xf1 NtCreateDirectoryObject` 가 만든다 (속성 `0xd0` = 영구 · 대소문자 무시 · 있으면 열기, 접근 `0xf000f`). 이름이 `\RPC Control` 이나 `\Windows` 면 (대소문자 무시 비교 `+0x90` · `+0xab`) `SmpLiberalSecurityDescriptor`, 그 밖이면 `SmpPrimarySecurityDescriptor` 를 넘긴다 — 두 이름은 같은 SD 한 벌을 같이 쓴다. Liberal SD = 소유자 · 그룹 없음 (smss 가 안 채움), DACL 허용 ACE 7개 (`S-1-1-0` · `S-1-5-12` 에 `0xE0000000`, `S-1-5-32-544` 에 `0x10000000` 이 이 디렉터리 몫 셋, 같은 셋 + `S-1-3-0` 이 상속 전용 `0x0b` 넷 — `S-1-5-18` 없음), SACL 라벨 ACE 1개 (형 `0x11` · 마스크 `1` NO_WRITE_UP · `S-1-16-4096` 낮은 무결성). `ProtectionMode` 가 0 이어도 SD 는 같다 — `SmpProtectionMode` 파일 값이 이미 1 이고 `SmpInit +0x2f4` 가 먼저 만들어 두며, 콜백의 둘째 부름은 비트 0 이 꺼지면 아무것도 안 하고 돌아온다 (`+0x1ff ~ +0x20c`). smss 는 이 디렉터리에 `NtSetSecurityObject` 를 안 부르고, 저장소에서 보이는 범위에선 `\RPC Control` 을 뒤에 고치는 곳이 없다. `\Windows` 는 csrss (`csrsrv!CsrParseServerCommandLine +0x210 CsrSetDirectorySecurity`) 가 DACL 을 바꾼다. 커널은 `\RPC Control` 을 안 만든다 (이름 비교 · ALPC 붙기로만 씀).** 같은 함수가 `\KnownDlls` 디렉터리 SD (test.exe 가 여는 그 객체) 도 만든다.

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### Q180b 결론 (run98)

- **지금 우리 (`\RPC Control` 객체에 SD 를 안 채움) 는 틀렸다** `[정적]` — 진짜 smss 는 아래 "Liberal SD" 를 넘겨 만든다. 에뮬레이터 객체에 이 SD 를 넣어 두면 `NtQuerySecurityObject(0x17)` 에 답할 재료가 생긴다 (바이트 꼴은 run97 "에뮬레이터용 규칙 한 장").
- **라벨이 있다** `[정적]` — SACL 에 형 `0x11` ACE 하나. 그래서 run97 의 "LABEL 만 물을 때 라벨 ACE 가 없으면" 세 경우 (a) · (b) · (c) 가 아니라, **라벨 ACE 1개짜리 SACL 이 대답에 실린다** (smss 가 넘긴 그대로 커널이 저장한다면 — 그건 Q180d · Q181).
- **처음 SD 만 맞추면 된다** `[정적, 보이는 범위]` — `\RPC Control` 을 만든 뒤 SD 를 고치는 줄을 smss 안에서도, 저장소 모듈 · 문자열 · import 로 보이는 smss 밖에서도 못 찾았다. 확정은 Q181 (부팅 뒤 실제 SD).
- **커널이 미리 만들 필요 없다** `[정적]` — ntoskrnl 은 `\RPC Control` 을 만들지 않는다. 에뮬레이터가 smss 몫을 대신해 객체를 두면 된다.
- **커널이 저장한 꼴은 아직 모른다** — 소유자 · 그룹은 어느 SID 로 채워지나, `GENERIC_*` 비트가 Directory 구체 권한으로 바뀌나, `AclSize` (184 · 32) 가 줄어드나. → Q180d (`SeAssignSecurityEx2`, sast/96 → run100 몫) · Q181.

### `\RPC Control` 의 처음 SD (smss 가 넘기는 것, `ProtectionMode` 1) — 결과서 1-5 · 1-7 옮김, flow-writer 가 `.asm` 줄 다시 봄

| 구성요소 | 값 | 근거 |
|---|---|---|
| Revision | 1 | `SmpCreateSecurityDescriptors +0xe0 ~ +0xf1` (`RtlCreateSecurityDescriptor(&몸통, 1)`) |
| Control (smss 쪽 절대형) | `0x0014` (`DACL_PRESENT` · `SACL_PRESENT`). `DEFAULTED` · `PROTECTED` · `AUTO_INHERITED` 없음 | `+0xe84 RtlSetDaclSecurityDescriptor` · `+0xea0 RtlSetSaclSecurityDescriptor` (둘 다 `DL=1`, `R9D=0`) |
| 소유자 | **없음 (NULL)** — smss 가 이 SD 에 `RtlSetOwnerSecurityDescriptor` 를 안 부름 | smss 안 유일한 호출은 `SmpSetDumpSecurityAndAttributes +0x1d7` (덤프 파일) |
| 그룹 | **없음 (NULL)** — `RtlSetGroupSecurityDescriptor` 는 import 도 안 함 | 결과서 (import 표) |
| DACL | `AclRevision 2` · **`AclSize 0xb8` (184)** · **`AceCount 7`** — 쓰인 156B, 뒤 28B 안 씀 | 크기 `+0xc27 ~ +0xc82`, 할당 `+0xc89`, `+0xd00 RtlCreateAcl` |
| SACL | `AclRevision 2` · **`AclSize 0x20` (32)** · **`AceCount 1`** — 쓰인 28B | `+0xcc2 LEA EBX,[RAX+0x14]`, `+0xe46 RtlCreateAcl` |

**DACL ACE (순서 그대로, 전부 허용 = 형 0)**

| # | 플래그 | 마스크 | SID | 크기 | 넣는 줄 (call) |
|---|---|---|---|---|---|
| 0 | `0x00` | `0xE0000000` (GENERIC_READ · WRITE · EXECUTE) | `S-1-1-0` | 20 | `+0xd1e` (마스크 `+0xd10 MOV R15D,0xe0000000`) |
| 1 | `0x00` | `0xE0000000` | `S-1-5-12` | 20 | `+0xd36` |
| 2 | `0x00` | `0x10000000` (GENERIC_ALL) | `S-1-5-32-544` | 24 | `+0xd54` |
| 3 | **`0x0b`** (OI · CI · INHERIT_ONLY) | `0xE0000000` | `S-1-1-0` | 20 | `+0xd6f`, 플래그 `+0xda0 MOV byte [RAX+1],0xb` |
| 4 | `0x0b` | `0xE0000000` | `S-1-5-12` | 20 | `+0xda8`, 플래그 `+0xdd6` |
| 5 | `0x0b` | `0x10000000` | `S-1-5-32-544` | 24 | `+0xdde`, 플래그 `+0xe0c` |
| 6 | `0x0b` | `0x10000000` | `S-1-3-0` | 20 | `+0xe14`, 플래그 `+0xe42` |

**SACL ACE**

| # | 형 | 플래그 | 마스크 | SID | 크기 | 줄 |
|---|---|---|---|---|---|---|
| 0 | **`0x11`** (SYSTEM_MANDATORY_LABEL) | `0x00` (`+0xe56 XOR R8D,R8D`) | `0x00000001` NO_WRITE_UP (`+0xe5b`) | `S-1-16-4096` (`+0xe52 R9=[RBP-0x31]`) | 20 | `+0xe68 RtlAddMandatoryAce` (`+0xe63` 형 `0x11`) |

- 바이트 (리틀엔디안, 결과서 1-7):
  - SID — `S-1-1-0` `01 01 00 00 00 00 00 01 00 00 00 00` · `S-1-5-12` `01 01 00 00 00 00 00 05 0c 00 00 00` · `S-1-5-32-544` `01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00` · `S-1-3-0` `01 01 00 00 00 00 00 03 00 00 00 00` · `S-1-16-4096` `01 01 00 00 00 00 00 10 00 10 00 00`
  - DACL 머리 `02 00 b8 00 07 00 00 00`, ACE 머리 `00 00 14 00` / `00 00 18 00` / `00 0b 14 00` / `00 0b 18 00` + 마스크 4B + SID
  - SACL 머리 `02 00 20 00 01 00 00 00`, ACE `11 00 14 00  01 00 00 00` + `S-1-16-4096`
- smss 가 넘기는 건 **절대형** SD (`0x28`B, Owner · Group NULL, Dacl · Sacl 포인터). self-relative 로 붙이는 차례 · 맞춤 · 커널 쪽 변환은 커널 몫 → Q180d · Q181.
- `S-1-5-18` (SYSTEM) 은 이 DACL 에 없다. SYSTEM 이 `S-1-5-32-544` 멤버라서 그 ACE 로 닿을 것이다 `[추측: 결과서]`.

### 지금 PE_emulator 맞나 · 틀리나 (결과서 "지금 우리" 줄 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

| 질문 | 진짜 (26100.9457) `[정적]` | 지금 우리 | 판정 |
|---|---|---|---|
| 2-가 만드는 곳 · SD | smss 표 콜백 → Liberal SD (DACL 7 + 라벨 SACL 1, 소유자 · 그룹 없음) | `\RPC Control` 에 SD 안 채움 | **틀림** — 위 표를 객체 SD 로 |
| 2-나 뒤에 고치나 | smss 안 없음, smss 밖 보이는 범위에서 못 찾음 (`\Windows` 만 csrss 가 고침) | 처음 SD 도 없음 | 처음 SD 만 맞추면 충분해 보임 — Q181 로 확정 |
| 3 커널이 미리 만드나 | 아니오 | 커널 쪽에서 안 만듦 | **맞음** |

### 원문 질문이 어디서 답해졌나

| 요청서 질문 | 답한 곳 (아래 절) | 한 줄 |
|---|---|---|
| 1 (2-가) 만드는 함수 · SD | "어디서 읽고 어디서 부르나" · "SD 를 만드는 함수" · 위 "처음 SD" 표 | `SmpConfigureObjectDirectories +0xf1`, 이름 비교 → Liberal, DACL 7 + 라벨 1, 모드 0 이어도 같음 |
| 2 (2-나) 만든 뒤 고치는 곳 | "만든 뒤 고치는 곳" | smss 안 없음 · smss 밖 못 찾음 · `\Windows` 는 csrss |
| 3 커널이 미리 만드나 | "커널은 `\RPC Control` 을 만드나" | 아니오 — 이름 비교 표 · ALPC 붙기만 |

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 사진 대조 없음 — 부팅 때 smss 의 일이라 run44 사진 구간 (K1 ~ P2) 앞이고, 객체 SD 는 사진에 칸이 없다. 사진도 다시 안 읽었다.
- 정적: **smss pdb `352F20C3490D7BF3449E0A0AE67D4E0F1`** (GUID+Age, `ms/sym/smss.exe.pdb`). **이번에 처음 받고 처음 analyze** (함수 325, `FUN_` 0, 별칭 4곳 이름 8 — 이 문서에 나오는 이름과 안 겹침). 바이너리 `System32\smss.exe` x64, 228,824B, 베이스 `0x140000000`.
  - 버전 대조: smss 는 "알려진 사실" 표에 없어 간접으로 (결과서) — 파일 크기 = 요청서 값, 문자열 `ObjectDirectories` (rva `0x29060`) · `ProtectionMode` (`0x29040`) · 기본값 `\Windows\0\RPC Control\0\0` (`0x29088`) 가 요청서 실마리와 같음, PE `FF 15` 스캔의 `NtCreateDirectoryObject` 3곳 · `NtSetSecurityObject` 3곳이 `_calls.tsv` 와 하나도 안 어긋남.
- csrsrv: 다시 analyze 안 함. `CsrSetDirectorySecurity` 는 `_index.tsv` 의 **이름 · rva (`0x7570`, 859B) 만** — 본문 export 안 함 (결과서가 PE 바이트로 본 `+0x4c` · `+0x2b3` · `+0x2b8` · `+0x2bb` 줄은 결과서 몫). 있던 `CsrParseServerCommandLine.c` · `.asm` 은 flow-writer 가 다시 읽음.
- ntoskrnl: Ghidra 프로젝트 · export 안 건드림 (sast/93 이 같은 시간에 씀). `_index.tsv` · `_calls.tsv` · 있던 `.c` + PE 바이트 (`objdump`, 파이썬 스캔) 만. 그래서 ntoskrnl 쪽 줄 (`SepValidateReferencedCachedHandles +0x27e` 등) 은 **결과서 값** 이고 `.asm` 파일이 없다.
- 에뮬레이터 쪽 숫자 (`0x17` · 383 샘플 · 레지스트리 덤프 값, 요청서 배경) 는 **PE_emulator 쪽 관찰**이라 이 문서의 `[관찰]` 이 아니다.
- 이 길은 **test.exe 초기화 지도 밖**이다 (부팅 때 다른 프로세스). 다만 같은 함수가 만드는 `\KnownDlls` 디렉터리는 test.exe 가 연다 (아래 "test.exe 길에 있나").

## 목적

- PE_emulator 요청 34 의 2-가 · 2-나 (대기열 Q180 을 나눈 **Q180b**, 사용자 결정 2026-11-15). `\RPC Control` 디렉터리는 **누가 · 어떤 SD 로** 만들고, **만든 뒤 누가 고치나**. 그리고 커널이 미리 만드나 (작게).
- 범위 밖: 커널이 넘긴 SD 를 객체에 붙이는 계산 (`SeAssignSecurityEx2`, Q180d · sast/96 → run100), rpcrt4 가 그 SD 에서 무엇을 보나 (Q180c · sast/95 → run99), 커널 묻기 길 (Q180a · run97).

## 방법

- 정적: `_workspace/sast/94_rpc-dir-create.md` — smss (새 모듈)
  - `fetch_pdb.py smss.exe` → `analyze` 1번 (15초) → 지도 `run_ghidra.sh map` 뿌리 `wmain` (152 함수, `ms/disassemble/smss/_map/wmain_*`).
  - export depth 0: 요청서 대상 `SmpConfigureObjectDirectories` · `SmpConfigureProtectionMode` · `SmpCreateSecurityDescriptors` · `SmpCreateProtectedPrefixes`, 길 위라 덧붙임 `SmpLoadDataFromRegistry` · `SmpInit` · `SmpOpenKnownDllsHandles` · `SmpInitializeDosDevices` · `SmpSetDumpSecurityAndAttributes` · `SmpInitializePendingRename` (본문 안 읽음).
  - `refs` 6번 (예: `SmpProtectionMode` 쓰기 한 곳, Liberal 포인터 읽기 · 쓰기).
  - 표 `SmpRegistryConfigurationTable` (rva `0x300b0`) · 문자열 · import 호출 자리는 PE 바이트를 직접 읽음 (결과서).
- 결과서: `_workspace/sast/94_rpc-dir-create_result.md` (**분석성공** — 못 닫은 것: 커널이 저장한 꼴 (소유자 · 그룹 · 마스크 변환 · `AclSize`) = 동적 / Q180d, smss 밖 후보 모듈 (`AppXDeploymentServer` · `usermgr`) 이 디렉터리 SD 를 고치는지).
- flow-writer 가 다시 본 것 (`ms/disassemble/smss/*.asm`, `_calls.tsv`, `ms/decomp/smss/*.c`, PE 바이트 `objdump -s`):
  - `SmpConfigureObjectDirectories` 전부 (`+0x0 ~ +0x15a`).
  - `SmpConfigureProtectionMode` 전부 (`+0x0 ~ +0x25`).
  - `SmpCreateSecurityDescriptors +0x9f` · `+0xa1` · `+0xea` · `+0xfd` · `+0x1ff ~ +0x214` · `+0x504` · `+0x506` · `+0x8cc` · `+0x8d3` · `+0xc11 ~ +0xd1e` · `+0xd36` · `+0xd54` · `+0xd6f` · `+0xda0` · `+0xda8` · `+0xdd6` · `+0xdde` · `+0xe0c` · `+0xe14` · `+0xe42 ~ +0xea7`.
  - `SmpInit +0x2f2` · `+0x2f4` · `+0x33a` · `+0x38c` · `+0x534`, `SmpLoadDataFromRegistry +0x54a` · `+0x55c` · `+0x5af ~ +0x5df` · `+0x6a4` · `+0x6e0` · `+0x708` · `+0x739` · `+0xbb5` · `+0xbc5`, `SmpOpenKnownDllsHandles +0x161` · `+0x18f` · `+0x1e1` · `+0x1fc` (+ `.c`).
  - `_calls.tsv` 의 `NtSetSecurityObject` 3줄, `_map/wmain_edges.tsv` 의 `wmain +0x556 SmpInit`.
  - PE 바이트: `.data` rva `0x31ce4` = `01 00 00 00` (`SmpProtectionMode` 파일 값), `0x29088` 기본값 글, `0x30e90` (`SmpKnownDllsObjectDirectoryName` = `{0x14, 0x16, → 0x28f70}`) · `0x28f70` = `\KnownDlls`, `0x30e80` (`SmpKnownDllsPathSuffix` → `\System32`).
  - csrsrv `CsrParseServerCommandLine.c` 80 ~ 112줄 · `.asm` `+0x15b` · `+0x167` · `+0x1f9` · `+0x210`.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/smss/_calls.tsv` · `.asm` · `_map/wmain_edges.tsv`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `smss!wmain` | `+0x556` | `+0x55b` | `SmpInit` | 지도 깊이 1 | [정적] |
| `smss!SmpInit` | **`+0x2f4`** | **`+0x2f9`** | **`SmpCreateSecurityDescriptors`** | `CL = 1` (`+0x2f2 MOV CL,1`) — SD 6벌 첫 만들기 | [정적] |
| 〃 | `+0x38c` | `+0x393` | `NtAlpcCreatePort` | SD = `SmpApiPortSecurityDescriptor` (`+0x33a`) — smss API 포트 | [정적] |
| 〃 | **`+0x534`** | **`+0x539`** | **`SmpLoadDataFromRegistry`** | | [정적] |
| `smss!SmpLoadDataFromRegistry` | **`+0x55c`** | **`+0x563`** | **`RtlQueryRegistryValuesEx`** | `(2 = RTL_REGISTRY_CONTROL, "Session Manager", SmpRegistryConfigurationTable)` (`+0x54a LEA R8`). 표 칸마다 콜백을 부른다 | [정적] |
| (표 0번 칸 콜백) `SmpConfigureProtectionMode` | `+0x1a` | `+0x1f` | `SmpCreateSecurityDescriptors` | `CL = 0` (`+0x12 XOR ECX,ECX`). 그 앞 `+0x14` 가 `SmpProtectionMode` 에 값을 씀 (길이 ≠ 4 면 1) | [정적] |
| (표 1번 칸 콜백) **`SmpConfigureObjectDirectories`** | **`+0xf1`** | **`+0xf8`** | **`NtCreateDirectoryObject`** | 이름마다 한 번. 속성 `0xd0` · 접근 `0xf000f` · SD = Liberal 또는 Primary | [정적] |
| 〃 | `+0x90` · `+0xab` | `+0x97` · `+0xb2` | `RtlEqualUnicodeString` | 이름 vs `\RPC Control` · `\Windows` (셋째 인자 1 = 대소문자 무시) | [정적] |
| 〃 | `+0x11f` | `+0x126` | `NtClose` | 성공이면 핸들만 닫음 (영구 객체) | [정적] |
| 〃 | `+0x114` | `+0x119` | `SmpLogFailureString` | 실패면 기록만, 다음 이름 계속 (`0x1cee`) | [정적] |
| `smss!SmpLoadDataFromRegistry` | `+0x5df` | `+0x5e4` | `SmpOpenKnownDllsHandles` | 이름 `SmpKnownDllsObjectDirectoryName` = `\KnownDlls` (`+0x5d8`) — `+0x55c` 보다 뒤 | [정적] |
| `smss!SmpOpenKnownDllsHandles` | `+0x18f` | `+0x196` | `NtCreateDirectoryObject` | SD = `SmpKnownDllsDirSecurityDescriptor` (`+0x161`), 속성 `0xd0` | [정적] |
| 〃 | `+0x1fc` | `+0x203` | `NtCreateSymbolicLinkObject` | `KnownDllPath` 링크, SD = `SmpKnownDllsSymLinkSecurityDescriptor` (`+0x1e1`) | [정적] |
| `smss!SmpLoadDataFromRegistry` | `+0x708` | `+0x70f` | `NtCreateDirectoryObject` | `\Sessions` (결과서), SD = Primary (`+0x6e0`) | [정적] |
| 〃 | `+0x6a4` · `+0x739` | `+0x6a9` · `+0x73e` | `SmpInitializeDosDevices` · `SmpCreateProtectedPrefixes` | Primary SD 의 다른 쓰임 · `NtSetSecurityObject` 둘 (파이프 · 메일슬롯) | [정적] |
| `smss!SmpCreateProtectedPrefixes` | `+0xa44` · `+0xab3` | `+0xa4b` · `+0xaba` | `NtSetSecurityObject` | `\Device\NamedPipe\` · `\Device\mailslot\`, 정보 `0x1c` | [정적] |
| `smss!SmpSetDumpSecurityAndAttributes` | `+0x1ed` | `+0x1f4` | `NtSetSecurityObject` | 덤프 · 페이지 파일 | [정적] |
| `csrsrv!CsrParseServerCommandLine` | `+0x1f9` | `+0x200` | `NtCreateDirectoryObject` | 명령줄 `ObjectDirectory=` 이름. 속성 `0xc0`, 서비스 세션이면 `0xd0` | [정적] |
| 〃 | **`+0x210`** | **`+0x215`** | **`CsrSetDirectorySecurity`** (rva `0x7570`) | 그 안 `+0x2bb NtSetSecurityObject(CsrObjectDirectory, 4 = DACL, …)` (결과서, PE 바이트) | [정적] |

- `SmpConfigureObjectDirectories` · `SmpConfigureProtectionMode` 는 **아무도 직접 안 부른다** — 표 `SmpRegistryConfigurationTable` 의 함수 포인터로만 불려서 `_calls.tsv` · 지도 둘 다 caller 가 없다. 표 칸은 결과서가 PE 바이트로 읽었다 (칸 `0x38`B, 0번 `ProtectionMode` / `REG_DWORD`, 1번 `ObjectDirectories` / `REG_MULTI_SZ` · 기본값 rva `0x29088`, 끝은 26번 칸).
- `NtCreateDirectoryObjectEx` 는 smss 가 import 하지 않는다 (결과서). smss 안 `NtCreateDirectoryObject` 는 위 셋뿐.

### 어디서 읽고 어디서 부르나 (결과서 1-1 · 1-2)

- **표 하나, 칸 차례가 곧 시간 차례**: 0번 `ProtectionMode` (콜백 `SmpConfigureProtectionMode`) → 1번 `ObjectDirectories` (콜백 `SmpConfigureObjectDirectories`). 그래서 디렉터리를 만들 땐 SD 가 이미 (다시) 만들어져 있다.
- `ProtectionMode` 값이 레지스트리에 없을 때의 모양은 `[추측]` (결과서 — 기본형 `REG_DWORD` 가 길이 0 으로 올 것). 콜백은 **길이가 4 가 아니면 1** 로 둔다 (`+0x4 MOV EAX,1` · `+0x9 CMP R9D,4` · `+0xd JNZ`) — 어느 쪽이든 1.
- `ObjectDirectories` 값이 없으면 표 기본값 `\Windows` · `\RPC Control` 두 줄 (PE 바이트 `0x29088` — flow-writer 가 `objdump -s` 로 한 번 더 봄).
- **이름마다 한 루프** (`+0x6b ~ +0x13a`): `RtlInitUnicodeString` → SD = Primary (`+0x7e`) → `\RPC Control` 이면 (`+0x9e JNZ → +0xbb`) 또는 `\Windows` 면 (`+0xb9 JZ` 가 안 뛰면) **Liberal** (`+0xbb`) → `OBJECT_ATTRIBUTES` (Length `0x30` · Root 0 · 이름 · **Attributes `0xd0`** · **SD** · QoS 0, `+0xc6 ~ +0xed`) → `+0xf1 NtCreateDirectoryObject` (접근 `0xf000f`, `+0xd5`) → 성공이면 `NtClose`, 실패면 기록만 → 다음 이름. 함수는 늘 0 을 돌려준다 (`+0x148`).
- 비교용 글: `[RBP-0x50]` = `{0x18, 0x1a}` → rva `0x2a0f8` (`\RPC Control`), `[RBP-0x40]` = `{0x10, 0x12}` → `0x2a118` (`\Windows`) (`+0x1c` · `+0x30` · `+0x3e` · `+0x55`).
- `0xd0` = `OBJ_PERMANENT 0x10` + `OBJ_CASE_INSENSITIVE 0x40` + `OBJ_OPENIF 0x80` (비트 이름은 `[추측: 공개 헤더]`, 값은 코드). 이미 있으면 열기만 하고, 그때 새 SD 는 안 붙는다 `[추측]` — `\RPC Control` 이 smss 전에 있을 일은 정적으로 못 찾았다 (아래 "커널은 만드나").

### SD 를 만드는 함수 — `SmpCreateSecurityDescriptors` (rva `0xa1b0`, 4138B) (결과서 1-4 · 1-6)

| 부름 | 인자 | 하는 일 | 줄 |
|---|---|---|---|
| 1번째 `SmpInit +0x2f4` | `CL = 1` | SD 6벌의 빈 틀 (NULL DACL) — Primary · **Liberal (몸통 rva `0x32680`, 포인터 `0x32668`)** · KnownDllsDir · KnownDll · KnownDllsSymLink · ApiPort → SID 9개 → KnownDlls · ApiPort SD 채움 → `+0x8cc` `SmpProtectionMode & 1` 이면 Primary · Liberal 의 DACL (+ Liberal SACL) | `+0xa7 ~ +0x1f8` 틀, Liberal 틀 `+0xe0 ~ +0x114` (포인터 쓰기 `+0xea`) |
| 2번째 `SmpConfigureProtectionMode +0x1a` | `CL = 0` | `+0x9f TEST CL,CL` · `+0xa1 JZ → +0x1ff`. `+0x1ff ~ +0x20c`: 비트 0 이 0 이면 `XOR EAX,EAX` → 함수 끝 (**아무것도 안 바꿈**). 1 이면 SID 9개 다시 → `+0x504 TEST BL,BL` · `+0x506 JZ → +0x8cc` → **Primary · Liberal 만 다시** | |

- **`ProtectionMode` 0 이어도 SD 는 같다** `[정적]`: `SmpProtectionMode` 파일 값 `01 00 00 00` (`.data` rva `0x31ce4` — flow-writer 가 `objdump -s` 로 다시 봄), 이 칸을 쓰는 곳은 `SmpConfigureProtectionMode +0x14` 하나 (`refs`). 1번째 부름이 이미 모드 1 로 만들었고, 레지스트리가 0 이면 2번째 부름은 바로 돌아온다. → "NULL DACL 만 남는 모드" 는 이 빌드 코드엔 없다.
- **SID 는 `RtlAllocateAndInitializeSid` 로 만든다** (상수 블롭 아님). 9개: `S-1-1-0` · `S-1-15-2-1` · `S-1-15-2-2` · `S-1-5-32-544` · `S-1-3-0` · `S-1-5-12` · `S-1-5-18` · `S-1-16-4096` · `S-1-19-512-8192` (만든 줄 `+0x254 ~ +0x4e3`, 결과서 1-4 표). 이름 (Everyone · 관리자 등) 은 `[추측: 공개 문서]`, 값은 코드.
- Liberal DACL · SACL 의 ACE 줄은 위 "PE_emulator 쪽" 표.
- **Primary SD** (그 밖 이름 · `\Sessions` · DosDevices · 파이프 접두어): 소유자 · 그룹 없음, Control `0x0004`, **SACL 없음**. DACL 7개, 순서 · 플래그는 Liberal 과 같고 `S-1-1-0` · `S-1-5-12` 마스크만 **`0xA0000000`** (쓰기 없음). 줄 `+0xade ~ +0xc11`, 붙임 `+0xc15` (결과서).

### 같은 함수가 만드는 다른 SD (결과서 1-8, 짐 지도 후보)

| SD (PDB 이름) | 쓰는 자리 | DACL (차례) | SACL |
|---|---|---|---|
| `SmpKnownDllsDirSecurityDescriptor` | `SmpOpenKnownDllsHandles +0x161` → `+0x18f NtCreateDirectoryObject` (**`\KnownDlls`**) | `S-1-5-32-544` `0x10000000` · `S-1-1-0` `0xA0000000` · `S-1-15-2-1` `0xA0000000` · `S-1-5-12` `0xA0000000` · `S-1-15-2-2` `0xA0000000` (AclSize 140) | 프로세스 신뢰 라벨 (형 `0x14`, 마스크 `0x20003`, `S-1-19-512-8192`) |
| `SmpKnownDllsSymLinkSecurityDescriptor` | `+0x1e1` → `+0x1fc NtCreateSymbolicLinkObject` (`KnownDllPath`) | 위와 **같은 ACL 객체** | 없음 |
| `SmpKnownDllSecurityDescriptor` | `SmpInitializeKnownDllsInternal +0xf4` (KnownDll 섹션들) | `S-1-1-0` · `S-1-15-2-1` · `S-1-15-2-2` · `S-1-5-12` 넷 `0xE0000000` · `S-1-5-32-544` `0x10000000` | 라벨 `0x11` 마스크 1 `S-1-16-4096` + 신뢰 라벨 `0x14` `S-1-19-512-8192` (마스크 `0x3000f` / `0x2000f`, `SmpHostSmss` 에 따라) |
| `SmpApiPortSecurityDescriptor` | `SmpInit +0x33a` → `+0x38c NtAlpcCreatePort` | `S-1-5-32-544` · `S-1-5-18` 둘 `0x10000000` | 없음 |

- 모두 `SmpCreateSecurityDescriptors(1)` 한 번에 만들고, 소유자 · 그룹 없음, ACE 형 허용 (플래그 0). 줄은 결과서 1-8.

### 만든 뒤 고치는 곳 (결과서 질문 2)

- **smss 안: `\RPC Control` 에는 없다** `[정적]` — `NtSetSecurityObject` 는 3곳뿐 (위 호출 표: 파이프 · 메일슬롯 장치 루트, 덤프 · 페이지 파일). 객체 디렉터리를 핸들로 여는 곳은 `SmpInitializeDosDevices +0x76` (읽기 열기) 하나이고 그 뒤에 `NtSetSecurityObject` 가 없다. Liberal 포인터 (`0x32668`) 쓰기는 `+0xea` 하나, 읽기는 `+0xfd` · `+0xe74` · `+0xe90` · `SmpConfigureObjectDirectories +0xbb` (`refs`).
- **smss 밖 (저장소 모듈 + System32 문자열 · import 로 보이는 만큼)**:
  - `RPC Control` 글이 든 이진 17개 중 분석이 있는 건 `ntoskrnl` · `kernelbase` · `gdi32full` 뿐. kernelbase 의 것은 AppContainer · 그림자 이름공간 쪽 `RPC Control` (`\Sessions\N\…\RPC Control`, `BasepOpenShadowRpcControlDirectory` · `BasepCreateAppContainerObjectDirectories +0x590`), gdi32full 은 ALPC 포트 이름 (`\RPC Control\UmpdProxy`) — **둘 다 이 디렉터리 SD 를 고치지 않음**.
  - **`csrsrv` 는 `\Windows` 를 고친다**: `CsrParseServerCommandLine +0x1f9 NtCreateDirectoryObject` (명령줄 `ObjectDirectory=`) → `+0x210 CsrSetDirectorySecurity` → (그 안 `+0x2bb`) `NtSetSecurityObject(CsrObjectDirectory, 4 = DACL 만)`. csrsrv 에 `RPC Control` 글은 없다.
  - `basesrv` 의 `NtCreateDirectoryObject` 4곳 (`ServerDllInitialization +0xa4f` 등) 은 BaseNamedObjects 류, `NtSetSecurityObject` import 없음.
  - 분석 없는 후보 중 `NtSetSecurityObject` · `SetSecurityInfo` 를 import 하는 것: `AppXDeploymentServer.dll` · `usermgr.dll` (지연) · `win32kfull.sys` (`ZwSetSecurityObject`). 이들이 디렉터리 SD 를 건드리는지는 **분석 안 함** — 부팅 첫 순간이 아니라 서비스 · 로그온 뒤일 것 `[추측: 결과서]`.
- 그래서 위 Liberal 표는 **`\RPC Control` 의 부팅 직후 값 (smss 가 넘긴 것)** 이고, **`\Windows` 는 csrss 뒤 값이 다르다**.

### 커널은 `\RPC Control` 을 만드나 (결과서 질문 3) — **아니오** `[정적, 보이는 범위]`

- `ntoskrnl.exe` 의 `RPC Control` 글은 2곳 (UTF-16 바이트 검색, 결과서):
  1. 이름 비교 표 (rva `0x1c90 ~ 0x1cd0`, `UNICODE_STRING` 5칸 — `Local` · `Global` · `RPC Control` · `Session` · `AppContainerNamedObjects`) — `SepValidateReferencedCachedHandles` (rva `0x8f48c0`) `+0x27e LEA` → `+0x296 RtlEqualUnicodeString`. 디렉터리를 만드는 게 아니라 AppContainer 토큰의 캐시 핸들 이름 검사 `[추측: 함수 이름 · 모양]`.
  2. `\RPC Control\FconAlpcPort` — `CmFcpConnectToAlpcServer` (rva `0xa9a398`) `+0x42 LEA`. 커널이 **이미 있는** `\RPC Control` 안 ALPC 포트에 붙는 쪽.
- 커널이 부팅 때 디렉터리를 만드는 곳 (`_calls.tsv`): `ObInitSystem +0x557` (루트) · `SepInitializationPhase1 +0x1b8` · `ExInitializeNls +0x28` — 이름에 `RPC Control` 없음.
- 이 두 줄은 PE 바이트 · `objdump` 로만 본 결과서 값이다 (ntoskrnl `.asm` 에 `RPC Control` 0 건, 이번에 새 export 없음).

### test.exe 길에 있나

- **smss 의 일은 test.exe 초기화 지도 밖이다** — 부팅 때 다른 프로세스 (smss) 가 한 번 한다. test.exe 는 rpcrt4 를 안 올린다 (run44 모듈 6개 `[관찰]`). `\RPC Control` 을 다루는 kernelbase 함수 `BasepOpenShadowRpcControlDirectory` 는 지도상 부모 쪽 `CreateProcessInternalW` (AppContainer · LowBox 갈래 — run97) · `UnhandledExceptionFilter` 지도에만 보이고, ntdll `_map/LdrInitializeThunk_*` 에는 없다 (flow-writer 검색). `\Windows` 를 직접 여는 줄은 flow.md 2장 test.exe 길에 없다.
- **연결점 하나 — `\KnownDlls`**: test.exe 의 `LdrpInitializeProcess +0xdb8 NtOpenDirectoryObject(&LdrpKnownDllDirectoryHandle, 3, "\KnownDlls")` (ret `+0xdbd`, run65 `[정적]`) 가 여는 디렉터리 객체는 **부팅 때 `SmpOpenKnownDllsHandles +0x18f` 가 `SmpKnownDllsDirSecurityDescriptor` 를 붙여 만든 것**이다 `[정적]` (이름 글 `\KnownDlls` = PE 바이트 `0x28f70`, `+0x5d8` 첫 인자 — flow-writer 가 확인). run44 핸들 `0x4c` = Directory `KnownDlls` `[관찰]` (짐 지도 7.4). 접근 3 이 이 DACL (`S-1-1-0` `0xA0000000`) 과 신뢰 라벨 (`0x20003`) 로 통과하는 까닭은 Directory 형 일반 매핑 · 라벨 검사 몫이라 `[추측]`.
- 그래서 **흐름도 그림은 안 고친다.** 줄은 flow.md 3장 새 절과 2.4 run65 절의 (run98) 한 줄에만.

### 호출 관계

```
smss!wmain
 └─ +0x556  SmpInit (0x155ac)                                                          [정적]
      ├─ (1) +0x2f4  SmpCreateSecurityDescriptors (0xa1b0) (CL=1)
      │        +0xa7 ~ +0x1f8   SD 6벌 빈 틀 (Primary · Liberal · KnownDllsDir · KnownDll · KnownDllsSymLink · ApiPort)
      │        +0x254 ~ +0x4e3  SID 9개 (RtlAllocateAndInitializeSid)
      │        KnownDlls · ApiPort DACL · SACL
      │        +0x8cc  SmpProtectionMode & 1 (파일 값 1) →
      │                 Primary DACL 7 (+0xade ~ +0xc15)
      │                 Liberal DACL 7 (+0xd00 ~ +0xe42) · SACL 라벨 1 (+0xe46 · +0xe68) → +0xe84 / +0xea0 붙임
      ├─ +0x38c  NtAlpcCreatePort (SD = ApiPort)
      └─ (2) +0x534  SmpLoadDataFromRegistry (0x162a4)
               ├─ +0x55c  RtlQueryRegistryValuesEx(CONTROL, "Session Manager", SmpRegistryConfigurationTable)
               │     ⇢ 표 0번  SmpConfigureProtectionMode (0x145d0)
               │     │      +0x14  SmpProtectionMode = 값 (길이 ≠ 4 면 1)
               │     │      └─ +0x1a  SmpCreateSecurityDescriptors (CL=0)
               │     │               +0x1ff ~ +0x20c  비트 0 = 0 → 그냥 돌아옴
               │     │               아니면 SID 다시 → +0x506 → +0x8cc Primary · Liberal 다시
               │     ⇢ 표 1번  SmpConfigureObjectDirectories (0x143d0)        이름마다:
               │            +0x7e   SD = Primary
               │            +0x90   == "\RPC Control" ?  /  +0xab  == "\Windows" ?  → +0xbb SD = Liberal
               │            └─ +0xf1  NtCreateDirectoryObject(&h, 0xf000f, {이름, 0xd0, SD})  → +0x11f NtClose
               ├─ +0x5df  SmpOpenKnownDllsHandles ("\KnownDlls")
               │        ├─ +0x18f  NtCreateDirectoryObject (SD = KnownDllsDir, 0xd0)
               │        └─ +0x1fc  NtCreateSymbolicLinkObject ("KnownDllPath", SD = KnownDllsSymLink)
               ├─ +0x6a4  SmpInitializeDosDevices (Primary)
               ├─ +0x708  NtCreateDirectoryObject ("\Sessions", Primary)
               └─ +0x739  SmpCreateProtectedPrefixes → +0xa44 / +0xab3 NtSetSecurityObject (파이프 · 메일슬롯, 0x1c)

뒤에 (다른 프로세스):
csrsrv!CsrParseServerCommandLine
 ├─ +0x1f9  NtCreateDirectoryObject ("ObjectDirectory=" 이름, 0xc0 / 서비스 세션 0xd0)
 └─ +0x210  CsrSetDirectorySecurity (0x7570) → +0x2bb NtSetSecurityObject(CsrObjectDirectory, DACL)     ← \Windows

test.exe (초기화, run65):
ntdll!LdrpInitializeProcess +0xdb8  NtOpenDirectoryObject(…, 3, "\KnownDlls")   ← 위 +0x18f 가 만든 객체
```

## 확인된 것

- `[정적]` `\RPC Control` 은 smss `SmpConfigureObjectDirectories +0xf1 NtCreateDirectoryObject` 가 만든다 — 레지스트리 `Session Manager\ObjectDirectories` 이름마다, 표 콜백으로 (`SmpLoadDataFromRegistry +0x55c RtlQueryRegistryValuesEx`). 속성 `0xd0`, 접근 `0xf000f`, 만든 뒤 핸들은 닫는다.
- `[정적]` 이름이 `\RPC Control` 또는 `\Windows` (대소문자 무시) 면 `SmpLiberalSecurityDescriptor`, 아니면 `SmpPrimarySecurityDescriptor`. 두 이름은 같은 SD 객체를 쓴다.
- `[정적]` Liberal SD = 소유자 · 그룹 없음, Control `0x14`, DACL 허용 7 (`AclSize 0xb8` · 위 표), SACL 라벨 1 (`0x11` · 마스크 1 · `S-1-16-4096`, `AclSize 0x20`). `S-1-5-18` 없음, 거부 ACE 없음.
- `[정적]` `ProtectionMode` 0 이어도 같은 SD — `SmpProtectionMode` 파일 값 1 (`0x31ce4`), 쓰는 곳 하나 (`SmpConfigureProtectionMode +0x14`), 인자 0 부름은 비트 0 이 0 이면 `+0x20c` 에서 바로 돌아옴.
- `[정적]` smss 는 이 디렉터리에 `NtSetSecurityObject` 를 안 부른다 (3곳 모두 다른 대상).
- `[정적]` csrsrv `CsrParseServerCommandLine +0x210 CsrSetDirectorySecurity` 가 `ObjectDirectory=` 디렉터리의 DACL 을 바꾼다 (결과서: `+0x2bb NtSetSecurityObject`, 정보 4).
- `[정적]` ntoskrnl 은 `\RPC Control` 을 만들지 않는다 (글 2곳 = 이름 비교 · ALPC 붙기, 결과서 PE 바이트).
- `[정적]` 같은 smss 가 `\KnownDlls` 디렉터리 (`SmpOpenKnownDllsHandles +0x18f`, `SmpKnownDllsDirSecurityDescriptor`) 와 `KnownDllPath` 링크 (`+0x1fc`) 를 만든다. 차례는 `\RPC Control` 뒤 (`+0x55c` → `+0x5df`).

## 결과서와 다르게 적은 곳 · 보탠 것

- ① (보탬) `SmpKnownDllsObjectDirectoryName` 의 글을 PE 바이트로 확인 — `.data` `0x30e90` = `{Length 0x14, Max 0x16, Buffer → 0x28f70}`, `0x28f70` = `\KnownDlls`. 결과서는 "`\KnownDlls` 류" 로 적음. 같은 호출의 `SmpKnownDllsPathSuffix` (`0x30e80`) → `\System32`.
- ② (보탬) `SmpLoadDataFromRegistry` 에는 **둘째 `RtlQueryRegistryValuesEx`** 가 있다 — `+0xbc5` (표 `SmpRegistryConfigurationTable2`, `+0xbb5`, `ECX = 0`). 이번 범위 밖 (본문 안 읽음).
- ③ (보탬) csrsrv `CsrParseServerCommandLine.c` 88 ~ 95줄: 서비스 세션이 **아니면** 디렉터리 이름이 명령줄 값 그대로가 아니라 `_snprintf_s(…, "%ws\%ld%s", …)` 로 만든 글이다 (`+0x15b`, 세션 번호가 들어감). 그래서 **`\Windows` 자체를 고치는 건 서비스 세션 (0) 의 csrss** 이고, 세션 1 csrss 는 세션 아래 이름을 고친다 `[정적: .c]`. 들어가는 글이 `\Sessions\1\Windows` 꼴인지는 `[추측]` (형식 인자 셋을 다 안 따라감). 결과서 "`ObjectDirectory=` 값이 `\Windows` 인지는 `[확인 필요]`" 와 같은 줄을 이어서 본 것.
- ④ (보탬, `[추측]`) `ObjectDirectories` 콜백은 받은 버퍼를 **이중 NUL 까지** 돈다 (`+0x12b ~ +0x13a`). `RtlQueryRegistryValuesEx` 가 `REG_MULTI_SZ` 를 줄마다 따로 넘겨도 포인터가 원래 버퍼 안이면 뒤 이름을 또 만들게 되는데, `OBJ_OPENIF` 라 열기만 하고 닫으니 결과는 같다. 결과서 후보 4 (낮음) 와 같은 질문.
- ⑤ (보탬, run99 때 `.asm` 으로 확인) `SmpOpenKnownDllsHandles` 는 **디렉터리를 새로 만들었을 때만** `KnownDllPath` 링크를 만든다 — `+0x19d CMP EAX,0x40000000` · `+0x1a2 JZ → +0x219` (이미 있어 열기만 했으면 `+0x1fc NtCreateSymbolicLinkObject` 를 건너뜀), `+0x1a6 JNS → +0x1c2` (새로 만들었으면 링크로), 음수면 `+0x1bb SmpLogFailure` (`.c` 95줄과 같음).
- 정정 없음. 결과서 값과 다르게 적은 오프셋 없음 (다시 본 줄 모두 일치).

## 아직 확인 필요

**동적** (결과서 "동적으로 확인할 것" 1 ~ 6. **Q181 에 붙일 것**, VM 은 이번에 안 함):

- `\RPC Control` · `\Windows` · `\KnownDlls` 는 전역 객체라 **test.exe 가 없어도** 볼 수 있다 `[추측]` — 부팅이 끝난 뒤 `!object` 로 객체를 얻으면 된다 (Q104 VM 단계에 붙일 수 있음). 헤더 SD 칸 = 객체 `-8` 값 `& ~0xf` (run97 함정 후보).
- ① **부팅 직후 `\RPC Control` 의 실제 SD**: `!object \RPC Control` → `!sd`. 위 Liberal 표와 대조 — **소유자 · 그룹** SID (smss 가 비워 커널이 채운 값), **마스크** (`0xE0000000` · `0x10000000` 그대로인지, Directory 구체 권한으로 바뀌었는지), **`AclSize`** (`0xb8` · `0x20` 그대로인지, `0x9c` · `0x1c` 로 줄었는지), **Control** (`0x8000 | 0x14` + 다른 비트?), 라벨 ACE (`0x11` · 1 · `S-1-16-4096`), **고유 SID 섞인 ACE** (로그온 · 서비스 SID — 정적으론 없음. 있으면 뒤에서 누가 붙인 것).
- ② **같은 때의 `\Windows` SD** — Liberal 과 csrss 뒤 값의 차이 (`CsrSetDirectorySecurity`).
- ③ **타깃 VM 레지스트리** `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager` 의 `ProtectionMode` (1 인가) · `ObjectDirectories` (정확히 `\Windows` · `\RPC Control` 두 줄인가 — 더 있으면 그건 Primary). 그리고 `SubSystems` 의 csrss 명령줄 `ObjectDirectory=` 값.
- ④ **자식 상속**: `\RPC Control\epmapper` 같은 ALPC 포트 SD 에 ACE 3 ~ 6 (`0x0b`) 이 어떻게 들어가나 (급하지 않음 — rpcrt4 가 묻는 건 디렉터리 자신).
- ⑤ **시간 차례**: `\RPC Control` 이 `\KnownDlls` 보다 먼저 만들어지나 (사슬은 정적으로 정함, 실제는 동적).
- ⑥ **`OBJ_OPENIF` 로 이미 있는 걸 열기만 했나**: smss 의 `+0xf1` 반환이 `0` 인가 `0x40000000` 인가. 부팅 맨 앞이라 멈추기 어렵다 — 멈출 수 있으면 `smss!SmpConfigureObjectDirectories` 입구 (이름마다 오나, `+0xe9` 의 `RDI` = `smss+0x32680`) · `+0xf8` (`eax`). 현실적인 대안은 부팅 뒤 ① 의 덤프.
- ⑦ (보탬) **`\KnownDlls` 디렉터리 SD** — 같은 `!sd` 로 위 KnownDllsDir 표 (DACL 5 + 신뢰 라벨 `0x14` `0x20003`) 와 대조. test.exe 가 접근 3 으로 여는 객체 (run44 핸들 `0x4c`) 라 짐 지도 후보와 짝.

**정적** (결과서 "다음 요청 후보" + 보탬):

- ① (= **Q180d**, sast/96 → run100 몫) ntoskrnl `SeAssignSecurityEx2` (rva `0x43d980`) 와 아래 — 소유자 · 그룹 채우기, GENERIC → 구체 권한, `AclSize`, 라벨 SACL 을 그대로 두나. 이 문서의 Liberal SD 를 입력으로.
- ② csrsrv `CsrSetDirectorySecurity` (rva `0x7570`, 859B) 본문 — `\Windows` 가 csrss 뒤 어떤 DACL 이 되나. 이미 analyze 된 모듈이라 export 1개. 보탬 ③ 의 세션 이름 글 (`0x18000cb08` 형식 · 인자) 도 같이.
- ③ (낮음) `AppXDeploymentServer` · `usermgr` · `win32kfull` 의 `\RPC Control` 쪽 — 디렉터리 SD 를 고치나 (새 모듈 analyze 필요). 동적 ① 의 값이 Liberal 과 다를 때만.
- ④ (낮음) ntdll `RtlQueryRegistryValuesEx` — `REG_MULTI_SZ` 를 콜백에 통째로 주나 줄마다 주나 (보탬 ④, SD 결과엔 영향 없음 `[추측]`).
- ⑤ (= **Q180c**, sast/95 → run99 몫) rpcrt4 가 이 SD 에서 무엇을 보나.
- ⑥ (보탬, 작음) `SmpLoadDataFromRegistry +0xbc5` 둘째 표 `SmpRegistryConfigurationTable2` 의 칸 — 이번 주제와 관계 있나 (이름만 봄).

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 안 고침 — smss 는 부팅 때 다른 프로세스, test.exe 초기화 지도 밖):
  - 기준: smss.pdb 줄 새로, csrsrv.pdb 줄에 run98 몫, ntkrnlmp.pdb 줄에 run98 몫 (읽기만), 근거 로그 run98 줄 (사진 안 씀).
  - 흐름도 앞 글: run98 문단 하나.
  - 2.4 run65 `\KnownDlls` 절: (run98) 한 줄 — 그 디렉터리 객체의 출처 · SD.
  - 3장 run97 절: (run98) 한 줄 — `\RPC Control` 처음 SD 에 라벨 SACL 이 있다 (run97 의 (a) · (b) · (c) 가 아님, 커널 저장 꼴은 Q180d · Q181).
  - 3장 새 절 **"부팅 때 smss 가 만드는 객체 디렉터리와 처음 SD — `smss!SmpInit` → … → `SmpConfigureObjectDirectories` → `NtCreateDirectoryObject` (run98)"**.
  - 4장: run97 동적 줄 · PE_emulator 줄에 (run98) 보탬, run98 새 줄 넷 (동적 · 정적 · 짐 지도 후보 · PE_emulator 와 보탠 것).
  - 5장: run 표 · 03_sprint 요청서 번호 run98 줄.
- 짐 지도: **고치지 않음** — 부팅 준비 짐 자리 (7.6.2 `Ⓑ` · 7.6.3) 는 **nt 커널이 부팅 때 놓는 전역** 이고, 7.6.11 "앞" 은 **csrss** 몫이다. smss 가 만드는 객체 디렉터리와 그 헤더 SD 는 둘 다 아니고, 객체 헤더 SD 칸 자체가 지금 짐 지도에 없다 (run97 과 같은 판단). 짐 지도 후보로 flow.md 4장 run98 줄에만 적음 — 넣을지 · 어느 단계 표시로 넣을지는 사용자와.
- 이전 run 문서: 정정 없음.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점** `[추측]`: 이번에 읽은 smss 줄 안에 디버거 칸을 읽는 줄은 없다. 단 부팅 맨 앞 (smss) 에 멈추면 시간 기록 · 타임아웃이 달라질 수 있어, 동적은 부팅 뒤 덤프로 보는 게 낫다 (결과서 추천).
- 도구 (결과서 "스크립트 문제" — 오류는 없음, 불편한 점 2개. `.claude/` 는 오케스트레이터 몫이라 제안만):
  - `run_ghidra.sh refs smss.exe NtSetSecurityObject` 가 0줄 — import 이름이 외부 위치로 잡혀서. 외부 위치 참조를 따라가는 모드 (`callers <모듈> <import 이름>`) 제안.
  - `_calls.tsv` 가 export 한 함수만 담아 "모듈 전체에서 X 를 부르는 곳" 을 바로 못 셈 — 결과서는 레포 밖 임시 파이썬으로 `FF 15 <IAT>` 를 훑어 `.asm` 과 맞춤.
- **함정 후보** (→ windbg-kd-runbook pitfalls 에 추가 제안): smss 의 표 콜백 (`SmpConfigureObjectDirectories` 등) 은 정적 지도에 caller 가 없다 — 멈춤점 콜스택 위 프레임은 `ntdll!RtlQueryRegistryValuesEx` 안쪽일 것 `[추측]`, `smss!SmpLoadDataFromRegistry+0x563` 을 기대해야 한다.
