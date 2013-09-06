package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.io.OutputStream;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import net.razorvine.pickle.IObjectPickler;
import net.razorvine.pickle.Opcodes;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.Pickler;
import net.razorvine.pyro.PyroException;
import net.razorvine.serpent.IClassSerializer;

/**
 * Pickler extension to be able to pickle PyroException objects.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroExceptionPickler implements IObjectPickler, IClassSerializer {

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

	@SuppressWarnings("unchecked")
	public static Object FromSerpentDict(Map<Object, Object> dict) {
		Object[] args = (Object[]) dict.get("args");
		PyroException ex = new PyroException((String)args[0]);
		Map<String, Object> attrs = (Map<String, Object>)dict.get("attributes");
		// we can only deal with a possible _pyroTraceback attribute
		if(attrs.containsKey("_pyroTraceback"))
		{
			Object tb = attrs.get("_pyroTraceback");
			// if the traceback is a list of strings, create one string from it
			if(tb instanceof List) {
				StringBuilder sb=new StringBuilder();
				for(Object line: (List<?>)tb) {
					sb.append(line);
				}	
				ex._pyroTraceback=sb.toString();
			} else {
				ex._pyroTraceback=(String)tb;
			}
		}
		return ex;
	}

	public Map<String, Object> convert(Object obj) {
		PyroException ex = (PyroException) obj;
		Map<String, Object> dict = new HashMap<String, Object>();
		// {'attributes':{},'__exception__':True,'args':('hello',),'__class__':'PyroError'}
		dict.put("__class__", "PyroError");
		dict.put("__exception__", true);
		dict.put("args", new Object[] {ex.getMessage()});
		Map<String, Object> attrMap = new HashMap<String,Object>();
		if(ex._pyroTraceback!=null)
			attrMap.put("_pyroTraceback", new String[]{ ex._pyroTraceback} );   // transform single string back into list
		dict.put("attributes", attrMap);
		return dict;
	}
}
