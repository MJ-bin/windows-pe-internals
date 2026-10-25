# 04 baggage-read — 실행 안내 (오프라인 덤프 읽기, Q77c)

> 로그: 서버 `ms/logs/04_baggage-read_<P>.txt` 8개 (지금 빈 파일). **명령 파일이 직접 쓴다** — 사용자가 `.logopen` 을 치거나 붙여넣을 필요가 없다 (공유 폴더 `logs`).
> 명령 파일: 서버 `ms/logs/04_baggage-read_cmds.txt` = 디버거 VM `\\vmware-host\Shared Folders\logs\04_baggage-read_cmds.txt`
> 덤프: 서버 `ms/dump/03_baggage-snap/<P>/memory.dmp` = 디버거 VM `\\vmware-host\Shared Folders\dump\03_baggage-snap\<P>\memory.dmp`
> 재부팅: 없음 · 예상 시간: 덤프 하나에 열기 1~2분 + 명령 파일 수 분 (`.data` 등 raw 가 길다) → K1 시험 + 확인 약 15분, 나머지 7개 약 40~60분
> 이번에 보는 것: dast/03 의 덤프 8개에서 sast/38 표의 대상(구획 1~17)을 **같은 명령 묶음**으로 읽어 지점별 로그로 남긴다. 지점끼리 비교는 run44 (flow-writer).

**쓰는 VM**: 디버거 VM `Windows 11 x64` 하나뿐이다. 타깃 `bob_baseline_vm` 은 **꺼도 된다**. `WIN11-26H1-HDAUDIO` 는 건드리지 않는다.

**누가 치나**: `[사용자]` = 디버거 VM WinDbg · `[서버 — 오케스트레이터가 실행 가능]` = 서버 터미널.

**`<P>` 순서**: `K1` (시험) → `K2` `K3` `P0` `P1` `P1a` `P1b` `P2`. **이 여덟 글자 중 하나를 그대로** 친다 (꺾쇠 없이, 대소문자 그대로).

---

## 명령 파일이 하는 일 (읽기만, 고칠 땐 서버에서)

사용자가 덤프를 열고 아래 **한 줄**만 치면, 명령 파일이 나머지를 한다:
```
$$>a< "\\vmware-host\Shared Folders\logs\04_baggage-read_cmds.txt" K1
```

- 인자 `K1` 이 로그 파일 이름(`04_baggage-read_K1.txt`)과 구획 머리(`==== K1 …`)가 된다.
- 하는 일:
  1. 스스로 `.logopen` 한다.
  2. 구획마다 `.echo "==== <P> NN 이름"` 머리를 붙인다.
  3. `.catch { }` 로 감싸 읽는다. 한 구획이 실패해도 다음 구획으로 넘어간다.
  4. 끝에 `==== <P> 99 END` 를 찍고 `.logclose` 한다.

