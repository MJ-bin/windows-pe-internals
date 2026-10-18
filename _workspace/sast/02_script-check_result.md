# 02 script-check — 정적 분석 결과 (도구 시험, 재시도 2회째)

- 요청서: `_workspace/sast/02_script-check.md` (재시도: sast/01 → 02 첫 시도 → 이번)
- 날짜: 2026-10-19 (KST 01:02 ~ 01:06)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), ntoskrnl 은 백그라운드 분석 중 (아래)
- 뽑은 함수: 요청서 대상 6개 (정규식 `^(LdrInitializeThunk|LdrpInitializeInternal|LdrpInitialize|LdrpInitializeProcess|LdrpInitializeNode|LdrpLoadDll)$`, depth 0) → `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`
  - 불일치 원인을 가리느라 **요청서 밖으로 몇 개 더 뽑았다** (아래 "더 뽑은 것"). 전부 지우지 않고 남김.
- 버전 확인: 알려진 사실 8줄 중 **오프셋 8/8 일치** (전부 `ret_off` 와 같음). 이름 3곳은 요청서/SKILL.md 표와 다르다 (아래).
- 판정: **분석성공** (스크립트 4개 모두 돌고, PDB 붙고, 오프셋이 디버거 관찰과 맞음). 단 스크립트 개선 제안 3건이 있다.
- 요청서 규칙 지킴: `.claude/` 아래는 한 글자도 안 고쳤고 임시 사본도 안 만들었다. 대기는 PID/포그라운드로만 했다 (`pgrep -f` 안 씀).

## 스크립트 문제 (맨 위)

**막히는 문제는 없다.** 아래 3건은 "돌긴 도는데 결과를 오해하게 만들 수 있는 것"이다. 고치는 건 사용자 몫이고, 안 고쳐도 지금 시험은 통과다.

### (가) `JMP` 꼬리호출이 `CALL ret=…` 로 찍힌다

- 증상: `ms/disassemble/ntdll/LdrpInitialize.asm` `+0x3c  JMP 0x18008be88    ; CALL ret=+0x41 -> LdrpInitializeInternal`, `_calls.tsv` 에 `LdrpInitialize +0x3c +0x41 LdrpInitializeInternal`.
- 실제: `+0x3c` 는 `POP RDI` 직후의 **`JMP`** (스택을 정리하고 뛰는 꼬리호출)이다. 돌아올 주소를 push 하지 않으니 `ret=+0x41` 은 콜스택에 나올 수 없다 (`+0x41` 은 그냥 다음 코드 블록 시작).
- 위치: `.claude/skills/ghidra-headless-export/scripts/ExportFunctions.java` 163~167줄 (`ins.getFlowType().isCall()` 하나로 CALL/JMP 를 구분 안 함. Ghidra 가 꼬리호출 JMP 에 CALL_RETURN 오버라이드를 달아 둔 듯 `[추정]`).
- 제안: 163줄 근처에서 `ins.getMnemonicString()` 이 `JMP` 면 `; TAILJMP -> 이름` 으로 적고 `_calls.tsv` 의 `ret_off` 칸에 `tail` 을 쓴다.

### (나) `INT3` / `INT 0x29` / `INT 0x2e` 가 `indirect:?` 호출로 찍힌다

- 증상: `_calls.tsv` 의 `indirect:?` 10줄 중 **8줄이 INT 계열** (INT3 4, INT 0x29 3, INT 0x2e 1). 진짜 간접 호출(`CALL RAX`)은 2줄뿐 (`LdrpInitializeProcess +0x17fc`, `_LdrpInitialize +0x9a`).
  - 예: `LdrInitializeThunk.asm` `+0x1f  INT3    ; CALL ret=+0x20 -> indirect:?`
- 원인 추정: Ghidra 가 INT 를 "호출로 끝나는 흐름(CALL_TERMINATOR)" 으로 분류해서 `isCall()` 이 true. 위치는 (가)와 같은 163줄.
- 제안: 같은 자리에서 니모닉이 `INT` 로 시작하면 건너뛰거나 `trap:INT3` 처럼 구분. `indirect:` 는 진짜 간접 호출에만.

### (다) `indirect:<포인터이름>` 형식은 이번에 확인 못 함

- 뽑은 함수 중에 `call [rip+포인터]` 형태가 없었다. `LdrpCallInitRoutine` 도 `LdrpCallInitRoutineInternal` 을 직접 부르고, 그 안에서 `_guard_dispatch_icall$thunk$…` 함수를 직접 부른다 (그냥 함수 이름으로 찍힘).
- 그래서 `reference.md` 예시 `indirect:__guard_dispatch_icall_fptr` 는 **이 빌드의 이 두 함수에서는 나오지 않는다.** 스크립트 오류라는 증거는 없고, 이 형식을 쓰는 다른 함수가 있어야 확인된다.

