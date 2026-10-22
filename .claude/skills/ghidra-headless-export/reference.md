# ghidra-headless-export 참고

## 출력 파일 형식

### `ms/decomp/<모듈>/_index.tsv`
```
# name	entry	rva	size	thunk
LdrpInitializeProcess	18008c404	0x8c404	7750
```

### `ms/decomp/<모듈>/<함수>.c`
```c
// ntdll!LdrpInitializeProcess  entry=18008c404  rva=0x8c404
// callers: LdrpInitialize
// callees: LdrpInitializeKernel32Functions LdrpInitializeGraphRecurse ...
<Ghidra 디컴파일 C>
```
같은 이름 함수가 둘이면 두 번째부터 `<함수>__<rva>.c`.

### `ms/disassemble/<모듈>/<함수>.asm`
```
; ntdll!LdrpInitializeProcess  entry=18008c404  rva=0x8c404
+0x1bb6   18008dfba  CALL 0x180087d60    ; CALL ret=+0x1bbb -> LdrpInitializeGraphRecurse
```
- 맨 앞 `+0x…` = 함수 시작에서의 거리. 함수 조각이 시작점보다 앞에 있으면 `-0x…`.
- `ret=` = call 다음 명령 위치 = **WinDbg 콜스택에 찍히는 `caller+0x…`**.

### `ms/disassemble/<모듈>/_calls.tsv`
```
LdrpInitializeProcess	+0x1bb6	+0x1bbb	LdrpInitializeGraphRecurse
LdrpCallInitRoutine	+0x41	+0x47	indirect:__guard_dispatch_icall_fptr
```
열: `caller  call_off  ret_off  callee`. 간접 호출은 `indirect:<포인터 이름>` (실제 대상은 동적분석으로만 알 수 있음).

- 정렬: caller 이름 → 오프셋 **숫자** 순 (`sort_calls.py`, 묶음 N1). 본체 줄 다음에 조각(`frag:`) 줄.

- `ret_off` 가 `frag` = **자기 조각(`FUN_`)으로 가는 `JMP`.** 호출이 아니라 함수 안에서 창고에 다녀오는 이동이다. `.asm` 에는 `; FRAGJMP -> FUN_…`. 예전(묶음 N1 전)엔 `tail` 로 찍혔다 (예: `MmCreatePeb +0x6b4`).
- `ret_off` 가 `tail` = **꼬리호출(`JMP`)**. 예: `LdrpInitialize  +0x3c  tail  LdrpInitializeInternal`.
  - Ghidra 분석기 "Shared Return Calls" 가 함수 시작으로 가는 `JMP` 를 호출로 바꿔 둔 것이다.
  - 돌아올 자리가 없어서 **WinDbg 콜스택엔 caller(`LdrpInitialize`)가 안 보이고**, callee 바로 위에 caller의 caller가 보인다.
  - `.asm` 에는 `; TAILJMP -> 대상`.
- `INT3`, `INT 0x29`(`__fastfail`), `INT 0x2e` 같은 `INT` 명령은 표에 **넣지 않는다.** `.asm` 에 `; TRAP` 만 붙는다.
  - Ghidra x86 사양(`ia.sinc`)이 이것들을 "인터럽트 처리기로의 간접 call" 로 적어서 호출로 잡히지만, 함수 호출이 아니다.
- callee 가 `indirect:cfg:<출처>` = **CFG(제어 흐름 보호) 검사 함수를 거친 간접 호출**. 예: `NtCreateUserProcess  +0x5b2  +0x5b7  indirect:cfg:<변수 이름>`.
  - ntoskrnl 은 `CALL _guard_dispatch_icall`, ntdll 은 `CALL [__guard_dispatch_icall_fptr]` 모양이다. 진짜 대상은 RAX 안에 있다.
  - `<출처>` = CALL 앞(최대 10개 명령)에서 RAX 를 채운 메모리 자리(함수 포인터 변수)의 이름. 이름이 없으면 `@0x<rva>`, 못 찾으면 `?`.
  - 실제 대상은 동적으로만 안다 (그 변수 값 또는 CALL 직전 `rax`).
  - 출처가 **지역 변수 칸**(스택)이면 `indirect:cfg:stack[-0x40]` 처럼 스택 오프셋으로 적는다. 묶음 N1 전엔 이 경우가 `@0xfffffffe…` 처럼 이상한 값으로 찍혔다 (지도에 커널 2곳·ntdll 7곳, 예: `PspReturnQuota +0x217`). 다른 모듈 주소(import)면 `indirect:cfg:import:<이름>`.
