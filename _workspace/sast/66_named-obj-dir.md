# 66 named-obj-dir — 정적 분석 요청

- 날짜: 2026-11-07
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run71
- 대기열: **Q136** (PE_emulator 요청 22, 원문 급함 높음. 사용자 결정 2026-11-07: 맨 앞, Q104 VM 단계 앞)
- 관련: run68 (sast/63 — `CsrClientConnectToServer` syscall 6, `BaseStaticServerData = [PEB+0x98]+8 − PEB+0x380 + PEB+0x88`, `_KernelBaseBaseDllInitialize +0x2fb ~ +0x469` 이 `+0x32b` 에서 씀, `BaseNlsDllInitialize` 가 `+0x178` 을 읽음), run34 (csrss 쪽 `basesrv` — 서버가 이 블록을 만드는 쪽), run67 (`BaseHeap`), Q84 (csrss 연결 결과 — 동적)
- 원문 (읽기만): `../PE_emulator/_workspace/02_reviewer/119_base-heap.md` "은퇴 뒤 새 stop 결론"

## 무슨 일이 있었나

- PE_emulator 은퇴 뒤 test.exe 가 `main` 첫 `Log` 에서 멈춘다 (PE_emulator 측정 사슬):
  - `main +0x12` → `Log +0x1e0` → `OutputDebugStringA` (ret `+0xc2`) → `CreateDBWinMutex` (ret `+0x371`) → `BaseGetNamedObjectDirectory` (ret `+0xb0`) → `BasepGetNamedObjectDirectoryForToken +0x2b0 MOV RAX,[BaseStaticServerData]` (kernelbase rva `0x3aedb8`) → `+0x2b7 MOVUPS XMM0,[RAX+0x20]` — **RAX = 0**
- PE_emulator 는 csrss 연결 (`BaseStaticServerData` 를 채우는 곳) 을 공사 중으로 둠. 그 앞 토큰 syscall (`NtOpenProcessToken`·`NtQueryInformationToken` 클래스 `0xc`·`0x1d`·`0x2a`) 은 기본 대답 (핸들 0) → 세션 0 갈래로 옴
- PE_emulator 는 csrss 가 없으니 이 블록을 **직접 지어** 넣어야 한다 — 그러려면 블록 모양과, 이 길이 실제로 읽는 칸·값이 필요하다

## 알고 싶은 것

1. **kernelbase 셋 본문 asm** — `CreateDBWinMutex` (rva `0x79260`, 1211B) · `BaseGetNamedObjectDirectory` (`0x7a790`, 440B) · `BasepGetNamedObjectDirectoryForToken` (`0x7a950`, 2677B)
   - 함수마다: 인자, 쓰는 전역 (예: 이름 있는 객체 폴더 핸들 캐시 `BaseNamedObjectDirectory`?·락), 하위 호출, **syscall 표** (이름·클래스·인자·조건), 반환
   - `BasepGetNamedObjectDirectoryForToken`: 토큰 syscall 셋 (`NtOpenProcessToken`/`NtOpenThreadToken`?·`NtQueryInformationToken` 클래스 `0xc` = 세션 ID·`0x1d`·`0x2a` — 클래스 이름은 PDB 열거 `_TOKEN_INFORMATION_CLASS` 로) 의 결과로 **갈래가 어떻게 갈리나** (세션 0 / 세션 N / AppContainer / 낮은 무결성 등), test.exe (세션 1, 보통 사용자 토큰 `[추측]`) 에서 타는 갈래. `+0x2b0 ~` 에서 `BaseStaticServerData` 의 **어느 칸들** (`+0x20` 외) 을 읽나, 그 뒤 폴더 이름을 어떻게 만들고 (`\Sessions\N\BaseNamedObjects`?) 어떤 syscall 로 여나 (`NtOpenDirectoryObject` 등)
   - 2677B 로 크다 — test.exe 갈래 (그리고 PE_emulator 가 지금 탄 세션 0 갈래) 만 자세히, 나머지는 조건·이름만
   - `CreateDBWinMutex`: 만드는 객체 (뮤텍스 `DBWinMutex`?, 보안 기술자), syscall (`NtCreateMutant` 등), `OutputDebugStringA` 가 그 뒤 무엇을 하나 (이름만 — `DBWIN_BUFFER` 섹션·이벤트 열기 등, 디버거 없으면 어느 갈래)
