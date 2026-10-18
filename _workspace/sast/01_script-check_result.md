# 01 script-check — 정적 분석 결과 (도구 시험)

- 요청서: `_workspace/sast/01_script-check.md`
- 날짜: 2026-10-19 (KST 00:23 ~ 00:35)
- 판정: **실패** (스크립트 4개 중 셸 래퍼만 돈다. Java 스크립트 3개는 전부 "로딩 단계"에서 죽는다)
- 요청서 규칙 지킴: `.claude/` 아래 파일은 하나도 안 고쳤고, 임시 사본도 안 만들었다.
- 버전 확인(알려진 사실 대조): **못 함** (export 결과물이 0개라 대조할 `_calls.tsv` 자체가 없다)

## 스크립트 문제 (맨 위)

> **정정 (02 결과서에서 확인):** 아래 "원인 추정"의 "Felix 가 Java 25 를 모른다" 는 정확하지 않았다. 실제로는 JDK 25 로 컴파일할 때 Ghidra 의 bnd 7.0.0 이 클래스 버전 69 를 몰라서 매니페스트에 `osgi.ee=UNKNOWN` 을 적은 것이다. 그래서 **JDK 만 21 로 바꿔서는 안 되고, 옛 컴파일 캐시(`~/.config/ghidra/ghidra_11.3.2_PUBLIC/osgi/compiled-bundles/d5ff6acd`)를 지워야 한다.** 자세한 건 `02_script-check_result.md`.

한 줄 요약: **JDK 25 에서는 Ghidra 11.3.2 가 `.java` 스크립트를 못 올린다.** 스크립트 코드 탓이 아니다.

### 증상

세 스크립트가 모두 똑같은 에러로 죽는다. 줄 번호는 없다 (스크립트 1줄도 실행되기 전, 로딩 단계에서 죽기 때문).

| 스크립트 | 어디서 | 결과 |
|---|---|---|
| `run_ghidra.sh` | `.claude/skills/ghidra-headless-export/scripts/run_ghidra.sh` | 아니오(부분). Ghidra를 띄우고 프로젝트 만들고 import 하는 것까지는 된다. 단 스크립트가 다 죽어도 **종료코드 0 + "[+] 끝" 출력** (아래 "래퍼가 실패를 못 알아챔" 참고) |
| `SetPdb.java` | analyze 의 `-preScript` | **죽음** (ntoskrnl 00:23:50, ntdll 00:24:27) |
| `ExportIndex.java` | analyze 의 `-postScript` | **죽음** (ntdll 00:24:58) |
| `ExportFunctions.java` | export 의 `-postScript` | **죽음** (ntdll export 00:34:41) |

### 에러 메시지 (로그에서 핵심만)

```
ERROR (HeadlessAnalyzer) REPORT SCRIPT ERROR: SetPdb.java : The class could not be found.
  It must be the public class of the .java file: SetPdb
  ghidra.app.script.GhidraScriptLoadException ...
    at ghidra.app.script.JavaScriptProvider.getScriptInstance(JavaScriptProvider.java:110)
Caused by: java.lang.ClassNotFoundException: SetPdb
    at org.apache.felix.framework.Felix.loadBundleClass(Felix.java:2102)
Caused by: org.osgi.framework.BundleException: Unable to resolve d5ff6acd [1](R 1.0):
  missing requirement [d5ff6acd [1](R 1.0)] osgi.ee; (osgi.ee=UNKNOWN)
  Unresolved requirements: [[d5ff6acd [1](R 1.0)] osgi.ee; (osgi.ee=UNKNOWN)]
```

- 같은 줄이 `ExportIndex`, `ExportFunctions` 에도 클래스 이름만 바뀌어 나온다.
- 로그 위치 (전체 에러는 여기서 본다):
  - `ms/ghidra/ntoskrnl.analyze.log` 180~207줄 근처 (+ `ntoskrnl.analyze.stdout`)
  - `ms/ghidra/ntdll.analyze.log` 52줄 (SetPdb), 130줄 (ExportIndex)
  - `ms/ghidra/ntdll.export.log` 41줄 (ExportFunctions)
