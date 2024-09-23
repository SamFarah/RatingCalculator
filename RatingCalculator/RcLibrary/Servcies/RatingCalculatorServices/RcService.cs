using AutoMapper;
using Microsoft.Extensions.Logging;
using RcLibrary.Helpers;
using RcLibrary.Models;
using RcLibrary.Models.BlizzardModels;
using RcLibrary.Models.RaiderIoModels;
using RcLibrary.Servcies.BlizzardServices;
using RcLibrary.Servcies.MemoryCacheServices;
using RcLibrary.Servcies.RaiderIoServices;

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
        new () { Level = 2  , Base = 165 },
        new () { Level = 3  , Base = 180 },
        new () { Level = 4  , Base = 205 },
        new () { Level = 5  , Base = 220 },
        new () { Level = 6  , Base = 235 },
        new () { Level = 7  , Base = 265 },
        new () { Level = 8  , Base = 280 },
        new () { Level = 9  , Base = 295 },
        new () { Level = 10 , Base = 320 },
        new () { Level = 11 , Base = 335 },
        new () { Level = 12 , Base = 365 },
    };

    private readonly List<Affix> _affixes = new()
    {
        new ()
        {
            Id = 10,
            Name = "Fortified",
            Description = "Non-boss enemies have 20% more health and inflict up to 20% increased damage.",
            IconUrl = "ability_toughness",
        },
        new()
        {
            Id = 9,
            Name = "Tyrannical",
            Description = "Bosses have 25% more health. Bosses and their minions inflict up to 15% increased damage.",
            IconUrl = "achievement_boss_archaedas",
        },
        new()
        {
            Id = 148,
            Name = "Xal'atath's Bargain: Ascendant",
            Description = "While in combat, Xal'atath rains down orbs of cosmic energy that empower enemies or players.",
            IconUrl = "inv_nullstone_cosmicvoid",
        },
        new()
        {
            Id = 158,
            Name = "Xal'atath's Bargain: Voidbound",
            Description = "While in combat, Xal'atath summons a Void Emissary that empowers nearby enemies. Upon defeating the Void Emissary, players will net themselves a +20% ability cooldown rate and +10% Critical Strike increase that lasts 20 seconds.",
            IconUrl = "inv_cosmicvoid_buff",
        },
        new()
        {
            Id = 159,
            Name = "Xal'atath's Bargain: Oblivion",
            Description = "While in combat, Xal'atath manifests crystals from the void that can be absorbed by enemies or players.",
            IconUrl = "spell_priest_void-blast",
        },
        new()
        {
            Id = 160,
            Name = "Xal'atath's Bargain: Devour",
            Description = "While in combat, Xal'atath tears open rifts that devour the essence of players.",
            IconUrl = "inv_ability_voidweaverpriest_entropicrift",
        },
        new()
        {
            Id = 147,
            Name = "Xal'atath's Guile",
            Description = "Xal'atath betrays players, revoking her bargains and increasing the health and damage of enemies by 20%",
            IconUrl = "ability_racial_chillofnight",
        },
        new()
        {
            Id = 152,
            Name = "Challenger's Peril",
            Description = "Dying subtracts 15 seconds from time remaining.",
            IconUrl = "achievement_challengemode_everbloom_hourglass",
        },

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



    public async Task<ProcessedCharacter?> ProcessCharacter(int expId, string seasonSlug, string region, string realm, string name, double targetRating, List<string>? avoidDungs, int? maxKeyLevel)
    {
        _logger.LogInformation("Processing {characterName}-{region}-{realm} with target rating: {targetRating} for season {season}", name, region, realm, targetRating, seasonSlug);

        var seasonInfo = await GetSeason(region, seasonSlug, expId);
        if (seasonInfo == null) { return null; }
        var seasonName = seasonInfo.Slug ?? "";

        //var thisWeeksAffix = await _memoryCache.GetCachedValue($"WeeksAffix{region}", () => _raiderIo.GetCurrentBaseAffix(region));

        var raiderIoToon = await _raiderIo.GetCharacter(region, realm, name, seasonName);
        var ratingColours = await _memoryCache.GetCachedValue($"RatingColours{region}_{seasonSlug}", () => _raiderIo.GetRatingColours(seasonName), checkNull: true);

        if (raiderIoToon == null) { return null; }
        var allBestPlayerRuns = new List<KeyRun>();
        //int FortAffixID = 10;
        //double? maxObtainableDunScore = 490.0;

        if (raiderIoToon?.BestMythicRuns != null) allBestPlayerRuns.AddRange(raiderIoToon.BestMythicRuns);
        // if (raiderIoToon?.AlternateMythicRuns != null) allBestPlayerRuns.AddRange(raiderIoToon.AlternateMythicRuns);

        ProcessedCharacter output = _mapper.Map<ProcessedCharacter>(raiderIoToon);
        output.TargetRating.Value = targetRating;
        output.TargetRating.Colour = ratingColours?.Where(x => targetRating >= x.Score).FirstOrDefault()?.RgbHex;
        //output.ThisWeekAffixId = thisWeeksAffix?.Id ?? 0;
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
                if (currentDun != null)
                {
                    currentDun.TimeLimit = playerRun.TimeLimit;
                    currentDun.Score = playerRun.Rating;
                    //if (playerRun.Affixes.Where(x => x.Id == FortAffixID).ToList().Count > 0) currentDun.FortScore = playerRun.Rating;
                    //else currentDun.TyrScore = playerRun.Rating;
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
                var anOptionList = await GetMinRuns(targetDungeonScore, runPool, i, maxKeyLevel ?? 30, region);
                if (anOptionList != null)
                {
                    var j = 0;
                    double adjustSum = 0;
                    for (j = 0; j < anOptionList.Count; j++)
                    {
                        adjustSum += (anOptionList[j].NewScore ?? 0) - (anOptionList[j].OldScore ?? 0);
                        if (adjustSum >= targetRating - output.Rating.Value) { break; }

                    }
                    //if (adjustSum >= targetRating - output.Rating.Value) 
                    output.RunOptions.Add(anOptionList.Take(j + 1).ToList());
                }
            }

        }
        output.RunOptions = output?.RunOptions?.Distinct(new KeyRunListComparer()).ToList();
        return output;
    }

    private async Task<List<KeyRun>?> GetMinRuns(double? targetDungeonScore, List<DungeonWithScores> runPool, int runCount, int maxKeyLevel, string region)
    {
        var output = new List<KeyRun>();
        //double? nextDungoenTarget = targetDungeonScore;
        for (int i = 0; i < runCount; i++)
        {
            var bestScore = (targetDungeonScore) ?? 0;

            var dungeonMetric = GetDungeonMetrics(bestScore);
            if (dungeonMetric != null)
            {
                if (dungeonMetric.Level > maxKeyLevel) return null;

                double? time = 0;


                if (bestScore < dungeonMetric.Base) // overtime
                {

                    var timePercent = Math.Min(0.4, (double)((dungeonMetric.Base - bestScore - 15) / 37.5));
                    time = runPool[i].TimeLimit + runPool[i].TimeLimit * timePercent;
                }
                else // beat timer
                {
                    var timePercent = Math.Min(0.4, (double)((bestScore - dungeonMetric.Base) / 37.5));
                    time = runPool[i].TimeLimit - runPool[i].TimeLimit * timePercent;
                }
                output.Add(new KeyRun
                {
                    DungeonName = runPool[i].Name,
                    KeyLevel = dungeonMetric.Level,
                    TimeLimit = runPool[i].TimeLimit,
                    ClearTimeMs = (int)time,
                    Affixes = await GetDungoenAffixes(dungeonMetric.Level, region),
                    OldScore = runPool[i].Score ?? 0,
                    NewScore = (GetDugneonScore(time.Value, runPool[i].TimeLimit, dungeonMetric.Level))
                });
            }
        }

        return output;
    }

    private async Task<(int l2id, int l4id)> GetWWRotatingAffixIds(string region)
    {
        var l2ids = new List<int> { 148, 158, 159, 160 }; // all the Xal'atath's Bargain affixes
        var l4ids = new List<int> { 9, 10 }; // Tyrannical, Fortified

        try
        {
            var affixes = await _raiderIo.GetWeeklyAffixes(region) ?? throw new Exception("raider.io returned null for affixes");
            var l2Id = affixes.FirstOrDefault(x => l2ids.Contains(x.Id))?.Id ?? throw new Exception("raider.io did not return l2 affix");
            var l4id = affixes.FirstOrDefault(x => l4ids.Contains(x.Id))?.Id ?? throw new Exception("raider.io did not return l4 affix");// asuming the first one between tyr and fort that appear in the list is the l4 one
            return (l2Id, l4id);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting l2/l4 affix Id's from raider.io: {errorMessage}, reverting to manual mode", ex.Message);

            //------------------------------ Workaround Start ------------------------------
            // if raider.io failed getting affiex, get them with this if it looks stupid but it works, its not stupid ok!
            // its not accurate but it gets something

            var seasonStartDate = new DateTime(2024, 09, 17);
            var today = DateTime.Now.Date;
            var numberOfWeeks = (int)((today - seasonStartDate).TotalDays / 7.0);
            var l2AffixRotation = new List<int> { 148, 158, 159, 160 }; // assuming the smae order in https://www.wowhead.com/guide/mythic-plus-dungeons/the-war-within-season-1/overview

            var weeklyL2Id = l2AffixRotation[numberOfWeeks % 4];
            var weeklyL4Id = numberOfWeeks % 2 == 0 ? 9 : 10; // alternate 1 week tyr, one week fort

            //------------------------------ Workaround End ------------------------------
            return (weeklyL2Id, weeklyL4Id);
        }
    }

    private async Task<List<Affix>> GetDungoenAffixes(int keyLevel, string region)
    {
        var output = new List<Affix>();

        // get weekly affix from raider.io 
        var (weeklyL2Id, weeklyL4Id) = await _memoryCache.GetCachedValue($"l2l4affixes_{region}", () => GetWWRotatingAffixIds(region));

        if (keyLevel < 12) output.Add(_affixes.First(x => x.Id == weeklyL2Id)); //Xal'atath's whatever this week is, if key level is less than 12
        if (keyLevel >= 4) output.Add(_affixes.First(x => x.Id == weeklyL4Id));
        if (keyLevel >= 7) output.Add(_affixes.First(x => x.Id == 152));// Challenger's Peril
        if (keyLevel >= 10) output.Add(_affixes.First(x => x.Id == (weeklyL4Id == 9 ? 10 : 9))); //  if the weekly is try then next one is fort, and vice versa            
        if (keyLevel >= 12) output.Add(_affixes.First(x => x.Id == 147)); // Xal'atath's Guile 

        return output;
    }

    private DungeonMetrics? GetDungeonMetrics(double bestScore)
    {
        DungeonMetrics? dungeonMetric;
        if (bestScore > 380)
        {
            var theoreticalLevel = (int)((bestScore - 185) / 15.0); // to get a rating that requires a key higher than level 10, this will calculates the theoretical key
                                                                    // 145 + (key level * 15) + (number of affixes * 10)                                                                                                                                        
            dungeonMetric = new DungeonMetrics
            {
                Level = theoreticalLevel,
                Base = 145 + (theoreticalLevel * 15) + 40
            };
        }
        else dungeonMetric = _dungeonMatrix.Where(x => bestScore <= x.Max && bestScore >= x.Min).FirstOrDefault();
        return dungeonMetric;
    }

    public double GetDugneonScore(double time, double timeLimit, int level)
    {
        var metric = _dungeonMatrix.FirstOrDefault(x => x.Level == level);
        if (level > 12 && metric == null) metric = new DungeonMetrics() { Base = (level >= 12 ? 145 : level >= 7 ? 130 : 125) + (level * 15) + (10 * (level >= 10 ? 4 : level >= 7 ? 3 : level >= 4 ? 2 : 1)) };
        if (metric == null) return 0;

        var pt = Math.Abs((timeLimit - time) / timeLimit);
        var corrededPt = Math.Min(pt, 0.4) * (time > timeLimit ? -1 : 1);
        var rating = metric.Base + (corrededPt * 37.5) - (time > timeLimit ? 15 : 0);
        return rating;
    }

    public async Task<Season?> GetSeason(string region, string slug, int expId)
    {
        return (await GetRegionSeasonsAsync(region, expId))?.FirstOrDefault(x => x.Slug == slug);
    }

    public List<DungeonMetrics> GetDungeonMetrics()
    {
        var output = _dungeonMatrix;
        for (var i = 13; i <= 20; i++) output.Add(new DungeonMetrics
        {
            Level = i,
            Base = 145 + (i * 15) + 40
        });

        return output;
    }


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
