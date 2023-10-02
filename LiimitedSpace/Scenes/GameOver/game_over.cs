using Godot;
using System;

public partial class game_over : Control
{

	float timer;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timer += (float)delta;
		if (timer > 5f)
			GameScenes.Title.Load();

		Effects.FadeFromBlack((float)delta);
	}
}
