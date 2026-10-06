@echo off
rem Runs the tests. Optional argument: a dotnet test filter, for example  test ParserReuseTests
if "%~1"=="" (
  dotnet test Toshal.Template.sln -c Release --nologo -v q -clp:ErrorsOnly --logger "console;verbosity=minimal"
) else (
  dotnet test Toshal.Template.sln -c Release --nologo -v q -clp:ErrorsOnly --logger "console;verbosity=minimal" --filter "%~1"
)
if errorlevel 1 (echo TESTS FAILED & exit /b 1)
