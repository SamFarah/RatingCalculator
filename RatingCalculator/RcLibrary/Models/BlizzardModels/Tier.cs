using Newtonsoft.Json;

namespace RcLibrary.Models.BlizzardModels;
public class Tier
{
    [JsonProperty(PropertyName = "id")]
    public int Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string? Name { get; set; } = string.Empty;
}
