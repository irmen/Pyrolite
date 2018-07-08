/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Razorvine.Pickle;
using Razorvine.Pickle.Objects;
using Xunit;
// ReSharper disable InconsistentNaming

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local

namespace Pyrolite.Tests.Pickle
{

/// <summary>
/// Unit tests for Unpickling every pickle opcode (protocol 0,1,2,3). 
/// </summary>
public class UnpickleOpcodesTests: IDisposable {

	private readonly Unpickler u;
	private static readonly string STRING256;
	private static readonly string STRING255;
	
	static UnpickleOpcodesTests() {
		StringBuilder sb=new StringBuilder();
		for(int i=0; i<256; ++i) {
			sb.Append((char)i);
		}
		STRING256=sb.ToString();
		STRING255=STRING256.Substring(1);
	}
	
	private object U(string strdata) {
		return u.loads(PickleUtils.str2bytes(strdata));
	}
	
	public UnpickleOpcodesTests() {
		u=new Unpickler();
	}

	public void Dispose() {
		u.close();
	}

	[Fact]
	public void TestStr2Bytes() {
		var bytes=PickleUtils.str2bytes(STRING256);
		for(int i=0; i<256; ++i) {
			int b=bytes[i];
			if(b<0) b+=256;
			Assert.Equal(i, b);  //"byte@"+i
		}
	}
	
	[Fact]
	public void TestNotExisting() {
		Assert.Throws<InvalidOpcodeException>(()=>U("%."));  // non existing opcode '%' should crash
	}

	[Fact]
	public void TestMARK() {
		// MARK           = b'('   # push special markobject on stack
		Assert.Null(U("(((N."));
	}

	[Fact]
	public void TestSTOP() {
		//STOP           = b'.'   # every pickle ends with STOP
		Assert.Throws<ArgumentOutOfRangeException>(()=>U("..")); // a stop without any data on the stack will throw an array exception
	}

	[Fact]
	public void TestPOP() {
		//POP            = b'0'   # discard topmost stack item
		Assert.Null(U("}N."));
		Assert.Equal(new Hashtable(), U("}N0."));
	}

	[Fact]
	public void TestPOPMARK() {
		//POP_MARK       = b'1'   # discard stack top through topmost markobject
		Assert.Equal(2, U("I1\n(I2\n(I3\nI4\n1."));
	}

	[Fact]
	public void TestDUP() {
		//DUP            = b'2'   # duplicate top stack item
		object[] tuple={ 42,42};
		Assert.Equal(tuple, (object[]) U("(I42\n2t."));
	}

	[Fact]
	public void TestFLOAT() {
		//FLOAT          = b'F'   # push float object; decimal string argument
		Assert.Equal(0.0d, U("F0\n."));
		Assert.Equal(0.0d, U("F0.0\n."));
		Assert.Equal(1234.5678d, U("F1234.5678\n."));
		Assert.Equal(-1234.5678d, U("F-1234.5678\n."));
		Assert.Equal(2.345e+202d, U("F2.345e+202\n."));
		Assert.Equal(-2.345e-202d, U("F-2.345e-202\n."));
		
		try {
			U("F1,2\n.");
			Assert.True(false, "expected numberformat exception");
		} catch (FormatException) {
			// ok
		}
	}

	[Fact]
	public void TestINT() {
		//INT            = b'I'   # push integer or bool; decimal string argument
		Assert.Equal(0, U("I0\n."));
		Assert.Equal(0, U("I-0\n."));
		Assert.Equal(1, U("I1\n."));
		Assert.Equal(-1, U("I-1\n."));
		Assert.Equal(1234567890, U("I1234567890\n."));
		Assert.Equal(-1234567890, U("I-1234567890\n."));
		Assert.Equal(1234567890123456L, U("I1234567890123456\n."));
		try {
			U("I123456789012345678901234567890\n.");
			Assert.True(false, "expected overflow exception");
		} catch (OverflowException) {
			// ok
		}
		try {
			U("I123456789@012345678901234567890\n.");
			Assert.True(false, "expected format exception");
		} catch (FormatException) {
			// ok
		}
	}

	[Fact]
	public void TestBININT() {
		//BININT         = b'J'   # push four-byte signed int (little endian)
		Assert.Equal(0, U("J\u0000\u0000\u0000\u0000."));
		Assert.Equal(1, U("J\u0001\u0000\u0000\u0000."));
		Assert.Equal(33554433, U("J\u0001\u0000\u0000\u0002."));
		Assert.Equal(-1, U("J\u00ff\u00ff\u00ff\u00ff."));
		Assert.Equal(-251658255, U("J\u00f1\u00ff\u00ff\u00f0."));
	}

