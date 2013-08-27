/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */
	
using System;
using System.Collections.Generic;
using Razorvine.Pickle;
using Razorvine.Pickle.Objects;

namespace Razorvine.Pyro
{
	/// <summary>
	/// Abstract base class of all Pyro serializers.
	/// </summary>
	public abstract class PyroSerializer
	{
		public abstract ushort serializer_id { get; }  // make sure this matches the id from Pyro

		public abstract byte[] serializeCall(string objectId, string method, object[] vargs, IDictionary<string, object> kwargs);
		public abstract byte[] serializeData(object obj);
		public abstract object deserializeData(byte[] data);
	}

	
	/// <summary>
	/// Serializer using the pickle protocol.
	/// </summary>
	public class PickleSerializer : PyroSerializer
	{
		public override ushort serializer_id {
			get {
				return 4;  // make sure this matches the id from Pyro
			}
		}
		
		static PickleSerializer() {
			Unpickler.registerConstructor("Pyro4.errors", "PyroError", new AnyClassConstructor(typeof(PyroException)));
			Unpickler.registerConstructor("Pyro4.errors", "CommunicationError", new AnyClassConstructor(typeof(PyroException)));
			Unpickler.registerConstructor("Pyro4.errors", "ConnectionClosedError", new AnyClassConstructor(typeof(PyroException)));
			Unpickler.registerConstructor("Pyro4.errors", "TimeoutError", new AnyClassConstructor(typeof(PyroException)));
			Unpickler.registerConstructor("Pyro4.errors", "ProtocolError", new AnyClassConstructor(typeof(PyroException)));
			Unpickler.registerConstructor("Pyro4.errors", "NamingError", new AnyClassConstructor(typeof(PyroException)));
			Unpickler.registerConstructor("Pyro4.errors", "DaemonError", new AnyClassConstructor(typeof(PyroException)));
			Unpickler.registerConstructor("Pyro4.errors", "SecurityError", new AnyClassConstructor(typeof(PyroException)));
			Unpickler.registerConstructor("Pyro4.errors", "AsyncResultTimeout",	new AnyClassConstructor(typeof(PyroException)));
			Unpickler.registerConstructor("Pyro4.core", "Proxy", new ProxyClassConstructor());
			Unpickler.registerConstructor("Pyro4.util", "Serializer", new AnyClassConstructor(typeof(DummyPyroSerializer)));
			Unpickler.registerConstructor("Pyro4.utils.flame", "FlameBuiltin", new AnyClassConstructor(typeof(FlameBuiltin)));
			Unpickler.registerConstructor("Pyro4.utils.flame", "FlameModule", new AnyClassConstructor(typeof(FlameModule)));
			Unpickler.registerConstructor("Pyro4.utils.flame", "RemoteInteractiveConsole", new AnyClassConstructor(typeof(FlameRemoteConsole)));
			Unpickler.registerConstructor("Pyro4.core", "URI", new AnyClassConstructor(typeof(PyroURI)));
			Pickler.registerCustomPickler(typeof(PyroURI), new PyroUriPickler());
		}

		public override byte[] serializeCall(string objectId, string method, object[] vargs, IDictionary<string, object> kwargs)
		{
			using(var p=new Pickler())
			{
				object[] invokeparams = new object[] {objectId, method, vargs, kwargs};
				return p.dumps(invokeparams);
			}
		}
		
		public override byte[] serializeData(object obj)
		{
			using(var p=new Pickler())
			{
				return p.dumps(obj);
			}
		}

		public override object deserializeData(byte[] data)
		{
			using(var u=new Unpickler())
			{
				return u.loads(data);
			}
		}
	}
}
