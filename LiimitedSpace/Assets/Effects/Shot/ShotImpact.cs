using Godot;
using System;

public partial class ShotImpact : Node3D
{
    AnimatedSprite3D sprite;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SFX.LazerShot_Impact.Play(Position, .5f);
        if (this.TryFind(out sprite))
        {
            sprite.Play();

        }
        else Debug.LogError("couldn't find shot impact");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!sprite.IsPlaying())
            QueueFree();
    }
}
