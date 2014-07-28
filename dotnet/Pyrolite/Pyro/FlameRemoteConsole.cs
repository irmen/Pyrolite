/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;

namespace Razorvine.Pyro
{

/// <summary>
/// Flame remote interactive console client.
/// </summary>
public class FlameRemoteConsole : IDisposable
{
	private PyroProxy remoteconsole;
	
	/// <summary>
	/// for the unpickler to restore state
	/// </summary>
	public void __setstate__(Hashtable values) {
		remoteconsole=(PyroProxy)values["remoteconsole"];
	}

	public void interact() {
		string banner=(String)remoteconsole.call("get_banner");
		Console.WriteLine(banner);
		string ps1=">>> ";
		string ps2="... ";
		bool more=false;
		while(true) {
			if(more)
				Console.Write(ps2);
			else
				Console.Write(ps1);
			Console.Out.Flush();
			string line=Console.ReadLine();
			if(line==null) {
				// end of input
				Console.WriteLine("");
				break;
			}
			object[] result=(object[])remoteconsole.call("push_and_get_output", line);
			if(result[0]!=null) {
				Console.Write(result[0]);
			}
			more=(bool)result[1];
		}
		Console.WriteLine("(Remote session ended)");
	}

	public void close()  {
		if(remoteconsole!=null) {
			remoteconsole.call("terminate");
			remoteconsole.close();
		}
	}
	
	public void Dispose()
	{
		close();
	}
}

}
