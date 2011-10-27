echo "Compiling .net source (with microsoft windows sdk msbuild)"
msbuild /verbosity:minimal /p:Platform="Any CPU" /p:Configuration="Debug" dotnet/Pyrolite.sln /t:Rebuild
copy dotnet\Pyrolite\bin\Debug\*.dll build\
copy dotnet\Pyrolite\bin\Debug\*.pdb build\
