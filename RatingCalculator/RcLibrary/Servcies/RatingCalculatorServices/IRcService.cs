using RcLibrary.Models;

namespace RcLibrary.Servcies.RatingCalculatorServices;

public interface IRcService
{
    Task<ProcessedCharacter?> ProcessCharacter(string region, string realm, string name, double targetRating, bool thisweekOnly, List<string>? avoidDungs, int? maxKeyLevel);
    Task<Season?> GetSeason();
    List<DungeonMetrics> GetDungeonMetrics();
}