# 94 rpc-dir-create — 정적 분석 결과

- 요청서: `_workspace/sast/94_rpc-dir-create.md` (Q180b = Q180 ②, PE_emulator 요청 34 의 2-가 · 2-나)
- 스크립트 문제: **오류는 없음.** (`analyze` 1번, `map` 1번, `export` 2번, `refs` 6번 모두 로그에 `SCRIPT ERROR` 0줄.) 다만 **불편한 점 2개**를 적어 둔다 (고치라는 게 아니라 제안):
  1. `run_ghidra.sh refs smss.exe NtSetSecurityObject` 는 **0줄** 이다 (`# NtSetSecurityObject rva=0xfffffffec0000031 len=0x1` — 이름이 외부 위치 (import) 로 잡혀서). import 한 함수의 "누가 부르나" 는 `refs` 로 안 나온다. 제안: `RefsTo.java` 가 외부 위치 (`ExternalLocation`) 의 참조도 따라가게 하거나, `run_ghidra.sh callers <모듈> <import 이름>` 같은 모드 추가.
  2. `_calls.tsv` 는 **export 한 함수만** 들어 있어서 "모듈 전체에서 `NtSetSecurityObject` 를 부르는 곳" 을 바로 못 센다. 이번엔 임시 파이썬 (스크래치패드, 레포 밖) 으로 `.text` 의 `FF 15 <IAT 칸>` 을 훑어서 센 뒤, 나온 호출 자리를 `.asm` 줄과 맞췄다 (전부 일치, 아래).
- 모듈 / 바이너리: smss (`ms/bin/.../System32/smss.exe`, x64, 베이스 `0x140000000`, **228,824 B** = 요청서 값과 같음)
  - PDB: `ms/sym/smss.exe.pdb` (290,816 B), GUID+Age = `352F20C3490D7BF3449E0A0AE67D4E0F1`. 이번에 처음 받음 (`fetch_pdb.py smss.exe`).
  - **analyze 새로 함** (15초). `.gpr` + `_index.tsv` (325 함수, `FUN_` 0개) + `ms/ghidra/smss.analyze.log` 63줄 `SetPdb: smss.exe <- ...smss.exe.pdb` 다 있음.
  - 별칭 표: `ms/sym/smss.exe.aliases.tsv` (4곳, 이름 8개). 이 요청에 나오는 이름은 별칭과 안 겹침.
- 지도: `ms/disassemble/smss/_map/wmain_{edges,nodes}.tsv`, `_summary.txt` (뿌리 `wmain`, 152 함수). 아래 "지도에서 옮긴 것".
- 뽑은 함수 (전부 depth 0, `ms/decomp/smss/*.c`, `ms/disassemble/smss/*.asm`, `_calls.tsv`):
  - 요청서 대상: `SmpConfigureObjectDirectories` · `SmpConfigureProtectionMode` · `SmpCreateSecurityDescriptors` · `SmpCreateProtectedPrefixes` (`NtSetSecurityObject` caller)
  - 덧붙임 (길 위에 있어서): `SmpLoadDataFromRegistry` · `SmpInit` · `SmpOpenKnownDllsHandles` · `SmpInitializeDosDevices` · `SmpSetDumpSecurityAndAttributes` (`NtSetSecurityObject` caller) · `SmpInitializePendingRename` (SD 를 만드는 줄이 있어서 뽑았으나 본문은 안 읽음)
  - 이름은 요청서의 `[추측]` 이름 중 `SmpCreateSecurityDescriptors` 가 **PDB 에 그대로 있다**. `SmpInitializeObjectDirectories` 는 없고, 일을 하는 건 **`SmpConfigureObjectDirectories`** (표 콜백).
- **ntoskrnl**: Ghidra 프로젝트·export 안 건드림. `ms/decomp/ntoskrnl/_index.tsv`, `ms/disassemble/ntoskrnl/_calls.tsv`, 이미 있는 `.c` 읽기 + PE 바이트 (`objdump`, 파이썬 스캔) 만 썼다.
- 버전 확인: **일치 (간접).** smss 는 "알려진 사실" 표에 없다. 대신:
  - 파일 크기 228,824 B = 요청서 값.
  - 바이너리 문자열이 요청서 실마리와 같음: `ObjectDirectories` (rva `0x29060`), `ProtectionMode` (rva `0x29040`), 기본값 `\Windows\0\RPC Control\0\0` (rva `0x29088`).
  - 호출 자리 이중 확인: PE 바이트 스캔 (`FF 15` 의 4바이트 변위 위치) 에서 나온 `NtCreateDirectoryObject` 3곳 · `NtSetSecurityObject` 3곳이 `_calls.tsv`/`.asm` 의 call 줄과 **하나도 안 어긋남** (스캔 위치 = call 오프셋 + 3, `48 FF 15` 7바이트 꼴이라서).
  - 커널 PDB GUID 확인은 이번 범위 밖 (ntoskrnl 안 건드림).

---

## 한눈에 (먼저 읽기)

1. **만드는 곳은 한 함수, 표 콜백이다.** `smss!SmpConfigureObjectDirectories` (rva `0x143d0`, 347 B) 가 `ObjectDirectories` 문자열마다 `+0xf1/+0xf8` 에서 `NtCreateDirectoryObject` 를 부른다. 이 함수는 **아무도 직접 안 부른다** (표 `SmpRegistryConfigurationTable` 의 1번 칸). 표를 돌리는 건 `SmpLoadDataFromRegistry +0x55c/+0x563` 의 `RtlQueryRegistryValuesEx` (`RelativeTo=2` = `...\Control`, 경로 `"Session Manager"`).
2. **`ProtectionMode` 가 먼저다.** 같은 표에서 0번 칸이 `ProtectionMode` (`SmpConfigureProtectionMode`), 1번 칸이 `ObjectDirectories`. 그래서 SD 는 `ProtectionMode` 콜백이 (다시) 만든 뒤에 디렉터리가 만들어진다.
3. **`\RPC Control` 은 이름으로 따로 골라진다.** 이름을 `\RPC Control` · `\Windows` 와 대소문자 무시로 비교해서 **같으면 `SmpLiberalSecurityDescriptor`, 아니면 `SmpPrimarySecurityDescriptor`** 를 `OBJECT_ATTRIBUTES.SecurityDescriptor` 에 넣는다. 에뮬레이터 레지스트리 값 (`\Windows`, `\RPC Control`) 은 **둘 다 Liberal 갈래**.
4. **Liberal SD (= `\RPC Control` 의 처음 SD), `ProtectionMode` 1.**
   - 소유자 **없음**, 그룹 **없음** (smss 가 안 채움 → 커널이 만든 쪽 토큰에서 채움 `[추측]`).
   - DACL **ACE 7개, 전부 허용 (type 0)**, 순서 고정:
     0 `S-1-1-0` `0xE0000000` · 1 `S-1-5-12` `0xE0000000` · 2 `S-1-5-32-544` `0x10000000` (여기까지 플래그 0) · 3 `S-1-1-0` `0xE0000000` · 4 `S-1-5-12` `0xE0000000` · 5 `S-1-5-32-544` `0x10000000` · 6 `S-1-3-0` `0x10000000` (3~6 은 플래그 `0x0b` = 상속 전용). **`S-1-5-18` 은 없다.**
   - SACL **있음**, 라벨 ACE **1개**: type `0x11`, 플래그 0, 마스크 `0x00000001` (NO_WRITE_UP), SID `S-1-16-4096` (낮은 무결성).
