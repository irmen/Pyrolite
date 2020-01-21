/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Runtime.Serialization;
// ReSharper disable InconsistentNaming

namespace Razorvine.Pyro
{
	/// <summary>
	/// Exception thrown when something is wrong in Pyro, or an exception was returned from a remote call.
	/// </summary>
	[Serializable]
	public class PyroException : Exception
	{
		public string _pyroTraceback {get; set;}
		public string PythonExceptionType {get; set;}

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
	}
}
