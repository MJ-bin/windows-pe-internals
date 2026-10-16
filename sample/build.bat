@echo off
rem Run in "x64 Native Tools Command Prompt".
rem Build from the bottom of the dependency chain: dll2 -> dll1 -> exe
if /i not "%VSCMD_ARG_TGT_ARCH%"=="x64" (
    echo [!] Run this in the x64 Native Tools Command Prompt.
    exit /b 1
)

cd /d %~dp0
if not exist bin mkdir bin

set CFLAGS=/nologo /utf-8 /Z7 /Od /MT
set LFLAGS=/DEBUG /INCREMENTAL:NO

cl %CFLAGS% /LD dll2.c /Fe:bin\dll2.dll /Fo:bin\ /link %LFLAGS%              || exit /b 1
cl %CFLAGS% /LD dll1.c /Fe:bin\dll1.dll /Fo:bin\ /link %LFLAGS% bin\dll2.lib || exit /b 1
cl %CFLAGS%     test.c /Fe:bin\test.exe /Fo:bin\ /link %LFLAGS% bin\dll1.lib || exit /b 1

echo [+] Build OK: bin\test.exe, bin\dll1.dll, bin\dll2.dll
