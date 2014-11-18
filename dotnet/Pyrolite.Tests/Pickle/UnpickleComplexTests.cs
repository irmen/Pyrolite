/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework;
using Razorvine.Pickle;
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
		PyroSerializer ser = new PickleSerializer();
		byte[] pickled_proxy=ser.serializeData(proxy);
		PyroProxy result = (PyroProxy) ser.deserializeData(pickled_proxy);
		Assert.AreEqual(proxy.hostname, result.hostname);
		Assert.AreEqual(proxy.objectid, result.objectid);
		Assert.AreEqual(proxy.port, result.port);
		Assert.AreEqual(Encoding.UTF8.GetBytes("secret"), result.pyroHmacKey);
	}

	[Test]
	public void testUnpickleRealProxy() {
		byte[] pickled_proxy=new byte[]
				{128, 3, 99, 80, 121, 114, 111, 52, 46, 99, 111, 114, 101, 10, 80, 114, 111, 120, 121, 10, 113,
				 0, 41, 129, 113, 1, 40, 99, 80, 121, 114, 111, 52, 46, 99, 111, 114, 101, 10, 85, 82, 73, 10,
				 113, 2, 41, 129, 113, 3, 40, 88, 4, 0, 0, 0, 80, 89, 82, 79, 113, 4, 88, 15, 0, 0, 0, 80, 121,
				 114, 111, 46, 78, 97, 109, 101, 83, 101, 114, 118, 101, 114, 113, 5, 78, 88, 9, 0, 0, 0, 108,
				 111, 99, 97, 108, 104, 111, 115, 116, 113, 6, 77, 130, 35, 116, 113, 7, 98, 99, 98, 117, 105,
				 108, 116, 105, 110, 115, 10, 115, 101, 116, 10, 113, 8, 93, 113, 9, 40, 88, 2, 0, 0, 0, 111, 49,
				 113, 10, 88, 2, 0, 0, 0, 111, 50, 113, 11, 101, 133, 113, 12, 82, 113, 13, 104, 8, 93, 113, 14,
				 40, 88, 4, 0, 0, 0, 108, 105, 115, 116, 113, 15, 88, 8, 0, 0, 0, 114, 101, 103, 105, 115, 116,
				 101, 114, 113, 16, 88, 5, 0, 0, 0, 99, 111, 117, 110, 116, 113, 17, 88, 6, 0, 0, 0, 108, 111,
				 111, 107, 117, 112, 113, 18, 88, 6, 0, 0, 0, 114, 101, 109, 111, 118, 101, 113, 19, 88, 4, 0,
				 0, 0, 112, 105, 110, 103, 113, 20, 101, 133, 113, 21, 82, 113, 22, 104, 8, 93, 113, 23, 40, 88,
				 5, 0, 0, 0, 97, 116, 116, 114, 50, 113, 24, 88, 5, 0, 0, 0, 97, 116, 116, 114, 49, 113, 25, 101,
				 133, 113, 26, 82, 113, 27, 71, 0, 0, 0, 0, 0, 0, 0, 0, 67, 6, 115, 101, 99, 114, 101, 116, 113,
				 28, 116, 113, 29, 98, 46};
		PyroSerializer ser = new PickleSerializer();
		PyroProxy proxy=(PyroProxy)ser.deserializeData(pickled_proxy);
		Assert.AreEqual("Pyro.NameServer",proxy.objectid);
		Assert.AreEqual("localhost",proxy.hostname);
		Assert.AreEqual(9090,proxy.port);
		CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("secret"), proxy.pyroHmacKey);
		ISet<string> expectedSet = new HashSet<string>();
		expectedSet.Add("attr1");
		expectedSet.Add("attr2");
		CollectionAssert.AreEquivalent(expectedSet, proxy.pyroAttrs);
		expectedSet.Clear();
		expectedSet.Add("lookup");
		expectedSet.Add("ping");
		expectedSet.Add("register");
		expectedSet.Add("remove");
		expectedSet.Add("list");
		expectedSet.Add("count");
		CollectionAssert.AreEquivalent(expectedSet, proxy.pyroMethods);
		expectedSet.Clear();
		expectedSet.Add("o1");
		expectedSet.Add("o2");
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
		Assert.AreEqual("hello", x.Message);
		// python 3.x
		x = (PythonException) U("c__builtin__\nZeroDivisionError\np0\n(Vhello\np1\ntp2\nRp3\n.");
		Assert.AreEqual("hello", x.Message);
		x = (PythonException) U("cbuiltins\nZeroDivisionError\np0\n(Vhello\np1\ntp2\nRp3\n.");
		Assert.AreEqual("hello", x.Message);

		// python 2.x
		x = (PythonException) U("cexceptions\nGeneratorExit\np0\n(tRp1\n.");
		Assert.IsNull(x.InnerException);
		// python 3.x
		x = (PythonException) U("c__builtin__\nGeneratorExit\np0\n(tRp1\n.");
		Assert.AreEqual("Exception of type 'Razorvine.Pickle.PythonException' was thrown.", x.Message);
		x = (PythonException) U("cbuiltins\nGeneratorExit\np0\n(tRp1\n.");
		Assert.AreEqual("Exception of type 'Razorvine.Pickle.PythonException' was thrown.", x.Message);
	}
}
}
