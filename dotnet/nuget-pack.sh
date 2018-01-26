#!/bin/sh
echo "Building and testing..."
dotnet test -c Release Razorvine.Pyrolite/Tests

echo "\n\nCreating nuget release package..."
dotnet pack -c Release -o $(pwd)/dist Razorvine.Pyrolite/Pyrolite

echo "\n\nPackage available in dist/ directory:"
ls -l dist

echo "\nIf this is allright, publish to nuget.org with:"
echo "dotnet nuget push dist/Razorvine.Pyrolite.xxxxx.nupkg -s https://www.nuget.org -k api_key_from_nuget_org"
