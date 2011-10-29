package net.razorvine.pickle.objects;

import java.util.ArrayList;
import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.PickleUtils;

/**
 * Creates arrays of objects. Returns a primitive type array such as int[] if
 * the objects are ints, etc. Returns an ArrayList<Object> if it needs to
 * contain arbitrary objects (such as lists).
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class ArrayConstructor implements IObjectConstructor {

	public Object construct(Object[] args) throws PickleException {
		// args for array constructor: [ String typecode, ArrayList<Object>
		// values ]
		// or: [ constructor_class, typecode, machinecode_type, byte[] ] (this
		// form is not supported yet)
		if (args.length == 4) {
			ArrayConstructor constructor = (ArrayConstructor) args[0];
			char typecode = ((String) args[1]).charAt(0);
			int machinecodeType = (Integer) args[2];
			byte[] data = (byte[]) args[3];
			return constructor.construct(typecode, machinecodeType, data);
		}
		if (args.length != 2) {
			throw new PickleException("invalid pickle data for array; expected 2 args, got " + args.length);
		}

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

	/**
	 * Create an object based on machine code type
	 */
	public Object construct(char typecode, int machinecode, byte[] data) throws PickleException {
		// Machine format codes.
		// Search for "enum machine_format_code" in Modules/arraymodule.c to get
		// the authoritative values.
		// UNKNOWN_FORMAT = -1
		// UNSIGNED_INT8 = 0
		// SIGNED_INT8 = 1
		// UNSIGNED_INT16_LE = 2
		// UNSIGNED_INT16_BE = 3
		// SIGNED_INT16_LE = 4
		// SIGNED_INT16_BE = 5
		// UNSIGNED_INT32_LE = 6
		// UNSIGNED_INT32_BE = 7
		// SIGNED_INT32_LE = 8
		// SIGNED_INT32_BE = 9
		// UNSIGNED_INT64_LE = 10
		// UNSIGNED_INT64_BE = 11
		// SIGNED_INT64_LE = 12
		// SIGNED_INT64_BE = 13
		// IEEE_754_FLOAT_LE = 14
		// IEEE_754_FLOAT_BE = 15
		// IEEE_754_DOUBLE_LE = 16
		// IEEE_754_DOUBLE_BE = 17
		// UTF16_LE = 18
		// UTF16_BE = 19
		// UTF32_LE = 20
		// UTF32_BE = 21

		if (machinecode < 0)
			throw new PickleException("unknown machine type format");

		switch (typecode) {
		case 'c':// character 1 -> char[]
		case 'u':// Unicode character 2 -> char[]
		{
			if (machinecode != 18 && machinecode != 19 && machinecode != 20 && machinecode != 21)
				throw new PickleException("for c/u type must be 18/19/20/21");
			if (machinecode == 18 || machinecode == 19) {
				// utf-16 , 2 bytes
				if (data.length % 2 != 0)
					throw new PickleException("data size alignment error");
				char[] result = new char[data.length / 2];
				for (int index = 0; index < data.length / 2; ++index) {
					byte b1 = data[index * 2];
					byte b2 = data[index * 2 + 1];
					if (machinecode == 18)
						result[index] = (char) (b1 * 256 + b2);
					else
						result[index] = (char) (b2 * 256 + b1);
				}
				return result;
			} else {
				// utf-32, 4 bytes
				if (data.length % 4 != 0)
					throw new PickleException("data size alignment error");
				char[] result = new char[data.length / 4];
				for (int index = 0; index < data.length / 4; ++index) {
					byte b1 = data[index * 4];
					byte b2 = data[index * 4 + 1];
					byte b3 = data[index * 4 + 2];
					byte b4 = data[index * 4 + 3];
					if (machinecode == 20)
						result[index] = (char) (b1 * 256 * 256 * 256 + b2 * 256 * 256 + b3 * 256 + b4);
					else
						result[index] = (char) (b4 * 256 * 256 * 256 + b3 * 256 * 256 + b2 * 256 + b1);
				}
				return result;
			}
		}
		case 'b':// signed integer 1 -> byte[]
		{
			if (machinecode != 1)
				throw new PickleException("for b type must be 1");
			byte[] result = new byte[data.length];
			for (int i = 0; i < data.length; ++i) {
				result[i] = data[i];
			}
			return result;
		}
		case 'B':// unsigned integer 1 -> short[]
		{
			if (machinecode != 0)
				throw new PickleException("for B type must be 0");
			short[] result = new short[data.length];
			return result;
		}
		case 'h':// signed integer 2 -> short[]
		{
			if (machinecode != 4 && machinecode != 5)
				throw new PickleException("for h type must be 4/5");
			if (data.length % 2 != 0)
				throw new PickleException("data size alignment error");
			short[] result = new short[data.length / 2];
			return result;
		}
		case 'H':// unsigned integer 2 -> int[]
		{
			if (machinecode != 2 && machinecode != 3)
				throw new PickleException("for H type must be 2/3");
			if (data.length % 2 != 0)
				throw new PickleException("data size alignment error");
			int[] result = new int[data.length / 2];
			return result;
		}
		case 'i':// signed integer 2 -> int[]
		{
			if (machinecode != 8 && machinecode != 9)
				throw new PickleException("for i type must be 8/9");
			if (data.length % 2 != 0)
				throw new PickleException("data size alignment error");
			int[] result = new int[data.length / 2];
			return result;
		}
		case 'l':// signed integer 4 -> int[]
		{
			if (machinecode != 8 && machinecode != 9 && machinecode != 12 && machinecode != 13)
				throw new PickleException("for l type must be 8/9/12/13");
			if (data.length % 4 != 0)
				throw new PickleException("data size alignment error");
			int[] result = new int[data.length / 4];
			return result;
		}
		case 'I':// unsigned integer 4 -> long[]
		{
			if (machinecode != 6 && machinecode != 7)
				throw new PickleException("for I type must be 6/7");
			if (data.length % 4 != 0)
				throw new PickleException("data size alignment error");
			long[] result = new long[data.length / 4];
			return result;
		}
		case 'L':// unsigned integer 4 -> long[]
		{
			if (machinecode != 6 && machinecode != 7 && machinecode != 10 && machinecode != 11)
				throw new PickleException("for L type must be 6/7/10/11");
			if (data.length % 4 != 0)
				throw new PickleException("data size alignment error");
			long[] result = new long[data.length / 4];
			return result;
		}
		case 'f':// floating point 4 -> float[]
		{
			if (machinecode != 14 && machinecode != 15)
				throw new PickleException("for f type must be 14/15");
			if (data.length % 4 != 0)
				throw new PickleException("data size alignment error");
			float[] result = new float[data.length / 4];
			byte[] bigendian=new byte[4];
			for (int i = 0; i < data.length / 4; ++i) {
				if (machinecode == 14) {
					result[i] = PickleUtils.bytes_to_float(data, i * 4);
				} else {
					// 15=big endian, flip the bytes
					bigendian[0]=data[3+i*4];
					bigendian[1]=data[2+i*4];
					bigendian[2]=data[1+i*4];
					bigendian[3]=data[0+i*4];
					result[i] = PickleUtils.bytes_to_float(bigendian, 0);
				}
			}
			return result;
		}
		case 'd':// floating point 8 -> double[]
		{
			if (machinecode != 16 && machinecode != 17)
				throw new PickleException("for d type must be 16/17");
			if (data.length % 8 != 0)
				throw new PickleException("data size alignment error");
			double[] result = new double[data.length / 8];
			byte[] bigendian=new byte[8];
			for (int i = 0; i < data.length / 8; ++i) {
				if(machinecode==16) {
					result[i] = PickleUtils.bytes_to_double(data, i * 8);
				} else {
					// 17=big endian, flip the bytes
					bigendian[0]=data[7+i*8];
					bigendian[1]=data[6+i*8];
					bigendian[2]=data[5+i*8];
					bigendian[3]=data[4+i*8];
					bigendian[4]=data[3+i*8];
					bigendian[5]=data[2+i*8];
					bigendian[6]=data[1+i*8];
					bigendian[7]=data[0+i*8];
					result[i] = PickleUtils.bytes_to_double(bigendian, 0);
				}
			}
			return result;
		}
		default:
			throw new PickleException("invalid array typecode: " + typecode);
		}
	}
}
