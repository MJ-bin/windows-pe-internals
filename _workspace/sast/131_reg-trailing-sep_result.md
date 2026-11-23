# 131 reg-trailing-sep — 정적 분석 결과

- 요청서: `_workspace/sast/131_reg-trailing-sep.md` (원문 `../PE_emulator/_workspace/05_tuning_planner/282_internals-reg-trailing-sep.md` 도 읽음)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), 26100.9457
- PDB GUID 확인: `ms/sym/ntoskrnl.exe.pdb.info.txt` 의 `guid_age=C29EBFB06B78B3C020DCA66D99713F9E1` = 기준 GUID 와 같음. 프로젝트 `ms/ghidra/ntoskrnl.analyze.log` 에 `SetPdb: ntoskrnl.exe <- ms/sym/ntoskrnl.exe.pdb`. 이미 analyze 된 프로젝트에서 `^이름$` export 만 했다 (analyze 다시 안 함).
- 버전 확인 (알려진 사실): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치. 요청서가 인용한 `ObpLookupObjectName +0x8ce/+0x8d3 → CmpParseKey` 도 `_calls.tsv` 와 일치. → 일치.
- 스크립트 문제: 없음.
- 뽑은 함수 (전부 depth 0, 로그 `ms/ghidra/ntoskrnl.export.sast131a~e.stdout`, refs `…refs.sast131_CmpGetNextName.stdout`):
  - 요청 대상: `CmpGetNextName` · `CmpParseKey` · `CmpDoParseKey` (`.c` + `.asm`). `ObpLookupObjectName.asm` 은 이미 있던 것을 읽음.
  - **범위를 넓힌 것과 이유**: 요청서는 `CmpParseKey → CmpDoParseKey → CmpGetNextName` 길을 가정했는데 그 길이 없다 (아래 "먼저 읽는 한 줄 답" 0번). 그래서 진짜 이름 자르는 함수를 찾으려고 `_calls.tsv` 로 한 단계씩만 더 뽑았다: `CmpComputeComponentHashes` · `CmpGetComponentNameAtIndex` · `CmpWalkOneLevel` · `CmpGetSymbolicLinkTarget` (링크 다시 파싱 한 줄) · `CmpWalkUnicodeStringPath`/`CmpWalkPath`/`CmpFindHiveSubKey`/`CmpGetKnownHivePathNode` (`CmpGetNextName` 부르는 자리 확인) · `NtOpenKey`/`NtOpenKeyEx`/`NtCreateKey`/`CmOpenKey`/`CmCreateKey`/`ObpCaptureObjectName` (만들기·앞단 이름 검사 확인).
  - 요청서의 depth 1 은 안 썼다: `CmpDoParseKey` 는 호출 줄이 264 개 (서로 다른 callee 101 개) 라서 필요한 것만 위처럼 골라 뽑았다.
- asm 위치: `ms/disassemble/ntoskrnl/<함수>.asm`, `ms/decomp/ntoskrnl/<함수>.c`. 호출 표 `ms/disassemble/ntoskrnl/_calls.tsv`.

## 먼저 읽는 한 줄 답

