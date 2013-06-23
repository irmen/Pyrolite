/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Razorvine.Pickle
{
	
/// <summary>
/// Pickle an object graph into a Python-compatible pickle stream. For
/// simplicity, the only supported pickle protocol at this time is protocol 2. 
/// See README.txt for a table with the type mapping.
/// </summary>
public class Pickler : IDisposable {

	public static int HIGHEST_PROTOCOL = 2;

	private Stream outs;
	private int PROTOCOL = 2;
	private PickleUtils utils;
	private static IDictionary<Type, IObjectPickler> customPicklers = new Dictionary<Type, IObjectPickler>();
	private bool useMemo=true;
	
	/**
	 * Create a Pickler.
	 */
	public Pickler() {
	}

	/**
	 * Create a Pickler. Specify if it is to use a memo table or not.
	 */
	public Pickler(bool useMemo) {
		this.useMemo=useMemo;
	}
	
	/**
	 * Close the pickler stream, discard any internal buffers.
	 */
	public void close() {
		outs.Flush();
		outs.Close();
	}

	/**
	 * Register additional object picklers for custom classes.
	 */
	public static void registerCustomPickler(Type clazz, IObjectPickler pickler) {
		customPicklers[clazz]=pickler;
	}
	
	/**
	 * Pickle a given object graph, returning the result as a byte array.
	 */
	public byte[] dumps(object o) {
		MemoryStream bo = new MemoryStream();
		dump(o, bo);
		bo.Flush();
		return bo.ToArray();
	}

	/**
	 * Pickle a given object graph, writing the result to the output stream.
	 */
	public void dump(object o, Stream stream) {
		outs = stream;
		utils = new PickleUtils(null);
		outs.WriteByte(Opcodes.PROTO);
		outs.WriteByte((byte)PROTOCOL);
		save(o);
		outs.WriteByte(Opcodes.STOP);
		outs.Flush();
	}

	/**
	 * Pickle a single object and write its pickle representation to the output stream.
	 * Normally this is used internally by the pickler, but you can also utilize it from
	 * within custom picklers. This is handy if as part of the custom pickler, you need
	 * to write a couple of normal objects such as strings or ints, that are already
	 * supported by the pickler.
	 */
	public void save(object o) {
		// null type?
		if(o==null) {
			outs.WriteByte(Opcodes.NONE);
			return;
		}
		
		// check the dispatch table
		Type t=o.GetType();
		bool must_memo;
		if(dispatch(t, o, out must_memo)) {
			if(must_memo && this.useMemo) {
				// @todo: add to memo
			}
			return;
		}

		throw new PickleException("couldn't pickle object of type "+t);
	}

	/**
	 * Process a single object to be pickled.
	 */
	private bool dispatch(Type t, object o, out bool must_memo) {
		// is it a primitive array?
		must_memo=true;
		
		if(o is Array) {
			Type componentType=t.GetElementType();
			if(componentType.IsPrimitive) {
				put_arrayOfPrimitives(componentType, o);
			} else {
				put_arrayOfObjects((object[])o);
			}
			return true;
		}
		
		// first the primitive types
		if(o is bool) {
			put_bool((Boolean)o);
			must_memo=false;
			return true;
		}
		if(o is byte) {
			put_long((byte)o);
			must_memo=false;
			return true;
		}
		if(o is sbyte) {
			put_long((sbyte)o);
			must_memo=false;
			return true;
		}
		if(o is short) {
			put_long((short)o);
			must_memo=false;
			return true;
		}
		if(o is ushort) {
			put_long((ushort)o);
			must_memo=false;
			return true;
		}
		if(o is int) {
			put_long((int)o);
			must_memo=false;
			return true;
		}
		if(o is uint) {
			put_long((uint)o);
			must_memo=false;
			return true;
		}
		if(o is long) {
			put_long((long)o);
			must_memo=false;
			return true;
		}
		if(o is ulong) {
			put_ulong((ulong)o);
			must_memo=false;
			return true;
		}
		if(o is float) {
			put_float((float)o);
			must_memo=false;
			return true;
		}
		if(o is double) {
			put_float((double)o);
			must_memo=false;
			return true;
		}
		if(o is char) {
			put_string(""+o);
			must_memo=false;
			return true;
		}
		
		// check registry
		if(customPicklers.ContainsKey(t)) {
			IObjectPickler custompickler=customPicklers[t];
			custompickler.pickle(o, this.outs, this);
			return true;
		}
		
		// more complex types
		if(o is string) {
			put_string((String)o);
			return true;
		}
		if(o is decimal) {
			put_decimal((decimal)o);
			return true;
		}
		if(o is DateTime) {
			put_datetime((DateTime)o);
			return true;
		}
		if(o is TimeSpan) {
			put_timespan((TimeSpan)o);
			return true;
		}
		if(t.IsGenericType && t.GetGenericTypeDefinition()==typeof(HashSet<>)) {
			put_set((IEnumerable)o);
			return true;
		}
		if(o is IDictionary) {
			put_map((IDictionary)o);
			return true;
		}
		if(o is IList) {
			put_enumerable((IList)o);
			return true;
		}
		if(o is IEnumerable) {
			put_enumerable((IEnumerable)o);
			return true;
		}
		if(o is Enum) {
			put_string(o.ToString());
			return true;
		}
		if(hasPublicProperties(o)) {
			put_objwithproperties(o);
			return true;
		}
		must_memo=false;
		return false;
	}
	
	bool hasPublicProperties(object o)
	{
		PropertyInfo[] props=o.GetType().GetProperties();
		return props.Length>0;
	}
		
	void put_datetime(DateTime dt) {
		outs.WriteByte(Opcodes.GLOBAL);
		byte[] bytes=Encoding.Default.GetBytes("datetime\ndatetime\n");
		outs.Write(bytes,0,bytes.Length);
		outs.WriteByte(Opcodes.MARK);
		save(dt.Year);
		save(dt.Month);
		save(dt.Day);
		save(dt.Hour);
		save(dt.Minute);
		save(dt.Second);
		save(dt.Millisecond*1000);
		outs.WriteByte(Opcodes.TUPLE);
		outs.WriteByte(Opcodes.REDUCE);
	}
		
	void put_timespan(TimeSpan ts) {
		outs.WriteByte(Opcodes.GLOBAL);
		byte[] bytes=Encoding.Default.GetBytes("datetime\ntimedelta\n");
		outs.Write(bytes,0,bytes.Length);
		save(ts.Days);
		save(ts.Hours*3600+ts.Minutes*60+ts.Seconds);
		save(ts.Milliseconds*1000);
		outs.WriteByte(Opcodes.TUPLE3);
		outs.WriteByte(Opcodes.REDUCE);	
	}

	void put_enumerable(IEnumerable list) {
		outs.WriteByte(Opcodes.EMPTY_LIST);
		outs.WriteByte(Opcodes.MARK);
		foreach(var o in list) {
			save(o);
		}
		outs.WriteByte(Opcodes.APPENDS);
	}

	void put_map(IDictionary o) {
		outs.WriteByte(Opcodes.EMPTY_DICT);
		outs.WriteByte(Opcodes.MARK);
		foreach(var k in o.Keys) {
			save(k);
			save(o[k]);
		}
		outs.WriteByte(Opcodes.SETITEMS);
	}

	void put_set(IEnumerable o) {
		outs.WriteByte(Opcodes.GLOBAL);
		byte[] output=Encoding.ASCII.GetBytes("__builtin__\nset\n");
		outs.Write(output,0,output.Length);
		outs.WriteByte(Opcodes.EMPTY_LIST);
		outs.WriteByte(Opcodes.MARK);
		foreach(object x in o) {
			save(x);
		}
		outs.WriteByte(Opcodes.APPENDS);
		outs.WriteByte(Opcodes.TUPLE1);
		outs.WriteByte(Opcodes.REDUCE);
	}

	void put_arrayOfObjects(object[] array) {
		// 0 objects->EMPTYTUPLE
		// 1 object->TUPLE1
		// 2 objects->TUPLE2
		// 3 objects->TUPLE3
		// 4 or more->MARK+items+TUPLE
		if(array.Length==0) {
			outs.WriteByte(Opcodes.EMPTY_TUPLE);
		} else if(array.Length==1) {
			save(array[0]);
			outs.WriteByte(Opcodes.TUPLE1);
		} else if(array.Length==2) {
			save(array[0]);
			save(array[1]);
			outs.WriteByte(Opcodes.TUPLE2);
		} else if(array.Length==3) {
			save(array[0]);
			save(array[1]);
			save(array[2]);
			outs.WriteByte(Opcodes.TUPLE3);
		} else {
			outs.WriteByte(Opcodes.MARK);
			foreach(object o in array) {
				save(o);
			}
			outs.WriteByte(Opcodes.TUPLE);
		}
	}

	void put_arrayOfPrimitives(Type t, object array) {
			
		byte[] output;

		if(t==typeof(bool)) {
			// a bool[] isn't written as an array but rather as a tuple
			bool[] source=(bool[])array;
			// this is stupid, but seems to be necessary because you can't cast a bool[] to an object[]
			object[] boolarray=new object[source.Length];
			Array.Copy(source, boolarray, source.Length);
			put_arrayOfObjects(boolarray);
			return;
		}
		if(t==typeof(char)) {
			// a char[] isn't written as an array but rather as a unicode string
			String s=new String((char[])array);
			put_string(s);
			return;
		}		
		if(t==typeof(byte)) {
			// a byte[] isn't written as an array but rather as a bytearray object
			outs.WriteByte(Opcodes.GLOBAL);
			output=Encoding.ASCII.GetBytes("__builtin__\nbytearray\n");
			outs.Write(output,0,output.Length);
			string str=PickleUtils.rawStringFromBytes((byte[])array);
			put_string(str);
			put_string("latin-1");	// this is what python writes in the pickle
			outs.WriteByte(Opcodes.TUPLE2);
			outs.WriteByte(Opcodes.REDUCE);
			return;
		} 
		
		outs.WriteByte(Opcodes.GLOBAL);
		output=Encoding.ASCII.GetBytes("array\narray\n");
		outs.Write(output,0,output.Length);
		outs.WriteByte(Opcodes.SHORT_BINSTRING);		// array typecode follows
		outs.WriteByte(1); // typecode is 1 char
		
		if(t==typeof(sbyte)) {
			outs.WriteByte((byte)'b'); // signed char
			outs.WriteByte(Opcodes.EMPTY_LIST);
			outs.WriteByte(Opcodes.MARK);
			foreach(sbyte s in (sbyte[])array) {
				save(s);
			}
		} else if(t==typeof(short)) {
			outs.WriteByte((byte)'h'); // signed short
			outs.WriteByte(Opcodes.EMPTY_LIST);
			outs.WriteByte(Opcodes.MARK);
			foreach(short s in (short[])array) {
				save(s);
			}
		} else if(t==typeof(ushort)) {
			outs.WriteByte((byte)'H'); // unsigned short
			outs.WriteByte(Opcodes.EMPTY_LIST);
			outs.WriteByte(Opcodes.MARK);
			foreach(ushort s in (ushort[])array) {
				save(s);
			}
		} else if(t==typeof(int)) {
			outs.WriteByte((byte)'i'); // signed int
			outs.WriteByte(Opcodes.EMPTY_LIST);
			outs.WriteByte(Opcodes.MARK);
			foreach(int i in (int[])array) {
				save(i);
			}
		} else if(t==typeof(uint)) {
			outs.WriteByte((byte)'I'); // unsigned int
			outs.WriteByte(Opcodes.EMPTY_LIST);
			outs.WriteByte(Opcodes.MARK);
			foreach(uint i in (uint[])array) {
				save(i);
			}
		} else if(t==typeof(long)) {
			outs.WriteByte((byte)'l');  // signed long
			outs.WriteByte(Opcodes.EMPTY_LIST);
			outs.WriteByte(Opcodes.MARK);
			foreach(long v in (long[])array) {
				save(v);
			}
		} else if(t==typeof(ulong)) {
			outs.WriteByte((byte)'L');  // unsigned long
			outs.WriteByte(Opcodes.EMPTY_LIST);
			outs.WriteByte(Opcodes.MARK);
			foreach(ulong v in (ulong[])array) {
				save(v);
			}
		} else if(t==typeof(float)) {
			outs.WriteByte((byte)'f');  // float
			outs.WriteByte(Opcodes.EMPTY_LIST);
			outs.WriteByte(Opcodes.MARK);
			foreach(float f in (float[])array) {
				save(f);
			}
		} else if(t==typeof(double)) {
			outs.WriteByte((byte)'d');  // double
			outs.WriteByte(Opcodes.EMPTY_LIST);
			outs.WriteByte(Opcodes.MARK);
			foreach(double d in (double[])array) {
				save(d);
			}
		} 
		
		outs.WriteByte(Opcodes.APPENDS);
		outs.WriteByte(Opcodes.TUPLE2);
		outs.WriteByte(Opcodes.REDUCE);
	}

	void put_decimal(decimal d) {
		//"cdecimal\nDecimal\nU\n12345.6789\u0085R."
		outs.WriteByte(Opcodes.GLOBAL);
		byte[] output=Encoding.ASCII.GetBytes("decimal\nDecimal\n");
		outs.Write(output,0,output.Length);
		put_string(Convert.ToString(d, CultureInfo.InvariantCulture));
		outs.WriteByte(Opcodes.TUPLE1);
		outs.WriteByte(Opcodes.REDUCE);
	}

	void put_string(string str) {
		byte[] encoded=Encoding.UTF8.GetBytes(str);
		outs.WriteByte(Opcodes.BINUNICODE);
		byte[] output=utils.integer_to_bytes(encoded.Length);
		outs.Write(output,0,output.Length);
		outs.Write(encoded,0,encoded.Length);
	}

	void put_float(double d) {
		outs.WriteByte(Opcodes.BINFLOAT);
		byte[] output=utils.double_to_bytes(d);
		outs.Write(output,0,output.Length);
	}	

	void put_long(long v) {
		byte[] output;
		// choose optimal representation
		// first check 1 and 2-byte unsigned ints:
		if(v>=0) {
			if(v<=0xff) {
				outs.WriteByte(Opcodes.BININT1);
				outs.WriteByte((byte)v);
				return;
			}
			if(v<=0xffff) {
				outs.WriteByte(Opcodes.BININT2);
				outs.WriteByte((byte)(v&0xff));
				outs.WriteByte((byte)(v>>8));
				return;
			}
		}
		
		// 4-byte signed int?
		long high_bits=v>>31;  // shift sign extends
		if(high_bits==0 || high_bits==-1) {
			// All high bits are copies of bit 2**31, so the value fits in a 4-byte signed int.
			outs.WriteByte(Opcodes.BININT);
			output=utils.integer_to_bytes((int)v);
			outs.Write(output,0,output.Length);
			return;
		}
		
		// int too big, store it as text
		outs.WriteByte(Opcodes.INT);
		output=Encoding.ASCII.GetBytes(""+v);
		outs.Write(output, 0, output.Length);
		outs.WriteByte((byte)'\n');
	}
	
	void put_ulong(ulong u) {
		if(u<=long.MaxValue) {
			long l=(long)u;
			put_long(l);
		} else {
			// ulong too big for a signed long, store it as text instead.
			outs.WriteByte(Opcodes.INT);
			byte[] output=Encoding.ASCII.GetBytes(u.ToString());
			outs.Write(output, 0, output.Length);
			outs.WriteByte((byte)'\n');
		}
	}
	
	void put_bool(bool b) {
		if(b)
			outs.WriteByte(Opcodes.NEWTRUE);
		else
			outs.WriteByte(Opcodes.NEWFALSE);
	}

	void put_objwithproperties(object o) {
		PropertyInfo[] properties=o.GetType().GetProperties();
		var map=new Dictionary<string, object>();
		foreach(var propinfo in properties) {
			if(propinfo.CanRead) {
				string name=propinfo.Name;
				try {
					map[name]=propinfo.GetValue(o, null);
				} catch (Exception x) {
					throw new PickleException("cannot pickle ISerializable:",x);
				}
			}
		}
		
		// if we're dealing with an anonymous type, don't output the type name.
		if(!o.GetType().Name.StartsWith("<>"))
			map["__class__"]=o.GetType().FullName;

		save(map);
	}
	
	public void Dispose()
	{
		this.close();
	}
}

}
