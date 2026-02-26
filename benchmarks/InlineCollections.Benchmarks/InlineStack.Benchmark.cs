using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using InlineCollections;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80, invocationCount: 1000)]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn, StdDevColumn]
    public class InlineStack32Benchmark
    {
        private int[] data32 = null!;
        private const int OperationsPerInvoke = 100;

        [GlobalSetup]
        public void Setup()
        {
            data32 = Enumerable.Range(0, 32).ToArray();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineStack_Push()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var stack = new InlineStack32<int>();
                stack.Push(1); stack.Push(2); stack.Push(3); stack.Push(4);
                stack.Push(5); stack.Push(6); stack.Push(7); stack.Push(8);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Stack_Push()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var stack = new Stack<int>(8);
                stack.Push(1); stack.Push(2); stack.Push(3); stack.Push(4);
                stack.Push(5); stack.Push(6); stack.Push(7); stack.Push(8);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public int InlineStack_PopPeek()
        {
            var stack = new InlineStack32<int>();
            for (int i = 0; i < 32; i++) stack.Push(i);

            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                sum += stack.Peek();
                sum += stack.Pop();
                stack.Push(i);
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public int Stack_PopPeek()
        {
            var stack = new Stack<int>(data32);
            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                sum += stack.Peek();
                sum += stack.Pop();
                stack.Push(i);
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public int InlineStack_Foreach()
        {
            var stack = new InlineStack32<int>();
            for (int i = 0; i < 32; i++) stack.Push(i);

            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                foreach (var item in stack)
                {
                    sum += item;
                }
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public int Stack_Foreach()
        {
            var stack = new Stack<int>(data32);
            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                foreach (var item in stack)
                {
                    sum += item;
                }
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineStack_Clear()
        {
            var stack = new InlineStack32<int>();
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                stack.Push(1); stack.Push(2);
                stack.Clear();
                stack.Push(3); stack.Push(4);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Stack_Clear()
        {
            var stack = new Stack<int>(8);
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                stack.Push(1); stack.Push(2);
                stack.Clear();
                stack.Push(3); stack.Push(4);
            }
        }

        [Benchmark]
        public void Creation_InlineStack()
        {
            var stack = new InlineStack32<int>();
        }

        [Benchmark]
        public void Creation_StandardStack()
        {
            var stack = new Stack<int>(32);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public bool InlineStack_TryPushPop()
        {
            var stack = new InlineStack32<int>();
            bool success = true;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                success &= stack.TryPush(i % 32);
                success &= stack.TryPop(out _);
            }
            return success;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public bool Stack_TryPop()
        {
            var stack = new Stack<int>(32);
            bool success = true;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                stack.Push(i % 32);
                success &= stack.TryPop(out _);
            }
            return success;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public int InlineStack_AsSpan_Loop()
        {
            var stack = new InlineStack32<int>();
            for (int i = 0; i < 32; i++) stack.Push(i);

            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                ReadOnlySpan<int> span = stack.AsSpan();
                for (int j = 0; j < span.Length; j++)
                {
                    sum += span[j];
                }
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void InlineStack_FillAndEmpty()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var stack = new InlineStack32<int>();
                for (int j = 0; j < 32; j++) stack.Push(j);
                for (int j = 0; j < 32; j++) stack.Pop();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Stack_FillAndEmpty()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var stack = new Stack<int>(32);
                for (int j = 0; j < 32; j++) stack.Push(j);
                for (int j = 0; j < 32; j++) stack.Pop();
            }
        }


        public static void Run()
        {
            BenchmarkRunner.Run<InlineStack32Benchmark>();
        }
    }
}