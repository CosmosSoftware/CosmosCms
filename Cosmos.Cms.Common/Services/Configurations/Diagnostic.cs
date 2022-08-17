using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    /// Diagnostic result
    /// </summary>
    public class Diagnostic
    {
        /// <summary>
        /// Information related to this test.
        /// </summary>
        [Display(Name = "Information or Error Message")]
        public string Message { get; set; }
        /// <summary>
        /// Service Type
        /// </summary>
        [Display(Name = "Service Type")]
        public string ServiceType { get; set; }
        /// <summary>
        /// Test was a success
        /// </summary>
        [Display(Name = "Successful")]
        public bool Success { get; set; }
    }
}