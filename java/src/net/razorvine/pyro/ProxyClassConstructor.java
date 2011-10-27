package net.razorvine.pyro;

import java.io.IOException;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;

public class ProxyClassConstructor implements IObjectConstructor {

	public PyroProxy construct(Object[] args) throws PickleException {
		if(args.length==0) {
			// no-arg constructor
			return new PyroProxy();
		} else if(args.length==1 && args[0] instanceof PyroURI) {
			// constructor with PyroURI arg
			try {
				return new PyroProxy((PyroURI)args[0]);
			} catch (IOException e) {
				throw new PickleException("can't create PyroProxy:" +e.getMessage());
			}
		} else if(args.length==3) {
			// constructor with hostname,port,objectid args
			String hostname=(String)args[0];
			Integer port=(Integer)args[1];
			String objectId=(String)args[2];
			try {
				return new PyroProxy(hostname, port, objectId);
			} catch (IOException e) {
				throw new PickleException("can't create PyroProxy:" +e.getMessage());
			}
		} else {
			throw new PickleException("invalid args for PyroProxy unpickling");
		}
	}

}
