package net.razorvine.pickle.test;

import static org.junit.Assert.assertArrayEquals;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNull;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.GregorianCalendar;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Set;

import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.Unpickler;
import net.razorvine.pickle.objects.AnyClassConstructor;
import net.razorvine.pickle.objects.ComplexNumber;
import net.razorvine.pickle.objects.Time;
import net.razorvine.pickle.objects.TimeDelta;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

/**
 * Unit tests for the unpickler.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class UnpicklerTests {

	@Before
	public void setUp() throws Exception {
	}

	@After
	public void tearDown() throws Exception {
	}

	Object U(String strdata) throws PickleException, IOException
	{
		Unpickler u=new Unpickler();
		Object o=u.loads(strdata.getBytes("ISO-8859-15"));
		u.close();
		return o;		
	}
	Object U(byte[] data) throws PickleException, IOException
	{
		Unpickler u=new Unpickler();
		Object o=u.loads(data);
		u.close();
		return o;		
	}
	
	
	@Test
	public void testSinglePrimitives() throws PickleException, IOException {
		// protocol level 1
		assertNull(U("N."));		// none
		assertEquals(123.456d, U("F123.456\n."));	// float
		assertEquals(Boolean.TRUE,U("I01\n."));	// true boolean
		assertEquals(Boolean.FALSE,U("I00\n."));	// false boolean
		assertArrayEquals(new byte[]{97,98,99},(byte[]) U("c__builtin__\nbytes\np0\n((lp1\nL97L\naL98L\naL99L\natp2\nRp3\n.")); // python3 bytes
		assertArrayEquals(new byte[]{97,98,99},(byte[]) U("c__builtin__\nbytes\n(](KaKbKcetR.")); // python3 bytes
		assertArrayEquals(new byte[]{97,98,99,100,101,102}, (byte[]) U("C\u0006abcdef.")); // python3 bytes
		assertEquals(123,U("I123\n."));   // int
		assertEquals(999999999,U("I999999999\n."));   // int
		assertEquals(-999999999,U("I-999999999\n."));   // int
		assertEquals(9999999999l,U("I9999999999\n."));   // int (From 64-bit python)
		assertEquals(-9999999999l,U("I-9999999999\n."));   // int (From 64-bit python)
		assertEquals(19999999999l,U("I19999999999\n."));   // int (From 64-bit python)
		assertEquals(-19999999999l,U("I-19999999999\n."));   // int (From 64-bit python)
		assertEquals(0x45443043,U("JC0DE."));	// 4 byte signed int 0x45443043 (little endian)
		assertEquals(0xeefffffe,U("J\u00fe\u00ff\u00ff\u00ee."));	// 4 byte signed int (little endian)
		assertEquals(255,U("K\u00ff."));   // unsigned int
		assertEquals(1234L,U("L1234\n.")); // long (as long)
		assertEquals(12345678987654321L,U("L12345678987654321L\n.")); // long (as long)
		assertEquals(new BigInteger("9999888877776666555544443333222211110000"),U("L9999888877776666555544443333222211110000L\n.")); // long (as bigint)
		assertEquals(12345,U("M90."));    // 2 byte unsigned
		assertEquals(65535,U("M\u00ff\u00ff."));    // 2 byte unsigned
		assertEquals("Foobar",U("S'Foobar'\n."));  // string with quotes
		assertEquals("abc",U("T\u0003\u0000\u0000\u0000abc."));  // counted string
		assertEquals("abc",U("U\u0003abc."));  // short counted string
		assertEquals("unicode",U("Vunicode\n."));
		assertEquals("unicode",U("X\u0007\u0000\u0000\u0000unicode."));
		assertEquals(new HashMap<Object,Object>(),U("}."));
		assertEquals(new ArrayList<Object>(),U("]."));
		assertArrayEquals(new Object[0], (Object[]) U(")."));
		assertEquals(1234.5678d, U("G@\u0093JEm\\\u00fa\u00ad."));  // 8-byte binary coded float
		// protocol level2
		assertEquals(Boolean.TRUE,U("\u0088."));	// True
		assertEquals(Boolean.FALSE,U("\u0089."));	// False
		assertEquals(12345678987654321L, U("\u008a\u0007\u00b1\u00f4\u0091\u0062\u0054\u00dc\u002b."));
		assertEquals(12345678987654321L, U("\u008b\u0007\u0000\u0000\u0000\u00b1\u00f4\u0091\u0062\u0054\u00dc\u002b."));
		// Protocol 3 (Python 3.x)
		assertArrayEquals(new byte[]{'a','b','c'}, (byte[]) U("B\u0003\u0000\u0000\u0000abc."));
		assertArrayEquals(new byte[]{'a','b','c'}, (byte[]) U("C\u0003abc."));
	}
	
	@Test
	public void testUnicodeStrings() throws PickleException, IOException
	{
		assertEquals("\u00ff", U("S'\\xff'\n."));
		assertEquals("\u20ac", U("V\\u20ac\n."));
		
		assertEquals("euro\u20ac", U("X\u0007\u0000\u0000\u0000euro\u00e2\u0082\u00ac."));   // utf-8 encoded
		
		assertEquals("\u0007\u00db\u007f\u0080",U(new byte[]{'T',0x04,0x00,0x00,0x00,0x07,(byte)0xdb,0x7f,(byte)0x80,'.'}));  // string with non-ascii symbols
		assertEquals("\u0007\u00db\u007f\u0080",U(new byte[]{'U',0x04,0x07,(byte)0xdb,0x7f,(byte)0x80,'.'}));  // string with non-ascii symbols
		assertEquals("\u0007\u00db\u007f\u0080",U(new byte[]{'V',0x07,(byte)0xdb,0x7f,(byte)0x80,'\n','.'}));  // string with non-ascii symbols
	}
	
	@Test
	public void testTuples() throws PickleException, IOException
	{
		assertArrayEquals(new Object[0], (Object[])U(")."));	// ()
		assertArrayEquals(new Object[]{97}, (Object[])U("Ka\u0085.")); // (97,)
		assertArrayEquals(new Object[]{97,98}, (Object[])U("KaKb\u0086.")); // (97,98)
		assertArrayEquals(new Object[]{97,98,99}, (Object[])U("KaKbKc\u0087.")); // (97,98,99)
		assertArrayEquals(new Object[]{97,98,99,100}, (Object[])U("(KaKbKcKdt.")); // (97,98,99,100)
	}

	@Test
	public void testLists() throws PickleException, IOException
	{
		ArrayList<Integer> list=new ArrayList<Integer>(0);
		
		assertEquals(list, U("]."));	// []
		list.add(97);
		assertEquals(list, U("]Kaa."));	// [97]
		assertEquals(list, U("(Kal."));	// [97]
		list.add(98);
		list.add(99);
		assertEquals(list, U("](KaKbKce."));	// [97,98,99]
	}
	
	@Test
	public void testDicts() throws PickleException, IOException
	{
		HashMap<Object,Object> map=new HashMap<Object,Object>();
		HashMap<Object,Object> map2=new HashMap<Object,Object>();
		ArrayList<Object> list=new ArrayList<Object>();
		assertEquals(map, U("}.") );	// {}
		map.put(97, 98);
		map.put(99, 100);
		assertEquals(map, U("}(KaKbKcKdu."));  // {97: 98, 99: 100}
		assertEquals(map, U("(dI97\nI98\nsI99\nI100\ns.")); // {97: 98, 99: 100}
		assertEquals(map, U("(I97\nI98\nI99\nI100\nd.")); // {97: 98, 99: 100}
	
		map.clear();
		map.put(1,2);
		map.put(3,4);
		map2.put(5,6);
		map2.put(7,8);
		list.add(map);
		list.add(map2);
		assertEquals(list, U("(lp0\n(dp1\nI1\nI2\nsI3\nI4\nsa(dp2\nI5\nI6\nsI7\nI8\nsa."));  // [{1:2, 3:4}, {5:6, 7:8}]
		assertEquals(list, U("\u0080\u0002]q\u0000(}q\u0001(K\u0001K\u0002K\u0003K\u0004u}q\u0002(K\u0005K\u0006K\u0007K\u0008ue."));  // [{1:2, 3:4}, {5:6, 7:8}]
		
		map.clear();
		map2.clear();
		list.clear();
		
		map.put("abc",null);
		assertEquals(map, U("(dp0\nS'abc'\np1\nNs.")); // {'abc': None}
		assertEquals(map, U("\u0080\u0002}q\u0000U\u0003abcq\u0001Ns.")); // {'abc': None}
		map.put("abc",111);
		assertEquals(map, U("(dp0\nS'abc'\np1\nI111\ns.")); // {'abc': 111}
		assertEquals(map, U("\u0080\u0002}q\u0000U\u0003abcq\u0001Kos.")); // {'abc': 111}
		list.add(111);
		list.add(111);
		map.put("abc", list);
		assertEquals(map, U("(dp0\nS'abc'\np1\n(lp2\nI111\naI111\nas.")); // {'abc': [111,111]}
		assertEquals(map, U("\u0080\u0002}q\u0000U\u0003abcq\u0001]q\u0002(KoKoes.")); // {'abc': 111}
		map.put("abc",map2);
		assertEquals(map, U("(dp0\nS'abc'\np1\n(dp2\ns.")); // {'abc': {} }
		assertEquals(map, U("\u0080\u0002}q\u0000U\u0003abcq\u0001}q\u0002s.")); // {'abc': {} }
		map2.put("def", 111);
		assertEquals(map, U("(dp0\nS'abc'\np1\n(dp2\nS'def'\np3\nI111\nss.")); // {'abc': {'def':111}}
		assertEquals(map, U("\u0080\u0002}q\u0000U\u0003abcq\u0001}q\u0002U\u0003defq\u0003Koss.")); // {'abc': {'def':111}}

		map2.put("def", list);
		assertEquals(map, U("(dp0\nS'abc'\np1\n(dp2\nS'def'\np3\n(lp4\nI111\naI111\nass.")); // {'abc': {'def': [111,111] }}
		assertEquals(map, U("\u0080\u0002}q\u0000U\u0003abcq\u0001}q\u0002U\u0003defq\u0003]q\u0004(KoKoess.")); // {'abc': {'def': [111,111] }}

		
		ArrayList<Object> list2=new ArrayList<Object>();
		list2.add(222);
		list2.add(222);
		map2.put("ghi", list2);
		assertEquals(map, U("(dp0\nS'abc'\np1\n(dp2\nS'ghi'\np3\n(lp4\nI222\naI222\nasS'def'\np5\n(lp6\nI111\naI111\nass.")); // {'abc': {'def': [111,111], ghi: [222,222] }}
		assertEquals(map, U("\u0080\u0002}q\u0000U\u0003abcq\u0001}q\u0002(U\u0003ghiq\u0003]q\u0004(K\u00deK\u00deeU\u0003defq\u0005]q\u0006(KoKoeus.")); // {'abc': {'def': [111,111], ghi: [222,222] }}

		map2.clear();
		map2.put("def", list);
		map2.put("abc", list);
		assertEquals(map, U("(dp0\nS'abc'\np1\n(dp2\ng1\n(lp3\nI111\naI111\nasS'def'\np4\ng3\nss.")); // {'abc': {'def': [111,111], abc: [111,111] }}
		assertEquals(map, U("\u0080\u0002}q\u0000U\u0003abcq\u0001}q\u0002(h\u0001]q\u0003(KoKoeU\u0003defq\u0004h\u0003us.")); // {'abc': {'def': [111,111], abc: [111,111] }}
	}
	
	@Test
	public void testComplex() throws PickleException, IOException
	{
		ComplexNumber c=new ComplexNumber(2.0, 4.0);
		assertEquals(c, U("c__builtin__\ncomplex\np0\n(F2.0\nF4.0\ntp1\nRp2\n."));
		assertEquals(c, U("c__builtin__\ncomplex\nq\u0000G@\u0000\u0000\u0000\u0000\u0000\u0000\u0000G@\u0010\u0000\u0000\u0000\u0000\u0000\u0000\u0086q\u0001Rq\u0002."));
	}
	
	@Test
	public void testDecimal() throws PickleException, IOException
	{
		assertEquals(new BigDecimal("12345.6789"), U("cdecimal\nDecimal\np0\n(S'12345.6789'\np1\ntp2\nRp3\n."));
		assertEquals(new BigDecimal("12345.6789"), U("\u0080\u0002cdecimal\nDecimal\nU\n12345.6789\u0085R."));
	}

	@Test
	public void testDateTime() throws PickleException, IOException
	{
		Calendar c=new GregorianCalendar(2011, Calendar.DECEMBER, 31);
		Calendar pc=(Calendar) U("cdatetime\ndate\nU\u0004\u0007\u00db\u000c\u001f\u0085R.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());

		Time time=(Time) U("cdatetime\ntime\nU\u0006\u000e!;\u0006\u00f5@\u0085R.");
		assertEquals(14, time.hours);
		assertEquals(33, time.minutes);
		assertEquals(59, time.seconds);
		assertEquals(456000, time.microseconds);
		assertEquals("Time: 14 hours, 33 minutes, 59 seconds, 456000 microseconds", time.toString());
		
		c=new GregorianCalendar(2011, Calendar.DECEMBER, 31);
		c.set(Calendar.HOUR_OF_DAY, 14);
		c.set(Calendar.MINUTE, 33);
		c.set(Calendar.SECOND, 59);
		c.set(Calendar.MILLISECOND,456);
		pc=(Calendar) U("cdatetime\ndatetime\nU\n\u0007\u00db\u000c\u001f\u000e!;\u0006\u00f5@\u0085R.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());
		pc=(Calendar) U("cdatetime\ndatetime\np0\n(S'\\x07\\xdb\\x0c\\x1f\\x0e!;\\x06\\xf5@'\np1\ntp2\nRp3\n.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());
		
		TimeDelta td=(TimeDelta) U("cdatetime\ntimedelta\nM\u00d9\u0002M\u00d5\u00d2M\u00c8\u0001\u0087R.");
		assertEquals(729,td.days);
		assertEquals(53973, td.seconds);
		assertEquals(456, td.microseconds);
		assertEquals("Timedelta: 729 days, 53973 seconds, 456 microseconds (total: 63039573.000456 seconds)", td.toString());
	}
	
	@Test
	public void testDateTimePython3() throws PickleException, IOException
	{
		Calendar c=new GregorianCalendar(2011, Calendar.DECEMBER, 31);
		Calendar pc=(Calendar) U("cdatetime\ndate\nC\u0004\u0007\u00db\u000c\u001f\u0085R.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());

		c=new GregorianCalendar(1970,Calendar.JANUARY,1);
		c.set(Calendar.HOUR_OF_DAY, 14);
		c.set(Calendar.MINUTE, 33);
		c.set(Calendar.SECOND, 59);
		c.set(Calendar.MILLISECOND,456);
		Time time=(Time) U("cdatetime\ntime\nC\u0006\u000e!;\u0006\u00f5@\u0085R.");
		assertEquals(14,time.hours);
		assertEquals(33,time.minutes);
		assertEquals(59,time.seconds);
		assertEquals(456000,time.microseconds);
		assertEquals("Time: 14 hours, 33 minutes, 59 seconds, 456000 microseconds", time.toString());
		
		c=new GregorianCalendar(2011, Calendar.DECEMBER, 31);
		c.set(Calendar.HOUR_OF_DAY, 14);
		c.set(Calendar.MINUTE, 33);
		c.set(Calendar.SECOND, 59);
		c.set(Calendar.MILLISECOND,456);
		pc=(Calendar) U("cdatetime\ndatetime\nC\n\u0007\u00db\u000c\u001f\u000e!;\u0006\u00f5@\u0085R.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());
	}
	
	@Test
	public void testBytes() throws PickleException, IOException
	{
		byte[] bytes=new byte[]{1,2,127,(byte)128,(byte)255};
		assertArrayEquals(bytes, (byte[])U("\u0080\u0003C\u0005\u0001\u0002\u007f\u0080\u00ffq\u0000."));
		assertArrayEquals(bytes, (byte[])U("c__builtin__\nbytearray\np0\n(V\u0001\u0002\u007f\u0080\u00ff\np1\nS'latin-1'\np2\ntp3\nRp4\n."));
		bytes=new byte[]{1,2,3};
		assertArrayEquals(bytes, (byte[])U("\u0080\u0002c__builtin__\nbytearray\nX\u0003\u0000\u0000\u0000\u0001\u0002\u0003X\u000b\u0000\u0000\u0000iso-8859-15\u0086R."));
	}
	
	@Test
	public void testArray() throws PickleException, IOException
	{
		// c=char
		char[] testc=new char[]{'a','b','c'};
		char[] arrayc=(char[]) U("carray\narray\np0\n(S'c'\np1\n(lp2\nS'a'\np3\naS'b'\np4\nag1\natp5\nRp6\n.");
		assertArrayEquals(testc,arrayc);
		testc=new char[]{'x','y','z'};
		arrayc=(char[])U("carray\narray\nU\u0001c](U\u0001xU\u0001yU\u0001ze\u0086R.");
		assertArrayEquals(testc,arrayc);
		
		// u=unicode char
		testc=new char[]{'a','b','c'};
		arrayc=(char[]) U("carray\narray\np0\n(S'u'\np1\n(lp2\nVa\np3\naVb\np4\naVc\np5\natp6\nRp7\n.");
		assertArrayEquals(testc,arrayc);
		// b=signed integer 1
		byte[] testb=new byte[]{1,2,-1,-2};
		byte[] arrayb=(byte[]) U("carray\narray\np0\n(S'b'\np1\n(lp2\nI1\naI2\naI-1\naI-2\natp3\nRp4\n.");
		assertArrayEquals(testb,arrayb);

		// B=unsigned integer 1
		short[] tests=new short[]{1,2,128,255};
		short[] arrays=(short[]) U("carray\narray\np0\n(S'B'\np1\n(lp2\nI1\naI2\naI128\naI255\natp3\nRp4\n.");
		assertArrayEquals(tests,arrays);

		// h=signed integer 2
		tests=new short[]{1,2,128,255,32700,-32700};
		arrays=(short[]) U("carray\narray\np0\n(S'h'\np1\n(lp2\nI1\naI2\naI128\naI255\naI32700\naI-32700\natp3\nRp4\n.");
		assertArrayEquals(tests,arrays);
		
		// H=unsigned integer 2
		int[] testi=new int[]{1,2,40000,65535};
		int[] arrayi=(int[]) U("carray\narray\np0\n(S'H'\np1\n(lp2\nI1\naI2\naI40000\naI65535\natp3\nRp4\n.");
		assertArrayEquals(testi,arrayi);

		// i=signed integer 2
		testi=new int[]{1,2,999999999,-999999999};
		arrayi=(int[]) U("carray\narray\np0\n(S'i'\np1\n(lp2\nI1\naI2\naI999999999\naI-999999999\natp3\nRp4\n.");
		assertArrayEquals(testi,arrayi);
		
		// l=signed integer 4
		testi=new int[]{1,2,999999999,-999999999};
		arrayi=(int[]) U("carray\narray\np0\n(S'l'\np1\n(lp2\nI1\naI2\naI999999999\naI-999999999\natp3\nRp4\n.");
		assertArrayEquals(testi,arrayi);

		// L=unsigned integer 4
		long[] testl=new long[]{1,2,999999999l};
		long[] arrayl=(long[]) U("carray\narray\np0\n(S'L'\np1\n(lp2\nL1L\naL2L\naL999999999L\natp3\nRp4\n.");
		assertArrayEquals(testl,arrayl);

		// I=unsigned integer 2
		testl=new long[]{1,2,999999999};
		arrayl=(long[]) U("carray\narray\np0\n(S'I'\np1\n(lp2\nL1L\naL2L\naL999999999L\natp3\nRp4\n.");
		assertArrayEquals(testl,arrayl);

		// f=float 4
		float[] testf=new float[]{-4.4f, 4.4f};
		float[] arrayf=(float[]) U("carray\narray\np0\n(S'f'\np1\n(lp2\nF-4.400000095367432\naF4.400000095367432\natp3\nRp4\n.");
		assertArrayEquals(testf,arrayf, 0.0f);

		// d=float 8
		double[] testd=new double[]{-4.4f, 4.4f};
		double[] arrayd=(double[]) U("carray\narray\np0\n(S'd'\np1\n(lp2\nF-4.4\naF4.4\natp3\nRp4\n.");
		assertArrayEquals(testd,arrayd,0.000001);
	}
	
	@Test(expected=net.razorvine.pickle.PickleException.class)
	public void testArrayPython3() throws IOException, PickleException {
		// python 3 array reconstructor, not yet supported
		int[] testi=new int[]{1,2,3};
		int[] arrayi=(int[])U("\u0080\u0003carray\n_array_reconstructor\nq\u0000(carray\narray\nq\u0001X\u0001\u0000\u0000\u0000iq\u0002K\u0008C\u000c\u000f'\u0000\u0000\u00b8\"\u0000\u0000a\u001e\u0000\u0000q\u0003tq\u0004Rq\u0005.");
		assertArrayEquals(testi, arrayi);
	}
	
	@Test
	@SuppressWarnings("unchecked")
	public void testSet() throws PickleException, IOException
	{
		Set<Object> set=new HashSet<Object>();
		set.add(1);
		set.add(2);
		set.add("abc");
		
		assertEquals(set,(Set<Object>)U("c__builtin__\nset\np0\n((lp1\nI1\naI2\naS'abc'\np2\natp3\nRp4\n."));
	}
	
	@SuppressWarnings("unchecked")
	@Test
	public void testMemoing() throws PickleException, IOException
	{
	    ArrayList<Object> list=new ArrayList<Object>();
        list.add("irmen");	 
        list.add("irmen");	 
        list.add("irmen");	 
		assertEquals(list, U("]q\u0000(U\u0005irmenq\u0001h\u0001h\u0001e."));
		
		ArrayList<Object>a=new ArrayList<Object>();
		a.add(111);
		ArrayList<Object>b=new ArrayList<Object>();
		b.add(222);
		ArrayList<Object>c=new ArrayList<Object>();
		c.add(333);
		
		Object[] array=new Object[] {a,b,c,a,b,c};
		assertArrayEquals(array, (Object[]) U("((lp0\nI111\na(lp1\nI222\na(lp2\nI333\nag0\ng1\ng2\ntp3\n."));
		
		list.clear();
		list.add("a");
		list.add("b");
		list.add(list);//recursive
		a=(ArrayList<Object>) U("(lp0\nS'a'\np1\naS'b'\np2\nag0\na.");
		assertEquals("[a, b, (this Collection)]", a.toString());
		a=(ArrayList<Object>) U("\u0080\u0002]q\u0000(U\u0001aq\u0001U\u0001bq\u0002h\u0000e.");
		assertEquals("[a, b, (this Collection)]", a.toString());
		a=(ArrayList<Object>)U("]q\u0000(]q\u0001(K\u0001K\u0002K\u0003e]q\u0002(h\u0001h\u0001ee.");
		assertEquals("[[1, 2, 3], [[1, 2, 3], [1, 2, 3]]]", a.toString());
	}
	
	@Test
	public void testBinint2WithObject() throws PickleException, IOException
	{
		Unpickler u=new Unpickler();
		Unpickler.registerConstructor("Pyro4.core", "URI", new AnyClassConstructor(String.class));
		byte[] data="\u0080\u0002cPyro4.core\nURI\n)\u0081M\u0082#.".getBytes("iso-8859-15");
		int result=(Integer) u.loads(data);
		assertEquals(9090,result);
	}
	
	public static void main(String[] args) throws PickleException, IOException
	{
		//Unpickler u=new Unpickler();
	}
	
}
