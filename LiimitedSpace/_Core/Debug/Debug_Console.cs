using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public static partial class Debug // console
{
    [Debug.Console.Help("Displays Console Commands")]
    static void Help(Debug.Console args) => Debug.Console.Send("Commands");
    /// <summary>
    /// Allows calling static methods in non generic classes using the Debug.Console
    /// </summary>
    public class Console : IEnumerable<string>
    {
        static void Init(Bootstrap.Ready args)
        {
            var tree = (SceneTree)Godot.Engine.GetMainLoop();
            tree.Root.CallDeferred("add_child", new Console_Impl { Name = "Debug Console", ProcessMode = Node.ProcessModeEnum.Always }, false, (int)Godot.Node.InternalMode.Front);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            foreach (var item in _parameters)
                yield return item;
        }
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<string>)this).GetEnumerator();


        /// <summary>
        /// Adds help message to Console.Debug methods in the debug console
        /// </summary>
        [System.AttributeUsage(AttributeTargets.Method)]
        public class Help : System.Attribute
        {
            public string _message;
            public Help(string message)
            {
                this._message = message;
            }
        }

        static Dictionary<string, Action<Console>> _callbacks = new Dictionary<string, Action<Console>>();
        static List<string> _help_meassages = new List<string>();

        static Console()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            var target_assembly = typeof(Debug).Assembly;
            var types = target_assembly.GetTypes();
            int count = types.Length;
            for (int i = 0; i < count; ++i)
            {
                var type = types[i];
                if (type.IsGenericType) continue;

                foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    var para = method.GetParameters();
                    if (para.Length != 1) continue;
                    if (para[0].ParameterType != typeof(Debug.Console)) continue;
                    if (method.ReturnType != typeof(void)) continue;

                    string name = method.Name.ToLower();
                    _callbacks.TryGetValue(name, out var action);
                    action += Delegate.CreateDelegate(typeof(Action<Debug.Console>), null, method) as Action<Debug.Console>;
                    _callbacks[name] = action;

                    var help_message = method.GetCustomAttribute<Help>()?._message;

                    if (help_message == null) help_message = "";
                    _help_meassages.Add($"{GetHelpName()}  :  {help_message}");

                    string GetHelpName()
                    {
                        var pending = method.Name;
                        builder.Clear();
                        builder.Append(pending[0]);
                        for (int i = 1; i < pending.Length; ++i)
                        {
                            if (char.IsLower(pending[i - 1]) && char.IsUpper(pending[i]))
                                builder.Append(' ');
                            builder.Append(pending[i]);
                        }
                        return builder.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Sends a message to the console as if you entered it into the console
        /// </summary>
        public static void Send(string input) => new Console(input);
        Console(string input)
        {
            input = input == null ? "" : input;
            for (int i = input.Length; i > 0; i--) // iterating backward so the most specific calls get called first
            {
                var search = input.Substr(0, i).Replace(" ", "").ToLower();
                if (_callbacks.TryGetValue(search, out var action))
                {
                    RawMessage = input.Substr(i, input.Length - i);
                    _parameters = RawMessage.ToLower().Split(' ');
                    action?.Invoke(this);
                    return;
                }
            }
        }

        public readonly string RawMessage;
        public Memory<string> Parameters => _parameters;
        string[] _parameters;

        public int Count => _parameters.Length;

        public string this[int index]
            => index < 0 ? "" : index >= _parameters.Length ? "" : _parameters[index];

        public int ToInt(int arg) => int.TryParse(this[arg], out int value) ? value : 0;
        public float ToFloat(int arg) => float.TryParse(this[arg], out float value) ? value : 0;
        public bool ToBool(int arg) => bool.TryParse(this[arg], out bool value) ? value : false;

        public string ToString(int arg) => this[arg];

        public override string ToString() => this[0];

        [Help("Displays Console Commands")]
        static void Commands(Debug.Console console)
        {
            Debug.Log();
            Debug.Log("Commands:");
            foreach (var message in _help_meassages)
                Debug.Log(message);
            Debug.Log();
        }
    }

    public static void Log(params object[] args)
    {
        WriteToConsole(Colors.White, out var message, args);
        GD.Print(message);
    }

    public static void LogWarning(params object[] args)
    {

        WriteToConsole(Colors.Red, out var message, args);
        GD.PushWarning(Colors.Orange, message);
    }

    public static void LogError(params object[] args)
    {
        WriteToConsole(Colors.Red, out var message, args);
        GD.PushError(message);
    }

    public static void WriteToConsole(Color color, params object[] args) => WriteToConsole(color, out var message, args);
    static void WriteToConsole(Color color, out string message, IEnumerable args)
    {
        System.Text.StringBuilder builder = new();
        foreach (var item in args)
        {
            builder.Append(item is null ? "null" : item.ToString());
            builder.Append(' ');
        }
        message = builder.ToString();
        Console_Impl.LogMessages.Enqueue((color, message));
    }

    partial class Console_Impl : Godot.Node
    {
        public static SceneTree Tree => ((SceneTree)Godot.Engine.GetMainLoop());
        public static Queue<(Color color, string message)> LogMessages = new(1000);
        public static IMGUI_Window console_window;
        public static Console_Data console_data = new();
        const int decoration_height = 29; // getting the decoration size seems to fail, probably won't work on other computers
        Console_Tabs console_tab = default;
        string console_input = "";
        ColorRect background_rect = new ColorRect();
        int log_index = 0;
        enum Console_Tabs { Messages, Filter, Settings, }

        public override void _Ready()
        {
            console_data.Load();
            console_window = new IMGUI_Window() { Name = "Debug Console" };
            Tree.Root.AddChild(console_window);
            console_window.TransparentBg = true;
            console_window.Transparent = true;
            var parent_window = Tree.Root.GetViewport().GetWindow();
            int console_default_width = 400;
            var position = parent_window.Position;
            console_window.Position = new Vector2I(16, decoration_height + 16);
            console_window.Size = new Vector2I(console_default_width, parent_window.Size.Y - decoration_height * 2);

            console_window.Visible = false;
            console_window.Title = "Debug Console";
            console_window.OnCloseWindow(() => console_window.Visible = false);

            background_rect.AnchorLeft = 0;
            background_rect.AnchorTop = 0;
            background_rect.AnchorBottom = 1;
            background_rect.AnchorRight = 1;
            background_rect.Color = console_data.console_bg_color;

            console_window.AddChild(background_rect);
            console_window.MoveChild(background_rect, 0);
        }

        public override void _Process(double delta)
        {
            KeyInput.Update();
            // console visiblity
            bool update_console_input_text = false;
            ToggleConsoleVisiblity();
            if (!console_window.Visible) return;
            background_rect.Color = console_data.console_bg_color;
            CycleMessageHistory();
            DrawTextInput();
            DrawConsole();
            while (LogMessages.Count > 1000)
                LogMessages.Dequeue();

            void ToggleConsoleVisiblity()
            {
                if (KeyInput.OnPress(Key.Quoteleft))
                {
                    if (!console_window.Visible)
                    {
                        update_console_input_text = true;

                        // for some fucked up reason the debug console moves around without this
                        if (!console_window.IsEmbedded())
                            console_window.Position = console_window.Position - new Vector2I(0, decoration_height);
                        console_input = "";
                    }
                    console_window.Visible = !console_window.Visible;
                }
            }

            void CycleMessageHistory()
            {
                var current_log_index = log_index;
                if (KeyInput.OnPress(Key.Up))
                    log_index--;
                if (KeyInput.OnPress(Key.Down))
                    log_index++;
                if (current_log_index != log_index)
                {
                    update_console_input_text = true;
                    if (log_index < 0)
                        log_index = console_data.input_history.Count - 1;
                    if (log_index >= console_data.input_history.Count)
                        log_index = 0;
                    if (console_data.input_history.Count > 0)
                    {
                        console_input = console_data.input_history[log_index];
                    }
                }
            }

            void DrawTextInput()
            {
                var text_edit = console_window.Footer.GetGUIElement<TextEdit>();

                if (KeyInput.OnPress(Key.Enter))
                {
                    var to_send = console_input.Replace("\n", "");
                    Console.Send(to_send);
                    log_index = -1;
                    console_input = text_edit.Text = "";

                    if (to_send != "")
                    {
                        while (console_data.input_history.Count > 20)
                            console_data.input_history.RemoveAt(0);

                        console_data.input_history.Add(to_send);
                        console_data.Save();
                    }
                }

                text_edit.CustomMinimumSize = new Vector2(0, console_data.input_height);
                if (update_console_input_text)
                {
                    text_edit.GrabFocus();
                    text_edit.Text = console_input = console_input.Replace("\n", "");
                }
                console_input = text_edit.Text;
            }

            void DrawConsole()
            {
                // draw console 
                if (KeyInput.OnPress(Key.Tab) && Input.IsKeyPressed(Key.Ctrl))
                {
                    var tabs = System.Enum.GetValues<Console_Tabs>();
                    var target_tab = tabs[0];
                    for (int i = 0; i < tabs.Length; ++i)
                        if (tabs[i] == console_tab && i + 1 < tabs.Length)
                            target_tab = tabs[i + 1];
                    console_tab = target_tab;
                }

                console_window.Footer.Tabs(console_tab, out console_tab);
                switch (console_tab)
                {
                    case Console_Tabs.Messages:
                        foreach (var message in LogMessages)
                            console_window.Label(message.message).SetColor(message.color);
                        break;

                    case Console_Tabs.Filter:
                        foreach (var message in LogMessages)
                        {
                            if (message.message.Contains(console_input, StringComparison.OrdinalIgnoreCase))
                                console_window.Label(message.message).SetColor(message.color);
                        }
                        break;

                    case Console_Tabs.Settings:
                        bool save = false;
                        foreach (var field in console_data.GetType().GetFields())
                        {
                            switch (field.Name)
                            {
                                case nameof(Console_Data.debug_info_width):
                                    if (console_window.Label(field.Name).SpinBox((float)field.GetValue(console_data), out var float_val, .01f, 0f, 1f))
                                    {
                                        field.SetValue(console_data, float_val);
                                        save = true;
                                    }
                                    break;

                                default:
                                    if (console_window.Label(field.Name).Property(field.GetValue(console_data), out var new_value))
                                    {
                                        field.SetValue(console_data, new_value);
                                        save = true;
                                    }
                                    break;
                            }
                        }
                        if (save) console_data.Save();
                        break;
                }
            }
        }


        public class Console_Data
        {
            static readonly string setting_file_path = OS.GetUserDataDir() + "/DebugConsoleSettings.txt";
            public Color console_bg_color = new Color(.15f, .15f, .15f, 1);
            public int input_height = 36;
            public float debug_info_width = .5f;
            public List<string> input_history = new();
            public void Save()
            {
                using (var writer = new System.IO.StreamWriter(setting_file_path))
                {
                    writer.WriteLine(nameof(console_bg_color));
                    writer.WriteLine($"{console_bg_color.R},{console_bg_color.G},{console_bg_color.B},{console_bg_color.A}");
                    writer.WriteLine(nameof(input_height));
                    writer.WriteLine(input_height);
                    writer.WriteLine(nameof(debug_info_width));
                    writer.WriteLine(debug_info_width);
                    writer.WriteLine(nameof(input_history));
                    foreach (var item in input_history)
                        writer.WriteLine(item);
                }
            }

            public void Load()
            {
                if (!System.IO.File.Exists(setting_file_path)) return;
                input_history = new();
                using (var reader = new System.IO.StreamReader(setting_file_path))
                {
                    string line = default;
                    while ((line = reader.ReadLine()) != null)
                    {
                        try
                        {
                            switch (line)
                            {
                                case nameof(console_bg_color):
                                    var items = reader.ReadLine().Split(',');
                                    console_bg_color = new Color(float.Parse(items[0]), float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                                    break;
                                case nameof(input_height):
                                    input_height = int.Parse(reader.ReadLine());
                                    break;
                                case nameof(debug_info_width):
                                    debug_info_width = float.Parse(reader.ReadLine());
                                    break;
                                case nameof(input_history):
                                    while ((line = reader.ReadLine()) != null)
                                        input_history.Add(line);
                                    break;
                            }
                        }
                        catch { continue; }
                    }
                }
            }
        }

        class KeyInput
        {
            public static bool OnPress(Godot.Key key)
            {
                if (!pressed.TryGetValue(key, out var data))
                    pressed[key] = data;
                return data.current && !data.previous;
            }

            static Dictionary<Godot.Key, (bool previous, bool current)> pressed = new();
            public static void Update()
            {
                foreach (var (key, (previous, current)) in pressed)
                    pressed[key] = (current, Input.IsKeyPressed(key));
            }
        }
    }

    static class Console_Commands
    {

        [Console.Help("Logs a message to the console")]
        static void Log(Debug.Console args) => WriteToConsole(Colors.White, out var message, args);

        [Console.Help("Quits the application")]
        static void Exit(Debug.Console console) => Quit(console);

        [Console.Help("Quits the application")]
        static void Quit(Debug.Console console) => ((SceneTree)Godot.Engine.GetMainLoop()).Quit();

        [Console.Help("Closes the Debug console")]
        static void Close(Debug.Console console) => Console_Impl.console_window.Visible = false;

        [Console.Help("Clears the debug console")]
        static void Clear(Debug.Console console) => Console_Impl.LogMessages.Clear();

        [Debug.Console.Help("Counts all lines currently in Debug Console")]
        static void CountLines(Debug.Console args) => Debug.Log("Lines:", Console_Impl.LogMessages.Count);

        [Console.Help("Changes mouse mode, Options = {visible, confined, hidden, captured }")]
        static void MouseMode(Debug.Console args)
        {
            switch (args.ToString().ToLower())
            {
                case "show":
                case "visible":
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    break;
                case "confined":
                    Input.MouseMode = Input.MouseModeEnum.Confined;
                    break;
                case "hidden":
                    Input.MouseMode = Input.MouseModeEnum.Hidden;
                    break;
                case "captured":
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    break;
                default:
                    Debug.Log(args.ToString().ToLower());
                    break;
            }
        }
    }
}