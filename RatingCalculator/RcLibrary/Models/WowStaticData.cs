using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models
{
    public class WowStaticData
    {                
        [JsonProperty(PropertyName = "seasons")]
        public List<Season>? Seasons { get; set; }                       
    }
    
    
}
