namespace Utils
{
    public sealed class RingBuffer<T>
    {
        public RingBuffer(int capacity)
        {
            buffer = new T[capacity + 1];
        }

        T Default = default;
        public ref T this[int index]
        {
            get
            {
                index = (start_index + index) % buffer.Length;
                if (index >= end_index && index < start_index)
                    return ref buffer[index - end_index + start_index];
                return ref buffer[index];
            }
        }


        T[] buffer;
        int start_index, end_index;

        public RingBuffer<T> Add(T item)
        {
            buffer[end_index] = item;
            end_index++;
            if (end_index == buffer.Length)
                end_index = 0;
            if (end_index == start_index)
            {
                start_index++;
                if (start_index == buffer.Length)
                    start_index = 0;
            }
            return this;
        }

        /// <summary>
        /// Removes matching item starting with the oldest
        /// </summary>
        public RingBuffer<T> Remove(T item)
        {
            int? swap_index = default;
            int current_index = start_index;
            var comparer = System.Collections.Generic.EqualityComparer<T>.Default;

            while (current_index != end_index)
            {
                if (swap_index.HasValue)
                {
                    buffer[swap_index.Value] = buffer[current_index];
                    swap_index = current_index;
                }
                else if (comparer.Equals(buffer[current_index], item))
                    swap_index = current_index;

                current_index++;
                if (current_index == buffer.Length)
                    current_index = 0;
            }

            if (swap_index.HasValue)
            {
                end_index--;
                if (end_index < 0)
                    end_index = buffer.Length - 1;
            }
            return this;
        }

        public int Count
        {
            get
            {
                if (end_index > start_index)
                    return end_index - start_index;

                if (end_index < start_index)
                    return buffer.Length - start_index + end_index;

                return 0;
            }
        }

        public RingBuffer<T> Clear()
        {
            System.Array.Clear(buffer);
            start_index = end_index = 0;
            return this;
        }

        public bool Contains(T item)
        {
            var comparer = System.Collections.Generic.EqualityComparer<T>.Default;
            foreach (var test in this)
                if (comparer.Equals(item, test))
                    return true;
            return false;
        }

        public Iterator GetEnumerator() => new Iterator(this);

        public struct Iterator  // on stack so no garbage
        {
            public Iterator(RingBuffer<T> ring_buffer)
            {
                this.ring_buffer = ring_buffer;
                index = ring_buffer.start_index - 1;
            }

            RingBuffer<T> ring_buffer;
            int index;

            public T Current => ring_buffer.buffer[index];
            public bool MoveNext()
            {
                index++;
                if (index == ring_buffer.buffer.Length)
                    index = 0;
                return index != ring_buffer.end_index;
            }

            public void Reset()
            {
                this.index = ring_buffer.start_index - 1;
            }
        }

        public T[] ToArray()
        {
            T[] array = new T[Count];
            for (int i = 0; i < Count; ++i)
                array[i] = this[i];
            return array;
        }

        public RingBuffer<T> Resize(int capacity)
        {
            var temp = new RingBuffer<T>(capacity); // so lazy but simple xD

            foreach (var item in this)
                temp.Add(item);

            buffer = temp.buffer;
            start_index = temp.start_index;
            end_index = temp.end_index;
            return this;
        }

        public bool AtCapacity => Count == buffer.Length - 1;
    }

    /*
    class testing
    {
        static RingBuffer<Inputs> ringbuffer = new(8);
        static void Update(Bootstrap.Process args)
        {
            foreach (var item in System.Enum.GetValues<Inputs>())
                if (item.isNumpad())
                {
                    if (Inputs.key_shift.Pressed() && item.OnPressed())
                    {
                        ringbuffer.Remove(item);
                    }
                    else if (item.OnPressed())
                        ringbuffer.Add(item);
                }

            Debug.Label("buffer count = ", ringbuffer.Count);
            int count = 0;
            foreach (var item in ringbuffer)
            {
                Debug.Label(item, ringbuffer[count]);
                count++;
            }
        }

        static void ClearRing(Debug.Console args) => ringbuffer.Clear();
    }
    /**/
}