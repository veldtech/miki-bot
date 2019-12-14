namespace Miki.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class VerbAttribute : Attribute
    {
        public string Value { get; set; }

        public VerbAttribute(string verb)
        {
            Value = verb;
        }
    }
}