5. `ProtectionMode` 0 이어도 **SD 가 달라지지 않는다** (코드상). 이유: `SmpProtectionMode` 초기값이 `.data` 에 이미 **1** 이고, `SmpInit` 이 먼저 한 번 (인자 1) 만들며, 콜백의 인자-0 호출은 모드 비트가 0 이면 **바로 돌아온다** (`+0x1ff ~ +0x20c`).
6. **smss 는 이 디렉터리에 `NtSetSecurityObject` 를 안 부른다.** smss 안 호출 3곳은 파이프·메일슬롯 장치 루트 (`SmpCreateProtectedPrefixes`) 와 덤프/페이지 파일 (`SmpSetDumpSecurityAndAttributes`) 뿐.
7. **smss 밖에서 `\RPC Control` 자체의 SD 를 고치는 곳은, 보이는 범위에선 못 찾음.** 같은 `ObjectDirectories` 이름 중 **`\Windows` 는** csrss 가 나중에 DACL 을 바꾼다 (`csrsrv!CsrSetDirectorySecurity`).
8. **커널은 `\RPC Control` 을 안 만든다.** `ntoskrnl.exe` 의 `RPC Control` 문자열 2개는 "이름 비교 표" 와 "ALPC 포트에 붙는 쪽 (`\RPC Control\FconAlpcPort`)" 이다.

---

## 지도에서 옮긴 것 (`ms/disassemble/smss/_map/wmain_*`, 뿌리 `wmain`)

| 함수 | rva | 크기 | 깊이 | caller 수 | 부르는 자리 (`_edges.tsv`) |
|---|---|---|---|---|---|
| `SmpInit` | `0x155ac` | 1532 | 1 | 1 | `wmain +0x556/+0x55b` |
| `SmpCreateSecurityDescriptors` | `0xa1b0` | 4138 | 2 | 2 | `SmpInit +0x2f4/+0x2f9` (인자 `CL=1`, `+0x2f2 MOV CL,1`), `SmpConfigureProtectionMode +0x1a/+0x1f` (인자 0) |
| `SmpLoadDataFromRegistry` | `0x162a4` | 3650 | 2 | 1 | `SmpInit +0x534/+0x539` |
| `SmpOpenKnownDllsHandles` | `0x176b0` | 709 | 3 | 2 | `SmpLoadDataFromRegistry +0x5df/+0x5e4`, `SmpInitializeKnownDlls +0x83/+0x88` |
| `SmpInitializeDosDevices` | `0x15bb0` | 485 | 3 | 1 | `SmpLoadDataFromRegistry +0x6a4/+0x6a9` |
| `SmpCreateProtectedPrefixes` | `0x94b0` | 3164 | 3 | 1 | `SmpLoadDataFromRegistry +0x739/+0x73e` |
| `SmpConfigureObjectDirectories` | `0x143d0` | 347 | **지도에 없음** | 0 | (표 콜백. `_calls.tsv`·지도 둘 다 caller 없음) |
| `SmpConfigureProtectionMode` | `0x145d0` | 38 | **지도에 없음** | 0 | (표 콜백) |

- 사슬: `wmain` → `SmpInit` → (`SmpCreateSecurityDescriptors(1)`) → `SmpLoadDataFromRegistry` → `RtlQueryRegistryValuesEx` → (표 콜백 `SmpConfigureProtectionMode` → `SmpCreateSecurityDescriptors(0)`, 이어서 `SmpConfigureObjectDirectories` → `NtCreateDirectoryObject`).
- 표 콜백은 함수 포인터 표를 거쳐서 정적 지도에 안 잡힌다 (`ghidra-headless-export/reference.md` "지도" 주의). 표는 PE 바이트에서 직접 읽었다 (아래 1-1).

---

## 질문별 답 [정적]

### 질문 1 (2-가). 만드는 함수와 SD

#### 1-1. 어디서 읽고 어디서 부르나

| 단계 | 어디 | 오프셋 / 값 |
|---|---|---|
| 표 위치 | `SmpRegistryConfigurationTable` | rva `0x300b0` (`SmpLoadDataFromRegistry +0x54a LEA R8,[0x1400300b0]`). 칸 크기 `0x38`. 아래 4칸 |
| 읽기 | `SmpLoadDataFromRegistry` | `+0x55c CALL RtlQueryRegistryValuesEx` / ret `+0x563` (`ECX=2` = `RTL_REGISTRY_CONTROL`, `RDX` = 문자열 `Session Manager`, `R8` = 표) |
| 0번 칸 (`+0x000`) | 이름 `ProtectionMode`, 콜백 `SmpConfigureProtectionMode`, 기본형 `REG_DWORD`(4), 기본값 없음 | 이름 포인터는 rva `0x300c0` |
| 1번 칸 (`+0x038`) | 이름 `ObjectDirectories`, 콜백 `SmpConfigureObjectDirectories`, 기본형 `REG_MULTI_SZ`(7), 기본값 rva `0x29088` = `\Windows`·`\RPC Control` (글자 그대로) | 이름 포인터는 rva `0x300f8` |
| 2번 칸 이후 | `BootExecute` 등. 표 끝은 26번 칸 (이름 NULL) | 이 요청 밖 |

- 문자열 `ObjectDirectories` · `ProtectionMode` 를 가리키는 곳은 **이 표 칸 둘뿐** (PE 스캔: 코드 참조 0, 데이터 포인터 1씩). 그래서 "문자열 xref" = 이 표.
- `ProtectionMode` 값이 레지스트리에 없으면 기본 `REG_DWORD` 가 길이 0 으로 오는 것으로 `[추측]` (`RtlQueryRegistryValuesEx` 쪽은 안 읽음). 콜백이 `길이 != 4` 면 값 1 로 두므로 어느 쪽이든 1.

