/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace Razorvine.Pyro
{

/// <summary>
/// Flame-Wrapper for a builtin function.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class FlameBuiltin : DynamicObject, IDisposable
{
	private PyroProxy _flameserver;
	private string _builtin;
	
	public byte[] pyroHmacKey {
		get {
			return _flameserver.pyroHmacKey;
		}
		set {
			_flameserver.pyroHmacKey = value;
		}
	}
	
	/// <summary>
	/// for the unpickler to restore state
	/// </summary>
	public void __setstate__(Hashtable values) {
		_flameserver=(PyroProxy) values["flameserver"];
		_builtin=(string) values["builtin"];
	}
	
	public object call(params object[] arguments) {
		return _flameserver.call("invokeBuiltin", _builtin, arguments, new Hashtable(0));
	}

	/// <summary>
	/// Makes it easier to call this builtin as if it was a normal callable.
	/// You'll have to use the 'dynamic' type for your FlameBuiltin object though.
	/// </summary>
	public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
	{
		result = _flameserver.call("invokeBuiltin", _builtin, args, new Hashtable(0));
		return true;
	}
		
	public void close()
	{
		_flameserver?.close();
	}
	
	public void Dispose()
	{
		close();
	}
}
}
