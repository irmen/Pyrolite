package net.razorvine.pickle.test;

import static org.junit.Assert.*;

import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.PickleUtils;
import net.razorvine.pickle.Pickler;
import net.razorvine.pickle.Unpickler;
import net.razorvine.pyro.PyroProxy;
import net.razorvine.pyro.PyroURI;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

/**
 * Unit tests for some more complex unpickler objects (PyroProxy).
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class UnpicklerComplexTests {

	Object U(String strdata) throws PickleException, IOException
	{
		return U(PickleUtils.str2bytes(strdata));	
	}
	Object U(byte[] data) throws PickleException, IOException
	{
		Unpickler u=new Unpickler();
		Object o=u.loads(data);
		u.close();
		return o;		
	}
	
	@Before
	public void setUp() throws Exception {
	}

	@After
	public void tearDown() throws Exception {
	}
	
	
	@Test
	public void testPickleUnpickleURI() throws IOException {
		PyroURI uri=new PyroURI("PYRO:test@localhost:9999");
		Pickler p=new Pickler();
		byte[] pickled_uri=p.dumps(uri);
		PyroURI uri2=(PyroURI) U(pickled_uri);
		assertEquals(uri,uri2);

		uri=new PyroURI();
		pickled_uri=p.dumps(uri);
		uri2=(PyroURI) U(pickled_uri);
		assertEquals(uri,uri2);
	}

	@Test
	public void testPickleUnpickleProxy() throws IOException {
		PyroProxy proxy=new PyroProxy("hostname",9999,"objectid");
		Pickler p=new Pickler();
		byte[] pickled_proxy=p.dumps(proxy);
		Object result=U(pickled_proxy);
		assertTrue(result instanceof HashMap); // proxy objects cannot be properly pickled and are pickled as bean, hence HashMap
	}

	@Test
	public void testUnpickleRealProxy() throws IOException {
		byte[] pickled_proxy=new byte[]
				{-128, 2, 99, 80, 121, 114, 111, 52, 46, 99, 111, 114, 101, 10, 80, 114, 111, 120, 121, 10, 113,
				 0, 41, -127, 113, 1, 40, 99, 80, 121, 114, 111, 52, 46, 99, 111, 114, 101, 10, 85, 82, 73, 10,
				 113, 2, 41, -127, 113, 3, 40, 85, 4, 80, 89, 82, 79, 113, 4, 85, 10, 115, 111, 109, 101, 111,
				 98, 106, 101, 99, 116, 113, 5, 78, 85, 9, 108, 111, 99, 97, 108, 104, 111, 115, 116, 113, 6,
				 77, 15, 39, 116, 113, 7, 98, 99, 95, 95, 98, 117, 105, 108, 116, 105, 110, 95, 95, 10, 115,
				 101, 116, 10, 113, 8, 93, 113, 9, -123, 113, 10, 82, 113, 11, 99, 80, 121, 114, 111, 52, 46,
				 117, 116, 105, 108, 10, 83, 101, 114, 105, 97, 108, 105, 122, 101, 114, 10, 113, 12, 41, -127,
				 113, 13, 125, 113, 14, 98, 71, 0, 0, 0, 0, 0, 0, 0, 0, 116, 113, 15, 98, 46};
		
		PyroProxy.RegisterPickleConstructors();
		
		PyroProxy proxy=(PyroProxy)U(pickled_proxy);
		assertEquals("someobject",proxy.objectid);
		assertEquals("localhost",proxy.hostname);
		assertEquals(9999,proxy.port);
	}
	
	@Test(expected=PickleException.class)
	public void testUnpickleUnsupportedClass() throws IOException {
		byte[] pickled = new byte[] {
				(byte)128, 2, 99, 95, 95, 109, 97, 105, 110, 95, 95, 10, 67, 117, 115, 116, 111, 109, 67, 108,
				97, 115, 115, 10, 113, 0, 41, (byte)129, 113, 1, 125, 113, 2, 40, 85, 3, 97, 103, 101, 113, 3,
				75, 34, 85, 6, 118, 97, 108, 117, 101, 115, 113, 4, 93, 113, 5, 40, 75, 1, 75, 2, 75, 3,
				101, 85, 4, 110, 97, 109, 101, 113, 6, 85, 5, 72, 97, 114, 114, 121, 113, 7, 117, 98, 46};
		Object o = U(pickled);
	}

	
	public class CustomClazz {
		public String name;
		public int age;
		public ArrayList values;
		public CustomClazz() 
		{
			
		}
		public CustomClazz(String name, int age, ArrayList values)
		{
			this.name=name;
			this.age=age;
			this.values=values;
		}
		
		/**
		 * called by the Unpickler to restore state.
		 */
		public void __setstate__(HashMap args) {
			this.name = (String) args.get("name");
			this.age = (Integer) args.get("age");
			this.values = (ArrayList) args.get("values");
		}			
	}
	
	class CustomClazzConstructor implements IObjectConstructor
	{
		public Object construct(Object[] args) throws PickleException
		{
			if(args.length==0)
			{
				return new CustomClazz();    // default constructor
			}
			else if(args.length==3)
			{
				String name = (String)args[0];
				int age = (Integer) args[1];
				ArrayList values = (ArrayList) args[2];
				return new CustomClazz(name, age, values);
			}
			else throw new PickleException("expected 0 or 3 constructor arguments");
		}
	}

	@Test
	public void testUnpickleCustomClass() throws IOException {
		byte[] pickled = new byte[] {
				(byte)128, 2, 99, 95, 95, 109, 97, 105, 110, 95, 95, 10, 67, 117, 115, 116, 111, 109, 67, 108,
				97, 122, 122, 10, 113, 0, 41, (byte)129, 113, 1, 125, 113, 2, 40, 85, 3, 97, 103, 101, 113, 3,
				75, 34, 85, 6, 118, 97, 108, 117, 101, 115, 113, 4, 93, 113, 5, 40, 75, 1, 75, 2, 75, 3,
				101, 85, 4, 110, 97, 109, 101, 113, 6, 85, 5, 72, 97, 114, 114, 121, 113, 7, 117, 98, 46};
		
		Unpickler.registerConstructor("__main__","CustomClazz", new CustomClazzConstructor());
		CustomClazz o = (CustomClazz) U(pickled);
		assertEquals("Harry" ,o.name);
		assertEquals(34 ,o.age);
		ArrayList expected = new ArrayList() {{
			add(1);
			add(2);
			add(3);
		}};
		assertEquals(expected, o.values);
	}
}
