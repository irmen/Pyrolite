package net.razorvine.pyro;

public class MessageHeader {
	public int type;
	public int flags;
	public int sequence;
	public int datasize;
	public byte[] hmac;
}