	[Fact]
	public void TestBININT1() {
		//BININT1        = b'K'   # push 1-byte unsigned int
		Assert.Equal(0, U("K\u0000."));
		Assert.Equal(128, U("K\u0080."));
		Assert.Equal(255, U("K\u00ff."));
	}

	[Fact]
	public void TestLONG() {
		//LONG           = b'L'   # push long; decimal string argument
		Assert.Equal(0L, U("L0\n."));
		Assert.Equal(0L, U("L-0\n."));
		Assert.Equal(1L, U("L1\n."));
		Assert.Equal(-1L, U("L-1\n."));
		Assert.Equal(1234567890L, U("L1234567890\n."));
		Assert.Equal(1234567890123456L, U("L1234567890123456\n."));
		Assert.Equal(-1234567890123456L, U("L-1234567890123456\n."));
		//Assert.Equal(new BigInteger("1234567890123456789012345678901234567890"), U("L1234567890123456789012345678901234567890\n."));
		try {
			U("L1234567890123456789012345678901234567890\n.");
			Assert.True(false, "expected pickle exception because c# doesn't have bigint");
		} catch (PickleException) {
			// ok
		}
		try {
			U("I1?0\n.");
			Assert.True(false, "expected numberformat exception");
		} catch (FormatException) {
			// ok
		}
	}

	[Fact]
	public void TestBININT2() {
		//BININT2        = b'M'   # push 2-byte unsigned int (little endian)
		Assert.Equal(0, U("M\u0000\u0000."));
		Assert.Equal(255, U("M\u00ff\u0000."));
		Assert.Equal(32768, U("M\u0000\u0080."));
		Assert.Equal(65535, U("M\u00ff\u00ff."));
	}

	[Fact]
	public void TestNONE() {
		//NONE           = b'N'   # push None
		Assert.Null(U("N."));
	}

	[Fact]
	public void TestPERSIDfail() {
		//PERSID         = b'P'   # push persistent object; id is taken from string arg
		Assert.Throws<PickleException>(() => U("Pbla\n."));
	}

	[Fact]
	public void TestBINPERSIDfail() {
		//BINPERSID      = b'Q'   #  push persistent object; id is taken from stack
		Assert.Throws<PickleException>(() => U("I42\nQ."));
	}

	private class PersistentIdUnpickler : Unpickler
	{
		protected override object persistentLoad(string pid)
		{
			if(pid=="9999")
				return "PersistentObject";
			throw new ArgumentException("unknown persistent_id "+pid);
		}
	}
	
	[Fact]
	public void TestPERSID() {
		//PERSID         = b'P'   # push persistent object; id is taken from string arg
		var pickle = PickleUtils.str2bytes("(lp0\nI42\naP9999\na.");
		Unpickler unpickler = new PersistentIdUnpickler();
		IList result = (IList)unpickler.loads(pickle);
		Assert.Equal(2, result.Count);
		Assert.Equal(42, result[0]);
		Assert.Equal("PersistentObject", result[1]);
	}

	[Fact]
	public void TestBINPERSID() {
		//BINPERSID      = b'Q'   #  push persistent object; id is taken from stack
		var pickle = PickleUtils.str2bytes("\u0080\u0004\u0095\u000f\u0000\u0000\u0000\u0000\u0000\u0000\u0000]\u0094(K*\u008c\u00049999\u0094Qe.");
		Unpickler unpickler = new PersistentIdUnpickler();
		IList result = (IList)unpickler.loads(pickle);
		Assert.Equal(2, result.Count);
		Assert.Equal(42, result[0]);
		Assert.Equal("PersistentObject", result[1]);
	}
	
	[Fact]
	public void TestREDUCE_and_GLOBAL()
	{
		//GLOBAL         = b'c'   # push self.find_class(modname, name); 2 string args
		//REDUCE         = b'R'   # apply callable to argtuple, both on stack
		//"cdecimal\nDecimal\n(V123.456\ntR."
		const decimal dec = 123.456m;
		Assert.Equal(dec, (decimal)U("cdecimal\nDecimal\n(V123.456\ntR."));
	}

