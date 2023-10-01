using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public static partial class Debug
{
    public class Exception : System.Exception
    {
        public Exception(params object[] args)
        {
            _message = "".AsString(args);
        }

        string _message;
        public override string Message => _message;
    }

    public static IMGUI_Interface GUI => DebugViewer.Get();
    public static bool Button(string label) => Debug.GUI.Button(label);
    public static bool Property<T>(string label, T input, out T output) => Debug.GUI.Label(label).Property(input, out output);
    public static IMGUI_Interface.Label Label(params object[] args)
        => DebugViewer.Get().Label(args);
    public static void ColorLabel(Godot.Color color, params object[] args)
        => DebugViewer.Get().Label(args).SetColor(color);

    public static void Label2D(Vector2 position, params object[] args)
        => ColorLabel2D(Colors.White, position, args);

    public static void ColorLabel2D(Color color, Vector2 position, params object[] args)
        => DebugLabel2D_Impl.Add(position, "".AsString(args), color);

    public static void Label3D(Vector3 position, params object[] args)
        => DebugLabel3D_Impl.Add(position, "".AsString(args), Colors.White);

    public static void ColorLabel3D(Color color, Vector3 position, params object[] args)
        => DebugLabel3D_Impl.Add(position, "".AsString(args), color);

    static System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

    public static TimeSpan Time(string name, System.Action action, bool output_to_console = true)
        => Time(name, 1, action, output_to_console);

    public static TimeSpan Time(string name, int interations, System.Action action, bool output_to_console = true)
    {
        interations = interations.MinValue(1);
        var time = timer.Elapsed;
        for (int i = 0; i < interations; ++i)
            action();
        time = timer.Elapsed - time;
        if (output_to_console)
            if (interations == 1)
                Debug.Log(name, "took", time.TotalMilliseconds.ToString("0.000"), "ms");
            else
                Debug.Log(name, $"x{interations}", "took", time.TotalMilliseconds.ToString("0.000"), "ms");
        return time;
    }

    partial class DebugLabel2D_Impl : Godot.Node
    {
        static DebugLabel2D_Impl instance;

        public static void Add(Vector2 position, string text, Color color)
        {
            if (!Node.IsInstanceValid(instance))
            {
                instance = new() { Name = "Debug Label2D" };
                ((SceneTree)Godot.Engine.GetMainLoop())
                    .Root.AddChild(instance, false, InternalMode.Back);
            }
            instance.values.Add((position, text, color));
        }

        List<(Vector2 position, string text, Color color)> values = new();
        List<Godot.Label> labels = new List<Label>();

        public override void _Process(double delta)
        {
            int i = 0;
            for (; i < values.Count; ++i)
            {
                Label label;
                if (labels.Count == i)
                {
                    label = new();
                    this.AddChild(label);
                    labels.Add(label);
                }
                else label = labels[i];
                label.Text = values[i].text;
                label.Modulate = values[i].color;
                label.Position = values[i].position;
                label.Visible = true;
            }
            for (; i < labels.Count; ++i)
                labels[i].Visible = false;

            values.Clear();
        }
    }

    partial class DebugLabel3D_Impl : Godot.Node
    {
        static DebugLabel3D_Impl instance;

        public static void Add(Vector3 position, string text, Color color)
        {
            if (!Node.IsInstanceValid(instance))
            {
                instance = new() { Name = "Debug Label3D" };
                ((SceneTree)Godot.Engine.GetMainLoop())
                    .Root.AddChild(instance, false, InternalMode.Back);
            }
            instance.values.Add((position, text, color));
        }

        List<(Vector3 position, string text, Color color)> values = new();
        List<Godot.Label> labels = new List<Label>();

        public override void _Process(double delta)
        {
            var camera = Scene.Current.GetViewport().GetCamera3D();
            if (camera.IsValid())
            {
                int i = 0;
                for (; i < values.Count; ++i)
                {
                    Label label;
                    if (labels.Count == i)
                    {
                        label = new();
                        //label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
                        this.AddChild(label);
                        labels.Add(label);
                    }
                    else label = labels[i];
                    label.Text = values[i].text;
                    label.Modulate = values[i].color;
                    label.Position = camera.UnprojectPosition(values[i].position);
                    label.Visible = camera.IsPositionInFrustum(values[i].position);
                }
                for (; i < labels.Count; ++i)
                    labels[i].Visible = false;
            }
            values.Clear();
        }
    }

    partial class DebugViewer
    {
        static IMGUI_VBoxContainer immediate_container;
        public static IMGUI_VBoxContainer Get()
        {
            if (!Godot.Node.IsInstanceValid(immediate_container))
            {
                var scroll = new Godot.ScrollContainer() { ProcessMode = Node.ProcessModeEnum.Always };
                scroll.Name = "Debug Viewer";
                scroll.AnchorRight = 1;
                scroll.AnchorBottom = 1;
                scroll.AnchorLeft = .7f;

                immediate_container = new();
                immediate_container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                immediate_container.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
                immediate_container.LayoutDirection = Control.LayoutDirectionEnum.Rtl;
                scroll.AddChild(immediate_container);
                scroll.OnProcess(() =>
                {
                    scroll.AnchorLeft = 1f - Console_Impl.console_data.debug_info_width;
                    scroll.Visible = immediate_container.Visible;
                });

                ((SceneTree)Godot.Engine.GetMainLoop())
                    .Root.AddChild(scroll, false, Node.InternalMode.Front);
            }
            return immediate_container;
        }
    }

    [System.Diagnostics.ConditionalAttribute("DEBUG")]
    public static void Assert(bool action, params object[] message) => Assert(action, "".AsString(message));

    [System.Diagnostics.ConditionalAttribute("DEBUG")]
    public static void Assert(Func<bool> action, params object[] message) => Assert(action(), message);

    [System.Diagnostics.ConditionalAttribute("DEBUG")]
    public static void Assert(bool value, string message = "Failed Assertion")
    {
        if (!value) throw new System.Exception(message);
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Test
    {
        /// <summary>
        /// Runs all static private methods marked with Debug.Test
        /// </summary>
        static void RunTests(Debug.Console args)
        {
            Debug.Log();
            Debug.Log("Starting Tests...");
            int pass = 0; int fail = 0;

            foreach (var type in typeof(Debug).Assembly.GetTypes())
            {
                if (type.IsGenericType) continue;
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Test))
                        continue;

                    var name = type.FullName + $".{method.Name}";

                    try
                    {
                        method.Invoke(null, new object[] { default });
                        Debug.WriteToConsole(Colors.LightGreen, "Pass:", name);
                        pass++;
                    }
                    catch (System.Exception e)
                    {
                        var message = e.InnerException?.Message;
                        Debug.WriteToConsole(Colors.OrangeRed, "FAIL:", name, "->", String.IsNullOrEmpty(message) ? e.Message : message);
                        fail++; Utils.RingBuffer<string> log_history = new(64);

                    }
                }
            }

            Debug.Log($"{pass}/{pass + fail} passed");
            if (fail == 0) Debug.WriteToConsole(Colors.Green, $"RESULT : PASS");
            else Debug.WriteToConsole(Colors.Red, $"RESULT: FAIL");
        }
    }
}

