using Godot;
using System;

public partial class Background : MeshInstance3D
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        material = this.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
    }

    [Export] float min_parallax_speed = .02f, max_parallax_speed = .2f;

    StandardMaterial3D material;

    float timer = default;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        timer += (float)delta;
        var ratio = (timer / 120f).MaxValue(1f);

        var offset = material.Uv1Offset;
        offset.X += (float)delta * min_parallax_speed.Lerp(max_parallax_speed, ratio);
        material.Uv1Offset = offset;
    }
}
