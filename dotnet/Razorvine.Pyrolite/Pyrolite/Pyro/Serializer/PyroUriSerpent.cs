using System.Collections;

namespace Razorvine.Pyro.Serializer
{
    public static class PyroUriSerpent
    {
        public static IDictionary ToSerpentDict(object obj)
        {
            PyroURI uri = (PyroURI)obj;
            var dict = new Hashtable
            {
                ["state"] = new object[] {uri.protocol, uri.objectid, null, uri.host, uri.port},
                ["__class__"] = "Pyro5.core.URI"
            };
            return dict;
        }
	
        public static object FromSerpentDict(IDictionary dict)
        {
            var state = (object[])dict["state"];  // protocol, objectid, socketname, hostname, port
            return new PyroURI((string)state[1], (string)state[3], (int)state[4]);
        }        
    }
}