public static partial class Debug
{
    partial class TestCamera3D : Godot.Camera3D
    {
        static TestCamera3D camera;


        [Console.Help("Spawns a 3D Camera")]
        static void TestCamera(Console consle)
        {
            if (camera.IsValid()) camera.QueueFree();
            else camera = new TestCamera3D { Name = "Test Camera", Current = true }.AddToScene();
        }

        float pitch, yaw;
        Vector3 current_velocity;

        public override void _Ready()
        {
            Position = new Vector3(0, 5, -10);
            LookAt(Vector3.Zero, Vector3.Up);
            pitch = Rotation.X;
            yaw = Rotation.Y;
        }

        bool locked;

        public override void _Process(double delta)
        {

            Vector3 target_velocity = default;

            if (Inputs.mouse_middle_click.OnPressed())
                locked = !locked;

            if (locked) goto Complete;

            if (Inputs.key_w.Pressed())
                target_velocity += -Transform.GetForward();

            if (Inputs.key_s.Pressed())
                target_velocity += -Transform.GetBack();

            if (Inputs.key_a.Pressed())
                target_velocity += Transform.GetLeft();

            if (Inputs.key_d.Pressed())
                target_velocity += Transform.GetRight();

            if (Inputs.key_e.Pressed())
                target_velocity += Transform.GetUp();

            if (Inputs.key_q.Pressed())
                target_velocity += Transform.GetDown();

            target_velocity *= (float)delta * 10f;

            if (Inputs.key_space.Pressed())
                target_velocity *= 4f;

            if (Inputs.key_shift.Pressed())
                target_velocity *= .25f;

            float offset = .0001f;

            Print(Inputs.mouse_move_left);
            Print(Inputs.mouse_move_right);
            Print(Inputs.mouse_move_up);
            Print(Inputs.mouse_move_down);


            void Print(Inputs input) => Debug.Label(input, input.CurrentValue());

            pitch += Inputs.mouse_move_up.CurrentValue() * offset;
            pitch -= Inputs.mouse_move_down.CurrentValue() * offset;

            yaw += Inputs.mouse_move_left.CurrentValue() * offset;
            yaw -= Inputs.mouse_move_right.CurrentValue() * offset;


        Complete:

            Debug.Label("WASDQE: move around");
            Debug.Label("Mouse: look around");
            Debug.Label("Space Shift: speed up, slow down");
            Debug.Label("Middle Click: toggle lock camera");

            current_velocity = current_velocity.Lerp(target_velocity, (float)delta * 10f);
            Position += current_velocity;
            Rotation = new Vector3(pitch, yaw, 0);
        }
    }
}