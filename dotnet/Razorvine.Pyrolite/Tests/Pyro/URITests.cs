/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using Xunit;
using Razorvine.Pyro;

// ReSharper disable CheckNamespace

namespace Pyrolite.Tests.Pyro
{

public class URITests {
	
	[Fact]
	public void TestIpv4()
	{
		PyroURI uri = new PyroURI("PYRO:objectname@hostname:1234");
		Assert.Equal(1234, uri.port);
		Assert.Equal("hostname", uri.host);
		Assert.Equal("objectname", uri.objectid);
		Assert.Equal("PYRO", uri.protocol);

		PyroURI uricopy = new PyroURI(uri);
		Assert.Equal(1234, uricopy.port);
		Assert.Equal("hostname", uricopy.host);
		Assert.Equal("objectname", uricopy.objectid);
		Assert.Equal("PYRO", uricopy.protocol);

		uri = new PyroURI("objectname", "hostname", 1234);
		Assert.Equal(1234, uri.port);
		Assert.Equal("hostname", uri.host);
		Assert.Equal("objectname", uri.objectid);
		Assert.Equal("PYRO", uri.protocol);
	}		

	[Fact]
	public void TestIpv6()
	{
		PyroURI uri = new PyroURI("PYRO:objectname@[::1]:1234");
		Assert.Equal(1234, uri.port);
		Assert.Equal("::1", uri.host);
		Assert.Equal("objectname", uri.objectid);
		Assert.Equal("PYRO", uri.protocol);

		PyroURI uricopy = new PyroURI(uri);
		Assert.Equal(1234, uricopy.port);
		Assert.Equal("::1", uricopy.host);
		Assert.Equal("objectname", uricopy.objectid);
		Assert.Equal("PYRO", uricopy.protocol);

		uri = new PyroURI("objectname", "::1", 1234);
		Assert.Equal(1234, uri.port);
		Assert.Equal("::1", uri.host);
		Assert.Equal("objectname", uri.objectid);
		Assert.Equal("PYRO", uri.protocol);

		Assert.Throws<PyroException>(() => new PyroURI("PYRO:objectname@[[::1]]:1234"));
		Assert.Throws<PyroException>(() => new PyroURI("PYRO:objectname@[invalid-ipv6]:1234"));
	}		

}

}
