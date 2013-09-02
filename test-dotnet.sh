#!/bin/sh
echo "Building"
. ./build-dotnet-mono.sh

echo "Running tests"

# note: nunit-2.6.2 crashes on Mono. Stick with 2.6.1 for the time being.
NUNIT="mono ${MONO_OPTIONS} ${HOME}/Projects/NUnit-2.6.1/bin/nunit-console.exe"

${NUNIT} -framework:4.0 -noshadow -nothread dotnet/Pyrolite.Tests/bin/Debug/Pyrolite.Tests.exe
