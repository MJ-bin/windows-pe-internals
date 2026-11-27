# run151 rpcss-connect-sd — 해설

> 이 글은 [run151 문서](run151-rpcss-connect-sd.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q288** + **Q284a** (`_workspace/plan.md`, 둘 다 이번에 **끝 (run151)**)
  - **Q288** — PE_emulator 요청 380 의 **② 절** "rpcss `_Connect` 응답의 SD 다섯 조각".
    - 원 요청 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` ②, 튜닝 `374_tune19.md` OR1 — 받은 581 · 탓 18 (요청 쪽 에뮬레이터 숫자).
  - **Q284a** — run149 가 남긴 정적 Q284 의 ① ~ ③ 만 떼어 온 것.
    - 프로시저 7 · 6 몸 · `LocalInterfaceOnlySecCallback` · `MakeSCMProxy` / `DelegateActivationToSCM`.
    - 남은 ④ ~ ⑥ 은 **Q284b** (대기).
  - 사용자 결정 2026-11-27 (PE_emulator D179) — 380 묶음 정적 차례 ②, **정적만**.
  - 결과서 판정 **분석성공**.
    - 물음 2 · 1 · 3 · 5 는 줄 근거로 닫혔다.
    - 4 · 6 은 몸 · 입출력 칸 · 실패 status 까지. 서버 쪽 진짜 값은 `[공백]`.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q283** 에 "읽기만" 얹기 (D5 · D8, 새 번호 없음)
    - 디버거 없이 VM 에서 떠 올 것 → 380 묶음 **2 단계 VM 수집 스크립트** 재료
    - 정적 → **Q294** (run151 남은 정적) · **Q284b**
- 근거:
  - run 문서 [`run151-rpcss-connect-sd.md`](run151-rpcss-connect-sd.md) (가장 중요)
  - 요청서 [`_workspace/sast/148_rpcss-connect-sd.md`](../../_workspace/sast/148_rpcss-connect-sd.md) · 결과서 [`148_rpcss-connect-sd_result.md`](../../_workspace/sast/148_rpcss-connect-sd_result.md)
    - **sechost 는 처음 analyze** 했다 (약 2분 24초, `MAXMEM=8G`).
    - rpcss · combase · ntdll 은 있던 프로젝트에 export 만 더 했다. advapi32 · rpcrt4 는 안 열었다.
    - 스크립트 문제 없음.
  - flow.md 변경: docs 커밋 `9b1a75e` (`git show 9b1a75e -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
  - 같은 커밋이 run149 문서 · 해설서 **맨 위에 "고쳐 읽기" 두 줄씩** 넣었다 (2.12).
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 안 탄다. 그리고 SD 를 짓는 쪽은 다른 가게다.**
   - 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) 에 combase · sechost 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run149 · run150 과 같은 줄).
   - `sample/*.c` 에 COM API 가 없다 `[소스]` (`sample/log.h:3`).
   - RPCSS 는 **다른 프로세스** (`svchost -k rpcss` 의 `rpcss.dll`, run149) 다.
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - BG05 · `rBG05_rv1 36a5d5d8e948 seq 1354` · 읽은 주소 `0x1700744cd540` · rip `ntdll +0xe8f3d` · `0xc0000005` · "lsapolicylookup 연결 8 번 `0xc0000034`" 은 모두 PE_emulator 로그다. `[관찰]` 이 아니다.
   - 원문 `[수집]` 레지스트리 크기 (124 · 228 · 232) 도 PE_emulator 쪽이 모은 값이다.
   - 다만 rip 자리 `ntdll +0xe8f3d` (= `0xe8f10 + 0x2d`) 의 **바이트** `0f b6 49 01` 은 이번에 파일에서 읽어 맞췄다 `[정적]` (결과서 · flow-writer 둘 다).
3. **SD 16진 바이트와 크기는 "계산한 값" 이다 — `[추측: 계산]`.**
   - 코드에서 읽은 글 `[정적]` 에 공개 규칙 `[규칙]` 을 얹어 **사람 (스크립트) 이 만든** 바이트다. 메모리에서 본 적이 없다.
   - 왜 그런지 2.5 에서 천천히 푼다.
4. **오프셋 표기가 두 가지 섞여 있었다.**
   - `+0x…` 는 함수 안 줄 (call 자리) 이다. 나무의 숫자는 ret 자리다 (앞 해설과 같은 규칙).
   - 결과서 몇 곳은 점프 목적지를 **주소 끝자리** 로 적었다 (예: `+0xeb8e` = 주소 `0x18007eb8e`). run 문서는 이것을 함수 시작 (`0x18007e880`) 기준으로 고쳐 적었다 (`+0x30e` 등).
   - 이 글은 **run 문서 숫자** 를 쓴다 (2.14).
5. **이번 바퀴는 run149 두 곳을 정정한다.**
   - run149 해설서 맨 위의 "고쳐 읽기 (run151)" 두 줄이 그것이다. 무엇이 왜 바뀌었는지 2.12 에서 따로 모았다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- **run149 에서 푼 것** → [run149 해설](run149-com-rpcss-해설.md)
  - **COM · RPCSS** (COM 담당 건물 · 동네 COM 관리실) → 1절 "새 말 — COM · RPCSS"
  - **`Connect`** (메뉴 0 "저 왔어요") · 메뉴 번호 · 스텁 → 2.7
  - **받는 칸 25** (그 가운데 칸 28 ~ 30 이 SD) → 2.8
  - `RetryRPC` · `CheckStatus` → 2.11
  - **암묵 호출** · 첫 COM 사용 → 2.12
- **SD (출입 명부)** · 소유자 · 그룹 · DACL · SACL · **ACE** · SID → [run97 해설](run97-rpc-sd-query-해설.md) 2.0 — 이번 글의 주인공이라 2.3 에서 한 줄씩 다시 짚는다.
- **self-relative (한 덩어리 꼴)** → [run97 해설](run97-rpc-sd-query-해설.md) 2.5 — 2.5 에서 다시 짚는다.
- IAT (벽 콘센트) → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.7, api-set (별명) → [run23 해설](../02_sprint/run23-load-dependent-해설.md) 2.5
- 114 (epmapper) · `ept_map` → [run145 해설](run145-epm-endpoint-해설.md)
- lsass → [run91 해설](run91-bcrypt-init-해설.md) 2.8, LSA 의 다른 창구 (`lsasspirpc`) → [run135 해설](run135-lsa-sspi-해설.md) 2.3
- 토큰 = **신분증**, SID = 신분증에 적힌 **신분 번호** → [run141 해설](run141-async-bind-해설.md) 2.4

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, combase = 가게에 들인 **COM 담당 건물**, rpcrt4 = **전화 부서**, RPCSS = 동네 **COM 관리실**, 프로시저 번호 = **메뉴 번호**, 컨텍스트 핸들 = **접수 번호표** (run149).
- SD = **출입 명부** (run97). run 문서는 "출입 명단 카드" 라고 불렀다 — 같은 뜻이다.
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 앞 해설 말로 바꾼 것 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - COM 관리실은 `Connect` 답장 끝에 **출입 명부 다섯 장** 을 붙여 보낸다.
  - 넷은 관리실에 **인쇄돼 있는 기본 명부** 다 (코드 상수). 관리실이 문을 열 때 한 번 찍어 둔다.
  - 다섯째는 **건물주가 따로 써 둔 명부** 다 (레지스트리). 없을 수도 있다.
  - COM 담당 건물은 다섯째가 없으면 넷째를 꺼내 **"이거 몇 쪽짜리지?" 하고 길이부터 센다.**
  - 에뮬레이터는 명부 대신 **빈 봉투 (0 B)** 를 보냈다 → 봉투 밖 종이를 명부로 읽다가 넘어졌다.

### 한 줄로 먼저 — 무엇을 얻었나

> **SD 다섯 조각 가운데 넷 (`gpNoReg*SD`) 은 rpcss 가 서비스 시작 때 코드 안 SDDL 글로 짓는다 (레지스트리 안 봄). 크기 124 · 124 · 232 · 228 B. 다섯째만 레지스트리 `MachineAccessRestriction` 이고, 없으면 빠진다.**
> **combase 는 `CoInitializeSecurity +0x385` 에서 다섯째 (없으면 넷째) 를 골라 아무 검사 없이 길이를 잰다 → `ntdll!RtlLengthSecurityDescriptor`. 에뮬레이터의 "크기 0 ×5" 대답이면 넷째가 0 B 덩어리라, 길이 함수가 덩어리 밖을 읽다 `+0x2d` 에서 접근 위반.**
> **lsapolicylookup 은 명시 호출 길에서도 끼지만 실패를 삼키고 지나간다 — 죽음과 무관. 암묵 (`EOAC_APPID`) 길에선 치명.**
> **첫 COM 사용은 메뉴 6 이 먼저다 (run149 의 "7 → 6" 은 차례가 반대).**

- 출처 · 사슬 · lsapolicylookup · 6 먼저 = `[정적]`.
- 바이트 · 크기 = `[추측: 계산]`. "첫 때 그 칸이 0" (6 먼저의 전제) = `[추측]`.

