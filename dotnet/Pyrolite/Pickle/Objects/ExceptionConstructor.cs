/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Razorvine.Pickle.Objects
{

/// <summary>
/// This creates PythonException instances. 
/// It keeps track of the original Python exception type name as well.
/// </summary>
public class ExceptionConstructor : IObjectConstructor {

	private readonly string pythonExceptionType;
	private readonly Type type;
	
	public ExceptionConstructor(Type type, string module, string name) {
		if(!string.IsNullOrEmpty(module))
			pythonExceptionType = module+"."+name;
		else
			pythonExceptionType = name;
		this.type = type;
	}

	public object construct(object[] args) {
		try {
			if(!string.IsNullOrEmpty(pythonExceptionType)) {
				// put the python exception type somewhere in the message
				if(args==null || args.Length==0) {
					args = new string[] { "["+pythonExceptionType+"]" };
				} else {
					string msg = (string)args[0];
					msg = string.Format("[{0}] {1}", pythonExceptionType, msg);
					args = new string[] {msg};
				}
			}
			object ex = Activator.CreateInstance(this.type, args);
			
			PropertyInfo prop=ex.GetType().GetProperty("PythonExceptionType");
			if(prop!=null) {
				prop.SetValue(ex, pythonExceptionType, null);
			}
			return ex;
		} catch (Exception x) {
			throw new PickleException("problem constructing object",x);
		}
	}
}

}