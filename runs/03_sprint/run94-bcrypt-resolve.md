# 2026-11-15 run94 — bcrypt 공급자 찾기 IOCTL (`BCryptResolveProviders`, 코드 `0x390400` · 기능 `0x20000`) 의 입출력 모양과 성공 최소 조건 (Q173a = run92 남은 정적 ②, PE_emulator 외부 피드백 — 한 바퀴로 끝): **에뮬레이터 로그의 입력 `0x38` · `0x30`, 출력 `0x180`, 결과 `0xc00000e5` 가 코드로 한 줄도 안 어긋나게 설명된다. 기본 대답 (status 0 인데 `IOSB.Information` 0, 또는 0 으로 채운 `0x180`) 은 `BCryptResolveProviders +0x4b0 JA` / `+0x7d7 JZ` → `+0x7f8 MOV R12D,0x54f` (Win32 `ERROR_INTERNAL_ERROR` `[추측: 이름]`) → `+0x3ba WinErrorToNtStatus` → `0xc00000e5` (`WinErrorToNtStatus +0x70`). 입력은 기능 `0x20000` (`+0x590`) 이고 길이 = `0x30 + Σ((글자수 × 2 + 9) & ~7)` — `0x38` = 이름 하나 (3글자 이하, `AES` 등), `0x30` = 이름 없음 (`BCryptGenRandom` 길의 인터페이스 6 질의). 출력 `0x180` 은 첫 시도 고정 크기 (`+0x7a8`). bcrypt 는 길이를 `IOSB.Information` 하나로만 믿고 (`IOSB.Status` 는 안 읽음), 출력은 "머리 `0x10` · 포인터 배열 · 공급자 칸 `0x38` · 이미지 칸 `0x10` · 문자열" — 칸은 모두 출력 시작 기준 오프셋, `-1` = 없음, 범위 검사 있음. 공급자 하나를 돌려주는 가장 작은 답 = `0x98` 바이트. 공급자 0 개 답은 쓰면 안 된다 (`BCryptOpenAlgorithmProvider +0x3a5 · +0x3a9` 가 검사 없이 첫 칸을 읽음) — "없음" 은 IOCTL 음수 status 로. 올릴 DLL 은 출력의 이미지 이름 → `LoadProviderEx` → `_LoadImage +0x9d LoadLibraryExW(시스템 폴더 + "\" + 이름)`. 이름 하나당 IOCTL 한 번 (캐시 `g_pResolveProviderCache`, rva `0x25c10`).** `BCryptOpenAlgorithmProvider +0x259 BCryptResolveProvidersForBcoapCached` (`+0x11b` 캐시 길 / `+0xcf` 그 밖) → `BCryptResolveProviders` (`+0x253 TEST · +0x256 JNZ +0x514` 직접 길 → `+0x547 BCryptAlloc(0x180)` → `+0x58a · +0x590` 머리 → `+0x5eb NtDeviceIoControlFile(0x390400)` → `+0x5fb JS` / `+0x60e` 크기 → `+0x4a0 ~ +0x4b0` · `+0x7d4 ~ +0x7ea` 머리 검사 → `+0x4fb IoUnpack_ProviderRef`) → `+0x3ac ~ +0x3b7` `dwInterface ∈ 1..8` → `+0x518 LoadProviderEx` → `_LoadImage +0x59 _GetImagePath` · `+0x9d LoadLibraryExW`. `BCryptGenRandom +0x157 GetSystemPreferredRngHandle +0x78 OpenSystemPreferredAlgorithmProvider` (`+0xf7 BCryptResolveProviders(인터페이스 6)` → `+0x106 CMP [개수],1` → `+0x47 BCryptOpenAlgorithmProvider(첫 공급자 이름)`)

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### Q173a 결론 (run94)

- **`0xc00000e5` 의 정체** `[정적]` — 기본 대답 두 모양이 같은 줄로 끝난다.
  - `IOSB.Information` 이 `0x10` 보다 작음 → `+0x4a0 CMP RCX,R15` · `+0x4b0 JA +0x7f8`.
  - 0 으로 채운 출력 (개수 0 인데 배열 칸이 `-1` 아님) → `+0x7d4 TEST R13D` · `+0x7d7 JZ +0x7f8`.
  - `+0x7f8 MOV R12D,0x54f` → `+0x7fe JMP` (함수 `+0x39b`, 내가 할당한 출력 버퍼 free) → `+0x3ba WinErrorToNtStatus(0x54f)` → `0xc00000e5` (`WinErrorToNtStatus +0x57 ~ +0x70`: `0x491 + 0x4a + 0x74 = 0x54f`).
- **입력 `0x38` · `0x30` 은 기능 `0x20000` 이 맞다** `[정적]` — 에뮬레이터 쪽 `[추측]` 이 코드로 섰다. 계산식은 아래 "입력 칸 표".
- **출력 `0x180` 은 첫 시도 고정** (`+0x514 MOV RCX,[RAX]` · `+0x51a JZ +0x7a8` → `+0x7a8 MOV R15D,0x180` → `+0x547 BCryptAlloc`) `[정적]`. 관찰된 두 호출자 (`BCryptOpenAlgorithmProvider +0x242` · `OpenSystemPreferredAlgorithmProvider +0x2c`) 가 둘 다 "버퍼 없음" 으로 시작해서 둘 다 `0x180`.
- **재시도는 있다 — 단 모양이 정해져 있다** `[정적]`:
  - status ≥ 0 인데 `Information` > 버퍼 크기 → 그 크기로 다시 할당해 IOCTL 다시 (`+0x60e CMP · +0x613 JC +0x725`).
  - status `0x80000005` + 출력 8 바이트 `{0xc0000023, 필요한 크기}` + `Information` 정확히 8 → 다시 (`+0x6c4 ~ +0x6f5`).
  - status 를 그냥 `0xc0000023` 으로 주면 **재시도 없이** `0xc00000e5` (`+0x6b9 · +0x6be JZ +0x7bd`).
- **성공으로 보내는 최소** `[정적]`: IOCTL status 0 + `Information` = N (`0x10 ≤ N ≤ 0x180`) + 출력 N 바이트 (아래 "가장 작은 출력 한 장"). 그다음 bcrypt 가 출력의 이미지 이름으로 `LoadLibraryExW` 를 부른다 — 엔진 몫은 거기서 로더로 넘어간다.
- **"그런 알고리즘 없음" 은 IOCTL 음수 status 로** `[정적]`. `0xc0000023` · `0x80000005` 아닌 음수는 그대로 위로 간다 (`+0x6c9 JNZ +0x7b3 → … → +0x669`). 무슨 음수가 진짜인지는 `[추측]` (`STATUS_NOT_FOUND 0xc0000225` 가 자연스러움 — 결과서).

### 지금 PE_emulator 맞나 · 틀리나 (결과서 질문마다 "지금 우리" 줄 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

| 갈래 | 지금 엔진 (요청서 "배경") | 진짜 (26100.9457) `[정적]` | 판정 |
|---|---|---|---|
| IOCTL 코드 · 기능 · 입력 길이 | 받는 그대로 (만들 필요 없음) | `0x390400` · `0x20000` · `0x38`/`0x30` (계산식) | **맞음** — 만들 것은 **답** |
| 출력 길이 `0x180` | 받는 그대로 | 첫 시도 고정 | **맞음** |
| 답 (status · `Information` · 출력) | 기본 대답 → `0xc00000e5` | status 0 + `Information` N + 칸 표 대로 | **틀림** — `+0x4b0` / `+0x7d7` 에서 걸림 |
| `GenRandom` 길 (입력 `0x30`, 3줄) | `0xc00000e5` | 같은 함수 (B3), 답이 맞으면 이어서 `0x38` 질의 (B4) 가 한 번 더 | **틀림** (같은 까닭). 고치면 IOCTL 이 둘 이어서 보일 것 |
| "없음" 답 | — | 음수 status. 개수 0 성공 답은 크래시 · 캐시에 박힘 | 빈 곳 — **개수 0 성공 답 쓰지 말 것** |
| 캐시 | 상관없음 | 성공 답만 이름별로 캐시 | 한 번 틀린 성공 답을 주면 그 이름은 프로세스 끝까지 그 답 |

### 원문 질문이 어디서 답해졌나

