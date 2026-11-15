# 90 bcrypt-resolve — 정적 분석 결과

- 요청서: `_workspace/sast/90_bcrypt-resolve.md` (Q173a, PE_emulator 요청 33 의 다음 벽)
- 스크립트 문제: **없음.** (`export` 4번 · `refs` 3번, 로그에 `SCRIPT ERROR` 0줄)
- 모듈 / 바이너리: bcrypt (`ms/bin/.../System32/bcrypt.dll`, x64, 이미지 베이스 `0x180000000`)
  - PDB GUID+Age = `5F1077ED8D3ABC83AD7D378F94EA41901` (sast/88 과 같음). analyze 는 **다시 안 함** (`.gpr` + `_index.tsv` 312줄 + `SetPdb:` 줄 다 있음).
  - **PDB 에 `_CRYPT_PROVIDER_REFS` 같은 구조체 정의가 없다** (`pdb_struct.py --list` 0건). 칸 표는 전부 코드(`IoUnpack_*` 의 오프셋)에서 읽었다. 이름(`pszFunction` 등)은 공개 헤더 기억 `[추측]`, 오프셋·크기·검사는 `[정적]`.
- 뽑은 함수 (`ms/disassemble/bcrypt/*.asm`, `ms/decomp/bcrypt/*.c`):
  - `IoUnpack_ProviderRef` + `OpenSystemPreferredAlgorithmProvider` (depth 1, 11개: `IoUnpack_ImageRef`·`IoUnpack_PropertyRef`·`SzString_CbLength`·`SzString_CchLength`·`BCryptCloseAlgorithmProvider`·`BCryptFreeBuffer`·`WPP_SF_sDsd`·`BCryptOpenAlgorithmProvider`·`BCryptResolveProviders` 다시 포함)
  - depth 0: `ValidateInterfaceId` · `WinErrorToNtStatus` · `IoUnpack_SG_SzString` · `IoUnpack_SG_Buffer`
  - **범위를 넓힌 것 (질문 4 때문):** `LoadProviderEx`, `_LoadImage`, `_GetImagePath` (depth 0, **어떤 DLL 을 올리나 한 줄 답하려고 그 필드·호출 이름만 봄**. 본문은 안 따라감). `refs`: `0x25b2c` (`g_fLoaderInitialized`) · `0x25c10` (`g_pResolveProviderCache`) · `0x25898` (`g_fLoadCNGDone`).
  - 이미 있던 것 그대로: `BCryptResolveProviders` (본문 처음 읽음) · `BCryptResolveProvidersForBcoapCached` · `BCryptOpenAlgorithmProvider` · `BCryptGenRandom` · `GetSystemPreferredRngHandle` · `IoCallKernelDriver` · `IoCallKernelDriverQueryLoop`
- 버전 확인: **일치.** (bcrypt 는 "알려진 사실" 표에 없음. 에뮬레이터 로그 값을 코드 값과 하나씩 맞춤)
  - IOCTL 코드 `0x390400` = `BCryptResolveProviders +0x5de MOV [RSP+0x28],0x390400`. 기능 `0x20000` = `+0x590`.
  - 입력 `0x38` / `0x30`, 출력 `0x180`, 결과 `0xc00000e5` 가 **전부 코드로 설명됨** (아래 한눈에). 어긋나는 줄 없음.

---

## 한눈에 (먼저 읽기)

1. **`0xc00000e5` 의 정체** (로그의 결말): 기본 대답이 "status 0 인데 `IOSB.Information` 이 0" (또는 "0 으로 채운 0x180") 이면 → `BCryptResolveProviders +0x4b0 JA` (또는 `+0x7d7 JZ`) → **`+0x7f8 MOV R12D,0x54f`** (Win32 `ERROR_INTERNAL_ERROR`) → `+0x3ba WinErrorToNtStatus(0x54f)` → **`0xc00000e5`** (`WinErrorToNtStatus +0x70`). 두 모양 다 같은 줄로 끝난다. `[정적]`
2. **입력 `0x38`/`0x30` = 기능 `0x20000` 맞다.** 계산식: `0x30 + (문자열마다 ((글자수×2)+9)&~7)`. `0x38` = 문자열 1개 (이름 0~3 글자: `AES`·`RNG`·`MD5`·`RC4`…), `0x30` = 문자열 0개 (`OpenSystemPreferredAlgorithmProvider` 의 인터페이스 6 질의). `[정적]`
3. **출력 `0x180` = 고정 첫 시도 크기.** 호출한 쪽이 "버퍼 없음 (`*ppBuffer == NULL`)" 으로 부르면 `+0x7a8 MOV R15D,0x180`. **재시도는 한다** — 단 "크기 모자람" 은 status `0x80000005` + 출력 8바이트 `{0xc0000023, 필요한 크기}` 로 알려야 하고, status 를 그냥 `0xc0000023` 으로 주면 재시도 없이 `0xc00000e5` 다 (아래 2-a). `[정적]`
4. **`IOSB.Information` 을 읽는다** — 그게 "출력이 몇 바이트냐" 의 유일한 근거다 (출력 안에 전체 크기 칸 없음). **`IOSB.Status` 는 안 읽는다.** `[정적]`
5. **성공으로 보내려면 (질문 4):** status `0` + `Information` = N (`0x10 ≤ N ≤ 0x180`) + 출력 N 바이트 = **머리 0x10 · 공급자 포인터 배열 · 공급자 한 칸 0x38 · 이미지 칸 0x10 · 문자열 (알고리즘 이름, `bcryptprimitives.dll`)**. 가장 작은 한 장 = 0x98 바이트 (4-b). 문자열·칸은 전부 "출력 시작 기준 오프셋" 이고 bcrypt 가 시작 주소를 더해 포인터로 고친다. `[정적]`
6. **공급자 0 개 답은 쓰지 말 것.** `BCryptOpenAlgorithmProvider +0x3a5/+0x3a9` 가 첫 공급자를 검사 없이 읽는다 → `RAX=0` 이라 정적으로 읽으면 **널 역참조 (크래시)**. (`BCryptGenRandom` 길은 `OpenSystemPreferredAlgorithmProvider +0x106 JC +0x174` 가 먼저 막아서 `0xc00000e5`.) `[정적]`
7. **어떤 DLL 을 올리나:** 출력의 공급자 칸 `+0x28` (이미지 칸) → 그 안의 이미지 이름. `LoadProviderEx` → `_LoadImage` 가 **`GetSystemDirectoryW() + "\" + 이름`** 으로 `LoadLibraryExW` 한다. 그러니 이름은 **파일 이름만** (`bcryptprimitives.dll`) 주면 된다. `[정적]`
8. **캐시:** 이름 하나당 IOCTL 한 번 (기본 모양일 때만). 전역 `g_pResolveProviderCache` (rva `0x25c10`). `[정적]`

---

## 질문별 답 [정적]

### 1. 누가 이 IOCTL 을 부르나 — 입력 길이와 맞추기

#### 1-a. 부르는 사슬 (두 길 다 같은 `BCryptResolveProviders` 로 모인다)

