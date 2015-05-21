package net.razorvine.pickle.test;

import static org.junit.Assert.*;

import java.util.ArrayList;

import net.razorvine.pickle.UnpickleStack;

import org.junit.Test;

/**
 * Unit tests for the unpickler stack object.
 *  
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class UnpickleStackTest {

	@Test
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
		ArrayList<Object> top=s.pop_all_since_marker();
		ArrayList<Object> expected=new ArrayList<Object>();
		expected.add("e");
		expected.add("f");
		assertEquals(expected, top);
		assertEquals("d",s.pop());
		assertEquals("c",s.pop());
	}

	@Test
	public void testAddPop() {
		UnpickleStack s=new UnpickleStack();
		assertEquals(0, s.size());
		s.add("x");
		assertEquals(1, s.size());
		s.add("y");
		assertEquals(2, s.size());
		assertEquals("y", s.peek());
		assertEquals("y", s.pop());
		assertEquals("x", s.peek());
		assertEquals("x", s.pop());
		assertEquals(0, s.size());
	}

	@Test
	public void testClear() {
		UnpickleStack s=new UnpickleStack();
		s.add("x");
		s.add("y");
		assertEquals(2, s.size());
		s.clear();
		assertEquals(0, s.size());
	}

	@Test
	public void testTrim() {
		UnpickleStack s=new UnpickleStack();
		s.add("a");
		s.add("b");
		s.add("c");
		s.add("d");
		s.add("e");
		assertEquals(5, s.size());
		s.trim();
		assertEquals(5, s.size());
	}
}
