using Godot;
using System;

public enum GameScenes
{
	Title,
	Game,
	GameOver,
	Win,
}

public static partial class GameExtensions
{
	public static void Load(this GameScenes target_scene)
	{
		Scene.Current = GD.Load<Godot.PackedScene>(GetPath(target_scene)).Instantiate();

		string GetPath(GameScenes scene)
		{
			switch (target_scene)
			{
				case GameScenes.Title: return "res://Scenes/Title/Title.tscn";
				case GameScenes.Game: return "res://Testing/Test.tscn";
				case GameScenes.GameOver: return "res://Scenes/GameOver/game_over.tscn";
				case GameScenes.Win: return "res://Scenes/Win/win.tscn";
				default: goto case GameScenes.Title;
			}
		}
	}
}
