
Pyrolite - Python Remote Objects "light"

This library allows your Java program to interface very easily with
the Python world. It uses the Pyro protocol to call methods on remote
objects. (See http://irmen.home.xs4all.nl/pyro/).

Pyrolite only implements part of the client side Pyro library,
hence its name 'lite'...  Because Pyrolite has no dependencies,
it is a much lighter way to use Pyro from Java than a solution with
jython+pyro would provide. So if you don't need Pyro's full feature set,
and don't require your Java code to host Pyro objects itself, Pyrolite
may be a good choice to connect java and python.

Small piece of example code:

    import net.razorvine.pyro.*;
    NameServerProxy ns = NameServerProxy.locateNS(null);
    PyroProxy something = new PyroProxy(ns.lookup("Your.Pyro.Object"));
    Object result = something.call("methodname",42,"arguments",[1,2,3]);


The library consists of 2 parts:
- a thin version of the client side part of Pyro.
- an almost complete implementation of Python's pickle protocol.


The source archive contains the full source, and also unit test code
and a couple of example programs in the java/test/ directory.

Pyrolite uses Pyro4 protocol only.
Pyrolite requires Java 1.5 or newer.


Pyrolite is written by Irmen de Jong (irmen@razorvine.net).
This software is distributed under the terms written in the file `LICENSE`.
