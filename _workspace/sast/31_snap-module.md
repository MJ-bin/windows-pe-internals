# 31 snap-module — 정적 분석 요청

- 날짜: 2026-10-24
- 스프린트: 02_sprint (예정 run36)
- 관련: 대기열 Q50 (단계 3 정적 묶음, 출처 sast/18 판단·sast/19), flow.md 2.5·2.6 `LdrpSnapModule` 상자 (그림 2 `ja4`·`jw5`·`jw9`·`jw11`, "본문은 Q50"), 4장 2.5 "import 주소를 쓰는 곳과 `State` 를 5 로 쓰는 곳"
- 이전 요청: sast/17 (`LdrpMapAndSnapDependency`, IAT 준비·`+0x4c2`), sast/18 (`LdrpLoadDependentModuleInternal`), sast/19 (`LdrpProcessWork +0x131`), sast/23 (TLS 길)

## 알고 싶은 것

1. **IAT 칸에 import 함수 주소를 적는 줄은 어디인가?** (`LdrpSnapModule +0x...` 오프셋, 쓰는 곳이 여러 곳이면 이름 import / 서수 import 별로)
   - 1a. 이름 → 주소 찾기를 어떤 함수로 하나? 지도에는 `LdrpGetProcedureAddress`·`LdrpResolveProcedureAddress`·`LdrpNameToOrdinal` 같은 helper 호출이 **없다**. 본문 안에서 직접(export 이름 표 이분 탐색, hint 먼저 보기 등) 하나? 간접 호출로 하나? (예/아니오 + 근거 줄)
   - 1b. 무엇을 기준으로 import 를 돌며 (`문맥+0x70/+0x78` IAT 범위, import 서술자), 한 서술자(DLL 하나)만 처리하나 모듈의 모든 서술자를 처리하나?
2. **`+0x4e5 strrchr` ~ `+0x5da LdrpLoadDependentModuleInternal` 갈래는 forwarder(export 가 `다른DLL.함수` 문자열) 처리인가?** (예/아니오)
   - 예라면: forwarder 대상 DLL 을 그 자리에서 올리나(`LdrpLoadDependentModuleInternal` 인자), `+0x94b RtlCharToInteger` 는 `DLL.#서수` 꼴 처리인가, forwarder 대상의 의존 간선(부모 카운터)과 엮이나.
   - 아니라면 그 갈래가 무엇인지 (지연 import? api-set?).
3. **노드 `State = 5`(Snapped) 를 쓰는 곳**과 그 앞뒤 (`+0xb61 LdrpDoPostSnapWork` 앞인가 뒤인가). 실패 때 State 는?
4. **`LdrpDoPostSnapWork` (rva `0xa4200`, 228B) 가 하는 일** — depth 0 으로 본문:
   - `+0x3e NtProtectVirtualMemory` = IAT 보호를 원래대로 되돌림 (`LdrpPrepareImportAddressTableForSnap` 이 쓰기 가능으로 바꾼 것의 짝)인가?
   - `+0x68 LdrControlFlowGuardEnforcedWithExportSuppression`·`+0xa4 LdrpUnsuppressAddressTakenIat` — CFG 쪽 한 줄.
   - `+0x8e LdrpHandleTlsData` — DLL 의 TLS 를 여기서 처리하나? 어떤 조건(exe 는 빼나 — exe 는 `LdrpInitializeTls`, run28)?
