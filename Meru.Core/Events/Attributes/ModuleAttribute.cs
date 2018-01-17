using System;

namespace IA.Events.Attributes
{
    public class ModuleAttribute : Attribute
    {
        internal RuntimeModule module = new RuntimeModule();

        public bool CanBeDisabled
        {
            get => module.CanBeDisabled;
            set => module.CanBeDisabled = value;
        }

        public string Name
        {
            get => module.Name;
            set => module.Name = value;
        }

        public bool Nsfw
        {
            get => module.Nsfw;
            set => module.Nsfw = value;
        }

        public ModuleAttribute()
        {
        }

        public ModuleAttribute(string Name)
        {
            module.Name = Name;
        }
    }
}