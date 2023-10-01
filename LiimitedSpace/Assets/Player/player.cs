using Godot;
using System;

public partial class player : RigidBody3D
{
    static void Win(Debug.Console args)
    {
        if (Scene.Current.TryFind(out player player))
            player.state.next = PlayerStates.Win;
    }

    [Export] Inputs up = Inputs.key_w, down = Inputs.key_s, left = Inputs.key_a, right = Inputs.key_d, dash = Inputs.key_space;
    [Export] float limitsX = 13f, limitsY = 7f;
    [Export] float speed = 10f;
    MeshInstance3D player_mesh;

    Utils.Statemachine<PlayerStates> state = new();

    Node3D burner;

    public override void _Ready()
    {
        foreach (var sprite in this.FindAll<AnimatedSprite3D>())
        {
            sprite.Play();
            burner = sprite.GetParent() as Node3D;
        }

        if (!this.TryFind(out player_mesh))
            Debug.LogError("failed to find player mesh");

        BodyEntered += (body) =>
        {
            switch (body)
            {
                case Grinder:
                    switch (state.current)
                    {
                        case PlayerStates.Death:
                        case PlayerStates.Win:
                            break;
                        default: state.next = PlayerStates.Death; break;
                    }
                    break;

                case Debris debris:
                    if (debris.Mass > 15)
                        SFX.HeavyImpact.Play(Position);
                    else SFX.LightImpact.Play(Position);
                    break;

                case Wall:
                    SFX.LightImpact.Play(Position);
                    break;
            }
        };
    }

    PinJoint3D joint;

    float dash_timer;

    float timer;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double _delta)
    {
        var delta = (float)_delta;
        //Debug.DrawArrow3D(GlobalPosition, player_mesh.GlobalTransform.GetForward(), Colors.Yellow);

        switch (state.Update(delta))
        {
            case PlayerStates.Move:

                var target_velocity = new Vector3();
                if (up.Pressed())
                    target_velocity = Vector3.Up;
                if (down.Pressed())
                    target_velocity += Vector3.Down;
                if (left.Pressed())
                    target_velocity += Vector3.Left;
                if (right.Pressed())
                    target_velocity += Vector3.Right;

                target_velocity *= speed;
                target_velocity = LinearVelocity.Lerp(target_velocity, delta * 2f);

                if (target_velocity.X * delta + GlobalPosition.X > limitsX)
                    target_velocity.X = target_velocity.X.MaxValue(-0.1f);

                if (target_velocity.X * delta + GlobalPosition.X < -limitsX)
                    target_velocity.X = target_velocity.X.MinValue(0.1f);

                if (target_velocity.Y * delta + GlobalPosition.Y > limitsY)
                    target_velocity.Y = target_velocity.Y.MaxValue(-0.1f);

                if (target_velocity.Y * delta + GlobalPosition.Y < -limitsY)
                    target_velocity.Y = target_velocity.Y.MinValue(0.1f);

                LinearVelocity = target_velocity;
                LookAtMouse();

                if ((dash_timer -= delta) < 0 && dash.OnPressed())
                    state.next = PlayerStates.Dash;

                if (state.update_time > 180)
                    state.next = PlayerStates.Win;


                burner.Scale = burner.Scale.Lerp(Vector3.One, delta * 5f);
                break;

            case PlayerStates.Death:
                LinearVelocity = LinearVelocity.Lerp(default, delta * 2f);

                Debug.Label("Dead", state.current_time).SetColor(Colors.Red);

                if (state.current_time > 5f)
                {
                    GameScenes.GameOver.Load();
                    Debug.Log("Go to Game Over");
                }
                break;

            case PlayerStates.Dash:
                burner.Scale = new Vector3(1, 1, 4);
                dash_timer = 2f;
                LinearVelocity = player_mesh.GlobalTransform.GetForward() * 20f;
                if (state.current_time > .1f)
                    state.next = PlayerStates.Move;

                if (Position.X > limitsX)
                    Position = new Vector3(limitsX, Position.Y, 0);
                break;

            default:
                state.next = PlayerStates.Move;
                break;

            case PlayerStates.Win:
                CollisionLayer = 0;
                CollisionMask = 0;
                LinearVelocity = Vector3.Right * 5f;
                burner.Scale = new Vector3(1, 1, 4);

                if (Effects.FadeToWhite(delta * .3f))
                    GameScenes.Win.Load();

                player_mesh.GlobalTransform = player_mesh.GlobalTransform.LookingAt(GlobalPosition - Vector3.Right, Vector3.Forward);
                break;
        }

        void LookAtMouse()
        {
            var target_direction = GetMousePosition() - Position;
            player_mesh.GlobalTransform = player_mesh.GlobalTransform.LookingAt(GlobalPosition - target_direction, Vector3.Forward);
        }
    }

    static Vector3 GetMousePosition()
    {
        Plane plane = new Plane(Vector3.Forward);
        var viewport = Scene.Current.GetViewport();
        var camera = viewport.GetCamera3D();
        var from = camera.ProjectRayOrigin(viewport.GetMousePosition());
        var dir = camera.ProjectRayNormal(viewport.GetMousePosition());
        return plane.IntersectsRay(from, dir).GetValueOrDefault();
    }
}

public enum PlayerStates
{
    Move,
    Death,
    Dash,
    Win,
}
