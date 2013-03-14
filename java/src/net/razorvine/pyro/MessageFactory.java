package net.razorvine.pyro;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.security.InvalidKeyException;
import java.security.Key;
import java.security.NoSuchAlgorithmException;
import java.util.Arrays;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;

/**
 * Create and parse Pyro wire protocol messages.
 * 
 * @author Irmen de Jong (irmen@razorvine.net)
 */
public class MessageFactory
{
    public static short MSG_CONNECT = 1;
    public static short MSG_CONNECTOK = 2;
    public static short MSG_CONNECTFAIL  = 3;
    public static short MSG_INVOKE = 4;
    public static short MSG_RESULT = 5;
    public static short FLAGS_EXCEPTION = 1<<0;
    public static short FLAGS_COMPRESSED = 1<<1;
    public static short FLAGS_ONEWAY = 1<<2;
    public static short FLAGS_HMAC = 1<<3;
    public static short FLAGS_BATCH = 1<<4;
    static int MAGIC = 0x34E9;
    static byte[] EMPTY_BYTES = new byte[0];
    static final int PROTOCOL_VERSION=44;
    static final int HEADER_SIZE=38;
    static final byte[] EMPTY_HMAC=new byte[20];		// sha1=20 bytes
    
    /**
     * Create the header for a message.
     */
    public static byte[] createMsgHeader(int msgtype, byte[] data, int flags, int sequenceNr) {
    	byte[] bodyhmac;
    	if(data==null)
    		data=EMPTY_BYTES;
    	if(Config.HMAC_KEY!=null) {
    		flags|=FLAGS_HMAC;
			bodyhmac = makeHMAC(data);
    	} else {
    		bodyhmac=EMPTY_HMAC;
    	}
 
    	if(sequenceNr>0xffff) {
    		throw new IllegalArgumentException("sequenceNr must be 0-65535 (unsigned short)");
    	}
 
    	int headerchecksum=msgtype+PROTOCOL_VERSION+data.length+flags+sequenceNr+MAGIC;
    	byte[] header=new byte[HEADER_SIZE];
    	
    	// headerFmt = '!4sHHHHiH20s'    # header (id, version, msgtype, flags, sequencenumber, dataLen, checksum, hmac)
    	header[0]='P';
    	header[1]='Y';
    	header[2]='R';
    	header[3]='O';
    	header[4]=(byte) (PROTOCOL_VERSION>>8);
    	header[5]=(byte) (PROTOCOL_VERSION&0xff);
    	header[6]=(byte) (msgtype>>8);
    	header[7]=(byte) (msgtype&0xff);
    	header[8]=(byte) (flags>>8);
    	header[9]=(byte) (flags&0xff);
    	header[10]=(byte)(sequenceNr>>8);
    	header[11]=(byte)(sequenceNr&0xff);
    	header[12]=(byte)((data.length>>24)&0xff);
    	header[13]=(byte)((data.length>>16)&0xff);
    	header[14]=(byte)((data.length>>8)&0xff);
    	header[15]=(byte)(data.length&0xff);
    	header[16]=(byte)(headerchecksum>>8);
    	header[17]=(byte)(headerchecksum&0xff);
    	System.arraycopy(bodyhmac, 0, header, 18, bodyhmac.length);    	//18..37=hmac (20 bytes)
    	return header;
    }

    /**
     * Calculate the SHA-1 HMAC of a piece of data.
     */
	public static byte[] makeHMAC(byte[] data) throws PyroException {
		try {
			Key key = new SecretKeySpec(Config.HMAC_KEY, "HmacSHA1");
			Mac hmac_algo = Mac.getInstance("HmacSHA1");
		    hmac_algo.init(key);
		    return hmac_algo.doFinal(data);
		} catch (NoSuchAlgorithmException e) {
			throw new PyroException("invalid hmac algorithm",e);
		} catch (InvalidKeyException e) {
			throw new PyroException("invalid hmac key",e);
		}
	}

