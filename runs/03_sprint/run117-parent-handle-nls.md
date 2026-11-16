# 2026-11-16 (작성일) run117 — 부모 쪽 핸들·출력 칸 (Q88) + `NtGetNlsSectionPtr` 커널 쪽 (Q89), 정적만: **스레드 핸들이 프로세스 핸들보다 먼저**, 부모 `PS_CREATE_INFO` 는 `PspUpdateCreateInfo(6, …)` 가 `State` 를 **맨 마지막**에 씀 · NLS 뷰는 **커널 핸들로 열고 닫음** (프로세스 핸들 표 안 건드림), VAD 는 `MiMapViewOfDataSection` 이 `_MMVAD` (0x88) 한 개씩

(글의 오프셋은 call 자리 · 쓰는 줄 자리. "호출 관계" 나무의 callee 앞 오프셋은 ret 자리 (콜스택 표시) — 둘 다 아래 "정적 — 호출 지점" 표.)

> **지도 범위**: 그림 1 만 조금 고쳤다. `PspInsertThread` 안 **스레드 핸들** `PspCreateObjectHandle` (ret `+0x525`) 을 따로 노드로 그렸다 (R7 "두 번 불림도 따로 노드" — 프로세스 핸들 쪽은 이미 `NtCreateUserProcess +0x10a7` 노드). `KeReadyThread` 번호가 (5) → (6). 새 단계 · 새 모듈은 없다. NLS 쪽 (`MiMapViewOfDataSection` 등) 은 그림 2 가 test.exe 유저 스레드라 커널 안쪽은 글 (2.4 NLS 절) 에만 둔다.

## 요약

- **Q88 (일 33 — 부모 cmd 쪽에 쓰는 것)** `[정적]`
  - 차례: `PspInsertProcess` (`+0xfbd`, 핸들 안 만듦) → `PspInsertThread` (`+0x1015`) 안 **스레드 핸들** (`+0x520 PspCreateObjectHandle` → `+0x573` 부모 `ThreadHandle` 칸에 씀) → 서버 silo 검사 (`+0x1043 ~ +0x104f`) → **프로세스 핸들** (`+0x10a2 PspCreateObjectHandle`) → 이미지 정보 속성 복사 (`+0x10bd ~ +0x10e4`) → 부모 `ProcessHandle` 칸 (`+0x10f6`) → **`+0x1124 PspUpdateCreateInfo(6, 문맥, 새 프로세스)`** (부모 `PS_CREATE_INFO` 출력 칸).
  - 핸들 둘은 **부모의 유저 핸들 표** (속성 `& 0x1df2` — 커널 핸들 비트 `0x200` 없음, 모드 유저). 접근 마스크는 `PspCreateObjectHandle` 의 인자가 아니라 **미리 채운 `_ACCESS_STATE`** 에 들어 있다 (`DesiredAccess` 0 으로 `ObOpenObjectByPointer`). 최종 숫자는 공용 부품 `ObpCreateHandle +0x3cc ~ +0x431` — 동적 D2.
  - `PS_CREATE_INFO` (PDB 에 타입 없음 — 칸 이름 `[추측-이름]`): 성공이면 `OutputFlags` (`+0x10`) · `UserProcessParametersNative` (`+0x28`) · `CurrentParameterFlags` (`+0x34`) · `PebAddressNative` (`+0x38`) 등을 쓰고, **`State` (`+0x8`) = 6 은 맨 마지막** (`+0x191`). 문맥 `+0x10` 비트 1 (`WriteOutputOnExit` 로 읽힘 `[추측-이름]`) 이 꺼져 있으면 `State` 만 쓴다. 부모가 이미지 이름 속성 (`0x20005`) 을 주면 파일·섹션 핸들 **사본 둘이 부모 표에 더** 생긴다 (`PspPropagateHandle → ObDuplicateObject`) — 실제로 주는지는 동적 D1.
- **Q89 (일 47 — NLS VAD 의 주인)** `[정적]`
  - test.exe 가 타는 종류는 **`0xb` (코드 페이지) 와 `0xe` (대문자 표)**. 요청서의 "12 = 대문자 표" 는 틀렸다 — `0xc` (12) 는 정규화 표.
  - `ZwOpenSection` (속성 `0x2d0` — **커널 핸들**) → `ObReferenceObjectByHandle` (모드 0) → `ZwClose` → `+0x2e6 MiMapViewOfSection` → (데이터 섹션 갈래) `+0x2d9 MiMapViewOfDataSection` → `+0x1d2 MiAllocatePool` (`0x88`B, 태그 `Vad `) · `+0x2f6 MiSelectUserAddress` · `+0x45c MiInsertVad`. **프로세스 핸들 표는 안 건드리고, VAD 한 개 = 호출 한 번.**
  - test.exe 호출은 2번 (ACP = OCP = 949 → `0xb` 1번, `UseCaseMapping ≠ 0` → `0xe` 1번) — 값은 앞 run 사진 `[관찰]`, 갈래는 `[정적]`.
