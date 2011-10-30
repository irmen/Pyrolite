/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;

using NUnit.Framework;
using Razorvine.Pickle;
using Razorvine.Pickle.Objects;

namespace Pyrolite.Tests.Pickle
{

/// <summary>
/// Unit tests for the unpickler of the special array construction
/// (Python3's array_reconstructor.)
/// </summary>
[TestFixture]
public class ArrayConstructorTest {

    [Test]
	public void testInvalidMachineTypes()
	{
		ArrayConstructor ac=new ArrayConstructor();
		try {
			ac.construct('b', -1, new byte[]{0});
			Assert.Fail("expected pickleexception");
		} catch (PickleException) {
			//ok
		}

		try {
			ac.construct('b', 0, new byte[]{0});
			Assert.Fail("expected pickleexception");
		} catch (PickleException) {
			//ok
		}

		try {
			ac.construct('?', 0, new byte[]{0});
			Assert.Fail("expected pickleexception");
		} catch (PickleException) {
			//ok
		}
		
		try {
			ac.construct('b', 22, new byte[]{0});
			Assert.Fail("expected pickleexception");
		} catch (PickleException) {
			//ok
		}

		try {
			ac.construct('d', 16, new byte[]{0});
			Assert.Fail("expected pickleexception");
		} catch (PickleException) {
			//ok
		}
	}

	[Test]
	public void testChars()
	{
		ArrayConstructor ac=new ArrayConstructor();
		char EURO=(char)0x20ac;
		
		// c/u
		Assert.AreEqual(new char[]{'A',EURO}, (char[])ac.construct('c', 18, new byte[]{65,0,0xac,0x20}));
		Assert.AreEqual(new char[]{'A',EURO}, (char[])ac.construct('u', 18, new byte[]{65,0,0xac,0x20}));
		Assert.AreEqual(new char[]{'A',EURO}, (char[])ac.construct('c', 19, new byte[]{0,65,0x20,0xac}));
		Assert.AreEqual(new char[]{'A',EURO}, (char[])ac.construct('u', 19, new byte[]{0,65,0x20,0xac}));
		Assert.AreEqual(new char[]{'A',EURO}, (char[])ac.construct('c', 20, new byte[]{65,0,0,0,0xac,0x20,0,0}));
		Assert.AreEqual(new char[]{'A',EURO}, (char[])ac.construct('u', 20, new byte[]{65,0,0,0,0xac,0x20,0,0}));
		Assert.AreEqual(new char[]{'A',EURO}, (char[])ac.construct('c', 21, new byte[]{0,0,0,65,0,0,0x20,0xac}));
		Assert.AreEqual(new char[]{'A',EURO}, (char[])ac.construct('u', 21, new byte[]{0,0,0,65,0,0,0x20,0xac}));
		Assert.AreEqual(new char[]{'A',EURO}, (char[])ac.construct('u', 21, new byte[]{1,0,0,65,1,0,0x20,0xac}));
		
		// b/B
		Assert.AreEqual(new byte[]{1,2,3,4,0xff,0xfe,0xfd,0xfc}, (byte[])ac.construct('b', 1, new byte[]{1,2,3,4,0xff,0xfe,0xfd,0xfc}));
		Assert.AreEqual(new short[]{1,2,3,4,0xff,0xfe,0xfd,0xfc}, (short[])ac.construct('B', 0, new byte[]{1,2,3,4,0xff,0xfe,0xfd,0xfc}));
	}
	
