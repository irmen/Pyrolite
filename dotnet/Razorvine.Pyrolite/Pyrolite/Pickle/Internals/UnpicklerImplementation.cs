/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using Razorvine.Pickle.Objects;
using System;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Razorvine.Pickle
{
    // the following type is generic in order to allow for 
    // IInputReader interface method devirtualizaiton and inlining
    // please see https://adamsitnik.com/Value-Types-vs-Reference-Types/#how-to-avoid-boxing-with-value-types-that-implement-interfaces for more
    internal class UnpicklerImplementation<T> where T : struct, IInputReader
    {
        private static readonly string[] quoteStrings = new[] { "\"", "'" };
        private static readonly object boxedFalse = false;
        private static readonly object boxedTrue = true;

        private readonly UnpickleStack stack;
        private readonly Unpickler unpickler;
        private readonly IDictionary<int, object> memo;

        private T input; // must NOT be readonly, it's a mutable struct
        private Dictionary<StringPair, string> concatenatedModuleNames;

        public UnpicklerImplementation(T input, IDictionary<int, object> memo, UnpickleStack stack, Unpickler unpickler)
        {
            this.input = input;
            this.memo = memo;
            this.stack = stack;
            this.unpickler = unpickler;
        }

        public object Load()
        {
            byte key = 0;
            while ((key = input.ReadByte()) != Opcodes.STOP)
            {
                Dispatch(key);
            }

            object value = stack.pop();
            stack.clear();
            unpickler.memo.Clear();
            return value; // final result value
        }
        
        private void Dispatch(byte key)
        {
            switch (key)
            {
                case Opcodes.MARK:
                    load_mark();
                    return;
                case Opcodes.POP:
                    load_pop();
                    return;
                case Opcodes.POP_MARK:
                    load_pop_mark();
                    return;
                case Opcodes.DUP:
                    load_dup();
                    return;
                case Opcodes.FLOAT:
                    load_float();
                    return;
                case Opcodes.INT:
                    load_int();
                    return;
                case Opcodes.BININT:
                    load_binint();
                    return;
                case Opcodes.BININT1:
                    load_binint1();
                    return;
                case Opcodes.LONG:
                    load_long();
                    return;
                case Opcodes.BININT2:
                    load_binint2();
                    return;
                case Opcodes.NONE:
                    load_none();
                    return;
                case Opcodes.PERSID:
                    load_persid();
                    return;
                case Opcodes.BINPERSID:
                    load_binpersid();
                    return;
                case Opcodes.REDUCE:
                    load_reduce();
                    return;
                case Opcodes.STRING:
                    load_string();
                    return;
                case Opcodes.BINSTRING:
                    load_binstring();
                    return;
                case Opcodes.SHORT_BINSTRING:
                    load_short_binstring();
                    return;
                case Opcodes.UNICODE:
                    load_unicode();
                    return;
                case Opcodes.BINUNICODE:
                    load_binunicode();
                    return;
                case Opcodes.APPEND:
                    load_append();
                    return;
                case Opcodes.BUILD:
                    load_build();
                    return;
                case Opcodes.GLOBAL:
                    load_global();
                    return;
                case Opcodes.DICT:
                    load_dict();
                    return;
                case Opcodes.EMPTY_DICT:
                    load_empty_dictionary();
                    return;
                case Opcodes.APPENDS:
                    load_appends();
                    return;
                case Opcodes.GET:
                    load_get();
                    return;
                case Opcodes.BINGET:
                    load_binget();
                    return;
                case Opcodes.INST:
                    load_inst();
                    return;
                case Opcodes.LONG_BINGET:
                    load_long_binget();
                    return;
                case Opcodes.LIST:
                    load_list();
                    return;
                case Opcodes.EMPTY_LIST:
                    load_empty_list();
                    return;
                case Opcodes.OBJ:
                    load_obj();
                    return;
                case Opcodes.PUT:
                    load_put();
                    return;
                case Opcodes.BINPUT:
                    load_binput();
                    return;
                case Opcodes.LONG_BINPUT:
                    load_long_binput();
                    return;
                case Opcodes.SETITEM:
                    load_setitem();
                    return;
                case Opcodes.TUPLE:
                    load_tuple();
                    return;
                case Opcodes.EMPTY_TUPLE:
                    load_empty_tuple();
                    return;
                case Opcodes.SETITEMS:
                    load_setitems();
                    return;
                case Opcodes.BINFLOAT:
                    load_binfloat();
                    return;

                // protocol 2
                case Opcodes.PROTO:
                    load_proto();
                    return;
                case Opcodes.NEWOBJ:
                    load_newobj();
                    return;
                case Opcodes.EXT1:
                case Opcodes.EXT2:
                case Opcodes.EXT4:
                    throw new PickleException("Unimplemented opcode EXT1/EXT2/EXT4 encountered. Don't use extension codes when pickling via copyreg.add_extension() to avoid this error.");
                case Opcodes.TUPLE1:
                    load_tuple1();
                    return;
                case Opcodes.TUPLE2:
                    load_tuple2();
                    return;
                case Opcodes.TUPLE3:
                    load_tuple3();
                    return;
                case Opcodes.NEWTRUE:
                    load_true();
                    return;
                case Opcodes.NEWFALSE:
                    load_false();
                    return;
                case Opcodes.LONG1:
                    load_long1();
                    return;
                case Opcodes.LONG4:
                    load_long4();
                    return;

                // Protocol 3 (Python 3.x)
                case Opcodes.BINBYTES:
                    load_binbytes();
                    return;
                case Opcodes.SHORT_BINBYTES:
                    load_short_binbytes();
                    return;

                // Protocol 4 (Python 3.4-3.7)
                case Opcodes.BINUNICODE8:
                    load_binunicode8();
                    return;
                case Opcodes.SHORT_BINUNICODE:
                    load_short_binunicode();
                    return;
                case Opcodes.BINBYTES8:
                    load_binbytes8();
                    return;
                case Opcodes.EMPTY_SET:
                    load_empty_set();
                    return;
                case Opcodes.ADDITEMS:
                    load_additems();
                    return;
                case Opcodes.FROZENSET:
                    load_frozenset();
                    return;
                case Opcodes.MEMOIZE:
                    load_memoize();
                    return;
                case Opcodes.FRAME:
                    load_frame();
                    return;
                case Opcodes.NEWOBJ_EX:
                    load_newobj_ex();
                    return;
                case Opcodes.STACK_GLOBAL:
                    load_stack_global();
                    return;
                
                // protocol 5 (Python 3.8+)
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
        }


        private void load_bytearray8()
        {
            // this is the same as load_binbytes8 because we make no distinction
            // here between the bytes and bytearray python types
            long len = BinaryPrimitives.ReadInt64LittleEndian(input.ReadBytes(sizeof(long)));
            stack.add(input.ReadBytes(PickleUtils.CheckedCast(len)).ToArray());
        }

        private void load_readonly_buffer()
        {
            // this opcode is ignored, we don't distinguish between readonly and read/write buffers
        }

        private void load_next_buffer()
        {
            stack.add(unpickler.nextBuffer());
        }
        
        
        private void load_build()
        {
            object args = stack.pop();
            object target = stack.peek();
            object[] arguments = { args };
            Type[] argumentTypes = { args.GetType() };

            // call the __setstate__ method with the given arguments
            try
            {
                MethodInfo setStateMethod = target.GetType().GetMethod("__setstate__", argumentTypes);
                if (setStateMethod == null)
                {
                    throw new PickleException($"no __setstate__() found in type {target.GetType()} with argument type {args.GetType()}");
                }
                setStateMethod.Invoke(target, arguments);
            }
            catch (Exception e)
            {
                throw new PickleException("failed to __setstate__()", e);
            }
        }

        private void load_proto()
        {
            byte proto = input.ReadByte();
            if (proto > Unpickler.HIGHEST_PROTOCOL)
                throw new PickleException("unsupported pickle protocol: " + proto);
        }

        private void load_none()
        {
            stack.add(null);
        }

        private void load_false()
        {
            stack.add(boxedFalse);
        }

        private void load_true()
        {
            stack.add(boxedTrue);
        }

        private void load_int()
        {
            ReadOnlySpan<byte> bytes = input.ReadLineBytes(includeLF: true);
            if (bytes.Length == 3 && bytes[2] == (byte)'\n' && bytes[0] == (byte)'0')
            {
                if (bytes[1] == (byte)'0')
                {
                    load_false();
                    return;
                }
                else if (bytes[1] == (byte)'1')
                {
                    load_true();
                    return;
                }
            }

            bytes = bytes.Slice(0, bytes.Length - 1);
            if (bytes.Length > 0 && Utf8Parser.TryParse(bytes, out int intNumber, out int bytesConsumed) && bytesConsumed == bytes.Length)
            {
                stack.add(intNumber);
            }
            else if (bytes.Length > 0 && Utf8Parser.TryParse(bytes, out long longNumber, out bytesConsumed) && bytesConsumed == bytes.Length)
            {
                stack.add(longNumber);
            }
            else
            {
                stack.add(long.Parse(PickleUtils.rawStringFromBytes(bytes)));
                Debug.Fail("long.Parse should have thrown.");
            }
        }

        private void load_binint()
        {
            int integer = BinaryPrimitives.ReadInt32LittleEndian(input.ReadBytes(sizeof(int)));
            stack.add(integer);
        }

        private void load_binint1()
        {
            stack.add((int)input.ReadByte());
        }

        private void load_binint2()
        {
            int integer = BinaryPrimitives.ReadUInt16LittleEndian(input.ReadBytes(sizeof(short)));
            stack.add(integer);
        }

        private void load_long()
        {
            string val = input.ReadLine();
            if (val.EndsWith("L"))
            {
                val = val.Substring(0, val.Length - 1);
            }
            if (long.TryParse(val, out long longvalue))
            {
                stack.add(longvalue);
            }
            else
            {
                throw new PickleException("long too large in load_long (need BigInt)");
            }
        }

        private void load_long1()
        {
            byte n = input.ReadByte();
            stack.add(PickleUtils.decode_long(input.ReadBytes(n)));
        }

        private void load_long4()
        {
            int n = BinaryPrimitives.ReadInt32LittleEndian(input.ReadBytes(sizeof(int)));
            stack.add(PickleUtils.decode_long(input.ReadBytes(n)));
        }

        private void load_float()
        {
            ReadOnlySpan<byte> bytes = input.ReadLineBytes(includeLF: true);
            if (!Utf8Parser.TryParse(bytes, out double d, out int bytesConsumed) || !PickleUtils.IsWhitespace(bytes.Slice(bytesConsumed)))
            {
                throw new FormatException();
            }
            stack.add(d);
        }

        private void load_binfloat()
        {
            double val = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(input.ReadBytes(sizeof(long))));
            stack.add(val);
        }

        private void load_string()
        {
            string rep = input.ReadLine();
            bool quotesOk = false;
            foreach (string q in quoteStrings) // double or single quote
            {
                if (rep.StartsWith(q))
                {
                    if (!rep.EndsWith(q))
                    {
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

        private void load_binstring()
        {
            int len = BinaryPrimitives.ReadInt32LittleEndian(input.ReadBytes(sizeof(int)));
            stack.add(PickleUtils.rawStringFromBytes(input.ReadBytes(len)));
        }

        private void load_binbytes()
        {
            int len = BinaryPrimitives.ReadInt32LittleEndian(input.ReadBytes(sizeof(int)));
            stack.add(input.ReadBytes(len).ToArray());
        }

        private void load_binbytes8()
        {
            long len = BinaryPrimitives.ReadInt64LittleEndian(input.ReadBytes(sizeof(long)));
            stack.add(input.ReadBytes(PickleUtils.CheckedCast(len)).ToArray());
        }

        private void load_unicode()
        {
            string str = PickleUtils.decode_unicode_escaped(input.ReadLine());
            stack.add(str);
        }

        private void load_binunicode()
        {
            int len = BinaryPrimitives.ReadInt32LittleEndian(input.ReadBytes(sizeof(int)));
            var data = input.ReadBytes(len);
            stack.add(PickleUtils.GetStringFromUtf8(data));
        }

        private unsafe void load_binunicode8()
        {
            long len = BinaryPrimitives.ReadInt64LittleEndian(input.ReadBytes(sizeof(long)));
            stack.add(PickleUtils.GetStringFromUtf8(input.ReadBytes(PickleUtils.CheckedCast(len))));
        }

        private void load_short_binunicode()
        {
            int len = input.ReadByte();
            stack.add(PickleUtils.GetStringFromUtf8(input.ReadBytes(len)));
        }

        private void load_short_binstring()
        {
            byte len = input.ReadByte();
            stack.add(PickleUtils.rawStringFromBytes(input.ReadBytes(len)));
        }

        private void load_short_binbytes()
        {
            byte len = input.ReadByte();
            stack.add(input.ReadBytes(len).ToArray());
        }

        private void load_tuple()
        {
            stack.add(stack.pop_all_since_marker_as_array());
        }

        private void load_empty_tuple()
        {
            stack.add(Array.Empty<object>());
        }

        private void load_tuple1()
        {
            stack.add(new[] { stack.pop() });
        }

        private void load_tuple2()
        {
            object o2 = stack.pop();
            object o1 = stack.pop();
            stack.add(new[] { o1, o2 });
        }

        private void load_tuple3()
        {
            object o3 = stack.pop();
            object o2 = stack.pop();
            object o1 = stack.pop();
            stack.add(new[] { o1, o2, o3 });
        }

        private void load_empty_list()
        {
            stack.add(new ArrayList(5));
        }

        private void load_empty_dictionary()
        {
            stack.add(new Hashtable(5));
        }

        private void load_empty_set()
        {
            stack.add(new HashSet<object>());
        }

        private void load_list()
        {
            ArrayList top = stack.pop_all_since_marker();
            stack.add(top); // simply add the top items as a list to the stack again
        }

        private void load_dict()
        {
            object[] top = stack.pop_all_since_marker_as_array();
            Hashtable map = new Hashtable(top.Length);
            for (int i = 0; i < top.Length; i += 2)
            {
                object key = top[i];
                object value = top[i + 1];
                map[key] = value;
            }
            stack.add(map);
        }

        private void load_frozenset()
        {
            object[] top = stack.pop_all_since_marker_as_array();
            var set = new HashSet<object>();
            foreach (var element in top)
                set.Add(element);
            stack.add(set);
        }

        private void load_additems()
        {
            object[] top = stack.pop_all_since_marker_as_array();
            var set = (HashSet<object>)stack.pop();
            foreach (object item in top)
                set.Add(item);
            stack.add(set);
        }

        private void load_global()
        {
            string module = PickleUtils.GetStringFromUtf8(input.ReadLineBytes());
            string name = PickleUtils.GetStringFromUtf8(input.ReadLineBytes());

            load_global_sub(module, name);
        }

        private void load_stack_global()
        {
            string name = (string)stack.pop();
            string module = (string)stack.pop();
            load_global_sub(module, name);
        }

        private void load_global_sub(string module, string name)
        {
            if (Unpickler.objectConstructors.TryGetValue(GetModuleNameKey(module, name), out IObjectConstructor constructor))
            {
                stack.add(constructor);
                return;
            }

            switch (module)
            {
                // check if it is an exception
                case "exceptions":
                    // python 2.x
                    stack.add(new ExceptionConstructor(typeof(PythonException), module, name));
                    return;
                case "builtins":
                case "__builtin__":
                    if (name.EndsWith("Error") || name.EndsWith("Warning") || name.EndsWith("Exception")
                        || name == "GeneratorExit" || name == "KeyboardInterrupt"
                        || name == "StopIteration" || name == "SystemExit")
                    {
                        // it's a python 3.x exception
                        stack.add(new ExceptionConstructor(typeof(PythonException), module, name));
                    }
                    else
                    {
                        // return a dictionary with the class's properties
                        stack.add(new ClassDictConstructor(module, name));
                    }
                    return;
                default:
                    stack.add(new ClassDictConstructor(module, name));
                    return;
            }
        }

        private void load_pop()
        {
            stack.pop();
        }

        private void load_pop_mark()
        {
            object o;
            do
            {
                o = stack.pop();
            } while (o != stack.MARKER);
            stack.trim();
        }

        private void load_dup()
        {
            stack.add(stack.peek());
        }

        private void load_get()
        {
            int i = int.Parse(input.ReadLine());
            if (!memo.TryGetValue(i, out var value)) throw new PickleException("invalid memo key");
            stack.add(value);
        }

        private void load_binget()
        {
            byte i = input.ReadByte();
            if (!memo.TryGetValue(i, out var value)) throw new PickleException("invalid memo key");
            stack.add(value);
        }

        private void load_long_binget()
        {
            int i = BinaryPrimitives.ReadInt32LittleEndian(input.ReadBytes(sizeof(int)));
            if (!memo.TryGetValue(i, out var value)) throw new PickleException("invalid memo key");
            stack.add(value);
        }

        private void load_put()
        {
            int i = int.Parse(input.ReadLine());
            memo[i] = stack.peek();
        }

        private void load_binput()
        {
            byte i = input.ReadByte();
            memo[i] = stack.peek();
        }

        private void load_memoize()
        {
            memo[memo.Count] = stack.peek();
        }

        private void load_long_binput()
        {
            int i = BinaryPrimitives.ReadInt32LittleEndian(input.ReadBytes(sizeof(int)));
            memo[i] = stack.peek();
        }

        private void load_append()
        {
            object value = stack.pop();
            ArrayList list = (ArrayList)stack.peek();
            list.Add(value);
        }

        private void load_appends()
        {
            object[] top = stack.pop_all_since_marker_as_array();
            ArrayList list = (ArrayList)stack.peek();
            for (int i = 0; i < top.Length; i++)
            {
                list.Add(top[i]);
            }
        }

        private void load_setitem()
        {
            object value = stack.pop();
            object key = stack.pop();
            Hashtable dict = (Hashtable)stack.peek();
            dict[key] = value;
        }

        private void load_setitems()
        {
            var newitems = new List<KeyValuePair<object, object>>();
            object value = stack.pop();
            while (value != stack.MARKER)
            {
                object key = stack.pop();
                newitems.Add(new KeyValuePair<object, object>(key, value));
                value = stack.pop();
            }

            Hashtable dict = (Hashtable)stack.peek();
            foreach (var item in newitems)
            {
                dict[item.Key] = item.Value;
            }
        }

        private void load_mark()
        {
            stack.add_mark();
        }

        private void load_reduce()
        {
            var args = (object[])stack.pop();
            IObjectConstructor constructor = (IObjectConstructor)stack.pop();
            stack.add(constructor.construct(args));
        }

        private void load_newobj()
        {
            load_reduce(); // we just do the same as class(*args) instead of class.__new__(class,*args)
        }

        private void load_newobj_ex()
        {
            Hashtable kwargs = (Hashtable)stack.pop();
            var args = (object[])stack.pop();
            IObjectConstructor constructor = (IObjectConstructor)stack.pop();
            if (kwargs.Count == 0)
                stack.add(constructor.construct(args));
            else
                throw new PickleException("newobj_ex with keyword arguments not supported");
        }

        private void load_frame()
        {
            // for now we simply skip the frame opcode and its length
            input.Skip(sizeof(long));
        }

        private void load_persid()
        {
            // the persistent id is taken from the argument
            string pid = input.ReadLine();
            stack.add(unpickler.persistentLoad(pid));
        }

        private void load_binpersid()
        {
            // the persistent id is taken from the stack
            string pid = stack.pop().ToString();
            stack.add(unpickler.persistentLoad(pid));
        }

        private void load_obj()
        {
            object[] popped = stack.pop_all_since_marker_as_array();

            object[] args;
            if (popped.Length > 1)
            {
                args = new object[popped.Length - 1];
                Array.Copy(popped, 1, args, 0, args.Length);
            }
            else
            {
                args = Array.Empty<object>();
            }

            stack.add(((IObjectConstructor)popped[0]).construct(args));
        }

        private void load_inst()
        {
            string module = PickleUtils.GetStringFromUtf8(input.ReadLineBytes());
            string classname = PickleUtils.GetStringFromUtf8(input.ReadLineBytes());

            object[] args = stack.pop_all_since_marker_as_array();

            if (!Unpickler.objectConstructors.TryGetValue(GetModuleNameKey(module, classname), out IObjectConstructor constructor))
            {
                constructor = new ClassDictConstructor(module, classname);
                args = Array.Empty<object>(); // classdict doesn't have constructor args... so we may lose info here, hmm.
            }
            stack.add(constructor.construct(args));
        }

        private string GetModuleNameKey(string module, string name)
        {
            if (concatenatedModuleNames == null)
            {
                concatenatedModuleNames = new Dictionary<StringPair, string>();
            }

            var sp = new StringPair(module, name);
            if (!concatenatedModuleNames.TryGetValue(sp, out string key))
            {
                key = module + "." + name;
                concatenatedModuleNames.Add(sp, key);
            }

            return key;
        }

        private readonly struct StringPair : IEquatable<StringPair>
        {
            public readonly string Item1, Item2;
            public StringPair(string item1, string item2) { Item1 = item1; Item2 = item2; }
            public bool Equals(StringPair other) => Item1 == other.Item1 && Item2 == other.Item2;
            public override bool Equals(object obj) => obj is StringPair sp && Equals(sp);
            public override int GetHashCode() => Item1.GetHashCode() ^ Item2.GetHashCode();
        }
    }
}