- callee 가 `alias:<이름>+N` = 대상 주소에 **PDB 이름이 N개 더 있다.** 링커가 내용이 같은 함수를 하나로 합친 것(ICF)일 때가 많다.
  - `<이름>` 은 Ghidra 가 고른 하나일 뿐이다. **문서에 그대로 쓰지 않는다.** `.asm` 에 `; ALIASES(N): …` 로 5개까지 보인다.
  - 그 주소의 **이름 전부**는 별칭 표에서 본다: `python3 .claude/skills/ghidra-headless-export/scripts/pdb_aliases.py --lookup ntoskrnl.exe <이름 또는 0xRVA>` (표: `ms/sym/<바이너리>.aliases.tsv`). PDB 기준이라 Ghidra 의 N 과 한두 개 다를 수 있다 (Ghidra 는 내보내기 이름도 센다).
  - `Ordinal_<숫자>`(내보내기 번호 라벨), `$thunk$` 가 든 이름, `Nt<X>`↔`Zw<X>` 짝은 이름이 틀릴 걱정이 없어서 **세지 않는다.**
  - 예: `NtCreateUserProcess +0x3cb` 의 대상은 `xor eax,eax; ret` 3바이트인데 `?GetStoragePropertyPost@SC_DEVICE...` 로 찍혔었다 (sast/03).
- `call_off`·`ret_off` 가 `frag:<FUN_이름>+0x..` = **함수에서 떨어져 나간 조각 안의 호출.** caller 칸은 본래 함수 이름이다.
  - 컴파일러는 잘 안 쓰는 길(에러 처리 등)을 함수 본체에서 떼어 멀리 둔다. Ghidra 는 그 조각을 이름 없는 `FUN_` 함수로 잡는다.
  - 스크립트는 함수가 `JMP`/`Jcc` 로 넘어가는 `FUN_` 함수를 조각으로 보고 따라가서 (조각의 조각도, 최대 20개) `.asm` 맨 아래 `; --- 조각 …` 아래에 붙인다.
  - 오프셋은 **조각 시작 기준**이다. WinDbg 가 이 주소를 콜스택에서 어떻게 보여 줄지는 확인 전이라 `caller+0x..` 로 바꿔 쓰지 않는다.
  - 예: `PspInsertThread +0xa33 tail FUN_140b0d692` 의 조각 안 `KeForceResumeThread` 등 (sast/03).
- **예외 정리 함수**: `<함수>$fin$N`, `<함수>$filt$N` 은 호출로 안 닿고 예외 때 운영체제가 부르는 형제 함수다. 장부·지도에 본래 함수 몫으로 같이 들어가고 오프셋은 `seh:<이름>+0x..` (그 함수 시작 기준). `.asm` 에는 `; --- 예외 정리 함수 …` 머리 (묶음 O2, 예: `CreateProcessInternalW$fin$0`, sast/09).
- 조각 줄 끝 `(= <함수> +0x..)` = 조각 오프셋을 본래 함수 시작 기준으로 바꾼 값 (`.asm` 만, 묶음 O1a). WinDbg 가 이 주소를 그 함수 이름으로 보여 줄지는 확인 전 `[추측]`.
- 보안 검사 호출 출처를 데이터 이름으로 못 찾으면 `indirect:cfg:[RBX+0x40]` 처럼 **RAX 를 채운 메모리 칸**(레지스터+오프셋)을 적는다 (전엔 `cfg:?`, 묶음 O1a). 구조체 칸에서 꺼낸 함수 주소라는 뜻이다.
  - `MOV RAX, [메모리]` 꼴일 때만 적는다. 레지스터끼리 옮김·XOR·LEA 등은 `cfg:?`.
  - 거슬러 올라가다 **다음 줄로 넘어오지 않는 명령**(`JMP`·`RET` 등)을 만나면 멈추고 `cfg:?` — 그 위는 다른 갈래라서다. 이 멈춤은 이름·`stack[…]`·`@0x…` 찾기에도 같다.
  - **알려진 한계**: 호출 자리가 `Jcc` 로만 들어오는 갈래이고 그 바로 위가 우연히 `JMP` 면, 같은 길인데도 멈춰서 `?` 가 된다 (예: 지도의 `MiValidateSectionCreate +0x1f050f`, 실제 출처는 `@0xf04d40`). `?` 를 보면 `.asm` 에서 그 자리로 뛰어오는 `Jcc` 를 찾아 위를 확인한다.
  - **주의**: 대괄호 안 레지스터 값은 **그 명령 직전** 값이다. 사이에 같은 레지스터를 덮어쓴 경우가 있다 (예: `PspInitializeThunkContext +0x35b` 의 `cfg:[RAX+0x8]` — RAX 가 `+0x33f` 에서 이미 바뀜). 무슨 칸인지는 `.asm` 에서 거슬러 확인한다.
