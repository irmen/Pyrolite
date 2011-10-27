/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;

namespace Razorvine.Pyrolite.Pyro
{

/// <summary>
/// Flame-Wrapper for a builtin function.
/// </summary>
public class FlameBuiltin : IDisposable
{

	private PyroProxy flameserver;
	private string builtin;
	
	/// <summary>
	/// for the unpickler to restore state
	/// </summary>
	public void __setstate__(Hashtable values) {
		flameserver=(PyroProxy) values["flameserver"];
		builtin=(string) values["builtin"];
	}
	
	public Object call(params object[] arguments) {
		return flameserver.call("_invokeBuiltin", builtin, arguments, new Hashtable(0));
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
