/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;

using Xunit;
using Razorvine.Pickle;
using Razorvine.Pickle.Objects;
using Razorvine.Pyro;
// ReSharper disable CheckNamespace

namespace Pyrolite.Tests.Pickle
{

/// <summary>
/// tests for more complex pickling/unpickling such as Proxy and URI objects.
/// </summary>
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

	[Fact]
	public void testPickleUnpickleURI() {
		PyroURI uri=new PyroURI("PYRO:test@localhost:9999");
		PyroSerializer ser = new PickleSerializer();
		byte[] pickled_uri=ser.serializeData(uri);
		PyroURI uri2=(PyroURI) ser.deserializeData(pickled_uri);
		Assert.Equal(uri,uri2);

		uri=new PyroURI();
		pickled_uri=ser.serializeData(uri);
		uri2=(PyroURI) ser.deserializeData(pickled_uri);
		Assert.Equal(uri,uri2);
	}

	[Fact]
	public void testPickleUnpickleProxy() {
		PyroProxy proxy = new PyroProxy("hostname", 9999, "objectid")
		{
			pyroHmacKey = Encoding.UTF8.GetBytes("secret"),
			pyroHandshake = "apples"
		};
		PyroSerializer ser = new PickleSerializer();
		byte[] pickled_proxy=ser.serializeData(proxy);
		PyroProxy result = (PyroProxy) ser.deserializeData(pickled_proxy);
		Assert.Equal(proxy.hostname, result.hostname);
		Assert.Equal(proxy.objectid, result.objectid);
		Assert.Equal(proxy.port, result.port);
		Assert.Equal(Encoding.UTF8.GetBytes("secret"), result.pyroHmacKey);
		Assert.Equal("apples", result.pyroHandshake);
	}

	[Fact]
	public void testUnpickleRealProxy2() {
		byte[] pickled_proxy=File.ReadAllBytes("pickled_nameserver_proxy_p2.dat");
		unpickleRealProxy(pickled_proxy);
	}

	[Fact]
	public void testUnpickleRealProxy2old() {
		byte[] pickled_proxy=File.ReadAllBytes("pickled_nameserver_proxy_p2old.dat");
		unpickleRealProxy(pickled_proxy);
	}

	[Fact]
	public void testUnpickleRealProxy3() {
		byte[] pickled_proxy=File.ReadAllBytes("pickled_nameserver_proxy_p3.dat");
		unpickleRealProxy(pickled_proxy);
	}

	[Fact]
	public void testUnpickleRealProxy4() {
		byte[] pickled_proxy=File.ReadAllBytes("pickled_nameserver_proxy_p4.dat");
		unpickleRealProxy(pickled_proxy);
	}

	[Fact]
	public void testUnpickleProto0Bytes() {
		byte[] pickle = File.ReadAllBytes("pickled_bytes_level0.dat");

		PickleSerializer ser = new PickleSerializer();
		string x = (string)ser.deserializeData(pickle);
		Assert.Equal(2496, x.Length);
		
		// validate that the bytes in the string are what we expect (based on md5 hash)
		var m = SHA1.Create();
		byte[] hashb = m.ComputeHash(Encoding.UTF8.GetBytes(x));
		string digest = BitConverter.ToString(hashb);
		Assert.Equal("22-f4-5b-87-63-83-c9-1b-1c-b2-0a-fe-51-ee-3b-30-f5-a8-5d-4c", digest.ToLowerInvariant());
	}

	private void unpickleRealProxy(byte[] pickled_proxy) {
		PyroSerializer ser = new PickleSerializer();
		PyroProxy proxy=(PyroProxy)ser.deserializeData(pickled_proxy);
		Assert.Equal("Pyro.NameServer",proxy.objectid);
		Assert.Equal("localhost",proxy.hostname);
		Assert.Equal(9090,proxy.port);
		Assert.Equal("hello", proxy.pyroHandshake);
		Assert.Equal(Encoding.UTF8.GetBytes("secret"), proxy.pyroHmacKey);
		ISet<string> expectedSet = new HashSet<string>();
		Assert.Equal(expectedSet, proxy.pyroAttrs);
		expectedSet.Clear();
		expectedSet.Add("lookup");
		expectedSet.Add("ping");
		expectedSet.Add("register");
		expectedSet.Add("remove");
		expectedSet.Add("list");
		expectedSet.Add("count");
		expectedSet.Add("set_metadata");
		proxy.pyroMethods.ExceptWith(expectedSet);
		Assert.Equal(0, proxy.pyroMethods.Count); // "something is wrong with the expected exposed methods"
		expectedSet = new HashSet<string>();
		Assert.Equal(expectedSet, proxy.pyroOneway);
	}

