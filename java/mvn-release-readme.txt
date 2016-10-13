Making a release to Sonatype Nexus/maven central:


$ mvn release:clean release:prepare release:perform


Requires version number in the pom.xml to be "x.y-SNAPSHOT".

Finalise and publish the release via https://oss.sonatype.org/#stagingRepositories


See also:
http://java.dzone.com/articles/deploy-maven-central
http://central.sonatype.org/pages/apache-maven.html#performing-a-release-deployment
