echo "Compiling .net source (with microsoft windows sdk msbuild)"
msbuild /verbosity:minimal /p:Platform="Any CPU" /p:Configuration="Debug" Pyrolite.sln /t:Rebuild
if not exist build mkdir build
copy Pyrolite\bin\Debug\*.dll build\
copy Pyrolite\bin\Debug\*.pdb build\
