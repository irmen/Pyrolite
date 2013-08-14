/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

namespace Razorvine.Pyro
{

/// <summary>
/// Minimalistic holders for Pyro config items.
/// </summary>
public static class Config  {
	public static byte[] HMAC_KEY=null;
	public static string MSG_TRACE_DIR=null;
    public static int NS_PORT = 9090;
    public static int NS_BCPORT = 9091;
    public static int PROTOCOL_VERSION = 44;    // up to Pyro 4.19.  version 45 is for Pyro 4.20

	public const string PYROLITE_VERSION="1.12";
}

}