| # | 길 | 함수 + 오프셋 (call/ret) | 넘기는 것 (`BCryptResolveProviders` 인자) | 갈림 |
|---|---|---|---|---|
| A1 | `BCryptOpenAlgorithmProvider` | `+0x259/+0x25e` → `BCryptResolveProvidersForBcoapCached` | 알고리즘 이름 `pszAlgId`, 공급자 이름, 모드 플래그 (아래 1-e) | `+0x1e4 JNZ` (`g_TrustedEnvironment & 1`) 안 타면 `VsmOpenAlgorithmProvider` (`+0x1f4`, 이번엔 안 봄) |
| A2 | `BcoapCached` | `+0x11b/+0x120` | `(NULL, 0, 이름, NULL, 1, 2, &cb, &refs)` — 캐시 미스일 때 | `+0x23 TEST RDX; +0x26 JNZ +0xa5`, `+0x28 CMP R8D,2; +0x2c JNZ +0xa5` — 공급자 있거나 플래그 ≠ 2 면 A3 |
| A3 | `BcoapCached` | `+0xcf/+0xd4` | `(NULL, 0, 이름, 공급자, 1, 플래그, &cb, &refs)` | 캐시 안 씀 |
| B1 | `BCryptGenRandom` | `+0x157/+0x15c` → `GetSystemPreferredRngHandle` | — | `+0xa2 JZ +0xe5` (핸들 NULL) → `+0xff TEST AL,1; +0x101 JNZ +0x152` (`g_TrustedEnvironment & 1`) |
| B2 | `GetSystemPreferredRngHandle` | `+0x78/+0x7d` → `OpenSystemPreferredAlgorithmProvider` | — | 캐시 칸 (rva `0x25ad0` / `0x25ac8`) 이 0 일 때만 (`+0x23 MOV RAX,[0x25ad0]; +0x36 TEST; +0x39 JZ +0x73`) |
| B3 | `OpenSystemPreferredAlgorithmProvider` | `+0xf7/+0xfc` → **`BCryptResolveProviders`** | `(NULL, 6, NULL, NULL, 1, 0, &cb, &refs)` | `+0x26 CMP [g_fLoadCNGDone],0; +0x30 JNZ +0xcd` — CNG 로드 끝났으면 이 질의, 아니면 곧바로 이름 `L"RNG"` (`+0x36 LEA RDX,[0x18001f128]`) |
| B4 | 〃 | `+0x47/+0x4c` → `BCryptOpenAlgorithmProvider(&h, 이름, NULL, 0)` | 이름 = B3 답의 **첫 공급자 `pszFunction`** (`+0x118 MOV RAX,[RBX+8]; +0x11c MOV RCX,[RAX]; +0x11f MOV RDX,[RCX+8]`) | 여기서 A1 로 이어짐 |

- **`GenRandom` 길은 다른 함수가 아니라 같은 `BCryptResolveProviders` 다** (B3). `IoCallKernelDriverQueryLoop` 은 부르는 쪽이 `BCryptEnumContexts`·`BCryptQueryContextConfiguration`·`BCryptEnumRegisteredProviders`·`BCryptQueryProviderRegistration` 같은 **설정 질의 함수들**이라 `OpenAlgorithmProvider`·`GenRandom` 길에 안 닿는다. 이름만. `[정적]`
- 즉 `BCryptGenRandom(NULL, …, 플래그 2)` 첫 호출은 **IOCTL 이 연달아 두 번**: ① B3 (입력 `0x30`, 인터페이스 6) → ② B4→A1→A2 (입력 `0x30 + 이름`, 이름이 ≤3 글자면 `0x38`). 에뮬레이터가 본 "`0x30` 3줄" 이 ①, "`0x38` 12줄" 이 ② 와 `BCryptOpenAlgorithmProvider` 직접 호출. `[정적]` 맞춤 / 실제 호출 횟수는 `[동적]`.

#### 1-b. 두 IOCTL 길 — 언제 타나

`BCryptResolveProviders` 의 8번째 인자 `ppBuffer` (`+0x74 MOV [RSP+0x60],RAX`) 하나로 갈린다.

| 길 | 갈림 | 부르는 자리 (call/ret) | 입력 길이 | 출력 | 기능 번호 (입력 `+4`) |
|---|---|---|---|---|---|
| **직접** `NtDeviceIoControlFile` | **`+0x253 TEST RAX,RAX; +0x256 JNZ +0x514`** (`ppBuffer` ≠ NULL) | **`+0x5eb/+0x5f2`** | `[RSP+0x54]` (1-c 식) | 첫 시도 `0x180` (`+0x7a8`) 또는 호출자 버퍼 크기 `*pcbBuffer`. 모자라면 다시 | `+0x590 MOV [RSI+4],0x20000` |
| `IoCallKernelDriver` | 위에서 안 갈림 (`ppBuffer == NULL`, 크기만 묻는 모드) | **`+0x276/+0x27b`** (`+0x271 MOV ECX,0x20000`) | 같음 (`+0x25c MOV R8D,[RSP+0x54]`) | **8 바이트 지역** + 출력 포인터 NULL → `IoCallKernelDriver +0x141` 이 `0xc0000023` 과 필요한 크기만 돌려줌. `BCryptResolveProviders +0x803` 이 이걸 **status 0 + `*pcbBuffer = 크기`** 로 바꿈 | `IoCallKernelDriver +0x5b` 가 `ECX` 를 입력 `+4` 에 씀 |

- **`OpenAlgorithmProvider` 길(A)·`GenRandom` 길(B) 둘 다 `ppBuffer ≠ NULL` 이라 "직접" 길만 탄다.** `IoCallKernelDriver` 길은 `BCryptEnumProviders`·`BCryptEnumAlgorithms` 같은 쪽에서 `ppBuffer` 를 NULL 로 줄 때뿐 (그 둘이 실제로 NULL 을 넘기는지는 안 읽음 `[공백]`).
- `IoCallKernelDriver` 는 기능 `0x20000` 이라고 달리 굴지 않는다 — 기능 번호는 그냥 입력 `+4` 에 복사. `IoPack_*` 도 이 길에선 안 쓴다 (`BCryptResolveProviders` 가 입력을 **직접** 채움: `SzString_CchLength` + `memcpy`). attach 의 `0x10500` 입력 (`IoPack_Configuration_ParamBlock`, `0x68`) 과 **칸 배치가 다르다** (아래 1-c).

#### 1-c. 입력 칸 표 (기능 `0x20000`)

채우는 곳: `BCryptResolveProviders +0x1f6 ~ +0x243` (머리), `+0x15c ~ +0x1eb`·`+0x313 ~ +0x36d` (문자열), `+0x58a/+0x590` (표지·기능).

| 오프셋 | 크기 | 뜻 | 출처 (줄) |
|---|---|---|---|
| `+0x00` | 4 | `0x1a2b3c4d` 표지 | `+0x58a MOV [RSI],0x1a2b3c4d` |
| `+0x04` | 4 | **`0x20000`** 기능 번호 | `+0x590` |
| `+0x08` | 8 | `pszContext` 문자열의 입력 안 오프셋, 없으면 `-1` | `+0x223 MOV [R14+8],RCX`, `-1` 은 `+0x2b5` |
| `+0x10` | 4 | `dwInterface` (OpenAlgorithm 길 = `0`, GenRandom 질의 = `6`) | `+0x1ff MOV [R14+0x10],EAX` |
| `+0x14` | 4 | 안 씀 (스택 쓰레기) | — |
| `+0x18` | 8 | `pszFunction` (= 알고리즘 이름) 오프셋, 없으면 `-1` | `+0x233`, `-1` 은 `+0x2c1` |
| `+0x20` | 8 | `pszProvider` 오프셋, 없으면 `-1` | `+0x243`, `-1` 은 `+0x2cd` |
| `+0x28` | 4 | `dwMode` (모두 `1`) | `+0x209` |
| `+0x2c` | 4 | `dwFlags` (A2 = `2`, A3·B3 = `0`) | `+0x213` |
| `+0x30` ~ | 가변 | 문자열 (UTF-16, NUL 포함). **순서: 컨텍스트 → 함수(이름) → 공급자**, 각 칸 8바이트로 올림. 없는 건 건너뜀 | `+0x313` (컨텍스트는 `R14+0x30`), `+0x166 MOV EDI,R15D` (함수), `+0x1c3 MOV ESI,R15D` (공급자) |

