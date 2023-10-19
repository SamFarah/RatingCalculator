using RcLibrary.Models;
using RcLibrary.Models.BlizzardModels;

namespace RcLibrary.Servcies.BlizzardServices;
public interface IBlizzardService
{
    Task<List<Expansion>?> GetExpansionsAsync(string region);
    Task<List<Realm>?> GetRegionRealms(string region);
}