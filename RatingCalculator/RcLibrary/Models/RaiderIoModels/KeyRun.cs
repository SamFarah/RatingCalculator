using Newtonsoft.Json;

namespace RcLibrary.Models.RaiderIoModels;

public class KeyRun
{
    [JsonProperty(PropertyName = "dungeon")]
    public string? DungeonName { get; set; }

    [JsonProperty(PropertyName = "short_name")]
    public string? DungeonShortName { get; set; }

    [JsonProperty(PropertyName = "mythic_level")]
    public int KeyLevel { get; set; }

    [JsonProperty(PropertyName = "completed_at")]
    public DateTime CompletedOn { get; set; }

    [JsonProperty(PropertyName = "clear_time_ms")]
    public int ClearTimeMs { get; set; }

    [JsonProperty(PropertyName = "par_time_ms")]
    public int TimeLimit { get; set; }

    [JsonProperty(PropertyName = "num_keystone_upgrades")]
    public int KeyPlusses { get; set; }

    [JsonProperty(PropertyName = "map_challenge_mode_id")]
    public int ChallengeModeId { get; set; }

    [JsonProperty(PropertyName = "zone_id")]
    public int ZoneId { get; set; }

    [JsonProperty(PropertyName = "score")]
    public double Rating { get; set; }

    [JsonProperty(PropertyName = "affixes")]
    public List<Affix>? Affixes { get; set; }

    [JsonProperty(PropertyName = "url")]
    public string? RunUrl { get; set; }

    public double? OldScore { get; set; }
    public double? NewScore { get; set; }
}
