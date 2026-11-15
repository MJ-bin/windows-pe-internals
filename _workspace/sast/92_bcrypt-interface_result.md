# 92 bcrypt-interface — 정적 분석 결과

- 요청서: `_workspace/sast/92_bcrypt-interface.md` (Q178a = Q178 ①, run94 남은 정적)
- 스크립트 문제: **없음.** (`export` 2번, 로그에 `SCRIPT ERROR` 0줄)
- 모듈 / 바이너리: bcrypt (`ms/bin/.../System32/bcrypt.dll`, x64, 베이스 `0x180000000`), bcryptprimitives (같은 폴더, **PE 내보내기 표만** 읽음)
  - PDB GUID+Age (bcrypt) = `5F1077ED8D3ABC83AD7D378F94EA41901` (sast/88·90 과 같음). analyze **다시 안 함** (`.gpr` + `_index.tsv` + `SetPdb:` 줄 다 있음).
  - **PDB 에 이 구조체들의 정의가 없다.** 칸 이름 (`OpenAlgorithmProvider` 등) 은 공개 헤더 기억 `[추측]`, 오프셋·크기·검사는 전부 코드에서 읽음 `[정적]`.
- 뽑은 함수 (`ms/disassemble/bcrypt/*.asm`, `ms/decomp/bcrypt/*.c`), 전부 depth 0:
  - 요청서 대상: `_LoadInterface` · `ValidateFunctionTable` · `_LoadImage` · `LoadProviderEx` · `VsmLoadInterface` · `BCryptOpenAlgorithmProvider`
  - 덧붙임 (질문 5 때문, 작게): `_FreeImage` (22바이트, `FreeLibrary` 여부), `UnloadProvider` (`.asm` 만 뽑음, 본문은 안 읽음), `_GetImagePath` (sast/90 에서 읽은 것 다시 뽑음)
  - 새로 읽은 줄: `_LoadInterface` 전체, `ValidateFunctionTable` 전체, `_LoadImage` 전체, `LoadProviderEx` 전체, `BCryptOpenAlgorithmProvider +0x39c ~ +0xd1c` (sast/90 은 `+0x518` 앞까지만 읽음)
- bcryptprimitives: `objdump -p` + 임시 파이썬으로 내보내기 표 이름·rva 만 (본문 안 읽음). `ms/disassemble/bcryptprimitives/` 에 `Get…Interface` 는 아직 없음 (sast/87 은 `DllMain` 만).
- 버전 확인: **일치.** (bcrypt 는 "알려진 사실" 표에 없음. 다른 근거로 맞춤)
  - 요청서가 준 `_index.tsv` 값 (`_LoadImage 0x2c28`/421, `ValidateFunctionTable 0xff60`/1244, `_LoadInterface 0x10810`/2222, `VsmLoadInterface 0x199b0`/957) 이 그대로 나옴.
  - sast/90 의 `BCryptOpenAlgorithmProvider +0x518/+0x51d LoadProviderEx`, `+0x70b/+0x710 ValidateFunctionTable`, `+0x851`·`+0x883` 간접 호출, `_LoadInterface +0x130/+0x137 GetProcAddress` 가 `_calls.tsv` 와 `.asm` 에서 같은 오프셋.
  - 지도 (`entry_nodes.tsv`/`entry_edges.tsv`): 아래 "지도에서 옮긴 것".

---

## 한눈에 (먼저 읽기)

1. **번호 ↔ 이름 (질문 1).** `dwInterface` `1..8` = `GetCipherInterface` · `GetHashInterface` · `GetAsymmetricEncryptionInterface` · `GetSecretAgreementInterface` · `GetSignatureInterface` · `GetRngInterface` · `GetKeyDerivationInterface` · `GetKeyEncapsulationInterface`. (`_LoadInterface +0x4e` 의 점프 표 rva `0x110cc`.) 그 밖에 `0x10001`·`0x10002`·`0x10004` 이름도 코드에 있지만 `BCryptOpenAlgorithmProvider +0x3b7` 이 `1..8` 만 통과시켜서 이 길에선 안 닿는다.
2. **bcryptprimitives 는 `1..8` 이름 8개를 다 내보낸다.** (+ `MSCryptConvertRsaPrivateBlobToFullRsaBlob` · `ProcessPrng` · `ProcessPrngGuid`.) `0x1000x` 쪽 이름 3개는 없다 — 상관없음. **질문 1 의 이름 문제는 "있다" 로 닫힘.**
3. **부르는 모양.** 번호 6 (난수) 만 `Get…Interface(pszProvider, &표포인터, 0)` (인자 3개), 나머지 `1..5·7·8` 은 `Get…Interface(pszProvider, pszFunction, &표포인터, 0)` (인자 4개). 간접 호출은 CFG 를 거친다 (`_guard_dispatch_icall` 썽크). 음수 status 는 **그대로 위로** 올라간다. 양수·0 이면 **표 머리 첫 2바이트 ≥ 1** 을 한 번 더 본다 (0 이면 `0xc00000bb`).
4. **표는 복사한다.** 받은 표 포인터는 핸들에 안 둔다. 번호별 크기 (`0x30 ~ 0x70`) 만큼 **핸들 `+0x30`** 으로 `memcpy` 한다 (`LoadProviderEx +0x247`). 이후 `BCryptGenRandom` 등은 `핸들 + 0x30 + 표 오프셋` 을 부른다 (`BCryptGenRandom +0x6b MOV RAX,[RAX+0x58]` = 난수 표 `+0x28`).
5. **`ValidateFunctionTable` 은 머리 (버전·크기) 를 안 본다.** 번호별로 **표 안 함수 칸이 NULL 이 아닌지만** 본다 (번호 1·5·8 = `+8 ~ +0x58` 11칸, 2 = `+8 ~ +0x48` 9칸, 3 = `+8 ~ +0x68` 13칸, 4 = `+8 ~ +0x60` 12칸, 6 = `+8 ~ +0x28` 5칸, 7 = `+8 ~ +0x50` 10칸). 하나라도 NULL 이면 **`0xc0000008`**. 불리는 자리는 `BCryptOpenAlgorithmProvider +0x70b` (= **`_LoadInterface` 안이 아니다**).
6. **열기 함수 = 표 `+8`.** `BCryptOpenAlgorithmProvider +0x851` 이 `표+8` 을 `(&핸들[+0x18], 호출자의 알고리즘 이름, 호출자의 플래그)` 로 부른다. status ≥ 0 이면 성공. 예외 한 개: status `0xc000a001` + 번호 2 + 플래그에 `0x8` 만 → `0x8` 을 빼고 `+0x883` 에서 한 번 더 부름.
7. **sast/90 4-b 정정 (중요).** 4-b 표 `0x28 pszProvider` 를 "로그에만 씀" 이라 적었는데 **틀렸다.** 그 칸은 `Get…Interface` 의 **첫 인자**로 그대로 간다 (`_LoadInterface +0x1d2`, 번호 6 은 `+0x338`). `0x20 pszFunction` 도 로그가 아니라 **둘째 인자 (알고리즘 이름)** 로 간다 (`+0x1cb`). 반대로 "핸들에 복사되는 알고리즘 이름" 은 KsecDD 출력이 아니라 **호출자의 `pszAlgId`** (`BCryptOpenAlgorithmProvider +0x4bb wcsncpy_s` 의 원본 = `R12` = `+0x5f MOV R12,RDX`).
8. **최소 조건은 거의 "진짜 DLL 이 알아서 맞춘다".** 공급자 DLL 이 진짜 `bcryptprimitives.dll` 이므로 표 머리·NULL 칸·열기 status 는 진짜 코드가 채운다. **에뮬레이터가 맞춰야 할 것**: (a) KsecDD 답의 번호·이름·공급자 이름 (b) `LoadLibraryExW` → 진짜 DLL attach (c) `GetProcAddress` 이름 찾기 (d) **CFG 간접 호출을 통과시키기** (`__guard_dispatch_icall_fptr` 의 값 + 대상 유효 표시) (e) `Get…Interface`·열기·`GetProperty` 가 진짜 코드로 돌아가는 것. 아래 "최소 조건 표".

