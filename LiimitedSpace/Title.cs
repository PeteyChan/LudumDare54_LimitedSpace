using Godot;
using System;

public partial class Title : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (!this.TryFind(out title_menu))
			Debug.LogError("Couldn't find title menu");
	}

	IMGUI_VBoxContainer title_menu;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (title_menu.Button("Start Game"))
			GameScenes.Game.Load();
		if (title_menu.Button("QuitGame"))
			this.GetTree().Quit();
	}
}
