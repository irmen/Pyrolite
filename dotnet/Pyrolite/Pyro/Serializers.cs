/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */
	
using System;
using System.Collections.Generic;
using System.Reflection;

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

		protected static IDictionary<Config.SerializerType, PyroSerializer> AvailableSerializers = new Dictionary<Config.SerializerType, PyroSerializer>();
		
		protected static readonly PickleSerializer pickleSerializer = new PickleSerializer();
		protected static SerpentSerializer serpentSerializer = null;
		
		public static PyroSerializer GetFor(Config.SerializerType type)
		{
			switch(type)
			{
				case Config.SerializerType.pickle:
					return pickleSerializer;
				case Config.SerializerType.serpent:
					{
						// Create a serpent serializer if not yet created.
						// This is done dynamically so there is no assembly dependency on the Serpent assembly,
						// and it will become available once you copy that into the correct location.
						lock(typeof(SerpentSerializer))
						{
							if(serpentSerializer==null)
							{
								try {
									serpentSerializer = new SerpentSerializer();
									return serpentSerializer;
								} catch (TypeInitializationException x) {
									throw new PyroException("serpent serializer unavailable", x);
								}
							}
							return serpentSerializer;
						}
					}
				default:
					throw new ArgumentException("unrecognised serializer type: "+type);
			}
		}
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
	
	
	/// <summary>
	/// Serializer using the serpent protocol.
	/// Uses dynamic access to the Serpent assembly types (with reflection) to avoid
	/// a required assembly dependency with that.
	/// </summary>
	public class SerpentSerializer : PyroSerializer
	{
		private static MethodInfo serializeMethod;
		private static MethodInfo parseMethod;
		private static MethodInfo astGetDataMethod;
		private static Type serpentSerializerType;
		private static Type serpentParserType;

		public override ushort serializer_id {
			get {
				return 1; // make sure this matches the id from Pyro
			}
		}
		
		static SerpentSerializer()
		{
			Assembly serpentAssembly = Assembly.Load("Razorvine.Serpent");
			serpentSerializerType = serpentAssembly.GetType("Razorvine.Serpent.Serializer");
			serpentParserType = serpentAssembly.GetType("Razorvine.Serpent.Parser");
			Type astType = serpentAssembly.GetType("Razorvine.Serpent.Ast");
			
			serializeMethod = serpentSerializerType.GetMethod("Serialize", new Type[] {typeof(object)});
			parseMethod = serpentParserType.GetMethod("Parse", new Type[] {typeof(byte[])});
			astGetDataMethod = astType.GetMethod("GetData");
		}
		
		public override byte[] serializeData(object obj)
		{
			// call the "Serialize" method, using reflection
			object serializer = Activator.CreateInstance(serpentSerializerType, new object[] {Config.SERPENT_INDENT, Config.SERPENT_SET_LITERALS});
			return (byte[]) serializeMethod.Invoke(serializer, new object[] {obj});
		}
		
		public override byte[] serializeCall(string objectId, string method, object[] vargs, IDictionary<string, object> kwargs)
		{
			object[] invokeparams = new object[] {objectId, method, vargs, kwargs};
			// call the "Serialize" method, using reflection
			object serializer = Activator.CreateInstance(serpentSerializerType, new object[] {Config.SERPENT_INDENT, Config.SERPENT_SET_LITERALS});
			return (byte[]) serializeMethod.Invoke(serializer, new object[] {invokeparams});
		}
		
		public override object deserializeData(byte[] data)
		{
			// call the "Parse" method, using reflection
			object parser = Activator.CreateInstance(serpentParserType);
			object ast = parseMethod.Invoke(parser, new object[] {data});
			// call the "GetData" method on the Ast, using reflection
			return astGetDataMethod.Invoke(ast, null);
		}
	}
}
