/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Buffers.Binary;
using System.Text;

namespace Razorvine.Pickle
{
    internal struct ArrayWriter : IOutputWriter
    {
        private byte[] output;
        private int position;

        public ArrayWriter(byte[] output)
        {
            this.output = output;
            position = 0;
        }

        public void Dispose() => output = null;

        public int BytesWritten => position;

        public byte[] Buffer => output;

        public void WriteByte(byte value)
        {
            EnsureSize(1);
            output[position++] = value;
        }

        // this method exists so we don't have to call EnsureSize(1) twice
        public void WriteBytes(byte first, byte second)
        {
            EnsureSize(2);
            output[position++] = first;
            output[position++] = second;
        }

        // this method exists so we don't have to call EnsureSize(1) three times
        public void WriteBytes(byte first, byte second, byte third)
        {
            EnsureSize(3);
            output[position++] = first;
            output[position++] = second;
            output[position++] = third;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            EnsureSize(count);
            buffer.AsSpan(offset, count).CopyTo(output.AsSpan(position, count));
            position += count;
        }

        public void WriteInt32LittleEndian(int value)
        {
            EnsureSize(sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(position, sizeof(int)), value);
            position += sizeof(int);
        }

        public void WriteInt64BigEndian(long value)
        {
            EnsureSize(sizeof(long));
            BinaryPrimitives.WriteInt64BigEndian(output.AsSpan(position, sizeof(long)), value);
            position += sizeof(long);
        }

        public void WriteAsUtf8String(string str)
        {
            int byteCount = Encoding.UTF8.GetByteCount(str);
            WriteInt32LittleEndian(byteCount);

            EnsureSize(byteCount);

            unsafe
            {
                fixed (char* source = str)
                fixed (byte* destination = output)
                {
                    // this part is crucial for the performance: instead of allocating a new byte array
                    // the output is written to the existing buffer
                    Encoding.UTF8.GetBytes(source, str.Length, destination + position, byteCount);
                }
            }

            position += byteCount;
        }

        public void Flush() { } // does nothing on purpose

        private void EnsureSize(int requested)
        {
            if (output.Length < position + requested)
            {
                Array.Resize(ref output, Math.Max(output.Length + requested, output.Length * 2));
            }
        }
    }
}