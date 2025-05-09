/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using Razorvine.Pyro;
// ReSharper disable CheckNamespace

namespace Pyrolite.TestPyroEcho;

/// <summary>
/// This custom proxy adds custom annotations to the pyro messages
/// </summary>
// ReSharper disable once ArrangeTypeModifiers
// ReSharper disable once UnusedMember.Global
class CustomProxy(PyroURI uri) : PyroProxy(uri)
{
	public override IDictionary<string, byte[]> annotations()
	{
		var ann = base.annotations();
		ann["XYZZ"] = Encoding.UTF8.GetBytes("A custom annotation!");
		return ann;
	}
}
	
/// <summary>
/// Test Pyro with the Pyro echo server. 
/// </summary>
public static class TestEcho {
	
	public static void Run() {

		Console.WriteLine("Testing Pyro echo server (make sure it's running, with nameserver enabled)...");
		Console.WriteLine("Pyrolite version: "+Config.PYROLITE_VERSION);

		var ns = NameServerProxy.locateNS(null);
		using dynamic p = new PyroProxy(ns.lookup("test.echoserver"));
		p.pyroHandshake = "banana";
			
		// non-dynamic way of constructing a proxy is:
		// PyroProxy p=new PyroProxy("localhost",9999,"test.echoserver");
	
		Console.WriteLine("echo(), param=42:");
		object result=p.echo(42);
		Console.WriteLine("return value:");
		Console.WriteLine(result);
			
		Console.WriteLine("oneway_echo(), param=999:");
		result=p.oneway_echo(999);
		Console.WriteLine("return value:");
		Console.WriteLine(result);
			
		// attribute access
		result = p.verbose;
		bool verbosity = (bool) result;
		Console.WriteLine("value of verbose attr: {0}", verbosity);
		p.verbose = !verbosity;
		result = p.getattr("verbose");
		verbosity = (bool) result;
		Console.WriteLine("value of verbose attr after toggle: {0}", verbosity);
				
			
		// some more examples
	
		string s="This string is way too long. This string is way too long. This string is way too long. This string is way too long. ";
		s=s+s+s+s+s;
		Console.WriteLine("echo param:");
		Console.WriteLine(s);
		result=p.echo(s);
		Console.WriteLine("return value:");
		Console.WriteLine(result);
	
		Console.WriteLine("dict test.");
		IDictionary<string, object> map = new Dictionary<string, object>
		{
			{"value", 42},
			{"message", "hello"},
			{"timestamp", DateTime.Now}
		};
		result = p.echo(map);
		Console.WriteLine("return value:");
		Console.WriteLine(result);
			
		// echo a pyro proxy and validate that all relevant attributes are also present on the proxy we got back.
		Console.WriteLine("proxy test.");
		result = p.echo(p);
		var p2 = (PyroProxy) result;
		Console.WriteLine("response proxy: " + p2);
		Debug.Assert(p2.objectid=="test.echoserver");
		Debug.Assert((string)p2.pyroHandshake == "banana");
		Debug.Assert(p2.pyroMethods.Contains("echo"));

		Console.WriteLine("remote iterator test.");
		var iter = p.generator();
		foreach(var item in iter) {
			Console.WriteLine("  got item: "+item);
		}
			
		Console.WriteLine("error test.");
		try
		{
			_ = p.error();
		} catch (PyroException e) {
			Console.WriteLine("Pyro Exception (expected)! {0}",e.Message);
			Console.WriteLine("Pyro Exception cause: {0}",e.InnerException);
			Console.WriteLine("Pyro Exception remote traceback:\n>>>\n{0}<<<",e._pyroTraceback);
		}
		try
		{
			_ = p.error_with_text();
		} catch (PyroException e) {
			Console.WriteLine("Pyro Exception (expected)! {0}",e.Message);
			Console.WriteLine("Pyro Exception cause: {0}",e.InnerException);
			Console.WriteLine("Pyro Exception remote traceback:\n>>>\n{0}<<<",e._pyroTraceback);
		}
	
//			Console.WriteLine("shutting down the test echo server.");
//			p.shutdown();
	}
}