2. **`BaseStaticServerData` 가 가리키는 블록**
   - PDB 타입 이름 (`_BASE_STATIC_SERVER_DATA` 류 — kernelbase·kernel32·basesrv·ntdll PDB 에서 찾기, `pdb_struct.py`), 칸 표 (오프셋·크기·이름·뜻). 타입이 PDB 에 없으면 kernelbase·kernel32·basesrv 의 읽기·쓰기 줄로 칸을 모아 표로 (`+0x20`·`+0x178` 등)
   - `+0x20` 칸 = 이름 있는 객체 폴더 이름 `UNICODE_STRING` 인가, 값 꼴 (`\Sessions\1\BaseNamedObjects`?) — 그 `Buffer` 포인터도 csrss 주소라 옮겨야 하나 (kernelbase 가 읽을 때 `PEB+0x88 − PEB+0x380` 을 더하나)
   - **누가 언제 채우나**: run68 의 csrss 연결 길 (`CsrpConnectToServer` syscall 6 중 `NtMapViewOfSection` 결과 뷰 + 응답 블록) 에서 어느 값이 `[PEB+0x98]+8` 이 되나, 블록 자체는 csrss (`basesrv`) 가 만들어 공유 섹션에 둔 것인가 (basesrv 의 블록 채우는 함수 이름만 — run34 와 이어서)
   - PE_emulator 가 블록을 지어 넣을 때 **최소로 채울 칸 목록** (test.exe 초기화 + `OutputDebugStringA` 길 + `BaseNlsDllInitialize` `+0x178` 까지 읽는 칸)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernelbase | `^CreateDBWinMutex$`, `^BaseGetNamedObjectDirectory$`, `^BasepGetNamedObjectDirectoryForToken$` | 1 | 1 |
| kernelbase | `^OutputDebugStringA$` (또는 `OutputDebugStringW` — 실제 몸통) | 0 | 1 (그 뒤 갈래 이름만) |
| kernelbase | (refs) `BaseStaticServerData` `0x3aedb8` — 읽는 곳 전부 (함수+오프셋+칸) | — | 2 |
| kernel32 | (refs) kernel32 `BaseStaticServerData` — 읽는 곳 | — | 2 |
| basesrv | 블록 채우는 함수 (run34·`_index.tsv` 로 찾아 이름·칸만) | 0 | 2 |
| kernelbase / ntdll | `_KernelBaseBaseDllInitialize.asm`·`CsrClientConnectToServer`·`CsrpConnectToServer` (있음 — 다시 뽑지 않음) | — | 2 |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where <모듈 파일 이름> <이름>`. **kernelbase 는 `KernelBase.dll` (대소문자 그대로)**.
- 공용 부품 (`RtlInitUnicodeString`·`RtlAppendUnicodeToString`·`RtlAllocateHeap`·락·`swprintf` 류) 은 이름만.
- Ghidra 를 열기 전 도는지 확인은 `pgrep -fa 'ghidra.app.util.headless'` (run70 도구 메모 — `analyzeHeadless` 소문자로는 못 잡음)

## 이미 아는 것

- `[정적]` run68: `BaseStaticServerData = [PEB+0x98]+8 − PEB+0x380 + PEB+0x88` (`_KernelBaseBaseDllInitialize +0x32b` 쯤), kernel32 도 자기 칸에 같은 식 (`_BaseDllInitialize +0x189`), `BaseNlsDllInitialize` 가 `+0x178` 을 읽음, `CsrpConnectToServer` syscall 6 (`NtOpenSection`·`NtCreateSection`·`NtConnectPort`·`NtClose`·`NtMapViewOfSection`·`NtClose`), `CsrpClientConnectToServer` 는 안 탐
- `[관찰]` run44 사진: PEB `+0x88`·`+0x98`·`+0x380` 값 (있으면 그대로 옮겨 줘), kernelbase `.data` 는 사진에 없음
- PE_emulator 원문 (위)

## 이번에 안 하는 것

- Q137 (요청 23 — 로더 일꾼 풀 import 일감 길, 다음 바퀴)
- csrss 쪽 연결 처리 본문 (basesrv 는 블록 채우는 함수 이름·칸만)
- `OutputDebugStringA` 의 디버거·DBWIN 섹션 쪽 깊은 본문 (이름·갈래 조건만)

## 받고 싶은 것

- 질문별 답 + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다, 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋. 결과서 쓰기 전에 `.asm` 줄과 다시 대조
- **`BaseStaticServerData` 블록 칸 표** + PE_emulator 가 지을 **최소 칸 목록** (칸·값 꼴·정적으로 정해지는 값 / 동적)
- **syscall 표** (함수 셋, test.exe 갈래·세션 0 갈래 각각)
- PE_emulator 한 줄: 은퇴 뒤 `OutputDebugStringA` 를 지나려면 무엇을 채워야 하나
- 질문마다 **"정적으로 닫힘 / 동적 필요"**. 동적 몫 (블록 `+0x0 ~ +0x40` 실제 값·탄 갈래·세션 ID) 은 Q104 VM 단계에 얹을 멈춤점·읽을 칸으로
- 짐 지도 반영거리
