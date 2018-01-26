/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using Xunit;
using Razorvine.Pickle;
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable 169
#pragma warning disable 414

namespace Pyrolite.Tests.Pickle
{


/// <summary>
/// Unit tests for the pickler. 
/// </summary>
public class PicklerTests {

	byte[] B(string s) {
		return B(PickleUtils.str2bytes(s));
	}
	
	byte[] B(byte[] bytes) {
		byte[] result=new byte[bytes.Length+3];
		result[0]=Opcodes.PROTO;
		result[1]=2;
		result[result.Length-1]=Opcodes.STOP;
		Array.Copy(bytes,0,result,2,bytes.Length);
		return result;
	}
	
	string S(byte[] pickled) {
		return PickleUtils.rawStringFromBytes(pickled);
	}

	private enum DayEnum {
	    SUNDAY, MONDAY, TUESDAY, WEDNESDAY, 
	    THURSDAY, FRIDAY, SATURDAY 
	};
	
	[Fact]
	public void testSinglePrimitives() {
		// protocol level 2
		Pickler p=new Pickler(false);
		byte[] o=p.dumps(null);	// none
		Assert.Equal(B("N"), o); 
		o=p.dumps('@');  // char --> string
		Assert.Equal(B("X\u0001\u0000\u0000\u0000@"), o);
		o=p.dumps(true);	// bool
		Assert.Equal(B("\u0088"), o);
		o=p.dumps("hello");      // unicode string
		Assert.Equal(B("X\u0005\u0000\u0000\u0000hello"), o);
		o=p.dumps("hello\u20ac");      // unicode string with non ascii
		Assert.Equal(B("X\u0008\u0000\u0000\u0000hello\u00e2\u0082\u00ac"), o);
		o=p.dumps((byte)'@');
		Assert.Equal(B("K@"), o);
		o=p.dumps((sbyte)-40);
		Assert.Equal( B(new byte[]{(byte)'J',0xd8,0xff,0xff,0xff}), o);
		o=p.dumps((short)-0x1234);
		Assert.Equal(B("J\u00cc\u00ed\u00ff\u00ff"), o);
		o=p.dumps((ushort)0xf234);
		Assert.Equal(B("M\u0034\u00f2"), o);
		o=p.dumps(-0x12345678);
		Assert.Equal(B("J\u0088\u00a9\u00cb\u00ed"), o);
		o=p.dumps((uint)0x12345678);
		Assert.Equal(B(new byte[]{(byte)'J', 0x78, 0x56, 0x34, 0x12}), o);
		o=p.dumps(0xf2345678);
		Assert.Equal(B("I4063516280\n"), o);
		o=p.dumps(0x12345678abcdefL);
		Assert.Equal(B("I5124095577148911\n"), o);
		o=p.dumps(1234.5678d);
		Assert.Equal(B(new byte[] {(byte)'G',0x40,0x93,0x4a,0x45,0x6d,0x5c,0xfa,0xad}), o);
		o=p.dumps(1234.5f);
		Assert.Equal(B(new byte[] {(byte)'G',0x40,0x93,0x4a,0,0,0,0,0}), o);
		o=p.dumps(1234.9876543210987654321m);
		Assert.Equal(B("cdecimal\nDecimal\nX\u0018\u0000\u0000\u00001234.9876543210987654321\u0085R"), o);
		
		DayEnum day=DayEnum.WEDNESDAY;
		o=p.dumps(day);	// enum is returned as just a string representing the value
		Assert.Equal(B("X\u0009\u0000\u0000\u0000WEDNESDAY"),o);
	}
	
