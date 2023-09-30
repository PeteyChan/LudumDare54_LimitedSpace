namespace Utils
{
    public sealed class CustomList<T> : IMGUI_ProperyDrawer
    {
        public CustomList(int capacity = 8)
        {
            items = new T[capacity.MinValue(4)];
        }

        public ref T this[int index] => ref items[index];
        T[] items;
        int count;

        public int Count => count;
        public int Capacity => items.Length;
        public void Add(T item)
        {
            if (count == items.Length) System.Array.Resize(ref items, count * 2);
            items[count] = item;
            count++;
        }

        public void Add(CustomList<T> range)
        {
            foreach (var (index, item) in range)
                Add(item);
        }

        public void Add(System.Collections.Generic.IEnumerable<T> range)
        {
            foreach (var item in range)
                Add(item);
        }

        /// <summary>
        /// returns true if item was removed, false if index was out of range or list is empty
        /// </summary>
        public bool RemoveFast(int index)
        {
            if (index < 0 || index >= count)
                return false;

            count--;
            items[index] = items[count];
            items[count] = default;
            return true;
        }

        /// <summary>
        /// returns true if item was removed, false if index was out of range or list is empty
        /// </summary>
        public bool Remove(int index)
        {
            if (index < 0 || index >= count) return false;
            count--;
            while (index < count)
            {
                items[index] = items[index + 1];
                index++;
            }
            items[count] = default;
            return true;
        }

        /// <summary>
        /// returns true if item was moved, false if index was out of range or list is empty
        /// </summary>
        public bool MoveEnd(int index)
        {
            if (index < 0 || index >= count - 1) return false;
            while (index < count - 1)
            {
                var temp = items[index + 1];
                items[index + 1] = items[index];
                items[index] = temp;
                index++;
            }
            return true;
        }

        /// <summary>
        /// returns true if item was moved, false if index was out of range or list is empty
        /// </summary>
        public bool MoveStart(int index)
        {
            if (index < 1 || index >= count) return false;
            while (index > 0)
            {
                var temp = items[index - 1];
                items[index - 1] = items[index];
                items[index] = temp;
                index--;
            }
            return true;
        }

        /// <summary>
        /// if index out of range clamps index to appropriate values
        /// </summary>
        public bool Insert(int index, T value)
        {
            index = index.Clamp(0, count);
            if (items.Length == count)
                System.Array.Resize(ref items, count * 2);
            count++;
            while (index < count)
            {
                var temp = items[index];
                items[index] = value;
                index++;
                value = temp;
            }
            return true;
        }

        public void Clear()
        {
            System.Array.Clear(items, 0, count);
            count = 0;
        }

        public Iterator GetEnumerator() => new Iterator(this);
        public ReverseIterator GetReverse() => new ReverseIterator(this);
        public struct Iterator
        {
            public Iterator(CustomList<T> list)
            {
                this.list = list;
                index = -1;
            }

            public CustomList<T> list;
            int index;
            public (int index, T item) Current => (index, list[index]);
            public bool MoveNext()
            {
                index++;
                return index < list.count;
            }
            public void Reset() => index = -1;
        }

        public struct ReverseIterator
        {
            public ReverseIterator(CustomList<T> list)
            {
                this.list = list;
                index = list.count;
            }

            CustomList<T> list;
            int index;
            public (int index, T item) Current => (index, list[index]);
            public bool MoveNext()
            {
                index--;
                return index >= 0;
            }
            public void Reset() => index = list.count;

            public ReverseIterator GetEnumerator() => this;
        }


        bool IMGUI_ProperyDrawer.DrawProperty(IMGUI_Interface gui)
        {
            var control = gui as Godot.Control;
            if (gui.Button($"{count} items"))
                control.TooltipText = control.TooltipText == "" ? "0" : "";
            bool updated = false;
            if (control.TooltipText == "")
            {
                for (int i = 0; i < count; ++i)
                    if (gui.Label(i.ToString()).Property(this[i], out this[i]))
                        updated = true;
            }
            return updated;
        }

        public void Sort(System.Collections.Generic.IComparer<T> comparer)
            => System.Array.Sort(items, 0, count, comparer);

        public void Reverse()
            => System.Array.Reverse(items, 0, count);

    }

    // class TestLists
    // {
    //     static void TestList(Debug.Console args)
    //     {
    //         var list = new CustomList<float>();
    //         var window = new ImmediateGUI_Window();
    //         float add_value = default;
    //         int move_start = default;
    //         int move_end = default;
    //         int remove_fast = default;
    //         int remove = default;
    //         int insert = default;

    //         window.OnUpdate(() =>
    //         {
    //             if (Button("Add", ref add_value))
    //                 list.Add(add_value);
    //             if (Button("Remove Fast", ref remove_fast))
    //                 list.RemoveFast(remove_fast);
    //             if (Button("Remove", ref remove))
    //                 list.Remove(remove);
    //             if (Button("Move Start", ref move_start))
    //                 list.MoveStart(move_start);
    //             if (Button("Move End", ref move_end))
    //                 list.MoveEnd(move_end);
    //             if (Button("Insert", ref insert))
    //                 list.Insert(insert, add_value);

    //             for (int i = 0; i < list.Count; ++i)
    //                 Debug.Label(i, list[i]);

    //             bool Button<T>(string label, ref T value)
    //             {
    //                 window.HBox(out var tab);
    //                 bool updated = tab.Button(label);
    //                 tab.Property(default, value, out value);
    //                 return updated;
    //             }
    //         });

    //     }
    // }
}