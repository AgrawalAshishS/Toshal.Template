@echo off
rem Rebuilds the website in docs\ from the XML comments, the example files and tools\Toshal.Template.Tools\content.
rem   docs        writes docs\
rem   docs check  fails when docs\ is out of date (the tests run this check too)
dotnet build src\Toshal.Template -c Release --nologo -v q -clp:ErrorsOnly
if errorlevel 1 (echo BUILD FAILED & exit /b 1)
dotnet run --project tools\Toshal.Template.Tools -c Release -v q -clp:ErrorsOnly -- docs %1
exit /b %errorlevel%
