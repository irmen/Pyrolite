package net.razorvine.pickle;

import java.util.HashMap;
import java.util.List;

/**
 * Exception thrown that represents a certain Python exception.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PythonException extends RuntimeException {
	private static final long serialVersionUID = 5104356796885969838L;

	public String _pyroTraceback;
	public String pythonExceptionType;

	public PythonException(String message, Throwable cause) {
		super(message, cause);
	}

	public PythonException(String message) {
		super(message);
	}
	
	public PythonException(Throwable cause)
	{
		super(cause);
	}

	public PythonException()
	{
		super();
	}
	
	// special constructor for UnicodeDecodeError
	public PythonException(String encoding, byte[] data, Integer i1, Integer i2, String message)
	{
		super("UnicodeDecodeError: "+encoding+": "+message);
	}
	
	/**
	 * called by the unpickler to restore state
	 */
	public void __setstate__(HashMap<String,Object> args) {
		Object tb=args.get("_pyroTraceback");
		// if the traceback is a list of strings, create one string from it
		if(tb instanceof List) {
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
