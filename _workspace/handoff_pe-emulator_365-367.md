# PE_emulator 에 넘길 요약 — 365 · 367 요청 넷의 답

- 쓴 날: 2026-11-26 (windows-pe-internals 오케스트레이터)
- 이 파일은 **길잡이**다. 근거 · 오프셋 · 표는 run 문서의 "PE_emulator 쪽" 절에 있다 (아래 경로). 이 파일과 run 문서가 다르면 run 문서가 맞다.
- 표시: `[정적]` 디컴파일 코드 · `[관찰]` 디버거 · `[추측]` · `[공백]`. "지금 우리" = PE_emulator. 넷 다 **정적만** — VM 확인거리는 아래 "남은 것" 의 대기열 번호.
- 범위: 365 epm-endpoint 는 **물음 1 만** (사용자 결정). 365 dialog-window 는 물음 1 · 2 → run146, 물음 3 → run148 로 나눔. 367 둘은 원문이 당겨 달라던 몫 (Q257 ① · Q278 N1) 만.

| 요청 (원문 `../PE_emulator/_workspace/05_tuning_planner/`) | 우리 Q | 답한 run (최종 근거) | 정적 결과서 | 해설 |
|---|---|---|---|---|
| `365_internals-epm-endpoint.md` (물음 1) | Q279 | `runs/03_sprint/run145-epm-endpoint.md` | `_workspace/sast/143_epm-endpoint_result.md` | `run145-epm-endpoint-해설.md` |
| `365_internals-dialog-window.md` (물음 1 · 2) | Q274a | `runs/03_sprint/run146-dialog-window.md` | `_workspace/sast/142_dialog-window_result.md` | `run146-dialog-window-해설.md` |
| `365_internals-dialog-window.md` (물음 3) | Q274a | `runs/03_sprint/run148-dialog-end.md` | `_workspace/sast/146_dialog-end_result.md` | `run148-dialog-end-해설.md` |
| `367_internals-gdi-display-dc.md` | Q278a | `runs/03_sprint/run147-gdi-display-dc.md` | `_workspace/sast/145_gdi-display-dc_result.md` | `run147-gdi-display-dc-해설.md` |
| `367_internals-com-rpcss.md` | Q257a | `runs/03_sprint/run149-com-rpcss.md` | `_workspace/sast/144_com-rpcss_result.md` | `run149-com-rpcss-해설.md` |

- 결과서와 run 문서가 다른 오프셋은 run 문서 "flow-writer 대조" 절이 고친 값이다.

---

## Q279 — epmapper `ept_map` 이 돌려주는 답 (365 epm-endpoint 물음 1) — run145

### 답

- **고르기** `[정적]`: 프로시저 3 · 7 · 8 은 한 몸. 인터페이스 UUID 같음 + 주 버전 같음 + **장부 부 버전 ≥ 질문 부 버전** + 장부 객체 UUID nil 이면 무엇이든 + 질문 3층 `0x0c` 이면 protseq "ncalrpc" 인 탑만. **전송 문법 · 4층 끝점은 안 본다.**
- **돌려주는 탑**: 등록 때 받은 바이트 **그대로 복사** (질문에서 안 가져옴). `max_towers` 까지 채움. nsi 하나면 탑 88 B · 응답 스텁 **140 B**.
- **맞는 게 없을 때**: 상태 `0x16c9a0d6` · 탑 0 · 핸들 0 · 배열 `max_towers · 0 · 0` = 40 B.
- **부른 쪽 검사**: 클라이언트 SID 인자가 NULL 이면 없음.
- **등록**: nsisvc 가 서비스 시작 때 `RpcEpRegisterW` → 로컬 인터페이스 `0b0a6584` v1.1 프로시저 2 (`ept_insert_ex`). 4층 이름 = 동적 끝점 `LRPC-` + 18자 `[추측: 마지막 연결]`.

### 지금 우리 맞나 · 틀리나

- `epm.cpp:24 ~ :27` 늘 40 B — **"없음" 답과 바이트까지 같다 (맞음).** `:23` `TODO(VM)` 은 정적으로 닫힘.
- **틀림 — 이게 벽**: `:21` 장부가 늘 비어 있음. 진짜는 nsi 가 미리 등록돼 있음 → epmapper 창구가 뜰 때 **nsi 항목 하나를 미리 채우면** 된다. 장부 항목 꼴 · 고르기 · 응답 140 B 칸 · 탑 바이트는 run145 "PE_emulator 쪽" 1 ~ 5.
- 끝점 이름은 우리 nsi 창구 포트 이름과 같아야 한다 (곧바로 둘째 연결이 그 이름으로 옴).

### 남은 것

