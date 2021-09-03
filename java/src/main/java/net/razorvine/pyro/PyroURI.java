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
		Matcher m = Pattern.compile("(PYRO[A-Z]*):(\\S+?)(@(\\S+))?$").matcher(uri);
		if (m.find()) {
			protocol = m.group(1);
			objectid = m.group(2);
			String location = m.group(4);
			if(location.charAt(0)=='[') {
				// ipv6
				if(location.startsWith("[["))
					throw new PyroException("invalid ipv6 address: enclosed in too many brackets");
				Matcher ipv6locationmatch = Pattern.compile("\\[([0-9a-fA-F:%]+)](:(\\d+))?").matcher(location);
				if(ipv6locationmatch.matches()) {
					host = ipv6locationmatch.group(1);
					port = Integer.parseInt(ipv6locationmatch.group(3));
				} else {
					throw new PyroException("invalid ipv6 address: the part between brackets must be a numeric ipv6 address");
				}
			} else {
				// regular ipv4
				String[] loc = location.split(":");
				host = loc[0];
				port = Integer.parseInt(loc[1]);
			}
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