	[Test]
	public void testInts()
	{
		ArrayConstructor ac=new ArrayConstructor();

		//h
		Assert.AreEqual((short)-0x7f01, ((short[])ac.construct('h', 5, new byte[]{0x80,0xff}))[0]);
		Assert.AreEqual((short)0x7fff, ((short[])ac.construct('h', 5, new byte[]{0x7f,0xff}))[0]);
		Assert.AreEqual((short)-1, ((short[])ac.construct('h', 5, new byte[]{0xff,0xff}))[0]);
		Assert.AreEqual((short)-1, ((short[])ac.construct('h', 4, new byte[]{0xff,0xff}))[0]);
		Assert.AreEqual(new short[]{0x1234,0x5678}, (short[])ac.construct('h', 5, new byte[]{0x12,0x34,0x56,0x78}));
		Assert.AreEqual(new short[]{0x3412,0x7856}, (short[])ac.construct('h', 4, new byte[]{0x12,0x34,0x56,0x78}));

		//H
		Assert.AreEqual((int)0x80ff, ((int[])ac.construct('H', 3, new byte[]{0x80,0xff}))[0]);
		Assert.AreEqual((int)0x7fff, ((int[])ac.construct('H', 3, new byte[]{0x7f,0xff}))[0]);
		Assert.AreEqual((int)0xffff, ((int[])ac.construct('H', 3, new byte[]{0xff,0xff}))[0]);
		Assert.AreEqual((int)0xffff, ((int[])ac.construct('H', 2, new byte[]{0xff,0xff}))[0]);
		Assert.AreEqual(new int[]{0x1234,0x5678}, (int[])ac.construct('H', 3, new byte[]{0x12,0x34,0x56,0x78}));
		Assert.AreEqual(new int[]{0x3412,0x7856}, (int[])ac.construct('H', 2, new byte[]{0x12,0x34,0x56,0x78}));

		//i
		Assert.AreEqual((int)-0x7fffff01, ((int[])ac.construct('i', 9, new byte[]{0x80,0x00,0x00,0xff}))[0]);
		Assert.AreEqual((int)0x7f0000ff, ((int[])ac.construct('i', 9, new byte[]{0x7f,0x00,0x00,0xff}))[0]);
		Assert.AreEqual((int)-0xfffff0f, ((int[])ac.construct('i', 9, new byte[]{0xf0,0x00,0x00,0xf1}))[0]);
		Assert.AreEqual((int)-2, ((int[])ac.construct('i', 8, new byte[]{0xfe,0xff,0xff,0xff}))[0]);
		Assert.AreEqual(new int[]{0x11223344,0x55667788}, (int[])ac.construct('i', 9, new byte[]{0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88}));
		Assert.AreEqual(new int[]{0x44332211,-0x778899ab}, (int[])ac.construct('i', 8, new byte[]{0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88}));

		//l-4bytes
		Assert.AreEqual(0x800000ff, ((int[])ac.construct('l', 9, new byte[]{0x80,0x00,0x00,0xff}))[0]);
		Assert.AreEqual(0x7f0000ff, ((int[])ac.construct('l', 9, new byte[]{0x7f,0x00,0x00,0xff}))[0]);
		Assert.AreEqual(0xf00000f1, ((int[])ac.construct('l', 9, new byte[]{0xf0,0x00,0x00,0xf1}))[0]);
		Assert.AreEqual(-2, ((int[])ac.construct('l', 8, new byte[]{0xfe,0xff,0xff,0xff}))[0]);
		Assert.AreEqual(new int[]{0x11223344,0x55667788}, (int[])ac.construct('l', 9, new byte[]{0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88}));
		Assert.AreEqual(new int[]{0x44332211,-0x778899ab}, (int[])ac.construct('l', 8, new byte[]{0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88}));
		//l-8bytes
		Assert.AreEqual(0x3400000000000012L, ((long[])ac.construct('l', 12, new byte[]{0x12,0x00,0x00,0x00,0x00,0x00,0x00,0x34}))[0]);
		Assert.AreEqual(0x3400009009000012L, ((long[])ac.construct('l', 12, new byte[]{0x12,0x00,0x00,0x09,0x90,0x00,0x00,0x34}))[0]);
		Assert.AreEqual(0x1200000000000034L, ((long[])ac.construct('l', 13, new byte[]{0x12,0x00,0x00,0x00,0x00,0x00,0x00,0x34}))[0]);
		Assert.AreEqual(0x1200000990000034L, ((long[])ac.construct('l', 13, new byte[]{0x12,0x00,0x00,0x09,0x90,0x00,0x00,0x34}))[0]);

		Assert.AreEqual(0x7fffffffffffffffL, ((long[])ac.construct('l', 13, new byte[]{0x7f,0xff,0xff,0xff,0xff,0xff,0xff,0xff}))[0]);
		Assert.AreEqual(0x7fffffffffffffffL, ((long[])ac.construct('l', 12, new byte[]{0xff,0xff,0xff,0xff,0xff,0xff,0xff,0x7f}))[0]);

		Assert.AreEqual(-2L, ((long[])ac.construct('l', 12, new byte[]{0xfe,0xff,0xff,0xff,0xff,0xff,0xff,0xff}))[0]);
		Assert.AreEqual(-2L, ((long[])ac.construct('l', 13, new byte[]{0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xfe}))[0]);
		Assert.AreEqual(new long[]{1,2}, (long[])ac.construct('l', 13, new byte[]{0,0,0,0,0,0,0,1, 0,0,0,0,0,0,0,2}));
		Assert.AreEqual(new long[]{1,2}, (long[])ac.construct('l', 12, new byte[]{1,0,0,0,0,0,0,0, 2,0,0,0,0,0,0,0}));

		//I 
		Assert.AreEqual(0x001000000u, ((long[])ac.construct('I', 6, new byte[]{0,0,0,0x01}))[0]);
		Assert.AreEqual(0x088000000u, ((long[])ac.construct('I', 6, new byte[]{0,0,0,0x88}))[0]);
		Assert.AreEqual(0x000000001u, ((long[])ac.construct('I', 7, new byte[]{0,0,0,0x01}))[0]);
		Assert.AreEqual(0x000000088u, ((long[])ac.construct('I', 7, new byte[]{0,0,0,0x88}))[0]);
		Assert.AreEqual(0x099000088u, ((long[])ac.construct('I', 7, new byte[]{0x99,0,0,0x88}))[0]);

		//L
    	ac.construct('L', 6, new byte[]{0,0,0,0x01});
    	Assert.Fail("L not implemented");
	}
	
