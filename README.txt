
Pyrolite - Python Remote Objects "light"

This library allows your Java or .NET program to interface very easily with
the Python world. It uses the Pyro protocol to call methods on remote
objects. (See http://irmen.home.xs4all.nl/pyro/).

Pyrolite only implements part of the client side Pyro library,
hence its name 'lite'...  Because Pyrolite has no dependencies,
it is a much lighter way to use Pyro from Java/.NET than a solution with
jython+pyro or IronPython+Pyro would provide.
So if you don't need Pyro's full feature set, and don't require your
Java/.NET code to host Pyro objects itself, Pyrolite may be
a good choice to connect java or .NET and python.

Small piece of example code: (java)

    import net.razorvine.pyro.*;
    
    NameServerProxy ns = NameServerProxy.locateNS(null);
    PyroProxy something = new PyroProxy(ns.lookup("Your.Pyro.Object"));
    Object result = something.call("pythonmethod",42,"arguments",[1,2,3]);
    // depending on what 'pythonmethod' returns you'll have to cast
    // the result object to the appropriate type, such as HashMap for dicts, etc.


The library consists of 2 parts:
- a thin version of the client side part of Pyro.
- an almost complete implementation of Python's pickle protocol.
  (memoizing is not implemented yet in Pickler, and machine type
   bytearray unpickling of array types is not yet supported)


The source archive contains the full source, and also unit test code
and a couple of example programs in the java/test/ directory.

Pyrolite uses Pyro4 protocol only.
Pyrolite requires Java 1.5 or newer.
The .net version requires .net runtime 3.5. Also tested with Mono.
The Java source was developed using Pycharm.
The C#/.NET source was developed using monodevelop and sharpdevelop.


Pyrolite is written by Irmen de Jong (irmen@razorvine.net).
This software is distributed under the terms written in the file `LICENSE`.

