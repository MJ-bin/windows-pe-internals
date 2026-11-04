# run65 ldr-init-bodies — 해설

> 이 글은 [run65 문서](run65-ldr-init-bodies.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q120b** — PE_emulator 요청 11 의 7번: "함께 쌓인 본문" (`LdrpInitializeProcess` 줄기 위 초기화 함수들 + 모듈 올리기 길 부품 + 공용 부품, 약 25개) (`_workspace/plan.md`, 이번에 **끝**)
  - 이걸로 **PE_emulator 요청 11 이 다 끝났다** (Q120a = run63, Q120c = run64, Q120b = run65).
  - 다음 항목: **Q104 VM 단계** (동적) → Q78d4 → …
  - 남은 질문이 간 곳: 동적 4건 → **Q104 VM 단계** (그중 1번 "클래스 `0xfa` 진짜 값" 이 가장 중요), 남은 작은 정적 + 사용자 결정 하나 → 새 **Q124** (맨 뒤).
- 근거:
  - run 문서 [`run65-ldr-init-bodies.md`](run65-ldr-init-bodies.md) (가장 중요)
  - 요청서 [`_workspace/sast/59_ldr-init-bodies.md`](../../_workspace/sast/59_ldr-init-bodies.md) — 머리에 "예정 run64" 라고 적혀 있지만, 같은 날 Q120c 가 먼저 끝나 run64 가 됐다. 이 글은 **run65** 다.
  - 결과서 [`_workspace/sast/59_ldr-init-bodies_result.md`](../../_workspace/sast/59_ldr-init-bodies_result.md) (판정: **분석성공**, PDB 일치, 질문 13개 모두 답)
  - flow.md 변경: 기준 · 그림 2 상자 둘 (`p3`·`p4c`) 의 **글만** · 2.4 트리 줄 · 2.4 새 절 "`\KnownDlls`·정책·현재 디렉터리·CPU 기능 비트맵·스케줄러 공유 데이터·TLS 준비" · 2.5 KnownDll 본문 · 2.7 TEB 줄 · 4장 · 5장
  - 짐 지도 변경: 3.2·3.3·3.8·3.9·7.3·7.6.5 (3절에서 짧게)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다.
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 `ms/logs/04_baggage-read_P1a.txt` 와 짐 지도 7.3 사진 분류를 **다시 읽은 것** 이다. 새 실행이 아니다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 정보 클래스 (`NtQuery…`/`NtSet…` 에 넘기는 번호) → [run51 해설 2.2](run51-eprocess-later-해설.md)
- 프로세스 매개변수 블록 = **개업 서류** (`ProcessParameters`) → [run32 해설 2.3](../02_sprint/run32-params-initblock-해설.md#23-새-말--프로세스-매개변수-블록)
- IFEO = **가게 이름별 특별 지시** → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 3.4
- KUSD = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 프로세스 힙 = **창고** → [run46 해설](run46-process-heap-해설.md)
- TLS = 직원 **개인 사물함**, TEB `+0x58` = 사물함 **열쇠꾸러미**, `LdrpTlsList` = 관리실 **TLS 서류철** → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) 2.3 · [run54 해설](run54-teb-fill-해설.md)
- TEB `+0x1850` `SchedulerSharedDataSlot` = 직원마다 받는 **열쇠 대출 카드** (8칸) → [run41 해설](../02_sprint/run41-tls-lock-detail-해설.md) 2.3
- 붉은-검은 트리 = 명부의 **정렬된 카드 상자** (색인함 둘) → [run35 해설 2.7](../02_sprint/run35-ntdll-entry-해설.md)
- `\KnownDlls` = **동네 공용 창고 위치** → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 2.4
- 섹션 객체 = 커널이 들고 있는 **조립식 건물 세트** → [run31 해설](../02_sprint/run31-image-section-해설.md) 2절
- 사진 P1a, `≤P1a` / `끝까지 0` 사진 분류 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가) · [run60 해설](run60-func-table-mrdata-해설.md)
- 빠른 실패 `INT 0x29` → [run24 해설](../02_sprint/run24-work-queue-해설.md) 2.3

이번에 처음 나오는 말 (**현재 디렉터리 레코드**, **비트맵 머리 `_RTL_BITMAP`**, **CPU 기능 비트맵**, **스케줄러 공유 데이터**, **TLS 비트맵**, **노드 넣기 칸 `_RTL_BALANCED_NODE`**, **장치 특성 마스크**) 은 본문에서 처음 나오는 자리에서 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소 **신청 창구**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**.
- 매개변수 블록 = **개업 서류**, 프로세스 힙 = **창고**, 핸들 = 관리사무소 물건을 가리키는 **번호표**.
- 이번 바퀴는 관리실이 개업 직전에 하는 **"살림 차리기" 뒷부분** 이다. 칠판 여기저기에 값을 적고, 창구에 서류 몇 장 내고, 창고에서 상자 몇 개를 꺼낸다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| 이 구간에서 창구에 몇 번 가나? | test.exe 기본 길에서 **8번** — `\KnownDlls` 4 · 현재 디렉터리 2 · CPU 기능 1 · 스케줄러 공유 데이터 1 `[정적]` (2.2) |
| **PE_emulator 와 진짜 VM 은 어디서 갈렸나?** | `LdrpInitializeProcess +0x1563` 의 문자열 해제는 **현재 디렉터리가 비었을 때만** 탄다. 진짜 VM 은 안 탄다 `[정적]` + `[관찰]` (2.4) |
| **TEB `+0x1850` 은 누가 쓰나?** | `LdrpAllocateSchedulerSharedData` 가 **아니다**. 바로 다음 `LdrpAcquireSchedulerSharedDataSlot +0x92` 다 `[정적]` (2.9) |
| 클래스 `0xfa` 는 무엇을 돌려주나? | 커널 `KeProcessorFeaturesBuffer` 16바이트 복사. 정적으로는 **모두 0** `[정적, 반증 가능]` (2.8) |
| test.exe 의 TLS 준비는? | exe 하나만 본다 (`N = 1`) → 비트맵 9비트, TLS 힙 = 프로세스 힙, 벡터 `0x58`B → TEB `+0x58` `[정적]` + `[관찰]` 맞음 (2.10) |
| 정책·앱 폴더 검사는? | `LdrpInitializePolicy` 는 거의 빈 일. `LdrpIncludeAlternateForwarders`·`LdrpCheckAppDirType` 은 **아예 안 불린다** `[정적]` (2.5 · 2.6) |
| 앞선 run 과 어긋난 곳은? | **없다.** 정정 없음 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

`LdrpInitializeProcess` 의 **중간 ~ 뒤쪽**이다. 명부에 ntdll·exe 를 적고 난 다음, import DLL 을 들이기 **전**.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스·주소 공간·PEB·첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk                         (사진 P1)
 │    └─ LdrpInitializeProcess
 │         ├─ … 명부 첫 줄 = ntdll (run35)
 │         ├─ +0xdbd  \KnownDlls 열기 (넷)                                    ← 이번 ①
 │         ├─ +0xfca  LdrpInitializePolicy  (DLL 정책)                        ← 이번 ②
 │         ├─ (+0x1183 LdrpCheckAppDirType — 앱 폴더 검사, test.exe 안 탐)     ← 이번 ③
 │         ├─ … 명부 둘째 줄 = exe
 │         ├─ +0x151c RtlpInitCurrentDir    (현재 디렉터리)                    ← 이번 ④
 │         ├─ (+0x1568 문자열 해제 — 진짜 VM 안 탐)                            ← 이번 ★갈림길
 │         ├─ +0x1766 RtlInitializeProcessorFeaturesBitMap (CPU 기능)         ← 이번 ⑤
 │         ├─ +0x182c LdrpAllocateSchedulerSharedData                         ← 이번 ⑥
 │         ├─ +0x183c LdrpAcquireSchedulerSharedDataSlot → TEB+0x1850 (run41) ← ★주인
 │         ├─ +0x18d0 LdrpInitializeTls                                       ← 이번 ⑦
 │         └─ … kernel32 → import DLL → TLS 콜백 → 진입점 (run19 ~ run28)
 │  (사진 P1a)
 └─ … → NtContinue → exe 진입점 → main
