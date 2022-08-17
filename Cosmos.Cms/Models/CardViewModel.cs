using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    public class CardViewModel
    {
        [Display(Name = "Card title (keep short!):")]
        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string CardTitle { get; set; }

        [Display(Name = "Select page or type URL:")]
        [UIHint("Url")]
        [Required]
        public string CardUrl { get; set; }

        [Display(Name = "Choose an icon:")]
        [UIHint("Icon")]
        [Required]
        public string CardIcon { get; set; }
    }
}