- 짐 지도: **2장 숫자 그대로.** 부모 쪽 짐 14줄은 체크리스트 밖 새 **7.6.14**, NLS VAD 는 3.17 에 **노드 단위 줄 3** (칸 단위 아님 — 칸 수 · 모름 그대로).

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: **ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1`** (`ms/sym/ntoskrnl.exe.pdb.info.txt` — 기준과 같음). 다시 analyze 안 함.
- 버전 대조 (결과서): `_calls.tsv` `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` call `+0x802` / ret `+0x807` → `PspCallProcessNotifyRoutines` — 둘 다 알려진 값과 같음.
- flow-writer 가 `_calls.tsv` 로 다시 맞춘 줄: 아래 "정적 — 호출 지점" 표 전부 (`NtCreateUserProcess +0x10a2` · `+0x1124` · `+0x75a` · `+0x1159` · `+0x1191`, `PspInsertThread +0x520`, `PspCreateObjectHandle +0x38` · `+0x98`, `ObOpenObjectByPointer +0x1ba`, `PspUpdateCreateInfo +0x8a` · `+0xa8` · `+0x1eb` · `+0x24f`, `PspPropagateHandle +0x3d`, `NtGetNlsSectionPtr +0xfd` · `+0x16d` · `+0x1af` · `+0x1bb` · `+0x2e6` · `+0x303` · `+0x464`, `MiMapViewOfSection +0x2d9` · `+0x409`, `MiMapViewOfDataSection +0x1d2` · `+0x2f6` · `+0x33c` · `+0x3c6` · `+0x45c` · `+0x76d`). `.asm` 쓰는 줄도 몇 개 다시 봤다: `PspInsertThread +0x564` · `+0x56b` · `+0x573`, `NtCreateUserProcess +0x10e8` · `+0x10ef` · `+0x10f6` · `+0x111f`, `PspUpdateCreateInfo +0x75` · `+0xbf` · `+0x133` · `+0x13c` · 조각 `FUN_140a48f2d +0x8` (= `+0x191`), `NtGetNlsSectionPtr +0x25b` · `+0x27d` · `+0x2ab` · `+0x316`, `MiMapViewOfDataSection +0x1b3` · `+0x1b8`.
- 사진 값: 새 실행 없음. run44 P1a VAD 줄 (`ms/logs/04_baggage-read_P1a.txt:1539` · `:1540`) 과 P1b 둘째 NLS 뷰 줄 (`04_baggage-read_P1b.txt:2236` · `:2238`) 만 다시 봤다.
- **sast/112 결과만 넣었다.** sast/114 ~ 120 은 안 넣었다 (다음 바퀴 몫).

## 목적

- **Q88** (run45 일 33): `NtCreateUserProcess` 끝 무렵 `PspCreateObjectHandle` · `PspUpdateCreateInfo` 가 **부모 쪽**에 무엇을 어디에 쓰나 — 부모 핸들 표의 핸들 둘, 부모 출력 인자, 부모 `PS_CREATE_INFO` 출력 칸. 짐 지도 7.6 "체크리스트 밖" 줄로.
- **Q89** (run45 일 47): test.exe NLS VAD 를 만드는 커널 길 (`NtGetNlsSectionPtr` → 뷰) 과 인자별 갈래. 짐 지도 3.17 · 3.18 VAD 줄로.

## 방법

- 정적: `_workspace/sast/112_parent-handle-nls.md` — ntoskrnl.
  - 새로 뽑음 (결과서 로그 `ms/ghidra/ntoskrnl.export.sast112{a,b}.stdout`, `SCRIPT ERROR` 0): a `^(PspCreateObjectHandle|PspPropagateHandle)$` depth 1 → 5개 (`PspCreateObjectHandle` · `PspPropagateHandle` · `ObOpenObjectByPointer` · `SepAdtPrivilegeObjectAuditAlarm` · `ObDuplicateObject`), b `^MiMapViewOfDataSection$` depth 0 → 1개 (요청서 범위에서 한 단계 넓힘 — VAD 를 누가 만드는지 이름 한 단계 아래).
  - 읽기만 (있던 것): `NtCreateUserProcess` · `PspUpdateCreateInfo` · `NtGetNlsSectionPtr` · `RtlpInitNlsSectionName` · `MiMapViewOfSection` · `PspInsertThread` · `PspInsertProcess` · `PspAllocateThread` · `PspCaptureCreateInfo` · `PspBuildCreateProcessContext` · `PspSetupUserProcessAddressSpace` · `PspLocateInPEManifest` · `ObpCreateHandle` · `ObInsertObjectEx`.
  - ntdll 쪽은 `ms/disassemble/ntdll/_calls.tsv` 세 줄만 대조 (부르는 자리).
- 결과서: `_workspace/sast/112_parent-handle-nls_result.md` (**분석성공** — 스크립트 문제 없음).
- 앞 바퀴 짝: run45 (`runs/03_sprint/run45-work-list.md` 일 33 · 47), run57 (sast/52 — NLS · ApiSet). sast/52 가 이미 답한 줄은 결과서가 근거만 옮김 (✔).

## 결과

비유 (부모 cmd = 새 가게를 차려 주는 본점, test.exe = 새 가게):

- 본점이 "가게 차려 줘" 하고 관공서 (커널) 에 서류 (`PS_CREATE_INFO`) 를 낸다.
- 관공서는 가게를 다 만든 뒤 **본점 열쇠고리 (부모 핸들 표)** 에 열쇠 두 개를 건다 — 직원 열쇠 (스레드 핸들) 가 먼저, 가게 열쇠 (프로세스 핸들) 가 나중.
- 그리고 본점이 낸 서류의 **답 칸**을 채우고, 맨 마지막에 "처리 완료 (6)" 도장을 찍는다.
- 글자 사전 (NLS) 은 새 가게가 자기가 직접 전화로 달라고 한다. 관공서는 창고에서 사전을 **관공서 열쇠로** 꺼내 읽기 전용으로 벽에 붙여 주고, 열쇠는 바로 돌려놓는다 (가게 열쇠고리엔 아무것도 안 걸림). 벽 한 칸 = 장부 (VAD) 한 장.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0xfbd` | `+0xfc2` | `PspInsertProcess` | 넣기만 (핸들 안 만듦). 접근 상태 처음 채움 (`+0x1a5 SeCreateAccessStateEx`) | [정적] (run9 · run50 그대로) |
| `nt!NtCreateUserProcess` | `+0x1015` | `+0x101a` | `PspInsertThread` | | [26100.9457 확인] run3 · [정적] |
| `nt!PspInsertThread` | `+0x520` | `+0x525` | `PspCreateObjectHandle` | **스레드 핸들** — 프로세스 핸들보다 먼저. 쓰기 `+0x573` | [정적] |
| `nt!NtCreateUserProcess` | `+0x1043` | `+0x1048` | `PsGetProcessServerSilo` | `+0x1048 CMP RAX,[RBP+0x348]` · `+0x104f JZ +0x1090` — 다르면 `0xc000022d` | [정적] |
| `nt!NtCreateUserProcess` | `+0x10a2` | `+0x10a7` | `PspCreateObjectHandle` | **프로세스 핸들**. 음수면 `+0x10ab JS +0x1160` | [정적] (run9 그대로) |
| `nt!NtCreateUserProcess` | `+0x1124` | `+0x1129` | `PspUpdateCreateInfo` | `+0x111f MOV ECX,6` — 성공 길 | [정적] (run9 그대로) |
| `nt!NtCreateUserProcess` | `+0x75a` | `+0x75f` | `PspUpdateCreateInfo` | 상태 1 (파일 열기 실패) · 2 (섹션 만들기 실패) | [정적] (run9 그대로) |
| `nt!NtCreateUserProcess` | `+0x1159` · `+0x1191` | `+0x115e` · `+0x1196` | `ObCloseHandle` | `PspUpdateCreateInfo` 실패 때 프로세스 핸들 → 스레드 핸들 닫기 | [정적] |
| `nt!PspCreateObjectHandle` | `+0x38` | `+0x3d` | `ObOpenObjectByPointer` | `DesiredAccess` 0 · `PassedAccessState` = 블록 · 속성 `[블록+0x180]` · 모드 `[블록+0x184]` · 출력 `[블록+0x188]` | [정적] |
| `nt!PspCreateObjectHandle` | `+0x98` | `+0x9d` | `SepAdtPrivilegeObjectAuditAlarm` | 권한을 썼고 유저 모드일 때만 (감사 기록, 값 안 바꿈) | [정적] |
| `nt!ObOpenObjectByPointer` | `+0x1ba` | `+0x1bf` | `ObpCreateHandle` | 공용 부품 — 이름만 | [정적] |
| `nt!PspUpdateCreateInfo` | `+0x8a` · `+0xa8` | `+0x8f` · `+0xad` | `PspPropagateHandle` | 상태 6, 문맥 `+0x8` 비트 5 (이미지 이름 속성) 일 때 — 파일 핸들 · 섹션 핸들 | [정적] |
| `nt!PspUpdateCreateInfo` | `+0x24f` · `+0x1eb` | `+0x254` · `+0x1f0` | `PspPropagateHandle` | 상태 2 (파일 핸들) · 상태 5 (문맥 `+0xc8` 핸들) — test.exe 길 아님 | [정적] |
| `nt!PspPropagateHandle` | `+0x3d` | `+0x42` | `ObDuplicateObject` | 유저 호출일 때 — 옵션 `2` (같은 접근), 원본은 그대로 | [정적] |
| `nt!PspAllocateProcess` | `+0xa06` | `+0xa0b` | `PspUpdateCreateInfo` | 상태 3 — 금지 이미지 특성과 겹칠 때 (`+0x9ef` · `+0x9f4`) | [정적] (결과서) |
| `nt!NtGetNlsSectionPtr` | `+0xfd` | `+0x102` | `RtlpInitNlsSectionName` | `\NLS\…` 이름 | [정적] (run57 그대로) |
| `nt!NtGetNlsSectionPtr` | `+0x16d` | `+0x172` | `ZwOpenSection` | 속성 `0x2d0` (`+0x11d`, 커널 핸들 포함) · 접근 4 (`+0x163`) | [정적] |
| `nt!NtGetNlsSectionPtr` | `+0x1af` · `+0x1bb` | `+0x1b4` · `+0x1c0` | `ObReferenceObjectByHandle` · `ZwClose` | 모드 `R9D = 0` (커널) · 형식 `MmSectionObjectType` → 핸들 바로 닫음 | [정적] |
| `nt!NtGetNlsSectionPtr` | `+0x2e6` | `+0x2eb` | `MiMapViewOfSection` | 유저 호출 — 현재 프로세스, 크기 0 · 보호 2 · 64KB · 주소 커널이 | [정적] (run57 그대로 — **sast/52 결과서 `+0x2e1` 은 오타**) |
| `nt!NtGetNlsSectionPtr` | `+0x303` | `+0x308` | `ObfDereferenceObject` | 셋째 출력 NULL (유저 호출 늘) → 섹션 참조 놓음 | [정적] (run57 그대로) |
| `nt!MiMapViewOfSection` | `+0x2d9` | `+0x2de` | `MiMapViewOfDataSection` | `_MMSECTION_FLAGS` `Image` (`+0x2a7`) · `PhysicalMemory` (`+0x29d`) 둘 다 꺼짐 | [정적] |
| `nt!MiMapViewOfSection` | `+0x409` · 조각 `FUN_1408b5a43 +0x33` | `+0x40e` · 조각 `+0x38` | `MiMapViewOfPhysicalSection` · `MiMapViewOfImageSection` | 다른 갈래 (NLS 는 아님 `[정적 추정]`) | [정적] |
| `nt!MiMapViewOfDataSection` | `+0x1d2` | `+0x1d7` | `MiAllocatePool` | `+0x1b3 MOV EDX,0x88` · `+0x1b8 MOV R8D,0x20646156` (`Vad `) — `_MMVAD` 한 개 | [정적] |
| `nt!MiMapViewOfDataSection` | `+0x2f6` · `+0x33c` · `+0x3c6` · `+0x45c` · `+0x76d` | `+0x2fb` · `+0x341` · `+0x3cb` · `+0x461` · `+0x772` | `MiSelectUserAddress` · `MiUpdateVadStartVpn` · `MiWriteVadFlags2` · `MiInsertVad` · `MiSetVadFlags` | 이름만 (칸 단위 안 읽음) | [정적] |

