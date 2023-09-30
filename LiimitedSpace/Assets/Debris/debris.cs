using Godot;
using System;

public partial class debris : RigidBody3D
{
    static int count;
    static void Updater(Bootstrap.Process args) => Debug.Label("Debris count:", count).SetColor(Colors.Black);

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
                        case grinder:
                            QueueFree();
                            break;
                    }
                };

                ConstantTorque = new Vector3(0, 0, System.Random.Shared.Range(-5f, 5f));
                ConstantForce = new Vector3(System.Random.Shared.Range(-10, -1), System.Random.Shared.Range(-1, 1), 0);
                if (this.TryFind(out CollisionShape3D shape))
                {
                    var size = System.Random.Shared.Range(.5f, 3f);
                    shape.Scale = size * Vector3.One;
                    Mass = size * size;
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

            case NotificationEnterTree:
                count++;
                break;

            case NotificationExitTree:
                count--;
                break;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Position.Length() > 25)
            QueueFree();
    }
}
