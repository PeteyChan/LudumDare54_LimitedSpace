using Godot;
using System;

public partial class PauseScreen : PanelContainer
{
    bool pause;

    public override void _Ready()
    {
        if (!this.TryFind(out menu))
            Debug.LogError("Failed to find Pause menu");
    }

    IMGUI_VBoxContainer menu;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Inputs.key_escape.OnPressed())
        {
            pause = !pause;
            GetTree().Paused = pause;
            Visible = pause;
        }

        if (pause)
        {
            if (menu.Button("Resume"))
            {
                this.GetTree().Paused = false;
                pause = false;
                Visible = false;
            }
            if (menu.Button("Exit to Main Menu"))
            {
                GameScenes.Title.Load();
                this.GetTree().Paused = false;
            }

            float master = Godot.AudioServer.GetBusVolumeDb(0);
            float sfx = Godot.AudioServer.GetBusVolumeDb(1);
            float bgm = Godot.AudioServer.GetBusVolumeDb(2);
            float min_volume = -40, max_volume = 0f;

            if (menu.Label("Master").HSlider(master, out master, min: min_volume, max: max_volume))
                Godot.AudioServer.SetBusVolumeDb(0, master);
            if (menu.Label("SFX").HSlider(sfx, out sfx, min: min_volume, max: max_volume))
                Godot.AudioServer.SetBusVolumeDb(1, sfx);
            if (menu.Label("BGM").HSlider(bgm, out bgm, min: min_volume, max: max_volume))
                Godot.AudioServer.SetBusVolumeDb(2, bgm);

            Godot.AudioServer.SetBusMute(0, master <= -40f);
            Godot.AudioServer.SetBusMute(1, sfx <= -40f);
            Godot.AudioServer.SetBusMute(2, bgm <= -40f);
        }
    }
}
