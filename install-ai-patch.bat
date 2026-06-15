@echo off
setlocal enabledelayedexpansion
title XMage AI Patch - Instalador

echo ============================================
echo  XMage AI Patch - Instalador
echo  Commander AI melhorado por Diego
echo ============================================
echo.

:: === URL base dos JARs no GitHub Releases ===
set GITHUB_BASE=https://github.com/dinga-hub/Xmage-improved/releases/latest/download

:: ============================================
:: ETAPA 1: Localizar a pasta mage-server
:: ============================================

set "SERVER_DIR="

:: Candidatos para a pasta raiz do XMage
for %%P in (
    "%APPDATA%\XMage"
    "%LOCALAPPDATA%\XMage"
    "%USERPROFILE%\XMage"
    "%USERPROFILE%\Desktop\XMage"
    "C:\XMage"
) do (
    if exist "%%~P\mage-server\lib" (
        if "!SERVER_DIR!"=="" set "SERVER_DIR=%%~P\mage-server"
    )
    if exist "%%~P\xmage\mage-server\lib" (
        if "!SERVER_DIR!"=="" set "SERVER_DIR=%%~P\xmage\mage-server"
    )
)

if not "!SERVER_DIR!"=="" (
    echo XMage encontrado em: !SERVER_DIR!
    echo.
    set /p CONFIRM=Usar este caminho? [S/N]:
    if /i "!CONFIRM!"=="N" set "SERVER_DIR="
)

if "!SERVER_DIR!"=="" (
    echo Nao foi possivel encontrar o XMage automaticamente.
    echo.
    echo Informe o caminho completo da pasta mage-server.
    echo Exemplos:
    echo   C:\Users\SeuNome\AppData\Roaming\XMage\mage-server
    echo   C:\Users\SeuNome\Desktop\XMage\xmage\mage-server
    echo.
    set /p BASE_INPUT=Caminho:
    set "BASE_INPUT=!BASE_INPUT:"=!"

    if exist "!BASE_INPUT!\lib" (
        set "SERVER_DIR=!BASE_INPUT!"
    ) else if exist "!BASE_INPUT!\mage-server\lib" (
        set "SERVER_DIR=!BASE_INPUT!\mage-server"
    ) else if exist "!BASE_INPUT!\xmage\mage-server\lib" (
        set "SERVER_DIR=!BASE_INPUT!\xmage\mage-server"
    ) else (
        echo.
        echo ERRO: Nao encontrei lib\ em: !BASE_INPUT!
        echo Verifique o caminho e tente novamente.
        pause
        exit /b 1
    )
)

echo.
echo Usando: !SERVER_DIR!
echo.

call :instalar "!SERVER_DIR!"
goto :fim

:: ============================================
:: ETAPA 2: Detectar versao e instalar JARs
:: ============================================
:instalar
set "SRVDIR=%~1"
set "LIB=%SRVDIR%\lib"
set "PLUGINS=%SRVDIR%\plugins"

:: --- Detectar nome correto dos JARs existentes ---
set "JAR_AI=mage-player-ai-1.4.58.jar"
set "JAR_AI_MA=mage-player-ai-ma-1.4.58.jar"
set "JAR_HUMAN=mage-player-human-1.4.58.jar"

for %%F in ("%LIB%\mage-player-ai-*.jar") do (
    echo %%~nxF| findstr /i /c:"-mcts-" /c:"-draftbot-" >nul || set "JAR_AI=%%~nxF"
)
for %%F in ("%PLUGINS%\mage-player-ai-ma-*.jar") do (
    if not "%%~nxF"=="mage-player-ai-ma-*.jar" set "JAR_AI_MA=%%~nxF"
)
for %%F in ("%PLUGINS%\mage-player-human-*.jar") do (
    if not "%%~nxF"=="mage-player-human-*.jar" set "JAR_HUMAN=%%~nxF"
)

echo Versao detectada: %JAR_AI%
echo Destinos:
echo   lib\%JAR_AI%
echo   plugins\%JAR_AI_MA%
echo   plugins\%JAR_HUMAN%
echo.

:: --- Escolher ferramenta de download (curl nativo prefere, fallback PowerShell) ---
curl.exe --version >nul 2>&1
if %errorlevel% equ 0 (
    set DOWNLOADER=curl
) else (
    set DOWNLOADER=powershell
)
echo Usando: %DOWNLOADER% para download
echo.

