using Newtonsoft.Json;

namespace RcLibrary.Models.BlizzardModels;
public class AccessToken
{
    [JsonProperty(PropertyName = "access_token")]
    public string? Token { get; set; }

    [JsonProperty(PropertyName = "token_type")]
    public string? Type { get; set; }

    [JsonProperty(PropertyName = "expires_in")]
    public int ExpiresInMins { get; set; }

    [JsonProperty(PropertyName = "sub")]
    public string? Sub { get; set; }
}

