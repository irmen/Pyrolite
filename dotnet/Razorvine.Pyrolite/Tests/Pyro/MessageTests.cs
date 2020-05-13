/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;
using Razorvine.Pyro;
using Razorvine.Pyro.Serializer;

// ReSharper disable CheckNamespace

namespace Pyrolite.Tests.Pyro
{

public class MessageTests {
	private readonly byte _serializerId = new SerpentSerializer().serializer_id;
	
	[Fact]
	public void TestMessage()
	{
		var unused = new Message(99, new byte[0], _serializerId, 0, 0, null, null);  // doesn't check msg type here
		Assert.Throws<PyroException>(()=>Message.from_header(Encoding.ASCII.GetBytes("FOOBAR")));
		var msg = new Message(Message.MSG_CONNECT, Encoding.ASCII.GetBytes("hello"), _serializerId, 0, 0, null, null);
		Assert.Equal(Message.MSG_CONNECT, msg.type);
		Assert.Equal(5, msg.data_size);
		Assert.Equal(new []{(byte)'h',(byte)'e',(byte)'l',(byte)'l',(byte)'o'}, msg.data);
		Assert.Equal(0, msg.annotations_size);
		Assert.Equal(0, msg.annotations.Count);

		var hdr = msg.to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.Equal(Message.MSG_CONNECT, msg.type);
		Assert.Equal(0, msg.annotations_size);
		Assert.Equal(5, msg.data_size);

		hdr = new Message(Message.MSG_RESULT, new byte[0], _serializerId, 0, 0, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.Equal(Message.MSG_RESULT, msg.type);
		Assert.Equal(0, msg.annotations_size);
		Assert.Equal(0, msg.data_size);

		hdr = new Message(Message.MSG_RESULT, Encoding.ASCII.GetBytes("hello"), 99, 60006, 30003, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.Equal(Message.MSG_RESULT, msg.type);
		Assert.Equal(60006, msg.flags);
		Assert.Equal(5, msg.data_size);
		Assert.Equal(99, msg.serializer_id);
		Assert.Equal(30003, msg.seq);

		var data = new Message(255, new byte[0], _serializerId, 0, 255, null, null).to_bytes();
		Assert.Equal(40, data.Length);
		data = new Message(1, new byte[0], _serializerId, 0, 255, null, null).to_bytes();
		Assert.Equal(40, data.Length);
		data = new Message(1, new byte[0], _serializerId, 253, 254, null, null).to_bytes();
		Assert.Equal(40, data.Length);

		// compression is a job of the code supplying the data, so the messagefactory should leave it untouched
		data = new byte[1000];
		var msgBytes1 = new Message(Message.MSG_INVOKE, data, _serializerId, 0, 0, null, null).to_bytes();
		var msgBytes2 = new Message(Message.MSG_INVOKE, data, _serializerId, Message.FLAGS_COMPRESSED, 0, null, null).to_bytes();
		Assert.Equal(msgBytes1.Length, msgBytes2.Length);
	}
	
	[Fact]
	public void TestMessageHeaderDatasize()
	{
		var msg =
			new Message(Message.MSG_RESULT, Encoding.ASCII.GetBytes("hello"), 99, 60006, 30003, null, null)
			{
				data_size = 0x12345678
			};
		// hack it to a large value to see if it comes back ok
		var hdr = msg.to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg = Message.from_header(hdr);
		Assert.Equal(Message.MSG_RESULT, msg.type);
		Assert.Equal(60006, msg.flags);
		Assert.Equal(0x12345678, msg.data_size);
		Assert.Equal(99, msg.serializer_id);
		Assert.Equal(30003, msg.seq);
	}
	
		
	[Fact]
	public void TestRecvNoAnnotations()
	{
		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, 42, 0, 0, null, null);
		var data = msg.to_bytes();
		var ms = new MemoryStream(data);
		msg = Message.recv(ms, null);
		Assert.Equal(-1, ms.ReadByte());
		Assert.Equal(5, msg.data_size);
		Assert.Equal(new byte[]{1,2,3,4,5}, msg.data);
		Assert.Equal(0, msg.annotations_size);
		Assert.Equal(0, msg.annotations.Count);
	}
	
	[Fact]
	public void TestAnnotations()
	{
		var annotations = new Dictionary<string, byte[]>
		{
			["TES1"] = new byte[] {10, 20, 30, 40, 50},
			["TES2"] = new byte[] {20, 30, 40, 50, 60},
			["TES3"] = new byte[] {30, 40, 50, 60, 70},
			["TES4"] = new byte[] {40, 50, 60, 70, 80}
		};

		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, _serializerId, 0, 0, annotations, null);
		var data = msg.to_bytes();
		const int annotationsSize = (4+4+5)*4;
		Assert.Equal(Message.HEADER_SIZE + annotationsSize + 5, data.Length);
		Assert.Equal(4, msg.annotations.Count);
		Assert.Equal(annotationsSize, msg.annotations_size);
		Assert.Equal(new byte[]{10,20,30,40,50}, msg.annotations["TES1"]);
		Assert.Equal(new byte[]{20,30,40,50,60}, msg.annotations["TES2"]);
		Assert.Equal(new byte[]{30,40,50,60,70}, msg.annotations["TES3"]);
		Assert.Equal(new byte[]{40,50,60,70,80}, msg.annotations["TES4"]);

		annotations = new Dictionary<string, byte[]>
		{
			["TES4"] = new byte[] {40, 50, 60, 70, 80},
			["TES3"] = new byte[] {30, 40, 50, 60, 70},
			["TES2"] = new byte[] {20, 30, 40, 50, 60},
			["TES1"] = new byte[] {10, 20, 30, 40, 50}
		};
		var msg2 = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, _serializerId, 0, 0, annotations, null);

