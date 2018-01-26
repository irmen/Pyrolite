/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Text;

namespace Razorvine.Pyro
{
	/// <summary>
	/// Exception thrown when something is wrong in Pyro, or an exception was returned from a remote call.
	/// </summary>
	[Serializable]
	public class PyroException : Exception
	{
		public String _pyroTraceback {get;set;}
		public String PythonExceptionType {get;set;}

		public PyroException()
		{
		}

	 	public PyroException(string message) : base(message)
		{
		}

		public PyroException(string message, Exception innerException) : base(message, innerException)
		{
		}

		// This constructor is needed for serialization.
		protected PyroException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
		
		/// <summary>
		/// for the unpickler to restore state
		/// </summary>
		public void __setstate__(Hashtable values) {
			if(!values.ContainsKey("_pyroTraceback"))
				return;
			object tb=values["_pyroTraceback"];
			// if the traceback is a list of strings, create one string from it
			if(tb is ICollection) {
				StringBuilder sb=new StringBuilder();
				ICollection tbcoll=(ICollection)tb;
				foreach(object line in tbcoll) {
					sb.Append(line);
				}	
				_pyroTraceback=sb.ToString();
			} else {
				_pyroTraceback=(string)tb;
			}
			//Console.WriteLine("pyroexception state set to:{0}",_pyroTraceback);
		}
	}
}
