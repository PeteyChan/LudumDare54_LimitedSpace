using Godot;
using System;

public partial class Shot : Area3D
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SFX.LazerShot.Play(Position, .5f);

        BodyEntered += body =>
        {

            switch (body)
            {
                case Debris debris:
                    if (debris.size < 1f)
                        debris.destroy = true;
                    goto default;

                default:
                    var explosion = GD.Load<PackedScene>("res://Assets/Effects/Shot/ShotImpact.tscn").Instantiate() as Node3D;
                    explosion.Position = GlobalPosition;
                    explosion.AddToScene();
                    QueueFree();
                    break;
            }
        };
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //Debug.DrawArrow3D(GlobalPosition, GlobalTransform.GetUp(), Colors.Red);
        Position += Transform.GetBack() * (float)delta * 10f;
        //GlobalPosition += GlobalTransform.GetForward();
    }
}

public static partial class Effects
{
    public static void Shoot(Vector3 position, Vector3 direction)
    {
        var node = GD.Load<PackedScene>("res://Assets/Effects/Shot/Shot.tscn").Instantiate() as Node3D;
        node.Position = position;
        node.AddToScene();

        node.LookAt(position + direction, Vector3.Right);

        //node.LookAt(position + direction, Vector3.Forward);
    }
}