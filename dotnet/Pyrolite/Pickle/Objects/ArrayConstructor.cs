/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;

namespace Razorvine.Pyrolite.Pickle.Objects
{

/// <summary>
/// Creates arrays of objects. Returns a primitive type array such as int[] if 
/// the objects are ints, etc. 
/// </summary>
class ArrayConstructor : IObjectConstructor {

	public object construct(object[] args) {
		// args for array constructor: [ string typecode, ArrayList values ]
		// or: [ constructor_class, typecode, machinecode_type, byte[] ]  (this form is not supported yet)
		if (args.Length==4)
			throw new PickleException("array constructor based on machinetype bytearray is not yet supported"); //@todo implement this
		if (args.Length != 2)
			throw new PickleException("invalid pickle data for array; expected 2 args, got "+args.Length);

		string typecode = (string) args[0];
		ArrayList values = (ArrayList)args[1];

		switch (typecode[0]) {
		case 'c':// character 1 -> char[]
		case 'u':// Unicode character 2 -> char[]
		{
			char[] result = new char[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = ((string) c)[0];
			}
			return result;
		}
		case 'b':// signed char -> sbyte[]
		{
			sbyte[] result = new sbyte[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToSByte(c);
			}
			return result;
		}
		case 'B':// unsigned char -> byte[]
		{
			byte[] result = new byte[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToByte(c);
			}
			return result;
		}
		case 'h':// signed short -> short[]
		{
			short[] result = new short[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToInt16(c);
			}
			return result;
		}
		case 'H':// unsigned short -> ushort[]
		{
			ushort[] result = new ushort[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToUInt16(c);
			}
			return result;
		}
		case 'i':// signed integer -> int[]
		{
			int[] result = new int[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToInt32(c);
			}
			return result;
		}
		case 'I':// unsigned integer 4 -> uint[]
		{
			uint[] result = new uint[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToUInt32(c);
			}
			return result;
		}
		case 'l':// signed long -> long[]
		{
			long[] result = new long[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToInt64(c);
			}
			return result;
		}
		case 'L':// unsigned long -> ulong[]
		{
			ulong[] result = new ulong[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToUInt64(c);
			}
			return result;
		}
		case 'f':// floating point 4 -> float[]
		{
			float[] result = new float[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToSingle(c);
			}
			return result;
		}
		case 'd':// floating point 8 -> double[]
		{
			double[] result = new double[values.Count];
			int i = 0;
			foreach(var c in values) {
				result[i++] = Convert.ToDouble(c);
			}
			return result;
		}
		default:
			throw new PickleException("invalid array typecode: " + typecode);
		}
	}
}

}
