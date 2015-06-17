package net.razorvine.examples;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.util.List;
import java.util.Scanner;
import java.util.SortedMap;
import java.util.UUID;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;

/**
 * Test Pyro with the Handshake example server to see
 * how custom annotations and handshake handling is done.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */


/**
 * This custom proxy adds custom annotations to the pyro messages
 */
@SuppressWarnings("serial")
class CustomAnnotationsProxy extends PyroProxy
{
	public CustomAnnotationsProxy(PyroURI uri) throws IOException 
	{
		super(uri);
	}
	@Override
	public SortedMap<String, byte[]> annotations()
	{
		SortedMap<String, byte[]> ann = super.annotations();
		ann.put("XYZZ", "A custom annotation!".getBytes());
		return ann;
	}

	@Override
	@SuppressWarnings("unchecked")
	public void validateHandshake(Object handshake_response)
	{
		// the handshake example server returns a list.
		String msg = "";
		for(Object o: (List<Object>) handshake_response) {
			msg += o;
			msg += ",";
		}
		System.out.println("Proxy received handshake response data: "+msg);
	}
	
	@Override
	public void responseAnnotations(SortedMap<String, byte[]> annotations, int msgtype)
	{
		System.out.println("    Got response (type=" + msgtype + "). Annotations:");
		for(String ann: annotations.keySet()) {
			String value;
			if(ann.equals("CORR")) {
				ByteBuffer bb = ByteBuffer.wrap(annotations.get(ann));
				value = new UUID(bb.getLong(), bb.getLong()).toString();
			} else if (ann.equals("HMAC")) {
				value = "[...]";
			} else {
				value = annotations.get(ann).toString();
			}
			System.out.println("      " + ann + " -> " + value);
		}
		
	}
}


public class HandshakeExample {

	public static void main(String[] args) throws IOException {

		setConfig();
		System.out.println("Testing Pyro handshake and custom annotations. Make sure the server from the pyro handshake example is running.");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);
		System.out.println("serializer used: " + Config.SERIALIZER);

		Scanner scanner = new Scanner(System.in);
		System.out.println("\r\nEnter the server URI: ");
		String uri = scanner.next().trim();
		System.out.println("Enter the secret code as printed by the server: ");
		String secret = scanner.next().trim();
		scanner.close();
		
		PyroProxy p = new CustomAnnotationsProxy(new PyroURI(uri));
		p.pyroHandshake = secret;
		p.correlation_id = UUID.randomUUID();
		System.out.println("Correlation id set to: "+p.correlation_id);
		p.call("ping");
		System.out.println("Connection Ok!");
		
		// tidy up:
		p.close();
	}

	
	static void setConfig() {
		String tracedir=System.getenv("PYRO_TRACE_DIR");
		if(System.getProperty("PYRO_TRACE_DIR")!=null) {
			tracedir=System.getProperty("PYRO_TRACE_DIR");
		}
		
		String serializer=System.getenv("PYRO_SERIALIZER");
		if(System.getProperty("PYRO_SERIALIZER")!=null) {
			serializer=System.getProperty("PYRO_SERIALIZER");
		}
		if(serializer!=null) {
			Config.SERIALIZER = Enum.valueOf(Config.SerializerType.class, serializer);
		}

		Config.MSG_TRACE_DIR=tracedir;
	}	
}