| 요청서 질문 | 답한 곳 | 한 줄 |
|---|---|---|
| 1 누가 부르나 · 입력 길이 `0x38`/`0x30` · 출력 `0x180` · 사슬 · `GenRandom` 길 | 아래 "부르는 사슬 표" · "두 IOCTL 길" · "입력 칸 표" | 두 길 다 `BCryptResolveProviders` 의 **직접** `NtDeviceIoControlFile` (`+0x5eb`). `IoCallKernelDriver` 길 (`+0x276`) 은 `ppBuffer == NULL` 크기 질의 때만 |
| 2 출력 읽기 · status · 칸 표 · 포인터 고치기 | 아래 "status 처리 표" · "출력 칸 표" | `Information` 만 길이. `IOSB.Status` · 표지 안 봄. 오프셋 → 시작 주소 + 오프셋, 범위 검사 있음 |
| 3 `0xc00000e5` 자리 · 실패 status | 아래 "실패 status 표" | 기본 대답 = `+0x7f8` 한 줄 |
| 4 최소 조건 · 어떤 DLL · 0 개 답 | 아래 "최소 조건 표" · "가장 작은 출력" · "0 개 답" | `0x98` 바이트 한 장. DLL = 출력의 이미지 이름 (파일 이름만). 0 개 = 크래시 |
| 5 캐시 | 아래 "캐시" | `g_pResolveProviderCache`, 키 = 알고리즘 이름 |

### 최소 조건 표 (결과서 4-a — run92 "최소 조건 표" 와 같은 칸 순서, attach 가 TRUE 로 끝난 전제: `g_hIoDevice ≠ 0` · `g_TrustedEnvironment & 1` · `g_fLoadCNGDone = 1` · `g_fLoaderInitialized = 1`)

| # | syscall (또는 읽는 칸) | 입력 | 기대 답 | 안 맞으면 |
|---|---|---|---|---|
| 1 | **`NtDeviceIoControlFile`** (공급자 질의) | 핸들 `[g_hIoDevice]`, Event · APC · 문맥 0, 코드 **`0x390400`**, 입력 `0x30` / `0x38` / … (아래 입력 칸 표), 출력 = bcrypt 힙 `0x180` | **status ≥ 0** (0 권장) | 음수 → 그대로 반환. `0xc0000023` → `0xc00000e5` |
| 2 | 그 IOCTL 의 IOSB (`[RSP+0x78]`, 16 바이트 0 으로 시작) | — | **`Information` = N, `0x10 ≤ N ≤ 0x180`**. `Status` 칸은 안 읽음 | `N < 0x10` → `+0x4b0` → **`0xc00000e5`** (지금 엔진). `N > 0x180` → bcrypt 가 N 으로 다시 할당해 IOCTL 다시 |
| 3 | 그 IOCTL 의 출력 버퍼 | — | 처음 N 바이트 = 아래 한 장 (공급자 1개) | 칸 어긋남 → `0x54f` → `0xc00000e5`. `dwInterface ∉ 1..8` → `0xc0000008` |
| 4 | `LoadLibraryExW` (로더 몫) | `시스템 폴더 + "\" + 출력의 이미지 이름` (`_GetImagePath +0x2e` · `+0x5e`, `_LoadImage +0x9d`) | 성공 + 그 DLL attach 통과 (bcryptprimitives — run91) | `_LoadImage +0x18c` `0xc0000135` · `+0x176` `0xc000007b` → 다음 공급자 → 다 실패면 그 status |
| 5 | `GetProcAddress` (그 DLL 의 `Get…Interface`) | `_LoadInterface +0x130` | 내보내기가 있어야 — 이름 ↔ 번호 `[공백]` | 이름만 |
| 6 | 공급자 DLL 함수 표 검사 · 열기 함수 (`BCryptOpenAlgorithmProvider +0x70b ValidateFunctionTable` · 간접 `+0x851` · `+0x883`) | — | 이름만 (공급자 몫) | 이름만 |

- `BcoapCached` 를 지나 성공 쪽으로 가는 최소 = 1 · 2 · 3 행. 4 행부터는 "그 뒤 어떤 DLL 을 올리나".

### 가장 작은 출력 한 장 — 공급자 하나 (`BCryptOpenAlgorithmProvider(L"AES", NULL, 0)` 기준, `Information = 0x98`, 결과서 4-b)

| 오프셋 | 크기 | 값 | 뜻 | 검사 줄 |
|---|---|---|---|---|
| `0x00` | 4 | `1` | 공급자 개수 | `+0x4ba MOV R13D,[R15]` · `+0x7d4 TEST R13D` |
| `0x04` | 4 | `0` | 안 읽음 | — |
| `0x08` | 8 | `0x10` | 포인터 배열 오프셋 | `+0x4bd CMP RAX,-1` |
| `0x10` | 8 | `0x18` | 배열 `[0]` = 공급자 칸 오프셋 | `IoUnpack_ProviderRef +0x30 ~ +0x49` |
| `0x18` | 4 | `1` (AES — 암호, 번호 뜻은 아래) | `dwInterface` | `BCryptOpenAlgorithmProvider +0x3ac ~ +0x3b7` (`1..8`) |
| `0x1c` | 4 | 0 | 안 씀 | — |
| `0x20` | 8 | `0x60` | `pszFunction` (알고리즘 이름) 오프셋 | `IoUnpack_ProviderRef +0x62 ~ +0x1c3` |
| `0x28` | 8 | `-1` | `pszProvider` 없음 | `+0x1c8 ~ +0x1d8` |
| `0x30` | 4 | `0` | `cProperties` | `+0x14c MOV EDI,[R14+0x18]` |
| `0x34` | 4 | 0 | 안 씀 | — |
| `0x38` | 8 | `-1` | `rgpProperties` 없음 (개수 0 이면 `-1` **필수**) | `+0x150 ~ +0x160` |
| `0x40` | 8 | `0x50` | `pUM` 이미지 칸 오프셋 (**NULL 불가**) | `IoUnpack_ImageRef +0x28 ~ +0x41` |
| `0x48` | 8 | `-1` | `pKM` 없음 | 같음 |
| `0x50` | 8 | `0x68` | 이미지 칸 `+0` = 이미지 이름 오프셋 | `IoUnpack_ImageRef +0xe1 ~ +0x12f` |
| `0x58` | 4 | `0` | 이미지 칸 `+8` `dwFlags` | 안 봄 |
| `0x60` | 8 | `L"AES\0"` | 알고리즘 이름 | `BCryptOpenAlgorithmProvider +0x4bb` 가 핸들 안에 복사 |
| `0x68` | 42 → 48 | `L"bcryptprimitives.dll\0"` + 0 | 이미지 이름 (**파일 이름만**) | `LoadProviderEx +0xb0` · `_LoadImage +0x2c ~ +0x45` |

- 칸 이름 (`pszFunction` · `pUM` 등) 은 공개 헤더 기억 `[추측]` — **PDB 에 `_CRYPT_PROVIDER_REFS` 같은 구조체 정의가 없다** (결과서 `pdb_struct.py --list` 0건). 오프셋 · 크기 · 검사는 `[정적]` (`IoUnpack_*` 의 오프셋).
- 결과서가 `IoUnpack_*` 검사 줄을 옮긴 파이썬 모델로 이 `0x98` 바이트를 돌려 통과, `Information 0` · `0x180` 영 채움은 `0x54f` 로 걸림을 확인했다 (`[정적]` 손 검산, 임시 파일).
- **`dwInterface` 는 알고리즘에 맞아야 한다** — 이 값이 핸들 `+0x24` 에 들어가고 (`BCryptOpenAlgorithmProvider +0x498`), `BCryptGenRandom +0x5a CMP [RAX+0x24],6` 이 `6` 을 요구한다. 코드로 본 번호: **6 = 난수** (`OpenSystemPreferredAlgorithmProvider +0xeb`), **2 = hash** (`+0xaa1` + 문자열 `HashBlockLength`), **3 = 비대칭 암호화** (`+0xb57` + `PaddingSchemes`). **1 = 암호** 는 `[추측]` (공개 헤더 기억).
- **`GenRandom` 질의 (입력 `0x30`, 인터페이스 6) 의 답**: 같은 모양, `dwInterface = 6`, **`pszFunction` 이 NULL 이면 안 됨** (그 문자열이 `BCryptOpenAlgorithmProvider` 의 이름으로 넘어감 — NULL 이면 `0xc000000d`). 이름은 `L"RNG"` 가 자연스럽다 `[추측]` (CNG 초기화 전 이 코드가 쓰는 이름이 `RNG` — `+0x36 LEA RDX,[0x18001f128]`, PE 바이트).

