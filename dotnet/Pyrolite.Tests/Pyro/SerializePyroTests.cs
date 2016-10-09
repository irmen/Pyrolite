using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using Razorvine.Pyro;

namespace Pyrolite.Tests.Pyro
{
	[TestFixture]
	public class SerializePyroTests
	{
		[TestFixtureSetUp]
		public void Setup()
		{
			Config.SERPENT_INDENT=true;
			Config.SERPENT_SET_LITERALS=true;
		}
		
		[TestFixtureTearDown]
		public void Teardown()
		{
			Config.SERPENT_INDENT=false;
			Config.SERPENT_SET_LITERALS=false;
		}

		[Test]
		public void PyroClassesSerpent()
		{
			var ser = new SerpentSerializer();
			var uri = new PyroURI("PYRO:something@localhost:4444");
			byte[] s = ser.serializeData(uri);
			object x = ser.deserializeData(s);
			Assert.AreEqual(uri, x);

			var proxy = new PyroProxy(uri);
			proxy.correlation_id = Guid.NewGuid();
			proxy.pyroHandshake = "apples";
			proxy.pyroHmacKey = Encoding.UTF8.GetBytes("secret");
			proxy.pyroAttrs = new HashSet<string>();
			proxy.pyroAttrs.Add("attr1");
			proxy.pyroAttrs.Add("attr2");
			s = ser.serializeData(proxy);
			x = ser.deserializeData(s);
			PyroProxy proxy2 = (PyroProxy) x;
			Assert.AreEqual(uri.host, proxy2.hostname);
			Assert.AreEqual(uri.objectid, proxy2.objectid);
			Assert.AreEqual(uri.port, proxy2.port);
			Assert.IsNull(proxy2.correlation_id, "correlation_id is not serialized on the proxy object");
			Assert.AreEqual(proxy.pyroHandshake, proxy2.pyroHandshake);
			Assert.AreEqual(proxy.pyroHmacKey, proxy2.pyroHmacKey);
			Assert.AreEqual(2, proxy2.pyroAttrs.Count);
			Assert.AreEqual(proxy.pyroAttrs, proxy2.pyroAttrs);

			PyroException ex = new PyroException("error");
			s = ser.serializeData(ex);
			x = ser.deserializeData(s);
			PyroException ex2 = (PyroException) x;
			Assert.AreEqual("[PyroError] error", ex2.Message);
			Assert.IsNull(ex._pyroTraceback);
			
			// try another kind of pyro exception
			s = Encoding.UTF8.GetBytes("{'attributes':{'tb': 'traceback', '_pyroTraceback': ['line1', 'line2']},'__exception__':True,'args':('hello',42),'__class__':'CommunicationError'}");
			x = ser.deserializeData(s);
			ex2 = (PyroException) x;
			Assert.AreEqual("[CommunicationError] hello", ex2.Message);
			Assert.AreEqual("traceback", ex2.Data["tb"]);
			Assert.AreEqual("line1line2", ex2._pyroTraceback);
			Assert.AreEqual("CommunicationError", ex2.PythonExceptionType);
		}
		
		[Test]
		public void PyroProxySerpent()
		{
			PyroURI uri = new PyroURI("PYRO:something@localhost:4444");
			PyroProxy proxy = new PyroProxy(uri);
			proxy.correlation_id = Guid.NewGuid();
			proxy.pyroHandshake = "apples";
			proxy.pyroHmacKey = Encoding.UTF8.GetBytes("secret");
			proxy.pyroAttrs = new HashSet<string>();
			proxy.pyroAttrs.Add("attr1");
			proxy.pyroAttrs.Add("attr2");
			var data = PyroProxyPickler.ToSerpentDict(proxy);
			Assert.AreEqual(2, data.Count);
			Assert.AreEqual("Pyro4.core.Proxy", data["__class__"]);
			Assert.AreEqual(8, ((object[])data["state"]).Length);
				
			PyroProxy proxy2 = (PyroProxy) PyroProxyPickler.FromSerpentDict(data);
			Assert.AreEqual(proxy.objectid, proxy2.objectid);
			Assert.AreEqual("apples", proxy2.pyroHandshake);
		}
		