	[Fact]
	public void TestSTRING() {
		//STRING         = b'S'   # push string; NL-terminated string argument
		Assert.Equal("", U("S''\n."));
		Assert.Equal("", U("S\"\"\n."));
		Assert.Equal("a", U("S'a'\n."));
		Assert.Equal("a", U("S\"a\"\n."));
		Assert.Equal("'", U("S'\\''\n."));
		Assert.Equal("\u00a1\u00a2\u00a3", U("S'\\xa1\\xa2\\xa3'\n."));
		Assert.Equal("a\\x00y", U("S'a\\\\x00y'\n."));
		
		StringBuilder p=new StringBuilder("S'");
		for(int i=0;i<256;++i) {
			p.Append("\\x");
			p.Append(i.ToString("X2"));
		}
		p.Append("'\n.");
		Assert.Equal(STRING256, U(p.ToString()));
		
		try {
			U("S'bla\n."); // missing quote
			Assert.True(false, "expected pickle exception");
		} catch (PickleException) {
			//ok
		}
	}

	[Fact]
	public void TestBINSTRING() {
		//BINSTRING      = b'T'   # push string; counted binary string argument
		Assert.Equal("", U("T\u0000\u0000\u0000\u0000."));
		Assert.Equal("a", U("T\u0001\u0000\u0000\u0000a."));
		Assert.Equal("\u00a1\u00a2\u00a3", U("T\u0003\u0000\u0000\u0000\u00a1\u00a2\u00a3."));
		Assert.Equal(STRING256,U("T\u0000\u0001\u0000\u0000"+STRING256+"."));
		Assert.Equal(STRING256+STRING256,U("T\u0000\u0002\u0000\u0000"+STRING256+STRING256+"."));
	}

	[Fact]
	public void TestSHORT_BINSTRING() {
		//SHORT_BINSTRING= b'U'   #  push string; counted binary string argument < 256 bytes
		Assert.Equal("", U("U\u0000."));
		Assert.Equal("a", U("U\u0001a."));
		Assert.Equal("\u00a1\u00a2\u00a3", U("U\u0003\u00a1\u00a2\u00a3."));
		Assert.Equal(STRING255,U("U\u00ff"+STRING255+"."));
	}

	[Fact]
	public void TestUNICODE() {
		//UNICODE        = b'V'   # push Unicode string; raw-unicode-escaped'd argument
		Assert.Equal("", U("V\n."));
		Assert.Equal("abc", U("Vabc\n."));
		Assert.Equal("\u20ac", U("V\\u20ac\n."));
		Assert.Equal("a\\u00y", U("Va\\u005cu00y\n."));
		Assert.Equal("\u0080\u00a1\u00a2", U("V\u0080\u00a1\u00a2\n."));
	}

	[Fact]
	public void TestBINUNICODE() {
		//BINUNICODE     = b'X'   # push Unicode string; counted UTF-8 string argument
		Assert.Equal("", U("X\u0000\u0000\u0000\u0000."));
		Assert.Equal("abc", U("X\u0003\u0000\u0000\u0000abc."));
		Assert.Equal("\u20ac", u.loads(new byte[]{Opcodes.BINUNICODE, 0x03,0x00,0x00,0x00,0xe2,0x82,0xac,Opcodes.STOP}));
	}

	[Fact]
	public void TestBINUNICODE8() {
		//BINUNICODE8 = 0x8d;  // push very long string
		Assert.Equal("", U("\u008d\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000."));
		Assert.Equal("abc", U("\u008d\u0003\u0000\u0000\u0000\u0000\u0000\u0000\u0000abc."));
		Assert.Equal("\u20ac", u.loads(new byte[]{Opcodes.BINUNICODE8, 0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xe2,0x82,0xac,Opcodes.STOP}));
	}

	[Fact]
	public void TestSHORTBINUNICODE() {
		//SHORT_BINUNICODE = 0x8c;  // push short string; UTF-8 length < 256 bytes
		Assert.Equal("", U("\u008c\u0000."));
		Assert.Equal("abc", U("\u008c\u0003abc."));
		Assert.Equal("\u20ac", u.loads(new byte[]{Opcodes.SHORT_BINUNICODE, 0x03,0xe2,0x82,0xac,Opcodes.STOP}));

		try {
			u.loads(new byte[]{Opcodes.SHORT_BINUNICODE, 0x00, 0x00, Opcodes.STOP});
			Assert.True(false, "expected error");
		} catch (PickleException) {
			// ok
		}
	}

