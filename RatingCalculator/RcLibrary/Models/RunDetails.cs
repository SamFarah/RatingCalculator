using Newtonsoft.Json;

namespace RcLibrary.Models;

public class RunsResponse
{
    [JsonProperty(PropertyName = "rankings")]
    public List<Ranking>? Rankings { get; set; }
}


public class Ranking
{
    [JsonProperty(PropertyName = "Run")]
    public RunDetails? run { get; set; }
}

public class RunDetails
{
    [JsonProperty(PropertyName = "dungeon")]
    public DungeonDetails? Dungeon { get; set; }
}

public class DungeonDetails
{
    [JsonProperty(PropertyName = "id")]
    public int Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string? Name { get; set; }


    [JsonProperty(PropertyName = "short_name")]
    public string? ShortName { get; set; }

    [JsonProperty(PropertyName = "slug")]
    public string? Slug { get; set; }

    [JsonProperty(PropertyName = "keystone_timer_ms")]
    public int TimeLimitMS { get; set; }

}
