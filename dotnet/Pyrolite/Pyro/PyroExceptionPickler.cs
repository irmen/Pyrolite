/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.IO;
using System.Text;

using Razorvine.Pickle;

namespace Razorvine.Pyro
{

/// <summary>
/// Pickler extension to be able to pickle PyroException objects.
/// </summary>
public class PyroExceptionPickler : IObjectPickler {

	public void pickle(object o, Stream outs, Pickler currentPickler) {
		PyroException error = (PyroException) o;
		outs.WriteByte(Opcodes.GLOBAL);
		byte[] output=Encoding.Default.GetBytes("Pyro4.errors\nPyroError\n");
		outs.Write(output,0,output.Length);
		object[] args = new object[] { error.Message };
		currentPickler.save(args);
		outs.WriteByte(Opcodes.REDUCE);
	}
	
	public static IDictionary ToSerpentDict(object obj)
	{
		PyroException ex = (PyroException) obj;
		IDictionary dict = new Hashtable();
		// {'attributes':{},'__exception__':True,'args':('hello',),'__class__':'PyroError'}
		dict["__class__"] = "PyroError";
		dict["__exception__"] = true;
		dict["args"] = new object[] {ex.Message};
		dict["attributes"] = ex.Data;
		return dict;
	}

	public static object FromSerpentDict(IDictionary dict)
	{
		object[] args = (object[]) dict["args"];
		PyroException ex = new PyroException((string)args[0]);
		IDictionary attrs = (IDictionary)dict["attributes"];
		foreach(DictionaryEntry entry in attrs)
		{
			string key = (string)entry.Key;
			ex.Data[key] = entry.Value;
			if("_pyroTraceback"==key)
			{
				ex._pyroTraceback = (string)entry.Value;
			}
		}
		return ex;
	}
}

}