- **입력 길이 계산식:** `0x30 + Σ ((글자수 × 2) + 9) & ~7` (`+0xb0 MOV R15D,0x30`, `+0xef LEA EAX,[9+RAX*2]`, `+0xf6 AND EAX,0xfffffff8`, `+0xf9 ADD ESI,EAX`; 컨텍스트 `+0x2fd`, 공급자 `+0x127`). 결과를 `[RSP+0x54]` 에 저장 (`+0x30a`, `+0xfb`, `+0x133`).
  - 예: 이름 `AES`/`RNG`/`MD5` (3글자) → `0x30+8 = 0x38`. `SHA1`·`SHA256` → `0x40`. `ECDSA_P256` → `0x48`. 이름 없음 (GenRandom 질의) → `0x30`.
  - 공급자 이름도 같이 줄 땐 (A3): `L"Microsoft Primitive Provider"` (28글자) 는 `+0x40` → `AES` 면 `0x30+8+64 = 0x78`. **에뮬레이터 로그에 아직 안 보인 모양이지만 `MS_PRIMITIVE_PROVIDER` 를 직접 쓰는 샘플은 이 모양이 온다.**
- `0x140` 보다 크면 `BCryptAlloc` (`+0x137 CMP ESI,0x140; +0x13d JA +0x372`), 아니면 스택 지역 (`LEA R14,[RBP-0x60]`).
- **두 가지 실제 입력 (에뮬레이터 로그 12줄/3줄에 맞춤):**

| 모양 | 길이 | `+0x10` | `+0x08` | `+0x18` | `+0x20` | `+0x28` | `+0x2c` | `+0x30` |
|---|---|---|---|---|---|---|---|---|
| `BCryptOpenAlgorithmProvider(L"AES", NULL, 0)` (A1→A2) | `0x38` | 0 | -1 | `0x30` | -1 | 1 | **2** | `L"AES\0"` |
| `OpenSystemPreferredAlgorithmProvider` 질의 (B3) | `0x30` | **6** | -1 | -1 | -1 | 1 | **0** | (없음) |

- **`dwMode`·`dwFlags` 이름:** 코드가 확인하는 건 값뿐 — `dwMode ∈ {1,2,3}` (`+0x86 ~ +0x451`, 아니면 `0x57`), `dwFlags & 0xfffffffc == 0` (`+0xa0/+0xaa`, 아니면 `0x3ec`). 공개 헤더의 `CRYPT_UM`(1)·`CRYPT_ALL_PROVIDERS`(2) 와 같은 값이라 보이지만 `[추측]`.
- **지금 우리: 입력 길이·코드·기능 번호는 맞음.** (우리가 만들 것은 입력이 아니라 **답**.)

#### 1-d. 출력 `0x180` 은 어디서 정해지나

- **`*ppBuffer == NULL` 이면 고정 `0x180`**: `+0x514 MOV RCX,[RAX]; +0x51a JZ +0x7a8 → MOV R15D,0x180 → JMP +0x52b` → `+0x544 MOV ECX,R15D; +0x547 BCryptAlloc` → `NtDeviceIoControlFile` 의 출력 버퍼·출력 길이 (`+0x5de ~ +0x5e6` 의 `[RSP+0x40]`/`[RSP+0x48]`).
- `*ppBuffer ≠ NULL` (호출자가 자기 버퍼를 줌) 이면 `*pcbBuffer` 크기를 쓴다 (`+0x520 ~ +0x52b`). 두 관찰 호출자 (`OpenAlgorithmProvider` `+0x242 MOV [RBP-0x68],RBX`, `OpenSystemPreferredAlgorithmProvider +0x2c MOV [RBP+0x30],RBX`) 는 둘 다 NULL 로 시작 → **둘 다 `0x180`.** 로그의 출력 `0x180` 과 같음.

#### 1-e. `BCryptOpenAlgorithmProvider → BcoapCached → BCryptResolveProviders` 인자

| 단계 | 자리 | 값 |
|---|---|---|
| `BCryptOpenAlgorithmProvider` 인자 검사 | `+0xc7 ~ +0xd3`, `+0xd9 TEST R9B,1` | `phAlgorithm`·`pszAlgId` NULL 이거나 `dwFlags` 비트0 켜짐 → `0xc000000d` |
| 공급자 이름 고르기 | `+0x201 CMP [g_fLoadCNGDone],EBX` | **0 (아직 CNG 초기화 전)** → 공급자 = `L"Microsoft Primitive Provider"` (rva `0x1f8f0`), 플래그 0 · **≠ 0** → 공급자 = 호출자 인자 `pszImplementation`; 그게 NULL 이면 플래그 `EDI=2`, "내가 소유" 표시 `[RSP+0x40]=1` (`+0x224 ~ +0x229`) |
| `BcoapCached` 호출 | `+0x259` | `RCX = pszAlgId`, `RDX = 공급자`, `R8D = 0|2`, `R9 = &cb [RSP+0x48]`, `[RSP+0x20] = &refs [RBP-0x68]` (0 으로 시작 `+0x242`), `[RSP+0x28] = &"호출자가 free 해야 하나" 불리언 [RSP+0x41]` |
| 캐시 조건 | `BcoapCached +0x23 ~ +0x2c` | 공급자 NULL **그리고** 플래그 == 2 일 때만 캐시 (질문 5) |
| `BCryptResolveProviders` 호출 | `+0x11b` (캐시) / `+0xcf` (그 외) | 위 1-c 표 |

#### 1-f. `GenRandom` 길 "이름과 갈림 자리만"

위 1-a 의 B1~B4. 결과 핸들은 전역 칸에 캐시 (`GetSystemPreferredRngHandle +0x8a CMPXCHG.LOCK [0x25ad0]`) — 그다음 `BCryptGenRandom` 부터는 IOCTL 안 나온다. `[정적]`

- **지금 우리:** 3줄(0x30) 은 `B3` 에서 막혀서 `0xc00000e5` (아래 3). `B4` 이후는 아직 못 감.

---

### 2. 출력을 어떻게 읽나 — `IoUnpack_ProviderRef` 와 그 앞뒤

#### 2-a. status 를 어떻게 보나 (`BCryptResolveProviders +0x5f7 ~ +0x7bd`)

