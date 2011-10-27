/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Collections;
using NUnit.Framework;
using Razorvine.Pyrolite.Pickle;
using Razorvine.Pyrolite.Pyro;

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
		Unpickler u=new Unpickler();
		object o=u.loads(data);
		u.close();
		return o;		
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
		Pickler p=new Pickler();
		byte[] pickled_uri=p.dumps(uri);
		PyroURI uri2=(PyroURI) U(pickled_uri);
		Assert.AreEqual(uri,uri2);

		uri=new PyroURI();
		pickled_uri=p.dumps(uri);
		uri2=(PyroURI) U(pickled_uri);
		Assert.AreEqual(uri,uri2);
	}

	[Test]
	public void testPickleUnpickleProxy() {
		PyroProxy proxy=new PyroProxy("hostname",9999,"objectid");
		Pickler p=new Pickler();
		byte[] pickled_proxy=p.dumps(proxy);
		object result=U(pickled_proxy);
		Assert.IsInstanceOfType(typeof(System.Collections.Hashtable), result); // proxy objects cannot be properly pickled and are pickled as bean, hence HashMap
	}

	[Test]
	public void testUnpickleRealProxy() {
		byte[] pickled_proxy=new byte[]
				{128, 2, 99, 80, 121, 114, 111, 52, 46, 99, 111, 114, 101, 10, 80, 114, 111, 120, 121, 10, 113,
				 0, 41, 129, 113, 1, 40, 99, 80, 121, 114, 111, 52, 46, 99, 111, 114, 101, 10, 85, 82, 73, 10,
				 113, 2, 41, 129, 113, 3, 40, 85, 4, 80, 89, 82, 79, 113, 4, 85, 10, 115, 111, 109, 101, 111,
				 98, 106, 101, 99, 116, 113, 5, 78, 85, 9, 108, 111, 99, 97, 108, 104, 111, 115, 116, 113, 6,
				 77, 15, 39, 116, 113, 7, 98, 99, 95, 95, 98, 117, 105, 108, 116, 105, 110, 95, 95, 10, 115,
				 101, 116, 10, 113, 8, 93, 113, 9, 133, 113, 10, 82, 113, 11, 99, 80, 121, 114, 111, 52, 46,
				 117, 116, 105, 108, 10, 83, 101, 114, 105, 97, 108, 105, 122, 101, 114, 10, 113, 12, 41, 129,
				 113, 13, 125, 113, 14, 98, 71, 0, 0, 0, 0, 0, 0, 0, 0, 116, 113, 15, 98, 46};
		PyroProxy proxy=(PyroProxy)U(pickled_proxy);
		Assert.AreEqual("someobject",proxy.objectid);
		Assert.AreEqual("localhost",proxy.hostname);
		Assert.AreEqual(9999,proxy.port);
	}

}
}
