using Newtonsoft.Json;

namespace RcLibrary.Models.RaiderIoModels;

public class Season
{
    [JsonProperty(PropertyName = "slug")]
    public string? Slug { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string? Name { get; set; }

    [JsonProperty(PropertyName = "short_name")]
    public string? ShortName { get; set; }

    [JsonProperty(PropertyName = "seasonal_affix")]
    public Affix? SeasonalAffix { get; set; }

    [JsonProperty(PropertyName = "dungeons")]
    public List<Dungeon>? Dungeons { get; set; }

    [JsonProperty(PropertyName = "starts")]
    public Dictionary<string, DateTime?>? Starts { get; set; }

    [JsonProperty(PropertyName = "ends")]
    public Dictionary<string, DateTime?>? Ends { get; set; }


}