| 구획 | 대상 | 명령 꼴 | 주소 (dast/03 라이브 값, 덤프마다 같음) |
|---|---|---|---|
| 00 | 머리: `vertarget`, `!lmi nt`, `.process /p /r`, `!process <EPROC> 0`, `!process 0 0 test.exe`, `.reload /f ntdll.dll=…`, `lm m ntdll` | — | EPROCESS `ffff82859908b080`, ntdll `7ffafb660000` 크기 `0x267000` |
| 01 / 01b / 01c | `_EPROCESS` / `_KPROCESS` / raw | `dt`, `dq L0x108` | `ffff82859908b080` |
| 02 / 02b / 02c | `_ETHREAD` / `_KTHREAD` / raw | `dt`, `dq L0xf3` | `ffff828598445080` (K1 엔 아직 없음) |
| 03 / 03b | `_KTRAP_FRAME` 첫 스레드 / cmd 스레드 | `dt`, `dq L0x32` | `ffffb58b9ad07460` / `ffffb58b99117460` (각 InitialStack−0x190) |
| 04 / 04b | `_HANDLE_TABLE` / 핸들 목록 | `dt`·`dq` / `!handle 0 3 <EPROC>` | `EPROCESS+0x300` |
| 05 / 05b | `_TOKEN` / `!token` | `dt`·`dq L0xb9` | `EPROCESS+0x248` 아래 4비트 지움 |
| 06 / 06b | VAD 나무 머리 / `!vad` 목록 | `dt nt!_RTL_AVL_TREE`, `!vad` | `EPROCESS+0x558` |
| 07 ~ 07d | `_SECTION` → `_CONTROL_AREA` → `_SEGMENT` → `_MI_SECTION_IMAGE_INFORMATION`(+`_SECTION_IMAGE_INFORMATION`) | `dt`·`dq` | `EPROCESS+0x2a8` → `+0x28`(아래 2비트 지움) → `+0x0` → `+0x38` |
| 08 | `_FILE_OBJECT` | `dt`·`dq L0x1b` | `EPROCESS+0x330` |
| 09 | `nt!PsNtdllExports` | `dt nt!_PS_NTDLL_EXPORTS`·`dq L0x15` | 전역 |
| 10 / 10b | `_PEB` / `_TEB` | `dt`·`dq L0xfb` / `L0x30f` | `9b2fa66000` / `9b2fa67000` |
| 11 / 11b | `_RTL_USER_PROCESS_PARAMETERS` / 환경 블록 앞 0x200 + 크기 | `dt`·`dq L0x8a` / `db` | **그 덤프의** `PEB+0x20` (① `25996cb0000`, ② `25996f37a50`) |
| 12 / 12b | `_PEB_LDR_DATA` / 모듈 항목 + `DdagNode` | `dt`·`dq`, `!list … -x "dt _LDR_DATA_TABLE_ENTRY; dt _LDR_DDAG_NODE"` | `PEB+0x18` (null 이면 건너뜀) |
| 13 | `_KUSER_SHARED_DATA` | `dt`·`dq L0x150` | `0x7ffe0000` |
| 14 | 맡긴 `_CONTEXT` | `dt`·`dq L0x9a` | `9b2fcffa40` |
| 15 | API set 지도 머리 | `dd L0x10`·`dq L8` | `PEB+0x68` |
| 16 | PEB·TEB 가 가리키는 영역 | 포인터 값 + `dq` 몇 줄 | PEB `+0x30 +0x78 +0x90 +0x2f8 +0x308 +0x7b8`, PEB 안 `+0x3a0`(0x400), TEB `+0x8 +0x10 +0x1478 +0x58 +0x2c8 +0x1850` |
| 17a / b / c | ntdll `.data` / `.mrdata` / `.00cfg` raw | `dq L0x11eb` / `L0x6b7` / `L5` | `7ffafb82c000` / `7ffafb847000` / `7ffafb846000` |
| 99 | 끝 표시 | — | — |

정한 것 (요청서가 dast-mentor 에게 맡긴 것):

- **VAD 는 `!vad` 목록만** 남긴다 (노드마다 `dt` 는 안 함). 노드가 수십 개라 출력이 몇 배가 되고, 목록에 이미 범위·종류·파일이 나온다. 칸까지 필요한 노드가 생기면 그 노드만 따로 읽는다.
- **타입은 모두 `nt!`** 로 쓴다 (`_PEB`·`_TEB`·`_LDR_*`·`_RTL_USER_PROCESS_PARAMETERS` 도). 커널 PDB 에 들어 있는 타입이라, ntdll 심볼이 안 올라와도 읽힌다 `[추측: 공개 지식, dast/01~03 에서 nt!_PEB·nt!_CONTEXT 는 됐음]`. 안 되는 타입은 K1 시험에서 드러난다.
- **명령 파일 자리 = `ms/logs/`**: 디버거 VM 이 이미 읽고 쓸 수 있는 공유 폴더라 새 설정이 필요 없다. 이름에 `_cmds` 를 붙여 로그와 구분한다.
- 계산한 주소를 받아야 하는 확장 명령(`!token`·`!vad`·`!list`)은 `as /x` 별칭 + `.block` 으로 숫자를 넘긴다. dast/01 에서 `!vtop` 이 `@$peb` 꼴을 못 받았기 때문이다.
- 모듈 목록 `!list` 는 첫 항목에서 출발해 한 바퀴 돈다. 그래서 목록 머리(`PebLdr+0x10`)가 "항목" 으로 한 번 찍힌다 — 그 한 덩어리는 쓰레기 값이 정상이다.

---

## 0. 준비

1. `[사용자]` 타깃 `bob_baseline_vm` 은 꺼도 된다 (Shut down 또는 Power off). **스냅샷은 지우지 않는다.**
2. `[사용자]` 디버거 VM 의 **커널 세션 WinDbg 는 닫는다** (P32 제안: 창을 헷갈리지 않게). WinDbg 를 **새로 하나만** 켠다.
3. `[서버 — 오케스트레이터가 실행 가능]` 파일이 다 있는지:
```
cd /home/mjbin/prj/bob/teamprj/windows-pe-internals
ls -l ms/dump/03_baggage-snap/*/memory.dmp
ls -l ms/logs/04_baggage-read_cmds.txt ms/logs/04_baggage-read_*.txt
```
- 정상이면: `memory.dmp` 8개 (각 8,589,942,784 B), 명령 파일 1개, 빈 로그 8개 (0 B).

## 1. K1 로 시험

