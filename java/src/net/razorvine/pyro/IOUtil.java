package net.razorvine.pyro;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

/**
 * Lowlevel I/O utilities.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
class IOUtil
{
	/**
	 * send a message to the outputstream.
	 */
	public static void send(OutputStream out, byte[] message) throws IOException {
		out.write(message);
	}

	/**
	 * Receive a message of the given size from the inputstream.
	 * Makes sure the complete message is received, raises IOException otherwise.
	 */
	public static byte[] recv(InputStream in, int size) throws IOException {
		byte [] bytes = new byte [size];
		int numRead = in.read(bytes);
		if(numRead==-1) {
			throw new IOException("premature end of data");
		}
		while (numRead < size) {
		  int len = in.read(bytes, numRead, size - numRead);
		  if(len==-1) {
			  throw new IOException("premature end of data");
		  }
		  numRead+=len;
		}
		return bytes;
	}
}
