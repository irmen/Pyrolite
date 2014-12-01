package net.razorvine.pyro.test;

import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.util.Map;

import net.razorvine.pyro.*;


/**
 * Test Pyro with the Pyro name server.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class NamingExample {

	static protected byte[] hmacKey;	// just ignore this if you don't specify a PYRO_HMAC_KEY environment var

	public static void main(String[] args) throws IOException {

		System.out.println("Testing Pyro nameserver connection (make sure it's running with a broadcast server)...");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);

		setConfig();

		NameServerProxy ns=NameServerProxy.locateNS(null, hmacKey);
		System.out.println("discovered ns at "+ns.hostname+":"+ns.port);
		ns.ping();

		System.out.println("objects registered in the name server:");
		Map<String, String> objects = ns.list(null, null);
		for (String name : objects.keySet()) {
			System.out.println(name + " --> " + objects.get(name));
		}
		
		ns.register("java.test", new PyroURI("PYRO:JavaTest@localhost:9999"), false);
		System.out.println("uri=" + ns.lookup("java.test"));
		System.out.println("using a new proxy to call the nameserver.");
		PyroProxy p=new PyroProxy(ns.lookup("Pyro.NameServer"));
		p.pyroHmacKey = hmacKey;
		p.call("ping");

		int num_removed=ns.remove(null, "java.", null);
		System.out.println("number of removed entries: "+num_removed);
		try {
			System.out.println("uri=" + ns.lookup("java.test"));
			 // should fail....
		} catch (PyroException x) {
			// ok
			System.out.println("got a Pyro Exception (expected): "+x);
		}
		
		p.close();
		ns.close();
	}

	static void setConfig() {
		String hmackey=System.getenv("PYRO_HMAC_KEY");
		String hmackey_property=System.getProperty("PYRO_HMAC_KEY");
		if(hmackey_property!=null) {
			hmackey=hmackey_property;
		}
		if(hmackey!=null && hmackey.length()>0) {
			try {
				hmacKey=hmackey.getBytes("UTF-8");
			} catch (UnsupportedEncodingException e) {
				hmacKey=null;
			}
		}
		String tracedir=System.getenv("PYRO_TRACE_DIR");
		if(System.getProperty("PYRO_TRACE_DIR")!=null) {
			tracedir=System.getProperty("PYRO_TRACE_DIR");
		}

		String serializer=System.getenv("PYRO_SERIALIZER");
		if(System.getProperty("PYRO_SERIALIZER")!=null) {
			serializer=System.getProperty("PYRO_SERIALIZER");
		}
		if(serializer!=null) {
			Config.SERIALIZER = Enum.valueOf(Config.SerializerType.class, serializer);
		}

		Config.MSG_TRACE_DIR=tracedir;
	}
}
