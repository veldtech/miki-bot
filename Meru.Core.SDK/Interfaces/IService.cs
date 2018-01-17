using IA.SDK.Events;

namespace IA.SDK.Interfaces
{
    public interface IService : IEvent
    {
        void Install(IModule m);

        void Uninstall(IModule m);
    }
}