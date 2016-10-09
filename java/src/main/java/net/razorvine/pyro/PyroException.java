package net.razorvine.pyro;

import java.util.HashMap;
import java.util.List;

/**
 * Exception thrown when something is wrong in Pyro.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroException extends RuntimeException {

	private static final long serialVersionUID = 5164514665621511957L;
	public String _pyroTraceback;
	public String pythonExceptionType;

	public PyroException() {
		super();
	}

	public PyroException(String message, Throwable cause) {
		super(message, cause);
	}

	public PyroException(String message) {
		super(message);
	}

	/**
	 * called by the Unpickler to restore state
	 */
	public void __setstate__(HashMap<?, ?> args) {
		Object tb=args.get("_pyroTraceback");
		// if the traceback is a list of strings, create one string from it
		Class<?> componentType = tb.getClass().getComponentType();
		if(componentType!=null) {
			StringBuilder sb=new StringBuilder();
			for(Object line: (Object[])tb) {
				sb.append(line);
			}	
			_pyroTraceback=sb.toString();
		}
		else if(tb instanceof List) {
			StringBuilder sb=new StringBuilder();
			for(Object line: (List<?>)tb) {
				sb.append(line);
			}	
			_pyroTraceback=sb.toString();
		} else {
			_pyroTraceback=(String)tb;
		}
	}
}