#### 1-2. 이름마다 디렉터리를 만드는 자리: `SmpConfigureObjectDirectories` (`ms/disassemble/smss/SmpConfigureObjectDirectories.asm`)

| 줄 | 하는 일 |
|---|---|
| `+0x1c`, `+0x30`~`+0x3a`, `+0x3e`~`+0x45`, `+0x55` | 비교용 `UNICODE_STRING` 2개를 스택에 만듦: `[RBP-0x50]` 길이 `0x18` → 문자열 rva `0x2a0f8` = `\RPC Control` (12글자), `[RBP-0x40]` 길이 `0x10` → rva `0x2a118` = `\Windows` (8글자) |
| `+0x61 CMP [R8],SI` / `+0x65 JZ` → `+0x140` | 입력 (여러 문자열이 `\0` 으로 이어진 것) 이 빈 문자열이면 끝. 문자열마다 루프 `+0x6b ~ +0x13a` |
| `+0x72/+0x79` | `RtlInitUnicodeString(&이름, 현재 문자열)` |
| `+0x7e` | `RDI = SmpPrimarySecurityDescriptor` (기본값) |
| `+0x90/+0x97` | `RtlEqualUnicodeString(이름, "\RPC Control", TRUE)` → 같으면 `+0x9e JNZ` 가 `+0xbb` 로 |
| `+0xab/+0xb2` | `RtlEqualUnicodeString(이름, "\Windows", TRUE)` → 다르면 `+0xb9 JZ` 가 `+0xc2` 로 (Primary 유지) |
| `+0xbb` | `RDI = SmpLiberalSecurityDescriptor` (둘 중 하나와 같을 때만) |
| `+0xc6 ~ +0xed` | `OBJECT_ATTRIBUTES` 채움: Length `0x30` (`+0xc6`), RootDirectory 0 (`+0xda`), ObjectName = 이름 (`+0xd1`), **Attributes `0xd0`** (`+0xe2`) = `OBJ_PERMANENT 0x10` + `OBJ_CASE_INSENSITIVE 0x40` + `OBJ_OPENIF 0x80`, **SecurityDescriptor = RDI** (`+0xe9`), QoS 0 (`+0xed`) |
| `+0xd5` | `DesiredAccess = 0xf000f` |
| **`+0xf1 CALL NtCreateDirectoryObject` / ret `+0xf8`** | 문자열마다 한 번. **`…Ex` 는 import 안 함** (PE import 표에 없음) |
| `+0x114/+0x119` | 실패 (음수) 면 `SmpLogFailureString(…, 0x1cee, …)` 만 하고 **계속** (함수는 항상 0 을 돌려줌 `+0x148 XOR EAX,EAX`) |
| `+0x11f/+0x126` | 성공이면 `NtClose(핸들)` (영구 객체라 닫아도 남음) |
| `+0x12b ~ +0x13a` | 현재 문자열 끝까지 건너뛰고 다음 문자열 |

- 같은 모듈에서 `NtCreateDirectoryObject` 를 부르는 곳 전체 (PE 스캔 + `_calls.tsv`): **3곳** — `SmpConfigureObjectDirectories +0xf1` (이 질문), `SmpLoadDataFromRegistry +0x708/+0x70f` (`\Sessions`, Primary SD, `+0x6e0` 에서 읽음, 핸들은 `SmpSessionsObjectDirectory` 에 둠), `SmpOpenKnownDllsHandles +0x18f/+0x196` (`\KnownDlls` 류, `SmpKnownDllsDirSecurityDescriptor`, `+0x161` 에서 읽음).

#### 1-3. 이름 비교: `\RPC Control` 만 따로 다루나? — **이름 비교는 있다. 하지만 `\Windows` 와 한 묶음.**

- 두 이름 중 하나면 Liberal, 그 밖의 이름이면 Primary. **`ObjectDirectories` 에 이름이 둘뿐이면 (`\Windows`, `\RPC Control`) 둘은 같은 Liberal SD** — 같은 **객체** (포인터 `0x32668`) 를 두 디렉터리가 같이 쓴다.
- `\RPC Control` 만의 줄은 `+0x90` 비교 하나. (둘째 비교 `+0xab` 가 `\Windows`.)

#### 1-4. SD 를 만드는 함수: `SmpCreateSecurityDescriptors` (rva `0xa1b0`, 4138 B, 인자 1개 `CL`)

**두 번 불린다. 1번째 `SmpInit +0x2f4` (CL=1), 2번째 `SmpConfigureProtectionMode +0x1a` (CL=0).**

- **CL=1 (`SmpInit`)**: `+0xa7 ~ +0x1f8` 에서 SD 6개의 **빈 틀**을 만든다 (`RtlCreateSecurityDescriptor(&몸통, 1)` + `RtlSetDaclSecurityDescriptor(sd, TRUE, NULL, FALSE)` = "NULL DACL"). 몸통 6개: Primary (rva `0x32640`, 포인터 `0x32628`), **Liberal (몸통 `0x32680`, 포인터 `0x32668`; `+0xe0 ~ +0x114`)**, KnownDllsDir, KnownDll, KnownDllsSymLink, ApiPort. 이어서 SID 9개 (`+0x254 ~ +0x4e3`), KnownDlls·ApiPort 쪽 SD (아래 1-8), 마지막에 `+0x8cc` 에서 `SmpProtectionMode & 1` 이면 Primary·Liberal 의 DACL (·SACL) 을 만든다.
- **CL=0 (`SmpConfigureProtectionMode`)**: `+0x9f TEST CL,CL; +0xa1 JZ → +0x1ff`. `+0x1ff ~ +0x20c`: `SmpProtectionMode & 1` 이 0 이면 `XOR EAX,EAX; JMP 0x14000b1ad` (= `+0xffd`, 함수 끝) 로 **아무것도 안 하고 돌아옴**. 1 이면 SID 9개를 다시 만들고 (`+0x220 ~`) `+0x504 TEST BL; JZ → +0x8cc` 로 점프해 **Primary·Liberal 만 다시 만든다.**
- SID 는 `RtlAllocateAndInitializeSid(권한 6바이트, 하위 개수, 하위 0..)` 로 만들어진다 (**상수 블롭 아님**). 권한 6바이트는 스택에 `dword 0 + word` 로 쓴 것 (`+0x34` 의 `0x100` → 권한 1, `+0x6d` `0xf00` → 15, `+0x77` `0x500` → 5, `+0x81` `0x1000` → 16, `+0x8b` `0x300` → 3, `+0x95` `0x1300` → 19).

