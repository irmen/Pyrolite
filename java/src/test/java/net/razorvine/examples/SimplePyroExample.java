package net.razorvine.examples;

import java.io.IOException;
import java.util.HashMap;
import java.util.Map;

import net.razorvine.pyro.*;

/**
 * Simple pyro example
 */
public class SimplePyroExample {

    @SuppressWarnings("unchecked")
	public static void main(String[] args) throws IOException
    {
    	// First, Make sure you have the builtin echoserver running, with name server enabled:
    	// $ python -m Pyro4.test.echoserver -Nv
    	// Then run this example client.
    	
    	// Config.PROTOCOL_VERSION = 45;		// uncomment this to enable talking to Pyro 4.20
    	
        NameServerProxy ns = NameServerProxy.locateNS(null);
        PyroProxy remoteobject = new PyroProxy(ns.lookup("test.echoserver"));

        
		// simple remote echo call
        String echomessage = (String) remoteobject.call("echo", "hello there");
        System.out.println("echo response: "+echomessage);
        
        // more complex call, pass a dict as argument
        Map<String, Object> argument = new HashMap<String, Object>();
        argument.put("value", 42);
        argument.put("message", "hello");
        argument.put("timestamp", new java.util.Date());
        Object obj = remoteobject.call("echo", argument);
        
        System.out.println("complex echo response: "+obj);
        Map<String, Object> result = (Map<String, Object>) obj;
        System.out.println("value="+result.get("value"));
        System.out.println("message="+result.get("messge"));
        System.out.println("timestamp=" +result.get("timestamp"));

        
        // error
        try {
        	remoteobject.call("error");
        } catch (PyroException e) {
        	System.out.println("Pyro Exception (expected)! "+e.getMessage());
        	System.out.println("Pyro Exception cause: "+e.getCause());
        	System.out.println("Pyro Exception remote traceback:\n" + e._pyroTraceback);
        }
        
        remoteobject.close();
        ns.close();
    }
}
