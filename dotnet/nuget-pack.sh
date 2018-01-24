#!/bin/sh
echo "Building and testing..."
. ./test.sh

echo "Creating nuget release package..."
dotnet pack -c Release Razorvine.Pyrolite/Pyrolite
