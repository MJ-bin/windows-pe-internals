# 04 allocate-process — 정적 분석 요청

- 날짜: 2026-10-19
- 스프린트: 02_sprint
- 대기열: **Q2** — 2.1 `nt!PspAllocateProcess`, `nt!MmCreatePeb` callee (`_workspace/plan.md`)
- 관련: `runs/02_sprint/flow.md` 그림 1, 2.1 (`PspAllocateProcess`·`MmCreatePeb` 절은 지금 `[관찰]` 호출 관계 + `[추측]` 역할뿐) / 이전 요청 sast/03 (run9)

## 알고 싶은 것

1. `PspAllocateProcess` 가 **직접 부르는 함수** 목록 (call 순서). 01_sprint 관찰 `+0x2314` → `MmCreatePeb` 가 정적으로도 같은가? (예/아니오, call_off·ret_off)
2. `PspAllocateProcess` 는 10186바이트로 크다. 호출을 **하는 일 단위로** 묶어서, 단위마다 흐름도에 넣을 대표 호출을 1~3개 골라라 (이름, ret_off, 디컴파일로 본 역할 한 줄). 나머지는 "잡일"로 개수만.
   - 특히 아래 일이 이 함수 안에서 일어나는지, 일어나면 어느 호출인지 (예/아니오 + 이름):
     - (가) 새 프로세스의 **주소 공간(메모리 공간) 만들기**
     - (나) **exe 를 새 프로세스 메모리에 올리기** (sast/03 에서 `NtCreateUserProcess` 가 `+0x93c` `MmCreateSpecialImageSection` 으로 만든 이미지 섹션을 쓰는 곳)
     - (다) **ntdll 을 새 프로세스 메모리에 올리기**
     - (라) **PEB 만들기** (`MmCreatePeb`)
   - 일의 순서가 (가) → (나) → (다) → (라) 인지, 다르면 실제 순서.
3. points.md `[확인 필요]` 이름 중 이 단계 것: `MmInitializeProcessAddressSpace`, `PspMapSystemDll` 을 **누가 부르나?**
   - 이 두 함수를 depth 0 으로 뽑아 `.c` 머리말의 `callers:` 줄만 본다 (본문 분석은 안 해도 됨).
   - `PspAllocateProcess` 가 직접 부르면 그 ret_off, 아니면 실제 caller 이름.
   - 참고: `_index.tsv` 에 `MmCreateProcessAddressSpace`(692바이트)도 있다. (가) 후보일 수 있으니 목록에 있으면 짚어 줘.
4. `MmCreatePeb` 가 직접 부르는 함수 목록, 그리고 PEB 에 채우는 것 중 **ntdll 이 나중에 읽는 것**(exe 주소 `ImageBaseAddress`, 프로세스 매개변수, 버전 정보 등)을 디컴파일로 본 만큼 우리 말로 3~5줄. 필드 오프셋은 Ghidra 가 보여 준 그대로 적되 `[정적]` 으로만.
5. 간접 호출(`indirect:`, `indirect:cfg:`), 꼬리호출(`tail`), 별칭 경고(`alias:`), 조각(`frag:`) 이 나오면 목록에 그대로 두고 아래 규칙대로 다룬다.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^(PspAllocateProcess\|MmCreatePeb)$` | 0 | Q2 본체 |
| ntoskrnl | `^(MmInitializeProcessAddressSpace\|PspMapSystemDll)$` | 0 | 질문 3: `callers:` 줄만 |

- 한 번에 정규식 하나로 뽑아도 된다: `^(PspAllocateProcess|MmCreatePeb|MmInitializeProcessAddressSpace|PspMapSystemDll)$`
- **depth 0.** callee 본문은 뽑지 않는다.
- ntoskrnl analyze 는 끝나 있다. 다시 analyze 하지 않는다.
- `_calls.tsv` 는 쌓이는 파일이다. 지금 ntoskrnl `_calls.tsv` 에는 sast/03 의 `NtCreateUserProcess`·`PspInsertThread` 줄(묶음 F 반영본)이 있다. 지우지 말고 그대로 이어서 export 한다.

## 스크립트 표기 (2026-10-19 묶음 B·D-1·F 반영본, `reference.md`)

- `ret_off=tail` = 꼬리호출. `INT` 는 표에서 빠지고 `.asm` 에 `; TRAP`.
- `indirect:cfg:<출처>` = CFG 검사를 거친 간접 호출 (진짜 대상은 RAX, 동적으로만 앎).
- `alias:<이름>+N` = 같은 주소에 다른 PDB 이름 N개 (ICF 등). **이 이름은 대표 지점으로 고르지 말고, 고를 수밖에 없으면 `.asm` 의 `ALIASES` 줄과 코드 모양(크기·내용)으로 무슨 함수인지 따로 적어라.**
- `frag:<FUN_이름>+0x..` = 함수에서 떨어진 조각 안의 호출. **조각이 나오면 진짜 조각인지(같은 레지스터·스택 자리를 쓰고 본체로 돌아오는지) 한 줄로 확인해 적어라.** 엉뚱한 함수를 조각으로 잡은 것 같으면 "스크립트 문제" 절에.
- `run_ghidra.sh` 종료코드 4 = 로그에 SCRIPT ERROR (결과를 쓰지 말 것).

## 이미 아는 것

- `[관찰]` (run3) `NtCreateUserProcess+0xc36` → `PspAllocateProcess`, `PspAllocateProcess+0x2314` → `MmCreatePeb` (부모 cmd.exe 스레드).
- `[정적]` (run9) `NtCreateUserProcess` 는 `PspAllocateProcess` 전에 `IopCreateFile`(+0x6cb, exe 열기)·`MmCreateSpecialImageSection`(+0x93c, 이미지 섹션 만들기)을 부른다. points.md 이름 4개(`MiCreateImageFileMap`, `MmInitializeProcessAddressSpace`, `PspMapSystemDll`, `MmCreateTeb`)는 `NtCreateUserProcess` 가 직접 부르지 않는다.
- `[추측]` (flow.md 2.5) 커널이 프로세스를 만들 때 올려 주는 건 exe 와 ntdll 뿐이다. 이번 질문 2 (나)(다)로 정적 근거를 찾는다.

## 받고 싶은 것

- 질문 1~5 답 + 근거 (`ms/disassemble/ntoskrnl/PspAllocateProcess.asm`, `MmCreatePeb.asm` 의 `+0x` 줄, `.c` 요약). 대표 지점 오프셋은 **ret_off** 로 적고 call_off 는 괄호에.
- 동적으로 확인할 것 + 추천 멈춤점 (있으면).
- 결과서: `_workspace/sast/04_allocate-process_result.md`
