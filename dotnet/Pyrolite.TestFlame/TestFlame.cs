/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Text;
using Razorvine.Pyro;

namespace Pyrolite.TestPyroFlame
{
	
/// <summary>
/// Test Pyro with a Flame server
/// </summary>
public class TestFlame {

	static protected byte[] hmacKey = null;

	public static void Main(String[] args) {
		try {
			Test();
		} catch (Exception x) {
			Console.WriteLine("unhandled exception: {0}",x);
		}
	}
	
	public static void Test() {

		Console.WriteLine("Testing Pyro flame server (make sure it's running on localhost 9999)...");
		Console.WriteLine("Pyrolite version: "+Config.PYROLITE_VERSION);

		setConfig();
		using(dynamic flame=new PyroProxy("localhost",9999,"Pyro.Flame"))
		{
			if(hmacKey!=null) flame.pyroHmacKey = hmacKey;
			
			Console.WriteLine("builtin:");
			using(dynamic r_max=(FlameBuiltin)flame.builtin("max"))
			{
				if(hmacKey!=null) r_max.pyroHmacKey = hmacKey;
				
				int maximum=(int)r_max(new int[]{22,99,1});		// invoke remote max() builtin function
				Console.WriteLine("maximum="+maximum);
			}
			
			using(dynamic r_module=(FlameModule)flame.module("socket"))
			{
				if(hmacKey!=null) r_module.pyroHmacKey = hmacKey;

				String hostname=(String)r_module.gethostname();		// get remote hostname
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

	static void setConfig()
	{
		string tracedir=Environment.GetEnvironmentVariable("PYRO_TRACE_DIR");
		if(tracedir!=null) {
			Config.MSG_TRACE_DIR=tracedir;
		}
		Config.SERIALIZER = Config.SerializerType.pickle;   // flame requires the pickle serializer
	}
}

}