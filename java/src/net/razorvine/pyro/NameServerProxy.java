package net.razorvine.pyro;

import java.io.IOException;
import java.io.Serializable;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.UnknownHostException;
import java.util.Map;

import net.razorvine.pickle.PickleException;

/**
 * A wrapper proxy for the Pyro Name Server, 
 * to simplify the access to its remote methods.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class NameServerProxy extends PyroProxy implements Serializable {

	private static final long serialVersionUID = -3774989423700492289L;

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
	
	public int remove(String name, String prefix, String regex) throws PickleException, IOException {
		return (Integer) this.call("remove", name, prefix, regex);
	}

	public void register(String name, PyroURI uri, boolean safe) throws PickleException, IOException {
		this.call("register", name, uri, safe);
	}
	
	@SuppressWarnings("unchecked")
	public Map<String,String> list(String prefix, String regex) throws PickleException, IOException {
		return (Map<String,String>) this.call("list", prefix, regex);
	}
	
	public static NameServerProxy locateNS(String host) throws IOException {
		return locateNS(host,0);
	}
	
	public static NameServerProxy locateNS(String host, int port) throws IOException {
		if(host!=null) {
			if(port==0)
				port=9090;
			NameServerProxy proxy=new NameServerProxy(host, port);
			proxy.ping();
			return proxy;
		}
		if(port==0)
			port=9091;
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
		udpsock.receive(response);
		String location=new String(response.getData(), 0, response.getLength());
		return new NameServerProxy(new PyroURI(location));
	}
}
