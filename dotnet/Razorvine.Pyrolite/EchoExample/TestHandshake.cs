/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Text;
using System.Collections.Generic;
using Razorvine.Pyro;
// ReSharper disable CheckNamespace
// ReSharper disable PossibleNullReferenceException
// ReSharper disable once InconsistentNaming

namespace Pyrolite.TestPyroEcho
{
	
/// <summary>
/// This custom proxy adds custom annotations to the pyro messages
/// </summary>
class CustomAnnotationsProxy : PyroProxy
{
	public CustomAnnotationsProxy(PyroURI uri): base(uri) 
	{
	}

	public override IDictionary<string, byte[]> annotations()
	{
		var ann = base.annotations();
		ann["XYZZ"] = Encoding.UTF8.GetBytes("A custom annotation!");
		return ann;
	}
	
	public override void validateHandshake(object handshake_response) {
		// the handshake example server returns a list.
		var responseList = (IList<object>) handshake_response;
		Console.WriteLine("Proxy received handshake response data: "+ string.Join(",", responseList));
	}

	public override void responseAnnotations(IDictionary<string, byte[]> annotations, ushort msgtype) {
		Console.WriteLine("    Got response (type={0}). Annotations:", msgtype);
		foreach(var ann in annotations) {
			string value = ann.Value.ToString();
			Console.WriteLine("      {0} -> {1}", ann.Key, value);
		}
	}
}
	
/// <summary>
/// Test Pyro with the Handshake example server to see
/// how custom annotations and handshake handling is done.
/// </summary>
public static class TestHandshake {

	public static void Run() {

		Console.WriteLine("Testing Pyro handshake and custom annotations. Make sure the server from the pyro handshake example is running.");
		Console.WriteLine("Pyrolite version: "+Config.PYROLITE_VERSION);

		Console.WriteLine("\r\nEnter the server URI: ");
		string uri = Console.ReadLine().Trim();
		Console.WriteLine("Enter the secret code as printed by the server: ");
		string secret = Console.ReadLine().Trim();
		
		using(dynamic p = new CustomAnnotationsProxy(new PyroURI(uri)))
		{
		    p.pyroHandshake = secret;
		    p.correlation_id = Guid.NewGuid();
		    Console.WriteLine("correlation id set to: {0}", p.correlation_id);
		    p.ping();
		    Console.WriteLine("Connection Ok!");
		}
	}
}

}