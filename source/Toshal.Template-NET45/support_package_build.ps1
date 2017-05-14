properties {
	$base_directory = ""
    $projectSrcPath = ""
	$projectDistPath = ""
}

task default -depends CopyLibFiles

task CopyLibFiles {
    New-Item $projectDistPath\lib\netstandard1.6 -Type Directory
    copy-item $base_directory\source\Toshal.Template\bin\Release\netstandard1.6\Toshal.Template.dll $projectDistPath\lib\netstandard1.6

    if ($LastExitCode -ne 0) {
        exit $LastExitCode
    }
}
