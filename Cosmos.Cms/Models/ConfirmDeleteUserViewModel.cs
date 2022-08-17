using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    public class ConfirmDeleteUserViewModel
    {
        [Key]
        [Required]
        [Display(Name = "User ID.")]
        public string UserId { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address:")]
        [DataType(DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Must confirm delete to continue.")]
        [Display(Name = "WARNING: This cannot be undone! Confirm delete:")]
        public bool ConfirmDelete { get; set; }
    }
}