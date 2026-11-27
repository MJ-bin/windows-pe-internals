# run143 mountdev-name — 해설

> 이 글은 [run143 문서](run143-mountdev-name.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

> **고쳐 읽기 (run156)**: 한눈 표 ⑧ · 2.15 의 "`ShellExecute*` → `CInvokeCreateProcessVerb::Execute` 는 가상 함수라 이어지지 않는다 `[공백]`" 은 **정적으로 닫혔다** `[정적]` — windows.storage `ShellExecuteExW` 부터 **14 걸음**, 가상 호출 둘 (`_ExecuteAssoc +0x175` · `InitAndCallExecute +0x214`, 칸 `[+0x48]` 의 대상은 PE 가상표 값) 을 지나 `CInvokeCreateProcessVerb::Execute` (rva `0xe1de0`) 에 닿고, 그 뒤는 이 글의 사슬 그대로다 (`_PrepareAndCallCreateProcess +0xdf _FixupWorkingDirectory`). **실제로 불리는지는 그대로 동적 Q275.** 본문은 그대로 두었다 → [run156 해설](run156-appinfo-runas-해설.md) 2.4 · [run156 문서](run156-appinfo-runas.md) 1-2

- 대기열: **Q269** — PE_emulator 요청 "`\??\C:` 에 IOCTL `0x4d0008` 을 보내는 kernelbase 함수 · 출력 칸 · status 갈래 · 바로 뒤 `\??\MountPointManager` IOCTL 입력·출력 · (되면) shell 쪽 실패 처리" (`_workspace/plan.md`, 이번에 **끝 (run143)**)
  - 원 요청 `../PE_emulator/_workspace/05_tuning_planner/335_internals-mountdev-name.md`, 튜닝 `335_tune15.md` MD1, 병목 샘플 98 · 기본 대답 받은 샘플 124 (요청 쪽 에뮬레이터 숫자).
  - 사용자 결정 2026-11-25 (PE_emulator D154) 요청 넷의 **셋째**. 정적만.
  - 결과서 판정 **분석성공** — 질문 1 · 2 는 코드 줄로 닫힘, 질문 3 (shell 쪽) 은 **부분**.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q275** (VM 묶음, 미룸)
    - 정적 → **Q276** (결과서 "다음 요청 후보" 1 ~ 5 + 보탬)
- 근거:
  - run 문서 [`run143-mountdev-name.md`](run143-mountdev-name.md) (가장 중요)
  - 요청서 [`_workspace/sast/140_mountdev-name.md`](../../_workspace/sast/140_mountdev-name.md) · 결과서 [`140_mountdev-name_result.md`](../../_workspace/sast/140_mountdev-name_result.md)
    - kernelbase 는 있던 `.asm` 을 읽기만 했다 (새로 뽑은 함수 0개).
    - mountmgr.sys 는 **처음 analyze** 하는 드라이버다. 다만 결과서에 따르면 요청서가 시작할 때 이미 analyze · export (12함수) 가 끝나 있어서, 결과서는 Ghidra 를 다시 안 돌렸다.
    - shell32 · windows.storage 는 **Ghidra 를 안 썼다.** PDB 만 받고 `objdump` + PDB 공개 이름으로 직접 호출만 따라갔다.
  - flow.md 변경: docs 커밋 `c72729e` (`git show c72729e -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 볼륨 API · `ShellExecute*` 를 안 부른다 — `sample/*.c` · `log.h` 에 그런 이름이 없다 `[소스]`.
   - shell32 · windows.storage 는 초기화 끝 (P2) 모듈 6개에 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run142 와 같은 줄).
   - mountmgr.sys 는 부팅 때 올라오는 드라이버지만, test.exe 길에 그 장치를 여는 syscall 이 없다.
   - 그래서 이번 이야기는 **"`main` 뒤에 어떤 프로그램이 C: 드라이브의 볼륨 이름을 물을 때"** 의 일이다.
2. **요청 쪽 숫자 · 로그는 에뮬레이터 것이다.**
   - "`NtQueryInformationProcess(0x17)` 둘 → `NtOpenFile` → …", "예외 자리 `RtlCopyMemory +0x100`", "`ShellExecuteW` 51" 같은 것은 PE_emulator 로그다. 진짜 Windows 를 본 게 아니라서 `[관찰]` 이 아니다. 이 글에서는 "에뮬레이터 로그" 라고만 부른다.
3. **오프셋 읽는 법** (앞 해설들과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문 "`+0x16c DeviceIoControl`" 처럼 함수 이름 앞 숫자 = **call 자리**.
   - "`+0x1a6 JZ → +0x611`" 은 그 줄과 점프가 가는 곳.
   - 콜스택으로 본 적은 없다 (정적만).
4. **shell 쪽 줄은 한 단계 약한 근거다.**
   - windows.storage 줄은 `.asm` 이 없어서 flow-writer 가 다시 대조하지 못했다. 그래서 태그가 `[정적: objdump · PDB 공개 이름 — .asm 없음]` 이고, 결과서 근거 수준을 그대로 옮긴다.
5. **IOCTL 이름은 짐작이다.**
   - `0x4d0008` = `IOCTL_MOUNTDEV_QUERY_DEVICE_NAME`, 칸 이름 `DeviceNameOffset` 같은 것은 **공개 헤더 지식** 이라 `[추측: 공개 헤더]` 다.
   - 칸의 **위치 · 크기 · 쓰는 법** 은 코드 줄이 근거라 `[정적]` 이다.
   - 예외 하나: `0x6d0008` 의 이름 `IOCTL_MOUNTMGR_QUERY_POINTS` 는 **mountmgr 안에 문자열로 있다** (결과서). `IOCTL_MOUNTDEV_*` 문자열은 mountmgr 안에 없다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = **번호 붙은 주문서**, IRP = 주문서를 담는 **배달 봉투** → [run66 해설 2.7](run66-apphelp-cache-해설.md)
- IOSB · `Information` = 상담실이 답과 함께 주는 **전표**, `0x80000005` = "**종이가 작아요**" 말투 (맨 앞이 8 이라 **음수 쪽**) → [run94 해설 2.6](run94-bcrypt-resolve-해설.md)
- status 도장 색 — 초록 (성공) · 노랑 (경고 `0x8…`) · 빨강 (오류 `0xC…`), 출력 버퍼 = **답장 종이**, I/O 관리자 = **접수 사무실**, "종이가 작으면 크기를 적어 주고 다시 묻게 하기" → [run126 해설](run126-afd-addrlist-해설.md) 2.7 · 2.16
- **"도장은 성공, 바구니는 빈 채"** — 성공 status 인데 출력 칸을 안 쓴 에뮬레이터 대답 → [run91 해설 2.3 · 2.4](run91-bcrypt-init-해설.md)
- `DeviceIoControl` 갈래 — 코드 종류 9 면 `NtFsControlFile`, 아니면 `NtDeviceIoControlFile` → [run91 해설](run91-bcrypt-init-해설.md) 2.7 · run91 문서
- FSCTL = **파일 시스템용 주문서** → [run132 해설 2.10](run132-npfs-pipes-해설.md) ("새 말 — FSCTL")
- **DOS 오류 번호** (`GetLastError` 값, status 를 바꾼 작은 번호) → [run132 해설 2.4](run132-npfs-pipes-해설.md) ("새 말 — DOS 오류 번호"), 그 값이 사는 TEB `LastErrorValue` 칸 → [run54 해설](run54-teb-fill-해설.md)
- `CreateFileW` · `CreateFileInternal` → [run84 해설](run84-console-crt-해설.md), `CreateFileInternal` 의 `pShimData` 읽기 → [run118 해설](run118-shimdata-read-해설.md)
- 이름 공간 = 관리사무소 안 **동네**, 심볼릭 링크 = **안내판** → [run119 해설](run119-systemroot-link-해설.md), 이름 따라가기 · parse → [run47 해설 2.10](run47-section-chain-해설.md#210-대출-카드-사슬-이름-따라가기와-parse-프로시저)
- 힙 = 실행 중에 "메모리 조금만" 하고 꺼내 쓰는 **공용 창고** → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) · [run46 해설](run46-process-heap-해설.md)
- silo = 따로 떼어 둔 이름 공간 `[추측]` → [run104 해설](run104-params-block-해설.md)
- HRESULT = COM 쪽 32비트 결과 코드, "음수면 실패" → [run89 해설](run89-dotnet-start-해설.md) ("새 말 — HRESULT")

이번에 처음 나오는 말 — **볼륨 · 장치 이름**, **마운트 관리자 (MountPointManager)**, **볼륨 GUID 이름 (`\\?\Volume{GUID}\`)**, **`\\.\` 와 `\??\`**, **이름 상자 (`UNICODE_STRING`)**, **16비트 덧셈 넘침**, **reparse 지점**, **가상 함수 (간접 호출)** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 핸들 = **손잡이**.
- run124 · run126 · run132 의 그림 그대로:
  - 드라이버 = 관리사무소 안 **전문 상담실**, I/O 관리자 = **접수 사무실**.
  - IOCTL = **번호 붙은 주문서**, IRP = **배달 봉투**, IOSB = **전표**, 출력 버퍼 = 손님이 내미는 **답장 종이**.
  - status 도장 색 — 초록 · 노랑 · 빨강.
- run84 의 그림 그대로: 손잡이 뒤가 **창고** 면 파일. 이번엔 그 **창고 한 동 전체** (C: 드라이브) 의 이름을 묻는다.
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 따름 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - `0x4d0008` = 창고 상담실에 내는 **"이 창고의 정식 주소를 알려 주세요"** 주문서. 상담실은 답장 종이 **첫 칸에 "주소 길이"**, 그 뒤에 주소를 적어 준다.
  - mountmgr = 관리사무소 안 **주소록 관리소**.
  - `0x6d0008` = 주소록 관리소에 내는 **"이 주소의 별명들을 다 주세요"** 주문서. 관리소는 종이가 작으면 "**종이가 모자라요, 이만큼 필요해요**" 라고 적어 준다.
  - 가게 직원 (kernelbase `GetVolumeNameForRoot`) 은 첫 주문서의 "주소 길이" 를 **의심 없이 믿고** 그만큼 베끼고, 둘째 주문서는 "모자라요" 를 들으면 **몇 번이든** 종이를 다시 준비해 또 낸다.
- 주의 — 이름이 다른 곳:
  - run 문서 "결과" 비유는 볼륨을 "**방**" 이라 부른다 ("이 방의 정식 주소").
  - run136 · run142 해설에서 "방" 은 이미 **데스크톱** 이다. 섞이지 않게 이 글은 볼륨을 **창고 (한 동)** 로 부른다. 그림만 다르고 사실은 같다.

### 한 줄로 먼저 — 무엇을 얻었나

> **C: 드라이브의 "진짜 이름" 을 두 창구에 차례로 묻는다.**
> **① 볼륨 장치에 `0x4d0008` → 장치 이름 (예: `\Device\HarddiskVolume3`).**
> **② 마운트 관리자에 그 장치 이름을 들고 가서 `0x6d0008` → 별명 목록 → 그중 `\??\Volume{GUID}` 하나를 골라 `\\?\Volume{GUID}\` 로 돌려준다.**

| 질문 | 답 (run 문서 그대로) | 이 글 |
|---|---|---|
| ① `0x4d0008` 을 부르는 함수는 | kernelbase **`GetVolumeNameForRoot`** (rva `0x24c30`, export 아님) 하나. `+0x162 MOV EDX,0x4d0008` · `+0x16c DeviceIoControl` `[정적]` | 2.4 |
| ② 출력에서 무엇을 읽나 | **앞 2 B = 이름 길이 (바이트), 이름은 `+2` 부터.** 그 길이를 **상한 검사 없이** 힙 크기 · `memcpy` 길이로 쓴다 `[정적]` | 2.5 |
| ③ 에뮬레이터는 왜 터졌나 | "성공인데 출력 안 씀" → 쓰레기 길이만큼 `memcpy` → 스택 넘어 읽기 `[정적 + 추측]` | 2.6 |
| ④ `0x4d0008` 이 실패하면 | 음수면 어떤 값이든 (`0x80000005` 포함) **재시도 없이 FALSE** `[정적]` | 2.7 |
| ⑤ 바로 뒤 IOCTL 은 | `\\.\MountPointManager` 를 열고 **`0x6d0008`**. 입력 = `0x18` B 머리 + 장치 이름 `[정적]` | 2.8 · 2.9 |
| ⑥ "다시 묻기" 고리 | LastError `0xea` 면 출력 첫 4 B 를 새 크기로 **횟수 상한 없이** 다시 부른다 `[정적]` | 2.11 |
| ⑦ 에뮬레이터가 줄 답 | 최소 바이트 · 정직한 실패 status · **금지 꼴** (끝없는 고리 · 성공인데 안 씀) | 2.16 |
| ⑧ shell 쪽은 | windows.storage `_FixupWorkingDirectory` 는 실패를 받아도 **그냥 지나간다**. 다만 `ShellExecute*` 가 거기를 타는지는 `[공백]` | 2.17 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**초기화가 끝난 뒤, 프로그램이 볼륨 이름을 묻는 순간** 이다. test.exe 에는 없는 옆길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → … → NtContinue (은퇴)
 │
 └─ exe 진입점 → (CRT) → main
      │  CRT 시작: 표준 핸들마다 GetFileType            ← run84 · run132
      │
      └─ (볼륨 이름을 묻는 프로그램이라면 — 예: ShellExecute* 가
          실행 전에 작업 폴더를 고치는 자리 [추측])
           └─ GetVolumeNameForVolumeMountPointW(L"C:\\", …)  ← 이번 (run143)
                → kernelbase → syscall → nt (I/O 관리자)
                → 볼륨 장치 (0x4d0008) · mountmgr (0x6d0008)
```

- flow.md 에서는 **3장 파이프 절 (run132) 바로 뒤** 에 새 절 "볼륨 이름 묻기" 로 들어갔다.
  - 새 절 첫 말: "`main` 뒤 프로그램이 볼륨 이름을 물을 때 (예: `ShellExecute*` 가 실행 전에 작업 폴더를 고치는 자리 `[추측]`) 의 길이다."
- 프로그램 쪽에서 부르는 일이라 CLAUDE.md 범위로 치면 "`main` 안에서 OS 를 다시 부르는 화살표" 쪽이다. 그래서 그림엔 안 넣었다.

### 왜 이 질문이 지금 왔나

- PE_emulator 는 진짜 nt · ntdll 이름으로 짓는 에뮬레이터다. 볼륨 장치 · mountmgr 같은 드라이버는 없고, 모르는 IOCTL 에는 **기본 대답** 을 준다.
- 악성 샘플이 떨군 파일을 띄우는 자리에서 막혔다 (에뮬레이터 로그, 원 요청):
  - 병목 샘플 98 — 98 / 98 이 "IOCTL 기본 대답 → 같은 스레드 읽기 예외".
  - 그때 열려 있던 맨 위 API 는 `ShellExecuteW` 51 · `ShellExecuteA` 7 · `ShellExecuteExW` 7 (65 / 98).
- 로그의 꼴 (에뮬레이터 로그):
  1. `NtQueryInformationProcess(0x17)` 둘
  2. `NtOpenFile(\??\C:, 0x100080, 3, 0x10)`
  3. `NtDeviceIoControlFile(…, 0x4d0008, 입력 0·0, 출력, 0x208)` — **기본 대답: 성공인데 출력 안 씀**
  4. `NtClose`
  5. `NtCreateFile(\??\MountPointManager, 0x100080, 0x80, 3, 1, 0x20060)` — 에뮬레이터는 여기서 `path_prefix_unsupported`
  - 그리고 예외 자리: ntdll `RtlCopyMemory +0x100 · +0x104 · +0x109` (**스택 끝 읽기**).

### 무엇이 궁금했나

요청서 질문 셋:

1. **`0x4d0008` 을 부르는 함수** — 이름 · 호출 자리, 출력 0x208 B 에서 읽는 칸 (길이 칸 폭 · 이름 시작 · 길이 단위), status 갈래, **길이 칸을 검사 없이 복사 길이로 쓰는 줄** 이 있나.
2. **바로 뒤 `\??\MountPointManager` IOCTL** — 코드 · 입력 칸 · 출력 칸 · 실패 갈래, mountmgr 쪽 검사 차례, `NtCreateFile` 실패 때 kernelbase 반환.
3. (되면) **shell 쪽** 이 FALSE 를 받으면 지나가나 멈추나 — "정직한 실패" 로도 길이 열리나.

### 바퀴 전에 알던 것

- `[정적]` (run91) kernelbase `DeviceIoControl` 갈래 — 장치 종류 9 면 `NtFsControlFile`, 아니면 `+0x6c NtDeviceIoControlFile`. 음수면 `+0x87 JS` → 경고면 바이트 수를 씀 → `+0x154 BaseSetLastNTError` → FALSE. **이번 줄과 같다.**
- `[정적]` (run84) `CreateFileW` · `CreateFileInternal` depth 1 읽기 전용 — `CONOUT$` 길.
- `[정적]` (run118) `CreateFileInternal +0x2f5` · `+0x61f` — `pShimData` 읽는 쪽.
- `[정적]` (run132) `BaseSetLastNTError` 를 뽑음 · `GetFileType` → `NtQueryVolumeInformationFile`.
- 앞 run 과 어긋나는 곳은 없다 (run 문서 "고쳐 읽기: 없음").
- 요청 쪽이 이미 안 것: 코드 이름 `IOCTL_MOUNTDEV_QUERY_DEVICE_NAME` · 출력 꼴 "길이 2 B + 이름" 은 **공개 헤더 글로만** 알았다. 근거 태그로 못 쓰는 상태였다. 이번에 그것을 **코드 줄로** 바꿨다.

---

## 2. 찾은 것

### 2.1 한눈에 — 창구 둘 `[정적]`

run 문서 "호출 관계" 를 줄인 것 (오프셋은 ret 자리):

```
kernelbase!GetVolumeNameForVolumeMountPointW (export)
 ├─ (1) +0x82   GetDriveTypeW
 │               └─ +0xe8  NtQueryInformationProcess(-1, 0x17, 0x24 B)          ── SYSCALL
 ├─ (2) +0xde   GetVolumeNameForRoot
 │               ├─ +0x12d  NtOpenFile(\??\C:, 0x100080, 3, 0x10)               ── SYSCALL
 │               ├─ +0x171  DeviceIoControl(0x4d0008, 0, 0, 출력, 0x208)        ← 창구 ①
 │               │           └─ +0x73  NtDeviceIoControlFile                     ── SYSCALL
 │               │          +0x1ac u16 길이 (검사 없음) → +0x209 memcpy(칸)
 │               ├─ +0x17f  NtClose                                              ── SYSCALL
 │               ├─ +0x334  CreateFileInternal(\\.\MountPointManager)
 │               │           └─ +0x373  NtCreateFile(\??\MountPointManager, …)  ── SYSCALL
 │               ├─ +0x37c  DeviceIoControl(0x6d0008, 입력, 길이+0x18, 출력, 0x20) ← 창구 ②
 │               │           └─ +0x73  NtDeviceIoControlFile                     ── SYSCALL
 │               │               ⇒ mountmgr!MountMgrDeviceControl +0x257 MountMgrQueryPoints
 │               │          LastError 0xea → … → +0x5e9 JMP +0x360   (상한 없음)
 │               ├─ +0x390  CloseHandle
 │               └─        항목 표 → 길이 0x60 \??\Volume{…} → \\?\Volume{…}\ → TRUE
 └─ (3) +0xf5   BasepGetVolumeNameFromReparsePoint   ((2) 가 FALSE 일 때)
```

- `⇒` 는 syscall 을 건너뛰어 커널 쪽으로 간 것이다.
- 전체 나무 (mountmgr 안쪽 · windows.storage 줄 포함) 는 run 문서 "호출 관계".

#### 새 말 — 볼륨 · 장치 이름

- **볼륨** = 파일들이 담긴 디스크 구역 하나. `C:` 같은 드라이브 글자는 그 볼륨에 붙은 이름이다 `[추측: 일반적으로 알려진 설명]`.
- **장치 이름** = 관리사무소 안에서 그 볼륨 장치를 부르는 정식 이름. 이번 글의 예는 `\Device\HarddiskVolume3` 다.
  - 이 값은 PE_emulator 쪽 수집값 (`[수집] reg-users` hivelist, 원 요청) 에서 온 예다. 우리가 VM 에서 본 값은 아니다.
- 비유: 볼륨 = **창고 한 동**, 장치 이름 = 관리사무소 장부에 적힌 그 창고의 **정식 주소**.

#### 새 말 — 마운트 관리자 (MountPointManager · mountmgr)

- `System32\drivers\mountmgr.sys`. 커널 드라이버이고, 장치 `\Device\MountPointManager` 를 만든다 `[정적]`.
- 하는 일 (이번에 본 범위): **볼륨마다 어떤 이름들 (별명) 이 붙어 있나** 를 장부에 들고 있다가, 물으면 목록으로 준다 `[정적: QueryPointsFromMemory]`.
- 비유: 관리사무소 안 **주소록 관리소**. "이 정식 주소의 별명 다 주세요" 하면 별명 목록을 적어 준다.

#### 새 말 — 볼륨 GUID 이름 (`\\?\Volume{GUID}\`)

- **GUID** = 겹치지 않게 만든 긴 번호 (128비트) `[추측: 일반적으로 알려진 설명]`.
- 볼륨마다 `\??\Volume{GUID}` 꼴 별명이 하나 붙어 있고, 드라이브 글자가 바뀌어도 이 별명은 그대로라고 알려져 있다 `[추측: 일반적으로 알려진 설명]`.
- `GetVolumeNameForVolumeMountPointW` 가 돌려주는 것이 바로 이 별명을 `\\?\Volume{GUID}\` 꼴로 바꾼 것이다 `[정적]` (2.12).
- 비유: 창고의 **고유 번호판**. 동네 이름 (드라이브 글자) 은 바뀌어도 번호판은 안 바뀐다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ +0x171  DeviceIoControl(0x4d0008, 0, 0, 출력, 0x208)`

- **누가**: kernelbase `GetVolumeNameForRoot`.
- **어디서**: `+0x16c` 에서 부르고, 돌아오는 자리가 `+0x171` 이다. 그래서 나무엔 `+0x171` 이 찍힌다.
- **누구를**: kernelbase `DeviceIoControl` (rva `0x23660`). 그 안 `+0x6c` 에서 `NtDeviceIoControlFile` syscall 이 나간다 (ret `+0x73`).
- **무엇을 위해**: 방금 연 `\??\C:` 손잡이에 주문서 `0x4d0008` 을 낸다. 보내는 내용은 없고 (입력 0 · 0), 답장 종이는 스택의 `[RBP-0x40]` 0x208 B 다 (`+0x14b` · `+0x153` · `+0x156` · `+0x15a` · `+0x162` · `+0x167`).
- 비유: 창고 상담실 창구에 **빈 질문 + 답장 종이 한 장 (0x208 B)** 을 내밀며 "정식 주소 적어 주세요".

#### 줄 2: `LastError 0xea → … → +0x5e9 JMP +0x360   (상한 없음)`

- **누가**: 같은 `GetVolumeNameForRoot`.
- **어디서**: `+0x380` 에서 실패면 `+0x567` 로 간다. `+0x567 ~ +0x56f` 에서 LastError (`GS:[0x68]`) 가 `0xea` 인지 본다.
- **누구를**: 맞으면 `+0x587` 출력 첫 4 B 를 새 크기로 → `+0x593` 옛 종이 버림 → `+0x5b5` 새 종이 (`RtlAllocateHeap`) → `+0x5e9 JMP` 로 **`+0x360`** (같은 `DeviceIoControl` 앞) 으로 돌아간다.
- **무엇을 위해**: 주소록 관리소가 "종이가 모자라요, 이만큼" 이라고 하면, 그만큼 종이를 다시 준비해 **또** 내기 위해서다. 몇 번째인지 세는 칸이 없다 (2.11).

#### 줄 3: `⇒ mountmgr!MountMgrDeviceControl +0x257 MountMgrQueryPoints`

- **누가**: 커널 쪽 mountmgr 드라이버의 주문서 받는 함수 `MountMgrDeviceControl` (rva `0x17940`).
- **어디서**: `+0x23c CMP R13D,0x6d0008` 로 주문서 번호를 보고, 맞으면 `+0x252` 에서 부른다 (ret `+0x257`).
- **누구를**: `MountMgrQueryPoints` (rva `0x19ac0`) — 입력 검사 후 `QueryPointsFromMemory` 로 답을 만든다.
- **무엇을 위해**: 주소록 관리소가 "별명 다 주세요" 주문서를 받아, 장부에서 별명을 찾아 답장 종이에 적는다.
- `⇒` 는 kernelbase → syscall → I/O 관리자 → 드라이버를 건너뛴 표시다. 그 가운데 (I/O 관리자) 는 이번에 안 읽었다.

### 2.3 앞마당 — `GetDriveTypeW` 와 `NtQueryInformationProcess(0x17)` `[정적]`

요청서는 "`NtQueryInformationProcess(0x17)` 둘 · `NtOpenFile` · `0x4d0008` 이 같은 함수에서 나오나" 를 물었다. 답은 **아니다** (run 문서 "정정" 1).

- export `GetVolumeNameForVolumeMountPointW` 가 맨 먼저 `+0x7d` 에서 **`GetDriveTypeW`** (rva `0x253c0`) 를 부른다.
- `NtQueryInformationProcess(-1, 0x17, [RBP-0x40], 0x24, NULL)` 은 그 안 `GetDriveTypeW +0xe1` 에 있다.
  - `+0xdd LEA EDX,[R9-0xd]` — R9D 가 `0x24` 라서 EDX = `0x17` 이 된다.
- 빠른 길: 받은 버퍼 첫 4 B (`DriveMap`) 에서 그 글자 비트가 서 있고, 그 글자 칸 1 B (`DriveType[n]`) 가 3 이면 3 을 돌려주고 끝난다 (`+0xf5 ~ +0x10f`). 아니면 느린 길.
- 이 export 길에서는 **한 번** 이다. 에뮬레이터 로그의 "둘" 중 둘째는 이 길에 없다 → 다른 호출자일 것 `[추측 → Q275]`.
- 에뮬레이터 판정: **맞음.** 고정 디스크로 답해서 `GetDriveTypeW` 가 3 을 얻고 `NtOpenFile` 로 간 것으로 보인다 (로그 순서 = 코드 순서).
- 비유: 창고에 가기 전에 동네 게시판에서 "C 동은 창고 맞지?" 를 먼저 확인하는 것.

### 2.4 창구 ① 열기 — `NtOpenFile(\??\C:)` 와 주문서 `0x4d0008` `[정적]`

`GetVolumeNameForRoot` 안 차례 (run 문서 "요약"):

1. `+0x56` `RtlDosPathNameToNtPathName_U(경로, &이름, 0, 0)` — `C:\` 를 커널 쪽 이름으로. 0 이면 `+0x64 → +0x618` LastError 3, FALSE.
2. 이름 끝 `\` 를 떼고 (`\??\C:\` → `\??\C:`), 드라이브 글자를 대문자로 (`+0xc2 toupper`).
3. **`+0x126 NtOpenFile`** — 접근 `0x100080` (`+0x110`) · 공유 3 (`+0x104`) · 옵션 `0x10` (`+0xe5`) · 속성 `0x40` (`+0x115`). 음수면 `+0x136 → +0x630` FALSE.
4. **`+0x16c DeviceIoControl(h, 0x4d0008, 0, 0, [RBP-0x40], 0x208, …)`**.
5. `+0x178 NtClose`, `+0x198 RtlFreeHeap` (경로 이름).

- **`0x4d0008` 을 싣는 줄은 kernelbase 전체에 하나** 다 — `+0x162 MOV EDX,0x4d0008`. 결과서가 kernelbase 전체 `objdump` 에서 즉시값을 찾아 확인했다 (VA `0x180024d92`).
  - 요청서는 "이름으로 짐작하지 말고 즉시값으로 찾으라" 고 했다. 그래서 이 함수가 답인 근거는 이름이 아니라 **이 줄 하나** 다.
- `GetVolumeNameForRoot` 는 **export 가 아니다**. 밖에서 부르는 입구는 `GetVolumeNameForVolumeMountPointW` (`+0xd9` · `+0x14e` · `+0x16d`) 와 `GetVolumePathNameW` → `GetVolumePathNameInternalW` (`+0x288` · `+0x3f2` · `+0x4a9` · `+0x54f`) 다.
- `DeviceIoControl` 은 `0x4d0008 >> 16 = 0x4d` ≠ 9 라서 **`NtDeviceIoControlFile`** (`+0x6c`) 을 낸다. run91 과 같은 갈래다.
- 에뮬레이터 판정: `NtOpenFile(\??\C:, 0x100080, 3, 0x10)` 은 코드와 값이 **맞음** (`+0x110` · `+0x104` · `+0xe5`).

### 2.5 ★ 답장 종이 첫 칸 2 B = 길이, 그대로 믿는다 `[정적]`

이번 바퀴의 첫째 열쇠다.

#### 답장 종이에서 읽는 칸

| 칸 | 내용 | 줄 |
|---|---|---|
| `+0` (2 B, u16) | 이름 길이, 단위 **바이트** | `+0x1ac MOVZX EAX, word [RBP-0x40]` |
| `+2` 부터 | 이름 (UTF-16, 끝 널 없음 — kernelbase 가 붙임) | `+0x1f5 LEA RDX,[RBP-0x3e]` |
| 그 뒤 ~ `+0x208` | 안 읽음 | — |

- u16 = 2바이트짜리 부호 없는 수 (0 ~ `0xFFFF`).
- "바이트" 라는 근거: `+0x1f0 MOVZX R8D` 로 길이 칸을 **그대로** `memcpy` 길이에 넣는다. 글자 수로 바꾸는 (`>> 1`) 곳은 끝에 널을 놓는 `+0x20e ~ +0x216` 하나뿐이다.

#### 길이 칸 하나가 네 군데로 간다

| 어디로 | 값 | 줄 |
|---|---|---|
| 이름 상자 `Length` | 칸 | `+0x1b0` |
| 이름 상자 `MaximumLength` | 칸 + 2 | `+0x1b5 ADD AX,2` ~ `+0x1bd` |
| 힙에서 빌리는 크기 | 칸 + 2 | `+0x1d6 RtlAllocateHeap` |
| **`memcpy` 길이** | 칸 | `+0x1f0` → `+0x204 memcpy(새 버퍼, [RBP-0x3e], 칸)` |

- **새 말 — 이름 상자 (`UNICODE_STRING`)**: 커널 · ntdll 이 글자열을 다룰 때 쓰는 작은 상자. "지금 길이 (`Length`)", "최대 길이 (`MaximumLength`)", "글자가 있는 곳" 세 칸이라고 알려져 있다 `[추측: 일반적으로 알려진 설명]`. 비유: 글자를 담은 **봉투 + 겉면에 적은 길이**.

#### 검사가 없다

- `+0x1a6` 와 `+0x204` 사이에 "길이 ≤ `0x206`" 을 보는 비교 (`CMP` · `JA`) 가 **없다** `[정적]`.
  - 답장 종이는 0x208 B 이고, 첫 2 B 가 길이 칸이니, 이름이 들어갈 수 있는 건 최대 `0x206` B 다.
  - 그 한계를 **코드가 지키지 않는다.** 상담실 (드라이버) 이 "종이가 작으면 `0x80000005`" 로 걸러 줄 거라는 **약속에 기댄다** (결과서).
- **새 말 — 16비트 덧셈 넘침**: `칸 + 2` 를 16비트 (`AX`) 로 더하기 때문에, 칸이 `0xFFFE` 면 `0x10000` 이 아니라 **0**, `0xFFFF` 면 **1** 이 된다. 그러면 힙에서 0 ~ 1 B 만 빌리고, `memcpy` 는 원래 칸 크기만큼 써서 **힙을 넘는다** `[정적]`.
- 하나 더: 답장 종이를 부르기 전에 **0 으로 채우지 않는다** (`+0x52` 의 0 채우기는 IOSB 쪽) `[정적]`. 그래서 상담실이 아무것도 안 쓰면, 첫 2 B 에는 **스택에 남아 있던 값** 이 그대로 있다.
- 하나 더: `lpBytesReturned` (`+0x14e`, "몇 바이트 적었음") 는 **안 읽는다** (`+0x58b` 에서 덮어씀). 길이는 오직 종이 첫 칸에서 온다.

비유:

- 직원은 답장 종이를 받으면 **첫 칸의 "주소 길이"** 만 보고, 그만큼 종이를 잘라 베껴 간다.
- 전표 아래 칸 ("몇 바이트 적었음") 은 안 본다. 종이 크기 (0x208) 와 견주지도 않는다.

### 2.6 ★ 에뮬레이터의 "성공인데 출력 안 씀" → 스택 넘어 읽기 — 단계별로 `[정적 + 추측]`

run91 해설의 "**도장은 성공, 바구니는 빈 채**" 와 같은 모양이다. 다만 이번엔 바구니가 "빈" 게 아니라 **낙서가 남은 종이** 라서 더 나쁘다.

**근거** (모두 `[정적]`):

1. 에뮬레이터는 `0x4d0008` 에 status 0 만 주고 답장 종이를 안 썼다 (에뮬레이터 로그).
2. `DeviceIoControl` 은 status ≥ 0 이면 TRUE (run91 갈래, `+0x87 JS` 안 탐 → `+0xa0 MOV EAX,1`).
3. `GetVolumeNameForRoot` 는 TRUE 면 `+0x1ac` 에서 종이 첫 2 B 를 읽는다.
4. 종이 (`[RBP-0x40]`) 는 미리 0 으로 안 채운다 (2.5).
5. 그 2 B 를 검사 없이 `+0x204 memcpy` 길이로 쓴다. 읽는 곳은 `RBP-0x3e` 부터다.

**그래서**:

- 첫 2 B 에 남아 있던 아무 값 (최대 `0xFFFF`) 이 길이가 된다.
- `memcpy` 가 `RBP-0x3e` 부터 **최대 64 KB** 를 읽는다. 스택 끝을 넘으면 읽기 예외다.
- 원문 예외 자리 ntdll `RtlCopyMemory +0x100 · +0x104 · +0x109` 는 **이 `memcpy` 의 몸** 으로 보인다.

**아직 확인 아님**:

- kernelbase 의 `memcpy` (`0x196e77`) 가 ntdll `RtlCopyMemory` 로 이어지는지는 안 따라갔다 `[추측]`.
- 에뮬레이터 `fake()` 가 status 0 만 준 것으로 보인다는 것도 결과서가 `fake()` 본문을 일부만 읽은 `[추측]` 이다.

비유: 흉내 상담실이 "**처리됐어요**" 도장만 찍고 답장 종이를 돌려줬다. 그 종이 첫 칸엔 **앞 사람이 쓰던 숫자** 가 그대로 있었다. 직원은 그 숫자를 "주소 길이" 로 믿고 그만큼 베끼다가, 종이 끝을 지나 **책상 너머까지** 손을 뻗었다.

### 2.7 창구 ① 이 실패하면 — 재시도 없음 `[정적]`

- `DeviceIoControl` 이 FALSE (status 음수) 면 `GetVolumeNameForRoot` 는 **어떤 status 든 같은 길** 이다: `+0x1a6 JZ → +0x611 XOR EAX,EAX` → FALSE.
  - `0x80000005` (종이가 작아요) 도 따로 안 본다. **더 큰 종이로 다시 묻지 않는다.**
  - 답장 종이도 안 읽는다.
- LastError = `DeviceIoControl +0x154 BaseSetLastNTError(status)` 가 세운 값 (= `RtlNtStatusToDosError(status)`).
  - 경고 (`0x8…`) 면 그 전에 IOSB.Information 을 바이트 수 칸에 넣는다 (`+0x192 ~ +0x1b3`, run91 과 같음). 하지만 이 호출자는 그 칸을 안 읽는다 (2.5).
- 요청서 짐작 정정 (run 문서 "정정" 3): "`0x80000005` 면 더 큰 버퍼로 다시 부르나" → `0x4d0008` 쪽은 **안 부른다.** 다시 부르는 쪽은 창구 ② (`0x6d0008`) 이고, 그쪽은 **상한이 없다** (2.11).
- 그래서 에뮬레이터에게: **빨강 도장 (음수) 하나만 찍으면 종이를 아예 안 본다.** "성공인데 안 씀" 보다 훨씬 안전하다 (2.16).

### 2.8 창구 ② 열기 — `\\.\MountPointManager` `[정적]`

창구 ① 이 성공하면, 같은 함수가 이어서 주소록 관리소를 연다.

#### 새 말 — `\\.\` 와 `\??\`

- `\\.\이름` 은 프로그램 (Win32) 쪽에서 장치를 부르는 꼴이고, `CreateFileInternal` 이 이것을 **`\??\이름`** 으로 바꿔 `NtCreateFile` 에 넘긴다 `[정적]` (결과서 2-a).
- `\??\` 는 관리사무소 동네의 **안내판 모음** 이다 (run119 해설의 심볼릭 링크 = 안내판). mountmgr 는 부팅 때 `\DosDevices\MountPointManager` 안내판을 달아 두고, 그게 사용자 쪽에서 `\??\MountPointManager` 로 보인다 `[정적: 문자열 원본 바이트 — 결과서]`.
- 비유: 가게 쪽 말 ("점 창구 MountPointManager") 을 관리사무소 쪽 주소 ("안내판 MountPointManager") 로 옮겨 적는 것.

#### kernelbase 쪽 — 여는 줄

- `+0x32f CreateFileInternal(L"\\.\MountPointManager", 접근 0, 공유 3, 처분 3 (OPEN_EXISTING), {크기 0x20, 속성 0x80}, 기능 플래그)`. 문자열은 `[0x1802a0ba8]` (`+0x305`).
- 그 안 `CreateFileInternal +0x36c` 에서 **`NtCreateFile`** — 접근 `| 0x100080` (`+0x337`), 처분 1 (`+0x481`), 옵션 `[RSP+0x60]`.
  - 옵션 최종값 `0x20060` 은 **에뮬레이터 로그 값** 이다. 어느 비트를 더해서 나오는지는 끝까지 안 따라갔다 `[공백 → Q276]`.
- 그 앞에 `RtlAllocateHeap` 두 번: 입력 (이름 길이 + `0x18`, `+0x238`) · 출력 0x20 B (`+0x2c2`).

#### mountmgr 쪽 — 문 · 안내판 · 여는 검사

- `DriverEntry +0x6a IoCreateDevice` → 장치 `\Device\MountPointManager` (확장 0x178, 종류 0x12).
- `DriverEntry +0x2df GlobalCreateSymbolicLink` → 안내판 `\DosDevices\MountPointManager`.
- 함수표 (`DRIVER_OBJECT`): `+0x70` · `+0x80` = `MountMgrCreateClose`, `+0xe0` = `MountMgrDeviceControl`.
- **열기 검사는 하나뿐**: `MountMgrCreateClose` 가 CREATE 면 옵션의 **0번 비트** 만 본다 (`+0x3c ~ +0x4c`). 서 있으면 `0xC0000103`, 아니면 성공.
  - 0번 비트 이름은 `FILE_DIRECTORY_FILE` `[추측: 공개 헤더]`.
  - `0x20060` 은 0번 비트가 0 → **진짜 mountmgr 는 연다.**
- 비유: 주소록 관리소 문지기는 "**폴더 열기로 오셨어요?**" 하나만 묻는다. 아니면 들여보낸다.

#### 에뮬레이터는 여기서 멈췄다

- 에뮬레이터 `nt_create_file.cpp:209` 는 `\??\MountPointManager` 를 모르는 접두사로 보고 `stop("path_prefix_unsupported")` 한다.
- 이건 **status 가 아니라 멈춤** 이다. kernelbase 는 아무 값도 못 받는다 (run 문서 "정정" 4).

### 2.9 창구 ② 주문서 — `0x18` B 머리 + 장치 이름 `[정적]`

`+0x377 DeviceIoControl(h, 0x6d0008, 입력, 이름길이 + 0x18, 출력, 0x20, …)` 의 입력 (`+0x261 ~ +0x286`):

| 오프셋 | 크기 | 값 | 줄 |
|---|---|---|---|
| `+0x00 ~ +0x0f` | — | 0 (링크 이름 · 고유ID 의 오프셋 · 길이) | `+0x261 MOVUPS [R14],XMM0` |
| `+0x10` | 4 | **`0x18`** (장치 이름이 어디서 시작하나) | `+0x269` |
| `+0x14` | 2 | **이름 길이 (바이트)** = 창구 ① 이 준 길이 칸 | `+0x276` |
| `+0x16` | 2 | 0 | `+0x265` |
| `+0x18` ~ | 이름 길이 | 장치 이름 (널 없음) | `+0x25d` · `+0x286 memcpy` |

- 칸 이름 (`DeviceNameOffset` 등) 은 `[추측: 공개 헤더]`. mountmgr 가 읽는 칸 번호 (`MountMgrQueryPoints +0x4a` · `+0x5f` · `+0x6c` · `+0x92` · `+0xac`) 가 위와 같다.
- **창구 ① 의 길이 칸이 여기서 또 쓰인다.** 크기 · `+0x14` 칸 · 복사 길이 모두. 창구 ① 이 엉터리면 창구 ② 주문서도 엉터리가 된다.
- 비유: 주소록 관리소에 내는 주문서 맨 위에 "**정식 주소는 0x18 번째 칸부터, 길이는 이만큼**" 이라고 적고, 그 아래에 창구 ① 에서 받은 정식 주소를 그대로 베껴 붙인다.

### 2.10 주소록 관리소가 주문서를 받으면 `[정적]`

#### 입구

- `MountMgrDeviceControl +0x2e` 코드 → `+0x66` 뮤텍스 → `+0x79 PsIsHostSilo` → (보통 길) `+0x23c CMP R13D,0x6d0008` → `+0x252 MountMgrQueryPoints`.
  - 호스트 silo 가 아닌 길은 `+0x349 SUB EAX,0x6d0008` → `+0x377`.
- 모르는 코드는 `0xC0000010` (decomp 의 `"IOCTL UNKNOWN"`).

#### 검사 차례 (대표만 — 전체 12줄은 run 문서 2-d 표)

| 차례 | 검사 | 실패 status |
|---|---|---|
| 1 | 입력 < `0x18` | `0xC000000D` |
| 3 | 오프셋 · 길이 홀수 | `0xC000000D` |
| 4 | (오프셋 + 길이) 최댓값 > 입력 길이 | `0xC000000D` |
| 5 | 출력 < `0x20` | `0xC000000D` |
| 6 | 이름 길이 > `0xf000` | `0xC000000D` |
| 9 | 그 장치를 못 엶 / 이름 칸 이상 | 그 status / `0xC0000034` |
| 10 | 장부에 그 장치 없음 | `0xC000000D` |
| 11 | **출력 < Size** | **`0x80000005`**, `Information = 8` (머리 8 B 만 씀) |
| 12 | 성공 | 0, `Information = Size` |

- kernelbase 가 처음 내는 종이는 `0x20` B 라서 차례 5 는 통과하고, Size 가 `0x20` 보다 크면 차례 11 에 걸린다. 결과서는 진짜 mountmgr 가 첫 호출에 `0x80000005` + 필요한 크기를 준다고 읽었다 (링크 하나만 있어도 최소 Size 가 `0x80`, 2.16).

#### 관리소도 볼륨에게 `0x4d0008` 을 묻는다

- `QueryPointsFromMemory +0x79 IoGetDeviceObjectPointer` 로 그 장치를 열고, **자기도 `0x4d0008` 을 보낸다** (`+0x141` 값 · `+0x155 IoBuildDeviceIoControlRequest` · `+0x180 IofCallDriver`).
- 받은 정식 이름을 사본으로 만들고 (`+0x1c4 RtlDuplicateUnicodeString`), 장부의 `DeviceName` 과 견준다 (`+0x250 RtlEqualUnicodeString`).
- 같은 요청이 `QueryDeviceName +0x14a` · `QueryDeviceNameInternal +0xb7` 에도 있다.
- 이 코드가 알려 주는 **볼륨 장치 쪽 약속** (결과서):
  - 출력 첫 u16 = 이름 길이 (바이트), 이름은 `+2`.
  - 종이가 작으면 `0x80000005` + 필요한 길이.
  - 부르는 쪽은 `(길이 & ~1) + 2` 크기로 **한 번 더**.
- 눈여겨볼 차이: **mountmgr 는 `0x4d0008` 이 작다고 하면 한 번 더 묻는데, kernelbase 는 안 묻는다** (2.7). 같은 주문서를 두 손님이 다르게 다룬다.
- 진짜 볼륨 드라이버 (volmgr · partmgr) 본문은 안 봤다 `[공백 → Q276]`.

#### 답장 종이 모양

- `+0` Size · `+4` 항목 수 · `+8` 부터 항목 `0x18` B 씩 (`+0x4e8 ~ +0x51f`) · 그 뒤 이름 본문 **[고유ID][장치 이름][링크 이름들…]** (`+0x4a5` · `+0x4b7` · `+0x534`).
- `Size = 8 + 0x18 × 항목수 + 모든 길이 합`.
- 비유: 관리소 답장은 "**필요한 종이 크기 · 별명 몇 개**" 머리 8 B, 그 밑에 별명마다 "어디 · 몇 글자" 표, 맨 밑에 별명 글자들.

### 2.11 ★ "크기 알려 주면 다시 묻기" 고리 — 상한이 없다 `[정적]`

이번 바퀴의 둘째 열쇠다.

#### 고리 한 바퀴

1. `+0x377` `DeviceIoControl(0x6d0008, …, 출력 0x20)`.
2. 관리소가 `0x80000005` + 머리 8 B (Size · 항목 수) 를 준다 (2.10 차례 11).
3. `DeviceIoControl` 은 FALSE. LastError = `RtlNtStatusToDosError(0x80000005)`.
4. `+0x380 JZ → +0x567`: LastError (`GS:[0x68]`) 를 읽고 `+0x56f CMP ECX,0xea`.
5. `0xea` 면: `+0x587 MOV EBX,[RSI]` — **출력 첫 4 B (Size) 를 새 크기로**.
6. `+0x58b` 그 값을 `[RSP+0x50]` 에 둠 → `+0x593` 옛 종이 버림 → `+0x5b5` 새 종이 (EBX B).
7. `+0x5e1 / +0x5e5` 출력 길이 칸 ← 그 값 → **`+0x5e9 JMP` → `+0x360`** (1번으로).

- `0xea` 가 아니면: `+0x575 JNZ → +0x386` 손잡이 닫고 LastError `0x57`, FALSE.
- `0xea` 의 이름은 결과서가 `ERROR_MORE_DATA` 라 부른다 (공개 이름).

#### 왜 "상한이 없다" 인가 — 단계별로

- **근거**: 되돌아가는 `+0x5e9 JMP` 가 **무조건** 이다. 몇 번 돌았는지 세는 칸이 없다.
- **근거**: 빠져나가는 길은 `+0x5cc JZ → +0x74d` (새 종이를 못 빌림 = 힙 할당 실패 → `CloseHandle`) **하나뿐** 이다.
- **그래서**: 진짜 mountmgr 처럼 "두 번째엔 Size 만큼 종이를 받았으니 성공" 이면 한 바퀴로 끝난다. 하지만 상대가 계속 `0x80000005` 를 주면 **끝없이 돈다.**
- **그래서 (금지 꼴 둘)**:
  - 엔진이 `0x80000005` 를 **계속** 주면 → 끝없는 고리.
  - 엔진이 `0x80000005` 를 주면서 **첫 4 B (Size) 를 안 쓰면** → 종이에 남은 값을 크기로 믿는다. 그 종이는 힙에서 받은 0x20 B 이고, 0 으로 채우는지는 `[공백]` 이다. 그 값이 작으면 같은 답을 또 받는다 → 사실상 끝없는 고리 `[정적 + 추측]`.
- **아직 확인 아님**: `0x80000005` → `0xea` 는 ntdll `RtlNtStatusToDosError` 표 값으로 본 것이다 `[추측: 표 안 읽음 — Q276]`. 이 값이 다르면 재시도 갈래 자체를 안 탄다.

비유:

- 직원이 주소록 관리소에 작은 종이 (0x20) 를 낸다.
- 관리소가 "**종이가 모자라요, 이만큼 필요해요**" 를 종이 첫 칸에 적어 돌려준다.
- 직원은 그 크기만큼 새 종이를 준비해 **또** 낸다. 몇 번째인지는 안 센다.
- 관리소가 매번 "모자라요" 만 하면, 직원은 **종이가 바닥날 때까지** 창구 앞을 떠나지 않는다.
- run126 해설의 afd 창구 (2.7 "종이가 작을 때") 와 닮은 꼴인데, 이번엔 **부르는 쪽에 멈춤 장치가 없다** 는 게 다르다.

### 2.12 답장 종이에서 별명 하나 고르기 `[정적]`

성공 (`≥ 0`) 하면 `+0x38b CloseHandle` · `+0x3a2` 입력 종이 버림 뒤, 항목 표를 훑는다.

- `+0x3b9 CMP EDX,[RSI+4]` — `+4` = 항목 수.
- 항목 i 는 `+8 + 0x18*i`. 읽는 칸은 둘뿐: `+0` 이름 오프셋 (u32, `+0x3ce`) · `+4` 이름 길이 (u16, `+0x3c8`).
  - `+8 ~ +0x17` (고유ID · 장치 이름 칸) 은 **안 읽는다.**
  - 결과서 2-a 는 이 두 칸을 "`[..+8]` · `[..+0xc]`" 로 적는다 — `[RSI+RCX*8+8]` 꼴이라 앞의 `+8` 이 붙은 셈이고, 항목 안 `+0` · `+4` 와 같은 자리다.
- 고르는 조건:
  - 길이가 **정확히 `0x60`** (`+0x49f CMP R8W,0x60`). `0x62` 짜리는 끝이 `\` 면 앞 검사까지 가다가 여기서 떨어진다.
  - 글자 자리: `\` `?`(또는 `\`) `?` `\` `Volume` (대소문자 구분) · `{` (10) · `-` (19 · 24 · 29 · 34) · `}` (47).
  - **16진 자리는 안 본다.** GUID 값은 아무거나 통과.
- 안 맞으면 `+0x5f9 INC EDX` 다음 항목. 끝까지 없으면 `+0x3bc JNC → +0x673` LastError `0x57`, FALSE.
- 맞으면:
  - 호출자 글자 수 × 2 ≥ `0x64` 가 아니면 `+0x4c3 JC → +0x766` LastError `0xce`, FALSE.
  - `0x60` B 복사 → `+0x508` 2번째 글자를 `\` 로 (`\??\` → `\\?\`) → `+0x50d` 끝에 `\` + NUL (`ECX = 0x5c`, `+0x4cd`) → `+0x52a MOV EAX,1`.
  - 결과 `\\?\Volume{GUID}\` **50 글자**. windows.storage `_InitLocalDrive` 가 주는 글자 수 `0x32` (= 50) 와 딱 맞는다 (결과서).
- 비유: 관리소 답장에는 별명이 여러 개 적혀 있을 수 있다 (예: 동네 이름 `\DosDevices\C:`). 직원은 그중 **글자 수가 딱 48 자이고 "Volume{ - - - - }" 모양인 것** 하나만 집어, 앞머리를 고쳐 쓰고 끝에 `\` 를 붙여 돌려준다.

### 2.13 주소록 관리소를 못 열면 — 다른 길로 한 번 더 `[정적]`

- `CreateFileInternal`: `NtCreateFile` 음수면 `+0x397 JS` → 조각 `FUN_18002635d` — 손잡이 `-1`, `BaseSetLastNTError(status)` (`+0xb1`).
  - 일부는 LastError 를 바꿔 쓴다: `0xC0000035` → `0x50`, `0xC00000BA` → 3 또는 5.
  - **`0xC0000022`** (접근 거부) 면 옵션 비트 `0x20000` 을 지우고 **`NtCreateFile` 을 한 번 더** 부른다 (`+0x37a` → `+0x696` → 조각 `FUN_18019b82e +0x22 BTR EAX,0x11` · `+0x47`).
- `GetVolumeNameForRoot`: `+0x337 CMP RAX,-1` · `+0x33b JZ → +0x6eb` → (검사 함수 한 번, 반환 안 봄) → 종이들 버림 → `+0x6d3` **FALSE**. 자기 LastError 는 안 세운다.
- export `GetVolumeNameForVolumeMountPointW` 는 그다음 `+0xf0` **`BasepGetVolumeNameFromReparsePoint`** 로 한 번 더 해 본다:
  - `+0xb0 CreateFileInternal(X:\, 플래그 0x2200000)` → `NtCreateFile`.
  - `+0x100 DeviceIoControl(0x900a8, 출력 0x1c)` → `0x900a8 >> 16 = 9` 라 **`NtFsControlFile`** (FSCTL).
  - 둘 중 하나라도 실패면 0. 그래도 FALSE 면 `+0xfb ~ +0x104` 출력 첫 글자 0, FALSE.
- **새 말 — reparse 지점**: 폴더나 파일에 "실제 내용은 다른 곳에 있어요" 표시를 붙여 두는 장치로 알려져 있다 `[추측: 일반적으로 알려진 설명]`. 이 갈래는 `C:\` 가 그런 표시를 달았으면 그 대상을 따라가 보는 길이고, 성공하면 `GetVolumeNameForRoot` 를 그 이름으로 다시 부른다 (`+0x16d`). 평범한 C: 루트는 reparse 가 아니다 `[추측]`.
- 비유: 주소록 관리소 문이 닫혀 있으면, 직원은 창고 문 앞에 "**이사 갔어요**" 쪽지가 붙어 있나 한 번 더 보고 온다.

### 2.14 syscall 차례와 반환값 — 짧게 `[정적]`

`GetVolumeNameForVolumeMountPointW(L"C:\\", 출력, 50)` 이 성공할 때 (run 문서 4절):

| 차례 | syscall | 핵심 | 에뮬레이터 로그와 |
|---|---|---|---|
| 1 | `NtQueryInformationProcess` | `0x17`, 0x24 B (`GetDriveTypeW +0xe1`) | 첫째와 같음. 둘째는 이 길에 없음 `[추측 → Q275]` |
| 2 | `NtOpenFile` | `\??\C:`, `0x100080`, 3, `0x10` | 같음 |
| 3 | `NtDeviceIoControlFile` | `0x4d0008`, 출력 0x208 | 같음 (**여기서 터짐**) |
| 4 | `NtClose` | — | 같음 |
| 5 | `NtCreateFile` | `\??\MountPointManager` | 같음 (**여기서 멈춤**) |
| 6 | `NtDeviceIoControlFile` | `0x6d0008`, 출력 0x20 | 아직 안 나옴 |
| 7 | `NtDeviceIoControlFile` | 같은 입력, 출력 Size (**상한 없음**) | — |
| 8 | `NtClose` (`CloseHandle`) | MPM 손잡이 | — |
| 9 ~ 11 | (실패 뒤) `NtCreateFile(\??\C:\)` · `NtFsControlFile(0x900a8)` · `NtClose` | — | — |

반환값 (run 문서 5절, 대표만):

| 갈래 | 반환 · LastError |
|---|---|
| `0x4d0008` 음수 | FALSE · `DeviceIoControl` 이 세운 값 |
| 이름 복사본 힙 실패 | FALSE · 8 |
| MPM 열기 실패 | FALSE · `CreateFileInternal` 이 세운 값 → export 가 reparse 길로 |
| `0x6d0008` 실패 + `0xea` | 크기 다시 받아 재호출 (상한 없음) |
| `0x6d0008` 그 밖 실패 · `0x60` 꼴 없음 | FALSE · `0x57` |
| 호출자 버퍼 < 50 글자 | FALSE · `0xce` |
| 성공 | TRUE · `\\?\Volume{GUID}\` + NUL |

### 2.15 shell 쪽 — 실패를 받아도 지나간다 (부분) `[정적: objdump · PDB 공개 이름 — .asm 없음]`

질문 3 은 **반만** 닫혔다.

#### 직접 호출 사슬 하나 (windows.storage, call 자리)

```
CInvokeCreateProcessVerb::Execute ⇢? (ShellExecute* 에서 — 가상 함수, [공백])
 └─ +0x58 Launch → +0x10d _TryCreateProcess → +0x3c _PrepareAndCallCreateProcess
     → +0xdf _FixupWorkingDirectory
         └─ +0x84 CMountPoint::GetMountPoint(int) → +0x6c _GetMountPointForDriveLetter
             → +0x250 _InitLocalDrive
                 └─ +0x40 GetVolumeNameForVolumeMountPointW (글자 수 0x32)
```

#### FALSE 가 올라가는 길

1. `_InitLocalDrive +0x4c test / +0x4e je → +0xff ResultFromKnownLastError()` — 마지막 오류를 HRESULT 로 바꿔 돌려준다.
2. `GetMountPoint(int) +0x71` 그대로 돌려준다.
3. `_FixupWorkingDirectory +0x89 test / +0x8b js → +0xd0` — **음수면 아무것도 안 하고 함수 끝** (반환 void).

- 그래서 **이 자리에선 그냥 지나간다.** "정직한 실패" (FALSE) 로도 이 자리는 열린다.
- 이름으로 보면 "실행 파일을 띄우기 전에 작업 폴더를 고치는 단계" 다. 고치는 데 실패해도 실행은 계속한다는 꼴로 보인다 `[추측]`.

#### 못 이은 곳 `[공백]`

- **새 말 — 가상 함수 (간접 호출)**: 부를 함수 주소를 그 객체의 **함수 표** 에서 꺼내 부르는 꼴. 코드 줄에는 "표의 몇 번째 칸" 만 있어서, 정적으로는 **누구를 부르는지 줄만 보고 못 정한다** `[추측: 일반적으로 알려진 설명]`. 비유: "담당자에게 전화" 라고만 적힌 메모 — 담당자 이름은 그날 명단을 봐야 안다.
- `CInvokeCreateProcessVerb::Execute` 가 가상 함수라서 `ShellExecute*` → `Execute` 가 직접 호출로 안 이어진다. 결과서는 `ShellExecuteExW` · `ShellExecuteW` · `ShellExecuteNormal` 에서 직접 호출로 12 단계 · 1001 함수까지 내려갔지만 위 사슬에 닿지 않았다.
- 그래서 **"`ShellExecuteW` 가 이 사슬을 탄다" 는 정적으로 못 정했다.** 이름과 모양이 맞아 후보 `[추측]`.
- shell32 `_GetVolumeNameForVolumeMountPoint` · `CDriveLetterCache::GetVolumePathNameAndCachedMountPoint` 도 실패를 HRESULT 로 올리는 같은 꼴이지만, 그 위에서 멈추는지는 안 봤다 `[공백]`.
- 부르는 자리 수 (직접 `call [IAT]` 만): windows.storage `GetVolumeNameForVolumeMountPointW` 13 · `GetVolumePathNameW` 8 · `GetVolumePathNamesForVolumeNameW` 4, shell32 10 · 5 · 2.

### 2.16 ★ 에뮬레이터에게 — 최소 답 · 정직한 실패 · 금지 꼴

run 문서 "PE_emulator 쪽" 그대로 옮긴다. 고칠지는 사용자 결정이고, flow-writer 는 `../PE_emulator` 를 줄 번호만 훑었다.

#### 지금 판정

| 지금 에뮬레이터 | 판단 |
|---|---|
| `0x4d0008` — 볼륨 갈래 (`nt_device_io_control_file.cpp:675 ~ :742`) 목록에 없어 `fake()` 또는 `device_control_code_unverified` (`:726 ~ :727`) | **틀림 (빠짐)** |
| `NtCreateFile(\??\MountPointManager)` → `stop("path_prefix_unsupported")` (`nt_create_file.cpp:209`) | **틀림 (빠짐)** — 멈춤이라 값이 안 감 |
| `NtQueryInformationProcess(0x17)` | **맞음** |
| `NtOpenFile(\??\C:, 0x100080, 3, 0x10)` | **맞음** |

#### 성공을 주려면 — 최소 바이트 (C: = `\Device\HarddiskVolume3`)

- **`0x4d0008`**: status 0, `Information = 0x30`, 출력 = `2e 00` + `\Device\HarddiskVolume3` UTF-16LE 46 B (널 없음).
  - `0x2e` = 23 글자 × 2 B. **길이 칸 + 이름을 반드시 써야 한다** (2.5 · 2.6).
  - `Information` 은 이 호출자가 안 읽지만 `0x30` 이 약속에 맞음 `[추측]`.
- **`NtCreateFile(\??\MountPointManager)`**: status 0 (옵션 0번 비트가 0 이면 진짜도 연다, 2.8).
- **`0x6d0008`** (진짜 mountmgr 흉내):
  - 입력 검사: 입력 ≥ `0x18`, 출력 ≥ `0x20`, `+0x10 == 0x18`, 이름 = `\Device\HarddiskVolume3`.
  - **첫 호출 (출력 0x20)**: status `0x80000005`, `+0` = Size, `+4` = 항목 수, `Information = 8`. 최소 Size = `8 + 0x18 + 0x60 = 0x80`, 항목 1.
  - **둘째 호출 (출력 ≥ Size)**: status 0, `Information = Size`. `+0` `0x80` · `+4` 1 · `+8` 링크 이름 오프셋 `0x20` · `+0xc` 길이 `0x60` · `+0x10 ~ +0x1f` 0 · `+0x20` `\??\Volume{GUID}` `0x60` B.
    - GUID 는 `MountedDevices` 수집값 (에뮬레이터 쪽).
  - 진짜처럼 하려면 링크마다 항목 (`\DosDevices\C:` `0x1c` B 등), 본문 순서 [고유ID][장치 이름][링크들]. 길이 `0x60` 짜리가 하나라도 있으면 kernelbase 는 그것을 고른다.

#### 정직한 실패 — 줄 status

- **`0x4d0008` 을 못 채우면**: 음수 아무거나 → 출력 안 읽고 FALSE (2.7). 권장 **`0xC0000010`** — mountmgr 가 모르는 IOCTL 에 주는 값이다 `[정적: mountmgr — 볼륨 드라이버가 같은 값인지는 공백]`.
- **MPM 을 못 열면**: 음수 (권장 **`0xC0000034`**) → FALSE. **`0xC0000022` 는 피할 것** — `NtCreateFile` 을 한 번 더 부르게 한다 (2.13).
- 둘 다 실패해도 export 는 9 ~ 11 (`NtCreateFile(\??\C:\)` · `NtFsControlFile(0x900a8)`) 을 더 부른다. 정직한 답: 열기 성공 + `0x900a8` 은 reparse 아님 (공개 값 `0xC0000275` `[추측]`) → 출력[0] = 0, FALSE.
- shell 쪽은 이 FALSE 를 받아도 `_FixupWorkingDirectory` 자리에선 지나간다 (2.15). 다만 `ShellExecuteW` 가 그 사슬을 타는지는 `[공백 → Q275]`.

#### 금지 꼴 둘

1. **status 0 인데 출력을 안 쓰는 대답** — 최악. 길이 칸이 쓰레기 → `memcpy` 최대 64 KB (2.6). 이번 막힘이 바로 이것.
2. **`0x6d0008` 에 `0x80000005` 를 주면서 Size 를 안 쓰거나, 둘째에도 계속 `0x80000005`** — `GetVolumeNameForRoot` 가 `+0x5e9` 에서 끝없이 돈다 (2.11).

비유: 흉내 상담실이 할 수 있는 것은 셋이다.

- 진짜처럼 **주소를 적어 준다** (최소 바이트).
- 정직하게 **빨강 도장** 을 찍는다 — 직원은 종이를 안 보고 "안 됐네" 하고 다음으로 간다.
- 하면 안 되는 것: **초록 도장 + 낙서 남은 종이**, 그리고 **끝없는 "모자라요"**.

### 2.17 요청서 짐작이 틀린 곳 (run 문서 "정정")

1. "`NtQueryInformationProcess(0x17)` 둘 · `NtOpenFile` · `0x4d0008` 이 같은 함수에서" → `NtOpenFile` 과 `0x4d0008` 만 같은 함수. `0x17` 은 `GetDriveTypeW +0xe1`, 이 길에선 한 번 (2.3).
2. 요청서의 PE_emulator 줄 번호 `:617 ~ :654` 는 밀렸다. 지금 파일에선 다른 장치 갈래이고, 볼륨 갈래는 `:675 ~ :742` 다.
3. "`0x80000005` 면 더 큰 버퍼로 다시 부르나" → `0x4d0008` 쪽은 안 부른다. 다시 부르는 건 `0x6d0008` 쪽, 상한 없음 (2.7 · 2.11).
4. "`path_prefix_unsupported` 가 kernelbase 에 돌아가는 값" → 멈춤이지 status 가 아니다 (2.8).
5. 코드 이름 `IOCTL_MOUNTDEV_QUERY_DEVICE_NAME` → mountmgr 안에 그 문자열이 없다. 이름은 `[추측: 공개 헤더]` 그대로.

- 앞 run 고쳐 읽기: **없음.** run91 의 `DeviceIoControl` 갈래 (`+0x2b` · `+0x6c` · `+0x87` · `+0x154` · `+0x1de`) 는 이번 줄과 같다.

### 2.18 flow-writer 대조 — 결과서와 다르게 적은 곳 (뜻은 같음, 짧게)

- `+0x50d` 는 `MOV dword [R15+0x60],ECX` 이고 `ECX = 0x5c` 는 `+0x4cd` 에서 온다.
- 재시도 크기는 `+0x58b [RSP+0x50]` → `+0x5e1` → `+0x5e5 [RSP+0x28]` 로 간다 (`[RSP+0x50]` 은 `lpBytesReturned` 자리이기도).
- `+0x6eb` 는 해제가 아니라 먼저 **검사 함수 하나** (`0x180128bf4`, 같은 주소 이름 11개 `IsBrokered…Present` 무리, 전역 칸 `0x1803a9d80` 캐시) 를 부른다. 반환은 안 쓰인다. 결과엔 영향 없음.
- `CreateFileInternal` 의 `JS` 는 `+0x395` 가 아니라 `+0x397`. 그 앞 `+0x388` 기능 검사 · `+0x38f JZ`.
- `GetDriveTypeW` 를 부르는 자리는 셋이 아니라 넷 (`GetVolumePathNameInternalW` 조각 `FUN_1800272d2 +0x2f` 추가).
- `+0x4bf` 는 `CMP`, `JC` 는 `+0x4c3`.
- `QueryPointsFromMemory +0x1c4` 는 복사가 아니라 `RtlDuplicateUnicodeString` (사본 만들기).
- shell32 · windows.storage 줄은 `.asm` 이 없어 대조 못 함.

### 2.19 정리 — 비유로 한 번 더

1. 어떤 프로그램 (아마 `ShellExecute*` 를 부른 쪽 `[추측]`) 이 "C 창고의 고유 번호판이 뭐예요?" 를 묻는다.
2. 직원 (kernelbase) 은 먼저 동네 게시판에서 "C 동은 창고 맞지?" 를 본다 (`GetDriveTypeW`, `0x17`).
3. **창구 ①**: 창고 상담실에 답장 종이 0x208 B 를 내밀며 "정식 주소 주세요" (`0x4d0008`). 상담실은 첫 칸에 길이, 그 뒤에 `\Device\HarddiskVolume3` 같은 주소를 적는다.
4. 직원은 **첫 칸 길이를 의심 없이 믿고** 그만큼 베낀다. 흉내 상담실이 초록 도장만 찍고 종이를 안 쓰면, 남은 낙서만큼 베끼다 책상 너머로 손을 뻗는다 — 이번 막힘.
5. **창구 ②**: 주소록 관리소 (mountmgr) 에 그 주소를 들고 가 "별명 다 주세요" (`0x6d0008`). 관리소는 자기도 창고에 정식 주소를 다시 물어 장부와 맞춰 본다.
6. 관리소가 "종이가 모자라요, 이만큼" 하면 직원은 그만큼 종이를 다시 준비해 또 낸다 — **몇 번이든.**
7. 별명 목록에서 48 자짜리 `\??\Volume{…}` 하나를 골라 `\\?\Volume{…}\` 로 고쳐 돌려준다.
8. 중간에 막히면 FALSE. 위층 (windows.storage 작업 폴더 고치기) 은 FALSE 를 받아도 그냥 지나간다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (docs 커밋 `c72729e` — `git show c72729e -- runs/03_sprint/flow.md`)

**그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 바뀜. test.exe 길에 없는 일이라서다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — kernelbase.pdb 줄 | run142 까지의 목록 | "run143 — 다시 analyze 안 함, 볼륨 함수 있던 `.asm` 읽기만 (새로 뽑은 함수 0)" 추가 |
| 기준 — 새 줄 | 없음 | **mountmgr.pdb** `B5FC1BC04B06273FE01BB638A26661D51` (처음 analyze, 194함수, export 12) |
| 기준 — 새 줄 | 없음 | **shell32.pdb** `011DE059CF3D0B8174FA9B5324EA76541` · **Windows.Storage.pdb** `EF42E52BBA15CEFDA22CB070B2FB69881` (PDB 받기만, analyze 안 함) |
| 기준 — 근거 줄 | — | "(run143) 정적 바퀴, 사진 값 대조 없음 — test.exe 는 볼륨 API · `ShellExecute*` 를 안 부름 `[소스]`, shell32 · windows.storage 를 안 올림 `[관찰]`" |
| 3장 파이프 절 뒤 | 없음 | **새 절 "볼륨 이름 묻기"** — 나무 하나 + `[정적]` 줄 여섯 + shell 쪽 `[정적: objdump · 결과서]` 한 줄 |
| 4장 "3 (범위 밖 — 볼륨)" | 없음 | **Q269 해결됨 (run143)** 줄 + run143 새 줄 셋 (동적 Q275 · 정적 Q276 · PE_emulator 쪽) |
| 5장 | 없음 | run143 두 줄 (한 일 · 근거) |

- 태그 변화로 보면: 요청 쪽이 **공개 헤더 글로만** 알던 "길이 2 B + 이름" 출력 꼴이 이제 **`[정적]` (코드 줄 `+0x1ac` · `+0x1f5`)** 이 됐다. 이름 (`IOCTL_MOUNTDEV_*`) 만 `[추측: 공개 헤더]` 로 남았다.

### 큰 그림에서 어떻게 이어지나

- **run91 과 이어진다.** run91 은 "도장은 성공, 바구니는 빈 채" 가 손잡이 칸 0 으로 이어지는 꼴이었다. 이번엔 같은 꼴이 **길이 칸 쓰레기 → 스택 넘어 읽기** 로 이어졌다. 같은 `DeviceIoControl` 갈래 (`+0x6c` · `+0x87` · `+0x154`) 위에서다.
  - 교훈은 같다: 흉내 창구가 "성공" 을 줄 땐 **부르는 쪽이 읽는 칸을 다 써야** 한다. 못 쓰겠으면 빨강 도장이 낫다.
- **run126 과 이어진다.** "종이가 작으면 크기를 적어 주고 다시 묻게 한다" 는 같은 약속을, 이번엔 mountmgr 와 kernelbase 사이에서 봤다. 차이는 **부르는 쪽 고리에 멈춤 장치가 없다** 는 것.
- **run132 과 이어진다.** flow.md 에서 파이프 절 바로 뒤에 놓였고, `BaseSetLastNTError` · `GetFileType` 같은 공용 부품을 그대로 가리킨다.
- **다음 벽**: 에뮬레이터가 창구 ① · ② 를 진짜처럼 답하면 `\\?\Volume{GUID}\` 가 돌아간다. 정직한 실패를 고르면 9 ~ 11 (`\??\C:\` 열기 · `0x900a8`) 이 다음 벽이다 (결과서 "다음 요청 후보" 2 → Q276).
- `ShellExecute*` 가 정말 이 자리를 지나는지 (가상 함수 잇기) 는 Q275 (동적) · Q276 (windows.storage · shell32 analyze) 에서 본다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q275** (동적, VM 묶음 — 미룸)

test.exe 는 이 길을 안 탄다. `ShellExecuteW` 를 부르는 프로세스 · 그 조건은 사용자 결정이다.

| # | 무엇을 모르나 | 왜 이번에 못 알았나 | 보는 자리 (run 문서) |
|---|---|---|---|
| 1 | 진짜 `\Device\HarddiskVolume3` 의 길이 칸 · IOSB.Information 이 `0x30` 인가 | 실행 때 값 | `KERNELBASE!GetVolumeNameForRoot+0x171` `eax`, `db @rbp-0x40 L0x30` |
| 2 | 첫 `0x6d0008` 이 정말 `0x80000005` 인가, Size · 항목 수 · 순서 · 링크 이름들, `+0x56f` 에서 LastError `0xea` 인가 | 실행 때 값 | `+0x37c` (첫째 · 둘째), 둘째 뒤 `db @rsi L<Size>` |
| 3 | `RtlNtStatusToDosError(0x80000005) = 0xea` 인지 | ntdll 표를 안 읽음 | — |
| 4 | 로그 `NtQueryInformationProcess(0x17)` 둘째의 호출자 | 이 export 길엔 하나뿐 | `ntdll!NtQueryInformationProcess` + 반환 주소를 shell32 / windows.storage PDB 이름으로 |
| 5 | `ShellExecute*` → `CInvokeCreateProcessVerb::Execute` 가 불리나, `_FixupWorkingDirectory +0x84` 가 `GetMountPoint` 를 부르나 | 가상 함수 | `_InitLocalDrive+0x47` 에서 `eax` |

### 정적으로 남은 것 → **Q276**

1. windows.storage · shell32 Ghidra analyze + 지도 (뿌리 `ShellExecuteExW` · `CInvokeCreateProcessVerb::Execute`) — 간접 호출 잇기. PDB 는 받아 둠. 하나씩.
2. `BasepGetVolumeNameFromReparsePoint` 실패 길 전체 — `NtCreateFile(\??\C:\)` 옵션, `0x900a8` 뒤 갈래, reparse 있을 때 성공 길.
3. ntdll `RtlDosPathNameToNtPathName_U` (`C:\` → `\??\C:\`) · `RtlNtStatusToDosError` 표 (`0x80000005` → ?) — sast/138 과 겹치니 그 뒤.
4. partmgr.sys / volmgr.sys 의 `0x4d0008` 처리 — 진짜 볼륨 장치 출력 바이트.
5. `GetVolumePathNameInternalW` 의 경로를 잘라 가며 다시 부르는 고리 (`+0x2c4 ~ +0x4f4`).
6. (보탬) `CreateFileInternal` 이 옵션 `0x20060` 을 만드는 비트 계산.
- plan.md Q276 줄엔 `+0x6eb` 검사 함수 (`0x128bf4`) 의 뜻도 같이 들어 있다.

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- 창구 ② 출력 종이 (`+0x2c2` 힙) 를 0 으로 채우는지 — Size 를 안 쓴 답의 결과가 이것으로 갈린다 (2.11) `[공백]`.
- 원문 예외 자리 `RtlCopyMemory` 가 kernelbase `memcpy` (`0x196e77`) 의 몸인지 `[추측]` (2.6).
- `+0x6eb` 검사 함수의 뜻 (2.18).
- mountmgr 장부 칸 이름 (장치 확장 `+0x10` 목록 머리 · 장치 항목 `+0x50` `DeviceName` · `+0x60` 고유ID …) — 칸 번호만, 구조체 이름은 확인 안 함 `[추측]`.

---

## 5. 더 읽기

- run 문서: [`run143-mountdev-name.md`](run143-mountdev-name.md) — 호출 지점 표 · 출력 칸 표 · 입력 칸 표 · mountmgr 검사 표 · syscall 차례 표 · status 표 · PE_emulator 최소 바이트
- 결과서: [`_workspace/sast/140_mountdev-name_result.md`](../../_workspace/sast/140_mountdev-name_result.md) — "엔진이 돌려줄 최소 바이트" · "정직한 실패" · 부록 (shell 쪽 부르는 자리 이름)
- 요청서: [`_workspace/sast/140_mountdev-name.md`](../../_workspace/sast/140_mountdev-name.md)
- flow.md: [`flow.md`](flow.md) 3장 파이프 절 뒤 "볼륨 이름 묻기" 절, 기준 (mountmgr · shell32 · windows.storage 줄), 4장 "3 (범위 밖 — 볼륨)" 줄, 5장 run143 줄 — 변경은 `git show c72729e -- runs/03_sprint/flow.md`
- 앞 해설: [run91 해설](run91-bcrypt-init-해설.md) 2.3 · 2.4 (도장은 성공, 바구니는 빈 채) · [run126 해설](run126-afd-addrlist-해설.md) 2.7 (종이가 작을 때) · [run132 해설](run132-npfs-pipes-해설.md) (FSCTL · DOS 오류 번호) · [run119 해설](run119-systemroot-link-해설.md) (안내판)
