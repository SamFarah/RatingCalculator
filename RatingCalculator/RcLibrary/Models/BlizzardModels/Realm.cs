using Newtonsoft.Json;

namespace RcLibrary.Models.BlizzardModels;
public class Realm
{
    [JsonProperty(PropertyName = "id")]
    public int Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string? Name { get; set; }

    [JsonProperty(PropertyName = "slug")]
    public string? Slug { get; set; }
}
