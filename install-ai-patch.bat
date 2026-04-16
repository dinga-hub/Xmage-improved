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
    :: Tentar direto: PASTA\mage-server\lib
    if exist "%%~P\mage-server\lib" (
        if "!SERVER_DIR!"=="" set "SERVER_DIR=%%~P\mage-server"
    )
    :: Tentar um nivel abaixo: PASTA\xmage\mage-server\lib
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
    echo Informe o caminho da pasta XMage.
    echo Exemplos:
    echo   C:\Users\SeuNome\AppData\Roaming\XMage
    echo   C:\Users\SeuNome\Desktop\XMage
    echo.
    set /p BASE_INPUT=Caminho:
    set "BASE_INPUT=!BASE_INPUT:"=!"

    :: Tentar com e sem \xmage no caminho informado
    if exist "!BASE_INPUT!\mage-server\lib" (
        set "SERVER_DIR=!BASE_INPUT!\mage-server"
    ) else if exist "!BASE_INPUT!\xmage\mage-server\lib" (
        set "SERVER_DIR=!BASE_INPUT!\xmage\mage-server"
    ) else (
        echo.
        echo ERRO: Nao encontrei a pasta mage-server em: !BASE_INPUT!
        echo Verifique o caminho e tente novamente.
        pause
        exit /b 1
    )
)

echo.
echo Usando: !SERVER_DIR!
echo.

:: Usar subrotina para isolar as variaveis de caminho
call :instalar "!SERVER_DIR!"
goto :fim

:: ============================================
:: ETAPA 2: Detectar versao e instalar JARs
:: ============================================
:instalar
set "SRVDIR=%~1"
set "LIB=%SRVDIR%\lib"
set "PLUGINS=%SRVDIR%\plugins"

:: --- Detectar nome correto dos JARs existentes (adapta a qualquer versao) ---
set "JAR_AI=mage-player-ai-1.4.58.jar"
set "JAR_AI_MA=mage-player-ai-ma-1.4.58.jar"
set "JAR_HUMAN=mage-player-human-1.4.58.jar"

for %%F in ("%LIB%\mage-player-ai-*.jar") do (
    if not "%%~nxF"=="mage-player-ai-*.jar" set "JAR_AI=%%~nxF"
)
for %%F in ("%PLUGINS%\mage-player-ai-ma-*.jar") do (
    if not "%%~nxF"=="mage-player-ai-ma-*.jar" set "JAR_AI_MA=%%~nxF"
)
for %%F in ("%PLUGINS%\mage-player-human-*.jar") do (
    if not "%%~nxF"=="mage-player-human-*.jar" set "JAR_HUMAN=%%~nxF"
)

echo Versao detectada: %JAR_AI%
echo.

:: --- Baixar e instalar ---
echo [1/3] Baixando mage-player-ai.jar...
powershell -Command "Invoke-WebRequest -Uri '%GITHUB_BASE%/mage-player-ai.jar' -OutFile '%LIB%\%JAR_AI%' -UseBasicParsing"
if %errorlevel% neq 0 (
    echo ERRO ao baixar mage-player-ai.jar. Verifique sua conexao com a internet.
    pause
    exit /b 1
)
echo [1/3] OK

echo [2/3] Baixando mage-player-ai-ma.jar...
powershell -Command "Invoke-WebRequest -Uri '%GITHUB_BASE%/mage-player-ai-ma.jar' -OutFile '%PLUGINS%\%JAR_AI_MA%' -UseBasicParsing"
if %errorlevel% neq 0 (
    echo ERRO ao baixar mage-player-ai-ma.jar. Verifique sua conexao com a internet.
    pause
    exit /b 1
)
echo [2/3] OK

echo [3/3] Baixando mage-player-human.jar...
powershell -Command "Invoke-WebRequest -Uri '%GITHUB_BASE%/mage-player-human.jar' -OutFile '%PLUGINS%\%JAR_HUMAN%' -UseBasicParsing"
if %errorlevel% neq 0 (
    echo ERRO ao baixar mage-player-human.jar. Verifique sua conexao com a internet.
    pause
    exit /b 1
)
echo [3/3] OK
goto :eof

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
    print(f'  [OK] {bat}')
    sys.exit(0)

c = re.sub(r'-Xmx\S+', '-Xmx4096m', c)
if 'UseG1GC' not in c:
    c = c.replace('java ', 'java -XX:+UseG1GC ', 1)
with open(bat, 'w', encoding='utf-8') as f:
    f.write(c)
print(f'  [ATUALIZADO] {bat}')
" 2>nul
if errorlevel 1 (
    echo  AVISO: Python nao encontrado. Corrija a memoria JVM manualmente:
    echo  No startServer.bat, troque -Xmx pelo valor original por -Xmx4096m
    echo  e adicione -XX:+UseG1GC nos argumentos java.
)

echo.
echo ============================================
echo  Patch instalado com sucesso!
echo  Reinicie o servidor XMage para aplicar.
echo ============================================
pause
