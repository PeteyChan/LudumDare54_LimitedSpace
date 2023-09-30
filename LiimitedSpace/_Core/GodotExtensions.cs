using Godot;
using System;
using System.Collections.Generic;

public static partial class GameExtensions
{
    partial class EventNode : Godot.Node
    {
        public Action<float> OnUpdate;
        public Action<float> OnPhysics;
        public Action OnDestroy;
        public Action<long> OnNotification;
        public override void _Notification(int what)
        {
            switch ((long)what)
            {
                case NotificationProcess:
                    OnUpdate?.Invoke((float)GetProcessDeltaTime());
                    break;

                case NotificationPhysicsProcess:
                    OnUpdate?.Invoke((float)GetPhysicsProcessDeltaTime());
                    break;

                case NotificationPredelete:
                    OnDestroy?.Invoke();
                    break;

                default:
                    OnNotification?.Invoke(what);
                    break;

            }
        }
    }

    static bool FindEventNode(this Godot.Node node, out EventNode target)
    {
        if (Godot.Node.IsInstanceValid(node))
        {
            foreach (var child in node.GetChildren(true))
                if (child is EventNode event_node)
                {
                    target = event_node;
                    return true;
                }
            node.AddChild(target = new EventNode(), false, Godot.Node.InternalMode.Front);
            return true;
        }
        target = default;
        return false;
    }

    public static T OnDestroy<T>(this T node, Action callback) where T : Godot.Node
    {
        if (node.FindEventNode(out var event_node))
            event_node.OnDestroy += callback;
        return node;
    }

    public static T OnPhysics<T>(this T node, Action callback) where T : Godot.Node
        => OnPhysics(node, delta => callback());

    public static T OnPhysics<T>(this T node, Action<float> callback) where T : Godot.Node
    {
        if (node.FindEventNode(out var event_node))
        {
            event_node.OnPhysics += callback;
            event_node.SetPhysicsProcess(true);
        }
        return node;
    }

    public static T OnProcess<T>(this T node, Action callback) where T : Godot.Node
        => OnProcess(node, delta => callback());

    public static T OnProcess<T>(this T node, Action<float> update_method) where T : Godot.Node
    {
        if (node.FindEventNode(out var event_node))
        {
            event_node.OnUpdate += update_method;
            event_node.SetProcess(true);
        }
        return node;
    }

    public static T OnProcess<T>(this T node, Action<T, float> update_method) where T : Godot.Node
        => OnProcess(node, delta => update_method(node, delta));

    public static T OnNofication<T>(this T node, Action<long> callback) where T : Godot.Node
    {
        if (node.FindEventNode(out var event_node))
            event_node.OnNotification += callback;
        return node;
    }

    public static T OnEnterTree<T>(this T node, Action callback) where T : Godot.Node
    {
        if (node.IsValid()) node.TreeEntered += callback;
        return node;
    }

    public static T OnExitTree<T>(this T node, Action callback) where T : Godot.Node
    {
        if (node.IsValid()) node.TreeExiting += callback;
        return node;
    }

    public static T AddToScene<T>(this T node) where T : Godot.Node
    {
        Scene.Current.AddChild(node);
        return node;
    }

    public static T AddToSceneDeffered<T>(this T node) where T : Godot.Node
    {
        Scene.Current.CallDeferred("add_child", node);
        return node;
    }

    public static T UnParent<T>(this T node) where T : Godot.Node
    {
        if (node?.GetParent() != null)
            node.GetParent().RemoveChild(node);
        return node;
    }

    public static void DestroyNode(this Godot.Node node)
    {
        if (Godot.Node.IsInstanceValid(node) && !node.IsQueuedForDeletion())
        {
            //if (node.IsInsideTree()) node.UnParent();
            node.QueueFree();
        }
    }

    public static T OnButtonDown<T>(this T button, Action<Godot.Button> action) where T : Godot.Button
        => OnButtonDown(button, () => action(button));

    public static T OnButtonDown<T>(this T button, Action action) where T : Godot.Button
    {
        button.Pressed += action;
        return button;
    }





