Param(
    [string]$version = "1.0.6.0",
	[string]$buildNumber = "0",
	[string]$preRelease = $null
)

if( !(Test-Path .\.nuget\nuget.exe)){
    if( !(Test-Path .\.nuget)){
        mkdir .nuget
    }
    $sourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
    $targetNugetExe = ".\.nuget\nuget.exe"
    Invoke-WebRequest $sourceNugetExe -OutFile $targetNugetExe
}

gci .\source -Recurse "packages.config" |% {
	"Restoring " + $_.FullName
	.\.nuget\nuget.exe restore $_.FullName -o .\source\packages
    
    if ($LastExitCode -ne 0) {
        exit $LastExitCode
    }
}

.\.nuget\nuget.exe restore .\.nuget\packages.config -o .\source\packages

Import-Module .\source\packages\psake.4.4.1\tools\psake.psm1

if(Test-Path Env:\APPVEYOR_BUILD_NUMBER){
	$buildNumber = [int]$Env:APPVEYOR_BUILD_NUMBER
	$task = "appVeyor"
    
    Write-Host "Using APPVEYOR_BUILD_NUMBER"
}

"Build number $buildNumber"

Invoke-Psake .\default.ps1 $task -framework "4.5.1x64" -properties @{ version=$version; buildNumber=$buildNumber; preRelease=$preRelease }

Remove-Module psake