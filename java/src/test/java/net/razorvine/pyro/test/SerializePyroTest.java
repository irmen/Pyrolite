package net.razorvine.pyro.test;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;
import net.razorvine.pyro.serializer.PyroProxySerpent;
import net.razorvine.pyro.serializer.SerpentSerializer;
import org.junit.After;
import org.junit.Before;
import org.junit.Test;

import java.io.IOException;
import java.util.*;

import static org.junit.Assert.*;

public class SerializePyroTest {

	@Before
	public void setUp() throws Exception {
		Config.SERPENT_INDENT=true;
	}

	@After
	public void tearDown() throws Exception {
		Config.SERPENT_INDENT=false;
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
		assertEquals(2, proxy2.pyroAttrs.size());
		assertEquals(proxy.pyroAttrs, proxy2.pyroAttrs);

		PyroException ex = new PyroException("error");
		s = ser.serializeData(ex);
		x = ser.deserializeData(s);
		PyroException ex2 = (PyroException) x;
		assertEquals("[PyroError] error", ex2.getMessage());
		assertNull(ex._pyroTraceback);

		// try another kind of pyro exception
		s = "{'attributes':{'tb': 'traceback', '_pyroTraceback': ['line1', 'line2']},'__exception__':True,'args':('hello',42),'__class__':'CommunicationError'}".getBytes();
		x = ser.deserializeData(s);
		ex2 = (PyroException) x;
		assertEquals("[CommunicationError] hello", ex2.getMessage());
		assertEquals("line1line2", ex2._pyroTraceback);
	}

	@Test
	public void testPyroProxySerpent() throws IOException
	{
		PyroProxySerpent serp = new PyroProxySerpent();
		PyroURI uri = new PyroURI("PYRO:something@localhost:4444");
		PyroProxy proxy = new PyroProxy(uri);
		proxy.correlation_id = UUID.randomUUID();
		proxy.pyroHandshake = "apples";
		proxy.pyroAttrs = new HashSet<String>();
		proxy.pyroAttrs.add("attr1");
		proxy.pyroAttrs.add("attr2");
		Map<String, Object> data = serp.convert(proxy);
		assertEquals(2, data.size());
		assertEquals("Pyro5.client.Proxy", data.get("__class__"));
		assertEquals(7, ((Object[])data.get("state")).length);

		Map<Object, Object> data2=new HashMap<Object, Object>(data);

		PyroProxy proxy2 = (PyroProxy) PyroProxySerpent.FromSerpentDict(data2);
		assertEquals(proxy.objectid, proxy2.objectid);
		assertEquals("apples", proxy2.pyroHandshake);
	}

	@Test
	public void testUnserpentProxy() throws IOException
	{
		byte[] data = ("# serpent utf-8 python3.2\n" +
					   "{'state':('PYRO:Pyro.NameServer@localhost:9090',(),('count','lookup','register','ping','list','remove'),(),0.0,'hello',0),'__class__':'Pyro5.client.Proxy'}").getBytes();

		SerpentSerializer ser = new SerpentSerializer();
		PyroProxy p = (PyroProxy) ser.deserializeData(data);
		assertNull(p.correlation_id);
		assertEquals("Pyro.NameServer", p.objectid);
		assertEquals("localhost", p.hostname);
		assertEquals(9090, p.port);
		assertEquals("hello", p.pyroHandshake);
		assertEquals(0, p.pyroAttrs.size());
		assertEquals(0, p.pyroOneway.size());
		assertEquals(6, p.pyroMethods.size());
		Set<String> methods = new HashSet<String>();
		methods.add("ping");
		methods.add("count");
		methods.add("lookup");
		methods.add("list");
		methods.add("register");
		methods.add("remove");
		assertEquals(methods, p.pyroMethods);
	}

	@Test
	public void testBytes()
	{
		byte[] bytes = new byte[] { 97, 98, 99, 100, 101, 102 };	// abcdef
		Map<String,String> dict = new HashMap<String, String>();
		dict.put("data", "YWJjZGVm");
		dict.put("encoding", "base64");

        byte[] bytes2 = SerpentSerializer.toBytes(dict);
        assertArrayEquals(bytes, bytes2);

        try {
        	SerpentSerializer.toBytes(12345);
        	fail("error expected");
        } catch (IllegalArgumentException x) {
        	//
        }
	}

}
