﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models
{
    public class RealmIntity
    {
        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }
        
        [JsonProperty(PropertyName = "realm")]
        public string? Realm { get; set; }
    }
}
