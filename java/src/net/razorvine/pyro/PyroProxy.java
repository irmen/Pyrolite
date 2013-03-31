package net.razorvine.pyro;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.Serializable;
import java.lang.reflect.Field;
import java.net.Socket;
import java.net.UnknownHostException;
import java.util.Collections;
import java.util.zip.DataFormatException;
import java.util.zip.Inflater;

import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.Pickler;
import net.razorvine.pickle.Unpickler;
import net.razorvine.pickle.objects.AnyClassConstructor;

/**
 * Proxy for Pyro objects.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroProxy implements Serializable {

	private static final long serialVersionUID = -5675423476693913030L;
	public String hostname;
	public int port;
	public String objectid;

	private transient int sequenceNr = 0;
	private transient Socket sock;
	private transient OutputStream sock_out;
	private transient InputStream sock_in;

	static {
		RegisterPickleConstructors();
	}

	/**
	 * register Pyro specific constructors with the Pickle library.
	 */
	public static void RegisterPickleConstructors()
	{
		Unpickler.registerConstructor("Pyro4.errors", "PyroError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "CommunicationError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "ConnectionClosedError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "TimeoutError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "ProtocolError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "NamingError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "DaemonError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "SecurityError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "AsyncResultTimeout",	new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.core", "Proxy", new ProxyClassConstructor());
		Unpickler.registerConstructor("Pyro4.util", "Serializer", new AnyClassConstructor(DummyPyroSerializer.class));
		Unpickler.registerConstructor("Pyro4.utils.flame", "FlameBuiltin", new AnyClassConstructor(FlameBuiltin.class));
		Unpickler.registerConstructor("Pyro4.utils.flame", "FlameModule", new AnyClassConstructor(FlameModule.class));
		Unpickler.registerConstructor("Pyro4.utils.flame", "RemoteInteractiveConsole", new AnyClassConstructor(FlameRemoteConsole.class));
		// make sure a PyroURI can also be pickled even when not directly imported:
		Unpickler.registerConstructor("Pyro4.core", "URI", new AnyClassConstructor(PyroURI.class));
		Pickler.registerCustomPickler(PyroURI.class, new PyroUriPickler());
	}
	
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
			handshake();
		}
	}

	/**
	 * Call a method on the remote Pyro object this proxy is for.
	 * @param method the name of the method you want to call
	 * @param arguments zero or more arguments for the remote method
	 * @return the result Object from the remote method call (can be anything, you need to typecast/introspect yourself).
	 */
	public Object call(String method, Object... arguments) throws PickleException, PyroException, IOException {
		return call(method, 0, arguments);
	}

	/**
	 * Call a method on the remote Pyro object this proxy is for, using Oneway call semantics (return immediately).
	 * @param method the name of the method you want to call
	 * @param arguments zero or more arguments for the remote method
	 */
	public void call_oneway(String method, Object... arguments) throws PickleException, PyroException, IOException {
		call(method, MessageFactory.FLAGS_ONEWAY, arguments);
	}

	/**
	 * Internal call method to actually perform the Pyro method call and process the result.
	 */
	private Object call(String method, int flags, Object... parameters) throws PickleException, PyroException, IOException {
		synchronized (this) {
			connect();
			sequenceNr=(sequenceNr+1)&0xffff;		// stay within an unsigned short 0-65535
		}
		if (parameters == null)
			parameters = new Object[] {};
		Object[] invokeparams = new Object[] { objectid, method, parameters, // vargs
				Collections.EMPTY_MAP // kwargs
		};
		Pickler pickler=new Pickler(false);
		byte[] pickle = pickler.dumps(invokeparams);
		pickler.close();
		byte[] headerdata = MessageFactory.createMsgHeader(MessageFactory.MSG_INVOKE, pickle, flags, sequenceNr);
		Message resultmsg;
		synchronized (this.sock) {
			IOUtil.send(sock_out, headerdata);
			IOUtil.send(sock_out, pickle);
			if(Config.MSG_TRACE_DIR!=null) {
				MessageFactory.TraceMessageSend(sequenceNr, headerdata, pickle);
			}
			pickle = null;
			headerdata = null;

			if ((flags & MessageFactory.FLAGS_ONEWAY) != 0)
				return null;

			resultmsg = MessageFactory.getMessage(sock_in, MessageFactory.MSG_RESULT);
		}
		if (resultmsg.sequence != sequenceNr) {
			throw new PyroException("result msg out of sync");
		}
		if ((resultmsg.flags & MessageFactory.FLAGS_COMPRESSED) != 0) {
			Inflater decompresser = new Inflater();
			decompresser.setInput(resultmsg.data);
			ByteArrayOutputStream bos = new ByteArrayOutputStream(resultmsg.data.length);
			byte[] buffer = new byte[8192];
			try {
				while (!decompresser.finished()) {
					int size = decompresser.inflate(buffer);
					bos.write(buffer, 0, size);
				}
				resultmsg.data = bos.toByteArray();
				decompresser.end();
			} catch (DataFormatException e) {
				throw new PyroException("invalid compressed data: ", e);
			}
		}
		if ((resultmsg.flags & MessageFactory.FLAGS_EXCEPTION) != 0) {
			Unpickler unpickler=new Unpickler();
			Throwable rx = (Throwable) unpickler.loads(resultmsg.data);
			unpickler.close();
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
		Unpickler unpickler=new Unpickler();
		Object result=unpickler.loads(resultmsg.data);
		unpickler.close();
		return result;
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
	}

	/**
	 * Perform the Pyro protocol connection handshake with the Pyro daemon.
	 */
	protected void handshake() throws IOException {
		// do connection handshake
		MessageFactory.getMessage(sock_in, MessageFactory.MSG_CONNECTOK);
		// message data is ignored for now, should be 'ok' :)
	}

	/**
	 * called by the Unpickler to restore state
	 * args: pyroUri, pyroOneway(hashset), pyroSerializer, pyroTimeout
	 */
	public void __setstate__(Object[] args) throws IOException {
		PyroURI uri=(PyroURI)args[0];
		// ignore the oneway hashset, the serializer object and the timeout 
		// the only thing we need here is the uri.
		this.hostname=uri.host;
		this.port=uri.port;
		this.objectid=uri.objectid;
		this.sock=null;
		this.sock_in=null;
		this.sock_out=null;
	}	
}