	[Fact]
	public void testArrays() 
	{
		Pickler p = new Pickler(false);
		var o = p.dumps(new string[] {});
		Assert.Equal(B(")"), o);
		o=p.dumps(new [] {"abc"});
		Assert.Equal(B("X\u0003\u0000\u0000\u0000abc\u0085"), o);
		o=p.dumps(new [] {"abc","def"});
		Assert.Equal(B("X\u0003\u0000\u0000\u0000abcX\u0003\u0000\u0000\u0000def\u0086"), o);
		o=p.dumps(new [] {"abc","def","ghi"});
		Assert.Equal(B("X\u0003\u0000\u0000\u0000abcX\u0003\u0000\u0000\u0000defX\u0003\u0000\u0000\u0000ghi\u0087"), o);
		o=p.dumps(new [] {"abc","def","ghi","jkl"});
		Assert.Equal(B("(X\u0003\u0000\u0000\u0000abcX\u0003\u0000\u0000\u0000defX\u0003\u0000\u0000\u0000ghiX\u0003\u0000\u0000\u0000jklt"), o);

		o=p.dumps(new [] {'A','B','C'});
		Assert.Equal(B("X\u0003\u0000\u0000\u0000ABC"), o);

		o=p.dumps(new [] {true,false,true});
		Assert.Equal(B("\u0088\u0089\u0088\u0087"), o);

		o=p.dumps(new byte[] {1,2,3});
		Assert.Equal(B("c__builtin__\nbytearray\nX\u0003\u0000\u0000\u0000\u0001\u0002\u0003X\u0007\u0000\u0000\u0000latin-1\u0086R"), o);

		o=p.dumps(new [] {1,2,3});
		Assert.Equal(B("carray\narray\nU\u0001i](K\u0001K\u0002K\u0003e\u0086R"), o);

		o=p.dumps(new [] {1.1,2.2,3.3});
		Assert.Equal(B("carray\narray\nU\u0001d](G?\u00f1\u0099\u0099\u0099\u0099\u0099\u009aG@\u0001\u0099\u0099\u0099\u0099\u0099\u009aG@\nffffffe\u0086R"), o);
	}
	
	[Fact]
	public void TestRecursiveArray2()
	{
		Pickler p = new Pickler(false);
		object[] a = new object[] { "hello", "placeholder" };
		a[1] = a; // make it recursive
		Assert.Throws<PickleException>(() => p.dumps(a));   // "recursive array not supported, use list"
	}
	
	[Fact]
	public void TestRecursiveArray6()
	{
		Pickler p = new Pickler(false);
		object[] a = new object[] { "a","b","c","d","e","f" };
		a[5] = a; // make it recursive
		Assert.Throws<PickleException>(() => p.dumps(a));   // "recursive array not supported, use list"
	}

	[Fact]
	public void testDates() 
	{
		Pickler p=new Pickler(false);
		Unpickler u=new Unpickler();
			
		DateTime date=new DateTime(2011,12,31,14,33,59);
		byte[] o = p.dumps(date);
		object unpickled=u.loads(o);
		Assert.Equal(date, unpickled);

		date=new DateTime(2011,12,31,14,33,59,456);
		o=p.dumps(date);
		unpickled=u.loads(o);
		Assert.Equal(date, unpickled);
	}
	
	[Fact]
	public void testTimes() 
	{
		Pickler p=new Pickler(false);
		Unpickler u=new Unpickler();
		
		TimeSpan ts=new TimeSpan(2, 0, 0, 7000, 456);
		byte[] o = p.dumps(ts);
		object unpickled=u.loads(o);
		Assert.Equal(ts,unpickled);
		Assert.Equal(B("cdatetime\ntimedelta\nK\u0002MX\u001bJ@\u00f5\u0006\u0000\u0087R"), o);
	}

	[Fact]
	public void testSets() 
	{
		Pickler p=new Pickler(false);
		Unpickler up=new Unpickler();

		var intset = new HashSet<int> {1, 2, 3};
		var o = p.dumps(intset);
		HashSet<object> resultset=(HashSet<object>)up.loads(o);
		AssertUtils.AssertEqual(intset, resultset);

		HashSet<string> stringset = new HashSet<string> {"A", "B", "C"};
		o=p.dumps(stringset);
		resultset=(HashSet<object>)up.loads(o);
		AssertUtils.AssertEqual(stringset, resultset);
	}

	[Fact]
	public void testMappings() 
	{
		Pickler p=new Pickler(false);
		Unpickler pu=new Unpickler();
		var intmap = new Dictionary<int, int> {{1, 11}, {2, 22}, {3, 33}};
		var o = p.dumps(intmap);
		Hashtable resultmap=(Hashtable)pu.loads(o);
		AssertUtils.AssertEqual(intmap, resultmap);

		var stringmap = new Dictionary<string, string> {{"A", "1"}, {"B", "2"}, {"C", "3"}};
		o=p.dumps(stringmap);
		resultmap=(Hashtable)pu.loads(o);
		AssertUtils.AssertEqual(stringmap, resultmap);

		Hashtable table = new Hashtable {{1, 11}, {2, 22}, {3, 33}};
		o=p.dumps(table);
		resultmap=(Hashtable)pu.loads(o);
		AssertUtils.AssertEqual(table, resultmap);
	}
	
