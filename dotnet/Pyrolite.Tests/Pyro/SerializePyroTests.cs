using System;
using System.Collections.Generic;
using System.IO;
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
			Assert.AreEqual(proxy.pyroAttrs, proxy2.pyroAttrs);

			PyroException ex = new PyroException("error");
			s = ser.serializeData(ex);
			x = ser.deserializeData(s);
			PyroException ex2 = (PyroException) x;
			Assert.AreEqual(ex.Message, ex2.Message);
			Assert.IsNull(ex._pyroTraceback);
			
			// try another kind of pyro exception
			s = Encoding.UTF8.GetBytes("{'attributes':{'tb': 'traceback', '_pyroTraceback': ['line1', 'line2']},'__exception__':True,'args':('hello',42),'__class__':'CommunicationError'}");
			x = ser.deserializeData(s);
			ex2 = (PyroException) x;
			Assert.AreEqual("hello", ex2.Message);
			Assert.AreEqual("traceback", ex2.Data["tb"]);
			Assert.AreEqual("line1line2", ex2._pyroTraceback);
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
			s = pickler.serializeData(proxy);
			x = pickler.deserializeData(s);
			PyroProxy proxy2 = (PyroProxy) x;
			Assert.AreEqual(uri.host, proxy2.hostname);
			Assert.AreEqual(uri.objectid, proxy2.objectid);
			Assert.AreEqual(uri.port, proxy2.port);
			Assert.IsNull(proxy2.correlation_id, "correlation_id is not serialized on the proxy object");
			Assert.AreEqual(proxy.pyroHandshake, proxy2.pyroHandshake);
			Assert.AreEqual(proxy.pyroHmacKey, proxy2.pyroHmacKey);

			PyroException ex = new PyroException("error");
			s = pickler.serializeData(ex);
			x = pickler.deserializeData(s);
			PyroException ex2 = (PyroException) x;
			Assert.AreEqual(ex.Message, ex2.Message);
			Assert.IsNull(ex._pyroTraceback);
		}		
	}
}