- VM: nsisvc svchost 의 ncalrpc 주소 개수 · 진짜 탑 바이트 · 트리거 키 → **Q280** (D2 `RpcEpMap!ept_map_auth_sd+0x2c7` · D4 `IFObjList` 덤프 필수 — svchost RPCSS 프로세스 조건). 원문 물음 2 · 3 도 Q280.
- 정적: `ept_insert_ex` NDR · `I_RpcServerCheckClientRestriction` ncalrpc 갈래 · 4층 이름 마지막 연결 · −1 핸들 마샬링 → **Q281**.

---

## Q274a — 대화상자 창이 서는 길 (365 dialog-window 물음 1 · 2) — run146

### 답

- **지금 벽** `[정적]`: `NtUserGetAtomName(0x8002)` 진짜 답 = **반환 6**, 버퍼에 `L"#32770"` + NUL (정수 원자 `< 0xc000` 은 표 없이 `#%u`). `Length` 는 커널이 안 씀 (user32 가 `반환×2` 로 씀).
- 대화상자 창 `NtUserCreateWindowEx` 는 클래스 `0x8002` 그대로 (둘째 · 셋째 인자). 돌아온 뒤 `ValidateHwnd` · `ValidateDialogPwnd` 가 0 이면 NULL. `NtUserSetDialogPointer` 반환은 안 봄.
- **사본 `+0x128` 여분 블록은 커널이 되부름 `0x7b` 로 받아 적는다** → stub 이 0 채운 블록을 대신 적으면 **창 프로시저 되부름은 필요 없다** (안 적으면 `InternalCreateDialog +0x6ab` 접근 위반, 블록은 있는데 `pdlg` 0 이면 `+0x6af`).
- 컨트롤마다 `NtUserCreateWindowEx` 한 번, 하나라도 NULL 이면 전체 NULL (`DS_NOFAILCREATE` 빼고).
- `SendMessageWorker` 가 syscall 없이 창 프로시저를 부르는 조건 · 핸들 항목 (`hwnd & 0xffff` 번호 · `(hwnd>>16) & 0x7fff` 고유 수) · 창 사본 칸 표 · NULL 가지 전부 → run146 표.

### 지금 우리 맞나 · 틀리나

- `nt_user_get_atom_name.cpp` — **틀림**: 정수 원자에 `#<십진>` 을 쓰고 글자 수 반환.
- `nt_user_create_window_ex.cpp` — **틀림 둘**: `0x8002` 를 받아야 하고, **hwnd 마다 다른 값** + 항목 · 사본 · 여분 블록.
- `csr_user_connect_stub.cpp` — 핸들 표 블록 칸 (`gSharedInfo+0x8` · `+0x10` · `psi+0x8`) 필요.
- 새 `NtUserSetDialogPointer` — 반환은 아무 값이지만 **몸 필요**: 여분 블록 `+8 = pdlg` · 사본 `+0x2a` 0 이면 `0x2a4` · 사본 `+0x12 |= 1`.
- `ShowWindow` · `UpdateWindow` · `SetFocus` 류 — 반환 안 봄 → 기본 대답 0 이면 됨.
- **stub 최소 목록 1 ~ 6** → run146 "PE_emulator 쪽".

---

## Q274a — 기본 단추 한 번으로 끝 (365 dialog-window 물음 3) — run148

### 답

- 진짜 Enter 는 `WM_KEYDOWN(VK_RETURN)` 으로 오고 `IsDialogMessageW` 가 `SendMessageW(WM_COMMAND, ID)` 로 바꿈. **기본 단추 ID = `pdlg+0x20` 아랫 16비트** (`EndDialog` 결과값과 같은 칸, 처음 값 1).
- `WM_COMMAND` 를 대화상자 hwnd 앞으로 곧장 주면 앱 프로시저까지 **win32k syscall 0번** (조건: 사본 `+0x12` 비트 2 = 0 · `+0x2a` ∈ {0, `0x2a4`} · TEB `+0x820`→`+0x10` / `+0x838` = 0).
- 모달 고리 (`DialogBox2`) 는 `NtUserPeekMessage` 의 **0 / 0 아님만** 본다. 끝 syscall (`SetWindowPos` · `PostMessage` · `DestroyWindow`) 반환은 안 봄. `DialogBoxParamW` 반환 = `pdlg+0x20`.
- **`DefDlgProcWorker` 는 사본 `+0x2a` 가 0 도 `0x2a4` 도 아니면 앱 프로시저를 안 부른다** → `NtUserSetDialogPointer` 의 `0x2a4` 가 필수.
- A 판 · `CreateDialogParam*` 은 끝 길이 같거나 고리가 없음 (run148 4절).

### 지금 우리 맞나 · 틀리나

