@echo off
rem Builds the whole solution in Release. Prints errors and a short summary only.
dotnet build Toshal.Template.sln -c Release --nologo -v q -clp:ErrorsOnly;Summary
if errorlevel 1 (echo BUILD FAILED & exit /b 1)
echo BUILD OK
