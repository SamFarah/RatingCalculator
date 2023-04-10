using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RcApi.Models;
using RcLibrary.RCLogic;
using System.Diagnostics;

namespace RcApi.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRcLogic _ratingCalculator;
        private readonly IMapper _mapper;

        public HomeController(IRcLogic ratingCalculator,
                              IMapper mapper)
        {
            _ratingCalculator = ratingCalculator;
            _mapper = mapper;
        }

        private async Task GetDungeonsView()
        {
            var season = await _ratingCalculator.GetSeason();
            ViewBag.seasonDugneons = season.Dungeons?.Select(x => new SelectListItem { Text = x.Name, Value = x.Slug }).ToList();
        }

        public async Task<IActionResult> Index()
        {
            await GetDungeonsView();
            return View();
        }

        public async Task<IActionResult> ProcessCharacter(string region, string realm, string name, double targetRating, bool thisweekOnly, string? avoidDung)
        {
            var toon = await _ratingCalculator.ProcessCharacter(region, realm, name, targetRating, thisweekOnly, avoidDung);
            if (toon == null) { return NotFound("Character not found"); }
            var model = _mapper.Map<WowCharacterViewModel>(toon);
            return PartialView("_CharInfo", model);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}