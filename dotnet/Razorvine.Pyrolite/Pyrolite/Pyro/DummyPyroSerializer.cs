/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */
	
using System.Collections;

namespace Razorvine.Pyro
{
	/// <summary>
	/// Dummy class to be able to unpickle Pyro Proxy objects.
	/// </summary>
	public class DummyPyroSerializer
	{
		/// <summary>
		/// for the unpickler to restore state
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		// ReSharper disable once UnusedParameter.Global
		public void __setstate__(Hashtable values) {
		}		
	}
}
