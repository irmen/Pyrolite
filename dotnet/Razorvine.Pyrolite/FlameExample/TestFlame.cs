/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using Razorvine.Pyro;
// ReSharper disable CheckNamespace

namespace Pyrolite.TestPyroFlame
{
	
/// <summary>
/// Test Pyro with a Flame server
/// </summary>
public static class TestFlame {
	private static readonly byte[] HmacKey = null;

	public static void Main() {
		try {
			Test();
		} catch (Exception x) {
			Console.WriteLine("unhandled exception: {0}",x);
		}
	}

	private static void Test() {

		Console.WriteLine("Testing Pyro flame server (make sure it's running on localhost 9999)...");
		Console.WriteLine("Pyrolite version: "+Config.PYROLITE_VERSION);

		SetConfig();
		using(dynamic flame=new PyroProxy("localhost",9999,"Pyro.Flame"))
		{
			if(HmacKey!=null) flame.pyroHmacKey = HmacKey;
			
			Console.WriteLine("builtin:");
			using(dynamic rMax=(FlameBuiltin)flame.builtin("max"))
			{
				if(HmacKey!=null) rMax.pyroHmacKey = HmacKey;
				
				int maximum=(int)rMax(new []{22,99,1});		// invoke remote max() builtin function
				Console.WriteLine("maximum="+maximum);
			}
			
			using(dynamic rModule=(FlameModule)flame.module("socket"))
			{
				if(HmacKey!=null) rModule.pyroHmacKey = HmacKey;

				string hostname=(string)rModule.gethostname();		// get remote hostname
				Console.WriteLine("hostname="+hostname);
			}
			
			int sum=(int)flame.evaluate("9+9");
			Console.WriteLine("sum="+sum);
			
			flame.execute("import sys; sys.stdout.write('HELLO FROM C#\\n')");
			
			using(FlameRemoteConsole console=(FlameRemoteConsole)flame.console())
			{
				console.interact();
			}

			Console.WriteLine("\r\nEnter to exit:"); Console.ReadLine();
		}
	}

	private static void SetConfig()
	{
		string tracedir=Environment.GetEnvironmentVariable("PYRO_TRACE_DIR");
		if(tracedir!=null) {
			Config.MSG_TRACE_DIR=tracedir;
		}
		Config.SERIALIZER = Config.SerializerType.pickle;   // flame requires the pickle serializer
	}
}

}
