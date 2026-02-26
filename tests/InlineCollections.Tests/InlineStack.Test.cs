using System;
using Xunit;
using InlineCollections;

namespace InlineCollections.Tests
{
    public class InlineStack32Tests
    {
        [Fact]
        public void Constructor_InitializesEmptyStack()
        {
            var stack = new InlineStack32<int>();
            Assert.Equal(0, stack.Count);
        }

        [Fact]
        public void Push_SingleItem_Succeeds()
        {
            var stack = new InlineStack32<int>();
            stack.Push(42);
            Assert.Equal(1, stack.Count);
        }

        [Fact]
        public void Push_MultipleItems_AllSucceed()
        {
            var stack = new InlineStack32<int>();
            for (int i = 0; i < 32; i++)
            {
                stack.Push(i);
            }

            Assert.Equal(32, stack.Count);
        }



        [Fact]
        public void TryPush_WithinCapacity_ReturnsTrue()
        {
            var stack = new InlineStack32<int>();
            bool result = stack.TryPush(42);
            Assert.True(result);
        }

        [Fact]
        public void TryPush_ExceedCapacity_ReturnsFalse()
        {
            var stack = new InlineStack32<int>();
            for (int i = 0; i < 32; i++) stack.TryPush(i);

            bool result = stack.TryPush(999);
            Assert.False(result);
        }

        [Fact]
        public void Pop_ReturnsLastPushedItem()
        {
            var stack = new InlineStack32<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            Assert.Equal(3, stack.Pop());
            Assert.Equal(2, stack.Pop());
            Assert.Equal(1, stack.Pop());
        }



        [Fact]
        public void Pop_DecreasesCount()
        {
            var stack = new InlineStack32<int>();
            stack.Push(1);
            stack.Push(2);

            stack.Pop();
            Assert.Equal(1, stack.Count);
        }

        [Fact]
        public void Push_ExceedCapacity_ThrowsInvalidOperationException()
        {
            var stack = new InlineStack32<int>();
            for (int i = 0; i < 32; i++) stack.Push(i);

            bool threw = false;
            try { stack.Push(999); }
            catch (InvalidOperationException) { threw = true; }
            Assert.True(threw, "Push should throw InvalidOperationException when full.");
        }

        [Fact]
        public void Pop_EmptyStack_ThrowsInvalidOperationException()
        {
            var stack = new InlineStack32<int>();
            bool threw = false;
            try { stack.Pop(); }
            catch (InvalidOperationException) { threw = true; }
            Assert.True(threw, "Pop should throw InvalidOperationException when empty.");
        }

        [Fact]
        public void Peek_EmptyStack_ThrowsInvalidOperationException_ManualCheck()
        {
            var stack = new InlineStack32<int>();
            bool threw = false;
            try { _ = stack.Peek(); }
            catch (InvalidOperationException) { threw = true; }
            Assert.True(threw);
        }

        [Fact]
        public void TryPop_WithItems_ReturnsTrueAndValue()
        {
            var stack = new InlineStack32<int>();
            stack.Push(42);

            bool result = stack.TryPop(out int value);
            Assert.True(result);
            Assert.Equal(42, value);
        }

        [Fact]
        public void TryPop_OnEmptyStack_DoesNotUnderflowCount()
        {
            var stack = new InlineStack32<int>();
            bool result = stack.TryPop(out _);

            Assert.False(result);
            Assert.Equal(0, stack.Count);
        }

        [Fact]
        public void TryPop_EmptyStack_ReturnsFalse()
        {
            var stack = new InlineStack32<int>();
            bool result = stack.TryPop(out int value);

            Assert.False(result);
            Assert.Equal(0, value);
        }

        [Fact]
        public void Peek_ReturnsTopWithoutRemoving()
        {
            var stack = new InlineStack32<int>();
            stack.Push(42);

            ref var top = ref stack.Peek();
            Assert.Equal(42, top);
            Assert.Equal(1, stack.Count);
        }



        [Fact]
        public void Peek_AllowsModification()
        {
            var stack = new InlineStack32<int>();
            stack.Push(10);

            ref var top = ref stack.Peek();
            top = 20;

            Assert.Equal(20, stack.Pop());
        }

        [Fact]
        public void Clear_RemovesAllItems()
        {
            var stack = new InlineStack32<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            stack.Clear();
            Assert.Equal(0, stack.Count);
        }

