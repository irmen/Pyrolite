package net.razorvine.pyro;

import java.io.Serializable;

/**
 * Minimalistic holders for Pyro config items.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 * @version 1.13
 */
public final class Config implements Serializable {
	private static final long serialVersionUID = 198635706890570066L;

	public static byte[] HMAC_KEY = null;
	public static String MSG_TRACE_DIR = null;
    public static int NS_PORT = 9090;
    public static int NS_BCPORT = 9091;
    public static int PROTOCOL_VERSION = 44;    // up to Pyro 4.19.  version 45 is for Pyro 4.20

	public final static String PYROLITE_VERSION = "1.13";
}
