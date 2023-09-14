using RcLibrary.Models;

namespace RcLibrary.RCLogic;

public interface IRcLogic
{
    Task<ProcessedCharacter?> ProcessCharacter(string region, string realm, string name, double targetRating, bool thisweekOnly, List<string>? avoidDungs, int? maxKeyLevel);
    Task<Affix?> GetCurrentBaseAffix(string region);
    Task<Season?> GetSeason();
    List<DungeonMetrics> GetDungeonMetrics();
}