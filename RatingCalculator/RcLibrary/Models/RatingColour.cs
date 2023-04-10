using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models
{
    public class RatingColour
    {
        [JsonProperty(PropertyName = "score")]
        public int? Score { get; set; }

        [JsonProperty(PropertyName = "rgbHex")]
        public string? RgbHex { get; set; }
        
    }
}
