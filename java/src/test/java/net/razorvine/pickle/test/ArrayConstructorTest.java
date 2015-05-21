package net.razorvine.pickle.test;

import static org.junit.Assert.*;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.objects.ArrayConstructor;

import org.junit.Test;

public class ArrayConstructorTest {

	@Test
	public void testInvalidMachineTypes()
	{
		ArrayConstructor ac=new ArrayConstructor();
		try {
			ac.construct('b', -1, new byte[]{0});
			fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}

		try {
			ac.construct('b', 0, new byte[]{0});
			fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}

		try {
			ac.construct('?', 0, new byte[]{0});
			fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}
		
		try {
			ac.construct('b', 22, new byte[]{0});
			fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}

		try {
			ac.construct('d', 16, new byte[]{0});
			fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}
	}

	@Test
	public void testChars()
	{
		ArrayConstructor ac=new ArrayConstructor();
		char EURO=(char)0x20ac;
		
		// c/u
		assertArrayEquals(new char[]{'A',EURO}, (char[])ac.construct('c', 18, new byte[]{65,0,(byte)0xac,0x20}));
		assertArrayEquals(new char[]{'A',EURO}, (char[])ac.construct('u', 18, new byte[]{65,0,(byte)0xac,0x20}));
		assertArrayEquals(new char[]{'A',EURO}, (char[])ac.construct('c', 19, new byte[]{0,65,0x20,(byte)0xac}));
		assertArrayEquals(new char[]{'A',EURO}, (char[])ac.construct('u', 19, new byte[]{0,65,0x20,(byte)0xac}));
		assertArrayEquals(new char[]{'A',EURO}, (char[])ac.construct('c', 20, new byte[]{65,0,0,0,(byte)0xac,0x20,0,0}));
		assertArrayEquals(new char[]{'A',EURO}, (char[])ac.construct('u', 20, new byte[]{65,0,0,0,(byte)0xac,0x20,0,0}));
		assertArrayEquals(new char[]{'A',EURO}, (char[])ac.construct('c', 21, new byte[]{0,0,0,65,0,0,0x20,(byte)0xac}));
		assertArrayEquals(new char[]{'A',EURO}, (char[])ac.construct('u', 21, new byte[]{0,0,0,65,0,0,0x20,(byte)0xac}));
		try {
			ac.construct('u', 20, new byte[]{65,0,1,0});	// out of bounds codepoints
			fail("expected error");
		} catch (PickleException x) {
			// ok
		}
		
		// b/B
		assertArrayEquals(new byte[]{1,2,3,4,-1,-2,-3,-4}, (byte[])ac.construct('b', 1, new byte[]{1,2,3,4,(byte)0xff,(byte)0xfe,(byte)0xfd,(byte)0xfc}));
		assertArrayEquals(new short[]{1,2,3,4,0xff,0xfe,0xfd,0xfc}, (short[])ac.construct('B', 0, new byte[]{1,2,3,4,(byte)0xff,(byte)0xfe,(byte)0xfd,(byte)0xfc}));
	}
	
