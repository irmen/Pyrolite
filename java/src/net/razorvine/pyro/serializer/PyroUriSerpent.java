package net.razorvine.pyro.serializer;

import java.util.HashMap;
import java.util.Map;

import net.razorvine.pyro.PyroURI;
import net.razorvine.serpent.IClassSerializer;

/**
 * Serpent extension to be able to serialize Pyro URI objects with Serpent.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroUriSerpent implements IClassSerializer {

	public static Object FromSerpentDict(Map<Object, Object> dict) {
		Object[] state = (Object[])dict.get("state");  // protocol, objectid, socketname, hostname, port
		return new PyroURI((String)state[1], (String)state[3], (Integer)state[4]);
	}

	public Map<String, Object> convert(Object obj) {
		PyroURI uri = (PyroURI) obj;
		Map<String, Object> dict = new HashMap<String, Object>();
		dict.put("state", new Object[]{uri.protocol, uri.objectid, null, uri.host, uri.port});
		dict.put("__class__", "Pyro4.core.URI");
		return dict;
	}
}
