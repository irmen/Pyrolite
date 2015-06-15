package net.razorvine.examples;

import java.io.IOException;
import java.util.SortedMap;

import net.razorvine.pickle.PrettyPrint;
import net.razorvine.pyro.Config;
import net.razorvine.pyro.NameServerProxy;
import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;

/**
 * Simple example that shows the use of Pyro with the Pyro echo server.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class EchoExample {

	static protected byte[] hmacKey = null; // "irmen".getBytes(); 
	
	
	public static void main(String[] args) throws IOException {

		System.out.println("Testing Pyro echo server (make sure it's running, with nameserver enabled)...");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);

		setConfig();

		NameServerProxy ns = NameServerProxy.locateNS(null, hmacKey);
		PyroProxy p = new PyroProxy(ns.lookup("test.echoserver"));
		p.pyroHmacKey = hmacKey;
		ns.close();
		
		// PyroProxy p=new PyroProxy("localhost",9999,"test.echoserver");

		Object x=42;
		System.out.println("echo param:");
		PrettyPrint.print(x);
		Object result=p.call("echo", x);
		System.out.println("return value:");
		PrettyPrint.print(result);
		
		String s="This string is way too long. This string is way too long. This string is way too long. This string is way too long. ";
		s=s+s+s+s+s;
		System.out.println("echo param:");
		PrettyPrint.print(s);
		result=p.call("echo", s);
		System.out.println("return value:");
		PrettyPrint.print(result);

		// echo a pyro proxy and validate that all relevant attributes are also present on the proxy we got back.
		System.out.println("proxy test.");
		result = p.call("echo", p);
		PyroProxy p2 = (PyroProxy) result;
		System.out.println("response proxy: " + p2);
		assert (p2.objectid=="test.echoserver");
		assert ((String)p2.pyroHandshake == "banana");
		assert (p2.pyroMethods.size() == 8);
		if(p2.pyroHmacKey!=null) {
			String hmac2 = new String(p2.pyroHmacKey);
			assert (hmac2==new String(hmacKey));
		}

		System.out.println("error test.");
		try {
			result=p.call("error");
		} catch (PyroException e) {
			System.out.println("Pyro Exception (expected)! "+e.getMessage());
			System.out.println("Pyro Exception cause: "+e.getCause());
			System.out.println("Pyro Exception remote traceback:\n>>>\n"+e._pyroTraceback+"<<<");
		}

		System.out.println("shutting down the test echo server.");
		p.call("shutdown");
		
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


/**
 * This custom proxy adds custom annotations to the pyro messages
 */
@SuppressWarnings("serial")
class CustomProxy extends PyroProxy
{
	public CustomProxy(PyroURI uri) throws IOException 
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
}