| 스택 칸 (`[RBP+x]`) | 만든 줄 (call / ret) | 값 | 아마 이름 `[추측: 이름은 공개 문서 기억, 값은 코드]` |
|---|---|---|---|
| `-0x49` | `+0x254/+0x25b` | `S-1-1-0` | Everyone |
| `-0x21` | `+0x2a9/+0x2b0` | `S-1-15-2-1` | ALL APPLICATION PACKAGES |
| `-0x19` | `+0x2f8/+0x2ff` | `S-1-15-2-2` | ALL RESTRICTED APPLICATION PACKAGES |
| `-0x51` | `+0x34d/+0x354` | `S-1-5-32-544` | Administrators |
| `-0x29` | `+0x39c/+0x3a3` | `S-1-3-0` | CREATOR OWNER |
| `-0x41` | `+0x3ec/+0x3f3` | `S-1-5-12` | RESTRICTED |
| `-0x11` | `+0x43c/+0x443` | `S-1-5-18` | SYSTEM |
| `-0x31` | `+0x48e/+0x495` | `S-1-16-4096` | 낮은 무결성 (Low Mandatory Level) |
| `-0x39` | `+0x4e3/+0x4ea` | `S-1-19-512-8192` | 프로세스 신뢰 (PPL, WinTcb 수준) |

#### 1-5. **Liberal SD** (`\RPC Control`, `\Windows` 의 처음 SD) — 구성요소별 출처

| 구성요소 | smss SD 에 있나 | 값 / 출처 | 근거 |
|---|---|---|---|
| Revision | 있음 | 1 (`RtlCreateSecurityDescriptor(&몸통, 1)`) | `+0xe0 ~ +0xf1` |
| Control (smss 쪽 절대형) | 있음 | `0x0014` = `SE_DACL_PRESENT 0x4` + `SE_SACL_PRESENT 0x10`. `DEFAULTED`·`PROTECTED`·`AUTO_INHERITED` 비트 **없음** | `RtlSetDaclSecurityDescriptor(sd, DL=1, …, R9D=0)` `+0xe84`, `RtlSetSaclSecurityDescriptor(sd, DL=1, …, R9D=0)` `+0xea0`. `RtlSetControlSecurityDescriptor` 는 import 도 안 함 |
| **소유자 (Owner)** | **없음 (NULL)** | smss 에서 `RtlSetOwnerSecurityDescriptor` 를 이 SD 에 안 부른다 (smss 전체에서 이 함수를 부르는 곳은 `SmpSetDumpSecurityAndAttributes +0x1d7` 한 곳 = 덤프 파일). **만든 프로세스 (smss, SYSTEM) 토큰에서 커널이 채움** `[추측]` — 어느 SID 가 들어가는지는 동적 |
| **그룹 (Group)** | **없음 (NULL)** | `RtlSetGroupSecurityDescriptor` 는 smss 가 import 도 안 함. 토큰의 기본 그룹에서 커널이 채움 `[추측]` |
| **DACL** | 있음, ACE 7개 | 아래 표 | `+0xe84/+0xe8b` 에서 SD 에 붙임 |
| **SACL** | 있음, 라벨 ACE 1개 | 아래 표 | `+0xea0/+0xea7` 에서 SD 에 붙임 |

**Liberal DACL ACE 표 (`ProtectionMode` 1 갈래, 순서 그대로)**

ACL 머리: `AclRevision` 2, `Sbz1` 0, **`AclSize` `0xb8` (184)**, **`AceCount` 7**, `Sbz2` 0. (`AclSize` = 힙 요청 크기: `(len(S-1-5-12)+len(S-1-5-32-544)+len(S-1-1-0)) *2 + len(S-1-3-0) + 0x5c` = `(12+16+12)*2 + 12 + 92` = **184**. 줄: `+0xc27 ~ +0xc82` 합산, `+0xc89` 할당, `+0xcf7/+0xd00 RtlCreateAcl(…, EDX=184, 2)`.) ACE 를 다 넣은 실제 사용분은 **156 B** (8 + 20+20+24+20+20+24+20) — 뒤 28 B 는 쓰이지 않음.

| # | 형 | 플래그 | 마스크 | SID | ACE 크기 | 넣는 줄 (call / ret) |
|---|---|---|---|---|---|---|
| 0 | 허용 (`0`) | `0x00` | `0xE0000000` (GENERIC_READ\|WRITE\|EXECUTE) | `S-1-1-0` | 20 (`0x14`) | `+0xd1e/+0xd25` |
| 1 | 허용 | `0x00` | `0xE0000000` | `S-1-5-12` | 20 | `+0xd36/+0xd3d` |
| 2 | 허용 | `0x00` | `0x10000000` (GENERIC_ALL) | `S-1-5-32-544` | 24 (`0x18`) | `+0xd54/+0xd5b` |
| 3 | 허용 | **`0x0b`** (OBJECT_INHERIT `1` + CONTAINER_INHERIT `2` + INHERIT_ONLY `8`) | `0xE0000000` | `S-1-1-0` | 20 | 넣기 `+0xd6f/+0xd76`, 플래그 쓰기 `+0xda0` (`GetAce(3)` `+0xd85`) |
| 4 | 허용 | `0x0b` | `0xE0000000` | `S-1-5-12` | 20 | `+0xda8/+0xdaf`, 플래그 `+0xdd6` (`GetAce(4)` `+0xdbe`) |
| 5 | 허용 | `0x0b` | `0x10000000` | `S-1-5-32-544` | 24 | `+0xdde/+0xde5`, 플래그 `+0xe0c` (`GetAce(5)` `+0xdf4`) |
| 6 | 허용 | `0x0b` | `0x10000000` | `S-1-3-0` | 20 | `+0xe14/+0xe1b`, 플래그 `+0xe42` (`GetAce(6)` `+0xe2a`) |

- ACE 형은 전부 `RtlAddAccessAllowedAce(acl, 2, 마스크, sid)` → **허용 (type 0)**. **거부 ACE 없음.** 플래그를 0x0b 로 바꾸는 줄은 위 표의 "플래그 쓰기" (`MOV byte [RAX+1],0xb`) 뿐.
- 앞 3개 (0~2) 는 **이 디렉터리 자신**에 적용, 뒤 4개 (3~6) 는 `INHERIT_ONLY` 라 **자식 객체가 만들어질 때** 상속되는 몫. (예: `\RPC Control\epmapper` 같은 ALPC 포트.)
- **`S-1-5-18` (SYSTEM) 은 이 DACL 에 없다.** SYSTEM 은 `S-1-5-32-544` 멤버라서 `Administrators` ACE 로 닿는 것 `[추측]`.
- **`S-1-5-12` (RESTRICTED) 와 `S-1-1-0` (Everyone) 에 쓰기 (`GENERIC_WRITE`) 까지 준다** — 이게 Liberal 이다 (아래 Primary 는 쓰기 없음).

