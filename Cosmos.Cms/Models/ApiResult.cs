using System;
using System.Collections.Generic;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// API Result
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="returnData"></param>
        public ApiResult(string returnData)
        {
            ReturnData = returnData;
        }
        /// <summary>
        /// Date/Time Stamp
        /// </summary>
        public DateTimeOffset DateTimeStamp { get; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Return data
        /// </summary>
        public string ReturnData { get; set; }

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
