package net.razorvine.pyro;

import java.io.Serializable;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * The Pyro URI object.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroURI implements Serializable {

	private static final long serialVersionUID = -7611447798373262153L;
	public String protocol = "PYRO";
	public String objectid;
	public String host;
	public int port;

	public PyroURI() {
	}

	public PyroURI(PyroURI other) {
		protocol = other.protocol;
		objectid = other.objectid;
		host = other.host;
		port = other.port;
	}

	public PyroURI(String uri) {
		Pattern p = Pattern.compile("(PYRO[A-Z]*):(\\S+?)(@(\\S+))?$");
		Matcher m = p.matcher(uri);
		if (m.find()) {
			protocol = m.group(1);
			objectid = m.group(2);
			String[] loc = m.group(4).split(":");
			host = loc[0];
			port = Integer.parseInt(loc[1]);
		} else {
			throw new PyroException("invalid URI string");
		}
	}

	public PyroURI(String objectid, String host, int port) {
		this.objectid = objectid;
		this.host = host;
		this.port = port;
	}

	public String toString() {
		return "<PyroURI " + protocol + ":" + objectid + "@" + host + ":" + port + ">";
	}


	@Override
	public int hashCode() {
		return toString().hashCode();
	}

	@Override
	public boolean equals(Object obj) {
		if (this == obj)
			return true;
		if (obj == null)
			return false;
		if (!(obj instanceof PyroURI))
			return false;
		PyroURI other = (PyroURI) obj;
		return toString().equals(other.toString());
	}
}
