using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models
{
    public class WowCharacter : RealmIntity
    {        
        [JsonProperty(PropertyName = "race")]
        public string? Race { get; set; }

        [JsonProperty(PropertyName = "class")]
        public string? Class { get; set; }

        [JsonProperty(PropertyName = "active_spec_name")]
        public string? ActiveSpec { get; set; }

        [JsonProperty(PropertyName = "active_spec_role")]
        public string? ActiveRole { get; set; }

        [JsonProperty(PropertyName = "faction")]
        public string? Faction { get; set; }

        [JsonProperty(PropertyName = "thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [JsonProperty(PropertyName = "region")]
        public string? Region { get; set; }

        [JsonProperty(PropertyName = "guild")]
        public RealmIntity? Guild { get; set; }
        

      
    }   
}
