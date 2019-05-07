using System;
using BenchmarkDotNet.Attributes;
using Razorvine.Pickle;
using System.IO;
using System.Linq;

namespace Benchmarks
{
    public class UnpicklerBenchmarks
    {
        [Params(100, 1000)]
        public int Count { get; set;}

        private byte[] _serializedDoubles;
        private byte[] _serializedIntegers;
        private byte[] _serializedBooleans;
        private byte[] _serializedStrings;

        [GlobalSetup]
        public void Setup()
        {
            double[] doubles = Enumerable.Range(0, Count).Select(x => x * 0.5).ToArray();
            int[] integers = Enumerable.Range(0, Count).ToArray();
            bool[] booleans = Enumerable.Range(0, Count).Select(x => x % 2 == 0).ToArray();
            string[] strings = doubles.Select(x => x.ToString()).ToArray();

            _serializedDoubles = new Pickler().dumps(doubles);
            _serializedIntegers = new Pickler().dumps(integers);
            _serializedBooleans = new Pickler().dumps(booleans);
            _serializedStrings = new Pickler().dumps(strings);
        }

        [Benchmark]
        public object DoublesFromStream() { return  new Unpickler().load(new MemoryStream(_serializedDoubles)); }

        [Benchmark]
        public object IntegersFromStream() => new Unpickler().load(new MemoryStream(_serializedIntegers));

        [Benchmark]
        public object BooleansFromStream() => new Unpickler().load(new MemoryStream(_serializedBooleans));

        [Benchmark]
        public object StringsFromStream() => new Unpickler().load(new MemoryStream(_serializedStrings));

        [Benchmark]
        public object DoublesFromArray() => new Unpickler().loads(_serializedDoubles);

        [Benchmark]
        public object IntegersFromArray() => new Unpickler().loads(_serializedIntegers);

        [Benchmark]
        public object BooleansFromArray() => new Unpickler().loads(_serializedBooleans);

        [Benchmark]
        public object StringsFromArray() => new Unpickler().loads(_serializedStrings);

        [Benchmark]
        public object DoublesFromReadOnlyMemory() => new Unpickler().loads(new ReadOnlyMemory<byte>(_serializedDoubles));

        [Benchmark]
        public object IntegersFromReadOnlyMemory() => new Unpickler().loads(new ReadOnlyMemory<byte>(_serializedIntegers));

        [Benchmark]
        public object BooleansFromReadOnlyMemory() => new Unpickler().loads(new ReadOnlyMemory<byte>(_serializedBooleans));

        [Benchmark]
        public object StringsFromReadOnlyMemory() => new Unpickler().loads(new ReadOnlyMemory<byte>(_serializedStrings));
    }
}