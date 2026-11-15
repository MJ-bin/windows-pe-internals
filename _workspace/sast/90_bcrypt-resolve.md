# 90 bcrypt-resolve — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q173a** (= Q173 ②, run92 남은 정적). 사용자 결정 2026-11-15: **지금 먼저** (Q104 VM 단계 앞 멈춤 자리에서 끼움, Q173 ① ③ ④ 는 Q173b 로 남김). 끝나면 다시 Q104 앞에서 멈춤. **정적만** — VM 몫은 결과서 "동적으로 확인할 것" 으로만 적는다.
- 빌드: 26100.9457. bcrypt.dll 은 sast/88 에서 analyze·지도 됨 (PDB 붙음). 이미 뽑힌 것: `ms/decomp/bcrypt/BCryptResolveProviders.c`, `BCryptResolveProvidersForBcoapCached.c`, `ms/disassemble/bcrypt/BCryptResolveProviders.asm` (이름만 보고 지나감 — 본문은 아무도 안 읽음). `IoUnpack_ProviderRef` 는 지도 깊이 10 끝이라 아직 안 뽑힘.
- 관련: **sast/88 결과서** `_workspace/sast/88_bcrypt-ksecdd_result.md` 2장 (IOCTL `0x390400` 기능 `0x10500` — 입력 `0x68` 칸 표, `IoCallKernelDriver` 모양, `IoPack_*` 꼴) · 3장 (TRUE 뒤 첫 사용) · "다음 요청 후보" 2. run 문서 `runs/03_sprint/run92-bcrypt-ksecdd.md`.

## 배경 (PE_emulator 쪽 — 외부 피드백)

- run92 덕에 bcrypt `DllMain` 은 이제 통과 (`\Device\KsecDD` 핸들 + 기능 `0x10500` status 0). 다음 벽이 이것이다.
- `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]` (원문 `../PE_emulator/_workspace/02_reviewer/252_ksecdd.md` "05 에게" 1, `../PE_emulator/_workspace/05_tuning_planner/256_tune04.md` T14 줄 — 읽어도 됨):
  - 같은 핸들로 `NtDeviceIoControlFile(코드 0x390400)` 이 **다른 모양**으로 온다: **입력 `0x38` · 출력 `0x180`** (12 줄, `BCryptOpenAlgorithmProvider` 길) · **입력 `0x30` · 출력 `0x180`** (3 줄, 다른 샘플 · `BCryptGenRandom` 길로 보임).
  - 에뮬레이터는 이 모양을 모른다 → 기본 대답 → 호출한 쪽이 **`0xc00000e5`** 로 끝남 (`STATUS_INTERNAL_ERROR` `[추측]`). 악성 샘플 36 개가 여기서 막힘.
  - 로그에 기능 번호(입력 `+4`)가 안 찍혀서, 이것이 기능 `0x20000` 인지는 **에뮬레이터 쪽 `[추측]`**. sast/88 은 `BCryptResolveProviders` 가 `0x20000` 을 쓴다고만 봄.

## 알고 싶은 것

1. **누가 이 IOCTL 을 부르나 — 입력 길이 `0x38`·`0x30` 과 맞추기**:
   - `BCryptResolveProviders` 안의 두 IOCTL 길 — 직접 `NtDeviceIoControlFile` (`+0x5eb/+0x5f2`) 과 `IoCallKernelDriver` (`+0x276/+0x27b`) — 은 각각 언제 타나 (갈림 오프셋·조건). 각 길의 **입력 길이·출력 길이·기능 번호** (입력 `+4`).
   - 입력 길이 `0x38` 과 `0x30` 이 어느 길·어떤 인자에서 나오나 (계산식, 줄). 출력 `0x180` 은 어디서 정해지나 (첫 버퍼 크기? 고정?).
   - `BCryptOpenAlgorithmProvider +0x259` → `BCryptResolveProvidersForBcoapCached` (`+0xcf`, `+0x11b`) → `BCryptResolveProviders` 사슬에서 넘기는 인자 (공급자 질의 종류·모드·알고리즘 이름 등) 만 — 길이 계산에 닿는 것 위주.
   - `BCryptGenRandom` 길 (`+0x157 GetSystemPreferredRngHandle` → `+0x78 OpenSystemPreferredAlgorithmProvider` → …) 이 같은 `BCryptResolveProviders` 로 오나, 다른 함수 (`IoCallKernelDriverQueryLoop` 등) 로 가나 — **이름과 갈림 자리만**.