요청서 질문에 대한 답 (run 문서 "요약" · 결과서 "먼저 읽는 한 줄 답" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| 1 SD 다섯의 출처 | 넷 = (가) 코드 상수 SDDL, 다섯째 = (나) 레지스트리, (다) 기계마다 다른 SID 없음 `[정적]` | 2.3 |
| 1 `[수집]` 셋은 어느 조각 | `MachineAccessRestriction` (228) 만 다섯째로 실림. 나머지 둘은 안 실림 `[정적]` | 2.4 |
| 1 SD 꼴 · 바이트 | 자기 상대, 머리 → DACL → 소유자 → 그룹. 바이트는 `[추측: 계산]` | 2.5 |
| 2 죽는 사슬 · 검사 | `+0x385` → `+0x2c` → `RtlLengthSecurityDescriptor +0x2d`, 검사 없음 `[정적]` | 2.6 |
| 2 실제로 읽는 조각 · 암묵 길 | 명시 길은 넷째 (또는 다섯째) 만. 암묵 길은 둘째도 `[정적]` | 2.7 |
| 3 lsapolicylookup | 명시 길: 끼지만 삼킴. 암묵 길: 치명 `[정적]` | 2.8 |
| 4 프로시저 7 · 6 | **6 이 먼저** (전제는 `[추측]`), 입출력 · status `[정적]` | 2.9 |
| 5 `LocalInterfaceOnlySecCallback` | 0 통과 · 5 거절. 같은 접근 제한 명부의 비트 `0x2` 를 본다 `[정적]` | 2.10 |
| 6 "클래스 없음" | combase 가 안 만든다 — 서버 값을 그대로 올림 `[정적]`, 값은 `[공백]` | 2.11 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run149 와 같은 **옆길** 이다. test.exe 흐름 밖, COM 을 쓰는 프로그램이 `main` 뒤에 부르는 일이다.

이번엔 run149 의 첫 전화 (`Connect`) **답장 끝** 과, 그 답장을 combase 가 **뒤에서 꺼내 쓰는 곳** 을 본다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll: LdrInitializeThunk → LdrpInitializeProcess → DLL 들이기 · 전원 켜기
 └─ NtContinue (은퇴) → exe 진입점 → (CRT 점선 상자) → main
                                                        │
      test.exe 는 여기서 COM 을 안 쓴다 [소스]          │ COM 을 쓰는 프로그램이라면:
                                                        └─ CoInitializeSecurity (SD NULL, −1, …)
                                                             ├─ GetConnection → … → Connect (메뉴 0)        (run149)
                                                             │     답장 끝 SD 다섯 조각 → combase 전역 여섯   ← 이번
                                                             ├─ LookupPrincName → lsapolicylookup 창구      ← 이번
                                                             └─ GetDefaultAccessRestrictionsSD → 길이 재기  ← 이번 (여기서 죽었다)
                                                        첫 COM 사용 → CallTheResolver → 메뉴 6 먼저       ← 이번 (정정)
      ── 다른 가게: svchost (-k rpcss) ──────────────────────────────────────────────────────
           rpcss.dll: 서비스 시작 때 SD 넷 짓기 · 레지스트리에서 다섯째 읽기       ← 이번
           _Connect: 다섯 조각을 이어 붙여 답장에 실음                             ← 이번
```

- flow.md 에서는 3장 "**COM 이 RPCSS 에 거는 첫 전화**" 절 (run149) 에 글머리 넷 + 작은 나무 하나로 붙었다.
- `main` 뒤 일이라 그림엔 안 넣었다.

### 왜 이 질문이 지금 왔나

PE_emulator 쪽에서 막혔다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님).

- run149 해설 2.8 "최소 대답" 3 은 칸 28 ~ 30 을 **"SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL"** 로 권했다.
- 갈래 BG05 가 그대로 `Connect` 답을 세웠다.
- 그랬더니 게스트가 `seq 1354` 에서 rip `ntdll +0xe8f3d` 로 죽었다 (`0xc0000005`, 읽기 `0x1700744cd540`).
- 그 바로 앞 `1339 ~ 1353` 에는 `\RPC Control\lsapolicylookup` 연결이 8 번 `0xc0000034` 로 실패했다.
- 그래서 물음이 셋 생겼다.
  - 진짜 rpcss 는 이 다섯 조각을 **무엇으로** 채우나?
  - combase 는 그걸 **어디서** 읽다 죽었나?
  - lsapolicylookup 실패는 죽음과 **상관이 있나**?

### 무엇이 궁금했나

요청서 물음 여섯 (차례는 2 → 1 → 3 → 4 → 5 → 6):

1. rpcss `gpNoReg*SD` 넷 + `GetDefaultAccessRestrictions` — 누가 무엇으로 짓나. (가) 코드 상수 · (나) 레지스트리 · (다) 기계마다 다른 것 가운데 무엇인가. 크기 0 이 진짜에서 나오나.
2. combase 가 `RtlLengthSecurityDescriptor` 까지 가는 사슬 · 검사 · 실패 HRESULT · 실제로 읽는 조각. 암묵 길도 같은가.
3. `lsapolicylookup` 이 끼는가. 실패하면 그 길이 어떻게 끝나나.
4. ~ 6. (Q284a) 메뉴 7 · 6 몸, 문지기 콜백 `LocalInterfaceOnlySecCallback` 의 거절 조건, "클래스 없음" HRESULT 길.

### 바퀴 전에 알던 것

- `[정적]` (run149) `_Connect` 응답 칸 28 ~ 30 = SD 다섯 조각 크기 · 총 크기 · 덩어리. 가름은 (가).
  - 그리고 "**크기 합 0 이면 덩어리 NULL 가능 → 0 ×5 · 0 · NULL**" 이라 적었다 — 이번에 틀린 걸로 드러난다 (2.12).
  - combase 의 답 검사는 SD 크기 합을 **안 막는다** (run149 해설 2.9). 이건 지금도 맞다.
- `[정적]` (run149) rpcss · combase 쪽 전역 이름. 요청서가 rva 를 줬다. 그런데 **누가 무엇으로 짓는지** 는 몰랐다.
- `[정적]` (run136) `CoInitializeSecurity +0x47d GetConnection`.
- `[정적]` (run149) 첫 COM 사용은 "메뉴 7 → 6" — 이번에 차례가 반대로 드러난다 (2.9).
- `[정적]` (run135) LSA 이름 묻기의 **lsass 쪽**. 이번엔 손님 쪽 (sechost) 이다.
- 몰랐던 것: combase 가 SD 를 **어디서 · 어떻게** 다시 꺼내 쓰는지. 왜 죽는지. lsapolicylookup 이 왜 나오는지.

---

## 2. 찾은 것

### 2.1 한눈에 — 짓는 쪽 · 읽는 쪽 `[정적]`

run 문서 "호출 관계" 나무를 대표만 줄인 것이다. (나무 오프셋은 ret 자리, `+0x40f ~ +0x483` 같은 범위는 함수 안 줄. 콜스택으로 본 적 없음.)

```
rpcss (svchost -k rpcss) — 서비스 시작                                       [정적]
InitializeSCMBeforeListen
 ├─ +0x1aa  InitializeSystemSecurityDescriptors (0x73e04)
 │           ├─ +0x27c IsWorkstationOS → 형식 0x147050 / 0x146ed0
 │           └─ +0x2fc · +0x3a6 · +0x44c · +0x4f2  ConvertStringSecurityDescriptorToSecurityDescriptorW
 │                                         → gpNoReg{LaunchPerm, AccessPerm, LaunchRestr, AccessRestr}SD
 └─ +0x1ba  ReadRemoteActivationKeys → ReadPermissions → SetDefaultAccessRestrictions (레지스트리)

rpcss!_Connect  +0x40f ~ +0x483 크기 다섯 · +0x471 GetDefaultAccessRestrictions · +0x5df memcpy 루프

combase (손님 쪽)
CoInitializeSecurity (0x7e880)                                                [정적]
 ├─ +0x482  GetConnection → … Connect ⇐ 답장 → InitializeSystemSecurityDescriptors
 ├─ +0x2b4  LookupPrincName(0xa)
 │           └─ +0x11d LookupAccountSidLocalW (sechost) … LsaLookupOpenLocalPolicy
 │                       +0x49 NdrClientCall3 ── ALPC ──▶ ncalrpc:[lsapolicylookup]   실패 → 삼킴
 └─ +0x38a  GetDefaultAccessRestrictionsSD (0x7e5f4)   [0x3380e0] ?: [0x3380c8]
             └─ +0x33  GetSecurityDescriptorLength (KernelBase jmp) → ntdll!RtlLengthSecurityDescriptor   ✗ +0x2d
```

- 위 덩어리 = **짓는 쪽** (다른 가게, 관리실이 문 열 때).
- 아래 덩어리 = **읽는 쪽** (COM 을 쓰는 프로그램 안).
- 둘 사이를 잇는 게 `Connect` 답장의 칸 28 ~ 30 이다.

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `└─ +0x38a  GetDefaultAccessRestrictionsSD (0x7e5f4)   [0x3380e0] ?: [0x3380c8]`

- **누가**: combase `CoInitializeSecurity`.
- **어디서**: call 자리는 `+0x385`, 돌아올 자리 (ret) 는 `+0x38a`. 나무엔 ret 을 적는다.
- **누구를**: `GetDefaultAccessRestrictionsSD` (rva `0x7e5f4`).
- **무엇을 위해**: 접근 제한 명부를 하나 골라 **사본** 을 만든다. 사본은 `gSecDescRestrictions` 에 들어간다 (`+0x38a`).
- `[0x3380e0] ?: [0x3380c8]` 는 run 문서의 줄임 말이다.
  - 뜻: "다섯째 칸 (`0x3380e0`) 이 비어 있지 않으면 그것, 비었으면 넷째 칸 (`0x3380c8`)".
  - 코드: `GetDefaultAccessRestrictionsSD +0xf · +0x16 · +0x20 CMOVNZ` `[정적]`.

#### 줄 ②: `└─ +0x33  GetSecurityDescriptorLength (KernelBase jmp) → ntdll!RtlLengthSecurityDescriptor   ✗ +0x2d`

- **누가**: `GetDefaultAccessRestrictionsSD`.
- **어디서**: call `+0x2c`, ret `+0x33`.
- **누구를**: combase 가 가져다 쓰는 `GetSecurityDescriptorLength`.
  - KernelBase 쪽 그 함수 (rva `0xfd2b0`) 는 몸이 **`jmp [IAT 0x29af70]` 한 줄** 이다. 그 IAT 칸 (벽 콘센트) 에 꽂힌 게 **ntdll `RtlLengthSecurityDescriptor`** (rva `0xe8f10`) 다 `[정적]` (`pefile`).
  - combase 가 가져오는 이름은 api-set (별명) 이다. 그 별명이 KernelBase 로 풀린다는 것은 `[추측: 일반 규칙]` — 결과서가 `apisetschema` 표에서 못 찾았다.
- **무엇을 위해**: 명부 길이 재기. 사본을 만들려면 몇 바이트인지 알아야 하니까.
- `✗ +0x2d` = 원문 (PE_emulator) 이 죽은 줄. `+0x2d` 명령은 `MOVZX ECX,byte ptr [RCX+1]` — SID 머리를 읽는 줄이다 `[정적]`.

### 2.3 ★ 다섯 조각 — 어디서 무엇으로 서나 `[정적]`

#### 새 말 — SD, 한 번 더 한 줄로

- **SD (보안 서술자, Security Descriptor)** = "이 물건은 누가 주인이고, 누가 무엇을 해도 되나" 를 적은 **출입 명부** ([run97 해설](run97-rpc-sd-query-해설.md) 2.0).
- 명부 칸: 주인 (소유자) · 소속 (그룹) · **허락 쪽** (DACL) · 특수 쪽 (SACL).
- 이번 명부들은 "COM 관리실 창구에 **누가 전화해도 되나 · 누가 물건을 띄워도 되나**" 를 정한다 — 이름이 Launch (실행) · Access (접근) 다.

#### 칸 차례

- `rpcss!_Connect +0x40f ~ +0x483` 이 크기 다섯을 `[RDI+0] · [+4] · [+8] · [+0xc] · [+0x10]` 에 쓴다.
- `+0x5ce ~ +0x5ee` 루프가 **같은 차례** 로 조각을 이어 붙인다 (`memcpy` ret `+0x5df`). 크기 0 이면 건너뛴다 (결과서).
- 비유: 봉투 겉에 "1쪽 124 · 2쪽 124 · …" 하고 쪽수를 적고, 안에 명부를 차례로 넣는다.

#### 다섯 조각 표 (run 문서 1절 표 줄임)

| 칸 | 조각 (rpcss 전역) | 출처 | 크기 | combase 쪽 전역 |
|---|---|---|---|---|
| `[0]` | 실행 권한 `gpNoRegLaunchPermissionsSD` | **(가)** SDDL 상수 `0x1472b0` | 124 B `[추측: 계산]` | `_pNoRegLaunchPermissionsSD` `0x3380e8` |
| `[1]` | 접근 권한 `gpNoRegAccessPermissionsSD` | **(가)** `0x147240` | 124 B `[추측: 계산]` | `_pNoRegAccessPermissionsSD` `0x3380f8` (+ 크기 `0x3380f0`) |
| `[2]` | 실행 제한 `gpNoRegLaunchRestrictionsSD` | **(가)** `0x147320` + 고정 SID 둘 | 232 B `[추측: 계산]` | `_pNoRegLaunchRestrictionsSD` `0x3380d0` |
| `[3]` | 접근 제한 `gpNoRegAccessRestrictionsSD` | **(가)** `0x147050` / `0x146ed0` + 고정 SID 둘 + OS 종류 | 228 B `[추측: 계산]` | **`_pNoRegAccessRestrictionsSD` `0x3380c8`** |
| `[4]` | 기본 접근 제한 `GetDefaultAccessRestrictions()` | **(나)** 레지스트리 | VM 값 그대로 (원문 `[수집]` 228 B). 없으면 **NULL → 0** | `_pDefaultAccessRestrictionsSD` `0x3380e0` |

- 그래서 combase 전역이 **여섯** 인 이유: 조각 다섯 + 둘째 조각의 크기 칸 하나 (`0x3380f0`).
- 넷 (`[0]` ~ `[3]`) 을 짓는 곳: rpcss `InitializeSystemSecurityDescriptors` (rva `0x73e04`). 부르는 곳은 `InitializeSCMBeforeListen +0x1a5` — **서비스가 시작할 때 한 번** `[정적]`.
  - 짓는 도구: `ConvertStringSecurityDescriptorToSecurityDescriptorW` (네 번, call `+0x2f5` · `+0x39f` · `+0x445` · `+0x4eb`).
  - 넷은 이 함수 안에서만 쓰인다 (refs) → 한 번 짓고 다시 안 바뀐다.
  - 레지스트리를 **안 본다.** 이름의 "NoReg" 그대로다.

#### 새 말 — SDDL

- **SDDL** = 출입 명부를 **한 줄 글자** 로 적는 꼴. 사람이 읽고 쓰기 쉬운 "글자판 명부" 다.
- `ConvertString…` 함수가 이 글을 받아서 컴퓨터가 쓰는 **바이트 명부 (SD)** 로 바꿔 준다.
- 비유: 손으로 쓴 메모 ("주인: 관리자, 모두 들어와도 됨") 를 **인쇄소에 맡겨 정식 명부 용지로 찍는** 것.
- 네 조각의 글은 rpcss 파일의 읽기 전용 칸 (`.rdata`) 에 박혀 있다 `[정적]` (`pefile` 로 읽음). 그래서 (가) "코드 상수" 다.

#### 한 줄 읽어 보기 — 넷째 `[3]` (워크스테이션) 글

run 문서 1절 표에 적힌 글 (형식 `0x147050`):

```
O:BAG:BAD:(A;;0x7;;;WD)(A;;0x3;;;AN)(A;;0x7;;;%s)(A;;0x7;;;%s)(A;;0x3;;;AC)(A;;0x3;;;S-1-15-3-1024-…)
```

앞에서부터 조각내 읽는다.

- `O:BA` = 소유자 BA. `G:BA` = 그룹 BA. BA = `S-1-5-32-544` `[규칙]`.
- `D:` = 여기서부터 **허락 쪽 (DACL)**.
- 괄호 하나 = **ACE 하나**.

#### 새 말 — ACE, 한 번 더 한 줄로

- **ACE** = 명부의 **한 줄** ("○○ 에게 □□ 를 허락") ([run97 해설](run97-rpc-sd-query-해설.md) 2.0).
- `(A;;0x7;;;WD)` 를 읽으면:
  - `A` = 허용 줄. 바이트로는 형 0 · 깃발 0 (run 문서 1-1, 그 절은 `[추측: 계산]` — 2.5).
  - 가운데 빈칸들 = 깃발 · 그 밖 칸 없음 `[추측: 일반적으로 알려진 SDDL 꼴]`.
  - `0x7` = 허락하는 일 (마스크).
  - `WD` = 누구에게. `S-1-1-0` `[규칙]` — run 문서는 이 줄을 "Everyone" 이라 부른다.
- 다른 약어: AN `S-1-5-7` · AC `S-1-15-2-1` · SY `S-1-5-18` · IU `S-1-5-4` · PS `S-1-5-10` `[규칙]`.
- 마스크 비트 `0x2` 는 2.10 의 문지기가 보는 비트다 `[정적]` (결과서 4-2). 그 비트의 이름 COM "실행 · 로컬" 은 공개 규칙이다 `[규칙]` (결과서 표기 "COM 권한 비트").

#### `%s` 둘 — 끼워 넣는 SID 도 기계마다 같다 `[정적]` + `[규칙]`

- 셋째 · 넷째 글에는 `%s` 칸이 둘 있다. 글을 바이트로 바꾸기 전에 `StringCbPrintfW` 가 SID 글 둘을 끼운다 (call `+0x2a1` · `+0x2c8`).
- 끼우는 SID:
  - 첫 `%s` = `gSidDCOMUsers` → `ConvertSidToStringSidW` (`+0x4e`)
  - 둘째 `%s` = `gSidPerfLoggingUsers` → `ConvertSidToStringSidW` (`+0xcd`)
- 두 SID 는 `StartTokenHandling +0x216 CreateWellKnownSid` 가 표 `gStartupSids` (`0x1643f0`, 16 B × 9칸) 로 짓는다.
  - 칸 3 = 종류 **61**, 도메인 0 → DCOM Users `S-1-5-32-562` `[규칙]`
  - 칸 5 = 종류 **58**, 도메인 0 → Performance Log Users `S-1-5-32-559` `[규칙]`
- "도메인 0" = 도메인 SID 를 안 붙인다 → **BUILTIN** 이라 **기계마다 같다.**
- 그래서 (다) "기계 · 부팅마다 다른 SID" 는 **없다.**

#### 워크스테이션이냐 서버냐 — 넷째만 한 글자 다르다

- `+0x277 IsWorkstationOS` 가 형식을 고른다 `[정적]`.
  - 워크스테이션 → `0x147050` → AN (익명) 마스크 `0x3`
  - 서버 → `0x146ed0` → AN 마스크 `0x7`
- `IsWorkstationOS` 는 NT 플랫폼 (`+0x5f CMP [..],2`) 이고 제품 종류 1 (`+0x66 CMP byte [..+0x14a],1`) 인지 본다 `[정적]`.
- Win 11 Education 은 워크스테이션 → AN `0x3` `[추측: OS 이름에서 — VM 의 제품 종류 값은 안 봄]`.

#### 다섯째 — 건물주가 써 둔 명부 `[정적]`

- `GetDefaultAccessRestrictions()` (rva `0x617f4`, 전역 `gpDefaultAccessRestrictionsSD` `0x1656b0`).
- 어디서 읽나 — 두 군데, 차례대로:
  1. 정책 `HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM` 의 값 `MachineAccessRestriction` (SDDL 글). 있으면 이걸 쓰고 아래는 건너뛴다.
  2. 없으면 `HKLM\SOFTWARE\Microsoft\OLE` 의 값 `MachineAccessRestriction` (REG_BINARY = 바이트 명부 그대로).
- 읽은 뒤 검사를 거친다: `ReadSecurityDescriptor` 의 모양 검사 → `CheckForExpectedAccess` (`ReadPermissions +0x280`) → `SetDefaultAccessRestrictions` (`+0x29f`).
- 둘 다 없거나 검사에 걸리면 **NULL**. 그러면 `_Connect +0x47b JZ` 가 칸 `[4]` 를 안 쓴다 → **크기 0** — **진짜에서도 나는 꼴** 이다.
- 다섯째만 **시작 뒤에도 바뀔 수 있다.** `ReadPermissions` 를 부르는 곳이 서비스 시작 (`ReadRemoteActivationKeys +0x17c`) 말고도 `TimerCallbackFn` · `UpdateActSettingsInternal` · 정책 알림 `PolicyCallbackFn` 에 있다 (결과서).
- 덤 하나: 등록값에 기대한 ACE 가 없으면 rpcss 가 **레지스트리를 고쳐 쓴다** (`HandleInvalidRestrictionFromOleKey` → `UpdateComSecurityDescriptors`, 옛값은 `…Old` 이름으로). → VM 레지스트리는 rpcss 가 한 번 뜬 **뒤에** 떠야 한다.

### 2.4 ★ `[수집]` 셋은 어느 조각? — 크기가 같아서 헷갈리는 곳 `[정적]`

원문은 레지스트리에서 크기 셋을 모아 왔다 (`[수집]`). 이게 어느 조각인지 물었다.

| 원문 `[수집]` 값 (크기) | rpcss 안에서 가는 곳 | `Connect` 답장에 실리나 |
|---|---|---|
| `DefaultLaunchPermission` (124 B) | `ReadPermissions +0x171 ~ +0x17e` → `SetDefaultLaunchPermissions` (`gpDefaultLaunchPermissionsSD` `0x1656a8`) — 활성화 검사가 읽음 | **안 실림** |
| `MachineLaunchRestriction` (232 B) | `gpDefaultLaunchRestrictionsSD` (`0x1656b8`) | **안 실림** |
| **`MachineAccessRestriction` (228 B)** | **다섯째 `[4]`** | **실림** |
| `DefaultAccessPermission` | rpcss 는 안 읽음. combase `GetLegacyBlanket +0x299` 가 읽음 (없으면 `[1]` 로) | — |

여기서 헷갈리기 쉬운 곳 — 단계로:

1. **근거**: 레지스트리 세 값의 크기 124 · 232 · 228 이 NoReg 조각 `[0]` · `[2]` · `[3]` 크기와 **하나씩 정확히 같다** (124 = `[0]`, 232 = `[2]`, 228 = `[3]`).
2. **그래서**: "이 VM 의 레지스트리 값은 **같은 SDDL 로 지은 명부를 레지스트리에 써 둔 것**" 일 가능성이 크다.
3. **아직 확인 아님**: 크기만 같다. 바이트가 같은지는 VM 에서 봐야 한다 `[추측: 크기만 같음, 바이트는 VM]`.
4. **조심**: 크기 셋 가운데 답장에 실리는 건 228 **하나뿐** 이고, 그것도 `[3]` 자리가 아니라 **`[4]` (다섯째)** 자리다. 나머지 둘은 rpcss 안 다른 전역으로 간다.

비유: 관리실 인쇄 명부와 건물주 명부가 **쪽수까지 똑같다.** 아마 같은 원본을 베낀 것 같다. 하지만 펼쳐 보기 전엔 모른다.

### 2.5 ★ 16진 바이트 — 왜 `[추측: 계산]` 인가

run 문서 1-1 에는 네 조각의 16진이 통째로 있다. 그런데 태그가 `[정적]` 이 아니라 **`[추측: 계산]`** 이다. 이걸 천천히 푼다.

#### 새 말 — 자기 상대 (self-relative), 한 번 더 한 줄로

- **자기 상대 SD** = 명부의 머리 칸에 "소유자는 **머리에서 몇 바이트 뒤**" 를 적고, 조각을 **한 덩어리로 이어 붙인** 꼴 ([run97 해설](run97-rpc-sd-query-해설.md) 2.5 "한 덩어리 꼴").
- 반대말은 **절대 형식** — 머리 칸에 조각의 **주소** 를 적는다.
- 비유: 자기 상대 = 한 장짜리 복사본에 "소유자는 이 종이 n바이트째부터". 절대 형식 = "소유자는 3층 서랍에" 같은 위치 메모.
- 봉투에 넣어 다른 가게로 보내려면 **한 덩어리** 여야 한다. 그래서 답장에 실리는 건 자기 상대다.
- 구별 표시: 머리의 Control 칸 비트 15 (`0x8000`). 켜져 있으면 자기 상대 `[정적]` (`RtlLengthSecurityDescriptor +0x14 · +0x18`).

#### 짓는 길과 모양 — 여기까지는 코드에서 읽었다 `[정적]`

- 짓는 길: sechost `ConvertStringSecurityDescriptorToSecurityDescriptorW` → `LocalConvertStringSDToSD_Rev1` → `RtlAbsoluteToSelfRelativeSD` → ntdll `RtlMakeSelfRelativeSD`.
- 모양 (`RtlMakeSelfRelativeSD +0xf0 ~ +0x179`, flow-writer 도 읽음):
  - 머리 `0x14` 바이트
  - 그다음 **DACL** (`+0x118` 복사, 오프셋은 `+0x129` 가 머리 `+0x10` 에)
  - 그다음 **소유자** (`+0x14a` → 머리 `+4`)
  - 그다음 **그룹** (`+0x16b` → 머리 `+8`)
  - Control 에 `0x8000` 을 켠다 (`+0x174 ~ +0x179`). SACL 은 없어서 머리 `+0xc` = 0.

#### 넷째 머리 20 바이트 읽어 보기 `[추측: 계산]`

run 문서 1-1 의 `[3]` 16진 맨 앞 20 바이트를 칸으로 자르면:

| 바이트 (16진 그대로) | 머리 칸 | 뜻 |
|---|---|---|
| `01` | 리비전 | 1 |
| `00` | (빈 칸) | |
| `0480` | Control | `0x8004` = `0x8000` 자기 상대 + `0x4` DACL 있음 |
| `c4000000` | `+4` 소유자 오프셋 | 머리에서 `0xc4` 뒤 |
| `d4000000` | `+8` 그룹 오프셋 | 머리에서 `0xd4` 뒤 |
| `00000000` | `+0xc` SACL 오프셋 | 0 = 없음 |
| `14000000` | `+0x10` DACL 오프셋 | `0x14` 뒤 = 머리 바로 뒤 |

- 바이트는 작은 쪽부터 적힌다 (`0480` → `0x8004`). 머리 `01 00 04 80` 은 run 문서가 적은 그대로다.
- 차례가 "머리 → DACL → 소유자 → 그룹" 이라서 DACL 오프셋이 제일 작고, 소유자 · 그룹이 뒤에 온다. 위 모양과 맞는다.
- 크기 셈 (run 문서): `[3]` = 머리 20 + ACL 176 + 소유자 · 그룹 32 = **228**.

#### 그럼 왜 `[정적]` 이 아니라 `[추측: 계산]` 인가 — 단계로

1. **코드에서 읽은 것** `[정적]`:
   - SDDL 글 넷 (rpcss `.rdata`, `pefile`).
   - 배치 차례 · Control `0x8004` · ACL 리비전 2 (`RtlMakeSelfRelativeSD`, `LocalGetAclForString`).
2. **코드 밖에서 가져온 것** `[규칙]`:
   - SDDL 약어 → SID 표 (BA → `S-1-5-32-544` 같은 것).
   - `WELL_KNOWN_SID_TYPE` 번호 (61 → DCOM Users 같은 것).
3. **둘을 합쳐 만든 것** = 16진.
   - 결과서가 스크래치 스크립트 (`s148/mksd.py`, 커밋 안 함) 로 계산했다.
   - 진짜 메모리에서 본 값이 **아니다.**
4. **flow-writer 가 따로 다시 계산했다** → 넷 다 **바이트까지 같았다** (124 · 124 · 232 · 228).
   - 그런데 **같은 규칙** 으로 계산했으니, 둘이 같다는 건 "계산을 실수 없이 했다" 는 뜻일 뿐이다.
   - "진짜도 이렇다" 를 보여 주는 **독립 증거가 아니다.** 그래서 태그를 안 올렸다.
5. **진짜와 어긋날 수 있는 곳** (결과서): ACL 안 ACE 차례 · SACL 이 없을 때 `+0xc` 값 · 소유자 / 그룹 / DACL 배치 차례.
6. **닫는 법**: VM 에서 `combase!CRpcResolver::_pNoRegAccessRestrictionsSD` 가 가리키는 228 B 를 덤프해 맞대 본다 (4절 Q283 D5).

- 비유: 설계도 (SDDL 글) 와 인쇄소 규칙 (배치 차례) 을 보고 **"찍으면 이렇게 나올 거야"** 하고 종이에 그려 본 것이다. 두 사람이 따로 그려서 같게 나왔지만, 둘 다 같은 규칙책을 봤다. **진짜 찍힌 명부를 한 번 펼쳐 봐야** 끝난다.
- 결과서와 표기 차이 하나: 결과서는 1-1 제목을 `[정적: 글 + self-relative 규칙]` 으로, PE_emulator 표 제목을 "`[정적]` 바이트" 로 적었다. 본문에선 "계산한 값 (진짜에서 본 것 아님)" 이라 했다. **run 문서는 이것을 `[추측: 계산]` 으로 적었고, 이 글도 run 문서를 따른다.**
- 덧붙여 계산값인 것: 덩어리 합 **708 B** (다섯째 없음) / **936 B** (다섯째 228 B), `RtlLengthSecurityDescriptor` 로 `[3]` 을 재면 228 이 나온다는 것.

### 2.6 ★ 죽는 사슬 — 빈 봉투를 받은 날 `[정적]`

명시 호출 `CoInitializeSecurity(SD NULL, cAuthSvc −1, …)` · AppID 없음 길이다. 원문 로그 (`seq 1217`) 가 이 꼴이었다.

#### 먼저 — combase 가 답장을 받아 둘 때 (run 문서 2절 표 0)

- combase `InitializeSystemSecurityDescriptors` 가 답장의 다섯 조각을 전역 여섯에 옮긴다.
  - 조각마다 `LocalAlloc(0x40, 크기)` 로 자리를 잡고 `memcpy` 로 복사한다 `[정적]`.
  - 조각 하나에 줄이 셋이다. 그래서 오프셋도 다섯 개씩 **세 벌** 이 나온다 — 셋 다 맞고, 가리키는 줄이 다르다 (run 문서 맨 위 "정정 · 대조 (run152 때 대조)" (a), 2절 표 0):

    | 무슨 줄 | 오프셋 (조각 다섯) |
    |---|---|
    | ① `LocalAlloc` call (자리 잡기) | `+0x2a` · `+0x62` · `+0x9a` · `+0xd0` · `+0x107` |
    | ② 받은 주소를 combase 전역에 넣는 줄 | `+0x36` · `+0x6e` · `+0xa6` · `+0xdc` · `+0x113` |
    | ③ `memcpy` call (답장에서 베끼기) | `+0x4f` · `+0x8c` · `+0xc2` · `+0xf4` · `+0x12b` |

    - 결과서 2절 표 0 은 ② 를, 결과서 1-1 끝 ("LocalAlloc 후 memcpy") 은 ③ 을 짚었다. 처음엔 서로 어긋나 보였지만, `.asm` 으로 가려 보니 한 조각을 옮기는 **차례의 세 걸음** 이었다.
    - 비유: 봉투 하나를 서랍에 넣을 때 ① 빈 봉투 받기 → ② 서랍 칸에 봉투 꽂기 → ③ 답장 종이를 봉투에 베껴 넣기.
    - 할당이 NULL 이면 `+0x40` 등 `JZ +0x14e` → `0x8007000e` (run 문서 2절 표 0).
- 여기가 핵심이다:
  - **넷째까지는 크기가 0 이어도 할당한다.**
  - **다섯째만** `+0xfc CMP [RDI+0x10],0; JZ` 로 건너뛴다 → NULL 로 남는다.
- 크기 0 할당이 NULL 이 아니라 **"0 B 짜리 덩어리" 주소** 를 준다는 것은 `[추측: 일반 동작]` 이다. 원문도 그렇게 봤다.

#### 새 말 — 0 B 덩어리

- 크기 0 으로 받은 메모리 자리. **주소는 있는데, 안에 쓸 칸이 하나도 없다.**
- 비유: 이름표는 붙었는데 **속이 빈 봉투.** 겉보기엔 "봉투 있음" 이다.

#### 단계로 (run 문서 2절 표 줄임)

| # | 줄 (`CoInitializeSecurity` 기준) | 하는 일 |
|---|---|---|
| 1 | `+0x47d GetConnection` (ret `+0x482`) → `+0x494 JNZ → +0xb2` | `Connect` 성공 → 본 줄기로 |
| 2 | `+0xb2 ~ +0xdc` | SD NULL 이면 인증 수준 → 2 |
| 4 | `+0x64a utGetAppIdForModule` (ret `+0x64f`) | 실행 파일 이름으로 AppID 찾기 → 없음 → `g_AppId` NULL 그대로 |
| 7 | **`+0x2af LookupPrincName`** (ret `+0x2b4`) · `+0x2bb JS → +0xa53` | **lsapolicylookup 이 끼는 자리** (2.8). 실패해도 HRESULT 안 바뀜 |
| 8 | `+0x30e ~ +0x369` | 넘긴 SD 가 NULL → 유효성 검사 건너뜀. `gSecDesc` = NULL (`+0x369`) |
| 9 | **`+0x385 GetDefaultAccessRestrictionsSD`** (ret `+0x38a`) | 다섯째 (`0x3380e0`) 가 NULL → 넷째 (`0x3380c8`) 를 고름 |
| 10 | `GetDefaultAccessRestrictionsSD +0x2c CALL [GetSecurityDescriptorLength]` (ret `+0x33`) | KernelBase `jmp [IAT]` → ntdll `RtlLengthSecurityDescriptor` |
| 11 | **`RtlLengthSecurityDescriptor +0x2d MOVZX ECX,byte ptr [RCX+1]`** | 소유자 SID 머리를 읽다 **접근 위반** |

에뮬레이터 대답 ("크기 0 ×5") 을 이 표에 대 보면 — 한 걸음씩:

1. 답장 받아 두기: 넷째 크기 0 → **0 B 덩어리 (NULL 아님)**. 다섯째 크기 0 → **NULL**.
2. `+0x385`: 다섯째가 NULL 이니 넷째를 고른다. 넷째는 NULL 이 아니니 **통과.**
3. `+0x2c`: 그 주소를 명부라 믿고 길이 함수에 넘긴다.
4. 길이 함수 `+0x0`: `MOVSX R9D,word [RCX+2]` — Control 칸을 읽는다. 0 B 덩어리라 **여기부터 이미 덩어리 밖** 이다.
5. 밖의 쓰레기가 우연히 "비트 15 켜짐 (자기 상대)" + "소유자 오프셋 ≠ 0" 이면:
   - `+0x1e` 소유자 오프셋 `[RCX+4]` 읽기 → `+0x25` 덩어리 주소에 더하기 → `+0x2b` NULL 만 거름.
   - **`+0x2d`** 에서 엉뚱한 주소를 SID 로 읽는다 → **접근 위반.**
6. 원문 관찰과 맞대기: rip `ntdll+0xe8f3d` = `0xe8f10 + 0x2d` ✓ (에뮬레이터 로그 — `[관찰]` 아님).

#### 왜 `+0x0` 이 아니라 `+0x2d` 에서 죽었나 `[추측: 원문 rip · 주소에서]`

- `+0x0` 도 덩어리 밖을 읽는다. 그런데 거기선 안 죽었다.
- run 문서의 짐작: 덩어리 **바로 뒤는 읽을 수 있는 힙** 이었다. 그 쓰레기 안 "오프셋" 값이 **먼 주소** 를 가리켜서, 따라간 `+0x2d` 에서 넘어졌다.
- 비유: 빈 봉투 바로 옆 책상 종이는 읽을 수 있었다. 그 종이에 "소유자는 300 쪽 뒤" 라고 적혀 있어서 따라갔더니 **건물 밖 낭떠러지** 였다.

#### 검사가 없다 `[정적]`

- `GetDefaultAccessRestrictionsSD` 가 하는 검사는 **NULL 검사 하나** (`+0x24 TEST RBX,RBX`) 뿐이다.
- 그다음은 길이 재기 → `HeapAlloc` (`+0x46`) → `memcpy` (`+0x63`). `IsValid…` 같은 모양 검사를 **안 부른다.**
- 실패 HRESULT 가 나는 길은 둘뿐이고, 둘 다 `0x8007000e`:
  - 다섯째 · 넷째 둘 다 NULL
  - `HeapAlloc` 실패
- 그래서 "0 B 덩어리" 는 실패 HRESULT 로 안 끝나고 **바로 넘어진다.** 넷째도 NULL 이었으면 `0x8007000e` 로 깨끗이 끝났을 것이다. 그런데 크기 0 으로는 combase 전역을 NULL 로 만들 수 없다 (위 1 단계 — `LocalAlloc` 크기 0 이 NULL 을 안 준다는 `[추측: 일반 동작]` 위에서).

#### 진짜 명부라면 — 끝까지 간다

- 길이 → 사본 → `gSecDescRestrictions` (`+0x38a`) → `CalculateAnonymousGrantedRemoteAccessInRestrictions` (`+0x39a`, **반환값 안 봄**) → `CoVrfCheckSecuritySettings` (`+0x39f`) → … → **HRESULT 0**.
- `CalculateAnonymous…` 는 같은 명부로 익명 접근 검사를 하지만, 실패해도 HRESULT 를 안 바꾼다 (결과서 `[정적]`). 그 안의 syscall 은 안 따라갔다 `[공백]`.

### 2.7 넷 중 실제로 읽는 것 — 명시 · 암묵 · 공개 API `[정적]`

| 조각 | 명시 길 (SD NULL, 능력 0) | 암묵 길 (능력 8 `EOAC_APPID`) | 공개 API `CoGetSystemSecurityPermissions` |
|---|---|---|---|
| `[0]` 실행 권한 | 안 읽음 | 안 읽음 | 종류 0 (`+0x11b` · `+0x14b`) |
| `[1]` 접근 권한 | 안 읽음 | **읽음** — `GenerateDefaultAccessPermissionsSecurityDescriptor +0x41` · `+0x48` (`DefaultAccessPermission` 이 없을 때) | 종류 1 (`+0xe2`) |
| `[2]` 실행 제한 | 안 읽음 | 안 읽음 | 종류 2 (`+0xa5` · `+0xd9`) |
| `[3]` 접근 제한 | **`+0x385`** (다섯째 없을 때) | **`+0x385`** (같음) | 종류 3 (`+0x65` · `+0x99`) |
| `[4]` 기본 접근 제한 | **`+0x385` 가 먼저 봄** | 같음 | 안 줌 |

- 공개 API 쪽 일꾼은 `GetSystemSecurityDescriptors` 다. 이것을 부르는 곳은 `CoGetSystemSecurityPermissions +0xf` **하나** (결과서 refs) → **초기화 길에선 안 불린다.**
  - 원문이 "죽기 전에 불리나" 를 `[모름]` 으로 뒀던 것이 닫혔다.
- 그러니 명시 길만 보면 `[0]` · `[1]` · `[2]` 는 0 B 여도 지나간다.
- 하지만 결과서 권고는 **"그래도 진짜 바이트를 줘라"** 다. 공개 API 를 쓰는 프로그램이 있으면 쓰레기를 받고, 암묵 길은 `[1]` 에 ACE 를 덧붙이며 읽는다.

### 2.8 ★ lsapolicylookup — 끼지만 (명시 길에선) 삼키고 지나간다 `[정적]`

#### 새 말 — lsapolicylookup 창구

- `\RPC Control\lsapolicylookup` = **SID ↔ 이름 찾기 (LSA lookup)** 창구 `[정적]` (run 문서 3절 "한 줄").
- 받는 쪽은 lsass 로 보인다 `[추측]`.
- 비유: 신분 번호 (SID) 를 불러 주면 **"아, 그건 ○○ 씨예요"** 하고 이름을 찾아 주는 안내 데스크. run135 의 `lsasspirpc` 와는 **다른 창구** 다.

#### 누가 · 왜 여나 — 한 걸음씩

1. `CoInitializeSecurity +0x2af LookupPrincName(0xa)` — 인증 서비스 번호 `0xa` 일 때 이 길 (`LookupPrincName +0x82 CMP R12W,0xa`).
2. `LookupPrincName +0x5e GetSelfSid` — **내 프로세스 토큰 (신분증) 의 사용자 SID** 를 꺼낸다.
3. `+0x116 LookupAccountSidLocalW` (sechost) — 그 SID 의 **이름** 을 묻는다. **최대 두 번** (크기 읽고 다시).
4. sechost `LookupAccountSidInternal +0xdd LsaLookupOpenLocalPolicy` — SID 캐시가 꺼져 있으면 곧장 창구를 연다.
5. `LsaLookupOpenLocalPolicy +0x42 NdrClientCall3` — 바인딩은 `PLSA_LOOKPR_SERVER_NAME_bind` 가 `"ncalrpc"` + `"lsapolicylookup"` 으로 만든다.
   - 즉 **114 에 번호를 안 묻고** (`ept_map` 없이) 곧장 ALPC 연결이다. 원문 "연결 8 번" 의 상대가 이것이다.

#### "삼킨다" 는 게 뭔가 — 실패 갈래

- 창구가 없으면: `LookupAccountSidInternal +0xe4 JS → +0x26c` → FALSE → `LookupPrincName` 이 에러 (`GetLastError | 0x80070000`) 를 돌려준다.
- `CoInitializeSecurity +0x2bb JS → +0xa53` 에서 그 에러를 받는다. 그리고:
  - `+0xa97 HeapAlloc(0xc)` 로 **기본 보안 배열** `gpsaSecurity` 를 만든다.
  - `+0xab8` 첫 칸 = `0x20004` (항목 4 · 보안 오프셋 2), `+0xabe` 다음 칸 = 0.
  - `+0xac2 JMP +0x306` → `+0x30e` 로 **본 줄기에 합류.**
- **HRESULT 는 안 바뀐다.** 이름을 못 찾은 채 기본값으로 계속 간다.
- 그래서 **죽음과 무관** 하다. 창구가 있든 없든 `+0x385` 에서 똑같이 죽는다.
- 비유: 안내 데스크가 문을 닫았으면 "이름은 모르겠네" 하고 **빈 이름표** 를 달고 그냥 들어간다. 넘어진 건 그 뒤 명부 봉투 때문이다.

#### 암묵 길에선 다르다 — 치명

- `CoInitializeSecurity` 를 직접 안 부르고 COM 을 쓰면, combase 가 능력 8 (`EOAC_APPID`) 로 대신 부른다 (run149 해설 2.12 "암묵 호출").
- 이 길은 `+0x5e7 GetLegacyBlanket` → `GenerateDefaultAccessPermissionsSecurityDescriptor` 로 간다.
  - 둘째 조각 `[1]` 을 바탕으로 깔고,
  - `LsaLookupOpenLocalPolicy` → `LsaLookupGetDomainInfo` → `CreateWellKnownSid` 로 ACE 를 지어,
  - `AddAcesToSecurityDescriptorViaApi` 로 덧붙인다.
- 여기서 창구가 실패하면 **`CoInitializeSecurity` 가 그 에러로 끝난다** (`+0x5f3 JS → +0x87b`). 정확한 HRESULT 는 `[공백]` (wil 변환).
- 명시 길과 **반대** 다.

#### "8 번" 은 왜 — 반만 안다

- sechost 안에는 재시도 반복이 **없다** — 호출 두 번 × `NdrClientCall3` 한 번씩 `[정적]`.
- 나머지는 rpcrt4 가 `ncalrpc` 연결을 다시 시도하는 것으로 읽히지만, rpcrt4 를 이번에 안 열었다 `[공백]`.

#### 원문 로그와 맞추기 `[추측: 요청서 인용 대조 — 로그는 지워져 직접 못 봄]`

- `seq 1217` 명시 `CoInitializeSecurity(SD NULL, −1, …)` → `1339 ~ 1353` 연결 실패 → `1354` 죽음.
- 이 차례는 2.6 표의 **7 → 9** (명시 길) 와 맞는다.

#### 그래서 PE_emulator 의 LSA1 바퀴는 (run 문서 "받을 꼴 ④")

- 명시 호출 표본 (r19 OR1): LSA1 은 OR1 **앞 바퀴가 아니다.** 칸 28 ~ 30 만 고치면 된다.
- `CoInitializeSecurity` 없이 COM 을 쓰는 표본 (암묵, 결과서 예: r18 432): lsapolicylookup (도메인 정보 포함) 이 먼저 필요 → LSA1 이 **앞 바퀴.**

### 2.9 ★ Q284a ① — 첫 부름은 메뉴 6 이다 (정정 b) `[정적]`

#### 새 말 — 아파트 · OXID

- **아파트** = COM 물건들이 사는 칸 (스레드 묶음) 이고, **OXID** 는 그 아파트의 번호다 `[추측: 일반적으로 알려진 설명]`. 코드에서 본 건 이름 `OXIDEntry` · `CComApartment` 와 칸뿐이다.
- OID = 물건 번호 (run149 해설 2.8 "예약 OID").
- 비유: COM 관리실 장부에 먼저 **"우리 동 (아파트) 번호 등록"** 을 해야, 그 동의 **호수 (OID)** 를 더 받을 수 있다.

#### 갈림길 한 줄 — `CallTheResolver +0x9d JZ`

- `+0x94 MOV AL,[RCX+0xd8]` · `+0x9b TEST AL,AL` · **`+0x9d JZ → +0x1ad`**.
- 단계로:
  1. **근거**: 그 칸 (`[..+0xd8]`) 이 0 이면 `+0x1ad` 쪽 → `+0x1b7 RegisterOXIDAndOIDs` → `ServerRegisterOXID +0xce NdrClientCall2` = **메뉴 6**. 0 이 아니면 `+0x106 NdrClientCall2` = **메뉴 7**.
  2. **그 칸이 무엇인가**: 여기서 `RCX = [RDI+0x10]` = 아파트의 **`OXIDEntry`** 이고, 칸은 **`OXIDEntry+0xd8` (1 바이트 등록 표시)** 다. 메뉴 6 이 성공하면 `RegisterOXIDAndOIDs` 가 OXID (`+0x93`) · MID (`+0xa7`) 를 저장하고 **`+0xb1 XCHG [OXIDEntry+0xd8],1`** 로 바로 그 칸에 1 을 쓴다 (run 문서 4-1, run152 때 대조).
  3. **그래서**: "아직 등록 안 됨 → 6 (등록)", "등록 뒤 → 7 (OID 더 받기)".
  4. **아직 확인 아님**: 처음 만들 때 그 칸이 0 이라는 것. `OXIDEntry` 생성자를 안 읽었다 → `[추측]`. 그래서 "첫 부름은 6" 도 그 전제 위에 있다.
- 다음 벽 = **메뉴 6.** 그 앞에 `ServerRegisterOXID +0x30 StartListen` 이 먼저 돈다.

#### 메뉴 6 · 7 한눈에 (run 문서 4-1 줄임)

| | 메뉴 7 (`ServerAllocateOIDs`) | 메뉴 6 (`ServerAllocateOXIDAndOIDs`) |
|---|---|---|
| 언제 | 아파트 등록 **뒤** | 아파트 **미등록** (첫 부름) |
| 서버 거절 | `ph` 모름 · PID 다름 → **5**, 널 인수 → **`0x57`**, OXID 없음 → **`0x776`** | 깃발 · 모양 틀림 → **`0x57`**, `CheckLocalSecurity` 실패 → **5**, DSA `DsaValid` 거짓 → **`0x57`** |
| combase 검사 | `RetryRPC` · `CheckStatus`. 받은 OID 를 `min(돌려준 수, 빈 칸)` 개 넣고 개수는 **잘라 더하지 않음** | `RetryRPC` · `CheckStatus`. 돌려받은 DSA 가 비-NULL 인데 `DsaValid` 거짓 → **`0x8000ffff`** |
| 안전한 최소 대답 | 상태 0 · 개수 ≤ 원하는 수 (그 뒤는 `[추측]`). 솔직한 실패 `0x776` | 상태 0 · **OXID ≠ 0** · DSA NULL |

- 메뉴 7 의 "원하는 수" = `0x14 − [아파트+0x30]` = 아파트 OID 버퍼 20 칸 가운데 빈 칸 수 `[정적]`.
- **같은 `+0xd8` 이지만 다른 상자다** `[정적]` (run 문서 맨 위 "정정 · 대조 (run152 때 대조)" (b)):
  - `CallTheResolver +0x94` · `RegisterOXIDAndOIDs +0xb1` 의 `+0xd8` = **`OXIDEntry+0xd8`** — 1 바이트, 등록 표시 (위 갈림길이 보는 칸).
  - 메뉴 7 의 더할 OID 수 `cOidsToAdd` 의 `+0xd8` = **`CComApartment+0xd8`** — 4 바이트, 더할 OID 수 (`CallTheResolver +0x33 MOV R15D,[RCX+0xd8]`, 여기선 `RCX` = `CComApartment`).
  - 비유: 같은 "`0xd8` 번 칸" 이라도 **동 사무소 장부** 의 그 칸과 **우리 집 수첩** 의 그 칸은 다른 칸이다.
  - 그래서 "6 먼저" 결론은 그대로다 — 갈림길은 `OXIDEntry` 쪽 칸만 본다.
- 메뉴 6 에 실려 가는 보안 DSA = 2.8 에서 만든 `gpsaSecurity` (lsapolicylookup 이 실패했으면 4 항목 기본 꼴 `{wNumEntries 4, wSecurityOffset 2, …}`).

### 2.10 Q284a ② — 문지기 콜백 `LocalInterfaceOnlySecCallback` `[정적]`

#### 새 말 — 보안 콜백

- 서버가 창구를 열 때 (`RpcServerRegisterIf3`) 함께 등록하는 **문지기 함수** 다. 전화가 오면 먼저 이 문지기가 "들여보낼까?" 를 정한다.
- 등록하는 곳: `StartObjectExporter +0xb65` (run 문서 4-2, sast/144 1-4).
- 반환 **0 = 통과, 5 = 거절.**

#### 문지기가 보는 것 (run 문서 4-2 표 줄임)

1. 전송이 **ncalrpc (4)** 인가 (`+0x56 I_RpcBindingInqTransportType`). 아니면 5.
2. 손님 신분증 (토큰) 을 얻을 수 있나 (`+0xb0 RpcImpersonateClient` · `+0xff OpenThreadToken` · …). 실패면 5.
3. **명부를 꺼낸다**: `gpDefaultAccessRestrictionsSD` (`0x1656b0`), 없으면 `gpNoRegAccessRestrictionsSD` (`0x1656d0`).
   - 이건 `Connect` 답장의 **`[4]` / `[3]` 과 같은 명부** 다 — 2.3 표의 다섯째 · 넷째.
4. DACL 이 없거나 NULL DACL 이면 무조건 통과 (결과서).
5. `+0x242 AccessCheckByType` 으로 손님이 얻는 권한 비트를 잰다.
6. 비트 **`0x2` (COM "실행 · 로컬")** 가 있으면 통과. 없으면 5.

- 그래서 진짜 명부라면 Everyone 줄 (`0x7` / `0x3`) 에 비트 `0x2` 가 있어 로컬 test.exe 꼴은 **통과** 다.
- 에뮬레이터는 rpcss **서버를 흉내내는** 쪽이라 이 문지기를 구현할 필요는 없다 (결과서).
- 7 번째 갈래 (인터페이스 `00000136-…` 일 때만 실행 제한 둘째 검사) 는 ILocalObjectExporter 엔 안 탄다 `[추측: RCX = 인터페이스 UUID]`.

### 2.11 Q284a ③ — "클래스 없음" 은 combase 가 안 만든다 `[정적]`

- `DelegateActivationToSCM +0x156 BindToSCMProxy` → 연결 (`+0x17 GetConnection`) → `+0x49 MakeSCMProxy` → `+0x12 MakeProxyHelper` (안 읽음 `[공백]`) · `+0x5f CoSetProxyBlanket`.
- 실제 활성화 부탁은 `DelegateActivationToSCM +0x75b GetSCM` → **`+0x789` 간접 호출** (ISCM 메서드).
- 돌아온 값을 **그대로** 올린다. 이 함수들 `.asm` 에 `0x8004…` "클래스 없음" 상수가 **없다** (결과서).
- 그래서 그 값은 **rpcss 활성화 서버가 정해 보낸다** `[공백: 서버 몸]`.
- 비유: COM 담당 건물은 관리실의 "그런 물건 없어요" 를 **받아 적어 전할 뿐** 이다. 그 말을 정하는 건 관리실이다.

### 2.12 ★ 무엇이 정정됐나 — run149 해설서 맨 위 두 줄

run149 문서 · 해설서 맨 위에 "**정정 · 고쳐 읽기 (run151)**" 두 줄씩 들어갔다. 원문은 그대로 두고 (그래서 줄 번호가 4 씩 밀림) 맨 위에서 고쳐 읽게 했다.

#### (a) SD 크기 0 — "0 ×5 · 0 · NULL" 은 틀렸다

| | run149 (전) | run151 (후) |
|---|---|---|
| 칸 28 ~ 30 가름 | "(라) 0 이어도 지나감 → 0 ×5 · 0 · NULL" | (라) 는 combase `InitializeSystemSecurityDescriptors` **안에서만** 맞다 |
| 최소 대답 | SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL | 칸 28 = **`{124, 124, 232, 228, 0}`** · 29 = **708** · 30 = 자기 상대 SD 넷 이어 붙임 (바이트 `[추측: 계산]`) |

- 고친 곳 (run149 해설): 2.8 표 칸 28 ~ 30 줄 (원래 `:436`, 지금 `:440`) · "PE_emulator 쪽" 최소 칸 3 (원래 `:638`, 지금 `:642`).
- 왜 틀렸나 — 단계로:
  1. run149 는 답장을 받아 두는 combase `InitializeSystemSecurityDescriptors` 까지 봤다. 거기선 크기 0 이어도 **지나간다** — 이건 맞았다.
  2. 그런데 그 **뒤** 에 `CoInitializeSecurity +0x385` 가 다섯째 (없으면 넷째) 를 다시 꺼내 길이를 잰다. run149 는 여기를 안 봤다.
  3. 넷째가 0 B 덩어리면 길이 함수가 밖을 읽다 넘어진다 (2.6).
- 비유: run149 는 "봉투를 **받는 창구** 는 빈 봉투도 받아 준다" 까지 봤다. 그 봉투를 **뒤에서 펼쳐 보는 사람** 이 있다는 걸 이번에 찾았다.

#### (b) 메뉴 차례 — "7 → 6" 은 반대다

| | run149 (전) | run151 (후) |
|---|---|---|
| 첫 COM 사용 | 메뉴 7 → 6 | **6 먼저** (아파트 미등록), 7 은 등록 뒤 OID 를 더 받을 때 (첫 때 0 인 것은 `[추측]`) |
| 그다음 벽 | 메뉴 7 | **메뉴 6** (`StartListen` 먼저) |

- 고친 곳 (run149 해설): `:114` · `:208` 나무 · `:590` · `:619` 비유 · `:654` · `:730` · `:735` (지금 4 씩 밀림).
- **틀린 건 "차례" 말뿐이다.** 나무의 call 자리 (`+0x106` = 7 · `+0x1b7` = 6) 는 그대로 맞다 (run 문서 "정정").
- 왜 그렇게 읽혔을까 `[추측]`: `+0x106` (7) 이 `+0x1b7` (6) 보다 **앞 줄** 이라, 위에서 아래로 읽으면 7 → 6 으로 보인다. 그 사이 `+0x9d JZ` 가 6 쪽으로 **먼저 건너뛰는** 갈림길이라는 걸 이번에 짚었다. (이 이유는 이 글의 짐작이다 — run 문서는 "차례 말이 틀렸다" 까지만 적었다.)

### 2.13 PE_emulator 쪽 — 칸 28 ~ 30 · 금지 꼴 · 다음 벽 (run 문서 "PE_emulator 쪽")

flow-writer 는 원문 380 ② 절 · "받을 꼴" 만 읽었다. `rpcss.cpp` · `alpc_frontend.cpp` 는 안 읽었다. 고칠지는 사용자 · PE_emulator 결정이다.

#### 칸 28 ~ 30 에 쓸 값

| 칸 | 값 |
|---|---|
| 28 `ulong[5]` | **`{124, 124, 232, 228, 0}`** — 다섯째 0 = "정책 · 등록값 없음", 진짜에서도 나는 꼴 `[정적]` |
| 29 총 크기 | **708** (다섯째 0) / 936 (다섯째 228) `[추측: 계산 — 합]` |
| 30 덩어리 | run 문서 1-1 의 `[0][1][2][3]` 16진을 **패딩 없이** 이어 붙인 자기 상대 SD 넷 `[추측: 계산]` |

- 다섯째를 0 으로 권하는 이유: combase 는 다섯째가 없으면 넷째를 쓴다. VM 의 `MachineAccessRestriction` 이 `[3]` 과 같다면 결과도 같다 `[추측: 바이트 같음은 VM]`.

#### 금지 꼴 (넘어지거나 실패 HRESULT 가 나는 답장)

1. `[3]` 크기 0 + `[4]` 도 0 — 지금 "0 ×5" 꼴. 2.6 그대로 접근 위반.
2. 명부 안 모양이 틀림 — 오프셋이 조각 밖, 또는 **절대 형식** (비트 15 꺼짐) 이면 칸 값을 **주소로** 따라가 접근 위반. → **자기 상대로 줄 것.**
3. 크기 합 > 총 크기 · 덩어리 (run149 금지 5 그대로).
4. `[4]` 가 비-NULL 인데 쓰레기 — `[3]` 대신 이게 쓰여 같은 사슬로 죽음. 모르면 `[4]` = 0.

#### 다음 벽

- 첫 COM 사용의 메뉴 **6** (`StartListen` 먼저) → `상태 0 · OXID ≠ 0 · DSA NULL`. 그 뒤 7.
- 몸을 짓기 전엔 솔직한 실패 (`0x6ba` — run149 권고, 7 이면 `0x776` 도 가능).

### 2.14 flow-writer 대조 — 결과서와 다르게 적은 곳 (뜻은 같음)

run 문서 "flow-writer 대조" 에서 대표만. **결론 (출처 · 사슬 · lsapolicylookup · 6 먼저) 을 바꾸는 어긋남은 없다.**

- 점프 목적지 다섯: 결과서 `+0xeb8e` · `+0xefad` · `+0xee4f` · `+0xec38` · `+0xf0fb` 는 **주소 끝자리**. 함수 기준으로 `+0x30e` · `+0x72d` · `+0x5cf` · `+0x3b8` · `+0x87b`.
- `GetDefaultAccessRestrictionsSD` 의 `HeapAlloc` call: 결과서 `+0x38` → **`+0x46`** (`+0x38` 은 `g_hHeap` 읽는 줄).
- lsapolicylookup 실패 갈래: 결과서 "`+0xa79` 조각" 은 명령 경계가 아님 → `+0xa97 HeapAlloc` · `+0xac2 JMP +0x306`.
- call 자리 다섯 (`+0x5b` · `+0x56` · `+0xb0` · `+0x12` · `+0x75b`) 은 결과서보다 몇 바이트 뒤 — 결과서는 그 앞 `LEA` 등을 짚었다.
- SD 16진: flow-writer 가 따로 계산해 **바이트까지 같음** — 그래도 같은 규칙이라 `[추측: 계산]` 유지 (2.5).
- 뒤에 더한 대조 (run 문서 맨 위 "정정 · 대조 (run152 때 대조)"): combase `InitializeSystemSecurityDescriptors` 오프셋 세 벌은 **다 맞고 뜻이 다름** (2.6), `+0xd8` 은 **두 상자의 다른 칸** (2.9). 결론은 안 바뀜.

### 2.15 정리 — 비유로 한 번 더

- COM 관리실은 문을 열 때 **기본 출입 명부 넷** 을 인쇄해 둔다. 원본은 관리실 안 메모 (SDDL 글) 라서 **어느 동네나 똑같다.**
- **다섯째 명부** 는 건물주가 따로 써 둔 것 (레지스트리). 없으면 안 보낸다.
- `Connect` 답장 끝에 이 명부들을 **쪽수를 적어 봉투에** 넣어 보낸다.
- COM 담당 건물은 답장을 받아 둘 때 쪽수가 0 이어도 **빈 봉투를 하나씩 만들어** 서랍에 넣는다 (다섯째만 빼고).
- 나중에 접수할 때 "다섯째 명부 있나? 없네, 그럼 넷째" 하고 꺼내서, **펼쳐 보지도 않고 (검사 없이) 몇 쪽인지부터 센다.**
- 빈 봉투면 봉투 밖 종이를 세다가 넘어진다. 그게 BG05 였다.
- 그 사이에 이름 안내 데스크 (lsapolicylookup) 가 문을 닫았던 건 **상관없었다** — 이름 없이도 접수는 계속된다.
- 고칠 길: 봉투에 **진짜 명부 넷** (계산한 바이트 — 아직 VM 대조 전) 을 넣어 보낸다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 9b1a75e -- runs/03_sprint/flow.md`)

