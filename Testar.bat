@echo off
rem Plano B (o Claude normalmente ja compila e instala sozinho):
rem compila, instala na pasta de mods e abre o jogo, tudo de uma vez.
setlocal
set GAME=C:\Program Files (x86)\Steam\steamapps\common\Outer Space
set DEST=%GAME%\mods\WuxiaMod

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERRO: .NET SDK nao encontrado. Instale o .NET SDK 8 em https://dotnet.microsoft.com/download
  goto erro
)
if not exist "%GAME%\Outer Space.exe" (
  echo ERRO: jogo nao encontrado em "%GAME%"
  goto erro
)

echo [1/3] Compilando...
dotnet build "%~dp0src\WuxiaMod\WuxiaMod.csproj" -c Release -nologo -v q
if errorlevel 1 (
  echo ERRO: o build falhou. Mande um print desta janela.
  goto erro
)

echo [2/3] Instalando em %DEST%
xcopy /E /I /Y "%~dp0Mod\Manifest" "%DEST%\Manifest\" >nul || goto semPermissao
xcopy /E /I /Y "%~dp0Mod\v_common" "%DEST%\v_common\" >nul || goto semPermissao
del /q "%DEST%\v_common\Assemblies\.gitkeep" 2>nul

echo [3/3] Abrindo o jogo...
start "" "steam://rungameid/3080070"
echo Pronto. Pode jogar; o Claude le o wuxia.log e o Player.log depois.
timeout /t 5 >nul
goto fim

:semPermissao
echo ERRO: sem permissao para gravar em %DEST%
echo Clique com o botao direito no Testar.bat e escolha "Executar como administrador".

:erro
echo.
pause
:fim
endlocal
