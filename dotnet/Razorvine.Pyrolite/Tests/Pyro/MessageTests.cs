/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Razorvine.Pyro;
// ReSharper disable CheckNamespace

namespace Pyrolite.Tests.Pyro
{

public class MessageTestsHmac {
	readonly ushort serializer_id = new PickleSerializer().serializer_id;
	
	[Fact]
	public void testMessage()
	{
		byte[] hmac = Encoding.UTF8.GetBytes("secret");
		
		var unused = new Message(99, new byte[0], this.serializer_id, 0, 0, null, hmac);  // doesn't check msg type here
		Assert.Throws<PyroException>(()=>Message.from_header(Encoding.ASCII.GetBytes("FOOBAR")));
		var msg = new Message(Message.MSG_CONNECT, Encoding.ASCII.GetBytes("hello"), this.serializer_id, 0, 0, null, hmac);
		Assert.Equal(Message.MSG_CONNECT, msg.type);
		Assert.Equal(5, msg.data_size);
		Assert.Equal(new []{(byte)'h',(byte)'e',(byte)'l',(byte)'l',(byte)'o'}, msg.data);
		Assert.Equal(4+2+20, msg.annotations_size);
		byte[] mac = msg.hmac(hmac);
		var expected = new Dictionary<string, byte[]> {["HMAC"] = mac};
		Assert.Equal(expected, msg.annotations);

		byte[] hdr = msg.to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.Equal(Message.MSG_CONNECT, msg.type);
		Assert.Equal(4+2+20, msg.annotations_size);
		Assert.Equal(5, msg.data_size);

		hdr = new Message(Message.MSG_RESULT, new byte[0], this.serializer_id, 0, 0, null, hmac).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.Equal(Message.MSG_RESULT, msg.type);
		Assert.Equal(4+2+20, msg.annotations_size);
		Assert.Equal(0, msg.data_size);

		hdr = new Message(Message.MSG_RESULT, Encoding.ASCII.GetBytes("hello"), 12345, 60006, 30003, null, hmac).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.Equal(Message.MSG_RESULT, msg.type);
		Assert.Equal(60006, msg.flags);
		Assert.Equal(5, msg.data_size);
		Assert.Equal(12345, msg.serializer_id);
		Assert.Equal(30003, msg.seq);

		byte[] data = new Message(255, new byte[0], this.serializer_id, 0, 255, null, hmac).to_bytes();
		Assert.Equal(50, data.Length);
		data = new Message(1, new byte[0], this.serializer_id, 0, 255, null, hmac).to_bytes();
		Assert.Equal(50, data.Length);
		data = new Message(1, new byte[0], this.serializer_id, 253, 254, null, hmac).to_bytes();
		Assert.Equal(50, data.Length);

		// compression is a job of the code supplying the data, so the messagefactory should leave it untouched
		data = new byte[1000];
		byte[] msg_bytes1 = new Message(Message.MSG_INVOKE, data, this.serializer_id, 0, 0, null, hmac).to_bytes();
		byte[] msg_bytes2 = new Message(Message.MSG_INVOKE, data, this.serializer_id, Message.FLAGS_COMPRESSED, 0, null, hmac).to_bytes();
		Assert.Equal(msg_bytes1.Length, msg_bytes2.Length);
	}
	
	[Fact]
	public void testMessageHeaderDatasize()
	{
		var msg =
			new Message(Message.MSG_RESULT, Encoding.ASCII.GetBytes("hello"), 12345, 60006, 30003, null, null)
			{
				data_size = 0x12345678
			};
		// hack it to a large value to see if it comes back ok
		byte[] hdr = msg.to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.Equal(Message.MSG_RESULT, msg.type);
		Assert.Equal(60006, msg.flags);
		Assert.Equal(0x12345678, msg.data_size);
		Assert.Equal(12345, msg.serializer_id);
		Assert.Equal(30003, msg.seq);
	}
	
	[Fact]
	public void testAnnotations()
	{
		byte[] hmac=Encoding.UTF8.GetBytes("secret");

		var annotations = new Dictionary<string, byte[]>
		{
			["TES1"] = new byte[] {10, 20, 30, 40, 50},
			["TES2"] = new byte[] {20, 30, 40, 50, 60},
			["TES3"] = new byte[] {30, 40, 50, 60, 70},
			["TES4"] = new byte[] {40, 50, 60, 70, 80}
		};

		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, annotations, hmac);
		byte[] data = msg.to_bytes();
		int annotations_size = 4+2+20 + (4+2+5)*4;
		Assert.Equal(Message.HEADER_SIZE + 5 + annotations_size, data.Length);
		Assert.Equal(annotations_size, msg.annotations_size);
		Assert.Equal(5, msg.annotations.Count);
		Assert.Equal(new byte[]{10,20,30,40,50}, msg.annotations["TES1"]);
		Assert.Equal(new byte[]{20,30,40,50,60}, msg.annotations["TES2"]);
		Assert.Equal(new byte[]{30,40,50,60,70}, msg.annotations["TES3"]);
		Assert.Equal(new byte[]{40,50,60,70,80}, msg.annotations["TES4"]);
		byte[] mac = msg.hmac(hmac);
		Assert.Equal(mac, msg.annotations["HMAC"]);

		annotations = new Dictionary<string, byte[]>
		{
			["TES4"] = new byte[] {40, 50, 60, 70, 80},
			["TES3"] = new byte[] {30, 40, 50, 60, 70},
			["TES2"] = new byte[] {20, 30, 40, 50, 60},
			["TES1"] = new byte[] {10, 20, 30, 40, 50}
		};
		var msg2 = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, annotations, hmac);
		Assert.Equal(msg.hmac(hmac), msg2.hmac(hmac));

