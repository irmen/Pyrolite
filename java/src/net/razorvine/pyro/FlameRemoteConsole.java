package net.razorvine.pyro;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.HashMap;

import net.razorvine.pickle.PickleException;

/**
 * Flame remote interactive console client. 
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */

public class FlameRemoteConsole {

	private PyroProxy remoteconsole;
	
	/**
	 * called by the Unpickler to restore state
	 */
	public void __setstate__(HashMap<?, ?> args) throws IOException {
		remoteconsole=(PyroProxy)args.get("remoteconsole"); 
	}

	public void interact() throws PickleException, PyroException, IOException {
		String banner=(String)remoteconsole.call("get_banner");
		System.out.println(banner);
		String ps1=">>> ";
		String ps2="... ";
		BufferedReader br = new BufferedReader(new InputStreamReader(System.in));
		boolean more=false;
		while(true) {
			if(more)
				System.out.print(ps2);
			else
				System.out.print(ps1);
			System.out.flush();
			String line=br.readLine();
			if(line==null) {
				// end of input
				System.out.println("");
				break;
			}
			try {
				Object[] result=(Object[])remoteconsole.call("push_and_get_output", line);
				if(result[0]!=null) {
					System.out.print(result[0]);
				}
				more=(Boolean)result[1];
			} catch(IOException x) {
				break;
			}
		}
		System.out.println("(Remote session ended)");
	}

	public void close() throws PickleException, PyroException, IOException {
		if(remoteconsole!=null) {
			remoteconsole.call("terminate");
			remoteconsole.close();
		}
	}
	
	public void setHmacKey(byte[] hmac) {
		remoteconsole.pyroHmacKey = hmac;
	}	
}
