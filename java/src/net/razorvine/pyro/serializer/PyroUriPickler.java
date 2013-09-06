package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.io.OutputStream;
import java.util.HashMap;
import java.util.Map;

import net.razorvine.pickle.IObjectPickler;
import net.razorvine.pickle.Opcodes;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.Pickler;
import net.razorvine.pyro.PyroURI;
import net.razorvine.serpent.IClassSerializer;

/**
 * Pickler extension to be able to pickle Pyro URI objects.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroUriPickler implements IObjectPickler, IClassSerializer {

	public void pickle(Object o, OutputStream out, Pickler currentPickler) throws PickleException, IOException {
		PyroURI uri = (PyroURI) o;
		out.write(Opcodes.GLOBAL);
		out.write("Pyro4.core\nURI\n".getBytes());
		out.write(Opcodes.EMPTY_TUPLE);
		out.write(Opcodes.NEWOBJ);
		out.write(Opcodes.MARK);
		currentPickler.save(uri.protocol);
		currentPickler.save(uri.objectid);
		currentPickler.save(null);
		currentPickler.save(uri.host);
		currentPickler.save(uri.port);
		out.write(Opcodes.TUPLE);
		out.write(Opcodes.BUILD);
	}

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