```

### 왜 이 질문이 지금 왔나

- PE_emulator 는 진짜 ntdll 코드를 Unicorn (CPU 흉내 내는 프로그램) 안에서 돌린다. 그러다 이 구간 함수들에서 **덫에 걸렸거나**, 몸통을 모르는 함수를 "공사 중" 빈 함수로 두었다.
- 함수마다 알고 싶은 건 다섯: **인자 · 쓰는 칸 · 하위 호출 · syscall · 반환**.
- 특히 둘이 이상했다:
  - PE_emulator 에선 `RtlFreeUnicodeString` 이 불렸고, 그 뒤 버퍼 첫 8바이트가 바뀌었다 `[관찰]` (PE_emulator — VM 관찰 아님).
  - PE_emulator 의 TEB `+0x1850` 은 0 인데, 진짜 VM 사진에선 0 이 아니었다 `[관찰]` (run44).

### 무엇이 궁금했나

- 묶음 A (`LdrpInitializeProcess` 줄기 위, 자세히): `\KnownDlls` 스텁 셋 · `LdrpInitializePolicy` · `LdrpCheckAppDirType` · `RtlpInitCurrentDir` · `RtlInitializeProcessorFeaturesBitMap` (+ 커널 클래스 `0xfa`) · `LdrpAllocateSchedulerSharedData` · TLS 묶음.
- 묶음 B (모듈 올리기 길 부품, 보통): `LdrpValidateEntrySection` · `LdrpIsImageAddress` · `RtlRbInsertNodeEx` · `LdrpFindKnownDll` · `LdrpLoadKnownDll` · `LdrpIllegalCWDDevices`.
- 묶음 C (공용 부품, 짧게): 문자열 부품 넷, `LdrControlFlowGuardEnforced`.

### 바퀴 전에 알던 것

- `[정적]` (run18) `+0xdb8` 자리에서 `\KnownDlls` 를 연다. 경로 문자열만 읽고, 공용 창고 안 섹션은 이 자리에서 안 연다.
- `[정적]` (run19) `+0x151c RtlpInitCurrentDir` 은 실패해도 로그만 찍고 계속한다.
- `[정적]` (run41) TEB `+0x1850` 이름은 `SchedulerSharedDataSlot`, 넣는 곳은 `LdrpAcquireSchedulerSharedDataSlot +0x92` 하나. 앞의 `LdrpAllocateSchedulerSharedData` 본문은 **안 봤다**.
- `[정적]` (run28) TLS 길, (run35) 명부 색인 = 트리 둘.
- `[관찰]` (run44 사진) TEB `+0x1850 ≠ 0`, 옛 매개변수 자리에 2쪽짜리 매핑 섹션이 생김.
- `[정적]` (run63) `LdrControlFlowGuardEnforced` 는 `LdrSystemDllInitBlock+0xb8` 로 정해지고 test.exe 에서 0.

---

## 2. 찾은 것

### 2.1 묶음 A 를 시간 순서로 — 한 줄씩 "무엇을 놓나"

먼저 큰 줄기만. 아래 일곱 줄이 이번 바퀴의 뼈대다. 2.3 ~ 2.10 에서 하나씩 푼다.

| 순서 | 자리 (call / ret) | 함수 | 무엇을 놓나 (test.exe) | syscall | 태그 |
|---|---|---|---|---|---|
| ① | `+0xdb8 / +0xdbd` ~ `+0xf04 / +0xf09` | `\KnownDlls` 스텁 셋 + `NtClose` | `.mrdata` `LdrpKnownDllDirectoryHandle` (디렉터리 핸들) · `.data` `LdrpKnownDllPath` (경로 문자열) | 4 | `[정적]` |
| ② | `+0xfc5 / +0xfca` | `LdrpInitializePolicy` | `LdrpAppPackagesPath`·`LdrpOriginalAppPackagesPath` 를 0 으로 (원래 0) — 그리고 끝 | 0 | `[정적]` |
| ③ | `+0x117e / +0x1183` | `LdrpCheckAppDirType` | **안 불림** (마스크 0) | 0 | `[정적]` |
| ④ | `+0x1517 / +0x151c` | `RtlpInitCurrentDir` | 현재 디렉터리 **레코드** (힙 `0x238`B) · `RtlpCurDirRef` · 매개변수 `CurrentDirectory` 칸 셋 · 디렉터리 핸들 | 2 | `[정적]` |
| ⑤ | `+0x1761 / +0x1766` | `RtlInitializeProcessorFeaturesBitMap` | `RtlProcessorFeaturesBuffer` (커널이 채움) · `RtlProcessorFeaturesBitMap = {0x80, &버퍼}` | 1 | `[정적]` |
| ⑥ | `+0x1827 / +0x182c` | `LdrpAllocateSchedulerSharedData` | 커널 쪽 섹션·매핑·핸들 · 노드 `0x18`B 를 `LdrpSchedulerSharedDataListHead` 에 | 1 | `[정적]` |
| ⑦ | `+0x18cb / +0x18d0` | `LdrpInitializeTls` | `LdrpTlsHeap` · `LdrpTlsBitmap` (9비트) · TLS 서류철 항목 · 벡터 → TEB `+0x58` | 0 | `[정적]` |

- `+0xfe4 / +0xfe9 LdrpIncludeAlternateForwarders` 도 ②와 ③ 사이에 있지만 test.exe 에선 **안 불린다** (2.5).
- ⑥과 ⑦ 사이에 `+0x1837 / +0x183c LdrpAcquireSchedulerSharedDataSlot` 이 있다. 이건 run41 몫이지만 ⑥과 한 사슬이다 (2.9).

비유로 한 번:

- ① 동네 공용 창고 **위치 쪽지**를 받아 칠판에 붙인다.
- ② "이 가게만의 DLL 정책" 칸을 비운다. 우리 가게는 특별 정책이 없어서 그걸로 끝.
- ③ "가게 건물이 놓인 땅 종류" 를 볼 필요가 없어서 건너뛴다.
- ④ 지도 위 **"현재 위치" 핀**을 꽂는다 (현재 디렉터리).
- ⑤ 관리사무소에 "이 CPU 에 무슨 기능이 있나요?" 확장 목록을 받아 칠판에 건다.
- ⑥ 관리사무소에 "직원들 카드 묶음 상자 하나 주세요" 를 신청해 번호표를 받아 둔다.
- ⑦ 직원 개인 사물함 제도를 준비하고, 첫 직원에게 열쇠꾸러미를 쥐여 준다.

### 2.2 창구에 가는 8번 — 표 하나로

run 문서 표 그대로다 `[정적]`.

| # | syscall (번호) | 클래스·인자 | 부르는 자리 (call / ret) | 결과를 쓰는 곳 |
|---|---|---|---|---|
| 1 | `NtOpenDirectoryObject` (`0x58`) | 접근 `3`, 이름 `\KnownDlls`, 속성 `0x40` | `LdrpInitializeProcess +0xdb8 / +0xdbd` | `.mrdata` `LdrpKnownDllDirectoryHandle` |
| 2 | `NtOpenSymbolicLinkObject` (`0x138`) | 접근 `1`, Root = 위 핸들, 이름 `KnownDllPath` | `+0xe47 / +0xe4c` | 지역 링크 핸들 |
| 3 | `NtQuerySymbolicLinkObject` (`0x16b`) | 링크 핸들, `&LdrpKnownDllPath` (처음 버퍼 `0x30`B) | `+0xe90 / +0xe95` | `.data` `LdrpKnownDllPath` |
| 4 | `NtClose` (`0xf`) | 링크 핸들 | `+0xf04 / +0xf09` | — |
| 5 | `NtOpenFile` (`0x33`) | 접근 `0x100020`, 공유 `3`, 옵션 `0x21`, 속성 `0x40`, 이름 = 현재 디렉터리 NT 경로 | `RtlpCreateNewDirectoryReference +0xbb / +0xc0` | 레코드 `+0x8` → 매개변수 `CurrentDirectory.Handle` (사진 `0x58`) |
| 6 | `NtQueryVolumeInformationFile` (`0x49`) | 클래스 `4` (`FileFsDeviceInformation`), 길이 8 | `RtlpCreateNewDirectoryReference +0xf7 / +0xfc` | 레코드 `+0x28` (장치 Characteristics) |
| 7 | `NtQuerySystemInformation` (`0x36`) | 클래스 **`0xfa`**, 버퍼 `&RtlProcessorFeaturesBuffer`, 길이 `0x10` | `RtlInitializeProcessorFeaturesBitMap +0x25 / +0x2a` | `.data` `RtlProcessorFeaturesBuffer` (커널이 직접 씀) |
| 8 | `NtSetInformationProcess` (`0x1c`) | 핸들 `-1`, 클래스 **`0x70`**, 버퍼 `&지역` (출력), 길이 `8` | `LdrpAllocateSchedulerSharedData +0x2c / +0x31` | 지역 → 노드 `+0x10` (섹션 핸들) |

- 이 8번 중 **커널 쪽에 새 짐을 만드는 것** `[정적]`:
  - `\KnownDlls` 디렉터리 핸들 — `0x4c` `[관찰]` (run44 P2 `!handle`)
  - 현재 디렉터리 핸들 — `0x58` `[관찰]`
  - 스케줄러 공유 데이터 섹션·매핑·핸들 — `[관찰]` run44 의 2쪽 매핑 섹션 VAD
- 안 타는 syscall (test.exe 길): `LdrpCheckAppDirType` 의 `NtOpenFile`·`NtQueryVolumeInformationFile`·`NtClose` 한 벌, 실패 길 `NtClose` 둘, `LdrpIsImageAddress` 의 `NpQueryVirtualMemory` (`0x23`).
- `LdrpFindKnownDll` 의 `NtOpenSection` (`0x37`) 과 `LdrpLoadKnownDll` 끝 `NtClose` 는 "KnownDll 로 올리는 모듈 수" 만큼 — **횟수는 동적** (4절).

#### 한 줄 읽기 — 7번 줄 (`RtlInitializeProcessorFeaturesBitMap +0x25 / +0x2a`)

- **누가**: `RtlInitializeProcessorFeaturesBitMap` (66바이트짜리 작은 함수). 관리실이다.
- **어디서**: call `+0x25`, ret `+0x2a`. 바로 앞 줄들에서 인자를 싣는다 (오케스트레이터가 `.asm` 으로 다시 본 줄):
  - `+0xb LEA RBX,RtlProcessorFeaturesBuffer` (받을 칸 주소)
  - `+0x1a MOV R8D,0x10` (길이 16바이트)
  - `+0x20 MOV ECX,0xfa` (클래스 번호)
- **누구를**: `NtQuerySystemInformation` → 창구 번호 `0x36` → 커널 `ExpQuerySystemInformation`.
- **무엇을 위해**: CPU 기능 확장 목록 16바이트를 관리실 칠판 (`RtlProcessorFeaturesBuffer`) 에 **커널이 직접** 써 넣게 하려고.
- 돌아온 뒤 `+0x2a`·`+0x35` 에서 칠판에 **비트맵 머리**를 단다 (2.8).

### 2.3 ① `\KnownDlls` — 공용 창고 위치 쪽지 받기

**새 말 — `\KnownDlls` (한 줄 다시)**: 자주 쓰는 시스템 DLL 을 미리 준비해 두는 커널 쪽 **이름 디렉터리**로 알려져 있다 `[추측]` (일반적으로 알려진 설명, [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 2.4 와 같은 말). 비유: 동네 **공용 창고**. 창고 안에는 조립식 건물 세트(섹션)가 이름표를 달고 서 있다.

run18 은 "여기서 창고를 연다" 까지 봤다. 이번엔 **창구 번호와 인자, 결과를 적는 칸**까지 붙었다 `[정적]`.

1. `+0xdb1 LEA RCX,[LdrpKnownDllDirectoryHandle]` → `+0xdb8 NtOpenDirectoryObject(&칸, 접근 3, "\KnownDlls")`.
   - 첫 인자가 칠판 칸 주소다. 즉 **커널이 그 칸에 핸들을 직접 써 준다** (출력 칸).
   - 짐 지도 3.9 의 `[추측]` "출력 칸일 것" 이 이 줄로 `[정적]` 이 됐다 (3절).
2. `+0xe47 NtOpenSymbolicLinkObject` — 창고 안 **링크** `KnownDllPath` 를 연다 (Root = 위 핸들).
3. `+0xe90 NtQuerySymbolicLinkObject` — 링크가 가리키는 **경로 글자**를 `LdrpKnownDllPath` 에 받는다.
   - 처음엔 창고(프로세스 힙)에서 `0x30`바이트를 꺼내 받을 자리로 준다 (`+0xd65`).
   - 자리가 모자라다는 답 (`0xc0000023`) 이 오면, 버리고 돌아온 길이만큼 다시 꺼내 한 번 더 (`+0xea2 JNZ`).
   - test.exe 에서 한 번에 끝나는지는 `[추측]` (대상이 `0x30`B 이내일 때).
4. `+0xf04 NtClose` — 링크 번호표 반납.

- 넷 다 **`PEB.IsProtectedProcess` 이면 건너뛴다** (`+0xd54/+0xd57`). test.exe 는 보통 프로세스라 탄다.
- 비유: 공용 창고 **열쇠 번호표**(디렉터리 핸들)는 칠판 유리 덮개 칸(`.mrdata`)에 붙여 두고, 창고 **주소 쪽지**(`C:\…` 경로)는 칠판에 적는다. 쪽지 글자 자체는 사진에 없어서 `C:\WINDOWS\System32` 로 짐작만 했다 `[추측]` (짐 지도 7.24).

### 2.4 ★ 갈림길 — PE_emulator 와 진짜 VM 이 갈린 곳 (`+0x1563`)

이번 바퀴에서 **가장 먼저 봐야 할 것**이다. PE_emulator 가 본 이상한 해제가 왜 생겼는지 설명된다.

#### 무엇이 이상했나

- PE_emulator 에선 `LdrpInitializeProcess +0x1563` 에서 문자열 해제 함수가 불렸다 `[관찰]` (PE_emulator, Unicorn — VM 관찰 아님).
- 해제 뒤 그 문자열 머리 16바이트가 0 이 되고, 버퍼 첫 8바이트가 바뀌었다 `[관찰]` (PE_emulator).

#### 코드는 이렇게 생겼다 — 단계별로 `[정적]`

1. **표시 칸 하나**: 지역 칸 `[RSP+0x5c]` 에 처음 **1** 을 넣는다 (`+0x32f`).
   - 비유: 관리실 메모지에 "현재 위치 핀, 원래 있던 것 씀" 에 체크.
2. **비었나 검사** (`+0xf44 ~ +0xf53`): 매개변수 `CurrentDirectory.DosPath` 사본이 다음 중 하나면 "비었다" 로 본다.
   - `Buffer` 가 NULL
   - `Length` 가 0
   - 첫 글자가 NUL
3. **빈 갈래** (`+0xf55 ~ +0xfb2`) — 비었을 때만:
   - 창고에서 8바이트를 꺼낸다 (`RtlAllocateHeap(힙, 0, 8)`, call `+0xf5e`).
   - 시스템 루트 (`RtlGetNtSystemRoot`, call `+0xf89`) 의 **앞 3글자 `C:\`** 를 넣는다 (`Length 6 / Max 8`, `+0xfa7`).
   - 표시 칸을 **0** 으로 바꾼다 (`+0xf84`).
   - 비유: 핀이 없으니 "일단 C 드라이브 맨 위" 에 임시 핀을 꽂고, 메모지에 "임시 핀 만들었음 — 나중에 치울 것" 표시.
4. **치우기** (`+0x1554 CMP [RSP+0x5c],R15D` · `+0x1559 JNZ`): 표시가 0 일 때**만** `+0x1563` 에서 해제한다. 해제하는 건 3번의 **8바이트 임시 버퍼**다.

#### 진짜 VM 은 이 갈래를 안 탄다

- 사진 P1a: `CurrentDirectory.DosPath` = `Length 0x84`, `MaximumLength 0x208`, `Buffer 0x25996f381c0`, `Handle 0x58` `[관찰]` (`04_baggage-read_P1a.txt:2552 ~ 2553`).
- `Length` 가 0 이 아니다 → 2번 검사에서 "비지 않음" → 3번 안 탐 → 표시 칸은 1 그대로 → 4번 해제 **안 탐** `[정적]` + `[관찰]`.

#### 그래서 PE_emulator 에겐 무슨 뜻인가

- 근거: 해제는 "빈 갈래를 탔을 때만" 이다 `[정적]`.
- 그래서: PE_emulator 가 이 해제를 봤다면, **입력 `ProcessParameters` 의 현재 디렉터리가 비어 있었다**는 뜻이다 (run 문서).
- 진짜 VM 처럼 가려면 `CurrentDirectory.DosPath` 에 exe 폴더 (`C:\…\sample\bin\`, 끝 `\` 포함) 를 넣어 주면 된다 (run 문서).
- 비유: 에뮬레이터 가게는 개업 서류의 "현재 위치" 칸을 빈칸으로 냈다. 그래서 관리실이 임시 핀을 꽂았다 치운 것이다. 진짜 가게는 서류에 위치가 적혀 와서 그럴 일이 없었다.

#### 해제 함수 자체는 무엇을 하나 `[정적]`

- 같은 주소(rva `0x4e200`) 에 이름이 3개 붙어 있다: `RtlFreeUnicodeString`·`RtlFreeAnsiString`·`RtlFreeUTF8String`. 셋 다 후보다.
- 하는 일은 둘뿐:
  - `Buffer != NULL` 이면 `RtlFreeHeap(PEB.ProcessHeap, 0, Buffer)` (`+0x12/+0x17`).
  - 문자열 머리 16바이트를 모두 0 으로 (`+0x17 XORPS` · `+0x1a MOVUPS`).
- PE_emulator 실측 "머리 16바이트가 0" → **코드와 맞다**.
- "버퍼 첫 8바이트가 바뀜" → 이 함수가 아니라 **`RtlFreeHeap` 안 (힙 관리자)** 의 일이다. 이 함수는 버퍼 안을 안 건드린다.
  - 힙 관리자가 정확히 무엇을 쓰는지는 run46 범위라 이번에 안 정했다 → 4절 (동적 4).
  - 비유: 상자를 창고에 돌려주면 **창고지기**가 상자 안에 자기 메모를 끼워 넣는다. 반납 담당(해제 함수)은 상자 겉 라벨만 지운다.

### 2.5 ② 정책 — `LdrpInitializePolicy` 는 test.exe 에서 거의 빈 일

`[정적]` 인자·반환 없음. 하는 일 순서:

1. 두 전역 `LdrpAppPackagesPath`·`LdrpOriginalAppPackagesPath` (각 `UNICODE_STRING` 16바이트) 의 앞·뒤를 0 으로 (`+0x22`·`+0x2d`·`+0x34`·`+0x3b`). 원래 0 이라 값은 안 바뀐다.
2. PEB `BitField` 를 본다.
   - 비트 4 (`IsPackagedProcess`) 면 → **패키지 앱 갈래** (`+0x72 ~`).
   - 비트 1 (`IsProtectedProcess`) 면 → `DllPath.Length = 0` 하고 끝.
3. `DllPath.Length != 0` 이면 `LdrSetDllDirectory(&DllPath)`.

- test.exe: 사진 `BitField = 0x4`, `DllPath = ""` `[관찰]` → `+0x4e` 에서 바로 끝. syscall 없음.
- 패키지 앱 갈래 (안 탐, 이름만): `RtlQueryPackageClaims` 다섯 번, 정책 전역 `LdrpPolicyBits` 의 비트를 켜고 끔, `LdrpDefaultDllDirectories = 0x1000`, 매개변수 `Flags` 비트 26 (`0x4000000`).
- 정책 전역 이름 = **`LdrpPolicyBits`** (rva `0x1cd7fc`, 파일 값 `0x16`). test.exe 에선 **안 바뀐다**.
  - 짐 지도에서 이걸 "쓴 자리 아는 칸" 으로 옮길지는 다음 바퀴에 정한다 (Q124 (a)).

**`LdrpIncludeAlternateForwarders` 도 안 불린다** `[정적]`:

- 부르는 조건: 매개변수 `Flags & 0x20000000` (`+0xfdb/+0xfe2`). 사진 `Flags = 0x4001` `[관찰]` → 꺼짐 → 안 탐.
- 불리면 `Flags & 0x4000000` 이나 `PEB.NtGlobalFlag2` 비트 1 을 보고 TRUE/FALSE 를 돌려준다. 그 결과로 `RtlpSystemDirs` 에 `forwarders\alt;` 를 붙일지 정한다 (`+0x10e9`).

비유: "이 가게만의 DLL 찾는 규칙" 칸. 우리 가게는 앱 스토어 가게도, 보호 가게도 아니라서 칸만 한 번 지우고 끝.

### 2.6 ③ 앱 폴더 검사 — `LdrpCheckAppDirType` 은 안 불린다

**새 말 — 장치 특성 마스크 (`LdrpIllegalCWDDevices`)**: 이름만 보면 "금지 장치 이름 목록" 같지만, **4바이트 마스크**다 `[정적]`. 비트 하나하나가 장치 종류 특성이다. DLL 을 찾을 때 현재 디렉터리를 뺄지 정하는 쪽이 읽는다 (결과서).

- 비유: "이런 종류의 땅 위에 있는 현재 위치 핀은 DLL 찾을 때 믿지 마" 체크 칸.

`[정적]` 이 마스크를 누가 정하나:

- `LdrpQueryIllegalCWDDevices` (`LdrpInitializeExecutionOptions +0x733` 이 부름) 가 정한다.
  - IFEO `CWDIllegalInDLLSearch` 값이 있으면 그 값.
  - 없으면 KUSD `+0x2d5` 비트 4~5 `CurDirDevicesSkippedForDlls`: `0` → 0, `1` → `0x2000`, `2` → `0x10`, `3` → −1.
- 사진 `CurDirDevicesSkippedForDlls = 0y00` `[관찰]` → 마스크 0.

`[정적]` `LdrpCheckAppDirType` 이 하는 일 (안 타지만):

- exe 경로를 열고 (`NtOpenFile`), 장치 특성을 묻고 (`NtQueryVolumeInformationFile` 클래스 4), 닫는다.
- `마스크 & 특성` 이 0 이 아니면 마스크를 0 으로 비운다 (`+0x11b`).

부르는 조건 (`+0x1169 ~ +0x1174`): 마스크가 **0 도 −1 도 아닐 때**만. test.exe 는 0 → **안 불린다**.

- 남은 물음: IFEO 키가 없을 때 `+0x733` 까지 실제로 닿는지는 이번에 안 봤다 (run46 은 IFEO 를 건너뛴다고 했다). 마스크는 어느 쪽이든 0 이다 (BSS 처음 값 0, 사진 `끝까지 0*`) → Q124 (b).

### 2.7 ④ 현재 디렉터리 — "현재 위치" 핀 꽂기

**새 말 — 현재 디렉터리**: 프로그램이 경로 없이 파일 이름만 줄 때 "여기서부터 찾는다" 의 기준 폴더로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 지도 위 **"현재 위치" 핀**.

**새 말 — 현재 디렉터리 레코드**: 관리실이 핀 하나마다 창고에서 꺼내 쓰는 **작은 상자**다. 그 폴더를 연 번호표(핸들)와 경로 글자를 같이 담는다. 칸 오프셋은 `[정적]`, 칸 뜻은 PDB 에 이름이 없어 `[추측: 뜻]` 이다.

#### 순서 `[정적]`

```
LdrpInitializeProcess +0x1517 / +0x151c  RtlpInitCurrentDir(현재 디렉터리 사본)
   ├─ +0x1c / +0x21  RtlDetermineDosPathNameType_Ustr     경로 종류 1·2·6·7 만 통과 (아니면 0xc000000d)
   └─ +0x32 / +0x37  RtlpCreateNewDirectoryReference(경로, 최대 길이, &레코드)
           ├─ +0xbb / +0xc0    NtOpenFile                 (syscall 0x33, 폴더 열기)
           ├─ +0xd5 / +0xda    RtlFreeHeap (NT 경로 버퍼 — 열기 성공과 무관)
           ├─ +0xf7 / +0xfc    NtQueryVolumeInformationFile (syscall 0x49, 클래스 4 — 장치 특성)
           └─ +0x115 / +0x11a  RtlAllocateHeap (레코드 0x30 + 최대 길이)
   돌아와서: +0x47 RtlpCurDirRef · +0x52 Handle · +0x5a Buffer · +0x62 Length