---

## 지도에서 옮긴 것 (`ms/disassemble/bcrypt/_map/entry_*`, 뿌리 `entry`)

| 함수 | rva | 크기 | 깊이 | caller 수 | 부르는 자리 |
|---|---|---|---|---|---|
| `BCryptOpenAlgorithmProvider` | `0x9410` | — | 7 | 3 | (sast/90) |
| `LoadProviderEx` | `0x8840` | 1365 | 8 | 1 | `BCryptOpenAlgorithmProvider +0x518/+0x51d` |
| `ValidateFunctionTable` | `0xff60` | 1244 | 8 | 2 | `BCryptOpenAlgorithmProvider +0x70b/+0x710`, `VsmOpenAlgorithmProvider +0x208/+0x20d` |
| `_LoadInterface` | `0x10810` | 2222 | 9 | 1 | `LoadProviderEx +0xee/+0xf3` |
| `_LoadImage` | `0x2c28` | 421 | 9 | 1 | `LoadProviderEx +0x121/+0x126` |
| `VsmLoadInterface` | `0x199b0` | 957 | 9 | 1 | `VsmOpenAlgorithmProvider +0x16d/+0x172` |

- `VsmOpenAlgorithmProvider` 자체는 `BCryptOpenAlgorithmProvider +0x1f4/+0x1f9` 한 곳이 부른다 (`_calls.tsv`).
- 사슬: `BCryptOpenAlgorithmProvider` → `LoadProviderEx` → (`_LoadImage` →) `_LoadInterface` (→ 공급자 DLL `Get…Interface`). 돌아와서 `BCryptOpenAlgorithmProvider +0x70b` `ValidateFunctionTable`, `+0x851` 열기.

---

## 질문별 답 [정적]

### 1. `dwInterface` 번호 ↔ 내보내기 이름 (`_LoadInterface`)

고르는 곳은 `_LoadInterface` 앞쪽 (`+0x19 ~ +0xb8`, `+0xba ~ +0x126`) 의 `LEA RDX,[이름]` → 모두 `+0x12d` 로 모여 `MOV RCX,R8` (R8 = 공급자 DLL 핸들) 후 `+0x130 CALL [GetProcAddress]`. 번호가 `1..8` 이면 `+0x38 DEC EAX; +0x3a CMP EAX,7; +0x43 MOV ECX,[R12+RAX*4+0x110cc]` (점프 표 rva `0x110cc`, 8칸) → `+0x4e JMP RCX`.

| `dwInterface` | 이름 | 문자열 rva | 고르는 줄 (`LEA RDX`) | bcryptprimitives 가 내보내나 |
|---|---|---|---|---|
| 1 | `GetCipherInterface` | `0x20010` | `+0x54` | **O** (rva `0xad50`, 128 B) |
| 2 | `GetHashInterface` | `0x20028` | `+0x60` | **O** (`0xacb0`, 140 B) |
| 3 | `GetAsymmetricEncryptionInterface` | `0x20040` | `+0x6c` | **O** (`0x48800`, 133 B) |
| 4 | `GetSecretAgreementInterface` | `0x20068` | `+0x78` | **O** (`0x46210`, 207 B) |
| 5 | `GetSignatureInterface` | `0x20088` | `+0x84` | **O** (`0x4c6a0`, 283 B) |
| 6 | `GetRngInterface` | `0x200a0` | `+0x90` | **O** (`0x48b90`, 13 B) |
| 7 | `GetKeyDerivationInterface` | `0x200b0` | `+0x9c` | **O** (`0x48c00`, 228 B) |
| 8 | `GetKeyEncapsulationInterface` | `0x200d0` | `+0xa8` | **O** (`0x5f290`, 126 B) |
| `0x10001` | `GetKeyStorageInterface` | `0x200f0` | `+0xb1` (`+0x36 JZ`) | X (이 길에선 안 닿음) |
| `0x10002` | `GetSChannelInterface` | `0x20128` | `+0x126` (`+0xbf JZ`) | X (〃) |
| `0x10004` | `GetKeyProtectionInterface` | `0x20108` | `+0x11d` (`+0xc4 JZ`) | X (〃) |
| 그 밖 | — | — | `+0xc6 MOV EBX,0xc00000bb` (WPP 줄 `0x768`) | — |

- 이름 확인: bcrypt 의 `.rdata` 바이트를 읽음 (ANSI). 점프 표 8칸 값 (`0x10864 … 0x108b8`) 이 위 `LEA` 줄과 순서대로 맞음.
- bcryptprimitives 내보내기 11개 (이름 순 아님, 번호 = 내보내기 번호): `GetAsymmetricEncryptionInterface`(1) · `GetCipherInterface`(2) · `GetHashInterface`(3) · `GetKeyDerivationInterface`(4) · `GetKeyEncapsulationInterface`(5) · `GetRngInterface`(6) · `GetSecretAgreementInterface`(7) · `GetSignatureInterface`(8) · `MSCryptConvertRsaPrivateBlobToFullRsaBlob`(9) · `ProcessPrng`(10) · `ProcessPrngGuid`(11). 이름 있음·포워더 없음.
- 질문 1 표는 Q178b ③ (알고리즘별 번호) 의 "번호 → 이름" 쪽을 덮는다. **"알고리즘 이름 → 번호"는 커널 몫** (KsecDD 출력 `+0x18`), 이번엔 안 봄 `[공백]`.
- **지금 우리:** 맞음. 우리 최소 답 (`AES`=1, hash=2, `RNG`=6) 은 전부 `1..8` 안이고 그 번호의 이름이 진짜 DLL 에 다 있다. 우리 쪽에서 이름을 만들 필요 없음 (이름은 bcrypt 안 문자열). 틀릴 수 있는 곳은 "알고리즘 ↔ 번호가 안 맞을 때" (공급자 열기가 실패) — 질문 6.

### 2. `Get…Interface` 를 어떻게 부르나

`_LoadInterface(HMODULE hModule, 공급자 칸* pRef, (안 읽음), 표포인터* pOut)` — `RCX=hModule`, `RDX=공급자 칸` (`R15`), `R9=pOut` (`R14`). `R8` 은 `+0x28 MOV R8,RCX` 로 덮어써서 3번째 인자는 안 쓴다.

**① 이름 찾기** (질문 1) → `+0x13c MOV R10,RAX; +0x13f TEST RAX,RAX; +0x142 JNZ`. **NULL 이면 `+0x144 MOV EBX,0xc0000139`** (WPP 줄 `0x773`) 로 바로 끝.

**② 호출** — 번호로 한 번 더 갈라진다 (`+0x188 MOV EAX,[R15]` … `+0x1ba MOV ECX,[R12+RAX*4+0x110ec]`, 점프 표 rva `0x110ec`). 부르는 레지스터 (`RAX` = ① 의 함수 주소, `CALL 0x18001d010` = CFG 썽크):