1. `[사용자]` WinDbg → File → **Open dump file** → 아래 경로를 붙여넣고 연다:
```
\\vmware-host\Shared Folders\dump\03_baggage-snap\K1\memory.dmp
```
2. 로딩이 끝나 `0: kd>` 프롬프트가 나오면 **이 한 줄**:
```
$$>a< "\\vmware-host\Shared Folders\logs\04_baggage-read_cmds.txt" K1
```
3. 기다린다. 화면에 `==== K1 …` 머리가 차례로 지나가고, 마지막에 `==== K1 99 END` 와 `Closing open log file` 이 나오면 끝.
   - 중간에 `Unable to get program counter`, `PEB is paged out`, `Memory read error`, `????` 가 보여도 정상이다 (덤프 CPU 문맥은 디버거 루프 — dast/02. K1 엔 TEB 등이 아직 없음).
   - 몇 분 동안 아무 출력 없이 `*BUSY*` 면 `.data` raw (구획 17a) 를 찍는 중일 수 있다. 기다린다.
4. `[사용자]` File → **Stop debugging** (덤프 닫기).

- 이상하면:
  - 한 줄을 치자마자 에러 한 줄로 끝남 (`Couldn't resolve`, `Syntax error`, 파일 못 찾음) → 그 화면을 오케스트레이터에게 알린다. 2단계(확인)에서 고친다.
  - `==== K1 99 END` 가 안 나오고 중간에서 멈춤 → 마지막 머리가 무엇인지 알린다.

## 2. K1 로그 확인 `[서버 — 오케스트레이터가 실행 가능]`
```
F=/home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/logs/04_baggage-read_K1.txt
ls -l "$F"; wc -l "$F"
grep -c '^==== K1 ' "$F"
grep -n '^==== K1 ' "$F"
grep -nE "Couldn't resolve|Syntax error|not found|No type|usage:|Invalid|Unable to (read|get)|Memory (read|access) error" "$F" | head -n 80
```
- **정상**:
  - 머리가 `00` 부터 `99 END` 까지 다 있다 (구획 07 사슬이 끝까지 가면 머리 35개, 사슬이 중간에 끊기면 32~34개).
  - 구획 01 에 `UniqueProcessId : 0x00000000'00001dd0`, `ImageFileName : "test.exe"`, `Peb : 0x0000009b'2fa66000` 이 있다.
  - 구획 10 에 `ImageBaseAddress : 0x00007ff6'1a370000`, `Ldr : (null)` 이 있다.
  - 구획 00 의 `!process 0 0 test.exe` 는 빈칸 (dast/03 K1 과 같음). 반면 `!process ffff82859908b080 0` 은 test.exe 를 낸다.
- **K1 에서 실패해도 괜찮은 것** (그 자체가 결과): 02·03 (스레드가 아직 없음 — 쓰레기 값이거나 읽기 실패), 10b·16 의 TEB 쪽, 11b 환경 블록, 12·12b (Ldr null), 14 (`_CONTEXT` 는 ① 이후), 17 의 일부 `????` (아직 안 건드린 페이지).
- **고쳐야 하는 것** — 명령 파일을 서버에서 고치고 K1 을 **다시** 돌린다 (로그는 덮어써진다):

  | 보이는 것 | 고칠 곳 |
  |---|---|
  | `nt!_<타입>` 에 `not found`/`No type` | 그 줄의 `nt!` → `ntdll!` (구획 00 의 `.reload /f ntdll.dll` 이 됐을 때만 먹음) |
  | `!token`/`!vad`/`!list` 에 `usage:`/`Invalid` | `as /x` + `.block` 줄. 별칭이 안 풀렸으면 그 확장 명령 줄을 빼고 `dt` 결과만 남김 |
  | `.reload /f ntdll.dll=…` 실패 | 그대로 둔다 (타입은 `nt!` 라 영향 적음) |
  | 머리가 `00` 하나뿐이거나 로그가 비었음 | → 3단계 (예비 방법) |

## 3. (예비) 한 줄 실행이 통째로 안 될 때만

`$$>a<` 는 파일 전체를 **한 줄로 이어 붙여** 돈다. 그래서 너무 길거나 따옴표가 어긋나면 처음부터 안 돌 수 있다 `[추측]`. 그럴 땐 줄마다 도는 `$$<` 로 바꾼다. 인자를 못 받으니 지점 이름을 박은 파일을 서버에서 만든다.

1. `[서버 — 오케스트레이터가 실행 가능]`
```
cd /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/logs
for P in K1 K2 K3 P0 P1 P1a P1b P2; do sed "s/\${\$arg1}/$P/g" 04_baggage-read_cmds.txt > 04_baggage-read_cmds_$P.txt; done
grep -c 'K1' 04_baggage-read_cmds_K1.txt
```
2. `[사용자]` 덤프를 연 뒤 (K1 이면):
```
$$< "\\vmware-host\Shared Folders\logs\04_baggage-read_cmds_K1.txt"
```
3. 2단계와 같이 확인한다. 나머지 지점도 파일 이름의 `K1` 만 바꿔서.