0. **요청서 길 가정이 틀림**: `CmpDoParseKey` 는 `CmpGetNextName` 을 부르지 않는다. 사용자가 넣은 레지스트리 이름은 `CmpGetNextName` 을 타지 않는다. 이름을 다듬는 곳은 둘: **`CmpParseKey` 앞머리** (끝·앞 `\` 떼기) 와 **`CmpComputeComponentHashes`** (조각 나누기, 가운데 `\\` 한 칸으로). `CmpGetNextName` 부르는 곳은 세 자리뿐이고 전부 하이브 안 고정 경로 걷기다 (`CmpWalkUnicodeStringPath` · `CmpGetKnownHivePathNode` · `CmpFindHiveSubKey`).
1. **절대 이름 끝 `\` 하나 → 예, 끝 `\` 를 떼고 그 키를 연다.** 키가 있으면 성공. status 안 만든다 (`0xc0000033` 아님). `CmpParseKey +0x112/+0x118 → +0x5fa` 가 끝 `\` 를 뗀다.
   - `CmpGetNextName` 규칙 (참고): 앞 `\` 는 전부 건너뜀. 빈 조각 = 반환 TRUE + 출력 조각 Length 0·Buffer NULL + `last=1`. "마지막 조각" = 조각을 뗀 뒤 입력의 남은 길이가 0.
   - `CmpDoParseKey`: 남은 이름 길이 0 → 조각 0개 → 시작 키 자체를 연다 (`+0x2df/+0x2e3 → +0x122d`). 오류 아님.
2. **끝 `\\` 둘 → 예, 같은 규칙** (`CmpParseKey` 가 끝 `\` 를 "전부" 뗀다). **가운데 `\\` → 예, 거절 아님**: 연속 `\` 를 한 칸으로 본다 (`CmpComputeComponentHashes +0x25b~+0x278`). 거절·status 없음.
3. **상대 이름 `Key\` → 예, 같은 줄을 탄다.** `ObpLookupObjectName` 이 이름을 손대지 않고 통째로 `CmpParseKey` 에 넘긴다 (`+0x20d`, `+0x8bb`, `+0x8ce`). 절대와 다른 점은 남은 이름에 앞 `\` 가 있나 없나뿐 (`CmpParseKey` 가 앞 `\` 도 전부 뗌). 덤: 상대 빈 이름 → 같은 키를 다시 연다 (새 핸들). 상대 이름이 `\` 로 시작 (`\` 하나 포함) → 키 핸들 기준이면 `0xc000003b`, `CmpParseKey` 까지 안 간다.
4. **지금 PE_emulator**: 절대 끝 `\` 에 `0xc0000033` → **틀림**. 상대 갈래 (끝 `\` 하나만 뗌) → 결과는 맞고 규칙이 좁음 (`\\` 둘 이상은 틀림). 만들기 (`nt_create_key.cpp:48`) 끝 `\` `0xc0000033` → **틀림**. 아래 "지금 우리" 절.

## 표: 이름 꼴 → 결과

(`CmpParseKey` = 약칭 PK, `CmpComputeComponentHashes` = CCH, `ObpLookupObjectName` = OLN. 줄은 `ms/disassemble/ntoskrnl/<함수>.asm` 의 `+오프셋`.)

| 이름 꼴 (예) | 조각 차례 | 빈 조각 처리 | 결과 | 줄 |
|---|---|---|---|---|
| 절대 끝 `\` 하나 `\Registry\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\` | OLN 폴더 걷기가 `Registry` 한 조각 → 레지스트리 루트 객체에서 PK 호출, 남은 이름 `\Machine\…\CommonGlobUserSettings\` (끝 `\` 그대로). PK 가 끝 `\` 떼고 앞 `\` 뗌 → `Machine\System\CurrentControlSet\Control\CommonGlobUserSettings` → CCH 가 5 조각 | PK 가 먼저 떼서 빈 조각이 안 생김 | **연다** (키 있음 → 성공. 없으면 `0xc0000034` 줄이 `CmpDoParseKey` 에 여러 곳 — 아래 공백) | OLN +0xa3e·+0xa78·+0xab0 (남은 이름 만들기), +0x8bb/+0x8ce (PK 호출) · PK +0x60~+0x8c (이름 복사), +0xf7~+0x118 (끝 `\` 검사), +0x5fa~+0x609 (떼기 루프), +0x11e~+0x12c·+0x49c~+0x4c0 (앞 `\` 떼기), +0x28f~+0x2e4 (잘린 이름으로 `CmpDoParseKey` 호출) |
| 절대 끝 `\\` 둘 `…\CommonGlobUserSettings\\` | 위와 같음 | 루프가 둘 다 뗌 | **연다** | PK +0x603 (`JNZ` → +0x10c 되돌아감) |
| 가운데 `\\` `…\Control\\CommonGlobUserSettings` | PK 는 안 건드림. CCH 가 `Control` 을 기록한 뒤 연속 `\` 를 전부 건너뜀 → 조각 5 | 가운데 빈 조각 안 생김 (건너뜀) | **연다** (거절 아님) | CCH +0x1a9/+0x1ae (`\` 검사), +0x25b~+0x278 (연속 `\` 건너뛰기 루프) |
| 절대 `\Registry\\Machine\…` (루트 바로 뒤 `\\`) | PK 가 앞 `\` 를 전부 뗌 | — | 연다 | PK +0x128~+0x12c, +0x49c~+0x4ba |
| 절대 맨 앞 `\\Registry\…` | OLN 폴더 걷기: 앞 `\` 하나만 떼고(+0xa78) 다음이 또 `\` 라 조각 길이 0 | OLN 이 거절 | **`0xc0000033`** (키 코드까지 못 감) | OLN +0xa78·+0xab0·+0xae5 → +0xe00 |
| 상대 `Key\` (RootDirectory = 키 핸들) | OLN: 이름 통째 복사해 PK 에 넘김 (앞 `\` 없음, 끝 `\` 그대로) → PK 가 `Key` 로 다듬음 → CCH 1 조각 | 같음 (PK 가 뗌) | **연다** (절대와 같은 줄) | OLN +0x132 (루트 있음 → +0x1a0), +0x1cb (핸들 → 객체), +0x20a/+0x20d (이름 통째 복사), +0x226, +0x8bb/+0x8ce · PK 위와 같음 |
| 상대 `Key\\` | 위와 같음 | 루프가 둘 다 뗌 | **연다** | PK +0x603 |
| (덤) 상대 빈 이름 (Length 0) | OLN 이 폴더 객체 확인 (`+0x1744`) 후 아니면 계속 → PK 에 길이 0 | 조각 0개 → 시작 키 다시 참조 | **같은 키를 다시 연다** (새 핸들) | OLN +0x1eb/+0x1ef → +0x1744/+0x1760 → +0x203 · PK +0xf7/+0xff · `CmpDoParseKey` +0x2df/+0x2e3 → +0x122d (`CmpReferenceKeyControlBlockUnsafe`) · CCH +0x2f/+0x33 → +0x15b~+0x162 (개수 0) |
| (덤) 상대 `\` 하나 / `\Key` (키 핸들 기준) | OLN 앞머리에서 끝: 첫 글자가 `\` 이고 루트가 파일 객체가 아님 | — | **`0xc000003b`** (STATUS_OBJECT_PATH_SYNTAX_BAD) | OLN +0x1f9/+0x1fd → +0x1282, +0x129b/+0x129e (파일 객체면 통과), +0x12ac (`0xc000003b`) |
| (덤) 절대 빈 이름 (RootDirectory 없음, Length 0) / `\` 로 안 시작 | OLN 앞머리에서 끝 | — | `0xc000003b` (OLN 안에서만 본 것 — NtOpenKey 앞단엔 이름 검사 줄이 안 보임) | OLN +0x132~+0x148 → +0x12ac |
| (덤) 홀수 길이 이름 | `ObpCaptureObjectName` 이 이름 복사 전에 거절 | — | `0xc0000033` (에뮬레이터가 이미 함 `nt_open_key.cpp:31`) | `ObpCaptureObjectName.asm` +0xc0/+0xc4, +0xca/+0xd0 → +0x1d2 |

## 질문별 답 [정적]

### 질문 1. 절대 이름 끝 `\` 하나

- 답: **끝 `\` 를 떼고 그 키를 연다.** `CmpParseKey` 가 `CmpDoParseKey` 부르기 전에 남은 이름 (복사본) 의 끝 `\` 를 전부 떼고, 이어서 앞 `\` 도 전부 뗀다. status 는 만들지 않는다.
- 근거:
  - 남은 이름 복사: `CmpParseKey.asm` +0x60 (`MOV RAX,[RBP+0x130]` = 7번째 인자 = 남은 이름 포인터), +0x86 (`MOVUPS XMM2,[RAX]`), +0x8c (`MOVAPS [RSP+0x50],XMM2`). 이 뒤로는 복사본 `[RSP+0x50]` 만 만진다 (Length = `[RSP+0x50]`, Buffer = `[RSP+0x58]`).
  - 길이 0 이면 다듬기 건너뜀: +0xf7 (`MOVZX ECX,[RSP+0x50]`), +0xff (`JZ` → +0x132).
  - **끝 `\` 떼기**: +0x112 (`CMP word [RDX+RAX*2-2],0x5c` = 마지막 글자가 `\`?), +0x118 (`JZ` → +0x5fa), +0x5fa (`ADD CX,R8W`, R8W = 0xfffe → Length −2), +0x603 (`JNZ` → +0x10c 로 되돌아가 다시 검사 → 끝 `\` 전부), +0x609 (길이 0 이면 +0x132).
  - **앞 `\` 떼기** (끝이 `\` 가 아니게 된 뒤): +0x123 (MaximumLength 읽음), +0x128 (`CMP word [RDX],0x5c`), +0x12c (`JZ` → +0x49c), +0x49c~+0x4b2 (Buffer +2, Length −2, MaximumLength −2), +0x4ba (`JNZ` → +0x128 되돌아감), +0x4c0 (`JMP` → +0x132).
  - 다듬은 복사본을 `CmpDoParseKey` 에 넘김: +0x28f (`MOVAPS XMM0,[RSP+0x50]`), +0x2bf (`LEA RAX,[RBP+0x80]`), +0x2dc (`MOVDQA [RBP+0x80],XMM0`), +0x2e4 (`CALL CmpDoParseKey`, ret +0x2e9). `CmpDoParseKey` 쪽에서 그 포인터 = 6번째 인자: `CmpDoParseKey.asm` +0xe1 (`MOVUPS XMM0,[R12]`, R12 = `[RBP+0x178]`), +0xef (`MOVUPS [RBP-0x8],XMM0`), +0x213 (`LEA RCX,[RBP-0x8]`), +0x217 (`CALL CmpComputeComponentHashes`, ret +0x21c).
  - OLN 이 끝 `\` 를 안 건드림: 이름 길이를 줄이는 비교는 `ObpLookupObjectName.asm` 안에 "첫 글자가 `\`" 검사뿐이다 (`CMP word [..],0x5c` 가 +0x144, +0x1f9, +0x24f, +0x547, +0x6d9, +0xa2e, +0xa78, +0xab0, +0xd90, +0xde4 — 전부 앞 글자, 마지막 글자 (`Length−2`) 를 보는 줄 없음).
  - 앞단: `NtOpenKey +0x7/+0xc → NtOpenKeyEx` · `NtOpenKeyEx +0x74 (LEA RCX,[0x140999460] = CmOpenKeyCallout), +0x7b/+0x80 KeExpandKernelStackAndCallout` · `CmOpenKey +0x244/+0x249 → ObOpenObjectByNameEx` · `ObOpenObjectByNameEx +0xfe/+0x103 → ObpCaptureObjectCreateInformation` · `+0x21f/+0x224 → ObpLookupObjectName`. `NtOpenKeyEx`·`CmOpenKey`·`NtCreateKey`·`CmCreateKey` `.asm` 에 `0x5c` 비교도 `0xc0000033`·`0xc000003b` 상수도 없다 (grep). `ObpCaptureObjectName` 은 이름을 그대로 복사하고 (+0x1c3/+0x1c8) 홀수 길이·0xfffe 만 `0xc0000033` (+0xc0~+0xd0 → +0x1d2).
- `CmpGetNextName` 규칙 (질문 1 의 세부, 이 길에서는 안 쓰임):
  - 인자: `RCX` = 남은 이름 (UNICODE_STRING, 제자리에서 줄임), `RDX` = 출력 조각, `R8` = 출력 `last` (BYTE). 반환 BYTE.
  - 앞 `\` 건너뛰기: +0x2d (`CMP [RAX],0x5c`), +0x33~+0x44 (Length −2, Buffer +2 를 입력에 바로 씀), +0x50 (`JNZ` → +0x2d, 길이가 0 이 될 때까지).
  - **빈 조각**: Buffer NULL (+0xf/+0x12), Length 0 (+0x14~+0x1c), 첫 글자가 NUL (+0x1e/+0x22), 또는 `\` 만 남아 길이가 0 이 됨 (+0x50) → 전부 +0x52: `*last = 1`, +0x56 `AL = 1` (TRUE), +0x58/+0x5c 출력 조각 Buffer = 0·Length = 0 (MaximumLength 는 안 씀).
  - 조각 자르기: +0x67 (출력 Buffer = 조각 시작), +0x6b~+0x8d (`\` 나 끝 나올 때까지 입력을 한 글자씩 줄임), +0x8f~+0x9f (출력 Length = 입력 Buffer 새 값 − 출력 Buffer, 아래 16 비트만).
  - **마지막 조각 표시**: +0x98 (`TEST DX,DX`, DX = 조각을 뗀 뒤 입력의 남은 Length), +0xa4 (`SETZ AL`), +0xa7 (`MOV [R8],AL`) → 남은 길이가 0 일 때만 `last = 1`. 그래서 `Key\` 는 조각 `Key` 에서 `last = 0` 이고, 다음 호출이 빈 조각 + `last = 1` 을 준다.
  - 반환 FALSE: 조각 Length > 0x200 바이트 (+0xaa `MOV EAX,0x200`, +0xb2 `SETBE AL`).
  - 부르는 곳 (`refs` 로 확인, 코드 참조 3곳 + 데이터 참조 1곳은 `.pdata` 칸): `CmpWalkUnicodeStringPath +0x39` · `CmpGetKnownHivePathNode +0x67` · `CmpFindHiveSubKey +0x56`. `CmpWalkUnicodeStringPath` 는 빈 조각 (+0x3e `CMP word [RBP-0x20],0`) 을 "경로 끝" 으로 보고 그때까지 간 칸을 돌려준다 (+0x42 `JZ` → +0xa6). 그 위 호출자: `CmpWalkPath` ← `CmpGetSystemControlValues`·`CmpPreserveSystemHiveData`·`CmSelectQualifiedInstallLanguage`; `CmpFindHiveSubKey` ← `CmpFindStateSepKeysRedirectionMapNode`·`CmpGetKnownHivePathNode` ← `CmpFindRedirectedDriverServiceStateNode` (각 `.c` 머리 `callers:`). 전부 이름이 고정된 하이브 안 걷기.
- `CmpDoParseKey` 가 빈 조각 / 길이 0 을 받았을 때:
  - 길이 0 (조각 0개): CCH +0x2f/+0x33 → +0x15b~+0x162 (개수 0 으로 돌려줌) → `CmpDoParseKey +0x2df/+0x2e3` (`CMP R12W,R14W` 같으면 `JZ` → +0x122d) → +0x122d `CmpReferenceKeyControlBlockUnsafe` → 시작 키 자체를 연다. 오류 아님.
  - 길이 0 짜리 조각 한 개 (CCH 가 끝 `\` 이름에서 마지막에 하나 더 만듦: +0x25b~+0x278 루프가 `\` 를 다 먹고 길이 0 이 되면 +0x73 에서 길이 0 조각을 기록): 이 길에서는 `CmpParseKey` 가 먼저 끝 `\` 를 떼서 안 생긴다. 생긴다 해도 `CmpDoParseKey +0x228~+0x256` 의 조각 검사는 길이 > 0x200 만 거절하고 (`CMP word [RAX],R9W`, R9D = 0x200, `JA` → +0x2167 → +0x216c `0xc000000d`) 길이 0 거절 줄은 못 찾았다. 그 뒤 `CmpWalkOneLevel` 안은 안 읽음 → `[공백]`.
  - 조각 수·길이 상한: CCH +0x19f/+0x1a3 (조각 32 개 넘으면 남은 길이 확인 → +0x3b6~+0x3c0 `0xc000000d`), 조각 길이 > 0x200 바이트 (256 글자) 는 `CmpDoParseKey +0x256 → +0x216c` `0xc000000d`.

### 질문 2. 끝 `\\` 둘 · 가운데 `\\`

- 답: **끝 `\\` = 같은 규칙으로 연다. 가운데 `\\` = 연속 `\` 를 한 칸으로 보고 연다. 거절하는 갈래 없음.**
- 근거:
  - 끝 `\\`: 위 끝 `\` 떼기 루프가 `JNZ → +0x10c` 로 되돌아가므로 둘 다 뗀다 (`CmpParseKey.asm` +0x5fa~+0x603).
  - 가운데 `\\`: `CmpParseKey` 는 가운데를 안 본다. `CmpComputeComponentHashes.asm`: +0x1a9/+0x1ae (글자가 `\` 가 아니면 +0x2a4 로 한 글자 전진), `\` 이면 +0x1b4 부터 조각 하나 기록 (길이 = 조각 시작 때 남은 길이 − 지금 남은 길이), 그다음 **+0x25b (`CMP [R15],0x5c`), +0x262 (Length −2), +0x266 (Buffer +2), +0x274/+0x278 (`JNZ` → +0x25b)** 루프가 `\` 를 연속으로 전부 건너뛰고, `\` 가 아닌 글자에서 +0x27a 로 나가 새 조각을 시작한다. 그래서 `Control\\CommonGlobUserSettings` 는 `Control`·`CommonGlobUserSettings` 두 조각이다.
  - 같은 이유로 `\Registry\\Machine…` (루트 바로 뒤 `\\`) 도 `CmpParseKey` 앞 `\` 떼기 루프 (+0x128~+0x4ba) 가 둘 다 뗀다.
  - 거절 status 를 만드는 `\` 관련 줄은 "OLN 폴더 걷기의 길이 0 조각" 하나뿐 (`ObpLookupObjectName.asm` +0xa78 앞 `\` 하나만 떼고 +0xab0~+0xaca 로 다음 `\` 까지 센 뒤 +0xae2/+0xae5 `JZ` → +0xe00 `MOV [RSP+0x60],0xc0000033`). 이 줄은 레지스트리 루트까지 가는 앞부분 (`\Registry` 앞 `\\`) 에만 걸린다. 에뮬레이터 주석(`nt_open_key.cpp:47`) 이 "빈 조각 0xc0000033 은 객체 폴더 갈래 규칙" 이라 한 것은 맞다 — 다만 그 규칙은 `\Registry` 조각 앞까지만이다.
- `[공백]`: 다듬은 뒤에도 (예: 링크 따라간 두 번째 패스의 이어 붙인 이름) 길이 0 조각이 `CmpWalkOneLevel` 에 들어가면 어떻게 되나는 안 읽었다. 이 요청 이름 꼴들은 그 길에 안 닿는다.

### 질문 3. 상대 이름 `Key\`

- 답: **같은 줄을 탄다.** `ObpLookupObjectName` 이 `RootDirectory` (키 핸들) 가 있으면 폴더 걷기를 건너뛰고 이름 전체를 남은 이름으로 `CmpParseKey` 에 넘긴다.
- 근거:
  - `ObpLookupObjectName.asm` +0x132 (`JNZ` → +0x1a0, `RBX` = RootDirectory), +0x1cb/+0x1d0 (`ObpReferenceObjectByHandleWithTag`), +0x1eb/+0x1ef (이름 길이 0 이면 +0x1744), +0x1f9/+0x1fd (첫 글자 `\` 이면 +0x1282), +0x20a (`MOVUPS XMM1,[RSI]`), **+0x20d** (`MOVAPS [RSP+0x70],XMM1` = 남은 이름 = 이름 전체), +0x226 (`JMP` → +0xa43 → 반복문 +0x765), 반복문에서 객체 형식의 파싱 함수가 `CmpParseKey` 면 **+0x8bb** (`LEA RAX,[RSP+0x70]` = 7번째 인자) → **+0x8ce** (`CALL CmpParseKey`, ret +0x8d3).
  - 절대 이름: 같은 `[RSP+0x70]` 를 +0xa3b/+0xa3e 에서 이름 통째로 채우고, 폴더 걷기 (+0xa61~+0xae5) 가 조각 `Registry` 를 먹은 뒤의 나머지 (`\Machine\…\Key\`) 를 남긴다. 그래서 절대는 남은 이름이 `\` 로 시작하고, 상대는 `Key\` 처럼 `\` 로 시작하지 않는다. 끝 `\` 는 둘 다 그대로.
  - `CmpParseKey` 는 이 둘을 같은 줄 (+0xf7~+0x4c0) 로 다듬어서 `Key` 로 만든다. `CmpDoParseKey` 에서도 기준이 루트 키냐 아니냐 (`+0x1e5 JNZ → +0x4bf`) 만 다르고 `R15 == 0` 이면 둘 다 +0x1f4 → +0x217 로 같이 간다 (`+0x4bf/+0x4c2 JZ → +0x1f4`).
  - 에뮬레이터 I02 가 "[추측]" 으로 받은 꼴 ("끝 `\` 하나는 떼고 연다") 의 결과는 맞다. 규칙은 "끝 `\` 전부" 가 맞고, 앞 `\` 가 있으면 `0xc000003b` (아래 덤).
- 덤:
  - **상대 빈 이름** → 같은 키를 다시 연다 (표). `ObpLookupObjectName` 이 루트가 폴더 객체일 때만 따로 처리하고 (+0x1744/+0x1760) 아니면 이름 길이 0 으로 `CmpParseKey` 에 간다.
  - **상대 이름 첫 글자 `\`** (`\` 하나·`\Key`): 루트가 파일 객체가 아니면 `0xc000003b` (+0x1282~+0x12ac). 에뮬레이터는 `_registry_common.hpp` 에서 `registry_path_syntax_unverified` 로 멈춘다 → 이 줄로 닫을 수 있다.
  - 핸들 기준일 때 이름과 무관한 접근 비교가 하나 더 있다 (`ObpLookupObjectName.asm` +0x1dc/+0x1e1 `JNZ` → +0x1ddc~+0x1e04, 실패 시 `0xc0000022`). 뜻은 `[추측]` (핸들 허용 접근 vs 요청 접근).

### 링크 (`CurrentControlSet` → `ControlSet001`) 따라 다시 파싱할 때 남은 이름

- 답: **끝 `\` 를 지니지 않는다.** 이미 잘린 이름에서 나온 조각 배열에서 다시 이어 붙이기 때문.
- 근거: `CmpDoParseKey +0xbe1/+0xbe6`, `+0x17c1/+0x17c6` 가 `CmpGetSymbolicLinkTarget` 을 부름 (조각 번호 범위 인자). `CmpGetSymbolicLinkTarget.asm` 은 링크 대상 문자열 뒤에 조각마다 "`\` 한 글자 쓰기 (+0x35b, +0xc55) + 조각 이름 붙이기 (+0x77d/+0xc36 `CmpGetComponentNameAtIndex`, +0xc62 `RtlUnicodeStringCat`)" 만 한다 — 끝에 `\` 를 덧붙이는 줄 없음. 성공하면 `CmpDoParseKey +0xc22` 가 status `0x104` (STATUS_REPARSE_OBJECT) 를 정한다 (뜻 [추측]). 새 패스에서 `CmpParseKey` 는 다시 이름을 다듬는다.
- `[공백]` 두 번째 패스에 `CmpDoParseKey` 의 `R15 ≠ 0` 갈래 (+0x1ee → +0x4f4 ~ +0x5d4) 를 타면 `CmpParseKey` 가 다듬은 이름이 아니라 `CompleteName` 에서 접두를 뗀 나머지로 `CCH` 를 부른다 (+0x5c0~+0x5d4). 이 `CompleteName` 이 새로 이어 붙인 이름이면 끝 `\` 없음. 진짜 그렇게 되는지는 동적으로 (조각 수) 보는 게 확실 — 아래.

### 만들기 (`NtCreateKey`) 는 같은 파싱인가

- 답: **같은 파싱.** `NtCreateKey +0x4a` (`LEA RCX,[0x140997fc0]` = `CmCreateKeyCallout`), `+0x8a/+0x8f` `KeExpandKernelStackAndCallout` → `CmCreateKey` (`.c` 머리 `callers: CmCreateKeyCallout`) → 조각 `FUN_14099849f +0xa6/+0xab` (= `CmCreateKey +0x3d5`) `ObOpenObjectByNameEx` → 위와 같이 `ObpLookupObjectName` → `CmpParseKey` → `CmpDoParseKey`. `CmCreateKey` 에 이름 `\` 검사 줄이 없다. 마지막 조각이 없으면 `CmpDoParseKey` 가 `CmpCreateChild` 를 부른다 (+0x159c/+0x15a1, +0x1cb3/+0x1cb8, +0x1fa2/+0x1fa7). 끝 `\` 는 `CmpParseKey` 가 먼저 떼므로 만들 조각은 그 앞 이름이다.
- `[공백]` 만들기 때 `CmpCreateChild` 호출 앞 조건 (부모 없음 → `0xc0000034`/`0xc000003a`, 이름 충돌 `0xc0000035`) 은 이 요청 밖이라 안 따라감.

## 지금 우리 (PE_emulator) 맞나·틀리나

(원문 경로는 `ext/syscall/syscall_layer_cpp/src/registry/`. 요청서의 `src/registry/nt_open_key.cpp:31~47` 는 실제로 이 경로. 에뮬레이터 쪽 코드는 읽기만 했다.)

1. `_registry_common.hpp` `resolve_absolute` (끝 `\` → `0xc0000033`, 가운데 빈 조각 `end==at` → `0xc0000033`): **틀림.** 진짜는 끝 `\` 전부 + 앞 `\` 전부 떼고, 가운데 연속 `\` 는 한 칸. (맨 앞 `\\Registry` 는 진짜도 `0xc0000033` 이라 그대로 두기.)
2. `nt_open_key.cpp:47~48` (I02 상대 갈래, 끝 `\` 하나만 뗌: `name.size()>=2 && name.back()==\ && name[size-2]!=\`): **결과 반은 맞고 규칙이 좁음.** `Key\` 는 맞음. `Key\\` 는 진짜는 열리는데 에뮬레이터는 안 떼서 `resolve_relative` 의 `0xc0000033` 에 걸림 (틀림). `Control\\Key` (가운데 `\\`) 도 `resolve_relative` `end==at` `0xc0000033` 이라 틀림. 이 `[추측]` + `TODO(VM)` 는 이 결과서로 닫을 수 있다 (정적 근거까지, 동적 4건은 아래).
3. 상대 이름 첫 글자 `\` → 현재 `registry_path_syntax_unverified` 로 멈춤: 진짜는 키 핸들 기준 `0xc000003b`. 닫을 수 있음 (정적).
4. 상대 빈 이름 (Length 0) → 현재 `resolve_relative` 가 시작 키를 돌려줌 (같은 키): **맞음.**
5. `nt_open_key_ex.cpp` (12줄): 원본 `NtOpenKey` 도 `NtOpenKeyEx` 로 가서 같은 콜아웃을 탄다 (`NtOpenKey +0x7/+0xc`). 같은 규칙 적용.
6. `nt_create_key.cpp:48` (`name.back()==\` → `0xc0000033`) 와 `:100` (`leaf.empty()` → `0xc0000033`): **틀림.** 진짜는 만들기도 같은 `CmpParseKey` 라서 끝 `\` 를 떼고 그 앞 조각을 만든다. `nt_create_key.cpp:72` 가 `resolve_relative` 의 `0xc0000033` 을 그대로 넘기는 것도 같은 이유로 틀림.
7. 홀수 길이 `0xc0000033` (`nt_open_key.cpp:31`): **맞음.** (`ObpCaptureObjectName +0xc0~+0xd0 → +0x1d2`)
8. 고칠 때 정리 (`[추측]` 아님, 위 줄에서 나온 규칙): 이름 → (절대면 `\Registry` 뒤 나머지, 상대면 통째) → ① 끝 `\` 전부 뗌 → ② 앞 `\` 전부 뗌 → ③ 가운데 연속 `\` 는 한 칸 → ④ 조각 0 개면 시작 키 → ⑤ 조각 32 개 넘음 · 조각 길이 > 256 글자 → `0xc000000d`. 이름이 없는 키 → `0xc0000034` 쪽은 이 요청 범위 밖.

## 동적으로 확인할 것

- VM 에서 `NtOpenKey` 넷 (원문 "원하면 한 번"): 이름 꼴만 바꾸고 결과 status 와 핸들만 본다.
  1. 절대 `\Registry\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\`
  2. 절대 끝 `\\`
  3. 절대 가운데 `\\` (`…\Control\\CommonGlobUserSettings`)
  4. 상대 (RootDirectory = `…\Control` 핸들, 이름 `CommonGlobUserSettings\`)
  - 예상 (정적): 넷 다 성공. 덤으로 상대 `\Key` → `0xc000003b`, 상대 빈 이름 → 성공 (새 핸들).
- 추천 멈춤점 (모두 `nt!`, 프로세스 조건 붙일 것):
  - `nt!CmpParseKey` 입구: 남은 이름 = 7번째 인자 = `poi(@rsp+0x38)` → `dS poi(@rsp+0x38)`. 끝 `\` 가 아직 붙어 있는지 (`ObpLookupObjectName +0x8ce` 가 넘기는 값).
  - `nt!CmpDoParseKey+0x217` (call 위치): `RCX` = 다듬은 이름 → `dS @rcx`. 끝 `\` 가 없는지, 앞 `\` 가 없는지.
  - `nt!CmpDoParseKey+0x21c` (ret 위치): `rax` = status, `? wo(@rbp-0x78)` = 조각 개수. 1번 이름이면 첫 패스 조각 5 (`Machine`·`System`·`CurrentControlSet`·`Control`·`CommonGlobUserSettings`) 예상.
  - `nt!CmpDoParseKey+0x5d9` (두 번째 `CCH` 호출, `R15≠0` 갈래): 링크 따라간 두 번째 패스가 이 갈래를 타는지, 타면 조각 끝에 길이 0 짜리가 없는지 (위 `[공백]`).
  - 오프셋은 모두 `ms/disassemble/ntoskrnl/CmpDoParseKey.asm` 의 `ret_off`/호출 줄 기준 (WinDbg 콜스택 `CmpDoParseKey+0x…` 와 같은 기준).
- 정적 한계: 이 결과는 코드를 읽은 것. "실행 때 이 길로 간다" 는 위 멈춤점으로 확인해야 한다 (특히 두 번째 패스, 간접 호출 `cfg` 로 이어지는 갈래는 안 따라감).

## 다음 요청 후보

1. `CmpWalkOneLevel` 의 길이 0 조각 갈래 + `CmpPerformCompleteKcbCacheLookup` (캐시 조회가 빈 조각을 어떻게 보나) — 위 `[공백]` 닫기. 에뮬레이터에 필요한 건 아님 (이 요청 꼴은 안 닿음).
2. 없는 키에서 `0xc0000034` 가 나오는 최종 줄 하나 고르기 + 중간 키 없음 (`0xc000003a`?) 과 구별 — `CmpDoParseKey` 에 `0xc0000034` 가 열 군데 넘게 있어 (+0x972, +0x1890, +0x197a, +0x2068, +0x22c9 …) 갈래를 나눠야 함. 만들기 (`CmpCreateChild` 앞 조건, `0xc0000035`) 와 같이.
3. `CmpParseKey +0x45a~+0x495` 의 "루트 바로 아래 한 글자 이름 (`\Registry\X`, `\Registry\X\…`) → `0xc0000022`" 갈래 (플래그 `0x40` 의 뜻) — 에뮬레이터가 루트 바로 아래 이름을 받을 때 영향 가능 `[추측]`.

## 판정

**분석성공** — 질문 1·2·3 모두 asm 줄 근거로 답함. 요청서의 길 가정 (`CmpGetNextName` 이 이 길 위) 은 틀렸고, 실제로 이름을 다듬는 곳 (`CmpParseKey +0xf7~+0x4c0`, `CmpComputeComponentHashes +0x25b~+0x278`) 을 찾아 답했다. 두 번째 패스 (`R15≠0`) 와 길이 0 조각의 `CmpWalkOneLevel` 쪽은 `[공백]` 으로 남김.
