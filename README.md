# Pyrolite - Pyro5 client library for Java and .NET

[![saythanks](https://img.shields.io/badge/say-thanks-ff69b4.svg)](https://saythanks.io/to/irmen)
[![Build Status](https://travis-ci.org/irmen/Pyrolite.svg?branch=master)](https://travis-ci.org/irmen/Pyrolite)
[![Maven Central](https://img.shields.io/maven-central/v/net.razorvine/pyrolite.svg)](http://search.maven.org/#search|ga|1|g%3A%22net.razorvine%22%20AND%20a%3A%22pyrolite%22)
[![NuGet](https://img.shields.io/nuget/v/Razorvine.Pyrolite.svg)](https://www.nuget.org/packages/Razorvine.Pyrolite/)



Pyrolite is written by Irmen de Jong (irmen@razorvine.net).
This software is distributed under the terms written in the file `LICENSE`.


## Introduction: Pyro

This library allows your Java or .NET program to interface very easily with
a Python program, using the Pyro protocol to call methods on remote objects
(see https://github.com/irmen/Pyro5). 

Pyrolite only implements a part of the *client side* Pyro library, hence its name
'lite'...  For the full Pyro experience (and the ability to host servers and
expose these via Pyro) you have to run Pyro itself in Python.
But if you don't need Pyro's full feature set, and don't require
your Java/.NET code to host Pyro objects but rather only call them,
Pyrolite could be a good choice to connect Java or .NET and Python!


## Installation and usage

Precompiled libraries are available:

* **Java**: from Maven, group id ``net.razorvine`` artifact id ``pyrolite``.
* **.NET**: nuget Razorvine.Pyrolite; https://www.nuget.org/packages/Razorvine.Pyrolite/


Some Java example code:

    import net.razorvine.pyro.*;
    
    NameServerProxy ns = NameServerProxy.locateNS(null);
    PyroProxy remoteobject = new PyroProxy(ns.lookup("Your.Pyro.Object"));
    Object result = remoteobject.call("pythonmethod", 42, "hello", new int[]{1,2,3});
    String message = (String)result;  // cast to the type that 'pythonmethod' returns
    System.out.println("result message="+message);
    remoteobject.close();
    ns.close();
    

Some C# example code:

    using Razorvine.Pyro;
    
    using( NameServerProxy ns = NameServerProxy.locateNS(null) )
    {
        // this uses the statically typed proxy class:
        using( PyroProxy something = new PyroProxy(ns.lookup("Your.Pyro.Object")) )
        {
            object result = something.call("pythonmethod", 42, "hello", new int[]{1,2,3});
            string message = (string)result;  // cast to the type that 'pythonmethod' returns
            Console.WriteLine("result message="+message);
            result = something.getattr("remote_attribute");
            Console.WriteLine("remote attribute="+result);
        }
        
        // but you can also use it as a dynamic!
        using( dynamic something = new PyroProxy(ns.lookup("Your.Pyro.Object")) )
        {
            object result = something.pythonmethod(42, "hello", new int[]{1,2,3});
            string message = (string)result;  // cast to the type that 'pythonmethod' returns
            Console.WriteLine("result message="+message);
            result = something.remote_attribute;
            Console.WriteLine("remote attribute="+result);
        }
    }
        
More examples can be found in the examples directory. You could also study the
unit tests. 


## "Where is Pickle?"

Until version 5.0, Pyrolite included a pickle protocol implementation that allowed your Java or .NET code
to read and write Python pickle files (pickle is Python's serialization format).
From 5.0 onwards, this is no longer included because Pyro5 no longer uses pickle.

If you still want to read or write pickled data, have a look at the now separate pickle library:
https://github.com/irmen/pickle


## Required dependency: Serpent serializer 

The serializer used is 'serpent' (a special serilization protocol that I designed for the Pyro library)
So this requires the ``Razorvine.Serpent`` assembly (.NET)
or the ``net.razorvine`` ``serpent`` artifact (serpent.jar, Java) to be available.

Serpent is a separate project (als by me), you'll have to install this dependency yourself.
You can find it at: https://github.com/irmen/Serpent
Download instructions are there as well.



## Dealing with exceptions

Pyrolite also maps Python exceptions that may occur in the remote object. It
has a rather simplistic approach:

*all* exceptions, including the Pyro ones (Pyro4.errors.*), are converted to
``PyroException`` objects. ``PyroException`` is a normal Java or C# exception type,
and it will be thrown as a normal exception in your program.  The message
string is taken from the original exception. The remote traceback string is
available on the ``PyroException`` object in the ``_pyroTraceback`` field.