    public static T AddChild<T>(this T node, string resource_path) where T : Godot.Node
    {
        node.AddChild(GD.Load<PackedScene>(resource_path).Instantiate());
        return node;
    }

    public static T AddChild<T>(this T node, string resource_path, out Godot.Node child) where T : Godot.Node
    {
        child = GD.Load<PackedScene>(resource_path).Instantiate();
        node.AddChild(child);
        return node;
    }

    public static T AddChildDeffered<T>(this T node, Godot.Node child_node) where T : Godot.Node
    {
        if (!child_node.IsValid()) throw new Exception($"child node is invalid, cannot add {node}");
        if (!node.IsValid()) throw new Exception($"node is invalid, cannot be add child node {child_node}");
        node.CallDeferred("add_child", child_node);
        return node;
    }

    public static T SetParentDeffered<T>(this T node, Godot.Node parent_node) where T : Godot.Node
    {
        parent_node.AddChildDeffered(node);
        return node;
    }
    public static T SetParent<T>(this T node, Godot.Node parent_node) where T : Godot.Node
    {
        parent_node.AddChild(node);
        return node;
    }

    public static bool IsValid(this Godot.GodotObject node) => Godot.GodotObject.IsInstanceValid(node);

    /// <summary>
    /// returns the first T value walking up the heirarchy
    /// </summary>
    public static bool TryFindRoot<T>(this Godot.Node node, out T value) where T : class
    {
        while (Node.IsInstanceValid(node))
        {
            if (node is T target)
            {
                value = target;
                return true;
            }
            node = node.GetParent();
        }
        value = default;
        return false;
    }

    [ThreadStatic] static Queue<Godot.Node> node_queue_buffer = new Queue<Node>();

    /// <summary>
    /// Tries to find the first instance of T in node heirarchy starting from node
    /// </summary>
    public static bool TryFind<T>(this Godot.Node node, out T target) where T : class
    {
        if (Godot.Node.IsInstanceValid(node))
            node_queue_buffer.Enqueue(node);

        while (node_queue_buffer.TryDequeue(out var test))
        {
            if (test is T value)
            {
                node_queue_buffer.Clear();
                target = value;
                return true;
            }

            foreach (var child in test.GetChildren())
                node_queue_buffer.Enqueue(child);
        }
        target = default;
        return false;
    }

    /// <summary>
    /// returns the first node of type in heirarchy that mataches the predicate 
    /// searches by child depth order
    /// </summary>
    public static bool TryFind<T>(this Godot.Node node, out T target, Func<T, bool> filter) where T : class
    {
        if (Godot.Node.IsInstanceValid(node))
            node_queue_buffer.Enqueue(node);

        while (node_queue_buffer.TryDequeue(out var test))
        {
            if (test is T value && filter(value))
            {
                node_queue_buffer.Clear();
                target = value;
                return true;
            }

            foreach (var child in test.GetChildren())
                node_queue_buffer.Enqueue(child);
        }
        target = default;
        return false;
    }

    /// <summary>
    /// Finds all instances of T in node heirarchy.
    /// instances are in order of heirerchy depth
    /// </summary>
    public static List<T> FindAll<T>(this Godot.Node node, List<T> return_buffer = default) where T : class
    {
        if (return_buffer == null) return_buffer = new List<T>();
        return_buffer.Clear();

        if (Godot.Node.IsInstanceValid(node))
            node_queue_buffer.Enqueue(node);

        while (node_queue_buffer.TryDequeue(out var test))
        {
            if (test is T target)
                return_buffer.Add(target);
            foreach (var child in test.GetChildren())
                node_queue_buffer.Enqueue(child);
        }
        return return_buffer;
    }

    /// <summary>
    /// Finds all instances of T in node heirarchy.
    /// instances are in order of heirerchy depth
    /// </summary>
    public static List<T> FindAll<T>(this Godot.Node node, Func<T, bool> filter, List<T> return_buffer = default) where T : class
    {
        if (return_buffer == null) return_buffer = new List<T>();
        return_buffer.Clear();

        if (Godot.Node.IsInstanceValid(node))
            node_queue_buffer.Enqueue(node);

        while (node_queue_buffer.TryDequeue(out var test))
        {
            if (test is T target && filter(target))
                return_buffer.Add(target);
            foreach (var child in test.GetChildren())
                node_queue_buffer.Enqueue(child);
        }
        return return_buffer;
    }

