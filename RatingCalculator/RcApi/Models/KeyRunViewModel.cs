using Newtonsoft.Json;
using RcLibrary.Models;
using System;

namespace RcApi.Models
{
    public class KeyRunViewModel
    {
        public string? DungeonName { get; set; }
        public int KeyLevel { get; set; }
        public int ClearTimeMs { get; set; }
        public int TimeLimit { get; set; }
        public List<Affix>? Affixes { get; set; }
        public double? OldScore { get; set; }
        public double? NewScore { get; set; }
        public string TimeLimitString
        {
            get
            {
                var timespan = TimeSpan.FromMilliseconds(TimeLimit);
                return timespan.ToString(@"hh\:mm\:ss");
            }
        }
        public string ClearTimeString
        {
            get
            {
                var timespan = TimeSpan.FromMilliseconds(ClearTimeMs);
                return timespan.ToString(@"hh\:mm\:ss");
            }
        }

        public string OverUnderTime
        {
            get
            {                
                if (ClearTimeMs > TimeLimit)
                {
                    
                    var timeOver = ClearTimeMs - TimeLimit;
                    return TimeSpan.FromMilliseconds(timeOver).ToString(@"mm\:ss\.ff");
                }
                else
                {
                    var timeUnder = TimeLimit - ClearTimeMs;
                    return TimeSpan.FromMilliseconds(timeUnder).ToString(@"mm\:ss\.ff");
                }

            }
        }

        public bool OverTime { get { return ClearTimeMs > TimeLimit; } }
        public string ScoreAdjust { get { return Math.Round((NewScore - OldScore)??0,0).ToString(); } }





    }
}
