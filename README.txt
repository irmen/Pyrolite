
Pyrolite - Python Remote Objects "light"

  Pyrolite is written by Irmen de Jong (irmen@razorvine.net).
  This software is distributed under the terms written in the file `LICENSE`.


Contents:
    1. INTRODUCTION
    2. THE LIBRARY
    3. TYPE MAPPINGS
    4. EXCEPTIONS


1. INTRODUCTION
---------------------

This library allows your Java or .NET program to interface very easily with
the Python world. It uses the Pyro protocol to call methods on remote
objects. (See http://irmen.home.xs4all.nl/pyro/).
Pyrolite uses its native pickle protocol implementation to exchange data
with Python. 

Pyrolite only implements part of the client side Pyro library,
hence its name 'lite'...  Because Pyrolite has no dependencies,
it is a much lighter way to use Pyro from Java/.NET than a solution with
jython+pyro or IronPython+Pyro would provide.
So if you don't need Pyro's full feature set, and don't require your
Java/.NET code to host Pyro objects itself, Pyrolite may be
a good choice to connect java or .NET and python.

Java packages:   net.razorvine.pickle,  net.razorvine.pyro
.NET namespaces: Razorvine.Pickle, Razorvine.Pyro

Small piece of example code in Java:

    import net.razorvine.pyro.*;
    
    NameServerProxy ns = NameServerProxy.locateNS(null);
    PyroProxy remoteobject = new PyroProxy(ns.lookup("Your.Pyro.Object"));
    Object result = remoteobject.call("pythonmethod", 42, "hello", new int[]{1,2,3});
    String message = (String)result;  // cast to the type that 'pythonmethod' returns
    System.out.println("result message="+message);
    remoteobject.close();
    ns.close();
    
Same piece of example code in C#:

    using Razorvine.Pyro;
    
    using( NameServerProxy ns = NameServerProxy.locateNS(null) )
    {
        using( PyroProxy something = new PyroProxy(ns.lookup("Your.Pyro.Object")) )
        {
            object result = something.call("pythonmethod", 42, "hello", new int[]{1,2,3});
            string message = (string)result;  // cast to the type that 'pythonmethod' returns
            Console.WriteLine("result message="+message);
        }
    }
        
More examples can be found in the examples directory.


2. THE LIBRARY
---------------------

The library consists of 2 parts:
- a thin version of the client side part of Pyro.
- an almost complete implementation of Python's pickle protocol.
  (Only memoizing is not implemented yet in the Pickler).
  It is fully compatible with pickles from Python 2.x and Python 3.x.
  (You can use this independently from the rest of the library if you wish).


The source archive contains the full source, and also unit test code
and a couple of example programs in the java/test/ directory.

Pyrolite uses Pyro4 protocol only.
Pyrolite requires Java 1.5 or newer.
The .net version requires .net runtime 3.5. Created and tested with Mono.
The Java source was developed using Pycharm.
The C#/.NET source was developed using mono, monodevelop and sharpdevelop.



3. TYPE MAPPINGS
---------------------

Pyrolite does the following type mappings:

PYTHON    ---->     JAVA
------              ----
None                null
bool                boolean
int                 int
long                long or BigInteger  (depending on size)
string              String
unicode             String
complex             net.razorvine.pickle.objects.ComplexNumber
datetime.date       java.util.Calendar
datetime.datetime   java.util.Calendar
datetime.time       java.util.Calendar
datetime.timedelta  net.razorvine.pickle.objects.TimeDelta
float               double   (float isn't used) 
array.array         array of appropriate primitive type (char, int, short, long, float, double)
list                java.util.List<Object>
tuple               Object[]
set                 java.util.Set
dict                java.util.Map
bytes               byte[]
bytearray           byte[]
decimal             BigDecimal    
custom class        Map<String, Object>  (dict with class attributes including its name in "__class__")
Pyro4.core.URI      net.razorvine.pyro.PyroURI
Pyro4.core.Proxy    net.razorvine.pyro.PyroProxy
Pyro4.errors.*      net.razorvine.pyro.PyroException
Pyro4.utils.flame.FlameBuiltin     net.razorvine.pyro.FlameBuiltin 
Pyro4.utils.flame.FlameModule      net.razorvine.pyro.FlameModule 
Pyro4.utils.flame.RemoteInteractiveConsole    net.razorvine.pyro.FlameRemoteConsole 

The unpickler simply returns an Object. Because Java is a statically
typed language you will have to cast that to the appropriate type.
Refer to this table to see what you can expect to receive.
                    

JAVA     ---->      PYTHON
-----               ------
null                None
boolean             bool
byte                int
char                str/unicode (length 1)
String              str/unicode
double              float
float               float
int                 int
short               int
BigDecimal          decimal
BigInteger          long
any array           array if elements are primitive type (else tuple)
Object[]            tuple
byte[]              bytearray
java.util.Date      datetime.datetime
java.util.Calendar  datetime.datetime
Enum                the enum value as string
java.util.Set       set
Map, Hashtable      dict
Vector, Collection  list
Serializable        treated as a JavaBean, see below.
JavaBean            dict of the bean's public properties + __class__ for the bean's type.
net.razorvine.pyro.PyroURI      Pyro4.core.URI
net.razorvine.pyro.PyroProxy    cannot be pickled.


PYTHON      ---->    C#
------              ----
None                null
bool                bool
int                 int
long                long (c# doesn't have BigInteger so there's a limit on the size)
string              string
unicode             string
complex             Razorvine.Pickle.Objects.ComplexNumber
datetime.date       DateTime
datetime.datetime   DateTime
datetime.time       TimeSpan
datetime.timedelta  TimeSpan
float               double
array.array         array (all kinds of element types supported)
list                ArrayList (of objects)
tuple               object[]
set                 HashSet<object>
dict                Hashtable (key=object, value=object)
bytes               ubyte[]
bytearray           ubyte[]
decimal             decimal
custom class        IDictionary<string, object>  (dict with class attributes including its name in "__class__")
Pyro4.core.URI      Razorvine.Pyro.PyroURI
Pyro4.core.Proxy    Razorvine.Pyro.PyroProxy
Pyro4.errors.*      Razorvine.Pyro.PyroException
Pyro4.utils.flame.FlameBuiltin     Razorvine.Pyro.FlameBuiltin 
Pyro4.utils.flame.FlameModule      Razorvine.Pyro.FlameModule 
Pyro4.utils.flame.RemoteInteractiveConsole    Razorvine.Pyro.FlameRemoteConsole 

The unpickler simply returns an object. Because C# is a statically
typed language you will have to cast that to the appropriate type.
Refer to this table to see what you can expect to receive.
TIP: if you are using C# 4.0 you can use the 'dynamic' type in some
places to avoid excessive type casting.


  C#      ---->     PYTHON
------              -------
null                None
boolean             bool
byte                byte
sbyte               int
char                str/unicode (length 1)
string              str/unicode
double              float
float               float
int/short/sbyte     int
uint/ushort/byte    int
decimal             decimal
byte[]              bytearray
primitivetype[]     array
object[]            tuple
DateTime            datetime.datetime
TimeSpan            datetime.timedelta
Enum                just the enum value as string
HashSet             set
Map, Hashtable      dict
Collection          list
Enumerable          list
object with public properties      dictionary of those properties + __class__
anonymous class type        dictonary of the public properties
Razorvine.Pyro.PyroURI      Pyro4.core.URI
Razorvine.Pyro.PyroProxy    cannot be pickled.


4. EXCEPTIONS
---------------------

Pyrolite also maps Python exceptions that may occur in the remote object.
It has a rather simplistic approach:

*all* exceptions, including the Pyro ones (Pyro4.errors.*), are converted
to PyroException objects. PyroException is a normal Java or C# exception type,
and it will be thrown as a normal exception in your program. 
The message string is taken from the original exception. The remote traceback
string is available on the PyroException object in the _pyroTraceback field. 

