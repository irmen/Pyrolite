package net.razorvine.pyro.serializer;

import java.io.IOException;
import java.util.HashMap;
import java.util.Map;
import java.util.Scanner;

import net.razorvine.pyro.Config;
import net.razorvine.pyro.PyroException;

/**
 * Abstract base class of all Pyro serializes.
 */
public abstract class PyroSerializer
{
	public abstract int getSerializerId();  // make sure this matches the id from Pyro

	public abstract byte[] serializeCall(String objectId, String method, Object[] vargs, Map<String, Object> kwargs) throws IOException;
	public abstract byte[] serializeData(Object obj) throws IOException;
	public abstract Object deserializeData(byte[] data) throws IOException;

	protected static Map<Config.SerializerType, PyroSerializer> AvailableSerializers = new HashMap<Config.SerializerType, PyroSerializer>();
	
	protected static PickleSerializer pickleSerializer = new PickleSerializer();  // built-in
	protected static SerpentSerializer serpentSerializer;   // loaded if serpent.jar is available
	
	public static PyroSerializer getFor(Config.SerializerType type)
	{
		switch(type)
		{
			case pickle:
				return pickleSerializer;
			case serpent:
			{
				synchronized(PyroSerializer.class)
				{
					if(serpentSerializer==null)
					{
						// try loading it
						try {
							serpentSerializer = new SerpentSerializer();
							final String requiredSerpentVersion = "1.23";
							if(compareLibraryVersions(net.razorvine.serpent.LibraryVersion.VERSION, requiredSerpentVersion) < 0)
							{
								throw new java.lang.RuntimeException("serpent version "+requiredSerpentVersion+" (or newer) is required");
							}
							return serpentSerializer;
						} catch (LinkageError x) {
							throw new PyroException("serpent serializer unavailable", x);
						}
					}
				}
				return serpentSerializer;
			}
			default:
				throw new IllegalArgumentException("unrecognised serializer type: "+type);
		}
	}

	public static PyroSerializer getFor(int serializer_id) {
		if(serpentSerializer!=null) {
			if(serializer_id == serpentSerializer.getSerializerId())
				return serpentSerializer;
		}
		if(serializer_id==pickleSerializer.getSerializerId())
			return pickleSerializer;
		
		throw new IllegalArgumentException("unsupported serializer id: "+serializer_id);
	}

	public static int compareLibraryVersions(String actual, String other) {
		Scanner s1 = new Scanner(actual);
		Scanner s2 = new Scanner(other);
		s1.useDelimiter("\\.");
		s2.useDelimiter("\\.");

		while(s1.hasNextInt() && s2.hasNextInt()) {
		    int v1 = s1.nextInt();
		    int v2 = s2.nextInt();
		    if(v1 < v2) {
		    	s1.close();
		    	s2.close();
		        return -1;
		    } else if(v1 > v2) {
		    	s1.close();
		    	s2.close();
		        return 1;
		    }
		}

		int result = 0;
		if(s1.hasNextInt()) result=1; //str1 has an additional lower-level version number
		s1.close();
		s2.close();
		return result;
	}
}
