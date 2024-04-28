using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RcLibrary.Helpers;
using RcLibrary.Models.Configurations;
using RcLibrary.Models.RaiderIoModels;

namespace RcLibrary.Servcies.RaiderIoServices;
public class RaiderIoService : IRaiderIoService
{
    private readonly ILogger<RaiderIoService> _logger;
    private readonly IApiHelper _raiderIoApi;
    private readonly Settings _config;

    public RaiderIoService(ILogger<RaiderIoService> logger,
                           IApiHelper raiderIoApi,
                           IOptions<Settings> config)
    {
        _logger = logger;
        _raiderIoApi = raiderIoApi;
        _config = config.Value;

        _raiderIoApi.InitializeClient(_config.RaiderIOAPI ?? "");
    }

    public async Task<RaiderIoCharacter?> GetCharacter(string region, string realm, string name, string season)
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

    public async Task<List<RatingColour>?> GetRatingColours(string seasonName)
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
            _logger.LogError(ex, "Error getting rating colours data from raider.io:{errorMessage}", ex.Message);
            return null;
        }
    }

    public async Task<int> GetDungoenTimeLimit(string region, string seasonName, string dungeonName)
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
            _logger.LogError(ex, "Error getting dungeon time limits data from raider.io:{errorMessage}", ex.Message);
            return 0;
        }
    }


    public async Task<List<Season>?> GetRegionSeasons(string region, int expId)
    {
        var qsParams = new Dictionary<string, string>() { { "expansion_id", expId.ToString() } };
        var endpoint = new Uri(QueryHelpers.AddQueryString("mythic-plus/static-data", qsParams), UriKind.Relative);
        try
        {
            var staticData = await _raiderIoApi.GetAsync<WowStaticData>(endpoint.ToString());

            var seasons = staticData?.Seasons?.Where(x => x != null && x.Starts?[region] != null).Take(1).ToList();            
            return seasons;
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
}