- **그림은 안 바뀌었다.** 이번 길은 test.exe 초기화 밖이고 다른 가게 (rpcss) 와 COM 손님 쪽 이야기다.
- **기준 절**:
  - 전: sechost 줄 없음.
  - 후: **sechost.pdb 줄 새로** (`0E8BAC436697447988FB72C706D1F1631`, 처음 analyze). run151 근거 줄 하나 — SD 16진 · 크기는 `[추측: 계산]` 이라고 못 박았다.
- **3장 "COM 이 RPCSS 에 거는 첫 전화" 절 (run149)**:
  - 제목 — 전: "… `CallTheResolver` (프로시저 7 → 6)". 후: 같은 자리에 "**run151 고쳐 읽기: 첫 부름은 6 먼저**" 가 붙고, `LookupPrincName` → lsapolicylookup · `GetDefaultAccessRestrictionsSD` → `RtlLengthSecurityDescriptor` 이름이 더해졌다.
  - 나무 한 줄 — 후: `CallTheResolver` 줄 밑에 "(run151) 첫 부름은 +0x9d JZ … (6) 먼저, 7 은 등록 뒤".
  - 글머리 넷 + 작은 나무 하나 — 전: 없음. 후: **SD 다섯 조각의 출처** `[정적]` · **SD 를 읽다 죽는 사슬** `[정적]` · **lsapolicylookup** `[정적]` · **"7 → 6" 고쳐 읽기** `[정적]`, 그리고 `CoInitializeSecurity` 작은 나무 (2.1 아래 덩어리와 같은 모양).
