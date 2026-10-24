# 34 dll-search-path — 정적 분석 결과

- 요청서: `_workspace/sast/34_dll-search-path.md` (Q53, 02_sprint)
- 판정: **분석성공** (질문 1~6 전부 답함. 실행 때 실제 값은 `[추측]`/동적으로 넘김)
- 날짜: 2026-10-24
- 모듈: ntdll 26100.9457 (`ms/bin/.../System32/ntdll.dll`, pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1`). 이미 분석된 모듈이라 analyze 는 안 했다 (`.gpr`, `_index.tsv`, `SetPdb:` 줄 있음).
- 스크립트 문제: 없음.
- 점프 목적지는 전부 **"목적지 주소 − 그 함수 시작"** 으로 적었다 (스크립트로 `.asm` 의 모든 `Jcc/JMP` 를 한 번에 바꿔서 확인 (`LdrpSearchPath`·`LdrpGetDllPath`·`RtlpGetCachedPath`·`RtlpComputeDllPath` 등 8개; `RtlpComputePath` 는 점프 표까지 따로 계산)).
- 한 일 (전부 몇 초):
  - `export` 1차: `'^(LdrpInitializeDllPath|LdrpSearchPath|LdrpComputeLazyDllPath|LdrpGetDllPath|RtlpGetCachedPath|RtlpLookupSafeCurDirList)$' 1` (35개 나옴).
  - **요청서 밖으로 더 뽑은 것** (이유: 폴더 목록 문자열을 실제로 만드는 곳이 `RtlpGetCachedPath` 안이 아니라 **함수 포인터로 넘기는 콜백** `RtlpComputeDllPath` 라서 요청서 depth 로는 안 닿는다).
    - 2차: `'^(RtlpComputeDllPath|RtlpComputeDllPathWithOptions|RtlGetExePath|RtlGetSearchPath)$' 1` (11개).
    - 3차: `'^(RtlpGetDirPath|RtlpNeedCurrentDirectoryForExePath)$' 0` (exe 폴더 자르기).
    - 4차: `'^(LdrpGetNtPathFromDosPath|LdrpGetFullPath)$' 0` (파일 있는지 보는 곳).
  - `refs` 3번: `RtlpSystemDirs`, `RtlpSystem32Dirs`, `0x1d2380` (SafeDllSearchMode 캐시 칸).
  - 원본 바이트(스크래치 파이썬, 커밋 안 함)로 읽은 것: 검색 순서 표 `ntdll+0x17e340`, 문자열 `"PATH"`·`"."`·`"SafeDllSearchMode"`·`\system;`·`forwarders\alt;` 등, 점프 표 `RtlpComputePath+0x750`.
  - `LdrpResolveDllName` 은 앞 `+0x92`·`+0x13b`·`+0x298` 과 호출 목록만 읽었다.
- 버전 대조: `_calls.tsv` 의 `LdrpInitializeProcess +0x1bb6 / +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpLoadDll +0xfb / +0x100 → LdrpLoadDllInternal` (SKILL 알려진 사실과 **일치**). 요청서가 적은 오프셋 `LdrpInitializeProcess +0x1313`, `LdrpSearchPath +0x36d/+0x225/+0x244/+0x4f7`, `LdrpComputeLazyDllPath +0x62`, `LdrpGetDllPath +0xa8` 도 전부 `_calls.tsv` 의 call 자리와 일치.
- 이름 확인: 요청서 다섯 함수 모두 `_index.tsv` 에 PDB 이름 그대로 (rva·크기: `LdrpInitializeDllPath 0x899a0/167`, `LdrpSearchPath 0x8eb0/1463`, `LdrpComputeLazyDllPath 0xb9870/295`, `LdrpGetDllPath 0xb9f30/635`, `RtlpGetCachedPath 0xba2f0/364`). 별칭 하나: `LdrpSearchPath +0x29a` 는 "free 함수" (이름 별칭 9개, 요청서 말대로 이름 믿지 말 것) — 아래에서 "버퍼 해제" 로만 쓴다.

## 파일 (레포 밖, 커밋 안 함)

- `ms/disassemble/ntdll/` `LdrpInitializeDllPath` `LdrpComputeLazyDllPath` `LdrpGetDllPath` `RtlpGetCachedPath` `RtlpComputeDllPath` `RtlpComputePath` `RtlpGetDirPath` `RtlpLookupCurDirSetting` `LdrpSearchPath` `LdrpResolveDllName` `LdrpGetNtPathFromDosPath` `LdrpGetFullPath` `RtlpLookupSafeCurDirList` `RtlpComputeDllPathWithOptions` `RtlGetExePath` `RtlGetSearchPath` (모두 `.asm`) + `_calls.tsv` + `_refs/{RtlpSystemDirs,RtlpSystem32Dirs,0x1d2380}.tsv`
- `ms/decomp/ntdll/` 같은 이름 `.c` (흐름은 전부 `.asm` 으로 확인. `.c` 는 보조)

## 한눈에 (한 장 그림)

```
LdrpInitializeProcess +0x1313  LdrpInitializeDllPath(0, 0, &P)     P = 스택 [RSP+0x380], 0x80바이트
   └ P 를 0 으로 밀고, +0x18 = 0, +0x20 = 0 만 쓴다.  폴더 목록 = 안 만듦  ("나중에" 표시)
        │   (exe 항목 LdrpImageEntry 는 +0x1374 에서야 생기므로 이 시점엔 만들 수도 없다)
        ▼
   첫 LdrpMapDllSearchPath → LdrpSearchPath(ctx, P, ...)
LdrpSearchPath +0xe4   [P+0] == 0 이면  +0x36d LdrpComputeLazyDllPath(P)
   └ +0x62 LdrpGetDllPath(P+0x20값, P+0x18값, 출력들...)
        └ +0xa8 RtlpGetCachedPath(&RtlpDllSearchPath, RtlpComputeDllPath, 0, 0)
             ├ 캐시에 있으면 그대로 (참조 수 +1)
             └ 없으면 +0xbe RtlpComputeDllPath(0, 0)
                  └ +0x7a RtlpComputePath(순서 표 한 줄, 5칸, 0, 0)   ← 문자열을 실제로 짓는 곳
LdrpSearchPath 고리: ';' 로 한 칸 → "폴더\" + 이름 → LdrpResolveDllName
   └ +0x298 LdrpGetFullPath → +0x13b LdrpGetNtPathFromDosPath → NtQueryAttributesFile
   처음 성공한 칸에서 멈춤
```

## 먼저 알아 둘 칸 — 경로 구조체 `P` (0x80바이트)

`ctx+0x10` 이 가리키는 구조체. PDB 에 타입이 없어서(`llvm-pdbutil` 확인) 오프셋 + 쓰는 줄로 적는다.

| 칸 | 쓰는 곳 | 뜻 |
|---|---|---|
| `+0x00` qword | `LdrpComputeLazyDllPath +0x91`, (밖에서 문자열을 줄 때) `LdrpInitializeDllPath +0x5b` | **폴더 목록 문자열 포인터** (`"폴더;폴더;…;"` UTF-16, 끝에 `;`). **0 이면 "아직 안 만듦"**. `LdrpSearchPath +0xe4` 와 `LdrpComputeLazyDllPath +0x2c` 가 이 칸을 본다 |
| `+0x08` qword | `LdrpComputeLazyDllPath +0x84` | 목록 안 `"."`(현재 폴더) 칸의 시작 주소. **플래그 `0x2000` 이 있을 때만 값**, 아니면 0 (`LdrpGetDllPath +0x197`) |
| `+0x10` qword | `LdrpComputeLazyDllPath +0x94` | 패키지 앱 폴더 목록 시작. 일반 프로세스는 0. 값이 있으면 `LdrpSearchPath +0x102` 가 이쪽부터 본다 |
| `+0x18` dword | `LdrpInitializeDllPath +0x46` | 플래그 = `인자2 & ~1` (부모 `DependentLoadFlags` 에서 온 값). 항목의 `ImplicitPathOptions` 로 복사됨 (sast/17). **0,0 이면 0** |
| `+0x20` qword | `LdrpInitializeDllPath +0x42` | 부모 모듈 `FullDllName.Buffer` (플래그 갈래). **0,0 이면 0** |
| `+0x28 ~ +0x77` (0x50바이트) | `LdrpGetDllPath +0xca ~ +0xec` (복사) | 캐시 블록 머리 사본: `+0x28` dword[5] 칸 종류 코드, `+0x40` qword[5] 칸마다 시작 주소, `+0x70` word 칸 수(5). `LdrpSearchPath` 는 안 읽는다 `[정적]`. 읽는 곳은 못 찾음 (공백 3) |
| `+0x78` dword | `LdrpGetDllPath +0x118` | 목록 문자열 바이트 수 |
| `+0x7c` byte | `LdrpComputeLazyDllPath +0x8d` | 계산 끝 표시 (1) |

캐시 블록(`RtlpComputePath` 가 할당, `RtlpDllSearchPath` 가 가리킴) 머리: `+0x00` 종류 dword[5], `+0x18` 칸 주소 qword[5], `+0x48` 칸 수, `+0x50` 참조 수, `+0x58` 만들 때 `ProcessParameters.EnvironmentVersion`(PDB `+0x3f8`), `+0x60` `LdrpAppPackagesPathVersion`, `+0x68` 패키지 폴더 주소, `+0x70` 문자열 바이트 수, `+0x74` 바이트 플래그, `+0x78` "." 칸 주소, **`+0x80` 부터 문자열**. 그래서 `P+0x00` 은 캐시 블록 `+0x80` 을 가리키는 것이지 따로 복사한 문자열이 아니다 (`LdrpGetDllPath +0xf0 ~ +0xf7`).

## 질문별 답 [정적]

### 1. `LdrpInitializeDllPath(0, 0, &P)` — 그 자리에서 만드나?

- **답: 아니다. "나중에 만들 것" 표시만 한다.** (요청서 추측이 맞음.)
- 하는 일 (`LdrpInitializeDllPath.asm`, 인자 `RCX=a1`, `RDX=a2`, `R8=P`):
  1. `+0x13 ~ +0x35` `P` 의 0x80바이트를 전부 0 으로 민다 (`MOVUPS` 8번).
  2. `+0x3a TEST DL,1` — `a2` 의 **비트 0** 을 본다.
  3. 갈래 **A (비트 0 이 켜짐)**: `+0x3d JZ +0x56` 을 안 타고 `+0x3f` 로: `P+0x20 = a1` (`+0x42`), `P+0x18 = a2 & ~1` (`+0x46`), 끝 (`+0x54 RET`). → 부모 경로 + 플래그를 적어 둠. `LdrpMapDllSearchPath +0x330` 이 이 갈래를 쓴다 (`a2 = (플래그&마스크)|1`, sast/21).
  4. 갈래 **B (비트 0 이 꺼짐 + `a2 == 0`)** — **우리 경우 (0, 0)**: `+0x3d JZ +0x56` → `+0x56 TEST RDX,RDX` → `+0x59 JZ +0x3f`. 즉 갈래 A 와 같은 `+0x3f` 로 가서 `P+0x20 = 0`, `P+0x18 = 0`. **나머지 칸은 전부 0**. 호출 없음, 할당 없음.
  5. 갈래 **C (`a2` 가 0 이 아니고 비트 0 이 꺼짐)**: `P+0x00 = a2` (`+0x5b`), 로그 "DLL search path passed in externally: %ws" (`+0x88 LdrpLogInternal`), `+0xa3` 꼬리호출 `LdrpLogDllStateEx2`. → 밖에서 만든 문자열을 그대로 받는 갈래 (shim·verifier 등 쪽 caller 가 쓸 것 `[추측]`, 범위 밖).
- **왜 나중이어야 하나** `[정적]`: 목록 첫 칸(exe 폴더)은 `LdrpImageEntry` 에서 읽는다 (질문 3). 그런데 `LdrpInitializeProcess` 는 `+0x1313` 에서 이 함수를 부른 **뒤** `+0x1359 LdrpAllocateModuleEntry` → `+0x1374 MOV [LdrpImageEntry],RAX` 로 exe 항목을 만든다. `+0x1313` 시점엔 읽을 항목이 아직 없다.
- 이어서 `+0x1318 ~ +0x1349` 에서 exe 로드 문맥 `[RSP+0x2b0]` 의 `+0x10` 칸 (`[RSP+0x2c0]`) 에 `&P` (`[RSP+0x380]`) 를 넣는다 (sast/21 과 같음).

### 2. 실제 폴더 목록은 어디서 만들어지나? 사슬이 맞나?

- **답: 사슬은 맞다 (예). 다만 목록을 "짓는" 곳은 한 단계 더 아래다.** `RtlpGetCachedPath` 는 캐시 창구이고, 짓는 건 콜백 `RtlpComputeDllPath → RtlpComputePath`.
- 함수마다 한 줄:
  - `LdrpSearchPath +0xe4 CMP [R12],RBP / +0xe8 JZ +0x36a → +0x36d`: `P+0` 이 0 이면 `LdrpComputeLazyDllPath(P)` (실패=음수면 `+0x376 JNS` 안 타고 `+0x37c JMP +0x2e9` 로 오류 반환). **조건: `P+0` 이 0 일 때만, 호출마다** — 한 번 채우면 다시 안 탄다.
  - `LdrpComputeLazyDllPath`: `+0x19 ~ +0x27` SRW 락 `LdrpPathLock` 잡기 → `+0x2c CMP [RBX],RDI / +0x2f JNZ +0xbf` 로 **락 안에서 한 번 더 확인** (이미 채워졌으면 건너뜀) → `+0x62 LdrpGetDllPath(P+0x20값, P+0x18값, 출력들)` → 성공이면 `P+0x08/+0x00/+0x10/+0x7c` 채우고 로그 "DLL search path computed: %ws" (`+0x9d ~ +0xba`) → `+0xbf ~ +0xc6` 락 풀기. 실패면 로그 "Lazy DLL search path computation failed with status" (`+0xd9`).
  - `LdrpGetDllPath`: **플래그 `P+0x18` 로 어떤 계산기를 쓸지 고른다.** 플래그가 0 (우리 경우) 이면 `+0x94 ~ +0xa8` 에서 `RtlpGetCachedPath(&RtlpDllSearchPath, RtlpComputeDllPath, 0, 0)` (`R8=0`, `R9=0`). 플래그가 있으면 `+0x141 ~ +0x192` 로 가서 `RtlpDllSearchPathWithOptions`/`RtlpComputeDllPathWithOptions` 를 같은 방식으로 쓴다. `+0xad` 뒤: 결과 블록의 `+0x80` 을 `*arg3`(문자열), 종류 사본 0x50바이트를 `arg6`, `+0x70` 을 `arg5`, `+0x68` 을 `arg7`, (플래그 `0x2000` 일 때만) `+0x78` 을 `arg4` 에 쓴다. 이름에 `\` 가 있는 경로를 `RtlDetermineDosPathNameType_U` 로 보는 줄(`+0x1ad`, `+0x1d3`)은 **플래그 `0x100` 이나 `8` 이 있을 때만** (`+0x73`, `+0x7b` JNZ `+0x1aa`) — 우리 경우 안 탄다.
  - `RtlpGetCachedPath`: `+0x27 JNZ +0xa7`, `+0x2c JNZ +0xa7` — **`R8`·`R9` 가 둘 다 0 일 때만 캐시를 쓴다** (우리 경우). 캐시 블록이 있고 (`+0x43`), `LdrpAppPackagesPathVersion` 이 같고 (`+0x54`), 환경 변수 판이 같으면 (`+0x78`; `+0x74` 바이트 플래그가 있으면 판 검사 생략 `+0x5e`) 참조 수만 올려 돌려준다 (`+0x7a`). 아니면 `+0xbe CALL RtlpComputeDllPath` (콜백 주소 비교 `+0xb9 CMP RBP,RAX` — 같으면 직접 호출, `RtlpComputeDllPathWithOptions` 도 같은 방식 `+0xe9`, 둘 다 아니면 `+0x152` CFG 간접 호출). 새 블록이면 참조 수 1 (`+0xcb`), 캐시 칸에 넣을 땐 +1 (`+0x114`), 옛 블록은 참조 수가 0 이면 `RtlFreeHeap` (`+0x142`).
  - **`RtlpComputeDllPath` (콜백, `0xba560`, 222바이트)**: SafeDllSearchMode 를 읽어(`+0x4b`) **순서 표 한 줄**을 고르고 (`+0x5e ~ +0x6c`), SetDllDirectory 폴더가 있으면 `+0x2e JNZ +0xaf` 로 네 번째 줄(`ntdll+0x17e37c`)로 바꾼다. 읽기용 락 `LdrpDllDirectoryLock` (`+0x59`). 그리고 `+0x7a RtlpComputePath(줄, 5, R8, 0)`. 결과 블록에 `+0x58 = EnvironmentVersion`, `+0x74 = 0` (`+0x93`, `+0x97`).
  - **`RtlpComputePath` (`0xba7a0`, 1885바이트)**: 두 번 돈다. 1차 `+0x7c ~ +0xd9`: 5칸을 돌며 **바이트 수**를 센다. 큰 `switch` 가 칸 종류 코드 하나로 갈라진다 (`+0x86 CMP ECX,3` → `+0x291` PATH, `+0x8f CMP ECX,7` → `+0x367`, 그 밖은 점프 표 `+0xa1` → `+0xab JMP RCX`). `+0xe3 JA +0x5ff` 로 **0xfffe 바이트를 넘으면** 상태 `0xc0000106` (이름 너무 김) 으로 포기. 그다음 `+0xfb RtlAllocateHeap(ProcessHeap, 0, 크기)` (`+0x100`), 2차 `+0x8f3 ~ +0x9a6` 가 칸마다 문자열을 복사 (`RtlCopyMemory`) 하고 `;`(`0x3b`) 를 붙인다. 칸 종류 코드 → 하는 일은 질문 4 표.
- **다른 이름과 구별**: `LdrGetDllPath`(`0xb9f00`, 34바이트) 는 `LdrpGetDllPath` 의 내보내기용 얇은 포장 (`LdrpGetDllPath` caller 목록에 있음). `RtlGetExePath`(`0x108a90`) 는 `RtlpGetCachedPath(&RtlpExeSearchPath, RtlpComputeExePath, !RtlpNeedCurrentDirectoryForExePath(), 0)`, `RtlGetSearchPath`(`0xb8680`) 는 `RtlpGetCachedPath(&RtlpSearchPath, RtlpComputeSearchPath, 0, 0)` — **같은 캐시 창구를 쓰지만 다른 목록** (프로세스 실행 파일 찾기·`SearchPath` API 용). 로더의 DLL 검색(`LdrpSearchPath`)은 **안 쓴다** `[정적: 호출 관계]`.

### 3. exe 폴더는 어떤 값에서 오나?

- **답: `LdrpImageEntry` 의 `FullDllName.Buffer` (항목 `+0x50`) 에서 읽는다. 그 값은 PEB `ProcessParameters.ImagePathName` 의 UNICODE_STRING 사본이다.** `RtlGetExePath`·`GetModuleFileName` 류가 아니다.
- 읽는 줄: `RtlpGetDirPath +0x28 MOV RAX,[LdrpImageEntry]` → `+0x2f MOV RBX,[RAX+0x50]`. (그 앞 `+0x1a ~ +0x26`: TEB `GS:[0x18]`(= `NtTib.SubSystemTib`) 가 0 이 아니면 `+0x87` 로 가서 다른 칸 `[[SubSystemTib+8]+8]` 을 쓴다. Win32 프로세스는 0 일 것 `[추측]`.)
- 값이 채워지는 줄: `LdrpInitializeProcess +0x13aa MOVAPS XMM0,[RSP+0xf0]` → `+0x13b9 MOVDQU [RCX+0x48],XMM0` (`RCX = LdrpImageEntry`). `[RSP+0xf0]` 이 `ProcessParameters.ImagePathName` (sast/14: `+0x6c8` 에서 `ProcessParameters +0x60`). 즉 항목 `+0x48 FullDllName` 은 16바이트 구조체 사본이고 **글자 버퍼는 `ProcessParameters` 의 것을 그대로 가리킨다**.
- 잘라서 폴더로 만드는 줄 (`RtlpGetDirPath`): `+0x33 MOV EDX,0x5c` (`\`) → `+0x3b wcschr` (첫 `\`) → `+0x48 wcsrchr` (**마지막** `\`) → `+0x55 CMP RAX,RSI` / `+0x58 LEA RCX,[RAX+2]` / `+0x5c CMOVNZ RCX,RAX` (첫 `\` 와 마지막 `\` 가 같은 글자면 — 루트 `C:\` — 뒤에 한 글자 더 포함, 다르면 마지막 `\` 자리) → `+0x65 SUB RCX,RBX` → `+0x6b MOV [RDI],RCX`: **길이(바이트) = 마지막 `\` 위치 − 시작**. 즉 끝의 `\` 는 빼고 돌려준다 (`C:\a\bin\test.exe` → `C:\a\bin`).
- 부르는 곳: `RtlpComputePath +0x555 RtlpGetDirPath(0, &[RSP+0x50])` (인자 0 = exe 경로 쓰기). 1차에서 길이만 `+0x56c` 로 더하고, 2차 `+0x590` 에서 `RtlCopyMemory` 후 `;` 를 붙인다 (`+0x4c8`). 부모 경로를 줄 때(`R8 != 0`, `LOAD_WITH_ALTERED_SEARCH_PATH` 플래그 8 인 경우 `[추측: 상수 이름]`; `LdrpGetDllPath +0x9e MOV R8,RBP`, `RBP` 는 `+0x1c2`/`+0x1df` 에서 그 경로가 됨)는 `+0x3f6 RtlpGetDirPath(그 경로)` 로 **그 경로의 폴더**를 쓴다. 우리 경우 `R8 = 0` 이라 exe 쪽.
- 뒤에 `\` 는 여기서 안 붙인다. `LdrpSearchPath +0x1fe ~ +0x211` 이 후보를 만들 때 마지막 글자가 `\`(`0x5c`)·`/`(`0x2f`) 가 아니면 `\` 를 붙인다.

### 4. 기본 목록의 순서 (인자 0, 0)

- **답 (한 줄 그림)**:

```
exe 폴더 ; System32 ; Windows\system ; Windows ; . ; (PATH 값) ; (패키지 폴더: 보통 빈칸)
 [칸 5]    └──── 칸 2 (RtlpSystemDirs 한 덩어리) ────┘  [칸 4]  [칸 3]      [칸 8]
```

  - 이것은 **SafeDllSearchMode 가 켜진 줄(= 기본)**. 칸 코드로는 `[5, 2, 4, 3, 8]`.
  - 코드에서 본 것 `[정적]`: 칸 순서와 각 칸의 출처. 실제 문자열(드라이브 글자 등)과 레지스트리 값은 `[동적]`.
- **순서 표** (`RtlpComputeDllPath +0x65 LEA RAX,[0x18017e340]`, 줄 크기 20바이트 = dword 5개, 줄 번호 = `EBX`):

| 줄 | 주소 | 칸 코드 | 언제 |
|---|---|---|---|
| 0 | `ntdll+0x17e340` | `5, 4, 2, 3, 8` = exe 폴더, **현재 폴더**, 시스템, PATH, 패키지 | SafeDllSearchMode = 0 |
| 1 | `ntdll+0x17e354` | `5, 2, 4, 3, 8` = exe 폴더, 시스템, **현재 폴더**, PATH, 패키지 | SafeDllSearchMode = 1 (**기본**) |
| 2 | `ntdll+0x17e368` | `1, 2, 3, 8, 9` | `RtlpComputeDllPath` 로는 못 닿음 (아래) |
| 3 | `ntdll+0x17e37c` | `5, 0, 2, 3, 8` = exe 폴더, **SetDllDirectory 폴더**, 시스템, PATH, 패키지 (현재 폴더 **빠짐**) | `LdrpDllDirectory+8 != 0` (`RtlpComputeDllPath +0x2e JNZ +0xaf` → `+0xc3 JZ +0xce` 안 탐 → `+0xc5` → `+0xcc JMP +0x70`) |

  - 줄 고르는 값: `RtlpComputeDllPath +0x30 MOV EBX,[0x1801d2380]` (캐시, 처음엔 0) → 0 이면 `+0x4b RtlpLookupCurDirSetting("SafeDllSearchMode", 기본값 1, &캐시)`. 그 안(`RtlpLookupCurDirSetting`): 보안 프로세스(`LdrpIsSecureProcess`)면 **1** (`+0x2b`, `+0xcd`); 아니면 레지스트리 `\Registry\MACHINE\System\CurrentControlSet\Control\Session Manager` 의 값 **`SafeDllSearchMode`** 를 읽어 (`+0x5f NtOpenKey`, `+0xa1 NtQueryValueKey`), **결과 길이가 0x10(= 헤더 + DWORD)이고 값이 0 또는 1 이면 그 값** (`+0xe3 ~ +0xef`), 아니면 기본값 **1**. → 값이 없거나 이상하면 1 = 위 그림.
  - 줄 2 는 `EBX` 가 0 또는 1 뿐이라 (이 캐시 칸을 쓰는 코드는 `RtlpComputeDllPath` 하나뿐, `refs 0x1d2380`) 못 닿는다 `[정적]`. 칸 코드 `9` 는 `RtlpComputePath` 가 처리하지 않아 `+0x9b JA +0x73f` 로 fail-fast (`INT 0x29`). 다른 계산기가 쓰는 줄일 것 `[추측]`.
- **칸 코드 → 하는 일** (`RtlpComputePath` 의 점프 표 두 개: 1차 크기용 `ntdll+0xbaef0`, 2차 복사용 `ntdll+0xbaf14`; 아래 오프셋은 `RtlpComputePath` 시작 기준):

| 코드 | 크기(1차) | 복사(2차) | 뜻 `[정적: 읽는 전역]` + 해석 |
|---|---|---|---|
| 0 | `+0x6d0` | `+0x6dc` | `LdrpDllDirectory` (SetDllDirectory 가 넣는 폴더) |
| 1 | `+0x54e` | `+0x583` | exe 폴더 (`RtlpGetDirPath(0)`) |
| 2 | `+0x456` | `+0x524` | **`RtlpSystemDirs`** (UNICODE_STRING, 이미 `;` 로 끝남). 복사 뒤 `;` 안 붙임 |
| 3 | `+0x291` | `+0x4da` | **환경 변수 `PATH`**: `FastPebLock` 잡고 (`+0x298`) `RtlQueryEnvironmentVariable(NULL, L"PATH", …)` 두 번 (`+0x2bc` 길이 → `+0x2ed` `RtlAllocateHeap` → `+0x324` 값), 없으면(`0xc0000100`, `+0x342`) 빈 칸. 현재 프로세스 환경 블록 `[정적]` |
| 4 | `+0x43a` | `+0x270` | 글자 `.` + `;` (4바이트). 현재 폴더. **상대 `.`** 이라 열 때 현재 디렉터리로 풀린다 |
| 5 | `+0x3e9` | `+0x48e` | 응용 프로그램 폴더: 부모 경로 인자(`R8`) 있으면 그 폴더, **없으면 exe 폴더** (`+0x3e9 TEST R13,R13` → `+0x405 TEST R15,R15 JZ +0x54e`) |
| 6 | `+0x5c2` | `+0x5e4` | `LdrpUserDllDirectories` 목록 (AddDllDirectory 로 넣은 폴더들) `[추측: 이름 근거]` |
| 7 | `+0x367` | `+0x3a1` | `RtlpSystem32Dirs` (System32 하나). 표 4줄엔 없음 — 아래 시스템 폴더 대체 모드에서만 `[정적]` |
| 8 | `+0xb1` | `+0x1aa` | `LdrpAppPackagesPath` (패키지 앱). 길이 0 이면 건너뜀 (`+0xb8 TEST AX,AX`) |

- **시스템 폴더 덩어리 `RtlpSystemDirs` 의 내용** `[정적]` (만드는 곳 `LdrpInitializeProcess +0x1065 ~ +0x1161`, 쓰는 줄 `+0x1161`; `refs RtlpSystemDirs`):
  - `<NtSystemRoot>\System32;` + `<윈도우 폴더>\system;` + `<윈도우 폴더>;` (끝 `;`). 문자열 조각은 `\system;`(`ntdll+0x19bd08`), `;`(`ntdll+0x197588`). 첫 조각은 `RtlpSystem32Dirs` 와 같은 버퍼의 앞부분 (`+0x1092` 에서 끝 `\` 를 `;` 로 바꿈, `+0x109e` 에서 `RtlpSystem32Dirs`·`LdrpSystem32` 에 저장).
  - **조건 갈래**: `ProcessParameters.Flags & 0x20000000` (`+0x10b9 ~ +0x10c4`) 이 켜지면 System32 조각 **뒤에** `…\forwarders\alt;`(`ntdll+0x19bbb0`, `LdrpIncludeAlternateForwarders` 가 참일 때 `+0x10c9`)·`…\forwarders;`(`ntdll+0x19bbd0`) 가 붙는다. `LdrpPolicyBits & 1` 이 켜지면 윈도우 폴더 두 조각은 안 붙는다 (`+0x1007 ~ +0x100d`, `+0x1111 CMP R15,RBX / JBE`). 보통 어떤지는 `[동적]`.
  - `<윈도우 폴더>` 문자열(`[RSP+0x120]`)의 출처 줄은 안 확인 `[추측: NtSystemRoot]`. 16비트 폴더 `system` 이 들어가는 건 문서와 같은 옛 호환 칸.
- **시스템 폴더 대체 모드** `[정적]`: `RtlpComputePath +0x21 / +0x2b / +0x32` 가 `LdrSystemDllInitBlock+0xa0` 의 비트 60~61 (`R14`) 을 읽는다. 이 값이 **1** 이면 칸 2(`+0x484`)·칸 7(`+0x397`)에서 `[RSP+0x48]` 에 시스템 폴더 구조체를 적어 두고, 1차 고리가 끝난 뒤 `+0x135 TEST RDI,RDI / +0x138 JNZ +0x671` 로 **목록 전체를 시스템 폴더 하나로 바꾼다**. 0 이면 이 갈래는 안 탐. 보통 0 일 것 `[추측]` → 공백 2.
- **`RtlpLookupSafeCurDirList` 와 SafeDllSearchMode**:
  - **SafeDllSearchMode 는 이 목록의 순서를 정한다** (위 표 줄 0/1). 예.
  - **`RtlpLookupSafeCurDirList` (`LdrpSearchPath +0x4f7`) 는 test.exe 처럼 플래그가 없을 때 관여하지 않는다.** 이유: 그 호출은 "지금 칸이 현재 폴더 칸" 일 때만 간다. `LdrpSearchPath +0x1cc CMP RBX,[R12+8]` / `+0x1d4 SETZ SIL` → `+0x24b TEST SIL / +0x24e JNZ +0x4ef`. 그런데 `P+0x08`("." 칸 주소)은 **플래그 `0x2000`**(`LdrpGetDllPath` 의 `bVar15`, `+0x197`) 일 때만 채워지고, 아니면 0 이라 `RBX` 와 같을 수가 없다. 플래그는 `P+0x18` 에서 오는데 우리 경우 0.
  - 참고: 그 갈래가 가면 `RtlpLookupSafeCurDirList` 가 레지스트리 `…\Session Manager\Safe Load Prefixes` 키와 `ProgramFiles`·`DontIncludeProgramFiles` 값, 환경 변수로 "안전한 현재 폴더 목록"을 만든다 (`+0x118 NtOpenKey`, `+0x18d NtQueryValueKey`, `+0x1b2 RtlQueryEnvironmentVariable_U`, `+0x2bb NtEnumerateValueKey`). 현재 폴더에서 찾은 DLL 이 목록에 없으면 **그 결과를 버리고** 다음 칸으로 가며 ETW `CurDirDllLoadFailureWarning/Error` 를 쓴다 (`+0x52c ~ +0x573`, `+0x4ae`).

### 5. `LdrpSearchPath` 가 목록을 어떻게 도나

- **답**: 예 — **처음 찾은 칸에서 멈춘다.** 돌려주는 값은 성공이면 그 상태(0 이상), 끝까지 못 찾으면 **`0xc0000135`** (`STATUS_DLL_NOT_FOUND`).
- 순서 (`LdrpSearchPath.asm`):
  1. `+0xb2`, `+0xdf` 로그 → `+0xe4` 목록이 비었으면 만들기 (질문 2).
  2. **시작 칸 고르기**: `R13`(= 인자 `&B`, "이어하기 표시") 의 첫 칸이 0 이 아니면 `+0xfc JNZ +0x3ce` → `RCX = [R13+8]` 에서 시작 (**이전에 찾고 멈춘 칸의 다음**). 아니면 `P+0x10` (패키지 폴더) 이 있으면 그것 (`+0x102`), 없으면 `P+0x00` (`+0x10c`).
  3. **버퍼 크기 정하기** `+0x113 ~ +0x157`: 목록을 `;`(`0x3b`) 로 끊어 가장 긴 칸 길이를 구한다. `+0x172 LdrpAllocateUnicodeString(&buf, 가장 긴 칸 + 이름 길이 + 2)`.
  4. **칸 돌기** (고리 머리 `+0x1b1`, `+0x1b6`):
     - 빈 칸(`;;`)은 건너뜀 (`+0x1c2 CMP AX,0x3b / +0x1c6 JZ +0x3d7` → `+0x1b6`).
     - 칸 글자를 버퍼로 복사 (`+0x1ea MOV [RCX],AX`), `;` 에서 멈춤 (`+0x1e4 / +0x1e8 JZ +0x1f9`). 빈 폴더면 건너뜀 (`+0x1fc JZ +0x1b6`).
     - 끝 글자가 `\` 도 `/` 도 아니면 `\` 붙임 (`+0x202 ~ +0x211`), 길이 기록 (`+0x21b`).
     - **`+0x225 RtlAppendUnicodeStringToString(&buf, 이름)`** → "폴더\이름".
     - **`+0x244 LdrpResolveDllName(&buf, A, D, C, 0)`** — 파일 확인은 이 함수다 (아래).
  5. **결과 처리**:
     - `+0x256 JNS +0x282` — **0 이상이면 멈추고 나감** (`0x4000000e` 같은 경고값도 성공 취급). 나가기 전 `+0x2b7`·`+0x2bb` 에서 `[R13] = 이 목록 머리`, `[R13+8] = 찾은 칸 다음 위치` 를 적어 둔다 (다시 부르면 이어서 — sast/21 의 `0x4000000e` 재시도).
     - 음수면 `+0x258 CMP EAX,0xc0000135 / +0x25d JNZ +0x4c9`: **`0xc0000135`**, `+0x4c9 0xc0000022`(접근 거부), `+0x4d4 0xc0000043`(공유 위반), `+0x4df 0xc000006d`(로그온 실패) **이 네 값이면 다음 칸으로** (`JZ +0x263` → `+0x263` 에서 `A` 버퍼 길이를 0 으로 지우고 `+0x26d JMP +0x1b1`). **그 밖의 오류는 그 자리에서 멈추고 그 오류를 돌려준다** (`+0x4ea JMP +0x282`).
  6. **목록 끝**: `+0x1b9 TEST AX,AX / +0x1bc JZ +0x272`. 패키지 폴더 목록을 막 끝냈고 플래그 `R8B`(`ctx.flags>>3 & 1`)가 0 이면 `P+0x00` 목록으로 이어서 (`+0x277 JZ +0x3b7` → `+0x3c2` → `+0x1b6`). 아니면 `+0x27d MOV EDI,0xc0000135`.
  7. 마무리 `+0x290 ~ +0x2d1` 버퍼 해제·표시, `+0x2e9 ~ +0x340` 로그 "Status: 0x%08lx", `+0x345 MOV EAX,EDI` 로 반환.
- **파일 있는지 확인하는 함수**: `LdrpResolveDllName` 이 맞다. 안쪽 (플래그 `0x200` 꺼짐 → `+0x92 TEST … 0x200 / +0x9d JZ +0x292`):
  - `+0x298 LdrpGetFullPath` (→ `+0x4e RtlGetFullPathName_Ustr`): 상대 후보(`.\이름`)를 **전체 DOS 경로**로 (현재 디렉터리 기준).
  - `+0x13b LdrpGetNtPathFromDosPath` (`LdrpGetNtPathFromDosPath.asm`): `+0x6c RtlpDosPathNameToRelativeNtPathName` 로 NT 경로를 만들고 **`+0xee NtQueryAttributesFile`** (ret `+0xf3`) — **파일 있는지 보는 시스템 호출은 여기.**
  - 실패 코드 `0xc000000f/0xc0000013/0xc0000033/0xc0000034/0xc0000039/0xc000003a/0xc000003b`(+`0x80000010`, `0xc00000a3`) 이면 `LdrpResolveDllName +0x23b ~ +0x290` 이 **`0xc0000135` 로 바꾼다** (`+0x28b MOV EBX,0xc0000135`). 그래서 `LdrpSearchPath` 가 "없음" 을 한 가지 값으로 받는다.
- **검색을 건너뛰는 갈래**: `LdrpSearchPath` **안에는 이름에 `\` 가 있는지 보는 줄이 없다** (`0x5c` 비교는 폴더 끝에 `\` 붙이는 `+0x202` 뿐). 건너뛰기는 호출하는 쪽 몫: 이름에 경로가 있으면 flags `0x400`(sast/18)이 서고 `LdrpProcessWork` 가 "전체 경로" 길(`LdrpMapDllFullPath`)로 간다 (sast/19 "올리기 길 고르기" 4번). 즉 **`LdrpSearchPath` 는 "맨 이름" 일 때만 불린다** `[정적: 앞선 결과]`.
- `LdrpFindLoadedDll*` 쪽 caller (`LdrpFindLoadedDllInternal +0x226`, `LdrpFindLoadedDll +0x2f5`) 도 같은 함수를 부르는 코드가 있다 (이미 올라온 모듈 찾기 — 범위 밖, 한 줄).

### 6. dll1·dll2 에 적용하면

- **답: 둘 다 목록의 첫 번째 칸 = exe 폴더에서 찾힌다** `[정적: 코드 흐름] + [소스: 한 폴더]` — 실제로 그런지는 `[동적]`.
  - 왜 첫 칸인가: 기본 목록의 맨 앞은 SafeDllSearchMode 가 0 이든 1 이든 **exe 폴더**다 (표 줄 0·1 모두 첫 코드가 5, 줄 3 도 5). `[소스]` `sample/build.bat` 가 dll2·dll1·test.exe 를 모두 `bin\` 한 폴더에 만든다. 그러면 첫 후보 `<bin>\dll1.dll` 에서 `+0x244 LdrpResolveDllName` → `NtQueryAttributesFile` 이 성공 → `+0x256 JNS +0x282` 로 나간다. `+0x244` 는 이 DLL 하나당 **한 번**.
  - 줄 3(SetDllDirectory)·줄 1(SafeDll)·줄 0 이 어느 쪽이든 결론 같음. 순서가 갈리는 건 **둘째 칸부터**.
- **어느 경로 구조체를 쓰나** (`LdrpMapDllSearchPath`, sast/21 과 이어서):
  - `LdrpMapDllSearchPath +0xb0 ~ +0xdb`: 부모 항목(`ctx+0x30`)의 `DependentLoadFlags(+0x118)` 가 마스크 `0x7b00`(`LdrpPolicyBits&4` 면 `0x7f00`) 와 겹치면 `+0x330 LdrpInitializeDllPath(부모 FullDllName.Buffer, (플래그&마스크)|1, &새 P)` (칸 `P+0x18 = 플래그`, `P+0x20 = 부모 경로`, `P+0x00 = 0` — 갈래 A) 로 **새 P 를 만들고** `R14 = 새 P`. 겹치지 않으면 `+0xe1 MOV R14,[RBX+0x10]` = **ctx 가 들고 온 P**.
  - dll1 의 부모는 test.exe 항목(`LdrpImageEntry`), dll2 의 부모는 dll1 항목. `build.bat` 에 `/DEPENDENTLOADFLAG` 가 없어서 둘 다 `+0x118 = 0` 일 것 `[추측: 링크 옵션 근거]` (이 값을 항목에 넣는 줄은 이번에 못 찾음, 공백 4). 그러면 `+0xdb JNZ` 안 타고 **`ctx+0x10` 그대로**.
  - **dll2 의 문맥도 exe 문맥 `+0x10` 을 그대로 물려받나: 예.** `LdrpLoadDependentModuleInternal +0x959 MOV RAX,[RAX+0x10]` (부모 문맥 `+0x10`) → `+0x95d [RSP+0xf0]` → `+0x10f8 MOV [RBX+0x10],RAX` (새 문맥 `+0x10`) `[정적]` (`[RSP+0xf8]` 가 부모 문맥이라는 건 sast/18 근거). exe 문맥 `+0x10` = `&P` (`LdrpInitializeProcess +0x1320` 근처), dll1 문맥 `+0x10` = 같은 `&P`, dll2 문맥 `+0x10` = 같은 `&P`. → **세 모듈이 한 `P` 를 같이 쓴다.** 부모의 `DependentLoadFlags` 가 0 이 아니면 그때만 갈라진다.
  - 그래서 **목록 문자열은 한 번만 만들어진다**: 맨 처음 `LdrpSearchPath` 에서 `P+0` 이 채워지면 이후 모두 `+0xe4` 에서 건너뛴다. 첫 계산이 정확히 어느 호출(dll1 을 찾을 때인지, `LdrpFindLoadedDll*` 가 먼저 부르는지)인지는 `[동적]`.
- 인자 확인: `LdrpMapDllSearchPath +0x116 MOV RCX,RBX` (ctx; **`ctx+0` 이 DLL 이름 UNICODE_STRING**), `+0x10e MOV RDX,R14` (P), `+0xe5 ~ +0x105 R8B = (ctx+0x20 >> 3) & 1`, `+0x12c CALL LdrpSearchPath`.

## 동적분석으로 확인해야 할 것 [dast 요청 필요]

1. **목록 문자열 실제 값 + 몇 번째 칸**: 첫 계산 때 문자열 (`LdrpComputeLazyDllPath +0x98` 에서 `du @rcx`). 기대: `<exe 폴더>;<시스템 폴더 덩어리>;.;<PATH>;`.
2. **줄 번호 (SafeDllSearchMode)**: `RtlpComputeDllPath +0x5e` (`ntdll+0xba5be`) 의 `EBX` (기대 1), 또는 `RtlpComputePath` 입구 `@rcx` 가 `ntdll+0x17e354`, `dd @rcx L5` = `5 2 4 3 8`.
3. **`P` 가 정말 0 으로 시작하나**: `LdrpInitializeProcess +0x1318` 직후 `dq @rsp+0x380 L10` 이 전부 0 (특히 `+0x00`, `+0x18`, `+0x20`).
4. **dll1·dll2 가 첫 칸에서 찾히나**: `LdrpSearchPath +0x244` 호출마다 후보 (`dS @rcx`), `+0x249` 직후 `eax`. 기대: dll1·dll2 둘 다 한 번에 0.
5. **dll1·dll2 의 부모 `DependentLoadFlags`**: `LdrpMapDllSearchPath +0xd8` 직전 `[부모+0x118]` (sast/21 과 같은 요청, 0 이면 `P` 공유).
6. **첫 계산을 일으키는 호출**: `LdrpComputeLazyDllPath` 입구 `k` (caller 가 `LdrpSearchPath +0x372`, 그 윗단이 `LdrpMapDllSearchPath` 인지 `LdrpFindLoadedDll*` 인지) + `LdrpSearchPath` 입구 `dS @rcx` (어떤 DLL 이름).
7. `RtlpSystemDirs` 실제 문자열 (`dS ntdll+0x1d2360`), 시스템 폴더 대체 모드가 꺼져 있나 (`LdrSystemDllInitBlock+0xa0` 비트 60~61).

추천 멈춤점 (`ntdll+rva`, rva 는 함수 시작 + 오프셋을 더한 값. 커널 멈춤점은 모든 프로세스에 걸리므로 test.exe 조건을 붙일 것):

| 함수 + 오프셋 | `ntdll+rva` | 볼 것 |
|---|---|---|
| `LdrpInitializeProcess +0x1318` (ret 자리) | `ntdll+0x8d71c` | `dq @rsp+0x380 L10` = `P` (전부 0) |
| `LdrpInitializeDllPath` 입구 | `ntdll+0x899a0` | `rcx`, `rdx`, `r8` = (0, 0, `&P`) |
| `LdrpSearchPath` 입구 | `ntdll+0x8eb0` | `dS @rcx` (이름), `dq @rdx L3` (`P+0/+8/+0x10`), `r8`(bit), `dq @r9 L2` (이어하기 표시) |
| `LdrpComputeLazyDllPath` 입구 | `ntdll+0xb9870` | `k` (누가 처음 불렀나) |
| `LdrpComputeLazyDllPath +0x98` | `ntdll+0xb9908` | `du @rcx` = **만들어진 폴더 목록** |
| `RtlpComputePath` 입구 | `ntdll+0xba7a0` | `dd @rcx L5`(순서 표 줄), `edx`=5, `r8`=0 |
| `RtlpGetDirPath +0x6b` | `ntdll+0xb96a3` | `rcx`=폴더 길이(바이트), `rax`=exe 경로 시작 → `du @rax L(@rcx/2)` |
| `LdrpSearchPath +0x244` (call) | `ntdll+0x90f4` | `dS @rcx` = 이번 후보 "폴더\이름" |
| `LdrpSearchPath +0x249` (ret) | `ntdll+0x90f9` | `r eax` (0 이면 찾음, `c0000135` 면 다음 칸) |
| `LdrpSearchPath +0x282` | `ntdll+0x9132` | 나가는 자리, `edi` = 최종 상태 |
| `LdrpGetNtPathFromDosPath +0xf3` (ret) | `ntdll+0xf0d3` | `eax` = `NtQueryAttributesFile` 상태 |
| 전역 | `ntdll+0x1d3050` | `RtlpDllSearchPath` 캐시: `du poi(ntdll+0x1d3050)+0x80` |

## 새로 생긴 정적 공백

1. **`RtlpComputeDllPathWithOptions`** (플래그 갈래, `0xba650`, 315바이트): 이름과 호출만 확인, 안쪽 안 읽음. test.exe 는 안 탈 것 `[추측]` (`P+0x18 = 0`). `DependentLoadFlags` 가 있는 DLL 이 나오면 필요.
2. **`LdrSystemDllInitBlock+0xa0` 비트 60~61** 의 뜻과 보통 값 (`RtlpComputePath +0x21`). 1 이면 목록이 시스템 폴더 하나로 바뀐다. PDB 에 타입 없음 (ntdll PDB 에 `_PS_SYSTEM_DLL_INIT_BLOCK` 없음, 커널 쪽 타입).
3. **`P+0x28 ~ +0x77` 사본을 읽는 곳** — `LdrpSaveLocationsSearched`(보안 ETW) 쪽일 것 `[추측]`, 안 봄.
4. **항목 `+0x118 DependentLoadFlags` 를 쓰는 줄** (`LdrpCfgProcessLoadConfig` 근처일 것 `[추측]`): 못 찾음. sample 이 0 이라는 건 링크 옵션 근거뿐.
5. **`<윈도우 폴더>`(`[RSP+0x120]`) 의 정확한 출처 줄**, `ProcessParameters.Flags & 0x20000000` 이 보통 꺼져 있는지.
6. **현재 폴더 칸(`.`)의 풀이**: `.\dll1.dll` 이 어느 현재 디렉터리로 풀리는지는 `LdrpGetFullPath → RtlGetFullPathName_Ustr` 가 PEB `CurrentDirectory` 를 쓰는 것으로 보이나 안쪽은 안 읽음 (sast/14 "현재 폴더 초기화" 와 이어서). dll1·dll2 는 첫 칸에서 끝나서 이번 sample 에선 영향 없음.

## 자체 검증

- [x] 질문 1~6 모두 답 (정적으로 못 정한 것은 동적으로 넘김).
- [x] 답마다 `.asm` 오프셋 (`+0x..`), 점프 목적지는 "주소 − 함수 시작".
- [x] 버전 대조 (알려진 사실 2건 일치) 적음.
- [x] `[정적]` 과 실행 여부 구분 ("읽는 코드가 있다", "기대", `[동적]`).
- [x] 디컴파일 코드는 붙이지 않음 (경로 + 요약만).
