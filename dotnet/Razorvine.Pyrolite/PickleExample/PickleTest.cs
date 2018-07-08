/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Razorvine.Pickle;
// ReSharper disable CheckNamespace

namespace Pyrolite.PickleExample
{
	internal static class PickleTest
	{
		public static void Main()
		{
			// going to pickle a c# datastructure

			var map = new Dictionary<string, object>
			{
				["apple"] = 42,
				["microsoft"] = "hello"
			};
			var values = new List<double>();
			values.AddRange(new [] { 1.11, 2.22, 3.33, 4.44, 5.55} );
			map["values"] = values;
			// You can add many other types if you like. See the readme about the type mappings.
			
			const string pickleFilename = "testpickle.dat";
			
			Console.WriteLine("Writing pickle to '{0}'", pickleFilename);
			
			var pickler = new Pickler(true);
			using(FileStream fos = new FileStream(pickleFilename, FileMode.Create)) 
			{
				pickler.dump(map, fos);
			}
			
			Console.WriteLine("Done. Try unpickling it in python.\n");

			Console.WriteLine("Reading a pickle created in python...");
			
			// the following pickle was created in Python 3.4.
			// it is this data:     [1, 2, 3, (11, 12, 13), {'banana', 'grape', 'apple'}]
			byte[] pythonpickle = {128, 4, 149, 48, 0, 0, 0, 0, 0, 0, 0, 93, 148, 40, 75, 1, 75, 2, 75, 3, 75, 11, 75, 12, 75, 13, 135, 148, 143, 148, 40, 140, 6, 98, 97, 110, 97, 110, 97, 148, 140, 5, 103, 114, 97, 112, 101, 148, 140, 5, 97, 112, 112, 108, 101, 148, 144, 101, 46};
			var unpickler = new Unpickler();
			object result = unpickler.loads(pythonpickle);
			
			Console.WriteLine("type: {0}", result.GetType());
			var list = (ArrayList) result;
			int integer1 = (int)list[0];
			int integer2 = (int)list[1];
			int integer3 = (int)list[2];
			var tuple = (object[]) list[3];
			var set = (HashSet<object>) list[4];
			Console.WriteLine("1-3: integers: {0}, {1}, {2}", integer1, integer2, integer3);
			Console.WriteLine("4: tuple: ({0}, {1}, {2})", tuple[0], tuple[1], tuple[2]);
			Console.WriteLine("5: set: {0}", string.Join(",", set));
			
			Console.WriteLine("\r\nEnter to exit:"); Console.ReadLine();
		}
	}
}
