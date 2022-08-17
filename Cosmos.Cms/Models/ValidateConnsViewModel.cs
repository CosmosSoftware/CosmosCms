using System.Collections.Generic;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Validate connection view model
    /// </summary>
    public class ValConViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ValConViewModel()
        {
            Results = new List<ConnectionResult>();
        }

        /// <summary>
        /// Connection results
        /// </summary>
        public List<ConnectionResult> Results { get; set; }
    }

    /// <summary>
    /// Connection result
    /// </summary>
    public class ConnectionResult
    {
        /// <summary>
        /// DNS name of host
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// Service type
        /// </summary>
        public string ServiceType { get; set; }
        /// <summary>
        /// Is successful
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }
    }
}