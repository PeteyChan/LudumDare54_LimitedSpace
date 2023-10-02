using Godot;
using System;

public partial class Win : Control
{
    System.Collections.Generic.SortedSet<int> queuue;

    float timer = default;
    public override void _Process(double delta)
    {
        timer += (float)delta;
        if (timer > 5)
            GameScenes.Title.Load();
    }
}
