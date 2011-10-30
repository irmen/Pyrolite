/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Reflection;

namespace Razorvine.Pickle.Objects
{

/// <summary>
/// This object constructor uses reflection to create instances of the string type.
/// AnyClassConstructor cannot be used because string doesn't have the appropriate constructors.
///	see http://stackoverflow.com/questions/2092530/how-do-i-use-activator-createinstance-with-strings
/// </summary>
public class StringConstructor : IObjectConstructor
{
	public StringConstructor()
	{
	}
	
	public object construct(object[] args)
	{
		if(args.Length==0) {
			return "";
		} else if(args.Length==1 && args[0] is string) {
			return (string)args[0];
		} else {
			throw new PickleException("invalid string constructor arguments");
		}
	}
}

}
