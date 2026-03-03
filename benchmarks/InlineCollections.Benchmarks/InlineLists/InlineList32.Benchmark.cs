using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using InlineCollections;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80, invocationCount: 1000)]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn, StdDevColumn]
    public class InlineList32Benchmark
    {
        private int[] data32 = null!;
        private const int OperationsPerInvoke = 100;

        [GlobalSetup]
        public void Setup()
        {
            data32 = Enumerable.Range(0, 32).ToArray();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineList_Add()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new InlineList32<int>();
                list.Add(1); list.Add(2); list.Add(3); list.Add(4);
                list.Add(5); list.Add(6); list.Add(7); list.Add(8);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void List_Add()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new List<int>(8);
                list.Add(1); list.Add(2); list.Add(3); list.Add(4);
                list.Add(5); list.Add(6); list.Add(7); list.Add(8);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public int InlineList_Indexer()
        {
            var list = new InlineList32<int>(data32);
            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                sum += list[i % 32];
                list[i % 32] = sum;
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public int List_Indexer()
        {
            var list = new List<int>(data32);
            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                sum += list[i % 32];
                list[i % 32] = sum;
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineList_Remove()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new InlineList32<int>(data32);
                list.RemoveAt(0);
                list.RemoveAt(15);
                list.RemoveAt(list.Count - 1);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void List_Remove()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new List<int>(data32);
                list.RemoveAt(0);
                list.RemoveAt(15);
                list.RemoveAt(list.Count - 1);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public bool InlineList_Contains()
        {
            var list = new InlineList32<int>(data32);
            bool result = false;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                result ^= list.Contains(31);
            }
            return result;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public bool List_Contains()
        {
            var list = new List<int>(data32);
            bool result = false;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                result ^= list.Contains(31);
            }
            return result;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public int InlineList_Foreach()
        {
            var list = new InlineList32<int>(data32);
            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                foreach (var item in list) sum += item;
            }
            return sum;
        }

        // --- AddRange Battle (Bulk) ---
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineList_AddRange()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new InlineList32<int>();
                list.AddRange(data32);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void List_AddRange()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new List<int>(32);
                list.AddRange(data32);
            }
        }

        // --- Insert Battle (Memory Shifting) ---
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineList_Insert_Middle()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new InlineList32<int>();
                // نملأ شوية داتا الأول
                list.Add(1); list.Add(2); list.Add(3); list.Add(4);
                // نحشر في النص - ده بيختبر الـ Span Shifting
                list.Insert(2, 99);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void List_Insert_Middle()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new List<int>(8);
                list.Add(1); list.Add(2); list.Add(3); list.Add(4);
                list.Insert(2, 99);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineList_TryAdd_Checked()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new InlineList32<int>();
                for (int j = 0; j < 8; j++) list.TryAdd(j);
            }
        }


        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineList_Insert_First()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new InlineList32<int>(data32);
                list.RemoveAt(list.Count - 1);
                list.Insert(0, 999);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineList_TryInsert_Checked()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new InlineList32<int>(data32);
                list.RemoveAt(list.Count - 1);
                list.TryInsert(0, 999);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void List_Insert_First()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new List<int>(data32);
                list.RemoveAt(list.Count - 1);
                list.Insert(0, 999);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineList_Constructor_Span()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new InlineList32<int>(data32);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void List_Constructor_Enumerable()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var list = new List<int>(data32);
            }
        }


        public static void Run()
        {
            BenchmarkRunner.Run<InlineList32Benchmark>();
        }
    }
}