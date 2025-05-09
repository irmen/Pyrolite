/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using Razorvine.Pyro;
// ReSharper disable CheckNamespace

namespace Pyrolite.TestPyroNaming;

/// <summary>
/// Test Pyro with the Pyro name server.
/// </summary>
public static class TestNaming {

	public static void Main()  {
		try {
			Test();
		} catch (Exception x) {
			Console.WriteLine("unhandled exception: {0}",x);
		}
		Console.WriteLine("\r\nEnter to exit:"); Console.ReadLine();
	}

	private static void Test() {

		Console.WriteLine("Testing Pyro nameserver connection (make sure it's running with a broadcast server)...");
		Console.WriteLine("Pyrolite version: "+Config.PYROLITE_VERSION);

		SetConfig();

		using var ns=NameServerProxy.locateNS(null);
		Console.WriteLine("discovered ns at "+ns.hostname+":"+ns.port);
		ns.ping();

		Console.WriteLine("lookup of name server object:");
		var uri = ns.lookup("Pyro.NameServer");
		Console.WriteLine("   "+uri);
		Console.WriteLine("lookup of name server object, with metadata:");
		var (item1, metadata) = ns.lookup("Pyro.NameServer", true);
		Console.WriteLine("   uri:  "+item1);
		Console.WriteLine("   meta: "+string.Join(", " ,metadata));
		metadata.Add("updated-by-dotnet-pyrolite");
		ns.set_metadata("Pyro.NameServer", metadata);
			
			
		Console.WriteLine("\nobjects registered in the name server, with metadata:");
		var objectsm = ns.list(null, null);
		foreach(string key in objectsm.Keys) {
			var registration = objectsm[key];
			Console.WriteLine(key + " --> " + registration.Item1);
			Console.WriteLine("      metadata: " + string.Join(", ", registration.Item2));
		}

		Console.WriteLine("\nobjects registered having all metadata:");
		var objects = ns.yplookup(new []{"blahblah", "class:Pyro5.nameserver.NameServer"}, null);
		foreach(string name in objects.Keys) {
			var entry = objectsm[name];
			Console.WriteLine(name + " --> " + entry.Item1);
			Console.WriteLine("      metadata: " + string.Join(", ", entry.Item2));
		}

		Console.WriteLine("\nobjects registered having any metadata:");
		objects = ns.yplookup(null, new []{"blahblah", "class:Pyro5.nameserver.NameServer"});
		foreach(string name in objects.Keys) {
			var entry = objectsm[name];
			Console.WriteLine(name + " --> " + entry.Item1);
			Console.WriteLine("      metadata: " + string.Join(", ", entry.Item2));
		}

		Console.WriteLine("");
		ns.register("dotnet.test", new PyroURI("PYRO:DotnetTest@localhost:9999"), false);
		ns.register("dotnet.testmeta", new PyroURI("PYRO:DotnetTest@localhost:9999"), false, new []{"example", "from-dotnet-pyrolite"});

		Console.WriteLine("uri=" + ns.lookup("dotnet.test"));
		Console.WriteLine("using a new proxy to call the nameserver.");
			
		using(var p=new PyroProxy(ns.lookup("Pyro.NameServer")))
		{
			p.call("ping");
		}
	
		int numRemoved=ns.remove(null, "dotnet.", null);
		Console.WriteLine("number of removed entries: {0}",numRemoved);
			
		try {
			Console.WriteLine("uri=" + ns.lookup("dotnet.test"));	 // should fail....
		} catch (PyroException x) {
			// ok
			Console.WriteLine("got a PyroException (expected): {0}", x.Message);
		}
	}

	private static void SetConfig()
	{
		string tracedir=Environment.GetEnvironmentVariable("PYRO_TRACE_DIR");
		if(tracedir!=null) {
			Config.MSG_TRACE_DIR=tracedir;
		}
	}
}