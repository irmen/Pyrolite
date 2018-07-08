/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Collections;
using System.IO;
using System.Text;

using Razorvine.Pickle;
// ReSharper disable InvertIf

namespace Razorvine.Pyro
{

/// <summary>
/// Pickler extension to be able to pickle PyroException objects.
/// </summary>
public class PyroExceptionPickler : IObjectPickler {

	public void pickle(object o, Stream outs, Pickler currentPickler) {
		PyroException error = (PyroException) o;
		outs.WriteByte(Opcodes.GLOBAL);
		var output=Encoding.Default.GetBytes("Pyro4.errors\nPyroError\n");
		outs.Write(output,0,output.Length);
		object[] args = { error.Message };
		currentPickler.save(args);
		outs.WriteByte(Opcodes.REDUCE);
		
		if(!string.IsNullOrEmpty(error._pyroTraceback))
		{
			// add _pyroTraceback attribute to the output
			Hashtable tb = new Hashtable {["_pyroTraceback"] = new[] {error._pyroTraceback}};
			// transform single string back into list
			currentPickler.save(tb);
			outs.WriteByte(Opcodes.BUILD);
		}
	}
	
	public static IDictionary ToSerpentDict(object obj)
	{
		PyroException ex = (PyroException) obj;
		IDictionary dict = new Hashtable();
		// {'attributes':{},'__exception__':True,'args':('hello',),'__class__':'PyroError'}
		dict["__class__"] = "PyroError";
		dict["__exception__"] = true;
		if(ex.Message != null)
			dict["args"] = new object[] {ex.Message};
		else
			dict["args"] = new object[0];
		if(!string.IsNullOrEmpty(ex._pyroTraceback))
			ex.Data["_pyroTraceback"] = new [] { ex._pyroTraceback } ;    	// transform single string back into list
		dict["attributes"] = ex.Data;
		return dict;
	}

	public static object FromSerpentDict(IDictionary dict)
	{
		var args = (object[]) dict["args"];
		
		string pythonExceptionType = (string) dict["__class__"];
		PyroException ex;
		if(args.Length==0)
		{
			ex = string.IsNullOrEmpty(pythonExceptionType) ? new PyroException() : new PyroException("["+pythonExceptionType+"]");
		}
		else
		{
			ex = string.IsNullOrEmpty(pythonExceptionType) ? new PyroException((string)args[0]) : new PyroException($"[{pythonExceptionType}] {args[0]}");
		}
		
		ex.PythonExceptionType = pythonExceptionType;

		IDictionary attrs = (IDictionary)dict["attributes"];
		foreach(DictionaryEntry entry in attrs)
		{
			string key = (string)entry.Key;
			ex.Data[key] = entry.Value;
			if("_pyroTraceback"==key)
			{
				// if the traceback is a list of strings, create one string from it
				var tbcoll = entry.Value as ICollection;
				if(tbcoll != null) {
					StringBuilder sb=new StringBuilder();
					foreach(object line in tbcoll) {
						sb.Append(line);
					}	
					ex._pyroTraceback=sb.ToString();
				} else {
					ex._pyroTraceback=(string)entry.Value;
				}
			}
		}
		return ex;
	}
}

}