	[Fact]
	public void TestAPPEND() {
		//APPEND         = b'a'   # append stack top to list below it
		ArrayList list = new ArrayList {42, 43};
		Assert.Equal(list, U("]I42\naI43\na."));
	}


	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
	private class ThingyWithSetstate {
		public string a;
		public ThingyWithSetstate(string param) {
			a=param;
		}
		public void __setstate__(Hashtable values) {
			a=(string)values["a"];
		}
	}
	
	private class ThingyConstructor : IObjectConstructor {

		public object construct(object[] args) {
			return new ThingyWithSetstate((string)args[0]);
		}
	}

	[Fact]
	public void TestBUILD() {
		//BUILD          = b'b'   # call __setstate__ or __dict__.update()
		Unpickler.registerConstructor("unittest", "Thingy", new ThingyConstructor());
		// create a thing with initial value for the field 'a',
		// the use BUILD to __setstate__() it with something else ('foo').
		ThingyWithSetstate thingy = (ThingyWithSetstate) U("cunittest\nThingy\n(V123\ntR}S'a'\nS'foo'\nsb.");
		Assert.Equal("foo",thingy.a);
	}

	[Fact]
	public void TestDICT() {
		//DICT           = b'd'   # build a dict from stack items
		var dict = new Hashtable
		{
			["a"] = 42,
			["b"] = 99
		};
		Assert.Equal(dict, U("(S'a'\nI42\nS'b'\nI99\nd."));
	}

	[Fact]
	public void TestEMPTY_DICT() {
		//EMPTY_DICT     = b'}'   # push empty dict
		Assert.Equal(new Hashtable(), U("}."));
	}

	[Fact]
	public void TestAPPENDS() {
		//APPENDS        = b'e'   # extend list on stack by topmost stack slice
		ArrayList list = new ArrayList {42, 43};
		Assert.Equal(list, U("](I42\nI43\ne."));
	}

	[Fact]
	public void TestGET_and_PUT() {
		//GET            = b'g'   # push item from memo on stack; index is string arg
		//PUT            = b'p'   # store stack top in memo; index is string arg
		
		// a list with three times the same string in it.
		// the string is stored ('p') and retrieved from the memo ('g').
		var list=new List<string>();
		const string str = "abc";
		list.Add(str);
		list.Add(str);
		list.Add(str);
		Assert.Equal(list, U("(lp0\nS'abc'\np1\nag1\nag1\na."));

		try {
			U("(lp0\nS'abc'\np1\nag2\nag2\na."); // invalid memo key
			Assert.True(false, "expected pickle exception");
		} catch (PickleException) {
			// ok
		}
	}

	[Fact]
	public void TestBINGET_and_BINPUT() {
		//BINGET         = b'h'   # push item from memo on stack; index is 1-byte arg
		//BINPUT         = b'q'   # store stack top in memo; index is 1-byte arg
		// a list with three times the same string in it.
		// the string is stored ('p') and retrieved from the memo ('g').
		var list=new List<string>();
		const string str = "abc";
		list.Add(str);
		list.Add(str);
		list.Add(str);
		Assert.Equal(list, U("]q\u0000(U\u0003abcq\u0001h\u0001h\u0001e."));

		try {
			U("]q\u0000(U\u0003abcq\u0001h\u0002h\u0002e."); // invalid memo key
			Assert.True(false, "expected pickle exception");
		} catch (PickleException) {
			// ok
		}
	}

	[Fact]
	public void TestINST() {
		//INST           = b'i'   # build & push class instance
		ClassDict result = (ClassDict) U("(i__main__\nThing\n(dS'value'\nI32\nsb.");
		Assert.Equal("__main__.Thing", result.ClassName);
		Assert.Equal(32, result["value"]);
	}

	[Fact]
	public void TestLONG_BINGET_and_LONG_BINPUT() {
		//LONG_BINGET    = b'j'   # push item from memo on stack; index is 4-byte arg
		//LONG_BINPUT    = b'r'   # store stack top in memo; index is 4-byte arg
		// a list with three times the same string in it.
		// the string is stored ('p') and retrieved from the memo ('g').
		var list=new List<string>();
		const string str = "abc";
		list.Add(str);
		list.Add(str);
		list.Add(str);
		Assert.Equal(list, U("]r\u0000\u0000\u0000\u0000(U\u0003abcr\u0001\u0002\u0003\u0004j\u0001\u0002\u0003\u0004j\u0001\u0002\u0003\u0004e."));

		try {
			// invalid memo key
			U("]r\u0000\u0000\u0000\u0000(U\u0003abcr\u0001\u0002\u0003\u0004j\u0001\u0005\u0005\u0005j\u0001\u0005\u0005\u0005e.");
			Assert.True(false, "expected pickle exception");
		} catch (PickleException) {
			// ok
		}
	}

