#!/bin/sh
echo "Running tests"
# find . -name *.exe | grep bin/.*Tests.exe | xargs nunit-console
# mono ./dotnet/Pyrolite.Tests/bin/Debug/Pyrolite.Tests.exe
nunit-console -noshadow -nothread dotnet/Pyrolite.Tests/bin/Debug/Pyrolite.Tests.exe
