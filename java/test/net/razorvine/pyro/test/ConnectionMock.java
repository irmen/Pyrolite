package net.razorvine.pyro.test;

import java.io.IOException;
import java.io.InputStream;
import java.util.Arrays;

class ConnectionMock extends InputStream
{
	byte[] data;

	public ConnectionMock(byte[] data) {
		this.data=data;
	}

	public ConnectionMock() {
		this.data=new byte[0];
	}

	public void send(byte[] to_bytes) {
		this.data = to_bytes;
	}

	public byte[] ReceivedData() {
		return data;
	}

	public int RemainingLength() {
		return data.length;
	}

	@Override
	public int read() throws IOException {
		if(data.length==0)
			throw new IOException("eof");
		byte b = data[0];
		data = Arrays.copyOfRange(data,  1, data.length);
		return b;
	}
	
}