| IOCTL status | 자리 | 하는 일 |
|---|---|---|
| **≥ 0 (0, `0x103` 도 성공 취급)** | `+0x5fb JS` 안 탐 | 성공 길. `IOSB.Information` 을 읽음 (`+0x601 MOV R15,[RBP-0x80]` = `[RSP+0x80]` = IOSB 의 `Information`). 반환 status 는 항상 `0` (`+0x62b MOV ESI,EBX`) |
| 〃 그런데 `Information > 내 버퍼 크기` | `+0x60e CMP [RSP+0x50],R15D; +0x613 JC +0x725` | 내가 할당한 버퍼면 **`Information` 크기로 다시 할당해 IOCTL 다시** (`+0x725 → +0x72f → +0x53b → +0x544`). 호출자 버퍼면 `0xc0000023` + `*pcb = 필요한 크기` 로 끝 |
| `0xc0000023` | `+0x6b9 CMP EAX,0xc0000023; +0x6be JZ +0x7bd` | **재시도 안 함. `0xc00000e5`** (`+0x664 MOV ESI,0xc00000e5`) |
| **`0x80000005`** | `+0x6c4 CMP EAX,0x80000005` | `Information < 4` → `0xc00000e5` (`+0x6d6 JC +0x7bd`). 출력 첫 DWORD (`[R13]`, `+0x6dc`) 로 갈림: |
| 〃 첫 DWORD = `0xc0000023` | `+0x6e0 ~ +0x6ef` | **`Information` 이 정확히 8** (아니면 `0xc00000e5`) 이어야 함. 출력 `+4` 의 DWORD 가 **필요한 크기** (`+0x6f5 MOV R15D,[R13+4]`) → 그 크기로 다시 할당해 **IOCTL 다시** (루프, 횟수 제한 없음). 호출자 버퍼면 `0xc0000023` + 크기 돌려주고 끝 |
| 〃 첫 DWORD ≠ `0xc0000023` | `+0x78e CMP EAX,4; +0x791 JNZ +0x7bd` | **`Information` 이 정확히 4** (아니면 `0xc00000e5`). 그 DWORD 를 **진짜 status 로 삼아 그대로 반환** (음수면 실패, 양수면 성공 처럼) |
| 그 밖의 음수 | `+0x6c9 JNZ +0x7b3 → +0x708 ... +0x720 JMP +0x669` | **그대로 반환** (예: `0xc0000008`, `0xc0000225`). 내가 할당한 출력 버퍼는 free, `*ppBuffer` 는 NULL 유지 |

- "몇 번째 칸을 크기로 읽나" → **8바이트 출력의 `+4` DWORD** (`0x80000005` 모드일 때), 또는 `IOSB.Information` (성공 모드일 때).
- **출력 안에 전체 크기 칸·표지(`0x1a2b3c4d`) 검사는 없다.** 길이는 오직 `IOSB.Information`. `[정적]`

#### 2-b. 출력 칸 표 (성공 모드, 전부 `[정적]`)

머리 (`CRYPT_PROVIDER_REFS` 꼴, `BCryptResolveProviders +0x481 ~ +0x4fb`, `+0x7d4 ~ +0x7f8`):

| 오프셋 | 크기 | 뜻 | 검사 → 실패 | 
|---|---|---|---|
| `+0x00` | 4 | **공급자 개수** `cProviders` | `Information < 0x10` → 실패 (`+0x4a0 ~ +0x4b0`) |
| `+0x04` | 4 | 안 읽음 (성공 모드). `0x80000005` 모드에선 "필요한 크기" 자리 | — |
| `+0x08` | 8 | **포인터 배열의 오프셋** `rgpProviders`, 또는 `-1` | `-1` 이면 개수 **== 0** 이어야 함 (`+0x4c7 ~ +0x4ce`). 아니면 개수 ≠ 0 (`+0x7d4 TEST R13D`), `오프셋 < Information` (`+0x7d9`), `오프셋 + 개수×8 ≤ Information` (`+0x7de ~ +0x7ea`) |

포인터 배열 (`+rgpProviders` 부터, 8바이트 × 개수): 각 칸 = **공급자 칸의 오프셋** 또는 `-1` (=NULL, 개수 안에 끼면 뒤에서 크래시하니 쓰지 말 것).

공급자 칸 `CRYPT_PROVIDER_REF` — **`0x38` 바이트** (`IoUnpack_ProviderRef +0x39 LEA RCX,[RAX+0x38]`):

| 오프셋 | 크기 | 뜻 | 검사 (`IoUnpack_ProviderRef`) |
|---|---|---|---|
| `+0x00` | 4 | `dwInterface` (1~8) | 여기선 안 봄. **`BCryptOpenAlgorithmProvider +0x3b7` 가 `1..8` 인지 봄** (아니면 `0xc0000008`) |
| `+0x08` | 8 | `pszFunction` (= 알고리즘 이름) 오프셋 / `-1` | `+0x62 ~ +0x1c3`: 오프셋 `< Information`, **NUL 로 끝나는 UTF-16 이 `Information` 안에 들어와야** (`SzString_CbLength` + `SzString_CchLength`) |
| `+0x10` | 8 | `pszProvider` 오프셋 / `-1` | `+0x1c8 ~ +0x217` 같은 검사 |
| `+0x18` | 4 | `cProperties` | — |
| `+0x20` | 8 | `rgpProperties` 오프셋 / `-1` | `-1` 이면 `cProperties == 0` (`+0x150 ~ +0x160`). 아니면 `cProperties ≠ 0`, 오프셋 `< Information`, `오프셋 + 개수×8 ≤ Information` (`+0x21f ~ +0x234`) |
| `+0x28` | 8 | **`pUM`** (유저 모드 이미지 칸) 오프셋 / `-1` | `IoUnpack_ImageRef` (아래). **-1 (NULL) 로 두면 `LoadProviderEx` 가 널 역참조** (4-d) |
| `+0x30` | 8 | `pKM` (커널 모드 이미지 칸) 오프셋 / `-1` | 같음. 유저 길에선 안 읽음 (`-1` 가능) |

이미지 칸 `CRYPT_IMAGE_REF` — **`0x10` 바이트** (`IoUnpack_ImageRef +0x31 LEA RCX,[RAX+0x10]`): `+0x00` 이미지 이름 문자열 오프셋 / `-1` (NUL 문자열 검사), `+0x08` 4바이트 `dwFlags` (unpack 은 안 봄). 속성 칸 `CRYPT_PROPERTY_REF` — **`0x18`**: `+0x00` 속성 이름, `+0x08` 값 크기 (4), `+0x10` 값 오프셋 / `-1` (`IoUnpack_PropertyRef +0x26 LEA RCX,[RAX+0x18]`, `IoUnpack_SG_Buffer`: 값 크기 0 인데 오프셋이 `-1` 아님이면 실패).

- 각 검사에서 걸리면 **Win32 `0x54f`** (또는 문자열이 안 끝나면 `0x7a`) → `+0x74b` 에서 내가 할당한 출력 버퍼 free → `WinErrorToNtStatus` → **`0xc00000e5`** (`0x7a` 는 `0xc0000023`).

#### 2-c. 포인터 고치기·범위 검사

- **한다.** 출력 안 모든 칸은 "출력 시작 기준 오프셋" 으로 와서 bcrypt 가 `시작 주소 + 오프셋` 으로 바꿔 쓴다: 배열 `+0x7ec ADD RAX,R15; +0x7ef MOV [R15+8],RAX`, 공급자 `IoUnpack_ProviderRef +0x4f LEA RDI,[RAX+RDX]`, 문자열 `+0x1c0 ADD RCX,RSI` / `+0x1c3`, 이미지 `IoUnpack_ImageRef +0x47`. `-1` → NULL (`+0x4c7 MOV [R15+8],RBX` 등).
- **범위 검사 있음.** 모든 오프셋이 `< Information`, `오프셋 + 칸 크기 ≤ Information`, 더하기 오버플로 검사 (`JC`/`JA`) → 어긋나면 `0x54f` → **`0xc00000e5`**. 정렬 검사는 없음 (8바이트로 맞출 필요 없음. 그래도 8바이트씩 맞추는 게 안전).
- **지금 우리:** 기본 대답은 **여기서 걸린다** — `Information 0` → `+0x4b0 JA`, 0 채움 → `+0x7d7 JZ`, 둘 다 `+0x7f8`. 로그와 맞음. `[정적]` 설명 / 에뮬레이터가 실제로 어느 쪽인지는 `[동적]` 아닌 에뮬레이터 로그로.

---

### 3. `0xc00000e5` 는 어디서 나오나 + 실패 status 표

