package net.razorvine.pickle;

/**
 * Interface for Object Constructors that are used by the unpickler
 * to create instances of non-primitive or custom classes.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public interface IObjectConstructor {

	/**
	 * Create an object. Use the given args as parameters for the constructor.
	 */
	Object construct(Object[] args) throws PickleException;
}
