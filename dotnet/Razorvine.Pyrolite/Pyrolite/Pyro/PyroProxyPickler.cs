/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

using Razorvine.Pickle;

namespace Razorvine.Pyro
{

/// <summary>
/// Pickler extension to be able to pickle PyroProxy objects.
/// </summary>
[SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
public class PyroProxyPickler : IObjectPickler {

	public void pickle(object o, Stream outs, Pickler currentPickler) {
		PyroProxy proxy = (PyroProxy) o;
		outs.WriteByte(Opcodes.GLOBAL);
		var output=Encoding.Default.GetBytes("Pyro4.core\nProxy\n");
		outs.Write(output,0,output.Length);
		outs.WriteByte(Opcodes.EMPTY_TUPLE);
		outs.WriteByte(Opcodes.NEWOBJ);
		
		// args(8): pyroUri, pyroOneway(hashset), pyroMethods(set), pyroAttrs(set), pyroTimeout, pyroHmacKey, pyroHandshake, pyroMaxRetries
		object[] args = {   
			new PyroURI(proxy.objectid, proxy.hostname, proxy.port),
			proxy.pyroOneway,
			proxy.pyroMethods,
			proxy.pyroAttrs,
			0.0,
			proxy.pyroHmacKey,
			proxy.pyroHandshake,
			0  // maxretries is not yet supported/used by pyrolite
		};
		currentPickler.save(args);
		outs.WriteByte(Opcodes.BUILD);
	}

	public static IDictionary ToSerpentDict(object obj)
	{
		// note: the state array returned here must conform to the list consumed by Pyro4's Proxy.__setstate_from_dict__ 
		// that means, we make an array of length 8:
		// uri, oneway set, methods set, attrs set, timeout, hmac_key, handshake, maxretries  (in this order)
		PyroProxy proxy = (PyroProxy)obj;
		var dict = new Hashtable();
		string uri = $"PYRO:{proxy.objectid}@{proxy.hostname}:{proxy.port}";
		string encodedHmac = proxy.pyroHmacKey!=null? "b64:"+Convert.ToBase64String(proxy.pyroHmacKey) : null;
		dict["state"] = new []{
			uri,
			proxy.pyroOneway,
			proxy.pyroMethods,
			proxy.pyroAttrs,
			0.0,
			encodedHmac,
			proxy.pyroHandshake,
			0  // maxretries is not yet supported/used by pyrolite
		};
		dict["__class__"] = "Pyro4.core.Proxy";
		return dict;
	}
	
	public static object FromSerpentDict(IDictionary dict)
	{
		// note: the state array received in the dict conforms to the list produced by Pyro4's Proxy.__getstate_for_dict__
		// that means, we must get an array of length 8:  (the same as with ToSerpentDict above!)
		// uri, oneway set, methods set, attrs set, timeout, hmac_key, handshake, maxretries  (in this order)
		var state = (object[])dict["state"];
		PyroURI uri = new PyroURI((string)state[0]);
		var proxy = new PyroProxy(uri);
		
		// the following nasty piece of code is similar to _processMetaData from the PyroProxy
		// this is because the three collections can either be an array or a set
		var onewayArray = state[1] as object[];
		var methodsArray = state[2] as object[];
		var attrsArray = state[3] as object[];
		if(onewayArray!=null)
			proxy.pyroOneway = new HashSet<string>(onewayArray.Select(o=>o as string));
		else if(state[1] is HashSet<string>)
			proxy.pyroOneway = (HashSet<string>) state[1];
		else
			proxy.pyroOneway = new HashSet<string> ((state[1] as HashSet<object>).Select(o=>o.ToString()));

		if(methodsArray!=null)
			proxy.pyroMethods = new HashSet<string>(methodsArray.Select(o=>o as string));
		else if(state[2] is HashSet<string>)
			proxy.pyroMethods = (HashSet<string>) state[2];
		else
			proxy.pyroMethods = new HashSet<string>((state[2] as HashSet<object>).Select(o=>o.ToString()));
		
		if(attrsArray!=null)
			proxy.pyroAttrs = new HashSet<string>(attrsArray.Select(o=>o as string));
		else if(state[3] is HashSet<string>)
			proxy.pyroAttrs = (HashSet<string>) state[3];
		else
			proxy.pyroAttrs = new HashSet<string>((state[3] as HashSet<object>).Select(o=>o.ToString()));

		if(state[5]!=null) {
			string encodedHmac = (string)state[5];
			if(encodedHmac.StartsWith("b64:", StringComparison.InvariantCulture)) {
				proxy.pyroHmacKey = Convert.FromBase64String(encodedHmac.Substring(4));
			} else {
				throw new PyroException("hmac encoding error");
			}
		}
		proxy.pyroHandshake = state[6];
		// maxretries is not used/supported in pyrolite, so simply ignore it

		return proxy;
	}
}

}
