using Godot;
using System;
using System.Linq;

public partial class player : RigidBody3D
{
	static void Win(Debug.Console args)
	{
		if (Scene.Current.TryFind(out player player))
			player.state.next = PlayerStates.Win;
	}

	[Export] Inputs up = Inputs.key_w, down = Inputs.key_s, left = Inputs.key_a, right = Inputs.key_d, dash = Inputs.key_space, grab = Inputs.mouse_right_click, shoot = Inputs.mouse_left_click;
	[Export] float limitsX = 13f, limitsY = 7f;
	[Export] float speed = 10f;
	MeshInstance3D player_mesh;
	Utils.Statemachine<PlayerStates> state = new();
	Area3D grabber;
	Node3D burner;
	Debris grabbed_debris;
	TractorBeam tractorBeam;
	float win_time = 200;
	float shot_timer = 0;

	public override void _Ready()
	{
		foreach (var sprite in this.FindAll<AnimatedSprite3D>())
		{
			sprite.Play();
			burner = sprite.GetParent() as Node3D;
		}

		if (!this.TryFind(out player_mesh))
			Debug.LogError("failed to find player mesh");

		if (!player_mesh.TryFind(out grabber))
			Debug.LogError("failed to find grabber area 3D");

		if (!this.TryFind(out tractorBeam))
			Debug.LogError("failed to find tractor beam");

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
						SFX.HeavyImpact.Play(Position, .75f);
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
	public override void _PhysicsProcess(double _delta)
	{
		var delta = (float)_delta;
		dash_timer -= delta;
		shot_timer -= delta;
		//Debug.DrawArrow3D(GlobalPosition, player_mesh.GlobalTransform.GetForward(), Colors.Yellow);

		if (state.current is not PlayerStates.Death)
			if (state.update_time > win_time)
				state.next = PlayerStates.Win;


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

				if (dash_timer < 0 && dash.OnPressed())
					state.next = PlayerStates.Dash;

				if (shot_timer < 0 && shoot.OnPressed())
				{
					Effects.Shoot(GlobalPosition + player_mesh.GlobalTransform.GetForward() * 1.5f, player_mesh.Transform.GetForward());
				}

				if (Inputs.mouse_right_click.OnPressed())
				{
					if (grabber.GetOverlappingBodies().FirstOrDefault() is Debris target_debris)
					{
						grabbed_debris = target_debris;
						state.next = PlayerStates.Grabbing;
					}
				}

				burner.Scale = burner.Scale.Lerp(Vector3.One, delta * 5f);
				break;

			case PlayerStates.Grabbing:
				if (!grab.Pressed())
				{
					state.next = PlayerStates.Move;
					break;
				}

				if (!Node.IsInstanceValid(grabbed_debris))
				{
					grabbed_debris = default;
					state.next = PlayerStates.Move;
					break;
				}

				tractorBeam.target = grabbed_debris.GlobalPosition;

				//Debug.DrawArrow3D(player_mesh.GlobalPosition, player_mesh.GlobalTransform.GetForward(), Colors.Red);

				grabbed_debris.LinearVelocity = (grabber.GlobalPosition - grabbed_debris.GlobalPosition);

				target_velocity = new Vector3();
				if (up.Pressed())
					target_velocity = Vector3.Up;
				if (down.Pressed())
					target_velocity += Vector3.Down;
				if (left.Pressed())
					target_velocity += Vector3.Left;
				if (right.Pressed())
					target_velocity += Vector3.Right;

				target_velocity *= speed / 3f;
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

				var player_forward = player_mesh.GlobalTransform.GetForward();
				var debris_forward = grabbed_debris.GlobalPosition - player_mesh.GlobalPosition;

				//Debug.DrawArrow3D(GlobalPosition, player_forward, Colors.Red);
				//Debug.DrawArrow3D(GlobalPosition, debris_forward, Colors.Red);

				var angle = player_forward.AngleTo(debris_forward);

				if (Godot.Mathf.RadToDeg(angle) < 45f)
					LookAtMouse(2f);
				else LookAtPosition(grabbed_debris.GlobalPosition);

				if ((dash_timer -= delta) < 0 && dash.OnPressed())
					state.next = PlayerStates.Dash;

				break;

			case PlayerStates.Death:
				LinearVelocity = LinearVelocity.Lerp(default, delta);

				if (state.entered)
				{
					player_mesh.Visible = false;
					Effects.Explosion(Position, 1);
				}

				if (Effects.FadetoBlack(delta * .5f))
				{
					GameScenes.GameOver.Load();
				}
				break;

			case PlayerStates.Dash:
				if (state.entered)
					SFX.Boost.Play(Position, .75f);

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

		void LookAtPosition(Vector3 position, float look_speed = 5f)
		{
			var target_direction = position - Position;
			var target_transform = player_mesh.GlobalTransform.LookingAt(GlobalPosition - target_direction, Vector3.Forward);
			player_mesh.GlobalTransform = player_mesh.GlobalTransform.Lerp(target_transform, delta * look_speed);
		}

		void LookAtMouse(float look_speed = 5f)
		{
			var target_direction = GetMousePosition() - Position;
			var target_transform = player_mesh.GlobalTransform.LookingAt(GlobalPosition - target_direction, Vector3.Forward);
			player_mesh.GlobalTransform = player_mesh.GlobalTransform.Lerp(target_transform, delta * look_speed);
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
	Grabbing
}
