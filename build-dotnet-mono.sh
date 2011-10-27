#!/bin/sh
echo "Compiling .net source"
if [ -d dotnet/Pyrolite/bin ]; then
  rm -r dotnet/Pyrolite/bin
fi
if [ -d dotnet/Pyrolite.Tests/bin ]; then
  rm -r dotnet/Pyrolite.Tests/bin
fi
xbuild /verbosity:minimal /property:Configuration=Debug /property:Platform="Any CPU" dotnet/Pyrolite.sln
cp dotnet/Pyrolite/bin/Debug/*.dll build