2. **출력을 어떻게 읽나 — `IoUnpack_ProviderRef` 와 그 앞뒤**:
   - status 를 어떻게 보나: 음수면? `STATUS_BUFFER_TOO_SMALL` (`0xc0000023`) / `STATUS_BUFFER_OVERFLOW` (`0x80000005`) 면 출력에서 필요한 크기를 읽고 **다시 부르나** (몇 번째 칸을 크기로 읽나)? IOSB `Information` 을 읽나?
   - 출력 버퍼 **칸 표**: 오프셋 · 크기 · 뜻 · 검사 (어떤 값이면 실패인가). 특히 머리 (개수·전체 크기·`0x1a2b3c4d` 같은 표지 검사가 있나), 공급자 하나당 꼴 (`CRYPT_PROVIDER_REF` 비슷한 것 — 문자열 오프셋 / `-1` 같은 "없음" 표시, sast/88 의 `IoPack_*` 와 짝인가).
   - 포인터 고치기: 출력 안 오프셋을 출력 버퍼 시작에 더하나 (상대 → 절대)? 범위 검사가 있나 (넘으면 무슨 status)?
3. **`0xc00000e5` 는 어디서 나오나**: `BCryptResolveProviders` · `IoUnpack_ProviderRef` · `BCryptResolveProvidersForBcoapCached` · `BCryptOpenAlgorithmProvider` 중 `0xc00000e5` 를 만드는 줄 (오프셋·조건). 다른 실패 status 들도 표로 (status · 자리 · 조건).
4. **최소 조건 표 (가장 중요)**: `BCryptOpenAlgorithmProvider` 가 `BCryptResolveProvidersForBcoapCached` 를 지나 성공 쪽으로 가려면 바깥 (nt 엔진의 KsecDD 대답) 이 줘야 하는 것 — status · IOSB · 출력 칸 값. 그리고 그 뒤 **어떤 DLL 을 올리나** (공급자 이미지 이름이 출력에서 나와 `LoadProviderEx` (`+0x518`) 로 가나 — 이름만, `LoadProviderEx` 본문은 안 따라감). 공급자가 "0 개" 인 답이면 어디로 가나 (실패 status 인가, 다른 길인가).
5. (작게) **캐시**: `BCryptResolveProvidersForBcoapCached` 가 결과를 캐시하나 (전역 이름·rva) — 같은 프로세스에서 IOCTL 이 몇 번 나는지 가늠하려고. 캐시 키가 뭔지만.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| bcrypt | `^BCryptResolveProviders$` | 1 | 질문 1·2·3 — 본문 (두 IOCTL 길, 출력 읽기) |
| bcrypt | `^IoUnpack_ProviderRef$` | 1 | 질문 2 — 출력 칸 표 (아직 안 뽑힘 → 뽑기) |
| bcrypt | `^BCryptResolveProvidersForBcoapCached$` | 1 | 질문 1·3·5 — 인자·캐시·실패 status |
| bcrypt | `^BCryptOpenAlgorithmProvider$` | 0 | `+0x259` 앞뒤 갈림·반환만 (질문 3·4). 본문 전체 아님 |
| bcrypt | `^IoCallKernelDriver$`, `^IoPack_` | 0 | sast/88 에서 읽음 — `+0x276` 길이 기능 `0x20000` 일 때 다른지만 |
| bcrypt | `^GetSystemPreferredRngHandle$`, `^OpenSystemPreferredAlgorithmProvider$`, `^IoCallKernelDriverQueryLoop$` | 0 | 질문 1 마지막 — 이름·갈림 자리만 |

- 범위: 유저 쪽 bcrypt 가 **무엇을 보내고 무엇을 기대하나** 만.
- **커널 `ksecdd.sys` 의 `0x390400` 처리는 범위 밖** (Q173b ③). 진짜 KsecDD 가 무슨 답을 주는지는 이번에 안 본다 — 출력 칸 뜻은 bcrypt 가 읽는 쪽에서만 정한다.
- `LoadProviderEx`·공급자 DLL 안 (`bcryptprimitives` 함수 표 등), `VsmOpenAlgorithmProvider`, ETW/WPP (`WPP_SF_*`, `McGenEventWrite_*`, `_tlg*`) 는 이름만.
- Q173 ① (`GetModuleHandleExW`·`LdrAddRefDll`), ④ (`NtClose` × 5, `NtTraceControl` × 4) 는 **이번에 안 한다** (Q173b).
- 공용 부품 (`memcpy`, `SzString_CchLength`, `BCryptAlloc`/`BCryptFree`, `WinErrorToNtStatus`, 락) 은 이름만. 단 `WinErrorToNtStatus` 가 `0xc00000e5` 를 만드는 자리라면 그 Win32 오류값은 적는다.
- 함수가 깊으면 질문 4 (최소 조건) 에 닿는 줄기만 끝까지, 나머지는 이름.

