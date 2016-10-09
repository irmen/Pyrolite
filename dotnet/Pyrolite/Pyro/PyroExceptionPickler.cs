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
		
		if(!string.IsNullOrEmpty(error._pyroTraceback))
		{
			// add _pyroTraceback attribute to the output
			Hashtable tb = new Hashtable();
			tb["_pyroTraceback"] = new string[]{ error._pyroTraceback };		// transform single string back into list
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
			dict["args"] = new object[1] {ex.Message};
		else
			dict["args"] = new object[0];
		if(!string.IsNullOrEmpty(ex._pyroTraceback))
			ex.Data["_pyroTraceback"] = new string[] { ex._pyroTraceback } ;    	// transform single string back into list
		dict["attributes"] = ex.Data;
		return dict;
	}

	public static object FromSerpentDict(IDictionary dict)
	{
		object[] args = (object[]) dict["args"];
		
		string pythonExceptionType = (string) dict["__class__"];
		PyroException ex;
		if(args.Length==0) {
			if(string.IsNullOrEmpty(pythonExceptionType))
				ex = new PyroException();
			else
				ex = new PyroException("["+pythonExceptionType+"]");
		}
		else {
			if(string.IsNullOrEmpty(pythonExceptionType))
				ex = new PyroException((string)args[0]);
			else
				ex = new PyroException(string.Format("[{0}] {1}", pythonExceptionType, args[0]));
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
				if(entry.Value is ICollection) {
					StringBuilder sb=new StringBuilder();
					ICollection tbcoll=(ICollection)entry.Value;
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
