using Microsoft.AspNetCore.Mvc;

namespace MythicPlanner.Controllers;
public class DiscordBotController : Controller
{
    public IActionResult TermsOfService() => View();
    public IActionResult PrivacyPolicy() => View();
}