```

#### 레코드 칸 (힙 `0x30 + 최대 길이`, 사진 기준 `0x238`B)

| 칸 | 내용 | 비유 |
|---|---|---|
| `+0x0` | 참조 수 1 | 이 상자를 쓰는 사람 수 |
| `+0x8` | 디렉터리 핸들 | 그 폴더 **번호표** |
| `+0x10` | KUSD `DismountCount` 복사 | 공용 게시판의 "드라이브 빠짐 횟수" 를 적어 둠 |
| `+0x18` / `+0x1a` | 길이 / 최대 길이 | |
| `+0x20` | 버퍼 포인터 (= 레코드 `+0x30`) | 글자는 상자 안쪽에 |
| `+0x28` | 장치 특성 | 2.6 의 마스크와 견줄 값 |
| `+0x30 ~` | 경로 글자 (끝 `\` 보장) | |

- 뜻은 `[추측: 뜻]`, 오프셋·쓰는 줄은 `[정적]`.

#### 쓰는 칸 넷 `[정적]`

- **`RtlpCurDirRef`** (rva `0x1d23f8`) ← 레코드 주소 (`+0x47`)
- 매개변수 `CurrentDirectory.Handle` ← 레코드 `+0x8` (`+0x52`)
- `DosPath.Buffer` ← 레코드 `+0x20` (`+0x5a`)
- `DosPath.Length` ← 레코드 `+0x18` (`+0x62`)

#### 사진과 맞나 — 맞는다

- 사진 P1a `Length 0x84 · Max 0x208 · Buffer 힙 · Handle 0x58` `[관찰]`.
- 레코드 크기 = `0x30 + 0x208 = 0x238`B, 버퍼 = 레코드 `+0x30` (run 문서 "사진과 맞춰 본 것").
- 비유: 개업 서류의 "현재 위치" 칸이 원래는 서류 종이 위 글자였는데, 관리실이 상자에 옮겨 담고 서류엔 "상자 안쪽 참고" 로 바꿔 적는다.

### 2.8 ⑤ CPU 기능 비트맵 — 클래스 `0xfa`

**새 말 — 비트맵 머리 `_RTL_BITMAP`**: 켜짐/꺼짐 칸이 줄지어 있는 것 (비트맵) 을 다루려고 따로 두는 16바이트 머리다. 칸은 둘: `SizeOfBitMap` (몇 비트짜리인가) · `Buffer` (비트 줄이 어디 있나) `[정적]` (PDB 타입). 비유: 체크리스트 **표지** — "이 체크리스트는 128칸, 본문은 저 종이".

**새 말 — CPU 기능 비트맵**: 16바이트 = 128비트. 비트 `k` 하나 = "프로세서 기능 번호 `0x40 + k` 가 있나" `[정적]`.

- 번호 `0x40` 미만은 여기가 아니라 KUSD `ProcessorFeatures` 에 있다 (run59 · run61 에서 본 쪽).
- 비유: 공용 게시판엔 기본 기능 64개 체크리스트가 붙어 있고, 그 **뒤쪽 확장 128칸**은 관리실이 창구에서 따로 받아 칠판에 건다.

#### ntdll 쪽 `[정적]`

- 조건: `LdrpIsSecureProcess == 0` (`+0x1758/+0x175f`). test.exe 는 탄다.
- `NtQuerySystemInformation(0xfa, &RtlProcessorFeaturesBuffer, 0x10, &ret)` → 그다음 칠판에 표지 달기: `RtlProcessorFeaturesBitMap = {0x80, &버퍼}` (`+0x2a`·`+0x35`).
- 반환값은 부른 쪽이 안 본다.

#### 커널 쪽 `[정적]`

- `NtQuerySystemInformation +0x58/+0x5d` → `ExpQuerySystemInformation`. **클래스 `0xfa` 만의 함수는 따로 없다.**
- `+0x866 JZ` → `+0xa3d` → `+0xf4f ~ +0xf60`:
  - 길이가 `0x10` 보다 작거나 8의 배수가 아니면 `0xc0000004`.
  - 아니면 커널 `KeProcessorFeaturesBuffer` (nt rva `0xfc7498`) 16바이트를 **그대로 복사**. 돌려주는 길이 `0x10`.
- (결과서는 이 범위를 `+0xf4f ~ +0xf63` 로 적었다. 이 글은 run 문서의 `+0xf60` 을 따른다 — 보고에 적음.)

#### 커널이 주는 값은? — "정적으로는 0, 하지만 반증 가능"

단계별로:

1. 근거: 그 커널 버퍼에 **쓰는 코드**를 `_refs` 로 찾으니 `KiSetFeatureBits +0x888` 하나뿐이고, 그건 **0 으로 채우는** 줄이다 `[정적]`.
2. 그래서: 정적으로는 16바이트 **모두 0** `[정적, 반증 가능]`.
3. 왜 "반증 가능" 인가: 비트를 켜는 줄이 "레지스터에 든 주소" 로 쓰면 `_refs` 가 못 잡는다. 놓쳤을 수 있다.
4. ntdll 쪽 사진: `RtlProcessorFeaturesBuffer` 는 `끝까지 0*` (P1a ~ P2 모두 0) `[관찰]` (짐 지도 7.3). 정적 0 과 맞는다.
5. 그런데 이것만으론 못 닫는다: **syscall 이 실패해도** 그 칸은 0 이다. "커널이 0 을 줬다" 와 "실패해서 안 썼다" 가 사진에선 똑같아 보인다.
6. 그래서 동적 1번 (커널 쪽 `KeProcessorFeaturesBuffer` 와 ntdll 쪽 상태값을 같이 보기) 이 이번 바퀴에서 **가장 중요한 남은 일**이다 → 4절.

- 덤: 이 비트맵을 읽는 `RtlIsProcessorFeaturePresent(n)` (`0x40 <= n < 0xc0`) 은 test.exe 길에 없다. ntdll 이 부르는 번호는 모두 `0x40` 미만 (KUSD 쪽) 이다 `[정적]`.

### 2.9 ⑥ ★ 스케줄러 공유 데이터 — 그리고 TEB `+0x1850` 의 진짜 주인

**새 말 — 스케줄러 공유 데이터**: 커널 스케줄러와 이 프로세스가 **같이 보는 메모리 한 덩어리**다. 커널이 섹션을 만들어 이 프로세스 주소 공간에 붙여 준다 `[정적]`. run41 에서 본 "열쇠 대출 카드" (TEB `+0x1850` 이 가리키는 8칸 표) 가 그 안에 있을 것이다 `[추측]` (flow.md 2.7 — 사진 `TEB+0x1850` 값이 그 매핑 시작 주소와 같아서).

- 비유: 관리사무소가 **카드 묶음 상자**를 가게 안 한쪽에 갖다 놓아 준다. 상자는 관리사무소와 가게가 같이 들여다본다. 직원은 그 상자에서 **자기 카드 한 장**을 받아 개인 안내판에 꽂는다.

#### 두 함수가 한 사슬이다 `[정적]`

```
LdrpInitializeProcess
 │  +0x180b ~ +0x1820   LdrpSchedulerSharedDataListHeadLock = 0, LdrpSchedulerSharedDataListHead = 빈 목록
 ├─ +0x1827 / +0x182c  LdrpAllocateSchedulerSharedData            ← "상자 신청" (이번)
 │        ├─ +0x2c / +0x31  NtSetInformationProcess(-1, 0x70, &핸들, 8)   syscall 0x1c
 │        │        ══▶ 커널 NtSetInformationProcess +0xec4 → +0xf17 PspSchedulerSharedDataRegionCreate
 │        │              섹션 (MmCreateSectionEx) · 이 프로세스에 붙이기 (MmMapViewOfSection) · 핸들 (ObInsertObjectEx)
 │        ├─ +0x57 / +0x5c  RtlAllocateHeap(LdrpHeap, 0x18)   노드 +0x10 = 핸들
 │        └─ +0x74 ~ +0xb2  자물쇠 잡고 목록에 노드 끼우기 (연결 검사 틀리면 INT 0x29), 자물쇠 풀기
 └─ +0x1837 / +0x183c  LdrpAcquireSchedulerSharedDataSlot         ← "카드 받기" (run41)
          ├─ +0x77           노드 +0x10 의 핸들을 꺼냄
          ├─ +0x84 / +0x89   NtSetInformationThread(-2, 0x39, 버퍼, 0x18)
          └─ +0x92           MOV [RDI+0x1850],RAX   ← TEB+0x1850 을 쓰는 단 하나의 줄
