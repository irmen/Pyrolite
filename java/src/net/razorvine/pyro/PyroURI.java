package net.razorvine.pyro;

import java.io.IOException;
import java.io.Serializable;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * The Pyro URI object.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroURI implements Serializable {

	private static final long serialVersionUID = 1341548046126997560L;
	String protocol = "PYRO";
	String object;
	String host;
	int port;

	public PyroURI() {
	}

	public PyroURI(PyroURI other) {
		protocol = other.protocol;
		object = other.object;
		host = other.host;
		port = other.port;
	}

	public PyroURI(String uri) {
		Pattern p = Pattern.compile("(PYRO[A-Z]*):(\\S+?)(@(\\S+))?$");
		Matcher m = p.matcher(uri);
		if (m.find()) {
			protocol = m.group(1);
			object = m.group(2);
			String[] loc = m.group(4).split(":");
			host = loc[0];
			port = Integer.parseInt(loc[1]);
		} else {
			throw new PyroException("invalid URI string");
		}
	}

	public PyroURI(String object, String host, int port) {
		this.object = object;
		this.host = host;
		this.port = port;
	}

	public String toString() {
		return "<PyroURI " + protocol + ":" + object + "@" + host + ":" + port + ">";
	}

	/**
	 * setState, called by the Unpickler to set the state back.
	 */
	public void setState(Object[] args) throws IOException {
		this.protocol = (String) args[0];
		this.object = (String) args[1];
		this.host = (String) args[3];
		this.port = (Integer) args[4];
	}
}
