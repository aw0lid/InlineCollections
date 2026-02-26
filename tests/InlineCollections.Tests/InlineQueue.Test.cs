using System;
using Xunit;
using InlineCollections;

namespace InlineCollections.Tests
{
    public class InlineQueue32Tests
    {
        [Fact]
        public void Constructor_InitializesEmptyQueue()
        {
            var queue = new InlineQueue32<int>();
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void Enqueue_SingleItem_Succeeds()
        {
            var queue = new InlineQueue32<int>();
            queue.Enqueue(42);
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void Enqueue_MultipleItems_AllSucceed()
        {
            var queue = new InlineQueue32<int>();
            for (int i = 0; i < 32; i++)
            {
                queue.Enqueue(i);
            }

            Assert.Equal(32, queue.Count);
        }



        [Fact]
        public void TryEnqueue_FullQueue_DoesNotModifyState()
        {
            var queue = new InlineQueue32<int>();
            for (int i = 0; i < 32; i++) queue.TryEnqueue(i);

            int countBefore = queue.Count;
            bool result = queue.TryEnqueue(999);

            Assert.False(result);
            Assert.Equal(countBefore, queue.Count);
            Assert.Equal(0, queue.Dequeue());
        }

        [Fact]
        public void Dequeue_ReturnFirstEnqueuedItem()
        {
            var queue = new InlineQueue32<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            Assert.Equal(1, queue.Dequeue());
            Assert.Equal(2, queue.Dequeue());
            Assert.Equal(3, queue.Dequeue());
        }



        [Fact]
        public void Dequeue_DecreasesCount()
        {
            var queue = new InlineQueue32<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);

            queue.Dequeue();
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void Enqueue_FullQueue_ThrowsInvalidOperationException()
        {
            var queue = new InlineQueue32<int>();
            for (int i = 0; i < 32; i++) queue.Enqueue(i);

            bool threw = false;
            try
            {
                queue.Enqueue(999);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert.True(threw, "Enqueue must throw InvalidOperationException when full.");
        }

        [Fact]
        public void Dequeue_EmptyQueue_ThrowsInvalidOperationException()
        {
            var queue = new InlineQueue32<int>();

            bool threw = false;
            try
            {
                queue.Dequeue();
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert.True(threw, "Dequeue must throw InvalidOperationException when empty.");
        }

        [Fact]
        public void Peek_EmptyQueue_ThrowsInvalidOperationException()
        {
            var queue = new InlineQueue32<int>();

            bool threw = false;
            try
            {
                _ = queue.Peek();
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert.True(threw, "Peek must throw InvalidOperationException when empty.");
        }

        [Fact]
        public void TryDequeue_EmptyQueue_ReturnsFalseAndDefault()
        {
            var queue = new InlineQueue32<int>();
            bool success = queue.TryDequeue(out int result);

            Assert.False(success);
            Assert.Equal(0, result);
        }

        [Fact]
        public void TryDequeue_CorrectlyUpdatesCount_AtBoundaries()
        {
            var queue = new InlineQueue32<int>();
            queue.Enqueue(1);

            Assert.True(queue.TryDequeue(out _));
            Assert.Equal(0, queue.Count);
            Assert.False(queue.TryDequeue(out _));
        }

        [Fact]
        public void Peek_ReturnsFirstWithoutRemoving()
        {
            var queue = new InlineQueue32<int>();
            queue.Enqueue(42);

            ref var first = ref queue.Peek();
            Assert.Equal(42, first);
            Assert.Equal(1, queue.Count);
        }



        [Fact]
        public void Peek_AllowsModification()
        {
            var queue = new InlineQueue32<int>();
            queue.Enqueue(10);

            ref var first = ref queue.Peek();
            first = 20;

            Assert.Equal(20, queue.Dequeue());
        }

        [Fact]
        public void Clear_RemovesAllItems()
        {
            var queue = new InlineQueue32<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            queue.Clear();
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void FIFO_Semantics()
        {
            var queue = new InlineQueue32<int>();
            queue.Enqueue(100);
            queue.Enqueue(200);
            queue.Enqueue(300);

            Assert.Equal(100, queue.Dequeue());
            Assert.Equal(200, queue.Dequeue());
            Assert.Equal(300, queue.Dequeue());
        }

        [Fact]
        public void WrapAround_EnqueueAfterDequeue()
        {
            var queue = new InlineQueue32<int>();

            for (int i = 0; i < 32; i++) queue.Enqueue(i);
            for (int i = 0; i < 16; i++) queue.Dequeue();
            for (int i = 32; i < 48; i++) queue.Enqueue(i);

            Assert.Equal(32, queue.Count);
        }

        [Fact]
        public void Capacity_IsAlways32()
        {
            Assert.Equal(32, InlineQueue32<int>.Capacity);
        }

        [Fact]
        public void Queue_InfiniteCycle_StressTest()
        {
            var queue = new InlineQueue32<int>();

            for (int i = 0; i < 1000; i++)
            {
                queue.Enqueue(i);
                Assert.Equal(1, queue.Count);
                int val = queue.Dequeue();
                Assert.Equal(i, val);
                Assert.Equal(0, queue.Count);
            }
        }

        [Fact]
        public void Clear_AfterWrapAround_ResetsPointers()
        {
            var queue = new InlineQueue32<int>();
            for (int i = 0; i < 20; i++) queue.Enqueue(i);
            for (int i = 0; i < 10; i++) queue.Dequeue();

            queue.Clear();
            Assert.Equal(0, queue.Count);

            for (int i = 0; i < 32; i++) queue.Enqueue(i);
            Assert.Equal(32, queue.Count);
        }

        [Fact]
        public void Fill_Empty_Fill_VerifiesFullCapacity()
        {
            var queue = new InlineQueue32<int>();

            for (int i = 0; i < 32; i++) queue.Enqueue(i);
            Assert.Equal(32, queue.Count);
            for (int i = 0; i < 32; i++) queue.Dequeue();

            for (int i = 0; i < 32; i++) queue.Enqueue(i + 100);
            Assert.Equal(32, queue.Count);
            Assert.Equal(100, queue.Peek());
        }



        [Fact]
        public void GetEnumerator_HandlesWrapAroundCorrectly()
        {
            var queue = new InlineQueue32<int>();
            for (int i = 0; i < 20; i++) queue.Enqueue(i);
            for (int i = 0; i < 15; i++) queue.Dequeue();
            for (int i = 20; i < 30; i++) queue.Enqueue(i);


            int expectedCount = 15;
            int actualCount = 0;
            foreach (var item in queue)
            {
                actualCount++;
            }
            Assert.Equal(expectedCount, actualCount);
        }

        [Fact]
        public void GetEnumerator_IteratesInCorrectOrder_AfterWrapAround()
        {
            var queue = new InlineQueue32<int>();

            for (int i = 1; i <= 20; i++) queue.Enqueue(i);
            for (int i = 0; i < 10; i++) queue.Dequeue();
            for (int i = 21; i <= 30; i++) queue.Enqueue(i);

            int expectedValue = 11;
            foreach (var item in queue)
            {
                Assert.Equal(expectedValue, item);
                expectedValue++;
            }
            Assert.Equal(31, expectedValue);
        }

        [Fact]
        public void Enumerator_AfterFullCycle_ReturnsCorrectData()
        {
            var queue = new InlineQueue32<int>();
            for (int i = 0; i < 100; i++)
            {
                queue.Enqueue(i);
                queue.Dequeue();
            }

            queue.Enqueue(999);
            var enumerator = queue.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(999, enumerator.Current);
        }


    }
}