using System;
using System.IO;

namespace Razorvine.Pickle
{
    internal struct StreamReader : IInputReader
    {
        private readonly Stream input;
        private byte[] buffer;

        public StreamReader(Stream input)
        {
            this.input = input;
            this.buffer = new byte[sizeof(long)]; // at least large enough for any primitive being deserialized;
        }

        public byte ReadByte()
        {
            return PickleUtils.readbyte(input);
        }

        public ReadOnlySpan<byte> ReadBytes(int bytesCount)
        {
            EnsureByteBufferLength(bytesCount);

            PickleUtils.readbytes_into(input, buffer, 0, bytesCount);

            return new ReadOnlySpan<byte>(buffer, 0, bytesCount);
        }

        public string ReadLine(bool includeLF = false)
        {
            return PickleUtils.readline(input, includeLF);
        }

        public ReadOnlySpan<byte> ReadLineBytes(bool includeLF = false)
        {
            int length = PickleUtils.readline_into(input, ref buffer, includeLF);

            return new ReadOnlySpan<byte>(buffer, 0, length);
        }

        public void Skip(int bytesCount)
        {
            input.Position += bytesCount;
        }

        private void EnsureByteBufferLength(int bytesCount)
        {
            if (bytesCount > buffer.Length)
            {
                Array.Resize(ref buffer, Math.Max(bytesCount, buffer.Length * 2));
            }
        }
    }
}