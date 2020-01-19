package net.razorvine.pyro.test;

import static org.junit.Assert.*;

import java.io.IOException;
import java.util.HashSet;
import java.util.Set;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.serializer.PyroSerializer;
import net.razorvine.pyro.serializer.SerpentSerializer;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

public class SerpentSerializerSetsTest {

	@Before
	public void setUp() throws Exception {
		Config.SERPENT_SET_LITERALS=true;
	}

	@After
	public void tearDown() throws Exception {
		Config.SERPENT_SET_LITERALS=false;
	}

	@SuppressWarnings("unchecked")
	@Test
	public void testSerializeData() throws IOException
	{
		Set<String> s = new HashSet<String>();
		s.add("element1");
		s.add("element2");
		PyroSerializer ser = PyroSerializer.getSerpentSerializer();
		byte[] data = ser.serializeData(s);
		String str = new String(data);
		assertTrue(str.equals("# serpent utf-8 python3.2\n{'element1','element2'}") ||
				   str.equals("# serpent utf-8 python3.2\n{'element2','element1'}"));

		Set<Object> s2 = (HashSet<Object>) ser.deserializeData(data);
		assertEquals(s, s2);
	}

	@Test
	public void testSerpentBytes() throws IOException
	{
		byte[] bytes = "hello".getBytes();
		SerpentSerializer ser = new SerpentSerializer();
		byte[] data = ser.serializeData(bytes);

		String str = new String(data);
		assertTrue(str.contains("base64"));

		net.razorvine.serpent.Parser p = new net.razorvine.serpent.Parser();
		Object data2 = p.parse(data).getData();
		byte[] bytes2 = SerpentSerializer.toBytes(data2);

		assertArrayEquals("hello".getBytes(), bytes2);
	}
}
