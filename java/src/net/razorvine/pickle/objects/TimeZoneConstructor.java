package net.razorvine.pickle.objects;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.objects.Tzinfo;
import java.util.TimeZone;

public class TimeZoneConstructor implements IObjectConstructor {
	public static int UTC = 1;
	public static int PYTZ = 2;
	public static int DATEUTIL_TZUTC = 3;
	public static int TZINFO = 4;

	private int pythontype;

	public TimeZoneConstructor(int pythontype) {
		this.pythontype = pythontype;
	}
	
	@Override
	public Object construct(Object[] args) throws PickleException {
	if (this.pythontype == UTC)
		return createUTC();
	if (this.pythontype == PYTZ)
		return createZoneFromPytz(args);
	if (this.pythontype == DATEUTIL_TZUTC)
		return createInfoFromDateutilTzutc(args);
	if (this.pythontype == TZINFO)
		return createInfo(args);

	throw new PickleException("invalid object type");
	}

	public Object reconstruct(Object baseConstructor, Object state) {
		if (!(state instanceof Tzinfo))
			throw new PickleException("invalid pickle data for tzinfo reconstruction; expected emtpy tzinfo state class");
		if (!(baseConstructor instanceof TimeZoneConstructor))
			throw new PickleException("invalid pickle data for tzinfo reconstruction; expected a TimeZoneConstructor from a known tzinfo subclass");

		// The subclass (this) is reconstructing the state given the base class and state. If it is known that the
		// subclass is always UTC, ie dateutil.tz.tzutc, then we can just return the timezone we know matches that.
		if (this.pythontype == DATEUTIL_TZUTC) {
			return TimeZone.getTimeZone("UTC");
		} else {
			throw new PickleException("unsupported pickle data for tzinfo reconstruction; support for tzinfo subclasses other than tztuc has not been implemented");
		}
	}

	private Object createInfo(Object[] args) {
		// args is empty, datetime.tzinfo objects are unpickled via setstate, so return an object which is ready to have it's state set
		return new Tzinfo();
	}

	private Object createInfoFromDateutilTzutc(Object[] args) {
		// In the case of the dateutil.tz.tzutc constructor, which is a python subclass of the datetime.tzinfo class, there is no state
		// to set, because the zone is implied by the constructor. Pass the timezone indicated by hte constructor here
		return new Tzinfo(TimeZone.getTimeZone("UTC"));
	}

	private Object createZoneFromPytz(Object[] args) {

		if (args.length != 4 && args.length != 1)
			throw new PickleException("invalid pickle data for pytz timezone; expected 1 or 4 args, got " + args.length);

		// args can be a tuple of 4 values: string timezone identifier, int seconds from utc offset, int seconds for DST, string python timezone name
		// if args came from a pytz.DstTzInfo object
		// Or, args is a tuple of 1 value: string timezone identifier
		// if args came from a pytz.StaticTzInfo object
		// In both cases we can ask the system for a timezone with that identifier and it should find one with that identifier if python did.

		if (!(args[0] instanceof String))
			throw new PickleException("invalid pickle data for pytz timezone; expected string argument as first tuple member");
		return TimeZone.getTimeZone((String) args[0]);
	}

	private Object createUTC() {
		return TimeZone.getTimeZone("UTC");
	}
}
