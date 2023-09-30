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

            Debug.Log("pressed", Godot.Engine.GetFramesDrawn(), Inputs.key_escape.CurrentValue(), Inputs.key_escape.PreviousValue());
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
                Scene.Load("res://Scenes/Title/Title.tscn");
                this.GetTree().Paused = false;
            }

        }
    }
}
