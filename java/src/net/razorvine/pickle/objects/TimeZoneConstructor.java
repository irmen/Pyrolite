package net.razorvine.pickle.objects;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;
import net.razorvine.pickle.objects.Tzinfo;
import java.util.TimeZone;

public class TimeZoneConstructor implements IObjectConstructor {
	public static int UTC = 1;
	public static int PYTZ = 2;

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

	throw new PickleException("invalid object type");
	}

	private Object createZoneFromPytz(Object[] args) {

		if (args.length != 4 && args.length != 1)
			throw new PickleException("invalid pickle data for pytz timezone; expected 2 or 4 args, got " + args.length);

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
