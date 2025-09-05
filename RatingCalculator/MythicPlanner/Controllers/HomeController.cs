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

    private async Task PopulateSharedUrlViewBags(string region, string realm, int expansionId, string? seasonSlug)
    {
        // Get all dropdown data server-side
        var regionEnum = Enum.TryParse<Enums.Regions>(region, true, out var parsedRegion) ? parsedRegion : Enums.Regions.US;
        var regionString = regionEnum.ToString().ToLower();

        // Get realms and create SelectList with correct selection
        var realms = await _ratingCalculator.GetRegionRealmsAsync(regionEnum.ToString());
        var mappedRealms = _mapper.Map<List<DropDownItem>>(realms);
        
        // Create SelectList with the correct selected value
        ViewBag.RealmsList = new SelectList(mappedRealms, "Value", "Text", realm);

        // Get seasons and mark the correct one as selected
        if (expansionId > 0)
        {
            var seasons = await _ratingCalculator.GetRegionSeasonsAsync(regionString, expansionId);
            var currentSeason = await _ratingCalculator.GetWowCurrentSeason(regionString, expansionId);
            
            // Use provided seasonSlug or fall back to current season
            var selectedSeasonSlug = seasonSlug ?? currentSeason?.Slug ?? seasons?.FirstOrDefault()?.Slug;
            
            ViewBag.seasons = seasons?.Select(x => new { 
                Text = x.Name, 
                Value = x.Slug, 
                Selected = (x.Slug == selectedSeasonSlug),
                Title = x.Name
            }).ToList();

            // Get dungeons if we have a season
            if (!string.IsNullOrEmpty(selectedSeasonSlug))
            {
                var season = await _ratingCalculator.GetSeason(regionString, selectedSeasonSlug, expansionId);
                ViewBag.dungeons = season?.Dungeons?.Select(x => new { 
                    Text = x.Name, 
                    Value = x.Slug,
                    Selected = false,
                    Title = x.Name
                }).ToList();
            }
        }

        // Mark this as a shared URL so the view knows to use pre-populated data
        ViewBag.IsSharedUrl = true;
    }

    public async Task<IActionResult> Index()
    {
        await GetExpansionView();
        ViewBag.dungeonMatrix = _ratingCalculator.GetDungeonMetrics();
        return View();
    }

    public async Task<IActionResult> Share(string region, string realm, string character, int targetScore,
        int? expansionId, string? seasonSlug, string? avoidDungeons, int? maxKeyLevel)
    {
        // Set up basic ViewBag data
        await GetExpansionView();
        ViewBag.dungeonMatrix = _ratingCalculator.GetDungeonMetrics();
        
        // Get current expansion if not provided
        var currentExpId = expansionId;
        if (currentExpId == null)
        {
            var exps = await _ratingCalculator.GetWowExpansionsAsync("us");
            currentExpId = exps?.Max(x => x.Id) ?? 0;
        }

        // Pre-populate all dropdown data on server-side
        await PopulateSharedUrlViewBags(region, realm, currentExpId.Value, seasonSlug);
        
        // Get the selected season from the server-side logic
        var selectedSeasonSlug = seasonSlug;
        if (string.IsNullOrEmpty(selectedSeasonSlug) && currentExpId > 0)
        {
            var seasons = await _ratingCalculator.GetRegionSeasonsAsync(region.ToLower(), currentExpId.Value);
            var currentSeason = await _ratingCalculator.GetWowCurrentSeason(region.ToLower(), currentExpId.Value);
            selectedSeasonSlug = currentSeason?.Slug ?? seasons?.FirstOrDefault()?.Slug ?? string.Empty;
        }
        
        // Create model with all data pre-populated
        var model = new SearchToonViewModel
        {
            Region = Enum.TryParse<Enums.Regions>(region, true, out var regionResult) ? regionResult : Enums.Regions.US,
            Realm = realm, // Use the original realm slug value (not unescaped)
            CharacterName = Uri.UnescapeDataString(character),
            TargetRating = targetScore,
            ExpansionId = currentExpId.Value,
            SeasonSlug = selectedSeasonSlug,
            MaxKeyLevel = maxKeyLevel ?? 15,
            AvoidDungeon = string.IsNullOrEmpty(avoidDungeons) 
                ? new List<string>() 
                : avoidDungeons.Split(',').ToList()
        };

        // Process the character server-side and get results
        try
        {
            var toon = await _ratingCalculator.ProcessCharacter(model.ExpansionId,
                                                                model.SeasonSlug,
                                                                (model.Region ?? Enums.Regions.US).ToString().ToLower(),
                                                                model.Realm,
                                                                model.CharacterName,
                                                                model.TargetRating ?? 0,
                                                                model.AvoidDungeon,
                                                                model.MaxKeyLevel);

            if (toon != null)
            {
                var resultModel = _mapper.Map<WowCharacterViewModel>(toon);
                ViewBag.CharacterResult = resultModel;
                
                // Generate shareable URL
                var shareableUrl = await GenerateShareableUrl(model);
                ViewBag.ShareableUrl = shareableUrl;
                ViewBag.ShowShareSection = true;
            }
            else
            {
                ViewBag.CharacterError = "Character not found";
            }
        }
        catch (Exception ex)
        {
            ViewBag.CharacterError = $"An error occurred: {ex.Message}";
            _logger.LogError(ex, "Error processing character for shared URL");
        }

        return View("Index", model);
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
            ViewBag.ShareableUrl = shareableUrl;
            
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
        
        if (m.AvoidDungeon?.Any() == true)
            queryParams.Add($"avoidDungeons={Uri.EscapeDataString(string.Join(",", m.AvoidDungeon))}");
        
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