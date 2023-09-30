using Godot;
using System;

public static class Bootstrap
{
    public struct Ready { }
    public struct Physics { public float delta_time; }
    public struct Process { public float delta_time; }
}

namespace Internal
{
    public partial class Bootstrap : Node
    {
        Action<global::Bootstrap.Physics> physics_callback;
        Action<global::Bootstrap.Process> process_callback;

        public override void _Ready()
        {
            GetAllMethods<global::Bootstrap.Ready>()?.Invoke(default);
            process_callback = GetAllMethods<global::Bootstrap.Process>();
            physics_callback = GetAllMethods<global::Bootstrap.Physics>();
        }

        public override void _Process(double delta)
        {
            process_callback?.Invoke(new global::Bootstrap.Process { delta_time = (float)delta });
        }

        public override void _PhysicsProcess(double delta)
        {
            physics_callback?.Invoke(new global::Bootstrap.Physics { delta_time = (float)delta });
        }


        Action<T> GetAllMethods<T>()
        {
            Action<T> action = default;
            foreach (var type in typeof(Bootstrap).Assembly.GetTypes())
            {
                if (type.IsGenericType) continue;
                foreach (var method in type.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    if (method.IsGenericMethod) continue;
                    if (method.IsAbstract) continue;
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1) continue;
                    if (parameters[0].ParameterType == typeof(T))
                    {
                        action += method.CreateDelegate(typeof(Action<T>), null) as Action<T>;
                    }
                }
            }
            return action;
        }
    }
}