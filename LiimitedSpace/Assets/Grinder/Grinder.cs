using Godot;
using System;

public partial class Grinder : AnimatableBody3D
{
    public override void _Ready()
    {
        if (this.TryFind(out AnimationPlayer animator))
        {
            animator.Play("Cylinder_001Action");
            animator.SetLoop(Animation.LoopModeEnum.Linear);
        }

        if (this.TryFind(out AudioStreamPlayer3D audio))
        {
            audio.OnProcess(() =>
            {
                if (!audio.Playing) audio.Play();
            });
        }

        this.OnProcess(delta =>
        {
            Position += Vector3.Right * delta * .2f;
        });

    }
}
