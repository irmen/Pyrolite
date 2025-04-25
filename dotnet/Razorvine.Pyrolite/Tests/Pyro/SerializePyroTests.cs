using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Razorvine.Pyro;
using Razorvine.Pyro.Serializer;

// ReSharper disable CheckNamespace

namespace Pyrolite.Tests.Pyro;

public class SerializePyroTests
{
	public SerializePyroTests()
	{
		Config.SERPENT_INDENT=true;
	}
		
	[Fact]
	public void PyroClassesSerpent()
	{
		var ser = new SerpentSerializer();
		var uri = new PyroURI("PYRO:something@localhost:4444");
		var s = ser.serializeData(uri);
		object x = ser.deserializeData(s);
		Assert.Equal(uri, x);

		var proxy = new PyroProxy(uri)
		{
			correlation_id = Guid.NewGuid(),
			pyroHandshake = "apples",
			pyroAttrs = new HashSet<string> {"attr1", "attr2"}
		};
		s = ser.serializeData(proxy);
		x = ser.deserializeData(s);
		var proxy2 = (PyroProxy) x;
		Assert.Equal(uri.host, proxy2.hostname);
		Assert.Equal(uri.objectid, proxy2.objectid);
		Assert.Equal(uri.port, proxy2.port);
		Assert.Null(proxy2.correlation_id); // "correlation_id is not serialized on the proxy object"
		Assert.Equal(proxy.pyroHandshake, proxy2.pyroHandshake);
		Assert.Equal(2, proxy2.pyroAttrs.Count);
		Assert.Equal(proxy.pyroAttrs, proxy2.pyroAttrs);

		var ex = new PyroException("error");
		s = ser.serializeData(ex);
		x = ser.deserializeData(s);
		var ex2 = (PyroException) x;
		Assert.Equal("[PyroError] error", ex2.Message);
		Assert.Null(ex._pyroTraceback);
			
		// try another kind of pyro exception
		s = Encoding.UTF8.GetBytes("{'attributes':{'tb': 'traceback', '_pyroTraceback': ['line1', 'line2']},'__exception__':True,'args':('hello',42),'__class__':'CommunicationError'}");
		x = ser.deserializeData(s);
		ex2 = (PyroException) x;
		Assert.Equal("[CommunicationError] hello", ex2.Message);
		Assert.Equal("traceback", ex2.Data["tb"]);
		Assert.Equal("line1line2", ex2._pyroTraceback);
		Assert.Equal("CommunicationError", ex2.PythonExceptionType);
	}
		
	[Fact]
	public void TestPyroProxySerpent()
	{
		var uri = new PyroURI("PYRO:something@localhost:4444");
		var proxy = new PyroProxy(uri)
		{
			correlation_id = Guid.NewGuid(),
			pyroHandshake = "apples",
			pyroAttrs = new HashSet<string> {"attr1", "attr2"}
		};
		var data = PyroProxySerpent.ToSerpentDict(proxy);
		Assert.Equal(2, data.Count);
		Assert.Equal("Pyro5.client.Proxy", (string) data["__class__"]);
		Assert.Equal(7, ((object[])data["state"])!.Length);
				
		var proxy2 = (PyroProxy) PyroProxySerpent.FromSerpentDict(data);
		Assert.Equal(proxy.objectid, proxy2.objectid);
		Assert.Equal("apples", proxy2.pyroHandshake);
	}
		
	[Fact]
	public void TestUnserpentProxy()
	{
		var data = Encoding.UTF8.GetBytes("# serpent utf-8 python3.2\n" +
		                                  "{'state':('PYRO:Pyro.NameServer@localhost:9090',(),('count','lookup','register','ping','list','remove'),(),0.0,'hello',0),'__class__':'Pyro5.client.Proxy'}");
			
		var ser = new SerpentSerializer();
		var p = (PyroProxy) ser.deserializeData(data);
		Assert.Null(p.correlation_id);
		Assert.Equal("Pyro.NameServer", p.objectid);
		Assert.Equal("localhost", p.hostname);
		Assert.Equal(9090, p.port);
		Assert.Equal("hello", p.pyroHandshake);
		Assert.Empty(p.pyroAttrs);
		Assert.Empty(p.pyroOneway);
		Assert.Equal(6, p.pyroMethods.Count);
		var methods = new List<string> {"count", "list", "lookup", "ping", "register", "remove"};
		Assert.Equal(methods, p.pyroMethods.OrderBy(m=>m).ToList());
	}
	
