using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Users index view model
    /// </summary>
    public class UsersIndexViewModel
    {
        /// <summary>
        /// Unique user ID
        /// </summary>
        [Key] public string UserId { get; set; }
        /// <summary>
        /// User's email address
        /// </summary>
        [Display(Name = "Email Address")]
        [EmailAddress]
        public string EmailAddress { get; set; }

        /// <summary>
        /// Role membership
        /// </summary>
        [Display(Name = "Role(s)")]
        public string Role { get; set; }
        /// <summary>
        /// User's phone number (can be SMS)
        /// </summary>
        [Display(Name = "Telephone #")]
        [Phone]
        public string PhoneNumber { get; set; }
        /// <summary>
        /// User's email address is confirmed.
        /// </summary>
        [Display(Name = "Email Confirmed")] public bool EmailConfirmed { get; set; }
        /// <summary>
        /// How user logs in (can be OAuth provider).
        /// </summary>
        [Display(Name = "Login Provider")] public string LoginProvider { get; set; }
    }
}