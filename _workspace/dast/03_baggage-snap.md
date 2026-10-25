# 03 baggage-snap — 동적 분석 요청

- 날짜: 2026-10-25
- 스프린트: 03_sprint / 예정 run: 따로 안 닫음 (Q77b 캡처. 결과는 Q77 본 바퀴 run 문서에 합침 — 오프라인 읽기 Q77c 까지 끝난 뒤)
- 대기열 항목: **Q77b 본 덤프 (캡처)** — Q77 을 그 자리에서 나눔: Q77a 시험 (끝, dast/01·02) → **Q77b 캡처 (이것)** → Q77c 오프라인 읽기 (명령 묶음, 다음 요청)
- 관련: `_workspace/sast/38_baggage-checklist_result.md` ("Q77(사진)에서 떠야 할 것"), `_workspace/dast/02_snapshot-test.md`·`_runbook.md`, `ms/logs/02_snapshot-test_result.md` (C 방식 분석성공)
- 재시도: 없음

## 대화에서 정한 것 (plan.md 결정 기록 2026-10-25)

- 방식 = **C**: 멈춤점마다 호스트 VMware 스냅샷 → 서버 `vmss2core -W8` → `memory.dmp` → 디버거 VM WinDbg 로 오프라인. 이번 요청은 **캡처와 변환까지**. 구조체 읽기 명령 묶음은 Q77c.
- **lazy 꺼짐**: 타깃 `.vmx` 에 `mainMem.partialLazySave = "FALSE"` (원본 백업 `BOB Windows 11 24H2.vmx.bak-20261025-lazy`). 시험 스냅샷 `lazy-test` 에서 `vmware.log` 에 `MainMem: Write full memory image` → `Wrote 8192MiB in 1885ms`, 시작~끝 약 2초 `[관찰]`. 요청 줄에는 여전히 `lazy=1` 이 찍히니 **`Wrote ... MiB` / `Done with snapshot` 줄로 확인**한다.
  - 사용자 결정: 스냅샷마다 `Done with snapshot` 을 확인한 **뒤에** `g` (기다리기). 이제 2초라 부담 없음.
- **지점 8개 전부** (사용자 결정), **한 번의 test.exe 실행 안에서** 순서대로. 각 지점에서: 멈춤 → (짧은 라이브 기록) → 스냅샷 → 서버에서 `Done with snapshot` 확인 → `g`.
- 실행 중 안내는 **오케스트레이터가 runbook 대로 직접** 한다 (dast/01·02 와 같음). 서버 쪽 명령 (스냅샷 확인, `vmware.log` 시각, 변환, HOST NOTES) 은 오케스트레이터가 서버에서 실행. runbook 에 `[서버 — 오케스트레이터가 실행 가능]` 표시.

## 지점 (시간 순서)

