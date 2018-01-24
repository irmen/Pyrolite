/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Razorvine.Pickle;

namespace Pyrolite.Tests.Pickle
{
	
/// <summary>
/// Unit tests for the pickler utils. 
/// </summary>
[TestClass]
public class PickleUtilsTest {

	private byte[] filedata;

	[TestInitialize]
	public void setUp() {
		filedata=Encoding.UTF8.GetBytes("str1\nstr2  \n  str3  \nend");
	}

	[TestCleanup]
	public void tearDown() {
	}
	
	
	[TestMethod]
	public void testReadline() {
		Stream bis = new MemoryStream(filedata);
		Assert.AreEqual("str1", PickleUtils.readline(bis));
		Assert.AreEqual("str2  ", PickleUtils.readline(bis));
		Assert.AreEqual("  str3  ", PickleUtils.readline(bis));
		Assert.AreEqual("end", PickleUtils.readline(bis));
		try
		{
			PickleUtils.readline(bis);
			Assert.Fail("expected IOException");
		}
		catch(IOException) {}
	}

	[TestMethod]
	public void testReadlineWithLF() {
		Stream bis=new MemoryStream(filedata);
		Assert.AreEqual("str1\n", PickleUtils.readline(bis, true));
		Assert.AreEqual("str2  \n", PickleUtils.readline(bis, true));
		Assert.AreEqual("  str3  \n", PickleUtils.readline(bis, true));
		Assert.AreEqual("end", PickleUtils.readline(bis, true));
		try
		{
			PickleUtils.readline(bis, true);
			Assert.Fail("expected IOException");
		}
		catch(IOException) {}
	}

	[TestMethod]
	public void testReadbytes() {
		Stream bis=new MemoryStream(filedata);
		
		Assert.AreEqual(115, PickleUtils.readbyte(bis));
		Assert.AreEqual(new byte[]{}, PickleUtils.readbytes(bis, 0));
		Assert.AreEqual(new byte[]{116}, PickleUtils.readbytes(bis, 1));
		Assert.AreEqual(new byte[]{114,49,10,115,116}, PickleUtils.readbytes(bis, 5));
		try
		{
			PickleUtils.readbytes(bis, 999);
			Assert.Fail("expected IOException");
		}
		catch(IOException) {}
	}

	[TestMethod]
	public void testReadbytes_into() {
		Stream bis=new MemoryStream(filedata);
		byte[] bytes = new byte[] {0,0,0,0,0,0,0,0,0,0};
		PickleUtils.readbytes_into(bis, bytes, 1, 4);
		Assert.AreEqual(new byte[] {0,115,116,114,49,0,0,0,0,0}, bytes);
		PickleUtils.readbytes_into(bis, bytes, 8, 1);
		Assert.AreEqual(new byte[] {0,115,116,114,49,0,0,0,10,0}, bytes);
	}