- **틀림 — 금지 꼴**: `NtUserPeekMessage` · `NtUserWaitMessage` 몸 없음 → 기본 대답 0 = **끝없는 바쁜 고리**.
- **"기본 단추 누름" 을 넣는 가장 짧은 곳 = `NtUserPeekMessage` 첫 출력 한 번**: 반환 1 + MSG `{대화상자 hwnd, 0x111, LOWORD(pdlg+0x20) (0 이면 1), 그 ID 컨트롤 hwnd 또는 0, 나머지 0}` → 그 대화상자의 syscall 은 넷 (`PeekMessage` · `SetWindowPos` · `PostMessage` · `DestroyWindow`).
- **틀림**: `NtUserSetWindowLongPtr` (`0x15da`) 기본 대답 0 — 앱이 `DWLP_USER` 에 넣은 포인터를 뒤에서 NULL 로 읽음. `[[사본+0x128]+인덱스] = 값`, 반환 = 옛 값.
- `nt_user_get_message.cpp` — `:5` 는 맞음, `:6 ~ :7` 주석 근거는 고칠 거리 (`DialogBox2` 는 `NtUserGetMessage` 를 안 부름), `:22` `0x10010` 조건은 run146 hwnd 규칙과 같이 바뀌어야 함.
- `NtUserDestroyWindow` 는 모달 끝에서 **늘** 불림 (run146 의 "실패 때만" 은 고쳐 읽기). 기본 대답 0 은 맞음.
- **stub 최소 목록 7 ~ 15** (run146 1 ~ 6 에 이어) → run148 "PE_emulator 쪽". 되부름은 끝 길에도 필요 없음.
- **조심**: 컨트롤 프로시저를 안 부르면 `pdlg+0x20` 은 처음 값 1 그대로 → `MessageBox(MB_YESNO)` 같은 판은 기본 ID 가 1 이 아니라 틀린 답 · 고리에 남을 위험 (Q282).
- **정정**: run142 의 "WM_QUIT → `MessageBoxW` 0" 은 틀림 — 처음 값 1 을 그대로 돌려줌.

### 남은 것 (두 run 합침)

- VM: run146 동적 1 ~ 10 · run148 동적 1 ~ 12 → **Q273** (VM GUI 시험 프로그램 — 사용자 결정).
- 정적: 자식 연결 순서 · ANSI 비트 커널 쪽 · 비트표 채우는 곳 · **`SoftModalMessageBox`/`MB_AddPushButtons` 기본 단추 ID** · `g_systemCallFilterId` → **Q282**. TEB `+0x820` 채우기 · 지표 · `LoadIcoCur` → Q274b (그대로).

---

## Q278a — 화면 DC 하나가 서는 길 (367 gdi-display-dc) — run147

### 답

- `CreateICA("DISPLAY")` = `NtGdiOpenDCW(0, 0, 0, 2, 1, 0, 0, 0, &p)` (4째 `2` = IC 표시, `CreateDCA` 는 4째 0. 5째 `1` = 화면 표시). **9째 출력은 화면 길에서 안 읽음.** "DISPLAY" 면 프린터 길 전체를 건너뜀.
- 화면 길의 NULL 은 **`NtGdiOpenDCW` 반환 0 하나뿐.** syscall 뒤 유저 쪽은 반환 0 인가 · 종류가 `0x10000` 인가만 본다.
- HDC 종류 비트 `0x10000` (IC · DC 같음). 표 칸 검사는 run144 3절 그대로.
- `DC_ATTR` 은 `HmgAllocateDcAttr` 의 **0x170 B** (유저가 읽는 끝 `+0x128`). `+0x0` 비트 1 = "주 화면 위 DC" (0 이어도 됨 — 그러면 지표가 전부 syscall). `+0x98` 처음 값 `0x12001f`. **`+0x90` · `+0xf0` · `+0xf8` · `+0x108` 은 0 이어야** (아니면 ICM 길).
- `GetDeviceCaps(0x58 / 0x5a)` 는 **늘 `NtGdiGetDeviceCaps` syscall**. 유저 쪽은 값을 안 검사 (≤ 0 이면 96.0). 커널은 DPI 가상화를 거침 (96 고정 아님).
- gdiplus 차례 5 ~ 7 에서 HDC 답에 달린 NULL 은 `+0x296` (`CreateICA` 0) · `+0x341` (`NtGdiCreateCompatibleDC` 0, 또는 `TECHNOLOGY == 1` 인데 `BITSPIXEL ≤ 8`) 둘뿐. 나머지는 할당뿐.

### 지금 우리 맞나 · 틀리나

