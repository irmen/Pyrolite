package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.io.OutputStream;

import net.razorvine.pickle.IObjectPickler;
import net.razorvine.pickle.Opcodes;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.Pickler;
import net.razorvine.pyro.PyroURI;

/**
 * Pickler extension to be able to pickle Pyro URI objects.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroUriPickler implements IObjectPickler {

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
}
