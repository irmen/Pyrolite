#!/bin/sh
echo "Compiling sources"
find . -name *.class | xargs rm
ecj -1.5 -g -d java/bin java/src
echo "Creating jar"
jar cf build/pyrolite.jar -C java/bin net
