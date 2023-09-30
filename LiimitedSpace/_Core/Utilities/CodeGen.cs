using System.Collections.Generic;

namespace Utils
{
    public class CodeGen_Helper
    {
        Queue<string> pending = new Queue<string>();
        List<string> to_write = new List<string>();

        /// <summary>
        /// Add lines in file to pending queue
        /// </summary>
        public void AddPendingFile(string file)
        {
            foreach (var item in System.IO.File.ReadAllLines(file))
                pending.Enqueue(item);
        }

        /// <summary>
        /// Adds all lines of text to pending queue
        /// </summary>
        public void AddPendingText(IEnumerable<string> text)
        {
            foreach (var item in text)
                pending.Enqueue(item);
        }

        /// <summary>
        /// writes text into file
        /// </summary>
        public void WriteLine(string text) => to_write.Add(text);

        /// <summary>
        /// writes text into file prefixed with specified number of tabs
        /// </summary>
        public void WriteLine(int tabs, string text) => WriteLine(text.PadLeft(tabs + text.Length, '\t'));

        /// <summary>
        /// writes all file text from code gen into this codegen
        /// </summary>
        public void WriteCodeGen(CodeGen_Helper code_gen)
        {
            foreach (var text in code_gen.to_write)
                to_write.Add(text);
        }


        /// <summary>
        /// dequeues pending text and inserts it into file until a pending line contains the target text.
        /// </summary>
        public void WriteUntil(string target, bool write_target_line)
        {
            while (pending.TryDequeue(out var value))
            {
                if (value.Contains(target))
                {
                    if (write_target_line)
                        to_write.Add(value);
                    return;
                }
                else
                    to_write.Add(value);
            }
        }

        /// removes pending text until pending line contains the target text.
        public void RemoveUntil(string target, bool write_target_line)
        {
            while (pending.TryDequeue(out var value))
            {
                if (value.Contains(target))
                {
                    if (write_target_line)
                        to_write.Add(value);
                    return;
                }
            }
        }

        /// <summary>
        /// calls CopyUntil(target)
        /// calls action.
        /// calls SkipUntil(target)
        /// </summary>
        public void WriteBetween(string target, bool write_target_lines, System.Action action)
        {
            WriteUntil(target, write_target_lines);
            action();
            RemoveUntil(target, write_target_lines);
        }

        /// <summary>
        /// writes all remaining pending text into the file
        /// </summary>
        public void WriteRest()
        {
            while (pending.TryDequeue(out var value))
                to_write.Add(value);
        }

        /// <summary>
        /// writes the file to disk at path
        /// </summary>
        public void WriteToFile(string path)
        {
            System.IO.File.WriteAllLines(path, to_write);
        }

        /// <summary>
        /// returns lines between first instance of target lines
        /// </summary>
        public List<string> GetBetween(string target, bool include_target_line, List<string> buffer = default)
        {
            if (buffer == null) buffer = new List<string>();
            buffer.Clear();

            bool writing = false;

            foreach (var line in pending)
            {
                if (line.Contains(target))
                {
                    if (include_target_line) buffer.Add(line);
                    if (writing) return buffer;
                    else writing = true;
                }
                else if (writing) buffer.Add(line);
            }
            return buffer;
        }


        /// <summary>
        /// clears both the pending text and file text
        /// </summary>
        public void Clear()
        {
            pending.Clear();
            to_write.Clear();
        }

        public string Pattern(int start_index, int end_index_exclusive, System.Func<int, string> text, string separator = ",")
        {
            var builder = new System.Text.StringBuilder();
            while (start_index < end_index_exclusive)
            {
                builder.Append(text(start_index));
                if (start_index != end_index_exclusive - 1)
                    builder.Append(separator);
                start_index ++;
            }
            return builder.ToString();
        }
    }
}