**Liberal SACL (라벨) — 있음**

ACL 머리: `AclRevision` 2, `Sbz1` 0, **`AclSize` `0x20` (32)** (= `len(S-1-16-4096)` 12 + `0x14`, `+0xcc2 LEA EBX,[RAX+0x14]`, `+0xe46 RtlCreateAcl`), **`AceCount` 1**, `Sbz2` 0. 사용분 28 B.

| # | 형 | 플래그 | 마스크 | SID | 크기 | 줄 |
|---|---|---|---|---|---|---|
| 0 | **`0x11`** (SYSTEM_MANDATORY_LABEL_ACE) | `0x00` | `0x00000001` (`SYSTEM_MANDATORY_LABEL_NO_WRITE_UP`) | `S-1-16-4096` | 20 (`0x14`) | `+0xe68/+0xe6f RtlAddMandatoryAce` (`+0xe5b MOV [RSP+0x28],R13D` = 1, `+0xe63 MOV byte [RSP+0x20],0x11`, `+0xe56 XOR R8D,R8D` = 플래그 0, `+0xe52 R9=[RBP-0x31]`) |

- 그 밖의 SACL ACE (감사 `0x02`, 신뢰 라벨 `0x14` 등) 는 **Liberal 에 없다.** (신뢰 라벨은 KnownDlls 쪽 SD 에만, 1-8.)

**참고: Primary SD (그 밖의 이름·`\Sessions`·DosDevices 에 쓰임), `ProtectionMode` 1.** 소유자·그룹 없음, Control `0x0004`, **SACL 없음 (라벨 없음).** DACL 7개, 순서·플래그는 Liberal 과 같고 **마스크만 다름**: 0·1·3·4 번 (`S-1-1-0`, `S-1-5-12`) 이 `0xA0000000` (GENERIC_READ\|EXECUTE, **쓰기 없음**), 2·5 번 `S-1-5-32-544` `0x10000000`, 6 번 `S-1-3-0` `0x10000000`. 줄: `+0xade ~ +0xc11` (ACE 7개, 플래그 쓰기 `+0xb5f +0xb98 +0xbd1 +0xc11`), `+0xc15` 에서 Primary SD 에 붙임. `AclSize` 도 184.

#### 1-6. `ProtectionMode` 0 갈래와 차이 (한 줄)

- **코드상 차이 없음**: 레지스트리가 0 (또는 비트 0 이 0 인 값, 예 2) 이어도 `SmpInit` 이 먼저 (초기값 1 로) Primary·Liberal 을 7-ACE 로 만들어 뒀고 (`SmpProtectionMode` 초기값 `.data` rva `0x31ce4` = `01 00 00 00`, 쓰는 곳은 `SmpConfigureProtectionMode +0x14` 한 곳 뿐 — `refs`), 콜백의 인자-0 호출이 `+0x1ff ~ +0x20c` 에서 바로 돌아와 **아무것도 안 바꾼다.** 모드 1 과 SD 가 같다.
- 모드 비트가 0 인 채로 SD 를 새로 만드는 길은 코드에 없다 → "NULL DACL 만 남는 모드" 는 이 빌드에선 만들어지지 않음 `[정적, 읽는 이는 한 번 더 확인 권장 — 초기값 줄이 핵심]`.

#### 1-7. self-relative 바이트를 다시 만들 수 있는 수준 — 구성요소 바이트 (리틀엔디안)

SD 를 자체 모양 (self-relative) 으로 붙이는 순서·맞춤은 **커널 몫** (sast/93 질문 3, Q181). smss 가 넘기는 건 **절대 모양 SD** (`0x28` B 짜리 `SECURITY_DESCRIPTOR`: Owner·Group NULL, Sacl·Dacl 은 포인터). 구성요소만 적는다:

```
SID 4개
  S-1-1-0        01 01 00 00 00 00 00 01 00 00 00 00                        (12 B)
  S-1-5-12       01 01 00 00 00 00 00 05 0c 00 00 00                        (12 B)
  S-1-5-32-544   01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00            (16 B)
  S-1-3-0        01 01 00 00 00 00 00 03 00 00 00 00                        (12 B)
  S-1-16-4096    01 01 00 00 00 00 00 10 00 10 00 00                        (12 B)

Liberal DACL (AclSize 0xb8, 쓰인 156 B, 뒤 28 B 미사용)
  02 00 b8 00 07 00 00 00
  00 00 14 00  00 00 00 e0  <S-1-1-0>
  00 00 14 00  00 00 00 e0  <S-1-5-12>
  00 00 18 00  00 00 00 10  <S-1-5-32-544>
  00 0b 14 00  00 00 00 e0  <S-1-1-0>
  00 0b 14 00  00 00 00 e0  <S-1-5-12>
  00 0b 18 00  00 00 00 10  <S-1-5-32-544>
  00 0b 14 00  00 00 00 10  <S-1-3-0>

Liberal SACL (AclSize 0x20, 쓰인 28 B, 뒤 4 B 미사용)
  02 00 20 00 01 00 00 00
  11 00 14 00  01 00 00 00  <S-1-16-4096>
```

- **알 수 없는 것 (정적):** 커널이 저장할 때 (a) `AclSize` 를 184·32 로 두는지 줄이는지, (b) `GENERIC_*` 비트 (`0xE0000000` 등) 를 Directory 의 구체 권한으로 바꿔 저장하는지 (`ObpDirectoryObjectType` 의 generic 매핑 `[추측]`), (c) 소유자·그룹을 어느 SID 로 채우는지. 이건 `SeAssignSecurityEx` → `SeAssignSecurityEx2` (rva `0x43d980`, 133 B, 이번에 안 뽑음) 몫 → "다음 요청 후보" · "동적으로 확인".

#### 1-8. 그 밖에 이 함수가 같이 만드는 SD (KnownDlls · API 포트) — 짐 지도용

모두 `SmpCreateSecurityDescriptors(1)` 이 만든다 (`SmpInit` 한 번). 전부 소유자·그룹 없음, ACE 형 허용 (플래그 0), SID 는 위 표 이름.

