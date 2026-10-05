@echo off
rem Builds the NuGet package into artifacts\
if exist artifacts rmdir /s /q artifacts
dotnet pack src\Toshal.Template -c Release --nologo -v q -clp:ErrorsOnly -o artifacts
if errorlevel 1 (echo PACK FAILED & exit /b 1)
dir /b artifacts\*.nupkg
