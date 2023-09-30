using Godot;
using System.Collections.Generic;

public static partial class Debug // Draw3d
{
    public static void DrawLine3D(Vector3 global_origin, Vector3 global_end, Color color)
    {
        DrawLine3DImpl.Get().Lines.Add((global_origin, global_end, color));
    }

    public static void DrawLine3D(Transform3D transform, Vector3 start, Vector3 end, Color color)
        => DrawLine3D(transform.TranslatedLocal(start).Origin, transform.TranslatedLocal(end).Origin, color);

    public static void DrawSquare3D(Vector3 global_origin, float size, Color color)
        => DrawRectangle3D(global_origin, new Vector2(size / 2, size / 2), color);

    public static void DrawArrow3D(Vector3 global_origin, Vector3 direction, Color color)
    {
        Debug.DrawLine3D(global_origin, global_origin + direction, color);

        var Viewport = ((SceneTree)Godot.Engine.GetMainLoop()).Root.GetViewport();
        var right = direction.Cross(Viewport.GetCamera3D().GlobalTransform.Basis.Z).Normalized();
        var head = global_origin + direction * .9f;
        var length = (direction * .1f).Length();
        Debug.DrawLine3D(head + right * length, global_origin + direction, color);
        Debug.DrawLine3D(head - right * length, global_origin + direction, color);
    }

    public static void DrawRectangle3D(Vector3 global_origin, Vector2 extents, Color color)
    {
        var p1 = global_origin + new Vector3(extents.X, 0, extents.Y);
        var p2 = global_origin + new Vector3(-extents.X, 0, extents.Y);
        var p3 = global_origin + new Vector3(extents.X, 0, -extents.Y);
        var p4 = global_origin + new Vector3(-extents.X, 0, -extents.Y);

        DrawLine3D(p1, p2, color);
        DrawLine3D(p1, p3, color);
        DrawLine3D(p2, p4, color);
        DrawLine3D(p3, p4, color);
    }

    public static void DrawCircle3D(Vector3 global_origin, float radius, Color color)
        => DrawCircle3D(global_origin, Vector3.Up, radius, color);

