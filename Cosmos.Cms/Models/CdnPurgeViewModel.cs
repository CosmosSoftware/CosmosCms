using System.Text.Json.Serialization;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// CDN Purge response view model
    /// </summary>
    public class CdnPurgeViewModel
    {
        /// <summary>
        /// Purge IID
        /// </summary>
        // Example return: "{\"detail\": \"Request accepted\", \"estimatedSeconds\": 5, \"purgeId\": \"eda4653e-2379-11eb-9bda-9dd6666ed213\", \"supportId\": \"17PY1605029214948278-223626432\", \"httpStatus\": 201}"
        [JsonPropertyName("purgeId")] public string PurgeId { get; set; }
        /// <summary>
        /// Detailed information
        /// </summary>
        [JsonPropertyName("detail")] public string Detail { get; set; }
        /// <summary>
        /// Estimated seconds until refresh
        /// </summary>
        [JsonPropertyName("estimatedSeconds")] public int EstimatedSeconds { get; set; }
        /// <summary>
        /// Azure support ID
        /// </summary>
        [JsonPropertyName("supportId")] public string SupportId { get; set; }
        /// <summary>
        /// HTTP result status
        /// </summary>
        [JsonPropertyName("httpStatus")] public string HttpStatus { get; set; }
    }
}