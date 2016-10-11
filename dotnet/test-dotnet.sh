#!/bin/sh
echo "Building..."
. ./build-dotnet-mono.sh

echo "Running tests..."
nunit-console -noshadow Pyrolite.Tests/bin/Release/Pyrolite.Tests.exe
