package net.razorvine.pickle.objects;

import java.io.Serializable;
import java.util.Calendar;

/**
 * Helper class to mimic the datetime.time Python object (holds a hours/minute/seconds/microsecond time).
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class Time implements Serializable {
	private static final long serialVersionUID = 7048987424134614062L;
	public int hours;
	public int minutes;
	public int seconds;
	public int microseconds;

	public Time(int h, int m, int s, int microsecs) {
		hours = h;
		minutes = m;
		seconds = s;
		microseconds = microsecs;
	}
	
	public Time(long milliseconds) {
		Calendar cal = Calendar.getInstance();
		cal.setTimeInMillis(milliseconds);
		this.hours = cal.get(Calendar.HOUR_OF_DAY);
		this.minutes = cal.get(Calendar.MINUTE);
		this.seconds = cal.get(Calendar.SECOND);
		this.microseconds = cal.get(Calendar.MILLISECOND) * 1000;
	}
	
	public String toString() {
		return String.format("Time: %d hours, %d minutes, %d seconds, %d microseconds", hours, minutes, seconds, microseconds);
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + hours;
		result = prime * result + microseconds;
		result = prime * result + minutes;
		result = prime * result + seconds;
		return result;
	}

	@Override
	public boolean equals(Object obj) {
		if (this == obj)
			return true;
		if (obj == null)
			return false;
		if (!(obj instanceof Time))
			return false;
		Time other = (Time) obj;
		return hours==other.hours && minutes==other.minutes && seconds==other.seconds && microseconds==other.microseconds;
	}

	
}
