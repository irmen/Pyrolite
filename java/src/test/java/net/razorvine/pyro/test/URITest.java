package net.razorvine.pyro.test;

import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroURI;
import org.junit.Test;

import java.io.IOException;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertThrows;


/**
 * Unit tests for the Pyro URI.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class URITest {

	@Test
	public void TestIpv4() throws IOException {
		PyroURI uri = new PyroURI("PYRO:objectname@hostname:1234");
		assertEquals(1234, uri.port);
		assertEquals("hostname", uri.host);
		assertEquals("objectname", uri.objectid);
		assertEquals("PYRO", uri.protocol);

		PyroURI uricopy = new PyroURI(uri);
		assertEquals(1234, uricopy.port);
		assertEquals("hostname", uricopy.host);
		assertEquals("objectname", uricopy.objectid);
		assertEquals("PYRO", uricopy.protocol);

		uri = new PyroURI("objectname", "hostname", 1234);
		assertEquals(1234, uri.port);
		assertEquals("hostname", uri.host);
		assertEquals("objectname", uri.objectid);
		assertEquals("PYRO", uri.protocol);
	}

	@Test
	public void TestIpv6() throws IOException {
		PyroURI uri = new PyroURI("PYRO:objectname@[::1]:1234");
		assertEquals(1234, uri.port);
		assertEquals("::1", uri.host);
		assertEquals("objectname", uri.objectid);
		assertEquals("PYRO", uri.protocol);

		PyroURI uricopy = new PyroURI(uri);
		assertEquals(1234, uricopy.port);
		assertEquals("::1", uricopy.host);
		assertEquals("objectname", uricopy.objectid);
		assertEquals("PYRO", uricopy.protocol);

		uri = new PyroURI("objectname", "::1", 1234);
		assertEquals(1234, uri.port);
		assertEquals("::1", uri.host);
		assertEquals("objectname", uri.objectid);
		assertEquals("PYRO", uri.protocol);

		assertThrows(PyroException.class, () -> new PyroURI("PYRO:objectname@[[::1]]:1234"));
		assertThrows(PyroException.class, () -> new PyroURI("PYRO:objectname@[invalid-ipv6]:1234"));
	}
}
