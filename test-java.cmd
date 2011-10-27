call ecj.cmd -1.5 -d java/bin -classpath java/lib/junit.jar;java/bin java/test
java -cp java\lib\junit.jar;java\lib\hamcrest.jar;java\bin org.junit.runner.JUnitCore net.razorvine.pickle.test.UnpickleStackTest
java -cp java\lib\junit.jar;java\lib\hamcrest.jar;java\bin org.junit.runner.JUnitCore net.razorvine.pickle.test.PickleUtilsTest
java -cp java\lib\junit.jar;java\lib\hamcrest.jar;java\bin org.junit.runner.JUnitCore net.razorvine.pickle.test.UnpicklerTests
java -cp java\lib\junit.jar;java\lib\hamcrest.jar;java\bin org.junit.runner.JUnitCore net.razorvine.pickle.test.PicklerTests


