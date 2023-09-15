
using MythicPlanner.Models;
using RcLibrary.Helpers;

using RcLibrary.Models.Configurations;
using RcLibrary.Servcies.BlizzardServices;
using RcLibrary.Servcies.RaiderIoServices;
using RcLibrary.Servcies.RatingCalculatorServices;
namespace MythicPlanner.Startup;

public static class WebAppBuilderExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.Configure<Settings>(builder.Configuration.GetSection(Settings.MainSectionName));
        services.AddOptions();

        services.AddMvc();

        services.AddSingleton(AutoMapper.CreateConfiguredMapper());
        services.AddSingleton<IApiHelper, ApiHelper>();
        services.AddSingleton<IRcService, RcService>();
        services.AddTransient<IAppVersionService, AppVersionService>();
        services.AddTransient<IRaiderIoService, RaiderIoService>();
        services.AddTransient<BlizzardService>();

        return builder;
    }
}
