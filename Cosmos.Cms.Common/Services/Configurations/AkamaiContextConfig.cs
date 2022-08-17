using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    ///     Akamai CDN Configuration
    /// </summary>
    public class AkamaiContextConfig
    {
        /// <summary>
        ///     Akamai CP Code
        /// </summary>
        [Display(Name = "CP code")]
        public string CpCode { get; set; }

        /// <summary>
        ///     Akamai Url Root
        /// </summary>
        [Display(Name = "Url root")]
        public string UrlRoot { get; set; }

        /// <summary>
        ///     Akamai Access Token
        /// </summary>
        [Display(Name = "Access token")]
        public string AccessToken { get; set; }

        /// <summary>
        ///     Akamai Host Name
        /// </summary>
        [Display(Name = "Host name")]
        public string AkamaiHost { get; set; }

        /// <summary>
        ///     Akamai Client Token
        /// </summary>
        [Display(Name = "Client token")]
        public string ClientToken { get; set; }

        /// <summary>
        ///     Akamai Secret
        /// </summary>
        [Display(Name = "Akamai secret")]
        public string Secret { get; set; }
    }
}