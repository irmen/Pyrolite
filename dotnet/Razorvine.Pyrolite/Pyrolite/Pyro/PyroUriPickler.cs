/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Collections;
using System.IO;
using System.Text;

using Razorvine.Pickle;

namespace Razorvine.Pyro
{

/// <summary>
/// Pickler extension to be able to pickle Pyro URI objects.
/// </summary>
public class PyroUriPickler : IObjectPickler {

	public void pickle(object o, Stream outs, Pickler currentPickler) {
		PyroURI uri = (PyroURI) o;
		outs.WriteByte(Opcodes.GLOBAL);
		byte[] output=Encoding.Default.GetBytes("Pyro4.core\nURI\n");
		outs.Write(output,0,output.Length);
		outs.WriteByte(Opcodes.EMPTY_TUPLE);
		outs.WriteByte(Opcodes.NEWOBJ);
		outs.WriteByte(Opcodes.MARK);
		currentPickler.save(uri.protocol);
		currentPickler.save(uri.objectid);
		currentPickler.save(null);
		currentPickler.save(uri.host);
		currentPickler.save(uri.port);
		outs.WriteByte(Opcodes.TUPLE);
		outs.WriteByte(Opcodes.BUILD);
	}

	public static IDictionary ToSerpentDict(object obj)
	{
		PyroURI uri = (PyroURI)obj;
		var dict = new Hashtable();
		dict["state"] = new object[]{uri.protocol, uri.objectid, null, uri.host, uri.port};
		dict["__class__"] = "Pyro4.core.URI";
		return dict;
	}
	
	public static object FromSerpentDict(IDictionary dict)
	{
		object[] state = (object[])dict["state"];  // protocol, objectid, socketname, hostname, port
		return new PyroURI((string)state[1], (string)state[3], (int)state[4]);
	}
}

}
