/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace Razorvine.Pickle
{
    // the following type is generic in order to allow for 
    // IInputReader interface method devirtualizaiton and inlining
    // please see https://adamsitnik.com/Value-Types-vs-Reference-Types/#how-to-avoid-boxing-with-value-types-that-implement-interfaces for more
    // it also derives from Stream to allow to treat it as stream to support IObjectPickler scenario which exposes the writer as Stream
    internal class PicklerImplementation<T> : Stream, IPicklerImplementation
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
        private int recurse; // recursion level

        internal PicklerImplementation(T output, Pickler pickler, bool useMemo)
        {
            this.output = output;
            this.pickler = pickler;
            memo = useMemo ? new Dictionary<object, int>() : null;
            recurse = 0;
        }

        protected override void Dispose(bool disposing)
        {
            output.Dispose();
            memo = null;
        }

        private bool useMemo => memo != null;

        public int BytesWritten => output.BytesWritten;

        public byte[] Buffer => output.Buffer;

        public void dump(object o)
        {
            recurse = 0;
            output.WriteBytes(Opcodes.PROTO, Pickler.PROTOCOL);

            save(o);

            memo = null;  // get rid of the memo table

            output.WriteByte(Opcodes.STOP);
            output.Flush();
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
                put_null();
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // important for performance when useMemo = false
        private void WriteMemo<TValue>(TValue value) // this method is generic to prevent boxing when useMemo = false
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
                else if (o is string[] strings)
                {
                    put_arrayOfStrings(strings);
                }
                else
                {
                    put_arrayOfObjects((object[])o);
                }
                return true;
            }

            if (t.IsPrimitive)
            {
                switch (o)
                {
                    case double v:
                        put_float(v);
                        return true;
                    case int v:
                        put_long(v);
                        return true;
                    case bool v:
                        put_bool(v);
                        return true;
                    case float v:
                        put_float(v);
                        return true;
                    case long v:
                        put_long(v);
                        return true;
                    case byte v:
                        put_byte(v);
                        return true;
                    case sbyte v:
                        put_long(v);
                        return true;
                    case short v:
                        put_long(v);
                        return true;
                    case ushort v:
                        put_long(v);
                        return true;
                    case uint v:
                        put_long(v);
                        return true;
                    case ulong v:
                        put_ulong(v);
                        return true;
                    case char v:
                        put_string(v.ToString());
                        return true;
                }
            }
            else
            {
                switch (o)
                {
                    case string v:
                        put_string(v);
                        return true;
                    case Enum v:
                        put_string(o.ToString());
                        return true;
                    case decimal v:
                        put_decimal(v);
                        return true;
                    case DateTime v:
                        put_datetime(v);
                        return true;
                    case TimeSpan v:
                        put_timespan(v);
                        return true;
                    case IDictionary v:
                        put_map(v);
                        return true;
                    case IList v:
                        put_enumerable(v);
                        return true;
                    case IEnumerable v:
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
                            put_set(v);
                        else
                            put_enumerable(v);
                        return true;
                }
            }

            // check registry
            IObjectPickler custompickler = pickler.getCustomPickler(t);
            if (custompickler != null)
            {
                // to support this scenario this type derives from Stream and implements Write methods
                custompickler.pickle(o, this, pickler);
                WriteMemo(o);
                return true;
            }

            // more complex types
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

            if (hasPublicProperties(t))
            {
                put_objwithproperties(o);
                return true;
            }

            return false;
        }

        private static bool hasPublicProperties(Type t) => t.GetProperties().Length > 0;

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

        // special case of put_arrayOfObjects to improve perf for arrays of strings scenario
        private void put_arrayOfStrings(string[] array)
        {
            switch (array.Length)
            {
                case 0:
                    output.WriteByte(Opcodes.EMPTY_TUPLE);
                    break;
                case 1:
                    put_string(array[0]);
                    output.WriteByte(Opcodes.TUPLE1);
                    break;
                case 2:
                    put_string(array[0]);
                    put_string(array[1]);
                    output.WriteByte(Opcodes.TUPLE2);
                    break;
                case 3:
                    put_string(array[0]);
                    put_string(array[1]);
                    put_string(array[2]);
                    output.WriteByte(Opcodes.TUPLE3);
                    break;
                default:
                    output.WriteByte(Opcodes.MARK);
                    foreach (string o in array)
                    {
                        put_string(o);
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

        private void put_null() => output.WriteByte(Opcodes.NONE);

        private void put_string(string str)
        {
            if (str == null)
            {
                put_null();
            }
            else
            {
                output.WriteByte(Opcodes.BINUNICODE);
                output.WriteAsUtf8String(str);
                WriteMemo(str);
            }
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

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        
        public override void Write(byte[] buffer, int offset, int count) => output.Write(buffer, offset, count);
        public override void WriteByte(byte value) => output.WriteByte(value);
        public override void Flush() => output.Flush();
    }
}