- **4장 (남은 것 표)**:
  - run149 줄 셋에 표시가 붙었다 — 동적 줄엔 "D5 뒤 · D8 에 읽기만 얹기 (run151)", 정적 줄엔 "**부분 해결 — 1 · 2 · 3 → run151**, 4 · 5 · 6 → Q284b", PE_emulator 줄엔 "**run151 정정 둘**".
  - run151 줄 셋이 새로 — 해결됨 · 남은 것 · PE_emulator 쪽.
- **5장 (바퀴 기록)**: run151 두 줄.
- 같은 커밋에서 run149 문서 · 해설서 맨 위에 고쳐 읽기 두 줄씩 (2.12).

### 큰 그림에서 어떻게 이어지나

- run136 → run149 → **run151** 로, COM 을 쓰는 프로그램의 `CoInitializeSecurity` 한 번이 처음부터 끝까지 이어졌다.
  - run136: 주소 글 (창 스테이션 · 데스크톱) 만들기.
  - run149: COM 관리실에 첫 전화 · `Connect` 답장 칸 25.
  - **run151**: 답장 끝 명부 다섯의 **출처** 와, 그걸 **뒤에서 꺼내 쓰는 곳** · lsapolicylookup 곁길. 진짜 명부면 HRESULT 0 으로 끝까지 간다.
