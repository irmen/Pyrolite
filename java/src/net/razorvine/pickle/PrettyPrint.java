package net.razorvine.pickle;

import java.io.IOException;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.io.StringWriter;
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

	/***
	 * Prettyprint into a string, no type header.
	 */
	public static String printToStr(Object o) {
		StringWriter sw=new StringWriter();
		try {
			print(o,sw,false);
		} catch (IOException e) {
			sw.write("<<error>>");
		}
		return sw.toString().trim();
	}
	
	/**
	 * Prettyprint directly to the standard output.
	 */
	public static void print(Object o) throws IOException {
		OutputStreamWriter w=new OutputStreamWriter(System.out,"UTF-8");
		print(o, w, true);
		w.flush();
	}
	
	/**
	 * Prettyprint the object to the outputstream. (UTF-8 output encoding is used)
	 */
	public static void print(Object o, OutputStream outs, boolean typeheader) throws IOException {
		OutputStreamWriter w=new OutputStreamWriter(outs,"UTF-8");
		print(o,w,typeheader);
		w.flush();
	}
	
	/**
	 * Prettyprint the object to the writer.
	 */
	public static void print(Object o, java.io.Writer writer, boolean typeheader) throws IOException {
		if (o == null) {
			if(typeheader) writer.write("null object\n");
			writer.write("null\n");
			writer.flush();
			return;
		}

		Class<?> arraytype = o.getClass().getComponentType();
		if (arraytype != null) {
			if(typeheader) writer.write("array of " + arraytype+"\n");
			if (!arraytype.isPrimitive()) {
				writer.write(Arrays.deepToString((Object[]) o));  writer.write("\n");
			} else if (arraytype.equals(Integer.TYPE)) {
				writer.write(Arrays.toString((int[]) o));  writer.write("\n");
			} else if (arraytype.equals(Double.TYPE)) {
				writer.write(Arrays.toString((double[]) o));  writer.write("\n");
			} else if (arraytype.equals(Boolean.TYPE)) {
				writer.write(Arrays.toString((boolean[]) o));  writer.write("\n");
			} else if (arraytype.equals(Short.TYPE)) {
				writer.write(Arrays.toString((short[]) o));  writer.write("\n");
			} else if (arraytype.equals(Long.TYPE)) {
				writer.write(Arrays.toString((long[]) o));  writer.write("\n");
			} else if (arraytype.equals(Float.TYPE)) {
				writer.write(Arrays.toString((float[]) o));  writer.write("\n");
			} else if (arraytype.equals(Character.TYPE)) {
				writer.write(Arrays.toString((char[]) o));  writer.write("\n");
			} else if (arraytype.equals(Byte.TYPE)) {
				writer.write(Arrays.toString((byte[]) o));  writer.write("\n");
			} else {
				writer.write("?????????\n");
			}
		} else if (o instanceof Set) {
			@SuppressWarnings("unchecked")
			Set<Object> set = (Set<Object>) o;
			ArrayList<String> list=new ArrayList<String>(set.size());
			for(Object obj: set) {
				list.add(obj.toString());
			}
			Collections.sort(list);
			if(typeheader) {
				writer.write(o.getClass().getName());
				writer.write("\n");
			}
			writer.write(list.toString()); writer.write("\n");
		} else if (o instanceof Map) {
			@SuppressWarnings("unchecked")
			Map<Object,Object> map=(Map<Object, Object>) o;
			ArrayList<String> list=new ArrayList<String>(map.size());
			for(Object key: map.keySet()) {
				list.add(key.toString()+"="+map.get(key));
			}
			Collections.sort(list);
			if(typeheader) {
				writer.write(o.getClass().getName());
				writer.write("\n");
			}
			writer.write(list.toString()); writer.write("\n");
		} else if (o instanceof Collection) {
			if(typeheader) {
				writer.write(o.getClass().getName());
				writer.write("\n");
			}
			writer.write(o.toString());   writer.write("\n");
		} else if (o instanceof String) {
			if(typeheader) writer.write("String\n");
			writer.write(o.toString());   writer.write("\n");
		} else if (o instanceof java.util.Calendar) {
			if(typeheader) writer.write("java.util.Calendar\n");
			DateFormat f = DateFormat.getDateTimeInstance(DateFormat.MEDIUM, DateFormat.MEDIUM, Locale.UK);
			Calendar c = (Calendar) o;
			writer.write(f.format(c.getTime()) + " millisec=" + c.get(Calendar.MILLISECOND)+"\n");
		} else {
			if(typeheader) {
				writer.write(o.getClass().getName());
				writer.write("\n");
			}
			writer.write(o.toString());   writer.write("\n");
		}
		
		writer.flush();
	}
}
