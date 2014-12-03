/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Dynamic;

namespace Razorvine.Pyro
{

/// <summary>
/// Flame-Wrapper for a builtin function.
/// </summary>
public class FlameBuiltin : DynamicObject, IDisposable
{
	private PyroProxy flameserver;
	private string builtin;
	
	public byte[] pyroHmacKey {
		get {
			return flameserver.pyroHmacKey;
		}
		set {
			flameserver.pyroHmacKey = value;
		}
	}
	
	/// <summary>
	/// for the unpickler to restore state
	/// </summary>
	public void __setstate__(Hashtable values) {
		flameserver=(PyroProxy) values["flameserver"];
		builtin=(string) values["builtin"];
	}
	
	public Object call(params object[] arguments) {
		return flameserver.call("invokeBuiltin", builtin, arguments, new Hashtable(0));
	}

	/// <summary>
	/// Makes it easier to call this builtin as if it was a normal callable.
	/// You'll have to use the 'dynamic' type for your FlameBuiltin object though.
	/// </summary>
	public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
	{
		result = flameserver.call("invokeBuiltin", builtin, args, new Hashtable(0));
		return true;
	}
		
	public void close()	{
		if(flameserver!=null)
			flameserver.close();
	}
	
	public void Dispose()
	{
		close();
	}
}
}
