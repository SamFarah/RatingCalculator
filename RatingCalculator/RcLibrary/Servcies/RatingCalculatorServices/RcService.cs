﻿using AutoMapper;
using Microsoft.Extensions.Logging;
using RcLibrary.Helpers;
using RcLibrary.Models;
using RcLibrary.Models.BlizzardModels;
using RcLibrary.Models.RaiderIoModels;
using RcLibrary.Servcies.BlizzardServices;
using RcLibrary.Servcies.MemoryCacheServices;
using RcLibrary.Servcies.RaiderIoServices;
using static RcLibrary.Models.Enums;

namespace RcLibrary.Servcies.RatingCalculatorServices;

public class RcService : IRcService
{
    private readonly IMemoryCacheService _memoryCache;
    private readonly IRaiderIoService _raiderIo;
    private readonly IBlizzardService _blizzard;
    private readonly IMapper _mapper;
    private readonly ILogger<RcService> _logger;
    private readonly List<DungeonMetrics> _dungeonMatrix = new()
    {
        new DungeonMetrics { Level = 2  , Base = 94 },
        new DungeonMetrics { Level = 3  , Base = 101 },
        new DungeonMetrics { Level = 4  , Base = 108 },
        new DungeonMetrics { Level = 5  , Base = 125 },
        new DungeonMetrics { Level = 6  , Base = 132 },
        new DungeonMetrics { Level = 7  , Base = 139 },
        new DungeonMetrics { Level = 8  , Base = 146 },
        new DungeonMetrics { Level = 9  , Base = 153 },
        new DungeonMetrics { Level = 10 , Base = 170},/*
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
        new DungeonMetrics { Level = 30 , Base = 240},*/
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


    public RcService(IMemoryCacheService memoryCache,
                     IRaiderIoService raiderIo,
                     IBlizzardService blizzard,
                     IMapper mapper,
                     ILogger<RcService> logger)
    {
        _memoryCache = memoryCache;
        _raiderIo = raiderIo;
        _blizzard = blizzard;
        _mapper = mapper;
        _logger = logger;
    }



    public async Task<ProcessedCharacter?> ProcessCharacter(int expId, string seasonSlug, string region, string realm, string name, double targetRating, bool thisweekOnly, List<string>? avoidDungs, int? maxKeyLevel)
    {
        _logger.LogInformation("Processing {characterName}-{region}-{realm} with target rating: {targetRating} for season {season}", name, region, realm, targetRating, seasonSlug);

        var seasonInfo = await GetSeason(region, seasonSlug, expId);
        if (seasonInfo == null) { return null; }
        var seasonName = seasonInfo.Slug ?? "";

        var thisWeeksAffix = await _memoryCache.GetCachedValue($"WeeksAffix{region}", () => _raiderIo.GetCurrentBaseAffix(region));
        var raiderIoToon = await _raiderIo.GetCharacter(region, realm, name, seasonName);
        var ratingColours = await _memoryCache.GetCachedValue($"RatingColours{region}_{seasonSlug}", () => _raiderIo.GetRatingColours(seasonName), checkNull: true);

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
            //double? maxScore = ratingPerDung;
            SeasonDungoens = SeasonDungoens.OrderByDescending(x => x.Score).ToList();
            var runPool = new List<DungeonWithScores>();

            foreach (var dungeon in SeasonDungoens)
            {
                if (dungeon.Score > ratingPerDung)
                {
                    //if (dungeon.Score > maxScore) { maxScore = dungeon.Score; }
                    var extraRating = (dungeon.Score - ratingPerDung) / seasonInfo.Dungeons.Count;
                    ratingPerDung -= extraRating;
                }
                else
                {
                    if (dungeon.TimeLimit == 0)
                    {
                        dungeon.TimeLimit = await _memoryCache.GetCachedValue($"{dungeon.Slug}TimeLimit{region}", () => _raiderIo.GetDungoenTimeLimit(region, seasonName, dungeon.Slug ?? ""));
                    }
                    if (avoidDungs == null || !avoidDungs.Contains(dungeon.Slug ?? "")) runPool.Add(dungeon);
                    //if ((ratingPerDung* runPool.Count() ) > (targetRating - output.Rating.Value )) break; 
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
                        if (adjustSum > targetRating - output.Rating.Value) { break; }

                    }
                    output.RunOptions.Add(anOptionList.Take(j + 1).ToList());
                }
            }

        }
        output.RunOptions = output?.RunOptions?.Distinct(new KeyRunListComparer()).ToList();
        return output;
    }

    private DungeonMetrics? GetDungeonMetrics(double bestScore)
    {
        DungeonMetrics? dungeonMetric;
        if (bestScore > 175)
        {
            var theoreticalLevel = (int)((bestScore - 100) / 7.0); // to get a rating that requires a key higher than level 10, this will calculates the theoretical key
                                                                   // 70 + (key level * 7) + (number of affixes * 10)
                                                                   // i.e 170 base for 10, so 177 base for 11, and 184 base for 12, etc...
                                                                   // https://www.wowhead.com/guide/blizzard-mythic-plus-rating-score-in-game By JElmore                                                                                                                                       
            dungeonMetric = new DungeonMetrics
            {
                Level = theoreticalLevel,
                Base = 70 + (theoreticalLevel * 7) + 30
            };
        }
        else dungeonMetric = _dungeonMatrix.Where(x => bestScore <= x.Max && bestScore >= x.Min).FirstOrDefault();
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
                var bestScore = (nextDungoenTarget - altScore * 0.5) / 1.5 ?? 0;

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
                            var x = bestScore * 1.5 + altScore * 0.5 - targetDungeonScore;
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
                        time = runPool[i].TimeLimit + runPool[i].TimeLimit * timePercent;
                    }
                    else // beat timer
                    {
                        var timePercent = Math.Min(0.4, (double)((bestScore - dungeonMetric.Base) / 12.5));
                        time = runPool[i].TimeLimit - runPool[i].TimeLimit * timePercent;
                    }
                    output.Add(new KeyRun
                    {
                        DungeonName = runPool[i].Name,
                        KeyLevel = dungeonMetric.Level,
                        TimeLimit = runPool[i].TimeLimit,
                        ClearTimeMs = (int)time,
                        Affixes = new List<Affix> { thisWeeksAffix },
                        OldScore = runPool[i].Score,
                        NewScore = (GetDugneonScore(time.Value, runPool[i].TimeLimit, dungeonMetric.Level)) * 1.5 + altScore * 0.5
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
                            var x = bestScore * 2 - targetDungeonScore;
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
                        time = runPool[i].TimeLimit + runPool[i].TimeLimit * timePercent;
                    }
                    else // beat timer
                    {
                        var timePercent = Math.Min(0.4, (double)((bestScore - dungeonMetric.Base) / 12.5));
                        time = runPool[i].TimeLimit - runPool[i].TimeLimit * timePercent;
                    }

                    bestScore = GetDugneonScore(time.Value, runPool[i].TimeLimit, dungeonMetric.Level);
                    var didThisWeek = false;
                    double newScore = 0;
                    if ((thisWeeksAffix.Id == 9 ? runPool[i].TyrScore ?? 0 : runPool[i].FortScore ?? 0) < bestScore)
                    {
                        didThisWeek = true;
                        var forScore = thisWeeksAffix.Id == 9 ? runPool[i].FortScore ?? 0 : bestScore;
                        var tyrScore = thisWeeksAffix.Id == 10 ? runPool[i].TyrScore ?? 0 : bestScore;
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

                    if ((thisWeeksAffix.Id == 10 ? runPool[i].TyrScore ?? 0 : runPool[i].FortScore ?? 0) < bestScore)
                    {
                        double? oldScore = runPool[i].Score;
                        if (didThisWeek)
                        {
                            oldScore = newScore;
                            newScore = 2 * bestScore;
                        }
                        else
                        {

                            var forScore = thisWeeksAffix.Id == 10 ? runPool[i].FortScore ?? 0 : bestScore;
                            var tyrScore = thisWeeksAffix.Id == 9 ? runPool[i].TyrScore ?? 0 : bestScore;

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

    public double GetDugneonScore(double time, double timeLimit, int level)
    {
        if (level > 20 && time > timeLimit) level = 20;

        var metric = _dungeonMatrix.FirstOrDefault(x => x.Level == level);
        if (level > 10 && metric == null) metric = new DungeonMetrics() { Base = 70 + (level * 7) + (10 * (level >= 10 ? 3 : level >= 5 ? 2 : 1)) };
        if (metric == null) return 0;

        var pt = Math.Abs((timeLimit - time) / timeLimit);
        var corrededPt = Math.Min(pt, 0.4) * (time > timeLimit ? -1 : 1);
        var rating = metric.Base + (corrededPt * 12.5) - (time > timeLimit ? 5 : 0);
        return rating;
    }

    public async Task<Season?> GetSeason(string region, string slug, int expId)
    {
        return (await GetRegionSeasonsAsync(region, expId))?.FirstOrDefault(x => x.Slug == slug);
    }

    public List<DungeonMetrics> GetDungeonMetrics() => _dungeonMatrix;


    public async Task<List<Realm>?> GetRegionRealmsAsync(string region)
    {
        var output = await _memoryCache.GetCachedValue($"Realms{region}", () => _blizzard.GetRegionRealms(region), 86400); // cache it once a day
        return output;
    }

    public async Task<List<Season>?> GetRegionSeasonsAsync(string region, int expId)
    {
        var output = await _memoryCache.GetCachedValue($"Seasons{region}_{expId}", () => _raiderIo.GetRegionSeasons(region, expId));
        return output;
    }

    public async Task<Season?> GetWowCurrentSeason(string region, int expId)
    {
        var seasons = await GetRegionSeasonsAsync(region, expId);
        var currentDate = DateTime.UtcNow;
        return seasons?.FirstOrDefault(x => x.Name != null
                                            && !x.Name.Contains('•') // no better way to select none PTR and other patch seasons
                                            && currentDate >= x.Starts?[region]
                                            && (x.Ends?[region] == null || currentDate < x.Ends?[region]));
    }

    public async Task<List<Expansion>?> GetWowExpansionsAsync(string region)
    {
        var output = await _memoryCache.GetCachedValue($"Expansions{region}", () => _blizzard.GetExpansionsAsync(region));
        return output;
    }
}