- PE_emulator 쪽에선 이번 값으로 `Connect` 가 서면, 그다음 벽은 **첫 COM 사용의 메뉴 6** 이다 (2.9).
- 짐 지도 (`baggage.md`) 는 안 바뀐다 — 새 짐 칸 없음 (run 문서 "짐 지도 반영 거리").

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q283** 에 "읽기만" 얹기 (동적, VM 묶음 — 대기)

- 새 번호는 안 만들었다. 필수도 아니다 — **계산값 대조용** 이다.
- 멈춤점은 test.exe 가 아니라 COM 을 쓰는 프로세스 · svchost rpcss 조건.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 넷째 228 B 의 **진짜 바이트** (2.5 계산값과 같은가) · 다섯째가 있나 (NULL / 228) | 정적 = 글 + 규칙으로 계산만 함 | D5 뒤 `combase!CoInitializeSecurity+0x385` 직전 — `_pNoRegAccessRestrictionsSD` · `_pDefaultAccessRestrictionsSD` 덤프 |
| 크기 칸 다섯 · 총 크기 (`{124,124,232,228,?}` · 708 / 936) | 같음 | D8 `rpcss!_Connect+0x5ce` 루프 앞 |
| (선택) 진짜 VM 에서 `LookupAccountSidLocalW` 가 성공하나 | 실행 때 값 | `combase!LookupPrincName+0x116` 반환 · `CoInitializeSecurity+0x2b4` 뒤 `eax` |
| (선택) 첫 부름이 정말 6 인가 | `OXIDEntry` 생성자를 안 읽어 `[추측]` | `combase!CComApartment::CallTheResolver+0x9d` |

