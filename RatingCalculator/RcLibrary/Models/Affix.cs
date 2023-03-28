using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models
{
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
}
