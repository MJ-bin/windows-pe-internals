# 68 image-base-select — 정적 분석 요청

- 날짜: 2026-11-07
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run73
- 대기열: **Q139a** (PE_emulator 요청 23 의 3번을 정적으로 닫기. 사용자 결정 2026-11-07: "요청 23-3 은 a 로 하자" — Q104 VM 단계 앞)
- 관련: run72 (sast/67 결과서 3-2·3-3 — `MiMapViewOfImageSection` 의 `0x40000003` 조건, `MiSelectImageBase +0xd6/+0xd9` 넷째 인자 비트 0, "`MiCreateNewSection` 은 안 읽었다 → 3번이 안 닫힌 가장 큰 이유"), run31 (`MiCreateNewSection`·`MiCreateImageFileMap`·`MiBuildImageControlArea` — 섹션 만들기), run38 (`LdrpMinimalMapModule +0x174 NtMapViewOfSection`, `0x40000003` → 재배치·재시도)
- 원문 (읽기만): `../PE_emulator/_workspace/00_planner/121_pool-notes.md` 0절·R1

## 무엇을 위해

- PE_emulator 요청 23 (3): dll1·dll2 이미지 맵 결과가 `STATUS_SUCCESS` (0) 인가 `STATUS_IMAGE_NOT_AT_BASE` (`0x40000003`) 인가. `0x40000003` 이면 병렬 로딩에서 재시도 길 (run72) 이 열린다.
- sample `dll1.dll`·`dll2.dll` 은 둘 다 DLL, `DYNAMIC_BASE`, 재배치 있음, **헤더 `ImageBase` 가 둘 다 `0x180000000`** (run72 결과서).
- run72 결과서: `MiSelectImageBase` 넷째 인자 비트 0 (= `MiRelocateImage` 여섯째 인자 `& 1`) 이 **켜지면 헤더 `ImageBase` 그대로** (→ 둘째 DLL 은 반드시 충돌 → `0x40000003`), **꺼지면 DLL 은 전역 비트맵에서 파일마다 다른 칸** (→ 기대 `0`). 이 비트를 정하는 `MiCreateNewSection` 을 안 읽어 닫히지 않았다.

## 알고 싶은 것

1. **`MiCreateNewSection` (ntoskrnl rva `0x96f4d0`, 1842B, `.asm` 있음) 이 `MiRelocateImage` 에 주는 여섯째 인자**
   - 부르는 자리 둘: `+0x3d5/+0x3da`, `+0x695/+0x69a`. 두 자리 모두 여섯째 인자 (`[RSP+0x28]`) 에 무엇이 들어가나, 그 값의 **출처** (어느 레지스터·어느 입력 칸에서, 어떤 셈으로)
   - 오케스트레이터가 요청서 쓰며 본 줄: `+0x38b SHR ESI,0x1b` · `+0x391 AND ESI,0x2` · `+0x3cc MOV dword [RSP+0x28],ESI` · `+0x68c MOV dword [RSP+0x28],ESI`. **`+0x391` 뒤 `+0x3cc`·`+0x68c` 까지 ESI 를 바꾸는 줄이 있나** (두 자리에 닿는 모든 흐름 — 점프 목적지를 "목적지 − 함수 시작" 으로 세어 따라갈 것). ESI 의 처음 값 (`+0x38b` 앞) 은 어느 입력에서 오나 (인자·섹션 플래그 — 이름은 PDB 로)
   - 결론: 여섯째 인자의 비트 0 이 **늘 0 인가** (예/아니오 + 근거 줄). 0 이 아닐 수 있다면 그 조건
