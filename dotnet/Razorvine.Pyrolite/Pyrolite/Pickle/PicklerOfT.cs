using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Razorvine.Pickle
{
    internal interface IPickler : IDisposable
    {
        int BytesWritten { get; }

        byte[] GetByteArray();

        void dump(object o);
        void save(object o);
    }

    internal class Pickler<T> : IPickler
        where T : struct, IOutputWriter
    {
        private static readonly byte[] datetimeDatetimeBytes = Encoding.ASCII.GetBytes("datetime\ndatetime\n");
        private static readonly byte[] datetimeTimedeltaBytes = Encoding.ASCII.GetBytes("datetime\ntimedelta\n");
        private static readonly byte[] builtinSetBytes = Encoding.ASCII.GetBytes("__builtin__\nset\n");
        private static readonly byte[] builtinBytearrayBytes = Encoding.ASCII.GetBytes("__builtin__\nbytearray\n");
        private static readonly byte[] arrayArrayBytes = Encoding.ASCII.GetBytes("array\narray\n");
        private static readonly byte[] decimalDecimalBytes = Encoding.ASCII.GetBytes("decimal\nDecimal\n");
        
        private readonly Pickler pickler;

        private T output; // must NOT be readonly, it's a mutable struct
        private Dictionary<object, int> memo; // maps objects to memo index
        private int recurse;	// recursion level

        internal Pickler(T output, Pickler pickler, bool useMemo)
        {
            this.output = output;
            this.pickler = pickler;
            memo = useMemo ? new Dictionary<object, int>() : null;
            recurse = 0;
        }

        void IDisposable.Dispose() => output.Dispose();

        private bool useMemo => memo != null;

        public int BytesWritten
            => output is ArrayWriter arrayWriter
                ? arrayWriter.Position
                : throw new InvalidOperationException();

        public byte[] GetByteArray()
            => output is ArrayWriter arrayWriter
                ? arrayWriter.Output
                : throw new InvalidOperationException();

        public void dump(object o)
        {
            recurse = 0;
            output.WriteBytes(Opcodes.PROTO, Pickler.PROTOCOL);

            save(o);

            memo = null;  // get rid of the memo table

            output.WriteByte(Opcodes.STOP);
            if (recurse != 0)  // sanity check
                throw new PickleException("recursive structure error, please report this problem");
        }

        public void save(object o)
        {
            recurse++;
            if (recurse > Pickler.MAX_RECURSE_DEPTH)
                throw new StackOverflowException("recursion too deep in Pickler.save (>" + Pickler.MAX_RECURSE_DEPTH + ")");

            // null type?
            if (o == null)
            {
                output.WriteByte(Opcodes.NONE);
                recurse--;
                return;
            }

            Type t = o.GetType();
            // check the memo table, otherwise simply dispatch
            if ((useMemo && LookupMemo(t, o)) || dispatch(t, o))
            {
                recurse--;
                return;
            }

            throw new PickleException("couldn't pickle object of type " + t);
        }

        /**
         * Write the object to the memo table and output a memo write opcode
         * Only works for hashable objects
        */
        private void WriteMemo<TValue>(TValue value)
        {
            if (useMemo)
                WriteMemoPrivate(value);

            void WriteMemoPrivate(object obj)
            {
                if (!memo.ContainsKey(obj))
                {
                    int memo_index = memo.Count;
                    memo[obj] = memo_index;
                    if (memo_index <= 0xFF)
                    {
                        output.WriteBytes(Opcodes.BINPUT, (byte)memo_index);
                    }
                    else
                    {
                        output.WriteByte(Opcodes.LONG_BINPUT);
                        output.WriteInt32LittleEndian(memo_index);
                    }
                }
            }
        }

        /**
         * Check the memo table and output a memo lookup if the object is found
        */
        private bool LookupMemo(Type objectType, object obj)
        {
            Debug.Assert(useMemo);

            if (objectType.IsPrimitive || !memo.TryGetValue(obj, out int memo_index))
                return false;

            if (memo_index <= 0xff)
            {
                output.WriteBytes(Opcodes.BINGET, (byte)memo_index);
            }
            else
            {
                output.WriteByte(Opcodes.LONG_BINGET);
                output.WriteInt32LittleEndian(memo_index);
            }
            return true;
        }

        /**
            * Process a single object to be pickled.
        */
        private bool dispatch(Type t, object o)
        {
            Debug.Assert(t != null);
            Debug.Assert(o != null);
            Debug.Assert(t == o.GetType());

            // is it a primitive array?
            if (o is Array)
            {
                Type componentType = t.GetElementType();
                if (componentType != null && componentType.IsPrimitive)
                {
                    put_arrayOfPrimitives(componentType, o);
                }
                else
                {
                    put_arrayOfObjects((object[])o);
                }
                return true;
            }

            // first check for enums, as GetTypeCode will return the underlying type.
            if (o is Enum)
            {
                put_string(o.ToString());
                return true;
            }

            // first the primitive types
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Boolean:
                    put_bool((bool)o);
                    return true;
                case TypeCode.Byte:
                    put_byte((byte)o);
                    return true;
                case TypeCode.SByte:
                    put_long((sbyte)o);
                    return true;
                case TypeCode.Int16:
                    put_long((short)o);
                    return true;
                case TypeCode.UInt16:
                    put_long((ushort)o);
                    return true;
                case TypeCode.Int32:
                    put_long((int)o);
                    return true;
                case TypeCode.UInt32:
                    put_long((uint)o);
                    return true;
                case TypeCode.Int64:
                    put_long((long)o);
                    return true;
                case TypeCode.UInt64:
                    put_ulong((ulong)o);
                    return true;
                case TypeCode.Single:
                    put_float((float)o);
                    return true;
                case TypeCode.Double:
                    put_float((double)o);
                    return true;
                case TypeCode.Char:
                    put_string(((char)o).ToString());
                    return true;
                case TypeCode.String:
                    put_string((string)o);
                    return true;
                case TypeCode.Decimal:
                    put_decimal((decimal)o);
                    return true;
                case TypeCode.DateTime:
                    put_datetime((DateTime)o);
                    return true;
            }

            // check registry
            IObjectPickler custompickler = pickler.getCustomPickler(t);
            if (custompickler != null)
            {
                if (output is StreamWriter streamWriter)
                {
                    custompickler.pickle(o, streamWriter.Stream, pickler);
                }
                else if (output is ArrayWriter arrayWriter)
                {
                    throw new NotSupportedException("todo adsitnik");
                }
                WriteMemo(o);
                return true;
            }

            // more complex types
            if (o is TimeSpan)
            {
                put_timespan((TimeSpan)o);
                return true;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                put_set((IEnumerable)o);
                return true;
            }

            var dictionary = o as IDictionary;
            if (dictionary != null)
            {
                put_map(dictionary);
                return true;
            }

            var list = o as IList;
            if (list != null)
            {
                put_enumerable(list);
                return true;
            }

            var enumerable = o as IEnumerable;
            if (enumerable != null)
            {
                put_enumerable(enumerable);
                return true;
            }

            DataContractAttribute dca = (DataContractAttribute)Attribute.GetCustomAttribute(t, typeof(DataContractAttribute));
            if (dca != null)
            {
                put_datacontract(t, o, dca);
                return true;
            }

            SerializableAttribute sa = (SerializableAttribute)Attribute.GetCustomAttribute(t, typeof(SerializableAttribute));
            if (sa != null)
            {
                put_serializable(t, o);
                return true;
            }

            if (hasPublicProperties(o))
            {
                put_objwithproperties(o);
                return true;
            }

            return false;
        }

        private static bool hasPublicProperties(object o) => o.GetType().GetProperties().Length > 0;

        private void put_datetime(DateTime dt)
        {
            output.WriteByte(Opcodes.GLOBAL);
            output.Write(datetimeDatetimeBytes, 0, datetimeDatetimeBytes.Length);
            output.WriteByte(Opcodes.MARK);
            put_long(dt.Year);
            put_long(dt.Month);
            put_long(dt.Day);
            put_long(dt.Hour);
            put_long(dt.Minute);
            put_long(dt.Second);
            put_long(dt.Millisecond * 1000);
            output.WriteBytes(Opcodes.TUPLE, Opcodes.REDUCE);
            WriteMemo(dt);
        }

        private void put_timespan(TimeSpan ts)
        {
            output.WriteByte(Opcodes.GLOBAL);
            output.Write(datetimeTimedeltaBytes, 0, datetimeTimedeltaBytes.Length);
            put_long(ts.Days);
            put_long(ts.Hours * 3600 + ts.Minutes * 60 + ts.Seconds);
            put_long(ts.Milliseconds * 1000);
            output.WriteBytes(Opcodes.TUPLE3, Opcodes.REDUCE);
            WriteMemo(ts);
        }

        private void put_enumerable(IEnumerable list)
        {
            output.WriteByte(Opcodes.EMPTY_LIST);
            WriteMemo(list);
            output.WriteByte(Opcodes.MARK);
            foreach (var o in list)
            {
                save(o);
            }
            output.WriteByte(Opcodes.APPENDS);
        }

        private void put_map(IDictionary o)
        {
            output.WriteByte(Opcodes.EMPTY_DICT);
            WriteMemo(o);
            output.WriteByte(Opcodes.MARK);
            foreach (var k in o.Keys)
            {
                save(k);
                save(o[k]);
            }
            output.WriteByte(Opcodes.SETITEMS);
        }

        private void put_set(IEnumerable o)
        {
            output.WriteByte(Opcodes.GLOBAL);
            output.Write(builtinSetBytes, 0, builtinSetBytes.Length);
            output.WriteBytes(Opcodes.EMPTY_LIST, Opcodes.MARK);
            foreach (object x in o)
            {
                save(x);
            }
            output.WriteBytes(Opcodes.APPENDS, Opcodes.TUPLE1, Opcodes.REDUCE);
            WriteMemo(o);   // sets cannot contain self-references (because not hashable) so it is fine to put this at the end
        }

        private void put_arrayOfObjects(object[] array)
        {
            switch (array.Length)
            {
                // 0 objects->EMPTYTUPLE
                // 1 object->TUPLE1
                // 2 objects->TUPLE2
                // 3 objects->TUPLE3
                // 4 or more->MARK+items+TUPLE
                case 0:
                    output.WriteByte(Opcodes.EMPTY_TUPLE);
                    break;
                case 1:
                    if (array[0] == array)
                        ThrowRecursiveArrayNotSupported();
                    save(array[0]);
                    output.WriteByte(Opcodes.TUPLE1);
                    break;
                case 2:
                    if (array[0] == array || array[1] == array)
                        ThrowRecursiveArrayNotSupported();
                    save(array[0]);
                    save(array[1]);
                    output.WriteByte(Opcodes.TUPLE2);
                    break;
                case 3:
                    if (array[0] == array || array[1] == array || array[2] == array)
                        ThrowRecursiveArrayNotSupported();
                    save(array[0]);
                    save(array[1]);
                    save(array[2]);
                    output.WriteByte(Opcodes.TUPLE3);
                    break;
                default:
                    output.WriteByte(Opcodes.MARK);
                    foreach (object o in array)
                    {
                        if (o == array)
                            ThrowRecursiveArrayNotSupported();
                        save(o);
                    }
                    output.WriteByte(Opcodes.TUPLE);
                    break;
            }

            WriteMemo(array);       // tuples cannot contain self-references so it is fine to put this at the end
        }

        private static void ThrowRecursiveArrayNotSupported() =>
            throw new PickleException("recursive array not supported, use list");

        private void put_arrayOfPrimitives(Type t, object array)
        {
            TypeCode typeCode = Type.GetTypeCode(t);

            // Special-case several array types written out specially.
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    // a bool[] isn't written as an array but rather as a tuple
                    var source = (bool[])array;
                    // this is stupid, but seems to be necessary because you can't cast a bool[] to an object[]
                    var boolarray = new object[source.Length];
                    Array.Copy(source, boolarray, source.Length);
                    put_arrayOfObjects(boolarray);
                    return;

                case TypeCode.Char:
                    // a char[] isn't written as an array but rather as a unicode string
                    string s = new string((char[])array);
                    put_string(s);
                    return;

                case TypeCode.Byte:
                    // a byte[] isn't written as an array but rather as a bytearray object
                    output.WriteByte(Opcodes.GLOBAL);
                    output.Write(builtinBytearrayBytes, 0, builtinBytearrayBytes.Length);
                    put_string(PickleUtils.rawStringFromBytes((byte[])array));
                    put_string("latin-1");  // this is what python writes in the pickle
                    output.WriteBytes(Opcodes.TUPLE2, Opcodes.REDUCE);
                    WriteMemo(array);
                    return;
            }

            output.WriteByte(Opcodes.GLOBAL);
            output.Write(arrayArrayBytes, 0, arrayArrayBytes.Length);
            output.WriteByte(Opcodes.SHORT_BINSTRING); // array typecode follows
            output.WriteByte(1); // typecode is 1 char

            switch (typeCode)
            {
                case TypeCode.SByte:
                    output.WriteBytes((byte)'b' /* signed char */, Opcodes.EMPTY_LIST, Opcodes.MARK);
                    foreach (sbyte s in (sbyte[])array)
                    {
                        put_long(s);
                    }
                    break;

                case TypeCode.Int16:
                    output.WriteByte((byte)'h'); // signed short
                    output.WriteByte(Opcodes.EMPTY_LIST);
                    output.WriteByte(Opcodes.MARK);
                    foreach (short s in (short[])array)
                    {
                        put_long(s);
                    }
                    break;

                case TypeCode.UInt16:
                    output.WriteByte((byte)'H'); // unsigned short
                    output.WriteByte(Opcodes.EMPTY_LIST);
                    output.WriteByte(Opcodes.MARK);
                    foreach (ushort s in (ushort[])array)
                    {
                        put_long(s);
                    }
                    break;

                case TypeCode.Int32:
                    output.WriteByte((byte)'i'); // signed int
                    output.WriteByte(Opcodes.EMPTY_LIST);
                    output.WriteByte(Opcodes.MARK);
                    foreach (int i in (int[])array)
                    {
                        put_long(i);
                    }
                    break;

                case TypeCode.UInt32:
                    output.WriteByte((byte)'I'); // unsigned int
                    output.WriteByte(Opcodes.EMPTY_LIST);
                    output.WriteByte(Opcodes.MARK);
                    foreach (uint i in (uint[])array)
                    {
                        put_long(i);
                    }
                    break;

                case TypeCode.Int64:
                    output.WriteByte((byte)'l');  // signed long
                    output.WriteByte(Opcodes.EMPTY_LIST);
                    output.WriteByte(Opcodes.MARK);
                    foreach (long v in (long[])array)
                    {
                        put_long(v);
                    }
                    break;

                case TypeCode.UInt64:
                    output.WriteByte((byte)'L');  // unsigned long
                    output.WriteByte(Opcodes.EMPTY_LIST);
                    output.WriteByte(Opcodes.MARK);
                    foreach (ulong v in (ulong[])array)
                    {
                        put_ulong(v);
                    }
                    break;

                case TypeCode.Single:
                    output.WriteByte((byte)'f');  // float
                    output.WriteByte(Opcodes.EMPTY_LIST);
                    output.WriteByte(Opcodes.MARK);
                    foreach (float f in (float[])array)
                    {
                        put_float(f);
                    }
                    break;

                case TypeCode.Double:
                    output.WriteByte((byte)'d');  // double
                    output.WriteByte(Opcodes.EMPTY_LIST);
                    output.WriteByte(Opcodes.MARK);
                    foreach (double d in (double[])array)
                    {
                        put_float(d);
                    }
                    break;
            }

            output.WriteByte(Opcodes.APPENDS);
            output.WriteByte(Opcodes.TUPLE2);
            output.WriteByte(Opcodes.REDUCE);

            WriteMemo(array); // array of primitives can by definition never be recursive, so okay to put this at the end
        }

        private void put_decimal(decimal d)
        {
            //"cdecimal\nDecimal\nU\n12345.6789\u0085R."
            output.WriteByte(Opcodes.GLOBAL);
            output.Write(decimalDecimalBytes, 0, decimalDecimalBytes.Length);
            put_string(d.ToString(CultureInfo.InvariantCulture));
            output.WriteByte(Opcodes.TUPLE1);
            output.WriteByte(Opcodes.REDUCE);
            WriteMemo(d);
        }

        private void put_string(string str)
        {
            output.WriteByte(Opcodes.BINUNICODE);
            output.WriteAsUtf8String(str);
            WriteMemo(str);
        }

        private void put_float(double d)
        {
            output.WriteByte(Opcodes.BINFLOAT);
            output.WriteInt64BigEndian(BitConverter.DoubleToInt64Bits(d));
        }

        private void put_byte(byte value)
        {
            output.WriteBytes(Opcodes.BININT1, value);
        }

        private void put_long(long v)
        {
            // choose optimal representation
            // first check 1 and 2-byte unsigned ints:
            if (v >= 0)
            {
                if (v <= byte.MaxValue)
                {
                    output.WriteByte(Opcodes.BININT1);
                    output.WriteByte((byte)v);
                    return;
                }
                if (v <= ushort.MaxValue)
                {
                    output.WriteByte(Opcodes.BININT2);
                    output.WriteByte((byte)(v & 0xff));
                    output.WriteByte((byte)(v >> 8));
                    return;
                }
            }

            // 4-byte signed int?
            long high_bits = v >> 31;  // shift sign extends
            if (high_bits == 0 || high_bits == -1)
            {
                // All high bits are copies of bit 2**31, so the value fits in a 4-byte signed int.
                output.WriteByte(Opcodes.BININT);
                output.WriteInt32LittleEndian((int)v);
                return;
            }

            // int too big, store it as text
            output.WriteByte(Opcodes.INT);
            byte[] bytes = Encoding.ASCII.GetBytes(v.ToString(CultureInfo.InvariantCulture));
            output.Write(bytes, 0, bytes.Length);
            output.WriteByte((byte)'\n');
        }

        private void put_ulong(ulong u)
        {
            if (u <= long.MaxValue)
            {
                put_long((long)u);
            }
            else
            {
                // ulong too big for a signed long, store it as text instead.
                output.WriteByte(Opcodes.INT);
                var bytes = Encoding.ASCII.GetBytes(u.ToString(CultureInfo.InvariantCulture));
                output.Write(bytes, 0, bytes.Length);
                output.WriteByte((byte)'\n');
            }
        }

        private void put_bool(bool b)
        {
            output.WriteByte(b ? Opcodes.NEWTRUE : Opcodes.NEWFALSE);
        }

        private void put_objwithproperties(object o)
        {
            var properties = o.GetType().GetProperties();
            var map = new Dictionary<string, object>();
            foreach (var propinfo in properties)
            {
                if (propinfo.CanRead)
                {
                    string name = propinfo.Name;
                    try
                    {
                        map[name] = propinfo.GetValue(o, null);
                    }
                    catch (Exception x)
                    {
                        throw new PickleException("cannot pickle object:", x);
                    }
                }
            }

            // if we're dealing with an anonymous type, don't output the type name.
            if (!o.GetType().Name.StartsWith("<>"))
                map["__class__"] = o.GetType().FullName;

            save(map);
        }

        private void put_serializable(Type t, object o)
        {
            var map = new Dictionary<string, object>();
            var fields = t.GetFields();
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute(typeof(NonSerializedAttribute)) == null)
                {
                    string name = field.Name;
                    try
                    {
                        map[name] = field.GetValue(o);
                    }
                    catch (Exception x)
                    {
                        throw new PickleException("cannot pickle [Serializable] object:", x);
                    }
                }
            }
            var properties = t.GetProperties();
            foreach (var propinfo in properties)
            {
                if (propinfo.CanRead)
                {
                    string name = propinfo.Name;
                    try
                    {
                        map[name] = propinfo.GetValue(o, null);
                    }
                    catch (Exception x)
                    {
                        throw new PickleException("cannot pickle [Serializable] object:", x);
                    }
                }
            }

            // if we're dealing with an anonymous type, don't output the type name.
            if (!o.GetType().Name.StartsWith("<>"))
                map["__class__"] = o.GetType().FullName;

            save(map);
        }

        private void put_datacontract(Type t, object o, DataContractAttribute dca)
        {
            var fields = t.GetFields();
            var map = new Dictionary<string, object>();
            foreach (var field in fields)
            {
                DataMemberAttribute dma = (DataMemberAttribute)field.GetCustomAttribute(typeof(DataMemberAttribute));
                if (dma != null)
                {
                    string name = dma.Name;
                    try
                    {
                        map[name] = field.GetValue(o);
                    }
                    catch (Exception x)
                    {
                        throw new PickleException("cannot pickle [DataContract] object:", x);
                    }
                }
            }
            var properties = t.GetProperties();
            foreach (var propinfo in properties)
            {
                if (propinfo.CanRead && propinfo.GetCustomAttribute(typeof(DataMemberAttribute)) != null)
                {
                    string name = propinfo.Name;
                    try
                    {
                        map[name] = propinfo.GetValue(o, null);
                    }
                    catch (Exception x)
                    {
                        throw new PickleException("cannot pickle [DataContract] object:", x);
                    }
                }
            }

            if (string.IsNullOrEmpty(dca.Name))
            {
                // if we're dealing with an anonymous type, don't output the type name.
                if (!o.GetType().Name.StartsWith("<>"))
                    map["__class__"] = o.GetType().FullName;
            }
            else
            {
                map["__class__"] = dca.Name;
            }

            save(map);
        }
    }
}