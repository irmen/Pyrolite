/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.IO;
using System.Text;
using Razorvine.Pyrolite.Pickle;

namespace Razorvine.Pyrolite.Pyro
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
}

}
