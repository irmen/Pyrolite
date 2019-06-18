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
	short  MARK = '('; // push special markobject on stack
	short  STOP = '.'; // every pickle ends with STOP
	short  POP = '0'; // discard topmost stack item
	short  POP_MARK = '1'; // discard stack top through topmost markobject
	short  DUP = '2'; // duplicate top stack item
	short  FLOAT = 'F'; // push float object; decimal string argument
	short  INT = 'I'; // push integer or bool; decimal string argument
	short  BININT = 'J'; // push four-byte signed int (little endian)
	short  BININT1 = 'K'; // push 1-byte unsigned int
	short  LONG = 'L'; // push long; decimal string argument
	short  BININT2 = 'M'; // push 2-byte unsigned int
	short  NONE = 'N'; // push None
	short  PERSID = 'P'; // push persistent object; id is taken from string arg
	short  BINPERSID = 'Q'; // push persistent object; id is taken from stack
	short  REDUCE = 'R'; // apply callable to argtuple, both on stack
	short  STRING = 'S'; // push string; NL-terminated string argument
	short  BINSTRING = 'T'; // push string; counted binary string argument
	short  SHORT_BINSTRING = 'U'; //push string; counted binary string < 256 bytes
	short  UNICODE = 'V'; // push Unicode string; raw-unicode-escaped'd argument
	short  BINUNICODE = 'X'; //push Unicode string; counted UTF-8 string argument
	short  APPEND = 'a'; // append stack top to list below it
	short  BUILD = 'b'; // call __setstate__ or __dict__.update()
	short  GLOBAL = 'c'; // push self.find_class(modname, name); 2 string args
	short  DICT = 'd'; // build a dict from stack items
	short  EMPTY_DICT = '}'; // push empty dict
	short  APPENDS = 'e'; // extend list on stack by topmost stack slice
	short  GET = 'g'; // push item from memo on stack; index is string arg
	short  BINGET = 'h'; // push item from memo on stack; index is 1-byte arg
	short  INST = 'i'; // build & push class instance
	short  LONG_BINGET = 'j'; // push item from memo on stack; index is 4-byte arg
	short  LIST = 'l'; // build list from topmost stack items
	short  EMPTY_LIST = ']'; // push empty list
	short  OBJ = 'o'; // build & push class instance
	short  PUT = 'p'; // store stack top in memo; index is string arg
	short  BINPUT = 'q'; //store stack top in memo; index is 1-byte arg
	short  LONG_BINPUT = 'r'; // store stack top in memo; index is 4-byte arg
	short  SETITEM = 's'; // add key+value pair to dict
	short  TUPLE = 't'; // build tuple from topmost stack items
	short  EMPTY_TUPLE = ')'; // push empty tuple
	short  SETITEMS = 'u'; // modify dict by adding topmost key+value pairs
	short  BINFLOAT = 'G'; // push float; arg is 8-byte float encoding

	String TRUE = "I01\n"; // not an opcode; see INT docs in pickletools.py
	String FALSE = "I00\n"; // not an opcode; see INT docs in pickletools.py

	// Protocol 2

	short  PROTO = 0x80; // identify pickle protocol
	short  NEWOBJ = 0x81; // build object by applying cls.__new__ to argtuple
	short  EXT1 = 0x82; // push object from extension registry; 1-byte index
	short  EXT2 = 0x83; // ditto, but 2-byte index
	short  EXT4 = 0x84; // ditto, but 4-byte index
	short  TUPLE1 = 0x85; // build 1-tuple from stack top
	short  TUPLE2 = 0x86; // build 2-tuple from two topmost stack items
	short  TUPLE3 = 0x87; // build 3-tuple from three topmost stack items
	short  NEWTRUE = 0x88; // push True
	short  NEWFALSE = 0x89; // push False
	short  LONG1 = 0x8a; // push long from < 256 bytes
	short  LONG4 = 0x8b; // push really big long

	// Protocol 3 (Python 3.x)

	short  BINBYTES = 'B'; // push bytes; counted binary string argument
	short  SHORT_BINBYTES = 'C'; // "     " ; "      " "      " < 256 bytes

	// Protocol 4 (Python 3.4+)

	short  SHORT_BINUNICODE = 0x8c;  // push short string; UTF-8 length < 256 bytes
	short  BINUNICODE8 = 0x8d;  // push very long string
	short  BINBYTES8 = 0x8e;  // push very long bytes string
	short  EMPTY_SET = 0x8f;  // push empty set on the stack
	short  ADDITEMS = 0x90;  // modify set by adding topmost stack items
	short  FROZENSET = 0x91;  // build frozenset from topmost stack items
	short  MEMOIZE = 0x94;  // store top of the stack in memo
	short  FRAME = 0x95;  // indicate the beginning of a new frame
	short  NEWOBJ_EX = 0x92;  // like NEWOBJ but work with keyword only arguments
	short  STACK_GLOBAL = 0x93;  // same as GLOBAL but using names on the stacks

	// Protocol 5 (Python 3.8+)

	short  BYTEARRAY8 = 0x96;		// push bytearray
	short  NEXT_BUFFER = 0x97;		// push next out-of-band buffer
	short  READONLY_BUFFER = 0x98;	//  make top of stack readonly
}
