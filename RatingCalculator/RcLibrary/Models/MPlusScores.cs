using Newtonsoft.Json;

namespace RcLibrary.Models;

public class MPlusScores
{
    [JsonProperty(PropertyName = "season")]
    public string? Season { get; set; }

    [JsonProperty(PropertyName = "scores")]
    public Dictionary<string, double>? Scores { get; set; }
}
