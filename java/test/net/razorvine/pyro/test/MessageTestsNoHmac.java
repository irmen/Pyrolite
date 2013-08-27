package net.razorvine.pyro.test;

import java.io.IOException;

import net.razorvine.pyro.Message;
import org.junit.Test;
import static org.junit.Assert.*;

/**
 * Unit tests for the Pyro message. Doesn't set HMAC.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class MessageTestsNoHmac {

	@Test
	public void testRecvNoAnnotations() throws IOException
	{
		Message msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, 42, 0, 0, null);
		ConnectionMock c = new ConnectionMock();
		c.send(msg.to_bytes());
		msg = Message.recv(c, null);
		assertEquals(0, c.RemainingLength());
		assertEquals(5, msg.data_size);
		assertArrayEquals(new byte[]{1,2,3,4,5}, msg.data);
		assertEquals(0, msg.annotations_size);
		assertEquals(0, msg.annotations.size());
	}

}
