/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Pyrolite.Tests.Pickle
{
/// <summary>
/// Some assertion things that don't appear to be in Nunit.
/// </summary>

static class AssertUtils
{
	public static void AssertEqual(IDictionary expected, object actual)
	{
		if(expected.Equals(actual)) return;
		IDictionary actualdict=(IDictionary)actual;
		Assert.AreEqual(expected.Count, actualdict.Count, "dictionary size must be equal");
		ArrayList keys1=new ArrayList(expected.Keys);
		ArrayList keys2=new ArrayList(actualdict.Keys);
		keys1.Sort();
		keys2.Sort();
		Assert.AreEqual(keys1, keys2, "dictionary keys must be the same");
		
		foreach(object key in expected.Keys) {
			object ev=expected[key];
			object av=actualdict[key];
			if(ev is IDictionary) {
				AssertEqual((IDictionary)ev, av);
			} else {
				Assert.AreEqual(ev,av, "dictionary values must be the same");
			}
		}
	}
	
	public static void AssertEqual<T>(HashSet<T> expected, object actual)
	{
		if(expected.Equals(actual)) return;
		List<T> expectedvalues=new List<T>(expected);
		List<T> actualvalues=new List<T>();
		foreach(object x in (IEnumerable)actual) {
			actualvalues.Add((T)x);
		}
		Assert.AreEqual(expectedvalues, actualvalues, "hashsets must be equal");
	}	
}

}
