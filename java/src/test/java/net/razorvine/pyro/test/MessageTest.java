package net.razorvine.pyro.test;

import java.io.*;
import java.util.Arrays;
import java.util.SortedMap;
import java.util.TreeMap;

import net.razorvine.pyro.*;
import net.razorvine.pyro.serializer.*;
import static org.junit.Assert.*;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;



/**
 * Unit tests for the Pyro message.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class MessageTest {

	PyroSerializer ser;

	@Before
	public void setUp() {
		this.ser = new SerpentSerializer();
	}

	@After
	public void tearDown() {
	}

	public byte[] getHeaderBytes(byte[] data)
	{
		return Arrays.copyOfRange(data,  0, Message.HEADER_SIZE);
	}

	@Test
	public void TestMessage()
	{
		new Message((byte)99, new byte[0], this.ser.getSerializerId(), 0, 0, null, null);  // doesn't check msg type here
		try {
			Message.from_header("FOOBAR".getBytes());
			fail("should crash");
		} catch(PyroException x) {
			// ok
		}
		Message msg = new Message(Message.MSG_CONNECT, "hello".getBytes(), this.ser.getSerializerId(), 0, 0, null, null);
		assertEquals(Message.MSG_CONNECT, msg.type);
		assertEquals(5, msg.data_size);
		assertArrayEquals(new byte[]{(byte)'h',(byte)'e',(byte)'l',(byte)'l',(byte)'o'}, msg.data);
		assertEquals(0, msg.annotations.size());
		assertEquals(0, msg.annotations_size);

		byte[] hdr = getHeaderBytes(msg.to_bytes());
		msg = Message.from_header(hdr);
		assertEquals(Message.MSG_CONNECT, msg.type);
		assertEquals(0, msg.annotations_size);
		assertEquals(5, msg.data_size);

		hdr = getHeaderBytes(new Message(Message.MSG_RESULT, new byte[0], this.ser.getSerializerId(), 0, 0, null, null).to_bytes());
		msg = Message.from_header(hdr);
		assertEquals(Message.MSG_RESULT, msg.type);
		assertEquals(0, msg.annotations_size);
		assertEquals(0, msg.data_size);

		hdr = getHeaderBytes(new Message(Message.MSG_RESULT, "hello".getBytes(), (byte)99, 60006, 30003, null, null).to_bytes());
		msg = Message.from_header(hdr);
		assertEquals(Message.MSG_RESULT, msg.type);
		assertEquals(60006, msg.flags);
		assertEquals(5, msg.data_size);
		assertEquals(99, msg.serializer_id);
		assertEquals(30003, msg.seq);

		byte[] data = new Message((byte)255, new byte[0], this.ser.getSerializerId(), 0, 255, null, null).to_bytes();
		assertEquals(40, data.length);
		data = new Message((byte)1, new byte[0], this.ser.getSerializerId(), 0, 255, null, null).to_bytes();
		assertEquals(40, data.length);
		data = new Message((byte)1, new byte[0], this.ser.getSerializerId(), 253, 254, null, null).to_bytes();
		assertEquals(40, data.length);

		// compression is a job of the code supplying the data, so the messagefactory should leave it untouched
		data = new byte[1000];
		byte[] msg_bytes1 = new Message(Message.MSG_INVOKE, data, this.ser.getSerializerId(), 0, 0, null, null).to_bytes();
		byte[] msg_bytes2 = new Message(Message.MSG_INVOKE, data, this.ser.getSerializerId(), Message.FLAGS_COMPRESSED, 0, null, null).to_bytes();
		assertEquals(msg_bytes1.length, msg_bytes2.length);
	}

	@Test
	public void testMessageHeaderDatasize()
	{
		Message msg = new Message(Message.MSG_RESULT, "hello".getBytes(), (byte)99, 60006, 30003, null, null);
		msg.data_size = 0x12345678;   // hack it to a large value to see if it comes back ok
		byte[] hdr = getHeaderBytes(msg.to_bytes());
		msg = Message.from_header(hdr);
		assertEquals(Message.MSG_RESULT, msg.type);
		assertEquals(60006, msg.flags);
		assertEquals(0x12345678, msg.data_size);
		assertEquals(99, msg.serializer_id);
		assertEquals(30003, msg.seq);
	}

	@Test
	public void TestAnnotations()
	{
		byte[] key = "secret".getBytes();
		SortedMap<String, byte[]> annotations = new TreeMap<String,byte[]>();
		annotations.put("TES1", new byte[]{10,20,30,40,50});
		annotations.put("TES2", new byte[]{20,30,40,50,60});
		annotations.put("TES3", new byte[]{30,40,50,60,70});
		annotations.put("TES4", new byte[]{40,50,60,70,80});

		Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, annotations, null);
		byte[] data = msg.to_bytes();
		int annotations_size = (4+4+5)*4;
		assertEquals(Message.HEADER_SIZE + annotations_size + 5, data.length);
		assertEquals(4, msg.annotations.size());
		assertEquals(annotations_size, msg.annotations_size);
		assertArrayEquals(new byte[]{10,20,30,40,50}, msg.annotations.get("TES1"));
		assertArrayEquals(new byte[]{20,30,40,50,60}, msg.annotations.get("TES2"));
		assertArrayEquals(new byte[]{30,40,50,60,70}, msg.annotations.get("TES3"));
		assertArrayEquals(new byte[]{40,50,60,70,80}, msg.annotations.get("TES4"));

		annotations = new TreeMap<String,byte[]>();
		annotations.put("TES4", new byte[]{40,50,60,70,80});
		annotations.put("TES3", new byte[]{30,40,50,60,70});
		annotations.put("TES2", new byte[]{20,30,40,50,60});
		annotations.put("TES1", new byte[]{10,20,30,40,50});
		Message msg2 = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, annotations, null);

		annotations = new TreeMap<String,byte[]>();
		annotations.put("TES4", new byte[]{40,50,60,70,80});
		Message msg3 = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, annotations, null);
	}

	@Test
	public void testAnnotationsIdLength4()
	{
		try {
			SortedMap<String, byte[]> anno = new TreeMap<String, byte[]>();
			anno.put("TOOLONG", new byte[]{10,20,30});
			Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, anno, null);
			msg.to_bytes();
			fail("should fail, too long");
		} catch(IllegalArgumentException x) {
			//ok
		}
		try {
			SortedMap<String, byte[]> anno = new TreeMap<String, byte[]>();
			anno.put("QQ", new byte[]{10,20,30});
			Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, anno, null);
			msg.to_bytes();
			fail("should fail, too short");
		} catch (IllegalArgumentException x) {
			//ok
		}
	}

	@Test
	public void testRecvAnnotations() throws IOException
	{
		SortedMap<String, byte[]> annotations = new TreeMap<String, byte[]>();
		annotations.put("TEST", new byte[]{10, 20,30,40,50});
		Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, annotations, null);
		byte[] data = msg.to_bytes();

		InputStream is = new ByteArrayInputStream(data);
		msg = Message.recv(is, null);
		assertEquals(0, is.available());
		assertEquals(5, msg.data_size);
		assertEquals(1, msg.annotations.size());
		assertArrayEquals(new byte[]{1,2,3,4,5}, msg.data);
		assertArrayEquals(new byte[]{10,20,30,40,50}, msg.annotations.get("TEST"));
	}

	@SuppressWarnings("serial")
	class CustomAnnProxy extends PyroProxy
	{
		public CustomAnnProxy(PyroURI uri) throws IOException
		{
			super(uri);
		}

		@Override
		public SortedMap<String, byte[]> annotations()
		{
			SortedMap<String, byte[]> ann = super.annotations();
			ann.put("XYZZ", "some value".getBytes());
			return ann;
		}
	}

	@Test
	public void testProxyAnnotations() throws IOException
	{
		PyroProxy p = new CustomAnnProxy(new PyroURI("PYRO:dummy@localhost:50000"));
		SortedMap<String, byte[]> annotations = p.annotations();
		assertEquals(1, annotations.size());
		assertTrue(annotations.containsKey("XYZZ"));
	}


	@Test
	public void testProtocolVersionKaputt()
	{
		byte[] msg = getHeaderBytes(new Message(Message.MSG_RESULT, new byte[0], this.ser.getSerializerId(), 0, 1, null, null).to_bytes());
		msg[4] = 99;   // screw up protocol version in message header
		msg[5] = 111;  // screw up protocol version in message header
		try {
			Message.from_header(msg);
			fail("should crash");
		} catch (PyroException x) {
			assertEquals("invalid protocol version: 25455", x.getMessage());
		}
	}

	@Test
	public void testProtocolVersionsNotSupported1()
	{
		byte[] msg = getHeaderBytes(new Message(Message.MSG_RESULT, new byte[0], this.ser.getSerializerId(), 0, 1, null, null).to_bytes());
		msg[4] = 0;
		msg[5] = 47;
		try {
			Message.from_header(msg);
			fail("should crash");
		} catch (PyroException x) {
			assertEquals("invalid protocol version: 47", x.getMessage());
		}
	}

	@Test
	public void testProtocolVersionsNotSupported2()
	{
		byte[] msg = getHeaderBytes(new Message(Message.MSG_RESULT, new byte[0], this.ser.getSerializerId(), 0, 1, null, null).to_bytes());
		msg[4] = 0;
		msg[5] = 49;
		try {
			Message.from_header(msg);
			fail("should crash");
		} catch (PyroException x) {
			assertEquals("invalid protocol version: 49", x.getMessage());
		}
	}

	@Test
	public void testRecvNoAnnotations() throws IOException
	{
		Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, (byte)42, 0, 0, null, null);
		byte[] data = msg.to_bytes();

		InputStream is = new ByteArrayInputStream(data);
		msg = Message.recv(is, null);
		assertEquals(0, is.available());
		assertEquals(5, msg.data_size);
		assertArrayEquals(new byte[]{1,2,3,4,5}, msg.data);
		assertEquals(0, msg.annotations_size);
		assertEquals(0, msg.annotations.size());
	}
}
