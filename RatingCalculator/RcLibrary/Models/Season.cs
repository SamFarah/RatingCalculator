using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models
{
    public class Season
    {
        [JsonProperty(PropertyName = "slug")]
        public string? Slug { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "short_name")]
        public string? ShortName { get; set; }

        [JsonProperty(PropertyName = "seasonal_affix")]
        public Affix? SeasonalAffix { get; set; }

        [JsonProperty(PropertyName = "dungeons")]
        public List<Dungeon>? Dungeons { get; set; }
    }
}
