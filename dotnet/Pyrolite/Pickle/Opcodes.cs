/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

namespace Razorvine.Pickle
{

/// <summary>
/// Pickle opcodes. Taken from Python's stdlib pickle.py.
/// See pickletools.py for extensive docs. The listing
/// here is in kind-of alphabetical order of 1-character pickle code.
/// pickletools groups them by purpose.
/// </summary>
public class Opcodes {

	// protocol 0 and 1
	public const byte  MARK = (byte)'('; // push special markobject on stack
	public const byte  STOP = (byte)'.'; // every pickle ends with STOP
	public const byte  POP = (byte)'0'; // discard topmost stack item
	public const byte  POP_MARK = (byte)'1'; // discard stack top through topmost markobject
	public const byte  DUP = (byte)'2'; // duplicate top stack item
	public const byte  FLOAT = (byte)'F'; // push float object; decimal string argument
	public const byte  INT = (byte)'I'; // push integer or bool; decimal string argument
	public const byte  BININT = (byte)'J'; // push four-byte signed int (little endian)
	public const byte  BININT1 = (byte)'K'; // push 1-byte unsigned int
	public const byte  LONG = (byte)'L'; // push long; decimal string argument
	public const byte  BININT2 = (byte)'M'; // push 2-byte unsigned int
	public const byte  NONE = (byte)'N'; // push None
	public const byte  PERSID = (byte)'P'; // push persistent object; id is taken from string arg
	public const byte  BINPERSID = (byte)'Q'; // "       " "  ;  " "   " " stack
	public const byte  REDUCE = (byte)'R'; // apply callable to argtuple, both on stack
	public const byte  STRING = (byte)'S'; // push string; NL-terminated string argument
	public const byte  BINSTRING = (byte)'T'; // push string; counted binary string argument
	public const byte  SHORT_BINSTRING = (byte)'U'; // "     " ; "      " "      " < 256 bytes
	public const byte  UNICODE = (byte)'V'; // push Unicode string; raw-unicode-escaped'd argument
	public const byte  BINUNICODE = (byte)'X'; // "     " " ; counted UTF-8 string argument
	public const byte  APPEND = (byte)'a'; // append stack top to list below it
	public const byte  BUILD = (byte)'b'; // call __setstate__ or __dict__.update()
	public const byte  GLOBAL = (byte)'c'; // push self.find_class(modname, name); 2 string args
	public const byte  DICT = (byte)'d'; // build a dict from stack items
	public const byte  EMPTY_DICT = (byte)'}'; // push empty dict
	public const byte  APPENDS = (byte)'e'; // extend list on stack by topmost stack slice
	public const byte  GET = (byte)'g'; // push item from memo on stack; index is string arg
	public const byte  BINGET = (byte)'h'; // "    " "    " "   " ; "    " 1-byte arg
	public const byte  INST = (byte)'i'; // build & push class instance
	public const byte  LONG_BINGET = (byte)'j'; // push item from memo on stack; index is 4-byte arg
	public const byte  LIST = (byte)'l'; // build list from topmost stack items
	public const byte  EMPTY_LIST = (byte)']'; // push empty list
	public const byte  OBJ = (byte)'o'; // build & push class instance
	public const byte  PUT = (byte)'p'; // store stack top in memo; index is string arg
	public const byte  BINPUT = (byte)'q'; // "     " "   " " ;   " " 1-byte arg
	public const byte  LONG_BINPUT = (byte)'r'; // "     " "   " " ;   " " 4-byte arg
	public const byte  SETITEM = (byte)'s'; // add key+value pair to dict
	public const byte  TUPLE = (byte)'t'; // build tuple from topmost stack items
	public const byte  EMPTY_TUPLE = (byte)')'; // push empty tuple
	public const byte  SETITEMS = (byte)'u'; // modify dict by adding topmost key+value pairs
	public const byte  BINFLOAT = (byte)'G'; // push float; arg is 8-byte float encoding

	public const string TRUE = "I01\n"; // not an opcode; see INT docs in pickletools.py
	public const string FALSE = "I00\n"; // not an opcode; see INT docs in pickletools.py

	// Protocol 2

	public const byte  PROTO = 0x80; // identify pickle protocol
	public const byte  NEWOBJ = 0x81; // build object by applying cls.__new__ to argtuple
	public const byte  EXT1 = 0x82; // push object from extension registry; 1-byte index
	public const byte  EXT2 = 0x83; // ditto, but 2-byte index
	public const byte  EXT4 = 0x84; // ditto, but 4-byte index
	public const byte  TUPLE1 = 0x85; // build 1-tuple from stack top
	public const byte  TUPLE2 = 0x86; // build 2-tuple from two topmost stack items
	public const byte  TUPLE3 = 0x87; // build 3-tuple from three topmost stack items
	public const byte  NEWTRUE = 0x88; // push True
	public const byte  NEWFALSE = 0x89; // push False
	public const byte  LONG1 = 0x8a; // push long from < 256 bytes
	public const byte  LONG4 = 0x8b; // push really big long

	// Protocol 3 (Python 3.x)

	public const byte  BINBYTES = (byte)'B'; // push bytes; counted binary string argument
	public const byte  SHORT_BINBYTES = (byte)'C'; // "     " ; "      " "      " < 256 bytes
}

}
