package net.razorvine.pyro.test;

import static org.junit.Assert.*;

import java.io.IOException;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.Set;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.serializer.PyroSerializer;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

public class SerpentSerializerNoSetsTest {

	@Before
	public void setUp() throws Exception {
	}

	@After
	public void tearDown() throws Exception {
	}

	@SuppressWarnings("unchecked")
	@Test
	public void testSerializeData() throws IOException
	{
		List<Object> list = new LinkedList<Object>();
		list.add("hello");
		list.add(42);
		
		PyroSerializer ser = PyroSerializer.getFor(Config.SerializerType.serpent);
		byte[] data = ser.serializeData(list);
		String str = new String(data);
		assertEquals("# serpent utf-8 python2.6\n['hello',42]", str);
		
		List<Object> list_obj = (List<Object>)ser.deserializeData(data);
		assertEquals(list, list_obj);
		
		Set<String> s = new HashSet<String>();
		s.add("element1");
		s.add("element2");
		data = ser.serializeData(s);
		str = new String(data);
		assertTrue(str.equals("# serpent utf-8 python2.6\n('element1','element2')") ||
				   str.equals("# serpent utf-8 python2.6\n('element2','element1')"));
		
		Object[] array_obj = (Object[]) ser.deserializeData(data);
		assertArrayEquals(s.toArray(), array_obj);
	}

	@Test
	public void testSerializeCall() throws IOException
	{
		PyroSerializer ser = PyroSerializer.getFor(Config.SerializerType.serpent);
		Map<String, Object> kwargs = new HashMap<String, Object>();
		kwargs.put("arg", 42);
		Object[] vargs = new Object[] {"hello"};
		
		byte[] data = ser.serializeCall("objectid", "method", vargs, kwargs);
		String s = new String(data);
		assertEquals("# serpent utf-8 python2.6\n('objectid','method',('hello',),{'arg':42})", s);
		
		Object[] call = (Object[])ser.deserializeData(data);
		
		kwargs = new HashMap<String, Object>();
		kwargs.put("arg", 42);
		Object[] expected = new Object[] {
			"objectid",
			"method",
			new Object[] {"hello"},
			kwargs
		};
		assertArrayEquals(expected, call);
	}

}
