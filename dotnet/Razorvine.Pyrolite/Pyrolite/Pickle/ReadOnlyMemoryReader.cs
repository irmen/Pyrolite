﻿using System;

namespace Razorvine.Pickle
{
    internal struct ReadOnlyMemoryReader : IInputReader
    {
        private readonly ReadOnlyMemory<byte> input;
        private int position;

        public ReadOnlyMemoryReader(ReadOnlyMemory<byte> bytes)
        {
            input = bytes;
            position = 0;
        }

        public byte ReadByte()
        {
            return input.Span[position++];
        }

        public ReadOnlySpan<byte> ReadBytes(int bytesCount)
        {
            var result = input.Slice(position, bytesCount).Span;
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
            var bytes = input.Slice(position).Span;

            if (includeLF)
                return bytes.IndexOf((byte) '\n') + 1;
            else
                return bytes.IndexOf((byte) '\n');
        }
    }
}