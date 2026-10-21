# 05 callgraph-size — 정적 분석 결과 (실험: 지도 크기 재기)

- 요청서: `_workspace/sast/05_callgraph-size.md` (Q37)
- 모듈: ntoskrnl (뿌리 `NtCreateUserProcess`), ntdll (뿌리 `LdrInitializeThunk`). 기준 빌드 26100.9457.
- 방법: 요청서의 명령 그대로. analyze 안 함 (`-noanalysis -readOnly`). 스크립트와 `.claude/` 안 고침.
- 두 모듈 다 이미 분석돼 있었다 (`.gpr`, `_index.tsv`, analyze 로그의 `SetPdb:` 줄 모두 있음).
- 출력 (ms/ 아래라 커밋 안 됨):
  - `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_{edges,nodes}.tsv`, `_summary.txt`
  - `ms/disassemble/ntdll/_map/LdrInitializeThunk_{edges,nodes}.tsv`, `_summary.txt`
- 로그: `ms/ghidra/{ntoskrnl,ntdll}.map.{log,stdout}`. `SCRIPT ERROR` 없음, 종료코드 0 (둘 다).
- 두 작업은 동시에 돌렸다 (서로 다른 프로젝트, 읽기 전용).

---

## 맨 위 요약 (질문 2)

**공용 도구로 잘린 흐름도 함수는 없다 (기준 20, 기준 50).** 요청서가 고른 흐름도 함수 중 지도에 안 들어온 것은 2개뿐이고, 둘 다 "공용 도구 걸러내기" 때문이 아니다.

- 기준 20: 커널 24개 전부 지도에 들어옴, 전부 `expanded` (깊이 0~5). 유저 17개 중 15개 들어옴 (`NtContinue` 는 `syscall` 로 끝남).
- 기준 10 으로 낮추면: 커널의 `IopCreateFile` 한 개가 잘린다 (caller 17곳 → `helper`). 나머지 23개는 그대로.
  - 이건 B20 줄 위에서 내가 다시 따라가 본 것 `[정적, 참고]`. `nodes.tsv` 에서 `IopCreateFile` 의 caller 수가 17인 것은 파일 값 그대로.
  - 유저 쪽은 흐름도 함수의 caller 수가 전부 8 이하라 기준 10 에서도 안 잘린다.
- 못 들어온 2개 (`ImageTlsCallbackCaller`, `RtlUserThreadStart`): **"호출"로는 닿지 않는 함수라서** 어떤 걸러내기를 써도 안 들어온다 (아래 질문 2).

## 스크립트 문제 (`CallGraphMap.java`, 고치지 않음)

