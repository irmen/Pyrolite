package net.razorvine.pyro;

/**
 * Exception thrown when something is wrong in Pyro.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroException extends RuntimeException {

	private static final long serialVersionUID = 5164514665621511957L;
	public String _pyroTraceback;
	public String pythonExceptionType;

	public PyroException() {
		super();
	}

	public PyroException(String message, Throwable cause) {
		super(message, cause);
	}

	public PyroException(String message) {
		super(message);
	}
}