```

#### 한 줄 읽기 — `LdrpAcquireSchedulerSharedDataSlot +0x92`

- **누가**: `LdrpAcquireSchedulerSharedDataSlot`. 첫 스레드에서는 `LdrpInitializeProcess` 가 call `+0x1837` / ret `+0x183c` 로 부른다.
- **어디서**: `+0x92` — `MOV [RDI+0x1850],RAX`. `RDI` 는 TEB.
- **누구를**: 부르기가 아니라 **쓰기** 한 줄. 바로 앞 `+0x84` (ret `+0x89`) 에서 커널이 돌려준 주소 (`RAX`) 를 적는다.
- **무엇을 위해**: 이 직원(스레드)의 **열쇠 대출 카드** 자리를 개인 안내판 `SchedulerSharedDataSlot` 에 적어 두려고.

#### 왜 "`LdrpAllocateSchedulerSharedData` 가 아니다" 라고 말하나 — 단계별로

1. 근거: `LdrpAllocateSchedulerSharedData` 본문엔 `GS:` (TEB 를 찾는 길) 접근이 **아예 없다** `[정적]`.
2. 근거: TEB `+0x1850` 에 쓰는 줄은 ntdll 에 **`LdrpAcquireSchedulerSharedDataSlot +0x92` 하나뿐**이다 `[정적]` (run41).
3. 그래서: `Allocate` 는 **상자를 받아 번호표를 목록에 걸어 둘 뿐**이고, 안내판에 적는 건 바로 다음 `Acquire` 다.
4. 사진과 이어 보기: run44 `TEB+0x1850 = 25996cb0000` `[관찰]` = 2쪽 매핑 섹션 VAD 시작. 그 섹션의 출처가 이번에 본 클래스 `0x70` 이다 `[정적]`.

#### `Acquire` 가 건너뛰는 조건 `[정적]`

- 목록이 비었을 때 (`+0x2d`/`+0x30`)
- `PEB.NtGlobalFlag2 & 0x10` 일 때 (`+0x40`)
- 건너뛰면 TEB `+0x1850` 은 0 으로 남는다 (run41).

#### PE_emulator 에겐 무슨 뜻인가

- 근거: `Acquire` 는 목록이 비었으면 건너뛴다 `[정적]`. 목록을 채우는 건 `Allocate` 다 `[정적]`.
- 그래서: PE_emulator 처럼 `Allocate` 를 "공사 중" 빈 함수로 두면 목록이 빈 채로 남고, `Acquire` 가 건너뛰어 TEB `+0x1850` 이 0 으로 남는 것과 들어맞는다 `[추측]` (PE_emulator 코드를 우리가 직접 보지 않았으니 짐작).
- 진짜처럼 하려면 사슬 **둘 다**가 필요하다: 클래스 `0x70` (섹션·매핑·핸들) 과 클래스 `0x39` (카드 주소). `Allocate` 하나만 채워서는 TEB 칸이 안 생긴다 — 쓰는 줄이 `Acquire` 에 있으니까.
- 섹션이 정확히 몇 바이트인지 (사진은 2쪽) 의 셈은 아직이다 → Q124 (d).

### 2.10 ⑦ TLS 준비 — exe 하나, 비트맵 9비트

**새 말 — TLS 비트맵 (`LdrpTlsBitmap`)**: TLS 번호마다 한 비트씩, "그 번호 주인 있음" 을 켜 두는 체크리스트로 보인다 `[추측]` (일반적으로 알려진 설명 — exe 에 번호 0 을 주고 비트 0 을 켜는 것과 들어맞는다). 머리는 2.8 과 같은 `_RTL_BITMAP` 이다 `[정적]`. 비유: 직원 사물함 **번호 배정표**.

#### 왜 "exe 하나" 인가 `[정적]`

- `LdrpInitializeTls` 는 이 시점 명부를 돈다. 명부에 있는 건 **ntdll 과 exe 뿐**.
- ntdll 항목은 `LoadReason == 9` 라 건너뛴다 (`+0x5d`/`+0x64`).
- kernel32·kernelbase·dll1·dll2 는 한참 뒤에 올라오고, 그쪽은 `LdrpHandleTlsData` 가 맡는다 (flow.md 2.7).
- 그래서 **`N = 1`**.

#### 순서 `[정적]`

```
LdrpInitializeProcess +0x18cb / +0x18d0  LdrpInitializeTls
   ├─ +0x1f / +0x24   LdrpInitializeTlsHeap        HeapPartitionName 비었음 → LdrpTlsHeap = PEB.ProcessHeap
   ├─ +0xf5 / +0xfa   LdrpAllocateTlsEntry (exe)   서류철 항목 0x48B · TLS 디렉터리 0x28B 복사 · exe _tls_index = 0
   │  +0x102          exe 명부 줄 TlsIndex = 0xffff
   │  +0x115 ~ +0x13d N + 8 = 9 ≤ 32 → 안쪽 32비트 칸 LdrpStaticTlsBitmapVector 사용
   │                  LdrpActualBitmapSize = 1 · LdrpTlsBitmap = {9, &LdrpStaticTlsBitmapVector}
   ├─ +0x144 / +0x149 RtlSetBits(&LdrpTlsBitmap, 0, 1)     비트 0 켬
   ├─ +0x158 / +0x15d RtlClearBits(&LdrpTlsBitmap, 1, 8)   비트 1 ~ 8 지움   → 바이트 0 = 0x01
   └─ +0x15d / +0x162 LdrpAllocateTls
            ├─ +0x4d / +0x52  LdrpGetNewTlsVector(9)   창고에서 0x58B (머리 0x10 + 칸 9 × 8), 돌려주는 건 블록 +0x10
            ├─                벡터 칸 0 에 exe TLS 블록을 건다
            ├─ +0x1e2         TEB+0x58 ThreadLocalStoragePointer = 벡터
            └─ +0x1e5         LdrpActiveThreadCount++
