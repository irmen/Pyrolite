package net.razorvine.examples;

import java.io.IOException;
import java.util.Map;
import java.util.Set;

import net.razorvine.pyro.*;


/**
 * Test Pyro with the Pyro name server.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class NamingExample {

	public static void main(String[] args) throws IOException {

		System.out.println("Testing Pyro nameserver connection (make sure it's running with a broadcast server)...");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);

		setConfig();

		NameServerProxy ns=NameServerProxy.locateNS(null);
		System.out.println("discovered ns at "+ns.hostname+":"+ns.port);
		ns.ping();

		System.out.println("lookup of name server object:");
		PyroURI uri = ns.lookup("Pyro.NameServer");
		System.out.println("   "+uri);
		System.out.println("lookup of name server object, with metadata:");
		Object[] lookupresult = ns.lookup("Pyro.NameServer", true);
		@SuppressWarnings("unchecked")
		Set<String> metadata = (Set<String>) lookupresult[1];
		System.out.println("   "+lookupresult[0]);
		System.out.println("   "+metadata);
		metadata.add("updated-by-java-pyrolite");
		ns.set_metadata("Pyro.NameServer", metadata);

		System.out.println("\nobjects registered in the name server, with metadata:");
		Map<String, Object[]> objects_meta = ns.list(null, null);
		for (String name : objects_meta.keySet()) {
			Object[] entry = objects_meta.get(name);
			String uri_m = (String) entry[0];
			@SuppressWarnings("unchecked")
			Set<String> metadata_m = (Set<String>) entry[1];
			System.out.println(name + " --> " + uri_m);
			System.out.println("      metadata: " + metadata_m);
		}

		System.out.println("\nobjects registered having all metadata:");
		Map<String, Object[]> objectsm = ns.yplookup(new String[] {"blahblah", "class:Pyro5.nameserver.NameServer"}, null);
		for (String name : objectsm.keySet()) {
			Object[] entry = objectsm.get(name);
			String uri_m = (String) entry[0];
			@SuppressWarnings("unchecked")
			Set<String> metadata_m = (Set<String>) entry[1];
			System.out.println(name + " --> " + uri_m);
			System.out.println("      metadata: " + metadata_m);
		}

		System.out.println("\nobjects registered having any metadata:");
		objectsm = ns.yplookup(null, new String[] {"blahblah", "class:Pyro5.nameserver.NameServer"});
		for (String name : objectsm.keySet()) {
			Object[] entry = objectsm.get(name);
			String uri_m = (String) entry[0];
			@SuppressWarnings("unchecked")
			Set<String> metadata_m = (Set<String>) entry[1];
			System.out.println(name + " --> " + uri_m);
			System.out.println("      metadata: " + metadata_m);
		}

		System.out.println("");
		ns.register("java.test", new PyroURI("PYRO:JavaTest@localhost:9999"), false);
		ns.register("java.testmeta", new PyroURI("PYRO:JavaTest@localhost:9999"), false, new String[]{"example", "from-java-pyrolite"});
		System.out.println("uri=" + ns.lookup("java.test"));
		System.out.println("using a new proxy to call the nameserver.");
		PyroProxy p=new PyroProxy(ns.lookup("Pyro.NameServer"));
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
		String tracedir=System.getenv("PYRO_TRACE_DIR");
		if(System.getProperty("PYRO_TRACE_DIR")!=null) {
			tracedir=System.getProperty("PYRO_TRACE_DIR");
		}

		Config.MSG_TRACE_DIR=tracedir;
	}
}
