properties {
	$base_directory = Resolve-Path .
	$src_directory = "$base_directory\source"
	$output_directory = "$base_directory\_build"
	$dist_directory = "$base_directory\_distribution"
	$sln_file = "$src_directory\Toshal.Template.sln"
	$target_config = "Release"
	$framework_version = "v4.5"
	$nuget_path = "$base_directory\.nuget\nuget.exe"

	$buildNumber = 0;
	$version = "1.0.0.0"
	$preRelease = $null
}

task default -depends Clean, CreateNuGetPackage

task Clean {
	rmdir $output_directory -ea SilentlyContinue -recurse
	rmdir $dist_directory -ea SilentlyContinue -recurse
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /t:Clean }
}

task Compile -depends UpdateVersion {
	exec { msbuild /nologo /verbosity:q $sln_file /p:Configuration=$target_config /p:TargetFrameworkVersion=v4.5 }

	if ($LastExitCode -ne 0) {
        exit $LastExitCode
    }
}

task UpdateVersion {
	$vSplit = $version.Split('.')
	if($vSplit.Length -ne 4)
	{
		throw "Version number is invalid. Must be in the form of 0.0.0.0"
	}
	$major = $vSplit[0]
	$minor = $vSplit[1]
	$patch = $vSplit[2]
	$assemblyFileVersion =  "$major.$minor.$patch.$buildNumber"
	$assemblyVersion = "$major.$minor.0.0"
	$versionAssemblyInfoFile = "$src_directory/VersionAssemblyInfo.cs"
	"using System.Reflection;" > $versionAssemblyInfoFile
	"" >> $versionAssemblyInfoFile
	"[assembly: AssemblyVersion(""$assemblyVersion"")]" >> $versionAssemblyInfoFile
	"[assembly: AssemblyFileVersion(""$assemblyFileVersion"")]" >> $versionAssemblyInfoFile
}

task CreateNuGetPackage -depends Compile {
	$vSplit = $version.Split('.')
	if($vSplit.Length -ne 4)
	{
		throw "Version number is invalid. Must be in the form of 0.0.0.0"
	}
	$major = $vSplit[0]
	$minor = $vSplit[1]
	$patch = $vSplit[2]
	$packageVersion =  "$major.$minor.$patch"
	if($preRelease){
		$packageVersion = "$packageVersion-$preRelease"
	}
	
	if ($buildNumber -ne 0){
		$packageVersion = $packageVersion + "-build" + $buildNumber.ToString().PadLeft(5,'0')
	}

    gci .\source -Recurse "*.nuspec" |% {
        "Packaging " + $_.FullName
        
        $project = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
        $projectSrcPath = [System.IO.Path]::GetDirectoryName($_.FullName)
        $projectDistPath = "$dist_directory\$project"
        
        New-Item $projectDistPath\lib\net45 -Type Directory
		copy-item $projectSrcPath\$project.nuspec $projectDistPath
		copy-item $projectSrcPath\bin\$project.dll $projectDistPath\lib\net45
        
        if(Test-Path $projectSrcPath\support_package_build.ps1){
            Invoke-Psake $projectSrcPath\support_package_build.ps1 -framework "4.5.1x64" -properties @{ base_directory=$projectSrcPath; dist_directory=$projectDistPath; }
        }
        
		exec { . $nuget_path pack $projectDistPath\$project.nuspec -BasePath $projectDistPath -o $dist_directory -version $packageVersion }

        if ($LastExitCode -ne 0) {
            exit $LastExitCode
        }
    }
}
