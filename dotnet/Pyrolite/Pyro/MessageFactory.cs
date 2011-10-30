/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.IO;
using System.Security.Cryptography;

namespace Razorvine.Pyro
{

/// <summary>
/// Create and parse Pyro wire protocol messages.
/// </summary>
class MessageFactory
{
    public const short MSG_CONNECT = 1;
    public const short MSG_CONNECTOK = 2;
    public const short MSG_CONNECTFAIL  = 3;
    public const short MSG_INVOKE = 4;
    public const short MSG_RESULT = 5;
    public const short FLAGS_EXCEPTION = 1<<0;
    public const short FLAGS_COMPRESSED = 1<<1;
    public const short FLAGS_ONEWAY = 1<<2;
    public const short FLAGS_HMAC = 1<<3;
    public const short FLAGS_BATCH = 1<<4;
    public const int MAGIC = 0x34E9;
    static readonly byte[] EMPTY_BYTES = new byte[0];
    public const int PROTOCOL_VERSION=44;
    public const int HEADER_SIZE=38;
    static readonly byte[] EMPTY_HMAC=new byte[20];		// sha1=20 bytes
    
    /**
     * Create the header for a message.
     */
    public static byte[] createMsgHeader(int msgtype, byte[] data, int flags, short sequenceNr) {
    	byte[] bodyhmac;
    	if(data==null)
    		data=EMPTY_BYTES;
    	if(Config.HMAC_KEY!=null) {
    		flags|=FLAGS_HMAC;
			bodyhmac = makeHMAC(data);
    	} else {
    		bodyhmac=EMPTY_HMAC;
    	}
 
    	int headerchecksum=msgtype+PROTOCOL_VERSION+data.Length+flags+sequenceNr+MAGIC;
    	byte[] header=new byte[HEADER_SIZE];
    	
    	// headerFmt = '!4sHHHHiH20s'    # header (id, version, msgtype, flags, sequencenumber, dataLen, checksum, hmac)
    	header[0]=(byte)'P';
    	header[1]=(byte)'Y';
    	header[2]=(byte)'R';
    	header[3]=(byte)'O';
    	header[4]=(byte) (PROTOCOL_VERSION>>8);
    	header[5]=(byte) (PROTOCOL_VERSION&0xff);
    	header[6]=(byte) (msgtype>>8);
    	header[7]=(byte) (msgtype&0xff);
    	header[8]=(byte) (flags>>8);
    	header[9]=(byte) (flags&0xff);
    	header[10]=(byte)(sequenceNr>>8);
    	header[11]=(byte)(sequenceNr&0xff);
    	header[12]=(byte)((data.Length>>24)&0xff);
    	header[13]=(byte)((data.Length>>16)&0xff);
    	header[14]=(byte)((data.Length>>8)&0xff);
    	header[15]=(byte)(data.Length&0xff);
    	header[16]=(byte)(headerchecksum>>8);
    	header[17]=(byte)(headerchecksum&0xff);
    	Array.Copy(bodyhmac, 0, header, 18, bodyhmac.Length); //18..37=hmac (20 bytes)
    	return header;
    }

    /**
     * Calculate the SHA-1 HMAC of a piece of data.
     */
	public static byte[] makeHMAC(byte[] data) {
		using(HMACSHA1 hmac=new HMACSHA1(Config.HMAC_KEY)) {
			return hmac.ComputeHash(data);
		}
	}

	/**
	 * Receive a message from the connection. If you set requiredMsgType to the required
	 * message type id instead of zero, it will check the incoming message type and
	 * raise a PyroException if they don't match.
	 */
	public static Message getMessage(Stream connection, int requiredMsgType) {
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
			if(header.hmac!=makeHMAC(data)) {
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

	/**
	 * extract the header fields from the message header bytes.
	 */
    static MessageHeader parseMessageHeader(byte[] headerdata) {
    	if(headerdata==null||headerdata.Length!=HEADER_SIZE) {
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
    	Array.Copy(headerdata,18,header.hmac,0,20);
    	return header;
    }

	public static void TraceMessageSend(int sequenceNr, byte[] headerdata, byte[] data) {
		string filename=Path.Combine(Config.MSG_TRACE_DIR, string.Format("{0:D5}-a-send-header.dat", sequenceNr));
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(headerdata, 0, headerdata.Length);
		}
		filename=Path.Combine(Config.MSG_TRACE_DIR, string.Format("{0:D5}-a-send-message.dat", sequenceNr));
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(data, 0, data.Length);
		}
	}
	
	public static void TraceMessageRecv(int sequenceNr, byte[] headerdata, byte[] data) {
		string filename=Path.Combine(Config.MSG_TRACE_DIR, string.Format("{0:D5}-b-recv-header.dat", sequenceNr));
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(headerdata, 0, headerdata.Length);
		}
		filename=Path.Combine(Config.MSG_TRACE_DIR, string.Format("{0:D5}-b-recv-message.dat", sequenceNr));
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(data, 0, data.Length);
		}
	}
}

class Message {
	public int type;
	public int flags;
	public int sequence;
	public byte[] data;
}

struct MessageHeader {
	public int type;
	public int flags;
	public int sequence;
	public int datasize;
	public byte[] hmac;
}

}