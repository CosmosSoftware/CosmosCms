using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Update inde view model
    /// </summary>
    public class UpgradeIndexViewModel
    {
        /// <summary>
        ///     Administrator authorizes upgrade
        /// </summary>
        [Display(Name = "Upgrade Authorized")]
        [Required(ErrorMessage = "Authorization is required.")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Authorization is required.")]
        public bool Authorized { get; set; } = false;

        /// <summary>
        /// System is backed up and ready to upgrade
        /// </summary>
        [Display(Name = "Upgrade Authorized")]
        [Required(ErrorMessage = "Acknowledgement is required.")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Acknowledgement is required.")]
        public bool IsBackedUp { get; set; } = false;

        /// <summary>
        /// Software upgrade is required
        /// </summary>
        [Display(Name = "Software Upgrade Required")]
        [Required(ErrorMessage = "Acknowledgement is required.")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Acknowledgement is required.")]
        public bool SoftwareUpgrade { get; set; } = false;
        /// <summary>
        /// Pending upgrades
        /// </summary>
        [Display(Name = "Pending Upgrades")] public List<string> PendingMigrations { get; set; }
    }
}