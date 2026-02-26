using System;
using Xunit;
using InlineCollections;

namespace InlineCollections.Tests
{
    public class InlineList32Tests
    {
        [Fact]
        public void Constructor_InitializesEmptyList()
        {
            var list = new InlineList32<int>();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void TryAdd_SingleItem_Succeeds()
        {
            var list = new InlineList32<int>();
            bool result = list.TryAdd(42);
            Assert.True(result);
            Assert.Equal(1, list.Count);
        }

        [Fact]
        public void TryAdd_MultipleItems_AllSucceed()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++)
            {
                bool result = list.TryAdd(i);
                Assert.True(result);
            }

            Assert.Equal(32, list.Count);
        }

        [Fact]
        public void TryAdd_ExceedCapacity_ReturnsFalse()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++)
            {
                list.TryAdd(i);
            }

            bool result = list.TryAdd(999);
            Assert.False(result);
            Assert.Equal(32, list.Count);
        }

        [Fact]
        public void Add_WithinCapacity_Succeeds()
        {
            var list = new InlineList32<int>();
            list.Add(10);
            list.Add(20);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public void TryAdd_ExceedCapacity_ReturnsFalse_AndPreservesState()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++) list.Add(i);

            bool result = list.TryAdd(999);

            Assert.False(result);
            Assert.Equal(32, list.Count);
        }

        [Fact]
        public void AddRange_MultipleItems_Succeeds()
        {
            var list = new InlineList32<int>();
            ReadOnlySpan<int> items = new int[] { 1, 2, 3, 4, 5 };
            list.AddRange(items);

            Assert.Equal(5, list.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(i + 1, list[i]);
            }
        }

        [Fact]
        public void AddRange_ExceedCapacity_ThrowsInvalidOperationException()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 30; i++) list.Add(i);
            int[] items = new int[] { 1, 2, 3, 4 };

            bool threw = false;

            try { list.AddRange(items); }
            catch (InvalidOperationException) { threw = true; }

            Assert.True(threw, "AddRange should throw InvalidOperationException when capacity is exceeded.");
        }

        [Fact]
        public void Insert_AtBeginning_ShiftsAllItems()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);

            list.Insert(0, 100);

            Assert.Equal(3, list.Count);
            Assert.Equal(100, list[0]);
            Assert.Equal(1, list[1]);
            Assert.Equal(2, list[2]);
        }

        [Fact]
        public void Insert_AtEnd_WorksLikeAdd()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Insert(1, 2);

            Assert.Equal(2, list.Count);
            Assert.Equal(2, list[1]);
        }

        [Fact]
        public void Insert_IntoEmptyList_Succeeds()
        {
            var list = new InlineList32<int>();
            list.Insert(0, 42);

            Assert.Equal(1, list.Count);
            Assert.Equal(42, list[0]);
        }

        [Fact]
        public void Insert_BeforeLastItem_ShiftsOnlyLast()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);

            list.Insert(1, 99);

            Assert.Equal(3, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(99, list[1]);
            Assert.Equal(2, list[2]);
        }

        [Fact]
        public void TryInsert_InvalidIndex_ReturnsFalse()
        {
            var list = new InlineList32<int>();
            list.Add(1);

            bool result = list.TryInsert(5, 100);

            Assert.False(result);
            Assert.Equal(1, list.Count);
        }

        [Fact]
        public void TryInsert_FullList_ReturnsFalse()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++) list.Add(i);

            bool result = list.TryInsert(10, 999);

            Assert.False(result);
        }

        [Fact]
        public void TryInsert_AtCountIndex_WhenFull_ReturnsFalse()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++) list.Add(i);

            bool result = list.TryInsert(32, 999);

            Assert.False(result);
        }

        [Fact]
        public void TryInsert_AtCountIndex_WhenNotFull_Succeeds()
        {
            var list = new InlineList32<int>();
            list.Add(1);

            bool result = list.TryInsert(1, 2);

            Assert.True(result);
            Assert.Equal(2, list.Count);
            Assert.Equal(2, list[1]);
        }

        [Fact]
        public void Indexer_Get_ReturnsCorrectValue()
        {
            var list = new InlineList32<int>();
            list.Add(100);
            Assert.Equal(100, list[0]);
        }

        [Fact]
        public void Indexer_Set_ModifiesValue()
        {
            var list = new InlineList32<int>();
            list.Add(100);
            list[0] = 200;
            Assert.Equal(200, list[0]);
        }

        [Fact]
        public void Indexer_AccessAtBoundary_WorksCorrectly()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++) list.Add(i * 10);

            Assert.Equal(0, list[0]);
            Assert.Equal(310, list[31]);
        }

        [Fact]
        public void RemoveAt_ValidIndex_RemovesItem()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.RemoveAt(1);
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(3, list[1]);
        }

        [Fact]
        public void RemoveAt_FirstItem_Succeeds()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);
            list.RemoveAt(0);

            Assert.Equal(1, list.Count);
            Assert.Equal(2, list[0]);
        }

        [Fact]
        public void RemoveAt_LastItem_Succeeds()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.RemoveAt(2);

            Assert.Equal(2, list.Count);
        }

        [Fact]
        public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            var list = new InlineList32<int>();
            list.Add(10);

            bool threw = false;
            try
            {
                list.RemoveAt(5);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }
            Assert.True(threw, "RemoveAt should throw ArgumentOutOfRangeException for invalid index.");
        }

        [Fact]
        public void RemoveAt_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            var list = new InlineList32<int>();
            list.Add(10);

            bool threw = false;
            try
            {
                list.RemoveAt(-1);
            }

            catch (ArgumentOutOfRangeException) { threw = true; }
            Assert.True(threw);
        }

        [Fact]
        public void Remove_NonExistingItem_ReturnsFalse()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);

            bool result = list.Remove(999);

            Assert.False(result);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public void Remove_ItemExists_RemovesAndReturnsTrue()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            bool result = list.Remove(2);
            Assert.True(result);
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(3, list[1]);
        }

        [Fact]
        public void Remove_ItemNotExists_ReturnsFalse()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            bool result = list.Remove(999);

            Assert.False(result);
            Assert.Equal(1, list.Count);
        }

        [Fact]
        public void Remove_DuplicateItems_RemovesFirstOccurrence()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);
            list.Add(2);
            list.Add(3);

            list.Remove(2);
            Assert.Equal(3, list.Count);
            Assert.Equal(2, list[1]);
        }

        [Fact]
        public void Contains_ItemExists_ReturnsTrue()
        {
            var list = new InlineList32<int>();
            list.Add(42);
            Assert.True(list.Contains(42));
        }

        [Fact]
        public void Contains_ItemNotExists_ReturnsFalse()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            Assert.False(list.Contains(999));
        }

        [Fact]
        public void Clear_RemovesAllItems()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.Clear();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void AsSpan_ReturnsValidSpan()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            var span = list.AsSpan();
            Assert.Equal(3, span.Length);
            Assert.Equal(1, span[0]);
            Assert.Equal(2, span[1]);
            Assert.Equal(3, span[2]);
        }

        [Fact]
        public void GetEnumerator_IteratesAllElements()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            int count = 0;
            foreach (var item in list)
            {
                count++;
            }

            Assert.Equal(3, count);
        }

        [Fact]
        public void Capacity_IsAlways32()
        {
            Assert.Equal(32, InlineList32<int>.Capacity);
        }

        [Fact]
        public void RemoveAt_Middle_ShiftsRemainingItemsCorrectly()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 5; i++) list.Add(i);

            list.RemoveAt(2);

            Assert.Equal(4, list.Count);
            Assert.Equal(0, list[0]);
            Assert.Equal(1, list[1]);
            Assert.Equal(3, list[2]);
            Assert.Equal(4, list[3]);
        }

        [Fact]
        public void RemoveAt_ZeroIndex_UntilEmpty()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++) list.Add(i);

            for (int i = 0; i < 32; i++)
            {
                list.RemoveAt(0);
            }

            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void AddRange_Exactly32Items_Succeeds()
        {
            var list = new InlineList32<int>();
            Span<int> items = stackalloc int[32];
            for (int i = 0; i < 32; i++) items[i] = i;

            int[] arr = items.ToArray();
            list.AddRange(arr);

            Assert.Equal(32, list.Count);
            Assert.Equal(31, list[31]);
        }

        [Fact]
        public void AsSpan_Modification_ReflectsInList()
        {
            var list = new InlineList32<int>();
            list.Add(10);

            var span = list.AsSpan();
            span[0] = 100;

            Assert.Equal(100, list[0]);
        }

        [Fact]
        public void Indexer_Set_AtLastIndex_Works()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++) list.Add(0);

            list[31] = 777;
            Assert.Equal(777, list[31]);
        }

        [Fact]
        public void Remove_NonExistentItem_ReturnsFalseAndKeepsState()
        {
            var list = new InlineList32<int>();
            list.Add(1);
            list.Add(2);

            bool result = list.Remove(3);

            Assert.False(result);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public void Indexer_ReturnsRef_ModifiesOriginalValue()
        {
            var list = new InlineList32<int>();
            list.Add(10);


            ref int itemRef = ref list[0];
            itemRef = 500;

            Assert.Equal(500, list[0]);
        }

        [Fact]
        public void EmptyList_Operations_ShouldBeSafe()
        {
            var list = new InlineList32<int>();

            Assert.False(list.Remove(42));
            Assert.False(list.Contains(42));
            Assert.True(list.AsSpan().IsEmpty);


            int count = 0;
            foreach (var item in list) count++;
            Assert.Equal(0, count);
        }

        [Fact]
        public void FullList_RemoveAtLastItem_Succeeds()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++) list.Add(i);

            list.RemoveAt(31);

            Assert.Equal(31, list.Count);
            Assert.Equal(30, list[30]);
        }

        [Fact]
        public void FullList_RemoveAtFirstItem_ShiftsCorrectly()
        {
            var list = new InlineList32<int>();
            for (int i = 0; i < 32; i++) list.Add(i);

            list.RemoveAt(0);

            Assert.Equal(31, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(31, list[30]);
        }

        [Fact]
        public void Constructor_WithSpan_CopiesDataCorrectly()
        {
            ReadOnlySpan<int> source = stackalloc int[] { 10, 20, 30 };
            var list = new InlineList32<int>(source);

            Assert.Equal(3, list.Count);
            Assert.Equal(30, list[2]);
        }

        [Fact]
        public void Constructor_ExceedCapacity_ThrowsInvalidOperationException()
        {
            ReadOnlySpan<int> source = stackalloc int[33];
            bool threw = false;
            try
            {
                var list = new InlineList32<int>(source);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert.True(threw);
        }
    }
}