| 번호 | 호출 줄 (call / ret) | 인자 (칸 순서) |
|---|---|---|
| 1 Cipher | `+0x1dc/+0x1e1` | `RCX=[R15+0x10]` (공급자 이름) · `RDX=[R15+8]` (알고리즘 이름 `pszFunction`) · `R8=R14` (받을 표 포인터 `**`) · `R9=0` (플래그) |
| 2 Hash | `+0x225/+0x22a` | 같음 |
| 3 AsymEnc | `+0x26e/+0x273` | 같음 |
| 4 SecretAgreement | `+0x2b7/+0x2bc` | 같음 |
| 5 Signature | `+0x300/+0x305` | 같음 |
| **6 Rng** | **`+0x345/+0x34a`** | **`RCX=[R15+0x10]` · `RDX=R14` · `R8=0`** (알고리즘 이름 없음, 인자 3개) |
| 7 KeyDerivation | `+0x38e/+0x393` | 4개 (1 과 같음) |
| 8 KeyEncapsulation | `+0x3d7/+0x3dc` | 4개 |
| `0x10001` / `0x10004` / `0x10002` | `+0x41c` / `+0x4ac` / `+0x4ee` | 3개 (Rng 와 같은 모양) — 이 길엔 안 닿음 |

- 칸 이름 (공개 헤더 기억 `[추측]`): `[R15+0x10]` = `pszProviderName`, `[R15+8]` = `pszAlgorithmName`, `R14` = `ppFunctionTable`, 마지막 0 = `dwFlags`. 공급자 칸 오프셋은 sast/90 표 (`+0x08 pszFunction`, `+0x10 pszProvider`).
- **status 를 보는 법:** `+0x1e5 TEST EAX,EAX; +0x1e7 JNS +0x58b` (번호마다 같은 모양). **음수면** `EBX=EBP=EAX` → WPP 로그 (줄 `0x5ca` 등) → `+0x573 MOV EAX,EBX` → 그대로 반환. 호출한 `LoadProviderEx +0xf5 TEST EAX,EAX; +0xf7 JZ` 는 0 이 아니면 `DebugTraceError` (`+0x10f`, 줄 `0x9ba`) 후 실패 길로 간다: 새로 올린 DLL 이면 `+0x514 _FreeImage` (→ `FreeLibrary`, `g_hInstance` 이거나 NULL 이면 안 함, `_FreeImage +0xb ~ +0x12`), `*출력 = 0` (`+0x519`).
- **양수·0 이면** `+0x58b` 로 와서 세 번째 갈림 (`+0x5c0 MOV ECX,[R12+RAX*4+0x1110c]`, 점프 표 rva `0x1110c`) → **표 머리 검사** (질문 3-a) → 통과하면 `EBP = 0` 을 돌려준다 (**성공 반환은 항상 0**, 공급자가 `1` 을 돌려줘도 0 으로 바뀜: `+0x5a2 XOR EBP,EBP`).
- **`*ppFunctionTable` 이 NULL 이면 크래시.** 머리 검사가 `MOV RAX,[R14]` 뒤 바로 `CMP CX,[RAX]` (`+0x5d1 ~ +0x5d9`). 부르기 전에 `[R14]` 를 0 으로 안 만든다 (`LoadProviderEx` 지역 변수 `[RSP+0x48]`, 초기화 안 함). 그러니 공급자는 **성공하면 반드시 표 포인터를 써 줘야** 한다 (진짜 DLL 은 그렇게 함 `[추측]`, 본문은 안 읽음).
- **표 포인터를 어디에 두나:**
  - `_LoadInterface` 는 포인터를 `LoadProviderEx` 지역 변수 `[RSP+0x48]` 에만 돌려준다.
  - `LoadProviderEx +0x237 MOV RDX,[RSP+0x48]; +0x23c MOV RCX,[RSP+0xa0]; +0x244 MOV R8D,크기; +0x247 CALL memcpy` — 목적지 `[RSP+0xa0]` = `LoadProviderEx` 의 5번째 인자 = **핸들 `+0x30`** (`BCryptOpenAlgorithmProvider +0x4e9 LEA R9,[R15+0x30]; +0x508 MOV [RSP+0x20],R9`).
  - 복사 크기는 번호로 (`LoadProviderEx +0x16a ~ +0x230`, 점프 표 rva `0x8d9c`): 1→`0x60`, 2→`0x60`, 3→`0x70`, 4→`0x68`, 5→`0x60`, 6→`0x30`, 7→`0x58`, 8→`0x60` (`0x10001`→`0x100`, `0x10004`→`0x28`, `0x10002`→`0x188`).
  - 그러니 **원본 표는 복사 뒤 안 쓴다.** 표는 공급자 DLL 의 정적 데이터에 있어도 되고, 복사할 크기만큼 **읽을 수 있어야** 한다.
  - `LoadProviderEx` 의 4번째 인자 = 핸들 `+0x28` 에 **"올린 공급자" 노드** 포인터 (`+0x44e MOV [R13],RBX`).
- 핸들 칸 (이번에 새로 보인 것, `0x3c8` 바이트): `+0x00 = 0x3c8`, `+0x04 = 0x55555551`, `+0x08` 플래그 (HMAC 재시도 뒤 `|= 8`), `+0x0c` HashBlockLength (번호 2), `+0x10 = 1`, **`+0x18` 공급자 쪽 핸들 (열기 함수가 써 줌)**, `+0x24 dwInterface`, `+0x28` 올린 공급자 노드, **`+0x30 ~` 표 복사**, `+0x1b8` PaddingSchemes (번호 3), `+0x1bc` 알고리즘 이름 (호출자의 `pszAlgId`, UTF-16 최대 `0x104` 글자).
- **지금 우리:** `Get…Interface` 를 우리가 부르는 게 아니다. 진짜 DLL 이 진짜 코드로 돈다. 우리 몫은 (a) **KsecDD 출력의 `pszProvider` (`+0x28` 칸) 가 진짜 DLL 의 첫 인자로 간다** — 4-b 에서 권한 `-1 (NULL)` 이 진짜 DLL 에서 통하는지는 DLL 몫이라 이번엔 `[공백]` (질문 6 / 다음 요청 1). 안전한 쪽은 진짜 값 `L"Microsoft Primitive Provider"` 를 넣는 것 `[추측]`. (b) 아래 CFG.

### 3. `ValidateFunctionTable` 이 무엇을 검사하나

**함수 모양** (`ms/disassemble/bcrypt/ValidateFunctionTable.asm`): `ValidateFunctionTable(DWORD dwInterface /*ECX*/, 표 복사본* /*RDX*/, 출력1* /*R8*/, 출력2* /*R9*/, 출력3* /*[RSP+0x70]*/, 출력4* /*[RSP+0x78]*/)`. 번호가 `1..8` 이 아니면 `+0xd JA +0x48e` → `0xc0000008` (줄 `0x56e`). 1..8 이면 `+0x1a MOV ECX,[R10+RCX*4+0x10444]` (점프 표 rva `0x10444`, 8칸) → `+0x25 JMP RCX`.

**불리는 자리 = `BCryptOpenAlgorithmProvider +0x70b` (call), `+0x710` (ret).** `_LoadInterface` 안이 아니다. 그 직전 `+0x682 ~ +0x6e3` 이 **핸들 `+0x30` 의 `0x188` 바이트를 지역 `[RBP-0x10]` 로 복사** (`RDX = LEA [RBP-0x10]`, `+0x6fe`). 인자: `ECX=[RSP+0x78]` (번호), `R8=&[RBP-0x28]`, `R9=&[RBP-0x50]`, `[RSP+0x20]=&[RBP-0x48]`, `[RSP+0x28]=&[RBP-0x40]`.

**표 머리 (버전·크기) 는 이 함수가 안 본다.** 머리 검사는 `_LoadInterface` 쪽 (3-a).

