namespace RcLibrary.Models
{
    public class ProcessedCharacter : WowCharacter
    {
        public double? Rating { get; set; }
        public double? TargetRating { get; set; }
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
}
