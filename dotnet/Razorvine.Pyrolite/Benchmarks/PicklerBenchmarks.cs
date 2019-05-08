using BenchmarkDotNet.Attributes;
using Razorvine.Pickle;
using System.IO;
using System.Linq;

namespace Benchmarks
{
    public class PicklerBenchmarks
    {
        [Params(100, 1000)]
        public int Count { get; set; }

        private double[] _doubles;
        private int[] _integers;
        private bool[] _booleans;
        private string[] _strings;

        private byte[] _reusable;

        [GlobalSetup]
        public void Setup()
        {
            _doubles = Enumerable.Range(0, Count).Select(x => x * 0.5).ToArray();
            _integers = Enumerable.Range(0, Count).ToArray();
            _booleans = Enumerable.Range(0, Count).Select(x => x % 2 == 0).ToArray();
            _strings = _doubles.Select(x => x.ToString()).ToArray();
            _reusable = new byte[Count * 10];
        }

        [Benchmark] public byte[] DoublesToByteArray() => new Pickler().dumps(_doubles);
        [Benchmark] public byte[] IntegersToByteArray() => new Pickler().dumps(_integers);
        [Benchmark] public byte[] BooleansToByteArray() => new Pickler().dumps(_booleans);
        [Benchmark] public byte[] StringsToByteArray() => new Pickler().dumps(_strings);

        [Benchmark] public int DoublesToReusableByteArray() => new Pickler().dumps(_doubles, ref _reusable);
        [Benchmark] public int IntegersToReusableByteArray() => new Pickler().dumps(_integers, ref _reusable);
        [Benchmark] public int BooleansToReusableByteArray() => new Pickler().dumps(_booleans, ref _reusable);
        [Benchmark] public int StringsToReusableByteArray() => new Pickler().dumps(_strings, ref _reusable);

        [Benchmark] public void DoublesToStream() => new Pickler().dump(_doubles, new MemoryStream());
        [Benchmark] public void IntegersToStream() => new Pickler().dump(_integers, new MemoryStream());
        [Benchmark] public void BooleansToStream() => new Pickler().dump(_booleans, new MemoryStream());
        [Benchmark] public void StringsToStream() => new Pickler().dump(_strings, new MemoryStream());
    }
}