| # | 멈출 곳 | 거는 법 | 이 사진으로 갈리는 것 | 근거 |
|---|---|---|---|---|
| K1 | `nt!PspAllocateThread` 입구 (rva `0x84296c`) | `bp /p <CMD>` (부모 cmd 스레드) | 프로세스 만들기 끝 (EPROCESS·주소 공간·PEB·ntdll 올리기·매개변수) / 스레드 만들기 전 | flow 그림 1, 2.1 |
| K2 | `nt!PspInsertProcess` 입구 (rva `0x84480c`) | `bp /p <CMD>` | 스레드 만들기 (ETHREAD·TEB·스택·트랩 프레임) / 핸들 표·프로세스 목록에 넣기 | 같음 |
| K3 | `nt!PspCallProcessNotifyRoutines` 입구 (rva `0x9b1ca8`) | `bp /p <CMD>` (dast/01·02 기준점) | 넣기 끝 / 알림·핸들 만들기·마무리 | 같음 |
| ⓪ | `nt!PspUserThreadStartup` 입구 (rva `0x910de0`) | `bp /p <TEST>` + **첫 스레드만** | 부모가 만든 것 전부 (부모의 `CsrClientCallServer`·`NtResumeThread` 포함) / 자식 스레드가 커널에서 출발하며 한 것 | flow 그림 2, 2.3 |
| ① | `ntdll!LdrInitializeThunk` 입구 (rva `0xe1420`) | `bp /p <TEST>` + 첫 스레드만 | 커널 몫 끝 / ntdll 시작 | dast/01·02 |
| ①-a | `ntdll!LdrpInitializeProcess+0x199d` (CALL → `LdrpInitializeKernel32Functions`, ret `+0x19a2`) | `bp /p <TEST>` | ntdll 혼자 한 것 (힙·ntdll 항목·실행 옵션 등) / KERNEL32·KERNELBASE 준비 | flow (7) `[정적]`, `LdrpInitializeProcess.asm` |
| ①-b | `ntdll!LdrpInitializeProcess+0x1bb6` (CALL → `LdrpInitializeGraphRecurse`, ret `+0x1bbb`, BEFORE-GRAPH) | `bp /p <TEST>` | 로더가 깐 것 (exe import 올리기·연결) / dll2·dll1 시작 함수·TLS 콜백이 쓴 것 | flow (13), 스킬 runbook-template BEFORE-GRAPH |
| ② | `ntdll!NtContinue` 입구 (rva `0x161610`, `LdrInitializeThunk+0x13` 이 부름) | `bp /p <TEST>` + 첫 스레드만 | 초기화 끝 (짐 지도 범위 끝) | sast/38 추천 |

- **첫 스레드만** 조건이 필요한 이유: `LdrEnableParallelLoading` 이후 ntdll 일꾼 스레드가 생길 수 있고 (flow 3장), 그 스레드도 `PspUserThreadStartup`·`LdrInitializeThunk`·`NtContinue` 를 지난다. ⓪ 에서 첫 스레드 ETHREAD 를 얻은 뒤 그 값으로 조건을 단다 (`/t` 는 쓰지 않음 — P6; 방법은 dast-mentor 가 스킬대로. dast/01 판정이 제안한 꼴: `.if (@$thread == <첫 ETHREAD>) {} .else {gc}`).
- ⓪ 은 test.exe 첫 스레드가 처음 도는 순간인데, 그때 `<TEST>` 는 이미 안다 (K1 에서 얻을 수 있음 — K1 은 `PspAllocateProcess` 가 끝난 뒤라 EPROCESS 가 있다 `[정적]`). 첫 ETHREAD 는 K2 (스레드 만든 뒤) 에서 얻을 수 있다 `[추측]` — 어디서 얻을지는 dast-mentor 가 정한다.
- K1~K3 은 부모 cmd 의 `NtCreateUserProcess` 안이다. 이 함수들이 test.exe 를 만드는 동안 **한 번씩만** 불리는지는 `[추측]` (cmd 는 다른 프로세스·스레드를 안 만듦). 두 번 서면 기록하고 첫 번째 것만 스냅샷.
- 멈춤점 거는 순서 (예): 처음에 K1~K3 (`/p <CMD>`) → K1 에서 `<TEST>` 얻기 → ①-a·①-b (`/p <TEST>`) 미리 걸기 → K2 에서 첫 ETHREAD → ⓪·①·② 를 첫 스레드 조건으로 걸기. 지난 지점의 멈춤점은 서고 나면 지운다 (P13 정신).
- 커널 쪽 지점(K1~K3)은 test.exe 의 유저 공간이 반쯤 만들어진 상태라, 오프라인에서 test.exe 문맥으로 바꿀 때 PEB 등이 아직 없을 수 있다 — 정상 (Q77c 에서 다룸).

## 확인할 것 (정적으로 못 본 것)

