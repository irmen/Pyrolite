/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.IO;
using System.Text;
using Xunit;
using Razorvine.Pickle;
// ReSharper disable CheckNamespace
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Pyrolite.Tests.Pickle
{
	
/// <summary>
/// Unit tests for the pickler utils. 
/// </summary>
public class PickleUtilsTest {

	private readonly byte[] filedata;

	public PickleUtilsTest() {
		filedata=Encoding.UTF8.GetBytes("str1\nstr2  \n  str3  \nend");
	}

	[Fact]
	public void testReadline() {
		Stream bis = new MemoryStream(filedata);
		Assert.Equal("str1", PickleUtils.readline(bis));
		Assert.Equal("str2  ", PickleUtils.readline(bis));
		Assert.Equal("  str3  ", PickleUtils.readline(bis));
		Assert.Equal("end", PickleUtils.readline(bis));
		try
		{
			PickleUtils.readline(bis);
			Assert.True(false, "expected IOException");
		}
		catch(IOException) {}
	}

	[Fact]
	public void testReadlineWithLF() {
		Stream bis=new MemoryStream(filedata);
		Assert.Equal("str1\n", PickleUtils.readline(bis, true));
		Assert.Equal("str2  \n", PickleUtils.readline(bis, true));
		Assert.Equal("  str3  \n", PickleUtils.readline(bis, true));
		Assert.Equal("end", PickleUtils.readline(bis, true));
		try
		{
			PickleUtils.readline(bis, true);
			Assert.True(false, "expected IOException");
		}
		catch(IOException) {}
	}

	[Fact]
	public void testReadbytes() {
		Stream bis=new MemoryStream(filedata);
		
		Assert.Equal(115, PickleUtils.readbyte(bis));
		Assert.Equal(new byte[]{}, PickleUtils.readbytes(bis, 0));
		Assert.Equal(new byte[]{116}, PickleUtils.readbytes(bis, 1));
		Assert.Equal(new byte[]{114,49,10,115,116}, PickleUtils.readbytes(bis, 5));
		try
		{
			PickleUtils.readbytes(bis, 999);
			Assert.True(false, "expected IOException");
		}
		catch(IOException) {}
	}

	[Fact]
	public void testReadbytes_into() {
		Stream bis=new MemoryStream(filedata);
		byte[] bytes = new byte[] {0,0,0,0,0,0,0,0,0,0};
		PickleUtils.readbytes_into(bis, bytes, 1, 4);
		Assert.Equal(new byte[] {0,115,116,114,49,0,0,0,0,0}, bytes);
		PickleUtils.readbytes_into(bis, bytes, 8, 1);
		Assert.Equal(new byte[] {0,115,116,114,49,0,0,0,10,0}, bytes);
	}

	[Fact]
	public void testBytes_to_integer() {
		try {
			PickleUtils.bytes_to_integer(new byte[] {});
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_to_integer(new byte[] {0});
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
		Assert.Equal(0x00000000, PickleUtils.bytes_to_integer(new byte[] {0x00, 0x00}));
		Assert.Equal(0x00003412, PickleUtils.bytes_to_integer(new byte[] {0x12, 0x34}));
		Assert.Equal(0x0000ffff, PickleUtils.bytes_to_integer(new byte[] {0xff, 0xff}));
		Assert.Equal(0x00000000, PickleUtils.bytes_to_integer(new byte[] {0,0,0,0}));
		Assert.Equal(0x12345678, PickleUtils.bytes_to_integer(new byte[] {0x78, 0x56, 0x34, 0x12}));
		Assert.Equal(-8380352,   PickleUtils.bytes_to_integer(new byte[] {0x40, 0x20, 0x80, 0xff}));
		Assert.Equal(0x01cc02ee, PickleUtils.bytes_to_integer(new byte[] {0xee, 0x02, 0xcc, 0x01}));
		Assert.Equal(-872288766, PickleUtils.bytes_to_integer(new byte[] {0x02, 0xee, 0x01, 0xcc}));
		Assert.Equal(-285212674, PickleUtils.bytes_to_integer(new byte[] {0xfe, 0xff, 0xff, 0xee}));
		try
		{
			PickleUtils.bytes_to_integer(new byte[] { 200,50,25,100,1,2,3,4});
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
	}

	[Fact]
	public void testBytes_to_uint() {
		try {
			PickleUtils.bytes_to_uint(new byte[] {},0);
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_to_uint(new byte[] {0},0);
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
		Assert.Equal(0x000000000L, PickleUtils.bytes_to_uint(new byte[] {0,0,0,0} ,0));
		Assert.Equal(0x012345678L, PickleUtils.bytes_to_uint(new byte[] {0x78, 0x56, 0x34, 0x12} ,0));
		Assert.Equal(0x0ff802040L, PickleUtils.bytes_to_uint(new byte[] {0x40, 0x20, 0x80, 0xff} ,0));
		Assert.Equal(0x0eefffffeL, PickleUtils.bytes_to_uint(new byte[] {0xfe, 0xff, 0xff,0xee} ,0));
	}

	[Fact]
	public void testBytes_to_long() {
		try {
			PickleUtils.bytes_to_long(new byte[] {}, 0);
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_to_long(new byte[] {0}, 0);
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
	    
		Assert.Equal(0x00000000L, PickleUtils.bytes_to_long(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00} ,0));
		Assert.Equal(0x00003412L, PickleUtils.bytes_to_long(new byte[] {0x12, 0x34, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00} ,0));
		Assert.Equal(-0xffffffffffff01L, PickleUtils.bytes_to_long(new byte[] {0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff} ,0));
		Assert.Equal(0L, PickleUtils.bytes_to_long(new byte[] {0,0,0,0,0,0,0,0} ,0));
		Assert.Equal(-0x778899aabbccddefL, PickleUtils.bytes_to_long(new byte[] {0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88} ,0));
		Assert.Equal(0x1122334455667788L, PickleUtils.bytes_to_long(new byte[] {0x88,0x77,0x66,0x55,0x44,0x33,0x22,0x11} ,0));
		Assert.Equal(-1L, PickleUtils.bytes_to_long(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff} ,0));
		Assert.Equal(-2L, PickleUtils.bytes_to_long(new byte[] {0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff} ,0));
	}
		
	[Fact]
	public void testInteger_to_bytes()
	{
		Assert.Equal(new byte[]{0,0,0,0}, PickleUtils.integer_to_bytes(0));
		Assert.Equal(new byte[]{0x78, 0x56, 0x34, 0x12}, PickleUtils.integer_to_bytes(0x12345678));
		Assert.Equal(new byte[]{0x40, 0x20, 0x80, 0xff}, PickleUtils.integer_to_bytes(-8380352));
		Assert.Equal(new byte[]{0xfe, 0xff, 0xff ,0xee}, PickleUtils.integer_to_bytes(-285212674));
		Assert.Equal(new byte[]{0xff, 0xff, 0xff, 0xff}, PickleUtils.integer_to_bytes(-1));
		Assert.Equal(new byte[]{0xee, 0x02, 0xcc, 0x01}, PickleUtils.integer_to_bytes(0x01cc02ee));
		Assert.Equal(new byte[]{0x02, 0xee, 0x01, 0xcc}, PickleUtils.integer_to_bytes(-872288766));
	}
	
	[Fact]
	public void testBytes_to_double() {
		try {
			PickleUtils.bytes_bigendian_to_double(new byte[] {} ,0);
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_bigendian_to_double(new byte[] {0} ,0);
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
		Assert.Equal(0.0d, PickleUtils.bytes_bigendian_to_double(new byte[] {0,0,0,0,0,0,0,0} ,0));
		Assert.Equal(1.0d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x3f,0xf0,0,0,0,0,0,0} ,0));
		Assert.Equal(1.1d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x3f,0xf1,0x99,0x99,0x99,0x99,0x99,0x9a} ,0));
		Assert.Equal(1234.5678d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x40,0x93,0x4a,0x45,0x6d,0x5c,0xfa,0xad} ,0));
		Assert.Equal(2.17e123d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x59,0x8a,0x42,0xd1,0xce,0xf5,0x3f,0x46} ,0));
		Assert.Equal(1.23456789e300d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x7e,0x3d,0x7e,0xe8,0xbc,0xaf,0x28,0x3a} ,0));
		Assert.Equal(double.PositiveInfinity, PickleUtils.bytes_bigendian_to_double(new byte[] {0x7f,0xf0,0,0,0,0,0,0} ,0));
		Assert.Equal(double.NegativeInfinity, PickleUtils.bytes_bigendian_to_double(new byte[] {0xff,0xf0,0,0,0,0,0,0} ,0));
		try
		{
			PickleUtils.bytes_bigendian_to_double(new byte[] { 200,50,25,100} ,0);
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}

		// test offset
		Assert.Equal(1.23456789e300d, PickleUtils.bytes_bigendian_to_double(new byte[] {0,0,0,0x7e,0x3d,0x7e,0xe8,0xbc,0xaf,0x28,0x3a} ,3));
		Assert.Equal(1.23456789e300d, PickleUtils.bytes_bigendian_to_double(new byte[] {0x7e,0x3d,0x7e,0xe8,0xbc,0xaf,0x28,0x3a,0,0,0} ,0));
	}
	
	[Fact]
	public void testBytes_to_float() {
		try {
			PickleUtils.bytes_bigendian_to_float(new byte[] {}, 0);
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
		try {
			PickleUtils.bytes_bigendian_to_float(new byte[] {0}, 0);
			Assert.True(false, "expected PickleException");
		} catch (PickleException) {}
		Assert.True(0.0f == PickleUtils.bytes_bigendian_to_float(new byte[] {0,0,0,0}, 0));
		Assert.True(1.0f == PickleUtils.bytes_bigendian_to_float(new byte[] {0x3f,0x80,0,0} ,0));
		Assert.True(1.1f == PickleUtils.bytes_bigendian_to_float(new byte[] {0x3f,0x8c,0xcc,0xcd} ,0));
		Assert.True(1234.5678f == PickleUtils.bytes_bigendian_to_float(new byte[] {0x44,0x9a,0x52,0x2b} ,0));
		Assert.True(float.PositiveInfinity == PickleUtils.bytes_bigendian_to_float(new byte[] {0x7f,0x80,0,0} ,0));
		Assert.True(float.NegativeInfinity == PickleUtils.bytes_bigendian_to_float(new byte[] {0xff,0x80,0,0} ,0));

		// test offset
		Assert.True(1234.5678f == PickleUtils.bytes_bigendian_to_float(new byte[] {0,0,0, 0x44,0x9a,0x52,0x2b} ,3));
		Assert.True(1234.5678f == PickleUtils.bytes_bigendian_to_float(new byte[] {0x44,0x9a,0x52,0x2b,0,0,0} ,0));
	}
	
	[Fact]
	public void testDouble_to_bytes()
	{
		Assert.Equal(new byte[]{0,0,0,0,0,0,0,0}, PickleUtils.double_to_bytes_bigendian(0.0d));
		Assert.Equal(new byte[]{0x3f,0xf0,0,0,0,0,0,0}, PickleUtils.double_to_bytes_bigendian(1.0d));
		Assert.Equal(new byte[]{0x3f,0xf1,0x99,0x99,0x99,0x99,0x99,0x9a}, PickleUtils.double_to_bytes_bigendian(1.1d));
		Assert.Equal(new byte[]{0x40,0x93,0x4a,0x45,0x6d,0x5c,0xfa,0xad}, PickleUtils.double_to_bytes_bigendian(1234.5678d));
		Assert.Equal(new byte[]{0x59,0x8a,0x42,0xd1,0xce,0xf5,0x3f,0x46}, PickleUtils.double_to_bytes_bigendian(2.17e123d));
		Assert.Equal(new byte[]{0x7e,0x3d,0x7e,0xe8,0xbc,0xaf,0x28,0x3a}, PickleUtils.double_to_bytes_bigendian(1.23456789e300d));
		// cannot test NaN because it's not always the same byte representation...
		// Assert.Equal(new byte[]{0xff,0xf8,0,0,0,0,0,0}, p.double_to_bytes(Double.NaN));
		Assert.Equal(new byte[]{0x7f,0xf0,0,0,0,0,0,0}, PickleUtils.double_to_bytes_bigendian(Double.PositiveInfinity));
		Assert.Equal(new byte[]{0xff,0xf0,0,0,0,0,0,0}, PickleUtils.double_to_bytes_bigendian(Double.NegativeInfinity));
	}

	[Fact]
	public void testDecode_long()
	{
		Assert.Equal(0L, PickleUtils.decode_long(new byte[0]));
		Assert.Equal(0L, PickleUtils.decode_long(new byte[]{0}));
		Assert.Equal(1L, PickleUtils.decode_long(new byte[]{1}));
		Assert.Equal(10L, PickleUtils.decode_long(new byte[]{10}));
		Assert.Equal(255L, PickleUtils.decode_long(new byte[]{0xff,0x00}));
		Assert.Equal(32767L, PickleUtils.decode_long(new byte[]{0xff,0x7f}));
		Assert.Equal(-256L, PickleUtils.decode_long(new byte[]{0x00,0xff}));
		Assert.Equal(-32768L, PickleUtils.decode_long(new byte[]{0x00,0x80}));
		Assert.Equal(-128L, PickleUtils.decode_long(new byte[]{0x80}));
		Assert.Equal(127L, PickleUtils.decode_long(new byte[]{0x7f}));
		Assert.Equal(128L, PickleUtils.decode_long(new byte[]{0x80, 0x00}));

		Assert.Equal(128L, PickleUtils.decode_long(new byte[]{0x80, 0x00}));
		Assert.Equal(0x78563412L, PickleUtils.decode_long(new byte[]{0x12,0x34,0x56,0x78}));
		Assert.Equal(0x785634f2L, PickleUtils.decode_long(new byte[]{0xf2,0x34,0x56,0x78}));
		Assert.Equal(0x12345678L, PickleUtils.decode_long(new byte[]{0x78,0x56,0x34,0x12}));
		
		Assert.Equal(-231451016L, PickleUtils.decode_long(new byte[]{0x78,0x56,0x34,0xf2}));
		Assert.Equal(0xf2345678L, PickleUtils.decode_long(new byte[]{0x78,0x56,0x34,0xf2,0x00}));
	}

	[Fact]
	public void testDecode_escaped()
	{
		Assert.Equal("abc", PickleUtils.decode_escaped("abc"));
		Assert.Equal("a\\c", PickleUtils.decode_escaped("a\\\\c"));
		Assert.Equal("a\u0042c", PickleUtils.decode_escaped("a\\x42c"));
		Assert.Equal("a\nc", PickleUtils.decode_escaped("a\\nc"));
		Assert.Equal("a\tc", PickleUtils.decode_escaped("a\\tc"));
		Assert.Equal("a\rc", PickleUtils.decode_escaped("a\\rc"));
		Assert.Equal("a'c", PickleUtils.decode_escaped("a\\'c"));
	}
	
	[Fact]
	public void testDecode_unicode_escaped()
	{
		Assert.Equal("abc", PickleUtils.decode_unicode_escaped("abc"));
		Assert.Equal("a\\c", PickleUtils.decode_unicode_escaped("a\\\\c"));
		Assert.Equal("a\u0042c", PickleUtils.decode_unicode_escaped("a\\u0042c"));
		Assert.Equal("a\nc", PickleUtils.decode_unicode_escaped("a\\nc"));
		Assert.Equal("a\tc", PickleUtils.decode_unicode_escaped("a\\tc"));
		Assert.Equal("a\rc", PickleUtils.decode_unicode_escaped("a\\rc"));
	}
}


}
