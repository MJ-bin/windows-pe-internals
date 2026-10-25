# 03 baggage-snap — 실행 안내

> 로그 붙여넣을 곳 (둘 다 지금 빈 파일):
> - 라이브: 서버 `ms/logs/03_baggage-snap.txt` ← 디버거 VM `C:\logs\03_baggage-snap.txt`
> - 오프라인 최소 확인: 서버 `ms/logs/03_baggage-snap_offline.txt` ← 디버거 VM `C:\logs\03_baggage-snap_offline.txt`
> 재부팅: 필요 없음 · 예상 시간: 라이브 약 40~60분 (지점 8개 × 기록·스냅샷·확인) + 변환 약 5분 (장당 ~26초) + 오프라인 확인 약 20분
> 이번에 보는 것: **한 번의 test.exe 실행 안에서** 8 지점에 차례로 멈춰, 지점마다 라이브 기록 + VMware 스냅샷 1장 → 서버 `vmss2core -W8` 로 `memory.dmp` 8개 → 각 덤프에서 test.exe 가 보이는지 최소 확인
> 지금 상태: 타깃 켜져 있음, 디버거 연결됨, **Break 안 함**, test.exe 아직 안 돌림 (lazy-test 스냅샷 직후)

**VM** (호스트 = 이 분석 서버, VMware Workstation 17.6.3): 타깃 `bob_baseline_vm` ("BOB Windows 11 24H2", `192.168.100.133`, lazy 꺼짐) · 디버거 `Windows 11 x64` (`192.168.100.130`) · **건드리지 않음** `WIN11-26H1-HDAUDIO`.

**누가 치나**: `[사용자]` = VM 안 / VMware 메뉴 · `[서버 — 오케스트레이터가 실행 가능]` = 서버 터미널.

## 지점 8개 (시간 순서) — 멈춤점 번호를 고정한다

| bp 번호 | 지점 | 스냅샷 이름 | 덤프 폴더 | 거는 곳 | 필터 | 언제 건다 |
|---|---|---|---|---|---|---|
| 1 | K1 | `03-K1` | `K1` | `nt!PspAllocateThread` | `/p <CMD>` | 2단계 |
| 2 | K2 | `03-K2` | `K2` | `nt!PspInsertProcess` | `/p <CMD>` | 2단계 |
| 3 | K3 | `03-K3` | `K3` | `nt!PspCallProcessNotifyRoutines` | `/p <CMD>` | 2단계 |
| 4 | ⓪ | `03-P0` | `P0` | `nt!PspUserThreadStartup` | `/p <TEST>` (첫 번째로 서는 것 = 첫 스레드) | K1 |
| 5 | ① | `03-P1` | `P1` | `ntdll!LdrInitializeThunk` | `/p <TEST>` + `@$thread == <T1>` | ⓪ |
| 6 | ①-a | `03-P1a` | `P1a` | `ntdll!LdrpInitializeProcess+0x199d` (CALL `LdrpInitializeKernel32Functions` 자리) | `/p <TEST>` | K1 |
| 7 | ①-b | `03-P1b` | `P1b` | `ntdll!LdrpInitializeProcess+0x1bb6` (CALL `LdrpInitializeGraphRecurse` 자리, BEFORE-GRAPH) | `/p <TEST>` | K1 |
| 8 | ② | `03-P2` | `P2` | `ntdll!NtContinue` | `/p <TEST>` + `@$thread == <T1>` + 부른 곳 = `LdrInitializeThunk+0x18` | ⓪ |

근거 `[정적]`:

- 이름·rva: `_index.tsv` 의 `PspAllocateThread 0x84296c`, `PspInsertProcess 0x84480c`, `PspCallProcessNotifyRoutines 0x9b1ca8`, `PspUserThreadStartup 0x910de0`, `LdrInitializeThunk 0xe1420`, `NtContinue 0x161610` 이다.
- 순서 K1 → K2 → K3: `NtCreateUserProcess` 디컴파일에서 `PspAllocateThread` → `PspInsertProcess` → `PspInsertThread` 순서로 부르고, `PspCallProcessNotifyRoutines` 는 `PspInsertThread` 안에서 부른다.
- K1·K2 의 `rcx` 는 둘 다 새 프로세스 객체다 (같은 `local_780`). 그래서 **K1 의 `rcx` 가 `<TEST>`** 다. K1 땐 test.exe 가 프로세스 목록에 아직 없을 수 있다.
- ①-a·①-b 는 **call 자리**(`_calls.tsv` call_off)라 그 함수를 부르기 직전에 선다 (P22).
- ② `NtContinue` 는 **별칭 무리**다 (`NtContinue`, `ZwContinue` 두 이름이 한 주소). 그래서 부른 곳이 `LdrInitializeThunk+0x18` (ret_off) 일 때만 선다 (P19).
- 나머지 넷은 별칭 표에 없다.

