using Godot;

public static partial class Effects
{
    static ColorRect fade;
    public static bool FadeToWhite(float delta)
    {
        if (!fade.IsValid())
        {
            fade = new ColorRect { }.AddToScene();
            fade.AnchorLeft = 0;
            fade.AnchorRight = 1;
            fade.AnchorTop = 0;
            fade.AnchorBottom = 1;
            fade.Color = new Color(1, 1, 1, 0);
        }

        fade.Color = fade.Color.Lerp(Godot.Colors.White, delta * 5f);
        if (fade.Color.A > .99f)
        {
            fade.Color = Colors.White;
            return true;
        }
        return false;
    }

    public static bool FadetoBlack(float delta)
    {
        if (!fade.IsValid())
        {
            fade = new ColorRect { }.AddToScene();
            fade.AnchorLeft = 0;
            fade.AnchorRight = 1;
            fade.AnchorTop = 0;
            fade.AnchorBottom = 1;
            fade.Color = new Color(0, 0, 0, 0);
        }

        fade.Color = fade.Color.Lerp(Godot.Colors.Black, delta * 5f);
        if (fade.Color.A > .99f)
        {
            fade.Color = Colors.Black;
            return true;
        }
        return false;
    }

    public static bool FadeFromBlack(float delta)
    {
        if (!fade.IsValid())
        {
            fade = new ColorRect { }.AddToScene();
            fade.AnchorLeft = 0;
            fade.AnchorRight = 1;
            fade.AnchorTop = 0;
            fade.AnchorBottom = 1;
            fade.Color = Colors.Black;
        }

        fade.Color = fade.Color.Lerp(new Color(0, 0, 0, 0), delta * 5f);
        if (fade.Color.A < 0)
        {
            fade.Color = new Color(0, 0, 0, 0);
            return true;
        }
        return false;
    }
}