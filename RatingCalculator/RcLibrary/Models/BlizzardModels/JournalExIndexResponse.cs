using Newtonsoft.Json;

namespace RcLibrary.Models.BlizzardModels;
public class JournalExIndexResponse
{
    [JsonProperty(PropertyName = "tiers")]
    public List<Tier>? Tiers { get; set; }
}