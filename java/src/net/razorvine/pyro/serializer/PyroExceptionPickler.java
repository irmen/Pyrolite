package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.io.OutputStream;
import java.util.HashMap;
import java.util.Map;

import net.razorvine.pickle.IObjectPickler;
import net.razorvine.pickle.Opcodes;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.Pickler;
import net.razorvine.pyro.PyroException;

/**
 * Pickler extension to be able to pickle PyroException objects.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroExceptionPickler implements IObjectPickler {

	public void pickle(Object o, OutputStream out, Pickler currentPickler) throws PickleException, IOException {
		PyroException error = (PyroException) o;
		out.write(Opcodes.GLOBAL);
		byte[] output="Pyro4.errors\nPyroError\n".getBytes();
		out.write(output,0,output.length);
		Object[] args = new Object[] { error.getMessage() };
		currentPickler.save(args);
		out.write(Opcodes.REDUCE);
		if(error._pyroTraceback!=null)
		{
			// add _pyroTraceback attribute to the output
			Map<String, Object> tb = new HashMap<String, Object>();
			tb.put("_pyroTraceback", new String[]{ error._pyroTraceback });		// transform single string back into list
			currentPickler.save(tb);
			out.write(Opcodes.BUILD);
		}		
	}
}
