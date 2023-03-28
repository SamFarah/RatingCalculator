using Newtonsoft.Json;

namespace RcLibrary.Models
{
    public class Dungeon
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "challenge_mode_id")]
        public int ChallengeModeId { get; set; }

        [JsonProperty(PropertyName = "slug")]
        public string? Slug { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "short_name")]
        public string? ShortName { get; set; }
    }

    public class DungeonWithScores : Dungeon
    {
        public double? FortScore { get; set; }
        public double? TyrScore { get; set; }
        public double? Score
        {
            get
            {
                return (Math.Max(FortScore ?? 0, TyrScore ?? 0) * 1.5) + (Math.Min(FortScore ?? 0, TyrScore ?? 0) * 0.5);
            }
        }

        public int TimeLimit { get; set; }

    }
}
