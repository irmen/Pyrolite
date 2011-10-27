/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using Razorvine.Pyrolite.Pickle;

namespace Razorvine.Pyrolite.Pyro
{
	/// <summary>
	/// Description of ProxyClassConstructor.
	/// </summary>
	public class ProxyClassConstructor : IObjectConstructor
	{
		public object construct(object[] args) {
			if(args.Length==0) {
				// no-arg constructor
				return new PyroProxy();
			} else if(args.Length==1 && args[0] is PyroURI) {
				// constructor with PyroURI arg
				return new PyroProxy((PyroURI)args[0]);
			} else if(args.Length==3) {
				// constructor with hostname,port,objectid args
				String hostname=(String)args[0];
				int port=(int)args[1];
				String objectId=(String)args[2];
				return new PyroProxy(hostname, port, objectId);
			} else {
				throw new PickleException("invalid args for PyroProxy unpickling");
			}
		}
	}
}