	@Test
	public void testInts()
	{
		ArrayConstructor ac=new ArrayConstructor();

		//h
		assertEquals((short)0x80ff, ((short[])ac.construct('h', 5, new byte[]{(byte)0x80,(byte)0xff}))[0]);
		assertEquals((short)0x7fff, ((short[])ac.construct('h', 5, new byte[]{(byte)0x7f,(byte)0xff}))[0]);
		assertEquals((short)0xffff, ((short[])ac.construct('h', 5, new byte[]{(byte)0xff,(byte)0xff}))[0]);
		assertEquals((short)0xffff, ((short[])ac.construct('h', 4, new byte[]{(byte)0xff,(byte)0xff}))[0]);
		assertArrayEquals(new short[]{0x1234,0x5678}, (short[])ac.construct('h', 5, new byte[]{0x12,0x34,0x56,0x78}));
		assertArrayEquals(new short[]{0x3412,0x7856}, (short[])ac.construct('h', 4, new byte[]{0x12,0x34,0x56,0x78}));

		//H
		assertEquals((int)0x80ff, ((int[])ac.construct('H', 3, new byte[]{(byte)0x80,(byte)0xff}))[0]);
		assertEquals((int)0x7fff, ((int[])ac.construct('H', 3, new byte[]{(byte)0x7f,(byte)0xff}))[0]);
		assertEquals((int)0xffff, ((int[])ac.construct('H', 3, new byte[]{(byte)0xff,(byte)0xff}))[0]);
		assertEquals((int)0xffff, ((int[])ac.construct('H', 2, new byte[]{(byte)0xff,(byte)0xff}))[0]);
		assertArrayEquals(new int[]{0x1234,0x5678}, (int[])ac.construct('H', 3, new byte[]{0x12,0x34,0x56,0x78}));
		assertArrayEquals(new int[]{0x3412,0x7856}, (int[])ac.construct('H', 2, new byte[]{0x12,0x34,0x56,0x78}));

		//i
		assertEquals((int)0x800000ff, ((int[])ac.construct('i', 9, new byte[]{(byte)0x80,0x00,0x00,(byte)0xff}))[0]);
		assertEquals((int)0x7f0000ff, ((int[])ac.construct('i', 9, new byte[]{(byte)0x7f,0x00,0x00,(byte)0xff}))[0]);
		assertEquals((int)0xf00000f1, ((int[])ac.construct('i', 9, new byte[]{(byte)0xf0,0x00,0x00,(byte)0xf1}))[0]);
		assertEquals((int)-2, ((int[])ac.construct('i', 8, new byte[]{(byte)0xfe,(byte)0xff,(byte)0xff,(byte)0xff}))[0]);
		assertArrayEquals(new int[]{0x11223344,0x55667788}, (int[])ac.construct('i', 9, new byte[]{0x11,0x22,0x33,0x44,0x55,0x66,0x77,(byte)0x88}));
		assertArrayEquals(new int[]{0x44332211,0x88776655}, (int[])ac.construct('i', 8, new byte[]{0x11,0x22,0x33,0x44,0x55,0x66,0x77,(byte)0x88}));

		//l-4bytes
		assertEquals(0x800000ff, ((int[])ac.construct('l', 9, new byte[]{(byte)0x80,0x00,0x00,(byte)0xff}))[0]);
		assertEquals(0x7f0000ff, ((int[])ac.construct('l', 9, new byte[]{(byte)0x7f,0x00,0x00,(byte)0xff}))[0]);
		assertEquals(0xf00000f1, ((int[])ac.construct('l', 9, new byte[]{(byte)0xf0,0x00,0x00,(byte)0xf1}))[0]);
		assertEquals(-2, ((int[])ac.construct('l', 8, new byte[]{(byte)0xfe,(byte)0xff,(byte)0xff,(byte)0xff}))[0]);
		assertArrayEquals(new int[]{0x11223344,0x55667788}, (int[])ac.construct('l', 9, new byte[]{0x11,0x22,0x33,0x44,0x55,0x66,0x77,(byte)0x88}));
		assertArrayEquals(new int[]{0x44332211,0x88776655}, (int[])ac.construct('l', 8, new byte[]{0x11,0x22,0x33,0x44,0x55,0x66,0x77,(byte)0x88}));
		//l-8bytes
		assertEquals(0x3400000000000012L, ((long[])ac.construct('l', 12, new byte[]{(byte)0x12,0x00,0x00,0x00,0x00,0x00,0x00,(byte)0x34}))[0]);
		assertEquals(0x3400009009000012L, ((long[])ac.construct('l', 12, new byte[]{(byte)0x12,0x00,0x00,0x09,(byte)0x90,0x00,0x00,(byte)0x34}))[0]);
		assertEquals(0x1200000000000034L, ((long[])ac.construct('l', 13, new byte[]{(byte)0x12,0x00,0x00,0x00,0x00,0x00,0x00,(byte)0x34}))[0]);
		assertEquals(0x1200000990000034L, ((long[])ac.construct('l', 13, new byte[]{(byte)0x12,0x00,0x00,0x09,(byte)0x90,0x00,0x00,(byte)0x34}))[0]);

		assertEquals(0x7fffffffffffffffL, ((long[])ac.construct('l', 13, new byte[]{(byte)0x7f,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff}))[0]);
		assertEquals(0x7fffffffffffffffL, ((long[])ac.construct('l', 12, new byte[]{(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0x7f}))[0]);

		assertEquals(-2L, ((long[])ac.construct('l', 12, new byte[]{(byte)0xfe,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff}))[0]);
		assertEquals(-2L, ((long[])ac.construct('l', 13, new byte[]{(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xfe}))[0]);
		assertArrayEquals(new long[]{1,2}, (long[])ac.construct('l', 13, new byte[]{0,0,0,0,0,0,0,1, 0,0,0,0,0,0,0,2}));
		assertArrayEquals(new long[]{1,2}, (long[])ac.construct('l', 12, new byte[]{1,0,0,0,0,0,0,0, 2,0,0,0,0,0,0,0}));

		//I 
		assertEquals(0x001000000L, ((long[])ac.construct('I', 6, new byte[]{0,0,0,0x01}))[0]);
		assertEquals(0x088000000L, ((long[])ac.construct('I', 6, new byte[]{0,0,0,(byte)0x88}))[0]);
		assertEquals(0x000000001L, ((long[])ac.construct('I', 7, new byte[]{0,0,0,0x01}))[0]);
		assertEquals(0x000000088L, ((long[])ac.construct('I', 7, new byte[]{0,0,0,(byte)0x88}))[0]);
		assertEquals(0x099000088L, ((long[])ac.construct('I', 7, new byte[]{(byte)0x99,0,0,(byte)0x88}))[0]);

		//L-4 bytes
		assertEquals(0x022000011L, ((long[])ac.construct('L', 6, new byte[]{0x11,0,0,0x22}))[0]);
		assertEquals(0x011000022L, ((long[])ac.construct('L', 7, new byte[]{0x11,0,0,0x22}))[0]);
		assertEquals(0x0fffffffeL, ((long[])ac.construct('L', 6, new byte[]{(byte)0xfe,(byte)0xff,(byte)0xff,(byte)0xff}))[0]);
		assertEquals(0x0fffffffeL, ((long[])ac.construct('L', 7, new byte[]{(byte)0xff,(byte)0xff,(byte)0xff,(byte)0xfe}))[0]);

		
		//L-8 bytes is not supported
		try {
			ac.construct('L', 10, new byte[]{0,0,0,0,0,0,0,0});
			fail("expected exception");
		} catch (PickleException x) {
			//ok
		}
		try {
			ac.construct('L', 11, new byte[]{0,0,0,0,0,0,0,0});
			fail("expected exception");
		} catch (PickleException x) {
			//ok
		}
	
	}
	
	@Test
	public void testFloats()
	{
		// f/d
		ArrayConstructor ac=new ArrayConstructor();
		// big endian
		assertTrue(16711938.0f ==
				((float[])ac.construct('f', 15, new byte[]{0x4b,0x7f,0x01,0x02}))[0] );
		assertTrue(Float.POSITIVE_INFINITY ==
				((float[])ac.construct('f', 15, new byte[]{(byte)0x7f,(byte)0x80,0x00,0x00}))[0]);
		assertTrue(Float.NEGATIVE_INFINITY ==
				((float[])ac.construct('f', 15, new byte[]{(byte)0xff,(byte)0x80,0x00,0x00}))[0]);
		assertTrue(-0.0f ==
				((float[])ac.construct('f', 15, new byte[]{(byte)0x80,0x00,0x00,0x00}))[0]);
		// little endian
		assertTrue(16711938.0f ==
				((float[])ac.construct('f', 14, new byte[]{0x02,0x01,0x7f,0x4b}))[0]);
		assertTrue(Float.POSITIVE_INFINITY ==
				((float[])ac.construct('f', 14, new byte[]{0x00,0x00,(byte)0x80,(byte)0x7f}))[0]);
		assertTrue(Float.NEGATIVE_INFINITY ==
				((float[])ac.construct('f', 14, new byte[]{0x00,0x00,(byte)0x80,(byte)0xff}))[0]);
		assertTrue(-0.0f ==
				((float[])ac.construct('f', 14, new byte[]{0x00,0x00,0x00,(byte)0x80}))[0]);
		// big endian
		assertTrue(9006104071832581.0d ==
				((double[])ac.construct('d', 17, new byte[]{0x43,0x3f,(byte)0xff,0x01,0x02,0x03,0x04,0x05}))[0]);
		assertTrue(Double.POSITIVE_INFINITY == 
				((double[])ac.construct('d', 17, new byte[]{(byte)0x7f,(byte)0xf0,0x00,0x00,0x00,0x00,0x00,0x00}))[0]);
		assertTrue(Double.NEGATIVE_INFINITY ==
				((double[])ac.construct('d', 17, new byte[]{(byte)0xff,(byte)0xf0,0x00,0x00,0x00,0x00,0x00,0x00}))[0]);
		assertTrue(-0.0d ==
				((double[])ac.construct('d', 17, new byte[]{(byte)0x80,0x00,0x00,0x00,0x00,0x00,0x00,0x00}))[0]);
		// little endian
		assertTrue(9006104071832581.0d ==
				((double[])ac.construct('d', 16, new byte[]{0x05,0x04,0x03,0x02,0x01,(byte)0xff,0x3f,0x43}))[0]);
		assertTrue(Double.POSITIVE_INFINITY ==
				((double[])ac.construct('d', 16, new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,(byte)0xf0,(byte)0x7f}))[0]);
		assertTrue(Double.NEGATIVE_INFINITY ==
				((double[])ac.construct('d', 16, new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,(byte)0xf0,(byte)0xff}))[0]);
		assertTrue(-0.0d ==
				((double[])ac.construct('d', 16, new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,(byte)0x80}))[0]);

	
		// check if multiple values in an array work
		assertArrayEquals(new float[] {1.1f, 2.2f}, (float[])  ac.construct('f', 15, new byte[]{0x3f,(byte)0x8c,(byte)0xcc,(byte)0xcd, 0x40,0x0c,(byte)0xcc,(byte)0xcd}) ,0);
		assertArrayEquals(new float[] {1.1f, 2.2f}, (float[])  ac.construct('f', 14, new byte[]{(byte)0xcd,(byte)0xcc,(byte)0x8c,0x3f, (byte)0xcd,(byte)0xcc,0x0c,0x40}) ,0);
		assertArrayEquals(new double[]{1.1d, 2.2d}, (double[]) ac.construct('d', 17, new byte[]{(byte)0x3f,(byte)0xf1,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x9a, (byte)0x40,(byte)0x01,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x9a}) ,0);
		assertArrayEquals(new double[]{1.1d, 2.2d}, (double[]) ac.construct('d', 16, new byte[]{(byte)0x9a,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0xf1,(byte)0x3f, (byte)0x9a,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x01,(byte)0x40}) ,0);
	}
}
