using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MythicPlanner.Models;
using RcLibrary.Models;
using RcLibrary.Models.RaiderIoModels;
using RcLibrary.Servcies.RatingCalculatorServices;
using System.Diagnostics;

namespace MythicPlanner.Controllers;

public class HomeController : Controller
{
    private readonly IRcService _ratingCalculator;
    private readonly IMapper _mapper;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IRcService ratingCalculator,
                          IMapper mapper,
                          ILogger<HomeController> logger)
    {
        _ratingCalculator = ratingCalculator;
        _mapper = mapper;
        _logger = logger;
    }


    private async Task GetSeasonView()
    {
        var seasons = await _ratingCalculator.GetRegionSeasonsAsync("us");
        var currentSeason = await _ratingCalculator.GetWowCurrentSeason("us");
        ViewBag.seasonSlug = currentSeason?.Slug;
        ViewBag.seasons = seasons?.Select(x => new { Text = x.Name, Value = x.Slug, Selected = (x.Slug == currentSeason?.Slug) }).ToList();
    }
 

    public async Task<IActionResult> Index()
    {
        await GetSeasonView();        
        ViewBag.dungeonMatrix = _ratingCalculator.GetDungeonMetrics();
        return View();
    }

    public async Task<IActionResult> ProcessCharacter(SearchToonViewModel m)
    {
        if (ModelState.IsValid)
        {
            var toon = await _ratingCalculator.ProcessCharacter(m.SeasonSlug,
                                                                (m.Region ?? Enums.Regions.US).ToString().ToLower(),
                                                                m.Realm,
                                                                m.CharacterName,
                                                                m.TargetRating ?? 0,
                                                                m.ThisWeekOnly,
                                                                m.AvoidDungeon,
                                                                m.MaxKeyLevel);

            if (toon == null) { return NotFound("Character not found"); }
            var resultModel = _mapper.Map<WowCharacterViewModel>(toon);
            return PartialView("_CharInfo", resultModel);
        }

        return BadRequest($"<ul class='error-list'>{string.Join(string.Empty, ModelState.Values.SelectMany(v => v.Errors).Select(x => $"<li>{x.ErrorMessage}</li>"))}</ul>");
    }

    public async Task<IActionResult> GetRealms(Enums.Regions region)
    {
        var output = _mapper.Map<List<DropDownItem>>(await _ratingCalculator.GetRegionRealmsAsync(region.ToString()));
        return Json(output);
    }

    public async Task<IActionResult> GetDungeons(string seasonSlug)
    {                
        var output = _mapper.Map<List<DropDownItem>>( (await _ratingCalculator.GetSeason("us", seasonSlug))?.Dungeons);
        return Json(output);
    }
    

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}