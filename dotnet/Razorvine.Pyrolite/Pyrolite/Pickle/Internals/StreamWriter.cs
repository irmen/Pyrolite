/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Razorvine.Pickle
{
    internal struct StreamWriter : IOutputWriter
    {
        private readonly Stream output;
        private readonly byte[] byteBuffer;

        public StreamWriter(Stream output)
        {
            this.output = output;
            byteBuffer = new byte[sizeof(long)]; // at least large enough for any primitive being serialized
        }

        public void Dispose() => output.Dispose();

        public int BytesWritten => throw new NotSupportedException("Not supported by design.");

        public byte[] Buffer => throw new NotSupportedException("Not supported by design.");

        public void WriteByte(byte value) => output.WriteByte(value);

        public void WriteBytes(byte first, byte second)
        {
            output.WriteByte(first);
            output.WriteByte(second);
        }

        public void WriteBytes(byte first, byte second, byte third)
        {
            output.WriteByte(first);
            output.WriteByte(second);
            output.WriteByte(third);
        }

        public void Write(byte[] buffer, int offset, int count) => output.Write(buffer, offset, count);

        public void WriteInt32LittleEndian(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(byteBuffer, value);
            output.Write(byteBuffer, 0, sizeof(int));
        }

        public void WriteInt64BigEndian(long value)
        {
            BinaryPrimitives.WriteInt64BigEndian(byteBuffer, value);
            output.Write(byteBuffer, 0, sizeof(long));
        }

        public void WriteAsUtf8String(string str)
        {
            var encoded = Encoding.UTF8.GetBytes(str);
            WriteInt32LittleEndian(encoded.Length);
            Write(encoded, 0, encoded.Length);
        }

        public void Flush() => output.Flush();
    }
}