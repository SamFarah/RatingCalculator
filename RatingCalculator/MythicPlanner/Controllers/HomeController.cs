using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MythicPlanner.Models;
using RcLibrary.Models;
using RcLibrary.Models.Configurations;
using RcLibrary.Servcies.RatingCalculatorServices;
using System.Diagnostics;

namespace MythicPlanner.Controllers;

public class HomeController : Controller
{
    private readonly IRcService _ratingCalculator;
    private readonly IMapper _mapper;
    private readonly ILogger<HomeController> _logger;
    private readonly Settings _configs;

    public HomeController(IRcService ratingCalculator,
                          IMapper mapper,
                          ILogger<HomeController> logger,
                          IOptions<Settings> configs)
    {
        _ratingCalculator = ratingCalculator;
        _mapper = mapper;
        _logger = logger;
        _configs = configs.Value;
    }

    private async Task GetExpansionView()
    {
        var defaultRegion = "us";
        var exps = await _ratingCalculator.GetWowExpansionsAsync(defaultRegion);
        var currentExpId = exps?.Max(x => x.Id);


        // I beleive those random times when the website breaks is because API did not get back the expansion list properlly
        // if thats the case.. then revert back to exp ID that is defined in the appsettings. 
        if (exps == null || exps.Count == 0)
        {
            _ratingCalculator.RemoveCachedWowExpansions(defaultRegion); // remove whatever empty list that have been cached, maybe it will do better next time.
            _logger.LogWarning("GetWowExpansionsAsync returned null or no elements, reverting back to fall back expansion");
            currentExpId = _configs.CurrentExpansionIdFallBack;
            exps = new() { new() { Id = currentExpId.Value, Name = "Current Expansion" } };
        }

        ViewBag.expansions = exps?.Select(x => new { Text = x.Name, Value = x.Id, Selected = (x.Id == currentExpId) }).ToList();
    }

    public async Task<IActionResult> Index()
    {
        await GetExpansionView();
        ViewBag.dungeonMatrix = _ratingCalculator.GetDungeonMetrics();
        return View();
    }

    public async Task<IActionResult> ProcessCharacter(SearchToonViewModel m)
    {
        if (ModelState.IsValid)
        {
            var toon = await _ratingCalculator.ProcessCharacter(m.ExpansionId,
                                                                m.SeasonSlug,
                                                                (m.Region ?? Enums.Regions.US).ToString().ToLower(),
                                                                m.Realm,
                                                                m.CharacterName,
                                                                m.TargetRating ?? 0,
                                                                // m.ThisWeekOnly,
                                                                m.AvoidDungeon,
                                                                m.MaxKeyLevel);

            if (toon == null) { return NotFound("Character not found"); }
            var resultModel = _mapper.Map<WowCharacterViewModel>(toon);
            return PartialView("_CharInfo", resultModel);
        }

        return BadRequest($"<ul class='error-list'>{string.Join(string.Empty, ModelState.Values.SelectMany(v => v.Errors).Select(x => $"<li>{x.ErrorMessage}</li>"))}</ul>");
    }

    public async Task<IActionResult> GetRealms(Enums.Regions id)
    {
        var realmList = await _ratingCalculator.GetRegionRealmsAsync(id.ToString());
        var output = _mapper.Map<List<DropDownItem>>(realmList);        
        if (realmList == null || realmList.Count == 0)
        {
            _ratingCalculator.RemoveCachedWowRealms(id.ToString()); 
            _logger.LogWarning("GetRegionRealmsAsync returned null or no elements");            
        }
        return Json(output);
    }

    public async Task<IActionResult> GetSeasons(int id)
    {
        var seasons = await _ratingCalculator.GetRegionSeasonsAsync("us", id);
        if (seasons == null || !seasons.Any()) return Json(null);
        var currentSeason = await _ratingCalculator.GetWowCurrentSeason("us", id);
        if (currentSeason == null)
        {
            seasons.First().Current = true;
        }
        else
        {
            seasons?.ForEach(x => x.Current = x.Slug == currentSeason?.Slug);
        }


        var output = _mapper.Map<List<DropDownItem>>(seasons);
        return Json(output);
        //ViewBag.seasons = seasons?.Select(x => new { Text = x.Name, Value = x.Slug, Selected = (x.Slug == currentSeason?.Slug) }).ToList();
    }

    public async Task<IActionResult> GetDungeons(string id)
    {
        var filter = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(id);
        var Season = (string)(filter?.Season ?? "");
        var expId = (int)(filter?.Expansion);
        var output = _mapper.Map<List<DropDownItem>>((await _ratingCalculator.GetSeason("us", Season, expId))?.Dungeons);
        return Json(output);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}