### 공급자 "0 개" 답 (결과서 4-c)

| 답 | 가는 자리 | 결과 |
|---|---|---|
| status 0, `Information ≥ 0x10`, 개수 0, 배열 `-1` | `BCryptResolveProviders` 는 status 0 으로 통과 (`+0x4c7 ~ +0x4da`), `BcoapCached` 가 **캐시에도 넣음** (`+0x163 ~ +0x177`), `BCryptOpenAlgorithmProvider +0x3a5 MOV RAX,[RAX+8]` · `+0x3a9 MOV RDX,[RAX]` | **널 역참조 (크래시)** `[정적: 읽기]`. 캐시에 박혀 같은 이름은 계속. **쓰지 말 것** |
| 〃 (`GenRandom` 질의) | `OpenSystemPreferredAlgorithmProvider +0x106 CMP [RBX],1` · `+0x109 JC +0x174` | `0xc00000e5` (크래시 아님) |
| 개수 0 + 배열 ≠ `-1` | `+0x7d7 JZ +0x7f8` | `0xc00000e5` |
| "없음" 을 알리고 싶다 | IOCTL 자체를 음수 status 로 | 그대로 위로 (`BCryptOpenAlgorithmProvider +0x263 JNS` 안 탐). **실패는 이쪽이 정상 길** |

### PE_emulator 확인거리 (우리 대기열 아님)

- 엔진 `NtDeviceIoControlFile` 몸이 `0x390400` 의 입력 `+4` (기능 번호) 를 보고 `0x20000` 이면 위 한 장 꼴의 답을 만드는지. 입력 `+0x18` (이름 오프셋) 에서 알고리즘 이름을 읽어 `pszFunction` 과 `dwInterface` 를 맞춰 줘야 한다 (`AES` → 1 `[추측]`, `RNG` → 6, `SHA256` → 2 등).
- 이미지 이름은 파일 이름만 (`bcryptprimitives.dll`). bcrypt 가 시스템 폴더를 앞에 붙인다. 이름이 `bcrypt.dll` 이면 `LoadLibrary` 없이 자기 자신 (`_LoadImage +0x7b RtlCompareUnicodeString` → `+0x140 g_hInstance`).
- 고치면 다음 벽 후보는 `GetProcAddress(…Interface)` (4 행 · 5 행) — 공급자 DLL 이 내보내야 할 이름 ↔ 번호 는 이번에 안 봄 (다음 요청 후보 1).

## 환경

- 26100.9457, **정적만** (VM 실행 없음). test.exe 는 bcrypt 를 안 올려 (run92 `[관찰]` run44 모듈 6개) 사진 대조 없음.
- 정적: **bcrypt pdb `5F1077ED8D3ABC83AD7D378F94EA41901`** (run92 와 같음). **다시 analyze 안 함** (`.gpr` · `_index.tsv` 312줄 · `SetPdb:` 줄 있음). `export` 4번 · `refs` 3번, `SCRIPT ERROR` 0줄.
- 이름: `IoUnpack_ProviderRef` · `WinErrorToNtStatus` · `IoCallKernelDriver` 는 PDB 에 C++ 꾸민 이름 하나뿐 (`?IoUnpack_ProviderRef@@YAKPEAPEAU_CRYPT_PROVIDER_REF@@PEAEK@Z` — `pdb_aliases.py --where bcrypt.dll IoUnpack_ProviderRef` rva `0x6bd0`). `BCryptAlloc` 은 "크기만큼 힙 할당" 공용 부품 — 같은 주소 이름 3개, 후보: `BCryptAlloc` · `MSCryptAlloc` · `SafeAllocaAllocateFromHeap` (`--lookup bcrypt.dll 0x32d0`). 전역 `g_pResolveProviderCache` = rva `0x25c10` (`--where`, 함수 밖).
- 버전 대조 (결과서): bcrypt 는 "알려진 사실" 표에 없어 **에뮬레이터 로그 값 ↔ 코드 값** 으로 맞춤 — 코드 `0x390400` (`+0x5de`) · 기능 `0x20000` (`+0x590`) · 입력 `0x38` / `0x30` · 출력 `0x180` · 결과 `0xc00000e5`. 어긋나는 줄 없음.

## 목적

- run92 "아직 확인 필요" 정적 ② (대기열 Q173 을 나눈 **Q173a**, 사용자 결정 2026-11-15 "지금 먼저", Q104 VM 단계 앞 멈춤 자리에서 끼움). PE_emulator 외부 피드백: run92 로 bcrypt `DllMain` 은 통과했고, 다음 벽이 같은 핸들의 IOCTL `0x390400` (입력 `0x38`/`0x30`, 출력 `0x180`) 기본 대답 → `0xc00000e5` (악성 샘플 36 개).
- 누가 이 IOCTL 을 부르나 (입력 길이 맞추기) · 출력을 어떻게 읽나 (`IoUnpack_ProviderRef`) · `0xc00000e5` 자리 · 성공 최소 조건과 올릴 DLL · 캐시.
- 범위 밖: 커널 `ksecdd.sys` 의 `0x390400` 처리 (Q173b ③), `LoadProviderEx` 본문 · 공급자 DLL 안, Q173 ① ④ (Q173b).

## 방법

- 정적: `_workspace/sast/90_bcrypt-resolve.md` — bcrypt, `^BCryptResolveProviders$` · `^IoUnpack_ProviderRef$` · `^BCryptResolveProvidersForBcoapCached$` depth 1, `^BCryptOpenAlgorithmProvider$` · `^IoCallKernelDriver$` · `^IoPack_` · `GenRandom` 길 셋 depth 0. 결과서가 질문 4 때문에 `LoadProviderEx` · `_LoadImage` · `_GetImagePath` (depth 0, 이름 · 필드만) 로 넓힘. `refs`: `0x25b2c` (`g_fLoaderInitialized`) · `0x25c10` (`g_pResolveProviderCache`) · `0x25898` (`g_fLoadCNGDone`).
- 결과서: `_workspace/sast/90_bcrypt-resolve_result.md` (**분석성공** — 못 닫은 것: 진짜 KsecDD 의 답 = 동적, 공급자 DLL 이 내보내야 할 이름 = 다음 요청 1, `BCryptEnumProviders` 쪽 NULL 길 = 다음 요청 2).
- flow-writer 가 다시 본 것 (`ms/disassemble/bcrypt/*.asm`, `_calls.tsv`):
  - `BCryptResolveProviders`: `+0x74` · `+0xb0` · `+0xef` · `+0xf6` · `+0x137 ~ +0x13d` · `+0x253 ~ +0x276` · `+0x393 ~ +0x3d0` · `+0x4a0 ~ +0x4ce` · `+0x4f2 ~ +0x51a` · `+0x514 ~ +0x5f7` 전부 · `+0x5f7 ~ +0x742` 전부 · `+0x742 ~ +0x7bd` 전부 · `+0x7d4 ~ +0x80c`, `_calls.tsv` 의 이 함수 줄 전부.
  - `IoUnpack_ProviderRef +0x39` · `+0x4f` · `+0x14c ~ +0x150` · `+0x1c0` · `+0x242`, `IoUnpack_ImageRef +0x31` · `+0x179`, `WinErrorToNtStatus +0x4d ~ +0x75`, `BCryptResolveProvidersForBcoapCached +0x23 ~ +0x2c` · `+0xcf` · `+0x11b` · `+0x12c` · `+0x150 ~ +0x157` · `+0x177`, `BCryptOpenAlgorithmProvider +0x1e4` · `+0x1f4` · `+0x201` · `+0x242` · `+0x259` · `+0x263` · `+0x3a5 ~ +0x3b7` · `+0x3cf ~ +0x3d4` · `+0x498` · `+0x514 ~ +0x518` · `+0xaaf` · `+0xb65`, `OpenSystemPreferredAlgorithmProvider +0x26 ~ +0x47` · `+0x75` · `+0xeb ~ +0x11f` · `+0x174`, `GetSystemPreferredRngHandle +0x39` · `+0x78` · `+0x8a`, `BCryptGenRandom +0x5a` · `+0x157`, `LoadProviderEx +0x35 ~ +0x37` · `+0xb0 ~ +0xb7`, `_LoadImage +0x59` · `+0x7b` · `+0x9d` · `+0x140` · `+0x176` · `+0x18c`, `_GetImagePath +0x2e` · `+0x5e` · `+0x70`.
  - 이름: `pdb_aliases.py --where bcrypt.dll` (`IoUnpack_ProviderRef` · `g_pResolveProviderCache`), `--lookup bcrypt.dll 0x32d0`.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/bcrypt/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `bcrypt!BCryptOpenAlgorithmProvider` | `+0x1f4` | `+0x1f9` | `VsmOpenAlgorithmProvider` | `+0x1e4 JNZ` 안 탈 때 (`g_TrustedEnvironment & 1` 아님) — 이름만 | [정적] |
