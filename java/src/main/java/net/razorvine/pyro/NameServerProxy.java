package net.razorvine.pyro;

import java.io.IOException;
import java.io.Serializable;
import java.net.*;
import java.util.Map;
import java.util.Map.Entry;
import java.util.Set;

/**
 * A wrapper proxy for the Pyro Name Server,
 * to simplify the access to its remote methods.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class NameServerProxy extends PyroProxy implements Serializable {

	private static final long serialVersionUID = -3774989423700493399L;

	public NameServerProxy(PyroURI uri) throws UnknownHostException, IOException {
		this(uri.host, uri.port, uri.objectid);
	}

	public NameServerProxy(String hostname, int port, String objectid) throws UnknownHostException, IOException {
		super(hostname, port, objectid);
	}

	public NameServerProxy(String hostname, int port) throws IOException {
		this(hostname, port, "Pyro.NameServer");
	}

	public void ping() throws IOException {
		this.call("ping");
	}

	public PyroURI lookup(String name) throws IOException {
		return (PyroURI) this.call("lookup", name);
	}

	public Object[] lookup(String name, boolean return_metadata) throws IOException {
		Object[] result = (Object[]) this.call("lookup", name, return_metadata);
		if(return_metadata) {
			result[1] = getSetOfStrings(result[1]);  // convert the metadata string lists to sets
		}
		return result;
	}

	public int remove(String name, String prefix, String regex) throws IOException {
		return (Integer) this.call("remove", name, prefix, regex);
	}

	public void register(String name, PyroURI uri, boolean safe) throws IOException {
		this.call("register", name, uri, safe);
	}

	public void register(String name, PyroURI uri, boolean safe, String[] metadata) throws IOException {
		this.call("register", name, uri, safe, metadata);
	}

	@SuppressWarnings("unchecked")
	public Map<String, Object[]> list(String prefix, String regex) throws IOException {
		Map<String, Object[]> result = (Map<String, Object[]>) this.call("list", prefix, regex, true);
		// meta to sets
		for(Entry<String, Object[]> entry: result.entrySet()) {
			Object[] registration = entry.getValue();
			registration[1] = getSetOfStrings(registration[1]);
		}
		return result;
	}

	@SuppressWarnings("unchecked")
	public Map<String, Object[]> yplookup(String[] meta_all, String[] meta_any) throws IOException {
		Map<String, Object[]> result = (Map<String, Object[]>) this.call("yplookup", meta_all, meta_any, true);
		// meta to sets
		for(Entry<String, Object[]> entry: result.entrySet()) {
			Object[] registration = entry.getValue();
			registration[1] = getSetOfStrings(registration[1]);
		}
		return result;
	}


	public void set_metadata(String name, Set<String> metadata) throws IOException {
		this.call("set_metadata", name, metadata);
	}

	public static NameServerProxy locateNS(String host) throws IOException {
		return locateNS(host,0);
	}

	public static NameServerProxy locateNS(String host, int port) throws IOException {
		if(host!=null) {
			if(port==0)
				port=Config.NS_PORT;
			NameServerProxy proxy=new NameServerProxy(host, port);
			proxy.ping();
			return proxy;
		}
		if(port==0)
			port=Config.NS_BCPORT;
		DatagramSocket udpsock=new DatagramSocket();
		udpsock.setSoTimeout(3000);
		udpsock.setBroadcast(true);
		byte[] buf="GET_NSURI".getBytes();
		if(host==null)
			host="255.255.255.255";
		InetAddress address=InetAddress.getByName(host);
		DatagramPacket packet=new DatagramPacket(buf, buf.length, address, port);
		udpsock.send(packet);

		DatagramPacket response=new DatagramPacket(new byte[100], 100);
		try
		{
			udpsock.receive(response);
		}
		catch(SocketTimeoutException x)
		{
			// try localhost explicitly (if host wasn't localhost already)
			if(!host.startsWith("127.0") && !host.equals("localhost"))
				return locateNS("localhost", Config.NS_PORT);
			else
				throw x;
		}
		finally {
			udpsock.close();
		}
		String location=new String(response.getData(), 0, response.getLength());
		NameServerProxy nsp = new NameServerProxy(new PyroURI(location));
		return nsp;
	}
}

