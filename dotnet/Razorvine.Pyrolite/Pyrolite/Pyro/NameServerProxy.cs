/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
// ReSharper disable MemberCanBePrivate.Global

namespace Razorvine.Pyro
{

/// <summary>
/// A wrapper proxy for the Pyro Name Server, 
/// to simplify the access to its remote methods.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class NameServerProxy : PyroProxy {

	public NameServerProxy(PyroURI uri) : this(uri.host, uri.port, uri.objectid) {
	}
	
	public NameServerProxy(string hostname, int port, string objectid = "Pyro.NameServer") : base(hostname, port, objectid) {
	}

	public void ping() {
		call("ping");
	}
	
	public PyroURI lookup(string name) {
		return (PyroURI) call("lookup", name);
	}
	
	public Tuple<PyroURI, ISet<string>> lookup(string name, bool return_metadata) {
		var result = (object[])call("lookup", name, return_metadata);
		PyroURI uri = (PyroURI) result[0];
		var metastrings = GetStringSet(result[1]);
		return new Tuple<PyroURI, ISet<string>>(uri, metastrings);
	}

	public int remove(string name, string prefix, string regex) {
		return (int) call("remove", name, prefix, regex);
	}

	public void register(string name, PyroURI uri, bool safe) {
		call("register", name, uri, safe);
	}
	
	public void register(string name, PyroURI uri, bool safe, IEnumerable<string> metadata) {
		call("register", name, uri, safe, metadata);
	}

	public void set_metadata(string name, ISet<string> metadata) {
		call("set_metadata", name, metadata);
	}

	public IDictionary<string, Tuple<string, ISet<string>>> list(string prefix, string regex) {
		IDictionary hash = (IDictionary) call("list", prefix, regex, true);
		IDictionary<string, Tuple<string, ISet<string>>> typed=new Dictionary<string, Tuple<string, ISet<string>>>(hash.Count);
		foreach(object name in hash.Keys) {
			var o = (object[]) hash[name];
			string uri = (string) o[0];
			var metastrings = GetStringSet(o[1]);
			typed[(string)name] =  new Tuple<string, ISet<string>>(uri, metastrings);
		}
		return typed;
	}

	public IDictionary<string, Tuple<string, ISet<string>>> yplookup(IEnumerable<string> meta_all, IEnumerable<string> meta_any) {
		IDictionary hash = (IDictionary) call("yplookup", meta_all, meta_any, true);
		IDictionary<string, Tuple<string, ISet<string>>> typed=new Dictionary<string, Tuple<string, ISet<string>>>(hash.Count);
		foreach(object name in hash.Keys) {
			var o = (object[]) hash[name];
			string uri = (string) o[0];
			var metastrings = GetStringSet(o[1]);
			typed[(string)name] =  new Tuple<string, ISet<string>>(uri, metastrings);
		}
		return typed;
	}

	public static NameServerProxy locateNS(string host, int port=0) {
		if(host!=null) {
			if(port==0)
				port=Config.NS_PORT;
			NameServerProxy proxy = new NameServerProxy(host, port);
			proxy.ping();
			return proxy;
		}
		if(port==0)
			port=Config.NS_BCPORT;
		
		IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Broadcast, port);
		using(UdpClient udpclient=new UdpClient()) {
			udpclient.Client.ReceiveTimeout = 2000;
			udpclient.EnableBroadcast=true;
			var buf=Encoding.ASCII.GetBytes("GET_NSURI");
			udpclient.Send(buf, buf.Length, ipendpoint);
			IPEndPoint source=null;
			try {
				buf=udpclient.Receive(ref source);
			} catch (SocketException) {
				// try localhost explicitly
				return locateNS("localhost", Config.NS_PORT);
			}
			string location=Encoding.ASCII.GetString(buf);
			var nsp = new NameServerProxy(new PyroURI(location));
			return nsp;
		}
	}
}

}
