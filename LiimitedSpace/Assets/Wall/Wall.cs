using Godot;
using System;

public partial class Wall : AnimatableBody3D
{
    [Export] Node3D target;

    public override void _Ready()
    {
        Vector3 original_position = GlobalPosition;
        Vector3 target_position = target.GlobalPosition;
        float timer = default;

        this.OnProcess(delta =>
        {
            timer += delta;
            GlobalPosition = original_position.Lerp(target_position, Mathf.Sin(timer * .1f).MinValue(0));

        });
    }
}
