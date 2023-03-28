using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models
{
    public class MPlusScores
    {
        [JsonProperty(PropertyName = "season")]
        public string? Season { get; set; }

        [JsonProperty(PropertyName = "scores")]
        public Dictionary<string, double>? Scores { get; set; }
    }
}