	[Fact]
	public void TestLIST() {
		//LIST           = b'l'   # build list from topmost stack items
		var list = new List<int> {1, 2};
		Assert.Equal(list, U("(I1\nI2\nl."));
	}

	[Fact]
	public void TestEMPTY_LIST() {
		//EMPTY_LIST     = b']'   # push empty list
		Assert.Equal(new ArrayList(), U("]."));
	}

	[Fact]
	public void TestOBJ() {
		//OBJ            = b'o'   # build & push class instance
		ClassDict result = (ClassDict) U("\u0080\u0002(c__main__\nThing\no}U\u0005valueK sb.");
		Assert.Equal("__main__.Thing", result.ClassName);
		Assert.Equal(32, result["value"]);
	}

	[Fact]
	public void TestSETITEM() {
		//SETITEM        = b's'   # add key+value pair to dict
		var dict = new Hashtable
		{
			["a"] = 42,
			["b"] = 43
		};
		Assert.Equal(dict, U("}S'a'\nI42\nsS'b'\nI43\ns."));
	}

	[Fact]
	public void TestTUPLE() {
		//TUPLE          = b't'   # build tuple from topmost stack items
		object[] tuple={1,2};
		Assert.Equal(tuple, (object[]) U("(I1\nI2\nt."));
	}

	[Fact]
	public void TestEMPTY_TUPLE() {
		//EMPTY_TUPLE    = b')'   # push empty tuple
		Assert.Equal(new object[0], (object[]) U(")."));
	}

	[Fact]
	public void TestEMPTY_SET() {
		//EMPTY_SET = 0x8f;  // push empty set on the stack
		var value = new HashSet<object>();
		Assert.Equal(value, u.loads(new byte[]{ 0x8f, Opcodes.STOP}));
	}

	[Fact]
	public void TestFROZENSET() {
		//FROZENSET = 0x91;  // build frozenset from topmost stack items
		var value = new HashSet<object>();
		Assert.Equal(value, u.loads(new[]{ Opcodes.MARK, Opcodes.FROZENSET, Opcodes.STOP}));
		value.Add(42);
		value.Add("a");
		Assert.Equal(value, u.loads(new byte[]{ Opcodes.MARK, Opcodes.BININT1, 42, Opcodes.SHORT_BINUNICODE, 1, 97, Opcodes.FROZENSET, Opcodes.STOP}));
	}

	[Fact]
	public void TestADDITEMS() {
		//ADDITEMS = 0x90;  // modify set by adding topmost stack items
		var value = new HashSet<object>();
		Assert.Equal(value, u.loads(new []{ Opcodes.EMPTY_SET, Opcodes.MARK, Opcodes.ADDITEMS, Opcodes.STOP}));
		value.Add(42);
		value.Add("a");
		Assert.Equal(value, u.loads(new byte[]{ Opcodes.EMPTY_SET, Opcodes.MARK, Opcodes.BININT1, 42, Opcodes.SHORT_BINUNICODE, 1, 97, Opcodes.ADDITEMS, Opcodes.STOP}));
	}

	[Fact]
	public void TestSETITEMS() {
		//SETITEMS       = b'u'   # modify dict by adding topmost key+value pairs
		var dict = new Hashtable
		{
			["b"] = 43,
			["c"] = 44
		};
		Assert.Equal(dict, U("}(S'b'\nI43\nS'c'\nI44\nu."));

		dict.Clear();
		dict["a"]=42;
		dict["b"]=43;
		dict["c"]=44;
		Assert.Equal(dict, U("}S'a'\nI42\ns(S'b'\nI43\nS'c'\nI44\nu."));
	}

	[Fact]
	public void TestBINFLOAT() {
		//BINFLOAT       = b'G'   # push float; arg is 8-byte float encoding
		Assert.Equal(2.345e123, u.loads(new byte[]{Opcodes.BINFLOAT, 0x59,0x8c,0x60,0xfb,0x80,0xae,0x2f,0xbb, Opcodes.STOP}));
		Assert.Equal(1.172419264827552e+123, u.loads(new byte[]{Opcodes.BINFLOAT, 0x59,0x7c,0x60,0x7b,0x70,0x7e,0x2f,0x7b, Opcodes.STOP}));
		Assert.Equal(double.PositiveInfinity, u.loads(new byte[]{Opcodes.BINFLOAT, 0x7f,0xf0,0,0,0,0,0,0, Opcodes.STOP}));
		Assert.Equal(double.NegativeInfinity, u.loads(new byte[]{Opcodes.BINFLOAT, 0xff,0xf0,0,0,0,0,0,0, Opcodes.STOP}));
		Assert.Equal(double.NaN, u.loads(new byte[]{Opcodes.BINFLOAT, 0xff,0xf8,0,0,0,0,0,0, Opcodes.STOP}));
	}
	

