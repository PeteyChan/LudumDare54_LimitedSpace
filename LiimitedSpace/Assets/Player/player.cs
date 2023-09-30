using Godot;
using System;

public partial class player : MeshInstance3D
{
    [Export] Inputs up = Inputs.key_w, down = Inputs.key_s, left = Inputs.key_a, right = Inputs.key_d;
    [Export] float limitsX = 13f, limitsY = 7f;
    [Export] float speed = 10f;

    Vector3 velocity;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double _delta)
    {
        var delta = (float)_delta;
        Debug.Label(Position.ToString("0.00")).SetColor(Colors.Black);

        var target_velocity = new Vector3();
        if (up.Pressed())
            target_velocity = Vector3.Up;
        if (down.Pressed())
            target_velocity += Vector3.Down;
        if (left.Pressed())
            target_velocity += Vector3.Left;
        if (right.Pressed())
            target_velocity += Vector3.Right;

        velocity = velocity.Lerp(target_velocity, delta * 2f);
        var target_position = Position + velocity * delta * 5f;
        if (target_position.X.Abs() > limitsX)
            target_position.X = limitsX * Mathf.Sign(target_position.X);

		if (target_position.Y.Abs() > limitsY)
            target_position.Y = limitsY * Mathf.Sign(target_position.Y);	
		Position = target_position;
    }
}