		[Test]
		public void UnserpentProxy()
		{
			byte[] data = Encoding.UTF8.GetBytes("# serpent utf-8 python3.2\n" +
			                                     "{'state':('PYRO:Pyro.NameServer@localhost:9090',(),('count','lookup','register','ping','list','remove'),(),0.0,'b64:c2VjcmV0','hello',0),'__class__':'Pyro4.core.Proxy'}");
			
			SerpentSerializer ser = new SerpentSerializer();
			PyroProxy p = (PyroProxy) ser.deserializeData(data);
			Assert.IsNull(p.correlation_id);
			Assert.AreEqual("Pyro.NameServer", p.objectid);
			Assert.AreEqual("localhost", p.hostname);
			Assert.AreEqual(9090, p.port);
			Assert.AreEqual("hello", p.pyroHandshake);
			Assert.AreEqual(Encoding.UTF8.GetBytes("secret"), p.pyroHmacKey);
			Assert.AreEqual(0, p.pyroAttrs.Count);
			Assert.AreEqual(0, p.pyroOneway.Count);
			Assert.AreEqual(6, p.pyroMethods.Count);
			ISet<string> methods = new HashSet<string>();
			methods.Add("ping");
			methods.Add("count");
			methods.Add("lookup");
			methods.Add("list");
			methods.Add("register");
			methods.Add("remove");
			CollectionAssert.AreEquivalent(methods, p.pyroMethods);
		}
	
		[Test]
		public void PyroClassesPickle()
		{
			var pickler = new PickleSerializer();
			var uri = new PyroURI("PYRO:something@localhost:4444");
			byte[] s = pickler.serializeData(uri);
			object x = pickler.deserializeData(s);
			Assert.AreEqual(uri, x);

			var proxy = new PyroProxy(uri);
			proxy.correlation_id = Guid.NewGuid();
			proxy.pyroHandshake = "apples";
			proxy.pyroHmacKey = Encoding.UTF8.GetBytes("secret");
			proxy.pyroAttrs = new HashSet<string>();
			proxy.pyroAttrs.Add("attr1");
			proxy.pyroAttrs.Add("attr2");
			s = pickler.serializeData(proxy);
			x = pickler.deserializeData(s);
			PyroProxy proxy2 = (PyroProxy) x;
			Assert.AreEqual(uri.host, proxy2.hostname);
			Assert.AreEqual(uri.objectid, proxy2.objectid);
			Assert.AreEqual(uri.port, proxy2.port);
			Assert.IsNull(proxy2.correlation_id, "correlation_id is not serialized on the proxy object");
			Assert.AreEqual(proxy.pyroHandshake, proxy2.pyroHandshake);
			Assert.AreEqual(proxy.pyroHmacKey, proxy2.pyroHmacKey);
			Assert.AreEqual(2, proxy2.pyroAttrs.Count);
			Assert.AreEqual(proxy.pyroAttrs, proxy2.pyroAttrs);

			PyroException ex = new PyroException("error");
			s = pickler.serializeData(ex);
			x = pickler.deserializeData(s);
			PyroException ex2 = (PyroException) x;
			Assert.AreEqual("[Pyro4.errors.PyroError] error", ex2.Message);
			Assert.IsNull(ex._pyroTraceback);
		}		
	}

	/// <summary>
	/// Miscellaneous tests.
	/// </summary>
	[TestFixture]
	public class MiscellaneousTests {
		[Test]
		public void testPyroExceptionType()
		{
			var ex=new PyroException("hello");
			var type = ex.GetType();
			var prop = type.GetProperty("PythonExceptionType");
			Assert.IsNotNull(prop, "pyro exception class has to have a property PythonExceptionType, it is used in constructor classes");
			prop = type.GetProperty("_pyroTraceback");
			Assert.IsNotNull(prop, "pyro exception class has to have a property _pyroTraceback, it is used in constructor classes");
		}
	}}
