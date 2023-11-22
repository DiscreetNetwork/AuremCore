using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    /// <summary>
    /// A queue that can support access to values via indexing. Values can be removed using RemoveAt(). Values cannot be inserted.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IndexQueue<T> : IEnumerable<T>
    {
        private const int defaultCapacity = 32;
        private T[] buf;
        private int _offset = 0;
        private bool isFixedSize = false;

        //public void PrintState()
        //{
        //    Console.Write("[");
        //    for (int i = 0; i < buf.Length; i++)
        //    {
        //        Console.Write($"{buf[i]}");
        //        if (i < buf.Length - 1)
        //        {
        //            Console.Write(" ");
        //        }
        //    }
        //    Console.WriteLine("]");
        //}


        public IndexQueue(int maxCapacity, bool fixedSize) : this(maxCapacity)
        {
            isFixedSize = fixedSize;
        }

        public IndexQueue(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            buf = new T[capacity];
        }

        public IndexQueue(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            var source = items.ToArray();
            var count = source.Length;

            if (count > 0)
            {
                buf = source;
            }
            else
            {
                buf = new T[defaultCapacity];
            }
        }

        public bool IsEmpty => Count == 0;

        public int Count { get; private set; }

        public bool IsFull => Count == Capacity;

        private bool IsSplit => _offset + Count > Capacity;

        public int Capacity
        {
            get
            {
                return buf.Length;
            }

            private set
            {
                if (isFixedSize) throw new Exception("cannot change capacity");

                if (value < Count) throw new ArgumentOutOfRangeException(nameof(value));

                if (value == buf.Length) return;

                T[] newBuf = new T[value];

                if (IsSplit)
                {
                    int len = Capacity - _offset;
                    Array.Copy(buf, _offset, newBuf, 0, len);
                    Array.Copy(buf, 0, newBuf, len, Count - len);
                }
                else
                {
                    Array.Copy(buf, _offset, newBuf, 0, buf.Length);
                }

                _offset = 0;
                buf = newBuf;
            }
        }

        public IndexQueue() : this(defaultCapacity) { }

        public T this[int index]
        {
            get
            {
                if (index < 0) throw new ArgumentOutOfRangeException();
                if (index >= Count) throw new ArgumentOutOfRangeException();

                return buf[(index + _offset) % Capacity];
            }
        }

        public void Enqueue(T item)
        {
            if (IsFull)
            {
                if (isFixedSize) return;

                Capacity = (Capacity == 0) ? defaultCapacity : 2 * Capacity;
            }

            buf[(Count + _offset) % Capacity] = item;
            ++Count;

            //PrintState();
        }

        public T Dequeue()
        {
            if (IsEmpty)
            {
                throw new Exception("empty");
            }

            _offset += 1;
            if (_offset >= Capacity) _offset -= Capacity;

            var res = buf[(Count + _offset) % Capacity];
            --Count;

            //PrintState();

            return res;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));

            if (index == 0)
            {
                Dequeue();
                return;
            }
            else if (index == Count - 1)
            {
                --Count;

                //PrintState();

                return;
            }

            for (int i = index; i < Count; i++)
            {
                buf[(i + _offset) % Capacity] = buf[(i + 1 + _offset) % Capacity];
            }

            //PrintState();
            Count--;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                yield return this[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            _offset = 0;
            Count = 0;
        }
    }
}
