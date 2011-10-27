/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */
	
using System;
using System.Collections;

namespace Razorvine.Pyrolite.Pyro
{
	/// <summary>
	/// Dummy class to be able to unpickle Pyro Proxy objects.
	/// </summary>
	public class DummyPyroSerializer
	{
		/// <summary>
		/// for the unpickler to restore state
		/// </summary>
		public void __setstate__(Hashtable values) {
		}		
	}
}
