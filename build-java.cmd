del /S/Q *.class
call ecj.cmd -1.5 -g -d java/bin java/src
jar cf build/pyrolite.jar -C java/bin net