#### 3-a. 머리 검사 (`_LoadInterface`, 공급자가 준 원본 표)

`+0x5d1 ~ +0x7cb` 번호마다 같은 모양: `MOV RAX,[R14]; MOV ECX,1; CMP CX,word ptr [RAX]; JBE +0x619` — **표의 첫 2바이트 (헤더 `Version` 의 앞 `USHORT`, 공개 헤더 `MajorVersion` `[추측]`) ≥ 1 이어야** 통과. 0 이면 `MOV EBP,0xc00000bb` (WPP 줄 `0x540`·`0x54a`·`0x554`·`0x55e`·`0x568`·`0x572`·`0x57c`·`0x586`, 번호 1~8 순서) 로 실패. 표 크기 칸·두 번째 `USHORT` (Minor) 는 안 본다. CMP 줄: 번호 1 `+0x5d9`, 2 `+0x64b`, 3 `+0x671`, 4 `+0x69a`, 5 `+0x6cf`, 6 `+0x704`, 7 `+0x739`, 8 `+0x76e`.

#### 3-b. 꼭 NULL 이 아니어야 할 칸 (`ValidateFunctionTable`, 표 안 오프셋)

표 안 함수 칸은 `+8` 부터 8바이트씩 (`+0` 은 머리). 아래 "비교"는 `TEST`/`CMP …,0` 줄.

| 번호 | 이름 | 복사 크기 | NULL 이면 안 되는 칸 | 검사 안 하는 칸 | 실패 줄 (`MOV EBX,0xc0000008`) | WPP 줄 |
|---|---|---|---|---|---|---|
| 1 | Cipher | `0x60` | `+8 +0x10 +0x18 +0x20 +0x28 +0x30 +0x38 +0x40 +0x48 +0x50 +0x58` (11칸) | — | `+0x88` (`+0x2b ~ +0x82`) | `0x4df` |
| 2 | Hash | `0x60` | `+8 +0x10 +0x18 +0x20 +0x28 +0x30 +0x38 +0x40 +0x48` (9칸) | `+0x50`, `+0x58` | `+0x117` (`+0xbb ~ +0x111`) | `0x4f2` |
| 3 | AsymEnc | `0x70` | `+8 ~ +0x68` 13칸 전부 | — | `+0x1be` (`+0x14a ~ +0x1b8`) | `0x509` |
| 4 | SecretAgreement | `0x68` | `+8 ~ +0x60` 12칸 전부 | — | `+0x254` (`+0x1f1 ~ +0x24e`) | `0x51e` |
| 5 | Signature | `0x60` | `+8 ~ +0x58` 11칸 전부 | — | `+0x2e4` (`+0x287 ~ +0x2de`) | `0x532` |
| 6 | Rng | `0x30` | `+8 +0x10 +0x18 +0x20 +0x28` (5칸) | — | `+0x350` (`+0x317 ~ +0x34a`) | `0x540` |
| 7 | KeyDerivation | `0x58` | `+8 ~ +0x50` 10칸 전부 | — | `+0x3da` (`+0x383 ~ +0x3d4`) | `0x553` |
| 8 | KeyEncapsulation | `0x60` | `+8 ~ +0x58` 11칸 전부 | — | `+0x466` (`+0x40d ~ +0x464`) | `0x567` |

- "복사 크기" = `LoadProviderEx` 가 핸들로 복사하는 바이트 수 (질문 2). 표 크기와 마지막 검사 칸이 번호마다 딱 맞는다 (번호 2 만 마지막 2칸 `+0x50`·`+0x58` 을 비워 둠).
- **출력으로 꺼내는 칸** (검사와 같이 `ValidateFunctionTable` 이 바깥 지역에 써 줌): `[R8] ← 표+8`, `[R9] ← 표+0x20`, `[출력3] ← 표+0x18`, `[출력4] ← 표+0x10` (**번호 2·3 만**; 나머지는 `0` 으로 남음 — `BCryptOpenAlgorithmProvider +0x66 MOV [RBP-0x40],RCX` 초기값).
- 칸 뜻 (공개 헤더 기억 `[추측]`, **부르는 모양은 코드로 확인 `[정적]`**):

| 표 칸 | 이름 (추측) | 부르는 자리 | 인자 (코드로 확인) |
|---|---|---|---|
| `+8` | `OpenAlgorithmProvider` | `BCryptOpenAlgorithmProvider +0x851`, `+0x883` | `(&핸들[+0x18], 호출자 pszAlgId, 호출자 플래그)` |
| `+0x10` | `GetProperty` | `+0xb19` (번호 2), `+0xbcd` (번호 3) | `(핸들[+0x18], 속성 이름, 출력 버퍼, 4, &길이, 0)` |
| `+0x18` | `SetProperty` | `+0xcc7` (속성 수 `cProperties` 만큼 반복) | `(핸들[+0x18], 이름, 값 포인터, 값 크기, 0)` |
| `+0x20` | `CloseAlgorithmProvider` | `+0xc31` (뒤처리) | `(핸들[+0x18], 0)` |
| `+0x28 ~` | 알고리즘별 (번호 6 이면 난수 만들기: `BCryptGenRandom +0x75`) | 다른 `BCrypt*` 함수 | 이번 범위 밖 |

#### 3-c. 실패 status 표 (이 사슬)

| status | 자리 | 조건 |
|---|---|---|
| `0xc0000008` | `ValidateFunctionTable +0x88 / +0x117 / +0x1be / +0x254 / +0x2e4 / +0x350 / +0x3da / +0x466` | 번호별 NULL 금지 칸 (3-b) |
| `0xc0000008` | `ValidateFunctionTable +0x48e` | 번호가 `1..8` 이 아님 (이 길에선 `BCryptOpenAlgorithmProvider +0x3b7` 이 먼저 걸러서 안 닿음) |
| `0xc00000bb` | `_LoadInterface +0x5de` 등 (3-a) / `+0xc6` (번호 이름 없음) / `+0x464`·`+0x7e0` | 표 머리 첫 2바이트 `= 0` / 번호가 정해진 11개 밖 |
| `0xc0000139` | `_LoadInterface +0x144` | `GetProcAddress` 가 NULL |
| `0xc0000135` / `0xc000007b` | `_LoadImage +0x18c` / `+0x176` | `LoadLibraryExW` 실패 (`GetLastError == 0x7e` 면 `0xc0000135`, 아니면 `0xc000007b`) |
| (공급자가 준 음수) | `_LoadInterface +0x1e7` 등 | `Get…Interface` 가 음수 |
| `0xc00000e5` | `LoadProviderEx +0x37` | `g_fLoaderInitialized == 0` (sast/90) |
| `0xc000000d` | `LoadProviderEx +0x1d3` | `LoadProviderEx` 안 번호 갈림이 `1..8`·`0x10001`·`0x10002`·`0x10004` 밖 (`+0x3b7` 이 먼저 걸러서 안 닿음) |
| `0xc000000d` | `LoadProviderEx +0x4d9` (`+0x2fa JZ +0x493`, `+0x329 JZ +0x498`) | 노드를 새로 만들 때 (`+0x2e9 ~ +0x330`) 이미지 이름 포인터가 NULL, 또는 `0x800` 글자 안에 NUL 없음 (이미지 칸 자체가 NULL 이면 그 앞 `_LoadImage +0x2c` 에서 이미 널 역참조 — sast/90 4-d) |
| `0xc0000017` | `LoadProviderEx +0x37a` | 노드 할당 실패 |
| `0xc000009a` / `0xc0000095` | `LoadProviderEx +0x261` / `+0x457` | 참조 수 오버플로 / 길이 계산 오버플로 |

