namespace RcLibrary.Models
{
    public class ProcessedCharacter : WowCharacter
    {        
        public double? Rating { get; set; }
        public double? TargetRating { get; set; }
        public List<List<KeyRun>>? RunOptions { get; set; }

    }
}
