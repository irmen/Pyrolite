package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.util.Map;

import net.razorvine.pickle.Pickler;
import net.razorvine.pickle.Unpickler;
import net.razorvine.pickle.objects.AnyClassConstructor;
import net.razorvine.pyro.DummyPyroSerializer;
import net.razorvine.pyro.FlameBuiltin;
import net.razorvine.pyro.FlameModule;
import net.razorvine.pyro.FlameRemoteConsole;
import net.razorvine.pyro.Message;
import net.razorvine.pyro.ProxyClassConstructor;
import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;

public class PickleSerializer extends PyroSerializer {

	static {
		Unpickler.registerConstructor("Pyro4.errors", "PyroError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "CommunicationError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "ConnectionClosedError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "TimeoutError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "ProtocolError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "NamingError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "DaemonError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "SecurityError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "SerializeError",	new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.errors", "MessageTooLargeError", new AnyClassConstructor(PyroException.class));
		Unpickler.registerConstructor("Pyro4.core", "Proxy", new ProxyClassConstructor());
		Unpickler.registerConstructor("Pyro4.util", "Serializer", new AnyClassConstructor(DummyPyroSerializer.class));
		Unpickler.registerConstructor("Pyro4.utils.flame", "FlameBuiltin", new AnyClassConstructor(FlameBuiltin.class));
		Unpickler.registerConstructor("Pyro4.utils.flame", "FlameModule", new AnyClassConstructor(FlameModule.class));
		Unpickler.registerConstructor("Pyro4.utils.flame", "RemoteInteractiveConsole", new AnyClassConstructor(FlameRemoteConsole.class));
		// make sure a PyroURI can also be pickled even when not directly imported:
		Unpickler.registerConstructor("Pyro4.core", "URI", new AnyClassConstructor(PyroURI.class));
		Pickler.registerCustomPickler(PyroURI.class, new PyroUriPickler());
		Pickler.registerCustomPickler(PyroProxy.class, new PyroProxyPickler());
		Pickler.registerCustomPickler(PyroException.class, new PyroExceptionPickler());
	}

	@Override
	public int getSerializerId() {
		return Message.SERIALIZER_PICKLE; 
	}

	@Override
	public byte[] serializeCall(String objectId, String method, Object[] vargs, Map<String, Object> kwargs) throws IOException {
		Pickler p=new Pickler();
		Object[] invokeparams = new Object[] {objectId, method, vargs, kwargs};
		byte[] result = p.dumps(invokeparams);
		p.close();
		return result;
	}

	@Override
	public byte[] serializeData(Object obj) throws IOException {
		Pickler p=new Pickler();
		byte[] result = p.dumps(obj);
		p.close();
		return result;
	}

	@Override
	public Object deserializeData(byte[] data) throws IOException {
		Unpickler u=new Unpickler();
		Object result=u.loads(data);
		u.close();
		return result;
	}

}
