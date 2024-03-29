﻿using AutoMapper;
using MythicPlanner.Models;
using RcLibrary.Models;
using RcLibrary.Models.BlizzardModels;
using RcLibrary.Models.RaiderIoModels;

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
            cfg.CreateMap<Realm, DropDownItem>()
            .ForMember(dest => dest.Text, opts => opts.MapFrom(src => src.Name))
            .ForMember(dest => dest.Value, opts => opts.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Title, opts => opts.MapFrom(src => src.Name));

            cfg.CreateMap<Season, DropDownItem>()
           .ForMember(dest => dest.Text, opts => opts.MapFrom(src => src.Name))
           .ForMember(dest => dest.Value, opts => opts.MapFrom(src => src.Slug))
           .ForMember(dest => dest.Title, opts => opts.MapFrom(src => src.Name))
           .ForMember(dest => dest.Selected, opts => opts.MapFrom(src => src.Current));

            cfg.CreateMap<Dungeon, DropDownItem>()
            .ForMember(dest => dest.Text, opts => opts.MapFrom(src => src.Name))
            .ForMember(dest => dest.Value, opts => opts.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Title, opts => opts.MapFrom(src => src.ShortName));

        });

        var output = config.CreateMapper();
        return output;
    }
}
