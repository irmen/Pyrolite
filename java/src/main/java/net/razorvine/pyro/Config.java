package net.razorvine.pyro;

import java.io.Serializable;


/**
 * Minimalistic holders for Pyro config items.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public final class Config implements Serializable {
	private static final long serialVersionUID = 2497120843470270662L;

	public static String MSG_TRACE_DIR = null;
	public static int NS_PORT = 9090;
	public static int NS_BCPORT = 9091;
	public static boolean SERPENT_INDENT = false;

	public final static int PROTOCOL_VERSION = 502;	  // Pyro5
	public final static String PYROLITE_VERSION = "5.0";

	public static String DAEMON_NAME = "Pyro.Daemon";
}
