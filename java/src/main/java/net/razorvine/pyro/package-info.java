/**
 * Lightweight implementation of the client side
 *  <a href="http://pypi.python.org/pypi/Pyro4/">Pyro</a> protocol.
 *
 * {@link net.razorvine.pyro.Config} contains the (very few) static config items.
 * {@link net.razorvine.pyro.NameServerProxy} is a wrapper proxy to make it easier to talk to Pyro's name server.
 * {@link net.razorvine.pyro.PyroProxy} is the proxy class that is used to connect to remote Pyro objects and invoke methods on them.
 * {@link net.razorvine.pyro.PyroURI} is the URI class that is used to point at a specific object at a certain location.
 *
 * This package makes heavy use of the {@link net.razorvine.pickle} package to be able to
 * serialize and de-serialize object graphs, which is used in the Pyro communication protocol.
 * Note that Pyrolite only supports Pyro4.
 *
 * @author Irmen de Jong (irmen@razorvine.net)
 * @version 4.30
 * @see net.razorvine.pickle
 */
package net.razorvine.pyro;