- **`NtGdiOpenDCW` ≠ 0 과 `NtGdiCreateCompatibleDC` ≠ 0 만 있으면 `GdiplusStartup` 은 `+0x5e3` 까지 통과** (표 칸 · `DC_ATTR` 없어도).
- `nt_user_get_dc.cpp:20` `0x21000` — **틀림 (종류 비트 2)**: `GetDeviceCaps` 가 표 앞에서 갈라져 0 + **LastError 6** (362 샘플의 길). `번호 | 0x10000` + 표 칸 + `DC_ATTR` 필요.
- `nt_gdi_create_compatible_dc.cpp:21` `0x20c00` — gdiplus 는 지금도 통과, 꼴은 틀림 (그 DC 로 하는 다음 일에서 같은 실패).
- 새로 만들 것: `NtGdiOpenDCW` · `NtGdiGetDeviceCaps` (최소 지표 값 표) · `NtUserGetDCEx` · `NtUserGetWindowDC` (`NtUserGetDC` 와 같은 꼴).
- **stub 최소 목록 8 ~ 16** (run144 1 ~ 7 에 이어) → run147 "PE_emulator 쪽".
- **그 뒤 벽**: `InternalGdiplusStartup +0x62b InitImagingLibrary` (N2).

### 남은 것

- VM: 표 칸 · `DC_ATTR` · `pGdiDevCaps` · 세션 기본 틀 덤프 → **Q277**.
- 정적: N2 `InitImagingLibrary` · N3 글 폭 (메시지 상자 12) · 세션 기본 `DC_ATTR` 틀 (스톡 핸들 값) · DPI 칸 쓰는 곳 → **Q278b**.

---

## Q257a — COM 이 RPCSS 에 거는 첫 전화 (367 com-rpcss) — run149

### 답

- 바인딩 글 `ncalrpc:[epmapper,Security=Impersonation Dynamic False]` — **`ept_map` 을 안 묻고 곧장** (끝점이 글에 박힘) `[정적]`.
- **`GetConnection` 한 번에 bind 가 넷** (모두 동기, NDR 2.0 전용): ILocalObjectExporter `e60c73e6-88f9-11cf-9af1-0020af6e72f4` **v2.0** (PDB 이름으로 확정) · IMachineActivatorControl `c6f3ee72-…` v1.0 · IROT `b9e79e60-…` v0.2 · ISCM `412f241e-…` v0.2. 하나라도 거절하면 `0x800706b5`.
- 받는 쪽은 `rpcss.dll` — RpcEpMap 과 같은 `epmapper` 포트를 나눠 씀.
- 첫 프로시저 = ILocalObjectExporter **0 `Connect`** (입력 6 · 출력 25). combase 는 상태 0 이어도 컨텍스트 핸들 · DSA · `gdwScmProcessID` · `_ProcessSignature` 중 하나가 0 / NULL 이면 `0x8000ffff`.
- **`RetryRPC` 는 `0x6bb` 일 때만 100 ms 뒤 다시 걸고 횟수 제한이 없다.** bind 단계 fault 는 `RetryRPC` 에 안 닿음.
- `CoInitializeSecurity` 를 직접 안 불러도 첫 COM 사용 (`CoCreateInstance`, in-proc 이어도) 이 같은 연결을 만들고, 이어 프로시저 7 → 6 을 보낸다.

### 지금 우리 맞나 · 틀리나

- `lrpc.cpp:74 ~ :77` bind fault `0x6b5` — **맞음 · 안전한 정직한 실패** (한 번에 끝남).
- `lrpc.cpp:79 ~ :80` 문법 마스크 1 — 맞음.
- `lrpc.cpp:102 ~ :103` — **틀려질 예정**: 프로시저를 `opnum` 만으로 찾음. 같은 포트에 인터페이스를 늘리면 번호가 겹친다 (EPM 8 vs ILocalObjectExporter 8) → **(인터페이스 UUID, 번호)** 로 찾아야 함.
- 성공시키려면 **bind 넷 + `Connect` 응답 최소 칸을 한꺼번에** (run149 "PE_emulator 쪽" 1 ~ 5).
- **금지 꼴 8개** (첫째: fault / 반환 `0x6bb` = 끝없이 다시 걺) → run149. 몸이 없는 다음 프로시저는 **`0x6ba`** 로 끝내기를 권함.
- **그다음 벽**: 첫 COM 사용의 프로시저 7 (OID 할당) → 6 (OXID 등록).

### 남은 것

- VM: bind 넷이 정말 한 연결인지 · 진짜 bind 답 · `Connect` out 값 · 응답 바이트 → **Q283** (멈춤점 `combase+0x18c1ee` / `+0x18c1f3`).
- 정적: rpcss 프로시저 7 · 6 · 10 · 1 몸 (다음 벽) · `LocalInterfaceOnlySecCallback` · 활성화 "클래스 없음" 길 · ole32 `CoGetObject` → **Q284**. 창 스테이션 커널 본문 · `E_ACCESSDENIED` 갈래 → Q257b (그대로).