    public static void DrawCircle3D(Vector3 global_origin, Vector3 normal, float radius, Color color, int segments = 16)
    {
        if (segments < 3) segments = 3;
        var arc_angle = Mathf.Pi / ((float)segments / 2f);

        if (normal == Vector3.Zero)
            return;
        if (normal == Vector3.Up)
            normal.X += 0.00001f;

        normal = normal.Normalized();
        Transform3D t = Transform3D.Identity;
        t = t.LookingAt(normal, Vector3.Up);
        t.Origin = global_origin;

        var angle = 0f;
        var start = t.TranslatedLocal(new Vector3(radius, 0, 0)).Origin;
        for (int i = 0; i < segments; ++i)
        {
            angle += arc_angle;
            var end = t.TranslatedLocal(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0)).Origin;
            DrawLine3D(start, end, color);
            start = end;
        }
    }

    public static void DrawCube(Transform3D transform, Vector3 extents, Color color)
    {
        var x = extents.X;
        var y = extents.Y;
        var z = extents.Z;

        DrawLine3D(transform, new Vector3(-x, -y, -z), new Vector3(-x, -y, z), color);
        DrawLine3D(transform, new Vector3(-x, -y, -z), new Vector3(x, -y, -z), color);
        DrawLine3D(transform, new Vector3(-x, -y, z), new Vector3(x, -y, z), color);
        DrawLine3D(transform, new Vector3(x, -y, -z), new Vector3(x, -y, z), color);

        DrawLine3D(transform, new Vector3(-x, y, -z), new Vector3(-x, y, z), color);
        DrawLine3D(transform, new Vector3(-x, y, -z), new Vector3(x, y, -z), color);
        DrawLine3D(transform, new Vector3(-x, y, z), new Vector3(x, y, z), color);
        DrawLine3D(transform, new Vector3(x, y, -z), new Vector3(x, y, z), color);

        DrawLine3D(transform, new Vector3(-x, -y, -z), new Vector3(-x, y, -z), color);
        DrawLine3D(transform, new Vector3(-x, -y, z), new Vector3(-x, y, z), color);
        DrawLine3D(transform, new Vector3(x, -y, -z), new Vector3(x, y, -z), color);
        DrawLine3D(transform, new Vector3(x, -y, z), new Vector3(x, y, z), color);
    }

    public static void DrawCube(Transform3D transform, float size, Color color)
        => DrawCube(transform, Vector3.One * size / 2f, color);

    public static void DrawCapsule(Transform3D transform, float height, float radius, Color color, bool draw_rings = false)
    {
        Vector3 Xform(Vector3 position) => transform.TranslatedLocal(position).Origin;
        var arc_angle = Mathf.Pi / 8f;

        height /= 2f;
        var angle = 0f;
        var start = Xform(new Vector3(radius, 0, height));
        Debug.DrawLine3D(start, Xform(new Vector3(radius, 0, -height)), color);
        for (int i = 0; i < 8; ++i)
        {
            angle += arc_angle;
            var end = Xform(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius + height));
            Debug.DrawLine3D(start, end, color);
            start = end;
        }
        start = Xform(new Vector3(-radius, 0, -height));
        Debug.DrawLine3D(start, Xform(new Vector3(-radius, 0, height)), color);
        for (int i = 0; i < 8; ++i)
        {
            angle += arc_angle;
            var end = Xform(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius - height));
            Debug.DrawLine3D(start, end, color);
            start = end;
        }

        angle = 0f;
        start = Xform(new Vector3(0, radius, height));
        Debug.DrawLine3D(start, Xform(new Vector3(0, radius, -height)), color);
        for (int i = 0; i < 8; ++i)
        {
            angle += arc_angle;
            var end = Xform(new Vector3(0, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius + height));
            Debug.DrawLine3D(start, end, color);
            start = end;
        }
        start = Xform(new Vector3(0, -radius, -height));
        Debug.DrawLine3D(start, Xform(new Vector3(0, -radius, height)), color);
        for (int i = 0; i < 8; ++i)
        {
            angle += arc_angle;
            var end = Xform(new Vector3(0, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius - height));
            Debug.DrawLine3D(start, end, color);
            start = end;
        }

        if (draw_rings)
        {
            angle = 0;
            start = Xform(new Vector3(radius, 0, -height));
            for (int i = 0; i < 16; ++i)
            {
                angle += arc_angle;
                var end = Xform(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -height));
                Debug.DrawLine3D(start, end, color);
                start = end;
            }
            angle = 0;
            start = Xform(new Vector3(radius, 0, height));
            for (int i = 0; i < 16; ++i)
            {
                angle += arc_angle;
                var end = Xform(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, height));
                Debug.DrawLine3D(start, end, color);
                start = end;
            }
        }
    }


    public static void DrawLine2D(Vector2 start, Vector2 end, Color color)
    {
        DrawLine2DImpl.Get().Lines.Add((start, end, color));
    }

    public static void DrawSquare2D(Vector2 position, float size, Color color)
        => DrawRectangle2D(position, new Vector2(size, size), color);

    public static void DrawRectangle2D(Vector2 position, Vector2 size, Color color)
    {
        size /= 2f;
        var top_left = new Vector2(position.X - size.X, position.Y - size.Y);
        var top_right = new Vector2(position.X + size.X, position.Y - size.Y);
        var bot_left = new Vector2(position.X - size.X, position.Y + size.Y);
        var bot_right = new Vector2(position.X + size.X, position.Y + size.Y);
        Debug.DrawLine2D(top_left, top_right, color);
        Debug.DrawLine2D(bot_left, bot_right, color);
        Debug.DrawLine2D(top_left, bot_left, color);
        Debug.DrawLine2D(top_right, bot_right, color);
    }

    public static void DrawCircle2D(Vector2 position, float radius, Color color, int segments = 16)
    {
        if (segments < 3) segments = 3;

        var arc_angle = Mathf.Pi / ((float)segments / 2f);

        Transform2D t = Transform2D.Identity;
        t.Origin = position;
        var angle = 0f;
        var start = t.TranslatedLocal(new Vector2(radius, 0)).Origin;
        for (int i = 0; i < segments; ++i)
        {
            angle += arc_angle;
            var end = t.TranslatedLocal(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius)).Origin;
            DrawLine2D(start, end, color);
            start = end;
        }
    }

    partial class DrawLine2DImpl : Godot.MeshInstance2D
    {
        static DrawLine2DImpl instance = new DrawLine2DImpl() { Name = "Debug Line 2D" };
        static DrawLine2DImpl()
        {
            ((SceneTree)Godot.Engine.GetMainLoop())
                .Root.AddChild(instance, false, InternalMode.Back);
        }

        public static DrawLine2DImpl Get() => instance;
        public List<(Vector2 start, Vector2 end, Color color)> Lines = new List<(Vector2 start, Vector2 end, Color color)>();
        ImmediateMesh mesh;
        Material material;
        public DrawLine2DImpl()
        {
            Mesh = mesh = new ImmediateMesh();
            var material = new StandardMaterial3D();
            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            material.VertexColorUseAsAlbedo = true;
            this.Material = material;
        }

        public override void _Process(double delta)
        {
            mesh.ClearSurfaces();
            if (Lines.Count == 0) return;
            mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
            foreach (var line in Lines)
            {
                mesh.SurfaceSetColor(line.color);
                mesh.SurfaceAddVertex(new Vector3(line.start.X, line.start.Y, 0));
                mesh.SurfaceSetColor(line.color);
                mesh.SurfaceAddVertex(new Vector3(line.end.X, line.end.Y, 0));
            }
            mesh.SurfaceEnd();
            Lines.Clear();
        }
    }
    partial class DrawLine3DImpl : Godot.MeshInstance3D
    {
        static DrawLine3DImpl instance = new DrawLine3DImpl { Name = "Debug Line 3D" };

        public static DrawLine3DImpl Get() => instance;

        static DrawLine3DImpl()
        {
            ((SceneTree)Godot.Engine.GetMainLoop())
                .Root.AddChild(instance, false, InternalMode.Back);
        }

        public List<(Vector3 start, Vector3 end, Color color)> Lines = new List<(Vector3 start, Vector3 end, Color color)>();
        ImmediateMesh mesh;
        Material material;

        public DrawLine3DImpl()
        {
            Mesh = mesh = new ImmediateMesh();
            var material = new StandardMaterial3D();
            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            material.VertexColorUseAsAlbedo = true;
            material.NoDepthTest = true;
            this.MaterialOverride = material;
        }

        public override void _Process(double delta)
        {
            mesh.ClearSurfaces();
            if (Lines.Count == 0) return;
            mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
            foreach (var line in Lines)
            {
                mesh.SurfaceSetColor(line.color);
                mesh.SurfaceAddVertex(line.start);
                mesh.SurfaceSetColor(line.color);
                mesh.SurfaceAddVertex(line.end);
            }
            mesh.SurfaceEnd();
            Lines.Clear();
        }
    }
}