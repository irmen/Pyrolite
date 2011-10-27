package net.razorvine.pyro.test;

import java.io.IOException;

import net.razorvine.pickle.PrettyPrint;
import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.Config;

/**
 * Test Pyro with the Pyro echo server.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class TestEcho {

	public static void main(String[] args) throws IOException {

		System.out.println("Testing Pyro echo server (make sure it's running on localhost 9999)...");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);

		// Config.HMAC_KEY="irmen".getBytes();
		
		PyroProxy p=new PyroProxy("localhost",9999,"test.echoserver");

		Object x=42;
		System.out.println("echo param:");
		PrettyPrint.print(x, System.out);
		Object result=p.call("echo", x);
		System.out.println("return value:");
		PrettyPrint.print(result, System.out);
		
		String s="This string is way too long. This string is way too long. This string is way too long. This string is way too long. ";
		s=s+s+s+s+s;
		System.out.println("echo param:");
		PrettyPrint.print(s, System.out);
		result=p.call("echo", s);
		System.out.println("return value:");
		PrettyPrint.print(result, System.out);

		System.out.println("error test.");
		try {
			result=p.call("error");
		} catch (PyroException e) {
			System.out.println("Pyro Exception! "+e);
			System.out.println("Pyro Exception cause: "+e.getCause());
			System.out.println("Pyro Exception remote traceback: >>>"+e._pyroTraceback+"<<<");
		}

		System.out.println("shutting down the test echo server.");
		p.call("shutdown");
	}
}