    public static bool HasFocusInHeirarchy(this Godot.Control node)
    {
        if (node.HasFocus()) return true;
        foreach (var child in node.GetChildren(true))
            if (child is Control control && control.HasFocusInHeirarchy())
                return true;
        return false;
    }

    public static Vector2I ToVec2i(this Vector2 vec2) => new Vector2I(Mathf.RoundToInt(vec2.X), Mathf.RoundToInt(vec2.Y));
    public static Vector2 ToVec2(this Vector2I vec2) => new Vector2(vec2.X, vec2.Y);
    public static Godot.Vector3 GetForward(this Godot.Transform3D t) => t.Basis.Z;
    public static Godot.Vector3 GetBack(this Godot.Transform3D t) => -t.Basis.Z;
    public static Godot.Vector3 GetRight(this Godot.Transform3D t) => t.Basis.X;
    public static Godot.Vector3 GetLeft(this Godot.Transform3D t) => -t.Basis.X;
    public static Godot.Vector3 GetUp(this Godot.Transform3D t) => t.Basis.Y;
    public static Godot.Vector3 GetDown(this Godot.Transform3D t) => -t.Basis.Y;
    public static Godot.Vector3 GetRelativeInputDirection(this Godot.Transform3D t, float x, float y)
    {
        var forward = t.Basis.Z;
        var right = t.Basis.X;
        forward.Y = 0;
        right.Y = 0;
        forward = forward.Normalized();
        right = right.Normalized();
        forward *= y;
        right *= x;
        var target = forward * right;
        return (forward + right).Normalized();
    }

    public static Godot.AnimationPlayer SetLoop(this Godot.AnimationPlayer animator, Animation.LoopModeEnum mode = Animation.LoopModeEnum.Linear)
    {
        if (!string.IsNullOrEmpty(animator.CurrentAnimation))
            animator.GetAnimation(animator.CurrentAnimation).LoopMode = mode;
        return animator;
    }

    public static Godot.AnimationPlayer SetLoop(this Godot.AnimationPlayer animator, string animation, bool loop = true)
    {
        animator.GetAnimation(animation).LoopMode = Animation.LoopModeEnum.Linear;
        return animator;
    }

    public static float GetTilt(this Godot.Vector2 vec2)
    {
        var val = Godot.Mathf.Sqrt(vec2.X * vec2.X + vec2.Y * vec2.Y);
        return val > 1 ? 1 : val;
    }

    public static Transform3D Lerp(this Transform3D transform, Transform3D target, float weight)
        => transform.InterpolateWith(target, weight);

    public static float Lerp(this float value, float target, float weight)
        => weight <= 0 ? value : weight >= 1 ? target : value + weight * (target - value);

    public static double Lerp(this double value, double target, double weight)
        => weight <= 0 ? value : weight >= 1 ? target : value + weight * (target - value);

    public static Vector2 Offset(this Vector2 target, float x, float y) => target + new Vector2(x, y);
    public static Vector3 Offset(this Vector3 target, float x, float y, float z) => target + new Vector3(x, y, z);
    public static Vector2I Offset(this Vector2I target, int x, int y) => target + new Vector2I(x, y);

    public static Vector3I ToVec3I(this Vector3 target) => new Vector3I(target.X.Round(), target.Y.Round(), target.Z.Round());

    public static Vector2 AsVec2(this Vector2I target) => new Vector2(target.X, target.Y);
    public static Vector3 AsVec3(this Vector3I target) => new Vector3(target.X, target.Y, target.Z);

    /// <summary>
    /// returns position of animation in relation to it's length. returns 1 if no animation is playing
    /// </summary>
    public static float GetPlaybackRatio(this AnimationPlayer animator, float time)
    {
        return time / animator.GetAnimation(animator.AssignedAnimation).Length;
    }
}
