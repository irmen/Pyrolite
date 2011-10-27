/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Text;
using Razorvine.Pyrolite.Pickle;
using Razorvine.Pyrolite.Pyro;

namespace Pyrolite.TestPyroFlame
{
	
/// <summary>
/// Test Pyro with a Flame server
/// </summary>
public class TestFlame {

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

		PyroProxy flame=new PyroProxy("localhost",9999,"Pyro.Flame");

		Console.WriteLine("builtin:");
		using(FlameBuiltin r_max=(FlameBuiltin)flame.call("builtin", "max"))
		{
			int maximum=(int)r_max.call(new int[]{22,99,1});
			Console.WriteLine("maximum="+maximum);
		}
		
		using(FlameModule r_module=(FlameModule)flame.call("module","socket"))
		{
			String hostname=(String)r_module.call("gethostname");
			Console.WriteLine("hostname="+hostname);
		}
		
		int sum=(int)flame.call("evaluate", "9+9");
		Console.WriteLine("sum="+sum);
		
		flame.call("execute", "import sys; sys.stdout.write('HELLO FROM C#\\n')");
		
		using(FlameRemoteConsole console=(FlameRemoteConsole)flame.call("console"))
		{
			console.interact();
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
	}
}

}