| SD (PDB 이름) | 쓰는 자리 | DACL (순서) | SACL |
|---|---|---|---|
| `SmpApiPortSecurityDescriptor` (몸통 `0x32780`) | `SmpInit +0x33a` → `+0x38c NtAlpcCreatePort` | ① `S-1-5-32-544` `0x10000000` ② `S-1-5-18` `0x10000000` (`+0x56a ~ +0x5a1`, AclSize 60) | 없음 |
| `SmpKnownDllsDirSecurityDescriptor` (몸통 `0x326c0`) | `SmpOpenKnownDllsHandles +0x161` → `+0x18f NtCreateDirectoryObject` (속성 `0xd0`, `+0x180`) | ① `S-1-5-32-544` `0x10000000` ② `S-1-1-0` `0xA0000000` ③ `S-1-15-2-1` `0xA0000000` ④ `S-1-5-12` `0xA0000000` ⑤ `S-1-15-2-2` `0xA0000000` (`+0x6ba ~ +0x73b`, AclSize 140) | 프로세스 신뢰 라벨 (type `0x14`, 마스크 `0x20003`, `S-1-19-512-8192`) (`+0x774`) |
| `SmpKnownDllsSymLinkSecurityDescriptor` (몸통 `0x32740`) | `SmpOpenKnownDllsHandles +0x1e1` → `+0x1fc NtCreateSymbolicLinkObject` | 위 KnownDllsDir 와 **같은 ACL 객체** (`+0x7c8`) | 없음 |
| `SmpKnownDllSecurityDescriptor` (몸통 `0x32700`) | `SmpInitializeKnownDllsInternal +0xf4` | ① `S-1-1-0` ② `S-1-15-2-1` ③ `S-1-15-2-2` ④ `S-1-5-12` (넷 다 `0xE0000000`) ⑤ `S-1-5-32-544` `0x10000000` (`+0x975 ~ +0x9f6`, AclSize 140) | ① 라벨 type `0x11` 마스크 `1` `S-1-16-4096` (`+0xa30`) ② 신뢰 라벨 type `0x14` `S-1-19-512-8192`, 마스크 `0x3000f` (`SmpHostSmss == 0`) 또는 `0x2000f` (`+0xa3c ~ +0xa65`) |

- `\Windows` 는 위 Liberal 표와 **같은 SD** (같은 포인터). `\KnownDlls` 는 Liberal 이 아니라 **KnownDllsDir** 쪽.
- Primary SD 의 다른 쓰임: `\Sessions` 디렉터리 (`SmpLoadDataFromRegistry +0x6e0`), 장치 이름 연결 (`SmpInitializeDosDevices +0xb7`, 심볼릭 링크 `+0x10d/+0x114`, 이때 Primary SD 의 `Control` 에 `0x8` 을 잠깐 켰다가 되돌림 `+0xed ~ +0xf5`, `+0x179 ~ +0x185`), 이름 있는 파이프 `Sessions` 접두어 (`SmpCreateProtectedPrefixes +0xaf9`, `NtCreateFile +0xb2e`).

**지금 우리 (PE_emulator) 맞나·틀리나: 틀림.** 에뮬레이터는 `\RPC Control` 에 SD 를 안 채운다. 실제 smss 는 **DACL 7 ACE + 낮은 무결성 라벨 SACL** 을 붙여 만든다 (소유자·그룹은 smss 가 비움). 에뮬레이터가 `NtQuerySecurityObject(0x17)` 에 답하려면 위 1-5·1-7 표를 객체의 SD 로 넣어 두면 된다 (라벨 비트 `0x10` 이 줄 것: SACL 의 라벨 ACE 1개).

---

### 질문 2 (2-나). 만든 뒤 고치는 곳

#### 2-1. smss 안: **`\RPC Control` 에는 없다.** (`NtSetSecurityObject` 를 부르는 곳 전부 — PE `FF 15` 스캔, `_calls.tsv`/`.asm` 과 일치)

| caller | call / ret | 대상 | SecurityInformation | 근거 |
|---|---|---|---|---|
| `SmpCreateProtectedPrefixes` | `+0xa44/+0xa4b` | `NtOpenFile` (`+0xa1e/+0xa25`, 접근 `0x11e0000`) 로 연 **`\Device\NamedPipe\`** | `0x1c` (DACL \| SACL \| LABEL) | `.c` 에서 `local_8b0 → local_8a8 = L"\Device\NamedPipe\"` |
| `SmpCreateProtectedPrefixes` | `+0xab3/+0xaba` | `NtOpenFile` (`+0xa8d/+0xa94`) 로 연 **`\Device\mailslot\`** | `0x1c` | 같은 `.c`, `local_8a0 → local_898 = L"\Device\mailslot\"` |
| `SmpSetDumpSecurityAndAttributes` | `+0x1ed/+0x1f4` | 덤프·페이지 파일 (`NtQueryInformationFile`/`NtSetInformationFile` 이웃, caller `SmpSavePageFile`·`SmpCopyFile`) | — | `.asm` (`+0x152 RtlCreateSecurityDescriptor` … `+0x1d7 RtlSetOwnerSecurityDescriptor`) |

- 객체 디렉터리 이름을 핸들로 열어 보안을 바꾸는 줄 (`NtOpenDirectoryObject`) 은 `SmpInitializeDosDevices +0x76/+0x7d` (`\??` 쪽으로 보이는 디렉터리, 읽기 열기) 하나뿐이고 그 뒤에 `NtSetSecurityObject` 는 없다.
- **`SmpCreateSecurityDescriptors` 가 만든 DACL·SACL 이 SD 에 붙은 뒤 smss 가 다시 쓰는 곳도 없다** (Liberal 포인터 `0x32668` 의 쓰기는 `+0xea` 한 곳, 읽기는 `+0xfd +0xe74 +0xe90` 와 `SmpConfigureObjectDirectories +0xbb`, `refs`).

#### 2-2. smss 밖 (이 저장소에 있는 것 + 문자열·import 로 보이는 만큼)

- `RPC Control` 문자열이 든 System32 이진 (UTF-16 바이트 검색, `.exe`/`.dll`/`.sys` 4000여 개): `ntoskrnl.exe`, `KernelBase.dll`, `gdi32full.dll`, `rpcrt4.dll`, `combase.dll`, `RpcEpMap.dll`, `RotMgr.dll`, `usermgr.dll`, `SDFHost.dll`, `VoipRT.dll`, `fcon.dll`, `AppXDeploymentServer.dll`, `IMEPADSM.DLL`, `drivers/msrpc.sys`, `win32kfull.sys`, `smss.exe` (+ 대체 커널 `ntkrla57.exe`). 이 중 **저장소에 분석이 있는 것은 `ntoskrnl`·`kernelbase`·`gdi32full`** 뿐.