- **같은 함수의 떨어진 범위**: Ghidra 가 한 함수 본체를 멀리 떨어진 여러 범위로 잡으면(.pdata 체인 등), `.asm` 에 `; --- 같은 함수의 떨어진 범위 …` 머리가 붙고 오프셋은 아주 커진다 (예: `KiSchedulerApc +0x4106e4`). 함수 일부가 맞지만 WinDbg 가 같은 `함수+0x..` 로 보여 줄지는 확인 전 `[추측]` — 이 줄을 그림 라벨로 쓰지 않는다 (묶음 N1).
- **주인 없는 덩어리**: 함수가 `JMP`/`Jcc` 로 넘어가는 곳이 **어느 함수에도 안 속한 코드**면 (Ghidra 가 함수 범위에 못 넣은 것), 스크립트가 거기서부터 흐름을 따라가 그 함수 몫으로 읽는다.
  - `_calls.tsv` 오프셋은 **본래 함수 시작 기준 `+0x..`** 그대로 (조각처럼 `frag:` 를 붙이지 않는다). `.asm` 에는 `; --- 주인 없는 덩어리 <시작>~<끝> ...` 머리 아래에 붙는다.
  - 예: `PspAllocateThread +0x849 ~ +0xbba` 안의 `PspWow64InitThread` 등 (sast/06). 고치기 전 장부엔 빠져 있었다. 커널에 이런 덩어리가 1225개(호출 576곳) 있다.
  - Ghidra 디컴파일(`.c`) 맨 위 `// callees:` 줄은 Ghidra 기준이라 여전히 이 호출들을 빠뜨릴 수 있다. 호출 목록은 `_calls.tsv` 를 믿는다.
- `_calls.tsv` 는 export 할 때마다 **덧붙여 쌓인다** (`sort -u` 로 같은 줄만 합침). 스크립트를 바꾼 뒤엔 옛 줄이 남으니, 그 모듈의 `_calls.tsv` 를 지우고 필요한 함수를 다시 export 한다.

## `.c`(디컴파일) 읽을 때 주의 (묶음 N3, 2026-10-22)

- **흐름은 `.asm` 으로 확인한다.** `.c` 는 Ghidra 가 다시 짠 글이라 아래처럼 틀어질 수 있다.
- 자기 조각(`FUN_`)으로 가는 점프가 `.c` 에선 `FUN_xxx(); return;` 처럼 **호출하고 끝나는 모양**으로 보일 수 있다. 실제로는 조각을 거쳐 본체로 돌아오기도 한다 (예: `MmCreatePeb +0x6b4`, sast/04). `.asm` 의 `FRAGJMP` 와 조각 끝의 `JMP` 를 본다.
- `*(… + -8) = 0x140…` 처럼 **스택에 주소를 쓰는 줄**은 대개 call 의 돌아올 주소(함수 시작 + ret_off)를 Ghidra 가 스택 쓰기로 그린 것이다 (예: `PspAllocateProcess.c`, sast/04). 진짜 변수 저장으로 읽지 않는다.
- 스택 자리를 잘못 읽어 인자·지역 변수가 엉킨 곳도 있었다 (sast/07). 인자 위치가 중요하면 `.asm` 에서 레지스터·`[RSP+..]` 로 다시 센다.
- 맨 위 `// callees:` 줄은 Ghidra 기준이라 주인 없는 덩어리 안 호출이 빠질 수 있다. 호출 목록은 `_calls.tsv` 를 믿는다.
- **`.c` 에 아예 안 나오는 명령**: `MOV CR8,…`(IRQL 바꾸기), `SWAPGS`(커널·유저 GS 바꾸기), `IRETQ`/`SYSRETQ`(유저로 넘어가기), `CLI`/`STI`. 커널 ↔ 유저 경계나 IRQL 이 중요한 함수(예: `KxStartUserThread`, `KiKernelExit`, sast/10)는 반드시 `.asm` 으로 본다 (묶음 O3).

## 지도 (`run_ghidra.sh map`, `CallGraphMap.java`)

`ms/disassemble/<모듈>/_map/<뿌리>_*` — 뿌리에서 정적 호출 관계를 따라간 **색인**. 디컴파일 없음.

### `_edges.tsv`
```
# depth	caller	call_off	ret_off	kind	callee	note
1	NtCreateUserProcess	+0xc31	+0xc36	call	PspAllocateProcess
```
- `kind`: `call` / `tail` (꼬리호출) / `indirect` / `indirect-cfg` / `cfg-check` / `external`. 오프셋 표기(`frag:`, `tail`)와 별칭 경고는 `_calls.tsv` 와 같은 규칙.
- `note` 의 `(seen)` = 이미 지도에 있는 함수라 다시 펼치지 않음.

