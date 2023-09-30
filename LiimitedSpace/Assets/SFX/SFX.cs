using Godot;
using System;


public enum SFX
{
    LightImpact,
    HeavyImpact
}

public static partial class GameExtensions
{
    public static void Play(this SFX sfx, Vector3 position)
    {
        if (Godot.Engine.GetFramesDrawn() - sfx_interval[sfx] < 5)
            return;
        sfx_interval[sfx] = Godot.Engine.GetFramesDrawn();

        if (!sfx_players[sfx].IsValid())
        {
            sfx_players[sfx] = new AudioStreamPlayer3D { Name = sfx.ToString() }.AddToScene();

            sfx_players[sfx].Stream = sfx switch
            {
                SFX.LightImpact => GD.Load<AudioStream>("res://Assets/SFX/LightImpact.wav"),
                SFX.HeavyImpact => GD.Load<AudioStream>("res://Assets/SFX/HeavyImpact.wav"),
                _ => default
            };
        }
        sfx_players[sfx].Position = position;
        sfx_players[sfx].Play();
    }

    static Utils.EnumMap<SFX, int> sfx_interval = new();
    static Utils.EnumMap<SFX, AudioStreamPlayer3D> sfx_players = new Utils.EnumMap<SFX, AudioStreamPlayer3D>();
}