	[Test]
	public void testFloats()
	{
		// f/d
		ArrayConstructor ac=new ArrayConstructor();
		Assert.AreEqual(16711938.0f,
				((float[])ac.construct('f', 14, new byte[]{0x4b,0x7f,0x01,0x02}))[0] );
		Assert.AreEqual(float.PositiveInfinity,
				((float[])ac.construct('f', 14, new byte[]{0x7f,0x80,0x00,0x00}))[0]);
		Assert.AreEqual(float.NegativeInfinity,
				((float[])ac.construct('f', 14, new byte[]{0xff,0x80,0x00,0x00}))[0]);
		Assert.AreEqual(-0.0f,
				((float[])ac.construct('f', 14, new byte[]{0x80,0x00,0x00,0x00}))[0]);
		
		Assert.AreEqual(16711938.0f,
				((float[])ac.construct('f', 15, new byte[]{0x02,0x01,0x7f,0x4b}))[0]);
		Assert.AreEqual(float.PositiveInfinity,
				((float[])ac.construct('f', 15, new byte[]{0x00,0x00,0x80,0x7f}))[0]);
		Assert.AreEqual(float.NegativeInfinity,
				((float[])ac.construct('f', 15, new byte[]{0x00,0x00,0x80,0xff}))[0]);
		Assert.AreEqual(-0.0f,
				((float[])ac.construct('f', 15, new byte[]{0x00,0x00,0x00,0x80}))[0]);

		Assert.AreEqual(9006104071832581.0d,
				((double[])ac.construct('d', 16, new byte[]{0x43,0x3f,0xff,0x01,0x02,0x03,0x04,0x05}))[0]);
		Assert.AreEqual(double.PositiveInfinity,
				((double[])ac.construct('d', 16, new byte[]{0x7f,0xf0,0x00,0x00,0x00,0x00,0x00,0x00}))[0]);
		Assert.AreEqual(double.NegativeInfinity,
				((double[])ac.construct('d', 16, new byte[]{0xff,0xf0,0x00,0x00,0x00,0x00,0x00,0x00}))[0]);
		Assert.AreEqual(-0.0d,
				((double[])ac.construct('d', 16, new byte[]{0x80,0x00,0x00,0x00,0x00,0x00,0x00,0x00}))[0]);
		
		Assert.AreEqual(9006104071832581.0d,
				((double[])ac.construct('d', 17, new byte[]{0x05,0x04,0x03,0x02,0x01,0xff,0x3f,0x43}))[0]);
		Assert.AreEqual(double.PositiveInfinity,
				((double[])ac.construct('d', 17, new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0xf0,0x7f}))[0]);
		Assert.AreEqual(double.NegativeInfinity,
				((double[])ac.construct('d', 17, new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0xf0,0xff}))[0]);
		Assert.AreEqual(-0.0d,
				((double[])ac.construct('d', 17, new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x80}))[0]);

	
		// check if multiple values in an array work
		Assert.AreEqual(new float[] {1.1f, 2.2f}, (float[])  ac.construct('f', 14, new byte[]{0x3f,0x8c,0xcc,0xcd, 0x40,0x0c,0xcc,0xcd}));
		Assert.AreEqual(new float[] {1.1f, 2.2f}, (float[])  ac.construct('f', 15, new byte[]{0xcd,0xcc,0x8c,0x3f, 0xcd,0xcc,0x0c,0x40}));
		Assert.AreEqual(new double[]{1.1d, 2.2d}, (double[]) ac.construct('d', 16, new byte[]{0x3f,0xf1,0x99,0x99,0x99,0x99,0x99,0x9a, 0x40,0x01,0x99,0x99,0x99,0x99,0x99,0x9a}));
		Assert.AreEqual(new double[]{1.1d, 2.2d}, (double[]) ac.construct('d', 17, new byte[]{0x9a,0x99,0x99,0x99,0x99,0x99,0xf1,0x3f, 0x9a,0x99,0x99,0x99,0x99,0x99,0x01,0x40}));
	}
}

}