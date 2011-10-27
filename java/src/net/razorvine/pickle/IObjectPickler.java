package net.razorvine.pickle;

import java.io.IOException;
import java.io.OutputStream;

/**
 * Interface for Object Picklers used by the pickler, to pickle custom classes. 
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public interface IObjectPickler {
	/**
	 * Pickle an object.
	 */
	public void pickle(Object o, OutputStream out, Pickler currentPickler) throws PickleException, IOException;
}
