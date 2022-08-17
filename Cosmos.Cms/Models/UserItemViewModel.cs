using System;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Identity user item view
    /// </summary>
    [Serializable]
    public class UserItemViewModel
    {
        /// <summary>
        ///     User ID
        /// </summary>
        [Key]
        [Display(Name = "User ID")]
        public string Id { get; set; }

        /// <summary>
        ///     User Email Address
        /// </summary>
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        /// <summary>
        ///     Email address is confirmed
        /// </summary>
        [Display(Name = "Email is Confirmed")]
        public bool EmailConfirmed { get; set; }

        /// <summary>
        ///     User's phone number
        /// </summary>
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        /// <summary>
        ///     Indicates if the user is selected
        /// </summary>
        [Display(Name = "Role Member")]
        public bool Selected { get; set; } = false;
    }
}