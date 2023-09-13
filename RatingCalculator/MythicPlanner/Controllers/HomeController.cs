using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MythicPlanner.Models;
using RcLibrary.Models;
using RcLibrary.RCLogic;
using System.Diagnostics;

namespace MythicPlanner.Controllers;

public class HomeController : Controller
{
    private readonly IRcLogic _ratingCalculator;
    private readonly IMapper _mapper;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IRcLogic ratingCalculator,
                          IMapper mapper,
                          ILogger<HomeController> logger)
    {
        _ratingCalculator = ratingCalculator;
        _mapper = mapper;
        _logger = logger;
    }

    private async Task GetDungeonsView()
    {
        var season = await _ratingCalculator.GetSeason();
        ViewBag.seasonDungeons = season?.Dungeons?.Select(x => new SelectListItem { Text = x.Name, Value = x.Slug }).ToList();
    }

    public async Task<IActionResult> Index()
    {
        await GetDungeonsView();
        ViewBag.dungeonMatrix = _ratingCalculator.GetDungeonMetrics();
        return View();
    }

    public async Task<IActionResult> ProcessCharacter(SearchToonViewModel m)
    {
        if (ModelState.IsValid)
        {
            var toon = await _ratingCalculator.ProcessCharacter((m.Region ?? Enums.Regions.US).ToString().ToLower(),
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


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}