	/**
	 * Receive a message from the connection. If you set requiredMsgType to the required
	 * message type id instead of zero, it will check the incoming message type and
	 * raise a PyroException if they don't match.
	 */
	public static Message getMessage(InputStream connection, int requiredMsgType) throws PyroException, IOException {
		byte[] headerdata=IOUtil.recv(connection, HEADER_SIZE);
		MessageHeader header=parseMessageHeader(headerdata);
		if(requiredMsgType!=0 && header.type!=requiredMsgType) {
			throw new PyroException("invalid msg type received: "+header.type);
		}
		byte[] data=IOUtil.recv(connection, header.datasize);
		if(Config.MSG_TRACE_DIR!=null) {
			TraceMessageRecv(header.sequence, headerdata, data);
		}
		if(((header.flags&FLAGS_HMAC) != 0) && (Config.HMAC_KEY!=null)) {
			if(!Arrays.equals(header.hmac, makeHMAC(data))) {
				throw new PyroException("message hmac mismatch");
			}
		} else if(((header.flags&FLAGS_HMAC) != 0) != (Config.HMAC_KEY!=null)) {
			throw new PyroException("hmac key config not symmetric");
		}
		
		Message msg=new Message();
		msg.type=header.type;
		msg.flags=header.flags;
		msg.sequence=header.sequence;
		msg.data=data;
		return msg;
	}

	public static void TraceMessageSend(int sequenceNr, byte[] headerdata, byte[] data) throws IOException {
		String filename=String.format("%s%s%05d-a-send-header.dat", Config.MSG_TRACE_DIR, File.separator, sequenceNr);
		FileOutputStream fos=new FileOutputStream(filename);
		fos.write(headerdata);	
		fos.close();
		filename=String.format("%s%s%05d-a-send-message.dat", Config.MSG_TRACE_DIR, File.separator, sequenceNr);
		fos=new FileOutputStream(filename);
		fos.write(data);
		fos.close();
	}
	
	public static void TraceMessageRecv(int sequenceNr, byte[] headerdata, byte[] data) throws IOException {
		String filename=String.format("%s%s%05d-b-recv-header.dat", Config.MSG_TRACE_DIR, File.separator, sequenceNr);
		FileOutputStream fos=new FileOutputStream(filename);	
		fos.write(headerdata);
		fos.close();
		filename=String.format("%s%s%05d-b-recv-message.dat", Config.MSG_TRACE_DIR, File.separator, sequenceNr);
		fos=new FileOutputStream(filename);
		fos.write(data);
		fos.close();
	}

	/**
	 * extract the header fields from the message header bytes.
	 */
    public static MessageHeader parseMessageHeader(byte[] headerdata) {
    	if(headerdata==null||headerdata.length!=HEADER_SIZE) {
    		throw new PyroException("msg header data size mismatch");
    	}
    	
    	int version=(headerdata[4]<<8)|headerdata[5];
    	if(headerdata[0]!='P'||headerdata[1]!='Y'||headerdata[2]!='R'||headerdata[3]!='O'||version!=PROTOCOL_VERSION) {
    		throw new PyroException("invalid msg or unsupported protocol version");    		
    	}

    	MessageHeader header=new MessageHeader();
    	header.type=headerdata[6]&0xff;
    	header.type<<=8;
    	header.type|=headerdata[7]&0xff;
    	header.flags=headerdata[8]&0xff;
    	header.flags<<=8;
    	header.flags|=headerdata[9]&0xff;
    	header.sequence=headerdata[10]&0xff;
    	header.sequence<<=8;
    	header.sequence|=headerdata[11]&0xff;
    	header.datasize=headerdata[12]&0xff;
    	header.datasize<<=8;
    	header.datasize|=headerdata[13]&0xff;
    	header.datasize<<=8;
    	header.datasize|=headerdata[14]&0xff;
    	header.datasize<<=8;
    	header.datasize|=headerdata[15]&0xff;
    	int currentchecksum=(header.type+version+header.datasize+header.flags+header.sequence+MAGIC)&0xffff;
    	int headerchecksum=headerdata[16]&0xff;
    	headerchecksum<<=8;
    	headerchecksum|=headerdata[17]&0xff;
    	if(currentchecksum!=headerchecksum) {
    		throw new PyroException("msg header checksum mismatch");
    	}
  
    	header.hmac=new byte[20];
    	System.arraycopy(headerdata, 18, header.hmac, 0, 20);
    	return header;
    }
}
