using RcLibrary.Models.RaiderIoModels;

namespace RcLibrary.Servcies.RaiderIoServices;
public interface IRaiderIoService
{
    Task<RaiderIoCharacter?> GetCharacter(string region, string realm, string name, string season);

    [Obsolete("This no longer needed for WW expansion, use GetWeeklyAffixes instead")]
    Task<Affix?> GetCurrentBaseAffix(string region);
    Task<List<Affix>?> GetWeeklyAffixes(string region);

    Task<int> GetDungoenTimeLimit(string region, string seasonName, string dungeonName);    
    Task<List<RatingColour>?> GetRatingColours(string seasonName);
    Task<List<Season>?> GetRegionSeasons(string region, int expId);
}