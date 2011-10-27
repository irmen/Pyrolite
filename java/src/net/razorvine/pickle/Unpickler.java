package net.razorvine.pickle;

import java.io.ByteArrayInputStream;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.lang.reflect.Method;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;

import net.razorvine.pickle.objects.AnyClassConstructor;
import net.razorvine.pickle.objects.ArrayConstructor;
import net.razorvine.pickle.objects.ByteArrayConstructor;
import net.razorvine.pickle.objects.ComplexNumber;
import net.razorvine.pickle.objects.DateTimeConstructor;
import net.razorvine.pickle.objects.SetConstructor;

/**
 * Unpickles an object graph from a pickle data inputstream.
 * Maps the python objects on the corresponding java equivalents or similar types..
 * 
 * PYTHON TYPE --> JAVA TYPE
 * None            null
 * bool            boolean
 * int             int
 * long            Number: long or BigInteger
 * string          String
 * unicode         String
 * complex         objects.ComplexNumber
 * datetime.date   Calendar
 * datetime.datetime Calendar
 * datetime.time   Calendar
 * datetime.timedelta objects.TimeDelta
 * float           double
 * array           array
 * list            ArrayList<Object>
 * tuple           Object[]
 * set             Set
 * dict            Map
 * bytes           byte[]
 * bytearray       byte[]
 * decimal         ????
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class Unpickler {

	private final int HIGHEST_PROTOCOL = 3;

	private PickleUtils pu;
	private Map<Integer, Object> memo;
	private UnpickleStack stack;
	private static Map<String, IObjectConstructor> objectConstructors;

	static {
		objectConstructors = new HashMap<String, IObjectConstructor>();
		objectConstructors.put("__builtin__.complex", new AnyClassConstructor(ComplexNumber.class));
		objectConstructors.put("builtins.complex", new AnyClassConstructor(ComplexNumber.class));
		objectConstructors.put("array.array", new ArrayConstructor());
		objectConstructors.put("array._array_reconstructor", new ArrayConstructor());
		objectConstructors.put("__builtin__.bytearray", new ByteArrayConstructor());
		objectConstructors.put("builtins.bytearray", new ByteArrayConstructor());
		objectConstructors.put("__builtin__.bytes", new ByteArrayConstructor());
		objectConstructors.put("__builtin__.set", new SetConstructor());
		objectConstructors.put("builtins.set", new SetConstructor());
		objectConstructors.put("datetime.datetime", new DateTimeConstructor(DateTimeConstructor.DATETIME));
		objectConstructors.put("datetime.time", new DateTimeConstructor(DateTimeConstructor.TIME));
		objectConstructors.put("datetime.date", new DateTimeConstructor(DateTimeConstructor.DATE));
		objectConstructors.put("datetime.timedelta", new DateTimeConstructor(DateTimeConstructor.TIMEDELTA));
		objectConstructors.put("decimal.Decimal", new AnyClassConstructor(BigDecimal.class));
	}

	/**
	 * Main method, used by the test scripts to drive the unpickler
	 */
	public static void main(String[] args) {
		if(args.length!=2) {
			throw new IllegalArgumentException("requires 2 arguments: the pickle file, and the result file");
		}
			
		Unpickler up=new Unpickler();
		try {
			InputStream stream;
			if(args[0].equals("-")) {
				stream=System.in;
			} else {
				stream=new FileInputStream(args[0]);
			}
			Object o = up.load(stream);
			
			FileOutputStream fos=new FileOutputStream(args[1]);
			PrettyPrint.print(o, fos);
			fos.flush();
			fos.close();
		} catch (Exception e) {
			e.printStackTrace();
		}
	}
	
	/**
	 * Create an unpickler.
	 */
	public Unpickler() {
		memo = new HashMap<Integer, Object>();
	}

	/**
	 * Register additional object constructors for custom classes.
	 */
	public static void registerConstructor(String module, String classname, IObjectConstructor constructor) {
		objectConstructors.put(module + "." + classname, constructor);
	}

	/**
	 * Read a pickled object representation from the given input stream.
	 * 
	 * @return the reconstituted object hierarchy specified in the file.
	 */
	public Object load(InputStream stream) throws PickleException, IOException {
		pu = new PickleUtils(stream);
		stack = new UnpickleStack();
		try {
			while (true) {
				short key = pu.readbyte();
				if (key == -1)
					throw new IOException("premature end of file");
				dispatch(key);
			}
		} catch (StopException x) {
			return x.value;
		}
	}

	/**
	 * Read a pickled object representation from the given pickle data bytes.
	 * 
	 * @return the reconstituted object hierarchy specified in the file.
	 */
	public Object loads(byte[] pickledata) throws PickleException, IOException {
		return load(new ByteArrayInputStream(pickledata));
	}

	/**
	 * Close the unpickler and frees the resources such as the unpickle stack and memo table.
	 */
	public void close() {
		stack.clear();
		memo.clear();
	}

	private class StopException extends RuntimeException {
		private static final long serialVersionUID = 6528222454688362873L;

		public StopException(Object value) {
			this.value = value;
		}

		public Object value;
	}

	/**
	 * Process a single pickle stream opcode.
	 */
	protected void dispatch(short key) throws PickleException, IOException {
		switch (key) {
		case Opcodes.MARK:
			load_mark();
			break;
		case Opcodes.STOP:
			Object value = stack.pop();
			stack.clear();
			throw new StopException(value);
		case Opcodes.POP:
			load_pop();
			break;
		case Opcodes.POP_MARK:
			load_pop_mark();
			break;
		case Opcodes.DUP:
			load_dup();
			break;
		case Opcodes.FLOAT:
			load_float();
			break;
		case Opcodes.INT:
			load_int();
			break;
		case Opcodes.BININT:
			load_binint();
			break;
		case Opcodes.BININT1:
			load_binint1();
			break;
		case Opcodes.LONG:
			load_long();
			break;
		case Opcodes.BININT2:
			load_binint2();
			break;
		case Opcodes.NONE:
			load_none();
			break;
		case Opcodes.PERSID:
			throw new PickleException("opcode not implemented: PERSID");
		case Opcodes.BINPERSID:
			throw new PickleException("opcode not implemented: BINPERSID");
		case Opcodes.REDUCE:
			load_reduce();
			break;
		case Opcodes.STRING:
			load_string();
			break;
		case Opcodes.BINSTRING:
			load_binstring();
			break;
		case Opcodes.SHORT_BINSTRING:
			load_short_binstring();
			break;
		case Opcodes.UNICODE:
			load_unicode();
			break;
		case Opcodes.BINUNICODE:
			load_binunicode();
			break;
		case Opcodes.APPEND:
			load_append();
			break;
		case Opcodes.BUILD:
			load_build();
			break;
		case Opcodes.GLOBAL:
			load_global();
			break;
		case Opcodes.DICT:
			load_dict();
			break;
		case Opcodes.EMPTY_DICT:
			load_empty_dictionary();
			break;
		case Opcodes.APPENDS:
			load_appends();
			break;
		case Opcodes.GET:
			load_get();
			break;
		case Opcodes.BINGET:
			load_binget();
			break;
		case Opcodes.INST:
			throw new PickleException("opcode not implemented: INST");
		case Opcodes.LONG_BINGET:
			load_long_binget();
			break;
		case Opcodes.LIST:
			load_list();
			break;
		case Opcodes.EMPTY_LIST:
			load_empty_list();
			break;
		case Opcodes.OBJ:
			throw new PickleException("opcode not implemented: OBJ");
		case Opcodes.PUT:
			load_put();
			break;
		case Opcodes.BINPUT:
			load_binput();
			break;
		case Opcodes.LONG_BINPUT:
			load_long_binput();
			break;
		case Opcodes.SETITEM:
			load_setitem();
			break;
		case Opcodes.TUPLE:
			load_tuple();
			break;
		case Opcodes.EMPTY_TUPLE:
			load_empty_tuple();
			break;
		case Opcodes.SETITEMS:
			load_setitems();
			break;
		case Opcodes.BINFLOAT:
			load_binfloat();
			break;

		// protocol 2
		case Opcodes.PROTO:
			load_proto();
			break;
		case Opcodes.NEWOBJ:
			load_newobj();
			break;
		case Opcodes.EXT1:
			throw new PickleException("opcode not implemented: EXT1");
		case Opcodes.EXT2:
			throw new PickleException("opcode not implemented: EXT2");
		case Opcodes.EXT4:
			throw new PickleException("opcode not implemented: EXT4");
		case Opcodes.TUPLE1:
			load_tuple1();
			break;
		case Opcodes.TUPLE2:
			load_tuple2();
			break;
		case Opcodes.TUPLE3:
			load_tuple3();
			break;
		case Opcodes.NEWTRUE:
			load_true();
			break;
		case Opcodes.NEWFALSE:
			load_false();
			break;
		case Opcodes.LONG1:
			load_long1();
			break;
		case Opcodes.LONG4:
			load_long4();
			break;

		// Protocol 3 (Python 3.x)
		case Opcodes.BINBYTES:
			load_binbytes();
			break;
		case Opcodes.SHORT_BINBYTES:
			load_short_binbytes();
			break;

		default:
			throw new PickleException("invalid pickle opcode: " + key);
		}
	}

	void load_build() {
		Object args=stack.pop();
		Object target=stack.peek();
		try {
			Method setStateMethod=target.getClass().getMethod("setState", args.getClass());
			setStateMethod.invoke(target, args);
		} catch (Exception e) {
			throw new PickleException("failed to setState()",e);
		}
	}

	void load_proto() throws IOException {
		short proto = pu.readbyte();
		if (proto < 0 || proto > HIGHEST_PROTOCOL)
			throw new PickleException("unsupported pickle protocol: " + proto);
	}

	void load_none() {
		stack.add(null);
	}

	void load_false() {
		stack.add(false);
	}

	void load_true() {
		stack.add(true);
	}

	void load_int() throws IOException {
		String data = pu.readline(true);
		Object val;
		if (data.equals(Opcodes.FALSE.substring(1)))
			val = false;
		else if (data.equals(Opcodes.TRUE.substring(1)))
			val = true;
		else {
			String number=data.substring(0, data.length() - 1);
			try {
				val = Integer.parseInt(number, 10);
			} catch (NumberFormatException x) {
				// hmm, integer didnt' work.. is it perhaps an int from a 64-bit python? so try long:
				val = Long.parseLong(number, 10);
			}
		}
		stack.add(val);
	}

	void load_binint() throws IOException {
		int integer = pu.bytes_to_integer(pu.readbytes(4));
		stack.add(integer);
	}

	void load_binint1() throws IOException {
		stack.add((int)pu.readbyte());
	}

	void load_binint2() throws IOException {
		int integer = pu.bytes_to_integer(pu.readbytes(2));
		stack.add(integer);
	}

	void load_long() throws IOException {
		String val = pu.readline();
		if (val != null && val.endsWith("L")) {
			val = val.substring(0, val.length() - 1);
		}
		BigInteger bi = new BigInteger(val);
		stack.add(pu.optimizeBigint(bi));
	}

	void load_long1() throws IOException {
		short n = pu.readbyte();
		byte[] data = pu.readbytes(n);
		stack.add(pu.decode_long(data));
	}

	void load_long4() throws IOException {
		int n = pu.bytes_to_integer(pu.readbytes(4));
		byte[] data = pu.readbytes(n);
		stack.add(pu.decode_long(data));
	}

	void load_float() throws IOException {
		String val = pu.readline(true);
		stack.add(Double.parseDouble(val));
	}

	void load_binfloat() throws IOException {
		double val = pu.bytes_to_double(pu.readbytes(8));
		stack.add(val);
	}

	void load_string() throws IOException {
		String rep = pu.readline();
		boolean quotesOk = false;
		for (String q : new String[] { "\"", "'" }) // double or single quote
		{
			if (rep.startsWith(q)) {
				if (!rep.endsWith(q)) {
					throw new PickleException("insecure string pickle");
				}
				rep = rep.substring(1, rep.length() - 1); // strip quotes
				quotesOk = true;
				break;
			}
		}

		if (!quotesOk)
			throw new PickleException("insecure string pickle");

		stack.add(pu.decode_escaped(rep));
	}

	void load_binstring() throws IOException {
		int len = pu.bytes_to_integer(pu.readbytes(4));
		byte[] data = pu.readbytes(len);
		stack.add(pu.rawStringFromBytes(data));
	}

	void load_binbytes() throws IOException {
		int len = pu.bytes_to_integer(pu.readbytes(4));
		stack.add(pu.readbytes(len));
	}

	void load_unicode() throws IOException {
		String str=pu.decode_unicode_escaped(pu.readline());
		stack.add(str);
	}

	void load_binunicode() throws IOException {
		int len = pu.bytes_to_integer(pu.readbytes(4));
		byte[] data = pu.readbytes(len);
		stack.add(new String(data,"UTF-8"));
	}

	void load_short_binstring() throws IOException {
		short len = pu.readbyte();
		byte[] data = pu.readbytes(len);
		stack.add(pu.rawStringFromBytes(data));
	}

	void load_short_binbytes() throws IOException {
		short len = pu.readbyte();
		stack.add(pu.readbytes(len));
	}

	void load_tuple() {
		ArrayList<Object> top = stack.pop_all_since_marker();
		stack.add(top.toArray());
	}

	void load_empty_tuple() {
		stack.add(new Object[0]);
	}

	void load_tuple1() {
		stack.add(new Object[] { stack.pop() });
	}

	void load_tuple2() {
		Object o2 = stack.pop();
		Object o1 = stack.pop();
		stack.add(new Object[] { o1, o2 });
	}

	void load_tuple3() {
		Object o3 = stack.pop();
		Object o2 = stack.pop();
		Object o1 = stack.pop();
		stack.add(new Object[] { o1, o2, o3 });
	}

	void load_empty_list() {
		stack.add(new ArrayList<Object>(0));
	}

	void load_empty_dictionary() {
		stack.add(new HashMap<Object, Object>(0));
	}

	void load_list() {
		ArrayList<Object> top = stack.pop_all_since_marker();
		stack.add(top); // simply add the top items as a list to the stack again
	}

	void load_dict() {
		ArrayList<Object> top = stack.pop_all_since_marker();
		HashMap<Object, Object> map = new HashMap<Object, Object>(top.size());
		for (int i = 0; i < top.size(); i += 2) {
			Object key = top.get(i);
			Object value = top.get(i + 1);
			map.put(key, value);
		}
		stack.add(map);
	}

	void load_global() throws IOException {
		String module = pu.readline();
		String name = pu.readline();
		IObjectConstructor constructor = objectConstructors.get(module + "." + name);
		if (constructor == null) {
			// check if it is an exception
			if(module.equals("exceptions")) {
				constructor=new AnyClassConstructor(PythonException.class);
			} else {
				throw new PickleException("unsupported class: " + module + "." + name);
			}
		}
		stack.add(constructor);
	}

	void load_pop() {
		stack.pop();
	}

	void load_pop_mark() {
		Object o = null;
		do {
			o = stack.pop();
		} while (o != stack.MARKER);
		stack.trim();
	}

	void load_dup() {
		stack.add(stack.peek());
	}

	void load_get() throws IOException {
		int i = Integer.parseInt(pu.readline(), 10);
		stack.add(memo.get(i));
	}

	void load_binget() throws IOException {
		int i = pu.readbyte();
		stack.add(memo.get(i));
	}

	void load_long_binget() throws IOException {
		int i = pu.bytes_to_integer(pu.readbytes(4));
		stack.add(memo.get(i));
	}

	void load_put() throws IOException {
		int i = Integer.parseInt(pu.readline(), 10);
		memo.put(i, stack.peek());
	}

	void load_binput() throws IOException {
		int i = pu.readbyte();
		memo.put(i, stack.peek());
	}

	void load_long_binput() throws IOException {
		int i = pu.bytes_to_integer(pu.readbytes(4));
		memo.put(i, stack.peek());
	}

	void load_append() {
		Object value = stack.pop();
		@SuppressWarnings("unchecked")
		ArrayList<Object> list = (ArrayList<Object>) stack.peek();
		list.add(value);
	}

	void load_appends() {
		ArrayList<Object> top = stack.pop_all_since_marker();
		@SuppressWarnings("unchecked")
		ArrayList<Object> list = (ArrayList<Object>) stack.peek();
		list.addAll(top);
		list.trimToSize();
	}

	void load_setitem() {
		Object value = stack.pop();
		Object key = stack.pop();
		@SuppressWarnings("unchecked")
		Map<Object, Object> dict = (Map<Object, Object>) stack.peek();
		dict.put(key, value);
	}

	void load_setitems() {
		HashMap<Object, Object> newitems = new HashMap<Object, Object>();
		Object value = stack.pop();
		while (value != stack.MARKER) {
			Object key = stack.pop();
			newitems.put(key, value);
			value = stack.pop();
		}
		
		@SuppressWarnings("unchecked")
		Map<Object, Object> dict = (Map<Object, Object>) stack.peek();
		dict.putAll(newitems);
	}

	void load_mark() {
		stack.add_mark();
	}

	void load_reduce() {
		Object[] args = (Object[]) stack.pop();
		IObjectConstructor constructor = (IObjectConstructor) stack.pop();
		stack.add(constructor.construct(args));
	}

	void load_newobj() {
		load_reduce(); // for Java we just do the same as class(*args) instead
						// of class.__new__(class,*args)
	}
}