        [Fact]
        public void AsSpan_ReturnsValidSpan()
        {
            var stack = new InlineStack32<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            var span = stack.AsSpan();
            Assert.Equal(3, span.Length);
            // ترتيب الـ Span بيعتمد على الـ Internal Array
            Assert.Equal(1, span[0]);
            Assert.Equal(2, span[1]);
            Assert.Equal(3, span[2]);
        }

        [Fact]
        public void GetEnumerator_IteratesAllElements()
        {
            var stack = new InlineStack32<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            int count = 0;
            foreach (var item in stack)
            {
                count++;
            }

            Assert.Equal(3, count);
        }

        [Fact]
        public void LIFO_Semantics()
        {
            var stack = new InlineStack32<int>();
            stack.Push(10);
            stack.Push(20);
            stack.Push(30);

            Assert.Equal(30, stack.Pop());
            Assert.Equal(20, stack.Pop());
            Assert.Equal(10, stack.Pop());
        }

        [Fact]
        public void Capacity_IsAlways32()
        {
            Assert.Equal(32, InlineStack32<int>.Capacity);
        }

        [Fact]
        public void Peek_Ref_Modification_ActuallyChangesStoredValue()
        {
            var stack = new InlineStack32<int>();
            stack.Push(100);

            ref int top = ref stack.Peek();
            top = 500;

            Assert.Equal(500, stack.Pop());
        }

        [Fact]
        public void PushPop_StressTest_1000Cycles()
        {
            var stack = new InlineStack32<int>();
            for (int i = 0; i < 1000; i++)
            {
                stack.Push(i);
                Assert.Equal(1, stack.Count);
                int popped = stack.Pop();
                Assert.Equal(i, popped);
                Assert.Equal(0, stack.Count);
            }
        }

        private struct TestPoint : IEquatable<TestPoint>
        {
            public int X;
            public int Y;

            public bool Equals(TestPoint other)
            {
                return X == other.X && Y == other.Y;
            }

            public override bool Equals(object? obj)
            {
                return obj is TestPoint other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(X, Y);
            }
        }

        [Fact]
        public void Stack_HandlesCustomStructs_Correctly()
        {
            var stack = new InlineStack32<TestPoint>();
            var p1 = new TestPoint { X = 1, Y = 2 };
            var p2 = new TestPoint { X = 10, Y = 20 };

            stack.Push(p1);
            stack.Push(p2);

            var top = stack.Pop();
            Assert.Equal(10, top.X);
            Assert.Equal(20, top.Y);
            Assert.Equal(1, stack.Count);
        }

        [Fact]
        public void GetEnumerator_EmptyStack_NeverIterates()
        {
            var stack = new InlineStack32<int>();
            int iterations = 0;
            foreach (var item in stack)
            {
                iterations++;
            }
            Assert.Equal(0, iterations);
        }

        [Fact]
        public void AsSpan_Modification_ReflectsInStack()
        {
            var stack = new InlineStack32<int>();
            stack.Push(1);
            stack.Push(2);

            var span = stack.AsSpan();
            span[0] = 99;

            Assert.Equal(2, stack.Pop());
            Assert.Equal(99, stack.Pop());
        }

        [Fact]
        public void GetEnumerator_ReturnsItemsInReverseOrder()
        {
            var stack = new InlineStack32<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            var expectedOrder = new[] { 3, 2, 1 };
            int i = 0;
            foreach (var item in stack)
            {
                Assert.Equal(expectedOrder[i++], item);
            }
        }

        [Fact]
        public void FullStack_Operations_AtBoundary()
        {
            var stack = new InlineStack32<int>();
            for (int i = 0; i < 32; i++) stack.Push(i);

            Assert.Equal(32, stack.Count);
            Assert.Equal(32, stack.AsSpan().Length);
            Assert.Equal(31, stack.Peek());

            Assert.Equal(31, stack.Pop());
            Assert.Equal(31, stack.Count);
        }



        private struct OddSizeStruct : IEquatable<OddSizeStruct>
        {
            public int A;
            public byte B;
            public bool Equals(OddSizeStruct other) => A == other.A && B == other.B;
        }

        [Fact]
        public void Stack_HandlesOddSizedStructs_WithoutCorruption()
        {
            var stack = new InlineStack32<OddSizeStruct>();
            var item1 = new OddSizeStruct { A = 100, B = 1 };
            var item2 = new OddSizeStruct { A = 200, B = 0 };

            stack.Push(item1);
            stack.Push(item2);

            Assert.Equal(item2, stack.Pop());
            Assert.Equal(item1, stack.Pop());
        }
    }
}