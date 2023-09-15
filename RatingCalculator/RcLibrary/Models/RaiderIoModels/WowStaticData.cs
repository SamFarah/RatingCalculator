using Newtonsoft.Json;

namespace RcLibrary.Models.RaiderIoModels;

public class WowStaticData
{
    [JsonProperty(PropertyName = "seasons")]
    public List<Season>? Seasons { get; set; }
}
