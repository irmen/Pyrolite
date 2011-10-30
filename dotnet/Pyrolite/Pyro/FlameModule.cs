/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;

namespace Razorvine.Pyro
{

/// <summary>
/// Flame-Wrapper for a remote module.
/// </summary>
public class FlameModule : IDisposable
{
	private PyroProxy flameserver;
	private string module;
	
	/// <summary>
	/// for the unpickler to restore state
	/// </summary>
	public void __setstate__(Hashtable values) {
		flameserver=(PyroProxy) values["flameserver"];
		module=(string) values["module"];
	}
	
	public Object call(string attribute, params object[] arguments) {
		return flameserver.call("_invokeModule", module+"."+attribute, arguments, new Hashtable(0));
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
