# 2026-11-23 (작성일, 정적 분석 2026-11-23) run134 — 레지스트리 키 이름 끝 `\` (Q247, 정적만): **끝 `\` 는 떼고 그 키를 연다** (`0xc0000033` 아님). 이름을 다듬는 곳은 **`CmpParseKey` 앞머리** (끝 `\` 전부 · 앞 `\` 전부 떼기) 와 **`CmpComputeComponentHashes`** (가운데 연속 `\` 를 한 칸으로) 둘이다. **요청서 전제가 틀렸다** — `CmpDoParseKey` 는 `CmpGetNextName` 을 부르지 않는다. 상대 이름 (`Key\`) 도 같은 줄, 만들기 (`NtCreateKey`) 도 같은 파싱

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("레지스트리 키 이름 끝에 `\` 가 붙으면 `CmpParseKey` 가 어떻게 대답하나", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/282_internals-reg-trailing-sep.md`, 튜닝 `282_tune07.md` R1, 샘플 2,730 — 병목 아님) 이다. 요청의 이름 (`…\Control\CommonGlobUserSettings\`) 은 에뮬레이터 로그에서 온 것이고 test.exe 초기화 지도엔 없다.
> - 다만 test.exe 초기화도 `NtOpenKey` 를 부른다 — 예: `ntdll!RtlpQueryNlsSystemCodePages +0xb7` 의 `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` (`[정적]` run58). 커널 안에서는 그 이름도 이번에 읽은 같은 사슬 (`NtOpenKey → … → ObpLookupObjectName → CmpParseKey → CmpDoParseKey`) 을 탄다 `[정적: 같은 코드, 그 이름으로 실행 본 적 없음]`. 끝 `\` 가 없는 이름이라 이번 질문의 갈래 (끝 `\` 떼기 루프) 는 그 이름에선 한 번도 안 돈다.
> - 커널의 레지스트리 이름 풀이는 "대표 지점" 이 아니라서 **그림은 안 바꿨다.** flow.md 는 기준 (ntkrnlmp 줄 · 근거 줄) · 2.1 run47 사슬 한 줄 · 2.4 run58 레지스트리 줄 한 줄 · 3장 끝 새 절 · 4장 (Q247 닫힘 줄 · run134 줄 셋) · 5장 (run134 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q247** (사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 첫째). 결과서 판정 **분석성공** — 질문 1 · 2 · 3 모두 `.asm` 줄로 답함. 남긴 `[공백]` 셋 (두 번째 패스 `R15 ≠ 0` 갈래 · 길이 0 조각이 `CmpWalkOneLevel` 에 들어갈 때 · 만들기의 `CmpCreateChild` 앞 조건).
> - 앞 run 과 겹치는 곳: run47 (`ObpLookupObjectName` 이 객체 형식의 Parse 를 **직접 호출** 하는 꼴 — 파일은 `IopParseDevice`). 레지스트리 키는 같은 함수의 다른 자리 (`+0x8ce CmpParseKey`) 다.
> - 남은 동적 → **Q251** (동적: `NtOpenKey` 넷), 남은 정적 → **Q252** (정적: `CmpWalkOneLevel` 길이 0 조각 · `0xc0000034` 최종 줄 · `+0x45a` 한 글자 이름 갈래).

## 요약

- **요청서 전제가 틀림** `[정적]`: 요청서는 `CmpParseKey → CmpDoParseKey → CmpGetNextName` 길을 가정했다. 그런데 **`CmpDoParseKey` 는 `CmpGetNextName` 을 부르지 않는다** (`_calls.tsv` 의 `CmpDoParseKey` 호출 264 줄 · callee 101 개에 없음, flow-writer 가 다시 셈).
  - `CmpGetNextName` 을 부르는 곳은 셋뿐이고 전부 **하이브 안 고정 경로 걷기** 다: `CmpWalkUnicodeStringPath +0x39` · `CmpGetKnownHivePathNode +0x67` · `CmpFindHiveSubKey +0x56`.
  - 사용자가 넘긴 이름을 다듬는 곳은 둘: **`CmpParseKey` 앞머리** (끝 · 앞 `\` 떼기) 와 **`CmpComputeComponentHashes`** (조각 나누기).
- **절대 이름 끝 `\` 하나 → 연다** `[정적]`: `CmpParseKey` 가 `CmpDoParseKey` 를 부르기 전에 남은 이름 복사본의 **끝 `\` 를 전부** 떼고 (`+0x112` · `+0x118` → `+0x5fa` 루프), 이어서 **앞 `\` 도 전부** 뗀다 (`+0x128` · `+0x12c` → `+0x49c` 루프). status 를 만들지 않는다.
- **끝 `\\` 둘 → 연다, 가운데 `\\` → 연다** `[정적]`: 끝은 같은 루프가 다 떼고, 가운데는 `CmpComputeComponentHashes` 가 연속 `\` 를 한 칸으로 건너뛴다 (`+0x25b ~ +0x278`). 거절하는 줄 없음.
- **상대 이름 `Key\` → 같은 줄로 연다** `[정적]`: `ObpLookupObjectName` 이 `RootDirectory` 가 있으면 이름을 **통째로** 남은 이름에 넣고 (`+0x20d`) 그대로 `CmpParseKey` 에 넘긴다 (`+0x8ce`). 절대와 다른 건 남은 이름이 `\` 로 시작하나뿐이고, 둘 다 `CmpParseKey` 가 같은 줄로 다듬는다.
  - 덤: 상대 **빈 이름** → 같은 키를 다시 연다 (새 핸들). 상대 이름이 **`\` 로 시작** → 키 핸들 기준이면 `0xc000003b` (`CmpParseKey` 까지 안 감).
- **`0xc0000033` 은 어디서?** `[정적]`: `\` 로 생기는 `0xc0000033` 은 `ObpLookupObjectName` 의 객체 폴더 걷기에서 **길이 0 조각** 하나뿐 (`+0xae5` → `+0xe00`) — 맨 앞 `\\Registry…` 처럼 **`\Registry` 앞** 에만 걸린다. 그 밖엔 홀수 길이 이름 (`ObpCaptureObjectName +0xc0 ~ +0xd0`).
- **만들기도 같은 파싱** `[정적]`: `NtCreateKey → CmCreateKey → ObOpenObjectByNameEx → ObpLookupObjectName → CmpParseKey → CmpDoParseKey`. 끝 `\` 는 먼저 떼이므로 만들 조각은 그 앞 이름이다.
- **PE_emulator** (결과서 "지금 우리 맞나"): 절대 끝 `\` `0xc0000033` **틀림**, 상대 갈래 (끝 `\` 하나만 뗌) 는 결과는 맞고 규칙이 좁음 (`\\` 둘 · 가운데 `\\` 틀림), 만들기 끝 `\` `0xc0000033` **틀림**.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔둘째 바퀴. plan **Q247**. **짐 지도 바퀴 아님.**
- 정적: ntoskrnl 은 **이미 analyze 된 프로젝트** 에서 `^이름$` export 만 (다시 analyze 안 함, 로그 `ms/ghidra/ntoskrnl.export.sast131a ~ e.stdout`), refs 1장 (`ms/ghidra/ntoskrnl.refs.sast131_CmpGetNextName.stdout`).

| 모듈 | 바이너리 (`System32\`) | PDB guid+age |
|---|---|---|
| ntoskrnl | `ntoskrnl.exe` | `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt` · 기준 줄과 같음, flow-writer 가 다시 확인) |

- 버전 대조 (결과서 "알려진 사실"): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines`, 요청서가 인용한 `ObpLookupObjectName +0x8ce/+0x8d3 → CmpParseKey` — **일치**.
- 이번 바퀴에 읽은 함수 (rva · `.asm` 머리):

| 함수 | rva | 크기 (`_index.tsv`) | 이번 역할 |
|---|---|---|---|
| `CmpParseKey` | `0x8ba4e0` | 1856 B | 남은 이름 다듬기 (끝 · 앞 `\`) |
| `CmpDoParseKey` | `0x867cf0` | 11350 B | 조각 나누기 부르기 · 조각 0개 갈래 · 링크 · 만들기 |
| `CmpComputeComponentHashes` | `0x867890` | 978 B | 조각 나누기 (가운데 `\\`) · 개수 · 해시 |
| `CmpGetNextName` | `0x7ef11c` | 182 B | (이 길 아님) 하이브 안 고정 경로 조각 |
| `CmpGetSymbolicLinkTarget` | `0x85f640` | | 링크 따라 이어 붙이기 |
| `CmpGetComponentNameAtIndex` | `0x9b92b0` | | 조각 번호 → 이름 |
| `CmpWalkOneLevel` | `0x85b140` | | (이름만 — 안 읽음) |
| `CmpWalkUnicodeStringPath` · `CmpWalkPath` · `CmpFindHiveSubKey` · `CmpGetKnownHivePathNode` | `0x7f1494` · `0x7f144c` · `0xc59b78` · `0xc5a03c` | | `CmpGetNextName` 부르는 자리 확인 |
| `NtOpenKey` · `NtOpenKeyEx` · `CmOpenKey` · `NtCreateKey` · `CmCreateKey` | `0xa4dd70` · `0xa4dd90` · `0x9994a0` · `0xa7df60` · `0x998170` | | 앞단에 이름 검사 줄이 있나 |
| `ObOpenObjectByNameEx` · `ObpCaptureObjectName` · `ObpLookupObjectName` | `0x8b6fc0` · `0x8b8360` · `0x8b85b0` | | 이름 복사 · 폴더 걷기 · Parse 부르기 (`ObpLookupObjectName.asm` 은 이미 있던 것) |

## 목적

- PE_emulator 의 에뮬레이터 로그에서 `NtOpenKey("\Registry\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\")` (접근 `0x2000000` · 속성 `0x240`) 에 에뮬레이터가 `0xc0000033` 으로 답한다 (요청 쪽 로그 — 진짜 Windows 관찰 아님). 로케일 묻기마다 받는 대답이라 틀리면 넓게 틀린다.
- 요청서 `_workspace/sast/131_reg-trailing-sep.md` 의 물음:
  1. 절대 이름 끝 `\` 하나 — 떼고 여나, 아니면 어느 줄 어떤 status 인가. (`CmpGetNextName` 조각 규칙 · `CmpDoParseKey` 의 빈 조각 갈래)
  2. 끝 `\\` 둘 · 가운데 `\\` 도 같은 규칙인가.
  3. 상대 이름 (`RootDirectory` = 키 핸들, `Key\`) 도 같은 줄인가. 덤: 상대 빈 이름 / `\`.

## 방법

- 정적 sast/131: ntoskrnl `^이름$` depth 0 (요청 셋 `CmpGetNextName` · `CmpParseKey` · `CmpDoParseKey` + 길이 없어서 넓힌 것 — 위 표). 요청서의 depth 1 은 안 썼다 (`CmpDoParseKey` callee 101 개라 필요한 것만 골라 뽑음).
- 결과서: `_workspace/sast/131_reg-trailing-sep_result.md` (**분석성공**, 스크립트 문제 없음).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` call · ret 짝 — 아래 "정적 — 호출 지점" 표 전부. `CmpDoParseKey` 호출 264 줄 · callee 101 개에 `CmpGetNextName` 없음, `CmpGetNextName` 의 caller 셋.
  - `.asm` 줄: `CmpParseKey +0x60` · `+0x86` · `+0x8c` · `+0xf7 ~ +0x132` · `+0x28f` · `+0x2bf` · `+0x2dc` · `+0x2e4` · `+0x45a ~ +0x49a` · `+0x49c ~ +0x4c0` · `+0x5fa ~ +0x609`; `CmpDoParseKey +0xe1` · `+0xef` · `+0x1d6 ~ +0x1f4` · `+0x213` · `+0x217` · `+0x21c ~ +0x256` · `+0x2cd ~ +0x2e3` · `+0x4bf` · `+0x4c2` · `+0x4f4 ~ +0x5d9` · `+0x8d8` · `+0x8e1` · `+0xbe1` · `+0xc22` · `+0x122d` · `+0x159c` · `+0x17c1` · `+0x1cb3` · `+0x1fa2` · `+0x2167` · `+0x216c` · `+0x2a7225`, `0xc0000034` 가 든 줄 23 개; `CmpComputeComponentHashes +0x2f ~ +0x80` · `+0x15b` · `+0x162` · `+0x19f ~ +0x2a8` · `+0x3b6` · `+0x3c0`; `CmpGetNextName +0xf ~ +0xb5`; `ObpLookupObjectName` 의 `0x5c` 비교 10 줄 전부 · `+0x132` · `+0x1a0` · `+0x1cb` · `+0x1dc` · `+0x1e1` · `+0x1eb` · `+0x1ef` · `+0x1fd` · `+0x203` · `+0x20a` · `+0x20d` · `+0x226` · `+0x765` · `+0x871 ~ +0x8ce` · `+0xa3b ~ +0xa61` · `+0xaca` · `+0xae2` · `+0xae5` · `+0xe00` · `+0x1282` · `+0x129b` · `+0x129e` · `+0x12ac` · `+0x1744` · `+0x1760` · `+0x1ddc` · `+0x1e04`; `ObpCaptureObjectName +0xc0 ~ +0xd0` · `+0x1c3` · `+0x1d2`; `NtOpenKey +0x7`, `NtOpenKeyEx +0x74` · `+0x7b`, `CmOpenKey +0x244`, `ObOpenObjectByNameEx +0xfe` · `+0x21f`, `NtCreateKey +0x4a` · `+0x8a`; `CmpWalkUnicodeStringPath +0x39 ~ +0x42` · `+0xa6`; `CmpGetSymbolicLinkTarget +0x35b` · `+0x77d` · `+0x9cd` · `+0xc36` · `+0xc55` · `+0xc62`. `NtOpenKeyEx` · `CmOpenKey` · `NtCreateKey` · `CmCreateKey` 에 `0x5c` · `0xc0000033` · `0xc000003b` 가 없음 (grep 0). 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- 레지스트리 키 이름은 **"서랍장 › 칸 › 상자"** 같은 주소다. 커널은 주소를 받으면 먼저 **양 끝에 붙은 `›` 를 지운다** (`CmpParseKey`). 가운데 `››` 처럼 두 번 찍힌 건 **한 번으로 읽는다** (`CmpComputeComponentHashes`). 그래서 끝에 `›` 가 하나 더 붙어도 같은 상자를 찾는다 `[추측: 비유]`.
- 요청서가 짚은 `CmpGetNextName` 은 이 길이 아니라, 커널이 **자기 안에 적어 둔 고정 주소** 를 찾아갈 때 쓰는 다른 도구다 `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtOpenKey` | `+0x7` | `+0xc` | `NtOpenKeyEx` | [정적] |
| `nt!NtOpenKeyEx` | `+0x7b` | `+0x80` | `KeExpandKernelStackAndCallout` (콜아웃 = `+0x74 LEA` `0x140999460` = `CmOpenKeyCallout`) | [정적] |
| `nt!CmOpenKey` | `+0x244` | `+0x249` | `ObOpenObjectByNameEx` | [정적] |
| `nt!NtCreateKey` | `+0x8a` | `+0x8f` | `KeExpandKernelStackAndCallout` (콜아웃 = `+0x4a LEA` `0x140997fc0` = `CmCreateKeyCallout`) → `CmCreateKey` | [정적] |
| `nt!CmCreateKey` 조각 `FUN_14099849f` | `+0xa6` (= `CmCreateKey +0x3d5`) | `+0xab` | `ObOpenObjectByNameEx` | [정적] |
| `nt!ObOpenObjectByNameEx` | `+0xfe` · `+0x21f` | `+0x103` · `+0x224` | `ObpCaptureObjectCreateInformation` · `ObpLookupObjectName` | [정적] |
| `nt!ObpCaptureObjectCreateInformation` | `+0x1c7` | `+0x1cc` | `ObpCaptureObjectName` (홀수 길이 · `0xfffe` → `0xc0000033`) | [정적] |
| `nt!ObpLookupObjectName` | `+0x1cb` | `+0x1d0` | `ObpReferenceObjectByHandleWithTag` (상대 이름 — 루트 핸들 → 객체) | [정적] |
| 〃 | **`+0x8ce`** | **`+0x8d3`** | **`CmpParseKey`** — 형식의 Parse 가 `CmpParseKey` 일 때 직접 호출 (`+0x871 LEA` · `+0x878 CMP`) | [정적] |
| `nt!CmpParseKey` | **`+0x2e4`** | **`+0x2e9`** | **`CmpDoParseKey`** — 다듬은 이름 복사본을 넘김 | [정적] |
| `nt!CmpDoParseKey` | **`+0x217`** | **`+0x21c`** | **`CmpComputeComponentHashes`** — 첫 패스 조각 나누기 | [정적] |
| 〃 | `+0x53c` | `+0x541` | `RtlPrefixUnicodeString` (`R15 ≠ 0` 갈래 — 접두 떼기) | [정적] |
| 〃 | `+0x5d4` | `+0x5d9` | `CmpComputeComponentHashes` — `R15 ≠ 0` 갈래 | [정적] |
| 〃 | `+0x8d8` | `+0x8dd` | `CmpWalkOneLevel` (안 읽음) | [정적] |
| 〃 | `+0xbe1` · `+0x17c1` · `+0x2a7225` | `+0xbe6` · `+0x17c6` · `+0x2a722a` | `CmpGetSymbolicLinkTarget` (링크) | [정적] |
| 〃 | `+0x122d` | `+0x1232` | `CmpReferenceKeyControlBlockUnsafe` — 조각 0개 → 시작 키 자체 | [정적] |
| 〃 | `+0x159c` · `+0x1cb3` · `+0x1fa2` | `+0x15a1` · `+0x1cb8` · `+0x1fa7` | `CmpCreateChild` (만들기) | [정적] |
| `nt!CmpGetSymbolicLinkTarget` | `+0x77d` · `+0xc36` | `+0x782` · `+0xc3b` | `CmpGetComponentNameAtIndex` | [정적] |
| 〃 | `+0x9cd` | `+0x9d2` | `CmpComputeComponentHashes` | [정적] |
| 〃 | `+0xc62` | `+0xc67` | `RtlUnicodeStringCat` | [정적] |
| `nt!CmpWalkUnicodeStringPath` | `+0x39` | `+0x3e` | `CmpGetNextName` | [정적] |
| `nt!CmpGetKnownHivePathNode` | `+0x67` | `+0x6c` | `CmpGetNextName` | [정적] |
| `nt!CmpFindHiveSubKey` | `+0x56` | `+0x5b` | `CmpGetNextName` | [정적] |
| `nt!CmpWalkPath` · `CmpFindHiveSubKey` | `+0x2e` · `+0x89` | `+0x33` · `+0x8e` | `CmpWalkUnicodeStringPath` | [정적] |
| `nt!CmpGetKnownHivePathNode` | `+0xc9` | `+0xce` | `CmpFindHiveSubKey` | [정적] |

### 0. 요청서 전제가 틀림 — `CmpGetNextName` 은 이 길이 아니다 [정적]

- `CmpDoParseKey` 가 부르는 함수 101 개 (호출 264 줄) 에 `CmpGetNextName` 이 없다. 조각 나누기는 `CmpComputeComponentHashes` (`+0x217` · `+0x5d4`) 가 한다.
- `CmpGetNextName` 의 코드 참조는 셋뿐 (refs — 넷째는 `.pdata` 데이터 참조): `CmpWalkUnicodeStringPath +0x39` · `CmpGetKnownHivePathNode +0x67` · `CmpFindHiveSubKey +0x56`. 그 위는 `CmpWalkPath` ← `CmpGetSystemControlValues` · `CmpPreserveSystemHiveData` · `CmSelectQualifiedInstallLanguage`, `CmpFindHiveSubKey` ← `CmpFindStateSepKeysRedirectionMapNode` · `CmpGetKnownHivePathNode` ← `CmpFindRedirectedDriverServiceStateNode` (결과서 — 각 `.c` 머리 `callers:`). 전부 **이름이 코드에 박힌 하이브 안 걷기** 다.
- 그래도 요청서가 물은 `CmpGetNextName` 규칙은 결과서가 답해 두었다 (참고, 이 길에선 안 쓰임):
  - 인자: `RCX` 남은 이름 (제자리에서 줄임) · `RDX` 출력 조각 · `R8` 출력 `last` (BYTE). 반환 BYTE.
  - 앞 `\` 는 전부 건너뜀 (`+0x2d` ~ `+0x50` 루프).
  - **빈 조각** = 반환 TRUE + 출력 Length 0 · Buffer NULL + `last = 1` (`+0x52` · `+0x56` · `+0x58` · `+0x5c`). 들어오는 이름이 NULL · 길이 0 · 첫 글자 NUL · `\` 만 남음 → 다 여기로.
  - **마지막 조각** = 조각을 뗀 뒤 남은 길이가 0 일 때만 `last = 1` (`+0x98 TEST DX,DX` · `+0xa4 SETZ` · `+0xa7`). 그래서 `Key\` 는 `Key` (`last = 0`) 다음에 빈 조각 (`last = 1`) 이 하나 더 나온다.
  - 반환 FALSE = 조각 길이 > `0x200` 바이트 (`+0xaa` · `+0xb2`).
  - `CmpWalkUnicodeStringPath` 는 빈 조각 (`+0x3e CMP word [RBP-0x20],0`) 을 "경로 끝" 으로 보고 그때까지 간 칸을 돌려준다 (`+0x42 JZ → +0xa6`).

### 1. 절대 이름 끝 `\` 하나 (질문 1) [정적]

답: **끝 `\` 를 떼고 그 키를 연다.** 키가 있으면 성공. `0xc0000033` 아님.

이름이 흘러가는 차례:

1. `ObpCaptureObjectName` 이 이름을 그대로 복사한다 (`+0x1c3` 복사 함수 — 같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`). 거르는 건 홀수 길이 · `0xfffe` 뿐 (`+0xc0 ~ +0xd0` → `+0x1d2` `0xc0000033`).
2. `ObpLookupObjectName` 이 남은 이름 칸 (`[RSP+0x70]`) 을 이름 통째로 채우고 (`+0xa3b` · `+0xa3e`), 객체 폴더 걷기 (`+0xa61 ~ +0xae5`) 가 `Registry` 한 조각을 먹는다. 남은 이름 = `\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\` (**끝 `\` 그대로**).
   - `ObpLookupObjectName` 은 끝 `\` 를 안 건드린다: `0x5c` 비교 10 줄 (`+0x144` · `+0x1f9` · `+0x24f` · `+0x547` · `+0x6d9` · `+0xa2e` · `+0xa78` · `+0xab0` · `+0xd90` · `+0xde4`) 중 마지막 글자 (`Length − 2`) 를 보는 줄이 없다.
3. 레지스트리 루트 객체의 Parse 가 `CmpParseKey` 라 `+0x871 LEA` · `+0x878 CMP` 로 맞춰 보고 **`+0x8ce` 에서 직접 부른다**. 남은 이름 = 7번째 인자 (`+0x8bb LEA RAX,[RSP+0x70]`).
4. **`CmpParseKey` 가 다듬는다** (복사본 `[RSP+0x50]` 만 만짐 — `+0x60` · `+0x86` · `+0x8c`):
   - 길이 0 이면 다듬기 건너뜀 (`+0xf7` · `+0xff`).
   - **끝 `\` 떼기**: `+0x112 CMP word [RDX+RAX*2-2],0x5c` → `+0x118 JZ +0x5fa` → `+0x5fa` Length −2 → `+0x603 JNZ +0x10c` 로 돌아가 다시 → **끝 `\` 전부**. 길이 0 이 되면 `+0x609` 로 빠짐.
   - **앞 `\` 떼기**: `+0x128 CMP word [RDX],0x5c` → `+0x12c JZ +0x49c` → `+0x49c ~ +0x4b2` (Buffer +2 · Length −2 · MaximumLength −2) → `+0x4ba JNZ +0x128` → **앞 `\` 전부**.
   - 다듬은 복사본을 `+0x28f ~ +0x2dc` 로 옮겨 **`+0x2e4 CmpDoParseKey`** 에 넘긴다 (6번째 인자).
5. `CmpDoParseKey` 가 받은 이름 (`+0xe1` · `+0xef` → `[RBP-0x8]`) 으로 **`+0x217 CmpComputeComponentHashes`** → 조각 5 개 (`Machine` · `System` · `CurrentControlSet` · `Control` · `CommonGlobUserSettings`).

빈 조각 · 길이 0 갈래 (`CmpDoParseKey`):

- **남은 길이 0 (조각 0개)** → `CmpComputeComponentHashes +0x2f` · `+0x33` → `+0x15b ~ +0x162` (개수 0) → `CmpDoParseKey +0x2df CMP R12W,R14W` · `+0x2e3 JZ` → **`+0x122d CmpReferenceKeyControlBlockUnsafe`** = 시작 키 자체를 연다. 오류 아님.
- **길이 0 조각 하나** (끝 `\` 이름이 다듬어지지 않고 `CmpComputeComponentHashes` 에 들어오면 — `+0x25b ~ +0x278` 이 `\` 를 다 먹고 길이 0 → `+0x73` 에서 마지막 조각을 하나 더 셈): 이 길에선 `CmpParseKey` 가 먼저 떼서 **안 생긴다**. 생긴다 해도 `CmpDoParseKey +0x228 ~ +0x256` 조각 검사는 **길이 > `0x200` 만** 거절 (`+0x256 JA` → `+0x2167` → `+0x216c` `0xc000000d`). 길이 0 거절 줄은 못 찾았고, 그 뒤 `CmpWalkOneLevel` 안은 안 읽었다 → `[공백]` (Q252).
- 상한: 조각 32 개 넘음 (`CmpComputeComponentHashes +0x19f CMP BX,0x20` · `+0x1a3` → `+0x3b6 ~ +0x3c0` `0xc000000d`), 조각 길이 > `0x200` 바이트 (256 글자) → `0xc000000d` (위).

### 2. 끝 `\\` 둘 · 가운데 `\\` (질문 2) [정적]

- **끝 `\\`** → 연다. 1 의 끝 `\` 루프가 `+0x603 JNZ +0x10c` 로 되돌아가니 둘 다 뗀다.
- **가운데 `\\`** (`…\Control\\CommonGlobUserSettings`) → 연다, **거절 아님**. `CmpParseKey` 는 가운데를 안 본다. `CmpComputeComponentHashes` 가:
  - `+0x1a9 CMP word [R15],0x5c` · `+0x1ae JNZ +0x2a4` — `\` 가 아니면 한 글자 전진.
  - `\` 이면 `+0x1b4` 부터 조각 하나 기록 (길이 = 조각 시작 때 남은 길이 − 지금 남은 길이), 대문자로 바꿔 해시 (`+0x216 ~ +0x23a`, `IMUL 0x25`).
  - **`+0x25b CMP word [R15],0x5c` · `+0x262` Length −2 · `+0x266` Buffer +2 · `+0x274` · `+0x278 JNZ +0x25b`** — 연속 `\` 를 다 건너뛰고 `+0x27a` 로 나가 새 조각.
  - 그래서 `Control\\CommonGlobUserSettings` = 조각 둘 (`Control` · `CommonGlobUserSettings`).
- 루트 바로 뒤 `\\` (`\Registry\\Machine…`) 도 `CmpParseKey` 앞 `\` 루프가 다 뗀다.
- **`\` 때문에 거절하는 줄은 하나뿐**: `ObpLookupObjectName` 폴더 걷기 — `+0xa78` 앞 `\` 하나만 떼고 `+0xab0 ~ +0xaca` 로 다음 `\` 까지 센 뒤 `+0xae2 TEST AX,AX` · `+0xae5 JZ` → **`+0xe00` `0xc0000033`**. 레지스트리 루트에 닿기 전 (`\Registry` 앞, 예: `\\Registry\…`) 에만 걸린다.

### 3. 상대 이름 `Key\` (질문 3) [정적]

- `ObpLookupObjectName`:
  - `+0x132 JNZ +0x1a0` (`RootDirectory` 있음) → `+0x1cb ObpReferenceObjectByHandleWithTag` (핸들 → 키 객체).
  - `+0x1eb` · `+0x1ef` — 이름 길이 0 이면 `+0x1744`.
  - `+0x1f9` · `+0x1fd` — 첫 글자 `\` 이면 `+0x1282`.
  - **`+0x20a` · `+0x20d MOVAPS [RSP+0x70],XMM1`** = 남은 이름 = **이름 전체** → `+0x226 JMP` → 반복문 → `+0x8bb` · **`+0x8ce CmpParseKey`** (1 의 3 과 같은 자리).
- 절대는 남은 이름이 `\Machine\…\Key\`, 상대는 `Key\` — **`\` 로 시작하나만 다르다.** 끝 `\` 는 둘 다 그대로 넘어가고 `CmpParseKey` 가 같은 줄 (`+0xf7 ~ +0x4c0`) 로 `Key` 로 만든다.
- `CmpDoParseKey` 에서도 기준이 루트 키냐 아니냐 (`+0x1d6 ~ +0x1e5` `CmpRegistryRootObject` 비교 · `+0x1e5 JNZ +0x4bf`) 만 다르고, `R15 == 0` 이면 둘 다 `+0x1f4` → `+0x217` 로 같이 간다 (`+0x4bf TEST R15,R15` · `+0x4c2 JZ +0x1f4`).
- 덤:
  - **상대 빈 이름** (Length 0) → 루트가 폴더 객체면 따로 (`+0x1744` · `+0x1760`), 아니면 `+0x203` 으로 돌아가 길이 0 으로 `CmpParseKey` → 조각 0 개 → **같은 키를 다시 연다** (새 핸들).
  - **상대 이름 첫 글자 `\`** (`\` 하나 · `\Key`) → `+0x1282` 루트가 파일 객체인지 (`+0x129b CMP` · `+0x129e JZ` 면 통과) → 아니면 **`+0x12ac` `0xc000003b`**. `CmpParseKey` 까지 안 간다.
  - 절대 이름인데 `\` 로 안 시작 (또는 빈 이름) → `+0x144` · `+0x148 JNZ` → `+0x12ac` `0xc000003b`.
  - 핸들 기준일 때 이름과 상관없는 접근 비교가 하나 더 있다 (`+0x1dc` · `+0x1e1 JNZ` → `+0x1ddc ~ +0x1e04` `0xc0000022`). 뜻은 `[추측]` (핸들이 허용한 접근 vs 요청 접근).

### 4. 링크 (`CurrentControlSet` → `ControlSet001`) 따라 다시 파싱할 때 [정적 + 공백]

- 링크면 `CmpDoParseKey +0xbe1` · `+0x17c1` 이 `CmpGetSymbolicLinkTarget` 을 조각 번호 범위로 부른다. 그 함수는 링크 대상 뒤에 조각마다 **`\` 한 글자 (`+0x35b` · `+0xc55`) + 조각 이름 (`+0x77d` · `+0xc36 CmpGetComponentNameAtIndex`, `+0xc62 RtlUnicodeStringCat`)** 만 붙인다. **끝에 `\` 를 덧붙이는 줄 없음** → 이어 붙인 이름은 끝 `\` 를 안 지닌다 (이미 다듬어진 이름의 조각 배열에서 만들므로).
- 성공하면 `CmpDoParseKey +0xc22` 가 status `0x104` 를 정한다 (STATUS_REPARSE 꼴로 읽힘 `[추측: 이름]`). 새 패스에서 `CmpParseKey` 가 다시 다듬는다.
- `[공백]` 두 번째 패스가 `R15 ≠ 0` 갈래 (`+0x1ee JNZ +0x4f4` ~ `+0x5d4`) 를 타면 `CmpParseKey` 가 다듬은 이름이 아니라 **다른 이름에서 접두를 뗀 나머지** 로 조각을 나눈다 (아래 "flow-writer 대조" 셋째 — 이 갈래는 앞 `\` 만 떼고 끝 `\` 떼기 줄이 없다). 진짜 이 갈래를 타는지 · 그 이름에 끝 `\` 가 있는지는 동적 (Q251 의 `+0x5d9`).

### 5. 만들기 (`NtCreateKey`) 도 같은 파싱인가 [정적]

- **같다.** `NtCreateKey +0x4a` (콜아웃 `CmCreateKeyCallout`) · `+0x8a KeExpandKernelStackAndCallout` → `CmCreateKey` → 조각 `FUN_14099849f +0xa6` (= `CmCreateKey +0x3d5`) `ObOpenObjectByNameEx` → `ObpLookupObjectName` → `CmpParseKey` → `CmpDoParseKey`.
- `CmCreateKey` 에 이름 `\` 검사 줄이 없다 (grep). 마지막 조각이 없으면 `CmpDoParseKey` 가 `CmpCreateChild` 를 부른다 (`+0x159c` · `+0x1cb3` · `+0x1fa2`). 끝 `\` 는 `CmpParseKey` 가 먼저 떼므로 **만들 조각은 그 앞 이름**.
- `[공백]` `CmpCreateChild` 앞 조건 (부모 없음 → `0xc0000034` / `0xc000003a`, 이름 충돌 `0xc0000035`) 은 요청 밖이라 안 따라감 (Q252 ② 와 같이).

### 표: 이름 꼴 → 결과 (결과서 표를 줄였다. PK = `CmpParseKey`, CCH = `CmpComputeComponentHashes`, OLN = `ObpLookupObjectName`)

| 이름 꼴 (예) | 무엇이 다듬나 | 결과 | 줄 |
|---|---|---|---|
| 절대 끝 `\` `\Registry\Machine\…\CommonGlobUserSettings\` | PK 가 끝 `\` · 앞 `\` 뗌 → CCH 5 조각 | **연다** (없으면 `0xc0000034` 쪽 — Q252) | OLN `+0xa3e` · `+0x8ce` · PK `+0x112` · `+0x118` · `+0x5fa ~ +0x609` · `+0x128 ~ +0x4c0` · `+0x2e4` |
| 절대 끝 `\\` | PK 루프가 둘 다 뗌 | **연다** | PK `+0x603` |
| 가운데 `\\` `…\Control\\CommonGlobUserSettings` | CCH 가 연속 `\` 를 한 칸으로 | **연다** | CCH `+0x1a9` · `+0x1ae` · `+0x25b ~ +0x278` |
| `\Registry\\Machine…` | PK 앞 `\` 루프 | 연다 | PK `+0x128 ~ +0x4ba` |
| 맨 앞 `\\Registry…` | OLN 폴더 걷기 길이 0 조각 | **`0xc0000033`** | OLN `+0xa78` · `+0xab0` · `+0xae5` → `+0xe00` |
| 상대 `Key\` (키 핸들) | OLN 이 통째로 넘김 → PK 가 `Key` 로 | **연다** | OLN `+0x132` · `+0x1cb` · `+0x20d` · `+0x8ce` |
| 상대 `Key\\` | PK 루프 | **연다** | PK `+0x603` |
| 상대 빈 이름 | 조각 0 개 | **같은 키를 다시 연다** | OLN `+0x1ef` → `+0x1744` / `+0x1760` → `+0x203` · `CmpDoParseKey +0x2e3` → `+0x122d` |
| 상대 `\` · `\Key` (키 핸들) | OLN 앞머리 | **`0xc000003b`** | OLN `+0x1fd` → `+0x1282` · `+0x129e` · `+0x12ac` |
| 절대 빈 이름 / `\` 로 안 시작 | OLN 앞머리 | `0xc000003b` | OLN `+0x144` · `+0x148` → `+0x12ac` |
| 홀수 길이 | `ObpCaptureObjectName` | `0xc0000033` | `+0xc0 ~ +0xd0` → `+0x1d2` |

### 호출 관계

```
(PE_emulator 요청 — 이름 꼴은 test.exe 초기화 밖. 사슬 자체는 test.exe 의 NtOpenKey 도 탐 [정적: 같은 코드])

NtOpenKey +0xc NtOpenKeyEx +0x80 KeExpandKernelStackAndCallout ⇢ CmOpenKeyCallout ⇢ CmOpenKey
NtCreateKey +0x8f KeExpandKernelStackAndCallout ⇢ CmCreateKeyCallout ⇢ CmCreateKey (조각 FUN_14099849f)
 └─ CmOpenKey +0x249 / CmCreateKey 조각 +0xab  ObOpenObjectByNameEx
      ├─ +0x103  ObpCaptureObjectCreateInformation +0x1cc ObpCaptureObjectName   홀수 길이 · 0xfffe → 0xc0000033
      └─ +0x224  ObpLookupObjectName
                  ├─ (상대) +0x1d0 ObpReferenceObjectByHandleWithTag   이름 통째 → 남은 이름 (+0x20d)
                  │         첫 글자 \ → 0xc000003b (+0x12ac)
                  ├─ (절대) 폴더 걷기 \Registry   길이 0 조각 → 0xc0000033 (+0xe00)
                  └─ +0x8d3  CmpParseKey                        끝 \ 전부 · 앞 \ 전부 떼기
                              └─ +0x2e9  CmpDoParseKey
                                          ├─ +0x21c  CmpComputeComponentHashes   가운데 \\ 한 칸 · 32 조각 · 0x200
                                          ├─ +0x1232 CmpReferenceKeyControlBlockUnsafe   조각 0 개 → 시작 키
                                          ├─ +0x8dd  CmpWalkOneLevel (안 읽음)
                                          ├─ +0xbe6 · +0x17c6 · +0x2a722a  CmpGetSymbolicLinkTarget   링크 → 0x104
                                          │           ├─ +0x782 · +0xc3b CmpGetComponentNameAtIndex
                                          │           ├─ +0x9d2 CmpComputeComponentHashes
                                          │           └─ +0xc67 RtlUnicodeStringCat
                                          ├─ (R15≠0) +0x541 RtlPrefixUnicodeString → +0x5d9 CmpComputeComponentHashes
                                          └─ +0x15a1 · +0x1cb8 · +0x1fa7  CmpCreateChild   (만들기)

(이 길 아님) CmpWalkPath +0x33 CmpWalkUnicodeStringPath +0x3e CmpGetNextName
             CmpGetKnownHivePathNode +0x6c CmpGetNextName · +0xce CmpFindHiveSubKey +0x5b CmpGetNextName
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). `⇢` 는 콜아웃 포인터로 가는 호출 (`KeExpandKernelStackAndCallout` 안은 안 읽음).

## 확인된 것

- `[정적]` `CmpDoParseKey` 는 `CmpGetNextName` 을 부르지 않는다 (`_calls.tsv` 264 줄). `CmpGetNextName` 은 하이브 안 고정 경로 걷기 셋만 부른다.
- `[정적]` `ObpLookupObjectName` 은 레지스트리 루트에서 `CmpParseKey` 를 **직접 호출** (`+0x871 LEA` · `+0x878 CMP` · `+0x8ce`) 하고, 남은 이름의 끝 `\` 를 안 건드린다.
- `[정적]` `CmpParseKey` 가 `CmpDoParseKey` 전에 **끝 `\` 전부 → 앞 `\` 전부** 를 뗀다 (`+0x112 ~ +0x12c` · `+0x49c ~ +0x4c0` · `+0x5fa ~ +0x609`).
- `[정적]` `CmpComputeComponentHashes` 는 연속 `\` 를 한 칸으로 본다 (`+0x25b ~ +0x278`). 조각 32 개 · 길이 `0x200` 넘으면 `0xc000000d`.
- `[정적]` 남은 이름 길이 0 → 조각 0 개 → 시작 키를 다시 연다 (`CmpDoParseKey +0x2e3` → `+0x122d`).
- `[정적]` 상대 이름은 이름 통째가 남은 이름 (`ObpLookupObjectName +0x20d`) → 절대와 같은 `CmpParseKey` 줄. 첫 글자 `\` 면 `0xc000003b` (`+0x12ac`).
- `[정적]` `\` 때문에 나는 `0xc0000033` 은 `\Registry` 앞 폴더 걷기의 길이 0 조각 하나 (`ObpLookupObjectName +0xe00`).
- `[정적]` `NtOpenKey` → `NtOpenKeyEx` 와 `NtCreateKey` → `CmCreateKey` 둘 다 같은 `ObOpenObjectByNameEx` → `ObpLookupObjectName` → `CmpParseKey` 길. 앞단 (`NtOpenKeyEx` · `CmOpenKey` · `NtCreateKey` · `CmCreateKey`) 에 이름 `\` 검사 줄 없음.
- `[정적]` 링크 다시 파싱 이름은 끝 `\` 를 붙이지 않는다 (`CmpGetSymbolicLinkTarget +0x35b` · `+0xc55` · `+0xc62`).

### flow-writer 대조 (결과서와 다른 곳 · 보탬 — 뜻은 같음)

결과서 오프셋은 `.asm` 과 모두 맞았다 (어긋난 줄 없음). 아래는 표기 · 셈 · 보탬:

- 결과서 "동적으로 확인할 것" 의 **`nt!CmpDoParseKey+0x5d9` (두 번째 `CCH` 호출)** — `+0x5d9` 는 **ret 자리**, call 은 `+0x5d4`. 멈춤점으로 쓰면 "호출에서 돌아온 순간" (`eax` = status, 조각 수 `[rbp-0x78]`) 이고, 넘기는 이름을 보려면 `+0x5d4` (`rcx` = `[rbp-0x8]` 의 주소). 같은 절의 `+0x217` 은 call 자리, `+0x21c` 는 ret 자리라 맞다.
- 결과서 "`CmpDoParseKey` 에 `0xc0000034` 가 열 군데 넘게" — flow-writer 셈: status 칸 `[RSP+0x64]` 에 쓰는 줄 **10** (`+0x972` · `+0x1890` · `+0x197a` · `+0x2068` · `+0x22c9` · `+0x24c7` · `+0x24ee` · `+0x2593` · `+0x265c` · `+0x2a72a6`), 같은 값을 `R8D` 에 싣는 줄 12, `+0x8e1 CMP EAX,0xc0000034` (`CmpWalkOneLevel` ret `+0x8dd` 바로 뒤) 1 — 합 23. 최종 줄 고르기 (Q252 ②) 는 `+0x8e1` 부터 보면 될 것 `[추측]`.
- (보탬) **`R15 ≠ 0` 갈래의 이름 다듬기** (결과서 `[공백]` 을 조금 좁힘): `+0x4f4 ~ +0x5d4` 는 `[R15+0x128]` 을 `RtlPrefixUnicodeString` (`+0x53c`, 대소문자 무시 `R8B = 1`) 으로 `[RBP-0x18]` 이 가리키는 이름 (결과서는 `CompleteName` 으로 읽음) 의 접두인지 보고, 접두를 뗀 뒤 **앞 `\` 만** 떼는 루프 (`+0x580 CMP word [R8],0x5c` ~ `+0x59c JNZ`) 를 돈다. 이 구간에 끝 `\` 를 떼는 줄은 없다 (`CmpDoParseKey.asm` 의 `0x5c` 비교는 `+0x580` 하나뿐) `[정적]`. 그래서 이 갈래에 들어오는 이름이 끝 `\` 를 지니면 CCH 가 길이 0 마지막 조각을 만들 수 있다 `[추측]` → Q251 (`+0x5d9` 조각 수) · Q252 ① (`CmpWalkOneLevel` 길이 0) 로 확인.
- (보탬) `CmpGetSymbolicLinkTarget` 을 부르는 자리는 **셋**: `+0xbe1` · `+0x17c1` 에 더해 떨어진 조각 `+0x2a7225` (ret `+0x2a722a`, 주소 `0x140b0ef15` — 함수 크기 11350 B 밖, Ghidra 가 같은 함수로 붙인 먼 조각). 결과서는 둘만 적었다. 셋째가 어느 갈래인지는 안 읽음 `[공백]`.
- (보탬) `CmpGetSymbolicLinkTarget +0x9cd` 도 `CmpComputeComponentHashes` 를 부른다 (ret `+0x9d2`) — 링크 대상 이름을 조각 내는 자리로 보임 `[추측]`.
- (보탬) `ObpLookupObjectName` 이 `CmpParseKey` 를 고르는 줄: `+0x871 LEA R14,[CmpParseKey]` · `+0x878 CMP RBX,R14` · `+0x885 JNZ` — run47 의 `IopParseDevice` (`+0xb75 LEA` → `+0xbcd`) 와 같은 꼴이다.
- (표기) `CmpGetNextName +0x8f ~ +0x9f` 는 출력 Length (`+0x9b`) 와 MaximumLength (`+0x9f`) 를 같은 값으로 쓴다 — 결과서는 "출력 Length" 로만 적음. 뜻은 같음.

## 요청서 전제가 틀린 곳 (정정 아님 — 앞 run 에 없던 가정)

- 요청서 (`_workspace/sast/131_reg-trailing-sep.md`) 와 PE_emulator 원문 (`282_internals-reg-trailing-sep.md`) 은 **`CmpParseKey → CmpDoParseKey → CmpGetNextName`** 길을 가정했다. **이 길은 없다.** `CmpDoParseKey` 는 `CmpGetNextName` 을 안 부르고, 조각은 `CmpComputeComponentHashes` 가 나눈다. 끝 `\` 를 떼는 건 그보다 앞인 `CmpParseKey` 다.
- 앞 run 문서 (run3 ~ run133) 에 이 세 함수 (`CmpParseKey` · `CmpDoParseKey` · `CmpGetNextName`) 를 적은 곳이 없어 고칠 run 은 없다 (`grep`). `_workspace/` 요청서는 중간 산출물이라 안 고친다. PE_emulator 쪽이 "`CmpGetNextName` 규칙" 을 기대하고 고치지 않도록 4장 PE_emulator 줄에 같이 적었다.

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

결과서는 실제 경로가 `ext/syscall/syscall_layer_cpp/src/registry/` 라고 적었다 (요청서의 `src/registry/…` 와 같은 파일).

| 줄 (결과서) | 판정 | 까닭 |
|---|---|---|
| `_registry_common.hpp` `resolve_absolute` (끝 `\` → `0xc0000033`, 가운데 빈 조각 → `0xc0000033`) | **틀림** | 진짜는 끝 `\` 전부 · 앞 `\` 전부 떼고, 가운데 연속 `\` 는 한 칸. 맨 앞 `\\Registry` 만 진짜도 `0xc0000033` |
| `nt_open_key.cpp:47 ~ 48` (I02 상대 갈래 — 끝 `\` **하나만** 뗌) | **결과 반은 맞고 규칙이 좁음** | `Key\` 맞음. `Key\\` · `Control\\Key` 는 진짜는 열림, 에뮬레이터는 `resolve_relative` 의 `0xc0000033`. 이 `[추측]` + `TODO(VM)` 은 정적 근거까지 닫을 수 있음 (동적은 Q251) |
| 상대 이름 첫 글자 `\` (`registry_path_syntax_unverified` 로 멈춤) | 닫을 수 있음 | 진짜는 키 핸들 기준 `0xc000003b` |
| 상대 빈 이름 → 시작 키 | **맞음** | |
| `nt_open_key_ex.cpp` | 같은 규칙 적용 | `NtOpenKey` 도 `NtOpenKeyEx` 로 감 (`+0x7`) |
| `nt_create_key.cpp:48` (끝 `\` → `0xc0000033`) · `:100` (`leaf.empty()` → `0xc0000033`) · `:72` (`resolve_relative` 의 `0xc0000033` 넘김) | **틀림** | 만들기도 같은 `CmpParseKey` — 끝 `\` 떼고 그 앞 조각을 만든다 |
| `nt_open_key.cpp:31` 홀수 길이 `0xc0000033` | **맞음** | `ObpCaptureObjectName +0xc0 ~ +0xd0` |

고칠 때 규칙 (결과서 8 — `[추측]` 아님, 위 줄에서 나온 차례): 이름 → (절대면 `\Registry` 뒤 나머지, 상대면 통째) → ① 끝 `\` 전부 뗌 → ② 앞 `\` 전부 뗌 → ③ 가운데 연속 `\` 는 한 칸 → ④ 조각 0 개면 시작 키 → ⑤ 조각 32 개 넘음 · 조각 길이 > 256 글자 → `0xc000000d`. 없는 키 `0xc0000034` 쪽은 범위 밖 (Q252 ②).

## 아직 확인 필요

동적 → **Q251 (동적: `NtOpenKey` 넷)** — 결과서 "동적으로 확인할 것". test.exe 가 아니라 이름 꼴만 바꿔 `NtOpenKey` 를 부르는 작은 프로그램 (고르기는 사용자 결정). 커널 멈춤점은 시스템 전체 레지스트리 열기가 지나가니 **그 프로세스 조건 필수**:
1. 절대 `\Registry\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\`
2. 절대 끝 `\\`
3. 절대 가운데 `\\` (`…\Control\\CommonGlobUserSettings`)
4. 상대 (`RootDirectory` = `…\Control` 핸들, 이름 `CommonGlobUserSettings\`)
- 예상 (정적): 넷 다 성공. 덤으로 상대 `\Key` → `0xc000003b`, 상대 빈 이름 → 성공 (새 핸들).
- 멈춤점 (결과서, `nt!`): `CmpParseKey` 입구 `dS poi(@rsp+0x38)` (남은 이름 — 끝 `\` 가 아직 있나) · `CmpDoParseKey+0x217` `dS @rcx` (다듬은 이름) · `CmpDoParseKey+0x21c` `r eax` · `? wo(@rbp-0x78)` (조각 수 — 1번은 첫 패스 5 예상) · `CmpDoParseKey+0x5d9` (ret — 두 번째 패스가 `R15 ≠ 0` 갈래를 타는지, 조각 수; 이름은 call `+0x5d4` 에서).

정적 → **Q252 (정적: `CmpWalkOneLevel` 길이 0 조각 · `0xc0000034` 최종 줄 · `+0x45a` 한 글자 이름 갈래)** — 결과서 "다음 요청 후보":
1. `CmpWalkOneLevel` (rva `0x85b140`) 의 길이 0 조각 갈래 + `CmpPerformCompleteKcbCacheLookup` (캐시가 빈 조각을 어떻게 보나). 에뮬레이터에 급한 건 아님 (이번 이름 꼴은 안 닿음) — 다만 위 "flow-writer 대조" 셋째 (`R15 ≠ 0` 갈래는 끝 `\` 를 안 뗌) 때문에 두 번째 패스에선 닿을 수 있다 `[추측]`.
2. 없는 키에서 `0xc0000034` 가 나오는 최종 줄 고르기 (위 10 자리, `+0x8e1` 부터) + 중간 키 없음 (`0xc000003a`?) 구별 + 만들기 `CmpCreateChild` 앞 조건 (`0xc0000035`).
3. `CmpParseKey +0x45a ~ +0x495` — 루트 바로 아래 한 글자 이름 (`\Registry\X`, `\Registry\X\…`) → `0xc0000022` 갈래 (`[R14] & 0x40` 의 뜻) `[추측: 에뮬레이터가 루트 바로 아래 이름을 받을 때 영향]`.

그 밖 `[공백]`: `CmpDoParseKey +0x2a7225` (셋째 링크 자리) 갈래, `KeExpandKernelStackAndCallout` 안 (콜아웃 → `CmOpenKey` / `CmCreateKey` 는 결과서가 `.c` 머리 `callers:` 로 이음).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서가 멈춤점 오프셋을 call 자리 (`+0x217`) 와 ret 자리 (`+0x5d9`) 로 섞어 적었다 ("flow-writer 대조" 첫째). runbook 을 쓸 때 "call 자리 = 인자 보기, ret 자리 = 결과 보기" 를 줄마다 적으면 된다 — 이미 있는 규칙이라 하네스 제안 아님.
- `_calls.tsv` 의 `+0x2a7225` 처럼 함수 크기보다 먼 오프셋은 Ghidra 가 붙인 떨어진 조각이다 (run111 메모와 같은 종류). WinDbg 콜스택은 그 주소를 `.pdata` 기준 다른 이름으로 보일 수 있다 `[추측]` — 멈춤점에 쓰기 전에 `ln` 으로 볼 것.
