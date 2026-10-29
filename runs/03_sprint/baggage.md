# 짐 지도 — 초기화 동안 nt·ntdll 이 세팅하는 것

## 기준

- 빌드 26100.9457, 대상 test.exe (sample/)
- 범위: 프로세스 생성 (`NtCreateUserProcess`) → 은퇴 (`NtContinue` 로 `RtlUserThreadStart` 에 가는 순간)
- 근거: run43 (sast/38 체크리스트, 정적만) + **run44 (사진: dast/03 한 번의 실행에서 스냅샷 8장 → dast/04 덤프 읽기)**. 3장의 "이미 아는 쓴 자리" 는 run9 ~ run42 · sast/01 ~ 37 결과서 · ntdll `_refs` 에서 옮긴 것 (8장). 3장 근거 칸 끝 "사진" 과 2.1·7장의 구간은 run44 `[관찰]`
  - 사진 로그: `ms/logs/04_baggage-read_<P>.txt` (+ 보충 `_supp_<P>.txt`), 지점 정의·라이브 값: `ms/logs/03_baggage-snap.txt`. 덤프 원본 `ms/dump/03_baggage-snap/` (커밋 안 함)
  - **run45** (sast/39 일 목록, 정적만): 체크리스트 밖 짐 C1~C12 는 7.6, 본문을 읽어 닫은 6건은 3.1·3.2·3.8·3.9 (근거 칸 `sast39`) 와 7.3·7.5·7.7·7.8. 일 번호 ("일 55" 등) 는 `runs/03_sprint/run45-work-list.md` 1장
  - **run46** (sast/40 프로세스 힙, 정적만 — **Q78a**, Q78 첫 조각): PEB 힙 4칸 (3.1), ntdll 힙 전역 17개 + 목록 밖 자리 4줄 (3.8, 근거 칸 `sast40`, 줄 끝 "Q78a 로 닫힘"), `_HEAP` 머리 (7.6.1, 체크리스트 밖), 공용 부품 첫 표 (6장). 값 대조는 run44 P1a 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`). 시스템 콜 목록·동적 질문은 `runs/03_sprint/run46-process-heap.md`
  - **run47** (sast/41 exe 섹션 묶음, 정적만 — **Q78b**, Q78 둘째 조각): 커널 다섯 구조체 (3.20 `_SECTION` · 3.21 `_CONTROL_AREA` · 3.22 `_SEGMENT` · 3.23 `_SECTION_IMAGE_INFORMATION` · 3.24 `_FILE_OBJECT`) 모르는 칸 74 **전부 닫음** (근거 칸 `sast41`, 줄 끝 "Q78b 로 닫힘"). 파일 시스템이 쓰는 칸은 "다른 드라이버 (ntfs.sys)" 로 분류만 (ntfs.sys 는 안 뽑음). 값 대조는 run44 K1 덤프 읽기 로그 (`ms/logs/04_baggage-read_K1.txt` `K1 07`·`K1 08`). 호출 지점·동적 질문은 `runs/03_sprint/run47-section-chain.md`
  - **run48** (sast/42 부팅 준비 갈래, 정적만 — **Q93**): 프로세스 생성 때 "입력" 으로 읽는 커널 짐 (`PspSystemDlls` 표·칸, ntdll 섹션, 기본 주소, `PsNtdllExports`) 의 **출처**. 체크리스트 밖 커널 전역이고 **프로세스 생성 전 (부팅)** 에 생긴다 → 새 단계 표시 `Ⓑ` (쓰는 말), 새 절 **7.6.2**, 4장 짝 출처 경로, 5장 `Ⓑ` 순서. 2장 숫자는 안 바뀜. 값 대조는 run44 K1 덤프 읽기 로그 (`PsNtdllExports`). 호출 사슬·동적 질문은 `runs/03_sprint/run48-system-dll-boot.md`
- PDB: ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E`, ntdll `C3093720177DA6851519DCFFE8DF53FE` (flow.md 기준과 같음)
  - 커널 구조체 14개는 ntoskrnl PDB, 유저 구조체 7개는 ntdll PDB 로 셌다. 유저 7개는 두 PDB 에서 크기·칸·줄이 완전히 같다 (결과서 1번).
- 첫판 (2026-10-24, run43), 둘째판 (2026-10-25, run44 — 사진 구간), 셋째판 (2026-10-26, run45 — 일 목록·체크리스트 밖 짐), 넷째판 (2026-10-27, run46 — Q78a 프로세스 힙 구간), 다섯째판 (2026-10-28, run47 — Q78b exe 섹션 묶음), 여섯째판 (2026-10-29, run48 — Q93 부팅 준비 갈래, 체크리스트 밖). 칸 = PDB **직접 칸** (필드 목록의 `LF_MEMBER`, 비트 칸도 한 칸. 펼친 칸은 안 셈).
- 표 원본 (칸마다 오프셋·타입·비트, 쓴 자리 대조): `ms/disassemble/ntoskrnl/_baggage/`, `ms/disassemble/ntdll/_baggage/` (커밋 안 함. 열 설명은 sast/38 결과서 "표 파일 경로와 열 설명")

## 쓰는 말

- **공간**: `유저` (에뮬레이터 가상 주소 공간, 게스트가 직접 읽음) / `커널` (에뮬레이터 바깥, nt 엔진·syscall로만 닿음)
- **단계**: `①` 커널 단계 (`NtCreateUserProcess` ~ 유저로 넘어가기) / `②` ntdll 단계 (`LdrInitializeThunk` ~ `NtContinue`)
  - (run48) `Ⓑ` **부팅 단계** = 프로세스 생성 **전** (컴퓨터를 켤 때 한 번). 짐 지도 범위 (①·②) 앞이라 여기서 놓인 짐은 ① 에서 보면 `입력` 이다. `①` 앞 사진 지점 `⓪` (`P0`) 과 섞이지 않게 `⓪ 이전` 대신 `Ⓑ` 로 쓴다 (sast/42 의견). 쪼갬: `Ⓑ0` 링크 때 (PE 파일에 박힌 처음 값) · `Ⓑ1` `Phase1` 안 `PsLocateSystemDlls` 구간 · `Ⓑ2` `PspInitPhase2` (`PspInitializeSystemDlls`) · `Ⓑ3` `MmInitSystemDll` · `Ⓑ4` `PspInitPhase3` (VBS 일 때만). 칸 수·구간은 7.6.2 에만 있고 2장 숫자에는 안 넣는다
- **놓는 법**:
  - `직접`: 함수가 칸에 바로 쓴다 (커널은 새 프로세스에 붙은 상태에서 유저 칸에 바로 쓴다)
  - `주소 넘김`: 칸 주소를 넘기고, 받은 함수가 쓴다
  - `붙이기`: 파일·섹션 내용을 주소 공간에 붙인다
  - `syscall`: ntdll이 syscall을 부르면 커널이 채운다
  - `입력`: 부모·부팅·파일·레지스트리에서 온 값
    - (run48) 부팅에서 온 입력 중 시스템 DLL 짐 (`PspSystemDlls` 칸·ntdll 섹션·`PsNtdllExports`) 은 **누가 놓았는지가 7.6.2 에 있다** (`Ⓑ` 줄). 4장 짝 줄 근거 칸 끝 "출처 `Ⓑ…`" 가 그 경로
- **쓴 주인**: `nt` / `ntdll` / `다른 DLL` (KERNELBASE·KERNEL32 등 — 범위 밖, 표시만) / `사용자 코드` (TLS·DllMain)
  - (run47) `다른 드라이버 (ntfs.sys)` = 커널 쪽 `다른 DLL`. 뜻이 같다 (범위 밖, 표시만, 에뮬레이터에선 입력으로 취급). 2장에선 `다른 DLL` 열에 센다. `nt (부르는 쪽 다른 드라이버)` = 쓰는 코드는 nt 공개 함수인데 부르는 쪽이 드라이버 — nt 로 센다
  - (run47) `—` + **아무도 안 씀** = 쓰는 줄이 없고, 할당 때 0 으로 채워진 그대로 (`memset`·풀 0). 아는 칸도 모르는 칸도 아니라 2장 아래 줄에서 따로 센다
- **태그**: flow-doc 표시 태그 그대로 (`[관찰]` / `[정적]` / `[소스]` / `[추측]` / `[공백]`)
- **사진 지점** (run44, 한 번의 실행, 시간 순):
  - `K1` `nt!PspAllocateThread` 입구 · `K2` `nt!PspInsertProcess` 입구 · `K3` `nt!PspCallProcessNotifyRoutines` 입구 (`PspInsertThread +0x807`) — 셋 다 부모 cmd 스레드의 `NtCreateUserProcess` 안
  - `P0` = ⓪ `nt!PspUserThreadStartup` 입구 · `P1` = ① `ntdll!LdrInitializeThunk` 입구 · `P1a` = ①-a `LdrpInitializeProcess +0x199d` (kernel32 준비 call 자리) · `P1b` = ①-b `+0x1bb6` (그래프 켜기 call 자리) · `P2` = ② `ntdll!NtContinue` 입구 — test.exe 첫 스레드
- **사진 구간** (칸마다 "처음 값이 생긴 때"):
  - `A~B` = A 에선 0 (또는 객체가 아직 없음), B 에선 0 이 아님 → A 와 B 사이에 처음 값. `K1 이전` = K1 에서 이미 값이 있음
  - `끝까지 0` = 읽힌 사진 모두 0. **"아무도 안 씀" 과 같지 않다** (0 을 씀 / 썼다 되돌림 / 조건이 안 맞아 안 씀을 못 가림). `*` = K1 ~ P1 은 읽기 불가 (ntdll `.data`·`.mrdata` 앞 3페이지, `PebLdr`)
  - `≤P1a` = K1 ~ P1 읽기 불가, P1a 에 이미 값 (더 못 좁힘) · `못 읽음` = 8장 모두 `????` (`.00cfg`) 또는 칸을 안 읽음 (VAD 노드, `!vad` 목록만)
  - `→ 바뀜 X` = 처음 값 뒤 X 구간에 또 바뀜. 모듈 항목·노드는 모듈마다 (`exe`·`ntdll`·`k32`·`kb`·`dll1`·`dll2`)
  - 구간은 **언제**만 말한다. **누가**는 말하지 않는다 (한 구간에 여러 함수·부모·다른 스레드가 섞임). 쓴 자리는 Q78
  - 트랩 프레임은 P1 부터 멈춤점 `int3`·시스템 콜 트랩 프레임이 같은 자리에 덮인다 → P1a 이후 바뀜은 짐이 아니다 (run44 "디버거 영향")

## 1. 공간 그림

- 숫자 = 3장에서 쓴 자리를 아는 칸 수 (주인별). `[모름 N]` = 7장으로 간 칸 수. 칸 수와 단계 표시는 3장 표에서 셌다.
- 한 칸을 nt 와 ntdll 이 둘 다 쓰면 양쪽에 센다.
- `[0 N]` = 사진 8장에서 모두 0 인 칸 수 (run44, `*` 는 K1 ~ P1 읽기 불가 포함). `[못 읽음]` = 사진에서 칸을 못 읽음. 구간별 숫자는 2.1.
- (run47) `(안 씀 N)` = 3장에서 "아무도 안 씀" 인 칸 수. `다른 드라이버 N` = 파일 시스템 (ntfs.sys) 몫 칸 수 (nt 와 같이 쓰는 칸은 양쪽에 셈).

```
유저 공간 (test.exe 주소 공간 — 게스트가 직접 읽음)
├─ PEB  _PEB 0x7d8 ............................................ ① nt 22 · ② ntdll 15   [모름 88]  [0 81]
│   └─ 프로세스 매개변수  _RTL_USER_PROCESS_PARAMETERS 0x450 .... ① nt 40 (그중 통째 복사 짐작 22) · ② ntdll 40 (힙으로 옮길 때 통째 복사)   [모름 0]  [0 19]
├─ TEB  _TEB 0x1878 (메인 스레드) ............................. ① nt 1 · ② ntdll 3   [모름 133]  [0 112]
│        (① MmCreateTeb 의 TEB 대입 약 47줄 — 칸별로 안 갈라 [공백]. 사진: K1~K2 에 9칸)
├─ ntdll 이미지 (커널이 ① 에 붙임)
│   ├─ .data  rva 0x1cc000 · 0x8f58 · 전역 438 ................. ② ntdll 64 · nt 1 (syscall)   [모름 373]  [0 265*]
│   │   └─ PebLdr = _PEB_LDR_DATA 0x58 .......................... ② ntdll 7   [모름 2]  [0 4*]
│   ├─ .mrdata  rva 0x1e7000 · 0x35b8 · 전역 50 ................ ① nt 1 (LdrSystemDllInitBlock) · ② ntdll 9   [모름 40]  [0 32*]
│   └─ .00cfg  rva 0x1e6000 · 0x28 · 5칸 ......................... [공백] (짐작 1, 쓰는 코드 줄 없음)   [모름 4]  [못 읽음 5]
├─ 힙 위, 모듈마다 한 쌍 (`LdrpAllocateModuleEntry` 가 같이 만듦 — 항목 주소가 프로세스 힙 `PEB.ProcessHeap` = `LdrpHeap` 영역 안, run44 [관찰])
│   ├─ 모듈 항목  _LDR_DATA_TABLE_ENTRY 0x138 .................. ② ntdll 31 · ② nt 1 (DllBase, syscall)   [모름 34]  [0 25]
│   └─ 그래프 노드  _LDR_DDAG_NODE 0x50 ......................... ② ntdll 6   [모름 4]  [0 3]
└─ KUSER_SHARED_DATA  0x7ffe0000 · 0xa80 ......................... [공백] (ntdll 은 읽기만. 사진: 바뀌는 건 시각 칸뿐)   [모름 103]  [0 41]

커널 공간 (에뮬레이터 바깥 — nt 엔진·syscall 로만 닿음)
├─ 프로세스 객체  _EPROCESS 0x840 ................................ ① nt 22   [모름 241]  [0 167]
│   ├─ Pcb = _KPROCESS 0x1c8 ...................................... [공백] (구조체 단위로만)   [모름 65]  [0 46]
│   ├─ ObjectTable (+0x300) → 핸들 표  _HANDLE_TABLE 0x80 ......... [공백]   [모름 17]  [0 7]
│   ├─ VadRoot (+0x558) → 메모리 구역 장부  _MMVAD 0x88 / _MMVAD_SHORT 0x40 ... [공백]   [모름 9 / 14]  [못 읽음 23]
│   ├─ Token (+0x248) → 토큰  _TOKEN 0x5c8 ......................... [공백]   [모름 55]  [0 26]
│   ├─ SectionObject (+0x2a8) → exe 섹션 묶음 (① 부모 스레드, run47 에서 모두 닫힘)
│   │   ├─ _SECTION 0x40 ........................................... ① nt 9   [모름 0]  [0 4]
│   │   ├─ _CONTROL_AREA 0x80 ...................................... ① nt 16 (안 씀 1)   [모름 0]  [0 5]
│   │   ├─ _SEGMENT 0x48 ........................................... ① nt 11   [모름 0]  [0 2]
│   │   └─ 이미지 정보  _SECTION_IMAGE_INFORMATION 0x40 ............ ① nt 24 (안 씀 2)   [모름 0]  [0 11]
│   └─ ImageFilePointer (+0x330) → 파일 객체  _FILE_OBJECT 0xd8 ... ① nt 18 · 다른 드라이버 (ntfs.sys) 5 (안 씀 8)   [모름 0]  [0 15]
└─ 스레드 객체  _ETHREAD 0x798 (메인 스레드) ...................... ① nt 5   [모름 144]  [0 107]
    ├─ Tcb = _KTHREAD 0x4c0 ...................................... ① nt 10 (그중 ApcState 는 ② 끝에도)   [모름 227]  [0 140]
    └─ 트랩 프레임  _KTRAP_FRAME 0x190 (InitialStack − 0x190) .... ① nt 6 · ② 끝 nt 4 (NtContinue)   [모름 60]  [0 32]
```

- 칸 오프셋 (`+0x300` 등) 은 PDB 칸이고, 화살표 (→) 는 "그 칸이 가리키는 것" 이다 (결과서 Q77 추천 표). 유저 스택 위 맡긴 문맥 (`_CONTEXT`), API set 지도, 환경 블록 같은 구역은 아직 칸 표가 없어 그림에 없다 → 7장.
- 체크리스트 밖에 남는 짐 (다른 DLL 의 섹션·IAT 칸·힙 안 블록·일꾼 스레드·핸들 표 항목·부모·csrss 쪽, run45 C1~C12) 도 그림에 없다 → 7.6.
- (run48) 부팅 때 놓인 커널 전역 (`PspSystemDlls` 표·칸, 시스템 DLL 섹션 객체, `PsNtdllExports`, `PsWowX86SharedInformation`, `MiState.Processes.SystemDllBase`, System 프로세스 안 ntdll 뷰) 은 칸 표가 7.6.2 에 있지만 **체크리스트 밖·단계 `Ⓑ` (프로세스 생성 전)** 이라 그림에 안 넣었다. 유저 쪽 "ntdll 이미지 (커널이 ① 에 붙임)" 가 붙이는 섹션이 그 `Ⓑ1` 섹션이다.
- (run46) 프로세스 힙 `_HEAP` 머리 칸 (힙 `25996f30000`, 유저 ②) 과 힙 관리자 큰 예약 둘 (`RtlpHpHeapManager` 의 희소 비트맵, 유저 ②, VAD 는 커널) 은 칸 표가 7.6.1·3.8 에 있지만 체크리스트 밖이라 그림 숫자에 안 넣었다.

## 2. 구조체별 요약

| 구조체·구역 | 공간 | 칸 수 | nt 가 씀 | ntdll 이 씀 | 다른 DLL | 0 그대로 (사진) | 아직 모름 |
|---|---|---|---|---|---|---|---|
| `_PEB` | 유저 | 124 | 22 | 15 | 0 | 81 | 88 |
| `_RTL_USER_PROCESS_PARAMETERS` | 유저 | 40 | 40 | 40 | 0 | 19 | 0 |
| `_TEB` | 유저 | 137 | 1 | 3 | 0 | 112 | 133 |
| `_PEB_LDR_DATA` | 유저 | 9 | 0 | 7 | 0 | 4* | 2 |
| `_LDR_DATA_TABLE_ENTRY` | 유저 | 65 | 1 | 31 | 0 | 25 | 34 |
| `_LDR_DDAG_NODE` | 유저 | 10 | 0 | 6 | 0 | 3 | 4 |
| `_KUSER_SHARED_DATA` | 유저 | 103 | 0 | 0 | 0 | 41 | 103 |
| ntdll `.data` 전역 | 유저 | 438 | 1 | 64 | 0 | 265* | 373 |
| ntdll `.mrdata` 전역 | 유저 | 50 | 1 | 9 | 0 | 32* | 40 |
| ntdll `.00cfg` 전역 | 유저 | 5 | 0 | 0 | 0 | 못 읽음 (5칸 모두) | 4 (+ 짐작 1 은 주인 모름) |
| `_EPROCESS` | 커널 | 263 | 22 | 0 | 0 | 167 | 241 |
| `_KPROCESS` | 커널 | 65 | 0 | 0 | 0 | 46 | 65 |
| `_ETHREAD` | 커널 | 149 | 5 | 0 | 0 | 107 | 144 |
| `_KTHREAD` | 커널 | 237 | 10 | 0 | 0 | 140 | 227 |
| `_KTRAP_FRAME` | 커널 | 66 | 6 | 0 | 0 | 32 | 60 |
| `_HANDLE_TABLE` | 커널 | 17 | 0 | 0 | 0 | 7 | 17 |
| `_MMVAD` | 커널 | 9 | 0 | 0 | 0 | 칸 안 읽음 | 9 |
| `_MMVAD_SHORT` | 커널 | 14 | 0 | 0 | 0 | 칸 안 읽음 | 14 |
| `_TOKEN` | 커널 | 55 | 0 | 0 | 0 | 26 | 55 |
| `_SECTION` | 커널 | 9 | 9 | 0 | 0 | 4 | 0 |
| `_CONTROL_AREA` | 커널 | 17 | 16 | 0 | 0 | 5 | 0 |
| `_SEGMENT` | 커널 | 11 | 11 | 0 | 0 | 2 | 0 |
| `_SECTION_IMAGE_INFORMATION` | 커널 | 26 | 24 | 0 | 0 | 11 | 0 |
| `_FILE_OBJECT` | 커널 | 30 | 18 | 0 | 5 | 15 | 0 |
| **구조체 유저 7개 합** | | **488** | 64 | 102 | 0 | **285** | **364** |
| **구조체 커널 14개 합** | | **968** | 121 | 0 | 5 | **562** (+ 칸 안 읽음 23) | **832** |
| **ntdll 전역 3구역 합** | | **493** | 2 | 73 | 0 | **297** (+ 못 읽음 5) | **417** |

- 구조체 21개 직접 칸 **1,456** = 아는 칸 **249** (정확 195 · 짐작 54) + 아무도 안 씀 **11** + 모름 **1,196**. (run47 전: 아는 칸 186 (정확 143 · 짐작 43) + 모름 1,270)
  - (run47, sast/41 — Q78b) **exe 섹션 묶음 다섯 구조체의 모르는 칸 74 를 모두 닫았다**: 아는 칸 63 (정확 52 · 짐작 11) + 아무도 안 씀 11 (`_CONTROL_AREA.ModifiedWriteCount`, `_SECTION_IMAGE_INFORMATION.ZeroBits`·`ImageFlags.Reserved`, `_FILE_OBJECT` `memset` 만 8칸). 그리고 이미 아는 칸 `_CONTROL_AREA.u` 가 짐작 → 정확. 구조체별은 3장 각 절 머리 줄, 분류 표는 run47 문서 맨 앞.
  - 그 전 이야기: 결과서 (sast/38) 1번 표 합은 181 · 1,275 였고, run45 가 PEB `ActiveCodePage`·`OemCodePage` 2칸을 (sast/39), **run46 이 PEB `NumberOfHeaps`·`MaximumNumberOfHeaps`·`ProcessHeaps` 3칸을** (sast/40, Q78a) 아는 칸으로 옮겼다 (쓰는 줄 `[정적]`). `ProcessHeap` 은 이미 아는 칸이라 쓰는 줄만 채움. 구조체마다 "아는 칸 = 정확 + 짐작" 은 3장 각 절 머리 줄.
- ntdll 전역 (쓰기 가능 2구역 + `.00cfg`) **493** = 쓴 자리 아는 것 **76** (`.data` 65 · `.mrdata` 10 · `.00cfg` 1) + 모름 **417**. 결과서 (sast/38) 2번 표는 54 · 439 였고, run45 가 5개 (`.data` `LdrpMrdataLock`, `.mrdata` `LdrpMaximumUserModeAddress`·`LdrpAllocationGranularity`·`LdrpGuardCheckIcallNoESFptr`·`LdrpGuardDispatchIcallNoESFptr`) 를 (sast/39), **run46 이 `.data` 힙 전역 17개를** (sast/40, Q78a — 3.8 머리 줄) 옮겼다. 이름 없는 자리 (목록 밖 4줄) 는 전역 수에 안 셌다. 읽기 전용 `.rdata` 데이터 이름 1,546개 (`globals_rdata.tsv`) 는 짐이 아니라 안 셌다.
- **nt 가 씀 / ntdll 이 씀** = 3장 표에서 그 주인 줄이 하나라도 있는 칸 수. 한 칸을 둘이 쓰면 양쪽에 센다 (매개변수 `LoaderThreads`). syscall 로 커널이 채우는 칸은 nt 로 셌다 (`LdrpInitCompleteEvent`, DLL 항목 `DllBase`).
  - (run45) 매개변수 "ntdll 이 씀 40" 은 `RtlpInitParameterBlock` 이 블록을 힙 새 자리로 **통째 복사**한 줄 때문이다 (값은 옛 블록 그대로). 칸 값을 새로 정한 ntdll 줄은 `LoaderThreads` (IFEO)·포인터 9개 고침·`Environment`·`EnvironmentSize`·`EnvironmentVersion` 이다 (3.2).
- `ntdll 이 씀` 에는 **범위 밖 길 (종료·복제·API) 에서만** 쓰는 전역 2개가 들어 있다: `LdrpDllDirectory`, `LdrpForkInProgress`. 초기화 동안엔 처음 값 그대로일 후보 → 사진으로 가린다 (7장).
- **다른 DLL 열에 드라이버 포함 (run47부터)**: `_FILE_OBJECT` 의 5 = 파일 시스템 드라이버 (ntfs.sys) 몫 — `Vpb`·`FsContext`·`FsContext2`·`SectionObjectPointer` + `Flags` 일부 (`Flags` 는 nt 와 양쪽에 셈). nt 에 쓰는 줄이 없다는 것은 `[정적]`, 쓰는 모듈이 ntfs.sys 라는 것은 동적 확인 전 `[추측]` (D7). `ReadAccess`·`SharedRead`·`SharedDelete` 등 공유 접근 6칸은 쓰는 코드가 nt (`IoSetLinkShareAccess`) 라 nt 로 셌다 (부르는 쪽이 드라이버).
- **다른 DLL 0** (`_FILE_OBJECT` 밖) = 문서·결과서에 KERNELBASE·KERNEL32 등이 이 칸들에 쓰는 자리가 아직 하나도 없다는 뜻. 사진 (run44) 은 **KERNEL32·KERNELBASE 준비 구간 (`P1a~P1b`) 에 처음 값이 생긴 칸**을 보여 주지만 (PEB `ReadOnlySharedMemoryBase` 등, 매개변수 `ConsoleHandle`, `OwnerProcessId`, ntdll `Csr*` 전역 — 2.1·7.8), 그 구간엔 ntdll 코드도 섞여서 주인은 아직 못 정한다 → 0 그대로 두고 Q78.
- **0 그대로 (사진)** = 사진 8장 (run44) 에서 모두 0 인 칸 수 (2.1 의 "끝까지 0" 과 같은 수). `*` = K1 ~ P1 은 읽기 불가였고 읽힌 P1a ~ P2 가 모두 0. **"아무도 안 씀" 의 증거는 아니다** — 0 을 썼거나, 썼다 되돌렸거나 (`SpecialApcDisable`·`LdrpTlsLock` 등, 3장 "사진"), 조건이 안 맞았거나를 못 가린다. 아는 칸 중 끝까지 0 인 것도 여기 센다 (3장 줄과 같이 보면 갈린다).
- 범위 밖 길 전역 2개는 사진에서 그대로였다: `LdrpForkInProgress` 끝까지 0*, `LdrpDllDirectory` 앞 16바이트 끝까지 0* (3.8).
- **아직 모름** = 결과서 대조에서 쓴 자리를 못 찾은 칸. 이름은 7장에 다 있다.
- (run47) **아무도 안 씀** (표에 열 없음) = 3장에 "아무도 안 씀" 줄만 있는 칸 — 커널 **11** (`_CONTROL_AREA` 1 · `_SECTION_IMAGE_INFORMATION` 2 · `_FILE_OBJECT` 8), 유저·전역 0. 그래서 커널 다섯 구조체는 "nt + 다른 DLL − 양쪽 + 안 씀 = 칸 수" 다 (`_FILE_OBJECT` 18 + 5 − 1 (`Flags`) + 8 = 30).
- (run48) **이 표 숫자는 run47 그대로다.** 부팅 준비 짐 (7.6.2) 은 체크리스트 21종·ntdll 전역 밖 커널 전역이라 안 셌다. 7.6.2 안에서만 따로 센다: 시스템 DLL 칸 (0x40) 9칸 = 파일 값 `Ⓑ0` 5 + nt 가 씀 `Ⓑ1` 4, `PsNtdllExports` 21칸 = nt 가 씀 `Ⓑ2` 19 + 아무도 안 씀 2. 체크리스트 칸 중 값이 이 짐에서 오는 칸 (트랩 프레임 `Rip`·`Rdx`, ntdll `LdrpSystemDllBase`, `LdrSystemDllInitBlock`) 은 쓴 주인 그대로 (nt ① / ntdll ②) 두고 근거 칸에 출처만 달았다.

### 2.1 사진으로 본 처음 값 구간 (run44)

- 칸마다 "처음 값이 생긴 구간" 을 센 것. 열 이름은 "쓰는 말" 의 사진 구간. 칸 수 = 행 합.
- 모듈 항목·노드는 6개 모듈 중 **가장 이른** 구간 (exe·ntdll 은 거의 `P1~P1a`, KERNEL32·KERNELBASE·dll1·dll2 는 `P1a~P1b`). 끝까지 0 은 6개 모두 0 일 때만. 모듈마다는 3.5·3.6 줄과 7.2.
- "바뀜 있음" = 처음 값 뒤에 또 바뀐 칸 수 (KUSD 시각 칸·스레드 통계 칸·트랩 프레임 P1a 이후는 소음 — 쓰는 말).

| 구조체·구역 | 칸 수 | K1 이전 | K1~K2 | K2~K3 | K3~P0 | P0~P1 | P1~P1a | P1a~P1b | P1b~P2 | ≤P1a | 끝까지 0 | 못 읽음 | 바뀜 있음 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `_PEB` | 124 | 23 | · | · | 2 | · | 13 | 5 | · | · | 81 | · | 4 |
| `_RTL_USER_PROCESS_PARAMETERS` | 40 | 20 | · | · | · | · | 1 | · | · | · | 19 | · | 8 |
| `_TEB` | 137 | · | 9 | · | · | 6 | 7 | 1 | 2 | · | 112 | · | 4 |
| `_PEB_LDR_DATA` | 9 | · | · | · | · | · | · | · | · | 5 | 4 | · | 3 |
| `_LDR_DATA_TABLE_ENTRY` | 65 | · | · | · | · | · | 33 | 7 | · | · | 25 | · | 8 |
| `_LDR_DDAG_NODE` | 10 | · | · | · | · | · | 3 | 4 | · | · | 3 | · | 1 |
| `_KUSER_SHARED_DATA` | 103 | 62 | · | · | · | · | · | · | · | · | 41 | · | 8 |
| **유저 구조체 합** | **488** | **105** | **9** | **0** | **2** | **6** | **57** | **17** | **2** | **5** | **285** | **0** | **36** |
| `_EPROCESS` | 263 | 62 | · | 7 | 12 | 9 | 5 | 1 | · | · | 167 | · | 26 |
| `_KPROCESS` | 65 | 15 | · | 2 | 2 | · | · | · | · | · | 46 | · | 3 |
| `_ETHREAD` | 149 | · | 31 | 6 | 4 | · | 1 | · | · | · | 107 | · | 11 |
| `_KTHREAD` | 237 | · | 49 | 11 | 9 | 9 | 7 | 6 | 6 | · | 140 | · | 44 |
| `_KTRAP_FRAME` | 66 | · | 9 | · | · | · | 24 | 1 | · | · | 32 | · | 25 |
| `_HANDLE_TABLE` | 17 | 10 | · | · | · | · | · | · | · | · | 7 | · | 3 |
| `_MMVAD` | 9 | · | · | · | · | · | · | · | · | · | · | 9 | 0 |
| `_MMVAD_SHORT` | 14 | · | · | · | · | · | · | · | · | · | · | 14 | 0 |
| `_TOKEN` | 55 | 29 | · | · | · | · | · | · | · | · | 26 | · | 0 |
| `_SECTION` | 9 | 5 | · | · | · | · | · | · | · | · | 4 | · | 0 |
| `_CONTROL_AREA` | 17 | 12 | · | · | · | · | · | · | · | · | 5 | · | 2 |
| `_SEGMENT` | 11 | 9 | · | · | · | · | · | · | · | · | 2 | · | 0 |
| `_SECTION_IMAGE_INFORMATION` | 26 | 15 | · | · | · | · | · | · | · | · | 11 | · | 0 |
| `_FILE_OBJECT` | 30 | 15 | · | · | · | · | · | · | · | · | 15 | · | 2 |
| **커널 구조체 합** | **968** | **172** | **89** | **26** | **27** | **18** | **37** | **8** | **6** | **0** | **562** | **23** | **116** |
| ntdll `.data` 전역 | 438 | · | · | · | · | · | · | 24 | 6 | 143 | 265 | · | 34 |
| ntdll `.mrdata` 전역 | 50 | 5 | · | · | · | · | · | · | · | 13 | 32 | · | 5 |
| ntdll `.00cfg` 전역 | 5 | · | · | · | · | · | · | · | · | · | · | 5 | 0 |

- **사진으로 정해진 칸**: 구조체 1,456 중 **1,428** (구간 581 + 끝까지 0 847). 못 좁힌 28 = `≤P1a` 5 (`PebLdr`) + VAD 칸 안 읽음 23. ntdll 전역 493 중 **332** (구간 35 + 끝까지 0 297). **전역** 못 좁힌 161 = `≤P1a` 156 + `.00cfg` 못 읽음 5 (구조체 쪽 `≤P1a` `PebLdr` 5칸은 이 161 에 안 들어감 — run45 정정).
- `_KTRAP_FRAME` 의 `P1~P1a` 24칸은 짐이 아니다 (시스템 콜·`int3` 트랩 프레임이 같은 자리에 덮임). 짐으로 쓸 트랩 프레임 값은 `K1~K2` 9칸 (처음 문맥) 과 `P0~P1` 의 바꿔 끼우기 (`Rip`·`Rcx`·`Rdx`·`Rsp` 의 "바뀜") 이다.
- **모르는 칸만 따로** (7장, Q78 후보):
  - 유저 구조체 모름 364 (run46 전 367, run45 전 369) = K1 이전 66 (run45 전 68 — `ActiveCodePage`·`OemCodePage` 를 3.1 로) · K1~K2 8 · K3~P0 2 · P0~P1 5 · P1~P1a 14 (run46 전 17 — PEB 힙 목록 3칸을 3.1 로) · P1a~P1b 10 · P1b~P2 2 · 끝까지 0 257
  - 커널 구조체 모름 832 (run47 전 906) = K1 이전 106 (run47 전 147 — 섹션 묶음 41칸이 3.20 ~ 3.24 로) · K1~K2 72 · K2~K3 20 · K3~P0 26 · P0~P1 18 · P1~P1a 37 · P1a~P1b 8 · P1b~P2 6 · 끝까지 0 516 (run47 전 549 — 섹션 묶음 33칸) · 칸 안 읽음 23
  - ntdll 전역 모름 417 (run46 전 434, run45 전 439) = K1 이전 2 · P1a~P1b 17 · P1b~P2 6 · ≤P1a 114 · 끝까지 0 274 · 못 읽음 4 (run45 가 3.8·3.9 로 옮긴 5개: K1 이전 1 · ≤P1a 3 · 끝까지 0 1. run46 이 3.8 로 옮긴 17개: ≤P1a 11 · 끝까지 0 6)
  - → 구간이 `K1 이전` 이 아닌 곳으로 정해진 모르는 칸 **251** (유저 41 · 커널 187 · 전역 23. run46 전 254 — PEB 힙 목록 3칸이 닫힘) 은 그 구간 안 함수만 뒤지면 쓴 자리를 찾을 수 있다. `K1 이전` 174 (유저 66 · 커널 106 · 전역 2. run47 전 215, run45 전 218) 은 `PspAllocateProcess` 까지 (+ 부팅 때 값). (run47) 섹션 만들기·exe 열기 몫은 닫혔다 — 남은 커널 `K1 이전` 106 은 프로세스 객체·토큰·핸들 표 (Q78c).
- **아는 칸 (3장) 과 맞춰 보기**: 3장 243줄 모두 근거 칸 끝에 "사진" 을 달았다. 단계 (①/②) 와 구간이 어긋나는 줄은 없다. 정적 줄 뒤에 **다른 누가 또 바꾼 칸** 넷 (`PEB.ProcessParameters`·매개변수 `EnvironmentVersion`·`ConsoleHandle`·`EPROCESS.OwnerProcessId`) 은 3장 해당 절 머리와 7.8.
  - (run45) 그중 `PEB.ProcessParameters` 의 둘째 주인 (`RtlpInitParameterBlock`) 과 `EnvironmentVersion` 의 +1 (`RtlpInitEnvironmentBlock`) 은 3.1·3.2 줄로 들어갔다. 남은 것: `EnvironmentVersion` 나머지 +2, `ConsoleHandle`, `OwnerProcessId` (7.8). run45 가 더한 3장 8줄도 근거 칸 끝에 사진 구간을 달았다.
  - (run47) 3장 3.20 ~ 3.24 가 20줄 → **85줄** (3장 모두 281 → 346줄). 고친 줄은 3.21 2줄 (정정) · 3.23 "오프셋 미상" 14줄. 모두 근거 칸 끝에 사진 구간을 달았다. 단계 (①) 와 구간 (`K1 이전`·`끝까지 0`) 이 어긋나는 줄은 없다. 뒤에 또 바뀐 칸 셋 (`NumberOfPfnReferences`·`FilePointer` 아래 비트·`FILE_OBJECT.Flags`) 은 섹션 사슬 밖 길 (매핑·페이지 폴트 / exe 핸들 닫기) 줄로 갔다 — 사진 구간 (뒤에 바뀜) 과 맞음. `FILE_OBJECT.Event` 바뀜은 닫을 때 `IopResetEvent` `[추측]`.
  - (run46) 더한 3장 30줄 (3.1 4 · 3.8 26) 도 근거 칸 끝에 사진 구간을 달았다. 단계 ② 와 구간 (`P1~P1a`·`≤P1a`·`끝까지 0`) 이 어긋나는 줄은 없다. 결과서가 정적 계산과 P1a 덤프 값을 20곳 넘게 맞춰 봤다 (run46 "환경").

## 3. 짐 표

읽는 법:

- 3장에는 **쓴 자리를 아는 칸만** 둔다 (결과서 4번 대조의 정확·짐작). 모르는 칸은 7장.
- 줄 하나 = (칸, 쓰는 함수). 같은 함수 안 여러 줄은 오프셋을 `·` 로 이었다. 근거가 같은 칸 여럿 (범위로만 말한 것, 통째 복사 등) 은 한 줄에 모았다. 같은 칸을 다른 함수가 또 쓰면 줄을 나눴다.
- 줄의 출처는 결과서 4번 대조 (`known_writers.tsv`). 그 밖에 flow-writer 가 이 문서를 쓰다 flow.md 에서 본 쓴 자리 **6줄**을 더했다 (근거 칸 **flow-writer 더함**). 이미 아는 칸에 쓴 곳만 더해서 칸 수는 안 바뀐다.
  - (run45) sast/39 가 본문을 읽어 닫은 쓴 자리 **8줄**을 더했다 (근거 칸 `sast39`): `_PEB` 2 · `_RTL_USER_PROCESS_PARAMETERS` 3 · `.data` 1 · `.mrdata` 2. 그중 모르던 칸 7개 (PEB 2 · `.data` 1 · `.mrdata` 4) 가 아는 칸이 됐다 → 2장 숫자 바뀜.
  - (run46) sast/40 (Q78a, 프로세스 힙 구간) 의 쓴 자리 **30줄**을 더했다 (근거 칸 `sast40`): `_PEB` 4 · `.data` 26 (그중 이름 없는 목록 밖 자리 4줄). 모르던 칸 20개 (PEB 3 · `.data` 17) 가 아는 칸이 됐다 → 2장 숫자 바뀜. 줄 끝 **"Q78a 로 닫힘"** = 이 조각으로 닫힌 칸 (나머지 Q78 이 다시 안 봐도 됨). PEB `ProcessHeap`·`LdrpHeap` 두 줄은 고쳐 씀 (정정은 run46 문서).
  - (run47) sast/41 (Q78b, exe 섹션 묶음) 으로 3.20 ~ 3.24 를 다시 썼다 (근거 칸 `sast41`): 20줄 → 85줄. 모르던 칸 74 가 모두 아는 칸 (63) 이나 "아무도 안 씀" (11) 이 됐다 → 2장 숫자 바뀜. 줄 끝 **"Q78b 로 닫힘"** = 이 조각으로 닫힌 칸. 정정 2줄 (3.21 `NumberOfUserReferences` 함수 이름, `u` 짐작 → 정확) 은 run47 문서 "정정".
  - 결과서 4번은 flow.md 2.5 ~ 2.10 · run 문서를 검색으로만 봤다. 빠진 쓴 자리가 더 있을 수 있다 → Q78.
- **근거** 칸: `[정적]` = 확실도 정확 (쓰는 함수와 칸 이름·오프셋이 근거에 같이 있음), `[추측]` = 짐작 (왜 짐작인지는 무엇을 칸). `_refs` = ntdll 참조 표 (`ms/disassemble/ntdll/_refs/`) 의 WRITE·READ_WRITE·DATA 줄.
- **단계** 칸: `①` / `②` (쓰는 말) 에 더해 `② 끝` = `NtContinue` 안 (커널), `② 일꾼` = ntdll 일꾼 스레드 (flow.md 3장), `—` = 초기화 범위 밖 길 (종료·복제·API), `?` = 지도 경로 모름.
- **사진** (근거 칸 끝, run44 `[관찰]`): 그 줄의 칸이 사진에서 **처음 값을 가진 구간** (쓰는 말 "사진 구간"). 칸 단위다 — 그 줄의 함수가 쓴 값인지는 말하지 않는다 (같은 칸을 여러 줄이 쓰면 줄마다 같은 구간이 붙음). 칸이 여럿인 줄은 구간별로 묶었다 (`구간: 칸·칸 / 구간: 칸`). 모듈 항목·노드는 모듈마다 (`exe`·`ntdll`·`k32`·`kb`·`dll1`·`dll2`), 무엇을 칸이 특정 모듈 (`exe 항목`·`ntdll 항목`·`DLL 항목`) 이면 그 모듈만.
  - 3장 243줄 중 단계 (①/②) 와 구간이 어긋나는 줄은 없다. 정적 줄 뒤에 다른 누가 또 바꾼 칸은 절 머리 "사진" 과 7.8.
- **경로** 칸: 함수 이름 사슬, `→` 부름, `⇒` 꼬리 JMP. 오프셋은 마지막 함수 뒤 (그 함수 안 쓰는 줄. `호출 +0x..` 은 부르는 자리). 사슬 머리는 줄여 쓴다:

| 줄임 | 사슬 |
|---|---|
| `①…` | `NtCreateUserProcess` — 부모 cmd.exe 스레드의 시스템 콜 안 (flow.md 2.1) |
| `①T…` | `KiStartUserThread → PspUserThreadStartup` — test.exe 스레드, 유저로 가기 전 (2.3). `KiStartUserThread` 에는 call 이 아니라 `SwapContext` 의 `RET` 로 들어온다 |
| `①S…` | (run47) exe 섹션 사슬 머리: `①… → MmCreateSpecialImageSection (호출 +0x937) → MiCreateSection (호출 +0xcf)` (2.1) |
| `①SC…` | (run47) 섹션 새로 만드는 길: `①S… → MiCreateImageOrDataSection (호출 +0xf3) → MiCreateNewSection (호출 +0x380)` |
| `①SN…` | (run47) 그 아래 이미지 파일 맵: `①SC… → MiCreateImageFileMap (호출 +0x210)`. `①SN… +0x..` 는 `MiCreateImageFileMap` 안 줄 |
| `①F…` | (run47) exe 열기 사슬: `①… → IopCreateFile (호출 +0x6c6) → ObOpenObjectByNameEx (조각 FUN_1408c7e1f +0x6a) → ObpLookupObjectName (호출 +0x21f) → IopParseDevice (호출 +0xbc8)`. `①F… +0x..` 는 `IopParseDevice` 안 줄 |
| `Ⓑ…` | (run48, 부팅 — 프로세스 생성 전) `Phase1Initialization → IoInitSystem (호출 +0x36) → IoInitSystemPreDrivers (호출 +0x12)`. 짐 표는 7.6.2 (체크리스트 밖) |
| `ⒷL…` | (run48) `Ⓑ… → PsLocateSystemDlls (호출 +0x10f5) → PspLocateSystemDll (호출 +0x9e)` — 시스템 DLL 칸마다 |
| `Ⓑ2…` | (run48) `Phase1Initialization → Phase1InitializationIoReady (호출 +0x61) → PsInitSystem (호출 +0x1d5) → PspInitPhase2 (호출 +0x3b) → PspInitializeSystemDlls (호출 +0x172)` |
| `②…` | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess` (2.4) |
| `②M…` | DLL 하나를 붙여 명부에 올리는 `LdrpMapDllWithSectionHandle` 까지. kernel32·kernelbase: `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpFindOrPrepareLoadingModule → LdrpLoadKnownDll → LdrpMapDllWithSectionHandle` (2.5, 지도 이름 사슬). dll1·dll2: `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal → (일감) LdrpProcessWork → LdrpMapDllSearchPath → LdrpMapDllNtFileName → LdrpMapDllWithSectionHandle` (2.6) |
| `{ntdll · exe · DLL}` | 모듈 항목마다 부르는 자리가 다르다 (아래 표) |

모듈마다 부르는 자리 (flow.md 2.4·2.6, call 또는 ret):

| 함수 | ntdll 항목 | exe 항목 | DLL 항목 |
|---|---|---|---|
| `LdrpAllocateModuleEntry` | `LdrpInitializeNtdllDataTableEntry +0x6c` | `LdrpInitializeProcess +0x135e` | 의존 DLL `LdrpLoadDependentModuleInternal +0x115c`, `LdrLoadDll` 길 `LdrpFindOrPrepareLoadingModule +0x47a` |
| `LdrpInsertDataTableEntry` | `LdrpInitializeNtdllDataTableEntry +0xf7` | `LdrpInitializeProcess +0x1455` | `LdrpMapDllWithSectionHandle +0x146` |
| `LdrpInsertModuleToIndexLockHeld` | `… +0x116 LdrpInsertModuleToIndex` 안 | `… +0x14d2 LdrpInsertModuleToIndex` 안 | `LdrpMapDllWithSectionHandle +0x151` (직접) |
| `LdrpProcessMappedModule` | `LdrpInitializeNtdllDataTableEntry +0x124` | `LdrpInitializeProcess +0x14ee` | `LdrpMapDllWithSectionHandle +0x26c` |

**유저 공간**

### 3.1 `_PEB` — PEB (유저, `0x7d8`, ntdll PDB)

- 칸 124 · 아는 칸 36 (정확 24 · 짐작 12) · 모름 88 · 줄 20 (그중 run45 더함 2: `ProcessParameters` 둘째 주인, `ActiveCodePage`·`OemCodePage` · **run46 더함 4**: `NumberOfHeaps` 2줄·`MaximumNumberOfHeaps`·`ProcessHeaps` — 근거 칸 `sast40`, Q78a 로 닫힘)
- 사진 (run44): ① 줄 (`MmCreatePeb`) 칸은 모두 `K1 이전` 이거나 값이 0, ② 줄은 `P1~P1a` — 단계와 맞다. **`ProcessParameters` 는 nt 가 쓴 뒤 `P1~P1a` 에 다른 주소로 바뀐다** (`25996cb0000` → 프로세스 힙 안 `25996f37a50`, 옛 자리 VAD 는 사라짐) — ~~옮긴 쪽 `[공백]` → 7.8~~ → (run45) 옮긴 쪽 = ntdll `RtlpInitParameterBlock` (`LdrpInitializeProcess +0xc3f`), 아래 둘째 줄. 모르는 칸 중 `K3~P0` `pShimData`·`SystemDefaultActivationContextData` (부모 쪽 `[추측]`), `P1a~P1b` CSR 공유 메모리 칸 4개 등은 7.2.

- (run46, sast/40) **프로세스 힙 칸 넷이 닫혔다 (Q78a)**: `ProcessHeap` 쓰는 줄 = `LdrpInitializeProcessHeap +0x1a2`, 힙 목록 3칸 (`NumberOfHeaps`·`MaximumNumberOfHeaps`·`ProcessHeaps`) 은 힙보다 먼저 `RtlInitializeHeapManager` 가 준비하고 첫 힙을 넣을 때 `RtlpProcessHeapsInsert` 가 `NumberOfHeaps` = 1. 사진 `P1~P1a` 와 맞음. 읽기만 하는 `HeapSegmentReserve` 등 4칸 (`+0xc8 ~ +0xe0`, ① `MmCreatePeb`) 은 그대로. `+0x48 IFEOKey` 는 이 구간이 안 쓴다 (IFEO 핸들은 호출자에게 돌려줄 뿐, 사진 null).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `InheritedAddressSpace` · `+0x3` `BitField` · `+0x8` `Mutant` | ① | `PspAllocateProcess` 가 넘긴 견본을 복사 | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 · 사진 `끝까지 0`: `InheritedAddressSpace` / `K1 이전`: `BitField`·`Mutant` |
| `+0x10` `ImageBaseAddress` | ① | exe 를 붙인 주소 (`_EPROCESS.SectionBaseAddress` 를 옮김) | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 · 사진 `K1 이전` |
| `+0x18` `Ldr` | ② | ntdll 전역 `PebLdr` 주소 (빈 목록 3개로 채운 뒤) | 직접 | ntdll | `②… +0x1c1` | `[정적]` run18 · 정확 · 사진 `P1~P1a` |
| `+0x20` `ProcessParameters` | ① | 새 프로세스 안 매개변수 블록 주소 (복사가 끝난 뒤). 갈래는 기능 스위치 (Q35) | 직접 (켜짐 갈래는 `RtlWriteULong64ToUser`) | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters +0x52b · +0x347` | `[정적]` run32 · sast27 · 정확 · 사진 `K1 이전` → 바뀜 `P1~P1a` |
| `+0x20` `ProcessParameters` | ② | 힙으로 옮긴 새 블록 주소 — `RtlAllocateHeap(PEB.ProcessHeap, 0, 블록.Length)` 자리에 통째 복사·포인터 9개 고친 뒤 갈아끼움. 이어서 옛 블록을 `NtFreeVirtualMemory(MEM_RELEASE)` 로 푼다 | 직접 | ntdll | `②… → RtlpInitParameterBlock (호출 ret +0xc3f) +0x10d` | `[정적]` sast39 · run45 · 정확 · 사진 `K1 이전` → 바뀜 `P1~P1a` (`25996cb0000` → 힙 안 `25996f37a50`, 옛 VAD 사라짐 — 이 줄과 맞음) |
| `+0x30` `ProcessHeap` | ② | 프로세스 힙 주소 (NT 힙 `_HEAP` 시작, 예약 1MB — 7.6.1). `RtlpCreateHeap` 반환을 `+0x19c` 0 검사 뒤 씀 (0 이면 `0xc0000017`). ~~쓰는 줄은 callee 안 (안 봄)~~ → run46 | 주소 넘김 (칸 주소 `LdrpInitializeProcess +0xa39` `LEA R9`) → 받은 함수가 직접 씀 | ntdll | `②… → LdrpInitializeProcessHeap (호출 +0xa4b) +0x1a2` | `[정적]` run18 · sast40 · run46 · 정확 · 사진 `P1~P1a` (`25996f30000`) — **Q78a 로 닫힘** |
| `+0x38` `FastPebLock` | ② | ntdll 전역 `FastPebLock` 주소 (`RtlInitializeCriticalSectionEx` 로 만든 뒤) | 직접 | ntdll | `②… +0x9e1` | `[정적]` run18 · 정확 · 사진 `P1~P1a` |
| `+0x50` `ProcessInitializing` (`CrossProcessFlags` 비트 1) | ② | 켬 (`BTS.LOCK`) → 끝에 끔 (`BTR.LOCK`). WOW64 길은 `LdrpInitializeProcess +0x17e7` 이 끔 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x107 · +0x20f` | `[정적]` run17 · sast12 · 정확 · 사진 `P1~P1a` → 바뀜 `P1b~P2` |
| `+0x78` `TlsBitmap` · `+0x80` `TlsBitmapBits` · `+0x238` `TlsExpansionBitmap` · `+0x240` `TlsExpansionBitmapBits` | ② | TLS 자리 비트맵 — 비트맵 구조와 비트 칸을 가리키게 설정 (칸마다 줄은 묶음으로만 봄) | 직접 | ntdll | `②… +0x774 ~ +0x8a3` | `[정적]` run18 · sast13 · 정확 · 사진 `P1~P1a`: `TlsBitmap`·`TlsBitmapBits`·`TlsExpansionBitmap` / `끝까지 0`: `TlsExpansionBitmapBits` |
| `+0xb8` `NumberOfProcessors` · `+0xbc` `NtGlobalFlag` · `+0xc0` `CriticalSectionTimeout` · `+0xc8` `HeapSegmentReserve` · `+0xd0` `HeapSegmentCommit` · `+0xd8` `HeapDeCommitTotalFreeThreshold` · `+0xe0` `HeapDeCommitFreeBlockThreshold` | ① | 프로세서 개수·전역 플래그 등 — run10 은 `+0xb8 ~ +0xe0` **범위로만** 말함 | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[추측]` run10 · 짐작 · 사진 `K1 이전`: `NumberOfProcessors`·`CriticalSectionTimeout`·`HeapSegmentReserve`·`HeapSegmentCommit`·`HeapDeCommitTotalFreeThreshold`·`HeapDeCommitFreeBlockThreshold` / `끝까지 0`: `NtGlobalFlag` |
| `+0xe8` `NumberOfHeaps` | ② | 0 (힙 목록 준비, 힙보다 먼저) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) +0xe7` | `[정적]` sast40 · run46 · 정확 · 사진 `P1~P1a` (P1a = 1) — **Q78a 로 닫힘** |
| `+0xe8` `NumberOfHeaps` | ② | 1 — 첫 힙을 목록에 넣을 때. `PEB.ProcessHeap` 이 아직 0 일 때만 (다음 힙부터는 다른 길) | 직접 | ntdll | `②… → LdrpInitializeProcessHeap (호출 +0xa4b) → RtlpCreateHeap (호출 +0x191) → RtlpProcessHeapsInsert (호출 +0x96f) +0xb9` | `[정적]` sast40 · run46 · 정확 · 사진 `P1~P1a` (1) — **Q78a 로 닫힘** |
| `+0xec` `MaximumNumberOfHeaps` | ② | `0x10` (정적 배열 `RtlpPebHeapListStaticBuffer` 칸 수) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) +0x10a` | `[정적]` sast40 · run46 · 정확 · 사진 `P1~P1a` (`0x10`) — **Q78a 로 닫힘** |
| `+0xf0` `ProcessHeaps` | ② | `&RtlpPebHeapListStaticBuffer` (ntdll `.data` 16칸 × 8바이트 배열). 배열 `[0]` 에 힙 주소를 넣는 줄은 3.8 `RtlpPebHeapListStaticBuffer` | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) +0xf5` | `[정적]` sast40 · run46 · 정확 · 사진 `P1~P1a` (`7ffafb833ec0`, `[0]` = `25996f30000`) — **Q78a 로 닫힘** |
| `+0x110` `LoaderLock` | ② | `&LdrpLoaderLock` | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0xfa` | `[정적]` run17 · sast12 · 정확 · 사진 `P1~P1a` |
| `+0x118` `OSMajorVersion` · `+0x11c` `OSMinorVersion` · `+0x120` `OSBuildNumber` · `+0x122` `OSCSDVersion` · `+0x124` `OSPlatformId` | ① | 버전 숫자 (exe 헤더 값이 있으면 덮어씀) — `+0x118 ~ +0x124` **범위로만** | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[추측]` run10 · 짐작 · 사진 `K1 이전`: `OSMajorVersion`·`OSBuildNumber`·`OSPlatformId` / `끝까지 0`: `OSMinorVersion`·`OSCSDVersion` |
| `+0x128` `ImageSubsystem` · `+0x12c` `ImageSubsystemMajorVersion` · `+0x130` `ImageSubsystemMinorVersion` | ① | exe 헤더의 서브시스템 값 (`+0x128 ~ +0x130`, 칸 3개와 정확히 맞음) | 직접 (값은 파일 헤더 = 입력) | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 · 사진 `K1 이전`: `ImageSubsystem`·`ImageSubsystemMajorVersion` / `끝까지 0`: `ImageSubsystemMinorVersion` |
| `+0x2c0` `SessionId` | ① | 세션 번호 | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 · 사진 `K1 이전` |
| `+0x34c` `ActiveCodePage` · `+0x34e` `OemCodePage` | ② | 코드 페이지 번호: 매니페스트 (`LdrpFusionManifestCodePages`) → 없으면 레지스트리 (`RtlpQueryNlsSystemCodePages`) → 실패면 `0xfde9`. 매개변수 `Flags` 비트 27 이면 곧장 `0xfde9` (`+0x77`, 두 칸 한 번에). `UseCaseMapping` (`+0x350`) 은 읽기만 | 직접 (값은 매니페스트·레지스트리 = 입력) | ntdll | `②… → LdrpInitializeNlsInfo (호출 ret +0x1cd) +0x4e · +0x55 · +0x77` | `[정적]` sast39 · run45 · 정확 · 사진 `K1 이전` — 8장 모두 `0x3b5` (949) 로 같음: ntdll 은 같은 값을 다시 씀. K1 전에 처음 쓴 쪽은 모름 (7.5) |
| `+0x7b8` `LeapSecondData` | ① | leap second 뷰 주소 (뷰는 `MmMapViewOfSection` `+0x132` 로 붙임) | 직접 (뷰는 붙이기) | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 · 사진 `K1 이전` |

### 3.2 `_RTL_USER_PROCESS_PARAMETERS` — 프로세스 매개변수 (유저, `0x450`, ntdll PDB)

- 칸 40 · 아는 칸 40 (정확 18 · 짐작 22 — run43 셈, nt 줄 기준) · 모름 0 · 줄 12 (그중 run45 더함 3, 모두 `[정적]` ntdll)
- 사진 (run44): P1 까지 `25996cb0000` 블록, P1a 부터 `25996f37a50` 블록 (오프셋끼리 비교). 블록 통째 복사 22칸 = `K1 이전` 8 · 끝까지 0 13 · `EnvironmentVersion` `P1~P1a` (옮긴 블록에서 0 → 3). Fixup 포인터 칸은 옮길 때 새 자리 기준으로 다시 고쳐진다 (`→ 바뀜 P1~P1a`). `ConsoleHandle` 은 `P1a~P1b` 에 4 (`\Reference {ConDrv}`) → `0x60` (`\Connect {ConDrv}`). `LoaderThreads` 는 끝까지 0 — 커널 기본값 `PsDefaultLoaderThreads` 도 0 이었을 것 `[추측]` (전역은 안 읽음). 다른 쓰는 쪽은 7.8.
- 블록 전체: 부모 (KERNELBASE) 가 만든 사본을 커널이 새 프로세스로 복사 (run32). 복사 뒤 ntdll `LdrpInitializeProcess +0x6c5 RtlNormalizeProcessParams` 가 무엇을 하는지는 모름.
- (run45) **ntdll 이 블록을 힙으로 옮긴다**: `+0xc32 RtlpInitEnvironmentBlock` 이 **옛 블록**의 `Environment`·`EnvironmentSize`·`EnvironmentVersion` 을 쓰고, 바로 뒤 `+0xc3f RtlpInitParameterBlock` 이 블록을 힙 새 자리로 통째 복사 → 포인터 9개 고침 → `PEB+0x20` 갈아끼움 → 옛 블록 해제 (sast/39). 사진의 "P1a 부터 `25996f37a50` 블록" 이 이것이다.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `MaximumLength` · `+0x4` `Length` · `+0xc` `DebugFlags` · `+0x18` `ConsoleFlags` · `+0x88` `StartingX` · `+0x8c` `StartingY` · `+0x90` `CountX` · `+0x94` `CountY` · `+0x98` `CountCharsX` · `+0x9c` `CountCharsY` · `+0xa0` `FillAttribute` · `+0xa4` `WindowFlags` · `+0xa8` `ShowWindowFlags` · `+0xf0` `CurrentDirectores` · `+0x3f0` `EnvironmentSize` · `+0x3f8` `EnvironmentVersion` · `+0x400` `PackageDependencyData` · `+0x408` `ProcessGroupId` · `+0x438` `DefaultThreadpoolCpuSetMaskCount` · `+0x43c` `DefaultThreadpoolThreadMaximum` · `+0x440` `HeapMemoryTypeMask` · `+0x448` `AttributeList` | ① | 부모 쪽이 만든 블록 값 **그대로** (블록 통째 복사). `PackageDependencyData` 포인터와 `CurrentDirectores` 안쪽은 Fixup 도 안 함 (run32, 이유 모름). 칸 단위로 말한 문서는 없음 | 입력 (통째 복사) | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters +0x41f · +0x324` | `[추측]` run32 · 짐작 · 사진 `K1 이전`: `MaximumLength`·`Length`·`StartingY`·`CountX`·`CountY`·`ShowWindowFlags`·`EnvironmentSize`·`ProcessGroupId` / `끝까지 0`: `DebugFlags`·`ConsoleFlags`·`StartingX`·`CountCharsX`·`CountCharsY`·`FillAttribute`·`WindowFlags`·`CurrentDirectores`·`PackageDependencyData`·`DefaultThreadpoolCpuSetMaskCount`·`DefaultThreadpoolThreadMaximum`·`HeapMemoryTypeMask`·`AttributeList` / `P1~P1a`: `EnvironmentVersion` |
| 블록 전체 (40칸 모두, 새 힙 블록) | ② | 옛 블록 (`PEB.ProcessParameters`) 을 `Length` (`+0x4`) 바이트만큼 힙 새 자리에 **통째 복사** — 값은 옛 블록 그대로 (앞 줄 `RtlpInitEnvironmentBlock` 이 쓴 환경 칸 포함) | 입력 (통째 복사) | ntdll | `②… → RtlpInitParameterBlock (호출 ret +0xc3f) → 메모리 복사 함수 (ret +0x57)` — 같은 주소 이름 4개, 후보: `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove` | `[정적]` sast39 · run45 · 정확 · 사진: P1a 부터 새 블록 (`25996f37a50`), 길이·`Flags`·핸들·문자열 길이가 옛 블록과 같음 (run44) |
| `+0x8` `Flags` | ① | 비트 29 `0x20000000` (조건 silo `OneCoreForwardersEnabled`) · 비트 15 `0x8000` (조건 `PspGlobalFlags` 비트 0). **붙기 전 커널 사본**에 쓰고 복사 때 같이 넘어감 | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace +0xe6 · +0xb1` | `[정적]` run32 · sast27 · 정확 · 사진 `K1 이전` |
| `+0x8` `Flags` | ① | 비트 14 `0x4000` (조건부) | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters +0x82` | `[정적]` run32 · sast27 · 정확 · 사진 `K1 이전` |
| `+0x10` `ConsoleHandle` | ① | 부모 콘솔 핸들을 새 프로세스용으로 복제한 값 (서브시스템 3 이고 핸들 > 0 일 때) | 주소 넘김 (`ObDuplicateObject`) | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters → ObDuplicateObject (ret +0xbc)` | `[정적]` run32 · 정확 · 사진 `K1 이전` → 바뀜 `P1a~P1b` |
| `+0x20` `StandardInput` · `+0x28` `StandardOutput` · `+0x30` `StandardError` | ① | 표준 핸들 복제 (복제할 목록이 있을 때, 세 번 도는 고리) | 주소 넘김 (`ObDuplicateObject`) | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters → ObDuplicateObject (ret +0x130)` | `[정적]` run32 · 정확 · 사진 `K1 이전` |
| `+0x38` `CurrentDirectory` · `+0x50` `DllPath` · `+0x60` `ImagePathName` · `+0x70` `CommandLine` · `+0xb0` `WindowTitle` · `+0xc0` `DesktopInfo` · `+0xd0` `ShellInfo` · `+0xe0` `RuntimeData` · `+0x410` `RedirectionDllName` · `+0x420` `HeapPartitionName` | ① | Fixup: 안쪽 `Buffer` 포인터만 새 주소 기준 절대 주소로 (`CurrentDirectory` 는 `DosPath.Buffer`) | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters` | `[정적]` run32 · 정확 · 사진 `K1 이전` → 바뀜 `P1~P1a`: `CurrentDirectory`·`ImagePathName`·`CommandLine`·`WindowTitle`·`DesktopInfo`·`ShellInfo` / `끝까지 0`: `DllPath`·`RuntimeData`·`RedirectionDllName`·`HeapPartitionName` |
| `+0x38` `CurrentDirectory` · `+0x50` `DllPath` · `+0x60` `ImagePathName` · `+0x70` `CommandLine` · `+0xb0` `WindowTitle` · `+0xc0` `DesktopInfo` · `+0xd0` `ShellInfo` · `+0xe0` `RuntimeData` · `+0x410` `RedirectionDllName` | ② | 안쪽 `Buffer` 포인터 (칸 `+0x40 +0x58 +0x68 +0x78 +0xb8 +0xc8 +0xd8 +0xe8 +0x418`) 를 새 블록 기준으로 (옛 값 + (새 − 옛)), 0 이 아닐 때만. `HeapPartitionName` 은 여기서 안 고침 | 직접 | ntdll | `②… → RtlpInitParameterBlock (호출 ret +0xc3f) +0x6b ~ +0x106` | `[정적]` sast39 · run45 · 정확 · 사진 바뀜 `P1~P1a`: `CurrentDirectory`·`ImagePathName`·`CommandLine`·`WindowTitle`·`DesktopInfo`·`ShellInfo` / `끝까지 0` (0 이라 안 고침): `DllPath`·`RuntimeData`·`RedirectionDllName` — 이 줄과 맞음 |
| `+0x80` `Environment` · `+0x430` `DefaultThreadpoolCpuSetMasks` | ① | Fixup: 새 주소 기준 절대 주소로 | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters` | `[정적]` run32 · 정확 · 사진 `K1 이전` → 바뀜 `P1~P1a`: `Environment` / `끝까지 0`: `DefaultThreadpoolCpuSetMasks` |
| `+0x80` `Environment` · `+0x3f0` `EnvironmentSize` · `+0x3f8` `EnvironmentVersion` | ② | 환경을 별도 힙 블록으로 복사 (`RtlpAllocateEnvBlock` + 복사, 환경이 없으면 `RtlCreateEnvironmentEx`) 해 그 주소·크기를 쓰고 `EnvironmentVersion` +1 (`INC`). **옮기기 전 옛 블록에** 쓰고, 다음 `RtlpInitParameterBlock` 통째 복사로 새 블록에 넘어감 | 직접 | ntdll | `②… → RtlpInitEnvironmentBlock (호출 ret +0xc32) +0x8d · +0x94 · +0x86` | `[정적]` sast39 · run45 · 정확 · 사진 `Environment` `K1 이전` → 바뀜 `P1~P1a` / `EnvironmentSize` `K1 이전` / `EnvironmentVersion` `P1~P1a` (0 → 3 — 이 줄은 +1 만. 나머지 +2 는 모름, `+0xa0 RtlpSetupEnvironmentHashTable` 쪽 `[추측]` → 7.8) |
| `+0x40c` `LoaderThreads` | ① | 0 이면 전역 `PsDefaultLoaderThreads` 로 채움 (붙기 전 커널 사본) | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace +0xb8 ~ +0xc7` | `[정적]` run32 · sast27 · 정확 · 사진 `끝까지 0` |
| `+0x40c` `LoaderThreads` | ② | IFEO `MaxLoaderThreads` 로 덮을 수 있음 (값이 있을 때) | 직접 | ntdll | `②… → LdrpInitializeExecutionOptions (호출 ret +0x536)` | `[정적]` run18 · run32 · 정확 · 사진 `끝까지 0` |

### 3.3 `_TEB` — TEB (메인 스레드) (유저, `0x1878`, ntdll PDB)

- 칸 137 · 아는 칸 4 (정확 4 · 짐작 0) · 모름 133 · 줄 4
- 사진 (run44): K1 엔 TEB 가 아직 없다 (전부 `????`). 아는 4줄은 단계와 맞다 (`NtTib` `K1~K2`, `CurrentLocale` `P0~P1` 값 `0x412`, `RanProcessInit`·`SchedulerSharedDataSlot` `P1~P1a`). 모르는 칸 8개가 `K1~K2` (`MmCreateTeb` 대입 후보, 7.2·7.5). `SchedulerSharedDataSlot` 값 `25996cb0000` = 옛 매개변수 블록 주소 (P1a 에 그 자리가 2쪽짜리 매핑 섹션이 됨).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `NtTib` | ② | 안쪽 `ArbitraryUserPointer`(+0x28) 만 DLL 이름으로 바꿨다가 `+0x17b ~ +0x17f` 에서 되돌림 | 직접 | ntdll | `②M… → LdrpMinimalMapModule +0xe2 ~ +0xea` | `[정적]` run38 · 정확 · 사진 `K1~K2` → 바뀜 `P1~P1a`·`P1a~P1b` |
| `+0x108` `CurrentLocale` | ① | `PsGetSessionLocaleId` 결과 (SEH `try` 안) | 직접 | nt | `KiStartUserThread → PspUserThreadStartup +0x78` | `[정적]` run15 · 정확 · 사진 `P0~P1` |
| `+0x17ee` `RanProcessInit` (`SameTebFlags` 비트 5) | ② | `SameTebFlags` 에 `0x20` 켬 (첫 스레드 길) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0xea` | `[정적]` run17 · sast12 · 정확 · 사진 `P1~P1a` |
| `+0x1850` `SchedulerSharedDataSlot` | ② | 락 소유 칸 표 주소 — 바로 앞 `+0x84 NtSetInformationThread`(클래스 0x39) 로 커널에서 받은 값. 조건이 안 맞으면 0 으로 남음 | 직접 | ntdll | `②… → LdrpAcquireSchedulerSharedDataSlot +0x92` | `[정적]` run41 · 정확 · 사진 `P1~P1a` |

### 3.4 `_PEB_LDR_DATA` — 로더 명부 머리 (= ntdll 전역 `PebLdr`) (유저, `0x58`, ntdll PDB)

- 칸 9 · 아는 칸 7 (정확 7 · 짐작 0) · 모름 2 · 줄 12
- 사진 (run44): K1 ~ P1 은 ntdll `.data` 안이라 읽기 불가 → `≤P1a`. P1a 에 `Length 0x58`·`Initialized 1`·목록 셋, 종료 칸 둘은 끝까지 0.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `Length` | ② | `0x58` (= PDB 크기) | 직접 | ntdll | `②… +0x158` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `≤P1a` |
| `+0x4` `Initialized` | ② | 바이트 하나 (값은 레지스터 `R15B` — 사진으로) | 직접 | ntdll | `②… +0x166` | `[정적]` `_refs` WRITE · 정확 · 사진 `≤P1a` |
| `+0x10` `InLoadOrderModuleList` | ② | 빈 목록 머리 | 직접 | ntdll | `②… +0x174 · +0x17b` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b` |
| `+0x10` `InLoadOrderModuleList` | ② | 항목 끼우기 — 꼬리 (exe 만 머리) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0xaf · +0xeb` | `[정적]` `_refs` WRITE · run35 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b` |
| `+0x20` `InMemoryOrderModuleList` | ② | 빈 목록 머리 | 직접 | ntdll | `②… +0x189 · +0x190` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b` |
| `+0x20` `InMemoryOrderModuleList` | ② | 항목 끼우기 — 꼬리 (exe 만 머리) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x57 · +0x107` | `[정적]` `_refs` WRITE · run35 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b` |
| `+0x30` `InInitializationOrderModuleList` | ② | 빈 목록 머리 (`+0x19e`·`+0x1a5`) → ntdll 항목 (핫패치면 패치 항목도) 머리에 끼움 (`+0x12a6`·`+0x12aa`·`+0x12d7`) | 직접 | ntdll | `②… +0x19e · +0x1a5 · +0x12a6 · +0x12aa · +0x12d7` | `[정적]` `_refs` WRITE · run35 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b`·`P1b~P2` |
| `+0x30` `InInitializationOrderModuleList` | ② | 나머지 모듈을 켜기 직전 꼬리에 (고리 A, exe 빼고) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse → LdrpInitializeNode +0x2e1` | `[정적]` `_refs` WRITE · run27 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b`·`P1b~P2` |
| `+0x48` `ShutdownInProgress` | ② | 처음 값 (`R14B`) | 직접 | ntdll | `②… +0x1ac` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |
| `+0x48` `ShutdownInProgress` | — | 종료 길에서 다시 씀 (범위 밖) | 직접 | ntdll | `LdrShutdownProcess +0xae` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |
| `+0x50` `ShutdownThreadId` | ② | 처음 값 (`R14`) | 직접 | ntdll | `②… +0x1b3` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |
| `+0x50` `ShutdownThreadId` | — | 종료 길에서 다시 씀 (범위 밖) | 직접 | ntdll | `LdrShutdownProcess +0xa7` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |

### 3.5 `_LDR_DATA_TABLE_ENTRY` — 모듈 항목 (모듈마다 하나) (유저, `0x138`, ntdll PDB)

- 칸 65 · 아는 칸 31 (정확 31 · 짐작 0) · 모름 34 · 줄 38 (그중 flow-writer 더함 1)
- 사진 (run44): exe·ntdll 항목은 P1a 에 이미 있고 (`P1~P1a`), KERNEL32·KERNELBASE·dll1·dll2 는 P1b 에 처음 보인다 (`P1a~P1b`). 값: `LoadReason` exe 4 · k32 4 · 나머지 0, `ObsoleteLoadCount` exe·ntdll·k32·kb `0xffff` · dll1·dll2 6, `ReferenceCount` kb 1 · 나머지 2, `TlsIndex` exe·kb·dll1·dll2 `0xffff`, `ImplicitPathOptions` k32·kb `0x4000`. KERNEL32·KERNELBASE 의 `ProcessAttachCalled` 는 `P1a~P1b` (kernel32 준비 안에서 시작 함수), dll1·dll2 는 `P1b~P2`.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `InLoadOrderLinks` | ② | 적재 순서 목록에 끼우기 — 꼬리 (exe 는 `+0xbd ~ +0x10e` 머리) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x87 ~ +0xaf` | `[정적]` run35 · 정확 · 사진 exe `P1~P1a` · ntdll `P1~P1a` → 바뀜 `P1a~P1b` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x10` `InMemoryOrderLinks` | ② | 메모리 순서 목록에 끼우기 — 꼬리 (exe 는 머리) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x4d ~ +0x57` | `[정적]` run35 · 정확 · 사진 exe `P1~P1a` · ntdll `P1~P1a` → 바뀜 `P1a~P1b` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x20` `InInitializationOrderLinks` | ② | ntdll 항목을 초기화 순서 목록 머리에. 나머지 모듈은 `LdrpInitializeNode` 고리 A 가 꼬리에 (위 `_PEB_LDR_DATA` 표) | 직접 | ntdll | `②… +0x1283 ~ +0x12d7` | `[정적]` run35 · run18 · 정확 · 사진 `P1~P1a` → 바뀜 `P1a~P1b` (ntdll) |
| `+0x30` `DllBase` | ② | exe 항목: `PEB.ImageBaseAddress` | 직접 | ntdll | `②… +0x144c` | `[정적]` run19 · 정확 · 사진 `P1~P1a` (exe) |
| `+0x30` `DllBase` | ② | ntdll 항목: 이미지 시작 주소 | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0xdc` | `[정적]` run35 · 정확 · 사진 `P1~P1a` (ntdll) |
| `+0x30` `DllBase` | ② | DLL 항목: 기준 주소 칸 = 이 칸 → **커널이 직접 채운다** (0 으로 시작) | syscall (`NtMapViewOfSection`) | nt | `②M… → LdrpMinimalMapModule +0x174` | `[정적]` run38 (flow.md 2.6) · 정확 · **flow-writer 더함** · 사진 `P1a~P1b` (k32·kb·dll1·dll2) |
| `+0x38` `EntryPoint` | ② | PE 헤더 `AddressOfEntryPoint` 로 정함 (ntdll 은 헤더 값이 0 이라 0) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpProcessMappedModule +0x4a ~ +0x78` | `[정적]` run35 · 정확 · 사진 exe `P1~P1a` · ntdll `끝까지 0` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x40` `SizeOfImage` · `+0x80` `TimeDateStamp` · `+0x120` `CheckSum` | ② | NT 헤더 값을 항목에 옮김 | 직접 (값은 헤더 = 입력) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0xa ~ +0x2e` | `[정적]` run35 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b`: `SizeOfImage`·`TimeDateStamp` / exe·dll1·dll2 `끝까지 0` · ntdll `P1~P1a` · k32·kb `P1a~P1b`: `CheckSum` |
| `+0x48` `FullDllName` | ② | exe 항목: `ImagePathName` | 직접 | ntdll | `②… +0x13aa` | `[정적]` run19 · 정확 · 사진 `P1~P1a` (exe) |
| `+0x48` `FullDllName` | ② | ntdll 항목: `<루트>\SYSTEM32\ntdll.dll` | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0xb6 ~ +0xd7` | `[정적]` run35 · 정확 · 사진 `P1~P1a` (ntdll) |
| `+0x58` `BaseDllName` | ② | exe 항목: 파일 이름 부분 | 직접 | ntdll | `②… +0x1408` | `[정적]` run19 · 정확 · 사진 `P1~P1a` (exe) |
| `+0x58` `BaseDllName` | ② | ntdll 항목: 상수 `NtDllName` (`ntdll.dll`) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0xb6 ~ +0xd7` | `[정적]` run35 · 정확 · 사진 `P1~P1a` (ntdll) |
| `+0x68` `Flags` | ② | 로드 문맥 `+0x20` 의 비트 몇 개를 옮김 (어느 비트인지·오프셋은 문서에 없음) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0x68` `ImageDll` (`Flags` 비트 2) | ② | 켬 | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0x11a` | `[정적]` run35 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x68` `ProcessStaticImport` (`Flags` 비트 5) | ② | 켬 — `LdrInitState == 1` 이고 지금 스레드가 `LdrpDllNotificationLock` 주인이 아닐 때 | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0x194` | `[정적]` run22 · 정확 · 사진 exe·ntdll·k32·kb `끝까지 0` · dll1·dll2 `P1a~P1b` |
| `+0x68` `InLegacyLists` (`Flags` 비트 6) | ② | 켬 (이미 켜졌으면 아무것도 안 함) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x5e` | `[정적]` run35 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x68` `InIndexes` (`Flags` 비트 7) | ② | 켬 (`BTS`) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0xec` | `[정적]` run35 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x68` `InExceptionTable` (`Flags` 비트 9) | ② | ntdll 항목: `Flags` 에 `0x204` 중 새 비트 | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x78 ~ +0x93` | `[정적]` run35 · 정확 · 사진 `P1~P1a` (ntdll) |
| `+0x68` `ProcessAttachCalled` (`Flags` 비트 19) | ② | 진입점을 부른 뒤 켬 (예외 없었을 때) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse → LdrpInitializeNode +0x231` | `[정적]` run27 · 정확 · 사진 exe·ntdll `끝까지 0` · k32·kb `P1a~P1b` · dll1·dll2 `P1b~P2` |
| `+0x6c` `ObsoleteLoadCount` | ② | ntdll 항목: `0xffff` (안 내려가는 모듈) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x78 ~ +0x93` | `[정적]` run35 · 정확 · 사진 `P1~P1a` (ntdll) |
| `+0x6c` `ObsoleteLoadCount` | ② | exe 항목: 문서엔 "`LoadCount` = -1" 만 있음 (`LoadCount` 는 PDB 상 노드 칸) | 직접 | ntdll | `②…` | `[추측]` run19 · 짐작 · 사진 `P1~P1a` (exe) |
| `+0x6c` `ObsoleteLoadCount` | ② | 새 항목 처음 값 = 6 (오프셋 미상) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x6e` `TlsIndex` | ② | DLL 항목: `0xffff` (TLS 디렉터리 있을 때) | 직접 | ntdll | `②… → … LdrpSnapModule → LdrpHandleTlsData +0x3a8` | `[정적]` run28 · 정확 · 사진 k32 `끝까지 0` · kb·dll1·dll2 `P1a~P1b` |
| `+0x6e` `TlsIndex` | ② | exe 항목: `0xffff` (TLS 디렉터리 있을 때) | 직접 | ntdll | `②… → LdrpInitializeTls +0x102` | `[정적]` run28 · 정확 · 사진 `P1~P1a` (exe) |
| `+0x70` `HashLinks` | ② | 해시 표 `LdrpHashTable` 버킷 꼬리에 (버킷 = 해시 & 0x1f) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x28 ~ +0x7a` | `[정적]` run35 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x98` `DdagNode` | ② | 같이 만든 새 그래프 노드 (항목 0x138 + 노드 0x50 한 쌍) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0xb0` `LoadContext` | ② | 로드 문맥 (문맥 `+0x38` = 항목, 서로 가리킴) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0x7f ~ +0x96` | `[정적]` run22 · 정확 · 사진 exe `P1~P1a` → 바뀜 `P1b~P2` · ntdll·k32·kb `끝까지 0` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0xb8` `ParentDllBase` | ② | 부모 `DllBase` | 직접 | ntdll | `②M… +0x286 ~ +0x28f` | `[정적]` run22 · run26 · 정확 · 사진 exe·ntdll·k32 `끝까지 0` · kb·dll1·dll2 `P1a~P1b` |
| `+0xc8` `BaseAddressIndexNode` | ② | 트리 색인 `LdrpModuleBaseAddressIndex` 에 넣기 (키 = `DllBase`) | 주소 넘김 (`RtlRbInsertNodeEx`) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0xe7` | `[정적]` run35 · 정확 · 사진 exe·ntdll `P1~P1a` → 바뀜 `P1a~P1b` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0xe0` `MappingInfoIndexNode` | ② | 트리 색인 `LdrpMappingInfoIndex` 에 넣기 (키 = TimeDateStamp, SizeOfImage) | 주소 넘김 (`RtlRbInsertNodeEx`) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0x91` | `[정적]` run35 · 정확 · 사진 exe·ntdll `P1~P1a` → 바뀜 `P1a~P1b` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0xf8` `OriginalBase` | ② | NT 헤더 `ImageBase` 로 덮음 (ntdll 은 앞서 `+0x9f ~ +0xa6` 이 뺀 값을 덮음) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpProcessMappedModule +0x97` | `[정적]` run35 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x108` `BaseNameHashValue` | ② | 0 이면 `LdrpHashUnicodeString(&BaseDllName)` 결과로 채움 | 주소 넘김 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x1d` | `[정적]` run35 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x10c` `LoadReason` | ② | exe 항목: 부모 칸이 0 이라 4 로 시작 (이 함수가 다시 쓰는 줄은 없음) | 직접 | ntdll | `②… → LdrpAllocateModuleEntry` | `[추측]` run22 · 짐작 · 사진 `P1~P1a` (exe) |
| `+0x10c` `LoadReason` | ② | 의존 DLL 새 항목 = 인자 4 | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0x1179` | `[정적]` run23 · 정확 · 사진 k32 `P1a~P1b` · kb·dll1·dll2 `끝까지 0` |
| `+0x110` `ImplicitPathOptions` | ② | 로드 문맥 `+0x10` 이 가리키는 구조체의 `+0x18` | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0x86 ~ +0x8d` | `[정적]` run22 · 정확 · 사진 exe·ntdll·dll1·dll2 `끝까지 0` · k32·kb `P1a~P1b` |
| `+0x114` `ReferenceCount` | ② | 새 항목 = 2 | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0xfb` | `[정적]` run22 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x130` `HotPatchState` | ② | = 1 (`LoadReason` 9 일 때) | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0x118a` | `[정적]` run23 · 정확 · 사진 `끝까지 0` |
| `+0x130` `HotPatchState` | ② | 핫패치 둘째 호출 때 원래 3 / 패치 2 (오프셋 미상) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry` | `[추측]` run35 · 짐작 · 사진 `끝까지 0` |

### 3.6 `_LDR_DDAG_NODE` — 그래프 노드 (모듈마다 하나) (유저, `0x50`, ntdll PDB)

- 칸 10 · 아는 칸 6 (정확 6 · 짐작 0) · 모름 4 · 줄 18 (그중 flow-writer 더함 3)
- 사진 (run44): 노드 `State` exe P1a 9 → P1b 7 → P2 9, dll1·dll2 P1b 7 → P2 9, ntdll·k32·kb 9. `LoadCount` exe·ntdll·k32·kb `0xffffffff`, dll1·dll2 1.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `Modules` | ② | 이 항목 하나든 목록 | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x18` `LoadCount` | ② | 새 노드 처음 값 = 1 (오프셋 미상) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x18` `LoadCount` | ② | 간선을 안 쓰는 경우 (부모 = 자식 · 자식 `State` 9 …) 자식 것을 1 되돌림 | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0x1294` | `[정적]` run23 · 정확 · 사진 exe·ntdll `P1~P1a` · k32·kb·dll1·dll2 `P1a~P1b` |
| `+0x18` `LoadCount` | ② | ntdll: -1 (문서는 "`LoadCount` = -1" 만 적음 — PDB 에서 이 이름은 노드 칸) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x78 ~ +0x93` | `[추측]` run35 · 짐작 · 사진 `P1~P1a` (ntdll) |
| `+0x20` `LowestLink` | ② | = min (고리 묶기 번호, 오프셋 미상) | 직접 | ntdll | `②… → LdrpPrepareModuleForExecution → LdrpCondenseGraph → LdrpCondenseGraphRecurse` | `[정적]` run24 · 정확 · 사진 exe·k32·kb·dll1·dll2 `P1a~P1b` · ntdll `끝까지 0` |
| `+0x28` `Dependencies` | ② | 간선 칸을 부모 노드 목록 꼬리에 끼움 | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0xf69` | `[정적]` run23 · 정확 · 사진 exe·k32·dll1 `P1a~P1b` · ntdll·kb·dll2 `끝까지 0` |
| `+0x30` `IncomingDependencies` | ② | 간선 칸 `+0x10` 을 자식 노드 목록 꼬리에 | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0xf8b` | `[정적]` run23 · 정확 · 사진 exe·ntdll·k32 `끝까지 0` · kb·dll1·dll2 `P1a~P1b` |
| `+0x38` `State` | ② | ntdll 노드 = 9 (`ReadyToRun`) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x150` | `[정적]` run35 · 정확 · 사진 `P1~P1a` (ntdll) |
| `+0x38` `State` | ② | = 2 (`Mapped`) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpProcessMappedModule +0x1c2` | `[정적]` run24 · run35 · 정확 · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0x38` `State` | ② | exe 노드 = 9 (`+0x1508`) → 다시 2 (`+0x1a60`) | 직접 | ntdll | `②… +0x1508 · +0x1a60` | `[정적]` run19 · 정확 · 사진 `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` (exe) |
| `+0x38` `State` | ② | = 4 (`Snapping`, `+0x4ac`) / IAT 없으면 = 5 (`Snapped`, `+0x5ae`) | 직접 | ntdll | `②… → LdrpMapAndSnapDependency +0x4ac · +0x5ae` | `[정적]` run22 · 정확 · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0x38` `State` | ② | 부모 노드 = 3 (`WaitingForDependencies`) | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0x162d` | `[정적]` run23 · 정확 · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0x38` `State` | ② | 부모 노드 = 4 (자식이 Mapped 되어 카운터 0 일 때) | 직접 | ntdll | `②M… → LdrpProcessMappedModule → LdrpSignalModuleMapped +0x52` | `[정적]` run24 · 정확 · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0x38` `State` | ② | importer 노드 = 5 | 직접 | ntdll | `②… → {LdrpMapAndSnapDependency · LdrpProcessWork} → LdrpSnapModule +0xb87` | `[정적]` run36 · 정확 · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0x38` `State` | ② | 혼자인 노드 5 → 6 (`Condensed`) | 직접 | ntdll | `②… → LdrpPrepareModuleForExecution → LdrpCondenseGraph → LdrpCondenseGraphRecurse +0x96 ~ +0xa3` | `[정적]` run24 · 정확 · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0x38` `State` | ② | = 7 (`ReadyToInit`), 실패면 `+0x29` 에서 6 으로 되돌림 | 직접 | ntdll | `②… → LdrpPrepareModuleForExecution → LdrpNotifyLoadOfGraph +0x19` | `[정적]` run27 (flow.md 2.7, 줄만 봄) · 정확 · **flow-writer 더함** · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0x38` `State` | ② | = 8 (`Initializing`, `+0x1f` 무조건) → 끝에 9 (`+0x2ba`, 실패면 -4) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse → LdrpInitializeNode +0x1f · +0x2ba` | `[정적]` run27 (flow.md 2.7) · 정확 · **flow-writer 더함** · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |
| `+0x38` `State` | ② | 자식이나 자기 켜기가 실패하면 = -4 (`InitError`) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse +0x6e` | `[정적]` run27 (flow.md 2.7) · 정확 · **flow-writer 더함** · 사진 exe `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` · ntdll `P1~P1a` · k32·kb `P1a~P1b` · dll1·dll2 `P1a~P1b` → 바뀜 `P1b~P2` |

### 3.7 `_KUSER_SHARED_DATA` — 공유 사용자 데이터 (KUSD) (유저, `0xa80`, ntdll PDB)

- 칸 103 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 103 · 줄 0
- 사진 (run44): 커널 별칭 `fffff78000000000` 으로 8장 모두 읽었다 (유저 `0x7ffe0000` 은 K1 ~ P1 읽기 불가, P1a 에선 두 주소의 0xa80 바이트가 같음). `K1 이전` 62 · 끝까지 0 41, 바뀌는 칸은 시각 칸 8개뿐 (소음) → **초기화 동안 KUSD 에 놓이는 짐은 안 보인다** (값은 부팅 때 정해진 것 `[추측]`).
- ntdll 은 **읽기만** 한다 (`Cookie` +0x330, `UserModeGlobalLogger` 쪽 +0x384 등 — run19 · run20 · run28). 쓰는 쪽 근거 문서 없음 `[공백]`.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

**유저 공간 — ntdll 전역**

### 3.8 ntdll `.data` 전역 (유저, rva `0x1cc000`, 크기 `0x8f58`)

- 구역: 쓰기 가능 (파일엔 0x2000 만, 나머지는 0 으로 시작 — run17)
- 전역 438 · 쓴 자리 아는 것 65 (정확 63 · 짐작 2) · 모름 373 · 줄 100 (그중 flow-writer 더함 2, run45 더함 1: `LdrpMrdataLock`, **run46 더함 26** — 근거 칸 `sast40`). 크기는 전부 `[추측]` (다음 이름까지 거리, 결과서 2번)
- (run46, sast/40 — **Q78a 프로세스 힙 구간으로 닫힘**) 이름 있는 전역 17개가 "쓴 자리 아는 것" 이 됐다: `≤P1a` 11 (`RtlpHpHeapGlobals`·`?RtlpHpEnvFlsContext@@3U_RTLP_FLS_CONTEXT@@A`·`RtlpHeapKey`·`RtlpHpHeapManager`·`RtlpLFHKey`·`RtlpHpLfhPerfFlags`·`RtlpHpHeapVaCallbacksRegistrar`·`RtlpAffinityState`·`RtlpPebHeapListStaticBuffer`·`RtlpProcessHeaps`·`RtlpProcessHeapsLock`) + `끝까지 0` 6 (`RtlpDisableBreakOnFailureCookie`·`RtlpHeapErrorHandlerThreshold`·`NtdllBaseTag`·`LdrpHeapPartitionName`·`RtlpHpEnvHandle`·`RtlpDefaultHeapDebuggingOptions` — 0 을 쓰거나 조건이 안 맞음). 줄 끝에 **Q78a 로 닫힘** 표시.
  - **목록 밖 자리** (이름 없음, 다른 이름의 짐작 크기 안 — 전역 수 438 에 안 셈) 4줄도 더했다: `0x1ce270~0x1ce280` (GC 타이머, `LdrpThunkSignature` 안), `0x1ce430` (IFEO 바탕 키 핸들, `LdrpInitOnceLoadAsDataCrits` 안), `0x1ce6e8`·`0x1ce6f0` (사용자 공간 최대 주소, `RtlpDisableHeapLookaside` 안), `RtlpHpHeapManager` 끝쪽 `0x1d21a0~0x1d21d8` (같은 이름 줄 안에 적음). 앞 이름 칸이 이 자리 때문에 `≤P1a` 로 보였을 수 있다 → 7.5.
  - 조건부로만 쓰는 힙 전역 (`RtlpHpHeapFeatures` 등 8개) 은 결과서가 닫힌 칸으로 안 세서 3장에 안 옮겼다 → 7.5.
- 사진 (run44): K1 ~ P1 은 이 구역 전부 읽기 불가 (그 전엔 이 프로세스 몫 페이지가 없었을 것 `[추측]`) → `≤P1a` 143. 처음 값이 P1a 뒤에 생긴 것: `P1a~P1b` 24 (`Csr*` 8개, `Kernel32ThreadInitThunkFunction`, `LdrpMapAndSnapWork`·`LdrpThreadPool`, `RtlpDllSearchPath`, `gSystemLocale` 등) · `P1b~P2` 6 (`x86_cpu_enable_*`·`g_SymCrypt*`). 전역 크기는 짐작이라 이웃 바이트가 섞일 수 있다 (`LdrpDllDirectory` 줄).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `LdrpTlsList` (`0x1cc938`) | ② | TLS 목록에 새 항목 잇기 | 직접 | ntdll | `②… → {LdrpInitializeTls (exe) · … LdrpHandleTlsData (DLL)} → LdrpAllocateTlsEntry +0x10a` | `[정적]` `_refs` WRITE · run28 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b` |
| `LdrpDebugFlags` (`0x1ccf60`) | ② | 묶음 5 "전역 켜고 끄기" 에 이름이 나옴 (칸 단위 줄은 문서에 없음) | 직접 | ntdll | `②… +0x57a ~ +0x773` | `[추측]` run18 · sast13 · 짐작 · 사진 `≤P1a` |
| `RtlpHpHeapGlobals` (`0x1cdec0`) | ② | 0x80B 를 0 으로 민 뒤: `+0x40` 난수 상태 (씨앗 = TEB `ClientId.UniqueThread` ^ KUSD 틱 값 곱 → `RtlRandom` 두 번 → `&RtlpHpHeapGlobals` XOR, 0 이면 1), `+0x0`·`+0x8` 키 둘 (상태를 한 칸씩 돌려 × `0x2545f4914f6cdd1d`), `+0x10` = `&RtlpHeapFailureInfo`, `+0x28` = 프로세서 기능 비트 (KUSD `ProcessorFeatures` `0x2a`·`0x3c`) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) → RtlHpGlobalsInitialize (호출 +0xd) +0x6c · +0xc1 · +0x101 · +0x108 · +0x118 · +0x13e` | `[정적]` sast40 · run46 · 정확 · 사진 `≤P1a` (키 `89c4…`·`0034…`, `+0x10` `7ffafb82d0e0`, `+0x28` 7) — **Q78a 로 닫힘** |
| `RtlpHpHeapGlobals` (`0x1cdec0`) `+0x40` | ② | 난수 상태를 한 칸씩 돌림 (`CMPXCHG.LOCK`, xorshift64*). 한 줄에 모음 — 쓰는 곳 다섯 (함수 넷): 키 `RtlpHeapKey`·`RtlpLFHKey`, 힙 무작위 몫, `_HEAP.Encoding` 두 번 | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) +0x154` · `… → RtlpInitializeLowFragHeapManager (호출 +0x191) +0x52` · `②… → LdrpInitializeProcessHeap (호출 +0xa4b) → RtlpCreateHeap (호출 +0x191) +0x3df` · `… → RtlpCreateHeap → RtlpCreateHeapEncoding (호출 +0x5ff) +0x59 · +0x96` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` |
| `?RtlpHpEnvFlsContext@@3U_RTLP_FLS_CONTEXT@@A` (`0x1cdf40`) | ② | 약 0x60B 를 0 으로 지우고 FLS 목록 머리가 자기를 가리키게 (`+0x48 ↔ +0x50`) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) ⇒ RtlHpInitializeHeapManager (꼬리 +0x1a0) +0x50 ~ +0xa1` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` — **Q78a 로 닫힘** |
| `0x1ce270` · `0x1ce278` · `0x1ce280` (이름 없음 — `LdrpThunkSignature` 짐작 크기 안. GC 타이머 상태) | ② | `0x1ce278` = GC 간격(ms) × −10000 (상대 100ns, 기본 −10,000,000), 나머지 0. 값 = `RtlpHpQueryGCTimerInterval` 반환 (IFEO `GCInterval`, 없으면 1000) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) +0x7d · +0x85 · +0x96` | `[정적]` sast40 · run46 · 정확 · 사진 `≤P1a` (`ffffffffff676980`) — **목록 밖 자리** (전역 수 438 에 안 셈, 7.5) |
| `0x1ce430` (이름 없음 — `LdrpInitOnceLoadAsDataCrits` `+0x8`. IFEO 바탕 키 핸들 캐시) | ② | `NtOpenKey` 로 연 IFEO 바탕 키 핸들 (구간 시스템 콜 #1). 남이 먼저 넣었으면 내 핸들을 `NtClose` (`+0x9e`) | 직접 (`CMPXCHG.LOCK`, 값은 syscall 출력) | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) → RtlpHpQueryGCTimerInterval (호출 +0x78) → RtlpOpenImageFileOptionsKeyEx (호출 +0x27) → RtlpOpenBaseImageFileOptionsKey (호출 +0xa9) +0x90` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` (`0x10`) — **목록 밖 자리** (7.5) |
| `RtlpDisableBreakOnFailureCookie` (`0x1ce6a0`) | ② | 섞은 프로세스 쿠키 (`NtQueryInformationProcess` 0x24, 구간 #3) → 곧 0 으로 덮음 (`RtlpDisableHeapLookaside` 비트 0 이 꺼짐) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) +0xa2 · +0xd0 · +0xe0` (`RtlSetHeapInformation +0x6b` 도 0 을 쓰지만 안 탐) | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `끝까지 0*` (0 을 쓰는 줄과 맞음) — **Q78a 로 닫힘** |
| `RtlpHeapKey` (`0x1ce6d8`) | ② | 키 (난수 상태 한 칸 돌려 × 상수) — `_HEAP.CommitRoutine` 인코딩에 씀 | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) +0x16d` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` — **Q78a 로 닫힘** |
| `0x1ce6e8` · `0x1ce6f0` (이름 없음 — `RtlpDisableHeapLookaside` `+0x8`·`+0x10`) | ② | `0x1ce6e8` = 사용자 공간 최대 주소 (구간 #9 `NtQuerySystemInformation` 0 의 `+0x28`), `0x1ce6f0` = `0x10000` (상수). `0x1ce6e8` 이 0 일 때만 (프로세스 첫 `RtlpCreateHeap`) | 직접 | ntdll | `②… → LdrpInitializeProcessHeap (호출 +0xa4b) → RtlpCreateHeap (호출 +0x191) +0xe1c · +0xdeb` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` (`7ffffffeffff`·`0x10000`) — **목록 밖 자리** (7.5) |
| `RtlpHeapErrorHandlerThreshold` (`0x1ce880`) | ② | 2 — `NtGlobalFlag & 0x100000` 이고 값이 1 이하일 때만 (test.exe 는 조건 안 맞음) | 직접 (조건) | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) +0xc6` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `끝까지 0*` (조건 안 맞음과 맞음) — **Q78a 로 닫힘** |
| `RtlpHpHeapManager` (`0x1ce900`) | ② | `+0x0` = `&RtlpHpHeapGlobals`, `+0x8` 부터 `0x38d8` 바이트 0, 끝쪽 `0x1d21a0 ~ 0x1d21d0` 0 · `0x1d21d8` = `0x4000` (LFH 켜기 설정 인자 — 이름 없는 자리, `LdrpInitializeProcessHeap +0x25d`·`RtlpHpHeapCreate +0x2b1` 이 읽음) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) ⇒ RtlHpInitializeHeapManager (꼬리 +0x1a0) → RtlHpHeapManagerInitialize (호출 +0x3e) +0x1d · +0x2d · +0x3d · +0x69 · +0x75` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` (`+0x0` `7ffafb82dec0`, `0x1d21d8` `0x4000`) — **Q78a 로 닫힘** (`0x1d21a0~` 은 목록 밖 자리) |
| `RtlpHpHeapManager` `+0x10` (`0x1ce910`, 희소 비트맵 #1 칸 0x48B) | ② | `+0x0`·`+0x20` (−1)·`+0x34` (`0x10`) 등 처음 칸 | 직접 | ntdll | `… ⇒ RtlHpInitializeHeapManager (꼬리 +0x1a0) → RtlHpHeapManagerInitialize (호출 +0x3e) → RtlCSparseBitmapInitialize (호출 +0x49)` | `[정적]` sast40 · run46 · 정확 · 사진 `≤P1a` (`+0x20` −1, `+0x34` `0x10`) |
| `RtlpHpHeapManager` `+0x10` (`0x1ce910`, 희소 비트맵 #1) | ② | `+0x0` = 요약 쪽 번지, `+0x8` = 예약 시작, `+0x10` = 비트 수 `0xFFFFFFE`, `+0x33` = `0x0c`, `+0x38` = 1. 예약 `0x2001000` (32MB + 4KB) · 요약 1쪽 확정 (구간 #6·#7) | 직접 (칸) · syscall (예약·확정 — VAD 는 nt) | ntdll | `… ⇒ RtlHpInitializeHeapManager (꼬리 +0x1a0) → RtlHpHeapManagerStart (호출 +0xd8) → RtlCSparseBitmapStart (호출 +0x2c) → RtlpHpEnvAllocVA (호출 +0xbb · +0x114) → NtAllocateVirtualMemoryEx (호출 +0xdd)` | `[정적]` sast40 · run46 · 정확 · 사진 `≤P1a` (`7ff5ef090000`·`7ff5ed090000`·`0xffffffe`·`0x0c`·1 = 계산과 같음, VAD `7ff5ed090–7ff5ef090`) |
| `RtlpHpHeapManager` `+0x58` (`0x1ce958`, VA 관리자 맥락) | ② | 처음 칸 (`+0x18` = 5 등) | 직접 | ntdll | `… ⇒ RtlHpInitializeHeapManager (꼬리 +0x1a0) → RtlHpHeapManagerInitialize (호출 +0x3e) → RtlpHpVaMgrCtxInitialize (호출 +0x55)` | `[정적]` sast40 · run46 · 정확 · 사진 `≤P1a` |
| `RtlpHpHeapManager` `+0x58` (`0x1ce958`, VA 관리자 맥락) | ② | 안의 희소 비트맵 (`+0x20`): 요약 번지 `7ff5ed070000`, 예약 시작 `7ff4ed070000`, 비트 수 `0x800000000`. 예약 `0x100020000` (4GB + 128KB), 확정 없음 (구간 #8). 할당자 참조 칸 6개 (`+0x860` 쪽) | 직접 (칸) · syscall (예약 — VAD 는 nt) | ntdll | `… → RtlHpHeapManagerStart (호출 +0xd8) → RtlpHpVaMgrCtxStart (호출 +0x51) → {RtlCSparseBitmapStart (호출 +0x59) → RtlpHpEnvAllocVA → NtAllocateVirtualMemoryEx · RtlpHpVaMgrCtxAllocatorReference (호출 +0x86 ×6)}` | `[정적]` sast40 · run46 · 정확 · 사진 `≤P1a` (위 값 일치, VAD `7ff4ed070–7ff5ed08f` 확정 0) |
| `RtlpLFHKey` (`0x1d2220`) | ② | 키 (난수 상태 한 칸 돌려 × 상수) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) → RtlpInitializeLowFragHeapManager (호출 +0x191) +0x73` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` — **Q78a 로 닫힘** |
| `LdrpAdvapi32DllHandle` (`0x1d2310`) | ② | ADVAPI32 모듈 핸들 (레지스트리 값이 있을 때만) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrpCodeAuthzInitialize +0x19e` | `[정적]` run20 · 정확 · 사진 `끝까지 0*` |
| `RtlpSystem32Dirs` (`0x1d2330`) | ② | 16바이트 쓰기 (`MOVDQU`, 묶음 9 시스템 경로 — 내용은 안 봄) | 직접 | ntdll | `②… +0x109e` | `[정적]` `_refs` WRITE · run39 · 정확 · 사진 `≤P1a` |
| `RtlpSystem32Dirs` (`0x1d2330`) | ② | `+0x8` 칸을 다시 씀 (뜻 모름, dll 올리기 뒤) | 직접 | ntdll | `②… → LdrpInitializePerUserWindowsDirectory +0x164` | `[정적]` `_refs` WRITE · run39 · 정확 · 사진 `≤P1a` |
| `RtlpSystem32Dirs` (`0x1d2330`) | — | `+0x10` 칸 `CMPXCHG.LOCK` — test.exe 길엔 관여 안 함 (플래그 `0x2000` 일 때만, run39) | 직접 | ntdll | `… LdrpSearchPath → RtlpLookupSafeCurDirList +0x135` | `[정적]` `_refs` READ_WRITE · run39 · 정확 · 사진 `≤P1a` |
| `RtlpSystemDirs` (`0x1d2360`) | ② | DLL 검색 목록의 시스템 폴더 덩어리 (`System32;Windows\system;Windows;`) | 직접 | ntdll | `②… +0x1161` | `[정적]` `_refs` WRITE · run39 · 정확 · 사진 `≤P1a` |
| `RtlpSystemDirs` (`0x1d2360`) | ② | 다시 씀 (뜻 모름, dll 올리기 뒤) | 직접 | ntdll | `②… → LdrpInitializePerUserWindowsDirectory +0x157` | `[정적]` `_refs` WRITE · run39 · 정확 · 사진 `≤P1a` |
| `LdrpDllDirectory` (`0x1d2370`) | — | DLL 폴더 API 가 씀 (이름으로 본 범위 밖 `[추측]`, 지도 경로 없음) | 직접 | ntdll | `LdrSetDllDirectory +0xcf` | `[정적]` `_refs` WRITE · 정확 · 사진 앞 16바이트 `끝까지 0*` (짐작 크기 24B 의 뒤 8B = rva `0x1d2380` 만 `P1a~P1b` 에 1 — 이름 없는 이웃일 것 `[추측]`) |
| `LdrInitState` (`0x1d23c8`) | ② | 처음 값 (`EBX`) → 네이티브 길 끝에 3 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x101 · +0x205` | `[정적]` `_refs` WRITE · run17 · run19 · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` |
| `LdrInitState` (`0x1d23c8`) | ② | = 1 (병렬 켠 직후 `+0x1a4c`) · = 2 (실행 준비 뒤 `+0x1b1e`) · = 3 (WOW64 길만 `+0x17dd`) | 직접 | ntdll | `②… +0x1a4c · +0x1b1e · +0x17dd` | `[정적]` `_refs` WRITE · run19 · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` |
| `SecurityCookieInitialized` (`0x1d23ce`) | ② | = 1 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize → InitSecurityCookie +0x42` | `[정적]` `_refs` WRITE · run17 · 정확 · 사진 `≤P1a` |
| `SpecialMachineFramesInitialized` (`0x1d23d0`) | ② | = 1 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize → InitSpecialMachineFrames +0x57` | `[정적]` `_refs` WRITE · run17 · 정확 · 사진 `≤P1a` |
| `LdrpModuleBaseAddressIndex` (`0x1d2420`) | ② | 트리에 항목 넣기 (키 = `DllBase`) | 주소 넘김 (`RtlRbInsertNodeEx`) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0xe7` | `[정적]` run35 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b` |
| `LdrpMappingInfoIndex` (`0x1d2430`) | ② | 트리에 항목 넣기 (키 = TimeDateStamp, SizeOfImage) | 주소 넘김 (`RtlRbInsertNodeEx`) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0x91` | `[정적]` run35 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b` |
| `LdrpTlsLock` (`0x1d2460`) | ② | TLS 락 잡고 풀기 (`CMPXCHG.LOCK` 6줄, `AND.LOCK` 1줄) | 직접 | ntdll | `②… → {LdrpInitializeGraphRecurse → LdrpInitializeNode (DLL) · 직접 (exe)} → LdrpCallTlsInitializers +0x6d · +0xae · +0x25a · +0x2c7 · +0x31e · +0x336 · +0x3bf` | `[정적]` `_refs` · run28 · 정확 · 사진 `끝까지 0*` |
| `LdrpTlsLock` (`0x1d2460`) | ? | `= 0x11` | 직접 | ntdll | `LdrpUnlockTlsDelayedReclaimTable +0x85 (지도 경로 모름)` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |
| `LdrpRetryingModuleIndex` (`0x1d25b0`) | ② | 16바이트 쓰기 (`MOVDQU`, 뜻 모름) | 직접 | ntdll | `②… → LdrpDrainWorkQueue +0x13b` | `[정적]` `_refs` WRITE · run24 · 정확 · 사진 `끝까지 0*` |
| `LdrpDetourExist` (`0x1d2610`) | ② | = 1 (조건은 이름으로 본 "detour 를 찾으면" `[추측]`) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpDetectDetour +0x7a · +0xac` | `[정적]` `_refs` WRITE · run21 · 정확 · 사진 `끝까지 0*` |
| `LdrpDetourExist` (`0x1d2610`) | ② | `R15B` 값 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x1d7` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |
| `LdrpDetourExist` (`0x1d2610`) | ? | = 1 | 직접 | ntdll | `LdrpCaptureCriticalThunks +0xa1 (지도 경로 모름)` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |
| `LdrpMainThreadToken` (`0x1d2618`) | ② | 메인 스레드 토큰 (`RCX`) | 직접 | ntdll | `{②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal · ②… (+0x1a8e)} → LdrpThreadTokenSetMainThreadToken +0x29` | `[정적]` `_refs` WRITE · run21 · 정확 · 사진 `끝까지 0*` |
| `LdrpMainThreadToken` (`0x1d2618`) | ② | = 0 | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpThreadTokenUnsetMainThreadToken +0x1f` | `[정적]` `_refs` WRITE · run21 · 정확 · 사진 `끝까지 0*` |
| `LdrpLoadCompleteEvent` (`0x1d2620`) | ② | 이벤트 (자동 리셋) 만들기 — 칸 주소를 넘김 (`+0x12` `LEA`) | 주소 넘김 | ntdll | `②… → LdrpInitParallelLoadingSupport → LdrpCreateLoaderEvents +0x12` | `[정적]` run19 · run24 · `_refs` DATA · 정확 · 사진 `≤P1a` |
| `LdrpRetryQueue` (`0x1d2630`) | ② | 빈 목록 머리 | 직접 | ntdll | `②… → LdrpInitParallelLoadingSupport +0x2c · +0x33` | `[정적]` `_refs` WRITE · 정확 · 사진 `≤P1a` |
| `LdrpRetryQueue` (`0x1d2630`) | ② | 비우기 (재시도 일감을 통째로 `LdrpWorkQueue` 로 옮긴 뒤) | 직접 | ntdll | `②… → LdrpDrainWorkQueue +0x12d · +0x134` | `[정적]` `_refs` WRITE · run24 · 정확 · 사진 `≤P1a` |
| `LdrpRetryQueue` (`0x1d2630`) | ② | 꼬리에 넣기 (재시도일 때 `[추측: 이름]`) | 직접 | ntdll | `… LdrpMapDllNtFileName → LdrpCheckForRetryLoading +0x182` | `[정적]` `_refs` WRITE · run26 · 정확 · 사진 `≤P1a` |
| `LdrpMapAndSnapWork` (`0x1d2640`) | ② | 일감 개체 (콜백 `LdrpWorkCallback`). 스레드 수 > 1 이고 `LdrpDetourExist == 0` 일 때만 | 주소 넘김 (`TpAllocWork`) | ntdll | `②… → LdrpEnableParallelLoading +0xe2` | `[정적]` run24 (flow.md 2.6) · 정확 · **flow-writer 더함** · 사진 `P1a~P1b` |
| `LdrpMapAndSnapWork` (`0x1d2640`) | ② | 지움 (flow.md 2.6 "지우는 곳", 조건은 안 봄) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpDetectDetour +0xd5` | `[정적]` `_refs` WRITE · run24 · 정확 · 사진 `P1a~P1b` |
| `LdrpMapAndSnapWork` (`0x1d2640`) | — | 지움 (`AND 0`, 복제 길 `[추측: 이름]`) | 직접 | ntdll | `LdrpCompleteProcessCloning +0x78` | `[정적]` `_refs` READ_WRITE · run24 · 정확 · 사진 `P1a~P1b` |
| `LdrpWorkCompleteEvent` (`0x1d2660`) | ② | 이벤트 (자동 리셋) 만들기 — 칸 주소를 넘김 (`+0x35` `LEA`) | 주소 넘김 | ntdll | `②… → LdrpInitParallelLoadingSupport → LdrpCreateLoaderEvents +0x35` | `[정적]` run19 · run24 · `_refs` DATA · 정확 · 사진 `≤P1a` |
| `LdrpWorkInProgress` (`0x1d2668`) | ② | 책임자 끝낼 때 = 1 (`+0xa3`·`+0x1af`), 일감 꺼낼 때 +1 (`+0x155`) | 직접 | ntdll | `②… → LdrpDrainWorkQueue +0xa3 · +0x155 · +0x1af` | `[정적]` `_refs` · run24 · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` |
| `LdrpWorkInProgress` (`0x1d2668`) | ② | 일감 끝에 −1 (인자 "센 적 없음" 0 일 때) | 직접 | ntdll | `②… → LdrpDrainWorkQueue → LdrpProcessWork +0x185` | `[정적]` `_refs` WRITE · run24 · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` |
| `LdrpWorkInProgress` (`0x1d2668`) | ② | 같은 일 (예외 정리 조각) | 직접 | ntdll | `②… → LdrpDrainWorkQueue → LdrpProcessWork$fin$0 +0x49` | `[정적]` `_refs` WRITE · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` |
| `LdrpWorkInProgress` (`0x1d2668`) | ② | = 0 (책임자 내려놓기) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpDropLastInProgressCount +0x25` | `[정적]` `_refs` READ_WRITE · run24 · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` |
| `LdrpWorkInProgress` (`0x1d2668`) | ② 일꾼 | +1 (일감 꺼낼 때) | 직접 | ntdll | `(ntdll 일꾼 스레드) TppWorkpExecuteCallback +0x207` | `[정적]` `_refs` READ_WRITE · run24 · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` |
| `LdrpWorkInProgress` (`0x1d2668`) | ② 일꾼 | 더하기 (`ADD`). 이 함수는 실행 때 안 불릴 수 있음 (run24) | 직접 | ntdll | `(일꾼) TppWorkpExecuteCallback → LdrpWorkCallback +0x5e` | `[정적]` `_refs` READ_WRITE · run24 · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` |
| `LdrpWorkQueueLock` (`0x1d2680`) | ② | 잠금 (임계 구역) 만들기. 잡고 푸는 곳은 `LdrpQueueWork +0x1e`·`+0x59`, `LdrpDrainWorkQueue`·`LdrpWorkCallback` | 주소 넘김 | ntdll | `②… → LdrpInitParallelLoadingSupport` | `[정적]` run24 (flow.md 2.6) · 정확 · **flow-writer 더함** · 사진 `≤P1a` |
| `LdrpWorkQueueLock` (`0x1d2680`) | — | 칸 넷을 다시 씀 (복제 길 `[추측: 이름]`) | 직접 | ntdll | `LdrpCompleteProcessCloning +0x1f · +0x27 · +0x2e · +0x34` | `[정적]` `_refs` WRITE · 정확 · 사진 `≤P1a` |
| `LdrpWorkQueue` (`0x1d26b0`) | ② | 빈 목록 머리 | 직접 | ntdll | `②… → LdrpInitParallelLoadingSupport +0xe · +0x1c` | `[정적]` `_refs` WRITE · run24 · 정확 · 사진 `≤P1a` |
| `LdrpWorkQueue` (`0x1d26b0`) | ② | 꼬리에 일감 (로드 문맥 `+0x40`) 끼우기 | 직접 | ntdll | `… {LdrpLoadDependentModuleInternal · LdrpMapAndSnapDependency · LdrpSignalModuleMapped} → LdrpQueueWork +0x4d` | `[정적]` `_refs` WRITE · run24 · 정확 · 사진 `≤P1a` |
| `LdrpWorkQueue` (`0x1d26b0`) | ② | 머리에서 빼기 · 재시도 큐 통째로 옮기기 | 직접 | ntdll | `②… → LdrpDrainWorkQueue +0x7c · +0x115 · +0x126` | `[정적]` `_refs` WRITE · run24 · 정확 · 사진 `≤P1a` |
| `LdrpWorkQueue` (`0x1d26b0`) | ② 일꾼 | 머리에서 빼기 | 직접 | ntdll | `(일꾼) TppWorkpExecuteCallback +0x1f7` | `[정적]` `_refs` WRITE · run24 · 정확 · 사진 `≤P1a` |
| `LdrpWorkQueue` (`0x1d26b0`) | ② 일꾼 | 머리에서 빼기 | 직접 | ntdll | `(일꾼) TppWorkpExecuteCallback → LdrpWorkCallback +0x4e` | `[정적]` `_refs` WRITE · run24 · 정확 · 사진 `≤P1a` |
| `LdrpIsHotPatchingEnabled` (`0x1d26f0`) | ② | `PEB.ProcessParameters.Flags` `0x2000000` 비트와 `NtManageHotPatch` 결과로 정함 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → LdrpInitializeHotPatching +0x55` | `[정적]` `_refs` WRITE · run17 · 정확 · 사진 `끝까지 0*` |
| `LdrpIsHotPatchingEnabled` (`0x1d26f0`) | ② | = 0 (`LdrpImageExpansionMitigation == 2` 일 때만) | 직접 | ntdll | `②… +0x587` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `끝까지 0*` |
| `FastPebLock` (`0x1d2700`) | ② | 임계 구역 만들기 (그 뒤 `+0x9e1` 이 PEB `+0x38` 에 연결) | 주소 넘김 (`RtlInitializeCriticalSectionEx`) | ntdll | `②… → RtlInitializeCriticalSectionEx (호출 +0x9cd)` | `[정적]` run18 · 정확 · 사진 `≤P1a` |
| `Kernel32ThreadInitThunkFunction` (`0x1d2760`) | ② | kernel32 에서 이름 `BaseThreadInitThunk` 로 찾은 주소. 직접 쓰는 줄은 없음 | 주소 넘김 (칸 주소 `+0xe4`) | ntdll | `②… → LdrpInitializeKernel32Functions → LdrGetProcedureAddressForCaller (호출 ret +0xff)` | `[정적]` run19 · run20 · `_refs` DATA · 정확 · 사진 `P1a~P1b` |
| `LdrpForkInProgress` (`0x1d2768`) | — | 켜고 끔 (프로세스 복제 길, sast12) | 직접 | ntdll | `RtlCloneUserProcess +0x169 · +0x218 · +0x293` | `[정적]` `_refs` WRITE · sast12 · 정확 · 사진 `끝까지 0*` |
| `LdrpForkInProgress` (`0x1d2768`) | — | = 0 (복제 길) | 직접 | ntdll | `RtlCompleteProcessCloning +0x58 · +0xd4` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |
| `LdrpForkInProgress` (`0x1d2768`) | — | = 1 (복제 길) | 직접 | ntdll | `RtlPrepareForProcessCloning +0x109` | `[정적]` `_refs` WRITE · sast12 · 정확 · 사진 `끝까지 0*` |
| `LdrpKnownDllPath` (`0x1d2770`) | ② | `\KnownDlls` 링크에서 읽은 경로 (`UNICODE_STRING` 칸 셋) | 직접 | ntdll | `②… +0xe64 · +0xe6b · +0xe72` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `≤P1a` |
| `LdrpLargePageDllKeyHandle` (`0x1d2780`) | ② | `R12` 값 (IFEO 쪽, 뜻 모름) | 직접 | ntdll | `②… → LdrpInitializeExecutionOptions +0x3d4` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |
| `LdrpLargePageDllKeyHandle` (`0x1d2780`) | ② | `R15` 값 (뜻 모름) | 직접 | ntdll | `②… +0x179e` | `[정적]` `_refs` WRITE · 정확 · 사진 `끝까지 0*` |
| `LdrpImageEntry` (`0x1d2828`) | ② | exe 모듈 항목 주소 (`refs` 46줄 중 쓰기는 여기 하나) | 직접 | ntdll | `②… +0x1374` | `[정적]` `_refs` WRITE · run19 · 정확 · 사진 `≤P1a` |
| `LdrpHeap` (`0x1d2860`) | ② | `R12` 값 = `PEB.ProcessHeap` (프로세스 힙과 **같은 힙**. ~~묶음 7 "로더 힙" — 이름으로 본 뜻 `[추측]`~~ → run46: 따로 만든 로더 힙이 아님, run18 "LdrpHeap = PEB 힙" + 사진 같은 값) | 직접 | ntdll | `②… +0xb12` | `[정적]` `_refs` WRITE · run18 · sast40 · 정확 · 사진 `≤P1a` (`25996f30000` = PEB `ProcessHeap`) |
| `UseWOW64` (`0x1d2868`) | ② | `TEB.WowTebOffset > 0` (x64 test.exe 면 0 `[추측]`) | 직접 | ntdll | `②… +0x5f2` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `끝까지 0*` |
| `PebLdr` (`0x1d2880`) | ② | 이 전역이 곧 `_PEB_LDR_DATA` 한 벌 — 쓰는 줄 20개는 위 `_PEB_LDR_DATA` 표와 같음 (종료 길 2줄 포함) | 직접 | ntdll | `_PEB_LDR_DATA` 표 | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b`·`P1b~P2` |
| `LdrpNtDllDataTableEntry` (`0x1d28d8`) | ② | ntdll 항목을 가리키는 포인터 (성공 길 끝에서만) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x154` | `[정적]` run35 · 정확 · 사진 `≤P1a` |
| `LdrpIsSecureProcess` (`0x1d28e0`) | ② | = 1 (`ProcessParameters.Flags` 맨 윗 비트가 켜졌을 때) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize +0x5a` | `[정적]` `_refs` WRITE · run17 · 정확 · 사진 `끝까지 0*` |
| `NtdllBaseTag` (`0x1d28e4`) | ② | `RtlCreateTagHeap` 반환 = 0 (`NtGlobalFlag & 0x800` 아님 → 태그 힙 안 만듦) | 직접 (값 = 함수 반환) | ntdll | `②… +0xb7d` (← `RtlCreateTagHeap` 호출 `+0xb78`) | `[정적]` sast40 · run46 · 정확 · 사진 `끝까지 0*` (0 을 쓰는 줄과 맞음) — **Q78a 로 닫힘** |
| `LdrpAppHeaders` (`0x1d28f8`) | ② | exe NT 헤더 주소 | 직접 | ntdll | `②… +0x36b` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `≤P1a` |
| `LdrpNtdllHotPatchContext` (`0x1d2908`) | ② | 지역 칸 주소 (`+0xa7`) → `_LdrpInitialize` 에서 돌아오면 비움 (`+0x5e`) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal +0xa7 · +0x5e` | `[정적]` `_refs` · run17 · 정확 · 사진 `≤P1a` → 바뀜 `P1b~P2` |
| `LdrpHotPatchInitialized` (`0x1d2928`) | ② | 0 → 1 (`CMPXCHG.LOCK`, 처음 온 스레드) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal +0x3b` | `[정적]` `_refs` READ_WRITE · run17 · 정확 · 사진 `≤P1a` |
| `LdrpInitCompleteEvent` (`0x1d2948`) | ② | 이벤트 핸들 — `NtCreateEvent(&LdrpInitCompleteEvent, …)` 가 칸에 받음 | syscall | nt | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → NtCreateEvent (ret +0xea)` | `[정적]` sast12 · run17 · 정확 · 사진 `≤P1a` |
| `LdrpProcessInitContextRecord` (`0x1d2950`) | ② | 첫 스레드 CONTEXT 레코드 (`+0x1b83`) → 0 (`+0x1bec`) | 직접 | ntdll | `②… +0x1b83 · +0x1bec` | `[정적]` `_refs` WRITE · run19 · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` |
| `LdrpHeapPartitionName` (`0x1d2960`) | ② | 매개변수 `HeapPartitionName` (`+0x420`, 16바이트 `UNICODE_STRING`) 복사 — test.exe 는 빈 값이라 0. `Buffer ≠ 0` 이면 이어서 `NtOpenPartition` (안 탐) | 직접 (`MOVUPS`) | ntdll | `②… → LdrpInitializeProcessHeap (호출 +0xa4b) +0x11f` | `[정적]` sast40 · run46 · 정확 · 사진 `끝까지 0*` — **Q78a 로 닫힘** |
| `LdrpSystemDllBase` (`0x1d2970`) | ② | ntdll 시작 주소 (`LdrInitializeThunk` 가 받은 `Rdx`) | 직접 | ntdll | `②… +0x68c` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `≤P1a` · 값 출처 `PspSystemDlls[0]+0x20` (`Ⓑ1`, 7.6.2, run48) |
| `LdrpImageExpansionMitigation` (`0x1d2978`) | ② | IFEO `ImageExpansionMitigation` 값 | 직접 | ntdll | `②… → LdrpInitializeExecutionOptions +0x4d0` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `끝까지 0*` |
| `LdrpProcessInitialized` (`0x1d2988`) | ② | 0 → 1 (`+0x3a CMPXCHG.LOCK`, 첫 스레드 판정). `+0x290` 에도 `CMPXCHG.LOCK` 한 줄 (뜻은 안 봄) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x3a · +0x290` | `[정적]` `_refs` READ_WRITE · run17 · 정확 · 사진 `≤P1a` → 바뀜 `P1b~P2` |
| `LdrpSystem32` (`0x1d2990`) | ② | 시스템 폴더 문자열 칸 | 직접 | ntdll | `②… +0x10a6 · +0x10b2` | `[정적]` `_refs` WRITE · run39 · 정확 · 사진 `≤P1a` |
| `LdrpCurrentDllInitializer` (`0x1d3048`) | ② | 지금 켜는 모듈 항목 (`+0x220` 에서 되돌림) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse → LdrpInitializeNode +0xbd · +0x220` | `[정적]` run27 · 정확 · 사진 `끝까지 0*` |
| `RtlpDllSearchPath` (`0x1d3050`) | ② | 프로세스 힙에 캐시 블록을 잡고 가리킴 (처음 DLL 검색 때, 쓰는 줄 오프셋은 문서에 없음) | 직접 | ntdll | `… LdrpSearchPath → LdrpComputeLazyDllPath → … RtlpGetCachedPath → RtlpComputePath` | `[추측]` run39 · 짐작 · 사진 `P1a~P1b` |
| `LdrpHashTable` (`0x1d30a0`) | ② | 해시 표 비우기 (묶음 3) | 직접 | ntdll | `②… +0x3c5 · +0x3c9` | `[정적]` `_refs` WRITE · run18 · 정확 · 사진 `≤P1a` → 바뀜 `P1a~P1b` |
| `LdrpSaferIsDllAllowedRoutine` (`0x1d32a0`) | ② | 함수 주소 (레지스트리 값이 있을 때만) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrpCodeAuthzInitialize +0x1a8` | `[정적]` run20 · 정확 · 사진 `끝까지 0*` |
| `RtlpHpLfhPerfFlags` (`0x1d3520`) | ② | LFH 성능 플래그: 제품 종류 1 + 메모리 정책값 > 10 → `0x240f`, 세그먼트 매개 0 이라 `0x2403` (정책 ≤ 10 이면 `0x2040`, 서버 `0x280f`, WOW64 면 `& ~4`) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) → RtlpHpInitializePerfPolicies (호출 +0x70) +0x57 · +0x80 · +0x94` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` (`0x2403`, 계산과 같음) — **Q78a 로 닫힘** |
| `RtlpHpHeapVaCallbacksRegistrar` (`0x1d3540`) | ② | 빈 등록부 머리 (`+0x4` = `0x800`, `+0x8` 목록 머리, 나머지 0) | 직접 | ntdll | `… ⇒ RtlHpInitializeHeapManager (꼬리 +0x1a0) → RtlHpHeapManagerInitialize (호출 +0x3e) → RtlpHpCustomVaCallbacksRegistrarInitialize (호출 +0x5a) +0x7 · +0x11 · +0x1f · +0x30 · +0x4b` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` (`0x800`) — **Q78a 로 닫힘** |
| `RtlpAffinityState` (`0x1d37a0`) | ② | `+0x0` = 프로세서 수를 2 의 거듭제곱으로 올림 (최대 `0x40`, 시스템 콜 실패면 1), `+0x4` = 0. 값 출처 = 구간 #4 `NtQuerySystemInformation` 0 | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) → RtlpInitializeLowFragHeapManager (호출 +0x191) +0xbf · +0xcc` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` (2) — **Q78a 로 닫힘** |
| `RtlpHpEnvHandle` (`0x1d39b0`) | ② | 16바이트 0 | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) ⇒ RtlHpInitializeHeapManager (꼬리 +0x1a0) +0x50 ~ +0xa1` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `끝까지 0*` (0 을 쓰는 줄과 맞음) — **Q78a 로 닫힘** |
| `RtlpPebHeapListStaticBuffer` (`0x1d3ec0`) | ② | `[0]` = 프로세스 힙 주소 (나머지 0). 배열 주소는 `RtlInitializeHeapManager +0xee`·`+0xf5` 가 PEB `ProcessHeaps` 에 넣음 (3.1) | 직접 | ntdll | `②… → LdrpInitializeProcessHeap (호출 +0xa4b) → RtlpCreateHeap (호출 +0x191) → RtlpProcessHeapsInsert (호출 +0x96f) +0xca` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` (`[0]` = `25996f30000`) — **Q78a 로 닫힘** |
| `RtlpProcessHeaps` (`0x1d3f40`) | ② | 머리·꼬리 16B — 자기 자신을 가리키는 빈 목록 | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) +0x116 · +0x123` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` — **Q78a 로 닫힘** |
| `RtlpProcessHeaps` (`0x1d3f40`) | ② | 힙 노드 하나 (`힙+0x860`, `0x30`B — 첫 힙이라 **새 힙 자신**에서 받음) 를 잇기 | 직접 | ntdll | `②… → LdrpInitializeProcessHeap (호출 +0xa4b) → RtlpCreateHeap (호출 +0x191) → RtlpProcessHeapsInsert (호출 +0x96f) +0x9a · +0x9d · +0xa1 · +0xa4` | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `≤P1a` (Flink = Blink = `25996f30860`) |
| `RtlpProcessHeapsLock` (`0x1d3f60`) | ② | 임계 구역 만들기 (플래그 `0x10000000`. `DebugInfo` 는 ntdll 정적 배열 `RtlpStaticDebugInfo` 에서) | 주소 넘김 (`RtlInitializeCriticalSectionEx`, 6장) | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) → RtlInitializeCriticalSectionEx (호출 +0x12a)` | `[정적]` sast40 · run46 · 정확 · 사진 `≤P1a` (`DebugInfo` = `7ffafb834020`, ntdll `.data` 안) — **Q78a 로 닫힘** |
| `RtlpDefaultHeapDebuggingOptions` (`0x1d3fa0`) | ② | 0x30B 를 0 으로 (`+0xc` 는 `RtlpDisableHeapLookaside & 0x30` 일 때만 값) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0x9ed) → RtlpInitializeLowFragHeapManager (호출 +0x191) +0xc5 · +0xd6 · +0xdd` (`+0x11a` 조건) | `[정적]` `_refs` · sast40 · run46 · 정확 · 사진 `끝까지 0*` (0 을 쓰는 줄과 맞음) — **Q78a 로 닫힘** |
| `LdrpMrdataLock` (`0x1d4de8`) | ② | 0 (이름으로 `.mrdata` 열고 닫기 락 `[추측]`) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrInitializeMrdata (호출 ret +0x14a) +0x23` | `[정적]` sast39 · run45 · 정확 · 사진 `끝까지 0*` (0 을 쓰는 줄과 맞음) |

### 3.9 ntdll `.mrdata` 전역 (유저, rva `0x1e7000`, 크기 `0x35b8`)

- 구역: 쓰기 가능 — 실행 중엔 ntdll 이 필요할 때만 열고 닫음 (run18)
- 전역 50 · 쓴 자리 아는 것 10 (정확 7 · 짐작 3) · 모름 40 · 줄 8 (그중 run45 더함 2: `LdrInitializeMrdata`·`LdrpInitializeCfgScpHelpers`). 크기는 전부 `[추측]` (다음 이름까지 거리, 결과서 2번)
- 사진 (run44): 앞 3페이지 (`0x1e7000` ~ `0x1e9fff`) 는 K1 ~ P1 읽기 불가, 끝 페이지 (`0x1ea000` ~) 는 K1 부터 읽힌다 — 커널이 K1 전에 쓰는 `LdrSystemDllInitBlock` 과 같은 페이지. `__security_cookie` 는 K1 파일 값 `0x2b992ddfa232` → P1a 새 값 (`P1~P1a`, `InitSecurityCookie` 짐작이 시점으로 맞음).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `LdrpMaximumUserModeAddress` (`0x1e7030`) · `LdrpAllocationGranularity` (`0x1e7038`) | ② | 시스템 정보 조회 (클래스 0, 0x40 바이트) 가 성공하면 그 결과의 유저 주소 상한 (`[RSP+0x48]`)·할당 단위 (`[RSP+0x38]`) | 직접 (값은 syscall 결과) | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrInitializeMrdata (호출 ret +0x14a) +0x52 · +0x46` — 조회 함수는 같은 주소 이름 2개 (`NtQuerySystemInformation`·`RtlGetNativeSystemInformation`) | `[정적]` sast39 · run45 · 정확 · 사진 `≤P1a` (P1a 값 `0x7ffffffeffff` · `0x10000`) |
| `AvrfAppVerifierMode` (`0x1e7044`) | ② (AppVerifier 만) | `R14D` 값. 이 값이 0 이면 `LdrpInitializeProcess +0x1568` 이 AppVerifier 묶음을 건너뜀 | 직접 | ntdll | `AVrfInitializeVerifier +0x414 (부르는 자리 확인은 Q78)` | `[정적]` `_refs` WRITE · run19 · 정확 · 사진 `끝까지 0*` |
| `g_pfnSE_InstallAfterInit` (`0x1e70c0`) | ② (shim 만 `[추측]`) | shim 엔진 함수 값 (읽는 쪽 `+0x1cb0 ~ +0x1cd1` 이 KUSD 쿠키로 풂) | 직접 | ntdll | `LdrpGetShimEngineInterface +0x4d9 (지도 경로 모름)` | `[정적]` `_refs` WRITE · run19 · 정확 · 사진 `끝까지 0*` |
| `g_LdrpWow64LdrpInitialize` (`0x1e70c8`) | ② (WOW64 만) | 칸 주소를 넘김 (파일 처음 값 0). 함수 밖 데이터 참조 1줄도 있음 | 주소 넘김 | ntdll | `②… → LdrpLoadWow64 +0xe8` | `[추측]` run17 · `_refs` DATA · 짐작 · 사진 `끝까지 0*` |
| `LdrpKnownDllDirectoryHandle` (`0x1e7120`) | ② | `\KnownDlls` 디렉터리 핸들 칸 — 칸 주소를 실음 (`LEA`). 바로 뒤 `+0xdb8` 이 `NtOpenDirectoryObject` (출력 칸일 것 `[추측]`) | 주소 넘김 (→ syscall `[추측]`) | ntdll | `②… +0xdb1` | `[추측]` `_refs` DATA · run18 · 짐작 · 사진 `≤P1a` |
| `LdrpGuardDispatchIcallNoESFptr` (`0x1e7428`) · `LdrpGuardCheckIcallNoESFptr` (`0x1ea440`) | ② (CFG 강제일 때만) | `LdrpDispatchUserCallTarget` (rva `0x124580`) · `LdrpValidateUserCallTarget` (rva `0x1244e0`) 주소. CFG 강제 (`LdrControlFlowGuardEnforced`) 이고 ntdll 헤더·LoadConfig 플래그가 맞을 때만, `LdrProtectMrdata(0)` 으로 열고 `(1)` 로 닫음 | 직접 | ntdll | `②… → LdrpInitializeCfgScpHelpers (호출 ret +0x180b) +0x79 · +0x6b` | `[정적]` sast39 · run45 · 정확 · 사진 `≤P1a`: Dispatch / `K1 이전`: Check. **사진 값은 이 줄의 함수가 아님**: Check = ntdll+`0xfac60` (8장 모두, 아무것도 안 하는 3바이트 함수 — 같은 주소 이름 26개, 후보 `_guard_check_icall_nop` 등), Dispatch = ntdll+`0x166480` (`_guard_dispatch_icall`, P1a ~ P2) → test.exe 에선 이 갈래를 안 탄 것 `[추측]` (7.5) |
| `LdrSystemDllInitBlock` (`0x1ea450`) | ① | 커널이 새 프로세스 안 ntdll 의 이 블록 (0x128) 에 직접 씀: `+0x98` 난수, `+0xa0` 24B 완화 옵션, `+0xb8` CFG 비트맵 주소, `+0xf0 ~` SCP 표 등. 블록 타입은 두 PDB 에 다 없음 (run43) | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspPrepareSystemDllInitBlock (호출 +0x1e6)` | `[정적]` run32 · run43 · 정확 · 사진 `K1 이전` |
| `__security_cookie` (`0x1ea5a8`) | ② | 쿠키 값 — 이름으로 본 것 (문서는 호출만 적음) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize → InitSecurityCookie (호출 +0x41)` | `[추측]` run17 · sast12 · 짐작 · 사진 `K1 이전` → 바뀜 `P1~P1a` |

### 3.10 ntdll `.00cfg` 칸 (유저, rva `0x1e6000`, 크기 `0x28`)

- 구역: PE 머리엔 읽기 전용. 로더가 값을 넣는 CFG 함수 포인터 칸 5개 (결과서 2번)
- 전역 5 · 쓴 자리 아는 것 1 (정확 0 · 짐작 1) · 모름 4 · 줄 1. 크기는 전부 `[추측]` (다음 이름까지 거리, 결과서 2번)
- 사진 (run44): 8장 모두 읽기 불가 (`????`).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `__guard_dispatch_icall_fptr` (`0x1e6000`) | — | PE 로드 구성 `GuardCFDispatchFunctionPointer` 가 가리키는 칸. 실행 중 쓰는 코드 줄 없음 (`_refs` 는 함수 밖 데이터 참조 3줄: `entry+0x3ac`·`_load_config_used+0x78`·`RtlpScpCfgNtdllExports+0x48`) | 입력 (파일 값) `[추측]` | 모름 | — | `[추측]` run28 · `_refs` DATA · 짐작 · 사진 `읽기 불가` |

**커널 공간**

### 3.11 `_EPROCESS` — 프로세스 객체 (커널, `0x840`, ntoskrnl PDB)

- 칸 263 · 아는 칸 22 (정확 17 · 짐작 5) · 모름 241 · 줄 17
- 사진 (run44): 아는 줄은 모두 단계와 맞다. `ProcessLock` K1·K2 1 → K3 0 (run12 `[정적 + 간접]` "호출자가 잡아 들고 들어옴" 이 맞음), `ActiveProcessLinks`·`SequenceNumber` (`0x134`)·`ActiveThreads`·`ThreadWasActive` `K2~K3`, `ProcessFirstResume` `K3~P0`, `Flags3` 비트 7개는 끝까지 0 (조건 안 맞음). **`OwnerProcessId` 는 `P1a~P1b` 에 `0x19a8` → `0x729`** — 다른 쓰는 곳 `[공백]` → 7.8. 모르는 칸 구간은 7.2.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `Pcb` | ① | 커널 프로세스 구조 초기화 (`KeInitializeProcess` 호출 — 칸마다 줄은 안 봄) | 주소 넘김 | nt | `①… → PspAllocateProcess → KeInitializeProcess (호출 +0x171e)` | `[추측]` sast04 · run10 · 짐작 · 사진 `K1 이전` → 바뀜 `K2~K3`·`K3~P0`·`P1a~P1b` |
| `+0x1c8` `ProcessLock` | ① | 잡기 — 호출자가 잡아서 `PspInsertThread` 로 들고 감 (flow.md `[정적 + 간접]`) | 주소 넘김 | nt | `①… → PspAllocateProcess → PspLockProcessExclusive (+0x26e9)` | `[추측]` run12 · 짐작 · 사진 `K1 이전` → 바뀜 `K2~K3` |
| `+0x1c8` `ProcessLock` | ① | 풀기 | 직접 | nt | `①… → PspInsertThread +0x2a2` | `[정적]` run12 · 정확 · 사진 `K1 이전` → 바뀜 `K2~K3` |
| `+0x1d0` `UniqueProcessId` | ① | `PspCidTable` 에 만든 핸들 값 (`ExCreateHandleEx` ret `+0x2703` 뒤). "이게 프로세스 ID" 는 문서 `[추측]` | 직접 | nt | `①… → PspAllocateProcess +0x27e4` | `[정적]` sast04 · run10 (저장 줄 `.asm` 확인) · 정확 · 사진 `K1 이전` |
| `+0x1d8` `ActiveProcessLinks` | ① | 시스템 프로세스 목록 맨 뒤에 잇기 (목록 잠금 안, 호출 없이) | 직접 | nt | `①… → PspInsertProcess +0xa6 ~ +0x109` | `[정적]` run11 · 정확 · 사진 `K2~K3` |
| `+0x1f8` `CreateTime` | ① | 생성 시각 (`KeQuerySystemTimePrecise`. "생성 시각" 은 문서 `[추측]`) | 주소 넘김 (칸 주소 `+0x27bd`) · 다른 갈래 `+0x27dd` 직접 | nt | `①… → PspAllocateProcess → KeQuerySystemTimePrecise (호출 +0x27c4)` | `[정적]` sast04 (`.asm` 확인) · 정확 · 사진 `K1 이전` |
| `+0x248` `Token` | ① | 토큰 붙이기 (`PspInitializeProcessSecurity` 호출 — "토큰을 붙임" 은 문서 `[추측]`) | 주소 넘김 | nt | `①… → PspAllocateProcess → PspInitializeProcessSecurity (호출 +0x1927)` | `[추측]` sast04 · 짐작 · 사진 `K1 이전` → 바뀜 `K3~P0`·`P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` |
| `+0x2b0` `SectionBaseAddress` | ① | exe 를 붙인 주소 (`+0xb8 MmMapViewOfSectionEx` 결과) | 직접 | nt | `①… → PspAllocateProcess → MmInitializeProcessAddressSpace → MiMapProcessExecutable +0xc5` | `[정적]` run10 · 정확 · 사진 `K1 이전` |
| `+0x2d8` `OwnerProcessId` | ① | 부모 PID (ProcessFlags 비트 9 가 꺼졌으면 `OR 2`) | 직접 | nt | `①… → PspAllocateProcess +0x710` | `[정적]` run33 · run34 · 정확 · 사진 `K1 이전` → 바뀜 `P1a~P1b` |
| `+0x2e0` `Peb` | ① | 새 PEB 주소 (출력 칸) | 주소 넘김 (칸 주소 `+0x22f2`) | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 · 사진 `K1 이전` |
| `+0x300` `ObjectTable` | ① | 핸들 표 준비 (`ObInitProcess` 호출 — "핸들 표 준비" 는 문서 `[추측]`) | 주소 넘김 | nt | `①… → PspAllocateProcess → ObInitProcess (호출 +0x1991)` | `[추측]` sast04 · run10 · 짐작 · 사진 `K1 이전` |
| `+0x370` `ThreadListHead` | ① | 프로세스 스레드 목록 꼬리에 새 스레드 달기 (`+0x5e0` 푸시락 안, 호출 없음). 문서는 목록 머리 오프셋을 안 적음 | 직접 | nt | `①… → PspInsertThread +0x6c0 ~ +0x266` | `[추측]` run12 · 짐작 · 사진 `K1 이전` → 바뀜 `K2~K3`·`P1a~P1b` |
| `+0x380` `ActiveThreads` | ① | 스레드 수 +1 (오프셋 미상) | 직접 | nt | `①… → PspInsertThread` | `[정적]` run12 · 정확 · 사진 `K2~K3` → 바뀜 `P1a~P1b` |
| `+0x5fc` `Flags3` — 0 `Minimal` · 5 `AuxiliaryProcess` · 7 `IndirectCpuSets` · 12 `SystemProcess` · 23 `VmProcessorHost` · 27 `DisallowUserTerminate` · 30 `MemoryCompressionProcess` | ① | `Flags3` 에 쓰는 비트 0·5·7·12·23·27·30 (문서는 비트 번호만, 이름은 PDB 로 맞춤. 오프셋 미상) | 직접 | nt | `①… → PspAllocateProcess` | `[정적]` run33 · 정확 · 사진 `끝까지 0` |
| `+0x5fc` `ProcessFirstResume` (`Flags3` 비트 15) | ① | 첫 풀어줌 표시 켜기 (켜는 곳은 이 한 줄뿐, 끄는 곳 없음) | 직접 | nt | `(부모, 다음 시스템 콜) NtResumeThread → PsMultiResumeThread +0x4e` | `[정적]` run13 · run33 · 정확 · 사진 `K3~P0` |
| `+0x678` `SequenceNumber` | ① | 전역 순번 +1 을 적음 | 직접 | nt | `①… → PspInsertProcess +0xa6 ~ +0x109` | `[정적]` run11 · 정확 · 사진 `K2~K3` |
| `+0x7dc` `ThreadWasActive` (`Flags4` 비트 0) | ① | 첫 스레드 판정 비트 ("이번에 처음 켜지면"). 켜는 줄 오프셋은 문서에 없음, 비트 이름은 PDB 로 맞춤 | 직접 | nt | `①… → PspInsertThread` | `[추측]` run12 · 짐작 · 사진 `K2~K3` |

### 3.12 `_KPROCESS` — 프로세스 객체 앞부분 (`Pcb`) (커널, `0x1c8`, ntoskrnl PDB)

- 칸 65 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 65 · 줄 0
- 사진 (run44): `K1 이전` 15 · `K2~K3` 2 (`StackCount`·`ProcessListEntry`) · `K3~P0` 2 (`ProcessFlags`·`PpmPolicy`) · 끝까지 0 46.
- 초기화 (구조체 단위): `PspAllocateProcess +0x171e KeInitializeProcess` (sast04 · run10). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.13 `_ETHREAD` — 스레드 객체 (메인 스레드) (커널, `0x798`, ntoskrnl PDB)

- 칸 149 · 아는 칸 5 (정확 5 · 짐작 0) · 모름 144 · 줄 6
- 사진 (run44): K1 은 스레드 생기기 전 메모리라 뺐다. 아는 줄 모두 맞다 (`StartAddress` = `RtlUserThreadStart` `7ffafb70cac0`, `Win32StartAddress` = test+`0x1d40`, `Cid` `0x26ec`, `ThreadLock` K2 1 → K3 0, `ThreadInserted` `K2~K3`).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x4e0` `StartAddress` | ① | `CONTEXT.Rip` 를 옮겨 적음 (= `RtlUserThreadStart`, run16) | 직접 | nt | `①… → PspAllocateThread +0x640 ~ +0x647` | `[정적]` run11 · sast06 · 정확 · 사진 `K1~K2` |
| `+0x508` `Cid` | ① | 스레드 ID 칸 | 주소 넘김 | nt | `①… → PspAllocateThread → PsAssignThreadId (호출 +0x532)` | `[정적]` sast06 · run11 · 정확 · 사진 `K1~K2` |
| `+0x560` `Win32StartAddress` | ① | `CONTEXT.Rcx` 를 옮겨 적음 (= exe 진입점) | 직접 | nt | `①… → PspAllocateThread +0x64e ~ +0x655` | `[정적]` run11 · sast06 · 정확 · 사진 `K1~K2` |
| `+0x590` `ThreadLock` | ① | 잡기 | 직접 | nt | `①… → PspAllocateThread +0x51c` | `[정적]` run11 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3` |
| `+0x590` `ThreadLock` | ① | 풀기 (`XADD`, 이어서 `KeAbPostRelease +0x4b2`) | 직접 | nt | `①… → PspInsertThread +0x499` | `[정적]` run12 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3` |
| `+0x5a0` `ThreadInserted` (`CrossThreadFlags` 비트 1) | ① | 켬 (`0x2`) | 직접 | nt | `①… → PspInsertThread +0x490` | `[정적]` run12 · 정확 · 사진 `K2~K3` |

### 3.14 `_KTHREAD` — 스레드 객체 앞부분 (`Tcb`) (커널, `0x4c0`, ntoskrnl PDB)

- 칸 237 · 아는 칸 10 (정확 9 · 짐작 1) · 모름 227 · 줄 14
- 사진 (run44): 아는 줄 모두 맞다. `Teb` = TEB 주소 `9b2fa67000` (짐작이던 칸 이름이 값으로 맞음), `SuspendCount` K3 1 → P0 0. `SuspendSchedulerApcWait`·`SpecialApcDisable` 은 썼다 되돌려서 끝까지 0. P0 이후 바뀌는 칸 다수는 스케줄러 통계 (소음).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x28` `InitialStack` | ① | 새 커널 스택 | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread +0x2ca` | `[정적]` run15 · 정확 · 사진 `K1~K2` |
| `+0x58` `KernelStack` | ① | 첫 스택 자리 (맨 위에 `KiStartUserThread` 주소) | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread +0x2ce` | `[정적]` run15 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` |
| `+0x74` `ApcQueueable` (`MiscFlags` 비트 14) | ① | 기본으로 켬 (`BTS`) | 직접 | nt | `①… → PspAllocateThread → KeInitThread +0xed` | `[정적]` run13 · 정확 · 사진 `K1~K2` |
| `+0x74` `SuspendSchedulerApcWait` (`MiscFlags` 비트 19) | ① | 멈춤 APC 안에서 기다리는 동안 켬 (`+0xf2`) → 깨면 끔 (`+0xf7`) | 직접 | nt | `①T… (APC 인터럽트) KiApcInterrupt → KiDeliverApc → KiSchedulerApc +0xf2 · +0xf7` | `[정적]` run13 · 정확 · 사진 `끝까지 0` |
| `+0x98` `ApcState` | ① | 안쪽 `KernelApcPending` = 1 (오프셋 미상, 새 스레드 갈래 `[추측]`) | 직접 | nt | `①… → PspInsertThread → KeSuspendThread → KiSuspendThread → KiSignalThreadForApc` | `[정적]` run13 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3`·`P0~P1`·`P1a~P1b` |
| `+0x98` `ApcState` | ② 끝 | 안쪽 `UserApcPending` 을 켤 수 있음 (유저 APC 큐가 비어 있지 않을 때) | 직접 | nt | `LdrInitializeThunk → NtContinue (SYSCALL 0x43) nt!NtContinue ⇒ NtContinueEx → KiContinueEx → KeTestAlertThread (호출 +0xc9)` | `[정적]` run29 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3`·`P0~P1`·`P1a~P1b` |
| `+0xf0` `Teb` | ① | `MmCreateTeb` 결과를 `KeInitThread` 인자로 넘김 (칸 이름 `Teb` 는 문서 `[추측]`) | 직접 | nt | `①… → PspAllocateThread → KeInitThread` | `[추측]` run11 · 짐작 · 사진 `K1~K2` |
| `+0x1e6` `SpecialApcDisable` | ① | −1 (특수 APC 막기 시작) → `+0x2de` 에서 되돌림 | 직접 | nt | `①T… → PspInitializeThunkContext +0x113 · +0x2de` | `[정적]` run16 · 정확 · 사진 `끝까지 0` |
| `+0x284` `SuspendCount` | ① | +1 | 직접 | nt | `①… → PspInsertThread → KeSuspendThread +0x60` | `[정적]` run13 · 정확 · 사진 `K2~K3` → 바뀜 `K3~P0` |
| `+0x284` `SuspendCount` | ① | −1 (0 이 될 때만 아래 `KiResumeThread`) | 직접 | nt | `(부모) NtResumeThread → PsMultiResumeThread → KeResumeThread +0x68` | `[정적]` run13 · 정확 · 사진 `K2~K3` → 바뀜 `K3~P0` |
| `+0x288` `SchedulerApc` | ① | `KeInitializeApc` — NormalRoutine = `KiSchedulerApc`, 커널 모드 | 주소 넘김 | nt | `①… → PspAllocateThread → KeInitThread → KeInitializeApc (호출 +0x152)` | `[정적]` run13 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3`·`P0~P1` |
| `+0x2e0` `SuspendEvent` | ① | `KeInitializeEvent` — 처음 신호 상태 (1) | 주소 넘김 | nt | `①… → PspAllocateThread → KeInitThread → KeInitializeEvent (호출 +0x163)` | `[정적]` run13 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3`·`K3~P0` |
| `+0x2e0` `SuspendEvent` | ① | `SignalState` 1 → 0 (멈춰 두기) | 직접 | nt | `①… → PspInsertThread → KeSuspendThread → KiSuspendThread +0x6c` | `[정적]` run13 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3`·`K3~P0` |
| `+0x2e0` `SuspendEvent` | ① | `SignalState` = 1 (풀어 주기) | 직접 | nt | `(부모) NtResumeThread → PsMultiResumeThread → KeResumeThread → KiResumeThread +0x20` | `[정적]` run13 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3`·`K3~P0` |

### 3.15 `_KTRAP_FRAME` — 트랩 프레임 (메인 스레드, `InitialStack − 0x190`) (커널, `0x190`, ntoskrnl PDB)

- 칸 66 · 아는 칸 6 (정확 4 · 짐작 2) · 모름 60 · 줄 11
- 사진 (run44): K2·K3·P0 = 처음 문맥 (`Rip` `RtlUserThreadStart`, `Rcx` 진입점 `7ff61a371d40`, `Rdx` PEB, `Rsp` `9b2fcfff48`, `SegCs 0x33`, `SegSs 0x2b`, `EFlags 0x200`) — 짐작이던 `SegCs`·`SegSs` 값도 확인. P1 = 바꿔 끼운 값 (`Rcx` = `_CONTEXT` `9b2fcffa40`, `Rdx` = ntdll 베이스, `Rsp` = `_CONTEXT` − 0x28) 인데 `Rip` 는 `LdrInitializeThunk` **+ 1** (멈춤점 `int3` 이 같은 자리에 트랩 프레임을 만듦). P1a 부터는 시스템 콜·`int3` 트랩 프레임이라 짐이 아니다. `② 끝` 줄은 P2 뒤라 사진에 없다.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x38` `Rcx` | ① | 처음 = exe 진입점 | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread → KxContextToKframes +0xe8` | `[정적]` run16 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` |
| `+0x38` `Rcx` | ① | 유저 스택에 맡긴 원래 문맥의 주소 | 직접 | nt | `①T… → PspInitializeThunkContext → PspSetContextThreadInternal → PspGetSetContextSpecialApc → PspGetSetContextInternal → PspSetContext +0xe3` | `[정적]` run16 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` |
| `+0x40` `Rdx` | ① | 처음 = PEB | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread → KxContextToKframes +0xf3` | `[정적]` run16 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` |
| `+0x40` `Rdx` | ① | `PspSystemDlls[0]+0x20` 값 (ntdll 시작 주소로 쓰임, run18) | 직접 | nt | `①T… → PspInitializeThunkContext → PspSetContextThreadInternal → PspGetSetContextSpecialApc → PspGetSetContextInternal → PspSetContext +0xee` | `[정적]` run16 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` · 값 출처 `Ⓑ1` (7.6.2, run48) |
| `+0x168` `Rip` | ① | 처음 = `RtlUserThreadStart` (CONTEXT 에서) | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread → KxContextToKframes +0x6d` | `[정적]` run16 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` · 값 출처 `PsNtdllExports+0x10` (`Ⓑ2`, 7.6.2, run48) |
| `+0x168` `Rip` | ① | `LdrInitializeThunk` 로 바꿔 끼움 (`PsNtdllExports+0x8`) | 직접 | nt | `①T… → PspInitializeThunkContext → PspSetContextThreadInternal → PspGetSetContextSpecialApc → PspGetSetContextInternal → PspSetContext +0x68` | `[정적]` run16 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` · 값 출처 `Ⓑ2` (7.6.2, run48) |
| `+0x168` `Rip` · `+0x180` `Rsp` · `+0x38` `Rcx` · `+0x40` `Rdx` | ② 끝 | 맡긴 문맥으로 다시 씀 — `Rip` 가 `RtlUserThreadStart` 로 바뀌는 순간 (`Rcx` = 진입점, `Rdx` = PEB) | 주소 넘김 (→ `KeContextToKframes → KxContextToKframes`) | nt | `LdrInitializeThunk → NtContinue (SYSCALL 0x43) nt!NtContinue ⇒ NtContinueEx → KiContinueEx → KiContinuePreviousModeUser +0x422` | `[정적]` run29 · sast24 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2`: `Rip`·`Rcx`·`Rdx` / `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1b~P2`: `Rsp` |
| `+0x170` `SegCs` | ① | `0x33` — 로컬 CONTEXT 사본에서 손본 뒤 넘김 (옮기는 칸 목록엔 안 나옴) | 주소 넘김 (→ `KxContextToKframes`) | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread +0xb5` | `[추측]` run15 · run16 · 짐작 · 사진 `K1~K2` |
| `+0x180` `Rsp` | ① | 처음 = CONTEXT `Rsp` (`PspSetupUserStack` 이 정한 값) | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread → KxContextToKframes +0x7b` | `[정적]` run16 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1b~P2` |
| `+0x180` `Rsp` | ① | 맡긴 문맥 자리 − 0x28 | 직접 | nt | `①T… → PspInitializeThunkContext → PspSetContextThreadInternal → PspGetSetContextSpecialApc → PspGetSetContextInternal → PspSetContext +0x76` | `[정적]` run16 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1b~P2` |
| `+0x188` `SegSs` | ① | `0x2b` — 같은 사본 | 주소 넘김 (→ `KxContextToKframes`) | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread +0xb5` | `[추측]` run15 · run16 · 짐작 · 사진 `K1~K2` |

### 3.16 `_HANDLE_TABLE` — 핸들 표 (커널, `0x80`, ntoskrnl PDB)

- 칸 17 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 17 · 줄 0
- 사진 (run44): `K1 이전` 10 · 끝까지 0 7. **`UniqueProcessId` 만 K1·K2 `0x19a8` (부모 PID) → K3 `0x1dd0`** (`K2~K3`, 쓴 자리 `[공백]` → 7.8). 핸들 수 4·4·4·4·4·26·44·46 (`!handle`).
- 만드는 곳 (구조체 단위, 이름으로 본 `[추측]`): `PspAllocateProcess +0x1991 ObInitProcess` (run10 · sast04). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.17 `_MMVAD` — 메모리 구역 장부 (긴 꼴) (커널, `0x88`, ntoskrnl PDB)

- 칸 9 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 9 · 줄 0
- 사진 (run44): 노드 칸은 안 읽었다 (`!vad` 목록만). VAD 수 8·9·9·11·11·21·32·32.
- 만드는 곳 (구조체 단위): `MmInitializeProcessAddressSpace +0x161 MiAllocateProcessVads`, `+0x1a8 MiInsertProcessVads` (run10 · sast04). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.18 `_MMVAD_SHORT` — 메모리 구역 장부 (짧은 꼴) (커널, `0x40`, ntoskrnl PDB)

- 칸 14 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 14 · 줄 0
- 사진 (run44): 3.17 과 같음 (칸 안 읽음).
- `_MMVAD` 와 같음 (`_MMVAD` 의 앞부분 격). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.19 `_TOKEN` — 토큰 (커널, `0x5c8`, ntoskrnl PDB)

- 칸 55 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 55 · 줄 0
- 사진 (run44): 8장 모두 같은 토큰 `ffffb689ae6b6060`. `K1 이전` 29 · 끝까지 0 26, 바뀜 없음 (`EPROCESS.Token` 은 아래 4비트만 바뀜).
- 붙이는 곳 (구조체 단위, `[추측]`): `PspAllocateProcess +0x1927 PspInitializeProcessSecurity` (sast04), `PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange` (run15). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.20 `_SECTION` — exe 섹션 객체 (커널, `0x40`, ntoskrnl PDB)

- 칸 9 · 아는 칸 9 (정확 9 · 짐작 0) · 모름 0 · 줄 8 — **(run47, sast/41) 전부 닫힘 (Q78b)**. 근거 칸 `sast41`, 줄 끝 "Q78b 로 닫힘".
- 사진 (run44): `K1 이전` 5 · 끝까지 0 4, 바뀜 없음. 아래 줄과 모두 맞다 (단계 ①, K1 = `PspAllocateThread` 입구 전에 끝나는 일).
- 만드는 곳: ~~`NtCreateUserProcess +0x93c MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap` (run31)~~ → (run47 정정) **사슬 끝 형제 `MiCreateSection +0x125 MiFinishCreateSection`** — 컨트롤 영역·세그먼트 사슬 (`+0xf8 MiCreateImageOrDataSection` 아래) 이 다 끝난 뒤 `+0x143 ObCreateObjectEx` (`MmSectionObjectType`, 몸통 `0x40`, 공용 부품 6장). 핸들은 그 뒤 `MmCreateSpecialImageSection +0x141 ObInsertObjectEx`.
- **놓는 법 — 패킷 사본 복사**: 사슬이 스택 패킷 (0xc8바이트, `MiCreateSection +0x58 memset` 으로 0) 의 `+0x50 ~ +0x8f` 에 값을 모으고, 객체를 받으면 `MiFinishCreateSection +0x15b ~ +0x179` 가 `MOVUPS` 4번으로 객체 `+0x00 ~ +0x3f` 에 통째 옮긴다 (패킷 `+0x50+X` = `_SECTION +X`). 아래 "무엇을" 은 패킷에 값이 들어가는 줄, 경로 끝 "복사" 는 이 4줄.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x28` `u1` | ① | 컨트롤 영역 주소 (패킷 `+0x78`). 원격 파일이면 파일 객체 주소 `\|1` (이미지) / `\|2` (데이터) 로 덮음 — 이 실행 아님 | 직접 (패킷 → 복사) | nt | `①S… → MiFinishCreateSection +0x2a`, 복사 `+0x15b ~ +0x179` (원격 갈래 `+0x266 ~ +0x27b`) | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`…983992b0`, 아래 비트 0 = 원격 아님) · Q78b 로 닫힘 |
| `+0x30` `SizeOfSection` | ① | 세그먼트 `SizeOfSegment` 를 원자적으로 읽은 값 (최대 크기 인자가 있으면 그 값, `MiCreateNewSection +0x12e`) | 직접 (패킷 → 복사) | nt | 새 길 `①SC… +0x21d CMPXCHG · +0x228` / 공유 길 `①S… → MiCreateImageOrDataSection → MiShareExistingControlArea +0x104 · +0xac`, 복사 `①S… → MiFinishCreateSection +0x15b ~ +0x179` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0x8c000`) · Q78b 로 닫힘 |
| `+0x38` `u` | ① | 컨트롤 영역 `u` 를 패킷 `+0x88` 로 복사하며 `FilePointerNull` (bit19) 끔 → 객체에 `UserReference` (bit16) 켬 (패킷 플래그 bit0 이 0 일 때). 조건부: `Based` (bit6, `SEC_BASED`) · `NoChange` (bit14, 패킷 `0x400000`) — 이 실행 아님 | 직접 | nt | `①S… → MiFinishCreateSection +0x31 ~ +0x43` · 복사 · `+0x1b3 ~ +0x1ba` (조건부 `+0x1d3` · `+0x252`) | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0x000100a0` = 컨트롤 영역 `u` `0xa0` + `UserReference`) · Q78b 로 닫힘 |
| `+0x3c` `InitialPageProtection` (0:12) | ① | `0x10` — 호출자가 준 페이지 보호 (`MmCreateSpecialImageSection +0xb4`) | 직접 (패킷 → 복사) | nt | `①S… → MiInitializeCreateSectionPacket +0x68 · +0xd7` (패킷 `+0x8c` 0:12) → 복사 `MiFinishCreateSection +0x15b ~ +0x179` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0x10`) · Q78b 로 닫힘 |
| `+0x3c` `NoValidationNeeded` (bit31) | ① | 1 — 이미지 (`SEC_IMAGE`) 이고 패킷 서명 수준 바이트 (`+0x18`, `MiCreateNewSection +0x318` 이 `SeGetImageRequiredSigningLevel` 결과를 넣음) ≤ 1 이고 패킷 플래그 bit18 꺼짐 | 직접 (패킷 → 복사) | nt | `①S… → MiFinishCreateSection +0x35 ~ +0x3f` (먼저 끔) · `+0x243 ~ +0x247` (켬), 복사 | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (1) · 서명 수준 값은 동적 D10 · Q78b 로 닫힘 |
| `+0x00` `SectionNode` · `+0x20` `EndingVpn` | ① | 0 — `SEC_BASED` (패킷 `0x200000`) 섹션일 때만 씀 (빈 주소 범위 찾기 → 시작·끝 VPN → 전역 AVL 트리 `140e2cd48` 에 잇기). test.exe 는 `SEC_IMAGE` 만이라 안 탐. 0 은 패킷 `memset` 의 복사 | 직접 (조건부, 안 탐) | nt | `①S… → MiFinishCreateSection` 조각 `FUN_140b23aba +0x3b MiFindEmptyAddressRangeDownTree · +0xb8 ~ +0xbf · +0x111 RtlAvlInsertNodeEx` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · D3 · Q78b 로 닫힘 |
| `+0x18` `StartingVpn` | ① | 0 을 씀 (값은 `SEC_BASED` 길에서만) | 직접 | nt | `①S… → MiFinishCreateSection +0x17d` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x3c` `SessionId` (12:19) | ① | 0 — 컨트롤 영역이 `GlobalOnlyPerSession` (`u` bit27) 일 때만 패킷 `+0xa8` 세션 id 를 넣음 (`MiCreatePerSessionProtos` 도 같이). 일반 이미지라 안 탐 | 직접 (조건부, 안 탐) | nt | `①S… → MiFinishCreateSection +0x2b3 ~ +0x2e5` (갈림 `+0x63`) | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · D3 · Q78b 로 닫힘 |

### 3.21 `_CONTROL_AREA` — exe 컨트롤 영역 (커널, `0x80`, ntoskrnl PDB)

- 칸 17 · 아는 칸 16 (정확 14 · 짐작 2) · 아무도 안 씀 1 · 모름 0 · 줄 22 — **(run47, sast/41) 모르던 15칸 닫힘 (Q78b)**. 옛 2줄은 고쳐 씀 (아래 "정정").
- 사진 (run44): `K1 이전` 12 · 끝까지 0 5. `NumberOfPfnReferences` 3 → `0x2d`, `FilePointer` 아래 비트 `P0~P1` — 두 줄 모두 "범위 밖 길" 로 맞다 (섹션 사슬은 이 값을 안 바꿈).
- 만드는 곳: **새 길** `①SN… → MiBuildImageControlArea +0x8d MiAllocatePool` (태그 `MmCi`, `0xc8 + 0x38×(구역 수+1)`, 컨트롤 영역 + 서브섹션). 풀은 **0 으로 채워져 온다** (플래그 `0x40`·`0x100` 에 "초기화 안 함" 비트 `0x2` 가 없음, `[정적]` · 비트 이름 `[추측]`) → 안 쓰는 칸은 그대로 0. **공유 길** (`①S… → MiCreateImageOrDataSection +0x1d5 MiShareExistingControlArea`) 은 카운터 둘만 올리고 다른 칸은 안 건드린다.
  - test.exe 가 어느 길이었는지는 **사진 값으로 못 가른다** (두 길이 같은 숫자 `NumberOfSectionReferences` 1 · `NumberOfUserReferences` 2 를 만든다). 동적 D1 (= plan Q61) — 7.10.
- (run47 정정) 옛 `NumberOfUserReferences` 줄 "`MiCreateImageOrDataSection +0x11d`" 는 **`MiReferenceControlArea +0x11d`** 의 오기 (`.asm` `INC qword ptr [RBX + 0x30]`), 옛 `u` 줄 "자리표시용 컨트롤 영역 (스택 사본) 에 `BeingCreated`" 는 진짜 칸 줄로 바꿈 (자리표시용 사본은 `MiCreateImageOrDataSection` 스택 `RBP-0x69`, 0x80바이트 — 짐이 아니라 임시).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x00` `Segment` | ① | 세그먼트 주소 | 직접 | nt | `①SN… → MiBuildImageControlArea +0x1f8` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |
| `+0x08` `ListHead` · `+0x08` `AweContext` (겹침) | ① | 자기 자신을 가리키는 빈 목록 (처음 값). 뒤에 매핑이 VAD 를 이음 (범위 밖 — 이름 `MiAddViewsForSection` `[추측]`) | 직접 | nt | `①SN… → MiBuildImageControlArea +0x1f4 ~ +0x1ff` | `[정적]` sast41 · run47 · 정확 (처음 값) · 사진 `K1 이전` (항목 1개 = 매핑 뒤) · Q78b 로 닫힘 |
| `+0x18` `NumberOfSectionReferences` | ① | 1 (새 길) | 직접 | nt | `①SN… → MiBuildImageControlArea +0x20b` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (1) · Q78b 로 닫힘 |
| `+0x18` `NumberOfSectionReferences` | ① | +1 (공유 길) | 직접 | nt | `①S… → MiCreateImageOrDataSection → MiReferenceControlArea +0x107` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (1) |
| `+0x20` `NumberOfPfnReferences` | ① | 0 으로 시작 (풀 0, 이 사슬은 안 씀) → 뒤에 페이지 폴트·프로토 PTE 쪽이 올림 | 직접 `[추측]` | nt | 범위 밖 길 — 쓰는 줄 못 찾음 (이름 `[추측]`) | `[추측]` sast41 · run47 · 짐작 · 사진 `K1 이전` (3) → 바뀜 (`0x2d`) · D5 |
| `+0x28` `NumberOfMappedViews` | ① | 0 으로 시작 (풀 0) → exe 매핑이 +1 | 직접 `[추측]` | nt | 범위 밖 길 `①… → PspAllocateProcess → MmInitializeProcessAddressSpace +0x1c9 → MiMapProcessExecutable +0xb3 → MmMapViewOfSectionEx` (이름만, Q78c / Q79) | `[추측]` sast41 · run47 · 짐작 · 사진 `K1 이전` (1) |
| `+0x30` `NumberOfUserReferences` | ① | 1 (새 길, 패킷 플래그 bit0 이 0 일 때) | 직접 | nt | `①SN… → MiBuildImageControlArea +0x214` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (2 — 매핑이 1 더 올림 `[추측]`) · Q78b 로 닫힘 |
| `+0x30` `NumberOfUserReferences` | ① | +1 (`INC`) — 같은 파일 컨트롤 영역이 이미 있을 때 (공유 길). 패킷 플래그 bit0 이 켜졌으면 대신 `+0x1fa BTS` (`WasPurged`) | 직접 | nt | `①S… → MiCreateImageOrDataSection → MiReferenceControlArea +0x11d` (run47 정정: 옛 줄 "`MiCreateImageOrDataSection +0x11d`") | `[정적]` sast26 · run31 · sast41 · run47 · 정확 · 사진 `K1 이전` (2) |
| `+0x38` `u` | ① | `Image` (`0x20`) · `File`+`BeingCreated` (`0x82`) · 선호 노드 (bit20~26) | 직접 | nt | `①SN… → MiBuildImageControlArea +0x9e · +0x222 · +0x22b ~ +0x238` | `[정적]` sast41 · run47 · 정확 (run47 전 짐작) · 사진 `K1 이전` (`0xa0`) · Q78b 로 닫힘 |
| `+0x38` `u` | ① | `BeingCreated` 끔 | 직접 | nt | `①SC… → MiSectionCreated +0xa6` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0xa0`, bit1 꺼짐) |
| `+0x3c` `u1` | ① | `PartitionId` = 0 을 씀 (시스템 파티션) | 직접 | nt | `①SN… +0x31e` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x40` `FilePointer` | ① | 파일 객체 주소 — 자리표시용 컨트롤 영역 `+0x40` 을 복사 (그 값은 `MiCreateImageOrDataSection +0x120` 이 채움). 쓴 값의 아래 4비트 `RefCnt` = 0 | 직접 | nt | `①SC… → MiSectionCreated +0x6f ~ +0x7a` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`…dc5d`) · Q78b 로 닫힘 |
| `+0x40` `FilePointer` 아래 4비트 (`_EX_FAST_REF.RefCnt`) | ① | 빠른 참조 수 | 직접 `[추측]` | nt | 범위 밖 길 — 후보 `MiReferenceControlAreaFile` / `MiDereferenceControlAreaFile` (`MmCreateSpecialImageSection +0x101 / +0x117`, `MiParseComAndCetHeaders +0x9f`) | `[추측]` sast41 · run47 · 짐작 · 사진 `K1 이전` (`0xd`) → 바뀜 `P0~P1` · D5 |
| `+0x48` `ControlAreaLock` | ① | 스핀락 — 잡았다 놓음 → 0 으로 돌아옴 | 주소 넘김 | nt | `①SC… → MiSectionCreated → ExAcquireSpinLockExclusive (호출 +0x31)` · `MiReleaseSpinLockExclusive (호출 +0xc6)` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x4c` `ModifiedWriteCount` | — | **아무도 안 씀** — 0 (풀 0 그대로, 사슬에 쓰는 줄 없음) | — | — | (할당 `①SN… → MiBuildImageControlArea +0x8d MiAllocatePool`) | `[정적]` 부재 · sast41 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x50` `WaitList` | ① | 자리표시용 컨트롤 영역의 `WaitList` 복사 (기다린 스레드가 없으면 0. 기다릴 때 채우는 곳 `MiReferenceControlArea +0x2bf`) | 직접 | nt | `①SC… → MiSectionCreated +0x11b` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` (= 안 기다림) · Q78b 로 닫힘 |
| `+0x58` `u2` | ① | `ImageRelocationStartBit` 처음 `-1` (`+0x218 OR [RSI+0x58],-1`) · `ImageInfoRef` (`+0x60`) = 할당 꼬리 `CA+0x80+0x38×n` (`_MI_IMAGE_ADDITIONAL_INFO` 0x48) | 직접 | nt | `①SN… → MiBuildImageControlArea +0x218 · +0xb4` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`+0x58` `ffffffff`) · Q78b 로 닫힘 |
| `+0x58` `u2` | ① | `u2.e2` 재배치 칸 — `ImageRelocationStartBit` · `ImageRelocationSizeIn64k` (0:16) · `BitMap` (20:2) · `ImageBaseOkToReuse` (bit23) | 직접 | nt | `①SC… → MiRelocateImage (호출 +0x2e6) → MiSelectImageBase +0x66 · +0x1d4 · +0x1d8 · +0x209` (조각 `+0x1b16d6 ~ +0x1b16e2`) | `[정적]` sast41 · run47 · 정확 (줄) · 갈래 `[공백]` D4 · 사진 `K1 이전` (`+0x5c` `0x00c00009` = 크기 9 · bit22·23) |
| `+0x58` `u2` | ① | `ImageActive` (bit22) 켬 | 직접 | nt | `①SC… → MiRelocateImage` 조각 `+0x1b127c BTS [R13+0x5c],0x16` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` |
| `+0x68` `FileObjectLock` | ① | 0 을 씀 | 직접 | nt | `①SN… → MiBuildImageControlArea +0x227` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x70` `LockedPages` | ① | 1 | 직접 | nt | `①SN… → MiBuildImageControlArea +0x207` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (1) · Q78b 로 닫힘 |
| `+0x78` `u3` | ① | `ImageCrossPartitionCharge` — 구역을 훑으며 쪽마다 +1 | 직접 | nt | `①SN… → MiBuildImageControlArea → MiParseImageSectionHeaders (호출 +0x362) +0x258` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0x89`) · Q78b 로 닫힘 |

### 3.22 `_SEGMENT` — exe 세그먼트 (커널, `0x48`, ntoskrnl PDB)

- 칸 11 · 아는 칸 11 (정확 11 · 짐작 0) · 모름 0 · 줄 13 — **(run47, sast/41) 모르던 10칸 닫힘 (Q78b)**.
- 사진 (run44): `K1 이전` 9 · 끝까지 0 2, 바뀜 없음. 아래 줄과 맞다.
- 만드는 곳: `①SN… → MiBuildImageControlArea +0x107 MiAllocatePool` (태그 `MSeg`, `0x98` = 세그먼트 `0x48` + 이미지 정보 `0x40` + `_MI_EXTRA_IMAGE_INFORMATION` `0x10`, 풀 0). 공유 길은 이 칸들을 안 건드린다 (`SizeOfSegment` 를 읽기만).
- 값 출처: `TotalNumberOfPtes`·`SizeOfSegment` = 헤더 `SizeOfImage` 에서 계산, `BasedAddress` = 헤더 `ImageBase` (입력) → `MiSelectImageBase` 가 고른 새 기본 주소 (ASLR), `PrototypePte`·`ControlArea` = 풀 주소 (실행 때 값).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x00` `ControlArea` | ① | 컨트롤 영역 주소 | 직접 | nt | `①SN… → MiBuildImageControlArea +0x138` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |
| `+0x08` `TotalNumberOfPtes` | ① | `SizeOfImage` 를 쪽으로 올린 수 (`0x8c`) | 직접 (값은 파일 헤더 = 입력에서 계산) | nt | `①SN… → MiBuildImageControlArea +0x13b` (EBP 계산 `+0x2c ~ +0x44`) | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |
| `+0x0c` `SegmentFlags` | ① | `ContainsDebug` (bit5) — 디버그 디렉터리가 있으면 | 직접 | nt | `①SN… → MiBuildImageControlArea +0x246 ~ +0x256` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0x270000`) · Q78b 로 닫힘 |
| `+0x0c` `SegmentFlags` | ① | `DefaultProtectionMask` = 7 (바이트 `+0xe` 의 0:5) | 직접 | nt | `①SN… → MiBuildImageControlArea → MiParseImageSectionHeaders (호출 +0x362) +0x3c ~ +0x43` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` |
| `+0x0c` `SegmentFlags` | ① | 조건부 (안 탐): `Binary32` (bit15, 헤더 `Magic` 이 PE32 일 때) · `ForceCollision` (bit14, COM 디렉터리 있을 때) | 직접 (조건부) | nt | `①SN… +0x415` · `①SC… → MiParseComAndCetHeaders +0x3aa` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`Short0` = 0 → 둘 다 꺼짐) |
| `+0x10` `NumberOfCommittedPages` | ① | 조건부 +1 (구역 속성 갈래 `+0x381 ~ +0x38a`). test.exe 는 안 탄 갈래 | 직접 (조건부) | nt | `①SN… → MiBuildImageControlArea → MiParseImageSectionHeaders +0x240` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x18` `SizeOfSegment` | ① | `TotalNumberOfPtes << 12` (`0x8c000`) | 직접 | nt | `①SN… → MiBuildImageControlArea +0x145 ~ +0x149` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |
| `+0x20` `ExtendInfo` · `+0x20` `BasedAddress` (겹침) | ① | 헤더 `ImageBase` (`0x140000000`) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x25e ~ +0x261` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |
| `+0x20` `BasedAddress` | ① | 새 기본 주소로 덮음 (`MiSelectImageBase` 가 고름) | 직접 | nt | `①SC… → MiRelocateImage` 조각 `+0x55` (= `nt+0x1b1269`) | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0x7ff61a370000`) |
| `+0x28` `SegmentLock` | ① | 0 을 씀 | 직접 | nt | `①SN… → MiBuildImageControlArea +0x130` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x30` `u1` | ① | `ImageCommitment` — 쓰기 가능 구역의 쪽마다 +1 (플랫 매핑이면 쪽 수, `MiBuildImageControlArea +0x428` — 안 탐) | 직접 | nt | `①SN… → MiBuildImageControlArea → MiParseImageSectionHeaders +0x249` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (4) · Q78b 로 닫힘 |
| `+0x38` `u2` | ① | `u2.ImageInformation` = 세그먼트 `+0x48` 쪽 이미지 정보 주소 | 직접 | nt | `①SN… → MiBuildImageControlArea +0x13e` | `[정적]` sast26 · run31 · 정확 · 사진 `K1 이전` |
| `+0x40` `PrototypePte` | ① | 프로토 PTE 풀 주소 (태그 `MmSt`, `+0xdb MiAllocatePool` → `+0xf4 memset`) | 직접 | nt | `①SN… → MiBuildImageControlArea +0x134` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |

### 3.23 `_SECTION_IMAGE_INFORMATION` — exe 이미지 정보 (커널, `0x40`, ntoskrnl PDB)

- 칸 26 · 아는 칸 24 (정확 24 · 짐작 0) · 아무도 안 씀 2 · 모름 0 · 줄 26 — **(run47, sast/41) 모르던 10칸 닫힘 (Q78b)**, 옛 17줄의 "오프셋 미상" 14곳 채움.
- 사진 (run44): `K1 이전` 15 · 끝까지 0 11. `TransferAddress` = `7ff61a371d40`. 사진 K1 값이 `ms/bin/test.exe` 헤더와 칸마다 맞는다 (결과서 "버전 확인" — 스택·부시스템 3·버전 6.0·`DllCharacteristics`·`Machine`·`ImageFileSize` 등). 끝까지 0 인 헤더 칸 4개 (`SubSystemMinorVersion`·`MinorOperatingSystemVersion`·`LoaderFlags`·`CheckSum`) 는 헤더 값이 0 일 것 `[추측]` — 버전 둘의 0 은 "6.0" 대조로 맞고, `LoaderFlags`·`CheckSum` 은 결과서가 값을 따로 안 적음.
- 사는 곳: 세그먼트 할당 안 (`_SEGMENT +0x48`, `u2.ImageInformation` 이 가리킴). 쓰는 함수 안 레지스터: `MiBuildImageControlArea` 의 `R8` = 이미지 정보, `R15` = `MiVerifyImageHeader` 가 채운 헤더 칸 모음 (0x70).
- `SubSystemVersion`·`OperatingSystemVersion` 은 **겹침** (`[정적] PDB`: ULONG 하나가 Minor·Major 두 USHORT 와 같은 4바이트). 쓰는 줄은 Minor/Major 줄과 같다.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `TransferAddress` | ① | `ImageBase + AddressOfEntryPoint` (처음 만드는 줄) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x16b ~ +0x172` | `[정적]` run31 · sast26 · 정확 · 사진 `K1 이전` |
| `+0x0` `TransferAddress` | ① | 새 기본 주소면 델타를 더함 (조건부) | 직접 | nt | `①SC… → MiRelocateImage` 조각 `FUN_140b24658 +0x5d ~ +0x65` (= `nt+0x1b1279`) | `[정적]` run31 · sast26 · 정확 · 사진 `K1 이전` |
| `+0x8` `ZeroBits` | — | **아무도 안 씀** — 0 (풀 0. `MiBuildImageControlArea` 가 쓰는 `R8` 오프셋 목록 `0x0·0x10·0x18·0x20·0x24 ~ 0x3c` 에 없음) | — | — | (할당 `①SN… → MiBuildImageControlArea +0x107 MiAllocatePool`) | `[정적]` 부재 · sast41 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x10` `MaximumStackSize` | ① | = `SizeOfStackReserve` (`0x100000`) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x179` (읽기 `+0x175`) | `[정적]` sast26 · run31 · sast41 (오프셋) · 정확 · 사진 `K1 이전` |
| `+0x18` `CommittedStackSize` | ① | = `SizeOfStackCommit` (`0x1000`) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x181` (읽기 `+0x17d`) | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `K1 이전` |
| `+0x20` `SubSystemType` | ① | = `Subsystem` (3) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x18a` (읽기 `+0x185`) | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `K1 이전` |
| `+0x24` `SubSystemMinorVersion` | ① | = `MinorSubsystemVersion` (0) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x19d` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `끝까지 0` |
| `+0x26` `SubSystemMajorVersion` | ① | = `MajorSubsystemVersion` (6) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x193` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `K1 이전` |
| `+0x24` `SubSystemVersion` (겹침) | ① | 위 두 칸을 합친 4바이트 (`0x60000` = 6<<16) | 직접 | nt | `①SN… → MiBuildImageControlArea +0x193 · +0x19d` | `[정적]` sast41 · run47 · `[정적] PDB` 겹침 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |
| `+0x28` `MajorOperatingSystemVersion` | ① | = `MajorOperatingSystemVersion` (6) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x1a7` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `K1 이전` |
| `+0x2a` `MinorOperatingSystemVersion` | ① | = `MinorOperatingSystemVersion` (0) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x1b1` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `끝까지 0` |
| `+0x28` `OperatingSystemVersion` (겹침) | ① | 위 두 칸을 합친 4바이트 (6) | 직접 | nt | `①SN… → MiBuildImageControlArea +0x1a7 · +0x1b1` | `[정적]` sast41 · run47 · `[정적] PDB` 겹침 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |
| `+0x2c` `ImageCharacteristics` | ① | = 파일 헤더 `Characteristics` (파일 헤더 `+0x12`) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x1df` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `K1 이전` |
| `+0x2e` `DllCharacteristics` | ① | = 선택 헤더 `DllCharacteristics` (`0x8160`) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x1bb` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `K1 이전` |
| `+0x30` `Machine` | ① | = 파일 헤더 `Machine` (`0x8664`, 파일 헤더 `+0x0`) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x1e7` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `K1 이전` |
| `+0x32` `ImageContainsCode` | ① | 1 — `SizeOfCode ≠ 0` (`+0x1cb`) 또는 진입점 ≠ 0 (`+0x45e`) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x1d7` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `K1 이전` |
| `+0x32` `ImageContainsCode` | ① | 1 — 실행 가능 구역이 있으면 (구역을 훑을 때) | 직접 | nt | `①SN… → MiBuildImageControlArea → MiParseImageSectionHeaders +0x1bf` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` |
| `+0x33` `ImageFlags` | ① | 이 실행에서 켜진 비트는 `ImageDynamicallyRelocated` 하나 (`0x4`). 다른 비트는 아래 조건부 줄 | 직접 | nt | `①SC… → MiRelocateImage` 조각 `nt+0x1b1357` (+ 아래 조건부 줄) | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0x4`) · Q78b 로 닫힘 |
| `+0x33` `ImageDynamicallyRelocated` (`ImageFlags` 비트 2) | ① | 켬 (`OR byte [R15+0x33],4`) | 직접 | nt | `①SC… → MiRelocateImage` 조각 `FUN_140b24658` (`nt+0x1b1357`) | `[정적]` run31 · sast26 · 정확 · 사진 `K1 이전` |
| `+0x33` `ComPlusNativeReady` (bit0) · `ComPlusILOnly` (bit1) · `ComPlusPrefer32bit` (bit5) | ① | 헤더 COM 디렉터리가 있고 CLR 플래그가 맞을 때만. test.exe 는 COM 디렉터리 0 (결과서가 파일 직접 읽음) → 안 탐 | 직접 (조건부, 안 탐) | nt | `①SC… → MiParseComAndCetHeaders (호출 +0x267) +0x3d2 ~ +0x40c` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x33` `ImageMappedFlat` (bit3) | ① | 플랫 매핑 요청 (패킷 플래그 `0x100000`) 일 때만 — 같은 갈래가 `ImageContainsCode` = 1 (`+0x3f2`) 도. 안 탐 | 직접 (조건부, 안 탐) | nt | `①SN… → MiBuildImageControlArea +0x3ed` (갈림 `+0x270`) | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x33` `BaseBelow4gb` (bit4) | ① | 4GB 아래 요청 (패킷 플래그 bit8, `+0x323`) 일 때만. 안 탐 | 직접 (조건부, 안 탐) | nt | `①SN… +0x790` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x33` `Reserved` (6:2) | — | **아무도 안 씀** — 0 (예약 비트. 사슬에서 쓰는 줄 못 찾음) | — | — | — | `[추측]` sast41 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x34` `LoaderFlags` | ① | = 선택 헤더 `LoaderFlags` (0) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x1f0` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `끝까지 0` |
| `+0x38` `ImageFileSize` | ① | = 파일 크기 하위 32비트 (`0x86a00`, `FsRtlGetFileSize` 결과 — 입력) | 직접 (값은 파일 시스템 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x167` (크기 묻기 `MiCreateImageFileMap → FsRtlGetFileSize (호출 +0x91)`) | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `K1 이전` |
| `+0x3c` `CheckSum` | ① | = 선택 헤더 `CheckSum` (0) | 직접 (값은 파일 헤더 = 입력) | nt | `①SN… → MiBuildImageControlArea +0x1c4` | `[정적]` sast26 · run31 · sast41 · 정확 · 사진 `끝까지 0` |

### 3.24 `_FILE_OBJECT` — exe 파일 객체 (커널, `0xd8`, ntoskrnl PDB)

- 칸 30 · 아는 칸 22 (정확 12 · 짐작 10) · 아무도 안 씀 8 · 모름 0 · 줄 16 — **(run47, sast/41) 전부 닫힘 (Q78b)**.
  - 주인별: nt 18 (그중 6 은 nt 함수인데 부르는 쪽이 파일 시스템) · **다른 드라이버 (ntfs.sys) 5** (`Vpb`·`FsContext`·`FsContext2`·`SectionObjectPointer` + `Flags` 일부 — `Flags` 는 nt 와 양쪽에 셈) · 아무도 안 씀 8.
  - **다른 드라이버 칸 = nt 에 쓰는 줄 없음 `[정적]`, 모듈은 동적 확인 전 `[추측]`** (D7). 짐 지도 "다른 DLL" 과 같은 뜻 — 범위 밖, 표시만, 에뮬레이터에선 입력으로 취급. 다만 `SectionObjectPointer` 는 아래 줄의 꼬리표대로 **에뮬레이터가 대신 채워야 한다**.
- 사진 (run44): `K1 이전` 15 · 끝까지 0 15. `Flags` 는 `K3~P0` 에 비트 `0x4400` 이 더 켜짐 (`0xc0042` → `0xc4442`), `Event` 도 뒤에 바뀜.
- 만드는 곳: `①F… +0xe19 → IopAllocRealFileObject` — 프로세서 룩어사이드 (`GS:[0x20]` `+0x840`/`+0x848`, `ExpInterlockedPopEntrySList` `+0x9a`·`+0xb7`) 에서 받거나 `+0x16a ObpAllocateObject` (`0xd8`, `IoFileObjectType`) → `+0x21e memset(…,0xd8)` → 칸 채우기. 핸들은 `ObOpenObjectByNameEx +0x405 ObpCreateHandle`. exe 를 여는 쪽은 `NtCreateUserProcess +0x6cb IopCreateFile` (run9 맞음) — `+0x740 IoCreateFileEx` 는 첫 번째가 실패했을 때만 타는 재시도.
- 열기 인자 (`NtCreateUserProcess +0x6c6` 앞): 접근 `0x100020` (`SYNCHRONIZE|FILE_EXECUTE`), 공유 5 (읽기+삭제), `FILE_OPEN`, 옵션 `0x60` (`FILE_NON_DIRECTORY_FILE|FILE_SYNCHRONOUS_IO_NONALERT`), 객체 속성 `OBJ_KERNEL_HANDLE|OBJ_CASE_INSENSITIVE` (+ 사용자 모드면 `OBJ_FORCE_ACCESS_CHECK`). 이름 문자열 `[RBP+0x290]` 을 채우는 줄은 `[공백]` (7.10). (플래그 이름은 공개 헤더 이름 `[추측]`)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x00` `Type` · `+0x02` `Size` | ① | 5 (파일) · `0xd8` (`MOV [RBX],0xd80005`) | 직접 | nt | `①F… → IopAllocRealFileObject +0x375` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (5·216) · Q78b 로 닫힘 |
| `+0x08` `DeviceObject` | ① | 장치 객체 (`IopParseDevice` 가 3번째 인자로 넘긴 `[RSP+0x70]`) | 직접 | nt | `①F… → IopAllocRealFileObject +0x383 ~ +0x387` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |
| `+0x10` `Vpb` | ① | 볼륨 VPB | 직접 `[추측]` | **다른 드라이버 (ntfs.sys)** — nt 에 쓰는 줄 없음 `[정적]`, 모듈은 동적 확인 전 `[추측]` | `①F… → IofCallDriver (호출 +0xf5a)` 안 (`IRP_MJ_CREATE`, 대상 = `Vpb->DeviceObject` `+0x71e`) | `[정적]` 쓰는 줄 부재 · `[추측]` 주체 · sast41 · run47 · 짐작 · 사진 `K1 이전` · D7 · Q78b 로 닫힘 |
| `+0x18` `FsContext` · `+0x20` `FsContext2` | ① | 파일 시스템 내부 문맥 둘 | 직접 `[추측]` | **다른 드라이버 (ntfs.sys)** — nt 에 쓰는 줄 없음 `[정적]`, 모듈은 동적 확인 전 `[추측]` | 위와 같음 | `[정적]` 쓰는 줄 부재 · `[추측]` 주체 · sast41 · run47 · 짐작 · 사진 `K1 이전` · D7 · Q78b 로 닫힘 |
| `+0x28` `SectionObjectPointer` | ① | `_SECTION_OBJECT_POINTERS` (0x18, FS 메모리) 주소. **꼬리표: nt 가 곧바로 읽는다** — `MiReferenceControlArea +0x44`·`MiCreateImageFileMap`·`MiSectionCreated +0x53` 가 읽고, 가리키는 곳의 `ImageSectionObject` (`+0x10`) 는 **nt 가 쓴다** (`MiReferenceControlArea +0x178` 자리표시 → `MiSectionCreated +0x124` 진짜 컨트롤 영역). → **에뮬레이터가 대신 채울 칸** (FS 가 없으니 칸과 0x18 블록을 에뮬레이터가 만들어야 섹션 사슬이 돈다) | 직접 `[추측]` | **다른 드라이버 (ntfs.sys)** — nt 에 쓰는 줄 없음 `[정적]`, 모듈은 동적 확인 전 `[추측]` | 위와 같음 (`IopAllocRealFileObject +0x419 ~ +0x44d` 의 복사는 다시 열기 갈래 — exe 는 아님) | `[정적]` 쓰는 줄 부재 · `[추측]` 주체 · sast41 · run47 · 짐작 · 사진 `K1 이전` · D7 · Q78b 로 닫힘 |
| `+0x4a` `ReadAccess` · `+0x4d` `SharedRead` · `+0x4f` `SharedDelete` | ① | 1 · 1 · 1 — `ReadAccess = (접근 & 0x21) ≠ 0`, 공유 비트 0·2 (접근 `0x100020`·공유 5 로 계산이 K1 과 맞음) | 주소 넘김 (FS 가 파일 객체를 넘김 `[추측]`) | nt (부르는 쪽 다른 드라이버 ntfs.sys `[추측]`) | `①F… → IofCallDriver (호출 +0xf5a) → [ntfs.sys] → IoSetShareAccess +0xf → IoSetLinkShareAccess +0x16 · +0x5b · +0x63` (또는 `IoSetShareAccessEx +0x1a →`. 검사만 `IoCheckLinkShareAccess +0x46 ~ +0x99`) | `[정적]` 쓰는 줄 · `[추측]` 부르는 쪽 (`refs`: nt 안 caller 는 `IoSetShareAccess*`·`IoCheckShareAccess*`·`RawCreate`·`FsRtlpCheckSharingAgainstOplockOwner` 뿐, `IopParseDevice` 아님) · sast41 · run47 · 짐작 · 사진 `K1 이전` · D9 · Q78b 로 닫힘 |
| `+0x4b` `WriteAccess` · `+0x4c` `DeleteAccess` · `+0x4e` `SharedWrite` | ① | 0 을 씀 (접근에 쓰기 `6`·삭제 `0x10000` 없음, 공유에 쓰기 없음) | 주소 넘김 (위와 같음) | nt (부르는 쪽 다른 드라이버 ntfs.sys `[추측]`) | `… → IoSetLinkShareAccess +0x23 · +0x2a · +0x67` | `[정적]` 쓰는 줄 · `[추측]` 부르는 쪽 · sast41 · run47 · 짐작 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x50` `Flags` | ① | `0x2` 동기 IO (`CreateOptions & 0x30` → `\|=2`) · `0x4` 알림 가능. 조건부 (안 탐): `0x20000` (인자 bit6 이 꺼졌을 때 — 사진 꺼짐), `0x800` 장치 직접 열기, `0x400000` | 직접 | nt | `①F… → IopAllocRealFileObject +0x2ee ~ +0x2f4 · +0x2fe` (조건부 `+0x36e` · `IopParseDevice +0xe37 · +0xaa5`) | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0xc0042`) · 비트 이름은 공개 헤더 `[추측]` · Q78b 로 닫힘 |
| `+0x50` `Flags` | ① | `0x40000` 핸들 만들어짐 (`BTS [FO+0x50],0x12`) | 직접 | nt | `①… → IopCreateFile` 조각 `FUN_1408c7e1f +0xbb` (= `IopCreateFile +0x522`) | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` |
| `+0x50` `Flags` | ① | K1: `0x40` (캐시 지원) · `0x80000` — nt 에 켜는 줄 없음. K3~P0: `0x4000` (`FO_CLEANUP_COMPLETE` 로 보임) · `0x400` (이름 못 정함 `[공백]`) — exe 핸들 닫기 때 FS 의 cleanup 처리 `[추측]` (`IopCloseFile` 에 두 상수 없음) | 직접 `[추측]` | **다른 드라이버 (ntfs.sys)** — nt 에 쓰는 줄 없음 `[정적]`, 모듈은 동적 확인 전 `[추측]` | K1 쪽: `①F… → IofCallDriver (호출 +0xf5a)` 안. K3~P0 쪽: `①… → PspDeleteCreateProcessContext (호출 +0x11d0) → ObCloseHandle (호출 +0x134) → … → IopCloseFile → IofCallDriver (호출 +0x138, IRP_MJ_CLEANUP)` 안 | `[정적]` 쓰는 줄 부재 · `[추측]` 주체·구간 · sast41 · run47 · 짐작 · 사진 `K1 이전` → 바뀜 `K3~P0` (`0xc4442`) · D6·D7 |
| `+0x58` `FileName` | ① | 남은 이름 — `MaximumLength` (`+0xe95`), 버퍼 풀 (태그 `IoNm`) → 복사 | 직접 · 주소 넘김 (`RtlCopyUnicodeString(&FO+0x58, …)`) | nt | `①F… +0xe95 · → ExAllocatePool2 (호출 +0xea8) · +0xead · → RtlCopyUnicodeString (호출 +0xec6)` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` · Q78b 로 닫힘 |
| `+0x80` `Lock` | ① | 동기 이벤트 (`SynchronizationEvent`) — 동기 파일 (`Flags & 2`, `+0x307`) 일 때만 | 주소 넘김 | nt | `①F… → IopAllocRealFileObject → KeInitializeEvent (호출 +0x319)` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (`0x00060001`) · Q78b 로 닫힘 |
| `+0x98` `Event` | ① | 알림 이벤트 (`NotificationEvent`). K1 `SignalState` 1 은 IRP 완료가 켠 것 `[추측]`. 뒤 바뀜은 닫을 때 `IopCloseFile → IopResetEvent (호출 +0xb8)` `[추측]` | 주소 넘김 | nt | `①F… → KeInitializeEvent (호출 +0xf1a)` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` → 바뀜 · Q78b 로 닫힘 |
| `+0xc0` `IrpList` · `+0xb8` `IrpListLock` | ① | 자기 자신을 가리키는 빈 목록 · 0 을 씀 | 직접 | nt | `①F… → IopAllocRealFileObject +0x39d · +0x3a1` · `+0x38b` | `[정적]` sast41 · run47 · 정확 · 사진 `K1 이전` (IrpList) · `끝까지 0` (IrpListLock) · Q78b 로 닫힘 |
| `+0x40` `RelatedFileObject` · `+0x68` `CurrentByteOffset` · `+0x70` `Waiters` | ① | 0 을 씀 (`RelatedFileObject` = 열기 패킷 값 0, `IopCreateFile +0x21b ~ +0x2ea` 가 0 으로 둠. 뒤 둘은 동기 파일일 때) | 직접 | nt | `①F… → IopAllocRealFileObject +0x37f` · `+0x323` · `+0x320` | `[정적]` sast41 · run47 · 정확 · 사진 `끝까지 0` · Q78b 로 닫힘 |
| `+0x30` `PrivateCacheMap` · `+0x38` `FinalStatus` · `+0x48` `LockOperation` · `+0x49` `DeletePending` · `+0x74` `Busy` · `+0x78` `LastLock` · `+0xb0` `CompletionContext` · `+0xd0` `FileObjectExtension` | — | **아무도 안 씀** — 0 (`memset` 만. 캐시 맵·완료 포트·확장은 이 열기에서 안 씀) | — | — | (할당 `①F… → IopAllocRealFileObject → memset (호출 +0x21e)`) | `[정적]` 부재 · sast41 · 사진 `끝까지 0` · Q78b 로 닫힘 |

## 4. 두 공간 짝

| 유저 쪽 | 커널 쪽 | 무엇이 맞아야 하나 | 근거 |
|---|---|---|---|
| PEB 자체 (`_PEB`) | `_EPROCESS.Peb` (+0x2e0) | 같은 주소 — `MmCreatePeb` 가 출력 칸에 씀 | `[정적]` run10 · `[관찰]` run44: `EPROCESS.Peb` = `9b2fa66000` = 실제 PEB 주소 (8장 모두) |
| `_PEB.ImageBaseAddress` (+0x10) = 붙인 exe 이미지 | `_EPROCESS.SectionBaseAddress` (+0x2b0), exe 섹션 (`SectionObject` +0x2a8 → `_SECTION` → `_CONTROL_AREA` → `_SEGMENT`), VAD 구역 | 같은 값 (`MiMapProcessExecutable +0xc5` → `MmCreatePeb` 가 옮김). 붙인 구역이 VAD 에도 있을 것 `[추측]` | `[정적]` run10 · 결과서 Q77 표 · `[관찰]` run44: 둘 다 `7ff61a370000` (K1 부터), VAD `7ff61a370` ~ `7ff61a3fb` Exe 매핑 |
| exe 진입점: 맡긴 문맥 `Rcx`, `_ETHREAD.Win32StartAddress` | `_SECTION_IMAGE_INFORMATION.TransferAddress` | `ImageBase + AddressOfEntryPoint` (다른 주소면 + 델타). 같은 값이 `PspCreateUserContext` → CONTEXT → 트랩 프레임 → 맡긴 문맥으로 흐른다 (flow.md 2.1 "진입점 값이 흘러가는 길") | `[정적]` run31 · run16 · run11 · `[관찰]` run44: `TransferAddress` = `Win32StartAddress` = 처음 트랩 프레임 `Rcx` = 맡긴 `_CONTEXT.Rcx` = `7ff61a371d40` |
| TEB (`_TEB`) | `_KTHREAD.Teb` (+0xf0) | 같은 주소 (`MmCreateTeb` 결과 → `KeInitThread`). 칸 이름은 `[추측]` | `[추측]` run11 · `[관찰]` run44: `KTHREAD.Teb` = `9b2fa67000` = TEB (K2 부터) — 칸 이름 짐작이 값으로 맞음 |
| 매개변수 블록 (`PEB.ProcessParameters` +0x20 이 가리킴) | 커널 사본 (만들기 정보 `+0xd8`, `NtCreateUserProcess +0xacb PspCaptureProcessParameters`) | 내용 = 커널 사본 (붙기 전 `Flags` 비트 15·29·`LoaderThreads` 를 손본 것). 안쪽 포인터 12개는 새 주소 기준으로 고친 값 | `[정적]` run32 · `[관찰]` run44: P1 까지는 이 짝 그대로, **P1a 부터 블록이 힙 안 다른 주소** (`25996f37a50`) 로 옮겨짐 — ~~옮긴 쪽 `[공백]` (7.8)~~ → (run45 `[정적]`) ntdll `RtlpInitParameterBlock` (`LdrpInitializeProcess +0xc3f`) 이 옮김. 그 뒤로는 커널 사본과 짝이 아니라 **프로세스 힙 블록**이다 |
| 매개변수 `ConsoleHandle`·`StandardInput`·`StandardOutput`·`StandardError` | 새 프로세스 핸들 표 (`_HANDLE_TABLE`) | 핸들 값 ↔ 핸들 표 항목 (`ObDuplicateObject` 로 부모 것을 복제). 핸들 표 쪽 칸은 `[공백]` | `[정적]` run32 · `[관찰]` run44: K1 `ConsoleHandle` 4 = 핸들 표 `0x4` File `\Reference {ConDrv}`, P1b 부터 `0x60` = File `\Connect {ConDrv}` (P2 `!handle`) |
| ntdll 전역 `LdrSystemDllInitBlock` (`.mrdata` rva `0x1ea450`, 0x128) | `PsNtdllExports.LdrSystemDllInitBlock` (+0x0, 부팅 때 채운 블록 주소), 새 프로세스 작업 집합 구조 `+0x4a0` (블록 `+0xb8` 출처), `PspApplyMitigationOptions` 에 넘긴 24바이트 (블록 `+0xa0`) | 블록 주소 = 이 프로세스 안 ntdll 전역. `+0xb8` = CFG 비트맵 주소, `+0xa0` = 완화 옵션 (칸 이름 `[추측]`) | `[정적]` run32 · run16 · `[관찰]` run44: `PsNtdllExports.LdrSystemDllInitBlock` = `7ffafb84a450` = ntdll+`0x1ea450`, 블록 칸은 K1 부터 값 있음 · 출처 (run48): `PsNtdllExports +0x0` = `Ⓑ2… → PspLookupEntryPoint (호출 +0x56) → RtlFindExportedRoutineByName (호출 +0x1b)`, 쓰기 `PspInitializeSystemDlls +0x6b` (7.6.2) |
| `LdrSystemDllInitBlock` `+0x10` (`SystemDllNativeRelocation`) · `+0x8` (`SystemDllWowRelocation`) (이름 run32) | `PspSystemDlls[0]` 칸 `+0x38` · WOW 칸 `+0x38` (재배치량) | 같은 값을 옮김 (`PspPrepareSystemDllInitBlock +0xa5/+0xac` · `+0xcb ~ +0xd6`) | `[정적]` sast42 · run48 · 출처 `ⒷL… → MmGetSectionInformation (호출 +0x17e)` 쓰기 `PspLocateSystemDll +0x1aa` (7.6.2) · 값은 안 봄 (D1) |
| ntdll 시작 주소 (`LdrpSystemDllBase`, `LdrInitializeThunk` 가 받은 `Rdx`) | `PspSystemDlls[0]` 칸 `+0x20` (부팅 때 정한 주소) | 같은 값. `PspMapSystemDll +0x172` 가 그 주소에 붙었는지 검사한다 | `[정적]` run16 · run18 · `[관찰]` run44: P1 트랩 프레임 `Rdx` = `LdrpSystemDllBase` (P1a) = ntdll 베이스 `7ffafb660000` (`PspSystemDlls` 는 안 읽음) · 출처 (run48): `ⒷL… → PspMapSystemDll (호출 +0x233) +0x1a5 → RtlImageNtHeader (호출 +0x1ae)` 뒤 떨어진 조각 `+0x207054` (rva `0xb18c2c`) `MOV [RBX+0x20]` = System 프로세스에 올린 ntdll 뷰 헤더의 `ImageBase` (= 섹션 만들 때 MM 이 고른 주소, `ⒷL… +0xef MmCreateSpecialImageSection → … → MiSelectImageBase`). 칸 `+0x20` 을 실제로 읽은 값은 아직 없음 (D1 → Q42) |
| ntdll 함수 `LdrInitializeThunk`·`RtlUserThreadStart` | `PsNtdllExports` (`+0x8`·`+0x10`, `_PS_NTDLL_EXPORTS` 0xa8) | 표 값 = 이 프로세스 안 그 함수 주소 (ntdll 이 부팅 때 정한 주소에 붙으니 같을 것 `[추측]`) | `[정적]` run16 · `[관찰]` run44: `PsNtdllExports` `+0x8` = ntdll+`0xe1420`, `+0x10` = ntdll+`0xacac0` = 실제 두 함수 (P1 `rip`, P0 트랩 `Rip`) · 출처 (run48): `Ⓑ2… → PspLookupEntryPoint (호출 +0x56) → RtlFindExportedRoutineByName (호출 +0x1b)`, 쓰기 `PspInitializeSystemDlls +0x6b`. 값 = 뷰 안 주소 + (칸 `+0x20` − `+0x28`) 이라 **`+0x20` 기본 주소 기준 사용자 주소** `[정적]` → 위 "같을 것 `[추측]`" 은 `PspMapSystemDll +0x172` (같은 주소 강제) 와 합쳐 `[정적]`. `[관찰]` run44 K1 로그 1531 ~ 1542줄: 21칸 중 이름 표에 없는 `+0x38`·`+0x98` 만 0 |
| 유저 스택 위 맡긴 문맥 (`_CONTEXT`, `LdrInitializeThunk` 의 `Rcx`) | `_KTRAP_FRAME` (`Rip` +0x168 · `Rsp` +0x180 · `Rcx` +0x38 · `Rdx` +0x40) | ① 끝: 트랩 `Rip` = `LdrInitializeThunk`, 맡긴 문맥 `Rip` = `RtlUserThreadStart`·`Rcx` = 진입점·`Rdx` = PEB, `ContextFlags` = `0x10001b` (CET 면 `0x10005b`). ② 끝 (`NtContinue`): 맡긴 문맥을 트랩 프레임에 다시 씀 | `[정적]` run16 · run29 · run42 · `[관찰]` run44: P0 트랩 프레임 (처음 문맥) 의 `Rip`·`Rcx`·`Rdx`·`Rsp`·`EFlags` = P1 `_CONTEXT` (`ContextFlags 0x10001b`), P2 `_CONTEXT` = P1 (같은 주소·0x4d0 바이트 같음). P1 트랩 `Rcx` = `_CONTEXT` 주소 |
| `_TEB.CurrentLocale` (+0x108) | 세션 로케일 (`PsGetSessionLocaleId`) | 같은 값 | `[정적]` run15 · `[관찰]` run44: `0x412` (`P0~P1`), 세션 쪽 값은 안 읽음 |
| `_TEB.SchedulerSharedDataSlot` (+0x1850) | `NtSetInformationThread` 클래스 0x39 → `PspSchedulerSharedDataRegionSlotAllocate` | 커널이 내준 표 주소 (조건이 안 맞으면 0) | `[정적]` run41 · `[관찰]` run44: `25996cb0000` (`P1~P1a`) = 2쪽짜리 매핑 섹션 VAD (옛 매개변수 블록 자리) |
| 이벤트 핸들 전역 (`LdrpInitCompleteEvent`, `LdrpLoadCompleteEvent`, `LdrpWorkCompleteEvent`) | 핸들 표 항목 → 이벤트 객체 | 핸들 값 ↔ 핸들 표. 이벤트 객체 쪽 칸은 `[공백]` | `[정적]` run17 · run24 · `[관찰]` run44: `0xc` · `0x50` · `0x54` = 핸들 표의 Event (P2 `!handle`) |
| `LdrpKnownDllDirectoryHandle` (`.mrdata`) | 핸들 표 항목 → `\KnownDlls` 디렉터리 객체 | `NtOpenDirectoryObject` 의 출력 칸일 것 `[추측]` | `[추측]` run18 · `_refs` DATA · `[관찰]` run44: `0x4c` = 핸들 표의 Directory `KnownDlls` (P2 `!handle`) — 짐작이던 짝이 값으로 맞음 |
| DLL 항목 `DllBase` (kernel32·kernelbase·dll1·dll2) | VAD 구역 + DLL 섹션 (`NtMapViewOfSection`) | 붙인 주소 — 커널이 칸에 직접 쓴다. VAD 구역 시작과 같을 것 `[추측]` | `[정적]` run38 · `[관찰]` run44: 넷 다 P2 VAD 의 Exe 매핑 시작과 같음 |
| `PEB.ApiSetMap` (+0x68) | `MmMapApiSetView` 가 올린 뷰 (`PspSetupUserProcessAddressSpace +0x255`) | 뷰 주소 `[추측]` — 이 칸을 쓰는 줄은 `[공백]` | `[정적]` run32 (뷰 올리기) · 결과서 Q77 표 · `[관찰]` run44: `25996cd0000` (`K1 이전`) = K1 VAD 의 읽기 전용 매핑 (`25996cd0` ~ `25996cf9`) |
| `PEB.LeapSecondData` (+0x7b8) | leap second 데이터 섹션 뷰 (`MmCreatePeb` 안 `MmMapViewOfSection +0x132`) | 뷰 주소 | `[정적]` run10 · `[관찰]` run44: `7ff5ef0a0000` (`K1 이전`) = 한 쪽짜리 매핑 VAD. 내용은 8장 모두 읽기 불가 |
| 프로세스 힙 (`PEB.ProcessHeap` = `LdrpHeap` = `_HEAP` 시작) | VAD 구역 하나 (private, 예약 1MB) | 예약 `0x100000 + n×0x10000` 을 받고 앞 `n×0x10000` 을 풀어 1MB 만 남김. 첫 확정 `0x2000` (뒤에 자람) | `[정적]` sast40 · run46 (`RtlpCreateHeap +0x45e / +0x4a6 / +0x517`) · `[관찰]` run44: P1a VAD `25996f30 – 2599702f` (`0x100` 쪽, 확정 9쪽), 힙 머리 `NumberOfUnCommittedPages` `0xf6` |
| `RtlpHpHeapManager +0x10` 희소 비트맵 #1 (`+0x8` 예약 시작, `+0x0` 요약 쪽) | VAD 구역 하나 (예약 `0x2001000`, `MEM_TOP_DOWN`) | 크기 = 사용자 공간 끝 (`NtQuerySystemInformation` 0x3e) 으로 계산한 32MB + 4KB, 요약 1쪽만 확정 | `[정적]` sast40 · run46 · `[관찰]` run44: VAD `7ff5ed090 – 7ff5ef090` (`0x2001` 쪽, 확정 1쪽) = 덤프 `0x1ce910+0x8` / `+0x0` |
| `RtlpHpHeapManager +0x58` VA 관리자 희소 비트맵 (`+0x28` 예약 시작) | VAD 구역 하나 (예약 `0x100020000`, `MEM_TOP_DOWN`) | 4GB + 128KB, 확정 없음 — #1 바로 아래 | `[정적]` sast40 · run46 · `[관찰]` run44: VAD `7ff4ed070 – 7ff5ed08f` (`0x100020` 쪽, 확정 0) = 덤프 `0x1ce958+0x28` |
| `0x1ce430` IFEO 바탕 키 핸들 캐시 (`.data`, 이름 없음) | 핸들 표 항목 → 레지스트리 키 객체 | 핸들 값 ↔ 핸들 표 (`NtOpenKey` 출력). 핸들 표 쪽 항목은 `[공백]` (안 맞춰 봄) | `[정적]` sast40 · run46 · `[관찰]` run44: `0x10` (P1a). 핸들 표의 `0x10` 이 Key 인지는 안 봄 |
| `_KUSER_SHARED_DATA` (`0x7ffe0000`) | 커널 쪽 별칭 주소 `[공백]` (이 문서군에 없음) | 같은 물리 페이지 `[추측]` | 결과서 Q77 표 · `[관찰]` run44: 커널 별칭 = `fffff78000000000` — P1a 에서 두 주소의 0xa80 바이트가 같음 (유저 주소는 K1 ~ P1 읽기 불가) |

## 5. 단계 순서

- 짐이 놓이는 순서를 **코드 순서** (flow.md 트리 · 오프셋) 로 적었다 `[정적]`. 갈림길·고리·일꾼 스레드 때문에 실행 순서는 다를 수 있다 (flow.md 2.1 주의). 사진 (run44) 은 아래 5.0 처럼 **구간** 단위로 실행 순서를 준다 — 구간 안 순서는 여전히 코드 순서.
- 괄호 안 오프셋은 그 함수 안 쓰는 줄 또는 부르는 자리. 칸 이름 뒤 "(짐작)" 은 3장에서 짐작인 줄.

### 5.0 사진으로 본 구간 순서 (run44 `[관찰]`)

- 구간마다 처음 값이 생긴 대표 칸. 아는 칸의 정적 순서 (아래 ①·② 목록) 는 이 구간 순서를 어기지 않는다 (2.1 마지막 줄).
1. **K1 이전** (`NtCreateUserProcess` ~ `PspAllocateProcess` 끝, 섹션 만들기) — EPROCESS 62칸·섹션 사슬·파일 객체·토큰·핸들 표 10칸, PEB 23칸 (`ProcessParameters` `25996cb0000`·`ApiSetMap`·`LeapSecondData` 포함), 매개변수 20칸, ntdll `.mrdata` 끝 페이지 (`LdrSystemDllInitBlock`). VAD 8개.
2. **K1~K2** (`PspAllocateThread`) — ETHREAD 31·KTHREAD 49·트랩 프레임 9 (처음 문맥)·TEB 9칸, 유저 스택 VAD.
3. **K2~K3** (`PspInsertProcess`, `PspInsertThread +0x807` 까지) — 목록 넣기 (`ActiveProcessLinks`·`SequenceNumber`), `HANDLE_TABLE.UniqueProcessId` 부모 → 자기, 락 둘 풀기, `SuspendCount` 1.
4. **K3~P0** (부모: 알림 뒤 마무리·csrss 알림·`NtResumeThread`, 자식: 잠들었다 깸) — `ProcessFirstResume`, `SuspendCount` 0, PEB `pShimData`·`SystemDefaultActivationContextData` (VAD 2개), `ExceptionPortData`.
5. **P0~P1** (`PspUserThreadStartup` → `PspInitializeThunkContext`) — TEB `CurrentLocale`, 맡긴 `_CONTEXT`, 트랩 프레임 바꿔 끼우기.
6. **P1~P1a** (ntdll 혼자) — PEB `Ldr`·`ProcessHeap`·`FastPebLock`·`LoaderLock`·TLS 비트맵, **매개변수 블록 옮기기**, exe·ntdll 항목, 보안 쿠키, 핸들 4 → 26, VAD 11 → 21 (힙·NLS 파일·스케줄러 칸 섹션).
7. **P1a~P1b** (kernel32·kernelbase 준비 → 병렬 로드 → dll1·dll2 올리기·잇기) — 모듈 4개 항목·노드, `Kernel32ThreadInitThunkFunction`, CSR 연결 (`Csr*`, PEB 공유 메모리 칸), 콘솔 핸들 `0x60`, `OwnerProcessId` `0x729`, 일꾼 2개 (`ActiveThreads` 3), 핸들 44, VAD 32.
8. **P1b~P2** (그래프 켜기 → `_LdrpInitialize` 뒤쪽) — dll1·dll2 `ProcessAttachCalled`·초기화 순서 목록, 노드 `State` 9, `LdrInitState` 3, `LdrpProcessInitialized` 2, `ProcessInitializing` 끔, `FlsData`, CPU 기능 전역.

### Ⓑ 부팅 단계 (프로세스 생성 전, run48 `[정적]`)

- 짐 지도 범위 (①·②) **앞**이다. 여기 놓인 짐은 ① 에서 `입력` 으로 읽힌다. 칸 표는 7.6.2.
1. `Ⓑ0` (링크 때) — `PspSystemDlls` 표 `[0]`·`[1]`·`[4]`, 칸 `+0x0`·`+0x8`·`+0xa`·`+0x10`·`+0x30`, 이름 표 `NtdllExportInformation` (ntdll 19 · WOW 8 · 나머지 0) 이 PE 파일에 박혀 있다.
2. `Ⓑ1` **`Phase1Initialization` → `IoInitSystem` → `IoInitSystemPreDrivers`** (`IopInitializeBootDrivers` `+0xf3b` 뒤, `IopReassignSystemRoot` `+0x1139` 앞) — `NtGlobalFlag |= 0x40000` (`+0x10ef`) → `+0x10f5 PsLocateSystemDlls` → (`ExVerifySuite` 건너뛰기 검사 → 표 6칸 고리, 칸마다 `+0x9e PspLocateSystemDll`: `ZwSystemDebugControl(0x26)` (디버깅 부팅이면 `ExpSysDbgPulledFileTable`) → `ZwOpenFile` → `MmCreateSpecialImageSection` (섹션 객체) → 파일 핸들 닫기 → 섹션 객체 참조 → 섹션 핸들 닫기 → 칸 `+0x38` (`+0x1aa`) → 칸 `*[+0x0]` 빠른 참조 (`+0x1b3`·`+0x1b6`) → (필수 칸만) 확인 셋 → `+0x233 PspMapSystemDll(…, 0, 1)`: System 프로세스에 뷰 → 칸 `+0x20` → `+0xc` → `+0x28`) → `NtGlobalFlag` 되돌림 (`+0x10fa`). 칸 순서는 `[0]` → `[1]` → `[4]` (고리 순서, 실제로 셋 다 도는지는 D7·D9).
3. `Ⓑ2` **`Phase1InitializationIoReady` `+0x1d5` → `PsInitSystem(2)` → `PspInitPhase2 +0x172 PspInitializeSystemDlls`** — 칸마다 (`+0x20 ≠ 0` 만) 이름 표 → `PsNtdllExports` 19칸 (`+0x6b`) → (WOW 칸) `PsWowX86SharedInformation` 8칸 + `+0x30` (`+0x80`).
4. `Ⓑ3` `Phase1InitializationIoReady +0x1e2 MmInitSystemDll` — `MiState.Processes.SystemDllBase` (`+0x22`).
5. `Ⓑ4` `Phase1InitializationIoReady +0x20a PsInitSystem(3) → PspInitPhase3` — `VslVsmEnabled` 일 때만 `PsSecureSystemProcess` 에 ntdll·vertdll 뷰 → `VslpEnterIumSecureMode` (D8).

### ① 커널 단계

1. **부모 cmd.exe 스레드 — `NtCreateUserProcess`** (2.1)
   1. (run47) `+0x6cb IopCreateFile` 아래 exe 파일 객체: `IopParseDevice +0xe1e IopAllocRealFileObject` — `memset` → `Flags` `0x2`·`0x4` · `Lock` · 0 칸 (`CurrentByteOffset`·`Waiters`) → `Type`·`Size` (`+0x375`) · `RelatedFileObject` · `DeviceObject` · `IrpListLock` · `IrpList` → `IopParseDevice` `FileName` (`+0xea8 ~ +0xec6`) → `Event` (`+0xf1a`) → `+0xf5f IofCallDriver` (ntfs.sys: `Vpb`·`FsContext`·`FsContext2`·`SectionObjectPointer`·공유 접근 6칸·`Flags` `0x40`·`0x80000`, `[추측]`) → `IopCreateFile` 조각 `Flags` `0x40000`. (`IopAllocRealFileObject` 안 칸 순서는 오프셋 순 = 코드 순서 `[정적]`)
   2. `+0x93c MmCreateSpecialImageSection → MiCreateSection` 아래 exe 섹션 (run47 고침): `+0xf8 MiCreateImageOrDataSection → MiReferenceControlArea` 갈림길 — 같은 파일 컨트롤 영역이 있으면 (공유 길) `MiReferenceControlArea +0x107` `NumberOfSectionReferences` · **`+0x11d`** `NumberOfUserReferences` +1 → `MiShareExistingControlArea` (`SizeOfSection` 만) / 없으면 (새 길) `+0x178` 자리표시 `ImageSectionObject` → `MiCreateNewSection → MiCreateImageFileMap` (`u1.PartitionId` `+0x31e`) → `MiBuildImageControlArea` (풀 3개 → 컨트롤 영역·세그먼트·이미지 정보 칸 — 3.21 ~ 3.23. 이미지 정보: 헤더 칸 `+0x167 ~ +0x1f0`·`TransferAddress` `+0x16b ~ +0x172`, 세그먼트 `+0x130 ~ +0x149`, 컨트롤 영역 `+0x1f4 ~ +0x238`, `BasedAddress` `+0x25e` → `MiParseImageSectionHeaders` `u3`·`ImageCommitment`·`DefaultProtectionMask`) → `MiSectionCreated` (`FilePointer`·`WaitList`·`BeingCreated` 끔·`ImageSectionObject` 교체) → `MiRelocateImage → MiSelectImageBase` (`u2` 재배치 칸·`BasedAddress`·`ImageActive`, `TransferAddress` += 델타, `ImageDynamicallyRelocated`) → 돌아와 **`+0x125 MiFinishCreateSection`** (`_SECTION`: `ObCreateObjectEx` → 패킷 사본 복사 → `StartingVpn` 0 → `u.UserReference`) → `+0x146 ObInsertObjectEx` 섹션 핸들.
   3. `+0xc36 PspAllocateProcess` — `OwnerProcessId` (`+0x710`) → `Pcb` (`+0x171e`, 짐작) → `Token` (`+0x1927`, 짐작) → `ObjectTable` (`+0x1991`, 짐작) → `Flags3` 비트 7개 (오프셋 미상) → `+0x2095 MmInitializeProcessAddressSpace → MiMapProcessExecutable` `SectionBaseAddress` (`+0xc5`) → `+0x2314 MmCreatePeb` `Peb` 칸 + PEB 22칸 → `+0x239c PspSetupUserProcessAddressSpace`: 매개변수 커널 사본 `Flags` 비트 15·29·`LoaderThreads` → (새 프로세스에 붙기) → `PspCopyAndFixupParameters`: 블록 통째 복사 → Fixup·핸들 복제·`Flags` 비트 14 → PEB `ProcessParameters` → `PspPrepareSystemDllInitBlock` ntdll `LdrSystemDllInitBlock` → (`+0x26e9` 프로세스 락, 짐작) → `CreateTime` (`+0x27c4`) → `UniqueProcessId` (`+0x27e4`).
   4. `+0xe5c PspAllocateThread` — `ThreadLock` 잡기 (`+0x51c`) → `Cid` (`+0x532`) → (`+0x5ef PspSetupUserStack` 유저 스택) → `+0x631 MmCreateTeb` (TEB, 칸별 `[공백]`) → `StartAddress`·`Win32StartAddress` (`+0x647`·`+0x655`) → `+0x6f9 KeInitThread`: `ApcQueueable` (`+0xed`) → `SchedulerApc` (`+0x152`) → `SuspendEvent` (`+0x163`) → `Teb` (짐작) → `KiInitializeContextThread`: `SegCs`·`SegSs` 손보기 (`+0xb5`, 짐작) → `KxContextToKframes` 트랩 `Rip`·`Rsp`·`Rcx`·`Rdx` 처음 → `InitialStack`·`KernelStack` (`+0x2ca`·`+0x2ce`).
   5. `+0xfc2 PspInsertProcess` — `ActiveProcessLinks`·`SequenceNumber` (`+0xa6 ~ +0x109`).
   6. `+0x101a PspInsertThread` (흐름 순서, run12) — `ActiveThreads` +1 → `ThreadWasActive` (짐작) → `ThreadListHead` (짐작) → `ProcessLock` 풀기 (`+0x2a2`) → (flags bit0) `KeSuspendThread`: `SuspendCount` +1 (`+0x60`) → `KiSuspendThread`: `SuspendEvent` 0 (`+0x6c`) → `KiSignalThreadForApc` `ApcState` → `ThreadInserted` (`+0x490`) → `ThreadLock` 풀기 (`+0x499`) → `KeReadyThread`.
   7. (run47) `+0x11d5 PspDeleteCreateProcessContext` — `+0x139 ObCloseHandle` (exe 파일 핸들) → `IopCloseFile +0x13d IofCallDriver` (`IRP_MJ_CLEANUP`) — `FILE_OBJECT.Flags` `0x4400` (`K3~P0`) 후보 `[추측]` (D6). `+0x143 ObfDereferenceObject` 로 파일 객체 참조를 놓음.
2. **부모의 다음 시스템 콜 `NtResumeThread`** (2.2, run13·run14) — `PsMultiResumeThread +0x4e` `ProcessFirstResume` → `KeResumeThread +0x68` `SuspendCount` −1 → `KiResumeThread +0x20` `SuspendEvent` = 1.
3. **test.exe 스레드 — `KiStartUserThread → PspUserThreadStartup`** (2.3) — (멈춘 스레드면) APC 인터럽트 → `KiSchedulerApc +0xf2` `SuspendSchedulerApcWait` 켜고 잠듦 → (2 가 풀어 줌) → `+0xf7` 끔 → `+0x78` TEB `CurrentLocale` → `+0xb3 PspInitializeThunkContext`: `SpecialApcDisable` (`+0x113`) → 원래 문맥을 유저 스택에 맡김 (`RtlCopyContext`, 7장 — 표 없음) → `PspSetContext` 트랩 `Rip` = `LdrInitializeThunk`·`Rsp`·`Rcx`·`Rdx` → `SpecialApcDisable` 되돌림 (`+0x2de`) → `IRETQ` 로 유저.
   - 2 와 3 의 앞뒤 (스레드가 먼저 잠드나, 부모가 먼저 푸나) 는 동적 Q19.

### ② ntdll 단계 (test.exe 첫 스레드)

1. **`LdrpInitialize`** (2.4) — `SecurityCookieInitialized` (`InitSecurityCookie +0x42`, `__security_cookie` 짐작) → `LdrpIsSecureProcess` (`+0x5a`, 조건부) → `SpecialMachineFramesInitialized` (`InitSpecialMachineFrames +0x57`) ⇒ 꼬리 점프.
2. **`LdrpInitializeInternal`** — `LdrpHotPatchInitialized` 0 → 1 (`+0x3b`) → `LdrpInitializeHotPatching` `LdrpIsHotPatchingEnabled` (`+0x55`) → `LdrpNtdllHotPatchContext` (`+0xa7`) → `_LdrpInitialize` → 돌아와 `LdrpNtdllHotPatchContext` 비움 (`+0x5e`).
3. **`_LdrpInitialize`** — `LdrpProcessInitialized` 0 → 1 (`+0x3a`) → `LdrpInitCompleteEvent` (`NtCreateEvent`, syscall) · TEB `RanProcessInit` (`+0xea`) → PEB `LoaderLock` (`+0xfa`) → `LdrInitState` (`+0x101`) → PEB `ProcessInitializing` 켬 (`+0x107`) → `LdrInitializeMrdata` (`+0x14a`: `LdrpMrdataLock` 0 → 조회 → `LdrpAllocationGranularity`·`LdrpMaximumUserModeAddress`, run45) → **`LdrpInitializeProcess`** (4) → `LdrpDetourExist` (`+0x1d7`) → `LdrInitState` = 3 (`+0x205`) → `ProcessInitializing` 끔 (`+0x20f`).
4. **`LdrpInitializeProcess`** (앞쪽 run18 → 뒤쪽 run19)
   - 앞쪽: `PebLdr` 길이·빈 목록 셋·종료 칸 처음 값 (`+0x158 ~ +0x1b3`) → PEB `Ldr` (`+0x1c1`) → PEB `ActiveCodePage`·`OemCodePage` (`+0x1cd LdrpInitializeNlsInfo`, run45) → `LdrpAppHeaders` (`+0x36b`) → `LdrpHashTable` 비우기 (`+0x3c5`) → `LdrpInitializeExecutionOptions` (`LdrpLargePageDllKeyHandle`·`LdrpImageExpansionMitigation`, 매개변수 `LoaderThreads` 덮기) → `LdrpIsHotPatchingEnabled` = 0 (조건, `+0x587`) → `UseWOW64` (`+0x5f2`) → `LdrpSystemDllBase` (`+0x68c`) → PEB TLS 비트맵 4칸 (`+0x774 ~ +0x8a3`) → `FastPebLock` 만들기 (`+0x9cd`) → PEB `FastPebLock` (`+0x9e1`) → **힙 관리자** (`+0x9ed RtlInitializeHeapManager`, run46: `RtlpHpHeapGlobals` 난수·키 → `RtlpHpLfhPerfFlags` → GC 타이머 `0x1ce270~` (IFEO 바탕 키 `0x1ce430`) → `RtlpDisableBreakOnFailureCookie` → PEB `NumberOfHeaps` 0·`ProcessHeaps`·`MaximumNumberOfHeaps` (`+0xe7 ~ +0x10a`) → `RtlpProcessHeaps` 빈 목록 → `RtlpProcessHeapsLock` → `RtlpHeapKey` → LFH 관리자 (`RtlpLFHKey`·`RtlpAffinityState`·`RtlpDefaultHeapDebuggingOptions`) ⇒ `RtlHpInitializeHeapManager` (`RtlpHpHeapManager`·등록부·`RtlpHpEnvFlsContext`·`RtlpHpEnvHandle` → 큰 예약 둘)) → (`+0xa34` 안 탐) → **프로세스 힙** (`+0xa4b LdrpInitializeProcessHeap`, run46: `LdrpHeapPartitionName` → `RtlpCreateHeap`: `0x1ce6e8`·`0x1ce6f0` → 힙 예약·확정 → `_HEAP` 머리 (7.6.1) → `RtlpProcessHeapsInsert`: `RtlpProcessHeaps` 노드·`RtlpPebHeapListStaticBuffer[0]`·PEB `NumberOfHeaps` 1 → `+0x1a2` PEB `ProcessHeap`) → `LdrpHeap` (`+0xb12`) → `NtdllBaseTag` = 0 (`+0xb7d`) → 옛 매개변수 블록의 `EnvironmentVersion`·`Environment`·`EnvironmentSize` (`+0xc32 RtlpInitEnvironmentBlock`, run45) → **매개변수 블록을 힙으로 옮김** (`+0xc3f RtlpInitParameterBlock`: 새 블록 40칸 → 포인터 9개 → PEB `ProcessParameters`, run45) → `LdrpKnownDllDirectoryHandle` (`+0xdb1`, 짐작) → `LdrpKnownDllPath` (`+0xe64 ~ +0xe72`) → `RtlpSystem32Dirs`·`LdrpSystem32` (`+0x109e ~ +0x10b2`) → `RtlpSystemDirs` (`+0x1161`) → **ntdll 항목** (`+0x11a2 LdrpInitializeNtdllDataTableEntry`: 항목·노드 → 칸 → 명부·해시 → 색인 → 마무리 → `State` 9 → `LdrpNtDllDataTableEntry`) → 초기화 순서 목록 머리 (`+0x1283 ~ +0x12d7`).
   - 뒤쪽: `+0x12e3 LdrpInitParallelLoadingSupport` (큐·재시도 큐·잠금·이벤트 둘) → **exe 항목** (`+0x135e LdrpAllocateModuleEntry` → `LdrpImageEntry` `+0x1374` → `FullDllName`·`BaseDllName`·`DllBase` → 명부 머리 → 색인 → 마무리 → `State` 9 `+0x1508`) → `LdrpLargePageDllKeyHandle` (`+0x179e`) → (WOW64 만 `LdrpLoadWow64`) → (CFG 강제일 때만 `+0x180b LdrpInitializeCfgScpHelpers`: `LdrpGuardCheckIcallNoESFptr`·`LdrpGuardDispatchIcallNoESFptr`, run45 — test.exe 는 안 탄 것 `[추측]`) → TEB `SchedulerSharedDataSlot` (`+0x183c`) → `LdrpInitializeTls` (`LdrpTlsList`, exe `TlsIndex`) → **`LdrpInitializeKernel32Functions`** (kernel32·kernelbase 항목 = `②M…` 길, `Kernel32ThreadInitThunkFunction`, `LdrpDetourExist`·`LdrpMainThreadToken`·일감 전역 …) → `LdrpEnableParallelLoading` (`LdrpMapAndSnapWork`) → `LdrInitState` = 1 (`+0x1a4c`) → exe 노드 `State` 2 (`+0x1a60`) → **`LdrpMapAndSnapDependency`** (dll1·dll2: 항목·노드·간선·일감·`State` 2 ~ 5) → `LdrpDrainWorkQueue(1)` → `LdrpPrepareModuleForExecution` (그래프 정리 `LowestLink`·`State` 6, `State` 7) → `LdrInitState` = 2 (`+0x1b1e`) → `LdrpInitializePerUserWindowsDirectory` (`RtlpSystemDirs`·`RtlpSystem32Dirs` 다시) → `LdrpProcessInitContextRecord` (`+0x1b83`) → **`LdrpInitializeGraphRecurse`** (모듈마다 `LdrpInitializeNode`: `State` 8 → 초기화 순서 목록 꼬리 → `LdrpCurrentDllInitializer` → TLS 콜백 (`LdrpTlsLock`) → 진입점 → `ProcessAttachCalled` → `State` 9) → `LdrpProcessInitContextRecord` = 0 (`+0x1bec`) → exe TLS (`+0x1c99 LdrpCallTlsInitializers`, `LdrpTlsLock`).
5. **은퇴** — `LdrInitializeThunk +0x18 NtContinue` → 커널 `KiContinueEx → KeTestAlertThread` (`ApcState`) → `KiContinuePreviousModeUser +0x422` 트랩 `Rip`·`Rsp`·`Rcx`·`Rdx` → `IRETQ` → `RtlUserThreadStart`.

## 6. 공용 부품

- (run46) 첫 표 — 프로세스 힙 구간 (Q78a) 에서 만난 부품. "끝까지 본 run" 이 run46 인 줄은 줄기 끝 (syscall 또는 더 부르는 게 없음) 까지 본 것, 아닌 줄은 이름·하는 일만.

| 부품 | 하는 일 | 끝까지 본 run | 쓰는 줄기 |
|---|---|---|---|
| **프로세스 힙** (`PEB.ProcessHeap` = `LdrpHeap`, `25996f30000` 이 실행) | 뒤 짐 대부분의 **블록 출처** (매개변수·환경 블록, 모듈 항목·노드, TLS, 활성화 컨텍스트 스택 …). 힙 안 첫 블록 둘: `FrontEndHeapUsageData` (`0x100`B, 힙 `+0x750`), `ProcessHeaps` 노드 (`0x30`B, 힙 `+0x860`). 첫 빈 블록 = 힙 `+0x740` | run46 (만들기까지. 나눠 주기는 범위 밖 — 7.6.1) | 3.1 `ProcessParameters` (②), 3.2 환경, 3.5·3.6 모듈 항목·노드, 3.8 `RtlpDllSearchPath` 등 |
| `RtlAllocateHeap` | 힙에서 블록 받기 | — (본문 범위 밖, 사용자 결정 2026-10-26 기록만 · 2026-10-27 후보 B 안 넣음) | 위 모든 블록 |
| `RtlpHpEnvAllocVA` | 힙 관리자의 가상 메모리 받기 → `NtAllocateVirtualMemoryEx` (`+0xdd`) | run46 | 3.8 `RtlpHpHeapManager` 희소 비트맵 둘 (세그먼트 힙 길도 이것) |
| `RtlCSparseBitmapInitialize` · `RtlCSparseBitmapStart` | 희소 비트맵 칸 준비 · 비트 수로 크기 계산 → 예약 한 번 + (요약 ≤ 1쪽이면) 확정 한 번 | run46 | 3.8 `RtlpHpHeapManager +0x10`·`+0x58` |
| `RtlpSecMemFreeVirtualMemory` | `NtFreeVirtualMemory` (`+0x2a`), `0xC0000045` 이고 보안 메모리 목록이 있으면 재시도 | run46 | 프로세스 힙 앞쪽 무작위 몫 풀기 |
| `RtlRandom` · xorshift64* (`RtlpHpHeapGlobals +0x40`) | 키·무작위 몫·`Encoding` 난수. 시스템 콜 없음 | run46 | 3.8 `RtlpHpHeapGlobals`·`RtlpHeapKey`·`RtlpLFHKey`, 7.6.1 `Encoding` |
| (run47) `ObCreateObjectEx` (커널) | 객체 만들기 (머리 `_OBJECT_HEADER` + 몸통). `_SECTION` 몸통 `0x40` (`MiFinishCreateSection +0x143`), 스레드 객체 (`PspAllocateThread +0x2e7`) | — (지도 `helper`, 이름만) | 3.20 · 3.13 |
| (run47) `ObpAllocateObject` (커널) | 파일 객체 `0xd8` 할당 (룩어사이드에 없을 때) | — (이름만) | 3.24 |
| (run47) `MiAllocatePool` (커널) | 풀 할당. 첫 인자 플래그에 "초기화 안 함" 비트 (`0x2`) 가 없으면 **0 으로 채워 줌** `[정적]` (비트 이름은 `ExAllocatePool2` POOL_FLAGS 기억 `[추측]`) — 3.21 ~ 3.23 "0 그대로" 칸의 이유 | 일부 (run47: 플래그만) | 3.21 · 3.22 · 3.23 |
| (run47) `ExAllocatePool2` · `RtlCopyUnicodeString` · `KeInitializeEvent` (커널) | 풀 할당 (태그 `IoNm` 파일 이름 버퍼) · 문자열 복사 · 이벤트 칸 초기화 (주소 넘김) | — (이름만) | 3.24 `FileName`·`Lock`·`Event`, 3.14 |
| (run47) 스핀락 `ExAcquireSpinLockExclusive` · `MiReleaseSpinLockExclusive` (커널) | 잡았다 놓음 → 칸이 0 으로 돌아옴 | — (이름만) | 3.21 `ControlAreaLock` |
| (run47) `IoSetShareAccess` · `IoSetShareAccessEx` → `IoSetLinkShareAccess` (커널 공개 함수) | 파일 객체 공유 접근 6칸 쓰기 (`ReadAccess` 등). nt 안에서 `IopParseDevice` 는 안 부름 → 부르는 쪽 파일 시스템 `[추측]` | run47 (본문 읽음) | 3.24 |
| `RtlInitializeCriticalSectionEx` → `RtlpAddDebugInfoToCriticalSection` | 임계 구역 만들기. `DebugInfo` 는 ntdll 정적 배열 `RtlpStaticDebugInfo` 에서 (`RtlCriticalSectionList*` 에 잇기). ETW (`NtTraceEvent`, `+0x118`) 는 로거가 켜졌을 때만 | 일부 (run46: DebugInfo 출처·ETW 조건만) | 3.8 `FastPebLock`·`RtlpProcessHeapsLock`, 7.6.1 힙 안 락 |
| (run48) `MmCreateSpecialImageSection` → `MiCreateSection` 사슬 (커널) | 이미지 섹션 만들기. **exe (① `NtCreateUserProcess +0x937`) 와 시스템 DLL (`Ⓑ1` `PspLocateSystemDll +0xef`) 이 같은 사슬**이고 인자만 다르다 (토큰 NULL · 서명 수준 `0x0c` · 할당 속성 `0x02400000` → `SEC_IMAGE` + 보호 이미지 패킷 플래그 `0x10`) | run47 (exe 길 칸까지) · run48 (인자 차이) | 3.20 ~ 3.23, 7.6.2 |
| (run48) `ObInitializeFastReference` · `PspReferenceSystemDll` · `ObFastDereferenceObject` (커널) | 빠른 참조 자리에 객체 넣기 (참조 +15 덤) · 하나 잡기 · 돌려주기 (태그 `PsSd`) | — (하는 일만, sast/42) | 7.6.2 칸 `*[+0x0]`, ① `PsMapSystemDlls` |
| (run48) `RtlFindExportedRoutineByName` · `RtlImageNtHeader` (커널) | export 표에서 이름 → 주소 · 뷰의 NT 헤더 찾기 | — (이름만) | 7.6.2 `PsNtdllExports`·칸 `+0x20`·`+0xc` |
- (run45) 후보 첫 판: sast/39 결과서 5.4 "공용 부품" 표 — 초기화 함수 15개 callee 637줄 중 공용 부품 146줄 (`memset` 25 · `LdrpLogInternal` 10 · `KiUnstackDetachProcess` 9 · `__security_check_cookie` 8 · `ObfDereferenceObject` 7 · `KiStackAttachProcess` 7 · `RtlAppendUnicodeStringToString` 7 · `ExAllocatePool2` 5 · `RtlAllocateHeap` 4 · 기능 스위치 `Feature_*`·`wil_details_*` 11 …). 이름·하는 일 짐작뿐이라 표에는 아직 안 올렸다.
- 3장에 "주소 넘김" 으로 나온 받는 함수들 (`RtlInitializeCriticalSectionEx`, `RtlRbInsertNodeEx`, `ObDuplicateObject`, `KeInitializeApc`, `KeInitializeEvent`, `LdrGetProcedureAddressForCaller`, `TpAllocWork` 등) 이 후보다. 이름만 적어 둔다 — 본문은 안 봤다.

## 7. 빈 곳

`_workspace/plan.md` 로 옮길 때 출처는 baggage.md. 이번 바퀴에 생긴 빈 곳은 오케스트레이터 결정대로 새 항목 없이 **Q77 (사진)·Q78 (쓴 자리·줄기)** 에 출처로 붙는다.
(run45) 일 목록 (sast/39) 에서 나온 체크리스트 밖 짐은 7.6 — 사용자 결정 (2026-10-26) 대로 Q79·Q82·Q86 에 붙거나 **기록만** 한다. 닫은 빈 곳은 지우지 않고 `(해결됨) … → run45` 로 남겼다.
(run46) Q78a (프로세스 힙 구간) 로 닫힌 칸은 7.2·7.3 목록에서 빼고 `(해결됨 → run46)` 줄로 남겼다. 이 조각의 동적 질문은 7.9 (plan Q91).
(run47) Q78b (exe 섹션 묶음) 로 닫힌 다섯 구조체 74칸은 7.2 에서 `(해결됨 → run47)` 로 남겼다. 이 조각의 동적 질문은 7.10 (plan Q61·Q92).

### 7.1 0 그대로 — 사진으로 셈 (run44)

- 2장 "0 그대로 (사진)" = 사진 8장 모두 0 인 칸: 구조체 **847** (유저 285 · 커널 562), ntdll 전역 **297** (`.data` 265 · `.mrdata` 32, 둘 다 K1 ~ P1 은 읽기 불가였음). 그중 모르는 칸은 구조체 773 (run47 전 806) · 전역 281 (2.1).
- **"아무도 안 씀" 의 증거가 아니다.** 아는 칸 중에도 끝까지 0 이 구조체 63 (run47 전 41 — 섹션 묶음 22칸: 조건부 안 탐·0 을 씀) · 전역 16 있다 — 모두 썼다 되돌림·조건 안 맞음·0 을 씀 중 하나로 설명된다 (3장 "사진", run44 "정적 쓴 자리와 맞춰 보기"). 모르는 칸의 끝까지 0 도 같은 셋 중 하나다. (run47) 섹션 묶음의 끝까지 0 33칸은 정적으로 갈랐다: 조건부 안 탐 9 · 0 을 씀 13 · 아무도 안 씀 (할당 0) 11 (3.20 ~ 3.24). 에뮬레이터에서 "0 으로 둬도 되나" 는 쓴 자리 (Q78) 와 같이 봐야 한다.
- 범위 밖 길 전역 `LdrpForkInProgress`·`LdrpDllDirectory` (앞 16바이트) 는 끝까지 0* — 초기화 동안 처음 값 그대로 (3.8).
- 못 읽은 것: `.00cfg` 5칸 (8장 모두 `????`), VAD 노드 23칸 (칸 안 읽음), `≤P1a` 전역 156 + `PebLdr` 5칸 (K1 ~ P1 읽기 불가) → 7.8. (run45 정정: 전엔 "`≤P1a` 161" 로 적었는데, "161" 은 2.1 의 "전역 못 좁힌 것" (`≤P1a` 156 + `.00cfg` 5) 과 숫자만 같은 다른 묶음이라 풀어 썼다)

### 7.2 구조체별 모르는 칸 — 사진 구간별 (모두 1,196칸. run47 전 1,270, run46 전 1,273, run45 전 1,275)

- 이름은 PDB 직접 칸. `X.Y` = 정수 칸 `X` 안의 비트 칸 `Y`. `*` = 처음 값 뒤에 또 바뀜 (어느 구간인지는 run44 비교 결과, 2.1 "바뀜 있음"). 오프셋·타입은 표 원본 `struct_<이름>.tsv`.
- 모듈 항목·노드는 6개 모듈 중 가장 이른 구간에 넣고, 6개 모두가 아니면 값이 생긴 모듈을 괄호에 적었다 (`test` = exe, `k32` = KERNEL32, `kb` = KERNELBASE).
- 구간이 `K1 이전` 이 아닌 칸 (유저 44 · 커널 187) 은 그 구간 안 함수만 뒤지면 된다 → Q78 후보 (7.8).
- K1 이전에 이미 0 이 아닌데 아는 쓴 자리가 없는 칸 = 커널 `PspAllocateProcess` 까지 (토큰·핸들 표 만들기 포함. 섹션 만들기·exe 열기는 run47 에서 닫힘) 또는 부팅 때 값 (KUSD).

**`_PEB`** (모름 88 / 124)

- `K1 이전` (4): BitField.IsImageDynamicallyRelocated, ApiSetMap, ActiveProcessAffinityMask, UseCaseMapping — (run45) `ActiveCodePage`·`OemCodePage` 는 쓰는 줄이 나와 3.1 로 옮김. `UseCaseMapping` 은 `LdrpInitializeNlsInfo` 가 읽기만 해서 그대로 모름
- `K3~P0` (2): pShimData, SystemDefaultActivationContextData
- `P1~P1a` (2): CrossProcessFlags*, TppWorkerpList* — (해결됨 → run46, sast/40) `NumberOfHeaps`·`MaximumNumberOfHeaps`·`ProcessHeaps` 는 쓰는 줄이 나와 3.1 로 옮김 (`RtlInitializeHeapManager +0xe7`·`+0x10a`·`+0xf5`, `RtlpProcessHeapsInsert +0xb9`)
- `P1a~P1b` (5): ReadOnlySharedMemoryBase, ReadOnlyStaticServerData, CSDVersion, PatchLoaderData, CsrServerReadOnlySharedMemoryBase
- `끝까지 0` (75): ReadImageFileExecOptions, BeingDebugged, BitField.ImageUsesLargePages, BitField.IsProtectedProcess, BitField.SkipPatchingUser32Forwarders, BitField.IsPackagedProcess, BitField.IsAppContainer, BitField.IsProtectedProcessLight, BitField.IsLongPathAwareProcess, Padding0, SubSystemData, AtlThunkSListPtr, IFEOKey, CrossProcessFlags.ProcessInJob, CrossProcessFlags.ProcessUsingVEH, CrossProcessFlags.ProcessUsingVCH, CrossProcessFlags.ProcessUsingFTH, CrossProcessFlags.ProcessPreviouslyThrottled, CrossProcessFlags.ProcessCurrentlyThrottled, CrossProcessFlags.ProcessImagesHotPatched, CrossProcessFlags.ReservedBits0, Padding1, KernelCallbackTable, UserSharedInfoPtr, SystemReserved, AtlThunkSListPtr32, TlsExpansionCounter, Padding2, SharedData, AnsiCodePageData, OemCodePageData, UnicodeCaseTableData, GdiSharedHandleTable, ProcessStarterHelper, GdiDCAttributeList, Padding3, Padding4, GdiHandleBuffer, PostProcessInitRoutine, Padding5, AppCompatFlags, AppCompatFlagsUser, AppCompatInfo, ActivationContextData, ProcessAssemblyStorageMap, SystemAssemblyStorageMap, MinimumStackCommit, SparePointers, ChpeV2ProcessInfo, AppModelFeatureState, SpareUlongs, UnusedNlsField, WerRegistrationData, WerShipAssertPtr, EcCodeBitMap, pImageHeaderHash, TracingFlags, TracingFlags.HeapTracingEnabled, TracingFlags.CritSecTracingEnabled, TracingFlags.LibLoaderTracingEnabled, TracingFlags.SpareTracingBits, Padding6, TppWorkerpListLock, WaitOnAddressHashTable, TelemetryCoverageHeader, CloudFileFlags, CloudFileDiagFlags, PlaceholderCompatibilityMode, PlaceholderCompatibilityModeReserved, LeapSecondFlags, LeapSecondFlags.SixtySecondEnabled, LeapSecondFlags.Reserved, NtGlobalFlag2, ExtendedFeatureDisableMask, Win32ClientProcInfo

**`_RTL_USER_PROCESS_PARAMETERS`** (모름 0 / 40)

- 없음.

**`_TEB`** (모름 133 / 137)

- `K1~K2` (8): ClientId, ProcessEnvironmentBlock, TxFsContext, RealClientId, StaticUnicodeString, DeallocationStack, SameTebFlags*, SameTebFlags.InitialThread
- `P0~P1` (5): CurrentIdealProcessor, IdealProcessorValue, ReservedPad2, IdealProcessor, PrimaryGroupAffinity
- `P1~P1a` (5): ThreadLocalStoragePointer, RngState, _ActivationStack, ActivationContextStackPointer, HeapData*
- `P1a~P1b` (1): SameTebFlags.LoadOwner*
- `P1b~P2` (2): LastStatusValue, FlsData
- `끝까지 0` (112): EnvironmentPointer, ActiveRpcHandle, LastErrorValue, CountOfOwnedCriticalSections, CsrClientThread, Win32ThreadInfo, User32Reserved, UserReserved, WOW32Reserved, FpSoftwareStatusRegister, ReservedForDebuggerInstrumentation, SystemReserved1, HeapFlsData, PlaceholderCompatibilityMode, PlaceholderHydrationAlwaysExplicit, PlaceholderReserved, ProxiedProcessId, WorkingOnBehalfTicket, ExceptionCode, Padding0, InstrumentationCallbackSp, InstrumentationCallbackPreviousPc, InstrumentationCallbackPreviousSp, InstrumentationCallbackDisabled, UnalignedLoadStoreExceptions, Padding1, GdiTebBatch, GdiCachedProcessHandle, GdiClientPID, GdiClientTID, GdiThreadLocalInfo, Win32ClientInfo, glDispatchTable, glReserved1, glReserved2, glSectionInfo, glSection, glTable, glCurrentRC, glContext, Padding2, StaticUnicodeBuffer, Padding3, TlsSlots, TlsLinks, Vdm, ReservedForNtRpc, DbgSsReserved, HardErrorMode, Padding4, Instrumentation, ActivityId, SubProcessTag, PerflibData, EtwTraceData, WinSockData, GdiBatchCount, ReservedPad0, ReservedPad1, GuaranteedStackBytes, Padding5, ReservedForPerf, ReservedForOle, WaitingOnLoaderLock, Padding6, SavedPriorityState, ReservedForCodeCoverage, ThreadPoolData, TlsExpansionSlots, ChpeV2CpuAreaInfo, Unused, MuiGeneration, IsImpersonating, NlsCache, pShimData, Padding7, CurrentTransactionHandle, ActiveFrame, PreferredLanguages, UserPrefLanguages, MergedPrefLanguages, MuiImpersonation, CrossTebFlags, CrossTebFlags.SpareCrossTebBits, SameTebFlags.SafeThunkCall, SameTebFlags.InDebugPrint, SameTebFlags.HasFiberData, SameTebFlags.SkipThreadAttach, SameTebFlags.WerInShipAssertCode, SameTebFlags.ClonedThread, SameTebFlags.SuppressDebugMsg, SameTebFlags.DisableUserStackWalk, SameTebFlags.RtlExceptionAttached, SameTebFlags.SessionAware, SameTebFlags.LoaderWorker, SameTebFlags.SkipLoaderInit, SameTebFlags.SkipFileAPIBrokering, TxnScopeEnterCallback, TxnScopeExitCallback, TxnScopeContext, LockCount, WowTebOffset, ResourceRetValue, ReservedForWdf, ReservedForCrt, EffectiveContainerId, LastSleepCounter, SpinCallCount, Padding8, ExtendedFeatureDisableMask, HeapWalkContext, Rcu

**`_PEB_LDR_DATA`** (모름 2 / 9)

- `끝까지 0` (2): SsHandle, EntryInProgress

**`_LDR_DATA_TABLE_ENTRY`** (모름 34 / 65)

- `P1~P1a` (7): FlagGroup, Flags.LoadConfigProcessed, Flags.ProtectDelayLoad(ntdll·k32·kb), Flags.ScpInExceptionTable(ntdll·k32·kb), NodeModuleLink, LoadTime, DependentLoadFlags(ntdll·kb)
- `P1a~P1b` (3): Flags.LoadNotificationsSent(test·k32·kb·dll1·dll2), Flags.DontCallForThreads(k32), SwitchBackContext(test·k32·kb·dll1·dll2)
- `끝까지 0` (24): Flags.PackagedBinary, Flags.MarkedForRemoval, Flags.TelemetryEntryProcessed, Flags.ShimDll, Flags.VerifierProvider, Flags.ShimEngineCalloutSent, Flags.LoadInProgress, Flags.EntryProcessed, Flags.AuxIatCopyPrivate, Flags.ReservedFlags3, Flags.ProcessAttachFailed, Flags.CorImage, Flags.DontRelocate, Flags.CorILOnly, Flags.ChpeImage, Flags.ChpeEmulatorImage, Flags.ReservedFlags5, Flags.Redirected, Flags.ReservedFlags6, Flags.CompatDatabaseProcessed, EntryPointActivationContext, Lock, SigningLevel, ActivePatchImageBase

**`_LDR_DDAG_NODE`** (모름 4 / 10)

- `P1a~P1b` (1): PreorderNumber(test·k32·kb·dll1·dll2)
- `끝까지 0` (3): ServiceTagList, LoadWhileUnloadingCount, CondenseLink

**`_KUSER_SHARED_DATA`** (모름 103 / 103)

- `K1 이전` (62): TickCountMultiplier, InterruptTime*, SystemTime*, TimeZoneBias, ImageNumberLow, ImageNumberHigh, NtSystemRoot, LargePageMinimum, RNGSeedVersion, TimeZoneBiasStamp, NtBuildNumber, NtProductType, ProductTypeIsValid, NativeProcessorArchitecture, NtMajorVersion, ProcessorFeatures, Reserved1, Reserved3, BootId, SuiteMask, KdDebuggerEnabled, MitigationPolicies, MitigationPolicies.NXSupportPolicy, MitigationPolicies.SEHValidationPolicy, CyclesPerYield, ActiveConsoleId, ComPlusPackage, LastSystemRITEventTickCount, NumberOfPhysicalPages, VirtualizationFlags, SharedDataFlags, SharedDataFlags.DbgElevationEnabled, SharedDataFlags.DbgVirtEnabled, SharedDataFlags.DbgInstallerDetectEnabled, SharedDataFlags.DbgMultiSessionSku, TestRetInstruction, QpcFrequency, FullNumberOfPhysicalPages, TickCount*, TickCountQuad*, ReservedTickCountOverlay*, Cookie, ConsoleSessionForegroundProcessId, TimeUpdateLock*, BaselineSystemTimeQpc*, BaselineInterruptTimeQpc*, QpcSystemTimeIncrement, QpcInterruptTimeIncrement, QpcSystemTimeIncrementShift, QpcInterruptTimeIncrementShift, UnparkedProcessorCount, TelemetryCoverageRound, LangGenerationCount, QpcBias, ActiveProcessorCount, ActiveGroupCount, QpcData, QpcBypassEnabled, TimeZoneBiasEffectiveStart, TimeZoneBiasEffectiveEnd, XState, FeatureConfigurationChangeStamp
- `끝까지 0` (41): TickCountLowDeprecated, MaxStackTraceDepth, CryptoExponent, TimeZoneId, AitSamplingValue, AppCompatFlag, GlobalValidationRunlevel, Reserved0, NtMinorVersion, TimeSlip, AlternativeArchitecture, SystemExpirationDate, MitigationPolicies.CurDirDevicesSkippedForDlls, MitigationPolicies.Reserved, DismountCount, SafeBootMode, Reserved12, SharedDataFlags.DbgErrorPortPresent, SharedDataFlags.DbgLkgEnabled, SharedDataFlags.DbgDynProcessorEnabled, SharedDataFlags.DbgConsoleBrokerEnabled, SharedDataFlags.DbgSecureBootEnabled, SharedDataFlags.DbgMultiUsersInSessionSku, SharedDataFlags.DbgStateSeparationEnabled, SharedDataFlags.SpareBits, DataFlagsPad, SystemCall, Reserved2, SystemCallPad, TickCountPad, CookiePad, EnclaveFeatureMask, UserModeGlobalLogger, ImageFileExecutionOptions, Reserved4, InterruptTimeBias, Reserved9, QpcReserved, Spare, UserPointerAuthMask, Reserved10

**`_EPROCESS`** (모름 241 / 263)

- `K1 이전` (52): Flags2*, Flags2.DefaultPagePriority, Flags*, Flags.AddressSpaceInitialized, Flags.ProcessInSession, Flags.HasAddressSpace, Flags.DefaultIoPriority, ProcessQuotaUsage*, ProcessQuotaPeak*, PeakVirtualSize*, VirtualSize*, SessionProcessLinks, NumberOfPrivatePages*, SectionObject, InheritedFromUniqueProcessId, Session, QuotaBlock, ImageFilePointer, ImageFileName, PriorityClass, SeAuditProcessCreationInfo, HighestUserAddress, DefaultHardErrorProcessing, CommitCharge*, CommitChargePeak*, Vm*, MmProcessLinks, ExitStatus, VadRoot*, VadHint*, VadCount*, AlpcContext*, SvmProcessDeviceListHead, DiskCounters, EnergyContext, CreateInterruptTime, CreateUnbiasedInterruptTime, LastAppStateUpdateTime, SharedCommitCharge*, SharedCommitLinks*, Machine, MmSlabIdentity, VirtualTimerListHead, MitigationFlags, MitigationFlagsValues, MitigationFlags2, MitigationFlags2Values, PartitionObject, SecurityDomain, ParentSecurityDomain, PageCombineSequence, NetworkCounters
- `K2~K3` (3): Flags.ProcessInserted, ActiveThreadsHighWatermark*, Flags4
- `K3~P0` (11): Flags2.EnableWriteVmLogging, ExceptionPortData, ExceptionPortValue, Flags3*, Flags3.EnableProcessSuspendResumeLogging, Flags3.EnableThreadSuspendResumeLogging, Flags3.EnableProcessRemoteExecProtectVmLogging, Flags3.EnableProcessLocalExecProtectVmLogging, WakeChannel, WakeInfo, BamPpmPolicy
- `P0~P1` (9): Flags2.PrimaryTokenFrozen, Flags.CreateReported, Flags.LaunchPrefetched, Flags.ImageNotifyDone, ImagePathHash, PrefetchTrace*, ModifiedPageCount*, Flags3.AddressPolicyFrozen, Flags3.SecurityFreezeComplete
- `P1~P1a` (5): Cookie, DeviceMap, EtwDataSource, InvertedFunctionTable, WnfContext
- `P1a~P1b` (1): Win32Process
- `끝까지 0` (160): RundownProtect, Flags2.JobNotReallyActive, Flags2.AccountingFolded, Flags2.NewProcessReported, Flags2.ExitProcessReported, Flags2.ReportCommitChanges, Flags2.LastReportMemory, Flags2.ForceWakeCharge, Flags2.CrossSessionCreate, Flags2.NeedsHandleRundown, Flags2.RefTraceEnabled, Flags2.PicoCreated, Flags2.EmptyJobEvaluated, Flags2.ProcessVerifierTarget, Flags2.RestrictSetThreadContext, Flags2.AffinityPermanent, Flags2.AffinityUpdateEnable, Flags2.PropagateNode, Flags2.ExplicitAffinity, Flags2.Flags2Available1, Flags2.EnableReadVmLogging, Flags2.FatalAccessTerminationRequested, Flags2.DisableSystemAllowedCpuSet, Flags2.Flags2Available2, Flags2.InPrivate, Flags.NoDebugInherit, Flags.ProcessExiting, Flags.ProcessDelete, Flags.ManageExecutableMemoryWrites, Flags.VmDeleted, Flags.OutswapEnabled, Flags.Outswapped, Flags.FailFastOnCommitFail, Flags.Wow64VaSpace4Gb, Flags.SetTimerResolution, Flags.BreakOnTermination, Flags.DeprioritizeViews, Flags.WriteWatch, Flags.OverrideAddressSpace, Flags.Reserved, Flags.VmTopDown, Flags.PdeUpdateNeeded, Flags.VdmAllowed, Flags.ProcessRundown, Flags.ProcessSelfDelete, Flags.SetTimerResolutionLink, ExceptionPortValue.ExceptionPortState, MmReserved, AddressCreationLock, PageTableCommitmentLock, RotateInProgress, ForkInProgress, CommitChargeJob, CloneRoot, MmReserved2, Job, WorkingSetWatch, Win32WindowStation, Spare1, DebugPort, WoW64Process, PageDirectoryPte, SecurityPort, JobLinks, LastThreadExitStatus, LockedPagesList, ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount, CommitChargeLimit, VadPhysicalPages, VadPhysicalPagesLimit, TimerResolutionLink, TimerResolutionStackRecord, RequestedTimerResolution, SmallestTimerResolution, ExitTime, InvertedFunctionTableLock, LargePrivateVadCount, ThreadListLock, ServerSilo, SignatureLevel, SectionSignatureLevel, Protection, .HangCount, .GhostCount, .PrefilterException, Flags3.ReplacingPageRoot, Flags3.Crashed, Flags3.JobVadsAreTracked, Flags3.VadTrackingDisabled, Flags3.SubsystemProcess, Flags3.RelinquishedCommit, Flags3.HighGraphicsPriority, Flags3.CommitFailLogged, Flags3.ReserveFailLogged, Flags3.AllImagesAtBasePristineBase, Flags3.ForegroundExternal, Flags3.ForegroundSystem, Flags3.HighMemoryPriority, Flags3.SecurityDomainChanged, Flags3.VmProcessorHostTransition, Flags3.AltSyscall, Flags3.TimerResolutionIgnore, Flags3.EnableProcessImpersonationLogging, DeviceAsid, SvmData, SvmProcessLock, SvmLock, LastFreezeInterruptTime, PicoContext, EnclaveTable, EnclaveNumber, EnclaveLock, HighPriorityFaultsAllowed, VmContext, TotalUnbiasedFrozenTime, .LastAppStateUptime, .LastAppState, SharedCommitLock, AllowedCpuSets, DefaultCpuSets, AllowedCpuSetsIndirect, DefaultCpuSetsIndirect, DiskIoAttribution, DxgProcess, Win32KFilterSet, Spare0, ProcessTimerDelay, KTimerSets, KTimer2Sets, ThreadTimerSets, VirtualTimerListLock, CoverageSamplerContext, MmHotPatchContext, DynamicEHContinuationTargetsTree, DynamicEHContinuationTargetsLock, DynamicEnforcedCetCompatibleRanges, DisabledComponentFlags, EnableOptionalXStateFeaturesLock, PathRedirectionHashes, SyscallProvider, SyscallProviderProcessLinks, SyscallProviderDispatchContext, MitigationFlags3, MitigationFlags3Values, Flags4.MinimalTerminate, Flags4.ImageExpansionDisable, Flags4.SessionFirstProcess, SyscallUsage, SyscallUsageValues, SupervisorDeviceAsid, SupervisorSvmData, Execution, ThreadIndexTable, FreezeWorkLinks, PpmPolicyLock

**`_KPROCESS`** (모름 65 / 65)

- `K1 이전` (15): Header*, ProfileListHead, DirectoryTableBase, ThreadListHead*, Affinity, AutoBoostState, ReadyListHead, ActiveProcessors, BasePriority, QuantumReset, ActiveGroupsMask, IdealProcessorAssignmentBlock, PerProcessorCycleTimes, CpuPartitionList, AvailableCpuState
- `K2~K3` (2): StackCount*, ProcessListEntry
- `K3~P0` (2): ProcessFlags.PpmPolicy, ProcessFlags
- `끝까지 0` (46): ProcessLock, ProcessTimerDelay, DeepFreezeStartTime, SwapListEntry, ProcessFlags.AutoAlignment, ProcessFlags.DisableBoost, ProcessFlags.DisableQuantum, ProcessFlags.DeepFreeze, ProcessFlags.TimerVirtualization, ProcessFlags.CheckStackExtents, ProcessFlags.CacheIsolationEnabled, ProcessFlags.VaSpaceDeleted, ProcessFlags.MultiGroup, ProcessFlags.ForegroundProcess, ProcessFlags.ReservedFlags, Spare0c, Visited, Flags, ActiveGroupPadding, Padding, Padding2, SchedulerAssistYieldBoostCount, SchedulerAssistYieldBoostAllowedTime, Spare0d, IdealGlobalNode, Spare1, CycleTime, ContextSwitches, SchedulingGroup, KernelTime, UserTime, ReadyTime, FreezeCount, Spare4, UserDirectoryTableBase, AddressPolicy, Spare2, InstrumentationCallback, SecureState, KernelWaitTime, UserWaitTime, LastRebalanceQpc, ExtendedFeatureDisableMask, PrimaryGroup, Spare3, UserCetLogging

**`_ETHREAD`** (모름 144 / 149)

- `K1~K2` (27): Tcb*, CreateTime, ExitTime, KeyedWaitChain, PostBlockList, ActiveTimerListHead, KeyedWaitSemaphore, AlpcWaitSemaphore, IrpList, ReadClusterSize, BoostList, DeboostList, PropertySet, EnergyValues, Silo, OwnerEntryListHead, DisownedOwnerEntryListHead, UpdateTebApc*, UpdateTebApcFill1, UpdateTebApcFill2, MmSlabIdentity, UpdateTebApcFill3*, RngState*, UpdateTebApcFill4*, UsedByRngState*, UpdateTebApcFill5*, ProcessFastRef
- `K2~K3` (5): ThreadListEntry*, CrossThreadFlags, CrossThreadFlags.ThreadIoPriority, CrossThreadFlags.ThreadPagePriority, KernelStackReference
- `K3~P0` (4): LastExpectedRunTime*, LastSoftParkElectionGroupAffinity, LastSoftParkElectionQos*, LastSoftParkElectionRunningType
- `P1~P1a` (1): SchedulerSharedDataObject
- `끝까지 0` (107): ForwardLinkShadow, TerminationPort, ReaperLink, KeyedWaitValue, ActiveTimerListLock, ClientSecurity, TopLevelIrp, DeviceToVerify, ChargeOnlySession, LegacyPowerObject, RundownProtect, MmLockOrdering, CrossThreadFlags.Terminated, CrossThreadFlags.HideFromDebugger, CrossThreadFlags.ActiveImpersonationInfo, CrossThreadFlags.HardErrorsAreDisabled, CrossThreadFlags.BreakOnTermination, CrossThreadFlags.SkipCreationMsg, CrossThreadFlags.SkipTerminationMsg, CrossThreadFlags.CopyTokenOnOpen, CrossThreadFlags.RundownFail, CrossThreadFlags.UmsForceQueueTermination, CrossThreadFlags.IndirectCpuSets, CrossThreadFlags.DisableDynamicCodeOptOut, CrossThreadFlags.ExplicitCaseSensitivity, CrossThreadFlags.PicoNotifyExit, CrossThreadFlags.DbgWerUserReportActive, CrossThreadFlags.ForcedSelfTrimActive, CrossThreadFlags.SamplingCoverage, CrossThreadFlags.ImpersonationSchedulingGroup, CrossThreadFlags.ReservedCrossThreadFlags, SameThreadPassiveFlags, SameThreadPassiveFlags.ActiveExWorker, SameThreadPassiveFlags.MemoryMaker, SameThreadPassiveFlags.StoreLockThread, SameThreadPassiveFlags.ClonedThread, SameThreadPassiveFlags.KeyedEventInUse, SameThreadPassiveFlags.SelfTerminate, SameThreadPassiveFlags.RespectIoPriority, SameThreadPassiveFlags.ActivePageLists, SameThreadPassiveFlags.SecureContext, SameThreadPassiveFlags.ZeroPageThread, SameThreadPassiveFlags.WorkloadClass, SameThreadPassiveFlags.GenerateDumpOnBadHandleAccess, SameThreadPassiveFlags.BalanceSetManager, SameThreadPassiveFlags.ReservedSameThreadPassiveFlags, SameThreadApcFlags, SameThreadApcFlags.OwnsProcessAddressSpaceExclusive, SameThreadApcFlags.OwnsProcessAddressSpaceShared, SameThreadApcFlags.HardFaultBehavior, SameThreadApcFlags.StartAddressInvalid, SameThreadApcFlags.EtwCalloutActive, SameThreadApcFlags.SuppressSymbolLoad, SameThreadApcFlags.Prefetching, SameThreadApcFlags.OwnsVadExclusive, .SystemPagePriorityActive, .SystemPagePriority, .AllowUserWritesToExecutableMemory, .AllowKernelWritesToExecutableMemory, .OwnsVadShared, .PasidMsrValid, .SlabReplenishInProgress, CacheManagerActive, DisablePageFaultClustering, ActiveFaultCount, LockOrderState, SharedPsModuleLockAcquires, MmReserved, AlpcMessageId, AlpcMessage, AlpcReceiveAttributeSet, AlpcWaitListEntry, ExitStatus, CacheManagerCount, IoBoostCount, IoQoSBoostCount, IoQoSThrottleCount, BoostListLock, IrpListLock, ReservedForSynchTracking, CmCallbackListHead, ActivityId, SeLearningModeListHead, VerifierContext, AdjustedClientToken, WorkOnBehalfThread, PicoContext, UserFsBase, UserGsBase, SelectedCpuSets, SelectedCpuSetsIndirect, ThreadName, SetContextState, EtwSupport, DisownedOwnerEntryListLock, CmThreadInfo, FlsData, LastSoftParkElectionRunTime, LastSoftParkElectionGeneration, UserIsolationDomain, Win32kPriorityFloor, LastSoftParkElectionWorkloadType, UpdateTebSpareByte2, HeapData, Win32kThreadLock, ThreadIndex, MmFaultClusterContext

**`_KTHREAD`** (모름 227 / 237)

- `K1~K2` (42): Header*, StackLimit, StackBase, ExpectedRunTime*, StateSaveArea, MiscFlags.AutoBoostActive, MiscFlags*, ThreadFlags.EnableStackSwap, ThreadFlags.KernelStackResident, ThreadFlags*, ApcStateFill*, Timer*, WaitBlock*, WaitBlockFill5*, WaitIrql*, WaitBlockFill6*, WaitBlockFill7*, WaitBlockFill8*, WaitBlockFill9*, WaitBlockFill10*, WaitBlockFill11*, Process, UserAffinity, PreviousMode, Affinity, NpxState, SchedulerApcFill1, SchedulerApcFill2*, SchedulerApcFill3*, SchedulerApcFill4*, SchedulerApcFill5*, MutantListHead, PropagateBoostsEntry, IoSelfBoostsEntry, GlobalForegroundListEntry, ForegroundDpcStackListEntry, GlobalUpdateVpThreadPriorityListEntry, UpdateVpThreadPriorityDpcStackListEntry, RealtimePriorityFloor, ModeHistory*, KernelAbEntries, UserAbEntries
- `K2~K3` (10): QuantumTarget*, ThreadFlags.SharedReadyQueueAffinity, Priority*, UserIdealProcessor, BasePriority, IdealProcessor, SavedApcState*, QuantumReset, ThreadListEntry*, WobPriority
- `K3~P0` (9): CycleTime*, CurrentRunTime*, Running, WaitListEntry*, SwapListEntry*, ContextSwitches*, State, WaitTime*, AffinityVersion
- `P0~P1` (9): WaitRegister*, WaitBlockList, WaitBlockFill4*, WaitBlockCount, SavedApcStateFill, ReadOperationCount, OtherOperationCount*, ReadTransferCount, KernelWaitTime*
- `P1~P1a` (7): SystemCallNumber*, FirstArgument*, TrapFrame, WaitReason*, SchedulerSharedSystemSlot, ResourceIndex, SchedulerSharedSwappablePage
- `P1a~P1b` (6): ThreadFlags.AutoBoostEntriesExhausted, ReadyTime, WaitMode*, AdjustIncrement*, DecayBoost, UserWaitTime
- `P1b~P2` (6): MiscFlags.Alertable, WaitStatus, NextProcessor, NextProcessor.NextProcessorNumber, WriteOperationCount, WriteTransferCount
- `끝까지 0` (138): SListFaultAddress, ThreadLock, SchedulingGroup, Alerted, MiscFlags.ReadyTransition, MiscFlags.WaitNext, MiscFlags.SystemAffinityActive, MiscFlags.Reserved1, MiscFlags.ApcInterruptRequest, MiscFlags.QuantumEndMigrate, MiscFlags.SecureThread, MiscFlags.TimerActive, MiscFlags.SystemThread, MiscFlags.ProcessDetachActive, MiscFlags.Reserved2, MiscFlags.ScbReadyQueue, MiscFlags.Reserved3, MiscFlags.WaitNextClearWobPriorityFloor, MiscFlags.TimerSuspended, MiscFlags.SuspendedWaitMode, MiscFlags.CetUserShadowStack, MiscFlags.BypassProcessFreeze, MiscFlags.CetKernelShadowStack, MiscFlags.StateSaveAreaDecoupled, MiscFlags.Reserved, ThreadFlags.UserIdealProcessorFixed, ThreadFlags.IsolationWidth, ThreadFlags.AutoAlignment, ThreadFlags.DisableBoost, ThreadFlags.AlertedByThreadId, ThreadFlags.QuantumDonation, ThreadFlags.GuiThread, ThreadFlags.DisableQuantum, ThreadFlags.ChargeOnlySchedulingGroup, ThreadFlags.DeferPreemption, ThreadFlags.QueueDeferPreemption, ThreadFlags.ForceDeferSchedule, ThreadFlags.FreezeCount, ThreadFlags.TerminationApcRequest, ThreadFlags.TerminateRequestReason, ThreadFlags.ProcessStackCountDecremented, ThreadFlags.RestrictedGuiThread, ThreadFlags.VpBackingThread, ThreadFlags.EtwStackTraceCrimsonApcDisabled, ThreadFlags.EtwStackTraceApcInserted, Tag, SameThreadTransientFlags.CalloutActive, SameThreadTransientFlags.ReservedStackInUse, SameThreadTransientFlags.UserStackWalkActive, SameThreadTransientFlags.SameThreadTransientFlagsReserved, SameThreadTransientFlags, SpecCtrl.RunningNonRetpolineCode, SpecCtrl.SpecCtrlSpare, SpecCtrl, Queue, RelativeTimerBias, Spare13, KernelApcDisable, CombinedApcDisable, ThreadCounters, XStateSave, Win32Thread, Spare18, LastXStateSaveDebugInfo, ThreadFlags2, ThreadFlags2.DisableKasan, ThreadFlags2.AbContextSwitchState, ThreadFlags2.ThreadFlags2Reserved, BamQosLevel, HardwareFeedbackClass, PriorityDecrement, PriorityDecrement.ForegroundBoost, PriorityDecrement.UnusualBoost, QueueListEntry, NextProcessor.SharedReadyQueue, QueuePriority, UserAffinityPrimaryGroup, Spare24, Preempted, AdjustReason, AffinityPrimaryGroup, ApcStateIndex, Saturation, SListFaultCount, KernelTime, WaitPrcb, LegoData, CallbackNestingLevel, UserTime, AbWaitEntryCount, AbOwnedEntryCount, AbEntryCountValue, FreezeFlags, FreezeFlags.FreezeCount2, FreezeFlags.FreezeNormal, FreezeFlags.FreezeDeep, SecureThreadCookie, PriorityFloorCounts, PriorityFloorSummary, AbCompletedIoBoostCount, AbCompletedIoQoSBoostCount, KeReferenceCount, Spare6, ForegroundLossTime, InGlobalForegroundList, OtherTransferCount, QueuedScb, ThreadTimerDelay, Spare26, PpmPolicy, Spare27, TracingPrivate, SchedulerAssist, AbWaitObject, ReservedPreviousReadyTimeValue, InGlobalUpdateVpThreadPriorityList, SchedulerAssistPriorityFloor, KernelShadowStack, KernelShadowStackInitial, KernelShadowStackBase, KernelShadowStackLimit, ExtendedFeatureDisableMask, HgsFeedbackStartTime, HgsFeedbackCycles, HgsInvalidFeedbackCount, HgsLowerPerfClassFeedbackCount, HgsHigherPerfClassFeedbackCount, SystemAffinityTokenListHead, IptSaveArea, CoreIsolationReasons, BamQosLevelFromAssistPage, SecureCallCoreIsolationCount, SchedulerSharedOffset, KcsanThread, SchedulerAssistYieldCounter, SchedulerAssistYieldBoostCount, SchedulerAssistLastYieldBoostTime, Padding

**`_KTRAP_FRAME`** (모름 60 / 66)

- `K1~K2` (3): ExceptionActive, MxCsr, EFlags*
- `P1~P1a` (24): P1Home*, P2Home*, P3Home, PreviousMode, InterruptRetpolineState, FaultIndicator*, NmiMsrIbrs*, Rax*, R8*, R9*, R10*, R11*, GsBase, GsSwap, Xmm1*, FaultAddress*, ContextRecord*, NmiPreviousSpecCtrl*, NmiPreviousSpecCtrlPad*, Rbx*, Rdi*, Rsi*, ErrorCode*, ExceptionFrame*
- `P1a~P1b` (1): P4Home*
- `끝까지 0` (32): P5, PreviousIrql, Xmm0, Xmm2, Xmm3, Xmm4, Xmm5, Dr0, Dr1, Dr2, Dr3, Dr6, Dr7, ShadowStackFrame, Spare, DebugControl, LastBranchToRip, LastBranchFromRip, LastExceptionToRip, LastExceptionFromRip, SegDs, SegEs, SegFs, SegGs, TrapFrame, Rbp, Fill0, Logging, Fill1, Fill2, Fill3, Fill4

**`_HANDLE_TABLE`** (모름 17 / 17)

- `K1 이전` (10): NextHandleNeedingPool, TableCode, QuotaProcess, HandleTableList, UniqueProcessId*, Flags, Flags.Duplicated, Flags.RaiseUMExceptionOnInvalidHandleClose, FreeLists*, ActualEntry*
- `끝까지 0` (7): ExtraInfoPages, Flags.StrictFIFO, Flags.EnableHandleExceptions, Flags.Rundown, HandleContentionEvent, HandleTableLock, DebugInfo

**`_MMVAD`** (모름 9 / 9)

- `못 읽음` (9): Core, VadFlags2, Subsection, FirstPrototypePte, LastContiguousPte, ViewLinks, VadsProcess, u4, FileObject

**`_MMVAD_SHORT`** (모름 14 / 14)

- `못 읽음` (14): NextVad, ExtraCreateInfo, VadNode, StartingVpn, EndingVpn, StartingVpnHigh, EndingVpnHigh, CommitChargeHigh, SpareNT64VadUChar, ReferenceCount, PushLock, u, CommitCharge, u5

**`_TOKEN`** (모름 55 / 55)

- `K1 이전` (29): TokenSource, TokenId, AuthenticationId, ParentTokenId, ExpirationTime, TokenLock, ModifiedId, Privileges, AuditPolicy, SessionId, UserAndGroupCount, VariableLength, DynamicCharged, UserAndGroups, PrimaryGroup, DynamicPart, DefaultDacl, TokenType, TokenFlags, TokenInUse, IntegrityLevelIndex, MandatoryPolicy, LogonSession, OriginatingLogonSession, SidHash, pSecurityAttributes, IndexEntry, SessionObject, VariablePart
- `끝까지 0` (26): RestrictedSidCount, DynamicAvailable, DefaultOwnerIndex, RestrictedSids, ImpersonationLevel, RestrictedSidHash, Package, Capabilities, CapabilityCount, CapabilitiesHash, LowboxNumberEntry, LowboxHandlesEntry, pClaimAttributes, TrustLevelSid, TrustLinkedToken, IntegrityLevelSidValue, TokenSidValues, DiagnosticInfo, BnoIsolationHandlesEntry, TokenFlags2, TokenFlags2.IsInstaller, TokenFlags2.IsAgentic, AntiTamperSids, AntiTamperSidCount, AntiTamperSidHash, AntiTamperEntry

**`_SECTION`** (모름 0 / 9 — run47 전 9)

- (해결됨 → run47, sast/41 Q78b) 9칸 모두 3.20 으로. `K1 이전` (5): u1, SizeOfSection, u, .InitialPageProtection, .NoValidationNeeded / `끝까지 0` (4): SectionNode, StartingVpn, EndingVpn, .SessionId (조건부 안 탐 3 · 0 을 씀 1)

**`_CONTROL_AREA`** (모름 0 / 17 — run47 전 15)

- (해결됨 → run47) 15칸 모두 3.21 로. `K1 이전` (10): Segment, ListHead, AweContext, NumberOfSectionReferences, NumberOfPfnReferences* (짐작 — 범위 밖 길), NumberOfMappedViews (짐작 — 매핑), FilePointer*, u2, LockedPages, u3 / `끝까지 0` (5): u1, ControlAreaLock, ModifiedWriteCount (아무도 안 씀), WaitList, FileObjectLock

**`_SEGMENT`** (모름 0 / 11 — run47 전 10)

- (해결됨 → run47) 10칸 모두 3.22 로. `K1 이전` (8): ControlArea, TotalNumberOfPtes, SegmentFlags, SizeOfSegment, ExtendInfo, BasedAddress, u1, PrototypePte / `끝까지 0` (2): NumberOfCommittedPages (조건부), SegmentLock (0 을 씀)

**`_SECTION_IMAGE_INFORMATION`** (모름 0 / 26 — run47 전 10)

- (해결됨 → run47) 10칸 모두 3.23 으로. `K1 이전` (3): SubSystemVersion·OperatingSystemVersion (겹침), ImageFlags / `끝까지 0` (7): ZeroBits (아무도 안 씀), ImageFlags.ComPlusNativeReady, ImageFlags.ComPlusILOnly, ImageFlags.ImageMappedFlat, ImageFlags.BaseBelow4gb, ImageFlags.ComPlusPrefer32bit (조건부 5), ImageFlags.Reserved (아무도 안 씀 `[추측]`)

**`_FILE_OBJECT`** (모름 0 / 30 — run47 전 30)

- (해결됨 → run47) 30칸 모두 3.24 로. `K1 이전` (15): Type, Size, DeviceObject, FileName, Lock, Event*, IrpList, Flags* (nt + 다른 드라이버) · ReadAccess, SharedRead, SharedDelete (nt 함수, 부르는 쪽 드라이버) · Vpb, FsContext, FsContext2, SectionObjectPointer (다른 드라이버 ntfs.sys `[추측]`) / `끝까지 0` (15): RelatedFileObject, CurrentByteOffset, Waiters, IrpListLock (nt 가 0 을 씀) · WriteAccess, DeleteAccess, SharedWrite (`IoSetLinkShareAccess` 가 0 을 씀) · PrivateCacheMap, FinalStatus, LockOperation, DeletePending, Busy, LastLock, CompletionContext, FileObjectExtension (아무도 안 씀)

### 7.3 ntdll 전역 — 쓴 자리 모르는 것, 사진 구간별 (모두 417개. run46 전 434, run45 전 439)

- 이름은 PDB 공개 이름. 크기는 표 원본 `globals.tsv` (전부 `[추측]`, 다음 이름까지 거리 — 이웃 바이트가 섞일 수 있음). 문서에 이름조차 없는 것이 `.data` 343, `.mrdata` 39 (결과서 2번). `*` = 처음 값 뒤에 또 바뀜.
- `.data` 와 `.mrdata` 앞 3페이지는 K1 ~ P1 읽기 불가라 `≤P1a`·`끝까지 0` 은 P1a ~ P2 기준이다. `.mrdata` 끝 페이지 (`0x1ea000` ~) 는 K1 부터 읽혔다.

**ntdll `.data`** (모름 373 / 438)

- `P1a~P1b` (17): gBaseAddress, RtlpSearchPathMode, LdrpCreateActCtxLanguageW, LdrpReleaseActCtxW, LdrpManifestProberRoutine, LdrpThreadPool, LdrpStatistics, gSystemLocale, LoadAsDataCrits, CsrClientProcess, CsrInitOnceDone, CsrPortName, CsrProcessId, CsrPortMemoryRemoteDelta, CsrPortHandle, CsrPortHeap, CsrHeap
- `P1b~P2` (6): x86_cpu_enable_avx512, x86_cpu_enable_ssse3, x86_cpu_enable_simd, g_SymCryptFlags, g_SymCryptCpuFeaturesPresentCheck, g_SymCryptCpuid1
- `≤P1a` (105): _pctype, _pwctype, __initiallocinfo, __lconv_c, RtlpContinuationContextMachineFrameEntries, ?g_SbTableEntry_VerifyVersionInfoSwitch_RTL@@3U_SWITCHBRANCH_MODULE_TABLE_ENTRY@@A, ?g_SbTableEntry_HeapPaddingAndLFHSubsegmentCommitSwitch_RTL@@3U_SWITCHBRANCH_MODULE_TABLE_ENTRY@@A, RtlpSecMemListHead, RtlpLockedSectionList, RtlpDebugPageHeapTable, NormalizationListHead, TppPoolpList*, TppCleanupGroupMemberpNoPoolList, LdrpUserDllDirectories, LdrpSnapsLock, LdrpSnapsLock_DEBUG, LdrpLoaderLock_DEBUG, LdrpLoaderLock*, LdrpDllNotificationList, RtlCriticalSectionList*, LdrpDllNotificationLock_DEBUG, LdrpDllNotificationLock, ?g_SbCachedModuleTable_RTL@@3U_SWITCHBRANCH_CACHED_MODULE_TABLE@@A, ?dazSupported@?1??_control87@@9@9, __isa_info, _matherr_flag, __mb_cur_max, ShowSnaps, g_sqmSessionHandle, g_SxsMaxDeadActivationContexts, GlobalRtlNlsState, NlsAnsiCodePage, Utf8TableInfo, PossibleDeadlockReportThreshold, RtlpPtrTree, RtlpLargestLfhBlock, RtlpOptimalFlushMethod, RtlpClFlushSize, RtlpIsFlushRequired, __ft_g_token, TelemetryEventThrottle, RtlpHeapFailureInfo, g_SymCryptCpuFeaturesNotPresent*, RtlpDiskSpeedPolicy, RtlpUnloadEventTraceExSize, RtlpUnloadEventTraceExNumber, LdrpQueryProcessModuleInformationLoopDetectorCount, ModuleNamePlaceholderBuffer, LdrpPolicyBits, SbDynamicContextDllCount*, RtlpFlsContext*, RtlCriticalSectionDebugSList*, LdrpThunkSignature*, LdrpInitOnceLoadAsDataCrits*, NlsMbOemCodePageTag, NlsMbCodePageTag*, RtlpDisableHeapLookaside, RtlpIsDrainRequired, RtlpRandomExInit, RtlpHpStackTraceProviderHandle, RtlpLowFragHeapGlobalFlags, EtwpRegistrationTable*, EtwpRegisterTpInitOnce, TppMaximumGroups, TppNumberNodes, TppPoolpGlobalPool, SecurityCookieInitCount, g_pShimmedModuleList, Feature_Servicing_LoaderSnapsBuffer__private_reporting, SpecialMachineFramesInitCount, RtlpCurDirRef, ?CookieValue@?1??RtlpGetCookieValue@@9@9, LdrpActiveThreadCount, LdrpTlsHeap, LdrpStaticTlsBitmapVector*, LdrpActualBitmapSize, LdrpTlsBitmap, RtlpTimeoutDisable, RtlpTimeout, LdrpSnapsUnicodeString2*, LdrpSnapsUnicodeString*, RtlpEnvironmentHashTable, RtlpUnhandledExceptionFilter*, LdrpHotPatchInitCompleteEvent, LdrpSchedulerSharedDataListHead, g_hPrivLoggerNotificationProvider, PrivateLoggerNotificationEntry, EtwpRegistrationCount*, EtwpRegistrationSequence*, RtlpLowFragHeapRandomData*, RtlpRandomExConstantVector*, RtlpRandomExAuxVarY*, RtlpWaitOnAddressSpinCycleCount, RtlpFcProcessManager*, RtlProcessorFeaturesBitMap, ConditionVariableSpinCycleCount, g_hUserDiagnosticProvider, RtlpHypervisorSharedUserVa, RtlpStaticDebugInfo*, SRWLockSpinCycleCount, LdrpEnclaveList, LdrpEnclaveListLock, Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_featureState, Feature_KVS_BugFixes_55830752__private_featureState, Feature_Servicing_LoaderSnapsBuffer__private_featureState
- `끝까지 0` (245): RtlpSrwLockAllowImplicitUpgrade, RuntimeGlobal, __@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUmgwooUwzbglmzUlyquivUznwGEUkxsOlyq@ntdll, AVrfpHandleTraces, AVrfpDebug, g_disableEscalation, g_werEscalationData, g_SxsCurrentDeadActivationContexts, g_SxsTrackReleaseStacks, g_SxsKeepActivationContextsAlive, g_SxsFreeActivationContexts, g_SxsLiveActivationContexts, EtwpFreeRegistrationList, RtlpCaptureActivationContextActivationStacks, Feature_ProjectTessera_CreateProcessInSandbox__private_reporting, g_wil_details_subscribeFeatureStateCacheToConfigurationChanges, Feature_UxAccOptimization__private_reporting, Feature_Standalone_26_08_NonSec__private_reporting, Feature_Standalone_26_06_NonSec__private_reporting, Feature_DriverSetup_ReofferFingerprinting_v2__private_reporting, Feature_LearningModeTraceService__private_reporting, Feature_SettingsDel__private_reporting, Feature_Tessera_202605Retail__private_reporting, Feature_Standalone_Future__private_reporting, g_wil_details_ensureSubscribedToFeatureConfigurationChanges, Feature_TestLoc02__private_reporting, Feature_ImplVal__private_reporting, g_wil_details_pfnFeatureLoggingHook, Feature_TestLoc03__private_reporting, LdrpInvertedFunctionTableSRWLock, MuiCacheSWRLock, NlsEmptyLeadByteInfoTable, Feature_KVS_BugFixes_55830752__private_reporting, Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_reporting, RtlpCsVerifyDoNotBreak, PossibleDeadlockReportCounter, RtlpCriticalSectionVerifier, RtlpLockedMemoryBlockLookasideCount, RtlpMemoryBlockLookasideLock, RtlpMemoryZoneLock, RtlpLockedMemoryZoneCount, Feature_GetPreferredThreadLanguage_Watson_AccessViolation_BugFix__private_reporting, InitRegistryInfoCritSect, g_RegInfo, RtlpGlobalTagHeap, RtlpPropStoreLock, RtlpPtrTreeLock, RtlpFreezeTimeBias, Feature_FasterCrc32__private_reporting, RtlpHpHeapFeatures, RtlpSecMemLock, RtlpLDNumBlocks, RtlpTempBlocks, RtlpLeakHeapAddress, RtlpLeaksCount, RtlpCrtHeapAddress, RtlpPreviousStartAddress, RtlpShutdownProcessFlags, RtlpLDPreviousPage, RtlpLDCrtPage, RtlpValidateHeapHdrsEnable, __ft_g_isTestReadyMedia, __ft_g_init_once, g_dwLastErrorToBreakOn, g_isErrorOriginProviderEnabled, gCustomCultureRegKey, gLocaleDataRegKey, gLocaleTables, pTblPtrs, TrkContext, RtlpExceptionLog2, RtlpLockedSectionListLock, RtlpProtectedPoliciesSRWLock, RtlpPerformanceCounterFrequency, RtlpCapChkTelemetryRunOnceCtx, RtlpHpNullGUID, RtlpHpTagInitVar, RtlpHeapStackTraceLog, RtlpHpStackTraceLogInstance, RtlpHpStackTrackingContext, RtlpHpAppCompatFlags, g_isUcpListInitialized, UcpExclusiveLock, UcpInitOnUnexpectedCodePathOnce, UcpMaxTriggeredNodeExcceded, UcpLockOwningThreadHandle, UcIsTraceLoggingInitialized, RtlpHpEnvThreadSuspendOwnershipLock, EtwpGuidEntryTable, EtwpProvLock, EtwpPrivSessionLock, TppPoolpSerializedPoolLock, TppPoolpGlobalPoolStackSize, TppPoolpListLock, TppPoolpSerializedPool, TppPoolpGlobalPoolLock, TppPoolpGlobalPoolMaxThreadsOverride, TppPoolpGlobalPoolMaxThreads, TppPoolpDefaultPoolCpuSetLock, TppPoolpDefaultPoolCpuSetCount, TppHeapTag, TppCleanupGroupMemberpNoPoolListLock, RtlpTpIoRegistered, RtlpTpIoTreeLock, RtlpTpIoRegisteredLock, RtlpTpIoTree, RtlpTpIoDllNotificationCookie, RtlpDiskSpeedInit, g_MicrosoftTelemetryAssertLock, g_MicrosoftTelemetryAssertsTriggeredList, LdrpPathLock, RtlpCachedPathLock, LdrpDefaultDllDirectories, LdrpDllDirectoryLock, LdrpAppPackagesPathVersion, RtlpUnloadEventTraceEx, LdrpUnloadIndex, LdrpUnloadIndexEx, g_ShimsEnabled, LdrpShouldCreateStackTraceDb, g_ShimsLoading, LdrpIllegalCWDDevices, Feature_TreatDosDevicePathsAsNormalFiles__private_reporting, LdrpForkActiveLock, LdrpForkConditionVariable, LdrpVchLock, LdrpVehLock, RtlCriticalSectionLock, LdrpModuleDatatableLock, LdrpDelayedTlsReclaimTable, LdrpCODScenarioTriggered, VsmEnclaveTelemetryInitRunOnce, LibLoaderTelemetryInitRunOnce, CastGuardTelemetryInitRunOnce, LdrpCODScenarioLock, CastGuardProvidersRegistered, CentennialIssueTracker_InitializedState, VSMEnclaveProvidersRegistered, Feature_PackagedComElevationSupport_v2__private_reporting, LdrpRedirectionTree, SbpContextLock, __@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUzkkxlnkzgUxzxsvUrmgviuzxvUorynrmUlyquivUznwGEUxlnkzgxzxsvkOlyq@compatcachemin, __@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUzkkxlnkzgUhdrgxsyzxpUxlmgvcgUolzwviUlyquivUznwGEUkivxlnkOlyq@SwitchContextLoader, LdrpCorExeMainRoutine, LdrpRedirectionByFunctionCalloutFunc, LdrpRedirectionModule, LdrpRedirectionCalloutFunc, LdrpPotentialTlsLeaks, RtlFailedCriticalDebugAllocations, LdrpFailureData, LdrpFatalHardErrorCount, LdrpSchedulerSharedDataListHeadLock, LdrpEnforceIntegrityContinuity, LdrpAuditIntegrityContinuity, LdrpSuppressedExportOverrideListPtr, UseCOR, g_pShimmedModuleListLength, g_LdrBreakOnLdrpInitializeProcessFailure, g_pShimEngineModule, LdrpUseImpersonatedDeviceMap, RtlpUnloadEventTrace, LdrpLoaderLockAcquisitionCount, RtlpSearchPath, RtlpDllSearchPathWithOptions, RtlpExeSearchPath, LdrpAppPackagesPath, LdrpOriginalAppPackagesPath, __@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUkilxhmzkUmgwooUlyquivUznwGEUkivxlnkOlyq@pssntlib, __@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUgsivzwklloUxlnkzgUwzbglmzUlyquivUznwGEUgkxlnkzgOlyq@tpcompat, __@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUgsivzwklloUmgwooUwzbglmzUlyquivUznwGEUgkkOlyq@tp, TppPoolpDefaultPoolCpuSets, __@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUvgdUmgwooUwzbglmzUlyquivUznwGEUvgdmgwookOlyq@etwntdll, EtwpGlobalSequence, EtwpLoggerArray, EtwpMaxLoggers, RtlpHpEnvEnableSimulatedLargePageCommit, UcpTriggeredList, UcpTriggeredNodeCount, NormalizationListLock, RtlpGetCurrentProcessorNumberHow, RtlpHpTagContext, RtlpDphInitFailureFlags, RtlpRcuState, RtlpCtPublishInProgress, RtlpStackTraceDatabase, RtlpExceptionLogSize, RtlpExceptionLogCount, RtlProcessorFeaturesBuffer, __ft_g_api_info, __ft_g_feature_store_change_registration_handle, RtlpValidateHeapTagsEnable, RtlpHeapStopOn, RtlpHeapInvalidBreakPoint, RtlpLeakHeap, RtlpProcessMemoryMap, RtlpBusyList, RtlpLeakList, RtlSecureMemorySystemRangeStart, SmtDelayedConfiguration, RtlpPropStoreEntriesTotalCount, RtlpPropStoreEntries, RtlpPropStoreEntriesActiveCount, RegistryInfoCritSect, RtlpForceCSDebugInfoCreation, RtlpRaiseExceptionOnPossibleDeadlock, RtlpForceCSToUseEvents, RtlpModernAppKey, LoadAsDataTableBlockCount, LoadAsDataTable, LoadAsDataTableCount, AlternateResourceModules, AlternateResourceModuleCount, AltResMemBlockCount, gMUICacheType, RtlpDynamicFunctionTableLock, TrustedAppsRemoteBreakinRoutine, AVrfpVerifierDllsString, AVrfpPageHeapPreviousStopData, AVrfpVerifierLock, AVrfpEnabledSystemWide, AVrfpVerifierProvidersList, AVrfpVerifierFlags, AvrfpLoaderEntry, AVrfpCurrentUserKeyPath, AVrfpVerifierStopMessageFunction, AVrfpPageHeapStopData, CsrServerApiRoutine, CsrPortBaseTag, HotPatchSpareGlobals, Feature_UxAccOptimization__private_featureState, Feature_Standalone_26_06_NonSec__private_featureState, Feature_TestLoc03__private_featureState, Feature_DriverSetup_ReofferFingerprinting_v2__private_featureState, Feature_ProjectTessera_CreateProcessInSandbox__private_featureState, Feature_LearningModeTraceService__private_featureState, Feature_Standalone_Future__private_featureState, Feature_TestLoc02__private_featureState, Feature_Standalone_26_08_NonSec__private_featureState, Feature_SettingsDel__private_featureState, Feature_Tessera_202605Retail__private_featureState, Feature_ImplVal__private_featureState, Feature_GetPreferredThreadLanguage_Watson_AccessViolation_BugFix__private_featureState, Feature_FasterCrc32__private_featureState, Feature_TreatDosDevicePathsAsNormalFiles__private_featureState, Feature_PackagedComElevationSupport_v2__private_featureState

**ntdll `.mrdata`** (모름 40 / 50)

- `K1 이전` (2): LdrpVectorHandlerList, __security_cookie_complement
- `≤P1a` (9): __guard_check_icall_fptr, LdrpMrdataSize, LdrpMrdataBase, RtlpDynamicFunctionTable*, RtlpDynamicFunctionTableTreeMin*, RtlpDynamicFunctionTableTreeMax*, NtUserPfn, RtlpUnwindHistoryTable, LdrpInvertedFunctionTables*
- `끝까지 0` (29): LdrpMrdataHeap, LdrpMrdataHeapUnprotected, AvrfpAPILookupCallbacksEnabled, AVrfpEnabled, RtlGuardAllowSuppressedCalls, AvrfpAPILookupCallbackRoutine, RtlpDynamicCallbackTableTreeMax, RtlpDynamicCallbackTableTreeMin, RtlpProtectedPolicies, RtlpProtectedPoliciesActiveCount, RtlpProtectedPoliciesTotalCount, g_pfnSE_GetProcAddressForCaller, g_pfnApphelpQueryModSettingsAllocProc, g_pfnSE_DllUnloaded, g_pfnSE_ProcessDying, g_pfnSE_ShimDllLoaded, g_pfnApphelpCheckModuleProc, Wow64ApcRoutine, g_pfnSE_InitializeEngine, Wow64PrepareForException, g_pfnSE_LdrResolveDllName, g_pfnSE_InstallBeforeInit, g_LdrpWow64PrepareForDebuggerAttach, g_pfnSE_LdrEntryRemoved, g_pfnSE_DllLoaded, g_LdrpWow64SuspendLocalProcess, g_LdrpWow64SuspendLocalThread, LdrpMrdataUnprotected, LdrpUnalignedUserCallTargetBitMap

- (해결됨 → run45, sast/39) 쓴 자리가 나와 3.8·3.9 로 옮긴 5개: `.data` `LdrpMrdataLock` (끝까지 0) ← `LdrInitializeMrdata +0x23` · `.mrdata` `LdrpMaximumUserModeAddress`·`LdrpAllocationGranularity` (`≤P1a`) ← `LdrInitializeMrdata +0x52`·`+0x46` · `LdrpGuardCheckIcallNoESFptr` (`K1 이전`)·`LdrpGuardDispatchIcallNoESFptr` (`≤P1a`) ← `LdrpInitializeCfgScpHelpers +0x6b`·`+0x79` (test.exe 에선 안 탄 갈래 `[추측]`, 7.5).

- (해결됨 → run46, sast/40 — Q78a) 프로세스 힙 구간에서 쓴 자리가 나와 3.8 로 옮긴 17개: `≤P1a` 11 — `RtlpHpHeapGlobals`·`?RtlpHpEnvFlsContext@@3U_RTLP_FLS_CONTEXT@@A`·`RtlpHeapKey`·`RtlpHpHeapManager`·`RtlpLFHKey`·`RtlpHpLfhPerfFlags`·`RtlpHpHeapVaCallbacksRegistrar`·`RtlpAffinityState`·`RtlpPebHeapListStaticBuffer`·`RtlpProcessHeaps`·`RtlpProcessHeapsLock` / `끝까지 0` 6 — `RtlpDisableBreakOnFailureCookie`·`RtlpHeapErrorHandlerThreshold`·`NtdllBaseTag`·`LdrpHeapPartitionName`·`RtlpHpEnvHandle`·`RtlpDefaultHeapDebuggingOptions`. 쓰는 줄은 모두 `RtlInitializeHeapManager` 아래 또는 `LdrpInitializeProcessHeap → RtlpCreateHeap` 아래 (`+0xb7d` `NtdllBaseTag` 만 `LdrpInitializeProcess` 직접).
- (run46) 이 조각이 **보고도 닫지 않은 것** (목록에 남김, 7.5): `RtlpLowFragHeapGlobalFlags`·`RtlpLowFragHeapRandomData*`·`RtlpDisableHeapLookaside`·`RtlpLargestLfhBlock`·`RtlpHeapFailureInfo`·`RtlpDebugPageHeapTable`·`RtlpSecMemListHead` (`≤P1a`), `RtlpHpHeapFeatures`·`RtlpHpAppCompatFlags`·`RtlpGlobalTagHeap`·`g_RegInfo` (`끝까지 0`).

**ntdll `.00cfg`** (모름 4 / 5)

- `못 읽음` (4): __guard_xfg_check_icall_fptr, __guard_xfg_dispatch_icall_fptr, __guard_xfg_table_dispatch_icall_fptr, __castguard_check_failure_os_handled_fptr

### 7.4 짐작이라 다시 볼 것 (3장에 있지만 칸 단위 근거가 약함)

- `_RTL_USER_PROCESS_PARAMETERS` 22칸 — "블록 통째 복사" 로만 앎. 사진 ① 값 = 부모 블록 값인지 (Q77).
  - (run44) K1 에 이미 값 8 · 끝까지 0 13 · `EnvironmentVersion` 만 P1~P1a (옮긴 블록에서 3). 부모 쪽 블록은 안 읽어서 "부모 값 그대로" 인지는 아직 `[추측]`.
- `_PEB` 12칸 — `MmCreatePeb` 가 범위 (`+0xb8 ~ +0xe0`, `+0x118 ~ +0x124`) 로만 말함. 칸마다 쓰는 줄 (Q78).
  - (run44) 9칸 `K1 이전`, 3칸 (`NtGlobalFlag`·`OSMinorVersion`·`OSCSDVersion`) 끝까지 0 — 시점은 짐작과 맞음, 칸마다 쓰는 줄은 그대로 Q78.
- `_EPROCESS` `Pcb`·`Token`·`ObjectTable` — 부르는 줄만 앎 (callee 본문 안 봄). `ThreadListHead`·`ThreadWasActive` 쓰는 줄 오프셋, `ProcessLock` 잡는 쪽 (flow.md 4장 2.1 `[정적 + 간접]`).
  - (run44) `ProcessLock` K1·K2 1 → K3 0 `[관찰]` — 잡힌 채 `PspAllocateThread` 앞에 와서 K2~K3 에 풀린다. `ThreadListHead`·`ThreadWasActive` 는 `K2~K3` (쓰는 줄 오프셋은 그대로 Q78).
- `_KTHREAD.Teb` (칸 이름 `[추측]`), `_KTRAP_FRAME.SegCs`·`SegSs` (옮기는 칸 목록에 안 나옴), ~~`_CONTROL_AREA.u` (진짜 컨트롤 영역 칸 아님)~~ → (해결됨 → run47) 진짜 칸 쓰는 줄 `MiBuildImageControlArea +0x9e · +0x222 ~ +0x238`, `MiSectionCreated +0xa6` — 정확 (3.21).
  - (run44) `KTHREAD.Teb` = TEB 주소, `SegCs 0x33`·`SegSs 0x2b` `[관찰]` — 값은 확인, 쓰는 줄은 여전히 짐작.
- `_LDR_DATA_TABLE_ENTRY` exe `ObsoleteLoadCount`·`LoadReason`, ntdll `HotPatchState`, `_LDR_DDAG_NODE` ntdll `LoadCount` — 문서의 `LoadCount = -1` 이 PDB 로는 노드 칸 (`+0x18`) 이라 이름 맞추기가 짐작.
  - (run44) 값: exe `ObsoleteLoadCount` `0xffff`·`LoadReason` 4, ntdll `HotPatchState` 0, 노드 `LoadCount` exe·ntdll `0xffffffff` (= -1) `[관찰]`.
- 전역: `LdrpDebugFlags`, `RtlpDllSearchPath`, `g_LdrpWow64LdrpInitialize`, `LdrpKnownDllDirectoryHandle`, `__security_cookie` (이름으로만), **`__guard_dispatch_icall_fptr`** (쓰는 코드 줄 없음 — 짐작으로 셌지만 주인은 사실상 모름).
  - (run44) `LdrpKnownDllDirectoryHandle` `0x4c` = Directory `KnownDlls`, `__security_cookie` K1 파일 값 → `P1~P1a` 새 값, `RtlpDllSearchPath` `P1a~P1b`, `LdrpDebugFlags` `≤P1a`, `g_LdrpWow64LdrpInitialize` 끝까지 0*, `__guard_dispatch_icall_fptr` 읽기 불가 `[관찰]`.

### 7.5 모르는 칸인데 문서에 실마리가 있는 것 (3장으로 안 옮김 — 결과서 숫자를 그대로 두려고)

- TEB 칸 다수 ← `MmCreateTeb` 의 TEB 대입 약 47줄 (64비트 갈래 + WOW64 갈래, 결과서·run43). 칸별 표는 Q78.
- `_LDR_DATA_TABLE_ENTRY` `ProcessAttachFailed` (`Flags` 비트) ← `LdrpInitializeNode +0x29e` (진입점 실패 길, flow.md 2.7 run27).
- `_LDR_DATA_TABLE_ENTRY.EntryPointActivationContext` (+0x88) ← `LdrpFindDllActivationContext` (`LdrpMapAndSnapDependency +0x63`, flow.md 2.7 "run22 가 넣는 칸").
- `_TEB` `LoadOwner` (`SameTebFlags` `0x1000`) ← `LdrpDrainWorkQueue +0xd3` 켬, `LdrpDropLastInProgressCount` 끔 (flow.md 2.6 run24). (run44) P1b 1 · P2 0 `[관찰]` — 실마리와 맞음.
- `_PEB.ApiSetMap` (+0x68) ← `MmMapApiSetView` (`PspSetupUserProcessAddressSpace +0x255`) 가 올리는 뷰 (run32). 칸 쓰는 줄은 모름. (run44) 값 `25996cd0000` 이 K1 에 이미 있음 → 커널 몫 `[관찰]`.
- PEB 칸 후보 ← `KeCopyXfdMaskToPeb` (`+0x227`), `PspUpdatePebForAffinityChange` (`+0x234`) — 이름으로 PEB 에 쓸 것 `[추측]` (flow.md 2.1 run32).
- `_PEB.PostProcessInitRoutine` (+0x230) — 읽는 곳 `LdrpInitializeProcess +0x1d16` 뿐, 넣는 쪽 모름 (run19, 동적 Q48). (run44) 끝까지 0 `[관찰]` — test.exe 길에선 아무도 안 넣음 (또는 0 을 넣음).
- `.mrdata` `LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected`·`LdrpMrdataUnprotected` ← ~~`LdrInitializeMrdata` (`_LdrpInitialize +0x14a`, 109바이트, 본문 안 봄) 후보 (결과서 공백 4)~~ → (run45 `[정적]`, sast/39) `LdrInitializeMrdata` 본문은 이 다섯을 **안 쓴다** (쓰는 건 `LdrpMrdataLock`·`LdrpAllocationGranularity`·`LdrpMaximumUserModeAddress` 셋 → 3.8·3.9). 다섯의 쓴 자리는 그대로 모름 → Q78. (run44) `LdrpMrdataBase` = `.mrdata` 시작, `LdrpMrdataSize` `0x35b8` 은 `≤P1a`, `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected`·`LdrpMrdataUnprotected` 은 끝까지 0* `[관찰]`.
- (run45) PEB `ActiveCodePage`·`OemCodePage` — 쓰는 줄 (`LdrpInitializeNlsInfo`, ②) 이 나와 3.1 로 옮겼지만, 사진은 8장 모두 `0x3b5` 로 **K1 전에 이미 값**이 있었다. K1 전에 처음 쓴 쪽은 모른다 `[공백]` — `MmCreatePeb` 후보 `[추측]` (같은 `K1 이전` PEB 칸들이 거기서 나옴). → Q78, 순서는 동적 Q85.
- (run45) CFG 도우미 두 칸 `LdrpGuardCheckIcallNoESFptr`·`LdrpGuardDispatchIcallNoESFptr` — 쓰는 줄 (`LdrpInitializeCfgScpHelpers`) 이 나와 3.9 로 옮겼지만, **사진 값은 그 줄이 넣을 함수가 아니다** (Check = ntdll+`0xfac60` 빈 함수, Dispatch = ntdll+`0x166480` `_guard_dispatch_icall`). test.exe 에선 그 갈래를 안 탄 것 `[추측]` (조건: CFG 강제·헤더·LoadConfig 플래그). 그러면 값의 주인은 파일 처음 값 또는 다른 쓰는 곳이다: Check 는 `.mrdata` 끝 페이지라 K1 부터 읽혔고 (파일 값 `[추측]`), Dispatch 는 앞 페이지라 `≤P1a` → Q80 (파일 처음 값과 P1a 값 비교) 으로 가려진다. flow.md 4장 2.4.
- (run45) 매개변수 `EnvironmentVersion` 0 → 3 중 +1 은 `RtlpInitEnvironmentBlock +0x86` (3.2). 나머지 +2 는 모름 — `RtlpInitEnvironmentBlock +0xa0 RtlpSetupEnvironmentHashTable` 쪽 `[추측]` → Q78.
- `LdrpProcessInitialized`·`LdrpHotPatchInitialized` 의 1 → 2 ← `LdrpInitializationComplete` (`_LdrpInitialize` ret `+0x252`, `LdrpInitializeInternal` ret `+0xdf`) 쪽으로 보임 `[추측]` (flow.md 2.4 "상태 1 → 2"). 칸 주소를 넘기는 줄 확인은 Q78. (run44) `LdrpHotPatchInitialized` 는 P1a 에 이미 2, `LdrpProcessInitialized` 는 P1a·P1b 1 → P2 2 `[관찰]` — 둘 다 그림의 호출 순서 (`LdrpInitializeInternal` `(2) +0xdf` 가 `(3) +0x5a` 앞, `_LdrpInitialize` `+0x252` 는 `LdrpInitializeProcess` 뒤) 와 맞다.
- 락·이벤트처럼 **API 에 주소를 넘겨 쓰는 전역**은 `_refs` WRITE 줄로 안 잡힌다 (예: `LdrpWorkQueueLock` 은 복제 길 WRITE 줄만 있었고, 만들기·잡기는 flow.md 2.6 에서 찾아 더함). `.data` 모름 391 중에도 이런 것이 있을 것 `[추측]` → Q78 은 `_refs` DATA 줄 (`LEA`) 도 같이 본다.

- (run46, sast/40) **이름 없는 자리가 앞 이름의 짐작 크기 안에 묻혀 있었다.** 3.8 "목록 밖 자리" 4줄 — `0x1ce270~0x1ce280` (GC 타이머) 는 `LdrpThunkSignature` (짐작 1020B) 안, `0x1ce430` (IFEO 바탕 키 핸들) 은 `LdrpInitOnceLoadAsDataCrits` (짐작 16B) 의 `+0x8`, `0x1ce6e8`·`0x1ce6f0` (사용자 공간 최대 주소·`0x10000`) 은 `RtlpDisableHeapLookaside` (짐작 24B) 의 `+0x8`·`+0x10`, `0x1d21a0~0x1d21d8` 은 `RtlpHpHeapManager` (짐작 14560B) 끝쪽. 그래서 `LdrpThunkSignature*`·`LdrpInitOnceLoadAsDataCrits*` 가 7.3 에서 `≤P1a` 로 보인 까닭이 이 자리일 수 있다 `[추측]` — 이름 자기 칸 (맨 앞 바이트) 의 쓴 자리는 그대로 모름 → Q78·Q80. 결과서 도구 메모: `.asm` 전역 주석은 "가장 가까운 앞 이름 + 칸" 이라 별개 변수가 앞 이름의 일부처럼 보인다.
- (run46, sast/40) **조건부로만 쓰는 힙 전역** — 쓰는 줄은 결과서에 있지만 결과서가 "Q78a 로 닫힌 칸" 으로 안 세서 3장에 안 옮겼다 (test.exe 는 모두 조건이 안 맞아 안 씀, 사진 값과 맞음):
  - `RtlpHpHeapFeatures` (`0x1ce6d0`) ← `RtlInitializeHeapManager +0x31` (`RtlpHpOptIntoSegmentHeap` 반환 ≠ 0 일 때 `\|= 1`). 사진 0.
  - `RtlpHpAppCompatFlags` (`0x1ce8f8`) ← `RtlpHpSetLowFragHeapGlobalFlags +0x85` · `RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit +0xf`. 사진 0.
  - `RtlpGlobalTagHeap` (`0x1ce6a8`) ← `RtlCreateTagHeap +0x97` (`NtGlobalFlag & 0x800` 일 때). 사진 0.
  - `g_RegInfo` `+0x8` (`0x1ce698`) ← `RtlInitializeHeapManager +0x5b` (`SbSelectProcedure` 반환, ①-b ≠ 0 일 때만). 사진 0.
  - `RtlpLowFragHeapGlobalFlags` (`0x1d2218`) ← 이 구간은 `RtlInitializeHeapManager +0x17f` (스위트 비트 16 일 때만). 구간 앞 레지스트리·IFEO 쪽 (`RtlpHpApplyGlobalSegmentHeapConfigurations`, `RtlSetGlobalHeapFeatures`). P1a 값 `1` 의 비트 0 은 LFH 만들기 `RtlpCreateLowFragHeap +0x24a ~ +0x256` (블록 나눠 주기 쪽 — 7.6.1 범위 밖).
  - `RtlpLargestLfhBlock` (`0x1cd0a8`) ← `RtlInitializeHeapManager +0x186` (스위트 비트 16 일 때 `0x400`). 사진 `0x4000` = 파일 처음 값.
  - `RtlpDisableHeapLookaside` (`0x1ce6e0`) ← IFEO `DisableHeapLookaside` 읽기 (`LdrpInitializeExecutionOptions +0x127` 가 칸 주소를 넘김). test.exe 는 IFEO 를 건너뛰어 안 씀, 사진 0.
  - `RtlpLowFragHeapRandomData*` (`0x1d3420`) ← `RtlpInitializeLfhRandomDataArray` (LFH 켜질 때, `RtlpCreateLowFragHeap +0x25c`) — 범위 밖.
  - 읽기만: `RtlpDebugPageHeapTable` (`RtlpCreateHeap +0x116`), `RtlpSecMemListHead` (`RtlpSecMemFreeVirtualMemory +0x47`, 정적으로 자기를 가리키는 빈 목록), `RtlpHeapFailureInfo` (정적 처음 값, 이 구간은 주소만 `RtlpHpHeapGlobals+0x10` 에).
  - 3장으로 옮길지 (경로를 끝까지 잇고 "쓴 자리 아는 것" 으로 셀지) 는 나머지 Q78 에서.

### 7.6 칸 표가 아직 없는 짐

- PDB 에 있는 이웃 타입 (표 안 만듦) — **(run45 확정 `[정적]`, sast/39 3.1: 정방향 선언이 아닌 정의 레코드)**:
  - nt PDB: `_HANDLE_TABLE_ENTRY` (0x10) · `_OBJECT_HEADER` (0x38) · `_SUBSECTION` (0x38) · `_MSUBSECTION` (0x98) · `_SECTION_OBJECT_POINTERS` (0x18) · `_PS_NTDLL_EXPORTS` (0xa8 — (run48) 칸 표는 7.6.2) · `_CONTEXT` (0x4d0) · `_MI_SECTION_IMAGE_INFORMATION` (0x50) · `_KEXCEPTION_FRAME` (0x140) · `_MMSUPPORT_FULL` (0x140) · `_MMPTE` (8) · `_MMPFN` (0x30) · `_OBJECT_DIRECTORY` (0x158) · `_ETW_REG_ENTRY` (0x78) · `_NLS_DATA_BLOCK` (0x18) · `_KPRCB` (0xcf00)
  - ntdll PDB: `_HEAP` (0x2c8) · `_HEAP_ENTRY` (0x10) · `_HEAP_SEGMENT` (0x70) · `_HEAP_LOCAL_DATA` (0x30) · `_ACTIVATION_CONTEXT_STACK` (0x28) · `_RTL_ACTIVATION_CONTEXT_STACK_FRAME` (0x18) · `_TP_POOL` (0x1d8) · `_CPTABLEINFO` (0x40) · `_ACTIVATION_CONTEXT_DATA` (0x20) · `_CURDIR`
- PDB 에 **없는** 타입 (오프셋으로만 적어야 함): `LdrSystemDllInitBlock` 블록 (0x128) · `InitialTeb` (`PspSetupUserStack` 스택 정보) · `PS_CREATE_INFO` · `PS_ATTRIBUTE_LIST` · 간선 칸 0x20바이트 (run23) · `_LDRP_LOAD_CONTEXT` 칸 정보 (run22) · `_KCONTINUE_ARGUMENT` (run29) · `_SWITCHBRANCH_*` · `_RTLP_FLS_CONTEXT` · **`_EPROCESS_QUOTA_BLOCK`** (run45) · `_PS_SYSTEM_DLL_INFO` (run48 — 함수 서명에 이름만, 칸 정의 없음. 칸 표는 오프셋으로 7.6.2). `_LDRP_LOAD_CONTEXT`·`_EPROCESS_QUOTA_BLOCK` 은 두 PDB 모두 **정방향 선언만** 있다 (run45 확정 `[정적]`).
- 칸이 아니라 구역인 짐: 유저 스택 위 **맡긴 문맥** (`_CONTEXT`, `PspInitializeThunkContext +0x285 RtlCopyContext`), 환경 블록 (`Environment` +0x80, 크기 `EnvironmentSize`), 매개변수 문자열들, API set 지도 뷰, leap second 뷰, `PEB.SharedData`·`TlsBitmap`·`ActivationContextData` 가 가리키는 것, TEB `ThreadLocalStoragePointer` (+0x58) 배열, 유저 스택, 프로세스 힙 (로더 항목이 사는 곳 — 결과서 Q77 표). 덤프 대상·주소 얻는 칸은 결과서 "Q77 에서 떠야 할 것".
- 커널 쪽 별칭 주소 (KUSD), 섹션·VAD·핸들 표 항목은 커널 객체라 칸 표 + 사진이 같이 필요하다.
- (run47, sast/41) exe 섹션 묶음 옆 짐 — **기록만** (결과서 제안, C1 과 같은 종류): `_SECTION_OBJECT_POINTERS` (0x18, 파일 시스템 메모리. `ImageSectionObject` 칸은 nt 가 씀 — 3.24 `SectionObjectPointer` 꼬리표) · `_MI_IMAGE_ADDITIONAL_INFO` (0x48, `CA.u2.ImageInfoRef` 가 가리키는 컨트롤 영역 할당 꼬리) · `_MI_EXTRA_IMAGE_INFORMATION` (0x10, 이미지 정보 `+0x40`) · `_SUBSECTION`·프로토 PTE 표 안 내용 (`MiInitializeNewImageSectionProtos`, 이름만) · `_OPEN_PACKET` (0xe0, 열기 동안만 쓰는 임시 — 짐 아님). PDB 정의 있음 → run43 방식으로 칸 표 가능.
- **(run45) 체크리스트 밖에 남는 짐 — "남는 곳" C1 ~ C12** (sast/39 결과서 3장, `runs/03_sprint/run45-work-list.md` 3장). 일 86개 중 48개가 체크리스트 (구조체 21종 + ntdll 전역) 밖에도 짐을 남긴다. "덤프로 읽힐지" 는 결과서의 `[추측]` — 읽어 본 것이 아니다. 관련 일 번호는 run45 일 목록 번호.
  - **C1** **커널 이웃 객체** (칸 표 없는 타입): `_SUBSECTION`·`_MSUBSECTION`·`_SECTION_OBJECT_POINTERS`·`_OBJECT_HEADER`·`_HANDLE_TABLE_ENTRY`·`_MI_SECTION_IMAGE_INFORMATION`·`_KEXCEPTION_FRAME`·`_MMSUPPORT_FULL` · 관련 일 3 4 8 12 15 21 28 29 30 · 덤프로 읽힐 것 `[추측]` (커널 풀). PDB 정의 있음 → run43 방식으로 칸 표 가능 → **기록만** — 구현 범위 정할 때 같이 (사용자 결정 2026-10-26) (sast/39 제안 2 "체크리스트 확장")
  - **C2** **PDB 에 정의가 없는 타입**: `_LDRP_LOAD_CONTEXT`·`_EPROCESS_QUOTA_BLOCK`·간선 칸 0x20B·`InitialTeb`·`LdrSystemDllInitBlock` · 관련 일 10 22 26 75 76 · 메모리엔 있으나 칸 이름 모름 — 코드에서 오프셋으로만 (run23·run32 방식) → **기록만** — 구현 범위 정할 때 같이 (사용자 결정 2026-10-26)
  - **C3** 커널 전역 목록·표 안 항목: `PspCidTable` PID·TID 항목, `PsActiveProcessHead`·이웃 프로세스 Flink, CPU 준비 큐 (`_KPRCB`), 쿼터 블록·세션 참조 수 · 관련 일 10 23 25 29 34 · 전역 이름으로 닿음 `[추측]`, K1/K2/K3 비교로 "언제" → **Q79** 에 붙임
  - **C4** **페이지 표·PFN**: PML4, 이미지가 붙을 때 이어지는 프로토 PTE · 관련 일 11 15 16 17 73 · DirBase 물리 주소로 읽힘 `[추측]`. 칸 단위 뜻이 약함 → **기록만** — 구현 범위 정할 때 같이 (사용자 결정 2026-10-26) (에뮬레이터가 페이지 표를 흉내 낼지에 달림)
  - **C5** 이미지 페이지 안 값: IAT 칸 (test.exe 91 · dll1 89 · dll2 88 · kernel32 약 1,274 · kernelbase 약 750), 재배치된 값, `.data` 처음 값 · 관련 일 16 17 59 73 77 · P1b·P2 는 읽힘, 그 전은 `????` 일 수 있음 `[추측]`. 기대값은 PE 파일로 계산 → **Q82** (IAT 기대값 대조, 정적)
  - **C6** 다른 모듈 (dll1·dll2·kernel32·kernelbase) 의 섹션·컨트롤 영역·파일 객체·VAD 노드 (체크리스트는 exe 것만) · 관련 일 15 72 73 · VAD → `Subsection` → `ControlArea` → `FilePointer` 로 닿음 `[추측]`, 칸 표는 이미 있음 → **Q79** 를 넓힘
  - **C7** **프로세스 힙 안 블록**: `_HEAP` 자체 (→ (run46) 머리 칸은 7.6.1 에 표), 새 매개변수·환경 블록, 로더 스냅 버퍼, 활성화 컨텍스트 스택, 경로 캐시, TLS 항목·벡터, 간선 칸, 로드 문맥, 스레드 풀 구조, ETW 등록 블록 · 관련 일 52 53 54 55 62 71 75 76 78 · P1a ~ P2 메모리엔 있음. 블록 주인 가르기는 `!heap` 이 오프라인에서 되는지에 달림 `[추측]` → **기록만** — 구현 범위 정할 때 같이 (사용자 결정 2026-10-26). 관련 대기: **Q86** (오프라인 `!heap` 시험)
  - **C8** 칸이 아닌 유저 구역: 맡긴 `_CONTEXT`, 유저 스택 (메인 + 일꾼 2), API set·leap second 뷰, NLS 매핑 2개, 스케줄러 공유 데이터 2쪽, shim·활성화 컨텍스트 데이터 VAD 2개, 환경 블록 구역 · 관련 일 19 20 26 37 42 47 60 68 · 주소는 앎. `_CONTEXT` 는 이미 읽음 `[관찰]`, leap second 는 8장 모두 읽기 불가 `[관찰]`, 나머지 내용 안 읽음 → **Q79** 에 붙임
  - **C9** **다른 모듈의 전역**: KERNELBASE `.data` (공개 데이터 이름 3,125, 구역 0xad18)·KERNEL32 `.data` (197, 0x16b8) — 시작 함수·`KernelbasePostInit` 이 씀 · 관련 일 66 67 · P1b·P2 에서 읽힘, P1a 엔 아직 없음 `[추측]`. 공개 이름 목록은 가능, 크기는 짐작 → **기록만** — 구현 범위 정할 때 같이 (사용자 결정 2026-10-26). 관련 대기: **Q84** (P1a~P1b 쪼개기, 동적)
  - **C10** 일꾼 스레드 2개의 객체 일체 (`_ETHREAD`·`_KTHREAD`·`_TEB`·트랩 프레임·유저/커널 스택·맡긴 `_CONTEXT`·TLS 벡터) · 관련 일 68 · P1b·P2 `ActiveThreads` 3 `[관찰]` — 메인 스레드 칸 표 재사용 → **Q79** 에 붙임
  - **C11** 핸들 표 항목과 그 뒤 객체: 이벤트 3 (`0xc`·`0x50`·`0x54`), `\KnownDlls` (`0x4c`), 콘솔 (`4` → `0x60`), CSR 포트 (`0x74`), 현재 폴더 등 (핸들 4 → 26 → 44 → 46) · 관련 일 12 21 45 46 47 56 58 67 · `!handle` 8장 모두 있음 `[관찰]` — 번호별 생긴 구간 표로 → **Q79** 에 붙임
  - **C12** **다른 프로세스**: 부모 cmd.exe (핸들 표 항목 2·출력 칸) / csrss (CSR 프로세스·스레드 기록, csrss 핸들 2, 연결 기록 — csrsrv `.data` 공개 이름 42 · basesrv 55) · 관련 일 33 37 38 67 · 부모: EPROCESS `ffff828598add080` 앎 `[관찰]`. csrss: 문맥 바꿔야 함, 페이지아웃이면 못 읽음 `[추측]` → 부모 → **Q79**. csrss → **기록만** — 구현 범위 정할 때 같이 (사용자 결정 2026-10-26). 관련 대기: **Q83** (K3→P0 자식 PEB 쓰기 — 부모·csrss 중 누구인지)
  - Q78 은 범위 그대로 (체크리스트 21종 + ntdll 전역, 사용자 결정 2026-10-26). (나) 는 대부분 "칸 → 쓴 자리" 로 안 보이기 때문 (칸 표가 없거나·다른 모듈·프로세스거나·구역이라).
  - 갈 곳이 정해지지 않은 정적 거리 (run45 "아직 확인 필요"): `nt!NtGetNlsSectionPtr` 커널 쪽 (C8 NLS 매핑의 주인, 일 47), `PspCreateObjectHandle`·`PspUpdateCreateInfo` 가 부모 쪽에 쓰는 것 (C12 부모, 일 33), 부모 KERNELBASE `BasepInitAppCompatData` 본문 (C8·C12, 일 37).

#### 7.6.1 `_HEAP` — NT 힙 머리 (프로세스 힙) (유저, `0x2c8`, ntdll PDB) — run46 (sast/40, Q78a)

- **체크리스트 밖 짐** (C7 의 `_HEAP` 자체). 21종 체크리스트 칸 수 (2장) 에는 안 넣었다.
- 범위: **프로세스 힙 하나를 만드는 동안 머리에 쓰는 칸까지.** 블록 나눠 주기 내부 (`RtlAllocateHeap` 본문, LFH 버킷 동작, 그 뒤 `FrontEndHeap`·`FrontEndHeapType` 바뀜) 는 **범위 밖** — 사용자 결정 2026-10-26 "기록만" (2026-10-27 "힙 내부 정적 (후보 B) 안 넣음" 도 그대로).
- 힙 번지 = P1a `25996f30000` (이 실행). 칸 이름은 PDB `_HEAP` (`+0x0 Segment: _HEAP_SEGMENT` 가 첫 `0x70` 바이트에 겹침). test.exe 는 **NT 힙** (세그먼트 힙 `_SEGMENT_HEAP` 아님 — `+0x10` = `0xffeeffee`).
- 단계 ②, 놓는 법 `직접`, 쓴 주인 ntdll. 경로는 모두 `②… → LdrpInitializeProcessHeap (호출 +0xa4b) → RtlpCreateHeap (호출 +0x191)` 아래. "쓰는 곳" 의 `+0x..` 는 `RtlpCreateHeap` 안이고, 다른 함수는 이름을 붙였다.
- 근거: `[정적]` sast40 · run46. `[관찰]` 은 run44 P1a 덤프 (`ms/logs/04_baggage-read_P1a.txt` 구획 16, 힙 앞 `0x80` 바이트) 에서 읽은 값. 나머지 값은 정적 계산 — 덤프 `dq 25996f30000 L0x5a` 로 맞춰 볼 수 있다 (D13, 7.9).

| `_HEAP` 칸 (PDB) | 값 | 쓰는 곳 | 근거 |
|---|---|---|---|
| `+0x8 Entry.Size` · `+0xa Flags` · `+0xf UnusedBytes` | 머리 크기 `(0x2f0+0xf)>>4` (16바이트 단위) · 1 · 1; 마지막에 `Encoding` 으로 XOR | `+0x5a1 · +0x5aa · +0x5b3`; XOR `RtlpInitializeHeapSegment +0x2cf · +0x2d8` | `[정적]` · `[관찰]` 인코딩된 값 (`01009bfbf7f5dcaf`) |
| `+0x98 Signature` | `0xeeffeeff` | `+0x5bc` | `[정적]` |
| `+0x70 Flags` | 호출자 플래그 `& ~0x10000000` (test.exe = 2) | `+0x5d3` | `[정적]` · `[관찰]` 2 |
| `+0x74 ForceFlags` | `플래그 & 0x6001007d` (= 0) | `+0x652` | `[정적]` · `[관찰]` 0 |
| `+0x78 CompatibilityFlags` | 만드는 동안 `\|= 0x80000000` (`+0x709`), 끝에 `& 0x7fffffff` (`+0x9e0`). `RtlpDisableHeapLookaside & 1` 이면 1 (`+0x874`) | 왼쪽 | `[정적]` · `[관찰]` 0 |
| `+0x90 Interceptor` | 0 (스택 추적 플래그 `0x8000000` 이면 인터셉터 번호) | `+0x5db` (`+0x632`) | `[정적]` |
| `+0x7c EncodeFlagMask` · `+0x80 Encoding` | `0x100000` · 난수로 채운 마스크 (조건: `CompatibilityFlags & 2 == 0` 이고 `Flags & 0x4000000 == 0`) | `RtlpCreateHeapEncoding` (호출 `+0x5ff`) `+0x1d · +0x24 · +0x31 · +0x6b · +0xad · +0xb5` | `[정적]` · `[관찰]` `+0x7c` = `0x100000` |
| `+0x238 Counters` (0x80B) · `+0x290 HeapPollInterval` | 0 으로 지움 · 1 | `+0x5f5 memset` · `+0x609` | `[정적]` |
| `Counters.TotalMemoryReserved` (`+0x238`) · `TotalMemoryCommitted` (`+0x240`) · `TotalSegments` (`+0x260`) · `TotalUCRs` (`+0x264`) · `TotalMemoryLargeUCR` (`+0x248`, 조건) | 세그먼트 크기만큼 더하고 줄임 · 1 · 1 | `RtlpInitializeHeapSegment +0x1fa · +0x20b · +0x32e`; `RtlpCreateUCREntry` | `[정적]` |
| 첫 세그먼트 (`_HEAP_SEGMENT`, 힙 자신) `+0x10 SegmentSignature` · `+0x14 SegmentFlags` · `+0x18 SegmentListEntry` · `+0x28 Heap` · `+0x30 BaseAddress` · `+0x38 NumberOfPages` · `+0x40 FirstEntry` · `+0x48 LastValidEntry` · `+0x50 NumberOfUnCommittedPages` · `+0x54 NumberOfUnCommittedRanges` · `+0x58 SegmentAllocatorBackTraceIndex` (스택 추적 때만) · `+0x60 UCRSegmentList` | `0xffeeffee` · 2 · `힙+0x120` 에 연결 · 힙 · 힙 · `0xff` · 힙 `+0x740` (= `0x2f0` 머리 + `0x450` 블록 색인) · 힙 `+0xff000` · (커밋 따라) · 1 | `RtlpInitializeHeapSegment` (호출 `+0x746`) `+0x1c5 · +0x1ca · +0x1ce · +0x1d1 · +0x1d8 · +0x1dc · +0x1e8 · +0x212`, UCR `+0x2bb RtlpCreateUCREntry` | `[정적]` · `[관찰]` `+0x10`·`+0x14`·`+0x18`·`+0x28`·`+0x30`·`+0x38` `0xff`·`+0x40` `…740`·`+0x48` `…2f000`·`+0x50` `0xf6` (커밋이 9쪽으로 자란 뒤)·`+0x54` 1 |
| `+0x120 SegmentList` · `+0x110 VirtualAllocdBlocks` · `+0xf0 UCRList` · `+0x150 FreeLists` | 자기를 가리키는 빈 목록 (`SegmentList` 엔 첫 세그먼트, `FreeLists` 엔 큰 빈 블록 하나 — `+0xc0 TotalFreeSize` 도 올림) | `+0x679 ~ +0x6b9`; `RtlpInitializeHeapSegment +0x306 ~ +0x310` (SegmentList), `+0x2ea RtlpInsertFreeBlock` (FreeLists) | `[정적]` · `[관찰]` `+0x18` 이 `힙+0x120` |
| `+0xd2 HeaderValidateLength` · `+0xd8 HeaderValidateCopy` | `0x2c8` · 0 | `+0x661 · +0x66d` | `[정적]` |
| `+0x160 LockVariable` | 힙 안 임계 구역 (`힙+0x2c8`, 0x28B — `+0x6df RtlInitializeCriticalSectionEx` 가 만듦, 6장) | `+0x6fd` | `[정적]` |
| `+0x2f0` 블록 색인 (`_HEAP_LIST_LOOKUP`: `+0x8 ArraySize` `0x80` · `+0x20 ListHead` `힙+0x150` · `+0x28 ListsInUseUlong` · `+0x30 ListHints`) · `+0x138 BlocksIndex` | 머리 뒤 `0x38+0x10+0x400` 바이트에 만들고 `+0x138` 이 가리킴 | `+0x76d ~ +0x78b`; `RtlpPopulateListIndex` (호출 `+0x797`) | `[정적]` |
| `+0xd0 ProcessHeapsListIndex` | 0 | `+0x7a1` | `[정적]` |
| `+0xa0 SegmentReserve` · `+0xa8 SegmentCommit` · `+0xb0 DeCommitFreeBlockThreshold` · `+0xb8 DeCommitTotalFreeThreshold` | PEB `+0xc8`·`+0xd0` 그대로 (`0x100000`, `0x2000`), PEB `+0xe0`·`+0xd8` ÷ 16 (`0x100`, `0x1000`) — 매개변수 값이 0 일 때 | `+0x7b5 · +0x7c9 · +0x7e1 · +0x7f9` | `[정적]` (입력은 `[관찰]` PEB 값) |
| `+0xc8 MaximumAllocationSize` | `0x7ffffffdefff` (사용자 공간 최대 − `0x10000` − `0x1000`) | `+0x80d` | `[정적]` |
| `+0x94 VirtualMemoryThreshold` | `(0xff000+0xf)>>4` = `0xff00` | `+0x829` | `[정적]` |
| `+0x168 CommitRoutine` | `0 ^ RtlpHeapKey` (= 키) | `+0x843` | `[정적]` |
| `+0x2b8 TuningParameters.CommittThresholdShift` · `+0x2c0 MaxPreCommittThreshold` | 4 · `0x1fe000` | `+0x84f · +0x85b` | `[정적]` |
| `+0x100 AlignRound` · `+0x108 AlignMask` | `0x1f` (꼬리 검사 플래그 `0x20` 이면 `+0x10`) · `0xfffffffffffffff0` | `+0x884 · +0x8aa · +0x894` | `[정적]` |
| `+0x198 FrontEndHeap` · `+0x1a0 FrontHeapLockCount` · `+0x1a2 FrontEndHeapType` · `+0x1a3 RequestedFrontEndHeapType` · `+0x140 UCRIndex` | 모두 0 (LFH 는 나중에 켜짐 — 범위 밖) | `+0x8b7 · +0x8c3 · +0x8cf · +0x8db · +0x8e7` | `[정적]` |
| `+0x1a8 FrontEndHeapUsageData` · `+0x1b0 FrontEndHeapMaximumIndex` | `RtlAllocateHeap(힙, 0x80000a, 0x100)` 가 준 블록 (힙 `+0x750`) · `0x80` | `+0x915 · +0x91f · +0x93b · +0x944` | `[정적]` · 조건 `RtlpDisableHeapLookaside & 1 == 0` 이고 `(Flags & 3) == 2` |
| `+0x233 ReadOnly/InternalFlags` · `+0x170 StackTraceInitVar` | 0 · 0 | `+0x950 · +0x95c` | `[정적]` |
| `+0x188 UserContext` | `ProcessHeaps` 노드 주소 (`힙+0x860`) | `RtlpProcessHeapsInsert +0x72` (호출 `+0x96f`) | `[정적]` · `[관찰]` 노드 = `RtlpProcessHeaps` Flink |

- 세그먼트 힙 길이었다면 (test.exe 는 안 탐, 개요만 `[정적]`): `RtlpHpHeapCreate` (`+0xc18`) 가 `_SEGMENT_HEAP` (`0xa00`) 을 `RtlpHpHeapAllocate → RtlpHpAllocVA → RtlpHpEnvAllocVA → NtAllocateVirtualMemoryEx` 로 받아 `+0x10 Signature` = `0xddeeddee`·`SegContexts[0..1]`·`VsContext`·`LfhContext` 등을 채운다. 요청 범위 밖이라 칸 표는 안 만든다.
- 힙 안 첫 블록 (범위 밖 앞까지): 첫 빈 블록 힙 `+0x740`, 첫 블록 `FrontEndHeapUsageData` (`0x100`B, `+0x750`), 둘째 `ProcessHeaps` 노드 (`0x30`B, `+0x860`). 그 뒤 블록 (매개변수 블록 `25996f37a50` 등) 의 자리 정하기는 `RtlAllocateHeap` 본문이라 범위 밖 (6장).

#### 7.6.2 시스템 DLL 표 `PspSystemDlls` 와 부팅 때 놓인 커널 전역 (커널, 단계 `Ⓑ`) — run48 (sast/42, Q93)

- **체크리스트 밖 짐** (21종 + ntdll 전역이 아닌 커널 전역). 2장 숫자에는 안 넣었다.
- **프로세스 생성 전 (부팅)** 에 한 번 놓인다. 프로세스 생성 (①) 은 이 짐을 **읽기·비교·붙이기만** 한다 → 짐 지도의 `입력` 중 "부팅에서 온 값" 의 출처가 이 절이다. 사용자 짐작 "부팅 때 준비, 프로세스마다는 붙이기만" 은 맞다 `[정적]` (run48).
- 칸 타입 `_PS_SYSTEM_DLL_INFO` 는 PDB 에 이름만 있고 칸 정의가 없다 (`pdb_struct.py` "없음") → 칸은 **오프셋으로만** 쓴다. 칸 하나 0x40.
- 처음 값 (`Ⓑ0`) 은 결과서가 PE 파일 바이트를 직접 읽은 값이다 `[정적]`. 칸 플래그 비트 뜻은 동작으로 읽은 것 `[추측]` (이름 없음).
- 경로 줄임 (3장 줄임 표에도 있음):

| 줄임 | 사슬 |
|---|---|
| `Ⓑ…` | `Phase1Initialization → IoInitSystem (호출 +0x36) → IoInitSystemPreDrivers (호출 +0x12)` — 부팅 `Phase1` 스레드 (System 프로세스 소속으로 보임 `[추측]`, D3) |
| `ⒷL…` | `Ⓑ… → PsLocateSystemDlls (호출 +0x10f5) → PspLocateSystemDll (호출 +0x9e)` — 표 칸마다 한 번 (이 빌드: `[0]` ntdll · `[1]` SysWOW64 ntdll · `[4]` vertdll) |
| `Ⓑ2…` | `Phase1Initialization → Phase1InitializationIoReady (호출 +0x61) → PsInitSystem (호출 +0x1d5) → PspInitPhase2 (호출 +0x3b) → PspInitializeSystemDlls (호출 +0x172)` |

**표 `PspSystemDlls`** (포인터 6칸, rva `0xfd8130`)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `[0]` · `[1]` · `[4]` | `Ⓑ0` | `PspNativeSystemDllData` (rva `0xfd8980`) · `PspWowX86SystemDllData` (`0xfd8940`) · `PspVsmEnclaveRuntimeDllData` (`0xfd89c0`) | 입력 (PE 파일 값) | — | (링크) | `[정적]` PE 바이트 · `[정적] PDB` 이름 · 값 D1 |
| `[2]` · `[3]` · `[5]` | `Ⓑ0` | 0 — 이 빌드에서 이 칸을 쓰는 줄 없음 | 입력 (파일 값) | — | — | `[정적]` `_refs/PspSystemDlls.tsv` 쓰기 1줄 (아래 줄) 뿐. 레지스터+오프셋 쓰기는 `_refs` 가 못 잡음 → D1 |
| 별칭 칸 (플래그 `0x20`) | `Ⓑ1` | 올리기 실패면 NULL | 직접 | nt | `Ⓑ… → PsLocateSystemDlls +0x8d` | `[정적]` · **이 빌드 표엔 그런 칸이 없어 안 탐** |

**칸** (0x40, 값은 `[0]` ntdll / `[1]` WOW ntdll / `[4]` vertdll 순)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` | `Ⓑ0` | 섹션 빠른 참조 자리 포인터 → `PspNativeSystemDll` (`0xfda548`) / `PspWowX86SystemDll` (`0xfda568`) / `PspVsmEnclaveRuntimeDll` (`0xfda558`). 그 자리는 16바이트 (`_EX_FAST_REF` 8 + 락 8), 0 으로 시작 | 입력 (파일 값) | — | (링크) | `[정적]` PE 바이트 · PDB 이름 |
| `*[+0x0]` (위 16바이트 자리) | `Ⓑ1` | 섹션 객체 `\| 0xf` (빠른 참조, 참조 +15 덤) · 락 칸 0 | 직접 (객체는 `MmCreateSpecialImageSection` 이 만듦) | nt | `ⒷL… +0xef` (섹션) `· +0x1ae ObInitializeFastReference · +0x1b6` (락) | `[정적]` sast42 · 정확 · 섹션 참조는 돌려주지 않음 (영구) |
| `+0x8` 플래그 (WORD) | `Ⓑ0` | `0x0013` / `0x0008` / `0x0012` | 입력 (파일 값) | — | (링크) | `[정적]` PE 바이트 · 비트 뜻 `[추측]` (아래) |
| `+0xa` 머신 (WORD) | `Ⓑ0` | `0x8664` / `0x014c` / `0x8664` | 입력 (파일 값) | — | (링크) | `[정적]` PE 바이트 |
| `+0xc` `SizeOfImage` | `Ⓑ1` | 올린 뷰 헤더의 `SizeOfImage` (기대 `0x267000` / `0x1bf000` / `0x36000`, PE 파일) | 직접 (값은 뷰 헤더 = 입력) | nt | `ⒷL… → PspMapSystemDll (호출 +0x233) +0x20705b` (rva `0xb18c33`) | `[정적]` sast42 · 정확 · 값 D1 (run44 `lm m ntdll` 크기 `0x267000` 은 간접) |
| `+0x10` 경로 `UNICODE_STRING` (버퍼 `+0x18`) | `Ⓑ0` | `\SystemRoot\System32\ntdll.dll` (길이 `0x3c`) / `\SystemRoot\SysWOW64\ntdll.dll` / `\SystemRoot\System32\vertdll.dll` | 입력 (파일 값) | — | (링크) · 읽는 곳 `ⒷL… +0xc0 ZwOpenFile` · `+0x59` (`ZwSystemDebugControl` 입력으로 복사) | `[정적]` PE 바이트 |
| `+0x20` 기본 주소 | `Ⓑ1` | 올린 뷰 NT 헤더 `ImageBase` (PE32+ `[+0x30]`, PE32 `[+0x34]`) = 섹션 만들 때 MM 이 고른 무작위 주소 | 직접 (값은 뷰 헤더) | nt | `ⒷL… → PspMapSystemDll (호출 +0x233) +0x1a5 → RtlImageNtHeader (호출 +0x1ae)`, 쓰기 `+0x207054` (rva `0xb18c2c`) | `[정적]` sast42 · 정확 · 뜻은 `[관찰]` run44 첫 트랩 `Rdx` = ntdll 베이스 (간접) · 뷰 헤더를 고치는 MM 줄 `[공백]` D5 · 값 D1 |
| `+0x28` 뷰 시작 (System 프로세스 안) | `Ⓑ1` | `MmMapViewOfSectionEx` 가 돌려준 주소 | 직접 | nt | `… PspMapSystemDll +0x20705e` (rva `0xb18c36`) | `[정적]` sast42 · 정확 · `+0x20` 과 같은지 D1, 뷰가 있나 D3 |
| `+0x30` 짧은 이름 포인터 | `Ⓑ0` | `ntdll.dll` / `ntdll32.dll` / `vertdll.dll` | 입력 (파일 값) | — | (링크) | `[정적]` PE 바이트 |
| `+0x38` 재배치량 | `Ⓑ1` | `MmGetSectionInformation` 클래스 2 (`MiRelocateAmount`) | 직접 | nt | `ⒷL… +0x17e MmGetSectionInformation · +0x1aa` (별칭 칸: `PsLocateSystemDlls +0x6c · +0x70` 0번 칸 값 복사) | `[정적]` sast42 · 정확 · 값 D1 |

- 칸 9 = 파일 값 (`Ⓑ0`) 5 (`+0x0`·`+0x8`·`+0xa`·`+0x10`·`+0x30`) + nt 가 씀 (`Ⓑ1`) 4 (`+0xc`·`+0x20`·`+0x28`·`+0x38`). 모르는 칸 0. `+0x0` 이 가리키는 16바이트 자리는 칸이 아니라 따로 센다 (nt `Ⓑ1`).
- 칸 플래그 `+0x8` 비트 (이 구간 코드가 읽는 것, 동작으로만 `[추측]`): `0x1` 필수 (실패하면 `KeBugCheckEx(0x6b)`, 사이트 9 확인 대상) · `0x8` 올릴 주소 상한 풀기 (WOW) · `0x10` VSL 비트4 를 섹션 만들기에 넘김 · `0x20` 별칭 칸 (찾지 않고 0번 섹션, `ImageMachine` 인자) · `0x40` `PsMapSystemDlls` 가 보안 프로세스에게도 올릴 칸 · `0x2` 이 구간은 안 읽음.

**이 구간이 놓는 다른 짐** (객체·뷰·전역)

| 짐 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| 시스템 DLL 섹션 객체 (칸마다, 이미지 섹션, 이름 없음) | `Ⓑ1` | `_SECTION` + 컨트롤 영역·세그먼트·이미지 정보 (exe 와 같은 타입 — 3.20 ~ 3.23). 다른 점: 토큰 NULL · 서명 수준 `0x0c` · 패킷 플래그 `0x10` (보호 이미지) | 직접 (함수가 만듦) | nt | `ⒷL… → MmCreateSpecialImageSection (호출 +0xef) → MiCreateSection → …` (exe 와 같은 사슬, `①S…` 의 함수들) | `[정적]` sast42 · 칸 값은 안 셈 (exe 묶음 칸 표를 그대로 쓸 수 있음 `[추측]`) |
| 파일 핸들 · 섹션 핸들 | `Ⓑ1` | 열었다 닫음 → 남는 것 없음 | 직접 | nt | `ⒷL… +0xc0 ZwOpenFile · +0x117 ObCloseHandle` / `+0xef · +0x169 ObCloseHandle` | `[정적]` sast42 |
| System 프로세스 주소 공간의 ntdll 뷰 (+ SysWOW64 ntdll · vertdll 뷰) | `Ⓑ1` | 섹션 통째, 페이지 보호 `2`, 할당 종류 `+0x40000` (인자4 ≠ 0) | 붙이기 | nt | `ⒷL… → PspMapSystemDll (호출 +0x233) → MmMapViewOfSectionEx (호출 +0x118)` | `[정적]` sast42 · 뷰가 있나 D3 · 반환이 `0x40000003` 인가 D10 |
| `NtGlobalFlag` 비트 `0x40000` | `Ⓑ1` | 켰다 되돌림 → 남는 것 없음 | 직접 | nt | `Ⓑ… +0x10e3 ~ +0x10ef` 켬 · `+0x10fa` 되돌림 | `[정적]` sast42 · 정확 · D6 · D11 |
| `ExpSysDbgPulledFileTable` | `Ⓑ1` **(커널 디버깅 켠 부팅만)** | "이미 끌어온 파일 표" (풀 `0x4010` 바이트, 해시 8바이트 줄, 용량 `0x800`) | 직접 | nt | `ⒷL… +0x79 ZwSystemDebugControl(0x26) → NtSystemDebugControl` (조각 `FUN_140b3e0e7 +0x1f8`) `→ ExpKdPullRemoteFileForUser` | `[정적]` sast42 (`.c` 읽음) · **디버거가 붙어서 달라지는 짐** (run48) · 값 D6 |
| `PsNtdllExports` (`_PS_NTDLL_EXPORTS` 0xa8, PDB 21칸) 중 19칸 | `Ⓑ2` | ntdll 함수 주소 (사용자 쪽 = 뷰 안 주소 + (`+0x20` − `+0x28`)). `+0x0 LdrSystemDllInitBlock`, `+0x8 LdrInitializeThunk`, `+0x10 RtlUserThreadStart` … `+0xa0 LdrHotPatchNotify` | 직접 | nt | `Ⓑ2… → PspLookupEntryPoint (호출 +0x56) → RtlFindExportedRoutineByName (호출 +0x1b)`, 쓰기 `PspInitializeSystemDlls +0x6b` | `[정적]` sast42 · run16 · 정확 · `[관찰]` run44 K1 (19칸 모두 `00007ffa…`, 덤프 8장 같음) |
| `PsNtdllExports +0x38 KiUserCallbackDispatcherReturn` · `+0x98 KiUserEmulationDispatcher` | — | 아무도 안 씀 (이름 표 `NtdllExportInformation[0]` 19개에 없음) | — | — | — | `[정적]` `_refs/PsNtdllExports.tsv` 쓰기 1줄 · `[관찰]` run44 K1 로그 (`04_baggage-read_K1.txt` 1531 ~ 1542줄) 둘 다 0 |
| `PsWowX86SharedInformation` (rva `0xfda900`) 8칸 + `+0x30` | `Ⓑ2` | WOW ntdll 함수 주소 8개 (`LdrInitializeThunk` … `RtlpFreezeTimeBias`) · `+0x30` = WOW 칸 `+0x20` | 직접 | nt | `Ⓑ2… +0x56 · +0x6b`, `+0x77 PspWow64GetSharedInformation → +0x80` | `[정적]` sast42 · WOW 칸을 못 찾았으면 안 채움 (`PsQuerySystemDllInfo` NULL) · 값 D2 |
| `MiState.Processes.SystemDllBase` (rva `0xe2db80` = `MiState+0x1000`) | `Ⓑ3` | ← 0번 칸 `+0x20` | 직접 | nt | `Phase1Initialization → Phase1InitializationIoReady (호출 +0x61) → MmInitSystemDll (호출 +0x1e2) +0x22` | `[정적]` sast42 · `[정적] PDB` 이름 (`_MI_PROCESS_STATE +0x0`) · 값 D4 |
| `PsSecureSystemProcess` 안 ntdll · vertdll 뷰, 두 섹션 참조를 넘김 | `Ⓑ4` (VBS 일 때만) | 붙이기 (인자4 = 0) 뒤 `VslpEnterIumSecureMode(2, 4, …)` | 붙이기 + 주소 넘김 | nt | `Phase1InitializationIoReady (호출 +0x20a) → PsInitSystem (호출 +0x34) → PspInitPhase3 +0x6b · +0xa0 PspMapSystemDll · +0xea VslpEnterIumSecureMode` | `[정적]` sast42 · 본문 범위 밖 · `VslVsmEnabled` 값 D8 |

- 읽기만 하는 커널 전역 (이 구간): `MmSectionObjectType` (`PspLocateSystemDll +0x11c`), `PsInitialSystemProcess` (`PspMapSystemDll +0x195`), `PsEmbeddedNTMask`·`NtGlobalFlag`, `PsSecureSystemProcess`·`VslVsmEnabled` (VBS).
- `PsNtdllExports` 21칸 = nt 가 씀 (`Ⓑ2`) 19 + 아무도 안 씀 2. 모르는 칸 0.

**프로세스 생성 (①) 때 읽는 자리 — 짝** (sast/42 2.5)

| 부팅 때 쓴 것 | ① 에서 읽는 자리 |
|---|---|
| 섹션 객체 (칸 `+0x0`) `Ⓑ1` | `MiMapProcessExecutable +0xdc → PsMapSystemDlls +0xc0 → PspMapSystemDll +0x40 PspReferenceSystemDll` → `+0x11d MmMapViewOfSectionEx` (인자4 = 0). 올릴 칸: 0번 늘 · WOW 프로세스면 `PsWow64GetProcessNtdllType` 번호 칸 · 보안 프로세스면 플래그 `0x40` 칸. `PsMapSystemDlls` 는 `MiMapProcessExecutable` 플래그 `0x10` 이 꺼졌을 때만. 그 밖 `DbgkCreateThread`·`MmInitSystemDll`·`PspInitPhase3` |
| 칸 `+0x20` `Ⓑ1` | `PspMapSystemDll +0x172` (비교 — 다르면 `0xc0000001`), `PspInitializeThunkContext +0x196 · +0x19d` (새 스레드 `Rdx` → 3.15), `PspGetScpCfgFunctions`·`PsShutdownSystem`·`PsQuerySystemDllInfo +0x13`·`MiRelocateImage +0x756 · +0x760` |
| 칸 `+0xc` `Ⓑ1` | `MiRelocateImage +0x75d` |
| 칸 `+0x38` `Ⓑ1` | `PspPrepareSystemDllInitBlock +0xa5/+0xac` (→ 블록 `+0x10`), `+0xcb ~ +0xd6` (WOW → `+0x8`) — 4장 짝 |
| 칸 `+0x8` `Ⓑ0` | `PsMapSystemDlls +0x12a`, `PspMapSystemDll +0x68` |
| `PsNtdllExports` `Ⓑ2` | `PspInitializeThunkContext +0x1a1 · +0x37f` (`+0x8` → 트랩 `Rip`, 3.15), `PspPrepareSystemDllInitBlock +0x214` (`+0x0`), `NtCreateUserProcess +0xd10` (`+0x10` → 처음 문맥 `Rip`) 등 읽기 약 40줄 (`_refs/PsNtdllExports.tsv`) |
| `PsWowX86SharedInformation` `Ⓑ2` | WOW 쪽 (안 봄). `PspPrepareSystemDllInitBlock +0xc3 ~ +0xef` 가 블록에 복사하는 것으로 읽힘 `[추측]` |
| `MiState.Processes.SystemDllBase` `Ⓑ3` | `MmGetSectionInformation` (그 밖 안 봄) |

### 7.7 결과서가 넘긴 공백 (sast/38 "새로 생긴 공백")

- 커널 객체 칸 근거가 거의 없다: `_HANDLE_TABLE`·`_MMVAD*`·`_TOKEN`·~~`_SECTION`·`_FILE_OBJECT`~~·`_KPROCESS` 아는 칸 0 ((해결됨, 일부 → run47) `_SECTION`·`_FILE_OBJECT` 와 섹션 묶음 나머지 셋은 Q78b 로 모두 닫힘), `_EPROCESS` 모름 241/263, `_KTHREAD` 227/237. `PspAllocateProcess` callee 105 중 문서에 이름도 없는 40, 본문 뽑은 것 12. 안 뽑은 `KeStartThread`·`KeInitializeProcess`·`ObInitProcess`·`PspInitializeProcessSecurity`·`PsAssignThreadId`·`PspApplyMitigationOptions`·`MiAllocateProcessVads`·`MiInsertProcessVads` → **Q78**.
- `MmCreateTeb` 가 TEB 에 쓰는 칸 (대입 약 47줄) → **Q78**.
- (해결됨, 일부 → run45) `.mrdata` 를 쓰는 곳 `LdrInitializeMrdata` 본문 (`.mrdata` 50 중 44 모름) → **Q78** → sast/39: 본문은 전역 3개 (`.data` `LdrpMrdataLock`, `.mrdata` `LdrpAllocationGranularity`·`LdrpMaximumUserModeAddress`) 만 쓴다. `LdrpInitializeCfgScpHelpers` 로 `.mrdata` 2개 더. `.mrdata` 모름 44 → 40, 나머지는 그대로 Q78.
- 지도에 없는 함수 둘 `PspUserThreadStartup`·`PspInitializeThunkContext` — 지도 뿌리를 `PspUserThreadStartup` 으로 하나 더 (`run_ghidra.sh map ntoskrnl.exe PspUserThreadStartup`) → **Q78**.
- `.data` 438 중 문서에 이름도 없는 343, 이름은 나오는데 쓴 자리를 못 찾은 48 → **Q78** (줄 찾기) · ~~**Q77** (바뀌나)~~ → (run44) 바뀌는 때는 7.3 구간별 목록.
- 초기화 함수 15개의 callee 중 어디에도 없는 57개 (nt 43 · ntdll 14, `callees.tsv` 의 `doc=none`) — "짐을 놓나 / 잡일 / 안 타는 갈래" 가르기 → **Q78**.
- 간접 호출 10줄의 실제 대상, 주소만 넘기는 전역 6개의 실제 값 → **Q77**.
  - (run44, 일부) 전역 6개 값 `[관찰]`: `Kernel32ThreadInitThunkFunction` = KERNEL32+`0x2cd70` (P1a~P1b), `LdrpKnownDllDirectoryHandle` `0x4c`, `LdrpLoadCompleteEvent` `0x50`, `LdrpWorkCompleteEvent` `0x54`, `g_LdrpWow64LdrpInitialize` 0, `__guard_dispatch_icall_fptr` 읽기 불가. 간접 호출 10줄의 대상은 사진 (멈춘 순간의 메모리) 으로는 못 본다 — 그 줄에 멈춰야 함 → 남음 (7.8).

### 7.8 사진이 새로 연 빈 곳 (run44)

- (해결됨 → run45) **`PEB.ProcessParameters` 를 누가 옮기나** (`P1~P1a`): 블록이 `25996cb0000` → 프로세스 힙 안 `25996f37a50` 으로 바뀌고 옛 private 영역 (32쪽) 이 사라진다. 새 블록은 포인터만 새 자리 기준, `EnvironmentVersion` 0 → 3. ~~정적 문서엔 이 구간에 옮기는 줄이 없다 → Q78 (`LdrpInitializeProcess` 앞쪽, `+0x6c5 RtlNormalizeProcessParams` 근처 `[추측]`)~~ → sast/39 `[정적]`: **`LdrpInitializeProcess +0xc3f RtlpInitParameterBlock`** (힙 할당 → 통째 복사 → 포인터 9개 → `PEB+0x20` `+0x10d` → 옛 블록 `NtFreeVirtualMemory(MEM_RELEASE)`), 환경은 앞 **`+0xc32 RtlpInitEnvironmentBlock`** (`Environment`·`EnvironmentSize`, `EnvironmentVersion` +1). 3.1·3.2 줄. 남은 것: `EnvironmentVersion` 나머지 +2 (7.5).
- **`HANDLE_TABLE.UniqueProcessId`** 부모 PID → 자기 PID (`K2~K3`) — 쓰는 줄 Q78.
- **`EPROCESS.OwnerProcessId`** `0x19a8` → `0x729` (`P1a~P1b`) — 정적으로 아는 쓰는 곳은 `PspAllocateProcess +0x710` 하나. flow.md 4장 Q65 의 `NtSetInformationProcess +0x1ecd` 와 겹칠 수 있음 `[추측]`.
- **매개변수 `ConsoleHandle`** 4 → `0x60` (`P1a~P1b`, `\Reference {ConDrv}` → `\Connect {ConDrv}`) — 쓰는 쪽 (KERNELBASE 콘솔 준비 `[추측]`) Q78.
- **부모 쪽이 ⓪ 전에 자식에 쓰는 칸** (`K3~P0`): PEB `pShimData`·`SystemDefaultActivationContextData` (+ VAD 2개), EPROCESS `ExceptionPortData`·`Enable…Logging` 비트 5개 (`Flags2`·`Flags3`)·`WakeChannel`·`BamPpmPolicy`, KPROCESS `ProcessFlags` — 부모 KERNELBASE·csrss·커널 알림 쪽 `[추측]`. "쓴 주인" 이 `다른 DLL` 일 수 있는 첫 후보.
- **구간이 정해진 모르는 칸 251개** (유저 41 · 커널 187 · 전역 23, 7.2·7.3. run46 전 254 — PEB 힙 목록 3칸 `P1~P1a` 가 Q78a 로 닫힘) → Q78 은 구간 안 함수만 뒤지면 된다. 특히 `K1~K2` TEB 8칸 ↔ `MmCreateTeb` 대입 약 47줄, `P0~P1` 칸 ↔ `PspUserThreadStartup` 정상 길 4개 (본문 안 봄), `P1a~P1b` `Csr*` 8개·PEB CSR 칸 4개 ↔ CSR 연결 (KERNELBASE 시작 함수 → ntdll `CsrClientConnectToServer` `[추측: 이름]`).
- **못 좁힌 것**: `≤P1a` 전역 156 (`.data` 143 · `.mrdata` 13) + `PebLdr` 5칸 (K1 ~ P1 읽기 불가. run45 정정: 전엔 "`≤P1a` 161" 로 적었음 — 2.1 의 "전역 못 좁힌 161" 과 다른 묶음) — ntdll 파일의 처음 값 (파일 0x2000 + 나머지 0, run17) 과 P1a 값 비교 (정적) 또는 K1 ~ P1 에서 다른 읽는 법 (동적). `.00cfg` 5칸 (8장 모두 읽기 불가). VAD 노드 23칸 (덤프 8개에서 노드 `dt` 다시 읽으면 됨).
- **`ProcessParameters.LoaderThreads` = 0** (8장 모두) — 커널 기본값 `PsDefaultLoaderThreads` 값 (덤프 하나에서 `dd`) 으로 확인.
- 간접 호출 10줄의 실제 대상 (7.7) — 사진으론 못 봄, 그 줄에 멈추는 동적.

### 7.9 Q78a (프로세스 힙 구간) 가 넘긴 동적 질문 (run46, sast/40 D1 ~ D13)

- 번호는 결과서 그대로. 멈춤점·찍을 것은 `runs/03_sprint/run46-process-heap.md` "아직 확인 필요". plan **Q91** 로 (오케스트레이터).
- **필요 (정적으로 못 정함)**: **D1** `PEB.pShimData` 안 두 바이트 (`+0x11aa` 비트 `0x40`·`+0x11b2` 비트 `0x4` — 세그먼트 힙을 안 고른 조건이 어느 것인가) · **D4** IFEO 열기 두 번의 결과와 이 프로세스 첫 IFEO 열기인지 · **D5** `NtQueryInformationProcess(0x24)` 반환·쿠키 · **D9** 무작위 몫 n 과 `NtFreeVirtualMemory` 실제 여부.
- **사진·정적으로 이미 닫힘 (확인만)**: D2 (`RtlpHpOptIntoSegmentHeap` 반환 0) · D6 (시스템 정보 조회 세 번 — 성공은 사진, 순서만) · D7 (큰 예약 세 번 — 번지·크기·확정은 사진, 순서만) · D8 (`RtlpCreateHeap` 입구 인자) · D10 (`+0xa34` 안 탐) · D11 (`+0x67` 간접 호출 안 탐) · D12 (`Segment Heap` 레지스트리 키, 효과 없음).
- **덤프로 답할 수 있음 (덤프 읽기는 이번 범위 밖)**: D1 (덤프에 `25996d10000` 2쪽이 있으면 `db 25996d10000+11a8 L10`), D13 (힙 머리 나머지 칸, `dq 25996f30000 L0x5a` — 7.6.1 정적 값 검증).
- 정적 `[추측]` 으로 남김: D3 (매니페스트 `heapType` 조회가 `ActivationContextData` null 이라 음수로 끝나는지).

### 7.10 Q78b (exe 섹션 묶음) 가 넘긴 동적 질문 (run47, sast/41 D1 ~ D10)

- 번호는 결과서 그대로. 멈춤점·찍을 것은 `runs/03_sprint/run47-section-chain.md` "아직 확인 필요". D1 은 plan **Q61** 에 합침, 나머지는 새 **Q92** (오케스트레이터).
- **필요 (정적으로 못 정함)**:
  - **D1** test.exe 가 공유 길인가 새 길인가 (3.21 — 두 길이 같은 숫자를 만들어 사진으로 못 가름). `MiCreateNewSection` / `MiShareExistingControlArea` 입구 중 서는 쪽, 또는 **재부팅 없이 두 번 실행해 두 K1 의 `_SECTION.u1` 비교** (사진 두 번으로 가능) → Q61
  - **D4** `CA.u2` (`ffffffff` / `0x00c00009`) 가 `MiSelectImageBase` 어느 갈래에서 나오나 → Q92
  - **D5** `NumberOfPfnReferences` 3 → `0x2d`, `FilePointer` 아래 비트를 바꾸는 주체 (`ba w8`) → Q92
  - **D6** `FILE_OBJECT.Flags` `0x4400` 을 켜는 때가 exe 핸들 닫기 (`PspDeleteCreateProcessContext +0x134`) 인가 (`ba w4`) → Q92
  - **D7** `Vpb`·`FsContext`·`FsContext2`·`SectionObjectPointer`·`Flags 0x40/0x80000` 쓴 모듈이 ntfs 인가 (`ba w8` + `lm a @rip`) — 2장 "다른 DLL (드라이버)" 5 의 `[추측]` 을 닫는 질문 → Q92
  - D5·D6·D7 은 데이터 멈춤점이라 한 실행에서 같은 주소로 같이 본다.
- **확인만 (낮은 우선순위, Q92)**: D2 (`_SECTION` 패킷 사본) · D3 (원격·`SEC_BASED`·세션 갈래 안 탐) · D8 (`IopParseFile` 안 탐, `+0xe1e` 에서 할당) · D9 (`IoSetShareAccess` 를 부르는 쪽) · D10 (`NoValidationNeeded` 의 서명 수준 값). 사진으로는 안 됨 (K1 은 이미 최종 값).
- **정적 `[공백]` 하나 (Q92 에 같이)**: exe 이름 문자열 `NtCreateUserProcess [RBP+0x290]` (→ 객체 속성 `ObjectName`) 을 채우는 줄 — 부모가 넘긴 NT 경로로 보임 `[추측]`.
- 나머지 Q78 로 넘긴 정적 거리: `EPROCESS.SectionObject`·`ImageFilePointer`·`SectionBaseAddress` 쓰기 (Q78c), 매핑이 올리는 `NumberOfMappedViews`·`ListHead`·`NumberOfUserReferences` (Q78c / Q79, `[추측]`), 칸 표 후보 (7.6).

### 7.11 Q93 (부팅 준비 갈래) 가 넘긴 질문 (run48, sast/42 D1 ~ D12)

- 번호는 결과서 그대로. 멈춤점·찍을 것은 `runs/03_sprint/run48-system-dll-boot.md` "아직 확인 필요". **D1·D2 는 plan Q42 에 출처로 합침, D3 ~ D12 는 새 Q94** (오케스트레이터).
- **A. 부팅 뒤 보통 KDNET 세션·덤프에서 값만** (`dq`·`dt`·`lm`):
  - **D1** `PspSystemDlls` 6칸과 칸마다 `+0x8`·`+0xc`·`+0x20`·`+0x28`·`+0x38` (7.6.2 칸 표의 "값 D1" 전부). `+0x20` = `+0x28` 인가, = 사용자 `lm m ntdll` 인가 → Q42
  - **D2** `PsWowX86SharedInformation` (`+0x30` = WOW ntdll 기본 주소) → Q42. (`PsNtdllExports +0x38`·`+0x98` 이 0 인가는 **이미 run44 K1 로그에 있음 `[관찰]`** — 7.6.2)
  - **D3** System 프로세스에 ntdll (+ WOW ntdll·vertdll) 뷰가 있나, 시작 = 칸 `+0x28` 인가 → Q94
  - **D4** `MiState+0x1000` = 0번 칸 `+0x20` 인가 → Q94
  - **D5** 뷰 헤더 `ImageBase` 가 새 주소로 바뀌었나 (정적 `[공백]` — MM 줄 못 찾음) → Q94
  - **D6** `NtGlobalFlag` 비트 `0x40000` 되돌림, `ExpSysDbgPulledFileTable` ≠ 0 인가 (**디버거가 붙어서 달라지는 짐**) → Q94
  - **D7** `[1]`·`[4]` 칸이 채워졌나 (= 실제로 찾아 올렸나) → Q94
  - **D8** `VslVsmEnabled` (0 이면 `Ⓑ4` 안 탐) → Q94
- **B. 부팅 디버깅 필요** (낮은 우선순위, Q94): **D9** `PspLocateSystemDll`·`PspMapSystemDll` 실제 호출 순서·인자 · **D10** `MmMapViewOfSectionEx` 반환 (`0` / `0x40000003`) · **D11** `ZwSystemDebugControl` 반환·`NtGlobalFlag` 임시 비트 · **D12** `MmCreateSpecialImageSection` 인자와 패킷 서명 수준.
- **정적 `[공백]`** (결과서 4장, 대기열은 오케스트레이터): 뷰 헤더 `ImageBase` 를 고치는 MM 줄 · `\SystemRoot` 링크 만드는 곳 (`_calls.tsv` 에 `Phase1InitializationDiscard +0xc31 CreateSystemRootLink` 줄 — 이름으로 본 후보 `[추측]`) · 서명 검사 본체 · `MmMapViewOfSectionEx` 할당 종류 `0x40000`·`0x20000000` 뜻 · `MiGetControlAreaLoadConfig` 칸 이름 · `PsInitSystem(1)` 인자 · `PspInitPhase3` VBS 본문 · 표 `[2][3][5]` 런타임 쓰기 (D1) · `PsWowX86SharedInformation` 읽는 자리.

## 8. 근거 run

| run | 무엇을 채웠나 |
|---|---|
| run43 | **첫판** (sast/38 체크리스트): 구조체 21개 칸 목록 (직접 칸 1,456), ntdll 전역 493 (`.data` 438 · `.mrdata` 50 · `.00cfg` 5), 초기화 함수 15개 callee 637줄, 이미 아는 쓴 자리 대조 (칸 정확 138 · 짐작 43 · 모름 1,275, 전역 54). `MmCreateTeb` 첫 export. `LdrSystemDllInitBlock` 블록 타입 없음 확인 |
| run44 | **둘째판 — 사진** (dast/03 한 번의 실행 8 지점 K1·K2·K3·P0·P1·P1a·P1b·P2 스냅샷, dast/04 덤프 읽기): 기준·쓰는 말 (사진 지점·구간), 1장 `[0 N]`, 2장 "0 그대로 (사진)" (구조체 847 · 전역 297) + **2.1 구간 표** (사진으로 정해진 칸 구조체 1,428/1,456 · 전역 332/493), 3장 243줄 근거 끝 "사진" + 절마다 "사진 (run44)", 4장 짝 18줄 `[관찰]` 값, 5.0 구간 순서, 7.1·7.2·7.3 구간별로 다시 묶음, 7.4·7.5·7.7 값 덧붙임, 7.8 새 빈 곳 (`ProcessParameters` 옮김 등) |
| run45 | **셋째판 — 일 목록** (sast/39, 정적만): 7.6 체크리스트 밖 짐 C1~C12 (PDB 이웃 타입 정의 있음/없음 확정, 사용자 결정대로 Q79·Q82 / 기록만 + 관련 Q83·Q84·Q86), 본문을 읽어 닫은 6건 → 3장 8줄 (모르던 칸 7개가 아는 칸) · 2장 숫자 (구조체 아는 칸 181 → 183, 전역 54 → 59) · 7.2·7.3 목록 · 7.5·7.7·7.8 `(해결됨)`, 4장 매개변수 짝, 5장 ② 순서 4곳, 6장 공용 부품 후보 첫 판 가리킴. run44 "161" 두 뜻 정정 (7.1·7.8·2.1) |
| run46 | **넷째판 — Q78a 프로세스 힙 구간** (sast/40, 정적만, 값 대조는 run44 P1a 덤프 로그): 3.1 PEB 힙 4칸 (`ProcessHeap` 쓰는 줄 `LdrpInitializeProcessHeap +0x1a2`, 목록 3칸 새로) · 3.8 힙 전역 17개 + 목록 밖 자리 4줄 (`LdrpHeap` 뜻 고침) · 2장 숫자 (구조체 아는 칸 183 → 186, 전역 59 → 76) · 4장 VAD 짝 3줄 + IFEO 키 핸들 · 5장 ② 힙 순서 · 6장 공용 부품 첫 표 · 7.2·7.3 목록 · 7.5 이름 없는 자리·조건부 전역 · **7.6.1 `_HEAP` 머리 칸 표** (체크리스트 밖) · 7.9 동적 질문 D1~D13 |
| run47 | **다섯째판 — Q78b exe 섹션 묶음** (sast/41, 정적만, 값 대조는 run44 K1 덤프 로그): 3.20 ~ 3.24 다시 씀 (20 → 85줄, 모르던 74칸 → 아는 칸 63 · 아무도 안 씀 11, 여전히 모름 0) · 정정 2줄 (3.21 `NumberOfUserReferences` = `MiReferenceControlArea +0x11d`, `u` 정확) · 3.23 "오프셋 미상" 14줄 · 2장 숫자 (구조체 아는 칸 186 → 249, 모름 1,270 → 1,196, 다른 DLL 에 드라이버 5) · 쓰는 말 (`다른 드라이버`, `아무도 안 씀`) · 3장 줄임 넷 (`①S…`·`①SC…`·`①SN…`·`①F…`) · 5장 ① 파일 객체·섹션·핸들 닫기 · 6장 커널 공용 부품 · 7.2·7.1·7.4·7.6·7.7 · 7.10 동적 질문 D1~D10 |
| run48 | **여섯째판 — Q93 부팅 준비 갈래** (sast/42, 정적만, 값 대조는 run44 K1 덤프 로그 `PsNtdllExports`): 쓰는 말 단계 **`Ⓑ`** (부팅, 프로세스 생성 전 — `Ⓑ0` ~ `Ⓑ4`) · **7.6.2** 시스템 DLL 표·칸 (`PspSystemDlls` 6칸, 칸 0x40 9칸 = 파일 값 5 · nt 4), 섹션 객체·System 프로세스 뷰·`NtGlobalFlag` 임시 비트·`ExpSysDbgPulledFileTable` (디버깅 부팅만)·`PsNtdllExports` 21칸 (nt 19 · 안 씀 2)·`PsWowX86SharedInformation`·`MiState.Processes.SystemDllBase`·VBS 뷰, ① 에서 읽는 자리 짝 · 4장 짝 3줄에 출처 경로 + 1줄 (재배치량) · 3장 줄임 3개 (`Ⓑ…`·`ⒷL…`·`Ⓑ2…`) · 3장 근거 칸 끝 "값 출처" 4줄 (3.8 `LdrpSystemDllBase`, 3.15 `Rdx`·`Rip` 둘) · 5장 `Ⓑ` 순서 · 6장 부품 3줄 · 7.11 질문 D1 ~ D12. **2장 숫자 안 바뀜** (체크리스트 밖) |
| run10 (sast/04) | 3장 근거 줄: `_PEB` 7 · `_EPROCESS` 7 |
| run11 (sast/06) | 3장 근거 줄: `_EPROCESS` 2 · `_ETHREAD` 4 · `_KTHREAD` 1 |
| run12 | 3장 근거 줄: `_EPROCESS` 5 · `_ETHREAD` 2 |
| run13 | 3장 근거 줄: `_EPROCESS` 1 · `_KTHREAD` 9 |
| run15 | 3장 근거 줄: `_TEB` 1 · `_KTHREAD` 2 · `_KTRAP_FRAME` 2 |
| run16 | 3장 근거 줄: `_KTHREAD` 1 · `_KTRAP_FRAME` 10 |
| run17 (sast/12) | 3장 근거 줄: `_PEB` 2 · `_TEB` 1 · `.data` 전역 11 · `.mrdata` 전역 2 |
| run18 (sast/13) | 3장 근거 줄: `_PEB` 4 · `_RTL_USER_PROCESS_PARAMETERS` 1 · `_PEB_LDR_DATA` 3 · `_LDR_DATA_TABLE_ENTRY` 1 · `.data` 전역 11 · `.mrdata` 전역 1 |
| run19 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 4 · `_LDR_DDAG_NODE` 1 · `.data` 전역 7 · `.mrdata` 전역 2 |
| run20 | 3장 근거 줄: `.data` 전역 3 |
| run21 | 3장 근거 줄: `.data` 전역 3 |
| run22 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 9 · `_LDR_DDAG_NODE` 3 |
| run23 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 2 · `_LDR_DDAG_NODE` 4 |
| run24 | 3장 근거 줄: `_LDR_DDAG_NODE` 4 · `.data` 전역 18 |
| run26 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 1 · `.data` 전역 1 |
| run27 | 3장 근거 줄: `_PEB_LDR_DATA` 1 · `_LDR_DATA_TABLE_ENTRY` 1 · `_LDR_DDAG_NODE` 3 · `.data` 전역 1 |
| run28 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 2 · `.data` 전역 2 · `.00cfg` 전역 1 |
| run29 (sast/24) | 3장 근거 줄: `_KTHREAD` 1 · `_KTRAP_FRAME` 1 |
| run31 (sast/26) | 3장 근거 줄: `_CONTROL_AREA` 2 · `_SEGMENT` 1 · `_SECTION_IMAGE_INFORMATION` 17 (run47: `_CONTROL_AREA` 2줄 고침 — 함수 이름 정정·`u` 진짜 칸으로) |
| run32 (sast/27) | 3장 근거 줄: `_PEB` 1 · `_RTL_USER_PROCESS_PARAMETERS` 9 · `.mrdata` 전역 1 |
| run33 | 3장 근거 줄: `_EPROCESS` 3 |
| run34 | 3장 근거 줄: `_EPROCESS` 1 |
| run35 | 3장 근거 줄: `_PEB_LDR_DATA` 3 · `_LDR_DATA_TABLE_ENTRY` 19 · `_LDR_DDAG_NODE` 3 · `.data` 전역 3 |
| run36 | 3장 근거 줄: `_LDR_DDAG_NODE` 1 |
| run38 | 3장 근거 줄: `_TEB` 1 · `_LDR_DATA_TABLE_ENTRY` 1 |
| run39 | 3장 근거 줄: `.data` 전역 7 |
| run41 | 3장 근거 줄: `_TEB` 1 |
| run45 (sast/39) | 3장 근거 줄: `_PEB` 2 · `_RTL_USER_PROCESS_PARAMETERS` 3 · `.data` 전역 1 · `.mrdata` 전역 2 (근거 칸 `sast39`) |
| run46 (sast/40) | 3장 근거 줄: `_PEB` 4 · `.data` 전역 26 (근거 칸 `sast40`, 그중 목록 밖 자리 4) + 고친 줄 2 (`ProcessHeap`·`LdrpHeap`) |
| run47 (sast/41) | 3장 근거 줄: `_SECTION` 8 · `_CONTROL_AREA` 22 · `_SEGMENT` 12 (+ run31 1) · `_SECTION_IMAGE_INFORMATION` 26 중 새로 9 + 오프셋 채움 14 · `_FILE_OBJECT` 16 (근거 칸 `sast41`) |
| run48 (sast/42) | 3장 근거 줄: 새 줄 없음 (짐은 체크리스트 밖 → 7.6.2). 근거 칸에 "값 출처" 만 4줄 덧붙임 |
| `_refs` (ntdll 참조 표) | 3장 전역·`_PEB_LDR_DATA` 줄 대부분 (WRITE·READ_WRITE = 정확, DATA = 짐작) |

- 3장 근거 칸의 run 번호는 결과서 4번이 문서에서 옮긴 것이다 (해설서 `*-해설.md` 는 뺌). sast 번호는 그 run 의 결과서.
