/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Razorvine.Pyro
{

/// <summary>
/// Flame remote interactive console client.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "InvertIf")]
public class FlameRemoteConsole : IDisposable
{
	private PyroProxy _remoteconsole;
	
	/// <summary>
	/// for the unpickler to restore state
	/// </summary>
	public void __setstate__(Hashtable values) {
		_remoteconsole=(PyroProxy)values["remoteconsole"];
	}

	public void interact() {
		string banner=(string)_remoteconsole.call("get_banner");
		Console.WriteLine(banner);
		const string ps1 = ">>> ";
		const string ps2 = "... ";
		bool more=false;
		while(true) {
			Console.Write(more ? ps2 : ps1);
			Console.Out.Flush();
			string line=Console.ReadLine();
			if(line==null) {
				// end of input
				Console.WriteLine("");
				break;
			}
			var result=(object[])_remoteconsole.call("push_and_get_output", line);
			if(result[0]!=null) {
				Console.Write(result[0]);
			}
			more=(bool)result[1];
		}
		Console.WriteLine("(Remote session ended)");
	}

	public void close()  {
		if(_remoteconsole!=null) {
			_remoteconsole.call("terminate");
			_remoteconsole.close();
			_remoteconsole = null;
		}
	}
	
	public void Dispose()
	{
		close();
	}
}

}
