using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Razorvine.Pyro;
using Razorvine.Pyro.Serializer;
using Razorvine.Serpent;
// ReSharper disable CheckNamespace

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Pyrolite.Tests.Pyro;

public class SerpentSerializerTestsNoSets
{
	[Fact]
	public void TestSerpentVersion()
	{
		var serpentVersion = new Version(LibraryVersion.Version);
		Assert.True(serpentVersion >= new Version(1, 16));
	}
		
	[Fact]
	public void TestSerializeData()
	{
		Config.SERPENT_INDENT=false;

		ICollection<object> list = new LinkedList<object>();
		list.Add("hello");
		list.Add(42);
			
		var ser = PyroSerializer.GetSerpentSerializer();
		var data = ser.serializeData(list);
		string str = Encoding.UTF8.GetString(data);
		Assert.Equal("# serpent utf-8 python3.2\n['hello',42]", str);
			
		var listObj = (List<object>)ser.deserializeData(data);
		Assert.Equal(list, listObj);
			
		ISet<object> s = new HashSet<object>();
		s.Add("element1");
		s.Add("element2");
		data = ser.serializeData(s);
		str = Encoding.UTF8.GetString(data);
		Assert.Equal("# serpent utf-8 python3.2\n{'element1','element2'}", str);
			
		var set = (HashSet<object>) ser.deserializeData(data);
		Assert.Equal(set, s);
	}

	[Fact]
	public void TestSerializeCall()
	{
		Config.SERPENT_INDENT=false;

		var ser = PyroSerializer.GetSerpentSerializer();
		IDictionary<string, object> kwargs = new Dictionary<string, object>();
		kwargs["arg"] = 42;
		object[] vargs = {"hello"};
			
		var data = ser.serializeCall("objectid", "method", vargs, kwargs);
		string s = Encoding.UTF8.GetString(data);
		Assert.Equal("# serpent utf-8 python3.2\n('objectid','method',('hello',),{'arg':42})", s);
			
		var call = (object[])ser.deserializeData(data);
		object[] expected = {
			"objectid",
			"method",
			new object[] {"hello"},
			new Dictionary<string, object>
			{
				{"arg", 42}
			}
		};
		Assert.Equal(expected, call);
	}
}

public class SerpentSerializerTestsSets
{
	[Fact]
	public void TestSerializeData()
	{
		Config.SERPENT_INDENT=false;

		ISet<object> s = new HashSet<object>();
		s.Add("element1");
		s.Add("element2");
		var ser = PyroSerializer.GetSerpentSerializer();
		var data = ser.serializeData(s);
		string str = Encoding.UTF8.GetString(data);
		Assert.Equal("# serpent utf-8 python3.2\n{'element1','element2'}", str);
			
		var s2 = (HashSet<object>) ser.deserializeData(data);
		Assert.Equal(s, s2);
	}
		
	[Fact]
	public void TestSerpentBytes()
	{
		Config.SERPENT_INDENT=false;

		var bytes = Encoding.ASCII.GetBytes("hello");
		var ser = new SerpentSerializer();
		var data = ser.serializeData(bytes);
			
		string str = Encoding.ASCII.GetString(data);
		Assert.Contains("base64", str);
			
		var p = new Parser();
		object data2 = p.Parse(data).GetData();
		var bytes2 = SerpentSerializer.ToBytes(data2);
		Assert.Equal(Encoding.ASCII.GetBytes("hello"), bytes2);
	}		
}