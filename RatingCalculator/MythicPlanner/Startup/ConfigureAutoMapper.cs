using AutoMapper;
using MythicPlanner.Models;
using RcLibrary.Models;

namespace MythicPlanner.Startup;

public static class AutoMapper
{
    public static IMapper CreateConfiguredMapper()
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
}
