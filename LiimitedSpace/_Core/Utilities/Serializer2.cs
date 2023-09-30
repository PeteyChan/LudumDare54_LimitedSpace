using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class Serializer
{
    static char deliminator = '\t';
    const char next_line = '\u0085';

    public static bool TryLoadFromFile<T>(string path, out T item)
        where T : new() => TryLoadFromFile(path, out item, static () => new T());

    public static bool TryLoadFromFile<T>(string path, out T item, Func<T> create_item)
    {
        item = default;
        object target = create_item();
        if (!File.Exists(path)) return false;
        if (!TryDeserializeFields(target, File.ReadAllLines(path), 0))
            return false;
        item = (T)target;
        return true;
    }

    static bool TryDeserializeObject(out object target, Type type, Span<string> data, int depth)
    {
        target = default;
        if (data.Length == 0 || data[0].Length <= depth) return false;
        var value = data[0].Substring(depth);

        if (type.IsPrimitive)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Single:
                    if (float.TryParse(value, out var float_value))
                        target = float_value;
                    break;

                case TypeCode.Int32:
                    if (int.TryParse(value, out var int_value))
                        target = int_value;
                    break;

                case TypeCode.Boolean:
                    if (bool.TryParse(value, out var bool_value))
                        target = bool_value;
                    break;

                case TypeCode.Double:
                    if (bool.TryParse(value, out var double_value))
                        target = double_value;
                    break;

                case TypeCode.Int16:
                    if (short.TryParse(value, out var short_value))
                        target = short_value;
                    break;
            }
        }
        else
            switch (type)
            {
                case Type t when t == typeof(string):
                    target = value.Replace(next_line, '\n');
                    return true;

                case Type t when t.IsEnum:
                    if (System.Enum.TryParse(type, value, out var enum_value))
                        target = enum_value;
                    break;

                case Type t when t == typeof(System.Guid):
                    if (Guid.TryParse(value, out var guid_value))
                        target = guid_value;
                    break;

                case Type t when typeof(ISerializable).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null:
                    var items = value.Split(' ', 2);
                    var serialized_type = Type.GetType(items[0]);
                    var serializable = System.Activator.CreateInstance(serialized_type) as ISerializable;
                    if (serializable.TryDeserialize(items[1].Replace(next_line, '\n')))
                        target = serializable;
                    break;

                case Type t when t.IsValueType:
                    target = Activator.CreateInstance(t);
                    TryDeserializeFields(target, data, depth);
                    break;

                case Type t when t.IsArray:
                    if (int.TryParse(value, out var size))
                    {
                        target = Array.CreateInstance(t.GetElementType(), size);
                        var array = (Array)target;
                        int index = -1, start = 0, length = 0;
                        for (int i = 1; i < data.Length; ++i)
                        {
                            if (data[i][depth + 1] != deliminator)
                            {
                                DeserilizeIndex(data);
                                if (int.TryParse(data[i].TrimStart(deliminator), out index))
                                {
                                    start = i + 1;
                                    length = 0;
                                }
                            }
                            else length++;
                        }
                        DeserilizeIndex(data);

                        void DeserilizeIndex(Span<string> data)
                        {
                            if (index >= 0 && index < array.Length)
                            {
                                if (TryDeserializeObject(out var array_item, t.GetElementType(), data.Slice(start, length), depth + 2))
                                    array.SetValue(array_item, index);
                            }
                        }
                    }
                    break;
            }
        return target != null;
    }

    static bool TryDeserializeFields(object target, Span<string> data, int depth)
    {
        var target_type = target.GetType();
        (string name, int start, int length) field = default;
        for (int i = 0; i < data.Length; ++i)
        {
            var line = data[i];
            if (line.Length <= depth) continue;
            if (line[depth] == deliminator) field.length++;
            else
            {
                TrySetField(data);
                field.name = line;
                field.start = i + 1;
                field.length = 0;
            }
        }
        TrySetField(data);
        return true;

        bool TrySetField(Span<string> data)
        {
            if (field.name != null)
            {
                var field_info = target_type.GetField(field.name.TrimStart(deliminator));
                if (field_info != null)
                {
                    if (TryDeserializeObject(out var field_value, field_info.FieldType, data.Slice(field.start, field.length), depth + 1))
                    {
                        field_info.SetValue(target, field_value);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public static bool TrySaveToFile<T>(string path, T item)
    {
        if (item == null || item.GetType().IsPrimitive)
            return false;

        var file_info = new System.IO.FileInfo(path);
        if (!file_info.Exists)
            System.IO.Directory.CreateDirectory(file_info.Directory.FullName);

        var builder = new System.Text.StringBuilder();
        Serialize(item, builder, 0);
        System.IO.File.WriteAllText(path, builder.ToString());
        return true;
    }

    static void Serialize(object target, System.Text.StringBuilder builder, int depth)
    {
        foreach (var field in target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            builder.AppendLine(new string(deliminator, depth) + field.Name);
            var field_value = field.GetValue(target);
            switch (field_value)
            {
                case int int_value: Write(int_value.ToString()); break;
                case string str_value: Write(str_value.Replace('\n', next_line)); break;
                case float float_value: Write(float_value.ToString()); break;
                case bool bool_value: Write(bool_value.ToString()); break;
                case System.Enum enum_value: Write(enum_value.ToString()); break;
                case double double_value: Write(double_value.ToString()); break;
                case short short_value: Write(short_value.ToString()); break;
                case uint uint_value: Write(uint_value.ToString()); break;
                case System.Guid guid_value: Write(guid_value.ToString()); break;
                case ISerializable serializable:
                    if (serializable.GetType().GetConstructor(Type.EmptyTypes) != null
                        && serializable.TrySerialize(out var serialized_data))
                    {
                        Write($"{serializable.GetType().FullName} {serialized_data.Replace('\n', next_line)}");
                    }
                    break;
                case System.Array array:
                    Write(array.Length.ToString());
                    var array_type = array.GetType().GetElementType();
                    object default_item = default;
                    if (array_type.IsValueType) default_item = System.Activator.CreateInstance(array_type);
                    for (int i = 0; i < array.Length; ++i)
                    {
                        if (array.GetValue(i).Equals(default_item)) continue;
                        Write(i.ToString(), 1);
                        Serialize(array.GetValue(i), builder, depth + 3);
                    }
                    break;
                default:
                    if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive)
                        Serialize(field_value, builder, depth + 1);
                    break;
            }

            void Write(string value, int indent = 0) => builder.AppendLine(new string(deliminator, depth + 1 + indent) + value);
        }
    }

    public static bool TrySaveToEncryptedFile<T>(string key, string path, T item)
    {
        var file_info = new System.IO.FileInfo(path);
        if (!file_info.Exists)
            System.IO.Directory.CreateDirectory(file_info.Directory.FullName);

        var builder = new System.Text.StringBuilder();
        Serialize(item, builder, 0);
        File.WriteAllText(path, Encrypt(key, builder.ToString()));
        return true;
    }

    public static bool TryLoadFromEncryptedFile<T>(string key, string path, out T item) where T : new()
    {
        item = default;
        if (!File.Exists(path)) return false;
        var text = File.ReadAllText(path);
        text = Decrypt(key, text);
        var lines = text.Split('\n');
        object target = new T();
        if (!TryDeserializeFields(target, lines, 0))
            return false;
        item = (T)target;
        return true;
    }

    static string Encrypt(string key, string plainText)
    {
        byte[] iv = new byte[16];
        byte[] array;

        using (Aes aes = Aes.Create())
        {
            using (var sha = SHA256.Create())
            {
                aes.Key = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            }

            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(plainText);
                    }

                    array = memoryStream.ToArray();
                }
            }
        }
        return Convert.ToBase64String(array);
    }

    static string Decrypt(string key, string cipherText)
    {
        byte[] iv = new byte[16];
        byte[] buffer = Convert.FromBase64String(cipherText);
        using (Aes aes = Aes.Create())
        {
            using (var sha = SHA256.Create())
            {
                aes.Key = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
            aes.IV = iv;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }

    /// <summary>
    /// implementor must have a pulic empty constructor to be serializable
    /// </summary>
    public interface ISerializable
    {
        bool TrySerialize(out string value);
        bool TryDeserialize(string value);
    }

    /*
        class Serial : ISerializable
        {
            public int serial_value;
            public string serial_text = "";
            public bool TrySerialize(out string value)
            {
                value = serial_text + '_' + serial_value;
                return true;
            }

            public bool TryDeserialize(string value)
            {
                var lines = value.Split('_', 2);
                if (lines.Length != 2) return false;

                serial_text = lines[0];
                if (int.TryParse(lines[1], out var int_value))
                    serial_value = int_value;
                return true;
            }
        }

        static void Test(Bootstrap.Ready test)
        {
            (int, string, bool, (string, string, (int, int)), Godot.Key, Inputs, Godot.Vector4, Serial serial, System.Guid, Inputs, Godot.Vector2[] array) structure = default;
            structure.serial = new Serial { };
            structure.array = new Godot.Vector2[4];
            string path = "Testing/test";
            var window = new IMGUI_Window().AddToScene();

            string data = "", encrypted = default;

            window.OnProcess(() =>
            {
                window.Label("Path").SetStretchRatio(.2f).Property(path, out path);
                window.Label("Data").SetStretchRatio(.2f).Property(structure, out structure);
                window.Footer.HBox(out var hbox);

                if (hbox.Button("Save"))
                    TrySaveToFile(path, structure);
                if (hbox.Button("Load"))
                    TryLoadFromFile(path, out structure);

                window.MultiLineTextEdit(structure.Item2, out structure.Item2);
                if (structure.serial?.serial_text != null)
                    window.MultiLineTextEdit(structure.serial.serial_text, out structure.serial.serial_text);

                foreach (var field in typeof(System.Guid).GetFields())
                    Debug.Label(field.Name, field.FieldType);

                window.VerticalSeparator();
                window.Label("data").Property(data, out data);
                window.Label("encrypted").Label(encrypted);
                window.HBox(out hbox);
                if (hbox.Button("Encrypt"))
                    encrypted = Encrypt("key", data);
                if (hbox.Button("Decrypt"))
                    data = Decrypt("key", encrypted);

                window.VerticalSeparator();

                window.Property(structure, out structure);
                window.HBox(out hbox);
                if (hbox.Button("Encrypt File"))
                    TrySaveToEncryptedFile("key", path + "_encrypt", structure);
                if (hbox.Button("Decrypt File"))
                    TryLoadFromEncryptedFile("key", path + "_encrypt", out structure);
            });
        }

        static void Test2(Bootstrap.Process test) => Debug.Label("PRocessing");

        /**/
}