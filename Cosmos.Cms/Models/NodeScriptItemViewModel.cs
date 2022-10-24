using System;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Script item view model
    /// </summary>
    public class NodeScriptItemViewModel
    {
        /// <summary>
        /// Script ID
        /// </summary>
        [Key]
        [Display(Name = "Script Id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Published date time
        /// </summary>
        [Display(Name = "Published")]
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Published date time
        /// </summary>
        [Display(Name = "Updated")]
        public DateTimeOffset Updated { get; set; }

        /// <summary>
        /// Expiration date and time
        /// </summary>
        [Display(Name = "Expires")]
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// End Point
        /// </summary>
        [Display(Name = "End Point")]
        [Required(AllowEmptyStrings = false)]
        [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "Letters or numbers only.")]
        public string EndPoint { get; set; }

        /// <summary>
        /// Versions
        /// </summary>
        [Display(Name = "Version")]
        public int Version { get; set; }

        /// <summary>
        /// Input variables
        /// </summary>
        [Display(Name = "Input Variables")]
        public string InputVars { get; set; }

        /// <summary>
        /// Description of what this script does.
        /// </summary>
        [Required]
        [MinLength(2)]
        [Display(Name = "Description")]
        public string Description { get; set; }

    }
}
