/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using Razorvine.Pickle;

namespace Razorvine.Pyro
{
	/// <summary>
	/// Construct a PyroProxy class. Used for the pickle serializer.
	/// </summary>
	public class ProxyClassConstructor : IObjectConstructor
	{
		public object construct(object[] args)
		{
			if(args.Length==0) {
				// no-arg constructor
				return new PyroProxy();
			}

			if(args.Length==1 && args[0] is PyroURI) {
				// constructor with PyroURI arg
				return new PyroProxy((PyroURI)args[0]);
			}

			if (args.Length != 3) throw new PickleException("invalid args for PyroProxy unpickling");
			// constructor with hostname,port,objectid args
			string hostname=(string)args[0];
			int port=(int)args[1];
			string objectId=(string)args[2];
			return new PyroProxy(hostname, port, objectId);

		}
	}
}