	[Fact]
	public void TestTRUE() {
		//TRUE           = b'I01\n'  # not an opcode; see INT docs in pickletools.py
		Assert.True((bool) U("I01\n."));
	}

	[Fact]
	public void TestFALSE() {
		//FALSE          = b'I00\n'  # not an opcode; see INT docs in pickletools.py
		Assert.False((bool) U("I00\n."));
	}

	[Fact]
	public void TestPROTO() {
		//PROTO          = b'\x80'  # identify pickle protocol
		U("\u0080\u0000N.");
		U("\u0080\u0001N.");
		U("\u0080\u0002N.");
		U("\u0080\u0003N.");
		U("\u0080\u0004N.");
		try {
			U("\u0080\u0005N."); // unsupported protocol 5.
			Assert.True(false, "expected pickle exception");
		} catch (PickleException) {
			// ok
		}
	}

	[Fact]
	public void TestNEWOBJ()
	{
		//NEWOBJ         = b'\x81'  # build object by applying cls.__new__ to argtuple
		//GLOBAL         = b'c'   # push self.find_class(modname, name); 2 string args
		//"cdecimal\nDecimal\n(V123.456\nt\x81."
		const decimal dec = 123.456m;
		Assert.Equal(dec, (decimal)U("cdecimal\nDecimal\n(V123.456\nt\u0081."));
	}
	
	[Fact]
	public void TestNEWOBJ_EX() {
		//NEWOBJ_EX = 0x92;  // like NEWOBJ but work with keyword only arguments
		const decimal dec = 123.456m;
		Assert.Equal(dec, (decimal)U("cdecimal\nDecimal\n(V123.456\nt}\u0092."));
		
		try {
			Assert.Equal(dec, (decimal)U("cdecimal\nDecimal\n(V123.456\nt}\u008c\u0004testK1s\u0092."));
			Assert.True(false, "expected exception");
		} catch (PickleException x) {
			Assert.Equal("newobj_ex with keyword arguments not supported", x.Message);
		}
	}

	
	[Fact]
	public void TestEXT1() {
		//EXT1           = b'\x82'  # push object from extension registry; 1-byte index
		Assert.Throws<PickleException>(()=>U("\u0082\u0001.")); // not implemented
	}

	[Fact]
	public void TestEXT2() {
		//EXT2           = b'\x83'  # ditto, but 2-byte index
		Assert.Throws<PickleException>(()=>U("\u0083\u0001\u0002.")); // not implemented
	}

	[Fact]
	public void TestEXT4() {
		//EXT4           = b'\x84'  # ditto, but 4-byte index
		Assert.Throws<PickleException>(()=>U("\u0084\u0001\u0002\u0003\u0004.")); // not implemented
	}

	[Fact]
	public void TestTUPLE1() {
		//TUPLE1         = b'\x85'  # build 1-tuple from stack top
		object[] tuple={ 42 };
		Assert.Equal(tuple, (object[]) U("I41\nI42\n\u0085."));
	}

	[Fact]
	public void TestTUPLE2() {
		//TUPLE2         = b'\x86'  # build 2-tuple from two topmost stack items
		object[] tuple={ 42, 43 };
		Assert.Equal(tuple, (object[]) U("I41\nI42\nI43\n\u0086."));
	}

	[Fact]
	public void TestTUPLE3() {
		//TUPLE3         = b'\x87'  # build 3-tuple from three topmost stack items
		object[] tuple={ 42, 43, 44 };
		Assert.Equal(tuple, (object[]) U("I41\nI42\nI43\nI44\n\u0087."));
	}

	[Fact]
	public void TestNEWTRUE() {
		//NEWTRUE        = b'\x88'  # push True
		Assert.True((bool) U("\u0088."));
	}

	[Fact]
	public void TestNEWFALSE() {
		//NEWFALSE       = b'\x89'  # push False
		Assert.False((bool) U("\u0089."));
	}

