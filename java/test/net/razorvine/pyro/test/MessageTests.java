package net.razorvine.pyro.test;

import java.io.*;
import java.util.Arrays;

import static org.junit.Assert.*;

import net.razorvine.pyro.*;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

/**
 * Unit tests for the Pyro message.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class MessageTests {

	@Before
	public void setUp() throws Exception {
	}

	@After
	public void tearDown() throws Exception {
	}

	
	@Test
	public void testMessage() throws IOException {
		Config.HMAC_KEY = null;
		byte[] data = new byte[] { 1,2,3,4 };
		byte[] headerdata = MessageFactory.createMsgHeader(MessageFactory.FLAGS_EXCEPTION, data, MessageFactory.FLAGS_EXCEPTION, 123);
		MessageHeader header = MessageFactory.parseMessageHeader(headerdata);
		assertEquals(4, header.datasize);
		assertEquals(MessageFactory.FLAGS_EXCEPTION, header.flags);
		assertEquals(123, header.sequence);
		assertEquals(MessageFactory.MSG_CONNECT, header.type);
		assertArrayEquals(new byte[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, header.hmac);
		
		ByteArrayOutputStream bos=new ByteArrayOutputStream();
		bos.write(headerdata);
		bos.write(data);
		
		ByteArrayInputStream bis = new ByteArrayInputStream(bos.toByteArray());
		
		bis.reset();
		try {
			MessageFactory.getMessage(bis, MessageFactory.MSG_CONNECTFAIL);
			fail("expected PyroException");
		} catch (PyroException x) {
			// ok (invalid message type)
		}

		bis.reset();
		Message msg = MessageFactory.getMessage(bis, MessageFactory.MSG_CONNECT);
		assertEquals(MessageFactory.FLAGS_EXCEPTION, msg.flags);
		assertEquals(123, msg.sequence);
		assertEquals(MessageFactory.FLAGS_EXCEPTION, msg.type);
		assertArrayEquals(data, msg.data);
	}
	
	@Test
	public void testHmac() throws IOException {
		Config.HMAC_KEY = "hello".getBytes();
		
		byte[] hmac = MessageFactory.makeHMAC(new byte[] {1,2,3,4});
		assertEquals(20, hmac.length);
		assertFalse(Arrays.equals(new byte[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, hmac));
		
		
		byte[] data = new byte[] { 1,2,3,4 };
		byte[] headerdata = MessageFactory.createMsgHeader(MessageFactory.FLAGS_EXCEPTION, data, MessageFactory.FLAGS_EXCEPTION, 123);
		MessageHeader header = MessageFactory.parseMessageHeader(headerdata);
		assertEquals(4, header.datasize);
		assertEquals(MessageFactory.FLAGS_EXCEPTION | MessageFactory.FLAGS_HMAC, header.flags);
		assertEquals(123, header.sequence);
		assertEquals(MessageFactory.MSG_CONNECT, header.type);
		assertFalse(Arrays.equals(new byte[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, header.hmac));
		
		ByteArrayOutputStream bos=new ByteArrayOutputStream();
		bos.write(headerdata);
		bos.write(data);
		
		ByteArrayInputStream bis = new ByteArrayInputStream(bos.toByteArray());
		
		Config.HMAC_KEY = "faulty key".getBytes();
		bis.reset();
		try {
			MessageFactory.getMessage(bis, MessageFactory.MSG_CONNECT);
			fail("expected PyroException");
		} catch (PyroException x) {
			// ok (hmac mismatch)
		}

		Config.HMAC_KEY = "hello".getBytes();
		bis.reset();
		Message msg = MessageFactory.getMessage(bis, MessageFactory.MSG_CONNECT);
		assertEquals(MessageFactory.FLAGS_EXCEPTION | MessageFactory.FLAGS_HMAC, msg.flags);
		assertEquals(123, msg.sequence);
		assertEquals(MessageFactory.FLAGS_EXCEPTION, msg.type);
		assertArrayEquals(data, msg.data);
	}	
}
