using Godot;
using System;
using System.Collections.Generic;

namespace Utils.Effects
{
    public sealed partial class WeaponTrails : Node//, Prefab
    {
        struct Section
        {
            public Vector3 _start;
            public Vector3 _end;
            public float _life_time;
        }

        public enum UpdateMethod
        {
            Process,
            Physics,
            Manual
        }

        [Export] Material _material;
        [Export] UpdateMethod _update_method = UpdateMethod.Process;
        [Export] public bool _enable = true;
        [Export] public Node3D _start;
        [Export] public Node3D _end;
        [Export] float _life_span = .5f;
        [Export] public bool _visible = true;
        MeshInstance3D _mesh_instance = new MeshInstance3D();
        SurfaceTool _tool = new SurfaceTool();
        Section[] _points = new Section[32];
        int _points_count;
        List<Section> to_draw = new();
        public sealed override void _Notification(int what)
        {
            switch ((long)what)
            {
                case Node.NotificationReady:
                    AddChild(_mesh_instance);
                    _mesh_instance.MaterialOverride = _material;

                    switch (_update_method)
                    {
                        case UpdateMethod.Process:
                            SetProcess(true);
                            break;
                        case UpdateMethod.Physics:
                            SetPhysicsProcess(true);
                            break;
                    }
                    break;

                case Node.NotificationProcess:
                    Update((float)this.GetProcessDeltaTime());
                    break;

                case Node.NotificationPhysicsProcess:
                    Update((float)this.GetPhysicsProcessDeltaTime());
                    break;
            }
        }

        public void Update(float delta)
        {
            _mesh_instance.Visible = _visible;
            _tool.Clear();
            _tool.Begin(Mesh.PrimitiveType.Triangles);

            if (_points_count == _points.Length)
                System.Array.Resize(ref _points, _points.Length * 2);

            if (Node.IsInstanceValid(_start) && Node.IsInstanceValid(_end))
            {
                _points[_points_count] = new Section { _start = _start.GlobalPosition, _end = _end.GlobalPosition, _life_time = _enable ? _life_span : 0 };
                _points_count++;

                for (int i = 1; i < _points_count; ++i)
                {
                    _points[i - 1]._start = _points[i]._start;
                    _points[i - 1]._end = _points[i]._end;
                }
            }

            {
                int new_end_index = 0;
                for (int i = 0; i < _points_count; ++i)
                {
                    ref var point = ref _points[i];
                    if (point._life_time > _life_span)
                        point._life_time = _life_span;
                    point._life_time -= delta;
                    if (point._life_time > 0)
                    {
                        _points[new_end_index] = point;
                        new_end_index++;
                    }
                }
                _points_count = new_end_index;
            }

            for (int i = _points_count - 1; i >= 1; --i)
                Draw(i);

            _tool.GenerateNormals();
            _mesh_instance.Mesh = _tool.Commit();
        }

        void Draw(int index)
        {
            var uv_current = 1f - (float)index / (float)_points_count;
            var uv_prev = 1f - (((float)index - 1) / (float)_points_count);

            var current_top = _points[index]._end;
            var current_bot = _points[index]._start;
            var prev_top = _points[index - 1]._end;
            var prev_bot = _points[index - 1]._start;

            _tool.SetUV(new Vector2(uv_current, 0));
            _tool.AddVertex(current_top);
            _tool.SetUV(new Vector2(uv_prev, 0));
            _tool.AddVertex(prev_top);
            _tool.SetUV(new Vector2(uv_current, 1));
            _tool.AddVertex(current_bot);

            _tool.SetUV(new Vector2(uv_current, 1));
            _tool.AddVertex(current_bot);
            _tool.SetUV(new Vector2(uv_prev, 0));
            _tool.AddVertex(prev_top);
            _tool.SetUV(new Vector2(uv_prev, 1));
            _tool.AddVertex(prev_bot);
        }
    }
}
