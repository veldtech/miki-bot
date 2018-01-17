using IA.SDK;
using System;

namespace IA.Events.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        internal RuntimeCommandEvent command = new RuntimeCommandEvent();
        internal string on = "";

        public EventAccessibility Accessibility
        {
            get => command.Accessibility;
            set => command.Accessibility = value;
        }

        public string[] Aliases
        {
            get => command.Aliases;
            set => command.Aliases = value;
        }

        public string Name
        {
            get => command.Name;
            set => command.Name = value.ToLower();
        }

        public string On
        {
            get => on;
            set => on = value.ToLower();
        }

        public bool CanBeDisabled
        {
            get => command.CanBeDisabled;
            set => command.CanBeDisabled = value;
        }

        public CommandAttribute()
        {
        }
    }
}