`0xc00000e5` 는 **bcrypt 안에서 `STATUS_INTERNAL_ERROR` 를 직접 쓰는 자리** 와 **Win32 `0x54f` (`ERROR_INTERNAL_ERROR`) 를 `WinErrorToNtStatus` 가 바꾸는 자리** 두 갈래다. 변환식: `WinErrorToNtStatus +0x4d ~ +0x70` (`0x491 → 0xc0000272`, `0x4db → 0xc0000160`, **`0x54f → 0xc00000e5`**).

**(가) `BCryptResolveProviders` (요청서 3의 1번)**

| status | 자리 | 조건 |
|---|---|---|
| **`0xc00000e5`** | **`+0x7f8 MOV R12D,0x54f` → `+0x74b` → `+0x3ba`** | ① `Information < 0x10` (`+0x4a0 ~ +0x4b0`) ② 개수 0 인데 배열이 `-1` 아님 (`+0x7d4 ~ +0x7d7`) ③ `-1` 인데 개수 ≠ 0 (`+0x4ce`) ④ 배열 오프셋·끝이 `Information` 밖 (`+0x7d9 ~ +0x7ea`) ⑤ `IoUnpack_*` 가 `0x54f` 를 돌려줌 (`+0x500 JNZ +0x74b`) |
| **`0xc00000e5`** | `+0x664 MOV ESI,0xc00000e5` | IOCTL status `0xc0000023` (`+0x6be`), 또는 `0x80000005` 인데 `Information` 이 `4`/`8` 이 아님 (`+0x6d6`, `+0x6ef`, `+0x791`) |
| `0xc00000e5` | `IoCallKernelDriver +0x133` (질의-크기 길만) | 입력 `< 8` 바이트, 또는 같은 `0x80000005` 규칙 어긋남 |
| `0xc000000d` | `+0x80 JZ +0x457`, `+0x98 JZ`, `+0x451` → `R12D=0x57` → `+0x3ba` | `ValidateInterfaceId` 실패, `pcbBuffer` NULL, `dwMode ∉ {1,2,3}` |
| `0xc000000d` | `+0xaa JNZ +0x79d` (`R12D=0x3ec`) | `dwFlags & ~3 ≠ 0` |
| `0xc000000d` / `0xc0000023` | `SzString_CchLength` 실패 (`+0xe5`·`+0x11d`·`+0x185`·`+0x1e2`·`+0x2f3`·`+0x33c` → `+0x767`) | 입력 문자열 NULL(`0x57`) / 65535 글자 안에 NUL 없음(`0x7a` → `0xc0000023`) |
| `0xc0000017` | `+0x385 ~ +0x393` / `+0x462 MOV ESI,0xc0000017` | 입력 (`> 0x140`) 또는 출력 버퍼 `BCryptAlloc` 실패 |
| **IOCTL 의 음수 그대로** | `+0x6c9 JNZ +0x7b3 → … → +0x669` | `0xc0000023`·`0x80000005` 아닌 모든 음수 (`_IoOpenDevice` 가 음수여도 같은 길) — **커널 쪽이 "없음" 을 알리는 정상 방법** |
| `0xc0000023` | `+0x72a MOV ESI,0xc0000023` (호출자 버퍼 길) | 호출자 버퍼가 모자람 + `*pcb` = 필요 크기 |

**(나) `IoUnpack_*`** — 반환은 **Win32 오류 숫자** (status 아님): `0x54f` (범위·`-1` 규칙 위반: `IoUnpack_ProviderRef +0x242`, `IoUnpack_ImageRef +0x179`, `IoUnpack_PropertyRef +0x7d`, `IoUnpack_SG_*`), `0x7a` (문자열 칸 남은 크기가 1바이트: `+0xe4`, `+0x96`, `+0xaf`).

**(다) `BCryptResolveProvidersForBcoapCached`** — **새 status 를 안 만든다.** 캐시 노드 할당 실패 (`+0x13e JZ`, 로그만 `STATUS_NO_MEMORY`) 나 `StringCchCopyW` 실패 (`+0x157 JNZ +0x22d`, 로그만 `0xc0000023`, 이름이 19글자를 넘을 때) 는 **status 0 으로 두고 캐시만 안 한다.** 반환 = `BCryptResolveProviders` 의 값 그대로 (`+0xd8 MOV EBP,EAX`, `+0x124`).

**(라) `BCryptOpenAlgorithmProvider`**

| status | 자리 | 조건 |
|---|---|---|
| `0xc000000d` | `+0xd1e` (`+0xca JZ`, `+0xd3 JZ`, `+0xdf`) | `phAlgorithm`/`pszAlgId` NULL, `dwFlags & 1` |
| (그대로) | `+0x263 JNS` 안 탐 → `+0x25e MOV R15D,EAX` | `BcoapCached` 가 음수 → 로그 후 그대로 반환 |
| **`0xc0000008`** | **`+0x3b7 JBE` 안 탐 → `+0x3d4 MOV R15D,0xc0000008`** | **첫 공급자의 `dwInterface` 가 `1..8` 밖** (`+0x3ac MOV EAX,[RDX]; +0x3b2 DEC EAX; +0x3b4 CMP EAX,7; +0x3b7 JBE`). `_LogBCryptOpenProviderFailure` (`+0x3cf`) 도 부름 |
| `0xc0000017` | `+0x432` | 핸들 객체 `BCryptAlloc(0x3c8)` 실패 |
| **`0xc00000e5`** | **`+0xaaf` / `+0xb65`** | 공급자를 다 올려 봤는데 인터페이스 2 (hash) / 3 (비대칭) 인데 속성 조회 함수 (`RBX`) 가 NULL — **공급자 DLL 쪽 문제**, 이름만 |
| 그대로 | 루프 `+0x4dd ~ +0xa84` | `LoadProviderEx` 실패 → 다음 공급자 시도 → 다 실패면 마지막 오류. 아래 4-d |
| `0xc00000e5` (`LoadProviderEx +0x37`) | `CMP [g_fLoaderInitialized],ESI; +0x35 JNZ` 안 탐 | `g_fLoaderInitialized` (rva `0x25b2c`) == 0. **attach 의 `InitializeLoader +0x22` 가 1 로 씀** (`refs`). attach 가 TRUE 로 끝났으면 1 |
| **(새로 찾음)** `OpenSystemPreferredAlgorithmProvider +0x174` | `+0x106 CMP [RBX],1; +0x109 JC` | `GenRandom` 길에서 공급자 개수 0 → `0xc00000e5` |

- 그 밖에 `OpenSystemPreferredAlgorithmProvider +0x100 JS` (Resolve 음수 → 그대로), `GetSystemPreferredRngHandle +0x81 JS` (그대로 위로) 는 새 status 를 안 만든다.
- **지금 우리:** 로그의 `0xc00000e5` 는 (가) 첫 줄 하나로 설명된다. `[정적]`

---

### 4. 최소 조건 표 (가장 중요)

전제: attach 가 TRUE 로 끝난 상태 (sast/88 5번 표 전부 만족: `g_hIoDevice ≠ 0`, `g_TrustedEnvironment & 1`, `g_fLoadCNGDone = 1`, `g_fLoaderInitialized = 1`).

#### 4-a. 최소 조건 표 (sast/88 질문 5 표와 같은 칸 순서)

