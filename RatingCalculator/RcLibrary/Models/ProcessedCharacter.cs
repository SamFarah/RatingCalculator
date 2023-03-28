namespace RcLibrary.Models
{
    public class ProcessedCharacter : WowCharacter
    {
        public DateTime LastCrawledAt { get; set; }
        public double? Rating { get; set; }
        public double? TargetRating { get; set; }
        public List<List<KeyRun>>? RunOptions { get; set; }

    }
}
