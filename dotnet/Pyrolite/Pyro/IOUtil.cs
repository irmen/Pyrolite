/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.IO;
using System.Net.Sockets;

namespace Razorvine.Pyro
{

/// <summary>
/// Lowlevel I/O utilities.
/// </summary>
class IOUtil
{
	/**
	 * send a message to the outputstream.
	 */
	public static void send(Stream outs, byte[] message) {
		outs.Write(message,0,message.Length);
	}

	/**
	 * Receive a message of the given size from the inputstream.
	 * Makes sure the complete message is received, raises IOException otherwise.
	 */
	public static byte[] recv(Stream ins, int size) {
		byte [] bytes = new byte [size];
		int numRead = ins.Read(bytes, 0, size);
		if(numRead<=0) {
			throw new IOException("premature end of data");
		}
		while (numRead < size) {
			int len = ins.Read(bytes, numRead, size - numRead);
			if(len<=0) {
				throw new IOException("premature end of data");
			}
			numRead+=len;
		}
		return bytes;
	}
}

}