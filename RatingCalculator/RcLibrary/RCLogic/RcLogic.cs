using AutoMapper;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RcLibrary.Helpers;
using RcLibrary.Models;
using RcLibrary.Models.Configurations;

namespace RcLibrary.RCLogic;

public class RcLogic : IRcLogic
{
    private readonly ILogger<RcLogic> _logger;
    private readonly IApiHelper _raiderIoApi;
    private readonly IMapper _mapper;
    private readonly Settings _config;
    private readonly IMemoryCache _memoryCache;

    private readonly List<DungeonMetrics> _dungeonMatrix = new()
    {
        new DungeonMetrics { Level = 2  , Base = 40 },
        new DungeonMetrics { Level = 3  , Base = 45 },
        new DungeonMetrics { Level = 4  , Base = 55 },
        new DungeonMetrics { Level = 5  , Base = 60 },
        new DungeonMetrics { Level = 6  , Base = 65 },
        new DungeonMetrics { Level = 7  , Base = 75 },
        new DungeonMetrics { Level = 8  , Base = 80 },
        new DungeonMetrics { Level = 9  , Base = 85 },
        new DungeonMetrics { Level = 10 , Base = 100},
        new DungeonMetrics { Level = 11 , Base = 107},
        new DungeonMetrics { Level = 12 , Base = 114},
        new DungeonMetrics { Level = 13 , Base = 121},
        new DungeonMetrics { Level = 14 , Base = 128},
        new DungeonMetrics { Level = 15 , Base = 135},
        new DungeonMetrics { Level = 16 , Base = 142},
        new DungeonMetrics { Level = 17 , Base = 149},
        new DungeonMetrics { Level = 18 , Base = 156},
        new DungeonMetrics { Level = 19 , Base = 163},
        new DungeonMetrics { Level = 20 , Base = 170},
        new DungeonMetrics { Level = 21 , Base = 177},
        new DungeonMetrics { Level = 22 , Base = 184},
        new DungeonMetrics { Level = 23 , Base = 191},
        new DungeonMetrics { Level = 24 , Base = 198},
        new DungeonMetrics { Level = 25 , Base = 205},
        new DungeonMetrics { Level = 26 , Base = 212},
        new DungeonMetrics { Level = 27 , Base = 219},
        new DungeonMetrics { Level = 28 , Base = 226},
        new DungeonMetrics { Level = 29 , Base = 233},
        new DungeonMetrics { Level = 30 , Base = 240},
    };

    private readonly Affix _fortAffix = new()
    {
        Id = 10,
        Name = "Fortified",
        Description = "Non-boss enemies have 20% more health and inflict up to 30% increased damage.",
        IconUrl = "ability_toughness",
        WowheadUrl = "https://wowhead.com/affix=10"
    };

    private readonly Affix _tyrAffix = new()
    {
        Id = 9,
        Name = "Tyrannical",
        Description = "Bosses have 30% more health. Bosses and their minions inflict up to 15% increased damage.",
        IconUrl = "achievement_boss_archaedas",
        WowheadUrl = "https://wowhead.com/affix=9"
    };


    public RcLogic(ILogger<RcLogic> logger,
                   IApiHelper raiderIoApi,
                   IOptions<Settings> config,
                   IMapper mapper,
                   IMemoryCache memoryCache)
    {
        _logger = logger;
        _config = config.Value;
        _raiderIoApi = raiderIoApi;
        _mapper = mapper;
        _memoryCache = memoryCache;
        _raiderIoApi.InitializeClient(_config.RaiderIOAPI ?? "");

    }