	[Fact]
	public void TestLONG1() {
		//LONG1          = b'\x8a'  # push long from < 256 bytes
		Assert.Equal(0L, U("\u008a\u0000."));
		Assert.Equal(0L, U("\u008a\u0001\u0000."));
		Assert.Equal(1L, U("\u008a\u0001\u0001."));
		Assert.Equal(-1L, U("\u008a\u0001\u00ff."));
		Assert.Equal(0L, U("\u008a\u0002\u0000\u0000."));
		Assert.Equal(1L, U("\u008a\u0002\u0001\u0000."));
		Assert.Equal(513L, U("\u008a\u0002\u0001\u0002."));
		Assert.Equal(-256L, U("\u008a\u0002\u0000\u00ff."));
		Assert.Equal(65280L, U("\u008a\u0003\u0000\u00ff\u0000."));

		Assert.Equal(0x12345678L, U("\u008a\u0004\u0078\u0056\u0034\u0012."));
		Assert.Equal(-231451016L, U("\u008a\u0004\u0078\u0056\u0034\u00f2."));
		Assert.Equal(0xf2345678L, U("\u008a\u0005\u0078\u0056\u0034\u00f2\u0000."));

		Assert.Equal(0x0102030405060708L, u.loads(new byte[] {Opcodes.LONG1,0x08,0x08,0x07,0x06,0x05,0x04,0x03,0x02,0x01,Opcodes.STOP}));
		//BigInteger big=new BigInteger("010203040506070809",16);
		try {
			u.loads(new byte[] {Opcodes.LONG1,0x09,0x09,0x08,0x07,0x06,0x05,0x04,0x03,0x02,0x01,Opcodes.STOP});
			Assert.True(false, "expected PickleException due to number overflow");
		} catch (PickleException) {
			// ok
		}
	}

	[Fact]
	public void TestLONG4() {
		//LONG4          = b'\x8b'  # push really big long
		Assert.Equal(0L, u.loads(new byte[] {Opcodes.LONG4, 0x00, 0x00, 0x00, 0x00, Opcodes.STOP}));
		Assert.Equal(0L, u.loads(new byte[] {Opcodes.LONG4, 0x01, 0x00, 0x00, 0x00, 0x00, Opcodes.STOP}));
		Assert.Equal(1L, u.loads(new byte[] {Opcodes.LONG4, 0x01, 0x00, 0x00, 0x00, 0x01, Opcodes.STOP}));
		Assert.Equal(-1L, u.loads(new byte[] {Opcodes.LONG4, 0x01, 0x00, 0x00, 0x00, 0xff, Opcodes.STOP}));
		Assert.Equal(0L, u.loads(new byte[] {Opcodes.LONG4, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, Opcodes.STOP}));
		Assert.Equal(1L, u.loads(new byte[] {Opcodes.LONG4, 0x02, 0x00, 0x00, 0x00, 0x01, 0x00, Opcodes.STOP}));
		Assert.Equal(513L, u.loads(new byte[] {Opcodes.LONG4, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02, Opcodes.STOP}));
		Assert.Equal(-256L, u.loads(new byte[] {Opcodes.LONG4, 0x02, 0x00, 0x00, 0x00, 0x00, 0xff, Opcodes.STOP}));
		Assert.Equal(65280L, u.loads(new byte[] {Opcodes.LONG4, 0x03, 0x00, 0x00, 0x00, 0x00, 0xff, 0x00, Opcodes.STOP}));

		Assert.Equal(0x12345678L, U("\u008b\u0004\u0000\u0000\u0000\u0078\u0056\u0034\u0012."));
		Assert.Equal(-231451016L, U("\u008b\u0004\u0000\u0000\u0000\u0078\u0056\u0034\u00f2."));
		Assert.Equal(0xf2345678L, U("\u008b\u0005\u0000\u0000\u0000\u0078\u0056\u0034\u00f2\u0000."));

		Assert.Equal(0x0102030405060708L, u.loads(new byte[] {Opcodes.LONG4,0x08, 0x00, 0x00, 0x00,0x08,0x07,0x06,0x05,0x04,0x03,0x02,0x01,Opcodes.STOP}));
		//BigInteger big=new BigInteger("010203040506070809",16);
		try {
			u.loads(new byte[] {Opcodes.LONG4,0x09, 0x00, 0x00, 0x00,0x09,0x08,0x07,0x06,0x05,0x04,0x03,0x02,0x01,Opcodes.STOP});
			Assert.True(false, "expected PickleException due to number overflow");
		} catch (PickleException) {
			// ok
		}
	}