| # | syscall (또는 읽는 칸) | 입력 | 기대 답 | 안 맞으면 가는 자리 |
|---|---|---|---|---|
| 1 | `NtDeviceIoControlFile` **(공급자 질의)** | 핸들 = `g_hIoDevice`, Event·APC·Context 0, 코드 **`0x390400`**, 입력 `0x30`/`0x38`/… (1-c 표, 기능 `0x20000`), 출력 = bcrypt 힙 `0x180` 바이트 (`+0x5de ~ +0x5eb`) | **status ≥ 0 (0 권장)** | 음수 → 그대로 반환 (또는 `0xc0000023`→`0xc00000e5`, 2-a) |
| 2 | 위 IOCTL 의 `IO_STATUS_BLOCK` | `[RSP+0x78]` (16바이트, 0 으로 시작) | **`Information` = N**, `0x10 ≤ N ≤ 0x180`. **`Status` 칸은 안 읽음** | `N < 0x10` → `+0x4b0 JA` → `+0x7f8` → **`0xc00000e5`** (**지금 우리**). `N > 0x180` → bcrypt 가 N 으로 다시 할당해 IOCTL 다시 (두 번째엔 N 이상 크기를 받으니 같은 답을 주면 됨) |
| 3 | 위 IOCTL 의 **출력 버퍼** (`[RSP+0x40]`) | — | 처음 N 바이트 = 4-b 의 한 장 (공급자 1개) | 칸 어긋남 → `0x54f` → **`0xc00000e5`**. `dwInterface ∉ 1..8` → `0xc0000008` |
| 4 | **`LoadLibraryExW`** (ntdll `Ldr` — 우리 로더 몫) | `C:\Windows\System32\bcryptprimitives.dll` (`GetSystemDirectoryW() + "\" + 출력의 이미지 이름`, `_GetImagePath +0x2e/+0x5e`, `_LoadImage +0x9d`) | **성공 + 그 DLL 의 attach 통과** (sast/87) | 실패 → `_LoadImage +0x18c` `0xc0000135` (`GetLastError == 0x7e`) / `+0x176` `0xc000007b` → 다음 공급자 시도 → 다 실패면 그 status |
| 5 | `GetProcAddress` (그 DLL 의 `Get…Interface`) | `_LoadInterface +0x130` | 내보내기가 있어야 (이름 목록은 `[공백]`, 5번 다음 요청 후보) | 이름만 |
| 6 | 공급자 DLL 안 함수 표 검사·열기 함수 (`ValidateFunctionTable` `+0x70b`, 간접 호출 `+0x851`·`+0x883`) | — | 이름만 (공급자 몫) | 이름만 |

- **`BcoapCached` 를 지나 성공 쪽으로 가는 최소 = 1·2·3 행.** 4행부터는 "그 뒤 어떤 DLL 을 올리나" (4-d).
- **status 0 이면 충분**하다. `0x103` 같은 양수도 성공으로 치지만 권하지 않음.

#### 4-b. 공급자 하나를 돌려주는 **가장 작은 출력** (한 장, `BCryptOpenAlgorithmProvider(L"AES", NULL, 0)` 기준 `0x98` 바이트)

`IOSB.Information = 0x98`. 아래 값을 `rva 시작` = 출력 버퍼 시작 기준 오프셋으로 놓는다. (`bcryptprimitives.dll` 은 sast/87 의 그 DLL — `bcrypt` 의 공급자 기본값 `[추측]`.)

| 오프셋 | 크기 | 값 | 뜻 | 검사 줄 |
|---|---|---|---|---|
| `0x00` | 4 | `1` | 공급자 개수 | `+0x4ba MOV R13D,[R15]`, `+0x7d4 TEST R13D; JZ` 통과 |
| `0x04` | 4 | `0` | 안 읽음 | — |
| `0x08` | 8 | `0x10` | 포인터 배열 오프셋 (`≠ -1`) | `+0x4bd CMP RAX,-1; JNZ +0x7d4` |
| `0x10` | 8 | `0x18` | 배열 `[0]` = 공급자 칸 오프셋 | `IoUnpack_ProviderRef +0x30 ~ +0x49` (`0x18 < 0x98`, `0x18+0x38 ≤ 0x98`) |
| `0x18` | 4 | `1` (AES = 암호; 알고리즘마다 다름, 아래) | `dwInterface` | `BCryptOpenAlgorithmProvider +0x3ac ~ +0x3b7` (`1..8`) |
| `0x1c` | 4 | 0 | 안 씀 | — |
| `0x20` | 8 | `0x60` | `pszFunction` = `"AES"` 오프셋 | `+0x62 ~ +0x1c3` |
| `0x28` | 8 | `-1` | `pszProvider` 없음 (로그에만 씀. 안전하게 `L"Microsoft Primitive Provider"` 를 넣어도 됨 `[추측]`) | `+0x1c8 ~ +0x1d8` |
| `0x30` | 4 | `0` | `cProperties` | `+0x14c MOV EDI,[R14+0x18]` |
| `0x34` | 4 | 0 | 안 씀 | — |
| `0x38` | 8 | `-1` | `rgpProperties` 없음 (개수 0 이므로 `-1` **필수**) | `+0x150 ~ +0x160` |
| `0x40` | 8 | `0x50` | `pUM` = 이미지 칸 오프셋 (**NULL 불가**) | `IoUnpack_ImageRef +0x28 ~ +0x41` |
| `0x48` | 8 | `-1` | `pKM` 없음 | 같음 (`-1` → NULL) |
| `0x50` | 8 | `0x68` | 이미지 칸 `+0`: 이미지 이름 오프셋 | `IoUnpack_ImageRef +0xe1 ~ +0x12f` |
| `0x58` | 4 | `0` | 이미지 칸 `+8` `dwFlags` | 안 봄 |
| `0x60` | 8 | `L"AES\0"` | 알고리즘 이름 (UTF-16, NUL 포함 8바이트) | 이 이름이 `wcsncpy_s` 로 핸들 안에 복사됨 (`BCryptOpenAlgorithmProvider +0x4bb`) |
| `0x68` | 42 → 48 | `L"bcryptprimitives.dll\0"` + 0 채움 | 이미지 이름 | `LoadProviderEx +0xb0` / `_LoadImage +0x2c ~ +0x45` |

(마지막 문자열이 `0x68 + 0x2a = 0x92` 에서 끝나고, 8바이트로 올려 `0x98`.) 이 바이트열을 파이썬 모델 (`IoUnpack_*` 검사 줄을 옮긴 것) 로 돌려 보면 `ERROR` 없이 통과하고, `Information = 0` 이나 `0x180` 영 채움은 `0x54f` 로 걸린다. `[정적]` 손 검산.

- **`dwInterface` 는 알고리즘에 맞아야 한다:** 이 값이 **핸들의 `+0x24`** 에 들어가고 (`BCryptOpenAlgorithmProvider +0x498 MOV [R15+0x24],EAX`), `BCryptGenRandom +0x5a CMP [RAX+0x24],6` 이 `6` (난수) 을 요구한다. 코드가 확인한 번호: **6 = 난수** (`OpenSystemPreferredAlgorithmProvider +0xeb LEA EDX,[R9+6]`), **2 = hash** (`+0xaa1 CMP EAX,2` + 문자열 `HashBlockLength`), **3 = 비대칭 암호화** (`+0xb57 CMP EAX,3` + `PaddingSchemes`). 1 = 암호 (공개 헤더 기억 `[추측]`). 같은 이름이 여러 인터페이스에 걸치는 알고리즘 (`RSA` 등) 의 선택은 커널 몫이라 이번엔 안 봄 `[공백]`.
- **`GenRandom` 질의 (B3, 입력 `0x30`, 인터페이스 6) 의 답:** 같은 모양인데 `dwInterface = 6`, **`pszFunction` 이 NULL 이면 안 된다** (`OpenSystemPreferredAlgorithmProvider` 가 그 문자열을 `BCryptOpenAlgorithmProvider` 의 알고리즘 이름으로 넘기는데, NULL 이면 `0xc000000d`). 이름은 `L"RNG"` 가 자연스럽다 `[추측]` (CNG 안 된 때 이 코드가 쓰는 이름이 `RNG`, `+0x36 LEA RDX,[0x18001f128]` = PE 바이트로 확인).

