using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models
{
    public class WeeksAffixes
    {
        [JsonProperty(PropertyName = "region")]
        public string? Region { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string? Title { get; set; }

        [JsonProperty(PropertyName = "affix_details")]
        public List<Affix>? Affixes { get; set; }
    }
}
