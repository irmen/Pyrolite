package net.razorvine.pickle.objects;

import java.util.HashMap;
import java.util.Map;

/// <summary>
/// A dictionary containing just the fields of the class.
/// </summary>
public class ClassDict extends HashMap<String, Object>
{
	private static final long serialVersionUID = 576056580143549390L;
	private String classname;
	
	public ClassDict(String modulename, String classname)
	{
		if(modulename==null)
			this.classname = classname;
		else
			this.classname = modulename+"."+classname;
	}
	
	/// <summary>
	/// for the unpickler to restore state
	/// </summary>
	public void __setstate__(HashMap<String, Object> values) {
		this.clear();
		this.put("__class__", this.classname);
		for(Map.Entry<String, Object> e: values.entrySet())
		{
			this.put(e.getKey(), e.getValue());
		}
	}
}
