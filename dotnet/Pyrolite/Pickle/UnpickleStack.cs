/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System.Collections;

namespace Razorvine.Pickle
{

/// <summary>
/// Helper type that represents the unpickler working stack. 
/// </summary>
public class UnpickleStack {
	private ArrayList stack;
	public object MARKER;

	public UnpickleStack() {
		stack = new ArrayList();
		MARKER = new object(); // any new unique object
	}

	public void add(object o) {
		stack.Add(o);
	}

	public void add_mark() {
		stack.Add(MARKER);
	}

	public object pop() {
		int size = stack.Count;
		var result = this.stack[size - 1];
		this.stack.RemoveAt(size-1);
		return result;
	}

	public ArrayList pop_all_since_marker() {
		ArrayList result = new ArrayList();
		object o = pop();
		while (o != this.MARKER) {
			result.Add(o);
			o = pop();
		}
		result.TrimToSize();
		result.Reverse();
		return result;
	}

	public object peek() {
		return this.stack[this.stack.Count-1];
	}

	public void trim() {
		this.stack.TrimToSize();
	}

	public int size() {
		return this.stack.Count;
	}

	public void clear() {
		this.stack.Clear();
		this.stack.TrimToSize();
	}
}

}
