using System;
using System.Collections.Generic;
using System.Text;

namespace LogManager.Utils
{
    internal class CircularBuffer<T>
    {
        public T[] Data;
        private int _index;
        public CircularBuffer(int size)
        {
            Data = new T[size];
        }

        private int _getBoundedIndex(int unbound)
        {
            int byMax = unbound % Data.Length;
            if (byMax < 0) return Data.Length + byMax;
            else return byMax;
        }

        public void Hold(T item)
        {
            int index = _getBoundedIndex(_index);
            Data[index] = item;
        }

        public void Push(T item)
        {
            int index = _getBoundedIndex(_index);
            Data[index] = item;
            _index++;
        }

        public T Pull(int back = 0)
        {
            int unbound = _index - back;
            int index = _getBoundedIndex(unbound);
            return Data[index];
        }
    }
}
