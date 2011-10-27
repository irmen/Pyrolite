package net.razorvine.pyro;

import java.io.Serializable;

/**
 * Minimalistic holders for Pyro config items.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 * @version 1.0
 */
public final class Config implements Serializable {
	private static final long serialVersionUID = 751820443534985644L;
	public static byte[] HMAC_KEY=null;

	public static String PYROLITE_VERSION="1.0";
}
