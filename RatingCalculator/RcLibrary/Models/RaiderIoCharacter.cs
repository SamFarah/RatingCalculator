using Newtonsoft.Json;

namespace RcLibrary.Models
{
    public class RaiderIoCharacter : WowCharacter
    {
        [JsonProperty(PropertyName = "mythic_plus_scores_by_season")]
        public List<MPlusScores>? MPlusSeasonScores { get; set; }

        [JsonProperty(PropertyName = "mythic_plus_best_runs")]
        public List<KeyRun>? BestMythicRuns { get; set; }

        [JsonProperty(PropertyName = "mythic_plus_alternate_runs")]
        public List<KeyRun>? AlternateMythicRuns { get; set; }        
    }
}
