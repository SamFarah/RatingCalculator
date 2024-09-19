using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RcLibrary.Helpers;
using RcLibrary.Models;
using RcLibrary.Models.BlizzardModels;
using RcLibrary.Models.Configurations;
using RcLibrary.Servcies.MemoryCacheServices;
using System.Text;

namespace RcLibrary.Servcies.BlizzardServices;
public class BlizzardService : IBlizzardService
{
    private readonly ILogger<BlizzardService> _logger;
    private readonly IApiHelper _blizzApi;
    private readonly IMemoryCacheService _memoryCache;
    private readonly Settings _config;

    public BlizzardService(ILogger<BlizzardService> logger,
                           IApiHelper blizzApi,
                           IOptions<Settings> config,
                           IMemoryCacheService memoryCache)
    {
        _logger = logger;
        _blizzApi = blizzApi;
        _memoryCache = memoryCache;
        _config = config.Value;

    }

    private async Task<AccessToken?> GetToken()
    {
        _blizzApi.Dispose();
        _blizzApi.InitializeClient(_config.BlizzardApi.OAuthUrl);

        var nvc = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        };
        var content = new FormUrlEncodedContent(nvc);
        var authenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_config.BlizzardApi.ClientId}:{_config.BlizzardApi.ClientSecret}"));
        _blizzApi.AddBasicAuthHeader(authenticationString);
        try
        {
            var ouput = await _blizzApi.PostAsync<AccessToken>("token", content);
            return ouput ?? throw new Exception("Something went wrong");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get access token from blizzard");
        }

        return null;
    }


    public async Task<List<Realm>?> GetRegionRealms(string region)
    {
        region = region.ToLower();

        var token = await _memoryCache.GetCachedValue("BlizzardToken", GetToken, 86399);
        if (token?.Token != null)
        {
            _blizzApi.Dispose();
            _blizzApi.InitializeClient(_config.BlizzardApi.Url.Replace("{region}", region));
            _blizzApi.AddBearerAuthHeader(token.Token);
            var qsParams = new Dictionary<string, string>() {
                { "namespace", $"dynamic-{region}" } ,
                { "locale","en_US"}
            };
            var endpoint = new Uri(QueryHelpers.AddQueryString("data/wow/realm/index", qsParams), UriKind.Relative);
            try
            {
                var realIndexResp = await _blizzApi.GetAsync<RealmIndexResponse>(endpoint.ToString());
                return realIndexResp.Realms?.OrderBy(o => o.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting realm data from blizzard}");
                throw;
            }
        }
        return null;
    }

    public async Task<List<Expansion>?> GetExpansionsAsync(string region)
    {
        region = region.ToLower();

        var token = await _memoryCache.GetCachedValue("BlizzardToken", GetToken, 86399);
        if (token?.Token != null)
        {
            _blizzApi.Dispose();
            _blizzApi.InitializeClient(_config.BlizzardApi.Url.Replace("{region}", region));
            _blizzApi.AddBearerAuthHeader(token.Token);
            var qsParams = new Dictionary<string, string>() {
                { "namespace", $"static-{region}" } ,
                { "locale","en_US"}
            };
            var endpoint = new Uri(QueryHelpers.AddQueryString("data/wow/journal-expansion/index", qsParams), UriKind.Relative);
            try
            {
                var expJIndexResp = await _blizzApi.GetAsync<JournalExIndexResponse>(endpoint.ToString());
                if (expJIndexResp?.Tiers?.Any() ?? false)
                {
                    var output = new List<Expansion>();
                    var raiderId = 0;
                    foreach (var tier in expJIndexResp.Tiers.Where(x => x.Name != "Current Season").OrderBy(x => x.Id))
                    {
                        output.Add(new Expansion
                        {
                            Id = raiderId++,
                            Name = tier?.Name ?? string.Empty
                        });
                    }
                    return output.Where(x => x.Id >= _config.OldestRaiderIOExpId).OrderByDescending(x => x.Id).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expansion data from blizzard}");
                throw;
            }
        }
        return null;
    }





}
