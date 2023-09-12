using AutoMapper;
using MythicPlanner.Models;
using RcLibrary.Helpers;
using RcLibrary.Models;
using RcLibrary.Models.Configurations;
using RcLibrary.RCLogic;

namespace MythicPlanner;

public class Startup
{

    public IConfiguration Configuration { get; }
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private static IMapper ConfigureAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ProcessedCharacter, WowCharacterViewModel>()
                .ForMember(dest => dest.Guild, opts => opts.MapFrom(src => src.Guild == null ? "" : src.Guild.Name));
            cfg.CreateMap<RaiderIoCharacter, ProcessedCharacter>();
            cfg.CreateMap<Dungeon, DungeonWithScores>();
            cfg.CreateMap<Affix, AffixViewModel>();
            cfg.CreateMap<KeyRun, KeyRunViewModel>();

        });

        var output = config.CreateMapper();
        return output;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<Settings>(Configuration.GetSection(Settings.MainSectionName));
        services.AddOptions();

        services.AddMvc();

        services.AddSingleton(ConfigureAutoMapper());
        services.AddSingleton<IApiHelper, ApiHelper>();
        services.AddSingleton<IRcLogic, RcLogic>();
        services.AddTransient<IAppVersionService, AppVersionService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapRazorPages();
        });
    }
}