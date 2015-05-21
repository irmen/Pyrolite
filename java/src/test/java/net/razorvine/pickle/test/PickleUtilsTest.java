package net.razorvine.pickle.test;

import static org.junit.Assert.*;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.math.BigInteger;

import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.PickleUtils;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

/**
 * Unit tests for the pickler utils.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PickleUtilsTest {

	private byte[] filedata;

	@Before
	public void setUp() throws Exception {
		filedata="str1\nstr2  \n  str3  \nend".getBytes("UTF-8");
	}

	@After
	public void tearDown() throws Exception {
	}
	
	
	@Test
	public void testReadline() throws IOException {
		InputStream bis=new ByteArrayInputStream(filedata);
		assertEquals("str1", PickleUtils.readline(bis));
		assertEquals("str2  ", PickleUtils.readline(bis));
		assertEquals("  str3  ", PickleUtils.readline(bis));
		assertEquals("end", PickleUtils.readline(bis));
		try
		{
			PickleUtils.readline(bis);
			fail("expected IOException");
		}
		catch(IOException x) {}
	}

	@Test
	public void testReadlineWithLF() throws IOException {
		InputStream bis=new ByteArrayInputStream(filedata);
		assertEquals("str1\n", PickleUtils.readline(bis, true));
		assertEquals("str2  \n", PickleUtils.readline(bis, true));
		assertEquals("  str3  \n", PickleUtils.readline(bis, true));
		assertEquals("end", PickleUtils.readline(bis, true));
		try
		{
			PickleUtils.readline(bis, true);
			fail("expected IOException");
		}
		catch(IOException x) {}
	}

	@Test
	public void testReadbytes() throws IOException {
		InputStream bis=new ByteArrayInputStream(filedata);
		
		assertEquals(115, PickleUtils.readbyte(bis));
		assertArrayEquals(new byte[]{}, PickleUtils.readbytes(bis, 0));
		assertArrayEquals(new byte[]{116}, PickleUtils.readbytes(bis, 1));
		assertArrayEquals(new byte[]{114,49,10,115,116}, PickleUtils.readbytes(bis, 5));
		try
		{
			PickleUtils.readbytes(bis, 999);
			fail("expected IOException");
		}
		catch(IOException x) {}
	}

	@Test
	public void testReadbytes_into() throws IOException {
		InputStream bis=new ByteArrayInputStream(filedata);
		byte[] bytes = new byte[] {0,0,0,0,0,0,0,0,0,0};
		PickleUtils.readbytes_into(bis, bytes, 1, 4);
		assertArrayEquals(new byte[] {0,115,116,114,49,0,0,0,0,0}, bytes);
		PickleUtils.readbytes_into(bis, bytes, 8, 1);
		assertArrayEquals(new byte[] {0,115,116,114,49,0,0,0,10,0}, bytes);
	}

	@Test
	public void testBytes_to_integer() {
		try {
			PickleUtils.bytes_to_integer(new byte[] {});
			fail("expected PickleException");
		} catch (PickleException x) {}
		try {
			PickleUtils.bytes_to_integer(new byte[] {0});
			fail("expected PickleException");
		} catch (PickleException x) {}
		assertEquals(0x00000000, PickleUtils.bytes_to_integer(new byte[] {0x00, 0x00}));
		assertEquals(0x00003412, PickleUtils.bytes_to_integer(new byte[] {0x12, 0x34}));
		assertEquals(0x0000ffff, PickleUtils.bytes_to_integer(new byte[] {(byte)0xff, (byte)0xff}));
		assertEquals(0x00000000, PickleUtils.bytes_to_integer(new byte[] {0,0,0,0}));
		assertEquals(0x12345678, PickleUtils.bytes_to_integer(new byte[] {0x78, 0x56, 0x34, 0x12}));
		assertEquals(0xff802040, PickleUtils.bytes_to_integer(new byte[] {0x40, 0x20, (byte)0x80, (byte)0xff}));
		assertEquals(0x01cc02ee, PickleUtils.bytes_to_integer(new byte[] {(byte)0xee, 0x02, (byte)0xcc, 0x01}));
		assertEquals(0xcc01ee02, PickleUtils.bytes_to_integer(new byte[] {0x02, (byte)0xee, 0x01, (byte)0xcc}));
		assertEquals(0xeefffffe, PickleUtils.bytes_to_integer(new byte[] {(byte)0xfe, -1,-1,(byte)0xee}));
		try
		{
			PickleUtils.bytes_to_integer(new byte[] {(byte) 200,50,25,100,1,2,3,4});
			fail("expected PickleException");
		} catch (PickleException x) {}
	}

	@Test
	public void testBytes_to_uint() {
		try {
			PickleUtils.bytes_to_uint(new byte[] {},0);
			fail("expected PickleException");
		} catch (PickleException x) {}
		try {
			PickleUtils.bytes_to_uint(new byte[] {0},0);
			fail("expected PickleException");
		} catch (PickleException x) {}
		assertEquals(0x000000000L, PickleUtils.bytes_to_uint(new byte[] {0,0,0,0} ,0));
		assertEquals(0x012345678L, PickleUtils.bytes_to_uint(new byte[] {0x78, 0x56, 0x34, 0x12} ,0));
		assertEquals(0x0ff802040L, PickleUtils.bytes_to_uint(new byte[] {0x40, 0x20, (byte)0x80, (byte)0xff} ,0));
		assertEquals(0x0eefffffeL, PickleUtils.bytes_to_uint(new byte[] {(byte)0xfe, -1,-1,(byte)0xee} ,0));
	}

	@Test
	public void testBytes_to_long() {
		try {
			PickleUtils.bytes_to_long(new byte[] {}, 0);
			fail("expected PickleException");
		} catch (PickleException x) {}
		try {
			PickleUtils.bytes_to_long(new byte[] {0}, 0);
			fail("expected PickleException");
		} catch (PickleException x) {}
		assertEquals(0x00000000L, PickleUtils.bytes_to_long(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00} ,0));
		assertEquals(0x00003412L, PickleUtils.bytes_to_long(new byte[] {0x12, 0x34, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00} ,0));
		assertEquals(0xff000000000000ffL, PickleUtils.bytes_to_long(new byte[] {(byte)0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte)0xff} ,0));
		assertEquals(0L, PickleUtils.bytes_to_long(new byte[] {0,0,0,0,0,0,0,0} ,0));
		assertEquals(0x8877665544332211L, PickleUtils.bytes_to_long(new byte[] {0x11,0x22,0x33,0x44,0x55,0x66,0x77,(byte)0x88} ,0));
		assertEquals(0x1122334455667788L, PickleUtils.bytes_to_long(new byte[] {(byte)0x88,0x77,0x66,0x55,0x44,0x33,0x22,0x11} ,0));
		assertEquals(-1L, PickleUtils.bytes_to_long(new byte[] {(byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff} ,0));
		assertEquals(-2L, PickleUtils.bytes_to_long(new byte[] {(byte)0xfe, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff} ,0));
	}
	
	
	@Test
	public void testInteger_to_bytes()
	{
		assertArrayEquals(new byte[]{0,0,0,0}, PickleUtils.integer_to_bytes(0));
		assertArrayEquals(new byte[]{0x78, 0x56, 0x34, 0x12}, PickleUtils.integer_to_bytes(0x12345678));
		assertArrayEquals(new byte[]{0x40, 0x20, (byte)0x80, (byte)0xff}, PickleUtils.integer_to_bytes(0xff802040));
		assertArrayEquals(new byte[]{(byte)0xfe, -1,-1,(byte)0xee}, PickleUtils.integer_to_bytes(0xeefffffe));
		assertArrayEquals(new byte[]{(byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff}, PickleUtils.integer_to_bytes(-1));
		assertArrayEquals(new byte[]{(byte)0xee, 0x02, (byte)0xcc, 0x01}, PickleUtils.integer_to_bytes(0x01cc02ee));
		assertArrayEquals(new byte[]{0x02, (byte)0xee, 0x01, (byte)0xcc}, PickleUtils.integer_to_bytes(0xcc01ee02));
	}
	
	@Test
	public void testBytes_to_double() {
		try {
			PickleUtils.bytes_to_double(new byte[] {}, 0);
			fail("expected PickleException");
		} catch (PickleException x) {}
		try {
			PickleUtils.bytes_to_double(new byte[] {0}, 0);
			fail("expected PickleException");
		} catch (PickleException x) {}
		assertTrue(0.0d == PickleUtils.bytes_to_double(new byte[] {0,0,0,0,0,0,0,0}, 0));
		assertTrue(1.0d == PickleUtils.bytes_to_double(new byte[] {0x3f,(byte)0xf0,0,0,0,0,0,0} ,0));
		assertTrue(1.1d == PickleUtils.bytes_to_double(new byte[] {0x3f,(byte)0xf1,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x9a} ,0));
		assertTrue(1234.5678d == PickleUtils.bytes_to_double(new byte[] {0x40,(byte)0x93,0x4a,0x45,0x6d,0x5c,(byte)0xfa,(byte)0xad} ,0));
		assertTrue(2.17e123d == PickleUtils.bytes_to_double(new byte[] {0x59,(byte)0x8a,0x42,(byte)0xd1,(byte)0xce,(byte)0xf5,0x3f,0x46} ,0));
		assertTrue(1.23456789e300d == PickleUtils.bytes_to_double(new byte[] {0x7e,0x3d,0x7e,(byte)0xe8,(byte)0xbc,(byte)0xaf,0x28,0x3a} ,0));
		assertTrue(Double.POSITIVE_INFINITY == PickleUtils.bytes_to_double(new byte[] {0x7f,(byte)0xf0,0,0,0,0,0,0} ,0));
		assertTrue(Double.NEGATIVE_INFINITY == PickleUtils.bytes_to_double(new byte[] {(byte)0xff,(byte)0xf0,0,0,0,0,0,0} ,0));
		try
		{
			PickleUtils.bytes_to_double(new byte[] {(byte) 200,50,25,100}, 0);
			fail("expected PickleException");
		} catch (PickleException x) {}

		// test offset
		assertTrue(1.23456789e300d == PickleUtils.bytes_to_double(new byte[] {0,0,0,0x7e,0x3d,0x7e,(byte)0xe8,(byte)0xbc,(byte)0xaf,0x28,0x3a} ,3));
		assertTrue(1.23456789e300d == PickleUtils.bytes_to_double(new byte[] {0x7e,0x3d,0x7e,(byte)0xe8,(byte)0xbc,(byte)0xaf,0x28,0x3a,0,0,0} ,0));
	}

	@Test
	public void testBytes_to_float() {
		try {
			PickleUtils.bytes_to_float(new byte[] {}, 0);
			fail("expected PickleException");
		} catch (PickleException x) {}
		try {
			PickleUtils.bytes_to_float(new byte[] {0}, 0);
			fail("expected PickleException");
		} catch (PickleException x) {}
		assertTrue(0.0f == PickleUtils.bytes_to_float(new byte[] {0,0,0,0}, 0));
		assertTrue(1.0f == PickleUtils.bytes_to_float(new byte[] {0x3f,(byte)0x80,0,0} ,0));
		assertTrue(1.1f == PickleUtils.bytes_to_float(new byte[] {0x3f,(byte)0x8c,(byte)0xcc,(byte)0xcd} ,0));
		assertTrue(1234.5678f == PickleUtils.bytes_to_float(new byte[] {0x44,(byte)0x9a,0x52,0x2b} ,0));
		assertTrue(Float.POSITIVE_INFINITY == PickleUtils.bytes_to_float(new byte[] {0x7f,(byte)0x80,0,0} ,0));
		assertTrue(Float.NEGATIVE_INFINITY == PickleUtils.bytes_to_float(new byte[] {(byte)0xff,(byte)0x80,0,0} ,0));

		// test offset
		assertTrue(1234.5678f == PickleUtils.bytes_to_float(new byte[] {0,0,0, 0x44,(byte)0x9a,0x52,0x2b} ,3));
		assertTrue(1234.5678f == PickleUtils.bytes_to_float(new byte[] {0x44,(byte)0x9a,0x52,0x2b,0,0,0} ,0));
	}

	@Test
	public void testDouble_to_bytes()
	{
		assertArrayEquals(new byte[]{0,0,0,0,0,0,0,0}, PickleUtils.double_to_bytes(0.0d));
		assertArrayEquals(new byte[]{0x3f,(byte)0xf0,0,0,0,0,0,0}, PickleUtils.double_to_bytes(1.0d));
		assertArrayEquals(new byte[]{0x3f,(byte)0xf1,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x9a}, PickleUtils.double_to_bytes(1.1d));
		assertArrayEquals(new byte[]{0x40,(byte)0x93,0x4a,0x45,0x6d,0x5c,(byte)0xfa,(byte)0xad}, PickleUtils.double_to_bytes(1234.5678d));
		assertArrayEquals(new byte[]{0x59,(byte)0x8a,0x42,(byte)0xd1,(byte)0xce,(byte)0xf5,0x3f,0x46}, PickleUtils.double_to_bytes(2.17e123d));
		assertArrayEquals(new byte[]{0x7e,0x3d,0x7e,(byte)0xe8,(byte)0xbc,(byte)0xaf,0x28,0x3a}, PickleUtils.double_to_bytes(1.23456789e300d));
		assertArrayEquals(new byte[]{0x7f,(byte)0xf8,0,0,0,0,0,0}, PickleUtils.double_to_bytes(Double.NaN));
		assertArrayEquals(new byte[]{0x7f,(byte)0xf0,0,0,0,0,0,0}, PickleUtils.double_to_bytes(Double.POSITIVE_INFINITY));
		assertArrayEquals(new byte[]{(byte)0xff,(byte)0xf0,0,0,0,0,0,0}, PickleUtils.double_to_bytes(Double.NEGATIVE_INFINITY));
	}
	
	@Test
	public void testDecode_long()
	{
		assertEquals(0L, PickleUtils.decode_long(new byte[0]));
		assertEquals(0L, PickleUtils.decode_long(new byte[]{0}));
		assertEquals(1L, PickleUtils.decode_long(new byte[]{1}));
		assertEquals(10L, PickleUtils.decode_long(new byte[]{10}));
		assertEquals(255L, PickleUtils.decode_long(new byte[]{(byte)0xff,0x00}));
		assertEquals(32767L, PickleUtils.decode_long(new byte[]{(byte)0xff,0x7f}));
		assertEquals(-256L, PickleUtils.decode_long(new byte[]{0x00,(byte)0xff}));
		assertEquals(-32768L, PickleUtils.decode_long(new byte[]{0x00,(byte)0x80}));
		assertEquals(-128L, PickleUtils.decode_long(new byte[]{(byte)0x80}));
		assertEquals(127L, PickleUtils.decode_long(new byte[]{0x7f}));
		assertEquals(128L, PickleUtils.decode_long(new byte[]{(byte)0x80, 0x00}));

		assertEquals(128L, PickleUtils.decode_long(new byte[]{(byte)0x80, 0x00}));
		assertEquals(0x78563412L, PickleUtils.decode_long(new byte[]{0x12,0x34,0x56,0x78}));
		assertEquals(0x785634f2L, PickleUtils.decode_long(new byte[]{(byte)0xf2,0x34,0x56,0x78}));
		assertEquals(0x12345678L, PickleUtils.decode_long(new byte[]{0x78,0x56,0x34,0x12}));
		
		assertEquals(-231451016L, PickleUtils.decode_long(new byte[]{0x78,0x56,0x34,(byte)0xf2}));
		assertEquals(0xf2345678L, PickleUtils.decode_long(new byte[]{0x78,0x56,0x34,(byte)0xf2,0x00}));
	}
	
	@Test
	public void testEncode_long()
	{
		assertArrayEquals(new byte[]{42}, PickleUtils.encode_long(BigInteger.valueOf(42)));
		assertArrayEquals(new byte[]{-1}, PickleUtils.encode_long(BigInteger.valueOf(-1)));
		assertArrayEquals(new byte[]{0x00, (byte)0x80}, PickleUtils.encode_long(BigInteger.valueOf(-32768)));
		assertArrayEquals(new byte[]{0x56,0x34,0x12}, PickleUtils.encode_long(BigInteger.valueOf(0x123456)));
		assertArrayEquals(new byte[]{(byte)0x99,(byte)0x88,0x77,0x66,0x55,0x44,0x33,0x22,0x11}, PickleUtils.encode_long(new BigInteger("112233445566778899",16)));
		
		assertArrayEquals(new byte[]{0x78,0x56,0x34,(byte)0xf2}, PickleUtils.encode_long(BigInteger.valueOf(-231451016L)));	
		assertArrayEquals(new byte[]{0x78,0x56,0x34,(byte)0xf2,0x00}, PickleUtils.encode_long(BigInteger.valueOf(0xf2345678L)));
	}
	
	@Test
	public void testDecode_escaped()
	{
		assertEquals("abc", PickleUtils.decode_escaped("abc"));
		assertEquals("a\\c", PickleUtils.decode_escaped("a\\\\c"));
		assertEquals("a\u0042c", PickleUtils.decode_escaped("a\\x42c"));
		assertEquals("a\nc", PickleUtils.decode_escaped("a\\nc"));
		assertEquals("a\tc", PickleUtils.decode_escaped("a\\tc"));
		assertEquals("a\rc", PickleUtils.decode_escaped("a\\rc"));
	}
	
	@Test
	public void testDecode_unicode_escaped()
	{
		assertEquals("abc", PickleUtils.decode_unicode_escaped("abc"));
		assertEquals("a\\c", PickleUtils.decode_unicode_escaped("a\\\\c"));
		assertEquals("a\u0042c", PickleUtils.decode_unicode_escaped("a\\u0042c"));
		assertEquals("a\nc", PickleUtils.decode_unicode_escaped("a\\nc"));
		assertEquals("a\tc", PickleUtils.decode_unicode_escaped("a\\tc"));
		assertEquals("a\rc", PickleUtils.decode_unicode_escaped("a\\rc"));
	}
}
