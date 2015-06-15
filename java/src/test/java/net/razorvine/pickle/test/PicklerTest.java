package net.razorvine.pickle.test;

import static org.junit.Assert.*;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.io.UnsupportedEncodingException;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.net.URISyntaxException;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.Collection;
import java.util.Date;
import java.util.GregorianCalendar;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.PriorityQueue;
import java.util.Set;
import java.util.Stack;
import java.util.Vector;
import java.util.TimeZone;

import net.razorvine.pickle.IObjectPickler;
import net.razorvine.pickle.Opcodes;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.PickleUtils;
import net.razorvine.pickle.Pickler;
import net.razorvine.pickle.Unpickler;
import net.razorvine.pickle.objects.Time;
import net.razorvine.pickle.objects.TimeDelta;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

/**
 * Unit tests for the pickler.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PicklerTest {

	@Before
	public void setUp() throws Exception {
	}

	@After
	public void tearDown() throws Exception {
	}


	byte[] B(String s) throws IOException {
		try {
			byte[] bytes=PickleUtils.str2bytes(s);
			byte[] result=new byte[bytes.length+3];
			result[0]=(byte)Opcodes.PROTO;
			result[1]=2;	
			result[result.length-1]=(byte)Opcodes.STOP;
			System.arraycopy(bytes,0,result,2,bytes.length);
			return result;
		} catch (UnsupportedEncodingException e) {
			e.printStackTrace();
			return null;
		}
	}
	
	byte[] B(short[] shorts)
	{
		byte[] result=new byte[shorts.length+3];
		result[0]=(byte)Opcodes.PROTO;
		result[1]=2;
		result[result.length-1]=(byte)Opcodes.STOP;
		for(int i=0; i<shorts.length; ++i) {
			result[i+2]=(byte)shorts[i];
		}
		return result;
	}

	String S(byte[] pickled) {
		return PickleUtils.rawStringFromBytes(pickled);
	}

	public enum DayEnum {
	    SUNDAY, MONDAY, TUESDAY, WEDNESDAY, 
	    THURSDAY, FRIDAY, SATURDAY 
	};
	
	@Test
	public void testSinglePrimitives() throws PickleException, IOException {
		// protocol level 2
		Pickler p=new Pickler(false);
		byte[] o=p.dumps(null);	// none
		assertArrayEquals(B("N"), o); 
		o=p.dumps('@');  // char --> string
		assertArrayEquals(B("X\u0001\u0000\u0000\u0000@"), o);
		o=p.dumps(true);	// boolean
		assertArrayEquals(B("\u0088"), o);
		o=p.dumps("hello");      // unicode string
		assertArrayEquals(B("X\u0005\u0000\u0000\u0000hello"), o);
		o=p.dumps("hello\u20ac");      // unicode string with non ascii
		assertArrayEquals(B("X\u0008\u0000\u0000\u0000hello\u00e2\u0082\u00ac"), o);
		o=p.dumps((byte)'@');
		assertArrayEquals(B("K@"), o);
		o=p.dumps((short)0x1234);
		assertArrayEquals(B("M\u0034\u0012"), o);
		o=p.dumps((int)0x12345678);
		assertArrayEquals(B("J\u0078\u0056\u0034\u0012"), o);
		o=p.dumps((long)0x12345678abcdefL);
		assertArrayEquals(B("I5124095577148911\n"), o);
		o=p.dumps(1234.5678d);
		assertArrayEquals(B(new short[] {'G',0x40,0x93,0x4a,0x45,0x6d,0x5c,0xfa,0xad}), o);
		o=p.dumps(1234.5f);
		assertArrayEquals(B(new short[] {'G',0x40,0x93,0x4a,0,0,0,0,0}), o);
		
		DayEnum day=DayEnum.WEDNESDAY;
		o=p.dumps(day);	// enum is returned as just a string representing the value
		assertArrayEquals(B("X\u0009\u0000\u0000\u0000WEDNESDAY"),o);
	}
	
	@Test 
	public void testZeroToTwoFiveSix() throws PickleException, IOException {
		byte[] bytes=new byte[256];
		for(int b=0; b<256; ++b) {
			bytes[b]=(byte)b;
		}
		StringBuilder sb=new StringBuilder();
		for(int i=0; i<256; ++i) {
			sb.append((char)i);
		}
		String str=sb.toString();
		
		Pickler p=new Pickler(false);
		Unpickler u=new Unpickler();
		
		ByteArrayOutputStream bos=new ByteArrayOutputStream(434);
		bos.write(Opcodes.PROTO); bos.write(2);
		bos.write("c__builtin__\nbytearray\n".getBytes());
		bos.write(Opcodes.BINUNICODE);
		bos.write(new byte[] {(byte)0x80,0x01,0x00,0x00});
		byte[] utf8=str.getBytes("UTF-8");
		bos.write(utf8,0,utf8.length);
		bos.write(Opcodes.BINUNICODE);
		bos.write(new byte[] {7,0,0,0});
		bos.write("latin-1".getBytes());
		bos.write(Opcodes.TUPLE2);
		bos.write(Opcodes.REDUCE);
		bos.write(Opcodes.STOP);
		
		byte[] bytesresult=bos.toByteArray();
		byte[] output=p.dumps(bytes);
		assertArrayEquals(bytesresult, output);
		assertArrayEquals(bytes, (byte[])u.loads(output)); 
		
		bos=new ByteArrayOutputStream(434);
		bos.write(Opcodes.PROTO); bos.write(2);
		bos.write(Opcodes.BINUNICODE);
		bos.write(new byte[] {(byte)0x80,0x01,0x00,0x00});
		utf8=str.getBytes("UTF-8");
		bos.write(utf8,0,utf8.length);
		bos.write(Opcodes.STOP);
		bytesresult=bos.toByteArray();

		output=p.dumps(str);
		assertArrayEquals(bytesresult, output);
		assertEquals(str, u.loads(output));
	}
	
	@Test
	public void testBigInts() throws PickleException, IOException
	{
		Pickler p = new Pickler(false);
		byte[] o;
		o=p.dumps(new BigInteger("65"));
		assertArrayEquals(B("\u008a\u0001A"), o);
		o=p.dumps(new BigInteger("1111222233334444555566667777888899990000",16));
		assertArrayEquals(B(new short[]{Opcodes.LONG1,20,0x00,0x00,0x99,0x99,0x88,0x88,0x77,0x77,0x66,0x66,0x55,0x55,0x44,0x44,0x33,0x33,0x22,0x22,0x11,0x11}), o);
		o=p.dumps(new BigDecimal("12345.6789"));
		assertArrayEquals(B("cdecimal\nDecimal\nX\n\u0000\u0000\u000012345.6789\u0085R"), o);
	}
	
	@Test
	public void testArrays() throws PickleException, IOException
	{
		Pickler p = new Pickler(false);
		byte[] o;
		o=p.dumps(new String[] {});
		assertArrayEquals(B(")"), o);
		o=p.dumps(new String[] {"abc"});
		assertArrayEquals(B("X\u0003\u0000\u0000\u0000abc\u0085"), o);
		o=p.dumps(new String[] {"abc","def"});
		assertArrayEquals(B("X\u0003\u0000\u0000\u0000abcX\u0003\u0000\u0000\u0000def\u0086"), o);
		o=p.dumps(new String[] {"abc","def","ghi"});
		assertArrayEquals(B("X\u0003\u0000\u0000\u0000abcX\u0003\u0000\u0000\u0000defX\u0003\u0000\u0000\u0000ghi\u0087"), o);
		o=p.dumps(new String[] {"abc","def","ghi","jkl"});
		assertArrayEquals(B("(X\u0003\u0000\u0000\u0000abcX\u0003\u0000\u0000\u0000defX\u0003\u0000\u0000\u0000ghiX\u0003\u0000\u0000\u0000jklt"), o);

		o=p.dumps(new char[] {65,66,67});
		assertArrayEquals(B("X\u0003\u0000\u0000\u0000ABC"), o);

		o=p.dumps(new boolean[] {true,false,true});
		assertArrayEquals(B("\u0088\u0089\u0088\u0087"), o);

		o=p.dumps(new byte[] {1,2,3});
		assertArrayEquals(B("c__builtin__\nbytearray\nX\u0003\u0000\u0000\u0000\u0001\u0002\u0003X\u0007\u0000\u0000\u0000latin-1\u0086R"), o);

		o=p.dumps(new int[] {1,2,3});
		assertArrayEquals(B("carray\narray\nU\u0001i](K\u0001K\u0002K\u0003e\u0086R"), o);

		o=p.dumps(new double[] {1.1,2.2,3.3});
		assertArrayEquals(B("carray\narray\nU\u0001d](G?\u00f1\u0099\u0099\u0099\u0099\u0099\u009aG@\u0001\u0099\u0099\u0099\u0099\u0099\u009aG@\nffffffe\u0086R"), o);
	}
	
	@Test(expected=PickleException.class)
	public void testRecursiveArray2() throws PickleException, IOException
	{
		Pickler p = new Pickler(false);
		Object[] a = new Object[] { "hello", "placeholder" };
		a[1] = a; // make it recursive
		p.dumps(a);
	}
	
	@Test(expected=PickleException.class)
	public void testRecursiveArray6() throws PickleException, IOException
	{
		Pickler p = new Pickler(false);
		Object[] a = new Object[] { "a","b","c","d","e","f" };
		a[5] = a; // make it recursive
		p.dumps(a);
	}

	@Test
	public void testDates() throws PickleException, IOException
	{
		Pickler p=new Pickler(false);
		Unpickler u=new Unpickler();

		Date date=new GregorianCalendar(2011,Calendar.DECEMBER,31,14,33,59).getTime();
		byte[] o=p.dumps(date);
		Object unpickled=u.loads(o);
		Date unpickledDate=((Calendar)unpickled).getTime();
		assertEquals(date,unpickledDate);
		
		Calendar cal=new GregorianCalendar(2011,Calendar.DECEMBER,31,14,33,59);
		cal.set(Calendar.MILLISECOND, 456);
		o=p.dumps(cal);
		unpickled=u.loads(o);
		assertEquals(cal,(Calendar)unpickled);

		TimeZone tz = TimeZone.getTimeZone("America/New_York");
		cal=new GregorianCalendar(2011,Calendar.DECEMBER,31,14,33,59);
		cal.set(Calendar.MILLISECOND, 456);
		cal.setTimeZone(tz);

		o=p.dumps(cal);
		unpickled=u.loads(o);
		assertEquals(cal, (Calendar) unpickled);
		assertEquals(tz, ((Calendar) unpickled).getTimeZone());

		// example ambiguous DST datetime where the zone is in DST and the time occurs in the transition
		// period, which means two different datetimes could come back
		tz = TimeZone.getTimeZone("America/St_Johns");
		cal=new GregorianCalendar(2009, Calendar.OCTOBER, 31, 23, 30, 59);
		cal.set(Calendar.MILLISECOND, 456);
		cal.setTimeZone(tz);

		o=p.dumps(cal);
		assertArrayEquals(B("coperator\nattrgetter\nX\u0008\u0000\u0000\u0000localize\u0085Rcpytz\ntimezone\n(X\u0010\u0000\u0000\u0000America/St_JohnstR\u0085Rcdatetime\ndatetime\n(M\u00d9\u0007K\nK\u001fK\u0017K\u001eK;J@\u00f5\u0006\u0000tR\u0085R"), o);
		unpickled=u.loads(o);
		assertEquals(cal, (Calendar) unpickled);
		assertEquals(tz, ((Calendar) unpickled).getTimeZone());

		// example UTC timezone which pytz differently to a special constructor
		tz = TimeZone.getTimeZone("UTC");
		cal=new GregorianCalendar(2011,Calendar.DECEMBER,31,14,33,59);
		cal.set(Calendar.MILLISECOND, 456);
		cal.setTimeZone(tz);

		o=p.dumps(cal);
		assertTrue(S(o).contains("pytz\n_UTC\n"));	// ensure pickling uses the _UTC constructor
		unpickled=u.loads(o);
		assertEquals(cal, (Calendar) unpickled);
		assertEquals(tz, ((Calendar) unpickled).getTimeZone());
	}
	
	@Test
	public void testTimes() throws PickleException, IOException
	{
		Pickler p=new Pickler(false);
		Unpickler u=new Unpickler();
		
		Time time=new Time(2,33,59,456789);
		Time time2=new Time(2,33,59,456789);
		Time time3=new Time(2,33,59,45678);
		assertTrue(time.equals(time2));
		assertFalse(time.equals(time3));

		byte[] o=p.dumps(time);
		Object unpickled=u.loads(o);
		assertEquals(time, unpickled);
		
		TimeDelta delta=new TimeDelta(2, 7000, 456789);
		TimeDelta delta2=new TimeDelta(2, 7000, 456789);
		TimeDelta delta3=new TimeDelta(2, 7000, 45678);
		assertTrue(delta.equals(delta2));
		assertFalse(delta.equals(delta3));

		o=p.dumps(delta);
		unpickled=u.loads(o);
		assertArrayEquals(B("cdatetime\ntimedelta\nK\u0002MX\u001bJU\u00f8\u0006\u0000\u0087R"), o);
		assertEquals(delta, unpickled);
	}
	
	@Test
	public void testSqlDateTimes() throws PickleException, IOException
	{
		Pickler p=new Pickler(false);
		Unpickler u=new Unpickler();
		
		// java.sql.Timestamp  maps to python datetime.datetime as usual
		Calendar cal = new GregorianCalendar(2011,Calendar.DECEMBER,31,23,30,59);
		cal.set(Calendar.MILLISECOND, 432);
		java.sql.Timestamp sqltimestamp = new java.sql.Timestamp(cal.getTime().getTime());
		byte[] o=p.dumps(sqltimestamp);
		assertTrue(new String(o).contains("datetime\ndatetime\n"));
		Object unpickled=u.loads(o);
		Calendar unpickledCal=(Calendar)unpickled;
		assertEquals(cal,unpickledCal);

		// java.sql.Date  maps to python datetime.date (only date info)
		cal = new GregorianCalendar(2011,Calendar.DECEMBER,31,23,30,59);
		cal.set(Calendar.MILLISECOND, 432);
		java.sql.Date sqldate=new java.sql.Date(cal.getTimeInMillis());
		o=p.dumps(sqldate);
		assertTrue(new String(o).contains("datetime\ndate\n"));
		unpickled=u.loads(o);
		unpickledCal=(Calendar)unpickled;
		assertNotEquals(cal,unpickledCal);   // it should have lost the time information
		assertEquals(2011, unpickledCal.get(Calendar.YEAR));
		assertEquals(Calendar.DECEMBER, unpickledCal.get(Calendar.MONTH));
		assertEquals(31, unpickledCal.get(Calendar.DAY_OF_MONTH));
		assertEquals(0, unpickledCal.get(Calendar.HOUR_OF_DAY));
		assertEquals(0, unpickledCal.get(Calendar.MINUTE));
		assertEquals(0, unpickledCal.get(Calendar.SECOND));
		assertEquals(0, unpickledCal.get(Calendar.MILLISECOND));

		// java.sql.Time  maps to python datetime.time (only time info)
		// (which if received back, maps back into a net.razorvine.pickle.objects.Time)
		cal = new GregorianCalendar(2011,Calendar.DECEMBER,31,23,30,59);
		cal.set(Calendar.MILLISECOND, 432);
		java.sql.Time sqltime = new java.sql.Time(cal.getTime().getTime());
		o=p.dumps(sqltime);
		assertTrue(new String(o).contains("datetime\ntime\n"));
		unpickled=u.loads(o);
		Time time = (Time) unpickled;
		assertEquals(23, time.hours);
		assertEquals(30, time.minutes);
		assertEquals(59, time.seconds);
		assertEquals(432000, time.microseconds);
	}
	
	@SuppressWarnings("unchecked")
	@Test
	public void testSets() throws PickleException, IOException
	{
		byte[] o;
		Pickler p=new Pickler(false);
		Unpickler up=new Unpickler();

		Set<Integer> intset=new HashSet<Integer>();
		intset.add(1);
		intset.add(2);
		intset.add(3);
		o=p.dumps(intset);
		Set<Object> resultset=(Set<Object>)up.loads(o);
		assertEquals(intset, resultset);

		Set<String> stringset=new HashSet<String>();
		stringset.add("A");
		stringset.add("B");
		stringset.add("C");
		o=p.dumps(stringset);
		resultset=(Set<Object>)up.loads(o);
		assertEquals(stringset, resultset);
	}

	@SuppressWarnings("unchecked")
	@Test
	public void testMappings() throws PickleException, IOException
	{
		byte[] o;
		Pickler p=new Pickler(false);
		Unpickler pu=new Unpickler();
		Map<Integer,Integer> intmap=new HashMap<Integer,Integer>();
		intmap.put(1, 11);
		intmap.put(2, 22);
		intmap.put(3, 33);
		o=p.dumps(intmap);
		Map<?,?> resultmap=(Map<?,?>)pu.loads(o);
		assertEquals(intmap, resultmap);

		Map<String,String> stringmap=new HashMap<String,String>();
		stringmap.put("A", "1");
		stringmap.put("B", "2");
		stringmap.put("C", "3");
		o=p.dumps(stringmap);
		resultmap=(Map<?,?>)pu.loads(o);
		assertEquals(stringmap, resultmap);
		
		@SuppressWarnings("rawtypes")
		java.util.Hashtable table=new java.util.Hashtable();
		table.put(1,11);
		table.put(2,22);
		table.put(3,33);
		o=p.dumps(table);
		resultmap=(Map<?, ?>)pu.loads(o);
		assertEquals(table, resultmap);
	}
	
	@SuppressWarnings("unchecked")
	@Test
	public void testLists() throws PickleException, IOException 
	{
		byte[] o;
		Pickler p=new Pickler(false);
		
		List<Object> list=new LinkedList<Object>();
		list.add(1);
		list.add("abc");
		list.add(null);
		o=p.dumps(list);
		assertArrayEquals(B("](K\u0001X\u0003\u0000\u0000\u0000abcNe"), o);
		
		@SuppressWarnings("rawtypes")
		Vector v=new Vector();
		v.add(1);
		v.add("abc");
		v.add(null);
		o=p.dumps(v);
		assertArrayEquals(B("](K\u0001X\u0003\u0000\u0000\u0000abcNe"), o);
		
		Stack<Integer> stack=new Stack<Integer>();
		stack.push(1);
		stack.push(2);
		stack.push(3);
		o=p.dumps(stack);
		assertArrayEquals(B("](K\u0001K\u0002K\u0003e"), o);
		
		java.util.Queue<Integer> queue=new PriorityQueue<Integer>();
		queue.add(3);
		queue.add(2);
		queue.add(1);
		o=p.dumps(queue);
		assertArrayEquals(B("](K\u0001K\u0003K\u0002e"), o);
 	}
	
	@SuppressWarnings("unchecked")
	@Test
	public void testMemoizationSet() throws PickleException, IOException
	{
		Set<String> set = new HashSet<String>();
		set.add("a");
		Object[] array = new Object[] {set, set};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		assertTrue(new String(data).indexOf(Opcodes.BINPUT)>0);   // check that memoization was done
		
		Unpickler u = new Unpickler();
		Object[] result = (Object[]) u.loads(data);
		assertEquals(2, result.length);
		Object first = result[0];
		Object second = result[1];
		assertTrue(first instanceof HashSet);
		assertTrue(second instanceof HashSet);
		assertSame(first, second);				// both objects should be the same memoized object

		set = (Set<String>) second;
		assertEquals(1, set.size());
		assertTrue(set.contains("a"));
	}
	
	@SuppressWarnings("unchecked")
	@Test
	public void testMemoizationMap() throws PickleException, IOException
	{
		HashMap<String,String> map = new HashMap<String,String>();
		map.put("key", "value");
		Object[] array = new Object[] {map, map};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		assertTrue(new String(data).indexOf(Opcodes.BINPUT)>0);   // check that memoization was done
		
		Unpickler u = new Unpickler();
		Object[] result = (Object[]) u.loads(data);
		assertEquals(2, result.length);
		Object first = result[0];
		Object second = result[1];
		assertTrue(first instanceof HashMap);
		assertTrue(second instanceof HashMap);
		assertSame(first, second);				// both objects should be the same memoized object

		map = (HashMap<String, String>) second;
		assertEquals(1, map.size());
		assertEquals("value", map.get("key"));
	}

	@SuppressWarnings("unchecked")
	@Test
	public void testMemoizationCollection() throws PickleException, IOException
	{
		Collection<String> list = new ArrayList<String>();
		list.add("a");
		Object[] array = new Object[] {list, list};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		assertTrue(new String(data).indexOf(Opcodes.BINPUT)>0);   // check that memoization was done
		
		Unpickler u = new Unpickler();
		Object[] result = (Object[]) u.loads(data);
		assertEquals(2, result.length);
		Object first = result[0];
		Object second = result[1];
		assertTrue(first instanceof ArrayList);
		assertTrue(second instanceof ArrayList);
		assertSame(first, second);				// both objects should be the same memoized object

		list = (Collection<String>) second;
		assertEquals(1, list.size());
		assertTrue(list.contains("a"));
	}
	
	@Test
	public void testMemoizationTimeStuff() throws PickleException, IOException
	{
		TimeDelta delta = new TimeDelta(1, 2, 3);
		Time time = new Time(1,2,3,4);
		Calendar now = Calendar.getInstance();
		Calendar cal = now;
	
		Object[] array = new Object[] {delta, delta, time, time, cal, cal};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		assertTrue(new String(data).indexOf(Opcodes.BINPUT)>0);   // check that memoization was done
		
		Unpickler u = new Unpickler();
		Object[] result = (Object[]) u.loads(data);
		assertEquals(6, result.length);
		assertTrue(result[0] instanceof TimeDelta);
		assertTrue(result[1] instanceof TimeDelta);
		assertTrue(result[2] instanceof Time);
		assertTrue(result[3] instanceof Time);
		assertTrue(result[4] instanceof Calendar);
		assertTrue(result[5] instanceof Calendar);
		assertSame(result[0], result[1]);				// both objects should be the same memoized object
		assertSame(result[2], result[3]);				// both objects should be the same memoized object
		assertSame(result[4], result[5]);				// both objects should be the same memoized object

		delta = (TimeDelta) result[1];
		time = (Time) result[3];
		cal = (Calendar) result[5];
		assertEquals(new TimeDelta(1,2,3), delta);
		assertEquals(new Time(1,2,3,4), time);
		assertEquals(now, cal);
}
	
	@Test
	public void testMemoizationBigNums() throws PickleException, IOException
	{
		BigDecimal bigd = new BigDecimal("12345678901234567890.99887766");
		BigInteger bigi = new BigInteger("12345678901234567890");
		
		Object[] array = new Object[] {bigd, bigd, bigi, bigi};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		assertTrue(new String(data).indexOf(Opcodes.BINPUT)>0);   // check that memoization was done
		
		Unpickler u = new Unpickler();
		Object[] result = (Object[]) u.loads(data);
		assertEquals(4, result.length);
		assertTrue(result[0] instanceof BigDecimal);
		assertTrue(result[1] instanceof BigDecimal);
		assertTrue(result[2] instanceof BigInteger);
		assertTrue(result[3] instanceof BigInteger);
		assertSame(result[0], result[1]);				// both objects should be the same memoized object
		assertSame(result[2], result[3]);				// both objects should be the same memoized object

		bigd = (BigDecimal) result[1];
		bigi = (BigInteger) result[3];
		assertEquals(new BigDecimal("12345678901234567890.99887766"), bigd);
		assertEquals(new BigInteger("12345678901234567890"), bigi);
}

	@Test
	public void testMemoizationString() throws PickleException, IOException
	{
		String str = "a";
		Object[] array = new Object[] {str, str};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		assertTrue(new String(data).indexOf(Opcodes.BINPUT)>0);   // check that memoization was done
		
		Unpickler u = new Unpickler();
		Object[] result = (Object[]) u.loads(data);
		assertEquals(2, result.length);
		Object first = result[0];
		Object second = result[1];
		assertTrue(first instanceof String);
		assertTrue(second instanceof String);
		assertSame(first, second);				// both objects should be the same memoized object
		
		str = (String) second;
		assertEquals("a", str);
	}
	
	@Test
	public void testMemoizationArray() throws PickleException, IOException
	{
		int[] arr = new int[] { 1, 2, 3};
		Object array = new Object[] {arr, arr};
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		assertTrue(new String(data).indexOf(Opcodes.BINPUT)>0);   // check that memoization was done
		
		Unpickler u = new Unpickler();
		Object[] result = (Object[]) u.loads(data);
		assertEquals(2, result.length);
		Object first = result[0];
		Object second = result[1];
		assertTrue(first instanceof int[]);
		assertTrue(second instanceof int[]);
		assertSame(first, second);				// both objects should be the same memoized object
		
		arr = (int[]) second;
		assertEquals(3, arr.length);
		assertArrayEquals(new int[] {1, 2, 3}, arr)	;
	}
	
	@SuppressWarnings("unchecked")
	@Test
	public void testMemoizationList() throws PickleException, IOException  
	{
		byte[] o;
		Pickler p=new Pickler();
		
		String reused = "reused";
		String another = "another";
		ArrayList<Object> list=new ArrayList<Object>();
		ArrayList<Object> sublist = new ArrayList<Object>();
		sublist.add(reused);
		sublist.add(reused);
		sublist.add(another);
		list.add(reused);
		list.add(reused);
		list.add(another);
		list.add(sublist);
		o=p.dumps(list);
		assertEquals("\u0080\u0002]q\u0000(X\u0006\u0000\u0000\u0000reusedq\u0001h\u0001X\u0007\u0000\u0000\u0000anotherq\u0002]q\u0003(h\u0001h\u0001h\u0002ee.", S(o));
		
		Unpickler u = new Unpickler();
		ArrayList<Object> data = (ArrayList<Object>) u.loads(o);
		assertEquals(4, data.size());
		String s1 = (String) data.get(0);
		String s2 = (String) data.get(1);
		String s3 = (String) data.get(2);
		data = (ArrayList<Object>) data.get(3);
		String s4 = (String) data.get(0);
		String s5 = (String) data.get(1);
		String s6 = (String) data.get(2);
		assertEquals("reused", s1);
		assertEquals("another", s3);
		assertSame(s1, s2);
		assertSame(s3, s6);
		assertSame(s1, s4);
		assertSame(s1, s5);
	}
		
	@SuppressWarnings("unused")
	@Test(expected=StackOverflowError.class)
	public void testMemoizationRecursiveNoMemo() throws PickleException, IOException  
	{
		byte[] o;
		Pickler p=new Pickler(false);
		
		String reused = "reused";
		ArrayList<Object> list=new ArrayList<Object>();
		list.add(reused);
		list.add(reused);
		list.add(list);
		o=p.dumps(list);
	}

	@SuppressWarnings({"unchecked", "serial"})
	@Test
	public void testMemoizationRecursiveMemo() throws PickleException, IOException  
	{
		// we have to override the hashCode() method because otherwise
		// the default implementation will crash in a recursive call
		class RecursiveList extends ArrayList<Object> {
			@Override
			public int hashCode()
			{
				return 21751*this.size();
			}
		}
		class RecursiveHashMap extends HashMap<String, Object> {
			@Override
			public int hashCode()
			{
				return 19937*this.size();
			}
		}
		
		byte[] o;
		Pickler p=new Pickler();
		Unpickler u = new Unpickler();
		
		// self-referencing list
		String reused = "reused";
		RecursiveList list=new RecursiveList();
		list.add(reused);
		list.add(reused);
		list.add(list);
		o=p.dumps(list);
		assertEquals("\u0080\u0002]q\u0000(X\u0006\u0000\u0000\u0000reusedq\u0001h\u0001h\u0000e.", S(o));

		ArrayList<Object> data = (ArrayList<Object>) u.loads(o);
		assertEquals(3, data.size());
		String s1 = (String) data.get(0);
		String s2 = (String) data.get(1);
		ArrayList<Object> data2 = (ArrayList<Object>) data.get(2);
		assertEquals("reused", s1);
		assertSame(s1, s2);
		assertSame(data, data2);
		assertSame(data.get(0), data2.get(0));
		
		// self-referencing hashtable
		RecursiveHashMap h = new RecursiveHashMap();
		h.put("myself", h);
		o=p.dumps(h);
		assertEquals("\u0080\u0002}q\u0000(X\u0006\u0000\u0000\u0000myselfq\u0001h\u0000u.", S(o));
		HashMap<String, Object> h2 = (HashMap<String,Object>) u.loads(o);
		assertEquals(1, h2.size());
		assertSame(h2, h2.get("myself"));
	}

	public static class PersonBaseBean
	{
		public static String getStaticName() {
			return "static base";
		}
		
		public String getBaseName() {
			return "basename";
		}
	}

	public static class PersonBean extends PersonBaseBean implements java.io.Serializable {
		private static final long serialVersionUID = 3236709849734459121L;
		private String name;
	    private boolean deceased;
	    private int[] values;
	    
	    public PersonBean(String name, boolean deceased, int[] values) {
	    	this.name=name;
	    	this.deceased=deceased;
	    	this.values = values;
	    }
	 
	    public String getName() {
	        return this.name;
	    }
	 
	    public boolean isDeceased() {
	        return this.deceased;
	    }
	    
	    public int[] getValues() {
	    	return this.values;
	    }
	    
	    @SuppressWarnings("unused")
		private String getPrivate() {
	    	return "nothing";
	    }
	    
	    public String doSomething() {
	    	return "nothing";
	    }
	    
	    public static String getStaticName() {
	    	return "static";
	    }
	    
	    public int get_int2() {
	    	return 42;
	    }
	    
	    public int getint3() {
	    	return 99;
	    }
	    
	    public int getNUMBER() {
	    	return 123;
	    }
	    
	    public int getX() {
	    	return 99;
	    }
	}


	@Test
	public void testBeans() throws PickleException, IOException
	{
		Pickler p=new Pickler(false);
		Unpickler pu=new Unpickler();
		byte[] o;
		PersonBean person=new PersonBean("Tupac",true, new int[] {3,4,5});
		o=p.dumps(person);
		@SuppressWarnings("unchecked")
		Map<String,Object> map=(Map<String,Object>)pu.loads(o);
		
		assertEquals(9, map.size());
		assertEquals("basename", map.get("baseName"));
		assertEquals(42, map.get("_int2"));
		assertEquals(99, map.get("int3"));
		assertEquals(123, map.get("NUMBER"));
		assertEquals(99, map.get("x"));
		assertEquals("Tupac", map.get("name"));
		assertEquals(true, map.get("deceased"));
		assertArrayEquals(new int[] {3,4,5}, (int[]) map.get("values"));
		assertEquals("net.razorvine.pickle.test.PicklerTest$PersonBean", map.get("__class__"));
	}
	
	class NotABean {
		public int x;
	}

	@Test(expected=PickleException.class)
	public void testFailure() throws PickleException, IOException, URISyntaxException
	{
		NotABean notabean=new NotABean();
		Pickler p=new Pickler(false);
		p.dumps(notabean);
	}
	
	class CustomClass {
		public int x=42;
	}
	class CustomClassPickler implements IObjectPickler {
		public void pickle(Object o, OutputStream out, Pickler currentpickler) throws PickleException, IOException {
			CustomClass c=(CustomClass)o;
			currentpickler.save("customclassint="+c.x);
		}
	}
	
	@Test
	public void testCustomPickler() throws PickleException, IOException
	{
		Pickler.registerCustomPickler(CustomClass.class, new CustomClassPickler());
		CustomClass c=new CustomClass();
		Pickler p=new Pickler(false);
		p.dumps(c);
	}
	
	public static void main(String[] args) throws PickleException, IOException
	{
	}
}
