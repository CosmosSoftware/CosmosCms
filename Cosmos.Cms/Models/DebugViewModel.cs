using System;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Debug Result View Model
    /// </summary>
    public class DebugViewModel
    {
        /// <summary>
        /// Script Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Input data
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// API Result
        /// </summary>
        public ApiResult ApiResult { get; set; }
    }
}
