/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Razorvine.Pickle.Objects;

namespace Razorvine.Pickle
{

/// <summary>
/// Unpickles an object graph from a pickle data inputstream. Supports all pickle protocol versions.
/// Maps the python objects on the corresponding java equivalents or similar types.
/// This class is NOT threadsafe! (Don't use the same unpickler from different threads)
/// See the README.txt for a table with the type mappings.
/// </summary>
public class Unpickler : IDisposable {

	private const int HIGHEST_PROTOCOL = 4;

	private IDictionary<int, object> memo;
	protected UnpickleStack stack;
	private Stream input;
	private static IDictionary<string, IObjectConstructor> objectConstructors;

	static Unpickler() {
		objectConstructors = new Dictionary<string, IObjectConstructor>();
		objectConstructors["__builtin__.complex"] = new AnyClassConstructor(typeof(ComplexNumber));
		objectConstructors["builtins.complex"] = new AnyClassConstructor(typeof(ComplexNumber));
		objectConstructors["array.array"] = new ArrayConstructor();
		objectConstructors["array._array_reconstructor"] = new ArrayConstructor();
		objectConstructors["__builtin__.bytearray"] = new ByteArrayConstructor();
		objectConstructors["builtins.bytearray"] =new ByteArrayConstructor();
		objectConstructors["__builtin__.bytes"] = new ByteArrayConstructor();
		objectConstructors["__builtin__.set"] = new SetConstructor();
		objectConstructors["builtins.set"] = new SetConstructor();
		objectConstructors["datetime.datetime"] = new DateTimeConstructor(DateTimeConstructor.PythonType.DATETIME);
		objectConstructors["datetime.time"] = new DateTimeConstructor(DateTimeConstructor.PythonType.TIME);
		objectConstructors["datetime.date"] = new DateTimeConstructor(DateTimeConstructor.PythonType.DATE);
		objectConstructors["datetime.timedelta"] = new DateTimeConstructor(DateTimeConstructor.PythonType.TIMEDELTA);
		objectConstructors["decimal.Decimal"] = new DecimalConstructor();
	}

	/**
	 * Create an unpickler.
	 */
	public Unpickler() {
		memo = new Dictionary<int, object>();
	}

	/**
	 * Register additional object constructors for custom classes.
	 */
	public static void registerConstructor(string module, string classname, IObjectConstructor constructor) {
		objectConstructors[module + "." + classname]=constructor;
	}

