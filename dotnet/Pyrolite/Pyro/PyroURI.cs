/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Text.RegularExpressions;
using Razorvine.Pyrolite.Pickle;
using Razorvine.Pyrolite.Pickle.Objects;

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

	static PyroURI() {
		Unpickler.registerConstructor("Pyro4.core", "URI", new AnyClassConstructor(typeof(PyroURI)));
		Pickler.registerCustomPickler(typeof(PyroURI), new PyroUriPickler());
	}
	
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

	#region Equals and GetHashCode implementation
	public override bool Equals(object obj)
	{
		PyroURI other = obj as PyroURI;
		if (other == null)
			return false;
		return other.ToString()==this.ToString();
	}
	
	public override int GetHashCode()
	{
		return ToString().GetHashCode();
	}
	
	public static bool operator ==(PyroURI lhs, PyroURI rhs)
	{
		if (ReferenceEquals(lhs, rhs))
			return true;
		if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
			return false;
		return lhs.Equals(rhs);
	}
	
	public static bool operator !=(PyroURI lhs, PyroURI rhs)
	{
		return !(lhs == rhs);
	}
	#endregion

	
	/**
	 * called by the Unpickler to restore state
	 */
	public void __setstate__(object[] args) {
		this.protocol = (string) args[0];
		this.objectid = (string) args[1];
		this.host = (string) args[3];
		this.port = (int) args[4];
	}
}

}
