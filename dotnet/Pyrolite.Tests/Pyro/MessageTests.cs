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

/// <summary>
/// A mock class that can be used in place of a socket connection, for test purposes
/// </summary>
class ConnectionMock : MemoryStream
{
	
	public long RemainingLength {
		get {
			return base.Length - base.Position;
		}
	}
	
	public byte[] ReceivedData {
		get {
			return base.ToArray();
		}
	}

	public ConnectionMock() : base()
	{
	}

	public ConnectionMock(byte[] initialData) : base(initialData)
	{
	}

	public void send(byte[] data)
	{
		base.Write(data, 0, data.Length);
		base.Seek(0, SeekOrigin.Begin);
	}
}
	
[TestFixture]
public class MessageTestsHmac {
	
	PyroSerializer ser;
	
	[TestFixtureSetUp]
	public void setUp() {
		Config.HMAC_KEY = Encoding.ASCII.GetBytes("testsuite");
		this.ser = new PickleSerializer();
        // this.ser = Pyro4.util.get_serializer(Pyro4.config.SERIALIZER)  @TODO
	}

	[TestFixtureTearDown]
	public void tearDown() {
		Config.HMAC_KEY = null;
	}

	public byte[] pyrohmac(byte[] data, IDictionary<string, byte[]> annotations)
	{
		using(HMACSHA1 hmac=new HMACSHA1(Config.HMAC_KEY)) {
			hmac.TransformBlock(data, 0, data.Length, data, 0);
			if(annotations!=null)
			{
				foreach(var e in annotations)
				{
					if(e.Key!="HMAC")
						hmac.TransformBlock(e.Value, 0, e.Value.Length, e.Value, 0);
				}
			}
			hmac.TransformFinalBlock(data, 0, 0);
			return hmac.Hash;
		}
	}
	
