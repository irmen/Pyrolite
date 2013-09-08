package net.razorvine.pyro.test;

import static org.junit.Assert.*;

import java.io.IOException;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;
import net.razorvine.pyro.serializer.PyroSerializer;
import net.razorvine.pyro.serializer.SerpentSerializer;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

public class SerializePyroTests_Serpent {

	protected PyroSerializer ser;

	@Before
	public void setUp() throws Exception {
		Config.SERPENT_INDENT=true;
		Config.SERPENT_SET_LITERALS=true;
		ser = new SerpentSerializer();
	}

	@After
	public void tearDown() throws Exception {
		Config.SERPENT_INDENT=false;
		Config.SERPENT_SET_LITERALS=false;
	}

	@Test
	public void testPyroClasses() throws IOException
	{
		PyroURI uri = new PyroURI("PYRO:something@localhost:4444");
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
		s = this.ser.serializeData(ex);
		x = this.ser.deserializeData(s);
		PyroException ex2 = (PyroException) x;
		assertEquals(ex.getMessage(), ex2.getMessage());
		assertNull(ex._pyroTraceback);
		
		// try another kind of pyro exception
		s = "{'attributes':{'tb': 'traceback', '_pyroTraceback': ['line1', 'line2']},'__exception__':True,'args':('hello',42),'__class__':'CommunicationError'}".getBytes();
		x = this.ser.deserializeData(s);
		ex2 = (PyroException) x;
		assertEquals("hello", ex2.getMessage());
		assertEquals("line1line2", ex2._pyroTraceback);
	}
}
