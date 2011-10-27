using System;
using Razorvine.Pyrolite.Pyro;

public class PyroFlameExample {

    public static void Main(string[] args)
    {
        Console.WriteLine("Pyrolite version: "+Config.PYROLITE_VERSION);
        string hostname=(string)args[0];
        int port=int.Parse(args[1]);
        using(var flame = new PyroProxy(hostname,port,"Pyro.Flame"))
        {
            dynamic r_module = flame.call("module","socket");
            Console.WriteLine("hostname=" + r_module.call("gethostname"));
            
            var console=(FlameRemoteConsole)flame.call("console");
            console.interact();
            console.close();
        }
    }
}