	/**
	 * Read a pickled object representation from the given input stream.
	 * 
	 * @return the reconstituted object hierarchy specified in the file.
	 */
	public object load(Stream stream) {
		input = stream;
		stack = new UnpickleStack();
		try {
			while (true) {
				byte key = PickleUtils.readbyte(input);
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
	public object loads(byte[] pickledata) {
		return load(new MemoryStream(pickledata));
	}

	/**
	 * Close the unpickler and frees the resources such as the unpickle stack and memo table.
	 */
	public void close() {
		if(stack!=null)	stack.clear();
		if(memo!=null) memo.Clear();
		if(input!=null) input.Close();
	}

	private class StopException : Exception {

		public StopException(object value) {
			this.value = value;
		}

		public object value;
	}

	/**
	 * Process a single pickle stream opcode.
	 */
	protected void dispatch(short key) {
		switch (key) {
		case Opcodes.MARK:
			load_mark();
			break;
		case Opcodes.STOP:
			object value = stack.pop();
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
			throw new InvalidOpcodeException("opcode not implemented: PERSID");
		case Opcodes.BINPERSID:
			throw new InvalidOpcodeException("opcode not implemented: BINPERSID");
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
			throw new InvalidOpcodeException("opcode not implemented: INST");
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
			throw new InvalidOpcodeException("opcode not implemented: OBJ");
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
			throw new InvalidOpcodeException("opcode not implemented: EXT1");
		case Opcodes.EXT2:
			throw new InvalidOpcodeException("opcode not implemented: EXT2");
		case Opcodes.EXT4:
			throw new InvalidOpcodeException("opcode not implemented: EXT4");
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
			
		// Protocol 4 (Python 3.4+)
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

		default:
			throw new InvalidOpcodeException("invalid pickle opcode: " + key);
		}
	}

	void load_build() {
		object args=stack.pop();
		object target=stack.peek();
		object[] arguments=new object[] {args};
		Type[] argumentTypes=new Type[] {args.GetType()};
		
		// call the __setstate__ method with the given arguments
		try {
			MethodInfo setStateMethod=target.GetType().GetMethod("__setstate__", argumentTypes);
			if(setStateMethod==null) {
				throw new PickleException(string.Format("no __setstate__() found in type {0} with argument type {1}", target.GetType(), args.GetType()));
			}
			setStateMethod.Invoke(target, arguments);
		} catch(Exception e) {
			throw new PickleException("failed to __setstate__()",e);
		}
	}

	void load_proto() {
		byte proto = PickleUtils.readbyte(input);
		if (proto > HIGHEST_PROTOCOL)
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

	void load_int() {
		string data = PickleUtils.readline(input, true);
		object val;
		if (data==Opcodes.FALSE.Substring(1))
			val = false;
		else if (data==Opcodes.TRUE.Substring(1))
			val = true;
		else {
			string number=data.Substring(0, data.Length - 1);
			try {
				val=int.Parse(number);
			} catch (OverflowException) {
				// hmm, integer didnt' work.. is it perhaps an int from a 64-bit python? so try long:
				val = long.Parse(number);
			}
		}
		stack.add(val);
	}

	void load_binint()  {
		int integer = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		stack.add(integer);
	}

	void load_binint1() {
		stack.add((int)PickleUtils.readbyte(input));
	}

	void load_binint2() {
		int integer = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 2));
		stack.add(integer);
	}

	void load_long() {
		string val = PickleUtils.readline(input);
		if (val.EndsWith("L")) {
			val = val.Substring(0, val.Length - 1);
		}
		long longvalue;
		if(long.TryParse(val, out longvalue)) {
			stack.add(longvalue);
		} else {
			throw new PickleException("long too large in load_long (need BigInt)");
		}
	}

	void load_long1() {
		byte n = PickleUtils.readbyte(input);
		byte[] data = PickleUtils.readbytes(input, n);
		stack.add(PickleUtils.decode_long(data));
	}

	void load_long4() {
		int n = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		byte[] data = PickleUtils.readbytes(input, n);
		stack.add(PickleUtils.decode_long(data));
	}

	void load_float() {
		string val = PickleUtils.readline(input, true);
		double d=double.Parse(val, NumberStyles.Float|NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);
		stack.add(d);
	}

	void load_binfloat() {
		double val = PickleUtils.bytes_bigendian_to_double(PickleUtils.readbytes(input, 8),0);
		stack.add(val);
	}

	void load_string() {
		string rep = PickleUtils.readline(input);
		bool quotesOk = false;
		foreach (string q in new string[] { "\"", "'" }) // double or single quote
		{
			if (rep.StartsWith(q)) {
				if (!rep.EndsWith(q)) {
					throw new PickleException("insecure string pickle");
				}
				rep = rep.Substring(1, rep.Length - 2); // strip quotes
				quotesOk = true;
				break;
			}
		}

		if (!quotesOk)
			throw new PickleException("insecure string pickle");

		stack.add(PickleUtils.decode_escaped(rep));
	}

	void load_binstring() {
		int len = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(PickleUtils.rawStringFromBytes(data));
	}

	void load_binbytes() {
		int len = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		stack.add(PickleUtils.readbytes(input, len));
	}

	void load_binbytes8() {
		long len = PickleUtils.bytes_to_long(PickleUtils.readbytes(input, 8),0);
		stack.add(PickleUtils.readbytes(input, len));
	}

	void load_unicode() {
		string str=PickleUtils.decode_unicode_escaped(PickleUtils.readline(input));
		stack.add(str);
	}

	void load_binunicode() {
		int len = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(Encoding.UTF8.GetString(data));
	}

	void load_binunicode8() {
		long len = PickleUtils.bytes_to_long(PickleUtils.readbytes(input, 8),0);
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(Encoding.UTF8.GetString(data));
	}

	void load_short_binunicode() {
		int len = PickleUtils.readbyte(input);
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(Encoding.UTF8.GetString(data));
	}

	void load_short_binstring() {
		byte len = PickleUtils.readbyte(input);
		byte[] data = PickleUtils.readbytes(input, len);
		stack.add(PickleUtils.rawStringFromBytes(data));
	}

	void load_short_binbytes() {
		byte len = PickleUtils.readbyte(input);
		stack.add(PickleUtils.readbytes(input, len));
	}

	void load_tuple() {
		ArrayList top=stack.pop_all_since_marker();
		stack.add(top.ToArray());
	}

	void load_empty_tuple() {
		stack.add(new object[0]);
	}

	void load_tuple1() {
		stack.add(new object[] { stack.pop() });
	}

	void load_tuple2() {
		object o2 = stack.pop();
		object o1 = stack.pop();
		stack.add(new object[] { o1, o2 });
	}

	void load_tuple3() {
		object o3 = stack.pop();
		object o2 = stack.pop();
		object o1 = stack.pop();
		stack.add(new object[] { o1, o2, o3 });
	}

	void load_empty_list() {
		stack.add(new ArrayList(5));
	}

	void load_empty_dictionary() {
		stack.add(new Hashtable(5));
	}

	void load_empty_set() {
		stack.add(new HashSet<object>());
	}

	void load_list() {
		ArrayList top = stack.pop_all_since_marker();
		stack.add(top); // simply add the top items as a list to the stack again
	}

	void load_dict() {
		ArrayList top = stack.pop_all_since_marker();
		Hashtable map=new Hashtable(top.Count);
		for (int i = 0; i < top.Count; i += 2) {
			object key = top[i];
			object value = top[i+1];
			map[key]=value;
		}
		stack.add(map);
	}
	
	void load_frozenset() {
		ArrayList top = stack.pop_all_since_marker();
		var set = new HashSet<object>();
		foreach(var element in top)
			set.Add(element);
		stack.add(set);
	}
	
	void load_additems() {
		ArrayList top = stack.pop_all_since_marker();
		var set = (HashSet<object>) stack.pop();
		foreach(object item in top)
			set.Add(item);
		stack.add(set);
	}

	void load_global() {
		string module = PickleUtils.readline(input);
		string name = PickleUtils.readline(input);
		load_global_sub(module, name);
	}
	
	void load_stack_global() {
		string name = (string) stack.pop();
		string module = (string) stack.pop();
		load_global_sub(module, name);
	}
	
	void load_global_sub(string module, string name) {
		IObjectConstructor constructor;
		string key=module+"."+name;
		if(objectConstructors.ContainsKey(key)) {
			 constructor = objectConstructors[module + "." + name];
		} else {
			// check if it is an exception
			if(module=="exceptions") {
				// python 2.x
				constructor=new AnyClassConstructor(typeof(PythonException));
			} else if(module=="builtins" || module=="__builtin__") {
				if(name.EndsWith("Error") || name.EndsWith("Warning") || name.EndsWith("Exception")
						|| name=="GeneratorExit" || name=="KeyboardInterrupt"
						|| name=="StopIteration" || name=="SystemExit")
				{
					// it's a python 3.x exception
					constructor=new AnyClassConstructor(typeof(PythonException));
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
		object o = null;
		do {
			o = stack.pop();
		} while (o != stack.MARKER);
		stack.trim();
	}

	void load_dup() {
		stack.add(stack.peek());
	}

	void load_get() {
		int i = int.Parse(PickleUtils.readline(input));
		if(!memo.ContainsKey(i)) throw new PickleException("invalid memo key");
		stack.add(memo[i]);
	}

	void load_binget() {
		byte i = PickleUtils.readbyte(input);
		if(!memo.ContainsKey(i)) throw new PickleException("invalid memo key");
		stack.add(memo[(int)i]);
	}

	void load_long_binget() {
		int i = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		if(!memo.ContainsKey(i)) throw new PickleException("invalid memo key");
		stack.add(memo[i]);
	}

	void load_put() {
		int i = int.Parse(PickleUtils.readline(input));
		memo[i]=stack.peek();
	}

	void load_binput() {
		byte i = PickleUtils.readbyte(input);
		memo[(int)i]=stack.peek();
	}

	void load_memoize() {
		memo[memo.Count]=stack.peek();
	}

	void load_long_binput() {
		int i = PickleUtils.bytes_to_integer(PickleUtils.readbytes(input, 4));
		memo[i]=stack.peek();
	}

	void load_append() {
		object value = stack.pop();
		ArrayList list = (ArrayList) stack.peek();
		list.Add(value);
	}

	void load_appends() {
		ArrayList top = stack.pop_all_since_marker();
		ArrayList list = (ArrayList) stack.peek();
		list.AddRange(top);
		list.TrimToSize();
	}

	void load_setitem() {
		object value = stack.pop();
		object key = stack.pop();
		Hashtable dict=(Hashtable)stack.peek();
		dict[key]=value;
	}

	void load_setitems() {
		var newitems=new List<KeyValuePair<object,object>>();
		object value = stack.pop();
		while (value != stack.MARKER) {
			object key = stack.pop();
			newitems.Add(new KeyValuePair<object,object>(key,value));
			value = stack.pop();
		}
		
		Hashtable dict=(Hashtable)stack.peek();
		foreach(var item in newitems) {
			dict[item.Key]=item.Value;
		}
	}

	void load_mark() {
		stack.add_mark();
	}

	void load_reduce() {
		object[] args = (object[]) stack.pop();
		IObjectConstructor constructor = (IObjectConstructor) stack.pop();
		stack.add(constructor.construct(args));
	}

	void load_newobj() {
		load_reduce(); // we just do the same as class(*args) instead of class.__new__(class,*args)
	}
	
	void load_newobj_ex() {
		Hashtable kwargs = (Hashtable) stack.pop();
		object[] args = (object[]) stack.pop();
		IObjectConstructor constructor = (IObjectConstructor) stack.pop();
		if(kwargs.Count==0)
			stack.add(constructor.construct(args));
		else
			throw new PickleException("newobj_ex with keyword arguments not supported");
	}
	
	void load_frame() {
		// for now we simply skip the frame opcode and its length
		PickleUtils.readbytes(input, 8);
	}
	
	public void Dispose()
	{
		this.close();
	}
}

}