ntdll 쪽 짝 (`ms/disassemble/ntdll/_calls.tsv`, 결과서가 읽기만): `LdrpInitializeNlsInfo +0x5c` (ret `+0x61`) → `RtlpInitCodePageTables` → `+0x46` / `+0x6c` (ret `+0x4b` / `+0x71`) `NtGetNlsSectionPtr`; `LdrpInitializeNlsInfo +0x6d` (꼬리 JMP) → `RtlpInitUppercaseTables +0x3b` (ret `+0x40`) `NtGetNlsSectionPtr` — run57 그대로.

### 호출 관계

```
[부모 cmd.exe 스레드 — ① 커널, 시스템 콜 NtCreateUserProcess 안]
NtCreateUserProcess
 │  +0x251 ~ +0x266  프로세스 핸들 속성 = ProcessObjectAttributes.Attributes & 0x1df2 (유저) → [RBP+0x580]
 │  +0x1eb           모드 PreviousMode → [RBP+0x584]
 │  +0xe8f ~ +0xf51  ProcessDesiredAccess · ThreadDesiredAccess (보호 프로세스 호환 1 이면 줄이고 비트 0 켬)
 ├─ +0xfc2  PspInsertProcess            +0x1a5 SeCreateAccessStateEx · +0x1de ObInsertObjectEx (핸들 출력 0)
 ├─ +0x101a PspInsertThread             +0x3aa SeCreateAccessStateEx · +0x3e5 ObInsertObjectEx
 │          └─ +0x525 PspCreateObjectHandle (스레드 블록)          ← 스레드 핸들 먼저
 │                    └─ +0x3d ObOpenObjectByPointer
 │                              └─ +0x1bf ObpCreateHandle          (부모 유저 핸들 표)
 │             +0x573  부모 ThreadHandle 칸 ← [블록+0x188]   (실패면 +0xa4a ~ +0xa72 ObCloseHandle)
 ├─ +0x1048 PsGetProcessServerSilo      +0x104f 같을 때만 계속
 ├─ +0x10a7 PspCreateObjectHandle (프로세스 블록 [RBP+0x400])       ← 프로세스 핸들
 │          └─ +0x3d ObOpenObjectByPointer → +0x1bf ObpCreateHandle
 │  +0x10bd ~ +0x10e4  PS_ATTRIBUTE 이미지 정보 출력 칸 (0x40B, 문맥 +0x38 ~ +0x77 복사)
 │  +0x10f6            부모 ProcessHandle 칸 ← [RBP+0x588]
 └─ +0x1129 PspUpdateCreateInfo(6, 문맥 [RBP+0x1a0], 새 프로세스)
            │  문맥 +0x10 비트 1 꺼짐 → State 만
            │  +0x75 문맥 +0x8 비트 5 (이미지 이름 속성 0x20005) 일 때만:
            ├─ +0x8f  PspPropagateHandle (파일 핸들, 문맥 +0xb0) → +0x42 ObDuplicateObject (옵션 2)
            ├─ +0xad  PspPropagateHandle (섹션 핸들, 문맥 +0xc0) → +0x42 ObDuplicateObject
            │  +0xbf ~ +0x112  OutputFlags (+0x10)
            │  +0x11a ~ +0x12b ManifestAddress · ManifestSize (+0x48 · +0x50, 찾았을 때만)
            │  +0x133 · +0x13c FileHandle · SectionHandle (+0x18 · +0x20)
            │  +0x140 ~ +0x172 +0x28 · +0x30 · +0x34 · +0x38 · +0x40
            │  +0x191          State (+0x8) = 6   ← 맨 마지막
            (실패면 +0x115e · +0x1196 ObCloseHandle 프로세스 → 스레드 핸들)

[test.exe 첫 스레드 — ② ntdll 이 부른 시스템 콜 0x102, 커널 안]
nt!NtGetNlsSectionPtr (종류 0xb · 0xe)
 ├─ +0x102  RtlpInitNlsSectionName      \NLS\NlsSectionCP949 / \NLS\NlsSectionCASE
 │  +0x11d  속성 0x2d0 (영구 · 대소문자 무시 · 있으면 열기 · 커널 핸들) · +0x163 접근 4
 ├─ +0x172  ZwOpenSection               (실패면 +0x370 → 파일로 만들기 — 부팅 때 갈래, test.exe 는 안 탈 것 [추측])
 ├─ +0x1b4  ObReferenceObjectByHandle   모드 0 (커널)
 ├─ +0x1c0  ZwClose                     ← 핸들은 바로 닫음 (프로세스 핸들 표 안 건드림)
 │  +0x25b 보호 2 · +0x27d 크기 0 · +0x2ab 정렬 0x10000 · +0x250 할당 종류 0x400000 · 시작 주소 0
 ├─ +0x2eb  MiMapViewOfSection          현재 프로세스 = test.exe
 │          └─ +0x2de MiMapViewOfDataSection
 │                    ├─ +0x1d7 MiAllocatePool     (0x88B, 'Vad ') — _MMVAD 한 개
 │                    ├─ +0x2fb MiSelectUserAddress
 │                    ├─ +0x341 MiUpdateVadStartVpn · +0x3cb MiWriteVadFlags2
 │                    ├─ +0x461 MiInsertVad
 │                    └─ +0x772 MiSetVadFlags
 ├─ +0x308  ObfDereferenceObject        섹션 참조 놓음 (남는 참조는 뷰 쪽 [추측])
 │  +0x316  넷째 출력 ← 뷰 주소 · +0x31e 다섯째 ← 크기 (ntdll 은 NULL)
```

