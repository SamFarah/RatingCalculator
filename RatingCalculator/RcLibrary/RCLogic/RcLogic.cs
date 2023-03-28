using AutoMapper;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RcLibrary.Helpers;
using RcLibrary.Models;
using RcLibrary.Models.Configurations;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using System.Xml.Linq;

namespace RcLibrary.RCLogic
{
    public class RcLogic : IRcLogic
    {
        private readonly ILogger<RcLogic> _logger;
        private readonly IApiHelper _raiderIoApi;
        private readonly IMapper _mapper;
        private readonly Settings _config;

        private List<DungeonMetrics> dungeonMatrix = new List<DungeonMetrics>()
        {
            new DungeonMetrics { Level = 30 , Base = 240},
            new DungeonMetrics { Level = 29 , Base = 233},
            new DungeonMetrics { Level = 28 , Base = 226},
            new DungeonMetrics { Level = 27 , Base = 219},
            new DungeonMetrics { Level = 26 , Base = 212},
            new DungeonMetrics { Level = 25 , Base = 205},
            new DungeonMetrics { Level = 24 , Base = 198},
            new DungeonMetrics { Level = 23 , Base = 191},
            new DungeonMetrics { Level = 22 , Base = 184},
            new DungeonMetrics { Level = 21 , Base = 177},
            new DungeonMetrics { Level = 20 , Base = 170},
            new DungeonMetrics { Level = 19 , Base = 163},
            new DungeonMetrics { Level = 18 , Base = 156},
            new DungeonMetrics { Level = 17 , Base = 149},
            new DungeonMetrics { Level = 16 , Base = 142},
            new DungeonMetrics { Level = 15 , Base = 135},
            new DungeonMetrics { Level = 14 , Base = 128},
            new DungeonMetrics { Level = 13 , Base = 121},
            new DungeonMetrics { Level = 12 , Base = 114},
            new DungeonMetrics { Level = 11 , Base = 107},
            new DungeonMetrics { Level = 10 , Base = 100},
            new DungeonMetrics { Level = 9  , Base = 85 },
            new DungeonMetrics { Level = 8  , Base = 80 },
            new DungeonMetrics { Level = 7  , Base = 75 },
            new DungeonMetrics { Level = 6  , Base = 65 },
            new DungeonMetrics { Level = 5  , Base = 60 },
            new DungeonMetrics { Level = 4  , Base = 55 },
            new DungeonMetrics { Level = 3  , Base = 45 },
            new DungeonMetrics { Level = 2  , Base = 40 },
        };

        public RcLogic(ILogger<RcLogic> logger,
                       IApiHelper raiderIoApi,
                       IOptions<Settings> config,
                       IMapper mapper)
        {
            _logger = logger;
            _config = config.Value;
            _raiderIoApi = raiderIoApi;
            _mapper = mapper;
            _raiderIoApi.InitializeClient(_config?.RaiderIOAPI ?? "");
        }

        private async Task<RaiderIoCharacter?> GetCharacter(string region, string realm, string name, string season)
        {
            string[] fields = {
                $"mythic_plus_scores_by_season:{season}",
                "mythic_plus_best_runs",
                "mythic_plus_alternate_runs",
                "guild"
            };
            var qsParams = new Dictionary<string, string>() {
                { "region", region},
                { "realm", realm },
                { "name", name },
                { "fields", string.Join(",",fields)}
            };
            var endpoint = new Uri(QueryHelpers.AddQueryString("characters/profile", qsParams), UriKind.Relative);
            try
            {
                var toon = await _raiderIoApi.GetAsync<RaiderIoCharacter>(endpoint.ToString());
                return toon;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting character from raider.io:{errorMessage}", ex.Message);
                return null;
            }

        }

        public async Task<WowStaticData?> GetWowStaticData()
        {
            var qsParams = new Dictionary<string, string>() { { "expansion_id", _config.ExpansionId.ToString() } };
            var endpoint = new Uri(QueryHelpers.AddQueryString("mythic-plus/static-data", qsParams), UriKind.Relative);
            try
            {
                var staticData = await _raiderIoApi.GetAsync<WowStaticData>(endpoint.ToString());
                return staticData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting static data from raider.io:{errorMessage}", ex.Message);
                return null;
            }

        }

