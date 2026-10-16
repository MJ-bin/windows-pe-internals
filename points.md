# 대표 지점 (초안)

범위: 프로세스 생성 → `main` 시작. 단계 번호는 PELdr `docs/PE_execution_process.md`를 따른다.

- `[26100.9457 확인]` = PELdr에서 이 빌드로 확인한 이름
- `[확인 필요]` = 이 빌드에 진짜 있는지 VM에서 먼저 봐야 하는 이름

## 커널

| 단계 | 하는 일 | 지점 | 상태 |
|---|---|---|---|
| 2 | 커널에 "프로세스 만들어 줘" | `nt!NtCreateUserProcess` | [확인 필요] |
| 3 | exe 헤더 읽기 | `nt!MiCreateImageFileMap` | [확인 필요] |
| 4 | 메모리 공간 만들고 exe·ntdll 올리기 | `nt!MmInitializeProcessAddressSpace`, `nt!PspMapSystemDll` | [확인 필요] |
| 5 | EPROCESS·PEB 만들기 | `nt!PspAllocateProcess`, `nt!MmCreatePeb` | [확인 필요] |
| 5 | 첫 스레드·TEB 만들기 | `nt!PspAllocateThread`, `nt!MmCreateTeb` | [확인 필요] |
| 5 | 첫 스레드가 유저로 넘어갈 준비 | `nt!PspUserThreadStartup` | [확인 필요] |

## 유저 (test.exe 안)

| 단계 | 하는 일 | 지점 | 상태 |
|---|---|---|---|
| 6 | 처음 깨어남 | `ntdll!LdrInitializeThunk` | [26100.9457 확인] |
| 6 | 로더 본체 시작 | `ntdll!LdrpInitializeProcess` | [26100.9457 확인] |
| 6-0 | `PEB.Ldr` 채우기 | 값 감시: PEB+0x18 | [26100.9457 확인] |
| 6-1 | 기본 힙 만들기 | `ntdll!LdrpInitializeProcessHeap` | [26100.9457 확인] |
| 6-2 | DLL마다 IAT 채우기 | `ntdll!LdrpSnapModule` | [26100.9457 확인] |
| 6-3 | DLL 초기화 (TLS·DllMain) | `ntdll!LdrpCallInitRoutine` | [26100.9457 확인] |
| 6-5 | 초기화 끝, 진짜 시작점으로 점프 | `ntdll!NtContinue` | [26100.9457 확인] |
| 7~9 | exe 시작 → main | `ntdll!RtlUserThreadStart` → `test!main` | [26100.9457 확인] |

## 다음

- 디버거 VM에서 `[확인 필요]` 이름부터 확인 (`x nt!Psp*`, `x nt!Mm*Peb*`, `x nt!Mi*ImageFile*`).
- 이름이 다르면 이 표를 고친다.
