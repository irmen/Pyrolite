/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;

namespace Razorvine.Pickle.Objects
{

/// <summary>
/// This object constructor uses reflection to create instances of any given class. 
/// </summary>
public class AnyClassConstructor : IObjectConstructor {

	private readonly Type _type;

	public AnyClassConstructor(Type type) {
		_type = type;
	}

	public object construct(object[] args) {
		try {
			return Activator.CreateInstance(_type, args);
		} catch (Exception x) {
			throw new PickleException("problem constructing object",x);
		}
	}
}

}
