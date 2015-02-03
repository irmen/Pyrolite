#!/bin/sh
echo "Building"
. ./build-dotnet-mono.sh

echo "Running tests"

# we need to use the runtime 4.x version of nunit.
# the default nunit-console command uses the framework 2.x version.

NUNIT_CONSOLE=/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/nunit-console.exe
NUNIT="mono ${MONO_OPTIONS} ${NUNIT_CONSOLE}"

${NUNIT} -noshadow dotnet/Pyrolite.Tests/bin/Debug/Pyrolite.Tests.exe
