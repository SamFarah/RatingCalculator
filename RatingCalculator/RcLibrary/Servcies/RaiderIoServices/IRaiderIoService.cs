using RcLibrary.Models.RaiderIoModels;

namespace RcLibrary.Servcies.RaiderIoServices;
public interface IRaiderIoService
{
    Task<RaiderIoCharacter?> GetCharacter(string region, string realm, string name, string season);
    Task<Affix?> GetCurrentBaseAffix(string region);
    Task<int> GetDungoenTimeLimit(string region, string seasonName, string dungeonName);
    Task<List<RatingColour>?> GetRatingColours(string seasonName);
    Task<List<Season>?> GetRegionSeasons(string region, int expId);        
}