#### 3-d. 검사 순서 (공급자 하나, `BCryptOpenAlgorithmProvider` 안)

1. `+0x518/+0x51d` `LoadProviderEx` → 안에서 (`g_pLoadedProviderList` 에 없으면 `_LoadImage`) → **`_LoadInterface`** (이름 찾기 → `Get…Interface` → **머리 검사 3-a**) → **`memcpy` 로 핸들 `+0x30` 에 복사**.
2. `+0x539 TEST R15D; +0x53c JNS +0x682` → 핸들 `+0x30` 에서 `0x188` 바이트 복사 (`+0x682 ~ +0x6e3`).
3. **`+0x70b/+0x710` `ValidateFunctionTable`** (3-b).
4. `+0x713 TEST EAX,EAX; +0x715 JNS +0x83a` → **`+0x851/+0x856` 열기 호출** (질문 4).
5. 열기 성공 (`+0x892 JNS +0xa86`) → 번호 2 면 `GetProperty("HashBlockLength")` (`+0xb19`), 번호 3 이면 `GetProperty("PaddingSchemes")` (`+0xbcd`) (문자열: rva `0x1f930`, `0x1f950`) → `cProperties` 개수만큼 `SetProperty` (`+0xcc7`, 반환 안 봄) → `*phAlgorithm = 핸들` (`+0xce7 MOV [RCX],RAX`), 반환 0 (`+0xd19 XOR R15D,R15D`).
- **실패했을 때 다음 공급자로 가나:** "시스템 기본 공급자" 모드일 때만 (`g_fLoadCNGDone != 0` 이고 `pszImplementation` 이 NULL → 지역 `[RSP+0x40] = 1`: `+0x201 CMP [g_fLoadCNGDone],EBX; +0x216 ~ +0x229 MOV byte [RSP+0x40],1`).
  - `LoadProviderEx` 음수 (`+0x539 JNS` 안 탐) 와 `ValidateFunctionTable` 음수 (`+0x715 JNS` 안 탐): 로그 (`+0x64d ~ +0x662`) 뒤 `+0x66e CMP byte [RSP+0x40],1; +0x673 JNZ +0xc36` (아니면 핸들 free 하고 실패 반환), 맞으면 `+0x67d JMP +0xa12` → 루프 `+0x500` 으로 다음 공급자.
  - 열기 음수 (`+0x892 JNS` 안 탐): `+0x9e2 CMP byte [RSP+0x40],1; +0x9e7 JNZ +0xc36`, 맞으면 `+0x9f4 UnloadProvider` 후 다음 공급자 (`+0xa12`).
  - `GetProperty` 음수 (`+0xb23 JNS`, `+0xbd7 JNS` 안 탐): 다음 공급자 시도 **없이** `Close` (`+0xc31`) 부르고 `+0xc36` 로 실패 반환.
- **지금 우리:** 진짜 DLL 이므로 표는 진짜 표라 통과 `[추측]`. 우리가 확인할 것은 표 안 값이 NULL 없이 **채워졌나** 정도 (동적 1번). 틀릴 수 있는 곳: 에뮬레이터가 DLL 의 **재배치·가져오기 주소 고치기 (IAT)** 를 안 해서 표 칸이 `0` 으로 남는 경우 — 그건 `0xc0000008` 로 보인다 (동적 증상).

### 4. `BCryptOpenAlgorithmProvider +0x851 / +0x883` 간접 호출

- **`+0x851/+0x856` — 열기 호출 (표 `+8`).** 줄: `+0x83a MOV RBX,[RBP-0x80]` (핸들) · `+0x841 MOV RDI,[RBP-0x28]` (표 `+8` 함수, `ValidateFunctionTable` 의 `R8` 출력) · `+0x845 MOV R8D,[RSP+0x44]` (**호출자 플래그**) · `+0x83e MOV RDX,R12` (**호출자 `pszAlgId`**) · `+0x84a MOV RAX,RDI` · `+0x84d LEA RCX,[RBX+0x18]` (**`&핸들[+0x18]` = 공급자 쪽 핸들을 받을 칸**) · `+0x851 CALL 0x18001d010`.
  - 곧 `OpenAlgorithmProvider(&핸들->공급자핸들, pszAlgId, dwFlags)` (공개 헤더 모양 `[추측]`, 레지스터로 확인 `[정적]`). **KsecDD 출력의 `pszFunction` 이 아니라 호출자의 `pszAlgId` 가 간다.** 값이 `L"AES"` 일 때 공급자는 `L"AES"` 를 받는다.
  - 반환: `+0x856 MOV R15D,EAX`. `+0x859 CMP EAX,0xc000a001` 이 아니면 `+0x88f TEST R15D; +0x892 JNS +0xa86` — **status ≥ 0 이면 성공** (0 권장).
- **`+0x883/+0x888` — 재시도 (표 `+8` 을 다시).** 조건 세 개 모두: ① 첫 status `== 0xc000a001` (`+0x859/+0x85e`, 이름 확인 안 함) ② `[RSP+0x78] == 2` (번호 2, hash; `+0x860/+0x86a JNZ`) ③ `(플래그 & 0x68) == 0x8` (`+0x86c ~ +0x873`). 그때 플래그에서 `0x8` (공개 헤더 `BCRYPT_ALG_HANDLE_HMAC_FLAG` `[추측]`) 를 빼고 (`+0x875 AND R8D,0xfffffff7`) 같은 열기 함수를 한 번 더 (`+0x883`), 결과와 관계없이 `핸들[+8] |= 8` (`+0x888`). 그 뒤 status 는 이 두 번째 값. 즉 "hash 공급자가 HMAC 플래그를 모르면 플래그 없이 다시".
- **CFG 를 지난다.** 두 호출 모두 `CALL 0x18001d010` → 거기는 `jmp 0x18001c060` (썽크 `_guard_dispatch_icall$thunk$…`, 5 B) → `jmp [0x18001ed98]` = `__guard_dispatch_icall_fptr` (bcrypt 의 로드 구성 `GuardCFDispatchFunctionPointer`). 진짜 대상은 `RAX` (= 표 `+8`).
  - **파일 속 초기값**: `[0x18001ed98] = 0x18001c090` (`jmp rax`, 검사 없는 자리 채움). `[0x18001ed90]` (`GuardCFCheckFunctionPointer`) = `0x18001bea0` (`ret`). 즉 **ntdll 이 올릴 때 바꿔 쓰지 않으면 검사 없이 `jmp rax`.** 바꿔 쓰면 ntdll 의 유효 대상 검사 (`[추측]` 이름: `LdrpDispatchUserCallTarget` 류) 를 지난다.
  - 같은 모양이 `_LoadInterface` 의 `Get…Interface` 호출 11곳 (`+0x1dc … +0x4ee`), `GetProperty` 2곳, `SetProperty`, `Close`, `BCryptGenRandom +0x75` 에도 있다 (`_calls.tsv` `indirect:cfg:?`).
- **열기 함수가 부르는 쪽 정리:** 핸들에서 꺼낸 `RBX` (`GetProperty`, `[RBP-0x40]`) 는 번호 2·3 일 때만 값이 있다. 번호 2·3 인데 `RBX == NULL` 이면 `+0xaaf` / `+0xb65` `0xc00000e5` (sast/90 에 있음. `ValidateFunctionTable` 이 이미 `+0x10` 을 검사해서 사실상 안 닿는다).
- **지금 우리:** 진짜 DLL 이 열기 코드를 갖고 있다. 에뮬레이터에서 중요한 것: ① **`__guard_dispatch_icall_fptr` 가 가리키는 곳이 에뮬레이터에서 돌아가는가** (ntdll 이 바꿔 썼다면 그 검사 함수가 이 대상 `[rax]` 를 "유효" 로 봐야 함 — 유효 표시는 로더가 DLL 올릴 때 남기는 CFG 표 `[추측]`) ② 이 호출이 **다른 DLL (bcryptprimitives) 안으로** 들어간다는 것 (에뮬레이터 CPU 가 DLL 사이 간접 호출을 그대로 따라가면 OK). 틀리면 `int 0x29` (CFG 실패는 `__fastfail`) — 동적 증상.