| 모듈 | `RPC Control` 이 보이는 모양 | `\RPC Control` 디렉터리 SD 를 고치나? |
|---|---|---|
| `kernelbase` | `\RPC Control` (rva `0x2a3cf8`), `%ws\%ld\BaseNamedObjects\%ws\RPC Control`, `RPC Control` — `BasepOpenShadowRpcControlDirectory +0x9b/+0x142/+0x199`, `BasepCreateAppContainerObjectDirectories +0x590` | **아님.** AppContainer·그림자 이름공간 쪽 "RPC Control" 디렉터리 (`\Sessions\N\…\RPC Control`). `NtSetSecurityObject` 는 `SetKernelObjectSecurity +0x7` 등 일반 API 몫 (`RegSetKeySecurity`, `SetFileSecurityW`, `BasepSetKernelIntegrityLabel`, `operator() +0x5c3`) |
| `gdi32full` | `\RPC Control\UmpdProxy` — `PROXYPORT +0x23b` | 아님 (ALPC 포트 이름) |
| `ntoskrnl` | 아래 질문 3 | 아님 |
| `csrsrv` | **`\RPC Control` 문자열 없음.** 그러나 **`NtSetSecurityObject` 를 `CsrSetDirectorySecurity +0x2bb` (call, rva `0x7570`) 에서 부른다.** 핸들 = `CsrObjectDirectory` (`+0x4c MOV RDI,[0x180014340]`, `+0x2b8 MOV RCX,RDI`), 정보 `4` (DACL 만, `+0x2b3 MOV EDX,4`) | **`\Windows` 는 고친다 (DACL 교체). `\RPC Control` 은 아님.** 부르는 자리: `CsrParseServerCommandLine +0x1f9/+0x200` 이 명령줄 `ObjectDirectory=` 이름으로 `NtCreateDirectoryObject` (속성 `0xc0` = 대소문자 무시 + `OBJ_OPENIF`, 서비스 세션이면 `0xd0` = + `PERMANENT`. 이미 있으면 열기만. `.c` 줄 89~92) 한 뒤 `+0x210/+0x215 CsrSetDirectorySecurity`. `ObjectDirectory=` 값이 `\Windows` 인지는 레지스트리 (`SubSystems`) 몫 `[확인 필요]` |
| `basesrv` | `ServerDllInitialization` 안 `NtCreateDirectoryObject` 4곳 (`+0xa4f +0xa98 +0xbd7 +0xedf`). `NtSetSecurityObject` import 없음 | 아님 (BaseNamedObjects 류) |
| 문자열만 있고 **분석 없음** (후보) | `rpcrt4` (`\RPC Control\`, `\RPC Control\DSEC`), `RpcEpMap` (rpcss 끝점 매퍼, `\RPC Control\DSEC` 등), `combase` (`\\RPC Control\\`), `RotMgr` (`\RPC Control\AutoRotationApiPort`), `fcon` (`\RPC Control\FconAlpcPort`), `SDFHost`, `VoipRT`, `win32kfull.sys` (`\RPC Control\UmpdProxy`), `msrpc.sys` (`\RPC Control\`), `IMEPADSM` (`\Sessions\%d\%s\RPC Control\%s`) | **`NtSetSecurityObject`/`SetSecurityInfo` 를 import 하는 것**: `AppXDeploymentServer.dll` (`NtSetSecurityObject`, 문자열 `\RPC Control` 단독), `usermgr.dll` (`SetSecurityInfo` 지연 import, 문자열 `RPC Control`·`\RPC Control`), `win32kfull.sys` (`ZwSetSecurityObject`). 나머지 (`RpcEpMap`, `combase`, `fcon`, `rpcrt4` 등) 는 SD 를 **만들지만** (`SetSecurityDescriptor*` import) 그건 ALPC 포트 같은 자식 객체용으로 보임 `[추측]`. 어느 하나가 디렉터리 SD 를 건드리는지는 **분석 안 함** |

- **결론:** smss 가 처음 붙인 SD 를 뒤에 **`\RPC Control` 에 바꾸는 코드는, 저장소에 있는 모듈과 문자열·import 로 본 범위에선 찾지 못했다.** (없다고 확정하려면 후보 모듈을 봐야 함. 특히 `AppXDeploymentServer`·`usermgr`. 둘 다 부팅 첫 순간이 아니라 서비스/로그온 뒤에 도는 것으로 보임 `[추측]`.)
- **`\Windows` 는 csrss 가 DACL 을 바꾼다** → 질문 1 의 Liberal 표는 "smss 가 처음 붙인 `\Windows` SD" 이고 부팅 뒤 값은 다르다. `\RPC Control` 은 이 표가 곧 부팅 직후 값.

**지금 우리 맞나·틀리나:** 처음 SD 만 정확히 채우면 충분해 보임 (뒤에서 고치는 곳을 못 찾았으므로). 지금 우리 (SD 안 채움) 는 틀림 — 질문 1 과 같은 이유.

---

### 질문 3. 커널이 `\RPC Control` 을 미리 만드나 — **아니오 (정적으로 보이는 범위에서)**

- `ntoskrnl.exe` 에서 `RPC Control` 이 나오는 곳은 **2곳**뿐 (UTF-16 바이트 검색):
  1. **이름 비교 표**: rva `0x1c90 ~ 0x1cd0` 의 `UNICODE_STRING` 5칸 (16 B 간격) = `Local` · `Global` · **`RPC Control`** (칸 rva `0x1cb0`, 문자열 rva `0x11890`) · `Session` · `AppContainerNamedObjects`. 읽는 코드는 **`SepValidateReferencedCachedHandles` (rva `0x8f48c0`) `+0x27e LEA RAX,[0x140001c90]`** → 번호 0..4 (`+0x275 CMP EBX,5`) 를 도는 루프에서 `+0x296 CALL RtlEqualUnicodeString` (대소문자 무시) 로 이름을 맞춰 본다. **디렉터리를 만드는 코드가 아니라 "AppContainer 토큰에 달린 캐시 핸들의 이름이 이 5개 중 하나인지" 검사.** (이 함수의 뜻은 이름·동작 모양에서 읽은 것 `[추측]`, 비교 호출은 `objdump` 로 확인.)
  2. **`\RPC Control\FconAlpcPort`** (PAGE 영역 rva `0xb00640`) — `CmFcpConnectToAlpcServer` (rva `0xa9a398`) `+0x42 LEA` 가 가리킴. **커널이 이 이름의 ALPC 서버 포트에 붙는 쪽** (서버는 `fcon.dll` 에도 같은 문자열). 즉 커널은 `\RPC Control` 이 **이미 있다고 가정**한다.
- 커널이 부팅 때 `NtCreateDirectoryObject`/`ZwCreateDirectoryObject` 를 부르는 곳 (`_calls.tsv`): `ObInitSystem +0x557/+0x55c` (`ObInitSystem.c` 의 `NtCreateDirectoryObject(&…, 0xf000f, …)`, 이름은 전역 `ObpRootDirectoryName` = 루트), `SepInitializationPhase1 +0x1b8/+0x1bd`, `ExInitializeNls +0x28/+0x2d`. 이 이름들에 `RPC Control` 은 없다 (문자열이 위 2곳뿐이라서).
- 이 저장소의 `ms/decomp/ntoskrnl`, `ms/disassemble/ntoskrnl` 에는 `RPC Control` 텍스트 **0건** (요청서 "이미 아는 것" 과 같음). 이번에 PE 바이트로만 본 것이라 새 `.asm` 은 없음.

**지금 우리 맞나·틀리나:** **맞음.** 에뮬레이터가 커널 쪽에서 `\RPC Control` 을 따로 만들어 둘 필요는 없고, smss 가 하는 일 (질문 1) 을 대신하면 된다. 다음 요청 후보로 넘길 커널 함수는 없다.

---

## 동적으로 확인해야 할 것 (Q181 에 넘길 것)

1. **부팅 직후 `\RPC Control` 의 실제 SD** (`!object \RPC Control` 로 객체 → 헤더의 `SecurityDescriptor` 칸 (`EX_FAST_REF`, 아래 4비트 마스크 `[추측]`) → `!sd`/바이트). 위 1-5·1-7 표와 대조할 것:
   - **소유자·그룹** 이 어느 SID 인지 (smss 가 안 채움 → 커널이 채운 값).
   - **마스크 값**: `0xE0000000`·`0x10000000` 이 그대로인지, Directory 구체 권한 (예: `0x20003`, `0xf000f` 꼴) 으로 바뀌었는지.
   - **`AclSize`**: DACL `0xb8`/SACL `0x20` 유지인지, 사용분 (`0x9c`/`0x1c`) 으로 줄었는지.
   - **Control** 값 (`0x8000 | 0x14` + 그 밖 비트?), 라벨 ACE (type `0x11`, 마스크 `1`, `S-1-16-4096`).
   - **고유 SID 가 섞인 ACE** 가 있는지 (예: 로그온 세션 SID, 서비스 SID). 정적으론 이 SD 에 그런 ACE 는 없다 — 뒤에서 누가 붙였다면 여기서 드러남.
2. **같은 시점의 `\Windows` SD** — smss 표 (Liberal, 같은 포인터) 와 csrss 가 DACL 을 바꾼 뒤 값의 차이 (`CsrSetDirectorySecurity`).
3. **타깃 VM 레지스트리 값**: `ProtectionMode` 가 1 인지, `ObjectDirectories` 가 정확히 `\Windows`, `\RPC Control` 두 줄뿐인지 (이름이 더 있으면 그건 Primary SD).
4. **자식 객체 상속**: `\RPC Control` 안 ALPC 포트 (`\RPC Control\epmapper` 등) 의 SD 에 위 ACE 3~6 (플래그 `0x0b`) 이 어떻게 들어가는지 — rpcrt4 가 `0x17` 으로 묻는 건 `\RPC Control` 자신이라 급하진 않음.
5. **시간 순서**: `\RPC Control` 이 부팅 중 언제 만들어지는지 (smss 가 `SmpLoadDataFromRegistry` 안에서, `SmpOpenKnownDllsHandles` 보다 **앞**) — 사슬은 정적으로 확정, 실제 시각은 동적.
6. **`NtCreateDirectoryObject` 가 `OBJ_OPENIF` 로 이미 있는 디렉터리를 열기만 하는지**: `\RPC Control` 이 smss 전에 이미 있을 일은 정적으로 못 찾았음 (질문 3), 그래도 맨 처음 SD 가 smss 몫인지 확인.

- 추천 멈춤점 (부팅 맨 앞이라 어려움, 가능하면):
  - `smss!SmpConfigureObjectDirectories` (함수 입구, rva `0x143d0`) — 이름마다 한 번씩 오는지, `RDI` (SD 포인터) 가 Liberal 몸통 `smss+0x32680` 인지.
  - `ntdll!NtCreateDirectoryObject` 의 `smss` 호출 (ret = `smss!SmpConfigureObjectDirectories+0xf8`, 3번째 인자 `OBJECT_ATTRIBUTES.SecurityDescriptor` 를 덤프).
  - 현실적인 대안: 부팅 끝난 뒤 VM 에서 객체 SD 덤프 (위 1번).

---

## 다음 요청 후보

1. **ntoskrnl `SeAssignSecurityEx2`** (rva `0x43d980`, 133 B; `SeAssignSecurityEx` 는 이 함수 호출 한 줄) 와 그 아래 — **소유자·그룹을 토큰에서 어떻게 채우나**, **ACE 의 GENERIC 비트를 구체 권한으로 바꾸나**, `AclSize` 를 그대로 두나. 에뮬레이터가 `\RPC Control` 의 객체 SD 를 정확히 "커널이 저장한 꼴" 로 만들 때 필요. (ntoskrnl 은 sast/93 이 끝난 뒤.)
2. **csrsrv `CsrSetDirectorySecurity`** (rva `0x7570`, 859 B) — `\Windows` 가 csrss 뒤 어떤 DACL 이 되나 (질문 2 의 마무리, 짐 지도의 `\Windows` 칸). 이미 analyze 된 모듈이라 export 1개로 끝남.
3. **낮음: `AppXDeploymentServer`·`usermgr`·`win32kfull` 의 `\RPC Control` 문자열 쪽** — 디렉터리 SD 를 고치는지 확인 (새 모듈 analyze 필요). 정적 증거가 약해서 Q181 의 부팅 뒤 SD 가 smss 표와 다르면 그때.
4. **낮음: ntdll `RtlQueryRegistryValuesEx`** — `REG_MULTI_SZ` 값을 콜백에 **문자열 하나씩** 주는지 통째로 주는지 (콜백이 통째 목록을 도는 모양이라 통째로 받는다고 보지만 확인 안 함). SD 선택 결과엔 영향 없음.
5. **Q180c (rpcrt4)** — 이미 요청 대기열에 있음 (`0x17` 을 묻는 쪽).

## 판정

**분석성공** — 질문 1 (만드는 함수·표·콜백·ACE 7+라벨 1·`ProtectionMode` 갈래·이름 비교) 은 `.asm` 줄로 전부 닫았고, 질문 2 는 smss 안 확정 + smss 밖은 "저장소·문자열·import 로 보이는 범위" 로 답했으며, 질문 3 은 "아니오 (커널은 이름 비교와 클라이언트로만 씀)". 남은 건 (소유자·그룹·마스크 변환·`AclSize`) 으로 **동적/커널 몫** 이라 의도한 것. 확정 못 한 것은 smss 밖 후보 모듈 (`AppXDeploymentServer`·`usermgr`) 이 디렉터리 SD 를 고치는지.
