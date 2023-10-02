using Godot;
using System;

public partial class TractorBeam : Node3D
{
    public Vector3? target;

    StandardMaterial3D material;

    public override void _Ready()
    {
        if (this.TryFind(out MeshInstance3D mesh))
        {
            material = mesh.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
        }
    }


    float total = default;
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double _delta)
    {
        float delta = (float)_delta;
        total += delta;

        if (target.HasValue)
        {
            var target_direction = target.GetValueOrDefault() - GlobalPosition;
            var scale = target_direction.Length();
            Scale = new Vector3(1, 1, -scale / 2f);
            material.AlbedoColor = material.AlbedoColor.Lerp(Colors.White, delta * 5f);

            LookAt(target.GetValueOrDefault(), Vector3.Forward);
        }
        else material.AlbedoColor = material.AlbedoColor.Lerp(new Color(1, 1, 1, 0), delta * 5f);

        material.Uv1Offset = material.Uv1Offset.Offset(0, -delta * 4f, 0);
        target = default;

    }
}
