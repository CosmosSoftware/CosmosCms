using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Create page view model
    /// </summary>
    public class CreatePageViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CreatePageViewModel()
        {
            Templates = new List<SelectListItem>();
        }
        /// <summary>
        /// Page ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }
        /// <summary>
        /// Page title
        /// </summary>
        [Display(Name = "Page Title")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Pages must have a title.")]
        public string Title { get; set; }
        /// <summary>
        /// Page templated used
        /// </summary>
        [Display(Name = "Page template (optional)")]
        public Guid? TemplateId { get; set; }
        /// <summary>
        /// Template list
        /// </summary>
        public List<SelectListItem> Templates { get; set; }
    }
}