스크립트: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/3f4d15aa-16f7-4279-8177-858e012325c7/scratchpad/exp/CallGraphMap.java`

1. **CFG 간접 호출이 `indirect-cfg` 로 안 잡히고 평범한 `call` 로 나온다.**
   - 증상: `_summary.txt` 의 "끊긴 끝" 종류별에 `indirect-cfg` 가 **0개**. 그런데 같은 바이너리의 `_calls.tsv` 에는 `indirect:cfg:...` 줄이 있다 (예: `NtCreateUserProcess +0x5b2`, `LdrpInitializeProcess +0x1b59`).
   - 지도 안에서는 이 호출들이 `callee = _guard_dispatch_icall` (커널) / `_guard_dispatch_icall$thunk$...` (ntdll) 로 찍힌다.
     - 커널: `_guard_dispatch_icall` 268줄 + `_guard_check_icall` 12줄 = 280곳.
     - ntdll: `_guard_dispatch_icall$thunk$...` 71줄 (call 65 + tail 6) + `_guard_check_icall$thunk$...` 3줄 = 74곳.
     - 숫자는 `_edges.tsv` 에서 센 값 (같은 caller+call_off 중복 없음).
   - 원인 추정: `resolve()` 의 CFG 판정(149~153째 줄)은 `ins.getFlows()` 가 비어 있을 때만 닿는데, 이 두 모듈에선 CFG 호출이 "함수로 가는 직접 호출"로 보여서 111~127째 줄의 `flows` 갈래에서 그냥 `target` 이 돼 버린다.
   - 영향: 지도에서 "실행 때 대상이 정해지는 곳"이 끊김으로 안 보이고, 대신 `_guard_dispatch_icall` 이 공용 도구(caller 3102 / 224곳)라 조용히 `helper` 로 잘린다. 예: `LdrpCallInitRoutineInternal +0x1d` (DLL 진입점·TLS 콜백을 실제로 부르는 자리)가 평범한 `call` 줄로만 남는다.
   - 고칠 방법 제안: `resolve()` 에서 대상 함수 이름이 `_guard_dispatch_icall`·`_guard_check_icall`(`$thunk$` 포함)로 시작하면 `kind=indirect-cfg`, `label=cfg:` + `cfgSource(ins, body)` 로 처리 (`_guard_check_icall` 은 검사만 하고 호출은 이어지는 명령이므로 따로 구분하면 더 좋다).
2. **"끊긴 끝(서로 다른 것)" 이 라벨로 중복을 없앤다** (`leaves.putIfAbsent(kind + ":" + label)`, 331째 줄 근처).
   - 증상: `indirect=1` 이라고 나오지만 실제 `indirect` 줄은 커널 8곳, ntdll 15곳 (`_edges.tsv` 기준). 라벨이 전부 `?` 라서 하나로 합쳐진다.
   - 고칠 방법: 호출 자리 수(caller+call_off)를 따로 세어 요약에 같이 적기.
3. (참고, 버그는 아님) `layers()` 는 (A) 거르지 않은 전체에서도 `SYSCALL` 이 든 함수는 안 펼친다 (252째 줄 근처). ntdll (A) 는 "syscall 에서 멈춘 전체"다. 커널엔 해당 없음.
4. (참고) `_summary.txt` 의 "걸린 시간" 은 뿌리 찾은 뒤부터라서 프로젝트 여는 시간이 빠진다. 아래 질문 7에 둘 다 적었다.
5. (참고) 깊이 10 으로는 끝까지 못 간다 (질문 1). 그래서 **보충으로 깊이 80 으로 한 번씩 더 돌렸다** (같은 스크립트, 결과는 repo 밖 scratchpad `.../scratchpad/deep/`). 요청서 명령의 결과(위 `_map/`)는 깊이 10 그대로이고 덮어쓰지 않았다.
   - 앞쪽 깊이 0~10 숫자는 깊이 10 결과와 똑같다 (스크립트 일관성 확인).

---

## 질문별 답 [정적]

### 1. 지도 크기

(숫자는 `_summary.txt` 그대로, 합계는 더한 값. 깊이 = 뿌리에서 가장 짧게 닿는 호출 단계. 깊이 0 은 뿌리 자신)

**ntoskrnl** (`NtCreateUserProcess`, `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_summary.txt`)

| 깊이 | (A) 전체 | (B) 기준10 | (B) 기준20 | (B) 기준50 |
|---|---|---|---|---|
| 0 | 1 | 1 | 1 | 1 |
| 1 | 62 | 62 | 62 | 62 |
| 2 | 279 | 204 | 244 | 246 |
| 3 | 608 | 349 | 440 | 491 |
| 4 | 955 | 473 | 586 | 696 |
| 5 | 1221 | 544 | 719 | 942 |
| 6 | 1396 | 688 | 936 | 1231 |
| 7 | 1261 | 689 | 914 | 1084 |
| 8 | 1021 | 555 | 726 | 802 |
| 9 | 938 | 468 | 607 | 651 |
| 10 | 752 | 365 | 463 | 494 |
| 합계(깊이 10까지) | **8494** | **4398** | **5698** | **6700** |

**ntdll** (`LdrInitializeThunk`, `ms/disassemble/ntdll/_map/LdrInitializeThunk_summary.txt`)

| 깊이 | (A) 전체 | (B) 기준10 | (B) 기준20 | (B) 기준50 |
|---|---|---|---|---|
| 0 | 1 | 1 | 1 | 1 |
| 1 | 3 | 3 | 3 | 3 |
| 2 | 6 | 4 | 4 | 4 |
| 3 | 14 | 11 | 11 | 11 |
| 4 | 44 | 30 | 30 | 30 |
| 5 | 139 | 120 | 123 | 125 |
| 6 | 238 | 176 | 186 | 195 |
| 7 | 326 | 219 | 230 | 244 |
| 8 | 316 | 220 | 226 | 245 |
| 9 | 203 | 165 | 188 | 200 |
| 10 | 163 | 172 | 217 | 212 |
| 합계(깊이 10까지) | **1453** | **1121** | **1219** | **1270** |

- (B) 가 깊이 10 에서 (A) 보다 클 수 있는 이유 `[추측]`: 길을 막으면 어떤 함수는 더 먼 길로 돌아가 늦게 닿는다.
- **깊이 10 에서는 포화하지 않았다.** 깊이 10 줄에도 새 함수가 아직 많이 나온다 (커널 (A) 752, ntdll (A) 163). 그래서 보충으로 깊이 80 을 돌렸다.
- 보충(깊이 80, 같은 스크립트, 결과는 `.../scratchpad/deep/`):

| 모듈 | (A) 마지막 새 함수 깊이 / 전체 | (B)10 | (B)20 | (B)50 |
|---|---|---|---|---|
| ntoskrnl | 깊이 37 / **11688** | 깊이 40 / 5324 | 깊이 47 / **9952** | 깊이 46 / 11081 |
| ntdll | 깊이 18 / **1738** | 깊이 18 / 1480 | 깊이 18 / **1640** | 깊이 18 / 1683 |

  - 커널 (A) 11688 은 `_index.tsv` 의 커널 함수 34982개 중 약 33%. ntdll (A) 1738 은 4593개 중 약 38%. (나눈 값은 내 계산)
  - 기준 20 의 전체(깊이 80)는 `_summary.txt` 의 "함수 9952개 / 1640개" 와 같다.
  - 눈에 띄는 점: 커널 (B)20 전체 9952 는 (A) 11688 의 85%. **공용 도구 기준 20 으로 걸러도 전체는 15% 밖에 안 줄어든다.** 기준 10 은 5324 로 절반 이하.

### 2. 흐름도 함수가 지도에 들어오나 (기준 20 지도, 깊이 10 결과)

근거: `_nodes.tsv` 의 이름·깊이·caller 수·상태 칸 그대로. "깊이" = 그 함수가 처음 나온 깊이.

**커널** (`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_nodes.tsv`) — 24개 전부 있음, 전부 `expanded`.

| 함수 | 처음 나온 깊이 | caller 수(프로그램 전체) | 상태 |
|---|---|---|---|
| NtCreateUserProcess | 0 | 0 | expanded (뿌리) |
| PspAllocateProcess | 1 | 3 | expanded |
| PspAllocateThread | 1 | 3 | expanded |
| PspInsertProcess | 1 | 3 | expanded |
| PspInsertThread | 1 | 3 | expanded |
| PspBuildCreateProcessContext | 1 | 3 | expanded |
| **IopCreateFile** | 1 | **17** | expanded (기준 10 이면 잘림) |
| MmCreateSpecialImageSection | 1 | 2 | expanded |
| PspCreateUserContext | 1 | 1 | expanded |
| PspMapThreadCreationFlags | 1 | 1 | expanded |
| PsTestProtectedProcessIncompatibility | 1 | 6 | expanded |
| PspCreateObjectHandle | 1 | 4 | expanded |
| PspUpdateCreateInfo | 1 | 2 | expanded |
| MmCreatePeb | 2 | 1 | expanded |
| PspCallProcessNotifyRoutines | 2 | 2 | expanded |
| KeSuspendThread | 2 | 2 | expanded |
| KeReadyThread | 2 | 1 | expanded |
| MmCreateProcessAddressSpace | 2 | 1 | expanded |
| MmInitializeProcessAddressSpace | 2 | 2 | expanded |
| PspSetupUserProcessAddressSpace | 2 | 1 | expanded |
| MiMapProcessExecutable | 3 | 1 | expanded |
| MmMapViewOfSectionEx | 4 | 5 | expanded |
| PsMapSystemDlls | 4 | 1 | expanded |
| PspMapSystemDll | 5 | 4 | expanded |

**유저** (`ms/disassemble/ntdll/_map/LdrInitializeThunk_nodes.tsv`) — 15개 있음, 2개 없음.

| 함수 | 처음 나온 깊이 | caller 수 | 상태 |
|---|---|---|---|
| LdrInitializeThunk | 0 | 0 | expanded (뿌리) |
| LdrpInitialize | 1 | 1 | expanded |
| NtContinue | 1 | 2 | **syscall** (함수 안에 SYSCALL, 커널로 넘어감) |
| LdrpInitializeInternal | 2 | 1 | expanded |
| _LdrpInitialize | 3 | 1 | expanded |
| LdrpInitializeProcess | 4 | 1 | expanded |
| LdrpInitializeKernel32Functions | 5 | 1 | expanded |
| LdrpPrepareModuleForExecution | 5 | 2 | expanded |
| LdrpInitializeGraphRecurse | 5 | 7 | expanded |
| LdrpCallTlsInitializers | 5 | 6 | expanded |
| LdrpCallInitRoutine | 5 | 8 | expanded |
| LdrLoadDll | 6 | 4 | expanded |
| LdrpLoadDll | 6 | 8 | expanded |
| LdrpInitializeNode | 6 | 2 | expanded |
| LdrpLoadDllInternal | 7 | 3 | expanded |
| **ImageTlsCallbackCaller** | 없음 | - | 지도에 없음 |
| **RtlUserThreadStart** | 없음 | - | 지도에 없음 |

**지도에 없는 2개 — 왜 없나** (`_index.tsv` 에는 둘 다 있다: 1222째 줄 `RtlUserThreadStart` rva 0xacac0, 1905째 줄 `ImageTlsCallbackCaller` rva 0xe83e0)

이 두 개는 걸러져서 빠진 게 아니라, **누구도 "호출"하지 않아서** 호출 지도에 닿지 않는다. 확인을 위해 ntdll 프로젝트에서 두 함수의 진입 주소로 오는 참조를 전부 찍어 봤다 (진단용 짧은 스크립트, 요청서 밖. 위치 `.../scratchpad/diag/RefsTo.java`).

- `ImageTlsCallbackCaller` 로 오는 참조: 전부 `DATA` 3개. 그중 하나가 `LdrpCallTlsInitializers+0x18f` (rva 0x10fdf) — **함수 주소를 꺼내 쓰는 자리**일 뿐 호출 명령이 아니다. 나머지 둘은 함수 밖(rva 0x1dedb0, 0x175fb9, 표 종류로 짐작 `[추측]`).
  - 읽는 법 `[정적+추측]`: `LdrpCallTlsInitializers` 가 이 함수의 **주소를 값으로 들고 있다가** `LdrpCallInitRoutine` 쪽으로 넘기는 모양일 것이다 (`LdrpCallTlsInitializers +0x196` 에서 `LdrpCallInitRoutine` 을 부르는 줄이 바로 뒤에 있다, `_edges.tsv`). 그래서 `LdrpCallInitRoutine` → … → CFG 간접 호출이 마지막에 실제로 부른다. 이 연결은 정적으로 안 보인다 (아래 동적 확인).
- `RtlUserThreadStart` 로 오는 참조: `DATA` 3개(전부 함수 밖: rva 0x1b9a9c, 0x1dc494, 0x1759f1) + `EXTERNAL` 1개(내보내기 표 항목으로 짐작 `[추측]`). **`CALL`/`JMP` 로 오는 참조가 0개.**
  - 요청서의 짐작 `[추측]` ("호출이 아니라 `NtContinue` 로 실행이 넘어가서 닿는다")과 **어긋나지 않는다.** 이 지도에서 `NtContinue` 는 깊이 1 에서 `syscall` 로 끝나고, 그 뒤(커널이 넘겨줄 RIP) 쪽은 호출 줄로 안 이어진다. 즉 끊긴 곳 = `NtContinue` 의 `SYSCALL`. 실제로 RIP 이 `RtlUserThreadStart` 로 가는지는 정적으로 못 정한다 (동적으로 확인).
- 이 두 함수는 **(A) 거르지 않은 지도에도 없다** (호출 참조가 없으므로). 즉 "공용 도구 걸러내기" 와 무관하게 호출 지도만으론 대표 지점을 못 얻는다.

### 3. 알려진 호출 줄이 지도(`_edges.tsv`)에 같은 오프셋으로 있나 — **전부 일치**

`_edges.tsv` 의 열 `ret_off` (WinDbg `caller+0x..` 와 같은 값) 로 비교했다. 같은 오프셋이면 스크립트가 맞게 계산한 것.

| caller | 기대 | 지도에서 찾은 줄 |
|---|---|---|
| NtCreateUserProcess | +0xc36 → PspAllocateProcess | call_off +0xc31, ret_off **+0xc36** 일치 |
| PspAllocateProcess | +0x2095 → MmInitializeProcessAddressSpace | call +0x2090, ret **+0x2095** 일치 |
| MiMapProcessExecutable | +0xdc → PsMapSystemDlls | call +0xd7, ret **+0xdc** 일치 |
| PspInsertThread | +0x807 → PspCallProcessNotifyRoutines | call +0x802, ret **+0x807** 일치 |
| PspInsertThread 조각 안 | `KeForceResumeThread` (frag:) | `frag:FUN_140b0d692+0x1a / +0x1f` 와 `frag:FUN_140b0d71b+0x3a / +0x3f` 두 곳에 있음. 조각 오프셋은 조각 시작 기준 (export 때와 같은 규칙) |
| LdrInitializeThunk | +0xe → LdrpInitialize | call +0x9, ret **+0xe** 일치 |
| LdrpInitialize | +0x3c tail → LdrpInitializeInternal | call_off +0x3c, ret_off `tail`, kind `tail` 일치 |
| _LdrpInitialize | +0x16c → LdrpInitializeProcess | call +0x167, ret **+0x16c** 일치 |
| LdrpInitializeProcess | +0x1bbb → LdrpInitializeGraphRecurse | call +0x1bb6, ret **+0x1bbb** 일치 |
| LdrpInitializeNode | +0x19c → LdrpCallInitRoutine | call +0x197, ret **+0x19c** 일치 |

- 덧붙여 `LdrpInitializeNode +0x16b → LdrpCallTlsInitializers` 도 일치 (스킬의 알려진 사실 표).
- 이 줄들의 `note` 칸에 `(seen)` 이 붙은 것은 "그 함수가 이미 지도에 있었음" 이라는 뜻일 뿐 줄 자체는 정상이다.
- 결론: 스크립트의 오프셋 계산은 믿을 수 있다. 다만 위 "스크립트 문제 1"(CFG 표시)은 별개.

### 4. 끊긴 곳

**상태별 함수 수** (`_summary.txt` 의 "상태별", 기준 20, 깊이 10)

| 모듈 | expanded | helper | syscall | depth | 합 |
|---|---|---|---|---|---|
| ntoskrnl | 4713 | 522 | 0 | 463 | 5698 |
| ntdll | 875 | 48 | 79 | 217 | 1219 |

- `depth` = 깊이 10 에 닿아서 더 안 본 것 (진짜 끊김 아님, 보충 깊이 80 에선 `depth` 가 0).
- `helper` = 공용 도구라 안 펼친 것 (지도 크기를 줄이려는 선택).
- `syscall` = ntdll 의 `Nt*`/`Zw*` 처럼 커널로 넘어가는 말단. 커널에는 없음.

**끊긴 끝 종류별** (`_nodes.tsv` 의 `-` 줄 = 서로 다른 라벨 수 / `_edges.tsv` 의 호출 자리 수)

| 종류 | ntoskrnl 라벨 / 자리 | ntdll 라벨 / 자리 | 예 3개 |
|---|---|---|---|
| external (다른 모듈로 나감) | 33 / 47 | 0 / 0 | `PsGetProcessParameterOverrides` (`PspGetProcessParameterOverrides +0x34`), `PsDestroyProcessParameterOverrides` (`PspDestroyProcessParameterOverrides +0xf`), `Win32kAsyncProcessFreezeThawSupportIsActive` (`PsFreezeProcess +0xb1`) |
| indirect (레지스터 안 주소, 라벨 `?`) | 1 / 8 | 1 / 15 | 커널: `RtlCopyFromUser +0x24`, `RtlWriteULong64ToUser +0x1c`, `RtlpExecuteHandlerForException +0xd`. ntdll: `_LdrpInitialize +0x9a`, `LdrpInitializeProcess +0x17fc`, `RtlpCallInterceptRoutine +0x2aa` |
| indirect-cfg | **0 / 0 (스크립트 문제 1: 실제론 있음)** | **0 / 0** | 아래 "숨은 CFG 호출" |

- 숨은 CFG 호출 (지도에선 평범한 `call _guard_dispatch_icall…` 로 찍힘, 대상은 RAX 안이라 정적으로 모름): 커널 280자리, ntdll 74자리.
  - 예 (커널): `NtCreateUserProcess +0x5b2` (`_calls.tsv` 에선 `indirect:cfg:@0xfd83d0`). (ntdll): `LdrpInitializeProcess +0x1b59` (`indirect:cfg:Kernel32ThreadInitThunkFunction`), `LdrpCallInitRoutineInternal +0x1d`.
- 지도에서 `tail`(꼬리호출) 줄: 커널 28, ntdll 79. 그중 CFG 쪽은 커널 `_guard_icall_bugcheck` 1개, ntdll `_guard_dispatch_icall$thunk$…` 6개.
- 지도 줄 수: 커널 26537줄 (`call` 26454 / `tail` 28 / `external` 47 / `indirect` 8), ntdll 4970줄 (`call` 4876 / `tail` 79 / `indirect` 15).
- 별칭 표시(`alias: 같은 주소 이름 N개 더`)가 붙은 줄: 커널 1813줄, ntdll 327줄. **지도의 함수 이름을 문서에 그대로 쓰면 안 되는 자리가 많다는 뜻** (이름은 Ghidra 가 고른 하나). 예: `NtCreateUserProcess +0x5b2` 의 `_guard_dispatch_icall` 도 alias 1개 표시.

### 5. 공용 도구로 잘린 것 중 caller 가 가장 많은 15개 (기준 20, 깊이 10)

`_nodes.tsv` 의 `helper` 줄을 caller 수로 정렬. 크기(바이트)는 `size` 칸.

**ntoskrnl** (helper 522개 중)

| # | 이름 | caller 수 | 크기 |
|---|---|---|---|
| 1 | __security_check_cookie | 4197 | 30 |
| 2 | ExFreePoolWithTag | 3136 | 6131 |
| 3 | _guard_dispatch_icall | 3102 | 141 |
| 4 | memset | 2630 | 236 |
| 5 | ExAllocatePool2 | 2097 | 268 |
| 6 | RtlCopyMemory | 1435 | 537 |
| 7 | KeBugCheckEx | 1225 | 288 |
| 8 | KeAbPreAcquire | 1036 | 382 |
| 9 | KeAbPostRelease | 998 | 237 |
| 10 | ZwClose | 820 | 30 |
| 11 | KiLowerIrqlProcessIrqlFlags | 817 | 91 |
| 12 | KiRaiseIrqlProcessIrqlFlags | 784 | 104 |
| 13 | KeLeaveCriticalRegion | 773 | 23 |
| 14 | ExfAcquirePushLockExclusiveEx | 740 | 733 |
| 15 | RtlInitUnicodeString | 706 | 68 |

**ntdll** (helper 48개 중)

| # | 이름 | caller 수 | 크기 |
|---|---|---|---|
| 1 | __security_check_cookie | 580 | 30 |
| 2 | memset | 397 | 5 |
| 3 | RtlFreeHeap | 343 | 2737 |
| 4 | RtlReleaseSRWLockExclusive | 316 | 452 |
| 5 | RtlCopyMemory | 298 | 537 |
| 6 | RtlAllocateHeap | 282 | 2797 |
| 7 | RtlAcquireSRWLockExclusive | 251 | 78 |
| 8 | _guard_dispatch_icall$thunk$10345483385596137414 | 224 | 5 |
| 9 | RtlGetCurrentServiceSessionId | 172 | 25 |
| 10 | RtlLeaveCriticalSection | 157 | 776 |
| 11 | wcslen | 112 | 278 |
| 12 | RtlEnterCriticalSection | 110 | 242 |
| 13 | RtlDeleteBoundaryDescriptor | 99 | 23 |
| 14 | RtlReleaseSRWLockShared | 90 | 564 |
| 15 | _invalid_parameter | 75 | 258 |

보는 눈 (사람이 판단할 거리):
- 위 15개는 전부 "잡일"(메모리 할당·해제, 락, 쿠키 검사, 복사, 문자열 길이, 버그체크)로 보인다 `[추측]`.
- **눈여겨볼 하나:** `_guard_dispatch_icall`(커널 3102곳) / `$thunk$…`(ntdll 224곳). 이건 "잡일"이 아니라 **간접 호출이 지나는 문**이다. 이걸 `helper` 로 자르면 "여기서 실행 때 정해지는 호출이 일어난다"는 정보가 지도에서 사라진다 (스크립트 문제 1).
- 큰 함수도 잘린다: 커널 helper 522개 중 크기 300바이트 이상이 155개 (큰 것: `ExReleaseResourceLite` 6563바이트 caller 362곳, `MiInsertPageInFreeOrZeroedList` 5725 / 52곳, `MiGetPage` 5178 / 39곳, `SeQueryInformationToken` 4957 / 29곳, `KiSearchForNewThreadsForRescheduleContext` 4580 / 22곳). caller 가 20곳 넘어도 흐름에 중요한 큰 함수일 수 있어서, 기준 20 이면 안전하다는 보장은 없다 (이번 24+15개 목록에선 해당 없음).
- ntdll 에서 caller 수 20 이상이지만 이름이 낯선 것 중 `LdrpLogInternal`(1245바이트, 70곳)은 로그 함수라 잡일로 보인다 `[추측]`.

### 6. 읽을 만한 크기인가 (기준 20 지도, 펼친 함수 expanded 수)

| 모듈 | 깊이 10 까지 | 끝까지(보충, 깊이 80) |
|---|---|---|
| ntoskrnl | **4713** | **9327** (helper 625 별도) |
| ntdll | **875** | **1479** (syscall 112, helper 49 별도) |

- 커널은 **수천**(4.7천 → 끝까지 9.3천), ntdll 은 **수백~천 남짓**.
- 읽을 수 있는 크기가 되는 깊이를 보면: 기준 20 지도의 누적은 커널 깊이 ≤2 가 307개(1+62+244), 깊이 ≤5 가 2052개. ntdll 은 깊이 ≤5 가 172개(1+3+4+11+30+123), 깊이 ≤7 가 588개(172+186+230) (내 계산: 위 표를 더한 값).
- 의견 `[추측]`:
  - 지도 전체를 "읽는 목록"으로 쓰기엔 너무 크다 (커널 수천 개, 의미는 함수 하나씩 봐야 한다). 지도는 **"이 함수가 뿌리 아래 몇 단계에 있나", "이 함수를 누가 부르나"를 찾아보는 색인**으로 쓰는 게 맞아 보인다.
  - 이번 24+15개 흐름도 함수는 지도 깊이 0~7 안에 다 있어서, 색인으로는 대표 지점을 잃지 않았다. 다만 두 가지 한계가 있다. (1) 호출이 아닌 이어짐(`NtContinue` → `RtlUserThreadStart`, `ImageTlsCallbackCaller`)은 지도로 안 잡힌다. (2) CFG 간접 호출(커널 280곳, ntdll 74곳)이 끊김으로 표시되지 않아서, "실행 때 정해지는 곳"이 지도에 안 보인다 (스크립트 문제 1 을 고친 뒤 다시 재야 정확하다).
  - 기준 20 은 이번 목록에선 안전했지만, 기준 10 은 `IopCreateFile` 을 자른다. 기준을 낮추면 흐름도 함수를 잃을 수 있다. 줄이려면 기준을 올리기보다 **깊이로 자르는 게(예: 깊이 ≤5)** 더 안전해 보인다.

### 7. 걸린 시간 (모듈마다)

| 모듈 | 스크립트 안(`_summary.txt` "걸린 시간") | 전체(JVM 시작부터 끝, `date` 차이) |
|---|---|---|
| ntoskrnl | 6초 | 10초 |
| ntdll | 1초 | 4초 |

- 두 모듈을 **동시에** 돌렸다 (프로젝트가 달라서 서로 안 막음). 30분 제한과는 거리가 멀다.
- 보충 깊이 80: ntoskrnl 8초(스크립트 안) / 11초(전체), ntdll 1초 / 4초 (이건 순서대로 돌림).
- analyze 는 하지 않았다.

---

## 동적분석으로 확인해야 할 것

요청서에서 "없어도 된다" 했지만, 지도로 안 잡힌 이어짐이 있어서 남긴다 (필요하면 dast 요청).

- `RtlUserThreadStart` 로 실제 RIP 이 가는 곳: 정적으론 `CALL`/`JMP` 참조가 0개. 커널이 `NtContinue` 가 쓰는 컨텍스트에 이 주소를 넣는지는 정적으로 못 정함.
- `ImageTlsCallbackCaller` 가 `LdrpCallInitRoutine` → CFG 간접 호출 어디서 불리는지: 주소를 꺼내는 자리는 `LdrpCallTlsInitializers+0x18f` 로 찾았으나 호출 연결은 간접이라 모름.
- CFG 간접 호출의 실제 대상 (예: `LdrpCallInitRoutineInternal +0x1d`): CALL 직전 `rax`.
- 추천 멈춤점 (이미 run8 등에서 쓴 것과 겹침): `ntdll!LdrpCallInitRoutineInternal` (+0x1d 호출 직전 `rax`), `ntdll!NtContinue` (넘겨줄 컨텍스트의 RIP).

## 판정

**분석성공.** 요청서 명령 그대로 두 모듈 다 오류 없이 끝났고 (종료코드 0, `SCRIPT ERROR` 없음), 알려진 호출 줄 10개가 지도에 같은 오프셋으로 있다. 질문 2 의 핵심: 기준 20 에서 공용 도구로 잘린 흐름도 함수는 0개 (기준 10 은 `IopCreateFile` 1개). 스크립트에는 고칠 점이 2개 있다 (CFG 표시, 끊긴 끝 중복 제거) — 숫자 해석에 영향이 있어서 위 "스크립트 문제" 절에 적었다.
