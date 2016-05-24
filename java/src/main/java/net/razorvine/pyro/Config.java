package net.razorvine.pyro;

import java.io.Serializable;


/**
 * Minimalistic holders for Pyro config items.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public final class Config implements Serializable {
	private static final long serialVersionUID = 197645607890570066L;

	public static String MSG_TRACE_DIR = null;
	public static int NS_PORT = 9090;
	public static int NS_BCPORT = 9091;

	public final static int PROTOCOL_VERSION = 48;	// Pyro 4.38 and later 
	public final static String PYROLITE_VERSION = "4.12";
	
	public enum SerializerType {
		pickle,
		serpent
	}

	public static boolean SERPENT_INDENT = false;
	public static boolean SERPENT_SET_LITERALS = false;     // set to true if talking to Python 3.2 or newer
	public static SerializerType SERIALIZER = SerializerType.serpent;
	public static boolean METADATA = true;		// get metadata from server?
	public static boolean PICKLE_CALENDAR_PYTZ_LOCALIZE = true;		// use pytz localize() pickle for Calendar with timezones 

	public static String DAEMON_NAME = "Pyro.Daemon";
}
