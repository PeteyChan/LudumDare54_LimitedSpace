namespace Internal
{
    class Commands
    {
        static bool show_fps;
        public static void FPS(Debug.Console args) => show_fps = !show_fps;
        static void Updater(global::Bootstrap.Process args)
        {
            if (show_fps)
            {
                var fps = Godot.Engine.GetFramesPerSecond();
                Godot.Color color = Godot.Colors.Cyan;
                if (fps < 60) color = Godot.Colors.Green;
                if (fps < 45) color = Godot.Colors.Yellow;
                if (fps < 30) color = Godot.Colors.Orange;
                if (fps < 15) color = Godot.Colors.Red;
                Debug.ColorLabel(color, fps);
            }
        }
    }
}