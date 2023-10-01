namespace Utils
{
    public class Statemachine<State> : IMGUI_ProperyDrawer
        where State : struct, System.Enum
    {
        public State? previous { get; private set; }
        public float previous_time { get; private set; }
        public State? current { get; private set; }
        public float current_time { get; private set; }
        public float delta_time { get; private set; }
        public State? next;
        public bool entered => current_time == 0;
        public bool exiting => next != null;
        public float update_time { get; private set; }

        /// <summary>
        /// Returns Current State
        /// </summary>
        public State? Update(float delta)
        {
            delta_time = delta;
            if (next != null)
            {
                previous_time = current_time;
                previous = current;
                current = next;
                current_time = 0;
                next = default;
            }
            else
            {
                current_time += delta;
            }
            update_time += delta;
            return current;
        }

        bool IMGUI_ProperyDrawer.DrawProperty(IMGUI_Interface gui)
        {
            bool toggle = gui.Button(typeof(State).Name, ":", current);
            var control = (Godot.Control)gui;
            if (toggle) control.TooltipText = control.TooltipText == "" ? "0" : "";
            if (control.TooltipText == "0")
            {
                gui.Label("Previous:", previous, ":", previous_time);
                gui.Label("Current : ", current, ":", current_time);
            }
            return false;
        }
    }

    public class Statemachine : IMGUI_ProperyDrawer
    {
        public object previous { get; private set; }
        public float previous_time { get; private set; }
        public object current { get; private set; }
        public float current_time { get; private set; }
        public float delta_time { get; private set; }
        public object next;
        public bool entered => current_time == 0;
        public bool exiting => next != null;

        /// <summary>
        /// Returns Current State
        /// </summary>
        public object Update(float delta)
        {
            delta_time = delta;
            if (next != null)
            {
                previous_time = current_time;
                previous = current;
                current = next;
                current_time = 0;
                next = default;
            }
            else
            {
                current_time += delta;
            }
            return current;
        }

        System.Collections.Generic.Dictionary<System.Type, object> retained_states = new();

        public void Goto<Retained_State>() where Retained_State : class, new()
        {
            if (!retained_states.TryGetValue(typeof(Retained_State), out var state))
                retained_states[typeof(Retained_State)] = state = new Retained_State();
            next = (Retained_State)state;
        }

        bool IMGUI_ProperyDrawer.DrawProperty(IMGUI_Interface gui)
        {
            return gui.Label($"State {current_time}:").Property(current, out _);
        }
    }
}