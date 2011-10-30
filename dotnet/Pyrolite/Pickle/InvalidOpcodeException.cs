/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Runtime.Serialization;

namespace Razorvine.Pickle
{
	/// <summary>
	/// Exception thrown when the unpickler encountered an unknown or unimplemented opcode.
	/// </summary>
	public class InvalidOpcodeException : PickleException
	{
		public InvalidOpcodeException()
		{
		}

	 	public InvalidOpcodeException(string message) : base(message)
		{
		}

		public InvalidOpcodeException(string message, Exception innerException) : base(message, innerException)
		{
		}

		// This constructor is needed for serialization.
		protected InvalidOpcodeException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}