5. **부모와의 관계**: snap 이 끝난 뒤 이 함수(또는 `LdrpDoPostSnapWork`)가 부모 노드 카운터·State 를 건드리나? 아니면 snap 끝은 caller(`LdrpMapAndSnapDependency +0x4c2` / `LdrpProcessWork +0x131`) 쪽이 마무리하나? (예/아니오 + 근거 줄)
6. 작은 것 (이름·한 줄만): `+0x81 LdrpHandlePendingModuleReplaced`·`+0xfc LdrpFreeReplacedModule` 갈래 조건, `+0xd5d`/`+0xecd LdrpCheckRedirection`·`+0xe29`/`+0xe44 CompatCachepLookupCdb` 갈래 조건 (보통 test.exe 에서 탈 것 같은가), import 를 못 찾을 때 실패 코드 (`+0xe8f LdrpReportError` 근처, `STATUS_ENTRYPOINT_NOT_FOUND`/`ORDINAL_NOT_FOUND` 인가).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpSnapModule$` | 1 | 본체 — 질문 1~3·5·6 (`.asm` 이미 있음, 908줄) |
| ntdll | `^LdrpDoPostSnapWork$` | 0 | 질문 4 |

- depth 1 callee 는 **이름·인자·갈래 조건만**. `LdrpLoadDependentModuleInternal`(sast/18 읽음), `RtlpxLookupFunctionTable`, `RtlAddressInSectionTable`, 로그·오류 함수(`LdrpLog*`, `LdrpReportError`), `memset`, `__security_check_cookie` 본문은 읽지 않음.
- `LdrpHandleTlsData` 본문은 범위 밖 (run28 에서 일부 봄 — 들어가는 조건만).
- `+0x605 RtlDeleteBoundaryDescriptor` 는 지도에 "같은 주소 이름 9개 더 (이름을 믿지 말 것)" — 실제로 무슨 일(아마 힙 해제 계열 공용 껍데기)인지 한 줄, `pdb_aliases.py --where ntdll` 로.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpSnapModule` rva `0x3a8c0`, 4052B, 깊이 6, caller 2곳 (`LdrpMapAndSnapDependency +0x4bd/+0x4c2`, `LdrpProcessWork +0x12c/+0x131`), expanded.
- 직접 호출 36줄 + SEH 필터 1 (`LdrpSnapModule$filt$0 → LdrpGenericExceptionFilter`). 눈에 띄는 것:
  - 앞: `+0x79 LdrpLogDllState`, `+0x81 LdrpHandlePendingModuleReplaced`, `+0xfc LdrpFreeReplacedModule`, `+0x189 RtlImageNtHeaderEx`
  - 가운데: `+0x4e5 strrchr`, `+0x5a5 LdrpAppendAnsiStringToFilenameBuffer`, `+0x5da LdrpLoadDependentModuleInternal`, `+0x605 RtlDeleteBoundaryDescriptor`(별칭 주의), `+0x6b1 RtlImageNtHeaderEx`, `+0x82a`/`+0xab5 RtlpxLookupFunctionTable`, `+0x855 RtlFreeHeap`, `+0x94b RtlCharToInteger`
  - 끝: `+0xb61 LdrpDoPostSnapWork`, `+0xb7b LdrpLogDllState`, `+0xbbc LdrpLogError`, `+0xbcf __security_check_cookie`
  - cookie 뒤: `+0xcf7`/`+0xd2b RtlAddressInSectionTable`, `+0xd5d`/`+0xecd LdrpCheckRedirection`, `+0xe29`/`+0xe44 CompatCachepLookupCdb`, `+0xe8f LdrpReportError`, `+0xef5 NtUnmapViewOfSection`, `+0xf48`/`+0xf63 LdrpLogLoadFailureEtwEvent`
- 이름 → 주소 helper 호출이 지도에 없음 (위 질문 1a).
- `LdrpDoPostSnapWork` rva `0xa4200`, 228B, caller 1곳 (`LdrpSnapModule +0xb61`). 직접 호출: `+0x3e NtProtectVirtualMemory`, `+0x68 LdrControlFlowGuardEnforcedWithExportSuppression`, `+0x8e LdrpHandleTlsData`, `+0xa4 LdrpUnsuppressAddressTakenIat`, `+0xde LdrpLogInternal`.

## 이미 아는 것

- `[정적]` (run22, sast/17) `LdrpMapAndSnapDependency`: `+0x9e LdrpPrepareImportAddressTableForSnap(문맥)` 이 IAT 를 찾아 `문맥+0x70/+0x78` 에 두고 쓰기 가능으로 바꿈 (`NtProtectVirtualMemory`). import 마다 `LdrpLoadDependentModuleInternal`, 끝에 남은 자식 0 이면 노드 `State = 4`(Snapping) → `문맥+0x30`(부모 항목) == 0 이면 `+0x4c2 LdrpSnapModule(문맥)`, 아니면 `+0x4f7 LdrpQueueWork(문맥)`. `문맥+0x70 == 0`(IAT 없음) 이면 `State = 5` 만 쓰고 `LdrpSnapModule` 안 부름 (`+0x5ae`).
- `[정적]` (run24, sast/19) `LdrpProcessWork`: 노드 `State ≠ 0` 이면 `+0x131 LdrpSnapModule(ctx)`, 결과 음수면 `*(ctx+0x28)` 에 저장. 부모 snap 일감은 `LdrpSignalModuleMapped` 가 부모 `State = 4` + `LdrpQueueWork` 로 넣은 것.
- `[정적]` (run28) exe TLS 는 `LdrpInitializeTls`, DLL TLS 목록 항목은 `LdrpHandleTlsData +0x10b LdrpAllocateTlsEntry`.
- `[관찰]` (run7) 26100.9457 에서 test.exe 의 import 가 연결되어 `main` 까지 감 (IAT 쓰기 자체는 안 봄).
- `[추측]` (flow.md 2.5) `LdrpSnapModule` = "IAT 에 import 함수 주소를 적는 곳". `+0x5df` 갈래 = 지연 import 또는 forwarder.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x` 줄). 칸 이름은 PDB 타입(`_LDR_DATA_TABLE_ENTRY`, `_LDRP_LOAD_CONTEXT` 등 있으면)으로.
- `LdrpSnapModule` 의 큰 구간 표 (오프셋 범위 → 하는 일, 대표 호출) — 4KB 함수라 구간 5~8개 정도로.
- 대표 지점 1~3개 (그림 2 `LdrpSnapModule` 상자 아래에 넣을 것: IAT 쓰기, State 5, `LdrpDoPostSnapWork`).
- 동적으로 확인할 멈춤점 추천 (`ntdll+rva` 꼴, test.exe 만): 예) IAT 쓰기 줄에서 쓰는 값 `ln`, dll1 import 가 forwarder 갈래를 타나. 대기열의 기존 동적 항목(Q23 dll 명부 순서, Q47 일꾼 스레드)과 같은 실행에서 볼 수 있으면 그렇다고 적기.
