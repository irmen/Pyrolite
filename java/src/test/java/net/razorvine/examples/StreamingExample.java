package net.razorvine.examples;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroProxy.StreamResultIterable;
import net.razorvine.pyro.PyroURI;

import java.io.IOException;
import java.util.Iterator;
import java.util.Scanner;

/**
 * Simple example that shows the use of the iterator item streaming feature.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class StreamingExample {

	@SuppressWarnings("unchecked")
	public static void main(String[] args) throws IOException {

		setConfig();

		System.out.println("Testing Pyro iterator item streaming");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);

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

		System.out.println("STOPPING GENERATOR HALFWAY:");
		result = p.call("slow_generator");
		Iterable<Integer> iterable = (Iterable<Integer>) result;
		Iterator<Integer> iterator = iterable.iterator();
		System.out.println(iterator.next());
		System.out.println(iterator.next());
		System.out.println("...stopping...");
		// the call below is a rather nasty way to force the iterator to close before reaching the end
		((StreamResultIterable.StreamResultIterator) iterator).close();

		iterable = null;
		iterator = null;

		// tidy up:
		p.close();
		scanner.close();
	}

	static void setConfig() {
		String tracedir=System.getenv("PYRO_TRACE_DIR");
		if(System.getProperty("PYRO_TRACE_DIR")!=null) {
			tracedir=System.getProperty("PYRO_TRACE_DIR");
		}

		Config.MSG_TRACE_DIR=tracedir;
	}
}
