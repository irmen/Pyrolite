package net.razorvine.pickle.objects;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;

/**
 * This object constructor creates ClassDicts (for unsupported classes).
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class ClassDictConstructor implements IObjectConstructor {

	String module;
	String name;

	public ClassDictConstructor(String module, String name) {
		this.module = module;
		this.name = name;
	}

	public Object construct(Object[] args) {
		if (args.length > 0)
			throw new PickleException("expected zero arguments for construction of ClassDict (for "+module+"."+name+")");
		return new ClassDict(module, name);
	}
}

