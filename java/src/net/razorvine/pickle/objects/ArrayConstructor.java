package net.razorvine.pickle.objects;

import java.util.ArrayList;
import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;

/**
 * Creates arrays of objects. Returns a primitive type array such as int[] if 
 * the objects are ints, etc. Returns an ArrayList<Object> if it needs to contain
 * arbitrary objects (such as lists).
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class ArrayConstructor implements IObjectConstructor {

	public Object construct(Object[] args) throws PickleException {
		// args for array constructor: [ String typecode, ArrayList<Object> values ]
		// or: [ constructor_class, typecode, machinecode_type, byte[] ]  (this form is not supported yet)
		if (args.length==4)
			throw new PickleException("array constructor based on machinetype bytearray is not yet supported"); //@todo implement this
		if (args.length != 2)
			throw new PickleException("invalid pickle data for array; expected 2 args, got "+args.length);

		String typecode = (String) args[0];
		@SuppressWarnings("unchecked")
		ArrayList<Object> values = (ArrayList<Object>) args[1];

		switch (typecode.charAt(0)) {
		case 'c':// character 1 -> char[]
		case 'u':// Unicode character 2 -> char[]
		{
			char[] result = new char[values.size()];
			int i = 0;
			for (Object c : values) {
				result[i++] = ((String) c).charAt(0);
			}
			return result;
		}
		case 'b':// signed integer 1 -> byte[]
		{
			byte[] result = new byte[values.size()];
			int i = 0;
			for (Object c : values) {
				result[i++] = ((Number) c).byteValue();
			}
			return result;
		}
		case 'B':// unsigned integer 1 -> short[]
		case 'h':// signed integer 2 -> short[]
		{
			short[] result = new short[values.size()];
			int i = 0;
			for (Object c : values) {
				result[i++] = ((Number) c).shortValue();
			}
			return result;
		}
		case 'H':// unsigned integer 2 -> int[]
		case 'i':// signed integer 2 -> int[]
		case 'l':// signed integer 4 -> int[]
		{
			int[] result = new int[values.size()];
			int i = 0;
			for (Object c : values) {
				result[i++] = ((Number) c).intValue();
			}
			return result;
		}
		case 'I':// unsigned integer 4 -> long[]
		case 'L':// unsigned integer 4 -> long[]
		{
			long[] result = new long[values.size()];
			int i = 0;
			for (Object c : values) {
				result[i++] = ((Number) c).longValue();
			}
			return result;
		}
		case 'f':// floating point 4 -> float[]
		{
			float[] result = new float[values.size()];
			int i = 0;
			for (Object c : values) {
				result[i++] = ((Number) c).floatValue();
			}
			return result;
		}
		case 'd':// floating point 8 -> double[]
		{
			double[] result = new double[values.size()];
			int i = 0;
			for (Object c : values) {
				result[i++] = ((Number) c).doubleValue();
			}
			return result;
		}
		default:
			throw new PickleException("invalid array typecode: " + typecode);
		}
	}
}