| 〃 | **`+0x259`** | **`+0x25e`** | **`BCryptResolveProvidersForBcoapCached`** | 공급자 = `g_fLoadCNGDone` 0 이면 `L"Microsoft Primitive Provider"` (rva `0x1f8f0`), 아니면 호출자 인자 (NULL 이면 플래그 2) (`+0x201 ~ +0x229`). `+0x263 JNS` | [정적] |
| 〃 | `+0x3cf` | `+0x3d4` | `_LogBCryptOpenProviderFailure` | 첫 공급자 `dwInterface ∉ 1..8` → `+0x3d4` `0xc0000008` | [정적] |
| 〃 | **`+0x518`** | **`+0x51d`** | **`LoadProviderEx`** | `+0x514 MOV RCX,[RSI+RAX]` = 배열 `[i]` 공급자 칸. 실패면 다음 공급자 (루프 `+0x4dd ~ +0xa84`) | [정적] |
| `BCryptResolveProvidersForBcoapCached` | `+0x11b` | `+0x120` | `BCryptResolveProviders` | 공급자 NULL **그리고** 플래그 2 (`+0x23 TEST RDX · +0x26 JNZ` · `+0x28 CMP R8D,2 · +0x2c JNZ`) — 캐시 미스 | [정적] |
| 〃 | `+0xcf` | `+0xd4` | `BCryptResolveProviders` | 그 밖 (캐시 안 씀) | [정적] |
| 〃 | `+0x133` · `+0x150` | `+0x138` · `+0x155` | `BCryptAlloc` (공용 할당) · `StringCchCopyW` | 캐시 노드 `0x40`, 이름 최대 19 글자 (`+0x157 JNZ` 넘으면 캐시 안 함) | [정적] |
| `BCryptResolveProviders` | `+0x79` | `+0x7e` | `ValidateInterfaceId` | 실패 → `0x57` → `0xc000000d` | [정적] |
| 〃 | `+0xdb` · `+0x113` · `+0x17b` · `+0x1d8` · `+0x2e9` · `+0x332` | `+0xe0` · … · `+0x337` | `SzString_CchLength` (공용 문자열 길이) | 입력 문자열 셋 (컨텍스트 · 이름 · 공급자) 길이 | [정적] |
| 〃 | `+0x276` | `+0x27b` | `IoCallKernelDriver` | **`ppBuffer == NULL` (크기 질의) 일 때만** (`+0x253 TEST RAX · +0x256 JNZ +0x514` 안 탐). `+0x271 MOV ECX,0x20000` | [정적] |
| 〃 | `+0x374` · `+0x547` | `+0x379` · `+0x54c` | `BCryptAlloc` | 입력 (`> 0x140` 일 때, `+0x137 · +0x13d JA`) · **출력 (`0x180` 또는 다시 받은 크기)** | [정적] |
| 〃 | `+0x38e` · **`+0x3ba`** | `+0x393` · `+0x3bf` | `WinErrorToNtStatus` | Win32 오류 → status. **`0x54f` → `0xc00000e5`** | [정적] |
| 〃 | `+0x3a9` | `+0x3ae` | `BCryptFree` | 실패 길, 내가 할당한 출력 버퍼 | [정적] |
| 〃 | **`+0x4fb`** | **`+0x500`** | **`IoUnpack_ProviderRef`** | 배열 칸마다 (`+0x4f2 ~ +0x4f7`). `+0x505 JNZ` (함수 `+0x39b`) → 실패 | [정적] |
| 〃 | **`+0x5eb`** | **`+0x5f2`** | **`NtDeviceIoControlFile`** | `([g_hIoDevice], 0, 0, 0, &IOSB[RSP+0x78], 0x390400, 입력, 입력 길이, 출력, 출력 길이)` (`+0x5b7 ~ +0x5e6`) | [정적] |
| 〃 | `+0x749` · `+0x761` · `+0x771` | `+0x750` · `+0x766` · `+0x778` | `EnterCriticalSection` · `_IoOpenDevice` · `LeaveCriticalSection` | **`g_hIoDevice == 0` 일 때만** (`+0x5a7 ~ +0x5b1 JZ +0x742`) — 보탬 ③ | [정적] |
| `IoUnpack_ProviderRef` | (안쪽) | — | `IoUnpack_ImageRef` · `IoUnpack_PropertyRef` · `IoUnpack_SG_SzString` · `IoUnpack_SG_Buffer` · `SzString_CbLength` · `SzString_CchLength` | 칸 오프셋 → 포인터, 범위 · NUL 검사. 실패 `0x54f` (`+0x242`) / `0x7a` | [정적] |
| `LoadProviderEx` | (안쪽) | — | `_LoadImage` | `+0x35 JNZ` 안 타면 (`g_fLoaderInitialized` 0) `+0x37` `0xc00000e5`. 이미지 이름 `+0xb0 MOV RCX,[R15+0x28]` · `+0xb7 MOV RCX,[RCX]` | [정적] |
| `_LoadImage` | `+0x59` | `+0x5e` | `_GetImagePath` | → `+0x2e` · `+0x5e GetSystemDirectoryW` · `+0x70` `\` 붙임 | [정적] |
| 〃 | `+0x7b` | `+0x82` | `RtlCompareUnicodeString` | 이름이 `bcrypt.dll` 이면 `+0x140 g_hInstance` (LoadLibrary 안 함) | [정적] |
| 〃 | **`+0x9d`** | **`+0xa4`** | **`LoadLibraryExW`** | `(경로, 0, 0)`. 실패 → `+0x18c` `0xc0000135` / `+0x176` `0xc000007b` | [정적] |
| `BCryptGenRandom` | `+0x157` | `+0x15c` | `GetSystemPreferredRngHandle` | 핸들 NULL + 플래그 2 (결과서 B1) | [정적] |
| `GetSystemPreferredRngHandle` | `+0x78` | `+0x7d` | `OpenSystemPreferredAlgorithmProvider` | 캐시 칸 (rva `0x25ad0`) 0 일 때만 (`+0x39 JZ`). 결과 `+0x8a CMPXCHG.LOCK` 저장 | [정적] |
| `OpenSystemPreferredAlgorithmProvider` | **`+0xf7`** | **`+0xfc`** | **`BCryptResolveProviders`** | `(NULL, 6, NULL, NULL, 1, 0, &cb, &refs)` — `+0x26 CMP [g_fLoadCNGDone]` · `+0x30 JNZ` (CNG 끝났을 때) | [정적] |
| 〃 | `+0x47` | `+0x4c` | `BCryptOpenAlgorithmProvider` | 이름 = 질의 답 첫 공급자 `pszFunction` (`+0x118 ~ +0x11f`), CNG 전이면 `L"RNG"` (`+0x36`) | [정적] |
| 〃 | `+0x75` | `+0x7a` | `BCryptFreeBuffer` | 질의 답은 캐시 안 함 | [정적] |

### 부르는 사슬 — 두 길이 같은 `BCryptResolveProviders` 로 모인다 (결과서 1-a)

| # | 길 | 넘기는 것 (`BCryptResolveProviders` 인자) |
|---|---|---|
| A1 → A2 | `BCryptOpenAlgorithmProvider +0x259` → `BcoapCached +0x11b` (캐시 미스) | `(NULL, 0, 이름, NULL, 1, 2, &cb, &refs)` |
| A3 | `BcoapCached +0xcf` | `(NULL, 0, 이름, 공급자, 1, 플래그, &cb, &refs)` |
| B1 → B2 → B3 | `BCryptGenRandom +0x157` → `GetSystemPreferredRngHandle +0x78` → `OpenSystemPreferredAlgorithmProvider +0xf7` | `(NULL, 6, NULL, NULL, 1, 0, &cb, &refs)` |
| B4 | `OpenSystemPreferredAlgorithmProvider +0x47` → A1 | 이름 = B3 답의 첫 공급자 이름 |

- **`BCryptGenRandom(NULL, …, 플래그 2)` 첫 호출은 IOCTL 이 연달아 둘** `[정적]`: ① B3 (입력 `0x30`) → ② B4 → A1 → A2 (입력 `0x30 + 이름`, 3 글자 이하면 `0x38`). 에뮬레이터의 "`0x30` 3줄" 이 ①, "`0x38` 12줄" 이 ② 와 `BCryptOpenAlgorithmProvider` 직접 호출 — `[정적]` 맞춤 / 실제 횟수는 동적.
- `IoCallKernelDriverQueryLoop` 은 이 두 길에 안 닿는다 — 부르는 쪽이 설정 질의 함수들 (`BCryptEnumContexts` · `BCryptQueryContextConfiguration` · `BCryptEnumRegisteredProviders` · `BCryptQueryProviderRegistration`), 이름만 `[정적]`.

### 두 IOCTL 길 — `ppBuffer` (8번째 인자, `+0x74 MOV [RSP+0x60],RAX`) 하나로 갈린다

| 길 | 갈림 | 부르는 자리 | 출력 | 기능 번호 |
|---|---|---|---|---|
| **직접** `NtDeviceIoControlFile` | `+0x253 TEST RAX · +0x256 JNZ +0x514` (`ppBuffer ≠ NULL`) | `+0x5eb` / `+0x5f2` | `*ppBuffer == NULL` → **`0x180`** (`+0x7a8`), 아니면 `*pcbBuffer`. 모자라면 다시 | `+0x590 MOV [RSI+4],0x20000` |
| `IoCallKernelDriver` | 위에서 안 갈림 (`ppBuffer == NULL`, 크기만 묻기) | `+0x276` / `+0x27b` | 8 바이트 지역 + 출력 포인터 NULL → 필요한 크기만. `+0x803 CMP EDX,0xc0000023 · +0x809 CMOVZ` 로 status 0 + 크기 | `+0x271 MOV ECX,0x20000` → `IoCallKernelDriver +0x5b` |

- A · B 두 길 다 `ppBuffer ≠ NULL` 이라 **직접 길만** 탄다. `IoCallKernelDriver` 길은 `BCryptEnumProviders` · `BCryptEnumAlgorithms` 같은 쪽이 NULL 을 줄 때뿐 — 그 둘이 실제로 NULL 을 넘기는지는 안 읽음 `[공백]`.
- 이 길의 입력은 `IoPack_*` 를 안 쓰고 `BCryptResolveProviders` 가 직접 채운다 (`SzString_CchLength` + `memcpy`). run92 attach 의 `0x10500` 입력 (`IoPack_Configuration_ParamBlock`, `0x68`) 과 **칸 배치가 다르다**.

### 입력 칸 표 (기능 `0x20000`, 결과서 1-c)

| 오프셋 | 크기 | 뜻 | 출처 |
|---|---|---|---|
| `+0x00` | 4 | `0x1a2b3c4d` 표지 | `+0x58a` |
| `+0x04` | 4 | **`0x20000`** 기능 번호 | `+0x590` |
| `+0x08` | 8 | 컨텍스트 문자열 오프셋 / `-1` | `+0x223` · `+0x2b5` |
| `+0x10` | 4 | 인터페이스 (A 길 `0`, B3 `6`) | `+0x1ff` |
| `+0x14` | 4 | 안 씀 (스택 쓰레기) | — |
| `+0x18` | 8 | 함수 (= 알고리즘 이름) 오프셋 / `-1` | `+0x233` · `+0x2c1` |
| `+0x20` | 8 | 공급자 이름 오프셋 / `-1` | `+0x243` · `+0x2cd` |
| `+0x28` | 4 | 모드 (모두 `1`) — 검사 `∈ {1,2,3}` 아니면 `0x57` | `+0x209` |
| `+0x2c` | 4 | 플래그 (A2 `2`, A3 · B3 `0`) — 검사 `& ~3 == 0` 아니면 `0x3ec` | `+0x213` |
| `+0x30` ~ | 가변 | 문자열 (UTF-16, NUL 포함, 각 8 바이트로 올림). 순서: 컨텍스트 → 이름 → 공급자 | `+0x313` · `+0x166` · `+0x1c3` |

- **길이 = `0x30 + Σ ((글자수 × 2) + 9) & ~7`** (`+0xb0 MOV R15D,0x30` · `+0xef LEA EAX,[9+RAX*2]` · `+0xf6 AND EAX,0xfffffff8` · `+0xf9 ADD ESI,EAX`). 예: `AES` · `RNG` · `MD5` → `0x38`, `SHA1` · `SHA256` → `0x40`, `ECDSA_P256` → `0x48`, 이름 없음 → `0x30`. 공급자 이름을 같이 주는 A3 (`L"Microsoft Primitive Provider"`, 28 글자 → `+0x40`) 는 `AES` 면 `0x78` — 에뮬레이터 로그엔 아직 안 보인 모양.
- `0x140` 넘으면 힙 (`+0x137 · +0x13d JA +0x372`), 아니면 스택 (`+0x58 LEA R14,[RBP-0x60]`).
- 모드 · 플래그 값 이름 (`CRYPT_UM` 1 · `CRYPT_ALL_PROVIDERS` 2) 은 `[추측]` (공개 헤더와 같은 값).

| 실제 모양 | 길이 | `+0x10` | `+0x08` | `+0x18` | `+0x20` | `+0x28` | `+0x2c` | `+0x30` |
|---|---|---|---|---|---|---|---|---|
| `BCryptOpenAlgorithmProvider(L"AES", NULL, 0)` (A1 → A2) | `0x38` | 0 | -1 | `0x30` | -1 | 1 | **2** | `L"AES\0"` |
| `OpenSystemPreferredAlgorithmProvider` 질의 (B3) | `0x30` | **6** | -1 | -1 | -1 | 1 | **0** | (없음) |

### status 처리 표 (`BCryptResolveProviders +0x5f7 ~ +0x7bd`, 결과서 2-a)

| IOCTL status | 자리 | 하는 일 |
|---|---|---|
| **≥ 0** (`0x103` 도) | `+0x5fb JS` 안 탐 → `+0x601 MOV R15,[RBP-0x80]` (= IOSB `Information`) | 성공 길. 반환 status 는 0 (`+0x62b MOV ESI,EBX`) |
| 〃 그런데 `Information` > 버퍼 크기 | `+0x60e CMP [RSP+0x50],R15D · +0x613 JC +0x725` | 내가 할당한 버퍼면 그 크기로 다시 할당해 IOCTL 다시. 호출자 버퍼면 `0xc0000023` + 필요 크기 (`+0x72a`) |
| `0xc0000023` | `+0x6b9 · +0x6be JZ +0x7bd` | **재시도 안 함 → `0xc00000e5`** (`+0x664`) |
| `0x80000005` | `+0x6c4` · `+0x6d3 CMP EAX,4 · +0x6d6 JC` | `Information < 4` → `0xc00000e5`. 출력 첫 DWORD (`+0x6dc`) 로 갈림 ↓ |
| 〃 첫 DWORD `0xc0000023` | `+0x6e0 ~ +0x6ef` | `Information` **정확히 8** 이어야. `+0x6f5 MOV R15D,[R13+4]` = 필요 크기 → 다시 할당, IOCTL 다시 (횟수 제한 없음) |
| 〃 첫 DWORD 그 밖 | `+0x78e CMP EAX,4 · +0x791 JNZ` | `Information` **정확히 4** 이어야. 그 DWORD 를 진짜 status 로 반환 |
| 그 밖의 음수 | `+0x6c9 JNZ +0x7b3` → `+0x708` … `+0x720 JMP +0x669` | **그대로 반환**. 출력 버퍼 free, `*ppBuffer` NULL |

- 출력 안에 전체 크기 칸 · 표지 (`0x1a2b3c4d`) 검사는 없다. 길이는 오직 `IOSB.Information`. `IOSB.Status` 는 안 읽는다 `[정적]`.

### 출력 칸 표 (성공 모드, 결과서 2-b)

머리 (`+0x481 ~ +0x4fb`, `+0x7d4 ~ +0x7f8`):

| 오프셋 | 크기 | 뜻 | 검사 → 실패 |
|---|---|---|---|
| `+0x00` | 4 | 공급자 개수 | `Information < 0x10` → 실패 (`+0x4a0 ~ +0x4b0`) |
| `+0x04` | 4 | 안 읽음 (성공 모드) | — |
| `+0x08` | 8 | 포인터 배열 오프셋 / `-1` | `-1` 이면 개수 == 0 (`+0x4c7 ~ +0x4ce`). 아니면 개수 ≠ 0 (`+0x7d4`), `오프셋 < Information` (`+0x7d9`), `오프셋 + 개수 × 8 ≤ Information` (`+0x7de ~ +0x7ea`) |

- 포인터 배열: 8 바이트 × 개수, 각 칸 = 공급자 칸 오프셋 (또는 `-1` = NULL — 개수 안에 끼면 뒤에서 크래시).
- 공급자 칸 **`0x38`** (`IoUnpack_ProviderRef +0x39 LEA RCX,[RAX+0x38]`): `+0x00` `dwInterface` (4, 여기선 안 봄 — `BCryptOpenAlgorithmProvider +0x3b7` 이 `1..8`) · `+0x08` 이름 오프셋 / `-1` (NUL 로 끝나는 UTF-16 이 `Information` 안에) · `+0x10` 공급자 이름 오프셋 / `-1` · `+0x18` 속성 개수 (4) · `+0x20` 속성 배열 오프셋 / `-1` (`-1` 이면 개수 0, `+0x150 ~ +0x160`) · `+0x28` **`pUM` 유저 이미지 칸** 오프셋 (NULL 이면 `LoadProviderEx +0xb7` 널 역참조) · `+0x30` `pKM` 커널 이미지 칸 (유저 길에선 안 읽음).
- 이미지 칸 **`0x10`** (`IoUnpack_ImageRef +0x31`): `+0x00` 이미지 이름 오프셋 / `-1`, `+0x08` `dwFlags` (안 봄). 속성 칸 **`0x18`** (`IoUnpack_PropertyRef +0x26`): `+0x00` 속성 이름, `+0x08` 값 크기 (4), `+0x10` 값 오프셋 / `-1`.
- **포인터 고치기**: 모든 칸이 출력 시작 기준 오프셋 → bcrypt 가 `시작 + 오프셋` 으로 바꿔 쓴다 (배열 `+0x7ec ADD RAX,R15`, 공급자 `IoUnpack_ProviderRef +0x4f LEA RDI,[RAX+RDX]`, 문자열 `+0x1c0 ADD RCX,RSI`). `-1` → NULL. **범위 검사 있음** (오프셋 `< Information`, 끝 `≤ Information`, 더하기 넘침 검사), 정렬 검사 없음.
- 검사에 걸리면 Win32 `0x54f` (문자열이 안 끝나면 `0x7a`) → `0xc00000e5` (`0x7a` 는 `0xc0000023`).

### 실패 status 표 (결과서 3 — 줄인 것)

| status | 자리 | 조건 |
|---|---|---|
| **`0xc00000e5`** | `BCryptResolveProviders +0x7f8` → `+0x3ba` | `Information < 0x10` · 개수 / 배열 `-1` 짝 어긋남 · 배열 범위 밖 · `IoUnpack_*` 가 `0x54f` (**기본 대답은 여기**) |
| `0xc00000e5` | `+0x664` | IOCTL `0xc0000023`, 또는 `0x80000005` 인데 `Information` 이 4 / 8 아님. 입력 `< 8` 바이트 (`+0x652`) |
| `0xc000000d` | `+0x80` · `+0x98` · `+0x451` · `+0x79d` (Win32 `0x57` / `0x3ec`) | 인터페이스 번호 · `pcbBuffer` NULL · 모드 · 플래그 |
| `0xc0000017` | `+0x385 ~ +0x393` · `+0x462` | 입력 / 출력 할당 실패 |
| IOCTL 음수 그대로 | `+0x6c9 JNZ +0x7b3 → … → +0x669` | 그 밖의 음수 (`_IoOpenDevice` 음수도) |
| (새로 안 만듦) | `BCryptResolveProvidersForBcoapCached` | 캐시 실패는 status 0 으로 두고 캐시만 안 함. 반환 = 받은 값 그대로 (`+0xd8` · `+0x124`) |
| `0xc000000d` | `BCryptOpenAlgorithmProvider +0xd1e` | `phAlgorithm` / `pszAlgId` NULL, `dwFlags & 1` |
| **`0xc0000008`** | `BCryptOpenAlgorithmProvider +0x3d4` | 첫 공급자 `dwInterface ∉ 1..8` (`+0x3ac ~ +0x3b7`) |
| `0xc00000e5` | `BCryptOpenAlgorithmProvider +0xaaf` / `+0xb65` | 인터페이스 2 / 3 인데 공급자 속성 조회 함수 NULL (공급자 DLL 쪽) |
| `0xc00000e5` | `LoadProviderEx +0x37` | `g_fLoaderInitialized` (rva `0x25b2c`) 0 — attach 의 `InitializeLoader +0x22` 가 1 로 씀 (`refs`) |
| `0xc00000e5` | `OpenSystemPreferredAlgorithmProvider +0x174` | `GenRandom` 질의 답 개수 0 (`+0x106 · +0x109 JC`) |

### 캐시 (결과서 5)

- 전역 **`g_pResolveProviderCache`** (rva `0x25c10`) — `BcoapCached` 만 읽고 쓴다 (`+0x2e` · `+0x16a` · `+0x177 CMPXCHG.LOCK`). 연결 리스트, 머리에 달기.
- 키 = 알고리즘 이름 (대소문자 구분 비교, `+0x50 ~ +0x62`). 노드 `0x40`: `+0x00` 다음 · `+0x08` 답 포인터 · `+0x10` 크기 · `+0x14` 이름 (19 글자 + NUL).
- 조건: 공급자 NULL **그리고** 플래그 2 — `BCryptOpenAlgorithmProvider(&h, 이름, NULL, 0)` 이고 `g_fLoadCNGDone ≠ 0` 일 때만. **성공 (≥ 0) 답만** (`+0x12c JS`). 지우는 코드 없음.
- IOCTL 횟수 가늠: 서로 다른 알고리즘 이름 수 (≤ 19 글자, 공급자 NULL) + `GenRandom` 이면 질의 한 번 (그 핸들도 전역 `0x25ad0` 에 캐시). 공급자 이름을 직접 주면 매번.

### 호출 관계

```
bcrypt!BCryptOpenAlgorithmProvider (rva 0x9410)  (phAlgorithm, pszAlgId, pszImplementation, dwFlags)        [정적]
 │  +0xc7 ~ +0xdf  인자 검사 → 0xc000000d
 │  +0x1e4 JNZ  (g_TrustedEnvironment & 1)  — 아니면 +0x1f4 VsmOpenAlgorithmProvider (이름만)
 │  +0x201  g_fLoadCNGDone 0 → 공급자 L"Microsoft Primitive Provider" / ≠0 → 인자 (NULL 이면 플래그 2)
 ├─ (1) +0x259  BCryptResolveProvidersForBcoapCached (rva 0x6120)
 │        │  +0x23 ~ +0x2c  공급자 NULL 그리고 플래그 2 → 캐시 g_pResolveProviderCache (0x25c10) 찾기
 │        ├─ (1) +0x11b  BCryptResolveProviders(NULL, 0, 이름, NULL, 1, 2, &cb, &refs)   캐시 미스
 │        │        +0x12c JS → 캐시 안 함 / 성공이면 +0x133 BCryptAlloc(0x40) · +0x150 StringCchCopyW · +0x177 CMPXCHG
 │        └─ (2) +0xcf   BCryptResolveProviders(NULL, 0, 이름, 공급자, 1, 플래그, …)       캐시 안 씀
 │                 │
 │                 ▼  BCryptResolveProviders (rva 0x63b0)
 │                 │  +0x79 ValidateInterfaceId · +0x86 ~ +0xaa 모드 · 플래그 검사
 │                 │  +0xb0 ~ +0x13d  입력 길이 = 0x30 + Σ((글자수×2+9)&~7)  (> 0x140 이면 +0x374 BCryptAlloc)
 │                 │  +0x15c ~ +0x36d  입력 칸 · 문자열 채움 (SzString_CchLength · memcpy)
 │                 │  +0x253 TEST · +0x256 JNZ +0x514   ← ppBuffer ≠ NULL (A · B 길 모두)
 │                 ├─ [크기 질의] +0x276  IoCallKernelDriver(0x20000, …)   ppBuffer == NULL 일 때만 → +0x803
 │                 │  +0x514 ~ +0x51a  *ppBuffer NULL → +0x7a8 R15D = 0x180
 │                 ├─ (1) +0x547  BCryptAlloc(0x180)                         ← 다시 받은 크기로 돌아옴 (+0x725 / +0x6f5)
 │                 │  +0x58a · +0x590  입력 +0 = 0x1a2b3c4d · +4 = 0x20000
 │                 │  +0x5a7 ~ +0x5b1  g_hIoDevice == 0 → +0x742 Enter · +0x761 _IoOpenDevice · +0x771 Leave (보탬 ③)
 │                 ├─ (2) +0x5eb  NtDeviceIoControlFile([g_hIoDevice], 0,0,0, &IOSB, 0x390400, 입력, 길이, 출력, 0x180)
 │                 │        +0x5fb JS → +0x6b9 (0xc0000023 → 0xc00000e5 / 0x80000005 → 크기 받아 다시 / 그 밖 그대로)
 │                 │        +0x601 Information 읽기 · +0x60e 버퍼보다 크면 → 다시 할당 · IOCTL 다시
 │                 │  +0x4a0 ~ +0x4b0  Information < 0x10 → +0x7f8          ← 기본 대답 (Information 0)
 │                 │  +0x4ba 개수 · +0x4bd 배열 -1? · +0x7d4 ~ +0x7ea 범위   ← 기본 대답 (0 채움) → +0x7f8
 │                 ├─ (3) +0x4fb  IoUnpack_ProviderRef (rva 0x6bd0)  × 개수
 │                 │        → IoUnpack_ImageRef · IoUnpack_PropertyRef · IoUnpack_SG_*        실패 0x54f / 0x7a
 │                 │  +0x505 JNZ (함수 +0x39b) → 실패
 │                 │  +0x7f8 MOV R12D,0x54f → +0x39b (출력 free) → +0x3ba WinErrorToNtStatus → 0xc00000e5
 │                 └─ 성공 → status 0, *ppBuffer = 출력 (오프셋 → 포인터로 고친 것)
 │  +0x263 JNS  — 음수면 로그 후 그대로 반환
 │  +0x3a5 · +0x3a9  첫 공급자 칸 (검사 없음 — 개수 0 이면 널 역참조)
 │  +0x3ac ~ +0x3b7  dwInterface ∈ 1..8  → 아니면 +0x3cf _LogBCryptOpenProviderFailure · +0x3d4 0xc0000008
 │  +0x498  핸들 +0x24 ← dwInterface
 ├─ (2) +0x518  LoadProviderEx(공급자 칸, …)   공급자마다 (루프 +0x4dd ~ +0xa84)
 │        │  +0x35 JNZ (g_fLoaderInitialized) → 아니면 +0x37 0xc00000e5
 │        │  +0xb0 · +0xb7  이미지 이름 = [공급자 +0x28] → [+0]
 │        └─ _LoadImage
 │                 ├─ +0x59  _GetImagePath → +0x2e · +0x5e GetSystemDirectoryW · +0x70 "\"
 │                 ├─ +0x7b  RtlCompareUnicodeString — "bcrypt.dll" 이면 +0x140 g_hInstance (안 올림)
 │                 └─ +0x9d  LoadLibraryExW(경로, 0, 0)     실패 → +0x18c 0xc0000135 / +0x176 0xc000007b
 └─ (그다음 이름만) _LoadInterface (+0x130 GetProcAddress) · +0x70b ValidateFunctionTable · 간접 +0x851 · +0x883

