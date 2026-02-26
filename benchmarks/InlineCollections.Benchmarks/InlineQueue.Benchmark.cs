using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using InlineCollections;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80, invocationCount: 100000)]
    [MemoryDiagnoser]
    public class InlineQueueBenchmark
    {
        private const int InnerOps = 100;


        [Benchmark(Baseline = true)]
        public void SysQueue_Cycle()
        {
            var queue = new Queue<int>(32);
            for (int i = 0; i < InnerOps; i++)
            {
                for (int j = 0; j < 16; j++) queue.Enqueue(j);
                for (int j = 0; j < 16; j++) queue.Dequeue();
            }
        }

        [Benchmark]
        public void InlineQueue_Cycle()
        {
            var queue = new InlineQueue32<int>();
            for (int i = 0; i < InnerOps; i++)
            {
                for (int j = 0; j < 16; j++) queue.Enqueue(j);
                for (int j = 0; j < 16; j++) queue.Dequeue();
            }
        }


        [Benchmark]
        public int InlineQueue_WrapAround()
        {
            var queue = new InlineQueue32<int>();
            int sum = 0;
            for (int i = 0; i < 20; i++) queue.Enqueue(i);

            for (int i = 0; i < InnerOps; i++)
            {
                sum += queue.Dequeue();
                queue.Enqueue(i);
            }
            return sum;
        }

        [Benchmark]
        public int SysQueue_WrapAround()
        {
            var queue = new Queue<int>(32);
            int sum = 0;
            for (int i = 0; i < 20; i++) queue.Enqueue(i);

            for (int i = 0; i < InnerOps; i++)
            {
                sum += queue.Dequeue();
                queue.Enqueue(i);
            }
            return sum;
        }

        [Benchmark]
        public int InlineQueue_Foreach()
        {
            var queue = new InlineQueue32<int>();
            for (int j = 0; j < 32; j++) queue.Enqueue(j);

            int sum = 0;
            for (int i = 0; i < InnerOps; i++)
            {
                foreach (var item in queue) sum += item;
            }
            return sum;
        }

        [Benchmark]
        public int SysQueue_Foreach()
        {
            var queue = new Queue<int>(32);
            for (int j = 0; j < 32; j++) queue.Enqueue(j);

            int sum = 0;
            for (int i = 0; i < InnerOps; i++)
            {
                foreach (var item in queue) sum += item;
            }
            return sum;
        }

        [Benchmark]
        public void InlineQueue_Clear()
        {
            var queue = new InlineQueue32<int>();
            for (int i = 0; i < InnerOps; i++)
            {
                queue.Enqueue(1);
                queue.Enqueue(2);
                queue.Clear();
            }
        }

        [Benchmark]
        public void SysQueue_Clear()
        {
            var queue = new Queue<int>(32);
            for (int i = 0; i < InnerOps; i++)
            {
                queue.Enqueue(1);
                queue.Enqueue(2);
                queue.Clear();
            }
        }

        [Benchmark]
        public bool InlineQueue_TryEnqueueDequeue_Full()
        {
            var queue = new InlineQueue32<int>();
            bool success = true;
            for (int i = 0; i < InnerOps; i++)
            {
                success &= queue.TryEnqueue(i);
                success &= queue.TryDequeue(out _);
            }
            return success;
        }

        public static void Run()
        {
            var summary = BenchmarkRunner.Run<InlineQueueBenchmark>();
        }
    }
}