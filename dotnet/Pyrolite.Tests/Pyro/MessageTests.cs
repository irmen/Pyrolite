/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

using NUnit.Framework;
using Razorvine.Pyro;

namespace Pyrolite.Tests.Pyro
{

[TestFixture]
public class MessageTestsHmac {
	
	ushort serializer_id = new PickleSerializer().serializer_id;
	
	[Test]
	public void TestMessage()
	{
		byte[] hmac = Encoding.UTF8.GetBytes("secret");
		
		new Message(99, new byte[0], this.serializer_id, 0, 0, null, hmac);  // doesn't check msg type here
		Assert.Throws(typeof(PyroException), ()=>Message.from_header(Encoding.ASCII.GetBytes("FOOBAR")));
		var msg = new Message(Message.MSG_CONNECT, Encoding.ASCII.GetBytes("hello"), this.serializer_id, 0, 0, null, hmac);
		Assert.AreEqual(Message.MSG_CONNECT, msg.type);
		Assert.AreEqual(5, msg.data_size);
		Assert.AreEqual(new byte[]{(byte)'h',(byte)'e',(byte)'l',(byte)'l',(byte)'o'}, msg.data);
		Assert.AreEqual(4+2+20, msg.annotations_size);
		byte[] mac = msg.hmac(hmac);
		var expected = new Dictionary<string, byte[]>();
		expected["HMAC"] = mac;
		CollectionAssert.AreEqual(expected, msg.annotations);

		byte[] hdr = msg.to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.AreEqual(Message.MSG_CONNECT, msg.type);
		Assert.AreEqual(4+2+20, msg.annotations_size);
		Assert.AreEqual(5, msg.data_size);

		hdr = new Message(Message.MSG_RESULT, new byte[0], this.serializer_id, 0, 0, null, hmac).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.AreEqual(Message.MSG_RESULT, msg.type);
		Assert.AreEqual(4+2+20, msg.annotations_size);
		Assert.AreEqual(0, msg.data_size);

		hdr = new Message(Message.MSG_RESULT, Encoding.ASCII.GetBytes("hello"), 12345, 60006, 30003, null, hmac).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.AreEqual(Message.MSG_RESULT, msg.type);
		Assert.AreEqual(60006, msg.flags);
		Assert.AreEqual(5, msg.data_size);
		Assert.AreEqual(12345, msg.serializer_id);
		Assert.AreEqual(30003, msg.seq);

		byte[] data = new Message(255, new byte[0], this.serializer_id, 0, 255, null, hmac).to_bytes();
		Assert.AreEqual(50, data.Length);
		data = new Message(1, new byte[0], this.serializer_id, 0, 255, null, hmac).to_bytes();
		Assert.AreEqual(50, data.Length);
		data = new Message(1, new byte[0], this.serializer_id, 253, 254, null, hmac).to_bytes();
		Assert.AreEqual(50, data.Length);

		// compression is a job of the code supplying the data, so the messagefactory should leave it untouched
		data = new byte[1000];
		byte[] msg_bytes1 = new Message(Message.MSG_INVOKE, data, this.serializer_id, 0, 0, null, hmac).to_bytes();
		byte[] msg_bytes2 = new Message(Message.MSG_INVOKE, data, this.serializer_id, Message.FLAGS_COMPRESSED, 0, null, hmac).to_bytes();
		Assert.AreEqual(msg_bytes1.Length, msg_bytes2.Length);
	}
	
	[Test]
	public void testMessageHeaderDatasize()
	{
		var msg = new Message(Message.MSG_RESULT, Encoding.ASCII.GetBytes("hello"), 12345, 60006, 30003, null, null);
		msg.data_size = 0x12345678;   // hack it to a large value to see if it comes back ok
		byte[] hdr = msg.to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.AreEqual(Message.MSG_RESULT, msg.type);
		Assert.AreEqual(60006, msg.flags);
		Assert.AreEqual(0x12345678, msg.data_size);
		Assert.AreEqual(12345, msg.serializer_id);
		Assert.AreEqual(30003, msg.seq);
	}
	
