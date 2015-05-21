name := "pyrolite"
organization := "net.razorvine"
version := "4.6-SNAPSHOT"
description := "Pyro (Python Remote Objects) client library and Pickle"
licenses += ("MIT", url("http://www.opensource.org/licenses/mit-license.php"))

libraryDependencies += "com.novocode" % "junit-interface" % "0.11" % "test"
libraryDependencies += "net.razorvine" % "serpent" % "1.11"

scalacOptions += "-target:jvm-1.6"
crossPaths := false
autoScalaLibrary := false

javaSource in Compile := baseDirectory.value / "src" / "main" / "java"
javaSource in Test := baseDirectory.value / "src" / "test" / "java"
testOptions += Tests.Argument(TestFrameworks.JUnit, "-q", "-v")


javacOptions in (Compile,doc) += "-Xdoclint:none"

publishMavenStyle := true

publishTo := {
  val nexus = "https://oss.sonatype.org/"
  if (isSnapshot.value)
    Some("snapshots" at nexus + "content/repositories/snapshots") 
  else
    Some("releases"  at nexus + "service/local/staging/deploy/maven2")
}
credentials += Credentials(Path.userHome / ".ivy2" / ".credentials")
