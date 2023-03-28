using AutoMapper;
using Microsoft.AspNetCore.Mvc;
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

        public IActionResult Index() { return View(); }

        public async Task<IActionResult> ProcessCharacter(string region, string realm, string name, double targetRating)
        {
            var toon = await _ratingCalculator.ProcessCharacter(region, realm, name, targetRating);
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