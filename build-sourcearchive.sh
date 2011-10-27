#!/bin/sh
if [ -d build/pyrolite-export ]; then
	rm -r build/pyrolite-export
fi
svn export . build/pyrolite-export
tar czf build/pyrolite-src.tar.gz -C build pyrolite-export
rm -r build/pyrolite-export

