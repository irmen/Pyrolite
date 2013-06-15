package net.razorvine.pyro;

import java.io.Serializable;

/**
 * Minimalistic holders for Pyro config items.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 * @version 1.10
 */
public final class Config implements Serializable {
	private static final long serialVersionUID = 198635706890570066L;

	public static byte[] HMAC_KEY=null;
	public static String MSG_TRACE_DIR=null;
    public static int NS_PORT = 9090;
    public static int NS_BCPORT = 9091;

	public static String PYROLITE_VERSION="1.10";
}