### 1. Q88 — 핸들 만들기 블록과 접근 마스크 (결과서 질문 1-1)

- `PspCreateObjectHandle` (rva `0x84509c`, 175B) 의 둘째 인자는 **0x190 바이트 "핸들 만들기 블록"** — `NtCreateUserProcess` 지역 둘 (프로세스 `RBP+0x400`, 스레드 `RBP+0x590`), `+0xc1 MOV EBX,0x190` 로 0 으로 채움.

| 블록 칸 | 크기 | 뜻 |
|---|---|---|
| `+0x00 ~ +0x9f` | 0xa0 | `_ACCESS_STATE` (PDB) |
| `+0xa0 ~ +0x17f` | 0xe0 | `_AUX_ACCESS_DATA` (PDB) — `PspInsertProcess +0x199 LEA R9,[RSI+0xa0]` 와 크기로 맞춤 |
| `+0x180` | 4 | 핸들 속성 |
| `+0x184` | 1 | 접근 모드 (`PreviousMode`) |
| `+0x188` | 8 | 핸들 값 출력 (커널 스택 칸) |

- 블록 전체 타입은 PDB 에 없다 (`pdb_struct.py --list` — 결과서). 뒤 세 칸 이름은 쓰임으로 붙인 것 `[정적 — 이름 없음]`.
- **접근**: 부모 인자 `ProcessDesiredAccess` (`[RBP+0x54]`) · `ThreadDesiredAccess` (`[RBP+0x50]`) → `PspInsertProcess +0x1a5` / `PspInsertThread +0x3aa` `SeCreateAccessStateEx` 가 블록에 접근 상태를 채움 (일반 매핑 = 형식 개체 `+0x4c`) → `ObOpenObjectByPointer` 는 `DesiredAccess` 0 으로 그 접근 상태를 그대로 씀.
  - 보호 프로세스 호환 검사 (`+0xe94 PsTestProtectedProcessIncompatibility`) 가 0 이면 인자 그대로 (`+0xe96 JZ` → 함수 시작 + `0xf6f`). 1 이면 `MAXIMUM_ALLOWED` (비트 25) 일 때 표 `RtlProtectedAccess2` 로 줄이고 비트 0 을 켬 (`+0xf08 ~ +0xf51`, `+0xf2c OR R8D,R14D`). 비트 0 의 이름은 `[추측]` (`PROCESS_TERMINATE`). test.exe 가 어느 쪽인지는 동적 D4.
  - 핸들 항목에 들어가는 값: `ObpCreateHandle +0x3cc ~ +0x431` 이 `MAXIMUM_ALLOWED` → `GENERIC_ALL` (`+0x3cf` · `+0x3d5` · `+0x3d9`) → 일반 매핑으로 풀고 `([+0x10] | [+0x14]) & (형식 ValidAccessMask [+0x5c] | 0x1000000)` (`+0x41b ~ +0x431`). 이 갈래 조건은 결과서가 안 봄 `[추측: 핸들 열기 길]`. 최종 숫자는 동적 D2.
- **속성**: 프로세스 `NtCreateUserProcess +0x251 ~ +0x266` (`& 0x1df2` 유저 / `& 0x11ff2` 커널), 스레드 `PspAllocateThread +0x203` · 조각 `+0x2ca883` · `+0x20c` (같은 마스크, 개체 속성 NULL 이면 `+0x1cd` 0). `0x1df2` 에 `0x200` (커널 핸들) 이 없다 → 유저 호출이면 **부모 유저 핸들** (비트 이름은 공개 헤더 값 `[추측-이름]`).
- **모드**: 프로세스 `+0x1eb MOV [RBP+0x584],R12B`, 스레드 `PspAllocateThread +0x1db` (셋째 인자 ← `NtCreateUserProcess +0xe46 ~ +0xe49` `PreviousMode`).
- 핸들 표는 **현재 프로세스 = 부모 cmd** 의 것 (아직 새 프로세스로 붙지 않은 시스템 콜 안).

