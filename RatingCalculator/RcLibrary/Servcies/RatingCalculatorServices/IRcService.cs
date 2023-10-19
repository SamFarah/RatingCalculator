using RcLibrary.Models;
using RcLibrary.Models.BlizzardModels;
using RcLibrary.Models.RaiderIoModels;

namespace RcLibrary.Servcies.RatingCalculatorServices;

public interface IRcService
{
    Task<ProcessedCharacter?> ProcessCharacter(string seasonSlug, string region, string realm, string name, double targetRating, bool thisweekOnly, List<string>? avoidDungs, int? maxKeyLevel);
    Task<Season?> GetCurrentSeason();
    List<DungeonMetrics> GetDungeonMetrics();
    Task<List<Realm>?> GetRegionRealmsAsync(string region);
    Task<List<Season>?> GetRegionSeasonsAsync(string region);
    Task<Season?> GetSeason(string region, string slug);
    Task<Season?> GetWowCurrentSeason(string region);
}