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
		// args for array constructor: [ String typecode, ArrayList<Object> values ]
		// or: [ constructor_class, typecode, machinecode_type, byte[] ] 
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
		if(args[1] instanceof String) {
			// python 2.6 encodes the array as a string sequence rather than a list
			// unpickling this is not supported at this time
			throw new PickleException("unsupported Python 2.6 array pickle format");		
		}
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
				return constructCharArrayUTF16(machinecode, data);
			} else {
				// utf-32, 4 bytes
				if (data.length % 4 != 0)
					throw new PickleException("data size alignment error");
				return constructCharArrayUTF32(machinecode, data);
			}
		}
		case 'b':// signed integer 1 -> byte[]
		{
			if (machinecode != 1)
				throw new PickleException("for b type must be 1");
			return data;
		}
		case 'B':// unsigned integer 1 -> short[]
		{
			if (machinecode != 0)
				throw new PickleException("for B type must be 0");
			return constructShortArrayFromUByte(data);
		}
		case 'h':// signed integer 2 -> short[]
		{
			if (machinecode != 4 && machinecode != 5)
				throw new PickleException("for h type must be 4/5");
			if (data.length % 2 != 0)
				throw new PickleException("data size alignment error");
			return constructShortArraySigned(machinecode, data);
		}
		case 'H':// unsigned integer 2 -> int[]
		{
			if (machinecode != 2 && machinecode != 3)
				throw new PickleException("for H type must be 2/3");
			if (data.length % 2 != 0)
				throw new PickleException("data size alignment error");
			return constructIntArrayFromUShort(machinecode, data);
		}
		case 'i':// signed integer 4 -> int[]
		{
			if (machinecode != 8 && machinecode != 9)
				throw new PickleException("for i type must be 8/9");
			if (data.length % 4 != 0)
				throw new PickleException("data size alignment error");
			return constructIntArrayFromInt32(machinecode, data);
		}
		case 'l':// signed integer 4/8 -> int[]
		{
			if (machinecode != 8 && machinecode != 9 && machinecode != 12 && machinecode != 13)
				throw new PickleException("for l type must be 8/9/12/13");
			if ((machinecode==8 || machinecode==9) && (data.length % 4 != 0))
				throw new PickleException("data size alignment error");
			if ((machinecode==12 || machinecode==13) && (data.length % 8 != 0))
				throw new PickleException("data size alignment error");
			if(machinecode==8 || machinecode==9) {
				//32 bits
				return constructIntArrayFromInt32(machinecode, data);
			} else {
				//64 bits
				return constructLongArrayFromInt64(machinecode, data);
			}
		}
		case 'I':// unsigned integer 4 -> long[]
		{
			if (machinecode != 6 && machinecode != 7)
				throw new PickleException("for I type must be 6/7");
			if (data.length % 4 != 0)
				throw new PickleException("data size alignment error");
			return constructLongArrayFromUInt32(machinecode, data);
		}
		case 'L':// unsigned integer 4/8 -> long[]
		{
			if (machinecode != 6 && machinecode != 7 && machinecode != 10 && machinecode != 11)
				throw new PickleException("for L type must be 6/7/10/11");
			if ((machinecode==6 || machinecode==7) && (data.length % 4 != 0))
				throw new PickleException("data size alignment error");
			if ((machinecode==10 || machinecode==11) && (data.length % 8 != 0))
				throw new PickleException("data size alignment error");
			if(machinecode==6 || machinecode==7) {
				// 32 bits
				return constructLongArrayFromUInt32(machinecode, data);
			} else {
				// 64 bits
				return constructLongArrayFromUInt64(machinecode, data);
			}
		}
		case 'f':// floating point 4 -> float[]
		{
			if (machinecode != 14 && machinecode != 15)
				throw new PickleException("for f type must be 14/15");
			if (data.length % 4 != 0)
				throw new PickleException("data size alignment error");
			return constructFloatArray(machinecode, data);
		}
		case 'd':// floating point 8 -> double[]
		{
			if (machinecode != 16 && machinecode != 17)
				throw new PickleException("for d type must be 16/17");
			if (data.length % 8 != 0)
				throw new PickleException("data size alignment error");
			return constructDoubleArray(machinecode, data);
		}
		default:
			throw new PickleException("invalid array typecode: " + typecode);
		}
	}

	protected int[] constructIntArrayFromInt32(int machinecode, byte[] data) {
		int[] result=new int[data.length/4];
		byte[] bigendian=new byte[4];
		for(int i=0; i<data.length/4; i++) {
			if(machinecode==8) {
				result[i]=PickleUtils.bytes_to_integer(data, i*4, 4);
			} else {
				// big endian, swap
				bigendian[0]=data[3+i*4];
				bigendian[1]=data[2+i*4];
				bigendian[2]=data[1+i*4];
				bigendian[3]=data[0+i*4];
				result[i]=PickleUtils.bytes_to_integer(bigendian);
			}
		}
		return result;
	}

	protected long[] constructLongArrayFromUInt32(int machinecode, byte[] data) {
		long[] result=new long[data.length/4];
		byte[] bigendian=new byte[4];
		for(int i=0; i<data.length/4; i++) {
			if(machinecode==6) {
				result[i]=PickleUtils.bytes_to_uint(data, i*4);
			} else {
				// big endian, swap
				bigendian[0]=data[3+i*4];
				bigendian[1]=data[2+i*4];
				bigendian[2]=data[1+i*4];
				bigendian[3]=data[0+i*4];
				result[i]=PickleUtils.bytes_to_uint(bigendian, 0);
			}
		}
		return result;
	}

	protected long[] constructLongArrayFromUInt64(int machinecode, byte[] data) {
		// java doesn't have a ulong (unsigned int 64-bits) datatype
		throw new PickleException("unsupported datatype: 64-bits unsigned long");
	}

	protected long[] constructLongArrayFromInt64(int machinecode, byte[] data) {
		long[] result=new long[data.length/8];
		byte[] bigendian=new byte[8];
		for(int i=0; i<data.length/8; i++) {
			if(machinecode==12) {
				// little endian can go
				result[i]=PickleUtils.bytes_to_long(data, i*8);
			} else {
				// 13=big endian, swap
				bigendian[0]=data[7+i*8];
				bigendian[1]=data[6+i*8];
				bigendian[2]=data[5+i*8];
				bigendian[3]=data[4+i*8];
				bigendian[4]=data[3+i*8];
				bigendian[5]=data[2+i*8];
				bigendian[6]=data[1+i*8];
				bigendian[7]=data[0+i*8];
				result[i]=PickleUtils.bytes_to_long(bigendian, 0);
			}
		}
		return result;
	}	

	protected double[] constructDoubleArray(int machinecode, byte[] data) {
		double[] result = new double[data.length / 8];
		byte[] bigendian=new byte[8];
		for (int i = 0; i < data.length / 8; ++i) {
			if(machinecode == 17) {
				result[i] = PickleUtils.bytes_to_double(data, i * 8);
			} else {
				// 16=little endian, flip the bytes
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

	protected float[] constructFloatArray(int machinecode, byte[] data) {
		float[] result = new float[data.length / 4];
		byte[] bigendian=new byte[4];
		for (int i = 0; i < data.length / 4; ++i) {
			if (machinecode == 15) {
				result[i] = PickleUtils.bytes_to_float(data, i * 4);
			} else {
				// 14=little endian, flip the bytes
				bigendian[0]=data[3+i*4];
				bigendian[1]=data[2+i*4];
				bigendian[2]=data[1+i*4];
				bigendian[3]=data[0+i*4];
				result[i] = PickleUtils.bytes_to_float(bigendian, 0);
			}
		}
		return result;
	}

	protected int[] constructIntArrayFromUShort(int machinecode, byte[] data) {
		int[] result = new int[data.length / 2];
		for(int i=0; i<data.length/2; ++i) {
			int b1=data[0+i*2] & 0xff;
			int b2=data[1+i*2] & 0xff;
			if(machinecode==2) {
				result[i] = (b2<<8) | b1;
			} else {
				// big endian
				result[i] = (b1<<8) | b2;
			}
		}
		return result;
	}

	protected short[] constructShortArraySigned(int machinecode, byte[] data) {
		short[] result = new short[data.length / 2];
		for(int i=0; i<data.length/2; ++i) {
			byte b1=data[0+i*2];
			byte b2=data[1+i*2];
			if(machinecode==4) {
				result[i] = (short) ((b2<<8) | (b1&0xff));
			} else {
				// big endian
				result[i] = (short) ((b1<<8) | (b2&0xff));
			}
		}
		return result;
	}

	protected short[] constructShortArrayFromUByte(byte[] data) {
		short[] result = new short[data.length];
		for(int i=0; i<data.length; ++i) {
			result[i] = (short) (data[i]&0xff);
		}
		return result;
	}
	
	protected char[] constructCharArrayUTF32(int machinecode, byte[] data) {
		char[] result = new char[data.length / 4];
		byte[] bigendian=new byte[4];
		for (int index = 0; index < data.length / 4; ++index) {
			if (machinecode == 20) { 
				int codepoint=PickleUtils.bytes_to_integer(data, index*4, 4);
				char[] cc=Character.toChars(codepoint);
				if(cc.length>1)
					throw new PickleException("cannot process UTF-32 character codepoint "+codepoint);
				result[index] = cc[0];
			}
			else {
				// big endian, swap
				bigendian[0]=data[3+index*4];
				bigendian[1]=data[2+index*4];
				bigendian[2]=data[1+index*4];
				bigendian[3]=data[index*4];
				int codepoint=PickleUtils.bytes_to_integer(bigendian);
				char[] cc=Character.toChars(codepoint);
				if(cc.length>1)
					throw new PickleException("cannot process UTF-32 character codepoint "+codepoint);
				result[index] = cc[0];
			}
		}
		return result;
	}

	protected char[] constructCharArrayUTF16(int machinecode, byte[] data) {
		char[] result = new char[data.length / 2];
		byte[] bigendian=new byte[2];
		for (int index = 0; index < data.length / 2; ++index) {
			if (machinecode == 18) { 
				result[index] = (char) PickleUtils.bytes_to_integer(data, index*2, 2);
			}
			else {
				// big endian, swap
				bigendian[0]=data[1+index*2];
				bigendian[1]=data[0+index*2];
				result[index] = (char) PickleUtils.bytes_to_integer(bigendian);
			}
		}
		return result;
	}
}
