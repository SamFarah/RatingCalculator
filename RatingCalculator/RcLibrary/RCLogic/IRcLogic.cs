using RcLibrary.Models;

namespace RcLibrary.RCLogic
{
    public interface IRcLogic
    {
        Task<ProcessedCharacter?> ProcessCharacter(string region, string realm, string name, double targetRating);
        Task<Affix?> GetCurrentBaseAffix(string region);
    }
}