bcrypt!BCryptGenRandom (hAlgorithm NULL, 플래그 2)                                                        [정적]
 └─ +0x157  GetSystemPreferredRngHandle
             │  +0x39 JZ — 캐시 칸 0x25ad0 이 0 일 때만
             └─ +0x78  OpenSystemPreferredAlgorithmProvider
                        │  +0x26 CMP [g_fLoadCNGDone] · +0x30 JNZ   0 이면 +0x36 이름 L"RNG" 로 바로 +0x47
                        ├─ (1) +0xf7   BCryptResolveProviders(NULL, 6, NULL, NULL, 1, 0, …)    입력 0x30
                        │        +0x100 JS 그대로 · +0x106 CMP [개수],1 · +0x109 JC +0x174 0xc00000e5
                        │        +0x118 ~ +0x11f  첫 공급자 이름
                        ├─ (2) +0x47   BCryptOpenAlgorithmProvider(&h, 그 이름, NULL, 0)       → 위 사슬 (입력 0x38 …)
                        └─ (3) +0x75   BCryptFreeBuffer (질의 답은 캐시 안 함)
             +0x8a  CMPXCHG.LOCK [0x25ad0] ← 핸들 (다음부터 IOCTL 없음)
```

## 확인된 것

- `[정적]` `BCryptOpenAlgorithmProvider` 길과 `BCryptGenRandom` 길은 같은 `BCryptResolveProviders` 의 **직접** `NtDeviceIoControlFile` (`+0x5eb`) 로 간다. `IoCallKernelDriver` (`+0x276`) 는 `ppBuffer == NULL` 일 때뿐 (`+0x256 JNZ +0x514`).
- `[정적]` 입력은 기능 `0x20000` (`+0x590`), 표지 `0x1a2b3c4d` (`+0x58a`), 길이 `0x30 + Σ((글자수 × 2 + 9) & ~7)` — 에뮬레이터 로그의 `0x38` · `0x30` 과 맞는다.
- `[정적]` 출력 첫 시도 `0x180` 고정 (`+0x7a8`). 길이는 `IOSB.Information` 만 (`+0x601`), `IOSB.Status` 와 출력 안 표지는 안 본다.
- `[정적]` 기본 대답 (`Information` 0 / 0 채움) 은 `+0x4b0` / `+0x7d7` → `+0x7f8` (`0x54f`) → `+0x3ba` → `0xc00000e5`. 로그의 결말과 같다.
- `[정적]` 출력 꼴: 머리 `0x10` · 배열 · 공급자 칸 `0x38` · 이미지 칸 `0x10` · 속성 칸 `0x18` · 문자열, 모두 시작 기준 오프셋 (`-1` = 없음), 범위 검사 있음. 공급자 하나 가장 작은 답 `0x98` (결과서 검산).
- `[정적]` 공급자 0 개 성공 답은 `BCryptOpenAlgorithmProvider +0x3a5 · +0x3a9` 에서 널 역참조 (읽기로 본 것 — 실행 안 봄). `GenRandom` 질의에선 `+0x109 JC` 가 먼저 막아 `0xc00000e5`.
- `[정적]` 올릴 DLL = 출력 공급자 칸 `+0x28` → 이미지 칸 `+0` 의 이름 → `_LoadImage +0x9d LoadLibraryExW(GetSystemDirectoryW + "\" + 이름, 0, 0)`.
- `[정적]` 캐시 `g_pResolveProviderCache` (rva `0x25c10`), 키 = 알고리즘 이름, 성공 답만, 지우지 않음.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① **결과서의 `+0x74b` 는 함수 오프셋이 아니라 주소 끝자리다** — `+0x7fe JMP 0x18000674b` · `+0x505 JNZ 0x18000674b` 의 대상은 함수 오프셋으로 **`+0x39b`** (`CMP [RSP+0x58],EBX` → `+0x3a9 BCryptFree` → `+0x3b7 MOV ECX,R12D` → `+0x3ba WinErrorToNtStatus`). 또 결과서의 "`+0x500 JNZ`" 는 `+0x505 JNZ` (`+0x500` 은 `MOV R12D,EAX`). 이 문서는 `+0x39b` · `+0x505` 로 적었다. 길과 결론은 같다.
- ② 결과서 머리의 "PE_emulator **요청 33** 의 다음 벽" — 요청 33 은 run93 (wait packet) 이다. 이번 일은 run92 (요청 32) 의 다음 벽이고, plan.md 결정 기록 (2026-11-15) 은 "PE_emulator 외부 피드백 — Q173 ②" 로 적었다. 이 문서는 그쪽을 따랐다.
- ③ (보탬) **`BCryptResolveProviders` 도 IOCTL 앞에서 핸들 칸을 다시 연다** — `+0x5a7 MOV RAX,[g_hIoDevice]` · `+0x5ae TEST` · `+0x5b1 JZ +0x742` → `+0x749 EnterCriticalSection(g_csIoInitialize)` → `+0x755` 다시 0 인지 → `+0x761 _IoOpenDevice` → `+0x771 LeaveCriticalSection` → `+0x781 TEST · +0x783 JS +0x793` (열기 음수면 그 status 로 끝) → `+0x789 JMP +0x5b7` (IOCTL). run92 의 `IoCallKernelDriver +0x7e ~ +0x1d3` 과 같은 꼴 — **열기 status 만 보고 칸 값은 안 본다**. attach 가 TRUE 였으면 칸이 이미 차 있어 안 탄다.
- ④ (보탬) **IOCTL 인자 줄** — `+0x5c6 XOR R9D` · `+0x5cd XOR R8D` · `+0x5d7 XOR EDX` (APC 문맥 · APC · Event 0), `+0x5d0` 핸들 `[g_hIoDevice]`, `+0x571 MOVUPS [RSP+0x78]` (IOSB 0) · `+0x5bc` · `+0x5e6` (IOSB 주소), `+0x56e MOV RSI,R14` (입력 = 칸을 채운 그 버퍼), 출력 `R13` · 길이 `R15D` (`+0x5c1` · `+0x5b7`). 출력 버퍼가 8 바이트보다 작으면 제 지역 8 바이트를 쓴다 (`+0x530 R12D = 8` · `+0x688 CMP R15D,R12D` · `+0x68b JC +0x5a0 LEA R13,[RBP-0x70]`) — 관찰된 호출자는 `0x180` 이라 안 탄다.
- 이전 run 이 틀린 것은 없다 — 정정 없음. run92 "아직 확인 필요" 정적 ② 줄에 `(해결됨 → run94)` 를 붙였다 (run81 방식).

## 아직 확인 필요

**동적** (결과서 "동적분석으로 확인해야 할 것" — 이번 묶음은 VM 안 함). **test.exe 로는 못 본다** (bcrypt 를 안 올림 `[관찰]` run44, sample 에 암호 API 없음 `[소스]`). bcrypt 를 올리고 `BCryptOpenAlgorithmProvider` 를 부르는 프로세스가 필요 — Q174 (bcrypt attach 동적) 와 같은 프로세스에서 묶을 후보, 샘플 고르기는 사용자 결정.

- 진짜 KsecDD 가 주는 출력 (제일 중요): `BCryptOpenAlgorithmProvider(L"AES")` 질의의 status · `Information` · 출력 바이트 전부 — `dwInterface`, `pszProvider` 가 채워지나, 속성 개수 (진짜는 몇 개 올 수 있음 `[추측]`), `pKM` 이 `-1` 인가, 공급자가 여럿 오나, `0x180` 을 넘어 한 번 다시 부르나.
- `GenRandom` 질의 (인터페이스 6) 의 진짜 답 — 이름이 `RNG` 인지.
- 진짜 입력이 계산식과 맞나 (`0x38` · `0x30` · `0x78` 후보).
- 캐시 — 같은 이름을 두 번 열면 두 번째엔 IOCTL 이 안 나나.
- `LoadLibraryExW` 경로가 `C:\Windows\System32\bcryptprimitives.dll` 인지.
- 추천 멈춤점 (모두 그 프로세스 조건, ret 오프셋):
  - `bcrypt!BCryptResolveProviders+0x5f2` (IOCTL 반환 직후) — `r eax` · IOSB `dq @rsp+78 L2` · 입력 `poi(@rsp+30)` 를 `dd @rsp+38` 길이만큼 · 출력 `poi(@rsp+40)` 를 `poi(@rsp+80)` 길이만큼. **한 곳에서 칸이 다 나온다.**
  - `bcrypt!BCryptResolveProviders+0x500` (`IoUnpack_ProviderRef` 반환) — `eax` 0 이어야.
  - `bcrypt!BCryptOpenAlgorithmProvider+0x25e` (`BcoapCached` 반환) — `eax` · `[rbp-0x68]` · `[rsp+0x48]`. 캐시 확인용으로 `bcrypt!BCryptResolveProvidersForBcoapCached+0x120` 도 걸어 횟수.
  - `bcrypt!OpenSystemPreferredAlgorithmProvider+0xfc` (질의 반환) — `eax` · `[rbp+0x30]`.
  - `bcrypt!BCryptOpenAlgorithmProvider+0x51d` (`LoadProviderEx` 반환) — `eax`.
  - `bcrypt!_LoadImage+0x9d` (`LoadLibraryExW` 직전) — `du @rcx` (경로).

**정적** (결과서 "다음 요청 후보"):

- 1 (이 다음 벽 후보) `_LoadInterface` · `ValidateFunctionTable` · `BCryptOpenAlgorithmProvider +0x851 / +0x883` 간접 호출 — 공급자 DLL 이 내보내야 할 이름 (`Get…Interface` ↔ `dwInterface` 번호) 과 함수 표 검사. 최소 조건 표 5 · 6 행이 비어 있다. run91 (bcryptprimitives) 지도와 이어서.
- 2 `BCryptEnumProviders` · `BCryptEnumAlgorithms` — `ppBuffer` NULL 크기 질의 길 (`IoCallKernelDriver +0x276`) 이 실제로 쓰이나.
- 4 (후순위) `ksecdd.sys` 의 `0x390400` / 기능 `0x20000` 처리 = **Q173b ③** (진짜 답을 정적으로 보고 싶을 때).
- 5 (후순위) Q173b ① (`GetModuleHandleExW` · `LdrAddRefDll`) · ④ (`NtClose` × 5 · `NtTraceControl` × 4) — 이번에도 안 함.
- `[공백]` 인터페이스 번호 1 (암호) 과 `Get…Interface` 이름 짝, 같은 이름이 여러 인터페이스에 걸치는 알고리즘 (`RSA` 등) 의 선택 (커널 몫).

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 안 고침 — test.exe 는 bcrypt 를 안 올림):
  - 기준: bcrypt pdb 줄에 run94 몫, 근거 로그 run94 줄.
  - 흐름도 앞 글: run94 문단 하나.
  - 3장 "bcrypt 시작 함수" 절: 머리에 run94, 하위 절 **"attach 뒤 첫 사용 — 공급자 찾기 IOCTL (run94)"** 새로, "동적으로 남은 것" 줄 고침.
  - 4장: run92 정적 Q173 줄의 ② 를 `(해결됨 → run94)`, run94 새 줄 셋 (동적 · 정적 남은 것 · PE_emulator 쪽 · 결과서와 다르게 적은 곳 · 보탠 것).
  - 5장: run 표 · 03_sprint 요청서 번호 run94 줄.
