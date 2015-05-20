name := "pyrolite"

organization := "com.github.irmen"

version := "4.5-SNAPSHOT"

libraryDependencies += "com.novocode" % "junit-interface" % "0.11" % "test"

unmanagedBase := baseDirectory.value / "java" / "lib"

scalacOptions += "-target:jvm-1.6"

crossPaths := false

autoScalaLibrary := false

javaSource in Compile := baseDirectory.value / "java" / "src"

javaSource in Test := baseDirectory.value / "java" / "test"

testOptions += Tests.Argument(TestFrameworks.JUnit, "-q", "-v")