### 기타 (스크립트 아님)

- `reference.md` 의 예시 숫자 `LdrpInitializeProcess entry=18002c404 rva=0x2c404 size 7812` 는 이 빌드와 다르다. 실제는 `entry=18008c404 rva=0x8c404 size=7750`. 예시일 뿐이지만 헷갈리니 고치면 좋다.
- `SKILL.md` "알려진 사실" 표와 요청서의 **함수 이름** 3곳이 어긋난다 (오프셋은 맞다). 아래 "질문 3" 참고.
- `ms/ghidra/ntdll.export.log` 는 export 할 때마다 덮어써진다 (마지막 실행 것만 남음). 각 실행의 stdout 은 `ntdll.export{,2,3,4}.stdout` 으로 따로 남겼다.

## 질문별 답

### 질문 1. 스크립트 4개가 에러 없이 도는가? → 예, 4개 모두

| 스크립트 | 결과 | 근거 |
|---|---|---|
| `run_ghidra.sh` | 예 | analyze·export 모두 종료코드 0, 아래 로그 grep 에서 스크립트 에러 0건. 실패 경로는 이번에 안 시험함 (01 에서 본 "스크립트가 죽어도 0" 문제는 그대로일 것) |
| `SetPdb.java` | 예 | ntoskrnl `ms/ghidra/ntoskrnl.analyze.log` 179줄 `SetPdb: ntoskrnl.exe <- .../ms/sym/ntoskrnl.exe.pdb`, ntdll 로그 53줄 `SetPdb: ntdll.dll <- ...`. 이어서 `PDB analyzer parsing file` 줄 있음 |
| `ExportIndex.java` | 예 | ntdll 로그 145줄 `ExportIndex: 4593 functions -> .../ms/decomp/ntdll` (01:03:24) |
| `ExportFunctions.java` | 예 | export 4번 모두 `이름으로 고른 함수 N개` → `완료`. depth 0 (6개 / 2개 / 1개), depth 1 (`LdrInitializeThunk` → 4개) |

- JDK 21.0.12.1 에서 JDK 25 경고(`sun.misc.Unsafe`) 없음.
- 처음 요청한 확인: 새 캐시 `~/.config/ghidra/ghidra_11.3.2_PUBLIC/osgi/compiled-bundles/d5ff6acd/META-INF/MANIFEST.MF` → `Created-By: 21.0.12.1 (Ubuntu)`, `Require-Capability: osgi.ee;filter:="(&(osgi.ee=JavaSE)(version=21))"`, `SetPdb.class` 앞 8바이트 `cafe babe 0000 0041` (0x41=65=Java 21). 지난번의 `UNKNOWN` 은 사라졌다.
- ntoskrnl 띄운 직후(01:02:16) SetPdb 성공 줄이 있고 `osgi.ee` 에러 없음을 먼저 확인한 뒤에 ntdll 로 넘어갔다.

### 질문 2. PDB 가 붙었는가? → 예

- `ms/decomp/ntdll/_index.tsv`: 함수 **4593개**, 이름이 `FUN_` 으로 시작하는 것 **0개** (`thunk_FUN_` 도 0개). thunk 표시 101개.
- 이름이 있는 것을 확인: `LdrpInitializeProcess`(0x8c404), `LdrpInitializeGraphRecurse`(0x87d60), `LdrpInitializeKernel32Functions`(0xb8024), `LdrpCallTlsInitializers`(0x10e50), `LdrpCallInitRoutine`(0x10ba0), `LdrpLoadDllInternal`(0x1fc30), `_LdrpInitialize`(0x8bf8c) 등 모두 있음.
- 커널 PDB GUID 는 요청서가 확인해 준 값을 믿음 (`C29EBFB06B78B3C020DCA66D99713F9E1`).

### 질문 3. 알려진 사실 표와 `_calls.tsv` 가 맞는가? → 오프셋은 8/8 일치

관찰 값은 **전부 `ret_off` 와 같다** (call 위치와 같은 줄은 없음).

