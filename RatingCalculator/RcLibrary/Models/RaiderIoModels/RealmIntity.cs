﻿using Newtonsoft.Json;

namespace RcLibrary.Models.RaiderIoModels;

public class RealmIntity
{
    [JsonProperty(PropertyName = "name")]
    public string? Name { get; set; }

    [JsonProperty(PropertyName = "realm")]
    public string? Realm { get; set; }
}
