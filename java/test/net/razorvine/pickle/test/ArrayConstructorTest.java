package net.razorvine.pickle.test;

import static org.junit.Assert.*;
import junit.framework.Assert;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.objects.ArrayConstructor;

import org.junit.Test;

// c, b, B, u, h, H, i, I, l, L, f or d

public class ArrayConstructorTest {

	@Test
	public void testInvalidMachineTypes()
	{
		ArrayConstructor ac=new ArrayConstructor();
		try {
			ac.construct('b', -1, new byte[]{0});
			Assert.fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}

		try {
			ac.construct('b', 0, new byte[]{0});
			Assert.fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}

		try {
			ac.construct('?', 0, new byte[]{0});
			Assert.fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}
		
		try {
			ac.construct('b', 22, new byte[]{0});
			Assert.fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}

		try {
			ac.construct('d', 16, new byte[]{0});
			Assert.fail("expected pickleexception");
		} catch (PickleException x) {
			//ok
		}
	}

	@Test
	public void testChars()
	{
		// c/b/B/u
		fail("not yet implemented");
	}
	
	@Test
	public void testInts()
	{
		// h/H/i/I/l/L
		fail("not yet implemented");
	}
	
	@SuppressWarnings("deprecation")
	@Test
	public void testFloats()
	{
		// f/d
		ArrayConstructor ac=new ArrayConstructor();
		assertTrue(16711938.0f ==
				((float[])ac.construct('f', 14, new byte[]{0x4b,0x7f,0x01,0x02}))[0] );
		assertTrue(Float.POSITIVE_INFINITY ==
				((float[])ac.construct('f', 14, new byte[]{(byte)0x7f,(byte)0x80,0x00,0x00}))[0]);
		assertTrue(Float.NEGATIVE_INFINITY ==
				((float[])ac.construct('f', 14, new byte[]{(byte)0xff,(byte)0x80,0x00,0x00}))[0]);
		assertTrue(-0.0f ==
				((float[])ac.construct('f', 14, new byte[]{(byte)0x80,0x00,0x00,0x00}))[0]);
		
		assertTrue(16711938.0f ==
				((float[])ac.construct('f', 15, new byte[]{0x02,0x01,0x7f,0x4b}))[0]);
		assertTrue(Float.POSITIVE_INFINITY ==
				((float[])ac.construct('f', 15, new byte[]{0x00,0x00,(byte)0x80,(byte)0x7f}))[0]);
		assertTrue(Float.NEGATIVE_INFINITY ==
				((float[])ac.construct('f', 15, new byte[]{0x00,0x00,(byte)0x80,(byte)0xff}))[0]);
		assertTrue(-0.0f ==
				((float[])ac.construct('f', 15, new byte[]{0x00,0x00,0x00,(byte)0x80}))[0]);

		assertTrue(9006104071832581.0d ==
				((double[])ac.construct('d', 16, new byte[]{0x43,0x3f,(byte)0xff,0x01,0x02,0x03,0x04,0x05}))[0]);
		assertTrue(Double.POSITIVE_INFINITY == 
				((double[])ac.construct('d', 16, new byte[]{(byte)0x7f,(byte)0xf0,0x00,0x00,0x00,0x00,0x00,0x00}))[0]);
		assertTrue(Double.NEGATIVE_INFINITY ==
				((double[])ac.construct('d', 16, new byte[]{(byte)0xff,(byte)0xf0,0x00,0x00,0x00,0x00,0x00,0x00}))[0]);
		assertTrue(-0.0d ==
				((double[])ac.construct('d', 16, new byte[]{(byte)0x80,0x00,0x00,0x00,0x00,0x00,0x00,0x00}))[0]);
		
		assertTrue(9006104071832581.0d ==
				((double[])ac.construct('d', 17, new byte[]{0x05,0x04,0x03,0x02,0x01,(byte)0xff,0x3f,0x43}))[0]);
		assertTrue(Double.POSITIVE_INFINITY ==
				((double[])ac.construct('d', 17, new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,(byte)0xf0,(byte)0x7f}))[0]);
		assertTrue(Double.NEGATIVE_INFINITY ==
				((double[])ac.construct('d', 17, new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,(byte)0xf0,(byte)0xff}))[0]);
		assertTrue(-0.0d ==
				((double[])ac.construct('d', 17, new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,(byte)0x80}))[0]);

	
		// check if multiple values in an array work
		assertArrayEquals(new float[] {1.1f, 2.2f}, (float[])  ac.construct('f', 14, new byte[]{0x3f,(byte)0x8c,(byte)0xcc,(byte)0xcd, 0x40,0x0c,(byte)0xcc,(byte)0xcd}) ,0);
		assertArrayEquals(new float[] {1.1f, 2.2f}, (float[])  ac.construct('f', 15, new byte[]{(byte)0xcd,(byte)0xcc,(byte)0x8c,0x3f, (byte)0xcd,(byte)0xcc,0x0c,0x40}) ,0);
		assertArrayEquals(new double[]{1.1d, 2.2d}, (double[]) ac.construct('d', 16, new byte[]{(byte)0x3f,(byte)0xf1,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x9a, (byte)0x40,(byte)0x01,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x9a}) ,0);
		assertArrayEquals(new double[]{1.1d, 2.2d}, (double[]) ac.construct('d', 17, new byte[]{(byte)0x9a,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0xf1,(byte)0x3f, (byte)0x9a,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x99,(byte)0x01,(byte)0x40}) ,0);
	}
}
