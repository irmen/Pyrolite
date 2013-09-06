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
import net.razorvine.pyro.serializer.PyroSerializer;

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
		call(method, Message.FLAGS_ONEWAY, arguments);
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
		PyroSerializer ser = PyroSerializer.getFor(Config.SERIALIZER);
		byte[] pickle = ser.serializeCall(objectid, method, parameters, Collections.<String, Object> emptyMap());
		Message msg = new Message(Message.MSG_INVOKE, pickle, ser.getSerializerId(), flags, sequenceNr, null);
		Message resultmsg;
		synchronized (this.sock) {
			IOUtil.send(sock_out, msg.to_bytes());
			if(Config.MSG_TRACE_DIR!=null) {
				Message.TraceMessageSend(sequenceNr, msg.get_header_bytes(), msg.get_annotations_bytes(), msg.data);
			}
			pickle = null;

			if ((flags & Message.FLAGS_ONEWAY) != 0)
				return null;

			resultmsg = Message.recv(sock_in, new int[]{Message.MSG_RESULT});
		}
		if (resultmsg.seq != sequenceNr) {
			throw new PyroException("result msg out of sync");
		}
		if ((resultmsg.flags & Message.FLAGS_COMPRESSED) != 0) {
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
		Message.recv(sock_in, new int[]{Message.MSG_CONNECTOK});
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
