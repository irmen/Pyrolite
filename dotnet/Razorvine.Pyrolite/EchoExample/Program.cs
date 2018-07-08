/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using Razorvine.Pyro;
// ReSharper disable CheckNamespace
// ReSharper disable PossibleNullReferenceException

namespace Pyrolite.TestPyroEcho
{
	/// <summary>
	/// Console program to select the test to run.
	/// </summary>
	public static class Program
	{
		public static void Main(string[] args) {
			
			char test;
			if(args.Length==1)
				test = args[0].ToLowerInvariant()[0];
			else{
				Console.WriteLine("Which test to run ([e]cho, [h]andshake, [s]treaming)?");
				test = Console.ReadLine().Trim().ToLowerInvariant()[0];
			}
			
			SetConfig();
			try {
				switch(test)
				{
					case 'e':
						Console.WriteLine("\r\nRunning ECHO test.\r\n");
						TestEcho.Run();
						break;
					case 'h':
						Console.WriteLine("\r\nRunning HANDSHAKE test.\r\n");
						TestHandshake.Run();
						break;
					case 's':
						Console.WriteLine("\r\nRunning STREAMING test.\r\n");
						TestStreaming.Run();
						break;
					default:
						Console.Error.WriteLine("invalid choice");
						break;
				}
			} catch (Exception x) {
				Console.WriteLine("unhandled exception: {0}",x);
			}

			Console.WriteLine("\r\nEnter to exit:"); Console.ReadLine();
		}

		private static void SetConfig()
		{
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
