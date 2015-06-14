/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Razorvine.Pyro
{

/// <summary>
/// Pyro wire protocol message.
/// </summary>
public class Message
{
	private const int CHECKSUM_MAGIC = 0x34E9;
	public const int HEADER_SIZE = 24;

	public const ushort MSG_CONNECT = 1;
	public const ushort MSG_CONNECTOK = 2;
	public const ushort MSG_CONNECTFAIL = 3;
	public const ushort MSG_INVOKE = 4;
	public const ushort MSG_RESULT = 5;
	public const ushort MSG_PING = 6;
	public const ushort FLAGS_EXCEPTION = 1<<0;
	public const ushort FLAGS_COMPRESSED = 1<<1;
	public const ushort FLAGS_ONEWAY = 1<<2;
	public const ushort FLAGS_BATCH = 1<<3;
	public const ushort FLAGS_META_ON_CONNECT = 1<<4;
	public const ushort SERIALIZER_SERPENT = 1;
	public const ushort SERIALIZER_JSON = 2;
	public const ushort SERIALIZER_MARSHAL = 3;
	public const ushort SERIALIZER_PICKLE = 4;
	
	public ushort type;
	public ushort flags;
	public byte[] data;
	public int data_size;
	public ushort annotations_size;
	public ushort serializer_id;
	public ushort seq;
	public IDictionary<string, byte[]> annotations;
	
	/// <summary>
	/// construct a header-type message, without data and annotations payload.
	/// </summary>
	public Message(ushort msgType, ushort serializer_id, ushort flags, ushort seq)
	{
		this.type = msgType;
		this.flags = flags;
		this.seq = seq;
		this.serializer_id = serializer_id;
	}

	/// <summary>
	/// construct a full wire message including data and annotations payloads.
	/// </summary>
	public Message(ushort msgType, byte[] databytes, ushort serializer_id, ushort flags, ushort seq, IDictionary<string, byte[]> annotations, byte[] hmac)
		:this(msgType, serializer_id, flags, seq)
	{
		this.data = databytes;
		this.data_size = databytes.Length;
		this.annotations = annotations;
		if(null==annotations)
			this.annotations = new Dictionary<string, byte[]>(0);
		
		if(hmac!=null)
			this.annotations["HMAC"] = this.hmac(hmac);
		
		this.annotations_size = (ushort) this.annotations.Sum(a=>a.Value.Length+6);
	}
	
	/// <summary>
	/// returns the hmac of the data and the annotation chunk values (except HMAC chunk itself)
	/// </summary>
	public byte[] hmac(byte[] key)
	{
		using(HMACSHA1 hmac=new HMACSHA1(key)) {
			hmac.TransformBlock(this.data, 0, this.data.Length, this.data, 0);
			foreach(var e in this.annotations.OrderBy(a=>a.Key))
			{
				if(e.Key!="HMAC")
					hmac.TransformBlock(e.Value, 0, e.Value.Length, e.Value, 0);
			}
			hmac.TransformFinalBlock(this.data, 0, 0);
			return hmac.Hash;
		}
	}

	/// <summary>
	/// creates a byte stream containing the header followed by annotations (if any) followed by the data
	/// </summary>
	public byte[] to_bytes()
	{
		byte[] header_bytes = get_header_bytes();
		byte[] annotations_bytes = get_annotations_bytes();
		byte[] result = new byte[header_bytes.Length + annotations_bytes.Length + data.Length];
		Array.Copy(header_bytes, result, header_bytes.Length);
		Array.Copy(annotations_bytes, 0, result, header_bytes.Length, annotations_bytes.Length);
		Array.Copy(data, 0, result, header_bytes.Length+annotations_bytes.Length, data.Length);
		return result;
	}

	public byte[] get_header_bytes()
	{
		int checksum = (type+Config.PROTOCOL_VERSION+data_size+annotations_size+serializer_id+flags+seq+CHECKSUM_MAGIC)&0xffff;
		byte[] header = new byte[HEADER_SIZE];
		/*
		 header format: '!4sHHHHiHHHH' (24 bytes)
		   4   id ('PYRO')
		   2   protocol version
		   2   message type
		   2   message flags
		   2   sequence number
		   4   data length
		   2   data serialization format (serializer id)
		   2   annotations length (total of all chunks, 0 if no annotation chunks present)
		   2   (reserved)
		   2   checksum
		   followed by annotations: 4 bytes type, annotations bytes.
		*/

		header[0]=(byte)'P';
		header[1]=(byte)'Y';
		header[2]=(byte)'R';
		header[3]=(byte)'O';

		header[4]=(byte) (Config.PROTOCOL_VERSION>>8);
		header[5]=(byte) (Config.PROTOCOL_VERSION&0xff);

		header[6]=(byte) (type>>8);
		header[7]=(byte) (type&0xff);

		header[8]=(byte) (flags>>8);
		header[9]=(byte) (flags&0xff);

		header[10]=(byte)(seq>>8);
		header[11]=(byte)(seq&0xff);

		header[12]=(byte)((data_size>>24)&0xff);
		header[13]=(byte)((data_size>>16)&0xff);
		header[14]=(byte)((data_size>>8)&0xff);
		header[15]=(byte)(data_size&0xff);

		header[16]=(byte)(serializer_id>>8);
		header[17]=(byte)(serializer_id&0xff);

		header[18]=(byte)((annotations_size>>8)&0xff);
		header[19]=(byte)(annotations_size&0xff);
		
		header[20]=0; // reserved
		header[21]=0; // reserved
		
		header[22]=(byte)((checksum>>8)&0xff);
		header[23]=(byte)(checksum&0xff);

		return header;
	}
	
	public byte[] get_annotations_bytes()
	{
		IEnumerable<byte> result = new byte[0];
		foreach(var ann in annotations)
		{
			if(ann.Key.Length!=4)
				throw new ArgumentException("annotation key must be length 4");
			result = result.Concat(Encoding.ASCII.GetBytes(ann.Key));
			byte[] size_bytes = new byte[2] { (byte)((ann.Value.Length>>8)&0xff), (byte)(ann.Value.Length&0xff) };
			result = result.Concat(size_bytes);
			result = result.Concat(ann.Value);
		}
		return result.ToArray();
	}


	/// <summary>
	/// Parses a message header. Does not yet process the annotations chunks and message data.
	/// </summary>
	public static Message from_header(byte[] header)
	{
		if(header==null || header.Length!=HEADER_SIZE)
			throw new PyroException("header data size mismatch");
		
		if(header[0]!='P'||header[1]!='Y'||header[2]!='R'||header[3]!='O') 
			throw new PyroException("invalid message");    		

		int version = (header[4] << 8)|header[5];
		if(version!=Config.PROTOCOL_VERSION)
			throw new PyroException("invalid protocol version: "+version);
		
		int msg_type = (header[6] << 8)|header[7];
		int flags = (header[8] << 8)|header[9];
		int seq = (header[10] << 8)|header[11];
		int data_size=(((((header[12] <<8) | header[13]) <<8) | header[14]) <<8) | header[15];
		int serializer_id = (header[16] << 8)|header[17];
		int annotations_size = (header[18]<<8)|header[19];
		// byte 20 and 21 are reserved.
		int checksum = (header[22]<<8)|header[23];
		if(checksum!=((msg_type+version+data_size+annotations_size+flags+serializer_id+seq+CHECKSUM_MAGIC)&0xffff))
			throw new PyroException("header checksum mismatch");
		
		var msg = new Message((ushort)msg_type, (ushort)serializer_id, (ushort)flags, (ushort)seq);
		msg.data_size = data_size;
		msg.annotations_size = (ushort)annotations_size;
		return msg;
	}

	
	// Note: this 'chunked' way of sending is not used because it triggers Nagle's algorithm
	// on some systems (linux). This causes massive delays, unless you change the socket option
	// TCP_NODELAY to disable the algorithm. What also works, is sending all the message bytes
	// in one go: connection.send(message.to_bytes())
//    public void send(Stream connection)
//    {
//    	// send the message as bytes over the connection
//    	IOUtil.send(connection, get_header_bytes());
//    	if(annotations_size>0)
//    		IOUtil.send(connection, get_annotations_bytes());
//    	IOUtil.send(connection, data);
//	}
	
	
	/// <summary>
	/// Receives a pyro message from a given connection.
	/// Accepts the given message types (None=any, or pass a sequence).
	/// Also reads annotation chunks and the actual payload data.
	/// Validates a HMAC chunk if present.
	/// </summary>
	public static Message recv(Stream connection, ushort[] requiredMsgTypes, byte[] hmac)
	{
		byte[] header_data = IOUtil.recv(connection, HEADER_SIZE);
		var msg = from_header(header_data);
		if(requiredMsgTypes!=null && !requiredMsgTypes.Contains(msg.type))
			throw new PyroException(string.Format("invalid msg type {0} received", msg.type));

		byte[] annotations_data = null;
		msg.annotations = new Dictionary<string, byte[]>();
		if(msg.annotations_size>0)
		{
			// read annotation chunks
			annotations_data = IOUtil.recv(connection, msg.annotations_size);
			int i=0;
			while(i<msg.annotations_size)
			{
				string anno = Encoding.ASCII.GetString(annotations_data, i, 4);
				int length = (annotations_data[i+4]<<8) | annotations_data[i+5];
				byte[] annotations_bytes = new byte[length];
				Array.Copy(annotations_data, i+6, annotations_bytes, 0, length);
				msg.annotations[anno] = annotations_bytes;
				i += 6+length;
			}
		}
		
		// read data
		msg.data = IOUtil.recv(connection, msg.data_size);
				
		if(Config.MSG_TRACE_DIR!=null) {
			TraceMessageRecv(msg.seq, header_data, annotations_data, msg.data);
		}
		
		if(msg.annotations.ContainsKey("HMAC") && hmac!=null)
		{
			if(!msg.annotations["HMAC"].SequenceEqual<byte>(msg.hmac(hmac)))
				throw new PyroException("message hmac mismatch");
		}
		else if (msg.annotations.ContainsKey("HMAC") != (hmac!=null))
		{
			// Message contains hmac and local HMAC_KEY not set, or vice versa. This is not allowed.
			throw new PyroException("hmac key config not symmetric");
		}
		return msg;
	}

	public static void TraceMessageRecv(int sequenceNr, byte[] headerdata, byte[] annotations, byte[] data) {
		string filename=Path.Combine(Config.MSG_TRACE_DIR, string.Format("{0:D5}-b-recv-header.dat", sequenceNr));
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(headerdata, 0, headerdata.Length);
			if(annotations!=null)
				fos.Write(annotations, 0, annotations.Length);
		}
		filename=Path.Combine(Config.MSG_TRACE_DIR, string.Format("{0:D5}-b-recv-message.dat", sequenceNr));
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(data, 0, data.Length);
		}
	}

	public static void TraceMessageSend(int sequenceNr, byte[] headerdata, byte[] annotations, byte[] data) {
		string filename=Path.Combine(Config.MSG_TRACE_DIR, string.Format("{0:D5}-a-send-header.dat", sequenceNr));
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(headerdata, 0, headerdata.Length);
			if(annotations!=null)
				fos.Write(annotations, 0, annotations.Length);
		}
		filename=Path.Combine(Config.MSG_TRACE_DIR, string.Format("{0:D5}-a-send-message.dat", sequenceNr));
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(data, 0, data.Length);
		}
	}

}
}
