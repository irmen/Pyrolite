using BenchmarkDotNet.Attributes;
using Razorvine.Pickle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Benchmarks
{
    [GenericTypeArguments(typeof(string))]
    [GenericTypeArguments(typeof(double))]
    [GenericTypeArguments(typeof(int))]
    [GenericTypeArguments(typeof(bool))]
    public class PicklerBenchmarks<T>
    {
        [Params(1000)]
        public int Count { get; set; }

        [Params(false, true)]
        public bool UseMemo { get; set; }

        [Params(false, true)]
        public bool Boxed { get; set; }

        private object _value;
        private Pickler _pickler;
        private byte[] _reusable;

        [GlobalSetup]
        public void Setup()
        {
            IEnumerable<T> input = Enumerable.Range(0, Count).Select(GetValue);

            if (Boxed) // a common case in dotnet/spark
                _value = input.Select(x => (object)x).ToArray();
            else
                _value = input.ToArray();

            _reusable = new byte[Count * 10];
            _pickler = new Pickler(UseMemo);
        }

        [GlobalCleanup]
        public void Cleanup() => _pickler.Dispose();

        [Benchmark]
        public byte[] ToByteArray() => _pickler.dumps(_value);

        [Benchmark]
        public void ToReusableByteArray() => _pickler.dumps(_value, ref _reusable, out _);

        [Benchmark]
        public void ToStream() => _pickler.dump(_value, new MemoryStream());

        private static T GetValue(int index)
        {
            if (typeof(T) == typeof(int))
                return (T)(object)index;
            if (typeof(T) == typeof(double))
                return (T)(object)(index * 0.5);
            if (typeof(T) == typeof(bool))
                return (T)(object)(index % 2 == 0);
            if (typeof(T) == typeof(string))
                return (T)(object)index.ToString();

            throw new NotSupportedException();
        }
    }
}