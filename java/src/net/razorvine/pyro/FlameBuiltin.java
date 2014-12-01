package net.razorvine.pyro;

import java.io.IOException;
import java.util.HashMap;

import net.razorvine.pickle.PickleException;

/**
 * Flame-Wrapper for a builtin function. 
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class FlameBuiltin {

	private PyroProxy flameserver;
	private String builtin;
	
	/**
	 * called by the Unpickler to restore state
	 */
	public void __setstate__(HashMap<?, ?> args) throws IOException {
		flameserver=(PyroProxy) args.get("flameserver");
		builtin=(String) args.get("builtin");
	}
	
	public Object call(Object... arguments) throws PickleException, PyroException, IOException {
		return flameserver.call("invokeBuiltin", builtin, arguments, new HashMap<Object, Object>(0));
	}

	public void close()	{
		if(flameserver!=null)
			flameserver.close();
	}
	
	public void setHmacKey(byte[] hmac) {
		flameserver.pyroHmacKey = hmac;
	}
}
