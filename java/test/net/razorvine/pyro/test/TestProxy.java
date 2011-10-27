package net.razorvine.pyro.test;

import java.io.IOException;
import java.util.Map;

import net.razorvine.pyro.*;


/**
 * Test Pyro with the Pyro name server.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class TestProxy {

	public static void main(String[] args) throws IOException {

		System.out.println("Testing Pyro nameserver connection (make sure it's running with a broadcast server)...");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);

		// Config.HMAC_KEY="irmen".getBytes();
		NameServerProxy ns=NameServerProxy.locateNS(null);
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
		p.call("ping");

		System.out.println(ns.remove(null, "java.", null));
		try {
			System.out.println("uri=" + ns.lookup("java.test"));
			 // should fail....
		} catch (PyroException x) {
			// ok
		}

	}
}