        public async Task<Affix?> GetCurrentBaseAffix(string region)
        {
            var qsParams = new Dictionary<string, string>() { { "region", region } };
            var endpoint = new Uri(QueryHelpers.AddQueryString("mythic-plus/affixes", qsParams), UriKind.Relative);
            try
            {
                var staticData = await _raiderIoApi.GetAsync<WeeksAffixes>(endpoint.ToString());
                return staticData?.Affixes?.Where(x => x.Id == 9 || x.Id == 10).First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting static data from raider.io:{errorMessage}", ex.Message);
                return null;
            }
        }

        public async Task<ProcessedCharacter?> ProcessCharacter(string region, string realm, string name, string season, double targetRating, WowStaticData? staticWowData)
        {
            var thisWeeksAffix = await GetCurrentBaseAffix(region);
            var raiderIoToon = await GetCharacter(region, realm, name, season);
            if (raiderIoToon == null) { return null; }
            var allBestPlayerRuns = new List<KeyRun>();
            int FortAffixID = 10;
            double? maxObtainableDunScore = 490.0;

            if (raiderIoToon?.BestMythicRuns != null) allBestPlayerRuns.AddRange(raiderIoToon.BestMythicRuns);
            if (raiderIoToon?.AlternateMythicRuns != null) allBestPlayerRuns.AddRange(raiderIoToon.AlternateMythicRuns);

            ProcessedCharacter output = _mapper.Map<ProcessedCharacter>(raiderIoToon);
            output.TargetRating = targetRating;
            var selectedSeason = raiderIoToon?.MPlusSeasonScores?.Where(x => x.Season == season).FirstOrDefault();
            if (selectedSeason?.Scores != null)
            {
                output.Rating = selectedSeason.Scores["all"];
            }

            if (output.Rating >= targetRating) { return output; }

            //var ratingNeeded = targetRating - output.Rating;
            var seasonInfo = staticWowData?.Seasons?.Where(x => x.Slug == season).FirstOrDefault();
            if (seasonInfo?.Dungeons != null)
            {
                var SeasonDungoens = _mapper.Map<List<DungeonWithScores>>(seasonInfo.Dungeons);
                foreach (var playerRun in allBestPlayerRuns)
                {
                    var currentDun = SeasonDungoens.Where(x => x.ChallengeModeId == playerRun.ChallengeModeId).FirstOrDefault();
                    if (currentDun != null && playerRun?.Affixes != null)
                    {
                        currentDun.TimeLimit = playerRun.TimeLimit;
                        if (playerRun.Affixes.Where(x => x.Id == FortAffixID).ToList().Count > 0) currentDun.FortScore = playerRun.Rating;
                        else currentDun.TyrScore = playerRun.Rating;
                    }
                }
                double? ratingPerDung = targetRating / seasonInfo.Dungeons.Count;
                double? maxScore = ratingPerDung;
                SeasonDungoens = SeasonDungoens.OrderByDescending(x => x.Score).ToList();
                List<DungeonWithScores> runPool = new List<DungeonWithScores>();

                foreach (var dungeon in SeasonDungoens)
                {
                    if (dungeon.Score > ratingPerDung)
                    {
                        if (dungeon.Score > maxScore) { maxScore = dungeon.Score; }
                        var extraRating = (dungeon.Score - ratingPerDung) / seasonInfo.Dungeons.Count;
                        ratingPerDung -= extraRating;// (targetRating - dungeon.Score + ratingPerDung) / seasonInfo.Dungeons.Count;
                    }
                    else
                    {
                        runPool.Add(dungeon);
                    }
                }
                var temp = new List<List<KeyRun>>();
                runPool = runPool.OrderBy(x => x.Score).ToList();
                for (int i = 1; i <= runPool.Count; i++)
                {
                    var targetDungeonScore = (targetRating - (output.Rating - runPool.Take(i).Sum(x => x.Score))) / i;// (x=>x.Score) + ((targetRating - output.Rating) / i);
                    if (targetDungeonScore > maxScore || targetDungeonScore > maxObtainableDunScore) continue;
                    temp.Add(getMinRuns(targetDungeonScore, runPool, i, thisWeeksAffix));
                }
                output.RunOptions = new List<List<KeyRun>>();
                output.RunOptions.AddRange(temp);
                //output.RunOptions.Add(temp.First());
                //output.RunOptions.Add(temp.Last());
            }

            return output;

        }

