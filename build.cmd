@echo off
rem Compila o mod. Precisa do .NET SDK (dotnet) instalado.
rem A DLL sai em Mod\v_common\Assemblies\WuxiaMod.dll
setlocal
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERRO: .NET SDK nao encontrado.
  echo Instale o .NET SDK 8 em https://dotnet.microsoft.com/download e rode de novo.
  goto fim
)
dotnet build "%~dp0src\WuxiaMod\WuxiaMod.csproj" -c Release
if errorlevel 1 (
  echo.
  echo ERRO: o build falhou. Mande um print desta janela.
  goto fim
)
if not exist "%~dp0Mod\v_common\Assemblies\WuxiaMod.dll" (
  echo.
  echo ERRO: o build terminou mas a WuxiaMod.dll nao foi gerada.
  goto fim
)
echo.
echo Build OK: Mod\v_common\Assemblies\WuxiaMod.dll
echo Agora rode install.cmd
:fim
echo.
pause
endlocal