- 디버거 메모 하나 `[추측]`: `CoInitializeSecurity` 의 실패 갈래들은 "중단 요청" 훅 (`BreakOnCoInitializeSecurityRuntimeFailureIfRequested`) 을 지난다. D5 를 걸 때 이 훅이 켜져 있으면 멈춤이 하나 더 생길 수 있다.

### 디버거 없이 VM 에서 떠 올 것 → 380 묶음 **2 단계 VM 수집 스크립트** 재료

- `reg export HKLM\SOFTWARE\Microsoft\OLE` — 레지스트리 값 바이트가 NoReg 와 같은지 (2.4), `DefaultAccessPermission` 이 정말 없는지, `…Old` 흔적, `AppCompat` 하위 키.
- `reg query "HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DCOM" /s` — 정책 키가 있나.
- AppID `test.exe` (HKLM · HKCU `SOFTWARE\Classes\AppID`) — "AppID 없음" 이 진짜에서도 같은가.
- `wmic os get producttype` (1 = 워크스테이션 → AN `0x3`) · `whoami /user`.

### 정적으로 남은 것 → **Q294** · **Q284b** (`_workspace/plan.md`, 대기)

- **Q294** (run151 남은 정적):
  - rpcss `ISCMLocalActivator` 서버 몸 — "클래스 없음" HRESULT 를 정하는 곳 (2.11 `[공백]`).
  - combase `GetLegacyBlanket` 의 AppID **있는** 길 · `CallAccessCheck` · `CalculateAnonymous…` 안 syscall.
  - `MakeProxyHelper` — 어떤 DSA 면 프록시가 서나.
  - 암묵 길 lsapolicylookup 실패의 정확한 HRESULT.
  - `OXIDEntry+0xd8` 첫 값 (6 먼저의 `[추측]` 닫기).
