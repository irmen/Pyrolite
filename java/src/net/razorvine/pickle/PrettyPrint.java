package net.razorvine.pickle;

import java.io.IOException;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.io.UnsupportedEncodingException;
import java.text.DateFormat;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Calendar;
import java.util.Collection;
import java.util.Collections;
import java.util.Locale;
import java.util.Map;
import java.util.Set;

/**
 * Object output pretty printing, to help with the test scripts.
 * Nothing fancy, just a simple readable output format for a handfull of classes.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class PrettyPrint {

	/**
	 * Prettyprint the object to the outputstream.
	 */
	public static void print(Object o, OutputStream out) throws IOException {
		OutputStreamWriter w=null;
		try {	
			w=new OutputStreamWriter(out,"UTF-8");
		} catch (UnsupportedEncodingException e) {
			e.printStackTrace();
		}
		if (o == null) {
			w.write("null object\n");
			w.write("null\n");
			w.flush();
			return;
		}

		Class<?> arraytype = o.getClass().getComponentType();
		if (arraytype != null) {
			w.write("array of " + arraytype+"\n");
			if (!arraytype.isPrimitive()) {
				w.write(Arrays.deepToString((Object[]) o));  w.write("\n");
			} else if (arraytype.equals(Integer.TYPE)) {
				w.write(Arrays.toString((int[]) o));  w.write("\n");
			} else if (arraytype.equals(Double.TYPE)) {
				w.write(Arrays.toString((double[]) o));  w.write("\n");
			} else if (arraytype.equals(Boolean.TYPE)) {
				w.write(Arrays.toString((boolean[]) o));  w.write("\n");
			} else if (arraytype.equals(Short.TYPE)) {
				w.write(Arrays.toString((short[]) o));  w.write("\n");
			} else if (arraytype.equals(Long.TYPE)) {
				w.write(Arrays.toString((long[]) o));  w.write("\n");
			} else if (arraytype.equals(Float.TYPE)) {
				w.write(Arrays.toString((float[]) o));  w.write("\n");
			} else if (arraytype.equals(Character.TYPE)) {
				w.write(Arrays.toString((char[]) o));  w.write("\n");
			} else if (arraytype.equals(Byte.TYPE)) {
				w.write(Arrays.toString((byte[]) o));  w.write("\n");
			} else {
				w.write("?????????\n");
			}
		} else if (o instanceof Set) {
			@SuppressWarnings("unchecked")
			Set<Object> set = (Set<Object>) o;
			ArrayList<String> list=new ArrayList<String>(set.size());
			for(Object obj: set) {
				list.add(obj.toString());
			}
			Collections.sort(list);
			w.write(o.getClass().getName());   w.write("\n");
			w.write(list.toString()); w.write("\n");
		} else if (o instanceof Map) {
			@SuppressWarnings("unchecked")
			Map<Object,Object> map=(Map<Object, Object>) o;
			ArrayList<String> list=new ArrayList<String>(map.size());
			for(Object key: map.keySet()) {
				list.add(key.toString()+"="+map.get(key));
			}
			Collections.sort(list);
			w.write(o.getClass().getName());   w.write("\n");
			w.write(list.toString()); w.write("\n");
		} else if (o instanceof Collection) {
			w.write(o.getClass().getName());   w.write("\n");
			w.write(o.toString());   w.write("\n");
		} else if (o instanceof String) {
			w.write("String\n");
			w.write(o.toString());   w.write("\n");
		} else if (o instanceof java.util.Calendar) {
			w.write("java.util.Calendar\n");
			DateFormat f = DateFormat.getDateTimeInstance(DateFormat.MEDIUM, DateFormat.MEDIUM, Locale.UK);
			Calendar c = (Calendar) o;
			w.write(f.format(c.getTime()) + " millisec=" + c.get(Calendar.MILLISECOND)+"\n");
		} else {
			w.write(o.getClass().getName());   w.write("\n");
			w.write(o.toString());   w.write("\n");
		}
		
		w.flush();
	}
}