	[Fact]
	public void TestBytes()
	{
		byte[] bytes = { 97, 98, 99, 100, 101, 102 };	// abcdef
		var dict = new Dictionary<string, string> {{"data", "YWJjZGVm"}, {"encoding", "base64"}};

		var bytes2 = SerpentSerializer.ToBytes(dict);
		Assert.Equal(bytes, bytes2);

		var hashtable = new Hashtable {{"data", "YWJjZGVm"}, {"encoding", "base64"}};

		bytes2 = SerpentSerializer.ToBytes(hashtable);
		Assert.Equal(bytes, bytes2);

		try {
			SerpentSerializer.ToBytes(12345);
			Assert.Fail("error expected");
		} catch (ArgumentException) {
			//
		}
	}
}


/// <summary>
/// Some tests about the peculiarities of the handshake
/// </summary>
public class HandshakeTests
{
	class MetadataProxy : PyroProxy
	{
		public MetadataProxy() : base("test", 999, "object42")
		{
		}

		public void TestMetadataHashtable(Hashtable table)
		{
			_processMetadata(table);
		}

		public void TestMetadataDictionary(IDictionary dict)
		{
			_processMetadata(dict);
		}

		public void TestMetadataGenericDict(IDictionary<object, object> dict)
		{
			_processMetadata(dict);
		}
	}
		
		
	[Fact]
	public void TestHandshakeDicts()
	{
		var proxy = new MetadataProxy();

		var hashtable = new Hashtable
		{
			{"methods", new object[] {"method1"}},
			{"attrs", new List<object> {"attr1"}}, 
			{"oneway", new HashSet<object> {"oneway1"}}
		};
		var dict = new SortedList
		{
			{"methods", new object[] {"method1"}},
			{"attrs", new List<object> {"attr1"}}, 
			{"oneway", new HashSet<object> {"oneway1"}}
		};
		var gdict = new Dictionary<object, object>
		{
			{"methods", new object[] {"method1"}},
			{"attrs", new List<object> {"attr1"}}, 
			{"oneway", new HashSet<object> {"oneway1"}}
		};

		var expectedMethods = new HashSet<string> {"method1"};
		var expectedAttrs = new HashSet<string> {"attr1"};
		var expectedOneway = new HashSet<string> {"oneway1"};

		proxy.pyroMethods.Clear();
		proxy.pyroAttrs.Clear();
		proxy.pyroOneway.Clear();
		proxy.TestMetadataHashtable(hashtable);
		Assert.Equal(expectedMethods, proxy.pyroMethods);
		Assert.Equal(expectedAttrs, proxy.pyroAttrs);
		Assert.Equal(expectedOneway, proxy.pyroOneway);

		proxy.pyroMethods.Clear();
		proxy.pyroAttrs.Clear();
		proxy.pyroOneway.Clear();
		proxy.TestMetadataDictionary(dict);
		Assert.Equal(expectedMethods, proxy.pyroMethods);
		Assert.Equal(expectedAttrs, proxy.pyroAttrs);
		Assert.Equal(expectedOneway, proxy.pyroOneway);
			
		proxy.pyroMethods.Clear();
		proxy.pyroAttrs.Clear();
		proxy.pyroOneway.Clear();
		proxy.TestMetadataDictionary(gdict);
		Assert.Equal(expectedMethods, proxy.pyroMethods);
		Assert.Equal(expectedAttrs, proxy.pyroAttrs);
		Assert.Equal(expectedOneway, proxy.pyroOneway);
			
		proxy.pyroMethods.Clear();
		proxy.pyroAttrs.Clear();
		proxy.pyroOneway.Clear();
		proxy.TestMetadataGenericDict(gdict);
		Assert.Equal(expectedMethods, proxy.pyroMethods);
		Assert.Equal(expectedAttrs, proxy.pyroAttrs);
		Assert.Equal(expectedOneway, proxy.pyroOneway);
	}
}
	
/// <summary>
/// Miscellaneous tests.
/// </summary>
public class MiscellaneousTests
{
	[Fact]
	public void TestPyroExceptionType()
	{
		var ex=new PyroException("hello");
		var type = ex.GetType();
		var prop = type.GetProperty("PythonExceptionType");
		Assert.NotNull(prop); // "pyro exception class has to have a property PythonExceptionType, it is used in constructor classes"
		prop = type.GetProperty("_pyroTraceback");
		Assert.NotNull(prop); // "pyro exception class has to have a property _pyroTraceback, it is used in constructor classes"
	}		
		
	[Fact]
	public void TestSerpentDictType()
	{
		var ht = new Hashtable {["key"] = "value"};
		var ser = new SerpentSerializer();
		var data = ser.serializeData(ht);
		var result = ser.deserializeData(data);
		Assert.IsAssignableFrom<Dictionary<object,object>>(result); // "in recent serpent versions, hashtables/dicts must be deserialized as IDictionary<object,object> rather than Hashtable"
		var dict = (IDictionary<object,object>)result;
		Assert.Equal("value", dict["key"]);
	}
}