1. 8 지점에서 각각 멈추고, 그 순간 스냅샷 8장을 같은 test.exe 실행 안에서 얻는다.
2. 각 지점의 라이브 기록 (짧게): `!process -1 0` (지금 누구 문맥인지), `!thread -1 0` (ETHREAD·CPU), `k 5`, `r` — 그리고 test.exe 가 이미 있으면 `!process <TEST> 0`. ① 에서는 dast/02 와 같은 대조값 (`? @$peb`, `dt nt!_PEB`, `dq @$peb L4`, `dt nt!_CONTEXT @rcx …`, 숫자 VA `!vtop`). 덤프의 CPU 문맥은 디버거 루프라 쓸 수 없으므로 (dast/02) **레지스터는 라이브에서 남긴다**. 커널 지점에서는 `dt nt!_KTHREAD @$thread InitialStack TrapFrame` 도.
3. 각 스냅샷: `vmware.log` 의 `TakeSnapshot start` / `Wrote ... MiB` / `Done with snapshot` 시각, 파일 이름·크기.
4. 8장 모두 `vmss2core -W8` 로 `memory.dmp` 변환 성공 + 오프라인에서 열리는지 최소 확인 (각 덤프에서 `!process 0 0 test.exe` 한 줄 — test.exe EPROCESS 가 라이브와 같은지). 구조체 읽기는 Q77c.

## 서버 쪽 (오케스트레이터)

- 스냅샷 이름: `03-K1`, `03-K2`, `03-K3`, `03-P0`, `03-P1`, `03-P1a`, `03-P1b`, `03-P2` (VMware 메뉴 이름. 한글 없이).
- 변환: **사본을 만들지 않고** VMware 폴더의 `.vmsn`/`.vmem` 을 바로 읽어 `ms/dump/03_baggage-snap/<지점>/memory.dmp` 로 쓴다 (장당 디스크 약 9 GB → 약 5 GB). `vmss2core` 는 현재 폴더에 `memory.dmp` 를 쓰므로 지점 폴더로 `cd` 해서 실행.
- 변환은 라이브가 다 끝난 뒤 한꺼번에 해도 되고, 지점마다 해도 된다 (라이브를 늦추지 않게 뒤로 미는 걸 기본으로).
- 디스크: 시작 전 `df` 로 여유 확인 (지금 약 470 GB). 8장 스냅샷 + 8개 `.dmp` ≈ 70~80 GB `[추측]`.
- 시험 스냅샷 `lazy-test` 와 dast/02 의 `02_snapshot-test` 는 VMware 에 남아 있다. 지우는 건 사용자 결정 (이번 범위 밖, 헷갈리지 않게 시각·이름으로 가린다).

## 디버거 VM 쪽 (선택, 사용자 결정 — runbook 에 선택지로)

- 오프라인 로그를 복사·붙여넣기 하지 않게, 디버거 VM 에 **쓰기 가능한 공유 폴더**를 하나 더 켜는 안: 호스트 `/home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/logs` ↔ 이름 `logs`. 그러면 WinDbg `.logopen "\\vmware-host\Shared Folders\logs\…"` 로 서버 파일에 바로 쓴다. Q77c 에서 출력이 아주 길어지므로 미리 켜 두면 좋다 `[추측]`. 라이브 로그도 이 길로 쓸 수 있다. 켤지는 사용자.

## 성공 기준

- 라이브 로그에 8 지점 모두 `Breakpoint N hit` 으로 섰고, 각 지점의 `!process -1 0`·`!thread -1 0`·`r` 이 있다. ⓪·①·② 는 test.exe **첫 스레드** (ETHREAD 같음).
- `vmware.log` 에 스냅샷 8개 모두 `Wrote ... MiB` + `Done with snapshot`, 각 `g` 는 그 뒤.
- 8개 `memory.dmp` 가 만들어졌고, 각 덤프에서 `!process 0 0 test.exe` 가 라이브 `<TEST>` 와 같다 (K1 은 test.exe 가 아직 목록에 없을 수 있음 — 그러면 그대로 기록).
- 일부 지점만 성공하면 → 성공한 지점과 실패 이유를 판정에 적는다 (다시 할지는 사용자와).

## 끝 지점

- ② 스냅샷 확인 뒤 `bc *` → `.logclose` → `g` (test.exe 끝까지). 그 뒤 서버 변환·오프라인 최소 확인.

## 범위 밖

- 구조체 전체 읽기 (sast/38 표의 칸들) — Q77c.
- 스냅샷·백업 정리 — 사용자 결정 뒤.
- 하네스 고치기 (pitfalls·환경 표) — 오케스트레이터가 사용자 검토 뒤 따로.
