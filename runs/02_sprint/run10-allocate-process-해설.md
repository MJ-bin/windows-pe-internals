# run10 allocate-process — 해설

> 이 글은 [run10 문서](run10-allocate-process.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q2** — 2.1 `nt!PspAllocateProcess`, `nt!MmCreatePeb` callee (`_workspace/plan.md`)
  - "callee" = 이 두 함수가 **직접 부르는 함수 목록**이라는 뜻이다.
- 근거:
  - run 문서 [`run10-allocate-process.md`](run10-allocate-process.md) (가장 중요)
  - 요청서 [`_workspace/sast/04_allocate-process.md`](../../_workspace/sast/04_allocate-process.md)
  - 결과서 [`_workspace/sast/04_allocate-process_result.md`](../../_workspace/sast/04_allocate-process_result.md) (판정: **분석성공**)
  - flow.md 변경: run10 커밋 `69a65eb`
  - 바퀴 전 관찰: [`runs/01_sprint/run3-create.md`](../01_sprint/run3-create.md)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다.

이 글은 [run9 해설](run9-create-user-process-해설.md) 다음 편이다.

- run9 해설에서 푼 말 17개는 **링크로만** 가리킨다.
- 비유도 거기서 이어 쓴다.
  - 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**
  - 이미지 섹션 = 미리 짜 둔 **조립식 건물 세트**
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**

한 줄로 먼저 말하면:

- 관리사무소(커널)가 새 가게(test.exe)의 **빈 땅**을 마련하고,
- 그 땅에 **test.exe 건물**과 **관리실(ntdll) 건물**을 갖다 놓고,
- 가게 문 앞에 **안내판(PEB)** 을 다는 순서를, 코드에서 확인한 바퀴다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (유저 쪽): "새 프로그램 띄워 줘"
     └─ 시스템 콜로 커널에 부탁
         └─ nt!NtCreateUserProcess                    (run9, Q1)
              ├─ +0x6cb   설계도(exe 파일) 꺼내기
              ├─ +0x93c   조립식 세트(이미지 섹션) 준비
              ├─ +0xc36   nt!PspAllocateProcess       ← 이번 바퀴 (Q2)
              │             새 가게 차리기: 땅 → 건물 → 관리실 → 안내판
              ├─ +0xe5c   PspAllocateThread           (첫 직원 뽑기)
              └─ …        등록, 손잡이 건네기
 ─ ─ (2.2 공백: 첫 직원은 언제 일을 시작하나?) ─ ─
 test.exe 첫 스레드 출발 (커널)
  └─ ntdll!LdrInitializeThunk  (유저 쪽 첫 자리)
      └─ ntdll 로더: dll 올리기, TLS 콜백, DllMain
          └─ main
```

- 이번 구간에서도 일하는 건 **부모 cmd.exe 의 스레드 하나**다 `[관찰]` (run3).
- run9 는 `NtCreateUserProcess` 라는 담당자가 하는 일의 **목록**을 봤다.
- run10 은 그 목록의 한 줄, `+0xc36` `PspAllocateProcess` **상자 안을 열어 본** 바퀴다.
  - 돋보기를 한 단계 더 들이댄 것이라고 보면 된다.

### 무엇이 궁금했나

1. `PspAllocateProcess` 가 직접 부르는 함수를 다 보고, **하는 일 단위로 묶고** 싶다.
2. 그 안에서 아래 4가지 일이 일어나는지, **어떤 순서인지** 보고 싶다.
   - (가) 새 프로세스의 주소 공간(메모리 공간) 만들기
   - (나) exe 를 새 프로세스 메모리에 올리기
   - (다) ntdll 을 새 프로세스 메모리에 올리기
   - (라) PEB 만들기
3. 01_sprint 에서 본 `+0x2314` → `MmCreatePeb` 가 **코드에서도 같은 자리**인지.
4. points.md 에 `[확인 필요]` 로 적어 둔 `MmInitializeProcessAddressSpace`, `PspMapSystemDll` 을 **누가 부르나**.
   - run9 에서는 "`NtCreateUserProcess` 가 직접 부르진 않는다" 까지만 알았다.
5. `MmCreatePeb` 가 PEB 에 **무엇을 쓰나** (요청서 질문 4).

### 바퀴 전에 알던 것

- `[관찰]` (run3) `NtCreateUserProcess +0xc36` → `PspAllocateProcess`, 그 안 `+0x2314` → `MmCreatePeb`.
  - `PspAllocateProcess` 안에서 아는 호출은 **이 하나뿐**이었다.
- `[정적]` (run9) `+0xc36` 은 코드에서도 같은 자리다 (call `+0xc31`).
- `[정적]` (run9) 그 앞 `+0x93c` `MmCreateSpecialImageSection` 이 exe 의 [이미지 섹션](run9-create-user-process-해설.md#이미지-섹션)(조립식 세트)을 만든다.
- `[정적]` (run9) points.md 이름 `MiCreateImageFileMap`, `MmInitializeProcessAddressSpace`, `PspMapSystemDll`, `MmCreateTeb` 는 이 빌드에 있지만, `NtCreateUserProcess` 가 **직접** 부르지는 않는다.
- `[추측]` (run10 전 flow.md 2.1)
  - `PspAllocateProcess` = 새 프로세스의 정보 묶음(`_EPROCESS`)과 메모리 공간 준비. "가게 건물 짓기".
  - `MmCreatePeb` = 유저 쪽(ntdll)이 나중에 읽을 프로세스 정보판(PEB)을 만든다.
- `[추측]` (flow.md 2.5) 커널이 프로세스를 만들 때 올려 주는 건 **exe 와 ntdll 뿐**이라고 알려져 있다.
- `[관찰]` (run4, flow.md 2.3·2.4) test.exe 의 새 스레드가 유저 쪽에서 **처음 도착하는 곳은 `ntdll!LdrInitializeThunk`** 다.

---

## 2. 바탕 개념

> 이 절의 "뜻"은 **일반적으로 알려진 설명**이다 `[추측]` (이 프로젝트에서 직접 확인한 게 아니다).
> 이 프로젝트에서 본 사실에는 따로 태그를 붙였다. 비유는 설명일 뿐이라 태그가 없다.

이번에 새로 푸는 말은 8개다. 세 묶음으로 나눴다.

- **땅과 건물**: 주소 공간 / 껍데기와 장부 / 매핑 올리기 / 남의 땅에 잠깐 들어가기
- **새 가게에 꼭 있어야 하는 것**: ntdll 이 먼저 와 있어야 하는 이유 / PEB / 구조체 안의 칸 오프셋
- **갈림길**: 보통 길과 복제 길

run9 해설에서 이미 푼 말 (다시 풀지 않음):

- [커널과 유저](run9-create-user-process-해설.md#커널과-유저) — 커널 = 관리사무소, 유저 = 각 가게
- [시스템 콜](run9-create-user-process-해설.md#시스템-콜) — 관리사무소의 신청 창구
- [프로세스](run9-create-user-process-해설.md#프로세스) — 가게. 주소 공간 = 빈 땅, 매핑 = 건물 갖다 놓기, PEB = 안내판 (이번에 이 셋을 더 자세히 푼다)
- [스레드](run9-create-user-process-해설.md#스레드) — 가게의 직원
- [함수와 주소](run9-create-user-process-해설.md#함수와-주소) — 요리책의 레시피와 쪽수
- [오프셋](run9-create-user-process-해설.md#오프셋) — 레시피 시작부터 몇째 줄
- [부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리) — call 과 ret, 책갈피
- [콜스택](run9-create-user-process-해설.md#콜스택) — 꽂혀 있는 책갈피 더미
- [정적분석과 동적분석](run9-create-user-process-해설.md#정적분석과-동적분석) — 노선도 읽기 / 버스 타 보기
- [표시 태그](run9-create-user-process-해설.md#표시-태그) — `[관찰]` `[정적]` `[추측]` `[공백]` `[소스]`
- [그림의 점선 정적 상자](run9-create-user-process-해설.md#그림의-점선-정적-상자) — 지도에만 있는 길
- [직접 호출과 간접 호출](run9-create-user-process-해설.md#직접-호출과-간접-호출) — "35쪽 보세요" / "냉장고 메모 보세요"
- [비트와 플래그](run9-create-user-process-해설.md#비트와-플래그) — 신청서의 체크 칸
- [스레드를 멈춘 채로 만들기](run9-create-user-process-해설.md#스레드를-멈춘-채로-만들기) — 직원 목의 "대기" 팻말
- [핸들](run9-create-user-process-해설.md#핸들) — 손잡이(번호표)
- [이미지 섹션](run9-create-user-process-해설.md#이미지-섹션) — 조립식 건물 세트
- [한 주소에 이름 여럿 ICF 별칭](run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭) — 문패 하나에 여러 가족

---

**땅과 건물**

### 주소 공간

- 뜻: 프로세스 하나가 쓰는 **메모리 번지의 범위**. 프로세스마다 따로 있다.
  - 같은 번지 숫자라도, A 프로세스에서와 B 프로세스에서 가리키는 내용이 다르다.
- 비유: 가게마다 받은 **빈 땅** ([프로세스](run9-create-user-process-해설.md#프로세스) 비유를 잇는다).
  - 땅마다 "1번지, 2번지 …" 번지수가 있다.
  - 가게 A 의 1번지와 가게 B 의 1번지는 **다른 땅**이다.
- 이 프로젝트에서:
  - `[정적]` (run10) 새 test.exe 의 땅은 `PspAllocateProcess +0x163c` 의 `MmCreateProcessAddressSpace` 에서 마련된다. 4가지 일 중 (가).
  - `[정적]` 코드 순서로는 이게 (나)(다)(라)보다 **먼저**다.
  - `[추측]` 그러니 이 함수 안의 순서로 보면, 이때 땅은 비어 있다. exe 도 ntdll 도 아직 안 놓였다 (코드 순서에서 끌어낸 짐작).

### 껍데기와 장부

빈 땅을 쓰려면, 땅 위에 뭘 놓기 전에 **서류 두 가지**가 먼저 있어야 한다.

- 뜻:
  - **페이지 표** = "이 번지 = 실제 메모리(RAM)의 어디" 를 적은 대응표. 맨 위 장이 있어야 나머지를 이어 붙일 수 있다.
  - **VAD** = "몇 번지부터 몇 번지까지는 무엇이 쓰고 있다" 를 적은 구역 장부. flow.md 2.1 은 "메모리 구역 장부" 라고 부른다.
  - 작업 집합 = 이 프로세스가 지금 실제 메모리에 들고 있는 페이지 목록.
- 비유:
  - 페이지 표 = 땅의 **지적도**. 번지수와 실제 위치를 잇는다. 맨 위 페이지 표 = 지적도의 첫 장.
  - VAD = 땅의 **구획 장부**. "100~200번지: test.exe 건물 자리" 같은 줄이 적힌다.
- 이 프로젝트에서 이 두 가지를 **다른 함수가** 만든다:
  - `[정적]` (run10) `MmCreateProcessAddressSpace` (가) 안:
    - `MiAllocateTopLevelPage` `+0x1fd` → 맨 위 페이지 표
    - `MiCreateNewProcessTopLevelMappings` `+0x208` → 커널 쪽 매핑 잇기
    - `PsJoinSession` `+0x21f` → 세션에 넣기
  - `[정적]` (run10) `MmInitializeProcessAddressSpace` 안 (돌아올 자리 순서):
    - 작업 집합 장부(`+0xed`) → 유저 주소 범위 계산(`+0x13b`) → VAD 만들고 넣기(`+0x161`, `+0x1a8`) → **그다음에** exe·ntdll 올리기(`+0x1ce`)
  - `[추측]` (flow.md 2.1) 그래서 (가) 는 새 프로세스 메모리의 **빈 껍데기**다. 유저 쪽 장부와 내용물은 다음 함수 몫이다.
- 비유로 정리: 관리사무소가 먼저 **지적도 첫 장**만 만들어 두고(가), 나중에 **구획 장부를 쓰고 건물을 놓는다**(나)(다).

### 매핑 올리기

- 뜻: 준비해 둔 섹션을 주소 공간의 어느 번지 범위에 **보이게 붙이는** 일.
  - 붙인 그 범위를 **뷰(view)** 라고 부른다.
  - 파일 내용을 통째로 복사하는 게 아니라, "이 번지를 읽으면 저 섹션 내용이 보이게" **연결**하는 것이다.
  - 이 프로젝트 문서에서는 "**올리기**" 라고 부른다.
- 비유: [이미지 섹션](run9-create-user-process-해설.md#이미지-섹션)은 미리 짜 둔 **조립식 건물 세트**였다. 매핑 = 그 세트를 빈 땅 몇 번지에 **갖다 놓기**.
- 함수 이름 읽기: `MmMapViewOfSectionEx`
  - `Mm` = 메모리 관리 쪽 함수라는 머리말
  - `MapViewOfSection` = "섹션의 뷰를 (주소 공간에) 붙인다"
  - `Ex` = 확장판
- 이 프로젝트에서 `[정적]` (run10):
  - exe 올리기: `MiMapProcessExecutable +0xb8` → `MmMapViewOfSectionEx`
  - ntdll 올리기: `PspMapSystemDll +0x11d` → `MmMapViewOfSectionEx`
  - **같은 함수로 둘 다 올린다.** 다른 건 "어떤 세트를 갖다 놓나" 뿐이다.
  - 그래서 flow.md 그림 1 에 `MmMapViewOfSectionEx` 상자가 **두 개** 있다 (exe 용, ntdll 용). 같은 함수지만 부르는 자리가 다르니 상자를 둘로 뒀다 (plan.md 결정 기록).

### 남의 땅에 잠깐 들어가기

- 뜻: 커널 코드는 평소 "지금 돌고 있는 스레드의 프로세스" 주소 공간을 본다. **다른 프로세스**의 유저 쪽 메모리를 만지려면, 잠깐 그 프로세스의 주소 공간으로 **들어갔다가 나와야** 한다.
- 비유: 관리사무소 직원이 자기 사무실에 앉아서는 남의 땅에 건물을 못 놓는다. 새 가게 땅 안으로 **들어가서** 일하고 **나온다**.
- 왜 이게 필요하나:
  - `[관찰]` (run3) 이번 구간의 일꾼은 **cmd.exe 의 스레드**다.
  - 그런데 건물은 **test.exe 의 땅**에 놓아야 한다.
  - `[추측]` 그 차이를 메우는 게 이 "들어가기·나오기" 다.
- 이 프로젝트에서 `[정적]` (run10):
  - 들어가기 = `KiStackAttachProcess`, 나오기 = `KiUnstackDetachProcess`
  - `MmInitializeProcessAddressSpace`: `+0xb5` 들어감 → … → `+0x1e5` 나옴. exe·ntdll 올리기(`+0x1ce`)가 **그 사이**에 있다.
  - `MmCreatePeb`: `+0xc7` 들어감 → … → `+0x5b5` 등에서 나옴.
  - `PspSetupUserProcessAddressSpace`: `+0xfa` 들어감 → … → `+0x26f` 나옴.
  - 새 땅에 뭔가를 놓는 세 함수가 **모두** 이 틀 안에서 일한다.

---

**새 가게에 꼭 있어야 하는 것**

### ntdll 이 먼저 와 있어야 하는 이유

- ntdll 이 뭔가: 모든 유저 프로세스에 들어가는 Windows 기본 DLL. 로더(dll 올리기·켜기)와 시스템 콜 입구가 여기 있다.
- 비유: 가게 안의 **관리실**.
  - 관리사무소(커널)와 헷갈리면 안 된다.
  - 관리사무소 = 상가 전체에 **하나**, 커널 안.
  - 관리실 = **가게마다 하나씩**, 가게 땅 안(유저 쪽)에 있다. 가게가 문을 열면 관리실 직원이 나머지 건물(kernel32, dll1, dll2)을 들여오고 정리한다.
  - flow.md 2.1 의 비유 "관리실 들이기(ntdll)" 와 같은 그림이다.

왜 커널이 ntdll 을 **미리** 올려 둬야 하는지, 근거를 하나씩 쌓아 보자.

1. `[관찰]` (run4, flow.md 2.3·2.4) test.exe 의 새 스레드가 유저 쪽에서 **처음 도착하는 곳은 `ntdll!LdrInitializeThunk`** 다. test.exe 자기 코드가 아니다.
2. `[관찰]` (flow.md 2.4) 그 뒤 kernel32 준비(`+0x19a2`), import DLL 초기화(`+0x1bb6`)를 ntdll 의 `LdrpInitializeProcess` 가 한다.
3. 그래서 → 첫 직원이 유저 쪽으로 넘어가기 **전에** ntdll 이 이미 새 땅에 있어야 앞뒤가 맞는다. 유저 쪽엔 아직 아무도 없으니, 올려 줄 수 있는 건 커널뿐이다. `[추측]` (관찰에서 끌어낸 추론. ntdll 이 올라오는 순간 자체는 아직 디버거로 안 봤다)
4. `[정적]` (run10) 실제로 커널 코드에 ntdll 을 올리는 줄이 있다: `PspMapSystemDll +0x11d` → `MmMapViewOfSectionEx`.

- 반대로 kernel32·kernelbase 는 커널이 아니라 ntdll 이 나중에 올린다고 알려져 있다 `[추측]` (flow.md 2.5).
- 비유로: 관리사무소는 **test.exe 건물과 관리실까지만** 놓아 준다. 나머지 건물은 관리실 직원이 출근해서 들여온다.

### PEB

- 뜻: Process Environment Block. 새 프로세스의 **유저 쪽 메모리 안**에 놓이는 정보 묶음.
  - exe 가 몇 번지에 올라왔는지, OS 버전, CPU 개수 같은 걸 적어 둔다.
  - 유저 쪽 코드(특히 ntdll)가 읽는다.
- 비유: 가게 문 앞 **안내판** ([프로세스](run9-create-user-process-해설.md#프로세스) 비유를 잇는다).
  - 관리실(ntdll) 직원이 출근하자마자 이걸 보고 "우리 건물이 몇 번지에 있지?" 를 안다.
- 커널 쪽 장부와 헷갈리지 않기:
  - 관리사무소에도 가게마다 **장부**가 있다 = `_EPROCESS` (flow.md 2.1 의 "프로세스 정보 묶음"). 커널만 본다.
  - PEB 는 가게 안에 걸어 둔 **안내판**이다. 가게 쪽(유저)이 본다.
  - run 문서의 "**새 프로세스** `+0x2b0`", "`+0x2e0`" 은 **장부 쪽 칸**이고, "**PEB** `+0x10`" 은 **안내판 쪽 칸**이다.
- 이 프로젝트에서:
  - `[관찰]`+`[정적]` `PspAllocateProcess +0x2314` → `MmCreatePeb`.
  - `[정적]` (run10) PEB 주소는 새 프로세스(장부) `+0x2e0` 칸에 적어 둔다 (호출부 `+0x22f2` 에서 그 자리를 인자로 줌).
  - `[정적]` (결과서, sast/02 때 뽑은 `.c`) ntdll 의 `LdrpInitializeProcess` 가 PEB `+0x10`, `+0x20` 등을 읽는다.

### 구조체 안의 칸 오프셋

- 뜻: `PEB +0x10` 의 `+0x10` 은 **묶음(구조체) 시작에서 몇 바이트째 칸**이라는 뜻이다.
  - [오프셋](run9-create-user-process-해설.md#오프셋)에서 본 `PspAllocateProcess+0x2314` 와 같은 생각이다 ("시작에서 몇 바이트").
  - 다른 점은 대상이다. 함수 오프셋은 **코드 줄**을, 구조체 오프셋은 **데이터 칸**을 가리킨다.
- 비유: 안내판에 칸이 줄지어 있다. `+0x0` 칸, `+0x8` 칸, `+0x10` 칸 …
  - "몇째 칸인지" 는 확실히 보인다.
  - 그런데 칸마다 **이름표는 안 붙어 있다.**
- 그래서 이번 바퀴의 PEB 표는 태그가 **둘로 갈린다**:
  - 오프셋 `[정적]`: 디스어셈블에서 "PEB 시작 + `0x10` 에 이 값을 쓴다" 를 **그대로 본** 것.
  - 이름 `[추측]`: "`+0x10` = ImageBaseAddress" 같은 이름은 **일반적으로 알려진 PEB 배치에 맞춰 본** 것이다 (run 문서).
  - 구조체 오프셋은 Windows 버전마다 다를 수 있어서(CLAUDE.md), 이름은 확인 전까지 `[추측]` 으로 둔다.

---

**갈림길**

### 보통 길과 복제 길

- 뜻:
  - **보통 길** = exe 파일(이미지 섹션)을 바탕으로 새 프로세스를 짓는다.
  - **복제 길** = **부모 프로세스를 바탕으로** 새 프로세스를 짓는다.
    - Windows 에는 부모의 주소 공간을 본떠 자식을 만드는 방식이 있다고 알려져 있다 (흔히 "프로세스 복제"). cmd.exe 가 test.exe 를 실행하는 건 이 경우가 아니라고 보는 게 자연스럽다 (아래 3.9 에서 근거를 쌓는다).
- 비유:
  - 보통 길 = 설계도(exe)와 조립식 세트로 **새 건물**을 짓는다. 안내판도 **새로** 만든다.
  - 복제 길 = 옆 가게(부모)를 **통째로 베껴** 짓는다. 조립식 세트가 필요 없고, 안내판도 옆 가게 걸 베낀다.
- 이 프로젝트에서 `[정적]` (run10):
  - 두 길은 `PspAllocateProcess` 안의 **같은 체크 칸(bit `0x4`)** 으로 갈린다 ([비트와 플래그](run9-create-user-process-해설.md#비트와-플래그)).
  - 보통 길: `+0x2095` `MmInitializeProcessAddressSpace` — 부모 없음, 섹션 있음
  - 복제 길: `+0x2113` `MmInitializeProcessAddressSpace` — 부모 있음, 섹션 없음
  - 복제 갈래(13줄)는 `MmCreatePeb` 대신 **부모 PEB 포인터를 복사**하고, 뒷일(들어가기, ApiSet, silo, ntdll 시작 정보)도 따로 한다 (결과서, `+0x23d7` ~ `+0x260d`).

---

## 3. 찾은 것

### 3.1 한눈에 — 큰 줄기

run 문서의 "호출 관계" 그대로다.

```
PspAllocateProcess                                   (번호 = 코드 순서)
 ├─ (1) +0x163c  MmCreateProcessAddressSpace          [정적] (가) 주소 공간 껍데기
 ├─ (2) +0x2095  MmInitializeProcessAddressSpace      [정적] 보통 길 (+0x2113 은 복제 길)
 │                └─ +0x1ce  MiMapProcessExecutable   [정적]
 │                     ├─ (1) +0xb8  MmMapViewOfSectionEx   [정적] (나) exe 올리기
 │                     └─ (2) +0xdc  PsMapSystemDlls        [정적]
 │                          └─ +0xc0  PspMapSystemDll       [정적]
 │                               └─ +0x11d  MmMapViewOfSectionEx  [정적] (다) ntdll 올리기
 ├─ (3) +0x2314  MmCreatePeb                          [관찰] run3 + [정적] (라) PEB
 └─ (4) +0x239c  PspSetupUserProcessAddressSpace      [정적]
```

읽을 때 주의 세 가지:

- 오프셋은 모두 **돌아올 자리(ret)** 다. 디버거 콜스택에 찍히는 숫자와 같다.
- **들여쓴 줄의 오프셋은 바로 위 함수 안의 자리**다.
  - `+0xb8` 은 `PspAllocateProcess+0xb8` 이 **아니라** `MiMapProcessExecutable+0xb8` 이다.
  - `+0xc0` 은 `PsMapSystemDlls+0xc0`, `+0x11d` 는 `PspMapSystemDll+0x11d`.
- `(1)` `(2)` … 는 **같은 함수 안**에서의 코드 순서다. 그래서 `PspAllocateProcess` 아래에도 `(1)` ~ `(4)`, `MiMapProcessExecutable` 아래에도 `(1)` `(2)` 가 따로 있다.

### 3.2 한 줄씩 읽는 법

트리 한 줄을 "**누가 / 어디서 / 누구를 / 무엇을 위해**" 로 풀어 읽어 보자.

**`PspAllocateProcess` 의 (1) `+0x163c` `MmCreateProcessAddressSpace` `[정적]`**

- 누가: `PspAllocateProcess`
- 어디서: 돌아올 자리 `+0x163c` (부르는 자리 `+0x1637`)
- 누구를: `MmCreateProcessAddressSpace`
- 무엇을 위해: (가) 새 가게의 **빈 땅** 마련. 안에서 맨 위 페이지 표를 만든다 `[정적]`. "껍데기" 라는 해석은 `[추측]`.
- 태그 뜻: 노선도에만 있다. 이 함수를 부르는 곳은 `PspAllocateProcess` 하나뿐이다 `[정적]` (머리말 `callers:`).

**`MiMapProcessExecutable` 의 (1) `+0xb8` `MmMapViewOfSectionEx` `[정적]` exe 올리기**

- 누가: `MiMapProcessExecutable`
  - 이 함수는 `MmInitializeProcessAddressSpace +0x1ce` 에서, 그 함수는 `PspAllocateProcess +0x2095` 에서 불린다.
- 어디서: 돌아올 자리 `+0xb8` (부르는 자리 `+0xb3`)
- 누구를: `MmMapViewOfSectionEx`
- 무엇을 위해: exe 의 조립식 세트(이미지 섹션)를 새 땅에 **갖다 놓기**.
  - 바로 뒤 `+0xc5` 에서, 놓은 번지를 새 프로세스(장부) `+0x2b0` 칸에 적어 둔다 `[정적]`. → 3.6 에서 이어진다.
- 디버거로 보면: 콜스택에 `nt!MiMapProcessExecutable+0xb8` 로 찍힐 자리다.

**`PspMapSystemDll` 의 `+0x11d` `MmMapViewOfSectionEx` `[정적]` ntdll 올리기**

- 누가: `PspMapSystemDll` (← `PsMapSystemDlls +0xc0` ← `MiMapProcessExecutable +0xdc`)
- 어디서: 돌아올 자리 `+0x11d` (부르는 자리 `+0x118`)
- 누구를: exe 때와 **같은** `MmMapViewOfSectionEx`
- 무엇을 위해: 미리 준비된 ntdll 섹션을 잡아서(`PspReferenceSystemDll` `+0x40`) 새 땅에 갖다 놓기.
- 사슬이 길다:
  - `PspAllocateProcess` → `MmInitializeProcessAddressSpace` → `MiMapProcessExecutable` → `PsMapSystemDlls` → `PspMapSystemDll` → `MmMapViewOfSectionEx`
  - 그래서 `PspAllocateProcess` 의 직접 호출 목록만 봐서는 "exe·ntdll 을 올린다" 는 이름이 **안 보였다.** 결과서는 `callers:` 줄을 거슬러 올라가서 이 사슬을 찾았다 (그래서 요청서 밖으로 함수 3개를 더 뽑았다).

### 3.3 가게 차리기 다섯 걸음

트리를 **순서대로 한 줄**로 펴 보면 이렇다. 이번 바퀴에서 제일 중요한 그림이다.

| 걸음 | 가게 비유 | 어디서 (ret) | 부르는 함수 | 태그 |
|---|---|---|---|---|
| 1 | **빈 땅** 마련 | `PspAllocateProcess +0x163c` | `MmCreateProcessAddressSpace` | `[정적]` |
| 2 | **test.exe 건물** 갖다 놓기 | `MiMapProcessExecutable +0xb8` | `MmMapViewOfSectionEx` | `[정적]` |
| 3 | **관리실(ntdll) 건물** 갖다 놓기 | `MiMapProcessExecutable +0xdc` → `PsMapSystemDlls +0xc0` → `PspMapSystemDll +0x11d` | `MmMapViewOfSectionEx` | `[정적]` |
| 4 | **안내판(PEB)** 달기 | `PspAllocateProcess +0x2314` | `MmCreatePeb` | `[관찰]` run3 + `[정적]` |
| 5 | 유저 쪽 **마무리** | `PspAllocateProcess +0x239c` | `PspSetupUserProcessAddressSpace` | `[정적]` |

순서의 근거 `[정적]`:

- `PspAllocateProcess` 안에서 `+0x163c` < `+0x2095` < `+0x2314` < `+0x239c`.
- 결과서가 디컴파일 흐름과 맞춰 보니 이 함수는 **주소 순서 = 실행 순서**로 보인다. 예외는 실패 정리 길과 복제 갈래뿐.
- 걸음 2·3 은 `PspAllocateProcess` 가 **따로 두 번 부르는 게 아니다.** `+0x2095` 한 번 안에서 **exe 먼저(`+0xb8`), ntdll 다음(`+0xdc`)** 이다.

왜 이 순서가 그럴듯한가 (비유, 태그 없음):

- 땅이 있어야 건물을 놓는다.
- 건물이 놓여야, 안내판에 "test.exe 건물은 ○○번지" 라고 **적을 수 있다.**
- 이건 비유로만 그런 게 아니다. 실제로 걸음 2 에서 적어 둔 번지를 걸음 4 가 가져다 쓴다 `[정적]` → 3.6.

걸음 5 의 안쪽 `[정적]` (돌아올 자리 순서, 주요한 것만):

- 새 땅에 들어감(`+0xfa`) → 메모리 할당(`ZwAllocateVirtualMemory` `+0x19c`) → `PspCopyAndFixupParameters` `+0x1b9` → `PspPrepareSystemDllInitBlock` `+0x1e6` → ApiSet 뷰 올리기(`MmMapApiSetView` `+0x255`) → silo 공유 데이터 뷰(`PspMapSiloSharedDataView` `+0x263`) → 나옴(`+0x26f`)
- `[추측]` (flow.md 2.1) `PspCopyAndFixupParameters` = 프로세스 매개변수(명령줄 등)를 새 프로세스로 복사. `PspPrepareSystemDllInitBlock` = ntdll 에 넘길 시작 정보 묶음 준비. 둘 다 **이름으로 본 것**이고 본문은 안 봤다.

### 3.4 144줄 중 어디쯤인가

- `[정적]` `PspAllocateProcess` 가 직접 부르는 호출은 **144줄**이다 (서로 다른 이름 107개).
- 하는 일로 묶으면 **14단위** + 실패 정리·출구(7) + 복제 갈래(13).
- 위 다섯 걸음은 그중 **8번, 12번, 13번** 단위에 들어 있다. 나머지는 가게 서류 일이다.

| # | 하는 일 | 개수 | 가게 비유 |
|---|---|---|---|
| 1 | 새 프로세스 객체 만들기 | 9 | 가게 장부 한 권 만들기 |
| 2 | 파티션·섹션 참조 잡기 | 7 | |
| 3 | exe 이미지 정보 읽기, IFEO 레지스트리, CPU 종류 | 23 | 설계도 정보 읽기, 가게 이름별 특별 지시 확인 |
| 4 | 토큰·권한 확인 | 5 | 신청자 자격 확인 |
| 5 | NUMA 노드·프로세서 그룹 정하기 | 9 | 상가 어느 구역에 둘지 |
| 6 | 세션 검사, 다른 세션 붙기 | 7 | |
| 7 | 쿼터 연결 | 3 | 쓸 수 있는 양 한도 |
| **8** | **(가) 주소 공간 만들기** | 4 | **빈 땅** |
| 9 | 커널 프로세스 구조, 보안, 핸들 표 | 12 | |
| 10 | 완화(mitigation) 정책 | 12 | 안전 규칙 |
| 11 | 생성 알림, 전체 이미지 이름 | 6 | |
| **12** | **(나)(다) exe·ntdll 올리기** | 7 | **건물 두 채** |
| **13** | 유저 쪽 준비 + **(라) PEB** | 5 | **안내판, 마무리** |
| 14 | 마무리: ID 핸들, 생성 시각 | 15 | 가게 번호 매기기, 개업 시각 |
| — | 실패 정리·출구 | 7 | |
| — | 복제 갈래 (부모 PEB 복사 등) | 13 | |

- IFEO = exe 이름별로 실행 옵션을 적어 두는 레지스트리 자리 `[추측]` (일반적으로 알려진 설명).
- 14번 단위 `[정적]`: `ExCreateHandleEx` `+0x2703` 으로 `PspCidTable` 에 핸들을 만들어 새 프로세스 `+0x1d0` 에 둔다. `[추측]` 이게 프로세스 ID.
- 포인트: 땅을 받기(8번) **전에** 서류·자격·구역 정하기가 먼저 한참 있다. run9 에서 `NtCreateUserProcess` 가 가게 짓기 전에 서류를 47개 처리한 것과 비슷한 모양이다.

### 3.5 exe 세트는 run9 에서 만든 바로 그것

run9 에서 "조립식 세트 준비" 를 봤고, 이번에 "세트 놓기" 를 봤다. 둘이 **한 줄로 이어진다** `[정적]`.

```
NtCreateUserProcess +0x93c   MmCreateSpecialImageSection → exe 조립식 세트 만듦   (run9)
NtCreateUserProcess +0xc0c   그 세트를 PspAllocateProcess 의 7번째 인자로 넘김
PspAllocateProcess  +0x2080 ~ +0x2090   MmInitializeProcessAddressSpace 의 3번째 인자로 넘김
MiMapProcessExecutable +0xb8   그 세트를 새 땅에 갖다 놓음                         (run10)
```

- 비유: 관리사무소가 서류 단계에서 짜 둔 세트를, 땅을 받은 뒤 그대로 들고 와서 놓는다.
- 세트를 **어떻게 짜는지**(`MmCreateSpecialImageSection` 안쪽)는 아직 안 봤다 → Q33.

### 3.6 exe 주소가 안내판까지 가는 길

걸음 2 와 걸음 4 가 실제로 이어지는 곳이다 `[정적]`.

1. `MiMapProcessExecutable +0xb8`: exe 를 새 땅에 올린다.
2. `MiMapProcessExecutable +0xc5`: 올린 번지를 새 프로세스(장부) `+0x2b0` 칸에 적는다.
3. `MmCreatePeb`: 장부 `+0x2b0` 값을 PEB(안내판) `+0x10` 칸에 옮겨 쓴다.
4. ntdll `LdrpInitializeProcess`: PEB `+0x10` 을 읽어서 exe 의 NT 헤더를 찾는다 (결과서, sast/02 때 뽑은 `.c`).

- 칸 이름: PEB `+0x10` = ImageBaseAddress `[추측]` (알려진 배치에 맞춰 본 것).
- 비유: 관리사무소가 건물을 놓고 → 자기 장부에 "test.exe 건물: ○○번지" 라고 적고 → 그걸 안내판 `0x10` 칸에 옮겨 적는다. 관리실 직원은 출근해서 **안내판만 보면** 된다.
- 이게 **커널이 쓰고, ntdll 이 읽는** 다리 중 하나다. 그림 1(부모 cmd.exe)과 그림 2(test.exe 첫 스레드)를 잇는 실 한 가닥이다.

### 3.7 안내판에 쓰는 것, 안 쓰는 것

`MmCreatePeb` 가 PEB 에 쓰는 칸 (run 문서 표 그대로):

| PEB 오프셋 [정적] | 무엇을 쓰나 [정적] | 이름 [추측] |
|---|---|---|
| `+0x0`, `+0x3`, `+0x8` | `PspAllocateProcess` 가 넘긴 16바이트 견본 복사 (`+0x8` 은 -1) | InheritedAddressSpace / BitField / Mutant |
| `+0x10` | 새 프로세스 `+0x2b0` 값 = `MiMapProcessExecutable` 이 exe 를 올린 뒤 저장한 주소 | ImageBaseAddress |
| `+0xb8` ~ `+0xe0` | 프로세서 개수, 전역 플래그, 커널 전역값 몇 개 | NumberOfProcessors, NtGlobalFlag 등 |
| `+0x118` ~ `+0x124` | 버전 숫자 (10, 0, 빌드 번호, CSD, 2). exe 헤더 값이 있으면 덮어씀 | OS 버전 칸들 |
| `+0x128` ~ `+0x130` | exe NT 헤더에서 읽은 서브시스템 값 | ImageSubsystem 등 |
| `+0x2c0` | 세션 번호 | SessionId |
| `+0x7b8` | leap second 뷰 주소 | LeapSecondData |

`MmCreatePeb` 안에서 그 밖에 하는 일 `[정적]`:

- 새 땅에 들어감(`+0xc7`)
- leap second 데이터 섹션을 새 땅에 올림(`MmMapViewOfSection` `+0x132`). 그 번지가 위 표의 `+0x7b8`.
- PEB 자리 만들기로 보이는 `MiCreatePebOrTeb` `+0x14b` — 이름과 출력 자리로 읽은 것이라 `[추측]`
- exe 헤더 읽기(`RtlImageNtHeader` `+0x2ce`, `RtlImageDirectoryEntryToData` `+0x311`). 위 표의 버전·서브시스템 칸이 여기서 온다.
- 32비트 조건일 때만 `MiInitializeWowPeb` `+0x680`

**안 쓰는 칸이 있다.** 여기가 재밌다.

- `[정적]` `MmCreatePeb` 는 PEB `+0x18`, `+0x20`, `+0x30` 에 쓰는 줄이 **없다.**
- `[정적]` (결과서, sast/02 `.c`) 그중 `+0x18`, `+0x30` 은 **ntdll 이 직접 쓰는** 칸이다. 그러니 커널이 안 써도 이상하지 않다.
- `[정적]` 그런데 `+0x20` 은 ntdll `LdrpInitializeProcess` 가 **읽는다** (`RtlNormalizeProcessParams` 에 넘김).
- 그래서 질문이 하나 남는다: **누가 `+0x20` 을 채우나?**
  - 후보 `[추측]`: 걸음 5 `PspSetupUserProcessAddressSpace` 안의 `PspCopyAndFixupParameters` (`+0x1b9`).
  - 그렇게 짐작하는 이유:
    - 이름이 "매개변수 복사·고치기" 다.
    - `MmCreatePeb` **바로 다음** 대표 호출 안에 있다.
    - 새 땅에 들어간 채로(`+0xfa`), 메모리를 할당한(`+0x19c`) **직후**에 부른다 `[정적]`.
  - 하지만 본문은 안 봤다. → Q36 (정적), Q35 (동적: `+0x2314` 때 `+0x20` 이 아직 0 인지, `+0x239c` 뒤에 채워졌는지).
  - `+0x20` 의 이름 = 프로세스 매개변수(명령줄 등) `[추측]`.
- 비유: 안내판에 칸이 여럿인데, 관리사무소가 **"영업 안내" 칸만 비워 두고** 간다. 관리실 직원은 그 칸을 읽는다. 그러니 누군가 사이에 채워야 하는데, 마무리 담당(걸음 5)이 유력하다.

### 3.8 관찰과 코드가 맞았다

| 관찰 (run3, 콜스택) | 코드 call | 코드 ret | callee | 일치 |
|---|---|---|---|---|
| `PspAllocateProcess+0x2314` | `+0x230f` | `+0x2314` | `MmCreatePeb` | 예 |
| `NtCreateUserProcess+0xc36` | `+0xc31` | `+0xc36` | `PspAllocateProcess` | 예 (run9 와 같은 값) |

- `[관찰]` run3 + `[정적]` run10: **돌아올 자리까지 같다.**
- run3·run9 와 **같은 커널**이다 (ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1`).
- 이게 왜 반가운지는 [run9 해설 3.4](run9-create-user-process-해설.md#34-관찰과-코드가-맞았다)와 같다: 노선도가 실제 버스 노선과 같은 판이라고 믿을 수 있다. 그래도 `[정적]` 줄은 여전히 "길이 있다" 일 뿐이다.

### 3.9 갈림길: test.exe 는 `+0x2095` 쪽인가 `+0x2113` 쪽인가

([보통 길과 복제 길](#보통-길과-복제-길) 을 먼저 보고 오면 좋다.)

이번 바퀴의 추론 하나다. 근거를 하나씩 쌓아 보자.

**근거 1 — 갈림길이 있다** `[정적]`

- `PspAllocateProcess` 는 `MmInitializeProcessAddressSpace` 를 **두 곳에서** 부른다.
  - `+0x2095` (call `+0x2090`): 보통 길
  - `+0x2113` (call `+0x210e`): 복제 길
- 어느 쪽으로 갈지는 체크 칸 **bit `0x4`** 가 정한다.

**근거 2 — 같은 체크 칸이 `MmCreatePeb` 도 정한다** `[정적]`

- 뒤쪽 `+0x22e9` 에서 **같은 bit `0x4`** 를 다시 검사한다.

```
+0x22e9  TEST BL,0x4
```

  - `TEST` = "그 칸이 켜졌나?" 검사.
  - 칸이 **꺼져 있을 때만** `MmCreatePeb`(`+0x2314`)로 간다.
  - 켜져 있으면(복제) 새 안내판을 만들지 않고 부모 걸 베낀다 (2절).

**근거 3 — 실제로 `MmCreatePeb` 를 지났다** `[관찰]`

- run3 에서 test.exe 를 만들 때 콜스택에 `PspAllocateProcess+0x2314` 가 찍혔다.

**그래서:**

1. `MmCreatePeb` 가 불렸다 → 그때 bit `0x4` 는 **꺼져 있었다.**
2. 같은 칸이 앞의 갈림길도 정한다 → 앞에서도 꺼져 있었다면 **`+0x2095`(보통 길)** 쪽이다. `[추측]`
3. 곁들이는 근거 `[추측]`: test.exe 는 exe 파일로 만들었고, run9 에서 이미지 섹션을 만드는 코드도 봤다. "섹션 있음" 인 보통 길과 맞는다.

**아직 확인 아님:**

- 2번은 "**두 곳에서 검사하는 값이 같은 변수이고, 사이에 안 바뀐다**" 는 가정 위에 서 있다.
- 디버거로 `MmInitializeProcessAddressSpace` 입구에서 콜스택을 찍어, 돌아올 자리가 `+0x2095` 인지 보면 끝난다 → Q35 (동적).

비유로 다시 (태그 없음):

- 노선도에 갈림길이 있고, 갈림길 표지판(bit `0x4`)이 길 뒤쪽에 **한 번 더** 서 있다.
- 버스를 타 보니 뒤쪽 표지판의 "왼쪽 길에만 있는 정류장"(`MmCreatePeb`)을 지났다.
- 그럼 앞 갈림길에서도 왼쪽(보통 길)으로 갔을 것이다. 단, **두 표지판이 같은 내용**이라는 가정이 있다.

### 3.10 ntdll 쪽 조금 더

- `[정적]` `PsMapSystemDlls` 는 `PspSystemDlls` 표를 **최대 6칸** 돈다.
  - 0번 칸은 늘 `PspMapSystemDll` 을 부른다.
  - 나머지 칸엔 WOW64 조건이 붙는다. WOW64 = 64비트 Windows 에서 32비트 프로그램을 돌리는 장치 `[추측]` (일반적으로 알려진 설명).
  - `[추측]` 64비트 test.exe 면 0번 칸(ntdll) 하나만.
- `[정적]` `PspMapSystemDll` 을 부르는 곳은 4곳: `PsMapSystemDlls`, `PspInitPhase3`, `PsLocateSystemDlls`, `PspLocateSystemDll`. 새 프로세스 길에서는 `PsMapSystemDlls`.
  - `[추측]` 나머지 셋은 부팅 때 ntdll 섹션을 준비하는 쪽이다.
  - `[추측]` (flow.md 2.1) ntdll 섹션은 부팅 때 **한 번** 만들어 두고, 프로세스마다 **같은 섹션을 붙이기만** 한다.
  - 비유: exe 세트는 가게마다 그때그때 짠다(run9 `+0x93c`). 관리실 세트는 본사 창고에 **한 벌** 있고, 가게마다 그걸 갖다 붙인다.
- 멈춤점 팁 `[정적]`: `PspMapSystemDll` 입구에 멈춤점을 걸면 부팅 때 System 프로세스에도 걸린다 (caller 에 `PspInitPhase3`). 새 프로세스만 보려면 `PsMapSystemDlls` 입구나 `MiMapProcessExecutable+0xdc` 가 낫다.

points.md 이름 두 개, 이번에 풀린 것 `[정적]`:

| 이름 | 누가 부르나 | `PspAllocateProcess` 가 직접? |
|---|---|---|
| `MmInitializeProcessAddressSpace` | `PspAllocateProcess` (`+0x2095`, `+0x2113`), 부팅 때 `MiInitializeBootProcess` | **예** |
| `PspMapSystemDll` | 새 프로세스 길에서는 `PsMapSystemDlls` (`+0xc0`) | **아니오** |

### 3.11 "exe·ntdll 을 올린다" 와 "그 둘뿐" 은 다른 말이다

- `[정적]` 커널 쪽에 exe 와 ntdll 을 **올리는 코드는 있다**: exe `MiMapProcessExecutable +0xb8`, ntdll `PspMapSystemDll +0x11d`.
- `[추측]` 그래도 "커널이 올리는 건 **그 둘뿐**" 은 아직 추측이다. 이유:
  1. `[정적]` 같은 단계에 새 땅에 붙는 **뷰가 더 있다.**
     - leap second 섹션 — `MmCreatePeb +0x132`
     - ApiSet 뷰 — `PspSetupUserProcessAddressSpace +0x255`
     - silo 공유 데이터 뷰 — `PspSetupUserProcessAddressSpace +0x263`
     - 이것들은 DLL 은 아니다. 뜻은 대략 이렇다 `[추측]` (일반적으로 알려진 설명):
       - leap second(윤초) = 시계를 맞추려고 1초를 끼우는 일. 그 데이터.
       - ApiSet = DLL 이름표를 실제 DLL 로 이어 주는 표.
       - silo = 컨테이너 같은 격리 칸.
  2. "다른 건 **없다**" 를 말하려면 다른 길도 다 봐야 한다. 이번에 본 건 함수 8개 안의 직접 호출뿐이다.
  3. `PsMapSystemDlls` 의 나머지 칸(WOW64)은 조건부다. 64비트면 안 탈 것 `[추측]`.
- 비유: 땅에 **건물 두 채**(test.exe, 관리실)를 놓는 건 노선도에 분명히 있다. 그런데 작은 표지판·게시판 같은 것도 몇 개 같이 놓인다. "건물은 딱 두 채" 라고 하려면 땅 전체를 한 바퀴 둘러봐야 한다.

### 3.12 그 밖에 확인된 것

- `[정적]` `PspAllocateProcess` 를 부르는 곳은 `NtCreateUserProcess` 말고도 `PspCreateProcess`, `PsCreateMinimalProcess` 가 있다.
- `[정적]` 함수 포인터로 부르는 [간접 호출](run9-create-user-process-해설.md#직접-호출과-간접-호출)이 **2곳** 있다: `+0x121a`(전역 포인터 `nt+0xf04d28`), `+0x17c4`(전역 포인터 `nt+0xf04d20`). 대상은 실행해 봐야 안다.
- `[정적]` 목록에 [ICF 별칭](run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭) 경고가 붙은 줄이 몇 개 있었다. 이름을 믿기 어려워서 **대표 지점으로 쓰지 않았다** (결과서).

---

## 4. 왜 중요한가

### flow.md 에서 바뀐 것

run10 커밋(`69a65eb`)에서 바뀐 것만 적는다.

| 자리 | 전 (run9 뒤) | 후 (run10) |
|---|---|---|
| 기준 | 정적 근거 run9 | 정적 근거 **run9 ~ run10** |
| 그림 1 | `PspAllocateProcess` 아래 화살표 하나 (`+0x2314` → `MmCreatePeb`) | **정적 상자 8개 추가** (`MmCreateProcessAddressSpace`, `MmInitializeProcessAddressSpace`, `MiMapProcessExecutable`, exe 용 `MmMapViewOfSectionEx`, `PsMapSystemDlls`, `PspMapSystemDll`, ntdll 용 `MmMapViewOfSectionEx`, `PspSetupUserProcessAddressSpace`). `PspAllocateProcess` 아래 번호 `(1)` ~ `(4)` |
| 2.1 트리 | `+0x2314 MmCreatePeb [관찰]` 한 줄 | 다섯 걸음 9줄. `MmCreatePeb` 는 `[관찰]+[정적]` |
| 2.1 `PspAllocateProcess` 절 | `[추측]` 정보 묶음과 메모리 공간 준비 | `[정적]` 144줄, 메모리 쪽 4가지 일의 순서. 비유("땅 다지기 → 건물 → 관리실 → 안내판")는 `[추측]` 으로 남김 |
| 2.1 새 절 | — | 5개: `MmCreateProcessAddressSpace`, `MmInitializeProcessAddressSpace`, `MiMapProcessExecutable`, `PsMapSystemDlls` → `PspMapSystemDll`, `PspSetupUserProcessAddressSpace` |
| 2.1 `MmCreatePeb` 절 | `[관찰]` 불린 자리 + `[추측]` 역할 | `[관찰]+[정적]` + **PEB 칸 표** (오프셋 `[정적]`, 이름 `[추측]`), `+0x20` 을 안 쓴다는 점 |
| 2.1 "아직 어디서 하는지 모르는 일" | points.md 이름 4개 | `MmInitializeProcessAddressSpace`, `PspMapSystemDll` **해결**. 남은 것 `MiCreateImageFileMap`, `MmCreateTeb` |
| 2.5 | "exe 와 ntdll 뿐" `[추측]` 한 줄 | 올리는 코드는 `[정적]` 근거 추가. "**그 둘뿐**" 은 `[추측]` 그대로 (3.11) |
| 4장 공백 표 | points.md 이름 한 줄 | 그 줄은 "일부 해결". 새 줄: 남은 이름 2개, `PspAllocateProcess` 직접 호출(해결됨), `+0x2095`/`+0x2113`, 간접 호출 2곳, PEB `+0x20`, `PspPrepareSystemDllInitBlock`, 2.5 "둘뿐" |
| 4.1 | — | 그림 1 정적 호출 등 **7줄** 추가 (각각 디버거로 무엇을 보면 되는지) |
| 5장 | run9 까지 | run10 줄, 요청서 번호 표에 run10 줄 |

### 큰 그림에서 어떻게 이어지나

- **그림 1 의 `PspAllocateProcess` 가 "점 하나" 에서 "다섯 걸음" 이 됐다.**
  - 전에는 `+0x2314` → `MmCreatePeb` 하나만 알았다.
  - 이제 땅 → test.exe 건물 → 관리실 → 안내판 → 마무리 순서가 **코드로** 보인다 `[정적]`.
- **run9 와 이어졌다.**
  - run9 `+0x93c` 에서 짠 조립식 세트가, run10 `MiMapProcessExecutable +0xb8` 에서 땅에 놓인다 `[정적]` (3.5).
- **커널과 ntdll 사이에 다리가 보이기 시작했다.**
  - PEB `+0x10` 은 커널(`MmCreatePeb`)이 쓰고, ntdll(`LdrpInitializeProcess`)이 읽는다. 둘 다 `[정적]` (3.6).
  - 그림 2 의 첫 자리 `ntdll!LdrInitializeThunk` 가 **있으려면** 필요한 ntdll 올리기도, 커널 코드에서 찾았다 `[정적]`.
- **2.5 의 `[추측]` 에 `[정적]` 근거가 붙었다.** 다만 "둘뿐" 은 아직이다.
- **다음 구간:**
  - Q3 — `PspAllocateThread`, `PspInsertProcess` 안쪽. 가게(프로세스) 다음은 **첫 직원(스레드)** 이다.
  - `[추측]` (flow.md 2.1) points.md 의 남은 이름 `MmCreateTeb` 는 `PspAllocateThread` 안쪽에 있을 것이다 (TEB 는 스레드마다 하나라서).

---

## 5. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 때 `MmInitializeProcessAddressSpace` 를 `+0x2095`(보통)와 `+0x2113`(복제) 중 어디서 탔나. `[추측]` `+0x2095` | 실행 때 값(bit `0x4`)이 정한다. "같은 변수" 가정도 확인 안 됨 | Q35 (동적) |
| PEB 실제 값: `+0x2314` 에서 `+0x10`, 그리고 `+0x20` 이 아직 0 인지. `+0x239c` 뒤에 `+0x20` 이 채워졌는지 | 실행 때 값이다 | Q35 (동적). run 문서는 `+0xb8`, `+0xbc`, `+0x118~` 도 같이 보자고 적었다 |
| 간접 호출 2곳(`+0x121a`, `+0x17c4`)의 진짜 대상 | 함수 포인터 칸의 내용은 실행 때 봐야 안다 | Q35 (동적) |
| 32비트용 갈림길 `PspWow64PickBestNtdll` `+0xffb` 를 타는지. `[추측]` 64비트면 안 탐 | 조건부 호출이다 | Q35 (동적) |
| `KeSecureProcess` `+0x27a0`, `PspSetProcessAffinitySafe` `+0x2364` 를 탔는지 | 조건부 호출이다 | run 문서 "동적으로 볼 것" 5번. **Q35 줄에는 안 적혀 있다** |
| 새 땅에 놓인 ntdll 번지가 나중 `LdrInitializeThunk` 주소와 맞는지 (`MiMapProcessExecutable+0xdc`) | 실행 때 번지다 | run 문서 "동적으로 볼 것" 4번. **Q35 줄에는 안 적혀 있다** |
| PEB `+0x20` 을 누가 채우나. 후보 `PspCopyAndFixupParameters` `[추측]` | 본문을 안 뽑았다 | Q36 (정적) |
| `PspPrepareSystemDllInitBlock` 이 ntdll 로 넘기는 시작 정보 묶음인지 `[추측]` | 본문을 안 뽑았다. 2.3 공백과 이어질 수 있다 `[추측]` | Q36 (정적) |
| exe 조립식 세트를 어떻게 짜나 (`MmCreateSpecialImageSection` 안쪽, `MiCreateImageFileMap` 과 이어지나) | 이번 범위 밖 | Q33 (정적) |
| points.md 의 `MmCreateTeb` 는 어디서 불리나. `[추측]` `PspAllocateThread` 안쪽 | 이번 범위 밖 | Q3 (정적) |
| 커널이 올리는 게 exe·ntdll "**뿐**" 인지 | 다른 매핑이 없다는 걸 보이려면 다른 길도 다 봐야 한다 (3.11) | 대기열에 따로 줄이 없다 (flow.md 4장 2.5 공백 줄) |
| PEB 칸 **이름** (ImageBaseAddress 등) | 알려진 배치에 맞춰 본 것이다 | 대기열에 따로 줄이 없다. Q35 에서 값을 보면 이름이 맞는지 짐작할 근거가 생긴다 `[추측]` |

---

## 6. 더 읽기

- run 문서: [`runs/02_sprint/run10-allocate-process.md`](run10-allocate-process.md)
- 정적 요청서: [`_workspace/sast/04_allocate-process.md`](../../_workspace/sast/04_allocate-process.md)
- 정적 결과서 (144줄 묶음 표 전체, PEB 칸 전체, 별칭·조각 처리): [`_workspace/sast/04_allocate-process_result.md`](../../_workspace/sast/04_allocate-process_result.md)
- 바퀴 전 관찰: [`runs/01_sprint/run3-create.md`](../01_sprint/run3-create.md)
- 앞 바퀴 해설: [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md)
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "그림 1 — 부모 cmd.exe 스레드"
  - "2.1 커널 `nt` — 부모 cmd.exe 스레드 안에서" 의 `PspAllocateProcess` ~ `PspSetupUserProcessAddressSpace` 절
  - "2.5" 의 "exe 와 ntdll 뿐" 줄
  - "4. 공백 목록", "4.1 추측·정적으로 둔 호출 관계"
  - run10 때 바뀐 것만 보려면 `git show 69a65eb -- runs/02_sprint/flow.md`
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q2, Q3, Q33, Q35, Q36
