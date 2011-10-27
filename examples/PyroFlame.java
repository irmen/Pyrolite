import java.io.IOException;
import net.razorvine.pyro.*;

public class PyroFlame {

    public static void main(String[] args) throws IOException
    {
        System.out.println("Pyrolite version: "+Config.PYROLITE_VERSION);
        String hostname=args[0];
        int port=Integer.parseInt(args[1]);

        PyroProxy flame = new PyroProxy(hostname, port, "Pyro.Flame");
        FlameModule r_module = (FlameModule) flame.call("module", "socket");
        System.out.println("hostname=" + r_module.call("gethostname"));
        
        FlameRemoteConsole console = (FlameRemoteConsole) flame.call("console");
        console.interact();
        console.close();

        flame.close();
    }
}