- 짐 지도 (짐 지도 바퀴 — test.exe 는 bcrypt 를 안 올려 **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**): 기준 run94 줄 · 판 목록 (쉰째판) · 8장 두 표. 이 DLL 이 쓰는 칸 (`g_pResolveProviderCache` · `0x25ad0` 등 bcrypt `.data`, 공급자 DLL) 은 체크리스트 밖 · test.exe 안 탐이라 안 셈 (run91 · run92 와 같은 판단).
- run92 문서: "아직 확인 필요" 정적 ② 에 `(해결됨 → run94)`.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 디버거 칸을 읽는 줄은 없다 `[정적: 읽은 줄 안에서]`. `_LogBCryptOpenProviderFailure` · WPP 줄은 실패 기록용이라 성공 / 실패 갈림에 안 닿는다.
- **C++ 꾸민 이름** (run92 와 같은 함정): `IoUnpack_ProviderRef` · `WinErrorToNtStatus` · `IoCallKernelDriver` 는 PDB 에 꾸민 이름만 있다. WinDbg 에서 `bcrypt!IoUnpack_ProviderRef` 로 받을지 모르니 `x bcrypt!*IoUnpack_ProviderRef*` 로 먼저 확인하거나 rva (`bcrypt+0x6bd0`) 로 건다 `[추측]` — run92 에서 낸 windbg-kd-runbook pitfalls 제안과 같은 내용.
- **결과서 오프셋 표기**: 점프 대상을 주소 끝자리 (`+0x74b` = `0x18000674b`) 로 적은 줄이 있었다 (위 ①). `JMP` / `Jcc` 대상은 `.asm` 의 주소에서 함수 시작을 빼 오프셋으로 적는 게 맞다 — decompiler 쪽 확인 습관 제안.
- 도구 (결과서 "스크립트 문제"): 없음.
