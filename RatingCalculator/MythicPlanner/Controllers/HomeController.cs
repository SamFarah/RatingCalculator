using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MythicPlanner.Models;
using RcLibrary.Models;
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

    private async Task GetExpansionView()
    {
        var defaultRegion = "us";
        var exps = await _ratingCalculator.GetWowExpansionsAsync(defaultRegion);
        var currentExpId = exps?.Max(x => x.Id);

        ViewBag.expansions = exps?.Select(x => new { Text = x.Name, Value = x.Id, Selected = (x.Id == currentExpId) }).ToList();
    }

    [Route("/")]
    [Route("/Index")]
    [Route("/share/{Region?}/{Realm?}/{CharacterName?}/{TargetRating?}", Name = "ShareRoute")]
    public async Task<IActionResult> Index(SearchToonViewModel model)
    {
        var routeName = HttpContext.GetEndpoint()?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;

        await GetExpansionView();
        ViewBag.dungeonMatrix = _ratingCalculator.GetDungeonMetrics();

        if (routeName == "ShareRoute")
        {
            model.FromUrl = true;
            return View(model);
        }
        ModelState.Clear();
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

            // Generate shareable URL
            var shareableUrl = await GenerateShareableUrl(m);            
            resultModel.ShareableUrl = shareableUrl;

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


    private async Task<string> GenerateShareableUrl(SearchToonViewModel m)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var region = m.Region?.ToString().ToLower() ?? "us";
        var realm = Uri.EscapeDataString(m.Realm);
        var character = Uri.EscapeDataString(m.CharacterName);
        var targetScore = m.TargetRating ?? 0;

        var url = $"{baseUrl}/share/{region}/{realm}/{character}/{targetScore}";

        var queryParams = new List<string>();

        // Get current defaults to compare against
        var exps = await _ratingCalculator.GetWowExpansionsAsync("us");
        var currentExpId = exps?.Max(x => x.Id) ?? 0;

        string? currentSeasonSlug = null;
        if (currentExpId > 0)
        {
            var currentSeason = await _ratingCalculator.GetWowCurrentSeason(region, currentExpId);
            currentSeasonSlug = currentSeason?.Slug;
        }

        // Only add parameters if they differ from defaults
        if (m.ExpansionId > 0 && m.ExpansionId != currentExpId)
            queryParams.Add($"expansionId={m.ExpansionId}");

        if (!string.IsNullOrEmpty(m.SeasonSlug) && m.SeasonSlug != currentSeasonSlug)
            queryParams.Add($"seasonSlug={Uri.EscapeDataString(m.SeasonSlug)}");

        // fixed formatting to allow mapping of the model
        m.AvoidDungeon?.ForEach(dungeon => queryParams.Add($"AvoidDungeon={Uri.EscapeDataString(dungeon)}"));


        if (m.MaxKeyLevel.HasValue && m.MaxKeyLevel != 15)
            queryParams.Add($"maxKeyLevel={m.MaxKeyLevel}");

        if (queryParams.Any())
            url += "?" + string.Join("&", queryParams);

        return url;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}