### 5. `_LoadImage` 와 이미 올라간 DLL

- **어떤 이름을 올리나:** `_LoadImage` 는 공급자 칸 `+0x28` (이미지 칸) → 그 `+0` 의 UTF-16 이름 (`+0x2c MOV RDX,[RCX+0x28]; +0x42 MOV RDX,[RDX]; +0x45 RtlInitUnicodeString`). `_GetImagePath` (`+0x59/+0x5e`) 가 `GetSystemDirectoryW() + "\" + 이름` 으로 경로를 만든다 (sast/90).
- **어떻게 올리나:** `+0x95 XOR R8D,R8D; +0x98 XOR EDX,EDX; +0x9a MOV RCX,RSI; +0x9d CALL [LoadLibraryExW]` → **`LoadLibraryExW(전체 경로, NULL, 0)`**. 플래그 0 (검색 경로 플래그 없음). 결과를 `*pOut = RAX` (`+0xac`), NULL 이면 `+0x16a GetLastError` → `0x7e` → `0xc0000135`, 그 밖 → `0xc000007b` (`+0x176 ~ +0x197`, `DebugTraceError` 줄 `0x4c8`).
- **이름이 `bcrypt.dll` 이면 안 올림:** `+0x7b RtlCompareUnicodeString(원래 이미지 이름 [RBP-0x10], "bcrypt.dll" [RBP-0x20] = rva 0x1f080 (길이 0x14), 대소문자 무시 = R8B=1)` — 비교 대상은 경로가 아니라 `+0x45 RtlInitUnicodeString` 으로 만든 **이름**. 같으면 `+0x140 MOV RAX,[g_hInstance]` 를 그대로 `*pOut` 에 쓰고 (`+0x147`) `+0x14a JMP +0x8f` 로 성공 길에 합류 (`+0x8f CMP [RDI],0; +0x93 JNZ +0xd6`). `LoadLibraryExW` 안 부름.
- **이미 올라간 DLL 을 안 올리는 장치는 두 겹이고, 둘 다 `LoadProviderEx` 안이다:**
  1. **`g_pLoadedProviderList`** (rva `0x25b80`, 락 `g_csLoaderLock` rva `0x25ba0`): `LoadProviderEx +0x9e ~ +0xd1` 이 연결 리스트를 돌며 `_wcsicmp(이미지 이름, 노드 이름)` (`+0xba`). 같으면 `+0xd8 MOV RAX,[RBX+8]` (노드에 저장한 `HMODULE`) 로 **`_LoadImage` 를 건너뛰고 바로 `_LoadInterface`** (`+0xee`). 노드 참조 수 `[RBX+0x10]` +1 (`+0x255 ~ +0x2a8`, 오버플로 `0xc000009a`).
  2. 새로 올렸으면 노드를 만든다: 크기 `0x20 + 이름 바이트`, `+0 이름 포인터`, `+8 HMODULE`, `+0x10 참조 수 = 2`, `+0x18 다음` (`+0x3d1 ~ +0x403`). 리스트 머리에 단다 (`+0x403 MOV [g_pLoadedProviderList],R14`).
- **이 리스트는 "bcrypt 가 올린 공급자" 만 안다.** 다른 이유로 이미 올라간 `bcryptprimitives.dll` 은 모른다 → 그 경우 `LoadLibraryExW` 가 불리고, **진짜 `ntdll` 의 로더가 "이미 있는 모듈이면 참조 수만 올리고 같은 `HMODULE` 반환" 해야 한다.** 에뮬레이터가 진짜 ntdll 코드를 따르면 자동 `[추측]`. test.exe 가 `bcrypt` 를 쓰기 전에 이미 올려 놨는지는 정적으로 못 정함 — 동적 (`lm m bcryptprimitives`).
- **`_FreeImage`** (`+0x514` 에서 실패 때만): `HMODULE == g_hInstance` 거나 NULL 이면 아무것도 안 하고, 아니면 **`FreeLibrary`** (`_FreeImage +0x12`).
- **`VsmLoadInterface` 갈래가 언제 타나 (조건 줄만):** `BCryptOpenAlgorithmProvider +0x1e2 TEST AL,0x1; +0x1e4 JNZ +0x201` 이 **안 갈 때** (`g_TrustedEnvironment` `rva 0x25ac0` 의 비트 0 이 0) `+0x1f4 CALL VsmOpenAlgorithmProvider` 로 가고 거기 `+0x16d` 가 `VsmLoadInterface` 를 부른다. `g_TrustedEnvironment` 값: `1` = 일반, `4` = trustlet (`NtQueryInformationProcess(-1,0)` 의 `Flags & 0x80`), `8`·`0x10` = enclave (`NtQueryInformationProcess(-1, 0x56)` 의 첫 DWORD `== 1` → 8, `0x10` 또는 `0x11` → `0x10`). `VsmLoadInterface` 안 갈림: `+0x50 TEST AL,0x18` → `8`·`0x10` 이면 `VsmGetLinkedInterfaceProvider`, 아니면 (`4`) `VsmGetInterfaceProviderFromLibrary`. 일반 프로세스는 `1` 이라 **안 탄다** — sast/88 5번 표 (`NtQueryInformationProcess` 0x56 의 출력 첫 DWORD 를 `1`·`0x10`·`0x11` 이 아닌 값으로) 를 지키면 그대로. 이번엔 이 갈래 본문 안 따라감.
- **지금 우리:** `LoadLibraryExW` 는 run94 4행 그대로 (에뮬레이터 로더 몫). 새로 알게 된 점: **같은 DLL 을 두 번째로 열 때 (예: AES 한 번, 난수 한 번) bcrypt 가 `LoadLibraryExW` 를 다시 안 부른다** (리스트 hit). 즉 `LoadLibraryExW` 는 이름당 한 번이다.

### 6. 최소 조건 표 (가장 중요) — sast/90 4-a 의 5·6 행을 채움

전제: sast/90 4-a 의 1~4 행을 맞췄고 (공급자 한 칸 + 이미지 이름 `bcryptprimitives.dll`) `LoadLibraryExW` 가 진짜 `bcryptprimitives.dll` 을 올려 attach 를 통과시켰다 (sast/87).

