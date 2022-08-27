using System;
using System.Collections.Generic;

namespace Cosmos.Cms.Models
{
    public class ApiResult
    {
        public ApiResult(string jsonData)
        {
            JsonData = jsonData;
        }
        /// <summary>
        /// Date/Time Stamp
        /// </summary>
        public DateTimeOffset DateTimeStamp { get; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// JSON Return data
        /// </summary>
        public string JsonData { get; set; }

        /// <summary>
        /// Indicates success
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Errors
        /// </summary>
        public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
    }
}