	[Test]
	public void TestMessage()
	{
		new Message(99, new byte[0], this.ser.serializer_id, 0, 0, null);  // doesn't check msg type here
		Assert.Throws(typeof(PyroException), ()=>Message.from_header(Encoding.ASCII.GetBytes("FOOBAR")));
		var msg = new Message(Message.MSG_CONNECT, Encoding.ASCII.GetBytes("hello"), this.ser.serializer_id, 0, 0, null);
        Assert.AreEqual(Message.MSG_CONNECT, msg.type);
        Assert.AreEqual(5, msg.data_size);
        Assert.AreEqual(new byte[]{(byte)'h',(byte)'e',(byte)'l',(byte)'l',(byte)'o'}, msg.data);
        Assert.AreEqual(4+2+20, msg.annotations_size);
        byte[] mac = pyrohmac(Encoding.ASCII.GetBytes("hello"), msg.annotations);
        var expected = new Dictionary<string, byte[]>();
        expected["HMAC"] = mac;
        CollectionAssert.AreEqual(expected, msg.annotations);

        byte[] hdr = msg.to_bytes().Take(Message.HEADER_SIZE).ToArray();
        msg = Message.from_header(hdr);
        Assert.AreEqual(Message.MSG_CONNECT, msg.type);
        Assert.AreEqual(4+2+20, msg.annotations_size);
        Assert.AreEqual(5, msg.data_size);

        hdr = new Message(Message.MSG_RESULT, new byte[0], this.ser.serializer_id, 0, 0, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
        msg = Message.from_header(hdr);
        Assert.AreEqual(Message.MSG_RESULT, msg.type);
        Assert.AreEqual(4+2+20, msg.annotations_size);
        Assert.AreEqual(0, msg.data_size);

        hdr = new Message(Message.MSG_RESULT, Encoding.ASCII.GetBytes("hello"), 12345, 60006, 30003, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
        msg = Message.from_header(hdr);
        Assert.AreEqual(Message.MSG_RESULT, msg.type);
        Assert.AreEqual(60006, msg.flags);
        Assert.AreEqual(5, msg.data_size);
        Assert.AreEqual(12345, msg.serializer_id);
        Assert.AreEqual(30003, msg.seq);

        byte[] data = new Message(255, new byte[0], this.ser.serializer_id, 0, 255, null).to_bytes();
        Assert.AreEqual(50, data.Length);
        data = new Message(1, new byte[0], this.ser.serializer_id, 0, 255, null).to_bytes();
        Assert.AreEqual(50, data.Length);
        data = new Message(1, new byte[0], this.ser.serializer_id, 253, 254, null).to_bytes();
        Assert.AreEqual(50, data.Length);

        // compression is a job of the code supplying the data, so the messagefactory should leave it untouched
        data = new byte[1000];
        byte[] msg_bytes1 = new Message(Message.MSG_INVOKE, data, this.ser.serializer_id, 0, 0, null).to_bytes();
        byte[] msg_bytes2 = new Message(Message.MSG_INVOKE, data, this.ser.serializer_id, Message.FLAGS_COMPRESSED, 0, null).to_bytes();
        Assert.AreEqual(msg_bytes1.Length, msg_bytes2.Length);
	}
	
	[Test]
	public void TestAnnotations()
	{
		var annotations = new Dictionary<string,byte[]>();
		annotations["TEST"]=new Byte[]{10,20,30,40,50};
		
		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.serializer_id, 0, 0, annotations);
		byte[] data = msg.to_bytes();
        int annotations_size = 4+2+20 + 4+2+5;
        Assert.AreEqual(Message.HEADER_SIZE + 5 + annotations_size, data.Length);
        Assert.AreEqual(annotations_size, msg.annotations_size);
        Assert.AreEqual(2, msg.annotations.Count);
        Assert.AreEqual(new byte[]{10,20,30,40,50}, msg.annotations["TEST"]);
        byte[] mac = pyrohmac(new byte[]{1,2,3,4,5}, annotations);
        Assert.AreEqual(mac, msg.annotations["HMAC"]);
	}
	
	[Test]
	public void testAnnotationsIdLength4()
	{
		try {
			var anno = new Dictionary<string, byte[]>();
			anno["TOOLONG"] = new Byte[]{10,20,30};
			var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.serializer_id, 0, 0, anno);
            byte[]data = msg.to_bytes();
            Assert.Fail("should fail, too long");
		} catch(ArgumentException) {
			//ok
		}
		try {
			var anno = new Dictionary<string, byte[]>();
			anno["QQ"] = new Byte[]{10,20,30};
            var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.serializer_id, 0, 0, anno);
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
		annotations["TEST"] = new Byte[]{10, 20,30,40,50};
		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, this.ser.serializer_id, 0, 0, annotations);
        var c = new ConnectionMock();
        c.send(msg.to_bytes());
        msg = Message.recv(c, null);
        Assert.AreEqual(0, c.RemainingLength);
        Assert.AreEqual(5, msg.data_size);
        Assert.AreEqual(new byte[]{1,2,3,4,5}, msg.data);
        Assert.AreEqual(new byte[]{10,20,30,40,50}, msg.annotations["TEST"]);
        Assert.IsTrue(msg.annotations.ContainsKey("HMAC"));
	}
	
	[Test]
	[ExpectedException(typeof(PyroException), ExpectedMessage="invalid protocol version: 25390")]
	public void testProtocolVersion()
	{
        byte[] msg = new Message(Message.MSG_RESULT, new byte[0], this.ser.serializer_id, 0, 1, null).to_bytes().Take(Message.HEADER_SIZE).ToArray();
        msg[4] = 99; // screw up protocol version in message header
        Message.from_header(msg);
	}
	
	[Test]
	public void testHmac()
	{
		byte[] hk=Config.HMAC_KEY;
		Stream c;
		byte[] data;
		
		try {
            Config.HMAC_KEY = Encoding.ASCII.GetBytes("test key");
            data = new Message(Message.MSG_RESULT, new byte[]{1,2,3,4,5}, 42, 0, 1, null).to_bytes();
            c = new ConnectionMock(data);
		}
		finally {
            Config.HMAC_KEY = hk;
		}
        // test checking of different hmacs
        try {
            Message.recv(c, null);
            Assert.Fail("crash expected");
        }
        catch(PyroException x) {
        	Assert.IsTrue(x.Message.Contains("hmac"));
        }
        c = new ConnectionMock(data);
        // test that it works again when resetting the key
        try {
            hk = Config.HMAC_KEY;
            Config.HMAC_KEY = Encoding.ASCII.GetBytes("test key");
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
            Assert.Fail("crash expected");
        }
        catch(PyroException x) {
        	Assert.IsTrue(x.Message.Contains("hmac key config"));
        }
        finally {
            Config.HMAC_KEY = hk;
        }
	}
	
	[Test]
	public void testChecksum()
	{
		var msg = new Message(Message.MSG_RESULT, new byte[]{1,2,3,4}, 42, 0, 1, null);
        var c = new ConnectionMock();
        c.send(msg.to_bytes());
        // corrupt the checksum bytes
        byte[] data = c.ReceivedData;
        data[Message.HEADER_SIZE-2] = 0;
        data[Message.HEADER_SIZE-1] = 0;
        c = new ConnectionMock(data);
        try {
            Message.recv(c, null);
            Assert.Fail("crash expected");
        }
        catch(PyroException x) {
        	Assert.IsTrue(x.Message.Contains("checksum"));
        }
	}
}


[TestFixture]
public class MessageTestsNoHmac {
	
	[Test]
	public void testRecvNoAnnotations()
	{
		var msg = new Message(Message.MSG_CONNECT, new byte[]{1,2,3,4,5}, 42, 0, 0, null);
        var c = new ConnectionMock();
        c.send(msg.to_bytes());
        msg = Message.recv(c, null);
        Assert.AreEqual(0, c.RemainingLength);
        Assert.AreEqual(5, msg.data_size);
        Assert.AreEqual(new byte[]{1,2,3,4,5}, msg.data);
        Assert.AreEqual(0, msg.annotations_size);
        Assert.AreEqual(0, msg.annotations.Count);
	}
}

}
