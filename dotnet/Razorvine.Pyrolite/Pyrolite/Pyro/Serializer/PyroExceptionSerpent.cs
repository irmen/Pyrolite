using System;
using System.Collections;
using System.Text;

namespace Razorvine.Pyro.Serializer
{
    public static class PyroExceptionSerpent
    {
        public static IDictionary ToSerpentDict(object obj)
        {
            var ex = (PyroException) obj;
            IDictionary dict = new Hashtable();
            // {'attributes':{},'__exception__':True,'args':('hello',),'__class__':'PyroError'}
            dict["__class__"] = "PyroError";
            dict["__exception__"] = true;
            if(ex.Message != null)
                dict["args"] = new object[] {ex.Message};
            else
                dict["args"] = Array.Empty<object>();
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

            var attrs = (IDictionary)dict["attributes"];
            foreach(DictionaryEntry entry in attrs)
            {
                string key = (string)entry.Key;
                ex.Data[key] = entry.Value;
                if ("_pyroTraceback" != key) 
                    continue;
                // if the traceback is a list of strings, create one string from it
                if(entry.Value is ICollection tbcoll) {
                    var sb=new StringBuilder();
                    foreach(object line in tbcoll) {
                        sb.Append(line);
                    }	
                    ex._pyroTraceback=sb.ToString();
                } else {
                    ex._pyroTraceback=(string)entry.Value;
                }
            }
            return ex;
        }        
    }
}