:: --- Backup dos JARs atuais ---
if exist "%LIB%\%JAR_AI%" copy "%LIB%\%JAR_AI%" "%LIB%\%JAR_AI%.backup" /Y >nul 2>&1
if exist "%PLUGINS%\%JAR_AI_MA%" copy "%PLUGINS%\%JAR_AI_MA%" "%PLUGINS%\%JAR_AI_MA%.backup" /Y >nul 2>&1
if exist "%PLUGINS%\%JAR_HUMAN%" copy "%PLUGINS%\%JAR_HUMAN%" "%PLUGINS%\%JAR_HUMAN%.backup" /Y >nul 2>&1

:: --- Download e verificacao ---
echo [1/3] Baixando mage-player-ai.jar...
call :baixar "%GITHUB_BASE%/mage-player-ai.jar" "%LIB%\%JAR_AI%"
if %errorlevel% neq 0 ( echo ERRO no download 1. Abortando. & pause & exit /b 1 )

echo [2/3] Baixando mage-player-ai-ma.jar...
call :baixar "%GITHUB_BASE%/mage-player-ai-ma.jar" "%PLUGINS%\%JAR_AI_MA%"
if %errorlevel% neq 0 ( echo ERRO no download 2. Abortando. & pause & exit /b 1 )

echo [3/3] Baixando mage-player-human.jar...
call :baixar "%GITHUB_BASE%/mage-player-human.jar" "%PLUGINS%\%JAR_HUMAN%"
if %errorlevel% neq 0 ( echo ERRO no download 3. Abortando. & pause & exit /b 1 )

goto :eof

:: --- Subrotina de download com verificacao de tamanho ---
:baixar
set "DL_URL=%~1"
set "DL_OUT=%~2"

if "%DOWNLOADER%"=="curl" (
    curl.exe -L -f -s -o "%DL_OUT%" "%DL_URL%"
) else (
    powershell -NoProfile -Command "$ErrorActionPreference='Stop'; try { Invoke-WebRequest -Uri '%DL_URL%' -OutFile '%DL_OUT%' -UseBasicParsing } catch { exit 1 }"
)

:: Verifica se o arquivo foi baixado e tem tamanho razoavel (>10KB)
if not exist "%DL_OUT%" (
    echo   ERRO: arquivo nao foi criado: %DL_OUT%
    exit /b 1
)
for %%A in ("%DL_OUT%") do set FSIZE=%%~zA
if !FSIZE! LSS 10240 (
    echo   ERRO: arquivo muito pequeno ^(!FSIZE! bytes^) - download falhou
    exit /b 1
)
echo   OK ^(!FSIZE! bytes^)
exit /b 0

:fim
echo.

:: ============================================
:: ETAPA 3: Patch de memoria JVM
:: ============================================
echo [4/4] Aplicando patch de memoria JVM...

python -c "
import re, os, sys

def find_server_bat(base):
    candidates = [
        os.path.join(base, 'mage-server', 'startServer.bat'),
        os.path.join(base, 'xmage', 'mage-server', 'startServer.bat'),
    ]
    for c in candidates:
        if os.path.exists(c):
            return c
    return None

import subprocess
result = subprocess.run(['reg', 'query', 'HKCU\\Software\\XMage', '/v', 'InstallDir'], capture_output=True, text=True)
base = None
for line in result.stdout.splitlines():
    if 'InstallDir' in line:
        base = line.split()[-1]
        break

candidates_base = []
if base:
    candidates_base.append(base)
import os
for b in [os.environ.get('APPDATA',''), os.environ.get('LOCALAPPDATA',''), os.environ.get('USERPROFILE','')]:
    candidates_base += [os.path.join(b,'XMage'), os.path.join(b,'Desktop','XMage'), os.path.join(b,'xmage')]
candidates_base.append('C:\\\\XMage')

bat = None
for b in candidates_base:
    bat = find_server_bat(b)
    if bat:
        break

if not bat:
    print('  AVISO: startServer.bat nao encontrado. Corrija a memoria JVM manualmente.')
    sys.exit(0)

with open(bat, 'r', encoding='utf-8', errors='replace') as f:
    c = f.read()

already = '-Xmx4096m' in c and 'UseG1GC' in c
if already:
    print('  [OK] ' + bat)
    sys.exit(0)

c = re.sub(r'-Xmx\S+', '-Xmx4096m', c)
if 'UseG1GC' not in c:
    c = c.replace('java ', 'java -XX:+UseG1GC ', 1)
with open(bat, 'w', encoding='utf-8') as f:
    f.write(c)
print('  [ATUALIZADO] ' + bat)
" 2>nul
if errorlevel 1 (
    echo  AVISO: Python nao encontrado. Ajuste o startServer.bat manualmente:
    echo  Troque -Xmx pelo valor atual por -Xmx4096m e adicione -XX:+UseG1GC
)

echo.
echo ============================================
echo  Patch instalado com sucesso!
echo  Reinicie o servidor XMage para aplicar.
echo ============================================
pause
