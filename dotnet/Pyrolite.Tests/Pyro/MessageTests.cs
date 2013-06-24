/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.IO;
using System.Text;

using NUnit.Framework;
using Razorvine.Pyro;

namespace Pyrolite.Tests.Pyro
{

/// <summary>
/// Unit tests for the messages used by the Pyro protocol
/// </summary>
[TestFixture]
public class MessageTests {
	
	[TestFixtureSetUp]
	public void setUp() {
		Config.HMAC_KEY = null;
		Config.PROTOCOL_VERSION = 44;
	}

	[TestFixtureTearDown]
	public void tearDown() {
	}
	
	[Test]
	public void TestMessageAcceptedVersions()
	{
		Config.PROTOCOL_VERSION = 44;
		byte[] data = new byte[] { 1,2,3,4 };
		byte[] headerdata = MessageFactory.createMsgHeader(MessageFactory.FLAGS_EXCEPTION, data, MessageFactory.FLAGS_EXCEPTION, 123);
		MessageHeader header = MessageFactory.parseMessageHeader(headerdata);

		Config.PROTOCOL_VERSION = 45;
		headerdata = MessageFactory.createMsgHeader(MessageFactory.FLAGS_EXCEPTION, data, MessageFactory.FLAGS_EXCEPTION, 123);
		header = MessageFactory.parseMessageHeader(headerdata);
	}
	
	[Test]
	public void TestMessageInvalidVersion()
	{
		Config.PROTOCOL_VERSION = 46;
		byte[] data = new byte[] { 1,2,3,4 };
		try {
			byte[] headerdata = MessageFactory.createMsgHeader(MessageFactory.FLAGS_EXCEPTION, data, MessageFactory.FLAGS_EXCEPTION, 123);
			Assert.Fail("expected error");
		} catch (ArgumentException x) {
			// ok
		}
		Config.PROTOCOL_VERSION = 43;
		try {
			byte[] headerdata = MessageFactory.createMsgHeader(MessageFactory.FLAGS_EXCEPTION, data, MessageFactory.FLAGS_EXCEPTION, 123);
			Assert.Fail("expected error");
		} catch (ArgumentException x) {
			// ok
		}
	}	

    [Test]
	public void testMessage()
	{
		Config.HMAC_KEY = null;
		byte[] data = new Byte[] { 1,2,3,4 };
		byte[] headerdata = MessageFactory.createMsgHeader(MessageFactory.FLAGS_EXCEPTION, data, MessageFactory.FLAGS_EXCEPTION, 123);
		MessageHeader header = MessageFactory.parseMessageHeader(headerdata);
		Assert.AreEqual(4, header.datasize);
		Assert.AreEqual(MessageFactory.FLAGS_EXCEPTION, header.flags);
		Assert.AreEqual(123, header.sequence);
		Assert.AreEqual(MessageFactory.MSG_CONNECT, header.type);
		Assert.AreEqual(new byte[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, header.hmac);
		
		MemoryStream ms=new MemoryStream();
		ms.Write(headerdata, 0, headerdata.Length);
		ms.Write(data, 0, data.Length);
		ms.Seek(0, SeekOrigin.Begin);
		try {
			MessageFactory.getMessage(ms, MessageFactory.MSG_CONNECTFAIL);
			Assert.Fail("expected PyroException");
		} catch (PyroException) {
			// ok (invalid message type)
		}

		ms.Seek(0, SeekOrigin.Begin);
		Message msg = MessageFactory.getMessage(ms, MessageFactory.MSG_CONNECT);
		Assert.AreEqual(MessageFactory.FLAGS_EXCEPTION, msg.flags);
		Assert.AreEqual(123, msg.sequence);
		Assert.AreEqual(MessageFactory.FLAGS_EXCEPTION, msg.type);
		Assert.AreEqual(data, msg.data);
	}

	[Test]
	public void testHmac()
	{
		Config.HMAC_KEY = Encoding.UTF8.GetBytes("hello");
		byte[] hmac = MessageFactory.makeHMAC(new byte[] {1,2,3,4});
		Assert.AreEqual(20, hmac.Length);
		Assert.AreNotEqual(new byte[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, hmac);

		byte[] data = new Byte[] { 1,2,3,4 };
		byte[] headerdata = MessageFactory.createMsgHeader(MessageFactory.MSG_CONNECT, data, MessageFactory.FLAGS_EXCEPTION, 123);
		MessageHeader header = MessageFactory.parseMessageHeader(headerdata);
		Assert.AreEqual(4, header.datasize);
		Assert.AreEqual(MessageFactory.FLAGS_EXCEPTION | MessageFactory.FLAGS_HMAC, header.flags);
		Assert.AreEqual(123, header.sequence);
		Assert.AreEqual(MessageFactory.MSG_CONNECT, header.type);
		Assert.AreNotEqual(new byte[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, header.hmac);
		
		MemoryStream ms=new MemoryStream();
		ms.Write(headerdata, 0, headerdata.Length);
		ms.Write(data, 0, data.Length);
		ms.Seek(0, SeekOrigin.Begin);
		try {
			MessageFactory.getMessage(ms, MessageFactory.MSG_CONNECTFAIL);
			Assert.Fail("expected PyroException");
		} catch (PyroException) {
			// ok (invalid message type)
		}

		ms.Seek(0, SeekOrigin.Begin);
		Config.HMAC_KEY = Encoding.UTF8.GetBytes("faulty key");
		try {
			MessageFactory.getMessage(ms, MessageFactory.MSG_CONNECT);
			Assert.Fail("expected PyroException");
		} catch (PyroException) {
			// ok (invalid hmac)
		}
		
		Config.HMAC_KEY = Encoding.UTF8.GetBytes("hello");
		ms.Seek(0, SeekOrigin.Begin);
		Message msg = MessageFactory.getMessage(ms, MessageFactory.MSG_CONNECT);
		Assert.AreEqual(MessageFactory.FLAGS_EXCEPTION | MessageFactory.FLAGS_HMAC, msg.flags);
		Assert.AreEqual(123, msg.sequence);
		Assert.AreEqual(MessageFactory.MSG_CONNECT, msg.type);
		Assert.AreEqual(data, msg.data);		
	}
}

}