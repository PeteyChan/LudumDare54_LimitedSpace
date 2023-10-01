using Godot;
using System;

public partial class Grinder : AnimatableBody3D
{
    public override void _Ready()
    {
        this.OnProcess(delta =>
        {
            Position += Vector3.Right * delta * .2f;
        });

    }
}
