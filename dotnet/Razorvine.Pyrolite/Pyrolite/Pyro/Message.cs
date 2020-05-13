/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Razorvine.Pyro
{

/// <summary>
/// Pyro wire protocol message.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Message
{
	private const ushort MAGIC_NUMBER = 0x4dc5;
	public const int HEADER_SIZE = 40;

	public const byte  MSG_CONNECT = 1;
	public const byte  MSG_CONNECTOK = 2;
	public const byte  MSG_CONNECTFAIL = 3;
	public const byte  MSG_INVOKE = 4;
	public const byte  MSG_RESULT = 5;
	public const byte  MSG_PING = 6;
	public const ushort FLAGS_EXCEPTION = 1<<0;
	public const ushort FLAGS_COMPRESSED = 1<<1;
	public const ushort FLAGS_ONEWAY = 1<<2;
	public const ushort FLAGS_BATCH = 1<<3;
	public const ushort FLAGS_ITEMSTREAMRESULT = 1<<4;
	public const ushort FLAGS_KEEPSERIALIZED = 1 << 5;
	public const ushort FLAGS_CORR_ID = 1 << 6;
	public const byte SERIALIZER_SERPENT = 1;
	public const byte SERIALIZER_MARSHAL = 2;
	public const byte SERIALIZER_JSON = 3;
	public const byte SERIALIZER_MSGPACK = 4;
	
	public byte type;
	public ushort flags;
	public byte[] data;
	public int data_size;
	public ushort annotations_size;
	public Guid? correlation_id;
	public readonly byte serializer_id;
	public readonly ushort seq;
	public IDictionary<string, byte[]> annotations;
	
	/// <summary>
	/// construct a header-type message, without data and annotations payload.
	/// </summary>
	public Message(byte msgType, byte serializer_id, ushort flags, ushort seq, Guid? correlation_id)
	{
		type = msgType;
		this.flags = flags;
		this.seq = seq;
		this.serializer_id = serializer_id;
		this.correlation_id = correlation_id;
	}

	/// <summary>
	/// construct a full wire message including data and annotations payloads.
	/// </summary>
	public Message(byte msgType, byte[] databytes, byte serializer_id, ushort flags, ushort seq, IDictionary<string, byte[]> annotations, Guid? correlation_id)
		:this(msgType, serializer_id, flags, seq, correlation_id)
	{
		data = databytes;
		data_size = databytes.Length;
		this.annotations = annotations;
		if(null==annotations)
			this.annotations = new Dictionary<string, byte[]>(0);
		
		annotations_size = (ushort) this.annotations.Sum(a=>a.Value.Length+8);
	}
	
	/// <summary>
	/// creates a byte stream containing the header followed by annotations (if any) followed by the data
	/// </summary>
	public byte[] to_bytes()
	{
		var header_bytes = get_header_bytes();
		var annotations_bytes = get_annotations_bytes();
		var result = new byte[header_bytes.Length + annotations_bytes.Length + data.Length];
		Array.Copy(header_bytes, result, header_bytes.Length);
		Array.Copy(annotations_bytes, 0, result, header_bytes.Length, annotations_bytes.Length);
		Array.Copy(data, 0, result, header_bytes.Length+annotations_bytes.Length, data.Length);
		return result;
	}

	public byte[] get_header_bytes()
	{
		var header = new byte[HEADER_SIZE];
		/*
The header format is::

0x00   4s  4   'PYRO' (message identifier)
0x04   H   2   protocol version
0x06   B   1   message type
0x07   B   1   serializer id
0x08   H   2   message flags
0x0a   H   2   sequence number   (to identify proper request-reply sequencing)
0x0c   I   4   data length   (max 4 Gb)
0x10   I   4   annotations length (max 4 Gb, total of all chunks, 0 if no annotation chunks present)
0x14   16s 16  correlation uuid
0x24   H   2   (reserved)
0x26   H   2   magic number 0x4dc5
total size: 0x28 (40 bytes)

After the header, zero or more annotation chunks may follow, of the format::

   4s  4   annotation id (4 ASCII letters)
   I   4   chunk length  (max 4 Gb)
   B   x   annotation chunk databytes

After that, the actual payload data bytes follow.
		*/

		header[0]=(byte)'P';
		header[1]=(byte)'Y';
		header[2]=(byte)'R';
		header[3]=(byte)'O';

		header[4]=Config.PROTOCOL_VERSION>>8;
		header[5]=Config.PROTOCOL_VERSION&0xff;

		header[6] = type;
		header[7] = serializer_id;

		header[8]=(byte) (flags>>8);
		header[9]=(byte) (flags&0xff);

		header[10]=(byte)(seq>>8);
		header[11]=(byte)(seq&0xff);

		header[12]=(byte)((data_size>>24)&0xff);
		header[13]=(byte)((data_size>>16)&0xff);
		header[14]=(byte)((data_size>>8)&0xff);
		header[15]=(byte)(data_size&0xff);

		header[16]=(byte)((annotations_size>>24)&0xff);
		header[17]=(byte)((annotations_size>>16)&0xff);
		header[18]=(byte)((annotations_size>>8)&0xff);
		header[19]=(byte)(annotations_size&0xff);

		if (correlation_id.HasValue)
		{
			var bytes = correlation_id.Value.ToByteArray();
			// the bytes are mixed up, store in correct order
			header[20] = bytes[3];
			header[21] = bytes[2];
			header[22] = bytes[1];
			header[23] = bytes[0];
			header[24] = bytes[5];
			header[25] = bytes[4];
			header[26] = bytes[7];
			header[27] = bytes[6];
			header[28] = bytes[8];
			header[29] = bytes[9];
			header[30] = bytes[10];
			header[31] = bytes[11];
			header[32] = bytes[12];
			header[33] = bytes[13];
			header[34] = bytes[14];
			header[35] = bytes[15];
		}

		// header[36]=0; // reserved
		// header[37]=0; // reserved
		
		header[38] = (byte)((MAGIC_NUMBER>>8)&0xff);
		header[39] = (byte)(MAGIC_NUMBER&0xff);

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
			byte[] size_bytes =
			{
				(byte)((ann.Value.Length>>24)&0xff), 
				(byte)((ann.Value.Length>>16)&0xff), 
				(byte)((ann.Value.Length>>8)&0xff), 
				(byte)(ann.Value.Length&0xff)
			};
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
		int magic = ((header[38]&0xff) << 8)|(header[39]&0xff);
		if(magic != MAGIC_NUMBER)
			throw new PyroException("invalid header magic number");
		
		byte msg_type = header[6];
		byte serializer_id = header[7];
		int flags = (header[8] << 8) | header[9];
		int seq = (header[10] << 8) | header[11];
		
		int data_size=(((((header[12] <<8) | header[13]) <<8) | header[14]) <<8) | header[15];
		int annotations_size=(((((header[16] <<8) | header[17]) <<8) | header[18]) <<8) | header[19];

		// for now, we're not reading the response correlation ID from [20]-[35].

		var msg = new Message(msg_type, serializer_id, (ushort) flags, (ushort) seq, null)
		{
			data_size = data_size,
			annotations_size = (ushort) annotations_size
		};
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
	/// </summary>
	// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
	public static Message recv(Stream connection, byte[] requiredMsgTypes)
	{
		var header_data = IOUtil.recv(connection, HEADER_SIZE);
		var msg = from_header(header_data);
		if(requiredMsgTypes!=null && !requiredMsgTypes.Contains(msg.type))
			throw new PyroException($"invalid msg type {msg.type} received");

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
				int length = (annotations_data[i+4]<<24) | (annotations_data[i+5]<<16) | (annotations_data[i+6]<<8) | annotations_data[i+7];
				var annotations_bytes = new byte[length];
				Array.Copy(annotations_data, i+8, annotations_bytes, 0, length);
				msg.annotations[anno] = annotations_bytes;
				i += 8+length;
			}
		}
		
		// read data
		msg.data = IOUtil.recv(connection, msg.data_size);
				
		if(Config.MSG_TRACE_DIR!=null) {
			TraceMessageRecv(msg.seq, header_data, annotations_data, msg.data);
		}
		
		return msg;
	}

	public static void TraceMessageRecv(int sequenceNr, byte[] headerdata, byte[] annotations, byte[] data) {
		string filename=Path.Combine(Config.MSG_TRACE_DIR, $"{sequenceNr:D5}-b-recv-header.dat");
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(headerdata, 0, headerdata.Length);
			if(annotations!=null)
				fos.Write(annotations, 0, annotations.Length);
		}
		filename=Path.Combine(Config.MSG_TRACE_DIR, $"{sequenceNr:D5}-b-recv-message.dat");
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(data, 0, data.Length);
		}
	}

	public static void TraceMessageSend(int sequenceNr, byte[] headerdata, byte[] annotations, byte[] data) {
		string filename=Path.Combine(Config.MSG_TRACE_DIR, $"{sequenceNr:D5}-a-send-header.dat");
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(headerdata, 0, headerdata.Length);
			if(annotations!=null)
				fos.Write(annotations, 0, annotations.Length);
		}
		filename=Path.Combine(Config.MSG_TRACE_DIR, $"{sequenceNr:D5}-a-send-message.dat");
		using(FileStream fos=new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
			fos.Write(data, 0, data.Length);
		}
	}

}
}
