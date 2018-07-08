/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
// ReSharper disable CheckNamespace

namespace Pyrolite.Tests.Pickle
{
/// <summary>
/// Some assertion things that don't appear to be in Nunit.
/// </summary>
internal static class AssertUtils
{
	public static void AssertEqual(IDictionary expected, object actual)
	{
		if(expected.Equals(actual)) return;
		IDictionary actualdict=(IDictionary)actual;
		Assert.Equal(expected.Count, actualdict.Count); // dictionary size must be equal
		ArrayList keys1=new ArrayList(expected.Keys);
		ArrayList keys2=new ArrayList(actualdict.Keys);
		keys1.Sort();
		keys2.Sort();
		Assert.Equal(keys1, keys2);  // dictionary keys must be the same
		
		foreach(object key in expected.Keys) {
			object ev=expected[key];
			object av=actualdict[key];
			var dictionary = ev as IDictionary;
			if(dictionary != null) {
				AssertEqual(dictionary, av);
			} else {
				Assert.Equal(ev, av);  // dictionary values must be the same
			}
		}
	}
	
	public static void AssertEqual<T>(HashSet<T> expected, object actual)
	{
		if(expected.Equals(actual)) return;
		var expectedvalues=new List<T>(expected);
		var actualvalues= ((IEnumerable) actual).Cast<T>().ToList();
		Assert.Equal(expectedvalues, actualvalues);  // hashsets must be equal
	}	
}

}