	[Fact]
	public void testUnpickleMemo() {
		// the pickle is of the following list: [65, 'hello', 'hello', {'recurse': [...]}, 'hello']
		// i.e. the 4th element is a dict referring back to the list itself and the 'hello' strings are reused
		byte[] pickle = new byte[]
			{128, 2, 93, 113, 0, 40, 75, 65, 85, 5, 104, 101, 108, 108, 111, 113, 1, 104, 1, 125, 113, 2,
			85, 7, 114, 101, 99, 117, 114, 115, 101, 113, 3, 104, 0, 115, 104, 1, 101, 46};
		ArrayList a = (ArrayList) U(pickle);
		Assert.Equal(5, a.Count);
		Assert.Equal(65, a[0]);
		Assert.Equal("hello", a[1]);
		Assert.Same(a[1], a[2]);
		Assert.Same(a[1], a[4]);
		Hashtable h = (Hashtable) a[3];
		Assert.Same(a, h["recurse"]);
	}
		 
	
	[Fact]
	public void testUnpickleUnsupportedClass() {
		// an unsupported class is mapped to a dictionary containing the class's attributes, and a __class__ attribute with the name of the class
		byte[] pickled = new byte[] {128, 2, 99, 95, 95, 109, 97, 105, 110, 95, 95, 10, 67, 117, 115, 116, 111, 109, 67, 108, 97, 115, 115, 10, 113, 0, 41, 129, 113, 1, 125, 113, 2, 40, 85, 3, 97, 103, 101, 113, 3, 75, 34, 85, 6, 118, 97, 108, 117, 101, 115, 113, 4, 93, 113, 5, 40, 75, 1, 75, 2, 75, 3, 101, 85, 4, 110, 97, 109, 101, 113, 6, 85, 5, 72, 97, 114, 114, 121, 113, 7, 117, 98, 46};
		IDictionary<string, object> o = (IDictionary<string, object> ) U(pickled);
		Assert.Equal(4, o.Count);
		Assert.Equal("Harry", o["name"]);
		Assert.Equal(34, o["age"]);
		Assert.Equal(new ArrayList() {1,2,3}, o["values"]);
		Assert.Equal("__main__.CustomClass", o["__class__"]);
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
		// ReSharper disable once UnusedMember.Local
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

	[Fact]
	public void testUnpickleCustomClassAsClassDict() {
		byte[] pickled = new byte[] {128, 2, 99, 95, 95, 109, 97, 105, 110, 95, 95, 10, 67, 117, 115, 115, 115, 115, 115, 115, 97, 122, 122, 10, 113, 0, 41, 129, 113, 1, 125, 113, 2, 40, 85, 3, 97, 103, 101, 113, 3, 75, 34, 85, 6, 118, 97, 108, 117, 101, 115, 113, 4, 93, 113, 5, 40, 75, 1, 75, 2, 75, 3, 101, 85, 4, 110, 97, 109, 101, 113, 6, 85, 5, 72, 97, 114, 114, 121, 113, 7, 117, 98, 46};

		ClassDict cd = (ClassDict) U(pickled);
		Assert.Equal("__main__.Cussssssazz", cd["__class__"]);
		Assert.Equal("Harry" , cd["name"]);
		Assert.Equal(34 , cd["age"]);
		Assert.Equal(new ArrayList() {1,2,3}, cd["values"]);
	}
	
	[Fact]
	public void testClassDictConstructorSetsClass() {
		ClassDict cd = new ClassDict("module", "myclass");
		Assert.Equal("module.myclass", cd["__class__"]);
		
		ClassDictConstructor cdc = new ClassDictConstructor("module", "myclass");
		cd = (ClassDict) cdc.construct(new Object[]{});
		Assert.Equal("module.myclass", cd["__class__"]);
		
		Assert.Equal("module.myclass", cd.ClassName);
	}
		
	[Fact]
	public void testUnpickleCustomClass() {
		byte[] pickled = new byte[] {128, 2, 99, 95, 95, 109, 97, 105, 110, 95, 95, 10, 67, 117, 115, 116, 111, 109, 67, 108, 97, 122, 122, 10, 113, 0, 41, 129, 113, 1, 125, 113, 2, 40, 85, 3, 97, 103, 101, 113, 3, 75, 34, 85, 6, 118, 97, 108, 117, 101, 115, 113, 4, 93, 113, 5, 40, 75, 1, 75, 2, 75, 3, 101, 85, 4, 110, 97, 109, 101, 113, 6, 85, 5, 72, 97, 114, 114, 121, 113, 7, 117, 98, 46};
		
		Unpickler.registerConstructor("__main__","CustomClazz", new CustomClazzConstructor());
		CustomClazz o = (CustomClazz) U(pickled);
		Assert.Equal("Harry" ,o.name);
		Assert.Equal(34 ,o.age);
		Assert.Equal(new ArrayList() {1,2,3}, o.values);
	}


	
	[Fact]
	public void testUnpickleException() {
		// python 2.x
		PythonException x = (PythonException) U("cexceptions\nZeroDivisionError\np0\n(S'hello'\np1\ntp2\nRp3\n.");
		Assert.Equal("[exceptions.ZeroDivisionError] hello", x.Message);
		Assert.Equal("exceptions.ZeroDivisionError", x.PythonExceptionType);
		// python 3.x
		x = (PythonException) U("c__builtin__\nZeroDivisionError\np0\n(Vhello\np1\ntp2\nRp3\n.");
		Assert.Equal("[__builtin__.ZeroDivisionError] hello", x.Message);
		Assert.Equal("__builtin__.ZeroDivisionError", x.PythonExceptionType);
		x = (PythonException) U("cbuiltins\nZeroDivisionError\np0\n(Vhello\np1\ntp2\nRp3\n.");
		Assert.Equal("[builtins.ZeroDivisionError] hello", x.Message);
		Assert.Equal("builtins.ZeroDivisionError", x.PythonExceptionType);

		// python 2.x
		x = (PythonException) U("cexceptions\nGeneratorExit\np0\n(tRp1\n.");
		Assert.Null(x.InnerException);
		Assert.Equal("exceptions.GeneratorExit", x.PythonExceptionType);
	
		// python 3.x
		x = (PythonException) U("c__builtin__\nGeneratorExit\np0\n(tRp1\n.");
		Assert.Equal("[__builtin__.GeneratorExit]", x.Message);
		Assert.Equal("__builtin__.GeneratorExit", x.PythonExceptionType);
		x = (PythonException) U("cbuiltins\nGeneratorExit\np0\n(tRp1\n.");
		Assert.Equal("[builtins.GeneratorExit]", x.Message);
		Assert.Equal("builtins.GeneratorExit", x.PythonExceptionType);
	}
}
}
