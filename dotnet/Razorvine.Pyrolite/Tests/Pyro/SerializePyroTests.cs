using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Razorvine.Pyro;
// ReSharper disable CheckNamespace

namespace Pyrolite.Tests.Pyro
{
	public class SerializePyroTests
	{
		public SerializePyroTests()
		{
			Config.SERPENT_INDENT=true;
			Config.SERPENT_SET_LITERALS=true;
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
				pyroHmacKey = Encoding.UTF8.GetBytes("secret"),
				pyroAttrs = new HashSet<string> {"attr1", "attr2"}
			};
			s = ser.serializeData(proxy);
			x = ser.deserializeData(s);
			PyroProxy proxy2 = (PyroProxy) x;
			Assert.Equal(uri.host, proxy2.hostname);
			Assert.Equal(uri.objectid, proxy2.objectid);
			Assert.Equal(uri.port, proxy2.port);
			Assert.Null(proxy2.correlation_id); // "correlation_id is not serialized on the proxy object"
			Assert.Equal(proxy.pyroHandshake, proxy2.pyroHandshake);
			Assert.Equal(proxy.pyroHmacKey, proxy2.pyroHmacKey);
			Assert.Equal(2, proxy2.pyroAttrs.Count);
			Assert.Equal(proxy.pyroAttrs, proxy2.pyroAttrs);

			PyroException ex = new PyroException("error");
			s = ser.serializeData(ex);
			x = ser.deserializeData(s);
			PyroException ex2 = (PyroException) x;
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
		public void PyroProxySerpent()
		{
			PyroURI uri = new PyroURI("PYRO:something@localhost:4444");
			PyroProxy proxy = new PyroProxy(uri)
			{
				correlation_id = Guid.NewGuid(),
				pyroHandshake = "apples",
				pyroHmacKey = Encoding.UTF8.GetBytes("secret"),
				pyroAttrs = new HashSet<string> {"attr1", "attr2"}
			};
			var data = PyroProxyPickler.ToSerpentDict(proxy);
			Assert.Equal(2, data.Count);
			Assert.Equal("Pyro4.core.Proxy", data["__class__"]);
			Assert.Equal(8, ((object[])data["state"]).Length);
				
			PyroProxy proxy2 = (PyroProxy) PyroProxyPickler.FromSerpentDict(data);
			Assert.Equal(proxy.objectid, proxy2.objectid);
			Assert.Equal("apples", proxy2.pyroHandshake);
		}
		
		[Fact]
		public void UnserpentProxy()
		{
			var data = Encoding.UTF8.GetBytes("# serpent utf-8 python3.2\n" +
			                                     "{'state':('PYRO:Pyro.NameServer@localhost:9090',(),('count','lookup','register','ping','list','remove'),(),0.0,'b64:c2VjcmV0','hello',0),'__class__':'Pyro4.core.Proxy'}");
			
			SerpentSerializer ser = new SerpentSerializer();
			PyroProxy p = (PyroProxy) ser.deserializeData(data);
			Assert.Null(p.correlation_id);
			Assert.Equal("Pyro.NameServer", p.objectid);
			Assert.Equal("localhost", p.hostname);
			Assert.Equal(9090, p.port);
			Assert.Equal("hello", p.pyroHandshake);
			Assert.Equal(Encoding.UTF8.GetBytes("secret"), p.pyroHmacKey);
			Assert.Equal(0, p.pyroAttrs.Count);
			Assert.Equal(0, p.pyroOneway.Count);
			Assert.Equal(6, p.pyroMethods.Count);
			var methods = new List<string> {"count", "list", "lookup", "ping", "register", "remove"};
			Assert.Equal(methods, p.pyroMethods.OrderBy(m=>m).ToList());
		}
	
		[Fact]
		public void PyroClassesPickle()
		{
			var pickler = new PickleSerializer();
			var uri = new PyroURI("PYRO:something@localhost:4444");
			var s = pickler.serializeData(uri);
			object x = pickler.deserializeData(s);
			Assert.Equal(uri, x);

			var proxy = new PyroProxy(uri)
			{
				correlation_id = Guid.NewGuid(),
				pyroHandshake = "apples",
				pyroHmacKey = Encoding.UTF8.GetBytes("secret"),
				pyroAttrs = new HashSet<string> {"attr1", "attr2"}
			};
			s = pickler.serializeData(proxy);
			x = pickler.deserializeData(s);
			PyroProxy proxy2 = (PyroProxy) x;
			Assert.Equal(uri.host, proxy2.hostname);
			Assert.Equal(uri.objectid, proxy2.objectid);
			Assert.Equal(uri.port, proxy2.port);
			Assert.Null(proxy2.correlation_id); // "correlation_id is not serialized on the proxy object"
			Assert.Equal(proxy.pyroHandshake, proxy2.pyroHandshake);
			Assert.Equal(proxy.pyroHmacKey, proxy2.pyroHmacKey);
			Assert.Equal(2, proxy2.pyroAttrs.Count);
			Assert.Equal(proxy.pyroAttrs, proxy2.pyroAttrs);

			PyroException ex = new PyroException("error");
			s = pickler.serializeData(ex);
			x = pickler.deserializeData(s);
			PyroException ex2 = (PyroException) x;
			Assert.Equal("[Pyro4.errors.PyroError] error", ex2.Message);
			Assert.Null(ex._pyroTraceback);
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
	        	Assert.True(false, "error expected");
	        } catch (ArgumentException) {
	        	//
	        }
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
			Hashtable ht = new Hashtable {["key"] = "value"};
			var ser = new SerpentSerializer();
			var data = ser.serializeData(ht);
			var result = ser.deserializeData(data);
			Assert.IsAssignableFrom<Dictionary<object,object>>(result); // "in recent serpent versions, hashtables/dicts must be deserialized as IDictionary<object,object> rather than Hashtable"
			var dict = (IDictionary<object,object>)result;
			Assert.Equal("value", dict["key"]);
		}
	}
}