        private List<KeyRun> getMinRuns(double? targetDungeonScore, List<DungeonWithScores> runPool, int runCount, Affix? thisWeeksAffix)
        {
            if (thisWeeksAffix == null) { throw new Exception("Cant get this weeks affix"); }
            List<KeyRun> output = new List<KeyRun>();
            for (int i = 0; i < runCount; i++)
            {
                var altScore = (thisWeeksAffix.Id == 9 ? runPool[i].FortScore : runPool[i].TyrScore);// Math.Max(runPool[i].FortScore ?? 0, runPool[i].TyrScore ?? 0);
                var bestScore = (targetDungeonScore - (altScore * 0.5)) / 1.5;
                //if (thisWeeksAffix.Id== 9) //tyr
                //{
                //    if (altScore == runPool[i].FortScore )
                //    {
                //        bestScore = (targetDungeonScore - (altScore * 0.5)) / 1.5;
                //    }
                //    else
                //    {
                //        bestScore = (targetDungeonScore - (altScore * 1.5)) / 0.5;
                //    }
                //}
                //else  // fort
                //{
                //    if (altScore == runPool[i].FortScore)
                //    {
                //        bestScore = (targetDungeonScore - (altScore * 1.5)) / 0.5;
                //    }
                //    else
                //    {
                //        bestScore = (targetDungeonScore - (altScore * 0.5)) / 1.5;
                //    }
                //}
                if (bestScore < 245)
                {
                    var dungeonMetric = dungeonMatrix.Where(x => bestScore >= x.Min && (bestScore >= x.Base || bestScore <= x.Base - 5)).FirstOrDefault();
                    if (dungeonMetric != null)
                    {
                        double? time = 0;
                        //Affix affix;

                        //if (runPool[i].FortScore < runPool[i].TyrScore)
                        //{
                        //    affix = new Affix
                        //    {
                        //        Id = 10,
                        //        Name = "Fortified",
                        //        Description = "Non-boss enemies have 20% more health and inflict up to 30% increased damage.",
                        //        IconUrl = "ability_toughness",
                        //        WowheadUrl = "https://wowhead.com/affix=10"
                        //    };
                        //}
                        //else
                        //{
                        //    affix = new Affix
                        //    {
                        //        Id = 9,
                        //        Name = "Tyrannical",
                        //        Description = "Bosses have 30% more health. Bosses and their minions inflict up to 15% increased damage.",
                        //        IconUrl = "achievement_boss_archaedas",
                        //        WowheadUrl = "https://wowhead.com/affix=9\r\n"
                        //    };
                        //}

                        if (bestScore < dungeonMetric.Base)
                        {
                            var timePercent = ((dungeonMetric.Base - 5 - bestScore) / 0.125) / 100;
                            time = runPool[i].TimeLimit + (runPool[i].TimeLimit * timePercent);
                        }
                        else
                        {
                            var timePercent = ((bestScore - dungeonMetric.Base) / 0.125) / 100;
                            time = runPool[i].TimeLimit - (runPool[i].TimeLimit * timePercent);
                        }
                        output.Add(new KeyRun
                        {
                            DungeonName = runPool[i].Name,
                            KeyLevel = dungeonMetric.Level,
                            TimeLimit = runPool[i].TimeLimit,
                            ClearTimeMs = (int)time,
                            Affixes = new List<Affix> { thisWeeksAffix  },
                            OldScore = runPool[i].Score,
                            NewScore = (bestScore * 1.5) + (altScore * 0.5)
                        });
                    }

                }
                else
                {


                }



            }

            return output;
        }


    }
}