| caller (기준 함수) | 관찰 | call_off | ret_off | 정적 callee | 관찰이 맞는 쪽 | 이름 비고 |
|---|---|---|---|---|---|---|
| `LdrInitializeThunk` | `+0xe` | `+0x9` | `+0xe` | `LdrpInitialize` | ret | 아래 "이름 어긋남" (꼬리호출 때문에 스택에서는 `LdrpInitializeInternal` 로 보임) |
| `LdrpInitializeInternal` | `+0x5a` | `+0x55` | `+0x5a` | **`_LdrpInitialize`** | ret | 요청서의 `LdrpInitialize` 가 아니라 앞에 `_` 붙은 함수 |
| `_LdrpInitialize` (요청서: `LdrpInitialize`) | `+0x16c` | `+0x167` | `+0x16c` | `LdrpInitializeProcess` | ret | `LdrpInitialize` 는 크기가 99바이트라 `+0x16c` 가 아예 없음 |
| `LdrpInitializeProcess` | `+0x1bbb` / `+0x1bb6` | `+0x1bb6` | `+0x1bbb` | `LdrpInitializeGraphRecurse` | `+0x1bbb`=ret, `+0x1bb6`=call (BEFORE-GRAPH 멈춤점이 call 위치라는 설명과 일치) | 이 함수 안에 같은 callee 호출이 3곳: `+0x16e6`/`+0x16eb`, `+0x193c`/`+0x1941`, `+0x1bb6`/`+0x1bbb` |
| `LdrpInitializeProcess` | `+0x19a2` | `+0x199d` | `+0x19a2` | `LdrpInitializeKernel32Functions` | **ret** | |
| `LdrpInitializeProcess` | `+0x1c99` | `+0x1c94` | `+0x1c99` | `LdrpCallTlsInitializers` | **ret** | |
| `LdrpInitializeNode` | `+0x16b` | `+0x166` | `+0x16b` | `LdrpCallTlsInitializers` | ret | |
| `LdrpInitializeNode` | `+0x19c` | `+0x197` | `+0x19c` | `LdrpCallInitRoutine` | ret | |
| `LdrpLoadDll` | `+0x100` | `+0xfb` | `+0x100` | `LdrpLoadDllInternal` | ret | |

- **이름 어긋남 (오프셋이 아니라 이름 문제):**
  - 실제 사슬은 `LdrInitializeThunk`(+0xe 로 복귀) → `LdrpInitialize` → (**`JMP` 꼬리호출**, `LdrpInitialize+0x3c`) → `LdrpInitializeInternal`(+0x5a 로 복귀) → **`_LdrpInitialize`**(+0x16c 로 복귀) → `LdrpInitializeProcess`.
  - `LdrpInitialize` 는 꼬리호출로 `LdrpInitializeInternal` 로 뛰고 자기 프레임을 버리므로, 콜스택에는 `LdrpInitialize` 가 안 보이고 `LdrInitializeThunk+0xe` 의 바로 위가 `LdrpInitializeInternal` 로 보인다. 관찰 첫 줄이 맞는 이유.
  - 그러니 두 번째 줄은 callee 가 `_LdrpInitialize`, 세 번째 줄은 caller 가 `_LdrpInitialize` 로 고쳐 적어야 한다. `SKILL.md` 알려진 사실 표와 01_sprint `flow.md` 라벨 확인을 제안 (숫자는 그대로).
- **손계산 (질문 5):** 함수 시작 주소 + 오프셋 = 명령 주소 가 `.asm` 의 주소 열과 같다.
  - `LdrpInitializeProcess` 0x18008c404 + 0x1bb6 = 0x18008dfba → `.asm` `+0x1bb6  18008dfba  CALL 0x180087d60`, 대상 0x180087d60 = `_index.tsv` 의 `LdrpInitializeGraphRecurse` 시작 주소. call 길이 5바이트라 ret = +0x1bbb ✓.
  - `LdrpInitializeProcess` + 0x199d = 0x18008dda1 ✓, + 0x1c94 = 0x18008e098 ✓.
  - `LdrpInitializeNode` 0x18000e5d0 + 0x166 = 0x18000e736 ✓, `LdrpLoadDll` 0x18001f9c0 + 0xfb = 0x18001fabb ✓, `LdrpInitializeInternal` 0x18008be88 + 0x55 = 0x18008bedd ✓, `_LdrpInitialize` 0x18008bf8c + 0x167 = 0x18008c0f3 ✓ (대상 0x18008c404 = `LdrpInitializeProcess`).
  - 불일치 원인: 숫자는 모두 맞으니 스크립트 오프셋 계산도 바이너리도 문제 없음. 어긋난 건 이름 표기뿐이고, 원인은 우리 문서의 이름 (스크립트 아님).

### 질문 4. export 결과물 형식이 reference.md 와 같은가? → 예 (표시 오해 3건 제외)

