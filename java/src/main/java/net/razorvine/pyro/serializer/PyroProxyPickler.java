package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.io.OutputStream;

import net.razorvine.pickle.IObjectPickler;
import net.razorvine.pickle.Opcodes;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.Pickler;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;

/**
 * Pickler extension to be able to pickle PyroProxy objects.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroProxyPickler implements IObjectPickler {

	public void pickle(Object o, OutputStream out, Pickler currentPickler) throws PickleException, IOException {
		PyroProxy proxy = (PyroProxy) o;
		out.write(Opcodes.GLOBAL);
		byte[] output="Pyro4.core\nProxy\n".getBytes();
		out.write(output,0,output.length);
		out.write(Opcodes.EMPTY_TUPLE);
		out.write(Opcodes.NEWOBJ);
		
		// args(8): pyroUri, pyroOneway(hashset), pyroMethods(set), pyroAttrs(set), pyroTimeout, pyroHmacKey, pyroHandshake, pyroMaxRetries
		Object[] args = new Object[] {   
			new PyroURI(proxy.objectid, proxy.hostname, proxy.port),
			proxy.pyroOneway,
			proxy.pyroMethods,
			proxy.pyroAttrs,
			0.0,
			proxy.pyroHmacKey,
			proxy.pyroHandshake,
			0  // maxretries is not yet used/supported by pyrolite
		};
		currentPickler.save(args);
		out.write(Opcodes.BUILD);
	}
}
