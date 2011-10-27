package net.razorvine.pickle.objects;

import java.lang.reflect.Constructor;
import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;

/**
 * This object constructor uses reflection to create instances of any given class.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class AnyClassConstructor implements IObjectConstructor {

	private Class<?> type;

	public AnyClassConstructor(Class<?> type) {
		this.type = type;
	}

	public Object construct(Object[] args) {
		try {
			Class<?>[] paramtypes = new Class<?>[args.length];
			for (int i = 0; i < args.length; ++i) {
				paramtypes[i] = args[i].getClass();
			}
			Constructor<?> cons = type.getConstructor(paramtypes);
			return cons.newInstance(args);
		} catch (Exception x) {
			throw new PickleException("problem construction object: " + x);
		}
	}
}
