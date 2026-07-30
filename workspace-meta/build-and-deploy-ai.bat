@echo off
setlocal EnableDelayedExpansion

set JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-8.0.482.8-hotspot
set MAVEN=C:\Users\diego\Desktop\XMage\apache-maven-3.9.6\bin\mvn.cmd
set SOURCE=C:\Users\diego\Desktop\XMage\mage-source
set LIB=C:\Users\diego\Desktop\XMage\xmage\mage-server\lib
set PLUGINS=C:\Users\diego\Desktop\XMage\xmage\mage-server\plugins

set "JAR_CORE="
set "JAR_AI="
set "JAR_AI_MA="
set "JAR_HUMAN="
for %%F in ("%LIB%\mage-*.jar") do (
  set "FN=%%~nxF"
  echo !FN!| findstr /i /c:"mage-common-" /c:"mage-sets-" /c:"mage-server-" /c:"mage-game-" /c:"mage-player-" /c:"mage-tournament-" >nul
  if errorlevel 1 set "JAR_CORE=%%~nxF"
)
for %%F in ("%LIB%\mage-player-ai-*.jar") do (
  echo %%~nxF| findstr /i /c:"-mcts-" /c:"-draftbot-" /c:"-mad-" /c:"-ma-" >nul || set "JAR_AI=%%~nxF"
)
:: Grath 1.4.60+ renamed MAD plugin: ai-ma → ai-mad
for %%F in ("%PLUGINS%\mage-player-ai-mad-*.jar") do (
  if not "%%~nxF"=="mage-player-ai-mad-*.jar" set "JAR_AI_MA=%%~nxF"
)
if "!JAR_AI_MA!"=="" (
  for %%F in ("%PLUGINS%\mage-player-ai-ma-*.jar") do (
    if not "%%~nxF"=="mage-player-ai-ma-*.jar" set "JAR_AI_MA=%%~nxF"
  )
)
for %%F in ("%PLUGINS%\mage-player-human-*.jar") do (
  if not "%%~nxF"=="mage-player-human-*.jar" set "JAR_HUMAN=%%~nxF"
)

if "!JAR_CORE!"=="" (
  echo ERRO: nao encontrei mage-*.jar core em %LIB%
  echo Instale/atualize o servidor XMage pelo launcher oficial primeiro.
  pause & exit /b 1
)
if "!JAR_AI!"=="" (
  echo ERRO: nao encontrei mage-player-ai-*.jar em %LIB%
  echo Instale/atualize o servidor XMage pelo launcher oficial primeiro.
  pause & exit /b 1
)
if "!JAR_AI_MA!"=="" (
  echo ERRO: nao encontrei mage-player-ai-mad-*.jar nem mage-player-ai-ma-*.jar em %PLUGINS%
  pause & exit /b 1
)
if "!JAR_HUMAN!"=="" (
  echo ERRO: nao encontrei mage-player-human-*.jar em %PLUGINS%
  pause & exit /b 1
)

echo ============================================
echo  XMage AI - Build e Deploy
echo ============================================
echo Destinos detectados:
echo   lib\!JAR_CORE!
echo   lib\!JAR_AI!
echo   plugins\!JAR_AI_MA!
echo   plugins\!JAR_HUMAN!
echo.

echo [1/5] Compilando modulos...
cd /d "%SOURCE%"
"%MAVEN%" install -pl "Mage.Server.Plugins/Mage.Player.AI,Mage.Server.Plugins/Mage.Player.AI.MA,Mage.Server.Plugins/Mage.Player.Human" -am -DskipTests
if %errorlevel% neq 0 (
  echo ERRO na compilacao. Abortando.
  pause & exit /b 1
)
echo [1/5] OK

echo [2/5] Deploy core Mage para lib\...
REM WHY skip by default: Mage/target/mage.jar is still built from pom 1.4.58.
REM Copying it over Grath 1.4.60 caused NoSuchMethodError / card-load failures
REM (memory 2026-07-23). GameChangerRegistry is injected surgically when needed.
REM Set DEPLOY_CORE=1 only after mage-source is synced to the same server version.
if /I "%DEPLOY_CORE%"=="1" (
  if exist "%LIB%\!JAR_CORE!" copy "%LIB%\!JAR_CORE!" "%LIB%\!JAR_CORE!.backup" /Y > nul 2>&1
  copy "%SOURCE%\Mage\target\mage.jar" "%LIB%\!JAR_CORE!" /Y > nul
  if %errorlevel% neq 0 ( echo ERRO deploy mage.jar & pause & exit /b 1 )
  echo [2/5] OK ^(DEPLOY_CORE=1^)
) else (
  echo [2/5] SKIPPED core overwrite ^(safe default on Grath 1.4.60^).
  echo       Para forcar: set DEPLOY_CORE=1
)

echo [3/5] Deploy AI para lib\...
if exist "%LIB%\!JAR_AI!" copy "%LIB%\!JAR_AI!" "%LIB%\!JAR_AI!.backup" /Y > nul 2>&1
copy "%SOURCE%\Mage.Server.Plugins\Mage.Player.AI\target\mage-player-ai.jar" "%LIB%\!JAR_AI!" /Y > nul
if %errorlevel% neq 0 ( echo ERRO deploy mage-player-ai.jar & pause & exit /b 1 )
echo [3/5] OK

echo [4/5] Deploy AI.MA para plugins\...
if exist "%PLUGINS%\!JAR_AI_MA!" copy "%PLUGINS%\!JAR_AI_MA!" "%PLUGINS%\!JAR_AI_MA!.backup" /Y > nul 2>&1
copy "%SOURCE%\Mage.Server.Plugins\Mage.Player.AI.MA\target\mage-player-ai-ma.jar" "%PLUGINS%\!JAR_AI_MA!" /Y > nul
if %errorlevel% neq 0 ( echo ERRO deploy mage-player-ai-ma.jar & pause & exit /b 1 )
echo [4/5] OK

echo [5/5] Deploy Human para plugins\...
if exist "%PLUGINS%\!JAR_HUMAN!" copy "%PLUGINS%\!JAR_HUMAN!" "%PLUGINS%\!JAR_HUMAN!.backup" /Y > nul 2>&1
copy "%SOURCE%\Mage.Server.Plugins\Mage.Player.Human\target\mage-player-human.jar" "%PLUGINS%\!JAR_HUMAN!" /Y > nul
if %errorlevel% neq 0 ( echo ERRO deploy mage-player-human.jar & pause & exit /b 1 )
echo [5/5] OK

echo.
echo ============================================
echo  Pronto! REINICIE o servidor XMage.
echo  JARs nao recarregam em caliente.
echo  Apos update oficial: use XMageAIPatch.exe
echo  para reaplicar os JARs do release ^(mesmo dos amigos^).
echo  Diego: rode este script de novo para mage.jar + players.
echo ============================================
pause
