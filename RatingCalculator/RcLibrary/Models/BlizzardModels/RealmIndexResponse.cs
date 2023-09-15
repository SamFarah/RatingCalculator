using Newtonsoft.Json;

namespace RcLibrary.Models.BlizzardModels;
public class RealmIndexResponse
{
    [JsonProperty(PropertyName = "realms")]
    public List<Realm>? Realms { get; set; }
}




