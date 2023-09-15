using RcLibrary.Models.BlizzardModels;

namespace RcLibrary.Servcies.BlizzardServices;
public interface IBlizzardService
{
    Task<List<Realm>?> GetRegionRealms(string region);
}