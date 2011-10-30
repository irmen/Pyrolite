/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Collections;
using NUnit.Framework;
using Razorvine.Pickle;

namespace Pyrolite.Tests.Pickle
{

/// <summary>
/// Unit tests for the unpickler stack object. 
/// </summary>
[TestFixture]
public class UnpickleStackTest {

	[Test]
	public void testPopSinceMarker() {
		UnpickleStack s=new UnpickleStack();
		s.add("a");
		s.add("b");
		s.add_mark();
		s.add("c");
		s.add("d");
		s.add_mark();
		s.add("e");
		s.add("f");
		ArrayList top=s.pop_all_since_marker();
		ArrayList expected=new ArrayList();
		expected.Add("e");
		expected.Add("f");
		Assert.AreEqual(expected, top);
		Assert.AreEqual("d",s.pop());
		Assert.AreEqual("c",s.pop());
	}

	[Test]
	public void testAddPop() {
		UnpickleStack s=new UnpickleStack();
		Assert.AreEqual(0, s.size());
		s.add("x");
		Assert.AreEqual(1, s.size());
		s.add("y");
		Assert.AreEqual(2, s.size());
		Assert.AreEqual("y", s.peek());
		Assert.AreEqual("y", s.pop());
		Assert.AreEqual("x", s.peek());
		Assert.AreEqual("x", s.pop());
		Assert.AreEqual(0, s.size());
	}

	[Test]
	public void testClear() {
		UnpickleStack s=new UnpickleStack();
		s.add("x");
		s.add("y");
		Assert.AreEqual(2, s.size());
		s.clear();
		Assert.AreEqual(0, s.size());
	}

	[Test]
	public void testTrim() {
		UnpickleStack s=new UnpickleStack();
		s.add("a");
		s.add("b");
		s.add("c");
		s.add("d");
		s.add("e");
		Assert.AreEqual(5, s.size());
		s.trim();
		Assert.AreEqual(5, s.size());
	}
}

}
