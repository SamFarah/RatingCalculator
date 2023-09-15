using Newtonsoft.Json;

namespace RcLibrary.Models.RaiderIoModels;

public class Affix
{
    [JsonProperty(PropertyName = "id")]
    public int Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string? Name { get; set; }

    [JsonProperty(PropertyName = "description")]
    public string? Description { get; set; }

    private string? iconUrl;
    [JsonProperty(PropertyName = "icon")]
    public string? IconUrl
    {
        get { return $"https://wow.zamimg.com/images/wow/icons/large/{iconUrl}.jpg"; }
        set { iconUrl = value; }
    }

    [JsonProperty(PropertyName = "wowhead_url")]
    public string? WowheadUrl { get; set; }
}
