package net.razorvine.pickle.objects;

import java.io.Serializable;

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
	
	public String toString() {
		return String.format("Time: %d hours, %d minutes, %d seconds, %d microseconds", hours, minutes, seconds, microseconds);
	}
}
