using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace Razorvine.Pickle
{
    internal interface IOutputWriter : IDisposable
    {
        void WriteByte(byte value);
        void WriteBytes(byte first, byte second);
        void WriteBytes(byte first, byte second, byte third);

        void Write(byte[] buffer, int offset, int count);

        void WriteInt32LittleEndian(int value);
        void WriteInt64BigEndian(long value);
    }

    internal struct StreamWriter : IOutputWriter
    {
        private readonly Stream _stream;
        private readonly byte[] _byteBuffer;

        public StreamWriter(Stream stream)
        {
            _stream = stream;
            _byteBuffer = new byte[sizeof(long)]; // at least large enough for any primitive being serialized
        }

        public void Dispose() => _stream.Dispose();

        internal Stream Stream => _stream;

        public void WriteByte(byte value) => _stream.WriteByte(value);

        public void WriteBytes(byte first, byte second)
        {
            _stream.WriteByte(first);
            _stream.WriteByte(second);
        }

        public void WriteBytes(byte first, byte second, byte third)
        {
            _stream.WriteByte(first);
            _stream.WriteByte(second);
            _stream.WriteByte(third);
        }

        public void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        public void WriteInt32LittleEndian(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_byteBuffer, value);
            _stream.Write(_byteBuffer, 0, sizeof(int));
        }

        public void WriteInt64BigEndian(long value)
        {
            BinaryPrimitives.WriteInt64BigEndian(_byteBuffer, value);
            _stream.Write(_byteBuffer, 0, sizeof(long));
        }
    }

    internal struct ArrayWriter : IOutputWriter
    {
        private byte[] _output;
        private int _position;

        public ArrayWriter(byte[] output)
        {
            _output = output;
            _position = 0;
        }

        public void Dispose() => _output = null;

        public int Position => _position;

        public byte[] Output => _output;

        public void WriteByte(byte value)
        {
            EnsureSize(1);
            _output[_position++] = value;
        }

        // this method exists so we don't have to call EnsureSize(1) twice
        public void WriteBytes(byte first, byte second)
        {
            EnsureSize(2);
            _output[_position++] = first;
            _output[_position++] = second;
        }

        // this method exists so we don't have to call EnsureSize(1) three times
        public void WriteBytes(byte first, byte second, byte third)
        {
            EnsureSize(3);
            _output[_position++] = first;
            _output[_position++] = second;
            _output[_position++] = third;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            EnsureSize(count);
            buffer.AsSpan(offset, count).CopyTo(_output.AsSpan(_position, count));
            _position += count;
        }

        public void WriteInt32LittleEndian(int value)
        {
            EnsureSize(sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(_output.AsSpan(_position, sizeof(int)), value);
            _position += sizeof(int);
        }

        public void WriteInt64BigEndian(long value)
        {
            EnsureSize(sizeof(long));
            BinaryPrimitives.WriteInt64BigEndian(_output.AsSpan(_position, sizeof(long)), value);
            _position += sizeof(long);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSize(int requested)
        {
            if (_output.Length < _position + requested)
            {
                Array.Resize(ref _output, Math.Max(_output.Length + requested, _output.Length * 2));
            }
        }
    }
}