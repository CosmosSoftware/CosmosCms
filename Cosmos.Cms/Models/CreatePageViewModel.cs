using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    public class CreatePageViewModel
    {
        public CreatePageViewModel()
        {
            Templates = new List<SelectListItem>();
        }

        [Key]
        public Guid Id { get; set; }

        [Display(Name = "Page Title")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Pages must have a title.")]
        public string Title { get; set; }

        [Display(Name = "Page template (optional)")]
        public Guid? TemplateId { get; set; }

        public List<SelectListItem> Templates { get; set; }
    }
}