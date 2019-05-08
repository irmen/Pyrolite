/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberInitializerValueIgnored
// ReSharper disable InconsistentNaming
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
// ReSharper disable SuggestBaseTypeForParameter

namespace Razorvine.Pickle
{

    /// <summary>
    /// Pickle an object graph into a Python-compatible pickle stream. For
    /// simplicity, the only supported pickle protocol at this time is protocol 2. 
    /// See README.txt for a table with the type mapping.
    /// This class is NOT threadsafe! (Don't use the same pickler from different threads)
    /// </summary>
    public class Pickler : IDisposable {

	// ReSharper disable once UnusedMember.Global
	public const int HIGHEST_PROTOCOL = 2;
	public const int MAX_RECURSE_DEPTH = 200;
	public const int PROTOCOL = 2;

	protected static readonly IDictionary<Type, IObjectPickler> customPicklers = new Dictionary<Type, IObjectPickler>();
    protected readonly bool useMemo=true;
    private IPickler pickler;
	
	/**
	 * Create a Pickler.
	 */
	public Pickler() : this(true) {
	}

	/**
	 * Create a Pickler. Specify if it is to use a memo table or not.
	 * The memo table is NOT reused across different calls.
	 * If you use a memo table, you can only pickle objects that are hashable.
	 */
	public Pickler(bool useMemo) {
		this.useMemo=useMemo;
	}
	
	/**
	 * Close the pickler stream, discard any internal buffers.
	 */
    public void close() => pickler.Dispose();

	/**
	 * Register additional object picklers for custom classes.
	 * If you register an interface or abstract base class, it means the pickler is used for 
	 * the whole inheritance tree of all classes ultimately implementing that interface or abstract base class.
	 * If you register a normal concrete class, the pickler is only used for objects of exactly that particular class.
	 */
	public static void registerCustomPickler(Type clazz, IObjectPickler pickler) {
		customPicklers[clazz]=pickler;
	}
	
	/**
	 * Pickle a given object graph, returning the result as a byte array.
	 */
	public byte[] dumps(object o)
    {
        pickler = new Pickler<ArrayWriter>(new ArrayWriter(new byte[16]), this, useMemo);

        pickler.dump(o);

        byte[] bytes = pickler.GetByteArray();
        Array.Resize(ref bytes, pickler.BytesWritten); // shrink it
        return bytes;
    }

    /// <summary>
    /// Pickle a given object graph
    /// </summary>
    /// <param name="o">object graph</param>
    /// <param name="output">resizable output</param>
    /// <returns>written bytes count</returns>
	public int dumps(object o, ref byte[] output)
    {
        pickler = new Pickler<ArrayWriter>(new ArrayWriter(output), this, useMemo);

        pickler.dump(o);
        output = pickler.GetByteArray();

        return pickler.BytesWritten;
    }

	/**
	 * Pickle a given object graph, writing the result to the output stream.
	 */
	public void dump(object o, Stream stream)
    {
        pickler = new Pickler<StreamWriter>(new StreamWriter(stream), this, useMemo);

        pickler.dump(o);

        stream.Flush();
    }

	/**
	 * Pickle a single object and write its pickle representation to the output stream.
	 * Normally this is used internally by the pickler, but you can also utilize it from
	 * within custom picklers. This is handy if as part of the custom pickler, you need
	 * to write a couple of normal objects such as strings or ints, that are already
	 * supported by the pickler.
	 * This method can be called recursively to output sub-objects.
	 */
    public void save(object o) => pickler.save(o);

    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
    protected internal IObjectPickler getCustomPickler(Type t)
    {
        IObjectPickler pickler;
        if (customPicklers.TryGetValue(t, out pickler))
            return pickler;     // exact match

        // check if there's a custom pickler registered for an interface or abstract base class
        // that this object implements or inherits from.
        foreach (var x in customPicklers)
        {
            if (x.Key.IsAssignableFrom(t))
            {
                return x.Value;
            }
        }

        return null;
    }

    public void Dispose() => close();
}

}
