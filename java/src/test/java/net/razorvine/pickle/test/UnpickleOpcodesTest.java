package net.razorvine.pickle.test;

import static org.junit.Assert.*;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.InvalidOpcodeException;
import net.razorvine.pickle.Opcodes;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.PickleUtils;
import net.razorvine.pickle.Unpickler;
import net.razorvine.pickle.objects.ByteArrayConstructor;
import net.razorvine.pickle.objects.DateTimeConstructor;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

/**
 * Unit tests for Unpickling every pickle opcode (all protocols).
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class UnpickleOpcodesTest {

	Unpickler u;
	static String STRING256;
	static String STRING255;
	
	static {
		StringBuilder sb=new StringBuilder();
		for(int i=0; i<256; ++i) {
			sb.append((char)i);
		}
		STRING256=sb.toString();
		STRING255=STRING256.substring(1);
	}
	
	Object U(String strdata) throws PickleException, IOException {
		return u.loads(PickleUtils.str2bytes(strdata));
	}
	
	@Before
	public void setUp() throws Exception {
		u=new Unpickler();
	}

	@After
	public void tearDown() throws Exception {
		u.close();
	}

	@Test
	public void testStr2Bytes() throws IOException {
		byte[] bytes=PickleUtils.str2bytes(STRING256);
		for(int i=0; i<256; ++i) {
			int b=bytes[i];
			if(b<0) b+=256;
			assertEquals("byte@"+i, i, b);
		}
	}
	
	@Test(expected=InvalidOpcodeException.class)
	public void testNotExisting() throws PickleException, IOException {
		U("%.");  // non existing opcode '%' should crash
	}

	@Test
	public void testMARK() throws PickleException, IOException {
		// MARK           = b'('   # push special markobject on stack
		assertNull(U("(((N."));
	}

	@Test(expected=ArrayIndexOutOfBoundsException.class)
	public void testSTOP() throws PickleException, IOException {
		//STOP           = b'.'   # every pickle ends with STOP
		U(".."); // a stop without any data on the stack will throw an array exception
	}

	@Test
	public void testPOP() throws PickleException, IOException {
		//POP            = b'0'   # discard topmost stack item
		assertNull(U("}N."));
		assertEquals(Collections.EMPTY_MAP, U("}N0."));
	}

	@Test
	public void testPOPMARK() throws PickleException, IOException {
		//POP_MARK       = b'1'   # discard stack top through topmost markobject
		assertEquals(2, U("I1\n(I2\n(I3\nI4\n1."));
	}

	@Test
	public void testDUP() throws PickleException, IOException {
		//DUP            = b'2'   # duplicate top stack item
		Object[] tuple=new Object[] { 42,42};
		assertArrayEquals(tuple, (Object[]) U("(I42\n2t."));
	}

	@Test
	public void testFLOAT() throws PickleException, IOException {
		//FLOAT          = b'F'   # push float object; decimal string argument
		assertEquals(0.0d, U("F0\n."));
		assertEquals(0.0d, U("F0.0\n."));
		assertEquals(1234.5678d, U("F1234.5678\n."));
		assertEquals(-1234.5678d, U("F-1234.5678\n."));
		assertEquals(2.345e+202d, U("F2.345e+202\n."));
		assertEquals(-2.345e-202d, U("F-2.345e-202\n."));
		try {
			U("F1,2\n.");
			fail("expected numberformat exception");
		} catch (NumberFormatException x) {
			// ok
		}
	}

	@Test
	public void testINT() throws PickleException, IOException {
		//INT            = b'I'   # push integer or bool; decimal string argument
		assertEquals(0, U("I0\n."));
		assertEquals(0, U("I-0\n."));
		assertEquals(1, U("I1\n."));
		assertEquals(-1, U("I-1\n."));
		assertEquals(1234567890, U("I1234567890\n."));
		assertEquals(-1234567890, U("I-1234567890\n."));
		assertEquals(1234567890123456L, U("I1234567890123456\n."));
		try {
			U("I123456789012345678901234567890\n.");
			fail("expected numberformat exception");
		} catch (NumberFormatException x) {
			// ok
		}
	}

	@Test
	public void testBININT() throws PickleException, IOException {
		//BININT         = b'J'   # push four-byte signed int (little endian)
		assertEquals(0, U("J\u0000\u0000\u0000\u0000."));
		assertEquals(1, U("J\u0001\u0000\u0000\u0000."));
		assertEquals(33554433, U("J\u0001\u0000\u0000\u0002."));
		assertEquals(-1, U("J\u00ff\u00ff\u00ff\u00ff."));
		assertEquals(-251658255, U("J\u00f1\u00ff\u00ff\u00f0."));
	}

	@Test
	public void testBININT1() throws PickleException, IOException {
		//BININT1        = b'K'   # push 1-byte unsigned int
		assertEquals(0, U("K\u0000."));
		assertEquals(128, U("K\u0080."));
		assertEquals(255, U("K\u00ff."));
	}

	@Test
	public void testLONG() throws PickleException, IOException {
		//LONG           = b'L'   # push long; decimal string argument
		assertEquals(0L, U("L0\n."));
		assertEquals(0L, U("L-0\n."));
		assertEquals(1L, U("L1\n."));
		assertEquals(-1L, U("L-1\n."));
		assertEquals(1234567890L, U("L1234567890\n."));
		assertEquals(1234567890123456L, U("L1234567890123456\n."));
		assertEquals(-1234567890123456L, U("L-1234567890123456\n."));
		assertEquals(new BigInteger("1234567890123456789012345678901234567890"), U("L1234567890123456789012345678901234567890\n."));
		try {
			U("I1?0\n.");
			fail("expected numberformat exception");
		} catch (NumberFormatException x) {
			// ok
		}
	}

	@Test
	public void testBININT2() throws PickleException, IOException {
		//BININT2        = b'M'   # push 2-byte unsigned int (little endian)
		assertEquals(0, U("M\u0000\u0000."));
		assertEquals(255, U("M\u00ff\u0000."));
		assertEquals(32768, U("M\u0000\u0080."));
		assertEquals(65535, U("M\u00ff\u00ff."));
	}

	@Test
	public void testNONE() throws PickleException, IOException {
		//NONE           = b'N'   # push None
		assertEquals(null, U("N."));
	}

	@Test(expected=InvalidOpcodeException.class)
	public void testPERSID() throws PickleException, IOException {
		//PERSID         = b'P'   # push persistent object; id is taken from string arg
		U("Pbla\n."); // this opcode is not implemented and should raise an exception
	}

	@Test(expected=InvalidOpcodeException.class)
	public void testBINPERSID() throws PickleException, IOException {
		//BINPERSID      = b'Q'   #  "       "         "  ;  "  "   "     "  stack
		U("I42\nQ."); // this opcode is not implemented and should raise an exception
	}

	@Test
	public void testREDUCE_and_GLOBAL() throws PickleException, IOException {
		//GLOBAL         = b'c'   # push self.find_class(modname, name); 2 string args
		//REDUCE         = b'R'   # apply callable to argtuple, both on stack
		//"cdecimal\nDecimal\n(V123.456\ntR."
		BigDecimal dec=new BigDecimal("123.456");
		assertEquals(dec, (BigDecimal)U("cdecimal\nDecimal\n(V123.456\ntR."));
	}

	@Test
	public void testSTRING() throws PickleException, IOException {
		//STRING         = b'S'   # push string; NL-terminated string argument
		assertEquals("", U("S''\n."));
		assertEquals("", U("S\"\"\n."));
		assertEquals("a", U("S'a'\n."));
		assertEquals("a", U("S\"a\"\n."));
		assertEquals("\u00a1\u00a2\u00a3", U("S'\\xa1\\xa2\\xa3'\n."));
		assertEquals("a\\x00y", U("S'a\\\\x00y'\n."));
		
		StringBuilder p=new StringBuilder("S'");
		for(int i=0;i<256;++i) {
			p.append("\\x");
			if(i<16) p.append("0");
			p.append(Integer.toHexString(i));
		}
		p.append("'\n.");
		assertEquals(STRING256, U(p.toString()));
		
		try {
			U("S'bla\n."); // missing quote
			fail("expected pickle exception");
		} catch (PickleException x) {
			//ok
		}
	}

	@Test
	public void testBINSTRING() throws PickleException, IOException {
		//BINSTRING      = b'T'   # push string; counted binary string argument
		assertEquals("", U("T\u0000\u0000\u0000\u0000."));
		assertEquals("a", U("T\u0001\u0000\u0000\u0000a."));
		assertEquals("\u00a1\u00a2\u00a3", U("T\u0003\u0000\u0000\u0000\u00a1\u00a2\u00a3."));
		assertEquals(STRING256,U("T\u0000\u0001\u0000\u0000"+STRING256+"."));
		assertEquals(STRING256+STRING256,U("T\u0000\u0002\u0000\u0000"+STRING256+STRING256+"."));
	}

	@Test
	public void testSHORT_BINSTRING() throws PickleException, IOException {
		//SHORT_BINSTRING= b'U'   #  push string; counted binary string argument < 256 bytes
		assertEquals("", U("U\u0000."));
		assertEquals("a", U("U\u0001a."));
		assertEquals("\u00a1\u00a2\u00a3", U("U\u0003\u00a1\u00a2\u00a3."));
		assertEquals(STRING255,U("U\u00ff"+STRING255+"."));
	}

	@Test
	public void testUNICODE() throws PickleException, IOException {
		//UNICODE        = b'V'   # push Unicode string; raw-unicode-escaped'd argument
		assertEquals("", U("V\n."));
		assertEquals("abc", U("Vabc\n."));
		assertEquals("\u20ac", U("V\\u20ac\n."));
		assertEquals("a\\u00y", U("Va\\u005cu00y\n."));
		assertEquals("\u0080\u00a1\u00a2", U("V\u0080\u00a1\u00a2\n."));
	}

	@Test
	public void testBINUNICODE() throws PickleException, IOException {
		//BINUNICODE     = b'X'   # push Unicode string; counted UTF-8 string argument
		assertEquals("", U("X\u0000\u0000\u0000\u0000."));
		assertEquals("abc", U("X\u0003\u0000\u0000\u0000abc."));
		assertEquals("\u20ac", u.loads(new byte[]{Opcodes.BINUNICODE, 0x03,0x00,0x00,0x00,(byte)0xe2,(byte)0x82,(byte)0xac,Opcodes.STOP}));
	}

	@Test
	public void testBINUNICODE8() throws IOException {
		//BINUNICODE8 = 0x8d;  // push very long string
		assertEquals("", U("\u008d\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000."));
		assertEquals("abc", U("\u008d\u0003\u0000\u0000\u0000\u0000\u0000\u0000\u0000abc."));
		assertEquals("\u20ac", u.loads(new byte[]{(byte)Opcodes.BINUNICODE8, 0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,(byte)0xe2,(byte)0x82,(byte)0xac,Opcodes.STOP}));
	}

	@Test
	public void testSHORTBINUNICODE() throws IOException {
		//SHORT_BINUNICODE = 0x8c;  // push short string; UTF-8 length < 256 bytes
		assertEquals("", U("\u008c\u0000."));
		assertEquals("abc", U("\u008c\u0003abc."));
		assertEquals("\u20ac", u.loads(new byte[]{(byte)Opcodes.SHORT_BINUNICODE, 0x03,(byte)0xe2,(byte)0x82,(byte)0xac,Opcodes.STOP}));

		try {
			u.loads(new byte[]{(byte)Opcodes.SHORT_BINUNICODE, 0x00, 0x00, Opcodes.STOP});
			fail("expected error");
		} catch (PickleException x) {
			// ok
		}
	}
	
	@Test
	public void testAPPEND() throws PickleException, IOException {
		//APPEND         = b'a'   # append stack top to list below it
		List<Integer> list=new ArrayList<Integer>();
		list.add(42);
		list.add(43);
		assertEquals(list, U("]I42\naI43\na."));
	}

	
	public class ThingyWithSetstate {
		public String a;
		public ThingyWithSetstate(String param) {
			a=param;
		}
		public void __setstate__(HashMap<String, Object> values) {
			a=(String)values.get("a");
		}
	}
	class ThingyConstructor implements IObjectConstructor {

		public Object construct(Object[] args) throws PickleException {
			return new ThingyWithSetstate((String)args[0]);
		}
	}

	@Test
	public void testBUILD() throws PickleException, IOException {
		//BUILD          = b'b'   # call __setstate__ or __dict__.update()
		Unpickler.registerConstructor("unittest", "Thingy", new ThingyConstructor());
		// create a thing with initial value for the field 'a',
		// the use BUILD to __setstate__() it with something else ('foo').
		ThingyWithSetstate thingy = (ThingyWithSetstate) U("cunittest\nThingy\n(V123\ntR}S'a'\nS'foo'\nsb.");
		assertEquals("foo",thingy.a);
	}

	@Test
	public void testDICT() throws PickleException, IOException {
		//DICT           = b'd'   # build a dict from stack items
		Map<String, Integer> dict=new HashMap<String, Integer>();
		dict.put("a", 42);
		dict.put("b", 99);
		assertEquals(dict, U("(S'a'\nI42\nS'b'\nI99\nd."));
	}

	@Test
	public void testEMPTY_DICT() throws PickleException, IOException {
		//EMPTY_DICT     = b'}'   # push empty dict
		assertEquals(Collections.EMPTY_MAP, U("}."));
	}

	@Test
	public void testAPPENDS() throws PickleException, IOException {
		//APPENDS        = b'e'   # extend list on stack by topmost stack slice
		List<Integer> list=new ArrayList<Integer>();
		list.add(42);
		list.add(43);
		assertEquals(list, U("](I42\nI43\ne."));
	}

	@Test
	public void testGET_and_PUT() throws PickleException, IOException {
		//GET            = b'g'   # push item from memo on stack; index is string arg
		//PUT            = b'p'   # store stack top in memo; index is string arg
		
		// a list with three times the same string in it.
		// the string is stored ('p') and retrieved from the memo ('g').
		List<String> list=new ArrayList<String>();
		String str="abc";
		list.add(str);
		list.add(str);
		list.add(str);
		assertEquals(list, U("(lp0\nS'abc'\np1\nag1\nag1\na."));

		try {
			U("(lp0\nS'abc'\np1\nag2\nag2\na."); // invalid memo key
			fail("expected pickle exception");
		} catch (PickleException x) {
			// ok
		}
	}

	@Test
	public void testBINGET_and_BINPUT() throws PickleException, IOException {
		//BINGET         = b'h'   # push item from memo on stack; index is 1-byte arg
		//BINPUT         = b'q'   # store stack top in memo; index is 1-byte arg
		// a list with three times the same string in it.
		// the string is stored ('p') and retrieved from the memo ('g').
		List<String> list=new ArrayList<String>();
		String str="abc";
		list.add(str);
		list.add(str);
		list.add(str);
		assertEquals(list, U("]q\u0000(U\u0003abcq\u0001h\u0001h\u0001e."));

		try {
			U("]q\u0000(U\u0003abcq\u0001h\u0002h\u0002e."); // invalid memo key
			fail("expected pickle exception");
		} catch (PickleException x) {
			// ok
		}
	}

	@Test(expected=InvalidOpcodeException.class)
	public void testINST() throws PickleException, IOException {
		//INST           = b'i'   # build & push class instance
		U("i.");
	}

	@Test
	public void testLONG_BINGET_and_LONG_BINPUT() throws PickleException, IOException {
		//LONG_BINGET    = b'j'   # push item from memo on stack; index is 4-byte arg
		//LONG_BINPUT    = b'r'   # store stack top in memo; index is 4-byte arg
		// a list with three times the same string in it.
		// the string is stored ('p') and retrieved from the memo ('g').
		List<String> list=new ArrayList<String>();
		String str="abc";
		list.add(str);
		list.add(str);
		list.add(str);
		assertEquals(list, U("]r\u0000\u0000\u0000\u0000(U\u0003abcr\u0001\u0002\u0003\u0004j\u0001\u0002\u0003\u0004j\u0001\u0002\u0003\u0004e."));

		try {
			// invalid memo key
			U("]r\u0000\u0000\u0000\u0000(U\u0003abcr\u0001\u0002\u0003\u0004j\u0001\u0005\u0005\u0005j\u0001\u0005\u0005\u0005e.");
			fail("expected pickle exception");
		} catch (PickleException x) {
			// ok
		}
	}

	@Test
	public void testLIST() throws PickleException, IOException {
		//LIST           = b'l'   # build list from topmost stack items
		List<Integer> list=new ArrayList<Integer>();
		list.add(1);
		list.add(2);
		assertEquals(list, U("(I1\nI2\nl."));
	}

	@Test
	public void testEMPTY_LIST() throws PickleException, IOException {
		//EMPTY_LIST     = b']'   # push empty list
		assertEquals(Collections.EMPTY_LIST, U("]."));
	}

	@Test(expected=InvalidOpcodeException.class)
	public void testOBJ() throws PickleException, IOException {
		//OBJ            = b'o'   # build & push class instance
		U("o.");
	}

	@Test
	public void testSETITEM() throws PickleException, IOException {
		//SETITEM        = b's'   # add key+value pair to dict
		Map<String, Integer> dict=new HashMap<String, Integer>();
		dict.put("a", 42);
		dict.put("b", 43);
		assertEquals(dict, U("}S'a'\nI42\nsS'b'\nI43\ns."));
	}

	@Test
	public void testTUPLE() throws PickleException, IOException {
		//TUPLE          = b't'   # build tuple from topmost stack items
		Object[] tuple=new Object[] {1,2};
		assertArrayEquals(tuple, (Object[]) U("(I1\nI2\nt."));
	}

	@Test
	public void testEMPTY_TUPLE() throws PickleException, IOException {
		//EMPTY_TUPLE    = b')'   # push empty tuple
		assertArrayEquals(new Object[0], (Object[]) U(")."));
	}

	@Test
	public void testEMPTY_SET() throws PickleException, IOException {
		//EMPTY_SET = 0x8f;  // push empty set on the stack
		HashSet<Object> value = new HashSet<Object>();
		assertEquals(value, u.loads(new byte[]{ (byte)0x8f, Opcodes.STOP}));
	}

	@Test
	public void testFROZENSET() throws PickleException, IOException {
		//FROZENSET = 0x91;  // build frozenset from topmost stack items
		HashSet<Object> value = new HashSet<Object>();
		assertEquals(value, u.loads(new byte[]{ Opcodes.MARK, (byte)Opcodes.FROZENSET, Opcodes.STOP}));
		value.add(42);
		value.add("a");
		assertEquals(value, u.loads(new byte[]{ Opcodes.MARK, Opcodes.BININT1, 42, (byte)Opcodes.SHORT_BINUNICODE, 1, 97, (byte)Opcodes.FROZENSET, Opcodes.STOP}));
	}

	@Test
	public void testADDITEMS() throws PickleException, IOException {
		//ADDITEMS = 0x90;  // modify set by adding topmost stack items
		HashSet<Object> value = new HashSet<Object>();
		assertEquals(value, u.loads(new byte[]{ (byte)Opcodes.EMPTY_SET, Opcodes.MARK, (byte)Opcodes.ADDITEMS, Opcodes.STOP}));
		value.add(42);
		value.add("a");
		assertEquals(value, u.loads(new byte[]{ (byte)Opcodes.EMPTY_SET, Opcodes.MARK, Opcodes.BININT1, 42, (byte)Opcodes.SHORT_BINUNICODE, 1, 97, (byte)Opcodes.ADDITEMS, Opcodes.STOP}));
	}

	@Test
	public void testSETITEMS() throws PickleException, IOException {
		//SETITEMS       = b'u'   # modify dict by adding topmost key+value pairs
		Map<String, Integer> dict=new HashMap<String, Integer>();
		dict.put("b", 43);
		dict.put("c", 44);
		assertEquals(dict, U("}(S'b'\nI43\nS'c'\nI44\nu."));

		dict.clear();
		dict.put("a", 42);
		dict.put("b", 43);
		dict.put("c", 44);
		assertEquals(dict, U("}S'a'\nI42\ns(S'b'\nI43\nS'c'\nI44\nu."));
	}

	@Test
	public void testBINFLOAT() throws PickleException, IOException {
		//BINFLOAT       = b'G'   # push float; arg is 8-byte float encoding
		assertEquals(2.345e123, u.loads(new byte[]{Opcodes.BINFLOAT, 0x59,(byte)0x8c,0x60,(byte)0xfb,(byte)0x80,(byte)0xae,0x2f,(byte)0xbb, Opcodes.STOP}));
		assertEquals(1.172419264827552e+123, u.loads(new byte[]{Opcodes.BINFLOAT, 0x59,0x7c,0x60,0x7b,0x70,0x7e,0x2f,0x7b, Opcodes.STOP}));
		assertEquals(Double.POSITIVE_INFINITY, u.loads(new byte[]{Opcodes.BINFLOAT, (byte)0x7f,(byte)0xf0,0,0,0,0,0,0, Opcodes.STOP}));
		assertEquals(Double.NEGATIVE_INFINITY, u.loads(new byte[]{Opcodes.BINFLOAT, (byte)0xff,(byte)0xf0,0,0,0,0,0,0, Opcodes.STOP}));
		assertEquals(Double.NaN, u.loads(new byte[]{Opcodes.BINFLOAT, (byte)0xff,(byte)0xf8,0,0,0,0,0,0, Opcodes.STOP}));
	}
	

	@Test
	public void testTRUE() throws PickleException, IOException {
		//TRUE           = b'I01\n'  # not an opcode; see INT docs in pickletools.py
		assertTrue((Boolean) U("I01\n."));
	}

	@Test
	public void testFALSE() throws PickleException, IOException {
		//FALSE          = b'I00\n'  # not an opcode; see INT docs in pickletools.py
		assertFalse((Boolean) U("I00\n."));
	}

	@Test
	public void testPROTO() throws PickleException, IOException {
		//PROTO          = b'\x80'  # identify pickle protocol
		U("\u0080\u0000N.");
		U("\u0080\u0001N.");
		U("\u0080\u0002N.");
		U("\u0080\u0003N.");
		U("\u0080\u0004N.");
		try {
			U("\u0080\u0005N."); // unsupported protocol 5.
			fail("expected pickle exception");
		} catch (PickleException x) {
			// ok
		}
	}

	@Test
	public void testNEWOBJ() throws PickleException, IOException {
		//NEWOBJ         = b'\x81'  # build object by applying cls.__new__ to argtuple
		//GLOBAL         = b'c'   # push self.find_class(modname, name); 2 string args
		//"cdecimal\nDecimal\n(V123.456\nt\x81."
		BigDecimal dec=new BigDecimal("123.456");
		assertEquals(dec, (BigDecimal)U("cdecimal\nDecimal\n(V123.456\nt\u0081."));
	}

	@Test
	public void testNEWOBJ_EX() throws PickleException, IOException {
		//NEWOBJ_EX = 0x92;  // like NEWOBJ but work with keyword only arguments
		BigDecimal dec=new BigDecimal("123.456");
		assertEquals(dec, (BigDecimal)U("cdecimal\nDecimal\n(V123.456\nt}\u0092."));
		
		try {
			assertEquals(dec, (BigDecimal)U("cdecimal\nDecimal\n(V123.456\nt}\u008c\u0004testK1s\u0092."));
			fail("expected exception");
		} catch (PickleException x) {
			assertEquals("newobj_ex with keyword arguments not supported", x.getMessage());
		}
	}

	@Test(expected=InvalidOpcodeException.class)
	public void testEXT1() throws PickleException, IOException {
		//EXT1           = b'\x82'  # push object from extension registry; 1-byte index
		U("\u0082\u0001."); // not implemented
	}

	@Test(expected=InvalidOpcodeException.class)
	public void testEXT2() throws PickleException, IOException {
		//EXT2           = b'\x83'  # ditto, but 2-byte index
		U("\u0083\u0001\u0002."); // not implemented
	}

	@Test(expected=InvalidOpcodeException.class)
	public void testEXT4() throws PickleException, IOException {
		//EXT4           = b'\x84'  # ditto, but 4-byte index
		U("\u0084\u0001\u0002\u0003\u0004."); // not implemented
	}

	@Test
	public void testTUPLE1() throws PickleException, IOException {
		//TUPLE1         = b'\x85'  # build 1-tuple from stack top
		Object[] tuple=new Object[] { 42 };
		assertArrayEquals(tuple, (Object[]) U("I41\nI42\n\u0085."));
	}

	@Test
	public void testTUPLE2() throws PickleException, IOException {
		//TUPLE2         = b'\x86'  # build 2-tuple from two topmost stack items
		Object[] tuple=new Object[] { 42, 43 };
		assertArrayEquals(tuple, (Object[]) U("I41\nI42\nI43\n\u0086."));
	}

	@Test
	public void testTUPLE3() throws PickleException, IOException {
		//TUPLE3         = b'\x87'  # build 3-tuple from three topmost stack items
		Object[] tuple=new Object[] { 42, 43, 44 };
		assertArrayEquals(tuple, (Object[]) U("I41\nI42\nI43\nI44\n\u0087."));
	}

	@Test
	public void testNEWTRUE() throws PickleException, IOException {
		//NEWTRUE        = b'\x88'  # push True
		assertTrue((Boolean) U("\u0088."));
	}

	@Test
	public void testNEWFALSE() throws PickleException, IOException {
		//NEWFALSE       = b'\x89'  # push False
		assertFalse((Boolean) U("\u0089."));
	}

	@Test
	public void testLONG1() throws PickleException, IOException {
		//LONG1          = b'\x8a'  # push long from < 256 bytes
		assertEquals(0L, U("\u008a\u0000."));
		assertEquals(0L, U("\u008a\u0001\u0000."));
		assertEquals(1L, U("\u008a\u0001\u0001."));
		assertEquals(-1L, U("\u008a\u0001\u00ff."));
		assertEquals(0L, U("\u008a\u0002\u0000\u0000."));
		assertEquals(1L, U("\u008a\u0002\u0001\u0000."));
		assertEquals(513L, U("\u008a\u0002\u0001\u0002."));
		assertEquals(-256L, U("\u008a\u0002\u0000\u00ff."));
		assertEquals(65280L, U("\u008a\u0003\u0000\u00ff\u0000."));
		
		assertEquals(0x12345678L, U("\u008a\u0004\u0078\u0056\u0034\u0012."));
		assertEquals(-231451016L, U("\u008a\u0004\u0078\u0056\u0034\u00f2."));
		assertEquals(0xf2345678L, U("\u008a\u0005\u0078\u0056\u0034\u00f2\u0000."));

		assertEquals(0x0102030405060708L, u.loads(new byte[] {(byte)Opcodes.LONG1,0x08,0x08,0x07,0x06,0x05,0x04,0x03,0x02,0x01,Opcodes.STOP}));
		BigInteger big=new BigInteger("010203040506070809",16);
		assertEquals(big, u.loads(new byte[] {(byte)Opcodes.LONG1,0x09,0x09,0x08,0x07,0x06,0x05,0x04,0x03,0x02,0x01,Opcodes.STOP}));
	}

	@Test
	public void testLONG4() throws PickleException, IOException {
		//LONG4          = b'\x8b'  # push really big long
		assertEquals(0L, u.loads(new byte[] {(byte)Opcodes.LONG4, 0x00, 0x00, 0x00, 0x00, Opcodes.STOP}));
		assertEquals(0L, u.loads(new byte[] {(byte)Opcodes.LONG4, 0x01, 0x00, 0x00, 0x00, 0x00, Opcodes.STOP}));
		assertEquals(1L, u.loads(new byte[] {(byte)Opcodes.LONG4, 0x01, 0x00, 0x00, 0x00, 0x01, Opcodes.STOP}));
		assertEquals(-1L, u.loads(new byte[] {(byte)Opcodes.LONG4, 0x01, 0x00, 0x00, 0x00, (byte)0xff, Opcodes.STOP}));
		assertEquals(0L, u.loads(new byte[] {(byte)Opcodes.LONG4, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, Opcodes.STOP}));
		assertEquals(1L, u.loads(new byte[] {(byte)Opcodes.LONG4, 0x02, 0x00, 0x00, 0x00, 0x01, 0x00, Opcodes.STOP}));
		assertEquals(513L, u.loads(new byte[] {(byte)Opcodes.LONG4, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02, Opcodes.STOP}));
		assertEquals(-256L, u.loads(new byte[] {(byte)Opcodes.LONG4, 0x02, 0x00, 0x00, 0x00, 0x00, (byte)0xff, Opcodes.STOP}));
		assertEquals(65280L, u.loads(new byte[] {(byte)Opcodes.LONG4, 0x03, 0x00, 0x00, 0x00, 0x00, (byte)0xff, 0x00, Opcodes.STOP}));

		assertEquals(0x12345678L, U("\u008b\u0004\u0000\u0000\u0000\u0078\u0056\u0034\u0012."));
		assertEquals(-231451016L, U("\u008b\u0004\u0000\u0000\u0000\u0078\u0056\u0034\u00f2."));
		assertEquals(0xf2345678L, U("\u008b\u0005\u0000\u0000\u0000\u0078\u0056\u0034\u00f2\u0000."));

		assertEquals(0x0102030405060708L, u.loads(new byte[] {(byte)Opcodes.LONG4,0x08, 0x00, 0x00, 0x00,0x08,0x07,0x06,0x05,0x04,0x03,0x02,0x01,Opcodes.STOP}));
		BigInteger big=new BigInteger("010203040506070809",16);
		assertEquals(big, u.loads(new byte[] {(byte)Opcodes.LONG4,0x09, 0x00, 0x00, 0x00,0x09,0x08,0x07,0x06,0x05,0x04,0x03,0x02,0x01,Opcodes.STOP}));
	}

	@Test
	public void testBINBYTES() throws PickleException, IOException {
		//BINBYTES       = b'B'   # push bytes; counted binary string argument
		byte[] bytes;
		bytes=new byte[]{};
		assertArrayEquals(bytes, (byte[]) U("B\u0000\u0000\u0000\u0000."));
		bytes=new byte[]{'a'};
		assertArrayEquals(bytes, (byte[]) U("B\u0001\u0000\u0000\u0000a."));
		bytes=new byte[]{(byte)0xa1, (byte)0xa2, (byte)0xa3};
		assertArrayEquals(bytes, (byte[]) U("B\u0003\u0000\u0000\u0000\u00a1\u00a2\u00a3."));
		bytes=new byte[512];
		for(int i=1; i<512; ++i) {
			bytes[i]=(byte)i;
		}
		assertArrayEquals(bytes, (byte[]) U("B\u0000\u0002\u0000\u0000"+STRING256+STRING256+"."));
	}

	@Test
	public void testBINBYTES8() throws PickleException, IOException {
		//BINBYTES8 = 0x8e;  // push very long bytes string
		byte[] bytes;
		bytes=new byte[]{};
		assertArrayEquals(bytes, (byte[]) U("\u008e\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000."));
		bytes=new byte[]{(byte)'a'};
		assertArrayEquals(bytes, (byte[]) U("\u008e\u0001\u0000\u0000\u0000\u0000\u0000\u0000\u0000a."));
		bytes=new byte[]{(byte)0xa1, (byte)0xa2, (byte)0xa3};
		assertArrayEquals(bytes, (byte[]) U("\u008e\u0003\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u00a1\u00a2\u00a3."));
		bytes=new byte[512];
		for(int i=1; i<512; ++i) {
			bytes[i]=(byte)(i&0xff);
		}
		assertArrayEquals(bytes, (byte[]) U("\u008e\u0000\u0002\u0000\u0000\u0000\u0000\u0000\u0000"+STRING256+STRING256+"."));
	}
	
	@Test
	public void testSHORT_BINBYTES() throws PickleException, IOException {
		//SHORT_BINBYTES = b'C'   #  push bytes; counted binary string argument < 256 bytes
		byte[] bytes;
		bytes=new byte[]{};
		assertArrayEquals(bytes, (byte[]) U("C\u0000."));
		bytes=new byte[]{'a'};
		assertArrayEquals(bytes, (byte[]) U("C\u0001a."));
		bytes=new byte[]{(byte)0xa1, (byte)0xa2, (byte)0xa3};
		assertArrayEquals(bytes, (byte[]) U("C\u0003\u00a1\u00a2\u00a3."));
		bytes=new byte[255];
		for(int i=1; i<256; ++i) {
			bytes[i-1]=(byte)i;
		}
		assertArrayEquals(bytes, (byte[]) U("C\u00ff"+STRING255+"."));
	}

	@Test
	public void testMEMOIZE() throws PickleException, IOException {
		// MEMOIZE = 0x94;  // store top of the stack in memo
		Object[] value = new Object[] {1,2,2};
		Object[] result = (Object[]) U("K\u0001\u0094K\u0002\u0094h\u0000h\u0001h\u0001\u0087.");
		assertArrayEquals(value, result);
	}
	
	@Test
	public void testFRAME() throws PickleException, IOException {
		// FRAME = 0x95;  // indicate the beginning of a new frame
		Object[] result = (Object[]) u.loads(new byte[] { (byte)Opcodes.FRAME, 6,0,0,0,0,0,0,0, 
		                     	Opcodes.BININT1, 42, Opcodes.BININT1, 43, Opcodes.BININT1, 44,
		                     	(byte)Opcodes.FRAME, 2,0,0,0,0,0,0,0, (byte)Opcodes.TUPLE3, Opcodes.STOP});
		Object[] value = new Object[] {42,43,44};
		assertArrayEquals(value, result);
	}

	@Test
	public void testGLOBAL() throws PickleException, IOException {
		//GLOBAL = (byte)'c'; // push self.find_class(modname, name); 2 string args
		Object result = U("cdatetime\ntime\n.");
		assertEquals(DateTimeConstructor.class,  result.getClass());
		result = U("cbuiltins\nbytearray\n.");
		assertEquals(ByteArrayConstructor.class,  result.getClass());
	}

	@Test
	public void testSTACK_GLOBAL() throws PickleException, IOException {
		//STACK_GLOBAL = 0x93;  // same as GLOBAL but using names on the stacks
		Object result = U("\u008c\u0008datetime\u008c\u0004time\u0093.");
		assertEquals(DateTimeConstructor.class,  result.getClass());
		result = U("\u008c\u0008builtins\u008c\u0009bytearray\u0093.");
		assertEquals(ByteArrayConstructor.class,  result.getClass());
	}	
}
