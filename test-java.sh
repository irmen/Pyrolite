#!/bin/sh
echo "Compiling sources"
ecj -1.5 -d java/bin -classpath java/lib/junit.jar:java/bin java/test
echo "Running tests"
java -cp java/lib/junit.jar:java/lib/hamcrest.jar:java/bin org.junit.runner.JUnitCore net.razorvine.pickle.test.UnpickleStackTest net.razorvine.pickle.test.PickleUtilsTest net.razorvine.pickle.test.UnpickleOpcodesTests net.razorvine.pickle.test.PicklerTests net.razorvine.pickle.test.UnpicklerTests
