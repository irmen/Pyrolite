/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Text.RegularExpressions;

namespace Razorvine.Pyrolite.Pyro
{

/// <summary>
/// The Pyro URI object.
/// </summary>
public class PyroURI {

	public string protocol {get;set;}
	public string objectid {get;set;}
	public string host {get;set;}
	public int port {get;set;}

	public PyroURI() {
		protocol="PYRO";
	}

	public PyroURI(PyroURI other) {
		protocol = other.protocol;
		objectid = other.objectid;
		host = other.host;
		port = other.port;
	}

	public PyroURI(string uri) {
		Regex r = new Regex("(PYRO[A-Z]*):(\\S+?)(@(\\S+))?$");
		Match m = r.Match(uri);
		if (m.Success) {
			protocol = m.Groups[1].Value;
			objectid = m.Groups[2].Value;
			string[] loc = m.Groups[4].Value.Split(':');
			host = loc[0];
			port = int.Parse(loc[1]);
		} else {
			throw new PyroException("invalid URI string");
		}
	}

	public PyroURI(string objectid, string host, int port) {
		this.objectid = objectid;
		this.host = host;
		this.port = port;
	}

	public override string ToString() {
		return "<PyroURI " + protocol + ":" + objectid + "@" + host + ":" + port + ">";
	}

	/**
	 * setState, called by the Unpickler to set the state back.
	 */
	public void setState(object[] args) {
		this.protocol = (string) args[0];
		this.objectid = (string) args[1];
		this.host = (string) args[3];
		this.port = (int) args[4];
	}
}

}
