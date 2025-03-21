using MythicPlanner.BackgroundServices;
using MythicPlanner.Models;
using RcLibrary.Helpers;
using RcLibrary.Models.Configurations;
using RcLibrary.Servcies.BlizzardServices;
using RcLibrary.Servcies.MemoryCacheServices;
using RcLibrary.Servcies.RaiderIoServices;
using RcLibrary.Servcies.RatingCalculatorServices;
using Serilog;

namespace MythicPlanner.Startup;

public static class WebAppBuilderExtensions
{

    public static WebApplicationBuilder UseSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog();
        return builder;
    }


    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.Configure<Settings>(builder.Configuration.GetSection(Settings.MainSectionName));
        services.AddOptions();

        services.AddMvc();

        services.AddHostedService<DiscordBotService>(); 

        services.AddSingleton(AutoMapper.CreateConfiguredMapper());
        services.AddSingleton<IRcService, RcService>();
        services.AddSingleton<IRaiderIoService, RaiderIoService>();
        services.AddSingleton<IBlizzardService, BlizzardService>();

        services.AddTransient<IAppVersionService, AppVersionService>();
        services.AddTransient<IMemoryCacheService, MemoryCacheService>();
        services.AddTransient<IApiHelper, ApiHelper>();

        return builder;
    }
}
