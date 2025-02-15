/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Razorvine.Pyro
{

/// <summary>
/// Minimalistic holder for Pyro config items.
/// </summary>
public static class Config  {
	
	public static string MSG_TRACE_DIR=null;
	public static int NS_PORT = 9090;
	public static int NS_BCPORT = 9091;
	public static bool SERPENT_INDENT = false;

	public const int PROTOCOL_VERSION = 502;	  // Pyro5
	public const string PYROLITE_VERSION="5.2";

	public const string DAEMON_NAME = "Pyro.Daemon";
}

}
