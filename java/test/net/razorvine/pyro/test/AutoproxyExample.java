package net.razorvine.pyro.test;

import java.io.IOException;
import java.io.UnsupportedEncodingException;

import net.razorvine.pickle.PrettyPrint;
import net.razorvine.pyro.Config;

import net.razorvine.pyro.PyroProxy;

/**
 * Simple example that shows the use of Pyro with an object returning proxies.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class AutoproxyExample {

	public static void main(String[] args) throws IOException {

		System.out.println("Testing Pyro autoproxy server (make sure it's running on localhost 9999)...");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);

		setConfig();
		
		PyroProxy p=new PyroProxy("localhost",51004,"example.autoproxy");

		Object result=p.call("createSomething", 42);
		System.out.println("return value:");
		PrettyPrint.print(result);
		PyroProxy resultproxy=(PyroProxy)result;
		resultproxy.call("speak", "hello from java");
	}
	
	static void setConfig() {
		String hmackey=System.getenv("PYRO_HMAC_KEY");
		String hmackey_property=System.getProperty("PYRO_HMAC_KEY");
		if(hmackey_property!=null) {
			hmackey=hmackey_property;
		}
		if(hmackey!=null && hmackey.length()>0) {
			try {
				Config.HMAC_KEY=hmackey.getBytes("UTF-8");
			} catch (UnsupportedEncodingException e) {
				Config.HMAC_KEY=null;
			}
		}
		String tracedir=System.getenv("PYRO_TRACE_DIR");
		if(System.getProperty("PYRO_TRACE_DIR")!=null) {
			tracedir=System.getProperty("PYRO_TRACE_DIR");
		}
		Config.MSG_TRACE_DIR=tracedir;
	}	
}
