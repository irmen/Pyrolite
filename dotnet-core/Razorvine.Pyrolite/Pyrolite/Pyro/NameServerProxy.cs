/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Razorvine.Pyro
{

/// <summary>
/// A wrapper proxy for the Pyro Name Server, 
/// to simplify the access to its remote methods.
/// </summary>
public class NameServerProxy : PyroProxy {

	public NameServerProxy(PyroURI uri) : this(uri.host, uri.port, uri.objectid) {
	}
	
	public NameServerProxy(string hostname, int port, string objectid) : base(hostname, port, objectid) {
	}
	
	public NameServerProxy(string hostname, int port) : this(hostname, port, "Pyro.NameServer") {
	}
		
	public void ping() {
		this.call("ping");
	}
	
	public PyroURI lookup(string name) {
		return (PyroURI) this.call("lookup", name);
	}
	
	public Tuple<PyroURI, ISet<string>> lookup(string name, bool return_metadata) {
		object[] result = (object[])this.call("lookup", name, return_metadata);
		PyroURI uri = (PyroURI) result[0];
		var metastrings = GetStringSet(result[1]);
		return new Tuple<PyroURI, ISet<string>>(uri, metastrings);
	}

	public int remove(string name, string prefix, string regex) {
		return (int) this.call("remove", name, prefix, regex);
	}

	public void register(string name, PyroURI uri, bool safe) {
		this.call("register", name, uri, safe);
	}
	
	public void register(string name, PyroURI uri, bool safe, IEnumerable<string> metadata) {
		this.call("register", name, uri, safe, metadata);
	}

	public void set_metadata(string name, ISet<string> metadata) {
		this.call("set_metadata", name, metadata);
	}

	public IDictionary<string,string> list(string prefix, string regex) {
		IDictionary hash = (IDictionary) this.call("list", prefix, regex);
		IDictionary<string,string> typed=new Dictionary<string,string>(hash.Count);
		foreach(object name in hash.Keys) {
			typed[(string)name]=(string)hash[name];
		}
		return typed;
	}
	
	public IDictionary<string,string> list(string prefix, string regex, IEnumerable<string> metadata_all, IEnumerable<string> metadata_any) {
		IDictionary hash = (IDictionary) this.call("list", prefix, regex, metadata_all, metadata_any);
		IDictionary<string,string> typed=new Dictionary<string,string>(hash.Count);
		foreach(object name in hash.Keys) {
			typed[(string)name]=(string)hash[name];
		}
		return typed;
	}

	public IDictionary<string, Tuple<string, ISet<string>>> list_with_meta(string prefix, string regex) {
		IDictionary hash = (IDictionary) this.call("list", prefix, regex, null, null, true);
		IDictionary<string, Tuple<string, ISet<string>>> typed=new Dictionary<string, Tuple<string, ISet<string>>>(hash.Count);
		foreach(object name in hash.Keys) {
			object[] o = (object[]) hash[name];
			string uri = (string) o[0];
			var metastrings = GetStringSet(o[1]);
			typed[(string)name] =  new Tuple<string, ISet<string>>(uri, metastrings);
		}
		return typed;
	}

	public IDictionary<string, Tuple<string, ISet<string>>> list_with_meta(string prefix, string regex, IEnumerable<string> metadata_all, IEnumerable<string> metadata_any) {
		IDictionary hash = (IDictionary) this.call("list", prefix, regex, metadata_all, metadata_any, true);
		IDictionary<string, Tuple<string, ISet<string>>> typed=new Dictionary<string, Tuple<string, ISet<string>>>(hash.Count);
		foreach(object name in hash.Keys) {
			object[] o = (object[]) hash[name];
			string uri = (string) o[0];
			var metastrings = GetStringSet(o[1]);
			typed[(string)name] =  new Tuple<string, ISet<string>>(uri, metastrings);
		}
		return typed;
	}

	public static NameServerProxy locateNS(string host, int port=0, byte[] hmacKey=null) {
		if(host!=null) {
			if(port==0)
				port=Config.NS_PORT;
			NameServerProxy proxy=new NameServerProxy(host, port);
			proxy.pyroHmacKey=hmacKey;
			proxy.ping();
			return proxy;
		}
		if(port==0)
			port=Config.NS_BCPORT;
		
		IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Broadcast, port);
		using(UdpClient udpclient=new UdpClient()) {
			udpclient.Client.ReceiveTimeout = 2000;
			udpclient.EnableBroadcast=true;
			byte[] buf=Encoding.ASCII.GetBytes("GET_NSURI");
			udpclient.Send(buf, buf.Length, ipendpoint);
			IPEndPoint source=null;
			try {
				buf=udpclient.Receive(ref source);
			} catch (SocketException) {
				// try localhost explicitly (if host wasn't localhost already)
				if(host==null || (!host.StartsWith("127.0") && host!="localhost"))
					return locateNS("localhost", Config.NS_PORT, hmacKey);
				else
					throw;
			}
			string location=Encoding.ASCII.GetString(buf);
			var nsp = new NameServerProxy(new PyroURI(location));
			nsp.pyroHmacKey = hmacKey;
			return nsp;
		}
	}
}

}