	[Test]
	public void TestAnnotations()
	{
		byte[] hmac=Encoding.UTF8.GetBytes("secret");
		
		var annotations = new Dictionary<string,byte[]>();
		annotations["TES1"]=new byte[]{10,20,30,40,50};
		annotations["TES2"]=new byte[]{20,30,40,50,60};
		annotations["TES3"]=new byte[]{30,40,50,60,70};
		annotations["TES4"]=new byte[]{40,50,60,70,80};

		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, annotations, hmac);
		byte[] data = msg.to_bytes();
		int annotations_size = 4+2+20 + (4+2+5)*4;
		Assert.AreEqual(Message.HEADER_SIZE + 5 + annotations_size, data.Length);
		Assert.AreEqual(annotations_size, msg.annotations_size);
		Assert.AreEqual(5, msg.annotations.Count);
		Assert.AreEqual(new byte[]{10,20,30,40,50}, msg.annotations["TES1"]);
		Assert.AreEqual(new byte[]{20,30,40,50,60}, msg.annotations["TES2"]);
		Assert.AreEqual(new byte[]{30,40,50,60,70}, msg.annotations["TES3"]);
		Assert.AreEqual(new byte[]{40,50,60,70,80}, msg.annotations["TES4"]);
		byte[] mac = msg.hmac(hmac);
		Assert.AreEqual(mac, msg.annotations["HMAC"]);

		annotations = new Dictionary<string,byte[]>();
		annotations["TES4"]=new byte[]{40,50,60,70,80};
		annotations["TES3"]=new byte[]{30,40,50,60,70};
		annotations["TES2"]=new byte[]{20,30,40,50,60};
		annotations["TES1"]=new byte[]{10,20,30,40,50};
		var msg2 = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, annotations, hmac);
		Assert.AreEqual(msg.hmac(hmac), msg2.hmac(hmac));

