#!/bin/sh
echo "Compiling .net source"
rm -r dotnet/Pyrolite/bin
#rm -r dotnet/Pyrolite/obj
rm -r dotnet/Pyrolite.Tests/bin
#rm -r dotnet/Pyrolite.Tests/obj
xbuild /verbosity:minimal /property:Configuration=Debug dotnet/Pyrolite.sln
cp dotnet/Pyrolite/bin/Debug/*.dll build

