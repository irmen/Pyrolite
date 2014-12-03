/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Dynamic;
	
namespace Razorvine.Pyro
{

/// <summary>
/// Flame-Wrapper for a remote module.
/// </summary>
public class FlameModule : DynamicObject, IDisposable
{
	private PyroProxy flameserver;
	private string module;
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
		module=(string) values["module"];
	}
	
	/// <summary>
	/// Makes it easier to call methods on the proxy by intercepting the methods calls.
	/// You'll have to use the 'dynamic' type for your FlameModule object though.
	/// </summary>
	public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
	{
		result = call(binder.Name, args);
		return true;
	}
	
	public Object call(string attribute, params object[] arguments) {
		return flameserver.call("invokeModule", module+"."+attribute, arguments, new Hashtable(0));
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
