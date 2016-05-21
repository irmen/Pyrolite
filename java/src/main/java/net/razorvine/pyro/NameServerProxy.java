package net.razorvine.pyro;

import java.io.IOException;
import java.io.Serializable;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.SocketTimeoutException;
import java.net.UnknownHostException;
import java.util.Map;
import java.util.Map.Entry;
import java.util.Set;

import net.razorvine.pickle.PickleException;

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
		
	public void ping() throws PickleException, IOException {
		this.call("ping");
	}
	
	public PyroURI lookup(String name) throws PickleException, IOException {
		return (PyroURI) this.call("lookup", name);
	}
	
	public Object[] lookup(String name, boolean return_metadata) throws PickleException, IOException {
		Object[] result = (Object[]) this.call("lookup", name, return_metadata);
		if(return_metadata) {
			result[1] = getSetOfStrings(result[1]);  // convert the metadata string lists to sets
		}
		return result;
	}

	public int remove(String name, String prefix, String regex) throws PickleException, IOException {
		return (Integer) this.call("remove", name, prefix, regex);
	}

	public void register(String name, PyroURI uri, boolean safe) throws PickleException, IOException {
		this.call("register", name, uri, safe);
	}
	
	public void register(String name, PyroURI uri, boolean safe, String[] metadata) throws PickleException, IOException {
		this.call("register", name, uri, safe, metadata);
	}

	@SuppressWarnings("unchecked")
	public Map<String,String> list(String prefix, String regex) throws PickleException, IOException {
		return (Map<String,String>) this.call("list", prefix, regex);
	}
	
	@SuppressWarnings("unchecked")
	public Map<String,String> list(String prefix, String regex, String[] metadata_all, String[] metadata_any) throws PickleException, IOException {
		return (Map<String,String>) this.call("list", prefix, regex, metadata_all, metadata_any);
	}

	@SuppressWarnings("unchecked")
	public Map<String, Object[]> list_with_meta(String prefix, String regex) throws PickleException, IOException {
		Map<String, Object[]> result = (Map<String, Object[]>) this.call("list", prefix, regex, null, null, true);
		// meta to sets
		for(Entry<String, Object[]> entry: result.entrySet()) {
			Object[] registration = entry.getValue();
			registration[1] = getSetOfStrings(registration[1]);
		}
		return result;
	}
	
	@SuppressWarnings("unchecked")
	public Map<String, Object[]> list_with_meta(String prefix, String regex, String[] metadata_all, String[] metadata_any) throws PickleException, IOException {
		Map<String, Object[]> result = (Map<String, Object[]>) this.call("list", prefix, regex, metadata_all, metadata_any, true);
		// meta to sets
		for(Entry<String, Object[]> entry: result.entrySet()) {
			Object[] registration = entry.getValue();
			registration[1] = getSetOfStrings(registration[1]);
		}
		return result;
	}

	public void set_metadata(String name, Set<String> metadata) throws PickleException, IOException {
		this.call("set_metadata", name, metadata);
	}

	public static NameServerProxy locateNS(String host) throws IOException {
		return locateNS(host,0,null);
	}

	public static NameServerProxy locateNS(String host, byte[] hmacKey) throws IOException {
		return locateNS(host,0,hmacKey);
	}
	
	public static NameServerProxy locateNS(String host, int port, byte[] hmacKey) throws IOException {
		if(host!=null) {
			if(port==0)
				port=Config.NS_PORT;
			NameServerProxy proxy=new NameServerProxy(host, port);
			proxy.pyroHmacKey = hmacKey;
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
				return locateNS("localhost", Config.NS_PORT, hmacKey);
			else
				throw x;
		}
		finally {
			udpsock.close();
		}
		String location=new String(response.getData(), 0, response.getLength());
		NameServerProxy nsp = new NameServerProxy(new PyroURI(location));
		nsp.pyroHmacKey = hmacKey;
		return nsp;
	}
}

