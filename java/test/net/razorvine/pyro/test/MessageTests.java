package net.razorvine.pyro.test;

import java.io.*;
import java.security.InvalidKeyException;
import java.security.Key;
import java.security.NoSuchAlgorithmException;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;
import java.util.Map.Entry;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;

import net.razorvine.pyro.*;
import net.razorvine.pyro.serializer.*;

import static org.junit.Assert.*;
import org.junit.After;
import org.junit.Before;
import org.junit.Test;



/**
 * Unit tests for the Pyro message. Sets HMAC.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class MessageTests {
	
	PyroSerializer ser;
	
	@Before
	public void setUp() {
		Config.HMAC_KEY = "testsuite".getBytes();
		this.ser = new PickleSerializer();
		// this.ser = Pyro4.util.get_serializer(Pyro4.config.SERIALIZER)  @TODO
	}

	@After
	public void tearDown() {
		Config.HMAC_KEY = null;
	}
	
	public byte[] getHeaderBytes(byte[] data)
	{
		return Arrays.copyOfRange(data,  0, Message.HEADER_SIZE);
	}

	public byte[] pyrohmac(byte[] data, Map<String, byte[]> annotations)
	{
		try {
			Key key = new SecretKeySpec(Config.HMAC_KEY, "HmacSHA1");
			Mac hmac_algo = Mac.getInstance("HmacSHA1");
			hmac_algo.init(key);
			hmac_algo.update(data);
			for(Entry<String, byte[]> a: annotations.entrySet())
			{
				if(!a.getKey().equals("HMAC"))
					hmac_algo.update(a.getValue());
			}
			return hmac_algo.doFinal();
		} catch (NoSuchAlgorithmException e) {
			throw new PyroException("invalid hmac algorithm",e);
		} catch (InvalidKeyException e) {
			throw new PyroException("invalid hmac key",e);
		}
	}
	
	@Test
	public void TestMessage()
	{
		new Message(99, new byte[0], this.ser.getSerializerId(), 0, 0, null);  // doesn't check msg type here
		try {
			Message.from_header("FOOBAR".getBytes());
			fail("should crash");
		} catch(PyroException x) {
			// ok
		}
		Message msg = new Message(Message.MSG_CONNECT, "hello".getBytes(), this.ser.getSerializerId(), 0, 0, null);
		assertEquals(Message.MSG_CONNECT, msg.type);
		assertEquals(5, msg.data_size);
		assertArrayEquals(new byte[]{(byte)'h',(byte)'e',(byte)'l',(byte)'l',(byte)'o'}, msg.data);
		assertEquals(4+2+20, msg.annotations_size);
		byte[] mac = pyrohmac("hello".getBytes(), msg.annotations);
		assertEquals(1, msg.annotations.size());
		assertArrayEquals(mac, msg.annotations.get("HMAC"));

		byte[] hdr = getHeaderBytes(msg.to_bytes());
		msg = Message.from_header(hdr);
		assertEquals(Message.MSG_CONNECT, msg.type);
		assertEquals(4+2+20, msg.annotations_size);
		assertEquals(5, msg.data_size);

		hdr = getHeaderBytes(new Message(Message.MSG_RESULT, new byte[0], this.ser.getSerializerId(), 0, 0, null).to_bytes());
		msg = Message.from_header(hdr);
		assertEquals(Message.MSG_RESULT, msg.type);
		assertEquals(4+2+20, msg.annotations_size);
		assertEquals(0, msg.data_size);

		hdr = getHeaderBytes(new Message(Message.MSG_RESULT, "hello".getBytes(), 12345, 60006, 30003, null).to_bytes());
		msg = Message.from_header(hdr);
		assertEquals(Message.MSG_RESULT, msg.type);
		assertEquals(60006, msg.flags);
		assertEquals(5, msg.data_size);
		assertEquals(12345, msg.serializer_id);
		assertEquals(30003, msg.seq);

		byte[] data = new Message(255, new byte[0], this.ser.getSerializerId(), 0, 255, null).to_bytes();
		assertEquals(50, data.length);
		data = new Message(1, new byte[0], this.ser.getSerializerId(), 0, 255, null).to_bytes();
		assertEquals(50, data.length);
		data = new Message(1, new byte[0], this.ser.getSerializerId(), 253, 254, null).to_bytes();
		assertEquals(50, data.length);

		// compression is a job of the code supplying the data, so the messagefactory should leave it untouched
		data = new byte[1000];
		byte[] msg_bytes1 = new Message(Message.MSG_INVOKE, data, this.ser.getSerializerId(), 0, 0, null).to_bytes();
		byte[] msg_bytes2 = new Message(Message.MSG_INVOKE, data, this.ser.getSerializerId(), Message.FLAGS_COMPRESSED, 0, null).to_bytes();
		assertEquals(msg_bytes1.length, msg_bytes2.length);
	}
	
	@Test
	public void testMessageHeaderDatasize()
	{
		Message msg = new Message(Message.MSG_RESULT, "hello".getBytes(), 12345, 60006, 30003, null);
		msg.data_size = 0x12345678;   // hack it to a large value to see if it comes back ok
		byte[] hdr = getHeaderBytes(msg.to_bytes());
		msg = Message.from_header(hdr);
		assertEquals(Message.MSG_RESULT, msg.type);
		assertEquals(60006, msg.flags);
		assertEquals(0x12345678, msg.data_size);
		assertEquals(12345, msg.serializer_id);
		assertEquals(30003, msg.seq);
	}
	
	@Test
	public void TestAnnotations()
	{
		Map<String, byte[]> annotations = new HashMap<String,byte[]>();
		annotations.put("TEST", new byte[]{10,20,30,40,50});
		
		Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, annotations);
		byte[] data = msg.to_bytes();
		int annotations_size = 4+2+20 + 4+2+5;
		assertEquals(Message.HEADER_SIZE + 5 + annotations_size, data.length);
		assertEquals(annotations_size, msg.annotations_size);
		assertEquals(2, msg.annotations.size());
		assertArrayEquals(new byte[]{10,20,30,40,50}, msg.annotations.get("TEST"));
		byte[] mac = pyrohmac(new byte[]{1,2,3,4,5}, annotations);
		assertArrayEquals(mac, msg.annotations.get("HMAC"));
	}
	
	@Test
	public void testAnnotationsIdLength4()
	{
		try {
			Map<String, byte[]> anno = new HashMap<String, byte[]>();
			anno.put("TOOLONG", new byte[]{10,20,30});
			Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, anno);
			msg.to_bytes();
			fail("should fail, too long");
		} catch(IllegalArgumentException x) {
			//ok
		}
		try {
			Map<String, byte[]> anno = new HashMap<String, byte[]>();
			anno.put("QQ", new byte[]{10,20,30});
			Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, anno);
			msg.to_bytes();
			fail("should fail, too short");
		} catch (IllegalArgumentException x) {
			//ok
		}
	}
	
	@Test
	public void testRecvAnnotations() throws IOException
	{
		Map<String, byte[]> annotations = new HashMap<String, byte[]>();
		annotations.put("TEST", new byte[]{10, 20,30,40,50});
		Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.getSerializerId(), 0, 0, annotations);
		ConnectionMock c = new ConnectionMock();
		c.send(msg.to_bytes());
		msg = Message.recv(c, null);
		assertEquals(0, c.RemainingLength());
		assertEquals(5, msg.data_size);
		assertArrayEquals(new byte[]{1,2,3,4,5}, msg.data);
		assertArrayEquals(new byte[]{10,20,30,40,50}, msg.annotations.get("TEST"));
		assertTrue(msg.annotations.containsKey("HMAC"));
	}
	
	@Test(expected=PyroException.class)
	public void testProtocolVersion()
	{
		byte[] msg = getHeaderBytes(new Message(Message.MSG_RESULT, new byte[0], this.ser.getSerializerId(), 0, 1, null).to_bytes());
		msg[4] = 99; // screw up protocol version in message header
		Message.from_header(msg);
	}
	
	@Test
	public void testHmac() throws IOException
	{
		byte[] hk=Config.HMAC_KEY;
		InputStream c;
		byte[] data;
		
		try {
			Config.HMAC_KEY = "test key".getBytes();
			data = new Message(Message.MSG_RESULT, new byte[]{1,2,3,4,5}, 42, 0, 1, null).to_bytes();
			c = new ConnectionMock(data);
		}
		finally {
			Config.HMAC_KEY = hk;
		}
		// test checking of different hmacs
		try {
			Message.recv(c, null);
			fail("crash expected");
		}
		catch(PyroException x) {
			assertTrue(x.getMessage().contains("hmac"));
		}
		c = new ConnectionMock(data);
		// test that it works again when resetting the key
		try {
			hk = Config.HMAC_KEY;
			Config.HMAC_KEY = "test key".getBytes();
			Message.recv(c, null);
		}
		finally {
			Config.HMAC_KEY = hk;
		}
		c = new ConnectionMock(data);
		// test that it doesn't work when no key is set
		try {
			hk = Config.HMAC_KEY;
			Config.HMAC_KEY = null;
			Message.recv(c, null);
			fail("crash expected");
		}
		catch(PyroException x) {
			assertTrue(x.getMessage().contains("hmac key config"));
		}
		finally {
			Config.HMAC_KEY = hk;
		}
	}
	
	@Test
	public void testChecksum() throws IOException
	{
		Message msg = new Message(Message.MSG_RESULT, new byte[]{1,2,3,4}, 42, 0, 1, null);
		ConnectionMock c = new ConnectionMock();
		c.send(msg.to_bytes());
		// corrupt the checksum bytes
		byte[] data = c.ReceivedData();
		data[Message.HEADER_SIZE-2] = 0;
		data[Message.HEADER_SIZE-1] = 0;
		c = new ConnectionMock(data);
		try {
			Message.recv(c, null);
			fail("crash expected");
		}
		catch(PyroException x) {
			assertTrue(x.getMessage().contains("checksum"));
		}
	}
}
