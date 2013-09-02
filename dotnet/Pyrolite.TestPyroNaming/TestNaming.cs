/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections.Generic;
using System.Text;
using Razorvine.Pickle;
using Razorvine.Pyro;

namespace Pyrolite.TestPyroNaming
{

/// <summary>
/// Test Pyro with the Pyro name server.
/// </summary>
public class TestNaming {

	public static void Main(String[] args)  {
		try {
			Test();
		} catch (Exception x) {
			Console.WriteLine("unhandled exception: {0}",x);
		}
	}
	
	public static void Test() {

		Console.WriteLine("Testing Pyro nameserver connection (make sure it's running with a broadcast server)...");
		Console.WriteLine("Pyrolite version: "+Config.PYROLITE_VERSION);

		setConfig();
		Console.WriteLine("serializer used: {0}", Config.SERIALIZER);
		if(Config.SERIALIZER==Config.SerializerType.serpent)
			Console.WriteLine("note that for the serpent serializer, you need to have the Razorvine.Serpent assembly available.");

		NameServerProxy ns=NameServerProxy.locateNS(null);
		Console.WriteLine("discovered ns at "+ns.hostname+":"+ns.port);
		ns.ping();

		Console.WriteLine("objects registered in the name server:");
		IDictionary<string,string> objects = ns.list(null, null);
		foreach(string key in objects.Keys) {
			Console.WriteLine(key + " --> " + objects[key]);
		}
	
		ns.register("java.test", new PyroURI("PYRO:JavaTest@localhost:9999"), false);
		Console.WriteLine("uri=" + ns.lookup("java.test"));
		Console.WriteLine("using a new proxy to call the nameserver.");
		PyroProxy p=new PyroProxy(ns.lookup("Pyro.NameServer"));
		p.call("ping");

		int num_removed=ns.remove(null, "java.", null);
		Console.WriteLine("number of removed entries: {0}",num_removed);
		
		try {
			Console.WriteLine("uri=" + ns.lookup("java.test"));	 // should fail....
		} catch (PyroException x) {
			// ok
			Console.WriteLine("got a PyroException (expected): {0}", x.Message);
		}

	}
	
	static void setConfig()
	{
		string hmackey=Environment.GetEnvironmentVariable("PYRO_HMAC_KEY");
		if(hmackey!=null) {
			Config.HMAC_KEY=Encoding.UTF8.GetBytes(hmackey);
		}
		string tracedir=Environment.GetEnvironmentVariable("PYRO_TRACE_DIR");
		if(tracedir!=null) {
			Config.MSG_TRACE_DIR=tracedir;
		}
		string serializer=Environment.GetEnvironmentVariable("PYRO_SERIALIZER");
		if(serializer!=null) {
			Config.SERIALIZER=(Config.SerializerType) Enum.Parse(typeof(Config.SerializerType), serializer, true);
		}
	}
}

}

