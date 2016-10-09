package net.razorvine.pyro.serializer;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

import net.razorvine.pyro.PyroException;
import net.razorvine.serpent.IClassSerializer;

/**
 * Serpent extension to be able to serialize PyroException objects with Serpent.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroExceptionSerpent implements IClassSerializer {

	@SuppressWarnings("unchecked")
	public static Object FromSerpentDict(Map<Object, Object> dict) {
		Object[] args = (Object[]) dict.get("args");
		
		String pythonExceptionType = (String) dict.get("__class__");
		PyroException ex;
		if(args.length==0) {
			if(pythonExceptionType==null)
				ex = new PyroException();
			else
				ex = new PyroException("["+pythonExceptionType+"]");
		} else {
			if(pythonExceptionType==null)
				ex = new PyroException((String)args[0]);
			else
				ex = new PyroException("["+pythonExceptionType+"] "+(String)args[0]);
		}
		
		ex.pythonExceptionType = pythonExceptionType;

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
