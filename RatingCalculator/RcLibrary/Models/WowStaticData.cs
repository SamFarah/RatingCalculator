using Newtonsoft.Json;

namespace RcLibrary.Models
{
    public class WowStaticData
    {
        [JsonProperty(PropertyName = "seasons")]
        public List<Season>? Seasons { get; set; }
    }


}
