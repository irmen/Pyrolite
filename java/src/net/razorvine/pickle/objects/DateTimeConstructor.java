package net.razorvine.pickle.objects;

import java.util.Calendar;
import java.util.GregorianCalendar;
import java.util.TimeZone;

import net.razorvine.pickle.IObjectConstructor;
import net.razorvine.pickle.PickleException;

/**
 * This constructor can create various datetime related objects.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class DateTimeConstructor implements IObjectConstructor {
	public static int DATETIME = 1;
	public static int DATE = 2;
	public static int TIME = 3;
	public static int TIMEDELTA = 4;

	private int pythontype;

	public DateTimeConstructor(int pythontype) {
		this.pythontype = pythontype;
	}

	public Object construct(Object[] args) {
		if (this.pythontype == DATE)
			return createDate(args);
		if (this.pythontype == TIME)
			return createTime(args);
		if (this.pythontype == DATETIME)
			return createDateTime(args);
		if (this.pythontype == TIMEDELTA)
			return createTimedelta(args);
		throw new PickleException("invalid object type");
	}

	private TimeDelta createTimedelta(Object[] args) {
		// python datetime.timedelta -> dt.TimeDelta
		// args is a tuple of 3 ints: days,seconds,microseconds
		if (args.length != 3)
			throw new PickleException("invalid pickle data for timedelta; expected 3 args, got "+args.length);
		int days=((Number)args[0]).intValue();
		int seconds=((Number)args[1]).intValue();
		int micro=((Number)args[2]).intValue();
		return new TimeDelta(days, seconds, micro);
	}

	private Calendar createDateTime(Object[] args) {
		// python datetime.time --> GregorianCalendar
		// args is 10 bytes: yhi, ylo, month, day, hour, minute, second, ms1, ms2, ms3
		// (can be String or byte[])
		// alternate constructor is with 7/8 integer arguments: year, month, day, hour, minute, second, microseconds, [timezone]
		if(args.length==7 || args.length==8)
		{
			int year = (Integer)args[0];
			int month = ((Integer)args[1]) - 1;   // in java month starts with 0...
			int day = (Integer)args[2];
			int hour = (Integer)args[3];
			int minute = (Integer)args[4];
			int second = (Integer)args[5];
			int microsec = (Integer)args[6];
			TimeZone tz = null;
			if(args.length==8)
				tz = (TimeZone) args[7];
			
			Calendar cal = new GregorianCalendar(year, month, day, hour, minute, second);
			cal.set(Calendar.MILLISECOND, microsec/1000);
			if(tz!=null)
				cal.setTimeZone(tz);
			return cal;
		}
		if (args.length != 1 && args.length != 2)
			throw new PickleException("invalid pickle data for datetime; expected 1, 2, 7 or 8 args, got "+args.length);
		
		int yhi, ylo, month, day, hour, minute, second, microsec;
		if(args[0] instanceof String) {
			String params = (String) args[0];
			if (params.length() != 10)
				throw new PickleException("invalid pickle data for datetime; expected arg of length 10, got length "+params.length());
			yhi = params.charAt(0);
			ylo = params.charAt(1);
			month = params.charAt(2) - 1; // blargh: months start at 0 in Java
			day = params.charAt(3);
			hour = params.charAt(4);
			minute = params.charAt(5);
			second = params.charAt(6);
			int ms1 = params.charAt(7);
			int ms2 = params.charAt(8);
			int ms3 = params.charAt(9);
			microsec = ((ms1 << 8) | ms2) << 8 | ms3;
		} else {
			byte[] params=(byte[])args[0];
			if (params.length != 10)
				throw new PickleException("invalid pickle data for datetime; expected arg of length 10, got length "+params.length);
			yhi=params[0]&0xff;
			ylo=params[1]&0xff;
			month=(params[2]&0xff) - 1; // blargh: months start at 0 in java
			day=params[3]&0xff;
			hour=params[4]&0xff;
			minute=params[5]&0xff;
			second=params[6]&0xff;
			int ms1 = params[7]&0xff;
			int ms2 = params[8]&0xff;
			int ms3 = params[9]&0xff;
			microsec = ((ms1 << 8) | ms2) << 8 | ms3;
		}
		Calendar cal = new GregorianCalendar(yhi * 256 + ylo, month, day, hour, minute, second);
		cal.set(Calendar.MILLISECOND, microsec/1000);
		if(args.length == 2) {
			// Timezone passed as the second constructor arg in pickle protocal 0
			if (args[1] instanceof TimeZone)
				cal.setTimeZone((TimeZone) args[1]);
			else if (args[1] instanceof Tzinfo) {
				cal.setTimeZone(((Tzinfo) args[1]).getTimeZone());
			} else {
				throw new PickleException("invalid pickle data for datetime; expected arg 2 to be a Tzinfo or TimeZone");
			}
		}
		return cal;
	}

	private Time createTime(Object[] args) {
		// python datetime.time --> Time object
		// args is 6 bytes: hour, minute, second, ms1,ms2,ms3  (String or byte[])
		// alternate constructor passes 4 integers args: hour, minute, second, microsecond)
		if (args.length==4)
		{
			int hour = (Integer) args[0];
			int minute = (Integer) args[1];
			int second = (Integer) args[2];
			int microsec = (Integer) args[3];
			return new Time(hour, minute, second, microsec);
		}
		if (args.length != 1)
			throw new PickleException("invalid pickle data for time; expected 1 or 4 args, got "+args.length);
		int hour, minute, second, microsec;
		if(args[0] instanceof String) {
			String params = (String) args[0];
			if (params.length() != 6)
				throw new PickleException("invalid pickle data for time; expected arg of length 6, got length "+params.length());
			hour = params.charAt(0);
			minute = params.charAt(1);
			second = params.charAt(2);
			int ms1 = params.charAt(3);
			int ms2 = params.charAt(4);
			int ms3 = params.charAt(5);
			microsec = ((ms1 << 8) | ms2) << 8 | ms3;
		} else {
			byte[] params=(byte[])args[0];
			if (params.length != 6)
				throw new PickleException("invalid pickle data for datetime; expected arg of length 6, got length "+params.length);
			hour=params[0]&0xff;
			minute=params[1]&0xff;
			second=params[2]&0xff;
			int ms1 = params[3]&0xff;
			int ms2 = params[4]&0xff;
			int ms3 = params[5]&0xff;
			microsec = ((ms1 << 8) | ms2) << 8 | ms3;
		}
		return new Time(hour, minute, second, microsec);
	}

	private Calendar createDate(Object[] args) {
		// python datetime.date --> GregorianCalendar
		// args is a string of 4 bytes yhi, ylo, month, day (String or byte[])
		// alternatively, args is 3 values year, month, day
		if (args.length==3) {
			int year = (Integer) args[0];
			int month = (Integer) args[1];
			int day = (Integer) args[2];
			return new GregorianCalendar(year, month, day);
 		}
		if (args.length != 1)
			throw new PickleException("invalid pickle data for date; expected 1 arg, got "+args.length);
		int yhi, ylo, month, day;
		if(args[0] instanceof String) {
			String params = (String) args[0];
			if (params.length() != 4)
				throw new PickleException("invalid pickle data for date; expected arg of length 4, got length "+params.length());
			yhi = params.charAt(0);
			ylo = params.charAt(1);
			month = params.charAt(2) - 1; // blargh: months start at 0 in Java
			day = params.charAt(3);
		} else {
			byte[] params=(byte[])args[0];
			if (params.length != 4)
				throw new PickleException("invalid pickle data for date; expected arg of length 4, got length "+params.length);
			yhi=params[0]&0xff;
			ylo=params[1]&0xff;
			month=(params[2]&0xff) - 1; // blargh: months start at 0 in java
			day=params[3]&0xff;
		}
		return new GregorianCalendar(yhi * 256 + ylo, month, day);
	}
}