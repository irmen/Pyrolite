/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace Razorvine.Pyro
{

/// <summary>
/// Proxy for Pyro objects.
/// If you declare it as a 'PyroProxy' variable, you have to use the call() method to invoke remote methods.
/// If you declare it as a 'dynamic' variable, you can just invoke the remote methods on it directly, and access remote attributes directly.
/// </summary>
[Serializable]
public class PyroProxy : DynamicObject, IDisposable {

	public string hostname {get;set;}
	public int port {get;set;}
	public string objectid {get;set;}

	private ushort sequenceNr = 0;
	private TcpClient sock;
	private NetworkStream sock_stream;
	
	protected ISet<string> pyroMethods = new HashSet<string>();	// remote methods
	protected ISet<string> pyroAttrs = new HashSet<string>();	// remote attributes
	protected ISet<string> pyroOneway = new HashSet<string>();	// oneway methods

	/// <summary>
	/// No-args constructor for (un)pickling support
	/// </summary>
	public PyroProxy() {
	}

	/// <summary>
	/// Create a proxy for the remote Pyro object denoted by the uri
	/// </summary>
	public PyroProxy(PyroURI uri) : this(uri.host, uri.port, uri.objectid) {
	}

	/// <summary>
	/// Create a proxy for the remote Pyro object on the given host and port, with the given objectid/name.
	/// </summary>
	public PyroProxy(string hostname, int port, string objectid) {
		this.hostname = hostname;
		this.port = port;
		this.objectid = objectid;
	}

	/// <summary>
	/// Release resources when descructed
	/// </summary>
	~PyroProxy() {
		this.close();
	}

	public void Dispose() {
		close();
	}

	/// <summary>
	/// (re)connect the proxy to the remote Pyro daemon.
	/// </summary>
	protected void connect() {
		if (sock == null) {
			sock = new TcpClient();
			sock.Connect(hostname,port);
			sock.NoDelay=true;
			sock_stream=sock.GetStream();
			sequenceNr = 0;
			handshake();

			if (Config.METADATA) {
				// obtain metadata if this feature is enabled, and the metadata is not known yet
				if (pyroMethods.Count>0 || pyroAttrs.Count>0) {
					// not checking _pyroOneway because that feature already existed and people are already modifying it on the proxy
					// log.debug("reusing existing metadata")
				} else {
					GetMetadata(this.objectid);
				}
			}
		}
	}

	/// <summary>
	/// get metadata from server (methods, attrs, oneway, ...) and remember them in some attributes of the proxy
	/// </summary>
	protected void GetMetadata(string objectId) {
		// get metadata from server (methods, attrs, oneway, ...) and remember them in some attributes of the proxy
		objectId = objectId ?? this.objectid;
		if(sock==null) {
			connect();
			if(pyroMethods.Count>0 || pyroAttrs.Count>0)
				return;    // metadata has already been retrieved as part of creating the connection
		}
	
		//  invoke the get_metadata method on the daemon
		Hashtable result = this.internal_call("get_metadata", Config.DAEMON_NAME, 0, false, new string[] {objectId}) as Hashtable;
		if(result==null)
			return;
		
		// the collections in the result can be either an object[] or a HashSet<object>, depending on the serializer that is used
		if((result["methods"] as object[])!=null)
		{
			// assume object[]
			object[] methods = (result["methods"] as object[]) ;
			object[] attrs = (result["attrs"] as object[]);
			object[] oneway = (result["oneway"] as object[]);
			this.pyroMethods = new HashSet<string>(methods.Select(o=>o as string));
			this.pyroAttrs = new HashSet<string>(attrs.Select(o=>o as string));
			this.pyroOneway = new HashSet<string>(oneway.Select(o=>o as string));
		}
		else 
		{
			// assume hashset
			this.pyroMethods = new HashSet<string>((result["methods"] as HashSet<object>).Select(o=>o.ToString()));
			this.pyroAttrs = new HashSet<string>((result["attrs"] as HashSet<object>).Select(o=>o.ToString()));
			this.pyroOneway =new HashSet<string> ((result["oneway"] as HashSet<object>).Select(o=>o.ToString()));
		}
		
		if(pyroMethods.Count()==0 && pyroAttrs.Count()==0) {
			throw new PyroException("remote object doesn't expose any methods or attributes");
		}
	}

	/// <summary>
	/// Makes it easier to call methods on the proxy by intercepting the methods calls.
	/// You'll have to use the 'dynamic' type for your proxy object though.
	/// </summary>
	public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
	{
		result = call(binder.Name, args);
		return true;
	}

	// dynamic attribute retrieval
	public override bool TryGetMember(GetMemberBinder binder, out object result)
	{
		result = getattr(binder.Name);
		return true;
	}

	// dynamic attribute setting
	public override bool TrySetMember(SetMemberBinder binder, object value)
	{
		setattr(binder.Name, value);
		return true;
	}
	
	    
	/// <summary>
	/// Call a method on the remote Pyro object this proxy is for.
	/// </summary>
	/// <param name="method">the name of the method you want to call</param>
	/// <param name="arguments">zero or more arguments for the remote method</param>
	/// <returns>the result Object from the remote method call (can be anything, you need to typecast/introspect yourself).</returns>
	public object call(string method, params object[] arguments) {
		return internal_call(method, null, 0, true, arguments);
	}

	/// <summary>
	/// Call a method on the remote Pyro object this proxy is for, using Oneway call semantics (return immediately).
	/// (obsolete: just use call.)
	/// </summary>
	/// <param name="method">the name of the method you want to call</param>
	/// <param name="arguments">zero or more arguments for the remote method</param>
	[Obsolete("Pyro now figures out automatically what methods are oneway by getting metadata from the server. Just use call().")]
	public void call_oneway(string method, params object[] arguments) {
		internal_call(method, null, Message.FLAGS_ONEWAY, true, arguments);
	}

	/// <summary>
	/// Get the value of a remote attribute.
	/// </summary>
	/// <param name="attr">the attribute name</param>
	public object getattr(string attr) {
		return this.internal_call("__getattr__", null, 0, false, new object[]{attr});
	}
	
	
	/// <summary>
	/// Set a new value on a remote attribute.
	/// </summary>
	/// <param name="attr">the attribute name</param>
	/// <param name="value">the new value for the attribute</param>
	public void setattr(string attr, object value) {
		this.internal_call("__setattr__", null, 0, false, new object[] {attr, value});
	}
	
	
	/// <summary>
	/// Internal call method to actually perform the Pyro method call and process the result.
	/// </summary>
	private object internal_call(string method, string actual_objectId, ushort flags, bool checkMethodName, params object[] parameters) {
		actual_objectId = actual_objectId ?? this.objectid;
		lock(this) {
			connect();
			unchecked {
			    sequenceNr++;        // unchecked so this ushort wraps around 0-65535 instead of raising an OverflowException
			}
		}
		if(pyroAttrs.Contains(method)) {
			throw new PyroException("cannot call an attribute");
		}
		if(pyroOneway.Contains(method)) {
			flags |= Message.FLAGS_ONEWAY;
		}
		if(checkMethodName && Config.METADATA && !pyroMethods.Contains(method)) {
			throw new PyroException(string.Format("remote object '{0}' has no exposed attribute or method '{1}'", actual_objectId, method));
		}

		if (parameters == null)
			parameters = new object[] {};
		
		PyroSerializer ser = PyroSerializer.GetFor(Config.SERIALIZER);
		byte[] pickle = ser.serializeCall(actual_objectId, method, parameters, new Dictionary<string,object>(0));
		var msg = new Message(Message.MSG_INVOKE, pickle, ser.serializer_id, flags, sequenceNr, null);
		Message resultmsg;
		lock (this.sock) {
			IOUtil.send(sock_stream, msg.to_bytes());
			if(Config.MSG_TRACE_DIR!=null) {
				Message.TraceMessageSend(sequenceNr, msg.get_header_bytes(), msg.get_annotations_bytes(), msg.data);
			}
			pickle = null;

			if ((flags & Message.FLAGS_ONEWAY) != 0)
				return null;

			resultmsg = Message.recv(sock_stream, new ushort[]{Message.MSG_RESULT});
		}
		if (resultmsg.seq != sequenceNr) {
			throw new PyroException("result msg out of sync");
		}
		if ((resultmsg.flags & Message.FLAGS_COMPRESSED) != 0) {
			// we need to skip the first 2 bytes in the buffer due to a tiny mismatch between zlib-written 
			// data and the deflate data bytes that .net expects.
			// See http://www.chiramattel.com/george/blog/2007/09/09/deflatestream-block-length-does-not-match.html
			using(MemoryStream compressed=new MemoryStream(resultmsg.data, 2, resultmsg.data.Length-2, false)) {
				using(DeflateStream decompresser=new DeflateStream(compressed, CompressionMode.Decompress)) {
					MemoryStream bos = new MemoryStream(resultmsg.data.Length);
	        		byte[] buffer = new byte[4096];
	        		int numRead;
	        		while ((numRead = decompresser.Read(buffer, 0, buffer.Length)) != 0) {
	        		    bos.Write(buffer, 0, numRead);
	        		}
	        		resultmsg.data=bos.ToArray();
				}
			}
		}

		if ((resultmsg.flags & Message.FLAGS_EXCEPTION) != 0) {
			Exception rx = (Exception) ser.deserializeData(resultmsg.data);
			if (rx is PyroException) {
				throw (PyroException) rx;
			} else {
				PyroException px = new PyroException("remote exception occurred", rx);
				PropertyInfo remotetbProperty=rx.GetType().GetProperty("_pyroTraceback");
				if(remotetbProperty!=null) {
					string remotetb=(string)remotetbProperty.GetValue(rx,null);
					px._pyroTraceback=remotetb;
				}
				throw px;
			}
		}
		
		return ser.deserializeData(resultmsg.data);
	}

	/// <summary>
	/// Close the network connection of this Proxy.
	/// If you re-use the proxy, it will automatically reconnect.
	/// </summary>
	public void close() {
		if (this.sock != null) {
			if(this.sock_stream!=null) this.sock_stream.Close();
			this.sock.Client.Close();
			this.sock.Close();
			this.sock=null;
			this.sock_stream=null;
		}
	}

	/// <summary>
	/// Perform the Pyro protocol connection handshake with the Pyro daemon.
	/// </summary>
	protected void handshake() {
		// do connection handshake
		Message.recv(sock_stream, new ushort[]{Message.MSG_CONNECTOK});
		// message data is ignored for now, should be 'ok' :)
	}

	/// <summary>
	/// called by the Unpickler to restore state.
	/// </summary>
	/// <param name="args">pyroUri, pyroOneway(hashset), pyroTimeout</param>
	public void __setstate__(object[] args) {
		PyroURI uri=(PyroURI)args[0];
		// the only thing we need here is the uri.
		this.hostname=uri.host;
		this.port=uri.port;
		this.objectid=uri.objectid;
		this.sock=null;
		this.sock_stream=null;
	}	
}

}