## 값 이름 (오케스트레이터가 채워 줄 수 있다)

| 이름 | 얻는 곳 |
|---|---|
| `<CMDPID>` · `<CMD>` | 0단계 tasklist · 1단계 `!process 0 0 cmd.exe` |
| `<THUNKVA>` | 1단계 `x ntdll!LdrInitializeThunk` |
| `<TEST>` · `<PEBVA>` | K1 의 `? @rcx` · K1 의 `dt nt!_EPROCESS @rcx ... Peb` |
| `<T1>` | ⓪ 의 `? @$thread` |

- 숫자를 옮길 때 가운데 **역따옴표(`` ` ``)는 뺀다**. 예: ``ffffe78a`afee7080`` → `ffffe78aafee7080`.

## 지점마다 되풀이하는 절차 (S-루틴)

멈출 때마다 이 순서로 한다. **`g` 는 반드시 5번 뒤.**

1. `[사용자]` 그 지점의 **기록 블록**을 친다 (아래 각 지점).
2. `[사용자]` **스냅샷 직전 확인**: VMware 에서 고른 탭이 `bob_baseline_vm` 이다. `WIN11-26H1-HDAUDIO` 도 `Windows 11 x64` 도 아니다.
3. `[사용자]` VMware → `bob_baseline_vm` → VM → Snapshot → **Take Snapshot...** → 이름 = 표의 스냅샷 이름 → OK. 타깃 화면 안은 클릭하지 않는다 (P15).
4. `[서버 — 오케스트레이터가 실행 가능]` 쓰기가 끝났는지 본다:
```
grep -nE "TakeSnapshot|Write full memory|Wrote [0-9]+MiB|Done with snapshot" "/home/mjbin/vmware/bob_baseline_vm/vmware.log" | tail -n 5
```
   - 정상이면: 방금 시각의 `Wrote 8192MiB in ...ms` 와 `Done with snapshot` 이 있다 (lazy-test 때 약 2초). 요청 줄에 `lazy=1` 이 찍혀도 괜찮다 — `Wrote`/`Done` 으로 판단한다.
   - 이 다섯 줄(시각 포함)을 HOST NOTES 에 그 지점 이름으로 옮겨 둔다.
   - 이상하면: `Done with snapshot` 이 1분 넘게 안 나오면 **`g` 하지 말고** 기다린다. `End lazy IO` 같은 lazy 줄이 보이면 lazy 가 다시 켜진 것 → 멈추고 알린다.
5. `[사용자]` 그 지점의 **다음 블록** (`bc <번호>`, 새 멈춤점, `g`) 을 친다.

---

## 0. 준비

1. `[사용자]` 타깃에서 **보통 cmd** 를 새로 연다 (test.exe 는 아직 실행하지 않음):
```
cd C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin
dir test.exe
title pesnap
tasklist /v /fi "imagename eq cmd.exe"
```
   - 창 제목 `pesnap` 줄의 PID (10진수) = `<CMDPID>`.
2. `[서버 — 오케스트레이터가 실행 가능]` 시작 전 모습:
```
vmrun -T ws list
grep -n "partialLazySave" "/home/mjbin/vmware/bob_baseline_vm/BOB Windows 11 24H2.vmx"
grep -nE "displayName|fileName" "/home/mjbin/vmware/bob_baseline_vm/BOB Windows 11 24H2.vmsd"
ls -l --time-style=full-iso /home/mjbin/vmware/bob_baseline_vm/ | grep -i snapshot
df -h /home/mjbin
```
   - 정상이면: `mainMem.partialLazySave = "FALSE"` 가 있다. 기존 스냅샷(`lazy-test`, `02_snapshot-test`)의 파일 번호를 적어 둔다 — 이번 8장과 섞이지 않게. 디스크 여유가 약 80 GB 보다 넉넉하다.
3. (선택, 사용자 결정) **쓰기 가능한 `logs` 공유 폴더** — 로그를 복사·붙여넣기 하지 않으려면.
   - `[사용자]` VMware → **`Windows 11 x64`** 에서만 → VM → Settings → Options → Shared Folders → Add. Host path `/home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/logs`, Name `logs`, **Read-only 끔**.
   - 켰다면 아래 `.logopen` 의 경로를 `"\\vmware-host\Shared Folders\logs\03_baggage-snap.txt"` (오프라인은 `..._offline.txt`) 로 바꿔 친다. 이 경우 서버 파일에 바로 써진다.
   - 안 켜면 그냥 `C:\logs\...` (기본).

## 1. Break → 로그 시작 → 이름 확인

`[사용자]` WinDbg 에서 **Break** (P1). `kd>` 에서:
```
.logopen C:\logs\03_baggage-snap.txt
.echo ==== STEP1 SETUP
.reload
!lmi nt
bc *
bl
!process 0 0 cmd.exe
? 0n<CMDPID>
```
- `? 0n<CMDPID>` 의 16진수와 `Cid` 가 같은 줄의 `PROCESS` = `<CMD>` (P5).
- `!lmi nt` GUID `C29EBFB0-6B78-B3C0-20DC-A66D99713F9E` Age `1`.

이어서 이름·자리 확인 (cmd 문맥에서 ntdll 이 보인다):
```
.echo ==== STEP1 NAMES
.reload /user
x nt!PspAllocateThread
x nt!PspInsertProcess
x nt!PspCallProcessNotifyRoutines
x nt!PspUserThreadStartup
x ntdll!LdrInitializeThunk
x ntdll!NtContinue
x ntdll!LdrpInitializeProcess
? nt!PspAllocateThread - nt
? nt!PspInsertProcess - nt
? nt!PspCallProcessNotifyRoutines - nt
? nt!PspUserThreadStartup - nt
? ntdll!LdrInitializeThunk - ntdll
? ntdll!NtContinue - ntdll
u ntdll!LdrpInitializeProcess+0x199d L1
u ntdll!LdrpInitializeProcess+0x1bb6 L1
u ntdll!LdrInitializeThunk+0x13 L1
```
- 정상이면:
  - `x` 7줄이 다 주소를 낸다. `x ntdll!NtContinue` 는 한 줄이거나 `ZwContinue` 와 같은 주소다.
  - `?` 6줄 = `0x84296c`, `0x84480c`, `0x9b1ca8`, `0x910de0`, `0xe1420`, `0x161610`.
  - `u` 세 줄 = `call ntdll!LdrpInitializeKernel32Functions`, `call ntdll!LdrpInitializeGraphRecurse`, `call ntdll!NtContinue` (또는 `ZwContinue`).
  - `x ntdll!LdrInitializeThunk` 주소 = `<THUNKVA>`.
- 이상하면: 값이 하나라도 다르면 **여기서 멈추고** 출력 붙여 주세요 (멈춤점을 걸기 전이라 안전).

## 2. K1~K3 멈춤점 → test.exe 실행
```
bp1 /p <CMD> nt!PspAllocateThread
bp2 /p <CMD> nt!PspInsertProcess
bp3 /p <CMD> nt!PspCallProcessNotifyRoutines
bl
g
```
- `bl` 에 1·2·3 이 있고 각각 `Match process data <CMD>`.
- `[사용자]` 타깃 `pesnap` cmd 에서 `test.exe` 실행.
- 정상이면: `Breakpoint 1 hit` / `nt!PspAllocateThread`.
- 이상하면: test.exe 를 치기 전에 서면 `<CMD>` 가 틀렸다 → `bl` 붙여 주세요. 중간에 `Break instruction exception` 이 끼면 `g`.

## K1. `nt!PspAllocateThread` (bp 1)

기록:
```
.echo ==== K1 PspAllocateThread
!process -1 0
!thread -1 0
k 5
r
r cr3
dt nt!_KTHREAD @$thread InitialStack TrapFrame
dt nt!_KTRAP_FRAME (poi(@$thread+0x28)-0x190) Rip Rsp Rcx Rdx SegCs
.echo ==== K1 TEST
? @rcx
dt nt!_EPROCESS @rcx UniqueProcessId ImageFileName Peb SectionBaseAddress
!process 0 0 test.exe
```
- 정상이면:
  - `!process -1 0` = **cmd.exe** (부모 문맥).
  - `dt nt!_EPROCESS @rcx` 의 `ImageFileName` = `"test.exe"`. 그러면 `? @rcx` 값 = `<TEST>`, `Peb` 값 = `<PEBVA>`.
  - `!process 0 0 test.exe` 는 **빈칸이어도 정상** (아직 목록에 안 넣음 `[추측]`).
- 이상하면: `ImageFileName` 이 test.exe 가 아니면 → 스냅샷 찍지 말고 멈추고 붙여 주세요.
- 체크포인트: `<TEST>` 가 헷갈리면 붙여 주세요.

→ **S-루틴 2~4** (스냅샷 `03-K1`). 그다음:
```
bc 1
bp4 /p <TEST> nt!PspUserThreadStartup
bp6 /p <TEST> ntdll!LdrpInitializeProcess+0x199d
bp7 /p <TEST> ntdll!LdrpInitializeProcess+0x1bb6
bl
g
```
- `bl` 에 2·3·4·6·7. 4·6·7 은 `Match process data <TEST>`.
- 이상하면: 6·7 에 `Unable to insert breakpoint` 류가 나오면 붙여 주세요 (P11).

## K2. `nt!PspInsertProcess` (bp 2)
```
.echo ==== K2 PspInsertProcess
!process -1 0
!thread -1 0
k 5
r
r cr3
dt nt!_KTHREAD @$thread InitialStack TrapFrame
dt nt!_KTRAP_FRAME (poi(@$thread+0x28)-0x190) Rip Rsp Rcx Rdx SegCs
? @rcx
!process <TEST> 0
```
- 정상이면: `!process -1 0` = cmd.exe, `? @rcx` = `<TEST>`.
- 이상하면: `? @rcx` 가 `<TEST>` 가 아니면 기록만 하고 진행 (그 자체가 발견).

→ **S-루틴 2~4** (`03-K2`). 그다음:
```
bc 2
g
```

## K3. `nt!PspCallProcessNotifyRoutines` (bp 3)
```
.echo ==== K3 PspCallProcessNotifyRoutines
!process -1 0
!thread -1 0
k 5
r
r cr3
dt nt!_KTHREAD @$thread InitialStack TrapFrame
dt nt!_KTRAP_FRAME (poi(@$thread+0x28)-0x190) Rip Rsp Rcx Rdx SegCs
!process 0 0 test.exe
!process <TEST> 2
```
- 정상이면: `!process -1 0` = cmd.exe, `!process 0 0 test.exe` 가 `<TEST>` 를 낸다 (dast/01·02 와 같음). `!process <TEST> 2` 에 스레드 줄이 보이면 그 `THREAD` 값을 적어 둔다 (⓪ 의 `<T1>` 과 같을 것 `[추측]`).

→ **S-루틴 2~4** (`03-K3`). 그다음 — 부모 쪽 멈춤점은 이걸로 끝 (P13):
```
bc 3
bl
g
```
- `bl` 에 4·6·7 만 남는다.

## ⓪. `nt!PspUserThreadStartup` (bp 4) — 여기서 `<T1>` 을 얻는다
```
.echo ==== P0 PspUserThreadStartup
!process -1 0
!thread -1 0
? @$thread
k 5
r
r cr3
dt nt!_KTHREAD @$thread InitialStack TrapFrame Teb
dt nt!_KTRAP_FRAME (poi(@$thread+0x28)-0x190) Rip Rsp Rcx Rdx SegCs
```
- 정상이면:
  - `!process -1 0` = **test.exe** (`<TEST>`). `? @$thread` = `<T1>` (첫 스레드, run4: 첫 번째로 서는 것이 첫 스레드).
  - `k` 에 `nt!KiStartUserThread+0x28` (run4).
  - 트랩 프레임의 `Rip`·`Rcx` 는 run15·16 과 맞춰 볼 값이다 (유저로 갈 자리 `[정적]`). 무엇이 나오든 기록만.
- 이상하면: test.exe 가 아니면 멈추고 붙여 주세요.

→ **S-루틴 2~4** (`03-P0`). 그다음 (`<T1>` 을 채워서):
```
bc 4
bp5 /p <TEST> ntdll!LdrInitializeThunk ".if (@$thread == <T1>) {.echo ==== HIT P1} .else {gc}"
bp8 /p <TEST> ntdll!NtContinue ".if ((@$thread == <T1>) and (poi(@rsp) == ntdll!LdrInitializeThunk+0x18)) {.echo ==== HIT P2} .else {gc}"
bl
g
```
- `bl` 에 5·6·7·8. 5·8 뒤에 명령 문자열이 붙어 있다.
- 조건이 참이면 `.echo` 만 하고 **멈춘 채로** 있다 (`g` 가 없으니까). 거짓이면 `gc` 로 지나간다.

## ①. `ntdll!LdrInitializeThunk` (bp 5)

`==== HIT P1` 이 찍히고 멈춘다.
```
.echo ==== P1 LdrInitializeThunk
!process -1 0
!thread -1 0
? @$thread
k 5
r
r cr3
dt nt!_KTHREAD @$thread InitialStack TrapFrame
dt nt!_KTRAP_FRAME (poi(@$thread+0x28)-0x190) Rip Rsp Rcx Rdx SegCs
? @$peb
? @$teb
dt nt!_EPROCESS @$proc UniqueProcessId Peb ImageFileName SectionBaseAddress
dt nt!_PEB @$peb ImageBaseAddress Ldr ProcessHeap ProcessParameters
dt nt!_CONTEXT @rcx ContextFlags Rip Rsp Rcx
dq @$peb L4
db ntdll!LdrInitializeThunk L10
```
`!vtop` 은 **숫자로** (P25 제안, dast/02):
```
!vtop 0 <PEBVA>
!vtop 0 <THUNKVA>
```
- 정상이면: test.exe, `? @$thread` = `<T1>`, `rip` = `<THUNKVA>`, `Ldr`·`ProcessHeap` `(null)` (dast/01·02 와 같음).
- 트랩 프레임 줄은 이번에 새로 남기는 값이다. 오프라인에서 레지스터 대신 쓸 수 있는지 Q77c 에서 맞춰 본다 (dast/02: 덤프의 CPU 문맥은 디버거 루프).

→ **S-루틴 2~4** (`03-P1`). 그다음:
```
bc 5
g
```

## ①-a. `ntdll!LdrpInitializeProcess+0x199d` (bp 6)
```
.echo ==== P1a LdrpInitializeProcess+0x199d
!process -1 0
!thread -1 0
? @$thread
k 5
r
r cr3
dt nt!_KTHREAD @$thread InitialStack TrapFrame
dt nt!_KTRAP_FRAME (poi(@$thread+0x28)-0x190) Rip Rsp Rcx Rdx SegCs
u @rip L1
```
- 정상이면: test.exe, `? @$thread` = `<T1>`, `u @rip L1` = `call ntdll!LdrpInitializeKernel32Functions`. `k` 에 `LdrpInitialize`·`LdrInitializeThunk` 쪽 사슬 (run7).
- 이상하면: 다른 스레드면 기록하고 멈추고 알려 주세요 (이 자리는 첫 스레드만 지나갈 것 `[추측]`).

→ **S-루틴 2~4** (`03-P1a`). 그다음:
```
bc 6
g
```

## ①-b. `ntdll!LdrpInitializeProcess+0x1bb6` (bp 7, BEFORE-GRAPH)
```
.echo ==== P1b LdrpInitializeProcess+0x1bb6
!process -1 0
!thread -1 0
? @$thread
k 5
r
r cr3
dt nt!_KTHREAD @$thread InitialStack TrapFrame
dt nt!_KTRAP_FRAME (poi(@$thread+0x28)-0x190) Rip Rsp Rcx Rdx SegCs
u @rip L1
.reload /user
lm m dll*
lm m test
```
- 정상이면: `u @rip L1` = `call ntdll!LdrpInitializeGraphRecurse`. `lm m dll*` 에 dll1·dll2 가 이미 있다 (BEFORE-GRAPH). 이 주소는 기록용이다 — 멈춤점에 쓰지 않는다 (P9).

→ **S-루틴 2~4** (`03-P1b`). 그다음:
```
bc 7
bl
g
```
- `bl` 에 8 만 남는다.

## ②. `ntdll!NtContinue` (bp 8) — 마지막

`==== HIT P2` 가 찍히고 멈춘다.
```
.echo ==== P2 NtContinue
!process -1 0
!thread -1 0
? @$thread
k 5
r
r cr3
dt nt!_KTHREAD @$thread InitialStack TrapFrame
dt nt!_KTRAP_FRAME (poi(@$thread+0x28)-0x190) Rip Rsp Rcx Rdx SegCs
? poi(@rsp)
dt nt!_CONTEXT @rcx ContextFlags Rip Rsp Rcx
dt nt!_PEB @$peb ImageBaseAddress Ldr ProcessHeap ProcessParameters
```
- 정상이면: `? poi(@rsp)` = `<THUNKVA>+0x18`, `? @$thread` = `<T1>`. `dt nt!_CONTEXT @rcx` 의 `Rip` 는 ntdll+0xacac0 (`RtlUserThreadStart`) 일 것 `[정적]`. 이제 `Ldr`·`ProcessHeap` 에는 값이 있다.

→ **S-루틴 2~4** (`03-P2`). 그다음 **라이브 끝**:
```
.echo ==== END
bc *
bl
.logclose
g
```
- 정상이면: test.exe 가 끝까지 돌고 cmd 프롬프트가 돌아온다.
- 이상하면: `Single step exception` 이면 `bc *` → `g` (P12).
- `[사용자]` (공유 폴더를 안 썼으면) `C:\logs\03_baggage-snap.txt` 를 **전부** 서버 `ms/logs/03_baggage-snap.txt` 에 붙인다.

---

## 9. 서버 — 스냅샷 이름 ↔ 파일 → 변환 `[서버 — 오케스트레이터가 실행 가능]`

1. 이름과 파일 번호를 맞춘다:
```
grep -nE "displayName|fileName" "/home/mjbin/vmware/bob_baseline_vm/BOB Windows 11 24H2.vmsd"
ls -l --time-style=full-iso /home/mjbin/vmware/bob_baseline_vm/ | grep -i snapshot
df -h /home/mjbin
```
   - `03-K1` … `03-P2` 마다 `-Snapshot<N>.vmsn` 번호를 적는다. 시각이 K1 → P2 순서인지도 본다.
2. 지점마다 (사본 없이 VMware 폴더의 파일을 바로 읽는다). `<P>` = 덤프 폴더 이름 (`K1` `K2` `K3` `P0` `P1` `P1a` `P1b` `P2`), `<N>` = 그 지점의 번호:
```
mkdir -p /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/03_baggage-snap/<P>
cd /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/03_baggage-snap/<P>
time vmss2core -W8 "/home/mjbin/vmware/bob_baseline_vm/BOB Windows 11 24H2-Snapshot<N>.vmsn" "/home/mjbin/vmware/bob_baseline_vm/BOB Windows 11 24H2-Snapshot<N>.vmem"
ls -l memory.dmp
du -h memory.dmp
```
   - 정상이면: 끝에 `Finished writing core.`, `memory.dmp` 약 8,589,942,784 B (dast/02).
   - 이상하면: 그 지점만 출력을 기록하고 다음 지점으로 간다. 다 끝나면 실패한 것만 모아 보고한다.
3. 마지막에:
```
cd /home/mjbin/prj/bob/teamprj/windows-pe-internals
git check-ignore -v ms/dump/03_baggage-snap/K1/memory.dmp
df -h /home/mjbin
```

## 10. 오프라인 최소 확인 (디버거 VM, 덤프 8개)

커널 세션 WinDbg 와 **별개인 새 WinDbg** 에서. 공유 폴더 `dump` (읽기 전용, dast/02 에 켬) 를 그대로 쓴다. 디버거 VM 디스크를 아끼려고 **복사하지 않고 공유 폴더에서 바로 연다** (느리면 그 덤프만 `C:\dump` 로 복사).

덤프마다 (`<P>` 를 바꿔 가며, 순서 K1 → P2):

1. `[사용자]` File → **Open dump file** → `\\vmware-host\Shared Folders\dump\03_baggage-snap\<P>\memory.dmp`.
2. 로딩이 끝나면 — **첫 덤프(K1)만** `.logopen`, 나머지는 `.logappend`:
```
.logopen C:\logs\03_baggage-snap_offline.txt
```
```
.logappend C:\logs\03_baggage-snap_offline.txt
```
3. 그다음:
```
.echo ==== OFF <P>
!lmi nt
!process 0 0 test.exe
.logclose
```
4. `[사용자]` File → **Stop debugging** (덤프 닫기) → 다음 덤프.

- 정상이면: `!lmi nt` GUID 가 라이브와 같다. `!process 0 0 test.exe` 의 `PROCESS` = `<TEST>`.
- **K1 은 빈칸이어도 정상** (라이브 K1 에서도 목록에 없었다면). 라이브 K1 의 `!process 0 0 test.exe` 결과와 같은지만 본다.
- "Unable to get program counter" 는 정상 (덤프의 CPU 문맥은 디버거 루프, dast/02).
- 마지막에 `[사용자]` (공유 폴더를 안 썼으면) `C:\logs\03_baggage-snap_offline.txt` 를 **전부** 서버 `ms/logs/03_baggage-snap_offline.txt` 에 붙인다.

## 11. HOST NOTES `[서버 — 오케스트레이터가 실행 가능]`

라이브 로그 `ms/logs/03_baggage-snap.txt` 맨 아래에 붙인다 (모르는 칸 `?`):
```
==== HOST NOTES
(filled by orchestrator from server-side commands; not debugger output)
vmx: mainMem.partialLazySave = "FALSE"
existing_snapshots_before: <names and file numbers>
K1:  name 03-K1  file Snapshot<N>  TakeSnapshot <t>  Wrote <MiB in ms> <t>  Done <t>  g_after_done <yes/no>
K2:  ...
K3:  ...
P0:  ...
P1:  ...
P1a: ...
P1b: ...
P2:  ...
snap_sizes: <per point: vmsn bytes / vmem bytes / du>
vmss2core: <per point: exit / last line / real time>
dmp_sizes: <per point: bytes / du>
disk: before <df>  after <df>
git_check_ignore: <line>
logs_shared_folder: <not used / used>
debugger_issues: <none / ...>
```
그다음 "로그 붙였어".

---

## 자주 하는 실수 (이번 run 관련)

- **P1** Break 먼저, 그다음 `.logopen`.
- **P2** 명령·`.echo` 영어만.
- **P4** 커널 멈춤점은 전부 `/p`. K1~K3 = `<CMD>`, ⓪ = `<TEST>`. `PspUserThreadStartup` 은 모든 유저 스레드가 지난다 — `/p` 를 빼먹으면 계속 선다.
- **P5** cmd 는 EPROCESS 로 (`<CMDPID>` → `Cid`).
- **P6** `/t` 는 안 쓴다. 첫 스레드만은 `.if (@$thread == <T1>)` 조건으로.
- **P8 / P13** 지점마다 `bc <번호>` 로 그 멈춤점만 지운다. 부모 쪽 1·2·3 은 K3 에서 다 지운다 (test.exe 끝난 뒤 cmd 가 다시 서는 일 방지).
- **P9** dll 주소는 멈춤점에 안 쓴다. ①-b 의 `lm m dll*` 는 기록용.
- **P12** 끝나고 이상하게 서면 `bc *` → `g`.
- **P15** 멈춘 동안 타깃 화면 클릭 금지. 스냅샷은 VMware 메뉴로.
- **P17** 끝나면 `bc *`.
- **P19** `NtContinue` = `ZwContinue` (별칭 무리). bp 8 은 `poi(@rsp) == ntdll!LdrInitializeThunk+0x18` 조건까지 붙인다.
- **P22** ①-a·①-b 는 call 자리 (`+0x199d`, `+0x1bb6`). ret 자리(`+0x19a2`, `+0x1bbb`)와 섞지 않는다.
- **dast/01·02 고침**:
  - `!vtop` 은 숫자 VA 로 (역따옴표 빼고).
  - 시간은 `.time` 대신 서버 `vmware.log` 의 `TakeSnapshot`/`Wrote`/`Done` 시각.
  - 레지스터는 **라이브에서** 남긴다 (`r` + 트랩 프레임). 덤프의 CPU 문맥은 디버거 루프라 쓸 수 없다.
- **`g` 는 `Done with snapshot` 확인 뒤** (S-루틴 4). lazy 를 껐어도 확인은 매번 한다.
- **엉뚱한 VM**: 스냅샷은 `bob_baseline_vm` 만. 공유 폴더 설정은 `Windows 11 x64` 만. `WIN11-26H1-HDAUDIO` 는 아무것도 안 한다. `vmrun` 은 `list` 만.
- **두 WinDbg 창**: 라이브(커널 세션)와 오프라인(덤프)을 섞지 않는다.
- 같은 지점에서 **두 번** 서면 (K1~K3, `[추측]` 으로 한 번씩만 불릴 것): 두 번째는 기록만 하고 스냅샷은 안 찍는다. `bc` 는 첫 번째 뒤에 이미 했으니 보통 생기지 않는다.