		annotations = new Dictionary<string, byte[]> {["TES4"] = new byte[] {40, 50, 60, 70, 80}};
		var msg3 = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, annotations, hmac);
		Assert.NotEqual(msg.hmac(hmac), msg3.hmac(hmac));
	}
	
	[Fact]
	public void testAnnotationsIdLength4()
	{
		try {
			var anno = new Dictionary<string, byte[]> {["TOOLONG"] = new byte[] {10, 20, 30}};
			var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, anno, null);
			msg.to_bytes();
			Assert.True(false, "should fail, too long");
		} catch(ArgumentException) {
			//ok
		}
		try {
			var anno = new Dictionary<string, byte[]> {["QQ"] = new byte[] {10, 20, 30}};
			var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, anno, null);
			msg.to_bytes();
			Assert.True(false, "should fail, too short");
		} catch (ArgumentException) {
			//ok
		}
	}
	
	[Fact]
	public void testRecvAnnotations()
	{
		var annotations = new Dictionary<string, byte[]> {["TEST"] = new byte[] {10, 20, 30, 40, 50}};
		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.serializer_id, 0, 0, annotations, Encoding.UTF8.GetBytes("secret"));
		
		var ms = new MemoryStream(msg.to_bytes());
		msg = Message.recv(ms, null, Encoding.UTF8.GetBytes("secret"));
		Assert.Equal(-1, ms.ReadByte());
		Assert.Equal(5, msg.data_size);
		Assert.Equal(new byte[]{1,2,3,4,5}, msg.data);
		Assert.Equal(new byte[]{10,20,30,40,50}, msg.annotations["TEST"]);
		Assert.True(msg.annotations.ContainsKey("HMAC"));
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
	
	[Fact]
	public void testProxyAnnotations()
	{
		var p = new CustomAnnProxy(new PyroURI("PYRO:dummy@localhost:50000"))
		{
			pyroHmacKey = Encoding.UTF8.GetBytes("secret"),
			correlation_id = Guid.NewGuid()
		};
		var annotations = p.annotations();
		Assert.Equal(2, annotations.Count);
		Assert.True(annotations.ContainsKey("CORR"));
		Assert.True(annotations.ContainsKey("XYZZ"));
	}
	
	[Fact]
	public void testProtocolVersionKaputt()
	{
		byte[] msg = new Message(Message.MSG_RESULT, new byte[0], this.serializer_id, 0, 1, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg[4] = 99; // screw up protocol version in message header
		msg[5] = 111; // screw up protocol version in message header
		Assert.Throws<PyroException>(() => Message.from_header(msg)); // "invalid protocol version: 25455"
	}
	
	[Fact]
	public void testProtocolVersionsNotSupported1()
	{
		byte[] msg = new Message(Message.MSG_RESULT, new byte[0], this.serializer_id, 0, 1, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg[4] = 0;
		msg[5] = 47;
		Assert.Throws<PyroException>(() => Message.from_header(msg)); // "invalid protocol version: 47"
	}

	[Fact]
	public void testProtocolVersionsNotSupported2()
	{
		byte[] msg = new Message(Message.MSG_RESULT, new byte[0], this.serializer_id, 0, 1, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg[4] = 0;
		msg[5] = 49;
		Assert.Throws<PyroException>(() => Message.from_header(msg));  //"invalid protocol version: 49"
	}

	[Fact]
	public void testHmac()
	{
		byte[] data = new Message(Message.MSG_RESULT, new byte[]{1,2,3,4,5}, 42, 0, 1, null, Encoding.UTF8.GetBytes("secret")).to_bytes();
		Stream c = new MemoryStream(data);

		// test checking of different hmacs
		try {
			Message.recv(c, null, Encoding.UTF8.GetBytes("wrong"));
			Assert.True(false, "crash expected");
		}
		catch(PyroException x) {
			Assert.True(x.Message.Contains("hmac"));
		}
		c = new MemoryStream(data);
		// test that it works again when resetting the key
		Message.recv(c, null, Encoding.UTF8.GetBytes("secret"));

		c = new MemoryStream(data);
		// test that it doesn't work when no key is set
		try {
			Message.recv(c, null, null);
			Assert.True(false, "crash expected");
		}
		catch(PyroException x) {
			Assert.True(x.Message.Contains("hmac key config"));
		}
	}
	
	[Fact]
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
			Assert.True(false, "crash expected");
		}
		catch(PyroException x) {
			Assert.True(x.Message.Contains("checksum"));
		}
	}
	
	[Fact]
	public void testProxyCorrelationId()
	{
		PyroProxy p = new PyroProxy(new PyroURI("PYRO:foo@localhost:55555")) {correlation_id = null};
		var ann = p.annotations();
		Assert.Equal(0, ann.Count);
		p.correlation_id = Guid.NewGuid();
		ann = p.annotations();
		Assert.Equal(1, ann.Count);
		Assert.True(ann.ContainsKey("CORR"));
		
		Guid uuid = new Guid(ann["CORR"]);
		Assert.Equal(p.correlation_id, uuid);
	}	
}


public class MessageTestsNoHmac {
	
	[Fact]
	public void testRecvNoAnnotations()
	{
		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, 42, 0, 0, null, null);
		byte[] data = msg.to_bytes();
		var ms = new MemoryStream(data);
		msg = Message.recv(ms, null, null);
		Assert.Equal(-1, ms.ReadByte());
		Assert.Equal(5, msg.data_size);
		Assert.Equal(new byte[]{1,2,3,4,5}, msg.data);
		Assert.Equal(0, msg.annotations_size);
		Assert.Equal(0, msg.annotations.Count);
	}
}

}
