using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Razorvine.Pyro.Serializer
{
    public static class PyroProxySerpent
    {
	    public static IDictionary ToSerpentDict(object obj)
		{
			// note: the state array returned here must conform to the list consumed by Pyro's Proxy.__setstate_from_dict__ 
			// that means, we make an array of length 8:
			// uri, oneway set, methods set, attrs set, timeout, handshake, maxretries  (in this order)
			var proxy = (PyroProxy)obj;
			var dict = new Hashtable();
			string uri = $"PYRO:{proxy.objectid}@{proxy.hostname}:{proxy.port}";
			dict["state"] = new []{
				uri,
				proxy.pyroOneway,
				proxy.pyroMethods,
				proxy.pyroAttrs,
				0.0,
				proxy.pyroHandshake,
				0  // maxretries is not yet supported/used by pyrolite
			};
			dict["__class__"] = "Pyro5.client.Proxy";
			return dict;
		}
		
		public static object FromSerpentDict(IDictionary dict)
		{
			// note: the state array received in the dict conforms to the list produced by Pyro's Proxy.__getstate_for_dict__
			// that means, we must get an array of length 8:  (the same as with ToSerpentDict above!)
			// uri, oneway set, methods set, attrs set, timeout, handshake, maxretries  (in this order)
			var state = (object[])dict["state"];
			var uri = new PyroURI((string)state[0]);
			var proxy = new PyroProxy(uri);
			
			// the following nasty piece of code is similar to _processMetaData from the PyroProxy
			// this is because the three collections can either be an array or a set
			var methodsArray = state[2] as object[];
			var attrsArray = state[3] as object[];
			proxy.pyroOneway = state[1] switch
			{
				object[] onewayArray => new HashSet<string>(onewayArray.Select(o => o as string)),
				HashSet<string> => (HashSet<string>)state[1],
				_ => new HashSet<string>((state[1] as HashSet<object>).Select(o => o.ToString()))
			};

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

			proxy.pyroHandshake = state[5];
			// maxretries is not used/supported in pyrolite, so simply ignore it

			return proxy;
		}
    }
}