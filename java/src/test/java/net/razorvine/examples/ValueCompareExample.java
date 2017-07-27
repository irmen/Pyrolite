package net.razorvine.examples;

import java.util.*;
import java.io.IOException;

import net.razorvine.pickle.Pickler;
import net.razorvine.pickle.Unpickler;

public class ValueCompareExample {

    public static void main(String[] args) throws IOException {

        Random random = new Random(1337);
        List<String> values = new ArrayList<String>();
        for (int i = 0; i < 100000; ++i) {
            values.add(("This is a string with a number in it: " + random.nextInt(100))
                    // .intern() // You could also see what happens when the strings are interned
            );
        }

        long t0 = System.nanoTime();
        byte[] noValueCompare = new Pickler(true, false).dumps(values);
        long t1 = System.nanoTime();
        byte[] withValueCompare = new Pickler(true, true).dumps(values);
        long t2 = System.nanoTime();

        System.out.println("Pickle size without: " + noValueCompare.length + "B, with: " + withValueCompare.length + "B"
                + " (" + (int) ((1 - withValueCompare.length / (double) noValueCompare.length) * 100.0) + "% smaller)");

        Unpickler unpickler = new Unpickler();

        long t3 = System.nanoTime();
        List<String> without = (List<String>) unpickler.loads(noValueCompare);
        long t4 = System.nanoTime();
        List<String> with = (List<String>) unpickler.loads(withValueCompare);
        long t5 = System.nanoTime();

        System.out.println("Whole List equality: ==: " + (without == with) + ", .equals(): " + (without.equals(with)));

        System.out.println(
                "Two equal elements equality without valueCompare: ==: " + (without.get(107) == without.get(259))
                        + ", .equals(): " + (without.get(107).equals(without.get(259))));
        System.out.println("Two equal elements equality with valueCompare: ==: " + (with.get(107) == with.get(259))
                + ", .equals(): " + (with.get(107).equals(with.get(259))));

        System.out.println("Pickling without valueCompare took " + (t1 - t0) / 1000000.0 + "ms");
        System.out.println("Pickling with valueCompare took " + (t2 - t1) / 1000000.0 + "ms");

        System.out.println("Unpickling without valueCompare took " + (t4 - t3) / 1000000.0 + "ms");
        System.out.println("Unpickling with valueCompare took " + (t5 - t4) / 1000000.0 + "ms");

    }

}
