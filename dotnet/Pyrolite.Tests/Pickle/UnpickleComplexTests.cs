/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework;
using Razorvine.Pickle;
using Razorvine.Pickle.Objects;
using Razorvine.Pyro;

namespace Pyrolite.Tests.Pickle
{

/// <summary>
/// tests for more complex pickling/unpickling such as Proxy and URI objects.
/// </summary>
[TestFixture]
public class UnpickleComplexTests
{
	object U(string strdata) 
	{
		return U(PickleUtils.str2bytes(strdata));
	}
	object U(byte[] data) 
	{
		using(Unpickler u=new Unpickler())
		{
			return u.loads(data);
		}
	}

	[TestFixtureSetUp]
	public void setUp() {
	}

	[TestFixtureTearDown]
	public void tearDown() {
	}

	[Test]
	public void testPickleUnpickleURI() {
		PyroURI uri=new PyroURI("PYRO:test@localhost:9999");
		PyroSerializer ser = new PickleSerializer();
		byte[] pickled_uri=ser.serializeData(uri);
		PyroURI uri2=(PyroURI) ser.deserializeData(pickled_uri);
		Assert.AreEqual(uri,uri2);

		uri=new PyroURI();
		pickled_uri=ser.serializeData(uri);
		uri2=(PyroURI) ser.deserializeData(pickled_uri);
		Assert.AreEqual(uri,uri2);
	}

	[Test]
	public void testPickleUnpickleProxy() {
		PyroProxy proxy=new PyroProxy("hostname",9999,"objectid");
		proxy.pyroHmacKey = Encoding.UTF8.GetBytes("secret");
		proxy.pyroHandshake = "apples";
		PyroSerializer ser = new PickleSerializer();
		byte[] pickled_proxy=ser.serializeData(proxy);
		PyroProxy result = (PyroProxy) ser.deserializeData(pickled_proxy);
		Assert.AreEqual(proxy.hostname, result.hostname);
		Assert.AreEqual(proxy.objectid, result.objectid);
		Assert.AreEqual(proxy.port, result.port);
		Assert.AreEqual(Encoding.UTF8.GetBytes("secret"), result.pyroHmacKey);
		Assert.AreEqual("apples", result.pyroHandshake);
	}

	[Test]
	public void testUnpickleRealProxy2() {
		byte[] pickled_proxy=File.ReadAllBytes("pickled_nameserver_proxy_p2.dat");
		unpickleRealProxy(pickled_proxy);
	}

	[Test]
	public void testUnpickleRealProxy2old() {
		byte[] pickled_proxy=File.ReadAllBytes("pickled_nameserver_proxy_p2old.dat");
		unpickleRealProxy(pickled_proxy);
	}

	[Test]
	public void testUnpickleRealProxy3() {
		byte[] pickled_proxy=File.ReadAllBytes("pickled_nameserver_proxy_p3.dat");
		unpickleRealProxy(pickled_proxy);
	}

	[Test]
	public void testUnpickleRealProxy4() {
		byte[] pickled_proxy=File.ReadAllBytes("pickled_nameserver_proxy_p4.dat");
		unpickleRealProxy(pickled_proxy);
	}

	private void unpickleRealProxy(byte[] pickled_proxy) {
		PyroSerializer ser = new PickleSerializer();
		PyroProxy proxy=(PyroProxy)ser.deserializeData(pickled_proxy);
		Assert.AreEqual("Pyro.NameServer",proxy.objectid);
		Assert.AreEqual("localhost",proxy.hostname);
		Assert.AreEqual(9090,proxy.port);
		Assert.AreEqual("hello", proxy.pyroHandshake);
		CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("secret"), proxy.pyroHmacKey);
		ISet<string> expectedSet = new HashSet<string>();
		CollectionAssert.AreEquivalent(expectedSet, proxy.pyroAttrs);
		expectedSet.Clear();
		expectedSet.Add("lookup");
		expectedSet.Add("ping");
		expectedSet.Add("register");
		expectedSet.Add("remove");
		expectedSet.Add("list");
		expectedSet.Add("count");
		expectedSet.Add("set_metadata");
		proxy.pyroMethods.ExceptWith(expectedSet);
		Assert.AreEqual(0, proxy.pyroMethods.Count, "something is wrong with the expected exposed methods");
		expectedSet = new HashSet<string>();
		CollectionAssert.AreEquivalent(expectedSet, proxy.pyroOneway);
	}

