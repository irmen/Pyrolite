package net.razorvine.pyro.test;

import java.io.IOException;
import java.io.UnsupportedEncodingException;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.FlameBuiltin;
import net.razorvine.pyro.FlameModule;
import net.razorvine.pyro.FlameRemoteConsole;
import net.razorvine.pyro.PyroProxy;


/**
 * Simple example that shows the use of Pyro with a Pyro Flame enabled server.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class FlameExample {

	static protected byte[] hmacKey;	// just ignore this if you don't specify a PYRO_HMAC_KEY environment var

	public static void main(String[] args) throws IOException {

		System.out.println("Testing Pyro flame server (make sure it's running on localhost 9999)...");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);

		setConfig();

		PyroProxy flame=new PyroProxy("localhost",9999,"Pyro.Flame");
		if(hmacKey!=null) flame.pyroHmacKey = hmacKey;

		System.out.println("builtin:");
		FlameBuiltin r_max=(FlameBuiltin)flame.call("builtin", "max");
		if(hmacKey!=null) r_max.setHmacKey(hmacKey);
		int maximum=(Integer)r_max.call(new int[]{22,99,1});
		System.out.println("maximum="+maximum);
		
		FlameModule r_module=(FlameModule)flame.call("module","socket");
		if(hmacKey!=null) r_module.setHmacKey(hmacKey);
		String hostname=(String)r_module.call("gethostname");
		
		System.out.println("hostname="+hostname);
		
		int sum=(Integer)flame.call("evaluate", "9+9");
		System.out.println("sum="+sum);
		
		flame.call("execute", "import sys; sys.stdout.write('HELLO FROM JAVA\\n')");
		
		FlameRemoteConsole console=(FlameRemoteConsole)flame.call("console");
		if(hmacKey!=null) console.setHmacKey(hmacKey);
		console.interact();
		console.close();
	}
	
	static void setConfig() {
		String hmackey=System.getenv("PYRO_HMAC_KEY");
		String hmackey_property=System.getProperty("PYRO_HMAC_KEY");
		if(hmackey_property!=null) {
			hmackey=hmackey_property;
		}
		if(hmackey!=null && hmackey.length()>0) {
			try {
				hmacKey=hmackey.getBytes("UTF-8");
			} catch (UnsupportedEncodingException e) {
				hmacKey=null;
			}
		}
		String tracedir=System.getenv("PYRO_TRACE_DIR");
		if(System.getProperty("PYRO_TRACE_DIR")!=null) {
			tracedir=System.getProperty("PYRO_TRACE_DIR");
		}
		Config.MSG_TRACE_DIR=tracedir;
		
		Config.SERIALIZER = Config.SerializerType.pickle;   // flame requires the pickle serializer
	}	
}
