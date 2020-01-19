/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;

namespace DebugHelper
{
	/// <summary>
	/// A little command line tool that is used to simplyfy starting a debugger for problematic code.
	/// </summary>
	public static class Program
	{
		public static void Main()
		{
			Unpickler u=new Unpickler();

			Console.WriteLine("here we go; 1");
			var data = PickleUtils.str2bytes("\u0080\u0002carray\narray\nq\u0000U\u0001iq\u0001]q\u0002\u0086q\u0003Rq\u0004.");
			var result = u.loads(data);
			PrettyPrint.print(result);
				
			Console.WriteLine("here we go; 2");
			data=PickleUtils.str2bytes("\u0080\u0003carray\n_array_reconstructor\nq\u0000(carray\narray\nq\u0001X\u0001\u0000\u0000\u0000iq\u0002K\u0008C\u000c\u000f'\u0000\u0000\u00b8\"\u0000\u0000a\u001e\u0000\u0000q\u0003tq\u0004Rq\u0005.");
			result=u.loads(data);
			PrettyPrint.print(result);
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
}