| # | syscall (또는 읽는 칸) | 입력 | 기대 답 | 안 맞으면 가는 자리 |
|---|---|---|---|---|
| 4 | `LoadLibraryExW` (sast/90 과 같음) | `C:\Windows\System32\bcryptprimitives.dll`, 플래그 0 | 성공 + attach 통과 | `_LoadImage +0x18c/+0x176` `0xc0000135`/`0xc000007b` |
| **5** | **`GetProcAddress`** (`_LoadInterface +0x130/+0x137`) | 위 `HMODULE` + 번호별 이름 (질문 1 표). AES=1 → `GetCipherInterface`, hash=2 → `GetHashInterface`, `RNG`=6 → `GetRngInterface` | **이름 있음 (bcryptprimitives 8개 다 있음 O)**. 반환 `RAX ≠ 0` | NULL → `+0x144` **`0xc0000139`** → `LoadProviderEx +0x10f` 로그 → `_FreeImage` (`FreeLibrary`) → 다음 공급자 / 실패 |
| **5-b** | `Get…Interface` 간접 호출 (CFG) | 번호 1·2·3·4·5·7·8: `(pszProvider, pszFunction, &표, 0)`, 번호 6: `(pszProvider, &표, 0)` | status ≥ 0 **그리고 `*&표 ≠ NULL`**, 표 머리 첫 2바이트 ≥ 1 | 음수 → 그대로 위로. `*&표 == NULL` → 크래시. 머리 0 → `0xc00000bb` |
| **5-c** | **KsecDD 가 준 `pszProvider`·`pszFunction`** (4-b 표 `+0x28`, `+0x20`) | 진짜 `Get…Interface` 가 받는다 | 진짜 DLL 이 받아 줄 값 (`pszFunction` = 알고리즘 이름, `pszProvider` = `L"Microsoft Primitive Provider"` 또는 NULL?) | **`[공백]`** — 진짜 DLL 본문을 이번엔 안 읽음 (다음 요청 1) |
| **5-d** | 표 복사 | 핸들 `+0x30 ← 표` (`0x30 ~ 0x70` 바이트, 번호별) | 표가 **복사 크기만큼 읽힘** | 읽기 오류 (`LoadProviderEx +0x247 memcpy`) |
| **6** | **`ValidateFunctionTable`** (`BCryptOpenAlgorithmProvider +0x70b`) | 복사된 표 | **번호별 NULL 금지 칸이 다 채워짐** (3-b 표) — 진짜 표는 채워져 있다 `[추측]` | **`0xc0000008`** → 다음 공급자 / 실패 |
| **6-b** | 열기 함수 (표 `+8`, CFG, `+0x851`) | `(&핸들[+0x18], 호출자 pszAlgId, 호출자 플래그)` | **status ≥ 0** (0 권장), `*&핸들[+0x18]` 을 채움 | 음수 → 다음 공급자 / 실패 (`+0x89d` 로그). 단 `0xc000a001` + hash + HMAC 플래그는 플래그 빼고 재시도 |
| **6-c** | `GetProperty` (번호 2: `L"HashBlockLength"`, 번호 3: `L"PaddingSchemes"`), `+0xb19`·`+0xbcd` | 4바이트 출력 | **status ≥ 0** | 음수 → `Close` 부르고 실패 반환 (다음 공급자 안 시도) |
| 6-d | `SetProperty` (표 `+0x18`, `+0xcc7`) | KsecDD 출력 `cProperties` 개 | 반환 **안 봄** (4-b 는 0개) | — |
| **7** | 성공 | `*phAlgorithm = 핸들` (`+0xce7`), 반환 `0` | | |

- **KsecDD 가 정해 주는 것은 "번호 · 공급자 이름 · 알고리즘 이름 (`pszFunction`) · 이미지 이름 · 속성" 뿐.** 표 머리·NULL 칸·열기 status 는 **진짜 `bcryptprimitives` 코드가 정한다.** 그래서 `0xc0000008`·`0xc00000bb` 가 에뮬레이터에서 보인다면 원인은 KsecDD 답이 아니라 "DLL 이 진짜처럼 안 돌아서" (재배치·IAT·TLS·CFG 표·메모리 접근) 일 가능성이 높다.
- 번호는 알고리즘에 맞아야 한다 (`1`=암호 `AES`, `2`=hash, `6`=난수 …). 틀리면 `Get…Interface` 가 다른 표를 주고, **열기 함수가 `pszAlgId` 를 모르는 이름이라 음수** → 실패 (진짜 공급자 몫 `[추측]`). 알고리즘 ↔ 번호 전체 표는 `[공백]` (Q178b ③, 커널 `ksecdd.sys` 몫).
- **번호 6 (난수) 은 가장 단순.** 표는 `0x30` 바이트, 검사 5칸, 열기 후 `GetProperty` 도 안 부름 (`+0xaa1 CMP EAX,2` / `+0xb57 CMP EAX,3` 둘 다 아님 → `+0xc74`). 이후 `BCryptGenRandom +0x75` 가 **핸들 `+0x58` (= 표 복사 `+0x28`) 을 `(핸들[+0x18], 버퍼, 크기, 플래그)` 로 부른다** (`+0x64 MOV RCX,[RAX+0x18]; +0x68 MOV R9D,EDI; +0x6b MOV RAX,[RAX+0x58]; +0x6f MOV R8D,ESI; +0x72 MOV RDX,RBP`, 호출 `+0x75`; 인자 순서는 `BCryptGenRandom(h, pb, cb, flags)` 의 `RDX=pb, R8=cb, R9=flags` 에서 옴 `+0x1a ~ +0x23`) — 이게 다음 벽.
- **지금 우리:** run94 4-b 에서 `dwInterface = 1`(AES)·`6`(RNG)·`2`(hash) 까지 맞췄다. **5·6 행은 우리가 값을 만들 칸이 없다 (진짜 DLL 몫).** 에뮬레이터가 막힐 곳 후보: ① `GetProcAddress` 가 이름을 못 찾음 (내보내기 표를 읽는 코드 — 진짜 `ntdll`/`kernel32` 코드가 돌면 O), ② **CFG 간접 호출** (위 질문 4), ③ DLL 속 `Get…Interface`·열기·속성 코드가 쓰는 API/명령 (이번에 안 읽음).

---

## 정정·메모 (sast/90 과 달라진 것)

| sast/90 | 이번에 본 것 |
|---|---|
| 4-b 표 `0x28 pszProvider` "로그에만 씀" | **`Get…Interface` 첫 인자로 간다** (`_LoadInterface +0x1d2 MOV RCX,[R15+0x10]`, 번호 6 은 `+0x338`). NULL (`-1`) 이면 진짜 DLL 에 NULL 이 간다 |
| 4-b 표 `0x60 L"AES"` "핸들 안에 복사됨 (`+0x4bb`)" | 핸들에 복사되는 건 **호출자 `pszAlgId`** (`R12`). KsecDD 의 `pszFunction` 은 **`Get…Interface` 둘째 인자** (`_LoadInterface +0x1cb MOV RDX,[R15+8]`). 두 이름이 같다고 가정해도 되지만 (`AES`) 서로 다른 칸이다 |
| 4-a 5행 "이름만" | 질문 1 표 (번호 ↔ 이름, bcryptprimitives O 8개) |
| 4-a 6행 "이름만" | 질문 3 표 (번호별 NULL 금지 칸) + 질문 4 (열기 인자) |
| 271줄 "이름 후보" | 번호 ↔ 이름 짝 전부 확정 (질문 1) |

---

## 동적분석으로 확인해야 할 것

정적으로 못 정한 것. 모든 멈춤은 `test.exe` 일 때만 (프로세스 이름 조건). 심볼은 C 라서 꾸밈이 없지만 PDB 이름 앞에 `_` 가 붙은 것 (`_LoadInterface`, `_LoadImage`) 이 있으니 **`x bcrypt!*LoadInterface*` 로 먼저 이름 확인** 후 건다.