- `.c` 머리 주석: `// ntdll!이름  entry=…  rva=0x…`, `// callers: …`, `// callees: …` 3줄 ✓ (진입점 `LdrInitializeThunk` 은 callers 가 비어 있음)
- `.asm`: `; ntdll!이름  entry=…  rva=…` 1줄 + `+0x… 주소 명령` ✓, call 줄 끝에 `; CALL ret=+0x… -> 이름` ✓. 음수 오프셋(`-0x…`)은 이번엔 안 나옴.
- `_calls.tsv`: 241줄, 전부 4열 (`awk NF!=4` 0줄), `sort -c` 통과 (정렬·중복 없음) ✓
- `_index.tsv`: 머리 `# name\tentry\trva\tsize\tthunk`, 모든 행 5열 ✓. size 는 함수 몸통의 주소 개수.
- `indirect:` : `indirect:?` 만 확인 (10줄. 위 (나)). 이름이 붙는 `indirect:<포인터>` 는 이번에 못 봄 (위 (다)).
- 같은 이름 함수 파일 이름 규칙(`이름__rva.c`): 이번 대상엔 중복 이름이 없어서 확인 못 함.

### 질문 5. 불일치 원인 → 위 "질문 3" 손계산 참고

- 오프셋 불일치 없음. 이름 어긋남은 문서 쪽 (스크립트도 바이너리도 아님).
- 오해 소지: (가) JMP 가 CALL 로 표시, (나) INT 가 간접 호출로 표시.

### 질문 6. ntoskrnl analyze 가 살아 있는가? → 예

- PID **847212** (세션 리더 `run_ghidra.sh analyze ntoskrnl.exe`, 01:02:10 시작). 자식 `launch.sh` 847223, java 847314 (`/usr/lib/jvm/java-21-openjdk-amd64/bin/java`).
- 01:05:45 기준 3분 36초째, java RSS 약 3.8GB, CPU 50~170%.
- 로그: `/home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/ghidra/ntoskrnl.analyze.log`, stdout: `ms/ghidra/ntoskrnl.analyze.stdout`
- 확인한 것: `SetPdb: ntoskrnl.exe <- .../ms/sym/ntoskrnl.exe.pdb` 성공 줄 있음 (179줄), `PDB analyzer parsing file` 줄 있음, `SCRIPT ERROR` 0건, `osgi.ee` 0건, `Skipping PDB processing` 없음. PDB 구조체 정렬 `WARN`(`CppCompositeType ... failed to align`) 다수는 PDB 적용 중에 나오는 보통 경고.
- 끝나면 `ms/decomp/ntoskrnl/_index.tsv` 가 생기고 로그 끝에 `ExportIndex: N functions` 가 찍힌다. 끝난 걸 알려면 `ps -p 847212` 또는 `ls ms/decomp/ntoskrnl/_index.tsv`. (`pgrep -f` 로 기다리지 말 것.)
- 주의: 이 프로세스가 도는 동안 `ms/ghidra/ntoskrnl.lock` 이 있다. 같은 모듈 export 는 끝난 뒤에.

### 질문 7. 로그에 `SCRIPT ERROR`·`Exception` 이 없는가? (직접 grep)

| 파일 | SCRIPT ERROR | osgi.ee | Exception |
|---|---|---|---|
| `ms/ghidra/ntoskrnl.analyze.log` / `.stdout` | 0 / 0 | 0 / 0 | 0 / 0 |
| `ms/ghidra/ntdll.analyze.log` | 0 | 0 | 2 (아래) |
| `ms/ghidra/ntdll.analyze.stdout` | 0 | 1 | 5 (아래) |
| `ms/ghidra/ntdll.export.log` (마지막 실행) | 0 | 0 | 0 |
| `ms/ghidra/ntdll.export{,2,3,4}.stdout` | 0 | 0 | 0 |

