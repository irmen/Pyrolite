package net.razorvine.pyro.test;

import static org.junit.Assert.*;

import java.io.IOException;
import java.util.HashSet;
import java.util.UUID;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;
import net.razorvine.pyro.serializer.PickleSerializer;
import net.razorvine.pyro.serializer.SerpentSerializer;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

public class SerializePyroTests {

	@Before
	public void setUp() throws Exception {
		Config.SERPENT_INDENT=true;
		Config.SERPENT_SET_LITERALS=true;
	}

	@After
	public void tearDown() throws Exception {
		Config.SERPENT_INDENT=false;
		Config.SERPENT_SET_LITERALS=false;
	}

	@Test
	public void testPyroClassesPickle() throws IOException
	{
		PickleSerializer pickler = new PickleSerializer();
		PyroURI uri = new PyroURI("PYRO:object@host:4444");
		byte[] s = pickler.serializeData(uri);
		Object x = pickler.deserializeData(s);
		assertEquals(uri, x);

		PyroProxy proxy = new PyroProxy(uri);
		proxy.correlation_id = UUID.randomUUID();
		proxy.pyroHmacKey = "secret".getBytes();
		proxy.pyroHandshake = "apples";
		proxy.pyroAttrs = new HashSet<String>();
		proxy.pyroAttrs.add("attr1");
		proxy.pyroAttrs.add("attr2");
		s = pickler.serializeData(proxy);
		x = pickler.deserializeData(s);
		PyroProxy proxy2 = (PyroProxy) x;
		assertEquals(uri.host, proxy2.hostname);
		assertEquals(uri.objectid, proxy2.objectid);
		assertEquals(uri.port, proxy2.port);
		assertNull(proxy2.correlation_id); // correlation_id is not serialized on the proxy object")
		assertEquals(proxy.pyroHandshake, proxy2.pyroHandshake);
		assertArrayEquals(proxy.pyroHmacKey, proxy2.pyroHmacKey);
		assertEquals(2, proxy2.pyroAttrs.size());
		assertEquals(proxy.pyroAttrs, proxy2.pyroAttrs);
		
		PyroException ex = new PyroException("error");
		ex._pyroTraceback = "traceback";
		s = pickler.serializeData(ex);
		x = pickler.deserializeData(s);
		PyroException ex2 = (PyroException) x;
		assertEquals(ex.getMessage(), ex2.getMessage());
		assertEquals("traceback", ex2._pyroTraceback);
	}
	
	@Test
	public void testPyroClassesSerpent() throws IOException
	{
		SerpentSerializer ser = new SerpentSerializer();
		PyroURI uri = new PyroURI("PYRO:something@localhost:4444");
		byte[] s = ser.serializeData(uri);
		Object x = ser.deserializeData(s);
		assertEquals(uri, x);

		PyroProxy proxy = new PyroProxy(uri);
		proxy.correlation_id = UUID.randomUUID();
		proxy.pyroHandshake = "apples";
		proxy.pyroHmacKey = "secret".getBytes();
		proxy.pyroAttrs = new HashSet<String>();
		proxy.pyroAttrs.add("attr1");
		proxy.pyroAttrs.add("attr2");
		s = ser.serializeData(proxy);
		x = ser.deserializeData(s);
		PyroProxy proxy2 = (PyroProxy) x;
		assertEquals(uri.host, proxy2.hostname);
		assertEquals(uri.objectid, proxy2.objectid);
		assertEquals(uri.port, proxy2.port);
		assertNull(proxy2.correlation_id); // correlation_id is not serialized on the proxy object")
		assertEquals(proxy.pyroHandshake, proxy2.pyroHandshake);
		assertArrayEquals(proxy.pyroHmacKey, proxy2.pyroHmacKey);
		assertEquals(2, proxy2.pyroAttrs.size());
		assertEquals(proxy.pyroAttrs, proxy2.pyroAttrs);
		
		PyroException ex = new PyroException("error");
		s = ser.serializeData(ex);
		x = ser.deserializeData(s);
		PyroException ex2 = (PyroException) x;
		assertEquals(ex.getMessage(), ex2.getMessage());
		assertNull(ex._pyroTraceback);
		
		// try another kind of pyro exception
		s = "{'attributes':{'tb': 'traceback', '_pyroTraceback': ['line1', 'line2']},'__exception__':True,'args':('hello',42),'__class__':'CommunicationError'}".getBytes();
		x = ser.deserializeData(s);
		ex2 = (PyroException) x;
		assertEquals("hello", ex2.getMessage());
		assertEquals("line1line2", ex2._pyroTraceback);
	}
	
	@Test
	public void testCompareLibVersions()
	{
		assertEquals(-1, PickleSerializer.compareLibraryVersions("1.2", "2"));
		assertEquals(-1, PickleSerializer.compareLibraryVersions("1.2", "2.5.6"));
		assertEquals(0, PickleSerializer.compareLibraryVersions("1.2", "1.2"));
		assertEquals(1, PickleSerializer.compareLibraryVersions("2", "1.2"));
		assertEquals(1, PickleSerializer.compareLibraryVersions("2.54.66", "1.2.3.4.99"));
	}
}
