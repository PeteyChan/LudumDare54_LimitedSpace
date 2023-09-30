using System.Collections;
using System.Collections.Generic;

namespace Utils
{
    public struct ReadonlyEnumMap<E, T>
        where E : struct, System.Enum
    {
        EnumMap<E, T> map;
        public T this[E index] => map[index];
        public E this[int index] => map[index];
        public ReadonlyEnumMap(EnumMap<E, T> map) => this.map = map;
        public EnumMap<E, T>.Iterator GetEnumerator() => new EnumMap<E, T>.Iterator(map);
    }

    // only works with default enum values
    public class EnumMap<E, T> : Serializer.ICustomSerialization, IMGUI_ProperyDrawer
    where E : struct, System.Enum
    {
        static readonly E[] enum_values = System.Enum.GetValues<E>();
        static EnumMap()
        {
            for (int i = 0; i < enum_values.Length; ++i)
            {
                if (System.Runtime.CompilerServices.Unsafe.As<E, int>(ref enum_values[i]) != i)
                    throw new System.Exception($"Enum map can only use enums with default values : {typeof(E)} has non default values");
            }
        }
        public ref T this[E index] => ref values[System.Runtime.CompilerServices.Unsafe.As<E, int>(ref index)];
        public T this[E? index]
        {
            get => index.HasValue ? this[index.Value] : default;
            set { if (index.HasValue) this[index.Value] = value; }
        }
        public E this[int index] => enum_values[index];
        public int Length => values.Length;
        T[] values = new T[enum_values.Length];
        public EnumMap() { }

        /// <summary>
        /// init's enum map values using init function
        /// </summary>
        public EnumMap(System.Func<E, T> init_function)
        {
            foreach (var e in enum_values)
                this[e] = init_function.Invoke(e);
        }

        public EnumMap(EnumMap<E, T> to_copy)
        {
            foreach (var value in enum_values)
                this[value] = to_copy[value];
        }

        public bool TrySet(string item, object value)
        {
            if (value is T type && System.Enum.TryParse<E>(item, out E result))
            {
                this[result] = type;
                return true;
            }
            return false;
        }

        public Iterator GetEnumerator() => new Iterator(this);
        public System.ReadOnlySpan<E> GetKeys() => enum_values;
        public System.ReadOnlySpan<T> GetValues() => values;
        public void CopyValues(EnumMap<E, T> target)
        {
            foreach (var item in target)
                this[item.item] = item.value;
        }
        public EnumMap<E, T> Copy() => new(e => this[e]);
        public static implicit operator bool(EnumMap<E, T> map) => map?.values != null;

        public struct Iterator  // on stack so no garbage
        {
            public Iterator(EnumMap<E, T> map)
            {
                this.map = map;
                index = -1;
            }
            EnumMap<E, T> map;
            int index;
            public (E item, T value) Current => (enum_values[index], map.values[index]);
            public bool MoveNext()
            {
                index++;
                return index != map.values.Length;
            }
            public void Reset()
            {
                this.index = -1;
            }
        }

        public static implicit operator ReadonlyEnumMap<E, T>(EnumMap<E, T> map) => new(map);

        string Utils.Serializer.ICustomSerialization.Serialize()
        {
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < values.Length; ++i)
            {
                if (Utils.Serializer.TrySerialize(enum_values[i], out var serialized_enum)
                && Utils.Serializer.TrySerialize(values[i], out var serialized_value))
                {
                    builder.Append(enum_values[i]);
                    builder.Append(" ");
                    builder.Append(serialized_value);
                    builder.Append(Utils.Serializer.DefaultSeparator);
                }
            }
            return builder.ToString();
        }

        void Utils.Serializer.ICustomSerialization.Deserialize(string serialized_data)
        {
            var items = serialized_data.Split(Utils.Serializer.DefaultSeparator);
            foreach (var item in items)
            {
                var options = item.Split(" ", 2);
                if (System.Enum.TryParse<E>(options[0], out E enum_value)
                    && Utils.Serializer.TryDeserialize(options[1], out var deserialized_value)
                    && deserialized_value is T value)
                {
                    this[enum_value] = value;
                }
            }
        }

        bool IMGUI_ProperyDrawer.DrawProperty(IMGUI_Interface gui)
        {
            bool updated = false;
            gui.Panel(out var panel);
            var node = panel as Godot.Control;
            bool toggle = panel.Button((node.TooltipText == "" ? "Show" : "Hide"));
            if (toggle) node.TooltipText = node.TooltipText == "" ? "0" : "";
            if (node.TooltipText == "0")
            {
                foreach (var (item, value) in this)
                {
                    if (panel.Label(item.ToString()).Property(value, out this[item]))
                        updated = true;
                }
            }
            return updated;
        }
    }
}