### 2. Q88 — 부모 출력 인자 둘 (결과서 질문 1-1 끝)

| 출력 (부모 유저 8B) | 값 | 쓰는 줄 | 미리 확인 |
|---|---|---|---|
| `ThreadHandle` (인자 2) | 스레드 블록 `+0x188` | **`PspInsertThread +0x573 MOV [RAX],RCX`** (`+0x564` · `+0x56b` — 열째 인자 `[RBP+0x80]`) | 입구 `+0x219 ~ +0x22e` "읽고 다시 쓰기" |
| `ProcessHandle` (인자 1) | `[RBP+0x588]` = 프로세스 블록 `+0x188` | **`NtCreateUserProcess +0x10f6 MOV [RCX],RAX`** (`+0x10e8` · `+0x10ef`) | 입구 `+0x201 ~ +0x216` 같은 방식 (한계 `0x7fffffff0000`) |

- 스레드 쪽은 예외 필터 `PspInsertThread$filt$1` (유저 호출일 때만 삼킴). 쓰기가 실패하면 (`+0x55e JS +0xa4a`) 방금 만든 스레드 핸들을 닫음.
- 프로세스 핸들 쓰기 (`+0x10f6`) 가 `NtCreateUserProcess$filt$0` 의 `try` 안인지는 예외 표를 안 봐서 `[공백]`.

### 3. Q88 — `PspUpdateCreateInfo` 와 부모 `PS_CREATE_INFO` (결과서 질문 1-2)

- 부모 `PS_CREATE_INFO` 는 **부모 유저 메모리 그 자체** — 문맥 `+0x30` 에 사용자 포인터 (`PspCaptureCreateInfo` 가 심음, 크기 `0x58` · 상태 0 검사). `PspUpdateCreateInfo +0x1d` `RBX = [RDX+0x30]` 로 곧장 씀 (이 함수엔 `try` 없음).
- **PDB 에 `_PS_CREATE_INFO` 가 없다** → 칸 이름 · 상태 번호 이름 (1 파일 열기 실패 · 2 섹션 실패 · 3 실행 형식 · 4 머신 · 5 이름 · 6 성공) 은 `[추측-이름]`, 오프셋 · 크기 · 출처 · 분기 모양은 `[정적]`.
- 상태 4 · 5 를 부르는 자리는 이 파일들 안에서 못 찾음 `[공백]`.
- **상태 6 (test.exe 부모 쪽)** 의 쓰기:

| 부모 칸 | 이름 `[추측-이름]` | 값 · 출처 | 줄 |
|---|---|---|---|
| `+0x10` (4B) | `OutputFlags` | 먼저 0 (`+0xbf`) → 비트 0 `ProtectedProcess` (`EPROCESS +0x5fa & 7 ≠ 0`) · 비트 4 `…Light` (`Type == 1`) · 비트 1 `AddressSpaceOverride` (`EPROCESS.Flags` 비트 17) · 비트 2 `DevOverrideEnabled` (전역 `PspGlobalFlags` 비트 0) · 비트 3 `ManifestDetected` (문맥 `+0x10` 비트 5) | `+0xbf ~ +0x112` |
| `+0x18` · `+0x20` (8B 둘) | `FileHandle` · `SectionHandle` | 부모 표로 옮긴 핸들 값 (아니면 0) — 문맥 `+0x8` 비트 5 일 때만 | `+0x133` · `+0x13c` |
| `+0x28` (8B) | `UserProcessParametersNative` | 새 프로세스 안 매개변수 사본 주소 (문맥 `+0xe0` ← `PspSetupUserProcessAddressSpace +0x170` · `+0x197`) | `+0x140 ~ +0x147` |
| `+0x30` (4B) | `UserProcessParametersWow64` | 문맥 `+0xe8` (64비트 0) | `+0x14b ~ +0x151` |
| `+0x34` (4B) | `CurrentParameterFlags` | 문맥 `+0xd8` 가 가리키는 매개변수 `Flags` (`+0x8`) | `+0x154 ~ +0x15e` |
| `+0x38` (8B) | `PebAddressNative` | `EPROCESS.Peb` (`+0x2e0`) | `+0x161 ~ +0x168` |
| `+0x40` (4B) | `PebAddressWow64` | 문맥 `+0xec` (64비트 0) | `+0x16c ~ +0x172` |
| `+0x48` · `+0x50` | `ManifestAddress` · `ManifestSize` | 문맥 `+0x130` · `+0x138` — `ManifestDetected` 일 때만 (`+0x115 TEST CL,8`) | `+0x11a ~ +0x12b` |
| **`+0x08` (4B)** | **`State`** | **6 — 맨 마지막** (`+0x189 TEST EDI` / `+0x18b JS` 로 실패면 건너뜀) | **`+0x191`** (조각 `FUN_140a48f2d +0x8`) |

- 공통 문: **문맥 `+0x10` 비트 1 이 꺼져 있으면 `State` 만** (`+0x4b JZ +0x189`). 이 비트는 `PspCaptureCreateInfo` 가 부모 `PS_CREATE_INFO +0x10` 비트 0 (`WriteOutputOnExit` `[추측-이름]`) 을 옮긴 것. 부모 KERNELBASE 가 켜 주는지는 nt 쪽에서 안 보임 → D1.
- **파일 · 섹션 핸들 사본** (`+0x75 TEST byte [RSI+8],0x20` / `+0x79 JZ`): 문맥 `+0x8` 은 `PspBuildCreateProcessContext` 가 본 **속성 번호별 비트** — 비트 5 = 이미지 이름 속성 `0x20005`. 부모가 이 속성을 주면 `PspPropagateHandle` 가 원본을 그대로 두고 (`ObDuplicateObject` 옵션 2 — "원본 닫기" 1 없음) 부모 표에 새 유저 핸들을 하나씩 더 만든다. 원본은 파일을 열 때 속성 `0x640` / `0x240` (`NtCreateUserProcess +0x56e ~ +0x57e` — `0x200` 포함 `[정적]`) 이라 커널 핸들일 것 `[추측]`. 원본을 닫는 쪽은 `PspDeleteCreateProcessContext` (`+0x11d0`) 일 것 `[추측]` — 다음 요청 후보 2.
- `PspPropagateHandle` 커널 호출이면 원본 칸을 0 으로 하고 넘기기만 (`+0x48` · `+0x4b`). 둘째 복제가 실패하면 첫째를 닫음 (`+0xb1 JS +0x1d1`).

### 4. Q89 — `NtGetNlsSectionPtr` 갈래 · 뷰 (결과서 질문 3)

