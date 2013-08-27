/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */
	
using System;
using System.Collections;

namespace Razorvine.Pyro
{
	/// <summary>
	/// Description of Serializers.
	/// </summary>
	public abstract class PyroSerializer
	{
		public readonly ushort serializer_id = 0;
	}
	
	
	public class PickleSerializer : PyroSerializer
	{
		public new readonly ushort serializer_id = 4;
	}
}
