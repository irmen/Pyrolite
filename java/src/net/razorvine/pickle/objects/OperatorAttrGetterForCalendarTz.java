package net.razorvine.pickle.objects;

import java.util.Calendar;
import java.util.TimeZone;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;

/**
 * This object constructor is a minimalistic placeholder for operator.itemgetter,
 * it can only be used in the case of unpickling the special pickle created for
 * localizing datetimes with pytz timezones.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class OperatorAttrGetterForCalendarTz implements IObjectConstructor {
	
	public OperatorAttrGetterForCalendarTz() {
	}

	public Object construct(Object[] args) {
		if (args.length != 1)
			throw new PickleException("expected exactly one string argument for construction of AttrGetter");
		if ("localize".equals(args[0]))
			return new AttrGetterForTz();
		else
			throw new PickleException("expected 'localize' string argument for construction of AttrGetter");
	}
	
	class AttrGetterForTz implements IObjectConstructor
	{
		public AttrGetterForTz() {
		}

		public Object construct(Object[] args) {
			if (args.length != 1 || !(args[0] instanceof TimeZone))
				throw new PickleException("expected exactly one TimeZone argument for construction of CalendarLocalizer");
			
			TimeZone tz = (TimeZone) args[0];
			return new CalendarLocalizer(tz);
		}
	}

	class CalendarLocalizer implements IObjectConstructor
	{
		TimeZone tz;
		
		public CalendarLocalizer(TimeZone tz) {
			this.tz=tz;
		}

		public Object construct(Object[] args) {
			if (args.length != 1 || !(args[0] instanceof Calendar))
				throw new PickleException("expected exactly one Calendar argument for construction of Calendar with timezone");
			
			Calendar cal = (Calendar)args[0];
			cal.setTimeZone(tz);
			return cal;
		}
	}
}

