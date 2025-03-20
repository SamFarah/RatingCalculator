namespace RcLibrary.Models.Configurations;

public class Settings
{
    public const string MainSectionName = "Settings";
    public string? RaiderIOAPI { get; set; }
    public BlizzardApiSettings BlizzardApi { get; set; } = new BlizzardApiSettings();
    public int OldestRaiderIOExpId { get; set; }
    public int CurrentExpansionIdFallBack { get; set; }
}
