using MythicPlanner.Helpers;
using RcLibrary.Models;

namespace MythicPlanner.Models;

public class WowCharacterViewModel
{
    public string? Name { get; set; }
    public string? Realm { get; set; }
    public string? Guild { get; set; }
    public string? Region { get; set; }
    public string? Race { get; set; }
    public string? Class { get; set; }
    public string ClassColour
    {
        get
        {
            return UiHelpers.GetClassColour(Class ?? "");
        }
    }
    public string? Faction { get; set; }
    public string FactionColour
    {
        get
        {
            return UiHelpers.GetFactionColour(Faction ?? "");
        }
    }

    public string? ThumbnailUrl { get; set; }
    public string? ActiveSpec { get; set; }
    public Rating? Rating { get; set; }
    public Rating? TargetRating { get; set; }
    public List<List<KeyRunViewModel>>? RunOptions { get; set; }
    public DateTime LastCrawledAt { get; set; }
    public string? ProfileUrl { get; set; }
    public int ThisWeekAffixId { get; set; }
    public string? ShareableUrl { get; set; }
}
