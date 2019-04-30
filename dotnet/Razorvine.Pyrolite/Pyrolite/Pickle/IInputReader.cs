using System;

namespace Razorvine.Pickle
{
    internal interface IInputReader
    {
        byte ReadByte();

        ReadOnlySpan<byte> ReadBytes(int bytesCount);

        ReadOnlySpan<byte> ReadLineBytes(bool includeLF = false);

        string ReadLine(bool includeLF = false);

        void Skip(int bytesCount);
    }
}