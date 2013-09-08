package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.util.Collections;
import java.util.HashMap;
import java.util.Map;

import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;
import net.razorvine.serpent.IClassSerializer;

/**
 * Serpent extension to be able to serialize PyroProxy objects with Serpent.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroProxySerpent implements IClassSerializer {

	public static Object FromSerpentDict(Map<Object, Object> dict) throws IOException {
		Object[] state = (Object[])dict.get("state");  // pyroUri, onway(set), timeout
		PyroURI uri = new PyroURI((String)state[0]);
		return new PyroProxy(uri);
	}

	public Map<String, Object> convert(Object obj) {
		PyroProxy proxy = (PyroProxy) obj;
		Map<String, Object> dict = new HashMap<String, Object>();
		String uri = String.format("PYRO:%s@%s:%d", proxy.objectid, proxy.hostname, proxy.port);
		dict.put("state", new Object[]{uri, Collections.EMPTY_SET, 0.0});
		dict.put("__class__", "Pyro4.core.Proxy");
		return dict;
	}
}
