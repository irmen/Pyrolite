package net.razorvine.examples;

import java.io.IOException;
import java.util.Calendar;
import java.util.TimeZone;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.NameServerProxy;
import net.razorvine.pyro.PyroProxy;

/**
 * Simple example that shows the use of Pyro with timezone support.
 * (when using the pickle serializer)
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class TimezoneExample {

	public static void main(String[] args) throws IOException {

		System.out.println("Testing Pyro timezone example server (make sure it's running, with nameserver enabled, and pickle as serializer)...");
		System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);

		setConfig();

		NameServerProxy ns = NameServerProxy.locateNS(null);
		PyroProxy p = new PyroProxy(ns.lookup("example.timezones"));
		ns.close();

		
		Calendar cal;

		System.out.println("\nPYTZ...:");
		cal = (Calendar) p.call("pytz");
		System.out.println(cal);
		System.out.println("Timezone="+cal.getTimeZone());

		System.out.println("\nDATEUTIL...:");
		cal = (Calendar) p.call("dateutil");
		System.out.println(cal);
		System.out.println("Timezone="+cal.getTimeZone());

		System.out.println("\nECHO Timezone...:");
		cal = Calendar.getInstance();
		cal.set(Calendar.YEAR, 2015);
		cal.set(Calendar.MONTH, Calendar.MAY);
		cal.set(Calendar.DAY_OF_MONTH, 18);
		cal.set(Calendar.HOUR_OF_DAY, 23);
		cal.set(Calendar.MINUTE, 59);
		cal.set(Calendar.SECOND, 59);
		cal.set(Calendar.MILLISECOND, 998);
		cal.setTimeZone(TimeZone.getTimeZone("Europe/Amsterdam"));
		System.out.println("****" + javax.xml.bind.DatatypeConverter.printDateTime(cal));
		Calendar cal2 = (Calendar) p.call("echo", cal);
		System.out.println("****" + javax.xml.bind.DatatypeConverter.printDateTime(cal2));
		System.out.println(cal2);
		System.out.println("Timezone="+cal2.getTimeZone());
		if(!cal.equals(cal2))
			System.err.println("returned calendar is different!");
		
		// tidy up:
		p.close();
	}
	
	static void setConfig() {
		Config.SERIALIZER = Config.SerializerType.pickle;
		// Config.MSG_TRACE_DIR="L:/pyrolite_traces";
	}	
}
