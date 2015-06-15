package net.razorvine.pyro;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.Serializable;
import java.lang.reflect.Field;
import java.net.Socket;
import java.net.UnknownHostException;
import java.nio.ByteBuffer;
import java.util.*;
import java.util.zip.DataFormatException;
import java.util.zip.Inflater;

import net.razorvine.pickle.PickleException;
import net.razorvine.pyro.serializer.PyroSerializer;

/**
 * Proxy for Pyro objects.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroProxy implements Serializable {

	private static final long serialVersionUID = -5564313476693913031L;
	public String hostname;
	public int port;
	public String objectid;
	public byte[] pyroHmacKey = null;		// per-proxy hmac key, used to be HMAC_KEY config item
	public UUID correlation_id = null;		// per-proxy correlation id (need to set/update this yourself)
	public Object pyroHandshake = "hello";	// data object that should be sent in the initial connection handshake message. Can be any serializable object.
	
	private transient int sequenceNr = 0;
	private transient Socket sock;
	private transient OutputStream sock_out;
	private transient InputStream sock_in;
	
	public Set<String> pyroMethods = new HashSet<String>();	// remote methods
	public Set<String> pyroAttrs = new HashSet<String>();	// remote attributes
	public Set<String> pyroOneway = new HashSet<String>();	// oneway methods
	

	/**
	 * No-args constructor for (un)pickling support
	 */
	public PyroProxy() {
	}
	
	/**
	 * Create a proxy for the remote Pyro object denoted by the uri
	 */
	public PyroProxy(PyroURI uri) throws UnknownHostException, IOException {
		this(uri.host, uri.port, uri.objectid);
	}

	/**
	 * Create a proxy for the remote Pyro object on the given host and port, with the given objectid/name.
	 */
	public PyroProxy(String hostname, int port, String objectid) throws UnknownHostException, IOException {
		this.hostname = hostname;
		this.port = port;
		this.objectid = objectid;
	}

	/**
	 * (re)connect the proxy to the remote Pyro daemon.
	 */
	protected void connect() throws UnknownHostException, IOException {
		if (sock == null) {
			sock = new Socket(hostname, port);
			sock.setKeepAlive(true);
			sock.setTcpNoDelay(true);
			sock_out = sock.getOutputStream();
			sock_in = sock.getInputStream();
			sequenceNr = 0;
			_handshake();
			
			if(Config.METADATA) {
				// obtain metadata if this feature is enabled, and the metadata is not known yet
				if(!pyroMethods.isEmpty() || !pyroAttrs.isEmpty()) {
					// not checking _pyroONeway because that feature already existed and it is not yet deprecated
					// log.debug("reusing existing metadata")
				} else {
					getMetadata(this.objectid);
				}
			}
		}
	}

	/**
	 * get metadata from server (methods, attrs, oneway, ...) and remember them in some attributes of the proxy
	 */
	protected void getMetadata(String objectId) throws PickleException, PyroException, IOException {
		// get metadata from server (methods, attrs, oneway, ...) and remember them in some attributes of the proxy
		if(objectId==null) objectId=this.objectid;
		if(sock==null) {
			connect();
			if(!pyroMethods.isEmpty() || !pyroAttrs.isEmpty())
				return;    // metadata has already been retrieved as part of creating the connection
		}
	
		// invoke the get_metadata method on the daemon
		@SuppressWarnings("unchecked")
		HashMap<String, Object> result = (HashMap<String, Object>) this.internal_call("get_metadata", Config.DAEMON_NAME, 0, false, new Object[] {objectId});
		if(result==null)
			return;
		
		_processMetadata(result);
	}

	/**
	 * Extract meta data and store it in the relevant properties on the proxy.
	 * If no attribute or method is exposed at all, throw an exception.
	 */
	private void _processMetadata(HashMap<String, Object> result) {
		// the collections in the result can be either an object[] or a HashSet<object>, depending on the serializer that is used
		Object methods = result.get("methods");
		Object attrs = result.get("attrs");
		Object oneways = result.get("oneways");
		
		if(methods instanceof Object[]) {
			Object[] methods_array = (Object[]) methods;
			this.pyroMethods = new HashSet<String>();
			for(int i=0; i<methods_array.length; ++i) {
				this.pyroMethods.add((String) methods_array[i]);
			}
		} else if(methods!=null) {
			@SuppressWarnings("unchecked")
			HashSet<String> methods_set = (HashSet<String>) methods;
			this.pyroMethods = methods_set;
		}
		if(attrs instanceof Object[]) {
			Object[] attrs_array = (Object[]) attrs;
			this.pyroAttrs = new HashSet<String>();
			for(int i=0; i<attrs_array.length; ++i) {
				this.pyroAttrs.add((String) attrs_array[i]);
			}
		} else if(attrs!=null) {
			@SuppressWarnings("unchecked")
			HashSet<String> attrs_set = (HashSet<String>) attrs;
			this.pyroAttrs = attrs_set;
		}
		if(oneways instanceof Object[]) {
			Object[] oneways_array = (Object[]) oneways;
			this.pyroOneway = new HashSet<String>();
			for(int i=0; i<oneways_array.length; ++i) {
				this.pyroOneway.add((String) oneways_array[i]);
			}
		} else if(oneways!=null) {
			@SuppressWarnings("unchecked")
			HashSet<String> oneways_set = (HashSet<String>) oneways;
			this.pyroOneway = oneways_set;
		}
		
		if(pyroMethods.isEmpty() && pyroAttrs.isEmpty()) {
			throw new PyroException("remote object doesn't expose any methods or attributes");
		}
	}

	/**
	 * Call a method on the remote Pyro object this proxy is for.
	 * @param method the name of the method you want to call
	 * @param arguments zero or more arguments for the remote method
	 * @return the result Object from the remote method call (can be anything, you need to typecast/introspect yourself).
	 */
	public Object call(String method, Object... arguments) throws PickleException, PyroException, IOException {
		return internal_call(method, null, 0, true, arguments);
	}

	/**
	 * Call a method on the remote Pyro object this proxy is for, using Oneway call semantics (return immediately).
	 * @param method the name of the method you want to call
	 * @param arguments zero or more arguments for the remote method
	 */
	public void call_oneway(String method, Object... arguments) throws PickleException, PyroException, IOException {
		internal_call(method, null, Message.FLAGS_ONEWAY, true, arguments);
	}

	/**
	 * Get the value of a remote attribute.
	 * @param attr the attribute name
	 */
	public Object getattr(String attr) throws PickleException, PyroException, IOException {
		return this.internal_call("__getattr__", null, 0, false, new Object[]{attr});
	}
	
	/**
	 * Set a new value on a remote attribute.
	 * @param attr the attribute name
	 * @param value the new value for the attribute
	 */
	public void setattr(String attr, Object value) throws PickleException, PyroException, IOException {
		this.internal_call("__setattr__", null, 0, false, new Object[] {attr, value});
	}

	/**
	 * Returns a sorted map with annotations to be sent with each message.
	 * Default behavior is to include the current correlation id (if it is set).
	 */
	public SortedMap<String, byte[]> annotations()
	{
		SortedMap<String,byte[]> ann = new TreeMap<String, byte[]>();
		if(correlation_id!=null) {
			long hi = correlation_id.getMostSignificantBits();
			long lo = correlation_id.getLeastSignificantBits();
			ann.put("CORR", ByteBuffer.allocate(16).putLong(hi).putLong(lo).array());
		}
		return ann;
	}
	
	/**
	 * Internal call method to actually perform the Pyro method call and process the result.
	 */
	private Object internal_call(String method, String actual_objectId, int flags, boolean checkMethodName, Object... parameters) throws PickleException, PyroException, IOException {
		if(actual_objectId==null) actual_objectId=this.objectid;
		synchronized (this) {
			connect();
			sequenceNr=(sequenceNr+1)&0xffff;		// stay within an unsigned short 0-65535
		}
		if(pyroAttrs.contains(method)) {
			throw new PyroException("cannot call an attribute");
		}
		if(pyroOneway.contains(method)) {
			flags |= Message.FLAGS_ONEWAY;
		}
		if(checkMethodName && Config.METADATA && !pyroMethods.contains(method)) {
			throw new PyroException(String.format("remote object '%s' has no exposed attribute or method '%s'", actual_objectId, method));
		}
		if (parameters == null)
			parameters = new Object[] {};
		PyroSerializer ser = PyroSerializer.getFor(Config.SERIALIZER);
		byte[] pickle = ser.serializeCall(actual_objectId, method, parameters, Collections.<String, Object> emptyMap());
		Message msg = new Message(Message.MSG_INVOKE, pickle, ser.getSerializerId(), flags, sequenceNr, annotations(), pyroHmacKey);
		Message resultmsg;
		synchronized (this.sock) {
			IOUtil.send(sock_out, msg.to_bytes());
			if(Config.MSG_TRACE_DIR!=null) {
				Message.TraceMessageSend(sequenceNr, msg.get_header_bytes(), msg.get_annotations_bytes(), msg.data);
			}
			pickle = null;

			if ((flags & Message.FLAGS_ONEWAY) != 0)
				return null;

			resultmsg = Message.recv(sock_in, new int[]{Message.MSG_RESULT}, pyroHmacKey);
		}
		if (resultmsg.seq != sequenceNr) {
			throw new PyroException("result msg out of sync");
		}
		if ((resultmsg.flags & Message.FLAGS_COMPRESSED) != 0) {
			_decompressMessageData(resultmsg);
		}
		if ((resultmsg.flags & Message.FLAGS_EXCEPTION) != 0) {
			Throwable rx = (Throwable) ser.deserializeData(resultmsg.data);
			if (rx instanceof PyroException) {
				throw (PyroException) rx;
			} else {
				PyroException px = new PyroException("remote exception occurred", rx);
				try {
					Field remotetbField = rx.getClass().getDeclaredField("_pyroTraceback");
					String remotetb = (String) remotetbField.get(rx);
					px._pyroTraceback = remotetb;
				} catch (Exception e) {
					// exception didn't provide a pyro remote traceback
				}
				throw px;
			}
		}
		return ser.deserializeData(resultmsg.data);
	}

	/**
	 * Decompress the data bytes in the given message (in place).
	 */
	private void _decompressMessageData(Message msg) {
		if((msg.flags & Message.FLAGS_COMPRESSED) == 0) {
			throw new IllegalArgumentException("message data is not compressed");
		}
		Inflater decompresser = new Inflater();
		decompresser.setInput(msg.data);
		ByteArrayOutputStream bos = new ByteArrayOutputStream(msg.data.length);
		byte[] buffer = new byte[8192];
		try {
			while (!decompresser.finished()) {
				int size = decompresser.inflate(buffer);
				bos.write(buffer, 0, size);
			}
			msg.data = bos.toByteArray();
			msg.flags &= ~Message.FLAGS_COMPRESSED;
			decompresser.end();
		} catch (DataFormatException e) {
			throw new PyroException("invalid compressed data: ", e);
		}
	}

	/**
	 * Close the network connection of this Proxy.
	 * If you re-use the proxy, it will automatically reconnect.
	 */
	public void close() {
		if (this.sock != null)
			try {
				this.sock_in.close();
				this.sock_out.close();
				this.sock.close();
				this.sock=null;
				this.sock_in=null;
				this.sock_out=null;
			} catch (IOException e) {
			}
	}

	public void finalize() {
		close();
		pyroHmacKey = null;
	}

	/**
	 * Perform the Pyro protocol connection handshake with the Pyro daemon.
	 */
	@SuppressWarnings("unchecked")
	protected void _handshake() throws IOException {
		// do connection handshake
		
		PyroSerializer ser = PyroSerializer.getFor(Config.SERIALIZER);
		Map<String, Object> handshakedata = new HashMap<String, Object>();
		handshakedata.put("handshake", pyroHandshake);
		if(Config.METADATA)
			handshakedata.put("object", objectid);
		byte[] data = ser.serializeData(handshakedata);
		int flags = Config.METADATA? Message.FLAGS_META_ON_CONNECT : 0;
		Message msg = new Message(Message.MSG_CONNECT, data, ser.getSerializerId(), flags, sequenceNr, annotations(), pyroHmacKey);
		IOUtil.send(sock_out, msg.to_bytes());
		if(Config.MSG_TRACE_DIR!=null) {
			Message.TraceMessageSend(sequenceNr, msg.get_header_bytes(), msg.get_annotations_bytes(), msg.data);
		}
		
		// process handshake response
		msg = Message.recv(sock_in, new int[]{Message.MSG_CONNECTOK, Message.MSG_CONNECTFAIL}, pyroHmacKey);
		Object handshake_response = "?";
		if(msg.data!=null) {
			if((msg.flags & Message.FLAGS_COMPRESSED) != 0) {
				_decompressMessageData(msg);
			}
			handshake_response = ser.deserializeData(msg.data);
		}
		if(msg.type==Message.MSG_CONNECTOK) {
			if((msg.flags & Message.FLAGS_META_ON_CONNECT) != 0) {
				HashMap<String, Object> response_dict = (HashMap<String, Object>)handshake_response;
				HashMap<String, Object> metadata = (HashMap<String, Object>) response_dict.get("meta");
				_processMetadata(metadata);
				handshake_response = response_dict.get("handshake");
				try {
					validateHandshake(handshake_response);
				} catch (IOException x) {
					close();
					throw x;
				}
			}
		} else if (msg.type==Message.MSG_CONNECTFAIL) {
			close();
			throw new PyroException("connection rejected, reason: "+handshake_response);
		} else {
			close();
			throw new PyroException("connect: invalid msg type "+msg.type+" received");
		}
	}
	
	/**
	 * Process and validate the initial connection handshake response data received from the daemon.
	 * Simply return without error if everything is ok.
     * Throw an IOException if something is wrong and the connection should not be made.
	 */
	public void validateHandshake(Object response) throws IOException
	{
		// override this in subclass
	}

	/**
	 * called by the Unpickler to restore state
	 * args(7): pyroUri, pyroOneway(hashset), pyroMethods(set), pyroAttrs(set), pyroTimeout, pyroHmacKey, pyroHandshake
	 */
	@SuppressWarnings("unchecked")
	public void __setstate__(Object[] args) throws IOException {
		if(args.length != 7) {
			throw new PyroException("invalid pickled proxy, using wrong pyro version?");
		}
		PyroURI uri=(PyroURI)args[0];
		this.hostname=uri.host;
		this.port=uri.port;
		this.objectid=uri.objectid;
		this.sock=null;
		this.sock_in=null;
		this.sock_out=null;
		this.correlation_id=null;
		if(args[1] instanceof Set)
			this.pyroOneway = (Set<String>) args[1];
		else
			this.pyroOneway = new HashSet<String>( (Collection<String>)args[1] );
		if(args[2] instanceof Set)
			this.pyroMethods = (Set<String>) args[2];
		else
			this.pyroMethods = new HashSet<String>( (Collection<String>)args[2] );
		if(args[3] instanceof Set)
			this.pyroAttrs = (Set<String>) args[3];
		else
			this.pyroAttrs = new HashSet<String>( (Collection<String>)args[3] );
		this.pyroHmacKey = (byte[]) args[5];
		this.pyroHandshake = args[6];
	}	
}
