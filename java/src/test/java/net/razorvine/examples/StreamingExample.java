package net.razorvine.examples;

import java.io.IOException;
import java.util.Scanner;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;

/**
 * Simple example that shows the use of the iterator item streaming feature.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class StreamingExample {

	static protected byte[] hmacKey = null; // "irmen".getBytes(); 
	
	
	@SuppressWarnings("unchecked")
	public static void main(String[] args) throws IOException {

		setConfig();
		/// Config.SERIALIZER = Config.SerializerType.pickle;

		System.out.println("Testing Pyro iterator item streaming");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);
		System.out.println("serializer used: " + Config.SERIALIZER);

		Scanner scanner = new Scanner(System.in);
		System.out.println("\r\nEnter the streaming server URI: ");
		String uri = scanner.next().trim();

		PyroProxy p = new PyroProxy(new PyroURI(uri));

		System.out.println("LIST:");
		Object result = p.call("list");
		java.lang.Iterable<Integer> iter = (Iterable<Integer>) result;
		for(int i: iter) {
			System.out.println(i);
		}
		
		System.out.println("ITERATOR:");
		result = p.call("iterator");
		iter = (Iterable<Integer>) result;
		for(int i: iter) {
			System.out.println(i);
		}
		
		System.out.println("GENERATOR:");
		result = p.call("generator");
		iter = (Iterable<Integer>) result;
		for(int i: iter) {
			System.out.println(i);
		}

		System.out.println("SLOW GENERATOR:");
		result = p.call("slow_generator");
		iter = (Iterable<Integer>) result;
		for(int i: iter) {
			System.out.println(i);
		}

		// tidy up:
		p.close();
		scanner.close();
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