	[Test]
	public void testUnpickleMemo() {
		// the pickle is of the following list: [65, 'hello', 'hello', {'recurse': [...]}, 'hello']
		// i.e. the 4th element is a dict referring back to the list itself and the 'hello' strings are reused
		byte[] pickle = new byte[]
			{128, 2, 93, 113, 0, 40, 75, 65, 85, 5, 104, 101, 108, 108, 111, 113, 1, 104, 1, 125, 113, 2,
			85, 7, 114, 101, 99, 117, 114, 115, 101, 113, 3, 104, 0, 115, 104, 1, 101, 46};
		ArrayList a = (ArrayList) U(pickle);
		Assert.AreEqual(5, a.Count);
		Assert.AreEqual(65, a[0]);
		Assert.AreEqual("hello", a[1]);
		Assert.AreSame(a[1], a[2]);
		Assert.AreSame(a[1], a[4]);
		Hashtable h = (Hashtable) a[3];
		Assert.AreSame(a, h["recurse"]);
	}
		 
	
	[Test]
	public void testUnpickleUnsupportedClass() {
		// an unsupported class is mapped to a dictionary containing the class's attributes, and a __class__ attribute with the name of the class
		byte[] pickled = new byte[] {128, 2, 99, 95, 95, 109, 97, 105, 110, 95, 95, 10, 67, 117, 115, 116, 111, 109, 67, 108, 97, 115, 115, 10, 113, 0, 41, 129, 113, 1, 125, 113, 2, 40, 85, 3, 97, 103, 101, 113, 3, 75, 34, 85, 6, 118, 97, 108, 117, 101, 115, 113, 4, 93, 113, 5, 40, 75, 1, 75, 2, 75, 3, 101, 85, 4, 110, 97, 109, 101, 113, 6, 85, 5, 72, 97, 114, 114, 121, 113, 7, 117, 98, 46};
		IDictionary<string, object> o = (IDictionary<string, object> ) U(pickled);
		Assert.AreEqual(4, o.Count);
		Assert.AreEqual("Harry", o["name"]);
		Assert.AreEqual(34, o["age"]);
		Assert.AreEqual(new ArrayList() {1,2,3}, o["values"]);
		Assert.AreEqual("__main__.CustomClass", o["__class__"]);
	}

	
	class CustomClazz {
		public string name;
		public int age;
		public ArrayList values;
		public CustomClazz() 
		{
			
		}
		public CustomClazz(string name, int age, ArrayList values)
		{
			this.name=name;
			this.age=age;
			this.values=values;
		}
		
		/**
		 * called by the Unpickler to restore state.
		 */
		public void __setstate__(Hashtable args) {
			this.name = (string) args["name"];
			this.age = (int) args["age"];
			this.values = (ArrayList) args["values"];
		}			
	}
	class CustomClazzConstructor: IObjectConstructor {
		public object construct(object[] args)
		{
			if(args.Length==0)
			{
				return new CustomClazz();    // default constructor
			}
			else if(args.Length==3)
			{
				string name = (string)args[0];
				int age = (int) args[1];
				ArrayList values = (ArrayList) args[2];
				return new CustomClazz(name, age, values);
			}
			else throw new PickleException("expected 0 or 3 constructor arguments");
		}
	}