- **CFG 가 진짜로 어떻게 돌아가나 (제일 중요):** `dq bcrypt!__guard_dispatch_icall_fptr L1` 값이 `bcrypt+0x1c090` (파일 초기값, `jmp rax`) 인지, ntdll 안 함수인지. 후자면 `ln` 으로 이름, 그 함수가 `bcryptprimitives` 안 대상을 받아 주는지.
- **진짜 KsecDD 출력에서 `pszProvider`·`pszFunction` 이 뭔지** (sast/90 동적 1번과 같은 멈춤으로 한 번에: `bcrypt!BCryptResolveProviders +0x5f2`). 특히 `pszProvider` 가 NULL 인지 `Microsoft Primitive Provider` 인지.
- `Get…Interface` 가 진짜로 돌려주는 표: 머리 첫 2바이트 (≥ 1 이어야), 칸 값 (NULL 없는지), 복사 크기 만큼 읽히는지.
- 열기 함수가 받는 `pszAlgId`·`플래그` 와 반환 status.
- 번호 2·3 일 때 `GetProperty` 의 반환 (`HashBlockLength`·`PaddingSchemes`).
- `bcryptprimitives.dll` 이 `bcrypt` 열기 전에 이미 올라가 있나 (`lm m bcryptprimitives`).
- 추천 멈춤점 (모두 `test.exe` 조건, `+0x` 는 이 결과서의 call/ret 자리):
  - `bcrypt!_LoadInterface +0x130` (call 직전): `rcx` = `HMODULE` (`lm a @rcx`), `da @rdx` = **고른 이름**. `+0x137` (돌아온 뒤): `rax` = 함수 주소 (`ln @rax`, 0 이면 `0xc0000139` 길).
  - `bcrypt!LoadProviderEx +0xf3` (`_LoadInterface` 반환): `eax` (status), `poi(@rsp+0x48)` = 표 포인터 → `dq poi(@rsp+0x48) L0xe` (표 `0x70` 바이트 내 머리·칸).
  - `bcrypt!LoadProviderEx +0x24c` (`memcpy` 반환): 복사 끝. 크기 `r8d` 는 `+0x247` 직전.
  - `bcrypt!BCryptOpenAlgorithmProvider +0x70b` (call 직전): `ecx` = 번호, `rdx` = 표 복사본 → `dq @rdx L0xe`. `+0x710`: `eax` (`0xc0000008` 이면 NULL 칸).
  - `bcrypt!BCryptOpenAlgorithmProvider +0x851` (call 직전): `rax` = 열기 함수 (`ln @rax`), `rcx` = `&핸들[+0x18]`, `du @rdx` = 이름, `r8d` = 플래그. `+0x856`: `eax`.
  - `bcrypt!BCryptOpenAlgorithmProvider +0xb19` / `+0xbcd` (call 직전, 번호 2·3): `rax` = `GetProperty` 함수, `du @rdx` = `HashBlockLength` / `PaddingSchemes`. `+0xb1e` / `+0xbd2`: `eax`.
  - `bcrypt!BCryptOpenAlgorithmProvider +0xce7` (성공 끝): `rax` = 핸들 → `dq @rax L0x79` 로 핸들 전체 (`+0x18` 공급자 핸들, `+0x24` 번호, `+0x28` 노드, `+0x30 ~` 표 복사).
  - `bcrypt!_LoadImage +0xa4` (sast/90 에 있음): 이름당 한 번만 걸리는지 (두 번째 열기에서 안 걸리면 리스트 hit).
  - 비교용: `x bcryptprimitives!Get*Interface` (주소) 와 `_LoadInterface +0x137` 의 `rax` 가 같은지.

## 다음 요청 후보

1. **(정적, 작게 — 이 요청의 다음 벽)** bcryptprimitives 의 `GetCipherInterface` · `GetHashInterface` · `GetRngInterface` (이름·rva 는 위 표) **본문과 그 표의 `+8` (열기)·`+0x10` (GetProperty)**: 첫 인자 `pszProvider` 가 NULL 이어도 되나, `pszAlgId` 이름 검사, 표 머리 값, 열기가 쓰는 전역·API. KsecDD 답의 `pszProvider` (4-b) 가 NULL 이어도 되는지의 근거.
2. **(정적, 작게)** `BCryptGenRandom +0x75` 가 부르는 **난수 표 `+0x28`** (`GenRandom` 함수) 과 `BCryptCloseAlgorithmProvider` 가 부르는 표 `+0x20`. 난수가 나오는 마지막 줄까지의 길 (test.exe 가 난수를 쓰는 길).
3. **(정적)** Q178b: `BCryptEnumProviders` · `BCryptEnumAlgorithms`, 알고리즘 ↔ `dwInterface` 번호 (커널 `ksecdd.sys` 몫, Q173b ③).
4. **(동적)** 위 추천 멈춤점. 특히 CFG 한 줄 (`__guard_dispatch_icall_fptr` 값) 과 `+0x137` / `+0x70b` / `+0x851`.
5. **(정적, 후순위)** Q173b ①·④ (`GetModuleHandleExW`·`LdrAddRefDll`, `NtClose` ×5, `NtTraceControl` ×4).

## 근거 파일

- `ms/disassemble/bcrypt/_LoadInterface.asm` (전체: `+0x0 ~ +0x589`, 점프 표 rva `0x110cc`·`0x110ec`·`0x1110c`)
- `ms/disassemble/bcrypt/ValidateFunctionTable.asm` (전체, 점프 표 rva `0x10444`)
- `ms/disassemble/bcrypt/LoadProviderEx.asm` (`+0x21 ~ +0xd8`, `+0xee ~ +0x126`, `+0x16a ~ +0x24c`, `+0x2e9 ~ +0x452`, `+0x50a ~ +0x539`, 점프 표 rva `0x8d9c`)
- `ms/disassemble/bcrypt/_LoadImage.asm` (전체), `_FreeImage.asm` (전체)
- `ms/disassemble/bcrypt/BCryptOpenAlgorithmProvider.asm` (`+0x39c ~ +0x4c2`, `+0x4dd ~ +0x535`, `+0x682 ~ +0x715`, `+0x83a ~ +0x892`, `+0xa86 ~ +0xd1c`, `+0x1e2 ~ +0x201`)
- `ms/disassemble/bcrypt/VsmLoadInterface.asm` (`+0x0 ~ +0xc3`), `GetTrustedEnvironment.asm`, `IsEnclave.asm`
- `ms/disassemble/bcrypt/BCryptGenRandom.asm` (`+0x51 ~ +0x7e`)
- 지도·장부: `ms/disassemble/bcrypt/_map/entry_nodes.tsv`·`entry_edges.tsv`, `_calls.tsv`
- PE 바이트 (bcrypt `.rdata`): rva `0x20010 ~ 0x20128` (이름 문자열), `0x110cc / 0x110ec / 0x1110c / 0x10444 / 0x8d9c` (점프 표), `0x1f930` `HashBlockLength`, `0x1f950` `PaddingSchemes`, `0x1f080` `bcrypt.dll`, `0x1ed90 / 0x1ed98` (CFG 포인터 초기값), `0x1c090` (`jmp rax`)
- PE 내보내기 표: `bcryptprimitives.dll` (임시 파이썬 파서, 레포 밖), `ms/decomp/bcryptprimitives/_index.tsv` (rva·크기)

## 판정

**분석성공.** 요청서 6개 질문 모두 `.asm` 줄로 답함. 질문 1 (번호 8개 ↔ 이름 ↔ 문자열 rva ↔ 고르는 줄) · 3 (번호별 NULL 금지 칸 + 실패 status + 순서) · 4 (열기 인자, 재시도 조건, CFG) · 6 (최소 조건 표) 가 전부 채워졌다. bcryptprimitives 가 `1..8` 이름 8개를 다 내보내는 것을 PE 내보내기 표로 확인했다. **못 닫은 것:** 진짜 `Get…Interface` 가 `pszProvider` 를 어떻게 쓰는지 (본문 안 읽음, 다음 요청 1), 알고리즘 ↔ 번호 전체 표 (커널 몫), `__guard_dispatch_icall_fptr` 의 실행 때 값 (동적). sast/90 4-b 의 두 칸 설명을 정정했다.