- 같이 찍히는 JDK 25 경고 (단독으로는 해롭지 않음):
  `WARNING: sun.misc.Unsafe::staticFieldOffset has been called by org.apache.felix.framework.util.SecureAction (org.apache.felix.framework-7.0.5.jar)`
- 상관없는 잡음: `GhidraMCP/Module.manifest` "Invalid line encountered" (사용자 `~/.config/ghidra` 쪽 확장. 무시해도 됨)

### 원인 추정

- Ghidra 11.3.2 는 `.java` 스크립트를 OSGi(Felix 7.0.5, 2022년 버전)로 올린다.
- Felix 는 "지금 도는 Java 가 어떤 버전인지"를 목록에서 찾는데, **Java 25 는 목록에 없어서 `osgi.ee=UNKNOWN`** 이 된다. 스크립트 번들이 "실행환경이 필요해요"라고 요구하는데 아무도 못 맞춘다 → 클래스를 못 찾음.
- 근거: `Ghidra/application.properties` 는 `java.min=21`, `java.max=` (비어 있음) 이라 25도 "시작"은 허용한다. 하지만 시작이 된다고 스크립트 로딩까지 된다는 보장은 아니었다. 이번 에러가 그 증거.
- 확인 못 한 것 `[추측]`: 스크립트 소스 자체에 컴파일 문제가 있는지는 모른다. 단, 에러가 "컴파일 실패"가 아니라 "번들 resolve 실패"로 나오는 걸 보면 컴파일까지는 간 것으로 보인다. 소스가 맞는지는 JDK 문제를 푼 뒤에야 알 수 있다.
- 서버에 있는 JDK: `jdk-25.0.2-oracle-x64`, `java-11-openjdk` 뿐이다. 11은 Ghidra 최소(21) 미만이라 못 쓴다.

### 고칠 방법 (제안만, 사용자가 직접)

1. **JDK 21 설치 후 Ghidra가 그걸 쓰게 한다.** (스크립트 수정 없음. 가장 안전)
   - 방법 A: `export JAVA_HOME=/path/to/jdk-21` 를 켜 두고 `run_ghidra.sh` 실행. `launch.sh` 가 PATH/JAVA_HOME 에서 찾는다.
   - 방법 B: `$GHIDRA_HOME/support/launch.properties` 의 `JAVA_HOME_OVERRIDE=/path/to/jdk-21`.
   - (이 서버에 `sudo` 가 없으면 tarball 을 `~/tools/` 에 풀어서 쓰면 된다.)
2. `[추측, 안 해봄]` JDK 25를 계속 쓰고 싶으면 Felix에 실행환경 목록을 알려 주는 방법이 있을 수 있다 (`org.osgi.framework.system.capabilities.extra` 계열). Ghidra가 그 값을 넘겨받는지 확인 못 했고, 이걸 넣으려면 `run_ghidra.sh` 44~46줄의 JVM 옵션을 고쳐야 한다. 추천하지 않는다.
3. **`run_ghidra.sh` 개선 제안** (고쳐도 되고 안 고쳐도 됨):
   - 76~77줄 근처: analyze/export 끝에서 로그에 `SCRIPT ERROR` 가 있으면 비정상 종료(`exit 4`) 하고 메시지 찍기. 지금은 스크립트가 다 죽어도 종료코드 0 + "[+] 끝".
   - 26줄 근처: `java` 메이저 버전이 21이 아니면 경고 (21 로 시험된 적 있는 조합만 믿기).
4. **`SKILL.md` Workflow 3번 문구 제안**: "`.gpr` 이 있으면 건너뜀" 은 위험하다. 아래 "후유증"처럼 **실패한 분석도 `.gpr` 을 남긴다.** `ms/decomp/<모듈>/_index.tsv` 가 있고 `FUN_` 비율이 낮은지까지 봐야 "분석 끝남"으로 친다.

## 후유증 (지금 서버에 남은 것) — 꼭 읽기

