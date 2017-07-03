package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Base64;

import net.razorvine.pyro.PyroException;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;
import net.razorvine.serpent.IClassSerializer;

/**
 * Serpent extension to be able to serialize PyroProxy objects with Serpent.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PyroProxySerpent implements IClassSerializer {

	public static Object FromSerpentDict(Map<Object, Object> dict) throws IOException {
		// note: the state array received in the dict conforms to the list produced by Pyro4's Proxy.__getstate_for_dict__
		// that means, we get an array of length 8:  (the same as with convert, below!)
		// uri, oneway set, methods set, attrs set, timeout, hmac_key, handshake, maxretries  (in this order)
		Object[] state = (Object[])dict.get("state");
		PyroURI uri = new PyroURI((String)state[0]);
		PyroProxy proxy = new PyroProxy(uri);
		
		// the following nasty piece of code is similar to _processMetadata from the PyroProxy
		// this is because the three collections can either be an array or a set
		Object methods = state[2];
		Object attrs = state[3];
		Object oneways = state[1];
		
		if(methods instanceof Object[]) {
			Object[] methods_array = (Object[]) methods;
			proxy.pyroMethods = new HashSet<String>();
			for(int i=0; i<methods_array.length; ++i) {
				proxy.pyroMethods.add((String) methods_array[i]);
			}
		} else if(methods!=null) {
			@SuppressWarnings("unchecked")
			HashSet<String> methods_set = (HashSet<String>) methods;
			proxy.pyroMethods = methods_set;
		}
		if(attrs instanceof Object[]) {
			Object[] attrs_array = (Object[]) attrs;
			proxy.pyroAttrs = new HashSet<String>();
			for(int i=0; i<attrs_array.length; ++i) {
				proxy.pyroAttrs.add((String) attrs_array[i]);
			}
		} else if(attrs!=null) {
			@SuppressWarnings("unchecked")
			HashSet<String> attrs_set = (HashSet<String>) attrs;
			proxy.pyroAttrs = attrs_set;
		}
		if(oneways instanceof Object[]) {
			Object[] oneways_array = (Object[]) oneways;
			proxy.pyroOneway = new HashSet<String>();
			for(int i=0; i<oneways_array.length; ++i) {
				proxy.pyroOneway.add((String) oneways_array[i]);
			}
		} else if(oneways!=null) {
			@SuppressWarnings("unchecked")
			HashSet<String> oneways_set = (HashSet<String>) oneways;
			proxy.pyroOneway = oneways_set;
		}
		
		if(state[5]!=null) {
			String encodedHmac = (String)state[5];
			if(encodedHmac.startsWith("b64:")) {
				proxy.pyroHmacKey = Base64.getDecoder().decode(encodedHmac.substring(4));
			} else {
				throw new PyroException("hmac encoding error");
			}		
		}
		
		proxy.pyroHandshake = state[6];
		// maxretries (state[7]) is not used/supported in pyrolite, so simply ignore it
		// custom pickler (state[8]) is also not supported

		return proxy;
	}

	public Map<String, Object> convert(Object obj) {
		// note: the state array returned here must conform to the list consumed by Pyro4's Proxy.__setstate_from_dict__ 
		// that means, we make a list with 8 entries:
		// uri, oneway set, methods set, attrs set, timeout, hmac_key, handshake, maxretries  (in this order)
		PyroProxy proxy = (PyroProxy) obj;
		Map<String, Object> dict = new HashMap<String, Object>();
		String uri = String.format("PYRO:%s@%s:%d", proxy.objectid, proxy.hostname, proxy.port);

		String encodedHmac = proxy.pyroHmacKey!=null? "b64:"+Base64.getEncoder().encodeToString(proxy.pyroHmacKey) : null;
		dict.put("state", new Object[]{
			uri,
			proxy.pyroOneway,
			proxy.pyroMethods,
			proxy.pyroAttrs,
			0.0,
			encodedHmac,
			proxy.pyroHandshake,
			0   // maxretries is not used/supported in pyrolite
		});
		dict.put("__class__", "Pyro4.core.Proxy");
		return dict;
	}
}
