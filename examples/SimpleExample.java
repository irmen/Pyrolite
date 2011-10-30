import java.io.IOException;
import net.razorvine.pyro.*;

public class SimpleExample {

    public static void main(String[] args) throws IOException
    {
        NameServerProxy ns = NameServerProxy.locateNS(null);
        PyroProxy remoteobject = new PyroProxy(ns.lookup("Your.Pyro.Object"));

        Object result = remoteobject.call("pythonmethod", 42, "hello", new int[]{1,2,3});
        String message = (String)result;   // cast to the type that 'pythonmethod' returns
        System.out.println("result message="+message);

        remoteobject.close();
        ns.close();
    }
}
