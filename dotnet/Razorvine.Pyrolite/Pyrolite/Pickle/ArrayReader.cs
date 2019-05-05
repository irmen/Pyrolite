using System;

namespace Razorvine.Pickle
{
    internal struct ArrayReader : IInputReader
    {
        private readonly byte[] input;
        private int position;

        public ArrayReader(byte[] bytes)
        {
            input = bytes;
            position = 0;
        }

        public byte ReadByte()
        {
            return input[position++];
        }

        public ReadOnlySpan<byte> ReadBytes(int bytesCount)
        {
            var result = new ReadOnlySpan<byte>(input, position, bytesCount);
            position += bytesCount;
            return result;
        }

        public string ReadLine(bool includeLF = false)
        {
            return PickleUtils.rawStringFromBytes(ReadLineBytes(includeLF));
        }

        public ReadOnlySpan<byte> ReadLineBytes(bool includeLF = false)
        {
            var result = ReadBytes(GetLineEndIndex(includeLF));
            if (!includeLF)
                Skip(1);
            return result;
        }

        public void Skip(int bytesCount)
        {
            position += bytesCount;
        }

        private int GetLineEndIndex(bool includeLF = false)
        {
            var bytes = new ReadOnlySpan<byte>(input, position, input.Length - position);
            int index = bytes.IndexOf((byte) '\n');
            if (includeLF)
                index++;
            return index;
        }
    }
}