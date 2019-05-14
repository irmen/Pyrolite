/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

// ReSharper disable InconsistentNaming

namespace Razorvine.Pickle
{
    /// <summary>
    /// Pickle an object graph into a Python-compatible pickle stream. For
    /// simplicity, the only supported pickle protocol at this time is protocol 2. 
    /// See README.txt for a table with the type mapping.
    /// This class is NOT threadsafe! (Don't use the same pickler from different threads)
    /// </summary>
    public class Pickler : IDisposable
    {
        public const int HIGHEST_PROTOCOL = 2;
        public const int MAX_RECURSE_DEPTH = 200;
        public const int PROTOCOL = 2;

        protected static readonly IDictionary<Type, IObjectPickler> customPicklers = new Dictionary<Type, IObjectPickler>();
        protected readonly bool useMemo = true;
        private IPicklerImplementation _picklerImplementation;

        /// <summary>
        /// Create a Pickler.
        /// </summary>
        public Pickler() : this(true) { }

        /// <summary>
        /// Create a Pickler. 
        /// </summary>
        /// <param name="useMemo">Specify if it is to use a memo table or not.</param>
        /// <remarks>
        /// The memo table is NOT reused across different calls.
        /// If you use a memo table, you can only pickle objects that are hashable.
        /// </remarks>
        public Pickler(bool useMemo) => this.useMemo = useMemo;

        /// <summary>
        /// Close the pickler stream, discard any internal buffers.
        /// </summary>
        public void Dispose() => close();

        /// <summary>
        /// Close the pickler stream, discard any internal buffers.
        /// </summary>
        public void close() => _picklerImplementation?.Dispose();

        /// <summary>
        /// Register additional object picklers for custom classes.
        /// If you register an interface or abstract base class, it means the pickler is used for 
        /// the whole inheritance tree of all classes ultimately implementing that interface or abstract base class.
        /// If you register a normal concrete class, the pickler is only used for objects of exactly that particular class.
        /// </summary>
        /// <param name="clazz">the custom class</param>
        /// <param name="pickler">additional object picklers</param>
        public static void registerCustomPickler(Type clazz, IObjectPickler pickler) => customPicklers[clazz] = pickler;

        /// <summary>
        /// Pickle a given object graph, returning the result as a byte array.
        /// </summary>
        /// <param name="o">object graph</param>
        /// <returns>result as byte array</returns>
        public byte[] dumps(object o)
        {
            close(); // the instance of this class can be reused so it needs to be closed first

            _picklerImplementation = new PicklerImplementation<ArrayWriter>(new ArrayWriter(new byte[64]), this, useMemo);

            _picklerImplementation.dump(o);

            byte[] bytes = _picklerImplementation.Buffer;
            Array.Resize(ref bytes, _picklerImplementation.BytesWritten); // shrink it
            return bytes;
        }

        /// <summary>
        /// Pickle a given object graph, writing the result to the provided byte array.
        /// </summary>
        /// <param name="o">object graph</param>
        /// <param name="output">resizable output</param>
        /// <param name="bytesWritten">written bytes count</param>
        /// <remarks>if the array is not big enought it's going to be resized</remarks>
        public void dumps(object o, ref byte[] output, out int bytesWritten)
        {
            close();

            _picklerImplementation = new PicklerImplementation<ArrayWriter>(new ArrayWriter(output), this, useMemo);

            _picklerImplementation.dump(o);

            output = _picklerImplementation.Buffer;
            bytesWritten = _picklerImplementation.BytesWritten;
        }

        /// <summary>
        /// Pickle a given object graph, writing the result to the output stream.
        /// </summary>
        /// <param name="o">object graph</param>
        /// <param name="stream">output stream</param>
        public void dump(object o, Stream stream)
        {
            close();

            _picklerImplementation = new PicklerImplementation<StreamWriter>(new StreamWriter(stream), this, useMemo);

            _picklerImplementation.dump(o);
        }

        /// <summary>
        /// Pickle a single object and write its pickle representation to the output stream.
        /// Normally this is used internally by the pickler, but you can also utilize it from
        /// within custom picklers. This is handy if as part of the custom pickler, you need
        /// to write a couple of normal objects such as strings or ints, that are already
        /// supported by the pickler.
        /// This method can be called recursively to output sub-objects.
        /// </summary>
        /// <param name="o">single object</param>
        public void save(object o) => _picklerImplementation.save(o);

        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        protected internal IObjectPickler getCustomPickler(Type t)
            => customPicklers.TryGetValue(t, out var pickler)
                ? pickler
                // check if there's a custom pickler registered for an interface or abstract base class
                // that this object implements or inherits from.
                : customPicklers.FirstOrDefault(x => x.Key.IsAssignableFrom(t)).Value;
    }
}