```

- **두 가지 "번호"** 를 헷갈리지 말자 `[정적]`:
  - exe **안의 변수** `_tls_index` ← 0 (`LdrpAllocateTlsEntry +0x9e ~ +0xaf`). exe 코드가 "내 사물함은 0번" 으로 읽는 값.
  - exe **명부 줄**의 `TlsIndex` 칸 ← `0xffff` (`+0x102`). 관리실 명부 쪽 표시.
- 왜 9비트인가: `N` 개에 **여분 8비트**를 더한다 (`+0x115`). 그 여분 1 ~ 8번은 지워 둔다 (`RtlClearBits`).
- TLS 힙을 따로 안 만드는 까닭: 매개변수 `HeapPartitionName` 이 비어서 (`""` `[관찰]`, P1a 2543줄). 비어 있지 않으면 `"TLSHeap"` 이라는 힙을 따로 만든다 (안 탐).

#### 사진과 맞나 — 맞는다

- `[관찰]` P1a `TEB+0x58 = 0x25996f383e0`, 그 칸 0 = `0x25996f38440` (16 정렬) — `LdrpAllocateTls +0x1e2`, 벡터 = 블록 `+0x10` 과 맞는다.
- 비유: 사물함 번호 배정표(9칸)에 0번만 체크, 첫 직원의 **열쇠꾸러미**(9칸)를 만들어 0번 고리에 exe 사물함 열쇠를 걸고 손에 쥐여 준다.

#### 에뮬레이터 덫 후보 하나 `[정적]`

- `RtlSetBits`·`RtlClearBits` 는 마스크 표 둘을 **ntdll 이미지 안 `.rdata`** 에서 읽는다 (`LEA RBP,[0x180000000]` 로 이미지 시작을 잡고 `[RSI+RBP+rva]`).
  - 표 `0x192548` = `00 01 03 07 0f 1f 3f 7f ff`
  - 표 `0x180558` = `ff fe fc f8 f0 e0 c0 80 00`
- 그래서 에뮬레이터에선 ntdll `.rdata` 가 **제자리에** 올라와 있어야 한다.
- 비유: 체크 도장을 찍을 때 관리실 벽에 붙은 **도장 견본표**를 본다. 벽이 없으면 도장을 못 찍는다.

### 2.11 묶음 B — 모듈 올리기 길 부품 (표로 짧게)

**새 말 — 노드 넣기 칸 `_RTL_BALANCED_NODE`**: 붉은-검은 트리 ([run35 해설 2.7](../02_sprint/run35-ntdll-entry-해설.md)) 의 카드 한 장이 들고 있는 칸 셋이다 `[정적]` (PDB 타입).

- `Left` (+0) · `Right` (+8) — 왼쪽·오른쪽 자식
- `ParentValue` (+0x10) — 부모 주소 + **맨 아래 비트 = 색** (0 = 검정, 1 = 빨강)
- 트리 머리 `_RTL_RB_TREE` 는 `Root` (+0) · `Min` (+8). 맨 아래 비트 = `Encoded`. 두 트리는 BSS 라 `Encoded` 꺼짐 (포인터 그대로).
- 주소 맨 아래 비트에 색을 넣을 수 있는 건, 노드 주소가 정렬돼 있어 그 비트가 늘 0 이기 때문이라고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유: 카드 상자의 카드마다 "내 위 카드는 몇 번" 을 적는데, 그 숫자 끝자리에 **빨강/검정 스티커**를 같이 붙여 둔다.

| 함수 | 무엇을 하나 | test.exe | 태그 |
|---|---|---|---|
| `LdrpValidateEntrySection` (`LdrpProcessMappedModule +0x83 / +0x88`) | 진입점이 0 · 명부 줄 `EntryPoint` 0 · 진입점 `>= SizeOfHeaders` 면 TRUE. **헤더 안을 가리키면 FALSE** → `+0x8a JZ` 로 `0xc000007b` | ntdll (진입점 0)·exe 둘 다 TRUE | `[정적]` |
| `LdrpIsImageAddress` (`+0x27b / +0x280`) | `NpQueryVirtualMemory` 클래스 0 → `Type == MEM_IMAGE` 인가 | **안 불림** (부르는 두 곳 모두 에러 길) | `[정적]` |
| `RtlRbInsertNodeEx(트리, 부모, 오른쪽?, 새 노드)` | 빈 트리: `Root = Min = 새 노드`, `ParentValue = 0` (검정). 둘째: 부모 자식 칸에 넣고 `ParentValue = 부모 \| 1` (빨강), 부모가 검정이라 회전 없음. 왼쪽이고 부모가 `Min` 이면 `Min = 새 노드` | ntdll = 트리 둘 다 첫 노드, exe = 둘째 노드 (회전 없음) | `[정적]` |
| `LdrpFindKnownDll` · `LdrpLoadKnownDll` | 디렉터리 핸들이 0 이면 바로 `0xc0000135`. 아니면 `NtOpenSection` (접근 `0xd`, Root = `\KnownDlls`) — 없으면 `0xc0000034` 를 `0xc0000135` 로 바꿔 돌려줌 (부른 쪽이 파일 길로 감). `LdrpLoadKnownDll` 끝에 늘 `NtClose(섹션)` | 횟수·이름은 동적 | `[정적]` |
| `\KnownDlls` 스텁 셋 | 번호 `0x58`·`0x138`·`0x16b` (2.2 표) | 탐 | `[정적]` |
| `LdrpIllegalCWDDevices` (데이터) | 장치 특성 마스크 4B (2.6). 읽는 곳 다섯 | 0 | `[정적]` |

- exe 노드 방향:
  - `LdrpMappingInfoIndex` 는 `TimeDateStamp` 가 작은 쪽이 왼쪽 → ntdll `0x6bdf03ca` > exe `0x6ad22b72` 라 **exe 는 왼쪽** `[정적 계산]`.
  - `LdrpModuleBaseAddressIndex` 는 `DllBase` 비교라 왼쪽**일 것** `[추측]` — 주소는 ASLR 로 실행마다 달라서 동적으로 본다 (4절 2).

### 2.12 묶음 C — 공용 부품 (한 줄씩)

| 함수 | 한 줄 | 태그 |
|---|---|---|
| `RtlGetNtSystemRoot` | `PEB.SharedData` 의 silo 경로가 있으면 그것, 없으면 KUSD `NtSystemRoot` (`0x7ffe0030`). 사진 `PEB+0x90 = 0` → KUSD | `[정적]` + `[관찰]` |
| `RtlInitUnicodeStringEx` · `RtlAppendUnicodeToString` | 길이 검사 (`0x7ffe` 글자, `0xc0000106` / `0xc0000023`) 와 복사 | `[정적]` |
| 문자열 해제 (rva `0x4e200`, 같은 주소 이름 3개) | 2.4 끝 — `Buffer` 있으면 `RtlFreeHeap`, 머리 16B 를 0 | `[정적]` |
| `LdrControlFlowGuardEnforced` | `LdrSystemDllInitBlock +0xb8` 이 0 이면 0, 아니면 `(~[+0x9c]) & 1`. test.exe 0 (run63 과 같음) | `[정적]` |

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 곳

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp·ntdll pdb 목록에 run64 까지 | **run65** 더함 (정적 바퀴, 새 동적 실행 없음 한 줄) |
| 그림 2 `p3` (`\KnownDlls` 상자) | "정적: `\KnownDlls` 열기, 경로만 읽음" | + "run65: syscall `0x58`, 출력 칸 = `.mrdata LdrpKnownDllDirectoryHandle`" |
| 그림 2 `p4c` (ntdll 색인 상자) | 트리 색인 둘 | + "run65: 둘 다 빈 트리 갈래 — Root = Min = ntdll 노드" |
| 그림 2 상자 수 | — | **그대로**. 새 callee 6개 (정책·앱 폴더·현재 디렉터리·CPU 기능·스케줄러·TLS 준비) 는 상자로 안 넣었다. 넣으면 `(8)` 부터 번호가 모두 밀린다. 넣을지는 **사용자 결정** (Q124 (e)) |
| 2.4 `LdrpInitializeProcess` 트리 | `+0xdbd` 줄, TLS 줄 | syscall 번호 셋·정책 한 마디, `+0x182c` 두 줄, TLS 한 줄 |
| 2.4 `+0x1563` 줄 | 문자열 해제가 있다는 것만 | + **현재 디렉터리가 빈 갈래에서만**, 진짜 VM 은 안 탐 `[정적]` |
| 2.4 `+0x151c` 줄 | "실패해도 로그만" | + 안쪽 본문은 새 절로 |
| 2.4 트리 넣기 (`LdrpInitializeNtdllDataTableEntry` 아래) | run35 줄 (트리 둘) 까지 | + `RtlRbInsertNodeEx` 칸·첫 노드·둘째 노드·방향 `[정적]` / `[정적 계산]` / `[추측]` |
| 2.4 `LdrpProcessMappedModule` | — | + `LdrpValidateEntrySection`·`LdrpIsImageAddress` 한 줄 |
| 2.4 **새 절** | — | "`\KnownDlls`·정책·현재 디렉터리·CPU 기능 비트맵·스케줄러 공유 데이터·TLS 준비" — 트리 + `[정적]` 줄들 (2.2 ~ 2.10 의 내용) + "디버거가 붙어서 달라지는 길" 한 줄 |
| 2.5 KnownDll 본문 | — | `LdrpLoadKnownDll` → `LdrpFindKnownDll` → `NtOpenSection` 본문 `[정적]` |
| 2.7 TEB 표 줄 | "표 주소를 넣는 곳은 하나뿐 (`+0x92`)" | + 넘기는 핸들 = 클래스 `0x70` 으로 받은 **스케줄러 공유 데이터 섹션 핸들** `[정적]`, 표는 그 섹션 안일 것 `[추측]` |
| 4장 | Q120b 대기 | 해결됨 줄 + 새 줄 셋 (동적 4건 → Q104, 정적 남은 것 → Q124, 결과서와 다르게 적은 곳) |
| 5장 | — | run65 줄 |

- **디버거 때문에 갈리는 길** `[정적]`: 이 구간은 `PEB.BeingDebugged` 를 안 읽는다 (flow.md 새 절 끝). 그러니 디버거를 붙여 봐도 이 구간 길은 같을 것이다.

### 짐 지도 (`baggage.md`) 에서 바뀐 곳

- ntdll `.data` 전역 **14개**가 "쓴 자리 아는 것" 으로 옮겨졌다 (모두 정확):
  - `≤P1a` 8 — `RtlpCurDirRef` · TLS 다섯 (`LdrpTlsHeap`·`LdrpStaticTlsBitmapVector`·`LdrpActualBitmapSize`·`LdrpTlsBitmap`·`LdrpActiveThreadCount`) · `LdrpSchedulerSharedDataListHead` · `RtlProcessorFeaturesBitMap`
  - `끝까지 0` 6 — `LdrpAppPackagesPath`·`LdrpOriginalAppPackagesPath`·`LdrpSchedulerSharedDataListHeadLock`·`LdrpIllegalCWDDevices`·`LdrpDefaultDllDirectories`·`RtlProcessorFeaturesBuffer`
  - `RtlProcessorFeaturesBuffer` 는 커널이 syscall 로 직접 쓰니 **주인 = nt** 로 셌다.
- 숫자: ntdll 전역 (3구역 합 493) 쓴 자리 아는 것 **145 → 159**, 모름 **348 → 334**.
- `.mrdata` `LdrpKnownDllDirectoryHandle`: `[추측]` "출력 칸일 것" → **`[정적]`**, 쓴 주인 ntdll (주소 넘김) → **nt (syscall, test.exe 가 부름)**.
- `LdrpKnownDllPath` 에 커널 쪽 둘째 줄 (`NtQuerySymbolicLinkObject` 가 `Length`·글자를 씀), 처음 버퍼 `0x30`B 와 재시도.
- 매개변수 블록 (3.2) 줄 셋 더함: `CurrentDirectory` 칸 셋, `DllPath.Length` 0 (조건부, 안 탐), `Flags` 비트 26 (조건부, 안 탐).
- TEB (3.3) `+0x58`·`+0x1850` 줄에 run65 설명 더함.
- 힙 상자·핸들 (현재 디렉터리 레코드 · 스케줄러 노드 · TLS 항목·벡터·블록 · `LdrpKnownDllPath` 문자열) 은 **7.6.5** 로.
  - TLS 블록 크기 `0x11`B 는 결과서에 셈 줄이 없어 `[추측]` 으로만 적었다 (Q124 (c)).

### 큰 그림과 이어지는 곳

- **PE_emulator 요청 11 이 이걸로 다 끝났다.** 이제 PE_emulator 는 이 구간 함수 약 25개의 인자·쓰는 칸·하위 호출·syscall·반환을 run 문서 표와 결과서 "PE_emulator 용 작은 표" 에서 그대로 가져갈 수 있다.
- 두 갈림길이 PE_emulator 에 바로 쓰인다:
  - 개업 서류의 `CurrentDirectory` 를 채우면 `+0x1563` 해제가 사라지고 진짜 VM 길과 같아진다 (2.4).
  - TEB `+0x1850` 을 진짜처럼 만들려면 클래스 `0x70` **과** 클래스 `0x39` 가 다 필요하다 (2.9).
- 이 구간이 끝나면 관리실은 kernel32 → import DLL 들이기 → TLS 콜백 → 진입점으로 간다 (run19 ~ run28). ⑦ 에서 exe 몫만 준비한 TLS 는, 나중에 들어오는 DLL 몫을 `LdrpHandleTlsData` 가 이어 받는다.
- 다음 바퀴는 **Q104 VM 단계** — 이번 동적 4건이 거기 얹힌다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **(가장 중요) 클래스 `0xfa` 의 진짜 값** — 커널 `KeProcessorFeaturesBuffer` 16바이트, ntdll 쪽 상태값 | 정적은 "모두 0" 인데, 레지스터 기준 쓰기를 `_refs` 가 놓쳤을 수 있다. ntdll 사진 0 은 syscall 실패여도 0 이라 못 가른다 (2.8) | 동적 1 → **Q104 VM 단계** (`nt!ExpQuerySystemInformation +0xf4f` 근처 `dq nt!KeProcessorFeaturesBuffer L2`, ntdll `+0x2a` 직후 `r eax` · `dq ntdll!RtlProcessorFeaturesBuffer L2`) |
| `LdrpModuleBaseAddressIndex` 둘째 노드 (exe) 가 정말 왼쪽인가 | `DllBase` 는 ASLR 로 실행마다 다르다 | 동적 2 → **Q104** (`RtlRbInsertNodeEx` 입구 4번, 둘째 트리 `r8b` 가 0 인가) |
| `NtOpenSection` 몇 번, 어떤 이름 | "KnownDll 로 올리는 모듈 수" 는 실행 때 정해진다 | 동적 3 → **Q104** (`LdrpFindKnownDll` 입구 `du @rcx`, 덤으로 `LdrpKnownDllPath` 글자) |
| 힙 해제 뒤 버퍼 첫 8바이트를 누가 무엇으로 바꾸나 | 힙 관리자 본문은 run46 범위. 진짜 VM 은 `+0x1563` 을 안 타서 그냥은 안 보인다 | 동적 4 → **Q104** (PE_emulator 와 같은 빈 현재 디렉터리를 줄 때만 `ba w8`) |
| `LdrpPolicyBits` 를 짐 지도 7.3 → 3.8 로 옮길지 | 쓰는 줄이 패키지 갈래뿐이고, 결과서 14개 목록에 없었다 | 정적 → **Q124 (a)** |
| `LdrpQueryIllegalCWDDevices` 가 IFEO 키 없을 때도 닿나 | 결과서가 안 봤다 (값은 어느 쪽이든 0) | 정적 → **Q124 (b)** |
| exe TLS 블록 크기 (`0x11`B) 의 근거 줄 | 결과서에 셈 줄이 없다 | 정적 → **Q124 (c)** |
| 스케줄러 공유 데이터 섹션 크기 | 사진은 2쪽인데 커널 안 셈을 안 봤다 | 정적 → **Q124 (d)** |
| 그림 2 에 새 callee 6개를 상자로 넣을지 | 넣으면 `(8)` 부터 번호가 밀린다 — 문서 모양 결정 | **사용자 결정** → **Q124 (e)** |

- 선택 확인 (정적 + 사진으로 이미 맞음): `LdrpPolicyBits = 0x16`, `LdrpIncludeAlternateForwarders`·`LdrpCheckAppDirType` 입구가 **안 걸리는지**, `RtlpCurDirRef` 값 (계산상 `0x25996f38190` 쯤 `[추측]` — 버퍼 − `0x30`), TLS 비트맵 9 · `01 00` 등 (run 문서 "아직 확인 필요" 끝).

---

## 5. 더 읽기

- run 문서: [`run65-ldr-init-bodies.md`](run65-ldr-init-bodies.md) — "PE_emulator 쪽에서 쓸 것" (갈림길 · syscall 8 · 함수 표 · 꼭 챙길 값), 호출 지점 표, 호출 관계 트리, 사진과 맞춰 본 것
- 결과서: [`_workspace/sast/59_ldr-init-bodies_result.md`](../../_workspace/sast/59_ldr-init-bodies_result.md) — 질문 1 ~ 13, "PE_emulator 용 작은 표" (묶음 B·C 전부), 클래스 `0xfa` 출력 칸 표
- 요청서: [`_workspace/sast/59_ldr-init-bodies.md`](../../_workspace/sast/59_ldr-init-bodies.md)
- flow.md: [2.4 "`\KnownDlls`·정책·현재 디렉터리·CPU 기능 비트맵·스케줄러 공유 데이터·TLS 준비"](flow.md) · 2.5 KnownDll 본문 · 2.7 TEB 표 줄 · 4장 run65 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.2 · 3.3 · 3.8 · 3.9 · 7.3 · 7.6.5
- 앞선 해설: [run18](../02_sprint/run18-proc-init-setup-해설.md) (`\KnownDlls` 자리) · [run28](../02_sprint/run28-tls-init-call-해설.md) (TLS 서류철) · [run35](../02_sprint/run35-ntdll-entry-해설.md) (트리 둘) · [run41](../02_sprint/run41-tls-lock-detail-해설.md) (열쇠 대출 카드) · [run44](run44-baggage-snap-해설.md) (사진) · [run63](run63-cfg-func-table-해설.md) (요청 11 의 1 ~ 6)
