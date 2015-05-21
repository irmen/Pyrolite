package net.razorvine.pyro;

import java.io.IOException;

/**
 * Dummy class to be able to unpickle Pyro Proxy objects.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class DummyPyroSerializer {
	/**
	 * called by the Unpickler to restore state
	 */
	public void __setstate__(java.util.HashMap<?,?> args) throws IOException {
	}
}
