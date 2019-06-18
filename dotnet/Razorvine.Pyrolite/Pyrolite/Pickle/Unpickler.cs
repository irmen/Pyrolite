/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Razorvine.Pickle.Objects;

namespace Razorvine.Pickle
{

/// <summary>
/// Unpickles an object graph from a pickle data inputstream. Supports all pickle protocol versions.
/// Maps the python objects on the corresponding java equivalents or similar types.
/// This class is NOT threadsafe! (Don't use the same unpickler from different threads)
/// See the README.txt for a table with the type mappings.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "InvertIf")]
public class Unpickler : IDisposable {

	public const int HIGHEST_PROTOCOL = 5;

	internal readonly IDictionary<int, object> memo;
	private UnpickleStack stack;
	protected internal static readonly IDictionary<string, IObjectConstructor> objectConstructors = CreateObjectConstructorsDictionary();

    private static Dictionary<string, IObjectConstructor> CreateObjectConstructorsDictionary()
    {
        return new Dictionary<string, IObjectConstructor>(15)
		{
			["__builtin__.complex"] = new AnyClassConstructor(typeof(ComplexNumber)),
			["builtins.complex"] = new AnyClassConstructor(typeof(ComplexNumber)),
			["array.array"] = new ArrayConstructor(),
			["array._array_reconstructor"] = new ArrayConstructor(),
			["__builtin__.bytearray"] = new ByteArrayConstructor(),
			["builtins.bytearray"] = new ByteArrayConstructor(),
			["__builtin__.bytes"] = new ByteArrayConstructor(),
			["__builtin__.set"] = new SetConstructor(),
			["builtins.set"] = new SetConstructor(),
			["datetime.datetime"] = new DateTimeConstructor(DateTimeConstructor.PythonType.DateTime),
			["datetime.time"] = new DateTimeConstructor(DateTimeConstructor.PythonType.Time),
			["datetime.date"] = new DateTimeConstructor(DateTimeConstructor.PythonType.Date),
			["datetime.timedelta"] = new DateTimeConstructor(DateTimeConstructor.PythonType.TimeDelta),
			["decimal.Decimal"] = new DecimalConstructor(),
			["_codecs.encode"] = new ByteArrayConstructor()
		};
		// we're lucky, the bytearray constructor is also able to mimic codecs.encode()
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
        stack = new UnpickleStack();
        var unpickler = new UnpicklerImplementation<StreamReader>(new StreamReader(stream), memo, stack, this);
        return unpickler.Load();
    }

    /// <summary>
    /// Read a pickled object representation from the given pickle data bytes.
    /// </summary>
    /// <param name="pickledata">Serialized pickle data.</param>
    /// <returns>the reconstituted object hierarchy specified in the memory buffer.</returns>
    public object loads(byte[] pickledata) {
        stack = new UnpickleStack();
        var unpickler = new UnpicklerImplementation<ArrayReader>(new ArrayReader(pickledata), memo, stack, this);
        return unpickler.Load();
    }

    /// <summary>
    /// Read a pickled object representation from the given pickle data bytes.
    /// </summary>
    /// <param name="pickledata">Serialized pickle data.</param>
    /// <param name="stackCapacity">Initial capacity of the UnpickleStack.</param>
    /// <returns>the reconstituted object hierarchy specified in the memory buffer.</returns>
    public object loads(byte[] pickledata, int stackCapacity) {
        stack = new UnpickleStack(stackCapacity);
        var unpickler = new UnpicklerImplementation<ArrayReader>(new ArrayReader(pickledata), memo, stack, this);
        return unpickler.Load();
    }

    /// <summary>
    /// Read a pickled object representation from the given pickle data memory buffer.
    /// </summary>
    /// <param name="pickledata">Serialized pickle data.</param>
    /// <param name="stackCapacity">Optional parameter that suggests the initial capacity of stack. The default value is 4.</param>
    /// <returns>the reconstituted object hierarchy specified in the memory buffer.</returns>
    public object loads(ReadOnlyMemory<byte> pickledata, int stackCapacity = UnpickleStack.DefaultCapacity) {
        // ROM is super fast for .NET Core 3.0, but Array is fast for all the runtimes
        // if we can get an array out of ROM, we use the Array instead
        if (MemoryMarshal.TryGetArray(pickledata, out ArraySegment<byte> arraySegment))
            return loads(arraySegment.Array, stackCapacity);

        stack = new UnpickleStack(stackCapacity);
        var unpickler = new UnpicklerImplementation<ReadOnlyMemoryReader>(new ReadOnlyMemoryReader(pickledata), memo, stack, this);
        return unpickler.Load();
    }

	/**
	 * Close the unpickler and frees the resources such as the unpickle stack and memo table.
	 */
	public void close() {
		stack?.clear();
		memo?.Clear();
	}

	
	/**
	 * Buffer support for protocol 5 out of band data
	 * If you want to unpickle such pickles, you'll have to subclass the unpickler
	 * and override this method to return the buffer data you want.
	 */
	public virtual object nextBuffer()  {
		throw new PickleException("pickle stream refers to out-of-band data but no user-overridden nextBuffer() method is used\n");
	}
	
	protected internal virtual object persistentLoad(string pid)
	{
		throw new PickleException("A load persistent id instruction was encountered, but no persistentLoad function was specified. (implement it in custom Unpickler subclass)");
	}
	
	public void Dispose()
	{
		close();
	}
}

}
