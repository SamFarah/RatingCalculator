using Newtonsoft.Json;

namespace RcLibrary.Models.RaiderIoModels;

public class Dungeon
{
    [JsonProperty(PropertyName = "id")]
    public int Id { get; set; }

    [JsonProperty(PropertyName = "challenge_mode_id")]
    public int ChallengeModeId { get; set; }

    [JsonProperty(PropertyName = "slug")]
    public string? Slug { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string? Name { get; set; }

    [JsonProperty(PropertyName = "short_name")]
    public string? ShortName { get; set; }
}

public class DungeonWithScores : Dungeon
{
    public double? Score { get; set; }
    public int TimeLimit { get; set; }

}