- **PDB 없이 만들어진 쓰레기 프로젝트가 둘 있다.**
  - `ms/ghidra/ntdll.gpr` : 분석은 끝났지만 **PDB 없이** (함수 이름이 `FUN_xxx`). `_index.tsv` 없음.
  - `ms/ghidra/ntoskrnl.gpr` : 분석이 **아직 도는 중**, 역시 PDB 없이.
  - 이 상태에서 에이전트가 "`.gpr` 있으니 export 만 하자" 하면 이름이 안 맞아서 헤맨다. **고치고 다시 돌리기 전에 지울 것.**
- **ntoskrnl analyze 는 지금 돌고 있지만 쓸모가 없다.**
  - PID **838669** (세션 리더 bash `run_ghidra.sh`). 자식: `launch.sh` 838681, java 838782.
  - 00:23:43 시작. 00:35 기준 11분째, java RSS 약 3.9GB, CPU 낮음(약 16%).
  - 로그: `ms/ghidra/ntoskrnl.analyze.log` (마지막 줄이 00:24:54 에서 멈춰 있음. 자동 분석 중이라 조용한 것), stdout: `ms/ghidra/ntoskrnl.analyze.stdout`
  - 왜 쓸모없나: ① PDB가 안 붙었다 (`Skipping PDB processing: failed to locate PDB file` 가 로그 207줄). ② 끝나도 `ExportIndex` 가 죽어서 `_index.tsv` 가 안 나온다. 몇 시간 돌려도 결과가 없다.
  - 요청서 지시대로 **안 죽였다.** 오케스트레이터가 정해라. 정리 명령(제안):
    ```bash
    kill -- -838669                         # 세션/그룹째 종료 (안 되면 kill 838782 838681 838669)
    rm -f  ms/ghidra/ntoskrnl.lock ms/ghidra/ntoskrnl.lock~
    rm -rf ms/ghidra/ntoskrnl.gpr ms/ghidra/ntoskrnl.rep ms/ghidra/ntdll.gpr ms/ghidra/ntdll.rep
    ```
    (JDK 문제를 고친 뒤 `run_ghidra.sh analyze` 는 `-overwrite` 를 쓰지만, 살아 있는 프로세스가 `ntoskrnl.lock` 을 쥐고 있으면 충돌한다. 먼저 죽일 것.)
- `ms/decomp/ntdll/`, `ms/disassemble/ntdll/` 는 폴더만 있고 **비어 있다.**
- 요청서의 예시 명령 한 가지: `> ms/ghidra/ntoskrnl.analyze.stdout` 리다이렉트는 `ms/ghidra/` 가 없으면 실패한다 (`run_ghidra.sh` 의 `mkdir -p` 는 그 뒤에 실행됨). 이번엔 `mkdir -p ms/ghidra` 를 먼저 했다. 다음 요청서 예시에 반영하면 좋다.

## 질문별 답

1. **스크립트 4개가 에러 없이 도는가?** → 아니오.
   - `run_ghidra.sh`: 셸은 정상 (GHIDRA_HOME 검사, ms/bin 에서 바이너리 찾기, 폴더 만들기, Ghidra 기동, 프로젝트 생성, import 까지 확인). 하지만 안쪽 스크립트가 죽어도 exit 0 으로 끝난다.
   - `SetPdb.java`: 아니오 (osgi.ee=UNKNOWN)
   - `ExportIndex.java`: 아니오 (같은 에러)
   - `ExportFunctions.java`: 아니오 (같은 에러)
   - JDK 25 관련: 위 에러 + `sun.misc.Unsafe` 경고. 원인은 JDK 25 로 보는 게 맞다 (Felix 7.0.5 가 Java 25 를 모름).
   - 부수 확인: ntdll analyze 는 PDB·스크립트 없이도 약 35초 만에 끝났다 (00:24:24 시작, 00:24:58 종료). ntdll 은 가볍다.
2. **PDB가 붙었는가?** → **아니오.**
   - ntdll 로그 79줄 `Skipping PDB processing: failed to locate PDB file in configured locations`. 원인은 `SetPdb.java` 가 죽어서 PDB 경로를 못 알려 준 것.
   - 전체 함수 수 / `FUN_` 수: `_index.tsv` 가 안 만들어져서 **셀 수 없다.**