#### 4-c. 공급자가 "0 개" 인 답이면 어디로 가나

| 답 | 가는 자리 | 결과 |
|---|---|---|
| status 0, `Information ≥ 0x10`, 개수 `0`, 배열 `-1` | `BCryptResolveProviders` 는 **status 0 으로 통과** (`+0x4c7 ~ +0x4da JZ +0x3d0` (끝맺음)). `BcoapCached` 가 **그 답을 캐시에도 넣음** (`+0x163 ~ +0x177`). `BCryptOpenAlgorithmProvider +0x3a5 MOV RAX,[RAX+8]; +0x3a9 MOV RDX,[RAX]` → `RAX = 0` | **널 역참조 (정적 읽기 — 크래시)**. 캐시에 박혀서 같은 이름은 계속 터짐. **쓰지 말 것** |
| 〃 (GenRandom 질의 B3) | `OpenSystemPreferredAlgorithmProvider +0x106 JC +0x174` | **`0xc00000e5`** (실패 status 로 끝, 크래시 아님) |
| 개수 `0` + 배열 `≠ -1` | `+0x7d4 TEST R13D; +0x7d7 JZ +0x7f8` | `0xc00000e5` |
| "그런 알고리즘 없음" 을 알리고 싶다 | IOCTL 자체를 **음수 status** 로 (`STATUS_NOT_FOUND 0xc0000225` 가 자연스러움 `[추측]` — bcrypt 의 `WinErrorToNtStatus` 가 `0x490` → `0xc0000225` 로 바꾸니 같은 계열) | `BCryptResolveProviders` 가 그대로 반환 → `BCryptOpenAlgorithmProvider` 가 그대로 반환 (`+0x263 JNS` 안 탐). **실패는 이쪽이 정상 길** |

#### 4-d. 그 뒤 어떤 DLL 을 올리나

