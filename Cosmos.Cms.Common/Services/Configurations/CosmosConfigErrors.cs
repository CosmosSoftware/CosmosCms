using System;
using System.Collections.Generic;

namespace Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    /// Holds Cosmos Configuration Status
    /// </summary>
    public class CosmosConfigStatus
    {
        /// <summary>
        /// Indicates if Cosmos passes configuration tests.
        /// </summary>
        public bool ReadyToRun { get; set; }
        /// <summary>
        /// Diagnostics.
        /// </summary>
        public List<Diagnostic> Diagnostics { get; set; }
        /// <summary>
        /// Date/Time when configuration was checked
        /// </summary>
        public DateTimeOffset DateTimeStamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
