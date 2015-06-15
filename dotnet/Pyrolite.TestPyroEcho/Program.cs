/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using Razorvine.Pyro;

namespace Pyrolite.TestPyroEcho
{
	/// <summary>
	/// Console program to select the test to run.
	/// </summary>
	public class Program
	{
		public static void Main(String[] args) {
			
			char test;
			if(args.Length==1)
				test = args[0].ToLowerInvariant()[0];
			else{
				Console.WriteLine("Which test to run ([e]cho, [h]andshake)?");
				test = Console.ReadLine().Trim().ToLowerInvariant()[0];
			}
			
			setConfig();
			try {
				switch(test)
				{
					case 'e':
						Console.WriteLine("\r\nRunning ECHO test.\r\n");
						new TestEcho().Run();
						break;
					case 'h':
						Console.WriteLine("\r\nRunning HANDSHAKE test.\r\n");
						new TestHandshake().Run();
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

		static void setConfig()
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
