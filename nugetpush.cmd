cd Source
dotnet pack -c release -o ..\_distribution
cd ..
for /f %%f in ('dir /b _distribution\*.nupkg') do .\.nuget\nuget push _distribution\%%f -source http://nuget.toshalinternal.com