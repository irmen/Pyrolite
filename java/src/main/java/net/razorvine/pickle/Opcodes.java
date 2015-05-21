package net.razorvine.pickle;

/**
 * Pickle opcodes. Taken from Python's stdlib pickle.py.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public interface Opcodes {
	// Pickle opcodes. See pickletools.py for extensive docs. The listing
	// here is in kind-of alphabetical order of 1-character pickle code.
	// pickletools groups them by purpose.
	// short datatype because they are UNSIGNED bytes 0..255.

	// protocol 0 and 1
	static short  MARK = '('; // push special markobject on stack
	static short  STOP = '.'; // every pickle ends with STOP
	static short  POP = '0'; // discard topmost stack item
	static short  POP_MARK = '1'; // discard stack top through topmost markobject
	static short  DUP = '2'; // duplicate top stack item
	static short  FLOAT = 'F'; // push float object; decimal string argument
	static short  INT = 'I'; // push integer or bool; decimal string argument
	static short  BININT = 'J'; // push four-byte signed int (little endian)
	static short  BININT1 = 'K'; // push 1-byte unsigned int
	static short  LONG = 'L'; // push long; decimal string argument
	static short  BININT2 = 'M'; // push 2-byte unsigned int
	static short  NONE = 'N'; // push None
	static short  PERSID = 'P'; // push persistent object; id is taken from string arg
	static short  BINPERSID = 'Q'; // push persistent object; id is taken from stack
	static short  REDUCE = 'R'; // apply callable to argtuple, both on stack
	static short  STRING = 'S'; // push string; NL-terminated string argument
	static short  BINSTRING = 'T'; // push string; counted binary string argument
	static short  SHORT_BINSTRING = 'U'; //push string; counted binary string < 256 bytes
	static short  UNICODE = 'V'; // push Unicode string; raw-unicode-escaped'd argument
	static short  BINUNICODE = 'X'; //push Unicode string; counted UTF-8 string argument
	static short  APPEND = 'a'; // append stack top to list below it
	static short  BUILD = 'b'; // call __setstate__ or __dict__.update()
	static short  GLOBAL = 'c'; // push self.find_class(modname, name); 2 string args
	static short  DICT = 'd'; // build a dict from stack items
	static short  EMPTY_DICT = '}'; // push empty dict
	static short  APPENDS = 'e'; // extend list on stack by topmost stack slice
	static short  GET = 'g'; // push item from memo on stack; index is string arg
	static short  BINGET = 'h'; // push item from memo on stack; index is 1-byte arg
	static short  INST = 'i'; // build & push class instance
	static short  LONG_BINGET = 'j'; // push item from memo on stack; index is 4-byte arg
	static short  LIST = 'l'; // build list from topmost stack items
	static short  EMPTY_LIST = ']'; // push empty list
	static short  OBJ = 'o'; // build & push class instance
	static short  PUT = 'p'; // store stack top in memo; index is string arg
	static short  BINPUT = 'q'; //store stack top in memo; index is 1-byte arg
	static short  LONG_BINPUT = 'r'; // store stack top in memo; index is 4-byte arg
	static short  SETITEM = 's'; // add key+value pair to dict
	static short  TUPLE = 't'; // build tuple from topmost stack items
	static short  EMPTY_TUPLE = ')'; // push empty tuple
	static short  SETITEMS = 'u'; // modify dict by adding topmost key+value pairs
	static short  BINFLOAT = 'G'; // push float; arg is 8-byte float encoding

	static String TRUE = "I01\n"; // not an opcode; see INT docs in pickletools.py
	static String FALSE = "I00\n"; // not an opcode; see INT docs in pickletools.py

	// Protocol 2

	static short  PROTO = 0x80; // identify pickle protocol
	static short  NEWOBJ = 0x81; // build object by applying cls.__new__ to argtuple
	static short  EXT1 = 0x82; // push object from extension registry; 1-byte index
	static short  EXT2 = 0x83; // ditto, but 2-byte index
	static short  EXT4 = 0x84; // ditto, but 4-byte index
	static short  TUPLE1 = 0x85; // build 1-tuple from stack top
	static short  TUPLE2 = 0x86; // build 2-tuple from two topmost stack items
	static short  TUPLE3 = 0x87; // build 3-tuple from three topmost stack items
	static short  NEWTRUE = 0x88; // push True
	static short  NEWFALSE = 0x89; // push False
	static short  LONG1 = 0x8a; // push long from < 256 bytes
	static short  LONG4 = 0x8b; // push really big long

	// Protocol 3 (Python 3.x)

	static short  BINBYTES = 'B'; // push bytes; counted binary string argument
	static short  SHORT_BINBYTES = 'C'; // "     " ; "      " "      " < 256 bytes

	// Protocol 4 (Python 3.4+)

	static short SHORT_BINUNICODE = 0x8c;  // push short string; UTF-8 length < 256 bytes
	static short BINUNICODE8 = 0x8d;  // push very long string
	static short BINBYTES8 = 0x8e;  // push very long bytes string
	static short EMPTY_SET = 0x8f;  // push empty set on the stack
	static short ADDITEMS = 0x90;  // modify set by adding topmost stack items
	static short FROZENSET = 0x91;  // build frozenset from topmost stack items
	static short MEMOIZE = 0x94;  // store top of the stack in memo
	static short FRAME = 0x95;  // indicate the beginning of a new frame
	static short NEWOBJ_EX = 0x92;  // like NEWOBJ but work with keyword only arguments
	static short STACK_GLOBAL = 0x93;  // same as GLOBAL but using names on the stacks
}
