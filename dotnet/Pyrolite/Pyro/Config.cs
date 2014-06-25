/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

namespace Razorvine.Pyro
{

/// <summary>
/// Minimalistic holder for Pyro config items.
/// </summary>
public static class Config  {
	
	public enum SerializerType {
		pickle,
		serpent
	};
	
	public static byte[] HMAC_KEY=null;
	public static string MSG_TRACE_DIR=null;
	public static int NS_PORT = 9090;
	public static int NS_BCPORT = 9091;
	public static bool SERPENT_INDENT = false;
	public static bool SERPENT_SET_LITERALS = false;     // set to true if talking to Python 3.2 or newer
	public static SerializerType SERIALIZER = SerializerType.serpent;

	public const int PROTOCOL_VERSION = 47;		// Pyro 4.26 
	public const string PYROLITE_VERSION="3.1";
}

}
