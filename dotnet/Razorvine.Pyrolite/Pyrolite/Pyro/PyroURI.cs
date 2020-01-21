/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Text.RegularExpressions;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Razorvine.Pyro
{

/// <summary>
/// The Pyro URI object.
/// </summary>
[Serializable]
public class PyroURI {

	public string protocol { get; }
	public string objectid { get; }
	public string host { get; }
	public int port { get; }


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
			var loc = m.Groups[4].Value.Split(':');
			host = loc[0];
			port = int.Parse(loc[1]);
		} else {
			throw new PyroException("invalid URI string");
		}
	}

	public PyroURI(string objectid, string host, int port) {
		protocol = "PYRO";
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
		return other.ToString()==ToString();
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
}

}
