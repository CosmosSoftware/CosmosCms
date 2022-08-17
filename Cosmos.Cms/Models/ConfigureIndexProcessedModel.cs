using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    public class ConfigureIndexProcessedModel
    {
        /// <summary>
        ///     Procecced configuration that can be copied and installed
        /// </summary>
        [Display(Name = "Output JSON")]
        public string OutputJson { get; set; }

        public ModelStateDictionary ModelState { get; set; }
    }
}