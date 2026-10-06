@echo off
rem Builds the NuGet packages into artifacts\: the engine, the run time of compiled templates, the source generator and the tool.
if exist artifacts rmdir /s /q artifacts
for %%p in (Toshal.Template Toshal.Template.Compiled Toshal.Template.Generator Toshal.Template.Cli) do (
  dotnet pack src\%%p -c Release --nologo -v q -clp:ErrorsOnly -o artifacts
  if errorlevel 1 (echo PACK FAILED & exit /b 1)
)
dir /b artifacts\*.nupkg