| 종류 | 섹션 이름 | 파일 (만들기 갈래) | test.exe 가 타나 |
|---|---|---|---|
| **`0xb` 코드 페이지** | `\NLS\NlsSectionCP<10진>` | `\SystemRoot\System32\c_<%.3d>.nls` | **예** — ANSI (`RtlpInitCodePageTables +0x46`), ANSI ≠ OEM 이면 OEM (`+0x6c`) 도 |
| **`0xe` 대문자 표** | `\NLS\NlsSectionCASE` | `l_intl.nls` | **예** — `UseCaseMapping ≠ 0` (`RtlpInitUppercaseTables +0x3b`) |
| `0xc` 정규화 표 | `\NLS\NlsSectionNORM%.8x` | 레지스트리 `…\Control\Nls\Normalization` | 아니오 |
| 그 밖 (`0xd` 포함) | — | — | `0xc00000ef` · `0xc0000001` (`+0x152 CMP ESI,0xd` / `+0x155 JZ`) |

- 근거: `RtlpInitNlsSectionName +0xd SUB ECX,0xb` / `+0x10 JZ` · `+0x12 SUB ECX,1` / `+0x15 JZ` · `+0x1b CMP ECX,2` / `+0x1e JNZ` (sast/52 표와 같음 ✔).
- 유저 호출 길: 인자 거름 (`+0x6f ~ +0xdd`) → 이름 (`+0xfd`) → **열기 `ZwOpenSection` (`+0x16d`, 속성 `0x2d0` — 커널 핸들)** → `ObReferenceObjectByHandle` (`+0x1af`, 접근 `0xf001f`, 모드 0) → **`ZwClose` (`+0x1bb`)** → 뷰 (`+0x2e6`) → `ObfDereferenceObject` (`+0x303`) → 출력 (`+0x316` · `+0x31e`).
- 뷰 값: 섹션 `[RSP+0x48]` · 현재 프로세스 (`+0x212 ~ +0x22a`) · **시작 주소 입력 0** (`+0x65`, 유저가 정할 길 없음) · 정렬 `0x10000` (`+0x2ab`) · 위쪽 한계 `min(EPROCESS.HighestUserAddress − 1, 0x7ffffffeffff)` (`+0x288 ~ +0x2a3`) · **크기 0 = 섹션 전체** (`+0x27d`, 돌아올 때 같은 칸에 실제 크기 `+0x2f1`) · **보호 2** (`+0x25b`) · 할당 종류 `0x400000` (`+0x250`) · 상속 1 (`+0x2bf`).
- 커널 호출 (부팅 `ExInitializeNls`) 은 시스템 공간 뷰 (`+0x1da JZ +0x46e` → `+0x4be MiInsertInSystemSpace`) — test.exe 와 무관 (run57 그대로).
- **`MiMapViewOfSection` 안** (새로 본 것): 컨트롤 영역 플래그 (`+0x299 MOV EAX,[R14+0x38]`) 의 `PhysicalMemory` (`+0x29d BT EAX,0xa`) · `Image` (`+0x2a7 TEST AL,0x20`) 가 둘 다 꺼지면 **`+0x2d9 MiMapViewOfDataSection`**. NLS 섹션은 파일 기반 데이터 섹션 (`ZwCreateSection` SEC_COMMIT) 이라 이 갈래 `[정적 추정 — 섹션 플래그 값은 안 봄]`.
- **`MiMapViewOfDataSection` 안** (rva `0x907090`, 이름만): `+0x1d2 MiAllocatePool` 로 **`_MMVAD` (0x88B, PDB 크기와 같음) 한 개** → `MiSelectUserAddress` (`+0x2f6`) → `MiUpdateVadStartVpn` (`+0x33c`) → `MiWriteVadFlags2` (`+0x3c6`) → `MiInsertVad` (`+0x45c`) → `MiSetVadFlags` (`+0x76d`). VAD 칸 값은 본문을 안 읽어 `[공백]` (다음 요청 후보 3).

## 확인된 것

- `[정적]` **스레드 핸들이 프로세스 핸들보다 먼저** 부모 핸들 표에 들어간다 — `PspInsertThread +0x520` (`NtCreateUserProcess +0x1015` 안) → `NtCreateUserProcess +0x10a2`. 부모 출력 칸도 `ThreadHandle` (`PspInsertThread +0x573`) 이 `ProcessHandle` (`+0x10f6`) 보다 먼저.
- `[정적]` 두 핸들은 **부모의 유저 핸들 표** (속성 마스크 `0x1df2` 에 `0x200` 없음 · 모드 = `PreviousMode`).
- `[정적]` 접근은 `PspCreateObjectHandle` 인자가 아니라 미리 채운 접근 상태 (`SeCreateAccessStateEx`, `PspInsertProcess +0x1a5` · `PspInsertThread +0x3aa`). `ObOpenObjectByPointer` 의 `DesiredAccess` 는 0.
- `[정적]` 부모 `PS_CREATE_INFO` 성공 쓰기는 `PspUpdateCreateInfo(6, …)` 한 자리 (`NtCreateUserProcess +0x1124`), `State` 6 은 다른 칸을 다 쓴 **뒤** (`+0x191`). 문맥 `+0x10` 비트 1 이 꺼지면 `State` 만.
- `[정적]` 이미지 이름 속성이 있으면 파일 · 섹션 핸들 사본 둘이 부모 표에 더 생긴다 (`PspPropagateHandle → ObDuplicateObject`, 옵션 2). 조건 값은 `[공백]` (D1).
- `[정적]` `NtGetNlsSectionPtr` 는 섹션을 **커널 핸들로 열고 참조만 남기고 바로 닫는다** (`+0x11d` `0x2d0` · `+0x1af` 모드 0 · `+0x1bb ZwClose`) → test.exe 핸들 표는 그대로. (run57 "핸들" 이야기에 없던 것 ★)
- `[정적]` 뷰는 `MiMapViewOfSection +0x2d9 → MiMapViewOfDataSection +0x1d2 MiAllocatePool` 로 **호출마다 새 `_MMVAD` (0x88B) 한 개** — VAD 한 개 = 호출 한 번.
- `[정적]` + `[관찰]` (run44 P1a VAD `25996d20 ~ 25996d50` `C_949.NLS` · `25996d60 ~ 25996d62` `l_intl.nls`, 둘 다 `Mapped READONLY` — `04_baggage-read_P1a.txt:1539` · `:1540`): 호출 2번 (ACP = OCP, `UseCaseMapping` `0x6a5`) 과 VAD 2개가 맞는다. 그 VAD 를 이 호출이 만든 것은 콜스택을 안 봐서 여전히 `[추측]`.
- `[정적]` 요청서 "12 = 대문자 표" 는 틀림 — 대문자 표는 `0xe` (14), `0xc` (12) 는 정규화.

