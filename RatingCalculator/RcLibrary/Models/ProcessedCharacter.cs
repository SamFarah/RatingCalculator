using RcLibrary.Models.RaiderIoModels;

namespace RcLibrary.Models;

public class ProcessedCharacter : WowCharacter
{
    public Rating Rating { get; set; } = new Rating();
    public Rating TargetRating { get; set; } = new Rating();
    public List<List<KeyRun>>? RunOptions { get; set; }
    public int ThisWeekAffixId { get; set; }

}