### `_nodes.tsv`
```
# name	rva	size	depth	callers_total	status	reason
PspAllocateProcess	0xa02568	10186	1	3	expanded
```
- `depth` = 뿌리에서 처음 닿은 단계 (넓이 우선이라 **가장 짧은 길** 기준. 실행 순서가 아니다).
- `callers_total` = 프로그램 전체에서 이 함수를 부르는 함수 수 (CALL 참조만, 함수 포인터는 못 셈).
- `status`: `expanded` (펼침) / `helper` (공용 도구: caller 가 기준 이상이라 안 펼침 — **진짜 끊김 아님, 지도를 줄이려는 선택**) / `syscall` (SYSCALL 이 있어 커널로 넘어감) / `depth` (최대 깊이).
- 끊긴 끝(간접·외부 등)도 `name` 칸에 `kind:label` 로 한 줄씩.

### 읽을 때 주의
- 정적 지도는 **"부를 수도 있는 것"**이다. 갈림길에서 안 타는 호출이 섞여 있고, 순서는 실행 순서가 아니다.
- 호출이 아닌 길(새 스레드 출발, `NtContinue` 로 실행이 넘어감, 함수 주소를 값으로 넘겨 나중에 부름)은 지도에 없다. 예: `RtlUserThreadStart`, `ImageTlsCallbackCaller` 는 `LdrInitializeThunk` 지도에 없다 (sast/05).
- 공용 도구 기준을 너무 낮추면 대표 함수가 잘린다 (기준 10 이면 `IopCreateFile` 이 잘림, sast/05). 기본 20.

## 누가 읽고 쓰나 (`run_ghidra.sh refs`, `RefsTo.java`)

`ms/disassemble/<모듈>/_refs/<이름>.tsv` — 한 주소(전역 변수·함수)를 가리키는 곳 (묶음 O2, sast/11).

```
# PsNtdllExports  rva=0xfc74b0  len=0x98
# kind	from_rva	where	ref_type	target	text
ref	0x910889	PspInitializeThunkContext+0x1a1	READ	+0x8	MOV R8,qword ptr [0x140fc74b8]
ptr	0x...	NtdllExports+0x10 (함수 밖)	(8바이트 값)	+0x8
```
- `ref` = Ghidra 참조 (코드의 읽기·쓰기·부르기, 데이터 포인터). `ptr` = 메모리 안 8바이트 값이 그 범위를 가리키는 곳 (Ghidra 가 참조로 안 만든 표 — 예: 부팅 때 쓰는 INIT 표). 위 숫자는 모양 예시다.
- `target` = 범위 안 몇 번째 칸(+0x..)을 가리키나. 구조체면 칸 이름은 PDB 타입으로 맞춘다.
- 바이트 수를 안 주면 Ghidra 데이터 길이(없으면 8). 구조체 전체를 보려면 크기를 준다.
- 한계: 실행 중에 더해서 만드는 주소(레지스터+오프셋)는 못 찾는다 → 동적.

## 결과서 양식: `_workspace/sast/NN_키워드_result.md`

```markdown
# NN 키워드 — 정적 분석 결과

- 요청서: _workspace/sast/NN_키워드.md
- 모듈 / 바이너리: ntdll (ms/bin/.../System32/ntdll.dll), pdb guid=...
- 뽑은 함수: N개 (정규식 `...`, depth d) → ms/decomp/ntdll/, ms/disassemble/ntdll/
- 버전 확인: LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse 일치

## 질문별 답 [정적]
1. <요청서 질문 1>
   - 답: ...
   - 근거: `ms/disassemble/ntdll/X.asm` +0x.. 줄, `ms/decomp/ntdll/X.c` 의 해당 부분 요약
2. ...

## 동적분석으로 확인해야 할 것
- (정적으로 결론 못 낸 것: 간접 호출 대상, 분기 중 실제로 탄 쪽, 런타임 값 등)
- 추천 멈춤점: `ntdll!X` (+ 이유)

## 판정
분석성공 / 부분성공 / 실패 중 하나 + 한 줄 이유
```

- 디컴파일 코드를 결과서에 **길게 붙이지 않는다.** 파일 경로와 몇 줄 요약만. (MS 코드라 레포에 남기면 안 됨)
- 사실은 우리 말로 쓴다: "A는 +0x1bb6에서 B를 부른다", "B 안에서 C를 부르기 전에 D를 부른다".

## 저장소 규칙

- `ms/bin`, `ms/sym`, `ms/ghidra`, `ms/decomp`, `ms/disassemble` 은 MS 바이너리·심볼·그 파생물이라 **커밋하지 않는다** (`.gitignore`).
- `_workspace/sast/*_result.md` 는 우리 말로 쓴 요약이라 커밋해도 된다.
