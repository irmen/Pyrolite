package net.razorvine.pickle;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.lang.reflect.Method;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;

import net.razorvine.pickle.objects.AnyClassConstructor;
import net.razorvine.pickle.objects.ArrayConstructor;
import net.razorvine.pickle.objects.ByteArrayConstructor;
import net.razorvine.pickle.objects.ClassDictConstructor;
import net.razorvine.pickle.objects.ComplexNumber;
import net.razorvine.pickle.objects.DateTimeConstructor;
import net.razorvine.pickle.objects.ExceptionConstructor;
import net.razorvine.pickle.objects.OperatorAttrGetterForCalendarTz;
import net.razorvine.pickle.objects.TimeZoneConstructor;
import net.razorvine.pickle.objects.Reconstructor;
import net.razorvine.pickle.objects.SetConstructor;


/**
 * Unpickles an object graph from a pickle data inputstream. Supports all pickle protocol versions.
 * Maps the python objects on the corresponding java equivalents or similar types.
 * This class is NOT threadsafe! (Don't use the same pickler from different threads)
 *
 * See the README.txt for a table of the type mappings.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class Unpickler {

	/**
	 * Used as return value for {@link Unpickler#dispatch} in the general case (because the object graph is built on the stack)
	 */
	protected static final Object NO_RETURN_VALUE = new Object();

	/**
	 * The highest Python Pickle protocol version supported by this library.
	 */
	protected final int HIGHEST_PROTOCOL = 5;

	/**
	 * Internal cache of memoized objects.
	 */
	protected Map<Integer, Object> memo;

	/**
	 * The stack that is used for building the resulting object graph.
	 */
	protected UnpickleStack stack;

	/**
	 * The stream where the pickle data is read from.
	 */
	protected InputStream input;

	/**
	 * Registry of object constructors that are used to create the appropriate Java objects for the given Python module.typename references.
	 */
	protected static Map<String, IObjectConstructor> objectConstructors;

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
		objectConstructors.put("pytz._UTC", new TimeZoneConstructor(TimeZoneConstructor.UTC));
		objectConstructors.put("pytz._p", new TimeZoneConstructor(TimeZoneConstructor.PYTZ));
		objectConstructors.put("pytz.timezone", new TimeZoneConstructor(TimeZoneConstructor.PYTZ));
		objectConstructors.put("dateutil.tz.tzutc", new TimeZoneConstructor(TimeZoneConstructor.DATEUTIL_TZUTC));
		objectConstructors.put("dateutil.tz.tzfile", new TimeZoneConstructor(TimeZoneConstructor.DATEUTIL_TZFILE));
		objectConstructors.put("dateutil.zoneinfo.gettz", new TimeZoneConstructor(TimeZoneConstructor.DATEUTIL_GETTZ));
		objectConstructors.put("datetime.tzinfo", new TimeZoneConstructor(TimeZoneConstructor.TZINFO));
		objectConstructors.put("decimal.Decimal", new AnyClassConstructor(BigDecimal.class));
		objectConstructors.put("copy_reg._reconstructor", new Reconstructor());
		objectConstructors.put("operator.attrgetter", new OperatorAttrGetterForCalendarTz());
		objectConstructors.put("_codecs.encode", new ByteArrayConstructor());   // we're lucky, the bytearray constructor is also able to mimic codecs.encode()
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
		stack = new UnpickleStack();
		input = stream;
		while (true) {
			short key = PickleUtils.readbyte(input);
			if (key == -1)
				throw new IOException("premature end of file");
			Object value = dispatch(key);
			if (value != NO_RETURN_VALUE) {
				return value;
			}
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
		if(stack!=null)	stack.clear();
		if(memo!=null) memo.clear();
		if(input!=null)
			try {
				input.close();
			} catch (IOException e) {
			}
	}


	/**
	 * Buffer support for protocol 5 out of band data
	 * If you want to unpickle such pickles, you'll have to subclass the unpickler
	 * and override this method to return the buffer data you want.
	 */
	protected Object next_buffer() throws PickleException, IOException {
		throw new PickleException("pickle stream refers to out-of-band data but no user-overridden next_buffer() method is used\n");
	}


	/**
	 * Process a single pickle stream opcode.
	 */
	protected Object dispatch(short key) throws PickleException, IOException {
		switch (key) {
		case Opcodes.MARK:
			load_mark();
			break;
		case Opcodes.STOP:
			Object value = stack.pop();
			stack.clear();
			memo.clear();
			return value;		// final result value
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
			load_persid();
			break;
		case Opcodes.BINPERSID:
			load_binpersid();
			break;
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
			load_inst();
			break;
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
			load_obj();
			break;
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
		case Opcodes.EXT2:
		case Opcodes.EXT4:
			throw new PickleException("Unimplemented opcode EXT1/EXT2/EXT4 encountered. Don't use extension codes when pickling via copyreg.add_extension() to avoid this error.");
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

		// Protocol 4 (Python 3.4-3.7)
		case Opcodes.BINUNICODE8:
			load_binunicode8();
			break;
		case Opcodes.SHORT_BINUNICODE:
			load_short_binunicode();
			break;
		case Opcodes.BINBYTES8:
			load_binbytes8();
			break;
		case Opcodes.EMPTY_SET:
			load_empty_set();
			break;
		case Opcodes.ADDITEMS:
			load_additems();
			break;
		case Opcodes.FROZENSET:
			load_frozenset();
			break;
		case Opcodes.MEMOIZE:
			load_memoize();
			break;
		case Opcodes.FRAME:
			load_frame();
			break;
		case Opcodes.NEWOBJ_EX:
			load_newobj_ex();
			break;
		case Opcodes.STACK_GLOBAL:
			load_stack_global();
			break;

		// protocol 5 (python 3.8+)
		case Opcodes.BYTEARRAY8:
			load_bytearray8();
			break;
		case Opcodes.READONLY_BUFFER:
			load_readonly_buffer();
			break;
		case Opcodes.NEXT_BUFFER:
			load_next_buffer();
			break;


		default:
			throw new InvalidOpcodeException("invalid pickle opcode: " + key);
		}

		return NO_RETURN_VALUE;
	}


	void load_readonly_buffer() {
		// this opcode is ignored, we don't distinguish between readonly and read/write buffers
	}

	void load_next_buffer() throws PickleException, IOException {
		stack.add(next_buffer());
	}

	void load_bytearray8() throws IOException {
		// this is the same as load_binbytes8 because we make no distinction
		// here between the bytes and bytearray python types
		long len = PickleUtils.bytes_to_long(PickleUtils.readbytes(input, 8),0);
		stack.add(PickleUtils.readbytes(input, len));
	}

	void load_build() {
		Object args=stack.pop();
		Object target=stack.peek();
		try {
			Method setStateMethod=target.getClass().getMethod("__setstate__", args.getClass());
			setStateMethod.invoke(target, args);
		} catch (Exception e) {
			throw new PickleException("failed to __setstate__()",e);
		}
	}

	void load_proto() throws IOException {
		short proto = PickleUtils.readbyte(input);
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
		String data = PickleUtils.readline(input, true);
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
				// hmm, integer didn't work.. is it perhaps an int from a 64-bit python? so try long:
				val = Long.parseLong(number, 10);
			}
		}
		stack.add(val);
	}

	void load_binint() throws IOException {
		int integer = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		stack.add(integer);
	}

	void load_binint1() throws IOException {
		stack.add((int)PickleUtils.readbyte(input));
	}

	void load_binint2() throws IOException {
		int integer = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 2));
		stack.add(integer);
	}

	void load_long() throws IOException {
		String val = PickleUtils.readline(input);
		if (val != null && val.endsWith("L")) {
			val = val.substring(0, val.length() - 1);
		}
		BigInteger bi = new BigInteger(val);
		stack.add(PickleUtils.optimizeBigint(bi));
	}

	void load_long1() throws IOException {
		short n = PickleUtils.readbyte(input);
		byte[] data = PickleUtils.readbytes(input, n);
		stack.add(PickleUtils.decode_long(data));
	}

	void load_long4() throws IOException {
		int n = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		byte[] data = PickleUtils.readbytes(input, n);
		stack.add(PickleUtils.decode_long(data));
	}

	void load_float() throws IOException {
		String val = PickleUtils.readline(input, true);
		stack.add(Double.parseDouble(val));
	}

	void load_binfloat() throws IOException {
		double val = PickleUtils.bytes_to_double(PickleUtils.readbytes(input, 8),0);
		stack.add(val);
	}

	void load_string() throws IOException {
		String rep = PickleUtils.readline(input);
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

		stack.add(PickleUtils.decode_escaped(rep));
	}

	void load_binstring() throws IOException {
		int len = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(PickleUtils.rawStringFromBytes(data));
	}

	void load_binbytes() throws IOException {
		int len = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		stack.add(PickleUtils.readbytes(input, len));
	}

	void load_binbytes8() throws IOException {
		long len = PickleUtils.bytes_to_long(PickleUtils.readbytes(input, 8),0);
		stack.add(PickleUtils.readbytes(input, len));
	}

	void load_unicode() throws IOException {
		String str=PickleUtils.decode_unicode_escaped(PickleUtils.readline(input));
		stack.add(str);
	}

	void load_binunicode() throws IOException {
		int len = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(new String(data,"UTF-8"));
	}

	void load_binunicode8() throws IOException {
		long len = PickleUtils.bytes_to_long(PickleUtils.readbytes(input, 8),0);
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(new String(data,"UTF-8"));
	}

	void load_short_binunicode() throws IOException {
		int len = PickleUtils.readbyte(input);
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(new String(data,"UTF-8"));
	}

	void load_short_binstring() throws IOException {
		short len = PickleUtils.readbyte(input);
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(PickleUtils.rawStringFromBytes(data));
	}

	void load_short_binbytes() throws IOException {
		short len = PickleUtils.readbyte(input);
		stack.add(PickleUtils.readbytes(input, len));
	}

	void load_tuple() {
		List<Object> top = stack.pop_all_since_marker();
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

	void load_empty_set() {
		stack.add(new HashSet<Object>());
	}

	void load_list() {
		List<Object> top = stack.pop_all_since_marker();
		stack.add(top); // simply add the top items as a list to the stack again
	}

	void load_dict() {
		List<Object> top = stack.pop_all_since_marker();
		HashMap<Object, Object> map = new HashMap<Object, Object>(top.size());
		for (int i = 0; i < top.size(); i += 2) {
			Object key = top.get(i);
			Object value = top.get(i + 1);
			map.put(key, value);
		}
		stack.add(map);
	}

	void load_frozenset() {
		List<Object> top = stack.pop_all_since_marker();
		HashSet<Object> set = new HashSet<Object>();
		set.addAll(top);
		stack.add(set);
	}

	void load_additems() {
		List<Object> top = stack.pop_all_since_marker();
		@SuppressWarnings("unchecked")
		HashSet<Object> set = (HashSet<Object>) stack.pop();
		set.addAll(top);
		stack.add(set);
	}

	void load_global() throws IOException {
		String module = PickleUtils.readline(input);
		String name = PickleUtils.readline(input);
		load_global_sub(module, name);
	}

	void load_stack_global() {
		String name = (String) stack.pop();
		String module = (String) stack.pop();
		load_global_sub(module, name);
	}

	void load_global_sub(String module, String name) {
		IObjectConstructor constructor = objectConstructors.get(module + "." + name);
		if (constructor == null) {
			// check if it is an exception
			if(module.equals("exceptions")) {
				// python 2.x
				constructor=new ExceptionConstructor(PythonException.class, module, name);
			} else if(module.equals("builtins") || module.equals("__builtin__")) {
				if(name.endsWith("Error") || name.endsWith("Warning") || name.endsWith("Exception")
						|| name.equals("GeneratorExit") || name.equals("KeyboardInterrupt")
						|| name.equals("StopIteration") || name.equals("SystemExit"))
				{
					// it's a python 3.x exception
					constructor=new ExceptionConstructor(PythonException.class, module, name);
				}
				else
				{
					// return a dictionary with the class's properties
					constructor=new ClassDictConstructor(module, name);
				}
			} else {
				// return a dictionary with the class's properties
				constructor=new ClassDictConstructor(module, name);
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
		int i = Integer.parseInt(PickleUtils.readline(input), 10);
		if(!memo.containsKey(i)) throw new PickleException("invalid memo key");
		stack.add(memo.get(i));
	}

	void load_binget() throws IOException {
		int i = PickleUtils.readbyte(input);
		if(!memo.containsKey(i)) throw new PickleException("invalid memo key");
		stack.add(memo.get(i));
	}

	void load_long_binget() throws IOException {
		int i = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		if(!memo.containsKey(i)) throw new PickleException("invalid memo key");
		stack.add(memo.get(i));
	}

	void load_put() throws IOException {
		int i = Integer.parseInt(PickleUtils.readline(input), 10);
		memo.put(i, stack.peek());
	}

	void load_binput() throws IOException {
		int i = PickleUtils.readbyte(input);
		memo.put(i, stack.peek());
	}

	void load_long_binput() throws IOException {
		int i = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		memo.put(i, stack.peek());
	}

	void load_memoize() {
		memo.put(memo.size(), stack.peek());
	}

	void load_append() {
		Object value = stack.pop();
		@SuppressWarnings("unchecked")
		ArrayList<Object> list = (ArrayList<Object>) stack.peek();
		list.add(value);
	}

	void load_appends() {
		List<Object> top = stack.pop_all_since_marker();
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
		load_reduce(); // for Java we just do the same as class(*args) instead of class.__new__(class,*args)
	}

	void load_newobj_ex() {
		HashMap<?, ?> kwargs = (HashMap<?, ?>) stack.pop();
		Object[] args = (Object[]) stack.pop();
		IObjectConstructor constructor = (IObjectConstructor) stack.pop();
		if(kwargs.size()==0)
			stack.add(constructor.construct(args));
		else
			throw new PickleException("newobj_ex with keyword arguments not supported");
	}

	void load_frame() throws IOException {
		// for now we simply skip the frame opcode and its length
		PickleUtils.readbytes(input, 8);
	}

	void load_persid() throws IOException {
		// the persistent id is taken from the argument
		String pid = PickleUtils.readline(input);
		stack.add(persistentLoad(pid));
	}

	void load_binpersid() throws IOException {
		// the persistent id is taken from the stack
		String pid = stack.pop().toString();
		stack.add(persistentLoad(pid));
	}

	void load_obj() throws IOException {
		List<Object> args = stack.pop_all_since_marker();
		IObjectConstructor constructor = (IObjectConstructor)args.get(0);
		args = args.subList(1, args.size());
		Object object = constructor.construct(args.toArray());
		stack.add(object);
	}

	void load_inst() throws IOException {
		String module = PickleUtils.readline(input);
		String classname = PickleUtils.readline(input);
		List<Object> args = stack.pop_all_since_marker();
		IObjectConstructor constructor = objectConstructors.get(module + "." + classname);
		if (constructor == null) {
			constructor = new ClassDictConstructor(module, classname);
			args.clear();  // classdict doesn't have constructor args... so we may lose info here, hmm.
		}
		Object object = constructor.construct(args.toArray());
		stack.add(object);
	}

	/**
	 * Hook for the persistent id feature where an id is replaced externally by the appropriate object.
	 * @param pid the persistent id from the pickle
	 * @return the actual object that belongs to that id. The default implementation throws a PickleException,
	 *     telling you that you should implement this function yourself in a subclass of the Unpickler.
	 */
	protected Object persistentLoad(String pid)
	{
		throw new PickleException("A load persistent id instruction was encountered, but no persistentLoad function was specified. (implement it in custom Unpickler subclass)");
	}
}