	[TestMethod]
	public void testBytes_to_integer() {
		try {
			PickleUtils.bytes_to_integer(new byte[] {});
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_to_integer(new byte[] {0});
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
		Assert.AreEqual(0x00000000, PickleUtils.bytes_to_integer(new byte[] {0x00, 0x00}));
		Assert.AreEqual(0x00003412, PickleUtils.bytes_to_integer(new byte[] {0x12, 0x34}));
		Assert.AreEqual(0x0000ffff, PickleUtils.bytes_to_integer(new byte[] {0xff, 0xff}));
		Assert.AreEqual(0x00000000, PickleUtils.bytes_to_integer(new byte[] {0,0,0,0}));
		Assert.AreEqual(0x12345678, PickleUtils.bytes_to_integer(new byte[] {0x78, 0x56, 0x34, 0x12}));
		Assert.AreEqual(-8380352,   PickleUtils.bytes_to_integer(new byte[] {0x40, 0x20, 0x80, 0xff}));
		Assert.AreEqual(0x01cc02ee, PickleUtils.bytes_to_integer(new byte[] {0xee, 0x02, 0xcc, 0x01}));
		Assert.AreEqual(-872288766, PickleUtils.bytes_to_integer(new byte[] {0x02, 0xee, 0x01, 0xcc}));
		Assert.AreEqual(-285212674, PickleUtils.bytes_to_integer(new byte[] {0xfe, 0xff, 0xff, 0xee}));
		try
		{
			PickleUtils.bytes_to_integer(new byte[] { 200,50,25,100,1,2,3,4});
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
	}

	[TestMethod]
	public void testBytes_to_uint() {
		try {
			PickleUtils.bytes_to_uint(new byte[] {},0);
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_to_uint(new byte[] {0},0);
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
		Assert.AreEqual(0x000000000L, PickleUtils.bytes_to_uint(new byte[] {0,0,0,0} ,0));
		Assert.AreEqual(0x012345678L, PickleUtils.bytes_to_uint(new byte[] {0x78, 0x56, 0x34, 0x12} ,0));
		Assert.AreEqual(0x0ff802040L, PickleUtils.bytes_to_uint(new byte[] {0x40, 0x20, 0x80, 0xff} ,0));
		Assert.AreEqual(0x0eefffffeL, PickleUtils.bytes_to_uint(new byte[] {0xfe, 0xff, 0xff,0xee} ,0));
	}

	[TestMethod]
	public void testBytes_to_long() {
		try {
			PickleUtils.bytes_to_long(new byte[] {}, 0);
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_to_long(new byte[] {0}, 0);
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
	    
		Assert.AreEqual(0x00000000L, PickleUtils.bytes_to_long(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00} ,0));
		Assert.AreEqual(0x00003412L, PickleUtils.bytes_to_long(new byte[] {0x12, 0x34, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00} ,0));
		Assert.AreEqual(-0xffffffffffff01L, PickleUtils.bytes_to_long(new byte[] {0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff} ,0));
		Assert.AreEqual(0L, PickleUtils.bytes_to_long(new byte[] {0,0,0,0,0,0,0,0} ,0));
		Assert.AreEqual(-0x778899aabbccddefL, PickleUtils.bytes_to_long(new byte[] {0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88} ,0));
		Assert.AreEqual(0x1122334455667788L, PickleUtils.bytes_to_long(new byte[] {0x88,0x77,0x66,0x55,0x44,0x33,0x22,0x11} ,0));
		Assert.AreEqual(-1L, PickleUtils.bytes_to_long(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff} ,0));
		Assert.AreEqual(-2L, PickleUtils.bytes_to_long(new byte[] {0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff} ,0));
	}
		
	[TestMethod]
	public void testInteger_to_bytes()
	{
		Assert.AreEqual(new byte[]{0,0,0,0}, PickleUtils.integer_to_bytes(0));
		Assert.AreEqual(new byte[]{0x78, 0x56, 0x34, 0x12}, PickleUtils.integer_to_bytes(0x12345678));
		Assert.AreEqual(new byte[]{0x40, 0x20, 0x80, 0xff}, PickleUtils.integer_to_bytes(-8380352));
		Assert.AreEqual(new byte[]{0xfe, 0xff, 0xff ,0xee}, PickleUtils.integer_to_bytes(-285212674));
		Assert.AreEqual(new byte[]{0xff, 0xff, 0xff, 0xff}, PickleUtils.integer_to_bytes(-1));
		Assert.AreEqual(new byte[]{0xee, 0x02, 0xcc, 0x01}, PickleUtils.integer_to_bytes(0x01cc02ee));
		Assert.AreEqual(new byte[]{0x02, 0xee, 0x01, 0xcc}, PickleUtils.integer_to_bytes(-872288766));
	}
	
	[TestMethod]
	public void testBytes_to_double() {
		try {
			PickleUtils.bytes_bigendian_to_double(new byte[] {} ,0);
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_bigendian_to_double(new byte[] {0} ,0);
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
		Assert.AreEqual(0.0d, PickleUtils.bytes_bigendian_to_double(new byte[] {0,0,0,0,0,0,0,0} ,0));
		Assert.AreEqual(1.0d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x3f,0xf0,0,0,0,0,0,0} ,0));
		Assert.AreEqual(1.1d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x3f,0xf1,0x99,0x99,0x99,0x99,0x99,0x9a} ,0));
		Assert.AreEqual(1234.5678d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x40,0x93,0x4a,0x45,0x6d,0x5c,0xfa,0xad} ,0));
		Assert.AreEqual(2.17e123d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x59,0x8a,0x42,0xd1,0xce,0xf5,0x3f,0x46} ,0));
		Assert.AreEqual(1.23456789e300d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x7e,0x3d,0x7e,0xe8,0xbc,0xaf,0x28,0x3a} ,0));
		Assert.AreEqual(double.PositiveInfinity, PickleUtils.bytes_bigendian_to_double(new byte[] {0x7f,0xf0,0,0,0,0,0,0} ,0));
		Assert.AreEqual(double.NegativeInfinity, PickleUtils.bytes_bigendian_to_double(new byte[] {0xff,0xf0,0,0,0,0,0,0} ,0));
		try
		{
			PickleUtils.bytes_bigendian_to_double(new byte[] { 200,50,25,100} ,0);
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}

		// test offset
		Assert.AreEqual(1.23456789e300d, PickleUtils.bytes_bigendian_to_double(new byte[] {0,0,0,0x7e,0x3d,0x7e,0xe8,0xbc,0xaf,0x28,0x3a} ,3));
		Assert.AreEqual(1.23456789e300d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x7e,0x3d,0x7e,0xe8,0xbc,0xaf,0x28,0x3a,0,0,0} ,0));
	}
	
	[TestMethod]
	public void testBytes_to_float() {
		try {
			PickleUtils.bytes_bigendian_to_float(new byte[] {}, 0);
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_bigendian_to_float(new byte[] {0}, 0);
			Assert.Fail("expected PickleException");
		} catch (PickleException) {}
		Assert.IsTrue(0.0f == PickleUtils.bytes_bigendian_to_float(new byte[] {0,0,0,0}, 0));
		Assert.IsTrue(1.0f == PickleUtils.bytes_bigendian_to_float(new byte[] {0x3f,0x80,0,0} ,0));
		Assert.IsTrue(1.1f == PickleUtils.bytes_bigendian_to_float(new byte[] {0x3f,0x8c,0xcc,0xcd} ,0));
		Assert.IsTrue(1234.5678f == PickleUtils.bytes_bigendian_to_float(new byte[] {0x44,0x9a,0x52,0x2b} ,0));
		Assert.IsTrue(float.PositiveInfinity == PickleUtils.bytes_bigendian_to_float(new byte[] {0x7f,0x80,0,0} ,0));
		Assert.IsTrue(float.NegativeInfinity == PickleUtils.bytes_bigendian_to_float(new byte[] {0xff,0x80,0,0} ,0));

		// test offset
		Assert.IsTrue(1234.5678f == PickleUtils.bytes_bigendian_to_float(new byte[] {0,0,0, 0x44,0x9a,0x52,0x2b} ,3));
		Assert.IsTrue(1234.5678f == PickleUtils.bytes_bigendian_to_float(new byte[] {0x44,0x9a,0x52,0x2b,0,0,0} ,0));
	}
	
	[TestMethod]
	public void testDouble_to_bytes()
	{
		Assert.AreEqual(new byte[]{0,0,0,0,0,0,0,0}, PickleUtils.double_to_bytes_bigendian(0.0d));
		Assert.AreEqual(new byte[]{0x3f,0xf0,0,0,0,0,0,0}, PickleUtils.double_to_bytes_bigendian(1.0d));
		Assert.AreEqual(new byte[]{0x3f,0xf1,0x99,0x99,0x99,0x99,0x99,0x9a}, PickleUtils.double_to_bytes_bigendian(1.1d));
		Assert.AreEqual(new byte[]{0x40,0x93,0x4a,0x45,0x6d,0x5c,0xfa,0xad}, PickleUtils.double_to_bytes_bigendian(1234.5678d));
		Assert.AreEqual(new byte[]{0x59,0x8a,0x42,0xd1,0xce,0xf5,0x3f,0x46}, PickleUtils.double_to_bytes_bigendian(2.17e123d));
		Assert.AreEqual(new byte[]{0x7e,0x3d,0x7e,0xe8,0xbc,0xaf,0x28,0x3a}, PickleUtils.double_to_bytes_bigendian(1.23456789e300d));
		// cannot test NaN because it's not always the same byte representation...
		// Assert.AreEqual(new byte[]{0xff,0xf8,0,0,0,0,0,0}, p.double_to_bytes(Double.NaN));
		Assert.AreEqual(new byte[]{0x7f,0xf0,0,0,0,0,0,0}, PickleUtils.double_to_bytes_bigendian(Double.PositiveInfinity));
		Assert.AreEqual(new byte[]{0xff,0xf0,0,0,0,0,0,0}, PickleUtils.double_to_bytes_bigendian(Double.NegativeInfinity));
	}

	[TestMethod]
	public void testDecode_long()
	{
		Assert.AreEqual(0L, PickleUtils.decode_long(new byte[0]));
		Assert.AreEqual(0L, PickleUtils.decode_long(new byte[]{0}));
		Assert.AreEqual(1L, PickleUtils.decode_long(new byte[]{1}));
		Assert.AreEqual(10L, PickleUtils.decode_long(new byte[]{10}));
		Assert.AreEqual(255L, PickleUtils.decode_long(new byte[]{0xff,0x00}));
		Assert.AreEqual(32767L, PickleUtils.decode_long(new byte[]{0xff,0x7f}));
		Assert.AreEqual(-256L, PickleUtils.decode_long(new byte[]{0x00,0xff}));
		Assert.AreEqual(-32768L, PickleUtils.decode_long(new byte[]{0x00,0x80}));
		Assert.AreEqual(-128L, PickleUtils.decode_long(new byte[]{0x80}));
		Assert.AreEqual(127L, PickleUtils.decode_long(new byte[]{0x7f}));
		Assert.AreEqual(128L, PickleUtils.decode_long(new byte[]{0x80, 0x00}));

		Assert.AreEqual(128L, PickleUtils.decode_long(new byte[]{0x80, 0x00}));
		Assert.AreEqual(0x78563412L, PickleUtils.decode_long(new byte[]{0x12,0x34,0x56,0x78}));
		Assert.AreEqual(0x785634f2L, PickleUtils.decode_long(new byte[]{0xf2,0x34,0x56,0x78}));
		Assert.AreEqual(0x12345678L, PickleUtils.decode_long(new byte[]{0x78,0x56,0x34,0x12}));
		
		Assert.AreEqual(-231451016L, PickleUtils.decode_long(new byte[]{0x78,0x56,0x34,0xf2}));
		Assert.AreEqual(0xf2345678L, PickleUtils.decode_long(new byte[]{0x78,0x56,0x34,0xf2,0x00}));
	}

	[TestMethod]
	public void testDecode_escaped()
	{
		Assert.AreEqual("abc", PickleUtils.decode_escaped("abc"));
		Assert.AreEqual("a\\c", PickleUtils.decode_escaped("a\\\\c"));
		Assert.AreEqual("a\u0042c", PickleUtils.decode_escaped("a\\x42c"));
		Assert.AreEqual("a\nc", PickleUtils.decode_escaped("a\\nc"));
		Assert.AreEqual("a\tc", PickleUtils.decode_escaped("a\\tc"));
		Assert.AreEqual("a\rc", PickleUtils.decode_escaped("a\\rc"));
		Assert.AreEqual("a'c", PickleUtils.decode_escaped("a\\'c"));
	}
	
	[TestMethod]
	public void testDecode_unicode_escaped()
	{
		Assert.AreEqual("abc", PickleUtils.decode_unicode_escaped("abc"));
		Assert.AreEqual("a\\c", PickleUtils.decode_unicode_escaped("a\\\\c"));
		Assert.AreEqual("a\u0042c", PickleUtils.decode_unicode_escaped("a\\u0042c"));
		Assert.AreEqual("a\nc", PickleUtils.decode_unicode_escaped("a\\nc"));
		Assert.AreEqual("a\tc", PickleUtils.decode_unicode_escaped("a\\tc"));
		Assert.AreEqual("a\rc", PickleUtils.decode_unicode_escaped("a\\rc"));
	}
}


}
