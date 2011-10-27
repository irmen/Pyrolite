package net.razorvine.pickle;

import java.io.Serializable;

/**
 * Just a simple class to hold 2 parameters.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public final class Pair<A,B> implements Serializable {
	private static final long serialVersionUID = 7257645944559910774L;
	public final A a;
    public final B b;

    public Pair(A a, B b) {
        this.a = a;
        this.b = b;
    }
};
