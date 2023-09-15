
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RcLibrary.Helpers;
using RcLibrary.Models.Configurations;

namespace RcLibrary.Servcies.BlizzardServices;
public class BlizzardService
{
    private readonly ILogger<BlizzardService> _logger;
    private readonly IApiHelper _blizzApi;
    private readonly Settings _config;

    public BlizzardService(ILogger<BlizzardService> logger,
                           IApiHelper blizzApi,
                           IOptions<Settings> config)
    {
        _logger = logger;
        _blizzApi = blizzApi;
        _config = config.Value;

        _blizzApi.InitializeClient(_config.RaiderIOAPI ?? "");        
    }


}
