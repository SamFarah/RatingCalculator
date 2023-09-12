using Newtonsoft.Json;

namespace RcLibrary.Models;

public class RatingColour
{
    [JsonProperty(PropertyName = "score")]
    public int? Score { get; set; }

    [JsonProperty(PropertyName = "rgbHex")]
    public string? RgbHex { get; set; }

}
