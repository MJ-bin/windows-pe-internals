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
