package net.razorvine.pyro;

import java.io.IOException;
import java.util.HashMap;

import net.razorvine.pickle.PickleException;

/**
 * Flame-Wrapper for a remote module. 
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class FlameModule {

	private PyroProxy flameserver;
	private String module;
	
	/**
	 * called by the Unpickler to restore state
	 */
	public void __setstate__(HashMap<?, ?> args) throws IOException {
		flameserver=(PyroProxy) args.get("flameserver");
		module=(String) args.get("module");
	}
	
	public Object call(String attribute, Object... arguments) throws PickleException, PyroException, IOException {
		return flameserver.call("invokeModule", module+"."+attribute, arguments, new HashMap<Object, Object>(0));
	}
	
	public void close()	{
		if(flameserver!=null)
			flameserver.close();
	}
	
	public void setHmacKey(byte[] hmac) {
		flameserver.pyroHmacKey = hmac;
	}	
}
