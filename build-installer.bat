@echo off
setlocal
title XMage - Build Installer EXE

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SRC=%~dp0installer-src\XMageInstaller.cs
set OUT=%~dp0XMageAIPatch.exe

echo ============================================
echo  XMage AI - Build Installer EXE
echo ============================================
echo.

if not exist "%CSC%" (
    echo ERRO: csc.exe nao encontrado em %CSC%
    pause & exit /b 1
)

echo Compilando %SRC%...
REM System.IO.Compression* needed for GameChangerRegistry inject into mage-*.jar
"%CSC%" /nologo /optimize+ /target:exe ^
  /r:System.IO.Compression.dll ^
  /r:System.IO.Compression.FileSystem.dll ^
  /out:"%OUT%" "%SRC%"
if %errorlevel% neq 0 (
    echo.
    echo ERRO na compilacao.
    pause & exit /b 1
)

echo.
echo OK: %OUT%
echo Tamanho:
dir "%OUT%" | findstr "XMageAIPatch"
echo.
echo Proximo passo: subir XMageAIPatch.exe + 3 JARs + GameChangerRegistry.class
echo no GitHub Releases.
pause
