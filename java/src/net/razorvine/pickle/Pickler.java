package net.razorvine.pickle;

import java.beans.BeanInfo;
import java.beans.IntrospectionException;
import java.beans.Introspector;
import java.beans.PropertyDescriptor;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.Calendar;
import java.util.Collection;
import java.util.GregorianCalendar;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * Pickle an object graph into a Python-compatible pickle stream. For
 * simplicity, the only supported pickle protocol at this time is protocol 2.
 *
 * JAVA TYPE --> PYTHON TYPE
 * null None
 * boolean bool
 * byte byte/int
 * char unicodestring (len 1)
 * String unicodestring
 * double float
 * float float
 * int/short/byte int
 * bigdecimal decimal
 * biginteger long
 * array array if elements are primitive type, else tuple
 * Object[] tuple
 * byte[] bytearray
 * date datetime
 * Calendar datetime
 * Enum just the enum value as string
 * set set
 * map,hashtable dict
 * vector,collection list
 * javabean dict
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class Pickler {

	public static int HIGHEST_PROTOCOL = 2;


	private OutputStream out;
	private int PROTOCOL = 2;
	private PickleUtils utils;
	private static Map<Class<?>, IObjectPickler> customPicklers=new HashMap<Class<?>, IObjectPickler>();
	
	/**
	 * Create a Pickler.
	 * 
	 * @throws IOException
	 */
	public Pickler() {
	}

	/**
	 * Close the pickler stream, discard any internal buffers.
	 */
	public void close() throws IOException {
		out.flush();
		out.close();
	}

	/**
	 * Register additional object picklers for custom classes.
	 */
	public static void registerCustomPickler(Class<?> clazz, IObjectPickler pickler) {
		customPicklers.put(clazz, pickler);
	}
	
	/**
	 * Pickle a given object graph, returning the result as a byte array.
	 */
	public byte[] dumps(Object o) throws PickleException, IOException {
		ByteArrayOutputStream bo = new ByteArrayOutputStream();
		dump(o, bo);
		bo.flush();
		return bo.toByteArray();
	}

	/**
	 * Pickle a given object graph, writing the result to the output stream.
	 */
	public void dump(Object o, OutputStream stream) throws IOException, PickleException {
		out = stream;
		utils = new PickleUtils(null);
		out.write(Opcodes.PROTO);
		out.write(PROTOCOL);
		save(o);
		out.write(Opcodes.STOP);
		out.flush();
	}

	/**
	 * Pickle a single object and write its pickle representation to the output stream.
	 * Normally this is used internally by the pickler, but you can also utilize it from
	 * within custom picklers. This is handy if as part of the custom pickler, you need
	 * to write a couple of normal objects such as strings or ints, that are already
	 * supported by the pickler.
	 */
	public void save(Object o) throws PickleException, IOException {
		// null type?
		if(o==null) {
			out.write(Opcodes.NONE);
			return;
		}
		
		// check the dispatch table
		Class<?> t=o.getClass();
		Pair<Boolean, Boolean> result=dispatch(t,o);    // returns:  output_ok, must_memo
		if(result.a) {
			if(result.b) {
				// @todo: add to memo
			}
			return;
		}

		throw new PickleException("couldn't pickle object of type "+t);
	}

	/**
	 * Process a single object to be pickled.
	 */
	private Pair<Boolean, Boolean> dispatch(Class<?> t, Object o) throws IOException {
		// is it a primitive array?
		Class<?> componentType = t.getComponentType();
		if(componentType!=null) {
			if(componentType.isPrimitive()) {
				put_arrayOfPrimitives(componentType, o);
			} else {
				put_arrayOfObjects((Object[])o);
			}
			
			return new Pair<Boolean,Boolean>(true,true);
		}
		
		// first the primitive types
		if(o instanceof Boolean || t.equals(Boolean.TYPE)) {
			put_bool((Boolean)o);
			return new Pair<Boolean,Boolean>(true,false);
		}
		if(o instanceof Byte || t.equals(Byte.TYPE)) {
			put_long(((Byte)o).longValue());
			return new Pair<Boolean,Boolean>(true,false);
		}
		if(o instanceof Short || t.equals(Short.TYPE)) {
			put_long(((Short)o).longValue());
			return new Pair<Boolean,Boolean>(true,false);
		}
		if(o instanceof Integer || t.equals(Integer.TYPE)) {
			put_long(((Integer)o).longValue());
			return new Pair<Boolean,Boolean>(true,false);
		}
		if(o instanceof Long || t.equals(Long.TYPE)) {
			put_long(((Long)o).longValue());
			return new Pair<Boolean,Boolean>(true,false);
		}
		if(o instanceof Float || t.equals(Float.TYPE)) {
			put_float(((Float)o).doubleValue());
			return new Pair<Boolean,Boolean>(true,false);
		}
		if(o instanceof Double || t.equals(Double.TYPE)) {
			put_float(((Double)o).doubleValue());
			return new Pair<Boolean,Boolean>(true,false);
		}
		if(o instanceof Character || t.equals(Character.TYPE)) {
			put_string(""+o);
			return new Pair<Boolean,Boolean>(true,false);
		}
		
		// check registry
		IObjectPickler custompickler=customPicklers.get(t);
		if(custompickler!=null) {
			custompickler.pickle(o, this.out, this);
			return new Pair<Boolean,Boolean>(true,true);
		}
		
		// more complex types
		if(o instanceof String) {
			put_string((String)o);
			return new Pair<Boolean,Boolean>(true,true);
		}
		if(o instanceof BigInteger) {
			put_bigint((BigInteger)o);
			return new Pair<Boolean,Boolean>(true,true);
		} 
		if(o instanceof BigDecimal) {
			put_decimal((BigDecimal)o);
			return new Pair<Boolean,Boolean>(true,true);
		}
		if(o instanceof java.util.Date) {
			java.util.Date date=(java.util.Date)o;
			Calendar cal=GregorianCalendar.getInstance();
			cal.setTime(date);
			put_calendar(cal);
			return new Pair<Boolean,Boolean>(true,true);
		}
		if(o instanceof Calendar) {
			put_calendar((Calendar)o);
			return new Pair<Boolean,Boolean>(true,true);
		}
		if(o instanceof Enum) {
			put_string(o.toString());
			return new Pair<Boolean,Boolean>(true,true);
		}
		if(o instanceof Set<?>) {
			put_set((Set<?>)o);
			return new Pair<Boolean,Boolean>(true,true);
		}
		if(o instanceof Map<?,?>) {
			put_map((Map<?,?>)o);
			return new Pair<Boolean,Boolean>(true,true);
		}
		if(o instanceof List<?>) {
			put_collection((List<?>)o);
			return new Pair<Boolean,Boolean>(true,true);
		}
		if(o instanceof Collection<?>) {
			put_collection((Collection<?>)o);
			return new Pair<Boolean,Boolean>(true,true);
		}
		// javabean		
		if(o instanceof java.io.Serializable ) {
			put_javabean(o);
			return new Pair<Boolean,Boolean>(true,true);
		}
		return new Pair<Boolean,Boolean>(false,false);
	}

	void put_collection(Collection<?> list) throws IOException {
		out.write(Opcodes.EMPTY_LIST);
		out.write(Opcodes.MARK);
		for(Object o: list) {
			save(o);
		}
		out.write(Opcodes.APPENDS);
	}

	void put_map(Map<?,?> o) throws IOException {
		out.write(Opcodes.EMPTY_DICT);
		out.write(Opcodes.MARK);
		for(Object k: o.keySet()) {
			save(k);
			save(o.get(k));
		}
		out.write(Opcodes.SETITEMS);
	}

	void put_set(Set<?> o) throws IOException {
		out.write(Opcodes.GLOBAL);
		out.write("__builtin__\nset\n".getBytes());
		out.write(Opcodes.EMPTY_LIST);
		out.write(Opcodes.MARK);
		for(Object x: o) {
			save(x);
		}
		out.write(Opcodes.APPENDS);
		out.write(Opcodes.TUPLE1);
		out.write(Opcodes.REDUCE);
	}

	void put_calendar(Calendar cal) throws IOException {
		out.write(Opcodes.GLOBAL);
		out.write("datetime\ndatetime\n".getBytes());
		out.write(Opcodes.SHORT_BINSTRING);
		out.write(10);
		int year=cal.get(Calendar.YEAR);
		out.write(year>>8);
		out.write(year&0xff);
		out.write(cal.get(Calendar.MONTH)+1);  // months start at 0 in java
		out.write(cal.get(Calendar.DAY_OF_MONTH));
		out.write(cal.get(Calendar.HOUR_OF_DAY));
		out.write(cal.get(Calendar.MINUTE));
		out.write(cal.get(Calendar.SECOND));
		int microsecs=1000*cal.get(Calendar.MILLISECOND);
		out.write((microsecs>>16)&0xff);
		out.write((microsecs>>8)&0xff);
		out.write(microsecs&0xff);

		out.write(Opcodes.TUPLE1);
		out.write(Opcodes.REDUCE);		
	}

	void put_arrayOfObjects(Object[] array) throws IOException {
		// 0 objects->EMPTYTUPLE
		// 1 object->TUPLE1
		// 2 objects->TUPLE2
		// 3 objects->TUPLE3
		// 4 or more->MARK+items+TUPLE
		if(array.length==0) {
			out.write(Opcodes.EMPTY_TUPLE);
		} else if(array.length==1) {
			save(array[0]);
			out.write(Opcodes.TUPLE1);
		} else if(array.length==2) {
			save(array[0]);
			save(array[1]);
			out.write(Opcodes.TUPLE2);
		} else if(array.length==3) {
			save(array[0]);
			save(array[1]);
			save(array[2]);
			out.write(Opcodes.TUPLE3);
		} else {
			out.write(Opcodes.MARK);
			for(Object o: array) {
				save(o);
			}
			out.write(Opcodes.TUPLE);
		}
	}

	void put_arrayOfPrimitives(Class<?> t, Object array) throws IOException {

		if(t.equals(Boolean.TYPE)) {
			// a bool[] isn't written as an array but rather as a tuple
			boolean[] source=(boolean[])array;
			Boolean[] boolarray=new Boolean[source.length];
			for(int i=0; i<source.length; ++i) {
				boolarray[i]=source[i];
			}
			put_arrayOfObjects(boolarray);
			return;
		}
		if(t.equals(Character.TYPE)) {
			// a char[] isn't written as an array but rather as a unicode string
			String s=new String((char[])array);
			put_string(s);
			return;
		}		
		if(t.equals(Byte.TYPE)) {
			// a byte[] isn't written as an array but rather as a bytearray object
			out.write(Opcodes.GLOBAL);
			out.write("__builtin__\nbytearray\n".getBytes());
			put_string(new String((byte[])array,"iso-8859-15"));
			put_string("iso-8859-15");
			out.write(Opcodes.TUPLE2);
			out.write(Opcodes.REDUCE);
			return;
		} 
		
		out.write(Opcodes.GLOBAL);
		out.write("array\narray\n".getBytes());
		out.write(Opcodes.SHORT_BINSTRING);		// array typecode follows
		out.write(1); // typecode is 1 char
		
		if(t.equals(Short.TYPE)) {
			out.write('h'); // signed short
			out.write(Opcodes.EMPTY_LIST);
			out.write(Opcodes.MARK);
			for(short s: (short[])array) {
				save(s);
			}
		} else if(t.equals(Integer.TYPE)) {
			out.write('i'); // signed int
			out.write(Opcodes.EMPTY_LIST);
			out.write(Opcodes.MARK);
			for(int i: (int[])array) {
				save(i);
			}
		} else if(t.equals(Long.TYPE)) {
			out.write('l');  // signed long
			out.write(Opcodes.EMPTY_LIST);
			out.write(Opcodes.MARK);
			for(long v: (long[])array) {
				save(v);
			}
		} else if(t.equals(Float.TYPE)) {
			out.write('f');  // float
			out.write(Opcodes.EMPTY_LIST);
			out.write(Opcodes.MARK);
			for(float f: (float[])array) {
				save(f);
			}
		} else if(t.equals(Double.TYPE)) {
			out.write('d');  // double
			out.write(Opcodes.EMPTY_LIST);
			out.write(Opcodes.MARK);
			for(double d: (double[])array) {
				save(d);
			}
		} 
		
		out.write(Opcodes.APPENDS);
		out.write(Opcodes.TUPLE2);
		out.write(Opcodes.REDUCE);
	}

	void put_decimal(BigDecimal d) throws IOException {
		//"cdecimal\nDecimal\nU\n12345.6789\u0085R."
		out.write(Opcodes.GLOBAL);
		out.write("decimal\nDecimal\n".getBytes());
		put_string(d.toEngineeringString());
		out.write(Opcodes.TUPLE1);
		out.write(Opcodes.REDUCE);
	}


	void put_bigint(BigInteger i) throws IOException {
		byte[] b=utils.encode_long(i);
		if(b.length<=0xff) {	
			out.write(Opcodes.LONG1);
			out.write(b.length);
			out.write(b);
		} else {
			out.write(Opcodes.LONG4);
			out.write(utils.integer_to_bytes(b.length));
			out.write(b);
		}
	}

	void put_string(String string) throws IOException {
		byte[] encoded=string.getBytes("UTF-8");
		out.write(Opcodes.BINUNICODE);
		out.write(utils.integer_to_bytes(encoded.length));
		out.write(encoded);
	}

	void put_float(double d) throws IOException {
		out.write(Opcodes.BINFLOAT);
		out.write(utils.double_to_bytes(d));
	}	

	void put_long(long v) throws IOException {
		// choose optimal representation
		// first check 1 and 2-byte unsigned ints:
		if(v>=0) {
			if(v<=0xff) {
				out.write(Opcodes.BININT1);
				out.write((int)v);
				return;
			}
			if(v<=0xffff) {
				out.write(Opcodes.BININT2);
				out.write((int)v&0xff);
				out.write((int)v>>8);
				return;
			}
		}
		
		// 4-byte signed int?
		long high_bits=v>>31;  // shift sign extends
		if(high_bits==0 || high_bits==-1) {
            // All high bits are copies of bit 2**31, so the value fits in a 4-byte signed int.
			out.write(Opcodes.BININT);
			out.write(utils.integer_to_bytes((int)v));
            return;
		}
		
		// int too big, store it as text
		out.write(Opcodes.INT);
		out.write((""+v).getBytes());
		out.write('\n');
	}
	
	void put_bool(boolean b) throws IOException {
		if(b)
			out.write(Opcodes.NEWTRUE);
		else
			out.write(Opcodes.NEWFALSE);
	}

	void put_javabean(Object o) throws PickleException, IOException {
		Map<String,Object> map=new HashMap<String,Object>();
		try {
			BeanInfo info=Introspector.getBeanInfo(o.getClass(), Object.class);
			for(PropertyDescriptor p: info.getPropertyDescriptors()) {
				String name=p.getName();
				Method readmethod=p.getReadMethod();
				Object value=readmethod.invoke(o);
				map.put(name, value);
			}
			map.put("__class__", o.getClass().getName());
			save(map);
		} catch (IntrospectionException e) {
			throw new PickleException("couldn't introspect javabean: "+e);
		} catch (IllegalArgumentException e) {
			throw new PickleException("couldn't introspect javabean: "+e);
		} catch (IllegalAccessException e) {
			throw new PickleException("couldn't introspect javabean: "+e);
		} catch (InvocationTargetException e) {
			throw new PickleException("couldn't introspect javabean: "+e);
		}
	}
}