	[Fact]
	public void testLists()  
	{
		Pickler p=new Pickler(false);
		
		IList list=new ArrayList();
		list.Add(1);
		list.Add("abc");
		list.Add(null);
		var o = p.dumps(list);
		Assert.Equal(B("](K\u0001X\u0003\u0000\u0000\u0000abcNe"), o);
		
		IList<object> ilist=new List<object>();
		ilist.Add(1);
		ilist.Add("abc");
		ilist.Add(null);
		o=p.dumps(ilist);
		Assert.Equal(B("](K\u0001X\u0003\u0000\u0000\u0000abcNe"), o);

		Stack<int> stack=new Stack<int>();
		stack.Push(1);
		stack.Push(2);
		stack.Push(3);
		o=p.dumps(stack);
		Assert.Equal(B("](K\u0003K\u0002K\u0001e"), o);
		
		var queue=new Queue<int>();
		queue.Enqueue(1);
		queue.Enqueue(2);
		queue.Enqueue(3);
		o=p.dumps(queue);
		Assert.Equal(B("](K\u0001K\u0002K\u0003e"), o);
 	}

	[Fact]
	public void testMemoizationSet()
	{
		var set = new HashSet<string> {"a"};
		object[] array = new object[] {set, set};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		Assert.Contains(Opcodes.BINPUT, data); // check that memoization was done
		
		Unpickler u = new Unpickler();
		object[] result = (object[]) u.loads(data);
		Assert.Equal(2, result.Length);
		object first = result[0];
		object second = result[1];
		Assert.IsType<HashSet<object>>(first);
		Assert.IsType<HashSet<object>>(second);
		Assert.Same(first, second);				// both objects should be the same memoized object

		HashSet<object> theSet = (HashSet<object>)second;
		Assert.Equal(1, theSet.Count);
		Assert.True(theSet.Contains("a"));
	}
	
	[Fact]
	public void testMemoizationMap()
	{
		var map = new Dictionary<string, string> {{"key", "value"}};
		object[] array = {map, map};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		Assert.Contains(Opcodes.BINPUT, data); // check that memoization was done
		
		Unpickler u = new Unpickler();
		object[] result = (object[]) u.loads(data);
		Assert.Equal(2, result.Length);
		object first = result[0];
		object second = result[1];
		Assert.IsType<Hashtable>(first);
		Assert.IsType<Hashtable>(second);
		Assert.Same(first, second);				// both objects should be the same memoized object

		Hashtable theMap = (Hashtable) second;
		Assert.Equal(1, theMap.Count);
		Assert.Equal("value", theMap["key"]);
	}

	[Fact]
	public void testMemoizationCollection()
	{
		ICollection<string> list = new List<string>();
		list.Add("a");
		object[] array = new object[] {list, list};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		Assert.Contains(Opcodes.BINPUT, data); // check that memoization was done
		
		Unpickler u = new Unpickler();
		object[] result = (object[]) u.loads(data);
		Assert.Equal(2, result.Length);
		object first = result[0];
		object second = result[1];
		Assert.IsType<ArrayList>(first);
		Assert.IsType<ArrayList>(second);
		Assert.Same(first, second);				// both objects should be the same memoized object

		ArrayList theList = (ArrayList) second;
		Assert.Equal(1, theList.Count);
		Assert.True(theList.Contains("a"));
	}
	
	[Fact]
	public void testMemoizationTimeStuff()
	{
		TimeSpan delta = new TimeSpan(1,2,3);
		DateTime time = new DateTime(2014,11,20,1,2,3);
	
		object[] array = new object[] {delta, delta, time, time};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		Assert.Contains(Opcodes.BINPUT, data); // check that memoization was done
		
		Unpickler u = new Unpickler();
		object[] result = (object[]) u.loads(data);
		Assert.Equal(4, result.Length);
		Assert.IsType<TimeSpan>(result[0]);
		Assert.IsType<TimeSpan>(result[1]);
		Assert.IsType<DateTime>(result[2]);
		Assert.IsType<DateTime>(result[3]);
		Assert.Same(result[0], result[1]);				// both objects should be the same memoized object
		Assert.Same(result[2], result[3]);				// both objects should be the same memoized object

		delta = (TimeSpan) result[1];
		time = (DateTime) result[3];
		Assert.Equal(new TimeSpan(1,2,3), delta);
		Assert.Equal(new DateTime(2014,11,20,1,2,3), time);
}
	
