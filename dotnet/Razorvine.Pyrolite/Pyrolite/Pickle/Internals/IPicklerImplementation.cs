/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;

namespace Razorvine.Pickle
{
    internal interface IPicklerImplementation : IDisposable
    {
        int BytesWritten { get; }
        byte[] Buffer { get; }

        void dump(object o);
        void save(object o);
    }
}