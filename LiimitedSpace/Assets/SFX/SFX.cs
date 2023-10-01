using Godot;
using System;


public enum SFX
{
    LightImpact,
    HeavyImpact,
    Explosion,
    Boost
}

public static partial class GameExtensions
{
    public static void Play(this SFX sfx, Vector3 position, float volume = 1f)
    {
        if (Godot.Engine.GetFramesDrawn() - sfx_interval[sfx] < 5)
            return;

        sfx_interval[sfx] = Godot.Engine.GetFramesDrawn();

        if (!sfx_players[sfx].IsValid())
        {
            sfx_players[sfx] = new AudioStreamPlayer { Name = sfx.ToString(), Bus = "SFX" }.AddToScene();

            sfx_players[sfx].Stream = sfx switch
            {
                SFX.LightImpact => GD.Load<AudioStream>("res://Assets/SFX/LightImpact.wav"),
                SFX.HeavyImpact => GD.Load<AudioStream>("res://Assets/SFX/HeavyImpact.wav"),
                SFX.Explosion => GD.Load<AudioStream>("res://Assets/SFX/Explosion.wav"),
                SFX.Boost => GD.Load<AudioStream>("res://Assets/SFX/Boost.wav"),
                _ => default
            };
        }
        //sfx_players[sfx].Position = position;
        sfx_players[sfx].VolumeDb = -40f.Lerp(0, volume);
        sfx_players[sfx].Play();
    }

    static Utils.EnumMap<SFX, int> sfx_interval = new();
    static Utils.EnumMap<SFX, AudioStreamPlayer> sfx_players = new ();
}
