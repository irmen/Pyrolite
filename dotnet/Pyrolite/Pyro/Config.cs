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

	public const string PYROLITE_VERSION="1.9";
}

}

