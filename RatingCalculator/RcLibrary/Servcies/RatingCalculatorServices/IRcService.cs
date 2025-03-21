using RcLibrary.Models;
using RcLibrary.Models.BlizzardModels;
using RcLibrary.Models.RaiderIoModels;

namespace RcLibrary.Servcies.RatingCalculatorServices;

public interface IRcService
{
    Task<ProcessedCharacter?> ProcessCharacter(int expId, string seasonSlug, string region, string realm, string name, double targetRating, List<string>? avoidDungs, int? maxKeyLevel, string source = "web");
    List<DungeonMetrics> GetDungeonMetrics();
    Task<List<Realm>?> GetRegionRealmsAsync(string region);
    Task<List<Season>?> GetRegionSeasonsAsync(string region, int expId);
    Task<Season?> GetSeason(string region, string slug, int expId);
    Task<Season?> GetWowCurrentSeason(string region, int expId);
    Task<List<Expansion>> GetWowExpansionsAsync(string region);
    void RemoveCachedWowExpansions(string region);
    void RemoveCachedWowRealms(string region);
    double GetDugneonScore(double time, double timeLimit, int level);
}
