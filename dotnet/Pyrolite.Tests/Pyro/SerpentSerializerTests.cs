using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Razorvine.Pyro;
using Razorvine.Serpent;

namespace Pyrolite.Tests.Pyro
{
	[TestFixture]
	public class SerpentSerializerTestsNoSets
	{
		[Test]
		public void TestSerpentVersion()
		{
			Version serpentVersion = new Version(LibraryVersion.Version);
			Assert.IsTrue(serpentVersion >= new Version(1, 16));
		}
		
		[Test]
		public void TestSerializeData()
		{
			ICollection<object> list = new LinkedList<object>();
			list.Add("hello");
			list.Add(42);
			
			var ser = PyroSerializer.GetFor(Config.SerializerType.serpent);
			byte[] data = ser.serializeData(list);
			string str = Encoding.UTF8.GetString(data);
			Assert.AreEqual("# serpent utf-8 python2.6\n['hello',42]", str);
			
			List<object> list_obj = (List<object>)ser.deserializeData(data);
			Assert.AreEqual(list, list_obj);
			
			ISet<string> s = new HashSet<string>();
			s.Add("element1");
			s.Add("element2");
			data = ser.serializeData(s);
			str = Encoding.UTF8.GetString(data);
			Assert.AreEqual("# serpent utf-8 python2.6\n('element1','element2')", str);
			
			object[] array_obj = (object[]) ser.deserializeData(data);
			Assert.AreEqual(s, array_obj);
		}

		[Test]
		public void TestSerializeCall()
		{
			var ser = PyroSerializer.GetFor(Config.SerializerType.serpent);
			IDictionary<string, object> kwargs = new Dictionary<string, object>();
			kwargs["arg"] = 42;
			object[] vargs = new object[] {"hello"};
			
			byte[] data = ser.serializeCall("objectid", "method", vargs, kwargs);
			string s = Encoding.UTF8.GetString(data);
			Assert.AreEqual("# serpent utf-8 python2.6\n('objectid','method',('hello',),{'arg':42})", s);
			
			object[] call = (object[])ser.deserializeData(data);
			object[] expected = new object[] {
				"objectid",
				"method",
				new object[] {"hello"},
				new Dictionary<string, object>() {
					{"arg", 42}
				}
			};
			Assert.AreEqual(expected, call);
		}
	}

	[TestFixture]
	public class SerpentSerializerTestsSets
	{
		[TestFixtureSetUp]
		public void Setup()
		{
			Config.SERPENT_SET_LITERALS=true;
		}
		[TestFixtureTearDown]
		public void Teardown()
		{
			Config.SERPENT_SET_LITERALS=false;
		}

		[Test]
		public void TestSerializeData()
		{
			ISet<string> s = new HashSet<string>();
			s.Add("element1");
			s.Add("element2");
			var ser = PyroSerializer.GetFor(Config.SerializerType.serpent);
			byte[] data = ser.serializeData(s);
			string str = Encoding.UTF8.GetString(data);
			Assert.AreEqual("# serpent utf-8 python3.2\n{'element1','element2'}", str);
			
			HashSet<object> s2 = (HashSet<object>) ser.deserializeData(data);
			Assert.AreEqual(s, s2);
		}
		
		[Test]
		public void TestSerpentBytes()
		{
			byte[] bytes = Encoding.ASCII.GetBytes("hello");
			SerpentSerializer ser = new SerpentSerializer();
			byte[] data = ser.serializeData(bytes);
			
			string str = Encoding.ASCII.GetString(data);
			Assert.IsTrue(str.Contains("base64"));
			
			Razorvine.Serpent.Parser p = new Razorvine.Serpent.Parser();
			Object data2 = p.Parse(data).GetData();
			byte[] bytes2 = SerpentSerializer.ToBytes(data2);
			Assert.AreEqual(Encoding.ASCII.GetBytes("hello"), bytes2);
		}		
	}
}
