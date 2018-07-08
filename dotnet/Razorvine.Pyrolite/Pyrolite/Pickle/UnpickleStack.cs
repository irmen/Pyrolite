/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Razorvine.Pickle
{

/// <summary>
/// Helper type that represents the unpickler working stack. 
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UnpickleStack {
	private readonly ArrayList _stack;
	public readonly object MARKER;

	public UnpickleStack() {
		_stack = new ArrayList();
		MARKER = new object(); // any new unique object
	}

	public void add(object o) {
		_stack.Add(o);
	}

	public void add_mark() {
		_stack.Add(MARKER);
	}

	public object pop() {
		int size = _stack.Count;
		var result = _stack[size - 1];
		_stack.RemoveAt(size-1);
		return result;
	}

	public ArrayList pop_all_since_marker() {
		ArrayList result = new ArrayList();
		object o = pop();
		while (o != MARKER) {
			result.Add(o);
			o = pop();
		}
		result.TrimToSize();
		result.Reverse();
		return result;
	}

	public object peek() {
		return _stack[_stack.Count-1];
	}

	public void trim() {
		_stack.TrimToSize();
	}

	public int size() {
		return _stack.Count;
	}

	public void clear() {
		_stack.Clear();
		_stack.TrimToSize();
	}
}

}
