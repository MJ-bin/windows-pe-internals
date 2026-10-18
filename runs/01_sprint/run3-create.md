# 2026-10-17 프로세스 생성 단계 (커널)

## 환경

- 타깃 VM: Windows 11 24H2, 26100.9457 (Secure Boot 꺼짐, BitLocker 꺼짐, 자동 업데이트 꺼짐)
  - ntkrnlmp.pdb: `C29EBFB06B78B3C020DCA66D99713F9E1`
  - IP 192.168.100.133, NIC e1000e (Intel 82574L)
- 디버거 VM: WinDbg, KDNET (192.168.100.132, port 50000)
- 실행: 타깃의 cmd.exe(Cid `1d94`, EPROCESS `ffff908f8acd4080`)에서 `test.exe` 실행

## 목적

cmd.exe가 test.exe를 만들 때, 커널 안에서 대표 함수들이 **실제로 불리는지**, **어떤 순서인지** 확인.

## 방법

- 모든 멈춤점에 `/p <cmd의 EPROCESS>` 필터
- 멈추지 않고 기록만: `".echo ==== 이름; ? @$thread; k 6; g"`
- 함정: 옛날 방식 `nt!PspCreateProcess`도 같이 걸어둠
- 로그: `run3-create.log`

## 결과

| 순번 | 커널/유저 | 스레드 | 함수 | 부른 곳 | 표시 |
|---|---|---|---|---|---|
| 1 | 커널 | `ffff908f8e0c0080` | `nt!NtCreateUserProcess` | `nt!KiSystemServiceCopyEnd+0x28` (시스템 콜 진입) | [26100.9457 확인] |
| 2 | 커널 | 〃 | `nt!PspAllocateProcess` | `NtCreateUserProcess+0xc36` | [26100.9457 확인] |
| 3 | 커널 | 〃 | `nt!MmCreatePeb` | `PspAllocateProcess+0x2314` | [26100.9457 확인] |
| 4 | 커널 | 〃 | `nt!PspAllocateThread` | `NtCreateUserProcess+0xe5c` | [26100.9457 확인] |
| 5 | 커널 | 〃 | `nt!PspInsertProcess` | `NtCreateUserProcess+0xfc2` | [26100.9457 확인] |
| 6 | 커널 | 〃 | `nt!PspInsertThread` | `NtCreateUserProcess+0x101a` | [26100.9457 확인] |
| 7 | 커널 | 〃 | `nt!PspCallProcessNotifyRoutines` | `PspInsertThread+0x807` | [26100.9457 확인] |
| - | 커널 | - | `nt!PspCreateProcess` | **안 불림** | [26100.9457 확인] |

### 호출 관계

```
NtCreateUserProcess
 ├─ +0xc36   PspAllocateProcess
 │            └─ +0x2314  MmCreatePeb
 ├─ +0xe5c   PspAllocateThread
 ├─ +0xfc2   PspInsertProcess
 └─ +0x101a  PspInsertThread
              └─ +0x807   PspCallProcessNotifyRoutines
```

## 확인된 것

- 유저 쪽 `CreateProcess`는 `NtCreateUserProcess` 시스템 콜로 커널에 들어온다.
- 생성 단계 전부를 **부모(cmd.exe)의 같은 스레드 하나**가 처리한다. 다른 스레드로 넘기지 않는다.
- 옛날 길 `PspCreateProcess`는 타지 않는다.
- 스레드를 먼저 만들고(4), 그다음 프로세스를 등록한다(5).
- 프로세스 생성 알림(`PspCallProcessNotifyRoutines`)은 **첫 스레드 등록(`PspInsertThread`) 안에서** 나간다.
- 이 단계가 끝날 때까지 test.exe 코드는 아직 실행되지 않았다.

## 아직 확인 필요

각 함수의 역할은 일반적으로 알려진 설명이고, 이번 로그로 직접 본 건 아님. [확인 필요]

- `PspAllocateProcess`: 프로세스 정보 묶음(`_EPROCESS`)과 메모리 공간 준비
- `MmCreatePeb`: 유저 쪽(ntdll)이 읽을 PEB 만들기
- `PspAllocateThread`: 첫 스레드 만들기
- `PspInsertProcess` / `PspInsertThread`: 시스템 목록에 등록
- `PspCallProcessNotifyRoutines`: 등록된 드라이버(백신·EDR 등)에 "새 프로세스" 알림

## 이번에 확인한 구조체 오프셋

- `_EPROCESS.UniqueProcessId` = `+0x1d0` [26100.9457 확인]
- `_EPROCESS.ImageFileName` = `+0x338`, 15바이트 [26100.9457 확인]

## 시행착오 (run1, run2)

- **run1: 중간 함수가 하나도 안 찍힘**
  - 원인(유력): "끝" 표시용으로 `@$ra`(=`nt!KiSystemServiceCopyEnd+0x28`)에 멈춤점을 걸었는데,
    이 주소는 **모든 시스템 콜이 같이 쓰는 출구**. 다른 스레드가 먼저 밟아서 기록이 일찍 끝남.
  - 교훈: 출구 표시용 멈춤점을 공용 주소에 걸지 않는다. 끝은 손으로 Break.
  - `/t @$thread` 필터가 제대로 걸렀는지는 확인 못 함. `/p`가 잘 되므로 `/t`는 안 쓴다.
- **run2: 필터 없는 멈춤점은 백그라운드 때문에 계속 멈춤**
  - svchost(Cid `0580`), MicrosoftEdgeUpdate 등이 수시로 프로세스를 만든다.
  - 해결: 멈추지 말고 `.echo ...; g`로 기록만.
  - 다른 cmd.exe(Cid `2360`, 부모 svchost)도 있었다 → 이름 대신 **EPROCESS 주소로 거는 `/p`** 가 정확하다.
- WinDbg 명령에 한글을 넣으면 깨진다. 영어만 쓴다.

## 디버거 때문에 보인 것

- 부팅 때 `DriverEntry failed 0xc00000bb` (uiomap, NetworkPrivacyPolicy): 원래 조용히 일어나던 일이 디버거로 보인 것. 흐름과 무관.

## 다음

- 첫 스레드 출근: `nt!PspUserThreadStartup` (test.exe 쪽 스레드)
- 유저 쪽 시작: `ntdll!LdrInitializeThunk` [확인 필요]