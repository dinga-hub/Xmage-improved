@echo off
setlocal
title XMage AI Patch - Empacotar Release

set SOURCE=C:\Users\diego\Desktop\XMage\mage-source
set RELEASE_DIR=C:\Users\diego\Desktop\XMage\release-jars

echo ============================================
echo  XMage AI Patch - Empacotar Release
echo ============================================
echo.

:: Limpar e criar pasta de release
if exist "%RELEASE_DIR%" rmdir /s /q "%RELEASE_DIR%"
mkdir "%RELEASE_DIR%"

echo Copiando JARs compilados para: %RELEASE_DIR%
echo.

copy "%SOURCE%\Mage.Server.Plugins\Mage.Player.AI\target\mage-player-ai.jar"       "%RELEASE_DIR%\mage-player-ai.jar"
copy "%SOURCE%\Mage.Server.Plugins\Mage.Player.AI.MA\target\mage-player-ai-ma.jar" "%RELEASE_DIR%\mage-player-ai-ma.jar"
copy "%SOURCE%\Mage.Server.Plugins\Mage.Player.Human\target\mage-player-human.jar" "%RELEASE_DIR%\mage-player-human.jar"

set "GC_CLASS=%SOURCE%\Mage\target\classes\mage\cards\repository\GameChangerRegistry.class"
if not exist "%GC_CLASS%" (
  echo AVISO: GameChangerRegistry.class nao encontrado em target\classes
  echo        Extraia de Mage\target\mage.jar se preciso.
) else (
  copy "%GC_CLASS%" "%RELEASE_DIR%\GameChangerRegistry.class"
)

echo.
echo ============================================
echo  Pronto! Suba no GitHub Releases:
echo.
echo  %RELEASE_DIR%\mage-player-ai.jar
echo  %RELEASE_DIR%\mage-player-ai-ma.jar
echo  %RELEASE_DIR%\mage-player-human.jar
echo  %RELEASE_DIR%\GameChangerRegistry.class
echo  + XMageAIPatch.exe ^(apos build-installer.bat^)
echo ============================================
echo.
start "" "%RELEASE_DIR%"
pause
