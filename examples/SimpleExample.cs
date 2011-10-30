using System;
using Razorvine.Pyro;

public class SimpleExample {

    public static void Main(string[] args)
    {
        using( NameServerProxy ns = NameServerProxy.locateNS(null) )
        {
            using( PyroProxy remoteobject = new PyroProxy(ns.lookup("Your.Pyro.Object")) )
            {
                object result = remoteobject.call("pythonmethod", 42, "hello", new int[]{1,2,3});
                string message = (string)result;   // cast to the type that 'pythonmethod' returns
                Console.WriteLine("result message="+message);
            }
        }
    }
}
