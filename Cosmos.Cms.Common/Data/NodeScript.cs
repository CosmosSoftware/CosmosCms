using System;
using System.ComponentModel.DataAnnotations;
using Cosmos.Cms.Common.Data.Logic;

namespace Cosmos.Cms.Common.Data
{
    /// <summary>
    /// Node script
    /// </summary>
    public class NodeScript
    {
        /// <summary>
        /// Script Id
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Endpoint Name
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        /// Version number
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Published date and time
        /// </summary>
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Date and time updated
        /// </summary>
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Script expiration date and time
        /// </summary>
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        ///     Status of this article
        /// </summary>
        /// <remarks>See <see cref="StatusCodeEnum" /> enum for code numbers.</remarks>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// Input variables
        /// </summary>
        public string[] InputVars { get; set; }

        /// <summary>
        /// Input configuration
        /// </summary>
        public string Config { get; set; }

        /// <summary>
        /// Node JavaScript code
        /// </summary>
        public string Code { get; set; }
    }
}
