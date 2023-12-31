using Godot;
using System;

public partial class Debris : RigidBody3D
{
    public float size = default;
    public bool destroy = false;

    public override void _Notification(int what)
    {
        switch ((long)what)
        {
            case NotificationReady:
                ContactMonitor = true;
                MaxContactsReported = 4;

                this.BodyEntered += body =>
                {
                    switch (body)
                    {
                        case Grinder:
                            destroy = true;
                            break;
                    }
                };

                ConstantTorque = new Vector3(0, 0, System.Random.Shared.Range(-5f, 5f));
                ConstantForce = new Vector3(System.Random.Shared.Range(-10, -1), System.Random.Shared.Range(-1, 1), 0);
                if (this.TryFind(out CollisionShape3D shape))
                {
                    size = System.Random.Shared.Range(.5f, 3f);
                    shape.Scale = size * Vector3.One;
                    Mass = size * size * 3f;
                }

                if (this.TryFind(out Sprite3D sprite))
                {
                    sprite.Texture = System.Random.Shared.Next(2) switch
                    {
                        0 => GD.Load<Texture2D>("res://Assets/Debris/DebrisA.png"),
                        _ => GD.Load<Texture2D>("res://Assets/Debris/DebrisB.png")
                    };
                }
                break;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Position.Length() > 25)
            QueueFree();

        if (destroy)
        {
            Effects.Explosion(Position, size);
            QueueFree();
        }
    }
}