	[Fact]
	public void testMemoizationDecimal()
	{
		decimal bigd = 12345678901234567890.99887766m;
		
		object[] array = new object[] {bigd, bigd};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		Assert.Contains(Opcodes.BINPUT, data); // check that memoization was done
		
		Unpickler u = new Unpickler();
		object[] result = (object[]) u.loads(data);
		Assert.Equal(2, result.Length);
		Assert.IsType<decimal>(result[0]);
		Assert.IsType<decimal>(result[1]);
		Assert.Same(result[0], result[1]);				// both objects should be the same memoized object

		bigd = (decimal) result[1];
		Assert.Equal(12345678901234567890.99887766m, bigd);
	}

	[Fact]
	public void testMemoizationString()
	{
		string str = "a";
		object[] array = new object[] {str, str};
		
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		Assert.Contains(Opcodes.BINPUT, data); // check that memoization was done
		
		Unpickler u = new Unpickler();
		object[] result = (object[]) u.loads(data);
		Assert.Equal(2, result.Length);
		object first = result[0];
		object second = result[1];
		Assert.IsType<string>(first);
		Assert.IsType<string>(second);
		Assert.Same(first, second);				// both objects should be the same memoized object
		
		str = (string) second;
		Assert.Equal("a", str);
	}
	
	[Fact]
	public void testMemoizationArray()
	{
		int[] arr = new int[] { 1, 2, 3};
		object array = new object[] {arr, arr};
		Pickler p = new Pickler(true);
		byte[] data = p.dumps(array);
		Assert.Contains(Opcodes.BINPUT, data); // check that memoization was done
		
		Unpickler u = new Unpickler();
		object[] result = (object[]) u.loads(data);
		Assert.Equal(2, result.Length);
		object first = result[0];
		object second = result[1];
		Assert.IsType<int[]>(first);
		Assert.IsType<int[]>(second);
		Assert.Same(first, second);				// both objects should be the same memoized object
		
		arr = (int[]) second;
		Assert.Equal(3, arr.Length);
		Assert.Equal(new [] {1, 2, 3}, arr)	;
	}
		
	[Fact]
	public void testMemoizationList()  
	{
		Pickler p=new Pickler();
		
		string reused = "reused";
		string another = "another";
		IList list=new ArrayList();
		IList sublist = new ArrayList();
		sublist.Add(reused);
		sublist.Add(reused);
		sublist.Add(another);
		list.Add(reused);
		list.Add(reused);
		list.Add(another);
		list.Add(sublist);
		var o = p.dumps(list);
		Assert.Equal("\x80\x02]q\x00(X\x06\x00\x00\x0000reusedq\x01h\x01X\x07\x00\x00\x0000anotherq\x02]q\x03(h\x01h\x01h\x0002ee.", S(o));
		
		Unpickler u = new Unpickler();
		ArrayList data = (ArrayList) u.loads(o);
		Assert.Equal(4, data.Count);
		string s1 = (string) data[0];
		string s2 = (string) data[1];
		string s3 = (string) data[2];
		data = (ArrayList) data[3];
		string s4 = (string) data[0];
		string s5 = (string) data[1];
		string s6 = (string) data[2];
		Assert.Equal("reused", s1);
		Assert.Equal("another", s3);
		Assert.Same(s1, s2);
		Assert.Same(s3, s6);
		Assert.Same(s1, s4);
		Assert.Same(s1, s5);
	}
		
	[Fact]
	public void testMemoizationRecursiveNoMemo()  
	{
		Pickler p=new Pickler(false);
		
		const string reused = "reused";
		IList list=new ArrayList();
		list.Add(reused);
		list.Add(reused);
		list.Add(list);
		Assert.Throws<StackOverflowException>(() => p.dumps(list));
	}