- **출력에서 나온다.** `BCryptOpenAlgorithmProvider +0x514 MOV RCX,[RSI+RAX]` (= 배열 `[i]` 공급자 칸) → **`+0x518 LoadProviderEx(공급자 칸, 출력 시작, …)`**. 공급자 칸 `+0x28` → 이미지 칸 `+0` → 이름 (`LoadProviderEx +0xb0 MOV RCX,[R15+0x28]; +0xb7 MOV RCX,[RCX]`, `_LoadImage +0x2c MOV RDX,[RCX+0x28]; +0x42 MOV RDX,[RDX]; +0x45 RtlInitUnicodeString`).
- **이름 처리:** `_LoadImage +0x59` `_GetImagePath` = **`GetSystemDirectoryW()` + `\` + 이름** (`_GetImagePath +0x2e`, `+0x5e`, `+0x70 MOV word ptr [RSI+RBX*2],0x5c`). 그다음 **`LoadLibraryExW(경로, 0, 0)`** (`_LoadImage +0x9d`). 예외: 이름이 `bcrypt.dll` (대소문자 무시, `+0x7b RtlCompareUnicodeString`) 이면 **LoadLibrary 안 하고 `g_hInstance` 를 그대로** 씀 (`+0x140`). 이미 올린 공급자 목록 `g_pLoadedProviderList` (rva `0x25b80`, 락 `g_csLoaderLock`) 에서 같은 이름 (`_wcsicmp`) 이 있으면 다시 안 올림 (`LoadProviderEx +0xb0 ~ +0xd1`).
- `pUM` 이 NULL 이면 `LoadProviderEx +0xb7` / `_LoadImage +0x42` 가 널 역참조.
- 그다음은 이름만: `_LoadInterface` (`GetProcAddress` 한 번 `+0x130`, 이름 후보는 bcrypt 안 문자열로 `GetCipherInterface`·`GetHashInterface`·`GetAsymmetricEncryptionInterface`·`GetSecretAgreementInterface`·`GetSignatureInterface`·`GetRngInterface`·`GetKeyDerivationInterface` 등. 어느 번호가 어느 이름인지는 안 읽음 `[공백]`), `ValidateFunctionTable` (`BCryptOpenAlgorithmProvider +0x70b`), 공급자 열기 함수 간접 호출 (`+0x851`, `+0x883`), 인터페이스 2·3 이면 속성 조회 (`+0xb19`, `+0xbcd`).
- 성공이면 `*phAlgorithm = 핸들` (`+0xd0f` 근처), `+0x3c8` 바이트 객체. 캐시 아닌 refs 는 맨 끝 `BCryptFreeBuffer` (`+0xf97`).

- **지금 우리:** 1·2·3 행을 못 채워서 거기서 `0xc00000e5`. 4 행 이후는 아직 못 감 — 올릴 DLL 은 `bcryptprimitives.dll` (sast/87 로 attach 는 이미 통과 `[관찰 — 에뮬레이터]`).

---

### 5. (작게) 캐시

- **있다.** 전역 **`g_pResolveProviderCache`** (rva `0x25c10`). `BcoapCached` 만 읽고 쓴다 (`refs`: `+0x2e`, `+0x16a`, `+0x177 CMPXCHG.LOCK`). 연결 리스트, 새 노드를 머리에 달기 (`+0x171 MOV [RSI],RAX`).
- **키 = 알고리즘 이름 문자열** (`+0x50 ~ +0x62` 가 대소문자 구분 `wcscmp` 꼴로 비교). 노드 `0x40` 바이트: `+0x00` 다음 노드, `+0x08` 결과 refs 포인터, `+0x10` 크기 `cb`, **`+0x14` 이름 (최대 19글자 + NUL, `StringCchCopyW(…, 0x14, …)` `+0x150`)**. 이름이 20글자 이상이면 캐시 못 하고 (`+0x157 JNZ +0x22d`) 노드 free.
- **조건:** 공급자 NULL **그리고** 플래그 == 2 (`+0x23 ~ +0x2c`) — 즉 `BCryptOpenAlgorithmProvider(&h, 이름, NULL, 0)` 이고 `g_fLoadCNGDone != 0` 일 때뿐. 공급자 이름을 직접 주면 (`MS_PRIMITIVE_PROVIDER` 등) **매번 IOCTL**.
- 성공 (status ≥ 0) 일 때만 넣는다 (`+0x12c JS +0x18d`). 지우는 코드 없음 (이 모듈 안에서 이 전역을 쓰는 곳이 위 셋뿐). 캐시 hit 이면 호출자에게 "free 하지 마" 표시 (`+0x90 MOV [RAX],DIL`, `DIL=0`).
- **IOCTL 횟수 가늠:** 알고리즘 이름 서로 다른 개수만큼 (≤19글자, 공급자 NULL 일 때) + `GenRandom` 이면 질의 B3 한 번 (그 결과 핸들도 전역 캐시 `0x25ad0`). B3 의 refs 는 캐시 아님 (`OpenSystemPreferredAlgorithmProvider +0x75 BCryptFreeBuffer`).
- **지금 우리:** 우리 쪽에선 상관없음 (캐시는 bcrypt 안). 다만 **빈/오류 답을 한번 성공 status 로 주면 캐시에 박힌다** — 4-c 첫 줄.

---

## 동적분석으로 확인해야 할 것

실행 때만 정해지는 것. 칸만 적음 (`[추측]` 은 값 짐작). 모듈 이름 `bcrypt`. 모든 멈춤은 `test.exe` 일 때만.

- **진짜 KsecDD 가 주는 출력 (제일 중요):** `BCryptOpenAlgorithmProvider(L"AES")` 질의의 `IOSB.Information` (N), status, 출력 바이트 전체. 특히 `dwInterface` (`+0x18`), `pszProvider` 가 채워지나, `cProperties` 가 0 인가 (진짜는 속성이 몇 개 올 수 있음 `[추측]`), `pKM` 이 `-1` 인가, 공급자가 **여러 개** 오나 (그러면 `+0x4dd` 루프가 차례로 `LoadProviderEx`). N 이 `0x180` 을 넘어 한 번 재시도하나 (IOCTL 두 번이 보이면).
- **`GenRandom` 질의 (인터페이스 6)** 의 진짜 답: 공급자 이름 문자열 (`pszFunction`) 이 `RNG` 인지.
- **진짜 입력과 우리 계산식이 맞나:** 같은 호출을 입력 길이 `0x38`/`0x30`/(`0x78` 후보) 로 한 번씩.
- **캐시:** 같은 `test.exe` 안에서 같은 이름을 두 번 열면 두 번째엔 IOCTL 이 안 나는가.
- **`LoadLibraryExW` 에 넘어가는 경로** (실제로 `C:\Windows\System32\bcryptprimitives.dll` 인지).
- 추천 멈춤점 (모두 `test.exe` 조건, ret_off = 콜스택의 `+0x`):
  - `bcrypt!BCryptResolveProviders +0x5f2` (= `NtDeviceIoControlFile` 반환 직후): `eax` (status), IOSB `[rsp+0x78]` (`dq @rsp+78 L2`), 입력 `[rsp+0x30]` 가 가리키는 `[rsp+0x38]` 바이트, 출력 `[rsp+0x40]` 가 가리키는 `poi(@rsp+80)` 바이트. **한 번에 위 모든 칸이 나온다.**
  - `bcrypt!BCryptResolveProviders +0x500` (= `IoUnpack_ProviderRef` 반환 직후): `eax` (Win32 오류 0 이어야).
  - `bcrypt!BCryptOpenAlgorithmProvider +0x25e` (= `BcoapCached` 반환 직후): `eax`, `[rbp-0x68]` (refs), `[rsp+0x48]` (cb). 캐시 hit 면 `BcoapCached +0x120` 이 안 걸림 (확인용: `bcrypt!BCryptResolveProvidersForBcoapCached +0x120` 도 걸어서 횟수).
  - `bcrypt!OpenSystemPreferredAlgorithmProvider +0xfc` (= 질의 B3 반환 직후): `eax`, `[rbp+0x30]`.
  - `bcrypt!BCryptOpenAlgorithmProvider +0x51d` (= `LoadProviderEx` 반환 직후): `eax`.
  - `bcrypt!_LoadImage +0xa4` (= `LoadLibraryExW` 반환 직후): `rcx` 는 이미 깨졌으니 직전 `+0x9d` 에서 `rcx` (경로).

## 다음 요청 후보

1. **(정적, 작게 — 이 요청 다음 벽 후보)** `_LoadInterface` · `ValidateFunctionTable` · `BCryptOpenAlgorithmProvider +0x851/+0x883` 간접 호출: **공급자 DLL (`bcryptprimitives`) 이 어떤 이름을 내보내야 하고 (`Get…Interface` ↔ `dwInterface` 번호), 함수 표에서 bcrypt 가 무엇을 검사하나.** 질문 4-a 의 5·6 행이 비어 있다. (sast/87 의 bcryptprimitives 지도와 이어서.)
2. **(정적)** `BCryptEnumProviders` · `BCryptEnumAlgorithms`: `ppBuffer` 를 NULL 로 넘기는 크기 질의 길이 샘플에서 쓰이나 (`IoCallKernelDriver` 길이 실제 쓰이는지).
3. **(동적)** 위 추천 멈춤점 — 특히 `+0x5f2` 한 곳이 진짜 답 전부를 준다.
4. **(정적, 후순위)** `ksecdd.sys` 의 `0x390400` / 기능 `0x20000` 처리 (Q173b ③) — 진짜 답을 정적으로 보고 싶을 때.
5. **(정적, 후순위)** Q173b ① (`GetModuleHandleExW`·`LdrAddRefDll`), ④ (`NtClose` ×5, `NtTraceControl` ×4) — 이번에도 안 함.

## 근거 파일

- `ms/disassemble/bcrypt/BCryptResolveProviders.asm` (`+0x79 ~ +0x7e`, `+0x86 ~ +0xaa`, `+0xb0 ~ +0x137`, `+0x15c ~ +0x243`, `+0x253 ~ +0x2d5`, `+0x372 ~ +0x3c7`, `+0x481 ~ +0x512`, `+0x514 ~ +0x64d`, `+0x6b9 ~ +0x7bd`, `+0x7d4 ~ +0x7f8`, `+0x803`)
- `ms/disassemble/bcrypt/IoUnpack_ProviderRef.asm`, `IoUnpack_ImageRef.asm`, `IoUnpack_PropertyRef.asm`, `IoUnpack_SG_SzString.asm`, `IoUnpack_SG_Buffer.asm`, `SzString_CchLength.asm`, `SzString_CbLength.asm`, `ValidateInterfaceId.asm`, `WinErrorToNtStatus.asm` (`+0x4d ~ +0x70`)
- `ms/disassemble/bcrypt/BCryptResolveProvidersForBcoapCached.asm` (전체), `BCryptOpenAlgorithmProvider.asm` (`+0xc7 ~ +0x263`, `+0x39c ~ +0x4e5`, `+0x514 ~ +0x53c`, `+0xa96 ~ +0xb57`), `OpenSystemPreferredAlgorithmProvider.asm` (전체), `GetSystemPreferredRngHandle.asm` (`+0x0 ~ +0x9f`), `BCryptGenRandom.asm` (`+0x51 ~ +0x16d`), `IoCallKernelDriver.asm` (`+0x0 ~ +0x1f9`)
- `ms/disassemble/bcrypt/LoadProviderEx.asm` (`+0x21 ~ +0xd6`), `_LoadImage.asm` (`+0x2c ~ +0xa4`, `+0x140`), `_GetImagePath.asm` (`+0x2e ~ +0x99`)
- 지도·참조: `ms/disassemble/bcrypt/_calls.tsv`, `_map/entry_edges.tsv`, `_refs/0x25b2c.tsv`, `_refs/0x25c10.tsv`, `_refs/0x25898.tsv`
- PE 바이트: rva `0x1f8f0` `Microsoft Primitive Provider`, `0x1f128` `RNG`, `0x1f080` `bcrypt.dll`, `0x1f368` `NULL`
- 검산: 파이썬 모델 (`IoUnpack_*` 검사 옮겨 적기) — 4-b 의 `0x98` 바이트가 통과, `Information 0` / `0x180` 영 채움이 `0x54f` 로 걸림 (임시 파일, 레포 밖)

## 판정

**분석성공.** 요청서 5개 질문 모두 `.asm` 줄로 답함. 로그의 입력 `0x38`/`0x30`·출력 `0x180`·결과 `0xc00000e5` 가 코드로 한 줄도 안 어긋나게 설명됨 (`+0x7f8` → `0x54f` → `0xc00000e5`). 질문 4 의 최소 답 한 장 (4-b) 은 코드의 검사 줄을 하나씩 옮겨 검산함. **못 닫은 것:** 진짜 KsecDD 가 어떤 답을 주는지 (공급자 속성·여러 개·`pszProvider`) = 동적, 공급자 DLL 이 내보내야 할 이름 (`_LoadInterface` 이후) = 다음 요청 1번, `BCryptEnumProviders` 쪽 NULL 길 = 다음 요청 2번.