		annotations = new Dictionary<string,byte[]>();
		annotations["TES4"]=new byte[]{40,50,60,70,80};
		var msg3 = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, annotations, hmac);
		Assert.AreNotEqual(msg.hmac(hmac), msg3.hmac(hmac));
	}
	
	[Test]
	public void testAnnotationsIdLength4()
	{
		try {
			var anno = new Dictionary<string, byte[]>();
			anno["TOOLONG"] = new byte[]{10,20,30};
			var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, anno, null);
			byte[]data = msg.to_bytes();
			Assert.Fail("should fail, too long");
		} catch(ArgumentException) {
			//ok
		}
		try {
			var anno = new Dictionary<string, byte[]>();
			anno["QQ"] = new byte[]{10,20,30};
			var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, anno, null);
			byte[] data = msg.to_bytes();
			Assert.Fail("should fail, too short");
		} catch (ArgumentException) {
			//ok
		}
	}
	
	[Test]
	public void testRecvAnnotations()
	{
		var annotations = new Dictionary<string, byte[]>();
		annotations["TEST"] = new byte[]{10, 20,30,40,50};
		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, annotations, Encoding.UTF8.GetBytes("secret"));
		
		var ms = new MemoryStream(msg.to_bytes());
		msg = Message.recv(ms, null, Encoding.UTF8.GetBytes("secret"));
		Assert.AreEqual(-1, ms.ReadByte());
		Assert.AreEqual(5, msg.data_size);
		Assert.AreEqual(new byte[]{1,2,3,4,5}, msg.data);
		Assert.AreEqual(new byte[]{10,20,30,40,50}, msg.annotations["TEST"]);
		Assert.IsTrue(msg.annotations.ContainsKey("HMAC"));
	}

	class CustomAnnProxy : PyroProxy
	{
		public CustomAnnProxy(PyroURI uri) : base(uri) {}
		
		public override IDictionary<string, byte[]> annotations()
		{
			var ann = base.annotations();
			ann["XYZZ"] = Encoding.UTF8.GetBytes("some value");
			return ann;
		}
	}
	
	[Test]
	public void testProxyAnnotations()
	{
		var p = new CustomAnnProxy(new PyroURI("PYRO:dummy@localhost:50000"));
		p.pyroHmacKey = Encoding.UTF8.GetBytes("secret");
		p.correlation_id = Guid.NewGuid();
		var annotations = p.annotations();
		Assert.AreEqual(2, annotations.Count);
		Assert.IsTrue(annotations.ContainsKey("CORR"));
		Assert.IsTrue(annotations.ContainsKey("XYZZ"));
	}
	
	[Test]
	[ExpectedException(typeof(PyroException), ExpectedMessage="invalid protocol version: 25455")]
	public void testProtocolVersionKaputt()
	{
		byte[] msg = new Message(Message.MSG_RESULT, new byte[0], this.serializer_id, 0, 1, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg[4] = 99; // screw up protocol version in message header
		msg[5] = 111; // screw up protocol version in message header
		Message.from_header(msg);
	}
	
	[Test]
	[ExpectedException(typeof(PyroException), ExpectedMessage="invalid protocol version: 47")]
	public void testProtocolVersionsNotSupported1()
	{
		byte[] msg = new Message(Message.MSG_RESULT, new byte[0], this.serializer_id, 0, 1, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg[4] = 0;
		msg[5] = 47;	
		Message.from_header(msg);
	}

	[Test]
	[ExpectedException(typeof(PyroException), ExpectedMessage="invalid protocol version: 49")]
	public void testProtocolVersionsNotSupported2()
	{
		byte[] msg = new Message(Message.MSG_RESULT, new byte[0], this.serializer_id, 0, 1, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg[4] = 0;
		msg[5] = 49;	
		Message.from_header(msg);
	}

	[Test]
	public void testHmac()
	{
		Stream c;
		byte[] data;
		
		data = new Message(Message.MSG_RESULT, new byte[]{1,2,3,4,5}, 42, 0, 1, null, Encoding.UTF8.GetBytes("secret")).to_bytes();
		c = new MemoryStream(data);

		// test checking of different hmacs
		try {
			Message.recv(c, null, Encoding.UTF8.GetBytes("wrong"));
			Assert.Fail("crash expected");
		}
		catch(PyroException x) {
			Assert.IsTrue(x.Message.Contains("hmac"));
		}
		c = new MemoryStream(data);
		// test that it works again when resetting the key
		Message.recv(c, null, Encoding.UTF8.GetBytes("secret"));

		c = new MemoryStream(data);
		// test that it doesn't work when no key is set
		try {
			Message.recv(c, null, null);
			Assert.Fail("crash expected");
		}
		catch(PyroException x) {
			Assert.IsTrue(x.Message.Contains("hmac key config"));
		}
	}
	
	[Test]
	public void testChecksum()
	{
		var msg = new Message(Message.MSG_RESULT, new byte[]{1,2,3,4}, 42, 0, 1, null, null);
		byte[] data = msg.to_bytes();
		// corrupt the checksum bytes
		data[Message.HEADER_SIZE-2] = 0;
		data[Message.HEADER_SIZE-1] = 0;
		Stream ms = new MemoryStream(data);
		try {
			Message.recv(ms, null, null);
			Assert.Fail("crash expected");
		}
		catch(PyroException x) {
			Assert.IsTrue(x.Message.Contains("checksum"));
		}
	}
	
	[Test]
	public void testProxyCorrelationId()
	{
		PyroProxy p = new PyroProxy(new PyroURI("PYRO:foo@localhost:55555"));
		p.correlation_id = null;
		var ann = p.annotations();
		Assert.AreEqual(0, ann.Count);
		p.correlation_id = Guid.NewGuid();
		ann = p.annotations();
		Assert.AreEqual(1, ann.Count);
		Assert.IsTrue(ann.ContainsKey("CORR"));
		
		Guid uuid = new Guid(ann["CORR"]);
		Assert.AreEqual(p.correlation_id, uuid);
	}	
}


[TestFixture]
public class MessageTestsNoHmac {
	
	[Test]
	public void testRecvNoAnnotations()
	{
		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, 42, 0, 0, null, null);
		byte[] data = msg.to_bytes();
		var ms = new MemoryStream(data);
		msg = Message.recv(ms, null, null);
		Assert.AreEqual(-1, ms.ReadByte());
		Assert.AreEqual(5, msg.data_size);
		Assert.AreEqual(new byte[]{1,2,3,4,5}, msg.data);
		Assert.AreEqual(0, msg.annotations_size);
		Assert.AreEqual(0, msg.annotations.Count);
	}
}

}