## 결과서와 다르게 적은 곳 · 보탠 것

- **짐 지도 절 번호**: 결과서는 부모 쪽 짐 표를 "새 7.6.13" 으로 제안했지만, **7.6.13 은 run109 가 이미 썼다** (sample dll CRT). 그래서 **7.6.14** 로 넣었다 (오케스트레이터 지시).
- **줄임**: 결과서 `①OP…` · `①OT…` · `①UC…` 를 짐 지도 3장 줄임 표에 넣었다. `①OT…` 는 이미 있는 `①IT…` (`①… → PspInsertThread (호출 +0x1015)`) 를 머리로 써서 `①IT… → PspCreateObjectHandle (호출 +0x520) → …` 로 적었다 (같은 사슬, 줄임만 맞춤).
- **NLS VAD 줄**: 결과서 제안대로 3.17 에 **노드 단위 줄 3** 을 두었다 (칸 단위가 아니라 2장 숫자 · 7.2 모름 23 은 그대로). 3.18 (`_MMVAD_SHORT`) 에는 "NLS 뷰는 긴 꼴 (0x88) — 이 절 줄 없음" 메모만.
- 노드 줄에 run44 VAD 노드 주소 `ffff828596c16a80` (`C_949.NLS`) · `ffff828596c15a40` (`l_intl.nls`) 를 붙였다 (`04_baggage-read_P1a.txt:1539` · `:1540` 첫 칸 — 이번 부팅 기준). 결과서엔 없고 flow-writer 가 같은 줄에서 읽은 것.

## 정적과 동적이 다른 곳

- 없다. 정적 갈래 (호출 2번, 읽기 전용 · 섹션 전체 · 64KB 정렬) 와 run44 VAD 줄 (두 개 · `READONLY` · 0x31쪽 / 3쪽, 시작 `…d20` · `…d60` 이 64KB (`0x10` 쪽) 정렬) 이 맞는다.
- **디버거가 붙어서 달라지는 점**: 없음 `[추측]` — 핸들 · 뷰 길에 디버거 깃발을 보는 갈래가 없다.

## 정정

- **sast/52 결과서 (질문 1 ③) "`MiMapViewOfSection` (`+0x2e1` call)" 은 오타** → call **`+0x2e6`** / ret `+0x2eb` (`+0x2e1` 은 그 앞 `MOV RCX,[RSP+0x48]`). run57 문서 (250줄 "정정") · flow.md 2.4 NLS 절 · 짐 지도 7.6.3 · 6장은 이미 `+0x2e6` 이라 **고칠 줄 없음** (확인만). 이전 run 문서 맨 위 정정 한 줄: 없음.
- **요청서 (sast/112) "타입 12 = 대문자 표" 틀림** → 대문자 표 `0xe` (14), `0xc` (12) = 정규화. flow.md · 짐 지도엔 이 틀린 값이 없다 (확인만).
- flow.md 2.1 `PspCreateObjectHandle` 절의 "결과를 복사하고, 핸들 값을 쓴다 (`+0x10b1 ~ +0x10f6`)" 를 나눔: `+0x10b1 ~ +0x10e4` = `PS_ATTRIBUTE` 이미지 정보 출력 칸 복사, `+0x10e8 ~ +0x10f6` = `ProcessHandle` 칸 쓰기. 틀린 것은 아니라 정정 줄 대신 보탬.

## 이전 run 에서 닫은 것

- run45 "갈 곳이 정해지지 않은 정적 거리" 중 **`PspCreateObjectHandle` · `PspUpdateCreateInfo` 가 부모 쪽에 쓰는 것 (일 33)** → 이번 바퀴 (짐 지도 7.6 · flow.md 4장 run45 줄 `(해결됨 → run117)`).
- run45 일 47 (`NtGetNlsSectionPtr` 커널 쪽) — run57 이 이미 닫았고, 이번에 **핸들 (커널 핸들 열고 닫기) · VAD 를 만드는 함수 (`MiMapViewOfDataSection`)** 를 보탰다.
- run57 7.6.3 "섹션 참조 — 남는 참조는 뷰 쪽 `[추측]`" 은 그대로 `[추측]` (이번에도 칸 단위로 안 봄).

## 아직 확인 필요

### 동적 (결과서 D1 ~ D7 — 대기열 번호는 오케스트레이터가 붙임. 멈춤점은 프로세스 조건: 부모 cmd 의 `NtCreateUserProcess` 때, 이어 test.exe)

- **D1** `nt!PspUpdateCreateInfo` 입구 (`ecx == 6`): `poi(rdx+0x30)` 에서 0x58B (`dq`) — 입구 값과 `NtCreateUserProcess+0x1129` 직후 값 비교. 같이 `rdx+0x8` (비트 5 = 이름 속성) · `rdx+0x10` (비트 1 `WriteOutputOnExit` · 비트 4 · 비트 5). → 파일 · 섹션 핸들 사본이 정말 생기나, `OutputFlags` 값.
- **D2** 부모 핸들 표의 두 핸들 값 · 접근: `nt!PspCreateObjectHandle` 입구 · 출구 (`PspInsertThread+0x525` · `NtCreateUserProcess+0x10a7`) 에서 `rdx+0x10/+0x14/+0x18` · `rdx+0x180/+0x184`, 출구 `rdx+0x188`. 그 값으로 부모 문맥에서 `!handle <값> f` — 스레드가 먼저인지 (번호), 접근 값, 속성.
- **D3** `nt!PspPropagateHandle` 입구: `cl` (모드 1), `[rdx]` (원본 핸들 — 커널 핸들인지), 끝 `[r8]`. 원본 커널 핸들을 누가 닫는지.
- **D4** `NtCreateUserProcess+0xe94` 직후 `al` (= 0 인지) + 입구 `r8d` · `r9d` (부모가 `MAXIMUM_ALLOWED` 를 쓰는지).
- **D5** `NtCreateUserProcess+0x10f6` · `PspInsertThread+0x573` 직후 부모 두 칸 값 = D2 출구 `rdx+0x188` 와 같은지.
- **D6** `nt!NtGetNlsSectionPtr+0x2eb`: `[rsp+0x50]` 뷰 주소 · `[rsp+0x128]` 뷰 크기 (쪽 올림인지), 콜스택 (`RtlpInitCodePageTables+0x4b` · `RtlpInitUppercaseTables+0x40` 말고 다른 호출자) — 짐 지도 7.18 (1) (run57 동적 1) 과 한 번에.
- **D7** `NtGetNlsSectionPtr+0x176 JS` (열기 실패) 가 test.exe 때 도는지 (정적으로는 안 돎 `[추측]`) — run57 동적 1 과 같음.

### 정적 (결과서 "다음 요청 후보")

