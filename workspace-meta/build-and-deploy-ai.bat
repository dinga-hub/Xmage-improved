@echo off
setlocal EnableDelayedExpansion

set JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-8.0.482.8-hotspot
set MAVEN=C:\Users\diego\Desktop\XMage\apache-maven-3.9.6\bin\mvn.cmd
set SOURCE=C:\Users\diego\Desktop\XMage\mage-source
set LIB=C:\Users\diego\Desktop\XMage\xmage\mage-server\lib
set PLUGINS=C:\Users\diego\Desktop\XMage\xmage\mage-server\plugins

set "JAR_AI="
set "JAR_AI_MA="
set "JAR_HUMAN="
for %%F in ("%LIB%\mage-player-ai-*.jar") do (
  if not "%%~nxF"=="mage-player-ai-*.jar" set "JAR_AI=%%~nxF"
)
for %%F in ("%PLUGINS%\mage-player-ai-ma-*.jar") do (
  if not "%%~nxF"=="mage-player-ai-ma-*.jar" set "JAR_AI_MA=%%~nxF"
)
for %%F in ("%PLUGINS%\mage-player-human-*.jar") do (
  if not "%%~nxF"=="mage-player-human-*.jar" set "JAR_HUMAN=%%~nxF"
)

if "!JAR_AI!"=="" (
  echo ERRO: nao encontrei mage-player-ai-*.jar em %LIB%
  echo Instale/atualize o servidor XMage pelo launcher oficial primeiro.
  pause & exit /b 1
)
if "!JAR_AI_MA!"=="" (
  echo ERRO: nao encontrei mage-player-ai-ma-*.jar em %PLUGINS%
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
echo   lib\!JAR_AI!
echo   plugins\!JAR_AI_MA!
echo   plugins\!JAR_HUMAN!
echo.

echo [1/4] Compilando modulos...
cd /d "%SOURCE%"
"%MAVEN%" install -pl "Mage.Server.Plugins/Mage.Player.AI,Mage.Server.Plugins/Mage.Player.AI.MA,Mage.Server.Plugins/Mage.Player.Human" -am -DskipTests
if %errorlevel% neq 0 (
  echo ERRO na compilacao. Abortando.
  pause & exit /b 1
)
echo [1/4] OK

echo [2/4] Deploy AI para lib\...
if exist "%LIB%\!JAR_AI!" copy "%LIB%\!JAR_AI!" "%LIB%\!JAR_AI!.backup" /Y > nul 2>&1
copy "%SOURCE%\Mage.Server.Plugins\Mage.Player.AI\target\mage-player-ai.jar" "%LIB%\!JAR_AI!" /Y > nul
if %errorlevel% neq 0 ( echo ERRO deploy mage-player-ai.jar & pause & exit /b 1 )
echo [2/4] OK

echo [3/4] Deploy AI.MA para plugins\...
if exist "%PLUGINS%\!JAR_AI_MA!" copy "%PLUGINS%\!JAR_AI_MA!" "%PLUGINS%\!JAR_AI_MA!.backup" /Y > nul 2>&1
copy "%SOURCE%\Mage.Server.Plugins\Mage.Player.AI.MA\target\mage-player-ai-ma.jar" "%PLUGINS%\!JAR_AI_MA!" /Y > nul
if %errorlevel% neq 0 ( echo ERRO deploy mage-player-ai-ma.jar & pause & exit /b 1 )
echo [3/4] OK

echo [4/4] Deploy Human para plugins\...
if exist "%PLUGINS%\!JAR_HUMAN!" copy "%PLUGINS%\!JAR_HUMAN!" "%PLUGINS%\!JAR_HUMAN!.backup" /Y > nul 2>&1
copy "%SOURCE%\Mage.Server.Plugins\Mage.Player.Human\target\mage-player-human.jar" "%PLUGINS%\!JAR_HUMAN!" /Y > nul
if %errorlevel% neq 0 ( echo ERRO deploy mage-player-human.jar & pause & exit /b 1 )
echo [4/4] OK

echo.
echo ============================================
echo  Pronto! Reinicie o servidor XMage.
echo  Apos update oficial: use XMageAIPatch.exe
echo  para reaplicar os JARs do release ^(mesmo dos amigos^).
echo ============================================
pause
