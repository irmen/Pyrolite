package net.razorvine.pyro.test;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;

import net.razorvine.pyro.Message;

import org.junit.Test;

import static org.junit.Assert.*;

/**
 * Unit tests for the Pyro message. Doesn't set HMAC.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class MessageNoHmacTest {

	@Test
	public void testRecvNoAnnotations() throws IOException
	{
		Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, 42, 0, 0, null, null);
		byte[] data = msg.to_bytes();

		InputStream is = new ByteArrayInputStream(data);
		msg = Message.recv(is, null, null);
		assertEquals(0, is.available());
		assertEquals(5, msg.data_size);
		assertArrayEquals(new byte[]{1,2,3,4,5}, msg.data);
		assertEquals(0, msg.annotations_size);
		assertEquals(0, msg.annotations.size());
	}

}