- **Q284b**: rpcss 메뉴 10 · 1 몸 · ole32 `CoGetObject` · rpcrt4 `PickAssociation`. lsapolicylookup **"8 번"** 의 나머지 (rpcrt4 `ncalrpc` 재시도) 도 여기 rpcrt4 몫과 겹친다.

### 대기열 밖 — 이름 · 짐작으로 남은 것

- api-set → KernelBase 연결 `[추측: 일반 규칙]` (2.2).
- 크기 0 `LocalAlloc` 이 NULL 이 아니라는 것 `[추측: 일반 동작]` (2.6).
- `+0x0` 이 아니라 `+0x2d` 에서 죽은 이유 `[추측: 원문 rip · 주소에서]` (2.6).
- lsapolicylookup 서버가 lsass 라는 것 `[추측]` (2.8).
- `LocalInterfaceOnlySecCallback` 의 RCX = 인터페이스 UUID `[추측]` (2.10).
- 원문 로그 줄 번호 (`1306 · 1307` · `1310 ~ 1313` …) 와 코드 줄 맞추기 — 로그가 지워져 직접 못 봄.

---

## 5. 더 읽기

- run 문서: [`run151-rpcss-connect-sd.md`](run151-rpcss-connect-sd.md)
  - "결과" 1절 (다섯 조각 표) · 1-1 (16진) · 2절 (죽는 사슬) · 2-1 (암묵 길) · 3절 (lsapolicylookup) · 4절 (Q284a) · "호출 관계" · "PE_emulator 쪽" · "flow-writer 대조" · "정정"
- 요청서: [`_workspace/sast/148_rpcss-connect-sd.md`](../../_workspace/sast/148_rpcss-connect-sd.md)
- 결과서: [`_workspace/sast/148_rpcss-connect-sd_result.md`](../../_workspace/sast/148_rpcss-connect-sd_result.md)
  - "먼저 읽는 한 줄 답" · 1절 SD 조각 표 · 2-2 금지 꼴 · 2-3 암묵 길 · "VM 으로 확인할 것" 두 표
- flow.md: [`flow.md`](flow.md) 3장 "COM 이 RPCSS 에 거는 첫 전화" 절 (run151 글머리 · 작은 나무), 4장 run149 · run151 줄, 5장 run151 줄
- 앞 해설:
  - [run149 해설](run149-com-rpcss-해설.md) — 맨 위 고쳐 읽기 두 줄 · 2.8 칸 25 · 2.12 첫 COM 사용
  - [run97 해설](run97-rpc-sd-query-해설.md) — 2.0 출입 명부 · 2.5 한 덩어리 꼴
  - [run136 해설](run136-winsta-name-해설.md) — `CoInitializeSecurity` · `GetConnection` 앞쪽
  - [run135 해설](run135-lsa-sspi-해설.md) — LSA 의 다른 창구
