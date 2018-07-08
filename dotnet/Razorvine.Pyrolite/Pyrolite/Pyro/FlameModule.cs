/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
	
namespace Razorvine.Pyro
{

/// <summary>
/// Flame-Wrapper for a remote module.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class FlameModule : DynamicObject, IDisposable
{
	private PyroProxy _flameserver;
	private string _module;
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
		_module=(string) values["module"];
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
	
	public object call(string attribute, params object[] arguments) {
		return _flameserver.call("invokeModule", _module+"."+attribute, arguments, new Hashtable(0));
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
