package net.razorvine.examples;

import java.util.*;
import java.io.FileOutputStream;
import java.io.IOException;

import net.razorvine.pickle.Pickler;
import net.razorvine.pickle.Unpickler;


public class PickleExample {

	public static void main(String[] args) throws IOException {

		// going to pickle a c# datastructure
		Map<String, Object> map = new HashMap<String, Object>();
		map.put("apple", 42);
		map.put("microsoft", "hello");
		List<Double> values = new LinkedList<Double>();
		values.add(1.11);
		values.add(2.22);
		values.add(3.33);
		values.add(4.44);
		values.add(5.55);
		map.put("values", values);
		// You can add many other types if you like. See the readme about the type mappings.

		final String pickleFilename = "testpickle.dat";

		System.out.println("Writing pickle to '"+pickleFilename+"'");

		Pickler pickler = new Pickler(true);
		FileOutputStream fos = new FileOutputStream(pickleFilename);
		pickler.dump(map, fos);
		fos.close();

		System.out.println("Done. Try unpickling it in python.\n");

		System.out.println("Reading a pickle created in python...");

		// the following pickle was created in Python 3.4.
		// it is this data:     [1, 2, 3, (11, 12, 13), {'banana', 'grape', 'apple'}]
		byte[] pythonpickle = new byte[]  {(byte)128, 4, (byte)149, 48, 0, 0, 0, 0, 0, 0, 0, 93, (byte)148, 40, 75, 1, 75, 2, 75, 3, 75, 11, 75, 12, 75, 13, (byte)135, (byte)148, (byte)143, (byte)148, 40, (byte)140, 6, 98, 97, 110, 97, 110, 97, (byte)148, (byte)140, 5, 103, 114, 97, 112, 101, (byte)148, (byte)140, 5, 97, 112, 112, 108, 101, (byte)148, (byte)144, 101, 46};
		Unpickler unpickler = new Unpickler();
		Object result = unpickler.loads(pythonpickle);

		System.out.println("type: " + result.getClass());
		List<?> list = (List<?>) result;
		Integer integer1 = (Integer) list.get(0);
		Integer integer2 = (Integer) list.get(1);
		Integer integer3 = (Integer) list.get(2);
		Object[] tuple = (Object[]) list.get(3);
		Set<?> set = (Set<?>) list.get(4);
		System.out.println("1-3: integers: "+integer1+","+integer2+","+integer3);
		System.out.println("4: tuple: ("+tuple[0]+","+tuple[1]+","+tuple[2]+")");
		System.out.println("5: set: "+ set);

		
	}

}
