@echo off
rem Copia a pasta Mod\ para a pasta de mods do jogo como "WuxiaMod".
rem Uso: install.cmd ["pasta do jogo"]
setlocal
set GAME=%~1
if "%GAME%"=="" set GAME=C:\Program Files (x86)\Steam\steamapps\common\Outer Space
if not exist "%GAME%\Outer Space.exe" (
  echo ERRO: jogo nao encontrado em %GAME%
  echo Uso: install.cmd "C:\caminho\para\Outer Space"
  goto fim
)
if not exist "%~dp0Mod\v_common\Assemblies\WuxiaMod.dll" (
  echo ERRO: a WuxiaMod.dll ainda nao existe. Rode build.cmd primeiro.
  goto fim
)
set DEST=%GAME%\mods\WuxiaMod
echo Instalando em %DEST%
xcopy /E /I /Y "%~dp0Mod\Manifest" "%DEST%\Manifest\" >nul
if errorlevel 1 goto semPermissao
xcopy /E /I /Y "%~dp0Mod\v_common" "%DEST%\v_common\" >nul
if errorlevel 1 goto semPermissao
del /q "%DEST%\v_common\Assemblies\.gitkeep" 2>nul
echo.
echo Pronto. Abra o jogo, ative "Harmony" e "Wuxia" no gerenciador de mods e reinicie o jogo.
goto fim

:semPermissao
echo.
echo ERRO: sem permissao para gravar em %DEST%
echo Clique com o botao direito no install.cmd e escolha "Executar como administrador".

:fim
echo.
pause
endlocal