2. **`MiRelocateImage` (rva `0x973444`, 2093B, `.asm` 있음) 가 여섯째 인자를 어떻게 쓰나** — run72 결과서는 `.c` 기준 `uVar12 = (param_6 & 1)*0x100 + 2` 라고 함. `.asm` 줄로 다시 확인: 여섯째 인자를 읽는 줄 (`[RSP+0x..]` 또는 받은 레지스터), `+0x2e6/+0x2eb MiSelectImageBase` 에 넷째 인자로 넘기는 값과 그 비트 0 의 관계
3. **`MiRelocateImage`·`MiSelectImageBase` 를 부르는 다른 곳** — `_calls.tsv` 로 전부. 이미지 섹션을 만드는 길 (`NtCreateSection` → `MiCreateSection` → … → `MiCreateNewSection`) 말고 비트 0 을 켜서 부르는 곳이 있나 (있으면 이름·조건만)
4. **`MiSelectImageBase` 비트 0 꺼짐 갈래** (run72 결과서 3-2: DLL 이면 `+0x114 JNZ` 를 안 타고 비트맵 `RtlFindClearBitsEx`·`MiObtainRelocationBits`) — 이 칸이 **파일 (섹션) 마다 다른가**, 같은 파일을 다시 열면 같은 칸을 쓰나 (섹션 공유 — run31), 그 결과 기준 주소가 `_SEGMENT.BasedAddress` 에 들어가는 줄 (Q139 (b) 일부 — 줄 하나면)
5. **결론 (PE_emulator 한 줄)**: test.exe 에서 dll1·dll2 맵 결과의 기대값. 비트 0 이 늘 0 이면 "커널이 두 DLL 에 서로 다른 기준 주소를 줌 → 그 주소 범위가 프로세스에서 비어 있으면 `0`". 남는 실행 때 조건 (범위가 이미 차 있는 경우 — `MiMapViewOfImageSection` 이 다른 곳에 붙이면 `0x40000003`) 은 "동적 필요" 로 갈라 적기. run44 사진 P1b `DllBase = OriginalBase` (dll1 `0x7ffae12e0000`, dll2 `0x7ffadb950000` `[관찰]`) 와 맞나

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^MiCreateNewSection$`, `^MiRelocateImage$`, `^MiSelectImageBase$` | 0 | 1·2·4 (`.asm` 있음 — 다시 뽑지 않아도 됨) |
| ntoskrnl | `_calls.tsv` 에서 `MiRelocateImage`·`MiSelectImageBase` 부르는 곳 | — | 3 |
| ntoskrnl | `MiObtainRelocationBits` (4 에 필요하면) | 0 | 4 |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where ntoskrnl.exe <이름>`.
- Ghidra 를 열어야 하면 먼저 `pgrep -fa 'ghidra.app.util.headless'` 로 확인 (같은 서버에 다른 사용자 Ghidra 도 돎 — 우리 `ms/ghidra` 와는 무관).

## 이미 아는 것

- `[정적]` run72 결과서 3-2·3-3 (위 "무엇을 위해")
- `[관찰]` run44 사진 P1b: dll1 `DllBase` = `OriginalBase` = `0x7ffae12e0000`, dll2 = `0x7ffadb950000` (`ms/logs/04_baggage-read_P1b.txt` 3802·3853·3907·3958줄 — run72 문서)
- 오케스트레이터가 본 `MiCreateNewSection` 줄 (질문 1) — 아직 흐름을 다 따라가지 않은 짐작이니 확인해 줄 것

## 이번에 안 하는 것

- Q139 (b)~(f) 나머지 (`EPROCESS.Flags3` 비트 13 뜻, `TpWaitForWork`, `NtReleaseWorkerFactoryWorker`, `ctx+0xb0` 훑기, 덤프 오프라인)
- `MiMapViewOfImageSection` 다시 읽기 (run72 결과서 그대로)

## 받고 싶은 것

- 질문별 답 (예/아니오·이름) + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다, 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋. 결과서 쓰기 전에 `.asm` 줄과 다시 대조
- 5번 한 줄 결론 (PE_emulator 가 바로 쓸 수 있게)
- 질문마다 **"정적으로 닫힘 / 동적 필요"**. 남는 동적 몫은 Q104 VM 단계 (`ntdll!LdrpMinimalMapModule+0x179` `eax`) 로
- run72 문서·flow·baggage 에 고칠 줄이 있으면 목록
