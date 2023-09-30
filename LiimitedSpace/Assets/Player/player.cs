using Godot;
using System;

public partial class player : RigidBody3D
{
    [Export] Inputs up = Inputs.key_w, down = Inputs.key_s, left = Inputs.key_a, right = Inputs.key_d;
    [Export] float limitsX = 13f, limitsY = 7f;
    [Export] float speed = 10f;
    MeshInstance3D player_mesh;

    Utils.Statemachine<PlayerStates> state = new();

    public override void _Ready()
    {
        if (!this.TryFind(out player_mesh))
            Debug.LogError("failed to find player mesh");

        BodyEntered += (body) =>
        {
            if (body is grinder)
            {
                switch (state.current)
                {
                    case PlayerStates.Death: break;
                    default: state.next = PlayerStates.Death; break;
                }
            }
        };
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double _delta)
    {
        var delta = (float)_delta;
        Debug.DrawArrow3D(GlobalPosition, player_mesh.GlobalTransform.GetForward(), Colors.Yellow);

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

            default:
                state.next = PlayerStates.Move;
                break;
        }

        void LookAtMouse()
        {
            var target_direction = GetMousePosition() - Position;
            var current_direction = this.Transform.GetForward();
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
}