	[Fact]
	public void testMemoizationRecursiveMemo()  
	{
		Pickler p=new Pickler();
		
		// self-referencing list
		const string reused = "reused";
		IList list=new ArrayList();
		list.Add(reused);
		list.Add(reused);
		list.Add(list);
		var o = p.dumps(list);
		Assert.Equal("\x80\x02]q\x00(X\x06\x00\x00\x0000reusedq\x01h\x01h\x0000e.", S(o));

		Unpickler u = new Unpickler();
		ArrayList data = (ArrayList) u.loads(o);
		Assert.Equal(3, data.Count);
		string s1 = (string) data[0];
		string s2 = (string) data[1];
		ArrayList data2 = (ArrayList) data[2];
		Assert.Equal("reused", s1);
		Assert.Same(s1, s2);
		Assert.Same(data, data2);
		Assert.Same(data[0], data2[0]);
		
		// self-referencing hashtable
		Hashtable h = new Hashtable();
		h["myself"] = h;
		o=p.dumps(h);
		Assert.Equal("\x80\x02}q\x00(X\x06\x00\x00\x0000myselfq\x01h\x0000u.", S(o));
		Hashtable h2 = (Hashtable) u.loads(o);
		Assert.Equal(1, h2.Count);
		Assert.Same(h2, h2["myself"]);
	}

	public class Person {
		
		public string Name {get;}
		public bool Deceased {get;}
		public int[] Values {get;}
				
		public Person(string name, bool deceased, int[] values) {
	    	this.Name=name;
	    	this.Deceased=deceased;
	    	this.Values = values;
	    }
	}
	
	public class Relative : Person {
		public string Relation{get;}

		public Relative(string name, bool deceased, int[] values) : base(name, deceased, values) {
			Relation="unspecified";
		}
	}


	[Fact]
	public void testClass() 
	{
		Pickler p=new Pickler(false);
		Unpickler pu=new Unpickler();
		Relative person=new Relative("Tupac",true, new [] {3,4,5});
		var o = p.dumps(person);
		Hashtable map=(Hashtable) pu.loads(o);
		
		Assert.Equal(5, map.Count);
		Assert.Equal("Pyrolite.Tests.Pickle.PicklerTests+Relative", map["__class__"]);
		Assert.Equal("Tupac", map["Name"]);
		Assert.Equal("unspecified", map["Relation"]);
		Assert.Equal(true, map["Deceased"]);
		Assert.Equal(new [] {3,4,5}, map["Values"]);
	}
	
	class NotABean {
		public int x;
	}

	[Fact]
	public void testFailure()
	{
		NotABean notabean = new NotABean {x = 42};
		Pickler p=new Pickler(false);
		Assert.Throws<PickleException>(() => p.dumps(notabean));
	}
	
	class CustomClass {
		public int x=42;
	}
	class CustomClassPickler : IObjectPickler {
		public void pickle(object o, Stream outs, Pickler currentpickler)  {
			CustomClass c=(CustomClass)o;
			currentpickler.save("customclassint="+c.x);		// write a string representation
		}
	}
	
	[Fact]
	public void testCustomPickler() 
	{
		Pickler.registerCustomPickler(typeof(CustomClass), new CustomClassPickler());
		CustomClass c=new CustomClass();
		Pickler p=new Pickler(false);
		byte[] ser = p.dumps(c);
		
		Unpickler u = new Unpickler();
		string x = (string) u.loads(ser);
		Assert.Equal("customclassint=42", x);
	}
	
	[Fact]
	public void testAnonType()
	{
		var x = new { Name="Harry", Country="UK", Age=34 };
		Pickler p = new Pickler();
		byte[] ser = p.dumps(x);
		
		Unpickler u = new Unpickler();
		object y = u.loads(ser);
		IDictionary dict = (IDictionary) y;
		Assert.Equal(3, dict.Count);
		Assert.Equal(34, dict["Age"]);
		Assert.False(dict.Contains("__class__"));
	}
	
	
	interface IBaseInterface {};
	interface ISubInterface : IBaseInterface {};
	class BaseClassWithInterface : IBaseInterface {};
	class SubClassWithInterface : BaseClassWithInterface, ISubInterface {};
	class BaseClass {};
	class SubClass : BaseClass {};
	abstract class AbstractBaseClass {};
	class ConcreteSubClass : AbstractBaseClass {};

