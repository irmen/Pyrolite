package net.razorvine.pickle.objects;

import java.lang.reflect.Method;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;

/**
 * This constructor is called by the helper methods that pickle protocol 0
 * uses from the python copy_reg module to reconstruct c objects.
 */
public class Reconstructor implements IObjectConstructor {
    public Object construct(Object[] args) {
        if(args.length != 3)
            throw new PickleException("invalid pickle data; expecting 3 args to copy_reg reconstructor but recieved " + args.length);

        Object reconstructor = args[0];
        try {
            Method reconstruct=reconstructor.getClass().getMethod("reconstruct", Object.class, Object.class);
            return reconstruct.invoke(reconstructor, args[1], args[2]);
        } catch (Exception e) {
            throw new PickleException("failed to reconstruct()", e);
        }
    }
}