	[Fact]
	public void TestBINBYTES() {
		//BINBYTES       = b'B'   # push bytes; counted binary string argument
		var bytes = new byte[]{};
		Assert.Equal(bytes, (byte[]) U("B\u0000\u0000\u0000\u0000."));
		bytes=new[]{(byte)'a'};
		Assert.Equal(bytes, (byte[]) U("B\u0001\u0000\u0000\u0000a."));
		bytes=new[]{(byte)0xa1, (byte)0xa2, (byte)0xa3};
		Assert.Equal(bytes, (byte[]) U("B\u0003\u0000\u0000\u0000\u00a1\u00a2\u00a3."));
		bytes=new byte[512];
		for(int i=1; i<512; ++i) {
			bytes[i]=(byte)(i&0xff);
		}
		Assert.Equal(bytes, (byte[]) U("B\u0000\u0002\u0000\u0000"+STRING256+STRING256+"."));
	}


	[Fact]
	public void TestBINBYTES8() {
		//BINBYTES8 = 0x8e;  // push very long bytes string
		var bytes = new byte[]{};
		Assert.Equal(bytes, (byte[]) U("\u008e\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000."));
		bytes=new[]{(byte)'a'};
		Assert.Equal(bytes, (byte[]) U("\u008e\u0001\u0000\u0000\u0000\u0000\u0000\u0000\u0000a."));
		bytes=new[]{(byte)0xa1, (byte)0xa2, (byte)0xa3};
		Assert.Equal(bytes, (byte[]) U("\u008e\u0003\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u00a1\u00a2\u00a3."));
		bytes=new byte[512];
		for(int i=1; i<512; ++i) {
			bytes[i]=(byte)(i&0xff);
		}
		Assert.Equal(bytes, (byte[]) U("\u008e\u0000\u0002\u0000\u0000\u0000\u0000\u0000\u0000"+STRING256+STRING256+"."));
	}

	[Fact]
	public void TestSHORT_BINBYTES() {
		//SHORT_BINBYTES = b'C'   #  push bytes; counted binary string argument < 256 bytes
		var bytes = new byte[]{};
		Assert.Equal(bytes, (byte[]) U("C\u0000."));
		bytes=new[]{(byte)'a'};
		Assert.Equal(bytes, (byte[]) U("C\u0001a."));
		bytes=new[]{(byte)0xa1, (byte)0xa2, (byte)0xa3};
		Assert.Equal(bytes, (byte[]) U("C\u0003\u00a1\u00a2\u00a3."));
		bytes=new byte[255];
		for(int i=1; i<256; ++i) {
			bytes[i-1]=(byte)i;
		}
		Assert.Equal(bytes, (byte[]) U("C\u00ff"+STRING255+"."));
	}
	
	[Fact]
	public void TestMEMOIZE() {
		// MEMOIZE = 0x94;  // store top of the stack in memo
		var value = new object[] {1,2,2};
		var result = (object[]) U("K\u0001\u0094K\u0002\u0094h\u0000h\u0001h\u0001\u0087.");
		Assert.Equal(value, result);
	}
	
	[Fact]
	public void TestFRAME() {
		// FRAME = 0x95;  // indicate the beginning of a new frame
		var result = u.loads(new byte[] { Opcodes.FRAME, 6,0,0,0,0,0,0,0, 
		                     	Opcodes.BININT1, 42, Opcodes.BININT1, 43, Opcodes.BININT1, 44,
		                     	Opcodes.FRAME, 2,0,0,0,0,0,0,0, Opcodes.TUPLE3, Opcodes.STOP});
		var value = new object[] {42,43,44};
		Assert.Equal(value, result);
	}

	[Fact]
	public void TestGLOBAL() {
		//GLOBAL = (byte)'c'; // push self.find_class(modname, name); 2 string args
		var result = U("cdatetime\ntime\n.");
		Assert.IsType<DateTimeConstructor>(result);
		result = U("cbuiltins\nbytearray\n.");
		Assert.IsType<ByteArrayConstructor>(result);
	}

	[Fact]
	public void TestSTACK_GLOBAL() {
		//STACK_GLOBAL = 0x93;  // same as GLOBAL but using names on the stacks
		var result = U("\u008c\u0008datetime\u008c\u0004time\u0093.");
		Assert.IsType<DateTimeConstructor>(result);
		result = U("\u008c\u0008builtins\u008c\u0009bytearray\u0093.");
		Assert.IsType<ByteArrayConstructor>(result);
	}
}

}