## 지도에서 본 것 (`ms/disassemble/bcrypt/_map/entry_*`)

- `BCryptOpenAlgorithmProvider` rva `0x9410`, 깊이 7, caller 3, expanded. `+0x259 BCryptResolveProvidersForBcoapCached`, `+0x518 LoadProviderEx`, `+0x3cf _LogBCryptOpenProviderFailure`, `NtQueryInformationProcess` × 4.
- `BCryptResolveProvidersForBcoapCached` rva `0x6120`, 깊이 8, caller 1. `+0xcf`, `+0x11b BCryptResolveProviders` (두 번), `+0x133 BCryptAlloc`, `+0x150 StringCchCopyW`, `+0x249 BCryptFree`.
- `BCryptResolveProviders` rva `0x63b0`, 깊이 9, caller 4, 크기 2064. 직접 호출: `+0x276 IoCallKernelDriver`, **`+0x4fb IoUnpack_ProviderRef`**, **`+0x5eb NtDeviceIoControlFile`**, `+0x749 EnterCriticalSection` · `+0x761 _IoOpenDevice` · `+0x771 LeaveCriticalSection` (핸들 없을 때 다시 열기로 보임), `BCryptAlloc` × 2 (`+0x374`, `+0x547`), `WinErrorToNtStatus` × 2 (`+0x38e`, `+0x3ba`), `memcpy` × 4, `SzString_CchLength` × 6.
- `IoUnpack_ProviderRef` rva `0x6bd0`, 크기 587, 깊이 10 (지도 끝 — 안쪽 안 펼침).
- `IoCallKernelDriverQueryLoop`: `+0x66 IoCallKernelDriver`, `+0xc1 RtlAllocateHeap`, `+0x173 NtDeviceIoControlFile` (크기 모자라면 늘려 다시 부르는 꼴로 보임 `[추측]`).
- `BCryptGenRandom` `+0x157 GetSystemPreferredRngHandle` → `+0x78 OpenSystemPreferredAlgorithmProvider` (지도 `_calls.tsv` 에 그 함수 호출 줄 없음 — 이름 확인 필요).

## 이미 아는 것

- `[정적]` sast/88: `0x390400` 은 CNG 의 만능 통로. 기능 번호는 입력 `+4` (머리 `+0` = `0x1a2b3c4d`). `BCryptResolveProviders` 는 기능 `0x20000` (`+0x590 MOV [RSI+4],0x20000`, `+0x276` 앞 `MOV ECX,0x20000`). attach 의 `0x10500` 은 출력을 안 읽지만 `BCryptResolveProviders` 는 `+0x4fb IoUnpack_ProviderRef` 로 출력을 읽음.
- `[정적]` sast/88: `IoCallKernelDriver` 는 status 부호만 봄, 출력 길이 포인터가 NULL 이면 출력 복사 안 함. 입력은 `IoPack_*` 가 "머리 + 뒤에 문자열" 꼴로 채우고 없는 칸은 `-1`.
- `[관찰 — 에뮬레이터 로그]` 위 "배경" — 입력 `0x38`/`0x30`, 출력 `0x180`, 결과 `0xc00000e5`.
- `[추측]` 출력 `0x180` 은 첫 시도용 고정 크기이고, 모자라면 크기를 받아 다시 부른다 — 코드로 맞는지 볼 것.
- `[추측]` (에뮬레이터 쪽) 입력 `0x38`·`0x30` 이 기능 `0x20000` — 코드로 맞는지 볼 것.

## 받고 싶은 것

- 결과서 `_workspace/sast/90_bcrypt-resolve_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: 차례 표 (함수+오프셋 · syscall · 입력 길이 · 출력 길이 · 기능 번호 · 갈림 조건). 입력 칸 표 (sast/88 2-b 양식).
- 질문 2: 출력 칸 표 (오프셋 · 크기 · 뜻 · 검사 · 실패 status).
- 질문 3: 실패 status 표.
- 질문 4: **최소 조건 표** — sast/88 질문 5 표와 나란히 볼 수 있게 (syscall · 입력 (IOCTL·기능·길이) · 기대 답 (status · IOSB · 출력 칸) · 안 맞으면 가는 자리). 가능하면 "공급자 하나 (예: `bcryptprimitives.dll`) 를 돌려주는 가장 작은 출력" 을 칸별로 한 장.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (지금 우리 = 이 모양 모름 → 기본 대답 → `0xc00000e5`).
- 끝에 **"동적으로 확인할 것"** (진짜 KsecDD 가 돌려주는 출력 실제 값 등 — 칸만, `[추측]`, 추천 멈춤점) 과 **"다음 요청 후보"**.
