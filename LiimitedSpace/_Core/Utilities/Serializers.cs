namespace Utils
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;

    public static class Serializer
    {
        public static bool TrySaveToFile<T>(string path, T item)
        {
            List<string> to_serialize = new List<string>();
            to_serialize.Add(item.GetType().ToString());
            foreach (var field in item.GetType().GetFields())
            {
                if (TrySerialize(field.GetValue(item), out var serialized_data))
                    to_serialize.Add($"{field.Name}:{serialized_data}");
                else Debug.LogWarning("Failed to serialize field", field.Name, field.FieldType);
            }

            if (path.StartsWith("res://") || path.StartsWith("user://"))
                using (var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write))
                    foreach (var line in to_serialize)
                        file.StoreLine(line);
            else System.IO.File.WriteAllLines(path, to_serialize);
            return true;
        }

        public static bool TryLoadFromFile<T>(string path, out T item) where T : new()
        {
            item = default;
            T target = default;
            Type type = default;

            if (path.StartsWith("res://") || path.StartsWith("user://"))
            {
                if (!Godot.FileAccess.FileExists(path)) return false;
                using (var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read))
                    while (!file.EofReached())
                        if (!DeserializeLine(file.GetLine()))
                            return false;
            }
            else if (System.IO.File.Exists(path))
                foreach (var line in System.IO.File.ReadLines(path))
                    if (!DeserializeLine(line))
                        return false;

            item = target;
            return type != null;

            bool DeserializeLine(string line)
            {
                if (target == null)
                {
                    type = Type.GetType(line);
                    if (type == null) type = typeof(Godot.Node).Assembly.GetType(line);
                    if (type != null)
                    {
                        if (System.Activator.CreateInstance(type) is T target_type)
                        {
                            target = target_type;
                            return true;
                        }

                        Debug.LogError("Type mismatch:", type, "is not assignable to", typeof(T));
                    }
                    Debug.LogError("Failed to Deserialize:", typeof(T), "->", line);
                    return false;
                }

                if (string.IsNullOrEmpty(line)) return true;

                var data = line.Split(':', 2);
                if (data.Length != 2)
                {
                    Debug.LogError("Failed to Deserialize line:  ", line);
                    return true;
                }

                if (TryDeserialize(data[1], out var deserialized_data))
                {
                    var field = type.GetField(data[0]);
                    if (field.FieldType == deserialized_data.GetType())
                    {
                        field.SetValue(target, deserialized_data);
                    }
                    else Debug.LogWarning("Field type Mismatch:", type, "->", field.Name, ":", field.FieldType, "!=", deserialized_data.GetType());
                }
                else
                {
                    Debug.LogWarning("Failed to deserialize field", data[0], data[1]);
                }
                return true;
            }
        }

        static Serializer()
        {
            List<ICustomSerialization> serializers = new List<ICustomSerialization>();
            foreach (var type in typeof(ICustomSerialization).GetAllImplementors())
                serializers.Add(System.Activator.CreateInstance(type) as ICustomSerialization);
            custom_serializers = serializers.ToArray();
        }
        static ICustomSerialization[] custom_serializers;

        /// <summary>
        /// implement on class for the class to use the default serializer
        /// </summary>
        public interface IUseDefaultSerialization { }

        /// <summary>
        /// implement on class or struct to customize the serializer
        /// </summary>
        public interface ICustomSerialization
        {
            /// <summary>
            /// return null to fail serialization
            /// </summary>
            string Serialize();
            void Deserialize(string serialized_data);
        }

        public static bool TrySerialize(object data, out string serialized_data)
        {
            serialized_data = default;
            if (data == null) return false;
            Separators.depth++;
            var to_serialize = Default_Serializer(data);
            Separators.depth--;

            if (to_serialize.identifier != null)
            {
                serialized_data = $"{to_serialize.identifier}={to_serialize.data}";
                return true;
            }
            return false;
        }

        public static string DefaultSeparator => Separators.struct_separator;

        static class Separators
        {
            public const string return_separator = "↳";
            public static string struct_separator => $" {(depth == 0 ? "" : depth.ToString())}⇁";
            public static int depth = -1;
        }

        public static bool TryDeserialize(string data, out object deserialized_data)
        {
            deserialized_data = default;
            if (data == null) return false;
            var split = data.Split('=', 2);
            if (split.Length != 2) return false;
            var identifier = split[0];
            var serialized_data = split[1];

            Separators.depth++;
            deserialized_data = Default_Deserializer(identifier, serialized_data);
            Separators.depth--;
            return deserialized_data != null;
        }

        static (string identifier, string data) Default_Serializer(object data)
        {
            switch (data)
            {
                case System.Half half_val: return ("f16", half_val.ToString("0.##"));
                case float float_val: return ("f32", float_val.ToString("0.####"));
                case double double_val: return ("f64", double_val.ToString("0.########"));
                case bool: return ("bool", data.ToString());
                case byte: return ("byte", data.ToString());
                case sbyte: return ("sbyte", data.ToString());
                case short: return ("i16", data.ToString());
                case ushort: return ("u16", data.ToString());
                case int: return ("i32", data.ToString());
                case uint: return ("u32", data.ToString());
                case long: return ("i64", data.ToString());
                case ulong: return ("u64", data.ToString());
                case string string_val: return ("str", string_val.Replace("\n", Separators.return_separator));
                case System.Enum: return (data.GetType().ToString(), data.ToString());
                case System.Guid: return ("guid", data.ToString());

                case Godot.Vector2 vec2_val: return ("vec2", $"{vec2_val.X} {vec2_val.Y}");
                case Godot.Vector3 vec3_val: return ("vec3", $"{vec3_val.X} {vec3_val.Y} {vec3_val.Z}");
                case Godot.Vector4 vec4_val: return ("vec3", $"{vec4_val.X} {vec4_val.Y} {vec4_val.Z} {vec4_val.W}");
                case Godot.Color color: return ("color", $"{color.R} {color.G} {color.B} {color.A}");

                case ICustomSerialization custom: return (custom.GetType().ToString(), custom.Serialize());

                case System.Collections.IList list:
                    {
                        var list_type = list.GetType();
                        if (list_type.IsArray)
                        {

                            var builder = new System.Text.StringBuilder();
                            builder.Append(list.Count);
                            builder.Append(Separators.struct_separator);
                            for (int i = 0; i < list.Count; ++i)
                            {
                                builder.Append(i);
                                builder.Append(":");
                                if (TrySerialize(list[i], out var serialized_data))
                                    builder.Append(serialized_data);
                                builder.Append(Separators.struct_separator);
                            }
                            return (list_type.ToString(), builder.ToString());
                        }
                        if (list_type.IsGenericType && list_type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                        {
                            var builder = new System.Text.StringBuilder();
                            foreach (var item in list)
                            {
                                if (TrySerialize(item, out var serialized_data))
                                {
                                    builder.Append(serialized_data);
                                    builder.Append(Separators.struct_separator);
                                }
                            }
                            return (list_type.ToString(), builder.ToString());
                        }
                        return default;
                    }
            }

            {
                var data_type = data.GetType();
                if (data_type.IsValueType || (typeof(IUseDefaultSerialization).IsAssignableFrom(data_type)))
                {

                    var builder = new System.Text.StringBuilder();
                    foreach (var field in data_type.GetFields())
                    {
                        if (TrySerialize(field.GetValue(data), out var serialized_field))
                            builder.Append($"{field.Name}:{serialized_field}{Separators.struct_separator}");
                    }
                    return (data_type.ToString(), builder.ToString());
                }
            }
            return default;
        }

        static object Default_Deserializer(string identifier, string serialized_data)
        {
            switch (identifier)
            {
                case "f16": { if (System.Half.TryParse(serialized_data, out System.Half value)) return value; return null; }
                case "f32": { if (float.TryParse(serialized_data, out float value)) return value; return null; }
                case "f64": { if (double.TryParse(serialized_data, out double value)) return value; return null; }

                case "bool": { if (bool.TryParse(serialized_data, out bool value)) return value; return null; }
                case "byte": { if (byte.TryParse(serialized_data, out byte value)) return value; return null; }
                case "sbyte": { if (sbyte.TryParse(serialized_data, out sbyte value)) return value; return null; }
                case "i16": { if (short.TryParse(serialized_data, out short value)) return value; return null; }
                case "u16": { if (ushort.TryParse(serialized_data, out ushort value)) return value; return null; }
                case "i32": { if (int.TryParse(serialized_data, out int value)) return value; return null; }
                case "u32": { if (uint.TryParse(serialized_data, out uint value)) return value; return null; }
                case "i64": { if (long.TryParse(serialized_data, out long value)) return value; return null; }
                case "u64": { if (ulong.TryParse(serialized_data, out ulong value)) return value; return null; }
                case "guid": { if (System.Guid.TryParse(serialized_data, out System.Guid value)) return value; return null; }

                case "str": return serialized_data.Replace(Separators.return_separator, "\n");

                case "vec2": { if (SplitFloats(out var buffer) && buffer.Count == 2) return new Godot.Vector2(buffer[0], buffer[1]); return null; }
                case "vec3": { if (SplitFloats(out var buffer) && buffer.Count == 3) return new Godot.Vector3(buffer[0], buffer[1], buffer[1]); return null; }
                case "vec4": { if (SplitFloats(out var buffer) && buffer.Count == 4) return new Godot.Vector4(buffer[0], buffer[1], buffer[2], buffer[3]); return null; }
                case "color": { if (SplitFloats(out var buffer) && buffer.Count == 4) return new Godot.Color(buffer[0], buffer[1], buffer[2], buffer[3]); return null; }
            }
            const string godot_namespace = $"{nameof(Godot)}.";
            var type = Type.GetType(identifier);
            if (type == null) type = typeof(Godot.Node).Assembly.GetType(identifier);
            if (type == null) return default;

            if (typeof(ICustomSerialization).IsAssignableFrom(type))
            {
                var data = System.Activator.CreateInstance(type) as ICustomSerialization;
                data.Deserialize(serialized_data);
                return data;
            }

            if (type.IsEnum) return (System.Enum.TryParse(type, serialized_data, out var result)) ? result : null;

            if (type.IsValueType || typeof(IUseDefaultSerialization).IsAssignableFrom(type))
            {
                var data = System.Activator.CreateInstance(type);
                bool isValueStruct = data is System.Runtime.CompilerServices.ITuple;
                var split = serialized_data.Split(Separators.struct_separator);
                foreach (var item in split)
                {
                    var field_data = item.Split(":", 2);
                    if (field_data.Length != 2) continue;
                    if (TryDeserialize(field_data[1], out var deserialized_field))
                    {
                        var field = type.GetField(field_data[0]);
                        if (field == null)
                            Debug.LogWarning("Missing Field", type, "->", field_data[0], deserialized_field.GetType());
                        else if (field.FieldType != deserialized_field.GetType())
                            Debug.LogWarning("Field Miss Match:", type.Name, field.Name, "->", field.FieldType, " != ", deserialized_field.GetType());
                        else field.SetValue(data, deserialized_field);
                    }
                }
                return data;
            }

            if (type.IsArray)
            {
                var items = serialized_data.Split(Separators.struct_separator);
                if (items.Length < 1) return default;
                if (!int.TryParse(items[0], out var count)) return default;

                var element_type = type.GetElementType();
                var data = (System.Collections.IList)System.Array.CreateInstance(element_type, count);
                for (int i = 1; i < items.Length; ++i)
                {
                    var element_data = items[i].Split(':', 2);
                    if (element_data.Length != 2) continue;
                    if (int.TryParse(element_data[0], out var index)
                        && index < count
                        && TryDeserialize(element_data[1], out var element))
                    {
                        if (element_type.IsAssignableFrom(element.GetType()))
                            data[index] = element;
                    }
                }
                return data;
            }

            if (type.IsGenericType) // value tuples are generic types so this needs to be after value types
            {
                if (type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                {
                    var list = System.Activator.CreateInstance(type);
                    var element_type = type.GetGenericArguments()[0];
                    var add = type.GetMethod("Add");
                    object[] param = new object[1];
                    foreach (var item in serialized_data.Split(Separators.struct_separator))
                    {
                        if (TryDeserialize(item, out var deserialized_data))
                        {
                            param[0] = deserialized_data;
                            add.Invoke(list, param);
                        }
                    }
                    return list;
                }
                return default;
            }
            return null;

            bool SplitFloats(out List<float> buffer)
            {
                float_buffer.Clear();
                buffer = float_buffer;
                var data = serialized_data.Split(' ');
                foreach (var item in data)
                    if (float.TryParse(item, out var value))
                        float_buffer.Add(value);
                    else return false;
                return true;
            }
        }

        static List<float> float_buffer = new();
    }
}