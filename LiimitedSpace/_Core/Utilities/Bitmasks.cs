using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Utils
{
    public static class EnumData<E> where E : struct, System.Enum
    {
        static System.Memory<string> mem_names = System.Enum.GetNames<E>();
        static System.Memory<E> mem_values = System.Enum.GetValues<E>();
        public static System.ReadOnlySpan<string> Names => mem_names.Span;
        public static System.ReadOnlySpan<E> Values => mem_values.Span;
        public static int Count => mem_values.Length;
    }

    public struct ReadonlyCollisonFlags<E> where E : struct, System.Enum
    {
        public bool this[E first, E second] => flags[first, second];
        public ReadonlyCollisonFlags(CollisonFlags<E> flags)
        {
            this.flags = flags;
        }
        CollisonFlags<E> flags;
    }

    public struct CollisonFlags<E> where E : struct, System.Enum
    {
        static CollisonFlags()
        {
            var enum_values = System.Enum.GetValues<E>();
            for (int i = 0; i < enum_values.Length; ++i)
            {
                if (System.Runtime.CompilerServices.Unsafe.As<E, int>(ref enum_values[i]) != i)
                    throw new System.Exception($"CollisonFlags can only use enums with default values : {typeof(E)} has non default values");
            }
        }

        public CollisonFlags(params (E first, E second)[] collison_pairs)
        {
            mask = new();
            foreach (var (first, second) in collison_pairs)
                this[first, second] = true;
        }

        BitMask64 mask;
        public bool this[E first, E second]
        {
            get
            {
                int index1 = System.Runtime.CompilerServices.Unsafe.As<E, int>(ref first);
                int index2 = System.Runtime.CompilerServices.Unsafe.As<E, int>(ref second);
                return mask[1 << index1 | 1 << index2];
            }

            set
            {
                int index1 = System.Runtime.CompilerServices.Unsafe.As<E, int>(ref first);
                int index2 = System.Runtime.CompilerServices.Unsafe.As<E, int>(ref second);
                mask[1 << index1 | 1 << index2] = value;
            }
        }
        public static implicit operator ReadonlyCollisonFlags<E>(CollisonFlags<E> flags) => new(flags);
    }

    public struct Flags<E> where E : struct, System.Enum
    {
        BitMask mask;
        public bool this[E index]
        {
            get => mask[System.Runtime.CompilerServices.Unsafe.As<E, int>(ref index)];
            set => mask[System.Runtime.CompilerServices.Unsafe.As<E, int>(ref index)] = value;
        }
        public Iterator GetEnumerator() => new Iterator(this);
        public struct Iterator
        {
            public Iterator(Flags<E> flags)
            {
                this.flags = flags;
                index = -1;
            }
            Flags<E> flags;
            int index;
            public bool Current => flags[EnumData<E>.Values[index]];
            public bool MoveNext()
            {
                index++;
                return index != EnumData<E>.Count;
            }
            public void Reset()
            {
                this.index = -1;
            }
        }
    }

    public struct Flags64<E> where E : struct, System.Enum
    {
        BitMask64 mask;
        public bool this[E index]
        {
            get => mask[System.Runtime.CompilerServices.Unsafe.As<E, int>(ref index)];
            set => mask[System.Runtime.CompilerServices.Unsafe.As<E, int>(ref index)] = value;
        }
    }

    public struct BitMask : Serializer.ICustomSerialization
    {
        const uint one = 1;
        uint flags;
        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= 32) return false;
                return (flags & one << index) == one << index;
            }
            set
            {
                if (index < 0 || index > 32) return;
                if (value) flags |= one << index;
                else flags &= ~(one << index);
            }
        }

        public bool Toggle(int index) => this[index] = !this[index];

        public IEnumerable<(int index, bool value)> GetEnumerator()
        {
            for (int i = 0; i < 32; ++i)
                yield return (i, this[i]);
        }

        string Serializer.ICustomSerialization.Serialize() => flags.ToString();
        void Serializer.ICustomSerialization.Deserialize(string serialized_data) => uint.TryParse(serialized_data, out flags);
        public static bool operator ==(BitMask a, BitMask b) => a.flags == b.flags;
        public static bool operator !=(BitMask a, BitMask b) => !(a == b);
        public override bool Equals([NotNullWhen(true)] object obj) => obj is BitMask m && m == this;
        public override int GetHashCode() { return base.GetHashCode(); }
    }

    public struct BitMask64 : Serializer.ICustomSerialization
    {
        const ulong one = 1;
        ulong flags;
        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= 64) return false;
                return (flags & one << index) == one << index;
            }
            set
            {
                if (index < 0 || index >= 64) return;
                if (value) flags |= one << index;
                else flags &= ~(one << index);
            }
        }

        public bool Toggle(int index) => this[index] = !this[index];

        public IEnumerable<(int index, bool value)> GetEnumerator()
        {
            for (int i = 0; i < 64; ++i)
                yield return (i, this[i]);
        }

        static void TestBitMask(Debug.Console args)
        {
            IMGUI_Window window = new ();
            var mask = new BitMask64();

            window.OnProcess(() =>
            {
                foreach (var (index, value) in mask.GetEnumerator())
                {
                    window.HBox(out var box);
                    box.Label(index);
                    if (box.Button(value.ToString()))
                        mask[index] = !value;
                    if (box.Button("Toggle"))
                        mask.Toggle(index);
                }
            });
        }
        string Serializer.ICustomSerialization.Serialize() => flags.ToString();
        void Serializer.ICustomSerialization.Deserialize(string serialized_data) => ulong.TryParse(serialized_data, out flags);
        public static bool operator ==(BitMask64 a, BitMask64 b) => a.flags == b.flags;
        public static bool operator !=(BitMask64 a, BitMask64 b) => !(a == b);
        public override bool Equals([NotNullWhen(true)] object obj) => obj is BitMask64 m && m == this;
        public override int GetHashCode() { return base.GetHashCode(); }
        public static explicit operator ulong(BitMask64 mask) => mask.flags;
        public static explicit operator BitMask64(ulong mask) => new BitMask64 { flags = mask };
    }

    public struct BitMask128 : Serializer.ICustomSerialization
    {
        BitMask64 first, second;

        public bool this[int index]
        {
            get => index < 0 ? false : index >= 128 ? false : index < 64 ? first[index] : second[index - 64];
            set
            {
                if (index < 0) return;
                if (index >= 128) return;
                if (index < 64) first[index] = value;
                else second[index - 64] = value;
            }
        }

        public bool Toggle(int index) => this[index] = !this[index];

        public static bool operator ==(BitMask128 a, BitMask128 b) => a.first == b.first && a.second == b.second;
        public static bool operator !=(BitMask128 a, BitMask128 b) => !(a == b);
        public override bool Equals([NotNullWhen(true)] object obj) => obj is BitMask128 bitmask && bitmask == this;
        public override int GetHashCode() => base.GetHashCode();
        string Serializer.ICustomSerialization.Serialize() => $"{(ulong)first} {(ulong)second}";
        void Serializer.ICustomSerialization.Deserialize(string serialized_data)
        {
            var items = serialized_data.Split(' ');
            if (items.Length != 2) return;
            if (ulong.TryParse(items[0], out var flags)) first = (BitMask64)flags;
            if (ulong.TryParse(items[1], out flags)) second = (BitMask64)flags;
        }

        public IEnumerable<(int index, bool value)> GetEnumerator()
        {
            for (int i = 0; i < 128; ++i)
                yield return (i, this[i]);
        }
    }

    public struct BitMask256 : Serializer.ICustomSerialization
    {
        BitMask128 first, second;

        public bool this[int index]
        {
            get => index < 0 ? false : index >= 256 ? false : index < 128 ? first[index] : second[index - 128];
            set
            {
                if (index < 0) return;
                if (index >= 256) return;
                if (index < 128) first[index] = value;
                else second[index - 128] = value;
            }
        }

        public bool Toggle(int index) => this[index] = !this[index];

        public static bool operator ==(BitMask256 a, BitMask256 b) => a.first == b.first && a.second == b.second;
        public static bool operator !=(BitMask256 a, BitMask256 b) => !(a == b);
        public override bool Equals([NotNullWhen(true)] object obj) => obj is BitMask256 bitmask && bitmask == this;
        public override int GetHashCode() => base.GetHashCode();
        string Serializer.ICustomSerialization.Serialize()
        {
            Utils.Serializer.TrySerialize(first, out var s1);
            Utils.Serializer.TrySerialize(second, out var s2);
            return $"{s1} {s2}";
        }
        void Serializer.ICustomSerialization.Deserialize(string serialized_data)
        {
            var items = serialized_data.Split(' ');
            if (items.Length != 2) return;
            if (Utils.Serializer.TryDeserialize(items[0], out var data))
                first = (BitMask128)data;
            if (Utils.Serializer.TryDeserialize(items[1], out data))
                second = (BitMask128)data;
        }

        public IEnumerable<(int index, bool value)> GetEnumerator()
        {
            for (int i = 0; i < 256; ++i)
                yield return (i, this[i]);
        }
    }
}