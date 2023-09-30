public static partial class Scene
{
    public static Godot.SceneTree Tree => Godot.Engine.GetMainLoop() as Godot.SceneTree;

    static Godot.Node _current;
    public static Godot.Node Current
    {
        get => _current = Godot.Node.IsInstanceValid(_current) ? _current : Tree.CurrentScene;
        set
        {
            if (value == _current) return;
            Tree.Root.AddChild(value);
            _current.QueueFree();
            _current = value;
        }
    }
    public static Godot.Node Load(string path) => Current = Godot.GD.Load<Godot.PackedScene>(path).Instantiate();
    public static int WindowHeight => (int)Current.GetViewport().GetVisibleRect().Size.Y;
    public static int WindowWidth => (int)Current.GetViewport().GetVisibleRect().Size.X;
}