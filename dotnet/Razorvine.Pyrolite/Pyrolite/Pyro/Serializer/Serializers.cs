/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using Razorvine.Serpent;

// ReSharper disable InconsistentNaming

namespace Razorvine.Pyro.Serializer
{
	/// <summary>
	/// Abstract base class of all Pyro serializers.
	/// </summary>
	public abstract class PyroSerializer
	{
		public abstract byte serializer_id { get; }  // make sure this matches the id from Pyro

		public abstract byte[] serializeCall(string objectId, string method, object[] vargs, IDictionary<string, object> kwargs);
		public abstract byte[] serializeData(object obj);
		public abstract object deserializeData(byte[] data);

		// ReSharper disable once MemberCanBePrivate.Global
		protected static SerpentSerializer serpentSerializer;
		
		public static PyroSerializer GetSerpentSerializer()
		{
			// Create a serpent serializer if not yet created.
			// This is done dynamically so there is no assembly dependency on the Serpent assembly,
			// and it will become available once you copy that into the correct location.
			lock(typeof(SerpentSerializer))
			{
				if (serpentSerializer != null) return serpentSerializer;
				try {
					serpentSerializer = new SerpentSerializer();
					return serpentSerializer;
				} catch (TypeInitializationException x) {
					throw new PyroException("serpent serializer unavailable", x);
				}
			}
		}

		public static PyroSerializer GetFor(int serializer_id)
		{
			if(serializer_id == serpentSerializer?.serializer_id)
				return serpentSerializer;
			
			throw new ArgumentException("unsupported serializer id: "+serializer_id);
		}
	}

	
	/// <summary>
	/// Serializer using the serpent protocol.
	/// Uses dynamic access to the Serpent assembly types (with reflection) to avoid
	/// a required assembly dependency with that.
	/// </summary>
	public class SerpentSerializer : PyroSerializer
	{
		public override byte serializer_id => Message.SERIALIZER_SERPENT;

		static SerpentSerializer()
		{
			// register a few custom class-to-dict converters

			Serpent.Serializer.RegisterClass(typeof(PyroURI), PyroUriSerpent.ToSerpentDict);
			Serpent.Serializer.RegisterClass(typeof(PyroException), PyroExceptionSerpent.ToSerpentDict);
			Serpent.Serializer.RegisterClass(typeof(PyroProxy), PyroProxySerpent.ToSerpentDict);
		}
	
		// ReSharper disable once MemberCanBePrivate.Global
		// ReSharper disable once MemberCanBeMadeStatic.Global
		public object DictToInstance(IDictionary dict)
		{
			string classname = (string)dict["__class__"];
			bool isException = dict.Contains("__exception__") && (bool)dict["__exception__"];
			if(isException)
			{
				// map all exception types to the PyroException
				return PyroExceptionSerpent.FromSerpentDict(dict);
			}
			switch(classname)
			{
				case "Pyro5.core.URI":
					return PyroUriSerpent.FromSerpentDict(dict);
				case "Pyro5.client.Proxy":
					return PyroProxySerpent.FromSerpentDict(dict);
				default:
					return null;
			}
		}

		public override byte[] serializeData(object obj)
		{
			var ser = new Serpent.Serializer(Config.SERPENT_INDENT,true);
			return ser.Serialize(obj);
		}
		
		public override byte[] serializeCall(string objectId, string method, object[] vargs, IDictionary<string, object> kwargs)
		{
			var ser = new Serpent.Serializer(Config.SERPENT_INDENT,true);
			object[] invokeparams = {objectId, method, vargs, kwargs};
			return ser.Serialize(invokeparams);
		}
		
		public override object deserializeData(byte[] data)
		{
			var ast = new Parser().Parse(data);
			return ast.GetData(DictToInstance);
		}
		
		/**
		 * Utility function to convert obj back to actual bytes if it is a serpent-encoded bytes dictionary
		 * (a IDictionary with base-64 encoded 'data' in it and 'encoding'='base64').
		 * If obj is already a byte array, return obj unmodified.
		 * If it is something else, throw an IllegalArgumentException
		 * (implementation used of net.razorvine.serpent.Parser)
		 */
		public static byte[] ToBytes(object obj) => Parser.ToBytes(obj);
	}
}
