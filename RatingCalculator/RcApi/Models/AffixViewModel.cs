using Newtonsoft.Json;

namespace RcApi.Models
{
    public class AffixViewModel
    {        
        public int Id { get; set; }       
        public string? Name { get; set; }        
        public string? Description { get; set; }        
        public string? IconUrl { get; set; }        
        public string? WowheadUrl { get; set; }
    }
}
