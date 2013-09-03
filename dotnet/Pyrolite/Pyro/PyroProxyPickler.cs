/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Razorvine.Pickle;

namespace Razorvine.Pyro
{

/// <summary>
/// Pickler extension to be able to pickle PyroProxy objects.
/// </summary>
public class PyroProxyPickler : IObjectPickler {

	public void pickle(object o, Stream outs, Pickler currentPickler) {
		PyroProxy proxy = (PyroProxy) o;
		outs.WriteByte(Opcodes.GLOBAL);
		byte[] output=Encoding.Default.GetBytes("Pyro4.core\nProxy\n");
		outs.Write(output,0,output.Length);
		outs.WriteByte(Opcodes.EMPTY_TUPLE);
		outs.WriteByte(Opcodes.NEWOBJ);
		
		// parameters are: pyroUri, pyroOneway(hashset), pyroTimeout
		object[] args = new object[] {   
			new PyroURI(proxy.objectid, proxy.hostname, proxy.port),
			new HashSet<object>(),
			0.0
		};
		currentPickler.save(args);
		outs.WriteByte(Opcodes.BUILD);
	}

	public static IDictionary ToSerpentDict(object obj)
	{
		PyroProxy proxy = (PyroProxy)obj;
		var dict = new Hashtable();
		string uri = string.Format("PYRO:{0}@{1}:{2}", proxy.objectid, proxy.hostname, proxy.port);
		dict["state"] = new object[]{uri, new HashSet<object>(), 0.0};
		dict["__class__"] = "Pyro4.core.Proxy";
		return dict;
	}
	
	public static object FromSerpentDict(IDictionary dict)
	{
		object[] state = (object[])dict["state"];  // pyroUri, onway(set), timeout
		PyroURI uri = new PyroURI((string)state[0]);
		return new PyroProxy(uri);
	}
}

}
