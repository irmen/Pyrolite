/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Razorvine.Pickle
{

/// <summary>
/// Object output pretty printing, to help with the test scripts.
/// Nothing fancy, just a simple readable output format for a handfull of classes.
/// </summary>
public class PrettyPrint {

	public static string printToString(object o) {
		StringWriter sw=new StringWriter();
		print(o, sw, false);
		return sw.ToString().TrimEnd();
	}
	
	public static void print(object o) {
		print(o, Console.Out, true);
	}

	/**
	 * Prettyprint the object to the outputstream.
	 */
	public static void print(object o, TextWriter w, bool typeheader) {
		if (o == null) {
			if(typeheader) w.WriteLine("null object");
			w.WriteLine("null");
			w.Flush();
			return;
		}

		if (o is IDictionary) {
			if(typeheader) w.WriteLine("hashtable");
			IDictionary map=(IDictionary)o;
			w.Write("{");
			foreach(object key in map.Keys) {
				w.Write(key.ToString()+"="+map[key].ToString()+", ");
			}
			w.WriteLine("}");
		} else if (o is string) {
			if(typeheader) w.WriteLine("String");
			w.WriteLine(o.ToString());
		} else if (o is DateTime) {
			if(typeheader) w.WriteLine("DateTime");
			w.WriteLine((DateTime)o);
		} else if (o is IEnumerable) {
			if(typeheader) w.WriteLine(o.GetType().Name);
			writeEnumerable(o, w);
			w.WriteLine("");
		} else {
			if(typeheader) w.WriteLine(o.GetType().Name);
			w.WriteLine(o.ToString());
		}
		
		w.Flush();
	}
	
	static void writeEnumerable(object o, TextWriter w)
	{
		IEnumerable e=(IEnumerable)o;
		w.Write("[");
		foreach(object x in e) {
			if(x==o) {
				w.Write("(this Collection), ");
			} else if (x is ICollection) {
				writeEnumerable(x, w);
				w.Write(", ");
			} else {
				w.Write(x.ToString());
				w.Write(", ");
			}
		}
		w.Write("]");
	}
}

}
