using System;
using System.IO;
using System.Text;

using NUnit.Framework;
using Razorvine.Pyro;

namespace Pyrolite.Tests.Pyro
{
	[TestFixture]
	public class SerializePyroTests
	{
		protected PyroSerializer ser;
		
		[TestFixtureSetUp]
		public void Setup()
		{
			ser = new PickleSerializer();
		}
	
		[Test]
		public void PyroClasses()
		{
			var uri = new PyroURI("PYRO:object@host:4444");
			byte[] s = this.ser.serializeData(uri);
			object x = this.ser.deserializeData(s);
			Assert.AreEqual(uri, x);

			var proxy = new PyroProxy(uri);
			s = this.ser.serializeData(proxy);
			x = this.ser.deserializeData(s);
			PyroProxy proxy2 = (PyroProxy) x;
			Assert.AreEqual(uri.host, proxy2.hostname);
			Assert.AreEqual(uri.objectid, proxy2.objectid);
			Assert.AreEqual(uri.port, proxy2.port);

			var ex = new PyroException("error");
			ex._pyroTraceback = "traceback";
			s = this.ser.serializeData(ex);
			File.WriteAllBytes("D:/dotnet.bin", s); // TODO weg
			x = this.ser.deserializeData(s);
			PyroException ex2 = (PyroException) x;
			Assert.AreEqual(ex.Message, ex2.Message);
			Assert.AreEqual("traceback", ex2._pyroTraceback);
		}
	}
	
	[TestFixture]
	public class SerializePyroTests_Serpent
	{
		protected PyroSerializer ser;

		[TestFixtureSetUp]
		public void Setup()
		{
			Config.SERPENT_INDENT=true;
			Config.SERPENT_SET_LITERALS=true;
			ser = new SerpentSerializer();
		}
		
		[TestFixtureTearDown]
		public void Teardown()
		{
			Config.SERPENT_INDENT=false;
			Config.SERPENT_SET_LITERALS=false;
		}

		[Test]
		public void PyroClasses()
		{
			var uri = new PyroURI("PYRO:something@localhost:4444");
			byte[] s = this.ser.serializeData(uri);
			object x = this.ser.deserializeData(s);
			Assert.AreEqual(uri, x);

			var proxy = new PyroProxy(uri);
			s = this.ser.serializeData(proxy);
			x = this.ser.deserializeData(s);
			PyroProxy proxy2 = (PyroProxy) x;
			Assert.AreEqual(uri.host, proxy2.hostname);
			Assert.AreEqual(uri.objectid, proxy2.objectid);
			Assert.AreEqual(uri.port, proxy2.port);

			PyroException ex = new PyroException("error");
			s = this.ser.serializeData(ex);
			x = this.ser.deserializeData(s);
			PyroException ex2 = (PyroException) x;
			Assert.AreEqual(ex.Message, ex2.Message);
			Assert.IsNull(ex._pyroTraceback);
			
			// try another kind of pyro exception
			s = Encoding.UTF8.GetBytes("{'attributes':{'tb': 'traceback', '_pyroTraceback': ['line1', 'line2']},'__exception__':True,'args':('hello',42),'__class__':'CommunicationError'}");
			x = this.ser.deserializeData(s);
			ex2 = (PyroException) x;
			Assert.AreEqual("hello", ex2.Message);
			Assert.AreEqual("traceback", ex2.Data["tb"]);
			Assert.AreEqual("line1line2", ex2._pyroTraceback);
		}
	}
}