    private async Task<T?> GetCachedValue<T>(string cacheKey, string region, Func<Task<T>> getter)
    {
        cacheKey = $"{cacheKey}{region}";
        if (!_memoryCache.TryGetValue(cacheKey, out T? cachedValue))
        {
            cachedValue = await getter();
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1));
            _memoryCache.Set(cacheKey, cachedValue, cacheEntryOptions);
        }
        return cachedValue;
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

    private async Task<List<RatingColour>?> GetRatingColours(string seasonName)
    {
        var qsParams = new Dictionary<string, string>() { { "season", seasonName } };
        var endpoint = new Uri(QueryHelpers.AddQueryString("mythic-plus/score-tiers", qsParams), UriKind.Relative);
        try
        {
            var ratingColours = await _raiderIoApi.GetAsync<List<RatingColour>>(endpoint.ToString());
            return ratingColours;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting static data from raider.io:{errorMessage}", ex.Message);
            return null;
        }
    }

    private async Task<int> GetDungoenTimeLimit(string region, string seasonName, string dungeonName)
    {
        var qsParams = new Dictionary<string, string>() { { "region", region }, { "season", seasonName }, { "dungeon", dungeonName }, };
        var endpoint = new Uri(QueryHelpers.AddQueryString("mythic-plus/runs", qsParams), UriKind.Relative);
        try
        {
            var runDetails = await _raiderIoApi.GetAsync<RunsResponse>(endpoint.ToString());
            var timeLimit = (runDetails?.Rankings?.First()?.run?.Dungeon?.TimeLimitMS) ?? 0;
            return timeLimit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting static data from raider.io:{errorMessage}", ex.Message);
            return 0;
        }
    }

    private async Task<Season?> GetWowCurrentSeason(string region)
    {
        var qsParams = new Dictionary<string, string>() { { "expansion_id", _config.ExpansionId.ToString() } };
        var endpoint = new Uri(QueryHelpers.AddQueryString("mythic-plus/static-data", qsParams), UriKind.Relative);
        try
        {
            var staticData = await _raiderIoApi.GetAsync<WowStaticData>(endpoint.ToString());
            var currentDate = DateTime.UtcNow;
            var season = staticData?.Seasons?.Where(x => x != null && currentDate >= x.Starts?[region] && (x.Ends?[region] == null || currentDate < x.Ends?[region])).FirstOrDefault();
            return season;
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

    public async Task<ProcessedCharacter?> ProcessCharacter(string region, string realm, string name, double targetRating, bool thisweekOnly, string? avoidDung, int? maxKeyLevel)
    {
        var seasonInfo = await GetCachedValue("SeasonInfo", region, () => GetWowCurrentSeason(region));
        if (seasonInfo == null) { return null; }
        var seasonName = seasonInfo.Slug ?? "";

        var thisWeeksAffix = await GetCachedValue("WeeksAffix", region, () => GetCurrentBaseAffix(region));
        var raiderIoToon = await GetCharacter(region, realm, name, seasonName);
        var ratingColours = await GetCachedValue("RatingColours", region, () => GetRatingColours(seasonName));

        if (raiderIoToon == null) { return null; }
        var allBestPlayerRuns = new List<KeyRun>();
        int FortAffixID = 10;
        //double? maxObtainableDunScore = 490.0;

        if (raiderIoToon?.BestMythicRuns != null) allBestPlayerRuns.AddRange(raiderIoToon.BestMythicRuns);
        if (raiderIoToon?.AlternateMythicRuns != null) allBestPlayerRuns.AddRange(raiderIoToon.AlternateMythicRuns);

        ProcessedCharacter output = _mapper.Map<ProcessedCharacter>(raiderIoToon);
        output.TargetRating.Value = targetRating;
        output.TargetRating.Colour = ratingColours?.Where(x => targetRating >= x.Score).FirstOrDefault()?.RgbHex;
        output.ThisWeekAffixId = thisWeeksAffix?.Id ?? 0;
        var selectedSeason = raiderIoToon?.MPlusSeasonScores?.Where(x => x.Season == seasonName).FirstOrDefault();
        if (selectedSeason?.Scores != null)
        {
            output.Rating.Value = selectedSeason.Scores["all"];
            output.Rating.Colour = ratingColours?.Where(x => output.Rating.Value >= x.Score).FirstOrDefault()?.RgbHex;
        }

        if (output.Rating.Value >= targetRating) { return output; }

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
            var runPool = new List<DungeonWithScores>();

            foreach (var dungeon in SeasonDungoens)
            {
                if (dungeon.Score > ratingPerDung)
                {
                    if (dungeon.Score > maxScore) { maxScore = dungeon.Score; }
                    var extraRating = (dungeon.Score - ratingPerDung) / seasonInfo.Dungeons.Count;
                    ratingPerDung -= extraRating;
                }
                else
                {
                    if (dungeon.TimeLimit == 0)
                    {
                        dungeon.TimeLimit = await GetCachedValue($"{dungeon.Slug}TimeLimit", region, () => GetDungoenTimeLimit(region, seasonName, dungeon.Slug ?? ""));
                    }
                    if (dungeon.Slug != avoidDung) runPool.Add(dungeon);
                }
            }

            output.RunOptions = new List<List<KeyRun>>();
            runPool = runPool.OrderBy(x => x.Score).ToList();
            for (int i = 1; i <= runPool.Count; i++)
            {

                var targetDungeonScore = (targetRating - (output.Rating.Value - runPool.Take(i).Sum(x => x.Score))) / i;
                //if (targetDungeonScore > maxObtainableDunScore) continue;
                var anOptionList = GetMinRuns(targetDungeonScore, runPool, i, thisWeeksAffix, thisweekOnly, maxKeyLevel ?? 30);
                if (anOptionList != null)
                {
                    var j = 0;
                    double adjustSum = 0;
                    for (j = 0; j < anOptionList.Count; j++)
                    {
                        adjustSum += (anOptionList[j].NewScore ?? 0) - (anOptionList[j].OldScore ?? 0);
                        if (adjustSum > (targetRating - output.Rating.Value)) { break; }

                    }
                    output.RunOptions.Add(anOptionList.Take(j + 1).ToList());
                }
            }
        }

        return output;
    }

    private DungeonMetrics? GetDungeonMetrics(double bestScore)
    {
        DungeonMetrics? dungeonMetric;
        if (bestScore > 245)
        {
            var theoreticalLevel = (int)((bestScore - 239) / 7.0) + 30; // to get a rating that requires a key higher than level 30, this will calculates the theoretical key
                                                                        // level assuming that every level over 30 adds 7 base points
                                                                        // i.e 240 base for 30, so 247 base for 31, and 254 base for 32, etc...
                                                                        // not sure about this assumption though, since as of today (the 14th of september 2023)
                                                                        // the highest key achieved is a 32,
                                                                        // so i do not have enough data to test this assumption.
                                                                        // on the other hand, can be a safe assumption since every key adds 7 base points to the one 
                                                                        // before it starting from key level 11
            dungeonMetric = new DungeonMetrics
            {
                Level = theoreticalLevel,
                Base = 240.0 + ((theoreticalLevel - 30) * 7.0)
            };
        }
        else dungeonMetric = _dungeonMatrix.Where(x => bestScore <= x.Max && (bestScore >= x.Base || bestScore <= x.Base - 5)).FirstOrDefault();
        return dungeonMetric;
    }

    private List<KeyRun>? GetMinRuns(double? targetDungeonScore, List<DungeonWithScores> runPool, int runCount, Affix? thisWeeksAffix, bool thisweekOnly, int maxKeyLevel)
    {
        if (thisWeeksAffix == null) { throw new Exception("Cant get this weeks affix"); }
        var output = new List<KeyRun>();
        double? nextDungoenTarget = targetDungeonScore;
        for (int i = 0; i < runCount; i++)
        {
            if (thisweekOnly)
            {
                var altScore = (thisWeeksAffix.Id == 9 ? runPool[i].FortScore : runPool[i].TyrScore) ?? 0;
                var bestScore = ((nextDungoenTarget - (altScore * 0.5)) / 1.5) ?? 0;

                var dungeonMetric = GetDungeonMetrics(bestScore);
                if (dungeonMetric != null)
                {
                    if (dungeonMetric.Level > maxKeyLevel) return null;


                    if (dungeonMetric.Level > 20 && bestScore < dungeonMetric.Base)
                    {

                        // if the chosen metric was to not time a 21+ then find the first level that at max gives us the score we need
                        dungeonMetric = _dungeonMatrix.Where(x => bestScore <= x.Max).FirstOrDefault();
                        if (dungeonMetric == null) return null;

                        // if the score we need is still smaller than the max, then its ok to get a bit more rating than we needed
                        if (bestScore < dungeonMetric.Base)
                        {
                            bestScore = dungeonMetric.Base;

                            // since we went a bit higher on these dungeons, we need to adjust the next target Dugneon score to counter it                                
                            var x = (bestScore * 1.5) + (altScore * 0.5) - targetDungeonScore;
                            nextDungoenTarget -= x;
                        }
                    }
                    else
                    {
                        // if no adjustments made this round, then go back to original target
                        nextDungoenTarget = targetDungeonScore;
                    }

                    double? time = 0;


                    if (bestScore < dungeonMetric.Base) // overtime
                    {

                        var timePercent = Math.Min(0.4, (double)((dungeonMetric.Base - bestScore - 5) / 12.5));
                        time = runPool[i].TimeLimit + (runPool[i].TimeLimit * timePercent);
                    }
                    else // beat timer
                    {
                        var timePercent = Math.Min(0.4, (double)((bestScore - dungeonMetric.Base) / 12.5));
                        time = runPool[i].TimeLimit - (runPool[i].TimeLimit * timePercent);
                    }
                    output.Add(new KeyRun
                    {
                        DungeonName = runPool[i].Name,
                        KeyLevel = dungeonMetric.Level,
                        TimeLimit = runPool[i].TimeLimit,
                        ClearTimeMs = (int)time,
                        Affixes = new List<Affix> { thisWeeksAffix },
                        OldScore = runPool[i].Score,
                        NewScore = (bestScore * 1.5) + (altScore * 0.5)
                    });
                }
            }
            else
            {
                var bestScore = (nextDungoenTarget ?? 0) / 2;

                var dungeonMetric = GetDungeonMetrics(bestScore);
                if (dungeonMetric != null)
                {
                    if (dungeonMetric.Level > maxKeyLevel) return null;

                    if (dungeonMetric.Level > 20 && bestScore < dungeonMetric.Base)
                    {
                        dungeonMetric = _dungeonMatrix.Where(x => bestScore <= x.Max).FirstOrDefault();
                        if (dungeonMetric == null) return null;

                        // if the score we need is still smaller than the max, then its ok to get a bit more rating than we needed
                        if (bestScore < dungeonMetric.Base)
                        {
                            bestScore = dungeonMetric.Base;

                            // since we went a bit higher on these dungeons, we need to adjust the next target Dugneon score to counter it                           
                            var x = (bestScore * 2) - targetDungeonScore;
                            nextDungoenTarget -= x;
                        }
                    }
                    else
                    {
                        // if no adjustments made this round, then go back to original target
                        nextDungoenTarget = targetDungeonScore;
                    }

                    double? time = 0;

                    if (bestScore < dungeonMetric.Base) // overtime
                    {
                        var timePercent = Math.Min(0.4, (double)((dungeonMetric.Base - bestScore - 5) / 12.5));
                        time = runPool[i].TimeLimit + (runPool[i].TimeLimit * timePercent);
                    }
                    else // beat timer
                    {
                        var timePercent = Math.Min(0.4, (double)((bestScore - dungeonMetric.Base) / 12.5));
                        time = runPool[i].TimeLimit - (runPool[i].TimeLimit * timePercent);
                    }

                    var didThisWeek = false;
                    double newScore = 0;
                    if ((thisWeeksAffix.Id == 9 ? (runPool[i].TyrScore ?? 0) : (runPool[i].FortScore ?? 0)) < bestScore)
                    {
                        didThisWeek = true;
                        var forScore = thisWeeksAffix.Id == 9 ? (runPool[i].FortScore ?? 0) : bestScore;
                        var tyrScore = thisWeeksAffix.Id == 10 ? (runPool[i].TyrScore ?? 0) : bestScore;
                        newScore = Math.Max(forScore, tyrScore) * 1.5 + Math.Min(forScore, tyrScore) * 0.5;
                        output.Add(new KeyRun
                        {
                            DungeonName = runPool[i].Name,
                            KeyLevel = dungeonMetric.Level,
                            TimeLimit = runPool[i].TimeLimit,
                            ClearTimeMs = (int)time,
                            Affixes = new List<Affix> { thisWeeksAffix },
                            OldScore = runPool[i].Score,
                            NewScore = newScore
                        });
                    }

                    if ((thisWeeksAffix.Id == 10 ? (runPool[i].TyrScore ?? 0) : (runPool[i].FortScore ?? 0)) < bestScore)
                    {
                        double? oldScore = runPool[i].Score;
                        if (didThisWeek)
                        {
                            oldScore = newScore;
                            newScore = 2 * bestScore;
                        }
                        else
                        {

                            var forScore = thisWeeksAffix.Id == 10 ? (runPool[i].FortScore ?? 0) : bestScore;
                            var tyrScore = thisWeeksAffix.Id == 9 ? (runPool[i].TyrScore ?? 0) : bestScore;

                            newScore = Math.Max(forScore, tyrScore) * 1.5 + Math.Min(forScore, tyrScore) * 0.5;
                        }
                        output.Add(new KeyRun
                        {
                            DungeonName = runPool[i].Name,
                            KeyLevel = dungeonMetric.Level,
                            TimeLimit = runPool[i].TimeLimit,
                            ClearTimeMs = (int)time,
                            Affixes = new List<Affix> { thisWeeksAffix.Id == 9 ? _fortAffix : _tyrAffix },
                            OldScore = oldScore,
                            NewScore = newScore
                        });
                    }
                }
            }
        }

        return output;
    }

    public async Task<Season?> GetSeason()
    {
        var seasonInfo = await GetCachedValue("SeasonInfo", "us", () => GetWowCurrentSeason("us"));
        return seasonInfo;
    }

    public List<DungeonMetrics> GetDungeonMetrics() => _dungeonMatrix;

}