	[Test]
	public void testUnpickleCustomClassAsClassDict() {
		byte[] pickled = new byte[] {128, 2, 99, 95, 95, 109, 97, 105, 110, 95, 95, 10, 67, 117, 115, 115, 115, 115, 115, 115, 97, 122, 122, 10, 113, 0, 41, 129, 113, 1, 125, 113, 2, 40, 85, 3, 97, 103, 101, 113, 3, 75, 34, 85, 6, 118, 97, 108, 117, 101, 115, 113, 4, 93, 113, 5, 40, 75, 1, 75, 2, 75, 3, 101, 85, 4, 110, 97, 109, 101, 113, 6, 85, 5, 72, 97, 114, 114, 121, 113, 7, 117, 98, 46};

		ClassDict cd = (ClassDict) U(pickled);
		Assert.AreEqual("__main__.Cussssssazz", cd["__class__"]);
		Assert.AreEqual("Harry" , cd["name"]);
		Assert.AreEqual(34 , cd["age"]);
		Assert.AreEqual(new ArrayList() {1,2,3}, cd["values"]);
	}
	
	[Test]
	public void testClassDictConstructorSetsClass() {
		ClassDict cd = new ClassDict("module", "myclass");
		Assert.AreEqual("module.myclass", cd["__class__"]);
		
		ClassDictConstructor cdc = new ClassDictConstructor("module", "myclass");
		cd = (ClassDict) cdc.construct(new Object[]{});
		Assert.AreEqual("module.myclass", cd["__class__"]);
		
		Assert.AreEqual("module.myclass", cd.ClassName);
	}
		
	[Test]
	public void testUnpickleCustomClass() {
		byte[] pickled = new byte[] {128, 2, 99, 95, 95, 109, 97, 105, 110, 95, 95, 10, 67, 117, 115, 116, 111, 109, 67, 108, 97, 122, 122, 10, 113, 0, 41, 129, 113, 1, 125, 113, 2, 40, 85, 3, 97, 103, 101, 113, 3, 75, 34, 85, 6, 118, 97, 108, 117, 101, 115, 113, 4, 93, 113, 5, 40, 75, 1, 75, 2, 75, 3, 101, 85, 4, 110, 97, 109, 101, 113, 6, 85, 5, 72, 97, 114, 114, 121, 113, 7, 117, 98, 46};
		
		Unpickler.registerConstructor("__main__","CustomClazz", new CustomClazzConstructor());
		CustomClazz o = (CustomClazz) U(pickled);
		Assert.AreEqual("Harry" ,o.name);
		Assert.AreEqual(34 ,o.age);
		Assert.AreEqual(new ArrayList() {1,2,3}, o.values);
	}


	
	[Test]
	public void testUnpickleException() {
		// python 2.x
		PythonException x = (PythonException) U("cexceptions\nZeroDivisionError\np0\n(S'hello'\np1\ntp2\nRp3\n.");
		Assert.AreEqual("[exceptions.ZeroDivisionError] hello", x.Message);
		Assert.AreEqual("exceptions.ZeroDivisionError", x.PythonExceptionType);
		// python 3.x
		x = (PythonException) U("c__builtin__\nZeroDivisionError\np0\n(Vhello\np1\ntp2\nRp3\n.");
		Assert.AreEqual("[__builtin__.ZeroDivisionError] hello", x.Message);
		Assert.AreEqual("__builtin__.ZeroDivisionError", x.PythonExceptionType);
		x = (PythonException) U("cbuiltins\nZeroDivisionError\np0\n(Vhello\np1\ntp2\nRp3\n.");
		Assert.AreEqual("[builtins.ZeroDivisionError] hello", x.Message);
		Assert.AreEqual("builtins.ZeroDivisionError", x.PythonExceptionType);

		// python 2.x
		x = (PythonException) U("cexceptions\nGeneratorExit\np0\n(tRp1\n.");
		Assert.IsNull(x.InnerException);
		Assert.AreEqual("exceptions.GeneratorExit", x.PythonExceptionType);
	
		// python 3.x
		x = (PythonException) U("c__builtin__\nGeneratorExit\np0\n(tRp1\n.");
		Assert.AreEqual("[__builtin__.GeneratorExit]", x.Message);
		x = (PythonException) U("cbuiltins\nGeneratorExit\np0\n(tRp1\n.");
		Assert.AreEqual("[builtins.GeneratorExit]", x.Message);
	}
}
}
