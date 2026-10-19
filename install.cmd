@echo off
rem Copia a pasta Mod\ para a pasta de mods do jogo como "WuxiaMod".
rem Uso: install.cmd ["pasta do jogo"]
setlocal
set GAME=%~1
if "%GAME%"=="" set GAME=C:\Program Files (x86)\Steam\steamapps\common\Outer Space
if not exist "%GAME%\Outer Space.exe" (
  echo ERRO: jogo nao encontrado em %GAME%
  echo Uso: install.cmd "C:\caminho\para\Outer Space"
  exit /b 1
)
if not exist "%~dp0Mod\v_common\Assemblies\WuxiaMod.dll" (
  echo ERRO: compile primeiro com build.cmd
  exit /b 1
)
set DEST=%GAME%\mods\WuxiaMod
echo Instalando em %DEST%
xcopy /E /I /Y "%~dp0Mod\Manifest" "%DEST%\Manifest\" >nul
xcopy /E /I /Y "%~dp0Mod\v_common" "%DEST%\v_common\" >nul
del /q "%DEST%\v_common\Assemblies\.gitkeep" 2>nul
echo.
echo Pronto. Abra o jogo, ative "Wuxia" no gerenciador de mods (precisa do Harmony ativo) e reinicie o jogo.
endlocal
