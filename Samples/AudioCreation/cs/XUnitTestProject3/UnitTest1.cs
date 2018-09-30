using System;
using CustomEffect;
using Xunit;

namespace XUnitTestProject3
{
    public class UnitTest1
    {
        [Fact]
        public void WhenNewThenCountIsZero()
        {
            var subject = new CircularBuffer(3);

            Assert.Equal(0, subject.Count);
        }

        [Fact]
        public void WhenOneItemAddedThenCountIsOne()
        {
            // ARRANGE
            var subject = new CircularBuffer(3);

            // ACT
            subject.Add(1.0f);

            // ASSERT
            Assert.Equal(1, subject.Count);
        }

        [Theory]
        [InlineData(1, 0, 1)]
        [InlineData(2, 1, 1)]
        [InlineData(3, 1, 2)]
        public void WhenAddingXAndRemovingYThenCountIsZ(int add, int remove, int sum)
        {
            // ARRANGE
            var subject = new CircularBuffer(3);

            // ACT
            for (var i = 0; i < add; i++)
                subject.Add(1.0f);

            for (var i = 0; i < remove; i++)
                subject.Remove();

            // ASSERT
            Assert.Equal(sum, subject.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(9)]
        public void WhenAddingOneMoreThanRemovedNTimesThenCountIsOne(int repeat)
        {
            // ARRANGE
            var subject = new CircularBuffer(3);

            // ACT
            subject.Add(1.0f);
            for (var i = 0; i < repeat; i++)
            {
                subject.Remove();
                subject.Add(1.0f);
            }

            // ASSERT
            Assert.Equal(1, subject.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public void WhenAddingCapacityCountShallBeCapacity(int capacity)
        {
            // ARRANGE
            var subject = new CircularBuffer(capacity);

            // ACT
            for (var i = 0; i < capacity; i++)
                subject.Add(1.0f);

            // ASSERT
            Assert.Equal(capacity, subject.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public void WhenAddingOneMoreThanCapacityThenShallThrow(int capacity)
        {
            // ARRANGE
            var subject = new CircularBuffer(capacity);

            // ACT
            var ex = Record.Exception(() =>
            {
                for (var i = 0; i < 1 + capacity; i++)
                    subject.Add(1.0f);

            });

            // ASSERT
            Assert.NotNull(ex);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(21)]
        public void WhenRemovingOneMoreThanAddedThenShallThrow(int addAndRemoveTimes)
        {
            // ARRANGE
            var subject = new CircularBuffer(10);

            // ACT
            var ex = Record.Exception(() =>
            {
                for (var i = 0; i < addAndRemoveTimes; i++)
                {
                    subject.Add(1.0f);
                    subject.Remove();
                }

                subject.Remove(); // One extra time
            });

            // ASSERT
            Assert.NotNull(ex);
        }

        [Fact]
        public void RemovedDataShallBeTheSameAsAddedData()
        {
            var subject = new CircularBuffer(3);

            subject.Add(1f);
            subject.Add(2f);
            subject.Add(3f);

            Assert.Equal(1f, subject.Remove());
            Assert.Equal(2f, subject.Remove());
            Assert.Equal(3f, subject.Remove());

            // Buffer wraps

            subject.Add(4f);
            subject.Add(5f);
            subject.Add(6f);

            Assert.Equal(4f, subject.Remove());
            Assert.Equal(5f, subject.Remove());
            Assert.Equal(6f, subject.Remove());
        }
    }
}
