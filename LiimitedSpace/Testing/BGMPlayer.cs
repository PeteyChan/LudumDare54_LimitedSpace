using Godot;
using System;

public partial class BGMPlayer : AudioStreamPlayer
{
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!Playing)
            Play();
    }
}
