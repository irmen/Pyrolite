#!/bin/sh
rm -r build/pyrolite-export
svn export . build/pyrolite-export
tar czf build/pyrolite-src.tar.gz -C build pyrolite-export
rm -r build/pyrolite-export