- ntdll analyze 의 `Exception`/`osgi.ee` 는 **우리 스크립트가 아니다.** `ntdll.analyze.log` 95줄 (01:03:15) `WARN (java file: WindowsResourceReference) GhidraScriptLoadException ...` 과 stdout 의 `BundleException: Unable to resolve 93cb37dd ... osgi.ee`.
  - 이는 Ghidra 가 분석기 단계에서 쓰는 **기본 스크립트 `WindowsResourceReference`** 가 JDK 25 로 만든 옛 캐시 `93cb37dd` (`Created-By: 25.0.2`, `UNKNOWN`) 에 걸려 못 올라온 것. 요청대로 `93cb37dd` 는 손대지 않았다.
  - 영향: ntdll analyze 는 `Import succeeded` / `Post-analysis succeeded` 로 정상 종료. 이 기본 스크립트가 하는 일(윈도우 리소스 참조 표시)이 빠졌을 수 있으나 우리 질문(함수 이름, 호출 지점)에는 영향 없음 `[추정]`.
  - ntoskrnl 쪽도 같은 단계에 도달하면 같은 WARN 이 나올 수 있다.
  - 제안: 시간이 날 때 `~/.config/ghidra/ghidra_11.3.2_PUBLIC/osgi/compiled-bundles/93cb37dd` 도 지우면 사라진다 (지금 돌고 있는 ntoskrnl 이 끝난 뒤에).
- 그 밖의 `Exception` 글자: `Windows x86 PE Exception Handling` (분석기 이름, 에러 아님).

## 더 뽑은 것 (요청서 밖, 불일치 원인을 가리느라)

| 정규식 | depth | 이유 |
|---|---|---|
| `^(_LdrpInitialize\|LdrpCallInitRoutine)$` | 0 | 질문 3 의 `+0x16c` 이름 확인 (`_LdrpInitialize`), `indirect:` 형식 확인 (`LdrpCallInitRoutine`) |
| `^LdrInitializeThunk$` | 1 | 요청서가 허락한 depth 1 시험. 대상 4개 (자신 + `LdrpInitialize` + `NtContinue` + `RtlRaiseStatus`) |
| `^LdrpCallInitRoutineInternal$` | 0 | `indirect:<포인터>` 형식 확인 시도 (결국 직접 호출이라 못 봄) |

- 현재 `ms/decomp/ntdll/`: `_index.tsv` + `.c` 11개, `ms/disassemble/ntdll/`: `_calls.tsv` + `.asm` 11개.
  - 요청서 6개 + `_LdrpInitialize`, `LdrpCallInitRoutine`, `LdrpCallInitRoutineInternal`, `NtContinue`, `RtlRaiseStatus`. 지우지 않음 (Q8·Q9 에서 다시 쓰임).

## 동적분석으로 확인해야 할 것

- 도구 시험이라 없음. 참고로, 정적으로 말할 수 없는 것 하나:
  - `LdrpInitializeProcess +0x17fc  CALL RAX` (간접 호출, 대상 모름) → 필요해지면 그때 요청.

## 이전 시도 (캐시 문제)

- 01 (JDK 25) 과 02 첫 시도 (JDK 21 로 바꿨는데도) 모두 `SetPdb`·`ExportIndex`·`ExportFunctions` 가 `osgi.ee=UNKNOWN` 으로 로딩 단계에서 죽었다.
- 원인: Ghidra 번들 도구(bnd 7.0.0)가 JDK 25 클래스(버전 69)를 몰라서 `Require-Capability: osgi.ee=UNKNOWN` 을 매니페스트에 적었고, 그 결과가 `~/.config/ghidra/ghidra_11.3.2_PUBLIC/osgi/compiled-bundles/d5ff6acd` 에 캐시됨. 소스가 캐시보다 오래돼서 JDK 를 21 로 바꿔도 재컴파일이 안 됐다 `[추정, 시각 비교로 본 정황]`.
- 해결: 사용자가 JDK 21 설치 + `JAVA_HOME_OVERRIDE` 지정 + 그 캐시 폴더 삭제 → 이번에 새 캐시가 `Created-By: 21.0.12.1` 로 생성되어 통과.
- 당시 로그: `ms/ghidra/*.jdk25.*`, `ms/ghidra/ntoskrnl.analyze.jdk21-stale.*` (지우지 말 것). 교훈: "JDK 를 바꾸면 `osgi/compiled-bundles` 를 지운다"를 하네스 문서에 넣을 만하다. 또 `pgrep -f "analyze ntdll.dll"` 로 기다리면 그 명령 자신이 매치돼서 끝나지 않는다 (PID 로 기다릴 것).

## 판정

**분석성공** — Ghidra 21 에서 스크립트 4개가 모두 돌았고, PDB 가 붙었고(4593개 중 `FUN_` 0), `_calls.tsv` 오프셋이 디버거 관찰과 8/8 일치(전부 ret 위치)하며 손계산으로도 확인됐다. 이름 3곳(`_LdrpInitialize`, 꼬리호출)은 우리 문서 쪽 수정거리이고, 스크립트는 JMP·INT 표시 개선 제안 2건이 있다. ntoskrnl analyze(PID 847212)는 PDB 가 붙은 채 도는 중이다.
