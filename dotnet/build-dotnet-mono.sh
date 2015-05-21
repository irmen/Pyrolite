#!/bin/sh
echo "Compiling .net source"
if [ -d Pyrolite/bin ]; then
  rm -r Pyrolite/bin
fi
if [ -d Pyrolite.Tests/bin ]; then
  rm -r Pyrolite.Tests/bin
fi
xbuild /verbosity:minimal /property:Configuration=Debug /property:Platform="Any CPU" Pyrolite.sln
mkdir -p build
cp Pyrolite/bin/Debug/*.dll build

