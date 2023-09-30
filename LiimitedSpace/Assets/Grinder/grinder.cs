using Godot;
using System;

public partial class grinder : AnimatableBody3D
{
    public override void _Ready()
    {
        this.OnProcess(delta =>
        {
            Position += Vector3.Right * delta * .1f;
        });

    }
}
