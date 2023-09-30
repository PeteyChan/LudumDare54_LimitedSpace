using Godot;
using System;

public partial class spawner : Node3D
{
    Godot.Node3D item;
    [Export] float variance = 5f;

    [Export] float min_start = .5f, max_start = 2f;
    [Export] float min_end = .1f, max_end = .5f;


    float timer;

    float uptime;

    public override void _Ready()
    {
        item = GD.Load<PackedScene>("res://Assets/Debris/debris.tscn").Instantiate() as RigidBody3D;
    }

    public override void _Process(double delta)
    {
        Debug.Label(uptime.ToString("0.00")).SetColor(Colors.Black);
        uptime += (float)delta;

        var ratio = (uptime / 120f).MaxValue(1f);
        Debug.Label("Difficulty:", ratio.ToString("0.00")).SetColor(Colors.Black);

        if (timer < 0)
        {
			var min = min_start.Lerp(min_end, ratio);
			var max = max_start.Lerp(max_end, ratio);
			timer = System.Random.Shared.Range(min, max);
            var debris = item.Duplicate().AddToScene() as RigidBody3D;
            debris.Position = Position + new Vector3(0, System.Random.Shared.Range(-variance, variance), 0);
        }
        timer -= (float)delta;
    }
}
