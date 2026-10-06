@echo off
rem Runs the tests with coverlet, prints a per-class table and writes coverage\index.html. Fails below 90 percent.
if exist coverage rmdir /s /q coverage
dotnet test Toshal.Template.sln -c Release --nologo -v q -clp:ErrorsOnly --logger "console;verbosity=minimal" --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory coverage
if errorlevel 1 (echo TESTS FAILED & exit /b 1)
dotnet run --project tools\Toshal.Template.Tools -c Release -v q -clp:ErrorsOnly -- coverage 90
exit /b %errorlevel%
