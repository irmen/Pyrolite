/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Collections;
using System.Collections.Generic;

namespace Razorvine.Pickle.Objects
{

/// <summary>
/// This object constructor creates ClassDicts (for unsupported classes)
/// </summary>
class ClassDictConstructor : IObjectConstructor {

	string module;
	string name;
	
	public ClassDictConstructor(string module, string name) {
		this.module=module;
		this.name=name;
	}

	public object construct(object[] args) {
		if(args.Length>0)
			throw new PickleException("expected zero arguments for construction of ClassDict (for "+module+"."+name+")");
		return new ClassDict(module, name);
	}
}

/// <summary>
/// A dictionary containing just the fields of the class.
/// </summary>
class ClassDict : Dictionary<string, object>
{
	private string classname;
	
	public ClassDict(string modulename, string classname)
	{
		if(string.IsNullOrEmpty(modulename))
			this.classname = classname;
		else
			this.classname = modulename+"."+classname;
	}
	
	/// <summary>
	/// for the unpickler to restore state
	/// </summary>
	public void __setstate__(Hashtable values) {
		this.Clear();
		this.Add("__class__", this.classname);
		foreach(string x in values.Keys)
			this.Add(x, values[x]);
	}
	
}

}