- 1 (작음) `PspAllocateThread` 의 속성 출력 쓰기 — `PS_ATTRIBUTE` `ClientId` (문맥 `+0x18`) · `TebAddress` (`+0x20`) 쓰는 줄 (짐 지도 7.6.14 마지막 줄 `[공백]`).
- 2 `PspDeleteCreateProcessContext` (`NtCreateUserProcess +0x11d0`) — 문맥 커널 핸들 (파일 `+0xb0` · 섹션 `+0xc0` · IFEO `+0xc8`) 을 언제 닫나 (D3 짝).
- 3 **`MiMapViewOfDataSection` 본문 칸 단위** — `_MMVAD` 칸 (`StartingVpn` · `EndingVpn` · `u.VadFlags` · 보호 · `Subsection` 등) 쓰는 줄 → 짐 지도 3.17 · 3.18 (지금 칸 단위 0) 채우기. NLS · ApiSet · ntdll · exe 뷰 모두에 공용이라 이득이 큼.
- 4 KERNELBASE `NlsProcessInitialize` 의 `NtGetNlsSectionPtr` 부르는 자리 — 둘째 `C_949.NLS` · `l_intl.nls` 뷰 (짐 지도 7.18 (1)) 의 주인. flow.md 그림 2 `bn1` 상자 (run68 `[정적]` "`NtGetNlsSectionPtr 0xe · 0xb`") 와 같은 자리인지.
- 5 (낮음) `PspInsertProcess` / `PspInsertThread` 접근 상태의 주체 (현재 프로세스 대 새 프로세스 토큰) — D2 로 갈리면.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- **Q88 첫 프로세스 길** (`Src/nt/Process.cpp` `PspCreateObjectHandle` · `PspUpdateCreateInfo` 빈 함수): **맞음** — 부모가 없어 쓸 칸이 없다. 주석 오프셋 (`+0x10b1 ~ +0x10f6` · 성공 6 · 실패 `+0x75f` 1 · 2) 도 `.asm` 과 같음 ("6 = 성공" 이름만 `[추측-이름]`).
- **Q88 게스트가 `NtCreateUserProcess` 를 부르는 길** (`ext/syscall/syscall_layer_cpp/src/process/nt_create_user_process.cpp`) — **부분 일치**:
  - 속성 `& 0x1df2` **맞음**. `MAXIMUM_ALLOWED` → 모든 권한 **방향 맞음** (진짜는 형식 일반 매핑 — 프로세스 `0x1fffff` 가정은 `확인 필요`: `PsProcessType+0x4c`).
  - **다름 (a) 핸들 순서**: 우리는 프로세스 → 스레드, 진짜는 **스레드 먼저**. 핸들 번호가 중요하면 바꿔야 함 (번호는 빈 칸 규칙에도 좌우 — D2).
  - **다름 (b) `PS_CREATE_INFO`**: 우리는 `Size` · `State=6` · `PebAddressNative` 만, 나머지 0. 진짜는 `OutputFlags` · `UserProcessParametersNative` · `CurrentParameterFlags` 도, 이름 속성이 있으면 `FileHandle` · `SectionHandle` (+ 부모 표 핸들 둘), 매니페스트면 `ManifestAddress/Size`. 부모 코드가 읽는지는 `확인 필요`.
  - **다름 (c)** 파일 · 섹션 핸들 사본을 안 만든다 (위 (b) 와 같은 조건).
  - `Size` (`+0x0`) 는 진짜가 안 쓴다 — 부모가 이미 넣은 값이라 결과는 같음.
- **Q89** (`ext/syscall/stub-work/decisions/opt_startup_syscalls_model.md` 표 · `Test/LdrInitTest.cpp`): 종류별 이름 · 파일 · 뷰 줄 (`+0x2e6` · 크기 0 · 보호 2 · 64KB · 넷째 · 다섯째) · **핸들 안 만듦** · 부를 때마다 새 뷰 — **맞음**. "뷰 크기 = 쪽 올림" (TODO(VM)) · 주소 고르기 순서 · 간격 (`next_base_`) 은 **확인 필요** (D6, run44 ApiSet 뷰 `25996cd0` 다음 `25996d20` 이 C_949). kernelbase `NlsProcessInitialize` 호출 (`LdrInitTest.cpp:7283` 덫) 은 정적 범위 밖 (다음 요청 후보 4).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntkrnlmp pdb 줄 run117 · run 메모 한 줄), 그림 1 머리 · 노드 `s9` 새로 (스레드 핸들) · `s7` · `s8` 라벨 · `KeReadyThread` 번호 (5) → (6), 2.1 머리 run 목록 · 나무 (`PspInsertThread` (5) · (6), (11) · (12) 메모) · `PspInsertThread` 흐름 10번 · `PspCreateObjectHandle` 절 · `PspUpdateCreateInfo` 절, 2.4 머리 · NLS 절 (커널 핸들 · `MiMapViewOfDataSection`), 4장 run45 줄 (일 33 해결됨) · 새 줄 셋 (동적 D1 ~ D7 · 정적 1 ~ 5 · PE_emulator), 4.1 두 줄 고침 + 한 줄 새로, 5장 두 표.
- 짐 지도 (`baggage.md`): 기준 run117 줄 · 판 줄 · 1장 (`VadRoot` 줄 · run117 메모) · 2장 아래 (run117) 한 줄 (숫자 그대로) · 3장 머리 run117 줄 · **3장 줄임 셋 (`①OP…` · `①OT…` · `①UC…`)** · **3.17 노드 단위 줄 3** · 3.18 메모 · 5장 ① · ② 한 줄씩 · 6장 고친 줄 2 + 새 줄 3 · 7장 머리 · 7.6 (PDB 없는 타입 · C12 · "갈 곳" 줄 해결됨) · 7.6.3 (뷰 경로 끝 · 핸들 · 정정 확인) · **새 7.6.14 (부모 핸들 표 5줄 + 부모 유저 메모리 9줄)** · 새 7.48 · 8장.
- 이전 run 문서 맨 위 정정 한 줄: 없음.

## 디버거 메모 / 도구 메모

- 결과서: 스크립트 문제 없음. `PspUpdateCreateInfo` 는 조각 셋 + "주인 없는 덩어리" 다섯이 붙은 함수라 `.asm` 아래쪽까지 봐야 한다 (`State` 쓰기 `+0x191` 이 조각 `FUN_140a48f2d +0x8` 줄에 "(= 함수 +0x..)" 로 달려 있음).
- 짐 지도 메모 (flow-writer): VAD 는 이번에 처음으로 **노드 단위 줄** 을 3장에 두었다. 칸 단위 줄이 생기면 (다음 요청 후보 3) 노드 줄은 그 위 머리 줄로 옮기고 2장 숫자를 그때 바꾼다.
