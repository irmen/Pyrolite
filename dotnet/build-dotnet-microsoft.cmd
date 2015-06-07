echo "Compiling .net source (with microsoft windows sdk msbuild)"
msbuild /verbosity:minimal /p:Platform="Any CPU" /p:Configuration="Release" Pyrolite.sln /t:Rebuild
if not exist build mkdir build
copy Pyrolite\bin\Release\*.dll build\
copy Pyrolite\bin\Release\*.pdb build\
