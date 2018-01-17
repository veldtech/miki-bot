using IA.SDK;
using IA.SDK.Interfaces;

namespace IA.Events
{
    public class BaseService : Event, IService
    {
        public virtual void Install(IModule m)
        {
            if (Module == null)
            {
                Module = m;
            }

            m.Services.Add(this);
        }

        public virtual void Uninstall(IModule m)
        {
            m.Services.Remove(this);
        }
    }
}