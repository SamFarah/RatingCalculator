using RcLibrary.Models;
using System.ComponentModel.DataAnnotations;

namespace MythicPlanner.Models;

public class SearchToonViewModel
{

    [Required]
    public Enums.Regions? Region { get; set; } = Enums.Regions.US;

    [Required]
    [MaxLength(100)]
    public string Realm { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Character Name")]
    [MaxLength(20)]
    public string CharacterName { get; set; } = string.Empty;

    [Required]
    [Range(1, 5000)]
    [Display(Name = "Target Rating")]
    public int? TargetRating { get; set; } = 0;

    [Display(Name = "Avoid These Dungeons")]
    public List<string>? AvoidDungeon { get; set; }

    [Required]
    [Range(2, 50)]
    [Display(Name = "Max Level")]
    public int? MaxKeyLevel { get; set; } = 15;

    [Display(Name = "Get the rating this week")]
    public bool ThisWeekOnly { get; set; } = true;

    [Display(Name = "Season")]
    public string SeasonSlug { get; set; } = string.Empty;

    [Display(Name = "Expansion")]
    public int ExpansionId { get; set; } = 0;
}
