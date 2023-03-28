using RcLibrary.Models;
using static RcLibrary.Models.Enums;

namespace RcLibrary.RCLogic
{
    public interface IRcLogic
    {        
        Task<WowStaticData?> GetWowStaticData();
        Task<ProcessedCharacter?> ProcessCharacter(string region, string realm, string name, string season, double targetRating, WowStaticData? staticWowData);
        Task<Affix?> GetCurrentBaseAffix(string region);
    }
}