## 4. 나머지 7개

K1 이 정상이면, `<P>` 를 `K2` → `K3` → `P0` → `P1` → `P1a` → `P1b` → `P2` 순서로 바꿔 가며 1단계의 1~4를 되풀이한다. 덤프 경로와 한 줄의 **지점 이름 두 곳**을 같이 바꾼다:
```
\\vmware-host\Shared Folders\dump\03_baggage-snap\K2\memory.dmp
```
```
$$>a< "\\vmware-host\Shared Folders\logs\04_baggage-read_cmds.txt" K2
```
- 덤프 하나 끝나면 **Stop debugging** 을 하고 다음 덤프를 연다 (덤프 두 개를 동시에 열지 않는다).
- `[서버 — 오케스트레이터가 실행 가능]` 지점마다 2단계 명령의 `K1` 을 그 지점으로 바꿔 확인해도 되고, 다 끝난 뒤 한꺼번에 봐도 된다.
- 지점별로 특히 볼 것:
  - **K2**: 구획 00 의 `!process ffff82859908b080 0` 이 test.exe 를 낸다 (목록 `!process 0 0 test.exe` 는 빈칸일 것 — dast/03 의 빈틈 확인).
  - **K3~P2**: 구획 01 `UniqueProcessId 0x1dd0`, 구획 10 `ImageBaseAddress 7ff61a370000`.
  - **P1**: 구획 10 `Ldr (null)`, `ProcessParameters 0x00000259'96cb0000`. 구획 14 `ContextFlags 0x10001b`.
  - **P2**: 구획 10 `Ldr 0x00007ffa'fb832880`, `ProcessHeap 0x00000259'96f30000`, `ProcessParameters 0x00000259'96f37a50`. 구획 12b 에 test·ntdll·kernel32·kernelbase·dll1·dll2 항목.

## 5. 마무리 `[서버 — 오케스트레이터가 실행 가능]`
```
cd /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/logs
for P in K1 K2 K3 P0 P1 P1a P1b P2; do printf "%s " $P; wc -l < 04_baggage-read_$P.txt | tr -d '\n'; printf " heads="; grep -c "^==== $P " 04_baggage-read_$P.txt; done
for P in K1 K2 K3 P0 P1 P1a P1b P2; do grep -q "^==== $P 99 END" 04_baggage-read_$P.txt && echo "$P END ok" || echo "$P END missing"; done
```
- 정상이면: 8개 모두 `END ok`.
- 그다음 오케스트레이터가 "로그 다 됐어" 로 판정을 맡긴다.
- `[사용자]` 디버거 VM WinDbg 는 닫아도 된다. 스냅샷·덤프는 이 바퀴(run44)가 끝날 때까지 지우지 않는다.

---

## 자주 하는 실수 (이번 run 관련)

- **지점 이름 두 곳**: 덤프 경로의 `\K2\` 와 한 줄 끝의 `K2` 가 같아야 한다. 다르면 K2 덤프 내용이 다른 이름의 로그에 써진다 (로그 머리 00 의 `!process`·HandleCount 로 나중에 가릴 수는 있다: K2·K3·P0·P1·P1a·P1b·P2 의 HandleCount = 4·4·4·4·26·44·46 — dast/03).
- **꺾쇠 금지**: `<K1>` 이 아니라 `K1` (dast/03).
- **두 WinDbg 창** (P32 제안): 커널 세션 창은 닫고 시작한다. 덤프도 한 번에 하나만.
- **덤프의 `r`·`k` 는 쓰지 않는다** (P28 제안): CPU 문맥이 디버거 루프다. 레지스터는 구획 03 (트랩 프레임)과 dast/03 라이브 로그에서 본다. 유저 지점의 트랩 프레임 `Rip` 는 멈춘 주소 +1 이다 (P31 제안).
- **`KTHREAD.TrapFrame` 은 ⓪·① 에서 null** 이다. 구획 03 은 그래서 `InitialStack−0x190` 고정 주소로 읽는다.
- **명령 파일을 VM 안에서 고치지 않는다**: 서버 파일이 원본이다. 고칠 땐 오케스트레이터가 서버에서 고치고 그 지점을 다시 돌린다.
- **로그를 다시 돌리면 덮어써진다** (`.logopen`). 다시 돌릴 땐 그 지점 하나만.
- **`WIN11-26H1-HDAUDIO`**: 아무것도 하지 않는다. 타깃 스냅샷(7~14)도 지우지 않는다.
