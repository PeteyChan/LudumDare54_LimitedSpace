using Godot;
using System;

public static partial class Effects
{
    public static void Explosion(Vector3 position, float size)
    {
        var node = GD.Load<PackedScene>("res://Assets/Effects/Explosion/explosion.tscn").Instantiate() as Node3D;
        node.Position = position;
        node.Scale = size * Vector3.One;
        node.AddToScene();

        //SFX.HeavyImpact.Play(position, size / 3f);
        SFX.Explosion.Play(position, size / 3f);
        //SFX.HeavyImpact.Play(position);

        if (node.TryFind(out AnimatedSprite3D sprite))
        {
            sprite.Play();
            sprite.OnProcess(() =>
            {
                if (!sprite.IsPlaying())
                    node.QueueFree();
            });
        }
    }
}
