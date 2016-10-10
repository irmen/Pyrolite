package net.razorvine.pickle.objects;

import java.lang.reflect.Constructor;
import java.lang.reflect.Field;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;

/**
 * This creates Python Exception instances. 
 * It keeps track of the original Python exception type name as well.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class ExceptionConstructor implements IObjectConstructor {

	private Class<?> type;
	private String pythonExceptionType;

	public ExceptionConstructor(Class<?> type, String module, String name) {
		if(module!=null)
			pythonExceptionType = module+"."+name;
		else
			pythonExceptionType = name;
		this.type = type;
	}

	public Object construct(Object[] args) {
		try {
			if(pythonExceptionType!=null) {
				// put the python exception type somewhere in the message
				if(args==null || args.length==0) {
					args = new String[] { "["+pythonExceptionType+"]" };
				} else {
					String msg = "["+pythonExceptionType+"] "+(String)args[0];
					args = new String[] {msg};
				}
			}
			Class<?>[] paramtypes = new Class<?>[args.length];
			for (int i = 0; i < args.length; ++i) {
				paramtypes[i] = args[i].getClass();
			}
			Constructor<?> cons = type.getConstructor(paramtypes);
			Object ex = cons.newInstance(args);
			
			try {
				Field prop = ex.getClass().getField("pythonExceptionType");
				prop.set(ex, pythonExceptionType);
			} catch (NoSuchFieldException x) {
				// meh.
			}
			return ex;
		} catch (Exception x) {
			throw new PickleException("problem construction object: " + x);
		}
	}
}
