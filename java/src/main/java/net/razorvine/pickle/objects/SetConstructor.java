package net.razorvine.pickle.objects;

import java.util.ArrayList;
import java.util.HashSet;

import net.razorvine.pickle.IObjectConstructor;

/**
 * This object constructor creates sets.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class SetConstructor implements IObjectConstructor {

	public SetConstructor() {
	}

	public Object construct(Object[] args) {
		// create a HashSet, args=arraylist of stuff to put in it
		@SuppressWarnings("unchecked")
		ArrayList<Object> data = (ArrayList<Object>) args[0];
		return new HashSet<Object>(data);
	}
}
