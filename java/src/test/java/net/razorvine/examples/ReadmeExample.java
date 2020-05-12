package net.razorvine.examples;
import java.io.IOException;

import net.razorvine.pyro.*;

public class ReadmeExample {

	public static void main(String[] args) throws IOException
	{
	    NameServerProxy ns = NameServerProxy.locateNS(null);
	    PyroProxy remoteobject = new PyroProxy(ns.lookup("Your.Pyro.Object"));
	    Object result = remoteobject.call("pythonmethod", 42, "hello", new int[]{1,2,3});
	    String message = (String)result;  // cast to the type that 'pythonmethod' returns
	    System.out.println("result message="+message);
	    remoteobject.close();
	    ns.close();
	}
}

/**

The above code works if you start a Pyro nameserver and then run this Pyro server program:


import Pyro5.api

class Readme:
    @Pyro5.api.expose
    def pythonmethod(self, number, message, array):
        print("got number", number)
        print("got message", message)
        print("got array", array)
        return "all done!"

Pyro5.api.serve({
    Readme: "Your.Pyro.Object"
})


**/
