package net.razorvine.pickle.test;

import static org.junit.Assert.*;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.GregorianCalendar;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.TimeZone;

import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.PickleUtils;
import net.razorvine.pickle.Pickler;
import net.razorvine.pickle.Unpickler;
import net.razorvine.pickle.objects.ComplexNumber;
import net.razorvine.pickle.objects.Time;
import net.razorvine.pickle.objects.TimeDelta;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import org.junit.Ignore;


/**
 * Unit tests for the unpickler.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class UnpicklerTest {

	@Before
	public void setUp() throws Exception {
	}

	@After
	public void tearDown() throws Exception {
	}

	Object U(String strdata) throws PickleException, IOException
	{
		return U(PickleUtils.str2bytes(strdata));	
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
		// protocol level 2
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
		
		c=new GregorianCalendar(2014, Calendar.JULY, 8);
		c.set(Calendar.HOUR_OF_DAY, 10);
		c.set(Calendar.MINUTE, 10);
		c.set(Calendar.SECOND, 1);
		c.set(Calendar.MILLISECOND, 1);
		pc=(Calendar) U("cdatetime\ndatetime\np0\n(S\'\\x07\\xde\\x07\\x08\\n\\n\\x01\\x00\\x03\\xe8\'\np1\ntp2\nRp3\n."); // has escaped newline characters encoding decimal 10
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());

		TimeDelta td=(TimeDelta) U("cdatetime\ntimedelta\nM\u00d9\u0002M\u00d5\u00d2JU\u00f8\u0006\u0000\u0087R.");
		assertEquals(729,td.days);
		assertEquals(53973, td.seconds);
		assertEquals(456789, td.microseconds);
		assertEquals("Timedelta: 729 days, 53973 seconds, 456789 microseconds (total: 63039573.456789 seconds)", td.toString());
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
	public void testDateTimeStringEscaping() throws PickleException, IOException
	{
		Calendar c=new GregorianCalendar(2011, Calendar.OCTOBER, 10);
		c.set(Calendar.HOUR_OF_DAY, 9);
		c.set(Calendar.MINUTE, 13);
		c.set(Calendar.SECOND, 10);
		c.set(Calendar.MILLISECOND,	10);
		Pickler p = new Pickler();
		byte[] pickle = p.dumps(c);
		Unpickler u = new Unpickler();
		Calendar c2 = (Calendar) u.loads(pickle);
		assertEquals(c, c2);
		
		c=new GregorianCalendar(2011, Calendar.OCTOBER, 9);
		c.set(Calendar.HOUR_OF_DAY, 13);
		c.set(Calendar.MINUTE, 10);
		c.set(Calendar.SECOND, 9);
		c.set(Calendar.MILLISECOND,	10);
		c2 = (Calendar) U("\u0080\u0002cdatetime\ndatetime\nq\u0000U\n\u0007\u00db\n\t\r\n\t\u0000'\u0010q\u0001\u0085q\u0002Rq\u0003.");	// protocol 2
		assertEquals(c, c2);
		c2 = (Calendar) U("cdatetime\ndatetime\nq\u0000(U\n\u0007\u00db\n\t\r\n\t\u0000'\u0010q\u0001tq\u0002Rq\u0003.");	// protocol 1
		assertEquals(c, c2);
		c2 = (Calendar) U("cdatetime\ndatetime\np0\n(S\"\\x07\\xdb\\n\\t\\r\\n\\t\\x00\'\\x10\"\np1\ntp2\nRp3\n.");	// protocol 0
		assertEquals(c, c2);
	}
	
	@Test
	public void testDateTimeWithTimezones() throws PickleException, IOException
	{
		TimeZone tz = TimeZone.getTimeZone("UTC");
		Calendar c=new GregorianCalendar(2014, Calendar.JULY, 8);
		c.set(Calendar.HOUR_OF_DAY, 10);
		c.set(Calendar.MINUTE, 10);
		c.set(Calendar.SECOND, 0);
		c.set(Calendar.MILLISECOND, 0);
		c.setTimeZone(tz);

		// pytz timezones that are utc
		Calendar pc=(Calendar) U("cdatetime\ndatetime\np0\n(S'\\x07\\xde\\x07\\x08\\n\\n\\x00\\x00\\x00\\x00'\np1\ncpytz\n_UTC\np2\n(tRp3\ntp4\nRp5\n.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());
		pc=(Calendar) U("\u0080\u0002cdatetime\ndatetime\nq\u0000U\n\u0007\u00de\u0007\u0008\n\n\u0000\u0000\u0000\u0000q\u0001cpytz\n_UTC\nq\u0002)Rq\u0003\u0086q\u0004Rq\u0005.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());

		// dateutil tzutcs
		pc=(Calendar) U("cdatetime\ndatetime\np0\n(S\'\\x07\\xde\\x07\\x08\\n\\n\\x00\\x00\\x00\\x00\'\np1\nccopy_reg\n_reconstructor\np2\n(cdateutil.tz\ntzutc\np3\ncdatetime\ntzinfo\np4\ng4\n(tRp5\ntp6\nRp7\ntp8\nRp9\n.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());
		pc=(Calendar) U("\u0080\u0002cdatetime\ndatetime\nq\u0000U\n\u0007\u00de\u0007\u0008\n\n\u0000\u0000\u0000\u0000q\u0001cdateutil.tz\ntzutc\nq\u0002)\u0081q\u0003}q\u0004b\u0086q\u0005Rq\u0006.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());

		tz = TimeZone.getTimeZone("America/New_York");
		c=new GregorianCalendar(2014, Calendar.JULY, 8);
		c.set(Calendar.HOUR_OF_DAY, 10);
		c.set(Calendar.MINUTE, 10);
		c.set(Calendar.SECOND, 0);
		c.set(Calendar.MILLISECOND, 0);
		c.setTimeZone(tz);

		// pytz timezones with DST support
		pc=(Calendar) U("cdatetime\ndatetime\np0\n(S\'\\x07\\xde\\x07\\x08\\n\\n\\x00\\x00\\x00\\x00\'\np1\ncpytz\n_p\np2\n(VAmerica/New_York\np3\nI-17760\nI0\nVLMT\np4\ntp5\nRp6\ntp7\nRp8\n.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());
		pc=(Calendar) U("\u0080\u0002cdatetime\ndatetime\nq\u0000U\n\u0007\u00de\u0007\u0008\n\n\u0000\u0000\u0000\u0000q\u0001cpytz\n_p\nq\u0002(X\u0010\u0000\u0000\u0000America/New_Yorkq\u0003J\u00a0\u00ba\u00ff\u00ffK\u0000X\u0003\u0000\u0000\u0000LMTq\u0004tq\u0005Rq\u0006\u0086q\u0007Rq\u0008.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());

		tz = TimeZone.getTimeZone("MST");
		c=new GregorianCalendar(2014, Calendar.JULY, 8);
		c.set(Calendar.HOUR_OF_DAY, 10);
		c.set(Calendar.MINUTE, 10);
		c.set(Calendar.SECOND, 0);
		c.set(Calendar.MILLISECOND, 0);
		c.setTimeZone(tz);

		// pytz timezones with static offsets
		pc=(Calendar) U("cdatetime\ndatetime\np0\n(S\'\\x07\\xde\\x07\\x08\\n\\n\\x00\\x00\\x00\\x00\'\np1\ncpytz\n_p\np2\n(S\'MST\'\np3\ntp4\nRp5\ntp6\nRp7\n.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());
		pc=(Calendar) U("\u0080\u0002cdatetime\ndatetime\nq\u0000U\n\u0007\u00de\u0007\u0008\n\n\u0000\u0000\u0000\u0000q\u0001cpytz\n_p\nq\u0002U\u0003MSTq\u0003\u0085q\u0004Rq\u0005\u0086q\u0006Rq\u0007.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());
		
		// dateutil gettz timezone
		tz = TimeZone.getTimeZone("Europe/Amsterdam");
		c=new GregorianCalendar(2015, Calendar.APRIL, 9);
		c.set(Calendar.HOUR_OF_DAY, 19);
		c.set(Calendar.MINUTE, 6);
		c.set(Calendar.SECOND, 26);
		c.set(Calendar.MILLISECOND, 472);
		c.setTimeZone(tz);

		pc=(Calendar) U("cdatetime\ndatetime\np0\n(S\'\\x07\\xdf\\x04\\t\\x13\\x06\\x1a\\x073\\xcc\'\np1\ncdateutil.tz\ntzfile\np2\n(S\'/usr/share/zoneinfo/Europe/Amsterdam\'\np3\ntp4\nRp5\ntp6\nRp7\n.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());
		pc=(Calendar) U("\u0080\u0002cdatetime\ndatetime\nq\u0000U\n\u0007\u00df\u0004\t\u0013\u0006\u001a\u00073\u00ccq\u0001cdateutil.tz\ntzfile\nq\u0002U$/usr/share/zoneinfo/Europe/Amsterdamq\u0003\u0085q\u0004Rq\u0005\u0086q\u0006Rq\u0007.");
		assertEquals(c.getTimeInMillis(), pc.getTimeInMillis());

		// multi word dateutil gettz timezone
		pc=(Calendar) U("cdatetime\ndatetime\np0\n(S\'\\x07\\xdf\\x04\\n\\x0f\\x17\\x13\\x07\\xdf\\x91\'\np1\ncdateutil.tz\ntzfile\np2\n(S\'/usr/share/zoneinfo/America/New_York\'\np3\ntp4\nRp5\ntp6\nRp7\n.");
		assertEquals("America/New_York", pc.getTimeZone().getID());

		// dateutil gettz timezone without full path to the zoneinfo file
		pc=(Calendar) U("\u0080\u0002cdatetime\ndatetime\nU\n\u0007\u00df\u0004\u0006\u000e*.\u0000a\u00a8cdateutil.zoneinfo\ngettz\nU\u0010Europe/Amsterdam\u0085R\u0086R.");
		assertEquals("Europe/Amsterdam", pc.getTimeZone().getID());
	}
	
	@Test
	public void testCodecBytes() throws IOException
	{
		// this is a protocol 2 pickle that contains the way python3 encodes bytes
		byte[] data = (byte[]) U("\u0080\u0002c_codecs\nencode\nX\u0004\u0000\u0000\u0000testX\u0006\u0000\u0000\u0000latin1\u0086R.");
		assertArrayEquals("test".getBytes(), data);
	}
	
	@Test
	public void testBytesAndByteArray() throws IOException
	{
		byte[] bytes=new byte[]{1,2,127,(byte)128,(byte)255};
		assertArrayEquals(bytes, (byte[])U("\u0080\u0003C\u0005\u0001\u0002\u007f\u0080\u00ffq\u0000."));
		assertArrayEquals(bytes, (byte[])U("c__builtin__\nbytearray\np0\n(V\u0001\u0002\u007f\u0080\u00ff\np1\nS'latin-1'\np2\ntp3\nRp4\n."));
		bytes=new byte[]{1,2,3};
		assertArrayEquals(bytes, (byte[])U("\u0080\u0002c__builtin__\nbytearray\nX\u0003\u0000\u0000\u0000\u0001\u0002\u0003X\n\u0000\u0000\u0000iso-8859-1\u0086R."));
		
		// the following bytecode pickle has been created in python by pickling a bytearray
		// from 0x00 to 0xff with protocol level 0.
		byte[] p0=new byte[] {
			0x63,0x5f,0x5f,0x62,0x75,0x69,0x6c,0x74,0x69,0x6e,0x5f,0x5f,0xa,0x62,0x79,0x74,0x65,0x61,0x72,0x72,0x61,0x79,0xa,0x70,0x30,0xa,0x28,0x56,
			0x0,0x1,0x2,0x3,0x4,0x5,0x6,0x7,0x8,0x9,0x5c,0x75,0x30,0x30,0x30,0x61,0xb,0xc,0xd,0xe,0xf,0x10,0x11,0x12,0x13,0x14,0x15,0x16,0x17,0x18,
			0x19,0x1a,0x1b,0x1c,0x1d,0x1e,0x1f,0x20,0x21,0x22,0x23,0x24,0x25,0x26,0x27,0x28,0x29,0x2a,0x2b,0x2c,0x2d,0x2e,0x2f,0x30,0x31,0x32,
			0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x3a,0x3b,0x3c,0x3d,0x3e,0x3f,0x40,0x41,0x42,0x43,0x44,0x45,0x46,0x47,0x48,0x49,0x4a,0x4b,0x4c,
			0x4d,0x4e,0x4f,0x50,0x51,0x52,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5a,0x5b,0x5c,0x75,0x30,0x30,0x35,0x63,0x5d,0x5e,0x5f,0x60,0x61,
			0x62,0x63,0x64,0x65,0x66,0x67,0x68,0x69,0x6a,0x6b,0x6c,0x6d,0x6e,0x6f,0x70,0x71,0x72,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7a,0x7b,
			0x7c,0x7d,0x7e,0x7f,(byte)0x80,(byte)0x81,(byte)0x82,(byte)0x83,(byte)0x84,(byte)0x85,(byte)0x86,(byte)0x87,(byte)0x88,(byte)0x89,
			(byte)0x8a,(byte)0x8b,(byte)0x8c,(byte)0x8d,(byte)0x8e,(byte)0x8f,(byte)0x90,(byte)0x91,(byte)0x92,(byte)0x93,(byte)0x94,(byte)0x95,
			(byte)0x96,(byte)0x97,(byte)0x98,(byte)0x99,(byte)0x9a,(byte)0x9b,(byte)0x9c,(byte)0x9d,(byte)0x9e,(byte)0x9f,(byte)0xa0,(byte)0xa1,
			(byte)0xa2,(byte)0xa3,(byte)0xa4,(byte)0xa5,(byte)0xa6,(byte)0xa7,(byte)0xa8,(byte)0xa9,(byte)0xaa,(byte)0xab,(byte)0xac,(byte)0xad,
			(byte)0xae,(byte)0xaf,(byte)0xb0,(byte)0xb1,(byte)0xb2,(byte)0xb3,(byte)0xb4,(byte)0xb5,(byte)0xb6,(byte)0xb7,(byte)0xb8,(byte)0xb9,
			(byte)0xba,(byte)0xbb,(byte)0xbc,(byte)0xbd,(byte)0xbe,(byte)0xbf,(byte)0xc0,(byte)0xc1,(byte)0xc2,(byte)0xc3,(byte)0xc4,(byte)0xc5,
			(byte)0xc6,(byte)0xc7,(byte)0xc8,(byte)0xc9,(byte)0xca,(byte)0xcb,(byte)0xcc,(byte)0xcd,(byte)0xce,(byte)0xcf,(byte)0xd0,(byte)0xd1,
			(byte)0xd2,(byte)0xd3,(byte)0xd4,(byte)0xd5,(byte)0xd6,(byte)0xd7,(byte)0xd8,(byte)0xd9,(byte)0xda,(byte)0xdb,(byte)0xdc,(byte)0xdd,
			(byte)0xde,(byte)0xdf,(byte)0xe0,(byte)0xe1,(byte)0xe2,(byte)0xe3,(byte)0xe4,(byte)0xe5,(byte)0xe6,(byte)0xe7,(byte)0xe8,(byte)0xe9,
			(byte)0xea,(byte)0xeb,(byte)0xec,(byte)0xed,(byte)0xee,(byte)0xef,(byte)0xf0,(byte)0xf1,(byte)0xf2,(byte)0xf3,(byte)0xf4,(byte)0xf5,
			(byte)0xf6,(byte)0xf7,(byte)0xf8,(byte)0xf9,(byte)0xfa,(byte)0xfb,(byte)0xfc,(byte)0xfd,(byte)0xfe,(byte)0xff,
			0xa,0x70,0x31,0xa,0x53,0x27,0x6c,0x61,0x74,0x69,0x6e,0x2d,0x31,0x27,0xa,0x70,0x32,0xa,0x74,0x70,0x33,0xa,0x52,0x70,0x34,0xa,0x2e
		};
		
		bytes=new byte[256];
		for(int i=0; i<256; ++i)
			bytes[i]=(byte)i;
		assertArrayEquals(bytes, (byte[])U(p0));

		// the following bytecode pickle has been created in python by pickling a bytearray
		// from 0x00 to 0xff with protocol level 2.
		byte[] p2=new byte[] {
			(byte)0x80,0x2,0x63,0x5f,0x5f,0x62,0x75,0x69,0x6c,0x74,0x69,0x6e,0x5f,0x5f,0xa,0x62,0x79,0x74,0x65,0x61,0x72,0x72,0x61,
			0x79,0xa,0x71,0x0,0x58,(byte)0x80,0x1,0x0,0x0,0x0,0x1,0x2,0x3,0x4,0x5,0x6,0x7,0x8,0x9,0xa,0xb,0xc,0xd,0xe,0xf,0x10,0x11,
			0x12,0x13,0x14,0x15,0x16,0x17,0x18,0x19,0x1a,0x1b,0x1c,0x1d,0x1e,0x1f,0x20,0x21,0x22,0x23,0x24,0x25,0x26,0x27,0x28,0x29,
			0x2a,0x2b,0x2c,0x2d,0x2e,0x2f,0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x3a,0x3b,0x3c,0x3d,0x3e,0x3f,0x40,0x41,
			0x42,0x43,0x44,0x45,0x46,0x47,0x48,0x49,0x4a,0x4b,0x4c,0x4d,0x4e,0x4f,0x50,0x51,0x52,0x53,0x54,0x55,0x56,0x57,0x58,0x59,
			0x5a,0x5b,0x5c,0x5d,0x5e,0x5f,0x60,0x61,0x62,0x63,0x64,0x65,0x66,0x67,0x68,0x69,0x6a,0x6b,0x6c,0x6d,0x6e,0x6f,0x70,0x71,
			0x72,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7a,0x7b,0x7c,0x7d,0x7e,0x7f,(byte)0xc2,(byte)0x80,(byte)0xc2,(byte)0x81,
			(byte)0xc2,(byte)0x82,(byte)0xc2,(byte)0x83,(byte)0xc2,(byte)0x84,(byte)0xc2,(byte)0x85,(byte)0xc2,(byte)0x86,(byte)0xc2,
			(byte)0x87,(byte)0xc2,(byte)0x88,(byte)0xc2,(byte)0x89,(byte)0xc2,(byte)0x8a,(byte)0xc2,(byte)0x8b,(byte)0xc2,(byte)0x8c,
			(byte)0xc2,(byte)0x8d,(byte)0xc2,(byte)0x8e,(byte)0xc2,(byte)0x8f,(byte)0xc2,(byte)0x90,(byte)0xc2,(byte)0x91,(byte)0xc2,
			(byte)0x92,(byte)0xc2,(byte)0x93,(byte)0xc2,(byte)0x94,(byte)0xc2,(byte)0x95,(byte)0xc2,(byte)0x96,(byte)0xc2,(byte)0x97,
			(byte)0xc2,(byte)0x98,(byte)0xc2,(byte)0x99,(byte)0xc2,(byte)0x9a,(byte)0xc2,(byte)0x9b,(byte)0xc2,(byte)0x9c,(byte)0xc2,
			(byte)0x9d,(byte)0xc2,(byte)0x9e,(byte)0xc2,(byte)0x9f,(byte)0xc2,(byte)0xa0,(byte)0xc2,(byte)0xa1,(byte)0xc2,(byte)0xa2,
			(byte)0xc2,(byte)0xa3,(byte)0xc2,(byte)0xa4,(byte)0xc2,(byte)0xa5,(byte)0xc2,(byte)0xa6,(byte)0xc2,(byte)0xa7,(byte)0xc2,
			(byte)0xa8,(byte)0xc2,(byte)0xa9,(byte)0xc2,(byte)0xaa,(byte)0xc2,(byte)0xab,(byte)0xc2,(byte)0xac,(byte)0xc2,(byte)0xad,
			(byte)0xc2,(byte)0xae,(byte)0xc2,(byte)0xaf,(byte)0xc2,(byte)0xb0,(byte)0xc2,(byte)0xb1,(byte)0xc2,(byte)0xb2,(byte)0xc2,
			(byte)0xb3,(byte)0xc2,(byte)0xb4,(byte)0xc2,(byte)0xb5,(byte)0xc2,(byte)0xb6,(byte)0xc2,(byte)0xb7,(byte)0xc2,(byte)0xb8,
			(byte)0xc2,(byte)0xb9,(byte)0xc2,(byte)0xba,(byte)0xc2,(byte)0xbb,(byte)0xc2,(byte)0xbc,(byte)0xc2,(byte)0xbd,(byte)0xc2,
			(byte)0xbe,(byte)0xc2,(byte)0xbf,(byte)0xc3,(byte)0x80,(byte)0xc3,(byte)0x81,(byte)0xc3,(byte)0x82,(byte)0xc3,(byte)0x83,
			(byte)0xc3,(byte)0x84,(byte)0xc3,(byte)0x85,(byte)0xc3,(byte)0x86,(byte)0xc3,(byte)0x87,(byte)0xc3,(byte)0x88,(byte)0xc3,
			(byte)0x89,(byte)0xc3,(byte)0x8a,(byte)0xc3,(byte)0x8b,(byte)0xc3,(byte)0x8c,(byte)0xc3,(byte)0x8d,(byte)0xc3,(byte)0x8e,
			(byte)0xc3,(byte)0x8f,(byte)0xc3,(byte)0x90,(byte)0xc3,(byte)0x91,(byte)0xc3,(byte)0x92,(byte)0xc3,(byte)0x93,(byte)0xc3,
			(byte)0x94,(byte)0xc3,(byte)0x95,(byte)0xc3,(byte)0x96,(byte)0xc3,(byte)0x97,(byte)0xc3,(byte)0x98,(byte)0xc3,(byte)0x99,
			(byte)0xc3,(byte)0x9a,(byte)0xc3,(byte)0x9b,(byte)0xc3,(byte)0x9c,(byte)0xc3,(byte)0x9d,(byte)0xc3,(byte)0x9e,(byte)0xc3,
			(byte)0x9f,(byte)0xc3,(byte)0xa0,(byte)0xc3,(byte)0xa1,(byte)0xc3,(byte)0xa2,(byte)0xc3,(byte)0xa3,(byte)0xc3,(byte)0xa4,
			(byte)0xc3,(byte)0xa5,(byte)0xc3,(byte)0xa6,(byte)0xc3,(byte)0xa7,(byte)0xc3,(byte)0xa8,(byte)0xc3,(byte)0xa9,(byte)0xc3,
			(byte)0xaa,(byte)0xc3,(byte)0xab,(byte)0xc3,(byte)0xac,(byte)0xc3,(byte)0xad,(byte)0xc3,(byte)0xae,(byte)0xc3,(byte)0xaf,
			(byte)0xc3,(byte)0xb0,(byte)0xc3,(byte)0xb1,(byte)0xc3,(byte)0xb2,(byte)0xc3,(byte)0xb3,(byte)0xc3,(byte)0xb4,(byte)0xc3,
			(byte)0xb5,(byte)0xc3,(byte)0xb6,(byte)0xc3,(byte)0xb7,(byte)0xc3,(byte)0xb8,(byte)0xc3,(byte)0xb9,(byte)0xc3,(byte)0xba,
			(byte)0xc3,(byte)0xbb,(byte)0xc3,(byte)0xbc,(byte)0xc3,(byte)0xbd,(byte)0xc3,(byte)0xbe,(byte)0xc3,(byte)0xbf,0x71,0x1,
			0x55,0x7,0x6c,0x61,0x74,0x69,0x6e,0x2d,0x31,0x71,0x2,(byte)0x86,0x71,0x3,0x52,0x71,0x4,0x2e
		};
		assertArrayEquals(bytes, (byte[])U(p2));
		
		// the following is a python3 pickle of a small bytearray. It constructs a bytearray using [SHORT_]BINBYTES instead of a list.
		byte[] p3 = new byte[] {
				(byte)0x80, 0x04, (byte)0x95, 0x23, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte)0x8c, 0x08, 0x62, 0x75, 0x69,
				0x6c, 0x74, 0x69, 0x6e, 0x73, (byte)0x8c, 0x09, 0x62, 0x79, 0x74, 0x65, 0x61, 0x72, 0x72, 0x61, 0x79,
				(byte)0x93, 0x43, 0x08, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, (byte)0x85, 0x52, 0x2e
		};
		assertArrayEquals(new byte[]{'A','B','C','D','E','F','G','H'}, (byte[])U(p3));
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
		double[] testd=new double[]{-4.4d, 4.4d};
		double[] arrayd=(double[]) U("carray\narray\np0\n(S'd'\np1\n(lp2\nF-4.4\naF4.4\natp3\nRp4\n.");
		assertArrayEquals(testd,arrayd,0.000001);
	}
	
	@Test
	public void testArrayPython3() throws IOException, PickleException {
		// python 3 array reconstructor
		short[] testi=new short[]{1,2,3};
		short[] arrayi=(short[])U("\u0080\u0003carray\n_array_reconstructor\nq\u0000(carray\narray\nq\u0001X\u0001\u0000\u0000\u0000hq\u0002K\u0004C\u0006\u0001\u0000\u0002\u0000\u0003\u0000q\u0003tq\u0004Rq\u0005.");
							  
		assertArrayEquals(testi, arrayi);
	}
	
	@Test(expected=PickleException.class)
	public void testArrayPython26NotSupported() throws PickleException, IOException {
		U("carray\narray\n(S'h'\nS'\\x01\\x00\\x02\\x00\\x03\\x00'\ntR.");
		fail("should crash with pickle exception because not supported");
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
		byte[] data=PickleUtils.str2bytes("\u0080\u0002cIgnore.ignore\nignore\n)\u0081M\u0082#.");
		int result=(Integer) u.loads(data);
		assertEquals(9090,result);
	}
	
    @Test
    @Ignore("performancetest")
    public void testUnpicklingPerformance() throws PickleException, IOException {
        Pickler pickler = new Pickler();

        List<String> myList = new ArrayList<String>();
        for (int i = 0; i < 10; i++) {
            myList.add(String.valueOf(i));
        }

        byte[] bytes = pickler.dumps(myList);

        Unpickler unpickler = new Unpickler();

        long start = System.currentTimeMillis();
        for (int i = 0; i < 1000000; i++) {
            unpickler.loads(bytes);
        }

        System.out.println(System.currentTimeMillis() - start);
    }
}
