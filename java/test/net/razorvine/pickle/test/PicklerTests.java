package net.razorvine.pickle.test;

import static org.junit.Assert.assertArrayEquals;
import static org.junit.Assert.assertEquals;

import java.io.IOException;
import java.io.OutputStream;
import java.io.UnsupportedEncodingException;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.net.URISyntaxException;
import java.util.Calendar;
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

import net.razorvine.pickle.IObjectPickler;
import net.razorvine.pickle.Opcodes;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.Pickler;
import net.razorvine.pickle.Unpickler;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

/**
 * Unit tests for the pickler.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PicklerTests {

	@Before
	public void setUp() throws Exception {
	}

	@After
	public void tearDown() throws Exception {
	}


	byte[] B(String s) {
		try {
			byte[] bytes=s.getBytes("ISO-8859-15");
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

	public enum DayEnum {
	    SUNDAY, MONDAY, TUESDAY, WEDNESDAY, 
	    THURSDAY, FRIDAY, SATURDAY 
	};
	
	@Test
	public void testSinglePrimitives() throws PickleException, IOException {
		// protocol level 2
		Pickler p=new Pickler();
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
	public void testBigInts() throws PickleException, IOException
	{
		Pickler p =new Pickler();
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
		Pickler p =new Pickler();
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
		assertArrayEquals(B("c__builtin__\nbytearray\nX\u0003\u0000\u0000\u0000\u0001\u0002\u0003X\u000b\u0000\u0000\u0000iso-8859-15\u0086R"), o);

		o=p.dumps(new int[] {1,2,3});
		assertArrayEquals(B("carray\narray\nU\u0001i](K\u0001K\u0002K\u0003e\u0086R"), o);

		o=p.dumps(new double[] {1.1,2.2,3.3});
		assertArrayEquals(B("carray\narray\nU\u0001d](G?\u00f1\u0099\u0099\u0099\u0099\u0099\u009aG@\u0001\u0099\u0099\u0099\u0099\u0099\u009aG@\nffffffe\u0086R"), o);
	}
	
	@Test
	public void testDates() throws PickleException, IOException
	{
		Pickler p=new Pickler();
		Date date=new GregorianCalendar(2011,Calendar.DECEMBER,31,14,33,59).getTime();
		byte[] o=p.dumps(date);
		assertArrayEquals(B("cdatetime\ndatetime\nU\n\u0007\u00db\u000c\u001f\u000e!;\u0000\u0000\u0000\u0085R"),o);
		
		Calendar cal=new GregorianCalendar(2011,Calendar.DECEMBER,31,14,33,59);
		cal.set(Calendar.MILLISECOND, 456);
		o=p.dumps(cal);
		assertArrayEquals(B("cdatetime\ndatetime\nU\n\u0007\u00db\u000c\u001f\u000e!;\u0006\u00f5@\u0085R"),o);
	}
	
	@SuppressWarnings("unchecked")
	@Test
	public void testSets() throws PickleException, IOException
	{
		byte[] o;
		Pickler p=new Pickler();
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
		Pickler p=new Pickler();
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
		Pickler p=new Pickler();
		
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

	public class PersonBean implements java.io.Serializable {
		private static final long serialVersionUID = 3236709849734459121L;
		private String name;
	    private boolean deceased;
	    
	    public PersonBean(String name, boolean deceased) {
	    	this.name=name;
	    	this.deceased=deceased;
	    }
	 
	    public String getName() {
	        return this.name;
	    }
	 
	    public boolean isDeceased() {
	        return this.deceased;
	    }
	}


	@Test
	public void testBeans() throws PickleException, IOException
	{
		Pickler p=new Pickler();
		Unpickler pu=new Unpickler();
		byte[] o;
		PersonBean person=new PersonBean("Tupac",true);
		o=p.dumps(person);
		@SuppressWarnings("unchecked")
		Map<String,Object> map=(Map<String,Object>)pu.loads(o);
		Map<String,Object> testmap=new HashMap<String,Object>();
		testmap.put("name","Tupac");
		testmap.put("deceased",true);
		testmap.put("__class__","net.razorvine.pickle.test.PicklerTests$PersonBean");
		assertEquals(testmap, map);
	}
	
	class NotABean {
		public int x;
	}

	@Test(expected=PickleException.class)
	public void testFailure() throws PickleException, IOException, URISyntaxException
	{
		NotABean notabean=new NotABean();
		Pickler p=new Pickler();
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
		Pickler p=new Pickler();
		p.dumps(c);
	}
	
	public static void main(String[] args) throws PickleException, IOException
	{
	}
}
