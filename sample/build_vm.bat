@echo off
rem Run in "x64 Native Tools Command Prompt" on the target VM.
rem VM trip tools: winhttp_once.exe (RPC/network bundle) and vm_probe.exe (vm-collect "no debugger" probes).
rem test.exe build (build.bat) is untouched.
if /i not "%VSCMD_ARG_TGT_ARCH%"=="x64" (
    echo [!] Run this in the x64 Native Tools Command Prompt.
    exit /b 1
)

cd /d %~dp0
if not exist bin mkdir bin

set CFLAGS=/nologo /utf-8 /Z7 /Od /MT
set LFLAGS=/DEBUG /INCREMENTAL:NO

cl %CFLAGS% winhttp_once.c /Fe:bin\winhttp_once.exe /Fo:bin\ /link %LFLAGS% || exit /b 1
cl %CFLAGS% vm_probe.c     /Fe:bin\vm_probe.exe     /Fo:bin\ /link %LFLAGS% || exit /b 1

echo [+] Build OK: bin\winhttp_once.exe, bin\vm_probe.exe
