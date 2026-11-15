# run98 rpc-dir-create — 해설

> 이 글은 [run98 문서](run98-rpc-dir-create.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q180b** — (Q180 ②) smss 가 `ObjectDirectories` 로 `\RPC Control` 을 만드는 함수와 SD (`ProtectionMode` 1 — 소유자 · 그룹 · DACL ACE · 라벨), 만든 뒤 `NtSetSecurityObject` 로 고치는 곳 (`_workspace/plan.md`, 이번에 **끝**)
  - **PE_emulator 요청 34 의 둘째 조각**이다. 첫 조각은 run97 (Q180a, 커널이 "보안 정보 묻기" 에 답하는 길) 이었다.
  - 같은 요청의 다음 조각: Q180c (rpcrt4, run99 예정) · Q180d (넘긴 SD 가 새 객체에 붙을 때, run100 예정). 이 글에선 **이름만** 나온다.
  - 이번에 남은 것이 간 곳 (4절):
    - 동적 → **Q181** (run98 몫 ① ~ ⑦)
    - 정적 → 새 **Q185 ① ~ ④**, 그리고 Q180d · Q180c
- 근거:
  - run 문서 [`run98-rpc-dir-create.md`](run98-rpc-dir-create.md) (가장 중요)
  - 요청서 [`_workspace/sast/94_rpc-dir-create.md`](../../_workspace/sast/94_rpc-dir-create.md) · 결과서 [`94_rpc-dir-create_result.md`](../../_workspace/sast/94_rpc-dir-create_result.md)
    - 판정 **분석성공**.
    - smss 는 이번에 **처음** PDB 를 받고 처음 analyze 했다 (함수 325, `FUN_` 0).
    - 못 닫은 것: 커널이 저장한 꼴 (소유자 · 그룹 · 마스크 변환 · `AclSize`) — 동적 / Q180d. smss 밖 후보 모듈 (`AppXDeploymentServer` · `usermgr`) 이 디렉터리 SD 를 고치는지.
    - ntoskrnl 은 이번에 새로 export 하지 않았다 (같은 시간에 다른 요청이 쓰고 있어서). 그래서 ntoskrnl 쪽 줄은 **결과서가 PE 바이트로 본 값** 이다.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 근거 로그 · 흐름도 앞 글 · 2.4 한 줄 · 3장 run97 절 한 줄 · **3장 새 절** · 4장 · 5장. **그림은 안 바뀜** (3절)
  - 짐 지도 [`baggage.md`](baggage.md): **안 고침** (3절)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행도, 사진 다시 읽기도 없다.

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **smss** — "한 줄로 먼저" 바로 아래
- **표 콜백** (레지스트리 값 이름마다 짝지은 함수) — 2.3
- **Liberal SD · Primary SD** (느슨한 명부 · 기본 명부) — 2.4
- **상속 전용 줄** (`INHERIT_ONLY`, 플래그 `0x0b`) — 2.5
- **generic 권한** — 2.5 (다음 바퀴 Q180d 몫이라 짧게만)
- **절대형 SD** 는 run97 해설 2.5 의 "absolute" 그대로 (링크만)

### 이번 바퀴에서 조심할 곳 넷

1. **이번 이야기의 주인공은 test.exe 가 아니다.**
   - 부팅 때 **다른 프로세스 (smss)** 가 한 번 하는 일이다.
   - test.exe 초기화 지도 (프로세스 생성 → 은퇴) 의 **앞 · 밖** 이다. 그래서 흐름도 **그림은 그대로** 다.
   - test.exe 와 닿는 곳은 딱 하나, `\KnownDlls` 디렉터리다 (2.9).
2. **에뮬레이터 쪽 숫자는 우리 `[관찰]` 이 아니다.**
   - 레지스트리 덤프의 `ObjectDirectories` · `ProtectionMode` 값, `0x17`, 383 샘플은 요청서 배경, 즉 **PE_emulator 쪽** 에서 왔다.
   - 이 글에서도 태그 없이 "에뮬레이터 쪽 관찰" 로만 부른다.
3. **"smss 가 넘긴 명부" 와 "객체에 실제로 붙은 명부" 는 다를 수 있다.**
   - 이번에 본 건 smss 가 `NtCreateDirectoryObject` 에 **넘기는** 명부다 `[정적]`.
   - 커널이 그걸 받아 저장할 때 소유자 · 그룹을 채우거나, 권한 비트를 바꾸거나, 크기를 다시 잴 수 있다 `[추측]`.
   - 그 계산은 **Q180d (run100)**, 부팅 뒤 실제 바이트는 **Q181 (동적)** 몫이다.
4. **ntoskrnl 쪽 줄은 결과서 값이다.** `SepValidateReferencedCachedHandles +0x27e` · `CmFcpConnectToAlpcServer +0x42` 같은 줄은 `.asm` 파일이 없고 결과서가 PE 바이트로 본 것이다 (2.8).

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 프로세스 = **가게**, 핸들 = **손잡이** → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 토큰 = 가게의 **신분증** → [run49 해설](run49-kprocess-token-htable-해설.md) "비유"
- 이름 있는 객체 = **이름표 붙은 공용 사물함**, 디렉터리 = 사물함이 모인 **방** → [run71 해설 2.2](run71-named-obj-dir-해설.md)
- SD = 사물함 · 방마다 붙은 **출입 명부**. 칸은 주인 (소유자) · 소속 (그룹) · **허락 쪽** (DACL) · **특수 쪽** (SACL) → [run97 해설 2.0](run97-rpc-sd-query-해설.md)
- 무결성 라벨 = **신뢰 등급 표** (특수 쪽의 형 `0x11` 줄). 형 `0x14` = 이름만 비슷한 **프로세스 신뢰 라벨** → [run97 해설 2.0](run97-rpc-sd-query-해설.md)
- 레지스트리 = 관리실 **설정 장부** → [run20 해설](../02_sprint/run20-kernel32-load-해설.md) · [run58 해설](run58-registry-strings-해설.md)
- csrss = 관리사무소와 따로 있는 **인사팀 사무실** → [run14 해설 2.6](../02_sprint/run14-create-process-internal-해설.md) · [run34 해설](../02_sprint/run34-parent-leftovers-해설.md)
- `\KnownDlls` = 동네 **공용 창고** → [run18 해설 2.4](../02_sprint/run18-proc-init-setup-해설.md) · [run65 해설 2.3](run65-ldr-init-bodies-해설.md)
- 영구 개체 = 손잡이를 다 놓아도 선반에 남는 **이름표 붙은 원본** → [run57 해설](run57-nls-apiset-해설.md) "언제 만드나 — 부팅"
- 콜백 = 다른 함수의 **주소를 건네서** "이걸로 해 줘" 하는 것 → [run39 해설](../02_sprint/run39-dll-search-path-해설.md) 2.4
- ALPC 포트 = 프로세스끼리 거는 **전화선 끝** → [run68 해설 2.4](run68-dll-init-rest-해설.md)

flow.md 3장 새 절은 **"게시판 · 규칙표 · 관리인 · 시청"** 비유를 썼다. 그림만 다르고 가리키는 건 같다.

| flow.md 3장 새 절 | 이 글 | 실제 |
|---|---|---|
| 동네 | 관리사무소 안 **이름 공간** | 객체 이름 공간 |
| 공용 게시판 | 사물함 **방** | 디렉터리 객체 (`\Windows` · `\RPC Control`) |
| 느슨한 규칙표 (Liberal) | **느슨한 명부** | `SmpLiberalSecurityDescriptor` |
| 기본 규칙표 (Primary) | **기본 명부** | `SmpPrimarySecurityDescriptor` |
| 도장 "낮은 등급이 붙인 글 위로 못 올라감" | 특수 쪽의 **신뢰 등급 표** 한 줄 | 라벨 ACE (`0x11` · 마스크 1 · `S-1-16-4096`) |
| 관리인 | **관리인** (아래 smss) | smss |
| 시청 | 관리사무소 | 커널 |

### 한 줄로 먼저 — 무엇을 얻었나

먼저 새 말 하나.

- **새 말 — smss** (세션 관리자, `smss.exe`) = 부팅 때 **가장 먼저 뜨는 유저 프로세스** 로, 세션과 전역 이름 공간을 차리는 일을 한다고 알려져 있다 `[추측]` (일반적으로 알려진 설명. run 문서는 "세션 관리자" 라고만 부른다).
  - 비유: 동네 가게들이 문 열기 **전에** 먼저 와서 공용 방들을 꾸미고 명부를 붙여 두는 **관리인**.
  - 관리사무소 (커널) 직원이 아니다. 가게 안 관리실 (ntdll) 도 아니다. **따로 있는 유저 쪽 사람** 이다. 그래서 방을 만들 때도 관리사무소 창구 (syscall `NtCreateDirectoryObject`) 를 거친다.

**이번 글의 중심은 이 두 줄이다.** `[정적]`

> **`\RPC Control` 방은 부팅 때 관리인 smss 가 만든다.** 레지스트리 `Session Manager\ObjectDirectories` 의 이름마다 `SmpConfigureObjectDirectories +0xf1 NtCreateDirectoryObject` 를 부른다. 커널은 이 방을 만들지 않는다.

> **이름이 `\RPC Control` 이나 `\Windows` 면 "느슨한 명부" (Liberal SD) 를 붙인다.** 주인 · 소속 칸은 **비어 있고**, 허락 쪽 7줄 (전부 허용), 특수 쪽에 **신뢰 등급 표 1줄** (`0x11` · 마스크 1 · `S-1-16-4096`). `ProtectionMode` 가 0 이어도 같은 명부다.

| 요청서 질문 | 한 줄 답 | 이 글 |
|---|---|---|
| 1 (2-가) 만드는 함수 · SD | `SmpConfigureObjectDirectories +0xf1`, 이름 비교 → Liberal, DACL 7 + 라벨 1, 모드 0 이어도 같음 | 2.2 ~ 2.6 |
| 2 (2-나) 만든 뒤 고치는 곳 | smss 안 없음 · smss 밖 보이는 범위에서 못 찾음 · `\Windows` 만 csrss 가 고침 | 2.7 |
| 3 커널이 미리 만드나 | 아니오 — 이름 비교 표 · ALPC 붙기로만 씀 | 2.8 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 길은 test.exe 가 태어나기 **훨씬 앞**, 부팅 때다.

```
(부팅)
 관리사무소 (커널) 부팅 — 이름 공간 루트 · 디렉터리 형 만들기 (run97 2.7)
 │
 관리인 smss 가 뜸
 ├─ \RPC Control · \Windows 방 만들기 + 느슨한 명부 붙이기         ← 이번 (run98)
 ├─ \KnownDlls 방 (공용 창고) 만들기 + 그 명부 붙이기                ← 이번 (2.9, test.exe 와 닿는 곳)
 │
 인사팀 사무실 csrss — \Windows 방의 허락 쪽을 고침                   ← 이번 (2.7)
 │
 … (로그온, cmd.exe)
 │
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → LdrpInitializeProcess
 │     └─ +0xdb8  NtOpenDirectoryObject(…, 3, "\KnownDlls")         ← 위에서 smss 가 만든 그 방 (run65)
 │  … → NtContinue (은퇴) → 진입점 → main
 │
(rpcrt4 를 쓰는 다른 프로그램이라면 — PE_emulator 가 만난 상황)
 rpcrt4 → NtOpenDirectoryObject("\RPC Control") → NtQuerySecurityObject(0x17)   ← run97: 커널이 어떻게 답하나
```

- 위 그림의 "로그온, cmd.exe" 줄과 csrss 의 정확한 차례는 이번 근거에 없다. 차례를 보여 주려는 그림일 뿐이다.

### 왜 이 질문이 지금 왔나

- run97 에서 커널이 "명부 사본 주세요" (`NtQuerySecurityObject`) 에 **어떤 규칙으로** 답하는지 봤다.
- 그런데 사본을 만들려면 **원본 명부** 가 있어야 한다. 에뮬레이터는 `\RPC Control` 방에 **명부를 아예 안 채워 두고** 있었다 (요청서 배경 — 에뮬레이터 쪽 관찰).
- 그래서 "진짜 Windows 에선 **누가, 어떤 명부로** 이 방을 만드나?" 가 다음 질문이 됐다.

### 무엇이 궁금했나

"`\RPC Control` 방은 **누가 · 어떤 명부를 붙여** 만들고, **만든 뒤 누가 명부를 고치나?** 커널이 미리 만들어 두나?"

- 요청서는 이걸 셋으로 나눴다 (위 "한 줄로 먼저" 표).
- 실마리는 요청서 배경에 있었다: 에뮬레이터 쪽 레지스트리 덤프에 `ObjectDirectories` (`\Windows` · `\RPC Control`) · `ProtectionMode` (1) 가 있고, smss.exe 안에 이 글자들이 나란히 있다 (바이트 검색만 — **실마리, 근거 아님**).

### 바퀴 전에 알던 것

- `[정적]` (run97) 커널은 LABEL (`0x10`) 을 물으면 객체 SACL 에서 **형 `0x11` 줄만** 골라 준다. 객체에 라벨 줄이 없을 때의 세 경우 (a) · (b) · (c) 도 정리했다 ([run97 해설 2.6](run97-rpc-sd-query-해설.md)).
  - 그때 결론: "진짜 `\RPC Control` 에 SACL 이 있나 없나는 진짜 명부를 떠 봐야 안다." → 이번에 **처음 명부** 쪽은 정적으로 답이 났다 (2.10).
- `[정적]` (run65) test.exe 는 `LdrpInitializeProcess +0xdb8 NtOpenDirectoryObject(&LdrpKnownDllDirectoryHandle, 3, "\KnownDlls")` 로 공용 창고를 연다. **그 창고를 누가 만들었나** 는 안 봤다.
- `[관찰]` (run44) test.exe 핸들 `0x4c` = Directory `KnownDlls` (짐 지도 7.4).
- `[관찰]` (run44) test.exe 가 올리는 모듈은 6개 — rpcrt4 는 없다.
- `[정적]` (run71) kernelbase 가 `DBWinMutex` 를 만들 때도 명부를 직접 만들어 넘겼다 ("SID 4 · DACL 3 · SACL (낮은 무결성 쓰기 금지)"). 명부를 **만드는 쪽** 이야기는 처음이 아니다. 이번엔 만드는 사람이 **smss** 다.
- smss 는 앞 해설에서 이름만 스쳤다 (run34 해설 "csrss 명령줄은 smss 가 만든다고 알려져 있다 `[추측: 일반 지식]`", run50 해설 "세션 새로 만들기 — smss 만"). 코드를 연 건 이번이 처음이다.

---

## 2. 찾은 것

### 2.1 한눈에 — 관리인이 방을 만드는 길

run 문서 "호출 관계" 에서 대표만 남겼다. 모두 `[정적]`.

```
smss!wmain
 └─ +0x556  SmpInit
      ├─ (1) +0x2f4  SmpCreateSecurityDescriptors (CL=1)        명부 6벌 · SID 9개 · 모드 1 → 기본 · 느슨한 명부 채움   ← 2.2 줄 ③, 2.6
      ├─      +0x38c  NtAlpcCreatePort (명부 = ApiPort)             (smss 자기 전화선)
      └─ (2) +0x534  SmpLoadDataFromRegistry
               ├─ +0x55c  RtlQueryRegistryValuesEx(CONTROL, "Session Manager", 표)          ← 2.2 줄 ②, 2.3
               │     ⇢ 표 0번  SmpConfigureProtectionMode    → +0x1a SmpCreateSecurityDescriptors (CL=0)   ← 2.6
               │     ⇢ 표 1번  SmpConfigureObjectDirectories      이름마다:
               │            +0x90 == "\RPC Control" ? · +0xab == "\Windows" ?  → +0xbb 느슨한 명부    ← 2.4
               │            └─ +0xf1  NtCreateDirectoryObject(&h, 0xf000f, {이름, 0xd0, 명부})      ← 2.2 줄 ①
               │                 → +0x11f NtClose
               ├─ +0x5df  SmpOpenKnownDllsHandles ("\KnownDlls")                                 ← 2.9
               │        ├─ +0x18f  NtCreateDirectoryObject (명부 = KnownDllsDir)
               │        └─ +0x1fc  NtCreateSymbolicLinkObject ("KnownDllPath")
               └─ +0x708  NtCreateDirectoryObject ("\Sessions", 기본 명부)

뒤에 (다른 프로세스, csrss):
csrsrv!CsrParseServerCommandLine
 └─ +0x210  CsrSetDirectorySecurity → +0x2bb NtSetSecurityObject(…, DACL)      ← \Windows   (2.7)
```

- `⇢` 는 "직접 부르는 게 아니라 **표를 거쳐** 불린다" 는 뜻이다 (2.3).

비유로 한 번:

1. 관리인 (smss) 이 출근하자마자 **명부 여섯 벌** 을 먼저 써 둔다 (`SmpInit +0x2f4`).
2. 그다음 관리실 설정 장부 (레지스트리) 의 "Session Manager" 쪽을 펼친다 (`+0x55c`).
3. 장부의 **항목마다 담당 함수** 가 정해져 있다. `ProtectionMode` 항목 담당이 먼저, `ObjectDirectories` 항목 담당이 그다음이다.
4. `ObjectDirectories` 담당은 적힌 방 이름마다 관리사무소 창구에 "**이 이름으로 방 하나, 이 명부 붙여서**" 를 신청한다 (`+0xf1`).
5. 이어서 공용 창고 (`\KnownDlls`) 방, `\Sessions` 방도 만든다.

---

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `└─ +0xf1  NtCreateDirectoryObject(&h, 0xf000f, {이름, 0xd0, 명부})`

- **누가**: `smss!SmpConfigureObjectDirectories` (rva `0x143d0`) — 장부 `ObjectDirectories` 항목 담당 함수.
- **어디서**: 자기 코드 `+0xf1` (ret `+0xf8`). 이름 하나마다 이 줄을 한 번 탄다 (루프 `+0x6b ~ +0x13a`).
- **누구를**: `NtCreateDirectoryObject` — 관리사무소에 "방 (디렉터리 객체) 만들어 주세요" 하는 syscall. 넘기는 것:
  - 손잡이 받을 칸 `&h`
  - 접근 `0xf000f` (`+0xd5`)
  - `OBJECT_ATTRIBUTES` (`+0xc6 ~ +0xed`): Length `0x30` · Root 0 · 이름 · **Attributes `0xd0`** · **SD = 고른 명부** · QoS 0
- **무엇을 위해**: 장부에 적힌 이름대로 **방을 만들고 명부를 붙이려고**.
  - `0xd0` = `OBJ_PERMANENT 0x10` + `OBJ_CASE_INSENSITIVE 0x40` + `OBJ_OPENIF 0x80` (비트 이름은 `[추측: 공개 헤더]`, 값은 코드).
    - **영구** — 손잡이를 놓아도 남는 원본 ([run57 해설](run57-nls-apiset-해설.md)). 그래서 성공하면 바로 `+0x11f NtClose` 로 손잡이를 놓는다. 방은 남는다.
    - **대소문자 무시** — 이름을 찾을 때 대문자 · 소문자를 안 가린다.
    - **있으면 열기** — 같은 이름 방이 이미 있으면 새로 안 만들고 그걸 연다. 그때 새 명부는 안 붙는다 `[추측]`. `\RPC Control` 이 smss 전에 있을 일은 정적으로 못 찾았다 (2.8).
  - 실패하면 `+0x114 SmpLogFailureString` 으로 **기록만 하고 다음 이름** 으로 간다 (`0x1cee`). 함수는 늘 0 을 돌려준다 (`+0x148`) `[정적]`.
  - smss 는 `NtCreateDirectoryObjectEx` 를 import 하지 않는다. smss 안 `NtCreateDirectoryObject` 는 이 줄 · `\KnownDlls` (`+0x18f`) · `\Sessions` (`+0x708`) 셋뿐이다 `[정적]`.

#### 줄 ②: `├─ +0x55c  RtlQueryRegistryValuesEx(CONTROL, "Session Manager", 표)`

- **누가**: `smss!SmpLoadDataFromRegistry`.
- **어디서**: `+0x55c` (ret `+0x563`). 표 주소는 바로 앞 `+0x54a LEA R8` 에서 잡는다.
- **누구를**: `RtlQueryRegistryValuesEx` (ntdll 쪽 함수) — 넘기는 것 = (`2` = `RTL_REGISTRY_CONTROL`, 글 `"Session Manager"`, 표 `SmpRegistryConfigurationTable`).
- **무엇을 위해**: 장부의 `...\Control\Session Manager` 쪽 값들을 **표에 적힌 차례대로** 읽고, 값마다 표에 짝지어 둔 **담당 함수 (콜백)** 를 부르게 하려고.
  - 그래서 줄 ① 의 함수를 **이 함수 안쪽에서** 부른다. smss 코드에 "`SmpConfigureObjectDirectories` 를 부르는 줄" 은 없다 (2.3).

#### 줄 ③: `├─ (1) +0x2f4  SmpCreateSecurityDescriptors (CL=1)`

- **누가**: `smss!SmpInit`.
- **어디서**: `+0x2f4` (ret `+0x2f9`). 바로 앞 `+0x2f2 MOV CL,1` 로 인자 1 을 넣는다.
- **누구를**: `SmpCreateSecurityDescriptors` (rva `0xa1b0`, 4138B) — smss 가 쓸 명부를 **다 만드는 함수**.
- **무엇을 위해**: 방을 만들기 **전에** 명부부터 준비하려고. 이 부름 하나로 명부 여섯 벌의 빈 틀, SID 9개, 그리고 모드 비트가 1 이면 기본 · 느슨한 명부의 허락 쪽 (+ 느슨한 명부의 특수 쪽) 까지 채운다 (2.6).
  - 이 줄이 `+0x534 SmpLoadDataFromRegistry` 보다 **앞** 이라서, 방을 만들 땐 명부가 이미 있다.

---

### 2.3 표 콜백 — 왜 지도에 "부르는 사람" 이 없나

#### 새 말 — 표 콜백

- **표 콜백** = 표에 "**이 장부 값을 읽으면 이 함수를 불러라**" 를 짝지어 적어 두고, 읽는 함수가 그 표를 보며 대신 불러 주는 방식.
  - 콜백 자체는 이미 나온 말이다 ([run39 해설](../02_sprint/run39-dll-search-path-해설.md) 2.4). 이번엔 그 주소가 **표의 칸** 에 들어 있다.
- 비유: 장부 옆에 붙은 **"항목 → 담당자" 배정표**. 장부를 넘기는 사람 (`RtlQueryRegistryValuesEx`) 이 항목을 읽을 때마다 배정표를 보고 담당자를 부른다.

#### 표 안 `[정적]` (`SmpRegistryConfigurationTable`, rva `0x300b0`, 칸 `0x38`B — 결과서가 PE 바이트로 읽음)

| 칸 | 장부 값 이름 | 담당 (콜백) | 기본형 | 값이 없을 때 |
|---|---|---|---|---|
| 0번 | `ProtectionMode` | `SmpConfigureProtectionMode` (rva `0x145d0`) | `REG_DWORD` | (아래 2.6) |
| 1번 | `ObjectDirectories` | `SmpConfigureObjectDirectories` (rva `0x143d0`) | `REG_MULTI_SZ` | 기본값 `\Windows` · `\RPC Control` 두 줄 (PE 바이트 `0x29088`) |
| 2번 ~ | `BootExecute` 등 | | | 이번 범위 밖. 표 끝은 26번 칸 |

#### 이 표에서 읽을 것 둘

- **칸 차례가 곧 시간 차례다.** 0번 (`ProtectionMode`) 이 1번 (`ObjectDirectories`) 보다 먼저라서, 방을 만들 땐 명부가 **이미 (다시) 만들어져 있다**.
- **정적 지도엔 담당자를 부르는 줄이 안 보인다.**
  - `SmpConfigureObjectDirectories` · `SmpConfigureProtectionMode` 는 `_calls.tsv` · 지도 둘 다 **caller 가 0** 이다.
  - 함수 주소가 **표 칸에만** 있고, 실제로 부르는 줄은 ntdll `RtlQueryRegistryValuesEx` 안쪽에 있기 때문이다.
  - 그래서 결과서는 표를 PE 바이트로 직접 읽었다. 문자열 `ObjectDirectories` · `ProtectionMode` 를 가리키는 곳도 **이 표 칸 둘뿐** 이다 (결과서).

비유: 담당자 이름은 배정표에만 있고, "○○ 씨, 와 주세요" 하고 부르는 건 장부 넘기는 사람이다. 그래서 담당자 쪽 서류만 보면 "누가 날 불렀지?" 가 비어 있다.

- 디버거로 이 함수에 멈추면 콜스택 바로 위가 `smss!SmpLoadDataFromRegistry` 가 아니라 **`ntdll!RtlQueryRegistryValuesEx` 안쪽** 일 것이다 `[추측]`. `smss!SmpLoadDataFromRegistry+0x563` 은 그보다 더 위에 보일 것이다 (run 문서 "함정 후보").

---

### 2.4 ★ 이름 비교로 명부를 고른다 — 느슨한 명부 · 기본 명부

#### 새 말 — Liberal SD · Primary SD

- 둘 다 smss 안 전역 칸의 **PDB 이름** 이다 (`SmpLiberalSecurityDescriptor` · `SmpPrimarySecurityDescriptor`).
- 이 글에선 **느슨한 명부 (Liberal)** · **기본 명부 (Primary)** 라고 부른다.
- 두 명부의 차이는 한 줄이다 (2.5 에서 자세히): 느슨한 명부는 "누구나" 에게 **쓰기** 까지 주고 특수 쪽에 **신뢰 등급 표** 가 있다. 기본 명부는 쓰기가 없고 특수 쪽이 없다.

#### 고르는 길 — 단계별로 `[정적]` (`SmpConfigureObjectDirectories`)

1. `+0x72` `RtlInitUnicodeString` — 지금 이름을 글 객체로 만든다.
2. `+0x7e` — 명부 = **기본 명부** 로 먼저 정해 둔다.
3. `+0x90 RtlEqualUnicodeString(이름, "\RPC Control", 1)` — 셋째 인자 1 = 대소문자 무시. 같으면 `+0x9e JNZ → +0xbb`.
4. 아니면 `+0xab RtlEqualUnicodeString(이름, "\Windows", 1)`. 다르면 `+0xb9 JZ → +0xc2` (기본 명부 그대로).
5. `+0xbb` — 둘 중 하나와 같을 때만 명부 = **느슨한 명부**.
6. 그 명부를 `OBJECT_ATTRIBUTES` 에 넣고 줄 ① (`+0xf1`).

- 비교용 글은 smss 안에 있다: `\RPC Control` (rva `0x2a0f8`) · `\Windows` (rva `0x2a118`) (`+0x1c` · `+0x30` · `+0x3e` · `+0x55`).

#### 그래서

- **`\RPC Control` 만을 위한 줄은 `+0x90` 비교 하나** 다. 그런데 `\Windows` 와 **한 묶음** 이다.
- 장부에 이름이 `\Windows` · `\RPC Control` 둘뿐이면 (에뮬레이터 쪽 덤프 값이 그렇다 — 에뮬레이터 쪽 관찰), **두 방이 같은 명부 한 장 (포인터 `0x32668`) 을 같이 쓴다** `[정적]`.
- 그 밖의 이름이 장부에 더 있으면 그 방은 기본 명부를 받는다. 타깃 VM 장부에 정확히 무엇이 있나는 동적 ③ (4절).

비유: 관리인 손에 명부가 두 묶음 있다. 이름표를 보고 "`\RPC Control`? `\Windows`? 그럼 느슨한 걸로." 아니면 기본 걸로.

---

### 2.5 ★ 느슨한 명부 안 — 주인 · 소속 없음, 허락 쪽 7줄, 특수 쪽 1줄

모두 `SmpCreateSecurityDescriptors` 안, `ProtectionMode` 1 갈래 `[정적]`. 바이트는 run 문서 "`\RPC Control` 의 처음 SD" 표에 있다.

#### 명부 머리

| 칸 | 값 | 한 줄 |
|---|---|---|
| Revision | 1 | `+0xe0 ~ +0xf1` `RtlCreateSecurityDescriptor(&몸통, 1)` |
| Control (smss 쪽) | `0x0014` | "허락 쪽 있음 (`0x4`)" + "특수 쪽 있음 (`0x10`)". 그 밖 비트 (`DEFAULTED` · `PROTECTED` · `AUTO_INHERITED`) 없음 |
| **주인 (소유자)** | **없음 (NULL)** | smss 가 이 명부에 `RtlSetOwnerSecurityDescriptor` 를 안 부른다. smss 안 유일한 호출은 `SmpSetDumpSecurityAndAttributes +0x1d7` (덤프 파일) |
| **소속 (그룹)** | **없음 (NULL)** | `RtlSetGroupSecurityDescriptor` 는 import 도 안 한다 |
| 허락 쪽 (DACL) | `AclRevision 2` · `AclSize 0xb8` (184) · `AceCount 7` | 쓰인 건 156B, 뒤 28B 는 안 씀 |
| 특수 쪽 (SACL) | `AclRevision 2` · `AclSize 0x20` (32) · `AceCount 1` | 쓰인 건 28B |

- 주인 · 소속이 비었으면 누가 채우나? **방을 만든 순간 관리사무소가, 만든 쪽 (smss) 신분증에서 채울 것이다** `[추측]` (결과서). 어느 SID 가 들어가는지는 Q180d · 동적 ① 몫이다.
  - flow.md 3장 비유의 "주인 칸은 비워 둔다 — 시청 (커널) 이 채운다" 도 이 `[추측]` 을 그린 것이다.
- smss 가 넘기는 명부는 **절대형** (`0x28`B 머리 + 허락 쪽 · 특수 쪽 **포인터**) 이다. 한 덩어리 꼴 (self-relative) 로 바꾸는 건 커널 몫 → 두 꼴의 차이는 [run97 해설 2.5](run97-rpc-sd-query-해설.md) "self-relative".

#### 허락 쪽 7줄 (차례 그대로, 전부 "허용" — 형 0)

| # | 플래그 | 마스크 | SID (이름은 `[추측: 결과서, 공개 문서]`) | 넣는 줄 |
|---|---|---|---|---|
| 0 | `0x00` | `0xE0000000` | `S-1-1-0` (Everyone) | `+0xd1e` |
| 1 | `0x00` | `0xE0000000` | `S-1-5-12` (RESTRICTED) | `+0xd36` |
| 2 | `0x00` | `0x10000000` | `S-1-5-32-544` (Administrators) | `+0xd54` |
| 3 | **`0x0b`** | `0xE0000000` | `S-1-1-0` | `+0xd6f`, 플래그 `+0xda0` |
| 4 | `0x0b` | `0xE0000000` | `S-1-5-12` | `+0xda8`, 플래그 `+0xdd6` |
| 5 | `0x0b` | `0x10000000` | `S-1-5-32-544` | `+0xdde`, 플래그 `+0xe0c` |
| 6 | `0x0b` | `0x10000000` | `S-1-3-0` (CREATOR OWNER) | `+0xe14`, 플래그 `+0xe42` |

- 넣는 함수는 전부 `RtlAddAccessAllowedAce` → **허용**. **거부 줄은 없다** (결과서).
- 플래그를 `0x0b` 로 바꾸는 줄은 위 "플래그" 의 `MOV byte [RAX+1],0xb` 뿐이다. 즉 **먼저 플래그 0 으로 넣고, 뒤에서 덮어쓴다**.

#### 새 말 — 상속 전용 줄 (플래그 `0x0b`)

- `0x0b` = `OBJECT_INHERIT 1` + `CONTAINER_INHERIT 2` + `INHERIT_ONLY 8` (run 문서 · 결과서).
- **상속 전용 줄** = **이 방 자신에겐 안 쓰이고**, 이 방 안에 **새로 생기는 물건에 물려줄** 줄.
- 비유: 방 명부 아래칸에 "**앞으로 이 방에 들일 사물함엔 이 줄을 베껴 붙일 것**" 이라고 적어 둔 메모.
- 그래서 7줄은 둘로 나뉜다 `[정적]`:
  - 0 ~ 2번 (플래그 0) = **이 방 몫** 셋
  - 3 ~ 6번 (플래그 `0x0b`) = **자식 몫** 넷 — 0 ~ 2번과 같은 셋 + `S-1-3-0`
- 자식이란 예를 들어 `\RPC Control\epmapper` 같은 ALPC 포트다 (결과서의 예). 실제로 어떻게 물려받는지는 동적 ④ (급하지 않음 — rpcrt4 가 묻는 건 방 자신이다).
- `S-1-3-0` (CREATOR OWNER) 은 물려줄 때 "**그 자식을 만든 사람**" 자리로 바뀌는 특별한 SID 로 알려져 있다 `[추측]` (일반적으로 알려진 설명 — 근거 문서엔 이름만 있다).

#### 새 말 — generic 권한 (짧게)

- 마스크 `0xE0000000` = GENERIC_READ · WRITE · EXECUTE, `0x10000000` = GENERIC_ALL (run 문서).
- **generic 권한** = "읽기 / 쓰기 / 실행 / 전부" 같은 **큰 묶음** 권한. 객체 형마다 **그 형의 구체 권한** 으로 풀어서 쓴다고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유: 명부에 "**읽기 OK**" 라고만 크게 써 두면, 방 종류에 따라 그게 "목록 보기 · 이름 찾기" 같은 세부 칸으로 풀린다.
- **커널이 저장할 때 이걸 디렉터리 구체 권한으로 바꿔 두나** 는 이번에 모른다 → **Q180d** (run100, 다음 바퀴 몫).

#### 특수 쪽 1줄 — 신뢰 등급 표

| # | 형 | 플래그 | 마스크 | SID | 줄 |
|---|---|---|---|---|---|
| 0 | **`0x11`** (SYSTEM_MANDATORY_LABEL) | `0x00` (`+0xe56`) | `0x00000001` NO_WRITE_UP (`+0xe5b`) | `S-1-16-4096` (낮은 무결성) (`+0xe52`) | `+0xe68 RtlAddMandatoryAce` (형 `0x11` 은 `+0xe63`) |

- 읽는 법: 이 방의 신뢰 등급은 **"낮음"** 이고, 규칙은 **"아래 등급이 위로 쓰기 금지" (NO_WRITE_UP)** 하나다.
  - 등급이 "낮음" 이라 거의 누구든 위에서 내려다보는 쪽이다. 그러니 이 줄이 막는 일은 거의 없을 것이다 `[추측]` (일반적인 라벨 검사 설명 — 근거엔 검사 코드가 없다).
- 감사 (`0x02`) · 프로세스 신뢰 라벨 (`0x14`) 같은 다른 특수 줄은 **느슨한 명부엔 없다** (결과서). `0x14` 는 `\KnownDlls` 쪽에만 있다 (2.9).
- run71 해설의 `DBWinMutex` 명부에도 "SACL (낮은 무결성 쓰기 금지)" 가 있었다. **같은 모양의 등급 표** 를 다른 사람 (kernelbase · smss) 이 각자 붙인 것으로 보인다 `[추측]` (run71 은 바이트를 적지 않았다).

#### 빠진 사람 하나 — `S-1-5-18` (SYSTEM)

- SID 9개는 만들지만 (2.6), **느슨한 명부의 허락 쪽엔 `S-1-5-18` 이 없다** `[정적]`.
- SYSTEM 은 `S-1-5-32-544` (Administrators) 멤버라서 그 줄로 닿을 것이다 `[추측: 결과서]`.

#### 기본 명부 (Primary) 와 비교 — 한 줄씩 `[정적]`

| | 느슨한 명부 | 기본 명부 |
|---|---|---|
| 쓰는 곳 | `\RPC Control` · `\Windows` | 그 밖 이름 · `\Sessions` · DosDevices · 파이프 접두어 |
| Control | `0x14` | `0x4` |
| 특수 쪽 | 등급 표 1줄 | **없음** |
| 허락 쪽 차례 · 플래그 | 7줄 (위 표) | 같음 |
| `S-1-1-0` · `S-1-5-12` 마스크 | `0xE0000000` (쓰기 포함) | **`0xA0000000`** (쓰기 없음) |

- 결과서 요약: `S-1-5-12` (RESTRICTED) 와 `S-1-1-0` (Everyone) 에 **쓰기까지 준다 — 이게 Liberal 이다.**
- 기본 명부 줄은 `+0xade ~ +0xc11`, 붙이는 곳 `+0xc15` (결과서).

---

### 2.6 ★ `ProtectionMode` 가 0 이어도 명부가 같은 이유 — 단계별로

요청서는 "`ProtectionMode` 1 갈래" 를 물으면서 "0 갈래와 다르면 차이만" 을 물었다. 답은 **"코드상 차이 없음"** 이다 `[정적]`. 왜 그런지 차근차근.

#### 등장인물

- `SmpProtectionMode` — smss 안 전역 칸 (`.data` rva `0x31ce4`). **파일에 들어 있는 처음 값이 이미 `01 00 00 00` = 1** 이다 (flow-writer 가 `objdump -s` 로 다시 봄).
- 이 칸에 값을 쓰는 곳은 **`SmpConfigureProtectionMode +0x14` 하나** 뿐이다 (`refs`).
- `SmpCreateSecurityDescriptors` 는 **두 번** 불린다. 인자 `CL` 이 다르다.

#### 차례대로

1. **첫째 부름 — `SmpInit +0x2f4` (CL=1).**
   - 명부 6벌 빈 틀 (`+0xa7 ~ +0x1f8`) → SID 9개 (`+0x254 ~ +0x4e3`) → `\KnownDlls` · ApiPort 명부 채움.
   - 마지막 `+0x8cc` 에서 `SmpProtectionMode & 1` 을 본다. 이때 값은 **파일 처음 값 1** → 기본 · 느슨한 명부의 허락 쪽 (+ 느슨한 명부의 특수 쪽) 까지 **다 채운다**.
2. **장부를 읽는다 — 표 0번 콜백 `SmpConfigureProtectionMode`.**
   - `+0x4 MOV EAX,1` · `+0x9 CMP R9D,4` · `+0xd JNZ` — 값 길이가 4 가 **아니면 1** 로 둔다.
   - `+0x14` — 그 값을 `SmpProtectionMode` 에 쓴다.
   - `+0x1a` — `SmpCreateSecurityDescriptors` 를 **CL=0** 으로 다시 부른다 (`+0x12 XOR ECX,ECX`).
3. **둘째 부름 (CL=0) 안.**
   - `+0x9f TEST CL,CL` · `+0xa1 JZ → +0x1ff` — 인자가 0 이라 빈 틀 만들기를 건너뛴다.
   - `+0x1ff ~ +0x20c` — 모드 비트 0 이 **0 이면** `XOR EAX,EAX` 하고 함수 끝으로 → **아무것도 안 바꾼다.**
   - 비트 0 이 **1 이면** SID 를 다시 만들고 `+0x504 TEST BL,BL` · `+0x506 JZ → +0x8cc` 로 뛰어 **기본 · 느슨한 명부만** 다시 만든다.

#### 그래서

- 장부 값이 **1** → 둘째 부름이 같은 명부를 한 번 더 만든다 → 결과 같음.
- 장부 값이 **0** → 둘째 부름이 그냥 돌아온다 → 첫째 부름이 만든 **모드 1 명부가 그대로** 남는다.
- 장부에 값이 **없거나 길이가 4 가 아니면** → 콜백이 1 로 둔다 → 결과 같음. (값이 없을 때 길이 0 으로 올 것이라는 건 `[추측]` — 결과서는 `RtlQueryRegistryValuesEx` 쪽을 안 읽었다.)
- → **"허락 쪽이 NULL 인 채로 남는 모드" 는 이 빌드 코드엔 없다** `[정적]`.

비유: 관리인이 출근하자마자 "**엄격 모드**" 로 명부를 미리 써 둔다. 그다음 장부에서 "모드: 엄격" 을 보면 한 번 더 고쳐 쓰고, "모드: 끔" 을 보면 **그냥 지나간다**. 지울 줄 모른다. 그래서 어느 쪽이든 엄격 모드 명부가 붙는다.

- 결과서도 이 결론에 "**초기값 줄이 핵심 — 읽는 이는 한 번 더 확인 권장**" 을 붙였다. flow-writer 가 그 바이트 (`0x31ce4`) 를 다시 봐서 일치했다.

---

### 2.7 만든 뒤 명부를 고치는 곳 — `\RPC Control` 은 못 찾음, `\Windows` 는 csrss

"방을 만든 뒤 누가 명부를 바꿔 붙이나 (`NtSetSecurityObject`)" 를 세 겹으로 찾았다.

#### ① smss 안 — `\RPC Control` 엔 없다 `[정적]`

smss 안 `NtSetSecurityObject` 는 **3곳뿐** 이다 (PE 바이트 스캔 + `_calls.tsv` 일치):

| 자리 | 대상 |
|---|---|
| `SmpCreateProtectedPrefixes +0xa44` | `\Device\NamedPipe\` (정보 `0x1c`) |
| `SmpCreateProtectedPrefixes +0xab3` | `\Device\mailslot\` (정보 `0x1c`) |
| `SmpSetDumpSecurityAndAttributes +0x1ed` | 덤프 · 페이지 파일 |

- 객체 디렉터리를 손잡이로 여는 곳은 `SmpInitializeDosDevices +0x76` (읽기 열기) 하나이고, 그 뒤에 `NtSetSecurityObject` 가 없다.
- 느슨한 명부 포인터 (`0x32668`) 에 **쓰는 곳은 `+0xea` 하나** (빈 틀 만들 때). 읽는 곳은 `+0xfd` · `+0xe74` · `+0xe90` · `SmpConfigureObjectDirectories +0xbb` (`refs`). 명부를 붙인 뒤 다시 손대는 줄이 없다.

#### ② smss 밖 — 보이는 범위에선 못 찾음 `[정적, 보이는 범위]`

- `RPC Control` 글이 든 System32 이진은 17개. 그중 이 저장소에 분석이 있는 건 `ntoskrnl` · `kernelbase` · `gdi32full` 셋뿐이다.
  - kernelbase 의 `RPC Control` 은 **AppContainer · 그림자 이름 공간 쪽** 의 다른 방 (`\Sessions\N\…\RPC Control`) 이다 (`BasepOpenShadowRpcControlDirectory` · `BasepCreateAppContainerObjectDirectories +0x590`).
  - gdi32full 의 것은 ALPC 포트 이름 (`\RPC Control\UmpdProxy`) 이다.
  - **둘 다 이 방의 명부를 안 고친다.** ntoskrnl 은 2.8.
- 분석이 없는 후보 중 `NtSetSecurityObject` · `SetSecurityInfo` 를 import 하는 건 `AppXDeploymentServer.dll` · `usermgr.dll` (지연) · `win32kfull.sys` (`ZwSetSecurityObject`). 이들이 방 명부를 건드리나는 **분석 안 함**. 부팅 첫 순간이 아니라 서비스 · 로그온 뒤일 것이다 `[추측: 결과서]` → Q185 ④ (낮음).

#### ③ csrss — `\Windows` 는 고친다 `[정적]`

- csrsrv (`csrss` 안에서 도는 DLL) 에 `RPC Control` 글은 없다. 그런데:
  - `CsrParseServerCommandLine +0x1f9 NtCreateDirectoryObject` — 명령줄 `ObjectDirectory=` 이름으로 방을 만든다 (속성 `0xc0`, 서비스 세션이면 `0xd0`).
  - `+0x210 CsrSetDirectorySecurity` (rva `0x7570`) → 그 안 `+0x2bb NtSetSecurityObject(CsrObjectDirectory, 4 = DACL, …)` — **허락 쪽만** 바꾼다 (결과서, PE 바이트).
- `CsrParseServerCommandLine` 은 [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 에서 "명령줄의 `ServerDLL=` 을 읽는 함수" 로 이미 만났다. 같은 함수가 `ObjectDirectory=` 도 읽는 것이다.
- **run 문서 보탬 ③** — 어느 `\Windows` 인가 (단계별로):
  1. `CsrParseServerCommandLine.c` 88 ~ 95줄: 서비스 세션이 **아니면** 방 이름을 명령줄 값 그대로 쓰지 않고 `"%ws\%ld%s"` 꼴로 **세션 번호를 넣어 새로 만든다** (`+0x15b`) `[정적: .c]`.
  2. 그래서 → **`\Windows` 자체를 고치는 건 서비스 세션 (0) 의 csrss** 이고, 세션 1 csrss 는 세션 아래 이름을 고친다 `[정적: .c]`.
  3. 그 이름이 `\Sessions\1\Windows` 꼴인지는 `[추측]` — 형식 인자 셋을 다 안 따라갔다 → Q185 ①.
  - 명령줄 `ObjectDirectory=` 값이 정말 `\Windows` 인지도 장부 (`SubSystems`) 몫이라 `[확인 필요]` (결과서) → 동적 ③.

#### 그래서

- 느슨한 명부 표 = **`\RPC Control` 의 부팅 직후 값** (smss 가 넘긴 것) 이다.
- **`\Windows` 는 csrss 뒤에 값이 달라진다.** 같은 명부로 태어나지만 나중에 허락 쪽이 바뀐다.
- "그 뒤로 `\RPC Control` 은 아무도 안 고친다" 는 **보이는 범위** 의 결론이다. 확정은 부팅 뒤 실제 명부를 뜨는 동적 ① 몫이다.

비유: 관리인이 두 방에 같은 느슨한 명부를 붙였다. 나중에 인사팀 사무실 (csrss) 이 와서 **`\Windows` 방 명부의 허락 쪽만** 갈아 끼운다. `\RPC Control` 방엔 아무도 손대는 걸 못 봤다.

---

### 2.8 커널은 `\RPC Control` 을 만드나 — 아니오 `[정적, 보이는 범위]`

`ntoskrnl.exe` 안 `RPC Control` 글은 **2곳** 이다 (UTF-16 바이트 검색, 결과서).

| 곳 | 쓰는 함수 | 무엇 |
|---|---|---|
| 이름 비교 표 (rva `0x1c90 ~ 0x1cd0`, 5칸 — `Local` · `Global` · `RPC Control` · `Session` · `AppContainerNamedObjects`) | `SepValidateReferencedCachedHandles` `+0x27e LEA` → `+0x296 RtlEqualUnicodeString` | 방을 **만드는** 게 아니라 이름을 **비교** 한다. AppContainer 신분증의 캐시 핸들 이름 검사로 보인다 `[추측: 함수 이름 · 모양]` |
| `\RPC Control\FconAlpcPort` | `CmFcpConnectToAlpcServer` `+0x42 LEA` | **이미 있는** 방 안의 전화선 (ALPC 포트) 에 **붙는** 쪽 |

- 커널이 부팅 때 방을 만드는 곳 (`ObInitSystem +0x557` 루트 · `SepInitializationPhase1 +0x1b8` · `ExInitializeNls +0x28`, `_calls.tsv`) 의 이름엔 `RPC Control` 이 없다.
- 그래서 → **관리사무소는 이 방을 안 만든다. 관리인 smss 가 만든다.**
- 조심: 위 두 줄은 PE 바이트 · `objdump` 로 본 **결과서 값** 이다 (ntoskrnl `.asm` 에 `RPC Control` 0건, 이번에 새 export 없음).

---

### 2.9 같은 함수가 만드는 다른 명부 — 그리고 test.exe 와 닿는 곳 `\KnownDlls`

#### `SmpCreateSecurityDescriptors(1)` 이 한 번에 만드는 명부들 `[정적]`

| 명부 (PDB 이름) | 붙는 곳 | 허락 쪽 | 특수 쪽 |
|---|---|---|---|
| `SmpKnownDllsDirSecurityDescriptor` | **`\KnownDlls` 방** (`SmpOpenKnownDllsHandles +0x18f`) | `S-1-5-32-544` `0x10000000` · `S-1-1-0` · `S-1-15-2-1` · `S-1-5-12` · `S-1-15-2-2` 넷 `0xA0000000` (AclSize 140) | **프로세스 신뢰 라벨** (형 `0x14`, 마스크 `0x20003`, `S-1-19-512-8192`) |
| `SmpKnownDllsSymLinkSecurityDescriptor` | `KnownDllPath` 링크 (`+0x1fc`) | 위와 **같은 ACL 객체** | 없음 |
| `SmpKnownDllSecurityDescriptor` | KnownDll 섹션들 (`SmpInitializeKnownDllsInternal +0xf4`) | `S-1-1-0` · `S-1-15-2-1` · `S-1-15-2-2` · `S-1-5-12` 넷 `0xE0000000` · `S-1-5-32-544` `0x10000000` | 등급 표 `0x11` (마스크 1, `S-1-16-4096`) + 신뢰 라벨 `0x14` (`S-1-19-512-8192`, 마스크 `0x3000f` / `0x2000f` — `SmpHostSmss` 에 따라) |
| `SmpApiPortSecurityDescriptor` | smss 자기 전화선 (`SmpInit +0x38c NtAlpcCreatePort`) | `S-1-5-32-544` · `S-1-5-18` 둘 `0x10000000` | 없음 |

- 전부 주인 · 소속 없음, 허락 줄은 전부 허용 (플래그 0) (결과서 1-8).
- `S-1-15-2-1` · `S-1-15-2-2` 는 결과서가 ALL APPLICATION PACKAGES · ALL RESTRICTED APPLICATION PACKAGES 로 짐작한 이름이다 `[추측: 결과서]`. `S-1-19-512-8192` 는 "프로세스 신뢰 (PPL, WinTcb 수준)" `[추측: 결과서]`.

#### test.exe 와 닿는 곳 — 공용 창고 `\KnownDlls` `[정적]`

단계별로:

1. 부팅 때 smss 가 `SmpLoadDataFromRegistry +0x5df SmpOpenKnownDllsHandles` 를 부른다. 첫 인자 = `SmpKnownDllsObjectDirectoryName` = 글 `\KnownDlls` (PE 바이트 `0x28f70`, `+0x5d8` — flow-writer 가 확인, run 문서 보탬 ①).
2. 그 안 `+0x18f NtCreateDirectoryObject` 가 **`\KnownDlls` 방을 만들고 `SmpKnownDllsDirSecurityDescriptor` 를 붙인다** (속성 `0xd0`).
3. 한참 뒤 test.exe 의 `LdrpInitializeProcess +0xdb8 NtOpenDirectoryObject(&LdrpKnownDllDirectoryHandle, 3, "\KnownDlls")` (run65 `[정적]`) 가 **바로 그 방** 을 연다.
4. 실제로 열린 건 사진이 보여 준다: run44 핸들 `0x4c` = Directory `KnownDlls` `[관찰]`.

- 접근 3 이 이 명부 (`S-1-1-0` `0xA0000000`) 와 신뢰 라벨 (`0x20003`) 을 **통과하는 까닭** 은 디렉터리 형의 generic 풀기 · 라벨 검사 몫이라 `[추측]` — 이번엔 안 봤다.
- 차례도 정해진다: smss 안에서 `\KnownDlls` (`+0x5df`) 는 `\RPC Control` (`+0x55c` 안) **뒤** 다 `[정적]`. 실제 시간 차례는 동적 ⑤.

비유: test.exe 가 run65 에서 연 공용 창고는, 동네가 열리기 전에 **관리인이 미리 세우고 명부를 붙여 둔 그 창고** 였다.

#### 그 밖엔 test.exe 길에 없다 — 그래서 그림은 그대로

- test.exe 는 rpcrt4 를 안 올린다 (run44 `[관찰]`).
- `\RPC Control` 을 다루는 kernelbase 함수 `BasepOpenShadowRpcControlDirectory` 는 **부모 쪽** `CreateProcessInternalW` (AppContainer · LowBox 갈래 — run97) · `UnhandledExceptionFilter` 지도에만 보이고, ntdll `_map/LdrInitializeThunk_*` 엔 없다 (flow-writer 검색).
- `\Windows` 를 직접 여는 줄은 flow.md 2장 test.exe 길에 없다.
- → `\KnownDlls` 도 이미 2.4 에 **있던 syscall 의 대상 객체의 출처** 일 뿐이라, 새 화살표가 생기지 않는다. 그래서 flow.md **그림은 안 고쳤다** (3절).

---

### 2.10 ★ PE_emulator 쪽 — 맞나, 틀리나

run 문서 "지금 PE_emulator 맞나 · 틀리나" 표에서. "지금 우리" 는 결과서가 요청서 배경으로 적은 것이다 (`../PE_emulator` 코드는 안 읽었다).

| 질문 | 진짜 (26100.9457) `[정적]` | 지금 에뮬레이터 | 판정 |
|---|---|---|---|
| 2-가 만드는 곳 · 명부 | smss 표 콜백 → 느슨한 명부 (허락 7 + 등급 표 1, 주인 · 소속 없음) | `\RPC Control` 에 명부 안 채움 | **틀림** — 2.5 표를 방 명부로 |
| 2-나 뒤에 고치나 | smss 안 없음, smss 밖 보이는 범위 못 찾음 (`\Windows` 만 csrss) | 처음 명부도 없음 | 처음 명부만 맞추면 충분해 보임 — Q181 로 확정 |
| 3 커널이 미리 만드나 | 아니오 | 커널 쪽에서 안 만듦 | **맞음** — 에뮬레이터가 smss 몫을 대신해 방을 두면 됨 |

#### run97 의 (a) · (b) · (c) 와 이어 보기 — 단계별로

run97 해설 2.6 에서 "LABEL 만 물었는데 객체에 라벨 줄이 없으면" 세 경우를 봤다. 이번 결과로:

1. 느슨한 명부의 특수 쪽에 **형 `0x11` 줄이 하나 있다** `[정적]`.
2. 그래서 `\RPC Control` 은 (a) · (b) · (c) 어느 것도 아니다. `0x17` · `0x10` 을 물으면 대답에 **등급 표 1줄짜리 특수 쪽이 실린다.**
3. 단, 이건 **커널이 smss 의 특수 쪽을 그대로 저장한다면** 이다 → Q180d (계산) · Q181 (실제 값).

- run97 해설은 "진짜에 SACL 이 없다면 지금 구조로도 바이트가 맞는다" 고 했다. 이번 결과로 **처음 명부엔 SACL 이 있다** 는 쪽으로 기울었다. 에뮬레이터는 특수 쪽 칸을 갖춰야 할 가능성이 크다 `[추측]` (최종은 Q181).

#### 커널이 저장한 꼴은 아직 모른다

- 주인 · 소속은 어느 SID 로 채워지나
- generic 비트 (`0xE0000000` · `0x10000000`) 가 디렉터리 구체 권한으로 바뀌나
- `AclSize` (184 · 32) 가 줄어드나 (예: `0x9c` · `0x1c`)

→ Q180d (`SeAssignSecurityEx2`, run100 몫) · Q181.

---

### 2.11 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

run 문서 "결과서와 다르게 적은 곳 · 보탠 것". **정정은 없다.** 다시 본 오프셋도 모두 일치했다.

- ① (보탬) `SmpKnownDllsObjectDirectoryName` 글을 PE 바이트로 확인 — `\KnownDlls`. 결과서는 "`\KnownDlls` 류" 로 적었다. → 2.9.
- ② (보탬) `SmpLoadDataFromRegistry` 엔 **둘째 `RtlQueryRegistryValuesEx`** 가 있다 — `+0xbc5` (표 `SmpRegistryConfigurationTable2`). 이번 범위 밖 → Q185 ②.
- ③ (보탬) csrss 의 방 이름은 서비스 세션이 아니면 세션 번호가 들어간 글이다. → 2.7 ③.
- ④ (보탬, `[추측]`) `ObjectDirectories` 콜백은 받은 버퍼를 **이중 NUL 까지** 돈다 (`+0x12b ~ +0x13a`). `RtlQueryRegistryValuesEx` 가 여러 줄 값을 줄마다 따로 넘기면서 포인터가 원래 버퍼 안이면 뒤 이름을 또 만들게 된다. 그래도 "있으면 열기" (`OBJ_OPENIF`) 라 열고 닫을 뿐이라 결과는 같다. → Q185 ③ (낮음).

---

### 2.12 디버거 메모 — 함정 둘

- **표 콜백은 정적 지도에 caller 가 없다** (2.3). 멈춤점 콜스택 바로 위는 `ntdll!RtlQueryRegistryValuesEx` 안쪽일 것이고, `smss!SmpLoadDataFromRegistry+0x563` 은 그 위에서 찾아야 한다 `[추측]`.
- **부팅 맨 앞 (smss) 에 멈추는 건 어렵고 위험하다.** 시간 기록 · 타임아웃이 달라질 수 있다 `[추측]`. 결과서 추천대로 **부팅이 끝난 뒤** 전역 방을 덤프로 보는 게 낫다.
  - 세 방 (`\RPC Control` · `\Windows` · `\KnownDlls`) 은 전역 객체라 **test.exe 가 없어도** 볼 수 있다 `[추측]`.
  - 헤더 명부 칸은 run97 함정 그대로 — 객체 `-8` 값 `& ~0xf`:
    ```
    !sd (poi(<객체>-8) & 0xfffffffffffffff0)
    ```
- 디버거가 붙어서 달라지는 점 `[추측]`: 이번에 읽은 smss 줄 안엔 디버거 칸을 읽는 줄이 없다.
- 함정 후보와 도구 제안 (`run_ghidra.sh callers <모듈> <import>` 모드 — `refs` 가 import 이름엔 0줄이라서) 은 하네스라 **제안** 으로만 올라갔다 (plan.md 결정 기록 — 오케스트레이터가 사용자에게 따로 묻는다).

---

### 2.13 정리 — 비유로 한 번 더

- 동네 (이름 공간) 가 열리기 전에 **관리인 smss** 가 먼저 온다.
- 관리인은 출근하자마자 **명부 여섯 벌** 을 써 둔다. 처음부터 "엄격 모드" 로 쓴다 (파일 처음 값 1).
- 관리실 설정 장부의 "Session Manager" 쪽을 펼친다. **항목 → 담당자 배정표** 를 따라:
  - `ProtectionMode` 담당이 모드를 확인한다. "엄격" 이면 명부를 한 번 더 고쳐 쓰고, "끔" 이면 **그냥 지나간다.** 그래서 어느 쪽이든 엄격 명부.
  - `ObjectDirectories` 담당이 적힌 이름마다 관리사무소 창구에 **방 만들기** 를 신청한다. 이름이 `\RPC Control` · `\Windows` 면 **느슨한 명부** (누구나 읽고 쓰기, 관리자는 전부, 등급 표 "낮음 · 위로 쓰기 금지"), 아니면 **기본 명부**.
  - 명부의 **주인 · 소속 칸은 비워서** 낸다. 관리사무소가 채울 것이다 (짐작).
  - 방은 **영구** 라서, 관리인은 손잡이를 바로 놓는다.
- 이어서 관리인은 **공용 창고 `\KnownDlls`** 방도 세운다. 한참 뒤 test.exe 가 run65 에서 여는 그 창고다.
- 나중에 **인사팀 사무실 (csrss)** 이 와서 `\Windows` 방 명부의 허락 쪽만 갈아 끼운다. `\RPC Control` 방엔 아무도 손대는 걸 못 봤다.
- 관리사무소 (커널) 는 `\RPC Control` 방을 **만들지 않는다.** 이름을 비교하거나, 그 방 안 전화선에 붙을 뿐이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

**그림 (1장) 은 안 바뀌었다.** smss 는 부팅 때 도는 다른 프로세스라 test.exe 초기화 지도 앞 · 밖이고, test.exe 와 닿는 `\KnownDlls` 도 이미 있던 syscall 의 **대상 객체의 출처** 라 화살표가 새로 생기지 않는다 (2.9). 태그가 올라간 줄도 없다. 새 줄만 생겼다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | smss.pdb 줄 없음 | **smss.pdb 줄 새로** (run98 — 처음 analyze, 함수 325, 지도 152 함수, export 10). csrsrv.pdb · ntkrnlmp.pdb 줄에 run98 몫 (읽기만) |
| 근거 로그 | run97 까지 | **run98 줄** — 정적 바퀴, 사진 대조 없음 (부팅 때 일이라 run44 사진 구간 앞, 객체 헤더 SD 는 사진에 칸이 없음), 에뮬레이터 숫자는 우리 `[관찰]` 아님 |
| 흐름도 앞 글 | run97 문단까지 | **run98 문단** — "그림을 안 고쳤다" 와 그 이유 |
| 2.4 제목 · run65 `\KnownDlls` 절 | `\KnownDlls` 를 **연다** 까지 (run65 `[정적]`) | **(run98) `[정적]` 한 줄** — 그 방을 부팅 때 smss 가 만든다 (`SmpOpenKnownDllsHandles +0x18f`), 명부 = 허락 5 + 신뢰 라벨 `0x14` `0x20003`. 접근 3 통과 까닭은 `[추측]` |
| 3장 run97 "보안 정보 묻기" 절 | 라벨 줄 없을 때 (a) · (b) · (c) | **(run98) `[정적]` 한 줄** — `\RPC Control` 처음 명부엔 등급 표가 있다 → (a) · (b) · (c) 가 아님 (커널이 그대로 저장한다면) |
| 3장 | (없음) | **새 절 "부팅 때 smss 가 만드는 객체 디렉터리와 처음 SD"** — 게시판 · 관리인 비유, 호출 트리, `[정적]` 여덟 줄, `[추측]` 한 줄 (커널이 저장할 때 바꿀 수 있는 것) |
| 4장 | — | run97 줄에 (run98) 보탬 + **run98 새 줄 넷**: 동적 (→ Q181) · 정적 (→ Q180d · Q185) · **짐 지도 후보** · PE_emulator 와 보탠 것 |
| 5장 | run97 까지 | run 표 · 요청서 번호 **run98 줄** |

### 짐 지도 `baggage.md` — 안 고침. 후보는 "사용자와 정할 것"

- 짐 지도가 지금 세는 부팅 쪽 짐은 둘이다: **커널이 부팅 때 놓는 전역** (7.6.2 `Ⓑ` · 7.6.3) 과 **csrss 몫** (7.6.11 "앞").
- smss 가 만드는 방과 그 **객체 헤더 명부** 는 둘 다 아니다. 그리고 **객체 헤더 명부 칸 자체가 지금 짐 지도에 없다** (run97 과 같은 판단).
- 그래서 flow.md 4장 run98 줄에 **후보로만** 적었다:
  - 후보: `\RPC Control` · `\Windows` (느슨한 명부) · `\KnownDlls` 방 · `KnownDllPath` 링크 · `\Sessions` (기본 명부) · KnownDll 섹션 명부
  - run97 이 남긴 후보 (토큰 · 프로세스 객체 헤더 명부, Q184 ②) 와 짝이다.
- **넣을지, 넣으면 어느 단계 표시로 넣을지는 사용자와 정할 것** 이다. plan.md 에도 "사용자 결정 대기 (묶음 끝에 물음)" 로 올라가 있다.

### 큰 그림에서 어떻게 이어지나

- **PE_emulator 쪽** — 이제 `\RPC Control` 방에 넣을 **원본 명부** 가 생겼다 (2.5 표, 바이트는 run 문서).
  - run97 의 "답하는 규칙" + 이번의 "원본 명부" 가 합쳐지면 `NtQuerySecurityObject(0x17)` 대답을 **만들 재료** 가 거의 다 모인다.
  - 남은 한 칸: 넘긴 명부가 **방에 붙을 때 커널이 무엇을 바꾸나** → Q180d (run100 예정).
  - 그 대답을 받은 rpcrt4 가 무엇을 보고 `0xe` 로 가나 → Q180c (run99 예정).
- **우리 지도 쪽** — test.exe 줄기에 직접 더할 건 없다. 대신 run65 에서 연 **공용 창고의 출처** 가 처음으로 정적으로 이어졌다. "test.exe 가 여는 전역 객체는 누가 언제 만들었나" 를 따라간 첫 예다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q181** (동적, run98 몫 ① ~ ⑦)

이번 바퀴엔 VM 을 안 썼다. Q181 은 Q104 VM 단계에 묶을 후보다. 세 방은 전역 객체라 **test.exe 없이 부팅 뒤 덤프로 볼 수 있을 것** 이다 `[추측]`.

| 번호 | 무엇을 모르나 | 왜 이번에 못 알았나 |
|---|---|---|
| ① | 부팅 직후 **`\RPC Control` 의 실제 명부** — 주인 · 소속 SID, 마스크가 generic 그대로인가, `AclSize` (`0xb8` · `0x20` 그대로 / 줄었나), Control (`0x8000` 과 `0x14` 를 합친 값에 다른 비트가 더 있나), 등급 표 줄, **고유 SID 가 섞인 줄** (로그온 · 서비스 SID — 정적으론 없음. 있으면 뒤에서 누가 붙인 것) | 정적은 smss 가 **넘긴** 것까지. 커널이 저장한 꼴은 실행 때 |
| ② | 같은 때 **`\Windows` 명부** — 느슨한 명부와 csrss 뒤 값의 차이 | `CsrSetDirectorySecurity` 본문을 안 읽었다 |
| ③ | 타깃 VM 장부 — `ProtectionMode` (1 인가), `ObjectDirectories` (정확히 두 줄인가 — 더 있으면 그건 기본 명부), csrss 명령줄 `ObjectDirectory=` 값 | 레지스트리 값은 실행 환경 몫. 에뮬레이터 쪽 덤프는 우리 `[관찰]` 이 아님 |
| ④ | **자식 상속** — `\RPC Control\epmapper` 같은 포트 명부에 상속 전용 줄 (3 ~ 6) 이 어떻게 들어가나 | 상속 계산은 커널 몫. 급하지 않음 |
| ⑤ | **시간 차례** — `\RPC Control` 이 `\KnownDlls` 보다 정말 먼저인가 | 사슬 차례는 정적으로 정했지만, 실제는 실행 때 |
| ⑥ | **"있으면 열기" 로 열기만 했나** — smss `+0xf1` 반환이 `0` 인가 `0x40000000` 인가 | 부팅 맨 앞이라 멈추기 어렵다. 현실적인 대안은 ① 의 덤프 |
| ⑦ | (보탬) **`\KnownDlls` 방 명부** — 2.9 표 (허락 5 + 신뢰 라벨 `0x14` `0x20003`) 와 대조 | test.exe 가 접근 3 으로 여는 그 방이라 짐 지도 후보와 짝 |

### 정적으로 남은 것

run 문서 "아직 확인 필요 — 정적" ① ~ ⑥ 이 대기열에서 이렇게 갔다.

| run 문서 | 대기열 | 무엇 | 왜 이번에 못 봤나 |
|---|---|---|---|
| ① | **Q180d** (run100 예정 — 이름만) | ntoskrnl `SeAssignSecurityEx2` (rva `0x43d980`) — 주인 · 소속 채우기, generic → 구체 권한, `AclSize`, 등급 표를 그대로 두나. 이번 느슨한 명부를 입력으로 | 이번 범위 밖 (커널 쪽 계산) |
| ② | **Q185 ①** | csrsrv `CsrSetDirectorySecurity` (rva `0x7570`, 859B) 본문 — `\Windows` 가 csrss 뒤 어떤 허락 쪽이 되나. `+0x15b` 세션 이름 형식 인자도 | 이번엔 이름 · rva 만 봤다 (결과서는 PE 바이트로 몇 줄만) |
| ⑥ | **Q185 ②** (작음) | `SmpLoadDataFromRegistry +0xbc5` 둘째 표 `SmpRegistryConfigurationTable2` 칸 | 이름만 봤다 (2.11 ②) |
| ④ | **Q185 ③** (낮음) | ntdll `RtlQueryRegistryValuesEx` 가 여러 줄 값을 콜백에 통째로 주나 줄마다 주나 | 명부 결과엔 영향 없음 `[추측]` (2.11 ④) |
| ③ | **Q185 ④** (낮음) | `AppXDeploymentServer` · `usermgr` · `win32kfull` 이 `\RPC Control` 명부를 고치나 | 새 모듈 analyze 필요. **동적 ① 값이 느슨한 명부와 다를 때만** |
| ⑤ | **Q180c** (run99 예정 — 이름만) | rpcrt4 가 이 명부에서 무엇을 보나 | 다른 조각 |

### 같은 요청의 다음 조각 — 다음 바퀴에서 이어짐

- **Q180c** (rpcrt4, run99) — 명부 사본을 **받는** 쪽.
- **Q180d** (run100) — 넘긴 명부가 **방에 붙을 때** 의 계산.

### 사용자와 정할 것

- 짐 지도에 **객체 헤더 명부** 를 넣을지 — 후보 (run98) `\RPC Control` · `\Windows` · `\KnownDlls` · `KnownDllPath` · `\Sessions` · KnownDll 섹션 명부 + (run97) 토큰 · 프로세스 객체 헤더 명부. 넣으면 단계 표시는 무엇으로 (3절).

---

## 5. 더 읽기

- run 문서: [`run98-rpc-dir-create.md`](run98-rpc-dir-create.md)
  - "PE_emulator 쪽에서 쓸 것" — 결론 · **`\RPC Control` 의 처음 SD** 표 (바이트 포함) · 맞나 틀리나 표
  - "정적 — 호출 지점" · "어디서 읽고 어디서 부르나" · "SD 를 만드는 함수" · "같은 함수가 만드는 다른 SD" · "만든 뒤 고치는 곳" · "커널은 만드나" · "test.exe 길에 있나" · "호출 관계"
  - "아직 확인 필요" — 동적 ① ~ ⑦, 정적 ① ~ ⑥
- 결과서: [`_workspace/sast/94_rpc-dir-create_result.md`](../../_workspace/sast/94_rpc-dir-create_result.md) — "한눈에", 질문 1 (1-1 ~ 1-8), 질문 2 (2-1 · 2-2), 질문 3
- 요청서: [`_workspace/sast/94_rpc-dir-create.md`](../../_workspace/sast/94_rpc-dir-create.md) — 배경 (에뮬레이터 쪽 관찰)
- flow.md: [`flow.md`](flow.md)
  - 3장 "부팅 때 smss 가 만드는 객체 디렉터리와 처음 SD — … (run98)"
  - 3장 "보안 정보 묻기" 절의 (run98) 한 줄
  - 2.4 run65 `\KnownDlls` 절의 (run98) 한 줄
  - 4장 run98 줄 넷, 5장 run98 줄
- 앞 해설:
  - [run97 해설](run97-rpc-sd-query-해설.md) — 출입 명부 · 신뢰 등급 표 · 커널이 "명부 사본" 에 답하는 길, (a) · (b) · (c)
  - [run65 해설 2.3](run65-ldr-init-bodies-해설.md) — test.exe 가 `\KnownDlls` 를 여는 줄
  - [run71 해설 2.2](run71-named-obj-dir-해설.md) — 공용 사물함 · 방, 명부를 **만드는** 다른 예 (`DBWinMutex`)
  - [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) — csrss 와 `CsrParseServerCommandLine`
  - [run57 해설](run57-nls-apiset-해설.md) — 영구 개체
