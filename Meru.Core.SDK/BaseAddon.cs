using System.Threading.Tasks;

namespace IA.SDK
{
    public interface IAddon
    {
        Task<IAddonInstance> Create(IAddonInstance i);
    }
}