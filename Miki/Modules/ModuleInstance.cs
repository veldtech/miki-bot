using System;

namespace Miki.Modules
{
    internal class ModuleInstance
    {
        private Func<object, object> p;

        public ModuleInstance(Func<object, object> p)
        {
            this.p = p;
        }
    }
}