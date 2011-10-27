package net.razorvine.pickle.objects;

import java.io.Serializable;
import java.text.NumberFormat;
import java.util.Locale;

/**
 * Helper class to mimic the datetime.timedelta Python object (holds a days/seconds/microsec time difference).
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class TimeDelta implements Serializable {
	private static final long serialVersionUID = 5793744158850914421L;
	public int days;
	public int seconds;
	public int microseconds;
	public double total_seconds;

	public TimeDelta(int days, int seconds, int microseconds) {
		this.days = days;
		this.seconds = seconds;
		this.microseconds = microseconds;
		this.total_seconds = days*86400+seconds+microseconds/1000000.0;
	}
	
	public String toString() {
		NumberFormat nf=NumberFormat.getInstance(Locale.UK);
		nf.setGroupingUsed(false);
		nf.setMaximumFractionDigits(6);
		String floatsecs=nf.format(total_seconds);
		return String.format("Timedelta: %d days, %d seconds, %d microseconds (total: %s seconds)", days, seconds, microseconds, floatsecs);
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + days;
		result = prime * result + microseconds;
		result = prime * result + seconds;
		long temp=Double.doubleToLongBits(total_seconds);
		result = prime * result + (int) (temp ^ (temp >>> 32));
		return result;
	}

	@Override
	public boolean equals(Object obj) {
		if (this == obj)
			return true;
		if (obj == null)
			return false;
		if (!(obj instanceof TimeDelta))
			return false;
		TimeDelta other = (TimeDelta) obj;
		return days==other.days && seconds==other.seconds && microseconds==other.microseconds && total_seconds==other.total_seconds;
	}
	
	
}
