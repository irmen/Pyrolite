package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.util.Map;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.Message;
import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;
import net.razorvine.serpent.IDictToInstance;
import net.razorvine.serpent.Parser;
import net.razorvine.serpent.Serializer;
import net.razorvine.serpent.ast.Ast;

public class SerpentSerializer extends PyroSerializer {

	static {
		Serializer.registerClass(PyroURI.class, new PyroUriSerpent());
		Serializer.registerClass(PyroException.class, new PyroExceptionSerpent());
		Serializer.registerClass(PyroProxy.class, new PyroProxySerpent());
	}

	@Override
	public int getSerializerId() {
		return Message.SERIALIZER_SERPENT; 
	}

	@Override
	public byte[] serializeCall(String objectId, String method, Object[] vargs, Map<String, Object> kwargs) throws IOException {
		Serializer s = new Serializer(Config.SERPENT_INDENT, Config.SERPENT_SET_LITERALS, true);
		Object[] invokeparams = new Object[] {objectId, method, vargs, kwargs};
		return s.serialize(invokeparams);
	}

	@Override
	public byte[] serializeData(Object obj) throws IOException {
		Serializer s = new Serializer(Config.SERPENT_INDENT, Config.SERPENT_SET_LITERALS, true);
		return s.serialize(obj);
	}

	@Override
	public Object deserializeData(byte[] data) throws IOException {
		Parser p = new Parser();
		Ast ast = p.parse(data);
		IDictToInstance dictConverter = new DictConverter();
		return ast.getData(dictConverter);
	}
	
	class DictConverter implements IDictToInstance
	{
		public Object convert(Map<Object, Object> dict) throws IOException {
			String classname = (String)dict.get("__class__");
			boolean isException = dict.containsKey("__exception__") && (Boolean)dict.get("__exception__");
			if(isException)
			{
				// map all exception types to the PyroException
				return PyroExceptionSerpent.FromSerpentDict(dict);
			}
			if("Pyro4.core.URI".equals(classname))
				return PyroUriSerpent.FromSerpentDict(dict);
			else if("Pyro4.core.Proxy".equals(classname))
				return PyroProxySerpent.FromSerpentDict(dict);
			else
				return null;
		}
	}
	
	/**
	 * Utility function to convert obj back to actual bytes if it is a serpent-encoded bytes dictionary
	 * (a IDictionary with base-64 encoded 'data' in it and 'encoding'='base64').
	 * If obj is already a byte array, return obj unmodified.
	 * If it is something else, throw an IllegalArgumentException
	 * (implementation used of net.razorvine.serpent.Parser)
	 */
	public static byte[] toBytes(Object obj) {
		return net.razorvine.serpent.Parser.toBytes(obj);
	}
}
