
using Newtonsoft.Json;

namespace RcApi.Models
{
    public class WowCharacterViewModel
    {
        public string? Name { get; set; }
        public string? Realm { get; set; }
        public string? Guild { get; set; }
        public string? Region { get; set; }
        public string? Race { get; set; }
        public string? Class { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ActiveSpec { get; set; }
        public double? Rating { get; set; }
        public double? TargetRating { get; set; }
        public List<List<KeyRunViewModel>>? RunOptions { get; set; }        
        public DateTime LastCrawledAt { get; set; }        
        public string? ProfileUrl { get; set; }
    }
}
