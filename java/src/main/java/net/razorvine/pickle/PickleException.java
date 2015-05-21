package net.razorvine.pickle;

/**
 * Exception thrown when something goes wrong with pickling or unpickling.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PickleException extends RuntimeException {
	private static final long serialVersionUID = -673995077281654640L;

	public PickleException(String message, Throwable cause) {
		super(message, cause);
	}

	public PickleException(String message) {
		super(message);
	}
}
