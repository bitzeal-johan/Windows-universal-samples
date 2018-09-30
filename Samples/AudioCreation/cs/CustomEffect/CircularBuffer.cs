using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomEffect
{
    public sealed class CircularBuffer
    {
        private int _capacity;
        private float[] _storage;
        private int _head;
        private int _tail;

        public CircularBuffer(int capacity)
        {
            _capacity = capacity;
            _storage = new float[capacity + 1];
        }

        public int Count => _head - _tail;

        public void Add(float f)
        {
            _storage[_head++] = f;

            WrapIfNeeded(ref _head);

            if (_head == _tail)
            {
                throw new Exception("Overflow when adding to circular buffer");
            }
        }

        public float Remove()
        {
            if (_head == _tail)
            {
                throw new Exception("Overflow when removing from circular buffer");
            }

            float value = _storage[_tail++];

            WrapIfNeeded(ref _tail);

            return value;
        }

        private void WrapIfNeeded(ref int index)
        {
            if (index > _capacity)
            {
                index = 0;
            }
        }
    }
}
