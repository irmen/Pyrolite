# Pyrolite - Pyro client and Python Pickle library for Java and .NET

### You're looking at the legacy branch of this library, compatible with Pyro4 



[![saythanks](https://img.shields.io/badge/say-thanks-ff69b4.svg)](https://saythanks.io/to/irmen)
[![Build Status](https://travis-ci.org/irmen/Pyrolite.svg?branch=master)](https://travis-ci.org/irmen/Pyrolite)
[![Maven Central](https://img.shields.io/maven-central/v/net.razorvine/pyrolite.svg)](http://search.maven.org/#search|ga|1|g%3A%22net.razorvine%22%20AND%20a%3A%22pyrolite%22)
[![NuGet](https://img.shields.io/nuget/v/Razorvine.Pyrolite.svg)](https://www.nuget.org/packages/Razorvine.Pyrolite/)



Pyrolite is written by Irmen de Jong (irmen@razorvine.net).
This software is distributed under the terms written in the file `LICENSE`.


## Introduction: Pyro and Pickle

This library allows your Java or .NET program to interface very easily with
a Python program, using the Pyro protocol to call methods on remote objects
(see https://github.com/irmen/Pyro4). 

It also provides a feature complete pickle protocol implementation -read and write- 
to exchange data with Pyro/Python.  This part of the library can also be 
used in stand alone scenarios to just read and write Python pickle files.

In fact, this is what the [Apache Spark™ / PySpark](http://spark.apache.org/) and [.NET for Apache Spark™](https://dotnet.microsoft.com/apps/data/spark)
projects are using Pyrolite for!

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

The library is living in:

* **Java** packages:   ``net.razorvine.pickle``,  ``net.razorvine.pyro``
* **.NET** namespaces: ``Razorvine.Pickle``, ``Razorvine.Pyro``

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
unit tests. These include a lot of code dealing with just the pickle subsystem
as well.


## Optional (but recommended) dependency: Serpent serializer 

The default serializer is set to 'serpent' (a special serilization protocol
that I designed for the Pyro library)
For this to work, Pyrolite will require the ``Razorvine.Serpent`` assembly (.NET)
or the ``net.razorvine`` ``serpent`` artifact (serpent.jar, Java) to be available.

Serpent is a separate project (als by me), you'll have to install this dependency yourself.
You can find it at: https://github.com/irmen/Serpent
Download instructions are there as well.

If you do not supply the serpent library, Pyrolite
will still work but only with the -built in- pickle serializer.
You'll have to tell Pyrolite that you want to use pickle though by setting the ``Config.SERIALIZER`` variable accordingly. 


## About the library

The library consists of 2 parts: a thin version of the client side part of
Pyro, and a feature complete implementation of Python's pickle protocol,
including memoization. It is fully compatible with pickles from Python 2.x and
Python 3.x, and you can use it idependently from the rest of the library, to
read and write Python pickle structures.

Pickle protocol version support: reading: 0,1,2,3,4,5;  writing: 2.
Pyrolite can read all pickle protocol versions (0 to 5, so this includes
the latest additions made in Python 3.8 related to out-of-band buffers).
Pyrolite always writes pickles in protocol version 2. There are no plans on 
including protocol version 1 support. Protocols 3 and 4 contain some nice new
features which may eventually be utilized (protocol 5 is quite obscure),
but for now, only version 2 is used.


The source archive contains the full source, and also unit test code and a
couple of example programs in the java/test/ directory.

Pyrolite speaks Pyro4 protocol version 48 only (Pyro 4.38 and later).
(get an older version of Pyrolite if you need to connect to earlier Pyro versions) 

Let me know if you are interested in a version compatible with Pyro5.

The java library requires java 8 (jdk/jre 1.8) or newer to compile and run, 
and is developed using Jetbrains's IntelliJ IDEA on Linux.
The .net library targets NetStandard 2.0 (.net framework 4.6 or dotnet core 2.0), 
and is developed using Jetbrains's Rider on Linux, with dotnet core.



## Type Mapping

### Python to Java (unpickling)

The Unpickler simply returns an Object. Because Java is a statically typed
language you will have to cast that to the appropriate type. Refer to this
table to see what you can expect to receive.

PYTHON             |JAVA
-------------------|----------------------
None               | null
bool               | boolean
int                | int
long               | long or BigInteger  (depending on size)
string             | String
unicode            | String
complex            | net.razorvine.pickle.objects.ComplexNumber
datetime.date      | java.util.Calendar
datetime.datetime  | java.util.Calendar
datetime.time      | net.razorvine.pickle.objects.Time
datetime.timedelta | net.razorvine.pickle.objects.TimeDelta
float              | double   (float isn't used)
array.array        | array of appropriate primitive type (char, int, short, long, float, double)
list               | java.util.List<Object>
tuple              | Object[]
set                | java.util.Set
dict               | java.util.Map
bytes              | byte[]
bytearray          | byte[]
decimal            | BigDecimal
custom class       | Map<String, Object>  (dict with class attributes including its name in "__class__")
Pyro4.core.URI     | net.razorvine.pyro.PyroURI
Pyro4.core.Proxy   | net.razorvine.pyro.PyroProxy
Pyro4.errors.*     | net.razorvine.pyro.PyroException
Pyro4.utils.flame.FlameBuiltin    | net.razorvine.pyro.FlameBuiltin 
Pyro4.utils.flame.FlameModule     | net.razorvine.pyro.FlameModule 
Pyro4.utils.flame.RemoteInteractiveConsole   | net.razorvine.pyro.FlameRemoteConsole 


### Java to Python  (pickling)

JAVA               | PYTHON
-------------------|-----------------------
null               | None
boolean            | bool
byte               | int
char               | str/unicode (length 1)
String             | str/unicode
double             | float
float              | float
int                | int
short              | int
BigDecimal         | decimal
BigInteger         | long
any array          | array if elements are primitive type (else tuple)
Object[]           | tuple (cannot contain self-references)
byte[]             | bytearray
java.util.Date     | datetime.datetime
java.util.Calendar | datetime.datetime
java.sql.Date      | datetime.date
java.sql.Time      | datetime.time
java.sql.Timestamp | datetime.datetime
Enum               | the enum value as string
java.util.Set      | set
Map, Hashtable     | dict
Vector, Collection | list
Serializable       | treated as a JavaBean, see below.
JavaBean           | dict of the bean's public properties + ``__class__`` for the bean's type.
net.razorvine.pyro.PyroURI     | Pyro4.core.URI
net.razorvine.pyro.PyroProxy   | cannot be pickled.

### Python to .NET (unpickling)

The unpickler simply returns an object. In the case of C#, that is a statically typed
language so you will have to cast that to the appropriate type. Refer to this
table to see what you can expect to receive. Tip: you can use the 'dynamic' type 
in some places to avoid excessive type casting.


PYTHON              | .NET
--------------------|-------------
None                | null
bool                | bool
int                 | int
long                | long (c# doesn't have BigInteger so there's a limit on the size)
string              | string
unicode             | string
complex             | Razorvine.Pickle.Objects.ComplexNumber
datetime.date       | DateTime
datetime.datetime   | DateTime
datetime.time       | TimeSpan
datetime.timedelta  | TimeSpan
float               | double
array.array         | array (all kinds of element types supported)
list                | ArrayList (of objects)
tuple               | object[]
set                 | HashSet<object>
dict                | Hashtable (key=object, value=object)
bytes               | ubyte[]
bytearray           | ubyte[]
decimal             | decimal
custom class        | IDictionary<string, object>  (dict with class attributes including its name in "__class__")
Pyro4.core.URI      | Razorvine.Pyro.PyroURI
Pyro4.core.Proxy    | Razorvine.Pyro.PyroProxy
Pyro4.errors.*      | Razorvine.Pyro.PyroException
Pyro4.utils.flame.FlameBuiltin    | Razorvine.Pyro.FlameBuiltin 
Pyro4.utils.flame.FlameModule     | Razorvine.Pyro.FlameModule 
Pyro4.utils.flame.RemoteInteractiveConsole   | Razorvine.Pyro.FlameRemoteConsole 


### .NET to Python (pickling)


.NET                | PYTHON
--------------------|---------------
null                | None
boolean             | bool
byte                | byte
sbyte               | int
char                | str/unicode (length 1)
string              | str/unicode
double              | float
float               | float
int/short/sbyte     | int
uint/ushort/byte    | int
decimal             | decimal
byte[]              | bytearray
primitivetype[]     | array
object[]            | tuple  (cannot contain self-references)
DateTime            | datetime.datetime
TimeSpan            | datetime.timedelta
Enum                | just the enum value as string
HashSet             | set
Map, Hashtable      | dict
Collection          | list
Enumerable          | list
object with public properties      | dictionary of those properties + __class__
anonymous class type        | dictonary of the public properties
Razorvine.Pyro.PyroURI      | Pyro4.core.URI
Razorvine.Pyro.PyroProxy    | cannot be pickled.


## Dealing with exceptions

Pyrolite also maps Python exceptions that may occur in the remote object. It
has a rather simplistic approach:

*all* exceptions, including the Pyro ones (Pyro4.errors.*), are converted to
``PyroException`` objects. ``PyroException`` is a normal Java or C# exception type,
and it will be thrown as a normal exception in your program.  The message
string is taken from the original exception. The remote traceback string is
available on the ``PyroException`` object in the ``_pyroTraceback`` field.


## Security warning when using pickle

Pyrolite can talk to Pyro using the pickle serialization protocol.
Because pickle is not enabled by default in Pyro,
you must configure your Pyro server to allow the pickle serializer in this case.
(See the Pyro documentation on how to do this) 
*IF YOU DO THIS, YOUR PYRO SERVER CAN BE VULNERABLE TO
REMOTE ARBITRARY CODE EXECUTION* due to the well-known pickle security problem,
so keep that in mind.

Note that your .NET or Java client code is perfectly safe! The unpickler
implementation in Pyrolite doesn't randomly construct arbitrary objects and is
safe to use for parsing data from the network.

