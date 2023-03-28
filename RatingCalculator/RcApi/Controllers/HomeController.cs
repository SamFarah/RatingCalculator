using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using RcApi.Models;
using RcLibrary.Models;
using RcLibrary.RCLogic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace RcApi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRcLogic _rc;
        private readonly IMemoryCache _memoryCache;
        private readonly IMapper _mapper;

        public HomeController(ILogger<HomeController> logger,
                              IRcLogic rc,
                              IMemoryCache memoryCache,
                              IMapper mapper)
        {
            _logger = logger;
            _rc = rc;
            _memoryCache = memoryCache;
            _mapper = mapper;
        }

      
        private async Task<WowStaticData?> GetStaticData()
        {
            var cacheKey = "wowStaticData";
            WowStaticData? cachedValue;
            if (!_memoryCache.TryGetValue(cacheKey, out cachedValue))
            {
                cachedValue = await _rc.GetWowStaticData();
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1));
                _memoryCache.Set(cacheKey, cachedValue, cacheEntryOptions);
            }
            return  cachedValue;
        }

        private List<SelectListItem>? GetSeasons(WowStaticData? wowStaticData)
        {
            return wowStaticData?.Seasons?.Select(x => new SelectListItem (x.Name, x.Slug )).ToList();
        }

        private async Task GetViewData()
        {
            var wowStaticData = await GetStaticData();
            ViewBag.Seasons = GetSeasons(wowStaticData)?? new List<SelectListItem> {new SelectListItem ("Error","Error")};
        }
        

        public async Task<IActionResult> Index()
        {
            await GetViewData();
            //await _rc.GetCharacter("us", "proudmoore", "saloraa", "season-df-1");


            return View();
        }     

        public async Task<IActionResult> ProcessCharacter(string region, string realm, string name, string season, double targetRating)
        {
            //_logger.LogInformation("Test: {region}, {realm}, {name}, {season}, {targetRating}", region, realm, name, season, targetRating);
            var toon = await GetCharacterInfo(region, realm, name, season, targetRating);
            if (toon == null) { return NotFound("Character not found"); }
            return PartialView("_CharInfo", toon);
        }

        private async Task<WowCharacterViewModel> GetCharacterInfo(string region, string realm, string name, string season, double targetRating)
        {
            var wowStaticData = await GetStaticData();
            var toon = await _rc.ProcessCharacter(region, realm, name, season, targetRating, wowStaticData);            
            var model  = _mapper.Map<WowCharacterViewModel>(toon);
            return model;
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}