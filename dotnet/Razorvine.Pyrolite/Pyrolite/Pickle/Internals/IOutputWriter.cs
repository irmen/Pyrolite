/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;

namespace Razorvine.Pickle
{
    internal interface IOutputWriter : IDisposable
    {
        int BytesWritten { get; }
        byte[] Buffer { get; }

        void WriteByte(byte value);
        void WriteBytes(byte first, byte second);
        void WriteBytes(byte first, byte second, byte third);

        void Write(byte[] buffer, int offset, int count);

        void WriteInt32LittleEndian(int value);
        void WriteInt64BigEndian(long value);
        void WriteAsUtf8String(string str);

        void Flush();
    }
}