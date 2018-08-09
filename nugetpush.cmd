rem nuget.exe sources add -name BSBNuget -Source http://nuget.bsbinternal.com:4001/api/v2
rem nuget.exe setapikey <KEY> -source http://nuget.bsbinternal.com:4001/
for /f %%f in ('dir /b _distribution\*.nupkg') do .\.nuget\nuget push _distribution\%%f -source http://nuget.toshalinternal.com
for /f %%f in ('dir /b _distribution\*.nupkg') do .\.nuget\nuget push _distribution\%%f -source http://nuget.bsbinternal.com:4001