	class AnyClassPickler : IObjectPickler {
		public void pickle(object o, Stream outs, Pickler currentpickler)  {
			currentpickler.save("[class="+o.GetType().FullName+"]");
		}
	}

	[Fact]
	public void testAbstractBaseClassHierarchyPickler()
	{
		ConcreteSubClass c = new ConcreteSubClass();
		Pickler p = new Pickler(false);
		try {
			p.dumps(c);
			Assert.True(false, "should crash");
		} catch (PickleException x) {
			Assert.True(x.Message.Contains("couldn't pickle object of type"));
		}
		
		Pickler.registerCustomPickler(typeof(AbstractBaseClass), new AnyClassPickler());
		byte[] data = p.dumps(c);
		Assert.True(S(data).Contains("[class=Pyrolite.Tests.Pickle.PicklerTests+ConcreteSubClass]"));
	}
	
	[Fact]
	public void testInterfaceHierarchyPickler()
	{
		BaseClassWithInterface b = new BaseClassWithInterface();
		SubClassWithInterface sub = new SubClassWithInterface();
		Pickler p = new Pickler(false);
		try {
			p.dumps(b);
			Assert.True(false, "should crash");
		} catch (PickleException x) {
			Assert.True(x.Message.Contains("couldn't pickle object of type"));
		}
		try {
			p.dumps(sub);
			Assert.True(false, "should crash");
		} catch (PickleException x) {
			Assert.True(x.Message.Contains("couldn't pickle object of type"));
		}
		Pickler.registerCustomPickler(typeof(IBaseInterface), new AnyClassPickler());
		byte[] data = p.dumps(b);
		Assert.True(S(data).Contains("[class=Pyrolite.Tests.Pickle.PicklerTests+BaseClassWithInterface]"));
		data = p.dumps(sub);
		Assert.True(S(data).Contains("[class=Pyrolite.Tests.Pickle.PicklerTests+SubClassWithInterface]"));
	}	
	
	
	[Serializable]
	class SerializableThing
	{
		[NonSerialized]
		public string NotThisOne = "apple";
		
		public string TakeThisOne = "banana";
		
		public int TakeThisInt => 42;
	}
	
	[DataContract(Name="CustomContractName", Namespace="http://namespace")]
	class DataContractThing
	{
		public string NotThisOne = "apple";
		
		[DataMember(Name="CustomMemberName")]
		public string TakeThisOne = "banana";
		[DataMember]
		public int TakeThisIntToo => 42;

		public int NotThisInt = 999;
		public int NotThisIntEither => 99;
	}
	
	[Fact]
	public void TestSerializableAttr()
	{
		var obj = new SerializableThing();
		
		var p = new Pickler();
		byte[] data = p.dumps(obj);

		var u = new Unpickler();
		IDictionary value = (IDictionary) u.loads(data);
		Assert.Equal(3, value.Count);
		Assert.Equal("Pyrolite.Tests.Pickle.PicklerTests+SerializableThing", value["__class__"]);
		Assert.Equal(42, value["TakeThisInt"]);
		Assert.Equal("banana", value["TakeThisOne"]);
	}

	[Fact]
	public void TestDatacontractAttr()
	{
		var obj = new DataContractThing();
		
		var p = new Pickler();
		byte[] data = p.dumps(obj);

		var u = new Unpickler();
		IDictionary value = (IDictionary) u.loads(data);
		Assert.Equal(3, value.Count);
		Assert.Equal("CustomContractName", value["__class__"]);
		Assert.Equal(42, value["TakeThisIntToo"]);
		Assert.Equal("banana", value["CustomMemberName"]);
	}
}

/// <summary>
/// Miscellaneous tests.
/// </summary>
public class MiscellaneousTests {
	[Fact]
	public void testPythonExceptionType()
	{
		var ex=new PythonException("hello");
		var type = ex.GetType();
		var prop = type.GetProperty("PythonExceptionType");
		Assert.NotNull(prop);  // "python exception class has to have a property PythonExceptionType, it is used in constructor classes");
		prop = type.GetProperty("_pyroTraceback");
		Assert.NotNull(prop);  // "python exception class has to have a property _pyroTraceback, it is used in constructor classes");
	}
}

}
