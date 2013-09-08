package net.razorvine.pyro.test;

import static org.junit.Assert.*;

import java.io.IOException;

import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;
import net.razorvine.pyro.serializer.PickleSerializer;
import net.razorvine.pyro.serializer.PyroSerializer;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

public class SerializePyroTests {

	protected PyroSerializer ser;

	@Before
	public void setUp() throws Exception {
		ser = new PickleSerializer();
	}

	@After
	public void tearDown() throws Exception {
	}

	@Test
	public void testPyroClasses() throws IOException
	{
		PyroURI uri = new PyroURI("PYRO:object@host:4444");
		byte[] s = this.ser.serializeData(uri);
		Object x = this.ser.deserializeData(s);
		assertEquals(uri, x);

		PyroProxy proxy = new PyroProxy(uri);
		s = this.ser.serializeData(proxy);
		x = this.ser.deserializeData(s);
		PyroProxy proxy2 = (PyroProxy) x;
		assertEquals(uri.host, proxy2.hostname);
		assertEquals(uri.objectid, proxy2.objectid);
		assertEquals(uri.port, proxy2.port);

		PyroException ex = new PyroException("error");
		ex._pyroTraceback = "traceback";
		s = this.ser.serializeData(ex);
		x = this.ser.deserializeData(s);
		PyroException ex2 = (PyroException) x;
		assertEquals(ex.getMessage(), ex2.getMessage());
		assertEquals("traceback", ex2._pyroTraceback);
	}
}