		annotations = new Dictionary<string, byte[]> {["TES4"] = new byte[] {40, 50, 60, 70, 80}};
		var msg3 = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, _serializerId, 0, 0, annotations, null);
	}
	
	[Fact]
	public void TestAnnotationsIdLength4()
	{
		try {
			var anno = new Dictionary<string, byte[]> {["TOOLONG"] = new byte[] {10, 20, 30}};
			var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, _serializerId, 0, 0, anno, null);
			msg.to_bytes();
			Assert.True(false, "should fail, too long");
		} catch(ArgumentException) {
			//ok
		}
		try {
			var anno = new Dictionary<string, byte[]> {["QQ"] = new byte[] {10, 20, 30}};
			var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, _serializerId, 0, 0, anno, null);
			msg.to_bytes();
			Assert.True(false, "should fail, too short");
		} catch (ArgumentException) {
			//ok
		}
	}
	
	[Fact]
	public void TestRecvAnnotations()
	{
		var annotations = new Dictionary<string, byte[]> {["TEST"] = new byte[] {10, 20, 30, 40, 50}};
		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, _serializerId, 0, 0, annotations, null);
		
		var ms = new MemoryStream(msg.to_bytes());
		msg = Message.recv(ms, null);
		Assert.Equal(-1, ms.ReadByte());
		Assert.Equal(5, msg.data_size);
		Assert.Equal(1, msg.annotations.Count);
		Assert.Equal(new byte[]{1,2,3,4,5}, msg.data);
		Assert.Equal(new byte[]{10,20,30,40,50}, msg.annotations["TEST"]);
	}

	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public class CustomAnnProxy : PyroProxy
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
	public void TestProxyAnnotations()
	{
		var p = new CustomAnnProxy(new PyroURI("PYRO:dummy@localhost:50000"))
		{
			correlation_id = Guid.NewGuid()
		};
		var annotations = p.annotations();
		Assert.Equal(1, annotations.Count);
		Assert.True(annotations.ContainsKey("XYZZ"));
	}
	
	[Fact]
	public void TestProtocolVersionKaputt()
	{
		var msg = new Message(Message.MSG_RESULT, new byte[0], _serializerId, 0, 1, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg[4] = 99; // screw up protocol version in message header
		msg[5] = 111; // screw up protocol version in message header
		Assert.Throws<PyroException>(() => Message.from_header(msg)); // "invalid protocol version: 25455"
	}
	
	[Fact]
	public void TestProtocolVersionsNotSupported1()
	{
		var msg = new Message(Message.MSG_RESULT, new byte[0], _serializerId, 0, 1, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg[4] = 0;
		msg[5] = 47;
		Assert.Throws<PyroException>(() => Message.from_header(msg)); // "invalid protocol version: 47"
	}

	[Fact]
	public void TestProtocolVersionsNotSupported2()
	{
		var msg = new Message(Message.MSG_RESULT, new byte[0], _serializerId, 0, 1, null, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
		msg[4] = 0;
		msg[5] = 49;
		Assert.Throws<PyroException>(() => Message.from_header(msg));  //"invalid protocol version: 49"
	}
}

}
