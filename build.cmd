@echo off
rem Compila o mod. Precisa do .NET SDK (dotnet) instalado.
rem A DLL sai em Mod\v_common\Assemblies\WuxiaMod.dll
dotnet build "%~dp0src\WuxiaMod\WuxiaMod.csproj" -c Release