3. **알려진 사실 표와 `_calls.tsv` 가 맞는가?** → **확인 못 함** (`_calls.tsv` 없음). 아래 표는 JDK 문제를 푼 뒤 채울 빈칸이다.

   | caller | 관찰 | callee | call_off | ret_off | 같은 쪽 |
   |---|---|---|---|---|---|
   | `LdrInitializeThunk` | `+0xe` | `LdrpInitializeInternal` | 미확인 | 미확인 | 미확인 |
   | `LdrpInitializeInternal` | `+0x5a` | `LdrpInitialize` | 미확인 | 미확인 | 미확인 |
   | `LdrpInitialize` | `+0x16c` | `LdrpInitializeProcess` | 미확인 | 미확인 | 미확인 |
   | `LdrpInitializeProcess` | `+0x1bbb`(ret) / `+0x1bb6`(call) | `LdrpInitializeGraphRecurse` | 미확인 | 미확인 | 미확인 |
   | `LdrpInitializeProcess` | `+0x19a2` | `LdrpInitializeKernel32Functions` | 미확인 | 미확인 | 미확인 |
   | `LdrpInitializeProcess` | `+0x1c99` | `LdrpCallTlsInitializers` | 미확인 | 미확인 | 미확인 |
   | `LdrpInitializeNode` | `+0x16b` / `+0x19c` | `LdrpCallTlsInitializers` / `LdrpCallInitRoutine` | 미확인 | 미확인 | 미확인 |
   | `LdrpLoadDll` | `+0x100` | `LdrpLoadDllInternal` | 미확인 | 미확인 | 미확인 |

4. **export 결과물 형식이 reference.md와 같은가?** → **확인 못 함.** 결과물 파일이 하나도 없다 (`.c`, `.asm`, `_calls.tsv`, `_index.tsv` 전부 0개). 소스를 읽은 것으로는, 코드가 reference.md 양식대로 쓰게 되어 있다 (`.c` 머리 주석 3줄, `.asm` 의 `+0x… 주소 명령 ; CALL ret=+0x… -> 이름`, `_calls.tsv` 4열, 간접 호출 `indirect:이름`, `_index.tsv` 5열 `name entry rva size thunk`). 하지만 `[소스]` 읽은 것일 뿐 돌려 본 것이 아니다.
5. **불일치 원인이 스크립트인가 바이너리인가?** → 대조할 값이 없어서 가릴 수 없다. 지금 문제는 **도구 환경(JDK 25 ↔ Ghidra 11.3.2 의 OSGi)** 이고, 스크립트 로직도 바이너리도 아니다. 바이너리 쪽은 커널 PDB GUID `C29EBFB06B78B3C020DCA66D99713F9E1` 이 기준과 같다는 것(요청서가 확인해 줌)까지만 믿는다.
6. **ntoskrnl analyze 가 살아 있는가?** → 예. PID 838669, 로그 `ms/ghidra/ntoskrnl.analyze.log`, stdout `ms/ghidra/ntoskrnl.analyze.stdout`. 시작하고 약 5초 만에 SetPdb 에러가 났는데 프로세스는 안 죽고 PDB 없이 분석을 계속하는 중. 위 "후유증" 참고.

## 동적분석으로 확인해야 할 것

- 없음 (도구 시험).

## 다음에 할 일 (제안)

1. 사용자가 JDK 21 설치 + `JAVA_HOME`/`JAVA_HOME_OVERRIDE` 설정.
2. 위 정리 명령으로 ntoskrnl 프로세스와 PDB 없는 `.gpr` 두 개 지우기.
3. `fetch_pdb.py` 는 다시 안 해도 된다 (PDB 이미 있음).
4. 01 요청서 그대로 다시 돌린다. 이번엔 ntdll 은 PDB가 붙는지(`_index.tsv` 의 `FUN_` 수)와 `_calls.tsv` 대조가 진짜 시험거리가 된다.

## 판정

**실패** — JDK 25 가 Ghidra 11.3.2 의 스크립트 로더(OSGi Felix 7.0.5)와 안 맞아서 SetPdb·ExportIndex·ExportFunctions 가 전부 로딩 단계에서 죽었다. PDB 미적용, 결과물 0개. 스크립트 로직과 오프셋 계산은 아직 시험하지 못했다.
