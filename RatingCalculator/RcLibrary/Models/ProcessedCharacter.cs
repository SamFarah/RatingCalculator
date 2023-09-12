namespace RcLibrary.Models;

public class ProcessedCharacter : WowCharacter
{
    public Rating Rating { get; set; } = new Rating();
    public Rating TargetRating { get; set; } = new Rating();
    public List<List<KeyRun>>? RunOptions { get; set; }
    public int ThisWeekAffixId { get; set; }

    //public List<List<KeyRun>>? ThisWeekOptions
    //{
    //    get { 
    //        return RunOptions?.Select(x=> x.Where(y=>y.Affixes?.First()?.Id == ThisWeekAffixId).ToList()).ToList();
    //    }
    //}
    //public List<List<KeyRun>>? NextWeekOptions { get {
    //        return RunOptions?.Select(x => x.Where(y => y.Affixes?.First()?.Id != ThisWeekAffixId).ToList()).ToList();
    //    } }

}
